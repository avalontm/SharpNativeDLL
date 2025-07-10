using System;
using System.Runtime.InteropServices;
using System.Threading;

public static class KeyboardMonitor
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private static Thread _monitorThread;
    private static bool _isMonitoring = false;
    private static uint _targetProcessId;
    private static Action<int, bool> _keyEventCallback;

    // Estados mejorados con temporización
    private struct KeyState
    {
        public bool CurrentState;
        public bool PreviousState;
        public long LastChangeTime;
    }

    private static KeyState[] _keyStates = new KeyState[256];
    private const int DEBOUNCE_TIME_MS = 20; // Tiempo mínimo entre cambios de estado

    public static void StartMonitoring(uint targetProcessId, Action<int, bool> onKeyEvent)
    {
        if (_isMonitoring) return;

        _targetProcessId = targetProcessId;
        _keyEventCallback = onKeyEvent;
        _isMonitoring = true;

        _monitorThread = new Thread(MonitorLoop)
        {
            Name = "KeyboardMonitorThread",
            Priority = ThreadPriority.AboveNormal,
            IsBackground = true
        };
        _monitorThread.Start();
    }

    public static void StopMonitoring()
    {
        _isMonitoring = false;
        _monitorThread?.Join(100);
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
                }

                Thread.Sleep(10); // 10ms = ~100 checks/segundo
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KeyboardMonitor error: {ex.Message}");
                Thread.Sleep(100);
            }
        }
    }

    private static void ScanKeyboard()
    {
        long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

        for (int vkCode = 1; vkCode < 256; vkCode++)
        {
            short state = GetAsyncKeyState(vkCode);
            bool isPressed = (state & 0x8000) != 0;

            // Solo procesar si ha pasado el tiempo de debounce
            if (currentTime - _keyStates[vkCode].LastChangeTime > DEBOUNCE_TIME_MS)
            {
                if (isPressed != _keyStates[vkCode].CurrentState)
                {
                    _keyStates[vkCode].PreviousState = _keyStates[vkCode].CurrentState;
                    _keyStates[vkCode].CurrentState = isPressed;
                    _keyStates[vkCode].LastChangeTime = currentTime;

                    // Solo notificar si es un cambio real (evitar fluctuaciones)
                    if (_keyStates[vkCode].CurrentState != _keyStates[vkCode].PreviousState)
                    {
                        _keyEventCallback?.Invoke(vkCode, isPressed);
                    }
                }
            }
        }
    }
}