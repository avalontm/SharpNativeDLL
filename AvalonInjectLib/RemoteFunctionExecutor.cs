using System.Runtime.InteropServices;
using System.Text;

namespace AvalonInjectLib
{
    public static class RemoteFunctionExecutor
    {
        // Imports de Windows API
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out uint lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateThread(IntPtr hThread, uint dwExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void Sleep(uint dwMilliseconds);

        // Constantes
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint MEM_RELEASE = 0x8000;
        private const uint PAGE_READWRITE = 0x04;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        private const uint WAIT_OBJECT_0 = 0x00000000;
        private const uint WAIT_TIMEOUT = 0x00000102;
        private const uint INFINITE = 0xFFFFFFFF;
        private const uint STILL_ACTIVE = 259;
        private const uint THREAD_SUSPEND_RESUME = 0x0002;
        private const uint THREAD_TERMINATE = 0x0001;

        // Control de rate limiting y estado
        private static readonly object _lockObject = new object();
        private static DateTime _lastCall = DateTime.MinValue;
        private static int _currentCalls = 0;
        private static readonly Dictionary<IntPtr, DateTime> _processCallHistory = new Dictionary<IntPtr, DateTime>();

        // Configuración anti-freeze
        public static int MaxConcurrentCalls { get; set; } = 3;
        public static int MinDelayBetweenCalls { get; set; } = 10; // ms
        public static int MaxExecutionTimeout { get; set; } = 2000; // ms
        public static bool EnableRateLimiting { get; set; } = true;
        public static bool EnableTimeoutProtection { get; set; } = true;

        // Enum para tipos de parámetros
        public enum ParamType
        {
            Int32,
            Int64,
            Float,
            Double,
            String,
            Bool,
            Pointer,
            Byte,
            Short,
            UInt32,
            UInt64
        }

        // Estructura para parámetros
        public struct Parameter
        {
            public ParamType Type;
            public object Value;

            public Parameter(ParamType type, object value)
            {
                Type = type;
                Value = value;
            }
        }

        // Estructura para resultado de ejecución
        public struct ExecutionResult
        {
            public bool Success;
            public string ErrorMessage;
            public uint ExitCode;
            public bool TimedOut;
            public int ExecutionTime;
        }

        // Métodos de conveniencia para crear parámetros
        public static Parameter Int(int value) => new Parameter(ParamType.Int32, value);
        public static Parameter Long(long value) => new Parameter(ParamType.Int64, value);
        public static Parameter Float(float value) => new Parameter(ParamType.Float, value);
        public static Parameter Double(double value) => new Parameter(ParamType.Double, value);
        public static Parameter String(string value) => new Parameter(ParamType.String, value);
        public static Parameter Bool(bool value) => new Parameter(ParamType.Bool, value);
        public static Parameter Pointer(IntPtr value) => new Parameter(ParamType.Pointer, value);
        public static Parameter Byte(byte value) => new Parameter(ParamType.Byte, value);
        public static Parameter Short(short value) => new Parameter(ParamType.Short, value);
        public static Parameter UInt(uint value) => new Parameter(ParamType.UInt32, value);
        public static Parameter ULong(ulong value) => new Parameter(ParamType.UInt64, value);

        /// <summary>
        /// Ejecuta una función remota con protección anti-freeze
        /// </summary>
        public static ExecutionResult CallRemoteFunctionSafe(IntPtr hProcess, IntPtr functionAddress, params Parameter[] parameters)
        {
            lock (GlobalSync.MainLock)
            {
                var result = new ExecutionResult
                {
                    Success = false,
                    ErrorMessage = "",
                    ExitCode = 0,
                    TimedOut = false,
                    ExecutionTime = 0
                };

                if (hProcess == IntPtr.Zero || functionAddress == IntPtr.Zero)
                {
                    Logger.Error("Invalid process handle or function address", "RemoteFunctionExecutor");
                    result.ErrorMessage = "Invalid process handle or function address";
                    return result;
                }

                // Rate limiting
                if (EnableRateLimiting && !CheckRateLimit(hProcess))
                {
                    Logger.Warning("Rate limit exceeded for remote function call", "RemoteFunctionExecutor");
                    result.ErrorMessage = "Rate limit exceeded";
                    return result;
                }

                // Control de concurrencia
                if (!AcquireExecutionSlot())
                {
                    Logger.Warning("Maximum concurrent calls reached", "RemoteFunctionExecutor");
                    result.ErrorMessage = "Max concurrent calls reached";
                    return result;
                }

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    Logger.Debug($"Executing remote function at 0x{functionAddress:X} with {parameters.Length} parameters", "RemoteFunctionExecutor");
                    result = ExecuteWithTimeoutProtection(hProcess, functionAddress, parameters);
                    result.ExecutionTime = (int)stopwatch.ElapsedMilliseconds;

                    if (result.Success)
                    {
                        Logger.Debug($"Remote function executed successfully in {result.ExecutionTime}ms", "RemoteFunctionExecutor");
                    }
                    else
                    {
                        Logger.Error($"Remote function execution failed: {result.ErrorMessage}", "RemoteFunctionExecutor");
                    }

                    return result;
                }
                finally
                {
                    ReleaseExecutionSlot();
                    stopwatch.Stop();
                }
            }
        }

