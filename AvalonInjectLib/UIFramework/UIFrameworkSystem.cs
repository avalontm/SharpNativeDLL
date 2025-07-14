using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvalonInjectLib.UIFramework
{
    internal class UIFrameworkSystem
    { // Control actualmente enfocado
        private static UIControl _focusedControl;

        // Lista de controles modales (para manejar jerarquías de focus)
        private static List<UIControl> _modalControls = new List<UIControl>();

        /// <summary>
        /// Obtiene o establece el control que actualmente tiene el foco
        /// </summary>
        public static UIControl FocusedControl
        {
            get => _focusedControl;
            set
            {
                if (_focusedControl != value)
                {
                    // Quitar el foco del control actual
                    _focusedControl?.OnFocusLost?.Invoke();

                    // Establecer nuevo control enfocado
                    _focusedControl = value;

                    // Notificar al nuevo control
                    _focusedControl?.OnFocusGained?.Invoke();
                }
            }
        }

        /// <summary>
        /// Indica si hay algún control modal activo
        /// </summary>
        public static bool HasModal => _modalControls.Count > 0;

        /// <summary>
        /// Obtiene el control modal superior (si existe)
        /// </summary>
        public static UIControl TopModal => HasModal ? _modalControls.Last() : null;

        /// <summary>
        /// Establece el foco en un control específico
        /// </summary>
        public static void SetFocus(UIControl control)
        {
            // Si hay controles modales, solo permitir enfocar controles dentro del modal superior
            if (HasModal && !IsInModalHierarchy(control))
                return;

            FocusedControl = control;
        }

        /// <summary>
        /// Quita el foco del control actual
        /// </summary>
        public static void ClearFocus()
        {
            FocusedControl = null;
        }

        /// <summary>
        /// Agrega un control modal a la jerarquía
        /// </summary>
        public static void AddModal(UIControl modal)
        {
            if (!_modalControls.Contains(modal))
            {
                _modalControls.Add(modal);

                // Si el control enfocado actual no está en la jerarquía modal, quitamos el foco
                if (FocusedControl != null && !IsInModalHierarchy(FocusedControl))
                {
                    ClearFocus();
                }
            }
        }

        /// <summary>
        /// Remueve un control modal de la jerarquía
        /// </summary>
        public static void RemoveModal(UIControl modal)
        {
            _modalControls.Remove(modal);
        }

        /// <summary>
        /// Verifica si un control está dentro de la jerarquía modal actual
        /// </summary>
        private static bool IsInModalHierarchy(UIControl control)
        {
            if (control == null) return false;
            if (!HasModal) return true;

            // Buscar hacia arriba en la jerarquía de padres
            UIControl current = control;
            while (current != null)
            {
                if (_modalControls.Contains(current))
                    return true;

                // Si el control está dentro de una ventana modal
                if (current is Window window && window.IsModal)
                    return true;

                // Para controles dentro de otros controles
                current = current.Parent;
            }

            return false;
        }
    }
}
