using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;

namespace GLPainter {
    using GLfloat = Single;
    using GLbitfield = UInt32;
    using GLFWwindow = IntPtr;
    using GLenum = UInt32;
    using GLboolean = Byte;
    using GLint = Int32;
    using GLuint = UInt32;
    using GLsizei = Int32;
    using GLsizeiptr = Int32;   // 32bit

    class Program {
        static void Main(string[] args) {
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
            GLFWwindow window = GLFW.glfwCreateWindow(640, 480, "こんにちわ!", IntPtr.Zero, IntPtr.Zero);
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

            {
                ImageProcessing app = new ImageProcessing();
                GLFW.glfwSetWindowSizeCallback(window, (w, width, height) => {
                    int renderBufferWidth, renderBufferHeight;
                    GLFW.glfwGetFramebufferSize(window, out renderBufferWidth, out renderBufferHeight);

                    app.Resize(renderBufferWidth, renderBufferHeight);

                    // ビューポート変換の更新
                    OpenGL.glViewport(0, 0, renderBufferWidth, renderBufferHeight);
                
                });
            }

            // ウィンドウが開いている間繰り返す
            while (GLFW.glfwWindowShouldClose(window) == OpenGL.GL_FALSE) {
                // バックバッファ消去
                OpenGL.glClear(OpenGL.GL_COLOR_BUFFER_BIT);

                // 描画処理

                // カラーバッファ入れ替え <= ダブルバッファリング (GLFW)
                GLFW.glfwSwapBuffers(window);

                // イベント待ち (GLFW)
                GLFW.glfwWaitEvents();
            }

            return;
        }
    }
    public class ImageProcessing {


        public class Program : IDisposable {
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
        public class Shader : IDisposable {
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

        GLuint positionBuffer;
        GLuint texcoordBuffer;
        ImageProcessing.Program program;

        List<Surface> cachedSurface = new List<Surface>();

        private int canvasWidth, canvasHeight;

        public void Resize(int width, int height) {
            canvasWidth = width;
            canvasHeight = height;
        }

