namespace AvalonInjectLib
{
    using System;
    using System.Runtime.InteropServices;

    internal static class ScreenDimensionsHandler
    {
        // Importaciones de Win32 API
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        // Constantes para GetDeviceCaps
        private const int HORZRES = 8;
        private const int VERTRES = 10;
        private const int DESKTOPHORZRES = 118;
        private const int DESKTOPVERTRES = 117;

        // Constantes para GetSystemMetrics
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;

        // Variables estáticas para las dimensiones
        private static int cachedScreenWidth = 0;
        private static int cachedScreenHeight = 0;
        private static int cachedViewportWidth = 0;
        private static int cachedViewportHeight = 0;

        private const int GL_VIEWPORT = 0x0BA2;

        /// <summary>
        /// Obtiene las dimensiones reales de la pantalla
        /// </summary>
        internal static (int width, int height) GetScreenDimensions()
        {
            if (cachedScreenWidth == 0 || cachedScreenHeight == 0)
            {
                // Método 1: Usar GetSystemMetrics (más rápido)
                cachedScreenWidth = GetSystemMetrics(SM_CXSCREEN);
                cachedScreenHeight = GetSystemMetrics(SM_CYSCREEN);

                // Método 2: Usar GetDeviceCaps para mayor precisión (opcional)
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    int dcWidth = GetDeviceCaps(hdc, HORZRES);
                    int dcHeight = GetDeviceCaps(hdc, VERTRES);
                    ReleaseDC(IntPtr.Zero, hdc);

                    // Usar las dimensiones del DC si son diferentes
                    if (dcWidth > 0 && dcHeight > 0)
                    {
                        cachedScreenWidth = dcWidth;
                        cachedScreenHeight = dcHeight;
                    }
                }
            }

            return (cachedScreenWidth, cachedScreenHeight);
        }

        /// <summary>
        /// Obtiene las dimensiones del viewport actual de OpenGL
        /// </summary>
        internal static (int width, int height) GetCurrentViewportDimensions()
        {
            int[] viewport = new int[4];
            OpenGLInterop.glGetIntegerv(GL_VIEWPORT, viewport);
            return (viewport[2], viewport[3]); // width, height
        }

        /// <summary>
        /// Configura el viewport para usar las dimensiones completas de la pantalla
        /// </summary>
        internal static void SetFullScreenViewport()
        {
            var (width, height) = GetScreenDimensions();
            OpenGLInterop.glViewport(0, 0, width, height);
            cachedViewportWidth = width;
            cachedViewportHeight = height;
        }

        /// <summary>
        /// Configura el viewport basado en el HDC actual
        /// </summary>
        internal static void SetViewportFromHDC(IntPtr hdc)
        {
            if (hdc != IntPtr.Zero)
            {
                int width = GetDeviceCaps(hdc, HORZRES);
                int height = GetDeviceCaps(hdc, VERTRES);

                if (width > 0 && height > 0)
                {
                    OpenGLInterop.glViewport(0, 0, width, height);
                    cachedViewportWidth = width;
                    cachedViewportHeight = height;
                }
            }
        }

        /// <summary>
        /// Obtiene las dimensiones del viewport cacheadas
        /// </summary>
        internal static (int width, int height) GetCachedViewportDimensions()
        {
            return (cachedViewportWidth, cachedViewportHeight);
        }

        /// <summary>
        /// Fuerza la actualización del cache de dimensiones
        /// </summary>
        internal static void RefreshDimensionsCache()
        {
            cachedScreenWidth = 0;
            cachedScreenHeight = 0;
            cachedViewportWidth = 0;
            cachedViewportHeight = 0;
        }
    }
}
