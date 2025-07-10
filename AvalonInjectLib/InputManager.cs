using System.Runtime.InteropServices;

namespace AvalonInjectLib
{
    public static class InputManager
    {
        // Constantes de mensajes
        public const uint WM_CHAR = 0x0102;
        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_KEYUP = 0x0101;
        public const uint WM_SETTEXT = 0x000C;
        public const uint EM_REPLACESEL = 0x00C2;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessageW(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            string lParam);

        /// <summary>
        /// Envía texto completo reemplazando el contenido actual (WM_SETTEXT)
        /// </summary>
        public static void SendString(IntPtr hWnd, string text)
        {
            SendMessageW(hWnd, WM_SETTEXT, IntPtr.Zero, text);
        }

        /// <summary>
        /// Inserta texto en la posición actual del cursor (EM_REPLACESEL)
        /// </summary>
        public static void InsertString(IntPtr hWnd, string text)
        {
            SendMessageW(hWnd, EM_REPLACESEL, (IntPtr)1, text);
        }

        /// <summary>
        /// Simula escritura por caracteres con delay personalizable
        /// </summary>
        public static void TypeString(IntPtr hWnd, string text, int delayMs = 50)
        {
            foreach (char c in text)
            {
                // Envía WM_CHAR para el caracter
                SendMessageW(hWnd, WM_CHAR, (IntPtr)c, null);

                // Pequeña pausa entre caracteres
                if (delayMs > 0)
                {
                    System.Threading.Thread.Sleep(delayMs);
                }
            }
        }

        /// <summary>
        /// Combina métodos para mejor compatibilidad con diferentes controles
        /// </summary>
        public static void SmartSendString(IntPtr hWnd, string text)
        {
            // Primero intenta WM_SETTEXT (más eficiente)
            try
            {
                SendString(hWnd, text);
            }
            catch
            {
                // Si falla, usa el método de tecleo
                TypeString(hWnd, text);
            }
        }
    }
}