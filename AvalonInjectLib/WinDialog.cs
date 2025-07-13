using System.Runtime.InteropServices;

namespace AvalonInjectLib
{
    public static class WinDialog
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(
            IntPtr hWnd,
            string lpText,
            string lpCaption,
            uint uType);


        // Constantes para MessageBox
        public const uint MB_OK = 0x00000000;
        public const uint MB_ICONINFORMATION = 0x00000040;
        public const uint MB_ICONERROR = 0x00000010;

        public static void ShowInfoDialog(string title, string message)
        {
            WinDialog.MessageBox(
                IntPtr.Zero,
                message,
                title,
                WinDialog.MB_OK | WinDialog.MB_ICONINFORMATION);
        }

        public static void ShowErrorDialog(string title, string message)
        {
            WinDialog.MessageBox(
                IntPtr.Zero,
                message,
                title,
                WinDialog.MB_OK | WinDialog.MB_ICONERROR);
        }
    }
}
