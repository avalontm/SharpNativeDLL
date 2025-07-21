namespace AvalonInjectLib
{
    public static class Logger
    {
        public enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error,
            Critical
        }

        // Configuración
        public static bool EnableDebug { get; set; } = true;
        public static bool EnableTimestamp { get; set; } = true;
        public static LogLevel MinimumLevel { get; set; } = LogLevel.Debug;

        private static readonly Dictionary<LogLevel, ConsoleColor> _levelColors = new()
        {
            { LogLevel.Debug, ConsoleColor.Gray },
            { LogLevel.Info, ConsoleColor.White },
            { LogLevel.Warning, ConsoleColor.Yellow },
            { LogLevel.Error, ConsoleColor.Red },
            { LogLevel.Critical, ConsoleColor.DarkRed }
        };

        private static readonly object _lock = new();
        private static bool _consoleAvailable = true;

        public static void Log(LogLevel level, string message, string module = null)
        {
            if ((int)level < (int)MinimumLevel)
                return;

            if (level == LogLevel.Debug && !EnableDebug)
                return;

            lock (_lock)
            {
                try
                {
                    if (_consoleAvailable)
                    {
                        var originalColor = Console.ForegroundColor;

                        // Timestamp
                        if (EnableTimestamp)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.Write($"[{DateTime.Now:HH:mm:ss.fff}] ");
                        }

                        // Nivel
                        Console.ForegroundColor = _levelColors[level];
                        Console.Write($"[{level.ToString().ToUpper()}] ");

                        // Módulo
                        if (!string.IsNullOrEmpty(module))
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.Write($"[{module}] ");
                        }

                        // Mensaje
                        Console.ForegroundColor = originalColor;
                        Console.WriteLine(message);

                        Console.ResetColor();
                    }
                }
                catch
                {
                    _consoleAvailable = false; // Desactiva futuros logs si no hay consola disponible
                }
            }
        }

        // Métodos rápidos
        public static void Debug(string message, string module = null) => Log(LogLevel.Debug, message, module);
        public static void Info(string message, string module = null) => Log(LogLevel.Info, message, module);
        public static void Warning(string message, string module = null) => Log(LogLevel.Warning, message, module);
        public static void Error(string message, string module = null) => Log(LogLevel.Error, message, module);
        public static void Critical(string message, string module = null) => Log(LogLevel.Critical, message, module);

        public static void Exception(Exception ex, string message = null, string module = null)
        {
            var msg = $"{message} - {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            Log(LogLevel.Error, msg, module);

            if (ex.InnerException != null)
            {
                Exception(ex.InnerException, "Inner Exception", module);
            }
        }
    }
}
