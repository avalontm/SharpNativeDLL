using static AvalonInjectLib.UIFramework;

namespace SharpNativeDLL
{
    public static class MenuSystem
    {
        private static Window _mainWindow;
        private static float _mouseX, _mouseY;
        private static bool _mouseDown;

        public static void Initialize()
        {
            _mainWindow = new Window
            {
                IsVisible = true,
                Bounds = new Rect { X = 50, Y = 50, Width = 300, Height = 200 },
                Title = "HACK MENU",
                BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f),
                Controls = new UIControl[]
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
                        Min = 1,
                        Max = 10,
                        Value = 5
                    }
                }
            };
        }

        public static void HandleInput(nint hWnd, uint msg, nint wParam, nint lParam)
        {
            switch (msg)
            {
                case 0x0201: // WM_LBUTTONDOWN
                    _mouseDown = true;
                    UpdateMousePos(lParam);
                    break;

                case 0x0202: // WM_LBUTTONUP
                    _mouseDown = false;
                    break;

                case 0x0200: // WM_MOUSEMOVE
                    UpdateMousePos(lParam);
                    break;

                case 0x0100 when wParam.ToInt32() == 0x76: // VK_F7
                    _mainWindow.IsVisible = !_mainWindow.IsVisible;
                    break;
            }
        }

        private static void UpdateMousePos(nint lParam)
        {
            _mouseX = (short)(lParam.ToInt32() & 0xFFFF);
            _mouseY = (short)(lParam.ToInt32() >> 16 & 0xFFFF);
        }

        public static void Render()
        {
            _mainWindow.Update(_mouseX, _mouseY, _mouseDown);
            _mainWindow.Draw();
        }

        private static void ToggleGodMode()
        {

        }
    }
}

