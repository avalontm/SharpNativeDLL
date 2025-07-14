using AvalonInjectLib;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static AvalonInjectLib.Structs;

namespace AssaultCube
{
    public unsafe class EntryPoint
    {
        // Handles
        public static ProcessEntry process { private set; get; }

        // Direcciones del juego
        public const int EntityListOffset = 0x18AC04;
        public const int ViewMatrixOffset = 0x17DFD0;
        public const int LocalPlayerOffset = 0x17E0A8;
        public const int PlayersNumOffset = 0x18AC0C;
        public const int FOVOffset = 0x18A7CC;

        // Offsets del jugador
        public const int HealthOffset = 0xEC;
        public const int ArmorOffset = 0xF0;
        public const int NameOffset = 0x205;
        public const int TeamIdOffset = 0x30C;

        // Offsets de posición 
        public const int PositionXOffset = 0x2C;
        public const int PositionYOffset = 0x30;
        public const int PositionZOffset = 0x28;

        // Offsets de posición de cabeza
        public const int HeadPositionXOffset = 0x04;
        public const int HeadPositionYOffset = 0x0C;
        public const int HeadPositionZOffset = 0x08;

        // Offsets de cámara
        public const int CameraXOffset = 0x34;
        public const int CameraYOffset = 0x38;

        /* Structura del juego */
        static List<Player> playersList = new List<Player>();

        static uint entityList;
        static int playersNum;

        static ViewMatrix viewMatrix;
        static Player localPlayer;

        // Constantes
        const uint DLL_PROCESS_ATTACH = 1;

