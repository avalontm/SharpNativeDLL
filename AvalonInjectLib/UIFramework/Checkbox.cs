using static AvalonInjectLib.Structs;

namespace AvalonInjectLib.UIFramework
{
    public enum CheckBoxOrientation
    {
        Left,   // Check + Texto (por defecto)
        Right   // Texto + Check
    }

    public class CheckBox : UIControl
    {
        // Constantes para el diseño
        private const float BOX_SIZE = 16f;
        private const float TEXT_SPACING = 5f;

        // Estados
        private bool _isChecked;
        private bool _isHovered;
        private bool _isPressed;
        private CheckBoxOrientation _orientation = CheckBoxOrientation.Left;

        // Propiedades
        public bool Checked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    CheckedChanged?.Invoke(_isChecked);
                }
            }
        }

        public string Text { get; set; } = string.Empty;

        public CheckBoxOrientation Orientation
        {
            get => _orientation;
            set
            {
                if (_orientation != value)
                {
                    _orientation = value;
                    UpdateLayout();
                }
            }
        }

        public Color BoxColor { get; set; } = Color.FromArgb(100, 149, 237); // CornflowerBlue
        public Color CheckColor { get; set; } = Color.White;
        public Color HoverBoxColor { get; set; } = Color.FromArgb(120, 169, 247);
        public Color PressedBoxColor { get; set; } = Color.FromArgb(80, 129, 207);
        public Font Font { get; set; } = Font.GetDefaultFont();

        // Evento
        public Action<bool>? CheckedChanged;

        public CheckBox()
        {
            IsFocusable = true;
            Width = 150f; // Ancho por defecto
            Height = 32f; // Altura por defecto
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            // Calcular dimensiones basadas en el texto
            float textWidth = 0f;
            float textHeight = 0f;

            if (!string.IsNullOrEmpty(Text))
            {
                var textSize = Font.MeasureText(Text);
                textWidth = textSize.X;
                textHeight = textSize.Y;
            }

            // Calcular dimensiones totales solo si la orientación es Left
            // Para Right, mantenemos el ancho actual del control
            if (Orientation == CheckBoxOrientation.Left)
            {
                Width = BOX_SIZE + (textWidth > 0 ? TEXT_SPACING + textWidth : 0);
            }
            // Para Right, el Width se mantiene como está establecido externamente

            Height = Math.Max(32f, textHeight);
        }

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();
            var currentBoxColor = GetCurrentBoxColor();

            // Calcular posiciones según la orientación
            float boxX, boxY, textX, textY;
            CalculatePositions(absPos, out boxX, out boxY, out textX, out textY);

            // Dibujar el cuadro del checkbox
            Renderer.DrawRect(
                new Rect(boxX, boxY, BOX_SIZE, BOX_SIZE),
                currentBoxColor
            );

            // Dibujar el borde
            Renderer.DrawRectOutline(
                new Rect(boxX, boxY, BOX_SIZE, BOX_SIZE),
                Color.FromArgb(150, 150, 150),
                1f
            );

            // Dibujar la marca de verificación
            if (Checked)
            {
                Renderer.DrawRect(
                    new Rect(boxX + 2, boxY + 2, BOX_SIZE - 4, BOX_SIZE - 4),
                    CheckColor
                );
            }

            // Dibujar el texto si existe
            if (!string.IsNullOrEmpty(Text))
            {
                Renderer.DrawText(
                    Text,
                    new Vector2(textX, textY),
                    ForeColor,
                    Font
                );
            }
        }

        private void CalculatePositions(Vector2 absPos, out float boxX, out float boxY, out float textX, out float textY)
        {
            // Calcular altura del texto para centrado vertical
            float textHeight = !string.IsNullOrEmpty(Text) ? Font.GetTextHeight(Text) : 0f;

            // Centrar verticalmente los elementos
            boxY = absPos.Y + (Height - BOX_SIZE) / 2;
            textY = absPos.Y + (Height - textHeight) / 2;

            switch (Orientation)
            {
                case CheckBoxOrientation.Left:
                    // Check + Texto (orientación por defecto)
                    boxX = absPos.X;
                    textX = absPos.X + BOX_SIZE + TEXT_SPACING;
                    break;

                case CheckBoxOrientation.Right:
                    // Texto ocupa todo el ancho disponible, Check fijo en el lado derecho
                    boxX = absPos.X + Width - BOX_SIZE;
                    textX = absPos.X;
                    break;

                default:
                    // Fallback a Left
                    boxX = absPos.X;
                    textX = absPos.X + BOX_SIZE + TEXT_SPACING;
                    break;
            }
        }

        private Color GetCurrentBoxColor()
        {
            if (!Enabled)
                return BoxColor.WithAlpha(0.5f);

            if (_isPressed)
                return PressedBoxColor;

            if (_isHovered)
                return HoverBoxColor;

            return BoxColor;
        }

        public override void Update()
        {
            base.Update();

            if (!Enabled || !Visible) return;

            Vector2 mousePos = UIEventSystem.MousePosition;
            bool isMouseOver = Contains(mousePos);

            // Actualizar estado hover
            _isHovered = isMouseOver;
        }

        protected override void OnClick(object sender, Vector2 pos)
        {
            base.OnClick(sender, pos);
            if (Enabled)
            {
                Checked = !Checked;
            }
        }

        /// <summary>
        /// Determina si el punto está dentro del área clickeable del CheckBox
        /// </summary>
        public override bool Contains(Vector2 point)
        {
            var absPos = GetAbsolutePosition();

            // El área clickeable siempre incluye todo el control
            return new Rect(absPos.X, absPos.Y, Width, Height).Contains(point);
        }

        /// <summary>
        /// Método auxiliar para configurar la orientación del checkbox
        /// </summary>
        /// <param name="orientation">Orientación deseada</param>
        public void SetOrientation(CheckBoxOrientation orientation)
        {
            Orientation = orientation;
        }

        /// <summary>
        /// Método auxiliar para configurar el texto y actualizar el layout
        /// </summary>
        /// <param name="text">Texto a mostrar</param>
        public void SetText(string text)
        {
            Text = text;
            UpdateLayout();
        }

        /// <summary>
        /// Método auxiliar para configurar el ancho del control (útil para orientación Right)
        /// </summary>
        /// <param name="width">Ancho deseado del control</param>
        public void SetWidth(float width)
        {
            Width = width;
            // No necesitamos llamar UpdateLayout() ya que el ancho se respeta en orientación Right
        }

        /// <summary>
        /// Método auxiliar para configurar texto, orientación y ancho al mismo tiempo
        /// </summary>
        /// <param name="text">Texto a mostrar</param>
        /// <param name="orientation">Orientación deseada</param>
        /// <param name="width">Ancho del control (opcional, solo se usa si orientation es Right)</param>
        public void SetTextAndOrientation(string text, CheckBoxOrientation orientation, float? width = null)
        {
            Text = text;
            if (width.HasValue)
            {
                Width = width.Value;
            }
            Orientation = orientation;
            // UpdateLayout() se llama automáticamente cuando cambia Orientation
        }
    }
}