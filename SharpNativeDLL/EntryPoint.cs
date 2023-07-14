using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SharpNativeDLL
{
    public class EntryPoint
    {
        const uint DLL_PROCESS_DETACH = 0,
                   DLL_PROCESS_ATTACH = 1,
                   DLL_THREAD_ATTACH = 2,
                   DLL_THREAD_DETACH = 3;

        const int PROCESS_WM_READ = 0x0010;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_ALL_ACCESS = 0xFFF;

        const uint SW_HIDE = 0;
        const uint SW_SHOWNORMAL = 1;
        const uint SW_SHOWMINIMIZED = 2;
        const uint SW_SHOWMAXIMIZED = 3;

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
            int CurrentProcessId = WindowAPI.GetCurrentProcessId();

            if (!WindowAPI.AttachConsole(CurrentProcessId))
            {
                if (!WindowAPI.AllocConsole())
                {
                    WindowAPI.MessageBox(0, "No se pudo asignar una consola.", "Error", 0);
                }
            }

            Console.WriteLine($"[CurrentProcessId] {string.Format("0x{0:X}", CurrentProcessId)}");

            IntPtr mainHandle = WindowAPI.OpenProcess(PROCESS_ALL_ACCESS, false, CurrentProcessId);

            Console.WriteLine($"[mainHandle] {string.Format("0x{0:X}", mainHandle)}");

            IntPtr notepadTextbox = WindowAPI.FindWindowEx(mainHandle, IntPtr.Zero, "Edit", "");

            Console.WriteLine($"[notepadTextbox] {string.Format("0x{0:X}", notepadTextbox)}");

            InputManager.SendString(notepadTextbox, "Hola mundo!");

            WindowAPI.ShowWindow(CurrentProcessId, SW_SHOWMAXIMIZED);
        }
    }
}