using System;
using System.Runtime.InteropServices;

namespace AvalonInjectLib
{
    public static class InputHook
    {
        // Make the delegate public to match the accessibility of the Install method  
        public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private static WndProcDelegate _originalWndProc;
        private static WndProcDelegate _hookDelegate;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static void Install(WndProcDelegate callback)
        {
            IntPtr hWnd = WinInterop.FindWindow(null, "AssaultCube");
            const int GWLP_WNDPROC = -4;

            _hookDelegate = callback;
            _originalWndProc = Marshal.GetDelegateForFunctionPointer<WndProcDelegate>(
                SetWindowLongPtr(hWnd, GWLP_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(_hookDelegate)));
        }

        public static IntPtr CallOriginalWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return _originalWndProc(hWnd, msg, wParam, lParam);
        }
    }
}
