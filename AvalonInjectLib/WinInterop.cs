using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using static AvalonInjectLib.ProcessManager;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    internal static unsafe class WinInterop
    {
        // ================= CONSTANTS =================
        internal const uint TH32CS_SNAPMODULE = 0x00000008;
        internal const uint TH32CS_SNAPMODULE32 = 0x00000010;
        internal const uint TH32CS_SNAPPROCESS = 0x00000002;
        internal const IntPtr INVALID_HANDLE_VALUE = (IntPtr)(-1);
        internal const ushort IMAGE_DOS_SIGNATURE = 0x5A4D;
        internal const uint IMAGE_NT_SIGNATURE = 0x00004550;
        internal const int IMAGE_DIRECTORY_ENTRY_EXPORT = 0;

        // Process access rights constants
        internal const int PROCESS_CREATE_THREAD = 0x0002;
        internal const int PROCESS_CREATE_PROCESS = 0x0080;
        internal const int PROCESS_VM_READ = 0x0010;
        internal const int PROCESS_VM_WRITE = 0x0020;
        internal const int PROCESS_VM_OPERATION = 0x0008;
        internal const int PROCESS_QUERY_INFORMATION = 0x0400;
        internal const int PROCESS_ALL_ACCESS = 0x1F0FFF;

        // Las que te faltan:
        internal const int PROCESS_TERMINATE = 0x0001;
        internal const int PROCESS_DUP_HANDLE = 0x0040;
        internal const int PROCESS_SET_INFORMATION = 0x0200;
        internal const int PROCESS_SET_QUOTA = 0x0100;
        internal const int PROCESS_SUSPEND_RESUME = 0x0800;
        internal const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        // Accesos genéricos (útiles)
        internal const int SYNCHRONIZE = 0x00100000;
        internal const int DELETE = 0x00010000;
        internal const int READ_CONTROL = 0x00020000;
        internal const int WRITE_DAC = 0x00040000;
        internal const int WRITE_OWNER = 0x00080000;

        // Máscaras combinadas útiles
        internal const int PROCESS_STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        internal const int PROCESS_SYNCHRONIZE = SYNCHRONIZE;

        // Actualización del PROCESS_ALL_ACCESS más precisa
        internal const int PROCESS_ALL_ACCESS_COMPLETE = 0x001FFFFF; // Incluye todos los bits

        // Memory protection constants
        internal const uint PAGE_NOACCESS = 0x01;
        internal const uint PAGE_READONLY = 0x02;
        internal const uint PAGE_READWRITE = 0x04;
        internal const uint PAGE_EXECUTE_READ = 0x20;
        internal const uint PAGE_EXECUTE_READWRITE = 0x40;
        internal const uint MEM_COMMIT = 0x1000;
        internal const uint MEM_RESERVE = 0x00002000;
        internal const uint MEM_RELEASE = 0x00008000;
        internal const uint STILL_ACTIVE = 259;

        internal const int LIST_MODULES_ALL = 0x03;
        internal const uint GW_OWNER = 4;

        // Overlay window constants
        internal const int GWL_EXSTYLE = -20;
        internal const int GWL_STYLE = -16;
        internal const int WS_EX_LAYERED = 0x00080000;
        internal const int WS_EX_TRANSPARENT = 0x00000020;
        internal const int WS_CAPTION = 0x00C00000;
        internal const int WS_THICKFRAME = 0x00040000;
        internal const int LWA_COLORKEY = 0x00000001;
        internal const int SW_SHOW = 5;
        internal const int HWND_TOPMOST = -1;
        internal const int HWND_BOTTOM = 1;

        // Wait constants
        internal const uint INFINITE = 0xFFFFFFFF;
        internal const uint WAIT_OBJECT_0 = 0x00000000;
        internal const uint WAIT_TIMEOUT = 0x00000102;
        internal const uint WAIT_FAILED = 0xFFFFFFFF;

        [Flags]
        internal enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
        }

        // ================= KERNEL32 APIs =================

        /// <summary>
        /// Terminates a thread
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool TerminateThread(IntPtr hThread, uint dwExitCode);


        /// <summary>
        /// Sets the output code page used by the console
        /// </summary>
        [DllImport("kernel32.dll")]
        internal static extern bool SetConsoleOutputCP(uint wCodePageID);

        /// <summary>
        /// Retrieves a pseudo handle for the current process
        /// </summary>
        [DllImport("kernel32", SetLastError = true)]
        internal static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// Retrieves the process identifier of the calling process
        /// </summary>
        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentProcessId();

        [Flags]
        internal enum ThreadAccess : int
        {
            SUSPEND_RESUME = 0x0002,
            TERMINATE = 0x0001,
        }

        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll")]
        internal static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll")]
        internal static extern uint ResumeThread(IntPtr hThread);

        /// <summary>
        /// Closes an open object handle
        /// </summary>
        [DllImport("kernel32", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Opens an existing local process object
        /// </summary>
        [DllImport("kernel32")]
        internal static extern IntPtr OpenProcess(
             uint dwDesiredAccess,
             [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
             uint dwProcessId);

        /// <summary>
        /// Reads data from an area of memory in a specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref IMAGE_DOS_HEADER lpBuffer, int dwSize, IntPtr lpNumberOfBytesRead);

        /// <summary>
        /// Reads data from an area of memory in a specified process (con out parameter)
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        /// <summary>
        /// Reads data from an area of memory in a specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref IMAGE_NT_HEADERS lpBuffer, int dwSize, IntPtr lpNumberOfBytesRead);

        /// <summary>
        /// Reads data from an area of memory in a specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref IMAGE_EXPORT_DIRECTORY lpBuffer, int dwSize, IntPtr lpNumberOfBytesRead);

        /// <summary>
        /// Reads data from an area of memory in a specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref uint lpBuffer, int dwSize, IntPtr lpNumberOfBytesRead);

        /// <summary>
        /// Reads data from an area of memory in a specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref ushort lpBuffer, int dwSize, IntPtr lpNumberOfBytesRead);

        /// <summary>
        /// Reads data from an area of memory in a specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, IntPtr lpNumberOfBytesRead);

        /// <summary>
        /// Reads data from an area of memory in a specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int nSize, IntPtr lpNumberOfBytesRead);

        /// <summary>
        /// Reserves, commits, or changes the state of a region of memory within the virtual address space of a specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint flAllocationType,
            uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint GetModuleFileName(IntPtr hModule, StringBuilder lpFilename, int nSize);

        /// <summary>
        /// Writes data to an area of memory in a specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte* lpBuffer,
            UIntPtr nSize,
            out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteProcessMemory(
    IntPtr hProcess,
    IntPtr lpBaseAddress,
    byte[] lpBuffer,
    int nSize,
    out IntPtr lpNumberOfBytesWritten);

        /// <summary>
        /// Writes data to an area of memory in a specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteProcessMemory(
          IntPtr hProcess,
          IntPtr lpBaseAddress,
          nint* lpBuffer,
          UIntPtr nSize,
          out IntPtr lpNumberOfBytesWritten);

        /// <summary>
        /// Writes data to an area of memory in a specified process
        /// </summary>
        [DllImport("kernel32", SetLastError = true)]
        internal static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            void* lpBuffer,
            int nSize,
            out int lpNumberOfBytesWritten);

        /// <summary>
        /// Determines whether the specified process is running under WOW64
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

        /// <summary>
        /// Flushes the instruction cache for the specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, UIntPtr dwSize);

        /// <summary>
        /// Changes the protection on a region of committed pages in the virtual address space of a specified process
        /// </summary>
        [DllImport("kernel32", SetLastError = true)]
        internal static extern bool VirtualProtectEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flNewProtect,
            out uint lpflOldProtect);

        /// <summary>
        /// Changes the protection on a region of committed pages in the virtual address space of the calling process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        /// <summary>
        /// Provides information about a range of pages in the virtual address space of a specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int VirtualQueryEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION lpBuffer,
            uint dwLength);

        /// <summary>
        /// Releases, decommits, or releases and decommits a region of memory within the virtual address space of a specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType);

        /// <summary>
        /// Retrieves the termination status of the specified thread
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        /// <summary>
        /// Retrieves the termination status of the specified process
        /// </summary>
        [DllImport("kernel32", SetLastError = true)]
        internal static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        /// <summary>
        /// Retrieves the calling thread's last-error code value
        /// </summary>
        [DllImport("kernel32", SetLastError = true)]
        internal static extern uint GetLastError();



        /// <summary>
        /// Creates a thread that runs in the virtual address space of the calling process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr CreateThread(
        IntPtr lpThreadAttributes,
        uint dwStackSize,
        IntPtr lpStartAddress,
        IntPtr lpParameter,
        uint dwCreationFlags,
        out uint lpThreadId);

        /// <summary>
        /// Creates a thread that runs in the virtual address space of another process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            out uint lpThreadId);

        /// <summary>
        /// Disables the DLL_THREAD_ATTACH and DLL_THREAD_DETACH notifications for the specified DLL
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool DisableThreadLibraryCalls(IntPtr hModule);

        /// <summary>
        /// Detaches the calling process from its console
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern bool FreeConsole();

        /// <summary>
        /// Attaches the calling process to the console of the specified process
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool AttachConsole(int dwProcessId);

        /// <summary>
        /// Allocates a new console for the calling process
        /// </summary>
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AllocConsole();

        /// <summary>
        /// Suspends the execution of the current thread until the time-out interval elapses
        /// </summary>
        [DllImport("kernel32")]
        internal static extern void Sleep(uint dwMilliseconds);

        /// <summary>
        /// Waits until the specified object is in the signaled state or the time-out interval elapses
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint WaitForSingleObject(
                IntPtr hHandle,
                uint dwMilliseconds
            );

        /// <summary>
        /// Takes a snapshot of the specified processes and modules
        /// </summary>
        [DllImport("kernel32")]
        internal static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

        /// <summary>
        /// Retrieves information about the first module associated with a process
        /// </summary>
        [DllImport("kernel32")]
        internal static extern bool Module32First(IntPtr hSnapshot, ref MODULEENTRY32 lpme);
        
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int PeekMessage(
           out MSG lpMsg,
           IntPtr hWnd,
           uint wMsgFilterMin,
           uint wMsgFilterMax,
           uint wRemoveMsg);

        /// <summary>
        /// Retrieves information about the next module associated with a process
        /// </summary>
        [DllImport("kernel32")]
        internal static extern bool Module32Next(IntPtr hSnapshot, ref MODULEENTRY32 lpme);

        /// <summary>
        /// Retrieves information about the first process encountered in a system snapshot
        /// </summary>
        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        /// <summary>
        /// Retrieves information about the next process recorded in a system snapshot
        /// </summary>
        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        /// <summary>
        /// Retrieves information about the first module associated with a process (Unicode version)
        /// </summary>
        [DllImport("kernel32", EntryPoint = "Module32FirstW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Module32FirstW(
            IntPtr hSnapshot,
            ref MODULEENTRY32W lpme);

        /// <summary>
        /// Retrieves information about the next module associated with a process (Unicode version)
        /// </summary>
        [DllImport("kernel32", EntryPoint = "Module32NextW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Module32NextW(
            IntPtr hSnapshot,
            ref MODULEENTRY32W lpme);

        /// <summary>
        /// Retrieves information about the first process encountered in a system snapshot (Unicode version)
        /// </summary>
        [DllImport("kernel32", EntryPoint = "Process32FirstW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Process32FirstW(
            IntPtr hSnapshot,
            ref PROCESSENTRY32W lppe);

        /// <summary>
        /// Retrieves information about the next process recorded in a system snapshot (Unicode version)
        /// </summary>
        [DllImport("kernel32", EntryPoint = "Process32NextW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Process32NextW(
            IntPtr hSnapshot,
            ref PROCESSENTRY32W lppe);

        /// <summary>
        /// Frees the loaded dynamic-link library (DLL) module
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// Decrements the reference count of a loaded dynamic-link library (DLL) and terminates the calling thread
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern void FreeLibraryAndExitThread(IntPtr hModule, uint dwExitCode);

        // ================= USER32 APIs =================

        /// <summary>
        /// Retrieves a handle to the top-level window whose class name and window name match the specified strings
        /// </summary>
        [DllImport("user32", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindowW(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Retrieves a handle to a window whose class name and window name match the specified strings
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// Retrieves the identifier of the thread that created the specified window
        /// </summary>
        [DllImport("user32", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        /// <summary>
        /// Registers a window class for subsequent use in calls to the CreateWindow or CreateWindowEx function
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll")]
        internal static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        internal static extern short GetKeyState(int vKey);

        /// <summary>
        /// Creates an overlapped, pop-up, or child window with an extended window style
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr CreateWindowEx(
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

        /// <summary>
        /// Retrieves information about the specified window
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>
        /// Changes an attribute of the specified window
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        /// <summary>
        /// Sets the opacity and transparency color key of a layered window
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetLayeredWindowAttributes(
            IntPtr hwnd,
            uint crKey,
            byte bAlpha,
            uint dwFlags);

        /// <summary>
        /// Sets the specified window's show state
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        /// <summary>
        /// Updates the client area of the specified window
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UpdateWindow(IntPtr hWnd);

        /// <summary>
        /// Retrieves the dimensions of the bounding rectangle of the specified window
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// Retrieves the show state and the restored, minimized, and maximized positions of the specified window
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        /// <summary>
        /// Changes the size, position, and Z order of a child, pop-up, or top-level window
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        /// <summary>
        /// Changes the position and dimensions of the specified window
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(
            IntPtr hWnd,
            int X,
            int Y,
            int nWidth,
            int nHeight,
            bool bRepaint);

        /// <summary>
        /// Calls the default window procedure to provide default processing for any window messages
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr DefWindowProc(
            IntPtr hWnd,
            uint uMsg,
            IntPtr wParam,
            IntPtr lParam);

        /// <summary>
        /// Indicates to the system that a thread has made a request to terminate (quit)
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool PostQuitMessage(int nExitCode);

        /// <summary>
        /// Enumerates all top-level windows on the screen
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        /// <summary>
        /// Determines the visibility state of the specified window
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// Retrieves a handle to the specified window's parent or owner
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern IntPtr GetParent(IntPtr hWnd);

        /// <summary>
        /// Copies the text of the specified window's title bar into a buffer
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// Retrieves a handle to a window that has the specified relationship to the specified window
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GetCurrentModule();

        /// <summary>
        /// Obtiene un handle al módulo especificado
        /// </summary>
        /// <param name="lpModuleName">Nombre del módulo (null para el módulo actual)</param>
        /// <returns>Handle del módulo o IntPtr.Zero si falla</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>
        /// Versión Unicode explícita
        /// </summary>
        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr GetModuleHandleW(string lpModuleName);

        /// <summary>
        /// Versión ANSI explícita
        /// </summary>
        [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleA", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern IntPtr GetModuleHandleA(string lpModuleName);

        internal delegate void TimerProc(IntPtr hwnd, uint uMsg, IntPtr idEvent, uint dwTime);

        // Timer functions
        [DllImport("user32.dll")]
        internal static extern IntPtr SetTimer(IntPtr hWnd, IntPtr nIDEvent, uint uElapse, TimerProc lpTimerFunc);

        [DllImport("user32.dll")]
        internal static extern bool KillTimer(IntPtr hWnd, IntPtr uIDEvent);

        /// <summary>
        /// Versión extendida que obtiene handle incluso si el módulo no está cargado
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool GetModuleHandleEx(uint dwFlags, string lpModuleName, out IntPtr phModule);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        internal static extern bool UnhookWindowsHookEx(int idHook);

        [DllImport("user32.dll")]
        internal static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        internal static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

        // Hook delegate
        internal delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        internal static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

        [DllImport("user32.dll")]
        internal static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        internal static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        internal static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        internal static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);
        // ================= PSAPI APIs =================

        /// <summary>
        /// Retrieves a handle for each module in the specified process
        /// </summary>
        [DllImport("psapi", SetLastError = true)]
        internal static extern bool EnumProcessModulesEx(
            IntPtr hProcess,
            IntPtr[] lphModule,
            uint cb,
            out uint lpcbNeeded,
            int dwFilterFlag);

        /// <summary>
        /// Retrieves the fully qualified path for the file containing the specified module
        /// </summary>
        [DllImport("psapi", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint GetModuleFileNameExW(
            IntPtr hProcess,
            IntPtr hModule,
            StringBuilder lpFilename,
            int nSize);

        // ================= OPENGL32 APIs =================

        /// <summary>
        /// Returns the address of an OpenGL extension function
        /// </summary>
        [DllImport("opengl32.dll", EntryPoint = "wglGetProcAddress")]
        internal static extern IntPtr wglGetProcAddress(string procName);

        // ================= STRUCTS =================

        [StructLayout(LayoutKind.Sequential)]
        internal struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        // ================= DELEGATES =================

        internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // ================= HELPER METHODS =================

        internal static IntPtr OpenProcessWithFullAccess(uint processId)
        {
            return OpenProcess(PROCESS_ALL_ACCESS, false, processId);
        }

        internal static bool IsProcessRunning(IntPtr hProcess)
        {
            return GetExitCodeProcess(hProcess, out uint exitCode) && exitCode == STILL_ACTIVE;
        }

        internal static void SetWindowTransparency(IntPtr hWnd, uint colorKey)
        {
            SetWindowLong(hWnd, GWL_EXSTYLE, GetWindowLong(hWnd, GWL_EXSTYLE) | WS_EX_LAYERED);
            SetLayeredWindowAttributes(hWnd, colorKey, 0, LWA_COLORKEY);
        }

        internal static void MakeClickThrough(IntPtr hWnd)
        {
            int style = GetWindowLong(hWnd, GWL_EXSTYLE);
            SetWindowLong(hWnd, GWL_EXSTYLE, style | WS_EX_TRANSPARENT | WS_EX_LAYERED);
        }

        internal static uint FindProcessId(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return 0;

            // Configure console for UTF-8
            SetConsoleOutputCP(65001);

            // Ensure .exe extension
            if (!processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                processName += ".exe";

            IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
            if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1))
            {
                Debug.WriteLine($"Error creating snapshot: {GetLastError()}");
                return 0;
            }

            try
            {
                PROCESSENTRY32W processEntry = new PROCESSENTRY32W();
                processEntry.dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32W>();

                if (!Process32FirstW(snapshot, ref processEntry))
                {
                    Console.WriteLine($"Error in Process32FirstW: {GetLastError()}");
                    return 0;
                }

                do
                {
                    if (!string.IsNullOrEmpty(processEntry.szExeFile) &&
                        string.Equals(processEntry.szExeFile, processName, StringComparison.OrdinalIgnoreCase))
                    {
                        return processEntry.th32ProcessID;
                    }
                } while (Process32NextW(snapshot, ref processEntry));

                Console.WriteLine($"Process '{processName}' not found");
                return 0;
            }
            finally
            {
                CloseHandle(snapshot);
            }
        }

        internal static IntPtr GetModuleBaseEx(uint processId, string moduleName = null)
        {
            try
            {
                IntPtr hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, processId);
                if (hSnapshot == INVALID_HANDLE_VALUE)
                    return IntPtr.Zero;

                MODULEENTRY32 moduleEntry = new MODULEENTRY32();
                moduleEntry.dwSize = (uint)Marshal.SizeOf(moduleEntry);

                IntPtr mainModuleBase = IntPtr.Zero;
                bool found = false;

                if (Module32First(hSnapshot, ref moduleEntry))
                {
                    do
                    {
                        // If no module name specified, return first module (main executable)
                        if (string.IsNullOrEmpty(moduleName))
                        {
                            mainModuleBase = moduleEntry.modBaseAddr;
                            found = true;
                            break;
                        }
                        else if (string.Equals(moduleEntry.szModule, moduleName, StringComparison.OrdinalIgnoreCase))
                        {
                            mainModuleBase = moduleEntry.modBaseAddr;
                            found = true;
                            break;
                        }
                    } while (Module32Next(hSnapshot, ref moduleEntry));
                }

                CloseHandle(hSnapshot);
                return found ? mainModuleBase : IntPtr.Zero;
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        internal static IntPtr GetProcAddressEx(IntPtr hProcess, IntPtr hModule, string procName)
        {
            const int IMAGE_DOS_SIGNATURE = 0x5A4D;      // "MZ"
            const int IMAGE_NT_SIGNATURE = 0x00004550;   // "PE\0\0"
            const int IMAGE_DIRECTORY_ENTRY_EXPORT = 0;  // Export Directory

            try
            {
                // Leer DOS header
                IMAGE_DOS_HEADER dosHeader;
                if (!ReadProcessMemory(hProcess, hModule, out dosHeader))
                    return IntPtr.Zero;

                if (dosHeader.e_magic != IMAGE_DOS_SIGNATURE)
                    return IntPtr.Zero;

                // Leer NT headers
                IMAGE_NT_HEADERS ntHeaders;
                IntPtr ntHeadersPtr = hModule + dosHeader.e_lfanew;
                if (!ReadProcessMemory(hProcess, ntHeadersPtr, out ntHeaders))
                    return IntPtr.Zero;

                if (ntHeaders.Signature != IMAGE_NT_SIGNATURE)
                    return IntPtr.Zero;

                // Obtener directorio de exportación
                var exportDir = ntHeaders.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT];
                if (exportDir.VirtualAddress == 0 || exportDir.Size == 0)
                    return IntPtr.Zero;

                // Leer tabla de exportación
                IMAGE_EXPORT_DIRECTORY exportTable;
                IntPtr exportTablePtr = hModule + (int)exportDir.VirtualAddress;
                if (!ReadProcessMemory(hProcess, exportTablePtr, out exportTable))
                    return IntPtr.Zero;

                // Leer arrays de exportación
                uint[] addressOfFunctions = new uint[exportTable.NumberOfFunctions];
                uint[] addressOfNames = new uint[exportTable.NumberOfNames];
                ushort[] addressOfNameOrdinals = new ushort[exportTable.NumberOfNames];

                IntPtr functionsPtr = hModule + (int)exportTable.AddressOfFunctions;
                IntPtr namesPtr = hModule + (int)exportTable.AddressOfNames;
                IntPtr ordinalsPtr = hModule + (int)exportTable.AddressOfNameOrdinals;

                if (!ReadProcessMemoryArray(hProcess, functionsPtr, addressOfFunctions))
                    return IntPtr.Zero;

                if (!ReadProcessMemoryArray(hProcess, namesPtr, addressOfNames))
                    return IntPtr.Zero;

                if (!ReadProcessMemoryArray(hProcess, ordinalsPtr, addressOfNameOrdinals))
                    return IntPtr.Zero;

                // Buscar la función por nombre
                for (int i = 0; i < exportTable.NumberOfNames; i++)
                {
                    string currentName;
                    IntPtr namePtr = hModule + (int)addressOfNames[i];

                    if (!ReadProcessMemoryString(hProcess, namePtr, out currentName))
                        continue;

                    if (string.Equals(currentName, procName, StringComparison.Ordinal))
                    {
                        ushort ordinal = addressOfNameOrdinals[i];
                        if (ordinal >= exportTable.NumberOfFunctions)
                            continue;

                        uint functionRva = addressOfFunctions[ordinal];
                        return hModule + (int)functionRva;
                    }
                }

                // Buscar por ordinal (si procName es un número)
                if (ushort.TryParse(procName, out ushort ordinalValue))
                {
                    if (ordinalValue >= exportTable.Base &&
                        ordinalValue < exportTable.Base + exportTable.NumberOfFunctions)
                    {
                        uint functionRva = addressOfFunctions[ordinalValue - exportTable.Base];
                        return hModule + (int)functionRva;
                    }
                }

                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetProcAddressEx: {ex.Message}");
                return IntPtr.Zero;
            }
        }

        // Métodos auxiliares para lectura de memoria
        private static bool ReadProcessMemory<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicConstructors |
    DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(IntPtr hProcess, IntPtr lpBaseAddress, out T structure) where T : struct
        {
            structure = default;
            int size = Marshal.SizeOf<T>();
            IntPtr buffer = Marshal.AllocHGlobal(size);

            try
            {
                if (!ReadProcessMemory(hProcess, lpBaseAddress, buffer, size, IntPtr.Zero))
                    return false;

                structure = Marshal.PtrToStructure<T>(buffer);
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static bool ReadProcessMemoryArray<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicConstructors |
    DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(IntPtr hProcess, IntPtr lpBaseAddress, T[] array) where T : struct
        {
            int elementSize = Marshal.SizeOf<T>();
            int totalSize = array.Length * elementSize;
            IntPtr buffer = Marshal.AllocHGlobal(totalSize);

            try
            {
                if (!ReadProcessMemory(hProcess, lpBaseAddress, buffer, totalSize, IntPtr.Zero))
                    return false;

                for (int i = 0; i < array.Length; i++)
                {
                    IntPtr elementPtr = buffer + (i * elementSize);
                    array[i] = Marshal.PtrToStructure<T>(elementPtr);
                }
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static bool ReadProcessMemoryString(IntPtr hProcess, IntPtr lpBaseAddress, out string value)
        {
            value = null;
            const int bufferSize = 256;
            byte[] buffer = new byte[bufferSize];

            if (!ReadProcessMemory(hProcess, lpBaseAddress, buffer, bufferSize, IntPtr.Zero))
                return false;

            int length = Array.IndexOf(buffer, (byte)0);
            if (length <= 0) return false;

            value = Encoding.ASCII.GetString(buffer, 0, length);
            return true;
        }

        /// <summary>
        /// Lista todos los procesos en ejecución (útil para debugging)
        /// </summary>
        /// <returns>Lista de nombres de procesos</returns>
        internal static string[] ListRunningProcesses()
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

        internal static IntPtr FindGameWindow(uint processId)
        {
            // Opción 1: Buscar por nombre de ventana si lo conoces
            IntPtr gameWindow = FindWindow(null, "AssaultCube"); // Cambia por el título real
            if (gameWindow != IntPtr.Zero)
            {
                uint windowProcessId;
                GetWindowThreadProcessId(gameWindow, out windowProcessId);
                if (windowProcessId == processId)
                {
                    return gameWindow;
                }
            }

            // Opción 2: Enumerar todas las ventanas del proceso
            gameWindow = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                uint windowProcessId;
                GetWindowThreadProcessId(hWnd, out windowProcessId);

                if (windowProcessId == processId)
                {
                    // Verificar si es una ventana visible y principal
                    if (IsWindowVisible(hWnd) && GetParent(hWnd) == IntPtr.Zero)
                    {
                        // Obtener el texto de la ventana para verificar
                        var windowText = new StringBuilder(256);
                        GetWindowText(hWnd, windowText, windowText.Capacity);

                        // Si la ventana tiene título o es la ventana principal
                        if (windowText.Length > 0 || IsMainWindow(hWnd))
                        {
                            gameWindow = hWnd;
                            return false; // Detener enumeración
                        }
                    }
                }
                return true; // Continuar enumeración
            }, IntPtr.Zero);

            return gameWindow;
        }

        private static bool IsMainWindow(IntPtr hWnd)
        {
            return GetWindow(hWnd, WinInterop.GW_OWNER) == IntPtr.Zero &&
                   IsWindowVisible(hWnd);
        }

        /// <summary>
        /// Lista todos los módulos de un proceso (útil para debugging)
        /// </summary>
        /// <param name="processId">ID del proceso</param>
        /// <returns>Lista de nombres de módulos</returns>
        internal static string[] ListProcessModules(uint processId)
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

        internal static IntPtr CalculatePointer(IntPtr baseAddress, params int[] offsets)
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


        internal static void ExecuteRemoteFunction(IntPtr hProcess, IntPtr funcAddress, int parameter)
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