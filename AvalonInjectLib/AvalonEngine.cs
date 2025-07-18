using AvalonInjectLib.Interfaces;

namespace AvalonInjectLib
{
    public class AvalonEngine : IAvalonEngine
    {
        public static AvalonEngine Instance { get; private set; }
        ProcessEntry _process;
        public ProcessEntry Process { get => _process; }


        public AvalonEngine()
        {
            if (Instance == null)
                Instance = this;
        }

        public void SetProcess(ProcessEntry process)
        {
            _process = process;
        }

        public void Log(string message, string module)
        {
            try
            {
                Logger.Info(message, module);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message, module);
            }
        }
    }
}
