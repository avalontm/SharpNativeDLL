using AvalonInjectLib;
using AvalonInjectLib.Scripting;
using AvalonInjectLib.UIFramework;
using System.Globalization;

namespace TargetGame
{
    public class MenuSystem : UIFrameworkRenderSystem
    {
        private bool _isInitialized = false;
        private MenuList _mainMenu;
        private Dictionary<string, MenuItem> _categoryItems = new Dictionary<string, MenuItem>();
        private Dictionary<string, MenuList> _activeSubmenus = new Dictionary<string, MenuList>();
        private HashSet<string> _processedScripts = new HashSet<string>(); // Para evitar duplicados

        // Configuración de diseño
        private const float MaxMenuHeight = 500f;
        private const float MenuWidth = 250f;
        private const float SubmenuWidth = 220f;
        private const float MenuPadding = 5f;

        public void Initialize()
        {
            if (_isInitialized) return;

            // Inicializar dependencias
            Font.Initialize();
            InputSystem.Initialize(AvalonEngine.Instance.Process.ProcessId);

            // Crear menú principal
            CreateMainMenu();
            LoadScripts();

            _isInitialized = true;
        }

        private void CreateMainMenu()
        {
            _mainMenu = new MenuList
            {
                X = MenuPadding,
                Y = MenuPadding,
                Width = MenuWidth,
                Height = 200f, // Se ajustará automáticamente
                HeaderText = "Avalon[HUB]",
                Visible = true
            };

        }

        private void LoadScripts()
        {
            _mainMenu.ClearItems();
            _categoryItems.Clear();
            _processedScripts.Clear();

            // Obtener todos los scripts y procesarlos
            var allScripts = MoonSharpScriptLoader.Scripts;

            // Primero crear todas las categorías basadas en la estructura de carpetas
            var uniqueCategories = new HashSet<string>();
            foreach (var script in allScripts)
            {
                string category = GetCategoryFromScript(script);
                if (!string.IsNullOrEmpty(category))
                {
                    uniqueCategories.Add(category);
                }
            }

            // Crear jerarquía de categorías
            foreach (var category in uniqueCategories)
            {
                CreateCategoryHierarchy(category);
            }

            // Luego agregar todos los scripts una sola vez
            foreach (var script in allScripts)
            {
                // Evitar duplicados usando un identificador único
                string scriptId = $"{script.FilePath}_{script.Name}";
                if (!_processedScripts.Contains(scriptId))
                {
                    string category = GetCategoryFromScript(script);
                    AddScriptToCategory(script, category);
                    _processedScripts.Add(scriptId);
                }
            }

            // Ajustar tamaño del menú
            AdjustMenuSize();
        }

        private string GetCategoryFromScript(AvalonScript script)
        {
            // Si el script tiene un FilePath, extraer la categoría de la ruta
            if (!string.IsNullOrEmpty(script.FilePath))
            {
                // Buscar la carpeta "Scripts" en el path
                string scriptsFolder = "Scripts";
                int scriptsIndex = script.FilePath.LastIndexOf(scriptsFolder, StringComparison.OrdinalIgnoreCase);

                if (scriptsIndex >= 0)
                {
                    // Obtener la parte después de "Scripts"
                    string afterScripts = script.FilePath.Substring(scriptsIndex + scriptsFolder.Length);

                    // Remover separadores al inicio
                    afterScripts = afterScripts.TrimStart('\\', '/');

                    // Obtener solo la parte del directorio (sin el archivo)
                    string directoryPath = Path.GetDirectoryName(afterScripts);

                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        // Reemplazar separadores con / para consistencia
                        return directoryPath.Replace('\\', '/');
                    }
                }
            }

            // Si no se puede determinar la categoría del path, usar "General"
            return "General";
        }

        private void CreateCategoryHierarchy(string categoryPath)
        {
            if (string.IsNullOrEmpty(categoryPath))
            {
                categoryPath = "General";
            }

            // Dividir la ruta de categoría (ej: "walk/test" -> ["walk", "test"])
            var parts = categoryPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
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
                        // Categoría raíz
                        _mainMenu.AddItem(categoryItem);
                    }
                    else
                    {
                        // Subcategoría
                        parentItem.AddSubItem(categoryItem);
                    }

