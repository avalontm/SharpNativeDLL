using System;
using System.Collections.Generic;
using System.Numerics;

namespace AvalonInjectLib
{
    public static class UIFramework
    {
        private static UIControl _focusedControl;
        public static UIControl FocusedControl => _focusedControl;

        #region Estructuras Base Mejoradas

        public struct Rect
        {
            public float X, Y, Width, Height;

            public bool Contains(float px, float py) =>
                px >= X && px <= X + Width && py >= Y && py <= Y + Height;

            public Rect(float x, float y, float w, float h)
            {
                X = x; Y = y; Width = w; Height = h;
            }

            public Rect(Vector2 position, Vector2 size)
            {
                X = position.X; Y = position.Y;
                Width = size.X; Height = size.Y;
            }

            public Vector2 Position => new Vector2(X, Y);
            public Vector2 Size => new Vector2(Width, Height);
            public Vector2 Center => new Vector2(X + Width / 2, Y + Height / 2);
        }

        public struct Color
        {
            public float R, G, B, A;

            public Color(float r, float g, float b, float a = 1f)
            {
                R = r; G = g; B = b; A = a;
            }

            // Colores predefinidos
            public static readonly Color White = new(1, 1, 1);
            public static readonly Color Black = new(0, 0, 0);
            public static readonly Color Red = new(1, 0, 0);
            public static readonly Color Green = new(0, 1, 0);
            public static readonly Color Blue = new(0, 0, 1);
            public static readonly Color Transparent = new(0, 0, 0, 0);

            // Métodos útiles
            public Color WithAlpha(float alpha) => new Color(R, G, B, alpha);
        }

        public struct TextStyle
        {
            public Color Color;
            public float Size;
            public bool Bold;
            public bool Italic;
            public HorizontalAlignment HAlign;
            public VerticalAlignment VAlign;

            public static readonly TextStyle Default = new()
            {
                Color = Color.White,
                Size = 12f,
                HAlign = HorizontalAlignment.Left,
                VAlign = VerticalAlignment.Top
            };
        }

        public enum HorizontalAlignment { Left, Center, Right }
        public enum VerticalAlignment { Top, Middle, Bottom }

        #endregion

        #region Sistema de Eventos


        public class UIEventSystem
        {
            private static Vector2 _mousePosition;
            private static bool _mouseDown;
            private static UIControl _focusedControl;

            public static Vector2 MousePosition { get { return _mousePosition; } }

            public static bool IsMouseDown { get { return _mouseDown; } }


            public static void UpdateInput(Vector2 mousePos, bool mouseDown)
            {
                _mousePosition = mousePos;
                _mouseDown = mouseDown;
            }

            public static void ProcessEvents(UIControl control)
            {
                bool containsMouse = control.Bounds.Contains(_mousePosition.X, _mousePosition.Y);

                // Evento Hover
                if (containsMouse && !control.IsHovered)
                {
                    control.OnMouseEnter?.Invoke();
                    control.IsHovered = true;
                }
                else if (!containsMouse && control.IsHovered)
                {
                    control.OnMouseLeave?.Invoke();
                    control.IsHovered = false;
                }

                // Evento Click
                if (containsMouse && _mouseDown && !control.WasMouseDown)
                {
                    control.OnMouseDown?.Invoke(_mousePosition);
                    _focusedControl = control;
                }

                // Evento Release
                if (control.WasMouseDown && !_mouseDown)
                {
                    control.OnMouseUp?.Invoke(_mousePosition);
                    if (containsMouse) control.OnClick?.Invoke(_mousePosition);
                    _focusedControl = null;
                }

                control.WasMouseDown = _mouseDown;
            }
        }

        #endregion

        #region Control Base Mejorado

        public abstract class UIControl
        {
            public Rect Bounds;
            public Color BackgroundColor = Color.Transparent;
            public bool IsVisible = true;
            public bool IsEnabled = true;
            public bool IsHovered;
            public bool WasMouseDown;

