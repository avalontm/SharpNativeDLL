using StbTrueTypeSharp;
using System.Collections.Concurrent;
using static AvalonInjectLib.OpenGLInterop;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    internal static unsafe class FontRenderer
    {
        public static bool IsContexted { get; internal set; }
        private const int DefaultAtlasSize = 512;

        // Estructuras para manejo asíncrono
        private static readonly ConcurrentQueue<PendingFont> _pendingFonts = new();
        private static readonly ConcurrentDictionary<uint, FontData> _loadedFonts = new();
        private static uint _nextPendingId = 1;

        // Estructuras adicionales para métricas detalladas
        public struct GlyphMetrics
        {
            public float Advance;
            public float BearingX;
            public float BearingY;
            public float Width;
            public float Height;
        }


        // Estructura extendida para las métricas
        public struct FontVerticalMetrics
        {
            // Métricas básicas
            public float Ascent { get; set; }
            public float Descent { get; set; }
            public float LineGap { get; set; }
            public float LineHeight { get; set; }

            // Métricas visuales
            public float VisualHeight { get; set; }
            public float VisualAscent { get; set; }
            public float VisualDescent { get; set; }
            public float CapHeight { get; set; }
            public float XHeight { get; set; }

            // Centros para diferentes tipos de centrado
            public float VisualCenter { get; set; }
            public float GeometricCenter { get; set; }
            public float CapCenter { get; set; }

            // Offsets precalculados para baseline
            public float BaselineOffsetForVisualCenter { get; set; }
            public float BaselineOffsetForCapCenter { get; set; }

            // Método helper para obtener baseline centrado
            public float GetCenteredBaseline(float elementY, float elementHeight, CenteringMode mode = CenteringMode.Visual)
            {
                float elementCenter = elementY + (elementHeight / 2);

                return mode switch
                {
                    CenteringMode.Visual => elementCenter - (VisualAscent - VisualDescent) / 2,
                    CenteringMode.Geometric => elementCenter - (Ascent - Descent) / 2,
                    CenteringMode.CapHeight => elementCenter + CapHeight / 2,
                    _ => elementCenter - (VisualAscent - VisualDescent) / 2
                };
            }
        }

        private struct PendingFont
        {
            public uint PendingId;
            public int Size;
            public byte[] FontData;
        }

        public struct FontData
        {
            public Dictionary<char, GlyphData> Glyphs;
            public uint TextureId;
            public float FontScale;
            public int Ascent;
            public int Descent;
            public int LineGap;
            public int AtlasSize;
            public bool IsLoaded;
        }

        public struct GlyphData
        {
            public float Advance;
            public float BearingX;
            public float BearingY;
            public int Width;
            public int Height;
            public Rect TexCoords;
        }

        // Método para solicitar carga de fuente (sin contexto OpenGL)
        public static uint RequestFont(string fontPath, int size, out int actualSize)
        {
            actualSize = size;

            if (!File.Exists(fontPath))
            {
                Logger.Error($"Font file not found: {fontPath}", "FontRenderer");
                return 0;
            }

            try
            {
                byte[] fontData = File.ReadAllBytes(fontPath);
                uint pendingId = _nextPendingId++;

                _pendingFonts.Enqueue(new PendingFont
                {
                    PendingId = pendingId,
                    Size = size,
                    FontData = fontData
                });

                Logger.Debug($"Requested font {pendingId} from {fontPath} (Size: {size})", "FontRenderer");
                return pendingId;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to request font from {fontPath}: {ex.Message}", "FontRenderer");
                return 0;
            }
        }

        public static uint RequestFont(byte[] fontData, int size, out int actualSize)
        {
            actualSize = size;

            try
            {
                if (fontData == null || fontData.Length == 0)
                {
                    Logger.Error("Empty font data provided", "FontRenderer");
                    return 0;
                }

                uint pendingId = _nextPendingId++;

                _pendingFonts.Enqueue(new PendingFont
                {
                    PendingId = pendingId,
                    Size = size,
                    FontData = fontData
                });

                Logger.Debug($"Requested font {pendingId} from embedded data (Size: {size})", "FontRenderer");
                return pendingId;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to request font from embedded data: {ex.Message}", "FontRenderer");
                return 0;
            }
        }

        public static void ProcessPendingFonts()
        {
            if (!IsContexted)
            {
                Logger.Warning("Cannot process fonts - no OpenGL context", "FontRenderer");
                return;
            }

            int processedCount = 0;

            while (_pendingFonts.TryDequeue(out var pending))
            {
                try
                {
                    var fontData = LoadFontData(pending.FontData, pending.Size);
                    _loadedFonts[pending.PendingId] = fontData;
                    processedCount++;

                    Logger.Debug($"Successfully loaded font {pending.PendingId} (Size: {pending.Size}, TextureID: {fontData.TextureId})", "FontRenderer");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to process font {pending.PendingId}: {ex.Message}", "FontRenderer");
                }
            }

            if (processedCount > 0)
            {
                Logger.Info($"Processed {processedCount} pending fonts", "FontRenderer");
            }
        }

        internal static FontData LoadFontData(byte[] fontData, int size)
        {
            var result = new FontData
            {
                Glyphs = new Dictionary<char, GlyphData>(),
                AtlasSize = DefaultAtlasSize,
                IsLoaded = false
            };

            // 1. Inicializar fuente STB
            var stbFont = new StbTrueType.stbtt_fontinfo();
            fixed (byte* ptr = fontData)
            {
                if (StbTrueType.stbtt_InitFont(stbFont, ptr, 0) == 0)
                    throw new Exception("Failed to initialize font");
            }

            // 2. Obtener métricas
            int ascent, descent, lineGap;
            StbTrueType.stbtt_GetFontVMetrics(stbFont, &ascent, &descent, &lineGap);

            result.FontScale = StbTrueType.stbtt_ScaleForPixelHeight(stbFont, size*4);
            result.Ascent = ascent;
            result.Descent = descent;
            result.LineGap = lineGap;

            // 3. Crear atlas
            byte[] atlasData = new byte[DefaultAtlasSize * DefaultAtlasSize * 4];
            int currentX = 0, currentY = 0, currentLineHeight = 0;

            // 4. Rasterizar glifos ASCII
            for (int i = 32; i < 128; i++)
            {
                char c = (char)i;
                AddGlyphToAtlas(stbFont, c, ref result, atlasData, ref currentX, ref currentY, ref currentLineHeight);
            }

            // 5. Crear textura OpenGL
            result.TextureId = CreateFontTexture(atlasData);
            result.IsLoaded = true;

            return result;
        }

        private static void AddGlyphToAtlas(StbTrueType.stbtt_fontinfo font, char c, ref FontData fontData,  byte[] atlasData, ref int currentX, ref int currentY, ref int currentLineHeight)
        {
            // Obtener métricas del glifo
            int advance, bearing;
            StbTrueType.stbtt_GetCodepointHMetrics(font, c, &advance, &bearing);

            // Obtener bounding box
            int x0, y0, x1, y1;
            StbTrueType.stbtt_GetCodepointBitmapBox(font, c, fontData.FontScale, fontData.FontScale, &x0, &y0, &x1, &y1);

            int w = x1 - x0;
            int h = y1 - y0;

            // Manejar espacios y caracteres especiales
            if (w <= 0 || h <= 0)
            {
                fontData.Glyphs[c] = new GlyphData
                {
                    Advance = advance * fontData.FontScale,
                    BearingX = bearing * fontData.FontScale,
                    BearingY = y0,
                    Width = 0,
                    Height = 0,
                    TexCoords = new Rect(0, 0, 0, 0)
                };
                return;
            }

            // Posicionamiento en el atlas
            if (currentX + w + 2 >= DefaultAtlasSize)
            {
                currentX = 0;
                currentY += currentLineHeight + 2;
                currentLineHeight = 0;

                if (currentY + h + 2 >= DefaultAtlasSize)
                {
                    Logger.Error($"Atlas full, skipping character: '{c}'", "FontRenderer");
                    return;
                }
            }

            if (h > currentLineHeight) currentLineHeight = h;

            int xPos = currentX + 1;
            int yPos = currentY + 1;

            // Rasterizar glifo
            byte[] bitmap = new byte[w * h];
            fixed (byte* ptr = bitmap)
            {
                StbTrueType.stbtt_MakeCodepointBitmap(font, ptr, w, h, w, fontData.FontScale, fontData.FontScale, c);
            }

            // Copiar al atlas
            for (int row = 0; row < h; row++)
            {
                for (int col = 0; col < w; col++)
                {
                    int dstPos = ((yPos + row) * DefaultAtlasSize + (xPos + col)) * 4;
                    byte alpha = bitmap[row * w + col];

                    atlasData[dstPos] = 255;     // R
                    atlasData[dstPos + 1] = 255; // G
                    atlasData[dstPos + 2] = 255; // B
                    atlasData[dstPos + 3] = alpha; // A
                }
            }

            // Guardar datos del glifo
            fontData.Glyphs[c] = new GlyphData
            {
                Advance = advance * fontData.FontScale,
                BearingX = bearing * fontData.FontScale,
                BearingY = y0,
                Width = w,
                Height = h,
                TexCoords = new Rect(
                    (float)xPos / DefaultAtlasSize,
                    (float)yPos / DefaultAtlasSize,
                    (float)w / DefaultAtlasSize,
                    (float)h / DefaultAtlasSize
                )
            };

            currentX += w + 2;
        }

        private static uint CreateFontTexture(byte[] atlasData)
        {
            uint textureId;
            glGenTextures(1, &textureId);
            glBindTexture(GL_TEXTURE_2D, textureId);

            fixed (byte* ptr = atlasData)
            {
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, DefaultAtlasSize, DefaultAtlasSize,
                            0, GL_RGBA, GL_UNSIGNED_BYTE, (IntPtr)ptr);
            }

            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

            return textureId;
        }

        // Métodos para acceder a las fuentes cargadas
        public static bool IsFontReady(uint pendingId) => _loadedFonts.ContainsKey(pendingId);
        public static FontData GetFontData(uint pendingId) => _loadedFonts.TryGetValue(pendingId, out var data) ? data : default;

        public static void DrawText(uint fontId, string text, float x, float y, float size, Color color)
        {
            if (!_loadedFonts.TryGetValue(fontId, out var font) || !font.IsLoaded) return;

            glEnable(GL_TEXTURE_2D);
            glBindTexture(GL_TEXTURE_2D, font.TextureId);
            glTexEnvi(GL_TEXTURE_ENV, GL_TEXTURE_ENV_MODE, GL_MODULATE);

            // Calcular escala basada en el tamaño solicitado vs el tamaño base de la fuente
            float scale = size / (font.Ascent * font.FontScale);
            float startX = x;
            float baseline = y + (font.Ascent * font.FontScale * scale);

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    startX = x;
                    baseline += GetLineHeight(fontId, scale);
                    continue;
                }

                if (!font.Glyphs.TryGetValue(c, out var glyph)) continue;

                if (glyph.Width == 0 || glyph.Height == 0)
                {
                    startX += glyph.Advance * scale;
                    continue;
                }

                float xPos = startX + glyph.BearingX * scale;
                float yPos = baseline + glyph.BearingY * scale;

                glBegin(GL_QUADS);
                glColor4f(color.R, color.G, color.B, color.A);

                glTexCoord2f(glyph.TexCoords.X, glyph.TexCoords.Y);
                glVertex2f(xPos, yPos);

                glTexCoord2f(glyph.TexCoords.X + glyph.TexCoords.Width, glyph.TexCoords.Y);
                glVertex2f(xPos + glyph.Width * scale, yPos);

                glTexCoord2f(glyph.TexCoords.X + glyph.TexCoords.Width, glyph.TexCoords.Y + glyph.TexCoords.Height);
                glVertex2f(xPos + glyph.Width * scale, yPos + glyph.Height * scale);

                glTexCoord2f(glyph.TexCoords.X, glyph.TexCoords.Y + glyph.TexCoords.Height);
                glVertex2f(xPos, yPos + glyph.Height * scale);

                glEnd();

                startX += glyph.Advance * scale;
            }

            glDisable(GL_TEXTURE_2D);
        }

        internal static Vector2 MeasureText(uint fontId, string text, float scale)
        {
            if (!_loadedFonts.TryGetValue(fontId, out var font) || !font.IsLoaded)
                return Vector2.Zero;

            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;

            float _scale = (scale / 4);
            float maxWidth = 0f;
            float currentWidth = 0f;
            int lineCount = 1;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    if (currentWidth > maxWidth)
                        maxWidth = currentWidth;
                    currentWidth = 0f;
                    lineCount++;
                    continue;
                }

                if (font.Glyphs.TryGetValue(c, out var glyph))
                {
                    currentWidth += glyph.Advance * _scale;
                }
            }

            // Verificar la última línea
            if (currentWidth > maxWidth)
                maxWidth = currentWidth;

            float height = lineCount * GetLineHeight(fontId, scale);
            return new Vector2(maxWidth, height);
        }

        internal static float GetLineHeight(uint fontId, float scale = 1.0f)
        {
            if (!_loadedFonts.TryGetValue(fontId, out var font) || !font.IsLoaded)
                return 0f;

            scale = (scale / 4);
            // Altura de línea = (ascent - descent + lineGap) * escala de fuente * escala aplicada
            return (font.Ascent - font.Descent + font.LineGap) * font.FontScale * scale;
        }

        public static void DeleteFont(uint pendingId)
        {
            if (_loadedFonts.TryRemove(pendingId, out var fontData))
            {
                uint tex = fontData.TextureId;
                glDeleteTextures(1, &tex);
            }
        }

        public static void CleanupFonts()
        {
            foreach (var font in _loadedFonts.Values)
            {
                uint tex = font.TextureId;
                glDeleteTextures(1, &tex);
            }
            _loadedFonts.Clear();
            _pendingFonts.Clear();
        }

        public static bool HasPendingFonts()
        {
            return !_pendingFonts.IsEmpty;
        }

        /// <summary>
        /// Verifica si una fuente específica está completamente cargada y lista para usar
        /// </summary>
        public static bool IsFontLoaded(uint fontId)
        {
            return _loadedFonts.TryGetValue(fontId, out var data) && data.IsLoaded;
        }

        /// <summary>
        /// Obtiene el ID real de la textura del atlas de fuentes
        /// </summary>
        public static uint GetFontTextureId(uint fontId)
        {
            return _loadedFonts.TryGetValue(fontId, out var data) ? data.TextureId : 0;
        }

    }
}