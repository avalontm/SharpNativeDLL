using static AvalonInjectLib.Structs;

namespace AvalonInjectLib.UIFramework
{
    public class MenuList : UIControl
    {
        // Propiedades de diseño (mantengo las originales)
        public Color BackgroundColor { get; set; } = Color.FromArgb(25, 25, 25);
        public Color BorderColor { get; set; } = Color.FromArgb(45, 45, 45);
        public Color HeaderColor { get; set; } = Color.FromArgb(255, 140, 0);
        public Color SeparatorColor { get; set; } = Color.FromArgb(40, 40, 40);
        public float BorderWidth { get; set; } = 1f;
        public float ItemHeight { get; set; } = 32f;
        public float HeaderHeight { get; set; } = 32f;
        public float SeparatorHeight { get; set; } = 1f;
        public float CornerRadius { get; set; } = 4f;

        // Header
        public string HeaderText { get; set; } = "Menu";
        public Font HeaderFont { get; set; } = Font.GetDefaultFont();
        public bool ShowHeader { get; set; } = true;
        public bool ShowBorder { get; set; } = true;

        // Colección de elementos
        private List<MenuItem> _items = new List<MenuItem>();

        // Elemento seleccionado
        public MenuItem SelectedItem { get; private set; }

        // Scroll
        private float _scrollOffset = 0f;
        private float _maxScrollOffset = 0f;

        // Sistema de submenús horizontales
        private List<SubMenuPanel> _activeSubMenus = new List<SubMenuPanel>();
        public List<SubMenuPanel> ActiveSubMenus => _activeSubMenus;   

        public bool IsSubMenuVisible(MenuItem parentItem)
        {
            return _activeSubMenus.Any(s => s.ParentItem == parentItem && s.Visible);
        }

        public IEnumerable<SubMenuPanel> GetActiveSubMenus()
        {
            return _activeSubMenus.Where(s => s.Visible);
        }

        // Eventos
        public Action<MenuItem> OnItemSelected;
        public Action<MenuItem> OnItemHovered;
        public Action<MenuItem> OnSubmenuRequested;

        public MenuList()
        {
            BackColor = BackgroundColor;
            Width = 220f;
            Height = 400f;
        }

        public void ToggleSubMenu(MenuItem parentItem)
        {
            var submenu = _activeSubMenus.FirstOrDefault(s => s.ParentItem == parentItem);
            if (submenu != null && submenu.Visible)
            {
                submenu.Visible = false;
            }
            else
            {
                ShowSubMenu(parentItem);
            }
        }

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();

            // Dibujar fondo
            Renderer.DrawRect(absPos.X, absPos.Y, Width, Height, BackgroundColor);

            // Dibujar borde si está habilitado
            if (ShowBorder)
            {
                Renderer.DrawRectOutline(absPos.X, absPos.Y, Width, Height, BorderColor, BorderWidth);
            }

            float contentY = absPos.Y + BorderWidth;

            // Dibujar header
            if (ShowHeader)
            {
                DrawHeader(absPos, ref contentY);
            }

            // Configurar área de contenido
            var contentArea = new Rect(
                absPos.X + BorderWidth,
                contentY,
                Width - (BorderWidth * 2),
                Height - (contentY - absPos.Y) - BorderWidth
            );

            // Dibujar elementos con scroll
            DrawItemsWithScroll(contentArea);

            // Dibujar submenús activos
            DrawActiveSubMenus();
        }

        private void DrawActiveSubMenus()
        {
            // Crear copia para iteración segura
            var subMenusToDraw = _activeSubMenus.ToList();

            foreach (var submenu in subMenusToDraw)
            {
                if (submenu.Visible)
                {
                    submenu.Draw();
                }
            }
        }

        private void DrawHeader(Vector2 absPos, ref float contentY)
        {
            var headerRect = new Rect(absPos.X + BorderWidth, contentY,
                Width - (BorderWidth * 2), HeaderHeight);

            // Fondo del header
            Renderer.DrawRect(headerRect.X, headerRect.Y, headerRect.Width, headerRect.Height, HeaderColor);

            // Texto del header
            float textX = headerRect.X + 8f;
            float textY = CalculateVerticalCenterPosition(headerRect.Y, HeaderHeight, HeaderFont, HeaderText);
            Renderer.DrawText(HeaderText, new Vector2(textX, textY), Color.Black, HeaderFont);

            contentY += HeaderHeight;

            // Separador
            Renderer.DrawRect(absPos.X + BorderWidth, contentY,
                Width - (BorderWidth * 2), SeparatorHeight, SeparatorColor);
            contentY += SeparatorHeight;
        }

