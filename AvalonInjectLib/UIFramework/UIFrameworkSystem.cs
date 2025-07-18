using static AvalonInjectLib.Structs;
namespace AvalonInjectLib.UIFramework
{
    internal static class UIFrameworkSystem
    {
        private static UIControl? _focusedControl;
        private static readonly List<Window> _windows = new();
        private static readonly HashSet<UIControl> _controls = new();

        internal static UIControl? FocusedControl => _focusedControl;
        internal static bool HasModal => _windows.Count > 0;


        internal static void SetFocus(UIControl? control)
        {
            if (control == _focusedControl) return;

            Vector2 mousePos = UIEventSystem.MousePosition;
 
            // Remover foco del control anterior
            if (_focusedControl != null)
            {
                _focusedControl.HasFocus = false;
                _focusedControl.MouseLeave?.Invoke(control, mousePos);
            }

            // Establecer nuevo foco
            _focusedControl = control;
            if (_focusedControl != null)
            {
                _focusedControl.HasFocus = true;
                _focusedControl.MouseEnter?.Invoke(control, mousePos);
            }

        }

        internal static void ClearFocusControls()
        {
            _controls.Clear();
        }

        internal static void ClearFocus()
        {
            SetFocus(null);
        }

        internal static bool IsFocusable(UIControl control)
        {
            return control.IsFocusable;
        }

        internal static void SetControl(UIControl control)
        {
            if (control == null) return;
            _controls.Add(control);
        }

        internal static bool IsValidClick()
        {
            foreach(var control in _controls.Reverse())
            {
                if(control.IsFocusable)
                {
                    SetFocus(control);
                    return true;
                }
            }

            return false;
        }
    }
}