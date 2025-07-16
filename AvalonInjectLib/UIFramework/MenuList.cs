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
            // Verificar si el elemento está en el área visible
            if (currentY + ItemHeight > contentArea.Y && currentY < contentArea.Y + contentArea.Height)
            {
                // Configurar posición del elemento
                var itemAbsPos = GetAbsolutePosition();
                item.X = BorderWidth;
                item.Y = currentY - itemAbsPos.Y;
                item.Width = Width - (BorderWidth * 2);
                item.Height = ItemHeight;
                item.Visible = true;

                // Dibujar el elemento
                item.Draw();
            }
            else
            {
                item.Visible = false;
            }

            currentY += ItemHeight;

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

            item.Parent = this;
            item.Level = 0;
            item.Height = ItemHeight;
            item.OnItemClick += OnItemClickHandler;
            item.OnItemExpanded += OnItemExpandedHandler;
            item.OnItemCollapsed += OnItemCollapsedHandler;

            _rootItems.Add(item);
            UpdateLayout();
        }

        public void RemoveRootItem(MenuItem item)
        {
            if (item == null) return;

            _rootItems.Remove(item);
            item.Parent = null;
            item.OnItemClick -= OnItemClickHandler;
            item.OnItemExpanded -= OnItemExpandedHandler;
            item.OnItemCollapsed -= OnItemCollapsedHandler;

            UpdateLayout();
        }

        public void ClearItems()
        {
            foreach (var item in _rootItems)
            {
                item.Parent = null;
                item.OnItemClick -= OnItemClickHandler;
                item.OnItemExpanded -= OnItemExpandedHandler;
                item.OnItemCollapsed -= OnItemCollapsedHandler;
            }

            _rootItems.Clear();
            SelectedItem = null;
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            float currentY = BorderWidth;

            foreach (var item in _rootItems)
            {
                UpdateItemLayout(item, ref currentY);
            }
        }

        private void UpdateItemLayout(MenuItem item, ref float currentY)
        {
            item.X = BorderWidth;
            item.Y = currentY;
            item.Width = Width - (BorderWidth * 2);
            item.Height = ItemHeight;

            currentY += ItemHeight;

            if (item.IsExpanded)
            {
                foreach (var child in item.Children)
                {
                    UpdateItemLayout(child, ref currentY);
                }
            }
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

    }
}