using System;
using System.Text;
using System.Runtime.InteropServices;

namespace AvalonInjectLib
{
    public static class MemoryManager
    {
        public static unsafe T Read<T>(IntPtr baseAddress, params int[] offsets) where T : unmanaged
        {
            byte* ptr = (byte*)baseAddress;

            if (offsets != null)
            {
                foreach (var offset in offsets)
                {
                    ptr = *(byte**)ptr;
                    ptr += offset;
                }
            }

            return *(T*)ptr;
        }

        public static unsafe void Write<T>(IntPtr baseAddress, T value, params int[] offsets) where T : unmanaged
        {
            byte* ptr = (byte*)baseAddress;

            if (offsets != null)
            {
                foreach (var offset in offsets)
                {
                    ptr = *(byte**)ptr;
                    ptr += offset;
                }
            }

            *(T*)ptr = value;
        }

        public static T Read<T>(long baseAddress, params int[] offsets) where T : unmanaged
            => Read<T>(new IntPtr(baseAddress), offsets);

        public static void Write<T>(long baseAddress, T value, params int[] offsets) where T : unmanaged
            => Write(new IntPtr(baseAddress), value, offsets);

        public static unsafe string ReadString(IntPtr baseAddress, int maxLength = 128, Encoding? encoding = null, params int[] offsets)
        {
            encoding ??= Encoding.ASCII;

            byte* ptr = (byte*)baseAddress;

            if (offsets != null)
            {
                foreach (var offset in offsets)
                {
                    ptr = *(byte**)ptr;
                    ptr += offset;
                }
            }

            int length = 0;
            while (length < maxLength && ptr[length] != 0)
                length++;

            byte[] buffer = new byte[length];
            Marshal.Copy((IntPtr)ptr, buffer, 0, length);
            return encoding.GetString(buffer);
        }

        public static string ReadString(long baseAddress, int maxLength = 128, Encoding? encoding = null, params int[] offsets)
            => ReadString(new IntPtr(baseAddress), maxLength, encoding, offsets);

        public static unsafe void WriteString(IntPtr baseAddress, string value, Encoding? encoding = null, bool nullTerminate = true, params int[] offsets)
        {
            encoding ??= Encoding.ASCII;
            byte[] bytes = encoding.GetBytes(value);
            int totalLength = bytes.Length + (nullTerminate ? 1 : 0);

            byte[] buffer = new byte[totalLength];
            Array.Copy(bytes, buffer, bytes.Length);
            if (nullTerminate)
                buffer[^1] = 0;

            byte* ptr = (byte*)baseAddress;

            if (offsets != null)
            {
                foreach (var offset in offsets)
                {
                    ptr = *(byte**)ptr;
                    ptr += offset;
                }
            }

            Marshal.Copy(buffer, 0, (IntPtr)ptr, buffer.Length);
        }

        public static void WriteString(long baseAddress, string value, Encoding? encoding = null, bool nullTerminate = true, params int[] offsets)
            => WriteString(new IntPtr(baseAddress), value, encoding, nullTerminate, offsets);
    }
}
