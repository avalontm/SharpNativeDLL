using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static SharpNativeDLL.Helpers.OpenGLInterop;

namespace SharpNativeDLL.Helpers
{
    public static class OpenGLManager
    {
        static IntPtr gldc = IntPtr.Zero;
        static IntPtr glrc = IntPtr.Zero;
        static IntPtr layWnd = IntPtr.Zero;

        public static void InitializeOpenGL(IntPtr overlayer)
        {
            layWnd = overlayer;

            gldc = OpenGLInterop.GetDC(layWnd);

            if (gldc == IntPtr.Zero)
            {
                Console.WriteLine("Error al crear la instancia OpenGL.");
                return;
            }

            PIXELFORMATDESCRIPTOR pfd = new PIXELFORMATDESCRIPTOR();
            pfd.nSize = (ushort)Marshal.SizeOf(pfd);
            pfd.nVersion = 1;
            pfd.dwFlags = 0x00000001 | 0x00000004 | 0x00000020; // PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER
            pfd.iPixelType = 0;
            pfd.cColorBits = 32; // 32 bits de color (RGBA)
            pfd.cDepthBits = 24; // 24 bits de profundidad de color
            pfd.iLayerType = 0;

            int pixelFormat = ChoosePixelFormat(gldc, ref pfd);

            if (pixelFormat == 0)
            {
                throw new Exception("Error al seleccionar el formato de píxel.");
            }

            if (!SetPixelFormat(gldc, pixelFormat, ref pfd))
            {
                throw new Exception("Error al establecer el formato de píxel.");
            }

            glrc = OpenGLInterop.wglCreateContext(gldc);

            if (glrc == IntPtr.Zero)
            {
                Console.WriteLine("Error al crear el contexto OpenGL.");
                return;
            }

            if (!OpenGLInterop.wglMakeCurrent(gldc, glrc))
            {
                Console.WriteLine("Error al establecer el contexto OpenGL.");
                return;
            }

            GLClear();

            DrawTriangle();

            SwapBuffers();
        }

        static void GLClear()
        {
            OpenGLInterop.glClearColor(1.0f, 0.0f, 1.0f, 1.0f);
            OpenGLInterop.glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
        }

        static void SwapBuffers()
        {
            gldc = OpenGLInterop.GetDC(layWnd);
            OpenGLInterop.wglSwapBuffers(gldc);
            OpenGLInterop.ReleaseDC(layWnd, gldc);
        }



        // Método para dibujar un triángulo utilizando OpenGL
        static void DrawTriangle()
        {
            glBegin(OpenGLInterop.GL_TRIANGLES);
            glColor3f(1.0f, 0.0f, 0.0f); // Color rojo
            glVertex2f(0.0f, 0.5f);
            glColor3f(0.0f, 1.0f, 0.0f); // Color verde
            glVertex2f(-0.5f, -0.5f);
            glColor3f(0.0f, 0.0f, 1.0f); // Color azul
            glVertex2f(0.5f, -0.5f);
            glEnd();
        }

    }
}
