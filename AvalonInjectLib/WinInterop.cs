using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static unsafe class WinInterop
    {
        // Constants for process access rights (complete set)
        public const int PROCESS_CREATE_THREAD = 0x0002;
        public const int PROCESS_CREATE_PROCESS = 0x0080;

        // Previously defined constants for completeness:
        public const int PROCESS_VM_READ = 0x0010;
        public const int PROCESS_VM_WRITE = 0x0020;
        public const int PROCESS_VM_OPERATION = 0x0008;
        public const int PROCESS_QUERY_INFORMATION = 0x0400;
        public const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        public const uint PAGE_NOACCESS = 0x01;
        public const uint PAGE_READONLY = 0x02;
        public const uint PAGE_READWRITE = 0x04;
        public const uint PAGE_EXECUTE_READ = 0x20;
        public const uint PAGE_EXECUTE_READWRITE = 0x40;
        public const uint MEM_COMMIT = 0x1000;
        public const uint STILL_ACTIVE = 259;

        public const int LIST_MODULES_ALL = 0x03;
        public const uint TH32CS_SNAPPROCESS = 0x00000002;
        public const uint TH32CS_SNAPMODULE = 0x00000008;
        public const uint TH32CS_SNAPMODULE32 = 0x00000010;

        // Estructuras esenciales
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleOutputCP(uint wCodePageID);

        // API de kernel32 (optimizadas para NativeAOT)
        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentProcessId();

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32")]
        public static extern IntPtr OpenProcess(
             uint dwDesiredAccess,
             [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
             uint dwProcessId);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            void* lpBuffer,
            int nSize,
            out int lpNumberOfBytesRead);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            void* lpBuffer,
            int nSize,
            out int lpNumberOfBytesWritten);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool VirtualProtectEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flNewProtect,
            out uint lpflOldProtect);

        [DllImport("kernel32", SetLastError = true)]
        public static extern int VirtualQueryEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION lpBuffer,
            uint dwLength);

        [DllImport("kernel32", SetLastError = true)]
        public static extern uint GetLastError();

        // API de user32 esenciales
        [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowW(string lpClassName, string lpWindowName);

        [DllImport("user32", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // API de psapi para módulos
        [DllImport("psapi", SetLastError = true)]
        public static extern bool EnumProcessModulesEx(
            IntPtr hProcess,
            IntPtr[] lphModule,
            uint cb,
            out uint lpcbNeeded,
            int dwFilterFlag);

        [DllImport("psapi", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint GetModuleFileNameExW(
            IntPtr hProcess,
            IntPtr hModule,
            StringBuilder lpFilename,
            int nSize);

        // Métodos de conveniencia
        public static IntPtr OpenProcessWithFullAccess(uint processId)
        {
            return OpenProcess(PROCESS_ALL_ACCESS, false, processId);
        }

        public static bool IsProcessRunning(IntPtr hProcess)
        {
            return GetExitCodeProcess(hProcess, out uint exitCode) && exitCode == STILL_ACTIVE;
        }

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateThread(
            IntPtr lpThreadAttributes,   // Seguridad del thread (NULL por defecto)
            uint dwStackSize,           // Tamaño de stack (0 = default)
            ThreadStart lpStartAddress,  // Puntero a función
            IntPtr lpParameter,          // Parámetros (opcional)
            uint dwCreationFlags,        // Flags de creación
            out uint lpThreadId);       // ID del thread (salida)

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            out uint lpThreadId);

        [DllImport("kernel32")]
        public static extern bool DisableThreadLibraryCalls(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        [DllImport("kernel32")]
        public static extern void Sleep(uint dwMilliseconds);

        internal static nint OpenProcess(object value, bool v, uint processId)
        {
            throw new NotImplementedException();
        }


        /* CONSTANTES PARA OVERLAY */
        public const int GWL_EXSTYLE = -20;
        public const int GWL_STYLE = -16;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_CAPTION = 0x00C00000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int LWA_COLORKEY = 0x00000001;
        public const int SW_SHOW = 5;
        public const int HWND_TOPMOST = -1;
        public const int HWND_BOTTOM = 1;

        /* API FUNCTIONS */
        [DllImport("user32.dll", SetLastError = true)]
        public static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetLayeredWindowAttributes(
            IntPtr hwnd,
            uint crKey,
            byte bAlpha,
            uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(
            IntPtr hWnd,
            int X,
            int Y,
            int nWidth,
            int nHeight,
            bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr DefWindowProc(
            IntPtr hWnd,
            uint uMsg,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostQuitMessage(int nExitCode);

        [DllImport("kernel32")]
        public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        [DllImport("kernel32")]
        public static extern bool Module32First(IntPtr hSnapshot, ref MODULEENTRY32 lpme);
        
        [DllImport("kernel32")]
        public static extern bool Module32Next(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32", EntryPoint = "Module32FirstW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Module32FirstW(
       IntPtr hSnapshot,
       ref MODULEENTRY32W lpme);

        [DllImport("kernel32", EntryPoint = "Module32NextW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Module32NextW(
            IntPtr hSnapshot,
            ref MODULEENTRY32W lpme);

        [DllImport("kernel32", EntryPoint = "Process32FirstW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32FirstW(
            IntPtr hSnapshot,
            ref PROCESSENTRY32W lppe);

        [DllImport("kernel32", EntryPoint = "Process32NextW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool Process32NextW(
            IntPtr hSnapshot,
            ref PROCESSENTRY32W lppe);

        // Estructuras Wide (Unicode)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MODULEENTRY32W
        {
            public uint dwSize;
            public uint th32ModuleID;
            public uint th32ProcessID;
            public uint GlblcntUsage;
            public uint ProccntUsage;
            public IntPtr modBaseAddr;
            public uint modBaseSize;
            public IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExePath;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct PROCESSENTRY32W
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MODULEENTRY32
        {
            public uint dwSize;
            public uint th32ModuleID;
            public uint th32ProcessID;
            public uint GlblcntUsage;
            public uint ProccntUsage;
            public IntPtr modBaseAddr;
            public uint modBaseSize;
            public IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExePath;
        }

        /* HELPER METHODS */
        public static void SetWindowTransparency(IntPtr hWnd, uint colorKey)
        {
            SetWindowLong(hWnd, GWL_EXSTYLE, GetWindowLong(hWnd, GWL_EXSTYLE) | WS_EX_LAYERED);
            SetLayeredWindowAttributes(hWnd, colorKey, 0, LWA_COLORKEY);
        }

        public static void MakeClickThrough(IntPtr hWnd)
        {
            int style = GetWindowLong(hWnd, GWL_EXSTYLE);
            SetWindowLong(hWnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        /// <summary>
        /// Encuentra un proceso por nombre con mejor manejo de errores
        /// </summary>
        /// <param name="processName">Nombre del proceso (con o sin .exe)</param>
        /// <returns>Process ID o 0 si no se encuentra</returns>
        public static uint FindProcessId(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return 0;

            // Configurar consola para UTF-8
            SetConsoleOutputCP(65001);

            // Asegurar la extensión .exe
            if (!processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                processName += ".exe";

            IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
            if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1))
            {
                Debug.WriteLine($"Error al crear snapshot: {GetLastError()}");
                return 0;
            }

            try
            {
                PROCESSENTRY32W processEntry = new PROCESSENTRY32W();
                processEntry.dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32W>();

                if (!Process32FirstW(snapshot, ref processEntry))
                {
                    Debug.WriteLine($"Error en Process32FirstW: {GetLastError()}");
                    return 0;
                }

                do
                {
                    // Comparación más robusta con validación de string
                    if (!string.IsNullOrEmpty(processEntry.szExeFile) &&
                        string.Equals(processEntry.szExeFile, processName, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Proceso encontrado: {processEntry.szExeFile} (PID: {processEntry.th32ProcessID})");
                        return processEntry.th32ProcessID;
                    }
                } while (Process32NextW(snapshot, ref processEntry));

                Debug.WriteLine($"Proceso '{processName}' no encontrado");
                return 0;
            }
            finally
            {
                CloseHandle(snapshot);
            }
        }

        /// <summary>
        /// Obtiene la dirección base de un módulo con mejor manejo de errores
        /// </summary>
        /// <param name="processId">ID del proceso</param>
        /// <param name="moduleName">Nombre del módulo (con o sin .dll/.exe)</param>
        /// <returns>Dirección base del módulo o IntPtr.Zero si no se encuentra</returns>
        public static IntPtr GetModuleBase(uint processId, string moduleName)
        {
            if (processId == 0 || string.IsNullOrEmpty(moduleName))
                return IntPtr.Zero;

            // Intentar con TH32CS_SNAPMODULE32 también para procesos de 32 bits
            IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, processId);
            if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1))
            {
                Debug.WriteLine($"Error al crear snapshot de módulos para PID {processId}: {GetLastError()}");
                return IntPtr.Zero;
            }

            try
            {
                MODULEENTRY32W moduleEntry = new MODULEENTRY32W();
                moduleEntry.dwSize = (uint)Marshal.SizeOf<MODULEENTRY32W>();

                if (!Module32FirstW(snapshot, ref moduleEntry))
                {
                    Debug.WriteLine($"Error en Module32FirstW: {GetLastError()}");
                    return IntPtr.Zero;
                }

                do
                {
                    // Comparar tanto el nombre del módulo como la ruta completa
                    if ((!string.IsNullOrEmpty(moduleEntry.szModule) &&
                         string.Equals(moduleEntry.szModule, moduleName, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(moduleEntry.szExePath) &&
                         string.Equals(System.IO.Path.GetFileName(moduleEntry.szExePath), moduleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        Debug.WriteLine($"Módulo encontrado: {moduleEntry.szModule} en {moduleEntry.modBaseAddr:X}");
                        return moduleEntry.modBaseAddr;
                    }
                } while (Module32NextW(snapshot, ref moduleEntry));

                Debug.WriteLine($"Módulo '{moduleName}' no encontrado en proceso {processId}");
                return IntPtr.Zero;
            }
            finally
            {
                CloseHandle(snapshot);
            }
        }

        /// <summary>
        /// Método alternativo usando EnumProcessModulesEx (más confiable para algunos casos)
        /// </summary>
        /// <param name="processId">ID del proceso</param>
        /// <param name="moduleName">Nombre del módulo</param>
        /// <returns>Dirección base del módulo</returns>
        public static IntPtr GetModuleBaseAlternative(uint processId, string moduleName)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                Debug.WriteLine($"No se pudo abrir el proceso {processId}: {GetLastError()}");
                return IntPtr.Zero;
            }

            try
            {
                const int maxModules = 1024;
                IntPtr[] modules = new IntPtr[maxModules];

                if (!EnumProcessModulesEx(hProcess, modules, (uint)(maxModules * IntPtr.Size), out uint bytesNeeded, LIST_MODULES_ALL))
                {
                    Debug.WriteLine($"Error en EnumProcessModulesEx: {GetLastError()}");
                    return IntPtr.Zero;
                }

                int moduleCount = (int)(bytesNeeded / IntPtr.Size);

                for (int i = 0; i < moduleCount; i++)
                {
                    StringBuilder moduleNameBuilder = new StringBuilder(260);
                    if (GetModuleFileNameExW(hProcess, modules[i], moduleNameBuilder, 260) > 0)
                    {
                        string currentModuleName = System.IO.Path.GetFileName(moduleNameBuilder.ToString());
                        if (string.Equals(currentModuleName, moduleName, StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.WriteLine($"Módulo encontrado (método alternativo): {currentModuleName} en {modules[i]:X}");
                            return modules[i];
                        }
                    }
                }

                Debug.WriteLine($"Módulo '{moduleName}' no encontrado (método alternativo)");
                return IntPtr.Zero;
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }

        /// <summary>
        /// Lista todos los procesos en ejecución (útil para debugging)
        /// </summary>
        /// <returns>Lista de nombres de procesos</returns>
        public static string[] ListRunningProcesses()
        {
            var processes = new System.Collections.Generic.List<string>();

            // Configurar consola para UTF-8
            SetConsoleOutputCP(65001);

            IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
            if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1))
                return processes.ToArray();

            try
            {
                PROCESSENTRY32W processEntry = new PROCESSENTRY32W();
                processEntry.dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32W>();

                if (Process32FirstW(snapshot, ref processEntry))
                {
                    do
                    {
                        if (!string.IsNullOrEmpty(processEntry.szExeFile))
                        {
                            processes.Add($"{processEntry.szExeFile} (PID: {processEntry.th32ProcessID})");
                        }
                    } while (Process32NextW(snapshot, ref processEntry));
                }
            }
            finally
            {
                CloseHandle(snapshot);
            }

            return processes.ToArray();
        }

        /// <summary>
        /// Lista todos los módulos de un proceso (útil para debugging)
        /// </summary>
        /// <param name="processId">ID del proceso</param>
        /// <returns>Lista de nombres de módulos</returns>
        public static string[] ListProcessModules(uint processId)
        {
            var modules = new System.Collections.Generic.List<string>();

            IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, processId);
            if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1))
                return modules.ToArray();

            try
            {
                MODULEENTRY32W moduleEntry = new MODULEENTRY32W();
                moduleEntry.dwSize = (uint)Marshal.SizeOf<MODULEENTRY32W>();

                if (Module32FirstW(snapshot, ref moduleEntry))
                {
                    do
                    {
                        if (!string.IsNullOrEmpty(moduleEntry.szModule))
                        {
                            modules.Add($"{moduleEntry.szModule} -> {moduleEntry.modBaseAddr:X} (Size: {moduleEntry.modBaseSize})");
                        }
                    } while (Module32NextW(snapshot, ref moduleEntry));
                }
            }
            finally
            {
                CloseHandle(snapshot);
            }

            return modules.ToArray();
        }

        public static IntPtr CalculatePointer(IntPtr baseAddress, params int[] offsets)
        {
            long current = baseAddress.ToInt64();

            foreach (int offset in offsets)
            {
                current += offset;

                if (IntPtr.Size == 4 && current > uint.MaxValue)
                {
                    throw new OverflowException("Dirección fuera del espacio 32-bit");
                }
            }

            return new IntPtr(current);
        }

        public static T ReadMemory<T>(IntPtr hProcess, IntPtr address) where T : unmanaged
        {
            T value;
            ReadProcessMemory(hProcess, address, &value, sizeof(T), out _);
            return value;
        }

        public static void ExecuteRemoteFunction(IntPtr hProcess, IntPtr funcAddress, int parameter)
        {
            IntPtr threadHandle = CreateRemoteThread(
                hProcess,
                IntPtr.Zero,
                0,
                funcAddress,
                (IntPtr)parameter,
                0,
                out _);

            if (threadHandle != IntPtr.Zero)
                CloseHandle(threadHandle);
        }
    }
}