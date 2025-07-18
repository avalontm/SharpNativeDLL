using AvalonInjectLib.Interfaces;
using MoonSharp.Interpreter;

namespace AvalonInjectLib.Scripting
{
    public class LuaAvalonCallbacks
    {
        private readonly IAvalonEngine _engine;

        public LuaAvalonCallbacks(IAvalonEngine engine)
        {
            _engine = engine;
        }

        public void RegisterAll(Script script)
        {
            // Registrar Memory
            RegisterMemoryFunctions(script);

            // Registrar funciones globales útiles
            RegisterGlobalFunctions(script);

        }

        private void RegisterMemoryFunctions(Script script)
        {
            var memoryTable = new Table(script);

            // Métodos de escritura
            memoryTable["Write"] = (Func<DynValue[], DynValue>)(args =>
            {
                if (args.Length == 2) // baseAddress, value
                {
                    var baseAddress = (int)args[0].Number;
                    var value = (int)args[1].Number;
                    MemoryManager.Write<int>(_engine.Process.Handle, baseAddress, value, null);
                }
                else if (args.Length == 3) // baseAddress, value, offsets
                {
                    var baseAddress = (int)args[0].Number;
                    var value = (int)args[1].Number;
                    var offsets = args[2].Table.Values.Select(v => (int)v.Number).ToArray();
                    MemoryManager.Write<int>(_engine.Process.Handle, baseAddress, value, offsets);
                }
                return DynValue.Void;
            });

            // Métodos de lectura específicos por tipo
            memoryTable["ReadInt"] = (Func<DynValue[], DynValue>)(args =>
            {
                var baseAddress = (int)args[0].Number;
                int[] offsets = null;

                if (args.Length == 2 && !args[1].IsNil())
                {
                    offsets = args[1].Table.Values.Select(v => (int)v.Number).ToArray();
                }

                var result = MemoryManager.Read<int>(_engine.Process.Handle, baseAddress, offsets);
                return DynValue.FromObject(script, result);
            });

            memoryTable["ReadByte"] = (Func<DynValue[], DynValue>)(args =>
            {
                var baseAddress = (int)args[0].Number;
                int[] offsets = null;

                if (args.Length == 2 && !args[1].IsNil())
                {
                    offsets = args[1].Table.Values.Select(v => (int)v.Number).ToArray();
                }

                var result = MemoryManager.Read<byte>(_engine.Process.Handle, baseAddress, offsets);
                return DynValue.FromObject(script, result);
            });

            memoryTable["ReadFloat"] = (Func<DynValue[], DynValue>)(args =>
            {
                var baseAddress = (int)args[0].Number;
                int[] offsets = null;

                if (args.Length == 2 && !args[1].IsNil())
                {
                    offsets = args[1].Table.Values.Select(v => (int)v.Number).ToArray();
                }

                var result = MemoryManager.Read<float>(_engine.Process.Handle, baseAddress, offsets);
                return DynValue.FromObject(script, result);
            });

            memoryTable["ReadString"] = (Func<DynValue[], DynValue>)(args =>
            {
                var baseAddress = (int)args[0].Number;
                int[] offsets = null;

                if (args.Length == 2 && !args[1].IsNil())
                {
                    offsets = args[1].Table.Values.Select(v => (int)v.Number).ToArray();
                }

                var result = MemoryManager.ReadString(_engine.Process.Handle, baseAddress);
                return DynValue.FromObject(script, result);
            });

            script.Globals["Memory"] = memoryTable;
        }

        private void RegisterGlobalFunctions(Script script)
        {
            // Funciones globales convenientes
            script.Globals["print"] = (Action<string>)(msg => _engine.Log(msg, "Script"));

            // Más funciones globales:
             script.Globals["sleep"] = (Action<int>)(ms => Thread.Sleep(ms));
             script.Globals["random"] = (Func<int, int>)(max => new Random().Next(max));

            // Función tostring mejorada
            script.Globals["tostring"] = (Func<object, string>)(value =>
            {
                if (value == null)
                    return "nil";

                if (value is bool)
                    return (bool)value ? "true" : "false";

                if (value is Table table)
                    return $"table: 0x{table.GetHashCode():X8}";

                return value.ToString();
            });

            script.Globals["tonumber"] = (Func<string, double?>)(s => double.TryParse(s, out var num) ? num : (double?)null);

            // Tipo de dato
            script.Globals["type"] = (Func<object, string>)(value =>
                value == null ? "nil" :
                value.GetType().Name.ToLower());

            // Matemáticas básicas
            script.Globals["math"] = new Table(script)
            {
                ["floor"] = (Func<double, double>)(Math.Floor),
                ["ceil"] = (Func<double, double>)(Math.Ceiling),
                ["abs"] = (Func<double, double>)(Math.Abs),
                ["min"] = (Func<double, double, double>)(Math.Min),
                ["max"] = (Func<double, double, double>)(Math.Max)
            };
        }

    }
}
