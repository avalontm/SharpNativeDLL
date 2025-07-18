using AvalonInjectLib.Interfaces;
using MoonSharp.Interpreter;

namespace AvalonInjectLib.Scripting
{
    public class AvalonScript : IAvalonScript
    {
        public string FilePath { get; private set; }

        ScriptControlType _type = ScriptControlType.CheckBox;
        public ScriptControlType Type { get => _type; set { _type = value; } }

        string _category = "General";
        public string Category { get => _category; set { _category = value; } }

        string _name = string.Empty;
        public string Name { get => _name; set { _name = value; } }

        bool _isEnabled;
        public bool IsEnabled { get => _isEnabled; set { _isEnabled = value; } }

        object _value;
        public object Value { get => _value; set { _value = value; } }

        private Script _script;
        private DynValue _initializeFunc;
        private DynValue _updateFunc;
        private DynValue _drawFunc;
        private DynValue _valueFunc;

        public AvalonScript(string scriptPath)
        {
            this.FilePath = scriptPath;
        }

        public void SetLua(Script script)
        {
            _script = script;
            _initializeFunc = _script.Globals.Get("initialize");
            _updateFunc = _script.Globals.Get("update");
            _drawFunc = _script.Globals.Get("draw");
            _valueFunc = _script.Globals.Get("change_value");
        }

        public void Initialize(IAvalonEngine engine)
        {
            if (_initializeFunc.Type == DataType.Function)
                _script.Call(_initializeFunc);
        }

        public void Update()
        {
            try
            {
                if (_updateFunc != null && _updateFunc.Type == DataType.Function)
                    _script.Call(_updateFunc);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error en Update(): {ex.Message}", "MoonSharp");
            }
        }

        public void Draw()
        {
            try
            {
                if (_drawFunc != null && _drawFunc.Type == DataType.Function)
                    _script.Call(_drawFunc);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error en Draw(): {ex.Message}", "MoonSharp");
            }
        }

        public void ChangeValue(object value)
        {
            try
            {
                this.Value = value;

                if (_valueFunc != null && _valueFunc.Type == DataType.Function)
                {
                    // Convertir el valor a un tipo compatible con Lua
                    DynValue luaValue = ConvertToLuaValue(value);
                    _script.Call(_valueFunc, luaValue);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error en ChangeValue(): {ex.Message}", "MoonSharp");
            }
        }

        private DynValue ConvertToLuaValue(object value)
        {
            if (value == null)
                return DynValue.NewNil();

            switch (value)
            {
                case bool b:
                    return DynValue.NewBoolean(b);
                case int i:
                    return DynValue.NewNumber(i);
                case float f:
                    return DynValue.NewNumber(f);
                case double d:
                    return DynValue.NewNumber(d);
                case string s:
                    return DynValue.NewString(s);
                default:
                    // Para otros tipos, intenta una conversión automática
                    return DynValue.FromObject(_script, value);
            }
        }
    }
}
