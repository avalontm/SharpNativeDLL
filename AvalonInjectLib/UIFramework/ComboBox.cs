using static AvalonInjectLib.Structs;

namespace AvalonInjectLib.UIFramework
{
    public class ComboItem
    {
        public string Text { get; set; }
        public object? Tag { get; set; }

        public ComboItem(string text, object? tag = null)
        {
            Text = text;
            Tag = tag;
        }

        public override string ToString()
        {
            return Text;
        }
    }

    public class ComboBox : UIControl
    {
        // Constantes para el diseño
        private const float ARROW_SIZE = 16f;
        private const float ITEM_HEIGHT = 30f;
        private const int MAX_VISIBLE_ITEMS = 5;
        private const float SCROLL_BAR_WIDTH = 10f;

        // Estados
        private bool _isExpanded;
        private int _selectedIndex = -1;
        private readonly List<ComboItem> _items = new();
        private float _dragOffset = 0f;

        // Propiedades para scroll
        private int _scrollOffset = 0;
        private bool _isDraggingScrollBar = false;
        private float _scrollBarThumbTop = 0f;
        private float _scrollBarThumbHeight = 0f;

        // Propiedades
        public IReadOnlyList<ComboItem> Items => _items.AsReadOnly();
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value >= -1 && value < _items.Count && _selectedIndex != value)
                {
                    _selectedIndex = value;

                    // Auto-scroll para mostrar el item seleccionado
                    if (value >= 0)
                    {
                        EnsureItemVisible(value);
                    }

                    SelectionChanged?.Invoke(_selectedIndex);
                }
            }
        }

        public ComboItem? SelectedItem => _selectedIndex >= 0 ? _items[_selectedIndex] : null;
        public string SelectedText => _selectedIndex >= 0 ? _items[_selectedIndex].Text : string.Empty;
        public object? SelectedValue => _selectedIndex >= 0 ? _items[_selectedIndex].Tag : null;

        public Color DropDownColor { get; set; } = Color.FromArgb(45, 45, 48);
        public Color HighlightColor { get; set; } = Color.FromArgb(0, 122, 204);
        public Color ArrowColor { get; set; } = Color.White;
        public Color BorderColor { get; set; } = Color.FromArgb(100, 100, 100);
        public Color ScrollBarColor { get; set; } = Color.FromArgb(60, 60, 60);
        public Color ScrollBarThumbColor { get; set; } = Color.FromArgb(120, 120, 120);
        public Font Font { get; set; } = Font.GetDefaultFont();

        // Eventos
        public Action<int>? SelectionChanged;

        public ComboBox()
        {
            Width = 150f;
            Height = 32f;
            BackColor = Color.FromArgb(37, 37, 38);
            ForeColor = Color.White;
            IsFocusable = true;
        }

        // Métodos para agregar items
        public void AddItem(string text, object? tag = null)
        {
            _items.Add(new ComboItem(text, tag));
            if (_selectedIndex == -1 && _items.Count > 0)
                _selectedIndex = 0;
        }

        public void AddItem(ComboItem item)
        {
            _items.Add(item);
            if (_selectedIndex == -1 && _items.Count > 0)
                _selectedIndex = 0;
        }

        public void AddRange(IEnumerable<ComboItem> items)
        {
            foreach (var item in items)
                AddItem(item);
        }

        public void AddRange(IEnumerable<string> items)
        {
            foreach (var item in items)
                AddItem(item);
        }

        public void ClearItems()
        {
            _items.Clear();
            _selectedIndex = -1;
            _scrollOffset = 0;
        }

        private void EnsureItemVisible(int itemIndex)
        {
            if (itemIndex < _scrollOffset)
            {
                _scrollOffset = itemIndex;
            }
            else if (itemIndex >= _scrollOffset + MAX_VISIBLE_ITEMS)
            {
                _scrollOffset = itemIndex - MAX_VISIBLE_ITEMS + 1;
            }

            // Asegurar que el offset no sea negativo o excesivo
            _scrollOffset = Math.Max(0, Math.Min(_scrollOffset, Math.Max(0, _items.Count - MAX_VISIBLE_ITEMS)));
        }

        protected override void OnMouseLeave(object sender, Vector2 pos)
        {
            base.OnMouseLeave(sender, pos);
        }

        protected override void OnClick(object sender, Vector2 pos)
        {
            base.OnClick(sender, pos);
            var mousePos = UIEventSystem.MousePosition;
            if (_isExpanded)
            {
                var absPos = GetAbsolutePosition();

                // Verificar si hizo clic en la barra de scroll
                if (IsMouseOverScrollBar(mousePos))
                {
                    HandleScrollBarClick(mousePos);
                }
                else
                {
                    // Si está expandido, verificar si hizo clic en un item
                    HandleItemSelection(mousePos);
                }
            }
            else
            {
                // Si está contraído, expandir
                _isExpanded = true;
            }
        }

        protected override void OnMouseDown(object sender, Vector2 pos)
        {
            base.OnMouseDown(sender, pos);
            var mousePos = UIEventSystem.MousePosition;
            if (_isExpanded)
            {
                if (IsMouseOverScrollBar(mousePos))
                {
                    HandleScrollBarClick(mousePos);
                }
            }
        }

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();

            // Dibujar el cuadro principal
            Renderer.DrawRect(
                new Rect(absPos.X, absPos.Y, Width, Height),
                BackColor
            );

            // Dibujar borde
            Renderer.DrawRectOutline(
                new Rect(absPos.X, absPos.Y, Width, Height),
                BorderColor,
                1f
            );

            // Dibujar texto seleccionado
            if (_selectedIndex >= 0)
            {
                Renderer.DrawText(
                    _items[_selectedIndex].Text,
                    new Vector2(absPos.X + 5, absPos.Y + (Height - Font.LineHeight) / 2),
                    ForeColor,
                    Font
                );
            }

            // Dibujar flecha desplegable
            DrawArrow(absPos);

            // Dibujar lista desplegable si está expandido
            if (_isExpanded)
            {
                DrawDropDownList(absPos);
            }
        }

        private void DrawArrow(Vector2 absPos)
        {
            // Triángulo apuntando hacia abajo o hacia arriba dependiendo del estado
            var arrowCenter = new Vector2(
                absPos.X + Width - ARROW_SIZE / 2 - 5,
                absPos.Y + Height / 2
            );

            if (_isExpanded)
            {
                // Flecha hacia arriba cuando está expandido
                Renderer.DrawTriangle(
                    new Vector2(arrowCenter.X - ARROW_SIZE / 3, arrowCenter.Y + ARROW_SIZE / 4),
                    new Vector2(arrowCenter.X + ARROW_SIZE / 3, arrowCenter.Y + ARROW_SIZE / 4),
                    new Vector2(arrowCenter.X, arrowCenter.Y - ARROW_SIZE / 4),
                    ArrowColor
                );
            }
            else
            {
                // Flecha hacia abajo cuando está contraído
                Renderer.DrawTriangle(
                    new Vector2(arrowCenter.X - ARROW_SIZE / 3, arrowCenter.Y - ARROW_SIZE / 4),
                    new Vector2(arrowCenter.X + ARROW_SIZE / 3, arrowCenter.Y - ARROW_SIZE / 4),
                    new Vector2(arrowCenter.X, arrowCenter.Y + ARROW_SIZE / 4),
                    ArrowColor
                );
            }
        }

        private void DrawDropDownList(Vector2 absPos)
        {
            float dropDownHeight = Math.Min(_items.Count, MAX_VISIBLE_ITEMS) * ITEM_HEIGHT;
            bool needsScrollBar = _items.Count > MAX_VISIBLE_ITEMS;
            float availableWidth = needsScrollBar ? Width - SCROLL_BAR_WIDTH : Width;

            // Dibujar fondo de la lista
            Renderer.DrawRect(
                new Rect(absPos.X, absPos.Y + Height, Width, dropDownHeight),
                DropDownColor
            );

            // Dibujar borde de la lista
            Renderer.DrawRectOutline(
                new Rect(absPos.X, absPos.Y + Height, Width, dropDownHeight),
                BorderColor,
                1f
            );

            // Dibujar items visibles
            int itemsToShow = Math.Min(_items.Count, MAX_VISIBLE_ITEMS);
            for (int i = 0; i < itemsToShow; i++)
            {
                int itemIndex = _scrollOffset + i;
                if (itemIndex >= _items.Count) break;

                var itemPos = new Vector2(
                    absPos.X + 5,
                    absPos.Y + Height + i * ITEM_HEIGHT + (ITEM_HEIGHT - Font.LineHeight) / 2
                );

                // Resaltar item si el mouse está sobre él
                bool isHighlighted = IsMouseOverItem(absPos, i);
                Color textColor = isHighlighted ? Color.White : ForeColor;

                if (isHighlighted)
                {
                    Renderer.DrawRect(
                        new Rect(absPos.X, absPos.Y + Height + i * ITEM_HEIGHT, availableWidth, ITEM_HEIGHT),
                        HighlightColor
                    );
                }

                Renderer.DrawText(
                    _items[itemIndex].Text,
                    itemPos,
                    textColor,
                    Font
                );
            }

            // Dibujar barra de desplazamiento si es necesaria
            if (needsScrollBar)
            {
                DrawScrollBar(absPos, dropDownHeight);
            }
        }

        private void DrawScrollBar(Vector2 absPos, float dropDownHeight)
        {
            float scrollBarX = absPos.X + Width - SCROLL_BAR_WIDTH - 1;
            float scrollBarY = absPos.Y + Height;

            // Fondo de la barra
            Renderer.DrawRect(
                new Rect(scrollBarX, scrollBarY, SCROLL_BAR_WIDTH, dropDownHeight),
                ScrollBarColor
            );

            // Calcular posición y tamaño del thumb
            float totalItems = _items.Count;
            float visibleItems = MAX_VISIBLE_ITEMS;

            _scrollBarThumbHeight = Math.Max(10f, (visibleItems / totalItems) * dropDownHeight);
            float scrollableHeight = dropDownHeight - _scrollBarThumbHeight;
            float scrollProgress = totalItems > visibleItems ? (float)_scrollOffset / (totalItems - visibleItems) : 0f;

            _scrollBarThumbTop = scrollBarY + scrollProgress * scrollableHeight;

            // Dibujar thumb
            Renderer.DrawRect(
                new Rect(scrollBarX + 1, _scrollBarThumbTop, SCROLL_BAR_WIDTH - 2, _scrollBarThumbHeight),
                ScrollBarThumbColor
            );
        }

        public override void Update()
        {
            base.Update();

            if (!Enabled || !Visible) return;

            Vector2 mousePos = UIEventSystem.MousePosition;

            // Manejar scroll con rueda del mouse
            if (_isExpanded && IsMouseOverDropDown(mousePos))
            {
                float scrollDelta = UIEventSystem.GetMouseWheelDelta();

                if (scrollDelta != 0)
                {
                    Scroll((int)Math.Sign(scrollDelta) * -1); // Invertir dirección para que sea natural
                }
            }

            // Manejar arrastre de la barra de scroll
            if (_isDraggingScrollBar)
            {
                HandleScrollBarDrag(mousePos);
            }

            // Verificar si se hizo clic fuera del control para contraer la lista
            if (_isExpanded && UIEventSystem.IsMouseClicked())
            {
                if (!IsMouseOverControl(mousePos) && !IsMouseOverDropDown(mousePos))
                {
                    _isExpanded = false;
                    _isDraggingScrollBar = false;
                }
            }

            // Detener arrastre si se suelta el mouse
            if (!UIEventSystem.IsMouseDown)
            {
                _isDraggingScrollBar = false;
            }
        }

        private void Scroll(int direction)
        {
            int newOffset = _scrollOffset + direction;
            int maxOffset = Math.Max(0, _items.Count - MAX_VISIBLE_ITEMS);
            _scrollOffset = Math.Max(0, Math.Min(newOffset, maxOffset));
        }

        private void HandleScrollBarClick(Vector2 mousePos)
        {
            var absPos = GetAbsolutePosition();
            float relativeY = mousePos.Y - absPos.Y - Height;

            // Verificar si hizo clic en el thumb
            // Corregir el cálculo: _scrollBarThumbTop ya es una coordenada absoluta
            float thumbRelativeTop = _scrollBarThumbTop - (absPos.Y + Height);
            float thumbRelativeBottom = thumbRelativeTop + _scrollBarThumbHeight;

            if (relativeY >= thumbRelativeTop && relativeY <= thumbRelativeBottom)
            {
                _isDraggingScrollBar = true;
                // Guardar el offset del clic dentro del thumb para un arrastre más suave
                _dragOffset = relativeY - thumbRelativeTop;
            }
            else
            {
                // Clic en el track - hacer scroll hacia esa posición
                float dropDownHeight = Math.Min(_items.Count, MAX_VISIBLE_ITEMS) * ITEM_HEIGHT;
                float scrollProgress = relativeY / dropDownHeight;
                int maxOffset = Math.Max(0, _items.Count - MAX_VISIBLE_ITEMS);
                _scrollOffset = Math.Max(0, Math.Min((int)(scrollProgress * maxOffset), maxOffset));
            }
        }

        private void HandleScrollBarDrag(Vector2 mousePos)
        {
            var absPos = GetAbsolutePosition();
            float dropDownHeight = Math.Min(_items.Count, MAX_VISIBLE_ITEMS) * ITEM_HEIGHT;
            float relativeY = mousePos.Y - absPos.Y - Height;

            // Ajustar la posición usando el offset del arrastre
            float adjustedY = relativeY - _dragOffset;

            float scrollableHeight = dropDownHeight - _scrollBarThumbHeight;
            float scrollProgress = Math.Max(0, Math.Min(1, adjustedY / scrollableHeight));

            int maxOffset = Math.Max(0, _items.Count - MAX_VISIBLE_ITEMS);
            _scrollOffset = Math.Max(0, Math.Min((int)(scrollProgress * maxOffset), maxOffset));
        }

        private void HandleItemSelection(Vector2 mousePos)
        {
            var absPos = GetAbsolutePosition();

            if (IsMouseOverDropDown(mousePos))
            {
                int relativeItemIndex = (int)((mousePos.Y - absPos.Y - Height) / ITEM_HEIGHT);
                int actualItemIndex = _scrollOffset + relativeItemIndex;

                if (relativeItemIndex >= 0 && relativeItemIndex < MAX_VISIBLE_ITEMS && actualItemIndex < _items.Count)
                {
                    SelectedIndex = actualItemIndex;
                }
                _isExpanded = false; // Contraer después de seleccionar
            }
        }

        private bool IsMouseOverControl(Vector2 mousePos)
        {
            var absPos = GetAbsolutePosition();
            return new Rect(absPos.X, absPos.Y, Width, Height).Contains(mousePos);
        }

        private bool IsMouseOverDropDown(Vector2 mousePos)
        {
            var absPos = GetAbsolutePosition();
            float dropDownHeight = Math.Min(_items.Count, MAX_VISIBLE_ITEMS) * ITEM_HEIGHT;

            return new Rect(
                absPos.X,
                absPos.Y + Height,
                Width,
                dropDownHeight).Contains(mousePos);
        }

        private bool IsMouseOverScrollBar(Vector2 mousePos)
        {
            if (_items.Count <= MAX_VISIBLE_ITEMS) return false;

            var absPos = GetAbsolutePosition();
            float dropDownHeight = Math.Min(_items.Count, MAX_VISIBLE_ITEMS) * ITEM_HEIGHT;
            float scrollBarX = absPos.X + Width - SCROLL_BAR_WIDTH - 1;

            return new Rect(
                scrollBarX,
                absPos.Y + Height,
                SCROLL_BAR_WIDTH,
                dropDownHeight).Contains(mousePos);
        }

        private bool IsMouseOverItem(Vector2 absPos, int relativeItemIndex)
        {
            Vector2 mousePos = UIEventSystem.MousePosition;
            bool needsScrollBar = _items.Count > MAX_VISIBLE_ITEMS;
            float availableWidth = needsScrollBar ? Width - SCROLL_BAR_WIDTH : Width;

            return new Rect(
                absPos.X,
                absPos.Y + Height + relativeItemIndex * ITEM_HEIGHT,
                availableWidth,
                ITEM_HEIGHT).Contains(mousePos);
        }

        public override bool Contains(Vector2 point)
        {
            var absPos = GetAbsolutePosition();
            var mainRect = new Rect(absPos.X, absPos.Y, Width, Height);

            if (_isExpanded)
            {
                // Si está expandido, incluir también el área de la lista desplegable
                float dropDownHeight = Math.Min(_items.Count, MAX_VISIBLE_ITEMS) * ITEM_HEIGHT;
                var dropDownRect = new Rect(absPos.X, absPos.Y + Height, Width, dropDownHeight);
                return mainRect.Contains(point) || dropDownRect.Contains(point);
            }

            return mainRect.Contains(point);
        }

        // Métodos de conveniencia para buscar items
        public int FindItemIndex(string text)
        {
            return _items.FindIndex(item => item.Text == text);
        }

        public ComboItem? FindItem(string text)
        {
            return _items.FirstOrDefault(item => item.Text == text);
        }

        public void SelectItem(string text)
        {
            int index = FindItemIndex(text);
            if (index >= 0)
                SelectedIndex = index;
        }

        // Métodos públicos para control programático del scroll
        public void ScrollToTop()
        {
            _scrollOffset = 0;
        }

        public void ScrollToBottom()
        {
            _scrollOffset = Math.Max(0, _items.Count - MAX_VISIBLE_ITEMS);
        }

        public void ScrollToItem(int itemIndex)
        {
            if (itemIndex >= 0 && itemIndex < _items.Count)
            {
                EnsureItemVisible(itemIndex);
            }
        }
    }
}