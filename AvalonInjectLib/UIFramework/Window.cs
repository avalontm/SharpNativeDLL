using AvalonInjectLib.UIFramework;
using static AvalonInjectLib.Structs;
using Color = AvalonInjectLib.UIFramework.Color;

namespace AvalonInjectLib
{
    public class Window : UIContainer
    {
        // Constantes
        public const float TITLE_BAR_HEIGHT = 30f;
        public const float BORDER_WIDTH = 2f;
        public const float TITLE_PADDING = 5f;
        public const float CLOSE_BUTTON_SIZE = 20f;

        // Controles de la barra de título
        private Label _titleLabel;
        private Button _closeButton;
        private bool _isDragging = false;
        private Vector2 _dragOffset;
        private Vector2 _screenSize;

        // Propiedades
        public string Title
        {
            get => _titleLabel.Text;
            set => _titleLabel.Text = value;
        }

        public Color TitleBarColor { get; set; } = Color.FromArgb(32, 32, 32);
        public Color TitleBarTextColor
        {
            get => _titleLabel.ForeColor;
            set => _titleLabel.ForeColor = value;
        }

        public Color BorderColor { get; set; } = Color.FromArgb(64, 64, 64);

        public bool HasTitleBar
        {
            get => _hasTitleBar;
            set
            {
                _hasTitleBar = value;
                _titleLabel.Visible = value;
                _closeButton.Visible = value && Closable;
                UpdateContentPosition();
            }
        }
        private bool _hasTitleBar = true;

        public bool HasBorder { get; set; } = true;
        public bool IsActive { get; set; } = true;

        public bool Closable
        {
            get => _closable;
            set
            {
                _closable = value;
                _closeButton.Visible = value && HasTitleBar;
            }
        }
        private bool _closable = true;

        // Contenido
        private UIControl? _content;
        public UIControl? Content
        {
            get => _content;
            set
            {
                if (_content != null) base.RemoveChild(_content);
                _content = value;
                if (_content != null)
                {
                    base.AddChild(_content);
                    UpdateContentPosition();
                }
            }
        }

        // Eventos
        public event Action? Closing;
        public event Action? Closed;
        public event Action? Shown;

        public Window()
        {
            Width = 400f;
            Height = 300f;
            BackColor = Color.FromArgb(240, 240, 240);

            // Inicializar título
            _titleLabel = new Label
            {
                Text = "Window",
                AutoSize = false,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            AddChild(_titleLabel);

            // Inicializar botón de cierre
            _closeButton = new Button
            {
                Text = "x",
                Font = Font.GetDefaultFont(),
                Width = CLOSE_BUTTON_SIZE,
                Height = CLOSE_BUTTON_SIZE,
                BackColor = Color.Red,
                TextColor = Color.White
            };

            _closeButton.Click += (pos) => Close();
            AddChild(_closeButton);

            // Inicializar tamaño de pantalla
            UpdateScreenSize();
            UpdateTitleBarControls();
        }

        public void UpdateScreenSize()
        {
            _screenSize = UIEventSystem.WindowSize;

            // Asegurarse que la ventana sigue dentro de los límites
            X = Math.Clamp(X, 0, _screenSize.X - Width);
            Y = Math.Clamp(Y, 0, _screenSize.Y - Height);
        }

        bool _isPressed;

        public override void Update()
        {
            if (!Visible) return;

            // Actualizar tamaño de pantalla
            UpdateScreenSize();

            // Manejar eventos de arrastre primero
            Vector2 mousePos = UIEventSystem.MousePosition;
            bool isMouseOver = Contains(mousePos);

            if (UIEventSystem.IsSCreenFocus)
            {
                if (isMouseOver)
                {
                    // Manejar MouseDown
                    if (UIEventSystem.IsMousePressed && !_isPressed)
                    {
                        _isPressed = true;
                        OnMouseDown(mousePos);
                    }
                    // Manejar MouseUp
                    else if (!UIEventSystem.IsMousePressed && _isPressed)
                    {
                        _isPressed = false;
                        OnMouseUp(mousePos);
                    }
                    // Manejar MouseMove
                    OnMouseMove(mousePos);
                }
                else if (_isPressed)
                {
                    // Manejar caso cuando el mouse se sale durante el arrastre
                    if (!UIEventSystem.IsMousePressed)
                    {
                        _isPressed = false;
                    }
                    OnMouseMove(mousePos); // Seguir moviendo aunque el mouse salga
                }
            }

            // Actualizar controles de la barra de título
            UpdateTitleBarControls();

            // Actualizar posición del contenido
            UpdateContentPosition();

            // Actualizar controles hijos (pero después de manejar los eventos de la ventana)
            _titleLabel.Update();
            _closeButton.Update();
            _content?.Update();
        }

        protected override void OnMouseDown(Vector2 mousePos)
        {
            base.OnMouseDown(mousePos);

            // Verificar si el click fue en la barra de título
            if (HasTitleBar && IsActive && IsMouseOnTitleBar(mousePos))
            {
                // Verificar que no sea en el botón de cierre
                if (!_closeButton.Contains(mousePos))
                {
                    _isDragging = true;
                    var absPos = GetAbsolutePosition();
                    _dragOffset = new Vector2(mousePos.X - absPos.X, mousePos.Y - absPos.Y);
                }
            }
        }

        protected override void OnMouseUp(Vector2 mousePos)
        {
            base.OnMouseUp(mousePos);
            if (_isDragging)
            {
                _isDragging = false;
            }
        }

        protected override void OnMouseMove(Vector2 mousePos)
        {
            base.OnMouseMove(mousePos);

            if (_isDragging)
            {
                // Calcular nueva posición
                float newX = mousePos.X - _dragOffset.X;
                float newY = mousePos.Y - _dragOffset.Y;

                // Aplicar límites de pantalla
                newX = Math.Clamp(newX, 0, _screenSize.X - Width);
                newY = Math.Clamp(newY, 0, _screenSize.Y - Height);

                // Actualizar posición
                X = newX;
                Y = newY;
            }
        }

        private bool IsMouseOnTitleBar(Vector2 mousePos)
        {
            if (!HasTitleBar) return false;

            var absPos = GetAbsolutePosition();
            var titleBarRect = new Rect(
                absPos.X + (HasBorder ? BORDER_WIDTH : 0),
                absPos.Y + (HasBorder ? BORDER_WIDTH : 0),
                Width - (HasBorder ? BORDER_WIDTH * 2 : 0),
                TITLE_BAR_HEIGHT
            );

            return titleBarRect.Contains(mousePos) && !_closeButton.Contains(mousePos);
        }

        public void Show()
        {
            this.Visible = true;
            Shown?.Invoke();
        }

        public void Close()
        {
            Closing?.Invoke();
            this.Visible = false;
            Closed?.Invoke();
        }

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();

            // Dibujar borde
            if (HasBorder)
            {
                Renderer.DrawRect(new Rect(absPos.X, absPos.Y, Width, Height), BorderColor);
            }

            // Dibujar barra de título (fondo)
            if (HasTitleBar)
            {
                var titleBarRect = GetTitleBarRect();
                Renderer.DrawRect(
                    new Rect(absPos.X + titleBarRect.X,
                            absPos.Y + titleBarRect.Y,
                            titleBarRect.Width,
                            titleBarRect.Height),
                    TitleBarColor);
            }

            // Dibujar controles hijos (solo estos dos en orden específico)
            _titleLabel.Draw();
            _closeButton.Draw();

            // Dibujar contenido si existe
            _content?.Draw();
        }

