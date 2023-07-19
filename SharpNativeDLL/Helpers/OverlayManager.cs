using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SharpNativeDLL.Helpers.Structs;

namespace SharpNativeDLL.Helpers
{
    public static class OverlayManager
    {
        // Algunos valores de mensajes comunes
        public const uint WM_CREATE = 0x0001;
        public const uint WM_CLOSE = 0x0010;
        public const uint WM_DESTROY = 0x0002;
        public const uint WM_PAINT = 0x000F;
        public const uint WM_SIZE = 0x0005;
        public const uint WM_MOUSEMOVE = 0x0200;
        public const uint WM_KEYDOWN = 0x0100;


        static RECT topWndRect;
        static int X = 0;
        static int Y = 0;
        static int Width = 0;
        static int Height = 0;
        static IntPtr layWnd = IntPtr.Zero;

        public static IntPtr CreateWindow(IntPtr mainHandle)
        {
            string className = "SharpNativeLayer1";

            // Registrar la clase de ventana
            WNDCLASSEX wndClassEx = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf(typeof(WNDCLASSEX)),
                style = Const.CS_HREDRAW | Const.CS_VREDRAW | Const.CS_OWNDC,
                lpfnWndProc = WndProc,
                hInstance = Marshal.GetHINSTANCE(typeof(EntryPoint).Module),
                lpszClassName = className
            };

            WinInterop.RegisterClassEx(ref wndClassEx);

            if (mainHandle != 0)
            {
                WinInterop.GetWindowRect(mainHandle, out topWndRect);

                X = topWndRect.Left;
                Y = topWndRect.Top;
                Width = topWndRect.Right - topWndRect.Left;
                Height = topWndRect.Bottom - topWndRect.Top;

                Console.WriteLine($"[Window] X: {X}, Y: {Y}, Width: {Width}, Height: {Height}");

                // Crear la ventana
                layWnd = WinInterop.CreateWindowEx(
                    Const.WS_EX_LAYERED,                      // Estilos extendidos
                    className,              // Nombre de la clase de ventana registrada
                    null,            // Título de la ventana
                    Const.WS_POPUP | Const.WS_EX_TOPMOST,               // Estilos de ventana
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
                    uint errorCode = WinInterop.GetLastError();

                    // Mostrar el código de error
                    Console.WriteLine($"Error al crear la ventana. Código de error: {errorCode}");
                    return IntPtr.Zero;
                }

                Console.WriteLine($"[layWnd] {layWnd.ToHex()}");

                WindowStyle();

                // Establecer el color transparente (en este ejemplo, el color magenta #FF00FF será transparente)
                WinInterop.SetLayeredWindowAttributes(layWnd, 0xFF00FF, 0, Const.LWA_COLORKEY);

                // Mostrar la ventana
                WinInterop.ShowWindow(layWnd, Const.SW_SHOW);
                WinInterop.UpdateWindow(layWnd);
            }

            return layWnd;
        }

        static void WindowStyle()
        {
            // Establecer el estilo de ventana WS_EX_LAYERED y WS_EX_TRANSPARENT
            uint exStyle = (uint)WinInterop.GetWindowLong(layWnd, Const.GWL_EXSTYLE);
            exStyle |= Const.WS_EX_LAYERED | Const.WS_EX_TRANSPARENT;
            WinInterop.SetWindowLong(layWnd, Const.GWL_EXSTYLE, exStyle & ~(uint)(Const.WS_CAPTION | Const.WS_THICKFRAME));

        }

        static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_CREATE:
                    // Se envía cuando la ventana está siendo creada.
                    break;

                case WM_CLOSE:
                    // Se envía cuando el usuario solicita cerrar la ventana (por ejemplo, al hacer clic en la "X" de la barra de título).
                    break;

                case WM_PAINT:
                    // Se envía cuando la ventana necesita ser repintada (redibujada).
                    break;

                case WM_SIZE:
                    // Se envía cuando el tamaño de la ventana ha cambiado.
                    break;

                case WM_MOUSEMOVE:
                    // Se envía cuando el mouse se mueve dentro de la ventana.
                    break;

                case WM_KEYDOWN:
                    // Se envía cuando una tecla del teclado es presionada.
                    break;
                case WM_DESTROY:
                    // Se envía cuando la ventana está siendo destruida.
                    WinInterop.PostQuitMessage(0);
                    break;
                default:
                    // Mensaje no reconocido, dejar que el sistema maneje el mensaje.
                    return WinInterop.DefWindowProc(hWnd, msg, wParam, lParam);
            }

            // Si el mensaje ha sido procesado, devolver cero.
            return IntPtr.Zero;
        }


        public static void OnUpdate(IntPtr mainHandle)
        {
            if (WinInterop.GetWindowRect(mainHandle, out topWndRect))
            {
                WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
                placement.length = Marshal.SizeOf(placement);
                WinInterop.GetWindowPlacement(mainHandle, ref placement);

                if (placement.showCmd == 2)
                {
                    WinInterop.SetWindowPos(layWnd, new IntPtr(Const.HWND_BOTTOM), 0, 0, 0, 0, 0);
                }
                else
                {
                    X = topWndRect.Left;
                    Y = topWndRect.Top;
                    Width = topWndRect.Right - topWndRect.Left;
                    Height = topWndRect.Bottom - topWndRect.Top;

                    WinInterop.SetWindowPos(layWnd, new IntPtr(Const.HWND_TOPMOST), X, Y, Width, Height, 0);

                    WinInterop.MoveWindow(layWnd, X, Y, Width, Height, true);
                }
            }
        }
    }
}
