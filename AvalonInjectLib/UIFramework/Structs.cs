using System.Runtime.InteropServices;

namespace AvalonInjectLib.UIFramework
{
    using System;

    public struct Color : IEquatable<Color>
    {
        public byte R, G, B, A;

        public Color(byte r, byte g, byte b, byte a = 255)
        {
            R = r; G = g; B = b; A = a;
        }

        // Colores predefinidos
        public static readonly Color White = new(255, 255, 255);
        public static readonly Color Black = new(0, 0, 0);
        public static readonly Color Red = new(255, 0, 0);
        public static readonly Color Green = new(0, 255, 0);
        public static readonly Color Blue = new(0, 0, 255);
        public static readonly Color Yellow = new(255, 255, 0);
        public static readonly Color Magenta = new(255, 0, 255);
        public static readonly Color Cyan = new(0, 255, 255);
        public static readonly Color Transparent = new(0, 0, 0, 0);
        public static readonly Color Gray = new(128, 128, 128);
        public static readonly Color LightGray = new(192, 192, 192);
        public static readonly Color DarkGray = new(64, 64, 64);
        public static readonly Color Orange = new(255, 165, 0);
        public static readonly Color Purple = new(128, 0, 128);

        public override string ToString() => $"R:{R} G:{G} B:{B} A:{A}";

        public Color WithAlpha(byte a) => new Color(R, G, B, a);

        public Color WithAlpha(float alpha)
        {
            // Asegurar que el valor alpha esté en el rango 0-1
            float clampedAlpha = Math.Clamp(alpha, 0f, 1f);

            // Convertir a byte (0-255) y crear nuevo color
            byte alphaByte = (byte)(clampedAlpha * 255);
            return new Color(R, G, B, alphaByte);
        }

        public Color Lerp(Color other, float t)
        {
            t = Math.Clamp(t, 0, 1);
            return new Color(
                (byte)(R + (other.R - R) * t),
                (byte)(G + (other.G - G) * t),
                (byte)(B + (other.B - B) * t),
                (byte)(A + (other.A - A) * t)
            );
        }

        // Implementación de IEquatable<Color>
        public bool Equals(Color other)
        {
            return R == other.R && G == other.G && B == other.B && A == other.A;
        }

        // Override de Object.Equals
        public override bool Equals(object obj)
        {
            return obj is Color other && Equals(other);
        }

        // Override de GetHashCode para consistencia
        public override int GetHashCode()
        {
            // Usar HashCode.Combine para mejor distribución
            return HashCode.Combine(R, G, B, A);
        }

        // Operadores de comparación
        public static bool operator ==(Color left, Color right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Color left, Color right)
        {
            return !left.Equals(right);
        }

        // Conversión implícita desde uint (formato ARGB)
        public static implicit operator Color(uint argb)
        {
            return new Color(
                (byte)((argb >> 16) & 0xFF), // R
                (byte)((argb >> 8) & 0xFF),  // G
                (byte)(argb & 0xFF),         // B
                (byte)((argb >> 24) & 0xFF)  // A
            );
        }

        // Conversión implícita a uint (formato ARGB)
        public static implicit operator uint(Color color)
        {
            return ((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B;
        }

        // Método de conveniencia para crear color desde valores float (0-1)
        public static Color FromFloat(float r, float g, float b, float a = 1.0f)
        {
            return new Color(
                (byte)(Math.Clamp(r, 0, 1) * 255),
                (byte)(Math.Clamp(g, 0, 1) * 255),
                (byte)(Math.Clamp(b, 0, 1) * 255),
                (byte)(Math.Clamp(a, 0, 1) * 255)
            );
        }

        // Método de conveniencia para crear color desde valores HSV
        public static Color FromHSV(float h, float s, float v, float a = 1.0f)
        {
            h = h % 360;
            if (h < 0) h += 360;

            s = Math.Clamp(s, 0, 1);
            v = Math.Clamp(v, 0, 1);
            a = Math.Clamp(a, 0, 1);

            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            float m = v - c;

            float r, g, b;
            if (h >= 0 && h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h >= 60 && h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h >= 120 && h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h >= 180 && h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h >= 240 && h < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }

            return new Color(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255),
                (byte)(a * 255)
            );
        }

        // Propiedades de conveniencia
        public bool IsTransparent => A == 0;
        public bool IsOpaque => A == 255;

        // Luminosidad percibida (usando fórmula estándar)
        public float Luminance => (0.299f * R + 0.587f * G + 0.114f * B) / 255f;

        // Método para obtener color contrastante (blanco o negro)
        public Color GetContrastingColor()
        {
            return Luminance > 0.5f ? Black : White;
        }

        public static Color FromArgb(byte r, byte g, byte b)
        {
            return new Color(r, g, b, 255);
        }

        public static Color FromArgb(byte a, byte r, byte g, byte b)
        {
            return new Color(r, g, b, a);
        }
    }

