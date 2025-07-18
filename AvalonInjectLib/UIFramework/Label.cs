using static AvalonInjectLib.Structs;

namespace AvalonInjectLib.UIFramework
{
    public class Label : UIControl
    {
        // Constantes por defecto
        private const float DEFAULT_WIDTH = 100f;
        private const float DEFAULT_HEIGHT = 32f;

        // Propiedades de texto
        private string _text = string.Empty;
        private Font? _font;
        private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Left;
        private VerticalAlignment _verticalAlignment = VerticalAlignment.Top;
        private bool _autoSize = true;

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    if (_autoSize) UpdateAutoSize();
                }
            }
        }

        public Font? Font
        {
            get => _font;
            set
            {
                if (_font != value)
                {
                    _font = value;
                    if (_autoSize) UpdateAutoSize();
                }
            }
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get => _horizontalAlignment;
            set
            {
                _horizontalAlignment = value;
            }
        }

        public VerticalAlignment VerticalAlignment
        {
            get => _verticalAlignment;
            set
            {
                _verticalAlignment = value;
            }
        }

        public bool AutoSize
        {
            get => _autoSize;
            set
            {
                if (_autoSize != value)
                {
                    _autoSize = value;
                    if (_autoSize) UpdateAutoSize();
                }
            }
        }

        public Color TextShadowColor { get; set; } = Color.Transparent;
        public Vector2 TextShadowOffset { get; set; } = new Vector2(1, 1);

        public Label()
        {
            Font = Font.GetDefaultFont();
            Width = DEFAULT_WIDTH;
            Height = DEFAULT_HEIGHT;
            BackColor = Color.Transparent;
            ForeColor = Color.White;
        }

        public override void Draw()
        {
            if (!Visible || string.IsNullOrEmpty(Text)) return;

            var absPos = GetAbsolutePosition();
            var textSize = MeasureText();

            // Calcular posición basada en alineación
            float textX = CalculateTextX(absPos.X, textSize.X);
            float textY = CalculateTextY(absPos.Y, textSize.Y);

            // Dibujar sombra si está configurada
            DrawTextShadow(textX, textY);

            // Dibujar texto principal
            Renderer.DrawText(Text, textX, textY, Enabled ? ForeColor : Color.Gray, Font);
        }

        private float CalculateTextX(float baseX, float textWidth)
        {
            return HorizontalAlignment switch
            {
                HorizontalAlignment.Center => baseX + (Width - textWidth) / 2,
                HorizontalAlignment.Right => baseX + Width - textWidth,
                _ => baseX // Left
            };
        }

        private float CalculateTextY(float baseY, float textHeight)
        {
            return VerticalAlignment switch
            {
                VerticalAlignment.Center => baseY + (Height - textHeight) / 2,
                VerticalAlignment.Bottom => baseY + Height - textHeight,
                _ => baseY // Top
            };
        }

        private void DrawTextShadow(float textX, float textY)
        {
            if (TextShadowColor.A > 0)
            {
                Renderer.DrawText(Text,
                    textX + TextShadowOffset.X,
                    textY + TextShadowOffset.Y,
                    TextShadowColor,
                    Font);
            }
        }

        private void UpdateAutoSize()
        {
            if (string.IsNullOrEmpty(Text)) return;

            var textSize = MeasureText();
            Width = textSize.X;
            Height = textSize.Y;
        }

        private Vector2 MeasureText()
        {
            return Font.MeasureText(Text);
        }

    }
}