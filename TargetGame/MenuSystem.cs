using AvalonInjectLib;
using AvalonInjectLib.UIFramework;
using static AvalonInjectLib.Structs;

namespace TargetGame
{
    public class MenuSystem : UIFrameworkRenderSystem
    {
        public Window? MainWindow { set; get; }
        public bool IsInitialized { set; get; }

        public void Initialize(uint processId)
        {
            if (IsInitialized) return;

            InputSystem.Initialize(processId);

            CreateMainWindow();

            IsInitialized = true;
        }

        private void CreateMainWindow()
        {
            // Crear ventana
            MainWindow = new Window
            {
                Title = "Mi Aplicacion",
                MinSize = new Vector2(300, 200),
                MaxSize = new Vector2(800, 600),
                Bounds = new Rect(100, 100, 400, 300)
            };

            // Crear grid con 3 filas y 3 columnas para facilitar el centrado
            var grid = new Grid()
            {
                BackgroundColor = new Color(0, 0, 0, 50)
            };

            // Configurar columnas
            grid.ColumnDefinitions.Add(new GridLength(1, GridUnitType.Star)); // Columna izquierda
            grid.ColumnDefinitions.Add(new GridLength(0, GridUnitType.Auto)); // Columna central
            grid.ColumnDefinitions.Add(new GridLength(1, GridUnitType.Star)); // Columna derecha

            // Configurar filas
            grid.RowDefinitions.Add(new GridLength(1, GridUnitType.Star));    // Fila superior
            grid.RowDefinitions.Add(new GridLength(0, GridUnitType.Auto));    // Fila central
            grid.RowDefinitions.Add(new GridLength(1, GridUnitType.Star));    // Fila inferior

            // Crear y configurar el título PRIMERO
            var titleLabel = new Label
            {
                Text = "Bienvenido",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10),
                BackgroundColor = new Color(255, 0, 0, 100) // Fondo rojo semi-transparente para debug
            };

            // Configurar posiciones usando métodos estáticos
            Grid.SetRow(titleLabel, 0);
            Grid.SetColumn(titleLabel, 0);
            Grid.SetColumnSpan(titleLabel, 3);

            // Crear y configurar el botón centrado
            var centeredButton = new Button
            {
                Text = "Boton Centrado",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5),
                Width = 120,
                Height = 48,
                BackgroundColor = new Color(0, 255, 0, 100) // Fondo verde semi-transparente para debug
            };

            // Posicionar el botón en la celda central (columna 1, fila 1)
            Grid.SetRow(centeredButton, 1);
            Grid.SetColumn(centeredButton, 1);

            // Configurar evento de clic para el botón
            centeredButton.OnClick += (point) =>
            {
                Console.WriteLine("Boton centrado fue clickeado!");
                // Puedes agregar aquí cualquier lógica adicional
            };

            // Agregar un label de debug para verificar las otras celdas
            var debugLabel = new Label
            {
                Text = "Debug (2,2)",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                BackgroundColor = new Color(0, 0, 255, 100) // Fondo azul semi-transparente para debug
            };

            Grid.SetRow(debugLabel, 2);
            Grid.SetColumn(debugLabel, 2);

            grid.AddChild(titleLabel);
            grid.AddChild(centeredButton);
            grid.AddChild(debugLabel);

            // Asignar el grid como contenido de la ventana
            MainWindow.Content = grid;

            // Configurar eventos de ventana
            MainWindow.OnClosing += () =>
            {
                Console.WriteLine("Cerrando ventana...");
            };

            MainWindow.OnClosed += () =>
            {
                Console.WriteLine("Ventana cerrada");
            };

            // Centrar la ventana en la pantalla
            MainWindow.CenterOnScreen();

            // Mostrar ventana
            MainWindow.Show();
        }

        public void Render()
        {
            if (IsInitialized)
            {
                MainWindow.Update();
                MainWindow.Draw();
            }
        }

        public void Shutdown()
        {
            MainWindow = null;
            IsInitialized = false;
        }
    }
}