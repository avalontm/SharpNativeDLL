using static AvalonInjectLib.Structs;
using System;

namespace AvalonInjectLib.UIFramework
{
    public class Button : UIControl
    {
        // Constantes para el botón
        private const float DEFAULT_WIDTH = 100f;
        private const float DEFAULT_HEIGHT = 30f;
        private const float TEXT_PADDING = 5f;
        private const float HOVER_ALPHA = 0.7f; // 70% de opacidad en hover
        private const float PRESSED_ALPHA = 0.5f; // 50% de opacidad al presionar

        // Estados del botón
        public bool IsHovered { get; private set; }
        public bool IsPressed { get; private set; }

        // Control de texto interno
        private readonly Label _label;

        // Borde del botón
        public bool ShowBorder { get; set; } = true;
        public Color BorderColor { get; set; } = Color.FromArgb(100, 100, 100);
        public float BorderWidth { get; set; } = 1f;

        Font _font;
        public Font Font
        {
            get => _font;
            set
            {
                _font = value;
                if(_label != null)
                    _label.Font = _font;
            }
        }

        // Propiedades del texto
        public string Text
        {
            get => _label.Text;
            set
            {
                _label.Text = value;
                UpdateLabel();
            }
        }

        public Color TextColor
        {
            get => _label.ForeColor;
            set => _label.ForeColor = value;
        }

        // Constructor
        public Button()
        {
            IsFocusable = true; 
            Width = DEFAULT_WIDTH;
            Height = DEFAULT_HEIGHT;
            BackColor = Color.FromArgb(100, 149, 237); // CornflowerBlue

            Font = Font.GetDefaultFont();

            // Inicializar label interno
            _label = new Label
            {
                Parent = this,
                Text = "Button",
                AutoSize = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = Font
            };

            UpdateLabel();
        }

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();
            var currentColor = GetCurrentButtonColor();

            // Dibujar fondo del botón
            Renderer.DrawRect(new Rect(absPos.X, absPos.Y, Width, Height), currentColor);

            // Dibujar borde
            if (ShowBorder)
            {
                Renderer.DrawRectOutline(
                    new Rect(absPos.X, absPos.Y, Width, Height),
                    BorderColor,
                    BorderWidth
                );
            }

            // Dibujar el texto
            _label.Draw();
        }

        private Color GetCurrentButtonColor()
        {
            if (!Enabled)
                return BackColor.WithAlpha(0.5f); // Media opacidad cuando está deshabilitado

            if (IsPressed)
                return BackColor.WithAlpha(PRESSED_ALPHA);

            if (IsHovered)
                return BackColor.WithAlpha(HOVER_ALPHA);

            return BackColor;
        }

        private void UpdateLabel()
        {
            // Actualizar posición y tamaño del label
            _label.X = TEXT_PADDING;
            _label.Y = TEXT_PADDING;
            _label.Width = Math.Max(0, Width - (2 * TEXT_PADDING));
            _label.Height = Math.Max(0, Height - (2 * TEXT_PADDING));

            // Actualizar posición absoluta
            _label.GetAbsolutePosition();
        }

        public override void Update()
        {
            base.Update();

            // Actualizar posición del label si cambió el tamaño
            if (_label.Width != Width - (2 * TEXT_PADDING) ||
                _label.Height != Height - (2 * TEXT_PADDING))
            {
                UpdateLabel();
            }

            _label.Update();
        }

        // Manejo de eventos de mouse
        protected override void OnMouseEnter(Vector2 mousePos)
        {
            base.OnMouseEnter(mousePos);
            if (Enabled)
            {
                IsHovered = true;
            }
        }

        protected override void OnMouseLeave(Vector2 mousePos)
        {
            base.OnMouseLeave(mousePos);
            if (Enabled && !IsPressed)
            {
                IsHovered = false;
            }
        }

        protected override void OnClick(Vector2 mousePos)
        {
            base.OnClick(mousePos);
        }
    }
}