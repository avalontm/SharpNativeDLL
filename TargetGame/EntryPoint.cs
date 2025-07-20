using AvalonInjectLib;
using AvalonInjectLib.Scripting;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using static AvalonInjectLib.ProcessManager;

namespace TargetGame
{
    public unsafe class EntryPoint
    {  
        // Delegado para evitar GC
        private static ThreadStartDelegate _mainThreadDelegate;
        private static IntPtr _mainThreadHandle = IntPtr.Zero;

        static AvalonEngine Engine { set; get; } = new AvalonEngine();
        public static MoonSharpScriptLoader? _luaLoader { private set; get; }
        static MenuSystem menuSystem = new MenuSystem();

        // Control de estado mejorado
        private static bool _isRunning = false;
        private static bool _isInitialized = false;

        // Constantes
        const uint DLL_PROCESS_ATTACH = 1;
        const uint DLL_THREAD_ATTACH = 2;
        const uint DLL_THREAD_DETACH = 3;
        const uint DLL_PROCESS_DETACH = 4;

        [UnmanagedCallersOnly(EntryPoint = "DllMain")]
        public static bool DllMain(nint hModule, uint ul_reason_for_call, nint lpReserved)
        {
            switch (ul_reason_for_call)
            {
                case DLL_PROCESS_ATTACH:
                    LibManager.DisableThreadLibraryCalls(hModule);
                    return HandleProcessAttach(hModule);

                case DLL_PROCESS_DETACH:
                case DLL_THREAD_ATTACH:
                case DLL_THREAD_DETACH:
                    break;
            }
            return true;
        }

        private static bool HandleProcessAttach(nint hModule)
        {
            // Crear delegado e iniciarlo
            _mainThreadDelegate = new ThreadStartDelegate(MainInjectionThread);

            // Obtener puntero nativo del delegado
            IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(_mainThreadDelegate);
            // Crear thread nativo inmediatamente usando TU método
            _mainThreadHandle = ProcessManager.CreateNativeThread(funcPtr, hModule);

            return _mainThreadHandle != IntPtr.Zero;
        }

        private static uint MainInjectionThread(IntPtr param)
        {
            try
            {
                // Registrar el thread principal
                PreventUnload(param);

                // Inicializar consola y logger
                InitializeConsole();

                Logger.Info("Thread principal iniciado");

                if (!Initialize())
                {
                    Logger.Error("Error en la inicialización");
                    return 0;
                }

                _isInitialized = true;
                _isRunning = true;

                long count = 0;

                while (_isRunning)
                {
                    //_luaLoader.UpdateAll();

                    Logger.Debug($"Tiempo transcurrido: {count++} segundos", "EntryPoint");

                    LibManager.Sleep(16);
                }

                Logger.Info("Thread principal terminado");

                return 1;
            }
            catch (Exception ex)
            {
                WinDialog.ShowFatalError("Error", ex.Message);
                return 0;

            }
        }


        private static bool Initialize()
        {
            InitializeScripts();

            var process = ProcessManager.AttachToSelf();
            if (process == null)
            {
                Logger.Error("No se pudo attachear al proceso actual");
                return false;
            }

            Engine.SetProcess(process);
            Logger.Info($"Proceso - PID: {process.ProcessId:X8}, Handle: {process.Handle:X8}, ModuleBase: {process.ModuleBase:X8}");

            /*
            // 3. Inicializar renderer y menú de manera MÁS SEGURA
            if (!InitializeGraphicsSafely())
            {
                Logger.Warning("Gráficos no disponibles, continuando en modo sin interfaz");
                // No es crítico - el cheat puede funcionar sin UI
            }
            */

            Logger.Info("Inicialización completada exitosamente");
            return true;
        }

        private static bool InitializeGraphicsSafely()
        {
            // Detectar API disponible de manera más conservadora
            Renderer.CurrentAPI = DetectSafeGraphicsAPI();

            if (Renderer.CurrentAPI == Renderer.GraphicsAPI.None)
            {
                Logger.Info("No se detectó API de gráficos, modo consola únicamente");
                return false;
            }

            if (!Renderer.InitializeGraphics()) // 2 segundos máximo
            {
                Logger.Error("Error inicializando gráficos");
                return false;
            }

            menuSystem.Initialize();
            Renderer.SetRenderCallback(menuSystem.Render);

            // 4. Inicializar scripts después de gráficos
            _luaLoader?.InitializeAll();

            return true;
        }

        private static Renderer.GraphicsAPI DetectSafeGraphicsAPI()
        {
            // Verificar OpenGL de manera más segura
            if (IsModuleLoaded("opengl32.dll") || IsModuleLoaded("opengl32"))
            {
                return Renderer.GraphicsAPI.OpenGL;
            }

            // Verificar DirectX
            if (IsModuleLoaded("d3d11.dll") || IsModuleLoaded("d3d9.dll") || IsModuleLoaded("dxgi.dll"))
            {
                return Renderer.GraphicsAPI.DirectX;
            }

            return Renderer.GraphicsAPI.None;
        }

        private static bool IsModuleLoaded(string moduleName)
        {
            try
            {
                return LibManager.GetModuleHandle(moduleName) != IntPtr.Zero;
            }
            catch
            {
                return false;
            }
        }


        private static void InitializeScripts()
        {
            _luaLoader = new MoonSharpScriptLoader(Engine);
            _luaLoader.LoadScripts("Scripts");
            Logger.Info("Scripts inicializados correctamente");
        }

        private static void InitializeConsole()
        {
            // Establecer codificación UTF-8
            LibManager.SetConsoleOutputCP(65001);
            LibManager.AllocConsole();

            Console.Title = "AvalonInjectLib - Universal Memory Cheat";
            Console.WriteLine("=== Universal Memory Cheat - V1.0.0.1 ===");
            Console.WriteLine("Estado: Inyectado correctamente");
            Console.WriteLine("Presiona INSERT para toggle del menú");
            Console.WriteLine("Presiona END para cerrar el cheat");
            Console.WriteLine("Presiona F1 para debug info");
            Console.WriteLine("=====================================");

            Logger.Info("Consola inicializada");
        }

        // Propiedades útiles para debugging
        public static bool IsInitialized => _isInitialized;
        public static bool IsRunning => _isRunning;
    }
}