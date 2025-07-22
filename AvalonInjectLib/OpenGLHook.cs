using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib
{
    internal static unsafe class OpenGLHook
    {
        private static volatile bool _initialized = false;
        private static volatile bool _disposing = false;
        private static int _screenWidth = 1920;
        private static int _screenHeight = 1080;

        private static volatile Action? _renderCallback = null;
        private static readonly object _callbackLock = new();

        private static IntPtr _originalWglSwapBuffers = IntPtr.Zero;
        private static IntPtr _hookTrampoline = IntPtr.Zero;
        private static byte[] _originalBytes = Array.Empty<byte>();

        // OpenGL state preservation
        private static readonly int[] _viewport = new int[4];
        private static readonly float[] _savedColor = new float[4];
        private static bool _savedBlend, _savedDepthTest, _savedLighting, _savedTexture2D;
        private static int _savedBlendSrc, _savedBlendDst, _savedMatrixMode;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool WglSwapBuffersDelegate(IntPtr hdc);
        private static WglSwapBuffersDelegate? _trampolineDelegate = null;

        internal static bool Initialized => _initialized;
        internal static int ScreenWidth => _screenWidth;
        internal static int ScreenHeight => _screenHeight;

        internal static void SetRenderCallback(Action? renderCallback)
        {
            lock (_callbackLock)
            {
                _renderCallback = renderCallback;
            }
        }

        internal static bool Initialize()
        {
            if (_initialized) return true;

            try
            {
                IntPtr hOpenGL = WinInterop.GetModuleHandleA("opengl32.dll");
                if (hOpenGL == IntPtr.Zero) return false;

                _originalWglSwapBuffers = WinInterop.GetProcAddress(hOpenGL, "wglSwapBuffers");
                if (_originalWglSwapBuffers == IntPtr.Zero) return false;

                if (!CreateHookTrampoline() || !InstallHookPatch())
                {
                    CleanupTrampoline();
                    return false;
                }

                _initialized = true;
                return true;
            }
            catch
            {
                CleanupTrampoline();
                return false;
            }
        }

        private static bool CreateHookTrampoline()
        {
            bool is64Bit = IntPtr.Size == 8;
            int originalBytesSize = is64Bit ? 12 : 5;
            int trampolineSize = originalBytesSize + (is64Bit ? 14 : 5);

            _originalBytes = new byte[originalBytesSize];
            Marshal.Copy(_originalWglSwapBuffers, _originalBytes, 0, originalBytesSize);

            _hookTrampoline = WinInterop.VirtualAlloc(IntPtr.Zero, (uint)trampolineSize, 0x3000, 0x40);
            if (_hookTrampoline == IntPtr.Zero) return false;

            byte[] trampolineCode = BuildTrampolineCode(is64Bit, originalBytesSize);
            Marshal.Copy(trampolineCode, 0, _hookTrampoline, trampolineCode.Length);

            _trampolineDelegate = Marshal.GetDelegateForFunctionPointer<WglSwapBuffersDelegate>(_hookTrampoline);
            return true;
        }

        private static byte[] BuildTrampolineCode(bool is64Bit, int originalBytesSize)
        {
            if (is64Bit)
            {
                byte[] trampolineCode = new byte[originalBytesSize + 14];
                Buffer.BlockCopy(_originalBytes, 0, trampolineCode, 0, originalBytesSize);

                trampolineCode[originalBytesSize] = 0x48;     // MOV RAX, address
                trampolineCode[originalBytesSize + 1] = 0xB8;

                long continuationAddress = _originalWglSwapBuffers.ToInt64() + originalBytesSize;
                Buffer.BlockCopy(BitConverter.GetBytes(continuationAddress), 0, trampolineCode, originalBytesSize + 2, 8);

                trampolineCode[originalBytesSize + 10] = 0xFF; // JMP RAX
                trampolineCode[originalBytesSize + 11] = 0xE0;

                return trampolineCode;
            }
            else
            {
                byte[] trampolineCode = new byte[originalBytesSize + 5];
                Buffer.BlockCopy(_originalBytes, 0, trampolineCode, 0, originalBytesSize);

                trampolineCode[originalBytesSize] = 0xE9; // JMP relative

                int continuationAddress = _originalWglSwapBuffers.ToInt32() + originalBytesSize;
                int trampolineEnd = _hookTrampoline.ToInt32() + originalBytesSize + 5;
                int jmpOffset = continuationAddress - trampolineEnd;

                Buffer.BlockCopy(BitConverter.GetBytes(jmpOffset), 0, trampolineCode, originalBytesSize + 1, 4);
                return trampolineCode;
            }
        }

        private static bool InstallHookPatch()
        {
            byte[] hookCode = BuildHookCode();

            if (!WinInterop.VirtualProtect(_originalWglSwapBuffers, (uint)hookCode.Length, 0x40, out uint oldProtect))
                return false;

            Marshal.Copy(hookCode, 0, _originalWglSwapBuffers, hookCode.Length);
            WinInterop.VirtualProtect(_originalWglSwapBuffers, (uint)hookCode.Length, oldProtect, out _);
            return true;
        }

        private static byte[] BuildHookCode()
        {
            bool is64Bit = IntPtr.Size == 8;

            if (is64Bit)
            {
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
                byte[] hookCode = new byte[5];
                hookCode[0] = 0xE9; // JMP rel32

                int hookAddress = (int)((delegate* unmanaged<IntPtr, bool>)&HookedWglSwapBuffers);
                int jmpOffset = hookAddress - (_originalWglSwapBuffers.ToInt32() + 5);

                Buffer.BlockCopy(BitConverter.GetBytes(jmpOffset), 0, hookCode, 1, 4);
                return hookCode;
            }
        }

        [UnmanagedCallersOnly]
        private static bool HookedWglSwapBuffers(IntPtr hdc)
        {
            if (_renderCallback != null && !_disposing)
                RenderOverlay();

            return _trampolineDelegate?.Invoke(hdc) ?? false;
        }

        private static void RenderOverlay()
        {
            try
            {
                if (!SaveOpenGLState()) return;
                SetupOverlayRendering();
                _renderCallback?.Invoke();
                OpenGLInterop.glFlush();
            }
            catch { }
            finally
            {
                RestoreOpenGLState();
            }
        }

        private static void SetupOverlayRendering()
        {
            fixed (int* viewportPtr = _viewport)
            {
                OpenGLInterop.glGetIntegerv(OpenGLInterop.GL_VIEWPORT, viewportPtr);
            }

            _screenWidth = _viewport[2];
            _screenHeight = _viewport[3];

            OpenGLInterop.glMatrixMode(OpenGLInterop.GL_PROJECTION);
            OpenGLInterop.glPushMatrix();
            OpenGLInterop.glLoadIdentity();
            OpenGLInterop.glOrtho(0, _screenWidth, _screenHeight, 0, -1, 1);

            OpenGLInterop.glMatrixMode(OpenGLInterop.GL_MODELVIEW);
            OpenGLInterop.glPushMatrix();
            OpenGLInterop.glLoadIdentity();

            OpenGLInterop.glDisable(OpenGLInterop.GL_DEPTH_TEST);
            OpenGLInterop.glDisable(OpenGLInterop.GL_LIGHTING);
            OpenGLInterop.glDisable(OpenGLInterop.GL_TEXTURE_2D);
            OpenGLInterop.glDisable(OpenGLInterop.GL_CULL_FACE);

            OpenGLInterop.glEnable(OpenGLInterop.GL_BLEND);
            OpenGLInterop.glBlendFunc(OpenGLInterop.GL_SRC_ALPHA, OpenGLInterop.GL_ONE_MINUS_SRC_ALPHA);

            OpenGLInterop.glColor4f(1.0f, 1.0f, 1.0f, 1.0f);
        }

        private static bool SaveOpenGLState()
        {
            try
            {
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

                fixed (float* colorPtr = _savedColor)
                {
                    OpenGLInterop.glGetFloatv(OpenGLInterop.GL_CURRENT_COLOR, colorPtr);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void RestoreOpenGLState()
        {
            try
            {
                OpenGLInterop.glMatrixMode(OpenGLInterop.GL_PROJECTION);
                OpenGLInterop.glPopMatrix();
                OpenGLInterop.glMatrixMode(OpenGLInterop.GL_MODELVIEW);
                OpenGLInterop.glPopMatrix();

                OpenGLInterop.glMatrixMode((uint)_savedMatrixMode);

                if (_savedBlend)
                {
                    OpenGLInterop.glEnable(OpenGLInterop.GL_BLEND);
                    OpenGLInterop.glBlendFunc((uint)_savedBlendSrc, (uint)_savedBlendDst);
                }
                else
                    OpenGLInterop.glDisable(OpenGLInterop.GL_BLEND);

                if (_savedDepthTest) OpenGLInterop.glEnable(OpenGLInterop.GL_DEPTH_TEST);
                else OpenGLInterop.glDisable(OpenGLInterop.GL_DEPTH_TEST);

                if (_savedLighting) OpenGLInterop.glEnable(OpenGLInterop.GL_LIGHTING);
                else OpenGLInterop.glDisable(OpenGLInterop.GL_LIGHTING);

                if (_savedTexture2D) OpenGLInterop.glEnable(OpenGLInterop.GL_TEXTURE_2D);
                else OpenGLInterop.glDisable(OpenGLInterop.GL_TEXTURE_2D);

                OpenGLInterop.glColor4f(_savedColor[0], _savedColor[1], _savedColor[2], _savedColor[3]);
            }
            catch { }
        }

        // Funciones de dibujo simplificadas
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

        private static void CleanupTrampoline()
        {
            if (_hookTrampoline != IntPtr.Zero)
            {
                WinInterop.VirtualFree(_hookTrampoline, 0, 0x8000);
                _hookTrampoline = IntPtr.Zero;
            }
            _trampolineDelegate = null;
        }

        internal static void Cleanup()
        {
            if (!_initialized) return;

            _disposing = true;

            try
            {
                lock (_callbackLock)
                {
                    _renderCallback = null;
                }

                if (_originalWglSwapBuffers != IntPtr.Zero && _originalBytes.Length > 0)
                {
                    if (WinInterop.VirtualProtect(_originalWglSwapBuffers, (uint)_originalBytes.Length, 0x40, out uint oldProtect))
                    {
                        Marshal.Copy(_originalBytes, 0, _originalWglSwapBuffers, _originalBytes.Length);
                        WinInterop.VirtualProtect(_originalWglSwapBuffers, (uint)_originalBytes.Length, oldProtect, out _);
                    }
                }

                CleanupTrampoline();
            }
            catch { }
            finally
            {
                _initialized = false;
                _disposing = false;
            }
        }

    }
}