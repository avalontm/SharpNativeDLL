namespace AvalonInjectLib.UIFramework
{
    public interface UIFrameworkRenderSystem
    {
        Window MainWindow { set; get; }
        bool IsInitialized { set; get; }
        void Initialize(uint processId);
        void Render();
        void Shutdown();
    }
}