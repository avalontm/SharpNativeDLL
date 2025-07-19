using StbTrueTypeSharp;
using System.Collections.Concurrent;
using static AvalonInjectLib.OpenGLInterop;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    /// <summary>
    /// Ultra-optimized font renderer with minimal resource usage and memory safety
    /// </summary>
    internal static unsafe class FontRenderer
    {
        // Reduced atlas size for better memory usage
        private const int DefaultAtlasSize = 256;
        // Minimal resolution multiplier
        private const float ResolutionMultiplier = 1.0f;
        // Minimal padding
        private const int GlyphPadding = 1;
        // Process only 1 font per frame to prevent stuttering
        private const int MaxFontsPerFrame = 1;
        // Maximum text length to prevent infinite loops
        private const int MaxTextLength = 1000;
        // Cache size limit to prevent memory leaks
        private const int MaxCachedFonts = 50;

        // Thread-safe collections with size limits
        private static readonly ConcurrentQueue<PendingFont> _pendingFonts = new();
        private static readonly ConcurrentDictionary<uint, FontData> _loadedFonts = new();
        private static readonly ConcurrentDictionary<string, uint> _fontPathCache = new();
        private static uint _nextFontId = 1;
        private static readonly object _lockObject = new object();

        // Memory usage tracking
        private static long _totalMemoryUsage = 0;
        private static readonly long MaxMemoryUsage = 100 * 1024 * 1024; // 100MB limit

        /// <summary>
        /// Lightweight glyph metrics
        /// </summary>
        public struct GlyphMetrics
        {
            public float Advance;
            public float BearingX;
            public float BearingY;
            public float Width;
            public float Height;
        }

        public enum CenteringMode
        {
            Visual,
            Geometric,
            CapHeight
        }

        public struct FontVerticalMetrics
        {
            public float Ascent { get; set; }
            public float Descent { get; set; }
            public float LineGap { get; set; }
            public float LineHeight { get; set; }

            public float GetCenteredBaseline(float elementY, float elementHeight, CenteringMode mode = CenteringMode.Visual)
            {
                float elementCenter = elementY + (elementHeight / 2);
                return elementCenter - (Ascent - Descent) / 2;
            }
        }

        private struct PendingFont
        {
            public uint FontId;
            public int Size;
            public byte[] FontData;
            public bool IsUrgent;
        }

        /// <summary>
        /// Memory-optimized font data structure
        /// </summary>
        public struct FontData
        {
            public Dictionary<char, GlyphData> Glyphs;
            public uint TextureId;
            public float FontScale;
            public float BaseSize;
            public float Ascent;
            public float Descent;
            public float LineGap;
            public bool IsLoaded;
            public bool IsLoading;
            public long MemorySize; // Track memory usage
        }

        public struct GlyphData
        {
            public float Advance;
            public float BearingX;
            public float BearingY;
            public float Width;
            public float Height;
            public Rect TexCoords;
        }

        /// <summary>
        /// Memory-safe synchronous font loading
        /// </summary>
        public static uint LoadFontSync(string fontPath, int size)
        {
            if (!IsValidPath(fontPath)) return 0;

            // Check memory limit before loading
            if (_totalMemoryUsage > MaxMemoryUsage)
            {
                CleanupOldestFonts();
            }

            string cacheKey = $"{fontPath}|{size}";
            if (_fontPathCache.TryGetValue(cacheKey, out uint cachedId))
            {
                if (_loadedFonts.TryGetValue(cachedId, out var cachedFont) && cachedFont.IsLoaded)
                    return cachedId;
            }

            try
            {
                byte[] fontData = File.ReadAllBytes(fontPath);
                if (fontData == null || fontData.Length == 0 || fontData.Length > 50 * 1024 * 1024) // 50MB max font file
                {
                    Logger.Error($"Invalid font file size: {fontPath}", "FontRenderer");
                    return 0;
                }
                return LoadFontSyncFromData(fontData, size, cacheKey);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load font {fontPath}: {ex.Message}", "FontRenderer");
                return 0;
            }
        }

        /// <summary>
        /// Ultra-optimized synchronous loading from byte array
        /// </summary>
        public static uint LoadFontSyncFromData(byte[] fontData, int size, string cacheKey = null)
        {
            if (!IsValidFontData(fontData, size)) return 0;

            lock (_lockObject)
            {
                uint fontId = _nextFontId++;
                try
                {
                    var fontDataStruct = LoadFontDataUltraOptimized(fontData, size);
                    fontDataStruct.IsLoaded = true;
                    fontDataStruct.IsLoading = false;

                    _loadedFonts[fontId] = fontDataStruct;
                    _totalMemoryUsage += fontDataStruct.MemorySize;

                    if (!string.IsNullOrEmpty(cacheKey))
                        _fontPathCache[cacheKey] = fontId;

                    Logger.Debug($"Loaded font {fontId} sync (Size: {size}, Memory: {fontDataStruct.MemorySize / 1024}KB)", "FontRenderer");
                    return fontId;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load font {fontId}: {ex.Message}", "FontRenderer");
                    return 0;
                }
            }
        }

        public static uint RequestFontAsync(string fontPath, int size)
        {
            if (!IsValidPath(fontPath)) return 0;

            string cacheKey = $"{fontPath}|{size}";
            if (_fontPathCache.TryGetValue(cacheKey, out uint cachedId))
            {
                if (_loadedFonts.TryGetValue(cachedId, out var cachedFont) && cachedFont.IsLoaded)
                    return cachedId;
            }

            try
            {
                byte[] fontData = File.ReadAllBytes(fontPath);
                return RequestFontAsync(fontData, size, cacheKey);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to queue font {fontPath}: {ex.Message}", "FontRenderer");
                return 0;
            }
        }

        public static uint RequestFontAsync(byte[] fontData, int size, string cacheKey = null)
        {
            if (!IsValidFontData(fontData, size)) return 0;

            try
            {
                uint fontId = _nextFontId++;

                _loadedFonts[fontId] = new FontData
                {
                    IsLoaded = false,
                    IsLoading = true,
                    BaseSize = size,
                    MemorySize = 0
                };

                _pendingFonts.Enqueue(new PendingFont
                {
                    FontId = fontId,
                    Size = size,
                    FontData = fontData,
                    IsUrgent = false
                });

                if (!string.IsNullOrEmpty(cacheKey))
                    _fontPathCache[cacheKey] = fontId;

                return fontId;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to queue font: {ex.Message}", "FontRenderer");
                return 0;
            }
        }

        public static bool ForceLoadFont(uint fontId)
        {
            if (_loadedFonts.TryGetValue(fontId, out var fontData) && fontData.IsLoaded) return true;

            var tempQueue = new Queue<PendingFont>();
            bool found = false;

            while (_pendingFonts.TryDequeue(out var pending))
            {
                if (pending.FontId == fontId)
                {
                    try
                    {
                        var loadedFont = LoadFontDataUltraOptimized(pending.FontData, pending.Size);
                        loadedFont.IsLoaded = true;
                        loadedFont.IsLoading = false;
                        _loadedFonts[fontId] = loadedFont;
                        _totalMemoryUsage += loadedFont.MemorySize;
                        found = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Failed to force load font {fontId}: {ex.Message}", "FontRenderer");
                        return false;
                    }
                    break;
                }
                else
                {
                    tempQueue.Enqueue(pending);
                }
            }

            while (tempQueue.Count > 0)
                _pendingFonts.Enqueue(tempQueue.Dequeue());

            return found;
        }

        /// <summary>
        /// Frame-rate optimized font processing
        /// </summary>
        public static void ProcessPendingFonts()
        {
            int processed = 0;
            while (processed < MaxFontsPerFrame && _pendingFonts.TryDequeue(out var pending))
            {
                try
                {
                    // Check memory before processing
                    if (_totalMemoryUsage > MaxMemoryUsage)
                    {
                        CleanupOldestFonts();
                    }

                    var fontData = LoadFontDataUltraOptimized(pending.FontData, pending.Size);
                    fontData.IsLoaded = true;
                    fontData.IsLoading = false;
                    _loadedFonts[pending.FontId] = fontData;
                    _totalMemoryUsage += fontData.MemorySize;

                    processed++;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load font {pending.FontId}: {ex.Message}", "FontRenderer");
                    if (_loadedFonts.TryGetValue(pending.FontId, out var failedFont))
                    {
                        failedFont.IsLoading = false;
                        _loadedFonts[pending.FontId] = failedFont;
                    }
                }
            }
        }

        /// <summary>
        /// Ultra-optimized font loading with minimal memory allocation
        /// </summary>
        internal static FontData LoadFontDataUltraOptimized(byte[] fontData, int size)
        {
            var result = new FontData
            {
                Glyphs = new Dictionary<char, GlyphData>(95), // Pre-size for ASCII characters
                BaseSize = size,
                IsLoaded = false,
                IsLoading = true,
                MemorySize = 0
            };

            var stbFont = new StbTrueType.stbtt_fontinfo();
            fixed (byte* ptr = fontData)
            {
                if (StbTrueType.stbtt_InitFont(stbFont, ptr, 0) == 0)
                    throw new Exception("Failed to initialize font");
            }

            int ascent, descent, lineGap;
            StbTrueType.stbtt_GetFontVMetrics(stbFont, &ascent, &descent, &lineGap);

            float pixelHeight = size;
            result.FontScale = StbTrueType.stbtt_ScaleForPixelHeight(stbFont, pixelHeight);
            result.Ascent = ascent * result.FontScale;
            result.Descent = descent * result.FontScale;
            result.LineGap = lineGap * result.FontScale;

            // Use smaller atlas for memory efficiency
            byte[] atlasData = new byte[DefaultAtlasSize * DefaultAtlasSize * 4];
            int currentX = GlyphPadding;
            int currentY = GlyphPadding;
            int currentLineHeight = 0;

            // Only load essential characters (32-126)
            for (int i = 32; i <= 126; i++)
            {
                char c = (char)i;
                if (!AddGlyphToAtlasUltraOptimized(stbFont, c, ref result, atlasData, ref currentX, ref currentY, ref currentLineHeight))
                    break; // Stop if atlas is full
            }

            result.TextureId = CreateMinimalFontTexture(atlasData);
            result.MemorySize = atlasData.Length + (result.Glyphs.Count * 64); // Estimate memory usage
            result.IsLoaded = true;
            result.IsLoading = false;

            return result;
        }

        /// <summary>
        /// Memory-optimized glyph addition with early exit on atlas full
        /// </summary>
        private static bool AddGlyphToAtlasUltraOptimized(StbTrueType.stbtt_fontinfo font, char c, ref FontData fontData, byte[] atlasData, ref int currentX, ref int currentY, ref int currentLineHeight)
        {
            int advance, bearing;
            StbTrueType.stbtt_GetCodepointHMetrics(font, c, &advance, &bearing);

            int x0, y0, x1, y1;
            StbTrueType.stbtt_GetCodepointBitmapBox(font, c, fontData.FontScale, fontData.FontScale, &x0, &y0, &x1, &y1);

            int w = x1 - x0;
            int h = y1 - y0;

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
                return true;
            }

            // Check if atlas is full before processing
            if (currentX + w + GlyphPadding >= DefaultAtlasSize)
            {
                currentX = GlyphPadding;
                currentY += currentLineHeight + GlyphPadding;
                currentLineHeight = 0;

                if (currentY + h + GlyphPadding >= DefaultAtlasSize)
                    return false; // Atlas is full
            }

            if (h > currentLineHeight) currentLineHeight = h;

            int xPos = currentX;
            int yPos = currentY;

            // Render glyph directly to atlas to save memory
            fixed (byte* atlasPtr = atlasData)
            {
                byte* bitmapPtr = stackalloc byte[w * h];
                StbTrueType.stbtt_MakeCodepointBitmap(font, bitmapPtr, w, h, w, fontData.FontScale, fontData.FontScale, c);

                // Fast copy to atlas
                for (int row = 0; row < h; row++)
                {
                    for (int col = 0; col < w; col++)
                    {
                        int dstPos = ((yPos + row) * DefaultAtlasSize + (xPos + col)) * 4;
                        byte alpha = bitmapPtr[row * w + col];

                        atlasPtr[dstPos] = 255;     // R
                        atlasPtr[dstPos + 1] = 255; // G
                        atlasPtr[dstPos + 2] = 255; // B
                        atlasPtr[dstPos + 3] = alpha; // A
                    }
                }
            }

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

            currentX += w + GlyphPadding;
            return true;
        }

        /// <summary>
        /// Minimal OpenGL texture creation
        /// </summary>
        private static uint CreateMinimalFontTexture(byte[] atlasData)
        {
            uint textureId;
            glGenTextures(1, &textureId);
            glBindTexture(GL_TEXTURE_2D, textureId);

            fixed (byte* ptr = atlasData)
            {
                glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_RGBA, DefaultAtlasSize, DefaultAtlasSize,
                            0, GL_RGBA, GL_UNSIGNED_BYTE, (IntPtr)ptr);
            }

            // Minimal texture parameters
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_NEAREST);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_NEAREST);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);

            return textureId;
        }

        /// <summary>
        /// Ultra-optimized text rendering with memory safety
        /// </summary>
        public static void DrawText(uint fontId, string text, float x, float y, float size, Color color)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (!_loadedFonts.TryGetValue(fontId, out var font) || !font.IsLoaded || font.TextureId == 0) return;

            // Limit text length to prevent memory issues
            string safeText = text.Length > MaxTextLength ? text.Substring(0, MaxTextLength) : text;

            // Validate OpenGL state before rendering
            uint glError = glGetError();
            if (glError != GL_NO_ERROR) return;

            // Minimal state management
            bool wasBlendEnabled = glIsEnabled(GL_BLEND);
            bool wasTextureEnabled = glIsEnabled(GL_TEXTURE_2D);

            try
            {
                if (!wasBlendEnabled) glEnable(GL_BLEND);
                glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

                if (!wasTextureEnabled) glEnable(GL_TEXTURE_2D);

                if (!glIsTexture(font.TextureId)) return;
                glBindTexture(GL_TEXTURE_2D, font.TextureId);

                float scale = size / font.BaseSize;
                float startX = x;
                float baseline = y + (font.Ascent * scale);

                glBegin(GL_QUADS);
                glColor4f(color.R, color.G, color.B, color.A);

                // Fast character processing
                foreach (char c in safeText)
                {
                    if (c == '\n')
                    {
                        startX = x;
                        baseline += GetLineHeight(fontId, scale);
                        continue;
                    }

                    if (!font.Glyphs.TryGetValue(c, out var glyph)) continue;
                    if (glyph.Width <= 0 || glyph.Height <= 0)
                    {
                        startX += glyph.Advance * scale;
                        continue;
                    }

                    // Skip invalid coordinates
                    if (!IsValidTextureCoords(glyph.TexCoords))
                    {
                        startX += glyph.Advance * scale;
                        continue;
                    }

                    float xPos = startX + (glyph.BearingX * scale);
                    float yPos = baseline + (glyph.BearingY * scale);
                    float glyphWidth = glyph.Width * scale;
                    float glyphHeight = glyph.Height * scale;

                    // Fast quad rendering
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
            }
            catch (Exception ex)
            {
                Logger.Error($"Text rendering error: {ex.Message}", "FontRenderer");
                try { glEnd(); } catch { }
            }
            finally
            {
                // Minimal state restoration
                if (!wasTextureEnabled) glDisable(GL_TEXTURE_2D);
                if (!wasBlendEnabled) glDisable(GL_BLEND);
            }
        }

        // Helper methods for validation and cleanup
        private static bool IsValidPath(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        private static bool IsValidFontData(byte[] data, int size)
        {
            return data != null && data.Length > 0 && data.Length < 50 * 1024 * 1024 && size > 0 && size <= 200;
        }

        private static bool IsValidTextureCoords(Rect coords)
        {
            return coords.X >= 0 && coords.Y >= 0 &&
                   coords.X + coords.Width <= 1.0f &&
                   coords.Y + coords.Height <= 1.0f;
        }

        /// <summary>
        /// Clean up oldest fonts to free memory
        /// </summary>
        private static void CleanupOldestFonts()
        {
            if (_loadedFonts.Count <= MaxCachedFonts) return;

            var fontsToRemove = _loadedFonts
                .Take(_loadedFonts.Count - MaxCachedFonts + 10) // Remove extra fonts
                .Select(kvp => kvp.Key)
                .ToArray();

            foreach (var fontId in fontsToRemove)
            {
                DeleteFont(fontId);
            }

            Logger.Debug($"Cleaned up {fontsToRemove.Length} fonts to free memory", "FontRenderer");
        }

        public static bool IsFontReady(uint fontId)
        {
            return _loadedFonts.TryGetValue(fontId, out var font) && font.IsLoaded;
        }

        public static bool IsFontLoading(uint fontId)
        {
            return _loadedFonts.TryGetValue(fontId, out var font) && font.IsLoading;
        }

        public static FontData GetFontData(uint fontId)
        {
            return _loadedFonts.TryGetValue(fontId, out var data) ? data : default;
        }

        internal static Vector2 MeasureText(uint fontId, string text, float scale)
        {
            if (!_loadedFonts.TryGetValue(fontId, out var font) || !font.IsLoaded || string.IsNullOrEmpty(text))
                return Vector2.Zero;

            // Limit text for measurement
            string safeText = text.Length > MaxTextLength ? text.Substring(0, MaxTextLength) : text;

            float maxWidth = 0f;
            float currentWidth = 0f;
            int lineCount = 1;

            foreach (char c in safeText)
            {
                if (c == '\n')
                {
                    if (currentWidth > maxWidth) maxWidth = currentWidth;
                    currentWidth = 0f;
                    lineCount++;
                    continue;
                }

                if (font.Glyphs.TryGetValue(c, out var glyph))
                    currentWidth += glyph.Advance * scale;
            }

            if (currentWidth > maxWidth) maxWidth = currentWidth;
            return new Vector2(maxWidth, lineCount * GetLineHeight(fontId, scale));
        }

        internal static float GetLineHeight(uint fontId, float scale = 1.0f)
        {
            if (!_loadedFonts.TryGetValue(fontId, out var font) || !font.IsLoaded)
                return 0f;
            return (font.Ascent - font.Descent + font.LineGap) * scale;
        }

        public static void DeleteFont(uint fontId)
        {
            if (_loadedFonts.TryRemove(fontId, out var fontData))
            {
                if (fontData.TextureId != 0)
                {
                    uint tex = fontData.TextureId;
                    glDeleteTextures(1, &tex);
                }
                _totalMemoryUsage -= fontData.MemorySize;
            }

            // Clean up cache
            var keysToRemove = _fontPathCache.Where(kvp => kvp.Value == fontId).Select(kvp => kvp.Key).ToArray();
            foreach (var key in keysToRemove)
                _fontPathCache.TryRemove(key, out _);
        }

        public static void CleanupFonts()
        {
            foreach (var font in _loadedFonts.Values)
            {
                if (font.TextureId != 0)
                {
                    uint tex = font.TextureId;
                    glDeleteTextures(1, &tex);
                }
            }
            _loadedFonts.Clear();
            _pendingFonts.Clear();
            _fontPathCache.Clear();
            _totalMemoryUsage = 0;
        }

        public static bool HasPendingFonts() => !_pendingFonts.IsEmpty;
        public static bool IsFontLoaded(uint fontId) => _loadedFonts.TryGetValue(fontId, out var data) && data.IsLoaded;
        public static uint GetFontTextureId(uint fontId) => _loadedFonts.TryGetValue(fontId, out var data) ? data.TextureId : 0;

        public static (int loaded, int loading, int pending, long memoryMB) GetFontStats()
        {
            int loaded = 0, loading = 0;
            foreach (var font in _loadedFonts.Values)
            {
                if (font.IsLoaded) loaded++;
                else if (font.IsLoading) loading++;
            }
            return (loaded, loading, _pendingFonts.Count, _totalMemoryUsage / (1024 * 1024));
        }
    }
}