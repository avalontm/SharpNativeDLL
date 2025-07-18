using static AvalonInjectLib.Structs;
using System;

namespace AvalonInjectLib.UIFramework
{
    public abstract class UIControl
    {
        // Propiedades básicas
        private float _x = 0f;
        private float _y = 0f;
        private float _width = 100f;
        private float _height = 25f;

        // Propiedades básicas con notificación de cambios
        public float X
        {
            get => _x;
            set
            {
                if (_x != value)
                {
                    _x = value;
                    OnPositionChanged();
                }
            }
        }

        public float Y
        {
            get => _y;
            set
            {
                if (_y != value)
                {
                    _y = value;
                    OnPositionChanged();
                }
            }
        }

        public float Width
        {
            get => _width;
            set
            {
                if (_width != value)
                {
                    _width = value;
                    OnSizeChanged();
                }
            }
        }

        public float Height
        {
            get => _height;
            set
            {
                if (_height != value)
                {
                    _height = value;
                    OnSizeChanged();
                }
            }
        }

        public bool Visible { get; set; } = true;
        public bool Enabled { get; set; } = true;
        public string Name { get; set; } = string.Empty;
        public bool HasFocus { get; set; }
        public Color BackColor { get; set; } = Color.Black;
        public Color ForeColor { get; set; } = Color.White;
        public UIControl? Parent { get; set; }
        public object Tag { get; set; }
        public bool IsFocusable { get; internal set; } = false;

        // Eventos
        public Action<object, Vector2>? Click;
        public Action<object, Vector2>? MouseDown;
        public Action<object, Vector2>? MouseUp;
        public Action<object, Vector2>? MouseMove;
        public Action<object, Vector2>? MouseEnter;
        public Action<object, Vector2>? MouseLeave;
        public Action<char>? KeyPress;

        private bool _isHovered;
        private bool _isPressed;

        protected UIControl()
        {

        }

        public virtual void Update()
        {
            if (!Visible || !Enabled || !UIEventSystem.IsSCreenFocus) return;
            
            Vector2 mousePos = UIEventSystem.MousePosition;
            bool isMouseOver = Contains(mousePos);

            // SIEMPRE ejecutar MouseMove cuando el mouse está sobre el control
            if (isMouseOver)
            {
                OnMouseMove(this, mousePos);
                MouseMove?.Invoke(this, mousePos);
            }

            // Manejar estado hover
            if (isMouseOver)
            {
                if (!_isHovered)
                {
                    _isHovered = true;
                    OnMouseEnter(this, mousePos);
                    MouseEnter?.Invoke(this, mousePos);
                }
            }
            else
            {
                if (_isHovered)
                {
                    _isHovered = false;
                    OnMouseLeave(this, mousePos);
                    MouseLeave?.Invoke(this, mousePos);
                }
            }

            // Manejar clics del mouse - MouseDown
            if (UIEventSystem.IsMousePressed && !_isPressed && isMouseOver)
            {
                // Click iniciado en este control
                _isPressed = true;
                OnMouseDown(this, mousePos);
                MouseDown?.Invoke(this, mousePos);
            }
            // Manejar MouseUp - CORREGIDO: ejecutar siempre que se suelte el mouse si estaba presionado
            else if (!UIEventSystem.IsMousePressed && _isPressed)
            {
                UIFrameworkSystem.SetControl(this);
                bool isValidClick = UIFrameworkSystem.IsValidClick();

                // Click liberado
                _isPressed = false;
               
                // SIEMPRE ejecutar MouseUp cuando se suelta el mouse
                OnMouseUp(this, mousePos);
                MouseUp?.Invoke(this, mousePos);

                // Solo ejecutar Click si es un click válido
                if (isValidClick)
                {
                    OnClick(this, mousePos);
                    Click?.Invoke(this, mousePos);
                }

                UIFrameworkSystem.ClearFocusControls();
            }

            // Manejar teclado si tiene el foco
            if (HasFocus && UIEventSystem.LastKeyPressed.HasValue)
            {
                OnKeyPress(UIEventSystem.LastKeyPressed.Value);
                UIEventSystem.LastKeyPressed = null;
            }
        }

        public abstract void Draw();

        // Manejo de eventos protegidos
        protected virtual void OnClick(object sender, Vector2 pos) { }
        protected virtual void OnMouseMove(object sender, Vector2 pos) { }
        protected virtual void OnMouseDown(object sender, Vector2 pos) { }
        protected virtual void OnMouseUp(object sender, Vector2 pos) { }
        protected virtual void OnKeyPress(char key) { }
        protected virtual void OnMouseEnter(object sender, Vector2 pos) { }
        protected virtual void OnMouseLeave(object sender, Vector2 pos) { }

        // Manejo de foco - MODIFICADO
        public virtual void Focus()
        {
            if (!HasFocus)
            {
                Vector2 mousePos = UIEventSystem.MousePosition;
                HasFocus = true;
                UIFrameworkSystem.SetFocus(this);
                MouseEnter?.Invoke(this, mousePos);
            }
        }

        /// <summary>
        /// Determina si el punto especificado está dentro de los límites del control
        /// </summary>
        /// <param name="point">Posición a verificar</param>
        /// <returns>True si el punto está dentro del control</returns>
        public virtual bool Contains(Vector2 point)
        {
            return GetAbsoluteBounds().Contains(point);
        }

        /// <summary>
        /// Obtiene la posición absoluta del control en la pantalla
        /// </summary>
        /// <returns>Vector2 con las coordenadas X,Y absolutas</returns>
        public virtual Vector2 GetAbsolutePosition()
        {
            if (Parent == null)
            {
                return new Vector2(X, Y);
            }

            // Posición acumulada a través de la jerarquía de padres
            Vector2 parentPos = Parent.GetAbsolutePosition();
            return new Vector2(parentPos.X + X, parentPos.Y + Y);
        }

        /// <summary>
        /// Obtiene los límites absolutos del control en la pantalla
        /// </summary>
        /// <returns>Rectángulo con posición y tamaño absolutos</returns>
        public virtual Rect GetAbsoluteBounds()
        {
            Vector2 pos = GetAbsolutePosition();
            return new Rect(pos.X, pos.Y, Width, Height);
        }


        /// <summary>
        /// Método llamado cuando cambia la posición del control (X o Y)
        /// </summary>
        protected virtual void OnPositionChanged()
        {
            // Puede ser sobrescrito por controles hijos para manejar cambios de posición
        }

        /// <summary>
        /// Método llamado cuando cambia el tamaño del control
        /// </summary>
        protected virtual void OnSizeChanged()
        {
            // Puede ser sobrescrito por controles hijos para manejar cambios de tamaño
        }

        public override string ToString()
        {
            return $"{this.GetType()}";
        }
    }
}