                    _categoryItems[currentPath] = categoryItem;
                }

                parentItem = _categoryItems[currentPath];
            }
        }

        private void AddScriptToCategory(AvalonScript script, string categoryPath)
        {
            if (string.IsNullOrEmpty(categoryPath))
            {
                categoryPath = "General";
            }

            // Normalizar el path de categoría
            string normalizedPath = NormalizeCategoryPath(categoryPath);

            if (!_categoryItems.TryGetValue(normalizedPath, out var categoryItem))
            {
                // Si la categoría no existe, crearla
                CreateCategoryHierarchy(normalizedPath);
                if (!_categoryItems.TryGetValue(normalizedPath, out categoryItem))
                {
                    // Fallback a General si aún no existe
                    if (!_categoryItems.TryGetValue("General", out categoryItem))
                    {
                        CreateCategoryHierarchy("General");
                        categoryItem = _categoryItems["General"];
                    }
                }
            }

            // Crear item del script
            var scriptItem = new MenuItem(script.Name)
            {
                IsEnabled = script.IsEnabled,
                HeaderBackgroundColor = Color.FromArgb(35, 35, 35),
                HeaderTextColor = Color.White
            };

            var contentItem = CreateControl(script);

            scriptItem.Content = contentItem;
            scriptItem.Height = contentItem.Height + 10;

            // Agregar a la categoría
            categoryItem.AddSubItem(scriptItem);
        }

        private string NormalizeCategoryPath(string categoryPath)
        {
            if (string.IsNullOrEmpty(categoryPath)) return "General";

            // Convertir separadores a formato consistente
            var parts = categoryPath.Split(new[] { '/', '\\', '>' }, StringSplitOptions.RemoveEmptyEntries);
            var normalizedParts = parts.Select(p => FormatCategoryName(p.Trim())).ToArray();

            return string.Join("/", normalizedParts);
        }

        private UIControl CreateControl(AvalonScript control)
        {
            switch (control.Type)
            {
                case ScriptControlType.Button:
                    var button = new Button();
                    button.Width = MenuWidth;
                    button.Text = control.Name;
                    button.Click += (sender, pos) => control.ChangeValue(true);
                    return button;

                case ScriptControlType.Slider:
                    var slider = new Slider();
                    slider.Width = MenuWidth;
                    slider.Text = control.Name;
                    slider.Value = Convert.ToSingle(control.Value);
                    slider.ValueChanged += (value) => control.ChangeValue(value);
                    return slider;

                default:
                    var checkBox = new CheckBox();
                    checkBox.Width = MenuWidth;
                    checkBox.Orientation = CheckBoxOrientation.Right;
                    checkBox.Text = control.Name;
                    checkBox.Checked = Convert.ToBoolean(control.Value);
                    checkBox.BackColor = Color.Red;
                    checkBox.CheckColor = Color.Green;
                    checkBox.CheckedChanged += (isChecked) => control.ChangeValue(isChecked);
                    return checkBox;
            }
        }

        private string FormatCategoryName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "General";
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(name.ToLower());
        }

        private void AdjustMenuSize()
        {
            float totalHeight = _mainMenu.ShowHeader ? _mainMenu.HeaderHeight + _mainMenu.SeparatorHeight : 0;
            totalHeight += _mainMenu.BorderWidth * 2;

            foreach (var item in _mainMenu.GetAllItems())
            {
                totalHeight += item.CalculateTotalHeight();
            }

            _mainMenu.Height = Math.Min(totalHeight, MaxMenuHeight);
        }

        #region RENDER
        public void Render()
        {
            if (!_isInitialized) return;

            // Toggle del menú con F1
            if (InputSystem.GetKeyDown(Keys.F1))
            {
                _mainMenu.Visible = !_mainMenu.Visible;
            }

            // Actualizar y dibujar
            _mainMenu.Update();
            _mainMenu.Draw();
            
            foreach(var script in MoonSharpScriptLoader.Scripts)
            {
                script.Update();
                script.Draw();
            }

            InputSystem.Update();
        }

        public void Shutdown()
        {
            _processedScripts.Clear();
            _isInitialized = false;
        }

        // Propiedades públicas
        public bool IsMenuVisible => _mainMenu?.Visible ?? false;
        public int ScriptCount => MoonSharpScriptLoader.Scripts.Count;
        public int CategoryCount => _categoryItems.Count;
        #endregion
    }
}