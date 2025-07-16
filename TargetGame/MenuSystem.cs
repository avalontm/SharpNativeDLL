using AvalonInjectLib;
using AvalonInjectLib.UIFramework;
using static AvalonInjectLib.Structs;
using System;

namespace TargetGame
{
    public class MenuSystem : UIFrameworkRenderSystem
    {
        public Window? MainWindow { get; private set; }
        public bool IsInitialized { get; private set; }
        private MenuList _mainMenu;
        private Panel _contentPanel;
        private Label _statusLabel;
        private Button _toggleButton;
        private CheckBox _enableCheckbox;
        private Slider _valueSlider;
        private TextBox _idTextBox;

        public void Initialize(uint processId)
        {
            if (IsInitialized) return;

            Font.Initialize();
            InputSystem.Initialize(processId);
            CreateControls();
            IsInitialized = true;
        }

        private void CreateControls()
        {
            // Crear ventana principal
            MainWindow = new Window
            {
                Title = "Menu System - Skinchanger",
                Bounds = new Rect(100, 100, 800, 600),
                BackColor = new Color(30, 30, 30)
            };

            // Crear el menú principal (lado izquierdo)
            CreateMainMenu();

            // Crear panel de contenido (lado derecho)
            CreateContentPanel();

            // Configurar ventana
            var mainPanel = new Panel
            {
                X = 0,
                Y = 0,
                Width = 800,
                Height = 600,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            mainPanel.AddChild(_mainMenu);
            mainPanel.AddChild(_contentPanel);

            MainWindow.Content = mainPanel;
            MainWindow.Show();
        }

        private void CreateMainMenu()
        {
            _mainMenu = new MenuList
            {
                X = 10,
                Y = 10,
                Width = 280,
                Height = 580,
                BackgroundColor = Color.FromArgb(35, 35, 35),
                BorderColor = Color.FromArgb(100, 100, 100),
                ItemHeight = 25f,
                ShowBorder = true
            };

            // Suscribirse a eventos del menú
            _mainMenu.OnItemSelected += OnMenuItemSelected;
            _mainMenu.OnItemExpanded += OnMenuItemExpanded;
            _mainMenu.OnItemCollapsed += OnMenuItemCollapsed;

            // Crear la estructura del menú
            CreateMenuStructure();
        }

        private void CreateMenuStructure()
        {
            // Crear elementos principales
            var softHub = CreateMenuItem("Soft[HUB]", Color.FromArgb(255, 165, 0));
            var orbwalker = CreateMenuItem("Orbwalker");
            var evade = CreateMenuItem("Evade");
            var summonerSpells = CreateMenuItem("Summoner Spells");
            var information = CreateMenuItem("Information");
            var champions = CreateMenuItem("Champions");
            var skinchanger = CreateMenuItem("Skinchanger", Color.FromArgb(255, 165, 0));
            var memory = CreateMenuItem("Memory");
            var settings = CreateMenuItem("Settings");
            var veigar = CreateMenuItem("Veigar");

            // Agregar elementos raíz
            _mainMenu.AddRootItem(softHub);
            _mainMenu.AddRootItem(orbwalker);
            _mainMenu.AddRootItem(evade);
            _mainMenu.AddRootItem(summonerSpells);
            _mainMenu.AddRootItem(information);
            _mainMenu.AddRootItem(champions);
            _mainMenu.AddRootItem(skinchanger);
            _mainMenu.AddRootItem(memory);
            _mainMenu.AddRootItem(settings);
            _mainMenu.AddRootItem(veigar);

            // Configurar Skinchanger
            CreateSkinchanGerMenu(skinchanger);
            skinchanger.Expand();
            _mainMenu.SetSelectedItem(skinchanger);
        }

        private void CreateSkinchanGerMenu(MenuItem skinchanger)
        {
            // Agregar advertencia
            var warningItem = CreateMenuItem("Use at your own risk!");
            warningItem.ForeColor = Color.FromArgb(255, 165, 0);
            skinchanger.AddChild(warningItem);

            // Crear subelementos
            var champion = CreateMenuItem("Champion");
            var wards = CreateMenuItem("Wards");
            var turettes = CreateMenuItem("Turettes");
            var laneMinions = CreateMenuItem("Lane Minions");

            skinchanger.AddChild(champion);
            skinchanger.AddChild(wards);
            skinchanger.AddChild(turettes);
            skinchanger.AddChild(laneMinions);

            // Configurar Lane Minions
            CreateLaneMinionsMenu(laneMinions);
            laneMinions.Expand();
            _mainMenu.SetSelectedItem(laneMinions);
        }

        private void CreateLaneMinionsMenu(MenuItem laneMinions)
        {
            // Crear configuración de Enable
            var enable = CreateMenuItem("Enable [ON]");
            enable.ForeColor = Color.FromArgb(0, 255, 0); // Verde para ON
            enable.Tag = "enable_toggle";

            // Crear configuración de minions
            var blueMinions = CreateMenuItem("Blue Lane Minions ID: 8");
            blueMinions.ShowWarning = true;
            blueMinions.WarningText = "⚠";
            blueMinions.Tag = "blue_minions";

            var redMinions = CreateMenuItem("Red Lane Minions ID: 0");
            redMinions.ShowWarning = true;
            redMinions.WarningText = "⚠";
            redMinions.Tag = "red_minions";

            laneMinions.AddChild(enable);
            laneMinions.AddChild(blueMinions);
            laneMinions.AddChild(redMinions);
        }

        private MenuItem CreateMenuItem(string text, Color? selectedColor = null)
        {
            return new MenuItem(text)
            {
                ForeColor = Color.White,
                NormalColor = Color.FromArgb(45, 45, 45),
                HoverColor = Color.FromArgb(60, 60, 60),
                SelectedColor = selectedColor ?? Color.FromArgb(255, 165, 0),
                Font = Font.GetDefaultFont()
            };
        }

        private void CreateContentPanel()
        {
            _contentPanel = new Panel
            {
                X = 300,
                Y = 10,
                Width = 480,
                Height = 580,
                BackColor = Color.FromArgb(40, 40, 40),
                BorderColor = Color.FromArgb(100, 100, 100),
                BorderWidth = 1f,
                ShowBorder = true
            };

            // Título del panel
            var titleLabel = new Label
            {
                Text = "Configuration Panel",
                X = 10,
                Y = 10,
                Width = 460,
                Height = 30,
                ForeColor = Color.White,
                Font = Font.GetDefaultFont(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Label de estado
            _statusLabel = new Label
            {
                Text = "Select an option from the menu",
                X = 10,
                Y = 50,
                Width = 460,
                Height = 20,
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = Font.GetDefaultFont()
            };

            // Botón de toggle
            _toggleButton = new Button
            {
                Text = "Toggle ON/OFF",
                X = 10,
                Y = 80,
                Width = 120,
                Height = 30,
                BackColor = Color.FromArgb(0, 122, 204),
                Visible = false
            };

            _toggleButton.Click += (pos) => {
                var selectedItem = _mainMenu.SelectedItem;
                if (selectedItem?.Tag?.ToString() == "enable_toggle")
                {
                    ToggleEnableState(selectedItem);
                }
            };

            // Checkbox de habilitación
            _enableCheckbox = new CheckBox
            {
                Text = "Enable Feature",
                X = 10,
                Y = 120,
                Width = 150,
                Height = 20,
                BoxColor = Color.FromArgb(70, 130, 180),
                CheckColor = Color.Green,
                HoverBoxColor = Color.FromArgb(100, 149, 237),
                PressedBoxColor = Color.FromArgb(30, 144, 255),
                Visible = false
            };

            _enableCheckbox.CheckedChanged += (isChecked) => {
                UpdateConfigurationStatus(isChecked);
            };

            // Slider para valores
            _valueSlider = new Slider
            {
                Text = "Minion ID",
                X = 10,
                Y = 160,
                Width = 200,
                Height = 25,
                Value = 8,
                MinValue = 0,
                MaxValue = 20,
                TrackColor = Color.FromArgb(70, 70, 70),
                FillColor = Color.FromArgb(0, 122, 204),
                ThumbColor = Color.White,
                ThumbHoverColor = Color.FromArgb(220, 220, 220),
                ShowValue = true,
                Visible = false
            };

            _valueSlider.ValueChanged += (value) => {
                UpdateMinionId((int)value);
            };

            // TextBox para ID personalizado
            _idTextBox = new TextBox
            {
                X = 10,
                Y = 200,
                Width = 100,
                Height = 25,
                PlaceholderText = "Custom ID...",
                BackColor = Color.White,
                ForeColor = Color.Black,
                BorderColor = Color.FromArgb(100, 100, 100),
                BorderColorFocus = Color.FromArgb(0, 122, 204),
                MaxLength = 10,
                Visible = false
            };

            _idTextBox.TextChanged += (text) => {
                if (int.TryParse(text, out int id))
                {
                    UpdateCustomMinionId(id);
                }
            };

            // Agregar controles al panel
            _contentPanel.AddChild(titleLabel);
            _contentPanel.AddChild(_statusLabel);
            _contentPanel.AddChild(_toggleButton);
            _contentPanel.AddChild(_enableCheckbox);
            _contentPanel.AddChild(_valueSlider);
            _contentPanel.AddChild(_idTextBox);
        }

        // Manejadores de eventos del menú
        private void OnMenuItemSelected(MenuItem item)
        {
            Console.WriteLine($"Menu item selected: {item.Text}");

            // Ocultar todos los controles de configuración
            HideAllConfigControls();

            // Mostrar controles específicos según el elemento seleccionado
            ShowConfigForItem(item);
        }

        private void OnMenuItemExpanded(MenuItem item)
        {
            Console.WriteLine($"Menu item expanded: {item.Text}");
            _mainMenu.ScrollToItem(item);
        }

        private void OnMenuItemCollapsed(MenuItem item)
        {
            Console.WriteLine($"Menu item collapsed: {item.Text}");
        }

        private void ShowConfigForItem(MenuItem item)
        {
            _statusLabel.Text = $"Configuring: {item.Text}";

            switch (item.Tag?.ToString())
            {
                case "enable_toggle":
                    _toggleButton.Visible = true;
                    _enableCheckbox.Visible = true;
                    _statusLabel.Text = "Enable or disable the feature";
                    break;

                case "blue_minions":
                    _valueSlider.Visible = true;
                    _idTextBox.Visible = true;
                    _valueSlider.Value = 8;
                    _statusLabel.Text = "Configure Blue Lane Minions ID";
                    break;

                case "red_minions":
                    _valueSlider.Visible = true;
                    _idTextBox.Visible = true;
                    _valueSlider.Value = 0;
                    _statusLabel.Text = "Configure Red Lane Minions ID";
                    break;

                default:
                    _statusLabel.Text = $"Selected: {item.Text}";
                    break;
            }
        }

        private void HideAllConfigControls()
        {
            _toggleButton.Visible = false;
            _enableCheckbox.Visible = false;
            _valueSlider.Visible = false;
            _idTextBox.Visible = false;
        }

        private void ToggleEnableState(MenuItem enableItem)
        {
            if (enableItem.Text.Contains("[ON]"))
            {
                enableItem.Text = enableItem.Text.Replace("[ON]", "[OFF]");
                enableItem.ForeColor = Color.FromArgb(255, 0, 0); // Rojo para OFF
                _enableCheckbox.Checked = false;
            }
            else
            {
                enableItem.Text = enableItem.Text.Replace("[OFF]", "[ON]");
                enableItem.ForeColor = Color.FromArgb(0, 255, 0); // Verde para ON
                _enableCheckbox.Checked = true;
            }

            Console.WriteLine($"Feature toggled: {enableItem.Text}");
        }

        private void UpdateConfigurationStatus(bool isEnabled)
        {
            _statusLabel.Text = $"Configuration {(isEnabled ? "enabled" : "disabled")}";
            Console.WriteLine($"Configuration changed: {isEnabled}");
        }

        private void UpdateMinionId(int id)
        {
            var selectedItem = _mainMenu.SelectedItem;
            if (selectedItem?.Tag?.ToString() == "blue_minions")
            {
                selectedItem.Text = $"Blue Lane Minions ID: {id}";
            }
            else if (selectedItem?.Tag?.ToString() == "red_minions")
            {
                selectedItem.Text = $"Red Lane Minions ID: {id}";
            }

            Console.WriteLine($"Minion ID updated to: {id}");
        }

        private void UpdateCustomMinionId(int id)
        {
            _valueSlider.Value = id;
            Console.WriteLine($"Custom Minion ID set to: {id}");
        }

        public void Render()
        {
            if (IsInitialized && MainWindow != null)
            {
                MainWindow.Update();
                MainWindow.Draw();
            }
        }

        public void Shutdown()
        {
            MainWindow?.Close();
            MainWindow = null;
            IsInitialized = false;
        }

        // Métodos públicos para interactuar con el menú
        public void SelectMenuItem(string itemName)
        {
            var item = _mainMenu.FindItem(itemName);
            if (item != null)
            {
                _mainMenu.SetSelectedItem(item);
            }
        }

        public void ExpandMenuItem(string itemName)
        {
            var item = _mainMenu.FindItem(itemName);
            if (item != null)
            {
                item.Expand();
            }
        }

        public void CollapseMenuItem(string itemName)
        {
            var item = _mainMenu.FindItem(itemName);
            if (item != null)
            {
                item.Collapse();
            }
        }

        public MenuItem GetSelectedItem()
        {
            return _mainMenu.SelectedItem;
        }

        public void ScrollToItem(string itemName)
        {
            var item = _mainMenu.FindItem(itemName);
            if (item != null)
            {
                _mainMenu.ScrollToItem(item);
            }
        }
    }
}