        private const string vertexShaderSrc = @"
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
        private const string fragmentShaderSrc = @"
#version 330 

precision mediump float;

uniform sampler2D u_front;
uniform sampler2D u_back;

uniform vec2 u_dstsize;
uniform int u_composit_mode;

uniform vec4  u_color;

uniform vec2  u_start;
uniform vec2  u_end;
uniform float u_size;
uniform float u_aliasSize;

in vec2 v_texCoord;
out vec4 fragColor;

vec4 composit_normal(vec4 front, vec4 back, vec3 blended) {
    float alpha_f = front.a;
    float alpha_b = back.a;
    vec3 C_f = front.rgb;
    vec3 C_b = back.rgb;
    vec3 C_r = blended;

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

vec4 composit_erase(vec4 front, vec4 back, vec3 blended) {
    float alpha_f = front.a;
    float alpha_b = back.a;
    vec3 C_b = back.rgb;

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

vec4 composit_copyColor(vec4 front, vec4 back, vec4 color) {
    return color;
}

vec4 composit_maxAlpha(vec4 back, vec4 color) {
    return vec4(color.rgb, max(back.a, color.a));
}

vec4 stencil_checkerBoard() {
    vec2 rads = step(0.0, cos(radians(gl_FragCoord.xy * 360.0/16.0)));
    float c = (255.0 - abs(rads.X - rads.Y) * 32.0) / 255.0;
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
    float aaRate = u_aliasSize * 0.01;

    float aa = 1.0 - clamp((dist - (u_size * aaRate)) / (u_size * (1.0-aaRate)),0.0,1.0);

    return vec4(color.rgb, aa* color.a);
}

vec3 blend_normal(vec4 front, vec4 back) {
    return front.rgb;
}

void main() {
    vec4 f = texture(u_front, v_texCoord);
    vec4 b = texture(u_back, gl_FragCoord.xy / u_dstsize  );

    vec3 blended = blend_normal(f, b);

    vec4 color;
    if (u_composit_mode == 0) {
        color = composit_normal(f, b, blended);
        fragColor = color;
    } else if (u_composit_mode == 1) {
        color = composit_erase(f, b, blended);
        fragColor = color;
    } else if (u_composit_mode == 2) {
        color = composit_copyFront(f, b, blended);
        fragColor = color;
    } else if (u_composit_mode == 3) {
        color = composit_maxAlpha(b, u_color);
        fragColor = color;
    } else if (u_composit_mode == 4) {
        color = stencil_checkerBoard();
        color = composit_copyColor(f, b, color);
        fragColor = color;
    } else if (u_composit_mode == 5) {
        color = stencil_Line(u_color);
        color = composit_maxAlpha(b, color);
        fragColor = color;
    } else {
        color = composit_copyFront(f, b, blended);
        fragColor = color;
    }


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
            OpenGL.glUniformMatrix3fv(matrixLocation, 9, OpenGL.GL_FALSE, projectionMatrix.ToArray());

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

        public enum CompositeMode {
            Normal = 0,
            Erase = 1,
            Copy = 2,
            MaxAlpha = 3,
            CheckerBoard = 4,
            DrawLine = 5,
        }


        public class DrawLinesOption {
            public float[] vertexes = null;
            public float size = 0.0f;
            public byte[] color = null;
            public float antialiasSize = 0.0f;
        }
        public void drawLines(Surface dst, DrawLinesOption opt = null) {
            if (opt == null) {
                opt = new DrawLinesOption();
            }
            var tmp = this.allocCachedTempOffScreen(dst.width, dst.height);

            // arrtibute変数の位置を取得
            //var positionLocation = OpenGL.glGetAttribLocation(this.program.programId, "a_position");
            //var texcoordLocation = OpenGL.glGetAttribLocation(this.program.programId, "a_texCoord");

            // uniform変数の位置を取得
            var matrixLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_matrix");
            var startLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_start");
            var endLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_end");
            var sizeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_size");
            var aliasRateLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_aliasSize");
            var colorLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_color");
            var texture0Location = OpenGL.glGetUniformLocation(this.program.programId, "u_front");
            var texture1Location = OpenGL.glGetUniformLocation(this.program.programId, "u_back");
            var dstsizeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_dstsize");
            var compositeModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_composit_mode");

            // 色を設定
            OpenGL.glUniform4fv(colorLocation, 4, opt.color.Select(x => x / 255.0f).ToArray());

            // 投影行列を設定
            var projectionMatrix = Matrix3.projection(tmp.width, tmp.height);
            OpenGL.glUniformMatrix3fv(matrixLocation, 9,OpenGL.GL_FALSE, projectionMatrix.ToArray());

            // 入力元とするレンダリングターゲット=dst
            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, 0);
            OpenGL.glUniform1i(texture0Location, 0);

            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE1);
            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, dst.texture);
            OpenGL.glUniform1i(texture1Location, 1);

            // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
            OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, tmp.framebuffer);
            OpenGL.glViewport(0, 0, tmp.width, tmp.height);

            // テクスチャサイズ情報ををシェーダのUniform変数に設定
            OpenGL.glUniform2f(dstsizeLocation, tmp.width, tmp.height);

            // 合成モードを設定
            OpenGL.glUniform1i(compositeModeLocation, (int)CompositeMode.DrawLine);

            var vertexes = opt.vertexes;
            var len = ~~(vertexes.Length / 2) - 1;

            // サイズを設定
            OpenGL.glUniform1f(sizeLocation, opt.size / 2);

            // アンチエイリアスサイズを指定
            OpenGL.glUniform1f(aliasRateLocation, opt.antialiasSize);

