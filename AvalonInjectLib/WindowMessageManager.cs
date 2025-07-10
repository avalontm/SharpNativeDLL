using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AvalonInjectLib
{
    public static class WindowMessageManager
    {
        // Constantes básicas de mensajes
        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_KEYUP = 0x0101;
        public const uint WM_CHAR = 0x0102;
        public const uint WM_LBUTTONDOWN = 0x0201;
        public const uint WM_LBUTTONUP = 0x0202;
        public const uint WM_RBUTTONDOWN = 0x0204;
        public const uint WM_RBUTTONUP = 0x0205;
        public const uint WM_SETTEXT = 0x000C;
        public const uint WM_COMMAND = 0x0111;
        public const uint WM_SYSCOMMAND = 0x0112;

        // Teclas virtuales comunes
        public const int VK_SPACE = 0x20;
        public const int VK_RETURN = 0x0D;
        public const int VK_ESCAPE = 0x1B;
        public const int VK_TAB = 0x09;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SendMessage(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        /// <summary>
        /// Envía un mensaje genérico a una ventana
        /// </summary>
        public static IntPtr Send(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return SendMessage(hWnd, msg, wParam, lParam);
        }

        /// <summary>
        /// Envía texto a un control de ventana
        /// </summary>
        public static void SetControlText(IntPtr hWnd, string text)
        {
            SendMessage(hWnd, WM_SETTEXT, IntPtr.Zero, text);
        }

        /// <summary>
        /// Simula una pulsación de tecla (keydown + keyup)
        /// </summary>
        public static void SendKeyPress(IntPtr hWnd, int virtualKey, int delayMs = 50)
        {
            SendMessage(hWnd, WM_KEYDOWN, (IntPtr)virtualKey, IntPtr.Zero);
            Thread.Sleep(delayMs);
            SendMessage(hWnd, WM_KEYUP, (IntPtr)virtualKey, IntPtr.Zero);
        }

        /// <summary>
        /// Simula un clic del mouse en coordenadas específicas
        /// </summary>
        public static void SendMouseClick(IntPtr hWnd, int x, int y, bool rightButton = false, int delayMs = 50)
        {
            uint downMsg = rightButton ? WM_RBUTTONDOWN : WM_LBUTTONDOWN;
            uint upMsg = rightButton ? WM_RBUTTONUP : WM_LBUTTONUP;
            IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));

            SendMessage(hWnd, downMsg, IntPtr.Zero, lParam);
            Thread.Sleep(delayMs);
            SendMessage(hWnd, upMsg, IntPtr.Zero, lParam);
        }

        /// <summary>
        /// Ejecuta un comando en la ventana (para botones, menús, etc.)
        /// </summary>
        public static void SendCommand(IntPtr hWnd, int commandId)
        {
            SendMessage(hWnd, WM_COMMAND, (IntPtr)commandId, IntPtr.Zero);
        }

        /// <summary>
        /// Envía una combinación de teclas (ej. Alt+F4)
        /// </summary>
        public static void SendKeyCombo(IntPtr hWnd, int mainKey, params int[] modifierKeys)
        {
            foreach (var key in modifierKeys)
            {
                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)key, IntPtr.Zero);
            }

            SendKeyPress(hWnd, mainKey, 30);

            foreach (var key in modifierKeys.Reverse())
            {
                SendMessage(hWnd, WM_KEYUP, (IntPtr)key, IntPtr.Zero);
            }
        }
    }
}