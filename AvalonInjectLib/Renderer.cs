using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static class Renderer
    {
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

        public static void DrawText(string text, float x, float y, UIFramework.Color color, int size = 24)
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
                    OpenGLHook.DrawText(text, position, apiColor, FontRenderer.GetScaleForDesiredSize(size));
                    break;

                case GraphicsAPI.DirectX:
                    DirectXHook.DrawText(text, position, apiColor, FontRenderer.GetScaleForDesiredSize(size));
                    break;

                default:
                    throw new NotSupportedException($"API no soportada: {CurrentAPI}");
            }
        }

        public static Vector2 MeasureText(string text, float size)
        {
            return new Vector2();
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

        // Sobrecarga para mantener compatibilidad con el código C original
        public static void DrawLine(float x1, float y1, float x2, float y2, float thickness, bool antiAlias, UIFramework.Color color)
        {
            // El parámetro antiAlias se mantiene para compatibilidad pero no se usa
            DrawLine(x1, y1, x2, y2, thickness, color);
        }

        public static void DrawCircle(Vector2 center, float radius, UIFramework.Color color, int segments)
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
                OpenGLHook.DrawFilledCircle(center, radius, apiColor, segments);
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

        public static Vector2 MeasureText(string textBeforeCaret)
        {
            return new Vector2();
        }

        public static void Shutdown()
        {
           
        }
    }
}
