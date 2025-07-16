using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    internal static unsafe class OpenGLHook
    {
        #region PROPERTIES
        // Constantes OpenGL
        private const int GL_LINES = 0x0001;
        private const int GL_TRIANGLES = 0x0004;
        private const int GL_TRIANGLE_FAN = 0x0006;
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
        public const uint GL_CULL_FACE = 0x0B44;
        public const int GL_ONE = 1;
        public const int GL_BLEND_SRC_ALPHA = 0x80CB;
        public const int GL_BLEND_DST_ALPHA = 0x80CA;
        public const int GL_CLAMP_TO_EDGE = 0x812F;
        // Constantes de iluminación y texturas
        public const int GL_LIGHTING = 0x0B50;
        public const int GL_TEXTURE_2D = 0x0DE1;

        // Constantes de filtrado de texturas
        public const int GL_TEXTURE_MIN_FILTER = 0x2801;
        public const int GL_TEXTURE_MAG_FILTER = 0x2800;
        public const int GL_LINEAR = 0x2601;

        // Constantes de wrapping de texturas
        public const int GL_TEXTURE_WRAP_S = 0x2802;
        public const int GL_TEXTURE_WRAP_T = 0x2803;

        // Alpha testing constants
        public const int GL_ALPHA_TEST = 0x0BC0;
        public const int GL_GREATER = 0x0204;

        // Shading model constant
        public const int GL_SMOOTH = 0x1D01;

        // Estado
        private static bool _initialized = false;
        private static int _screenWidth = 1920;
        private static int _screenHeight = 1080;
        private static List<Action> _renderCallbacks = new List<Action>();


        // Hooking
        private static IntPtr _originalWglSwapBuffers = IntPtr.Zero;
        private static IntPtr _hookTrampoline = IntPtr.Zero;
        private static byte[] _originalBytes = new byte[12];

        // Delegados
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool wglSwapBuffersDelegate(IntPtr hdc);
        private static wglSwapBuffersDelegate _originalWglSwapBuffersDelegate;

        internal static bool Initialized => _initialized;
        internal static int ScreenWidth => _screenWidth;
        internal static int ScreenHeight => _screenHeight;
        internal static int[] _savedViewport = new int[4];

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, uint* lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, uint dwSize);

        #endregion

        /// <summary>
        /// Agrega un nuevo callback de renderizado a la lista
        /// </summary>
        internal static void AddRenderCallback(Action renderCallback)
        {
            if (renderCallback != null && !_renderCallbacks.Contains(renderCallback))
            {
                _renderCallbacks.Add(renderCallback);
            }
        }

        /// <summary>
        /// Elimina un callback de renderizado de la lista
        /// </summary>
        internal static void RemoveRenderCallback(Action renderCallback)
        {
            if (renderCallback != null)
            {
                _renderCallbacks.Remove(renderCallback);
            }
        }

        /// <summary>
        /// Ejecuta todos los callbacks de renderizado registrados
        /// </summary>
        internal static void ExecuteRenderCallbacks()
        {
            foreach (var callback in _renderCallbacks.ToList()) // Usamos ToList() para evitar modificaciones durante la iteración
            {
                try
                {
                    callback?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en render callback: {ex.Message}");
                }
            }
        }

        internal static bool Initialize(uint processId)
        {
            if (_initialized) return true;

            try
            {
                var hProcess = WinInterop.OpenProcess(WinInterop.PROCESS_ALL_ACCESS, false, processId);

                if (hProcess == IntPtr.Zero)
                {
                    Logger.Warning($"Failed to open process: (0x{WinInterop.GetLastError():X8})", "OpenGLHook");
                    return false;
                }

                Logger.Debug($"hProcess: {hProcess:X8}", "OpenGLHook");

                // Obtener dirección de wglSwapBuffers
                IntPtr hOpenGL = WinInterop.GetModuleBaseEx(processId, "opengl32.dll");

                if (hOpenGL == IntPtr.Zero)
                {
                    Logger.Error($"Failed to find opengl32.dll", "OpenGLHook");
                    return false;
                }

                Logger.Debug($"hOpenGL: {hOpenGL:X8}", "OpenGLHook");

                _originalWglSwapBuffers = WinInterop.GetProcAddressEx(hProcess, hOpenGL, "wglSwapBuffers");

                if (_originalWglSwapBuffers == IntPtr.Zero)
                {
                    Logger.Error("Failed to find wglSwapBuffers", "OpenGLHook");
                    return false;
                }

                Logger.Debug($"originalWglSwapBuffers: {_originalWglSwapBuffers:X8}", "OpenGLHook");

                _originalWglSwapBuffersDelegate = Marshal.GetDelegateForFunctionPointer<wglSwapBuffersDelegate>(_originalWglSwapBuffers);

                // Crear trampolín
                if (!CreateTrampoline())
                {
                    Logger.Error("Failed to create trampoline", "OpenGLHook");
                    return false;
                }

                // Instalar hook
                if (!InstallHook())
                {
                    Logger.Error("Failed to install hook", "OpenGLHook");
                    return false;
                }

                _initialized = true;
                Logger.Debug($"Hook Installed!", "OpenGLHook");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Initialization error: {ex.Message}", "OpenGLHook");
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
                    //Guardar contexto original
                    IntPtr originalContext = OpenGLInterop.wglGetCurrentContext();
                    IntPtr originalHdc = OpenGLInterop.wglGetCurrentDC();

                    //Crear contexto temporal si no existe uno actual
                    if (originalContext == IntPtr.Zero)
                    {
                        originalContext = OpenGLInterop.wglCreateContext(hdc);
                        if (originalContext == IntPtr.Zero)
                        {
                            return CallOriginalWglSwapBuffers(hdc);
                        }
                        OpenGLInterop.wglMakeCurrent(hdc, originalContext);
                    }

                    try
                    {
                        //Configurar renderizado
                        SetupRendering();

                        TextureRenderer.IsContexted = true;
                        FontRenderer.IsContexted = true;
                       
                        ProcessAllPendingFonts();
                        ProcessAllPendingTextures();
                       
                        ExecuteRenderCallbacks();
                    }
                    finally
                    {
                        if (TextureRenderer.HasPendingTextures())
                        {
                            ProcessAllPendingTextures();
                        }

                        RestoreRendering();

                        TextureRenderer.IsContexted = false;
                        FontRenderer.IsContexted = false;

                        // Solo hacer MakeCurrent si teníamos un contexto original
                        if (originalHdc != IntPtr.Zero)
                        {
                            OpenGLInterop.wglMakeCurrent(originalHdc, originalContext);
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

        // Método helper para procesar todas las texturas pendientes
        private static void ProcessAllPendingTextures()
        {
            int maxIterations = 100; // Prevenir bucle infinito
            int iterations = 0;

            while (TextureRenderer.HasPendingTextures() && iterations < maxIterations)
            {
                TextureRenderer.ProcessPendingTextures();
                iterations++;

                // Pequeña pausa para evitar usar demasiado CPU
                if (TextureRenderer.HasPendingTextures())
                {
                    WinInterop.Sleep(1);
                }
            }

            if (iterations >= maxIterations)
            {
                Logger.Warning("Se alcanzó el límite máximo de iteraciones procesando texturas pendientes", "OpenGLHook");
            }
            else if (iterations > 0)
            {
                Logger.Debug($"Procesadas texturas pendientes en {iterations} iteraciones", "OpenGLHook");
            }
        }

        private static void ProcessAllPendingFonts()
        {
            int maxIterations = 100; // Prevenir bucle infinito
            int iterations = 0;

            while (FontRenderer.HasPendingFonts() && iterations < maxIterations)
            {
                FontRenderer.ProcessPendingFonts();
                iterations++;

                // Pequeña pausa para evitar usar demasiado CPU
                if (FontRenderer.HasPendingFonts())
                {
                    WinInterop.Sleep(1);
                }
            }

            if (iterations >= maxIterations)
            {
                Logger.Warning("Se alcanzó el límite máximo de iteraciones procesando fonts pendientes", "OpenGLHook");
            }
            else if (iterations > 0)
            {
                Logger.Debug($"Procesadas fonts pendientes en {iterations} iteraciones", "OpenGLHook");
            }
        }

        private static void SetupRendering()
        {
            // Guardar todos los estados relevantes que vamos a modificar
            OpenGLInterop.glPushAttrib(GL_ALL_ATTRIB_BITS); // Guarda todos los atributos
            OpenGLInterop.glGetIntegerv(GL_VIEWPORT, _savedViewport);

            // Guardar matrices actuales
            OpenGLInterop.glMatrixMode(GL_PROJECTION);
            OpenGLInterop.glPushMatrix();
            OpenGLInterop.glMatrixMode(GL_MODELVIEW);
            OpenGLInterop.glPushMatrix();

            // Obtener dimensiones actuales del viewport
            int[] viewport = new int[4];
            OpenGLInterop.glGetIntegerv(GL_VIEWPORT, viewport);
            _screenWidth = viewport[2];
            _screenHeight = viewport[3];

            // Configurar proyección ortográfica 2D
            OpenGLInterop.glMatrixMode(GL_PROJECTION);
            OpenGLInterop.glLoadIdentity();
            OpenGLInterop.glOrtho(0, _screenWidth, _screenHeight, 0, -1, 1);

            // Configurar matriz modelo-vista
            OpenGLInterop.glMatrixMode(GL_MODELVIEW);
            OpenGLInterop.glLoadIdentity();

            // Estados básicos para overlay 2D
            OpenGLInterop.glDisable(GL_DEPTH_TEST);
            OpenGLInterop.glEnable(GL_BLEND);
            OpenGLInterop.glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
            OpenGLInterop.glDisable(GL_LIGHTING);
            OpenGLInterop.glDisable(GL_TEXTURE_2D); // Importante para overlays simples
        }

        private static void RestoreRendering()
        {
            // Restaurar matrices
            OpenGLInterop.glMatrixMode(GL_PROJECTION);
            OpenGLInterop.glPopMatrix();
            OpenGLInterop.glMatrixMode(GL_MODELVIEW);
            OpenGLInterop.glPopMatrix();

            // Restaurar atributos
            OpenGLInterop.glPopAttrib();

            // Restaurar viewport específicamente (por si acaso)
            OpenGLInterop.glViewport(_savedViewport[0], _savedViewport[1], _savedViewport[2], _savedViewport[3]);
        }

        internal static void Cleanup()
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
        internal static void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
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
        internal static void DrawBox(float x, float y, float width, float height, float thickness, Color color)
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
        internal static void DrawFilledBox(float x, float y, float width, float height, Color color)
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
        internal static void DrawCircle(Vector2 center, float radius, int segments, float thickness, Color color)
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
        internal static void DrawFilledCircle(Vector2 center, float radius, Color color)
        {
            if (!_initialized || radius <= 0) return;

            // Cálculo dinámico del número de segmentos basado en el radio
            int segments = CalculateOptimalSegments(radius);

            OpenGLInterop.glBegin(GL_TRIANGLE_FAN);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);

            // Primer vértice en el centro
            OpenGLInterop.glVertex2f(center.X, center.Y);

            // Vértices del perímetro - CORREGIDO: va de 0 a segments (inclusive)
            // para cerrar correctamente el círculo
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * 6.28318530718f; // 2π radianes
                OpenGLInterop.glVertex2f(
                    center.X + FastCos(angle) * radius,
                    center.Y + FastSin(angle) * radius
                );
            }

            OpenGLInterop.glEnd();
        }

        /// <summary>
        /// Dibuja un triángulo relleno
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawTriangle(Vector2 vector21, Vector2 vector22, Vector2 vector23, UIFramework.Color arrowColor)
        {
            if (!_initialized) return;

            OpenGLInterop.glBegin(GL_TRIANGLES);
            OpenGLInterop.glColor4f(arrowColor.R, arrowColor.G, arrowColor.B, arrowColor.A);

            // Primer vértice
            OpenGLInterop.glVertex2f(vector21.X, vector21.Y);
            // Segundo vértice
            OpenGLInterop.glVertex2f(vector22.X, vector22.Y);
            // Tercer vértice
            OpenGLInterop.glVertex2f(vector23.X, vector23.Y);

            OpenGLInterop.glEnd();
        }

        private static int CalculateOptimalSegments(float radius)
        {
            // Fórmula mejorada: más segmentos para círculos más grandes
            // pero con una progresión más suave
            const int minSegments = 12;   // Mínimo para que se vea circular
            const int maxSegments = 64;   // Máximo para performance

            // Cálculo basado en el perímetro aproximado
            // Más radio = más perímetro = más segmentos necesarios
            int segments = (int)(radius * 0.5f + 16f);

            // Aplicar límites
            return Math.Clamp(segments, minSegments, maxSegments);
        }

        // Alternativa más simple y efectiva:
        private static int CalculateOptimalSegmentsSimple(float radius)
        {
            // Fórmula simple pero efectiva
            if (radius < 10f) return 12;
            if (radius < 20f) return 16;
            if (radius < 40f) return 24;
            if (radius < 80f) return 32;
            return 48;
        }

        /// <summary>
        /// Dibuja texto básico (requiere implementación de font atlas)
        /// </summary>
        internal static void DrawText(uint fontId, string text, Vector2 position, Color color, float scale = 1f)
        {
            if (!_initialized || string.IsNullOrEmpty(text)) return;
            FontRenderer.DrawText(fontId, text, position.X, position.Y, scale, color);
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