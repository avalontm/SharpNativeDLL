namespace AvalonInjectLib.UIFramework
{
    using static AvalonInjectLib.Structs;

    public class Checkbox : UIControl
    {
        // Propiedades básicas
        private bool _isChecked = false;
        public string Text { get; set; } = "";
        public Color CheckColor { get; set; } = new Color(0, 120, 215);
        public Color BoxColor { get; set; } = new Color(120, 120, 120);
        public Color HoverColor { get; set; } = new Color(150, 150, 150);
        public float BoxSize { get; set; } = 16f;
        public float TextSpacing { get; set; } = 5f;
        public float BorderThickness { get; set; } = 1f;

        // Evento
        public Action<bool> OnCheckedChanged;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnCheckedChanged?.Invoke(_isChecked);
                }
            }
        }

        public override void Draw()
        {
            if (!IsVisible) return;

            // Calcular posición de la caja (centrada verticalmente)
            float boxY = Bounds.Y + (Bounds.Height - BoxSize) / 2;

            // Determinar color de fondo de la caja
            Color boxBgColor = IsHovered ? HoverColor : BoxColor;
            if (!IsEnabled) boxBgColor = boxBgColor.WithAlpha(127);

            // Dibujar caja exterior del checkbox
            Renderer.DrawRect(Bounds.X, boxY, BoxSize, BoxSize, boxBgColor);
            Renderer.DrawRectOutline(Bounds.X, boxY, BoxSize, BoxSize, BoxColor, BorderThickness);

            // Dibujar marca de verificación (simple cuadro relleno)
            if (IsChecked)
            {
                float padding = 3f;
                Renderer.DrawRect(
                    Bounds.X + padding,
                    boxY + padding,
                    BoxSize - padding * 2,
                    BoxSize - padding * 2,
                    IsEnabled ? CheckColor : CheckColor.WithAlpha(127)
                );
            }

            // Dibujar texto si existe
            if (!string.IsNullOrEmpty(Text))
            {
                float textX = Bounds.X + BoxSize + TextSpacing;
                float textY = Bounds.Y + (Bounds.Height - 12) / 2; // Tamaño de fuente fijo a 12
                Color textColor = IsEnabled ? Color.White : new Color(150, 150, 150);

                Renderer.DrawText(Text, textX, textY, textColor, 12);
            }
        }

        public override void Update()
        {
            if (!IsVisible || !IsEnabled) return;

            base.Update();

            // Manejar clic para alternar el estado
            if (IsHovered && UIEventSystem.IsMousePressed)
            {
                IsChecked = !IsChecked;
            }
        }

        public override void Measure(Vector2 availableSize)
        {
            if (!IsVisible)
            {
                Bounds = new Rect(Bounds.X, Bounds.Y, 0, 0);
                return;
            }

            // Calcular tamaño basado en el texto y el checkbox
            float width = BoxSize;
            float height = BoxSize;

            if (!string.IsNullOrEmpty(Text))
            {
                var textSize = Renderer.MeasureText(Text, 12);
                width += TextSpacing + textSize.X;
                height = Math.Max(height, textSize.Y);
            }

            // Mantener tamaño manual si fue especificado
            if (!float.IsNaN(Bounds.Width)) width = Bounds.Width;
            if (!float.IsNaN(Bounds.Height)) height = Bounds.Height;

            Bounds = new Rect(Bounds.X, Bounds.Y, width, height);
        }
    }
}