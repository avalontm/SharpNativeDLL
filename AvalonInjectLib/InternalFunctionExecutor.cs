using AvalonInjectLib.Exteptions;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AvalonInjectLib
{
    public static class InternalFunctionExecutor
    {
        private static readonly bool Is64Bit = IntPtr.Size == 8;

        // Cache para shellcode reutilizable
        private static readonly ConcurrentDictionary<string, byte[]> ShellcodeCache = new();

        // Pool de arrays para reutilización
        private static readonly ArrayPool<byte> ByteArrayPool = ArrayPool<byte>.Shared;
        private static readonly ArrayPool<Parameter> ParameterArrayPool = ArrayPool<Parameter>.Shared;

        // Cache de arrays de bytes comúnes (0, 1, 2, 3, 4 parámetros)
        private static readonly byte[][] CachedPrologueX64 = new byte[5][];
        private static readonly byte[][] CachedPrologueX86 = new byte[5][];

        // Buffers pre-calculados para operaciones comunes
        private static readonly byte[] MovRaxBytes = { 0x48, 0xB8 };
        private static readonly byte[] CallRaxBytes = { 0xFF, 0xD0 };
        private static readonly byte[] EpilogueX64Bytes = { 0x48, 0x83, 0xC4, 0x28, 0xC3 };
        private static readonly byte[] PrologueX86Bytes = { 0x55, 0x89, 0xE5 };
        private static readonly byte[] EpilogueX86Bytes = { 0x89, 0xEC, 0x5D, 0xC3 };

        // Cache para conversión de parámetros
        private static readonly ThreadLocal<Dictionary<Type, ParamType>> TypeMappingCache =
            new(() => new Dictionary<Type, ParamType>
            {
                [typeof(int)] = ParamType.Int32,
                [typeof(float)] = ParamType.Float,
                [typeof(double)] = ParamType.Double,
                [typeof(bool)] = ParamType.Bool,
                [typeof(byte)] = ParamType.Byte,
                [typeof(short)] = ParamType.Int16,
                [typeof(long)] = ParamType.Int64,
                [typeof(uint)] = ParamType.UInt32,
                [typeof(IntPtr)] = ParamType.IntPtr,
                [typeof(string)] = ParamType.String
            });

        static InternalFunctionExecutor()
        {
            // Pre-calcular prólogos comunes para evitar allocaciones
            InitializeCachedPrologues();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InitializeCachedPrologues()
        {
            for (int i = 0; i < 5; i++)
            {
                // X64 prólogos con shadow space apropiado
                var shadowSpace = Math.Max(32, (i + 3) / 4 * 16); // Align to 16 bytes
                CachedPrologueX64[i] = new byte[] { 0x48, 0x83, 0xEC, (byte)shadowSpace };

                // X86 prólogos simples
                CachedPrologueX86[i] = (byte[])PrologueX86Bytes.Clone();
            }
        }

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

        // Factory methods optimizados (sin allocaciones adicionales)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter Int(int value) => new(ParamType.Int32, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter Ptr(IntPtr value) => new(ParamType.IntPtr, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Parameter Str(string value) => new(ParamType.String, value ?? string.Empty);

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
        /// Executes a function locally or in a remote process.
        /// If <paramref name="hProcess"/> is <c>IntPtr.Zero</c>, the function runs locally.
        /// </summary>
        /// <param name="hProcess">Target process handle, or <c>IntPtr.Zero</c> for local execution.</param>
        /// <param name="functionAddr">Pointer to the function to execute.</param>
        /// <param name="parameters">Typed parameters for the function.</param>
        /// <returns><c>true</c> if the function executed successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="RemoteFunctionException">Thrown on invalid address or execution failure.</exception>

        public static bool CallFunction(IntPtr functionAddr, params Parameter[] parameters)
        {
            try
            {
                if (functionAddr == IntPtr.Zero)
                    throw new RemoteFunctionException("Validation", "Dirección de función inválida (IntPtr.Zero)");

                return ExecuteFunctionDirect(functionAddr, parameters);
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
        /// Executes a function locally or in a remote process.
        /// If <paramref name="hProcess"/> is <c>IntPtr.Zero</c>, the function runs locally.
        /// </summary>
        /// <param name="hProcess">Target process handle, or <c>IntPtr.Zero</c> for local execution.</param>
        /// <param name="functionAddr">Pointer to the function to execute.</param>
        /// <param name="parameters">Typed parameters for the function.</param>
        /// <returns><c>true</c> if the function executed successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="RemoteFunctionException">Thrown on invalid address or execution failure.</exception>

        public static bool CallFunction(IntPtr functionAddr, params object[] parameters)
        {
            if (functionAddr == IntPtr.Zero)
                throw new RemoteFunctionException("Validation", "Dirección de función inválida (IntPtr.Zero)");

            if (parameters == null || parameters.Length == 0)
                return CallFunction(functionAddr, Array.Empty<Parameter>());

            // Usar pool de arrays para evitar allocaciones
            var typedParams = ParameterArrayPool.Rent(parameters.Length);
            try
            {
                // Convertir objetos a parámetros tipados sin allocaciones adicionales
                for (int i = 0; i < parameters.Length; i++)
                {
                    try
                    {
                        typedParams[i] = ConvertToParameterOptimized(parameters[i]);
                    }
                    catch (Exception ex)
                    {
                        throw new RemoteFunctionException("ParameterConversion",
                            $"Error convirtiendo parámetro {i} (tipo: {parameters[i]?.GetType()?.Name ?? "null"})", ex);
                    }
                }

                // Crear span para evitar crear nuevo array
                var paramSpan = new ReadOnlySpan<Parameter>(typedParams, 0, parameters.Length);
                return CallFunction(functionAddr, paramSpan.ToArray()); // Necesario para la firma actual
            }
            catch (RemoteFunctionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new RemoteFunctionException("CallFunction", "Error inesperado en CallFunction con conversión automática", ex);
            }
            finally
            {
                ParameterArrayPool.Return(typedParams, true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Parameter ConvertToParameterOptimized(object value)
        {
            if (value == null) return Str(string.Empty);

            var type = value.GetType();
            var typeMapping = TypeMappingCache.Value;

            if (typeMapping.TryGetValue(type, out var paramType))
            {
                return paramType switch
                {
                    ParamType.Int32 => Int((int)value),
                    ParamType.Float => Float((float)value),
                    ParamType.Double => Double((double)value),
                    ParamType.Bool => Bool((bool)value),
                    ParamType.Byte => Byte((byte)value),
                    ParamType.Int16 => Short((short)value),
                    ParamType.Int64 => Long((long)value),
                    ParamType.UInt32 => UInt((uint)value),
                    ParamType.IntPtr => Ptr((IntPtr)value),
                    ParamType.String => Str((string)value),
                    _ => throw new ArgumentException($"Tipo no soportado: {type.Name}")
                };
            }

            throw new ArgumentException($"Tipo no soportado: {type.Name}");
        }

        // Ejecución local directa sin delegates (sin cambios, ya estaba optimizada)
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
                default:
                    throw new RemoteFunctionException("ExecuteX64Direct", $"Demasiados parámetros: {parameters.Length} (máximo 2 en esta implementación)");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool ExecuteX86Direct(IntPtr functionAddr, Parameter[] parameters)
        {
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
                return CreateX64ShellcodeOptimized(functionAddr, parameters);
            }
            else
            {
                return CreateX86ShellcodeOptimized(functionAddr, parameters);
            }
        }

        private static byte[] CreateX64ShellcodeOptimized(IntPtr functionAddr, Parameter[] parameters)
        {
            int estimatedSize = 64 + (parameters.Length * 16); // Estimación más precisa
            byte[] buffer = ByteArrayPool.Rent(estimatedSize);
            int position = 0;

            try
            {
                // Usar prólogo pre-calculado
                var prologue = CachedPrologueX64[Math.Min(parameters.Length, 4)];
                prologue.CopyTo(buffer, position);
                position += prologue.Length;

                // Cargar parámetros
                for (int i = 0; i < parameters.Length; i++)
                {
                    position += LoadParameterX64Optimized(buffer.AsSpan(position), parameters[i], i);
                }

                // Llamar función (usar arrays pre-calculados)
                MovRaxBytes.CopyTo(buffer, position);
                position += MovRaxBytes.Length;

                BitConverter.GetBytes(functionAddr.ToInt64()).CopyTo(buffer, position);
                position += 8;

                CallRaxBytes.CopyTo(buffer, position);
                position += CallRaxBytes.Length;

                // Epílogo pre-calculado
                EpilogueX64Bytes.CopyTo(buffer, position);
                position += EpilogueX64Bytes.Length;

                // Crear array del tamaño exacto
                var result = new byte[position];
                buffer.AsSpan(0, position).CopyTo(result);
                return result;
            }
            finally
            {
                ByteArrayPool.Return(buffer);
            }
        }

        private static byte[] CreateX86ShellcodeOptimized(IntPtr functionAddr, Parameter[] parameters)
        {
            int estimatedSize = 32 + (parameters.Length * 8);
            byte[] buffer = ByteArrayPool.Rent(estimatedSize);
            int position = 0;

            try
            {
                // Prólogo pre-calculado
                PrologueX86Bytes.CopyTo(buffer, position);
                position += PrologueX86Bytes.Length;

                // Push parámetros en orden reverso
                for (int i = parameters.Length - 1; i >= 0; i--)
                {
                    position += LoadParameterX86Optimized(buffer.AsSpan(position), parameters[i]);
                }

                // Llamar función
                buffer[position++] = 0xB8; // mov eax, immediate32
                BitConverter.GetBytes(functionAddr.ToInt32()).CopyTo(buffer, position);
                position += 4;
                buffer[position++] = 0xFF; // call eax
                buffer[position++] = 0xD0;

                // Limpiar stack si es stdcall
                if (parameters.Length > 0)
                {
                    buffer[position++] = 0x83; // add esp, paramCount*4
                    buffer[position++] = 0xC4;
                    buffer[position++] = (byte)(parameters.Length * 4);
                }

                // Epílogo pre-calculado
                EpilogueX86Bytes.CopyTo(buffer, position);
                position += EpilogueX86Bytes.Length;

                var result = new byte[position];
                buffer.AsSpan(0, position).CopyTo(result);
                return result;
            }
            finally
            {
                ByteArrayPool.Return(buffer);
            }
        }

        private static int LoadParameterX64Optimized(Span<byte> buffer, Parameter param, int index)
        {
            ReadOnlySpan<byte> registerCodes = stackalloc byte[] { 0xB9, 0xBA, 0x41, 0x41 };
            ReadOnlySpan<byte> registerExtensions = stackalloc byte[] { 0x00, 0x00, 0xB8, 0xB9 };

            if (index < 4)
            {
                int position = 0;
                var value = GetParameterBytesSpan(param);

                if (index >= 2) buffer[position++] = registerExtensions[index];
                buffer[position++] = registerCodes[index];

                value.Slice(0, Math.Min(4, value.Length)).CopyTo(buffer.Slice(position));
                position += 4;

                return position;
            }
            else
            {
                // Stack parameters - implementar según sea necesario
                return 0;
            }
        }

        private static int LoadParameterX86Optimized(Span<byte> buffer, Parameter param)
        {
            var value = GetParameterBytesSpan(param);
            buffer[0] = 0x68; // push immediate32
            value.Slice(0, Math.Min(4, value.Length)).CopyTo(buffer.Slice(1));
            return 5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<byte> GetParameterBytesSpan(Parameter param)
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

        // Método para limpiar cache si es necesario
        public static void ClearCache()
        {
            ShellcodeCache.Clear();
        }

        // Método para obtener estadísticas de cache (útil para debugging)
        public static int GetCacheCount() => ShellcodeCache.Count;
    }
}