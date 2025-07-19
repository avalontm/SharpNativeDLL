using System;
using System.Runtime.InteropServices;

namespace AvalonInjectLib
{
    internal unsafe static class OpenGLInterop
    {
        #region OpenGL Constants
        // Rendering modes
        internal const uint GL_POINTS = 0x0000;
        internal const uint GL_LINES = 0x0001;
        internal const uint GL_LINE_LOOP = 0x0002;
        internal const uint GL_LINE_STRIP = 0x0003;
        internal const uint GL_TRIANGLES = 0x0004;
        internal const uint GL_TRIANGLE_STRIP = 0x0005;
        internal const uint GL_TRIANGLE_FAN = 0x0006;
        internal const uint GL_QUADS = 0x0007;
        internal const uint GL_QUAD_STRIP = 0x0008;
        internal const uint GL_POLYGON = 0x0009;

        // Boolean values
        internal const uint GL_FALSE = 0;
        internal const uint GL_TRUE = 1;

        // Blend values
        internal const uint GL_ZERO = 0;
        internal const uint GL_ONE = 1;
        internal const uint GL_SRC_COLOR = 0x0300;
        internal const uint GL_ONE_MINUS_SRC_COLOR = 0x0301;
        internal const uint GL_SRC_ALPHA = 0x0302;
        internal const uint GL_ONE_MINUS_SRC_ALPHA = 0x0303;
        internal const uint GL_DST_ALPHA = 0x0304;
        internal const uint GL_ONE_MINUS_DST_ALPHA = 0x0305;
        internal const uint GL_DST_COLOR = 0x0306;
        internal const uint GL_ONE_MINUS_DST_COLOR = 0x0307;
        internal const uint GL_SRC_ALPHA_SATURATE = 0x0308;

        // Matrix modes
        internal const uint GL_MODELVIEW = 0x1700;
        internal const uint GL_PROJECTION = 0x1701;
        internal const uint GL_TEXTURE = 0x1702;

        // Face modes
        internal const uint GL_FRONT = 0x0404;
        internal const uint GL_BACK = 0x0405;
        internal const uint GL_FRONT_AND_BACK = 0x0408;

        // ===== TEXTURE BINDING CONSTANTS =====

        internal const uint GL_TEXTURE_BINDING_2D = 0x8069;
        internal const uint GL_TEXTURE_BINDING_1D = 0x8068;
        internal const uint GL_TEXTURE_BINDING_3D = 0x806A;
        internal const uint GL_TEXTURE_BINDING_CUBE_MAP = 0x8514;

        // Texture targets
        internal const uint GL_TEXTURE_1D = 0x0DE0;
        internal const uint GL_TEXTURE_2D = 0x0DE1;
        internal const uint GL_TEXTURE_3D = 0x806F;
        internal const uint GL_TEXTURE_CUBE_MAP = 0x8513;
        internal const uint GL_TEXTURE_CUBE_MAP_POSITIVE_X = 0x8515;
        internal const uint GL_TEXTURE_CUBE_MAP_NEGATIVE_X = 0x8516;
        internal const uint GL_TEXTURE_CUBE_MAP_POSITIVE_Y = 0x8517;
        internal const uint GL_TEXTURE_CUBE_MAP_NEGATIVE_Y = 0x8518;
        internal const uint GL_TEXTURE_CUBE_MAP_POSITIVE_Z = 0x8519;
        internal const uint GL_TEXTURE_CUBE_MAP_NEGATIVE_Z = 0x851A;

        // Pixel formats
        internal const uint GL_COLOR_INDEX = 0x1900;
        internal const uint GL_STENCIL_INDEX = 0x1901;
        internal const uint GL_DEPTH_COMPONENT = 0x1902;
        internal const uint GL_RED = 0x1903;
        internal const uint GL_GREEN = 0x1904;
        internal const uint GL_BLUE = 0x1905;
        internal const uint GL_ALPHA = 0x1906;
        internal const uint GL_RGB = 0x1907;
        internal const uint GL_RGBA = 0x1908;
        internal const uint GL_LUMINANCE = 0x1909;
        internal const uint GL_LUMINANCE_ALPHA = 0x190A;
        internal const uint GL_BGR = 0x80E0;
        internal const uint GL_BGRA = 0x80E1;

