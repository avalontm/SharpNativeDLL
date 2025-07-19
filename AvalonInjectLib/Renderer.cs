using AvalonInjectLib.Graphics;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    /// <summary>
    /// Provides hardware-accelerated 2D rendering capabilities with support for multiple graphics APIs.
    /// Manages rendering state, transformations, and resource loading efficiently.
    /// </summary>
    public static class Renderer
    {
        // Reusable color struct to avoid allocations
        private static Structs.Color _tempColor;

        /// <summary>
        /// Supported graphics APIs
        /// </summary>
        public enum GraphicsAPI
        {
            None,
            OpenGL,
            DirectX
        }

        /// <summary>
        /// Currently active graphics API
        /// </summary>
        public static GraphicsAPI CurrentAPI { get; set; } = GraphicsAPI.OpenGL;

        /// <summary>
        /// Gets the screen width in pixels
        /// </summary>
        public static int ScreenWidth => CurrentAPI == GraphicsAPI.OpenGL ?
            OpenGLHook.ScreenWidth : 1920;

        /// <summary>
        /// Gets the screen height in pixels
        /// </summary>
        public static int ScreenHeight => CurrentAPI == GraphicsAPI.OpenGL ?
            OpenGLHook.ScreenHeight : 1080;

        /// <summary>
        /// Initializes the graphics subsystem for the specified process
        /// </summary>
        public static bool InitializeGraphics()
        {
            if (CurrentAPI == GraphicsAPI.DirectX)
            {
                DirectXHook.Initialize();
            }
            else
            {
              return OpenGLHook.Initialize();
            }

            return true;
        }

        /// <summary>
        /// Registers a callback to be invoked during the rendering phase
        /// </summary>
        /// <param name="render">Callback action</param>
        public static void SetRenderCallback(Action render)
        {
            if (CurrentAPI == GraphicsAPI.OpenGL)
                OpenGLHook.SetRenderCallback(render);
            else
                DirectXHook.AddRenderCallback(render);
        }

        /// <summary>
        /// Unregisters a rendering callback
        /// </summary>
        /// <param name="render">Callback to remove</param>
        public static void ClearRenderCallback()
        {
            if (CurrentAPI == GraphicsAPI.OpenGL)
                OpenGLHook.ClearRenderCallback();
            else
                DirectXHook.ClearRenderCallback();
        }

        /// <summary>
        /// Draws a filled rectangle
        /// </summary>
        public static void DrawRect(float x, float y, float w, float h, UIFramework.Color color)
        {
            ConvertColor(ref color, ref _tempColor);

            if (CurrentAPI == GraphicsAPI.OpenGL)
                OpenGLHook.DrawFilledBox(x, y, w, h, _tempColor);
            else
                DirectXHook.DrawRect(x, y, w, h, _tempColor);
        }

        /// <summary>
        /// Draws a filled rectangle using Rect structure
        /// </summary>
        public static void DrawRect(Rect bounds, UIFramework.Color color)
        {
            DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, color);
        }

        /// <summary>
        /// Draws an outlined rectangle with specified border thickness
        /// </summary>
        /// <param name="bounds">Rectangle dimensions</param>
        /// <param name="borderColor">Color of the border</param>
        /// <param name="borderThickness">Thickness of the border in pixels</param>
        public static void DrawRectOutline(Rect bounds, UIFramework.Color borderColor, float borderThickness)
        {
            // Convert color once and reuse
            ConvertColor(ref borderColor, ref _tempColor);

            // Calculate coordinates (reusing values to minimize calculations)
            float left = bounds.X;
            float top = bounds.Y;
            float right = bounds.X + bounds.Width;
            float bottom = bounds.Y + bounds.Height;

            switch (CurrentAPI)
            {
                case GraphicsAPI.OpenGL:
                    // Draw 4 lines to form the rectangle outline
                    OpenGLHook.DrawLine(new Vector2(left, top), new Vector2(right, top), borderThickness, _tempColor); // Top
                    OpenGLHook.DrawLine(new Vector2(right, top), new Vector2(right, bottom), borderThickness, _tempColor); // Right
                    OpenGLHook.DrawLine(new Vector2(left, bottom), new Vector2(right, bottom), borderThickness, _tempColor); // Bottom
                    OpenGLHook.DrawLine(new Vector2(left, top), new Vector2(left, bottom), borderThickness, _tempColor); // Left
                    break;

                case GraphicsAPI.DirectX:
                    // Alternative DirectX implementation
                    DirectXHook.DrawLine(new Vector2(left, top), new Vector2(right, top), borderThickness, _tempColor);
                    DirectXHook.DrawLine(new Vector2(right, top), new Vector2(right, bottom), borderThickness, _tempColor);
                    DirectXHook.DrawLine(new Vector2(left, bottom), new Vector2(right, bottom), borderThickness, _tempColor);
                    DirectXHook.DrawLine(new Vector2(left, top), new Vector2(left, bottom), borderThickness, _tempColor);
                    break;

                default:
                    throw new NotSupportedException($"API not supported: {CurrentAPI}");
            }
        }

        /// <summary>
        /// Draws text at specified position
        /// </summary>
        public static void DrawText(string text, Vector2 pos, UIFramework.Color color, Font font)
        {
            DrawText(text, pos.X, pos.Y, color, font);
        }

        /// <summary>
        /// Draws text at specified coordinates
        /// </summary>
        public static void DrawText(string text, float x, float y, UIFramework.Color color, Font font)
        {
            ConvertColor(ref color, ref _tempColor);
            var position = new Vector2(x, y); // Struct is small, stack allocation is fine

            switch (CurrentAPI)
            {
                case GraphicsAPI.OpenGL:
                    OpenGLHook.DrawText(font.GetFontId(), text, position, _tempColor, font.Size);
                    break;
                case GraphicsAPI.DirectX:
                    DirectXHook.DrawText(text, position, _tempColor, font.Size);
                    break;
                default:
                    throw new NotSupportedException($"API not supported: {CurrentAPI}");
            }
        }

        /// <summary>
        /// Draws a line between two points
        /// </summary>
        public static void DrawLine(float x1, float y1, float x2, float y2, float thickness, UIFramework.Color color)
        {
            ConvertColor(ref color, ref _tempColor);

            // Reuse Vector2 structs to minimize allocations
            var start = new Vector2(x1, y1);
            var end = new Vector2(x2, y2);

            if (CurrentAPI == GraphicsAPI.OpenGL)
                OpenGLHook.DrawLine(start, end, thickness, _tempColor);
            else
                DirectXHook.DrawLine(start, end, thickness, _tempColor);
        }

        /// <summary>
        /// Draws a filled circle
        /// </summary>
        public static void DrawCircle(Vector2 center, float radius, UIFramework.Color color)
        {
            ConvertColor(ref color, ref _tempColor);

            if (CurrentAPI == GraphicsAPI.OpenGL)
                OpenGLHook.DrawFilledCircle(center, radius, _tempColor);
        }

        /// <summary>
        /// Gets the current screen dimensions
        /// </summary>
        public static Vector2 GetScreenSize()
        {
            return CurrentAPI == GraphicsAPI.OpenGL ?
                new Vector2(OpenGLHook.ScreenWidth, OpenGLHook.ScreenHeight) :
                default;
        }

        /// <summary>
        /// Draws a filled triangle
        /// </summary>
        public static void DrawTriangle(Vector2 v1, Vector2 v2, Vector2 v3, UIFramework.Color color)
        {
            ConvertColor(ref color, ref _tempColor);
            OpenGLHook.DrawTriangle(v1, v2, v3, _tempColor);
        }

  
        // ================ Texture Management ================ //

        /// <summary>
        /// Loads a texture asynchronously
        /// </summary>
        public static Texture2D LoadTexture(string filePath)
        {
            return new Texture2D(filePath);
        }

        /// <summary>
        /// Draws a texture within the specified rectangle
        /// </summary>
        public static void DrawTexture(Texture2D texture, Rectangle rect, UIFramework.Color tintColor)
        {
            if (texture?.IsLoaded == true)
            {
                ConvertColor(ref tintColor, ref _tempColor);
                TextureRenderer.DrawTexture(texture, rect, _tempColor);
            }
        }

        // ================ Private Helpers ================ //

        /// <summary>
        /// Converts UI color to API-specific color format without allocations
        /// </summary>
        private static void ConvertColor(ref UIFramework.Color src, ref Structs.Color dest)
        {
            dest.R = src.R / 255f;
            dest.G = src.G / 255f;
            dest.B = src.B / 255f;
            dest.A = src.A / 255f;
        }
    }
}