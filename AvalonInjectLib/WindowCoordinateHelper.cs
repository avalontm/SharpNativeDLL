using System;
using System.Runtime.InteropServices;
using System.Text;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static class WindowCoordinateHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        private static IntPtr _gameWindowHandle = IntPtr.Zero;
        private static uint _targetProcessId;
        private static IntPtr _consoleHandle = IntPtr.Zero;

        /// <summary>
        /// Inicializa el helper con el proceso objetivo
        /// </summary>
        public static void Initialize(uint processId)
        {
            _targetProcessId = processId;
            _consoleHandle = GetConsoleWindow();
            FindGameWindow();
        }

        /// <summary>
        /// Busca la ventana principal del juego usando múltiples métodos
        /// </summary>
        private static void FindGameWindow()
        {
            // Método 1: Buscar por enumeración de ventanas
            EnumWindows(EnumWindowCallback, IntPtr.Zero);

            // Método 2: Si no encontramos nada, intentar con ventana en primer plano
            if (_gameWindowHandle == IntPtr.Zero)
            {
                UpdateGameWindowHandle();
            }
        }

        /// <summary>
        /// Callback para enumerar ventanas
        /// </summary>
        private static bool EnumWindowCallback(IntPtr hWnd, IntPtr lParam)
        {
            // Skip si es la consola
            if (hWnd == _consoleHandle)
                return true;

            // Verificar si la ventana pertenece al proceso objetivo
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid != _targetProcessId)
                return true;

            // Verificar si la ventana es visible
            if (!IsWindowVisible(hWnd))
                return true;

            // Obtener información de la ventana
            StringBuilder windowText = new StringBuilder(256);
            StringBuilder className = new StringBuilder(256);
            GetWindowText(hWnd, windowText, windowText.Capacity);
            GetClassName(hWnd, className, className.Capacity);

            string windowTitle = windowText.ToString();
            string classNameStr = className.ToString();

            // Filtrar consolas y ventanas no deseadas
            if (IsConsoleWindow(classNameStr, windowTitle))
                return true;

            // Verificar que tenga un área cliente válida
            if (GetClientRect(hWnd, out RECT clientRect))
            {
                if (clientRect.Width > 100 && clientRect.Height > 100) // Ventana de tamaño razonable
                {
                    _gameWindowHandle = hWnd;
                    return false; // Detener enumeración
                }
            }

            return true;
        }

        /// <summary>
        /// Verifica si una ventana es una consola
        /// </summary>
        private static bool IsConsoleWindow(string className, string windowTitle)
        {
            return className.ToLower().Contains("console") ||
                   windowTitle.ToLower().Contains("console") ||
                   className == "ConsoleWindowClass" ||
                   windowTitle.StartsWith("C:\\") || // Ventanas de consola típicas
                   windowTitle.Contains("cmd") ||
                   windowTitle.Contains("powershell");
        }

        /// <summary>
        /// Actualiza el handle de la ventana del juego (método de respaldo)
        /// </summary>
        private static void UpdateGameWindowHandle()
        {
            IntPtr foregroundWindow = GetForegroundWindow();

            // Skip si es la consola
            if (foregroundWindow == _consoleHandle)
                return;

            GetWindowThreadProcessId(foregroundWindow, out uint pid);

            if (pid == _targetProcessId)
            {
                // Verificar que no sea una ventana de consola
                StringBuilder className = new StringBuilder(256);
                StringBuilder windowTitle = new StringBuilder(256);
                GetClassName(foregroundWindow, className, className.Capacity);
                GetWindowText(foregroundWindow, windowTitle, windowTitle.Capacity);

                if (!IsConsoleWindow(className.ToString(), windowTitle.ToString()))
                {
                    _gameWindowHandle = foregroundWindow;
                }
            }
        }

        /// <summary>
        /// Fuerza la actualización del handle de la ventana
        /// </summary>
        public static void RefreshGameWindow()
        {
            _gameWindowHandle = IntPtr.Zero;
            FindGameWindow();
        }

        /// <summary>
        /// Obtiene información de debug sobre la ventana actual
        /// </summary>
        public static string GetWindowInfo()
        {
            if (_gameWindowHandle == IntPtr.Zero)
                return "No game window found";

            StringBuilder windowText = new StringBuilder(256);
            StringBuilder className = new StringBuilder(256);
            GetWindowText(_gameWindowHandle, windowText, windowText.Capacity);
            GetClassName(_gameWindowHandle, className, className.Capacity);

            GetWindowThreadProcessId(_gameWindowHandle, out uint pid);

            return $"Window Handle: {_gameWindowHandle}\n" +
                   $"Window Title: {windowText}\n" +
                   $"Class Name: {className}\n" +
                   $"Process ID: {pid}\n" +
                   $"Target Process ID: {_targetProcessId}";
        }

        /// <summary>
        /// Convierte coordenadas globales a coordenadas locales de la ventana del juego
        /// </summary>
        public static Vector2 GlobalToLocal(Vector2 globalPos)
        {
            if (_gameWindowHandle == IntPtr.Zero || !IsWindow(_gameWindowHandle))
            {
                RefreshGameWindow();
                if (_gameWindowHandle == IntPtr.Zero)
                    return globalPos; // Si no encontramos la ventana, devolver posición original
            }

            POINT point = new POINT { X = (int)globalPos.X, Y = (int)globalPos.Y };

            if (ScreenToClient(_gameWindowHandle, ref point))
            {
                return new Vector2(point.X, point.Y);
            }

            return globalPos;
        }

        /// <summary>
        /// Obtiene las dimensiones del área cliente de la ventana del juego
        /// </summary>
        public static Vector2 GetClientSize()
        {
            if (_gameWindowHandle == IntPtr.Zero || !IsWindow(_gameWindowHandle))
            {
                RefreshGameWindow();
                if (_gameWindowHandle == IntPtr.Zero)
                    return new Vector2(1920, 1080); // Valor por defecto
            }

            if (GetClientRect(_gameWindowHandle, out RECT clientRect))
            {
                return new Vector2(clientRect.Width, clientRect.Height);
            }

            return new Vector2(1920, 1080);
        }

        /// <summary>
        /// Verifica si el mouse está dentro del área cliente de la ventana
        /// </summary>
        public static bool IsMouseInWindow(Vector2 localPos)
        {
            var clientSize = GetClientSize();
            return localPos.X >= 0 && localPos.Y >= 0 &&
                   localPos.X < clientSize.X && localPos.Y < clientSize.Y;
        }

        /// <summary>
        /// Verifica si la ventana del juego está activa
        /// </summary>
        public static bool IsGameWindowActive()
        {
            if (_gameWindowHandle == IntPtr.Zero)
                return false;

            IntPtr foregroundWindow = GetForegroundWindow();
            return foregroundWindow == _gameWindowHandle;
        }
    }
}