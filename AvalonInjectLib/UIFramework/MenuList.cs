using static AvalonInjectLib.Structs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AvalonInjectLib.UIFramework
{
    public class MenuList : UIControl
    {
        // Propiedades de diseño
        public Color BackgroundColor { get; set; } = Color.FromArgb(35, 35, 35);
        public Color BorderColor { get; set; } = Color.FromArgb(100, 100, 100);
        public float BorderWidth { get; set; } = 1f;
        public bool ShowBorder { get; set; } = true;
        public float ItemHeight { get; set; } = 25f;

        // Colección de elementos raíz
        private List<MenuItem> _rootItems = new List<MenuItem>();

        // Elemento seleccionado actualmente
        public MenuItem SelectedItem { get; private set; }

        // Eventos
        public Action<MenuItem> OnItemSelected;
        public Action<MenuItem> OnItemExpanded;
        public Action<MenuItem> OnItemCollapsed;

        // Scroll (básico)
        private float _scrollOffset = 0f;
        private float _maxScrollOffset = 0f;

        public MenuList()
        {
            BackColor = BackgroundColor;
            Width = 200f;
            Height = 300f;
        }

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();

            // Dibujar fondo
            Renderer.DrawRect(new Rect(absPos.X, absPos.Y, Width, Height), BackgroundColor);

            // Dibujar borde
            if (ShowBorder)
            {
                Renderer.DrawRectOutline(
                    new Rect(absPos.X, absPos.Y, Width, Height),
                    BorderColor,
                    BorderWidth
                );
            }

            // Configurar clipping para el área de contenido
            var contentArea = new Rect(absPos.X + BorderWidth, absPos.Y + BorderWidth,
                Width - (BorderWidth * 2), Height - (BorderWidth * 2));

            // Dibujar elementos con scroll
            DrawItemsWithScroll(contentArea);
        }

        private void DrawItemsWithScroll(Rect contentArea)
        {
            float currentY = contentArea.Y - _scrollOffset;

            foreach (var item in _rootItems)
            {
                DrawItemAndChildren(item, contentArea, ref currentY);
            }
        }

        private void DrawItemAndChildren(MenuItem item, Rect contentArea, ref float currentY)
        {
            var menuListAbsPos = GetAbsolutePosition();

            // Verificar si el elemento está en el área visible
            if (currentY + ItemHeight > contentArea.Y && currentY < contentArea.Y + contentArea.Height)
            {
                // Configurar posición del elemento RELATIVA al MenuList
                item.X = BorderWidth;
                item.Y = currentY - menuListAbsPos.Y;
                item.Width = Width - (BorderWidth * 2);
                item.Height = ItemHeight;
                item.Visible = true;
                item.Parent = this; // Asegurar que el parent esté configurado correctamente
            }
            else
            {
                item.Visible = false;
            }

            currentY += ItemHeight;

            //Dibujar Item
            item.Draw();

            // Dibujar hijos si están expandidos
            if (item.IsExpanded)
            {
                foreach (var child in item.Children)
                {
                    DrawItemAndChildren(child, contentArea, ref currentY);
                }
            }
        }

        public override void Update()
        {
            base.Update();

            // Actualizar scroll máximo
            UpdateMaxScroll();

            // Actualizar elementos visibles
            UpdateVisibleItems();
        }

        private void UpdateVisibleItems()
        {
            foreach (var item in GetAllItems())
            {
                if (item.Visible)
                {
                    item.Parent = this;
                    item.Update();
                }
            }
        }

        private void UpdateMaxScroll()
        {
            float totalHeight = GetTotalContentHeight();
            _maxScrollOffset = Math.Max(0, totalHeight - (Height - BorderWidth * 2));
        }

        private float GetTotalContentHeight()
        {
            float totalHeight = 0;
            foreach (var item in _rootItems)
            {
                totalHeight += GetItemTotalHeight(item);
            }
            return totalHeight;
        }

        private float GetItemTotalHeight(MenuItem item)
        {
            float height = ItemHeight;

            if (item.IsExpanded)
            {
                foreach (var child in item.Children)
                {
                    height += GetItemTotalHeight(child);
                }
            }

            return height;
        }

        // Métodos públicos para manejo de elementos
        public void AddRootItem(MenuItem item)
        {
            if (item == null) return;

            SetupMenuItem(item, 0);
            _rootItems.Add(item);
            UpdateLayout();
        }

        private void SetupMenuItem(MenuItem item, int level)
        {
            item.Parent = this;
            item.Level = level;
            item.Height = ItemHeight;
            item.OnItemClick += OnItemClickHandler;
            item.OnItemExpanded += OnItemExpandedHandler;
            item.OnItemCollapsed += OnItemCollapsedHandler;

            // Configurar recursivamente los hijos
            foreach (var child in item.Children)
            {
                SetupMenuItem(child, level + 1);
            }
        }

        public void RemoveRootItem(MenuItem item)
        {
            if (item == null) return;

            _rootItems.Remove(item);
            CleanupMenuItem(item);
            UpdateLayout();
        }

        private void CleanupMenuItem(MenuItem item)
        {
            item.Parent = null;
            item.OnItemClick -= OnItemClickHandler;
            item.OnItemExpanded -= OnItemExpandedHandler;
            item.OnItemCollapsed -= OnItemCollapsedHandler;

            // Limpiar recursivamente los hijos
            foreach (var child in item.Children)
            {
                CleanupMenuItem(child);
            }
        }

        public void ClearItems()
        {
            foreach (var item in _rootItems)
            {
                CleanupMenuItem(item);
            }

            _rootItems.Clear();
            SelectedItem = null;
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            // No necesitamos actualizar layout aquí porque se maneja en DrawItemsWithScroll
            // Solo actualizamos el scroll máximo
            UpdateMaxScroll();
        }

        // Manejadores de eventos
        private void OnItemClickHandler(MenuItem item)
        {
            SetSelectedItem(item);
            OnItemSelected?.Invoke(item);
        }

        private void OnItemExpandedHandler(MenuItem item)
        {
            UpdateLayout();
            OnItemExpanded?.Invoke(item);
        }

        private void OnItemCollapsedHandler(MenuItem item)
        {
            UpdateLayout();
            OnItemCollapsed?.Invoke(item);
        }

        // Métodos de selección
        public void SetSelectedItem(MenuItem item)
        {
            if (SelectedItem != null)
            {
                SelectedItem.SetSelected(false);
            }

            SelectedItem = item;

            if (item != null)
            {
                item.SetSelected(true);
            }
        }

        public void SelectItem(string text)
        {
            var item = FindItem(text);
            if (item != null)
            {
                SetSelectedItem(item);
            }
        }

        // Métodos de búsqueda
        public MenuItem FindItem(string text)
        {
            foreach (var item in _rootItems)
            {
                var found = item.FindItem(text);
                if (found != null) return found;
            }
            return null;
        }

        public List<MenuItem> GetAllItems()
        {
            var allItems = new List<MenuItem>();
            foreach (var item in _rootItems)
            {
                allItems.AddRange(item.GetAllItems());
            }
            return allItems;
        }

        public List<MenuItem> GetRootItems()
        {
            return new List<MenuItem>(_rootItems);
        }

        // Métodos de scroll
        public void ScrollUp(float amount = 25f)
        {
            _scrollOffset = Math.Max(0, _scrollOffset - amount);
        }

        public void ScrollDown(float amount = 25f)
        {
            _scrollOffset = Math.Min(_maxScrollOffset, _scrollOffset + amount);
        }

        public void ScrollToItem(MenuItem item)
        {
            if (item == null) return;

            // Calcular posición Y del elemento
            float itemY = GetItemYPosition(item);

            // Ajustar scroll si es necesario
            if (itemY < _scrollOffset)
            {
                _scrollOffset = itemY;
            }
            else if (itemY + ItemHeight > _scrollOffset + Height - BorderWidth * 2)
            {
                _scrollOffset = itemY + ItemHeight - (Height - BorderWidth * 2);
            }

            _scrollOffset = Math.Max(0, Math.Min(_maxScrollOffset, _scrollOffset));
        }

        private float GetItemYPosition(MenuItem item)
        {
            float currentY = 0;

            foreach (var rootItem in _rootItems)
            {
                if (FindItemYPosition(rootItem, item, ref currentY))
                {
                    return currentY;
                }
            }

            return 0;
        }

        private bool FindItemYPosition(MenuItem current, MenuItem target, ref float currentY)
        {
            if (current == target)
            {
                return true;
            }

            currentY += ItemHeight;

            if (current.IsExpanded)
            {
                foreach (var child in current.Children)
                {
                    if (FindItemYPosition(child, target, ref currentY))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void OnClick(Vector2 mousePos)
        {
            base.OnClick(mousePos);

            // Propagar el evento de mouse a todos los elementos visibles
            foreach (var item in GetAllItems())
            {
                if (item.Visible)
                {
                    var itemAbsPos = item.GetAbsolutePosition();
                    var itemRect = new Rect(itemAbsPos.X, itemAbsPos.Y, item.Width, item.Height);

                    if (IsPointInRect(mousePos, itemRect))
                    {
                        item.Click?.Invoke(mousePos);
                        break;
                    }
                }
            }
        }

        private bool IsPointInRect(Vector2 point, Rect rect)
        {
            return point.X >= rect.X && point.X <= rect.X + rect.Width &&
                   point.Y >= rect.Y && point.Y <= rect.Y + rect.Height;
        }
    }
}