    /// <summary>
    /// Modos de ajuste de la imagen
    /// </summary>
    public enum StretchMode
    {
        /// <summary>No ajustar - mostrar tamaño original</summary>
        None,
        /// <summary>Estirar para llenar todo el espacio (puede distorsionar)</summary>
        Fill,
        /// <summary>Mantener relación de aspecto ajustando al tamaño disponible (sin recortar)</summary>
        Uniform,
        /// <summary>Mantener relación de aspecto llenando todo el espacio (puede recortar)</summary>
        UniformToFill
    }


    /// <summary>
    /// Unidad de medida para definir el tamaño de columnas y filas
    /// </summary>
    public enum GridUnitType
    {
        Auto,    // Tamaño automático basado en el contenido
        Pixel,   // Tamaño fijo en píxeles
        Star     // Tamaño proporcional (*, 2*, 3*, etc.)
    }

    /// <summary>
    /// Definición de una columna o fila del Grid
    /// </summary>
    public struct GridLength
    {
        public float Value { get; }
        public GridUnitType UnitType { get; }

        public GridLength(float value, GridUnitType unitType)
        {
            Value = Math.Max(0, value);
            UnitType = unitType;
        }

        public static GridLength Auto => new GridLength(0, GridUnitType.Auto);
        public static GridLength Star => new GridLength(1, GridUnitType.Star);
        public static GridLength Pixel(float pixels) => new GridLength(pixels, GridUnitType.Pixel);
        public static GridLength Stars(float stars) => new GridLength(stars, GridUnitType.Star);

        public override string ToString()
        {
            return UnitType switch
            {
                GridUnitType.Auto => "Auto",
                GridUnitType.Pixel => $"{Value}px",
                GridUnitType.Star => Value == 1 ? "*" : $"{Value}*",
                _ => "Auto"
            };
        }
    }

    public struct Thickness : IEquatable<Thickness>
    {
        public float Left, Top, Right, Bottom;

        public Thickness(float uniform) : this(uniform, uniform, uniform, uniform) { }
        public Thickness(float horizontal, float vertical) : this(horizontal, vertical, horizontal, vertical) { }
        public Thickness(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public bool Equals(Thickness other)
        {
            return Left == other.Left &&
                   Top == other.Top &&
                   Right == other.Right &&
                   Bottom == other.Bottom;
        }

        public override bool Equals(object obj)
        {
            return obj is Thickness other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Left.GetHashCode();
                hashCode = (hashCode * 397) ^ Top.GetHashCode();
                hashCode = (hashCode * 397) ^ Right.GetHashCode();
                hashCode = (hashCode * 397) ^ Bottom.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Thickness left, Thickness right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Thickness left, Thickness right)
        {
            return !(left == right);
        }
    }

    public enum HorizontalAlignment { Left, Center, Right, Stretch }
    public enum VerticalAlignment { Top, Center, Bottom, Stretch }

    public enum Orientation { Horizontal, Vertical }

    public enum TextAlignment
    {
        Left,
        Center,
        Right,
        Justified
    }

    public struct TextStyle
    {
        public Color Color;
        public int Size;
        public bool Bold;
        public bool Italic;
        public HorizontalAlignment HAlign;
        public VerticalAlignment VAlign;

        public static readonly TextStyle Default = new()
        {
            Color = Color.White,
            Size = 12,
            HAlign = HorizontalAlignment.Left,
            VAlign = VerticalAlignment.Top
        };

        public static readonly TextStyle Button = new()
        {
            Color = Color.White,
            Size = 14,
            HAlign = HorizontalAlignment.Center,
            VAlign = VerticalAlignment.Center,
            Bold = false
        };
    }
}
