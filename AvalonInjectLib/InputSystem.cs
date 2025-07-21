using System.Diagnostics;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    internal static class InputSystem
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
            internal bool CurrentState;
            internal bool PreviousState;
            internal long PressTimestamp;
            internal long ReleaseTimestamp;
            internal int RepeatCount;
        }

        /// <summary>
        /// Información del estado del mouse
        /// </summary>
        private struct MouseStateInfo
        {
            internal int X;
            internal int Y;
            internal int PreviousX;
            internal int PreviousY;
            internal bool HasMoved;
        }

        /// <summary>
        /// Inicializa el sistema de input para un proceso específico
        /// </summary>
        internal static void Initialize()
        {
            if (_isInitializing) return;

            _processId = (uint)Process.GetCurrentProcess().Id;
            UIEventSystem.Initialize(_processId);

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

            KeyboardMouseMonitor.StartMonitoring(_processId, OnInputEvent);
            _isInitializing = true;
        }

        internal static void Shutdown()
        {
            KeyboardMouseMonitor.StopMonitoring();
            _frameTimer.Stop();
        }

        /// <summary>
        /// Actualiza los estados de las teclas (debe llamarse una vez por frame)
        /// </summary>
        internal static void Update()
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

            // Aquí agregamos polling activo para teclado para mayor confiabilidad
            PollKeyboard();
        }

        private static void PollKeyboard()
        {
            long now = _frameTimer.ElapsedMilliseconds;

            foreach (var keyCode in _keyStates.Keys.ToList())
            {
                // Leer estado actual con GetAsyncKeyState
                short state = WinInterop.GetAsyncKeyState(keyCode);
                bool isPressed = (state & 0x8000) != 0;

                var keyState = _keyStates[keyCode];

                // Actualizar solo si cambio estado
                if (keyState.CurrentState != isPressed)
                {
                    keyState.PreviousState = keyState.CurrentState;
                    keyState.CurrentState = isPressed;

                    if (isPressed)
                    {
                        keyState.PressTimestamp = now;
                        keyState.RepeatCount = keyState.PreviousState ? keyState.RepeatCount + 1 : 1;
                    }
                    else
                    {
                        keyState.ReleaseTimestamp = now;
                        keyState.RepeatCount = 0;
                    }

                    _keyStates[keyCode] = keyState;
                }
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

                    /*
                case KeyboardMouseMonitor.InputType.MouseWheel:
                    HandleMouseWheelEvent(e);
                    break;
*/
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

            // Solo asignar teclas válidas (caracteres + Backspace/Delete)
            if (state.CurrentState)
            {
                // Convertir KeyCode a char solo si es imprimible
                char keyChar = (char)e.KeyCode;

                // Casos especiales: Backspace (8) y Delete (46) en ASCII
                if (e.KeyCode == 8 || e.KeyCode == 46) // Backspace o Delete
                {
                    UIEventSystem.LastKeyPressed = keyChar; // Borrar
                }
                // Excluir teclas no imprimibles (flechas, F1-F12, etc.)
                else if (IsValidTypingChar(keyChar) && !IsSpecialKey(e.KeyCode))
                {
                    UIEventSystem.LastKeyPressed = keyChar; // Caracter normal
                }
                // Las flechas y otras teclas especiales se ignoran
            }
        }

        private static void HandleMouseWheelEvent(KeyboardMouseMonitor.InputEventArgs e)
        {
            //UIEventSystem.UpdateWell(e.IsHorizontalWheel, e.WheelDelta);
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
        internal static KeyState GetKeyState(Keys key)
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

        private static bool IsValidTypingChar(char c)
        {
            // Solo acepta letras y números (incluye caracteres Unicode como 'ñ', 'á', etc.)
            return char.IsLetterOrDigit(c);
        }

        private static bool IsSpecialKey(int keyCode)
        {
            // Teclas a excluir: flechas, F1-F12, Ctrl, Alt, etc.
            int[] specialKeys = new int[]
            {
        37, 38, 39, 40, // Flechas (←, ↑, →, ↓)
        112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, // F1-F12
        16, 17, 18,  // Shift, Ctrl, Alt
        9, 13, 27    // Tab, Enter, Esc
            };
            return specialKeys.Contains(keyCode);
        }

        /// <summary>
        /// Verifica si la tecla está siendo presionada o mantenida
        /// </summary>
        internal static bool GetKey(Keys key)
            => GetKeyState(key) == KeyState.Holding || GetKeyState(key) == KeyState.Pressed;

        /// <summary>
        /// Verifica si la tecla acaba de ser presionada (solo primer frame)
        /// </summary>
        internal static bool GetKeyDown(Keys key)
            => GetKeyState(key) == KeyState.Pressed;

        /// <summary>
        /// Verifica si la tecla acaba de ser liberada (solo primer frame)
        /// </summary>
        internal static bool GetKeyUp(Keys key)
            => GetKeyState(key) == KeyState.Released;

        /// <summary>
        /// Obtiene el tiempo en milisegundos que la tecla ha estado presionada
        /// </summary>
        internal static long GetKeyPressDuration(Keys key)
        {
            int keyCode = (int)key;
            if (!_keyStates.TryGetValue(keyCode, out var state) || !state.CurrentState)
                return 0;

            return _frameTimer.ElapsedMilliseconds - state.PressTimestamp;
        }

        /// <summary>
        /// Obtiene el número de veces que la tecla ha repetido desde que fue presionada
        /// </summary>
        internal static int GetKeyRepeatCount(Keys key)
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
        internal static KeyState GetMouseButtonState(MouseButton button)
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
        /// Verifica si la tecla está actualmente presionada (sin importar si es el primer frame o no)
        /// </summary>
        internal static bool IsKeyPressed(Keys key)
        {
            int keyCode = (int)key;
            if (!_keyStates.TryGetValue(keyCode, out var state))
                return false;

            return state.CurrentState;
        }

        /// <summary>
        /// Verifica si el botón del mouse está actualmente presionado (sin importar si es el primer frame o no)
        /// </summary>
        internal static bool IsMouseButtonPressed(MouseButton button)
        {
            int keyCode = (int)button;
            if (!_keyStates.TryGetValue(keyCode, out var state))
                return false;

            return state.CurrentState;
        }

        /// <summary>
        /// Verifica si el botón del mouse está siendo presionado
        /// </summary>
        internal static bool GetMouseButton(MouseButton button)
            => GetMouseButtonState(button) == KeyState.Holding || GetMouseButtonState(button) == KeyState.Pressed;

        /// <summary>
        /// Verifica si el botón del mouse acaba de ser presionado
        /// </summary>
        internal static bool GetMouseButtonDown(MouseButton button)
            => GetMouseButtonState(button) == KeyState.Pressed;

        /// <summary>
        /// Verifica si el botón del mouse acaba de ser liberado
        /// </summary>
        internal static bool GetMouseButtonUp(MouseButton button)
            => GetMouseButtonState(button) == KeyState.Released;

        /// <summary>
        /// Obtiene la posición actual del mouse
        /// </summary>
        internal static Vector2 GetMousePosition()
            => new (_mouseState.X, _mouseState.Y);

        /// <summary>
        /// Obtiene el delta de movimiento del mouse desde el último frame
        /// </summary>
        internal static Vector2 GetMouseDelta()
            => new (_mouseState.X - _mouseState.PreviousX, _mouseState.Y - _mouseState.PreviousY);

        /// <summary>
        /// Verifica si el mouse se ha movido en este frame
        /// </summary>
        internal static bool HasMouseMoved()
            => _mouseState.HasMoved;
    }

    /// <summary>
    /// Estados posibles de una tecla
    /// </summary>
    internal enum KeyState
    {
        Up,       // Tecla no presionada
        Pressed,  // Tecla acaba de ser presionada
        Holding,  // Tecla mantenida presionada
        Released  // Tecla acaba de ser liberada
    }

    /// <summary>
    /// Enumeración para los botones del mouse
    /// </summary>
    internal enum MouseButton
    {
        Left = 0x01,
        Right = 0x02,
        Middle = 0x04,
        XButton1 = 0x05,
        XButton2 = 0x06
    }
}