using StbTrueTypeSharp;
using System.Collections.Concurrent;
using static AvalonInjectLib.OpenGLInterop;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    /// <summary>
    /// Optimized font renderer with non-blocking operations to prevent OpenGL hook freezing.
    /// Designed for high-performance text rendering with minimal frame drops.
    /// </summary>
    internal static unsafe class FontRenderer
    {
        public static bool IsContexted { get; internal set; }

        // Reduced atlas size to prevent memory issues
        private const int DefaultAtlasSize = 512;
        // Moderate resolution multiplier for balance between quality and performance
        private const float ResolutionMultiplier = 1.5f;
        // Minimal padding to prevent bleeding
        private const int GlyphPadding = 1;
        // Maximum fonts to process per frame to prevent freezing
        private const int MaxFontsPerFrame = 1;

        // Thread-safe collections for asynchronous font handling
        private static readonly ConcurrentQueue<PendingFont> _pendingFonts = new();
        private static readonly ConcurrentDictionary<uint, FontData> _loadedFonts = new();
        private static uint _nextPendingId = 1;

        /// <summary>
        /// Detailed glyph metrics for precise text positioning
        /// </summary>
        public struct GlyphMetrics
        {
            public float Advance;
            public float BearingX;
            public float BearingY;
            public float Width;
            public float Height;
        }

        /// <summary>
        /// Enumeration for different text centering modes
        /// </summary>
        public enum CenteringMode
        {
            Visual,
            Geometric,
            CapHeight
        }

        /// <summary>
        /// Lightweight font metrics structure
        /// </summary>
        public struct FontVerticalMetrics
        {
            public float Ascent { get; set; }
            public float Descent { get; set; }
            public float LineGap { get; set; }
            public float LineHeight { get; set; }

            /// <summary>
            /// Calculates the baseline position for centered text within a given element
            /// </summary>
            /// <param name="elementY">Top Y position of the element</param>
            /// <param name="elementHeight">Height of the element</param>
            /// <param name="mode">Centering mode to use</param>
            /// <returns>Baseline Y position for centered text</returns>
            public float GetCenteredBaseline(float elementY, float elementHeight, CenteringMode mode = CenteringMode.Visual)
            {
                float elementCenter = elementY + (elementHeight / 2);
                return elementCenter - (Ascent - Descent) / 2;
            }
        }

        /// <summary>
        /// Lightweight font loading request
        /// </summary>
        private struct PendingFont
        {
            public uint PendingId;
            public int Size;
            public byte[] FontData;
        }

        /// <summary>
        /// Optimized font data structure
        /// </summary>
        public struct FontData
        {
            public Dictionary<char, GlyphData> Glyphs;
            public uint TextureId;
            public float FontScale;
            public float BaseSize;
            public int Ascent;
            public int Descent;
            public int LineGap;
            public bool IsLoaded;
        }

        /// <summary>
        /// Compact glyph rendering data
        /// </summary>
        public struct GlyphData
        {
            public float Advance;
            public float BearingX;
            public float BearingY;
            public int Width;
            public int Height;
            public Rect TexCoords;
        }

        /// <summary>
        /// Fast font request from file path (non-blocking)
        /// </summary>
        /// <param name="fontPath">Path to the font file</param>
        /// <param name="size">Desired font size in pixels</param>
        /// <param name="actualSize">Returns the actual size that will be used</param>
        /// <returns>Font ID for later use, or 0 if failed</returns>
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
                // Load font data asynchronously to prevent blocking
                byte[] fontData = File.ReadAllBytes(fontPath);
                uint pendingId = _nextPendingId++;

                _pendingFonts.Enqueue(new PendingFont
                {
                    PendingId = pendingId,
                    Size = size,
                    FontData = fontData
                });

                Logger.Debug($"Queued font {pendingId} from {fontPath} (Size: {size})", "FontRenderer");
                return pendingId;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to queue font from {fontPath}: {ex.Message}", "FontRenderer");
                return 0;
            }
        }

        /// <summary>
        /// Fast font request from byte array (non-blocking)
        /// </summary>
        /// <param name="fontData">Font data as byte array</param>
        /// <param name="size">Desired font size in pixels</param>
        /// <param name="actualSize">Returns the actual size that will be used</param>
        /// <returns>Font ID for later use, or 0 if failed</returns>
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

                Logger.Debug($"Queued font {pendingId} from embedded data (Size: {size})", "FontRenderer");
                return pendingId;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to queue font from embedded data: {ex.Message}", "FontRenderer");
                return 0;
            }
        }

        /// <summary>
        /// Processes pending fonts with frame-rate limiting to prevent freezing
        /// Only processes one font per call to maintain smooth performance
        /// </summary>
        public static void ProcessPendingFonts()
        {
            if (!IsContexted)
            {
                return; // Silently return to avoid spam
            }

            // Process only one font per frame to prevent freezing
            if (_pendingFonts.TryDequeue(out var pending))
            {
                try
                {
                    var fontData = LoadFontDataOptimized(pending.FontData, pending.Size);
                    _loadedFonts[pending.PendingId] = fontData;

                    Logger.Debug($"Loaded font {pending.PendingId} (Size: {pending.Size})", "FontRenderer");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load font {pending.PendingId}: {ex.Message}", "FontRenderer");
                }
            }
        }

        /// <summary>
        /// Optimized font loading with minimal OpenGL operations
        /// </summary>
        /// <param name="fontData">Raw font file data</param>
        /// <param name="size">Font size in pixels</param>
        /// <returns>Optimized font data structure</returns>
        internal static FontData LoadFontDataOptimized(byte[] fontData, int size)
        {
            var result = new FontData
            {
                Glyphs = new Dictionary<char, GlyphData>(),
                BaseSize = size,
                IsLoaded = false
            };

            // Fast STB TrueType font initialization
            var stbFont = new StbTrueType.stbtt_fontinfo();
            fixed (byte* ptr = fontData)
            {
                if (StbTrueType.stbtt_InitFont(stbFont, ptr, 0) == 0)
                    throw new Exception("Failed to initialize font");
            }

            // Quick metrics calculation
            int ascent, descent, lineGap;
            StbTrueType.stbtt_GetFontVMetrics(stbFont, &ascent, &descent, &lineGap);

            // Balanced resolution for quality vs performance
            float pixelHeight = size * ResolutionMultiplier;
            result.FontScale = StbTrueType.stbtt_ScaleForPixelHeight(stbFont, pixelHeight);
            result.Ascent = ascent;
            result.Descent = descent;
            result.LineGap = lineGap;

            // Create optimized atlas
            byte[] atlasData = new byte[DefaultAtlasSize * DefaultAtlasSize * 4];
            int currentX = GlyphPadding;
            int currentY = GlyphPadding;
            int currentLineHeight = 0;

            // Process essential ASCII characters only (fast)
            for (int i = 32; i < 127; i++)
            {
                char c = (char)i;
                AddGlyphToAtlasOptimized(stbFont, c, ref result, atlasData, ref currentX, ref currentY, ref currentLineHeight);
            }

            // Fast texture creation
            result.TextureId = CreateOptimizedFontTexture(atlasData);
            result.IsLoaded = true;

            return result;
        }

        /// <summary>
        /// Optimized glyph addition with minimal processing
        /// </summary>
        private static void AddGlyphToAtlasOptimized(StbTrueType.stbtt_fontinfo font, char c, ref FontData fontData, byte[] atlasData, ref int currentX, ref int currentY, ref int currentLineHeight)
        {
            // Fast glyph metrics
            int advance, bearing;
            StbTrueType.stbtt_GetCodepointHMetrics(font, c, &advance, &bearing);

            // Quick bounding box
            int x0, y0, x1, y1;
            StbTrueType.stbtt_GetCodepointBitmapBox(font, c, fontData.FontScale, fontData.FontScale, &x0, &y0, &x1, &y1);

            int w = x1 - x0;
            int h = y1 - y0;

            // Handle empty glyphs quickly
            if (w <= 0 || h <= 0)
            {
                fontData.Glyphs[c] = new GlyphData
                {
                    Advance = (advance * fontData.FontScale) / ResolutionMultiplier,
                    BearingX = (bearing * fontData.FontScale) / ResolutionMultiplier,
                    BearingY = y0 / ResolutionMultiplier,
                    Width = 0,
                    Height = 0,
                    TexCoords = new Rect(0, 0, 0, 0)
                };
                return;
            }

            // Simple atlas positioning
            if (currentX + w + GlyphPadding >= DefaultAtlasSize)
            {
                currentX = GlyphPadding;
                currentY += currentLineHeight + GlyphPadding;
                currentLineHeight = 0;

                if (currentY + h + GlyphPadding >= DefaultAtlasSize)
                {
                    // Skip character if atlas is full
                    return;
                }
            }

            if (h > currentLineHeight) currentLineHeight = h;

            int xPos = currentX;
            int yPos = currentY;

            // Fast glyph rasterization
            byte[] bitmap = new byte[w * h];
            fixed (byte* ptr = bitmap)
            {
                StbTrueType.stbtt_MakeCodepointBitmap(font, ptr, w, h, w, fontData.FontScale, fontData.FontScale, c);
            }

            // Optimized atlas copying
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

            // Store glyph data
            fontData.Glyphs[c] = new GlyphData
            {
                Advance = (advance * fontData.FontScale) / ResolutionMultiplier,
                BearingX = (bearing * fontData.FontScale) / ResolutionMultiplier,
                BearingY = y0 / ResolutionMultiplier,
                Width = w,
                Height = h,
                TexCoords = new Rect(
                    (float)xPos / DefaultAtlasSize,
                    (float)yPos / DefaultAtlasSize,
                    (float)w / DefaultAtlasSize,
                    (float)h / DefaultAtlasSize
                )
            };

            currentX += w + GlyphPadding;
        }

        /// <summary>
        /// Fast OpenGL texture creation with minimal settings
        /// </summary>
        /// <param name="atlasData">Texture atlas data</param>
        /// <returns>OpenGL texture ID</returns>
        private static uint CreateOptimizedFontTexture(byte[] atlasData)
        {
            uint textureId;
            glGenTextures(1, &textureId);
            glBindTexture(GL_TEXTURE_2D, textureId);

            fixed (byte* ptr = atlasData)
            {
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, DefaultAtlasSize, DefaultAtlasSize,
                            0, GL_RGBA, GL_UNSIGNED_BYTE, (IntPtr)ptr);
            }

            // Minimal texture parameters for speed
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

            return textureId;
        }

        /// <summary>
        /// Checks if a font is ready for use
        /// </summary>
        /// <param name="pendingId">Font ID to check</param>
        /// <returns>True if font is loaded and ready</returns>
        public static bool IsFontReady(uint pendingId) => _loadedFonts.ContainsKey(pendingId);

        /// <summary>
        /// Gets font data for a loaded font
        /// </summary>
        /// <param name="pendingId">Font ID</param>
        /// <returns>Font data structure</returns>
        public static FontData GetFontData(uint pendingId) => _loadedFonts.TryGetValue(pendingId, out var data) ? data : default;

        /// <summary>
        /// Optimized text rendering with minimal OpenGL state changes
        /// </summary>
        /// <param name="fontId">Font ID to use</param>
        /// <param name="text">Text to render</param>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="size">Font size</param>
        /// <param name="color">Text color</param>
        public static void DrawText(uint fontId, string text, float x, float y, float size, Color color)
        {
            if (!_loadedFonts.TryGetValue(fontId, out var font) || !font.IsLoaded) return;

            // Minimal OpenGL state setup
            glEnable(GL_BLEND);
            glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
            glEnable(GL_TEXTURE_2D);
            glBindTexture(GL_TEXTURE_2D, font.TextureId);

            // Fast scale calculation
            float scale = size / font.BaseSize;
            float startX = x;
            float baseline = y + (font.Ascent * font.FontScale * scale) / ResolutionMultiplier;

            // Optimized character rendering
            glBegin(GL_QUADS);
            glColor4f(color.R, color.G, color.B, color.A);

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

                float xPos = startX + (glyph.BearingX * scale);
                float yPos = baseline + (glyph.BearingY * scale);
                float glyphWidth = (glyph.Width * scale) / ResolutionMultiplier;
                float glyphHeight = (glyph.Height * scale) / ResolutionMultiplier;

                // Render glyph quad
                glTexCoord2f(glyph.TexCoords.X, glyph.TexCoords.Y);
                glVertex2f(xPos, yPos);

                glTexCoord2f(glyph.TexCoords.X + glyph.TexCoords.Width, glyph.TexCoords.Y);
                glVertex2f(xPos + glyphWidth, yPos);

                glTexCoord2f(glyph.TexCoords.X + glyph.TexCoords.Width, glyph.TexCoords.Y + glyph.TexCoords.Height);
                glVertex2f(xPos + glyphWidth, yPos + glyphHeight);

                glTexCoord2f(glyph.TexCoords.X, glyph.TexCoords.Y + glyph.TexCoords.Height);
                glVertex2f(xPos, yPos + glyphHeight);

                startX += glyph.Advance * scale;
            }

            glEnd();
            glDisable(GL_TEXTURE_2D);
            glDisable(GL_BLEND);
        }

        /// <summary>
        /// Fast text measurement
        /// </summary>
        /// <param name="fontId">Font ID to use</param>
        /// <param name="text">Text to measure</param>
        /// <param name="scale">Scale factor</param>
        /// <returns>Text dimensions as Vector2</returns>
        internal static Vector2 MeasureText(uint fontId, string text, float scale)
        {
            if (!_loadedFonts.TryGetValue(fontId, out var font) || !font.IsLoaded)
                return Vector2.Zero;

            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;

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
                    currentWidth += glyph.Advance * scale;
                }
            }

            if (currentWidth > maxWidth)
                maxWidth = currentWidth;

            float height = lineCount * GetLineHeight(fontId, scale);
            return new Vector2(maxWidth, height);
        }

        /// <summary>
        /// Fast line height calculation
        /// </summary>
        /// <param name="fontId">Font ID</param>
        /// <param name="scale">Scale factor</param>
        /// <returns>Line height in pixels</returns>
        internal static float GetLineHeight(uint fontId, float scale = 1.0f)
        {
            if (!_loadedFonts.TryGetValue(fontId, out var font) || !font.IsLoaded)
                return 0f;

            return ((font.Ascent - font.Descent + font.LineGap) * font.FontScale * scale) / ResolutionMultiplier;
        }

        /// <summary>
        /// Fast font deletion
        /// </summary>
        /// <param name="pendingId">Font ID to delete</param>
        public static void DeleteFont(uint pendingId)
        {
            if (_loadedFonts.TryRemove(pendingId, out var fontData))
            {
                uint tex = fontData.TextureId;
                glDeleteTextures(1, &tex);
            }
        }

        /// <summary>
        /// Fast cleanup of all fonts
        /// </summary>
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

        /// <summary>
        /// Checks if there are fonts waiting to be processed
        /// </summary>
        /// <returns>True if fonts are pending</returns>
        public static bool HasPendingFonts()
        {
            return !_pendingFonts.IsEmpty;
        }

        /// <summary>
        /// Verifies if a specific font is loaded and ready
        /// </summary>
        /// <param name="fontId">Font ID to check</param>
        /// <returns>True if font is loaded and ready</returns>
        public static bool IsFontLoaded(uint fontId)
        {
            return _loadedFonts.TryGetValue(fontId, out var data) && data.IsLoaded;
        }

        /// <summary>
        /// Gets the OpenGL texture ID for a font atlas
        /// </summary>
        /// <param name="fontId">Font ID</param>
        /// <returns>OpenGL texture ID, or 0 if not found</returns>
        public static uint GetFontTextureId(uint fontId)
        {
            return _loadedFonts.TryGetValue(fontId, out var data) ? data.TextureId : 0;
        }
    }
}