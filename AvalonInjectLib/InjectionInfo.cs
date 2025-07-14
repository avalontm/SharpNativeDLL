using System;
using System.Runtime.InteropServices;

namespace AvalonInjectLib
{
    public static class InjectionInfo
    {
        // Importar APIs de Windows necesarias
        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentProcessId();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern uint GetModuleFileName(IntPtr hModule, char[] lpFilename, uint nSize);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Información del proceso donde se inyectó la DLL
        public static uint CurrentProcessId => GetCurrentProcessId();
        public static IntPtr CurrentProcessHandle => GetCurrentProcess();

        /// <summary>
        /// Obtiene información básica del proceso donde se inyectó la DLL
        /// </summary>
        /// <returns>Información del proceso actual</returns>
        public static ProcessInfo GetCurrentProcessInfo()
        {
            uint processId = GetCurrentProcessId();
            IntPtr processHandle = GetCurrentProcess();
            string processName = GetCurrentProcessName();

            return new ProcessInfo
            {
                ProcessId = processId,
                ProcessHandle = processHandle,
                ProcessName = processName
            };
        }

        /// <summary>
        /// Obtiene el nombre del proceso actual
        /// </summary>
        /// <returns>Nombre del proceso</returns>
        private static string GetCurrentProcessName()
        {
            try
            {
                char[] buffer = new char[260]; // MAX_PATH
                uint length = GetModuleFileName(IntPtr.Zero, buffer, (uint)buffer.Length);

                if (length > 0)
                {
                    string fullPath = new string(buffer, 0, (int)length);
                    return System.IO.Path.GetFileName(fullPath);
                }
            }
            catch
            {
                // Si falla, retornar un valor por defecto
            }

            return "Unknown";
        }

        /// <summary>
        /// Información del proceso
        /// </summary>
        public struct ProcessInfo
        {
            public uint ProcessId { get; set; }
            public IntPtr ProcessHandle { get; set; }
            public string ProcessName { get; set; }

            public override string ToString()
            {
                return $"Process: {ProcessName} | PID: 0x{ProcessId:X8} | Handle: 0x{ProcessHandle:X8}";
            }
        }
    }
}