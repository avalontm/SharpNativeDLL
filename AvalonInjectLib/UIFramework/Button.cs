using AvalonInjectLib;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib.UIFramework
{
    public class Button : UIControl
    {
        private string _text = "";
        private Color _textColor = Color.White;
        private Color _hoverColor = new Color(60, 60, 60);
        private Color _pressColor = new Color(40, 40, 40);
        private Color _disabledColor = new Color(100, 100, 100, 150);
        private Color _borderColor = new Color(80, 80, 80);
        private int _fontSize = 12;
        private bool _isHovered = false;
        private bool _isPressed = false;

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    InvalidateMeasure(); // CORRECCIÓN: Usar InvalidateMeasure en lugar de InvalidateLayout
                }
            }
        }

        public Color TextColor
        {
            get => _textColor;
            set => _textColor = value;
        }

        public Color HoverColor
        {
            get => _hoverColor;
            set => _hoverColor = value;
        }

        public Color PressColor
        {
            get => _pressColor;
            set => _pressColor = value;
        }

        public Color DisabledColor
        {
            get => _disabledColor;
            set => _disabledColor = value;
        }

        public Color BorderColor
        {
            get => _borderColor;
            set => _borderColor = value;
        }

        public int FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    InvalidateMeasure();
                }
            }
        }

        public Action<Vector2> OnClick;

        public Button()
        {
            BackgroundColor = new Color(50, 50, 50);
            _borderColor = new Color(80, 80, 80);
            Padding = new Thickness(8, 4, 8, 4);
        }

        public Button(string text) : this()
        {
            Text = text;
        }

        public override void Draw()
        {
            if (!IsVisible) return;

            // Determinar color de fondo basado en el estado
            Color bgColor = BackgroundColor;

            if (!IsEnabled)
            {
                bgColor = DisabledColor;
            }
            else if (_isPressed)
            {
                bgColor = PressColor;
            }
            else if (_isHovered)
            {
                bgColor = HoverColor;
            }

            // Dibujar fondo rectangular del botón
            Renderer.DrawRect(Bounds, bgColor);

            // Dibujar borde si está habilitado
            if (BorderColor.A > 0)
            {
                Renderer.DrawRectOutline(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BorderColor, 1f);
            }

            // Dibujar texto si existe
            if (!string.IsNullOrEmpty(Text))
            {
                // CORRECCIÓN: Calcular área de contenido respetando padding
                var contentArea = new Rect(
                    Bounds.X + Padding.Left,
                    Bounds.Y + Padding.Top,
                    Math.Max(0, Bounds.Width - Padding.Left - Padding.Right),
                    Math.Max(0, Bounds.Height - Padding.Top - Padding.Bottom)
                );

                // CORRECCIÓN: Verificar que hay espacio para dibujar
                if (contentArea.Width <= 0 || contentArea.Height <= 0) return;

                var textSize = Renderer.MeasureText(Text, FontSize);

                // CORRECCIÓN: Centrar el texto en el área de contenido
                float textX = contentArea.X + (contentArea.Width - textSize.X) / 2;
                float textY = contentArea.Y + (contentArea.Height - textSize.Y) / 2;

                // CORRECCIÓN: Asegurar que el texto no se dibuje fuera del área de contenido
                textX = Math.Max(contentArea.X, Math.Min(textX, contentArea.X + contentArea.Width - textSize.X));
                textY = Math.Max(contentArea.Y, Math.Min(textY, contentArea.Y + contentArea.Height - textSize.Y));

                // Ajustar color del texto si está deshabilitado
                Color finalTextColor = IsEnabled ? TextColor : TextColor.WithAlpha(150);

                Renderer.DrawText(Text, textX, textY, finalTextColor, FontSize);
            }
        }

        public override void Update()
        {
            if (!IsVisible || !IsEnabled)
            {
                // CORRECCIÓN: Resetear estados cuando está deshabilitado
                _isHovered = false;
                _isPressed = false;
                return;
            }

            base.Update();

            // Actualizar estado de hover
            _isHovered = Bounds.Contains(UIEventSystem.MousePosition.X, UIEventSystem.MousePosition.Y);

            // CORRECCIÓN: Simplificar lógica de manejo de clics
            if (_isHovered)
            {
                if (UIEventSystem.IsMousePressed && !_isPressed)
                {
                    _isPressed = true;
                    OnMouseDown?.Invoke(UIEventSystem.MousePosition);
                }
                else if (_isPressed && !UIEventSystem.IsMouseDown)
                {
                    _isPressed = false;
                    OnMouseUp?.Invoke(UIEventSystem.MousePosition);
                    OnClick?.Invoke(UIEventSystem.MousePosition);
                }
            }
            else if (_isPressed && !UIEventSystem.IsMouseDown)
            {
                // CORRECCIÓN: Manejar caso cuando se suelta el botón fuera del área
                _isPressed = false;
                OnMouseUp?.Invoke(UIEventSystem.MousePosition);
                // No disparar OnClick si se suelta fuera del botón
            }
        }

        // CORRECCIÓN: Implementar MeasureCore en lugar de Measure
        protected override Vector2 MeasureCore(Vector2 availableSize)
        {
            if (!IsVisible)
            {
                return Vector2.Zero;
            }

            // Calcular tamaño basado en el texto y padding
            Vector2 textSize = string.IsNullOrEmpty(Text)
                ? Vector2.Zero
                : Renderer.MeasureText(Text, FontSize);

            // CORRECCIÓN: Siempre incluir padding en el cálculo
            float width = textSize.X + Padding.Left + Padding.Right;
            float height = textSize.Y + Padding.Top + Padding.Bottom;

            // CORRECCIÓN: Asegurar un tamaño mínimo para botones sin texto
            if (string.IsNullOrEmpty(Text))
            {
                width = Math.Max(width, 20); // Mínimo 20px de ancho
                height = Math.Max(height, 20); // Mínimo 20px de alto
            }

            // CORRECCIÓN: Respetar el tamaño disponible si es menor
            return new Vector2(
                Math.Min(width, availableSize.X),
                Math.Min(height, availableSize.Y)
            );
        }

        // CORRECCIÓN: Eliminar el override de Measure ya que debe usar MeasureCore
        // public override void Measure(Vector2 availableSize) - REMOVIDO
    }
}