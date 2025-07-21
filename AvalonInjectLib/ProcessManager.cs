using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AvalonInjectLib
{
    public unsafe static class ProcessManager
    {
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate uint ThreadStartDelegate(IntPtr lpParam);

        private const uint ACCESS_MASK = 0x001F0FFF; // PROCESS_ALL_ACCESS

        public static IntPtr GetMainWindowHandle(uint processId)
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
        public static bool CloseProcessHandle(IntPtr hProcess)
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
        public static IntPtr GetCurrentModuleHandle()
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

        public static IntPtr Open(uint processId)
        {
            return WinInterop.OpenProcess(ACCESS_MASK, false, processId);
        }

        public static ProcessEntry Create(string processName, string moduleName = null)
        {
            ProcessEntry moduleProcess = null;
            var processId = Find(processName);
            var hProcess = Open(processId);
            var moduleBase = Module(processId, moduleName);

            if (processId > 0 && hProcess != IntPtr.Zero && moduleBase != IntPtr.Zero)
            {
                moduleProcess = new ProcessEntry(processId, hProcess, moduleBase);
            }
            else
            {
                if (hProcess != IntPtr.Zero)
                    WinInterop.CloseHandle(hProcess);
            }
            return moduleProcess;
        }

        /// <summary>
        /// Obtiene el proceso actual (donde está inyectada la DLL)
        /// </summary>
        /// <param name="moduleName">Nombre del módulo a buscar (opcional)</param>
        /// <returns>ProcessEntry del proceso actual</returns>
        public static ProcessEntry GetCurrentProcess(string moduleName = null)
        {
            uint currentProcessId = WinInterop.GetCurrentProcessId();
            IntPtr currentHandle = WinInterop.GetCurrentProcess();
            IntPtr moduleBase = IntPtr.Zero;

            if (string.IsNullOrEmpty(moduleName))
            {
                // Obtener base del módulo principal
                moduleBase = WinInterop.GetModuleHandle(null);
            }
            else
            {
                moduleBase = Module(currentProcessId, moduleName);
            }

            if (moduleBase != IntPtr.Zero)
            {
                return new ProcessEntry(currentProcessId, currentHandle, moduleBase);
            }

            return null;
        }

        /// <summary>
        /// Función automática para manipular el proceso actual desde la DLL inyectada
        /// </summary>
        /// <param name="targetModuleName">Módulo objetivo a manipular (null = proceso principal)</param>
        /// <returns>ProcessEntry listo para usar</returns>
        public static ProcessEntry AttachToSelf(string targetModuleName = null)
        {
            return GetCurrentProcess(targetModuleName);
        }

        /// <summary>
        /// Hook automático en el proceso actual
        /// </summary>
        /// <param name="targetAddress">Dirección a hookear</param>
        /// <param name="hookFunction">Función de reemplazo</param>
        /// <param name="originalBytes">Bytes originales (salida)</param>
        /// <returns>True si el hook fue exitoso</returns>
        public static bool InstallHook(IntPtr targetAddress, IntPtr hookFunction, out byte[] originalBytes)
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
        public static bool UninstallHook(IntPtr targetAddress, byte[] originalBytes)
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

        public static void PreventUnload(IntPtr hModule)
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
        public static bool ResumeThread(IntPtr threadHandle)
        {
            return WinInterop.ResumeThread(threadHandle) != 0xFFFFFFFF;
        }

        /// <summary>
        /// Suspender un thread
        /// </summary>
        public static bool SuspendThread(IntPtr threadHandle)
        {
            return WinInterop.SuspendThread(threadHandle) != 0xFFFFFFFF;
        }

        /// <summary>
        /// Esperar a que termine un thread con timeout
        /// </summary>
        public static bool WaitForThread(IntPtr threadHandle, uint timeoutMs = 5000)
        {
            uint result = WinInterop.WaitForSingleObject(threadHandle, timeoutMs);
            return result == 0; // WAIT_OBJECT_0
        }

        /// <summary>
        /// Obtener el código de salida de un thread
        /// </summary>
        public static bool GetThreadExitCode(IntPtr threadHandle, out uint exitCode)
        {
            return WinInterop.GetExitCodeThread(threadHandle, out exitCode);
        }

        public static bool CloseThread(IntPtr threadHandle)
        {
            return WinInterop.CloseHandle(threadHandle);
        }

        public static uint Find(string processName)
        {
            return WinInterop.FindProcessId(processName);
        }

        public static IntPtr Module(uint processId, string moduleName)
        {
            return WinInterop.GetModuleBaseEx(processId, moduleName);
        }

        public static bool IsOpen(IntPtr hProcess)
        {
            return WinInterop.IsProcessRunning(hProcess);
        }

        /// <summary>
        /// Verificar si estamos ejecutándose en el contexto correcto
        /// </summary>
        public static bool IsInjectedContext()
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
        /// Obtener información detallada del proceso actual
        /// </summary>
        public static ProcessInfo GetCurrentProcessInfo()
        {
            try
            {
                uint pid = WinInterop.GetCurrentProcessId();
                IntPtr handle = WinInterop.GetCurrentProcess();
                IntPtr moduleBase = WinInterop.GetModuleHandle(null);

                // Obtener nombre del proceso
                string processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

                return new ProcessInfo
                {
                    ProcessId = pid,
                    ProcessName = processName,
                    Handle = handle,
                    ModuleBase = moduleBase
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Error obteniendo información del proceso: {ex}");
                return null;
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

        /// <summary>
        /// Crear un patch de memoria temporal
        /// </summary>
        public static MemoryPatch CreateMemoryPatch(IntPtr address, byte[] newBytes)
        {
            try
            {
                // Leer bytes originales
                byte[] originalBytes = new byte[newBytes.Length];
                if (!WinInterop.ReadProcessMemory(WinInterop.GetCurrentProcess(), address, originalBytes, newBytes.Length, out _))
                    return null;

                return new MemoryPatch(address, originalBytes, newBytes);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creando memory patch: {ex}");
                return null;
            }
        }
    }

    // Clases de soporte
    public class ProcessInfo
    {
        public uint ProcessId { get; set; }
        public string ProcessName { get; set; }
        public IntPtr Handle { get; set; }
        public IntPtr ModuleBase { get; set; }
    }

    public class MemoryPatch : IDisposable
    {
        public IntPtr Address { get; private set; }
        public byte[] OriginalBytes { get; private set; }
        public byte[] PatchBytes { get; private set; }
        public bool IsApplied { get; private set; }

        public MemoryPatch(IntPtr address, byte[] originalBytes, byte[] patchBytes)
        {
            Address = address;
            OriginalBytes = originalBytes;
            PatchBytes = patchBytes;
            IsApplied = false;
        }

        public bool Apply()
        {
            if (IsApplied) return true;

            try
            {
                if (!WinInterop.VirtualProtect(Address, (uint)PatchBytes.Length, WinInterop.PAGE_EXECUTE_READWRITE, out uint oldProtect))
                    return false;

                bool success = WinInterop.WriteProcessMemory(WinInterop.GetCurrentProcess(), Address, PatchBytes, PatchBytes.Length, out _);

                WinInterop.VirtualProtect(Address, (uint)PatchBytes.Length, oldProtect, out _);
                WinInterop.FlushInstructionCache(WinInterop.GetCurrentProcess(), Address, (uint)PatchBytes.Length);

                IsApplied = success;
                return success;
            }
            catch
            {
                return false;
            }
        }

        public bool Remove()
        {
            if (!IsApplied) return true;

            try
            {
                if (!WinInterop.VirtualProtect(Address, (uint)OriginalBytes.Length, WinInterop.PAGE_EXECUTE_READWRITE, out uint oldProtect))
                    return false;

                bool success = WinInterop.WriteProcessMemory(WinInterop.GetCurrentProcess(), Address, OriginalBytes, OriginalBytes.Length, out _);

                WinInterop.VirtualProtect(Address, (uint)OriginalBytes.Length, oldProtect, out _);
                WinInterop.FlushInstructionCache(WinInterop.GetCurrentProcess(), Address, (uint)OriginalBytes.Length);

                IsApplied = !success;
                return success;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (IsApplied)
            {
                Remove();
            }
        }

     
    }
}