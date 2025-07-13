using System;

namespace AvalonInjectLib
{
    public static class ProcessManager
    {
        public static IntPtr Open(uint processId)
        {
            return WinInterop.OpenProcess(WinInterop.PROCESS_ALL_ACCESS, false, processId);
        }

        public static ProcessEntry Create(string processName)
        {
            ProcessEntry moduleProcess = null;

            var processId = Find(processName);
            var hProcess = Open(processId);
            var moduleBase = Module(processId, "ac_client.exe");

            if (processId > 0 && hProcess != IntPtr.Zero && moduleBase != IntPtr.Zero)
            {
               
                moduleProcess = new ProcessEntry(processId, hProcess, moduleBase);
            }

            return moduleProcess;
        }

        public static uint Find(string processName)
        {
            return WinInterop.FindProcessId(processName);
        }

        public static IntPtr Module(uint processId, string moduleName)
        {
            return WinInterop.GetModuleBaseEx(processId, moduleName);
        }

        public static bool IsOpen(IntPtr hProcess)
        {
            return WinInterop.IsProcessRunning(hProcess);
        }

        public static IntPtr CreateThread(ThreadStart threadHandle)
        {
            return WinInterop.CreateThread(
                nint.Zero,
                0,
                threadHandle,
                nint.Zero,
                0,
                out _);
        }

        public static bool CloseThread(IntPtr threadHandle)
        {
            return WinInterop.CloseHandle(threadHandle);
        }
    }
}
