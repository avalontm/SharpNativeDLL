using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using static AvalonInjectLib.Structs;
using static System.Net.Mime.MediaTypeNames;

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
        private bool _isRequested;

        public string Name { get; private set; }
      
        public int Size { get; private set; }
        public bool IsBold { get; private set; }
        public bool IsItalic { get; private set; }


        public bool IsReady => FontRenderer.IsFontLoaded(FontId);
        public float LineHeight => FontRenderer.GetLineHeight(FontId, 1.0f);
        public float ScaledLineHeight => FontRenderer.GetLineHeight(FontId);

        public static void Initialize()
        {
            if (_isInitialized) return;

            // Cargar fuente por defecto embebida
            byte[] defaultFontData = EmbeddedResourceLoader.LoadResource("AvalonInjectLib.InputMono-Medium.ttf");
            _defaultFont = LoadFromMemory(defaultFontData, 14, "Default");

            _isInitialized = true;
        }

        // Patrón de carga similar a Texture2D
        public static Font LoadFromFile(string path, int size, string? name = null, bool bold = false, bool italic = false)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Font file not found: {path}");

            string key = $"{path}|{size}|{bold}|{italic}";

            return _loadedFonts.GetOrAdd(key, _ =>
            {
                var font = new Font(path, size, name ?? Path.GetFileNameWithoutExtension(path), bold, italic);
                int actualSize;
                FontRenderer.RequestFont(path, size, out actualSize);
                return font;
            });
        }

        public static Font LoadFromMemory(byte[] fontData, int size, string name, bool bold = false, bool italic = false)
        {
            string key = $"MEM:{name}|{size}|{bold}|{italic}";

            return _loadedFonts.GetOrAdd(key, _ =>
            {
                var font = new Font(fontData, size, name, bold, italic);
                int actualSize;

                font.FontId = FontRenderer.RequestFont(fontData, size, out actualSize);
                return font;
            });
        }

        public Font(string path, int size, string name, bool bold, bool italic)
        {
            _fontPath = path;
            Size = size;
            Name = name;
            IsBold = bold;
            IsItalic = italic;
            _isRequested = true;
        }

        public Font(byte[] fontData, int size, string name, bool bold, bool italic)
        {
            _fontData = fontData;
            Size = size;
            Name = name;
            IsBold = bold;
            IsItalic = italic;
            _isRequested = true;
        }

        public static Font? GetDefaultFont()
        {
            if (_defaultFont == null)
            {
                Logger.Error("No se han cargado fuentes. Debes cargar al menos una fuente primero.", "Font");
                return null;
            }
            return _defaultFont;
        }

        public uint GetFontId()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("Font");
            if (FontId == 0)
            {
                FontId = FontRenderer.GetFontTextureId(FontId);
            }
            return FontId;
        }

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

        public void Dispose()
        {
            _fontData = null;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (FontId != 0)
                {
                    FontRenderer.DeleteFont(FontId);
                    FontId = 0;
                }

                ClearCache();
                _isDisposed = true;
            }
        }

        public static void ClearCache()
        {
            foreach (var font in _loadedFonts.Values)
                font.Dispose();

            _loadedFonts.Clear();
        }

        public Font? WithSize(int newSize)
        {
            if (FontId == 0) 
                return this;
            
            if (newSize == Size)
                return this;

            if(string.IsNullOrEmpty(_fontPath))
            {
                return LoadFromMemory(_fontData, newSize, Name, IsBold, IsItalic);
            }

            return LoadFromFile(_fontPath, newSize, Name, IsBold, IsItalic);
        }

    }
}