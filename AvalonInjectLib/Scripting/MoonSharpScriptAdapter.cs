using AvalonInjectLib.Interfaces;
using MoonSharp.Interpreter;

namespace AvalonInjectLib.Scripting
{
    public class MoonSharpScriptAdapter
    {
        public AvalonScript Script { get; private set; }
        private Script _script;

        public MoonSharpScriptAdapter(string scriptPath, string categoryFromPath = null)
        {
            Script = new AvalonScript(scriptPath);
            _script = new Script(CoreModules.Preset_Default | CoreModules.LoadMethods);

            // Default category from path or "General"
            Script.Category = categoryFromPath ?? "General";
        }

        public void Initialize()
        {
            try
            {
                var callbacks = new LuaAvalonCallbacks();
                callbacks.RegisterAll(_script);
                _script.DoFile(Script.FilePath);

                Script.IsEnabled = false;

                // Process script metadata
                ProcessScriptMetadata();

                // Process script value (including arrays)
                ProcessScriptValue();

                Script.SetLua(_script);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MoonSharp] Error initializing {Script.FilePath}: {ex.Message}");
            }
        }

        private void ProcessScriptMetadata()
        {
            // Get category from script if defined
            var categoryDyn = _script.Globals.Get("Category");
            if (categoryDyn.Type == DataType.String && !string.IsNullOrEmpty(categoryDyn.String))
            {
                Script.Category = categoryDyn.String;
            }

            // Get script type
            var typeDyn = _script.Globals.Get("Type");

            if (typeDyn.Type == DataType.String)
            {
                switch (typeDyn.String.ToLower())
                {
                    case "button":
                        Script.Type = ScriptControlType.Button;
                        break;
                    case "slider":
                        Script.Type = ScriptControlType.Slider;
                        break;
                    case "combobox":
                        Script.Type = ScriptControlType.ComboBox;
                        break;
                    case "textbox":
                        Script.Type = ScriptControlType.TextBox;
                        break;
                    case "label":
                        Script.Type = ScriptControlType.Label;
                        break;
                    case "separator":
                        Script.Type = ScriptControlType.Separator;
                        break;
                    default:
                        Script.Type = ScriptControlType.CheckBox;
                        break;
                }
            }

            // Get script name
            var nameDyn = _script.Globals.Get("Name");
            if (nameDyn.Type == DataType.String)
                Script.Name = nameDyn.String;
            else
                Script.Name = Path.GetFileNameWithoutExtension(Script.FilePath);


            Script.IsEnabled = true;
        }

        private void ProcessScriptValue()
        {
            var valueDyn = _script.Globals.Get("Value");

            switch (valueDyn.Type)
            {
                case DataType.String:
                    Script.Value = valueDyn.String;
                    break;
                case DataType.Number:
                    Script.Value = (float)valueDyn.Number;
                    break;
                case DataType.Boolean:
                    Script.Value = valueDyn.Boolean;
                    break;
                case DataType.Table:
                    // Handle array types
                    if (Script.Type == ScriptControlType.ComboBox || Script.Type == ScriptControlType.Slider)
                    {
                        // For ComboBox, we expect an array of strings
                        if (Script.Type == ScriptControlType.ComboBox)
                        {
                            Script.Value = valueDyn.Table.Values
                                .Where(v => v.Type == DataType.String)
                                .Select(v => v.String)
                                .ToArray();
                        }
                        // For Slider, we might expect min/max/step values
                        else if (Script.Type == ScriptControlType.Slider)
                        {
                            var values = valueDyn.Table.Values
                                .Where(v => v.Type == DataType.Number)
                                .Select(v => (float)v.Number)
                                .ToArray();

                            if (values.Length >= 3)
                            {
                                Script.Value = new SliderValues
                                {
                                    Min = values[0],
                                    Max = values[1],
                                    Step = values[2],
                                    Default = values.Length > 3 ? values[3] : values[0]
                                };
                            }
                        }
                    }
                    else
                    {
                        // Generic table to object[] conversion
                        Script.Value = valueDyn.Table.Values
                            .Select(v => v.ToObject())
                            .ToArray();
                    }
                    break;
                case DataType.Nil:
                default:
                    Script.Value = null;
                    break;
            }
        }

        // Helper class for slider values
        private class SliderValues
        {
            public float Min { get; set; }
            public float Max { get; set; }
            public float Step { get; set; }
            public float Default { get; set; }
        }
    }
}