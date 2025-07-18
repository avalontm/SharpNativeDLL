﻿using AvalonInjectLib;
using AvalonInjectLib.Scripting;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TargetGame
{
    public class EntryPoint
    {
        static AvalonEngine Engine { set; get; } = new AvalonEngine();
        public static MoonSharpScriptLoader _luaLoader { private set; get; }
        static MenuSystem menuSystem = new MenuSystem();

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
                InitializeScripts();
                FindGameProcess();
                MainLoop();

            }
            catch (Exception ex)
            {
                WinDialog.ShowErrorDialog("Error crítico", ex.ToString());
            }
        }

        private static void InitializeScripts()
        {
            _luaLoader = new MoonSharpScriptLoader(Engine);

            // Carga y inicializa todos los scripts Lua del directorio "Scripts"
            _luaLoader.LoadScripts("Scripts");
        }


        private static void InitializeConsole()
        {
            // Establecer la codificación de salida a UTF-8 (65001)
            LibManager.SetConsoleOutputCP(65001);

            //if (LibManager.AttachConsole(-1))
            LibManager.AllocConsole();

            Console.Title = "AvalonInjectLib";
            Console.WriteLine("=== Universal Memory Cheat - V1.0.0.0 ===");
        }

        private static void FindGameProcess()
        {
            var info = InjectionInfo.GetCurrentProcessInfo();
            Logger.Info($"ProcessName: {info.ProcessName}");

            var process = ProcessManager.Create(info.ProcessName);

            Engine.SetProcess(process);

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
            menuSystem.Initialize();
            Renderer.SetRenderCallback(menuSystem.Render);
        }

        private static void MainLoop()
        {
            while (true)
            {
                LibManager.Sleep(100);
            }
        }

    }
}
