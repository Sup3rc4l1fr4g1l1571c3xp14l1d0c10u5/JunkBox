using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;

namespace GLPainter
{
    using GLfloat = Single;
    using GLbitfield = UInt32;
    using GLFWwindow = IntPtr;
    using GLenum = UInt32;
    using GLboolean = Byte;
    using GLint = Int32;
    using GLuint = UInt32;
    using GLsizei = Int32;
    using GLsizeiptr = Int32;   // 32bit

    class Program
    {
        static void Main(string[] args) {

            var v1 = new PointF(100,45);
            var m_ = Matrix3.translation(10, 0);
            var p = Matrix3.projection(640, 480);
            var mp = Matrix3.multiply(m_, p);
            var v2 = Matrix3.transform(mp, v1);
            var v3 = Matrix3.transform(Matrix3.inverse(mp), v2);

            // GLFW の初期化 (GLFW)
            if (GLFW.glfwInit() == OpenGL.GL_FALSE) {
                throw new Exception("Can't initialize GLFW");
            }

            // Using OpenGL 4.0 Core Profile Only (disable backward compatibility)
            GLFW.glfwWindowHint(GLFW.GLFW_CONTEXT_VERSION_MAJOR, 3);
            GLFW.glfwWindowHint(GLFW.GLFW_CONTEXT_VERSION_MINOR, 2);
            GLFW.glfwWindowHint(GLFW.GLFW_OPENGL_FORWARD_COMPAT, OpenGL.GL_TRUE);
            GLFW.glfwWindowHint(GLFW.GLFW_OPENGL_PROFILE, GLFW.GLFW_OPENGL_CORE_PROFILE);

            // ウィンドウを作成 (GLFW)
            GLFWwindow window = GLFW.glfwCreateWindow(640, 480, "GLPainter", IntPtr.Zero, IntPtr.Zero);
            if (window == IntPtr.Zero) {
                throw new Exception("Can't create GLFW window.");
            }

            // 作成したウィンドウを処理対象とする (GLFW)
            GLFW.glfwMakeContextCurrent(/* GLFWwindow *  window = */ window);

            // Initialize GLEW
            // You must place the glewInit() immediately after glfwMakeContextCurrent()! !! MUST !!
            GLEW.glewExperimental = OpenGL.GL_TRUE;
            if (GLEW.glewInit() != GLEW.GLEW_OK) {
                throw new Exception("Failed to initialize GLEW.");
            }

            // 頂点配列オブジェクト
            GLuint[] vao = new GLuint[1];
            OpenGL.glGenVertexArrays(1, vao);
            OpenGL.glBindVertexArray(vao[0]);

            ImageProcessing app = new ImageProcessing();
            {
                GLFW.glfwSetWindowSizeCallback(window, (w, width, height) => {
                    int renderBufferWidth, renderBufferHeight;
                    GLFW.glfwGetFramebufferSize(window, out renderBufferWidth, out renderBufferHeight);

                    app.Resize(renderBufferWidth, renderBufferHeight);

                    // ビューポート変換の更新
                    OpenGL.glViewport(0, 0, renderBufferWidth, renderBufferHeight);

                });
                {
                    int renderBufferWidth, renderBufferHeight;
                    GLFW.glfwGetFramebufferSize(window, out renderBufferWidth, out renderBufferHeight);

                    app.Resize(renderBufferWidth, renderBufferHeight);

                    // ビューポート変換の更新
                    OpenGL.glViewport(0, 0, renderBufferWidth, renderBufferHeight);

                }
            }

            ImageProcessing.Surface surf1 = new ImageProcessing.Surface(Image.FromFile("1.png"));
            ImageProcessing.Surface surf2 = new ImageProcessing.Surface(Image.FromFile("4.png"));
            ImageProcessing.Surface surf3 = new ImageProcessing.Surface(640, 480);

            app.draw(surf3, surf2);
            //app.draw(surf3, surf2, new ImageProcessing.DrawOption() { blendMode = ImageProcessing.BlendMode.Additive });
            //app.drawLines(surf3, new ImageProcessing.DrawLinesOption() { color = new byte[] { 255, 0, 0, 255 }, blendMode = ImageProcessing.BlendMode.Normal, compositeMode = ImageProcessing.CompositeMode.Normal, vertexes = new float[] { 160, 120, 480, 120, 480, 360, 160, 480, 160, 120 } });
            var m = Matrix3.translation(-50, 0);
            var m2 = Matrix3.rotation(5 * (float)Math.PI / 180.0f);
            //var m3 = Matrix3.multiply(m2, m);
            app.fillRect(surf3, new ImageProcessing.FillRectOption() { color = new byte[] { 255, 255, 0, 255 }, blendMode = ImageProcessing.BlendMode.Normal, compositeMode = ImageProcessing.CompositeMode.Normal, start = new PointF(160, 120), end = new PointF(480, 360), matrix = m2 });
            app.fillRect(surf3, new ImageProcessing.FillRectOption() { color = new byte[] { 255, 0, 0, 128}, blendMode = ImageProcessing.BlendMode.Normal, compositeMode = ImageProcessing.CompositeMode.Normal, stencilMode = ImageProcessing.StencilMode.Ellipse, start = new PointF(160, 120), end = new PointF(480, 360), matrix = m2 });

            var pb = new ImageProcessing.PixelBuffer(640, 480);
            var pixels = pb.capture(surf3);
            var bitmap = new Bitmap(640, 480, PixelFormat.Format32bppArgb);
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            var ptr = bmpData.Scan0;
            for (var y = 0; y < bmpData.Height; y++) {
                var x = y * bmpData.Width * 4;
                for (var i = 0; i < bmpData.Width; i++) {
                    var r = pixels[x + 0];
                    var g = pixels[x + 1];
                    var b = pixels[x + 2];
                    var a = pixels[x + 3];
                    pixels[x + 3] = a;
                    pixels[x + 2] = r;
                    pixels[x + 1] = g;
                    pixels[x + 0] = b;
                    x += 4;
                }
                Marshal.Copy(pixels, y * bmpData.Width * 4, ptr + y * bmpData.Stride, bmpData.Width * 4);
            }
            bitmap.UnlockBits(bmpData);
            bitmap.Save("captured.bmp");


            // ウィンドウが開いている間繰り返す
            while (GLFW.glfwWindowShouldClose(window) == OpenGL.GL_FALSE) {
                // バックバッファ消去
                OpenGL.glClear(OpenGL.GL_COLOR_BUFFER_BIT);

                // 描画処理
                app.draw(null, surf3, null);

                // カラーバッファ入れ替え <= ダブルバッファリング (GLFW)
                GLFW.glfwSwapBuffers(window);

                // イベント待ち (GLFW)
                GLFW.glfwWaitEvents();
            }

            return;
        }
    }
    public class ImageProcessing
    {


