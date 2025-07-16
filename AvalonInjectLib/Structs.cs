using AvalonInjectLib.UIFramework;
using System.Numerics;
using System.Runtime.InteropServices;

namespace AvalonInjectLib
{
    public static class Structs
    {
        public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [Flags]
        public enum MouseButtons
        {
            None = 0x00,
            Left = 0x01,
            Right = 0x02,
            Middle = 0x04,
            XButton1 = 0x05,
            XButton2 = 0x06,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X;
            public int Y;
        }

        // Estructura ViewMatrix
        [StructLayout(LayoutKind.Sequential)]
        public struct ViewMatrix
        {
            // Column-major order (compatible con OpenGL/DirectX)
            public float M11, M12, M13, M14; // Columna 1
            public float M21, M22, M23, M24; // Columna 2
            public float M31, M32, M33, M34; // Columna 3
            public float M41, M42, M43, M44; // Columna 4

            /// <summary>
            /// Crea una ViewMatrix desde un array de OpenGL (column-major de 16 elementos)
            /// </summary>
            /// <param name="openGLMatrix">Array de 16 floats en formato column-major de OpenGL</param>
            /// <returns>ViewMatrix con los datos del array</returns>
            public static ViewMatrix FromOpenGLArray(float[] openGLMatrix)
            {
                if (openGLMatrix == null)
                    throw new ArgumentNullException(nameof(openGLMatrix));

                if (openGLMatrix.Length != 16)
                    throw new ArgumentException("El array debe tener exactamente 16 elementos", nameof(openGLMatrix));

                // Como ambos usan column-major, podemos copiar directamente
                return new ViewMatrix
                {
                    // Columna 1 (elementos 0-3)
                    M11 = openGLMatrix[0],
                    M12 = openGLMatrix[1],
                    M13 = openGLMatrix[2],
                    M14 = openGLMatrix[3],

                    // Columna 2 (elementos 4-7)
                    M21 = openGLMatrix[4],
                    M22 = openGLMatrix[5],
                    M23 = openGLMatrix[6],
                    M24 = openGLMatrix[7],

                    // Columna 3 (elementos 8-11)
                    M31 = openGLMatrix[8],
                    M32 = openGLMatrix[9],
                    M33 = openGLMatrix[10],
                    M34 = openGLMatrix[11],

                    // Columna 4 (elementos 12-15)
                    M41 = openGLMatrix[12],
                    M42 = openGLMatrix[13],
                    M43 = openGLMatrix[14],
                    M44 = openGLMatrix[15]
                };
            }

            /// <summary>
            /// Convierte la ViewMatrix a un array compatible con OpenGL
            /// </summary>
            /// <returns>Array de 16 floats en formato column-major</returns>
            public float[] ToOpenGLArray()
            {
                return new float[]
                {
            // Columna 1
            M11, M12, M13, M14,
            // Columna 2
            M21, M22, M23, M24,
            // Columna 3
            M31, M32, M33, M34,
            // Columna 4
            M41, M42, M43, M44
                };
            }

            /// <summary>
            /// Crea una matriz identidad
            /// </summary>
            public static ViewMatrix Identity
            {
                get
                {
                    return new ViewMatrix
                    {
                        M11 = 1.0f,
                        M12 = 0.0f,
                        M13 = 0.0f,
                        M14 = 0.0f,
                        M21 = 0.0f,
                        M22 = 1.0f,
                        M23 = 0.0f,
                        M24 = 0.0f,
                        M31 = 0.0f,
                        M32 = 0.0f,
                        M33 = 1.0f,
                        M34 = 0.0f,
                        M41 = 0.0f,
                        M42 = 0.0f,
                        M43 = 0.0f,
                        M44 = 1.0f
                    };
                }
            }

