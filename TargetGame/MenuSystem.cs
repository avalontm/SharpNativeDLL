using AvalonInjectLib;
using AvalonInjectLib.UIFramework;
using static AvalonInjectLib.Structs;

namespace TargetGame
{
    public class MenuSystem : UIFrameworkRenderSystem
    {
        public Window? MainWindow { get; private set; }
        public bool IsInitialized { get; private set; }

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
                Title = "Ventana con Controles",
                Bounds = new Rect(100, 100, 500, 400),
                BackColor = new Color(240, 240, 240)
            };

            // Crear el TabControl
            var tabControl = new TabControl
            {
                X = 0,
                Y = 0,
                Width = 500,
                Height = 380
            };

            // Crear contenido para el primer tab
            var panel1 = new Panel();

            // Crear botón para el primer tab
            var boton = new Button
            {
                Text = "Botón en Tab 1",
                X = 50,
                Y = 50,
                Width = 120,
                Height = 30
            };

            // Crear checkbox para el primer tab
            var checkbox = new CheckBox
            {
                Text = "Habilitar función",
                X = 50,
                Y = 100,
                Checked = true,
                BoxColor = Color.FromArgb(70, 130, 180),  // SteelBlue
                CheckColor = Color.Green,
                HoverBoxColor = Color.FromArgb(100, 149, 237),  // CornflowerBlue
                PressedBoxColor = Color.FromArgb(30, 144, 255)  // DodgerBlue
            };

            // Evento cuando cambia el estado del checkbox
            checkbox.CheckedChanged += (isChecked) => {
                Console.WriteLine($"Checkbox cambiado a: {isChecked}");
                boton.Enabled = isChecked;  // Ejemplo: deshabilitar botón cuando el checkbox está desmarcado
            };

            // Manejar evento click del botón
            boton.Click += (pos) => {
                Console.WriteLine("Botón clickeado! Checkbox está " + (checkbox.Checked ? "activado" : "desactivado"));
            };

            var slider = new Slider
            {
                Text = "Nivel de volumen",
                X = 50,
                Y = 150,  // Posición debajo del checkbox
                Width = 200,
                Value = 75,  // Valor inicial (75%)
                MinValue = 0,
                MaxValue = 100,
                TrackColor = Color.FromArgb(70, 70, 70),
                FillColor = Color.FromArgb(0, 122, 204),  // Azul más oscuro
                ThumbColor = Color.White,
                ThumbHoverColor = Color.FromArgb(220, 220, 220),
                ShowValue = true  // Mostrar el valor numérico
            };

            // Evento cuando cambia el valor del slider
            slider.ValueChanged += (value) => {
                Console.WriteLine($"Volumen ajustado a: {value}%");
                // Aquí podrías agregar lógica para ajustar el volumen del juego
            };

            // Crear ComboBox
            var comboBox = new ComboBox
            {
                X = 50,
                Y = 220,  // Posición debajo del slider
                Width = 150,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                DropDownColor = Color.FromArgb(37, 37, 38),
                HighlightColor = Color.FromArgb(0, 122, 204),
                BorderColor = Color.FromArgb(100, 100, 100)
            };

            // Agregar items
            for (int i = 1; i <= 10; i++)
            {
                comboBox.AddItem($"Opcion {i}");
            }

            // Evento cuando cambia la selección
            comboBox.SelectionChanged += (index) => {
                Console.WriteLine($"Seleccionado: {comboBox.SelectedItem} (índice {index})");
            };

            panel1.AddChild(comboBox);

            // Agregar slider al panel
            panel1.AddChild(slider);

            // Agregar controles al panel
            panel1.AddChild(boton);
            panel1.AddChild(checkbox);

            // Crear contenido para el segundo tab
            var panel2 = new Panel();

            // Agregar tabs
            tabControl.AddTab("Opciones", panel1);
            tabControl.AddTab("Configuración", panel2);

            // Manejar eventos de cambio de tab
            tabControl.TabSelected += (index) => {
                Console.WriteLine($"Tab seleccionado: {index}");
            };

            // Configurar ventana
            MainWindow.Content = tabControl;
            MainWindow.Show();
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
    }
}