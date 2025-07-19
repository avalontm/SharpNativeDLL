using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AvalonInjectLib
{
    /// <summary>
    /// AOT-optimized memory manager for process injection scenarios.
    /// Provides safe memory operations with automatic cleanup and error handling.
    /// </summary>
    public static unsafe class MemoryManager
    {
        #region Constants and Native Structures

        // Memory protection constants
        internal const uint PAGE_NOACCESS = 0x01;
        internal const uint PAGE_READONLY = 0x02;
        internal const uint PAGE_READWRITE = 0x04;
        internal const uint PAGE_EXECUTE_READ = 0x20;
        internal const uint PAGE_EXECUTE_READWRITE = 0x40;

        // Memory allocation constants
        private const int MAX_STRING_LENGTH = 4096;
        private const int DEFAULT_BUFFER_SIZE = 1024;
        private const int MAX_RETRY_COUNT = 3;

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

        #endregion

        #region Native API Declarations

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

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool FlushInstructionCache(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            uint dwSize);

        #endregion

        #region Thread-Safe Operation Tracking

        private static volatile int _activeOperations = 0;
        private static readonly object _lockObject = new object();

        /// <summary>
        /// Tracks active operations to prevent race conditions
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void BeginOperation()
        {
            Interlocked.Increment(ref _activeOperations);
        }

        /// <summary>
        /// Ends operation tracking and optionally triggers cleanup
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EndOperation()
        {
            int current = Interlocked.Decrement(ref _activeOperations);
            if (current == 0)
            {
                // Trigger cleanup when no operations are active
                TriggerCleanup();
            }
        }

        /// <summary>
        /// Performs lightweight cleanup operations
        /// </summary>
        private static void TriggerCleanup()
        {
            try
            {
                // Force garbage collection for AOT scenarios
                GC.Collect(0, GCCollectionMode.Optimized);
                GC.WaitForPendingFinalizers();
            }
            catch
            {
                // Ignore cleanup failures to prevent cascading issues
            }
        }

        #endregion

        #region Address Resolution Methods

        /// <summary>
        /// Adds offsets to a base address without memory reads (static calculation)
        /// </summary>
        /// <param name="baseAddress">Base memory address</param>
        /// <param name="offsets">Array of offsets to add</param>
        /// <returns>Calculated address</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        /// Resolves a pointer chain with automatic validation and error handling
        /// </summary>
        /// <param name="hProcess">Process handle</param>
        /// <param name="baseAddress">Base address to start from</param>
        /// <param name="offsets">Chain of offsets to follow</param>
        /// <returns>Final resolved address</returns>
        public static IntPtr ResolvePointerChain(IntPtr hProcess, IntPtr baseAddress, params int[] offsets)
        {
            if (offsets == null || offsets.Length == 0)
                return baseAddress;

            BeginOperation();
            try
            {
                IntPtr current = baseAddress;

                for (int i = 0; i < offsets.Length; i++)
                {
                    // Validate current pointer before reading
                    if (!IsValidAddress(hProcess, current))
                        throw new InvalidOperationException($"Invalid address at offset {i}: 0x{current:X}");

                    // Read pointer value with retry logic
                    current = ReadWithRetry<IntPtr>(hProcess, current);

                    if (current == IntPtr.Zero)
                        throw new InvalidOperationException($"Null pointer encountered at offset {i}");

                    // Add the offset
                    current = IntPtr.Add(current, offsets[i]);
                }

                return current;
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Resolves address using CalculatedAddress with validation
        /// </summary>
        /// <param name="calcAddr">Calculated address object</param>
        /// <param name="offsets">Optional offsets to apply</param>
        /// <returns>Resolved address</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr ResolveAddress(CalculatedAddress calcAddr, params int[] offsets)
        {
            if (calcAddr.Process?.Handle == IntPtr.Zero)
                throw new ArgumentException("Invalid process handle in CalculatedAddress");

            return ResolvePointerChain(calcAddr.Process.Handle, calcAddr.Address, offsets);
        }

        /// <summary>
        /// Validates if an address is readable in the target process
        /// </summary>
        /// <param name="hProcess">Process handle</param>
        /// <param name="address">Address to validate</param>
        /// <returns>True if address is valid</returns>
        private static bool IsValidAddress(IntPtr hProcess, IntPtr address)
        {
            if (address == IntPtr.Zero) return false;

            int result = VirtualQueryEx(hProcess, address, out MEMORY_BASIC_INFORMATION mbi,
                (uint)Marshal.SizeOf<MEMORY_BASIC_INFORMATION>());

            return result != 0 && mbi.State == 0x1000; // MEM_COMMIT
        }

        #endregion

        #region Direct Memory Operations (No Address Resolution)

        /// <summary>
        /// Reads a value directly from memory with error handling
        /// </summary>
        /// <typeparam name="T">Unmanaged type to read</typeparam>
        /// <param name="hProcess">Process handle</param>
        /// <param name="address">Memory address</param>
        /// <returns>Read value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadDirect<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(IntPtr hProcess, IntPtr address) where T : unmanaged
        {
            BeginOperation();
            try
            {
                return ReadWithRetry<T>(hProcess, address);
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Writes a value directly to memory with protection handling
        /// </summary>
        /// <typeparam name="T">Unmanaged type to write</typeparam>
        /// <param name="hProcess">Process handle</param>
        /// <param name="address">Memory address</param>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDirect<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(IntPtr hProcess, IntPtr address, T value) where T : unmanaged
        {
            BeginOperation();
            try
            {
                WriteWithRetry(hProcess, address, value);
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Reads a string directly from memory with encoding detection
        /// </summary>
        /// <param name="hProcess">Process handle</param>
        /// <param name="address">Memory address</param>
        /// <param name="maxLength">Maximum string length</param>
        /// <param name="unicode">True for UTF-16, false for UTF-8</param>
        /// <returns>Read string</returns>
        public static string ReadStringDirect(IntPtr hProcess, IntPtr address, int maxLength = 256, bool unicode = false)
        {
            // Clamp max length to prevent excessive memory usage
            maxLength = Math.Min(maxLength, MAX_STRING_LENGTH);

            BeginOperation();
            try
            {
                // Use stack allocation for small strings, heap for large ones
                if (maxLength <= DEFAULT_BUFFER_SIZE)
                {
                    byte* buffer = stackalloc byte[maxLength];
                    return ReadStringFromBuffer(hProcess, address, buffer, maxLength, unicode);
                }
                else
                {
                    // Use pinned memory for large strings
                    byte[] managedBuffer = new byte[maxLength];
                    fixed (byte* buffer = managedBuffer)
                    {
                        return ReadStringFromBuffer(hProcess, address, buffer, maxLength, unicode);
                    }
                }
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Helper method to read string from buffer with proper encoding
        /// </summary>
        private static string ReadStringFromBuffer(IntPtr hProcess, IntPtr address, byte* buffer, int maxLength, bool unicode)
        {
            ReadRaw(hProcess, address, buffer, maxLength);

            if (unicode)
            {
                // UTF-16 processing
                int charCount = maxLength / 2;
                int length = 0;
                char* charBuffer = (char*)buffer;

                while (length < charCount && charBuffer[length] != '\0')
                    length++;

                return new string(charBuffer, 0, length);
            }
            else
            {
                // UTF-8 processing
                int length = 0;
                while (length < maxLength && buffer[length] != 0)
                    length++;

                return Encoding.UTF8.GetString(buffer, length);
            }
        }

        /// <summary>
        /// Writes a string directly to memory with proper encoding
        /// </summary>
        /// <param name="hProcess">Process handle</param>
        /// <param name="address">Memory address</param>
        /// <param name="value">String value to write</param>
        /// <param name="unicode">True for UTF-16, false for UTF-8</param>
        public static void WriteStringDirect(IntPtr hProcess, IntPtr address, string value, bool unicode = false)
        {
            if (string.IsNullOrEmpty(value))
            {
                // Write null terminator only
                byte nullByte = 0;
                WriteRaw(hProcess, address, &nullByte, 1);
                return;
            }

            BeginOperation();
            try
            {
                if (unicode)
                {
                    WriteUnicodeString(hProcess, address, value);
                }
                else
                {
                    WriteUtf8String(hProcess, address, value);
                }
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Writes UTF-16 string to memory
        /// </summary>
        private static void WriteUnicodeString(IntPtr hProcess, IntPtr address, string value)
        {
            int byteCount = (value.Length + 1) * 2;

            if (byteCount <= DEFAULT_BUFFER_SIZE)
            {
                byte* buffer = stackalloc byte[byteCount];
                WriteUnicodeToBuffer(hProcess, address, value, buffer, byteCount);
            }
            else
            {
                byte[] managedBuffer = new byte[byteCount];
                fixed (byte* buffer = managedBuffer)
                {
                    WriteUnicodeToBuffer(hProcess, address, value, buffer, byteCount);
                }
            }
        }

        /// <summary>
        /// Helper to write Unicode string to buffer
        /// </summary>
        private static void WriteUnicodeToBuffer(IntPtr hProcess, IntPtr address, string value, byte* buffer, int byteCount)
        {
            fixed (char* pValue = value)
            {
                Buffer.MemoryCopy(pValue, buffer, byteCount, value.Length * 2);
            }

            // Add null terminator
            buffer[byteCount - 2] = 0;
            buffer[byteCount - 1] = 0;

            WriteRaw(hProcess, address, buffer, byteCount);
        }

        /// <summary>
        /// Writes UTF-8 string to memory
        /// </summary>
        private static void WriteUtf8String(IntPtr hProcess, IntPtr address, string value)
        {
            int byteCount = Encoding.UTF8.GetByteCount(value) + 1;

            if (byteCount <= DEFAULT_BUFFER_SIZE)
            {
                byte* buffer = stackalloc byte[byteCount];
                WriteUtf8ToBuffer(hProcess, address, value, buffer, byteCount);
            }
            else
            {
                byte[] managedBuffer = new byte[byteCount];
                fixed (byte* buffer = managedBuffer)
                {
                    WriteUtf8ToBuffer(hProcess, address, value, buffer, byteCount);
                }
            }
        }

        /// <summary>
        /// Helper to write UTF-8 string to buffer
        /// </summary>
        private static void WriteUtf8ToBuffer(IntPtr hProcess, IntPtr address, string value, byte* buffer, int byteCount)
        {
            int encodedBytes = Encoding.UTF8.GetBytes(value, new Span<byte>(buffer, byteCount - 1));
            buffer[encodedBytes] = 0; // Null terminator

            WriteRaw(hProcess, address, buffer, encodedBytes + 1);
        }

        #endregion

        #region High-Level Read Methods with Address Resolution

        /// <summary>
        /// Reads a value from memory using CalculatedAddress with offsets
        /// </summary>
        /// <typeparam name="T">Unmanaged type to read</typeparam>
        /// <param name="calcAddr">Calculated address object</param>
        /// <param name="offsets">Optional offsets to apply</param>
        /// <returns>Read value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(CalculatedAddress calcAddr, params int[] offsets) where T : unmanaged
        {
            IntPtr resolvedAddress = ResolveAddress(calcAddr, offsets);
            return ReadDirect<T>(calcAddr.Process.Handle, resolvedAddress);
        }

        /// <summary>
        /// Reads a value from memory using base address and offsets
        /// </summary>
        /// <typeparam name="T">Unmanaged type to read</typeparam>
        /// <param name="hProcess">Process handle</param>
        /// <param name="baseAddress">Base memory address</param>
        /// <param name="offsets">Offsets to apply</param>
        /// <returns>Read value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(IntPtr hProcess, IntPtr baseAddress, params int[] offsets) where T : unmanaged
        {
            IntPtr resolvedAddress = AddOffsets(baseAddress, offsets);
            return ReadDirect<T>(hProcess, resolvedAddress);
        }

        /// <summary>
        /// Reads a string from memory using CalculatedAddress with offsets
        /// </summary>
        /// <param name="calcAddr">Calculated address object</param>
        /// <param name="maxLength">Maximum string length</param>
        /// <param name="unicode">True for UTF-16, false for UTF-8</param>
        /// <param name="offsets">Optional offsets to apply</param>
        /// <returns>Read string</returns>
        public static string ReadString(CalculatedAddress calcAddr, int maxLength = 256, bool unicode = false, params int[] offsets)
        {
            IntPtr resolvedAddress = ResolveAddress(calcAddr, offsets);
            return ReadStringDirect(calcAddr.Process.Handle, resolvedAddress, maxLength, unicode);
        }

        /// <summary>
        /// Reads a string from memory using base address and offsets
        /// </summary>
        /// <param name="hProcess">Process handle</param>
        /// <param name="baseAddress">Base memory address</param>
        /// <param name="maxLength">Maximum string length</param>
        /// <param name="unicode">True for UTF-16, false for UTF-8</param>
        /// <param name="offsets">Offsets to apply</param>
        /// <returns>Read string</returns>
        public static string ReadString(IntPtr hProcess, IntPtr baseAddress, int maxLength = 256, bool unicode = false, params int[] offsets)
        {
            IntPtr resolvedAddress = AddOffsets(baseAddress, offsets);
            return ReadStringDirect(hProcess, resolvedAddress, maxLength, unicode);
        }

        #endregion

        #region High-Level Write Methods with Address Resolution

        /// <summary>
        /// Writes a value to memory using CalculatedAddress with offsets
        /// </summary>
        /// <typeparam name="T">Unmanaged type to write</typeparam>
        /// <param name="calcAddr">Calculated address object</param>
        /// <param name="value">Value to write</param>
        /// <param name="offsets">Optional offsets to apply</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(CalculatedAddress calcAddr, T value, params int[] offsets) where T : unmanaged
        {
            IntPtr resolvedAddress = ResolveAddress(calcAddr, offsets);
            WriteDirect(calcAddr.Process.Handle, resolvedAddress, value);
        }

        /// <summary>
        /// Writes a value to memory using base address and offsets
        /// </summary>
        /// <typeparam name="T">Unmanaged type to write</typeparam>
        /// <param name="hProcess">Process handle</param>
        /// <param name="baseAddress">Base memory address</param>
        /// <param name="value">Value to write</param>
        /// <param name="offsets">Offsets to apply</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<[DynamicallyAccessedMembers( DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(IntPtr hProcess, IntPtr baseAddress, T value, params int[] offsets) where T : unmanaged
        {
            IntPtr resolvedAddress = AddOffsets(baseAddress, offsets);
            WriteDirect(hProcess, resolvedAddress, value);
        }

        /// <summary>
        /// Writes a string to memory using CalculatedAddress with offsets
        /// </summary>
        /// <param name="calcAddr">Calculated address object</param>
        /// <param name="value">String value to write</param>
        /// <param name="unicode">True for UTF-16, false for UTF-8</param>
        /// <param name="offsets">Optional offsets to apply</param>
        public static void WriteString(CalculatedAddress calcAddr, string value, bool unicode = false, params int[] offsets)
        {
            IntPtr resolvedAddress = ResolveAddress(calcAddr, offsets);
            WriteStringDirect(calcAddr.Process.Handle, resolvedAddress, value, unicode);
        }

        /// <summary>
        /// Writes a string to memory using base address and offsets
        /// </summary>
        /// <param name="hProcess">Process handle</param>
        /// <param name="baseAddress">Base memory address</param>
        /// <param name="value">String value to write</param>
        /// <param name="unicode">True for UTF-16, false for UTF-8</param>
        /// <param name="offsets">Offsets to apply</param>
        public static void WriteString(IntPtr hProcess, IntPtr baseAddress, string value, bool unicode = false, params int[] offsets)
        {
            IntPtr resolvedAddress = AddOffsets(baseAddress, offsets);
            WriteStringDirect(hProcess, resolvedAddress, value, unicode);
        }

        #endregion

        #region Low-Level Raw Operations with Retry Logic

        /// <summary>
        /// Reads from memory with retry logic for improved reliability
        /// </summary>
        /// <typeparam name="T">Unmanaged type to read</typeparam>
        /// <param name="hProcess">Process handle</param>
        /// <param name="address">Memory address</param>
        /// <returns>Read value</returns>
        private static T ReadWithRetry<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                                        DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(IntPtr hProcess, IntPtr address) where T : unmanaged
        {
            T value = default;
            int size = sizeof(T);

            for (int retry = 0; retry < MAX_RETRY_COUNT; retry++)
            {
                try
                {
                    ReadRaw(hProcess, address, &value, size);
                    return value;
                }
                catch when (retry < MAX_RETRY_COUNT - 1)
                {
                    // Brief delay before retry
                    Thread.Sleep(1);
                }
            }

            // Final attempt without catch
            ReadRaw(hProcess, address, &value, size);
            return value;
        }

        /// <summary>
        /// Writes to memory with retry logic for improved reliability
        /// </summary>
        /// <typeparam name="T">Unmanaged type to write</typeparam>
        /// <param name="hProcess">Process handle</param>
        /// <param name="address">Memory address</param>
        /// <param name="value">Value to write</param>
        private static void WriteWithRetry<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                                           DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(IntPtr hProcess, IntPtr address, T value) where T : unmanaged
        {
            int size = sizeof(T);

            for (int retry = 0; retry < MAX_RETRY_COUNT; retry++)
            {
                try
                {
                    WriteRaw(hProcess, address, &value, size);
                    return;
                }
                catch when (retry < MAX_RETRY_COUNT - 1)
                {
                    // Brief delay before retry
                    Thread.Sleep(1);
                }
            }

            // Final attempt without catch
            WriteRaw(hProcess, address, &value, size);
        }

        /// <summary>
        /// Low-level memory read with error handling
        /// </summary>
        /// <param name="hProcess">Process handle</param>
        /// <param name="address">Memory address</param>
        /// <param name="buffer">Buffer to read into</param>
        /// <param name="size">Number of bytes to read</param>
        private static void ReadRaw(IntPtr hProcess, IntPtr address, void* buffer, int size)
        {
            if (!ReadProcessMemory(hProcess, address, buffer, size, out int bytesRead))
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(errorCode,
                    $"ReadProcessMemory failed at 0x{address:X} (size: {size})");
            }

            if (bytesRead != size)
            {
                throw new InvalidOperationException(
                    $"Partial read at 0x{address:X}: expected {size} bytes, got {bytesRead}");
            }
        }

        /// <summary>
        /// Low-level memory write with protection handling and cache flushing
        /// </summary>
        /// <param name="hProcess">Process handle</param>
        /// <param name="address">Memory address</param>
        /// <param name="buffer">Buffer to write from</param>
        /// <param name="size">Number of bytes to write</param>
        private static void WriteRaw(IntPtr hProcess, IntPtr address, void* buffer, int size)
        {
            uint oldProtect;

            // Change memory protection
            if (!VirtualProtectEx(hProcess, address, (uint)size, PAGE_EXECUTE_READWRITE, out oldProtect))
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(errorCode,
                    $"VirtualProtectEx failed at 0x{address:X}");
            }

            try
            {
                // Write memory
                if (!WriteProcessMemory(hProcess, address, buffer, size, out int bytesWritten))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new System.ComponentModel.Win32Exception(errorCode,
                        $"WriteProcessMemory failed at 0x{address:X}");
                }

                if (bytesWritten != size)
                {
                    throw new InvalidOperationException(
                        $"Partial write at 0x{address:X}: expected {size} bytes, wrote {bytesWritten}");
                }

                // Flush instruction cache for executable memory
                if ((oldProtect & (PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE)) != 0)
                {
                    FlushInstructionCache(hProcess, address, (uint)size);
                }
            }
            finally
            {
                // Restore original protection
                VirtualProtectEx(hProcess, address, (uint)size, oldProtect, out _);
            }
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Forces immediate cleanup of internal resources
        /// </summary>
        public static void ForceCleanup()
        {
            lock (_lockObject)
            {
                TriggerCleanup();
            }
        }

        /// <summary>
        /// Gets the current number of active memory operations
        /// </summary>
        /// <returns>Number of active operations</returns>
        public static int GetActiveOperationCount()
        {
            return _activeOperations;
        }

        /// <summary>
        /// Waits for all active operations to complete
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <returns>True if all operations completed within timeout</returns>
        public static bool WaitForOperationsToComplete(int timeoutMs = 5000)
        {
            int elapsed = 0;
            const int sleepInterval = 10;

            while (_activeOperations > 0 && elapsed < timeoutMs)
            {
                Thread.Sleep(sleepInterval);
                elapsed += sleepInterval;
            }

            return _activeOperations == 0;
        }

        #endregion
    }
}