        // Pixel types
        internal const uint GL_BITMAP = 0x1A00;
        internal const uint GL_BYTE = 0x1400;
        internal const uint GL_UNSIGNED_BYTE = 0x1401;
        internal const uint GL_SHORT = 0x1402;
        internal const uint GL_UNSIGNED_SHORT = 0x1403;
        internal const uint GL_INT = 0x1404;
        internal const uint GL_UNSIGNED_INT = 0x1405;
        internal const uint GL_FLOAT = 0x1406;
        internal const uint GL_DOUBLE = 0x140A;

        // Texture parameters
        internal const uint GL_TEXTURE_MIN_FILTER = 0x2801;
        internal const uint GL_TEXTURE_MAG_FILTER = 0x2800;
        internal const uint GL_TEXTURE_WRAP_S = 0x2802;
        internal const uint GL_TEXTURE_WRAP_T = 0x2803;
        internal const uint GL_TEXTURE_WRAP_R = 0x8072;
        internal const uint GL_TEXTURE_BORDER_COLOR = 0x1004;
        internal const uint GL_TEXTURE_PRIORITY = 0x8066;

        // Texture filtering
        internal const uint GL_NEAREST = 0x2600;
        internal const uint GL_LINEAR = 0x2601;
        internal const uint GL_NEAREST_MIPMAP_NEAREST = 0x2700;
        internal const uint GL_LINEAR_MIPMAP_NEAREST = 0x2701;
        internal const uint GL_NEAREST_MIPMAP_LINEAR = 0x2702;
        internal const uint GL_LINEAR_MIPMAP_LINEAR = 0x2703;

        // Texture wrapping
        internal const uint GL_CLAMP = 0x2900;
        internal const uint GL_REPEAT = 0x2901;
        internal const uint GL_CLAMP_TO_EDGE = 0x812F;
        internal const uint GL_CLAMP_TO_BORDER = 0x812D;
        internal const uint GL_MIRRORED_REPEAT = 0x8370;

        // Internal texture formats
        internal const uint GL_ALPHA4 = 0x803B;
        internal const uint GL_ALPHA8 = 0x803C;
        internal const uint GL_ALPHA12 = 0x803D;
        internal const uint GL_ALPHA16 = 0x803E;
        internal const uint GL_LUMINANCE4 = 0x803F;
        internal const uint GL_LUMINANCE8 = 0x8040;
        internal const uint GL_LUMINANCE12 = 0x8041;
        internal const uint GL_LUMINANCE16 = 0x8042;
        internal const uint GL_LUMINANCE4_ALPHA4 = 0x8043;
        internal const uint GL_LUMINANCE6_ALPHA2 = 0x8044;
        internal const uint GL_LUMINANCE8_ALPHA8 = 0x8045;
        internal const uint GL_LUMINANCE12_ALPHA4 = 0x8046;
        internal const uint GL_LUMINANCE12_ALPHA12 = 0x8047;
        internal const uint GL_LUMINANCE16_ALPHA16 = 0x8048;
        internal const uint GL_INTENSITY = 0x8049;
        internal const uint GL_INTENSITY4 = 0x804A;
        internal const uint GL_INTENSITY8 = 0x804B;
        internal const uint GL_INTENSITY12 = 0x804C;
        internal const uint GL_INTENSITY16 = 0x804D;
        internal const uint GL_R3_G3_B2 = 0x2A10;
        internal const uint GL_RGB4 = 0x804F;
        internal const uint GL_RGB5 = 0x8050;
        internal const uint GL_RGB8 = 0x8051;
        internal const uint GL_RGB10 = 0x8052;
        internal const uint GL_RGB12 = 0x8053;
        internal const uint GL_RGB16 = 0x8054;
        internal const uint GL_RGBA2 = 0x8055;
        internal const uint GL_RGBA4 = 0x8056;
        internal const uint GL_RGB5_A1 = 0x8057;
        internal const uint GL_RGBA8 = 0x8058;
        internal const uint GL_RGB10_A2 = 0x8059;
        internal const uint GL_RGBA12 = 0x805A;
        internal const uint GL_RGBA16 = 0x805B;

