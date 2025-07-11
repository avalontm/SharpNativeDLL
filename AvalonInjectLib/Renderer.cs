using System.Runtime.InteropServices;
using static AvalonInjectLib.Structs;
using static System.Net.Mime.MediaTypeNames;

namespace AvalonInjectLib
{
    public static class Renderer
    {
        public enum GraphicsAPI { OpenGL, DirectX11 }
        public static GraphicsAPI CurrentAPI = GraphicsAPI.OpenGL;

        public static void InitializeGraphics(uint processId, IntPtr swapChain = default)
        {
            if (CurrentAPI == GraphicsAPI.DirectX11 && swapChain != IntPtr.Zero)
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
                OpenGLHook.SetRenderCallback(render);
            }
            else
            {
                DirectXHook.SetRenderCallback(render);
            }

        }

        public static void DrawRect(float x, float y, float w, float h, UIFramework.Color color)
        {
            if (CurrentAPI == GraphicsAPI.OpenGL)
            {
                OpenGLHook.DrawFilledBox(x, y, w, h, new Structs.Color() { A = color.A, R = color.R, G = color.G, B = color.B });
            }
            else
            {
                DirectXHook.DrawRect(x, y, w, h, color);
            }
        }

        public static void DrawText(string text, float x, float y, UIFramework.Color color, float size = 12f)
        {
            if (CurrentAPI == GraphicsAPI.OpenGL)
            {
                OpenGLHook.DrawText(text, new Vector2(x, y), new Structs.Color() { A = color.A, R = color.R, G = color.G, B = color.B }, 1f);
            }
            else
            {
                DirectXHook.DrawText(text, new Vector2(x, y), new Structs.Color() { A = color.A, R = color.R, G = color.G, B = color.B }, 1f);
            }
        }

        public static Vector2 MeasureText(string text, float size)
        {
            return new Vector2();
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
    }
}