        /// <summary>
        /// GLSLプログラム
        /// </summary>
        public class Program : IDisposable
        {
            public uint programId { get; private set; }
            public Shader[] shaders { get; private set; }

            public Program(uint program, Shader[] shaders) {
                this.programId = program;
                this.shaders = shaders;
            }

            public static Program Create(params Shader[] shaders) {
                var program = OpenGL.glCreateProgram();
                foreach (var shader in shaders) {
                    OpenGL.glAttachShader(program, shader.shaderId);
                }
                OpenGL.glLinkProgram(program);

                var linked = OpenGL.glGetProgramParameter(program, OpenGL.GL_LINK_STATUS);
                if (linked == 0) {
                    // something went wrong with the link
                    var lastError = OpenGL.glGetProgramInfoLog_(program);
                    OpenGL.glDeleteProgram(program);
                    throw new Exception("Error in program linking:" + lastError);
                }
                return new Program(program, shaders);
            }

            public void Dispose() {
                OpenGL.glDeleteProgram(programId); programId = 0;
                foreach (var shader in shaders) {
                    OpenGL.glDeleteShader(shader.shaderId);
                }
                shaders = null;
            }
        }

        /// <summary>
        /// GLSLシェーダー
        /// </summary>
        public class Shader     : IDisposable
        {
            public uint shaderId { get; private set; }
            public uint shaderType { get; private set; }

            public Shader(uint shaderId, uint shaderType) {
                this.shaderId = shaderId;
                this.shaderType = shaderType;
            }

            public static Shader Create(string shaderSource, GLenum shaderType) {
                var shader = OpenGL.glCreateShader(shaderType);
                OpenGL.glShaderSource_(shader, shaderSource);
                OpenGL.glCompileShader(shader);
                var compiled = OpenGL.glGetShaderParameter(shader, OpenGL.GL_COMPILE_STATUS);
                if (compiled == 0) {
                    var lastError = OpenGL.glGetShaderInfoLog_(shader);
                    OpenGL.glDeleteShader(shader);
                    throw new Exception("*** Error compiling shader '" + shader + "':" + lastError);
                } else {
                    return new Shader(shader, shaderType);
                }
            }

            public void Dispose() {
                OpenGL.glDeleteShader(this.shaderId);
                this.shaderId = 0;
                this.shaderType = 0;
            }
        }

        /// <summary>
        /// ピクセルバッファ
        /// </summary>
        public class PixelBuffer : IDisposable
        {
            public GLuint pbo { get; private set; }
            public byte[] data { get; private set; }
            public int width { get; private set; }
            public int height { get; private set; }

            public PixelBuffer(int width, int height) {
                this.width = width;
                this.height = height;
                GLuint[] ret = new GLuint[1];
                OpenGL.glCreateBuffers(1, ret);
                this.pbo = ret[0];
                this.data = new byte[this.width * this.height * 4];
                OpenGL.glBindBuffer(OpenGL.GL_PIXEL_PACK_BUFFER, this.pbo);
                OpenGL.glBufferData(OpenGL.GL_PIXEL_PACK_BUFFER, this.width * this.height * 4, null, OpenGL.GL_STREAM_READ);
                OpenGL.glBindBuffer(OpenGL.GL_PIXEL_PACK_BUFFER, 0);
            }
            public byte[] capture(Surface src) {
                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, src.framebuffer);
                OpenGL.glViewport(0, 0, this.width, this.height);

                // フレームバッファをピクセルバッファにロード
                OpenGL.glBindBuffer(OpenGL.GL_PIXEL_PACK_BUFFER, this.pbo);
                OpenGL.glReadPixels(0, 0, this.width, this.height, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, null);
                OpenGL.glFlush();
                OpenGL.glFinish();

                // ピクセルバッファからCPU側の配列にロード
                OpenGL.glGetBufferSubData(OpenGL.GL_PIXEL_PACK_BUFFER, 0, this.data.Length, this.data);
                OpenGL.glBindBuffer(OpenGL.GL_PIXEL_PACK_BUFFER, 0);

                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
                return this.data;
            }

            public void Dispose() {
                OpenGL.glDeleteBuffers(1, new[] { this.pbo });
                this.pbo = 0;
                this.data = null;
                this.width = 0;
                this.height = 0;
            }
        }


        GLuint positionBuffer;
        GLuint texcoordBuffer;
        ImageProcessing.Program program;

        List<Surface> cachedSurface = new List<Surface>();

        private int canvasWidth, canvasHeight;

        public void Resize(int width, int height) {
            canvasWidth = width;
            canvasHeight = height;
        }

        private static string EnumToDefine<T>(string separator = "_") {
            var ty = typeof(T);
            var tyName = ty.Name;
            return String.Join("\r\n", Enum.GetNames(ty).Select(x => $"#define {tyName}{separator}{x} ({(int)Enum.Parse(ty, x)})"));
        }