        private void DrawItemsWithScroll(Rect contentArea)
        {
            float currentY = contentArea.Y - _scrollOffset;

            foreach (var item in _items)
            {
                if (item.Visible)
                {
                    // Calcular altura total del item
                    float totalItemHeight = item.CalculateTotalHeight();

                    // Verificar si el elemento está en el área visible
                    if (currentY + totalItemHeight > contentArea.Y && currentY < contentArea.Y + contentArea.Height)
                    {
                        // Configurar posición del elemento
                        var menuListAbsPos = GetAbsolutePosition();
                        item.X = BorderWidth;
                        item.Y = currentY - menuListAbsPos.Y;
                        item.Width = Width - (BorderWidth * 2);
                        item.Parent = this;

                        // Dibujar el item
                        item.Draw();
                    }

                    currentY += totalItemHeight;
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

            // Crear una copia de la lista para iterar
            var subMenusToUpdate = _activeSubMenus.ToList();

            // Actualizar submenús usando la copia
            foreach (var submenu in subMenusToUpdate)
            {
                if (submenu.Visible) // Solo actualizar si está visible
                {
                    submenu.Update();
                }
            }

            // Verificar clicks fuera de los submenús
            CheckClickOutsideSubMenus();
        }

        private void CheckClickOutsideSubMenus()
        {
            if (UIEventSystem.IsMouseClicked())
            {
                var mousePos = UIEventSystem.MousePosition;
                bool clickedInside = IsPointInAnyMenu(mousePos);

                if (!clickedInside)
                {
                    CloseAllSubMenus();
                }
            }
        }

        private bool IsPointInAnyMenu(Vector2 point)
        {
            // Verificar menú principal
            var absPos = GetAbsolutePosition();
            if (IsPointInRect(point, new Rect(absPos.X, absPos.Y, Width, Height)))
            {
                return true;
            }

            // Verificar submenús
            foreach (var submenu in _activeSubMenus)
            {
                if (submenu.Visible && submenu.IsPointInside(point))
                {
                    return true;
                }
            }

            return false;
        }

        public void ShowSubMenu(MenuItem parentItem)
        {
            if (!parentItem.HasSubItems) return;

            int parentLevel = GetItemLevel(parentItem);
            int newSubMenuLevel = parentLevel + 1;

            // 1. Verificar si ya existe este submenú (incluso si está invisible)
            var existingSubmenu = _activeSubMenus.FirstOrDefault(s => s.ParentItem == parentItem);
            if (existingSubmenu != null)
            {
                if (existingSubmenu.Visible)
                {
                    return; // Ya está visible, no hacer nada
                }
                else
                {
                    // Remover completamente el submenú invisible
                    existingSubmenu.OnItemSelected = null;
                    existingSubmenu.OnItemHovered = null;
                    foreach (var item in existingSubmenu.SubItems)
                    {
                        item.Click -= existingSubmenu.OnSubItemClick;
                        item.MouseEnter -= existingSubmenu.OnSubItemHover;
                    }
                    _activeSubMenus.Remove(existingSubmenu);
                }
            }

            // 2. Verificar jerarquía y cerrar submenús no relacionados
            if (parentLevel == 0)
            {
                CloseSubMenusNotInHierarchy(parentItem);
            }
            else
            {
                bool belongsToCurrentHierarchy = BelongsToCurrentHierarchy(parentItem);

                if (!belongsToCurrentHierarchy)
                {
                    CloseAllSubMenus();
                    parentLevel = GetItemLevel(parentItem);
                    newSubMenuLevel = parentLevel + 1;
                }
            }

            // 3. Cerrar submenús de niveles superiores al actual
            CloseSubMenusFromLevel(newSubMenuLevel);

            // 4. Calcular posición del nuevo submenú
            var itemRect = GetItemRect(parentItem);
            float posY = itemRect.Y;
            float posX = CalculateSubMenuXPosition(parentItem, newSubMenuLevel);

            // Verificar límites de pantalla
            float screenWidth = Renderer.ScreenWidth;
            float screenHeight = Renderer.ScreenHeight;

            if (posX + Width > screenWidth)
            {
                posX = itemRect.X - Width - 2f;
            }

            if (posY + Height > screenHeight)
            {
                posY = Math.Max(0, screenHeight - Height);
            }

            // 5. Crear nuevo submenú
            var newSubmenu = new SubMenuPanel(parentItem, new Vector2(posX, posY), this, newSubMenuLevel)
            {
                BackgroundColor = this.BackgroundColor,
                BorderColor = this.BorderColor,
                BorderWidth = this.BorderWidth,
                ItemHeight = this.ItemHeight,
                Width = this.Width
            };

            newSubmenu.OnItemSelected += (item) => OnItemSelected?.Invoke(item);
            _activeSubMenus.Add(newSubmenu);
        }

        private float CalculateSubMenuXPosition(MenuItem parentItem, int level)
        {
            if (level == 1) // Primer nivel (desde menú principal)
            {
                var mainMenuAbsPos = GetAbsolutePosition();
                return mainMenuAbsPos.X + Width + 2f; // Menú principal + gap
            }
            else // Niveles superiores
            {
                // Encontrar el submenú padre
                var parentSubmenu = _activeSubMenus.FirstOrDefault(s =>
                    s.SubItems.Contains(parentItem) && s.Visible);

                if (parentSubmenu != null)
                {
                    return parentSubmenu.X + parentSubmenu.Width + 2f; // Submenú padre + gap
                }
                else
                {
                    // Fallback: calcular basado en nivel
                    var mainMenuAbsPos = GetAbsolutePosition();
                    return mainMenuAbsPos.X + (Width + 2f) * level;
                }
            }
        }

        private bool BelongsToCurrentHierarchy(MenuItem item)
        {
            if (item == null) return false;

            // Si no hay submenús activos, cualquier item del menú principal pertenece
            if (!_activeSubMenus.Any(s => s.Visible))
            {
                return item.Parent == this;
            }

            // Verificar si el item está en algún submenú visible
            foreach (var submenu in _activeSubMenus.Where(s => s.Visible))
            {
                if (submenu.SubItems.Contains(item))
                {
                    return true;
                }
            }

            // Verificar si el item está en el menú principal y tiene conexión con algún submenú visible
            if (item.Parent == this)
            {
                foreach (var submenu in _activeSubMenus.Where(s => s.Visible))
                {
                    if (IsInHierarchy(item, submenu.ParentItem))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsDescendantOf(MenuItem child, MenuItem ancestor)
        {
            if (child == null || ancestor == null) return false;

            var current = child;
            while (current != null)
            {
                if (current.Parent is SubMenuPanel subMenu)
                {
                    if (subMenu.ParentItem == ancestor) return true;
                    current = subMenu.ParentItem;
                }
                else if (current.Parent == this) // Es del menú principal
                {
                    return false;
                }
                else
                {
                    break;
                }
            }

            return false;
        }

        private void CloseSubMenusNotInHierarchy(MenuItem rootItem)
        {
            var subMenusToClose = new List<SubMenuPanel>();

            foreach (var submenu in _activeSubMenus.ToList())
            {
                if (submenu.Visible && !IsInHierarchy(submenu.ParentItem, rootItem))
                {
                    subMenusToClose.Add(submenu);
                }
            }

            // Limpiar completamente los submenús que no pertenecen a la jerarquía
            foreach (var submenu in subMenusToClose)
            {
                submenu.Visible = false;
                submenu.OnItemSelected = null;
                submenu.OnItemHovered = null;

                foreach (var item in submenu.SubItems)
                {
                    item.Click -= submenu.OnSubItemClick;
                    item.MouseEnter -= submenu.OnSubItemHover;
                }
            }

            // Remover completamente de la lista
            _activeSubMenus.RemoveAll(s => subMenusToClose.Contains(s));
        }

        private bool IsInHierarchy(MenuItem item, MenuItem root)
        {
            if (item == root) return true;

            // Verificar si root es ancestro de item
            if (IsDescendantOf(item, root)) return true;
           
            // Verificar si item es ancestro de root
            if (IsDescendantOf(root, item)) return true;

            return false;
        }

        // Método auxiliar simplificado para GetItemLevel
        public int GetItemLevel(MenuItem item)
        {
            if (item == null) return 0;

            int level = 0;
            var current = item;

            while (current != null)
            {
                if (current.Parent is SubMenuPanel subMenu)
                {
                    level++;
                    current = subMenu.ParentItem;
                }
                else
                {
                    break;
                }
            }

            return level;
        }

        // Método auxiliar para verificar jerarquía
        private bool IsChildOf(MenuItem parent, MenuItem child)
        {
            if (parent == null || child == null) return false;

            var current = child;
            while (current != null)
            {
                if (current.Parent is SubMenuPanel subMenu)
                {
                    if (subMenu.ParentItem == parent) return true;
                    current = subMenu.ParentItem;
                }
                else
                {
                    current = null;
                }
            }

            return false;
        }

        public void CloseSubMenusFromLevel(int level)
        {
            // Crear lista de submenús a cerrar
            var subMenusToClose = new List<SubMenuPanel>();

            foreach (var submenu in _activeSubMenus.ToList()) // Usar ToList() para evitar modificación durante iteración
            {
                int submenuLevel = GetItemLevel(submenu.ParentItem);
                if (submenuLevel >= level)
                {
                    subMenusToClose.Add(submenu);
                }
            }

            // Cerrar y limpiar submenús
            foreach (var submenu in subMenusToClose)
            {
                submenu.Visible = false;
                // Limpiar eventos para evitar referencias colgantes
                submenu.OnItemSelected = null;
                submenu.OnItemHovered = null;

                // Limpiar eventos de los subitems
                foreach (var subItem in submenu.SubItems)
                {
                    subItem.Click -= submenu.OnSubItemClick;
                    subItem.MouseEnter -= submenu.OnSubItemHover;
                }
            }

            // Remover de la lista activa
            _activeSubMenus.RemoveAll(s => subMenusToClose.Contains(s));
        }

        public void CloseAllSubMenus()
        {
            // Limpiar todos los submenús
            foreach (var submenu in _activeSubMenus.ToList())
            {
                submenu.Visible = false;
                submenu.OnItemSelected = null;
                submenu.OnItemHovered = null;

                foreach (var item in submenu.SubItems)
                {
                    item.Click -= submenu.OnSubItemClick;
                    item.MouseEnter -= submenu.OnSubItemHover;
                }
            }

            _activeSubMenus.Clear();

            // Deseleccionar el ítem actual si es necesario
            if (SelectedItem != null)
            {
                SelectedItem.SetSelected(false);
                SelectedItem = null;
            }
        }

        private Rect GetItemRect(MenuItem item)
        {
            // Verificar si el item está en el menú principal
            if (item.Parent == this)
            {
                var absPos = GetAbsolutePosition();
                return new Rect(
                    absPos.X + item.X,
                    absPos.Y + item.Y,
                    item.Width,
                    item.CalculateTotalHeight()
                );
            }

            // Verificar si está en algún submenú
            foreach (var submenu in _activeSubMenus)
            {
                if (submenu.SubItems.Contains(item))
                {
                    return new Rect(
                        submenu.X + item.X,
                        submenu.Y + item.Y,
                        item.Width,
                        item.Height
                    );
                }
            }

            // Fallback
            var fallbackAbsPos = GetAbsolutePosition();
            return new Rect(fallbackAbsPos.X, fallbackAbsPos.Y, Width, ItemHeight);
        }

        private int GetSubMenuLevel(SubMenuPanel submenu)
        {
            return GetItemLevel(submenu.ParentItem);
        }

        private bool IsPointInRect(Vector2 point, Rect rect)
        {
            return point.X >= rect.X && point.X <= rect.X + rect.Width &&
                   point.Y >= rect.Y && point.Y <= rect.Y + rect.Height;
        }

        // Resto de métodos originales...
        private void UpdateVisibleItems()
        {
            foreach (var item in _items)
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
            float totalHeight = 0f;
            foreach (var item in _items)
            {
                if (item.Visible)
                {
                    totalHeight += item.CalculateTotalHeight();
                }
            }

            float availableHeight = Height - BorderWidth * 2;

            if (ShowHeader)
            {
                availableHeight -= HeaderHeight + SeparatorHeight;
            }

            _maxScrollOffset = Math.Max(0, totalHeight - availableHeight);
        }

        public void AddItem(MenuItem item)
        {
            if (item == null) return;

            SetupMenuItem(item);
            _items.Add(item);
            UpdateLayout();
        }

        private void SetupMenuItem(MenuItem item)
        {
            item.Parent = this;
            item.Click += OnItemClickHandler;
            item.MouseEnter += OnItemHoverHandler;
        }

        public void RemoveItem(MenuItem item)
        {
            if (item == null) return;

            _items.Remove(item);
            CleanupMenuItem(item);
            UpdateLayout();
        }

        private void CleanupMenuItem(MenuItem item)
        {
            item.Parent = null;
            item.Click -= OnItemClickHandler;
            item.MouseEnter -= OnItemHoverHandler;
        }

        public void ClearItems()
        {
            foreach (var item in _items)
            {
                CleanupMenuItem(item);
            }

            _items.Clear();
            SelectedItem = null;
            CloseAllSubMenus();
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            UpdateMaxScroll();
        }

        private void OnItemClickHandler(object sender, Vector2 pos)
        {
            MenuItem item = (MenuItem)sender;
            SetSelectedItem(item);
            OnItemSelected?.Invoke(item);

            // Obtener el nivel del item clickeado
            int currentLevel = GetItemLevel(item);

            if (item.HasSubItems)
            {
                // Verificar si ya está visible
                var existingSubmenu = _activeSubMenus.FirstOrDefault(s => s.ParentItem == item && s.Visible);
                if (existingSubmenu != null)
                {
                    // Si está visible, cerrarlo junto con todos sus submenús (niveles superiores)
                    CloseSubMenusFromLevel(currentLevel + 1);

                    // Limpiar y remover el submenú actual
                    existingSubmenu.Visible = false;
                    existingSubmenu.OnItemSelected = null;
                    existingSubmenu.OnItemHovered = null;
                    foreach (var subItem in existingSubmenu.SubItems)
                    {
                        subItem.Click -= existingSubmenu.OnSubItemClick;
                        subItem.MouseEnter -= existingSubmenu.OnSubItemHover;
                    }
                    _activeSubMenus.Remove(existingSubmenu);
                }
                else
                {
                    // Si no está visible, cerrar solo los niveles superiores y mostrarlo
                    CloseSubMenusFromLevel(currentLevel + 1);
                    ShowSubMenu(item);
                }
            }
            else
            {
                // Item final - cerrar solo los niveles superiores al actual
                CloseSubMenusFromLevel(currentLevel + 1);
            }
        }

        private void OnItemHoverHandler(object sender, Vector2 pos)
        {
            // Solo manejar hover para resaltar el item, no para abrir submenús
            MenuItem item = (MenuItem)sender;
            OnItemHovered?.Invoke(item);
        }

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

        public MenuItem FindItem(string text)
        {
            return _items.FirstOrDefault(item => item.Name == text);
        }

        public List<MenuItem> GetAllItems()
        {
            return new List<MenuItem>(_items);
        }

        public void ScrollUp(float amount = 25f)
        {
            _scrollOffset = Math.Max(0, _scrollOffset - amount);
        }

        public void ScrollDown(float amount = 25f)
        {
            _scrollOffset = Math.Min(_maxScrollOffset, _scrollOffset + amount);
        }

        private float CalculateVerticalCenterPosition(float elementY, float elementHeight, Font font, string text)
        {
            if (font == null || !font.IsReady || string.IsNullOrEmpty(text))
            {
                return elementY + (elementHeight / 2);
            }

            var textSize = font.MeasureText(text);
            float textHeight = textSize.Y;
            return elementY + (elementHeight - textHeight) / 2;
        }

        public void CleanupSubMenus()
        {
            // Este método sí limpia completamente los submenús
            foreach (var submenu in _activeSubMenus)
            {
                submenu.OnItemSelected = null;
                submenu.OnItemHovered = null;

                foreach (var item in submenu.SubItems)
                {
                    item.Click -= submenu.OnSubItemClick;
                    item.MouseEnter -= submenu.OnSubItemHover;
                }
            }
            _activeSubMenus.Clear();
        }


        public void Clean()
        {
            ClearItems();
            CleanupSubMenus();
        }

        public int ItemCount => _items.Count;
        public bool HasItems => _items.Count > 0;
    }
}