        // Capabilities
        internal const uint GL_ALPHA_TEST = 0x0BC0;
        internal const uint GL_AUTO_NORMAL = 0x0D80;
        internal const uint GL_BLEND = 0x0BE2;
        internal const uint GL_CLIP_PLANE0 = 0x3000;
        internal const uint GL_CLIP_PLANE1 = 0x3001;
        internal const uint GL_CLIP_PLANE2 = 0x3002;
        internal const uint GL_CLIP_PLANE3 = 0x3003;
        internal const uint GL_CLIP_PLANE4 = 0x3004;
        internal const uint GL_CLIP_PLANE5 = 0x3005;
        internal const uint GL_COLOR_LOGIC_OP = 0x0BF2;
        internal const uint GL_COLOR_MATERIAL = 0x0B57;
        internal const uint GL_CULL_FACE = 0x0B44;
        internal const uint GL_DEPTH_TEST = 0x0B71;
        internal const uint GL_DITHER = 0x0BD0;
        internal const uint GL_FOG = 0x0B60;
        internal const uint GL_INDEX_LOGIC_OP = 0x0BF1;
        internal const uint GL_LIGHT0 = 0x4000;
        internal const uint GL_LIGHT1 = 0x4001;
        internal const uint GL_LIGHT2 = 0x4002;
        internal const uint GL_LIGHT3 = 0x4003;
        internal const uint GL_LIGHT4 = 0x4004;
        internal const uint GL_LIGHT5 = 0x4005;
        internal const uint GL_LIGHT6 = 0x4006;
        internal const uint GL_LIGHT7 = 0x4007;
        internal const uint GL_LIGHTING = 0x0B50;
        internal const uint GL_LINE_SMOOTH = 0x0B20;
        internal const uint GL_LINE_STIPPLE = 0x0B24;
        internal const uint GL_MAP1_COLOR_4 = 0x0D90;
        internal const uint GL_MAP1_INDEX = 0x0D91;
        internal const uint GL_MAP1_NORMAL = 0x0D92;
        internal const uint GL_MAP1_TEXTURE_COORD_1 = 0x0D93;
        internal const uint GL_MAP1_TEXTURE_COORD_2 = 0x0D94;
        internal const uint GL_MAP1_TEXTURE_COORD_3 = 0x0D95;
        internal const uint GL_MAP1_TEXTURE_COORD_4 = 0x0D96;
        internal const uint GL_MAP1_VERTEX_3 = 0x0D97;
        internal const uint GL_MAP1_VERTEX_4 = 0x0D98;
        internal const uint GL_MAP2_COLOR_4 = 0x0DB0;
        internal const uint GL_MAP2_INDEX = 0x0DB1;
        internal const uint GL_MAP2_NORMAL = 0x0DB2;
        internal const uint GL_MAP2_TEXTURE_COORD_1 = 0x0DB3;
        internal const uint GL_MAP2_TEXTURE_COORD_2 = 0x0DB4;
        internal const uint GL_MAP2_TEXTURE_COORD_3 = 0x0DB5;
        internal const uint GL_MAP2_TEXTURE_COORD_4 = 0x0DB6;
        internal const uint GL_MAP2_VERTEX_3 = 0x0DB7;
        internal const uint GL_MAP2_VERTEX_4 = 0x0DB8;
        internal const uint GL_NORMALIZE = 0x0BA1;
        internal const uint GL_POINT_SMOOTH = 0x0B10;
        internal const uint GL_POLYGON_OFFSET_FILL = 0x8037;
        internal const uint GL_POLYGON_OFFSET_LINE = 0x2A02;
        internal const uint GL_POLYGON_OFFSET_POINT = 0x2A01;
        internal const uint GL_POLYGON_SMOOTH = 0x0B41;
        internal const uint GL_POLYGON_STIPPLE = 0x0B42;
        internal const uint GL_SCISSOR_TEST = 0x0C11;
        internal const uint GL_STENCIL_TEST = 0x0B90;
        internal const uint GL_TEXTURE_GEN_Q = 0x0C63;
        internal const uint GL_TEXTURE_GEN_R = 0x0C62;
        internal const uint GL_TEXTURE_GEN_S = 0x0C60;
        internal const uint GL_TEXTURE_GEN_T = 0x0C61;