        private static readonly string vertexShaderSrc = @"
#version 330 

in vec2 a_position;
in vec2 a_texCoord;

uniform mat3 u_matrix;

out vec2 v_texCoord;

void main() {
    gl_Position = vec4((u_matrix * vec3(a_position, 1)).xy, 0, 1);
    v_texCoord = a_texCoord;
}
";
        private static readonly string fragmentShaderSrc = @"
#version 330 
precision mediump float;

" + EnumToDefine<InputType>() + @"
" + EnumToDefine<BlendMode>() + @"
" + EnumToDefine<CompositeMode>() + @"
" + EnumToDefine<StencilMode>() + @"

/* 前景 */
uniform int u_front_mode;
uniform sampler2D u_front;  /* front_mode == InputType_Texture : テクスチャ */
uniform vec4  u_color;      /* front_mode == InputType_Color   : 色 */

/* 背景 */
uniform int u_back_mode;     
uniform sampler2D u_back;   /* back_mode == InputType_Texture : テクスチャ */

/* 出力先大きさ */
uniform vec2 u_dstsize;

/* 混合モード */
uniform int u_blend_mode;

/* 合成モード */
uniform int u_composit_mode;

/* ステンシルモード */
uniform int u_stencil_mode;

/**
 * ステンシル用パラメータ 
 * チェッカーボード：専用処理なのでパラメータなし
 * 線分：始点:u_start、終点:u_end、幅:u_size、アンチエイリアス率:u_anti_aliasing_rate
 * 楕円：矩形:u_start、u_end、アンチエイリアス率:u_anti_aliasing_rate
 */
uniform vec2  u_start;
uniform vec2  u_end;
uniform float u_size;
uniform float u_anti_aliasing_rate;

uniform  mat3 u_invMatrix;

/* 入出力 */
in  vec2 v_texCoord;
out vec4 fragColor;

/**
 * @brief   アルファ値を考慮した合成演算
 * @param   front   前景(上のレイヤー)の色
 * @param   back    背景(下のレイヤー)の色
 * @param   blended 背景色と前景色をブレンドモードに従って合成した色
 * @retval  合成結果
 */
vec4 composit_normal(vec4 front, vec4 back, vec3 blended) {
    float alpha_f = front.a;    // 前景のアルファ
    float alpha_b = back.a;     // 背景のアルファ

    vec3 C_f = front.rgb;
    vec3 C_b = back.rgb;
    vec3 C_r = blended;

    // 色の重み
    float alpha_1 = alpha_f * alpha_b;
    float alpha_2 = alpha_f * (1.0 - alpha_b);
    float alpha_3 = (1.0 - alpha_f) * alpha_b;

    float A = alpha_1 + alpha_2 + alpha_3;
    vec3 C;
    if (A == 0.0) {
        C = vec3(0,0,0);
    } else {
        C = ((alpha_1 * C_r) + (alpha_2 * C_f) + (alpha_3 * C_b)) / A;
    }
    return vec4(C,A);
}

/**
 * @brief   消しゴム演算
 * @param   front   前景(上のレイヤー)の色（α値を消しゴムの強さとして利用しており、rgbは使わない）
 * @param   back    背景(下のレイヤー)の色
 * @retval  合成結果
 */
vec4 composit_erase(vec4 front, vec4 back) {
    float alpha_f = front.a;    // 前景のアルファ
    float alpha_b = back.a;     // 背景のアルファ

    // 背景の色
    vec3 C_b = back.rgb;

    // 色の重み
    float alpha_3 = (1.0 - alpha_f) * alpha_b;

    float A = alpha_3;
    vec3 C;
    if (A == 0.0) {
        C = vec3(0,0,0);
    } else {
        C = C_b;
    }
    return vec4(C, A);
}

vec4 composit_copyFront(vec4 front, vec4 back, vec3 blended) {
    return front;
}

vec4 composit_maxAlpha(vec4 front, vec4 back) {
    return vec4(front.rgb, max(front.a, back.a));
}

vec4 stencil_checkerBoard() {
    vec2 rads = step(0.0, cos(radians(gl_FragCoord.xy * 360.0/16.0)));
    float c = (255.0 - abs(rads.x - rads.y) * 32.0) / 255.0;
    return vec4(c, c, c, 1.0);
}

vec4 stencil_Line(vec4 color) {

    vec2 a = u_start;
    vec2 b = u_end;
    vec2 c = gl_FragCoord.xy;

    vec2 u = b - a;
    vec2 v = c - a;
    float r = clamp(dot(u,v) / dot(u,u), 0.0, 1.0);
    vec2 p = a + r * u;
    float dist = distance(p, c);

    float aa = 1.0 - clamp((dist - (u_size * u_anti_aliasing_rate)) / (u_size * (1.0-u_anti_aliasing_rate)),0.0,1.0);

    return vec4(color.rgb, aa* color.a);
}

vec4 stencil_Ellipse(vec4 color) {
    // 描画する楕円は 方程式 (x^2)/(w^2) + (y^2)/(h^2) = 1 で示される
    vec2 c  = (u_end + u_start) * 0.5;
    vec2 wh = abs(u_end - u_start) * 0.5;
    //vec2 xy = gl_FragCoord.xy-c;
    vec2 xy = (u_invMatrix* vec3(gl_FragCoord.xy, 1)).xy-c;
    vec2 v = (xy * xy) / (wh * wh);
    float dist = (v.x + v.y);

    //float aa = 1.0 - clamp((dist - (u_size * u_anti_aliasing_rate)) / (u_size * (1.0-u_anti_aliasing_rate)),0.0,1.0);

    float th = clamp(1.0 - u_anti_aliasing_rate, 0.0, 1.0);
    if (dist <= th) {
        return color;
    } else {
        if (u_anti_aliasing_rate == 0) {
            return vec4(color.rgb, 0);
        } else {
            float aa = (dist-th) / u_anti_aliasing_rate;
            return vec4(color.rgb, aa * color.a);
        }
    }

}

/**
 * 入力読み取り
 */
vec4 read_input(int mode, sampler2D tex, vec2 texCoord, vec4 color) {
    switch (mode) {
        case InputType_Texture:
            return texture(tex, texCoord);
        case InputType_Color:
            return color;
        case InputType_Disable:
        default:
            return vec4(0.0,0.0,0.0,0.0);
    }
}

/**
 * 混合演算
 */
vec3 blend(int mode, vec4 front, vec4 back) {
    switch (mode) {
        case BlendMode_Normal:  // 不透明度を持たない色同士の混合(通常合成)
            return front.rgb;
        case BlendMode_Additive: // 不透明度を持たない色同士の混合(加算合成)
            return clamp(front.rgb + back.rgb,vec3(0.0,0.0,0.0),vec3(1.0,1.0,1.0));
        case BlendMode_Disable:
        default:
            return vec3(0.0,0.0,0.0);
    }
}

/**
 * 形状処理
 */
vec4 stencil(int mode, vec4 front, vec4 back) {
    switch (mode) {
        case StencilMode_CheckerBoard: {
            return stencil_checkerBoard();
        }
        case StencilMode_DrawLine: {
            return stencil_Line(front);
        }
        case StencilMode_Ellipse: {
            return stencil_Ellipse(front);
        }
        case StencilMode_Disable: 
        default: {
            return front;
        }
    }
}

/**
 * 合成処理
 */
vec4 composite(int mode, vec4 front, vec4 back, vec3 blended) {
    switch (mode) {
        case CompositeMode_Normal: {          /* α合成 */
            vec4 color = composit_normal(front, back, blended);
            return color;
        }
        case CompositeMode_Erase: {           /* 消しゴム（α値のみ反転乗算合成） */
            vec4 color = composit_erase(front, back);
            return color;
        }
        case CompositeMode_Copy: {            /* 前景色で上書き */
            vec4 color = front;
            return color;
        }
        case CompositeMode_MaxAlpha: {        /* 色は前景色で上書きするが、α値は背景色と前景色の大きいほうを採用する。単色アンチエイリアスブラシのステンシル生成で使う。 */
            vec4 color = composit_maxAlpha(u_color, back);
            return color;
        }
        default: {
            return vec4(0.0,0.0,0.0,0.0);
        }
    }
}


void main() {
    /* 前景色 */
    vec4 f = read_input(u_front_mode, u_front, v_texCoord, u_color);

    /* 背景色 */
    vec4 b = read_input(u_back_mode, u_back, gl_FragCoord.xy / u_dstsize, vec4(0,0,0,0) );

    /* 混合 */
    vec3 blended = blend(u_blend_mode, f, b);

    /* ステンシル */
    vec4 f2 = stencil(u_stencil_mode, f, b);

    /* 合成 */
    fragColor = composite(u_composit_mode, f2, b, blended);

}
";
        public ImageProcessing() {
            // 背景色 (OpenGL)
            OpenGL.glClearColor(0.2f, 0.2f, 0.2f, 0.0f);

            // シェーダをロード＆設定
            var vertexShader = Shader.Create(vertexShaderSrc, OpenGL.GL_VERTEX_SHADER);
            var fragmentShader = Shader.Create(fragmentShaderSrc, OpenGL.GL_FRAGMENT_SHADER);
            program = Program.Create(vertexShader, fragmentShader);
            OpenGL.glUseProgram(program.programId);

            // attribute変数の位置を取得
            var positionLocation = OpenGL.glGetAttribLocation(program.programId, "a_position");
            var texcoordLocation = OpenGL.glGetAttribLocation(program.programId, "a_texCoord");

            // 頂点バッファ（座標）を作成
            positionBuffer = OpenGL.glCreateBuffer_(1)[0];
            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, positionBuffer);
            OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER, 4096, null, OpenGL.GL_STREAM_DRAW);

            // シェーダの頂点座標Attributeを有効化
            OpenGL.glEnableVertexAttribArray((uint)positionLocation);

            // 頂点座標Attributeの位置情報を設定
            OpenGL.glVertexAttribPointer(
                (uint)positionLocation,
                2,          // 2 components per iteration
                OpenGL.GL_FLOAT,    // the data is 32bit floats
                OpenGL.GL_FALSE,    // don"t normalize the data
                0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
                null           // start at the beginning of the buffer
            );

            // 頂点バッファ（テクスチャ座標）を作成
            texcoordBuffer = OpenGL.glCreateBuffer_(1)[0];
            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, texcoordBuffer);
            OpenGL.glBufferData(OpenGL.GL_ARRAY_BUFFER, 4096, null, OpenGL.GL_STREAM_DRAW);

            // シェーダのテクスチャ座標Attributeを有効化
            OpenGL.glEnableVertexAttribArray((uint)texcoordLocation);
            // テクスチャ座標Attributeの位置情報を設定
            OpenGL.glVertexAttribPointer(
                (uint)texcoordLocation,
                2,          // 2 components per iteration
                OpenGL.GL_FLOAT,   // the data is 32bit floats
                OpenGL.GL_FALSE,      // don"t normalize the data
                0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
                null           // start at the beginning of the buffer
            );

        }


        private void setVertexPosition(IList<float> array) {
            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, this.positionBuffer);
            var byteArray = array.SelectMany(x => BitConverter.GetBytes(x)).ToArray();
            OpenGL.glBufferSubData(OpenGL.GL_ARRAY_BUFFER, 0, byteArray.Length, byteArray);
        }

        private void setTexturePosition(IList<float> array) {
            OpenGL.glBindBuffer(OpenGL.GL_ARRAY_BUFFER, this.texcoordBuffer);
            var byteArray = array.SelectMany(x => BitConverter.GetBytes(x)).ToArray();
            OpenGL.glBufferSubData(OpenGL.GL_ARRAY_BUFFER, 0, byteArray.Length, byteArray);
        }
        private Surface allocCachedTempOffScreen(int width, int height) {
            var index = this.cachedSurface.FindIndex(x => x.width == width && x.height == height);
            if (index != -1) {
                var surf = this.cachedSurface[index];
                this.cachedSurface.RemoveAt(index);
                surf.clear();
                return surf;
            } else {
                var surf = new Surface(width, height);
                surf.clear();
                return surf;

            }
        }
        private void freeCachedTempOffScreen(Surface surface) {
            if (this.cachedSurface.Count >= 5) {
                var surf = this.cachedSurface[0];
                this.cachedSurface.RemoveAt(0);
                surf.Dispose();
            }
            this.cachedSurface.Add(surface);
        }

        public Surface createBlankOffscreenTarget(int width, int height) {
            var surf = new Surface(width, height);
            surf.clear();
            return surf;
        }
        public Surface createOffscreenTargetFromImage(Image image) {
            return new Surface(image);
        }

        public static List<float> createRectangle(float x1, float y1, float x2, float y2, List<float> target = null) {
            if (target != null) {
                target.AddRange(new[] {
                x1, y1,
                x2, y1,
                x1, y2,
                x1, y2,
                x2, y1,
                x2, y2
            });
                return target;
            } else {
                return new List<float>() {
                x1, y1,
                x2, y1,
                x1, y2,
                x1, y2,
                x2, y1,
                x2, y2
            };
            }
        }

        // フィルタカーネルの重み合計（重み合計が0以下の場合は1とする）
        private static float computeKernelWeight(float[] kernel) {
            var weight = kernel.Sum();
            return weight <= 0 ? 1 : weight;
        }
        public void applyKernel(Surface dst, Surface src, IList<float> kernel = null) {
            // arrtibute変数の位置を取得
            var positionLocation = OpenGL.glGetAttribLocation(this.program.programId, "a_position");
            var texcoordLocation = OpenGL.glGetAttribLocation(this.program.programId, "a_texCoord");

            // uniform変数の位置を取得
            //var resolutionLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_resolution");
            var matrixLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_matrix");
            var textureSizeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_textureSize");
            var kernelLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_kernel[0]");
            var kernelWeightLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_kernelWeight");
            var texture0Location = OpenGL.glGetUniformLocation(this.program.programId, "texture0");

            // シェーダを設定

            // シェーダの頂点座標Attributeを有効化
            OpenGL.glEnableVertexAttribArray((uint)positionLocation);
            // 頂点バッファ（座標）を設定
            this.setVertexPosition(ImageProcessing.createRectangle(0, 0, src.width, src.height));

            // 頂点座標Attributeの位置情報を設定
            // シェーダのテクスチャ座標Attributeを有効化
            OpenGL.glEnableVertexAttribArray((uint)texcoordLocation);
            // 頂点バッファ（テクスチャ座標）を設定
            this.setTexturePosition(ImageProcessing.createRectangle(0, 0, 1, 1));

            // 変換行列を設定
            var projectionMatrix = Matrix3.projection(dst.width, dst.height);
            OpenGL.glUniformMatrix3fv(matrixLocation, 1, OpenGL.GL_FALSE, projectionMatrix.ToArray());

            // フィルタ演算で使う入力テクスチャサイズを設定
            OpenGL.glUniform2f(textureSizeLocation, src.width, src.height);

            // 入力元とするレンダリングターゲットのテクスチャを選択
            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, src.texture);
            OpenGL.glUniform1i(texture0Location, 0);

            // 出力先とするレンダリングターゲットのフレームバッファを選択し、ビューポートを設定
            OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, dst.framebuffer);
            OpenGL.glViewport(0, 0, dst.width, dst.height);

            // カーネルシェーダを適用したオフスクリーンレンダリングを実行
            var k = kernel.ToArray();
            var weight = ImageProcessing.computeKernelWeight(k);
            OpenGL.glUniform1fv(kernelLocation, k.Length, k);
            OpenGL.glUniform1f(kernelWeightLocation, weight);
            OpenGL.glDrawArrays(OpenGL.GL_TRIANGLES, 0, 6);

            // フィルタ適用完了

        }
        public void copySurface(Surface dst, Surface src) {
            OpenGL.glFlush();
            OpenGL.glFinish();
            if (dst != null) {
                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, src.framebuffer);
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, dst.texture);
                OpenGL.glCopyTexSubImage2D(OpenGL.GL_TEXTURE_2D, 0, 0, 0, 0, 0, dst.width, dst.height);
                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
            } else {
                this.draw(null, src, new DrawOption() { compositeMode = CompositeMode.Copy });
            }
        }

        /// <summary>
        /// フラグメントシェーダーの入力種別
        /// </summary>
        public enum InputType
        {
            /// <summary>
            /// 入力なし。色を参照した場合rgba(0,0,0,0)となる。
            /// </summary>
            Disable,
            /// <summary>
            /// 入力はテクスチャ。テクスチャ座標の色になる。
            /// </summary>
            Texture,
            /// <summary>
            /// 入力は色。指定した色になる。
            /// </summary>
            Color,
        }


        /// <summary>
        /// 色の合成モード
        /// </summary>
        public enum BlendMode
        {
            /// <summary>
            /// 合成なし
            /// </summary>
            Disable,
            /// <summary>
            /// 通常合成
            /// </summary>
            Normal,
            /// <summary>
            /// 加算合成
            /// </summary>
            Additive
        }

        /// <summary>
        /// 合成モード
        /// </summary>
        public enum CompositeMode
        {
            Normal,
            Erase,  // back側のαをfront側のα分減算する。αが0になると黒にする。
            Copy,   // front側をそのまま転送
            MaxAlpha,   // RGBについてはfront側をそのまま転送。α値は
        }

        public enum StencilMode
        {
            Disable,        // ステンシルなし
            CheckerBoard,   // 背景用のチェッカーボード描画
            DrawLine,   // 線分ステンシル
            Ellipse,   // 楕円ステンシル
        }

        public class DrawLinesOption
        {
            public BlendMode blendMode = BlendMode.Disable;
            public StencilMode stencilMode = StencilMode.DrawLine;
            public CompositeMode compositeMode = CompositeMode.MaxAlpha;
            public float[] vertexes = null;
            public float size = 1.0f;
            public byte[] color = null;
            public float antialiasSize = 1.0f;
        }
        public void drawLines(Surface dst, DrawLinesOption opt = null) {
            if (opt == null) {
                opt = new DrawLinesOption();
            }
            //var tmp = this.allocCachedTempOffScreen(dst.width, dst.height);

            // arrtibute変数の位置を取得
            //var positionLocation = OpenGL.glGetAttribLocation(this.program.programId, "a_position");
            //var texcoordLocation = OpenGL.glGetAttribLocation(this.program.programId, "a_texCoord");

            // uniform変数の位置を取得
            var matrixLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_matrix");
            var startLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_start");
            var endLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_end");
            var sizeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_size");
            var antiAliasingRateLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_anti_aliasing_rate");

            var frontModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_front_mode");
            var texture0Location = OpenGL.glGetUniformLocation(this.program.programId, "u_front");
            var colorLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_color");

            var backModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_back_mode");
            var texture1Location = OpenGL.glGetUniformLocation(this.program.programId, "u_back");

            var dstsizeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_dstsize");

            var blendModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_blend_mode");
            var compositeModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_composit_mode");
            var stencilModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_stencil_mode");

            // 色を設定
            OpenGL.glUniform4fv(colorLocation, 1, opt.color.Select(x => x / 255.0f).ToArray());

            // 投影行列を設定
            var projectionMatrix = Matrix3.projection(dst.width, dst.height);
            OpenGL.glUniformMatrix3fv(matrixLocation, 1, OpenGL.GL_FALSE, projectionMatrix.ToArray());

            // 入力元とするレンダリングターゲット=dst
            OpenGL.glUniform1i(frontModeLocation, (int)InputType.Color);
            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, 0);
            OpenGL.glUniform1i(texture0Location, 0);

            OpenGL.glUniform1i(backModeLocation, (int)InputType.Texture);
            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE1);
            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, dst.texture);
            OpenGL.glUniform1i(texture1Location, 1);

            // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
            OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, dst.framebuffer);
            OpenGL.glViewport(0, 0, dst.width, dst.height);

            // テクスチャサイズ情報ををシェーダのUniform変数に設定
            OpenGL.glUniform2f(dstsizeLocation, dst.width, dst.height);

            // 合成モードを設定
            OpenGL.glUniform1i(blendModeLocation, (int)opt.blendMode);
            OpenGL.glUniform1i(stencilModeLocation, (int)opt.stencilMode);
            OpenGL.glUniform1i(compositeModeLocation, (int)opt.compositeMode);

            var vertexes = opt.vertexes;
            var len = (vertexes.Length / 2) - 1;

            // サイズを設定
            OpenGL.glUniform1f(sizeLocation, opt.size / 2);

            // アンチエイリアスサイズを指定
            OpenGL.glUniform1f(antiAliasingRateLocation, opt.antialiasSize);

            // 矩形設定
            var vp = new List<float>();
            var tp = new List<float>();
            for (var i = 0; i < len; i++) {

                var x1 = vertexes[i * 2 + 0] + 0.5f;
                var y1 = vertexes[i * 2 + 1] + 0.5f;
                var x2 = vertexes[i * 2 + 2] + 0.5f;
                var y2 = vertexes[i * 2 + 3] + 0.5f;

                var left = (x1 < x2 ? x1 : x2) - opt.size;
                var top = (y1 < y2 ? y1 : y2) - opt.size;
                var right = (x1 > x2 ? x1 : x2) + opt.size;
                var bottom = (y1 > y2 ? y1 : y2) + opt.size;

                OpenGL.glUniform2f(startLocation, x1, y1);
                OpenGL.glUniform2f(endLocation, x2, y2);

                // 頂点バッファ（座標）を設定
                vp.Clear();
                this.setVertexPosition(ImageProcessing.createRectangle(left, top, right, bottom, vp));

                // 頂点バッファ（テクスチャ座標）を設定
                tp.Clear();
                this.setTexturePosition(ImageProcessing.createRectangle(left / dst.width, top / dst.height, right / dst.width, bottom / dst.height, tp));

                // シェーダを適用したオフスクリーンレンダリングを実行
                OpenGL.glDrawArrays(OpenGL.GL_TRIANGLES, 0, 6);

                // レンダリング範囲をフィードバック
                var clippedLeft = (left >= dst.width) ? dst.width - 1 : (left < 0) ? 0 : (int)left;
                var clippedTop = (top >= dst.height) ? dst.height - 1 : (top < 0) ? 0 : (int)top;
                var clippedRight = (right >= dst.width) ? dst.width - 1 : (right < 0) ? 0 : (int)right;
                var clippedBottom = (bottom >= dst.height) ? dst.height - 1 : (bottom < 0) ? 0 : (int)bottom;
                OpenGL.glCopyTexSubImage2D(OpenGL.GL_TEXTURE_2D, 0, clippedLeft, clippedTop, clippedLeft, clippedTop, clippedRight - clippedLeft, clippedBottom - clippedTop);
            }

            // フレームバッファの選択解除
            OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);


        }


        public class FillRectOption
        {
            public BlendMode blendMode = BlendMode.Disable;
            public StencilMode stencilMode = StencilMode.Disable;
            public CompositeMode compositeMode = CompositeMode.MaxAlpha;
            public PointF start;
            public PointF end;
            public byte[] color;
            public Matrix3 matrix = null;
        }

        public void fillRect(Surface dst, FillRectOption opt) {

            //var tmp = this.allocCachedTempOffScreen(dst.width, dst.height);
            //OpenGL.ErrorCheck();

            // arrtibute変数の位置を取得
            //var positionLocation = OpenGL.glgetAttribLocation(this.program.programId, "a_position");
            //var texcoordLocation = OpenGL.glgetAttribLocation(this.program.programId, "a_texCoord");

            // uniform変数の位置を取得
            var matrixLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_matrix");
            var invMatrixLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_invMatrix");
            
            var startLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_start");
            var endLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_end");
            var sizeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_size");
            var antiAliasingRateLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_anti_aliasing_rate");

            var frontModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_front_mode");
            var texture0Location = OpenGL.glGetUniformLocation(this.program.programId, "u_front");
            var colorLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_color");

            var backModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_back_mode");
            var texture1Location = OpenGL.glGetUniformLocation(this.program.programId, "u_back");

            var dstsizeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_dstsize");

            var blendModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_blend_mode");
            var compositeModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_composit_mode");
            var stencilModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_stencil_mode");

            OpenGL.ErrorCheck();

            // 色を設定
            OpenGL.glUniform4fv(colorLocation, 1, opt.color.Select(x => x / 255.0f).ToArray());
            OpenGL.ErrorCheck();

            // 投影行列を設定
            //var projectionMatrix = Matrix3.projection(dst.width, dst.height);
            var projectionMatrix = (opt.matrix == null)
                ? Matrix3.projection(dst.width, dst.height)
                : Matrix3.multiply(Matrix3.projection(dst.width, dst.height), opt.matrix); ;

            OpenGL.glUniformMatrix3fv(matrixLocation, 1, OpenGL.GL_FALSE, projectionMatrix.ToArray());
            OpenGL.glUniformMatrix3fv(invMatrixLocation, 1, OpenGL.GL_FALSE, (opt.matrix == null ? Matrix3.identity() : Matrix3.inverse(opt.matrix)).ToArray());
            OpenGL.ErrorCheck();

            // 入力元とするレンダリングターゲット
            OpenGL.glUniform1i(frontModeLocation, (int)InputType.Color);
            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, 0);
            OpenGL.glUniform1i(texture0Location, 0);
            OpenGL.ErrorCheck();

            OpenGL.glUniform1i(backModeLocation, (int)InputType.Texture);
            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE1);
            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, dst.texture);
            OpenGL.glUniform1i(texture1Location, 1);
            OpenGL.ErrorCheck();

            // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
            OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, dst.framebuffer);
            OpenGL.glViewport(0, 0, dst.width, dst.height);
            OpenGL.ErrorCheck();

            // テクスチャサイズ情報ををシェーダのUniform変数に設定
            OpenGL.glUniform2f(dstsizeLocation, dst.width, dst.height);
            OpenGL.ErrorCheck();

            // 合成モードを設定
            OpenGL.glUniform1i(blendModeLocation, (int)opt.blendMode);
            OpenGL.glUniform1i(stencilModeLocation, (int)opt.stencilMode);
            OpenGL.glUniform1i(compositeModeLocation, (int)opt.compositeMode);
            OpenGL.ErrorCheck();

            // アンチエイリアスサイズを指定
            OpenGL.glUniform1f(antiAliasingRateLocation, 0);
            OpenGL.ErrorCheck();

            // 矩形設定

            var x1 = opt.start.X + 0.5f;
            var y1 = opt.start.Y + 0.5f;
            var x2 = opt.end.X + 0.5f;
            var y2 = opt.end.Y + 0.5f;

            var left = Math.Min(x1, x2);
            var top = Math.Min(y1, y2);
            var right = Math.Max(x1, x2);
            var bottom = Math.Max(y1, y2);

            // 頂点位置設定
            OpenGL.glUniform2f(startLocation, x1, y1);
            OpenGL.glUniform2f(endLocation, x2, y2);

            // 頂点バッファ（テクスチャ座標）を設定
            this.setVertexPosition(ImageProcessing.createRectangle(left, top, right, bottom));
            OpenGL.ErrorCheck();

            // 頂点バッファ（テクスチャ座標）を設定
            this.setTexturePosition(ImageProcessing.createRectangle(left / dst.width, top / dst.height, right / dst.width, bottom / dst.height));
            OpenGL.ErrorCheck();

            // シェーダを適用したオフスクリーンレンダリングを実行
            OpenGL.glDrawArrays(OpenGL.GL_TRIANGLES, 0, 6);
            OpenGL.ErrorCheck();

            var clippedLeft = (left < 0) ? 0 : (left > dst.width - 1) ? dst.width - 1 : left;
            var clippedTop = (top < 0) ? 0 : (top > dst.height - 1) ? dst.height - 1 : top;
            var clippedRight = (right < 0) ? 0 : (right > dst.width - 1) ? dst.width - 1 : right;
            var clippedBottom = (bottom < 0) ? 0 : (bottom > dst.height - 1) ? dst.height - 1 : bottom;
            OpenGL.glCopyTexSubImage2D(OpenGL.GL_TEXTURE_2D, 0, (int)clippedLeft, (int)clippedTop, (int)clippedLeft, (int)clippedTop, (int)(clippedRight - clippedLeft), (int)(clippedBottom - clippedTop));
            OpenGL.ErrorCheck();

            // フレームバッファの選択解除
            OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);

            //this.freeCachedTempOffScreen(tmp);
            OpenGL.ErrorCheck();

        }

        public class DrawOption
        {
            public BlendMode blendMode = BlendMode.Normal;
            public StencilMode stencilMode = StencilMode.Disable;
            public CompositeMode compositeMode = CompositeMode.Normal;
            public PointF? start = null;
            public PointF? end = null;
            public Matrix3 matrix = null;
        }

        // dest に frontを描画
        public void draw(Surface dst, Surface front, DrawOption opt = null) {

            if (opt == null) {
                opt = new DrawOption();
            }

            // arrtibute変数の位置を取得
            //var positionLocation = OpenGL.glgetAttribLocation(this.program.programId, "a_position");
            //var texcoordLocation = OpenGL.glgetAttribLocation(this.program.programId, "a_texCoord");

            // uniform変数の位置を取得
            var matrixLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_matrix");

            var frontModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_front_mode");
            var texture0Location = OpenGL.glGetUniformLocation(this.program.programId, "u_front");
            var colorLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_color");

            var backModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_back_mode");
            var texture1Location = OpenGL.glGetUniformLocation(this.program.programId, "u_back");

            var dstsizeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_dstsize");

            var blendModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_blend_mode");
            var compositeModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_composit_mode");
            var stencilModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_stencil_mode");


            Surface tmpSurface = null;
            if (dst == front) {
                tmpSurface = front = this.allocCachedTempOffScreen(front.width, front.height);
                this.copySurface(front, dst);
            }

            // 頂点バッファ（座標）を設定
            var sw = (opt.start != null && opt.end != null) ? opt.end.Value.X - opt.start.Value.X : front.width;
            var sh = (opt.start != null && opt.end != null) ? opt.end.Value.Y - opt.start.Value.Y : front.height;
            this.setVertexPosition(ImageProcessing.createRectangle(0, 0, sw, sh));

            // 頂点バッファ（テクスチャ座標）を設定
            float left = (opt.start != null && opt.end != null) ? opt.start.Value.X : 0;
            float top = (opt.start != null && opt.end != null) ? opt.start.Value.Y : 0;
            float right = (opt.start != null && opt.end != null) ? opt.end.Value.X : front.width;
            float bottom = (opt.start != null && opt.end != null) ? opt.end.Value.Y : front.height;
            this.setTexturePosition(ImageProcessing.createRectangle(left / front.width, top / front.height, right / front.width, bottom / front.height));

            var projMat = (dst == null) ? Matrix3.projection(canvasWidth, canvasHeight) : Matrix3.projection(dst.width, dst.height);
            var projectionMatrix = (opt.matrix == null)
                ? Matrix3.multiply(Matrix3.scaling(1, (dst == null) ? -1 : 1), projMat)
                : Matrix3.multiply(Matrix3.multiply(Matrix3.scaling(1, (dst == null) ? -1 : 1), projMat), opt.matrix); ;
            OpenGL.glUniformMatrix3fv(matrixLocation, 1, OpenGL.GL_FALSE, projectionMatrix.ToArray());

            if (dst == null) {
                // テクスチャサイズ情報ををシェーダのUniform変数に設定
                OpenGL.glUniform2f(dstsizeLocation, canvasWidth, canvasHeight);

                // 合成モードを設定
                OpenGL.glUniform1i(blendModeLocation, (int)opt.blendMode);
                OpenGL.glUniform1i(stencilModeLocation, (int)opt.stencilMode);
                OpenGL.glUniform1i(compositeModeLocation, (int)opt.compositeMode);

                // 入力元とするレンダリングターゲットのテクスチャを選択
                OpenGL.glUniform1i(frontModeLocation, (int)InputType.Texture);
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, front.texture);
                OpenGL.glUniform1i(texture0Location, 0);

                // 入力元とするレンダリングターゲットのテクスチャを選択
                OpenGL.glUniform1i(backModeLocation, (int)InputType.Disable);
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE1);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, 0);
                OpenGL.glUniform1i(texture1Location, 1);

                // オンスクリーンフレームバッファを選択し、レンダリング解像度とビューポートを設定
                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
                OpenGL.glViewport(0, 0, canvasWidth, canvasHeight);

                // WebGL の描画結果を HTML に正しく合成する方法 より
                // http://webos-goodies.jp/archives/overlaying_webgl_on_html.html
                OpenGL.glClearColor(0, 0, 0, 0);
                OpenGL.glClear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                OpenGL.glEnable(OpenGL.GL_BLEND);
                OpenGL.glBlendEquation(OpenGL.GL_FUNC_ADD);
                OpenGL.glBlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
                OpenGL.glBlendFuncSeparate(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA, OpenGL.GL_ONE, OpenGL.GL_ZERO);

                // レンダリングを実行
                OpenGL.glDrawArrays(OpenGL.GL_TRIANGLES, 0, 6);

                OpenGL.glDisable(OpenGL.GL_BLEND);

            } else {
                // テクスチャサイズ情報ををシェーダのUniform変数に設定
                OpenGL.glUniform2f(dstsizeLocation, dst.width, dst.height);

                // 合成モードを設定
                OpenGL.glUniform1i(blendModeLocation, (int)opt.blendMode);
                OpenGL.glUniform1i(stencilModeLocation, (int)opt.stencilMode);
                OpenGL.glUniform1i(compositeModeLocation, (int)opt.compositeMode);

                // 入力元とするレンダリングターゲットのテクスチャを選択
                OpenGL.glUniform1i(frontModeLocation, (int)InputType.Texture);
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, front.texture);
                OpenGL.glUniform1i(texture0Location, 0);

                // 入力元とするレンダリングターゲットのテクスチャを選択
                OpenGL.glUniform1i(backModeLocation, (int)InputType.Texture);
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE1);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, dst.texture);
                OpenGL.glUniform1i(texture1Location, 1);

                // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, dst.framebuffer);
                OpenGL.glViewport(0, 0, dst.width, dst.height);

                // オフスクリーンレンダリングを実行
                OpenGL.glDrawArrays(OpenGL.GL_TRIANGLES, 0, 6);

                // フレームバッファの選択解除
                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
            }

            // テンポラリを破棄
            if (tmpSurface != null) {
                this.freeCachedTempOffScreen(tmpSurface);
            }
        }

        /// <summary>
        /// 合成
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="back"></param>
        /// <param name="front"></param>
        /// <param name="opt"></param>
        public void draw2(Surface dst, Surface back, Surface front, DrawOption opt) {

            // arrtibute変数の位置を取得
            //var positionLocation = OpenGL.glgetAttribLocation(this.program.programId, "a_position");
            //var texcoordLocation = OpenGL.glgetAttribLocation(this.program.programId, "a_texCoord");

            // uniform変数の位置を取得
            var matrixLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_matrix");

            var frontModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_front_mode");
            var texture0Location = OpenGL.glGetUniformLocation(this.program.programId, "u_front");
            var colorLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_color");

            var backModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_back_mode");
            var texture1Location = OpenGL.glGetUniformLocation(this.program.programId, "u_back");

            var dstsizeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_dstsize");

            var blendModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_blend_mode");
            var compositeModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_composit_mode");
            var stencilModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_stencil_mode");


            Surface tmpSurface = null;
            if (dst == back) {
                tmpSurface = back = this.allocCachedTempOffScreen(back.width, back.height);
                this.copySurface(back, dst);
            } else if (dst == front) {
                tmpSurface = front = this.allocCachedTempOffScreen(front.width, front.height);
                this.copySurface(front, dst);
                this.copySurface(dst, back);
            } else {
                this.copySurface(dst, back);
            }

            // 頂点バッファ（座標）を設定
            var sw = (opt.start != null && opt.end != null) ? opt.end.Value.X - opt.start.Value.X : front.width;
            var sh = (opt.start != null && opt.end != null) ? opt.end.Value.Y - opt.start.Value.Y : front.height;
            this.setVertexPosition(ImageProcessing.createRectangle(0, 0, sw, sh));

            // 頂点バッファ（テクスチャ座標）を設定
            float left = (opt.start != null && opt.end != null) ? opt.start.Value.X : 0;
            float top = (opt.start != null && opt.end != null) ? opt.start.Value.Y : 0;
            float right = (opt.start != null && opt.end != null) ? opt.end.Value.X : front.width;
            float bottom = (opt.start != null && opt.end != null) ? opt.end.Value.Y : front.height;
            this.setTexturePosition(ImageProcessing.createRectangle(left / front.width, top / front.height, right / front.width, bottom / front.height));

            var projMat = Matrix3.projection(dst.width, dst.height);
            var projectionMatrix = (opt.matrix == null) ? projMat : Matrix3.multiply(projMat, opt.matrix);
            OpenGL.glUniformMatrix3fv(matrixLocation, 1, OpenGL.GL_FALSE, projectionMatrix.ToArray());

            // テクスチャサイズ情報ををシェーダのUniform変数に設定
            OpenGL.glUniform2f(dstsizeLocation, dst.width, dst.height);

            // 合成モードを設定
            OpenGL.glUniform1i(blendModeLocation, (int)opt.blendMode);
            OpenGL.glUniform1i(stencilModeLocation, (int)opt.stencilMode);
            OpenGL.glUniform1i(compositeModeLocation, (int)opt.compositeMode);

            // 入力元とするレンダリングターゲットのテクスチャを選択
            OpenGL.glUniform1i(frontModeLocation, (int)InputType.Texture);
            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, front.texture);
            OpenGL.glUniform1i(texture0Location, 0);

            // 入力元とするレンダリングターゲットのテクスチャを選択
            OpenGL.glUniform1i(backModeLocation, (int)InputType.Texture);
            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE1);
            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, back.texture);
            OpenGL.glUniform1i(texture1Location, 1);

            // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
            OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, dst.framebuffer);
            OpenGL.glViewport(0, 0, dst.width, dst.height);

            // オフスクリーンレンダリングを実行
            OpenGL.glDrawArrays(OpenGL.GL_TRIANGLES, 0, 6);

            // フレームバッファの選択解除
            OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);

            // テンポラリを破棄
            if (tmpSurface != null) {
                this.freeCachedTempOffScreen(tmpSurface);
            }
        }
        public class Surface : IDisposable
        {
            public uint framebuffer { get; internal set; }
            public int height { get; internal set; }
            public uint texture { get; internal set; }
            public int width { get; internal set; }

            public Surface(Image image) {
                // テクスチャを生成。ピクセルフォーマットはRGBA(8bit)。画像サイズは指定されたものを使用。
                uint[] ret = new uint[1];
                OpenGL.glCreateTextures(OpenGL.GL_TEXTURE_2D, 1, ret);
                this.texture = ret[0];
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, this.texture);
                OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP_TO_EDGE);
                OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP_TO_EDGE);
                OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_NEAREST);
                OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_NEAREST);

                using (Bitmap bitmap = new Bitmap(image)) {
                    BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    var ptr = bmpData.Scan0;
                    var pixels = new byte[bmpData.Width * bmpData.Height * 4];
                    for (var y = 0; y < bmpData.Height; y++) {
                        Marshal.Copy(ptr + y * bmpData.Stride, pixels, y * bmpData.Width * 4, bmpData.Width * 4);
                        var x = y * bmpData.Width * 4;
                        for (var i = 0; i < bmpData.Width; i++) {
                            var a = pixels[x + 3];
                            var r = pixels[x + 2];
                            var g = pixels[x + 1];
                            var b = pixels[x + 0];
                            pixels[x + 0] = r;
                            pixels[x + 1] = g;
                            pixels[x + 2] = b;
                            pixels[x + 3] = a;
                            x += 4;
                        }
                    }
                    OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA, bmpData.Width, bmpData.Height, 0, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, pixels);
                    this.width = bmpData.Width;
                    this.height = bmpData.Height;
                    bitmap.UnlockBits(bmpData);
                }

                // フレームバッファを生成し、テクスチャと関連づけ
                OpenGL.glCreateFramebuffers(1, ret);
                this.framebuffer = ret[0];
                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, this.framebuffer);
                OpenGL.glViewport(0, 0, this.width, this.height);
                OpenGL.glFramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0, OpenGL.GL_TEXTURE_2D, this.texture, 0);
                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
            }

            public Surface(int width, int height) {
                // テクスチャを生成。ピクセルフォーマットはRGBA(8bit)。画像サイズは指定されたものを使用。
                uint[] ret = new uint[1];
                OpenGL.glCreateTextures(OpenGL.GL_TEXTURE_2D, 1, ret);
                this.texture = ret[0];
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, this.texture);
                OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP_TO_EDGE);
                OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP_TO_EDGE);
                OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_NEAREST);
                OpenGL.glTexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_NEAREST);

                {
                    OpenGL.glTexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA, width, height, 0, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, null);
                    this.width = width;
                    this.height = height;
                }

                // フレームバッファを生成し、テクスチャと関連づけ
                OpenGL.glCreateFramebuffers(1, ret);
                this.framebuffer = ret[0];
                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, this.framebuffer);
                OpenGL.glViewport(0, 0, this.width, this.height);
                OpenGL.glFramebufferTexture2D(OpenGL.GL_FRAMEBUFFER, OpenGL.GL_COLOR_ATTACHMENT0, OpenGL.GL_TEXTURE_2D, this.texture, 0);
                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
            }



            public void clear() {
                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, this.framebuffer);
                OpenGL.glViewport(0, 0, this.width, this.height);
                OpenGL.glClear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT | OpenGL.GL_STENCIL_BUFFER_BIT);
                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, 0);
            }

            public void Dispose() {
                OpenGL.glDeleteFramebuffers(1, new[] { this.framebuffer });
                OpenGL.glDeleteTextures(1, new[] { this.texture });
                this.texture = 0;
                this.framebuffer = 0;
                this.width = 0;
                this.height = 0;
            }
        }

    }

}

