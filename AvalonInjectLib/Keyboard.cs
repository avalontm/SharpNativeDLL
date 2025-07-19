using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvalonInjectLib
{
    internal class Keyboard
    {
        internal static string GetKeyName(int keyCode)
        {
            return keyCode switch
            {
                // Mouse buttons
                0x01 => "MouseLeft",
                0x02 => "MouseRight",
                0x04 => "MouseMiddle",
                0x05 => "MouseX1",
                0x06 => "MouseX2",

                // Special keys
                0x08 => "Backspace",
                0x09 => "Tab",
                0x0D => "Enter",
                0x10 => "Shift",
                0x11 => "Ctrl",
                0x12 => "Alt",
                0x1B => "Escape",
                0x20 => "Space",

                // Arrows
                0x25 => "Left",
                0x26 => "Up",
                0x27 => "Right",
                0x28 => "Down",

                // Numbers
                0x30 => "0",
                0x31 => "1",
                0x32 => "2",
                0x33 => "3",
                0x34 => "4",
                0x35 => "5",
                0x36 => "6",
                0x37 => "7",
                0x38 => "8",
                0x39 => "9",

                // Letters
                0x41 => "A",
                0x42 => "B",
                0x43 => "C",
                0x44 => "D",
                0x45 => "E",
                0x46 => "F",
                0x47 => "G",
                0x48 => "H",
                0x49 => "I",
                0x4A => "J",
                0x4B => "K",
                0x4C => "L",
                0x4D => "M",
                0x4E => "N",
                0x4F => "O",
                0x50 => "P",
                0x51 => "Q",
                0x52 => "R",
                0x53 => "S",
                0x54 => "T",
                0x55 => "U",
                0x56 => "V",
                0x57 => "W",
                0x58 => "X",
                0x59 => "Y",
                0x5A => "Z",

                _ => $"Key{keyCode:X2}"
            };
        }
    }
}