        // Matrix and state parameters
        internal const uint GL_MATRIX_MODE = 0x0BA0;
        internal const uint GL_MODELVIEW_MATRIX = 0x0BA6;
        internal const uint GL_PROJECTION_MATRIX = 0x0BA7;
        internal const uint GL_TEXTURE_MATRIX = 0x0BA8;
        internal const uint GL_MODELVIEW_STACK_DEPTH = 0x0BA3;
        internal const uint GL_PROJECTION_STACK_DEPTH = 0x0BA4;
        internal const uint GL_TEXTURE_STACK_DEPTH = 0x0BA5;
        internal const uint GL_VIEWPORT = 0x0BA2;
        internal const uint GL_CURRENT_COLOR = 0x0B00;
        internal const uint GL_CURRENT_INDEX = 0x0B01;
        internal const uint GL_CURRENT_NORMAL = 0x0B02;
        internal const uint GL_CURRENT_TEXTURE_COORDS = 0x0B03;
        internal const uint GL_CURRENT_RASTER_COLOR = 0x0B04;
        internal const uint GL_CURRENT_RASTER_INDEX = 0x0B05;
        internal const uint GL_CURRENT_RASTER_TEXTURE_COORDS = 0x0B06;
        internal const uint GL_CURRENT_RASTER_POSITION = 0x0B07;
        internal const uint GL_CURRENT_RASTER_POSITION_VALID = 0x0B08;
        internal const uint GL_CURRENT_RASTER_DISTANCE = 0x0B09;
        internal const uint GL_POINT_SIZE = 0x0B11;
        internal const uint GL_POINT_SIZE_RANGE = 0x0B12;
        internal const uint GL_POINT_SIZE_GRANULARITY = 0x0B13;
        internal const uint GL_LINE_WIDTH = 0x0B21;
        internal const uint GL_LINE_WIDTH_RANGE = 0x0B22;
        internal const uint GL_LINE_WIDTH_GRANULARITY = 0x0B23;
        internal const uint GL_LINE_STIPPLE_PATTERN = 0x0B25;
        internal const uint GL_LINE_STIPPLE_REPEAT = 0x0B26;
        internal const uint GL_POLYGON_MODE = 0x0B40;
        internal const uint GL_EDGE_FLAG = 0x0B43;
        internal const uint GL_CULL_FACE_MODE = 0x0B45;
        internal const uint GL_FRONT_FACE = 0x0B46;

        // Error codes
        internal const uint GL_NO_ERROR = 0;
        internal const uint GL_INVALID_ENUM = 0x0500;
        internal const uint GL_INVALID_VALUE = 0x0501;
        internal const uint GL_INVALID_OPERATION = 0x0502;
        internal const uint GL_STACK_OVERFLOW = 0x0503;
        internal const uint GL_STACK_UNDERFLOW = 0x0504;
        internal const uint GL_OUT_OF_MEMORY = 0x0505;

        // Comparison functions
        internal const uint GL_NEVER = 0x0200;
        internal const uint GL_LESS = 0x0201;
        internal const uint GL_EQUAL = 0x0202;
        internal const uint GL_LEQUAL = 0x0203;
        internal const uint GL_GREATER = 0x0204;
        internal const uint GL_NOTEQUAL = 0x0205;
        internal const uint GL_GEQUAL = 0x0206;
        internal const uint GL_ALWAYS = 0x0207;

