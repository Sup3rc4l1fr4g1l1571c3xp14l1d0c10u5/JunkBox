"use strict";

// ReSharper disable InconsistentNaming
interface WebGL2RenderingContext extends WebGLRenderingContext {
    RGBA8: number;
    PIXEL_PACK_BUFFER: number;
    STREAM_READ: number;
    SYNC_GPU_COMMANDS_COMPLETE: number;
    fenceSync: (sync: number, flag: number) => void;
    getBufferSubData: (target: number, offset: number, buffer: any) => void;
}
// ReSharper restore InconsistentNaming

class Shader {
    protected constructor(public gl: WebGL2RenderingContext, public shader: WebGLShader, public shaderType: number) { }

    public static create(gl: WebGL2RenderingContext, shaderSource: string, shaderType: number): Shader {
        // Create the shader object
        const shader = gl.createShader(shaderType);

        // Load the shader source
        gl.shaderSource(shader, shaderSource);

        // Compile the shader
        gl.compileShader(shader);

        // Check the compile status
        const compiled = gl.getShaderParameter(shader, gl.COMPILE_STATUS);
        if (!compiled) {
            // Something went wrong during compilation; get the error
            const lastError = gl.getShaderInfoLog(shader);
            console.error("*** Error compiling shader '" + shader + "':" + lastError);
            gl.deleteShader(shader);
            return null;
        }

        return new Shader(gl, shader, shaderType);
    }

    public static loadShaderById(gl: WebGL2RenderingContext, id: string, optShaderType?: number) {
        const shaderScript: HTMLScriptElement = document.getElementById(id) as HTMLScriptElement;
        if (!shaderScript) {
            throw ("*** Error: unknown element `" + id + "`");
        }
        const shaderSource = shaderScript.text;

        let shaderType = optShaderType;
        if (!optShaderType) {
            if (shaderScript.type === "x-shader/x-vertex") {
                shaderType = gl.VERTEX_SHADER;
            } else if (shaderScript.type === "x-shader/x-fragment") {
                shaderType = gl.FRAGMENT_SHADER;
            } else if (shaderType !== gl.VERTEX_SHADER && shaderType !== gl.FRAGMENT_SHADER) {
                throw ("*** Error: unknown shader type");
            }
        }

        return Shader.create(gl, shaderSource, shaderType);

    }
}

class Program {
    protected constructor(public gl: WebGL2RenderingContext, public program: WebGLProgram) { }
    public static create(gl: WebGL2RenderingContext, shaders: WebGLShader[], optAttribs?: string[], optLocations?: number[]) {
        const program = gl.createProgram();
        shaders.forEach(shader => {
            gl.attachShader(program, shader);
        });
        if (optAttribs) {
            optAttribs.forEach((attrib, ndx) => {
                gl.bindAttribLocation(
                    program,
                    optLocations ? optLocations[ndx] : ndx,
                    attrib);
            });
        }
        gl.linkProgram(program);

        // Check the link status
        const linked = gl.getProgramParameter(program, gl.LINK_STATUS);
        if (!linked) {
            // something went wrong with the link
            const lastError = gl.getProgramInfoLog(program);
            console.error("Error in program linking:" + lastError);

            gl.deleteProgram(program);
            return null;
        }
        return new Program(gl, program);
    }
    public static loadShaderById(gl: WebGL2RenderingContext, ids: string[], shaderTypes: number[], optAttribs?: string[], optLocations?: number[]) {
        const shaders: Shader[] = [];
        for (let ii = 0; ii < ids.length; ++ii) {
            shaders.push(Shader.loadShaderById(gl, ids[ii], shaderTypes[ii]));
        }
        return Program.create(gl, shaders.map((x) => x.shader), optAttribs, optLocations);
    }
}
class OffscreenTarget {
    gl: WebGL2RenderingContext;
    texture: WebGLTexture;
    framebuffer: WebGLFramebuffer;

    private static createAndBindTexture(gl: WebGL2RenderingContext): WebGLTexture {
        const texture = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, texture);

