using System.Collections.Concurrent;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public class Font : IDisposable
    {
        /// <summary>
        /// Estructura que contiene información detallada sobre las métricas de texto
        /// </summary>
        public struct TextMetrics
        {
            public float Width;
            public float Height;
            public int LineCount;
            public float LineHeight;
            public float MaxLineWidth;
        }

        // Sistema de gestión de fuentes similar a Texture2D
        private static readonly ConcurrentDictionary<string, Font> _loadedFonts = new();
        private static Font? _defaultFont;
        private static bool _isInitialized;
        private byte[]? _fontData;

        // Datos de la fuente
        internal uint FontId;
        private string? _fontPath;
        private bool _isDisposed;
        private bool _loadRequested;

        public string Name { get; private set; }
        public int Size { get; private set; }
        public bool IsBold { get; private set; }
        public bool IsItalic { get; private set; }

        // Propiedades ajustadas para usar FontRenderer
        public bool IsReady => FontRenderer.IsFontReady(FontId);
        public bool IsLoading => FontRenderer.IsFontLoading(FontId);
        public float LineHeight => FontRenderer.GetLineHeight(FontId, 1.0f);
        public float ScaledLineHeight => FontRenderer.GetLineHeight(FontId);

        public static void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                // Cargar fuente por defecto embebida
                byte[] defaultFontData = EmbeddedResourceLoader.LoadResource("AvalonInjectLib.InputMono-Medium.ttf");
                _defaultFont = LoadFromMemory(defaultFontData, 14, "Default");

                Logger.Debug("Font system initialized with default font", "Font");
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to initialize font system: {ex.Message}", "Font");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Carga una fuente desde archivo (asíncrono por defecto)
        /// </summary>
        public static Font LoadFromFile(string path, int size, string? name = null, bool bold = false, bool italic = false)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Font file not found: {path}");

            string key = $"{path}|{size}|{bold}|{italic}";

            return _loadedFonts.GetOrAdd(key, _ =>
            {
                var font = new Font(path, size, name ?? Path.GetFileNameWithoutExtension(path), bold, italic);
                font.RequestLoadAsync();
                return font;
            });
        }

        /// <summary>
        /// Carga una fuente desde archivo de forma síncrona (inmediata)
        /// </summary>
        public static Font LoadFromFileSync(string path, int size, string? name = null, bool bold = false, bool italic = false)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Font file not found: {path}");

            string key = $"SYNC:{path}|{size}|{bold}|{italic}";

            return _loadedFonts.GetOrAdd(key, _ =>
            {
                var font = new Font(path, size, name ?? Path.GetFileNameWithoutExtension(path), bold, italic);
                font.RequestLoadSync();
                return font;
            });
        }

        /// <summary>
        /// Carga una fuente desde memoria (asíncrono por defecto)
        /// </summary>
        public static Font LoadFromMemory(byte[] fontData, int size, string name, bool bold = false, bool italic = false)
        {
            if (fontData == null || fontData.Length == 0)
                throw new ArgumentException("Font data cannot be null or empty");

            string key = $"MEM:{name}|{size}|{bold}|{italic}";

            return _loadedFonts.GetOrAdd(key, _ =>
            {
                var font = new Font(fontData, size, name, bold, italic);
                font.RequestLoadAsync();
                return font;
            });
        }

        /// <summary>
        /// Carga una fuente desde memoria de forma síncrona (inmediata)
        /// </summary>
        public static Font LoadFromMemorySync(byte[] fontData, int size, string name, bool bold = false, bool italic = false)
        {
            if (fontData == null || fontData.Length == 0)
                throw new ArgumentException("Font data cannot be null or empty");

            string key = $"MEMSYNC:{name}|{size}|{bold}|{italic}";

            return _loadedFonts.GetOrAdd(key, _ =>
            {
                var font = new Font(fontData, size, name, bold, italic);
                font.RequestLoadSync();
                return font;
            });
        }

        // Constructor privado para fuentes desde archivo
        private Font(string path, int size, string name, bool bold, bool italic)
        {
            _fontPath = path;
            Size = size;
            Name = name;
            IsBold = bold;
            IsItalic = italic;
            _loadRequested = false;
        }

        // Constructor privado para fuentes desde memoria
        private Font(byte[] fontData, int size, string name, bool bold, bool italic)
        {
            _fontData = fontData;
            Size = size;
            Name = name;
            IsBold = bold;
            IsItalic = italic;
            _loadRequested = false;
        }

        /// <summary>
        /// Solicita la carga asíncrona de la fuente
        /// </summary>
        private void RequestLoadAsync()
        {
            if (_loadRequested) return;

            if (!string.IsNullOrEmpty(_fontPath))
            {
                FontId = FontRenderer.RequestFontAsync(_fontPath, Size);
            }
            else if (_fontData != null)
            {
                FontId = FontRenderer.RequestFontAsync(_fontData, Size);
            }

            _loadRequested = true;

            if (FontId == 0)
                Logger.Warning($"Failed to request font loading: {Name}", "Font");
        }

        /// <summary>
        /// Solicita la carga síncrona de la fuente
        /// </summary>
        private void RequestLoadSync()
        {
            if (_loadRequested) return;

            if (!string.IsNullOrEmpty(_fontPath))
            {
                FontId = FontRenderer.LoadFontSync(_fontPath, Size);
            }
            else if (_fontData != null)
            {
                FontId = FontRenderer.LoadFontSyncFromData(_fontData, Size);
            }

            _loadRequested = true;

            if (FontId == 0)
                Logger.Error($"Failed to load font synchronously: {Name}", "Font");
        }

        /// <summary>
        /// Fuerza la carga inmediata si la fuente estaba pendiente
        /// </summary>
        public bool ForceLoad()
        {
            if (IsReady) return true;
            if (FontId == 0) return false;

            return FontRenderer.ForceLoadFont(FontId);
        }

        public static Font? GetDefaultFont()
        {
            if (!_isInitialized)
                Initialize();

            if (_defaultFont == null)
            {
                Logger.Error("No default font available. Font system may not be initialized.", "Font");
                return null;
            }
            return _defaultFont;
        }

        public uint GetFontId()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Font");

            return FontId;
        }

        /// <summary>
        /// Mide el texto con escala por defecto
        /// </summary>
        public Vector2 MeasureText(string text)
        {
            if (string.IsNullOrEmpty(text) || !IsReady)
                return Vector2.Zero;

            return FontRenderer.MeasureText(FontId, text, 1.0f);
        }

        /// <summary>
        /// Mide el texto con un escalado personalizado
        /// </summary>
        public Vector2 MeasureText(string text, float scale)
        {
            if (string.IsNullOrEmpty(text) || !IsReady)
                return Vector2.Zero;

            return FontRenderer.MeasureText(FontId, text, scale);
        }

        /// <summary>
        /// Obtiene el ancho específico de una línea de texto
        /// </summary>
        public float GetLineWidth(string line)
        {
            if (string.IsNullOrEmpty(line) || !IsReady)
                return 0f;

            return FontRenderer.MeasureText(FontId, line, 1.0f).X;
        }

        /// <summary>
        /// Obtiene el ancho máximo de múltiples líneas de texto
        /// </summary>
        public float GetMaxLineWidth(string text)
        {
            if (string.IsNullOrEmpty(text) || !IsReady)
                return 0f;

            string[] lines = text.Split('\n');
            float maxWidth = 0f;

            foreach (string line in lines)
            {
                float lineWidth = GetLineWidth(line);
                if (lineWidth > maxWidth)
                    maxWidth = lineWidth;
            }

            return maxWidth;
        }

        /// <summary>
        /// Calcula la altura total del texto considerando múltiples líneas
        /// </summary>
        public float GetTextHeight(string text)
        {
            if (string.IsNullOrEmpty(text) || !IsReady)
                return 0f;

            int lineCount = text.Split('\n').Length;
            return lineCount * LineHeight;
        }

        /// <summary>
        /// Obtiene información detallada sobre las dimensiones del texto
        /// </summary>
        public TextMetrics GetTextMetrics(string text)
        {
            if (string.IsNullOrEmpty(text) || !IsReady)
                return new TextMetrics();

            string[] lines = text.Split('\n');
            float maxWidth = 0f;
            float totalHeight = lines.Length * LineHeight;

            foreach (string line in lines)
            {
                float lineWidth = GetLineWidth(line);
                if (lineWidth > maxWidth)
                    maxWidth = lineWidth;
            }

            return new TextMetrics
            {
                Width = maxWidth,
                Height = totalHeight,
                LineCount = lines.Length,
                LineHeight = LineHeight,
                MaxLineWidth = maxWidth
            };
        }

        /// <summary>
        /// Crea una nueva instancia con diferente tamaño
        /// </summary>
        public Font? WithSize(int newSize)
        {
            if (newSize == Size)
                return this;

            if (!string.IsNullOrEmpty(_fontPath))
            {
                return LoadFromFile(_fontPath, newSize, Name, IsBold, IsItalic);
            }
            else if (_fontData != null)
            {
                return LoadFromMemory(_fontData, newSize, Name, IsBold, IsItalic);
            }

            Logger.Error("Cannot create font with new size: no source data available", "Font");
            return null;
        }

        /// <summary>
        /// Crea una nueva instancia con diferente tamaño (carga síncrona)
        /// </summary>
        public Font? WithSizeSync(int newSize)
        {
            if (newSize == Size)
                return this;

            if (!string.IsNullOrEmpty(_fontPath))
            {
                return LoadFromFileSync(_fontPath, newSize, Name, IsBold, IsItalic);
            }
            else if (_fontData != null)
            {
                return LoadFromMemorySync(_fontData, newSize, Name, IsBold, IsItalic);
            }

            Logger.Error("Cannot create font with new size: no source data available", "Font");
            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _fontData = null;
                }

                if (FontId != 0)
                {
                    FontRenderer.DeleteFont(FontId);
                    FontId = 0;
                }

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Limpia la caché de fuentes cargadas
        /// </summary>
        public static void ClearCache()
        {
            foreach (var font in _loadedFonts.Values)
                font.Dispose();

            _loadedFonts.Clear();
            _defaultFont?.Dispose();
            _defaultFont = null;
            _isInitialized = false;
        }

        /// <summary>
        /// Obtiene estadísticas del sistema de fuentes
        /// </summary>
        public static (int loaded, int loading, int pending, long memoryMB) GetStats()
        {
            return FontRenderer.GetFontStats();
        }

        /// <summary>
        /// Procesa fuentes pendientes (debe llamarse en el loop principal)
        /// </summary>
        public static void ProcessPendingFonts()
        {
            FontRenderer.ProcessPendingFonts();
        }

        ~Font()
        {
            Dispose(false);
        }
    }
}