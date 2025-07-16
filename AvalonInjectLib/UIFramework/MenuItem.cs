using static AvalonInjectLib.Structs;
using System;
using System.Collections.Generic;
using System.Linq;

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
            float textY = absPos.Y + (Height / 2);

            // Dibujar flecha si tiene hijos
            if (HasChildren)
            {
                float arrowX = textX + ARROW_PADDING;
                DrawArrow(arrowX, textY, IsExpanded);
                textX += ARROW_SIZE + ARROW_PADDING * 2;
            }

            // Dibujar texto
            Renderer.DrawText(Text, new Vector2(textX, textY), ForeColor, Font);

            // Dibujar indicador de advertencia si es necesario
            if (ShowWarning && !string.IsNullOrEmpty(WarningText))
            {
                float warningX = absPos.X + Width - 100f; // Posición fija desde la derecha
                Renderer.DrawText(WarningText, new Vector2(warningX, textY), WarningColor, Font);
            }

            // Dibujar hijos si están expandidos
            if (IsExpanded)
            {
                foreach (var child in Children)
                {
                    child.Draw();
                }
            }
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

            // Actualizar hijos si están expandidos
            if (IsExpanded)
            {
                foreach (var child in Children)
                {
                    child.Update();
                }
            }
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

            UpdateChildrenLayout();
        }

        public void RemoveChild(MenuItem child)
        {
            if (child == null) return;

            Children.Remove(child);
            child.ParentItem = null;
            UpdateChildrenLayout();
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

            UpdateChildrenLayout();
        }

        public void Expand()
        {
            if (!IsExpanded)
            {
                IsExpanded = true;
                OnItemExpanded?.Invoke(this);
                UpdateChildrenLayout();
            }
        }

        public void Collapse()
        {
            if (IsExpanded)
            {
                IsExpanded = false;
                OnItemCollapsed?.Invoke(this);
                UpdateChildrenLayout();
            }
        }

        private void UpdateChildrenLayout()
        {
            if (!IsExpanded || !HasChildren) return;

            float currentY = Y + Height;

            foreach (var child in Children)
            {
                child.X = X;
                child.Y = currentY;
                child.Width = Width;
                child.Visible = true;

                currentY += child.Height;

                // Si el hijo también está expandido, actualizar su layout
                if (child.IsExpanded)
                {
                    child.UpdateChildrenLayout();
                    currentY += child.GetTotalChildrenHeight();
                }
            }
        }

        private float GetTotalChildrenHeight()
        {
            if (!IsExpanded || !HasChildren) return 0;

            float totalHeight = 0;
            foreach (var child in Children)
            {
                totalHeight += child.Height;
                totalHeight += child.GetTotalChildrenHeight();
            }
            return totalHeight;
        }

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

        public override string ToString()
        {
            return $"MenuItem: {Text} (Level: {Level}, Children: {Children.Count})";
        }
    }
}