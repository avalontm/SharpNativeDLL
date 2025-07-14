using static AvalonInjectLib.Structs;

namespace AvalonInjectLib.UIFramework
{
    /// <summary>
    /// Control Grid que organiza elementos en filas y columnas
    /// </summary>
    public class Grid : UIControl
    {
        private List<GridLength> _columnDefinitions = new List<GridLength>();
        private List<GridLength> _rowDefinitions = new List<GridLength>();
        private List<UIControl> _children = new List<UIControl>();

        // Cache para optimización
        private float[] _columnWidths;
        private float[] _rowHeights;
        private float[] _columnPositions;
        private float[] _rowPositions;
        private bool _gridLayoutInvalidated = true;

        // Debug visual
        public bool ShowGridLines { get; set; } = true;
        public bool ShowCellColors { get; set; } = true;
        public Color GridLineColor { get; set; } = Color.Gray;

        // Colores para debug de celdas
        private static readonly Color[] DebugColors = {
            Color.FromArgb(50, 255, 0, 0),     // Rojo transparente
            Color.FromArgb(50, 0, 255, 0),     // Verde transparente
            Color.FromArgb(50, 0, 0, 255),     // Azul transparente
            Color.FromArgb(50, 255, 255, 0),   // Amarillo transparente
            Color.FromArgb(50, 255, 0, 255),   // Magenta transparente
            Color.FromArgb(50, 0, 255, 255),   // Cyan transparente
            Color.FromArgb(50, 128, 128, 128), // Gris transparente
            Color.FromArgb(50, 255, 128, 0),   // Naranja transparente
        };

        /// <summary>
        /// Definiciones de columnas del Grid
        /// </summary>
        public List<GridLength> ColumnDefinitions
        {
            get => _columnDefinitions;
            set
            {
                _columnDefinitions = value ?? new List<GridLength>();
                InvalidateGridLayout();
            }
        }

        /// <summary>
        /// Definiciones de filas del Grid
        /// </summary>
        public List<GridLength> RowDefinitions
        {
            get => _rowDefinitions;
            set
            {
                _rowDefinitions = value ?? new List<GridLength>();
                InvalidateGridLayout();
            }
        }

        /// <summary>
        /// Controles hijos del Grid
        /// </summary>
        public List<UIControl> Children => _children;

        /// <summary>
        /// Número de columnas en el Grid
        /// </summary>
        public int ColumnCount => _columnDefinitions.Count;

        /// <summary>
        /// Número de filas en el Grid
        /// </summary>
        public int RowCount => _rowDefinitions.Count;

        public Grid()
        {
            // CORRECCIÓN CRÍTICA: No agregar definiciones por defecto
            // El Grid debe usar las definiciones que se agreguen explícitamente
        }

        /// <summary>
        /// Agrega un control al Grid y lo posiciona usando los métodos estáticos
        /// </summary>
        /// <param name="control">Control a agregar</param>
        public void AddChild(UIControl control)
        {
            if (control == null) return;
            if (_children.Contains(control)) return;

            control.Parent = this;
            _children.Add(control);
            AutoExpandGrid(control);
            InvalidateGridLayout();
        }

        /// <summary>
        /// Método de conveniencia para agregar y posicionar en una sola llamada
        /// </summary>
        /// <param name="control">Control a agregar</param>
        /// <param name="column">Columna</param>
        /// <param name="row">Fila</param>
        /// <param name="columnSpan">Span de columnas</param>
        /// <param name="rowSpan">Span de filas</param>
        public void AddChild(UIControl control, int column, int row, int columnSpan = 1, int rowSpan = 1)
        {
            if (control == null) return;

            // Usar los métodos estáticos para establecer la posición
            SetPosition(control, column, row, columnSpan, rowSpan);
            AddChild(control);
        }

        /// <summary>
        /// Elimina un control hijo del Grid
        /// </summary>
        public void RemoveChild(UIControl control)
        {
            if (control == null) return;

            if (_children.Remove(control))
            {
                control.Parent = null;
                InvalidateGridLayout();
            }
        }

        /// <summary>
        /// Elimina todos los controles hijos
        /// </summary>
        public void ClearChildren()
        {
            foreach (var child in _children)
            {
                child.Parent = null;
            }
            _children.Clear();
            InvalidateGridLayout();
        }

