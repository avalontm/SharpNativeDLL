using AvalonInjectLib;
using AvalonInjectLib.Scripting;
using System.Runtime.InteropServices;

namespace TargetGame
{
    public unsafe class EntryPoint
    {
        public static MoonSharpScriptLoader? _luaLoader { private set; get; }
        static MenuSystem menuSystem = new MenuSystem();

        // Control de estado mejorado
        private static bool _running = false;

        [UnmanagedCallersOnly(EntryPoint = "DllMain")]
        public static bool DllMain(IntPtr hModule, uint reason, IntPtr lpReserved)
        {
            switch ((DLL_PROCESS)reason)
            {
                case DLL_PROCESS.ATTACH:
                    Application.Run(StartLoop);
                    break;
                case DLL_PROCESS.DETACH:
                    StopLoop();
                    break;
            }

            return true;
        }

        public static void StartLoop()
        {
            if (_running) return;

            try
            {
                Initialize();

                Logger.Info("Thread principal iniciado");

                _running = true;

                while (_running)
                {
                    if (Application.GetKeyDown(Keys.Insert))
                    {
                        menuSystem.Toggle();
                    }
                    else if (Application.GetKeyDown(Keys.Home))
                    {
                        menuSystem.ReloadScripts();
                    }
                    _luaLoader?.UpdateAll();

                    Application.Sleep(10);

                    Application.InputUpdate();
                }

                Logger.Info("Thread principal terminado");
                Application.FreeConsole();
            }
            catch (Exception ex)
            {
                WinDialog.ShowFatalError("Error", ex.Message);
            }
        }


        private static bool Initialize()
        {
            InitializeConsole();

            InitializeScripts();

            Application.InitInput();

            
            // Inicializar renderer
            if (!InitializeGraphicsSafely())
            {
                Logger.Warning("Gráficos no disponibles, continuando en modo sin interfaz");
            }
            
            // Inicializar scripts
            _luaLoader?.InitializeAll();

 
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
            _luaLoader = new MoonSharpScriptLoader();
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

        private static void StopLoop()
        {
            if (!_running) return;

            Logger.Info("[Runtime] Deteniendo...");
            _running = false;
        }


        [UnmanagedCallersOnly(EntryPoint = "StopApp")]
        public static void StopApp()
        {
            StopLoop();
        }

    }
}