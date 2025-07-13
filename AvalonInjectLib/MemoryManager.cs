using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace AvalonInjectLib
{
    public static unsafe class MemoryManager
    {
        // Memory protection constants
        internal const uint PAGE_NOACCESS = 0x01;
        internal const uint PAGE_READONLY = 0x02;
        internal const uint PAGE_READWRITE = 0x04;
        internal const uint PAGE_EXECUTE_READ = 0x20;
        internal const uint PAGE_EXECUTE_READWRITE = 0x40;

        // Native methods
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            void* lpBuffer,
            int nSize,
            out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            void* lpBuffer,
            int nSize,
            out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool VirtualProtectEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flNewProtect,
            out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int VirtualQueryEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            out MEMORY_BASIC_INFORMATION lpBuffer,
            uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        internal struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        #region Address Resolution Methods

        /// <summary>
        /// Suma una dirección base con una serie de offsets sin realizar lecturas de memoria
        /// </summary>
        public static IntPtr AddOffsets(IntPtr baseAddress, params int[] offsets)
        {
            if (offsets == null || offsets.Length == 0)
                return baseAddress;

            IntPtr current = baseAddress;

            foreach (int offset in offsets)
            {
                current = IntPtr.Add(current, offset);
            }

            return current;
        }

        /// <summary>
        /// Resolves a pointer chain (base + offset1 + offset2 + ...)
        /// </summary>
        public static IntPtr ResolvePointerChain(IntPtr hProcess, IntPtr baseAddress, params int[] offsets)
        {
            if (offsets == null || offsets.Length == 0)
                return baseAddress;

            IntPtr current = baseAddress;

            for (int i = 0; i < offsets.Length; i++)
            {
                // Read the pointer value first
                current = ReadDirect<IntPtr>(hProcess, current);
                if (current == IntPtr.Zero)
                    throw new InvalidOperationException($"Pointer chain resolution failed at offset {i} - null pointer encountered");

                // Then add the offset
                current = IntPtr.Add(current, offsets[i]);
            }

            return current;
        }

        /// <summary>
        /// Resolves address using CalculatedAddress with offsets
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ResolveAddress(CalculatedAddress calcAddr, params int[] offsets)
        {
            return ResolvePointerChain(calcAddr.Process.Handle, calcAddr.Address, offsets);
        }

        /// <summary>
        /// Resolves address and validates it's not null
        /// </summary>
        public static IntPtr ResolveAndValidateAddress(IntPtr hProcess, IntPtr baseAddress, params int[] offsets)
        {
            IntPtr resolvedAddress = ResolvePointerChain(hProcess, baseAddress, offsets);
            if (resolvedAddress == IntPtr.Zero)
                throw new InvalidOperationException("Resolved address is null");
            return resolvedAddress;
        }

        #endregion

        #region Direct Read/Write Methods (No Address Resolution)

        /// <summary>
        /// Reads a value directly from memory address (no pointer chain resolution)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadDirect<T>(IntPtr hProcess, IntPtr address) where T : unmanaged
        {
            T value;
            ReadRaw(hProcess, address, &value, sizeof(T));
            return value;
        }

        /// <summary>
        /// Writes a value directly to memory address (no pointer chain resolution)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDirect<T>(IntPtr hProcess, IntPtr address, T value) where T : unmanaged
        {
            WriteRaw(hProcess, address, &value, sizeof(T));
        }

        /// <summary>
        /// Reads a string directly from memory address (no pointer chain resolution)
        /// </summary>
        public static string ReadStringDirect(IntPtr hProcess, IntPtr address, int maxLength = 256, bool unicode = false)
        {
            byte* buffer = stackalloc byte[maxLength];
            ReadRaw(hProcess, address, buffer, maxLength);

            if (unicode)
            {
                // For Unicode (UTF-16)
                int length = 0;
                while (length < maxLength / 2 && ((char*)buffer)[length] != '\0')
                    length++;

                string result = new string((char*)buffer, 0, length);
                Debug.WriteLine($"[READ STRING DIRECT] 0x{address.ToInt64():X} -> \"{result}\" (Unicode)");
                return result;
            }
            else
            {
                // For UTF-8
                int length = 0;
                while (length < maxLength && buffer[length] != 0)
                    length++;

                string result = Encoding.UTF8.GetString(buffer, length);
                Debug.WriteLine($"[READ STRING DIRECT] 0x{address.ToInt64():X} -> \"{result}\" (UTF-8)");
                return result;
            }
        }

        /// <summary>
        /// Writes a string directly to memory address (no pointer chain resolution)
        /// </summary>
        public static void WriteStringDirect(IntPtr hProcess, IntPtr address, string value, bool unicode = false)
        {
            Debug.WriteLine($"[WRITE STRING DIRECT] 0x{address.ToInt64():X} <- \"{value}\" ({(unicode ? "Unicode" : "UTF-8")})");

            if (unicode)
            {
                // UTF-16 (2 bytes per character)
                int byteCount = (value.Length + 1) * 2;
                byte* buffer = stackalloc byte[byteCount];

                fixed (char* pValue = value)
                {
                    Buffer.MemoryCopy(pValue, buffer, byteCount, value.Length * 2);
                }
                buffer[byteCount - 2] = 0; // Null-terminator
                buffer[byteCount - 1] = 0;

                WriteRaw(hProcess, address, buffer, byteCount);
            }
            else
            {
                // UTF-8 (1-4 bytes per character)
                int byteCount = Encoding.UTF8.GetByteCount(value) + 1;
                byte* buffer = stackalloc byte[byteCount];

                int encodedBytes = Encoding.UTF8.GetBytes(value, new Span<byte>(buffer, byteCount - 1));
                buffer[encodedBytes] = 0; // Null-terminator

                WriteRaw(hProcess, address, buffer, encodedBytes + 1);
            }
        }

        #endregion

        #region Read Methods with Address Resolution

        /// <summary>
        /// Reads a value from memory using CalculatedAddress with optional offsets
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(CalculatedAddress calcAddr, params int[] offsets) where T : unmanaged
        {
            IntPtr resolvedAddress = ResolveAddress(calcAddr, offsets);
            return ReadDirect<T>(calcAddr.Process.Handle, resolvedAddress);
        }

        /// <summary>
        /// Reads a value from memory (base address + offsets)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(IntPtr hProcess, IntPtr baseAddress, params int[] offsets) where T : unmanaged
        {
            IntPtr resolvedAddress = AddOffsets(baseAddress, offsets);
            return ReadDirect<T>(hProcess, resolvedAddress);
        }

        /// <summary>
        /// Reads a string from memory using CalculatedAddress with optional offsets
        /// </summary>
        public static string ReadString(CalculatedAddress calcAddr, int maxLength = 256, bool unicode = false, params int[] offsets)
        {
            IntPtr resolvedAddress = ResolveAddress(calcAddr, offsets);
            return ReadStringDirect(calcAddr.Process.Handle, resolvedAddress, maxLength, unicode);
        }

        /// <summary>
        /// Reads a string from memory (base address + offsets)
        /// </summary>
        public static string ReadString(IntPtr hProcess, IntPtr baseAddress, int maxLength = 256, bool unicode = false, params int[] offsets)
        {
            IntPtr resolvedAddress = AddOffsets(baseAddress, offsets);
            return ReadStringDirect(hProcess, resolvedAddress, maxLength, unicode);
        }

        #endregion

        #region Write Methods with Address Resolution

        /// <summary>
        /// Writes a value to memory using CalculatedAddress with optional offsets
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(CalculatedAddress calcAddr, T value, params int[] offsets) where T : unmanaged
        {
            IntPtr resolvedAddress = ResolveAddress(calcAddr, offsets);
            WriteDirect(calcAddr.Process.Handle, resolvedAddress, value);
        }

        /// <summary>
        /// Writes a value to memory (base address + offsets)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(IntPtr hProcess, IntPtr baseAddress, T value, params int[] offsets) where T : unmanaged
        {
            IntPtr resolvedAddress = AddOffsets(baseAddress, offsets);
            WriteDirect(hProcess, resolvedAddress, value);
        }

        /// <summary>
        /// Writes a string to memory using CalculatedAddress with optional offsets
        /// </summary>
        public static void WriteString(CalculatedAddress calcAddr, string value, bool unicode = false, params int[] offsets)
        {
            IntPtr resolvedAddress = ResolveAddress(calcAddr, offsets);
            WriteStringDirect(calcAddr.Process.Handle, resolvedAddress, value, unicode);
        }

        /// <summary>
        /// Writes a string to memory (base address + offsets)
        /// </summary>
        public static void WriteString(IntPtr hProcess, IntPtr baseAddress, string value, bool unicode = false, params int[] offsets)
        {
            IntPtr resolvedAddress = AddOffsets(baseAddress, offsets);
            WriteStringDirect(hProcess, resolvedAddress, value, unicode);
        }

        #endregion

        #region Low-Level Raw Methods

        private static void ReadRaw(IntPtr hProcess, IntPtr address, void* buffer, int size)
        {
            if (!ReadProcessMemory(hProcess, address, buffer, size, out int bytesRead) || bytesRead != size)
                ThrowLastWin32Error($"Failed to read memory at 0x{address:X8}");
        }

        private static void WriteRaw(IntPtr hProcess, IntPtr address, void* buffer, int size)
        {
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
                VirtualProtectEx(hProcess, address, (uint)size, oldProtect, out _);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ThrowLastWin32Error(string message)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), message);
        }

        #endregion
    }
}