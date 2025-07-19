using System;
using System.Runtime.InteropServices;

namespace AvalonInjectLib
{
    internal static class ConsoleManager
    {
        const int STD_INPUT_HANDLE = -10;
        const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
        const uint ENABLE_EXTENDED_FLAGS = 0x0080;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        internal static void DisableQuickEdit()
        {
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);
            if (!GetConsoleMode(consoleHandle, out uint consoleMode))
                return;

            // Clear the quick edit bit in the mode flags
            consoleMode &= ~ENABLE_QUICK_EDIT_MODE;
            consoleMode |= ENABLE_EXTENDED_FLAGS; // Must be set

            SetConsoleMode(consoleHandle, consoleMode);
        }
    }

}
