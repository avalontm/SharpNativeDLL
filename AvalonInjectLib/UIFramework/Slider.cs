using static AvalonInjectLib.Structs;
using System;

namespace AvalonInjectLib.UIFramework
{
    public class Slider : UIControl
    {
        // Constantes para el diseño
        private const float TRACK_HEIGHT = 6f;
        private const float THUMB_WIDTH = 12f;
        private const float THUMB_HEIGHT = 16f;
        private const float TEXT_PADDING = 5f;

        // Estados
        private bool _isDragging;
        private float _value;
        private string _text = string.Empty;

        // Propiedades
        public float Value
        {
            get => _value;
            set
            {
                var newValue = Math.Clamp(value, MinValue, MaxValue);
                if (_value != newValue)
                {
                    _value = newValue;
                    ValueChanged?.Invoke(_value);
                }
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                UpdateLayout();
            }
        }

        public float MinValue { get; set; } = 0f;
        public float MaxValue { get; set; } = 100f;
        public Color TrackColor { get; set; } = Color.FromArgb(70, 70, 70);
        public Color FillColor { get; set; } = Color.FromArgb(100, 149, 237);
        public Color ThumbColor { get; set; } = Color.White;
        public Color ThumbHoverColor { get; set; } = Color.FromArgb(220, 220, 220);
        public Color ThumbBorderColor { get; set; } = Color.FromArgb(150, 150, 150);
        public bool ShowValue { get; set; } = true;
        public Font Font { get; set; } = Font.GetDefaultFont();

        // Evento
        public Action<float>? ValueChanged;

        public Slider()
        {
            Width = 200f;
            Height = Math.Max(THUMB_HEIGHT, (Font.LineHeight * 3));
            IsFocusable = true;
        }

        private void UpdateLayout()
        {
            // Ajustar altura si el texto es más grande que el thumb
            if (!string.IsNullOrEmpty(Text))
            {
                Height = Math.Max(THUMB_HEIGHT, (Font.LineHeight *3));
            }
        }

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();
            float trackY = absPos.Y + (Height - TRACK_HEIGHT) / 2;

            // Calcular posición del thumb
            float percentage = (Value - MinValue) / (MaxValue - MinValue);
            float thumbX = absPos.X + (Width - THUMB_WIDTH) * percentage;

            // Dibujar texto descriptivo si existe
            if (!string.IsNullOrEmpty(Text))
            {
                Renderer.DrawText(
                    Text,
                    new Vector2(absPos.X, absPos.Y),
                    ForeColor,
                    Font
                );
            }

            // Dibujar track (fondo)
            Renderer.DrawRect(
                new Rect(absPos.X, trackY, Width, TRACK_HEIGHT),
                TrackColor
            );

            // Dibujar fill (parte llena)
            Renderer.DrawRect(
                new Rect(absPos.X, trackY, (Width - THUMB_WIDTH) * percentage + THUMB_WIDTH / 2, TRACK_HEIGHT),
                FillColor
            );

            // Dibujar thumb (rectángulo)
            Color currentThumbColor = _isDragging || Contains(UIEventSystem.MousePosition) ?
                ThumbHoverColor : ThumbColor;

            Renderer.DrawRect(
                new Rect(thumbX, trackY - (THUMB_HEIGHT - TRACK_HEIGHT) / 2, THUMB_WIDTH, THUMB_HEIGHT),
                currentThumbColor
            );

            // Dibujar borde del thumb
            Renderer.DrawRectOutline(
                new Rect(thumbX, trackY - (THUMB_HEIGHT - TRACK_HEIGHT) / 2, THUMB_WIDTH, THUMB_HEIGHT),
                ThumbBorderColor,
                1f
            );

            // Mostrar valor numérico si está habilitado
            if (ShowValue)
            {
                string valueText = $"{Value:0}";
                var textSize = Font.MeasureText(valueText);
                Renderer.DrawText(
                    valueText,
                    new Vector2(absPos.X + Width + TEXT_PADDING, absPos.Y + (Height - textSize.Y) / 2),
                    ForeColor,
                    Font
                );
            }
        }

        public override void Update()
        {
            base.Update();

            if (!Enabled || !Visible || !UIEventSystem.IsSCreenFocus) return;

            Vector2 mousePos = UIEventSystem.MousePosition;
            bool isMouseOver = Contains(mousePos);

            // Comenzar arrastre
            if (UIEventSystem.IsMousePressed && isMouseOver && !_isDragging)
            {
                _isDragging = true;
                UpdateValueFromMouse(mousePos);
            }
            // Continuar arrastre
            else if (_isDragging && UIEventSystem.IsMousePressed)
            {
                UpdateValueFromMouse(mousePos);
            }
            // Finalizar arrastre
            else if (_isDragging && !UIEventSystem.IsMousePressed)
            {
                _isDragging = false;
            }
        }

        private void UpdateValueFromMouse(Vector2 mousePos)
        {
            var absPos = GetAbsolutePosition();
            float relativeX = mousePos.X - absPos.X - THUMB_WIDTH / 2;
            float percentage = Math.Clamp(relativeX / (Width - THUMB_WIDTH), 0f, 1f);
            Value = MinValue + percentage * (MaxValue - MinValue);
        }

        protected override void OnClick(Vector2 mousePos)
        {
            base.OnClick(mousePos);
            if (Enabled && !_isDragging)
            {
                UpdateValueFromMouse(mousePos);
            }
        }

        public override bool Contains(Vector2 point)
        {
            var absPos = GetAbsolutePosition();
            float trackY = absPos.Y + (Height - TRACK_HEIGHT) / 2;

            // Área interactiva incluye el thumb y la pista
            return new Rect(
                absPos.X,
                trackY - (THUMB_HEIGHT - TRACK_HEIGHT) / 2,
                Width,
                THUMB_HEIGHT).Contains(point);
        }
    }
}