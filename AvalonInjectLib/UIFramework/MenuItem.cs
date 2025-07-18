using static AvalonInjectLib.Structs;

namespace AvalonInjectLib.UIFramework
{
    public class MenuItem : UIControl
    {
        // Propiedades visuales del header
        public Color HeaderBackgroundColor { get; set; } = Color.FromArgb(35, 35, 35);
        public Color HeaderBackgroundColorHover { get; set; } = Color.FromArgb(45, 45, 45);
        public Color HeaderBackgroundColorSelected { get; set; } = Color.FromArgb(55, 55, 55);
        public Color HeaderTextColor { get; set; } = Color.White;
        public Color HeaderTextColorDisabled { get; set; } = Color.FromArgb(128, 128, 128);
        public Color StatusOnColor { get; set; } = Color.FromArgb(0, 255, 0);
        public Color StatusOffColor { get; set; } = Color.FromArgb(255, 0, 0);
        public Color ArrowColor { get; set; } = Color.FromArgb(200, 200, 200);

        // Propiedades del header
        public float HeaderHeight { get; set; } = 28f;
        public Font HeaderFont { get; set; } = Font.GetDefaultFont();
        public bool IsExpanded { get; set; } = false;
        public bool IsEnabled { get; set; } = true;

        // Estados
        public bool IsSelected { get; private set; }
        public bool IsHovered { get; private set; }

        // Submenús
        private List<MenuItem> _subItems = new List<MenuItem>();
        public bool HasSubItems => _subItems.Count > 0;

        // Espaciado y medidas
        public float ArrowSize { get; set; } = 8f;
        public float StatusIndicatorSize { get; set; } = 8f;

        // NUEVA PROPIEDAD: Content para reemplazar el texto por defecto
        private UIControl _content;
        public UIControl Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    // Limpiar el contenido anterior
                    if (_content != null)
                    {
                        _content.Parent = null;
                    }

                    _content = value;