            // Eventos
            public Action<Vector2> OnClick;
            public Action<Vector2> OnMouseDown;
            public Action<Vector2> OnMouseUp;
            public Action OnMouseEnter;
            public Action OnMouseLeave;

            // Métodos principales
            public abstract void Draw();

            public virtual void Update()
            {
                if (!IsVisible || !IsEnabled) return;
                UIEventSystem.ProcessEvents(this);
            }

            // Métodos útiles
            public void SetPosition(Vector2 position) => Bounds = new Rect(position, Bounds.Size);
            public void SetSize(Vector2 size) => Bounds = new Rect(Bounds.Position, size);
        }

        #endregion

        #region Controles Implementados

        public class Panel : UIControl
        {
            public List<UIControl> Children = new List<UIControl>();
            public Color BorderColor = Color.White;
            public float BorderThickness = 1f;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Fondo
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BackgroundColor);

                // Borde
                if (BorderThickness > 0)
                {
                    Renderer.DrawRectOutline(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height,
                                           BorderColor, BorderThickness);
                }

                // Hijos
                foreach (var child in Children) child.Draw();
            }

            public override void Update()
            {
                if (!IsVisible) return;

                base.Update();
                foreach (var child in Children) child.Update();
            }

            public void AddChild(UIControl control) => Children.Add(control);
            public void RemoveChild(UIControl control) => Children.Remove(control);
        }

        public class Button : UIControl
        {
            public string Text;
            public Color TextColor = Color.White;
            public Color HoverColor = new Color(0.8f, 0.8f, 0.8f);
            public Color PressColor = new Color(0.6f, 0.6f, 0.6f);
            public TextStyle TextStyle = TextStyle.Default;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Fondo
                Color bgColor = BackgroundColor;
                if (!IsEnabled) bgColor = bgColor.WithAlpha(0.5f);
                else if (WasMouseDown && IsHovered) bgColor = PressColor;
                else if (IsHovered) bgColor = HoverColor;

                Renderer.DrawRoundedRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, 5f, bgColor);

                // Texto
                var textSize = Renderer.MeasureText(Text, TextStyle.Size);
                float textX = Bounds.X + (Bounds.Width - textSize.X) / 2;
                float textY = Bounds.Y + (Bounds.Height - textSize.Y) / 2;

                Renderer.DrawText(Text, textX, textY, TextColor, TextStyle.Size);
            }
        }

        public class Label : UIControl
        {
            public string Text;
            public Color TextColor = Color.White;
            public TextStyle TextStyle = TextStyle.Default;

            public override void Draw()
            {
                if (!IsVisible) return;

                var textSize = Renderer.MeasureText(Text, TextStyle.Size);
                float textX = TextStyle.HAlign switch
                {
                    HorizontalAlignment.Center => Bounds.X + (Bounds.Width - textSize.X) / 2,
                    HorizontalAlignment.Right => Bounds.X + Bounds.Width - textSize.X,
                    _ => Bounds.X
                };

                float textY = TextStyle.VAlign switch
                {
                    VerticalAlignment.Middle => Bounds.Y + (Bounds.Height - textSize.Y) / 2,
                    VerticalAlignment.Bottom => Bounds.Y + Bounds.Height - textSize.Y,
                    _ => Bounds.Y
                };

                Renderer.DrawText(Text, textX, textY, TextColor, TextStyle.Size);
            }
        }

        public class TextBox : UIControl
        {
            public string Text = "";
            public Color TextColor = Color.White;
            public Color CaretColor = Color.White;
            public int CaretPosition;
            public float BlinkInterval = 0.5f;
            public bool IsPassword;
            public char PasswordChar = '*';

            private float _blinkTimer;
            private bool _caretVisible;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Fondo
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BackgroundColor);
                Renderer.DrawRectOutline(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height,
                                       IsHovered ? Color.White : new Color(0.7f, 0.7f, 0.7f), 1f);

                // Texto
                string displayText = IsPassword ? new string(PasswordChar, Text.Length) : Text;
                Renderer.DrawText(displayText, Bounds.X + 5, Bounds.Y + 5, TextColor);

                // Caret (solo si tiene foco)
                if (_focusedControl == this && _caretVisible)
                {
                    string textBeforeCaret = displayText.Substring(0, Math.Min(CaretPosition, displayText.Length));
                    var caretX = Bounds.X + 5 + Renderer.MeasureText(textBeforeCaret).X;
                    Renderer.DrawRect(caretX, Bounds.Y + 3, 2, Bounds.Height - 6, CaretColor);
                }
            }

            public override void Update()
            {
                base.Update();

                if (_focusedControl == this)
                {
                    _blinkTimer += Time.DeltaTime;
                    if (_blinkTimer >= BlinkInterval)
                    {
                        _blinkTimer = 0;
                        _caretVisible = !_caretVisible;
                    }

                    // Aquí iría la lógica para manejar input de texto
                }
                else
                {
                    _caretVisible = false;
                }
            }
        }

        public class Slider : UIControl
        {
            public float MinValue = 0;
            public float MaxValue = 100;
            public float Value = 50;
            public Color FillColor = new Color(0.2f, 0.6f, 1f);
            public Color EmptyColor = new Color(0.3f, 0.3f, 0.3f);
            public bool ShowValue = true;
            public string Label { get; set; }

            private bool _dragging;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Barra de fondo
                Renderer.DrawRect(Bounds.X, Bounds.Y + Bounds.Height / 2 - 5,
                                Bounds.Width, 10, EmptyColor);

                // Barra de valor
                float fillWidth = (Value - MinValue) / (MaxValue - MinValue) * Bounds.Width;
                Renderer.DrawRect(Bounds.X, Bounds.Y + Bounds.Height / 2 - 5,
                                fillWidth, 10, FillColor);

                // Control deslizante
                Renderer.DrawRect(Bounds.X + fillWidth - 5, Bounds.Y, 10, Bounds.Height,
                                _dragging ? FillColor : Color.White);

                // Texto de valor
                if (ShowValue)
                {
                    string valueText = $"{Value:F1}";
                    var textSize = Renderer.MeasureText(valueText);
                    Renderer.DrawText(valueText,
                                    Bounds.X + Bounds.Width + 5,
                                    Bounds.Y + (Bounds.Height - textSize.Y) / 2,
                                    Color.White);
                }
            }

            public override void Update()
            {
                base.Update();

                /*
                if (IsHovered && InputSystem.GetMouseButtonDown(0))
                {
                    _dragging = true;
                }

                if (_dragging)
                {
                    if (!InputSystem.GetMouseButton(0))
                    {
                        _dragging = false;
                    }
                    else
                    {
                        float mouseX = InputSystem.MousePosition.X;
                        float normalized = Math.Clamp((mouseX - Bounds.X) / Bounds.Width, 0, 1);
                        Value = MinValue + normalized * (MaxValue - MinValue);
                        OnValueChanged?.Invoke(Value);
                    }
                }*/
            }

            public Action<float> OnValueChanged;
        }

        public class Window : UIControl
        {
            public string Title;
            public List<UIControl> Controls = new List<UIControl>();
            public Color TitleBarColor = new Color(0.2f, 0.4f, 0.8f);
            public Color BorderColor = new Color(0.3f, 0.3f, 0.3f);
            public float BorderThickness = 2f;
            public bool IsDraggable = true;
            public bool ShowCloseButton = true;

            private bool _isDragging;
            private Vector2 _dragOffset;
            private Button _closeButton;

            public Window()
            {
                // Configurar el botón de cerrar
                _closeButton = new Button()
                {
                    Bounds = new Rect(0, 0, 20, 20),
                    Text = "X",
                    BackgroundColor = new Color(0.8f, 0.2f, 0.2f),
                    TextColor = Color.White,
                    OnClick = _ => IsVisible = false
                };

                Controls.Add(_closeButton);
            }

            public override void Draw()
            {
                if (!IsVisible) return;

                // Fondo de la ventana
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BackgroundColor);

                // Borde
                Renderer.DrawRectOutline(Bounds.X, Bounds.Y, Bounds.Height, Bounds.Height, BorderColor, BorderThickness);

                // Barra de título
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, 30, TitleBarColor);
                Renderer.DrawText(Title, Bounds.X + 10, Bounds.Y + 5, Color.White, 14f);

                // Botón de cerrar (si está habilitado)
                if (ShowCloseButton)
                {
                    _closeButton.Bounds = new Rect(Bounds.X + Bounds.Width - 25, Bounds.Y + 5, 20, 20);
                    _closeButton.Draw();
                }

                // Controles hijos (con transformación de coordenadas)
                var originalPosition = Bounds.Position;
                foreach (var control in Controls)
                {
                    if (control == _closeButton && !ShowCloseButton) continue;

                    // Guardar posición original
                    var originalBounds = control.Bounds;

                    // Aplicar transformación de la ventana
                    control.Bounds = new Rect(
                        originalPosition.X + originalBounds.X,
                        originalPosition.Y + originalBounds.Y,
                        originalBounds.Width,
                        originalBounds.Height
                    );

                    control.Draw();

                    // Restaurar posición original
                    control.Bounds = originalBounds;
                }
            }

            public override void Update()
            {
                if (!IsVisible) return;

                var mousePos = UIEventSystem.MousePosition;
                var mouseDown = UIEventSystem.IsMouseDown;

                // Manejar arrastre de la ventana
                if (IsDraggable)
                {
                    var titleBarRect = new Rect(Bounds.X, Bounds.Y, Bounds.Width, 30);

                    if (mouseDown && titleBarRect.Contains(mousePos.X, mousePos.Y) && !_isDragging)
                    {
                        _isDragging = true;
                        _dragOffset = new Vector2(mousePos.X - Bounds.X, mousePos.Y - Bounds.Y);
                    }

                    if (_isDragging)
                    {
                        if (mouseDown)
                        {
                            Bounds = new Rect(
                                mousePos.X - _dragOffset.X,
                                mousePos.Y - _dragOffset.Y,
                                Bounds.Width,
                                Bounds.Height
                            );
                        }
                        else
                        {
                            _isDragging = false;
                        }
                    }
                }

                // Actualizar controles hijos (con transformación de coordenadas)
                var originalPosition = Bounds.Position;
                foreach (var control in Controls)
                {
                    if (control == _closeButton && !ShowCloseButton) continue;

                    // Guardar posición original
                    var originalBounds = control.Bounds;

                    // Aplicar transformación de la ventana
                    control.Bounds = new Rect(
                        originalPosition.X + originalBounds.X,
                        originalPosition.Y + originalBounds.Y,
                        originalBounds.Width,
                        originalBounds.Height
                    );

                    control.Update();

                    // Restaurar posición original
                    control.Bounds = originalBounds;
                }
            }

            public void AddControl(UIControl control)
            {
                Controls.Add(control);
            }

            public void RemoveControl(UIControl control)
            {
                Controls.Remove(control);
            }
        }

        // Más controles: Checkbox, RadioButton, ComboBox, ListBox, ProgressBar, etc.
        #endregion


        #region Sistema de Tiempo

        public static class Time
        {
            public static float DeltaTime { get; private set; }
            private static DateTime _lastFrameTime;

            public static void Update()
            {
                var now = DateTime.Now;
                DeltaTime = (float)(now - _lastFrameTime).TotalSeconds;
                _lastFrameTime = now;
            }
        }

        #endregion
    }
}