        /// <summary>
        /// Versión compatible con el método original
        /// </summary>
        public static bool CallRemoteFunction(IntPtr hProcess, IntPtr functionAddress, params Parameter[] parameters)
        {
            var result = CallRemoteFunctionSafe(hProcess, functionAddress, parameters);
            return result.Success;
        }

        /// <summary>
        /// Versión simplificada para objetos
        /// </summary>
        public static bool CallRemoteFunction(IntPtr hProcess, IntPtr functionAddress, params object[] parameters)
        {
            var convertedParams = parameters.Select(ConvertObjectToParameter).ToArray();
            return CallRemoteFunction(hProcess, functionAddress, convertedParams);
        }

        private static bool CheckRateLimit(IntPtr hProcess)
        {
            lock (_lockObject)
            {
                var now = DateTime.Now;

                // Verificar delay mínimo global
                if ((now - _lastCall).TotalMilliseconds < MinDelayBetweenCalls)
                {
                    Thread.Sleep(MinDelayBetweenCalls - (int)(now - _lastCall).TotalMilliseconds);
                }

                // Verificar delay por proceso
                if (_processCallHistory.ContainsKey(hProcess))
                {
                    var lastProcessCall = _processCallHistory[hProcess];
                    if ((now - lastProcessCall).TotalMilliseconds < MinDelayBetweenCalls)
                    {
                        Thread.Sleep(MinDelayBetweenCalls - (int)(now - lastProcessCall).TotalMilliseconds);
                    }
                }

                _lastCall = DateTime.Now;
                _processCallHistory[hProcess] = DateTime.Now;
                return true;
            }
        }

        private static bool AcquireExecutionSlot()
        {
            lock (_lockObject)
            {
                if (_currentCalls >= MaxConcurrentCalls)
                {
                    return false;
                }
                _currentCalls++;
                return true;
            }
        }

        private static void ReleaseExecutionSlot()
        {
            lock (_lockObject)
            {
                _currentCalls = Math.Max(0, _currentCalls - 1);
            }
        }

