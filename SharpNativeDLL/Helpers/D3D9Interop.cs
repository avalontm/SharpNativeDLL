using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static SharpNativeDLL.Helpers.Structs;

namespace SharpNativeDLL.Helpers
{
    public static class D3D9Interop
    {
        public static IntPtr DEVICE = IntPtr.Zero;
        public static IntPtr FONT = IntPtr.Zero;
        public const int FONT_HEIGHT = 14;

        // Constantes de DirectX 9
        public const int D3D_SDK_VERSION = 32; // Cambiar este valor según la versión de DirectX SDK que estés utilizando

        const string D3DX9Library = "d3d9.dll";

        public const int D3DDEVTYPE_HAL = 1;
        public const int D3DPRESENT_INTERVAL_IMMEDIATE = unchecked((int)0x80000000);
        public const int D3DCREATE_HARDWARE_VERTEXPROCESSING = 0x00000040;

        [DllImport(D3DX9Library, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr Direct3DCreate9(int sdkVersion);

        [DllImport(D3DX9Library)]
        public static extern int Direct3DCreate9Ex(int sdkVersion, out IntPtr ppD3D);

        [DllImport(D3DX9Library, CallingConvention = CallingConvention.StdCall)]
        public static extern int D3DXCreateTextureFromFile(IntPtr device, string filename, out IntPtr texture);

        [DllImport(D3DX9Library, CallingConvention = CallingConvention.StdCall)]
        public static extern int D3DXSaveTextureToFile(string filename, int imageFileFormat, IntPtr texture, IntPtr palette);

        [DllImport("d3dx9_43.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
        public static extern int D3DXCreateFont(IntPtr device, int height, int width, int weight, int mipLevels, bool italic,
         int charSet, int outputPrecision, int quality, int pitchAndFamily, string faceName, out IntPtr font);

        [DllImport(D3DX9Library, CallingConvention = CallingConvention.StdCall)]
        public static extern int D3DXFont_Release(IntPtr font);

        [DllImport(D3DX9Library, CallingConvention = CallingConvention.StdCall)]
        public static extern int Direct3D9CreateDevice(IntPtr direct3D, int adapter, int deviceType, IntPtr hFocusWindow, int behaviorFlags, ref D3DPRESENT_PARAMETERS presentationParameters, out IntPtr device);

        [DllImport(D3DX9Library, CallingConvention = CallingConvention.StdCall)]
        public static extern int D3DXFont_DrawText(IntPtr font, string text, int count,  ref RECT rect, uint format, uint color);

        [DllImport("d3dx9_43.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int D3DXCreateLine(IntPtr device, out IntPtr line);

        [DllImport("d3dx9_43.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int D3DXCreateLineEx(IntPtr device, ref IntPtr line);

        [DllImport("d3dx9_43.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int D3DXLineDraw(IntPtr line, ref D3DXVECTOR2 vertexList, int vertexCount, uint color);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct D3DPRESENT_PARAMETERS
        {
            public int BackBufferWidth;
            public int BackBufferHeight;
            public int BackBufferFormat;
            public int BackBufferCount;
            public int MultiSampleType;
            public int MultiSampleQuality;
            public int SwapEffect;
            public IntPtr hDeviceWindow;
            public int Windowed;
            public int EnableAutoDepthStencil;
            public int AutoDepthStencilFormat;
            public int Flags;
            public int FullScreen_RefreshRateInHz;
            public int PresentationInterval;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D3DXVECTOR2
        {
            public float x;
            public float y;
        }
    }
}
