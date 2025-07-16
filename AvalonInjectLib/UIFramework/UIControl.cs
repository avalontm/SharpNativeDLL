using static AvalonInjectLib.Structs;
using System;

namespace AvalonInjectLib.UIFramework
{
    public abstract class UIControl
    {
        // Propiedades básicas
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; } = 100f;
        public float Height { get; set; } = 25f;
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
        public Action<Vector2>? Click;
        public Action<Vector2>? MouseDown;
        public Action<Vector2>? MouseUp;
        public Action<Vector2>? MouseMove;
        public Action<Vector2>? MouseEnter;
        public Action<Vector2>? MouseLeave;
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
                OnMouseMove(mousePos);
                MouseMove?.Invoke(mousePos);
            }

            // Manejar estado hover
            if (isMouseOver)
            {
                if (!_isHovered)
                {
                    _isHovered = true;
                    OnMouseEnter(mousePos);
                    MouseEnter?.Invoke(mousePos);
                }
            }
            else
            {
                if (_isHovered)
                {
                    _isHovered = false;
                    OnMouseLeave(mousePos);
                    MouseLeave?.Invoke(mousePos);
                }
            }

            // Manejar clics del mouse - MouseDown
            if (UIEventSystem.IsMousePressed && !_isPressed && isMouseOver)
            {
                // Click iniciado en este control
                _isPressed = true;
                OnMouseDown(mousePos);
                MouseDown?.Invoke(mousePos);
            }
            // Manejar MouseUp - CORREGIDO: ejecutar siempre que se suelte el mouse si estaba presionado
            else if (!UIEventSystem.IsMousePressed && _isPressed)
            {
                UIFrameworkSystem.SetControl(this);
                bool isValidClick = UIFrameworkSystem.IsValidClick();

                // Click liberado
                _isPressed = false;
               
                // SIEMPRE ejecutar MouseUp cuando se suelta el mouse
                OnMouseUp(mousePos);
                MouseUp?.Invoke(mousePos);

                // Solo ejecutar Click si es un click válido
                if (isValidClick)
                {
                    OnClick(mousePos);
                    Click?.Invoke(mousePos);
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
        protected virtual void OnClick(Vector2 mousePos) { }
        protected virtual void OnMouseMove(Vector2 mousePos) { }
        protected virtual void OnMouseDown(Vector2 mousePos) { }
        protected virtual void OnMouseUp(Vector2 mousePos) { }
        protected virtual void OnKeyPress(char key) { }
        protected virtual void OnMouseEnter(Vector2 mousePos) { }
        protected virtual void OnMouseLeave(Vector2 mousePos) { }

        /// <summary>
        /// Método público para simular un evento MouseMove en el control
        /// </summary>
        /// <param name="mousePos">Posición del mouse</param>
        public void SimulateMouseMove(Vector2 mousePos)
        {
            OnMouseMove(mousePos);
            MouseMove?.Invoke(mousePos);
        }

        // Manejo de foco - MODIFICADO
        public virtual void Focus()
        {
            if (!HasFocus)
            {
                Vector2 mousePos = UIEventSystem.MousePosition;
                HasFocus = true;
                UIFrameworkSystem.SetFocus(this);
                MouseEnter?.Invoke(mousePos);
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
        /// Propiedad para acceder a los límites del control (coordenadas relativas al padre)
        /// </summary>
        public Rect Bounds
        {
            get => new Rect(X, Y, Width, Height);
            set
            {
                X = value.X;
                Y = value.Y;
                Width = value.Width;
                Height = value.Height;

                // Notificar cambio de tamaño si es necesario
                OnSizeChanged();
            }
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