        // Clear buffer bits
        internal const uint GL_COLOR_BUFFER_BIT = 0x00004000;
        internal const uint GL_DEPTH_BUFFER_BIT = 0x00000100;
        internal const uint GL_ACCUM_BUFFER_BIT = 0x00000200;
        internal const uint GL_STENCIL_BUFFER_BIT = 0x00000400;

        // Polygon modes
        internal const uint GL_POINT = 0x1B00;
        internal const uint GL_LINE = 0x1B01;
        internal const uint GL_FILL = 0x1B02;

        // Shading models
        internal const uint GL_FLAT = 0x1D00;
        internal const uint GL_SMOOTH = 0x1D01;

        // Logic operations
        internal const uint GL_CLEAR = 0x1500;
        internal const uint GL_AND = 0x1501;
        internal const uint GL_AND_REVERSE = 0x1502;
        internal const uint GL_COPY = 0x1503;
        internal const uint GL_AND_INVERTED = 0x1504;
        internal const uint GL_NOOP = 0x1505;
        internal const uint GL_XOR = 0x1506;
        internal const uint GL_OR = 0x1507;
        internal const uint GL_NOR = 0x1508;
        internal const uint GL_EQUIV = 0x1509;
        internal const uint GL_INVERT = 0x150A;
        internal const uint GL_OR_REVERSE = 0x150B;
        internal const uint GL_COPY_INVERTED = 0x150C;
        internal const uint GL_OR_INVERTED = 0x150D;
        internal const uint GL_NAND = 0x150E;
        internal const uint GL_SET = 0x150F;

        // Stencil operations
        internal const uint GL_KEEP = 0x1E00;
        internal const uint GL_REPLACE = 0x1E01;
        internal const uint GL_INCR = 0x1E02;
        internal const uint GL_DECR = 0x1E03;

        // String names
        internal const uint GL_VENDOR = 0x1F00;
        internal const uint GL_RENDERER = 0x1F01;
        internal const uint GL_VERSION = 0x1F02;
        internal const uint GL_EXTENSIONS = 0x1F03;

        // Hints
        internal const uint GL_PERSPECTIVE_CORRECTION_HINT = 0x0C50;
        internal const uint GL_POINT_SMOOTH_HINT = 0x0C51;
        internal const uint GL_LINE_SMOOTH_HINT = 0x0C52;
        internal const uint GL_POLYGON_SMOOTH_HINT = 0x0C53;
        internal const uint GL_FOG_HINT = 0x0C54;
        internal const uint GL_DONT_CARE = 0x1100;
        internal const uint GL_FASTEST = 0x1101;
        internal const uint GL_NICEST = 0x1102;

        // Lighting model parameters
        internal const uint GL_LIGHT_MODEL_AMBIENT = 0x0B53;
        internal const uint GL_LIGHT_MODEL_LOCAL_VIEWER = 0x0B51;
        internal const uint GL_LIGHT_MODEL_TWO_SIDE = 0x0B52;

        // Light source parameters
        internal const uint GL_AMBIENT = 0x1200;
        internal const uint GL_DIFFUSE = 0x1201;
        internal const uint GL_SPECULAR = 0x1202;
        internal const uint GL_POSITION = 0x1203;
        internal const uint GL_SPOT_DIRECTION = 0x1204;
        internal const uint GL_SPOT_EXPONENT = 0x1205;
        internal const uint GL_SPOT_CUTOFF = 0x1206;
        internal const uint GL_CONSTANT_ATTENUATION = 0x1207;
        internal const uint GL_LINEAR_ATTENUATION = 0x1208;
        internal const uint GL_QUADRATIC_ATTENUATION = 0x1209;

        // Material parameters
        internal const uint GL_EMISSION = 0x1600;
        internal const uint GL_SHININESS = 0x1601;
        internal const uint GL_AMBIENT_AND_DIFFUSE = 0x1602;
        internal const uint GL_COLOR_INDEXES = 0x1603;

