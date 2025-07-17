using static AvalonInjectLib.FontRenderer;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib.UIFramework
{
    public class MenuItem : UIControl
    {
        // Constantes para el diseño
        private const float DEFAULT_HEIGHT = 25f;
        private const float INDENT_SIZE = 15f;
        private const float ARROW_SIZE = 8f;
        private const float TEXT_PADDING = 5f;
        private const float ARROW_PADDING = 5f;

        // Propiedades del elemento
        public string Text { get; set; } = string.Empty;
        public bool IsExpanded { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        public bool HasChildren => Children.Any();
        public int Level { get; set; } = 0;
        public bool ShowWarning { get; set; } = false;
        public string WarningText { get; set; } = string.Empty;

        // Colores
        public Color NormalColor { get; set; } = Color.FromArgb(45, 45, 45);
        public Color SelectedColor { get; set; } = Color.FromArgb(255, 165, 0); // Orange
        public Color HoverColor { get; set; } = Color.FromArgb(60, 60, 60);
        public Color ArrowColor { get; set; } = Color.White;
        public Color WarningColor { get; set; } = Color.FromArgb(255, 165, 0); // Orange

        // Fuente
        public Font Font { get; set; } = Font.GetDefaultFont();

        // Jerarquía
        public List<MenuItem> Children { get; private set; } = new List<MenuItem>();
        public MenuItem ParentItem { get; set; }

        // Estados
        private bool _isHovered = false;

        // Eventos
        public Action<MenuItem> OnItemClick;
        public Action<MenuItem> OnItemExpanded;
        public Action<MenuItem> OnItemCollapsed;

        public MenuItem()
        {
            Height = DEFAULT_HEIGHT;
            IsFocusable = true;
            BackColor = NormalColor;
        }

        public MenuItem(string text, int level = 0) : this()
        {
            Text = text;
            Level = level;
        }

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();
            var currentColor = GetCurrentBackgroundColor();

            // Dibujar fondo
            Renderer.DrawRect(new Rect(absPos.X, absPos.Y, Width, Height), currentColor);

            // Calcular posición del texto con indentación
            float textX = absPos.X + (Level * INDENT_SIZE) + TEXT_PADDING;

            // Centrar texto verticalmente usando métricas de la fuente
            float textY = CalculateVerticalCenterPosition(absPos.Y, Height, Font, Text);

            // Dibujar flecha si tiene hijos
            if (HasChildren)
            {
                float arrowX = textX + ARROW_PADDING;
                float arrowY = absPos.Y + (Height / 2); // La flecha se centra simple
                DrawArrow(arrowX, arrowY, IsExpanded);
                textX += ARROW_SIZE + ARROW_PADDING * 2;
            }

            // Dibujar texto
            Renderer.DrawText(Text, new Vector2(textX, textY), ForeColor, Font);

            // Dibujar indicador de advertencia si es necesario
            if (ShowWarning && !string.IsNullOrEmpty(WarningText))
            {
                float warningX = absPos.X + Width - 100f; // Posición fija desde la derecha
                float warningY = CalculateVerticalCenterPosition(absPos.Y, Height, Font, Text);
                Renderer.DrawText(WarningText, new Vector2(warningX, warningY), WarningColor, Font);
            }

            // IMPORTANTE: No dibujar hijos aquí - el MenuList se encarga de esto
        }

        /// <summary>
        /// Calcula la posición Y para centrar el texto verticalmente dentro del elemento
        /// </summary>
        private float CalculateVerticalCenterPosition(float elementY, float elementHeight, Font font, string text)
        {
            if (font == null || !font.IsReady || string.IsNullOrEmpty(text))
            {
                return elementY + (elementHeight / 2);
            }

            // Usar el mismo método que tu Label funcional
            var textSize = font.MeasureText(text);
            float textHeight = textSize.Y;

            // Centrar usando el tamaño real del texto
            return elementY + (elementHeight - textHeight) / 2;
        }

        private void DrawArrow(float x, float y, bool isExpanded)
        {
            // Dibujar flecha simple usando líneas
            Vector2 center = new Vector2(x, y);
            float halfSize = ARROW_SIZE / 2;

            if (isExpanded)
            {
                // Flecha hacia abajo (▼)
                Vector2 p1 = new Vector2(center.X - halfSize, center.Y - halfSize / 2);
                Vector2 p2 = new Vector2(center.X + halfSize, center.Y - halfSize / 2);
                Vector2 p3 = new Vector2(center.X, center.Y + halfSize / 2);

                Renderer.DrawLine(p1, p2, 1f, ArrowColor);
                Renderer.DrawLine(p2, p3, 1f, ArrowColor);
                Renderer.DrawLine(p3, p1, 1f, ArrowColor);
            }
            else
            {
                // Flecha hacia la derecha (▶)
                Vector2 p1 = new Vector2(center.X - halfSize / 2, center.Y - halfSize);
                Vector2 p2 = new Vector2(center.X - halfSize / 2, center.Y + halfSize);
                Vector2 p3 = new Vector2(center.X + halfSize / 2, center.Y);

                Renderer.DrawLine(p1, p2, 1f, ArrowColor);
                Renderer.DrawLine(p2, p3, 1f, ArrowColor);
                Renderer.DrawLine(p3, p1, 1f, ArrowColor);
            }
        }

        private Color GetCurrentBackgroundColor()
        {
            if (IsSelected)
                return SelectedColor;

            if (_isHovered)
                return HoverColor;

            return NormalColor;
        }

        public override void Update()
        {
            base.Update();
            // No actualizar hijos aquí - el MenuList se encarga de esto
        }

        protected override void OnMouseEnter(Vector2 mousePos)
        {
            base.OnMouseEnter(mousePos);
            _isHovered = true;
        }

        protected override void OnMouseLeave(Vector2 mousePos)
        {
            base.OnMouseLeave(mousePos);
            _isHovered = false;
        }

        protected override void OnClick(Vector2 mousePos)
        {
            base.OnClick(mousePos);

            // Si tiene hijos, alternar expansión
            if (HasChildren)
            {
                ToggleExpansion();
            }

            // Notificar click del elemento
            OnItemClick?.Invoke(this);
        }

        // Métodos públicos para manejo de jerarquía
        public void AddChild(MenuItem child)
        {
            if (child == null) return;

            child.ParentItem = this;
            child.Level = this.Level + 1;
            child.Parent = this.Parent; // Mismo contenedor padre
            Children.Add(child);

            // Configurar eventos si el parent ya está configurado
            if (this.Parent != null && this.Parent is MenuList menuList)
            {
                SetupChildEvents(child, menuList);
            }
        }

        private void SetupChildEvents(MenuItem child, MenuList menuList)
        {
            child.OnItemClick += menuList.OnItemSelected;
            child.OnItemExpanded += menuList.OnItemExpanded;
            child.OnItemCollapsed += menuList.OnItemCollapsed;

            // Configurar recursivamente para los hijos del hijo
            foreach (var grandChild in child.Children)
            {
                SetupChildEvents(grandChild, menuList);
            }
        }

        public void RemoveChild(MenuItem child)
        {
            if (child == null) return;

            Children.Remove(child);
            child.ParentItem = null;
        }

        public void ToggleExpansion()
        {
            IsExpanded = !IsExpanded;

            if (IsExpanded)
            {
                OnItemExpanded?.Invoke(this);
            }
            else
            {
                OnItemCollapsed?.Invoke(this);
            }
        }

        public void Expand()
        {
            if (!IsExpanded)
            {
                IsExpanded = true;
                OnItemExpanded?.Invoke(this);
            }
        }

        public void Collapse()
        {
            if (IsExpanded)
            {
                IsExpanded = false;
                OnItemCollapsed?.Invoke(this);
            }
        }

        // Método eliminado - UpdateChildrenLayout ya no es necesario
        // El MenuList se encarga del layout

        // Método eliminado - GetTotalChildrenHeight ya no es necesario
        // El MenuList se encarga del cálculo de altura

        public void SetSelected(bool selected)
        {
            IsSelected = selected;

            // Deseleccionar hermanos si este se selecciona
            if (selected && ParentItem != null)
            {
                foreach (var sibling in ParentItem.Children)
                {
                    if (sibling != this)
                    {
                        sibling.SetSelected(false);
                    }
                }
            }
        }

        public List<MenuItem> GetAllItems()
        {
            var items = new List<MenuItem> { this };

            foreach (var child in Children)
            {
                items.AddRange(child.GetAllItems());
            }

            return items;
        }

        public MenuItem FindItem(string text)
        {
            if (Text == text) return this;

            foreach (var child in Children)
            {
                var found = child.FindItem(text);
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>
        /// Ajusta automáticamente la altura del elemento basándose en la fuente
        /// </summary>
        public void AutoSizeHeight()
        {
            if (Font != null && Font.IsReady)
            {
                float fontHeight = Font.LineHeight;
                Height = Math.Max(fontHeight + (TEXT_PADDING * 2), DEFAULT_HEIGHT);
            }
        }

        public override string ToString()
        {
            return $"MenuItem: {Text} (Level: {Level}, Children: {Children.Count})";
        }
    }
}