using AvalonInjectLib.UIFramework;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static class UIEventSystem
    {
        private static Vector2 _mousePosition;
        private static Vector2 _lastMousePosition;
        private static Vector2 _globalMousePosition;
        private static bool _mouseDown;
        private static bool _lastMouseDown;
        private static UIControl _focusedControl;
        private static bool _isMouseInWindow = true;

        // Propiedades públicas
        public static Vector2 MousePosition => _mousePosition;
        public static Vector2 GlobalMousePosition => _globalMousePosition;
        public static Vector2 MouseDelta => new Vector2(_mousePosition.X - _lastMousePosition.X, _mousePosition.Y - _lastMousePosition.Y);
        public static bool IsMouseDown => _mouseDown;
        public static bool IsMousePressed => _mouseDown && !_lastMouseDown;
        public static bool IsMouseReleased => !_mouseDown && _lastMouseDown;
        public static bool IsMouseInWindow => _isMouseInWindow;
        public static Vector2 WindowSize => WindowCoordinateHelper.GetClientSize();

        public static string InputText { get; internal set; }
        public static bool BlockOtherControls { get; internal set; }

        /// <summary>
        /// Inicializa el sistema de eventos UI
        /// </summary>
        internal static void Initialize(uint processId)
        {
            WindowCoordinateHelper.Initialize(processId);
        }

        /// <summary>
        /// Actualiza el input del sistema UI con normalización automática
        /// </summary>
        internal static void UpdateInput((int X, int Y) globalMousePos, bool? mouseDown = null)
        {
            if (WindowCoordinateHelper.IsGameWindowActive())
            {
                // Guardar estado anterior
                _lastMousePosition = _mousePosition;
                if (mouseDown != null)
                {
                    _lastMouseDown = _mouseDown;
                }

                // Guardar posición global
                _globalMousePosition = new Vector2(globalMousePos.X, globalMousePos.Y);

                // Normalizar posición del mouse a coordenadas de ventana
                _mousePosition = WindowCoordinateHelper.GlobalToLocal(_globalMousePosition);


                // Verificar si el mouse está dentro de la ventana
                _isMouseInWindow = WindowCoordinateHelper.IsMouseInWindow(_mousePosition);

                if (mouseDown != null)
                {
                    // Actualizar otros estados
                    _mouseDown = mouseDown.Value && _isMouseInWindow; // Solo considerar clicks dentro de la ventana
                }
            }
        }

        /// <summary>
        /// Versión alternativa que acepta Vector2
        /// </summary>
        internal static void UpdateInput(Vector2 globalMousePos, bool mouseDown)
        {
            UpdateInput(((int)globalMousePos.X, (int)globalMousePos.Y), mouseDown);
        }

        /// <summary>
        /// Obtiene la posición del mouse normalizada (0-1)
        /// </summary>
        public static Vector2 GetNormalizedMousePosition()
        {
            var windowSize = WindowCoordinateHelper.GetClientSize();
            return new Vector2(
                windowSize.X > 0 ? _mousePosition.X / windowSize.X : 0,
                windowSize.Y > 0 ? _mousePosition.Y / windowSize.Y : 0
            );
        }

        /// <summary>
        /// Obtiene la posición del mouse en coordenadas relativas al centro de la ventana
        /// </summary>
        public static Vector2 GetCenteredMousePosition()
        {
            var windowSize = WindowCoordinateHelper.GetClientSize();
            return new Vector2(
                _mousePosition.X - windowSize.X / 2,
                _mousePosition.Y - windowSize.Y / 2
            );
        }

        internal static bool IsKeyPressed(Keys key)
        {
            return InputSystem.GetKeyDown(key);
        }

        internal static void ProcessEvents(UIControl control)
        {
            if (!control.IsVisible || !control.IsEnabled) return;

            // Solo procesar eventos si el mouse está dentro de la ventana
            if (!_isMouseInWindow)
            {
                // Si el mouse salió de la ventana, limpiar hover states
                if (control.IsHovered)
                {
                    control.OnMouseLeave?.Invoke();
                    control.IsHovered = false;
                }
                control.WasMouseDown = false;
                return;
            }

            bool containsMouse = control.Bounds.Contains(_mousePosition.X, _mousePosition.Y);

            // Eventos de mouse hover
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

            // Eventos de mouse press
            if (containsMouse && IsMousePressed)
            {
                control.OnMouseDown?.Invoke(_mousePosition);
                _focusedControl = control;
            }

            // Eventos de mouse release
            if (control.WasMouseDown && IsMouseReleased)
            {
                control.OnMouseUp?.Invoke(_mousePosition);
                if (containsMouse) control.OnClick?.Invoke(_mousePosition);
            }

            control.WasMouseDown = _mouseDown && containsMouse;
        }

        /// <summary>
        /// Obtiene el control que tiene el foco actualmente
        /// </summary>
        internal static UIControl GetFocusedControl()
        {
            return _focusedControl;
        }

        /// <summary>
        /// Establece el foco en un control específico
        /// </summary>
        internal static void SetFocusedControl(UIControl control)
        {
            _focusedControl = control;
        }

        /// <summary>
        /// Limpia el foco actual
        /// </summary>
        internal static void ClearFocus()
        {
            _focusedControl = null;
        }
    }
}
