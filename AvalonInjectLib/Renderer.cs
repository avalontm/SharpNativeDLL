
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
            var (X, Y) = FontRenderer.MeasureText(text, FontRenderer.GetScaleForDesiredSize(size));
            return new Vector2(X, Y);
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

        internal static void DrawTriangle(float v1, float v2, float v3, float v4, float v5, float v6, UIFramework.Color color)
        {
            
        }

        internal static void DrawTriangle(Vector2 vector21, Vector2 vector22, Vector2 vector23, UIFramework.Color arrowColor)
        {
          
        }

        internal static void PushClip(Rect contentRect)
        {
          
        }

        internal static void PopClip()
        {
          
        }

        internal static void DrawBorder(float x, float y, float width, float height, float borderThickness, UIFramework.Color borderColor)
        {
          
        }

        internal static Vector2 TransformPoint(float x, float y)
        {
            Vector4 point = new Vector4(x, y, 0, 1);
            Vector4 transformed = Vector4.Transform(point, _currentTransform);
            return new Vector2(transformed.X, transformed.Y);
        }

        internal static void PushTransform(float x, float y)
        {
            _transformStack.Push(_currentTransform);
            _currentTransform *= ViewMatrix.CreateTranslation(x, y, 0);
        }

        internal static void PopTransform()
        {
            if (_transformStack.Count > 0)
            {
                _currentTransform = _transformStack.Pop();
            }
        }

        internal static void DrawRectOutline(Rect bounds, UIFramework.Color borderColor, float borderThickness)
        {
           
        }
    }
}