        /// <summary>
        /// CORRECCIÓN CRÍTICA: Expande automáticamente el Grid para acomodar un control
        /// </summary>
        private void AutoExpandGrid(UIControl control)
        {
            var maxColumn = control.GridColumn + control.GridColumnSpan;
            var maxRow = control.GridRow + control.GridRowSpan;

            // Expandir columnas si es necesario
            while (_columnDefinitions.Count < maxColumn)
            {
                _columnDefinitions.Add(GridLength.Auto);
            }

            // Expandir filas si es necesario
            while (_rowDefinitions.Count < maxRow)
            {
                _rowDefinitions.Add(GridLength.Auto);
            }
        }

        /// <summary>
        /// Invalida el layout del Grid
        /// </summary>
        private void InvalidateGridLayout()
        {
            _gridLayoutInvalidated = true;
            InvalidateMeasure();
        }

        protected override Vector2 MeasureCore(Vector2 availableSize)
        {
            // CORRECCIÓN CRÍTICA: Verificar que hay definiciones de grid válidas
            if (_columnDefinitions.Count == 0 || _rowDefinitions.Count == 0)
            {
                return new Vector2(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);
            }

            if (_children.Count == 0)
                return new Vector2(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);

            // Asegurar que el Grid se expanda para todos los controles
            foreach (var child in _children)
            {
                AutoExpandGrid(child);
            }

            // Calcular tamaño de contenido respetando el padding del Grid
            var contentSize = new Vector2(
                Math.Max(0, availableSize.X - Padding.Left - Padding.Right),
                Math.Max(0, availableSize.Y - Padding.Top - Padding.Bottom)
            );

            // Medir todos los controles hijos
            foreach (var child in _children)
            {
                child.Measure(contentSize);
            }

            // Calcular tamaños de columnas y filas
            CalculateGridSizes(contentSize);

            // El tamaño deseado es la suma de todas las columnas y filas + padding del Grid
            var totalWidth = _columnWidths?.Sum() ?? 0;
            var totalHeight = _rowHeights?.Sum() ?? 0;

            return new Vector2(
                totalWidth + Padding.Left + Padding.Right,
                totalHeight + Padding.Top + Padding.Bottom
            );
        }

        protected override void ArrangeCore(Rect finalRect)
        {
            if (_columnDefinitions.Count == 0 || _rowDefinitions.Count == 0 || _children.Count == 0)
                return;

            // Calcular el área de contenido respetando el padding del Grid
            var contentArea = new Rect(
                finalRect.X + Padding.Left,
                finalRect.Y + Padding.Top,
                Math.Max(0, finalRect.Width - Padding.Left - Padding.Right),
                Math.Max(0, finalRect.Height - Padding.Top - Padding.Bottom)
            );

            // Si el padre es una Window, restar la altura de la barra de título
            if (Parent is Window window)
            {
                contentArea.Y += window.TitleBarHeight;
                contentArea.Height = Math.Max(0, contentArea.Height - window.TitleBarHeight);
            }

            // Verificar que el área de contenido es válida
            if (contentArea.Width <= 0 || contentArea.Height <= 0) return;

            // Recalcular tamaños con el espacio final disponible
            var contentSize = new Vector2(contentArea.Width, contentArea.Height);
            CalculateGridSizes(contentSize);

            // Calcular posiciones acumulativas
            CalculatePositions();

            // Organizar cada control hijo
            foreach (var child in _children)
            {
                ArrangeChild(child, contentArea);
            }

            _gridLayoutInvalidated = false;
        }

        /// <summary>
        /// Calcula los tamaños de columnas y filas
        /// </summary>
        private void CalculateGridSizes(Vector2 availableSize)
        {
            _columnWidths = CalculateColumnWidths(availableSize.X);
            _rowHeights = CalculateRowHeights(availableSize.Y);
        }