        // Fog parameters
        internal const uint GL_FOG_INDEX = 0x0B61;
        internal const uint GL_FOG_DENSITY = 0x0B62;
        internal const uint GL_FOG_START = 0x0B63;
        internal const uint GL_FOG_END = 0x0B64;
        internal const uint GL_FOG_MODE = 0x0B65;
        internal const uint GL_FOG_COLOR = 0x0B66;
        internal const uint GL_EXP = 0x0800;
        internal const uint GL_EXP2 = 0x0801;

        // Accumulation buffer
        internal const uint GL_ACCUM = 0x0100;
        internal const uint GL_LOAD = 0x0101;
        internal const uint GL_RETURN = 0x0102;
        internal const uint GL_MULT = 0x0103;
        internal const uint GL_ADD = 0x0104;

        // Alpha testing
        internal const uint GL_ALPHA_TEST_FUNC = 0x0BC1;
        internal const uint GL_ALPHA_TEST_REF = 0x0BC2;

        // Blending
        internal const uint GL_BLEND_DST = 0x0BE0;
        internal const uint GL_BLEND_SRC = 0x0BE1;

        // Depth buffer
        internal const uint GL_DEPTH_FUNC = 0x0B74;
        internal const uint GL_DEPTH_WRITEMASK = 0x0B72;
        internal const uint GL_DEPTH_CLEAR_VALUE = 0x0B73;
        internal const uint GL_DEPTH_RANGE = 0x0B70;

        // Stencil buffer
        internal const uint GL_STENCIL_FUNC = 0x0B92;
        internal const uint GL_STENCIL_VALUE_MASK = 0x0B93;
        internal const uint GL_STENCIL_FAIL = 0x0B94;
        internal const uint GL_STENCIL_PASS_DEPTH_PASS = 0x0B96;
        internal const uint GL_STENCIL_PASS_DEPTH_FAIL = 0x0B95;
        internal const uint GL_STENCIL_REF = 0x0B97;
        internal const uint GL_STENCIL_WRITEMASK = 0x0B98;
        internal const uint GL_STENCIL_CLEAR_VALUE = 0x0B91;
        #endregion

        #region Basic Rendering Functions
        [DllImport("opengl32.dll")]
        internal static extern void glBegin(uint mode);

        [DllImport("opengl32.dll")]
        internal static extern void glEnd();

        [DllImport("opengl32.dll")]
        internal static extern void glVertex2f(float x, float y);

        [DllImport("opengl32.dll")]
        internal static extern void glVertex3f(float x, float y, float z);

        [DllImport("opengl32.dll")]
        internal static extern void glVertex2d(double x, double y);

        [DllImport("opengl32.dll")]
        internal static extern void glVertex3d(double x, double y, double z);

        [DllImport("opengl32.dll")]
        internal static extern void glColor3f(float r, float g, float b);

        [DllImport("opengl32.dll")]
        internal static extern void glColor4f(float r, float g, float b, float a);

        [DllImport("opengl32.dll")]
        internal static extern void glLineWidth(float width);

        [DllImport("opengl32.dll")]
        internal static extern void glPointSize(float size);
        #endregion

        #region State Management
        [DllImport("opengl32.dll")]
        internal static extern void glEnable(uint cap);

        [DllImport("opengl32.dll")]
        internal static extern void glDisable(uint cap);

        [DllImport("opengl32.dll")]
        internal static extern void glBlendFunc(uint sfactor, uint dfactor);

