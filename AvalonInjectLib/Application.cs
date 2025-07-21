namespace AvalonInjectLib
{
    public static class Application
    {
        public static bool Run<TState>(Action<TState> callBack, TState state, bool preferLocal = false)
        {
            if (callBack == null)
                return false;

            try
            {
                // Usa directamente la sobrecarga genérica de ThreadPool
                return ThreadPool.QueueUserWorkItem(callBack, state, preferLocal);
            }
            catch
            {
                return false;
            }
        }

        public static bool Run(Action callBack, bool preferLocal = false)
        {
            if (callBack == null)
                return false;

            try
            {
                if (preferLocal)
                {
                    // Para preferLocal con Action sin estado, usamos la versión genérica
                    return ThreadPool.QueueUserWorkItem<object>(_ => callBack(), null, preferLocal);
                }
                else
                {
                    // Versión clásica con WaitCallback
                    return ThreadPool.QueueUserWorkItem(_ => callBack());
                }
            }
            catch
            {
                return false;
            }
        }

        internal static bool QueueUserWorkItem<TState>(Action<TState> callBack, TState state, bool preferLocal)
        {
            if (callBack == null)
                return false;

            try
            {
                return ThreadPool.QueueUserWorkItem(callBack, state, preferLocal);
            }
            catch
            {
                return false;
            }
        }

        public static bool AllocConsole()
        {
            bool status = WinInterop.AllocConsole();

            if (status)
            {
                ConsoleManager.DisableQuickEdit();
            }
            return status;
        }

        public static bool AttachConsole(int dwProcessId)
        {
            return WinInterop.AttachConsole(dwProcessId);
        }

        public static bool FreeConsole()
        {
            return WinInterop.AttachConsole(-1);
        }

        public static bool SetConsoleOutputCP(int wCodePageID)
        {
            return WinInterop.SetConsoleOutputCP((uint)wCodePageID);
        }

        public static void Sleep(int dwMilliseconds)
        {
            WinInterop.Sleep((uint)dwMilliseconds);
        }


        /// <summary>
        /// Obtiene el handle de un módulo (DLL) cargado en el proceso actual
        /// </summary>
        /// <param name="lpModuleName">Nombre del módulo. Si es null, retorna el handle del ejecutable actual</param>
        /// <returns>Handle del módulo o IntPtr.Zero si no se encuentra</returns>
        public static IntPtr GetModuleHandle(string lpModuleName)
        {
            return WinInterop.GetModuleHandle(lpModuleName);
        }

        public static bool InitInput()
        {
            InputSystem.Initialize();
            return true;
        }

        public static void InputUpdate()
        {
            InputSystem.Update();
        }

        public static bool GetKeyDown(Keys key)
        {
           return InputSystem.GetKeyDown(key);
        }
    }
}
