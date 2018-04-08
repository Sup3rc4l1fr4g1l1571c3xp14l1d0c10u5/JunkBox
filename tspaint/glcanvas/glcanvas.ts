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


interface IPoint {
    x: number;
    y: number;
}

type Matrix3 = [number, number, number, number, number, number, number, number, number];

module Matrix3 {
    export function projection(width:number, height:number) : Matrix3 {
        // Note: This matrix flips the Y axis so that 0 is at the top.
        return [
            2 / width, 0, 0,
            0,  2 / height, 0,
            -1,-1, 1
        ];
    }

    export function identity(): Matrix3 {
        return [
            1, 0, 0,
            0, 1, 0,
            0, 0, 1
        ];
    }

    export function translation(tx:number, ty:number) : Matrix3 {
        return [
            1, 0, 0,
            0, 1, 0,
            tx, ty, 1
        ];
    }

    export function rotation(angleInRadians:number) : Matrix3 {
        const c = Math.cos(angleInRadians);
        const s = Math.sin(angleInRadians);
        return [
            c, -s, 0,
            s, c, 0,
            0, 0, 1
        ];
    }

    export function scaling(sx:number, sy:number) : Matrix3 {
        return [
            sx, 0, 0,
            0, sy, 0,
            0, 0, 1
        ];
    }

    export function multiply(a:Matrix3, b:Matrix3) : Matrix3 {
        const a00 = a[0 * 3 + 0];
        const a01 = a[0 * 3 + 1];
        const a02 = a[0 * 3 + 2];
        const a10 = a[1 * 3 + 0];
        const a11 = a[1 * 3 + 1];
        const a12 = a[1 * 3 + 2];
        const a20 = a[2 * 3 + 0];
        const a21 = a[2 * 3 + 1];
        const a22 = a[2 * 3 + 2];
        const b00 = b[0 * 3 + 0];
        const b01 = b[0 * 3 + 1];
        const b02 = b[0 * 3 + 2];
        const b10 = b[1 * 3 + 0];
        const b11 = b[1 * 3 + 1];
        const b12 = b[1 * 3 + 2];
        const b20 = b[2 * 3 + 0];
        const b21 = b[2 * 3 + 1];
        const b22 = b[2 * 3 + 2];
        return [
            b00 * a00 + b01 * a10 + b02 * a20,
            b00 * a01 + b01 * a11 + b02 * a21,
            b00 * a02 + b01 * a12 + b02 * a22,
            b10 * a00 + b11 * a10 + b12 * a20,
            b10 * a01 + b11 * a11 + b12 * a21,
            b10 * a02 + b11 * a12 + b12 * a22,
            b20 * a00 + b21 * a10 + b22 * a20,
            b20 * a01 + b21 * a11 + b22 * a21,
            b20 * a02 + b21 * a12 + b22 * a22
        ];
    }
}

class Shader {
    constructor(public gl: WebGL2RenderingContext, public shader: WebGLShader, public shaderType: number) { }

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
    constructor(public gl: WebGL2RenderingContext, public program: WebGLProgram) { }
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

class Surface {
    gl: WebGL2RenderingContext;
    texture: WebGLTexture;
    framebuffer: WebGLFramebuffer;
    width: number;
    height: number;

    private static createAndBindTexture(gl: WebGL2RenderingContext): WebGLTexture {
        const texture = gl.createTexture();
        gl.activeTexture(gl.TEXTURE0);
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
    constructor(gl: WebGL2RenderingContext, srcImageOrWidth: HTMLImageElement | ImageData | number, height?: number) {
        this.gl = gl;

        // フレームバッファテクスチャを生成。ピクセルフォーマットはRGBA(8bit)。画像サイズは指定されたものを使用。
        this.texture = Surface.createAndBindTexture(gl);
        if (srcImageOrWidth instanceof HTMLImageElement) {
            const img = srcImageOrWidth as HTMLImageElement;
            this.width = img.width;
            this.height = img.height;
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
        } else if (srcImageOrWidth instanceof ImageData) {
                const img = srcImageOrWidth as ImageData;
                this.width = img.width;
                this.height = img.height;
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
        } else {
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, srcImageOrWidth, height, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
            this.width = srcImageOrWidth;
            this.height = height;
        }

        // フレームバッファを生成し、テクスチャと関連づけ
        this.framebuffer = gl.createFramebuffer();
        gl.bindFramebuffer(gl.FRAMEBUFFER, this.framebuffer);
        gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, this.texture, 0);
    }

