using static AvalonInjectLib.Structs;
using System;
using System.Collections.Generic;

namespace AvalonInjectLib.UIFramework
{
    public class Panel : UIContainer
    {
        // Constantes para el diseño
        private const float DEFAULT_BORDER_WIDTH = 1f;

        // Propiedades de borde
        public bool ShowBorder { get; set; } = false;
        public Color BorderColor { get; set; } = Color.FromArgb(100, 100, 100);
        public float BorderWidth { get; set; } = DEFAULT_BORDER_WIDTH;

        // Propiedades de padding interno
        public float PaddingLeft { get; set; } = 0f;
        public float PaddingTop { get; set; } = 0f;
        public float PaddingRight { get; set; } = 0f;
        public float PaddingBottom { get; set; } = 0f;

        // Propiedades de scroll (para futura implementación)
        public bool AutoScroll { get; set; } = false;
        public bool ShowScrollBars { get; set; } = false;

        // Propiedades de diseño
        public BorderStyle BorderStyle { get; set; } = BorderStyle.Solid;

        // Constructor
        public Panel()
        {
            BackColor = Color.FromArgb(64, 64, 64);
            Width = 200f;
            Height = 150f;
        }

        // Métodos de conveniencia para establecer padding
        public void SetPadding(float all)
        {
            PaddingLeft = PaddingTop = PaddingRight = PaddingBottom = all;
        }

        public void SetPadding(float horizontal, float vertical)
        {
            PaddingLeft = PaddingRight = horizontal;
            PaddingTop = PaddingBottom = vertical;
        }

        public void SetPadding(float left, float top, float right, float bottom)
        {
            PaddingLeft = left;
            PaddingTop = top;
            PaddingRight = right;
            PaddingBottom = bottom;
        }

        // Propiedades calculadas
        public float ContentWidth => Math.Max(0, Width - PaddingLeft - PaddingRight - (ShowBorder ? BorderWidth * 2 : 0));
        public float ContentHeight => Math.Max(0, Height - PaddingTop - PaddingBottom - (ShowBorder ? BorderWidth * 2 : 0));

        public Vector2 ContentPosition
        {
            get
            {
                float offsetX = PaddingLeft + (ShowBorder ? BorderWidth : 0);
                float offsetY = PaddingTop + (ShowBorder ? BorderWidth : 0);
                return new Vector2(X + offsetX, Y + offsetY);
            }
        }

        // Método para obtener el área de contenido disponible
        public Rect GetContentArea()
        {
            var contentPos = ContentPosition;
            return new Rect(contentPos.X, contentPos.Y, ContentWidth, ContentHeight);
        }

        // Método para obtener el área de contenido en coordenadas absolutas
        public Rect GetAbsoluteContentArea()
        {
            var absPos = GetAbsolutePosition();
            float offsetX = PaddingLeft + (ShowBorder ? BorderWidth : 0);
            float offsetY = PaddingTop + (ShowBorder ? BorderWidth : 0);

            return new Rect(
                absPos.X + offsetX,
                absPos.Y + offsetY,
                ContentWidth,
                ContentHeight
            );
        }

        // Override del método AddChild para posicionar automáticamente dentro del área de contenido
        public override void AddChild(UIControl child)
        {
            if (child == null) return;

            base.AddChild(child);

            // Opcional: Ajustar posición del hijo al área de contenido
            // (comentado porque a veces querrás posicionar manualmente)
            /*
            var contentPos = ContentPosition;
            child.X += contentPos.X;
            child.Y += contentPos.Y;
            */
        }

        // Método para auto-posicionar un control dentro del área de contenido
        public void AddChildToContent(UIControl child, float x = 0, float y = 0)
        {
            if (child == null) return;

            var contentPos = ContentPosition;
            child.X = x;
            child.Y = y;

            AddChild(child);
        }

        // Método para centrar un control dentro del panel
        public void CenterChild(UIControl child)
        {
            if (child == null || !children.Contains(child)) return;

            child.X = (ContentWidth - child.Width) / 2;
            child.Y = (ContentHeight - child.Height) / 2;
        }

        // Método para organizar controles hijos en layout automático
        public void ArrangeChildrenVertically(float spacing = 5f)
        {
            float currentY = 0;

            foreach (var child in children)
            {
                if (!child.Visible) continue;

                child.Y = currentY;
                currentY += child.Height + spacing;
            }
        }

        public void ArrangeChildrenHorizontally(float spacing = 5f)
        {
            float currentX = 0;

            foreach (var child in children)
            {
                if (!child.Visible) continue;

                child.X = currentX;
                currentX += child.Width + spacing;
            }
        }

        // Método para organizar en grid
        public void ArrangeChildrenInGrid(int columns, float spacingX = 5f, float spacingY = 5f)
        {
            if (columns <= 0) return;

            int row = 0;
            int col = 0;

            foreach (var child in children)
            {
                if (!child.Visible) continue;

                child.X = col * (child.Width + spacingX);
                child.Y = row * (child.Height + spacingY);

                col++;
                if (col >= columns)
                {
                    col = 0;
                    row++;
                }
            }
        }

        // Override del método Draw
        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();

            // Dibujar fondo del panel
            Renderer.DrawRect(new Rect(absPos.X, absPos.Y, Width, Height), BackColor);

            // Dibujar borde si está habilitado
            if (ShowBorder)
            {
                DrawBorder(absPos);
            }

