using AvalonInjectLib;
using AvalonInjectLib.Scripting;
using AvalonInjectLib.UIFramework;

namespace TargetGame
{
    public class MenuSystem : UIFrameworkRenderSystem
    {
        private bool _isInitialized = false;
        private MenuList _mainMenu;
        private volatile bool _isReloading = false;
        private string originalTitle;

        // Configuración de diseño
        private const float MaxMenuHeight = 500f;
        private const float MenuWidth = 250f;
        private const float MenuPadding = 5f;

        public void Initialize()
        {
            if (_isInitialized) return;

            // Inicializar dependencias
            Font.Initialize();
            InputSystem.Initialize(AvalonEngine.Instance.Process.ProcessId);

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
        public void OnReloadScripts()
        {
            if (_isReloading) return;
            _isReloading = true;
            new Thread(() =>
            {
                Thread.Sleep(100);
                MoonSharpScriptLoader.Instance?.ReloadScripts();
                LoadScripts();

                _isReloading = false;
            }).Start();
        }
        #endregion

        #region RENDER
        public void Render()
        {
            if (!_isInitialized) return;

            try
            {
                if (!_isReloading)
                {
                    if (InputSystem.GetKeyDown(Keys.F1))
                    {
                        Toggle();
                    }
                    else if (InputSystem.GetKeyDown(Keys.F5))
                    {
                        OnReloadScripts();
                    }
                    _mainMenu.Update();
                    _mainMenu.Draw();

                    MoonSharpScriptLoader.Instance?.UpdateAll();
                    MoonSharpScriptLoader.Instance?.DrawAll();
                }
                else
                {
                    DrawReloadIndicator();
                }

                InputSystem.Update();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in Render: {ex.Message}", "MenuSystem");
            }
        }

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
            try
            {
                MoonSharpHelper.ClearMenu(_mainMenu);
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during shutdown: {ex.Message}", "MenuSystem");
            }
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
        #endregion
    }
}