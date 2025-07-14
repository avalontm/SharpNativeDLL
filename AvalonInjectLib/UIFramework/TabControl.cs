namespace AvalonInjectLib.UIFramework
{
    using static AvalonInjectLib.Structs;

    public class TabControl : UIControl
    {
        private List<TabPage> _tabPages = new List<TabPage>();
        private int _selectedIndex = 0;
        private float _tabHeight = 30f;
        private Color _tabColor = new Color(50, 50, 50);
        private Color _selectedTabColor = new Color(70, 70, 70);
        private Color _tabTextColor = Color.White;
        private Color _borderColor = new Color(80, 80, 80);

        public IReadOnlyList<TabPage> TabPages => _tabPages.AsReadOnly();
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (value >= 0 && value < _tabPages.Count && value != _selectedIndex)
                {
                    _selectedIndex = value;
                    OnTabChanged?.Invoke(_selectedIndex);
                }
            }
        }
        public TabPage SelectedTab => _selectedIndex >= 0 && _selectedIndex < _tabPages.Count ? _tabPages[_selectedIndex] : null;
        public float TabHeight
        {
            get => _tabHeight;
            set => _tabHeight = Math.Max(20, value);
        }
        public Action<int> OnTabChanged;

        public TabControl()
        {
            Padding = new Thickness(5); // Padding por defecto
        }

        public override void Draw()
        {
            if (!IsVisible || _tabPages.Count == 0) return;

            // Calcular área de contenido considerando padding
            var contentRect = GetContentRect();

            // Dibujar fondo del control
            Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BackgroundColor);

            // Calcular ancho de cada pestaña (considerando márgenes)
            float tabWidth = (contentRect.Width - (Margin.Left + Margin.Right)) / _tabPages.Count;

            // Dibujar pestañas
            for (int i = 0; i < _tabPages.Count; i++)
            {
                float tabX = contentRect.X + i * tabWidth;
                bool isSelected = i == _selectedIndex;

                // Color de fondo de la pestaña
                Color bgColor = isSelected ? _selectedTabColor : _tabColor;

                // Dibujar pestaña (con margen)
                Renderer.DrawRect(tabX, contentRect.Y, tabWidth, _tabHeight, bgColor);
                Renderer.DrawRectOutline(tabX, contentRect.Y, tabWidth, _tabHeight, _borderColor, 1f);

                // Texto de la pestaña (centrado)
                if (!string.IsNullOrEmpty(_tabPages[i].Title))
                {
                    var textSize = Renderer.MeasureText(_tabPages[i].Title, 12);
                    float textX = tabX + (tabWidth - textSize.X) / 2;
                    float textY = contentRect.Y + (_tabHeight - textSize.Y) / 2;
                    Renderer.DrawText(_tabPages[i].Title, textX, textY, _tabTextColor, 12);
                }
            }

            // Dibujar borde del área de contenido
            float contentY = contentRect.Y + _tabHeight;
            float contentHeight = contentRect.Height - _tabHeight;
            Renderer.DrawRectOutline(contentRect.X, contentY, contentRect.Width, contentHeight, _borderColor, 1f);

            // Dibujar contenido de la pestaña seleccionada
            if (SelectedTab != null)
            {
                foreach (var control in SelectedTab.Controls)
                {
                    if (!control.IsVisible) continue;

                    // Aplicar transformación considerando padding y posición del TabControl
                    var originalBounds = control.Bounds;
                    control.Bounds = new Rect(
                        contentRect.X + originalBounds.X + control.Margin.Left,
                        contentY + originalBounds.Y + control.Margin.Top,
                        originalBounds.Width,
                        originalBounds.Height
                    );

                    control.Draw();
                    control.Bounds = originalBounds;
                }
            }
        }

        public override void Update()
        {
            if (!IsVisible || _tabPages.Count == 0) return;

            base.Update();

            var contentRect = GetContentRect();

            // Manejar clic en pestañas
            if (UIEventSystem.IsMousePressed)
            {
                var mousePos = UIEventSystem.MousePosition;
                if (mousePos.Y >= contentRect.Y && mousePos.Y <= contentRect.Y + _tabHeight)
                {
                    float tabWidth = contentRect.Width / _tabPages.Count;
                    int clickedTab = (int)((mousePos.X - contentRect.X) / tabWidth);

                    if (clickedTab >= 0 && clickedTab < _tabPages.Count)
                    {
                        SelectedIndex = clickedTab;
                    }
                }
            }

            // Actualizar controles de la pestaña seleccionada
            if (SelectedTab != null)
            {
                float contentY = contentRect.Y + _tabHeight;

                foreach (var control in SelectedTab.Controls)
                {
                    if (!control.IsVisible) continue;

                    var originalBounds = control.Bounds;
                    control.Bounds = new Rect(
                        contentRect.X + originalBounds.X + control.Margin.Left,
                        contentY + originalBounds.Y + control.Margin.Top,
                        originalBounds.Width,
                        originalBounds.Height
                    );

                    control.Update();
                    control.Bounds = originalBounds;
                }
            }
        }

        public void AddTab(TabPage tabPage)
        {
            if (tabPage != null && !_tabPages.Contains(tabPage))
            {
                _tabPages.Add(tabPage);
                if (_tabPages.Count == 1) // Primera pestaña añadida
                {
                    SelectedIndex = 0;
                }
            }
        }

        public void RemoveTab(TabPage tabPage)
        {
            if (tabPage != null && _tabPages.Contains(tabPage))
            {
                int index = _tabPages.IndexOf(tabPage);
                _tabPages.Remove(tabPage);

                // Ajustar el índice seleccionado si es necesario
                if (_selectedIndex >= _tabPages.Count)
                {
                    SelectedIndex = _tabPages.Count - 1;
                }
                else if (index <= _selectedIndex && _selectedIndex > 0)
                {
                    SelectedIndex--;
                }
            }
        }

        public void RemoveTabAt(int index)
        {
            if (index >= 0 && index < _tabPages.Count)
            {
                RemoveTab(_tabPages[index]);
            }
        }

        public void ClearTabs()
        {
            _tabPages.Clear();
            _selectedIndex = -1;
        }
    }

    public class TabPage
    {
        public string Title { get; set; }
        public List<UIControl> Controls { get; } = new List<UIControl>();
        public Color BackgroundColor { get; set; } = new Color(40, 40, 40);

        public TabPage(string title = "")
        {
            Title = title;
        }

        public void AddControl(UIControl control)
        {
            if (control != null && !Controls.Contains(control))
            {
                Controls.Add(control);
            }
        }

        public void RemoveControl(UIControl control)
        {
            if (control != null)
            {
                Controls.Remove(control);
            }
        }

        public void ClearControls()
        {
            Controls.Clear();
        }
    }
}