        private static ExecutionResult ExecuteWithTimeoutProtection(IntPtr hProcess, IntPtr functionAddress, Parameter[] parameters)
        {
            var result = new ExecutionResult { Success = false };
            List<IntPtr> remoteParams = new List<IntPtr>();
            IntPtr remoteStub = IntPtr.Zero;
            IntPtr hThread = IntPtr.Zero;

            try
            {
                // 1. Preparar parámetros
                foreach (var param in parameters)
                {
                    IntPtr remoteParam = WriteParameterToRemoteProcess(hProcess, param);
                    if (remoteParam == IntPtr.Zero)
                    {
                        Logger.Error("Failed to write parameter to remote process", "RemoteFunctionExecutor");
                        result.ErrorMessage = "Failed to write parameter to remote process";
                        return result;
                    }
                    remoteParams.Add(remoteParam);
                }

                // 2. Crear stub con protección adicional
                remoteStub = CreateProtectedCallStub(hProcess, functionAddress, remoteParams, parameters);
                if (remoteStub == IntPtr.Zero)
                {
                    Logger.Error("Failed to create call stub", "RemoteFunctionExecutor");
                    result.ErrorMessage = "Failed to create call stub";
                    return result;
                }

                // 3. Ejecutar con timeout y monitoreo
                hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, remoteStub, IntPtr.Zero, 0, out uint threadId);
                if (hThread == IntPtr.Zero)
                {
                    Logger.Error("Failed to create remote thread", "RemoteFunctionExecutor");
                    result.ErrorMessage = "Failed to create remote thread";
                    return result;
                }

                // 4. Esperar con timeout
                uint waitResult = WaitForSingleObject(hThread, (uint)MaxExecutionTimeout);

                if (waitResult == WAIT_TIMEOUT)
                {
                    Logger.Warning($"Remote function execution timed out after {MaxExecutionTimeout}ms", "RemoteFunctionExecutor");
                    result.TimedOut = true;
                    result.ErrorMessage = "Execution timed out";

                    // Intentar terminar el thread de forma segura
                    if (EnableTimeoutProtection)
                    {
                        Logger.Debug("Terminating timed out thread", "RemoteFunctionExecutor");
                        TerminateThread(hThread, 1);
                    }

                    return result;
                }
                else if (waitResult == WAIT_OBJECT_0)
                {
                    // Obtener código de salida
                    GetExitCodeThread(hThread, out result.ExitCode);
                    result.Success = true;
                    Logger.Debug($"Remote function completed with exit code: {result.ExitCode}", "RemoteFunctionExecutor");
                    return result;
                }
                else
                {
                    Logger.Error($"Thread wait failed with result: {waitResult}", "RemoteFunctionExecutor");
                    result.ErrorMessage = "Thread wait failed";
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception during remote function execution: {ex.Message}", "RemoteFunctionExecutor");
                result.ErrorMessage = $"Exception: {ex.Message}";
                return result;
            }
            finally
            {
                // Cleanup mejorado
                if (hThread != IntPtr.Zero)
                {
                    CloseHandle(hThread);
                }

                // Limpiar memoria remota
                foreach (var remoteParam in remoteParams)
                {
                    if (remoteParam != IntPtr.Zero)
                    {
                        VirtualFreeEx(hProcess, remoteParam, 0, MEM_RELEASE);
                    }
                }

                if (remoteStub != IntPtr.Zero)
                {
                    VirtualFreeEx(hProcess, remoteStub, 0, MEM_RELEASE);
                }
            }
        }

        private static IntPtr CreateProtectedCallStub(IntPtr hProcess, IntPtr functionAddress, List<IntPtr> paramAddresses, Parameter[] parameters)
        {
            byte[] stubCode;

            if (IntPtr.Size == 8) // x64
            {
                stubCode = CreateProtectedX64CallStub(functionAddress, paramAddresses, parameters);
            }
            else // x86
            {
                stubCode = CreateProtectedX86CallStub(functionAddress, paramAddresses, parameters);
            }

            return WriteCodeToRemoteProcess(hProcess, stubCode);
        }

        private static byte[] CreateProtectedX64CallStub(IntPtr functionAddress, List<IntPtr> paramAddresses, Parameter[] parameters)
        {
            List<byte> stub = new List<byte>();

            // Prólogo con protección de stack
            stub.AddRange(new byte[] { 0x55 }); // push rbp
            stub.AddRange(new byte[] { 0x48, 0x89, 0xE5 }); // mov rbp, rsp
            stub.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x40 }); // sub rsp, 64 (más shadow space)

            // Guardar registros importantes
            stub.AddRange(new byte[] { 0x53 }); // push rbx
            stub.AddRange(new byte[] { 0x56 }); // push rsi
            stub.AddRange(new byte[] { 0x57 }); // push rdi

            // Usar el código original para cargar parámetros
            byte[][] regCodes = {
                new byte[] { 0x48, 0xB9 }, // mov rcx, imm64
                new byte[] { 0x48, 0xBA }, // mov rdx, imm64
                new byte[] { 0x49, 0xB8 }, // mov r8, imm64
                new byte[] { 0x49, 0xB9 }  // mov r9, imm64
            };

            // Cargar primeros 4 parámetros en registros
            for (int i = 0; i < Math.Min(parameters.Length, 4); i++)
            {
                stub.AddRange(regCodes[i]);
                stub.AddRange(BitConverter.GetBytes(paramAddresses[i].ToInt64()));

                // Si el parámetro no es string o pointer, desreferenciar
                if (parameters[i].Type != ParamType.String && parameters[i].Type != ParamType.Pointer)
                {
                    switch (i)
                    {
                        case 0: stub.AddRange(GetDereferenceCode(parameters[i].Type, 0x09)); break;
                        case 1: stub.AddRange(GetDereferenceCode(parameters[i].Type, 0x0A)); break;
                        case 2: stub.AddRange(new byte[] { 0x4D, 0x8B, 0x00 }); break;
                        case 3: stub.AddRange(new byte[] { 0x4D, 0x8B, 0x09 }); break;
                    }
                }
            }

