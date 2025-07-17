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
        private const float VALUE_PADDING = 5f;
        private const float TEXT_PADDING = 5f;

        // Estados
        private bool _isDragging;
        private float _value;
        private string _text = string.Empty;
        private float _minValue = 0f;
        private float _maxValue = 100f;
        private bool _isIntegerValue = false;

        // Propiedades
        public float Value
        {
            get => _value;
            set
            {
                var newValue = Math.Clamp(value, MinValue, MaxValue);
                if (_isIntegerValue)
                {
                    newValue = (float)Math.Round(newValue);
                }

                if (Math.Abs(_value - newValue) > float.Epsilon)
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

        public float MinValue
        {
            get => _minValue;
            set
            {
                if (_minValue != value)
                {
                    _minValue = value;
                    // Asegurar que MaxValue > MinValue
                    if (_maxValue <= _minValue)
                    {
                        _maxValue = _minValue + 1f;
                    }
                    // Reclampar el valor actual
                    Value = Math.Clamp(_value, _minValue, _maxValue);
                }
            }
        }

        public float MaxValue
        {
            get => _maxValue;
            set
            {
                if (_maxValue != value)
                {
                    _maxValue = value;
                    // Asegurar que MaxValue > MinValue
                    if (_maxValue <= _minValue)
                    {
                        _minValue = _maxValue - 1f;
                    }
                    // Reclampar el valor actual
                    Value = Math.Clamp(_value, _minValue, _maxValue);
                }
            }
        }

        public bool IsIntegerValue
        {
            get => _isIntegerValue;
            set
            {
                _isIntegerValue = value;
                if (_isIntegerValue)
                {
                    Value = (float)Math.Round(Value);
                }
            }
        }

        public Color TrackColor { get; set; } = Color.FromArgb(70, 70, 70);
        public Color FillColor { get; set; } = Color.FromArgb(100, 149, 237);
        public Color ThumbColor { get; set; } = Color.White;
        public Color ThumbHoverColor { get; set; } = Color.FromArgb(220, 220, 220);
        public Color ThumbBorderColor { get; set; } = Color.FromArgb(150, 150, 150);
        public bool ShowValue { get; set; } = true;
        public Font Font { get; set; } = Font.GetDefaultFont();
        public HorizontalAlignment ValueAlignment { get; set; } = HorizontalAlignment.Right;

        // Evento
        public Action<float>? ValueChanged;

        public Slider()
        {
            Width = 200f;
            Height = Math.Max(THUMB_HEIGHT, Font.GetTextHeight(Text) + (THUMB_HEIGHT / 2));
            IsFocusable = true;
            _value = _minValue; // Inicializar con valor mínimo válido
        }

        private void UpdateLayout()
        {
            // Ajustar altura si el texto es más grande que el thumb
            if (!string.IsNullOrEmpty(Text))
            {
                Height = Math.Max(THUMB_HEIGHT, Font.GetTextHeight(Text) + (THUMB_HEIGHT / 2));
            }
        }

        private float GetValuePercentage()
        {
            float range = MaxValue - MinValue;
            if (Math.Abs(range) < float.Epsilon)
            {
                return 0f; // Evitar división por cero
            }
            return Math.Clamp((Value - MinValue) / range, 0f, 1f);
        }

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();
            float trackY = absPos.Y + (Height - TRACK_HEIGHT) / 2;

            // Calcular posición del thumb usando porcentaje seguro
            float percentage = GetValuePercentage();
            float availableWidth = Width - THUMB_WIDTH;
            float thumbX = absPos.X + availableWidth * percentage;

            // Dibujar texto descriptivo si existe
            if (!string.IsNullOrEmpty(Text))
            {
                Renderer.DrawText(
                    Text,
                    new Vector2(absPos.X, absPos.Y - (THUMB_HEIGHT / 2)),
                    ForeColor,
                    Font
                );
            }

            // Dibujar track (fondo)
            Renderer.DrawRect(
                new Rect(absPos.X, trackY, Width, TRACK_HEIGHT),
                TrackColor
            );

            // Dibujar fill (parte llena) - corregido para mantener proporciones
            float fillWidth = availableWidth * percentage + THUMB_WIDTH / 2;
            Renderer.DrawRect(
                new Rect(absPos.X, trackY, fillWidth, TRACK_HEIGHT),
                FillColor
            );

            // Dibujar thumb (rectángulo)
            Color currentThumbColor = _isDragging || Contains(UIEventSystem.MousePosition) ?
                ThumbHoverColor : ThumbColor;

            float thumbY = trackY - (THUMB_HEIGHT - TRACK_HEIGHT) / 2;
            Renderer.DrawRect(
                new Rect(thumbX, thumbY, THUMB_WIDTH, THUMB_HEIGHT),
                currentThumbColor
            );

            // Dibujar borde del thumb
            Renderer.DrawRectOutline(
                new Rect(thumbX, thumbY, THUMB_WIDTH, THUMB_HEIGHT),
                ThumbBorderColor,
                1f
            );

            // Mostrar valor numérico si está habilitado
            // Mostrar valor numérico si está habilitado
            if (ShowValue)
            {
                string valueText = FormatValue(Value);
                var textSize = Font.MeasureText(valueText);
                float valueX = 0f;
                float valueY = (absPos.Y+ (THUMB_HEIGHT / 2)) + (Height - textSize.Y) / 2;

                // Ajustar posición X considerando el padding
                switch (ValueAlignment)
                {
                    case HorizontalAlignment.Left:
                        valueX = absPos.X + VALUE_PADDING;
                        break;
                    case HorizontalAlignment.Center:
                        valueX = absPos.X + (Width - textSize.X) / 2;
                        break;
                    case HorizontalAlignment.Right:
                        valueX = absPos.X + Width - textSize.X - VALUE_PADDING;
                        break;
                }

                // Asegurarnos de que no se salga por la izquierda
                valueX = Math.Max(absPos.X, valueX);

                Renderer.DrawText(
                    valueText,
                    new Vector2(valueX, valueY),
                    ForeColor,
                    Font
                );
            }
        }

        private string FormatValue(float value)
        {
            if (_isIntegerValue)
            {
                // Formato para valores enteros
                int intValue = (int)Math.Round(value);
                float range = MaxValue - MinValue;

                if (range > 1000)
                {
                    return $"{intValue:N0}"; // Formato con separadores de miles
                }
                return $"{intValue}";
            }
            else
            {
                // Formato para valores decimales
                float range = MaxValue - MinValue;
                if (range >= 100)
                {
                    return $"{value:0}"; // Sin decimales para rangos grandes
                }
                else if (range >= 10)
                {
                    return $"{value:0.0}"; // 1 decimal para rangos medianos
                }
                else
                {
                    return $"{value:0.00}"; // 2 decimales para rangos pequeños
                }
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
            float availableWidth = Width - THUMB_WIDTH;

            // Calcular posición relativa considerando el centro del thumb
            float relativeX = mousePos.X - absPos.X - THUMB_WIDTH / 2;
            float percentage = Math.Clamp(relativeX / availableWidth, 0f, 1f);

            // Convertir porcentaje a valor en el rango
            float newValue = MinValue + percentage * (MaxValue - MinValue);

            if (_isIntegerValue)
            {
                newValue = (float)Math.Round(newValue);
            }

            Value = newValue;
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

        // Métodos auxiliares para testing/debugging
        public void SetRange(float min, float max, bool isInteger = false)
        {
            if (max <= min)
            {
                throw new ArgumentException("MaxValue debe ser mayor que MinValue");
            }

            _minValue = min;
            _maxValue = max;
            _isIntegerValue = isInteger;
            Value = Math.Clamp(_value, min, max);
        }

        public void SetValue(float value)
        {
            Value = value;
        }
    }
}