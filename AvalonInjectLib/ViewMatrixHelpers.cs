using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    // Clase helper para trabajar con las matrices
    public static class MatrixHelper
    {
        // Obtener ViewMatrix directamente de OpenGL
        public static ViewMatrix GetCurrentViewMatrix()
        {
            float[] matrix = new float[16];
            OpenGLInterop.glGetFloatv(OpenGLInterop.GL_MODELVIEW_MATRIX, matrix);
            return ViewMatrix.FromOpenGLArray(matrix);
        }

        // Obtener matriz de proyección
        public static ViewMatrix GetCurrentProjectionMatrix()
        {
            float[] matrix = new float[16];
            OpenGLInterop.glGetFloatv(OpenGLInterop.GL_PROJECTION_MATRIX, matrix);
            return ViewMatrix.FromOpenGLArray(matrix);
        }

        // Cargar una ViewMatrix en OpenGL
        public static void LoadViewMatrix(ViewMatrix viewMatrix)
        {
            OpenGLInterop.glMatrixMode(OpenGLInterop.GL_MODELVIEW);
            OpenGLInterop.glLoadMatrixf(viewMatrix.ToOpenGLArray());
        }

        // Multiplicar la matriz actual con una ViewMatrix
        public static void MultiplyViewMatrix(ViewMatrix viewMatrix)
        {
            OpenGLInterop.glMatrixMode(OpenGLInterop.GL_MODELVIEW);
            OpenGLInterop.glMultMatrixf(viewMatrix.ToOpenGLArray());
        }
    }
}
