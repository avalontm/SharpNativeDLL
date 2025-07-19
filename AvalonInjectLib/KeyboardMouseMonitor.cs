using System.Runtime.InteropServices;
using System.Collections.Generic;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    internal static class KeyboardMouseMonitor
    {
        // Solo constantes necesarias para polling
        private const int VK_LBUTTON = 0x01;
        private const int VK_RBUTTON = 0x02;
        private const int VK_MBUTTON = 0x04;

        private static volatile bool _isMonitoring = false;
        private static uint _targetProcessId;
        private static Action<InputEventArgs> _inputEventCallback;

        // Estados simples sin concurrency
        private struct KeyState
        {
            internal bool CurrentState;
            internal long LastChangeTime;
        }

        private static readonly Dictionary<int, KeyState> _keyStates = new Dictionary<int, KeyState>();
        private static POINT _lastMousePosition;

        // Configuración optimizada para AOT
        private const int POLLING_INTERVAL_MS = 16; // ~60 FPS
        private const int DEBOUNCE_TIME_MS = 10;
        private const int MOUSE_MOVE_THRESHOLD = 2;

        // Cache para ventana activa
        private static IntPtr _lastForegroundWindow = IntPtr.Zero;
        private static uint _lastForegroundProcessId = 0;
        private static long _lastWindowCheckTime = 0;
        private const int WINDOW_CHECK_INTERVAL_MS = 500; // Verificar menos frecuentemente

        // Timer simple compatible con AOT
        private static System.Timers.Timer _pollingTimer;

        public class InputEventArgs
        {
            public InputType Type { get; set; }
            public int KeyCode { get; set; }
            public bool IsPressed { get; set; }
            public string KeyName { get; set; }
            public int MouseX { get; set; }
            public int MouseY { get; set; }
            public string ButtonName { get; set; }
            public int WheelDelta { get; set; }
            public bool IsHorizontalWheel { get; set; }
        }

        public enum InputType
        {
            Keyboard,
            MouseButton,
            MouseMove,
            MouseWheel
        }

        internal static void StartMonitoring(uint targetProcessId, Action<InputEventArgs> onInputEvent, Action<string> onLog = null)
        {
            if (_isMonitoring) return;

            _targetProcessId = targetProcessId;
            _inputEventCallback = onInputEvent;
            _isMonitoring = true;

            Logger.Debug("Starting simple polling monitoring...", "Monitor");

            // Inicializar estado
            ResetState();

            // Timer simple sin threads adicionales
            _pollingTimer = new System.Timers.Timer(POLLING_INTERVAL_MS);
            _pollingTimer.Elapsed += OnTimerTick;
            _pollingTimer.AutoReset = true;
            _pollingTimer.Start();

            Logger.Debug("Simple monitoring started successfully", "Monitor");
        }

        internal static void StopMonitoring()
        {
            if (!_isMonitoring) return;

            Logger.Debug("Stopping monitoring...", "Monitor");
            _isMonitoring = false;

            // Detener timer
            _pollingTimer?.Stop();
            _pollingTimer?.Dispose();
            _pollingTimer = null;

            Logger.Debug("Monitoring stopped", "Monitor");
        }

        private static void ResetState()
        {
            _lastForegroundWindow = IntPtr.Zero;
            _lastForegroundProcessId = 0;
            _lastWindowCheckTime = 0;

            WinInterop.GetCursorPos(out _lastMousePosition);
            _keyStates.Clear();
        }

        private static void OnTimerTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_isMonitoring) return;

            try
            {
                // Solo procesar si la ventana objetivo está activa
                if (IsTargetWindowActive())
                {
                    CheckMouseInput();
                    CheckKeyboardInput();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Polling error: {ex.Message}", "Monitor");
            }
        }

        private static bool IsTargetWindowActive()
        {
            long currentTime = Environment.TickCount64;

            // Cache para reducir llamadas API
            if (currentTime - _lastWindowCheckTime < WINDOW_CHECK_INTERVAL_MS)
            {
                return _lastForegroundProcessId == _targetProcessId;
            }

            try
            {
                IntPtr hWnd = WinInterop.GetForegroundWindow();
                if (hWnd != _lastForegroundWindow)
                {
                    _lastForegroundWindow = hWnd;
                    WinInterop.GetWindowThreadProcessId(hWnd, out uint processId);
                    _lastForegroundProcessId = processId;
                }

                _lastWindowCheckTime = currentTime;
                return _lastForegroundProcessId == _targetProcessId;
            }
            catch
            {
                return false;
            }
        }

        private static void CheckMouseInput()
        {
            long currentTime = Environment.TickCount64;

            try
            {
                // Verificar posición del mouse
                if (WinInterop.GetCursorPos(out POINT currentPos))
                {
                    int deltaX = Math.Abs(currentPos.X - _lastMousePosition.X);
                    int deltaY = Math.Abs(currentPos.Y - _lastMousePosition.Y);

                    if (deltaX > MOUSE_MOVE_THRESHOLD || deltaY > MOUSE_MOVE_THRESHOLD)
                    {
                        _inputEventCallback?.Invoke(new InputEventArgs
                        {
                            Type = InputType.MouseMove,
                            MouseX = currentPos.X,
                            MouseY = currentPos.Y
                        });

                        _lastMousePosition = currentPos;
                    }
                }

                // Verificar botones del mouse
                CheckMouseButton(VK_LBUTTON, "LeftButton", currentTime);
                CheckMouseButton(VK_RBUTTON, "RightButton", currentTime);
                CheckMouseButton(VK_MBUTTON, "MiddleButton", currentTime);
            }
            catch
            {
                // Ignorar errores silenciosamente
            }
        }

        private static void CheckMouseButton(int button, string buttonName, long currentTime)
        {
            try
            {
                short state = WinInterop.GetAsyncKeyState(button);
                bool isPressed = (state & 0x8000) != 0;

                if (!_keyStates.TryGetValue(button, out KeyState keyState))
                {
                    keyState = new KeyState();
                    _keyStates[button] = keyState;
                }

                if (currentTime - keyState.LastChangeTime > DEBOUNCE_TIME_MS)
                {
                    if (isPressed != keyState.CurrentState)
                    {
                        keyState.CurrentState = isPressed;
                        keyState.LastChangeTime = currentTime;
                        _keyStates[button] = keyState;

                        _inputEventCallback?.Invoke(new InputEventArgs
                        {
                            Type = InputType.MouseButton,
                            KeyCode = button,
                            IsPressed = isPressed,
                            ButtonName = buttonName,
                            MouseX = _lastMousePosition.X,
                            MouseY = _lastMousePosition.Y
                        });
                    }
                }
            }
            catch
            {
                // Ignorar errores
            }
        }

        private static void CheckKeyboardInput()
        {
            long currentTime = Environment.TickCount64;

            // Lista ampliada de teclas más comunes
            var essentialKeys = new int[] {
                // Letras principales
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, // A-J
                0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50, 0x51, 0x52, 0x53, 0x54, // K-T
                0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, // U-Z
                
                // Números
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, // 0-9
                
                // Teclas especiales
                0x20, // Space
                0x0D, // Enter
                0x1B, // Escape
                0x08, // Backspace
                0x09, // Tab
                
                // Modificadores
                0x10, 0x11, 0x12, // Shift, Ctrl, Alt
                0xA0, 0xA1, // Left Shift, Right Shift
                0xA2, 0xA3, // Left Ctrl, Right Ctrl
                0xA4, 0xA5, // Left Alt, Right Alt
                
                // Flechas
                0x25, 0x26, 0x27, 0x28, // Left, Up, Right, Down
                
                // Funciones
                0x70, 0x71, 0x72, 0x73, // F1-F4
                
                // Numpad
                0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, // Numpad 0-9
            };

            foreach (int vkCode in essentialKeys)
            {
                if (!_isMonitoring) break;

                try
                {
                    // Usar tanto GetAsyncKeyState como GetKeyState para mejor detección
                    short asyncState = WinInterop.GetAsyncKeyState(vkCode);
                    short keyState = WinInterop.GetKeyState(vkCode);

                    // Una tecla está presionada si cualquiera de los dos métodos lo detecta
                    bool isPressed = ((asyncState & 0x8000) != 0) || ((keyState & 0x8000) != 0);

                    if (!_keyStates.TryGetValue(vkCode, out KeyState currentKeyState))
                    {
                        currentKeyState = new KeyState();
                        _keyStates[vkCode] = currentKeyState;
                    }

                    if (currentTime - currentKeyState.LastChangeTime > DEBOUNCE_TIME_MS)
                    {
                        if (isPressed != currentKeyState.CurrentState)
                        {
                            currentKeyState.CurrentState = isPressed;
                            currentKeyState.LastChangeTime = currentTime;
                            _keyStates[vkCode] = currentKeyState;

                            _inputEventCallback?.Invoke(new InputEventArgs
                            {
                                Type = InputType.Keyboard,
                                KeyCode = vkCode,
                                IsPressed = isPressed,
                                KeyName = Keyboard.GetKeyName(vkCode)
                            });
                        }
                    }
                }
                catch
                {
                    // Ignorar errores individuales
                }
            }
        }
    }
}