using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AvalonInjectLib
{

    public static unsafe class FontRenderer
    {
        // ============ CONSTANTES ============
        private const int GL_TEXTURE_2D = 0x0DE1;
        private const int GL_QUADS = 0x0007;
        private const int GL_BLEND = 0x0BE2;
        private const int GL_RGBA = 0x1908;
        private const int GL_UNSIGNED_BYTE = 0x1401;

        // ============ ESTADO ============
        private static uint _fontTexture;
        private static float _charWidth = 8.0f;
        private static float _charHeight = 13.0f;
        private static int _textureWidth = 128;
        private static int _textureHeight = 128;

        // Modificación para corregir el error CS1503: Argumento 2: no se puede convertir de 'uint[]' a 'uint*'  
        public static void Initialize()
        {
            // 1. Crear textura OpenGL  
            uint texture;
            glGenTextures(1, &texture);
            _fontTexture = texture;
            glBindTexture(GL_TEXTURE_2D, _fontTexture);

            // 2. Generar bitmap de fuente minimalista (8x13 píxeles por carácter)  
            byte[] pixels = GenerateSimpleFontBitmap();

            fixed (byte* ptr = pixels)
            {
                glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, _textureWidth, _textureHeight, 0,
                             GL_RGBA, GL_UNSIGNED_BYTE, ptr);
            }

            // 3. Configurar textura  
            glTexParameteri(GL_TEXTURE_2D, 0x2801, 0x2601); // GL_LINEAR  
            glTexParameteri(GL_TEXTURE_2D, 0x2800, 0x2601); // GL_LINEAR  
        }

        private static byte[] GenerateSimpleFontBitmap()
        {
            byte[] pixels = new byte[_textureWidth * _textureHeight * 4];

            // Caracteres ASCII 32-126 (95 caracteres)
            for (int i = 32; i < 127; i++)
            {
                int charX = (i % 16) * 8;
                int charY = (i / 16) * 13;

                // Datos hardcodeados para caracteres básicos (ejemplo: 'A')
                if (i == 'A') GenerateCharA(pixels, charX, charY);
                // Añadir más caracteres según sea necesario...
            }

            return pixels;
        }

        private static void GenerateCharA(byte[] pixels, int startX, int startY)
        {
            // Patrón de la letra 'A' en 0s y 1s (8x13)
            int[,] pattern = new int[13, 8]
            {
        {0,0,1,1,1,0,0,0},
        {0,1,1,0,1,1,0,0},
        {1,1,0,0,0,1,1,0},
        {1,1,0,0,0,1,1,0},
        {1,1,1,1,1,1,1,0},
        {1,1,0,0,0,1,1,0},
        {1,1,0,0,0,1,1,0},
        {1,1,0,0,0,1,1,0},
        {0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0},
        {0,0,0,0,0,0,0,0}
            };

            for (int y = 0; y < 13; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int pos = ((startY + y) * _textureWidth + (startX + x)) * 4;
                    if (pattern[y, x] == 1)
                    {
                        pixels[pos] = 255; // R
                        pixels[pos + 1] = 255; // G
                        pixels[pos + 2] = 255; // B
                        pixels[pos + 3] = 255; // A
                    }
                }
            }
        }

        // ============ RENDERIZADO DE TEXTO ============
        public static void DrawText(string text, float x, float y, float scale, UIFramework.Color color)
        {
            if (_fontTexture == 0) return;

            glEnable(GL_TEXTURE_2D);
            glBindTexture(GL_TEXTURE_2D, _fontTexture);
            glEnable(GL_BLEND);
            glBlendFunc(0x0302, 0x0303); // SRC_ALPHA, ONE_MINUS_SRC_ALPHA

            float startX = x;
            foreach (char c in text)
            {
                if (c < 32 || c > 126) continue;

                int charIndex = c - 32;
                float texX = (charIndex % 16) * (_charWidth / _textureWidth);
                float texY = (charIndex / 16) * (_charHeight / _textureHeight);

                float w = _charWidth * scale;
                float h = _charHeight * scale;

                glBegin(GL_QUADS);
                glColor3f(color.R, color.R, color.R);

                glTexCoord2f(texX, texY); glVertex2f(startX, y);
                glTexCoord2f(texX + (_charWidth / _textureWidth), texY); glVertex2f(startX + w, y);
                glTexCoord2f(texX + (_charWidth / _textureWidth), texY + (_charHeight / _textureHeight));
                glVertex2f(startX + w, y + h);
                glTexCoord2f(texX, texY + (_charHeight / _textureHeight)); glVertex2f(startX, y + h);

                glEnd();

                startX += w;
            }

            glDisable(GL_TEXTURE_2D);
        }

        // ============ IMPORTS OPENGL ============
        [DllImport("opengl32.dll")]
        private static extern void glGenTextures(int n, uint* textures);

        [DllImport("opengl32.dll")]
        private static extern void glBindTexture(int target, uint texture);

        [DllImport("opengl32.dll")]
        private static extern void glTexImage2D(int target, int level, int internalFormat,
                                              int width, int height, int border,
                                              int format, int type, void* data);

        [DllImport("opengl32.dll")]
        private static extern void glTexParameteri(int target, int pname, int param);

        [DllImport("opengl32.dll")]
        private static extern void glEnable(int cap);

        [DllImport("opengl32.dll")]
        private static extern void glDisable(int cap);

        [DllImport("opengl32.dll")]
        private static extern void glBlendFunc(int sfactor, int dfactor);

        [DllImport("opengl32.dll")]
        private static extern void glBegin(int mode);

        [DllImport("opengl32.dll")]
        private static extern void glEnd();

        [DllImport("opengl32.dll")]
        private static extern void glTexCoord2f(float s, float t);

        [DllImport("opengl32.dll")]
        private static extern void glVertex2f(float x, float y);

        [DllImport("opengl32.dll")]
        private static extern void glColor3f(float r, float g, float b);
    }
}
