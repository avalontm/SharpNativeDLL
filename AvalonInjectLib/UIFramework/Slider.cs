namespace AvalonInjectLib.UIFramework
{
    using static AvalonInjectLib.Structs;

    public class Slider : UIControl
    {
        // Propiedades del slider
        private float _value = 50f;
        private float _minValue = 0f;
        private float _maxValue = 100f;
        private bool _isDragging = false;

        // Apariencia
        public Color FillColor { get; set; } = new Color(70, 130, 180);
        public Color EmptyColor { get; set; } = new Color(60, 60, 60);
        public Color HandleColor { get; set; } = Color.White;
        public float TrackHeight { get; set; } = 6f;
        public float HandleSize { get; set; } = 16f;
        public bool ShowValue { get; set; } = true;
        public string ValueFormat { get; set; } = "F1"; // Formato para mostrar el valor
        public string Label { get; set; } = "";
        public TextStyle TextStyle { get; set; } = TextStyle.Default;

        // Eventos
        public Action<float> OnValueChanged;

        public float Value
        {
            get => _value;
            set
            {
                float newValue = Math.Clamp(value, _minValue, _maxValue);
                if (Math.Abs(_value - newValue) > float.Epsilon)
                {
                    _value = newValue;
                    OnValueChanged?.Invoke(_value);
                }
            }
        }

        public float MinValue
        {
            get => _minValue;
            set
            {
                _minValue = value;
                Value = _value; // Re-clamp el valor actual
            }
        }

        public float MaxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = value;
                Value = _value; // Re-clamp el valor actual
            }
        }

        public int DecimalPlaces { get; set; }

        public override void Draw()
        {
            if (!IsVisible) return;

            // Calcular posición y tamaño de la barra de progreso
            float trackY = Bounds.Y + (Bounds.Height - TrackHeight) / 2;
            float fillWidth = (_value - _minValue) / (_maxValue - _minValue) * Bounds.Width;

            // Dibujar barra de fondo
            Renderer.DrawRect(Bounds.X, trackY, Bounds.Width, TrackHeight, EmptyColor);

            // Dibujar barra de progreso
            if (fillWidth > 0)
            {
                Renderer.DrawRect(Bounds.X, trackY, fillWidth, TrackHeight, FillColor);
            }

            // Dibujar manejador
            float handleX = Bounds.X + fillWidth - HandleSize / 2;
            float handleY = Bounds.Y + (Bounds.Height - HandleSize) / 2;
            Color handleColor = _isDragging ? FillColor : HandleColor;

            Renderer.DrawRect(handleX, handleY, HandleSize, HandleSize, handleColor);

            // Dibujar etiqueta si existe
            if (!string.IsNullOrEmpty(Label))
            {
                Renderer.DrawText(Label, Bounds.X, Bounds.Y - 20, TextStyle.Color, TextStyle.Size);
            }

            // Mostrar valor actual si está habilitado
            if (ShowValue)
            {
                string valueText = _value.ToString(ValueFormat);
                var textSize = Renderer.MeasureText(valueText, TextStyle.Size);
                Renderer.DrawText(
                    valueText,
                    Bounds.X + Bounds.Width + 10,
                    Bounds.Y + (Bounds.Height - textSize.Y) / 2,
                    TextStyle.Color,
                    TextStyle.Size
                );
            }
        }

        public override void Update()
        {
            if (!IsVisible || !IsEnabled) return;

            base.Update();

            var mousePos = UIEventSystem.MousePosition;

            // Manejar inicio de arrastre
            if (!_isDragging && UIEventSystem.IsMousePressed)
            {
                // Verificar si se hizo clic en el manejador o cerca de la barra
                float handleX = Bounds.X + ((_value - _minValue) / (_maxValue - _minValue)) * Bounds.Width;
                float handleY = Bounds.Y + Bounds.Height / 2;

                float distToHandle = Math.Abs(mousePos.X - handleX);
                float distToTrack = Math.Abs(mousePos.Y - handleY);

                if (distToHandle < HandleSize * 1.5f && distToTrack < HandleSize * 2f)
                {
                    _isDragging = true;
                }
                else if (Bounds.Contains(mousePos.X, mousePos.Y))
                {
                    // Click directo en la barra - saltar a esa posición
                    _isDragging = true;
                    UpdateValueFromMouse(mousePos.X);
                }
            }

            // Manejar arrastre continuo
            if (_isDragging)
            {
                if (UIEventSystem.IsMouseDown)
                {
                    UpdateValueFromMouse(mousePos.X);
                }
                else
                {
                    _isDragging = false;
                }
            }
        }

        private void UpdateValueFromMouse(float mouseX)
        {
            float normalized = Math.Clamp((mouseX - Bounds.X) / Bounds.Width, 0, 1);
            Value = _minValue + normalized * (_maxValue - _minValue);
        }

        public override void Measure(Vector2 availableSize)
        {
            if (!IsVisible)
            {
                Bounds = new Rect(Bounds.X, Bounds.Y, 0, 0);
                return;
            }

            // Tamaño mínimo recomendado
            float width = 100f;
            float height = HandleSize;

            // Ajustar por etiqueta
            if (!string.IsNullOrEmpty(Label))
            {
                var labelSize = Renderer.MeasureText(Label, TextStyle.Size);
                height += 20; // Espacio para la etiqueta
            }

            // Ajustar por valor mostrado
            if (ShowValue)
            {
                var sampleValue = _maxValue.ToString(ValueFormat);
                var valueSize = Renderer.MeasureText(sampleValue, TextStyle.Size);
                width += valueSize.X + 15; // Espacio para el texto del valor
            }

            // Mantener tamaño manual si fue especificado
            if (!float.IsNaN(Bounds.Width)) width = Bounds.Width;
            if (!float.IsNaN(Bounds.Height)) height = Bounds.Height;

            Bounds = new Rect(Bounds.X, Bounds.Y, width, height);
        }
    }
}