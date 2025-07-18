using AvalonInjectLib.Scripting;
using AvalonInjectLib.UIFramework;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace TargetGame
{
    public static class MoonSharpHelper
    {
        private static readonly Dictionary<string, MenuItem> _categoryItems = new();
        private static readonly HashSet<string> _processedScripts = new();

        public static void InitializeMenuStructure(MenuList menu, IEnumerable<AvalonScript> scripts)
        {
            ClearExistingData();
            CreateCategoryStructure(menu, scripts);
            PopulateScripts(scripts);
        }

        private static void ClearExistingData()
        {
            _categoryItems.Clear();
            _processedScripts.Clear();
        }

        private static void CreateCategoryStructure(MenuList menu, IEnumerable<AvalonScript> scripts)
        {
            var uniqueCategories = new HashSet<string>();

            foreach (var script in scripts)
            {
                string category = GetCategoryFromScript(script);
                if (!string.IsNullOrEmpty(category))
                {
                    uniqueCategories.Add(category);
                }
            }

            foreach (var category in uniqueCategories)
            {
                CreateCategoryHierarchy(menu, category);
            }
        }

        private static void PopulateScripts(IEnumerable<AvalonScript> scripts)
        {
            foreach (var script in scripts)
            {
                string scriptId = $"{script.FilePath}_{script.Name}";
                if (!_processedScripts.Contains(scriptId))
                {
                    string category = GetCategoryFromScript(script);
                    AddScriptToCategory(script, category);
                    _processedScripts.Add(scriptId);
                }
            }
        }

        public static string GetCategoryFromScript(AvalonScript script)
        {
            if (!string.IsNullOrEmpty(script.FilePath))
            {
                const string scriptsFolder = "Scripts";
                int scriptsIndex = script.FilePath.LastIndexOf(scriptsFolder, StringComparison.OrdinalIgnoreCase);

                if (scriptsIndex >= 0)
                {
                    string afterScripts = script.FilePath.Substring(scriptsIndex + scriptsFolder.Length);
                    afterScripts = afterScripts.TrimStart('\\', '/');
                    string directoryPath = Path.GetDirectoryName(afterScripts);

                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        return directoryPath.Replace('\\', '/');
                    }
                }
            }
            return "General";
        }

        public static void CreateCategoryHierarchy(MenuList menu, string categoryPath)
        {
            if (string.IsNullOrEmpty(categoryPath))
            {
                categoryPath = "General";
            }

            string normalizedPath = NormalizeCategoryPath(categoryPath);
            if (_categoryItems.ContainsKey(normalizedPath)) return;

            var parts = normalizedPath.Split('/');
            string currentPath = "";
            MenuItem parentItem = null;

            for (int i = 0; i < parts.Length; i++)
            {
                string part = FormatCategoryName(parts[i]);
                currentPath = i == 0 ? part : $"{currentPath}/{part}";

                if (!_categoryItems.ContainsKey(currentPath))
                {
                    var categoryItem = new MenuItem(part)
                    {
                        HeaderBackgroundColor = Color.FromArgb(40, 40, 40),
                        HeaderTextColor = Color.FromArgb(255, 165, 0)
                    };

                    if (parentItem == null)
                    {
                        menu.AddItem(categoryItem);
                    }
                    else
                    {
                        parentItem.AddSubItem(categoryItem);
                    }

                    _categoryItems[currentPath] = categoryItem;
                }
                parentItem = _categoryItems[currentPath];
            }
        }

        public static void AddScriptToCategory(AvalonScript script, string categoryPath)
        {
            string normalizedPath = NormalizeCategoryPath(categoryPath);

            if (!_categoryItems.TryGetValue(normalizedPath, out var categoryItem))
            {
                if (!_categoryItems.TryGetValue("General", out categoryItem))
                {
                    return;
                }
            }

            var scriptItem = new MenuItem(script.Name)
            {
                IsEnabled = script.IsEnabled,
                HeaderBackgroundColor = Color.FromArgb(35, 35, 35),
                HeaderTextColor = Color.White
            };

            var contentItem = CreateControl(script);
            scriptItem.Content = contentItem;
            scriptItem.Height = contentItem.Height + 10;

            categoryItem.AddSubItem(scriptItem);
        }

        private static UIControl CreateControl(AvalonScript control)
        {
            switch (control.Type)
            {
                case ScriptControlType.Button:
                    var button = new Button
                    {
                        Width = 250f,
                        Text = control.Name
                    };
                    button.Click += (sender, pos) => control.ChangeValue(true);
                    return button;

                case ScriptControlType.Slider:
                    var slider = new Slider
                    {
                        Width = 250f,
                        Text = control.Name,
                        Value = Convert.ToSingle(control.Value)
                    };
                    slider.ValueChanged += (value) => control.ChangeValue(value);
                    return slider;

                default:
                    var checkBox = new CheckBox
                    {
                        Width = 250f,
                        Orientation = CheckBoxOrientation.Right,
                        Text = control.Name,
                        Checked = Convert.ToBoolean(control.Value),
                        BackColor = Color.Red,
                        CheckColor = Color.Green
                    };
                    checkBox.CheckedChanged += (isChecked) => control.ChangeValue(isChecked);
                    return checkBox;
            }
        }

        public static string NormalizeCategoryPath(string categoryPath)
        {
            if (string.IsNullOrEmpty(categoryPath)) return "General";

            var parts = categoryPath.Split(new[] { '/', '\\', '>' }, StringSplitOptions.RemoveEmptyEntries);
            var normalizedParts = parts.Select(p => FormatCategoryName(p.Trim())).ToArray();

            return string.Join("/", normalizedParts);
        }

        private static string FormatCategoryName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "General";
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(name.ToLower());
        }

        public static void ClearMenu(MenuList menu)
        {
            menu.ClearItems();
            ClearExistingData();
        }

        public static int GetCategoryCount()
        {
            return _categoryItems.Count;
        }
    }
}