            // Parámetros adicionales al stack
            for (int i = parameters.Length - 1; i >= 4; i--)
            {
                stub.AddRange(new byte[] { 0x48, 0xB8 }); // mov rax, paramAddress
                stub.AddRange(BitConverter.GetBytes(paramAddresses[i].ToInt64()));

                if (parameters[i].Type != ParamType.String && parameters[i].Type != ParamType.Pointer)
                {
                    stub.AddRange(GetDereferenceCode(parameters[i].Type, 0x00));
                }
                stub.AddRange(new byte[] { 0x50 }); // push rax
            }

            // Llamar función con protección
            stub.AddRange(new byte[] { 0x48, 0xB8 }); // mov rax, functionAddress
            stub.AddRange(BitConverter.GetBytes(functionAddress.ToInt64()));
            stub.AddRange(new byte[] { 0xFF, 0xD0 }); // call rax

            // Restaurar registros
            stub.AddRange(new byte[] { 0x5F }); // pop rdi
            stub.AddRange(new byte[] { 0x5E }); // pop rsi
            stub.AddRange(new byte[] { 0x5B }); // pop rbx

            // Epílogo
            stub.AddRange(new byte[] { 0x48, 0x89, 0xEC }); // mov rsp, rbp
            stub.AddRange(new byte[] { 0x5D }); // pop rbp
            stub.AddRange(new byte[] { 0xC3 }); // ret