        /// <summary>
        /// CORRECCIÓN CRÍTICA: Calcula el ancho de cada columna
        /// </summary>
        private float[] CalculateColumnWidths(float availableWidth)
        {
            var widths = new float[ColumnCount];
            var remainingWidth = availableWidth;
            var starColumns = new List<int>();
            var totalStars = 0f;

            // Primera pasada: columnas Pixel y Auto
            for (int i = 0; i < ColumnCount; i++)
            {
                var def = _columnDefinitions[i];

                switch (def.UnitType)
                {
                    case GridUnitType.Pixel:
                        widths[i] = def.Value;
                        remainingWidth -= def.Value;
                        break;

                    case GridUnitType.Auto:
                        widths[i] = Math.Max(def.Value, CalculateAutoColumnWidth(i));
                        remainingWidth -= widths[i];
                        break;

                    case GridUnitType.Star:
                        starColumns.Add(i);
                        totalStars += def.Value;
                        break;
                }
            }

            remainingWidth = Math.Max(0, remainingWidth);

            // Segunda pasada: columnas Star
            if (starColumns.Count > 0 && totalStars > 0)
            {
                foreach (var columnIndex in starColumns)
                {
                    var def = _columnDefinitions[columnIndex];
                    widths[columnIndex] = (def.Value / totalStars) * remainingWidth;
                }
            }

            return widths;
        }

        /// <summary>
        /// CORRECCIÓN CRÍTICA: Calcula la altura de cada fila
        /// </summary>
        private float[] CalculateRowHeights(float availableHeight)
        {
            var heights = new float[RowCount];
            var remainingHeight = availableHeight;
            var starRows = new List<int>();
            var totalStars = 0f;

            // Primera pasada: filas Pixel y Auto
            for (int i = 0; i < RowCount; i++)
            {
                var def = _rowDefinitions[i];

                switch (def.UnitType)
                {
                    case GridUnitType.Pixel:
                        heights[i] = def.Value;
                        remainingHeight -= def.Value;
                        break;

                    case GridUnitType.Auto:
                        heights[i] = Math.Max(def.Value, CalculateAutoRowHeight(i));
                        remainingHeight -= heights[i];
                        break;

                    case GridUnitType.Star:
                        starRows.Add(i);
                        totalStars += def.Value;
                        break;
                }
            }

            remainingHeight = Math.Max(0, remainingHeight);

            // Segunda pasada: filas Star
            if (starRows.Count > 0 && totalStars > 0)
            {
                foreach (var rowIndex in starRows)
                {
                    var def = _rowDefinitions[rowIndex];
                    heights[rowIndex] = (def.Value / totalStars) * remainingHeight;
                }
            }

            return heights;
        }

        /// <summary>
        /// Calcula el ancho automático de una columna
        /// </summary>
        private float CalculateAutoColumnWidth(int column)
        {
            float maxWidth = 0;

            foreach (var child in _children)
            {
                if (child.GridColumn == column)
                {
                    var childDesiredWidth = child.DesiredSize.X;

                    // Incluir el margen del control en el cálculo
                    if (child.Margin != null)
                    {
                        childDesiredWidth += child.Margin.Left + child.Margin.Right;
                    }

                    // Si el control ocupa múltiples columnas, distribuir el ancho
                    if (child.GridColumnSpan > 1)
                    {
                        var endColumn = Math.Min(column + child.GridColumnSpan, ColumnCount);
                        var autoColumnsInSpan = 0;

                        for (int i = column; i < endColumn; i++)
                        {
                            if (i < _columnDefinitions.Count && _columnDefinitions[i].UnitType == GridUnitType.Auto)
                            {
                                autoColumnsInSpan++;
                            }
                        }

                        if (autoColumnsInSpan > 0)
                        {
                            childDesiredWidth /= autoColumnsInSpan;
                        }
                    }

                    maxWidth = Math.Max(maxWidth, childDesiredWidth);
                }
            }

            return maxWidth;
        }

        /// <summary>
        /// Calcula la altura automática de una fila
        /// </summary>
        private float CalculateAutoRowHeight(int row)
        {
            float maxHeight = 0;

            foreach (var child in _children)
            {
                if (child.GridRow == row)
                {
                    var childDesiredHeight = child.DesiredSize.Y;

                    // Incluir el margen del control en el cálculo
                    if (child.Margin != null)
                    {
                        childDesiredHeight += child.Margin.Top + child.Margin.Bottom;
                    }

                    // Si el control ocupa múltiples filas, distribuir la altura
                    if (child.GridRowSpan > 1)
                    {
                        var endRow = Math.Min(row + child.GridRowSpan, RowCount);
                        var autoRowsInSpan = 0;

                        for (int i = row; i < endRow; i++)
                        {
                            if (i < _rowDefinitions.Count && _rowDefinitions[i].UnitType == GridUnitType.Auto)
                            {
                                autoRowsInSpan++;
                            }
                        }

                        if (autoRowsInSpan > 0)
                        {
                            childDesiredHeight /= autoRowsInSpan;
                        }
                    }

                    maxHeight = Math.Max(maxHeight, childDesiredHeight);
                }
            }

            return maxHeight;
        }

