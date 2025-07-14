namespace AvalonInjectLib.UIFramework
{
    using static AvalonInjectLib.Structs;

    public class ComboBox : UIControl
    {
        private List<string> _items = new List<string>();
        private int _selectedIndex = -1;
        private bool _isDropdownOpen = false;
        private float _dropdownItemHeight = 25f;
        private int _hoveredIndex = -1;

        // Propiedades de apariencia
        public Color DropdownColor { get; set; } = new Color(60, 60, 60);
        public Color SelectedItemColor { get; set; } = new Color(70, 130, 180);
        public Color HoveredItemColor { get; set; } = new Color(80, 80, 80);
        public Color TextColor { get; set; } = Color.White;
        public Color ArrowColor { get; set; } = Color.White;
        public float DropdownMaxHeight { get; set; } = 150f;
        public TextStyle TextStyle { get; set; } = TextStyle.Default;

        // Eventos
        public Action<int, string> OnSelectionChanged;

        public List<string> Items
        {
            get => _items;
            set
            {
                _items = value ?? new List<string>();
                _selectedIndex = _items.Count > 0 ? 0 : -1;
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value >= -1 && value < _items.Count && value != _selectedIndex)
                {
                    _selectedIndex = value;
                    OnSelectionChanged?.Invoke(_selectedIndex, SelectedItem);
                }
            }
        }

        public string SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : "";

        public override void Draw()
        {
            if (!IsVisible) return;

            // Dibujar caja principal
            Color bgColor = IsHovered ? new Color(70, 70, 70) : new Color(60, 60, 60);
            Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, bgColor);
            Renderer.DrawRectOutline(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, new Color(80, 80, 80), 1f);

            // Dibujar texto seleccionado
            if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
            {
                Renderer.DrawText(
                    _items[_selectedIndex],
                    Bounds.X + 5,
                    Bounds.Y + (Bounds.Height - TextStyle.Size) / 2,
                    TextColor,
                    TextStyle.Size
                );
            }

            // Dibujar flecha
            float arrowSize = 8f;
            float arrowX = Bounds.X + Bounds.Width - arrowSize - 5;
            float arrowY = Bounds.Y + (Bounds.Height - arrowSize) / 2;

            if (_isDropdownOpen)
            {
                // Flecha hacia arriba
                Renderer.DrawTriangle(
                    new Vector2(arrowX, arrowY + arrowSize),
                    new Vector2(arrowX + arrowSize, arrowY + arrowSize),
                    new Vector2(arrowX + arrowSize / 2, arrowY),
                    ArrowColor
                );
            }
            else
            {
                // Flecha hacia abajo
                Renderer.DrawTriangle(
                    new Vector2(arrowX, arrowY),
                    new Vector2(arrowX + arrowSize, arrowY),
                    new Vector2(arrowX + arrowSize / 2, arrowY + arrowSize),
                    ArrowColor
                );
            }

            // Dibujar dropdown si está abierto
            if (_isDropdownOpen && _items.Count > 0)
            {
                float dropdownY = Bounds.Y + Bounds.Height;
                float dropdownHeight = Math.Min(_items.Count * _dropdownItemHeight, DropdownMaxHeight);

                // Fondo del dropdown
                Renderer.DrawRect(Bounds.X, dropdownY, Bounds.Width, dropdownHeight, DropdownColor);
                Renderer.DrawRectOutline(Bounds.X, dropdownY, Bounds.Width, dropdownHeight, new Color(80, 80, 80), 1f);

                // Calcular desplazamiento si hay muchos items
                int startIndex = 0;
                int endIndex = _items.Count;
                float visibleItems = DropdownMaxHeight / _dropdownItemHeight;

                if (_items.Count > visibleItems)
                {
                    // Implementar scroll aquí si es necesario
                }

                // Dibujar items
                for (int i = startIndex; i < endIndex; i++)
                {
                    float itemY = dropdownY + (i - startIndex) * _dropdownItemHeight;

                    // Resaltar item seleccionado o hovered
                    if (i == _selectedIndex)
                    {
                        Renderer.DrawRect(Bounds.X, itemY, Bounds.Width, _dropdownItemHeight, SelectedItemColor);
                    }
                    else if (i == _hoveredIndex)
                    {
                        Renderer.DrawRect(Bounds.X, itemY, Bounds.Width, _dropdownItemHeight, HoveredItemColor);
                    }

                    // Texto del item
                    Renderer.DrawText(
                        _items[i],
                        Bounds.X + 5,
                        itemY + (_dropdownItemHeight - TextStyle.Size) / 2,
                        TextColor,
                        TextStyle.Size
                    );
                }
            }
        }

        public override void Update()
        {
            if (!IsVisible) return;

            base.Update();

            var mousePos = UIEventSystem.MousePosition;
            var mousePressed = UIEventSystem.IsMousePressed;

            // Manejar clic en el combobox principal
            if (mousePressed && Bounds.Contains(mousePos.X, mousePos.Y))
            {
                _isDropdownOpen = !_isDropdownOpen;
                if (_isDropdownOpen)
                {
                    // Actualizar índice hovered al abrir
                    UpdateHoveredIndex(mousePos.Y);
                }
            }

            // Manejar dropdown abierto
            if (_isDropdownOpen)
            {
                // Actualizar hovered item
                UpdateHoveredIndex(mousePos.Y);

                // Manejar selección de item
                if (mousePressed)
                {
                    float dropdownY = Bounds.Y + Bounds.Height;
                    float dropdownHeight = Math.Min(_items.Count * _dropdownItemHeight, DropdownMaxHeight);

                    // Verificar si se hizo clic fuera del dropdown
                    if (!new Rect(Bounds.X, dropdownY, Bounds.Width, dropdownHeight).Contains(mousePos.X, mousePos.Y) &&
                        !Bounds.Contains(mousePos.X, mousePos.Y))
                    {
                        _isDropdownOpen = false;
                    }
                    // Verificar si se hizo clic en un item
                    else if (_hoveredIndex >= 0)
                    {
                        SelectedIndex = _hoveredIndex;
                        _isDropdownOpen = false;
                    }
                }
            }
        }

        private void UpdateHoveredIndex(float mouseY)
        {
            if (!_isDropdownOpen)
            {
                _hoveredIndex = -1;
                return;
            }

            float dropdownY = Bounds.Y + Bounds.Height;
            _hoveredIndex = (int)((mouseY - dropdownY) / _dropdownItemHeight);

            if (_hoveredIndex < 0 || _hoveredIndex >= _items.Count)
            {
                _hoveredIndex = -1;
            }
        }

        public override void Measure(Vector2 availableSize)
        {
            if (!IsVisible)
            {
                Bounds = new Rect(Bounds.X, Bounds.Y, 0, 0);
                return;
            }

            // Calcular tamaño basado en el texto más largo
            float maxWidth = 100f; // Ancho mínimo
            float height = 30f; // Altura fija

            foreach (var item in _items)
            {
                var textSize = Renderer.MeasureText(item, TextStyle.Size);
                maxWidth = Math.Max(maxWidth, textSize.X + 30); // +30 para la flecha y padding
            }

            // Mantener tamaño manual si fue especificado
            if (!float.IsNaN(Bounds.Width)) maxWidth = Bounds.Width;
            if (!float.IsNaN(Bounds.Height)) height = Bounds.Height;

            Bounds = new Rect(Bounds.X, Bounds.Y, maxWidth, height);
        }

        public void AddItem(string item)
        {
            if (!string.IsNullOrEmpty(item))
            {
                _items.Add(item);
                if (_selectedIndex == -1 && _items.Count > 0)
                {
                    _selectedIndex = 0;
                }
            }
        }

        public void RemoveItem(string item)
        {
            int index = _items.IndexOf(item);
            if (index >= 0)
            {
                _items.RemoveAt(index);
                if (_selectedIndex >= _items.Count)
                {
                    _selectedIndex = _items.Count - 1;
                }
            }
        }

        public void ClearItems()
        {
            _items.Clear();
            _selectedIndex = -1;
        }
    }
}