using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AvalonInjectLib
{
    internal static class KeyboardMouseMonitor
    {
        // Keyboard imports
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // Mouse imports
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        // Hook imports para detectar mouse wheel
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Message loop imports
        [DllImport("user32.dll")]
        private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

        [DllImport("user32.dll")]
        private static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // Hook constants
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E; // Horizontal wheel
        private const uint WM_QUIT = 0x0012;

        // Hook delegate
        private delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Mouse button virtual key codes
        private const int VK_LBUTTON = 0x01;
        private const int VK_RBUTTON = 0x02;
        private const int VK_MBUTTON = 0x04;
        private const int VK_XBUTTON1 = 0x05;
        private const int VK_XBUTTON2 = 0x06;

        private static Thread _monitorThread;
        private static Thread _hookThread;
        private static bool _isMonitoring = false;
        private static uint _targetProcessId;
        private static Action<InputEventArgs> _inputEventCallback;

        // Hook variables
        private static HookProc _hookProc;
        private static int _mouseHookId = 0;
        private static readonly object _hookLock = new object();

        // Estados mejorados con temporización
        private struct mKeyState
        {
            internal bool CurrentState;
            internal bool PreviousState;
            internal long LastChangeTime;
        }

        private static mKeyState[] _keyStates = new mKeyState[256];
        private static POINT _lastMousePosition;
        private const int DEBOUNCE_TIME_MS = 20;
        private const int MOUSE_MOVE_THRESHOLD = 2;

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

            Logger.Debug("Starting monitoring...", "Monitor");

            GetCursorPos(out _lastMousePosition);

            // Instalar hook en un hilo separado para evitar congelamiento
            _hookThread = new Thread(() =>
            {
                try
                {
                    Logger.Debug("Installing mouse hook...", "Monitor");
                    lock (_hookLock)
                    {
                        _hookProc = MouseHookProc;
                        _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _hookProc, GetModuleHandle(null), 0);

                        if (_mouseHookId == 0)
                        {
                            int error = Marshal.GetLastWin32Error();
                            Logger.Error($"Failed to install mouse hook. Error: {error}", "Monitor");
                        }
                        else
                        {
                              Logger.Debug($"Mouse hook installed successfully. Hook ID: {_mouseHookId}", "Monitor");
                        }
                    }

                    // Message loop para el hook
                    MSG msg;
                    while (_isMonitoring && GetMessage(out msg, IntPtr.Zero, 0, 0) != 0)
                    {
                        TranslateMessage(ref msg);
                        DispatchMessage(ref msg);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Hook thread error: {ex.Message}", "Monitor");
                }
            })
            {
                Name = "MouseHookThread",
                IsBackground = true
            };
            _hookThread.SetApartmentState(ApartmentState.STA);
            _hookThread.Start();

            // Esperar un poco para que el hook se instale
            Thread.Sleep(100);

            _monitorThread = new Thread(MonitorLoop)
            {
                Name = "KeyboardMouseMonitorThread",
                Priority = ThreadPriority.AboveNormal,
                IsBackground = true
            };
            _monitorThread.Start();

            Logger.Debug("Monitoring started successfully", "Monitor");
        }

        internal static void StopMonitoring()
        {
            if (!_isMonitoring) return;

            Logger.Debug("Stopping monitoring...", "Monitor");
            _isMonitoring = false;

            // Desinstalar hook de forma segura
            lock (_hookLock)
            {
                if (_mouseHookId != 0)
                {
                    Logger.Debug("Uninstalling mouse hook...", "Monitor");
                    bool success = UnhookWindowsHookEx(_mouseHookId);
                    Logger.Debug($"Hook uninstalled: {success}", "Monitor");
                    _mouseHookId = 0;
                }
            }

            // Terminar el hilo del hook
            if (_hookThread != null && _hookThread.IsAlive)
            {
                PostThreadMessage((uint)_hookThread.ManagedThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
                _hookThread.Join(500);
            }

            _monitorThread?.Join(100);
            Logger.Debug("Monitoring stopped", "Monitor");
        }

        private static int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && _isMonitoring)
                {
                    int message = wParam.ToInt32();

                    if (message == WM_MOUSEWHEEL || message == WM_MOUSEHWHEEL)
                    {
   
                        // Verificar si la ventana activa pertenece al proceso objetivo
                        IntPtr hWnd = GetForegroundWindow();
                        GetWindowThreadProcessId(hWnd, out uint pid);

                        if (pid == _targetProcessId)
                        {
                            MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                            // Extraer el delta de la rueda del mouse
                            int wheelDelta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);

                            var inputEvent = new InputEventArgs
                            {
                                Type = InputType.MouseWheel,
                                MouseX = hookStruct.pt.X,
                                MouseY = hookStruct.pt.Y,
                                WheelDelta = wheelDelta,
                                IsHorizontalWheel = message == WM_MOUSEHWHEEL
                            };

                            _inputEventCallback?.Invoke(inputEvent);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"MouseHookProc error: {ex.Message}", "Monitor");
            }

            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        private static void MonitorLoop()
        {
            while (_isMonitoring)
            {
                try
                {
                    IntPtr hWnd = GetForegroundWindow();
                    GetWindowThreadProcessId(hWnd, out uint pid);

                    if (pid == _targetProcessId)
                    {
                        ScanKeyboard();
                        ScanMouse();
                    }

                    Thread.Sleep(10);
                }
                catch (Exception ex)
                {
                    Logger.Error($"KeyboardMouseMonitor error: {ex.Message}", "Monitor");
                    Thread.Sleep(100);
                }
            }
        }

        private static void ScanKeyboard()
        {
            long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            for (int vkCode = 1; vkCode < 256; vkCode++)
            {
                if (vkCode == VK_LBUTTON || vkCode == VK_RBUTTON || vkCode == VK_MBUTTON ||
                    vkCode == VK_XBUTTON1 || vkCode == VK_XBUTTON2)
                    continue;

                short state = GetAsyncKeyState(vkCode);
                bool isPressed = (state & 0x8000) != 0;

                if (currentTime - _keyStates[vkCode].LastChangeTime > DEBOUNCE_TIME_MS)
                {
                    if (isPressed != _keyStates[vkCode].CurrentState)
                    {
                        _keyStates[vkCode].PreviousState = _keyStates[vkCode].CurrentState;
                        _keyStates[vkCode].CurrentState = isPressed;
                        _keyStates[vkCode].LastChangeTime = currentTime;

                        if (_keyStates[vkCode].CurrentState != _keyStates[vkCode].PreviousState)
                        {
                            _inputEventCallback?.Invoke(new InputEventArgs
                            {
                                Type = InputType.Keyboard,
                                KeyCode = vkCode,
                                IsPressed = isPressed,
                                KeyName = GetKeyName(vkCode)
                            });
                        }
                    }
                }
            }
        }

        private static void ScanMouse()
        {
            long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            if (GetCursorPos(out POINT currentPos))
            {
                if (Math.Abs(currentPos.X - _lastMousePosition.X) > MOUSE_MOVE_THRESHOLD ||
                    Math.Abs(currentPos.Y - _lastMousePosition.Y) > MOUSE_MOVE_THRESHOLD)
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

            int[] mouseButtons = { VK_LBUTTON, VK_RBUTTON, VK_MBUTTON, VK_XBUTTON1, VK_XBUTTON2 };

            foreach (int button in mouseButtons)
            {
                short state = GetAsyncKeyState(button);
                bool isPressed = (state & 0x8000) != 0;

                if (currentTime - _keyStates[button].LastChangeTime > DEBOUNCE_TIME_MS)
                {
                    if (isPressed != _keyStates[button].CurrentState)
                    {
                        _keyStates[button].PreviousState = _keyStates[button].CurrentState;
                        _keyStates[button].CurrentState = isPressed;
                        _keyStates[button].LastChangeTime = currentTime;

                        if (_keyStates[button].CurrentState != _keyStates[button].PreviousState)
                        {
                            GetCursorPos(out POINT mousePos);

                            _inputEventCallback?.Invoke(new InputEventArgs
                            {
                                Type = InputType.MouseButton,
                                KeyCode = button,
                                IsPressed = isPressed,
                                ButtonName = GetMouseButtonName(button),
                                MouseX = mousePos.X,
                                MouseY = mousePos.Y
                            });
                        }
                    }
                }
            }
        }

        private static string GetKeyName(int vkCode)
        {
            return vkCode switch
            {
                0x08 => "Backspace",
                0x09 => "Tab",
                0x0D => "Enter",
                0x10 => "Shift",
                0x11 => "Ctrl",
                0x12 => "Alt",
                0x13 => "Pause",
                0x14 => "CapsLock",
                0x1B => "Escape",
                0x20 => "Space",
                0x21 => "PageUp",
                0x22 => "PageDown",
                0x23 => "End",
                0x24 => "Home",
                0x25 => "Left",
                0x26 => "Up",
                0x27 => "Right",
                0x28 => "Down",
                0x2C => "PrintScreen",
                0x2D => "Insert",
                0x2E => "Delete",
                0x30 => "0",
                0x31 => "1",
                0x32 => "2",
                0x33 => "3",
                0x34 => "4",
                0x35 => "5",
                0x36 => "6",
                0x37 => "7",
                0x38 => "8",
                0x39 => "9",
                0x41 => "A",
                0x42 => "B",
                0x43 => "C",
                0x44 => "D",
                0x45 => "E",
                0x46 => "F",
                0x47 => "G",
                0x48 => "H",
                0x49 => "I",
                0x4A => "J",
                0x4B => "K",
                0x4C => "L",
                0x4D => "M",
                0x4E => "N",
                0x4F => "O",
                0x50 => "P",
                0x51 => "Q",
                0x52 => "R",
                0x53 => "S",
                0x54 => "T",
                0x55 => "U",
                0x56 => "V",
                0x57 => "W",
                0x58 => "X",
                0x59 => "Y",
                0x5A => "Z",
                0x70 => "F1",
                0x71 => "F2",
                0x72 => "F3",
                0x73 => "F4",
                0x74 => "F5",
                0x75 => "F6",
                0x76 => "F7",
                0x77 => "F8",
                0x78 => "F9",
                0x79 => "F10",
                0x7A => "F11",
                0x7B => "F12",
                _ => $"Key{vkCode}"
            };
        }

        private static string GetMouseButtonName(int button)
        {
            return button switch
            {
                VK_LBUTTON => "LeftButton",
                VK_RBUTTON => "RightButton",
                VK_MBUTTON => "MiddleButton",
                VK_XBUTTON1 => "XButton1",
                VK_XBUTTON2 => "XButton2",
                _ => $"MouseButton{button}"
            };
        }
    }
}