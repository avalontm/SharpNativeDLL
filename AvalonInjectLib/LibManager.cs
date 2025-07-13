using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvalonInjectLib
{
    public static  class LibManager
    {
        public static bool AllocConsole()
        {
            return WinInterop.AllocConsole();
        }

        public static bool AttachConsole(int dwProcessId)
        {
            return WinInterop.AttachConsole(dwProcessId);
        }

        public static bool DisableThreadLibraryCalls(IntPtr hModule)
        {
            return WinInterop.DisableThreadLibraryCalls(hModule);
        }

        public static bool FreeConsole()
        {
            return WinInterop.AttachConsole(-1);
        }

        /// <summary>
        /// Libera una biblioteca (DLL) del espacio de direcciones del proceso
        /// </summary>
        /// <param name="handle">Handle de la biblioteca a liberar</param>
        public static void FreeLibrary(nint handle)
        {
            if (handle != IntPtr.Zero)
            {
                WinInterop.FreeLibrary(handle);
            }
        }

        /// <summary>
        /// Libera una biblioteca y termina el hilo actual
        /// </summary>
        /// <param name="handle">Handle de la biblioteca a liberar</param>
        /// <param name="exitCode">Código de salida del hilo</param>
        public static void FreeLibraryAndExitThread(nint handle, int exitCode)
        {
            if (handle != IntPtr.Zero)
            {
                WinInterop.FreeLibraryAndExitThread(handle, (uint)exitCode);
            }
        }

        public static bool SetConsoleOutputCP(int wCodePageID)
        {
            return WinInterop.SetConsoleOutputCP((uint)wCodePageID);
        }

        public static void Sleep(int dwMilliseconds)
        {
            WinInterop.Sleep((uint)dwMilliseconds);
        }
    }
}
