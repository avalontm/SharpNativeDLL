using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    internal static unsafe class OpenGLHook
    {
        #region CONSTANTS
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
        private const int GL_CULL_FACE = 0x0B44;
        private const int GL_LIGHTING = 0x0B50;
        private const int GL_TEXTURE_2D = 0x0DE1;
        private const int GL_ALPHA_TEST = 0x0BC0;
        private const int GL_GREATER = 0x0204;
        #endregion

        #region FIELDS
        private static volatile bool _initialized = false;
        private static volatile bool _hookInstalled = false;
        private static volatile bool _disposing = false;
        private static int _screenWidth = 1920;
        private static int _screenHeight = 1080;

        // NUEVO: Control de estado de recarga
        private static volatile bool _isReloading = false;
        private static volatile bool _skipNextFrames = false;
        private static int _skipFrameCount = 0;
        private static readonly object _reloadStateLock = new();

        // Callbacks thread-safe
        private static readonly ConcurrentBag<Action> _renderCallbacks = new();
        private static readonly object _callbackLock = new();
        private static Action[] _cachedCallbacks = Array.Empty<Action>();
        private static volatile bool _callbacksCacheDirty = true;

        // Hook state
        private static IntPtr _originalWglSwapBuffers = IntPtr.Zero;
        private static IntPtr _hookTrampoline = IntPtr.Zero;
        private static byte[] _originalBytes = Array.Empty<byte>();

        // OpenGL state preservation
        private static readonly OpenGLState _savedState = new();
        private static readonly int[] _viewportBuffer = new int[4];

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool WglSwapBuffersDelegate(IntPtr hdc);
        #endregion

        #region RELOAD STATE MANAGEMENT
        /// <summary>
        /// Indica al hook que se está iniciando una recarga de scripts
        /// </summary>
        internal static void BeginReload()
        {
            lock (_reloadStateLock)
            {
                _isReloading = true;
                _skipNextFrames = true;
                _skipFrameCount = 0;
            }
        }

        /// <summary>
        /// Indica al hook que la recarga de scripts ha terminado
        /// </summary>
        internal static void EndReload()
        {
            lock (_reloadStateLock)
            {
                _isReloading = false;
                // Seguir saltando algunos frames para estabilizar
                _skipNextFrames = true;
                _skipFrameCount = 0;
            }
        }

        /// <summary>
        /// Verifica si está seguro renderizar callbacks
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSafeToRender()
        {
            lock (_reloadStateLock)
            {
                // Si está recargando, no renderizar
                if (_isReloading) return false;

                // Si acabamos de terminar la recarga, saltar algunos frames
                if (_skipNextFrames)
                {
                    _skipFrameCount++;
                    if (_skipFrameCount >= 3) // Saltar 3 frames después de la recarga
                    {
                        _skipNextFrames = false;
                        _skipFrameCount = 0;
                    }
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Propiedad para verificar el estado de recarga desde otros componentes
        /// </summary>
        internal static bool IsReloading => _isReloading;
        #endregion

        #region OPENGL STATE MANAGEMENT
        private sealed class OpenGLState
        {
            public int MatrixMode;
            public bool DepthTest;
            public bool Blend;
            public bool CullFace;
            public bool Lighting;
            public bool Texture2D;
            public bool AlphaTest;
            public readonly float[] ProjectionMatrix = new float[16];
            public readonly float[] ModelViewMatrix = new float[16];
            public readonly int[] Viewport = new int[4];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SaveState()
            {
                OpenGLInterop.glGetIntegerv(OpenGLInterop.GL_MATRIX_MODE, out MatrixMode);
                OpenGLInterop.glGetIntegerv(GL_VIEWPORT, Viewport);

                DepthTest = OpenGLInterop.glIsEnabled(GL_DEPTH_TEST);
                Blend = OpenGLInterop.glIsEnabled(GL_BLEND);
                CullFace = OpenGLInterop.glIsEnabled(GL_CULL_FACE);
                Lighting = OpenGLInterop.glIsEnabled(GL_LIGHTING);
                Texture2D = OpenGLInterop.glIsEnabled(GL_TEXTURE_2D);
                AlphaTest = OpenGLInterop.glIsEnabled(GL_ALPHA_TEST);

                OpenGLInterop.glGetFloatv(OpenGLInterop.GL_PROJECTION_MATRIX, ProjectionMatrix);
                OpenGLInterop.glGetFloatv(OpenGLInterop.GL_MODELVIEW_MATRIX, ModelViewMatrix);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RestoreState()
            {
                // Restaurar matrices
                OpenGLInterop.glMatrixMode(GL_PROJECTION);
                OpenGLInterop.glLoadMatrixf(ProjectionMatrix);
                OpenGLInterop.glMatrixMode(GL_MODELVIEW);
                OpenGLInterop.glLoadMatrixf(ModelViewMatrix);
                OpenGLInterop.glMatrixMode((uint)MatrixMode);

                // Restaurar viewport
                OpenGLInterop.glViewport(Viewport[0], Viewport[1], Viewport[2], Viewport[3]);

                // Restaurar estados
                SetGLState(GL_DEPTH_TEST, DepthTest);
                SetGLState(GL_BLEND, Blend);
                SetGLState(GL_CULL_FACE, CullFace);
                SetGLState(GL_LIGHTING, Lighting);
                SetGLState(GL_TEXTURE_2D, Texture2D);
                SetGLState(GL_ALPHA_TEST, AlphaTest);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void SetGLState(int cap, bool enabled)
            {
                if (enabled)
                    OpenGLInterop.glEnable((uint)cap);
                else
                    OpenGLInterop.glDisable((uint)cap);
            }
        }
        #endregion

        #region PROPERTIES
        internal static bool Initialized => _initialized;
        internal static int ScreenWidth => _screenWidth;
        internal static int ScreenHeight => _screenHeight;
        #endregion

        #region P/INVOKE
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, uint dwSize);
        #endregion

        #region CALLBACK MANAGEMENT
        internal static void AddRenderCallback(Action renderCallback)
        {
            if (renderCallback == null || _disposing) return;

            _renderCallbacks.Add(renderCallback);
            _callbacksCacheDirty = true;
        }

        internal static void RemoveRenderCallback(Action renderCallback)
        {
            if (renderCallback == null) return;
            _callbacksCacheDirty = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ExecuteRenderCallbacks()
        {
            if (_disposing || _renderCallbacks.IsEmpty) return;

            // CRÍTICO: Verificar si es seguro renderizar
            if (!IsSafeToRender()) return;

            // Actualizar cache si es necesario
            if (_callbacksCacheDirty)
            {
                lock (_callbackLock)
                {
                    if (_callbacksCacheDirty)
                    {
                        _cachedCallbacks = _renderCallbacks.ToArray();
                        _callbacksCacheDirty = false;
                    }
                }
            }

            var callbacks = _cachedCallbacks;
            if (callbacks.Length == 0) return;

            // Ejecutar callbacks con manejo de errores
            foreach (var callback in callbacks)
            {
                try
                {
                    // Verificar nuevamente antes de cada callback
                    if (!IsSafeToRender()) break;

                    callback();
                }
                catch (Exception ex)
                {
                    // Log error in debug mode only
#if DEBUG
                    Logger.Error($"Callback error: {ex.Message}", "OpenGLHook");
#endif
                }
            }
        }
        #endregion

        #region INITIALIZATION
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

                // Obtener dirección de wglSwapBuffers
                IntPtr hOpenGL = WinInterop.GetModuleBaseEx(processId, "opengl32.dll");
                if (hOpenGL == IntPtr.Zero)
                {
                    Logger.Error("Failed to find opengl32.dll", "OpenGLHook");
                    return false;
                }

                _originalWglSwapBuffers = WinInterop.GetProcAddressEx(hProcess, hOpenGL, "wglSwapBuffers");
                if (_originalWglSwapBuffers == IntPtr.Zero)
                {
                    Logger.Error("Failed to find wglSwapBuffers", "OpenGLHook");
                    return false;
                }

                // Crear trampolín e instalar hook
                if (!CreateTrampoline() || !InstallHook())
                {
                    Logger.Error("Failed to create trampoline or install hook", "OpenGLHook");
                    return false;
                }

                _initialized = true;
                _hookInstalled = true;
                Logger.Debug("OpenGL Hook installed successfully!", "OpenGLHook");
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
                int patchSize = is64Bit ? 12 : 5;

                // Leer bytes originales
                _originalBytes = new byte[patchSize];
                Marshal.Copy(_originalWglSwapBuffers, _originalBytes, 0, patchSize);

                // Asignar memoria ejecutable
                _hookTrampoline = Marshal.AllocHGlobal(patchSize + (is64Bit ? 12 : 5));

                // Construir trampolín
                byte[] trampolineCode;
                if (is64Bit)
                {
                    trampolineCode = new byte[patchSize + 12];
                    Buffer.BlockCopy(_originalBytes, 0, trampolineCode, 0, patchSize);

                    // mov rax, &original+patchSize
                    trampolineCode[patchSize] = 0x48;
                    trampolineCode[patchSize + 1] = 0xB8;
                    long targetAddr = _originalWglSwapBuffers.ToInt64() + patchSize;
                    Buffer.BlockCopy(BitConverter.GetBytes(targetAddr), 0, trampolineCode, patchSize + 2, 8);

                    // jmp rax
                    trampolineCode[patchSize + 10] = 0xFF;
                    trampolineCode[patchSize + 11] = 0xE0;
                }
                else
                {
                    trampolineCode = new byte[patchSize + 5];
                    Buffer.BlockCopy(_originalBytes, 0, trampolineCode, 0, patchSize);

                    // jmp rel32
                    trampolineCode[patchSize] = 0xE9;
                    int targetAddr = _originalWglSwapBuffers.ToInt32() + patchSize;
                    int jmpOffset = targetAddr - (_hookTrampoline.ToInt32() + patchSize + 5);
                    Buffer.BlockCopy(BitConverter.GetBytes(jmpOffset), 0, trampolineCode, patchSize + 1, 4);
                }

                Marshal.Copy(trampolineCode, 0, _hookTrampoline, trampolineCode.Length);

                // Marcar como ejecutable
                return VirtualProtect(_hookTrampoline, (uint)trampolineCode.Length, 0x40, out _);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create trampoline: {ex.Message}", "OpenGLHook");
                return false;
            }
        }

        private static bool InstallHook()
        {
            try
            {
                byte[] hookCode;
                if (IntPtr.Size == 8)
                {
                    hookCode = new byte[] {
                        0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // mov rax, address
                        0xFF, 0xE0 // jmp rax
                    };
                    fixed (byte* p = hookCode)
                    {
                        *(ulong*)(p + 2) = (ulong)((delegate* unmanaged<IntPtr, bool>)&HookedWglSwapBuffers);
                    }
                }
                else
                {
                    hookCode = new byte[] { 0xE9, 0x00, 0x00, 0x00, 0x00 }; // jmp rel32
                    fixed (byte* p = hookCode)
                    {
                        *(uint*)(p + 1) = (uint)((int)((delegate* unmanaged<IntPtr, bool>)&HookedWglSwapBuffers) - ((int)_originalWglSwapBuffers + 5));
                    }
                }

                // Instalar hook atómicamente
                if (!VirtualProtect(_originalWglSwapBuffers, (uint)hookCode.Length, 0x40, out uint oldProtect))
                    return false;

                var success = WriteProcessMemory(Process.GetCurrentProcess().Handle, _originalWglSwapBuffers, hookCode, (uint)hookCode.Length, out _);
                VirtualProtect(_originalWglSwapBuffers, (uint)hookCode.Length, oldProtect, out _);

                if (success)
                    FlushInstructionCache(Process.GetCurrentProcess().Handle, _originalWglSwapBuffers, (uint)hookCode.Length);

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to install hook: {ex.Message}", "OpenGLHook");
                return false;
            }
        }
        #endregion

        #region HOOK FUNCTION
        [UnmanagedCallersOnly]
        private static bool HookedWglSwapBuffers(IntPtr hdc)
        {
            if (_disposing || hdc == IntPtr.Zero)
                return CallOriginalWglSwapBuffers(hdc);

            try
            {
                // Verificar contexto válido antes de cualquier operación
                if (!IsValidOpenGLContext(hdc))
                    return CallOriginalWglSwapBuffers(hdc);

                // CRÍTICO: Usar GlobalSync para prevenir condiciones de carrera
                if (!GlobalSync.TryBeginRender(out var renderScope))
                    return CallOriginalWglSwapBuffers(hdc);

                using (renderScope)
                {
                    // Renderizar overlay solo si es seguro
                    if (IsSafeToRender())
                    {
                        RenderOverlayStable();
                    }
                }

                // DESPUÉS del renderizado, hacer swap una sola vez
                return CallOriginalWglSwapBuffers(hdc);
            }
            catch (Exception ex)
            {
#if DEBUG
                Logger.Error($"Hook error: {ex.Message}", "OpenGLHook");
#endif
                return CallOriginalWglSwapBuffers(hdc);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CallOriginalWglSwapBuffers(IntPtr hdc)
        {
            if (_hookTrampoline == IntPtr.Zero) return false;

            var originalFunc = (delegate* unmanaged<IntPtr, bool>)_hookTrampoline.ToPointer();
            return originalFunc(hdc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidOpenGLContext(IntPtr hdc)
        {
            var currentContext = OpenGLInterop.wglGetCurrentContext();
            var currentDC = OpenGLInterop.wglGetCurrentDC();

            if (currentContext == IntPtr.Zero || currentDC == IntPtr.Zero)
                return false;

            // Si el DC no coincide, intentar hacer current
            if (currentDC != hdc)
            {
                return OpenGLInterop.wglMakeCurrent(hdc, currentContext);
            }

            return true;
        }

        private static void RenderOverlayStable()
        {
            // Guardar estado completo de OpenGL
            _savedState.SaveState();

            try
            {
                // Configurar para renderizado 2D estable
                ConfigureOverlayRendering();

                // Marcar contexto como disponible solo si es seguro
                if (IsSafeToRender())
                {
                    TextureRenderer.IsContexted = true;
                    FontRenderer.IsContexted = true;

                    // Procesar contenido pendiente (limitado por frame)
                    ProcessPendingContent();

                    // Ejecutar callbacks de renderizado
                    ExecuteRenderCallbacks();
                }

                // Asegurar que todo se renderice al back buffer
                OpenGLInterop.glFlush();
            }
            finally
            {
                // Restaurar estado completo
                _savedState.RestoreState();
                TextureRenderer.IsContexted = false;
                FontRenderer.IsContexted = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ConfigureOverlayRendering()
        {
            // Actualizar dimensiones de pantalla
            _screenWidth = _savedState.Viewport[2];
            _screenHeight = _savedState.Viewport[3];

            // Configurar matrices para overlay 2D
            OpenGLInterop.glMatrixMode(GL_PROJECTION);
            OpenGLInterop.glLoadIdentity();
            OpenGLInterop.glOrtho(0, _screenWidth, _screenHeight, 0, -1, 1);

            OpenGLInterop.glMatrixMode(GL_MODELVIEW);
            OpenGLInterop.glLoadIdentity();

            // Estados para overlay 2D estable
            OpenGLInterop.glDisable(GL_DEPTH_TEST);
            OpenGLInterop.glDisable(GL_CULL_FACE);
            OpenGLInterop.glDisable(GL_LIGHTING);
            OpenGLInterop.glDisable(GL_TEXTURE_2D);
            OpenGLInterop.glDisable(GL_ALPHA_TEST);

            // Configurar blending apropiado
            OpenGLInterop.glEnable(GL_BLEND);
            OpenGLInterop.glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);

            // Color base
            OpenGLInterop.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
        }

        private static void ProcessPendingContent()
        {
            // Solo procesar si es seguro
            if (!IsSafeToRender()) return;

            // Procesar texturas pendientes (máximo 3 por frame)
            int textureCount = 0;
            while (TextureRenderer.HasPendingTextures() && textureCount < 3 && IsSafeToRender())
            {
                TextureRenderer.ProcessPendingTextures();
                textureCount++;
            }

            // Procesar fuentes pendientes (máximo 2 por frame)
            int fontCount = 0;
            while (FontRenderer.HasPendingFonts() && fontCount < 2 && IsSafeToRender())
            {
                FontRenderer.ProcessPendingFonts();
                fontCount++;
            }
        }
        #endregion

        #region DRAWING FUNCTIONS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
        {
            if (!_initialized || !IsSafeToRender()) return;

            OpenGLInterop.glLineWidth(thickness);
            OpenGLInterop.glBegin(GL_LINES);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            OpenGLInterop.glVertex2f(start.X, start.Y);
            OpenGLInterop.glVertex2f(end.X, end.Y);
            OpenGLInterop.glEnd();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawBox(float x, float y, float width, float height, float thickness, Color color)
        {
            if (!_initialized || !IsSafeToRender()) return;

            OpenGLInterop.glLineWidth(thickness);
            OpenGLInterop.glBegin(GL_LINE_LOOP);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            OpenGLInterop.glVertex2f(x, y);
            OpenGLInterop.glVertex2f(x + width, y);
            OpenGLInterop.glVertex2f(x + width, y + height);
            OpenGLInterop.glVertex2f(x, y + height);
            OpenGLInterop.glEnd();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawFilledBox(float x, float y, float width, float height, Color color)
        {
            if (!_initialized || !IsSafeToRender()) return;

            OpenGLInterop.glBegin(GL_QUADS);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            OpenGLInterop.glVertex2f(x, y);
            OpenGLInterop.glVertex2f(x + width, y);
            OpenGLInterop.glVertex2f(x + width, y + height);
            OpenGLInterop.glVertex2f(x, y + height);
            OpenGLInterop.glEnd();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawFilledCircle(Vector2 center, float radius, Color color)
        {
            if (!_initialized || !IsSafeToRender() || radius <= 0) return;

            // Optimización: segmentos basados en tamaño
            int segments = radius switch
            {
                < 20f => 12,
                < 50f => 16,
                < 100f => 24,
                _ => 32
            };

            OpenGLInterop.glBegin(GL_TRIANGLE_FAN);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            OpenGLInterop.glVertex2f(center.X, center.Y);

            const float PI2 = 6.28318530718f;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * PI2;
                OpenGLInterop.glVertex2f(
                    center.X + MathF.Cos(angle) * radius,
                    center.Y + MathF.Sin(angle) * radius
                );
            }

            OpenGLInterop.glEnd();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawTriangle(Vector2 v1, Vector2 v2, Vector2 v3, Color color)
        {
            if (!_initialized || !IsSafeToRender()) return;

            OpenGLInterop.glBegin(GL_TRIANGLES);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            OpenGLInterop.glVertex2f(v1.X, v1.Y);
            OpenGLInterop.glVertex2f(v2.X, v2.Y);
            OpenGLInterop.glVertex2f(v3.X, v3.Y);
            OpenGLInterop.glEnd();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawText(uint fontId, string text, Vector2 position, Color color, float scale = 1f)
        {
            if (!_initialized || !IsSafeToRender() || string.IsNullOrEmpty(text)) return;
            FontRenderer.DrawText(fontId, text, position.X, position.Y, scale, color);
        }
        #endregion

        #region CLEANUP
        internal static void Cleanup()
        {
            if (!_initialized) return;

            _disposing = true;

            try
            {
                // Restaurar hook original
                if (_hookInstalled && _originalWglSwapBuffers != IntPtr.Zero && _originalBytes.Length > 0)
                {
                    VirtualProtect(_originalWglSwapBuffers, (uint)_originalBytes.Length, 0x40, out uint oldProtect);
                    WriteProcessMemory(Process.GetCurrentProcess().Handle, _originalWglSwapBuffers, _originalBytes, (uint)_originalBytes.Length, out _);
                    VirtualProtect(_originalWglSwapBuffers, (uint)_originalBytes.Length, oldProtect, out _);
                    FlushInstructionCache(Process.GetCurrentProcess().Handle, _originalWglSwapBuffers, (uint)_originalBytes.Length);
                }

                // Liberar memoria del trampolín
                if (_hookTrampoline != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_hookTrampoline);
                    _hookTrampoline = IntPtr.Zero;
                }

                Logger.Debug("OpenGL Hook cleaned up successfully", "OpenGLHook");
            }
            catch (Exception ex)
            {
                Logger.Error($"Cleanup error: {ex.Message}", "OpenGLHook");
            }
            finally
            {
                _initialized = false;
                _hookInstalled = false;
                _disposing = false;
            }
        }
        #endregion
    }
}