using System.Diagnostics;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib.UIFramework
{
    // Clase TabPage mejorada con constructor adicional
    public class TabPage
    {
        public string Title { get; set; } = "";
        public UIContainer Content { get; internal set; }
        public bool Enabled { get; set; } = true;
        public bool Visible { get; set; } = true;
        public object? Tag { get; set; }

        public TabPage(string title)
        {
            Title = title;
            Content = new Panel();
        }

        // Constructor adicional para facilitar la creación con contenido
        public TabPage(string title, UIContainer content)
        {
            Title = title;
            Content = content ?? new Panel();
        }

        public void AddChild(UIControl control)
        {
            Content.AddChild(control);
        }
    }

    // Clase builder para facilitar la creación de tabs
    public class TabBuilder
    {
        private readonly TabControl _tabControl;
        private readonly TabPage _tabPage;

        internal TabBuilder(TabControl tabControl, TabPage tabPage)
        {
            _tabControl = tabControl;
            _tabPage = tabPage;
        }

        public TabBuilder AddControl<T>(T control) where T : UIControl
        {
            _tabPage.Content.AddChild(control);
            return this;
        }

        public TabBuilder AddControls(params UIControl[] controls)
        {
            foreach (var control in controls)
            {
                control.Parent = _tabControl;
                _tabPage.Content.AddChild(control);
            }
            return this;
        }

        public TabBuilder SetEnabled(bool enabled)
        {
            _tabPage.Enabled = enabled;
            return this;
        }

        public TabBuilder SetTag(object tag)
        {
            _tabPage.Tag = tag;
            return this;
        }

        public TabControl Build()
        {
            return _tabControl;
        }
    }

    public class TabControl : UIContainer
    {
        // Constantes para el diseño
        private const float TAB_HEIGHT = 30f;
        private const float TAB_PADDING = 10f;
        private const float TAB_SPACING = 2f;
        private const float BORDER_WIDTH = 1f;
        private const float MIN_TAB_WIDTH = 80f;
        private const float MAX_TAB_WIDTH = 200f;

        // Lista de tabs
        private readonly List<TabPage> _tabs = new List<TabPage>();
        private int _selectedIndex = -1;

        // Botones para los tabs
        private readonly List<Button> _tabButtons = new List<Button>();

        // Propiedades para auto-ajuste
        public bool AutoSize { get; set; } = true;
        public bool AutoFitTabs { get; set; } = true;
        public float MinTabWidth { get; set; } = MIN_TAB_WIDTH;
        public float MaxTabWidth { get; set; } = MAX_TAB_WIDTH;

        // NUEVA PROPIEDAD: Controla si el contenido se ajusta automáticamente
        public bool StretchContent { get; set; } = true;

        // Colores para el tema dark profesional
        public Color TabBackColor { get; set; } = Color.FromArgb(30, 30, 30);
        public Color TabForeColor { get; set; } = Color.FromArgb(220, 220, 220);
        public Color SelectedTabColor { get; set; } = Color.FromArgb(45, 45, 45);
        public Color SelectedTabBorderColor { get; set; } = Color.FromArgb(0, 122, 204);
        public Color TabBorderColor { get; set; } = Color.FromArgb(60, 60, 60);
        public Color ContentBackColor { get; set; } = Color.FromArgb(37, 37, 38);
        public Color HoverTabColor { get; set; } = Color.FromArgb(50, 50, 50);
        public Color DisabledTabColor { get; set; } = Color.FromArgb(30, 30, 30);
        public Color DisabledTextColor { get; set; } = Color.FromArgb(100, 100, 100);

        // Eventos
        public event Action<int>? TabSelected;
        public event Action<int>? TabClosing;
        public event Action? TabsChanged;

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

        // ============ MÉTODOS SIMPLIFICADOS PARA AGREGAR TABS ============

        /// <summary>
        /// Método más simple para agregar un tab con título
        /// </summary>
        public TabBuilder AddTab(string title)
        {
            var tab = new TabPage(title);
            AddTabInternal(tab);
            return new TabBuilder(this, tab);
        }

        /// <summary>
        /// Método simple para agregar un tab con título y contenido
        /// </summary>
        public TabControl AddTab(string title, UIContainer content)
        {
            var tab = new TabPage(title, content);
            AddTabInternal(tab);
            return this;
        }

        /// <summary>
        /// Método simple para agregar un tab con título y múltiples controles
        /// </summary>
        public TabControl AddTab(string title, params UIControl[] controls)
        {
            var tab = new TabPage(title);
            foreach (var control in controls)
            {
                tab.Content.AddChild(control);
            }
            AddTabInternal(tab);
            return this;
        }

        /// <summary>
        /// Método tradicional para agregar un TabPage completo
        /// </summary>
        public TabControl AddTab(TabPage tab)
        {
            if (tab == null) return this;
            AddTabInternal(tab);
            return this;
        }

        /// <summary>
        /// Método fluido para crear múltiples tabs fácilmente
        /// </summary>
        public TabControl CreateTabs(params (string title, UIContainer content)[] tabs)
        {
            foreach (var (title, content) in tabs)
            {
                AddTab(title, content);
            }
            return this;
        }

        /// <summary>
        /// Método fluido para crear múltiples tabs con builder
        /// </summary>
        public TabControl CreateTabs(params string[] titles)
        {
            foreach (var title in titles)
            {
                AddTab(title);
            }
            return this;
        }

        // ============ MÉTODOS INTERNOS ============

        private void AddTabInternal(TabPage tab)
        {
            _tabs.Add(tab);
            CreateTabButton(tab, _tabs.Count - 1);

            // Configurar el contenido del tab
            if (tab.Content != null)
            {
                tab.Content.Parent = this;
                UpdateTabContentLayout(tab.Content);
                tab.Content.Visible = (_tabs.Count == 1);

                // Asegurar que todos los hijos tengan el parent correcto
                foreach (var child in tab.Content.Children)
                {
                    child.Parent = tab.Content;
                }
            }

            // Seleccionar el primer tab automáticamente
            if (_tabs.Count == 1)
            {
                SetSelectedIndex(0);
            }

            // Auto-ajustar el tamaño si está habilitado
            if (AutoSize || AutoFitTabs)
            {
                AutoAdjustSize();
            }

            RecalculateTabLayout();
            TabsChanged?.Invoke();
        }

        /// <summary>
        /// Auto-ajusta el tamaño del TabControl basado en el contenido
        /// </summary>
        private void AutoAdjustSize()
        {
            if (!AutoSize && !AutoFitTabs) return;

            float totalTabWidth = 0;
            float maxContentWidth = 0;
            float maxContentHeight = 0;

            // Calcular el ancho total necesario para los tabs
            if (AutoFitTabs)
            {
                foreach (var tab in _tabs)
                {
                    // Estimar el ancho del texto + padding
                    float textWidth = EstimateTextWidth(tab.Title) + (TAB_PADDING * 2);
                    totalTabWidth += Math.Max(MinTabWidth, Math.Min(MaxTabWidth, textWidth));
                }
                totalTabWidth += (_tabs.Count - 1) * TAB_SPACING;
            }

            // Calcular el tamaño necesario basado en el contenido
            if (AutoSize)
            {
                foreach (var tab in _tabs)
                {
                    if (tab.Content != null)
                    {
                        var contentSize = CalculateContentSize(tab.Content);
                        maxContentWidth = Math.Max(maxContentWidth, contentSize.Width);
                        maxContentHeight = Math.Max(maxContentHeight, contentSize.Height);
                    }
                }
            }

            // Ajustar el ancho para acomodar los tabs
            if (AutoFitTabs)
            {
                Width = Math.Max(Width, totalTabWidth + (BORDER_WIDTH * 2));
            }

            // Ajustar el tamaño para acomodar el contenido
            if (AutoSize)
            {
                Width = Math.Max(Width, maxContentWidth + (BORDER_WIDTH * 2));
                Height = Math.Max(Height, maxContentHeight + TAB_HEIGHT + (BORDER_WIDTH * 2));
            }
        }

        /// <summary>
        /// Estima el ancho del texto (implementación simple)
        /// </summary>
        private float EstimateTextWidth(string text)
        {
            // Implementación simple - en un caso real usarías las métricas del font
            return text.Length * 8f; // Aproximadamente 8 pixels por caracter
        }

        /// <summary>
        /// Calcula el tamaño necesario para el contenido
        /// </summary>
        private (float Width, float Height) CalculateContentSize(UIContainer container)
        {
            float maxX = 0, maxY = 0;

            foreach (var child in container.Children)
            {
                maxX = Math.Max(maxX, child.X + child.Width);
                maxY = Math.Max(maxY, child.Y + child.Height);
            }

            return (maxX, maxY);
        }

        // ============ MÉTODOS DE REMOCIÓN MEJORADOS ============

        public TabControl RemoveTab(int index)
        {
            if (index < 0 || index >= _tabs.Count) return this;

            var tab = _tabs[index];
            var button = _tabButtons[index];

            TabClosing?.Invoke(index);

            if (tab.Content != null)
            {
                RemoveChild(tab.Content);
            }

            RemoveChild(button);
            _tabs.RemoveAt(index);
            _tabButtons.RemoveAt(index);

            // Ajustar índice seleccionado
            if (_selectedIndex == index)
            {
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

            // Auto-ajustar tamaño después de remover
            if (AutoSize || AutoFitTabs)
            {
                AutoAdjustSize();
            }

            RecalculateTabLayout();
            TabsChanged?.Invoke();
            return this;
        }

        public TabControl RemoveTab(TabPage tab)
        {
            int index = _tabs.IndexOf(tab);
            if (index >= 0)
            {
                RemoveTab(index);
            }
            return this;
        }

        public TabControl RemoveTab(string title)
        {
            int index = _tabs.FindIndex(t => t.Title == title);
            if (index >= 0)
            {
                RemoveTab(index);
            }
            return this;
        }

        public TabControl ClearTabs()
        {
            while (_tabs.Count > 0)
            {
                RemoveTab(0);
            }
            return this;
        }

        // ============ MÉTODOS DE LAYOUT MEJORADOS ============

        /// <summary>
        /// MÉTODO MEJORADO: Actualiza el layout del contenido del tab con soporte para stretch
        /// </summary>
        private void UpdateTabContentLayout(UIContainer content)
        {
            if (content == null) return;

            // Calcular el área disponible para el contenido
            var contentArea = GetContentArea();

            // Aplicar la posición y tamaño del contenedor
            content.X = contentArea.X;
            content.Y = contentArea.Y;

            if (StretchContent)
            {
                // Hacer que el contenido se ajuste completamente al área disponible
                content.Width = contentArea.Width;
                content.Height = contentArea.Height;
            }
            else
            {
                // Mantener el tamaño original del contenido (comportamiento anterior)
                content.Width = Math.Min(content.Width, contentArea.Width);
                content.Height = Math.Min(content.Height, contentArea.Height);
            }

            // NUEVO: Aplicar stretch a los controles hijos si están configurados para ello
            ApplyStretchToChildren(content, contentArea);
        }

        /// <summary>
        /// NUEVO MÉTODO: Aplica stretch a los controles hijos del contenedor
        /// </summary>
        private void ApplyStretchToChildren(UIContainer container, Rect contentArea)
        {
            if (!StretchContent) return;

            foreach (var child in container.Children)
            {
                // Aplicar stretch horizontal para controles que ocupan todo el ancho
                if (child is Panel panel)
                {
                    // Los paneles se ajustan al ancho disponible
                    if (panel.Width >= container.Width * 0.9f) // Si el panel ocupa casi todo el ancho
                    {
                        panel.Width = container.Width - 20; // Dejar un margen de 10px a cada lado
                    }
                }
                else if (child is Button button)
                {
                    // Los botones que ocupan todo el ancho se ajustan
                    if (button.Width >= container.Width * 0.9f)
                    {
                        button.Width = container.Width - 20;
                    }
                }
                else if (child is TextBox textBox)
                {
                    // Los textboxes que ocupan todo el ancho se ajustan
                    if (textBox.Width >= container.Width * 0.9f)
                    {
                        textBox.Width = container.Width - 20;
                    }
                }
                else if (child is Slider slider)
                {
                    // Los sliders que ocupan todo el ancho se ajustan
                    if (slider.Width >= container.Width * 0.9f)
                    {
                        slider.Width = container.Width - 20;
                    }
                }

                // Aplicar recursivamente a contenedores hijos
                if (child is UIContainer childContainer)
                {
                    ApplyStretchToChildren(childContainer, contentArea);
                }
            }
        }

        private void CreateTabButton(TabPage tab, int index)
        {
            var button = new Button
            {
                Text = tab.Title,
                Height = TAB_HEIGHT,
                BackColor = (index == _selectedIndex) ? SelectedTabColor : TabBackColor,
                ForeColor = tab.Enabled ? TabForeColor : DisabledTextColor,
                ShowBorder = false,
                Parent = this,
                Tag = index,
                Enabled = tab.Enabled
            };

            // Efecto hover
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
            if (index < -1 || index >= _tabs.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index),
                    $"Index {index} is out of range. Valid range is -1 to {_tabs.Count - 1}");
            }

            if (_tabs.Count == 0 || index == -1)
            {
                if (_selectedIndex != -1)
                {
                    DeselectCurrentTab();
                    _selectedIndex = -1;
                }
                return;
            }

            if (index == _selectedIndex)
            {
                return;
            }

            try
            {
                if (_selectedIndex >= 0 && _selectedIndex < _tabs.Count)
                {
                    DeselectCurrentTab();
                }

                _selectedIndex = index;
                SelectNewTab();

                TabSelected?.Invoke(index);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error changing tab selection: {ex.Message}");
                _selectedIndex = -1;
            }
        }

        private void DeselectCurrentTab()
        {
            var oldTab = _tabs[_selectedIndex];
            var oldButton = _tabButtons[_selectedIndex];

            if (oldTab.Content != null)
            {
                oldTab.Content.Visible = false;
                SetChildrenVisibility(oldTab.Content, false);
            }

            oldButton.BackColor = oldTab.Enabled ? TabBackColor : DisabledTabColor;
            oldButton.ForeColor = oldTab.Enabled ? TabForeColor : DisabledTextColor;
        }

        private void SelectNewTab()
        {
            var newTab = _tabs[_selectedIndex];
            var newButton = _tabButtons[_selectedIndex];

            if (newTab.Content != null)
            {
                newTab.Content.Visible = true;
                SetChildrenVisibility(newTab.Content, true);

                // IMPORTANTE: Actualizar el layout cuando se selecciona un tab
                UpdateTabContentLayout(newTab.Content);
            }

            newButton.BackColor = SelectedTabColor;
            newButton.ForeColor = TabForeColor;
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
            if (_tabButtons.Count == 0) return;

            float currentX = 0;
            float totalAvailableWidth = Width;

            if (AutoFitTabs)
            {
                // Calcular anchos individuales basados en el contenido
                var tabWidths = new float[_tabButtons.Count];
                float totalDesiredWidth = 0;

                for (int i = 0; i < _tabButtons.Count; i++)
                {
                    float textWidth = EstimateTextWidth(_tabs[i].Title) + (TAB_PADDING * 2);
                    tabWidths[i] = Math.Max(MinTabWidth, Math.Min(MaxTabWidth, textWidth));
                    totalDesiredWidth += tabWidths[i];
                }

                totalDesiredWidth += (_tabButtons.Count - 1) * TAB_SPACING;

                // Si no caben, escalar proporcionalmente
                if (totalDesiredWidth > totalAvailableWidth)
                {
                    float scale = (totalAvailableWidth - (_tabButtons.Count - 1) * TAB_SPACING) / (totalDesiredWidth - (_tabButtons.Count - 1) * TAB_SPACING);
                    for (int i = 0; i < tabWidths.Length; i++)
                    {
                        tabWidths[i] *= scale;
                    }
                }

                // Aplicar los anchos calculados
                for (int i = 0; i < _tabButtons.Count; i++)
                {
                    var button = _tabButtons[i];
                    button.X = currentX;
                    button.Y = 0;
                    button.Width = tabWidths[i];
                    currentX += tabWidths[i] + TAB_SPACING;
                }
            }
            else
            {
                // Distribución uniforme (comportamiento original)
                float tabWidth = (totalAvailableWidth - (_tabButtons.Count - 1) * TAB_SPACING) / _tabButtons.Count;

                for (int i = 0; i < _tabButtons.Count; i++)
                {
                    var button = _tabButtons[i];
                    button.X = currentX;
                    button.Y = 0;
                    button.Width = tabWidth;
                    currentX += tabWidth + TAB_SPACING;
                }
            }
        }

        // ============ MÉTODOS DE DIBUJO Y ACTUALIZACIÓN ============

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();

            // Dibujar fondo principal
            Renderer.DrawRect(new Rect(absPos.X, absPos.Y, Width, Height), ContentBackColor);

            // Dibujar barra de tabs
            Renderer.DrawRect(new Rect(absPos.X, absPos.Y, Width, TAB_HEIGHT), TabBackColor);

            // Línea separadora
            Renderer.DrawLine(
                new Vector2(absPos.X, absPos.Y + TAB_HEIGHT),
                new Vector2(absPos.X + Width, absPos.Y + TAB_HEIGHT),
                BORDER_WIDTH,
                Color.FromArgb(60, 60, 60));

            // Dibujar botones de tabs
            foreach (var button in _tabButtons)
            {
                button.Draw();

                // Borde inferior para tab seleccionado
                if ((int)button.Tag == _selectedIndex)
                {
                    Renderer.DrawLine(
                        new Vector2(button.GetAbsolutePosition().X, absPos.Y + TAB_HEIGHT - 2),
                        new Vector2(button.GetAbsolutePosition().X + button.Width, absPos.Y + TAB_HEIGHT - 2),
                        3,
                        SelectedTabBorderColor);
                }
            }

            // Bordes del control
            Renderer.DrawRectOutline(new Rect(absPos.X, absPos.Y, Width, Height), TabBorderColor, BORDER_WIDTH);

            // Contenido del tab seleccionado
            if (SelectedTab?.Content != null && SelectedTab.Content.Visible)
            {
                DrawTabPageContent(SelectedTab.Content);
            }
        }

        private void DrawTabPageContent(UIContainer container)
        {
            container.Draw();

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

            foreach (var button in _tabButtons)
            {
                button.Update();
            }

            if (SelectedTab?.Content != null && SelectedTab.Content.Visible)
            {
                UpdateTabPageContent(SelectedTab.Content);
            }
        }

        private void UpdateTabPageContent(UIContainer container)
        {
            container.Update();

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
            RecalculateTabLayout();

            // IMPORTANTE: Actualizar el layout de todos los tabs cuando cambie el tamaño
            foreach (var tab in _tabs)
            {
                if (tab.Content != null)
                {
                    UpdateTabContentLayout(tab.Content);
                }
            }
        }

        // ============ MÉTODOS DE UTILIDAD ============

        public Rect GetContentArea()
        {
            return new Rect(
                BORDER_WIDTH,
                TAB_HEIGHT + BORDER_WIDTH,
                Width - (2 * BORDER_WIDTH),
                Height - TAB_HEIGHT - (2 * BORDER_WIDTH)
            );
        }

        public TabControl SetTabTitle(int index, string title)
        {
            if (index >= 0 && index < _tabs.Count)
            {
                _tabs[index].Title = title;
                _tabButtons[index].Text = title;

                if (AutoFitTabs)
                {
                    RecalculateTabLayout();
                }
            }
            return this;
        }

        public TabControl SetTabEnabled(int index, bool enabled)
        {
            if (index >= 0 && index < _tabs.Count)
            {
                _tabs[index].Enabled = enabled;
                _tabButtons[index].Enabled = enabled;

                if (index == _selectedIndex && !enabled)
                {
                    for (int i = 0; i < _tabs.Count; i++)
                    {
                        if (_tabs[i].Enabled)
                        {
                            SetSelectedIndex(i);
                            break;
                        }
                    }
                }

                if (index != _selectedIndex)
                {
                    _tabButtons[index].BackColor = enabled ? TabBackColor : DisabledTabColor;
                    _tabButtons[index].ForeColor = enabled ? TabForeColor : DisabledTextColor;
                }
            }
            return this;
        }

        // Método para encontrar tab por título
        public int FindTabIndex(string title)
        {
            return _tabs.FindIndex(t => t.Title == title);
        }

        // Método para seleccionar tab por título
        public TabControl SelectTab(string title)
        {
            int index = FindTabIndex(title);
            if (index >= 0)
            {
                SetSelectedIndex(index);
            }
            return this;
        }

        // ============ NUEVOS MÉTODOS PARA CONTROLAR EL STRETCH ============

        /// <summary>
        /// Habilita o deshabilita el stretch del contenido
        /// </summary>
        public TabControl SetStretchContent(bool stretch)
        {
            StretchContent = stretch;

            // Actualizar todos los tabs existentes
            foreach (var tab in _tabs)
            {
                if (tab.Content != null)
                {
                    UpdateTabContentLayout(tab.Content);
                }
            }

            return this;
        }

        /// <summary>
        /// Fuerza una actualización del layout de todos los tabs
        /// </summary>
        public TabControl RefreshLayout()
        {
            RecalculateTabLayout();

            foreach (var tab in _tabs)
            {
                if (tab.Content != null)
                {
                    UpdateTabContentLayout(tab.Content);
                }
            }

            return this;
        }
    }
}