        /// <summary>
        /// CORRECCIÓN CRÍTICA: Calcula las posiciones acumulativas de columnas y filas
        /// </summary>
        private void CalculatePositions()
        {
            // Verificar que tenemos datos válidos
            if (_columnWidths == null || _rowHeights == null) return;

            // Posiciones de columnas
            _columnPositions = new float[ColumnCount + 1];
            _columnPositions[0] = 0;
            for (int i = 0; i < ColumnCount; i++)
            {
                _columnPositions[i + 1] = _columnPositions[i] + _columnWidths[i];
            }

            // Posiciones de filas
            _rowPositions = new float[RowCount + 1];
            _rowPositions[0] = 0;
            for (int i = 0; i < RowCount; i++)
            {
                _rowPositions[i + 1] = _rowPositions[i] + _rowHeights[i];
            }
        }

        /// <summary>
        /// CORRECCIÓN CRÍTICA: Organiza un control hijo en su celda correspondiente
        /// </summary>
        private void ArrangeChild(UIControl child, Rect contentArea)
        {
            // CORRECCIÓN CRÍTICA: Validar que las posiciones están dentro del rango válido
            var column = Math.Max(0, Math.Min(child.GridColumn, ColumnCount - 1));
            var row = Math.Max(0, Math.Min(child.GridRow, RowCount - 1));
            var columnSpan = Math.Max(1, Math.Min(child.GridColumnSpan, ColumnCount - column));
            var rowSpan = Math.Max(1, Math.Min(child.GridRowSpan, RowCount - row));

            // CORRECCIÓN CRÍTICA: Verificar que tenemos posiciones válidas
            if (_columnPositions == null || _rowPositions == null) return;

            // Calcular el rectángulo de la celda
            var cellX = contentArea.X + _columnPositions[column];
            var cellY = contentArea.Y + _rowPositions[row];
            var cellWidth = _columnPositions[column + columnSpan] - _columnPositions[column];
            var cellHeight = _rowPositions[row + rowSpan] - _rowPositions[row];

            var cellRect = new Rect(cellX, cellY, cellWidth, cellHeight);

            // Aplicar márgenes del control DENTRO de la celda
            var arrangeRect = cellRect;
            if (child.Margin != null)
            {
                arrangeRect = new Rect(
                    cellRect.X + child.Margin.Left,
                    cellRect.Y + child.Margin.Top,
                    Math.Max(0, cellRect.Width - child.Margin.Left - child.Margin.Right),
                    Math.Max(0, cellRect.Height - child.Margin.Top - child.Margin.Bottom)
                );
            }

            // Aplicar alineación dentro del rectángulo disponible
            var finalRect = ApplyAlignment(child, arrangeRect);

            // Organizar el control en su posición final
            child.Arrange(finalRect);
        }

        /// <summary>
        /// Aplica la alineación horizontal y vertical del control dentro de su celda
        /// </summary>
        private Rect ApplyAlignment(UIControl child, Rect availableRect)
        {
            var desiredSize = child.DesiredSize;
            var arrangeRect = availableRect;

            // Obtener la alineación, usando Stretch como valor por defecto
            var horizontalAlignment = child.HorizontalAlignment;
            var verticalAlignment = child.VerticalAlignment;

            // Alineación horizontal
            switch (horizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    arrangeRect.Width = Math.Min(desiredSize.X, availableRect.Width);
                    break;

                case HorizontalAlignment.Center:
                    var preferredWidth = Math.Min(desiredSize.X, availableRect.Width);
                    arrangeRect.X += (availableRect.Width - preferredWidth) / 2;
                    arrangeRect.Width = preferredWidth;
                    break;

                case HorizontalAlignment.Right:
                    var rightWidth = Math.Min(desiredSize.X, availableRect.Width);
                    arrangeRect.X += availableRect.Width - rightWidth;
                    arrangeRect.Width = rightWidth;
                    break;

                case HorizontalAlignment.Stretch:
                default:
                    // Usar todo el ancho disponible de la celda
                    break;
            }

            // Alineación vertical
            switch (verticalAlignment)
            {
                case VerticalAlignment.Top:
                    arrangeRect.Height = Math.Min(desiredSize.Y, availableRect.Height);
                    break;

                case VerticalAlignment.Center:
                    var preferredHeight = Math.Min(desiredSize.Y, availableRect.Height);
                    arrangeRect.Y += (availableRect.Height - preferredHeight) / 2;
                    arrangeRect.Height = preferredHeight;
                    break;

                case VerticalAlignment.Bottom:
                    var bottomHeight = Math.Min(desiredSize.Y, availableRect.Height);
                    arrangeRect.Y += availableRect.Height - bottomHeight;
                    arrangeRect.Height = bottomHeight;
                    break;

                case VerticalAlignment.Stretch:
                default:
                    // Usar toda la altura disponible de la celda
                    break;
            }

            return arrangeRect;
        }

