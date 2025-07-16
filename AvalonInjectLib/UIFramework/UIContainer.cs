using static AvalonInjectLib.Structs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AvalonInjectLib.UIFramework
{
    public abstract class UIContainer : UIControl
    {
        // Lista de controles hijos
        protected List<UIControl> children = new List<UIControl>();

        // Propiedades públicas de solo lectura
        public IReadOnlyList<UIControl> Children => children.AsReadOnly();
        public int ChildCount => children.Count;

        // Propiedades para el manejo de clipping
        public bool ClipChildren { get; set; } = true;
        public bool AutoSize { get; set; } = false;
        public bool AutoScroll { get; set; } = false;

        // Propiedades para el layout
        public LayoutStyle LayoutStyle { get; set; } = LayoutStyle.None;
        public float LayoutSpacing { get; set; } = 5f;
        public LayoutDirection LayoutDirection { get; set; } = LayoutDirection.Vertical;

        // Eventos
        public event Action<UIControl>? ChildAdded;
        public event Action<UIControl>? ChildRemoved;
        public event Action? ChildrenChanged;

        // Constructor
        protected UIContainer() : base()
        {
            // Configuración por defecto para contenedores
            Width = 200f;
            Height = 150f;
        }

        // Métodos para manejo de controles hijos
        public virtual void AddChild(UIControl child)
        {
            if (child == null) return;

            // Evitar duplicados
            if (children.Contains(child)) return;

            // Evitar ciclos (un control no puede ser hijo de sí mismo o de sus descendientes)
            if (IsDescendantOf(child)) return;

            // Remover del contenedor anterior si existe
            if (child.Parent is UIContainer container)
                container?.RemoveChild(child);

            // Agregar a la lista de hijos
            children.Add(child);
            child.Parent = this;

            // Aplicar layout automático si está habilitado
            if (LayoutStyle != LayoutStyle.None)
            {
                ApplyLayout();
            }

            // Aplicar auto-size si está habilitado
            if (AutoSize)
            {
                RecalculateSize();
            }

            // Disparar eventos
            ChildAdded?.Invoke(child);
            ChildrenChanged?.Invoke();
        }

        public virtual void RemoveChild(UIControl child)
        {
            if (child == null) return;

            if (children.Remove(child))
            {
                child.Parent = null;

                // Aplicar layout automático si está habilitado
                if (LayoutStyle != LayoutStyle.None)
                {
                    ApplyLayout();
                }

                // Aplicar auto-size si está habilitado
                if (AutoSize)
                {
                    RecalculateSize();
                }

                // Disparar eventos
                ChildRemoved?.Invoke(child);
                ChildrenChanged?.Invoke();
            }
        }

        public virtual void RemoveChildAt(int index)
        {
            if (index >= 0 && index < children.Count)
            {
                RemoveChild(children[index]);
            }
        }

        public virtual void InsertChild(int index, UIControl child)
        {
            if (child == null) return;
            if (index < 0 || index > children.Count) return;

            // Evitar duplicados
            if (children.Contains(child)) return;

            // Evitar ciclos
            if (IsDescendantOf(child)) return;

            // Remover del contenedor anterior si existe
            if (child.Parent is UIContainer container)
                container?.RemoveChild(child);

            // Insertar en la posición especificada
            children.Insert(index, child);
            child.Parent = this;

            // Aplicar layout automático si está habilitado
            if (LayoutStyle != LayoutStyle.None)
            {
                ApplyLayout();
            }

            // Aplicar auto-size si está habilitado
            if (AutoSize)
            {
                RecalculateSize();
            }

            // Disparar eventos
            ChildAdded?.Invoke(child);
            ChildrenChanged?.Invoke();
        }

        public virtual void ClearChildren()
        {
            var childrenCopy = children.ToArray();
            foreach (var child in childrenCopy)
            {
                RemoveChild(child);
            }
        }

        public virtual void BringChildToFront(UIControl child)
        {
            if (child == null || !children.Contains(child)) return;

            children.Remove(child);
            children.Add(child);
        }

        public virtual void SendChildToBack(UIControl child)
        {
            if (child == null || !children.Contains(child)) return;

            children.Remove(child);
            children.Insert(0, child);
        }

        // Métodos de búsqueda
        public UIControl? FindChild(string name)
        {
            foreach (var child in children)
            {
                if (child.Name == name)
                    return child;

                // Búsqueda recursiva en contenedores hijos
                if (child is UIContainer container)
                {
                    var found = container.FindChild(name);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        public T? FindChild<T>(string name) where T : UIControl
        {
            return FindChild(name) as T;
        }

        public List<T> FindControlsByType<T>() where T : UIControl
        {
            var result = new List<T>();

            foreach (var child in children)
            {
                if (child is T typedControl)
                {
                    result.Add(typedControl);
                }

                if (child is UIContainer container)
                {
                    result.AddRange(container.FindControlsByType<T>());
                }
            }

            return result;
        }

        public UIControl? FindChildByTag(object tag)
        {
            foreach (var child in children)
            {
                if (Equals(child.Tag, tag))
                    return child;

                if (child is UIContainer container)
                {
                    var found = container.FindChildByTag(tag);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        // Métodos de layout automático
        protected virtual void ApplyLayout()
        {
            switch (LayoutStyle)
            {
                case LayoutStyle.Vertical:
                    ApplyVerticalLayout();
                    break;
                case LayoutStyle.Horizontal:
                    ApplyHorizontalLayout();
                    break;
                case LayoutStyle.Grid:
                    ApplyGridLayout();
                    break;
                case LayoutStyle.Flow:
                    ApplyFlowLayout();
                    break;
            }
        }

        protected virtual void ApplyVerticalLayout()
        {
            float currentY = 0;
            foreach (var child in children)
            {
                if (!child.Visible) continue;

                child.Y = currentY;
                currentY += child.Height + LayoutSpacing;
            }
        }

        protected virtual void ApplyHorizontalLayout()
        {
            float currentX = 0;
            foreach (var child in children)
            {
                if (!child.Visible) continue;

                child.X = currentX;
                currentX += child.Width + LayoutSpacing;
            }
        }

        protected virtual void ApplyGridLayout()
        {
            // Implementación básica de grid - puede ser extendida
            int columns = (int)Math.Max(1, Width / (100 + LayoutSpacing)); // Asume ancho de 100px por defecto
            int row = 0;
            int col = 0;

            foreach (var child in children)
            {
                if (!child.Visible) continue;

                child.X = col * (child.Width + LayoutSpacing);
                child.Y = row * (child.Height + LayoutSpacing);

                col++;
                if (col >= columns)
                {
                    col = 0;
                    row++;
                }
            }
        }

        protected virtual void ApplyFlowLayout()
        {
            float currentX = 0;
            float currentY = 0;
            float lineHeight = 0;

            foreach (var child in children)
            {
                if (!child.Visible) continue;

                // Si el control no cabe en la línea actual, pasar a la siguiente
                if (currentX + child.Width > Width && currentX > 0)
                {
                    currentX = 0;
                    currentY += lineHeight + LayoutSpacing;
                    lineHeight = 0;
                }

                child.X = currentX;
                child.Y = currentY;

                currentX += child.Width + LayoutSpacing;
                lineHeight = Math.Max(lineHeight, child.Height);
            }
        }

        // Auto-size
        protected virtual void RecalculateSize()
        {
            if (!AutoSize || children.Count == 0) return;

            float maxRight = 0;
            float maxBottom = 0;

            foreach (var child in children)
            {
                if (!child.Visible) continue;

                maxRight = Math.Max(maxRight, child.X + child.Width);
                maxBottom = Math.Max(maxBottom, child.Y + child.Height);
            }

            Width = maxRight + LayoutSpacing;
            Height = maxBottom + LayoutSpacing;
        }

        // Renderizado
        public virtual void RenderWithChildren()
        {
            if (!Visible) return;

            // Dibujar el contenedor
            Draw();

            // Dibujar los hijos
            foreach (var child in children)
            {
                if (!child.Visible) continue;

                // Verificar clipping si está habilitado
                if (ClipChildren && !IsChildInBounds(child))
                    continue;

                if (child is UIContainer container)
                {
                    container.RenderWithChildren();
                }
                else
                {
                    child.Draw();
                }
            }
        }

        // Procesamiento de input
        public override void Update()
        {
            if (!Visible || !Enabled) return;

            // Procesar input del contenedor
            base.Update();

            // Procesar input de los hijos en orden inverso (último dibujado primero)
            for (int i = children.Count - 1; i >= 0; i--)
            {
                var child = children[i];
                if (child.Visible && child.Enabled)
                {
                    child.Update();
                }
            }
        }

        // Método para obtener el control que está bajo el mouse
        public UIControl? GetControlUnderMouse(Vector2 mousePos)
        {
            if (!Visible || !Enabled) return null;

            // Buscar en orden inverso para priorizar controles que están "arriba"
            for (int i = children.Count - 1; i >= 0; i--)
            {
                var child = children[i];
                if (!child.Visible || !child.Enabled) continue;

                // Verificar clipping
                if (ClipChildren && !IsChildInBounds(child))
                    continue;

                if (child is UIContainer container)
                {
                    var found = container.GetControlUnderMouse(mousePos);
                    if (found != null) return found;
                }
                else if (child.Contains(mousePos))
                {
                    return child;
                }
            }

            // Si ningún hijo maneja el mouse, verificar si el contenedor lo hace
            return Contains(mousePos) ? this : null;
        }

        // Métodos auxiliares
        protected virtual bool IsChildInBounds(UIControl child)
        {
            var childBounds = child.GetAbsoluteBounds();
            var containerBounds = GetAbsoluteBounds();

            return childBounds.X < containerBounds.X + containerBounds.Width &&
                   childBounds.X + childBounds.Width > containerBounds.X &&
                   childBounds.Y < containerBounds.Y + containerBounds.Height &&
                   childBounds.Y + childBounds.Height > containerBounds.Y;
        }

        protected virtual bool IsDescendantOf(UIControl control)
        {
            UIControl? current = this;
            while (current != null)
            {
                if (current == control) return true;
                current = current.Parent;
            }
            return false;
        }

        // Métodos de ordenamiento
        public void SortChildren<T>(Func<UIControl, T> keySelector) where T : IComparable<T>
        {
            children.Sort((a, b) => keySelector(a).CompareTo(keySelector(b)));
        }

        public void SortChildren(Comparison<UIControl> comparison)
        {
            children.Sort(comparison);
        }

        // Métodos de iteración
        public void ForEachChild(Action<UIControl> action)
        {
            foreach (var child in children)
            {
                action(child);
            }
        }

        public void ForEachChild<T>(Action<T> action) where T : UIControl
        {
            foreach (var child in children.OfType<T>())
            {
                action(child);
            }
        }

        // Override de métodos base
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            // Aplicar layout si está habilitado
            if (LayoutStyle != LayoutStyle.None)
            {
                ApplyLayout();
            }
        }

        // Método para verificar si el contenedor está vacío
        public bool IsEmpty => children.Count == 0;

        // Método para obtener estadísticas
        public ContainerStats GetStats()
        {
            return new ContainerStats
            {
                ChildCount = children.Count,
                VisibleChildren = children.Count(c => c.Visible),
                EnabledChildren = children.Count(c => c.Enabled),
                ContainerChildren = children.OfType<UIContainer>().Count()
            };
        }
    }

    // Enums para el layout
    public enum LayoutStyle
    {
        None,
        Vertical,
        Horizontal,
        Grid,
        Flow
    }

    public enum LayoutDirection
    {
        Vertical,
        Horizontal
    }

    // Estructura para estadísticas
    public struct ContainerStats
    {
        public int ChildCount { get; set; }
        public int VisibleChildren { get; set; }
        public int EnabledChildren { get; set; }
        public int ContainerChildren { get; set; }
    }
}