        // Set up texture so we can render any size image and so we are
        // working with pixels.
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
        return texture;
    }


    constructor(gl: WebGL2RenderingContext, srcImage: HTMLImageElement);
    constructor(gl: WebGL2RenderingContext, width: number, height: number);
    constructor(gl: WebGL2RenderingContext, srcImageOrWidth: HTMLImageElement | number, height?: number) {
        this.gl = gl;

        // フレームバッファテクスチャを生成。ピクセルフォーマットはRGBA(8bit)。画像サイズは指定されたものを使用。
        this.texture = OffscreenTarget.createAndBindTexture(gl);
        if (srcImageOrWidth instanceof HTMLImageElement) {
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, srcImageOrWidth as HTMLImageElement);
        } else {
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, srcImageOrWidth, height, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
        }

        // フレームバッファを生成し、テクスチャと関連づけ
        this.framebuffer = gl.createFramebuffer();
        gl.bindFramebuffer(gl.FRAMEBUFFER, this.framebuffer);
        gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, this.texture, 0);

    }

}
class PixelBuffer {
    pbo: WebGLBuffer;

    constructor(public gl: WebGL2RenderingContext, public width: number, public height: number) {
        this.pbo = gl.createBuffer();
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, this.pbo);
        gl.bufferData(gl.PIXEL_PACK_BUFFER, this.width * this.height * 4, gl.STREAM_READ);
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, null);

    }
}
class ImageProcessing {
    gl: WebGL2RenderingContext;

    positionBuffer: WebGLBuffer;
    texcoordBuffer: WebGLBuffer;
    offscreenTargets: OffscreenTarget[];

    pixelBuffer: PixelBuffer;

    width: number;
    height: number;
    flipCnt: number;

    // フィルタカーネルの重み合計（重み合計が0以下の場合は1とする）
    private static  computeKernelWeight(kernel: number[]): number {
        const weight = kernel.reduce((prev, curr) => prev + curr);
        return weight <= 0 ? 1 : weight;
    }

    constructor(gl: WebGL2RenderingContext, image: HTMLImageElement) {
        this.gl = gl;
        this.width = image.width;
        this.height = image.height;

        // 頂点バッファ（座標）を作成
        this.positionBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
        {
            const x1 = 0;
            const x2 = image.width;
            const y1 = 0;
            const y2 = image.height;
            gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([
                x1, y1,
                x2, y1,
                x1, y2,
                x1, y2,
                x2, y1,
                x2, y2
            ]), gl.STATIC_DRAW);
        }

