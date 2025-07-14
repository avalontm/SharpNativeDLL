namespace AvalonInjectLib.UIFramework
{
    using static AvalonInjectLib.Structs;

    public class Window : UIControl
    {
        public string Title { get; set; } = "";
        public List<UIControl> Controls { get; } = new List<UIControl>();
        public Color TitleBarColor { get; set; } = new Color(51, 102, 204);
        public Color BorderColor { get; set; } = new Color(77, 77, 77);
        public float BorderThickness { get; set; } = 2f;
        public bool IsDraggable { get; set; } = true;
        public bool IsResizable { get; set; } = false;
        public bool ShowCloseButton { get; set; } = true;
        public bool ShowMinimizeButton { get; set; } = false;
        public bool ShowMaximizeButton { get; set; } = false;
        public Vector2 MinSize { get; set; } = new Vector2(100, 50);
        public Vector2 MaxSize { get; set; } = new Vector2(float.MaxValue, float.MaxValue);
        public float TitleBarHeight { get; set; } = 30f;
        public bool IsModal { get; private set; } = false;
        public bool IsOpen { get; private set; } = false;

        private bool _isDragging = false;
        private bool _isResizing = false;
        private Vector2 _dragOffset;
        private Button _closeButton;
        private Button _minimizeButton;
        private Button _maximizeButton;
        private bool _isMinimized = false;
        private bool _isMaximized = false;
        private Rect _restoreRect;
        private const float ResizeHandleSize = 10f;

        private UIControl _content;
        private bool _closing = false;

        public UIControl Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    if (_content != null)
                    {
                        Controls.Remove(_content);
                    }
                    _content = value;
                    if (_content != null)
                    {
                        Controls.Add(_content);
                        _content.Parent = this;
                    }
                }
            }
        }

        public Action OnClosing;

        public Window()
        {
            Bounds = new Rect(0, 0, 300, 300);
            BackgroundColor = new Color(45, 45, 45);
            Padding = new Thickness(5); 

            // Configurar botones
            _closeButton = new Button()
            {
                Text = "×",
                BackgroundColor = new Color(232, 17, 35),
                TextColor = Color.White,
                OnClick = _ => Close()
            };

            _minimizeButton = new Button()
            {
                Text = "−",
                BackgroundColor = new Color(128, 128, 128),
                TextColor = Color.White,
                OnClick = _ => Minimize()
            };

            _maximizeButton = new Button()
            {
                Text = "□",
                BackgroundColor = new Color(128, 128, 128),
                TextColor = Color.White,
                OnClick = _ => Maximize()
            };
        }

        public override void Draw()
        {
            if (!IsOpen) return;

            // Dibujar fondo de la ventana
            Renderer.DrawRect(Bounds, BackgroundColor);

            // Dibujar borde
            Renderer.DrawRectOutline(Bounds, BorderColor, BorderThickness);

            // Dibujar barra de título
            var titleBarRect = new Rect(Bounds.X, Bounds.Y, Bounds.Width, TitleBarHeight);
            Renderer.DrawRect(titleBarRect, TitleBarColor);

            // Dibujar título
            if (!string.IsNullOrEmpty(Title))
            {
                Renderer.DrawText(Title, Bounds.X + 10, Bounds.Y + 8, Color.White, 14);
            }

            // Dibujar botones de la ventana
            if (ShowCloseButton || ShowMinimizeButton || ShowMaximizeButton)
            {
                float buttonY = Bounds.Y + (TitleBarHeight - 20) / 2;
                float buttonSize = 20f;
                float buttonSpacing = 5f;
                float buttonX = Bounds.X + Bounds.Width - buttonSize - 5;

                if (ShowCloseButton)
                {
                    _closeButton.SetBounds(buttonX, buttonY, buttonSize, buttonSize);
                    _closeButton.Draw();
                    buttonX -= buttonSize + buttonSpacing;
                }

                if (ShowMaximizeButton && !_isMaximized)
                {
                    _maximizeButton.SetBounds(buttonX, buttonY, buttonSize, buttonSize);
                    _maximizeButton.Draw();
                    buttonX -= buttonSize + buttonSpacing;
                }

                if (ShowMinimizeButton)
                {
                    _minimizeButton.SetBounds(buttonX, buttonY, buttonSize, buttonSize);
                    _minimizeButton.Draw();
                }
            }

            // Dibujar contenido solo si no está minimizada
            if (!_isMinimized && Content != null)
            {
                // Calcular área de contenido (restando barra de título y padding)
                var contentRect = new Rect(
                    Bounds.X + Padding.Left,
                    Bounds.Y + TitleBarHeight + Padding.Top,
                    Math.Max(0, Bounds.Width - Padding.Left - Padding.Right),
                    Math.Max(0, Bounds.Height - TitleBarHeight - Padding.Top - Padding.Bottom)
                );

                // Guardar posición original del contenido
                var originalBounds = Content.Bounds;

                // Establecer posición y tamaño del contenido dentro del área disponible
                Content.Bounds = new Rect(
                    contentRect.X + Content.Margin.Left,
                    contentRect.Y + Content.Margin.Top,
                    Math.Max(0, contentRect.Width - Content.Margin.Left - Content.Margin.Right),
                    Math.Max(0, contentRect.Height - Content.Margin.Top - Content.Margin.Bottom)
                );

                // Dibujar el contenido
                Content.Draw();

                // Restaurar posición original
                Content.Bounds = originalBounds;
            }

            // Dibujar manejador de redimensionamiento si está activo
            if (IsResizable && !_isMinimized && !_isMaximized)
            {
                Renderer.DrawTriangle(
                    Bounds.X + Bounds.Width - ResizeHandleSize, Bounds.Y + Bounds.Height,
                    Bounds.X + Bounds.Width, Bounds.Y + Bounds.Height - ResizeHandleSize,
                    Bounds.X + Bounds.Width, Bounds.Y + Bounds.Height,
                    new Color(200, 200, 200, 150)
                );
            }
        }

        public override void Update()
        {
            if (!IsOpen) return;

            base.Update();

            // Actualizar botones
            if (ShowCloseButton) _closeButton.Update();
            if (ShowMinimizeButton) _minimizeButton.Update();
            if (ShowMaximizeButton) _maximizeButton.Update();

            // Manejar eventos de la ventana (arrastre, redimensionamiento, etc.)
            HandleWindowEvents();

            // Actualizar contenido solo si no está minimizada
            if (!_isMinimized && Content != null)
            {
                // Calcular área de contenido (igual que en Draw)
                var contentRect = new Rect(
                    Bounds.X + Padding.Left,
                    Bounds.Y + TitleBarHeight + Padding.Top,
                    Math.Max(0, Bounds.Width - Padding.Left - Padding.Right),
                    Math.Max(0, Bounds.Height - TitleBarHeight - Padding.Top - Padding.Bottom)
                );

                // Guardar posición original
                var originalBounds = Content.Bounds;

                // Establecer posición y tamaño para la actualización
                Content.Bounds = new Rect(
                    contentRect.X + Content.Margin.Left,
                    contentRect.Y + Content.Margin.Top,
                    Math.Max(0, contentRect.Width - Content.Margin.Left - Content.Margin.Right),
                    Math.Max(0, contentRect.Height - Content.Margin.Top - Content.Margin.Bottom)
                );

                // Actualizar el contenido
                Content.Update();

                // Restaurar posición original
                Content.Bounds = originalBounds;
            }

            // Bloquear interacción con otras ventanas si es modal
            if (IsModal)
            {
                UIEventSystem.BlockOtherControls = true;
            }
        }

        private void HandleWindowEvents()
        {
            var mousePos = UIEventSystem.MousePosition;
            var mouseDown = UIEventSystem.IsMouseDown;
            var mousePressed = UIEventSystem.IsMousePressed;
            var screenSize = Renderer.GetScreenSize();

            // Verificar si se hizo clic en los botones de la ventana
            bool clickedButton = false;

            if (ShowCloseButton && _closeButton.Bounds.Contains(mousePos))
                clickedButton = true;
            if (ShowMinimizeButton && _minimizeButton.Bounds.Contains(mousePos))
                clickedButton = true;
            if (ShowMaximizeButton && _maximizeButton.Bounds.Contains(mousePos))
                clickedButton = true;

            // Manejar arrastre de la ventana
            if (IsDraggable && !_isMaximized && !_isResizing && !clickedButton)
            {
                var titleBarRect = new Rect(Bounds.X, Bounds.Y, Bounds.Width, TitleBarHeight);

                if (mousePressed && titleBarRect.Contains(mousePos) && !_isDragging)
                {
                    _isDragging = true;
                    _dragOffset = new Vector2(mousePos.X - Bounds.X, mousePos.Y - Bounds.Y);
                    Focus(); // Dar foco a la ventana al arrastrar
                }

                if (_isDragging)
                {
                    if (mouseDown)
                    {
                        // Calcular nueva posición con restricciones
                        float newX = mousePos.X - _dragOffset.X;
                        float newY = mousePos.Y - _dragOffset.Y;

                        // Restricciones para que la ventana no salga de la pantalla
                        newX = Math.Max(0, newX); // No salir por izquierda
                        newY = Math.Max(0, newY); // No salir por arriba
                        newX = Math.Min(screenSize.X - Bounds.Width, newX); // No salir por derecha
                        newY = Math.Min(screenSize.Y - TitleBarHeight, newY); // No salir por abajo (solo barra de título visible)

                        Bounds = new Rect(newX, newY, Bounds.Width, Bounds.Height);
                    }
                    else
                    {
                        _isDragging = false;
                    }
                }
            }

            // Manejar redimensionamiento
            if (IsResizable && !_isMinimized && !_isMaximized && !_isDragging)
            {
                // Área del manejador de redimensionamiento (esquina inferior derecha)
                bool overResizeHandle = mousePos.X >= Bounds.X + Bounds.Width - ResizeHandleSize &&
                                      mousePos.Y >= Bounds.Y + Bounds.Height - ResizeHandleSize &&
                                      mousePos.X <= Bounds.X + Bounds.Width &&
                                      mousePos.Y <= Bounds.Y + Bounds.Height;

                if (mousePressed && overResizeHandle)
                {
                    _isResizing = true;
                }

                if (_isResizing)
                {
                    if (mouseDown)
                    {
                        // Calcular nuevo tamaño con restricciones
                        float newWidth = Math.Clamp(mousePos.X - Bounds.X, MinSize.X, MaxSize.X);
                        float newHeight = Math.Clamp(mousePos.Y - Bounds.Y, MinSize.Y, MaxSize.Y);

                        Bounds = new Rect(Bounds.X, Bounds.Y, newWidth, newHeight);
                    }
                    else
                    {
                        _isResizing = false;
                    }
                }
            }


            // Manejar foco al hacer clic en la ventana
            if (mousePressed && Bounds.Contains(mousePos) && !clickedButton)
            {
                Focus();
            }
        }

        protected override Vector2 MeasureCore(Vector2 availableSize)
        {
            if (!IsVisible)
            {
                DesiredSize = Vector2.Zero;
                return DesiredSize;
            }

            // Tamaño mínimo de la ventana (barra de título + padding)
            var minSize = new Vector2(
                Padding.Left + Padding.Right,
                TitleBarHeight + Padding.Top + Padding.Bottom
            );

            // Si no hay contenido, devolver el tamaño mínimo
            if (Content == null || _isMinimized)
            {
                DesiredSize = minSize;
                return DesiredSize;
            }

            // Calcular tamaño disponible para el contenido
            var contentAvailableSize = new Vector2(
                Math.Max(0, availableSize.X - Padding.Left - Padding.Right),
                Math.Max(0, availableSize.Y - TitleBarHeight - Padding.Top - Padding.Bottom)
            );

            // Medir el contenido
            Content.Measure(contentAvailableSize);

            // Calcular tamaño deseado de la ventana
            DesiredSize = new Vector2(
                Math.Max(minSize.X, Content.DesiredSize.X + Padding.Left + Padding.Right),
                Math.Max(minSize.Y, Content.DesiredSize.Y + TitleBarHeight + Padding.Top + Padding.Bottom)
            );

            return DesiredSize;
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            if (!IsVisible) return;

            // Establecer los bounds de la ventana
            Bounds = finalRect;

            // Si está minimizada o no hay contenido, no hay nada más que hacer
            if (_isMinimized || Content == null) return;

            // Calcular área de contenido (restando barra de título y padding)
            var contentRect = new Rect(
                finalRect.X + Padding.Left,
                finalRect.Y + TitleBarHeight + Padding.Top,
                Math.Max(0, finalRect.Width - Padding.Left - Padding.Right),
                Math.Max(0, finalRect.Height - TitleBarHeight - Padding.Top - Padding.Bottom)
            );

            // Organizar el contenido dentro del área disponible
            Content.Arrange(contentRect);
        }


        public void Show()
        {
            IsOpen = true;
            IsModal = false;
            OnShown?.Invoke();
        }

        public void ShowDialog()
        {
            IsOpen = true;
            IsModal = true;
            OnShown?.Invoke();
        }

        public void Close()
        {
            if (_closing) return;
            _closing = true;

            // Invoke closing event
            OnClosing?.Invoke();

            IsOpen = false;
            IsModal = false;
            IsVisible = false;
            UIEventSystem.BlockOtherControls = false;
            OnClosed?.Invoke();
        }

        public void AddControl(UIControl control)
        {
            if (control != null && !Controls.Contains(control))
            {
                Controls.Add(control);
                control.Parent = this;
            }
        }

        public void RemoveControl(UIControl control)
        {
            if (control != null)
            {
                Controls.Remove(control);
                control.Parent = null;
            }
        }

        public void ClearControls()
        {
            foreach (var control in Controls)
            {
                control.Parent = null;
            }
            Controls.Clear();
        }

        public void Minimize()
        {
            _isMinimized = !_isMinimized;
            if (_isMinimized)
            {
                _restoreRect = Bounds;
                Bounds = new Rect(Bounds.X, Bounds.Y, Bounds.Width, TitleBarHeight);
            }
            else
            {
                Bounds = _restoreRect;
            }
        }

        public void Maximize()
        {
            if (!_isMaximized)
            {
                _restoreRect = Bounds;
                var screenSize = Renderer.GetScreenSize();
                Bounds = new Rect(0, 0, screenSize.X, screenSize.Y);
                _isMaximized = true;
            }
            else
            {
                Bounds = _restoreRect;
                _isMaximized = false;
            }
        }

        public void CenterOnScreen()
        {
            var screenSize = Renderer.GetScreenSize();
            Bounds = new Rect(
                (screenSize.X - Bounds.Width) / 2,
                (screenSize.Y - Bounds.Height) / 2,
                Bounds.Width,
                Bounds.Height
            );
        }

        public Action OnShown;
        public Action OnClosed;
    }
}