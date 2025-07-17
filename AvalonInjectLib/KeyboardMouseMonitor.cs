using System.Runtime.InteropServices;
using System.Collections.Concurrent;

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

        [DllImport("kernel32.dll")]
        private static extern void Sleep(uint dwMilliseconds);

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
        private const int WM_MOUSEHWHEEL = 0x020E;
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
        private static volatile bool _isMonitoring = false;
        private static uint _targetProcessId;
        private static Action<InputEventArgs> _inputEventCallback;

        // Hook variables
        private static HookProc _hookProc;
        private static int _mouseHookId = 0;
        private static readonly object _hookLock = new object();

        // Estados con temporización optimizada
        private struct KeyState
        {
            internal bool CurrentState;
            internal bool PreviousState;
            internal long LastChangeTime;
        }

        private static KeyState[] _keyStates = new KeyState[256];
        private static POINT _lastMousePosition;

        // Constantes optimizadas para evitar congelamiento
        private const int DEBOUNCE_TIME_MS = 20;
        private const int MOUSE_MOVE_THRESHOLD = 2;
        private const int MONITOR_SLEEP_MS = 10;
        private const int ERROR_SLEEP_MS = 100;
        private const int HOOK_SLEEP_MS = 1;

        // Cache mejorado para ventana activa
        private static IntPtr _lastForegroundWindow = IntPtr.Zero;
        private static uint _lastForegroundProcessId = 0;
        private static long _lastWindowCheckTime = 0;
        private const int WINDOW_CHECK_INTERVAL_MS = 100; // Aumentado para reducir carga

        // Cola de eventos con procesamiento asíncrono mejorado
        private static readonly ConcurrentQueue<InputEventArgs> _eventQueue = new ConcurrentQueue<InputEventArgs>();
        private static Thread _eventProcessorThread;
        private static readonly AutoResetEvent _eventSignal = new AutoResetEvent(false);

        // Limitadores de frecuencia
        private static long _lastKeyboardScan = 0;
        private static long _lastMouseScan = 0;
        private const int KEYBOARD_SCAN_INTERVAL_MS = 8;
        private const int MOUSE_SCAN_INTERVAL_MS = 5;

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

            // Inicializar estado
            ResetState();

            // Procesador de eventos con menor prioridad
            _eventProcessorThread = new Thread(ProcessEventQueue)
            {
                Name = "EventProcessorThread",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal // Reducida para evitar interferencia
            };
            _eventProcessorThread.Start();

            // Hook con prioridad normal (NO Highest)
            _hookThread = new Thread(HookThreadProc)
            {
                Name = "MouseHookThread",
                IsBackground = true,
                Priority = ThreadPriority.Normal // Cambiado de Highest a Normal
            };
            _hookThread.SetApartmentState(ApartmentState.STA);
            _hookThread.Start();

            Thread.Sleep(100); // Dar tiempo al hook para inicializarse

            // Monitor principal con prioridad baja
            _monitorThread = new Thread(MonitorLoop)
            {
                Name = "KeyboardMouseMonitorThread",
                Priority = ThreadPriority.BelowNormal, // Reducida para evitar bloqueo
                IsBackground = true
            };
            _monitorThread.Start();

            Logger.Debug("monitoring started successfully", "Monitor");
        }

        internal static void StopMonitoring()
        {
            if (!_isMonitoring) return;

            Logger.Debug("Stopping monitoring...", "Monitor");
            _isMonitoring = false;

            // Señalar al procesador de eventos
            _eventSignal.Set();

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

            // Terminar hilos con timeout más generoso
            TerminateThread(_hookThread, "Hook", 2000);
            TerminateThread(_monitorThread, "Monitor", 1000);
            TerminateThread(_eventProcessorThread, "EventProcessor", 1000);

            Logger.Debug("Monitoring stopped", "Monitor");
        }

        private static void ResetState()
        {
            _lastForegroundWindow = IntPtr.Zero;
            _lastForegroundProcessId = 0;
            _lastWindowCheckTime = 0;
            _lastKeyboardScan = 0;
            _lastMouseScan = 0;

            GetCursorPos(out _lastMousePosition);

            // Limpiar cola de eventos
            while (_eventQueue.TryDequeue(out _)) { }

            // Inicializar estados de teclas
            for (int i = 0; i < _keyStates.Length; i++)
            {
                _keyStates[i] = new KeyState();
            }
        }

        private static void TerminateThread(Thread thread, string name, int timeoutMs)
        {
            if (thread != null && thread.IsAlive)
            {
                if (thread.Name == "MouseHookThread")
                {
                    PostThreadMessage((uint)thread.ManagedThreadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
                }

                if (!thread.Join(timeoutMs))
                {
                    Logger.Debug($"{name} thread did not terminate gracefully", "Monitor");
                }
            }
        }

        private static void HookThreadProc()
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
                        return;
                    }
                    else
                    {
                        Logger.Debug($"Mouse hook installed successfully. Hook ID: {_mouseHookId}", "Monitor");
                    }
                }

                // Message loop optimizado con yield
                MSG msg;
                while (_isMonitoring)
                {
                    int result = GetMessage(out msg, IntPtr.Zero, 0, 0);
                    if (result == 0 || result == -1) break;

                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);

                    // Pequeño yield para evitar monopolizar CPU
                    if (msg.message == WM_MOUSEWHEEL || msg.message == WM_MOUSEHWHEEL)
                    {
                        Sleep(HOOK_SLEEP_MS);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Hook thread error: {ex.Message}", "Monitor");
            }
        }

        private static void ProcessEventQueue()
        {
            while (_isMonitoring)
            {
                try
                {
                    bool hasEvents = false;

                    // Procesar múltiples eventos en lote
                    for (int i = 0; i < 10 && _eventQueue.TryDequeue(out InputEventArgs eventArgs); i++)
                    {
                        _inputEventCallback?.Invoke(eventArgs);
                        hasEvents = true;
                    }

                    if (!hasEvents)
                    {
                        // Esperar señal o timeout
                        _eventSignal.WaitOne(50);
                    }
                    else
                    {
                        // Pequeño yield después de procesar eventos
                        Thread.Sleep(1);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Event processor error: {ex.Message}", "Monitor");
                    Thread.Sleep(50);
                }
            }
        }

        private static void EnqueueEvent(InputEventArgs eventArgs)
        {
            const int MAX_QUEUE_SIZE = 50; // Reducido para evitar acumulación

            if (_eventQueue.Count < MAX_QUEUE_SIZE)
            {
                _eventQueue.Enqueue(eventArgs);
                _eventSignal.Set(); // Señalar al procesador
            }
        }

        private static bool IsTargetWindowActive()
        {
            long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            // Cache más agresivo para reducir llamadas
            if (currentTime - _lastWindowCheckTime < WINDOW_CHECK_INTERVAL_MS)
            {
                return _lastForegroundProcessId == _targetProcessId;
            }

            try
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd != _lastForegroundWindow)
                {
                    _lastForegroundWindow = hWnd;
                    GetWindowThreadProcessId(hWnd, out _lastForegroundProcessId);
                }

                _lastWindowCheckTime = currentTime;
                return _lastForegroundProcessId == _targetProcessId;
            }
            catch
            {
                // En caso de error, asumir que no está activa
                return false;
            }
        }

        private static int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                // Procesar solo si es necesario
                if (nCode >= 0 && _isMonitoring)
                {
                    int message = wParam.ToInt32();

                    if (message == WM_MOUSEWHEEL || message == WM_MOUSEHWHEEL)
                    {
                        // Verificación rápida sin bloqueo
                        if (_lastForegroundProcessId == _targetProcessId ||
                            (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - _lastWindowCheckTime > WINDOW_CHECK_INTERVAL_MS && IsTargetWindowActive()))
                        {
                            MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                            int wheelDelta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);

                            var inputEvent = new InputEventArgs
                            {
                                Type = InputType.MouseWheel,
                                MouseX = hookStruct.pt.X,
                                MouseY = hookStruct.pt.Y,
                                WheelDelta = wheelDelta,
                                IsHorizontalWheel = message == WM_MOUSEHWHEEL
                            };

                            EnqueueEvent(inputEvent);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"MouseHookProc error: {ex.Message}", "Monitor");
            }

            // CRÍTICO: Siempre llamar al siguiente hook inmediatamente
            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        private static void MonitorLoop()
        {
            while (_isMonitoring)
            {
                try
                {
                    if (IsTargetWindowActive())
                    {
                        long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

                        // Limitar frecuencia de escaneo
                        if (currentTime - _lastKeyboardScan >= KEYBOARD_SCAN_INTERVAL_MS)
                        {
                            ScanKeyboard();
                            _lastKeyboardScan = currentTime;
                        }

                        if (currentTime - _lastMouseScan >= MOUSE_SCAN_INTERVAL_MS)
                        {
                            ScanMouse();
                            _lastMouseScan = currentTime;
                        }
                    }

                    // Sleep más generoso para evitar monopolizar CPU
                    Thread.Sleep(MONITOR_SLEEP_MS);
                }
                catch (Exception ex)
                {
                    Logger.Error($"KeyboardMouseMonitor error: {ex.Message}", "Monitor");
                    Thread.Sleep(ERROR_SLEEP_MS);
                }
            }
        }

        private static void ScanKeyboard()
        {
            long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            // Escanear solo teclas esenciales para reducir carga
            var essentialKeys = new int[] {
                0x20, 0x0D, 0x1B, 0x08, 0x09, // Space, Enter, Escape, Backspace, Tab
                0x10, 0x11, 0x12, // Shift, Ctrl, Alt
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, // A-J
                0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50, 0x51, 0x52, 0x53, 0x54, // K-T
                0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, // U-Z
                0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, // 0-9
                0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, // F1-F12
                0x25, 0x26, 0x27, 0x28, // Arrow keys
                0x21, 0x22, 0x23, 0x24 // PageUp, PageDown, End, Home
            };

            foreach (int vkCode in essentialKeys)
            {
                if (!_isMonitoring) break;

                try
                {
                    short state = GetAsyncKeyState(vkCode);
                    bool isPressed = (state & 0x8000) != 0;

                    if (currentTime - _keyStates[vkCode].LastChangeTime > DEBOUNCE_TIME_MS)
                    {
                        if (isPressed != _keyStates[vkCode].CurrentState)
                        {
                            _keyStates[vkCode].PreviousState = _keyStates[vkCode].CurrentState;
                            _keyStates[vkCode].CurrentState = isPressed;
                            _keyStates[vkCode].LastChangeTime = currentTime;

                            EnqueueEvent(new InputEventArgs
                            {
                                Type = InputType.Keyboard,
                                KeyCode = vkCode,
                                IsPressed = isPressed,
                                KeyName = GetKeyName(vkCode)
                            });
                        }
                    }
                }
                catch
                {
                    // Ignorar errores individuales de teclas
                    continue;
                }
            }
        }

        private static void ScanMouse()
        {
            long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            try
            {
                // Movimiento del mouse
                if (GetCursorPos(out POINT currentPos))
                {
                    int deltaX = Math.Abs(currentPos.X - _lastMousePosition.X);
                    int deltaY = Math.Abs(currentPos.Y - _lastMousePosition.Y);

                    if (deltaX > MOUSE_MOVE_THRESHOLD || deltaY > MOUSE_MOVE_THRESHOLD)
                    {
                        EnqueueEvent(new InputEventArgs
                        {
                            Type = InputType.MouseMove,
                            MouseX = currentPos.X,
                            MouseY = currentPos.Y
                        });

                        _lastMousePosition = currentPos;
                    }
                }

                // Botones del mouse
                int[] mouseButtons = { VK_LBUTTON, VK_RBUTTON, VK_MBUTTON };

                foreach (int button in mouseButtons)
                {
                    if (!_isMonitoring) break;

                    try
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

                                EnqueueEvent(new InputEventArgs
                                {
                                    Type = InputType.MouseButton,
                                    KeyCode = button,
                                    IsPressed = isPressed,
                                    ButtonName = GetMouseButtonName(button),
                                    MouseX = _lastMousePosition.X,
                                    MouseY = _lastMousePosition.Y
                                });
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch
            {
                // Ignorar errores de mouse
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