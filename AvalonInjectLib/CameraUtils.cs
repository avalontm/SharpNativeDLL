using System;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static class CameraUtils
    {
        public static bool WorldToScreen(Vector3 world, out Vector2 screen, ViewMatrix viewMatrix, int width, int height)
        {
            screen = new Vector2();

            // Transformar coordenadas mundiales a coordenadas de clip
            float clipX = world.X * viewMatrix.M11 + world.Y * viewMatrix.M21 + world.Z * viewMatrix.M31 + viewMatrix.M41;
            float clipY = world.X * viewMatrix.M12 + world.Y * viewMatrix.M22 + world.Z * viewMatrix.M32 + viewMatrix.M42;
            float clipW = world.X * viewMatrix.M14 + world.Y * viewMatrix.M24 + world.Z * viewMatrix.M34 + viewMatrix.M44;

            // Verificar si el punto está detrás de la cámara
            if (clipW < 0.001f)
                return false;

            // Convertir a coordenadas de dispositivo normalizado (NDC)
            float ndcX = clipX / clipW;
            float ndcY = clipY / clipW;

            // Convertir NDC a coordenadas de pantalla
            screen.X = (width / 2.0f) + (ndcX * width / 2.0f);
            screen.Y = (height / 2.0f) - (ndcY * height / 2.0f);

            return true;
        }


        /// <summary>
        /// Multiplica una matriz 4x4 por un vector 4D
        /// </summary>
        private static Vector4 MultiplyMatrixVector(ViewMatrix matrix, Vector4 vector)
        {
            return new Vector4(
                matrix.M11 * vector.X + matrix.M21 * vector.Y + matrix.M31 * vector.Z + matrix.M41 * vector.W,
                matrix.M12 * vector.X + matrix.M22 * vector.Y + matrix.M32 * vector.Z + matrix.M42 * vector.W,
                matrix.M13 * vector.X + matrix.M23 * vector.Y + matrix.M33 * vector.Z + matrix.M43 * vector.W,
                matrix.M14 * vector.X + matrix.M24 * vector.Y + matrix.M34 * vector.Z + matrix.M44 * vector.W
            );
        }


        // ==================== FUNCIONES AUXILIARES ====================

            // Crea una matriz de rotación (pitch, yaw, roll en grados)
        public static ViewMatrix CreateRotationMatrix(float pitchDegrees, float yawDegrees, float rollDegrees)
        {
            float pitch = pitchDegrees * (MathF.PI / 180f);
            float yaw = yawDegrees * (MathF.PI / 180f);
            float roll = rollDegrees * (MathF.PI / 180f);

            float cosPitch = MathF.Cos(pitch);
            float sinPitch = MathF.Sin(pitch);
            float cosYaw = MathF.Cos(yaw);
            float sinYaw = MathF.Sin(yaw);
            float cosRoll = MathF.Cos(roll);
            float sinRoll = MathF.Sin(roll);

            ViewMatrix matrix = new ViewMatrix();

            // Configuración de la matriz de rotación
            matrix.M11 = cosYaw * cosRoll + sinYaw * sinPitch * sinRoll;
            matrix.M12 = cosPitch * sinRoll;
            matrix.M13 = sinYaw * cosRoll - cosYaw * sinPitch * sinRoll;
            matrix.M14 = 0;

            matrix.M21 = -sinYaw * cosPitch;
            matrix.M22 = cosPitch * cosRoll;
            matrix.M23 = cosYaw * cosPitch;
            matrix.M24 = 0;

            matrix.M31 = sinYaw * sinRoll - cosYaw * sinPitch * cosRoll;
            matrix.M32 = -sinPitch;
            matrix.M33 = cosYaw * cosRoll;
            matrix.M34 = 0;

            // Sin traslación (dejamos M41..M44 en 0)
            matrix.M41 = matrix.M42 = matrix.M43 = 0;
            matrix.M44 = 1;

            return matrix;
        }

        // Multiplica dos matrices 4x4
        public static ViewMatrix MultiplyMatrices(ViewMatrix a, ViewMatrix b)
        {
            ViewMatrix result = new ViewMatrix();

            result.M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41;
            result.M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42;
            result.M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43;
            result.M14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44;

            result.M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41;
            result.M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42;
            result.M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43;
            result.M24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44;

            result.M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41;
            result.M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42;
            result.M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43;
            result.M34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44;

            result.M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41;
            result.M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42;
            result.M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43;
            result.M44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44;

            return result;
        }
    }
}
