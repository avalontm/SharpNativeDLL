
using AvalonInjectLib.Interfaces;
using MoonSharp.Interpreter;

namespace AvalonInjectLib.Scripting
{
    public class AvalonScript : IAvalonScript, IDisposable
    {
        private bool _disposed;

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

        public void Initialize()
        {
            try
            {
                if (IsEnabled)
                {
                    if (_initializeFunc.Type == DataType.Function)
                        _script.Call(_initializeFunc);
                }
            }
            catch (ArgumentException ex)
            {
                Logger.Error($"Error en Initialize(): {ex.Message}", "MoonSharp");
            }
        }

        public void Update()
        {
            try
            {
                if (IsEnabled)
                {
                    if (_updateFunc != null && _updateFunc.Type == DataType.Function)
                        _script.Call(_updateFunc);
                }
            }
            catch (ArgumentException ex)
            {
                Logger.Error($"Error en Update(): {ex.Message}", "MoonSharp");
            }
        }

        public void Draw()
        {
            try
            {
                if (IsEnabled)
                {
                    if (_drawFunc != null && _drawFunc.Type == DataType.Function)
                        _script.Call(_drawFunc);
                }
            }
            catch (ArgumentException ex)
            {
                Logger.Error($"Error en Draw(): {ex.Message}", "MoonSharp");
            }
        }

        public void ChangeValue(object value)
        {
            try
            {
                this.Value = value;

                if (IsEnabled)
                {
                    if (_valueFunc != null && _valueFunc.Type == DataType.Function)
                    {
                        _script.Call(_valueFunc, Value);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                Logger.Error($"Error en ChangeValue(): {ex.Message}", "MoonSharp");
            }
        }

        // Lógica de Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Libera recursos administrados aquí si los tuvieras
                _script = null;
                _initializeFunc = null;
                _updateFunc = null;
                _drawFunc = null;
                _valueFunc = null;
            }

            // Libera recursos no administrados aquí si los tuvieras

            _disposed = true;
        }

        ~AvalonScript()
        {
            Dispose(false);
        }
    }
}