            /// <summary>
            /// Obtiene el elemento en la posición especificada (1-indexado)
            /// </summary>
            public float this[int row, int column]
            {
                get
                {
                    switch (row)
                    {
                        case 1:
                            switch (column)
                            {
                                case 1: return M11;
                                case 2: return M21;
                                case 3: return M31;
                                case 4: return M41;
                            }
                            break;
                        case 2:
                            switch (column)
                            {
                                case 1: return M12;
                                case 2: return M22;
                                case 3: return M32;
                                case 4: return M42;
                            }
                            break;
                        case 3:
                            switch (column)
                            {
                                case 1: return M13;
                                case 2: return M23;
                                case 3: return M33;
                                case 4: return M43;
                            }
                            break;
                        case 4:
                            switch (column)
                            {
                                case 1: return M14;
                                case 2: return M24;
                                case 3: return M34;
                                case 4: return M44;
                            }
                            break;
                    }
                    throw new ArgumentOutOfRangeException("Row y Column deben estar entre 1 y 4");
                }
            }

            public override string ToString()
            {
                return string.Format("\n" +
                    "[{0,10:F4} {1,10:F4} {2,10:F4} {3,10:F4}]\n" +
                    "[{4,10:F4} {5,10:F4} {6,10:F4} {7,10:F4}]\n" +
                    "[{8,10:F4} {9,10:F4} {10,10:F4} {11,10:F4}]\n" +
                    "[{12,10:F4} {13,10:F4} {14,10:F4} {15,10:F4}]",
                    M11, M12, M13, M14,
                    M21, M22, M23, M24,
                    M31, M32, M33, M34,
                    M41, M42, M43, M44);
            }

            /// <summary>
            /// Creates a translation matrix
            /// </summary>
            /// <param name="x">X translation</param>
            /// <param name="y">Y translation</param>
            /// <param name="z">Z translation (default 0 for 2D)</param>
            /// <returns>Translation matrix</returns>
            public static ViewMatrix CreateTranslation(float x, float y, float z = 0f)
            {
                return new ViewMatrix
                {
                    M11 = 1.0f,
                    M12 = 0.0f,
                    M13 = 0.0f,
                    M14 = 0.0f,
                    M21 = 0.0f,
                    M22 = 1.0f,
                    M23 = 0.0f,
                    M24 = 0.0f,
                    M31 = 0.0f,
                    M32 = 0.0f,
                    M33 = 1.0f,
                    M34 = 0.0f,
                    M41 = x,
                    M42 = y,
                    M43 = z,
                    M44 = 1.0f
                };
            }