    clear(): void {
        const gl = this.gl;
        gl.bindFramebuffer(gl.FRAMEBUFFER, this.framebuffer);
        gl.viewport(0, 0, this.width, this.height);
        gl.clearColor(0,0,0,0);
        gl.clear(gl.COLOR_BUFFER_BIT);

    }

}
class PixelBuffer {
    pbo: WebGLBuffer;
    data: Uint8ClampedArray;

    constructor(public gl: WebGL2RenderingContext, public width: number, public height: number) {
        this.pbo = gl.createBuffer();
        this.data = new Uint8ClampedArray(this.width * this.height * 4);
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, this.pbo);
        gl.bufferData(gl.PIXEL_PACK_BUFFER, this.width * this.height * 4, gl.STREAM_READ);
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, null);
    }
    capture(src: Surface): Uint8ClampedArray {
        const gl = this.gl;

        gl.bindFramebuffer(gl.FRAMEBUFFER, src.framebuffer);

        // フレームバッファをピクセルバッファにロード
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, this.pbo);
        gl.readPixels(0, 0, this.width, this.height, gl.RGBA, gl.UNSIGNED_BYTE, <ArrayBufferView><any>0);

        // ピクセルバッファからCPU側の配列にロード
        gl.fenceSync(gl.SYNC_GPU_COMMANDS_COMPLETE, 0);
        gl.getBufferSubData(gl.PIXEL_PACK_BUFFER, 0, this.data);
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, null);
        return this.data;
    }
}
class ImageProcessing {
    gl: WebGL2RenderingContext;

    positionBuffer: WebGLBuffer;
    texcoordBuffer: WebGLBuffer;

    // フィルタカーネルの重み合計（重み合計が0以下の場合は1とする）
    private static computeKernelWeight(kernel: Float32Array): number {
        const weight = kernel.reduce((prev, curr) => prev + curr);
        return weight <= 0 ? 1 : weight;
    }

    constructor(gl: WebGL2RenderingContext) {
        this.gl = gl;

        // 頂点バッファ（座標）を作成
        this.positionBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);

