using StbImageSharp;
using static AvalonInjectLib.OpenGLInterop;
using static AvalonInjectLib.Structs;
using System.Collections.Concurrent;
using AvalonInjectLib.Graphics;
using Rectangle = AvalonInjectLib.Structs.Rectangle;

namespace AvalonInjectLib
{
    internal unsafe static class TextureRenderer
    {
        public static bool IsContexted { get; internal set; }

        // Cola para texturas pendientes de crear
        private static readonly ConcurrentQueue<PendingTexture> PendingTextures = new();

        // Diccionario para mapear IDs de texturas pendientes a texturas creadas
        private static readonly ConcurrentDictionary<uint, uint> TextureIdMap = new();

        // Contador para IDs únicos de texturas pendientes
        private static uint NextPendingId = 1;

        private struct PendingTexture
        {
            public uint PendingId;
            public string FilePath;
            public int Width;
            public int Height;
            public Graphics.PixelFormat Format;
            public byte[] ImageData;
        }

        // Método para solicitar la creación de una textura (no requiere contexto OpenGL)
        public static uint RequestTexture(string filePath, out int width, out int height, out Graphics.PixelFormat format)
        {
            width = 0;
            height = 0;
            format = Graphics.PixelFormat.RGBA32;

            if (!File.Exists(filePath))
            {
                Logger.Error($"Texture file not found: {filePath}", "TextureRenderer");
                return 0;
            }

            try
            {
                // Cargar imagen usando StbImageSharp
                using (var stream = File.OpenRead(filePath))
                {
                    var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                    width = image.Width;
                    height = image.Height;

                    // Determinar formato basado en componentes
                    switch (image.Comp)
                    {
                        case ColorComponents.RedGreenBlue:
                            format = Graphics.PixelFormat.RGB24;
                            break;
                        case ColorComponents.RedGreenBlueAlpha:
                            format = Graphics.PixelFormat.RGBA32;
                            break;
                        case ColorComponents.Grey:
                            format = Graphics.PixelFormat.R8;
                            break;
                        default:
                            format = Graphics.PixelFormat.RGB24;
                            break;
                    }

                    // Crear una copia de los datos de la imagen
                    byte[] imageData = new byte[image.Data.Length];
                    Array.Copy(image.Data, imageData, image.Data.Length);

                    // Crear entrada pendiente
                    var pendingTexture = new PendingTexture
                    {
                        PendingId = NextPendingId++,
                        FilePath = filePath,
                        Width = width,
                        Height = height,
                        Format = format,
                        ImageData = imageData
                    };

                    // Agregar a la cola de pendientes
                    PendingTextures.Enqueue(pendingTexture);

                    Logger.Debug($"Requested texture {pendingTexture.PendingId} from {filePath} ({width}x{height}, {format})", "TextureRenderer");
                    return pendingTexture.PendingId;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to request texture from {filePath}: {ex.Message}", "TextureRenderer");
                return 0;
            }
        }

        // Método para procesar texturas pendientes (debe llamarse desde el hook con contexto OpenGL)
        public static void ProcessPendingTextures()
        {
            if (!IsContexted) return;

            while (PendingTextures.TryDequeue(out var pendingTexture))
            {
                try
                {
                    // Generar textura OpenGL
                    uint texture;
                    glGenTextures(1, &texture);
                    uint textureId = texture;

                    if (glGetError() != GL_NO_ERROR)
                    {
                        Logger.Error($"Failed to generate texture for pending ID {pendingTexture.PendingId}");
                        continue;
                    }

                    glBindTexture(GL_TEXTURE_2D, textureId);

                    // Configurar parámetros de textura
                    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_LINEAR);
                    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_LINEAR);
                    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
                    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);

                    // Subir datos de textura
                    fixed (byte* ptr = pendingTexture.ImageData)
                    {
                        uint glFormat = pendingTexture.Format == Graphics.PixelFormat.RGB24 ? GL_RGB : GL_RGBA;
                        glTexImage2D(GL_TEXTURE_2D, 0, (int)glFormat, pendingTexture.Width, pendingTexture.Height, 0, glFormat, GL_UNSIGNED_BYTE, (IntPtr)ptr);
                    }

                    if (glGetError() != GL_NO_ERROR)
                    {
                        Logger.Error($"Failed to upload texture data for pending ID {pendingTexture.PendingId}");
                        glDeleteTextures(1, &textureId);
                        continue;
                    }

                    // Mapear ID pendiente a ID real de textura
                    TextureIdMap[pendingTexture.PendingId] = textureId;

                    Logger.Debug($"Created texture {textureId} from pending ID {pendingTexture.PendingId} ({pendingTexture.Width}x{pendingTexture.Height}, {pendingTexture.Format})", "TextureRenderer");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to process pending texture {pendingTexture.PendingId}: {ex.Message}", "TextureRenderer");
                }
            }
        }

        // Método para obtener el ID real de textura OpenGL
        public static uint GetRealTextureId(uint pendingId)
        {
            return TextureIdMap.TryGetValue(pendingId, out uint realId) ? realId : 0;
        }

        // Método para verificar si una textura está lista
        public static bool IsTextureReady(uint pendingId)
        {
            return TextureIdMap.ContainsKey(pendingId);
        }

        // Método mejorado para el Renderer.cs
        public static void DrawTexture(Texture2D texture, Rectangle destRect, Rectangle sourceRect, Color tintColor)
        {
            // Verificar si la textura está lista antes de renderizar
            if (texture != null && texture.IsLoaded)
            {
                // Calcular coordenadas de textura normalizadas
                float texLeft = (float)sourceRect.X / texture.Width;
                float texTop = (float)sourceRect.Y / texture.Height;
                float texRight = (float)(sourceRect.X + sourceRect.Width) / texture.Width;
                float texBottom = (float)(sourceRect.Y + sourceRect.Height) / texture.Height;

                DrawTextureWithCoords(texture.TextureId,
                    destRect.X, destRect.Y, destRect.Width, destRect.Height,
                    texLeft, texTop, texRight, texBottom, tintColor);
            }
            else if (texture != null && !texture.IsLoaded)
            {
                // Opcional: dibujar un placeholder o debug info
                Logger.Debug($"Textura no está lista: {texture.FilePath} - {texture.GetStatusInfo()}", "Renderer");
            }
        }

        // Método existente (mantener para compatibilidad)
        public static void DrawTexture(Texture2D texture, Rectangle rect, Color tintColor)
        {
            if (texture != null && texture.IsLoaded)
            {
                var sourceRect = new Rectangle(0, 0, texture.Width, texture.Height);
                DrawTexture(texture, rect, sourceRect, tintColor);
            }
            else if (texture != null && !texture.IsLoaded)
            {
                Logger.Debug($"Textura no está lista: {texture.FilePath} - {texture.GetStatusInfo()}", "Renderer");
            }
        }

        // Método mejorado para TextureRenderer
        public static unsafe void DrawTextureWithCoords(uint pendingId, float x, float y, float width, float height,
            float texLeft, float texTop, float texRight, float texBottom, Color color)
        {
            uint textureId = GetRealTextureId(pendingId);
            if (textureId <= 0) return;

            glEnable(GL_TEXTURE_2D);
            glBindTexture(GL_TEXTURE_2D, textureId);

            // Configurar modulación de color
            glColor4f(color.R, color.G, color.B, color.A);

            glBegin(GL_QUADS);
            {
                glTexCoord2f(texLeft, texTop); glVertex2f(x, y);
                glTexCoord2f(texRight, texTop); glVertex2f(x + width, y);
                glTexCoord2f(texRight, texBottom); glVertex2f(x + width, y + height);
                glTexCoord2f(texLeft, texBottom); glVertex2f(x, y + height);
            }
            glEnd();
            glDisable(GL_TEXTURE_2D);
        }

        // Método existente (mantener para compatibilidad)
        public static unsafe void DrawTexture(uint pendingId, float x, float y, float width, float height, Color color)
        {
            DrawTextureWithCoords(pendingId, x, y, width, height, 0, 0, 1, 1, color);
        }

        public static unsafe void DeleteTexture(uint pendingId)
        {
            uint textureId = GetRealTextureId(pendingId);
            if (textureId <= 0) return;

            uint tex = textureId;
            glDeleteTextures(1, &tex);

            // Remover del mapeo
            TextureIdMap.TryRemove(pendingId, out _);

            Logger.Debug($"Deleted texture {textureId} (pending ID: {pendingId})", "TextureRenderer");
        }

        // Método para verificar si hay texturas pendientes
        public static bool HasPendingTextures()
        {
            return !PendingTextures.IsEmpty;
        }

        // Método para obtener el número de texturas pendientes
        public static int GetPendingTextureCount()
        {
            return PendingTextures.Count;
        }

        // Método para obtener el número de texturas cargadas
        public static int GetLoadedTextureCount()
        {
            return TextureIdMap.Count;
        }

        // Método para obtener información de una textura específica
        public static string GetTextureInfo(uint pendingId)
        {
            if (TextureIdMap.TryGetValue(pendingId, out uint realId))
            {
                return $"Loaded (Real ID: {realId})";
            }
            return "Pending";
        }

        // Método para limpiar todas las texturas
        public static void CleanupTextures()
        {
            foreach (var kvp in TextureIdMap)
            {
                uint tex = kvp.Value;
                glDeleteTextures(1, &tex);
            }
            TextureIdMap.Clear();

            // Limpiar cola de pendientes
            while (PendingTextures.TryDequeue(out _)) { }
        }

    }
}