        [UnmanagedCallersOnly(EntryPoint = "DllMain", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static bool DllMain(nint hModule, uint ul_reason_for_call, nint lpReserved)
        {
            if (ul_reason_for_call == DLL_PROCESS_ATTACH)
            {
                LibManager.DisableThreadLibraryCalls(hModule);
                CreateInjectionThread();
            }
            return true;
        }

        private static void CreateInjectionThread()
        {
            IntPtr threadHandle = ProcessManager.CreateThread(InjectionThread);

            if (threadHandle != IntPtr.Zero)
                ProcessManager.CloseThread(threadHandle);
        }

        private static void InjectionThread()
        {
            try
            {
                InitializeConsole();
                FindGameProcess();
                MainLoop();

            }
            catch (Exception ex)
            {
                WinDialog.ShowErrorDialog("Error crítico", ex.ToString());
            }
        }

        private static void InitializeConsole()
        {
            // Establecer la codificación de salida a UTF-8 (65001)
            LibManager.SetConsoleOutputCP(65001);

            //if (LibManager.AttachConsole(-1))
            LibManager.AllocConsole();

            Console.Title = "AssaultCube";
            Console.WriteLine("=== AssaultCube - V1.3.0.2 ===");
        }

        private static void FindGameProcess()
        {
            var info = InjectionInfo.GetCurrentProcessInfo();
            Logger.Info($"ProcessName: {info.ProcessName}");

            process = ProcessManager.Create(info.ProcessName);

            if (process == null)
            {
                throw new Exception("Proceso del juego no encontrado");
            }

            Logger.Info($"Proceso encontrado - PID: {process.ProcessId:X8}", "EntryPoint");
            Logger.Info($"Handle encontrado - Handle: {process.Handle:X8}", "EntryPoint");
            Logger.Info($"Modulo Base - moduleBase: {process.ModuleBase:X8}", "EntryPoint");

            //MemoryUpdate();

            Renderer.CurrentAPI = Renderer.GraphicsAPI.OpenGL;
            Renderer.InitializeGraphics(process.ProcessId);
            MenuSystem.Initialize();
            Renderer.SetRenderCallback(DrawESP);
            Renderer.SetRenderCallback(MenuSystem.Render);
        }

        private static void MainLoop()
        {
            InputSystem.Initialize(process.ProcessId);

            try
            {
                while (true)
                {
                    if (!process.IsOpen)
                    {
                        Logger.Warning("El juego ha sido cerrado", "EntryPoint");
                        break;
                    }

                    if (InputSystem.GetKeyDown(Keys.F1))
                    {

                    }

                    if (InputSystem.GetKeyDown(Keys.F2))
                    {

                    }

                    InputSystem.Update();
                    LibManager.Sleep(16); // ~60 FPS
                }
            }
            finally
            {
                InputSystem.Shutdown();
            }
        }

        // Función para leer posición del jugador
        private static Vector3 GetPlayerPosition(IntPtr entityPtr)
        {
            var x = process.Read<float>(entityPtr, PositionXOffset);
            var y = process.Read<float>(entityPtr, PositionYOffset);
            var z = process.Read<float>(entityPtr, PositionZOffset);

            return new Vector3(x, z, y);
        }

        // Función para leer posición de la cabeza
        private static Vector3 GetHeadPosition(IntPtr entityPtr)
        {
            var x = process.Read<float>(entityPtr, HeadPositionXOffset);
            var y = process.Read<float>(entityPtr, HeadPositionYOffset);
            var z = process.Read<float>(entityPtr, HeadPositionZOffset);

            return new Vector3(x, z, y);
        }

        private static void DrawESP()
        {
            try
            {
                int screenWidth = Renderer.ScreenWidth;
                int screenHeight = Renderer.ScreenHeight;

                MemoryUpdate();

                // Convertir posición del jugador local a pantalla para las líneas
                Vector2 localPlayerScreen = Vector2.Zero;
                bool localPlayerVisible = CameraUtils.WorldToScreen(localPlayer.pos, out localPlayerScreen, viewMatrix, screenWidth, screenHeight);

                Renderer.DrawText($"Matrix: {viewMatrix}", 10, 10, new UIFramework.Color(255, 0, 0, 255), 24);

                foreach (Player player in playersList)
                {
                    if (player.health <= 0)
                        continue;

                    // Posición de los pies del jugador
                    if (!CameraUtils.WorldToScreen(player.pos, out Vector2 vScreen, viewMatrix, screenWidth, screenHeight))
                        continue;

                    // Posición de la cabeza
                    Vector3 headPos = GetHeadPosition(player.entityObj);

                    if (!CameraUtils.WorldToScreen(headPos, out Vector2 vHead, viewMatrix, screenWidth, screenHeight))
                        continue;

                    // Calcular dimensiones de la caja
                    float height = vScreen.Y - vHead.Y;
                    float width = height * 0.4f; // Ratio típico para cajas de jugadores

                    if (height < 10f)
                    {
                        height = 10f;
                        width = 4f;
                    }

                    float centerX = vScreen.X - (width / 2);

                    // Texto del nombre del jugador
                    Renderer.DrawText(player.playerName, vScreen.X - 50, vScreen.Y + 5, new UIFramework.Color(255, 255, 255, 255), 14);

                    // Texto de la distancia
                    string distanceInMeter = player.distance.ToString("F1") + "m";

                    if (localPlayer.teamID != player.teamID)
                    {
                        // Línea desde el jugador local hasta el enemigo (solo si el jugador local es visible)
                        if (localPlayerVisible)
                        {
                            Renderer.DrawLine(
                                localPlayerScreen.X, localPlayerScreen.Y,
                                vScreen.X, vScreen.Y,
                                2, new UIFramework.Color(255, 0, 0, 255));
                        }

                        // Caja bordeada alrededor del enemigo
                        Renderer.DrawRect(
                            centerX, vHead.Y,
                            width, height,
                            new UIFramework.Color(255, 0, 0, 150));

                        // Texto de distancia
                        Renderer.DrawText(distanceInMeter, vScreen.X - 50, vHead.Y - 20, new UIFramework.Color(0, 255, 0, 255), 14);
                    }
                    else
                    {
                        // Jugadores del mismo equipo en verde
                        Renderer.DrawRect(
                            centerX, vHead.Y,
                            width, height,
                            new UIFramework.Color(0, 255, 0, 180));

                        Renderer.DrawText(distanceInMeter, vScreen.X - 50, vHead.Y - 20, new UIFramework.Color(0, 255, 0, 255), 14);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, "EntryPoint");
            }
        }

        // Función actualizada para leer datos de memoria
        private static void MemoryUpdate()
        {
            try
            {
                playersList.Clear();

                // Leer direcciones base
                entityList = process.Read<uint>(process.ModuleBase + EntityListOffset);
                playersNum = process.Read<int>(process.ModuleBase + PlayersNumOffset);
                var localPlayerObj = process.Read<uint>(process.ModuleBase + LocalPlayerOffset);

                // Obtener matriz de vista actualizada
                viewMatrix = process.Read<ViewMatrix>(process.ModuleBase + ViewMatrixOffset);

                // Leer datos del jugador local
                if (localPlayerObj != 0)
                {
                    var m_pos = GetPlayerPosition((IntPtr)localPlayerObj);
                    var m_teamID = process.Read<int>((IntPtr)localPlayerObj, TeamIdOffset);

                    int currentHealth = process.Read<int>((IntPtr)localPlayerObj, HealthOffset);
                    string localName = process.ReadString((IntPtr)localPlayerObj, NameOffset, 16);

                    localPlayer = new Player(
                        entityObj: (IntPtr)localPlayerObj,
                        playerName: localName,
                        pos: m_pos,
                        health: currentHealth,
                        teamID: m_teamID,
                        distance: 0
                    );
                }

                // Obtener lista de otros jugadores
                UpdateOtherPlayersList();

            }
            catch (Exception ex)
            {
                Logger.Error($"Error en MemoryUpdate: {ex.Message}", "EntryPoint");
            }
        }

        // Función actualizada para leer datos de jugadores
        private static void UpdateOtherPlayersList()
        {
            if (entityList == 0 || playersNum <= 0 || localPlayer.entityObj <= 0)
                return;

            const int entitySize = 0x4;

            for (int i = 0; i < playersNum; i++)
            {
                try
                {
                    IntPtr playerAddress = (IntPtr)(entityList + i * entitySize);
                    uint playerPtr = process.Read<uint>(playerAddress);

                    if (playerPtr == 0 || playerPtr == localPlayer.entityObj)
                        continue;

                    // Leer datos del jugador usando los offsets correctos
                    int health = process.Read<int>((IntPtr)playerPtr, HealthOffset);
                    int teamId = process.Read<int>((IntPtr)playerPtr, TeamIdOffset);
                    Vector3 position = GetPlayerPosition((IntPtr)playerPtr);
                    string name = process.ReadString((IntPtr)playerPtr, NameOffset, 16);

                    // Calcular distancia respecto al jugador local
                    float distance = Vector3.Distance(position, localPlayer.pos);

                    playersList.Add(new Player(
                        entityObj: (IntPtr)playerPtr,
                        playerName: name,
                        pos: position,
                        health: health,
                        teamID: teamId,
                        distance: distance
                    ));
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error al leer jugador {i}: {ex.Message}", "EntryPoint");
                }
            }
        }
    }

}
