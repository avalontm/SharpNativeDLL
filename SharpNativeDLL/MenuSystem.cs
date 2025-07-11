using System.Numerics;
using static AvalonInjectLib.UIFramework;

namespace SharpNativeDLL
{
    public static class MenuSystem
    {
        private static Window _mainWindow;

        public static void Initialize()
        {
            _mainWindow = new Window
            {
                IsVisible = true,
                Bounds = new Rect { X = 50, Y = 50, Width = 300, Height = 200 },
                Title = "HACK MENU",
                BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f),
                Controls = new List<UIControl>
                {
                    new Button
                    {
                        Bounds = new Rect { X = 20, Y = 40, Width = 120, Height = 30 },
                        Text = "God Mode",
                        OnClick = ToggleGodMode
                    },
                    new Slider
                    {
                        Bounds = new Rect { X = 20, Y = 80, Width = 150, Height = 40 },
                        Label = "Velocidad",
                        MinValue = 1,
                        MaxValue = 10,
                        Value = 5
                    }
                }
            };
        }

        private static void ToggleGodMode(Vector2 vector)
        {
           
        }

        public static void Render()
        {
            _mainWindow.Update();
            _mainWindow.Draw();
        }

    }
}

