using static AvalonInjectLib.Structs;
using static AvalonInjectLib.UIFramework;

namespace AssaultCube
{
    public static class MenuSystem
    {
        private static Window _mainWindow;
        private static TabControl _tabControl;

        // Controles para las diferentes funcionalidades
        private static Checkbox _godModeCheckbox;
        private static Checkbox _noClipCheckbox;
        private static Slider _speedSlider;
        private static Checkbox _aimbotCheckbox;
        private static Slider _aimbotFovSlider;
        private static Slider _aimbotSmoothSlider;
        private static Checkbox _espPlayersCheckbox;
        private static Checkbox _espNamesCheckbox;
        private static Checkbox _espHealthCheckbox;
        private static Checkbox _espDistanceCheckbox;
        private static ComboBox _themeComboBox;

        public static void Initialize()
        {
            // Configuración de la ventana principal
            _mainWindow = new Window
            {
                IsVisible = true,
                Bounds = new Rect(50, 50, 500, 400),
                Title = "AC HACK v2.0",
                BackgroundColor = Themes.Dark.Surface,
                TitleBarColor = Themes.Dark.Primary,
                BorderColor = Themes.Dark.Border,
                ShowCloseButton = true,
                IsDraggable = true,
                MinSize = new Vector2(400, 300)
            };

            // Crear el control de pestañas
            _tabControl = new TabControl
            {
                Bounds = new Rect(10, 10, _mainWindow.Bounds.Width - 20, _mainWindow.Bounds.Height - 20),
                BackgroundColor = Themes.Dark.Surface
            };

            // Añadir pestañas
            AddPlayerTab();
            AddAimbotTab();
            AddESPTab();
            AddVisualTab();
            AddConfigTab();

            _mainWindow.AddControl(_tabControl);

            // Configurar eventos
            _mainWindow.OnClosed += () => { /* Lógica para cerrar la aplicación si es necesario */ };
        }

        private static void AddPlayerTab()
        {
            var playerTab = new TabPage { Title = "Player" };

            var playerGroup = new GroupBox
            {
                Title = "Player Modifications",
                Bounds = new Rect(10, 10, 220, 150),
                BackgroundColor = Themes.Dark.Surface,
                BorderColor = Themes.Dark.Border
            };

            _godModeCheckbox = new Checkbox
            {
                Bounds = new Rect(10, 30, 200, 20),
                Text = "God Mode",
                TextColor = Themes.Dark.Text,
                IsChecked = false,
                OnCheckedChanged = (isChecked) => { /* Implementar lógica de God Mode */ }
            };

            _noClipCheckbox = new Checkbox
            {
                Bounds = new Rect(10, 60, 200, 20),
                Text = "No Clip",
                TextColor = Themes.Dark.Text,
                IsChecked = false,
                OnCheckedChanged = (isChecked) => { /* Implementar lógica de No Clip */ }
            };

            _speedSlider = new Slider
            {
                Bounds = new Rect(10, 90, 200, 40),
                Label = "Speed Multiplier",
                MinValue = 1,
                MaxValue = 10,
                Value = 1,
                DecimalPlaces = 1,
                FillColor = Themes.Dark.Primary,
                EmptyColor = Themes.Dark.Hover,
                //TextColor = Themes.Dark.Text,
                OnValueChanged = (value) => { /* Implementar cambio de velocidad */ }
            };

            playerGroup.AddControl(_godModeCheckbox);
            playerGroup.AddControl(_noClipCheckbox);
            playerGroup.AddControl(_speedSlider);
            playerTab.AddControl(playerGroup);

            _tabControl.AddTab(playerTab);
        }

        private static void AddAimbotTab()
        {
            var aimbotTab = new TabPage { Title = "Aimbot" };

            var aimbotGroup = new GroupBox
            {
                Title = "Aimbot Settings",
                Bounds = new Rect(10, 10, 220, 180),
                BackgroundColor = Themes.Dark.Surface,
                BorderColor = Themes.Dark.Border
            };

            _aimbotCheckbox = new Checkbox
            {
                Bounds = new Rect(10, 30, 200, 20),
                Text = "Enable Aimbot",
                TextColor = Themes.Dark.Text,
                IsChecked = false,
                OnCheckedChanged = (isChecked) => { /* Implementar activación de aimbot */ }
            };

            _aimbotFovSlider = new Slider
            {
                Bounds = new Rect(10, 60, 200, 40),
                Label = "Aimbot FOV",
                MinValue = 1,
                MaxValue = 180,
                Value = 30,
                FillColor = Themes.Dark.Primary,
                EmptyColor = Themes.Dark.Hover,
                //TextColor = Themes.Dark.Text,
                OnValueChanged = (value) => { /* Implementar cambio de FOV */ }
            };

            _aimbotSmoothSlider = new Slider
            {
                Bounds = new Rect(10, 110, 200, 40),
                Label = "Smoothness",
                MinValue = 1,
                MaxValue = 100,
                Value = 30,
                FillColor = Themes.Dark.Primary,
                EmptyColor = Themes.Dark.Hover,
                //TextColor = Themes.Dark.Text,
                OnValueChanged = (value) => { /* Implementar cambio de suavidad */ }
            };

            aimbotGroup.AddControl(_aimbotCheckbox);
            aimbotGroup.AddControl(_aimbotFovSlider);
            aimbotGroup.AddControl(_aimbotSmoothSlider);
            aimbotTab.AddControl(aimbotGroup);

            _tabControl.AddTab(aimbotTab);
        }