            // 矩形設定
            OpenGL.glClear(OpenGL.GL_COLOR_BUFFER_BIT);
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
                var clippedLeft = (left >= tmp.width) ? tmp.width - 1 : (left < 0) ? 0 : (int)left;
                var clippedTop = (top >= tmp.height) ? tmp.height - 1 : (top < 0) ? 0 : (int)top;
                var clippedRight = (right >= tmp.width) ? tmp.width - 1 : (right < 0) ? 0 : (int)right;
                var clippedBottom = (bottom >= tmp.height) ? tmp.height - 1 : (bottom < 0) ? 0 : (int)bottom;
                OpenGL.glCopyTexSubImage2D(OpenGL.GL_TEXTURE_2D, 0, clippedLeft, clippedTop, clippedLeft, clippedTop, clippedRight - clippedLeft, clippedBottom - clippedTop);
            }

            this.freeCachedTempOffScreen(tmp);

        }


        public class FillRectOption {
            public CompositeMode compositeMode = CompositeMode.MaxAlpha;
            public PointF start;
            public PointF end;
            public byte[] color;
        }

        public void fillRect(Surface dst, FillRectOption opt) {

            var tmp = this.allocCachedTempOffScreen(dst.width, dst.height);

            // arrtibute変数の位置を取得
            //var positionLocation = OpenGL.glgetAttribLocation(this.program.programId, "a_position");
            //var texcoordLocation = OpenGL.glgetAttribLocation(this.program.programId, "a_texCoord");

            // uniform変数の位置を取得
            var matrixLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_matrix");
            var aliasRateLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_aliasSize");
            var colorLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_color");
            var texture0Location = OpenGL.glGetUniformLocation(this.program.programId, "u_front");
            var texture1Location = OpenGL.glGetUniformLocation(this.program.programId, "u_back");
            var dstsizeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_dstsize");
            var compositeModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_composit_mode");

            // 色を設定
            OpenGL.glUniform4fv(colorLocation, 4, opt.color.Select(x => x / 255.0f).ToArray());

            // 投影行列を設定
            var projectionMatrix = Matrix3.projection(tmp.width, tmp.height);
            OpenGL.glUniformMatrix3fv(matrixLocation, 9, OpenGL.GL_FALSE, projectionMatrix.ToArray());

            // 入力元とするレンダリングターゲット
            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, 0);
            OpenGL.glUniform1i(texture0Location, 0);

            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE1);
            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, dst.texture);
            OpenGL.glUniform1i(texture1Location, 1);

            // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
            OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, tmp.framebuffer);
            OpenGL.glViewport(0, 0, tmp.width, tmp.height);

            // テクスチャサイズ情報ををシェーダのUniform変数に設定
            OpenGL.glUniform2f(dstsizeLocation, tmp.width, tmp.height);

            // 合成モードを設定
            OpenGL.glUniform1i(compositeModeLocation, (int)opt.compositeMode);

            // アンチエイリアスサイズを指定
            OpenGL.glUniform1f(aliasRateLocation, 0);

            // 矩形設定
            OpenGL.glClear(OpenGL.GL_COLOR_BUFFER_BIT);

            var x1 = opt.start.X + 0.5f;
            var y1 = opt.start.Y + 0.5f;
            var x2 = opt.end.X + 0.5f;
            var y2 = opt.end.Y + 0.5f;

            var left = Math.Min(x1, x2);
            var top = Math.Min(y1, y2);
            var right = Math.Max(x1, x2);
            var bottom = Math.Max(y1, y2);


            // 頂点バッファ（テクスチャ座標）を設定
            this.setVertexPosition(ImageProcessing.createRectangle(left, top, right, bottom));

            // 頂点バッファ（テクスチャ座標）を設定
            this.setTexturePosition(ImageProcessing.createRectangle(left / dst.width, top / dst.height, right / dst.width, bottom / dst.height));

            // シェーダを適用したオフスクリーンレンダリングを実行
            OpenGL.glDrawArrays(OpenGL.GL_TRIANGLES, 0, 6);

            var clippedLeft = (left < 0) ? 0 : (left > dst.width - 1) ? dst.width - 1 : left;
            var clippedTop = (top < 0) ? 0 : (top > dst.height - 1) ? dst.height - 1 : top;
            var clippedRight = (right < 0) ? 0 : (right > dst.width - 1) ? dst.width - 1 : right;
            var clippedBottom = (bottom < 0) ? 0 : (bottom > dst.height - 1) ? dst.height - 1 : bottom;
            OpenGL.glCopyTexSubImage2D(OpenGL.GL_TEXTURE_2D, 0, (int)clippedLeft, (int)clippedTop, (int)clippedLeft, (int)clippedTop, (int)(clippedRight - clippedLeft), (int)(clippedBottom - clippedTop));
            this.freeCachedTempOffScreen(tmp);

        }

        public class DrawOption {
            public CompositeMode? compositeMode = null;
            public PointF? start = null;
            public PointF? end = null;
            public Matrix3 matrix = null;

        }



        // copy
        public void draw(Surface dst, Surface front, DrawOption opt) {

            // arrtibute変数の位置を取得
            //var positionLocation = OpenGL.glgetAttribLocation(this.program.programId, "a_position");
            //var texcoordLocation = OpenGL.glgetAttribLocation(this.program.programId, "a_texCoord");

            // uniform変数の位置を取得
            var matrixLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_matrix");
            var texture0Location = OpenGL.glGetUniformLocation(this.program.programId, "u_front");
            var texture1Location = OpenGL.glGetUniformLocation(this.program.programId, "u_back");
            var dstsizeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_dstsize");
            var compositeModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_composit_mode");


            Surface tmpSurface = null;
            if (dst == front) {
                tmpSurface = front = this.allocCachedTempOffScreen(front.width, front.height);
                this.copySurface(front, dst);
            } else {
                if (dst != null) {
                    this.copySurface(dst, front);
                }
            }

            // 頂点バッファ（座標）を設定
            var sw = (opt.start != null && opt.end != null) ? opt.end.Value.X - opt.start.Value.X : front.width;
            var sh = (opt.start != null && opt.end != null) ? opt.end.Value.Y - opt.start.Value.Y : front.height;
            this.setVertexPosition(ImageProcessing.createRectangle(0, 0, sw, sh));

            // 頂点バッファ（テクスチャ座標）を設定
            var left = (opt.start != null && opt.end != null) ? opt.start.Value.X : 0;
            var top = (opt.start != null && opt.end != null) ? opt.start.Value.Y : 0;
            var right = (opt.start != null && opt.end != null) ? opt.end.Value.X : front.width;
            var bottom = (opt.start != null && opt.end != null) ? opt.end.Value.Y : front.height;
            this.setTexturePosition(ImageProcessing.createRectangle(left / front.width, top / front.height, right / front.width, bottom / front.height));

            var projMat = (dst == null) ? Matrix3.projection(canvasWidth, canvasHeight) : Matrix3.projection(dst.width, dst.height);
            var projectionMatrix = (opt.matrix == null)
                ? Matrix3.multiply(Matrix3.scaling(1, (dst == null) ? -1 : 1), projMat)
                : Matrix3.multiply(Matrix3.multiply(Matrix3.scaling(1, (dst == null) ? -1 : 1), projMat), opt.matrix); ;
            OpenGL.glUniformMatrix3fv(matrixLocation, 9,OpenGL.GL_FALSE, projectionMatrix.ToArray());

            if (dst == null) {
                // テクスチャサイズ情報ををシェーダのUniform変数に設定
                OpenGL.glUniform2f(dstsizeLocation, canvasWidth, canvasHeight);

                // 合成モードを設定
                OpenGL.glUniform1i(compositeModeLocation, (int)opt.compositeMode);

                // 入力元とするレンダリングターゲットのテクスチャを選択
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, front.texture);
                OpenGL.glUniform1i(texture0Location, 0);

                // 入力元とするレンダリングターゲットのテクスチャを選択
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
                OpenGL.glUniform1i(compositeModeLocation, (int)opt.compositeMode);

                // 入力元とするレンダリングターゲットのテクスチャを選択
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, front.texture);
                OpenGL.glUniform1i(texture0Location, 0);

                // 入力元とするレンダリングターゲットのテクスチャを選択
                OpenGL.glActiveTexture(OpenGL.GL_TEXTURE1);
                OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, 0);
                OpenGL.glUniform1i(texture1Location, 1);

                // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
                OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, dst.framebuffer);
                OpenGL.glViewport(0, 0, dst.width, dst.height);

                // オフスクリーンレンダリングを実行
                OpenGL.glDrawArrays(OpenGL.GL_TRIANGLES, 0, 6);
            }

            // テンポラリを破棄
            if (tmpSurface != null) {
                this.freeCachedTempOffScreen(tmpSurface);
            }
        }

        public void draw2(Surface dst, Surface back, Surface front, DrawOption opt) {

            // arrtibute変数の位置を取得
            //var positionLocation = OpenGL.glgetAttribLocation(this.program.programId, "a_position");
            //var texcoordLocation = OpenGL.glgetAttribLocation(this.program.programId, "a_texCoord");

            // uniform変数の位置を取得
            var matrixLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_matrix");
            var texture0Location = OpenGL.glGetUniformLocation(this.program.programId, "u_front");
            var texture1Location = OpenGL.glGetUniformLocation(this.program.programId, "u_back");
            var dstsizeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_dstsize");
            var compositeModeLocation = OpenGL.glGetUniformLocation(this.program.programId, "u_composit_mode");


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
            var left = (opt.start != null && opt.end != null) ? opt.start.Value.X : 0;
            var top = (opt.start != null && opt.end != null) ? opt.start.Value.Y : 0;
            var right = (opt.start != null && opt.end != null) ? opt.end.Value.X : front.width;
            var bottom = (opt.start != null && opt.end != null) ? opt.end.Value.Y : front.height;
            this.setTexturePosition(ImageProcessing.createRectangle(left / front.width, top / front.height, right / front.width, bottom / front.height));

            var projMat = Matrix3.projection(dst.width, dst.height);
            var projectionMatrix = (opt.matrix == null) ? projMat : Matrix3.multiply(projMat, opt.matrix);
            OpenGL.glUniformMatrix3fv(matrixLocation, 9, OpenGL.GL_FALSE, projectionMatrix.ToArray());

            // テクスチャサイズ情報ををシェーダのUniform変数に設定
            OpenGL.glUniform2f(dstsizeLocation, dst.width, dst.height);

            // 合成モードを設定
            OpenGL.glUniform1i(compositeModeLocation, (int)opt.compositeMode);

            // 入力元とするレンダリングターゲットのテクスチャを選択
            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE0);
            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, front.texture);
            OpenGL.glUniform1i(texture0Location, 0);

            // 入力元とするレンダリングターゲットのテクスチャを選択
            OpenGL.glActiveTexture(OpenGL.GL_TEXTURE1);
            OpenGL.glBindTexture(OpenGL.GL_TEXTURE_2D, back.texture);
            OpenGL.glUniform1i(texture1Location, 1);

            // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
            OpenGL.glBindFramebuffer(OpenGL.GL_FRAMEBUFFER, dst.framebuffer);
            OpenGL.glViewport(0, 0, dst.width, dst.height);

            // オフスクリーンレンダリングを実行
            OpenGL.glDrawArrays(OpenGL.GL_TRIANGLES, 0, 6);


            // テンポラリを破棄
            if (tmpSurface != null) {
                this.freeCachedTempOffScreen(tmpSurface);
            }
        }
        public class Surface : IDisposable {
            private Image image;

            public Surface(Image image) {
                this.image = image;
            }

            public Surface(int width, int height) {
                this.width = width;
                this.height = height;
            }

            public uint framebuffer { get; internal set; }
            public int height { get; internal set; }
            public uint texture { get; internal set; }
            public int width { get; internal set; }

            public void clear() {
                throw new NotImplementedException();
            }

            public void Dispose() {
                throw new NotImplementedException();
            }
        }

    }

}
