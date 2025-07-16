namespace AvalonInjectLib.UIFramework
{
    public interface UIFrameworkRenderSystem
    {
        void Initialize(uint processId);
        void Render();
        void Shutdown();
    }
}