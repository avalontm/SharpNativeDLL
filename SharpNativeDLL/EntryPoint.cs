using SharpNativeDLL.Helpers;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static SharpNativeDLL.Helpers.OpenGLInterop;
using static SharpNativeDLL.Helpers.Structs;

namespace SharpNativeDLL
{
    public class EntryPoint
    {
        const uint DLL_PROCESS_DETACH = 0,
                   DLL_PROCESS_ATTACH = 1,
                   DLL_THREAD_ATTACH = 2,
                   DLL_THREAD_DETACH = 3;


        static IntPtr layWnd = IntPtr.Zero;
        static IntPtr mainHandle = IntPtr.Zero;

        [UnmanagedCallersOnly(EntryPoint = "DllMain", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static bool DllMain(IntPtr hModule, uint nReason, IntPtr lpReserved)
        {
            uint threadId;

            switch (nReason)
            {
                case DLL_PROCESS_ATTACH:
                    WinInterop.CreateThread(IntPtr.Zero, 0, new ThreadStart(onMain), IntPtr.Zero, 0, out threadId);
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


        static async void onMain()
        {
            mainHandle = (int)WinInterop.FindWindow("notepad", null);

            if (mainHandle == 0)
            {
                WinInterop.MessageBox(0, "No se encontro el proceso.", "Error", 0);
                return;
            }

            if (!WinInterop.AttachConsole(mainHandle.ToInt32()))
            {
                if (!WinInterop.AllocConsole())
                {
                    WinInterop.MessageBox(0, "No se pudo asignar una consola.", "Error", 0);
                }
            }

            Console.WriteLine($"[mainHandle] {mainHandle.ToHex()}");

            IntPtr notepadTextbox = WinInterop.FindWindowEx(mainHandle, IntPtr.Zero, "Edit", "");

            Console.WriteLine($"[notepadTextbox] {notepadTextbox.ToHex()}");

            InputManager.SendString(notepadTextbox, "Hola mundo!");

            //OVERLAY
            layWnd = OverlayManager.CreateWindow(mainHandle);

            //OPENGL
            OpenGLManager.InitializeOpenGL(layWnd);

            bool running = true;

            //LOOP
            while (running)
            {
                MSG msg;
                while (WinInterop.PeekMessage(out msg, IntPtr.Zero, 0, 0, Const.PM_REMOVE))
                {
                    if (msg.message == Const.WM_QUIT)
                    {
                        running = false;
                    }
                    else
                    {
                        WinInterop.TranslateMessage(ref msg);
                        WinInterop.DispatchMessage(ref msg);
                    }

                    OverlayManager.OnUpdate(mainHandle);
                }

                await Task.Delay(1);
            }


        }

    }
}