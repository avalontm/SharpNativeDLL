using System.Diagnostics;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static class InputSystem
    {
        private static Dictionary<int, KeyStateInfo> _keyStates = new Dictionary<int, KeyStateInfo>();
        private static MouseStateInfo _mouseState = new MouseStateInfo();
        private static uint _processId;
        private static Stopwatch _frameTimer = new Stopwatch();
        static bool _isInitializing = false;

        /// <summary>
        /// Información extendida del estado de una tecla
        /// </summary>
        private struct KeyStateInfo
        {
            public bool CurrentState;
            public bool PreviousState;
            public long PressTimestamp;
            public long ReleaseTimestamp;
            public int RepeatCount;
        }

        /// <summary>
        /// Información del estado del mouse
        /// </summary>
        private struct MouseStateInfo
        {
            public int X;
            public int Y;
            public int PreviousX;
            public int PreviousY;
            public bool HasMoved;
        }

        /// <summary>
        /// Inicializa el sistema de input para un proceso específico
        /// </summary>
        public static void Initialize(uint processId)
        {
            if (_isInitializing) return;

            _processId = processId;
            UIEventSystem.Initialize(processId);

            _frameTimer.Start();

            // Inicializar estados para todas las teclas definidas en el enum
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                _keyStates[(int)key] = new KeyStateInfo
                {
                    CurrentState = false,
                    PreviousState = false,
                    PressTimestamp = 0,
                    ReleaseTimestamp = 0,
                    RepeatCount = 0
                };
            }

            // Inicializar estado del mouse
            _mouseState = new MouseStateInfo
            {
                X = 0,
                Y = 0,
                PreviousX = 0,
                PreviousY = 0,
                HasMoved = false
            };

            KeyboardMouseMonitor.StartMonitoring(processId, OnInputEvent);
            _isInitializing = true;
        }

        public static void Shutdown()
        {
            KeyboardMouseMonitor.StopMonitoring();
            _frameTimer.Stop();
        }

        /// <summary>
        /// Actualiza los estados de las teclas (debe llamarse una vez por frame)
        /// </summary>
        public static void Update()
        {
            // Actualizar estados previos del mouse
            _mouseState.PreviousX = _mouseState.X;
            _mouseState.PreviousY = _mouseState.Y;
            _mouseState.HasMoved = false;

            // Actualizar estados de las teclas
            var keyCodes = new List<int>(_keyStates.Keys);
            foreach (var keyCode in keyCodes)
            {
                var state = _keyStates[keyCode];
                state.PreviousState = state.CurrentState;

                // Resetear contador de repetición si la tecla no está presionada
                if (!state.CurrentState)
                {
                    state.RepeatCount = 0;
                }

                _keyStates[keyCode] = state;
            }
        }

        private static void OnInputEvent(KeyboardMouseMonitor.InputEventArgs e)
        {
            switch (e.Type)
            {
                case KeyboardMouseMonitor.InputType.Keyboard:
                    HandleKeyboardEvent(e);
                    break;

                case KeyboardMouseMonitor.InputType.MouseButton:
                    HandleMouseButtonEvent(e);
                    break;

                case KeyboardMouseMonitor.InputType.MouseWheel:
                    HandleMouseWheelEvent(e);
                    break;

                case KeyboardMouseMonitor.InputType.MouseMove:
                    HandleMouseMoveEvent(e);
                    break;
            }
        }

        private static void HandleKeyboardEvent(KeyboardMouseMonitor.InputEventArgs e)
        {
            if (!_keyStates.TryGetValue(e.KeyCode, out var state))
            {
                state = new KeyStateInfo();
            }

            state.CurrentState = e.IsPressed;

            if (e.IsPressed)
            {
                if (!state.PreviousState) // Primera vez presionada
                {
                    state.PressTimestamp = _frameTimer.ElapsedMilliseconds;
                    state.RepeatCount = 1;
                }
                else // Mantenida presionada
                {
                    state.RepeatCount++;
                }
            }
            else
            {
                state.ReleaseTimestamp = _frameTimer.ElapsedMilliseconds;
                state.RepeatCount = 0;
            }

            _keyStates[e.KeyCode] = state;
        }

        private static void HandleMouseWheelEvent(KeyboardMouseMonitor.InputEventArgs e)
        {
            UIEventSystem.UpdateWell(e.IsHorizontalWheel, e.WheelDelta);
        }

        private static void HandleMouseButtonEvent(KeyboardMouseMonitor.InputEventArgs e)
        {
            // Actualizar posición del mouse
            _mouseState.X = e.MouseX;
            _mouseState.Y = e.MouseY;

            // Tratar los botones del mouse como teclas especiales
            if (!_keyStates.TryGetValue(e.KeyCode, out var state))
            {
                state = new KeyStateInfo();
            }

            state.CurrentState = e.IsPressed;

            if (e.IsPressed)
            {
                if (!state.PreviousState)
                {
                    state.PressTimestamp = _frameTimer.ElapsedMilliseconds;
                    state.RepeatCount = 1;
                }
                else
                {
                    state.RepeatCount++;
                }
            }
            else
            {
                state.ReleaseTimestamp = _frameTimer.ElapsedMilliseconds;
                state.RepeatCount = 0;
            }

            _keyStates[e.KeyCode] = state;

            UIEventSystem.UpdateInput(GetMousePosition(), GetMouseButtonDown(MouseButton.Left));
        }

        private static void HandleMouseMoveEvent(KeyboardMouseMonitor.InputEventArgs e)
        {
            _mouseState.X = e.MouseX;
            _mouseState.Y = e.MouseY;
            _mouseState.HasMoved = true;
            UIEventSystem.UpdateInput(GetMousePosition());
        }

        /// <summary>
        /// Obtiene el estado detallado de una tecla
        /// </summary>
        public static KeyState GetKeyState(Keys key)
        {
            int keyCode = (int)key;
            if (!_keyStates.TryGetValue(keyCode, out var state))
                return KeyState.Up;

            if (state.CurrentState && !state.PreviousState)
                return KeyState.Pressed;
            if (state.CurrentState && state.PreviousState)
                return KeyState.Holding;
            if (!state.CurrentState && state.PreviousState)
                return KeyState.Released;

            return KeyState.Up;
        }

        /// <summary>
        /// Verifica si la tecla está siendo presionada o mantenida
        /// </summary>
        public static bool GetKey(Keys key)
            => GetKeyState(key) == KeyState.Holding || GetKeyState(key) == KeyState.Pressed;

        /// <summary>
        /// Verifica si la tecla acaba de ser presionada (solo primer frame)
        /// </summary>
        public static bool GetKeyDown(Keys key)
            => GetKeyState(key) == KeyState.Pressed;

        /// <summary>
        /// Verifica si la tecla acaba de ser liberada (solo primer frame)
        /// </summary>
        public static bool GetKeyUp(Keys key)
            => GetKeyState(key) == KeyState.Released;

        /// <summary>
        /// Obtiene el tiempo en milisegundos que la tecla ha estado presionada
        /// </summary>
        public static long GetKeyPressDuration(Keys key)
        {
            int keyCode = (int)key;
            if (!_keyStates.TryGetValue(keyCode, out var state) || !state.CurrentState)
                return 0;

            return _frameTimer.ElapsedMilliseconds - state.PressTimestamp;
        }

        /// <summary>
        /// Obtiene el número de veces que la tecla ha repetido desde que fue presionada
        /// </summary>
        public static int GetKeyRepeatCount(Keys key)
        {
            int keyCode = (int)key;
            if (!_keyStates.TryGetValue(keyCode, out var state))
                return 0;

            return state.RepeatCount;
        }

        // ==================== MÉTODOS ESPECÍFICOS DEL MOUSE ====================

        /// <summary>
        /// Obtiene el estado de un botón del mouse
        /// </summary>
        public static KeyState GetMouseButtonState(MouseButton button)
        {
            int keyCode = (int)button;
            if (!_keyStates.TryGetValue(keyCode, out var state))
                return KeyState.Up;

            if (state.CurrentState && !state.PreviousState)
                return KeyState.Pressed;
            if (state.CurrentState && state.PreviousState)
                return KeyState.Holding;
            if (!state.CurrentState && state.PreviousState)
                return KeyState.Released;

            return KeyState.Up;
        }

        /// <summary>
        /// Verifica si el botón del mouse está siendo presionado
        /// </summary>
        public static bool GetMouseButton(MouseButton button)
            => GetMouseButtonState(button) == KeyState.Holding || GetMouseButtonState(button) == KeyState.Pressed;

        /// <summary>
        /// Verifica si el botón del mouse acaba de ser presionado
        /// </summary>
        public static bool GetMouseButtonDown(MouseButton button)
            => GetMouseButtonState(button) == KeyState.Pressed;

        /// <summary>
        /// Verifica si el botón del mouse acaba de ser liberado
        /// </summary>
        public static bool GetMouseButtonUp(MouseButton button)
            => GetMouseButtonState(button) == KeyState.Released;

        /// <summary>
        /// Obtiene la posición actual del mouse
        /// </summary>
        public static Vector2 GetMousePosition()
            => new (_mouseState.X, _mouseState.Y);

        /// <summary>
        /// Obtiene el delta de movimiento del mouse desde el último frame
        /// </summary>
        public static Vector2 GetMouseDelta()
            => new (_mouseState.X - _mouseState.PreviousX, _mouseState.Y - _mouseState.PreviousY);

        /// <summary>
        /// Verifica si el mouse se ha movido en este frame
        /// </summary>
        public static bool HasMouseMoved()
            => _mouseState.HasMoved;
    }

    /// <summary>
    /// Estados posibles de una tecla
    /// </summary>
    public enum KeyState
    {
        Up,       // Tecla no presionada
        Pressed,  // Tecla acaba de ser presionada
        Holding,  // Tecla mantenida presionada
        Released  // Tecla acaba de ser liberada
    }

    /// <summary>
    /// Enumeración para los botones del mouse
    /// </summary>
    public enum MouseButton
    {
        Left = 0x01,
        Right = 0x02,
        Middle = 0x04,
        XButton1 = 0x05,
        XButton2 = 0x06
    }
}