namespace AvalonInjectLib.Interfaces
{
    public interface IAvalonEngine
    {
        ProcessEntry Process { get; }

        void Log(string msg, string module);
    }
}