        // 頂点バッファ（テクスチャ座標）を作成
        this.texcoordBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);

    }

    private setVertexPosition(array: number[]) {
        const gl = this.gl;

        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(array), gl.STATIC_DRAW);
    }

    private setTexturePosition(array: number[]) {
        const gl = this.gl;

        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(array), gl.STATIC_DRAW);
    }

    createBlankOffscreenTarget(width: number, height: number): Surface {
        return new Surface(this.gl, width, height);
    }
    createOffscreenTargetFromImage(image: HTMLImageElement): Surface {
        return new Surface(this.gl, image);
    }

    static createRectangle(x1: number, y1: number, x2: number, y2: number, target?: number[]): number[] {
        if (target) {
            Array.prototype.push.call(target,
                x1, y1,
                x2, y1,
                x1, y2,
                x1, y2,
                x2, y1,
                x2, y2
            );
            return target;
        } else {
            return [
                x1, y1,
                x2, y1,
                x1, y2,
                x1, y2,
                x2, y1,
                x2, y2
            ];
        }
    }

    applyKernel(dst: Surface, src: Surface, { kernel = null, program = null }: { kernel: Float32Array, program: Program }) {
        const gl = this.gl;

        // arrtibute変数の位置を取得
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");

        // uniform変数の位置を取得
        //const resolutionLocation = gl.getUniformLocation(program.program, "u_resolution");
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");
        const textureSizeLocation = gl.getUniformLocation(program.program, "u_textureSize");
        const kernelLocation = gl.getUniformLocation(program.program, "u_kernel[0]");
        const kernelWeightLocation = gl.getUniformLocation(program.program, "u_kernelWeight");
        const texture0Lication = gl.getUniformLocation(program.program, 'texture0');

        // シェーダを設定
        gl.useProgram(program.program);

        // シェーダの頂点座標Attributeを有効化
        gl.enableVertexAttribArray(positionLocation);

        // 頂点バッファ（座標）を設定
        this.setVertexPosition(ImageProcessing.createRectangle(0, 0, src.width, src.height));

        // 頂点座標Attributeの位置情報を設定
        gl.vertexAttribPointer(
            positionLocation,
            2,          // 2 components per iteration
            gl.FLOAT,   // the data is 32bit floats
            false,      // don't normalize the data
            0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
            0           // start at the beginning of the buffer
        );

        // シェーダのテクスチャ座標Attributeを有効化
        gl.enableVertexAttribArray(texcoordLocation);

        // 頂点バッファ（テクスチャ座標）を設定
        this.setTexturePosition(ImageProcessing.createRectangle(0, 0, 1, 1));

        // テクスチャ座標Attributeの位置情報を設定
        gl.vertexAttribPointer(
            texcoordLocation,
            2,          // 2 components per iteration
            gl.FLOAT,   // the data is 32bit floats
            false,      // don't normalize the data
            0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
            0           // start at the beginning of the buffer
        );

        // 変換行列を設定
        const projectionMatrix = Matrix3.projection(dst.width, dst.height);
        gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);

        // フィルタ演算で使う入力テクスチャサイズを設定
        gl.uniform2f(textureSizeLocation, src.width, src.height);

        // 入力元とするレンダリングターゲットのテクスチャを選択
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, src.texture);
        gl.uniform1i(texture0Lication, 0);

        // 出力先とするレンダリングターゲットのフレームバッファを選択し、ビューポートを設定
        gl.bindFramebuffer(gl.FRAMEBUFFER, dst.framebuffer);
        gl.viewport(0, 0, dst.width, dst.height);

        // カーネルシェーダを適用したオフスクリーンレンダリングを実行
        gl.uniform1fv(kernelLocation, kernel);
        gl.uniform1f(kernelWeightLocation, ImageProcessing.computeKernelWeight(kernel));
        gl.drawArrays(gl.TRIANGLES, 0, 6);

        // フィルタ適用完了

    }
    applyShader(dst: Surface, src: Surface, { program = null }: { program: Program }): void {
        const gl = this.gl;
        // arrtibute変数の位置を取得
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");

        // uniform変数の位置を取得
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");
        const texture0Lication = gl.getUniformLocation(program.program, 'texture0');

        // シェーダを設定
        gl.useProgram(program.program);

        // シェーダの頂点座標Attributeを有効化
        gl.enableVertexAttribArray(positionLocation);

        // 頂点バッファ（座標）を設定
        this.setVertexPosition(ImageProcessing.createRectangle(0, 0, src.width, src.height));

        // 頂点座標Attributeの位置情報を設定
        gl.vertexAttribPointer(
            positionLocation,
            2,          // 2 components per iteration
            gl.FLOAT,   // the data is 32bit floats
            false,      // don't normalize the data
            0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
            0           // start at the beginning of the buffer
        );

        // シェーダのテクスチャ座標Attributeを有効化
        gl.enableVertexAttribArray(texcoordLocation);

        // 頂点バッファ（テクスチャ座標）を設定
        this.setTexturePosition(ImageProcessing.createRectangle(0, 0, 1, 1));

        // テクスチャ座標Attributeの位置情報を設定
        gl.vertexAttribPointer(
            texcoordLocation,
            2,          // 2 components per iteration
            gl.FLOAT,   // the data is 32bit floats
            false,      // don't normalize the data
            0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
            0           // start at the beginning of the buffer
        );

        //// テクスチャサイズ情報ををシェーダのUniform変数に設定
        //gl.uniform2f(textureSizeLocation, this.width, this.height);

        // 入力元とするレンダリングターゲットのテクスチャを選択
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, src.texture);
        gl.uniform1i(texture0Lication, 0);

        if (dst == null) {
            // オフスクリーンレンダリングにはしない
            gl.bindFramebuffer(gl.FRAMEBUFFER, null);
            if (gl.canvas.width !== gl.canvas.clientWidth || gl.canvas.height !== gl.canvas.clientHeight) {
                gl.canvas.width = gl.canvas.clientWidth;
                gl.canvas.height = gl.canvas.clientHeight;
            }
            gl.clearColor(0,0,0,0);
            gl.clear(gl.COLOR_BUFFER_BIT);
            //const projectionMatrix = Matrix3.projection(gl.canvas.width, gl.canvas.height);
            const projectionMatrix = Matrix3.multiply(Matrix3.scaling(1, -1), Matrix3.projection(gl.canvas.width, gl.canvas.height));
            gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);
            gl.viewport(0, 0, gl.canvas.width, gl.canvas.height);
        } else {
            // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
            gl.bindFramebuffer(gl.FRAMEBUFFER, dst.framebuffer);
            const projectionMatrix = Matrix3.projection(dst.width, dst.height);
            gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);
            gl.viewport(0, 0, dst.width, dst.height);
        }

        // オフスクリーンレンダリングを実行
        gl.drawArrays(gl.TRIANGLES, 0, 6);

        // 適用完了

    }

    drawLines(dst: Surface, { program = null, vertexes = null, size = null, color = null }: { program: Program, vertexes: Float32Array, size: number, color: [number, number, number, number] }): void {
        const gl = this.gl;

        // arrtibute変数の位置を取得
        const positionLocation = gl.getAttribLocation(program.program, "a_position");

        // uniform変数の位置を取得
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");
        const startLocation = gl.getUniformLocation(program.program, "u_start");
        const endLocation = gl.getUniformLocation(program.program, "u_end");
        const sizeLocation = gl.getUniformLocation(program.program, "u_size");
        const colorLocation = gl.getUniformLocation(program.program, "u_color");
        //const texture0Lication = gl.getUniformLocation(program.program, 'texture0');

        // シェーダを設定
        gl.useProgram(program.program);

        // シェーダの頂点座標Attributeを有効化
        gl.enableVertexAttribArray(positionLocation);

        // 入力元とするレンダリングターゲットはなし
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, null);
        //gl.uniform1i(texture0Lication, 0);

        // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
        gl.bindFramebuffer(gl.FRAMEBUFFER, dst.framebuffer);
        gl.viewport(0, 0, dst.width, dst.height);

        const projectionMatrix = Matrix3.projection(dst.width, dst.height);
        gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);
        
        // 色を設定
        gl.uniform4fv(colorLocation, color);

        // 頂点バッファ（座標）をワークバッファに切り替えるが、頂点情報は後で設定
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);

        // 頂点座標Attributeの位置情報を設定
        gl.vertexAttribPointer(
            positionLocation,
            2,          // 2 components per iteration
            gl.FLOAT,   // the data is 32bit floats
            false,      // don't normalize the data
            0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
            0           // start at the beginning of the buffer
        );


        const len = ~~(vertexes.length / 2) - 1;

        // サイズを設定
        gl.uniform1f(sizeLocation, size / 2);

        // 矩形設定
        for (let i = 0; i < len; i++) {
            const x1 = vertexes[i * 2 + 0] + 0.5;
            const y1 = vertexes[i * 2 + 1] + 0.5;
            const x2 = vertexes[i * 2 + 2] + 0.5;
            const y2 = vertexes[i * 2 + 3] + 0.5;

            const left = Math.min(x1, x2) - size * 2;
            const top = Math.min(y1, y2) - size * 2;
            const right = Math.max(x1, x2) + size * 2;
            const bottom = Math.max(y1, y2) + size * 2;

            gl.uniform2f(startLocation, x1, y1);
            gl.uniform2f(endLocation, x2, y2);

            this.setVertexPosition(ImageProcessing.createRectangle(left, top, right, bottom));

            // カーネルシェーダを適用したオフスクリーンレンダリングを実行
            gl.drawArrays(gl.TRIANGLES, 0, 6);
        }

    }

    composit(dst: Surface, front: Surface, back: Surface, program: Program) {
        const gl = this.gl;
        // arrtibute変数の位置を取得
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");

        // uniform変数の位置を取得
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");
        const texture0Lication = gl.getUniformLocation(program.program, 'u_front');
        const texture1Lication = gl.getUniformLocation(program.program, 'u_back');

        // シェーダを設定
        gl.useProgram(program.program);

        // シェーダの頂点座標Attributeを有効化
        gl.enableVertexAttribArray(positionLocation);

        // 頂点バッファ（座標）を設定
        this.setVertexPosition(ImageProcessing.createRectangle(0, 0, front.width, front.height));

        // 頂点座標Attributeの位置情報を設定
        gl.vertexAttribPointer(
            positionLocation,
            2,          // 2 components per iteration
            gl.FLOAT,   // the data is 32bit floats
            false,      // don't normalize the data
            0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
            0           // start at the beginning of the buffer
        );

        // シェーダのテクスチャ座標Attributeを有効化
        gl.enableVertexAttribArray(texcoordLocation);

        // 頂点バッファ（テクスチャ座標）を設定
        this.setTexturePosition(ImageProcessing.createRectangle(0, 0, 1, 1));

        // テクスチャ座標Attributeの位置情報を設定
        gl.vertexAttribPointer(
            texcoordLocation,
            2,          // 2 components per iteration
            gl.FLOAT,   // the data is 32bit floats
            false,      // don't normalize the data
            0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
            0           // start at the beginning of the buffer
        );

        const projectionMatrix = Matrix3.projection(dst.width, dst.height);
        gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);

        //// テクスチャサイズ情報ををシェーダのUniform変数に設定
        //gl.uniform2f(textureSizeLocation, this.width, this.height);

        // 入力元とするレンダリングターゲットのテクスチャを選択
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, front.texture);
        gl.uniform1i(texture0Lication, 0);

        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_2D, back.texture);
        gl.uniform1i(texture1Lication, 1);

        // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
        gl.bindFramebuffer(gl.FRAMEBUFFER, dst.framebuffer);
        gl.viewport(0, 0, dst.width, dst.height);

        // オフスクリーンレンダリングを実行
        gl.drawArrays(gl.TRIANGLES, 0, 6);

        // 適用完了
    }

    createPixelBuffer(width: number, height: number): PixelBuffer {
        return new PixelBuffer(this.gl, width, height);
    }

}

