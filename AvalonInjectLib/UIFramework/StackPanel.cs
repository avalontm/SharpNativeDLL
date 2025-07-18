using static AvalonInjectLib.Structs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AvalonInjectLib.UIFramework
{
    public class StackPanel : UIControl
    {
        // Propiedades de diseño
        public Color BackgroundColor { get; set; } = Color.FromArgb(30, 30, 30);
        public float ItemSpacing { get; set; } = 2f;
        public float PaddingTop { get; set; } = 4f;
        public float PaddingBottom { get; set; } = 4f;
        public float PaddingLeft { get; set; } = 16f;
        public float PaddingRight { get; set; } = 4f;

        // Colección de controles hijos
        private List<UIControl> _children = new List<UIControl>();

        // Propiedades
        public bool HasChildren => _children.Count > 0;
        public int ChildrenCount => _children.Count;

        public StackPanel()
        {
            BackColor = BackgroundColor;
            Height = 0f;
        }

        public override void Draw()
        {
            if (!Visible || !HasChildren) return;

            var absPos = GetAbsolutePosition();

            // Dibujar fondo
            Renderer.DrawRect(absPos.X, absPos.Y, Width, Height, BackgroundColor);

            // Dibujar todos los controles hijos
            foreach (var child in _children)
            {
                if (child.Visible)
                {
                    child.Draw();
                }
            }
        }

        public override void Update()
        {
            base.Update();

            if (!Visible || !HasChildren) return;

            // Actualizar todos los controles hijos
            foreach (var child in _children)
            {
                if (child.Visible)
                {
                    child.Update();
                }
            }
        }

        // Métodos para manejo de hijos
        public void AddChild(UIControl child)
        {
            if (child == null) return;

            child.Parent = this;
            _children.Add(child);
            UpdateLayout();
        }

        public void RemoveChild(UIControl child)
        {
            if (child == null) return;

            _children.Remove(child);
            child.Parent = null;
            UpdateLayout();
        }

        public void ClearChildren()
        {
            foreach (var child in _children)
            {
                child.Parent = null;
            }
            _children.Clear();
            UpdateLayout();
        }

        public void InsertChild(int index, UIControl child)
        {
            if (child == null || index < 0 || index > _children.Count) return;

            child.Parent = this;
            _children.Insert(index, child);
            UpdateLayout();
        }

        public List<UIControl> GetChildren()
        {
            return new List<UIControl>(_children);
        }

        public T FindChild<T>(string name) where T : UIControl
        {
            return _children.OfType<T>().FirstOrDefault(c => c.Name == name);
        }

        public UIControl FindChild(string name)
        {
            return _children.FirstOrDefault(c => c.Name == name);
        }

        // Actualizar layout de los controles hijos
        private void UpdateLayout()
        {
            if (!HasChildren)
            {
                Height = 0f;
                return;
            }

            float currentY = PaddingTop;
            float contentWidth = Width - PaddingLeft - PaddingRight;

            foreach (var child in _children)
            {
                if (child.Visible)
                {
                    // Posicionar el control hijo
                    child.X = PaddingLeft;
                    child.Y = currentY;

                    // Ajustar ancho al contenedor (respetando padding)
                    child.Width = contentWidth;

                    // Avanzar a la siguiente posición
                    currentY += child.Height + ItemSpacing;
                }
            }

            // Calcular altura total del stack panel
            if (currentY > PaddingTop)
            {
                Height = currentY - ItemSpacing + PaddingBottom;
            }
            else
            {
                Height = PaddingTop + PaddingBottom;
            }
        }

        // Override de propiedades que afectan el layout
        public new float Width
        {
            get => base.Width;
            set
            {
                base.Width = value;
                UpdateLayout();
            }
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            UpdateLayout();
        }

        // Métodos de utilidad
        public void SetChildrenVisible(bool visible)
        {
            foreach (var child in _children)
            {
                child.Visible = visible;
            }
            UpdateLayout();
        }

        public void SetChildrenEnabled(bool enabled)
        {
            foreach (var child in _children)
            {
                child.Enabled = enabled;
            }
        }

        // Métodos para ordenar hijos
        public void MoveChildUp(UIControl child)
        {
            int index = _children.IndexOf(child);
            if (index > 0)
            {
                _children.RemoveAt(index);
                _children.Insert(index - 1, child);
                UpdateLayout();
            }
        }

        public void MoveChildDown(UIControl child)
        {
            int index = _children.IndexOf(child);
            if (index >= 0 && index < _children.Count - 1)
            {
                _children.RemoveAt(index);
                _children.Insert(index + 1, child);
                UpdateLayout();
            }
        }

        public void MoveChildToTop(UIControl child)
        {
            if (_children.Remove(child))
            {
                _children.Insert(0, child);
                UpdateLayout();
            }
        }

        public void MoveChildToBottom(UIControl child)
        {
            if (_children.Remove(child))
            {
                _children.Add(child);
                UpdateLayout();
            }
        }

        // Métodos para obtener información del layout
        public float GetTotalContentHeight()
        {
            if (!HasChildren) return 0f;

            float totalHeight = PaddingTop + PaddingBottom;
            int visibleCount = 0;

            foreach (var child in _children)
            {
                if (child.Visible)
                {
                    totalHeight += child.Height;
                    visibleCount++;
                }
            }

            // Agregar espaciado entre elementos
            if (visibleCount > 1)
            {
                totalHeight += (visibleCount - 1) * ItemSpacing;
            }

            return totalHeight;
        }

        public UIControl GetChildAtPosition(Vector2 position)
        {
            var absPos = GetAbsolutePosition();
            var relativePos = new Vector2(position.X - absPos.X, position.Y - absPos.Y);

            foreach (var child in _children)
            {
                if (child.Visible && child.Contains(position))
                {
                    return child;
                }
            }

            return null;
        }
    }
}