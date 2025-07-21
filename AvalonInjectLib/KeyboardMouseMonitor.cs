using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    internal static class KeyboardMouseMonitor
    {
        private const int POLL_INTERVAL = 16;
        private const int DEBOUNCE_MS = 10;
        private const int MOUSE_MOVE_TOLERANCE = 2;

        private static readonly Dictionary<int, KeyState> _keyStates = new();
        private static System.Timers.Timer? _timer;
        private static Action<InputEventArgs>? _callback;
        private static volatile bool _isRunning = false;

        private static uint _targetPid;
        private static IntPtr _lastWindow = IntPtr.Zero;
        private static uint _lastPid = 0;
        private static long _lastWindowCheck = 0;
        private static POINT _lastMouse;

        private struct KeyState
        {
            public bool IsPressed;
            public long LastChange;
        }

        public enum InputType
        {
            Keyboard,
            MouseButton,
            MouseMove,
        }

        public class InputEventArgs
        {
            public InputType Type { get; set; }
            public int KeyCode { get; set; }
            public bool IsPressed { get; set; }
            public string KeyName { get; set; } = string.Empty;
            public string ButtonName { get; set; } = string.Empty;
            public int MouseX { get; set; }
            public int MouseY { get; set; }
        }

        internal static void StartMonitoring(uint targetPid, Action<InputEventArgs> onInput)
        {
            if (_isRunning) return;

            _isRunning = true;
            _targetPid = targetPid;
            _callback = onInput;

            _lastWindow = IntPtr.Zero;
            _lastPid = 0;
            _lastWindowCheck = 0;
            _keyStates.Clear();

            WinInterop.GetCursorPos(out _lastMouse);

            _timer = new System.Timers.Timer(POLL_INTERVAL)
            {
                AutoReset = true
            };
            _timer.Elapsed += (_, _) => Poll();
            _timer.Start();
        }

        internal static void StopMonitoring()
        {
            _isRunning = false;
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }

        private static void Poll()
        {
            if (!_isRunning || !IsTargetActive()) return;

            try
            {
                HandleMouse();
                HandleKeys();
            }
            catch (Exception ex)
            {
                Logger.Error($"[Monitor] Poll error: {ex.Message}");
            }
        }

        private static bool IsTargetActive()
        {
            long now = Environment.TickCount64;

            if (now - _lastWindowCheck < 500)
                return _lastPid == _targetPid;

            _lastWindowCheck = now;

            try
            {
                IntPtr hWnd = WinInterop.GetForegroundWindow();
                if (hWnd != _lastWindow)
                {
                    _lastWindow = hWnd;
                    WinInterop.GetWindowThreadProcessId(hWnd, out _lastPid);
                }

                return _lastPid == _targetPid;
            }
            catch
            {
                return false;
            }
        }

        private static void HandleMouse()
        {
            WinInterop.GetCursorPos(out POINT current);
            int dx = Math.Abs(current.X - _lastMouse.X);
            int dy = Math.Abs(current.Y - _lastMouse.Y);

            if (dx > MOUSE_MOVE_TOLERANCE || dy > MOUSE_MOVE_TOLERANCE)
            {
                _callback?.Invoke(new InputEventArgs
                {
                    Type = InputType.MouseMove,
                    MouseX = current.X,
                    MouseY = current.Y
                });

                _lastMouse = current;
            }

            CheckMouseButton(0x01, "Left");
            CheckMouseButton(0x02, "Right");
            CheckMouseButton(0x04, "Middle");
        }

        private static void CheckMouseButton(int vk, string name)
        {
            short state = WinInterop.GetAsyncKeyState(vk);
            bool pressed = (state & 0x8000) != 0;
            HandleStateChange(vk, pressed, InputType.MouseButton, name);
        }

        private static void HandleKeys()
        {
            int[] keys = [
                // Letters
                ..Enumerable.Range(0x41, 26),
                // Numbers
                ..Enumerable.Range(0x30, 10),
                // Arrows
                0x25, 0x26, 0x27, 0x28,
                // Modifiers
                0x10, 0x11, 0x12, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5,
                // Common keys
                0x20, 0x0D, 0x1B, 0x08, 0x09,
                // F1-F4
                0x70, 0x71, 0x72, 0x73
            ];

            foreach (int vk in keys)
            {
                short state = WinInterop.GetAsyncKeyState(vk);
                bool pressed = (state & 0x8000) != 0;
                HandleStateChange(vk, pressed, InputType.Keyboard);
            }
        }

        private static void HandleStateChange(int key, bool isPressed, InputType type, string buttonName = "")
        {
            long now = Environment.TickCount64;

            if (!_keyStates.TryGetValue(key, out var state))
                state = new KeyState();

            if (now - state.LastChange > DEBOUNCE_MS && isPressed != state.IsPressed)
            {
                state.IsPressed = isPressed;
                state.LastChange = now;
                _keyStates[key] = state;

                _callback?.Invoke(new InputEventArgs
                {
                    Type = type,
                    KeyCode = key,
                    IsPressed = isPressed,
                    KeyName = type == InputType.Keyboard ? Keyboard.GetKeyName(key) : "",
                    ButtonName = type == InputType.MouseButton ? buttonName : "",
                    MouseX = _lastMouse.X,
                    MouseY = _lastMouse.Y
                });
            }
        }
    }
}
