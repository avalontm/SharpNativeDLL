using AvalonInjectLib;
using AvalonInjectLib.Scripting;
using System.Runtime.InteropServices;

namespace TargetGame
{
    public unsafe class EntryPoint
    {
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

                    return Application.Run(Main);

                case DLL_PROCESS_DETACH:
                case DLL_THREAD_ATTACH:
                case DLL_THREAD_DETACH:
                    break;
            }
            return true;
        }

        private static void Main()
        {
            try
            {
                // Inicializar consola y logger
                InitializeConsole();

                Logger.Info("Thread principal iniciado");

                if (!Initialize())
                {
                    Logger.Error("Error en la inicialización");
                    return;
                }

                _isInitialized = true;
                _isRunning = true;

                while (_isRunning)
                {
                    _luaLoader.UpdateAll();
                    Thread.Sleep(10); // Reducir uso de CPU
                }

                Logger.Info("Thread principal terminado");

            }
            catch (Exception ex)
            {
                WinDialog.ShowFatalError("Error", ex.Message);
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


            // 3. Inicializar renderer y menú de manera MÁS SEGURA
            if (!InitializeGraphicsSafely())
            {
                Logger.Warning("Gráficos no disponibles, continuando en modo sin interfaz");
            }


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
                return Application.GetModuleHandle(moduleName) != IntPtr.Zero;
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
            Application.SetConsoleOutputCP(65001);
            Application.AllocConsole();

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