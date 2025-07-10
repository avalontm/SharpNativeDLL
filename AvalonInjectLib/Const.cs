namespace AvalonInjectLib
{
    public static class Const
    {
        public const int CS_HREDRAW = 0x0001;
        public const int CS_VREDRAW = 0x0002;
        public const int CS_OWNDC = 0x0020;

        public const int WS_OVERLAPPED = 0x00000000;
        // Constantes para los estilos de ventana
        public const int WS_POPUP = unchecked((int)0x80000000);

        public const int WS_CAPTION = 0x00000000;
        public const int WS_VISIBLE = 0x10000000;
        public const int WS_SYSMENU = 0x00080000;
        public const int WS_THICKFRAME = 0x00040000;
        public const int WS_MINIMIZE = 0x20000000;
        public const int WS_MAXIMIZE = 0x01000000;
        public const int WS_CLIPCHILDREN = 0x02000000;
        public const int WS_CLIPSIBLINGS = 0x04000000;
        public const int WS_EX_APPWINDOW = 0x00040000;
        public const int WS_EX_WINDOWEDGE = 0x00000100;

        public const int SWP_NOSIZE = 0x0001;
        public const int SWP_NOMOVE = 0x0002;
        public const int SWP_NOZORDER = 0x0004;
        public const int SWP_FRAMECHANGED = 0x0020;

        public const uint PM_REMOVE = 0x0001;
        public const uint WM_QUIT = 0x0012;

        public const int HWND_TOPMOST = -1;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_HIDEWINDOW = 0x0080;

        public const int GWL_EXSTYLE = -20;
        public const int GWL_STYLE = -16;
        public const uint WS_MINIMIZEBOX = 0x00020000;

        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int LWA_COLORKEY = 0x00000001;

        public const int WM_NCHITTEST = 0x0084;
        public const int HTTRANSPARENT = -1;
        public const int WS_EX_TOPMOST = 0x00000008;

        public const int SW_SHOW = 5;    // Mostrar la ventana
        public const int SW_HIDE = 0;    // Ocultar la ventana

        public const int HWND_BOTTOM = 1;
    }
}
