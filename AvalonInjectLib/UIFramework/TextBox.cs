using static AvalonInjectLib.Structs;
using System;
using System.Text;

namespace AvalonInjectLib.UIFramework
{
    public class TextBox : UIControl
    {
        // Constantes para el TextBox
        private const float DEFAULT_WIDTH = 200f;
        private const float DEFAULT_HEIGHT = 25f;
        private const float TEXT_PADDING = 5f;
        private const float CURSOR_WIDTH = 1f;
        private const int CURSOR_BLINK_MS = 500;
        private const float SELECTION_ALPHA = 0.3f;

        // Estados del TextBox
        public bool IsHovered { get; private set; }
        public bool IsReadOnly { get; set; } = false;
        public bool Multiline { get; set; } = false;
        public char PasswordChar { get; set; } = '\0';
        public int MaxLength { get; set; } = int.MaxValue;
        public bool NumbersOnly { get; set; } = false;

        // Control de texto interno
        private readonly StringBuilder _textBuffer;
        private readonly Label _displayLabel;
        private int _cursorPosition = 0;
        private int _selectionStart = 0;
        private int _selectionLength = 0;
        private bool _showCursor = true;
        private long _lastCursorBlink = 0;
        private float _textOffset = 0f; // Para scroll horizontal
        private long _lastClickTime = 0;
        private const int DOUBLE_CLICK_MS = 500;
        private bool _isSelecting = false;
        private Vector2 _selectionStartPos;

        // Borde del TextBox
        public bool ShowBorder { get; set; } = true;
        public Color BorderColor { get; set; } = Color.FromArgb(100, 100, 100);
        public Color BorderColorFocus { get; set; } = Color.FromArgb(100, 149, 237);
        public float BorderWidth { get; set; } = 1f;
        public Color SelectionColor { get; set; } = Color.FromArgb(100, 149, 237);