        // 頂点バッファ（テクスチャ座標）を作成
        this.texcoordBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([
            0.0, 0.0,
            1.0, 0.0,
            0.0, 1.0,
            0.0, 1.0,
            1.0, 0.0,
            1.0, 1.0
        ]), gl.STATIC_DRAW);

        // フィルタの入力用と出力用となるフレームバッファを生成
        this.offscreenTargets = [
            new OffscreenTarget(gl, image),                     // 0番は初回時に入力側になるので指定された画像をロードしておく
            new OffscreenTarget(gl, image.width, image.height)  // 1番は初回時に出力側になるので空のテクスチャを生成
        ];

        // フリップ状態変数を初期化
        this.flipCnt = 0;

        //// 演算結果取得用のフレームバッファとレンダーバッファ、ピクセルバッファを生成

        //// 空のレンダーバッファを生成し、フォーマットを設定
        //this.renderbuffer = gl.createRenderbuffer();
        //gl.bindRenderbuffer(gl.RENDERBUFFER, this.renderbuffer);
        //gl.renderbufferStorage(gl.RENDERBUFFER, gl.RGBA8, this.width, this.height);

        //// フレームバッファを生成し、レンダーバッファをカラーバッファとしてフレームバッファにアタッチ
        //this.fbo = gl.createFramebuffer();
        //gl.bindFramebuffer(gl.FRAMEBUFFER, this.fbo);
        //gl.framebufferRenderbuffer(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.RENDERBUFFER, this.renderbuffer);
        //gl.bindFramebuffer(gl.FRAMEBUFFER, null);

        // ピクセルバッファを生成

        this.pixelBuffer = new PixelBuffer(gl, this.width, this.height);

    }
    applyKernel(kernel: number[], program: Program) {
        const gl = this.gl;

        // arrtibute変数の位置を取得
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");

        // uniform変数の位置を取得
        const resolutionLocation = gl.getUniformLocation(program.program, "u_resolution");
        const textureSizeLocation = gl.getUniformLocation(program.program, "u_textureSize");
        const kernelLocation = gl.getUniformLocation(program.program, "u_kernel[0]");
        const kernelWeightLocation = gl.getUniformLocation(program.program, "u_kernelWeight");

        // シェーダを設定
        gl.useProgram(program.program);

        // シェーダの頂点座標Attributeを有効化
        gl.enableVertexAttribArray(positionLocation);

        // 頂点バッファ（座標）を設定
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);

        // 頂点座標Attributeの位置情報を設定
        gl.vertexAttribPointer(
            positionLocation,
            2,          // 2 components per iteration
            gl.FLOAT,   // the data is 32bit floats
            false,      // don't normalize the data
            0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
            0,          // start at the beginning of the buffer
        );

        // シェーダのテクスチャ座標Attributeを有効化
        gl.enableVertexAttribArray(texcoordLocation);

        // 頂点バッファ（テクスチャ座標）を設定
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);

        // テクスチャ座標Attributeの位置情報を設定
        gl.vertexAttribPointer(
            texcoordLocation,
            2,          // 2 components per iteration
            gl.FLOAT,   // the data is 32bit floats
            false,      // don't normalize the data
            0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
            0           // start at the beginning of the buffer
        );
        // テクスチャサイズ情報ををシェーダのUniform変数に設定
        gl.uniform2f(textureSizeLocation, this.width, this.height);

        // 入力元とするレンダリングターゲットのテクスチャを選択
        gl.bindTexture(gl.TEXTURE_2D, this.offscreenTargets[(this.flipCnt) % 2].texture);

        // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
        gl.bindFramebuffer(gl.FRAMEBUFFER, this.offscreenTargets[(this.flipCnt + 1) % 2].framebuffer);
        gl.uniform2f(resolutionLocation, this.width, this.height);
        gl.viewport(0, 0, this.width, this.height);

        // カーネルシェーダを適用したオフスクリーンレンダリングを実行
        gl.uniform1fv(kernelLocation, kernel);
        gl.uniform1f(kernelWeightLocation, ImageProcessing.computeKernelWeight(kernel));
        gl.drawArrays(gl.TRIANGLES, 0, 6);

        // ステップカウントを更新
        this.flipCnt++;

        // フィルタ適用完了

    }
    applyShader(program: Program) : void {
        const gl = this.gl;
            // arrtibute変数の位置を取得
            const positionLocation = gl.getAttribLocation(program.program, "a_position");
            const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");

            // uniform変数の位置を取得
            const resolutionLocation = gl.getUniformLocation(program.program, "u_resolution");
            const textureSizeLocation = gl.getUniformLocation(program.program, "u_textureSize");

            // レンダリング実行

            gl.useProgram(program.program);
            // シェーダの頂点座標Attributeを有効化
            gl.enableVertexAttribArray(positionLocation);

            // 頂点バッファ（座標）を設定
            gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);

            // 頂点座標Attributeの位置情報を設定
            gl.vertexAttribPointer(
                positionLocation,
                2,          // 2 components per iteration
                gl.FLOAT,   // the data is 32bit floats
                false,      // don't normalize the data
                0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
                0,          // start at the beginning of the buffer
            );

            // シェーダのテクスチャ座標Attributeを有効化
            gl.enableVertexAttribArray(texcoordLocation);

            // 頂点バッファ（テクスチャ座標）を設定
            gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);

            // テクスチャ座標Attributeの位置情報を設定
            gl.vertexAttribPointer(
                texcoordLocation,
                2,          // 2 components per iteration
                gl.FLOAT,   // the data is 32bit floats
                false,      // don't normalize the data
                0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
                0           // start at the beginning of the buffer
            );
            // テクスチャサイズ情報ををシェーダのUniform変数に設定
            gl.uniform2f(textureSizeLocation, this.width, this.height);

        // 入力元とするレンダリングターゲットのテクスチャを選択
        gl.bindTexture(gl.TEXTURE_2D, this.offscreenTargets[(this.flipCnt) % 2].texture);

        // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
        gl.bindFramebuffer(gl.FRAMEBUFFER, this.offscreenTargets[(this.flipCnt + 1) % 2].framebuffer);
        gl.uniform2f(resolutionLocation, this.width, this.height);
        gl.viewport(0, 0, this.width, this.height);

        // オフスクリーンレンダリングを実行
        gl.drawArrays(gl.TRIANGLES, 0, 6);

        // ステップカウントを更新
        this.flipCnt++;

        // 適用完了

    }
    download(): Uint8Array {
        const gl = this.gl;

        if (gl.checkFramebufferStatus(gl.FRAMEBUFFER) !== gl.FRAMEBUFFER_COMPLETE) {
            console.log("framebuffer with RGB8 color buffer is incomplete");
            return null;
        } else {
            const data = new Uint8Array(this.width * this.height * 4);

            // フレームバッファをピクセルバッファにロード
            gl.bindBuffer(gl.PIXEL_PACK_BUFFER, this.pixelBuffer.pbo);
            gl.readPixels(0, 0, this.width, this.height, gl.RGBA, gl.UNSIGNED_BYTE, <null>0);

            // ピクセルバッファからCPU側の配列にロード
            gl.fenceSync(gl.SYNC_GPU_COMMANDS_COMPLETE, 0);
            gl.getBufferSubData(gl.PIXEL_PACK_BUFFER, 0, data);
            gl.bindBuffer(gl.PIXEL_PACK_BUFFER, null);

            return data;
        }

    }

}

