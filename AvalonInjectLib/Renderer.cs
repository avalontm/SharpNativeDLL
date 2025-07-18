using AvalonInjectLib.Graphics;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static class Renderer
    {
        private static Stack<ViewMatrix> _transformStack = new Stack<ViewMatrix>();
        private static ViewMatrix _currentTransform = ViewMatrix.Identity;

        public enum GraphicsAPI
        {
            None, OpenGL, DirectX,
        }

        public static GraphicsAPI CurrentAPI = GraphicsAPI.OpenGL;

        public static int ScreenWidth
        {
            get
            {
                if (CurrentAPI == GraphicsAPI.OpenGL)
                {
                    return OpenGLHook.ScreenWidth;
                }
                else
                {
                    return 1920;
                }
            }
        }

        public static int ScreenHeight
        {
            get
            {
                if (CurrentAPI == GraphicsAPI.OpenGL)
                {
                    return OpenGLHook.ScreenHeight;
                }
                else
                {
                    return 1080;
                }
            }
        }


        public static void InitializeGraphics(uint processId, IntPtr swapChain = default)
        {
            if (CurrentAPI == GraphicsAPI.DirectX && swapChain != IntPtr.Zero)
            {
                DirectXHook.Initialize(swapChain);
                return;
            }

            OpenGLHook.Initialize(processId);

        }


        public static void SetRenderCallback(Action render)
        {
            if (CurrentAPI == GraphicsAPI.OpenGL)
            {
                OpenGLHook.AddRenderCallback(render);
            }
            else
            {
                DirectXHook.AddRenderCallback(render);
            }

        }

        public static void RemoveRenderCallback(Action render)
        {
            if (CurrentAPI == GraphicsAPI.OpenGL)
            {
                OpenGLHook.RemoveRenderCallback(render);
            }
            else
            {
                DirectXHook.RemoveRenderCallback(render);
            }

        }
        public static void DrawRect(float x, float y, float w, float h, UIFramework.Color color)
        {
            var apiColor = new Structs.Color
            {
                R = color.R / 255f,
                G = color.G / 255f,
                B = color.B / 255f,
                A = color.A / 255f
            };

            if (CurrentAPI == GraphicsAPI.OpenGL)
            {
                OpenGLHook.DrawFilledBox(x, y, w, h, apiColor);
            }
            else
            {
                DirectXHook.DrawRect(x, y, w, h, apiColor);
            }
        }

        public static void DrawRect(Rect bounds, UIFramework.Color color)
        {
            DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, color);
        }

        public static void DrawText(string text, Vector2 pos, UIFramework.Color color, Font font)
        {
            DrawText(text, pos.X, pos.Y, color, font);

        }

        public static void DrawText(string text, float x, float y, UIFramework.Color color, Font font)
        {
            // Convertir color al formato específico de la API
            var apiColor = new Structs.Color
            {
                R = color.R / 255f,
                G = color.G / 255f,
                B = color.B / 255f,
                A = color.A / 255f
            };

            var position = new Vector2(x, y);

            switch (CurrentAPI)
            {
                case GraphicsAPI.OpenGL:
                    OpenGLHook.DrawText(font.GetFontId(), text, position, apiColor, font.Size);
                    break;

                case GraphicsAPI.DirectX:
                    DirectXHook.DrawText(text, position, apiColor, font.Size);
                    break;

                default:
                    throw new NotSupportedException($"API no soportada: {CurrentAPI}");
            }
        }

        public static void DrawLine(float x1, float y1, float x2, float y2, float thickness, UIFramework.Color color)
        {
            var apiColor = new Structs.Color
            {
                R = color.R / 255f,
                G = color.G / 255f,
                B = color.B / 255f,
                A = color.A / 255f
            };

            var startPoint = new Vector2(x1, y1);
            var endPoint = new Vector2(x2, y2);

            if (CurrentAPI == GraphicsAPI.OpenGL)
            {
                OpenGLHook.DrawLine(startPoint, endPoint, thickness, apiColor);
            }
            else
            {
                DirectXHook.DrawLine(startPoint, endPoint, thickness, apiColor);
            }
        }

        public static void DrawLine(Vector2 vector21, Vector2 vector22, float thickness, UIFramework.Color color)
        {
            DrawLine(vector21.X, vector21.Y, vector22.X, vector22.Y, thickness, color);
        }

        // Sobrecarga para mantener compatibilidad con el código C original
        public static void DrawLine(float x1, float y1, float x2, float y2, float thickness, bool antiAlias, UIFramework.Color color)
        {
            // El parámetro antiAlias se mantiene para compatibilidad pero no se usa
            DrawLine(x1, y1, x2, y2, thickness, color);
        }

        public static void DrawCircle(Vector2 center, float radius, UIFramework.Color color)
        {
            var apiColor = new Structs.Color
            {
                R = color.R / 255f,
                G = color.G / 255f,
                B = color.B / 255f,
                A = color.A / 255f
            };

            if (CurrentAPI == GraphicsAPI.OpenGL)
            {
                OpenGLHook.DrawFilledCircle(center, radius, apiColor);
            }
            else
            {
          
            }
        }

        public static void DrawRoundedRect(float x, float y, float width, float height, float v, UIFramework.Color bgColor)
        {

        }

        public static void DrawRectOutline(float x, float y, float width, float height, UIFramework.Color color, float v)
        {

        }


        public static void Shutdown()
        {
           
        }

        public static Vector2 GetScreenSize()
        {
            if (CurrentAPI == GraphicsAPI.OpenGL)
            {
                return new Vector2(OpenGLHook.ScreenWidth, OpenGLHook.ScreenHeight);
            }
            else
            {
                return default(Vector2);
            }
        }

        public static void DrawTriangle(float x1, float y1, float x2, float y2, float x3, float y3, UIFramework.Color color)
        {
            DrawTriangle(
                new Vector2(x1, y1),
                new Vector2(x2, y2),
                new Vector2(x3, y3),
                color
            );
        }


        public static void DrawTriangle(Vector2 vector21, Vector2 vector22, Vector2 vector23, UIFramework.Color color)
        {
            var apiColor = new Structs.Color
            {
                R = color.R / 255f,
                G = color.G / 255f,
                B = color.B / 255f,
                A = color.A / 255f
            };

            OpenGLHook.DrawTriangle(vector21, vector22, vector23, apiColor);
        }

        public static void PushClip(Rect contentRect)
        {
          
        }

        public static void PopClip()
        {
          
        }

        public static void DrawBorder(float x, float y, float width, float height, float borderThickness, UIFramework.Color borderColor)
        {
          
        }

        public static Vector2 TransformPoint(float x, float y)
        {
            Vector4 point = new Vector4(x, y, 0, 1);
            Vector4 transformed = Vector4.Transform(point, _currentTransform);
            return new Vector2(transformed.X, transformed.Y);
        }

        public static void PushTransform(float x, float y)
        {
            _transformStack.Push(_currentTransform);
            _currentTransform *= ViewMatrix.CreateTranslation(x, y, 0);
        }

        public static void PopTransform()
        {
            if (_transformStack.Count > 0)
            {
                _currentTransform = _transformStack.Pop();
            }
        }

        public static void DrawRectOutline(Rect bounds, UIFramework.Color borderColor, float borderThickness)
        {
           
        }

        // Renderer.cs - Métodos actualizados para sistema diferido

        public static void ReleaseTexture(uint textureId)
        {
            TextureRenderer.DeleteTexture(textureId);
        }

        public static uint LoadTextureNative(string filePath, out int width, out int height, out PixelFormat format)
        {
            // Usar el nuevo sistema de texturas diferidas
            return TextureRenderer.RequestTexture(filePath, out width, out height, out format);
        }

        public static Texture2D LoadTexture(string filePath)
        {
            return new Texture2D(filePath);
        }

        public static void DrawTexture(Texture2D texture, Rectangle rect, UIFramework.Color tintColor)
        {
            // Verificar si la textura está lista antes de renderizar
            if (texture != null && texture.IsLoaded)
            {
                TextureRenderer.DrawTexture(texture, rect, tintColor);
            }
            else if (texture != null && !texture.IsLoaded)
            {
                // Opcional: dibujar un placeholder o debug info
                Logger.Debug($"Textura no está lista: {texture.FilePath} - {texture.GetStatusInfo()}", "Renderer");
            }
        }

        // Método adicional para verificar si una textura está lista
        public static bool IsTextureReady(Texture2D texture)
        {
            return texture != null && texture.IsLoaded;
        }

        // Método para obtener estadísticas de texturas
        public static string GetTextureStats()
        {
            // Puedes agregar lógica para obtener estadísticas del TextureRenderer
            return $"Texturas en cola: {TextureRenderer.GetPendingTextureCount()}";
        }
        public static void UnloadFont(uint fontId)
        {
   
        }

        internal static void SetClipRect(Rect contentRect)
        {
            throw new NotImplementedException();
        }
    }
}
