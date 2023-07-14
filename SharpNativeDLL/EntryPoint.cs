using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpNativeDLL
{
    public class EntryPoint
    {
        private const uint DLL_PROCESS_DETACH = 0,
                           DLL_PROCESS_ATTACH = 1,
                           DLL_THREAD_ATTACH = 2,
                           DLL_THREAD_DETACH = 3;

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
            int ATTACH_PARENT_PROCESS = WindowAPI.GetCurrentProcessId();

            if (!WindowAPI.AttachConsole(ATTACH_PARENT_PROCESS))
            {
                // A console was not allocated, so we need to make one.
                if (!WindowAPI.AllocConsole())
                {
                    WindowAPI.MessageBox(0, "No se pudo asignar una consola.", "Error", 0);
                }
            }
           
            Console.WriteLine("presione una tecla...");
            Console.ReadKey(true);
        }
    }
}