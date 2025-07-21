using AvalonInjectLib;
using AvalonInjectLib.Scripting;
using AvalonInjectLib.UIFramework;
using System.Diagnostics;

namespace TargetGame
{
    public class MenuSystem : UIFrameworkRenderSystem
    {
        private bool _isInitialized = false;
        private MenuList? _mainMenu;
        private volatile bool _isReloading = false;
        private string? originalTitle;

        // Configuración de diseño
        private const float MaxMenuHeight = 500f;
        private const float MenuWidth = 250f;
        private const float MenuPadding = 5f;

        public void Initialize()
        {
            if (_isInitialized) return;

            // Inicializar dependencias
            Font.Initialize();

            // Crear menú principal
            CreateMainMenu();
            LoadScripts();

            _isInitialized = true;
        }

        private void CreateMainMenu()
        {
            _mainMenu = new MenuList
            {
                X = MenuPadding,
                Y = MenuPadding,
                Width = MenuWidth,
                Height = 200f,
                HeaderText = "Avalon[HUB]",
                Visible = true
            };

            originalTitle = _mainMenu.HeaderText;
        }

        private void LoadScripts()
        {
            try
            {
                MoonSharpHelper.ClearMenu(_mainMenu);
                MoonSharpHelper.InitializeMenuStructure(_mainMenu, MoonSharpScriptLoader.Instance.Scripts);
                AdjustMenuSize();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading scripts: {ex.Message}", "MenuSystem");
            }
        }

        private void AdjustMenuSize()
        {
            float totalHeight = _mainMenu.ShowHeader ? _mainMenu.HeaderHeight + _mainMenu.SeparatorHeight : 0;
            totalHeight += _mainMenu.BorderWidth * 2;

            foreach (var item in _mainMenu.GetAllItems())
            {
                totalHeight += item.CalculateTotalHeight();
            }

            _mainMenu.Height = Math.Min(totalHeight, MaxMenuHeight);
        }

        #region RELOAD SYSTEM
        public void ReloadScripts()
        {
            if (_isReloading) return;
            _isReloading = true;

            Thread.Sleep(100);
            //MoonSharpScriptLoader.Instance?.ReloadScripts();
            LoadScripts();

            _isReloading = false;
        }
        #endregion

        #region RENDER
        public void Render()
        {
            if (!_isInitialized) return;

            // Variables para el control de FPS
            double targetFrameTime = 1000.0 / 60.0; // 60 FPS en milisegundos
            DateTime now = DateTime.Now;
            TimeSpan elapsed;

            if (!_isReloading)
            {
                // Ejecutar el código específico a ~60 FPS
                elapsed = now - _lastFrameTime;
                if (elapsed.TotalMilliseconds >= targetFrameTime)
                {
                    _mainMenu.Update();
                    
                    if (MoonSharpScriptLoader.Instance != null)
                    {
                        MoonSharpScriptLoader.Instance?.UpdateAll();
                    }
                    _lastFrameTime = now; // Actualizar el tiempo del último frame
                }

                // Procesar fuentes pendientes en cada frame de renderizado (cuando hay contexto OpenGL)
                Font.ProcessPendingFonts();
                
                if (MoonSharpScriptLoader.Instance != null)
                {
                    MoonSharpScriptLoader.Instance.DrawAll();
                }
                _mainMenu.Draw();
            }
            else
            {
                DrawReloadIndicator();
            }

        }

        // Añade este campo a tu clase
        private DateTime _lastFrameTime = DateTime.Now;

        private void DrawReloadIndicator()
        {
            if (_mainMenu.Visible)
            {
                _mainMenu.HeaderText = "Reloading...";
                _mainMenu.Update();
                _mainMenu.Draw();
                _mainMenu.HeaderText = originalTitle;
            }
        }

        public void Shutdown()
        {
            MoonSharpHelper.ClearMenu(_mainMenu);
            _isInitialized = false;
        }

        public void Toggle()
        {
            _mainMenu.Visible = !_mainMenu.Visible;
        }

        // Propiedades públicas
        public bool IsMenuVisible => _mainMenu?.Visible ?? false;
        public int ScriptCount => MoonSharpScriptLoader.Instance?.Scripts.Count ?? 0;
        public int CategoryCount => MoonSharpHelper.GetCategoryCount();
        public bool IsReloading => _isReloading;
        public bool IsInitialized => _isInitialized;
        public MenuList MainMenu => _mainMenu;
        #endregion
    }
}