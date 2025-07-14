namespace AvalonInjectLib.UIFramework
{
    using static AvalonInjectLib.Structs;

    public class GroupBox : UIControl
    {
        // Propiedades
        public string Title { get; set; } = "";
        public Color BorderColor { get; set; } = new Color(80, 80, 80);
        public float BorderThickness { get; set; } = 1f;
        public Thickness Padding { get; set; } = new Thickness(5);
        private List<UIControl> _children = new List<UIControl>();

        public IReadOnlyList<UIControl> Children => _children.AsReadOnly();

        public override void Draw()
        {
            if (!IsVisible) return;

            // Calcular altura del título
            float titleHeight = string.IsNullOrEmpty(Title) ? 0 : 15;

            // Dibujar fondo
            if (BackgroundColor.A > 0)
            {
                Renderer.DrawRect(
                    Bounds.X, Bounds.Y + titleHeight,
                    Bounds.Width, Bounds.Height - titleHeight,
                    BackgroundColor
                );
            }

            // Dibujar borde
            if (BorderThickness > 0)
            {
                // Borde principal
                Renderer.DrawRectOutline(
                    Bounds.X, Bounds.Y + titleHeight,
                    Bounds.Width, Bounds.Height - titleHeight,
                    BorderColor, BorderThickness
                );

                // Línea del título
                if (!string.IsNullOrEmpty(Title))
                {
                    float textWidth = Renderer.MeasureText(Title, 12).X;
                    Renderer.DrawRect(
                        Bounds.X + 10, Bounds.Y + titleHeight,
                        textWidth + 10, BorderThickness,
                        BorderColor
                    );
                }
            }

            // Dibujar título
            if (!string.IsNullOrEmpty(Title))
            {
                Renderer.DrawText(
                    Title,
                    Bounds.X + 15,
                    Bounds.Y + 2,
                    Color.White,
                    12
                );
            }

            // Dibujar hijos
            foreach (var child in _children)
            {
                if (!child.IsVisible) continue;

                // Guardar posición original
                var originalBounds = child.Bounds;

                // Aplicar transformación
                child.Bounds = new Rect(
                    Bounds.X + Padding.Left + originalBounds.X,
                    Bounds.Y + titleHeight + Padding.Top + originalBounds.Y,
                    originalBounds.Width,
                    originalBounds.Height
                );

                child.Draw();

                // Restaurar posición
                child.Bounds = originalBounds;
            }
        }

        public override void Update()
        {
            if (!IsVisible) return;

            base.Update();

            float titleHeight = string.IsNullOrEmpty(Title) ? 0 : 15;

            foreach (var child in _children)
            {
                if (!child.IsVisible) continue;

                // Guardar posición original
                var originalBounds = child.Bounds;

                // Aplicar transformación
                child.Bounds = new Rect(
                    Bounds.X + Padding.Left + originalBounds.X,
                    Bounds.Y + titleHeight + Padding.Top + originalBounds.Y,
                    originalBounds.Width,
                    originalBounds.Height
                );

                child.Update();

                // Restaurar posición
                child.Bounds = originalBounds;
            }
        }

        public void AddChild(UIControl child)
        {
            if (child != null && !_children.Contains(child))
            {
                _children.Add(child);
            }
        }

        public void RemoveChild(UIControl child)
        {
            if (child != null)
            {
                _children.Remove(child);
            }
        }

        public void ClearChildren()
        {
            _children.Clear();
        }

        public override void Measure(Vector2 availableSize)
        {
            if (!IsVisible)
            {
                Bounds = new Rect(Bounds.X, Bounds.Y, 0, 0);
                return;
            }

            // Calcular tamaño basado en los hijos
            float maxWidth = 0;
            float totalHeight = 0;
            float titleHeight = string.IsNullOrEmpty(Title) ? 0 : 15;

            foreach (var child in _children)
            {
                child.Measure(new Vector2(
                    availableSize.X - Padding.Left - Padding.Right,
                    availableSize.Y - titleHeight - Padding.Top - Padding.Bottom
                ));

                maxWidth = Math.Max(maxWidth, child.Bounds.Width);
                totalHeight += child.Bounds.Height;
            }

            // Calcular tamaño total
            float width = Padding.Left + maxWidth + Padding.Right;
            float height = titleHeight + Padding.Top + totalHeight + Padding.Bottom;

            // Ajustar por título si es necesario
            if (!string.IsNullOrEmpty(Title))
            {
                float titleWidth = Renderer.MeasureText(Title, 12).X + 30;
                width = Math.Max(width, titleWidth);
            }

            // Mantener tamaño manual si fue especificado
            if (!float.IsNaN(Bounds.Width)) width = Bounds.Width;
            if (!float.IsNaN(Bounds.Height)) height = Bounds.Height;

            Bounds = new Rect(Bounds.X, Bounds.Y, width, height);
        }
    }
}