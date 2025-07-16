using static AvalonInjectLib.Structs;
using System;
using System.Transactions;

namespace AvalonInjectLib.UIFramework
{
    public class CheckBox : UIControl
    {
        // Constantes para el diseño
        private const float BOX_SIZE = 16f;
        private const float TEXT_SPACING = 5f;

        // Estados
        private bool _isChecked;
        private bool _isHovered;
        private bool _isPressed;

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
            Width = BOX_SIZE + TEXT_SPACING + 100; // Espacio para texto
            Height = BOX_SIZE;
            IsFocusable = true;
        }

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();
            var currentBoxColor = GetCurrentBoxColor();

            // Dibujar el cuadro del checkbox
            Renderer.DrawRect(
                new Rect(absPos.X, absPos.Y, BOX_SIZE, BOX_SIZE),
                currentBoxColor
            );

            // Dibujar el borde
            Renderer.DrawRectOutline(
                new Rect(absPos.X, absPos.Y, BOX_SIZE, BOX_SIZE),
                Color.FromArgb(150, 150, 150),
                1f
            );

            // Dibujar la marca de verificación
            if (Checked)
            {
                Renderer.DrawRect(
                    new Rect(absPos.X + 2, absPos.Y + 2, BOX_SIZE - 4, BOX_SIZE - 4),
                    CheckColor
                );
            }

            // Dibujar el texto si existe
            if (!string.IsNullOrEmpty(Text))
            {
                Renderer.DrawText(
                    Text,
                    new Vector2(absPos.X + BOX_SIZE + TEXT_SPACING, absPos.Y),
                    ForeColor,
                    Font
                );
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

        protected override void OnClick(Vector2 mousePos)
        {
            base.OnClick(mousePos);
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

            // Área clickeable incluye el cuadro y el texto
            float totalWidth = BOX_SIZE;
            if (!string.IsNullOrEmpty(Text))
            {
                var textSize = Font.MeasureText(Text);
                totalWidth += TEXT_SPACING + textSize.X;
            }

            return new Rect(absPos.X, absPos.Y, totalWidth, Height).Contains(point);
        }
    }
}