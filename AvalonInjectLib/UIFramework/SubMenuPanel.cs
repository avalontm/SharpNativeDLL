using static AvalonInjectLib.Structs;

namespace AvalonInjectLib.UIFramework
{
    public class SubMenuPanel : UIControl
    {
        public MenuItem ParentItem { get; private set; }
        public MenuList ParentMenu { get; private set; }

        public Color BackgroundColor { get; set; } = Color.FromArgb(25, 25, 25);
        public Color BorderColor { get; set; } = Color.FromArgb(45, 45, 45);
        public float BorderWidth { get; set; } = 1f;
        public float ItemHeight { get; set; } = 28f;
        public float MinWidth { get; set; } = 200f;



        private List<MenuItem> _subItems = new List<MenuItem>();
        public List<MenuItem> SubItems => _subItems;

        private float _scrollOffset = 0f;
        private float _maxScrollOffset = 0f;
        private int _level;
        private int MaxHeight = 400;

        public Action<MenuItem> OnItemSelected;
        public Action<MenuItem> OnItemHovered;

        public SubMenuPanel(MenuItem parentItem, Vector2 position, MenuList parentMenu, int level)
        {
            ParentItem = parentItem;
            ParentMenu = parentMenu;
            X = position.X;
            Y = position.Y;
            _level = level;
            Visible = true;

            // Obtener subitems del item padre
            if (parentItem.HasSubItems)
            {
                _subItems = parentItem.GetSubItems();
            }

            CalculateSize();
            SetupSubItems();
        }

        private void CalculateSize()
        {
            float maxWidth = MinWidth;
            float totalHeight = 0f;

            // Sumar alturas de items sin márgenes
            foreach (var item in _subItems)
            {
                if (item.Visible)
                {
                    // Sumar la altura real de cada item individual
                    totalHeight += item.Height;

                    // Actualizamos el ancho máximo si es necesario
                    maxWidth = Math.Max(maxWidth, item.Width);
                }
            }

            Width = maxWidth + (BorderWidth * 2); // Añadir el ancho del borde

            // La altura del panel es la suma de todas las alturas de los items más los bordes
            // Aplicar límite máximo si es necesario
            Height = Math.Min(totalHeight + (BorderWidth * 2), MaxHeight);

            // Calcular el offset máximo para scroll si es necesario
            if (totalHeight + (BorderWidth * 2) > MaxHeight)
            {
                _maxScrollOffset = (totalHeight + (BorderWidth * 2)) - MaxHeight;
            }
            else
            {
                _maxScrollOffset = 0f;
            }
        }

        private void SetupSubItems()
        {
            foreach (var item in _subItems)
            {
                item.Parent = this;
                item.Click += OnSubItemClick;
                item.MouseEnter += OnSubItemHover;
            }
        }

        public override void Draw()
        {
            if (!Visible) return;

            // Dibujar fondo
            Renderer.DrawRect(X, Y, Width, Height, BackgroundColor);

            // Dibujar borde
            Renderer.DrawRectOutline(new Rect(X, Y, Width, Height), BorderColor, BorderWidth);

            // Dibujar items sin márgenes
            float currentY = Y + BorderWidth - _scrollOffset;

            foreach (var item in _subItems)
            {
                if (item.Visible)
                {
                    // Verificar si está en área visible
                    if (currentY + item.Height > Y && currentY < Y + Height)
                    {
                        item.X = BorderWidth;
                        item.Y = currentY - Y;
                        item.Width = Width - (BorderWidth * 2);
                        // Mantener la altura original del item (no forzar ItemHeight)
                        // item.Height ya tiene su valor correcto

                        item.Draw();
                    }

                    // Avanzar usando la altura real del item
                    currentY += item.Height;
                }
            }
        }

        public override void Update()
        {
            base.Update();

            if (!Visible) return;

            foreach (var item in _subItems)
            {
                if (item.Visible)
                {
                    item.Update();
                }
            }
        }

        public void OnSubItemClick(object sender, Vector2 pos)
        {
            MenuItem item = (MenuItem)sender;
            OnItemSelected?.Invoke(item);

            // Obtener el nivel del item clickeado
            int currentLevel = ParentMenu.GetItemLevel(item);

            if (item.HasSubItems)
            {
                // Verificar si ya está visible
                var existingSubmenu = ParentMenu.GetActiveSubMenus().FirstOrDefault(s => s.ParentItem == item && s.Visible);
                if (existingSubmenu != null)
                {
                    // Si está visible, cerrarlo junto con todos sus submenús (niveles superiores)
                    ParentMenu.CloseSubMenusFromLevel(currentLevel + 1);

                    // Limpiar y remover el submenú actual
                    existingSubmenu.Visible = false;
                    existingSubmenu.OnItemSelected = null;
                    existingSubmenu.OnItemHovered = null;
                    foreach (var subItem in existingSubmenu.SubItems)
                    {
                        subItem.Click -= existingSubmenu.OnSubItemClick;
                        subItem.MouseEnter -= existingSubmenu.OnSubItemHover;
                    }
                    ParentMenu.ActiveSubMenus.Remove(existingSubmenu);
                }
                else
                {
                    // Mostrar nuevo submenú, cerrando solo los niveles superiores
                    // IMPORTANTE: NO cerrar el submenú actual, solo los posteriores
                    ParentMenu.CloseSubMenusFromLevel(currentLevel + 1);
                    ParentMenu.ShowSubMenu(item);
                }
            }
            else
            {
                // Item final - cerrar solo los niveles superiores al actual
                // IMPORTANTE: NO cerrar el submenú actual donde está el item
                ParentMenu.CloseSubMenusFromLevel(currentLevel + 1);
            }
        }

        public void OnSubItemHover(object sender, Vector2 pos)
        {
            MenuItem item = (MenuItem)sender;
            OnItemHovered?.Invoke(item);
        }

        public bool IsPointInside(Vector2 point)
        {
            return point.X >= X && point.X <= X + Width &&
                   point.Y >= Y && point.Y <= Y + Height;
        }

        // Método para recalcular el tamaño si los items cambian dinámicamente
        public void RecalculateSize()
        {
            CalculateSize();
        }

        // Método para obtener la posición Y de un item específico
        public float GetItemYPosition(MenuItem targetItem)
        {
            float currentY = Y + BorderWidth - _scrollOffset;

            foreach (var item in _subItems)
            {
                if (item.Visible)
                {
                    if (item == targetItem)
                    {
                        return currentY;
                    }

                    currentY += item.Height;
                }
            }

            return currentY;
        }

        // Método para obtener la altura total del contenido (para scroll)
        public float GetTotalContentHeight()
        {
            float totalHeight = 0f;

            foreach (var item in _subItems)
            {
                if (item.Visible)
                {
                    totalHeight += item.Height;
                }
            }

            return totalHeight;
        }
    }
}