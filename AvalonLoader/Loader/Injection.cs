using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AvalonLoader.Loader
{
    public class Injection
    {
        #region Native Imports
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint processAccess,
            bool bInheritHandle,
            int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flAllocationType,
            uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            uint nSize,
            out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(
            IntPtr hModule,
            string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(
            string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(
            IntPtr hHandle,
            uint dwMilliseconds);

        private const uint PROCESS_CREATE_THREAD = 0x0002;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_VM_WRITE = 0x0020;
        private const uint PROCESS_VM_READ = 0x0010;
        private const uint MEM_COMMIT = 0x00001000;
        private const uint MEM_RESERVE = 0x00002000;
        private const uint PAGE_READWRITE = 0x04;
        #endregion

        public struct InjectionParameters
        {
            public string RootPath;
            public string RootScripts;
        }

        /// <summary>
        /// Inyecta la DLL especificada en el proceso objetivo con los parámetros de configuración
        /// </summary>
        public static bool Inject(int processId, string dllPath, InjectionParameters parameters)
        {
            if (!File.Exists(dllPath))
                throw new FileNotFoundException("La DLL especificada no existe", dllPath);

            IntPtr hProcess = OpenProcess(
                PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION |
                PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ,
                false, processId);

            if (hProcess == IntPtr.Zero)
                throw new Exception($"No se pudo abrir el proceso (Error: {Marshal.GetLastWin32Error()})");

            try
            {
                // Serializar parámetros a JSON
                string jsonParams = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                byte[] paramsBytes = Encoding.UTF8.GetBytes(jsonParams + "\0");

                // 1. Allocate memory for parameters
                IntPtr paramsAddr = VirtualAllocEx(
                    hProcess,
                    IntPtr.Zero,
                    (uint)paramsBytes.Length,
                    MEM_COMMIT | MEM_RESERVE,
                    PAGE_READWRITE);

                if (paramsAddr == IntPtr.Zero)
                    throw new Exception($"Error al asignar memoria para parámetros (Error: {Marshal.GetLastWin32Error()})");

                // 2. Write parameters
                if (!WriteProcessMemory(hProcess, paramsAddr, paramsBytes, (uint)paramsBytes.Length, out _))
                    throw new Exception($"Error al escribir parámetros (Error: {Marshal.GetLastWin32Error()})");

                // 3. Allocate memory for DLL path
                byte[] dllPathBytes = Encoding.UTF8.GetBytes(dllPath + "\0");
                IntPtr dllPathAddr = VirtualAllocEx(
                    hProcess,
                    IntPtr.Zero,
                    (uint)dllPathBytes.Length,
                    MEM_COMMIT | MEM_RESERVE,
                    PAGE_READWRITE);

                if (dllPathAddr == IntPtr.Zero)
                    throw new Exception($"Error al asignar memoria para la ruta DLL (Error: {Marshal.GetLastWin32Error()})");

                // 4. Write DLL path
                if (!WriteProcessMemory(hProcess, dllPathAddr, dllPathBytes, (uint)dllPathBytes.Length, out _))
                    throw new Exception($"Error al escribir ruta DLL (Error: {Marshal.GetLastWin32Error()})");

                // 5. Get LoadLibraryA address
                IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
                if (loadLibraryAddr == IntPtr.Zero)
                    throw new Exception("No se pudo obtener la dirección de LoadLibraryA");

                // 6. Create remote thread to load our DLL
                IntPtr hThread = CreateRemoteThread(
                    hProcess,
                    IntPtr.Zero,
                    0,
                    loadLibraryAddr,
                    dllPathAddr,
                    0,
                    IntPtr.Zero);

                if (hThread == IntPtr.Zero)
                    throw new Exception($"Error al crear thread remoto (Error: {Marshal.GetLastWin32Error()})");

                // Wait for injection to complete
                WaitForSingleObject(hThread, 5000);

                // 7. Create another thread to pass parameters
                IntPtr initFuncAddr = GetRemoteProcAddress(hProcess, dllPath, "InitializeAvalon");
                if (initFuncAddr != IntPtr.Zero)
                {
                    hThread = CreateRemoteThread(
                        hProcess,
                        IntPtr.Zero,
                        0,
                        initFuncAddr,
                        paramsAddr,
                        0,
                        IntPtr.Zero);

                    if (hThread != IntPtr.Zero)
                    {
                        WaitForSingleObject(hThread, 5000);
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                Marshal.Release(hProcess);
            }
        }

        private static IntPtr GetRemoteProcAddress(IntPtr hProcess, string dllPath, string functionName)
        {
            // This is a simplified version - in a real implementation you would need to:
            // 1. Read the remote DLL's headers
            // 2. Locate the export table
            // 3. Find the function address
            // For simplicity, we assume the function is at the same offset as in our process

            IntPtr localModule = LoadLibrary(dllPath);
            if (localModule == IntPtr.Zero)
                return IntPtr.Zero;

            try
            {
                IntPtr funcAddr = GetProcAddress(localModule, functionName);
                if (funcAddr == IntPtr.Zero)
                    return IntPtr.Zero;

                // Calculate the offset from the module base
                long offset = funcAddr.ToInt64() - localModule.ToInt64();

                // Find the remote module base (would need to enumerate modules in remote process)
                // This is simplified - in real code you'd need to find the actual base address
                return new IntPtr(localModule.ToInt64() + offset);
            }
            finally
            {
                FreeLibrary(localModule);
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);
    }
}
