using AvalonInjectLib;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static AvalonInjectLib.Structs;
using static AvalonInjectLib.UIFramework;

namespace SharpNativeDLL
{
    public unsafe class EntryPoint
    {
        // Handles
        public static IntPtr hProcess { private set; get; } = IntPtr.Zero;
        public static uint processId { private set; get; } = 0;

        // Direcciones del juego C (ejemplo)
        static IntPtr PLAYER_BASE = IntPtr.Zero;
        static readonly IntPtr HEALTH_OFFSET = (IntPtr)0xEC;
        static readonly IntPtr AMMOUNT_OFFSET = (IntPtr)0x140;
        static readonly IntPtr PACK_OFFSET = (IntPtr)0x11C;
        static readonly IntPtr BOOM_OFFSET = (IntPtr)0x144;
        static readonly IntPtr NAME_OFFSET = (IntPtr)0x205;

        static readonly IntPtr MOUSEX_OFFSET = (IntPtr)0x04;
        static readonly IntPtr MOUSEY_OFFSET = (IntPtr)0x08;
        static readonly IntPtr MOUSEZ_OFFSET = (IntPtr)0x0C;

        static readonly IntPtr POSX_OFFSET = (IntPtr)0x28;
        static readonly IntPtr POSY_OFFSET = (IntPtr)0x2C;
        static readonly IntPtr POSZ_OFFSET = (IntPtr)0x30;

        static readonly IntPtr CAMERAX_OFFSET = (IntPtr)0x34;
        static readonly IntPtr CAMERAY_OFFSET = (IntPtr)0x38;

        // Constantes
        const uint DLL_PROCESS_ATTACH = 1;

        [UnmanagedCallersOnly(EntryPoint = "DllMain", CallConvs = new[] { typeof(CallConvStdcall) })]
        public static bool DllMain(nint hModule, uint ul_reason_for_call, nint lpReserved)
        {
            if (ul_reason_for_call == DLL_PROCESS_ATTACH)
            {
                WinInterop.DisableThreadLibraryCalls(hModule);
                CreateInjectionThread();
            }
            return true;
        }

        private static void CreateInjectionThread()
        {
            nint threadHandle = WinInterop.CreateThread(
                nint.Zero,
                0,
                InjectionThread,
                nint.Zero,
                0,
                out _);

            if (threadHandle != nint.Zero)
                WinInterop.CloseHandle(threadHandle);
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
            WinInterop.SetConsoleOutputCP(65001);

            //if (WinInterop.AttachConsole(-1))
            WinInterop.AllocConsole();

            Console.Title = "Game Hacking Tool";
            Console.WriteLine("=== HERRAMIENTA DE HACKEAR JUEGO ===");
            Console.WriteLine("Teclas:\n1 - Mostrar stats\n2 - Modificar vida\n3 - Modificar balas\n5 - Salir");
        }

        private static void FindGameProcess()
        {
            processId = WinInterop.FindProcessId("ac_client.exe");

            if (processId == 0)
            {
                Console.WriteLine("Proceso del juego no encontrado");
                return;
            }

            Console.WriteLine($"processId: {processId}");

            // 2. Obtener handle al proceso
            hProcess = WinInterop.OpenProcess(WinInterop.PROCESS_ALL_ACCESS, false, processId);

            if (hProcess == IntPtr.Zero)
            {
                Console.WriteLine($"Error al abrir proceso (0x{WinInterop.GetLastError():X8})");
                return;
            }

            IntPtr moduleBase = WinInterop.GetModuleBaseEx(processId, "ac_client.exe");

            Console.WriteLine($"Proceso encontrado - PID: {processId:X8}");
            Console.WriteLine($"Modulo Base - moduleBase: {moduleBase:X8}");

            PLAYER_BASE = MemoryManager.ResolvePointer(hProcess, moduleBase + 0x17E0A8, 0);

            Console.WriteLine($"PLAYER_BASE: {PLAYER_BASE:X8}");

            // Para OpenGL (en tu hook de wglSwapBuffers)
            Renderer.CurrentAPI = Renderer.GraphicsAPI.OpenGL;
            Renderer.InitializeGraphics(processId);
            MenuSystem.Initialize();
        }

        private static void MainLoop()
        {
            while (true)
            {
                if (!WinInterop.IsProcessRunning(hProcess))
                {
                    Console.WriteLine("El juego ha sido cerrado");
                    break;
                }

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.D1:
                            ShowPlayerStats();
                            break;

                        case ConsoleKey.D2:
                            ModifyHealth();
                            break;

                        case ConsoleKey.D3:
                            ModifyAmmount();
                            break;
                        case ConsoleKey.D5:
                            return;
                    }
                }

                WinInterop.Sleep(50);
                
            }
        }

        private static void ShowPlayerStats()
        {
            try
            {
                int health = MemoryManager.Read<int>(hProcess, PLAYER_BASE + HEALTH_OFFSET);
                int ammount = MemoryManager.Read<int>(hProcess, PLAYER_BASE + AMMOUNT_OFFSET);
                // int exp = MemoryManager.Read<int>(hProcess, PLAYER_BASE + EXP_OFFSET);

                //string name = MemoryManager.ReadString(hProcess, PLAYER_BASE + NAME_OFFSET, 50);

                Console.WriteLine($"\n=== ESTADO DEL JUGADOR ===");
                // Console.WriteLine($"Nombre: {name}");
                Console.WriteLine($"Vida: {health}");
                Console.WriteLine($"Balas: {ammount}");
                Console.WriteLine($"OpenGL Hook: {(OpenGLHook.Initialized ? "✓ Activo" : "✗ Inactivo")}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        private static void ModifyHealth()
        {
            Console.Write("\nNuevo valor de vida: ");
            if (int.TryParse(Console.ReadLine(), out int newHealth))
            {
                MemoryManager.Write(hProcess, PLAYER_BASE + HEALTH_OFFSET, newHealth);
                Console.WriteLine("Vida actualizada!");
            }
        }

        private static void ModifyAmmount()
        {

            Console.Write("\nNuevo valor de municiones: ");
            if (int.TryParse(Console.ReadLine(), out int newAmmount))
            {
                MemoryManager.Write(hProcess, PLAYER_BASE + AMMOUNT_OFFSET, newAmmount);
                Console.WriteLine("municiones actualizada!");
            }
        }
    }
}
