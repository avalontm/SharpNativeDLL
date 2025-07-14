using AvalonInjectLib;
using static AvalonInjectLib.Structs;
namespace AvalonInjectLib.UIFramework
{
    public class Label : UIControl
    {
        private string _text = "";
        private Color _textColor = Color.White;
        private int _fontSize = 12;
        private HorizontalAlignment _textAlignment = HorizontalAlignment.Left;
        private bool _autoSize = true;

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    InvalidateMeasure(); // Notificar al layout que debe recalcular
                }
            }
        }

        public Color TextColor
        {
            get => _textColor;
            set => _textColor = value;
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

        public HorizontalAlignment TextAlignment
        {
            get => _textAlignment;
            set => _textAlignment = value;
        }

        public bool AutoSize
        {
            get => _autoSize;
            set
            {
                if (_autoSize != value)
                {
                    _autoSize = value;
                    InvalidateMeasure();
                }
            }
        }

        public Label()
        {
            BackgroundColor = Color.Transparent;
        }

        public Label(string text) : this()
        {
            Text = text;
        }

        public override void Draw()
        {
            if (!IsVisible || string.IsNullOrEmpty(Text)) return;

            // Draw background
            if (BackgroundColor.A > 0)
                Renderer.DrawRect(Bounds, BackgroundColor);

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

            // CORRECCIÓN: Calcular posición X respetando el área de contenido
            float textX = contentArea.X;
            if (TextAlignment == HorizontalAlignment.Center)
            {
                textX = contentArea.X + (contentArea.Width - textSize.X) / 2;
            }
            else if (TextAlignment == HorizontalAlignment.Right)
            {
                textX = contentArea.X + contentArea.Width - textSize.X;
            }

            // CORRECCIÓN: Centrar verticalmente en el área de contenido
            float textY = contentArea.Y + (contentArea.Height - textSize.Y) / 2;

            // CORRECCIÓN: Asegurar que el texto no se dibuje fuera del área de contenido
            textX = Math.Max(contentArea.X, Math.Min(textX, contentArea.X + contentArea.Width - textSize.X));
            textY = Math.Max(contentArea.Y, Math.Min(textY, contentArea.Y + contentArea.Height - textSize.Y));

            Renderer.DrawText(Text, textX, textY, TextColor, FontSize);
        }

        // CORRECCIÓN: Implementar MeasureCore en lugar de Measure
        protected override Vector2 MeasureCore(Vector2 availableSize)
        {
            if (!IsVisible)
            {
                return Vector2.Zero;
            }

            if (string.IsNullOrEmpty(Text))
            {
                // Sin texto, el tamaño deseado es solo el padding
                return new Vector2(
                    Padding.Left + Padding.Right,
                    Padding.Top + Padding.Bottom
                );
            }

            var textSize = Renderer.MeasureText(Text, FontSize);

            if (_autoSize)
            {
                // CORRECCIÓN: AutoSize - usar el tamaño del texto + padding
                return new Vector2(
                    textSize.X + Padding.Left + Padding.Right,
                    textSize.Y + Padding.Top + Padding.Bottom
                );
            }
            else
            {
                // CORRECCIÓN: No AutoSize - usar el espacio disponible, pero asegurar espacio mínimo para el texto
                var minWidth = textSize.X + Padding.Left + Padding.Right;
                var minHeight = textSize.Y + Padding.Top + Padding.Bottom;

                return new Vector2(
                    Math.Max(minWidth, availableSize.X),
                    Math.Max(minHeight, availableSize.Y)
                );
            }
        }

        // CORRECCIÓN: Eliminar el override de Measure ya que debe usar MeasureCore
        // public override void Measure(Vector2 availableSize) - REMOVIDO

        public override void Update()
        {
            base.Update();
            // CORRECCIÓN: Agregar lógica de actualización si es necesaria
            // Por ejemplo, animaciones de texto, parpadeo, etc.
        }
    }
}