            return stub.ToArray();
        }

        private static byte[] CreateProtectedX86CallStub(IntPtr functionAddress, List<IntPtr> paramAddresses, Parameter[] parameters)
        {
            List<byte> stub = new List<byte>();

            // Prólogo con protección
            stub.AddRange(new byte[] { 0x55 }); // push ebp
            stub.AddRange(new byte[] { 0x89, 0xE5 }); // mov ebp, esp
            stub.AddRange(new byte[] { 0x53 }); // push ebx
            stub.AddRange(new byte[] { 0x56 }); // push esi
            stub.AddRange(new byte[] { 0x57 }); // push edi

            // Push parámetros en orden inverso
            for (int i = parameters.Length - 1; i >= 0; i--)
            {
                stub.AddRange(new byte[] { 0xB8 }); // mov eax, paramAddress
                stub.AddRange(BitConverter.GetBytes(paramAddresses[i].ToInt32()));

                if (parameters[i].Type != ParamType.String && parameters[i].Type != ParamType.Pointer)
                {
                    stub.AddRange(GetDereferenceCode32(parameters[i].Type));
                }
                stub.AddRange(new byte[] { 0x50 }); // push eax
            }

            // Llamar función
            stub.AddRange(new byte[] { 0xB8 }); // mov eax, functionAddress
            stub.AddRange(BitConverter.GetBytes(functionAddress.ToInt32()));
            stub.AddRange(new byte[] { 0xFF, 0xD0 }); // call eax

            // Limpiar stack
            if (parameters.Length > 0)
            {
                stub.AddRange(new byte[] { 0x83, 0xC4, (byte)(parameters.Length * 4) });
            }

            // Restaurar registros
            stub.AddRange(new byte[] { 0x5F }); // pop edi
            stub.AddRange(new byte[] { 0x5E }); // pop esi
            stub.AddRange(new byte[] { 0x5B }); // pop ebx

            // Epílogo
            stub.AddRange(new byte[] { 0x5D }); // pop ebp
            stub.AddRange(new byte[] { 0xC3 }); // ret

            return stub.ToArray();
        }

        // Métodos auxiliares originales (mantenidos igual)
        private static Parameter ConvertObjectToParameter(object obj)
        {
            return obj switch
            {
                int i => Int(i),
                long l => Long(l),
                float f => Float(f),
                double d => Double(d),
                string s => String(s),
                bool b => Bool(b),
                IntPtr ptr => Pointer(ptr),
                byte bt => Byte(bt),
                short sh => Short(sh),
                uint ui => UInt(ui),
                ulong ul => ULong(ul),
                _ => throw new ArgumentException($"Tipo no soportado: {obj.GetType()}")
            };
        }

        private static IntPtr WriteParameterToRemoteProcess(IntPtr hProcess, Parameter param)
        {
            return param.Type switch
            {
                ParamType.Int32 => WriteValueToRemoteProcess(hProcess, BitConverter.GetBytes((int)param.Value)),
                ParamType.Int64 => WriteValueToRemoteProcess(hProcess, BitConverter.GetBytes((long)param.Value)),
                ParamType.Float => WriteValueToRemoteProcess(hProcess, BitConverter.GetBytes((float)param.Value)),
                ParamType.Double => WriteValueToRemoteProcess(hProcess, BitConverter.GetBytes((double)param.Value)),
                ParamType.String => WriteStringToRemoteProcess(hProcess, (string)param.Value),
                ParamType.Bool => WriteValueToRemoteProcess(hProcess, BitConverter.GetBytes((bool)param.Value ? 1 : 0)),
                ParamType.Pointer => WriteValueToRemoteProcess(hProcess,
                    IntPtr.Size == 8 ? BitConverter.GetBytes(((IntPtr)param.Value).ToInt64()) :
                                      BitConverter.GetBytes(((IntPtr)param.Value).ToInt32())),
                ParamType.Byte => WriteValueToRemoteProcess(hProcess, new byte[] { (byte)param.Value }),
                ParamType.Short => WriteValueToRemoteProcess(hProcess, BitConverter.GetBytes((short)param.Value)),
                ParamType.UInt32 => WriteValueToRemoteProcess(hProcess, BitConverter.GetBytes((uint)param.Value)),
                ParamType.UInt64 => WriteValueToRemoteProcess(hProcess, BitConverter.GetBytes((ulong)param.Value)),
                _ => IntPtr.Zero
            };
        }

        private static IntPtr WriteStringToRemoteProcess(IntPtr hProcess, string text)
        {
            if (string.IsNullOrEmpty(text))
                return IntPtr.Zero;

            byte[] messageBytes = Encoding.UTF8.GetBytes(text + "\0");
            return WriteValueToRemoteProcess(hProcess, messageBytes);
        }

        private static IntPtr WriteValueToRemoteProcess(IntPtr hProcess, byte[] data)
        {
            IntPtr remoteAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)data.Length,
                MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

            if (remoteAddress == IntPtr.Zero)
                return IntPtr.Zero;

            bool success = WriteProcessMemory(hProcess, remoteAddress, data, (uint)data.Length, out _);

            if (!success)
            {
                VirtualFreeEx(hProcess, remoteAddress, 0, MEM_RELEASE);
                return IntPtr.Zero;
            }

            return remoteAddress;
        }

        private static byte[] GetDereferenceCode(ParamType type, byte regCode)
        {
            return type switch
            {
                ParamType.Int32 or ParamType.UInt32 or ParamType.Float or ParamType.Bool =>
                    new byte[] { 0x8B, regCode },
                ParamType.Int64 or ParamType.UInt64 or ParamType.Double =>
                    new byte[] { 0x48, 0x8B, regCode },
                ParamType.Byte =>
                    new byte[] { 0x8A, regCode },
                ParamType.Short =>
                    new byte[] { 0x66, 0x8B, regCode },
                _ => new byte[] { 0x8B, regCode }
            };
        }

        private static byte[] GetDereferenceCode32(ParamType type)
        {
            return type switch
            {
                ParamType.Byte => new byte[] { 0x8A, 0x00 },
                ParamType.Short => new byte[] { 0x66, 0x8B, 0x00 },
                _ => new byte[] { 0x8B, 0x00 }
            };
        }

        private static IntPtr WriteCodeToRemoteProcess(IntPtr hProcess, byte[] code)
        {
            IntPtr remoteAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)code.Length,
                MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

            if (remoteAddress == IntPtr.Zero)
                return IntPtr.Zero;

            bool success = WriteProcessMemory(hProcess, remoteAddress, code, (uint)code.Length, out _);

            if (!success)
            {
                VirtualFreeEx(hProcess, remoteAddress, 0, MEM_RELEASE);
                return IntPtr.Zero;
            }

            return remoteAddress;
        }

        /// <summary>
        /// Método para limpiar recursos y resetear estado
        /// </summary>
        public static void ResetState()
        {
            lock (_lockObject)
            {
                _currentCalls = 0;
                _processCallHistory.Clear();
                _lastCall = DateTime.MinValue;
                Logger.Debug("RemoteFunctionExecutor state reset", "RemoteFunctionExecutor");
            }
        }

        /// <summary>
        /// Obtener estadísticas del executor
        /// </summary>
        public static string GetStats()
        {
            lock (_lockObject)
            {
                var stats = $"Current calls: {_currentCalls}/{MaxConcurrentCalls}, " +
                           $"Processes tracked: {_processCallHistory.Count}, " +
                           $"Last call: {_lastCall:HH:mm:ss.fff}";
                Logger.Debug($"RemoteFunctionExecutor stats: {stats}", "RemoteFunctionExecutor");
                return stats;
            }
        }
    }
}