        public override void Update()
        {
            base.Update();

            // Actualizar controles hijos
            foreach (var child in _children)
            {
                child.Update();
            }
        }

        public override void Draw()
        {
            if (!IsVisible) return;

            // CORRECCIÓN CRÍTICA: Verificar que hay definiciones válidas antes de dibujar
            if (_columnDefinitions.Count == 0 || _rowDefinitions.Count == 0)
                return;

            // Calcular el área de contenido respetando el padding del Grid
            var contentArea = new Rect(
                Bounds.X + Padding.Left,
                Bounds.Y + Padding.Top,
                Math.Max(0, Bounds.Width - Padding.Left - Padding.Right),
                Math.Max(0, Bounds.Height - Padding.Top - Padding.Bottom)
            );

            // Dibujar fondo del Grid si es necesario
            if (BackgroundColor.A > 0)
            {
                Renderer.DrawRect(contentArea, BackgroundColor);
            }

            // Verificar que hay espacio válido para dibujar
            if (contentArea.Width <= 0 || contentArea.Height <= 0) return;

            // Dibujar debug visual de las celdas
            if (ShowCellColors && _columnWidths != null && _rowHeights != null)
            {
                DrawCellDebugColors(contentArea);
            }

            // Dibujar controles hijos
            foreach (var child in _children)
            {
                child.Draw();
            }

            // Dibujar líneas de la grilla
            if (ShowGridLines && _columnWidths != null && _rowHeights != null)
            {
                DrawGridLines(contentArea);
            }
        }

        /// <summary>
        /// Dibuja colores de debug para cada celda
        /// </summary>
        private void DrawCellDebugColors(Rect contentArea)
        {
            for (int row = 0; row < RowCount; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    var cellRect = new Rect(
                        contentArea.X + _columnPositions[column],
                        contentArea.Y + _rowPositions[row],
                        _columnWidths[column],
                        _rowHeights[row]
                    );

                    // Solo dibujar si el rectángulo tiene tamaño válido
                    if (cellRect.Width > 0 && cellRect.Height > 0)
                    {
                        var colorIndex = (row * ColumnCount + column) % DebugColors.Length;
                        var cellColor = DebugColors[colorIndex];
                        Renderer.DrawRect(cellRect, cellColor);
                    }
                }
            }
        }

        /// <summary>
        /// Dibuja las líneas de la grilla
        /// </summary>
        private void DrawGridLines(Rect contentArea)
        {
            // Líneas verticales (columnas)
            for (int i = 0; i <= ColumnCount; i++)
            {
                var x = contentArea.X + _columnPositions[i];
                if (x >= contentArea.X && x <= contentArea.X + contentArea.Width)
                {
                    Renderer.DrawLine(
                        new Vector2(x, contentArea.Y),
                        new Vector2(x, contentArea.Y + contentArea.Height),
                        1,
                        GridLineColor
                    );
                }
            }

            // Líneas horizontales (filas)
            for (int i = 0; i <= RowCount; i++)
            {
                var y = contentArea.Y + _rowPositions[i];
                if (y >= contentArea.Y && y <= contentArea.Y + contentArea.Height)
                {
                    Renderer.DrawLine(
                        new Vector2(contentArea.X, y),
                        new Vector2(contentArea.X + contentArea.Width, y),
                        1,
                        GridLineColor
                    );
                }
            }
        }