// フィルタカーネル
const kernels: { [name: string]: number[] } = {
    normal: [
        0, 0, 0,
        0, 1, 0,
        0, 0, 0
    ],
    gaussianBlur: [
        0.045, 0.122, 0.045,
        0.122, 0.332, 0.122,
        0.045, 0.122, 0.045
    ],
}

function main() {
    const image = new Image();
    image.src = "leaves.jpg";
    image.onload = () => { render(image); }
}

function render(image: HTMLImageElement) {
    const canvas1 = document.getElementById("can1") as HTMLCanvasElement;
    const gl = canvas1.getContext('webgl2') as WebGL2RenderingContext;

    const canvas2 = document.getElementById("can2") as HTMLCanvasElement;
    const ctx = canvas2.getContext('2d') as CanvasRenderingContext2D;

    if (!gl) {
        console.error("context does not exist");
    } else {

        // フィルタシェーダを読み込み
        const program = Program.loadShaderById(gl, ["2d-vertex-shader", "2d-fragment-shader"], [gl.VERTEX_SHADER, gl.FRAGMENT_SHADER]);
        const program2 = Program.loadShaderById(gl, ["2d-vertex-shader-2", "2d-fragment-shader-2"], [gl.VERTEX_SHADER, gl.FRAGMENT_SHADER]);

        const s0 = Date.now();
        const ip = new ImageProcessing(gl, image);
        console.log("initialize time: ", Date.now() - s0);
        
        //// フィルタを適用
        const s1 = Date.now();
        ip.applyKernel(kernels["gaussianBlur"], program);
        console.log("gaussianBlur time: ", Date.now() - s1);

        const s2 = Date.now();
        ip.applyShader(program2);
        console.log("swap r and b time: ", Date.now() - s2);

        // レンダリング結果をImageDataとして取得
        const s3 = Date.now();
        const data = ip.download();
        console.log("capture rendering data: ", Date.now() - s3);

        // CPU側の配列からcanvasに転送
        const s4 = Date.now();
        const imageData = ctx.createImageData(image.width, image.height);
        imageData.data.set(data);
        console.log("copy to context: ", Date.now() - s4);

        ctx.putImageData(imageData, 0, 0);
        return;
    }
}

window.requestAnimationFrame(main);

