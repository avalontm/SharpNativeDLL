using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static class OverlayExtensions
    {
        public static void PositionOverlay(this IntPtr overlayHandle, IntPtr targetHandle)
        {
            if (WinInterop.GetWindowRect(targetHandle, out RECT rect))
            {
                WinInterop.SetWindowPos(
                    overlayHandle,
                    new IntPtr(WinInterop.HWND_TOPMOST),
                    rect.Left,
                    rect.Top,
                    rect.Right - rect.Left,
                    rect.Bottom - rect.Top,
                    0x0010); // SWP_NOACTIVATE
            }
        }

        public static void SetClickThrough(this IntPtr hWnd, bool enabled)
        {
            int style = WinInterop.GetWindowLong(hWnd, WinInterop.GWL_EXSTYLE);
            if (enabled)
                style |= WinInterop.WS_EX_TRANSPARENT;
            else
                style &= ~WinInterop.WS_EX_TRANSPARENT;

            WinInterop.SetWindowLong(hWnd, WinInterop.GWL_EXSTYLE, style);
        }
    }
}
