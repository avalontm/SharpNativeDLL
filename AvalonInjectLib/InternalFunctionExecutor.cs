using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AvalonInjectLib
{
    public static class InternalFunctionExecutor
    {
        private static readonly bool Is64Bit = IntPtr.Size == 8;

        /// <summary>
        /// Calls a function at the specified address with the given parameters
        /// Optimized for high-frequency calls (every 10ms)
        /// </summary>
        /// <param name="functionAddress">Base address of the function to call</param>
        /// <param name="parameters">Parameters to pass to the function</param>
        /// <returns>True if successful</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CallFunction(IntPtr functionAddress, params object[] parameters)
        {
            if (functionAddress == IntPtr.Zero)
                return false;

            try
            {
                return Is64Bit ?
                    CallFunctionX64(functionAddress, parameters) :
                    CallFunctionX86(functionAddress, parameters);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Optimized overload for specific parameter counts (most common scenarios)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CallFunction(IntPtr functionAddress) =>
            CallFunction(functionAddress, Array.Empty<object>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CallFunction(IntPtr functionAddress, object param1) =>
            CallFunction(functionAddress, new[] { param1 });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CallFunction(IntPtr functionAddress, object param1, object param2) =>
            CallFunction(functionAddress, new[] { param1, param2 });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CallFunction(IntPtr functionAddress, object param1, object param2, object param3) =>
            CallFunction(functionAddress, new[] { param1, param2, param3 });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CallFunction(IntPtr functionAddress, object param1, object param2, object param3, object param4) =>
          CallFunction(functionAddress, new[] { param1, param2, param3, param4 });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool CallFunctionX64(IntPtr functionAddress, object[] parameters)
        {
            switch (parameters.Length)
            {
                case 0:
                    ((delegate*<void>)functionAddress)();
                    return true;

                case 1:
                    var p1 = ConvertToIntPtr(parameters[0]);
                    ((delegate*<IntPtr, void>)functionAddress)(p1);
                    return true;

                case 2:
                    var p1_2 = ConvertToIntPtr(parameters[0]);
                    var p2_2 = ConvertToIntPtr(parameters[1]);
                    ((delegate*<IntPtr, IntPtr, void>)functionAddress)(p1_2, p2_2);
                    return true;

                case 3:
                    var p1_3 = ConvertToIntPtr(parameters[0]);
                    var p2_3 = ConvertToIntPtr(parameters[1]);
                    var p3_3 = ConvertToIntPtr(parameters[2]);
                    ((delegate*<IntPtr, IntPtr, IntPtr, void>)functionAddress)(p1_3, p2_3, p3_3);
                    return true;

                case 4:
                    var p1_4 = ConvertToIntPtr(parameters[0]);
                    var p2_4 = ConvertToIntPtr(parameters[1]);
                    var p3_4 = ConvertToIntPtr(parameters[2]);
                    var p4_4 = ConvertToIntPtr(parameters[3]);
                    ((delegate*<IntPtr, IntPtr, IntPtr, IntPtr, void>)functionAddress)(p1_4, p2_4, p3_4, p4_4);
                    return true;

                case 5:
                    var p1_5 = ConvertToIntPtr(parameters[0]);
                    var p2_5 = ConvertToIntPtr(parameters[1]);
                    var p3_5 = ConvertToIntPtr(parameters[2]);
                    var p4_5 = ConvertToIntPtr(parameters[3]);
                    var p5_5 = ConvertToIntPtr(parameters[4]);
                    ((delegate*<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void>)functionAddress)(p1_5, p2_5, p3_5, p4_5, p5_5);
                    return true;

                default:
                    // For more than 4 parameters, use general approach
                    return CallFunctionGeneral(functionAddress, parameters, true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool CallFunctionX86(IntPtr functionAddress, object[] parameters)
        {
            switch (parameters.Length)
            {
                case 0:
                    ((delegate* unmanaged[Stdcall]<void>)functionAddress)();
                    return true;

                case 1:
                    var p1 = ConvertToIntPtr(parameters[0]);
                    ((delegate* unmanaged[Stdcall]<IntPtr, void>)functionAddress)(p1);
                    return true;

                case 2:
                    var p1_2 = ConvertToIntPtr(parameters[0]);
                    var p2_2 = ConvertToIntPtr(parameters[1]);
                    ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, void>)functionAddress)(p1_2, p2_2);
                    return true;

                case 3:
                    var p1_3 = ConvertToIntPtr(parameters[0]);
                    var p2_3 = ConvertToIntPtr(parameters[1]);
                    var p3_3 = ConvertToIntPtr(parameters[2]);
                    ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, void>)functionAddress)(p1_3, p2_3, p3_3);
                    return true;

                case 4:
                    var p1_4 = ConvertToIntPtr(parameters[0]);
                    var p2_4 = ConvertToIntPtr(parameters[1]);
                    var p3_4 = ConvertToIntPtr(parameters[2]);
                    var p4_4 = ConvertToIntPtr(parameters[3]);
                    ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, IntPtr, void>)functionAddress)(p1_4, p2_4, p3_4, p4_4);
                    return true;

                case 5:
                    var p1_5 = ConvertToIntPtr(parameters[0]);
                    var p2_5 = ConvertToIntPtr(parameters[1]);
                    var p3_5 = ConvertToIntPtr(parameters[2]);
                    var p4_5 = ConvertToIntPtr(parameters[3]);
                    var p5_5 = ConvertToIntPtr(parameters[4]);
                    ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void>)functionAddress)(p1_5, p2_5, p3_5, p4_5, p5_5);
                    return true;

                default:
                    return CallFunctionGeneral(functionAddress, parameters, false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IntPtr ConvertToIntPtr(object value)
        {
            if (value == null) return IntPtr.Zero;

            return value switch
            {
                IntPtr ptr => ptr,
                int i => new IntPtr(i),
                uint ui => new IntPtr(ui),
                long l => new IntPtr(l),
                ulong ul => new IntPtr((long)ul),
                float f => new IntPtr(BitConverter.ToInt32(BitConverter.GetBytes(f), 0)),
                double d when Is64Bit => new IntPtr(BitConverter.ToInt64(BitConverter.GetBytes(d), 0)),
                double d => new IntPtr(BitConverter.ToInt32(BitConverter.GetBytes((float)d), 0)),
                bool b => new IntPtr(b ? 1 : 0),
                byte bt => new IntPtr(bt),
                short s => new IntPtr(s),
                ushort us => new IntPtr(us),
                string str => Marshal.StringToHGlobalAnsi(str ?? string.Empty),
                _ => new IntPtr(Convert.ToInt32(value))
            };
        }

        // Fallback for more than 4 parameters using dynamic shellcode
        private static unsafe bool CallFunctionGeneral(IntPtr functionAddress, object[] parameters, bool isX64)
        {
            if (parameters.Length > 8) // Reasonable limit for performance
                return false;

            // Allocate executable memory
            var shellcodeSize = isX64 ? 64 + (parameters.Length * 16) : 32 + (parameters.Length * 8);
            var execMemory = VirtualAlloc(IntPtr.Zero, (uint)shellcodeSize, 0x3000, 0x40);

            if (execMemory == IntPtr.Zero)
                return false;

            try
            {
                var shellcode = CreateMinimalShellcode(functionAddress, parameters, isX64);
                Marshal.Copy(shellcode, 0, execMemory, shellcode.Length);

                // Execute
                ((delegate*<void>)execMemory)();
                return true;
            }
            finally
            {
                VirtualFree(execMemory, 0, 0x8000);
            }
        }

        private static byte[] CreateMinimalShellcode(IntPtr functionAddress, object[] parameters, bool isX64)
        {
            if (isX64)
                return CreateX64Shellcode(functionAddress, parameters);
            else
                return CreateX86Shellcode(functionAddress, parameters);
        }

        private static byte[] CreateX64Shellcode(IntPtr functionAddress, object[] parameters)
        {
            var shellcode = new List<byte>();

            // Prologue: sub rsp, 40 (shadow space + alignment)
            shellcode.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x28 });

            // Load parameters into registers (RCX, RDX, R8, R9)
            var registers = new byte[][] {
                new byte[] { 0x48, 0xB9 }, // mov rcx
                new byte[] { 0x48, 0xBA }, // mov rdx  
                new byte[] { 0x49, 0xB8 }, // mov r8
                new byte[] { 0x49, 0xB9 }  // mov r9
            };

            for (int i = 0; i < Math.Min(parameters.Length, 4); i++)
            {
                shellcode.AddRange(registers[i]);
                var value = ConvertToIntPtr(parameters[i]).ToInt64();
                shellcode.AddRange(BitConverter.GetBytes(value));
            }

            // Call function: mov rax, addr; call rax
            shellcode.AddRange(new byte[] { 0x48, 0xB8 });
            shellcode.AddRange(BitConverter.GetBytes(functionAddress.ToInt64()));
            shellcode.AddRange(new byte[] { 0xFF, 0xD0 });

            // Epilogue: add rsp, 40; ret
            shellcode.AddRange(new byte[] { 0x48, 0x83, 0xC4, 0x28, 0xC3 });

            return shellcode.ToArray();
        }

        private static byte[] CreateX86Shellcode(IntPtr functionAddress, object[] parameters)
        {
            var shellcode = new List<byte>();

            // Prologue
            shellcode.AddRange(new byte[] { 0x55, 0x89, 0xE5 }); // push ebp; mov ebp, esp

            // Push parameters in reverse order
            for (int i = parameters.Length - 1; i >= 0; i--)
            {
                shellcode.Add(0x68); // push immediate
                var value = ConvertToIntPtr(parameters[i]).ToInt32();
                shellcode.AddRange(BitConverter.GetBytes(value));
            }

            // Call function: mov eax, addr; call eax
            shellcode.Add(0xB8);
            shellcode.AddRange(BitConverter.GetBytes(functionAddress.ToInt32()));
            shellcode.AddRange(new byte[] { 0xFF, 0xD0 });

            // Clean stack if stdcall
            if (parameters.Length > 0)
            {
                shellcode.AddRange(new byte[] { 0x83, 0xC4, (byte)(parameters.Length * 4) });
            }

            // Epilogue
            shellcode.AddRange(new byte[] { 0x89, 0xEC, 0x5D, 0xC3 }); // mov esp, ebp; pop ebp; ret

            return shellcode.ToArray();
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualFree(IntPtr lpAddress, uint dwSize, uint dwFreeType);
    }
}