        [DllImport("opengl32.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool glIsEnabled(uint cap);

        [DllImport("opengl32.dll")]
        internal static extern uint glGetError();

        [DllImport("opengl32.dll")]
        internal static extern void glAlphaFunc(uint func, float reference);
        #endregion

        #region Matrix Management
        [DllImport("opengl32.dll")]
        internal static extern void glMatrixMode(uint mode);

        [DllImport("opengl32.dll")]
        internal static extern void glLoadIdentity();

        [DllImport("opengl32.dll")]
        internal static extern void glPushMatrix();

        [DllImport("opengl32.dll")]
        internal static extern void glPopMatrix();

        [DllImport("opengl32.dll")]
        internal static extern void glOrtho(double left, double right, double bottom, double top, double zNear, double zFar);

        [DllImport("opengl32.dll")]
        internal static extern void glFrustum(double left, double right, double bottom, double top, double zNear, double zFar);

        [DllImport("opengl32.dll")]
        internal static extern void glPolygonMode(uint face, uint mode);

        [DllImport("opengl32.dll")]
        internal static extern void glLoadMatrixf(float[] m);

        [DllImport("opengl32.dll")]
        internal static extern void glLoadMatrixd(double[] m);

        [DllImport("opengl32.dll")]
        internal static extern void glMultMatrixf(float[] m);

        [DllImport("opengl32.dll")]
        internal static extern void glMultMatrixd(double[] m);

        [DllImport("opengl32.dll")]
        internal static extern void glTranslatef(float x, float y, float z);

        [DllImport("opengl32.dll")]
        internal static extern void glRotatef(float angle, float x, float y, float z);

        [DllImport("opengl32.dll")]
        internal static extern void glScalef(float x, float y, float z);
        #endregion

        #region State Queries
        [DllImport("opengl32.dll")]
        internal static extern void glGetIntegerv(uint pname, int* parameters);

        [DllImport("opengl32.dll")]
        internal static extern void glGetIntegerv(uint pname, out int parameter);

        [DllImport("opengl32.dll")]
        internal static extern void glGetFloatv(uint pname, float* parameters);

        [DllImport("opengl32.dll")]
        internal static extern void glGetFloatv(uint pname, out float parameter);

        [DllImport("opengl32.dll")]
        internal static extern void glGetDoublev(uint pname, double* parameters);

        [DllImport("opengl32.dll")]
        internal static extern void glGetDoublev(uint pname, out double parameter);

        [DllImport("opengl32.dll")]
        internal static extern sbyte* glGetString(uint name);
        #endregion

        #region Texture Management
        [DllImport("opengl32.dll")]
        internal static extern void glGenTextures(int n, uint* textures);

        [DllImport("opengl32.dll")]
        internal static extern void glGenTextures(int n, uint[] textures);

        [DllImport("opengl32.dll")]
        internal static extern void glBindTexture(uint target, uint texture);

        [DllImport("opengl32.dll")]
        internal static extern void glTexImage2D(uint target, int level, int internalformat, int width, int height, int border, uint format, uint type, IntPtr pixels);

        [DllImport("opengl32.dll")]
        internal static extern void glTexParameteri(uint target, uint pname, int param);

        [DllImport("opengl32.dll")]
        internal static extern void glTexParameterf(uint target, uint pname, float param);

        [DllImport("opengl32.dll")]
        internal static extern void glTexCoord2f(float s, float t);

        [DllImport("opengl32.dll")]
        internal static extern void glTexCoord2d(double s, double t);

        [DllImport("opengl32.dll")]
        internal static extern void glDeleteTextures(int n, uint* textures);

        [DllImport("opengl32.dll")]
        internal static extern void glDeleteTextures(int n, uint[] textures);

        [DllImport("opengl32.dll")]
        internal static extern void glDeleteTextures(int n, ref uint texture);
        #endregion

        #region Context and Window Management
        [DllImport("opengl32.dll")]
        internal static extern IntPtr wglGetCurrentContext();

        [DllImport("opengl32.dll")]
        internal static extern IntPtr wglGetCurrentDC();

        [DllImport("opengl32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool wglMakeCurrent(IntPtr hdc, IntPtr hglrc);

        [DllImport("opengl32.dll")]
        internal static extern IntPtr wglCreateContext(IntPtr hdc);

        [DllImport("opengl32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool wglDeleteContext(IntPtr hglrc);

        [DllImport("opengl32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool wglShareLists(IntPtr hglrc1, IntPtr hglrc2);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);
        #endregion

        #region Pixel Format
        [DllImport("gdi32.dll")]
        internal static extern int ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR ppfd);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetPixelFormat(IntPtr hdc, int format, ref PIXELFORMATDESCRIPTOR ppfd);

        [DllImport("gdi32.dll")]
        internal static extern int DescribePixelFormat(IntPtr hdc, int pixelFormat, uint bytes, ref PIXELFORMATDESCRIPTOR ppfd);

        [DllImport("opengl32.dll", EntryPoint = "glIsTexture")]
        internal static extern bool glIsTexture(uint texture);

        [DllImport("opengl32.dll", EntryPoint = "glIsTexture")]
        internal static extern byte glIsTextureByte(uint texture);

        // Pixel Format Descriptor flags
        internal const uint PFD_DOUBLEBUFFER = 0x00000001;
        internal const uint PFD_STEREO = 0x00000002;
        internal const uint PFD_DRAW_TO_WINDOW = 0x00000004;
        internal const uint PFD_DRAW_TO_BITMAP = 0x00000008;
        internal const uint PFD_SUPPORT_GDI = 0x00000010;
        internal const uint PFD_SUPPORT_OPENGL = 0x00000020;
        internal const uint PFD_GENERIC_FORMAT = 0x00000040;
        internal const uint PFD_NEED_PALETTE = 0x00000080;
        internal const uint PFD_NEED_SYSTEM_PALETTE = 0x00000100;
        internal const uint PFD_SWAP_EXCHANGE = 0x00000200;
        internal const uint PFD_SWAP_COPY = 0x00000400;
        internal const uint PFD_SWAP_LAYER_BUFFERS = 0x00000800;
        internal const uint PFD_GENERIC_ACCELERATED = 0x00001000;

        // Pixel types
        internal const byte PFD_TYPE_RGBA = 0;
        internal const byte PFD_TYPE_COLORINDEX = 1;

        // Layer types
        internal const byte PFD_MAIN_PLANE = 0;
        internal const byte PFD_OVERLAY_PLANE = 1;
        internal const byte PFD_UNDERLAY_PLANE = 255;

        [StructLayout(LayoutKind.Sequential)]
        internal struct PIXELFORMATDESCRIPTOR
        {
            internal ushort nSize;
            internal ushort nVersion;
            internal uint dwFlags;
            internal byte iPixelType;
            internal byte cColorBits;
            internal byte cRedBits;
            internal byte cRedShift;
            internal byte cGreenBits;
            internal byte cGreenShift;
            internal byte cBlueBits;
            internal byte cBlueShift;
            internal byte cAlphaBits;
            internal byte cAlphaShift;
            internal byte cAccumBits;
            internal byte cAccumRedBits;
            internal byte cAccumGreenBits;
            internal byte cAccumBlueBits;
            internal byte cAccumAlphaBits;
            internal byte cDepthBits;
            internal byte cStencilBits;
            internal byte cAuxBuffers;
            internal byte iLayerType;
            internal byte bReserved;
            internal uint dwLayerMask;
            internal uint dwVisibleMask;
            internal uint dwDamageMask;
        }
        #endregion

        #region Utility Functions
        [DllImport("opengl32.dll")]
        internal static extern void glFlush();

        [DllImport("opengl32.dll")]
        internal static extern void glFinish();

        [DllImport("opengl32.dll")]
        internal static extern void glClear(uint mask);

        [DllImport("opengl32.dll")]
        internal static extern void glClearColor(float red, float green, float blue, float alpha);

        [DllImport("opengl32.dll")]
        internal static extern void glClearDepth(double depth);

        [DllImport("opengl32.dll")]
        internal static extern void glViewport(int x, int y, int width, int height);

        [DllImport("opengl32.dll")]
        internal static extern void glDrawArrays(uint mode, int first, int count);

        [DllImport("opengl32.dll")]
        internal static extern void glDrawElements(uint mode, int count, uint type, IntPtr indices);

        [DllImport("opengl32.dll")]
        internal static extern void glReadPixels(int x, int y, int width, int height, uint format, uint type, IntPtr pixels);
        #endregion

        #region Normal and Lighting Functions
        [DllImport("opengl32.dll")]
        internal static extern void glNormal3f(float nx, float ny, float nz);

        [DllImport("opengl32.dll")]
        internal static extern void glNormal3d(double nx, double ny, double nz);
        #endregion
    }
}