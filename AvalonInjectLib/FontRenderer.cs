using StbImageWriteSharp;
using StbTrueTypeSharp;
using static AvalonInjectLib.OpenGLInterop;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    // Enum para tipos de fuente
    public enum FontType
    {
        Normal,
        Bold
    }

    // Estructura para almacenar información de cada fuente
    internal struct FontInfo
    {
        public Dictionary<char, GlyphData> Glyphs;
        public int TextureId;
        public float FontScale;
        public int Ascent;
        public int Descent;
        public int LineGap;
        public byte[] AtlasData;
    }

    internal static unsafe class FontRenderer
    {
        // Diccionario para almacenar múltiples fuentes
        private static readonly Dictionary<FontType, FontInfo> _fonts = new();

        private static int _atlasSize = 512;
        private static float pixelHeight = 24;
        private static bool _isInitialized;

        // Glyph packing (se reutiliza para cada fuente)
        private static int _currentX = 0;
        private static int _currentY = 0;
        private static int _currentLineHeight = 0;

        public static bool IsInitialized { get { return _isInitialized; } }

        public static void Initialize(IntPtr originalContext)
        {
            Logger.Debug("Iniciando inicialización del renderer de fuentes...", "FontRenderer");

            if (IsInitialized) return;

            try
            {
                // Inicializar fuente normal
                InitializeFont(FontType.Normal, "AvalonInjectLib.ARIAL.TTF");

                // Inicializar fuente bold
                InitializeFont(FontType.Bold, "AvalonInjectLib.ARIALBD.TTF");

                Logger.Debug("Renderer de fuentes inicializado correctamente", "FontRenderer");
            }
            catch (Exception ex)
            {
                Logger.Error($"ERROR durante la inicialización: {ex.Message}", "FontRenderer");
                throw;
            }
            finally
            {
                _isInitialized = true;
            }
        }

        private static void InitializeFont(FontType fontType, string resourceName)
        {
            Logger.Debug($"Inicializando fuente {fontType}...", "FontRenderer");

            // Resetear variables de posición para cada fuente
            _currentX = 0;
            _currentY = 0;
            _currentLineHeight = 0;

            Logger.Debug($"Cargando datos de fuente embebida: {resourceName}...", "FontRenderer");
            byte[] fontData = EmbeddedResourceLoader.LoadResource(resourceName);
            Logger.Debug($"Fuente {fontType} cargada, tamaño: {fontData.Length} bytes", "FontRenderer");

            // 1. Inicializar STB TrueType
            var stbFontInfo = new StbTrueType.stbtt_fontinfo();
            fixed (byte* ptr = fontData)
            {
                Logger.Debug($"Inicializando fuente {fontType} con STB TrueType...", "FontRenderer");
                if (StbTrueType.stbtt_InitFont(stbFontInfo, ptr, 0) == 0)
                    throw new Exception($"Failed to load font {fontType}");
                Logger.Debug($"Fuente {fontType} inicializada correctamente con STB TrueType", "FontRenderer");
            }

            // 2. Obtener métricas de la fuente
            int ascent, descent, lineGap;
            int* pAscent = &ascent;
            int* pDescent = &descent;
            int* pLineGap = &lineGap;
            StbTrueType.stbtt_GetFontVMetrics(stbFontInfo, pAscent, pDescent, pLineGap);

            // 3. Configurar atlas
            float fontScale = StbTrueType.stbtt_ScaleForPixelHeight(stbFontInfo, pixelHeight);
            Logger.Debug($"Escala de fuente {fontType} calculada: {fontScale} para altura {pixelHeight}pix", "FontRenderer");
            Logger.Debug($"Métricas {fontType}: ascent={ascent}, descent={descent}, lineGap={lineGap}", "FontRenderer");

            // Crear atlas RGBA
            byte[] atlasData = new byte[_atlasSize * _atlasSize * 4];
            Logger.Debug($"Atlas de textura {fontType} creado, tamaño: {_atlasSize}x{_atlasSize} RGBA", "FontRenderer");

            // Crear estructura de información de fuente
            var fontInfo = new FontInfo
            {
                Glyphs = new Dictionary<char, GlyphData>(),
                FontScale = fontScale,
                Ascent = ascent,
                Descent = descent,
                LineGap = lineGap,
                AtlasData = atlasData
            };

            // 4. Rasterizar glifos básicos (ASCII)
            Logger.Debug($"Rasterizando glifos ASCII {fontType} (32-127)...", "FontRenderer");
            for (int i = 32; i < 128; i++)
            {
                char c = (char)i;
                AddGlyph(stbFontInfo, c, ref fontInfo);
            }
            Logger.Debug($"{fontInfo.Glyphs.Count} glifos {fontType} rasterizados", "FontRenderer");

            // 5. Crear textura OpenGL
            Logger.Debug($"Creando textura OpenGL {fontType}...", "FontRenderer");
            fontInfo.TextureId = CreateTexture(fontInfo.AtlasData);
            Logger.Debug($"Textura OpenGL {fontType} creada, ID: {fontInfo.TextureId}", "FontRenderer");

            // Guardar información de la fuente
            _fonts[fontType] = fontInfo;

            Logger.Debug($"Fuente {fontType} inicializada correctamente", "FontRenderer");
        }

        public static float GetScaleForDesiredSize(float desiredPixelHeight)
        {
            return desiredPixelHeight / pixelHeight;
        }

        private static void AddGlyph(StbTrueType.stbtt_fontinfo fontInfo, char c, ref FontInfo font)
        {
            // Obtener métricas del glifo
            int advance, bearing;
            int* pAdvance = &advance;
            int* pBearing = &bearing;
            StbTrueType.stbtt_GetCodepointHMetrics(fontInfo, c, pAdvance, pBearing);

            // Obtener cuadro delimitador del glifo
            int x0, y0, x1, y1;
            int* pX0 = &x0;
            int* pY0 = &y0;
            int* pX1 = &x1;
            int* pY1 = &y1;
            StbTrueType.stbtt_GetCodepointBitmapBox(fontInfo, c, font.FontScale, font.FontScale, pX0, pY0, pX1, pY1);

            // Calcular dimensiones del bitmap
            int w = x1 - x0;
            int h = y1 - y0;

            // Manejar caracteres especiales (espacios, etc.)
            if (w <= 0 || h <= 0)
            {
                // Para espacios y caracteres sin bitmap, solo guardar el advance
                font.Glyphs[c] = new GlyphData
                {
                    Advance = advance * font.FontScale,
                    BearingX = bearing * font.FontScale,
                    BearingY = y0,
                    Width = 0,
                    Height = 0,
                    TexCoords = new Rect { X = 0, Y = 0, Width = 0, Height = 0 }
                };
                return;
            }

            // Verificar si el glifo cabe en la línea actual
            if (_currentX + w + 2 >= _atlasSize) // +2 para padding
            {
                // No cabe, mover a siguiente línea
                _currentX = 0;
                _currentY += _currentLineHeight + 2; // +2 para padding entre líneas
                _currentLineHeight = 0;

                // Verificar si hay espacio vertical suficiente
                if (_currentY + h + 2 >= _atlasSize)
                {
                    Logger.Error($"Atlas lleno! No se pudo añadir el carácter: '{c}'", "FontRenderer");
                    return;
                }
            }

            // Actualizar altura máxima de la línea actual
            if (h > _currentLineHeight)
            {
                _currentLineHeight = h;
            }

            // Calcular posición exacta en el atlas (con padding)
            int xPos = _currentX + 1; // +1 para padding izquierdo
            int yPos = _currentY + 1; // +1 para padding superior

            // Rasterizar el glifo
            byte[] bitmap = new byte[w * h];
            fixed (byte* ptr = bitmap)
            {
                StbTrueType.stbtt_MakeCodepointBitmap(fontInfo, ptr, w, h, w, font.FontScale, font.FontScale, c);
            }

            // Copiar datos al atlas RGBA con alpha correcto
            for (int row = 0; row < h; row++)
            {
                int dstY = yPos + row;
                if (dstY >= _atlasSize) break;

                for (int col = 0; col < w; col++)
                {
                    int dstX = xPos + col;
                    if (dstX >= _atlasSize) break;

                    int srcPos = row * w + col;
                    int dstPos = (dstY * _atlasSize + dstX) * 4; // RGBA = 4 bytes por pixel

                    byte alpha = bitmap[srcPos];

                    // Formato RGBA: R, G, B, A
                    font.AtlasData[dstPos] = 255;     // R - blanco
                    font.AtlasData[dstPos + 1] = 255; // G - blanco
                    font.AtlasData[dstPos + 2] = 255; // B - blanco
                    font.AtlasData[dstPos + 3] = alpha; // A - usar el valor del bitmap como alpha
                }
            }

            // Guardar información del glifo con coordenadas correctas
            font.Glyphs[c] = new GlyphData
            {
                Advance = advance * font.FontScale,
                BearingX = bearing * font.FontScale,
                BearingY = y0, // Usar y0 directamente
                Width = w,
                Height = h,
                TexCoords = new Rect
                {
                    X = (float)xPos / _atlasSize,
                    Y = (float)yPos / _atlasSize,
                    Width = (float)w / _atlasSize,
                    Height = (float)h / _atlasSize
                }
            };

            // Actualizar posición para el siguiente glifo
            _currentX += w + 2; // Avanzar con padding derecho
        }

        private static int CreateTexture(byte[] atlasData)
        {
            uint texture;
            glGenTextures(1, &texture);
            int textureId = (int)texture;

            glBindTexture(GL_TEXTURE_2D, (uint)textureId);

            // Usar GL_RGBA para textura con alpha
            fixed (byte* ptr = atlasData)
            {
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, _atlasSize, _atlasSize, 0, GL_RGBA, GL_UNSIGNED_BYTE, (IntPtr)ptr);
            }

            // Configuración de filtrado
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);

            return textureId;
        }

        private static void SaveAtlasToFile(string filePath, byte[] atlasData)
        {
            try
            {
                using (var stream = File.Create(filePath))
                {
                    ImageWriter writer = new ImageWriter();
                    writer.WritePng(atlasData, _atlasSize, _atlasSize,
                                  ColorComponents.RedGreenBlueAlpha, stream);
                }
                Logger.Info($"Atlas guardado en: {filePath}", "FontRenderer");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error al guardar atlas: {ex.Message}", "FontRenderer");
            }
        }

        // Método principal para renderizar texto con tipo de fuente especificado
        public static unsafe void DrawText(string text, float x, float y, float scale, Color color, FontType fontType = FontType.Normal)
        {
            if (!_fonts.ContainsKey(fontType) || string.IsNullOrEmpty(text)) return;

            var font = _fonts[fontType];
            if (font.TextureId == 0) return;

            // Configurar textura y blending para texto
            glEnable(GL_TEXTURE_2D);
            glBindTexture(GL_TEXTURE_2D, (uint)font.TextureId);

            // Configurar texture environment para multiplicar color con textura
            glTexEnvi(GL_TEXTURE_ENV, GL_TEXTURE_ENV_MODE, GL_MODULATE);

            float startX = x;
            float currentY = y;

            // Calcular la línea base usando el ascent de la fuente
            float baseline = currentY + (font.Ascent * font.FontScale * scale);

            foreach (char c in text)
            {
                // Manejar saltos de línea
                if (c == '\n')
                {
                    startX = x;
                    currentY += pixelHeight * scale;
                    baseline = currentY + (font.Ascent * font.FontScale * scale);
                    continue;
                }

                if (!font.Glyphs.TryGetValue(c, out var glyph)) continue;

                // Saltear caracteres sin bitmap (espacios)
                if (glyph.Width == 0 || glyph.Height == 0)
                {
                    startX += glyph.Advance * scale;
                    continue;
                }

                // Calcular posición del glifo relativa a la baseline
                float xPos = startX + glyph.BearingX * scale;
                float yPos = baseline + glyph.BearingY * scale; // BearingY puede ser negativo

                float w = glyph.Width * scale;
                float h = glyph.Height * scale;

                // Coordenadas de textura
                float u0 = glyph.TexCoords.X;
                float v0 = glyph.TexCoords.Y;
                float u1 = u0 + glyph.TexCoords.Width;
                float v1 = v0 + glyph.TexCoords.Height;

                // Renderizar el quad
                glBegin(GL_QUADS);
                glColor4f(color.R, color.G, color.B, color.A);

                // Quad con coordenadas de textura
                glTexCoord2f(u0, v0); glVertex2f(xPos, yPos);           // Superior izquierdo
                glTexCoord2f(u1, v0); glVertex2f(xPos + w, yPos);       // Superior derecho
                glTexCoord2f(u1, v1); glVertex2f(xPos + w, yPos + h);   // Inferior derecho
                glTexCoord2f(u0, v1); glVertex2f(xPos, yPos + h);       // Inferior izquierdo

                glEnd();

                // Avanzar a la siguiente posición
                startX += glyph.Advance * scale;
            }

            // Limpiar estado
            glDisable(GL_TEXTURE_2D);
        }

        // Sobrecarga del método original para mantener compatibilidad
        public static unsafe void DrawText(string text, float x, float y, float scale, Color color)
        {
            DrawText(text, x, y, scale, color, FontType.Normal);
        }

        // Método auxiliar para obtener dimensiones del texto con tipo de fuente especificado
        public static (float width, float height) MeasureText(string text, float scale, FontType fontType = FontType.Normal)
        {
            if (string.IsNullOrEmpty(text) || !_fonts.ContainsKey(fontType)) return (0, 0);

            var font = _fonts[fontType];
            float width = 0;
            float height = (font.Ascent - font.Descent) * font.FontScale * scale;

            foreach (char c in text)
            {
                if (font.Glyphs.TryGetValue(c, out var glyph))
                {
                    width += glyph.Advance * scale;
                }
            }

            return (width, height);
        }

        // Sobrecarga del método original para mantener compatibilidad
        public static (float width, float height) MeasureText(string text, float scale)
        {
            return MeasureText(text, scale, FontType.Normal);
        }

        // Método para obtener información de una fuente específica
        public static FontInfo? GetFontInfo(FontType fontType)
        {
            return _fonts.ContainsKey(fontType) ? _fonts[fontType] : null;
        }

        // Método para verificar si una fuente está cargada
        public static bool IsFontLoaded(FontType fontType)
        {
            return _fonts.ContainsKey(fontType) && _fonts[fontType].TextureId > 0;
        }
    }
}