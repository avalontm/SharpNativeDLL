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
        private const float VERTICAL_SPACING = 2f; // Espacio entre elementos

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
                    UpdateLayout(); // Actualizar layout por si cambia el formato del valor
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
                    UpdateLayout(); // Actualizar layout por si cambia el formato del valor
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
                UpdateLayout(); // Actualizar layout porque cambia el formato del valor
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
            Height = 32f;
            Width = 200f;
            _value = _minValue; // Inicializar con valor mínimo válido
            UpdateLayout();
            IsFocusable = true;
        }

        private void UpdateLayout()
        {
            float totalHeight = 0f;

            // Altura del texto descriptivo (si existe)
            if (!string.IsNullOrEmpty(Text))
            {
                totalHeight += Font.GetTextHeight(Text) + VERTICAL_SPACING;
            }

            // Altura del slider (thumb)
            totalHeight += THUMB_HEIGHT;

            // Altura del valor mostrado (si está habilitado)
            if (ShowValue)
            {
                totalHeight += VERTICAL_SPACING + Font.GetTextHeight(FormatValue(Value));
            }

            // Asignar la altura total calculada
            Height = totalHeight;
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

        private float GetTextHeight()
        {
            return !string.IsNullOrEmpty(Text) ? Font.GetTextHeight(Text) : 0f;
        }

        private float GetValueHeight()
        {
            return ShowValue ? Font.GetTextHeight(FormatValue(Value)) : 0f;
        }

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();
            float currentY = absPos.Y;

            // Dibujar texto descriptivo si existe
            if (!string.IsNullOrEmpty(Text))
            {
                Renderer.DrawText(
                    Text,
                    new Vector2(absPos.X, currentY),
                    ForeColor,
                    Font
                );
                currentY += Font.GetTextHeight(Text) + VERTICAL_SPACING;
            }

            // Calcular posición del track y thumb
            float trackY = currentY + (THUMB_HEIGHT - TRACK_HEIGHT) / 2;
            float percentage = GetValuePercentage();
            float availableWidth = Width - THUMB_WIDTH;
            float thumbX = absPos.X + availableWidth * percentage;

            // Dibujar track (fondo)
            Renderer.DrawRect(
                new Rect(absPos.X, trackY, Width, TRACK_HEIGHT),
                TrackColor
            );

            // Dibujar fill (parte llena)
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
            if (ShowValue)
            {
                currentY += THUMB_HEIGHT + VERTICAL_SPACING;
                string valueText = FormatValue(Value);
                var textSize = Font.MeasureText(valueText);
                float valueX = 0f;

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
                    new Vector2(valueX, currentY),
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
                UpdateValueFromMouse();
            }
            // Continuar arrastre
            else if (_isDragging && UIEventSystem.IsMousePressed)
            {
                UpdateValueFromMouse();
            }
            // Finalizar arrastre
            else if (_isDragging && !UIEventSystem.IsMousePressed)
            {
                _isDragging = false;
            }
        }

        private void UpdateValueFromMouse()
        {
            var mousePos = UIEventSystem.MousePosition;
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

        protected override void OnClick(object sender, Vector2 pos)
        {
            base.OnClick(sender, pos);
            if (Enabled && !_isDragging)
            {
                UpdateValueFromMouse();
            }
        }

        public override bool Contains(Vector2 point)
        {
            var absPos = GetAbsolutePosition();
            float sliderY = absPos.Y;

            // Ajustar Y si hay texto descriptivo
            if (!string.IsNullOrEmpty(Text))
            {
                sliderY += Font.GetTextHeight(Text) + VERTICAL_SPACING;
            }

            float trackY = sliderY + (THUMB_HEIGHT - TRACK_HEIGHT) / 2;

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

        // Método para actualizar el layout cuando cambian propiedades que afectan la altura
        public void RefreshLayout()
        {
            UpdateLayout();
        }
    }
}