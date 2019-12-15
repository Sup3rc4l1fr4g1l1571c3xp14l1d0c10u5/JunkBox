using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace GLPainter
{

    using GLboolean = Byte;

    using GLbyte = SByte;
    using GLubyte = Byte;

    using GLshort = Int16;
    using GLushort = Int16;

    using GLint = Int32;
    using GLuint = UInt32;
    using GLsizei = Int32;
    using GLenum = UInt32;

    using GLint64 = Int64;
    using GLuint64 = UInt64;

    using GLsizeiptr = Int32;   // 32bit
    using GLintptr = Int32;   // 32bit

    using GLbitfield = UInt32;

    using GLfloat = Single;
    using GLclampf = Single; // range [0,1]

    using GLdouble = Double;
    using GLclampd = Double; // range [0,1]

    // GLFW Type
    using GLFWwindow = IntPtr;

    public static class GLEW
    {
        public const GLenum GLEW_OK = 0x00000000;
        public const GLenum GLEW_ERROR_NO_GL_VERSION = 0x00000001;
        public const GLenum GLEW_ERROR_GL_VERSION_10_ONLY = 0x00000002;
        public const GLenum GLEW_ERROR_GLX_VERSION_11_ONLY = 0x00000003;
        public const GLenum GLEW_ERROR_NO_GLX_DISPLAY = 0x00000004;

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);


        [DllImport("glew32.dll", EntryPoint = "glewInit", CallingConvention = CallingConvention.StdCall)]
        private extern static GLenum _glewInit();
        public static GLenum glewInit() {
            var ret = _glewInit();
            OpenGL.SetupGLEW(DllHandle);
            return ret;
        }

        private static IntPtr DllHandle = IntPtr.Zero;
        private static IntPtr glewExperimentalPtr = IntPtr.Zero;

        private static void OpenDll() {
            DllHandle = LoadLibrary("glew32.dll");
            if (DllHandle == IntPtr.Zero) {
                throw new Exception("glew32.dll is not found.");
            }
            glewExperimentalPtr = GetProcAddress(DllHandle, "glewExperimental");
            if (glewExperimentalPtr == IntPtr.Zero) {
                throw new Exception("glewExperimental is not found.");
            }
        }

        public static GLboolean glewExperimental {
            get { if (glewExperimentalPtr == IntPtr.Zero) { OpenDll(); } return Marshal.ReadByte(glewExperimentalPtr); }
            set { if (glewExperimentalPtr == IntPtr.Zero) { OpenDll(); } Marshal.WriteByte(glewExperimentalPtr, value); }
        }
    }
    public static partial class OpenGL
    {
        public const GLenum GL_FRAGMENT_SHADER = 0x8B30;
        public const GLenum GL_VERTEX_SHADER = 0x8B31;
        public const GLenum GL_COMPILE_STATUS = 0x8B81;
        public const GLenum GL_LINK_STATUS = 0x8B82;
        public const GLenum GL_INFO_LOG_LENGTH = 0x8B84;
        public const GLenum GL_ARRAY_BUFFER = 0x8892;
        public const GLenum GL_STREAM_DRAW = 0x88E0;
        public const GLenum GL_STREAM_READ = 0x88E1;

        public const GLenum GL_TEXTURE0 = 0x84C0;
        public const GLenum GL_TEXTURE1 = 0x84C1;
        public const GLenum GL_FRAMEBUFFER = 0x8D40;
        public const GLenum GL_COLOR_ATTACHMENT0 = 0x8CE0;
        public const GLenum GL_FUNC_ADD = 0x8006;
        public const GLenum GL_PIXEL_PACK_BUFFER = 0x88EB;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        public delegate GLuint PFNGLCREATESHADERPROC(GLenum shaderType);
        public static PFNGLCREATESHADERPROC glCreateShader { get; private set; }

        public delegate void PFNGLDELETESHADERPROC(GLuint shader);
        public static PFNGLDELETESHADERPROC glDeleteShader { get; private set; }

        public delegate GLuint PFNGLCREATEPROGRAMPROC();
        public static PFNGLCREATEPROGRAMPROC glCreateProgram { get; private set; }

        public delegate void PFNGLDELETEPROGRAMPROC(GLuint program);
        public static PFNGLDELETEPROGRAMPROC glDeleteProgram { get; private set; }

        public delegate void PFNGLSHADERSOURCEPROC(GLuint shader, GLsizei count, IntPtr[] @string, GLint[] length);
        public static PFNGLSHADERSOURCEPROC glShaderSource { get; private set; }

        public delegate void PFNGLCOMPILESHADERPROC(GLuint shader);
        public static PFNGLCOMPILESHADERPROC glCompileShader { get; private set; }

        public delegate void PFNGLGETSHADERIVPROC(GLuint shader, GLenum pname, out GLint param);
        public static PFNGLGETSHADERIVPROC glGetShaderiv { get; private set; }

        public delegate void PFNGLGETSHADERINFOLOGPROC(GLuint shader, GLsizei bufSize, out GLsizei length, byte[] infoLog);
        public static PFNGLGETSHADERINFOLOGPROC glGetShaderInfoLog { get; private set; }

        public delegate void PFNGLATTACHSHADERPROC(GLuint program, GLuint shader);
        public static PFNGLATTACHSHADERPROC glAttachShader { get; private set; }


        public delegate void PFNGLLINKPROGRAMPROC(GLuint program);
        public static PFNGLLINKPROGRAMPROC glLinkProgram { get; private set; }


        public delegate void PFNGLGETPROGRAMIVPROC(GLuint program, GLenum pname, out GLint param);
        public static PFNGLGETPROGRAMIVPROC glGetProgramiv { get; private set; }

        public delegate void PFNGLGETPROGRAMINFOLOGPROC(GLuint program, GLsizei bufSize, out GLsizei length, byte[] infoLog);
        public static PFNGLGETPROGRAMINFOLOGPROC glGetProgramInfoLog { get; private set; }

        public delegate void PFNGLUSEPROGRAMPROC(GLuint program);
        public static PFNGLUSEPROGRAMPROC glUseProgram { get; private set; }

        public delegate GLint PFNGLGETATTRIBLOCATIONPROC(GLuint program, string name);
        public static PFNGLGETATTRIBLOCATIONPROC glGetAttribLocation { get; private set; }

        public delegate void PFNGLCREATEBUFFERSPROC(GLsizei n, GLuint[] buffers);
        public static PFNGLCREATEBUFFERSPROC glCreateBuffers { get; private set; }

        public delegate void PFNGLBINDBUFFERPROC(GLenum target, GLuint buffer);
        public static PFNGLBINDBUFFERPROC glBindBuffer { get; private set; }

        public delegate void PFNGLBUFFERDATAPROC(GLenum target, GLsizeiptr size, byte[] data, GLenum usage);
        public static PFNGLBUFFERDATAPROC glBufferData { get; private set; }

        public delegate void PFNGLENABLEVERTEXATTRIBARRAYPROC(GLuint index);
        public static PFNGLENABLEVERTEXATTRIBARRAYPROC glEnableVertexAttribArray { get; private set; }

        public delegate void PFNGLVERTEXATTRIBPOINTERPROC(GLuint index, GLint size, GLenum type, GLboolean normalized, GLsizei stride, byte[] pointer);
        public static PFNGLVERTEXATTRIBPOINTERPROC glVertexAttribPointer { get; private set; }

        public delegate void PFNGLBUFFERSUBDATAPROC(GLenum target, GLintptr offset, GLsizeiptr size, byte[] data);
        public static PFNGLBUFFERSUBDATAPROC glBufferSubData { get; private set; }

        public delegate GLint PFNGLGETUNIFORMLOCATIONPROC(GLuint program, string name);
        public static PFNGLGETUNIFORMLOCATIONPROC glGetUniformLocation { get; private set; }

        public delegate void PFNGLUNIFORMMATRIX3FVPROC(GLint location, GLsizei count, GLboolean transpose, GLfloat[] value);
        public static PFNGLUNIFORMMATRIX3FVPROC glUniformMatrix3fv { get; private set; }

        public delegate void PFNGLUNIFORM2FPROC(GLint location, GLfloat v0, GLfloat v1);
        public static PFNGLUNIFORM2FPROC glUniform2f { get; private set; }

        public delegate void PFNGLACTIVETEXTUREPROC(GLenum texture);
        public static PFNGLACTIVETEXTUREPROC glActiveTexture { get; private set; }

        public delegate void PFNGLBINDTEXTURESPROC(GLuint first, GLsizei count, GLuint[] textures);
        public static PFNGLBINDTEXTURESPROC glBindTextures { get; private set; }

        public delegate void PFNGLUNIFORM1IPROC(GLint location, GLint v0);
        public static PFNGLUNIFORM1IPROC glUniform1i { get; private set; }

        public delegate void PFNGLBINDFRAMEBUFFERPROC(GLenum target, GLuint framebuffer);
        public static PFNGLBINDFRAMEBUFFERPROC glBindFramebuffer { get; private set; }

        public delegate void PFNGLUNIFORM1FVPROC(GLint location, GLsizei count, GLfloat[] value);
        public static PFNGLUNIFORM1FVPROC glUniform1fv { get; private set; }

        public delegate void PFNGLUNIFORM1FPROC(GLint location, GLfloat v0);
        public static PFNGLUNIFORM1FPROC glUniform1f { get; private set; }

        public delegate void PFNGLUNIFORM4FVPROC(GLint location, GLsizei count, GLfloat[] value);
        public static PFNGLUNIFORM4FVPROC glUniform4fv { get; private set; }

        public delegate void PFNGLBLENDEQUATIONPROC(GLenum mode);
        public static PFNGLBLENDEQUATIONPROC glBlendEquation { get; private set; }

        public delegate void PFNGLBLENDFUNCSEPARATEPROC(GLenum sfactorRGB, GLenum dfactorRGB, GLenum sfactorAlpha, GLenum dfactorAlpha);
        public static PFNGLBLENDFUNCSEPARATEPROC glBlendFuncSeparate { get; private set; }

        public delegate void PFNGLCREATETEXTURESPROC(GLenum target, GLsizei n, GLuint[] textures);
        public static PFNGLCREATETEXTURESPROC glCreateTextures { get; private set; }

        public delegate void PFNGLCREATEFRAMEBUFFERSPROC(GLsizei n, GLuint[] framebuffers);
        public static PFNGLCREATEFRAMEBUFFERSPROC glCreateFramebuffers { get; private set; }

        public delegate void PFNGLFRAMEBUFFERTEXTURE2DPROC(GLenum target, GLenum attachment, GLenum textarget, GLuint texture, GLint level);
        public static PFNGLFRAMEBUFFERTEXTURE2DPROC glFramebufferTexture2D { get; private set; }

        public delegate void PFNGLDELETEFRAMEBUFFERSPROC(GLsizei n, GLuint[] framebuffers);
        public static PFNGLDELETEFRAMEBUFFERSPROC glDeleteFramebuffers { get; private set; }

        public delegate void PFNGLGENVERTEXARRAYSPROC(GLsizei n, GLuint[] arrays);
        public static PFNGLGENVERTEXARRAYSPROC glGenVertexArrays { get; private set; }

        public delegate void PFNGLBINDVERTEXARRAYPROC(GLuint array);
        public static PFNGLBINDVERTEXARRAYPROC glBindVertexArray { get; private set; }

        public delegate void PFNGLGETBUFFERSUBDATAPROC(GLenum target, GLintptr offset, GLsizeiptr size, byte[] data);
        public static PFNGLGETBUFFERSUBDATAPROC glGetBufferSubData { get; private set; }

        public delegate void PFNGLDELETEBUFFERSPROC(GLsizei n, GLuint[] buffers);
        public static PFNGLDELETEBUFFERSPROC glDeleteBuffers { get; private set; }


        internal static void SetupGLEW(IntPtr DllHandle) {
            glCreateShader = Marshal.GetDelegateForFunctionPointer<PFNGLCREATESHADERPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewCreateShader")));
            glDeleteShader = Marshal.GetDelegateForFunctionPointer<PFNGLDELETESHADERPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewDeleteShader")));
            glCreateProgram = Marshal.GetDelegateForFunctionPointer<PFNGLCREATEPROGRAMPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewCreateProgram")));
            glDeleteProgram = Marshal.GetDelegateForFunctionPointer<PFNGLDELETEPROGRAMPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewDeleteProgram")));
            glShaderSource = Marshal.GetDelegateForFunctionPointer<PFNGLSHADERSOURCEPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewShaderSource")));
            glCompileShader = Marshal.GetDelegateForFunctionPointer<PFNGLCOMPILESHADERPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewCompileShader")));
            glGetShaderiv = Marshal.GetDelegateForFunctionPointer<PFNGLGETSHADERIVPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewGetShaderiv")));
            glGetShaderInfoLog = Marshal.GetDelegateForFunctionPointer<PFNGLGETSHADERINFOLOGPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewGetShaderInfoLog")));
            glAttachShader = Marshal.GetDelegateForFunctionPointer<PFNGLATTACHSHADERPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewAttachShader")));
            glLinkProgram = Marshal.GetDelegateForFunctionPointer<PFNGLLINKPROGRAMPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewLinkProgram")));
            glGetProgramiv = Marshal.GetDelegateForFunctionPointer<PFNGLGETPROGRAMIVPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewGetProgramiv")));
            glGetProgramInfoLog = Marshal.GetDelegateForFunctionPointer<PFNGLGETPROGRAMINFOLOGPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewGetProgramInfoLog")));
            glUseProgram = Marshal.GetDelegateForFunctionPointer<PFNGLUSEPROGRAMPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewUseProgram")));
            glGetAttribLocation = Marshal.GetDelegateForFunctionPointer<PFNGLGETATTRIBLOCATIONPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewGetAttribLocation")));
            glCreateBuffers = Marshal.GetDelegateForFunctionPointer<PFNGLCREATEBUFFERSPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewCreateBuffers")));

            glBindBuffer = Marshal.GetDelegateForFunctionPointer<PFNGLBINDBUFFERPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewBindBuffer")));
            glBufferData = Marshal.GetDelegateForFunctionPointer<PFNGLBUFFERDATAPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewBufferData")));
            glEnableVertexAttribArray = Marshal.GetDelegateForFunctionPointer<PFNGLENABLEVERTEXATTRIBARRAYPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewEnableVertexAttribArray")));
            glVertexAttribPointer = Marshal.GetDelegateForFunctionPointer<PFNGLVERTEXATTRIBPOINTERPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewVertexAttribPointer")));
            glBufferSubData = Marshal.GetDelegateForFunctionPointer<PFNGLBUFFERSUBDATAPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewBufferSubData")));
            glGetUniformLocation = Marshal.GetDelegateForFunctionPointer<PFNGLGETUNIFORMLOCATIONPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewGetUniformLocation")));

            glUniformMatrix3fv = Marshal.GetDelegateForFunctionPointer<PFNGLUNIFORMMATRIX3FVPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewUniformMatrix3fv")));
            glUniform2f = Marshal.GetDelegateForFunctionPointer<PFNGLUNIFORM2FPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewUniform2f")));
            glActiveTexture = Marshal.GetDelegateForFunctionPointer<PFNGLACTIVETEXTUREPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewActiveTexture")));
            glBindTextures = Marshal.GetDelegateForFunctionPointer<PFNGLBINDTEXTURESPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewBindTextures")));
            glUniform1i = Marshal.GetDelegateForFunctionPointer<PFNGLUNIFORM1IPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewUniform1i")));
            glBindFramebuffer = Marshal.GetDelegateForFunctionPointer<PFNGLBINDFRAMEBUFFERPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewBindFramebuffer")));
            glUniform1fv = Marshal.GetDelegateForFunctionPointer<PFNGLUNIFORM1FVPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewUniform1fv")));
            glUniform1f = Marshal.GetDelegateForFunctionPointer<PFNGLUNIFORM1FPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewUniform1f")));
            glUniform4fv = Marshal.GetDelegateForFunctionPointer<PFNGLUNIFORM4FVPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewUniform4fv")));
            glBlendEquation = Marshal.GetDelegateForFunctionPointer<PFNGLBLENDEQUATIONPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewBlendEquation")));
            glBlendFuncSeparate = Marshal.GetDelegateForFunctionPointer<PFNGLBLENDFUNCSEPARATEPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewBlendFuncSeparate")));
            glCreateTextures = Marshal.GetDelegateForFunctionPointer<PFNGLCREATETEXTURESPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewCreateTextures")));
            glCreateFramebuffers = Marshal.GetDelegateForFunctionPointer<PFNGLCREATEFRAMEBUFFERSPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewCreateFramebuffers")));
            glFramebufferTexture2D = Marshal.GetDelegateForFunctionPointer<PFNGLFRAMEBUFFERTEXTURE2DPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewFramebufferTexture2D")));
            glDeleteFramebuffers = Marshal.GetDelegateForFunctionPointer<PFNGLDELETEFRAMEBUFFERSPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewDeleteFramebuffers")));
            glGenVertexArrays = Marshal.GetDelegateForFunctionPointer<PFNGLGENVERTEXARRAYSPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewGenVertexArrays")));
            glBindVertexArray = Marshal.GetDelegateForFunctionPointer<PFNGLBINDVERTEXARRAYPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewBindVertexArray")));
            glGetBufferSubData = Marshal.GetDelegateForFunctionPointer<PFNGLGETBUFFERSUBDATAPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewGetBufferSubData")));
            glDeleteBuffers = Marshal.GetDelegateForFunctionPointer<PFNGLDELETEBUFFERSPROC>(Marshal.ReadIntPtr(GetProcAddress(DllHandle, "__glewDeleteBuffers")));
        }

        public static void glShaderSource_(GLuint shader, string @string) {
            var utf8bytes = Encoding.UTF8.GetBytes(@string);
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(utf8bytes.Length);
            Marshal.Copy(utf8bytes, 0, unmanagedPointer, utf8bytes.Length);
            glShaderSource(shader, 1, new[] { unmanagedPointer }, new GLint[] { utf8bytes.Length });
            Marshal.FreeHGlobal(unmanagedPointer);
        }

        public static GLint glGetShaderParameter(GLuint shader, GLenum pname) {
            GLint ret;
            glGetShaderiv(shader, pname, out ret);
            return ret;
        }

        public static string glGetShaderInfoLog_(GLuint shader) {
            var bufSize = glGetShaderParameter(shader, GL_INFO_LOG_LENGTH);
            byte[] infoLog = new byte[bufSize];
            GLsizei length;
            glGetShaderInfoLog(shader, bufSize, out length, infoLog);
            return Encoding.Default.GetString(infoLog);
        }
        public static GLint glGetProgramParameter(GLuint program, GLenum pname) {
            GLint ret;
            glGetProgramiv(program, pname, out ret);
            return ret;
        }

        public static string glGetProgramInfoLog_(GLuint program) {
            var bufSize = glGetProgramParameter(program, GL_INFO_LOG_LENGTH);
            byte[] infoLog = new byte[bufSize];
            GLsizei length;
            glGetProgramInfoLog(program, bufSize, out length, infoLog);
            return Encoding.Default.GetString(infoLog);
        }
        internal static GLuint[] glCreateBuffer_(GLsizei n) {
            GLuint[] buffers = new GLuint[n];
            OpenGL.glCreateBuffers(n, buffers);
            //OpenGL.glGenBuffers(n, buffers);
            return buffers;
        }
    }

    public static partial class OpenGL
    {
        [DllImport("opengl32.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr wglGetProcAddress(String function);

        public delegate void PFNGLGENBUFFERSPROC(GLsizei n, GLuint[] buffers);
        public static PFNGLGENBUFFERSPROC glGenBuffers { get; } = Marshal.GetDelegateForFunctionPointer<PFNGLGENBUFFERSPROC>(wglGetProcAddress("glGenBuffers"));
    }

    public static class GLFW
    {
        public const GLint GLFW_CONTEXT_VERSION_MAJOR = 0x00022002;
        public const GLint GLFW_CONTEXT_VERSION_MINOR = 0x00022003;
        public const GLint GLFW_OPENGL_FORWARD_COMPAT = 0x00022006;
        public const GLint GLFW_OPENGL_PROFILE = 0x00022008;

        public const GLint GLFW_OPENGL_CORE_PROFILE = 0x00032001;

        [DllImport("glfw3.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int glfwInit();

        [DllImport("glfw3.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void glfwTerminate();

        [DllImport("glfw3.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public extern static GLFWwindow glfwCreateWindow(int width, int height, string title, IntPtr /* GLFWmonitor* */ monitor, IntPtr /* GLFWwindow* */ share);

        [DllImport("glfw3.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void glfwMakeContextCurrent(IntPtr /* GLFWwindow* */ window);

        [DllImport("glfw3.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int glfwWindowShouldClose(IntPtr /* GLFWwindow* */ window);

        [DllImport("glfw3.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void glfwSwapBuffers(IntPtr /* GLFWwindow* */ window);

        [DllImport("glfw3.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void glfwWaitEvents();

        [DllImport("glfw3.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void glfwWindowHint(int target, int hint);

        [DllImport("glfw3.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void glfwGetFramebufferSize(IntPtr /* GLFWwindow* */ window, out int renderBufferWidth, out int renderBufferHeight);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void GLFWwindowsizefun(IntPtr /* GLFWwindow* */ window, int width, int height);

        [DllImport("glfw3.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static void glfwSetWindowSizeCallback(IntPtr /* GLFWwindow* */ window, GLFWwindowsizefun callback);
    }

    /**
     * OpenGL 1.0
     */
    public static partial class OpenGL
    {
        /* Boolean values */
        public const int GL_FALSE = 0;
        public const int GL_TRUE = 1;
        /* Data types */
        public const int GL_BYTE = 0x1400;
        public const int GL_UNSIGNED_BYTE = 0x1401;
        public const int GL_SHORT = 0x1402;
        public const int GL_UNSIGNED_SHORT = 0x1403;
        public const int GL_INT = 0x1404;
        public const int GL_UNSIGNED_INT = 0x1405;
        public const int GL_FLOAT = 0x1406;
        public const int GL_2_BYTES = 0x1407;
        public const int GL_3_BYTES = 0x1408;
        public const int GL_4_BYTES = 0x1409;
        public const int GL_DOUBLE = 0x140A;
        /* Primitives */
        public const int GL_POINTS = 0x0000;
        public const int GL_LINES = 0x0001;
        public const int GL_LINE_LOOP = 0x0002;
        public const int GL_LINE_STRIP = 0x0003;
        public const int GL_TRIANGLES = 0x0004;
        public const int GL_TRIANGLE_STRIP = 0x0005;
        public const int GL_TRIANGLE_FAN = 0x0006;
        public const int GL_QUADS = 0x0007;
        public const int GL_QUAD_STRIP = 0x0008;
        public const int GL_POLYGON = 0x0009;
        /* Vertex Arrays */
        public const int GL_VERTEX_ARRAY = 0x8074;
        public const int GL_NORMAL_ARRAY = 0x8075;
        public const int GL_COLOR_ARRAY = 0x8076;
        public const int GL_INDEX_ARRAY = 0x8077;
        public const int GL_TEXTURE_COORD_ARRAY = 0x8078;
        public const int GL_EDGE_FLAG_ARRAY = 0x8079;
        public const int GL_VERTEX_ARRAY_SIZE = 0x807A;
        public const int GL_VERTEX_ARRAY_TYPE = 0x807B;
        public const int GL_VERTEX_ARRAY_STRIDE = 0x807C;
        public const int GL_NORMAL_ARRAY_TYPE = 0x807E;
        public const int GL_NORMAL_ARRAY_STRIDE = 0x807F;
        public const int GL_COLOR_ARRAY_SIZE = 0x8081;
        public const int GL_COLOR_ARRAY_TYPE = 0x8082;
        public const int GL_COLOR_ARRAY_STRIDE = 0x8083;
        public const int GL_INDEX_ARRAY_TYPE = 0x8085;
        public const int GL_INDEX_ARRAY_STRIDE = 0x8086;
        public const int GL_TEXTURE_COORD_ARRAY_SIZE = 0x8088;
        public const int GL_TEXTURE_COORD_ARRAY_TYPE = 0x8089;
        public const int GL_TEXTURE_COORD_ARRAY_STRIDE = 0x808A;
        public const int GL_EDGE_FLAG_ARRAY_STRIDE = 0x808C;
        public const int GL_VERTEX_ARRAY_POINTER = 0x808E;
        public const int GL_NORMAL_ARRAY_POINTER = 0x808F;
        public const int GL_COLOR_ARRAY_POINTER = 0x8090;
        public const int GL_INDEX_ARRAY_POINTER = 0x8091;
        public const int GL_TEXTURE_COORD_ARRAY_POINTER = 0x8092;
        public const int GL_EDGE_FLAG_ARRAY_POINTER = 0x8093;
        public const int GL_V2F = 0x2A20;
        public const int GL_V3F = 0x2A21;
        public const int GL_C4UB_V2F = 0x2A22;
        public const int GL_C4UB_V3F = 0x2A23;
        public const int GL_C3F_V3F = 0x2A24;
        public const int GL_N3F_V3F = 0x2A25;
        public const int GL_C4F_N3F_V3F = 0x2A26;
        public const int GL_T2F_V3F = 0x2A27;
        public const int GL_T4F_V4F = 0x2A28;
        public const int GL_T2F_C4UB_V3F = 0x2A29;
        public const int GL_T2F_C3F_V3F = 0x2A2A;
        public const int GL_T2F_N3F_V3F = 0x2A2B;
        public const int GL_T2F_C4F_N3F_V3F = 0x2A2C;
        public const int GL_T4F_C4F_N3F_V4F = 0x2A2D;
        /* Matrix Mode */
        public const int GL_MATRIX_MODE = 0x0BA0;
        public const int GL_MODELVIEW = 0x1700;
        public const int GL_PROJECTION = 0x1701;
        public const int GL_TEXTURE = 0x1702;
        /* Points */
        public const int GL_POINT_SMOOTH = 0x0B10;
        public const int GL_POINT_SIZE = 0x0B11;
        public const int GL_POINT_SIZE_GRANULARITY = 0x0B13;
        public const int GL_POINT_SIZE_RANGE = 0x0B12;
        /* Lines */
        public const int GL_LINE_SMOOTH = 0x0B20;
        public const int GL_LINE_STIPPLE = 0x0B24;
        public const int GL_LINE_STIPPLE_PATTERN = 0x0B25;
        public const int GL_LINE_STIPPLE_REPEAT = 0x0B26;
        public const int GL_LINE_WIDTH = 0x0B21;
        public const int GL_LINE_WIDTH_GRANULARITY = 0x0B23;
        public const int GL_LINE_WIDTH_RANGE = 0x0B22;
        /* Polygons */
        public const int GL_POINT = 0x1B00;
        public const int GL_LINE = 0x1B01;
        public const int GL_FILL = 0x1B02;
        public const int GL_CW = 0x0900;
        public const int GL_CCW = 0x0901;
        public const int GL_FRONT = 0x0404;
        public const int GL_BACK = 0x0405;
        public const int GL_POLYGON_MODE = 0x0B40;
        public const int GL_POLYGON_SMOOTH = 0x0B41;
        public const int GL_POLYGON_STIPPLE = 0x0B42;
        public const int GL_EDGE_FLAG = 0x0B43;
        public const int GL_CULL_FACE = 0x0B44;
        public const int GL_CULL_FACE_MODE = 0x0B45;
        public const int GL_FRONT_FACE = 0x0B46;
        public const int GL_POLYGON_OFFSET_FACTOR = 0x8038;
        public const int GL_POLYGON_OFFSET_UNITS = 0x2A00;
        public const int GL_POLYGON_OFFSET_POINT = 0x2A01;
        public const int GL_POLYGON_OFFSET_LINE = 0x2A02;
        public const int GL_POLYGON_OFFSET_FILL = 0x8037;
        /* Display Lists */
        public const int GL_COMPILE = 0x1300;
        public const int GL_COMPILE_AND_EXECUTE = 0x1301;
        public const int GL_LIST_BASE = 0x0B32;
        public const int GL_LIST_INDEX = 0x0B33;
        public const int GL_LIST_MODE = 0x0B30;
        /* Depth buffer */
        public const int GL_NEVER = 0x0200;
        public const int GL_LESS = 0x0201;
        public const int GL_EQUAL = 0x0202;
        public const int GL_LEQUAL = 0x0203;
        public const int GL_GREATER = 0x0204;
        public const int GL_NOTEQUAL = 0x0205;
        public const int GL_GEQUAL = 0x0206;
        public const int GL_ALWAYS = 0x0207;
        public const int GL_DEPTH_TEST = 0x0B71;
        public const int GL_DEPTH_BITS = 0x0D56;
        public const int GL_DEPTH_CLEAR_VALUE = 0x0B73;
        public const int GL_DEPTH_FUNC = 0x0B74;
        public const int GL_DEPTH_RANGE = 0x0B70;
        public const int GL_DEPTH_WRITEMASK = 0x0B72;
        public const int GL_DEPTH_COMPONENT = 0x1902;
        /* Lighting */
        public const int GL_LIGHTING = 0x0B50;
        public const int GL_LIGHT0 = 0x4000;
        public const int GL_LIGHT1 = 0x4001;
        public const int GL_LIGHT2 = 0x4002;
        public const int GL_LIGHT3 = 0x4003;
        public const int GL_LIGHT4 = 0x4004;
        public const int GL_LIGHT5 = 0x4005;
        public const int GL_LIGHT6 = 0x4006;
        public const int GL_LIGHT7 = 0x4007;
        public const int GL_SPOT_EXPONENT = 0x1205;
        public const int GL_SPOT_CUTOFF = 0x1206;
        public const int GL_CONSTANT_ATTENUATION = 0x1207;
        public const int GL_LINEAR_ATTENUATION = 0x1208;
        public const int GL_QUADRATIC_ATTENUATION = 0x1209;
        public const int GL_AMBIENT = 0x1200;
        public const int GL_DIFFUSE = 0x1201;
        public const int GL_SPECULAR = 0x1202;
        public const int GL_SHININESS = 0x1601;
        public const int GL_EMISSION = 0x1600;
        public const int GL_POSITION = 0x1203;
        public const int GL_SPOT_DIRECTION = 0x1204;
        public const int GL_AMBIENT_AND_DIFFUSE = 0x1602;
        public const int GL_COLOR_INDEXES = 0x1603;
        public const int GL_LIGHT_MODEL_TWO_SIDE = 0x0B52;
        public const int GL_LIGHT_MODEL_LOCAL_VIEWER = 0x0B51;
        public const int GL_LIGHT_MODEL_AMBIENT = 0x0B53;
        public const int GL_FRONT_AND_BACK = 0x0408;
        public const int GL_SHADE_MODEL = 0x0B54;
        public const int GL_FLAT = 0x1D00;
        public const int GL_SMOOTH = 0x1D01;
        public const int GL_COLOR_MATERIAL = 0x0B57;
        public const int GL_COLOR_MATERIAL_FACE = 0x0B55;
        public const int GL_COLOR_MATERIAL_PARAMETER = 0x0B56;
        public const int GL_NORMALIZE = 0x0BA1;
        /* User clipping planes */
        public const int GL_CLIP_PLANE0 = 0x3000;
        public const int GL_CLIP_PLANE1 = 0x3001;
        public const int GL_CLIP_PLANE2 = 0x3002;
        public const int GL_CLIP_PLANE3 = 0x3003;
        public const int GL_CLIP_PLANE4 = 0x3004;
        public const int GL_CLIP_PLANE5 = 0x3005;
        /* Accumulation buffer */
        public const int GL_ACCUM_RED_BITS = 0x0D58;
        public const int GL_ACCUM_GREEN_BITS = 0x0D59;
        public const int GL_ACCUM_BLUE_BITS = 0x0D5A;
        public const int GL_ACCUM_ALPHA_BITS = 0x0D5B;
        public const int GL_ACCUM_CLEAR_VALUE = 0x0B80;
        public const int GL_ACCUM = 0x0100;
        public const int GL_ADD = 0x0104;
        public const int GL_LOAD = 0x0101;
        public const int GL_MULT = 0x0103;
        public const int GL_RETURN = 0x0102;
        /* Alpha testing */
        public const int GL_ALPHA_TEST = 0x0BC0;
        public const int GL_ALPHA_TEST_REF = 0x0BC2;
        public const int GL_ALPHA_TEST_FUNC = 0x0BC1;
        /* Blending */
        public const int GL_BLEND = 0x0BE2;
        public const int GL_BLEND_SRC = 0x0BE1;
        public const int GL_BLEND_DST = 0x0BE0;
        public const int GL_ZERO = 0;
        public const int GL_ONE = 1;
        public const int GL_SRC_COLOR = 0x0300;
        public const int GL_ONE_MINUS_SRC_COLOR = 0x0301;
        public const int GL_SRC_ALPHA = 0x0302;
        public const int GL_ONE_MINUS_SRC_ALPHA = 0x0303;
        public const int GL_DST_ALPHA = 0x0304;
        public const int GL_ONE_MINUS_DST_ALPHA = 0x0305;
        public const int GL_DST_COLOR = 0x0306;
        public const int GL_ONE_MINUS_DST_COLOR = 0x0307;
        public const int GL_SRC_ALPHA_SATURATE = 0x0308;
        /* Render Mode */
        public const int GL_FEEDBACK = 0x1C01;
        public const int GL_RENDER = 0x1C00;
        public const int GL_SELECT = 0x1C02;
        /* Feedback */
        public const int GL_2D = 0x0600;
        public const int GL_3D = 0x0601;
        public const int GL_3D_COLOR = 0x0602;
        public const int GL_3D_COLOR_TEXTURE = 0x0603;
        public const int GL_4D_COLOR_TEXTURE = 0x0604;
        public const int GL_POINT_TOKEN = 0x0701;
        public const int GL_LINE_TOKEN = 0x0702;
        public const int GL_LINE_RESET_TOKEN = 0x0707;
        public const int GL_POLYGON_TOKEN = 0x0703;
        public const int GL_BITMAP_TOKEN = 0x0704;
        public const int GL_DRAW_PIXEL_TOKEN = 0x0705;
        public const int GL_COPY_PIXEL_TOKEN = 0x0706;
        public const int GL_PASS_THROUGH_TOKEN = 0x0700;
        public const int GL_FEEDBACK_BUFFER_POINTER = 0x0DF0;
        public const int GL_FEEDBACK_BUFFER_SIZE = 0x0DF1;
        public const int GL_FEEDBACK_BUFFER_TYPE = 0x0DF2;
        /* Selection */
        public const int GL_SELECTION_BUFFER_POINTER = 0x0DF3;
        public const int GL_SELECTION_BUFFER_SIZE = 0x0DF4;
        /* Fog */
        public const int GL_FOG = 0x0B60;
        public const int GL_FOG_MODE = 0x0B65;
        public const int GL_FOG_DENSITY = 0x0B62;
        public const int GL_FOG_COLOR = 0x0B66;
        public const int GL_FOG_INDEX = 0x0B61;
        public const int GL_FOG_START = 0x0B63;
        public const int GL_FOG_END = 0x0B64;
        public const int GL_LINEAR = 0x2601;
        public const int GL_EXP = 0x0800;
        public const int GL_EXP2 = 0x0801;
        /* Logic Ops */
        public const int GL_LOGIC_OP = 0x0BF1;
        public const int GL_INDEX_LOGIC_OP = 0x0BF1;
        public const int GL_COLOR_LOGIC_OP = 0x0BF2;
        public const int GL_LOGIC_OP_MODE = 0x0BF0;
        public const int GL_CLEAR = 0x1500;
        public const int GL_SET = 0x150F;
        public const int GL_COPY = 0x1503;
        public const int GL_COPY_INVERTED = 0x150C;
        public const int GL_NOOP = 0x1505;
        public const int GL_INVERT = 0x150A;
        public const int GL_AND = 0x1501;
        public const int GL_NAND = 0x150E;
        public const int GL_OR = 0x1507;
        public const int GL_NOR = 0x1508;
        public const int GL_XOR = 0x1506;
        public const int GL_EQUIV = 0x1509;
        public const int GL_AND_REVERSE = 0x1502;
        public const int GL_AND_INVERTED = 0x1504;
        public const int GL_OR_REVERSE = 0x150B;
        public const int GL_OR_INVERTED = 0x150D;
        /* Stencil */
        public const int GL_STENCIL_BITS = 0x0D57;
        public const int GL_STENCIL_TEST = 0x0B90;
        public const int GL_STENCIL_CLEAR_VALUE = 0x0B91;
        public const int GL_STENCIL_FUNC = 0x0B92;
        public const int GL_STENCIL_VALUE_MASK = 0x0B93;
        public const int GL_STENCIL_FAIL = 0x0B94;
        public const int GL_STENCIL_PASS_DEPTH_FAIL = 0x0B95;
        public const int GL_STENCIL_PASS_DEPTH_PASS = 0x0B96;
        public const int GL_STENCIL_REF = 0x0B97;
        public const int GL_STENCIL_WRITEMASK = 0x0B98;
        public const int GL_STENCIL_INDEX = 0x1901;
        public const int GL_KEEP = 0x1E00;
        public const int GL_REPLACE = 0x1E01;
        public const int GL_INCR = 0x1E02;
        public const int GL_DECR = 0x1E03;
        /* Buffers, Pixel Drawing/Reading */
        public const int GL_NONE = 0;
        public const int GL_LEFT = 0x0406;
        public const int GL_RIGHT = 0x0407;
        /*GL_FRONT					0x0404 */
        /*GL_BACK					0x0405 */
        /*GL_FRONT_AND_BACK				0x0408 */
        public const int GL_FRONT_LEFT = 0x0400;
        public const int GL_FRONT_RIGHT = 0x0401;
        public const int GL_BACK_LEFT = 0x0402;
        public const int GL_BACK_RIGHT = 0x0403;
        public const int GL_AUX0 = 0x0409;
        public const int GL_AUX1 = 0x040A;
        public const int GL_AUX2 = 0x040B;
        public const int GL_AUX3 = 0x040C;
        public const int GL_COLOR_INDEX = 0x1900;
        public const int GL_RED = 0x1903;
        public const int GL_GREEN = 0x1904;
        public const int GL_BLUE = 0x1905;
        public const int GL_ALPHA = 0x1906;
        public const int GL_LUMINANCE = 0x1909;
        public const int GL_LUMINANCE_ALPHA = 0x190A;
        public const int GL_ALPHA_BITS = 0x0D55;
        public const int GL_RED_BITS = 0x0D52;
        public const int GL_GREEN_BITS = 0x0D53;
        public const int GL_BLUE_BITS = 0x0D54;
        public const int GL_INDEX_BITS = 0x0D51;
        public const int GL_SUBPIXEL_BITS = 0x0D50;
        public const int GL_AUX_BUFFERS = 0x0C00;
        public const int GL_READ_BUFFER = 0x0C02;
        public const int GL_DRAW_BUFFER = 0x0C01;
        public const int GL_DOUBLEBUFFER = 0x0C32;
        public const int GL_STEREO = 0x0C33;
        public const int GL_BITMAP = 0x1A00;
        public const int GL_COLOR = 0x1800;
        public const int GL_DEPTH = 0x1801;
        public const int GL_STENCIL = 0x1802;
        public const int GL_DITHER = 0x0BD0;
        public const int GL_RGB = 0x1907;
        public const int GL_RGBA = 0x1908;
        /* Implementation limits */
        public const int GL_MAX_LIST_NESTING = 0x0B31;
        public const int GL_MAX_EVAL_ORDER = 0x0D30;
        public const int GL_MAX_LIGHTS = 0x0D31;
        public const int GL_MAX_CLIP_PLANES = 0x0D32;
        public const int GL_MAX_TEXTURE_SIZE = 0x0D33;
        public const int GL_MAX_PIXEL_MAP_TABLE = 0x0D34;
        public const int GL_MAX_ATTRIB_STACK_DEPTH = 0x0D35;
        public const int GL_MAX_MODELVIEW_STACK_DEPTH = 0x0D36;
        public const int GL_MAX_NAME_STACK_DEPTH = 0x0D37;
        public const int GL_MAX_PROJECTION_STACK_DEPTH = 0x0D38;
        public const int GL_MAX_TEXTURE_STACK_DEPTH = 0x0D39;
        public const int GL_MAX_VIEWPORT_DIMS = 0x0D3A;
        public const int GL_MAX_CLIENT_ATTRIB_STACK_DEPTH = 0x0D3B;
        /* Gets */
        public const int GL_ATTRIB_STACK_DEPTH = 0x0BB0;
        public const int GL_CLIENT_ATTRIB_STACK_DEPTH = 0x0BB1;
        public const int GL_COLOR_CLEAR_VALUE = 0x0C22;
        public const int GL_COLOR_WRITEMASK = 0x0C23;
        public const int GL_CURRENT_INDEX = 0x0B01;
        public const int GL_CURRENT_COLOR = 0x0B00;
        public const int GL_CURRENT_NORMAL = 0x0B02;
        public const int GL_CURRENT_RASTER_COLOR = 0x0B04;
        public const int GL_CURRENT_RASTER_DISTANCE = 0x0B09;
        public const int GL_CURRENT_RASTER_INDEX = 0x0B05;
        public const int GL_CURRENT_RASTER_POSITION = 0x0B07;
        public const int GL_CURRENT_RASTER_TEXTURE_COORDS = 0x0B06;
        public const int GL_CURRENT_RASTER_POSITION_VALID = 0x0B08;
        public const int GL_CURRENT_TEXTURE_COORDS = 0x0B03;
        public const int GL_INDEX_CLEAR_VALUE = 0x0C20;
        public const int GL_INDEX_MODE = 0x0C30;
        public const int GL_INDEX_WRITEMASK = 0x0C21;
        public const int GL_MODELVIEW_MATRIX = 0x0BA6;
        public const int GL_MODELVIEW_STACK_DEPTH = 0x0BA3;
        public const int GL_NAME_STACK_DEPTH = 0x0D70;
        public const int GL_PROJECTION_MATRIX = 0x0BA7;
        public const int GL_PROJECTION_STACK_DEPTH = 0x0BA4;
        public const int GL_RENDER_MODE = 0x0C40;
        public const int GL_RGBA_MODE = 0x0C31;
        public const int GL_TEXTURE_MATRIX = 0x0BA8;
        public const int GL_TEXTURE_STACK_DEPTH = 0x0BA5;
        public const int GL_VIEWPORT = 0x0BA2;
        /* Evaluators */
        public const int GL_AUTO_NORMAL = 0x0D80;
        public const int GL_MAP1_COLOR_4 = 0x0D90;
        public const int GL_MAP1_INDEX = 0x0D91;
        public const int GL_MAP1_NORMAL = 0x0D92;
        public const int GL_MAP1_TEXTURE_COORD_1 = 0x0D93;
        public const int GL_MAP1_TEXTURE_COORD_2 = 0x0D94;
        public const int GL_MAP1_TEXTURE_COORD_3 = 0x0D95;
        public const int GL_MAP1_TEXTURE_COORD_4 = 0x0D96;
        public const int GL_MAP1_VERTEX_3 = 0x0D97;
        public const int GL_MAP1_VERTEX_4 = 0x0D98;
        public const int GL_MAP2_COLOR_4 = 0x0DB0;
        public const int GL_MAP2_INDEX = 0x0DB1;
        public const int GL_MAP2_NORMAL = 0x0DB2;
        public const int GL_MAP2_TEXTURE_COORD_1 = 0x0DB3;
        public const int GL_MAP2_TEXTURE_COORD_2 = 0x0DB4;
        public const int GL_MAP2_TEXTURE_COORD_3 = 0x0DB5;
        public const int GL_MAP2_TEXTURE_COORD_4 = 0x0DB6;
        public const int GL_MAP2_VERTEX_3 = 0x0DB7;
        public const int GL_MAP2_VERTEX_4 = 0x0DB8;
        public const int GL_MAP1_GRID_DOMAIN = 0x0DD0;
        public const int GL_MAP1_GRID_SEGMENTS = 0x0DD1;
        public const int GL_MAP2_GRID_DOMAIN = 0x0DD2;
        public const int GL_MAP2_GRID_SEGMENTS = 0x0DD3;
        public const int GL_COEFF = 0x0A00;
        public const int GL_ORDER = 0x0A01;
        public const int GL_DOMAIN = 0x0A02;
        /* Hints */
        public const int GL_PERSPECTIVE_CORRECTION_HINT = 0x0C50;
        public const int GL_POINT_SMOOTH_HINT = 0x0C51;
        public const int GL_LINE_SMOOTH_HINT = 0x0C52;
        public const int GL_POLYGON_SMOOTH_HINT = 0x0C53;
        public const int GL_FOG_HINT = 0x0C54;
        public const int GL_DONT_CARE = 0x1100;
        public const int GL_FASTEST = 0x1101;
        public const int GL_NICEST = 0x1102;
        /* Scissor box */
        public const int GL_SCISSOR_BOX = 0x0C10;
        public const int GL_SCISSOR_TEST = 0x0C11;
        /* Pixel Mode / Transfer */
        public const int GL_MAP_COLOR = 0x0D10;
        public const int GL_MAP_STENCIL = 0x0D11;
        public const int GL_INDEX_SHIFT = 0x0D12;
        public const int GL_INDEX_OFFSET = 0x0D13;
        public const int GL_RED_SCALE = 0x0D14;
        public const int GL_RED_BIAS = 0x0D15;
        public const int GL_GREEN_SCALE = 0x0D18;
        public const int GL_GREEN_BIAS = 0x0D19;
        public const int GL_BLUE_SCALE = 0x0D1A;
        public const int GL_BLUE_BIAS = 0x0D1B;
        public const int GL_ALPHA_SCALE = 0x0D1C;
        public const int GL_ALPHA_BIAS = 0x0D1D;
        public const int GL_DEPTH_SCALE = 0x0D1E;
        public const int GL_DEPTH_BIAS = 0x0D1F;
        public const int GL_PIXEL_MAP_S_TO_S_SIZE = 0x0CB1;
        public const int GL_PIXEL_MAP_I_TO_I_SIZE = 0x0CB0;
        public const int GL_PIXEL_MAP_I_TO_R_SIZE = 0x0CB2;
        public const int GL_PIXEL_MAP_I_TO_G_SIZE = 0x0CB3;
        public const int GL_PIXEL_MAP_I_TO_B_SIZE = 0x0CB4;
        public const int GL_PIXEL_MAP_I_TO_A_SIZE = 0x0CB5;
        public const int GL_PIXEL_MAP_R_TO_R_SIZE = 0x0CB6;
        public const int GL_PIXEL_MAP_G_TO_G_SIZE = 0x0CB7;
        public const int GL_PIXEL_MAP_B_TO_B_SIZE = 0x0CB8;
        public const int GL_PIXEL_MAP_A_TO_A_SIZE = 0x0CB9;
        public const int GL_PIXEL_MAP_S_TO_S = 0x0C71;
        public const int GL_PIXEL_MAP_I_TO_I = 0x0C70;
        public const int GL_PIXEL_MAP_I_TO_R = 0x0C72;
        public const int GL_PIXEL_MAP_I_TO_G = 0x0C73;
        public const int GL_PIXEL_MAP_I_TO_B = 0x0C74;
        public const int GL_PIXEL_MAP_I_TO_A = 0x0C75;
        public const int GL_PIXEL_MAP_R_TO_R = 0x0C76;
        public const int GL_PIXEL_MAP_G_TO_G = 0x0C77;
        public const int GL_PIXEL_MAP_B_TO_B = 0x0C78;
        public const int GL_PIXEL_MAP_A_TO_A = 0x0C79;
        public const int GL_PACK_ALIGNMENT = 0x0D05;
        public const int GL_PACK_LSB_FIRST = 0x0D01;
        public const int GL_PACK_ROW_LENGTH = 0x0D02;
        public const int GL_PACK_SKIP_PIXELS = 0x0D04;
        public const int GL_PACK_SKIP_ROWS = 0x0D03;
        public const int GL_PACK_SWAP_BYTES = 0x0D00;
        public const int GL_UNPACK_ALIGNMENT = 0x0CF5;
        public const int GL_UNPACK_LSB_FIRST = 0x0CF1;
        public const int GL_UNPACK_ROW_LENGTH = 0x0CF2;
        public const int GL_UNPACK_SKIP_PIXELS = 0x0CF4;
        public const int GL_UNPACK_SKIP_ROWS = 0x0CF3;
        public const int GL_UNPACK_SWAP_BYTES = 0x0CF0;
        public const int GL_ZOOM_X = 0x0D16;
        public const int GL_ZOOM_Y = 0x0D17;
        /* Texture mapping */
        public const int GL_TEXTURE_ENV = 0x2300;
        public const int GL_TEXTURE_ENV_MODE = 0x2200;
        public const int GL_TEXTURE_1D = 0x0DE0;
        public const int GL_TEXTURE_2D = 0x0DE1;
        public const int GL_TEXTURE_WRAP_S = 0x2802;
        public const int GL_TEXTURE_WRAP_T = 0x2803;
        public const int GL_TEXTURE_MAG_FILTER = 0x2800;
        public const int GL_TEXTURE_MIN_FILTER = 0x2801;
        public const int GL_TEXTURE_ENV_COLOR = 0x2201;
        public const int GL_TEXTURE_GEN_S = 0x0C60;
        public const int GL_TEXTURE_GEN_T = 0x0C61;
        public const int GL_TEXTURE_GEN_R = 0x0C62;
        public const int GL_TEXTURE_GEN_Q = 0x0C63;
        public const int GL_TEXTURE_GEN_MODE = 0x2500;
        public const int GL_TEXTURE_BORDER_COLOR = 0x1004;
        public const int GL_TEXTURE_WIDTH = 0x1000;
        public const int GL_TEXTURE_HEIGHT = 0x1001;
        public const int GL_TEXTURE_BORDER = 0x1005;
        public const int GL_TEXTURE_COMPONENTS = 0x1003;
        public const int GL_TEXTURE_RED_SIZE = 0x805C;
        public const int GL_TEXTURE_GREEN_SIZE = 0x805D;
        public const int GL_TEXTURE_BLUE_SIZE = 0x805E;
        public const int GL_TEXTURE_ALPHA_SIZE = 0x805F;
        public const int GL_TEXTURE_LUMINANCE_SIZE = 0x8060;
        public const int GL_TEXTURE_INTENSITY_SIZE = 0x8061;
        public const int GL_NEAREST_MIPMAP_NEAREST = 0x2700;
        public const int GL_NEAREST_MIPMAP_LINEAR = 0x2702;
        public const int GL_LINEAR_MIPMAP_NEAREST = 0x2701;
        public const int GL_LINEAR_MIPMAP_LINEAR = 0x2703;
        public const int GL_OBJECT_LINEAR = 0x2401;
        public const int GL_OBJECT_PLANE = 0x2501;
        public const int GL_EYE_LINEAR = 0x2400;
        public const int GL_EYE_PLANE = 0x2502;
        public const int GL_SPHERE_MAP = 0x2402;
        public const int GL_DECAL = 0x2101;
        public const int GL_MODULATE = 0x2100;
        public const int GL_NEAREST = 0x2600;
        public const int GL_REPEAT = 0x2901;
        public const int GL_CLAMP = 0x2900;
        public const int GL_S = 0x2000;
        public const int GL_T = 0x2001;
        public const int GL_R = 0x2002;
        public const int GL_Q = 0x2003;
        /* Utility */
        public const int GL_VENDOR = 0x1F00;
        public const int GL_RENDERER = 0x1F01;
        public const int GL_VERSION = 0x1F02;
        public const int GL_EXTENSIONS = 0x1F03;
        /* Errors */
        public const int GL_NO_ERROR = 0;
        public const int GL_INVALID_ENUM = 0x0500;
        public const int GL_INVALID_VALUE = 0x0501;
        public const int GL_INVALID_OPERATION = 0x0502;
        public const int GL_STACK_OVERFLOW = 0x0503;
        public const int GL_STACK_UNDERFLOW = 0x0504;
        public const int GL_OUT_OF_MEMORY = 0x0505;
        /* glPush/PopAttrib bits */
        public const uint GL_CURRENT_BIT = 0x00000001;
        public const uint GL_POINT_BIT = 0x00000002;
        public const uint GL_LINE_BIT = 0x00000004;
        public const uint GL_POLYGON_BIT = 0x00000008;
        public const uint GL_POLYGON_STIPPLE_BIT = 0x00000010;
        public const uint GL_PIXEL_MODE_BIT = 0x00000020;
        public const uint GL_LIGHTING_BIT = 0x00000040;
        public const uint GL_FOG_BIT = 0x00000080;
        public const uint GL_DEPTH_BUFFER_BIT = 0x00000100;
        public const uint GL_ACCUM_BUFFER_BIT = 0x00000200;
        public const uint GL_STENCIL_BUFFER_BIT = 0x00000400;
        public const uint GL_VIEWPORT_BIT = 0x00000800;
        public const uint GL_TRANSFORM_BIT = 0x00001000;
        public const uint GL_ENABLE_BIT = 0x00002000;
        public const uint GL_COLOR_BUFFER_BIT = 0x00004000;
        public const uint GL_HINT_BIT = 0x00008000;
        public const uint GL_EVAL_BIT = 0x00010000;
        public const uint GL_LIST_BIT = 0x00020000;
        public const uint GL_TEXTURE_BIT = 0x00040000;
        public const uint GL_SCISSOR_BIT = 0x00080000;
        public const uint GL_ALL_ATTRIB_BITS = 0xFFFFFFFF;

        /* OpenGL 1.1 */
        public const int GL_PROXY_TEXTURE_1D = 0x8063;
        public const int GL_PROXY_TEXTURE_2D = 0x8064;
        public const int GL_TEXTURE_PRIORITY = 0x8066;
        public const int GL_TEXTURE_RESIDENT = 0x8067;
        public const int GL_TEXTURE_BINDING_1D = 0x8068;
        public const int GL_TEXTURE_BINDING_2D = 0x8069;
        public const int GL_TEXTURE_INTERNAL_FORMAT = 0x1003;
        public const int GL_ALPHA4 = 0x803B;
        public const int GL_ALPHA8 = 0x803C;
        public const int GL_ALPHA12 = 0x803D;
        public const int GL_ALPHA16 = 0x803E;
        public const int GL_LUMINANCE4 = 0x803F;
        public const int GL_LUMINANCE8 = 0x8040;
        public const int GL_LUMINANCE12 = 0x8041;
        public const int GL_LUMINANCE16 = 0x8042;
        public const int GL_LUMINANCE4_ALPHA4 = 0x8043;
        public const int GL_LUMINANCE6_ALPHA2 = 0x8044;
        public const int GL_LUMINANCE8_ALPHA8 = 0x8045;
        public const int GL_LUMINANCE12_ALPHA4 = 0x8046;
        public const int GL_LUMINANCE12_ALPHA12 = 0x8047;
        public const int GL_LUMINANCE16_ALPHA16 = 0x8048;
        public const int GL_INTENSITY = 0x8049;
        public const int GL_INTENSITY4 = 0x804A;
        public const int GL_INTENSITY8 = 0x804B;
        public const int GL_INTENSITY12 = 0x804C;
        public const int GL_INTENSITY16 = 0x804D;
        public const int GL_R3_G3_B2 = 0x2A10;
        public const int GL_RGB4 = 0x804F;
        public const int GL_RGB5 = 0x8050;
        public const int GL_RGB8 = 0x8051;
        public const int GL_RGB10 = 0x8052;
        public const int GL_RGB12 = 0x8053;
        public const int GL_RGB16 = 0x8054;
        public const int GL_RGBA2 = 0x8055;
        public const int GL_RGBA4 = 0x8056;
        public const int GL_RGB5_A1 = 0x8057;
        public const int GL_RGBA8 = 0x8058;
        public const int GL_RGB10_A2 = 0x8059;
        public const int GL_RGBA12 = 0x805A;
        public const int GL_RGBA16 = 0x805B;
        public const uint GL_CLIENT_PIXEL_STORE_BIT = 0x00000001;
        public const uint GL_CLIENT_VERTEX_ARRAY_BIT = 0x00000002;
        public const uint GL_ALL_CLIENT_ATTRIB_BITS = 0xFFFFFFFF;
        public const uint GL_CLIENT_ALL_ATTRIB_BITS = 0xFFFFFFFF;

        [DllImport("opengl32.dll")]
        public static extern void glAccum(uint op, float value);
        [DllImport("opengl32.dll")]
        public static extern void glAlphaFunc(uint func, float ref_notkeword);
        [DllImport("opengl32.dll")]
        public static extern byte glAreTexturesResident(int n, uint[] textures, byte[] residences);
        [DllImport("opengl32.dll")]
        public static extern void glArrayElement(int i);
        [DllImport("opengl32.dll")]
        public static extern void glBegin(uint mode);
        [DllImport("opengl32.dll")]
        public static extern void glBindTexture(uint target, uint texture);
        [DllImport("opengl32.dll")]
        public static extern void glBitmap(int width, int height, float xorig, float yorig, float xmove, float ymove, byte[] bitmap);
        [DllImport("opengl32.dll")]
        public static extern void glBlendFunc(uint sfactor, uint dfactor);
        [DllImport("opengl32.dll")]
        public static extern void glCallList(uint list);
        [DllImport("opengl32.dll")]
        public static extern void glCallLists(int n, uint type, IntPtr lists);
        [DllImport("opengl32.dll")]
        public static extern void glCallLists(int n, uint type, uint[] lists);
        [DllImport("opengl32.dll")]
        public static extern void glCallLists(int n, uint type, byte[] lists);
        [DllImport("opengl32.dll")]
        public static extern void glClear(uint mask);
        [DllImport("opengl32.dll")]
        public static extern void glClearAccum(float red, float green, float blue, float alpha);
        [DllImport("opengl32.dll")]
        public static extern void glClearColor(float red, float green, float blue, float alpha);
        [DllImport("opengl32.dll")]
        public static extern void glClearDepth(double depth);
        [DllImport("opengl32.dll")]
        public static extern void glClearIndex(float c);
        [DllImport("opengl32.dll")]
        public static extern void glClearStencil(int s);
        [DllImport("opengl32.dll")]
        public static extern void glClipPlane(uint plane, double[] equation);
        [DllImport("opengl32.dll")]
        public static extern void glColor3b(byte red, byte green, byte blue);
        [DllImport("opengl32.dll")]
        public static extern void glColor3bv(byte[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor3d(double red, double green, double blue);
        [DllImport("opengl32.dll")]
        public static extern void glColor3dv(double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor3f(float red, float green, float blue);
        [DllImport("opengl32.dll")]
        public static extern void glColor3fv(float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor3i(int red, int green, int blue);
        [DllImport("opengl32.dll")]
        public static extern void glColor3iv(int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor3s(short red, short green, short blue);
        [DllImport("opengl32.dll")]
        public static extern void glColor3sv(short[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor3ub(byte red, byte green, byte blue);
        [DllImport("opengl32.dll")]
        public static extern void glColor3ubv(byte[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor3ui(uint red, uint green, uint blue);
        [DllImport("opengl32.dll")]
        public static extern void glColor3uiv(uint[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor3us(ushort red, ushort green, ushort blue);
        [DllImport("opengl32.dll")]
        public static extern void glColor3usv(ushort[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor4b(byte red, byte green, byte blue, byte alpha);
        [DllImport("opengl32.dll")]
        public static extern void glColor4bv(byte[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor4d(double red, double green, double blue, double alpha);
        [DllImport("opengl32.dll")]
        public static extern void glColor4dv(double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor4f(float red, float green, float blue, float alpha);
        [DllImport("opengl32.dll")]
        public static extern void glColor4fv(float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor4i(int red, int green, int blue, int alpha);
        [DllImport("opengl32.dll")]
        public static extern void glColor4iv(int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor4s(short red, short green, short blue, short alpha);
        [DllImport("opengl32.dll")]
        public static extern void glColor4sv(short[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor4ub(byte red, byte green, byte blue, byte alpha);
        [DllImport("opengl32.dll")]
        public static extern void glColor4ubv(byte[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor4ui(uint red, uint green, uint blue, uint alpha);
        [DllImport("opengl32.dll")]
        public static extern void glColor4uiv(uint[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColor4us(ushort red, ushort green, ushort blue, ushort alpha);
        [DllImport("opengl32.dll")]
        public static extern void glColor4usv(ushort[] v);
        [DllImport("opengl32.dll")]
        public static extern void glColorMask(byte red, byte green, byte blue, byte alpha);
        [DllImport("opengl32.dll")]
        public static extern void glColorMaterial(uint face, uint mode);
        [DllImport("opengl32.dll")]
        public static extern void glColorPointer(int size, uint type, int stride, int[] pointer);
        [DllImport("opengl32.dll")]
        public static extern void glCopyPixels(int x, int y, int width, int height, uint type);
        [DllImport("opengl32.dll")]
        public static extern void glCopyTexImage1D(uint target, int level, uint internalFormat, int x, int y, int width, int border);
        [DllImport("opengl32.dll")]
        public static extern void glCopyTexImage2D(uint target, int level, uint internalFormat, int x, int y, int width, int height, int border);
        [DllImport("opengl32.dll")]
        public static extern void glCopyTexSubImage1D(uint target, int level, int xoffset, int x, int y, int width);
        [DllImport("opengl32.dll")]
        public static extern void glCopyTexSubImage2D(uint target, int level, int xoffset, int yoffset, int x, int y, int width, int height);
        [DllImport("opengl32.dll")]
        public static extern void glCullFace(uint mode);
        [DllImport("opengl32.dll")]
        public static extern void glDeleteLists(uint list, int range);
        [DllImport("opengl32.dll")]
        public static extern void glDeleteTextures(int n, uint[] textures);
        [DllImport("opengl32.dll")]
        public static extern void glDepthFunc(uint func);
        [DllImport("opengl32.dll")]
        public static extern void glDepthMask(byte flag);
        [DllImport("opengl32.dll")]
        public static extern void glDepthRange(double zNear, double zFar);
        [DllImport("opengl32.dll")]
        public static extern void glDisable(uint cap);
        [DllImport("opengl32.dll")]
        public static extern void glDisableClientState(uint array);
        [DllImport("opengl32.dll")]
        public static extern void glDrawArrays(uint mode, int first, int count);
        [DllImport("opengl32.dll")]
        public static extern void glDrawBuffer(uint mode);
        [DllImport("opengl32.dll")]
        public static extern void glDrawElements(uint mode, int count, uint type, int[] indices);
        [DllImport("opengl32.dll")]
        public static extern void glDrawPixels(int width, int height, uint format, uint type, float[] pixels);
        [DllImport("opengl32.dll")]
        public static extern void glEdgeFlag(byte flag);
        [DllImport("opengl32.dll")]
        public static extern void glEdgeFlagPointer(int stride, int[] pointer);
        [DllImport("opengl32.dll")]
        public static extern void glEdgeFlagv(byte[] flag);
        [DllImport("opengl32.dll")]
        public static extern void glEnable(uint cap);
        [DllImport("opengl32.dll")]
        public static extern void glEnableClientState(uint array);
        [DllImport("opengl32.dll")]
        public static extern void glEnd();
        [DllImport("opengl32.dll")]
        public static extern void glEndList();
        [DllImport("opengl32.dll")]
        public static extern void glEvalCoord1d(double u);
        [DllImport("opengl32.dll")]
        public static extern void glEvalCoord1dv(double[] u);
        [DllImport("opengl32.dll")]
        public static extern void glEvalCoord1f(float u);
        [DllImport("opengl32.dll")]
        public static extern void glEvalCoord1fv(float[] u);
        [DllImport("opengl32.dll")]
        public static extern void glEvalCoord2d(double u, double v);
        [DllImport("opengl32.dll")]
        public static extern void glEvalCoord2dv(double[] u);
        [DllImport("opengl32.dll")]
        public static extern void glEvalCoord2f(float u, float v);
        [DllImport("opengl32.dll")]
        public static extern void glEvalCoord2fv(float[] u);
        [DllImport("opengl32.dll")]
        public static extern void glEvalMesh1(uint mode, int i1, int i2);
        [DllImport("opengl32.dll")]
        public static extern void glEvalMesh2(uint mode, int i1, int i2, int j1, int j2);
        [DllImport("opengl32.dll")]
        public static extern void glEvalPoint1(int i);
        [DllImport("opengl32.dll")]
        public static extern void glEvalPoint2(int i, int j);
        [DllImport("opengl32.dll")]
        public static extern void glFeedbackBuffer(int size, uint type, float[] buffer);
        [DllImport("opengl32.dll")]
        public static extern void glFinish();
        [DllImport("opengl32.dll")]
        public static extern void glFlush();
        [DllImport("opengl32.dll")]
        public static extern void glFogf(uint pname, float param);
        [DllImport("opengl32.dll")]
        public static extern void glFogfv(uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glFogi(uint pname, int param);
        [DllImport("opengl32.dll")]
        public static extern void glFogiv(uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glFrontFace(uint mode);
        [DllImport("opengl32.dll")]
        public static extern void glFrustum(double left, double right, double bottom, double top, double zNear, double zFar);
        [DllImport("opengl32.dll")]
        public static extern uint glGenLists(int range);
        [DllImport("opengl32.dll")]
        public static extern void glGenTextures(int n, uint[] textures);
        [DllImport("opengl32.dll")]
        public static extern void glGetBooleanv(uint pname, byte[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetClipPlane(uint plane, double[] equation);
        [DllImport("opengl32.dll")]
        public static extern void glGetDoublev(uint pname, double[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern uint glGetError();
        [DllImport("opengl32.dll")]
        public static extern void glGetFloatv(uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetIntegerv(uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetLightfv(uint light, uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetLightiv(uint light, uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetMapdv(uint target, uint query, double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glGetMapfv(uint target, uint query, float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glGetMapiv(uint target, uint query, int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glGetMaterialfv(uint face, uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetMaterialiv(uint face, uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetPixelMapfv(uint map, float[] values);
        [DllImport("opengl32.dll")]
        public static extern void glGetPixelMapuiv(uint map, uint[] values);
        [DllImport("opengl32.dll")]
        public static extern void glGetPixelMapusv(uint map, ushort[] values);
        [DllImport("opengl32.dll")]
        public static extern void glGetPointerv(uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetPolygonStipple(byte[] mask);
        [DllImport("opengl32.dll")]
        public static extern IntPtr glGetString(uint name);
        [DllImport("opengl32.dll")]
        public static extern void glGetTexEnvfv(uint target, uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetTexEnviv(uint target, uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetTexGendv(uint coord, uint pname, double[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetTexGenfv(uint coord, uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetTexGeniv(uint coord, uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetTexImage(uint target, int level, uint format, uint type, int[] pixels);
        [DllImport("opengl32.dll")]
        public static extern void glGetTexLevelParameterfv(uint target, int level, uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetTexLevelParameteriv(uint target, int level, uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetTexParameterfv(uint target, uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glGetTexParameteriv(uint target, uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glHint(uint target, uint mode);
        [DllImport("opengl32.dll")]
        public static extern void glIndexMask(uint mask);
        [DllImport("opengl32.dll")]
        public static extern void glIndexPointer(uint type, int stride, int[] pointer);
        [DllImport("opengl32.dll")]
        public static extern void glIndexd(double c);
        [DllImport("opengl32.dll")]
        public static extern void glIndexdv(double[] c);
        [DllImport("opengl32.dll")]
        public static extern void glIndexf(float c);
        [DllImport("opengl32.dll")]
        public static extern void glIndexfv(float[] c);
        [DllImport("opengl32.dll")]
        public static extern void glIndexi(int c);
        [DllImport("opengl32.dll")]
        public static extern void glIndexiv(int[] c);
        [DllImport("opengl32.dll")]
        public static extern void glIndexs(short c);
        [DllImport("opengl32.dll")]
        public static extern void glIndexsv(short[] c);
        [DllImport("opengl32.dll")]
        public static extern void glIndexub(byte c);
        [DllImport("opengl32.dll")]
        public static extern void glIndexubv(byte[] c);
        [DllImport("opengl32.dll")]
        public static extern void glInitNames();
        [DllImport("opengl32.dll")]
        public static extern void glInterleavedArrays(uint format, int stride, int[] pointer);
        [DllImport("opengl32.dll")]
        public static extern byte glIsEnabled(uint cap);
        [DllImport("opengl32.dll")]
        public static extern byte glIsList(uint list);
        [DllImport("opengl32.dll")]
        public static extern byte glIsTexture(uint texture);
        [DllImport("opengl32.dll")]
        public static extern void glLightModelf(uint pname, float param);
        [DllImport("opengl32.dll")]
        public static extern void glLightModelfv(uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glLightModeli(uint pname, int param);
        [DllImport("opengl32.dll")]
        public static extern void glLightModeliv(uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glLightf(uint light, uint pname, float param);
        [DllImport("opengl32.dll")]
        public static extern void glLightfv(uint light, uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glLighti(uint light, uint pname, int param);
        [DllImport("opengl32.dll")]
        public static extern void glLightiv(uint light, uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glLineStipple(int factor, ushort pattern);
        [DllImport("opengl32.dll")]
        public static extern void glLineWidth(float width);
        [DllImport("opengl32.dll")]
        public static extern void glListBase(uint base_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glLoadIdentity();
        [DllImport("opengl32.dll")]
        public static extern void glLoadMatrixd(double[] m);
        [DllImport("opengl32.dll")]
        public static extern void glLoadMatrixf(float[] m);
        [DllImport("opengl32.dll")]
        public static extern void glLoadName(uint name);
        [DllImport("opengl32.dll")]
        public static extern void glLogicOp(uint opcode);
        [DllImport("opengl32.dll")]
        public static extern void glMap1d(uint target, double u1, double u2, int stride, int order, double[] points);
        [DllImport("opengl32.dll")]
        public static extern void glMap1f(uint target, float u1, float u2, int stride, int order, float[] points);
        [DllImport("opengl32.dll")]
        public static extern void glMap2d(uint target, double u1, double u2, int ustride, int uorder, double v1, double v2, int vstride, int vorder, double[] points);
        [DllImport("opengl32.dll")]
        public static extern void glMap2f(uint target, float u1, float u2, int ustride, int uorder, float v1, float v2, int vstride, int vorder, float[] points);
        [DllImport("opengl32.dll")]
        public static extern void glMapGrid1d(int un, double u1, double u2);
        [DllImport("opengl32.dll")]
        public static extern void glMapGrid1f(int un, float u1, float u2);
        [DllImport("opengl32.dll")]
        public static extern void glMapGrid2d(int un, double u1, double u2, int vn, double v1, double v2);
        [DllImport("opengl32.dll")]
        public static extern void glMapGrid2f(int un, float u1, float u2, int vn, float v1, float v2);
        [DllImport("opengl32.dll")]
        public static extern void glMaterialf(uint face, uint pname, float param);
        [DllImport("opengl32.dll")]
        public static extern void glMaterialfv(uint face, uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glMateriali(uint face, uint pname, int param);
        [DllImport("opengl32.dll")]
        public static extern void glMaterialiv(uint face, uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glMatrixMode(uint mode);
        [DllImport("opengl32.dll")]
        public static extern void glMultMatrixd(double[] m);
        [DllImport("opengl32.dll")]
        public static extern void glMultMatrixf(float[] m);
        [DllImport("opengl32.dll")]
        public static extern void glNewList(uint list, uint mode);
        [DllImport("opengl32.dll")]
        public static extern void glNormal3b(byte nx, byte ny, byte nz);
        [DllImport("opengl32.dll")]
        public static extern void glNormal3bv(byte[] v);
        [DllImport("opengl32.dll")]
        public static extern void glNormal3d(double nx, double ny, double nz);
        [DllImport("opengl32.dll")]
        public static extern void glNormal3dv(double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glNormal3f(float nx, float ny, float nz);
        [DllImport("opengl32.dll")]
        public static extern void glNormal3fv(float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glNormal3i(int nx, int ny, int nz);
        [DllImport("opengl32.dll")]
        public static extern void glNormal3iv(int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glNormal3s(short nx, short ny, short nz);
        [DllImport("opengl32.dll")]
        public static extern void glNormal3sv(short[] v);
        [DllImport("opengl32.dll")]
        public static extern void glNormalPointer(uint type, int stride, float[] pointer);
        [DllImport("opengl32.dll")]
        public static extern void glOrtho(double left, double right, double bottom, double top, double zNear, double zFar);
        [DllImport("opengl32.dll")]
        public static extern void glPassThrough(float token);
        [DllImport("opengl32.dll")]
        public static extern void glPixelMapfv(uint map, int mapsize, float[] values);
        [DllImport("opengl32.dll")]
        public static extern void glPixelMapuiv(uint map, int mapsize, uint[] values);
        [DllImport("opengl32.dll")]
        public static extern void glPixelMapusv(uint map, int mapsize, ushort[] values);
        [DllImport("opengl32.dll")]
        public static extern void glPixelStoref(uint pname, float param);
        [DllImport("opengl32.dll")]
        public static extern void glPixelStorei(uint pname, int param);
        [DllImport("opengl32.dll")]
        public static extern void glPixelTransferf(uint pname, float param);
        [DllImport("opengl32.dll")]
        public static extern void glPixelTransferi(uint pname, int param);
        [DllImport("opengl32.dll")]
        public static extern void glPixelZoom(float xfactor, float yfactor);
        [DllImport("opengl32.dll")]
        public static extern void glPointSize(float size);
        [DllImport("opengl32.dll")]
        public static extern void glPolygonMode(uint face, uint mode);
        [DllImport("opengl32.dll")]
        public static extern void glPolygonOffset(float factor, float units);
        [DllImport("opengl32.dll")]
        public static extern void glPolygonStipple(byte[] mask);
        [DllImport("opengl32.dll")]
        public static extern void glPopAttrib();
        [DllImport("opengl32.dll")]
        public static extern void glPopClientAttrib();
        [DllImport("opengl32.dll")]
        public static extern void glPopMatrix();
        [DllImport("opengl32.dll")]
        public static extern void glPopName();
        [DllImport("opengl32.dll")]
        public static extern void glPrioritizeTextures(int n, uint[] textures, float[] priorities);
        [DllImport("opengl32.dll")]
        public static extern void glPushAttrib(uint mask);
        [DllImport("opengl32.dll")]
        public static extern void glPushClientAttrib(uint mask);
        [DllImport("opengl32.dll")]
        public static extern void glPushMatrix();
        [DllImport("opengl32.dll")]
        public static extern void glPushName(uint name);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos2d(double x, double y);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos2dv(double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos2f(float x, float y);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos2fv(float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos2i(int x, int y);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos2iv(int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos2s(short x, short y);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos2sv(short[] v);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos3d(double x, double y, double z);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos3dv(double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos3f(float x, float y, float z);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos3fv(float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos3i(int x, int y, int z);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos3iv(int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos3s(short x, short y, short z);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos3sv(short[] v);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos4d(double x, double y, double z, double w);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos4dv(double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos4f(float x, float y, float z, float w);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos4fv(float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos4i(int x, int y, int z, int w);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos4iv(int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos4s(short x, short y, short z, short w);
        [DllImport("opengl32.dll")]
        public static extern void glRasterPos4sv(short[] v);
        [DllImport("opengl32.dll")]
        public static extern void glReadBuffer(uint mode);
        [DllImport("opengl32.dll")]
        public static extern void glReadPixels(int x, int y, int width, int height, uint format, uint type, byte[] pixels);
        [DllImport("opengl32.dll")]
        public static extern void glReadPixels(int x, int y, int width, int height, uint format, uint type, IntPtr pixels);
        [DllImport("opengl32.dll")]
        public static extern void glRectd(double x1, double y1, double x2, double y2);
        [DllImport("opengl32.dll")]
        public static extern void glRectdv(double[] v1, double[] v2);
        [DllImport("opengl32.dll")]
        public static extern void glRectf(float x1, float y1, float x2, float y2);
        [DllImport("opengl32.dll")]
        public static extern void glRectfv(float[] v1, float[] v2);
        [DllImport("opengl32.dll")]
        public static extern void glRecti(int x1, int y1, int x2, int y2);
        [DllImport("opengl32.dll")]
        public static extern void glRectiv(int[] v1, int[] v2);
        [DllImport("opengl32.dll")]
        public static extern void glRects(short x1, short y1, short x2, short y2);
        [DllImport("opengl32.dll")]
        public static extern void glRectsv(short[] v1, short[] v2);
        [DllImport("opengl32.dll")]
        public static extern int glRenderMode(uint mode);
        [DllImport("opengl32.dll")]
        public static extern void glRotated(double angle, double x, double y, double z);
        [DllImport("opengl32.dll")]
        public static extern void glRotatef(float angle, float x, float y, float z);
        [DllImport("opengl32.dll")]
        public static extern void glScaled(double x, double y, double z);
        [DllImport("opengl32.dll")]
        public static extern void glScalef(float x, float y, float z);
        [DllImport("opengl32.dll")]
        public static extern void glScissor(int x, int y, int width, int height);
        [DllImport("opengl32.dll")]
        public static extern void glSelectBuffer(int size, uint[] buffer);
        [DllImport("opengl32.dll")]
        public static extern void glShadeModel(uint mode);
        [DllImport("opengl32.dll")]
        public static extern void glStencilFunc(uint func, int ref_notkeword, uint mask);
        [DllImport("opengl32.dll")]
        public static extern void glStencilMask(uint mask);
        [DllImport("opengl32.dll")]
        public static extern void glStencilOp(uint fail, uint zfail, uint zpass);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord1d(double s);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord1dv(double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord1f(float s);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord1fv(float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord1i(int s);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord1iv(int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord1s(short s);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord1sv(short[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord2d(double s, double t);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord2dv(double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord2f(float s, float t);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord2fv(float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord2i(int s, int t);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord2iv(int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord2s(short s, short t);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord2sv(short[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord3d(double s, double t, double r);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord3dv(double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord3f(float s, float t, float r);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord3fv(float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord3i(int s, int t, int r);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord3iv(int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord3s(short s, short t, short r);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord3sv(short[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord4d(double s, double t, double r, double q);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord4dv(double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord4f(float s, float t, float r, float q);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord4fv(float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord4i(int s, int t, int r, int q);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord4iv(int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord4s(short s, short t, short r, short q);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoord4sv(short[] v);
        [DllImport("opengl32.dll")]
        public static extern void glTexCoordPointer(int size, uint type, int stride, float[] pointer);
        [DllImport("opengl32.dll")]
        public static extern void glTexEnvf(uint target, uint pname, float param);
        [DllImport("opengl32.dll")]
        public static extern void glTexEnvfv(uint target, uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glTexEnvi(uint target, uint pname, int param);
        [DllImport("opengl32.dll")]
        public static extern void glTexEnviv(uint target, uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glTexGend(uint coord, uint pname, double param);
        [DllImport("opengl32.dll")]
        public static extern void glTexGendv(uint coord, uint pname, double[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glTexGenf(uint coord, uint pname, float param);
        [DllImport("opengl32.dll")]
        public static extern void glTexGenfv(uint coord, uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glTexGeni(uint coord, uint pname, int param);
        [DllImport("opengl32.dll")]
        public static extern void glTexGeniv(uint coord, uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glTexImage1D(uint target, int level, int internalformat, int width, int border, uint format, uint type, byte[] pixels);
        [DllImport("opengl32.dll")]
        public static extern void glTexImage2D(uint target, int level, int internalformat, int width, int height, int border, uint format, uint type, byte[] pixels);
        [DllImport("opengl32.dll")]
        public static extern void glTexImage2D(uint target, int level, int internalformat, int width, int height, int border, uint format, uint type, IntPtr pixels);
        [DllImport("opengl32.dll")]
        public static extern void glTexParameterf(uint target, uint pname, float param);
        [DllImport("opengl32.dll")]
        public static extern void glTexParameterfv(uint target, uint pname, float[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glTexParameteri(uint target, uint pname, int param);
        [DllImport("opengl32.dll")]
        public static extern void glTexParameteriv(uint target, uint pname, int[] params_notkeyword);
        [DllImport("opengl32.dll")]
        public static extern void glTexSubImage1D(uint target, int level, int xoffset, int width, uint format, uint type, int[] pixels);
        [DllImport("opengl32.dll")]
        public static extern void glTexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, byte[] pixels);
        [DllImport("opengl32.dll")]
        public static extern void glTranslated(double x, double y, double z);
        [DllImport("opengl32.dll")]
        public static extern void glTranslatef(float x, float y, float z);
        [DllImport("opengl32.dll")]
        public static extern void glVertex2d(double x, double y);
        [DllImport("opengl32.dll")]
        public static extern void glVertex2dv(double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glVertex2f(float x, float y);
        [DllImport("opengl32.dll")]
        public static extern void glVertex2fv(float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glVertex2i(int x, int y);
        [DllImport("opengl32.dll")]
        public static extern void glVertex2iv(int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glVertex2s(short x, short y);
        [DllImport("opengl32.dll")]
        public static extern void glVertex2sv(short[] v);
        [DllImport("opengl32.dll")]
        public static extern void glVertex3d(double x, double y, double z);
        [DllImport("opengl32.dll")]
        public static extern void glVertex3dv(double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glVertex3f(float x, float y, float z);
        [DllImport("opengl32.dll")]
        public static extern void glVertex3fv(float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glVertex3i(int x, int y, int z);
        [DllImport("opengl32.dll")]
        public static extern void glVertex3iv(int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glVertex3s(short x, short y, short z);
        [DllImport("opengl32.dll")]
        public static extern void glVertex3sv(short[] v);
        [DllImport("opengl32.dll")]
        public static extern void glVertex4d(double x, double y, double z, double w);
        [DllImport("opengl32.dll")]
        public static extern void glVertex4dv(double[] v);
        [DllImport("opengl32.dll")]
        public static extern void glVertex4f(float x, float y, float z, float w);
        [DllImport("opengl32.dll")]
        public static extern void glVertex4fv(float[] v);
        [DllImport("opengl32.dll")]
        public static extern void glVertex4i(int x, int y, int z, int w);
        [DllImport("opengl32.dll")]
        public static extern void glVertex4iv(int[] v);
        [DllImport("opengl32.dll")]
        public static extern void glVertex4s(short x, short y, short z, short w);
        [DllImport("opengl32.dll")]
        public static extern void glVertex4sv(short[] v);
        [DllImport("opengl32.dll")]
        public static extern void glVertexPointer(int size, uint type, int stride, float[] pointer);
        [DllImport("opengl32.dll")]
        public static extern void glViewport(int x, int y, int width, int height);


    }

    public static partial class OpenGL
    {
        public const int GL_RESCALE_NORMAL = 0x803A;
        public const int GL_CLAMP_TO_EDGE = 0x812F;
        public const int GL_MAX_ELEMENTS_VERTICES = 0x80E8;
        public const int GL_MAX_ELEMENTS_INDICES = 0x80E9;
        public const int GL_BGR = 0x80E0;
        public const int GL_BGRA = 0x80E1;
        public const int GL_UNSIGNED_BYTE_3_3_2 = 0x8032;
        public const int GL_UNSIGNED_BYTE_2_3_3_REV = 0x8362;
        public const int GL_UNSIGNED_SHORT_5_6_5 = 0x8363;
        public const int GL_UNSIGNED_SHORT_5_6_5_REV = 0x8364;
        public const int GL_UNSIGNED_SHORT_4_4_4_4 = 0x8033;
        public const int GL_UNSIGNED_SHORT_4_4_4_4_REV = 0x8365;
        public const int GL_UNSIGNED_SHORT_5_5_5_1 = 0x8034;
        public const int GL_UNSIGNED_SHORT_1_5_5_5_REV = 0x8366;
        public const int GL_UNSIGNED_INT_8_8_8_8 = 0x8035;
        public const int GL_UNSIGNED_INT_8_8_8_8_REV = 0x8367;
        public const int GL_UNSIGNED_INT_10_10_10_2 = 0x8036;
        public const int GL_UNSIGNED_INT_2_10_10_10_REV = 0x8368;
        public const int GL_LIGHT_MODEL_COLOR_CONTROL = 0x81F8;
        public const int GL_SINGLE_COLOR = 0x81F9;
        public const int GL_SEPARATE_SPECULAR_COLOR = 0x81FA;
        public const int GL_TEXTURE_MIN_LOD = 0x813A;
        public const int GL_TEXTURE_MAX_LOD = 0x813B;
        public const int GL_TEXTURE_BASE_LEVEL = 0x813C;
        public const int GL_TEXTURE_MAX_LEVEL = 0x813D;
        public const int GL_SMOOTH_POINT_SIZE_RANGE = 0x0B12;
        public const int GL_SMOOTH_POINT_SIZE_GRANULARITY = 0x0B13;
        public const int GL_SMOOTH_LINE_WIDTH_RANGE = 0x0B22;
        public const int GL_SMOOTH_LINE_WIDTH_GRANULARITY = 0x0B23;
        public const int GL_ALIASED_POINT_SIZE_RANGE = 0x846D;
        public const int GL_ALIASED_LINE_WIDTH_RANGE = 0x846E;
        public const int GL_PACK_SKIP_IMAGES = 0x806B;
        public const int GL_PACK_IMAGE_HEIGHT = 0x806C;
        public const int GL_UNPACK_SKIP_IMAGES = 0x806D;
        public const int GL_UNPACK_IMAGE_HEIGHT = 0x806E;
        public const int GL_TEXTURE_3D = 0x806F;
        public const int GL_PROXY_TEXTURE_3D = 0x8070;
        public const int GL_TEXTURE_DEPTH = 0x8071;
        public const int GL_TEXTURE_WRAP_R = 0x8072;
        public const int GL_MAX_3D_TEXTURE_SIZE = 0x8073;
        public const int GL_TEXTURE_BINDING_3D = 0x806A;
    }

    public static partial class OpenGL
    {
        public static void ErrorCheck() {
            for (var err = OpenGL.glGetError(); err != OpenGL.GL_NO_ERROR; err = OpenGL.glGetError()) {
                switch (err) {
                    case OpenGL.GL_INVALID_ENUM:
                        Console.WriteLine("GL_INVALID_ENUM");
                        break;
                    case OpenGL.GL_INVALID_VALUE:
                        Console.WriteLine("GL_INVALID_VALUE");
                        break;
                    case OpenGL.GL_INVALID_OPERATION:
                        Console.WriteLine("GL_INVALID_OPERATION");
                        break;
                    case OpenGL.GL_STACK_OVERFLOW:
                        Console.WriteLine("GL_STACK_OVERFLOW");
                        break;
                    case OpenGL.GL_STACK_UNDERFLOW:
                        Console.WriteLine("GL_STACK_UNDERFLOW");
                        break;
                    case OpenGL.GL_OUT_OF_MEMORY:
                        Console.WriteLine("GL_OUT_OF_MEMORY");
                        break;
                    default:
                        Console.WriteLine(err);
                        break;
                }
            }
        }
    }
}
