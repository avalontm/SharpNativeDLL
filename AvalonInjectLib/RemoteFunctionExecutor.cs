using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AvalonInjectLib
{
    public class RemoteFunctionException : Exception
    {
        public string Operation { get; }
        public int? ErrorCode { get; }

        public RemoteFunctionException(string operation, string message) : base(message)
        {
            Operation = operation;
        }

        public RemoteFunctionException(string operation, string message, int errorCode) : base(message)
        {
            Operation = operation;
            ErrorCode = errorCode;
        }

        public RemoteFunctionException(string operation, string message, Exception innerException) : base(message, innerException)
        {
            Operation = operation;
        }
    }

    public static class RemoteFunctionExecutor
    {
        private static readonly bool Is64Bit = IntPtr.Size == 8;
        public static int TimeoutMs { get; set; } = 5000;

        // APIs de Windows para inyección de código
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetLastError();

        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        private const uint MEM_RELEASE = 0x8000;
        private const uint WAIT_OBJECT_0 = 0x00000000;
        private const uint INFINITE = 0xFFFFFFFF;

        public enum ParamType : byte
        {
            Int32 = 0,
            IntPtr = 1,
            String = 2,
            Float = 3,
            Double = 4,
            Bool = 5,
            Byte = 6,
            Int16 = 7,
            Int64 = 8,
            UInt32 = 9,
            Single = 10
        }

        public struct Parameter
        {
            public ParamType Type;
            public object Value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Parameter(ParamType type, object value)
            {
                Type = type;
                Value = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter Int(int value) => new(ParamType.Int32, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter Ptr(IntPtr value) => new(ParamType.IntPtr, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter Str(string value) => new(ParamType.String, value ?? "");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter Float(float value) => new(ParamType.Float, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter Single(float value) => new(ParamType.Single, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter Double(double value) => new(ParamType.Double, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter Bool(bool value) => new(ParamType.Bool, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter Byte(byte value) => new(ParamType.Byte, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter Short(short value) => new(ParamType.Int16, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter Long(long value) => new(ParamType.Int64, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter UInt(uint value) => new(ParamType.UInt32, value);

        /// <summary>
        /// Ejecuta una función remota o local con parámetros tipados
        /// </summary>
        public static bool CallFunction(IntPtr hProcess, IntPtr functionAddr, params Parameter[] parameters)
        {
            try
            {
                if (functionAddr == IntPtr.Zero)
                    throw new RemoteFunctionException("Validation", "Dirección de función inválida (IntPtr.Zero)");

                // Determinar si es ejecución local o remota
                IntPtr currentProcess = AvalonEngine.Instance.Process.Handle;
                bool isLocalExecution = hProcess == IntPtr.Zero || hProcess == currentProcess;

                if (isLocalExecution)
                {
                    return ExecuteFunctionDirect(functionAddr, parameters);
                }
                else
                {
                    return ExecuteFunctionRemote(hProcess, functionAddr, parameters);
                }
            }
            catch (RemoteFunctionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RemoteFunctionException("CallFunction", "Error inesperado en CallFunction", ex);
            }
        }

        /// <summary>
        /// Ejecuta una función con conversión automática de parámetros
        /// </summary>
        public static bool CallFunction(IntPtr hProcess, IntPtr functionAddr, params object[] parameters)
        {
            try
            {
                if (functionAddr == IntPtr.Zero)
                    throw new RemoteFunctionException("Validation", "Dirección de función inválida (IntPtr.Zero)");

                if (parameters == null || parameters.Length == 0)
                    return CallFunction(hProcess, functionAddr, Array.Empty<Parameter>());

                // Convertir objetos a parámetros tipados
                var typedParams = new Parameter[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    try
                    {
                        typedParams[i] = ConvertToParameter(parameters[i]);
                    }
                    catch (Exception ex)
                    {
                        throw new RemoteFunctionException("ParameterConversion",
                            $"Error convirtiendo parámetro {i} (tipo: {parameters[i]?.GetType()?.Name ?? "null"})", ex);
                    }
                }

                return CallFunction(hProcess, functionAddr, typedParams);
            }
            catch (RemoteFunctionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RemoteFunctionException("CallFunction", "Error inesperado en CallFunction con conversión automática", ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Parameter ConvertToParameter(object value)
        {
            return value switch
            {
                int intVal => Int(intVal),
                float floatVal => Float(floatVal),
                double doubleVal => Double(doubleVal),
                bool boolVal => Bool(boolVal),
                byte byteVal => Byte(byteVal),
                short shortVal => Short(shortVal),
                long longVal => Long(longVal),
                uint uintVal => UInt(uintVal),
                IntPtr ptrVal => Ptr(ptrVal),
                string strVal => Str(strVal),
                null => Str(""),
                _ => throw new ArgumentException($"Tipo no soportado: {value.GetType().Name}")
            };
        }

        // Ejecución remota usando inyección de código
        private static bool ExecuteFunctionRemote(IntPtr hProcess, IntPtr functionAddr, Parameter[] parameters)
        {
            try
            {
                byte[] shellcode = CreateShellcode(functionAddr, parameters);

                IntPtr remoteMemory = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)shellcode.Length,
                    MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

                if (remoteMemory == IntPtr.Zero)
                {
                    uint error = GetLastError();
                    throw new RemoteFunctionException("MemoryAllocation", $"No se pudo alocar memoria remota. Error: {error}", (int)error);
                }

                try
                {
                    if (!WriteProcessMemory(hProcess, remoteMemory, shellcode, (uint)shellcode.Length, out uint bytesWritten))
                    {
                        uint error = GetLastError();
                        throw new RemoteFunctionException("WriteMemory", $"Error escribiendo memoria remota. Error: {error}", (int)error);
                    }

                    IntPtr remoteThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, remoteMemory, IntPtr.Zero, 0, out uint threadId);
                    if (remoteThread == IntPtr.Zero)
                    {
                        uint error = GetLastError();
                        throw new RemoteFunctionException("ThreadCreation", $"Error creando thread remoto. Error: {error}", (int)error);
                    }

                    try
                    {
                        uint waitResult = WaitForSingleObject(remoteThread, (uint)TimeoutMs);
                        return waitResult == WAIT_OBJECT_0;
                    }
                    finally
                    {
                        CloseHandle(remoteThread);
                    }
                }
                finally
                {
                    VirtualFreeEx(hProcess, remoteMemory, 0, MEM_RELEASE);
                }
            }
            catch (RemoteFunctionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RemoteFunctionException("ExecuteFunctionRemote", "Error en ejecución remota", ex);
            }
        }

        // Ejecución local directa sin delegates
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool ExecuteFunctionDirect(IntPtr functionAddr, Parameter[] parameters)
        {
            try
            {
                if (Is64Bit)
                {
                    return ExecuteX64Direct(functionAddr, parameters);
                }
                else
                {
                    return ExecuteX86Direct(functionAddr, parameters);
                }
            }
            catch (Exception ex)
            {
                throw new RemoteFunctionException("ExecuteFunctionDirect", "Error en ejecución directa", ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool ExecuteX64Direct(IntPtr functionAddr, Parameter[] parameters)
        {
            // Implementación para x64 usando calli
            // Nota: Esto requiere unsafe context y es altamente dependiente de la arquitectura
            switch (parameters.Length)
            {
                case 0:
                    ((delegate*<void>)functionAddr)();
                    return true;
                case 1:
                    var p1 = GetParameterValue(parameters[0]);
                    ((delegate*<IntPtr, void>)functionAddr)(p1);
                    return true;
                case 2:
                    var p1_2 = GetParameterValue(parameters[0]);
                    var p2_2 = GetParameterValue(parameters[1]);
                    ((delegate*<IntPtr, IntPtr, void>)functionAddr)(p1_2, p2_2);
                    return true;
                // Agregar más casos según sea necesario
                default:
                    throw new RemoteFunctionException("ExecuteX64Direct", $"Demasiados parámetros: {parameters.Length} (máximo 2 en esta implementación)");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool ExecuteX86Direct(IntPtr functionAddr, Parameter[] parameters)
        {
            // Implementación para x86 usando calli
            switch (parameters.Length)
            {
                case 0:
                    ((delegate* unmanaged[Stdcall]<void>)functionAddr)();
                    return true;
                case 1:
                    var p1 = GetParameterValue(parameters[0]);
                    ((delegate* unmanaged[Stdcall]<IntPtr, void>)functionAddr)(p1);
                    return true;
                case 2:
                    var p1_2 = GetParameterValue(parameters[0]);
                    var p2_2 = GetParameterValue(parameters[1]);
                    ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, void>)functionAddr)(p1_2, p2_2);
                    return true;
                // Agregar más casos según sea necesario
                default:
                    throw new RemoteFunctionException("ExecuteX86Direct", $"Demasiados parámetros: {parameters.Length} (máximo 2 en esta implementación)");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IntPtr GetParameterValue(Parameter param)
        {
            return param.Type switch
            {
                ParamType.Int32 or ParamType.UInt32 or ParamType.Bool or ParamType.Byte or ParamType.Int16 =>
                    (IntPtr)Convert.ToInt32(param.Value),
                ParamType.Int64 =>
                    (IntPtr)Convert.ToInt64(param.Value),
                ParamType.Float or ParamType.Single =>
                    (IntPtr)BitConverter.ToInt32(BitConverter.GetBytes((float)param.Value), 0),
                ParamType.Double =>
                    (IntPtr)BitConverter.ToInt64(BitConverter.GetBytes((double)param.Value), 0),
                ParamType.IntPtr =>
                    (IntPtr)param.Value,
                ParamType.String =>
                    Marshal.StringToHGlobalAnsi((string)param.Value), // Cuidado con la gestión de memoria!
                _ => IntPtr.Zero
            };
        }

        private static byte[] CreateShellcode(IntPtr functionAddr, Parameter[] parameters)
        {
            if (Is64Bit)
            {
                return CreateX64Shellcode(functionAddr, parameters);
            }
            else
            {
                return CreateX86Shellcode(functionAddr, parameters);
            }
        }

        private static byte[] CreateX64Shellcode(IntPtr functionAddr, Parameter[] parameters)
        {
            List<byte> code = new List<byte>();

            // Prólogo: salvar registros
            code.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x28 }); // sub rsp, 40 (shadow space)

            // Cargar parámetros en registros y stack
            for (int i = 0; i < parameters.Length; i++)
            {
                LoadParameterX64(code, parameters[i], i);
            }

            // Llamar función
            code.AddRange(new byte[] { 0x48, 0xB8 }); // mov rax, immediate64
            code.AddRange(BitConverter.GetBytes(functionAddr.ToInt64()));
            code.AddRange(new byte[] { 0xFF, 0xD0 }); // call rax

            // Epílogo
            code.AddRange(new byte[] { 0x48, 0x83, 0xC4, 0x28 }); // add rsp, 40
            code.Add(0xC3); // ret

            return code.ToArray();
        }

        private static byte[] CreateX86Shellcode(IntPtr functionAddr, Parameter[] parameters)
        {
            List<byte> code = new List<byte>();

            // Prólogo
            code.AddRange(new byte[] { 0x55 }); // push ebp
            code.AddRange(new byte[] { 0x89, 0xE5 }); // mov ebp, esp

            // Push parámetros en orden reverso (stdcall convention)
            for (int i = parameters.Length - 1; i >= 0; i--)
            {
                LoadParameterX86(code, parameters[i]);
            }

            // Llamar función
            code.AddRange(new byte[] { 0xB8 }); // mov eax, immediate32
            code.AddRange(BitConverter.GetBytes(functionAddr.ToInt32()));
            code.AddRange(new byte[] { 0xFF, 0xD0 }); // call eax

            // Limpiar stack si es stdcall
            if (parameters.Length > 0)
            {
                code.AddRange(new byte[] { 0x83, 0xC4, (byte)(parameters.Length * 4) }); // add esp, paramCount*4
            }

            // Epílogo
            code.AddRange(new byte[] { 0x89, 0xEC }); // mov esp, ebp
            code.AddRange(new byte[] { 0x5D }); // pop ebp
            code.Add(0xC3); // ret

            return code.ToArray();
        }

        private static void LoadParameterX64(List<byte> code, Parameter param, int index)
        {
            byte[] registerCodes = { 0xB9, 0xBA, 0x41, 0x41 }; // mov para RCX, RDX, R8, R9
            byte[] registerExtensions = { 0x00, 0x00, 0xB8, 0xB9 }; // extensiones para R8, R9

            if (index < 4)
            {
                var value = GetParameterBytes(param);
                if (index >= 2) code.Add(registerExtensions[index]); // prefijo REX para R8/R9
                code.Add(registerCodes[index]);
                code.AddRange(value.Take(4)); // primeros 4 bytes
            }
            else
            {
                // Implementar push a stack para parámetros adicionales
            }
        }

        private static void LoadParameterX86(List<byte> code, Parameter param)
        {
            var value = GetParameterBytes(param);
            code.Add(0x68); // push immediate32
            code.AddRange(value.Take(4));
        }

        private static byte[] GetParameterBytes(Parameter param)
        {
            return param.Type switch
            {
                ParamType.Int32 => BitConverter.GetBytes((int)param.Value),
                ParamType.UInt32 => BitConverter.GetBytes((uint)param.Value),
                ParamType.Float or ParamType.Single => BitConverter.GetBytes((float)param.Value),
                ParamType.Double => BitConverter.GetBytes((double)param.Value),
                ParamType.Int64 => BitConverter.GetBytes((long)param.Value),
                ParamType.IntPtr => Is64Bit ?
                    BitConverter.GetBytes(((IntPtr)param.Value).ToInt64()) :
                    BitConverter.GetBytes(((IntPtr)param.Value).ToInt32()),
                _ => BitConverter.GetBytes(0)
            };
        }
    }
}