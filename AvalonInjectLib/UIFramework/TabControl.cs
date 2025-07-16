using System.Diagnostics;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib.UIFramework
{
    public class TabPage
    {
        public string Title { get; set; } = "";
        public UIContainer Content { get; set; }
        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
        public object? Tag { get; set; }

        public TabPage(string title, UIContainer content)
        {
            Title = title;
            Content = content;
        }
    }

    public class TabControl : UIContainer
    {
        // Constantes para el diseño
        private const float TAB_HEIGHT = 30f;
        private const float TAB_PADDING = 10f;
        private const float TAB_SPACING = 2f;
        private const float BORDER_WIDTH = 1f;

        // Lista de tabs
        private readonly List<TabPage> _tabs = new List<TabPage>();
        private int _selectedIndex = -1;

        // Botones para los tabs
        private readonly List<Button> _tabButtons = new List<Button>();

        // Colores para el tema dark profesional
        public Color TabBackColor { get; set; } = Color.FromArgb(30, 30, 30);          // Fondo de la barra de tabs
        public Color TabForeColor { get; set; } = Color.FromArgb(220, 220, 220);       // Texto de los tabs
        public Color SelectedTabColor { get; set; } = Color.FromArgb(45, 45, 45);      // Tab seleccionado
        public Color SelectedTabBorderColor { get; set; } = Color.FromArgb(0, 122, 204); // Borde inferior del tab seleccionado
        public Color TabBorderColor { get; set; } = Color.FromArgb(60, 60, 60);        // Bordes generales
        public Color ContentBackColor { get; set; } = Color.FromArgb(37, 37, 38);      // Fondo del área de contenido
        public Color HoverTabColor { get; set; } = Color.FromArgb(50, 50, 50);         // Color al pasar el mouse
        public Color DisabledTabColor { get; set; } = Color.FromArgb(30, 30, 30);      // Tabs deshabilitados
        public Color DisabledTextColor { get; set; } = Color.FromArgb(100, 100, 100);  // Texto deshabilitado

        // Eventos
        public event Action<int>? TabSelected;
        public event Action<int>? TabClosing;

        // Propiedades
        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetSelectedIndex(value);
        }

        public TabPage? SelectedTab
        {
            get => _selectedIndex >= 0 && _selectedIndex < _tabs.Count ? _tabs[_selectedIndex] : null;
        }

        public IReadOnlyList<TabPage> Tabs => _tabs.AsReadOnly();

        // Constructor
        public TabControl()
        {
            BackColor = ContentBackColor;
            Width = 300f;
            Height = 200f;
        }

        // Métodos públicos para manejo de tabs
        public void AddTab(TabPage tab)
        {
            if (tab == null) return;

            _tabs.Add(tab);
            CreateTabButton(tab, _tabs.Count - 1);

            // Configurar el contenido del tab con stretch completo
            if (tab.Content != null)
            {
                tab.Content.Parent = this;
                UpdateTabContentLayout(tab.Content);
                tab.Content.Visible = (_tabs.Count == 1); // Mostrar solo si es el primer tab

                // Asegurarse de que todos los hijos del TabPage tengan el parent correcto
                foreach (var child in tab.Content.Children)
                {
                    child.Parent = tab.Content;
                }
            }

            if (_tabs.Count == 1)
            {
                SetSelectedIndex(0);
            }

            RecalculateTabLayout();
        }

        public void AddTab(string title, UIContainer content)
        {
            AddTab(new TabPage(title, content));
        }

        public void RemoveTab(int index)
        {
            if (index < 0 || index >= _tabs.Count) return;

            var tab = _tabs[index];
            var button = _tabButtons[index];

            // Disparar evento de cierre
            TabClosing?.Invoke(index);

            // Remover el contenido del contenedor
            if (tab.Content != null)
            {
                RemoveChild(tab.Content);
            }

            // Remover el botón
            RemoveChild(button);

            // Remover de las listas
            _tabs.RemoveAt(index);
            _tabButtons.RemoveAt(index);

            // Ajustar índice seleccionado
            if (_selectedIndex == index)
            {
                // Si se eliminó el tab seleccionado, seleccionar otro
                if (_tabs.Count > 0)
                {
                    int newIndex = Math.Min(index, _tabs.Count - 1);
                    SetSelectedIndex(newIndex);
                }
                else
                {
                    _selectedIndex = -1;
                }
            }
            else if (_selectedIndex > index)
            {
                _selectedIndex--;
            }

            RecalculateTabLayout();
        }

        public void RemoveTab(TabPage tab)
        {
            int index = _tabs.IndexOf(tab);
            if (index >= 0)
            {
                RemoveTab(index);
            }
        }

        public void ClearTabs()
        {
            while (_tabs.Count > 0)
            {
                RemoveTab(0);
            }
        }

        // Método para actualizar el layout del contenido del tab (stretch)
        private void UpdateTabContentLayout(UIContainer content)
        {
            content.X = BORDER_WIDTH;
            content.Y = TAB_HEIGHT + BORDER_WIDTH;
            content.Width = Width - (2 * BORDER_WIDTH);
            content.Height = Height - TAB_HEIGHT - (2 * BORDER_WIDTH);
        }

        // Métodos privados
        private void CreateTabButton(TabPage tab, int index)
        {
            var button = new Button
            {
                Text = tab.Title,
                Height = TAB_HEIGHT,
                BackColor = (index == _selectedIndex) ? SelectedTabColor : TabBackColor,
                TextColor = tab.Enabled ? TabForeColor : DisabledTextColor,
                ShowBorder = false,
                Parent = this,
                Tag = index,
                Enabled = tab.Enabled
            };

            // CORREGIDO: Efecto hover mejorado con colores del tema
            if (tab.Enabled)
            {
                button.MouseEnter += (pos) => {
                    if (index != _selectedIndex)
                        button.BackColor = HoverTabColor;
                };

                button.MouseLeave += (pos) => {
                    if (index != _selectedIndex)
                        button.BackColor = TabBackColor;
                };
            }

            button.Click += (mousePos) => OnTabButtonClick(index);

            _tabButtons.Add(button);
            AddChild(button);
        }

        private void OnTabButtonClick(int index)
        {
            SetSelectedIndex(index);
        }

        private void SetSelectedIndex(int index)
        {
            // Validación robusta del índice
            if (index < -1 || index >= _tabs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Index {index} is out of range. Valid range is -1 to {_tabs.Count - 1}");
            }

            // Si no hay tabs o índice es -1, deseleccionar todo
            if (_tabs.Count == 0 || index == -1)
            {
                if (_selectedIndex != -1)
                {
                    DeselectCurrentTab();
                    _selectedIndex = -1;
                }
                return;
            }

            // Si es el mismo índice, no hacer nada
            if (index == _selectedIndex)
            {
                return;
            }

            try
            {
                // Deseleccionar el tab actual si existe
                if (_selectedIndex >= 0 && _selectedIndex < _tabs.Count)
                {
                    DeselectCurrentTab();
                }

                // Seleccionar el nuevo tab
                _selectedIndex = index;
                SelectNewTab();

                // Disparar evento
                TabSelected?.Invoke(index);
            }
            catch (Exception ex)
            {
                // Manejar cualquier error inesperado
                Debug.WriteLine($"Error changing tab selection: {ex.Message}");
                _selectedIndex = -1; // Reset a estado seguro
            }
        }

        private void DeselectCurrentTab()
        {
            var oldTab = _tabs[_selectedIndex];
            var oldButton = _tabButtons[_selectedIndex];

            // Ocultar contenido
            if (oldTab.Content != null)
            {
                oldTab.Content.Visible = false;
                SetChildrenVisibility(oldTab.Content, false);
            }

            // CORREGIDO: Restaurar apariencia del botón con colores del tema
            oldButton.BackColor = oldTab.Enabled ? TabBackColor : DisabledTabColor;
            oldButton.ForeColor = oldTab.Enabled ? TabForeColor : DisabledTextColor; // CORREGIDO: Usar ForeColor
        }

        private void SelectNewTab()
        {
            var newTab = _tabs[_selectedIndex];
            var newButton = _tabButtons[_selectedIndex];

            // Mostrar contenido
            if (newTab.Content != null)
            {
                newTab.Content.Visible = true;
                SetChildrenVisibility(newTab.Content, true);
            }

            // CORREGIDO: Resaltar botón seleccionado con colores del tema
            newButton.BackColor = SelectedTabColor;
            newButton.ForeColor = TabForeColor; // CORREGIDO: Usar ForeColor
        }

        private void SetChildrenVisibility(UIContainer container, bool visible)
        {
            foreach (var child in container.Children)
            {
                child.Visible = visible;
                if (child is UIContainer childContainer)
                {
                    SetChildrenVisibility(childContainer, visible);
                }
            }
        }

        private void RecalculateTabLayout()
        {
            float currentX = 0;
            float tabWidth = CalculateTabWidth();

            for (int i = 0; i < _tabButtons.Count; i++)
            {
                var button = _tabButtons[i];
                button.X = currentX;
                button.Y = 0;
                button.Width = tabWidth;

                currentX += tabWidth + TAB_SPACING;
            }
        }

        private float CalculateTabWidth()
        {
            if (_tabButtons.Count == 0) return 0;

            float availableWidth = Width - ((_tabButtons.Count - 1) * TAB_SPACING);
            return availableWidth / _tabButtons.Count;
        }

        // Override de métodos base
        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();

            // 1. Dibujar el fondo principal del TabControl
            Renderer.DrawRect(new Rect(absPos.X, absPos.Y, Width, Height), ContentBackColor);

            // 2. Dibujar la barra de pestañas con un degradado oscuro
            Renderer.DrawRect(new Rect(absPos.X, absPos.Y, Width, TAB_HEIGHT), TabBackColor);

            // 3. Dibujar línea separadora inferior de la barra de tabs
            Renderer.DrawLine(
                new Vector2(absPos.X, absPos.Y + TAB_HEIGHT),
                new Vector2(absPos.X + Width, absPos.Y + TAB_HEIGHT),
                BORDER_WIDTH,
                Color.FromArgb(60, 60, 60));

            // 4. Dibujar los botones de las pestañas
            foreach (var button in _tabButtons)
            {
                button.Draw();

                // Dibujar borde inferior para el tab seleccionado
                if ((int)button.Tag == _selectedIndex)
                {
                    Renderer.DrawLine(
                        new Vector2(button.GetAbsolutePosition().X, absPos.Y + TAB_HEIGHT - 2),
                        new Vector2(button.GetAbsolutePosition().X + button.Width, absPos.Y + TAB_HEIGHT - 2),
                        3,
                        SelectedTabBorderColor);
                }
            }

            // 5. Dibujar bordes laterales y superior del control
            Renderer.DrawRectOutline(new Rect(absPos.X, absPos.Y, Width, Height), TabBorderColor, BORDER_WIDTH);

            // 6. Dibujar el contenido del TabPage seleccionado y sus hijos
            if (SelectedTab?.Content != null && SelectedTab.Content.Visible)
            {
                var contentRect = new Rect(
                    absPos.X + BORDER_WIDTH,
                    absPos.Y + TAB_HEIGHT + BORDER_WIDTH,
                    Width - (2 * BORDER_WIDTH),
                    Height - TAB_HEIGHT - (2 * BORDER_WIDTH)
                );

                // Dibujar el TabPage y todos sus hijos recursivamente
                DrawTabPageContent(SelectedTab.Content);
            }
        }

        private void DrawTabPageContent(UIContainer container)
        {
            // Dibujar este contenedor primero
            container.Draw();

            // Luego dibujar todos sus hijos recursivamente
            foreach (var child in container.Children)
            {
                if (child is UIContainer childContainer)
                {
                    DrawTabPageContent(childContainer);
                }
                else
                {
                    child.Draw();
                }
            }
        }

        public override void Update()
        {
            if (!Visible || !Enabled) return;

            // Actualizar los botones de las pestañas
            foreach (var button in _tabButtons)
            {
                button.Update();
            }

            // Actualizar el contenido del TabPage seleccionado y sus hijos
            if (SelectedTab?.Content != null && SelectedTab.Content.Visible)
            {
                UpdateTabPageContent(SelectedTab.Content);
            }
        }

        private void UpdateTabPageContent(UIContainer container)
        {
            // Actualizar este contenedor primero
            container.Update();

            // Luego actualizar todos sus hijos recursivamente
            foreach (var child in container.Children)
            {
                if (child is UIContainer childContainer)
                {
                    UpdateTabPageContent(childContainer);
                }
                else
                {
                    child.Update();
                }
            }
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            // Recalcular layout cuando cambia el tamaño
            RecalculateTabLayout();

            // CORREGIDO: Ajustar tamaño del contenido de todos los tabs para mantener stretch
            foreach (var tab in _tabs)
            {
                if (tab.Content != null)
                {
                    UpdateTabContentLayout(tab.Content);
                }
            }
        }

        // Método para obtener el área de contenido disponible
        public Rect GetContentArea()
        {
            return new Rect(
                BORDER_WIDTH,
                TAB_HEIGHT + BORDER_WIDTH,
                Width - (2 * BORDER_WIDTH),
                Height - TAB_HEIGHT - (2 * BORDER_WIDTH)
            );
        }

        // Método para cambiar el título de un tab
        public void SetTabTitle(int index, string title)
        {
            if (index >= 0 && index < _tabs.Count)
            {
                _tabs[index].Title = title;
                _tabButtons[index].Text = title;
            }
        }

        // CORREGIDO: Método para habilitar/deshabilitar un tab con colores del tema
        public void SetTabEnabled(int index, bool enabled)
        {
            if (index >= 0 && index < _tabs.Count)
            {
                _tabs[index].Enabled = enabled;
                _tabButtons[index].Enabled = enabled;

                if (index == _selectedIndex && !enabled)
                {
                    // Si deshabilitamos el tab seleccionado, buscar el siguiente habilitado
                    for (int i = 0; i < _tabs.Count; i++)
                    {
                        if (_tabs[i].Enabled)
                        {
                            SetSelectedIndex(i);
                            break;
                        }
                    }
                }

                // CORREGIDO: Actualizar colores visuales usando ForeColor y colores del tema
                if (index != _selectedIndex)
                {
                    _tabButtons[index].BackColor = enabled ? TabBackColor : DisabledTabColor;
                    _tabButtons[index].ForeColor = enabled ? TabForeColor : DisabledTextColor;
                }
            }
        }
    }
}