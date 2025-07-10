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

    }
}
