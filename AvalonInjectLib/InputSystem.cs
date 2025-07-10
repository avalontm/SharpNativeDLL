using System;
using System.Collections.Generic;
using System.Diagnostics;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static class InputSystem
    {
        private static Dictionary<int, KeyStateInfo> _keyStates = new Dictionary<int, KeyStateInfo>();
        private static uint _processId;
        private static Stopwatch _frameTimer = new Stopwatch();

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
        /// Inicializa el sistema de input para un proceso específico
        /// </summary>
        public static void Initialize(uint processId)
        {
            _processId = processId;
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

            KeyboardMonitor.StartMonitoring(processId, OnKeyboardEvent);
        }

        public static void Shutdown()
        {
            KeyboardMonitor.StopMonitoring();
            _frameTimer.Stop();
        }

        /// <summary>
        /// Actualiza los estados de las teclas (debe llamarse una vez por frame)
        /// </summary>
        public static void Update()
        {
            foreach (var keyCode in _keyStates.Keys)
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

        private static void OnKeyboardEvent(int vkCode, bool isPressed)
        {
            if (!_keyStates.ContainsKey(vkCode))
            {
                _keyStates[vkCode] = new KeyStateInfo();
            }

            var currentTime = _frameTimer.ElapsedMilliseconds;
            var state = _keyStates[vkCode];

            state.CurrentState = isPressed;

            if (isPressed)
            {
                if (!state.PreviousState)
                {
                    state.PressTimestamp = currentTime;
                    state.RepeatCount = 0;
                }
                else
                {
                    state.RepeatCount++;
                }
            }
            else
            {
                state.ReleaseTimestamp = currentTime;
            }

            _keyStates[vkCode] = state;
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
    }
}