using AvalonInjectLib.UIFramework;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static class UIEventSystem
    {
        // Estados del teclado
        private static char? _lastKeyPressed;
        private static string _inputText = string.Empty;

        // Estados del mouse
        private static float _mouseWheelDelta;
        private static Vector2 _mousePosition;
        private static Vector2 _lastMousePosition;
        private static Vector2 _globalMousePosition;
        private static bool _mouseDown;
        private static bool _lastMouseDown;
        private static bool _isMouseInWindow = true;

        // Control con foco
        private static UIControl _focusedControl;

        // Propiedades públicas
        public static float MouseWheelDelta => _mouseWheelDelta;
        public static Vector2 MousePosition => _mousePosition;
        public static Vector2 GlobalMousePosition => _globalMousePosition;
        public static Vector2 MouseDelta => _mousePosition - _lastMousePosition;
        public static bool IsMouseDown => _mouseDown;
        public static bool IsMousePressed => _mouseDown && !_lastMouseDown;
        public static bool IsMouseReleased => !_mouseDown && _lastMouseDown;
        public static bool IsMouseInWindow => _isMouseInWindow;
        public static Vector2 WindowSize => WindowCoordinateHelper.GetClientSize();
        public static bool IsSCreenFocus => WindowCoordinateHelper.IsGameWindowActive();
        private static bool _isPressed;

        // Propiedades de entrada de texto
        public static string InputText
        {
            get => _inputText;
            internal set => _inputText = value ?? string.Empty;
        }

        public static char? LastKeyPressed
        {
            get => _lastKeyPressed;
            internal set => _lastKeyPressed = value;
        }

        public static bool BlockOtherControls { get; internal set; } = false;
   
        /// <summary>
        /// Inicializa el sistema de eventos UI
        /// </summary>
        internal static void Initialize(uint processId)
        {
            WindowCoordinateHelper.Initialize(processId);
        }

        /// <summary>
        /// Actualiza el estado del input
        /// </summary>
        internal static void UpdateInput(Vector2 globalMousePos, bool? mouseDown = null)
        {
            // Guardar estado anterior
            _lastMousePosition = _mousePosition;
            if (mouseDown.HasValue)
            {
                _lastMouseDown = _mouseDown;
            }

            // Actualizar posición global
            _globalMousePosition = globalMousePos;

            // Convertir a coordenadas locales de la ventana
            _mousePosition = WindowCoordinateHelper.GlobalToLocal(_globalMousePosition);
            _isMouseInWindow = WindowCoordinateHelper.IsMouseInWindow(_mousePosition);

            // Actualizar estado del botón del mouse si se proporciona
            if (mouseDown.HasValue)
            {
                _mouseDown = mouseDown.Value && _isMouseInWindow;
            }

            // Limpiar foco si se hizo clic fuera de la ventana
            if (!_isMouseInWindow && IsMousePressed)
            {
                ClearFocus();
            }
        }

        /// <summary>
        /// Procesa los eventos para un control específico
        /// </summary>
        internal static void ProcessEvents(UIControl control)
        {
            if (!control.Visible || !control.Enabled) return;

            bool containsMouse = control.Contains(_mousePosition);
        }

        /// <summary>
        /// Obtiene la posición del mouse normalizada (0-1)
        /// </summary>
        public static Vector2 GetNormalizedMousePosition()
        {
            var windowSize = WindowSize;
            return new Vector2(
                windowSize.X > 0 ? _mousePosition.X / windowSize.X : 0,
                windowSize.Y > 0 ? _mousePosition.Y / windowSize.Y : 0
            );
        }

        /// <summary>
        /// Obtiene la posición del mouse relativa al centro de la ventana
        /// </summary>
        public static Vector2 GetCenteredMousePosition()
        {
            var windowSize = WindowSize;
            return _mousePosition - (windowSize / 2);
        }

        /// <summary>
        /// Verifica si una tecla está presionada
        /// </summary>
        internal static bool IsKeyPressed(Keys key)
        {
            return InputSystem.GetKeyDown(key);
        }

        /// <summary>
        /// Obtiene el control con foco actual
        /// </summary>
        public static UIControl GetFocusedControl() => _focusedControl;

        /// <summary>
        /// Establece el control con foco
        /// </summary>
        public static void SetFocusedControl(UIControl control)
        {
            if (_focusedControl != control)
            {
                _focusedControl = control;
            }
        }

        /// <summary>
        /// Limpia el control con foco actual
        /// </summary>
        public static void ClearFocus()
        {
            SetFocusedControl(null);
        }

        /// <summary>
        /// Verifica si se hizo clic con el mouse (presionado y luego liberado)
        /// </summary>
        internal static bool IsMouseClicked()
        {
            if (IsMousePressed && !_isPressed)
            {
                _isPressed = true;
            }
            else if (!IsMousePressed && _isPressed)
            {
                _isPressed = false;
                return true;
            }

            return false;
        }

        internal static float GetMouseWheelDelta()
        {
            return _mouseWheelDelta;
        }

        internal static void UpdateWell(bool isHorizontalWheel, int wheelDelta)
        {
            _mouseWheelDelta = wheelDelta;
        }
    }
}