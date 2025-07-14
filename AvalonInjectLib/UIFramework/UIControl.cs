using static AvalonInjectLib.Structs;

namespace AvalonInjectLib.UIFramework
{
    public abstract class UIControl
    {
        private Rect _bounds;
        private Thickness _margin = new Thickness(0);
        private Thickness _padding = new Thickness(0);
        private bool _measureInvalidated = true;
        private bool _arrangeInvalidated = true;
        private bool _layoutInvalidated = true;
        private bool _isInvalidating = false; // Prevenir recursión infinita

        // Propiedades de Grid
        private int _gridColumn = 0;
        private int _gridRow = 0;
        private int _gridColumnSpan = 1;
        private int _gridRowSpan = 1;

        public Vector2 DesiredSize { get; protected set; }

        public Rect Bounds
        {
            get => _bounds;
            set
            {
                if (_bounds.Equals(value)) return;

                _bounds = value;
                OnBoundsChanged();
                InvalidateArrange();
            }
        }

        public float Width
        {
            get => _bounds.Width;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value)) return;
                if (_bounds.Width.Equals(value)) return;

                _bounds.Width = value;
                OnBoundsChanged();
                InvalidateArrange();
            }
        }

        public float Height
        {
            get => _bounds.Height;
            set
            {
                if (float.IsNaN(value) || float.IsInfinity(value)) return;
                if (_bounds.Height.Equals(value)) return;

                _bounds.Height = value;
                OnBoundsChanged();
                InvalidateArrange();
            }
        }

        public Thickness Margin
        {
            get => _margin;
            set
            {
                if (_margin.Equals(value)) return;

                _margin = value;
                InvalidateMeasure();
            }
        }

        public Thickness Padding
        {
            get => _padding;
            set
            {
                if (_padding.Equals(value)) return;

                _padding = value;
                InvalidateMeasure();
            }
        }

        // Propiedades de Grid
        public int GridColumn
        {
            get => _gridColumn;
            set
            {
                if (_gridColumn == value) return;
                _gridColumn = Math.Max(0, value);
                InvalidateArrange();
            }
        }

        public int GridRow
        {
            get => _gridRow;
            set
            {
                if (_gridRow == value) return;
                _gridRow = Math.Max(0, value);
                InvalidateArrange();
            }
        }

        public int GridColumnSpan
        {
            get => _gridColumnSpan;
            set
            {
                if (_gridColumnSpan == value) return;
                _gridColumnSpan = Math.Max(1, value);
                InvalidateArrange();
            }
        }

        public int GridRowSpan
        {
            get => _gridRowSpan;
            set
            {
                if (_gridRowSpan == value) return;
                _gridRowSpan = Math.Max(1, value);
                InvalidateArrange();
            }
        }

        public Color BackgroundColor = Color.Transparent;
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled = true;
        public bool IsHovered;
        public bool WasMouseDown;
        public string Tag = "";
        public object UserData;

        // Propiedades de layout
        public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Stretch;
        public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Stretch;

        // Eventos
        public Action<Vector2> OnClick;
        public Action<Vector2> OnMouseDown;
        public Action<Vector2> OnMouseUp;
        public Action OnMouseEnter;
        public Action OnMouseLeave;
        public Action OnFocusGained;
        public Action OnFocusLost;
        public Action OnLayoutUpdated;

        /// <summary>
        /// Obtiene o establece el control padre de este control
        /// </summary>
        public UIControl Parent { get; set; }

        public bool HasFocus => UIFrameworkSystem.FocusedControl == this;

        /// <summary>
        /// Indica si este control está ubicado dentro de un Grid
        /// </summary>
        public bool IsInGrid => Parent != null && Parent.GetType().Name.Contains("Grid");

        /// <summary>
        /// Obtiene información sobre la posición del control en el Grid
        /// </summary>
        public GridPosition GetGridPosition()
        {
            return new GridPosition
            {
                Column = GridColumn,
                Row = GridRow,
                ColumnSpan = GridColumnSpan,
                RowSpan = GridRowSpan,
                IsInGrid = IsInGrid
            };
        }

        /// <summary>
        /// Establece la posición del control en el Grid
        /// </summary>
        public void SetGridPosition(int column, int row, int columnSpan = 1, int rowSpan = 1)
        {
            GridColumn = column;
            GridRow = row;
            GridColumnSpan = columnSpan;
            GridRowSpan = rowSpan;
        }

        public abstract void Draw();

        public virtual void Update()
        {
            if (!IsVisible || !IsEnabled) return;

            // Procesar eventos solo si es visible y está habilitado
            UIEventSystem.ProcessEvents(this);

            // Actualizar layout si es necesario
            if (_layoutInvalidated)
            {
                UpdateLayout();
            }
        }

        protected virtual void UpdateLayout()
        {
            if (_measureInvalidated)
            {
                Measure(new Vector2(float.PositiveInfinity));
                _measureInvalidated = false;
            }

            if (_arrangeInvalidated)
            {
                // Si no tenemos padre, usamos nuestros propios bounds
                Arrange(Parent?.GetContentRect() ?? Bounds);
                _arrangeInvalidated = false;
            }

            _layoutInvalidated = false;
        }

        protected virtual void OnBoundsChanged()
        {
            // Puede ser sobrescrito por clases derivadas
        }

        public virtual void Measure(Vector2 availableSize)
        {
            if (!IsVisible)
            {
                DesiredSize = Vector2.Zero;
                return;
            }

            // Validar entrada
            if (float.IsNaN(availableSize.X) || float.IsNaN(availableSize.Y))
            {
                DesiredSize = Vector2.Zero;
                return;
            }

            // Restamos los márgenes del tamaño disponible
            var availableSizeAfterMargins = new Vector2(
                Math.Max(0, availableSize.X - Margin.Left - Margin.Right),
                Math.Max(0, availableSize.Y - Margin.Top - Margin.Bottom)
            );

            // Implementación base - las clases derivadas deben sobrescribir esto
            var desiredSizeWithoutMargins = MeasureCore(availableSizeAfterMargins);

            DesiredSize = new Vector2(
                desiredSizeWithoutMargins.X + Margin.Left + Margin.Right,
                desiredSizeWithoutMargins.Y + Margin.Top + Margin.Bottom
            );
        }

        protected virtual Vector2 MeasureCore(Vector2 availableSize)
        {
            // Implementación por defecto: usar el tamaño mínimo necesario
            return new Vector2(
                Math.Max(0, Padding.Left + Padding.Right),
                Math.Max(0, Padding.Top + Padding.Bottom)
            );
        }

        public virtual void Arrange(Rect finalRect)
        {
            if (!IsVisible) return;

            // Aplicar márgenes
            var arrangeRect = new Rect(
                finalRect.X + Margin.Left,
                finalRect.Y + Margin.Top,
                Math.Max(0, finalRect.Width - Margin.Left - Margin.Right),
                Math.Max(0, finalRect.Height - Margin.Top - Margin.Bottom)
            );

            // Calcular tamaño sin márgenes
            var desiredSizeWithoutMargins = new Vector2(
                Math.Max(0, DesiredSize.X - Margin.Left - Margin.Right),
                Math.Max(0, DesiredSize.Y - Margin.Top - Margin.Bottom)
            );

            // Aplicar alineación horizontal
            float x = arrangeRect.X;
            float width = arrangeRect.Width;

            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    width = Math.Min(desiredSizeWithoutMargins.X, arrangeRect.Width);
                    break;
                case HorizontalAlignment.Center:
                    width = Math.Min(desiredSizeWithoutMargins.X, arrangeRect.Width);
                    x += (arrangeRect.Width - width) / 2;
                    break;
                case HorizontalAlignment.Right:
                    width = Math.Min(desiredSizeWithoutMargins.X, arrangeRect.Width);
                    x += arrangeRect.Width - width;
                    break;
                case HorizontalAlignment.Stretch:
                    // Usar todo el ancho disponible
                    break;
            }

            // Aplicar alineación vertical
            float y = arrangeRect.Y;
            float height = arrangeRect.Height;

            switch (VerticalAlignment)
            {
                case VerticalAlignment.Top:
                    height = Math.Min(desiredSizeWithoutMargins.Y, arrangeRect.Height);
                    break;
                case VerticalAlignment.Center:
                    height = Math.Min(desiredSizeWithoutMargins.Y, arrangeRect.Height);
                    y += (arrangeRect.Height - height) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    height = Math.Min(desiredSizeWithoutMargins.Y, arrangeRect.Height);
                    y += arrangeRect.Height - height;
                    break;
                case VerticalAlignment.Stretch:
                    // Usar toda la altura disponible
                    break;
            }

            var finalBounds = new Rect(x, y, width, height);
            ArrangeCore(finalBounds);

            Bounds = finalBounds;
            OnLayoutUpdated?.Invoke();
        }

        protected virtual void ArrangeCore(Rect finalRect)
        {
            // Implementación por defecto: usar el rectángulo tal como viene
        }

        public Rect GetContentRect()
        {
            return new Rect(
                Bounds.X + Padding.Left,
                Bounds.Y + Padding.Top,
                Math.Max(0, Bounds.Width - Padding.Left - Padding.Right),
                Math.Max(0, Bounds.Height - Padding.Top - Padding.Bottom)
            );
        }

        protected void RequestLayoutUpdate()
        {
            InvalidateLayout();
        }

        public virtual void SetPosition(Vector2 position) => Bounds = new Rect(position, Bounds.Size);

        public virtual void SetSize(Vector2 size)
        {
            Width = size.X;
            Height = size.Y;
        }

        public virtual void SetBounds(Rect bounds) => Bounds = bounds;

        public virtual void SetBounds(float x, float y, float width, float height) =>
            Bounds = new Rect(x, y, width, height);

        public virtual void Focus()
        {
            if (UIFrameworkSystem.FocusedControl == this) return;

            UIFrameworkSystem.FocusedControl?.OnFocusLost?.Invoke();
            UIFrameworkSystem.FocusedControl = this;
            OnFocusGained?.Invoke();
        }

        // Métodos para manejar el sistema de layout - CON PROTECCIÓN CONTRA RECURSIÓN
        protected virtual void InvalidateMeasure()
        {
            if (_measureInvalidated || _isInvalidating) return;

            _measureInvalidated = true;
            _layoutInvalidated = true;

            // Propagar hacia arriba con protección
            if (Parent != null && !_isInvalidating)
            {
                _isInvalidating = true;
                Parent.InvalidateMeasure();
                _isInvalidating = false;
            }
        }

        protected virtual void InvalidateArrange()
        {
            if (_arrangeInvalidated || _isInvalidating) return;

            _arrangeInvalidated = true;
            _layoutInvalidated = true;

            // Propagar hacia arriba con protección
            if (Parent != null && !_isInvalidating)
            {
                _isInvalidating = true;
                Parent.InvalidateArrange();
                _isInvalidating = false;
            }
        }

        protected virtual void InvalidateLayout()
        {
            if (_isInvalidating) return;

            _isInvalidating = true;
            InvalidateMeasure();
            InvalidateArrange();
            _isInvalidating = false;
        }
    }

    /// <summary>
    /// Estructura que contiene información sobre la posición de un control en el Grid
    /// </summary>
    public struct GridPosition
    {
        public int Column;
        public int Row;
        public int ColumnSpan;
        public int RowSpan;
        public bool IsInGrid;

        public override string ToString()
        {
            if (!IsInGrid)
                return "No está en Grid";

            return $"Column: {Column}, Row: {Row}, ColumnSpan: {ColumnSpan}, RowSpan: {RowSpan}";
        }
    }
}