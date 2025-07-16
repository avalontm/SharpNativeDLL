using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public class Font : IDisposable
    {
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
        public float LineHeight => FontRenderer.GetLineHeight(FontId, FontRenderer.GetScaleForDesiredSize(Size));

        public static void Initialize()
        {
            if (_isInitialized) return;

            // Cargar fuente por defecto embebida
            byte[] defaultFontData = EmbeddedResourceLoader.LoadResource("AvalonInjectLib.ARIAL.TTF");
            _defaultFont = LoadFromMemory(defaultFontData, (int)FontRenderer.DefaultFontSize, "Default");

            _isInitialized = true;
        }

        // Patrón de carga similar a Texture2D
        public static Font LoadFromFile(string path, int size, string name = null, bool bold = false, bool italic = false)
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
            if (_isDisposed)
                throw new ObjectDisposedException("Font");

            return FontRenderer.MeasureText(FontId, text, FontRenderer.GetScaleForDesiredSize(Size));
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