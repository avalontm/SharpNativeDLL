using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace AvalonInjectLib
{
    internal unsafe static class ProcessManager
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        internal delegate uint ThreadStartDelegate(IntPtr lpParam);

        private const uint ACCESS_MASK = 0x001F0FFF; // PROCESS_ALL_ACCESS

        internal static IntPtr GetMainWindowHandle(uint processId)
        {
            IntPtr windowHandle = IntPtr.Zero;
            WinInterop.EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                WinInterop.GetWindowThreadProcessId(hWnd, out var windowProcessId);
                if (windowProcessId == processId)
                {
                    windowHandle = hWnd;
                    return false; // Detener la enumeración
                }
                return true; // Continuar enumeración
            }, IntPtr.Zero);

            return windowHandle;
        }

        /// <summary>
        /// Cierra un handle de proceso de forma segura
        /// </summary>
        internal static bool CloseProcessHandle(IntPtr hProcess)
        {
            if (hProcess == IntPtr.Zero)
                return false;

            bool result = WinInterop.CloseHandle(hProcess);
            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                Logger.Warning($"Error al cerrar handle: 0x{error:X8}");
            }
            return result;
        }

        /// <summary>
        /// Obtiene el handle del módulo actual (DLL inyectada)
        /// </summary>
        internal static IntPtr GetCurrentModuleHandle()
        {
            try
            {
                // Intenta el método más directo (soporte NativeAOT)
                var handle = WinInterop.GetCurrentModule();
                if (handle != IntPtr.Zero)
                    return handle;

                // Fallback para versiones más antiguas
                return WinInterop.GetModuleHandle(null);
            }
            catch
            {
                // Último recurso: usa el stack trace para encontrar la DLL
                return GetModuleHandleFromStackTrace();
            }
        }

        /// <summary>
        /// Método alternativo para obtener el handle cuando los otros fallan
        /// </summary>
        private static IntPtr GetModuleHandleFromStackTrace()
        {
            try
            {
                // Obtiene la ruta de la DLL actual
                var dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                // Convierte a nombre de módulo (sin ruta completa)
                var moduleName = System.IO.Path.GetFileName(dllPath);

                return WinInterop.GetModuleHandle(moduleName);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        internal static IntPtr Open(uint processId)
        {
            return WinInterop.OpenProcess(ACCESS_MASK, false, processId);
        }


        /// <summary>
        /// Hook automático en el proceso actual
        /// </summary>
        /// <param name="targetAddress">Dirección a hookear</param>
        /// <param name="hookFunction">Función de reemplazo</param>
        /// <param name="originalBytes">Bytes originales (salida)</param>
        /// <returns>True si el hook fue exitoso</returns>
        internal static bool InstallHook(IntPtr targetAddress, IntPtr hookFunction, out byte[] originalBytes)
        {
            originalBytes = null;

            try
            {
                // Validar direcciones
                if (targetAddress == IntPtr.Zero || hookFunction == IntPtr.Zero)
                    return false;

                // Leer bytes originales (JMP de 5 bytes o call de 6 bytes, usamos 6 para mayor compatibilidad)
                int hookSize = 6;
                originalBytes = new byte[hookSize];

                if (!WinInterop.ReadProcessMemory(WinInterop.GetCurrentProcess(), targetAddress, originalBytes, hookSize, out _))
                    return false;

                // Crear el JMP hacia nuestra función
                byte[] jumpBytes = CreateJumpBytes(targetAddress, hookFunction);

                // Cambiar protección de memoria
                if (!WinInterop.VirtualProtect(targetAddress, (uint)jumpBytes.Length, WinInterop.PAGE_EXECUTE_READWRITE, out uint oldProtect))
                    return false;

                // Escribir el hook
                bool success = WinInterop.WriteProcessMemory(WinInterop.GetCurrentProcess(), targetAddress, jumpBytes, jumpBytes.Length, out _);

                // Restaurar protección original
                WinInterop.VirtualProtect(targetAddress, (uint)jumpBytes.Length, oldProtect, out _);

                // Flush instruction cache para asegurar que los cambios surtan efecto
                WinInterop.FlushInstructionCache(WinInterop.GetCurrentProcess(), targetAddress, (uint)jumpBytes.Length);

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error en InstallHook: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Restaurar hook original
        /// </summary>
        /// <param name="targetAddress">Dirección hooked</param>
        /// <param name="originalBytes">Bytes originales</param>
        /// <returns>True si se restauró correctamente</returns>
        internal static bool UninstallHook(IntPtr targetAddress, byte[] originalBytes)
        {
            if (originalBytes == null || originalBytes.Length < 5)
                return false;

            try
            {
                // Cambiar protección
                if (!WinInterop.VirtualProtect(targetAddress, (uint)originalBytes.Length, WinInterop.PAGE_EXECUTE_READWRITE, out uint oldProtect))
                    return false;

                // Restaurar bytes originales
                bool success = WinInterop.WriteProcessMemory(WinInterop.GetCurrentProcess(), targetAddress, originalBytes, originalBytes.Length, out _);

                // Restaurar protección
                WinInterop.VirtualProtect(targetAddress, (uint)originalBytes.Length, oldProtect, out _);

                // Flush instruction cache
                WinInterop.FlushInstructionCache(WinInterop.GetCurrentProcess(), targetAddress, (uint)originalBytes.Length);

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error en UninstallHook: {ex}");
                return false;
            }
        }

        internal static void PreventUnload(IntPtr hModule)
        {
            StringBuilder path = new StringBuilder(260);
            if (WinInterop.GetModuleFileName(hModule, path, path.Capacity) > 0)
            {
                IntPtr result = WinInterop.LoadLibrary(path.ToString());
                if (result == IntPtr.Zero)
                {
                    int err = Marshal.GetLastWin32Error();
                    Logger.Error($"Error al prevenir unload del módulo: {err}");
                }
            }
        }

        /// <summary>
        /// Reanudar un thread suspendido
        /// </summary>
        internal static bool ResumeThread(IntPtr threadHandle)
        {
            return WinInterop.ResumeThread(threadHandle) != 0xFFFFFFFF;
        }

        /// <summary>
        /// Suspender un thread
        /// </summary>
        internal static bool SuspendThread(IntPtr threadHandle)
        {
            return WinInterop.SuspendThread(threadHandle) != 0xFFFFFFFF;
        }

        /// <summary>
        /// Esperar a que termine un thread con timeout
        /// </summary>
        internal static bool WaitForThread(IntPtr threadHandle, uint timeoutMs = 5000)
        {
            uint result = WinInterop.WaitForSingleObject(threadHandle, timeoutMs);
            return result == 0; // WAIT_OBJECT_0
        }

        /// <summary>
        /// Obtener el código de salida de un thread
        /// </summary>
        internal static bool GetThreadExitCode(IntPtr threadHandle, out uint exitCode)
        {
            return WinInterop.GetExitCodeThread(threadHandle, out exitCode);
        }

        internal static bool CloseThread(IntPtr threadHandle)
        {
            return WinInterop.CloseHandle(threadHandle);
        }

        internal static uint Find(string processName)
        {
            return WinInterop.FindProcessId(processName);
        }

        internal static IntPtr Module(int processId, string moduleName)
        {
            return WinInterop.GetModuleBaseEx((uint)processId, moduleName);
        }

        internal static bool IsOpen(IntPtr hProcess)
        {
            return WinInterop.IsProcessRunning(hProcess);
        }

        /// <summary>
        /// Verificar si estamos ejecutándose en el contexto correcto
        /// </summary>
        internal static bool IsInjectedContext()
        {
            try
            {
                // Verificar si podemos obtener información del proceso actual
                uint currentPid = WinInterop.GetCurrentProcessId();
                IntPtr currentHandle = WinInterop.GetCurrentProcess();

                return currentPid > 0 && currentHandle != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Crear bytes para un salto JMP de 32 bits - Versión mejorada
        /// </summary>
        private static byte[] CreateJumpBytes(IntPtr from, IntPtr to)
        {
            long offset = to.ToInt64() - from.ToInt64() - 5; // -5 por el tamaño del JMP

            // Verificar si el offset está dentro del rango de 32 bits
            if (offset > int.MaxValue || offset < int.MinValue)
            {
                // Para offsets grandes, usar JMP indirecto (no implementado en este ejemplo)
                Logger.Warning("Offset fuera de rango para JMP directo");
            }

            return new byte[]
            {
                0xE9, // JMP opcode
                (byte)(offset & 0xFF),
                (byte)((offset >> 8) & 0xFF),
                (byte)((offset >> 16) & 0xFF),
                (byte)((offset >> 24) & 0xFF)
            };
        }

    }
}