        protected void UpdateTitleBarControls()
        {
            if (!HasTitleBar) return;

            // Obtener posición absoluta de la ventana
            var absPos = GetAbsolutePosition();
            var titleBarRect = GetTitleBarRect();

            // Calcular posición relativa del título
            float titleX = TITLE_PADDING + (HasBorder ? BORDER_WIDTH : 0);
            float titleY = (TITLE_BAR_HEIGHT - _titleLabel.Height) / 2 + (HasBorder ? BORDER_WIDTH : 0);

            // Posicionar título (coordenadas relativas a la ventana)
            _titleLabel.X = titleX;
            _titleLabel.Y = titleY;
            _titleLabel.Width = titleBarRect.Width - CLOSE_BUTTON_SIZE - (TITLE_PADDING * 2);

            // Calcular posición relativa del botón de cierre
            float closeX = Width - CLOSE_BUTTON_SIZE - TITLE_PADDING - (HasBorder ? BORDER_WIDTH : 0);
            float closeY = (TITLE_BAR_HEIGHT - CLOSE_BUTTON_SIZE) / 2 + (HasBorder ? BORDER_WIDTH : 0);

            // Posicionar botón de cierre (coordenadas relativas a la ventana)
            _closeButton.X = closeX;
            _closeButton.Y = closeY;
            _closeButton.Width = CLOSE_BUTTON_SIZE;
            _closeButton.Height = CLOSE_BUTTON_SIZE;
        }

        private void UpdateContentPosition()
        {
            if (_content == null) return;

            var contentArea = GetContentArea();
            _content.X = contentArea.X;
            _content.Y = contentArea.Y;
            _content.Width = contentArea.Width;
            _content.Height = contentArea.Height;
        }

        private Rect GetTitleBarRect()
        {
            return new Rect(
                HasBorder ? BORDER_WIDTH : 0,
                HasBorder ? BORDER_WIDTH : 0,
                Width - (HasBorder ? BORDER_WIDTH * 2 : 0),
                TITLE_BAR_HEIGHT
            );
        }

        private Rect GetContentArea()
        {
            float contentY = HasTitleBar ? TITLE_BAR_HEIGHT : 0;
            contentY += HasBorder ? BORDER_WIDTH : 0;

            return new Rect(
                HasBorder ? BORDER_WIDTH : 0,
                contentY,
                Width - (HasBorder ? BORDER_WIDTH * 2 : 0),
                Height - contentY - (HasBorder ? BORDER_WIDTH : 0)
            );
        }
    }
}