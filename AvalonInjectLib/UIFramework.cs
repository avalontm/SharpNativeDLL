using System;
using System.Collections.Generic;
using System.Linq;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static class UIFramework
    {
        private static UIControl _focusedControl;
        private static List<UIControl> _modalControls = new List<UIControl>();
        public static UIControl FocusedControl => _focusedControl;

        #region Estructuras Base Mejoradas

        public struct Color
        {
            public byte R, G, B, A;

            public Color(byte r, byte g, byte b, byte a = 255)
            {
                R = r; G = g; B = b; A = a;
            }

            // Colores predefinidos
            public static readonly Color White = new(255, 255, 255);
            public static readonly Color Black = new(0, 0, 0);
            public static readonly Color Red = new(255, 0, 0);
            public static readonly Color Green = new(0, 255, 0);
            public static readonly Color Blue = new(0, 0, 255);
            public static readonly Color Yellow = new(255, 255, 0);
            public static readonly Color Magenta = new(255, 0, 255);
            public static readonly Color Cyan = new(0, 255, 255);
            public static readonly Color Transparent = new(0, 0, 0, 0);
            public static readonly Color Gray = new(128, 128, 128);
            public static readonly Color LightGray = new(192, 192, 192);
            public static readonly Color DarkGray = new(64, 64, 64);
            public static readonly Color Orange = new(255, 165, 0);
            public static readonly Color Purple = new(128, 0, 128);

            public override string ToString() => $"R:{R} G:{G} B:{B} A:{A}";
            public Color WithAlpha(byte a) => new Color(R, G, B, a);
            public Color Lerp(Color other, float t)
            {
                t = Math.Clamp(t, 0, 1);
                return new Color(
                    (byte)(R + (other.R - R) * t),
                    (byte)(G + (other.G - G) * t),
                    (byte)(B + (other.B - B) * t),
                    (byte)(A + (other.A - A) * t)
                );
            }
        }

        public struct TextStyle
        {
            public Color Color;
            public int Size;
            public bool Bold;
            public bool Italic;
            public HorizontalAlignment HAlign;
            public VerticalAlignment VAlign;

            public static readonly TextStyle Default = new()
            {
                Color = Color.White,
                Size = 12,
                HAlign = HorizontalAlignment.Left,
                VAlign = VerticalAlignment.Top
            };

            public static readonly TextStyle Title = new()
            {
                Color = Color.White,
                Size = 18,
                Bold = true,
                HAlign = HorizontalAlignment.Center,
                VAlign = VerticalAlignment.Top
            };

            public static readonly TextStyle Button = new()
            {
                Color = Color.White,
                Size = 14,
                HAlign = HorizontalAlignment.Center,
                VAlign = VerticalAlignment.Middle
            };
        }

        public enum HorizontalAlignment { Left, Center, Right }
        public enum VerticalAlignment { Top, Middle, Bottom }

        #endregion

        #region Sistema de Eventos Mejorado

        public class UIEventSystem
        {
            private static Vector2 _mousePosition;
            private static Vector2 _lastMousePosition;
            private static bool _mouseDown;
            private static bool _lastMouseDown;
            private static string _inputText = "";
            private static Dictionary<int, bool> _keys = new Dictionary<int, bool>();

            public static Vector2 MousePosition => _mousePosition;
            public static Vector2 MouseDelta => new Vector2(_mousePosition.X - _lastMousePosition.X, _mousePosition.Y - _lastMousePosition.Y);
            public static bool IsMouseDown => _mouseDown;
            public static bool IsMousePressed => _mouseDown && !_lastMouseDown;
            public static bool IsMouseReleased => !_mouseDown && _lastMouseDown;
            public static string InputText => _inputText;

            public static void UpdateInput(Vector2 mousePos, bool mouseDown, string inputText = "", Dictionary<int, bool> keys = null)
            {
                _lastMousePosition = _mousePosition;
                _lastMouseDown = _mouseDown;
                _mousePosition = mousePos;
                _mouseDown = mouseDown;
                _inputText = inputText ?? "";
                _keys = keys ?? new Dictionary<int, bool>();
            }

            public static bool IsKeyPressed(int keyCode)
            {
                return _keys.ContainsKey(keyCode) && _keys[keyCode];
            }

            public static void ProcessEvents(UIControl control)
            {
                if (!control.IsVisible || !control.IsEnabled) return;

                bool containsMouse = control.Bounds.Contains(_mousePosition.X, _mousePosition.Y);

                // Eventos de mouse
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

                if (containsMouse && IsMousePressed)
                {
                    control.OnMouseDown?.Invoke(_mousePosition);
                    _focusedControl = control;
                }

                if (control.WasMouseDown && IsMouseReleased)
                {
                    control.OnMouseUp?.Invoke(_mousePosition);
                    if (containsMouse) control.OnClick?.Invoke(_mousePosition);
                }

                control.WasMouseDown = _mouseDown && containsMouse;
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
            public string Tag = "";
            public object UserData;

            // Eventos
            public Action<Vector2> OnClick;
            public Action<Vector2> OnMouseDown;
            public Action<Vector2> OnMouseUp;
            public Action OnMouseEnter;
            public Action OnMouseLeave;
            public Action OnFocusGained;
            public Action OnFocusLost;

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
            public void SetBounds(Rect bounds) => Bounds = bounds;
            public void SetBounds(float x, float y, float width, float height) => Bounds = new Rect(x, y, width, height);

            public virtual void Focus()
            {
                if (_focusedControl != this)
                {
                    _focusedControl?.OnFocusLost?.Invoke();
                    _focusedControl = this;
                    OnFocusGained?.Invoke();
                }
            }

            public bool HasFocus => _focusedControl == this;
        }

        #endregion

        #region Controles Implementados

        public class Panel : UIControl
        {
            public List<UIControl> Children = new List<UIControl>();
            public Color BorderColor = Color.White;
            public float BorderThickness = 1f;
            public bool AutoSize = false;
            public Vector2 Padding = new Vector2(5, 5);

            public override void Draw()
            {
                if (!IsVisible) return;

                // Auto-resize si está habilitado
                if (AutoSize && Children.Count > 0)
                {
                    float maxX = 0, maxY = 0;
                    foreach (var child in Children)
                    {
                        maxX = Math.Max(maxX, child.Bounds.X + child.Bounds.Width);
                        maxY = Math.Max(maxY, child.Bounds.Y + child.Bounds.Height);
                    }
                    Bounds = new Rect(Bounds.Position, new Vector2(maxX + Padding.X, maxY + Padding.Y));
                }

                // Fondo
                if (BackgroundColor.A > 0)
                    Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BackgroundColor);

                // Borde
                if (BorderThickness > 0)
                    Renderer.DrawRectOutline(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BorderColor, BorderThickness);

                // Hijos
                foreach (var child in Children.Where(c => c.IsVisible))
                    child.Draw();
            }

            public override void Update()
            {
                if (!IsVisible) return;
                base.Update();
                foreach (var child in Children) child.Update();
            }

            public void AddChild(UIControl control) => Children.Add(control);
            public void RemoveChild(UIControl control) => Children.Remove(control);
            public void ClearChildren() => Children.Clear();
        }

        public class Button : UIControl
        {
            public string Text = "";
            public Color TextColor = Color.White;
            public Color HoverColor = new Color(204, 204, 204);
            public Color PressColor = new Color(153, 153, 153);
            public Color DisabledColor = new Color(100, 100, 100);
            public TextStyle TextStyle = TextStyle.Button;
            public float CornerRadius = 5f;
            public bool UseGradient = false;
            public Color GradientColor = Color.LightGray;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Determinar color de fondo
                Color bgColor = BackgroundColor;
                if (!IsEnabled) bgColor = DisabledColor;
                else if (WasMouseDown && IsHovered) bgColor = PressColor;
                else if (IsHovered) bgColor = HoverColor;

                // Dibujar fondo
                if (CornerRadius > 0)
                    Renderer.DrawRoundedRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, CornerRadius, bgColor);
                else
                    Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, bgColor);

                // Texto
                if (!string.IsNullOrEmpty(Text))
                {
                    var textSize = Renderer.MeasureText(Text, TextStyle.Size);
                    float textX = Bounds.X + (Bounds.Width - textSize.X) / 2;
                    float textY = Bounds.Y + (Bounds.Height - textSize.Y) / 2;

                    Color finalTextColor = IsEnabled ? TextColor : TextColor.WithAlpha(127);
                    Renderer.DrawText(Text, textX, textY, finalTextColor, TextStyle.Size);
                }
            }
        }

        public class Label : UIControl
        {
            public string Text = "";
            public Color TextColor = Color.White;
            public TextStyle TextStyle = TextStyle.Default;
            public bool WordWrap = false;
            public bool AutoSize = true;

            public override void Draw()
            {
                if (!IsVisible || string.IsNullOrEmpty(Text)) return;

                var textSize = Renderer.MeasureText(Text, TextStyle.Size);

                // Auto-size si está habilitado
                if (AutoSize)
                    Bounds = new Rect(Bounds.Position, textSize);

                // Calcular posición del texto
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

                // Dibujar fondo si es necesario
                if (BackgroundColor.A > 0)
                    Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BackgroundColor);

                // Dibujar texto
                Renderer.DrawText(Text, textX, textY, TextColor, TextStyle.Size);
            }
        }

        public class TextBox : UIControl
        {
            public string Text = "";
            public Color TextColor = Color.White;
            public Color CaretColor = Color.White;
            public Color SelectionColor = new Color(0, 120, 215, 128);
            public int CaretPosition = 0;
            public int SelectionStart = 0;
            public int SelectionEnd = 0;
            public float BlinkInterval = 0.5f;
            public bool IsPassword = false;
            public char PasswordChar = '*';
            public bool IsReadOnly = false;
            public int MaxLength = 0;
            public string PlaceholderText = "";
            public Color PlaceholderColor = Color.Gray;

            private float _blinkTimer;
            private bool _caretVisible = true;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Fondo
                Color bgColor = IsEnabled ? BackgroundColor : BackgroundColor.WithAlpha(127);
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, bgColor);

                // Borde
                Color borderColor = HasFocus ? Color.Blue : (IsHovered ? Color.White : Color.Gray);
                Renderer.DrawRectOutline(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, borderColor, 1f);

                // Texto o placeholder
                string displayText = "";
                Color textColor = TextColor;

                if (string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(PlaceholderText))
                {
                    displayText = PlaceholderText;
                    textColor = PlaceholderColor;
                }
                else
                {
                    displayText = IsPassword ? new string(PasswordChar, Text.Length) : Text;
                }

                if (!string.IsNullOrEmpty(displayText))
                {
                    Renderer.DrawText(displayText, Bounds.X + 5, Bounds.Y + 5, textColor, 12);
                }

                // Selección
                if (HasFocus && SelectionStart != SelectionEnd)
                {
                    int start = Math.Min(SelectionStart, SelectionEnd);
                    int end = Math.Max(SelectionStart, SelectionEnd);
                    string beforeSelection = displayText.Substring(0, start);
                    string selection = displayText.Substring(start, end - start);

                    float selectionX = Bounds.X + 5 + Renderer.MeasureText(beforeSelection, 12).X;
                    float selectionWidth = Renderer.MeasureText(selection, 12).X;

                    Renderer.DrawRect(selectionX, Bounds.Y + 3, selectionWidth, Bounds.Height - 6, SelectionColor);
                }

                // Caret
                if (HasFocus && _caretVisible && !IsReadOnly)
                {
                    string textBeforeCaret = displayText.Substring(0, Math.Min(CaretPosition, displayText.Length));
                    float caretX = Bounds.X + 5 + Renderer.MeasureText(textBeforeCaret, 12).X;
                    Renderer.DrawRect(caretX, Bounds.Y + 3, 2, Bounds.Height - 6, CaretColor);
                }
            }

            public override void Update()
            {
                base.Update();

                if (HasFocus)
                {
                    _blinkTimer += Time.DeltaTime;
                    if (_blinkTimer >= BlinkInterval)
                    {
                        _blinkTimer = 0;
                        _caretVisible = !_caretVisible;
                    }

                    // Procesar input de texto
                    if (!IsReadOnly)
                    {
                        ProcessTextInput();
                    }
                }
                else
                {
                    _caretVisible = false;
                }
            }

            private void ProcessTextInput()
            {
                string inputText = UIEventSystem.InputText;
                foreach (char c in inputText)
                {
                    if (char.IsControl(c))
                    {
                        // Manejar teclas de control
                        switch (c)
                        {
                            case '\b': // Backspace
                                if (CaretPosition > 0)
                                {
                                    Text = Text.Remove(CaretPosition - 1, 1);
                                    CaretPosition--;
                                }
                                break;
                            case '\r': // Enter
                            case '\n':
                                // Trigger enter event if needed
                                break;
                        }
                    }
                    else
                    {
                        // Insertar caracter
                        if (MaxLength == 0 || Text.Length < MaxLength)
                        {
                            Text = Text.Insert(CaretPosition, c.ToString());
                            CaretPosition++;
                        }
                    }
                }
            }

            public Action<string> OnTextChanged;
            public Action OnEnterPressed;
        }

        public class Checkbox : UIControl
        {
            public bool IsChecked = false;
            public string Text = "";
            public Color TextColor = Color.White;
            public Color CheckColor = Color.Green;
            public Color BoxColor = Color.White;
            public float BoxSize = 16f;
            public float TextSpacing = 5f;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Caja del checkbox
                float boxY = Bounds.Y + (Bounds.Height - BoxSize) / 2;
                Color boxBgColor = IsHovered ? Color.LightGray : Color.DarkGray;

                Renderer.DrawRect(Bounds.X, boxY, BoxSize, BoxSize, boxBgColor);
                Renderer.DrawRectOutline(Bounds.X, boxY, BoxSize, BoxSize, BoxColor, 1f);

                // Marca de verificación
                if (IsChecked)
                {
                    Renderer.DrawRect(Bounds.X + 3, boxY + 3, BoxSize - 6, BoxSize - 6, CheckColor);
                }

                // Texto
                if (!string.IsNullOrEmpty(Text))
                {
                    float textX = Bounds.X + BoxSize + TextSpacing;
                    float textY = Bounds.Y + (Bounds.Height - 12) / 2;
                    Renderer.DrawText(Text, textX, textY, TextColor, 12);
                }
            }

            public override void Update()
            {
                base.Update();

                // Manejar click
                if (OnClick == null)
                {
                    OnClick = _ => {
                        IsChecked = !IsChecked;
                        OnCheckedChanged?.Invoke(IsChecked);
                    };
                }
            }

            public Action<bool> OnCheckedChanged;
        }

        public class RadioButton : UIControl
        {
            public bool IsChecked = false;
            public string Text = "";
            public Color TextColor = Color.White;
            public Color CheckColor = Color.Green;
            public Color CircleColor = Color.White;
            public float CircleSize = 16f;
            public float TextSpacing = 5f;
            public string GroupName = "";

            private static Dictionary<string, List<RadioButton>> _groups = new Dictionary<string, List<RadioButton>>();

            public RadioButton()
            {
                OnClick = _ => SetChecked(true);
            }

            public override void Draw()
            {
                if (!IsVisible) return;

                // Círculo del radio button
                float circleY = Bounds.Y + (Bounds.Height - CircleSize) / 2;
                float centerX = Bounds.X + CircleSize / 2;
                float centerY = circleY + CircleSize / 2;

                Color circleBgColor = IsHovered ? Color.LightGray : Color.DarkGray;

                // Simular círculo con rect redondeado
                Renderer.DrawRoundedRect(Bounds.X, circleY, CircleSize, CircleSize, CircleSize / 2, circleBgColor);
                Renderer.DrawRoundedRect(Bounds.X, circleY, CircleSize, CircleSize, CircleSize / 2, Color.Transparent);

                // Punto de verificación
                if (IsChecked)
                {
                    float innerSize = CircleSize * 0.6f;
                    float innerOffset = (CircleSize - innerSize) / 2;
                    Renderer.DrawRoundedRect(Bounds.X + innerOffset, circleY + innerOffset, innerSize, innerSize, innerSize / 2, CheckColor);
                }

                // Texto
                if (!string.IsNullOrEmpty(Text))
                {
                    float textX = Bounds.X + CircleSize + TextSpacing;
                    float textY = Bounds.Y + (Bounds.Height - 12) / 2;
                    Renderer.DrawText(Text, textX, textY, TextColor, 12);
                }
            }

            public void SetChecked(bool _checked)
            {
                if (_checked && !string.IsNullOrEmpty(GroupName))
                {
                    // Desmarcar otros radio buttons del mismo grupo
                    if (_groups.ContainsKey(GroupName))
                    {
                        foreach (var radio in _groups[GroupName])
                        {
                            if (radio != this) radio.IsChecked = false;
                        }
                    }
                    else
                    {
                        _groups[GroupName] = new List<RadioButton>();
                    }

                    if (!_groups[GroupName].Contains(this))
                        _groups[GroupName].Add(this);
                }

                IsChecked = _checked;
                OnCheckedChanged?.Invoke(IsChecked);
            }

            public Action<bool> OnCheckedChanged;
        }

        public class Slider : UIControl
        {
            public float MinValue = 0;
            public float MaxValue = 100;
            public float Value = 50;
            public Color FillColor = new Color(51, 153, 255);
            public Color EmptyColor = new Color(77, 77, 77);
            public Color HandleColor = Color.White;
            public bool ShowValue = true;
            public string Label = "";
            public int DecimalPlaces = 1;
            public float HandleSize = 20f;

            private bool _dragging;

            public override void Draw()
            {
                if (!IsVisible) return;

                float trackHeight = 10f;
                float trackY = Bounds.Y + (Bounds.Height - trackHeight) / 2;

                // Barra de fondo
                Renderer.DrawRoundedRect(Bounds.X, trackY, Bounds.Width, trackHeight, trackHeight / 2, EmptyColor);

                // Barra de valor
                float fillWidth = (Value - MinValue) / (MaxValue - MinValue) * Bounds.Width;
                Renderer.DrawRoundedRect(Bounds.X, trackY, fillWidth, trackHeight, trackHeight / 2, FillColor);

                // Handle
                float handleX = Bounds.X + fillWidth - HandleSize / 2;
                float handleY = Bounds.Y + (Bounds.Height - HandleSize) / 2;
                Color handleColor = _dragging ? FillColor : HandleColor;
                Renderer.DrawRoundedRect(handleX, handleY, HandleSize, HandleSize, HandleSize / 2, handleColor);

                // Label
                if (!string.IsNullOrEmpty(Label))
                {
                    Renderer.DrawText(Label, Bounds.X, Bounds.Y - 20, Color.White, 12);
                }

                // Valor
                if (ShowValue)
                {
                    string valueText = Value.ToString($"F{DecimalPlaces}");
                    var textSize = Renderer.MeasureText(valueText, 12);
                    Renderer.DrawText(valueText, Bounds.X + Bounds.Width + 10, Bounds.Y + (Bounds.Height - textSize.Y) / 2, Color.White, 12);
                }
            }

            public override void Update()
            {
                base.Update();

                if (IsHovered && UIEventSystem.IsMousePressed)
                {
                    _dragging = true;
                }

                if (_dragging)
                {
                    if (!UIEventSystem.IsMouseDown)
                    {
                        _dragging = false;
                    }
                    else
                    {
                        float mouseX = UIEventSystem.MousePosition.X;
                        float normalized = Math.Clamp((mouseX - Bounds.X) / Bounds.Width, 0, 1);
                        float newValue = MinValue + normalized * (MaxValue - MinValue);

                        if (Math.Abs(newValue - Value) > 0.01f)
                        {
                            Value = newValue;
                            OnValueChanged?.Invoke(Value);
                        }
                    }
                }
            }

            public Action<float> OnValueChanged;
        }

        public class ProgressBar : UIControl
        {
            public float MinValue = 0;
            public float MaxValue = 100;
            public float Value = 0;
            public Color FillColor = new Color(51, 153, 255);
            public Color EmptyColor = new Color(77, 77, 77);
            public Color BorderColor = Color.White;
            public bool ShowPercentage = true;
            public bool ShowValue = false;
            public string Label = "";

            public override void Draw()
            {
                if (!IsVisible) return;

                // Borde
                Renderer.DrawRectOutline(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BorderColor, 1f);

                // Fondo
                Renderer.DrawRect(Bounds.X + 1, Bounds.Y + 1, Bounds.Width - 2, Bounds.Height - 2, EmptyColor);

                // Progreso
                float fillWidth = ((Value - MinValue) / (MaxValue - MinValue)) * (Bounds.Width - 2);
                if (fillWidth > 0)
                {
                    Renderer.DrawRect(Bounds.X + 1, Bounds.Y + 1, fillWidth, Bounds.Height - 2, FillColor);
                }

                // Texto
                string text = "";
                if (ShowPercentage)
                {
                    float percentage = ((Value - MinValue) / (MaxValue - MinValue)) * 100;
                    text = $"{percentage:F1}%";
                }
                else if (ShowValue)
                {
                    text = $"{Value:F1}/{MaxValue:F1}";
                }

                if (!string.IsNullOrEmpty(text))
                {
                    var textSize = Renderer.MeasureText(text, 12);
                    float textX = Bounds.X + (Bounds.Width - textSize.X) / 2;
                    float textY = Bounds.Y + (Bounds.Height - textSize.Y) / 2;
                    Renderer.DrawText(text, textX, textY, Color.White, 12);
                }

                // Label
                if (!string.IsNullOrEmpty(Label))
                {
                    Renderer.DrawText(Label, Bounds.X, Bounds.Y - 20, Color.White, 12);
                }
            }

            public void SetProgress(float value)
            {
                Value = Math.Clamp(value, MinValue, MaxValue);
            }
        }

        public class ComboBox : UIControl
        {
            public List<string> Items = new List<string>();
            public int SelectedIndex = -1;
            public Color DropdownColor = new Color(64, 64, 64);
            public Color SelectedColor = new Color(51, 153, 255);
            public Color TextColor = Color.White;
            public float ItemHeight = 25f;
            public bool IsDropdownOpen = false;

            public string SelectedItem => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : "";

            public override void Draw()
            {
                if (!IsVisible) return;

                // Caja principal
                Color bgColor = IsHovered ? Color.LightGray : BackgroundColor;
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, bgColor);
                Renderer.DrawRectOutline(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, Color.White, 1f);

                // Texto seleccionado
                if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
                {
                    Renderer.DrawText(Items[SelectedIndex], Bounds.X + 5, Bounds.Y + 5, TextColor, 12);
                }

                // Flecha
                float arrowX = Bounds.X + Bounds.Width - 20;
                float arrowY = Bounds.Y + Bounds.Height / 2;
                string arrow = IsDropdownOpen ? "▲" : "▼";
                Renderer.DrawText(arrow, arrowX, arrowY - 6, Color.White, 12);

                // Dropdown
                if (IsDropdownOpen)
                {
                    float dropdownY = Bounds.Y + Bounds.Height;
                    float dropdownHeight = Items.Count * ItemHeight;

                    Renderer.DrawRect(Bounds.X, dropdownY, Bounds.Width, dropdownHeight, DropdownColor);
                    Renderer.DrawRectOutline(Bounds.X, dropdownY, Bounds.Width, dropdownHeight, Color.White, 1f);

                    for (int i = 0; i < Items.Count; i++)
                    {
                        float itemY = dropdownY + i * ItemHeight;
                        bool isHovered = UIEventSystem.MousePosition.Y >= itemY && UIEventSystem.MousePosition.Y < itemY + ItemHeight;

                        if (isHovered)
                        {
                            Renderer.DrawRect(Bounds.X, itemY, Bounds.Width, ItemHeight, SelectedColor);
                        }

                        Renderer.DrawText(Items[i], Bounds.X + 5, itemY + 5, TextColor, 12);
                    }
                }
            }

            public override void Update()
            {
                base.Update();

                if (OnClick == null)
                {
                    OnClick = _ => {
                        if (IsDropdownOpen)
                        {
                            // Verificar si se hizo clic en un elemento
                            float mouseY = UIEventSystem.MousePosition.Y;
                            float dropdownY = Bounds.Y + Bounds.Height;

                            if (mouseY >= dropdownY && mouseY < dropdownY + Items.Count * ItemHeight)
                            {
                                int clickedIndex = (int)((mouseY - dropdownY) / ItemHeight);
                                if (clickedIndex >= 0 && clickedIndex < Items.Count)
                                {
                                    SelectedIndex = clickedIndex;
                                    OnSelectionChanged?.Invoke(SelectedIndex, Items[SelectedIndex]);
                                }
                            }
                        }
                        IsDropdownOpen = !IsDropdownOpen;
                    };
                }
            }

            public Action<int, string> OnSelectionChanged;
        }

        public class ListBox : UIControl
        {
            public List<string> Items = new List<string>();
            public int SelectedIndex = -1;
            public Color ItemColor = new Color(64, 64, 64);
            public Color SelectedColor = new Color(51, 153, 255);
            public Color TextColor = Color.White;
            public float ItemHeight = 25f;
            public bool MultiSelect = false;
            public List<int> SelectedIndices = new List<int>();

            public override void Draw()
            {
                if (!IsVisible) return;

                // Fondo
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BackgroundColor);
                Renderer.DrawRectOutline(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, Color.White, 1f);

                // Items
                float maxItems = Bounds.Height / ItemHeight;
                int startIndex = 0; // Aquí podrías implementar scroll

                for (int i = startIndex; i < Math.Min(Items.Count, startIndex + maxItems); i++)
                {
                    float itemY = Bounds.Y + (i - startIndex) * ItemHeight;
                    bool isSelected = MultiSelect ? SelectedIndices.Contains(i) : i == SelectedIndex;

                    // Fondo del item
                    if (isSelected)
                    {
                        Renderer.DrawRect(Bounds.X, itemY, Bounds.Width, ItemHeight, SelectedColor);
                    }
                    else
                    {
                        Renderer.DrawRect(Bounds.X, itemY, Bounds.Width, ItemHeight, ItemColor);
                    }

                    // Texto
                    Renderer.DrawText(Items[i], Bounds.X + 5, itemY + 5, TextColor, 12);
                }
            }

            public override void Update()
            {
                base.Update();

                if (OnClick == null)
                {
                    OnClick = mousePos => {
                        float relativeY = mousePos.Y - Bounds.Y;
                        int clickedIndex = (int)(relativeY / ItemHeight);

                        if (clickedIndex >= 0 && clickedIndex < Items.Count)
                        {
                            if (MultiSelect)
                            {
                                if (SelectedIndices.Contains(clickedIndex))
                                    SelectedIndices.Remove(clickedIndex);
                                else
                                    SelectedIndices.Add(clickedIndex);
                            }
                            else
                            {
                                SelectedIndex = clickedIndex;
                            }

                            OnSelectionChanged?.Invoke(clickedIndex, Items[clickedIndex]);
                        }
                    };
                }
            }

            public Action<int, string> OnSelectionChanged;
        }

        public class Window : UIControl
        {
            public string Title = "";
            public List<UIControl> Controls = new List<UIControl>();
            public Color TitleBarColor = new Color(51, 102, 204);
            public Color BorderColor = new Color(77, 77, 77);
            public float BorderThickness = 2f;
            public bool IsDraggable = true;
            public bool IsResizable = false;
            public bool ShowCloseButton = true;
            public bool ShowMinimizeButton = false;
            public bool ShowMaximizeButton = false;
            public bool IsModal = false;
            public Vector2 MinSize = new Vector2(100, 50);
            public Vector2 MaxSize = new Vector2(float.MaxValue, float.MaxValue);

            private bool _isDragging;
            private bool _isResizing;
            private Vector2 _dragOffset;
            private Button _closeButton;
            private Button _minimizeButton;
            private Button _maximizeButton;
            private bool _isMinimized;
            private bool _isMaximized;
            private Rect _restoreRect;

            public Window()
            {
                BackgroundColor = new Color(45, 45, 45);

                // Configurar botones
                _closeButton = new Button()
                {
                    Text = "×",
                    BackgroundColor = new Color(232, 17, 35),
                    TextColor = Color.White,
                    OnClick = _ => Close()
                };

                _minimizeButton = new Button()
                {
                    Text = "−",
                    BackgroundColor = new Color(128, 128, 128),
                    TextColor = Color.White,
                    OnClick = _ => Minimize()
                };

                _maximizeButton = new Button()
                {
                    Text = "□",
                    BackgroundColor = new Color(128, 128, 128),
                    TextColor = Color.White,
                    OnClick = _ => Maximize()
                };
            }

            public override void Draw()
            {
                if (!IsVisible) return;

                // Fondo de la ventana
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BackgroundColor);

                // Borde
                Renderer.DrawRectOutline(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BorderColor, BorderThickness);

                // Barra de título
                float titleBarHeight = 30f;
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, titleBarHeight, TitleBarColor);

                // Título
                if (!string.IsNullOrEmpty(Title))
                {
                    Renderer.DrawText(Title, Bounds.X + 10, Bounds.Y + 8, Color.White, 14);
                }

                // Botones de la ventana
                float buttonY = Bounds.Y + 5;
                float buttonSize = 20f;
                float buttonSpacing = 25f;
                float buttonX = Bounds.X + Bounds.Width - buttonSize - 5;

                if (ShowCloseButton)
                {
                    _closeButton.SetBounds(buttonX, buttonY, buttonSize, buttonSize);
                    _closeButton.Draw();
                    buttonX -= buttonSpacing;
                }

                if (ShowMaximizeButton)
                {
                    _maximizeButton.SetBounds(buttonX, buttonY, buttonSize, buttonSize);
                    _maximizeButton.Draw();
                    buttonX -= buttonSpacing;
                }

                if (ShowMinimizeButton)
                {
                    _minimizeButton.SetBounds(buttonX, buttonY, buttonSize, buttonSize);
                    _minimizeButton.Draw();
                }

                // Contenido de la ventana (solo si no está minimizada)
                if (!_isMinimized)
                {
                    // Área de contenido
                    float contentY = Bounds.Y + titleBarHeight;
                    float contentHeight = Bounds.Height - titleBarHeight;

                    // Dibujar controles hijos
                    foreach (var control in Controls)
                    {
                        if (!control.IsVisible) continue;

                        // Guardar posición original
                        var originalBounds = control.Bounds;

                        // Aplicar transformación de la ventana
                        control.Bounds = new Rect(
                            Bounds.X + originalBounds.X,
                            contentY + originalBounds.Y,
                            originalBounds.Width,
                            originalBounds.Height
                        );

                        control.Draw();

                        // Restaurar posición original
                        control.Bounds = originalBounds;
                    }
                }
            }

            public override void Update()
            {
                if (!IsVisible) return;

                var mousePos = UIEventSystem.MousePosition;
                var mouseDown = UIEventSystem.IsMouseDown;
                var mousePressed = UIEventSystem.IsMousePressed;

                // Actualizar botones
                if (ShowCloseButton) _closeButton.Update();
                if (ShowMinimizeButton) _minimizeButton.Update();
                if (ShowMaximizeButton) _maximizeButton.Update();

                // Manejar arrastre de la ventana
                if (IsDraggable && !_isMaximized)
                {
                    var titleBarRect = new Rect(Bounds.X, Bounds.Y, Bounds.Width, 30);

                    if (mousePressed && titleBarRect.Contains(mousePos.X, mousePos.Y) && !_isDragging)
                    {
                        // Verificar que no se haga clic en los botones
                        bool clickedButton = false;
                        if (ShowCloseButton && _closeButton.Bounds.Contains(mousePos.X, mousePos.Y)) clickedButton = true;
                        if (ShowMinimizeButton && _minimizeButton.Bounds.Contains(mousePos.X, mousePos.Y)) clickedButton = true;
                        if (ShowMaximizeButton && _maximizeButton.Bounds.Contains(mousePos.X, mousePos.Y)) clickedButton = true;

                        if (!clickedButton)
                        {
                            _isDragging = true;
                            _dragOffset = new Vector2(mousePos.X - Bounds.X, mousePos.Y - Bounds.Y);
                        }
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

                // Actualizar controles hijos (solo si no está minimizada)
                if (!_isMinimized)
                {
                    float contentY = Bounds.Y + 30;

                    foreach (var control in Controls)
                    {
                        if (!control.IsVisible) continue;

                        // Guardar posición original
                        var originalBounds = control.Bounds;

                        // Aplicar transformación de la ventana
                        control.Bounds = new Rect(
                            Bounds.X + originalBounds.X,
                            contentY + originalBounds.Y,
                            originalBounds.Width,
                            originalBounds.Height
                        );

                        control.Update();

                        // Restaurar posición original
                        control.Bounds = originalBounds;
                    }
                }
            }

            public void AddControl(UIControl control) => Controls.Add(control);
            public void RemoveControl(UIControl control) => Controls.Remove(control);
            public void ClearControls() => Controls.Clear();

            public void Close()
            {
                IsVisible = false;
                OnClosed?.Invoke();
            }

            public void Minimize()
            {
                _isMinimized = !_isMinimized;
                if (_isMinimized)
                {
                    Bounds = new Rect(Bounds.X, Bounds.Y, Bounds.Width, 30);
                }
                else
                {
                    // Restaurar tamaño original (aquí deberías guardar el tamaño original)
                }
            }

            public void Maximize()
            {
                if (!_isMaximized)
                {
                    _restoreRect = Bounds;
                    // Maximizar a pantalla completa (necesitarías las dimensiones de la pantalla)
                    _isMaximized = true;
                }
                else
                {
                    Bounds = _restoreRect;
                    _isMaximized = false;
                }
            }

            public Action OnClosed;
        }

        public class GroupBox : UIControl
        {
            public string Title = "";
            public List<UIControl> Controls = new List<UIControl>();
            public Color TitleColor = Color.White;
            public Color BorderColor = Color.Gray;
            public Vector2 Padding = new Vector2(10, 20);

            public override void Draw()
            {
                if (!IsVisible) return;

                // Título
                float titleHeight = 0;
                if (!string.IsNullOrEmpty(Title))
                {
                    titleHeight = 15;
                    Renderer.DrawText(Title, Bounds.X + 10, Bounds.Y, TitleColor, 12);
                }

                // Borde del grupo
                Renderer.DrawRectOutline(Bounds.X, Bounds.Y + titleHeight / 2, Bounds.Width, Bounds.Height - titleHeight / 2, BorderColor, 1f);

                // Fondo
                if (BackgroundColor.A > 0)
                {
                    Renderer.DrawRect(Bounds.X + 1, Bounds.Y + titleHeight / 2 + 1, Bounds.Width - 2, Bounds.Height - titleHeight / 2 - 2, BackgroundColor);
                }

                // Controles hijos
                foreach (var control in Controls)
                {
                    if (!control.IsVisible) continue;

                    var originalBounds = control.Bounds;
                    control.Bounds = new Rect(
                        Bounds.X + Padding.X + originalBounds.X,
                        Bounds.Y + titleHeight + Padding.Y + originalBounds.Y,
                        originalBounds.Width,
                        originalBounds.Height
                    );

                    control.Draw();
                    control.Bounds = originalBounds;
                }
            }

            public override void Update()
            {
                if (!IsVisible) return;

                base.Update();

                float titleHeight = string.IsNullOrEmpty(Title) ? 0 : 15;

                foreach (var control in Controls)
                {
                    if (!control.IsVisible) continue;

                    var originalBounds = control.Bounds;
                    control.Bounds = new Rect(
                        Bounds.X + Padding.X + originalBounds.X,
                        Bounds.Y + titleHeight + Padding.Y + originalBounds.Y,
                        originalBounds.Width,
                        originalBounds.Height
                    );

                    control.Update();
                    control.Bounds = originalBounds;
                }
            }

            public void AddControl(UIControl control) => Controls.Add(control);
            public void RemoveControl(UIControl control) => Controls.Remove(control);
        }

        public class TabControl : UIControl
        {
            public List<TabPage> TabPages = new List<TabPage>();
            public int SelectedTabIndex = 0;
            public Color TabColor = new Color(64, 64, 64);
            public Color SelectedTabColor = new Color(51, 102, 204);
            public Color TabTextColor = Color.White;
            public float TabHeight = 30f;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Fondo
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BackgroundColor);

                // Tabs
                float tabWidth = TabPages.Count > 0 ? Bounds.Width / TabPages.Count : 0;
                for (int i = 0; i < TabPages.Count; i++)
                {
                    float tabX = Bounds.X + i * tabWidth;
                    Color tabBgColor = i == SelectedTabIndex ? SelectedTabColor : TabColor;

                    Renderer.DrawRect(tabX, Bounds.Y, tabWidth, TabHeight, tabBgColor);
                    Renderer.DrawRectOutline(tabX, Bounds.Y, tabWidth, TabHeight, Color.White, 1f);

                    // Texto del tab
                    var textSize = Renderer.MeasureText(TabPages[i].Title, 12);
                    float textX = tabX + (tabWidth - textSize.X) / 2;
                    float textY = Bounds.Y + (TabHeight - textSize.Y) / 2;
                    Renderer.DrawText(TabPages[i].Title, textX, textY, TabTextColor, 12);
                }

                // Contenido del tab seleccionado
                if (SelectedTabIndex >= 0 && SelectedTabIndex < TabPages.Count)
                {
                    var selectedTab = TabPages[SelectedTabIndex];
                    float contentY = Bounds.Y + TabHeight;
                    float contentHeight = Bounds.Height - TabHeight;

                    // Fondo del contenido
                    Renderer.DrawRect(Bounds.X, contentY, Bounds.Width, contentHeight, selectedTab.BackgroundColor);

                    // Controles del tab
                    foreach (var control in selectedTab.Controls)
                    {
                        if (!control.IsVisible) continue;

                        var originalBounds = control.Bounds;
                        control.Bounds = new Rect(
                            Bounds.X + originalBounds.X,
                            contentY + originalBounds.Y,
                            originalBounds.Width,
                            originalBounds.Height
                        );

                        control.Draw();
                        control.Bounds = originalBounds;
                    }
                }
            }

            public override void Update()
            {
                if (!IsVisible) return;

                base.Update();

                // Manejar clics en tabs
                if (UIEventSystem.IsMousePressed)
                {
                    var mousePos = UIEventSystem.MousePosition;
                    if (mousePos.Y >= Bounds.Y && mousePos.Y <= Bounds.Y + TabHeight)
                    {
                        float tabWidth = TabPages.Count > 0 ? Bounds.Width / TabPages.Count : 0;
                        int clickedTab = (int)((mousePos.X - Bounds.X) / tabWidth);

                        if (clickedTab >= 0 && clickedTab < TabPages.Count)
                        {
                            SelectedTabIndex = clickedTab;
                            OnTabChanged?.Invoke(SelectedTabIndex);
                        }
                    }
                }

                // Actualizar controles del tab seleccionado
                if (SelectedTabIndex >= 0 && SelectedTabIndex < TabPages.Count)
                {
                    var selectedTab = TabPages[SelectedTabIndex];
                    float contentY = Bounds.Y + TabHeight;

                    foreach (var control in selectedTab.Controls)
                    {
                        if (!control.IsVisible) continue;

                        var originalBounds = control.Bounds;
                        control.Bounds = new Rect(
                            Bounds.X + originalBounds.X,
                            contentY + originalBounds.Y,
                            originalBounds.Width,
                            originalBounds.Height
                        );

                        control.Update();
                        control.Bounds = originalBounds;
                    }
                }
            }

            public void AddTab(TabPage tabPage) => TabPages.Add(tabPage);
            public void RemoveTab(TabPage tabPage) => TabPages.Remove(tabPage);
            public void RemoveTabAt(int index) => TabPages.RemoveAt(index);

            public Action<int> OnTabChanged;
        }

        public class TabPage
        {
            public string Title = "";
            public List<UIControl> Controls = new List<UIControl>();
            public Color BackgroundColor = new Color(45, 45, 45);

            public void AddControl(UIControl control) => Controls.Add(control);
            public void RemoveControl(UIControl control) => Controls.Remove(control);
        }

        public class ToolTip : UIControl
        {
            public string Text = "";
            public Color TextColor = Color.White;
            public Color BackgroundColor = new Color(64, 64, 64, 240);
            public Color BorderColor = Color.White;
            public Vector2 Padding = new Vector2(5, 3);
            public float ShowDelay = 0.5f;
            public float HideDelay = 0.1f;

            private float _showTimer;
            private float _hideTimer;
            private bool _shouldShow;

            public override void Draw()
            {
                if (!IsVisible || string.IsNullOrEmpty(Text)) return;

                // Auto-size basado en el texto
                var textSize = Renderer.MeasureText(Text, 12);
                Bounds = new Rect(Bounds.Position, new Vector2(textSize.X + Padding.X * 2, textSize.Y + Padding.Y * 2));

                // Fondo
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BackgroundColor);
                Renderer.DrawRectOutline(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BorderColor, 1f);

                // Texto
                Renderer.DrawText(Text, Bounds.X + Padding.X, Bounds.Y + Padding.Y, TextColor, 12);
            }

            public void Show(Vector2 position, string text)
            {
                Text = text;
                SetPosition(position);
                _shouldShow = true;
                _showTimer = 0;
            }

            public void Hide()
            {
                _shouldShow = false;
                _hideTimer = 0;
            }

            public override void Update()
            {
                if (_shouldShow)
                {
                    _showTimer += Time.DeltaTime;
                    if (_showTimer >= ShowDelay)
                    {
                        IsVisible = true;
                    }
                }
                else
                {
                    _hideTimer += Time.DeltaTime;
                    if (_hideTimer >= HideDelay)
                    {
                        IsVisible = false;
                    }
                }
            }
        }

        #endregion

        #region Utilidades y Helpers

        public static class Layout
        {
            public static void StackVertical(List<UIControl> controls, float spacing = 5f)
            {
                float currentY = 0;
                foreach (var control in controls)
                {
                    control.SetPosition(new Vector2(control.Bounds.X, currentY));
                    currentY += control.Bounds.Height + spacing;
                }
            }

            public static void StackHorizontal(List<UIControl> controls, float spacing = 5f)
            {
                float currentX = 0;
                foreach (var control in controls)
                {
                    control.SetPosition(new Vector2(currentX, control.Bounds.Y));
                    currentX += control.Bounds.Width + spacing;
                }
            }

            public static void Grid(List<UIControl> controls, int columns, float spacingX = 5f, float spacingY = 5f)
            {
                for (int i = 0; i < controls.Count; i++)
                {
                    int row = i / columns;
                    int col = i % columns;

                    float x = col * (controls[i].Bounds.Width + spacingX);
                    float y = row * (controls[i].Bounds.Height + spacingY);

                    controls[i].SetPosition(new Vector2(x, y));
                }
            }

            public static void Center(UIControl control, Rect container)
            {
                float x = container.X + (container.Width - control.Bounds.Width) / 2;
                float y = container.Y + (container.Height - control.Bounds.Height) / 2;
                control.SetPosition(new Vector2(x, y));
            }

            public static void Anchor(UIControl control, Rect container, HorizontalAlignment hAlign, VerticalAlignment vAlign)
            {
                float x = hAlign switch
                {
                    HorizontalAlignment.Left => container.X,
                    HorizontalAlignment.Center => container.X + (container.Width - control.Bounds.Width) / 2,
                    HorizontalAlignment.Right => container.X + container.Width - control.Bounds.Width,
                    _ => container.X
                };

                float y = vAlign switch
                {
                    VerticalAlignment.Top => container.Y,
                    VerticalAlignment.Middle => container.Y + (container.Height - control.Bounds.Height) / 2,
                    VerticalAlignment.Bottom => container.Y + container.Height - control.Bounds.Height,
                    _ => container.Y
                };

                control.SetPosition(new Vector2(x, y));
            }
        }

        public static class Themes
        {
            public static class Dark
            {
                public static readonly Color Background = new(30, 30, 30);
                public static readonly Color Surface = new(45, 45, 45);
                public static readonly Color Primary = new(51, 153, 255);
                public static readonly Color Secondary = new(128, 128, 128);
                public static readonly Color Text = Color.White;
                public static readonly Color TextSecondary = new(192, 192, 192);
                public static readonly Color Border = new(77, 77, 77);
                public static readonly Color Hover = new(64, 64, 64);
                public static readonly Color Active = new(32, 32, 32);
            }

            public static class Light
            {
                public static readonly Color Background = new(240, 240, 240);
                public static readonly Color Surface = Color.White;
                public static readonly Color Primary = new(0, 120, 215);
                public static readonly Color Secondary = new(128, 128, 128);
                public static readonly Color Text = new(32, 32, 32);
                public static readonly Color TextSecondary = new(96, 96, 96);
                public static readonly Color Border = new(200, 200, 200);
                public static readonly Color Hover = new(229, 229, 229);
                public static readonly Color Active = new(225, 225, 225);
            }
        }

        #endregion

        #region Sistema de Tiempo

        public static class Time
        {
            public static float DeltaTime { get; private set; }
            private static DateTime _lastFrameTime = DateTime.Now;

            public static void Update()
            {
                var now = DateTime.Now;
                DeltaTime = (float)(now - _lastFrameTime).TotalSeconds;
                _lastFrameTime = now;
            }
        }

        #endregion

        #region Gestión Global

        public static void SetFocusedControl(UIControl control)
        {
            if (_focusedControl != control)
            {
                _focusedControl?.OnFocusLost?.Invoke();
                _focusedControl = control;
                _focusedControl?.OnFocusGained?.Invoke();
            }
        }

        public static void ClearFocus()
        {
            _focusedControl?.OnFocusLost?.Invoke();
            _focusedControl = null;
        }

        public static void AddModal(UIControl control)
        {
            if (!_modalControls.Contains(control))
                _modalControls.Add(control);
        }

        public static void RemoveModal(UIControl control)
        {
            _modalControls.Remove(control);
        }

        public static bool HasModal => _modalControls.Count > 0;

        #endregion
    }
}