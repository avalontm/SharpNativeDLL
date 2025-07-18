using System.Runtime.InteropServices;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    internal unsafe class DirectXHook
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct DXVertex
        {
            internal float X, Y;
            internal float R, G, B, A;
        }

        private static IntPtr _device;
        private static IntPtr _context;
        private static IntPtr _vertexBuffer;

        internal static void Initialize(IntPtr swapChain)
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


        internal static void DrawRect(float x, float y, float w, float h, Color color)
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

        internal static void DrawText(string text, Vector2 vector2, Color color, float v)
        {

        }

        internal static void AddRenderCallback(Action render)
        {

        }

        internal static void DrawLine(Vector2 startPoint, Vector2 endPoint, float thickness, Color apiColor)
        {
            throw new NotImplementedException();
        }

        internal static void DrawCircle(Vector2 center, float radius, int segments, float thinkness, Color apiColor)
        {
            throw new NotImplementedException();
        }

        internal static void RemoveRenderCallback(Action render)
        {
            throw new NotImplementedException();
        }
    }
}
