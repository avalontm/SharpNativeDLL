using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    public static unsafe class OpenGLHook
    {
        #region PROPERTIES
        // Constantes OpenGL
        private const int GL_LINES = 0x0001;
        private const int GL_TRIANGLES = 0x0004;
        private const int GL_QUADS = 0x0007;
        private const int GL_LINE_LOOP = 0x0002;
        private const int GL_MODELVIEW = 0x1700;
        private const int GL_PROJECTION = 0x1701;
        private const int GL_VIEWPORT = 0x0BA2;
        private const int GL_BLEND = 0x0BE2;
        private const int GL_SRC_ALPHA = 0x0302;
        private const int GL_ONE_MINUS_SRC_ALPHA = 0x0303;
        private const int GL_DEPTH_TEST = 0x0B71;
        private const uint GL_ALL_ATTRIB_BITS = 0xFFFFFFFF;

        // Estado
        private static bool _initialized = false;
        private static int _screenWidth = 1920;
        private static int _screenHeight = 1080;
        private static Action _renderCallback = null;

        // Hooking
        private static IntPtr _originalWglSwapBuffers = IntPtr.Zero;
        private static IntPtr _hookTrampoline = IntPtr.Zero;
        private static byte[] _originalBytes = new byte[12];

        // Delegados
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool wglSwapBuffersDelegate(IntPtr hdc);
        private static wglSwapBuffersDelegate _originalWglSwapBuffersDelegate;

        public static bool Initialized => _initialized;
        public static int ScreenWidth => _screenWidth;
        public static int ScreenHeight => _screenHeight;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, uint* lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, uint dwSize);

        #endregion

        public static void SetRenderCallback(Action renderCallback)
        {
            _renderCallback = renderCallback;
        }

        public static bool Initialize(uint processId)
        {
            if (_initialized) return true;

            try
            {
                var hProcess = WinInterop.OpenProcess(WinInterop.PROCESS_ALL_ACCESS, false, processId);

                if (hProcess == IntPtr.Zero)
                {
                    Console.WriteLine($"Failed to open process: (0x{WinInterop.GetLastError():X8})");
                    return false;
                }

                Console.WriteLine($"hProcess: {hProcess:X8}");

                // Obtener dirección de wglSwapBuffers
                IntPtr hOpenGL = WinInterop.GetModuleBaseEx(processId, "opengl32.dll");

                if (hOpenGL == IntPtr.Zero)
                {
                    Console.WriteLine($"Failed to find opengl32.dll");
                    return false;
                }

                Console.WriteLine($"hOpenGL: {hOpenGL:X8}");

                _originalWglSwapBuffers = WinInterop.GetProcAddressEx(hProcess, hOpenGL, "wglSwapBuffers");

                if (_originalWglSwapBuffers == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to find wglSwapBuffers");
                    return false;
                }

                Console.WriteLine($"originalWglSwapBuffers: {_originalWglSwapBuffers:X8}");

                _originalWglSwapBuffersDelegate = Marshal.GetDelegateForFunctionPointer<wglSwapBuffersDelegate>(_originalWglSwapBuffers);

                // Crear trampolín
                if (!CreateTrampoline())
                {
                    Console.WriteLine("Failed to create trampoline");
                    return false;
                }

                // Instalar hook
                if (!InstallHook())
                {
                    Console.WriteLine("Failed to install hook");
                    return false;
                }

                _initialized = true;
                Console.WriteLine($"Hook Installed!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Initialization error: {ex}");
                return false;
            }
        }

        private static bool CreateTrampoline()
        {
            try
            {
                bool is64Bit = IntPtr.Size == 8;
                int patchSize = is64Bit ? 12 : 5; // Tamaño suficiente para el jmp

                // 1. Leer los bytes originales
                _originalBytes = new byte[patchSize];
                for (int i = 0; i < patchSize; i++)
                {
                    _originalBytes[i] = Marshal.ReadByte(_originalWglSwapBuffers, i);
                }

                // 2. Asignar memoria ejecutable para el trampolín
                _hookTrampoline = Marshal.AllocHGlobal(patchSize + (is64Bit ? 12 : 5));

                // 3. Construir el trampolín
                if (is64Bit)
                {
                    // x64: [instrucciones originales] + jmp a original+12
                    byte[] trampolineCode = new byte[patchSize + 12];

                    // Copiar bytes originales
                    Buffer.BlockCopy(_originalBytes, 0, trampolineCode, 0, patchSize);

                    // mov rax, &original+patchSize
                    trampolineCode[patchSize] = 0x48; // REX.W
                    trampolineCode[patchSize + 1] = 0xB8; // MOV RAX
                    long targetAddr = _originalWglSwapBuffers.ToInt64() + patchSize;
                    Buffer.BlockCopy(BitConverter.GetBytes(targetAddr), 0, trampolineCode, patchSize + 2, 8);

                    // jmp rax
                    trampolineCode[patchSize + 10] = 0xFF;
                    trampolineCode[patchSize + 11] = 0xE0;

                    Marshal.Copy(trampolineCode, 0, _hookTrampoline, trampolineCode.Length);
                }
                else
                {
                    // x86: [instrucciones originales] + jmp a original+5
                    byte[] trampolineCode = new byte[patchSize + 5];

                    // Copiar bytes originales
                    Buffer.BlockCopy(_originalBytes, 0, trampolineCode, 0, patchSize);

                    // jmp rel32
                    trampolineCode[patchSize] = 0xE9;
                    int targetAddr = _originalWglSwapBuffers.ToInt32() + patchSize;
                    int jmpOffset = targetAddr - (_hookTrampoline.ToInt32() + patchSize + 5);
                    Buffer.BlockCopy(BitConverter.GetBytes(jmpOffset), 0, trampolineCode, patchSize + 1, 4);

                    Marshal.Copy(trampolineCode, 0, _hookTrampoline, trampolineCode.Length);
                }

                // 4. Marcar como ejecutable
                uint oldProtect;
                VirtualProtect(_hookTrampoline, (uint)(patchSize + (is64Bit ? 12 : 5)), 0x40, out oldProtect);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Trampoline error: {ex}");
                return false;
            }
        }

        private static bool CallOriginalWglSwapBuffers(IntPtr hdc)
        {
            if (_hookTrampoline == IntPtr.Zero)
                return false;

            // Llamar al trampolín usando un puntero de función nativo
            var originalFunc = (delegate* unmanaged<IntPtr, bool>)_hookTrampoline.ToPointer();
            return originalFunc(hdc);
        }

        private static bool InstallHook()
        {
            try
            {
                byte[] hookCode;
                if (IntPtr.Size == 8) // x64
                {
                    hookCode = new byte[] {
                        0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // mov rax, &HookedWglSwapBuffers
                        0xFF, 0xE0                                                    // jmp rax
                    };
                    fixed (byte* p = hookCode)
                    {
                        *(ulong*)(p + 2) = (ulong)((delegate* unmanaged<IntPtr, bool>)&HookedWglSwapBuffers);
                    }
                }
                else // x86
                {
                    hookCode = new byte[] {
                        0xE9, 0x00, 0x00, 0x00, 0x00  // jmp &HookedWglSwapBuffers
                    };
                    fixed (byte* p = hookCode)
                    {
                        *(uint*)(p + 1) = (uint)((int)((delegate* unmanaged<IntPtr, bool>)&HookedWglSwapBuffers) - ((int)_originalWglSwapBuffers + 5));
                    }
                }

                // Cambiar protección de memoria
                uint oldProtect;
                VirtualProtect(_originalWglSwapBuffers, (uint)hookCode.Length, 0x40, out oldProtect);

                // Escribir hook
                UIntPtr bytesWritten;
                WriteProcessMemory(Process.GetCurrentProcess().Handle, _originalWglSwapBuffers, hookCode, (uint)hookCode.Length, out bytesWritten);

                // Restaurar protección
                VirtualProtect(_originalWglSwapBuffers, (uint)hookCode.Length, oldProtect, out oldProtect);

                // Flush cache
                FlushInstructionCache(Process.GetCurrentProcess().Handle, _originalWglSwapBuffers, (uint)hookCode.Length);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hook installation error: {ex}");
                return false;
            }
        }

        [UnmanagedCallersOnly]
        private static bool HookedWglSwapBuffers(IntPtr hdc)
        {
            try
            {
                if (hdc != IntPtr.Zero)
                {
                    IntPtr originalContext = OpenGLInterop.wglGetCurrentContext();
                    IntPtr originalHdc = OpenGLInterop.wglGetCurrentDC();

                    // Crear contexto temporal
                    IntPtr tempContext = OpenGLInterop.wglCreateContext(hdc);
                    if (tempContext != IntPtr.Zero &&
                        OpenGLInterop.wglShareLists(originalContext, tempContext))
                    {
                        OpenGLInterop.wglMakeCurrent(hdc, tempContext);

                        try
                        {
                            SetupOverlayRendering();
                            _renderCallback?.Invoke();
                            RestoreOpenGLState();
                        }
                        finally
                        {
                            OpenGLInterop.wglMakeCurrent(originalHdc, originalContext);
                            OpenGLInterop.wglDeleteContext(tempContext);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Render error: {ex}");
            }

            return CallOriginalWglSwapBuffers(hdc);
        }

        private static void SetupOverlayRendering()
        {
            OpenGLInterop.glPushAttrib(GL_ALL_ATTRIB_BITS);
            OpenGLInterop.glMatrixMode(GL_PROJECTION);
            OpenGLInterop.glPushMatrix();
            OpenGLInterop.glLoadIdentity();
            OpenGLInterop.glOrtho(0, _screenWidth, _screenHeight, 0, -1, 1);
            OpenGLInterop.glMatrixMode(GL_MODELVIEW);
            OpenGLInterop.glPushMatrix();
            OpenGLInterop.glLoadIdentity();
            OpenGLInterop.glEnable(GL_BLEND);
            OpenGLInterop.glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
            OpenGLInterop.glDisable(GL_DEPTH_TEST);
        }

        private static void RestoreOpenGLState()
        {
            OpenGLInterop.glMatrixMode(GL_PROJECTION);
            OpenGLInterop.glPopMatrix();
            OpenGLInterop.glMatrixMode(GL_MODELVIEW);
            OpenGLInterop.glPopMatrix();
            OpenGLInterop.glPopAttrib();
        }

        public static void Cleanup()
        {
            if (!_initialized) return;

            try
            {
                // Restaurar bytes originales
                if (_originalWglSwapBuffers != IntPtr.Zero && _originalBytes != null)
                {
                    uint oldProtect;
                    VirtualProtect(_originalWglSwapBuffers, (uint)_originalBytes.Length, 0x40, out oldProtect);

                    UIntPtr bytesWritten;
                    WriteProcessMemory(Process.GetCurrentProcess().Handle, _originalWglSwapBuffers, _originalBytes, (uint)_originalBytes.Length, out bytesWritten);

                    VirtualProtect(_originalWglSwapBuffers, (uint)_originalBytes.Length, oldProtect, out oldProtect);
                    FlushInstructionCache(Process.GetCurrentProcess().Handle, _originalWglSwapBuffers, (uint)_originalBytes.Length);
                }

                // Liberar memoria
                if (_hookTrampoline != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_hookTrampoline);
                    _hookTrampoline = IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cleanup error: {ex}");
            }
            finally
            {
                _initialized = false;
            }
        }

        // ================= DRAWING FUNCTIONS =================

        /// <summary>
        /// Dibuja una línea entre dos puntos
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
        {
            if (!_initialized) return;

            OpenGLInterop.glLineWidth(thickness);
            OpenGLInterop.glBegin(GL_LINES);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            OpenGLInterop.glVertex2f(start.X, start.Y);
            OpenGLInterop.glVertex2f(end.X, end.Y);
            OpenGLInterop.glEnd();
        }

        /// <summary>
        /// Dibuja un rectángulo (contorno)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawBox(float x, float y, float width, float height, float thickness, Color color)
        {
            if (!_initialized) return;

            OpenGLInterop.glLineWidth(thickness);
            OpenGLInterop.glBegin(GL_LINE_LOOP);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            OpenGLInterop.glVertex2f(x, y);
            OpenGLInterop.glVertex2f(x + width, y);
            OpenGLInterop.glVertex2f(x + width, y + height);
            OpenGLInterop.glVertex2f(x, y + height);
            OpenGLInterop.glEnd();
        }

        /// <summary>
        /// Dibuja un rectángulo relleno
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawFilledBox(float x, float y, float width, float height, Color color)
        {
            if (!_initialized) return;

            OpenGLInterop.glBegin(GL_QUADS);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            OpenGLInterop.glVertex2f(x, y);
            OpenGLInterop.glVertex2f(x + width, y);
            OpenGLInterop.glVertex2f(x + width, y + height);
            OpenGLInterop.glVertex2f(x, y + height);
            OpenGLInterop.glEnd();
        }

        /// <summary>
        /// Dibuja un círculo (contorno)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawCircle(Vector2 center, float radius, int segments, float thickness, Color color)
        {
            if (!_initialized) return;

            OpenGLInterop.glLineWidth(thickness);
            OpenGLInterop.glBegin(GL_LINE_LOOP);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * 6.28318530718f;
                float x = center.X + FastCos(angle) * radius;
                float y = center.Y + FastSin(angle) * radius;
                OpenGLInterop.glVertex2f(x, y);
            }

            OpenGLInterop.glEnd();
        }

        /// <summary>
        /// Dibuja un círculo relleno
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawFilledCircle(Vector2 center, float radius, int segments, Color color)
        {
            if (!_initialized) return;

            OpenGLInterop.glBegin(GL_TRIANGLES);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);

            for (int i = 0; i < segments; i++)
            {
                float angle1 = (float)i / segments * 6.28318530718f;
                float angle2 = (float)(i + 1) / segments * 6.28318530718f;

                OpenGLInterop.glVertex2f(center.X, center.Y);
                OpenGLInterop.glVertex2f(center.X + FastCos(angle1) * radius, center.Y + FastSin(angle1) * radius);
                OpenGLInterop.glVertex2f(center.X + FastCos(angle2) * radius, center.Y + FastSin(angle2) * radius);
            }

            OpenGLInterop.glEnd();
        }

        /// <summary>
        /// Dibuja texto básico (requiere implementación de font atlas)
        /// </summary>
        public static void DrawText(string text, Vector2 position, Color color, float scale = 1.0f)
        {
            if (!_initialized || string.IsNullOrEmpty(text)) return;

            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            // Implementación de texto requeriría una textura de fuente pre-cargada
            // Aquí iría la lógica para renderizar cada carácter
        }

        // ================= MATH FUNCTIONS =================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FastSin(float x)
        {
            // Optimización para cálculo rápido de seno
            const float B = 4.0f / MathF.PI;
            const float C = -4.0f / (MathF.PI * MathF.PI);

            float y = B * x + C * x * MathF.Abs(x);
            return 0.225f * (y * MathF.Abs(y) - y) + y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FastCos(float x)
        {
            return FastSin(x + MathF.PI / 2);
        }

    }
}