// フィルタカーネル
const kernels: { [name: string]: Float32Array } = {
    normal: new Float32Array([
        0, 0, 0,
        0, 1, 0,
        0, 0, 0
    ]),
    gaussianBlur: new Float32Array([
        0.045, 0.122, 0.045,
        0.122, 0.332, 0.122,
        0.045, 0.122, 0.045
    ]),
}

function main() {
    const image = new Image();
    image.src = "leaves.jpg";
    image.onload = () => { render(image); }
}

function render(image: HTMLImageElement) {

    const canvas1 = document.getElementById("can2") as HTMLCanvasElement;
    //const canvas1 = document.createElement("canvas") as HTMLCanvasElement;
    const gl = canvas1.getContext('webgl2') as WebGL2RenderingContext;

    const canvas2 = document.getElementById("can2") as HTMLCanvasElement;
    const ctx = canvas2.getContext('2d') as CanvasRenderingContext2D;

    if (!gl) {
        console.error("context does not exist");
    } else {
        // フィルタシェーダを読み込み
        const program = Program.loadShaderById(gl, ["2d-vertex-shader", "2d-fragment-shader"], [gl.VERTEX_SHADER, gl.FRAGMENT_SHADER]);
        const program2 = Program.loadShaderById(gl, ["2d-vertex-shader-2", "2d-fragment-shader-2"], [gl.VERTEX_SHADER, gl.FRAGMENT_SHADER]);
        const program3 = Program.loadShaderById(gl, ["2d-vertex-shader-3", "2d-fragment-shader-3"], [gl.VERTEX_SHADER, gl.FRAGMENT_SHADER]);
        const program4 = Program.loadShaderById(gl, ["2d-vertex-shader-4", "2d-fragment-shader-4"], [gl.VERTEX_SHADER, gl.FRAGMENT_SHADER]);
        const program5 = Program.loadShaderById(gl, ["2d-vertex-shader-5", "2d-fragment-shader-5"], [gl.VERTEX_SHADER, gl.FRAGMENT_SHADER]);

        const s0 = Date.now();
        const ip = new ImageProcessing(gl);

        console.log("initialize time: ", Date.now() - s0);

        // 作業エリアなどを作る
        const t1 = ip.createOffscreenTargetFromImage(image);
        
        const t2 = ip.createBlankOffscreenTarget(image.width, image.height);
        const t3 = ip.createBlankOffscreenTarget(image.width, image.height);
        const t4 = ip.createBlankOffscreenTarget(image.width, image.height);
        const ret = ip.createPixelBuffer(image.width, image.height);

        // フィルタを適用
        const s1 = Date.now();
        //ip.applyKernel(t2, t1, { kernel: kernels["gaussianBlur"], program: program });
        console.log("gaussianBlur time: ", Date.now() - s1);

        // シェーダを適用
        const s2 = Date.now();
        //ip.applyShader(t1, t2, { program: program2 });
        console.log("swap r and b time: ", Date.now() - s2);

        // ブラシを想定した線引き
        const s3 = Date.now();
        ip.drawLines(t3, { program: program3, vertexes: new Float32Array([100, 100, 150, 150]), size: 5, color: [0, 0, 1, 0.1] });
        console.log("drawline: ", Date.now() - s3);

        // レイヤー合成
        const s4 = Date.now();
        ip.composit(t4, t3, t2, program4);
        console.log("composit layer: ", Date.now() - s4);

        //// 消しゴム合成（レイヤー合成の特殊形）
        //const s4 = Date.now();
        //ip.composit(t4, t3, t2, program5);
        //console.log("eraser: ", Date.now() - s4);
        /*
        // レンダリング結果をUint8Arrayとして取得
        const s5 = Date.now();
        const data = ret.capture(t4);
        console.log("capture rendering data: ", Date.now() - s5);

        // Uint8Arrayからcanvasに転送
        const s6 = Date.now();
        const imageData = ctx.createImageData(image.width, image.height);
        imageData.data.set(data);
        console.log("copy to context: ", Date.now() - s6);

        ctx.putImageData(imageData, 0, 0);

        //*/
        // WebGL の描画結果を HTML に正しく合成する方法 より
        // http://webos-goodies.jp/archives/overlaying_webgl_on_html.html
        gl.enable(gl.BLEND);
        gl.blendEquation(gl.FUNC_ADD);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
        gl.blendFuncSeparate(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA, gl.ONE, gl.ZERO);
        // canvasにレンダリング
        ip.applyShader(null, t3, { program: program2 });


        return;
    }
}

window.requestAnimationFrame(main);