                    // Configurar el nuevo contenido
                    if (_content != null)
                    {
                        _content.Parent = this;
                        // REMOVIDO: UpdateContentLayout() para evitar cambio automático de altura
                    }
                }
            }
        }

        // Propiedades para el contenido personalizado
        public float ContentMarginLeft { get; set; } = 4f;
        public float ContentMarginRight { get; set; } = 4f;
        public bool UseCustomContent => Content != null;

        // Eventos
        public Action<MenuItem> OnItemSelected;
        public Action<MenuItem> OnToggleExpanded;

        public MenuItem(string name = "")
        {
            Name = name;
            IsFocusable = true;
            // REMOVIDO: UpdateHeight() para evitar establecer altura automáticamente
            // La altura ahora debe ser establecida manualmente por el usuario
        }

        // MÉTODO REMOVIDO: UpdateHeight() ya no se llama automáticamente
        // Si necesitas calcular una altura sugerida, puedes usar CalculateSuggestedHeight()
        public float CalculateSuggestedHeight()
        {
            // Si hay contenido personalizado, sugiere Content.Height
            if (UseCustomContent && Content != null)
            {
                return Content.Height;
            }
            else
            {
                // Si no hay contenido, sugiere HeaderHeight
                return HeaderHeight;
            }
        }

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();

            if (UseCustomContent)
            {
                // Solo dibujar el contenido personalizado (sin header)
                DrawCustomContent(absPos.X, absPos.Y);
            }
            else
            {
                // Dibujar solo el header con el texto
                DrawHeader(absPos);
            }
        }

        private void DrawHeader(Vector2 absPos)
        {
            // Determinar color de fondo del header
            Color bgColor = HeaderBackgroundColor;
            if (IsSelected)
                bgColor = HeaderBackgroundColorSelected;
            else if (IsHovered)
                bgColor = HeaderBackgroundColorHover;

            // Dibujar fondo del header
            Renderer.DrawRect(absPos.X, absPos.Y, Width, HeaderHeight, bgColor);

            // Dibujar texto del header
            DrawDefaultText(absPos.X, absPos.Y);

            // Dibujar flecha de expansión si hay subitems
            if (HasSubItems)
            {
                float centerY = absPos.Y + (HeaderHeight / 2);
                float arrowX = absPos.X + Width - ArrowSize;
                var arrowPos = new Vector2(arrowX, centerY);
                var arrowColor = IsEnabled ? ArrowColor : HeaderTextColorDisabled;
                DrawArrow(arrowPos, arrowColor);
            }
        }

        private void DrawCustomContent(float startX, float startY)
        {
            if (Content == null) return;

            // Calcular el área disponible para el contenido
            float availableWidth = Math.Max(0, Width - ContentMarginLeft - ContentMarginRight);
            float availableHeight = Height; // Altura total del MenuItem

            // Calcular posición centrada verticalmente
            float contentHeight = Content.Height;
            float centeredY = (availableHeight - contentHeight) / 2;

            // Posicionar el contenido centrado
            Content.X = startX - GetAbsolutePosition().X + ContentMarginLeft;
            Content.Y = startY - GetAbsolutePosition().Y + centeredY;
            Content.Width = availableWidth;

            // Dibujar el contenido
            Content.Draw();
        }

        private void DrawDefaultText(float startX, float headerY)
        {
            if (string.IsNullOrEmpty(Name) || HeaderFont == null || !HeaderFont.IsReady)
                return;

            var textColor = IsEnabled ? HeaderTextColor : HeaderTextColorDisabled;
            var textPos = new Vector2(
                startX + 5,
                CalculateVerticalCenterPositionForText(headerY, HeaderHeight, HeaderFont, Name)
            );
            Renderer.DrawText(Name, textPos, textColor, HeaderFont);
        }

        // MÉTODO REMOVIDO: UpdateContentLayout() ya no cambia la altura automáticamente

        private void DrawArrow(Vector2 position, Color color)
        {
            if (HasSubItems)
            {
                float triangleSize = ArrowSize * 0.8f;
                Vector2[] points;

                // Flecha hacia la derecha ▶
                points = new Vector2[]
                {
                    new Vector2(position.X + triangleSize / 2, position.Y),               // Vértice derecho
                    new Vector2(position.X - triangleSize / 2, position.Y - triangleSize / 2), // Superior izquierdo
                    new Vector2(position.X - triangleSize / 2, position.Y + triangleSize / 2)  // Inferior izquierdo
                };

                Renderer.DrawTriangle(points[0], points[1], points[2], color);
            }

        }

        public override void Update()
        {
            base.Update();

            // Actualizar contenido personalizado
            if (UseCustomContent && Content != null)
            {
                Content.Update();
            }

        }

        // Cálculo de altura total (renombrado para claridad)
        public float CalculateTotalHeight()
        {
            return UseCustomContent ? Content.Height : HeaderHeight;
        }

        // Métodos de manejo de submenús
        public void AddSubItem(MenuItem item)
        {
            if (item == null) return;

            item.Parent = this;
            _subItems.Add(item);
        }

        public void RemoveSubItem(MenuItem item)
        {
            if (item == null) return;

            _subItems.Remove(item);
            item.Parent = null;
        }

        public void ClearSubItems()
        {
            foreach (var item in _subItems)
            {
                item.Parent = null;
            }
            _subItems.Clear();
        }

        public List<MenuItem> GetSubItems()
        {
            return _subItems;
        }

        // Métodos de estado
        public void SetSelected(bool selected)
        {
            if (IsSelected != selected)
            {
                IsSelected = selected;
                if (selected)
                {
                    OnItemSelected?.Invoke(this);
                }
            }
        }

        public void SetHovered(bool hovered)
        {
            if (IsHovered != hovered)
            {
                IsHovered = hovered;
            }
        }

        // Override de eventos del UIControl
        protected override void OnClick(object sender, Vector2 pos)
        {
            if (!IsEnabled) return;
        }

        protected override void OnMouseEnter(object sender, Vector2 pos)
        {
            SetHovered(true);
        }

        protected override void OnMouseLeave(object sender, Vector2 pos)
        {
            SetHovered(false);
        }

        // Método específico para centrar texto por defecto (sin paddings adicionales)
        private float CalculateVerticalCenterPositionForText(float elementY, float elementHeight, Font font, string text)
        {
            if (font == null || !font.IsReady || string.IsNullOrEmpty(text))
            {
                return elementY + (elementHeight / 2);
            }

            var textSize = font.MeasureText(text);
            float textHeight = textSize.Y;

            // Centrar verticalmente el texto directamente en el header
            return elementY + (elementHeight - textHeight) / 2;
        }

    }
}