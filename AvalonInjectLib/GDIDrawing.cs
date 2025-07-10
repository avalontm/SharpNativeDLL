using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static unsafe class GDIDrawing
    {
        // Constantes
        public const uint DLL_PROCESS_ATTACH = 1;
        public const int TRANSPARENT = 1;

        public static IntPtr HDC_Desktop;
        public static IntPtr Handle;
        public static IntPtr TargetWindow; // Ventana objetivo
        public static uint TextColor = 0x00FF00; // RGB(0, 255, 0)

        // Variables para tracking de ventana
        public static RECT LastWindowRect;
        public static bool IsTracking = false;

        // Importaciones GDI
        [DllImport("gdi32.dll")]
        public static extern bool Rectangle(IntPtr hdc, int left, int top, int right, int bottom);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateSolidBrush(uint color);

        [DllImport("user32.dll")]
        public static extern bool FillRect(IntPtr hdc, ref RECT lprc, IntPtr hbr);

        [DllImport("gdi32.dll")]
        public static extern bool TextOutA(IntPtr hdc, int x, int y, byte* lpString, int c);

        [DllImport("gdi32.dll")]
        public static extern bool SetBkMode(IntPtr hdc, int mode);

        [DllImport("gdi32.dll")]
        public static extern bool SetTextColor(IntPtr hdc, uint color);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hwnd, IntPtr hdc);

        // Nuevas importaciones para window tracking
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hwnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        // Métodos para inicializar y trackear ventana
        public static void InitializeWindowTracking(IntPtr targetWindow)
        {
            TargetWindow = targetWindow;
            HDC_Desktop = GetDC(GetDesktopWindow());
            IsTracking = true;
            GetWindowRect(TargetWindow, out LastWindowRect);
        }

        public static void StopTracking()
        {
            IsTracking = false;
            if (HDC_Desktop != IntPtr.Zero)
            {
                ReleaseDC(GetDesktopWindow(), HDC_Desktop);
                HDC_Desktop = IntPtr.Zero;
            }
        }

        // Método para verificar si la ventana se movió o cambió de tamaño
        public static bool HasWindowChanged()
        {
            if (!IsTracking || TargetWindow == IntPtr.Zero) return false;

            if (!IsWindow(TargetWindow) || !IsWindowVisible(TargetWindow))
                return false;

            GetWindowRect(TargetWindow, out RECT currentRect);

            bool changed = (currentRect.Left != LastWindowRect.Left ||
                          currentRect.Top != LastWindowRect.Top ||
                          currentRect.Right != LastWindowRect.Right ||
                          currentRect.Bottom != LastWindowRect.Bottom);

            if (changed)
            {
                LastWindowRect = currentRect;
            }

            return changed;
        }

        // Método para obtener las coordenadas de la ventana objetivo
        public static RECT GetTargetWindowRect()
        {
            if (TargetWindow != IntPtr.Zero && IsWindow(TargetWindow))
            {
                GetWindowRect(TargetWindow, out RECT rect);
                return rect;
            }
            return new RECT();
        }

        // Método para obtener el área cliente de la ventana objetivo
        public static RECT GetTargetClientRect()
        {
            if (TargetWindow != IntPtr.Zero && IsWindow(TargetWindow))
            {
                GetClientRect(TargetWindow, out RECT clientRect);

                // Convertir coordenadas del cliente a coordenadas de pantalla
                POINT topLeft = new POINT { x = clientRect.Left, y = clientRect.Top };
                POINT bottomRight = new POINT { x = clientRect.Right, y = clientRect.Bottom };

                ClientToScreen(TargetWindow, ref topLeft);
                ClientToScreen(TargetWindow, ref bottomRight);

                return new RECT
                {
                    Left = topLeft.x,
                    Top = topLeft.y,
                    Right = bottomRight.x,
                    Bottom = bottomRight.y
                };
            }
            return new RECT();
        }

        // Método mejorado para dibujar texto con limpieza automática
        public static void DrawTextOnWindow(string text, int offsetX = 10, int offsetY = 10, bool clearPrevious = true)
        {
            if (!IsTracking || HDC_Desktop == IntPtr.Zero) return;

            RECT windowRect = GetTargetWindowRect();
            if (windowRect.Left == 0 && windowRect.Top == 0 && windowRect.Right == 0 && windowRect.Bottom == 0)
                return;

            int textX = windowRect.Left + offsetX;
            int textY = windowRect.Top + offsetY;

            // Limpiar área anterior si se solicita
            if (clearPrevious)
            {
                // Estimar tamaño del texto (aproximadamente)
                int textWidth = text.Length * 8; // Aproximado
                int textHeight = 16; // Altura estándar

                ClearArea(textX, textY, textWidth, textHeight);
            }

            // Configurar modo de dibujo
            SetBkMode(HDC_Desktop, TRANSPARENT);
            SetTextColor(HDC_Desktop, TextColor);

            // Convertir string a bytes
            byte[] textBytes = System.Text.Encoding.ASCII.GetBytes(text);

            fixed (byte* pText = textBytes)
            {
                TextOutA(HDC_Desktop, textX, textY, pText, textBytes.Length);
            }

            // Guardar área del texto para futuras limpiezas
            RECT textArea = new RECT
            {
                Left = textX,
                Top = textY,
                Right = textX + text.Length * 8,
                Bottom = textY + 16
            };
            textAreas.Add(textArea);
        }

        // Método alternativo con fondo sólido
        public static void DrawTextWithBackground(string text, int offsetX = 10, int offsetY = 10, uint bgColor = 0x000000)
        {
            if (!IsTracking || HDC_Desktop == IntPtr.Zero) return;

            RECT windowRect = GetTargetWindowRect();
            if (windowRect.Left == 0 && windowRect.Top == 0 && windowRect.Right == 0 && windowRect.Bottom == 0)
                return;

            int textX = windowRect.Left + offsetX;
            int textY = windowRect.Top + offsetY;

            // Calcular tamaño del texto
            int textWidth = text.Length * 8;
            int textHeight = 16;

            // Dibujar fondo
            RECT bgRect = new RECT
            {
                Left = textX - 2,
                Top = textY - 2,
                Right = textX + textWidth + 2,
                Bottom = textY + textHeight + 2
            };

            IntPtr bgBrush = CreateSolidBrush(bgColor);
            FillRect(HDC_Desktop, ref bgRect, bgBrush);
            DeleteObject(bgBrush);

            // Dibujar texto
            SetBkMode(HDC_Desktop, TRANSPARENT);
            SetTextColor(HDC_Desktop, TextColor);

            byte[] textBytes = System.Text.Encoding.ASCII.GetBytes(text);
            fixed (byte* pText = textBytes)
            {
                TextOutA(HDC_Desktop, textX, textY, pText, textBytes.Length);
            }
        }

        // Método para limpiar todo el overlay
        public static void ClearAllOverlay()
        {
            if (!IsTracking) return;

            ClearTextAreas();

            // Invalidar toda la ventana objetivo para forzar redibujado
            RECT windowRect = GetTargetWindowRect();
            if (windowRect.Left != 0 || windowRect.Top != 0 || windowRect.Right != 0 || windowRect.Bottom != 0)
            {
                InvalidateRect(GetDesktopWindow(), IntPtr.Zero, true);
            }
        }

        // Método para dibujar un rectángulo que coincida con la ventana objetivo
        public static void DrawWindowFrame(uint color = 0xFF0000, int thickness = 2)
        {
            if (!IsTracking || HDC_Desktop == IntPtr.Zero) return;

            RECT windowRect = GetTargetWindowRect();
            if (windowRect.Left == 0 && windowRect.Top == 0 && windowRect.Right == 0 && windowRect.Bottom == 0)
                return;

            IntPtr brush = CreateSolidBrush(color);

            // Dibujar marco (4 rectángulos para formar el borde)
            RECT topBorder = new RECT { Left = windowRect.Left, Top = windowRect.Top, Right = windowRect.Right, Bottom = windowRect.Top + thickness };
            RECT bottomBorder = new RECT { Left = windowRect.Left, Top = windowRect.Bottom - thickness, Right = windowRect.Right, Bottom = windowRect.Bottom };
            RECT leftBorder = new RECT { Left = windowRect.Left, Top = windowRect.Top, Right = windowRect.Left + thickness, Bottom = windowRect.Bottom };
            RECT rightBorder = new RECT { Left = windowRect.Right - thickness, Top = windowRect.Top, Right = windowRect.Right, Bottom = windowRect.Bottom };

            FillRect(HDC_Desktop, ref topBorder, brush);
            FillRect(HDC_Desktop, ref bottomBorder, brush);
            FillRect(HDC_Desktop, ref leftBorder, brush);
            FillRect(HDC_Desktop, ref rightBorder, brush);

            DeleteObject(brush);
        }

        // Variables para tracking de texto
        private static List<RECT> textAreas = new List<RECT>();
        private static IntPtr backgroundBrush = IntPtr.Zero;

        // Importaciones adicionales para limpieza
        [DllImport("user32.dll")]
        public static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll")]
        public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        [DllImport("gdi32.dll")]
        public static extern int GetBkColor(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern int SetBkColor(IntPtr hdc, uint color);

        [DllImport("gdi32.dll")]
        public static extern bool PatBlt(IntPtr hdc, int x, int y, int w, int h, uint rop);

        // Constantes para PatBlt
        public const uint PATCOPY = 0x00F00021;
        public const uint BLACKNESS = 0x00000042;
        public const uint WHITENESS = 0x00FF0062;

        // Método mejorado para limpiar texto anterior
        public static void ClearTextAreas()
        {
            if (!IsTracking || HDC_Desktop == IntPtr.Zero) return;

            for (int i = 0; i < textAreas.Count; i++)
            {
                // Opción 1: Limpiar con color de fondo
                IntPtr brush = CreateSolidBrush(0x000000); // Negro
                RECT textArea = textAreas[i];
                FillRect(HDC_Desktop, ref textArea, brush);
                DeleteObject(brush);

                // Opción 2: Forzar redibujado del área
                InvalidateRect(GetDesktopWindow(), IntPtr.Zero, true);
            }

            textAreas.Clear();
        }



        // Método alternativo usando captura de pantalla
        public static void ClearPreviousDrawing()
        {
            if (!IsTracking) return;

            // Forzar redibujado completo del desktop
            RedrawWindow(GetDesktopWindow(), IntPtr.Zero, IntPtr.Zero, 0x0100 | 0x0400);
        }

        // Método para limpiar área específica
        public static void ClearArea(int x, int y, int width, int height)
        {
            if (!IsTracking || HDC_Desktop == IntPtr.Zero) return;

            RECT clearRect = new RECT
            {
                Left = x,
                Top = y,
                Right = x + width,
                Bottom = y + height
            };

            // Limpiar con color negro
            IntPtr brush = CreateSolidBrush(0x000000);
            FillRect(HDC_Desktop, ref clearRect, brush);
            DeleteObject(brush);
        }
    }
}