        private static void AddESPTab()
        {
            var espTab = new TabPage { Title = "ESP" };

            var espGroup = new GroupBox
            {
                Title = "ESP Settings",
                Bounds = new Rect(10, 10, 220, 180),
                BackgroundColor = Themes.Dark.Surface,
                BorderColor = Themes.Dark.Border
            };

            _espPlayersCheckbox = new Checkbox
            {
                Bounds = new Rect(10, 30, 200, 20),
                Text = "Player ESP",
                TextColor = Themes.Dark.Text,
                IsChecked = false,
                OnCheckedChanged = (isChecked) => { /* Implementar ESP de jugadores */ }
            };

            _espNamesCheckbox = new Checkbox
            {
                Bounds = new Rect(10, 60, 200, 20),
                Text = "Show Names",
                TextColor = Themes.Dark.Text,
                IsChecked = false,
                OnCheckedChanged = (isChecked) => { /* Implementar nombres en ESP */ }
            };

            _espHealthCheckbox = new Checkbox
            {
                Bounds = new Rect(10, 90, 200, 20),
                Text = "Show Health",
                TextColor = Themes.Dark.Text,
                IsChecked = false,
                OnCheckedChanged = (isChecked) => { /* Implementar salud en ESP */ }
            };

            _espDistanceCheckbox = new Checkbox
            {
                Bounds = new Rect(10, 120, 200, 20),
                Text = "Show Distance",
                TextColor = Themes.Dark.Text,
                IsChecked = false,
                OnCheckedChanged = (isChecked) => { /* Implementar distancia en ESP */ }
            };

            espGroup.AddControl(_espPlayersCheckbox);
            espGroup.AddControl(_espNamesCheckbox);
            espGroup.AddControl(_espHealthCheckbox);
            espGroup.AddControl(_espDistanceCheckbox);
            espTab.AddControl(espGroup);

            _tabControl.AddTab(espTab);
        }

        private static void AddVisualTab()
        {
            var visualTab = new TabPage { Title = "Visual" };

            var crosshairGroup = new GroupBox
            {
                Title = "Crosshair Settings",
                Bounds = new Rect(10, 10, 220, 120),
                BackgroundColor = Themes.Dark.Surface,
                BorderColor = Themes.Dark.Border
            };

            var crosshairCheckbox = new Checkbox
            {
                Bounds = new Rect(10, 30, 200, 20),
                Text = "Custom Crosshair",
                TextColor = Themes.Dark.Text,
                IsChecked = false,
                OnCheckedChanged = (isChecked) => { /* Implementar crosshair personalizado */ }
            };

            var crosshairColorButton = new Button
            {
                Bounds = new Rect(10, 60, 200, 25),
                Text = "Crosshair Color",
                BackgroundColor = Themes.Dark.Primary,
                TextColor = Themes.Dark.Text,
                OnClick = (pos) => { /* Implementar selector de color */ }
            };

            crosshairGroup.AddControl(crosshairCheckbox);
            crosshairGroup.AddControl(crosshairColorButton);
            visualTab.AddControl(crosshairGroup);

            _tabControl.AddTab(visualTab);
        }

        private static void AddConfigTab()
        {
            var configTab = new TabPage { Title = "Config" };

            var settingsGroup = new GroupBox
            {
                Title = "Settings",
                Bounds = new Rect(10, 10, 220, 180),
                BackgroundColor = Themes.Dark.Surface,
                BorderColor = Themes.Dark.Border
            };

            var saveButton = new Button
            {
                Bounds = new Rect(10, 30, 200, 25),
                Text = "Save Config",
                BackgroundColor = Themes.Dark.Primary,
                TextColor = Themes.Dark.Text,
                OnClick = (pos) => { /* Implementar guardado de configuración */ }
            };

            var loadButton = new Button
            {
                Bounds = new Rect(10, 65, 200, 25),
                Text = "Load Config",
                BackgroundColor = Themes.Dark.Primary,
                TextColor = Themes.Dark.Text,
                OnClick = (pos) => { /* Implementar carga de configuración */ }
            };

            _themeComboBox = new ComboBox
            {
                Bounds = new Rect(10, 100, 200, 25),
                Items = new List<string> { "Dark Theme", "Light Theme" },
                SelectedIndex = 0,
                TextColor = Themes.Dark.Text,
                DropdownColor = Themes.Dark.Surface,
                SelectedColor = Themes.Dark.Primary,
                OnSelectionChanged = (index, text) => { ChangeTheme(index); }
            };

            var themeLabel = new Label
            {
                Bounds = new Rect(10, 130, 200, 20),
                Text = "UI Theme:",
                TextColor = Themes.Dark.Text
            };

            settingsGroup.AddControl(saveButton);
            settingsGroup.AddControl(loadButton);
            settingsGroup.AddControl(_themeComboBox);
            settingsGroup.AddControl(themeLabel);
            configTab.AddControl(settingsGroup);

            _tabControl.AddTab(configTab);
        }

        private static void ChangeTheme(int themeIndex)
        {

        }

        public static void Render()
        {
            if (_mainWindow.IsVisible)
            {
                _mainWindow.Update();
                _mainWindow.Draw();
            }
        }

        public static void ToggleMenu()
        {
            _mainWindow.IsVisible = !_mainWindow.IsVisible;
        }

        public static void Shutdown()
        {
            // Limpieza si es necesaria
        }
    }
}