            /// <summary>
            /// Multiplies two matrices (this * other)
            /// </summary>
            public static ViewMatrix operator *(ViewMatrix a, ViewMatrix b)
            {
                return new ViewMatrix
                {
                    M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41,
                    M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42,
                    M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43,
                    M14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,

                    M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41,
                    M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42,
                    M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43,
                    M24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,

                    M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41,
                    M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42,
                    M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43,
                    M34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,

                    M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41,
                    M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42,
                    M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43,
                    M44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44
                };
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Vector2 : IEquatable<Vector2>
        {
            public float X;
            public float Y;

            public Vector2(float x, float y)
            {
                this.X = x;
                this.Y = y;
            }

            public Vector2(float value)
            {
                this.X = value;
                this.Y = value;
            }

            public static Vector2 Zero => new Vector2(0f);
            public static Vector2 One => new Vector2(1f);
            public static Vector2 UnitX => new Vector2(1f, 0f);
            public static Vector2 UnitY => new Vector2(0f, 1f);
            public static Vector2 PositiveInfinity => new Vector2(float.PositiveInfinity);
            public static Vector2 NegativeInfinity => new Vector2(float.NegativeInfinity);

            public bool IsInfinity => float.IsInfinity(X) || float.IsInfinity(Y);
            public bool IsPositiveInfinity => float.IsPositiveInfinity(X) && float.IsPositiveInfinity(Y);
            public bool IsNegativeInfinity => float.IsNegativeInfinity(X) && float.IsNegativeInfinity(Y);
            public bool IsFinite => !float.IsInfinity(X) && !float.IsInfinity(Y);

            public override string ToString() => $"({X:F2}, {Y:F2})";

            public override bool Equals(object obj) => obj is Vector2 other && Equals(other);

            public bool Equals(Vector2 other) => X == other.X && Y == other.Y;

            public override int GetHashCode() => HashCode.Combine(X, Y);

            public static bool operator ==(Vector2 left, Vector2 right) => left.Equals(right);
            public static bool operator !=(Vector2 left, Vector2 right) => !left.Equals(right);

            public static Vector2 operator +(Vector2 left, Vector2 right) =>
                new Vector2(left.X + right.X, left.Y + right.Y);

            public static Vector2 operator -(Vector2 left, Vector2 right) =>
                new Vector2(left.X - right.X, left.Y - right.Y);

            public static Vector2 operator *(Vector2 left, Vector2 right) =>
                new Vector2(left.X * right.X, left.Y * right.Y);

            public static Vector2 operator *(Vector2 left, float right) =>
                new Vector2(left.X * right, left.Y * right);

            public static Vector2 operator /(Vector2 left, Vector2 right) =>
                new Vector2(left.X / right.X, left.Y / right.Y);

            public static Vector2 operator /(Vector2 left, float right) =>
                new Vector2(left.X / right, left.Y / right);

            public static Vector2 operator -(Vector2 value) =>
                new Vector2(-value.X, -value.Y);

            // Métodos útiles para UI
            public Vector2 Max(Vector2 other) =>
                new Vector2(Math.Max(X, other.X), Math.Max(Y, other.Y));

            public Vector2 Min(Vector2 other) =>
                new Vector2(Math.Min(X, other.X), Math.Min(Y, other.Y));

            public Vector2 Clamp(Vector2 min, Vector2 max) =>
                new Vector2(
                    Math.Clamp(X, min.X, max.X),
                    Math.Clamp(Y, min.Y, max.Y)
                );

            public Vector2 Abs() =>
                new Vector2(Math.Abs(X), Math.Abs(Y));

            public float Length() =>
                (float)Math.Sqrt(X * X + Y * Y);

            public float LengthSquared() =>
                X * X + Y * Y;

            // Método específico para tu caso de uso en Measure()
            public Vector2 SubtractMargins(Thickness margin) =>
                new Vector2(
                    Math.Max(0, X - margin.Left - margin.Right),
                    Math.Max(0, Y - margin.Top - margin.Bottom)
                );
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public float X {set;get;}
            public float Y {set;get;}
            public float Width {set;get;}
            public float Height {set;get;}

            public bool Contains(float px, float py) =>
                px >= X && px <= X + Width && py >= Y && py <= Y + Height;

            public Rect(float x, float y, float w, float h)
            {
                X = x; Y = y; Width = w; Height = h;
            }

            public Rect(Vector2 position, Vector2 size)
            {
                X = position.X; Y = position.Y;
                Width = size.X; Height = size.Y;
            }

            public Vector2 Position => new Vector2(X, Y);
            public Vector2 Size => new Vector2(Width, Height);
            public Vector2 Center => new Vector2(X + Width / 2, Y + Height / 2);

            public static readonly Rect Empty = new Rect(0, 0, 0, 0);

            /// <summary>
            /// Resta un Thickness de este Vector2, restando los márgenes izquierdo/derecho de X
            /// y los márgenes superior/inferior de Y. Los valores resultantes no serán menores que 0.
            /// </summary>
            /// <param name="thickness">El Thickness a restar</param>
            /// <returns>Nuevo Vector2 con los valores restados</returns>
            public Vector2 Subtract(Thickness thickness)
            {
                return new Vector2(
                    Math.Max(0, X - thickness.Left - thickness.Right),
                    Math.Max(0, Y - thickness.Top - thickness.Bottom)
                );
            }

            /// <summary>
            /// Aplica márgenes al rectángulo, reduciendo su tamaño y ajustando su posición
            /// </summary>
            /// <param name="margin">Thickness con los márgenes a aplicar</param>
            /// <returns>Nuevo Rect con los márgenes aplicados</returns>
            public Rect ApplyMargin(Thickness margin)
            {
                float newX = X + margin.Left;
                float newY = Y + margin.Top;
                float newWidth = Math.Max(0, Width - margin.Left - margin.Right);
                float newHeight = Math.Max(0, Height - margin.Top - margin.Bottom);

                return new Rect(newX, newY, newWidth, newHeight);
            }

            public override string ToString()
            {
                return $"[{X}, {Y}, {Width}, {Height}]";
            }

            internal bool Contains(Vector2 mousePos)
            {
                return this.Contains(mousePos.X, mousePos.Y);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GlyphData
        {
            public float Advance;
            public float BearingX;
            public float BearingY;
            public float Width;
            public float Height;
            public Rect TexCoords;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Vector3
        {
            public float X;
            public float Y;
            public float Z;

            public Vector3(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            // Operaciones vectoriales básicas
            public static Vector3 operator -(Vector3 a, Vector3 b)
            {
                return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
            }

            public static Vector3 operator +(Vector3 a, Vector3 b)
            {
                return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
            }

            public static Vector3 operator *(Vector3 a, float scalar)
            {
                return new Vector3(a.X * scalar, a.Y * scalar, a.Z * scalar);
            }

            // Métodos de utilidad
            public static float Distance(Vector3 a, Vector3 b)
            {
                float dx = a.X - b.X;
                float dy = a.Y - b.Y;
                float dz = a.Z - b.Z;
                return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }

            public float DistanceTo(Vector3 other)
            {
                return Distance(this, other);
            }

            public static Vector3 SetOrigin(Vector3 currentPosition, Vector3 targetPosition)
            {
                return targetPosition - currentPosition;
            }

            // Nuevo: Normalización del vector
            public Vector3 Normalized()
            {
                float length = (float)Math.Sqrt(X * X + Y * Y + Z * Z);
                if (length > 0)
                    return new Vector3(X / length, Y / length, Z / length);
                return new Vector3(0, 0, 0);
            }

            // Sobreescritura de ToString para mejor visualización
            public override string ToString()
            {
                return $"({X:F2}, {Y:F2}, {Z:F2})";
            }
        }


        // Estructura Vector4
        [StructLayout(LayoutKind.Sequential)]
        public struct Vector4
        {
            public float X;
            public float Y;
            public float Z;
            public float W;

            public Vector4(float x, float y, float z, float w)
            {
                this.X = x;
                this.Y = y;
                this.Z = z;
                this.W = w;
            }

            // Constructor desde Vector3
            public Vector4(Vector3 vector3, float w)
            {
                this.X = vector3.X;
                this.Y = vector3.Y;
                this.Z = vector3.Z;
                this.W = w;
            }

            // Método Transform para ViewMatrix (column-major order)
            public static Vector4 Transform(Vector4 vector, ViewMatrix matrix)
            {
                return new Vector4(
                    vector.X * matrix.M11 + vector.Y * matrix.M12 + vector.Z * matrix.M13 + vector.W * matrix.M14,
                    vector.X * matrix.M21 + vector.Y * matrix.M22 + vector.Z * matrix.M23 + vector.W * matrix.M24,
                    vector.X * matrix.M31 + vector.Y * matrix.M32 + vector.Z * matrix.M33 + vector.W * matrix.M34,
                    vector.X * matrix.M41 + vector.Y * matrix.M42 + vector.Z * matrix.M43 + vector.W * matrix.M44
                );
            }

            // Propiedades útiles
            public static Vector4 Zero => new Vector4(0, 0, 0, 0);
            public static Vector4 One => new Vector4(1, 1, 1, 1);

            // Conversión a Vector3 (dividiendo por W si es necesario)
            public Vector3 ToVector3()
            {
                if (W != 0)
                {
                    return new Vector3(X / W, Y / W, Z / W);
                }
                return new Vector3(X, Y, Z);
            }

            // ToString para debug
            public override string ToString()
            {
                return $"({X}, {Y}, {Z}, {W})";
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Color
        {
            public float R, G, B, A;
            public Color(float r, float g, float b, float a = 1.0f)
            {
                R = r; G = g; B = b; A = a;
            }
        }

        // Estructura POINT (equivalente a WinAPI POINT)
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner

        }

        public struct WNDCLASSEX
        {
            public int cbSize;
            public int style;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public WndProcDelegate lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        // Estructura MSG utilizada por PeekMessage
        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hWnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public Point pt;
        }


    public struct Rectangle : IEquatable<Rectangle>
    {
        public int X; 
        public int Y;
        public int Width;
        public int Height;

        public Rectangle(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        // Sobrecarga del operador ==
        public static bool operator ==(Rectangle left, Rectangle right)
        {
            return left.Equals(right);
        }

        // Sobrecarga del operador !=
        public static bool operator !=(Rectangle left, Rectangle right)
        {
            return !(left == right);
        }

        // Implementación de IEquatable<Rectangle>
        public bool Equals(Rectangle other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Width == other.Width &&
                   Height == other.Height;
        }

        // Sobrescritura de Equals para compatibilidad con object.Equals
        public override bool Equals(object obj)
        {
            return obj is Rectangle other && Equals(other);
        }

        // Sobrescritura de GetHashCode para consistencia con Equals
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }

        // Opcional: Sobrescritura de ToString para mejor legibilidad
        public override string ToString()
        {
            return $"Rectangle(X={X}, Y={Y}, Width={Width}, Height={Height})";
        }
    }

    // Estructura WINDOWPLACEMENT para obtener el estado de la ventana
    [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }


        // ================= ESTRUCTURAS PARA PE PARSING =================
        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_DOS_HEADER
        {
            public ushort e_magic;
            public ushort e_cblp;
            public ushort e_cp;
            public ushort e_crlc;
            public ushort e_cparhdr;
            public ushort e_minalloc;
            public ushort e_maxalloc;
            public ushort e_ss;
            public ushort e_sp;
            public ushort e_csum;
            public ushort e_ip;
            public ushort e_cs;
            public ushort e_lfarlc;
            public ushort e_ovno;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public ushort[] e_res1;
            public ushort e_oemid;
            public ushort e_oeminfo;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public ushort[] e_res2;
            public int e_lfanew;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_NT_HEADERS
        {
            public uint Signature;
            public IMAGE_FILE_HEADER FileHeader;
            public IMAGE_OPTIONAL_HEADER OptionalHeader;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_FILE_HEADER
        {
            public ushort Machine;
            public ushort NumberOfSections;
            public uint TimeDateStamp;
            public uint PointerToSymbolTable;
            public uint NumberOfSymbols;
            public ushort SizeOfOptionalHeader;
            public ushort Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_OPTIONAL_HEADER
        {
            public ushort Magic;
            public byte MajorLinkerVersion;
            public byte MinorLinkerVersion;
            public uint SizeOfCode;
            public uint SizeOfInitializedData;
            public uint SizeOfUninitializedData;
            public uint AddressOfEntryPoint;
            public uint BaseOfCode;
            public uint BaseOfData;
            public uint ImageBase;
            public uint SectionAlignment;
            public uint FileAlignment;
            public ushort MajorOperatingSystemVersion;
            public ushort MinorOperatingSystemVersion;
            public ushort MajorImageVersion;
            public ushort MinorImageVersion;
            public ushort MajorSubsystemVersion;
            public ushort MinorSubsystemVersion;
            public uint Win32VersionValue;
            public uint SizeOfImage;
            public uint SizeOfHeaders;
            public uint CheckSum;
            public ushort Subsystem;
            public ushort DllCharacteristics;
            public uint SizeOfStackReserve;
            public uint SizeOfStackCommit;
            public uint SizeOfHeapReserve;
            public uint SizeOfHeapCommit;
            public uint LoaderFlags;
            public uint NumberOfRvaAndSizes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public IMAGE_DATA_DIRECTORY[] DataDirectory;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_DATA_DIRECTORY
        {
            public uint VirtualAddress;
            public uint Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IMAGE_EXPORT_DIRECTORY
        {
            public uint Characteristics;
            public uint TimeDateStamp;
            public ushort MajorVersion;
            public ushort MinorVersion;
            public uint Name;
            public uint Base;
            public uint NumberOfFunctions;
            public uint NumberOfNames;
            public uint AddressOfFunctions;
            public uint AddressOfNames;
            public uint AddressOfNameOrdinals;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct MODULEENTRY32
        {
            public uint dwSize;
            public uint th32ModuleID;
            public uint th32ProcessID;
            public uint GlblcntUsage;
            public uint ProccntUsage;
            public IntPtr modBaseAddr;
            public uint modBaseSize;
            public IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExePath;
        }

        // Estructuras Wide (Unicode)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MODULEENTRY32W
        {
            public uint dwSize;
            public uint th32ModuleID;
            public uint th32ProcessID;
            public uint GlblcntUsage;
            public uint ProccntUsage;
            public IntPtr modBaseAddr;
            public uint modBaseSize;
            public IntPtr hModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szModule;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExePath;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct PROCESSENTRY32W
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct PROCESSENTRY32
        {
            public uint dwSize;
            public uint cntUsage;
            public uint th32ProcessID;
            public IntPtr th32DefaultHeapID;
            public uint th32ModuleID;
            public uint cntThreads;
            public uint th32ParentProcessID;
            public int pcPriClassBase;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szExeFile;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D3D11_BUFFER_DESC
        {
            public uint ByteWidth;
            public uint Usage;
            public uint BindFlags;
            public uint CPUAccessFlags;
            public uint MiscFlags;
            public uint StructureByteStride;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct D3D11_MAPPED_SUBRESOURCE
        {
            public IntPtr pData;
            public uint RowPitch;
            public uint DepthPitch;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int CreateBufferDelegate(
            IntPtr device,
            ref D3D11_BUFFER_DESC desc,
            IntPtr initialData,
            out IntPtr buffer);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int MapDelegate(
            IntPtr context,
            IntPtr resource,
            uint subresource,
            uint mapType,
            uint mapFlags,
            out D3D11_MAPPED_SUBRESOURCE mappedResource);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void UnmapDelegate(
            IntPtr context,
            IntPtr resource,
            uint subresource);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void DrawDelegate(
            IntPtr context,
            int vertexCount,
            int startVertexLocation);
    }


    public enum MouseButtons
    {
        None = 0,
        Left = 1,
        Right = 2,
        Middle = 4
    }

    public enum Keys
    {
        // Teclas alfabéticas
        A = 0x41,
        B = 0x42,
        C = 0x43,
        D = 0x44,
        E = 0x45,
        F = 0x46,
        G = 0x47,
        H = 0x48,
        I = 0x49,
        J = 0x4A,
        K = 0x4B,
        L = 0x4C,
        M = 0x4D,
        N = 0x4E,
        O = 0x4F,
        P = 0x50,
        Q = 0x51,
        R = 0x52,
        S = 0x53,
        T = 0x54,
        U = 0x55,
        V = 0x56,
        W = 0x57,
        X = 0x58,
        Y = 0x59,
        Z = 0x5A,

        // Teclas numéricas
        D0 = 0x30,
        D1 = 0x31,
        D2 = 0x32,
        D3 = 0x33,
        D4 = 0x34,
        D5 = 0x35,
        D6 = 0x36,
        D7 = 0x37,
        D8 = 0x38,
        D9 = 0x39,

        // Teclas de función
        F1 = 0x70,
        F2 = 0x71,
        F3 = 0x72,
        F4 = 0x73,
        F5 = 0x74,
        F6 = 0x75,
        F7 = 0x76,
        F8 = 0x77,
        F9 = 0x78,
        F10 = 0x79,
        F11 = 0x7A,
        F12 = 0x7B,

        // Teclas especiales
        Space = 0x20,
        Enter = 0x0D,
        Escape = 0x1B,
        LShift = 0xA0,
        RShift = 0xA1,
        LControl = 0xA2,
        RControl = 0xA3,
        LAlt = 0xA4,
        RAlt = 0xA5,
        Tab = 0x09,
        Backspace = 0x08,
        Insert = 0x2D,
        Delete = 0x2E,
        Home = 0x24,
        End = 0x23,
        PageUp = 0x21,
        PageDown = 0x22,

        // Teclas de dirección
        Left = 0x25,
        Up = 0x26,
        Right = 0x27,
        Down = 0x28,

        // Teclas del teclado numérico
        NumPad0 = 0x60,
        NumPad1 = 0x61,
        NumPad2 = 0x62,
        NumPad3 = 0x63,
        NumPad4 = 0x64,
        NumPad5 = 0x65,
        NumPad6 = 0x66,
        NumPad7 = 0x67,
        NumPad8 = 0x68,
        NumPad9 = 0x69,
        Multiply = 0x6A,
        Add = 0x6B,
        Subtract = 0x6D,
        Decimal = 0x6E,
        Divide = 0x6F,
    }

}
