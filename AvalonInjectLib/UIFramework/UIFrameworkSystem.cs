using static AvalonInjectLib.Structs;
namespace AvalonInjectLib.UIFramework
{
    public static class UIFrameworkSystem
    {
        private static UIControl? _focusedControl;
        private static readonly List<Window> _windows = new();
        private static readonly HashSet<UIControl> _controls = new();

        public static UIControl? FocusedControl => _focusedControl;
        public static bool HasModal => _windows.Count > 0;


        public static void SetFocus(UIControl? control)
        {
            if (control == _focusedControl) return;

            Vector2 mousePos = UIEventSystem.MousePosition;
 
            // Remover foco del control anterior
            if (_focusedControl != null)
            {
                _focusedControl.HasFocus = false;
                _focusedControl.MouseLeave?.Invoke(mousePos);
            }

            // Establecer nuevo foco
            _focusedControl = control;
            if (_focusedControl != null)
            {
                _focusedControl.HasFocus = true;
                _focusedControl.MouseEnter?.Invoke(mousePos);
            }

        }

        public static void ClearFocusControls()
        {
            _controls.Clear();
        }

        public static void ClearFocus()
        {
            SetFocus(null);
        }

        public static bool IsFocusable(UIControl control)
        {
            return control.IsFocusable;
        }

        public static void SetControl(UIControl control)
        {
            if (control == null) return;
            _controls.Add(control);
        }

        public static bool IsValidClick()
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