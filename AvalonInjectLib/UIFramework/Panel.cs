using AvalonInjectLib;
using AvalonInjectLib.UIFramework;
using static AvalonInjectLib.Structs;
using System.Collections.Generic;
using System.Linq;

namespace AvalonInjectLib.UIFramework
{
    public class Panel : UIControl
    {
        private List<UIControl> _children = new List<UIControl>();
        private Color _borderColor = Color.Transparent;
        private float _borderThickness = 0f;
        private bool _clipChildren = true;

        public IReadOnlyList<UIControl> Children => _children.AsReadOnly();

        public Color BorderColor
        {
            get => _borderColor;
            set => _borderColor = value;
        }

        public float BorderThickness
        {
            get => _borderThickness;
            set => _borderThickness = Math.Max(0, value);
        }

        public bool ClipChildren
        {
            get => _clipChildren;
            set => _clipChildren = value;
        }

        public Panel()
        {
            BackgroundColor = new Color(40, 40, 40);
        }

        public override void Draw()
        {
            if (!IsVisible) return;

            // Draw background
            if (BackgroundColor.A > 0)
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BackgroundColor);

            // Draw border
            if (BorderColor.A > 0 && BorderThickness > 0)
                Renderer.DrawRectOutline(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BorderColor, BorderThickness);

            // Handle child clipping
            if (_clipChildren)
                Renderer.PushClip(GetContentRect());

            // Draw children
            foreach (var child in _children.Where(c => c.IsVisible))
            {
                var originalBounds = child.Bounds;

                // Apply panel transformation (position + padding)
                child.Bounds = new Rect(
                    Bounds.X + originalBounds.X + Padding.Left + child.Margin.Left,
                    Bounds.Y + originalBounds.Y + Padding.Top + child.Margin.Top,
                    originalBounds.Width,
                    originalBounds.Height
                );

                child.Draw();
                child.Bounds = originalBounds;
            }

            if (_clipChildren)
                Renderer.PopClip();
        }

        public override void Update()
        {
            if (!IsVisible) return;

            base.Update();

            // Update children
            foreach (var child in _children)
            {
                var originalBounds = child.Bounds;

                // Apply panel transformation (position + padding)
                child.Bounds = new Rect(
                    Bounds.X + originalBounds.X + Padding.Left + child.Margin.Left,
                    Bounds.Y + originalBounds.Y + Padding.Top + child.Margin.Top,
                    originalBounds.Width,
                    originalBounds.Height
                );

                child.Update();
                child.Bounds = originalBounds;
            }
        }

        public override void Measure(Vector2 availableSize)
        {
            var contentSize = new Vector2(
                availableSize.X - Margin.Left - Margin.Right - Padding.Left - Padding.Right,
                availableSize.Y - Margin.Top - Margin.Bottom - Padding.Top - Padding.Bottom
            );

            // Measure children first
            foreach (var child in _children)
            {
                child.Measure(contentSize);
            }

            // Calculate desired size based on children
            if (_children.Count > 0)
            {
                float maxWidth = _children.Max(c => c.DesiredSize.X);
                float maxHeight = _children.Max(c => c.DesiredSize.Y);

                DesiredSize = new Vector2(
                    maxWidth + Padding.Left + Padding.Right + Margin.Left + Margin.Right,
                    maxHeight + Padding.Top + Padding.Bottom + Margin.Top + Margin.Bottom
                );
            }
            else
            {
                DesiredSize = new Vector2(
                    availableSize.X,
                    availableSize.Y
                );
            }
        }

        public override void Arrange(Rect finalRect)
        {
            base.Arrange(finalRect);

            var contentRect = GetContentRect();

            // Arrange children
            foreach (var child in _children)
            {
                child.Arrange(new Rect(
                    contentRect.X + child.Margin.Left,
                    contentRect.Y + child.Margin.Top,
                    child.DesiredSize.X - child.Margin.Left - child.Margin.Right,
                    child.DesiredSize.Y - child.Margin.Top - child.Margin.Bottom
                ));
            }
        }

        public void AddChild(UIControl child)
        {
            if (child != null && !_children.Contains(child))
            {
                _children.Add(child);
                child.Parent = this;
                InvalidateLayout();
            }
        }

        public void RemoveChild(UIControl child)
        {
            if (child != null && _children.Contains(child))
            {
                _children.Remove(child);
                child.Parent = null;
                InvalidateLayout();
            }
        }

        public void ClearChildren()
        {
            foreach (var child in _children)
            {
                child.Parent = null;
            }
            _children.Clear();
            InvalidateLayout();
        }

        protected override void InvalidateLayout()
        {
            base.InvalidateLayout();
           
        }
    }
}