            // Dibujar todos los controles hijos
            foreach (var child in children)
            {
                if (child.Visible)
                {
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
        }

        private void DrawBorder(Vector2 absPos)
        {
            var borderRect = new Rect(absPos.X, absPos.Y, Width, Height);

            switch (BorderStyle)
            {
                case BorderStyle.Solid:
                    Renderer.DrawRectOutline(borderRect, BorderColor, BorderWidth);
                    break;

                case BorderStyle.Dashed:
                    // Implementación básica de borde punteado
                    DrawDashedBorder(borderRect);
                    break;

                case BorderStyle.Dotted:
                    // Implementación básica de borde puntos
                    DrawDottedBorder(borderRect);
                    break;

                case BorderStyle.Double:
                    // Borde doble
                    DrawDoubleBorder(borderRect);
                    break;
            }
        }

        private void DrawDashedBorder(Rect rect)
        {
            // Implementación básica - puedes mejorarla según tu sistema de renderizado
            float dashLength = 5f;
            float gapLength = 3f;

            // Top border
            DrawDashedLine(rect.X, rect.Y, rect.X + rect.Width, rect.Y, dashLength, gapLength);
            // Bottom border
            DrawDashedLine(rect.X, rect.Y + rect.Height, rect.X + rect.Width, rect.Y + rect.Height, dashLength, gapLength);
            // Left border
            DrawDashedLine(rect.X, rect.Y, rect.X, rect.Y + rect.Height, dashLength, gapLength);
            // Right border
            DrawDashedLine(rect.X + rect.Width, rect.Y, rect.X + rect.Width, rect.Y + rect.Height, dashLength, gapLength);
        }

        private void DrawDottedBorder(Rect rect)
        {
            // Implementación básica de puntos
            float dotSize = 2f;
            float spacing = 4f;

            // Similar a dashed pero con puntos más pequeños
            DrawDashedLine(rect.X, rect.Y, rect.X + rect.Width, rect.Y, dotSize, spacing);
            DrawDashedLine(rect.X, rect.Y + rect.Height, rect.X + rect.Width, rect.Y + rect.Height, dotSize, spacing);
            DrawDashedLine(rect.X, rect.Y, rect.X, rect.Y + rect.Height, dotSize, spacing);
            DrawDashedLine(rect.X + rect.Width, rect.Y, rect.X + rect.Width, rect.Y + rect.Height, dotSize, spacing);
        }

        private void DrawDoubleBorder(Rect rect)
        {
            // Borde exterior
            Renderer.DrawRectOutline(rect, BorderColor, BorderWidth);

            // Borde interior
            float innerOffset = BorderWidth + 2f;
            var innerRect = new Rect(
                rect.X + innerOffset,
                rect.Y + innerOffset,
                rect.Width - (2 * innerOffset),
                rect.Height - (2 * innerOffset)
            );

            if (innerRect.Width > 0 && innerRect.Height > 0)
            {
                Renderer.DrawRectOutline(innerRect, BorderColor, BorderWidth);
            }
        }

        private void DrawDashedLine(float x1, float y1, float x2, float y2, float dashLength, float gapLength)
        {
            float totalLength = (float)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
            float unitX = (x2 - x1) / totalLength;
            float unitY = (y2 - y1) / totalLength;

            float currentLength = 0;
            bool drawing = true;

            while (currentLength < totalLength)
            {
                float segmentLength = drawing ? dashLength : gapLength;
                float endLength = Math.Min(currentLength + segmentLength, totalLength);

                if (drawing)
                {
                    float startX = x1 + unitX * currentLength;
                    float startY = y1 + unitY * currentLength;
                    float endX = x1 + unitX * endLength;
                    float endY = y1 + unitY * endLength;

                    // Dibujar segmento (usando rect muy delgado como línea)
                    Renderer.DrawRect(new Rect(startX, startY, endX - startX, BorderWidth), BorderColor);
                }

                currentLength = endLength;
                drawing = !drawing;
            }
        }

        // Método para limpiar todos los controles hijos
        public void ClearChildren()
        {
            foreach (var child in children.ToArray())
            {
                RemoveChild(child);
            }
        }

        // Método para encontrar controles por tipo
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

        // Método para verificar si un punto está dentro del área de contenido
        public bool ContainsInContent(Vector2 point)
        {
            return GetAbsoluteContentArea().Contains(point);
        }

        // Override del método Contains para considerar el padding
        public override bool Contains(Vector2 point)
        {
            return GetAbsoluteBounds().Contains(point);
        }

        // Método para obtener el control hijo bajo el mouse dentro del área de contenido
        public UIControl? GetControlUnderMouse(Vector2 mousePos)
        {
            if (!Visible || !Enabled) return null;

            // Verificar si el mouse está dentro del área de contenido
            if (!GetAbsoluteContentArea().Contains(mousePos))
            {
                // Si está fuera del área de contenido pero dentro del panel, retornar el panel
                return Contains(mousePos) ? this : null;
            }

            // Buscar en los hijos
            for (int i = children.Count - 1; i >= 0; i--)
            {
                var child = children[i];
                if (!child.Visible || !child.Enabled) continue;

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

            return this;
        }
    }

    // Enum para estilos de borde
    public enum BorderStyle
    {
        None,
        Solid,
        Dashed,
        Dotted,
        Double
    }
}