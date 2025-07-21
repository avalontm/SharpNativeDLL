using AvalonInjectLib;
using System.Runtime.InteropServices;

namespace TargetGame
{
    public unsafe class EntryPoint
    {
        //public static MoonSharpScriptLoader? _luaLoader { private set; get; }
        //static MenuSystem menuSystem = new MenuSystem();

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

        private static readonly List<(float X, float Y)> Waypoints = new()
        {
            (-0.64f, 0.56f),  // Posición 1
            (0.60f, 0.58f),   // Posición 2
            (0.67f, -0.56f),  // Posición 3
            (-0.65f, -0.49f)  // Posición 4
        };

        public static void StartLoop()
        {
            if (_running) return;

            try
            {
                InitializeConsole();
                Logger.Info("Thread principal iniciado");

                _running = true;

                int currentWaypointIndex = 0;

                while (_running)
                {
                    var (x, y) = Waypoints[currentWaypointIndex];

                    Logger.Info($"Moviendo a waypoint {currentWaypointIndex}: ({x}, {y})");
                    // Llamada a tu función con las coordenadas
                    InternalFunctionExecutor.CallFunction(0xF01220, x, y);

                    currentWaypointIndex = (currentWaypointIndex + 1) % Waypoints.Count;

                    Thread.Sleep(1000); // Ajusta según frecuencia deseada
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
            InitializeScripts();

            /*
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

            // menuSystem.Initialize();
            // Renderer.SetRenderCallback(menuSystem.Render);

            // 4. Inicializar scripts después de gráficos
            // _luaLoader?.InitializeAll();

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
            // _luaLoader = new MoonSharpScriptLoader(Engine);
            // _luaLoader.LoadScripts("Scripts");
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