        /// <summary>
        /// Métodos de conveniencia para configurar el Grid
        /// </summary>
        public void SetColumns(params GridLength[] columns)
        {
            _columnDefinitions = columns?.ToList() ?? new List<GridLength>();
            InvalidateGridLayout();
        }

        public void SetRows(params GridLength[] rows)
        {
            _rowDefinitions = rows?.ToList() ?? new List<GridLength>();
            InvalidateGridLayout();
        }

        public void SetColumns(params string[] columns)
        {
            _columnDefinitions = columns?.Select(ParseGridLength).ToList() ?? new List<GridLength>();
            InvalidateGridLayout();
        }

        public void SetRows(params string[] rows)
        {
            _rowDefinitions = rows?.Select(ParseGridLength).ToList() ?? new List<GridLength>();
            InvalidateGridLayout();
        }

        private GridLength ParseGridLength(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                return GridLength.Auto;

            if (value == "*")
                return GridLength.Star;

            if (value.EndsWith("*"))
            {
                var starValue = value.Substring(0, value.Length - 1);
                if (float.TryParse(starValue, out float stars))
                    return GridLength.Stars(stars);
            }

            if (float.TryParse(value, out float pixels))
                return GridLength.Pixel(pixels);

            return GridLength.Auto;
        }

        /// <summary>
        /// Métodos para controlar el debug visual
        /// </summary>
        public void EnableDebugVisualization(bool showCellColors = true, bool showGridLines = true)
        {
            ShowCellColors = showCellColors;
            ShowGridLines = showGridLines;
        }

        public void DisableDebugVisualization()
        {
            ShowCellColors = false;
            ShowGridLines = false;
        }

        #region Métodos Estáticos Estilo WPF

        /// <summary>
        /// Establece la columna de un control en el Grid
        /// </summary>
        public static void SetColumn(UIControl control, int column)
        {
            if (control == null) return;
            control.SetGridPosition(column, control.GridRow, control.GridColumnSpan, control.GridRowSpan);
        }

        /// <summary>
        /// Obtiene la columna de un control en el Grid
        /// </summary>
        public static int GetColumn(UIControl control)
        {
            return control?.GridColumn ?? 0;
        }

        /// <summary>
        /// Establece la fila de un control en el Grid
        /// </summary>
        public static void SetRow(UIControl control, int row)
        {
            if (control == null) return;
            control.SetGridPosition(control.GridColumn, row, control.GridColumnSpan, control.GridRowSpan);
        }

        /// <summary>
        /// Obtiene la fila de un control en el Grid
        /// </summary>
        public static int GetRow(UIControl control)
        {
            return control?.GridRow ?? 0;
        }

        /// <summary>
        /// Establece el número de columnas que ocupa un control
        /// </summary>
        public static void SetColumnSpan(UIControl control, int columnSpan)
        {
            if (control == null) return;
            control.SetGridPosition(control.GridColumn, control.GridRow, Math.Max(1, columnSpan), control.GridRowSpan);
        }

        /// <summary>
        /// Obtiene el número de columnas que ocupa un control
        /// </summary>
        public static int GetColumnSpan(UIControl control)
        {
            return control?.GridColumnSpan ?? 1;
        }

        /// <summary>
        /// Establece el número de filas que ocupa un control
        /// </summary>
        public static void SetRowSpan(UIControl control, int rowSpan)
        {
            if (control == null) return;
            control.SetGridPosition(control.GridColumn, control.GridRow, control.GridColumnSpan, Math.Max(1, rowSpan));
        }

        /// <summary>
        /// Obtiene el número de filas que ocupa un control
        /// </summary>
        public static int GetRowSpan(UIControl control)
        {
            return control?.GridRowSpan ?? 1;
        }

        /// <summary>
        /// Establece la posición completa de un control en el Grid
        /// </summary>
        public static void SetPosition(UIControl control, int column, int row, int columnSpan = 1, int rowSpan = 1)
        {
            if (control == null) return;
            control.SetGridPosition(column, row, columnSpan, rowSpan);
        }

        /// <summary>
        /// Obtiene la posición completa de un control en el Grid
        /// </summary>
        public static (int column, int row, int columnSpan, int rowSpan) GetPosition(UIControl control)
        {
            if (control == null) return (0, 0, 1, 1);
            return (control.GridColumn, control.GridRow, control.GridColumnSpan, control.GridRowSpan);
        }

        #endregion
    }
}