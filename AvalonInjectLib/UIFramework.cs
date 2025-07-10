using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{

    public static class UIFramework
    {
        // ================= ESTRUCTURAS BASE =================
        public struct Rect
        {
            public float X, Y, Width, Height;
            public bool Contains(float px, float py) =>
                px >= X && px <= X + Width && py >= Y && py <= Y + Height;
        }

        public struct Color
        {
            public float R, G, B, A;
            public static readonly Color White = new(1, 1, 1, 1);
            public static readonly Color Red = new(1, 0, 0, 1);

            public Color(float r, float g, float b, float a)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }
        }

        // ================= CONTROL BASE =================
        public abstract class UIControl
        {
            public Rect Bounds;
            public Color BackgroundColor;
            public bool IsVisible = true;
            public bool IsHovered;

            public abstract void Draw();
            public virtual void Update(float mouseX, float mouseY, bool mouseDown)
            {
                IsHovered = Bounds.Contains(mouseX, mouseY);
            }
        }

        // ================= CONTROLES IMPLEMENTADOS =================
        public class Button : UIControl
        {
            public string Text;
            public Action OnClick;
            public Color TextColor = Color.White;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Fondo
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height,
                    IsHovered ? new Color(0.4f, 0.4f, 0.4f, 1) : BackgroundColor);

                // Texto (centrado)
                float textWidth = Text.Length * 8; // Aproximación
                Renderer.DrawText(Text,
                    Bounds.X + (Bounds.Width - textWidth) / 2,
                    Bounds.Y + Bounds.Height / 3,
                    TextColor);
            }

            public override void Update(float mouseX, float mouseY, bool mouseDown)
            {
                base.Update(mouseX, mouseY, mouseDown);
                if (IsHovered && mouseDown) OnClick?.Invoke();
            }
        }

        public class Checkbox : UIControl
        {
            public string Label;
            public bool IsChecked;
            public Action<bool> OnValueChanged;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Caja
                Renderer.DrawRect(Bounds.X, Bounds.Y, 15, 15,
                    IsChecked ? new Color(0, 1, 0, 1) : new Color(0.3f, 0.3f, 0.3f, 1));

                // Texto
                Renderer.DrawText(Label, Bounds.X + 20, Bounds.Y, Color.White);
            }

            public override void Update(float mouseX, float mouseY, bool mouseDown)
            {
                base.Update(mouseX, mouseY, mouseDown);
                if (IsHovered && mouseDown)
                {
                    IsChecked = !IsChecked;
                    OnValueChanged?.Invoke(IsChecked);
                }
            }
        }

        public class Slider : UIControl
        {
            public string Label;
            public float Min, Max, Value;
            private bool _isDragging;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Barra
                Renderer.DrawRect(Bounds.X, Bounds.Y + 10, Bounds.Width, 5, new Color(0.3f, 0.3f, 0.3f, 1));

                // Indicador
                float pos = Bounds.X + (Value - Min) / (Max - Min) * Bounds.Width;
                Renderer.DrawRect(pos - 5, Bounds.Y, 10, 20, Color.Red);

                // Texto
                Renderer.DrawText($"{Label}: {Value:F1}", Bounds.X, Bounds.Y + 25, Color.White);
            }

            public override void Update(float mouseX, float mouseY, bool mouseDown)
            {
                base.Update(mouseX, mouseY, mouseDown);

                if (mouseDown && IsHovered) _isDragging = true;
                if (!mouseDown) _isDragging = false;

                if (_isDragging)
                {
                    Value = Math.Clamp(Min + (mouseX - Bounds.X) / Bounds.Width * (Max - Min), Min, Max);
                }
            }
        }

        public class Window : UIControl
        {
            public string Title;
            public UIControl[] Controls;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Fondo
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BackgroundColor);

                // Barra de título
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, 25, new Color(0.2f, 0.4f, 0.8f, 1));
                Renderer.DrawText(Title, Bounds.X + 10, Bounds.Y + 5, Color.White);

                // Controles
                foreach (var control in Controls) control.Draw();
            }

            public override void Update(float mouseX, float mouseY, bool mouseDown)
            {
                if (!IsVisible) return;

                // Convertir a coordenadas relativas a la ventana
                float relX = mouseX - Bounds.X;
                float relY = mouseY - Bounds.Y;

                foreach (var control in Controls)
                {
                    control.Update(relX, relY, mouseDown);
                }
            }
        }
    }
}
