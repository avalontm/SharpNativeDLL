using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SharpNativeDLL.Helpers;
using static SharpNativeDLL.Helpers.Structs;

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

        const int SW_HIDE = 0;
        const int SW_SHOWNORMAL = 1;
        const int SW_SHOWMINIMIZED = 2;
        const int SW_SHOWMAXIMIZED = 3;
        const int SW_SHOW = 5;

        const int CS_HREDRAW = 0x0001;
        const int CS_VREDRAW = 0x0002;
        const int CS_COMBINED = 0x0003;
        const int WS_EX_TOPMOST = 0x00000008;
        const int WS_EX_LAYERED = 0x080000;
        const int WS_POPUP = 0x08000000;
        const int LWA_COLORKEY = 0x00000001;
        const int IDC_CROSS = 32515;
        const int WS_OVERLAPPEDWINDOW = 0x00CF0000;
        const int WS_VISIBLE = 0x10000000;
        const int IDI_APPLICATION = 0x7F00;
        const int WS_EX_CLIENTEDGE = 0x00000200;
        const int CW_USEDEFAULT = unchecked((int)0x80000000);
        const int IDC_ARROW = 32512;
        const int COLOR_WINDOW = 5;
        const uint WM_DESTROY = 0x0002;

        static IntPtr layWnd; 
        static RECT topWndRect;
        static int X = 0;
        static int Y = 0;
        static int Width = 0; 
        static int Height = 0; 

        [UnmanagedCallersOnly(EntryPoint = "DllMain", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static bool DllMain(IntPtr hModule, uint nReason, IntPtr lpReserved)
        {
            uint threadId;

            switch (nReason)
            {
                case DLL_PROCESS_ATTACH:
                        WindowAPI.CreateThread(IntPtr.Zero, 0, new ThreadStart(onMain), IntPtr.Zero, 0, out threadId);
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
            int mainHandle = (int)WindowAPI.FindWindow("notepad", null);

            if (mainHandle == 0)
            {
                WindowAPI.MessageBox(0, "No se encontro el proceso.", "Error", 0);
                return;
            }

            if (!WindowAPI.AttachConsole(mainHandle))
            {
                if (!WindowAPI.AllocConsole())
                {
                    WindowAPI.MessageBox(0, "No se pudo asignar una consola.", "Error", 0);
                }
            }

            Console.WriteLine($"[mainHandle] {mainHandle.ToHex()}");

            IntPtr notepadTextbox = WindowAPI.FindWindowEx(mainHandle, IntPtr.Zero, "Edit", "");

            Console.WriteLine($"[notepadTextbox] {notepadTextbox.ToHex()}");

            InputManager.SendString(notepadTextbox, "Hola mundo!");

            //WindowAPI.ShowWindow(mainHandle, SW_SHOWMAXIMIZED);


            //Overlay

            string className = "NotePadLayer1";
            string windowTitle = "SherpNativeDLL";

            // Registrar la clase de ventana
            WNDCLASSEX wndClassEx = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = CS_HREDRAW | CS_VREDRAW,
                lpfnWndProc = WndProc,
                hInstance = Marshal.GetHINSTANCE(typeof(EntryPoint).Module),
                hCursor = WindowAPI.LoadCursor(IntPtr.Zero, IDC_ARROW),
                hbrBackground = (IntPtr)(COLOR_WINDOW + 1),
                lpszClassName = className
            };

            WindowAPI.RegisterClassEx(ref wndClassEx);

            if (mainHandle != 0)
            {
                WindowAPI.GetWindowRect(mainHandle, out topWndRect);

                X = topWndRect.Left;
                Y = topWndRect.Top;
                Width = topWndRect.Right - topWndRect.Left;
                Height = topWndRect.Bottom - topWndRect.Top;

                Console.WriteLine($"[Window] X: {X}, Y: {Y}, Width: {Width}, Height: {Height}");

                // Crear la ventana
                layWnd = WindowAPI.CreateWindowEx(
                    WS_EX_TOPMOST | WS_EX_LAYERED,                      // Estilos extendidos
                    className,              // Nombre de la clase de ventana registrada
                    windowTitle,            // Título de la ventana
                    WS_OVERLAPPEDWINDOW,               // Estilos de ventana
                    X,                      // Posición X
                    Y,                      // Posición Y
                    Width,                  // Ancho
                    Height,                 // Alto
                    IntPtr.Zero,            // Ventana padre (en este caso, no tiene)
                    IntPtr.Zero,            // Menú (en este caso, no tiene)
                    Marshal.GetHINSTANCE(typeof(EntryPoint).Module), // Instancia de la aplicación
                    IntPtr.Zero             // Parámetros adicionales
                );

                if (layWnd == IntPtr.Zero)
                {
                    uint errorCode = WindowAPI.GetLastError();

                    // Mostrar el código de error
                    Console.WriteLine($"Error al crear la ventana. Código de error: {errorCode}");
                    return;
                }

                Console.WriteLine($"[layWnd] {layWnd.ToHex()}");

                // Mostrar la ventana
                WindowAPI.SetLayeredWindowAttributes(layWnd, 0, RGB(255, 0, 0), LWA_COLORKEY);
                WindowAPI.ShowWindow(layWnd, SW_SHOW);
                WindowAPI.UpdateWindow(layWnd);
            }

            while (true)
            {
                if (WindowAPI.GetWindowRect(mainHandle, out topWndRect))
                {
                    X = topWndRect.Left;
                    Y = topWndRect.Top;
                    Width = topWndRect.Right - topWndRect.Left;
                    Height = topWndRect.Bottom - topWndRect.Top;

                    WindowAPI.MoveWindow(layWnd, X, Y, Width, Height, true); 
                }

                await Task.Delay(1);
            }
        }

        static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_DESTROY:
                    WindowAPI.PostQuitMessage(0);
                    return IntPtr.Zero;
                default:
                    return WindowAPI.DefWindowProc(hWnd, msg, wParam, lParam);
            }
        }

        static byte RGB(int r, int g, int b)
        {

            return (byte)Color.FromArgb(255, r, g, b).ToArgb();
        }
    }
}