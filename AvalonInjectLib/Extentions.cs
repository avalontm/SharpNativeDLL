using AvalonInjectLib.UIFramework;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static class Extentions
    {
        public static string ToHex(this int str)
        {
            return string.Format("0x{0:X}", str.ToString());
        }
        public static string ToHex(this nint str)
        {
            return string.Format("0x{0:X}", str.ToString());
        }

        public static Vector2 Subtract(this Vector2 vector, Thickness thickness)
        {
            return vector.Subtract(thickness);
        }

        public static Rect ApplyMargin(this Rect rect, Thickness margin)
        {
            return rect.ApplyMargin(margin);
        }
    }
}
