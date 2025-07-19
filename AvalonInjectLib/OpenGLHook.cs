using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    internal static unsafe class OpenGLHook
    {
        #region FIELDS
        private static volatile bool _initialized = false;
        private static volatile bool _hookInstalled = false;
        private static volatile bool _disposing = false;
        private static int _screenWidth = 1920;
        private static int _screenHeight = 1080;

        // Thread-safe callback execution without rate limiting
        private static volatile Action? _renderCallback = null;
        private static readonly object _callbackLock = new();

        // Hook management
        private static IntPtr _originalWglSwapBuffers = IntPtr.Zero;
        private static IntPtr _hookTrampoline = IntPtr.Zero;
        private static byte[] _originalBytes = Array.Empty<byte>();

        // OpenGL state preservation (minimal for performance)
        private static readonly int[] _viewport = new int[4];
        private static readonly float[] _savedColor = new float[4];
        private static bool _savedBlend, _savedDepthTest, _savedLighting, _savedTexture2D;
        private static int _savedBlendSrc, _savedBlendDst, _savedMatrixMode;

        // Context validation
        private static IntPtr _lastValidContext = IntPtr.Zero;
        private static volatile bool _contextValid = false;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool WglSwapBuffersDelegate(IntPtr hdc);

        private static WglSwapBuffersDelegate? _trampolineDelegate = null;
        #endregion

        #region PROPERTIES
        internal static bool Initialized => _initialized;
        internal static bool HookInstalled => _hookInstalled;
        internal static int ScreenWidth => _screenWidth;
        internal static int ScreenHeight => _screenHeight;
        internal static bool HasRenderCallback => _renderCallback != null;
        #endregion

        #region CALLBACK MANAGEMENT
        internal static void SetRenderCallback(Action? renderCallback)
        {
            lock (_callbackLock)
            {
                _renderCallback = renderCallback;
                Logger.Debug($"Render callback {(renderCallback != null ? "registered" : "cleared")}", "OpenGLHook");
            }
        }

        internal static void ClearRenderCallback()
        {
            lock (_callbackLock)
            {
                _renderCallback = null;
                Logger.Debug("Render callback cleared", "OpenGLHook");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ExecuteRenderCallback()
        {
            if (_disposing || _renderCallback == null) return;

            try
            {
                _renderCallback.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error($"Render callback error: {ex.Message}", "OpenGLHook");
            }
        }
        #endregion

        #region INITIALIZATION
        internal static bool Initialize()
        {
            if (_initialized) return true;

            try
            {
                Logger.Info("Initializing OpenGL Hook...", "OpenGLHook");

                if (!FindOpenGLFunction())
                {
                    Logger.Error("Failed to find OpenGL function", "OpenGLHook");
                    return false;
                }

                if (!CreateHookTrampoline())
                {
                    Logger.Error("Failed to create hook trampoline", "OpenGLHook");
                    return false;
                }

                if (!InstallHookPatch())
                {
                    Logger.Error("Failed to install hook patch", "OpenGLHook");
                    CleanupTrampoline();
                    return false;
                }

                _initialized = true;
                _hookInstalled = true;

                Logger.Info("OpenGL Hook initialized successfully!", "OpenGLHook");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Initialization failed: {ex.Message}", "OpenGLHook");
                CleanupTrampoline();
                return false;
            }
        }

        private static bool FindOpenGLFunction()
        {
            try
            {
                IntPtr hOpenGL = ProcessManager.Module(AvalonEngine.Instance.Process.ProcessId, "opengl32.dll");
                if (hOpenGL == IntPtr.Zero)
                {
                    Logger.Error("OpenGL32.dll not found in target process", "OpenGLHook");
                    return false;
                }

                _originalWglSwapBuffers = WinInterop.GetProcAddressEx(
                    AvalonEngine.Instance.Process.Handle,
                    hOpenGL,
                    "wglSwapBuffers"
                );

                if (_originalWglSwapBuffers == IntPtr.Zero)
                {
                    Logger.Error("wglSwapBuffers function not found", "OpenGLHook");
                    return false;
                }

                Logger.Debug($"Found wglSwapBuffers at: 0x{_originalWglSwapBuffers:X8}", "OpenGLHook");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error finding OpenGL function: {ex.Message}", "OpenGLHook");
                return false;
            }
        }

        private static bool CreateHookTrampoline()
        {
            try
            {
                bool is64Bit = IntPtr.Size == 8;
                int originalBytesSize = is64Bit ? 12 : 5;
                int trampolineSize = originalBytesSize + (is64Bit ? 14 : 5);

                // Save original bytes
                _originalBytes = new byte[originalBytesSize];
                Marshal.Copy(_originalWglSwapBuffers, _originalBytes, 0, originalBytesSize);

                // Allocate trampoline memory
                _hookTrampoline = Marshal.AllocHGlobal(trampolineSize);
                if (_hookTrampoline == IntPtr.Zero)
                {
                    Logger.Error("Failed to allocate trampoline memory", "OpenGLHook");
                    return false;
                }

                // Build trampoline code
                byte[] trampolineCode = BuildTrampolineCode(is64Bit, originalBytesSize);

                // Write trampoline code to memory
                Marshal.Copy(trampolineCode, 0, _hookTrampoline, trampolineCode.Length);

                // Make trampoline executable
                if (!WinInterop.VirtualProtect(_hookTrampoline, (uint)trampolineCode.Length, 0x40, out _))
                {
                    Logger.Error("Failed to make trampoline executable", "OpenGLHook");
                    return false;
                }

                // Create delegate for trampoline
                _trampolineDelegate = Marshal.GetDelegateForFunctionPointer<WglSwapBuffersDelegate>(_hookTrampoline);

                Logger.Debug($"Trampoline created at: 0x{_hookTrampoline:X8}", "OpenGLHook");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Trampoline creation failed: {ex.Message}", "OpenGLHook");
                return false;
            }
        }

        private static byte[] BuildTrampolineCode(bool is64Bit, int originalBytesSize)
        {
            if (is64Bit)
            {
                // x64 trampoline: original bytes + jump to continuation
                byte[] trampolineCode = new byte[originalBytesSize + 14];

                // Copy original bytes
                Buffer.BlockCopy(_originalBytes, 0, trampolineCode, 0, originalBytesSize);

                // MOV RAX, address
                trampolineCode[originalBytesSize] = 0x48;
                trampolineCode[originalBytesSize + 1] = 0xB8;

                long continuationAddress = _originalWglSwapBuffers.ToInt64() + originalBytesSize;
                Buffer.BlockCopy(BitConverter.GetBytes(continuationAddress), 0, trampolineCode, originalBytesSize + 2, 8);

                // JMP RAX
                trampolineCode[originalBytesSize + 10] = 0xFF;
                trampolineCode[originalBytesSize + 11] = 0xE0;

                return trampolineCode;
            }
            else
            {
                // x86 trampoline: original bytes + relative jump
                byte[] trampolineCode = new byte[originalBytesSize + 5];

                // Copy original bytes
                Buffer.BlockCopy(_originalBytes, 0, trampolineCode, 0, originalBytesSize);

                // JMP relative
                trampolineCode[originalBytesSize] = 0xE9;

                int continuationAddress = _originalWglSwapBuffers.ToInt32() + originalBytesSize;
                int trampolineEnd = _hookTrampoline.ToInt32() + originalBytesSize + 5;
                int jmpOffset = continuationAddress - trampolineEnd;

                Buffer.BlockCopy(BitConverter.GetBytes(jmpOffset), 0, trampolineCode, originalBytesSize + 1, 4);

                return trampolineCode;
            }
        }

        private static bool InstallHookPatch()
        {
            try
            {
                byte[] hookCode = BuildHookCode();

                // Make original function writable
                if (!WinInterop.VirtualProtect(_originalWglSwapBuffers, (uint)hookCode.Length, 0x40, out uint oldProtect))
                {
                    Logger.Error("Failed to make original function writable", "OpenGLHook");
                    return false;
                }

                bool success = false;

                // Write hook code
                fixed (byte* pHookCode = hookCode)
                {
                    success = WinInterop.WriteProcessMemory(
                        AvalonEngine.Instance.Process.Handle,
                        _originalWglSwapBuffers,
                        pHookCode,
                        (uint)hookCode.Length,
                        out _
                    );
                }

                // Restore original protection
                WinInterop.VirtualProtect(_originalWglSwapBuffers, (uint)hookCode.Length, oldProtect, out _);

                if (success)
                {
                    // Flush instruction cache
                    WinInterop.FlushInstructionCache(
                        AvalonEngine.Instance.Process.Handle,
                        _originalWglSwapBuffers,
                        (uint)hookCode.Length
                    );

                    Logger.Debug("Hook patch installed successfully", "OpenGLHook");
                }
                else
                {
                    Logger.Error("Failed to write hook code", "OpenGLHook");
                }

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error($"Hook patch installation failed: {ex.Message}", "OpenGLHook");
                return false;
            }
        }

        private static byte[] BuildHookCode()
        {
            bool is64Bit = IntPtr.Size == 8;

            if (is64Bit)
            {
                // x64: MOV RAX, address; JMP RAX
                byte[] hookCode = new byte[12];
                hookCode[0] = 0x48; // REX.W
                hookCode[1] = 0xB8; // MOV RAX, imm64

                ulong hookAddress = (ulong)((delegate* unmanaged<IntPtr, bool>)&HookedWglSwapBuffers);
                Buffer.BlockCopy(BitConverter.GetBytes(hookAddress), 0, hookCode, 2, 8);

                hookCode[10] = 0xFF; // JMP RAX
                hookCode[11] = 0xE0;

                return hookCode;
            }
            else
            {
                // x86: JMP relative
                byte[] hookCode = new byte[5];
                hookCode[0] = 0xE9; // JMP rel32

                int hookAddress = (int)((delegate* unmanaged<IntPtr, bool>)&HookedWglSwapBuffers);
                int jmpOffset = hookAddress - (_originalWglSwapBuffers.ToInt32() + 5);

                Buffer.BlockCopy(BitConverter.GetBytes(jmpOffset), 0, hookCode, 1, 4);

                return hookCode;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CallOriginalFunction(IntPtr hdc)
        {
            if (_trampolineDelegate == null) return false;

            try
            {
                return _trampolineDelegate(hdc);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error calling original function: {ex.Message}", "OpenGLHook");
                return false;
            }
        }
        #endregion

        #region HOOK FUNCTION - OPTIMIZED FOR EVERY FRAME
        [UnmanagedCallersOnly]
        private static bool HookedWglSwapBuffers(IntPtr hdc)
        {
            // Fast context validation
            if (!ValidateOpenGLContext(hdc))
            {
                return CallOriginalFunction(hdc);
            }

            // EXECUTE RENDER CALLBACK EVERY FRAME - No rate limiting
            if (_renderCallback != null && !_disposing)
            {
                RenderOverlay(hdc);
            }

            // CRITICAL: Call original function after rendering overlay
            return CallOriginalFunction(hdc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ValidateOpenGLContext(IntPtr hdc)
        {
            if (hdc == IntPtr.Zero) return false;

            try
            {
                IntPtr currentContext = OpenGLInterop.wglGetCurrentContext();
                bool isValid = currentContext != IntPtr.Zero;

                if (isValid && currentContext != _lastValidContext)
                {
                    _lastValidContext = currentContext;
                    _contextValid = true;
                }

                return isValid;
            }
            catch
            {
                _contextValid = false;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RenderOverlay(IntPtr hdc)
        {
            if (!_contextValid) return;

            try
            {
                // Save minimal OpenGL state
                if (!SaveOpenGLState()) return;

                // Setup for 2D overlay rendering
                SetupOverlayRendering();

                // EXECUTE CALLBACK EVERY FRAME
                ExecuteRenderCallback();

                // Ensure overlay is drawn
                OpenGLInterop.glFlush();
            }
            catch (Exception ex)
            {
                Logger.Error($"Overlay rendering error: {ex.Message}", "OpenGLHook");
            }
            finally
            {
                // ALWAYS restore OpenGL state
                RestoreOpenGLState();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetupOverlayRendering()
        {
            // Get current viewport
            fixed (int* viewportPtr = _viewport)
            {
                OpenGLInterop.glGetIntegerv(OpenGLInterop.GL_VIEWPORT, viewportPtr);
            }

            _screenWidth = _viewport[2];
            _screenHeight = _viewport[3];

            // Setup projection matrix for 2D overlay
            OpenGLInterop.glMatrixMode(OpenGLInterop.GL_PROJECTION);
            OpenGLInterop.glPushMatrix();
            OpenGLInterop.glLoadIdentity();
            OpenGLInterop.glOrtho(0, _screenWidth, _screenHeight, 0, -1, 1);

            // Setup modelview matrix
            OpenGLInterop.glMatrixMode(OpenGLInterop.GL_MODELVIEW);
            OpenGLInterop.glPushMatrix();
            OpenGLInterop.glLoadIdentity();

            // Configure render states for transparent overlay
            OpenGLInterop.glDisable(OpenGLInterop.GL_DEPTH_TEST);
            OpenGLInterop.glDisable(OpenGLInterop.GL_LIGHTING);
            OpenGLInterop.glDisable(OpenGLInterop.GL_TEXTURE_2D);
            OpenGLInterop.glDisable(OpenGLInterop.GL_CULL_FACE);

            // Enable blending for transparency
            OpenGLInterop.glEnable(OpenGLInterop.GL_BLEND);
            OpenGLInterop.glBlendFunc(OpenGLInterop.GL_SRC_ALPHA, OpenGLInterop.GL_ONE_MINUS_SRC_ALPHA);

            // Set default color
            OpenGLInterop.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
        }
        #endregion

        #region OPENGL STATE MANAGEMENT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool SaveOpenGLState()
        {
            try
            {
                // Save essential states only for performance
                OpenGLInterop.glGetIntegerv(OpenGLInterop.GL_MATRIX_MODE, out _savedMatrixMode);

                _savedBlend = OpenGLInterop.glIsEnabled(OpenGLInterop.GL_BLEND);
                _savedDepthTest = OpenGLInterop.glIsEnabled(OpenGLInterop.GL_DEPTH_TEST);
                _savedLighting = OpenGLInterop.glIsEnabled(OpenGLInterop.GL_LIGHTING);
                _savedTexture2D = OpenGLInterop.glIsEnabled(OpenGLInterop.GL_TEXTURE_2D);

                if (_savedBlend)
                {
                    OpenGLInterop.glGetIntegerv(OpenGLInterop.GL_BLEND_SRC, out _savedBlendSrc);
                    OpenGLInterop.glGetIntegerv(OpenGLInterop.GL_BLEND_DST, out _savedBlendDst);
                }

                // Save current color
                fixed (float* colorPtr = _savedColor)
                {
                    OpenGLInterop.glGetFloatv(OpenGLInterop.GL_CURRENT_COLOR, colorPtr);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save OpenGL state: {ex.Message}", "OpenGLHook");
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RestoreOpenGLState()
        {
            try
            {
                // Restore matrices
                OpenGLInterop.glMatrixMode(OpenGLInterop.GL_PROJECTION);
                OpenGLInterop.glPopMatrix();
                OpenGLInterop.glMatrixMode(OpenGLInterop.GL_MODELVIEW);
                OpenGLInterop.glPopMatrix();

                // Restore matrix mode
                OpenGLInterop.glMatrixMode((uint)_savedMatrixMode);

                // Restore render states
                if (_savedBlend)
                {
                    OpenGLInterop.glEnable(OpenGLInterop.GL_BLEND);
                    OpenGLInterop.glBlendFunc((uint)_savedBlendSrc, (uint)_savedBlendDst);
                }
                else
                {
                    OpenGLInterop.glDisable(OpenGLInterop.GL_BLEND);
                }

                if (_savedDepthTest)
                    OpenGLInterop.glEnable(OpenGLInterop.GL_DEPTH_TEST);
                else
                    OpenGLInterop.glDisable(OpenGLInterop.GL_DEPTH_TEST);

                if (_savedLighting)
                    OpenGLInterop.glEnable(OpenGLInterop.GL_LIGHTING);
                else
                    OpenGLInterop.glDisable(OpenGLInterop.GL_LIGHTING);

                if (_savedTexture2D)
                    OpenGLInterop.glEnable(OpenGLInterop.GL_TEXTURE_2D);
                else
                    OpenGLInterop.glDisable(OpenGLInterop.GL_TEXTURE_2D);

                // Restore color
                OpenGLInterop.glColor4f(_savedColor[0], _savedColor[1], _savedColor[2], _savedColor[3]);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to restore OpenGL state: {ex.Message}", "OpenGLHook");
            }
        }
        #endregion

        #region DRAWING FUNCTIONS - 2D OVERLAY
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
        {
            if (!_initialized || _disposing) return;

            OpenGLInterop.glLineWidth(thickness);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            OpenGLInterop.glBegin(OpenGLInterop.GL_LINES);
            OpenGLInterop.glVertex2f(start.X, start.Y);
            OpenGLInterop.glVertex2f(end.X, end.Y);
            OpenGLInterop.glEnd();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawBox(float x, float y, float width, float height, float thickness, Color color)
        {
            if (!_initialized || _disposing) return;

            OpenGLInterop.glLineWidth(thickness);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            OpenGLInterop.glBegin(OpenGLInterop.GL_LINE_LOOP);
            OpenGLInterop.glVertex2f(x, y);
            OpenGLInterop.glVertex2f(x + width, y);
            OpenGLInterop.glVertex2f(x + width, y + height);
            OpenGLInterop.glVertex2f(x, y + height);
            OpenGLInterop.glEnd();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawFilledBox(float x, float y, float width, float height, Color color)
        {
            if (!_initialized || _disposing) return;

            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            OpenGLInterop.glBegin(OpenGLInterop.GL_QUADS);
            OpenGLInterop.glVertex2f(x, y);
            OpenGLInterop.glVertex2f(x + width, y);
            OpenGLInterop.glVertex2f(x + width, y + height);
            OpenGLInterop.glVertex2f(x, y + height);
            OpenGLInterop.glEnd();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawFilledCircle(Vector2 center, float radius, Color color)
        {
            if (!_initialized || _disposing || radius <= 0) return;

            // Adaptive segments for performance optimization
            int segments = Math.Max(8, Math.Min(48, (int)(radius * 0.8f)));
            const float TWO_PI = 6.28318530718f;

            OpenGLInterop.glBegin(OpenGLInterop.GL_TRIANGLE_FAN);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);

            // Center vertex
            OpenGLInterop.glVertex2f(center.X, center.Y);

            // Circle vertices
            float angleStep = TWO_PI / segments;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * angleStep;
                OpenGLInterop.glVertex2f(
                    center.X + MathF.Cos(angle) * radius,
                    center.Y + MathF.Sin(angle) * radius
                );
            }
            OpenGLInterop.glEnd();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawCircle(Vector2 center, float radius, float thickness, Color color)
        {
            if (!_initialized || _disposing || radius <= 0) return;

            // Adaptive segments for performance optimization
            int segments = Math.Max(8, Math.Min(48, (int)(radius * 0.8f)));
            const float TWO_PI = 6.28318530718f;

            OpenGLInterop.glLineWidth(thickness);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            OpenGLInterop.glBegin(OpenGLInterop.GL_LINE_LOOP);

            float angleStep = TWO_PI / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
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
            if (!_initialized || _disposing) return;

            OpenGLInterop.glBegin(OpenGLInterop.GL_TRIANGLES);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);
            OpenGLInterop.glVertex2f(v1.X, v1.Y);
            OpenGLInterop.glVertex2f(v2.X, v2.Y);
            OpenGLInterop.glVertex2f(v3.X, v3.Y);
            OpenGLInterop.glEnd();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawText(uint fontId, string text, Vector2 position, Color color, float scale = 1f)
        {
            if (!_initialized || _disposing || string.IsNullOrEmpty(text)) return;
            FontRenderer.DrawText(fontId, text, position.X, position.Y, scale, color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DrawPolygon(Vector2[] vertices, Color color)
        {
            if (!_initialized || _disposing || vertices == null || vertices.Length < 3) return;

            OpenGLInterop.glBegin(OpenGLInterop.GL_POLYGON);
            OpenGLInterop.glColor4f(color.R, color.G, color.B, color.A);

            for (int i = 0; i < vertices.Length; i++)
            {
                OpenGLInterop.glVertex2f(vertices[i].X, vertices[i].Y);
            }

            OpenGLInterop.glEnd();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ClearOverlay()
        {
            if (!_initialized || _disposing) return;
            // Note: Avoid using glClear to prevent interference with the game
            // If needed, draw a transparent rectangle instead
        }
        #endregion

        #region CLEANUP
        private static void CleanupTrampoline()
        {
            if (_hookTrampoline != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_hookTrampoline);
                _hookTrampoline = IntPtr.Zero;
            }
            _trampolineDelegate = null;
        }

        internal static void Cleanup()
        {
            if (!_initialized) return;

            _disposing = true;
            Logger.Info("Cleaning up OpenGL Hook...", "OpenGLHook");

            try
            {
                // Clear callback
                lock (_callbackLock)
                {
                    _renderCallback = null;
                }

                // Restore original function
                if (_hookInstalled && _originalWglSwapBuffers != IntPtr.Zero && _originalBytes.Length > 0)
                {
                    if (WinInterop.VirtualProtect(_originalWglSwapBuffers, (uint)_originalBytes.Length, 0x40, out uint oldProtect))
                    {
                        fixed (byte* pOriginalBytes = _originalBytes)
                        {
                            bool restored = WinInterop.WriteProcessMemory(
                                AvalonEngine.Instance.Process.Handle,
                                _originalWglSwapBuffers,
                                pOriginalBytes,
                                (uint)_originalBytes.Length,
                                out _
                            );

                            if (restored)
                            {
                                WinInterop.FlushInstructionCache(
                                    AvalonEngine.Instance.Process.Handle,
                                    _originalWglSwapBuffers,
                                    (uint)_originalBytes.Length
                                );
                                Logger.Debug("Original function restored", "OpenGLHook");
                            }
                            else
                            {
                                Logger.Error("Failed to restore original function", "OpenGLHook");
                            }
                        }

                        WinInterop.VirtualProtect(_originalWglSwapBuffers, (uint)_originalBytes.Length, oldProtect, out _);
                    }
                }

                // Cleanup trampoline
                CleanupTrampoline();

                Logger.Info("OpenGL Hook cleanup completed", "OpenGLHook");
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
                _contextValid = false;
                _lastValidContext = IntPtr.Zero;
            }
        }
        #endregion
    }
}