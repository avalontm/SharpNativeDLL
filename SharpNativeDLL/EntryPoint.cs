using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SharpNativeDLL
{
    public class EntryPoint
    {
        private const uint DLL_PROCESS_DETACH = 0,
                           DLL_PROCESS_ATTACH = 1,
                           DLL_THREAD_ATTACH = 2,
                           DLL_THREAD_DETACH = 3;

        const int PROCESS_WM_READ = 0x0010;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_OPERATION = 0x0008;

        [UnmanagedCallersOnly(EntryPoint = "DllMain", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static bool DllMain(IntPtr hModule, uint nReason, IntPtr lpReserved)
        {
            switch (nReason)
            {
                case DLL_PROCESS_ATTACH:
                    onMain();
                    break;
                case DLL_PROCESS_DETACH:
                    break;
                case DLL_THREAD_ATTACH:
                    break;
                case DLL_THREAD_DETACH:
                default:

                    break;
            }
            return true;
        }


        public static void onMain()
        {
            int processID = WindowAPI.GetCurrentProcessId();
            IntPtr processHandle = WindowAPI.OpenProcess(PROCESS_WM_READ, false, processID);

            if (!WindowAPI.AttachConsole(processID))
            {
                // A console was not allocated, so we need to make one.
                if (!WindowAPI.AllocConsole())
                {
                    WindowAPI.MessageBox(0, "No se pudo asignar una consola.", "Error", 0);
                }
            }

            Console.WriteLine($"[ProcessHandle] {processHandle}");

            int bytesRead = 0;
            byte[] buffer = new byte[4];

            WindowAPI.ReadProcessMemory((int)processHandle, 0x3CF1003C, buffer, buffer.Length, ref bytesRead);

            Console.WriteLine($"{BitConverter.ToInt32(buffer, 0)} ({bytesRead} bytes)");

        }


    }
}