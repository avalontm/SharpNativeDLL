using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNativeDLL.Helpers
{
    public static class MemoryManager
    {
        public static int ReadInt16(int hProcess, int lpBaseAddress)
        {
            int bytesRead = 0;
            byte[] buffer = new byte[2];

            WinInterop.ReadProcessMemory(hProcess, lpBaseAddress, buffer, buffer.Length, ref bytesRead);
            return BitConverter.ToInt16(buffer, 0);
        }

        public static int ReadInt32(int hProcess, int lpBaseAddress)
        {
            int bytesRead = 0;
            byte[] buffer = new byte[4];

            WinInterop.ReadProcessMemory(hProcess, lpBaseAddress, buffer, buffer.Length, ref bytesRead);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static string ReadString(int hProcess, int lpBaseAddress)
        {
            int bytesRead = 0;
            byte[] buffer = new byte[255];

            WinInterop.ReadProcessMemory(hProcess, lpBaseAddress, buffer, buffer.Length, ref bytesRead);
            return Encoding.UTF8.GetString(buffer);
        }
    }
}