        // Propiedades del texto
        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value ?? string.Empty;
                    _textBuffer.Clear();
                    _textBuffer.Append(_text);
                    _cursorPosition = Math.Min(_cursorPosition, _text.Length);
                    ClearSelection();
                    UpdateDisplayText();
                    OnTextChanged();
                }
            }
        }

        public string PlaceholderText { get; set; } = string.Empty;
        public Color PlaceholderColor { get; set; } = Color.FromArgb(128, 128, 128);

        // Eventos específicos del TextBox
        public Action<string>? TextChanged;
        public Action<string>? TextValidating;

        Font _font;
        public Font Font
        {
            get => _font;
            set
            {
                _font = value;
                if (_displayLabel != null)
                    _displayLabel.Font = _font;
            }
        }

        // Constructor
        public TextBox()
        {
            IsFocusable = true;
            Width = DEFAULT_WIDTH;
            Height = DEFAULT_HEIGHT;
            BackColor = Color.White;
            ForeColor = Color.Black;
            Font = Font.GetDefaultFont();

            _textBuffer = new StringBuilder();
            _lastCursorBlink = Environment.TickCount64;

            // Inicializar label interno para mostrar texto
            _displayLabel = new Label
            {
                Parent = this,
                Text = string.Empty,
                AutoSize = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                BackColor = Color.Transparent,
                ForeColor = ForeColor,
                Font = Font
            };

            UpdateDisplayLabel();
        }

        public override void Draw()
        {
            if (!Visible) return;

            var absPos = GetAbsolutePosition();
            var currentBorderColor = HasFocus ? BorderColorFocus : BorderColor;

            // Dibujar fondo del TextBox
            Renderer.DrawRect(new Rect(absPos.X, absPos.Y, Width, Height), BackColor);

            // Dibujar selección si existe
            if (HasSelection())
            {
                DrawSelection(absPos);
            }

            // Dibujar borde
            if (ShowBorder)
            {
                Renderer.DrawRectOutline(
                    new Rect(absPos.X, absPos.Y, Width, Height),
                    currentBorderColor,
                    BorderWidth
                );
            }

            // Dibujar el texto o placeholder
            DrawText(absPos);

            // Dibujar cursor si tiene foco
            if (HasFocus && !IsReadOnly && _showCursor)
            {
                DrawCursor(absPos);
            }
        }

        private void DrawText(Vector2 absPos)
        {
            if (string.IsNullOrEmpty(_text) && !string.IsNullOrEmpty(PlaceholderText) && !HasFocus)
            {
                // Dibujar placeholder - centrado verticalmente
                var textY = absPos.Y + (Height - GetTextHeight(PlaceholderText)) / 2f;
                var textPos = new Vector2(absPos.X + TEXT_PADDING, textY);
                Renderer.DrawText(PlaceholderText, textPos, PlaceholderColor, Font);
            }
            else
            {
                // Dibujar texto normal - centrado verticalmente
                var displayText = GetDisplayText();
                var textY = absPos.Y + (Height - GetTextHeight(displayText)) / 2f;
                var textPos = new Vector2(absPos.X + TEXT_PADDING - _textOffset, textY);
                Renderer.DrawText(displayText, textPos, ForeColor, Font);
            }
        }

        private void DrawCursor(Vector2 absPos)
        {
            var cursorX = absPos.X + TEXT_PADDING + GetCursorPixelPosition() - _textOffset;
            var cursorY = absPos.Y + 2f;
            var cursorHeight = Height - 4f;

            // Asegurar que el cursor esté visible dentro del control
            if (cursorX >= absPos.X + TEXT_PADDING && cursorX <= absPos.X + Width - TEXT_PADDING)
            {
                Renderer.DrawRect(new Rect(cursorX, cursorY, CURSOR_WIDTH, cursorHeight), ForeColor);
            }
        }

        private void DrawSelection(Vector2 absPos)
        {
            if (!HasSelection()) return;

            int selStart = Math.Min(_selectionStart, _selectionStart + _selectionLength);
            int selEnd = Math.Max(_selectionStart, _selectionStart + _selectionLength);

            float startX = absPos.X + TEXT_PADDING + GetTextWidth(_text.Substring(0, selStart)) - _textOffset;
            float endX = absPos.X + TEXT_PADDING + GetTextWidth(_text.Substring(0, selEnd)) - _textOffset;

            var selectionRect = new Rect(startX, absPos.Y + 2f, endX - startX, Height - 4f);
            Renderer.DrawRect(selectionRect, SelectionColor.WithAlpha(SELECTION_ALPHA * 0.7f)); // Más visible
        }

        private string GetDisplayText()
        {
            if (PasswordChar != '\0')
            {
                return new string(PasswordChar, _text.Length);
            }
            return _text;
        }

        private float GetTextWidth(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            // Necesitarás implementar un método para medir el ancho del texto
            // Por ahora uso una aproximación básica
            return text.Length * (Font.Size * 0.6f); // Aproximación basada en el tamaño de fuente
        }

        private float GetTextHeight(string text)
        {
            if (string.IsNullOrEmpty(text)) return Font.Size;
            return Font.Size; // La altura es generalmente el tamaño de la fuente
        }

        private float GetCursorPixelPosition()
        {
            if (_cursorPosition <= 0) return 0f;
            var textToCursor = GetDisplayText().Substring(0, _cursorPosition);
            return GetTextWidth(textToCursor);
        }

        private void UpdateDisplayLabel()
        {
            _displayLabel.X = TEXT_PADDING;
            _displayLabel.Y = 0;
            _displayLabel.Width = Math.Max(0, Width - (2 * TEXT_PADDING));
            _displayLabel.Height = Height;
        }

        private void UpdateTextOffset()
        {
            var cursorPixelPos = GetCursorPixelPosition();
            var visibleWidth = Width - (2 * TEXT_PADDING);

            // Scroll a la derecha si el cursor está fuera del área visible
            if (cursorPixelPos - _textOffset > visibleWidth)
            {
                _textOffset = cursorPixelPos - visibleWidth;
            }
            // Scroll a la izquierda si el cursor está antes del área visible
            else if (cursorPixelPos < _textOffset)
            {
                _textOffset = cursorPixelPos;
            }

            // Asegurar que el offset no sea negativo
            _textOffset = Math.Max(0, _textOffset);
        }

        public override void Update()
        {
            base.Update();

            // Actualizar parpadeo del cursor
            if (Environment.TickCount64 - _lastCursorBlink > CURSOR_BLINK_MS)
            {
                _showCursor = !_showCursor;
                _lastCursorBlink = Environment.TickCount64;
            }

            // Actualizar posición del label si cambió el tamaño
            if (_displayLabel.Width != Width - (2 * TEXT_PADDING) ||
                _displayLabel.Height != Height)
            {
                UpdateDisplayLabel();
            }

            _displayLabel.Update();
        }

        // Manejo de eventos de mouse
        protected override void OnMouseEnter(Vector2 mousePos)
        {
            base.OnMouseEnter(mousePos);
            IsHovered = true;
        }

        protected override void OnMouseLeave(Vector2 mousePos)
        {
            base.OnMouseLeave(mousePos);
            IsHovered = false;
        }

        protected override void OnClick(Vector2 mousePos)
        {
            base.OnClick(mousePos);

            long currentTime = Environment.TickCount64;
            if (currentTime - _lastClickTime < DOUBLE_CLICK_MS)
            {
                // Doble click - seleccionar todo
                SelectAll();
            }
            else
            {
                // Click simple - posicionar cursor
                if (!IsReadOnly)
                {
                    Focus();
                    SetCursorFromMousePosition(mousePos);
                }
            }
            _lastClickTime = currentTime;
        }


        protected override void OnMouseDown(Vector2 mousePos)
        {
            base.OnMouseDown(mousePos);

            if (!IsReadOnly && HasFocus)
            {
                _isSelecting = true;
                _selectionStartPos = mousePos;
                SetCursorFromMousePosition(mousePos);
                _selectionStart = _cursorPosition;
                _selectionLength = 0;
            }
        }

        protected override void OnMouseUp(Vector2 mousePos)
        {
            base.OnMouseUp(mousePos);
            _isSelecting = false;
        }

        protected override void OnMouseMove(Vector2 mousePos)
        {
            base.OnMouseMove(mousePos);

            if (_isSelecting)
            {
                SetCursorFromMousePosition(mousePos);
                _selectionLength = _cursorPosition - _selectionStart;
            }
        }

        private void SetCursorFromMousePosition(Vector2 mousePos)
        {
            var absPos = GetAbsolutePosition();
            var relativeX = mousePos.X - absPos.X - TEXT_PADDING + _textOffset;

            // Encontrar la posición del cursor más cercana
            var displayText = GetDisplayText();
            _cursorPosition = 0;

            for (int i = 0; i <= displayText.Length; i++)
            {
                var textWidth = GetTextWidth(displayText.Substring(0, i));
                if (relativeX <= textWidth)
                {
                    _cursorPosition = i;
                    break;
                }
                _cursorPosition = i;
            }

            ClearSelection();
            UpdateTextOffset();
        }

        // Manejo de eventos de teclado
        protected override void OnKeyPress(char key)
        {
            base.OnKeyPress(key);

            if (IsReadOnly) return;

            if (char.IsControl(key))
            {
                HandleControlKey(key);
            }
            else if ((!NumbersOnly && (char.IsLetterOrDigit(key) || char.IsPunctuation(key) || char.IsSymbol(key) || key == ' ')) ||
                     (NumbersOnly && char.IsDigit(key)))
            {
                InsertChar(key);
            }
        }

        private void HandleControlKey(char key)
        {
            switch (key)
            {
                case '\b': // Backspace
                    if (HasSelection())
                    {
                        DeleteSelection();
                    }
                    else if (_cursorPosition > 0)
                    {
                        _textBuffer.Remove(_cursorPosition - 1, 1);
                        _cursorPosition--;
                        UpdateText();
                    }
                    break;

                case '\r': // Enter
                case '\n':
                    if (Multiline)
                    {
                        InsertChar('\n');
                    }
                    break;
            }
        }

        private void InsertChar(char c)
        {
            if (_text.Length >= MaxLength) return;

            if (HasSelection())
            {
                DeleteSelection();
            }

            _textBuffer.Insert(_cursorPosition, c);
            _cursorPosition++;
            UpdateText();
        }

        private void DeleteSelection()
        {
            if (!HasSelection()) return;

            int start = Math.Min(_selectionStart, _selectionStart + _selectionLength);
            int length = Math.Abs(_selectionLength);

            _textBuffer.Remove(start, length);
            _cursorPosition = start;
            ClearSelection();
            UpdateText();
        }

        private void UpdateText()
        {
            _text = _textBuffer.ToString();
            UpdateTextOffset();
            OnTextChanged();
        }

        private void OnTextChanged()
        {
            TextChanged?.Invoke(_text);
        }

        // Métodos de selección
        public bool HasSelection()
        {
            return _selectionLength != 0;
        }

        private void UpdateDisplayText()
        {

        }

        public void ClearSelection()
        {
            _selectionStart = 0;
            _selectionLength = 0;
        }

        public void SelectAll()
        {
            _selectionStart = 0;
            _selectionLength = _text.Length;
        }

        public string GetSelectedText()
        {
            if (!HasSelection()) return string.Empty;

            int start = Math.Min(_selectionStart, _selectionStart + _selectionLength);
            int length = Math.Abs(_selectionLength);

            return _text.Substring(start, length);
        }

        // Métodos de navegación
        public void MoveCursorToStart()
        {
            _cursorPosition = 0;
            ClearSelection();
            UpdateTextOffset();
        }

        public void MoveCursorToEnd()
        {
            _cursorPosition = _text.Length;
            ClearSelection();
            UpdateTextOffset();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            UpdateDisplayLabel();
            UpdateTextOffset();
        }

        public override void Focus()
        {
            base.Focus();
            _showCursor = true;
            _lastCursorBlink = Environment.TickCount64;
        }
    }
}