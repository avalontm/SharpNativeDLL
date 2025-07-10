using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace AvalonInjectLib
{
    public static unsafe class MemoryManager
    {
        // Constantes de protección de memoria
        public const uint PAGE_NOACCESS = 0x01;
        public const uint PAGE_READONLY = 0x02;
        public const uint PAGE_READWRITE = 0x04;
        public const uint PAGE_EXECUTE_READ = 0x20;
        public const uint PAGE_EXECUTE_READWRITE = 0x40;

        // Métodos nativos (P/Invoke minimalista)
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            void* lpBuffer,
            int nSize,
            out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            void* lpBuffer,
            int nSize,
            out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtectEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flNewProtect,
            out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int VirtualQueryEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION lpBuffer,
            uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        /// <summary>
        /// Lectura de memoria ultra-rápida (unsafe)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(IntPtr hProcess, IntPtr address) where T : unmanaged
        {
            T value;
            Read(hProcess, address, &value, sizeof(T));
            return value;
        }

        /// <summary>
        /// Lectura directa a buffer (máximo control)
        /// </summary>
        public static void Read(IntPtr hProcess, IntPtr address, void* buffer, int size)
        {
            if (!ReadProcessMemory(hProcess, address, buffer, size, out int bytesRead) || bytesRead != size)
                ThrowLastWin32Error("ReadProcessMemory failed");

            // Verificación opcional de protección
            if (!IsMemoryReadable(hProcess, address, size))
                throw new AccessViolationException($"Memory at 0x{address:X8} is not readable");
        }

        /// <summary>
        /// Escritura de memoria optimizada
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(IntPtr hProcess, IntPtr address, T value) where T : unmanaged
        {
            Write(hProcess, address, &value, sizeof(T));
        }

        /// <summary>
        /// Escritura directa desde buffer
        /// </summary>
        public static void Write(IntPtr hProcess, IntPtr address, void* buffer, int size)
        {
            // Cambiar protección temporalmente si es necesario
            uint oldProtect;
            if (!VirtualProtectEx(hProcess, address, (uint)size, PAGE_EXECUTE_READWRITE, out oldProtect))
                ThrowLastWin32Error("VirtualProtectEx failed");

            try
            {
                if (!WriteProcessMemory(hProcess, address, buffer, size, out int bytesWritten) || bytesWritten != size)
                    ThrowLastWin32Error("WriteProcessMemory failed");
            }
            finally
            {
                // Restaurar protección original
                VirtualProtectEx(hProcess, address, (uint)size, oldProtect, out _);
            }
        }

        /// <summary>
        /// Lectura de cadenas (UTF-8/Unicode)
        /// </summary>
        public static string ReadString(IntPtr hProcess, IntPtr address, int maxLength = 256, bool unicode = false)
        {
            byte* buffer = stackalloc byte[maxLength];
            Read(hProcess, address, buffer, maxLength);

            if (unicode)
            {
                // Para Unicode (UTF-16)
                int length = 0;
                while (length < maxLength && ((char*)buffer)[length] != '\0')
                    length++;

                return new string((char*)buffer, 0, length);
            }
            else
            {
                // Para UTF-8
                int length = 0;
                while (length < maxLength && buffer[length] != 0)
                    length++;

                return Encoding.UTF8.GetString(buffer, length);
            }
        }

        /// <summary>
        /// Escritura de cadenas (UTF-8/Unicode)
        /// </summary>
        public static void WriteString(IntPtr hProcess, IntPtr address, string value, bool unicode = false)
        {
            if (unicode)
            {
                // UTF-16 (2 bytes por caracter)
                int byteCount = (value.Length + 1) * 2;
                byte* buffer = stackalloc byte[byteCount];

                fixed (char* pValue = value)
                {
                    Buffer.MemoryCopy(pValue, buffer, byteCount, value.Length * 2);
                }
                buffer[byteCount - 2] = 0; // Null-terminator
                buffer[byteCount - 1] = 0;

                Write(hProcess, address, buffer, byteCount);
            }
            else
            {
                // UTF-8 (1-4 bytes por caracter)
                int byteCount = Encoding.UTF8.GetByteCount(value) + 1;
                byte* buffer = stackalloc byte[byteCount];

                int encodedBytes = Encoding.UTF8.GetBytes(value, new Span<byte>(buffer, byteCount - 1));
                buffer[encodedBytes] = 0; // Null-terminator

                Write(hProcess, address, buffer, encodedBytes + 1);
            }
        }

        /// <summary>
        /// Resolución de punteros en cadena
        /// </summary>
        public static IntPtr ResolvePointer(IntPtr hProcess, IntPtr baseAddress, params int[] offsets)
        {
            IntPtr current = baseAddress;
            foreach (int offset in offsets)
            {
                current = Read<IntPtr>(hProcess, current);
                if (current == IntPtr.Zero) return IntPtr.Zero;
                current = IntPtr.Add(current, offset);
            }
            return current;
        }

        /// <summary>
        /// Verifica permisos de memoria
        /// </summary>
        public static bool IsMemoryReadable(IntPtr hProcess, IntPtr address, int size)
        {
            MEMORY_BASIC_INFORMATION mbi;
            if (VirtualQueryEx(hProcess, address, out mbi, (uint)sizeof(MEMORY_BASIC_INFORMATION)) == 0)
                return false;

            return (mbi.Protect & (PAGE_READONLY | PAGE_READWRITE | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE)) != 0 &&
                   (mbi.State & 0x1000) != 0; // MEM_COMMIT
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowLastWin32Error(string message)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), message);
        }
    }
}
