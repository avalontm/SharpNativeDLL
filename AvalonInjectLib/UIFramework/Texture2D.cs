using System;
using System.IO;

namespace AvalonInjectLib.Graphics
{
    /// <summary>
    /// Representa una textura 2D que puede ser cargada y renderizada
    /// </summary>
    public class Texture2D : IDisposable
    {
        /// <summary>
        /// Ancho de la textura en píxeles
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Alto de la textura en píxeles
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Formato de los píxeles
        /// </summary>
        public PixelFormat Format { get; private set; }

        /// <summary>
        /// Identificador nativo de la textura (ID pendiente o real)
        /// </summary>
        public uint TextureId { get; private set; }

        /// <summary>
        /// Indica si la textura ha sido solicitada para carga
        /// </summary>
        internal bool IsRequested { get; private set; }

        /// <summary>
        /// Indica si la textura ha sido cargada correctamente en OpenGL
        /// </summary>
        public bool IsLoaded => IsRequested && TextureRenderer.IsTextureReady(TextureId);

        /// <summary>
        /// Ruta del archivo de la textura
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// Crea una nueva instancia de Texture2D
        /// </summary>
        public Texture2D()
        {
            TextureId = 0;
            IsRequested = false;
            FilePath = string.Empty;
        }

        /// <summary>
        /// Crea una nueva instancia de Texture2D y solicita la carga desde archivo
        /// </summary>
        /// <param name="filePath">Ruta del archivo de imagen</param>
        public Texture2D(string filePath)
        {
            TextureId = 0;
            IsRequested = false;
            FilePath = string.Empty;
            LoadFromFile(filePath);
        }

        /// <summary>
        /// Solicita la carga de una textura desde un archivo
        /// </summary>
        /// <param name="filePath">Ruta del archivo de imagen</param>
        /// <returns>True si la solicitud fue exitosa</returns>
        public bool LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Logger.Debug($"Archivo de textura no encontrado: {filePath}");
                return false;
            }

            try
            {
                // Liberar textura anterior si existe
                Dispose();

                // Solicitar la textura (esto no requiere contexto OpenGL)
                int width, height;
                PixelFormat format;
                TextureId = TextureRenderer.RequestTexture(filePath, out width, out height, out format);

                if (TextureId != 0)
                {
                    Width = width;
                    Height = height;
                    Format = format;
                    FilePath = filePath;
                    IsRequested = true;

                    Logger.Debug($"Textura solicitada: {filePath} (ID: {TextureId}, {Width}x{Height})", "Texture2D");
                    return true;
                }
                else
                {
                    Logger.Debug($"No se pudo solicitar la textura: {filePath}", "Texture2D");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error al solicitar textura desde {filePath}: {ex.Message}", "Texture2D");
                return false;
            }
        }

        /// <summary>
        /// Verifica si la textura está lista para renderizar
        /// </summary>
        /// <returns>True si la textura está lista</returns>
        internal bool IsReady()
        {
            return IsLoaded;
        }

        /// <summary>
        /// Obtiene información sobre el estado de la textura
        /// </summary>
        /// <returns>String con información de debug</returns>
        public string GetStatusInfo()
        {
            if (!IsRequested)
                return "No solicitada";

            if (IsLoaded)
                return $"Cargada (ID: {TextureRenderer.GetRealTextureId(TextureId)})";

            return $"Pendiente (ID: {TextureId})";
        }

        /// <summary>
        /// Libera los recursos de la textura
        /// </summary>
        public void Dispose()
        {
            if (TextureId != 0 && IsRequested)
            {
                TextureRenderer.DeleteTexture(TextureId);
                TextureId = 0;
                IsRequested = false;
                FilePath = string.Empty;
            }
            GC.SuppressFinalize(this);
        }

        ~Texture2D()
        {
            Dispose();
        }
    }

    /// <summary>
    /// Formatos de píxel soportados
    /// </summary>
    public enum PixelFormat
    {
        /// <summary>Formato RGBA (8 bits por canal)</summary>
        RGBA32,
        /// <summary>Formato RGB (8 bits por canal)</summary>
        RGB24,
        /// <summary>Formato con un solo canal (8 bits)</summary>
        R8,
        /// <summary>Formato RG (8 bits por canal)</summary>
        RG16
    }
}