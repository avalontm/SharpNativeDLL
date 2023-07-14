using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace SharpNativeDLL
{
    public static class InputManager
    {
        const uint WM_CHAR = 0x0102;
        const uint WM_KEYDOWN = 0x0100;
        const uint WM_KEYUP = 0x0101;
        const uint WM_SYSKEYDOWN = 0x0104;
        const uint WM_SYSCOMMAND = 0x018;
        const uint SC_CLOSE = 0x053;
        const uint WM_SETTEXT = 0x000c;

        public static void SendString(IntPtr handle, string message)
        {
            WindowAPI.SendMessage(handle, WM_SETTEXT, 0, message);
        }
    }
}
