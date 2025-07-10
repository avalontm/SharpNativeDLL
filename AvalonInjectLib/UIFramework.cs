using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{

    public static class UIFramework
    {
        // ================= ESTRUCTURAS BASE =================
        public struct Rect
        {
            public float X, Y, Width, Height;
            public bool Contains(float px, float py) =>
                px >= X && px <= X + Width && py >= Y && py <= Y + Height;
        }

        public struct Color
        {
            public float R, G, B, A;
            public static readonly Color White = new(1, 1, 1, 1);
            public static readonly Color Red = new(1, 0, 0, 1);

            public Color(float r, float g, float b, float a)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }
        }

        // ================= CONTROL BASE =================
        public abstract class UIControl
        {
            public Rect Bounds;
            public Color BackgroundColor;
            public bool IsVisible = true;
            public bool IsHovered;

            public abstract void Draw();
            public virtual void Update(float mouseX, float mouseY, bool mouseDown)
            {
                IsHovered = Bounds.Contains(mouseX, mouseY);
            }
        }

        // ================= CONTROLES IMPLEMENTADOS =================
        public class Button : UIControl
        {
            public string Text;
            public Action OnClick;
            public Color TextColor = Color.White;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Fondo
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height,
                    IsHovered ? new Color(0.4f, 0.4f, 0.4f, 1) : BackgroundColor);

                // Texto (centrado)
                float textWidth = Text.Length * 8; // Aproximación
                Renderer.DrawText(Text,
                    Bounds.X + (Bounds.Width - textWidth) / 2,
                    Bounds.Y + Bounds.Height / 3,
                    TextColor);
            }

            public override void Update(float mouseX, float mouseY, bool mouseDown)
            {
                base.Update(mouseX, mouseY, mouseDown);
                if (IsHovered && mouseDown) OnClick?.Invoke();
            }
        }

        public class Checkbox : UIControl
        {
            public string Label;
            public bool IsChecked;
            public Action<bool> OnValueChanged;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Caja
                Renderer.DrawRect(Bounds.X, Bounds.Y, 15, 15,
                    IsChecked ? new Color(0, 1, 0, 1) : new Color(0.3f, 0.3f, 0.3f, 1));

                // Texto
                Renderer.DrawText(Label, Bounds.X + 20, Bounds.Y, Color.White);
            }

            public override void Update(float mouseX, float mouseY, bool mouseDown)
            {
                base.Update(mouseX, mouseY, mouseDown);
                if (IsHovered && mouseDown)
                {
                    IsChecked = !IsChecked;
                    OnValueChanged?.Invoke(IsChecked);
                }
            }
        }

        public class Slider : UIControl
        {
            public string Label;
            public float Min, Max, Value;
            private bool _isDragging;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Barra
                Renderer.DrawRect(Bounds.X, Bounds.Y + 10, Bounds.Width, 5, new Color(0.3f, 0.3f, 0.3f, 1));

                // Indicador
                float pos = Bounds.X + (Value - Min) / (Max - Min) * Bounds.Width;
                Renderer.DrawRect(pos - 5, Bounds.Y, 10, 20, Color.Red);

                // Texto
                Renderer.DrawText($"{Label}: {Value:F1}", Bounds.X, Bounds.Y + 25, Color.White);
            }

            public override void Update(float mouseX, float mouseY, bool mouseDown)
            {
                base.Update(mouseX, mouseY, mouseDown);

                if (mouseDown && IsHovered) _isDragging = true;
                if (!mouseDown) _isDragging = false;

                if (_isDragging)
                {
                    Value = Math.Clamp(Min + (mouseX - Bounds.X) / Bounds.Width * (Max - Min), Min, Max);
                }
            }
        }

        public class Window : UIControl
        {
            public string Title;
            public UIControl[] Controls;

            public override void Draw()
            {
                if (!IsVisible) return;

                // Fondo
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, BackgroundColor);

                // Barra de título
                Renderer.DrawRect(Bounds.X, Bounds.Y, Bounds.Width, 25, new Color(0.2f, 0.4f, 0.8f, 1));
                Renderer.DrawText(Title, Bounds.X + 10, Bounds.Y + 5, Color.White);

                // Controles
                foreach (var control in Controls) control.Draw();
            }

            public override void Update(float mouseX, float mouseY, bool mouseDown)
            {
                if (!IsVisible) return;

                // Convertir a coordenadas relativas a la ventana
                float relX = mouseX - Bounds.X;
                float relY = mouseY - Bounds.Y;

                foreach (var control in Controls)
                {
                    control.Update(relX, relY, mouseDown);
                }
            }
        }

        // ================= SISTEMA DE RENDERIZADO =================
        public static class Renderer
        {
            public enum GraphicsAPI { OpenGL, DirectX11 }
            public static GraphicsAPI CurrentAPI = GraphicsAPI.OpenGL;


            // ============ DIRECTX 11 IMPLEMENTATION ============
            private unsafe static class DirectX
            {
                [StructLayout(LayoutKind.Sequential)]
                private struct DXVertex
                {
                    public float X, Y;
                    public float R, G, B, A;
                }

                private static IntPtr _device;
                private static IntPtr _context;
                private static IntPtr _vertexBuffer;

                public static void Initialize(IntPtr swapChain)
                {
                    // Obtener device y context desde el swapchain
                    var vtbl = Marshal.ReadIntPtr(swapChain);
                    var getDevice = Marshal.GetDelegateForFunctionPointer<GetDeviceDelegate>(Marshal.ReadIntPtr(vtbl, 8 * sizeof(IntPtr))); // VTBL[8] es GetDevice

                    getDevice(swapChain, out _device);

                    // Crear vertex buffer
                    CreateVertexBuffer();
                }

                [UnmanagedFunctionPointer(CallingConvention.StdCall)]
                private delegate int GetDeviceDelegate(IntPtr swapChain, out IntPtr device);

                private static void CreateVertexBuffer()
                {
                    var bufferDesc = new D3D11_BUFFER_DESC
                    {
                        ByteWidth = (uint)(4 * Marshal.SizeOf<DXVertex>()),
                        Usage = 1, // D3D11_USAGE_DYNAMIC
                        BindFlags = 1, // D3D11_BIND_VERTEX_BUFFER
                        CPUAccessFlags = 0x10000 // D3D11_CPU_ACCESS_WRITE
                    };

                    var createBuffer = Marshal.GetDelegateForFunctionPointer<CreateBufferDelegate>(
                        Marshal.ReadIntPtr(Marshal.ReadIntPtr(_device), 12 * sizeof(IntPtr))); // VTBL[12]

                    createBuffer(_device, ref bufferDesc, IntPtr.Zero, out _vertexBuffer);
                }


                public static void DrawRect(float x, float y, float w, float h, Color color)
                {
                    var vertices = new DXVertex[4]
                    {
                       new() { X = x, Y = y, R = color.R, G = color.G, B = color.B, A = color.A },
                       new() { X = x + w, Y = y, R = color.R, G = color.G, B = color.B, A = color.A },
                       new() { X = x, Y = y + h, R = color.R, G = color.G, B = color.B, A = color.A },
                       new() { X = x + w, Y = y + h, R = color.R, G = color.G, B = color.B, A = color.A }
                    };

                    // Convert DXVertex[] to byte[] using a memory stream and binary writer
                    int vertexSize = Marshal.SizeOf<DXVertex>();
                    byte[] vertexData = new byte[vertices.Length * vertexSize];
                    IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(vertices, 0);
                    Marshal.Copy(ptr, vertexData, 0, vertexData.Length);

                    // Map the vertex buffer
                    var map = Marshal.GetDelegateForFunctionPointer<MapDelegate>(
                        Marshal.ReadIntPtr(Marshal.ReadIntPtr(_context), 14 * sizeof(IntPtr))); // VTBL[14]

                    map(_context, _vertexBuffer, 0, 1, 0x10000, out var mappedResource); // D3D11_MAP_WRITE_DISCARD

                    // Copy data
                    Marshal.Copy(vertexData, 0, mappedResource.pData, vertexData.Length);

                    // Unmap
                    var unmap = Marshal.GetDelegateForFunctionPointer<UnmapDelegate>(
                        Marshal.ReadIntPtr(Marshal.ReadIntPtr(_context), 15 * sizeof(IntPtr)));
                    unmap(_context, _vertexBuffer, 0);

                    // Draw
                    var draw = Marshal.GetDelegateForFunctionPointer<DrawDelegate>(
                        Marshal.ReadIntPtr(Marshal.ReadIntPtr(_context), 20 * sizeof(IntPtr)));
                    draw(_context, 4, 0);
                }

            }

            // ============ INTERFAZ UNIFICADA ============
            public static void DrawRect(float x, float y, float w, float h, Color color)
            {
                if (CurrentAPI == GraphicsAPI.OpenGL)
                {
                    OpenGLHook.DrawBox(x, y, w, h, 2, new Structs.Color() { A = color.A, R = color.R , G = color.G, B = color.B });
                }
                else
                {
                    DirectX.DrawRect(x, y, w, h, color);
                }
            }

            public static void InitializeGraphics(uint processId, IntPtr swapChain = default)
            {
                if (CurrentAPI == GraphicsAPI.DirectX11 && swapChain != IntPtr.Zero)
                {
                    DirectX.Initialize(swapChain);
                    return;
                }

                OpenGLHook.Initialize(processId);
  
            }

            public static void DrawText(string text, float x, float y, Color color)
            {
              
            }
        }

        // ================= EJEMPLO DE USO =================
        public static class MenuSystem
        {
            private static Window _mainWindow;
            private static float _mouseX, _mouseY;
            private static bool _mouseDown;

            public static void Initialize()
            {
                _mainWindow = new Window
                {
                    IsVisible = true,
                    Bounds = new Rect { X = 50, Y = 50, Width = 300, Height = 200 },
                    Title = "HACK MENU",
                    BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f),
                    Controls = new UIControl[]
                    {
                    new Button
                    {
                        Bounds = new Rect { X = 20, Y = 40, Width = 120, Height = 30 },
                        Text = "God Mode",
                        OnClick = ToggleGodMode
                    },
                    new Slider
                    {
                        Bounds = new Rect { X = 20, Y = 80, Width = 150, Height = 40 },
                        Label = "Velocidad",
                        Min = 1,
                        Max = 10,
                        Value = 5
                    }
                    }
                };

                OpenGLHook.SetRenderCallback(Render);
            }

            public static void HandleInput(nint hWnd, uint msg, nint wParam, nint lParam)
            {
                switch (msg)
                {
                    case 0x0201: // WM_LBUTTONDOWN
                        _mouseDown = true;
                        UpdateMousePos(lParam);
                        break;

                    case 0x0202: // WM_LBUTTONUP
                        _mouseDown = false;
                        break;

                    case 0x0200: // WM_MOUSEMOVE
                        UpdateMousePos(lParam);
                        break;

                    case 0x0100 when wParam.ToInt32() == 0x76: // VK_F7
                        _mainWindow.IsVisible = !_mainWindow.IsVisible;
                        break;
                }
            }

            private static void UpdateMousePos(nint lParam)
            {
                _mouseX = (short)(lParam.ToInt32() & 0xFFFF);
                _mouseY = (short)(lParam.ToInt32() >> 16 & 0xFFFF);
            }

            public static void Render()
            {
                _mainWindow.Update(_mouseX, _mouseY, _mouseDown);
                _mainWindow.Draw();
            }

            private static void ToggleGodMode()
            {
              
            }
        }
    }
}
