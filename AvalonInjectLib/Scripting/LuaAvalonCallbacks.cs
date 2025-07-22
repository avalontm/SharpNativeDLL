using AvalonInjectLib.Interfaces;
using MoonSharp.Interpreter;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace AvalonInjectLib.Scripting
{
    /// <summary>
    /// Provides Lua callback functions for Avalon engine operations.
    /// Optimized for AOT compilation and native library usage.
    /// </summary>
    public sealed class LuaAvalonCallbacks
    {
        #region Private Fields

        private readonly Random _random;
        private static readonly object _randomLock = new object();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LuaAvalonCallbacks"/> class.
        /// </summary>
        /// <param name="engine">The Avalon engine instance to use for operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when engine is null.</exception>
        public LuaAvalonCallbacks()
        {
            _random = new Random();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers all available Lua callback functions to the specified script.
        /// </summary>
        /// <param name="script">The MoonSharp script instance to register functions to.</param>
        /// <exception cref="ArgumentNullException">Thrown when script is null.</exception>
        public void RegisterAll(Script script)
        {
            if (script == null)
                throw new ArgumentNullException(nameof(script));

            RegisterProcessFunctions(script);
            RegisterGlobalFunctions(script);
        }

        #endregion

        #region Process Functions Registration

        /// <summary>
        /// Registers process-related functions for memory operations and remote function calls.
        /// </summary>
        /// <param name="script">The script instance to register functions to.</param>
        private void RegisterProcessFunctions(Script script)
        {
            var memoryTable = new Table(script);

            // Memory write operations
            RegisterMemoryWriteFunctions(memoryTable);

            // Memory read operations  
            RegisterMemoryReadFunctions(memoryTable);

            // Remote function execution
            RegisterRemoteFunctionCalls(memoryTable);

            script.Globals["Process"] = memoryTable;
        }

        /// <summary>
        /// Registers memory write functions to the memory table.
        /// </summary>
        /// <param name="memoryTable">The memory table to register functions to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterMemoryWriteFunctions(Table memoryTable)
        {
            memoryTable["Write"] = (Func<DynValue[], DynValue>)(args =>
            {
                if (args.Length == 2) // baseAddress, value
                {
                    var baseAddress = ConvertToInt32(args[0]);
                    var value = ConvertToInt32(args[1]);
                    MemoryManager.Write<int>(baseAddress, value);
                }
                else if (args.Length == 3) // baseAddress, value, offsets
                {
                    var baseAddress = ConvertToInt32(args[0]);
                    var value = ConvertToInt32(args[1]);
                    var offsets = ExtractOffsetsFromTable(args[2]);
                    MemoryManager.Write<int>(baseAddress, value, offsets);
                }
                return DynValue.Void;
            });
        }

        /// <summary>
        /// Registers memory read functions for different data types.
        /// </summary>
        /// <param name="memoryTable">The memory table to register functions to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterMemoryReadFunctions(Table memoryTable)
        {
            // Read integer value
            memoryTable["ReadInt"] = (Func<long, DynValue, int>)((address, offsetsDyn) =>
            {
                var offsets = ExtractOffsetsFromDynValue(offsetsDyn);
                return MemoryManager.Read<int>(address, offsets);
            });

            // Read byte value
            memoryTable["ReadByte"] = (Func<long, DynValue, byte>)((address, offsetsDyn) =>
            {
                var offsets = ExtractOffsetsFromDynValue(offsetsDyn);
                return MemoryManager.Read<byte>(address, offsets);
            });

            // Read float value
            memoryTable["ReadFloat"] = (Func<long, DynValue, float>)((address, offsetsDyn) =>
            {
                var offsets = ExtractOffsetsFromDynValue(offsetsDyn);
                return MemoryManager.Read<float>(address, offsets);
            });

            // Read string value
            memoryTable["ReadString"] = (Func<long, DynValue, string>)((address, offsetsDyn) =>
            {
                return MemoryManager.ReadString(address);
            });
        }

        /// <summary>
        /// Registers remote function call capabilities.
        /// </summary>
        /// <param name="memoryTable">The memory table to register functions to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterRemoteFunctionCalls(Table memoryTable)
        {
            memoryTable["CallFunction"] = (Func<long, DynValue[], object>)((functionAddress, args) =>
            {
                // Convert Lua parameters to C# objects with type safety
                var parameters = new List<object>();

                foreach (var arg in args)
                {
                    if (arg.Type == DataType.Number)
                    {
                        if (arg.Number == Math.Floor(arg.Number))
                        {
                            parameters.Add((int)arg.Number);
                        }
                        else
                        {
                            parameters.Add((float)arg.Number);
                        }
                    }
                    else if (arg.Type == DataType.String)
                    {
                        parameters.Add(arg.String ?? string.Empty);
                    }
                    else if (arg.Type == DataType.Boolean)
                    {
                        parameters.Add(arg.Boolean);
                    }
                    else if (arg.Type == DataType.Table)
                    {
                        // Convertir tablas a arrays si es necesario
                        var tableValues = arg.Table.Values
                            .Select(v => v.ToObject())
                            .ToArray();
                        parameters.Add(tableValues);
                    }
                    else
                    {
                        parameters.Add(arg.ToObject());
                    }
                }

                // Execute remote function call
                return InternalFunctionExecutor.CallFunction(
                    new IntPtr(functionAddress),
                    parameters.ToArray());
            });
        }

        #endregion

        #region Global Functions Registration

        /// <summary>
        /// Registers global utility functions available throughout the Lua script.
        /// </summary>
        /// <param name="script">The script instance to register functions to.</param>
        private void RegisterGlobalFunctions(Script script)
        {
            RegisterUtilityFunctions(script);
            RegisterOSFunctions(script);
            RegisterTypeConversionFunctions(script);
            RegisterMathFunctions(script);
        }

        private void RegisterOSFunctions(Script script)
        {
            script.Globals["os"] = new Table(script)
            {
                ["time"] = (Func<double>)(() =>
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                ["clock"] = (Func<double>)(() =>
                    (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency * 1000)
            };
        }

        /// <summary>
        /// Registers basic utility functions like print, sleep, and random.
        /// </summary>
        /// <param name="script">The script instance to register functions to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterUtilityFunctions(Script script)
        {
            // Logging function
            script.Globals["print"] = (Action<string>)(msg =>
            {
                if (!string.IsNullOrEmpty(msg))
                    Logger.Info(msg, "Script");
            });

            // Sleep function with validation
            script.Globals["sleep"] = (Action<int>)(ms =>
            {
                if (ms > 0 && ms <= 60000) // Max 60 seconds
                    Thread.Sleep(ms);
            });

            // Thread-safe random function
            script.Globals["random"] = (Func<int, int>)(max =>
            {
                if (max <= 0) return 0;
                lock (_randomLock)
                {
                    return _random.Next(max);
                }
            });
        }

        /// <summary>
        /// Registers type conversion and inspection functions.
        /// </summary>
        /// <param name="script">The script instance to register functions to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterTypeConversionFunctions(Script script)
        {
            // Enhanced tostring function
            script.Globals["tostring"] = (Func<object, string>)(value =>
            {
                if (value == null) return "nil";
                if (value is bool boolVal) return boolVal ? "true" : "false";
                if (value is Table table) return $"table: 0x{table.GetHashCode():X8}";
                return value.ToString() ?? "nil";
            });

            // Safe number conversion
            script.Globals["tonumber"] = (Func<string, double?>)(s =>
            {
                if (string.IsNullOrEmpty(s)) return null;
                return double.TryParse(s, out var num) ? num : null;
            });

            // Type inspection function
            script.Globals["type"] = (Func<object, string>)(value =>
            {
                if (value == null) return "nil";
                return value.GetType().Name.ToLowerInvariant();
            });
        }

        /// <summary>
        /// Registers mathematical functions for common operations.
        /// </summary>
        /// <param name="script">The script instance to register functions to.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterMathFunctions(Script script)
        {
            var mathTable = new Table(script)
            {
                ["floor"] = (Func<double, double>)(Math.Floor),
                ["ceil"] = (Func<double, double>)(Math.Ceiling),
                ["abs"] = (Func<double, double>)(Math.Abs),
                ["min"] = (Func<double, double, double>)(Math.Min),
                ["max"] = (Func<double, double, double>)(Math.Max),
                ["sqrt"] = (Func<double, double>)(Math.Sqrt),
                ["pow"] = (Func<double, double, double>)(Math.Pow),
                ["round"] = (Func<double, double>)(Math.Round)
            };

            script.Globals["math"] = mathTable;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Converts a DynValue to a 32-bit integer safely.
        /// </summary>
        /// <param name="value">The DynValue to convert.</param>
        /// <returns>The converted integer value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ConvertToInt32(DynValue value)
        {
            return value.Type == DataType.Number ? (int)value.Number : 0;
        }

        /// <summary>
        /// Extracts offset array from a Lua table DynValue.
        /// </summary>
        /// <param name="offsetsDyn">The DynValue containing the offsets table.</param>
        /// <returns>An array of integer offsets, or null if not applicable.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int[]? ExtractOffsetsFromDynValue(DynValue offsetsDyn)
        {
            if (offsetsDyn.IsNil() || offsetsDyn.Type != DataType.Table)
                return null;

            return offsetsDyn.Table.Values.Select(v => (int)v.Number).ToArray();
        }

        /// <summary>
        /// Extracts offset array from a table DynValue with validation.
        /// </summary>
        /// <param name="tableValue">The DynValue containing the table.</param>
        /// <returns>An array of integer offsets.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int[] ExtractOffsetsFromTable(DynValue tableValue)
        {
            if (tableValue.Type != DataType.Table)
                throw new ArgumentException("Expected table for offsets");

            return tableValue.Table.Values.Select(v => (int)v.Number).ToArray();
        }

        #endregion
    }
}