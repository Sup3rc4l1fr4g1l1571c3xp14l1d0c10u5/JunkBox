// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />
/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.dom.d.ts" />
/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.es2016.d.ts" />
"use strict";
///////////////////////////////////////////////////////////////
function saveFileToLocal(filename: string, blob: Blob): void {
    const blobURL = window.URL.createObjectURL(blob, { oneTimeOnly: true });
    const element: HTMLAnchorElement = document.createElement("a");
    element.href = blobURL;
    element.style.display = "none";
    element.download = filename;
    document.body.appendChild(element);
    setTimeout(() => {
        element.click();
        document.body.removeChild(element);
    });
}
function loadFileFromLocal(accept: string): Promise<Blob> {
    return new Promise<Blob>((resolve, reject) => {
        const element: HTMLInputElement = document.createElement("input");
        element.type = "file";
        element.style.display = "none";
        element.accept = accept;
        document.body.appendChild(element);
        element.onclick = () => {
            const handler = () => {
                element.onclick = null;
                window.removeEventListener("focus", handler);
                document.body.removeChild(element);
                const file = (element.files.length != 0) ? element.files[0] : null;
                if (file == null) {
                    reject("cannot load file.");
                    return;
                } else {
                    resolve(file);
                    return;
                }
            };
            window.addEventListener("focus", handler);
        };
        setTimeout(() => { element.click() });
    });
}
///////////////////////////////////////////////////////////////
namespace JSZip {

    export interface JSZip {
        files: { [key: string]: JSZipObject };

        /**
         * Get a file from the archive
         *
         * @param Path relative path to file
         * @return File matching path, null if no file found
         */
        file(path: string): JSZipObject;

        /**
         * Get files matching a RegExp from archive
         *
         * @param path RegExp to match
         * @return Return all matching files or an empty array
         */
        file(path: RegExp): JSZipObject[];

        /**
         * Add a file to the archive
         *
         * @param path Relative path to file
         * @param content Content of the file
         * @param options Optional information about the file
         * @return JSZip object
         */
        file(path: string, data: any, options?: JSZipFileOptions): JSZip;

        /**
         * Return an new JSZip instance with the given folder as root
         *
         * @param name Name of the folder
         * @return New JSZip object with the given folder as root or null
         */
        folder(name: string): JSZip;

        /**
         * Returns new JSZip instances with the matching folders as root
         *
         * @param name RegExp to match
         * @return New array of JSZipFile objects which match the RegExp
         */
        folder(name: RegExp): JSZipObject[];

        /**
         * Call a callback function for each entry at this folder level.
         *
         * @param callback function
         */
        forEach(callback: (relativePath: string, file: JSZipObject) => void): void;

        /**
         * Get all files wchich match the given filter function
         *
         * @param predicate Filter function
         * @return Array of matched elements
         */
        filter(predicate: (relativePath: string, file: JSZipObject) => boolean): JSZipObject[];

        /**
         * Removes the file or folder from the archive
         *
         * @param path Relative path of file or folder
         * @return Returns the JSZip instance
         */
        remove(path: string): JSZip;

        /**
         * @deprecated since version 3.0
         * @see {@link generateAsync}
         * http://stuk.github.io/jszip/documentation/upgrade_guide.html
         */
        generate(options?: JSZipGeneratorOptions): any;

        /**
         * Generates a new archive asynchronously
         *
         * @param options Optional options for the generator
         * @param onUpdate The optional function called on each internal update with the metadata
         * @return The serialized archive
         */
        generateAsync(options?: JSZipGeneratorOptions, onUpdate?: Function): Promise<any>;

        /**
         * Generates the complete zip file as a nodejs stream asynchronously
         *
         * @param options Optional options for the generator
         * @param onUpdate The optional function called on each internal update with the metadata
         * @return The serialized archive as a NodeJS ReadableStream
         */
        generateNodeStream(options?: JSZipGeneratorOptions, onUpdate?: Function): any;

        /**
         * @deprecated since version 3.0
         * @see {@link loadAsync}
         * http://stuk.github.io/jszip/documentation/upgrade_guide.html
         */
        load(): void;

        /**
         * Deserialize zip file asynchronously
         *
         * @param data Serialized zip file
         * @param options Options for deserializing
         * @return Returns promise
         */
        loadAsync(data: any, options?: JSZipLoadOptions): Promise<JSZip>;
    }

    interface JSZipObject {
        name: string;
        dir: boolean;
        date: Date;
        comment: string;
        options: JSZipObjectOptions;

        /**
         * Prepare the content in the asked type.
         * @param {String} type the type of the result.
         * @param {Function} onUpdate a function to call on each internal update.
         * @return Promise the promise of the result.
         */
        async(type: Serialization, onUpdate?: Function): Promise<any>;

        /**
         * @deprecated since version 3.0
         */
        asText(): void;
        /**
         * @deprecated since version 3.0
         */
        asBinary(): void;
        /**
         * @deprecated since version 3.0
         */
        asArrayBuffer(): void;
        /**
         * @deprecated since version 3.0
         */
        asUint8Array(): void;
    }

    type Serialization = ("string" | "text" | "base64" | "binarystring" |
        "uint8array" | "arraybuffer" | "blob" | "nodebuffer");

    interface JSZipFileOptions {
        base64?: boolean;
        binary?: boolean;
        date?: Date;
        compression?: string;
        comment?: string;
        optimizedBinaryString?: boolean;
        createFolders?: boolean;
    }

    interface JSZipObjectOptions {
        /** deprecated */
        base64: boolean;
        /** deprecated */
        binary: boolean;
        /** deprecated */
        dir: boolean;
        /** deprecated */
        date: Date;
        compression: string;
    }

    interface JSZipGeneratorOptions {
        compression?: string;
        compressionOptions?: any;
        type?: string;
        comment?: string;
        mimeType?: string;
        platform?: string;
        encodeFileName?: string;
        streamFiles?: boolean;
    }

    export interface JSZipLoadOptions {
        base64?: boolean;
        checkCRC32?: boolean;
        optimizedBinaryString?: boolean;
        createFolders?: boolean;
    }

    export interface JSZipSupport {
        arraybuffer: boolean;
        uint8array: boolean;
        blob: boolean;
        nodebuffer: boolean;
        nodestream: boolean;
    }

}

declare var JSZip: {
    /**
     * Create JSZip instance
     */
    (): JSZip.JSZip;
    /**
     * Create JSZip instance
     * If no parameters given an empty zip archive will be created
     *
     * @param data Serialized zip archive
     * @param options Description of the serialized zip archive
     */
    (data: any, options?: JSZip.JSZipLoadOptions): JSZip.JSZip;

    /**
     * Create JSZip instance
     */
    new (): JSZip.JSZip;
    /**
     * Create JSZip instance
     * If no parameters given an empty zip archive will be created
     *
     * @param data Serialized zip archive
     * @param options Description of the serialized zip archive
     */
    new (data: any, options?: JSZip.JSZipLoadOptions): JSZip.JSZip;

    prototype: JSZip.JSZip;
    support: JSZip.JSZipSupport;
};
///////////////////////////////////////////////////////////////
interface HTMLElement {
    [key: string]: any;
}
interface HTMLAnchorElement {
    download: string;
}
///////////////////////////////////////////////////////////////
interface CanvasRenderingContext2D {
    mozImageSmoothingEnabled: boolean;
    imageSmoothingEnabled: boolean;
    webkitImageSmoothingEnabled: boolean;

    /**
     * 楕円描画 (experimental)
     * @param x 
     * @param y 
     * @param radiusX 
     * @param radiusY 
     * @param rotation 
     * @param startAngle 
     * @param endAngle 
     * @param anticlockwise 
     * @returns {} 
     */
    ellipse: (x: number, y: number, radiusX: number, radiusY: number, rotation: number, startAngle: number, endAngle: number, anticlockwise?: boolean) => void;

    /**
     * 指定した矩形内に収まるように文字列を描画
     * @param text 
     * @param left 
     * @param top 
     * @param width 
     * @param height 
     * @param drawTextPred 
     * @returns {} 
     */
    drawTextBox: (text: string, left: number, top: number, width: number, height: number, drawTextPred: (text: string, x: number, y: number, maxWidth?: number) => void) => void;
    fillTextBox: (text: string, left: number, top: number, width: number, height: number) => void;
    strokeTextBox: (text: string, left: number, top: number, width: number, height: number) => void;
    strokeRectOriginal: (x: number, y: number, w: number, h: number) => void;

};
CanvasRenderingContext2D.prototype.strokeRectOriginal = CanvasRenderingContext2D.prototype.strokeRect;
CanvasRenderingContext2D.prototype.strokeRect = function (x: number, y: number, w: number, h: number): void {
    this.strokeRectOriginal(x + 0.5, y + 0.5, w - 1, h - 1);
}
CanvasRenderingContext2D.prototype.drawTextBox = function (text: string, left: number, top: number, width: number, height: number, drawTextPred: (text: string, x: number, y: number, maxWidth?: number) => void) {
    const metrics = this.measureText(text);
    const lineHeight = this.measureText("あ").width;
    const lines = text.split(/\n/);
    let offY = 0;
    lines.forEach((x: string, i: number) => {
        const metrics = this.measureText(x);
        const sublines: string[] = [];
        if (metrics.width > width) {
            let len = 1;
            while (x.length > 0) {
                const metrics = this.measureText(x.substr(0, len));
                if (metrics.width > width) {
                    sublines.push(x.substr(0, len - 1));
                    x = x.substring(len - 1);
                    len = 1;
                } else if (len == x.length) {
                    sublines.push(x);
                    break;
                } else {
                    len++;
                }
            }
        } else {
            sublines.push(x);
        }
        sublines.forEach((x) => {
            drawTextPred(x, left + 1, top + offY + 1);
            offY += (lineHeight + 1);
        });
    });
};
CanvasRenderingContext2D.prototype.fillTextBox = function (text: string, left: number, top: number, width: number, height: number) {
    this.drawTextBox(text, left, top, width, height, this.fillText.bind(this));
}
CanvasRenderingContext2D.prototype.strokeTextBox = function (text: string, left: number, top: number, width: number, height: number) {
    this.drawTextBox(text, left, top, width, height, this.strokeText.bind(this));
}
///////////////////////////////////////////////////////////////
type RGB = [number, number, number];
type RGBA = [number, number, number, number];
type HSV = [number, number, number];
type HSVA = [number, number, number, number];
function rgb2hsv([r, g, b]: RGB): HSV {
    const max = Math.max(r, g, b);
    const min = Math.min(r, g, b);
    let h = max - min;
    if (h > 0.0) {
        if (max == r) {
            h = (g - b) / h;
            if (h < 0.0) {
                h += 6.0;
            }
        } else if (max == g) {
            h = 2.0 + (b - r) / h;
        } else {
            h = 4.0 + (r - g) / h;
        }
    }
    h /= 6.0;
    let s = (max - min);
    if (max != 0.0) {
        s /= max;
    }
    let v = max;

    return [~~(h * 360), s, v]
}
function hsv2rgb([h, s, v]: HSV): RGB {
    if (s == 0) {
        const vv: number = ~~(v * 255 + 0.5);
        return [vv, vv, vv];
    } else {
        const t: number = ((h * 6) % 360) / 360.0;
        const c1: number = v * (1 - s);
        const c2: number = v * (1 - s * t);
        const c3: number = v * (1 - s * (1 - t));

        let r: number = 0;
        let g: number = 0;
        let b: number = 0;
        switch (~~(h / 60)) {
            case 0: r = v; g = c3; b = c1; break;
            case 1: r = c2; g = v; b = c1; break;
            case 2: r = c1; g = v; b = c3; break;
            case 3: r = c1; g = c2; b = v; break;
            case 4: r = c3; g = c1; b = v; break;
            case 5: r = v; g = c1; b = c2; break;
            default: throw new Error();
        }
        const rr: number = ~~(r * 255 + 0.5);
        const gg: number = ~~(g * 255 + 0.5);
        const bb: number = ~~(b * 255 + 0.5);

        return [rr, gg, bb];
    }

}
///////////////////////////////////////////////////////////////
interface WebGL2RenderingContext extends WebGLRenderingContext {
    PIXEL_PACK_BUFFER: number;
    STREAM_READ: number;
    SYNC_GPU_COMMANDS_COMPLETE: number;
    fenceSync: (sync: number, flag: number) => void;
    getBufferSubData: (target: number, offset: number, buffer: any) => void;
    uniformMatrix3fv(location: WebGLUniformLocation, transpose: boolean, value: Float32Array | number[]): void;
    uniform4fv(location: WebGLUniformLocation, v: Float32Array | number[]): void;
}
///////////////////////////////////////////////////////////////
type Matrix3 = [number, number, number, number, number, number, number, number, number];
module Matrix3 {
    export function projection(width: number, height: number): Matrix3 {
        // Note: This matrix flips the Y axis so that 0 is at the top.
        return [
            2 / width, 0, 0,
            0, 2 / height, 0,
            -1, -1, 1
        ];
    }

    export function identity(): Matrix3 {
        return [
            1, 0, 0,
            0, 1, 0,
            0, 0, 1
        ];
    }

    export function translation(tx: number, ty: number): Matrix3 {
        return [
            1, 0, 0,
            0, 1, 0,
            tx, ty, 1
        ];
    }

    export function rotation(angleInRadians: number): Matrix3 {
        const c = Math.cos(angleInRadians);
        const s = Math.sin(angleInRadians);
        return [
            c, -s, 0,
            s, c, 0,
            0, 0, 1
        ];
    }

    export function scaling(sx: number, sy: number): Matrix3 {
        return [
            sx, 0, 0,
            0, sy, 0,
            0, 0, 1
        ];
    }

    export function multiply(a: Matrix3, b: Matrix3): Matrix3 {
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
///////////////////////////////////////////////////////////////
interface IDisposable {
    dispose(): void;
}
///////////////////////////////////////////////////////////////
class Shader implements IDisposable {
    constructor(public gl: WebGL2RenderingContext, public shader: WebGLShader, public shaderType: number) { }

    public static create(gl: WebGL2RenderingContext, shaderSource: string, shaderType: number): Shader {
        const shader = gl.createShader(shaderType);
        gl.shaderSource(shader, shaderSource);
        gl.compileShader(shader);
        const compiled = gl.getShaderParameter(shader, gl.COMPILE_STATUS);
        if (!compiled) {
            const lastError = gl.getShaderInfoLog(shader);
            console.error("*** Error compiling shader '" + shader + "':" + lastError);
            gl.deleteShader(shader);
            return null;
        } else {
            return new Shader(gl, shader, shaderType);
        }
    }

    public static loadShaderById(gl: WebGL2RenderingContext, id: string, optShaderType?: number): Shader {
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
    public dispose(): void {
        this.gl.deleteShader(this.shader);
        this.shader = null;
        this.gl = null;
        this.shaderType = NaN;
    }
}
///////////////////////////////////////////////////////////////
class Program implements IDisposable {
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
    public dispose(): void {
        this.gl.deleteProgram(this.program);
        this.program = null;
        this.gl = null;
    }
}
///////////////////////////////////////////////////////////////
class Surface implements IDisposable {
    private _gl: WebGL2RenderingContext;
    private _width: number;
    private _height: number;
    private _texture: WebGLTexture;
    private _framebuffer: WebGLFramebuffer;

    public get gl(): WebGL2RenderingContext {
        return this._gl;
    }
    public get texture(): WebGLTexture {
        return this._texture;
    }
    public get framebuffer(): WebGLFramebuffer {
        return this._framebuffer;
    }
    public get width(): number {
        return this._width;
    }
    public get height(): number {
        return this._height;
    }

    constructor(gl: WebGL2RenderingContext, srcImage: HTMLImageElement | ImageData);
    constructor(gl: WebGL2RenderingContext, width: number, height: number);
    constructor(gl: WebGL2RenderingContext, srcImageOrWidth: HTMLImageElement | ImageData | number, height?: number) {
        this._gl = gl;

        // テクスチャを生成。ピクセルフォーマットはRGBA(8bit)。画像サイズは指定されたものを使用。
        this._texture = gl.createTexture();
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, this._texture);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);

        if (srcImageOrWidth instanceof HTMLImageElement) {
            const img = srcImageOrWidth as HTMLImageElement;
            this._width = img.width;
            this._height = img.height;
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
        } else if (srcImageOrWidth instanceof ImageData) {
            const img = srcImageOrWidth as ImageData;
            this._width = img.width;
            this._height = img.height;
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
        } else {
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, srcImageOrWidth, height, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
            this._width = srcImageOrWidth;
            this._height = height;
        }

        // フレームバッファを生成し、テクスチャと関連づけ
        this._framebuffer = gl.createFramebuffer();
        gl.bindFramebuffer(gl.FRAMEBUFFER, this._framebuffer);
        gl.viewport(0, 0, this._width, this._height);
        gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, this._texture, 0);
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
    }

    public clear(): void {
        const gl = this._gl;
        gl.bindFramebuffer(gl.FRAMEBUFFER, this.framebuffer);
        gl.viewport(0, 0, this._width, this._height);
        if (this._width == 0 || this._height == 0) {
            console.log();
        }
        gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT | gl.STENCIL_BUFFER_BIT);
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
    }

    public dispose(): void {
        const gl = this._gl;
        gl.deleteFramebuffer(this._framebuffer);
        gl.deleteTexture(this._texture);
        this._gl = null;
        this._texture = null;
        this._framebuffer = null;
        this._width = NaN;
        this._height = NaN;
    }
}
///////////////////////////////////////////////////////////////
class PixelBuffer implements IDisposable {
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
        gl.viewport(0, 0, this.width, this.height);

        // フレームバッファをピクセルバッファにロード
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, this.pbo);
        gl.readPixels(0, 0, this.width, this.height, gl.RGBA, gl.UNSIGNED_BYTE, <ArrayBufferView><any>0);

        // ピクセルバッファからCPU側の配列にロード
        gl.fenceSync(gl.SYNC_GPU_COMMANDS_COMPLETE, 0);
        gl.getBufferSubData(gl.PIXEL_PACK_BUFFER, 0, this.data);
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, null);
        return this.data;
    }
    dispose(): void {
        this.gl.deleteBuffer(this.pbo);
        this.pbo = null;
        this.data = null;
        this.gl = null;
        this.width = NaN;
        this.height = NaN;

    }
}
///////////////////////////////////////////////////////////////
class ImageProcessing {
    positionBuffer: WebGLBuffer;
    texcoordBuffer: WebGLBuffer;

    // フィルタカーネルの重み合計（重み合計が0以下の場合は1とする）
    private static computeKernelWeight(kernel: Float32Array): number {
        const weight = kernel.reduce((prev, curr) => prev + curr);
        return weight <= 0 ? 1 : weight;
    }

    constructor(public gl: WebGL2RenderingContext) {

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
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(array), gl.STREAM_DRAW);
    }

    private setTexturePosition(array: number[]) {
        const gl = this.gl;

        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(array), gl.STREAM_DRAW);
    }

    createBlankOffscreenTarget(width: number, height: number): Surface {
        const surf = new Surface(this.gl, width, height);
        surf.clear();
        return surf;
    }
    createOffscreenTargetFromImage(image: HTMLImageElement | ImageData): Surface {
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
    applyShader(dst: Surface, src: Surface, { program = null, matrix = null }: { program: Program, matrix?: Matrix3 }): void {
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

        const projMat = (dst == null) ? Matrix3.projection(gl.canvas.width, gl.canvas.height) : Matrix3.projection(dst.width, dst.height)
        const projectionMatrix = (matrix == null)
            ? Matrix3.multiply(Matrix3.scaling(1, (dst == null) ? -1 : 1), projMat)
            : Matrix3.multiply(Matrix3.multiply(Matrix3.scaling(1, (dst == null) ? -1 : 1), projMat), matrix);;

        if (dst == null) {
            // オフスクリーンレンダリングにはしない
            gl.bindFramebuffer(gl.FRAMEBUFFER, null);
            // WebGL の描画結果を HTML に正しく合成する方法 より
            // http://webos-goodies.jp/archives/overlaying_webgl_on_html.html
            gl.clearColor(0, 0, 0, 0);
            gl.clear(gl.COLOR_BUFFER_BIT | gl.STENCIL_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
            gl.enable(gl.BLEND);
            gl.blendEquation(gl.FUNC_ADD);
            gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
            gl.blendFuncSeparate(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA, gl.ONE, gl.ZERO);
            // 
            gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);
            gl.viewport(0, 0, gl.canvas.width, gl.canvas.height);
            // レンダリングを実行
            gl.drawArrays(gl.TRIANGLES, 0, 6);
            gl.disable(gl.BLEND);
        } else {
            // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
            gl.bindFramebuffer(gl.FRAMEBUFFER, dst.framebuffer);
            gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);
            gl.viewport(0, 0, dst.width, dst.height);
            // オフスクリーンレンダリングを実行
            gl.drawArrays(gl.TRIANGLES, 0, 6);
        }


        // 適用完了

    }
    applyShaderUpdate(dst: Surface, { program = null }: { program: Program }): void {
        const gl = this.gl;
        // arrtibute変数の位置を取得
        const positionLocation = gl.getAttribLocation(program.program, "a_position");

        // uniform変数の位置を取得
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");

        // シェーダを設定
        gl.useProgram(program.program);

        // シェーダの頂点座標Attributeを有効化
        gl.enableVertexAttribArray(positionLocation);

        // 頂点バッファ（座標）を設定
        this.setVertexPosition(ImageProcessing.createRectangle(0, 0, dst.width, dst.height));

        // 頂点座標Attributeの位置情報を設定
        gl.vertexAttribPointer(
            positionLocation,
            2,          // 2 components per iteration
            gl.FLOAT,   // the data is 32bit floats
            false,      // don't normalize the data
            0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
            0           // start at the beginning of the buffer
        );

        if (dst == null) {
            // オフスクリーンレンダリングにはしない
            gl.bindFramebuffer(gl.FRAMEBUFFER, null);
            const projectionMatrix = Matrix3.multiply(Matrix3.scaling(1, -1), Matrix3.projection(gl.canvas.width, gl.canvas.height));
            // WebGL の描画結果を HTML に正しく合成する方法 より
            // http://webos-goodies.jp/archives/overlaying_webgl_on_html.html
            gl.clearColor(0, 0, 0, 0);
            gl.clear(gl.COLOR_BUFFER_BIT | gl.STENCIL_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
            gl.enable(gl.BLEND);
            gl.blendEquation(gl.FUNC_ADD);
            gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
            gl.blendFuncSeparate(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA, gl.ONE, gl.ZERO);
            // 
            gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);
            gl.viewport(0, 0, gl.canvas.width, gl.canvas.height);
            // レンダリングを実行
            gl.drawArrays(gl.TRIANGLES, 0, 6);
            gl.disable(gl.BLEND);
        } else {
            // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
            gl.bindFramebuffer(gl.FRAMEBUFFER, dst.framebuffer);
            const projectionMatrix = Matrix3.projection(dst.width, dst.height);
            gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);
            gl.viewport(0, 0, dst.width, dst.height);
            // オフスクリーンレンダリングを実行
            gl.drawArrays(gl.TRIANGLES, 0, 6);
        }


        // 適用完了

    }

    drawLines(dst: Surface, { program = null, vertexes = null, size = null, color = null, antialiasSize = null }: { program: Program, vertexes: Float32Array, size: number, color: [number, number, number, number], antialiasSize: number }): void {
        const gl = this.gl;

        const tmp: Surface = this.createBlankOffscreenTarget(dst.width, dst.height);

        // arrtibute変数の位置を取得
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");

        // uniform変数の位置を取得
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");
        const startLocation = gl.getUniformLocation(program.program, "u_start");
        const endLocation = gl.getUniformLocation(program.program, "u_end");
        const sizeLocation = gl.getUniformLocation(program.program, "u_size");
        const aliasSizeLocation = gl.getUniformLocation(program.program, "u_aliasSize");
        const colorLocation = gl.getUniformLocation(program.program, "u_color");
        const texture0Lication = gl.getUniformLocation(program.program, 'u_front');

        // シェーダを設定
        gl.useProgram(program.program);

        // 色を設定
        gl.uniform4fv(colorLocation, color.map(x => x / 255));

        // シェーダの頂点座標Attributeを有効化
        gl.enableVertexAttribArray(positionLocation);


        // 頂点座標Attributeの位置情報を設定
        // 頂点情報は後で設定
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
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

        // テクスチャ座標Attributeの位置情報を設定
        // テクスチャ座標は後で設定
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
        gl.vertexAttribPointer(
            texcoordLocation,
            2,          // 2 components per iteration
            gl.FLOAT,   // the data is 32bit floats
            false,      // don't normalize the data
            0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
            0           // start at the beginning of the buffer
        );

        const projectionMatrix = Matrix3.projection(tmp.width, tmp.height);
        gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);


        // 入力元とするレンダリングターゲット=dst
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, dst.texture);
        gl.uniform1i(texture0Lication, 0);

        // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
        gl.bindFramebuffer(gl.FRAMEBUFFER, tmp.framebuffer);
        gl.viewport(0, 0, tmp.width, tmp.height);

        const len = ~~(vertexes.length / 2) - 1;

        // サイズを設定
        gl.uniform1f(sizeLocation, size / 2);
        gl.uniform1f(aliasSizeLocation, antialiasSize / 2);

        // 矩形設定
        gl.clear(gl.COLOR_BUFFER_BIT);
        for (let i = 0; i < len; i++) {

            const x1 = vertexes[i * 2 + 0] + 0.5;
            const y1 = vertexes[i * 2 + 1] + 0.5;
            const x2 = vertexes[i * 2 + 2] + 0.5;
            const y2 = vertexes[i * 2 + 3] + 0.5;

            const left = (x1 < x2 ? x1 : x2) - size;
            const top = (y1 < y2 ? y1 : y2) - size;
            const right = (x1 > x2 ? x1 : x2)  + size;
            const bottom = (y1 > y2 ? y1 : y2) + size;

            gl.uniform2f(startLocation, x1, y1);
            gl.uniform2f(endLocation, x2, y2);

            // 頂点バッファ（テクスチャ座標）を設定
            this.setVertexPosition(ImageProcessing.createRectangle(left, top, right, bottom));

            // 頂点バッファ（テクスチャ座標）を設定
            this.setTexturePosition(ImageProcessing.createRectangle(left / dst.width, top / dst.height, right / dst.width, bottom / dst.height));

            // シェーダを適用したオフスクリーンレンダリングを実行
            gl.drawArrays(gl.TRIANGLES, 0, 6);

            // レンダリング範囲をフィードバック
            const clipedLeft = (left >= tmp.width) ? tmp.width - 1 : (left < 0) ? 0 : ~~left;
            const clipedtop = (top >= tmp.height) ? tmp.height - 1 : (top < 0) ? 0 : ~~top;
            const clipedRight = (right >= tmp.width) ? tmp.width - 1 : (right < 0) ? 0 : ~~right;
            const clipedBottom = (bottom >= tmp.height) ? tmp.height - 1 : (bottom < 0) ? 0 : ~~bottom;
            gl.copyTexSubImage2D(gl.TEXTURE_2D, 0, clipedLeft, clipedtop, clipedLeft, clipedtop, clipedRight - clipedLeft, clipedBottom - clipedtop);
        }

        tmp.dispose();

    }

    fillRect(dst: Surface, { program = null, start = null, end = null, color = null }: { program: Program, start: IPoint, end: IPoint, color: [number, number, number, number] }): void {
        const gl = this.gl;

        const tmp: Surface = this.createBlankOffscreenTarget(dst.width, dst.height);

        // arrtibute変数の位置を取得
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");

        // uniform変数の位置を取得
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");
        const startLocation = gl.getUniformLocation(program.program, "u_start");
        const endLocation = gl.getUniformLocation(program.program, "u_end");
        const sizeLocation = gl.getUniformLocation(program.program, "u_size");
        const aliasSizeLocation = gl.getUniformLocation(program.program, "u_aliasSize");
        const colorLocation = gl.getUniformLocation(program.program, "u_color");
        const texture0Lication = gl.getUniformLocation(program.program, 'u_front');

        // シェーダを設定
        gl.useProgram(program.program);

        // 色を設定
        gl.uniform4fv(colorLocation, color.map(x => x / 255));

        // シェーダの頂点座標Attributeを有効化
        gl.enableVertexAttribArray(positionLocation);


        // 頂点座標Attributeの位置情報を設定
        // 頂点情報は後で設定
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
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

        // テクスチャ座標Attributeの位置情報を設定
        // テクスチャ座標は後で設定
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
        gl.vertexAttribPointer(
            texcoordLocation,
            2,          // 2 components per iteration
            gl.FLOAT,   // the data is 32bit floats
            false,      // don't normalize the data
            0,          // 0 = move forward size * sizeof(type) each iteration to get the next position
            0           // start at the beginning of the buffer
        );

        const projectionMatrix = Matrix3.projection(tmp.width, tmp.height);
        gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);


        // 入力元とするレンダリングターゲット=dst
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, dst.texture);
        gl.uniform1i(texture0Lication, 0);

        // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
        gl.bindFramebuffer(gl.FRAMEBUFFER, tmp.framebuffer);
        gl.viewport(0, 0, tmp.width, tmp.height);


        // 矩形設定
        gl.clear(gl.COLOR_BUFFER_BIT);

        const x1 = start.x + 0.5;
        const y1 = start.y + 0.5;
        const x2 = end.x + 0.5;
        const y2 = end.y + 0.5;

        const left = Math.min(x1, x2);
        const top = Math.min(y1, y2);
        const right = Math.max(x1, x2);
        const bottom = Math.max(y1, y2);

        gl.uniform2f(startLocation, x1, y1);
        gl.uniform2f(endLocation, x2, y2);

        // 頂点バッファ（テクスチャ座標）を設定
        this.setVertexPosition(ImageProcessing.createRectangle(left, top, right, bottom));

        // 頂点バッファ（テクスチャ座標）を設定
        this.setTexturePosition(ImageProcessing.createRectangle(left / dst.width, top / dst.height, right / dst.width, bottom / dst.height));

        // シェーダを適用したオフスクリーンレンダリングを実行
        gl.drawArrays(gl.TRIANGLES, 0, 6);

        const cleft = (left < 0) ? 0 : (left > dst.width - 1) ? dst.width - 1 : left;
        const ctop = (top < 0) ? 0 : (top > dst.height - 1) ? dst.height - 1 : top;
        const cright = (right < 0) ? 0 : (right > dst.width - 1) ? dst.width - 1 : right;
        const cbottom = (bottom < 0) ? 0 : (bottom > dst.height - 1) ? dst.height - 1 : bottom;
        gl.copyTexSubImage2D(gl.TEXTURE_2D, 0, cleft, ctop, cleft, ctop, cright - cleft, cbottom - ctop);

        tmp.dispose();

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
        this.setVertexPosition(ImageProcessing.createRectangle(0, 0, dst.width, dst.height));

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

        //const st = gl.checkFramebufferStatus(gl.FRAMEBUFFER);
        //if (st !== gl.FRAMEBUFFER_COMPLETE) {
        //    console.log(st)
        //}

        // オフスクリーンレンダリングを実行
        gl.drawArrays(gl.TRIANGLES, 0, 6);

        // 適用完了

    }

    compositUpdate(dst: Surface, src: Surface, program: Program) {
        const gl = this.gl;
        // arrtibute変数の位置を取得
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");

        // uniform変数の位置を取得
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");
        const texture0Lication = gl.getUniformLocation(program.program, 'u_front');
        const texture1Lication = gl.getUniformLocation(program.program, 'u_back');

        // 出力先を生成
        const tmp: Surface = this.createBlankOffscreenTarget(dst.width, dst.height);
        tmp.clear();

        // シェーダを設定
        gl.useProgram(program.program);

        // シェーダの頂点座標Attributeを有効化
        gl.enableVertexAttribArray(positionLocation);

        // 頂点バッファ（座標）を設定
        this.setVertexPosition(ImageProcessing.createRectangle(0, 0, dst.width, dst.height));

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
        gl.bindTexture(gl.TEXTURE_2D, src.texture);
        gl.uniform1i(texture0Lication, 0);

        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_2D, dst.texture);
        gl.uniform1i(texture1Lication, 1);

        // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
        gl.bindFramebuffer(gl.FRAMEBUFFER, tmp.framebuffer);
        gl.viewport(0, 0, tmp.width, tmp.height);

        //const st = gl.checkFramebufferStatus(gl.FRAMEBUFFER);
        //if (st !== gl.FRAMEBUFFER_COMPLETE) {
        //    console.log(st)
        //}

        // オフスクリーンレンダリングを実行
        gl.drawArrays(gl.TRIANGLES, 0, 6);

        // 適用完了

        // レンダリング範囲をフィードバック
        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_2D, null);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, dst.texture);
        gl.copyTexSubImage2D(gl.TEXTURE_2D, 0, 0, 0, 0, 0, dst.width, dst.height);
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);

        // 出力先を破棄
        tmp.dispose();
    }

    draw(dst: Surface, src: Surface, {program = null, start = null, end, matrix = null}: { program: Program, start: IPoint, end: IPoint, matrix: Matrix3 }) {
        const gl = this.gl;
        // arrtibute変数の位置を取得
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");

        // uniform変数の位置を取得
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");
        const textureLication = gl.getUniformLocation(program.program, 'u_front');

        // シェーダを設定
        gl.useProgram(program.program);

        // シェーダの頂点座標Attributeを有効化
        gl.enableVertexAttribArray(positionLocation);

        // 頂点バッファ（座標）を設定
        this.setVertexPosition(ImageProcessing.createRectangle(start.x, start.y, end.x, end.y));

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
            0 * 4,          // 0 = move forward size * sizeof(type) each iteration to get the next position
            0 * 4           // start at the beginning of the buffer
        );

        const projMat = Matrix3.projection(dst.width, dst.height);
        const projectionMatrix = (matrix == null) ? projMat : Matrix3.multiply(projMat, matrix);
        gl.uniformMatrix3fv(matrixLocation, false, projectionMatrix);

        //// テクスチャサイズ情報ををシェーダのUniform変数に設定
        //gl.uniform2f(textureSizeLocation, this.width, this.height);

        // 入力元とするレンダリングターゲットのテクスチャを選択
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, src.texture);
        gl.uniform1i(textureLication, 0);

        // 出力先とするレンダリングターゲットのフレームバッファを選択し、レンダリング解像度とビューポートを設定
        gl.bindFramebuffer(gl.FRAMEBUFFER, dst.framebuffer);
        gl.viewport(0, 0, dst.width, dst.height);

        //const st = gl.checkFramebufferStatus(gl.FRAMEBUFFER);
        //if (st !== gl.FRAMEBUFFER_COMPLETE) {
        //    console.log(st)
        //}

        // オフスクリーンレンダリングを実行
        gl.drawArrays(gl.TRIANGLES, 0, 6);

    }
}


///////////////////////////////////////////////////////////////
const sizeOfBitmapFileHeader: number = 14;
const sizeOfBitmapInfoHeader: number = 40;
function saveAsBmp(imageData: Surface): ArrayBuffer {
    const pixelData = new PixelBuffer(imageData.gl, imageData.width, imageData.height);
    const pixels = pixelData.capture(imageData);
    const sizeOfImageData: number = imageData.width * imageData.height * 4;

    pixelData.dispose();

    const bitmapData: ArrayBuffer = new ArrayBuffer(sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader + sizeOfImageData);

    //
    // BITMAPFILEHEADER
    //
    const viewOfBitmapFileHeader: DataView = new DataView(bitmapData, 0, sizeOfBitmapFileHeader);
    viewOfBitmapFileHeader.setUint16(0, 0x4D42, true);                                                              // bfType : 'BM'
    viewOfBitmapFileHeader.setUint32(2, sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader + sizeOfImageData, true);   // bfSize :  sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER) + sizeof(IMAGEDATA)
    viewOfBitmapFileHeader.setUint16(6, 0x0000, true);                                                              // bfReserved1 : 0
    viewOfBitmapFileHeader.setUint16(8, 0x0000, true);                                                              // bfReserved2 : 0
    viewOfBitmapFileHeader.setUint32(10, sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader, true);                    // bfOffBits : sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER)

    //
    // BITMAPINFOHEADER
    //
    const viewOfBitmapInfoHeader: DataView = new DataView(bitmapData, sizeOfBitmapFileHeader, sizeOfBitmapInfoHeader);
    viewOfBitmapInfoHeader.setUint32(0, sizeOfBitmapInfoHeader, true);  // biSize : sizeof(BITMAPINFOHEADER)
    viewOfBitmapInfoHeader.setUint32(4, imageData.width, true);         // biWidth : data.width
    viewOfBitmapInfoHeader.setUint32(8, imageData.height, true);        // biHeight : data.height
    viewOfBitmapInfoHeader.setUint16(12, 1, true);                      // biPlanes : 1
    viewOfBitmapInfoHeader.setUint16(14, 32, true);                     // biBitCount : 32
    viewOfBitmapInfoHeader.setUint32(16, 0, true);                      // biCompression : 0
    viewOfBitmapInfoHeader.setUint32(20, sizeOfImageData, true);        // biSizeImage : sizeof(IMAGEDATA)
    viewOfBitmapInfoHeader.setUint32(24, 0, true);                      // biXPixPerMeter : 0
    viewOfBitmapInfoHeader.setUint32(28, 0, true);                      // biYPixPerMeter : 0
    viewOfBitmapInfoHeader.setUint32(32, 0, true);                      // biClrUsed : 0
    viewOfBitmapInfoHeader.setUint32(36, 0, true);                      // biCirImportant : 0

    //
    // IMAGEDATA
    //
    const viewOfBitmapPixelData: DataView = new DataView(bitmapData, sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader, imageData.width * imageData.height * 4);
    for (let y = 0; y < imageData.height; y++) {
        let scan = (imageData.height - 1 - y) * imageData.width * 4;
        let base = y * imageData.width * 4;
        for (let x = 0; x < imageData.width; x++) {
            viewOfBitmapPixelData.setUint8(base + 0, pixels[scan + 2]); // B
            viewOfBitmapPixelData.setUint8(base + 1, pixels[scan + 1]); // G
            viewOfBitmapPixelData.setUint8(base + 2, pixels[scan + 0]); // R
            viewOfBitmapPixelData.setUint8(base + 3, pixels[scan + 3]); // A
            base += 4;
            scan += 4;
        }
    }
    return bitmapData;
};
function loadFromBmp(ip: ImageProcessing, bitmapData: ArrayBuffer, { reqWidth = -1, reqHeight = -1 }: { reqWidth?: number; reqHeight?: number }): Surface {

    const dataLength = bitmapData.byteLength;

    //
    // BITMAPFILEHEADER
    //
    const reqSize = (reqWidth >= 0 && reqHeight >= 0) ? reqWidth * reqHeight * 4 : -1;
    const viewOfBitmapFileHeader: DataView = new DataView(bitmapData, 0, sizeOfBitmapFileHeader);
    const bfType = viewOfBitmapFileHeader.getUint16(0, true);       // bfType : 'BM'
    const bfSize = viewOfBitmapFileHeader.getUint32(2, true);       // bfSize :  sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER) + sizeof(IMAGEDATA)
    const bfReserved1 = viewOfBitmapFileHeader.getUint16(6, true);  // bfReserved1 : 0
    const bfReserved2 = viewOfBitmapFileHeader.getUint16(8, true);  // bfReserved2 : 0
    const bfOffBits = viewOfBitmapFileHeader.getUint32(10, true);   // bfOffBits : sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER)
    if ((bfType != 0x4D42) ||
        (bfSize < sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader) || (bfSize != dataLength) || (reqSize != -1 && bfSize < sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader + reqSize) ||
        (bfReserved1 != 0) ||
        (bfReserved2 != 0) ||
        (bfOffBits < sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader) || (bfOffBits >= dataLength) || (reqSize != -1 && bfOffBits > dataLength - reqSize)
    ) {
        return null;
    }

    //
    // BITMAPINFOHEADER
    //
    const viewOfBitmapInfoHeader: DataView = new DataView(bitmapData, sizeOfBitmapFileHeader, sizeOfBitmapInfoHeader);
    const biSize = viewOfBitmapInfoHeader.getUint32(0, true);           // biSize : sizeof(BITMAPINFOHEADER)
    const biWidth = viewOfBitmapInfoHeader.getUint32(4, true);          // biWidth : this.width
    const biHeight = viewOfBitmapInfoHeader.getUint32(8, true);         // biHeight : this.height
    const biPlanes = viewOfBitmapInfoHeader.getUint16(12, true);        // biPlanes : 1
    const biBitCount = viewOfBitmapInfoHeader.getUint16(14, true);      // biBitCount : 32
    const biCompression = viewOfBitmapInfoHeader.getUint32(16, true);   // biCompression : 0
    const biSizeImage = viewOfBitmapInfoHeader.getUint32(20, true);     // biSizeImage : this.width * this.height * 4
    const biXPixPerMeter = viewOfBitmapInfoHeader.getUint32(24, true);  // biXPixPerMeter : 0
    const biYPixPerMeter = viewOfBitmapInfoHeader.getUint32(28, true);  // biYPixPerMeter : 0
    const biClrUsed = viewOfBitmapInfoHeader.getUint32(32, true);       // biClrUsed : 0
    const biCirImportant = viewOfBitmapInfoHeader.getUint32(36, true);  // biCirImportant : 0
    if (
        (biSize != sizeOfBitmapInfoHeader) ||
        (reqWidth >= 0 && biWidth != reqWidth) ||
        (reqHeight >= 0 && biHeight != reqHeight) ||
        (biPlanes != 1) ||
        (biBitCount != 32) ||
        (biSizeImage != biWidth * biHeight * 4) || (reqSize >= 0 && biSizeImage != reqSize) ||
        (biXPixPerMeter != 0) ||
        (biYPixPerMeter != 0) ||
        (biClrUsed != 0) ||
        (biCirImportant != 0)
    ) {
        return null;
    }

    //
    // IMAGEDATA
    //
    const pixeldata = new Uint8ClampedArray(biWidth * biHeight * 4);
    const viewOfBitmapPixelData: DataView = new DataView(bitmapData, bfOffBits, biWidth * biHeight * 4);
    for (let y = 0; y < biHeight; y++) {
        let scan = (biHeight - 1 - y) * biWidth * 4;
        let base = y * biWidth * 4;
        for (let x = 0; x < biWidth; x++) {
            pixeldata[scan + 2] = viewOfBitmapPixelData.getUint8(base + 0); // B
            pixeldata[scan + 1] = viewOfBitmapPixelData.getUint8(base + 1); // G
            pixeldata[scan + 0] = viewOfBitmapPixelData.getUint8(base + 2); // R
            pixeldata[scan + 3] = viewOfBitmapPixelData.getUint8(base + 3); // A
            base += 4;
            scan += 4;
        }
    }
    const surface = ip.createOffscreenTargetFromImage(new ImageData(pixeldata, biWidth, biHeight));
    return surface;
}
///////////////////////////////////////////////////////////////
interface IPoint {
    x: number;
    y: number;
}
///////////////////////////////////////////////////////////////
class HSVColorWheel {
    private canvas: HTMLCanvasElement;
    private context: CanvasRenderingContext2D;
    private imageData: ImageData;
    private wheelRadius: { min: number, max: number };
    private svBoxSize: number;
    private _hsv: HSV = [0, 0, 0];
    public get hsv(): HSV {
        return this._hsv.slice() as HSV;
    }
    public set hsv(v: HSV) {
        this._hsv = v.slice() as HSV;
    }
    public get rgb(): RGB {
        return hsv2rgb(this._hsv);
    }
    public set rgb(v: RGB) {
        this._hsv = rgb2hsv(v);
    }
    constructor({ width = 0, height = 0, wheelRadiusMin = 76, wheelRadiusMax = 96, svBoxSize = 100 }: { width: number, height: number, wheelRadiusMin?: number, wheelRadiusMax?: number, svBoxSize?: number }) {
        this.canvas = document.createElement("canvas");
        this.canvas.width = width;
        this.canvas.height = height;
        this.context = this.canvas.getContext("2d");
        this.imageData = new ImageData(this.canvas.width, this.canvas.height);
        this.wheelRadius = { min: wheelRadiusMin, max: wheelRadiusMax };
        this.svBoxSize = svBoxSize;
        this._hsv = [0, 0, 0];
        this.updateImage();
    }


    private getPixel(x: number, y: number): RGB {
        if (x < 0 || this.canvas.width <= x || y < 0 || this.canvas.height <= y) {
            return [0, 0, 0];
        }
        const index = (~~y * this.canvas.width + ~~x) * 4;
        return [
            this.imageData.data[index + 0],
            this.imageData.data[index + 1],
            this.imageData.data[index + 2],
        ];
    }
    private setPixel(x: number, y: number, color: RGB): void {
        if (x < 0 || this.canvas.width <= x || y < 0 || this.canvas.height <= y) {
            return;
        }
        const index = (~~y * this.canvas.width + ~~x) * 4;
        this.imageData.data[index + 0] = color[0];
        this.imageData.data[index + 1] = color[1];
        this.imageData.data[index + 2] = color[2];
        this.imageData.data[index + 3] = 255;
    }
    private xorPixel(x: number, y: number): void {
        if (x < 0 || this.canvas.width <= x || y < 0 || this.canvas.height <= y) {
            return;
        }
        const index = (~~y * this.canvas.width + ~~x) * 4;
        this.imageData.data[index + 0] = 255 ^ this.imageData.data[index + 0];
        this.imageData.data[index + 1] = 255 ^ this.imageData.data[index + 1];
        this.imageData.data[index + 2] = 255 ^ this.imageData.data[index + 2];
    }

    private drawInvBox(x: number, y: number, w: number, h: number): void {
        for (let yy = 0; yy < h; yy++) {
            this.xorPixel(x + 0, y + yy);
            this.xorPixel(x + w - 1, y + yy);
        }
        for (let xx = 1; xx < w - 1; xx++) {
            this.xorPixel(x + xx, y + 0);
            this.xorPixel(x + xx, y + h - 1);
        }
    }

    private drawHCircle(): void {
        for (let iy = 0; iy < this.canvas.height; iy++) {
            const yy = iy - this.canvas.height / 2;
            for (let ix = 0; ix < this.canvas.width; ix++) {
                const xx = ix - this.canvas.width / 2;

                const r = ~~Math.sqrt(xx * xx + yy * yy);

                if (r < this.wheelRadius.min || r >= this.wheelRadius.max) {
                    continue;
                }

                const h = (~~(-Math.atan2(yy, xx) * 180 / Math.PI) + 360) % 360;

                const col = hsv2rgb([h, 1.0, 1.0]);

                this.setPixel(ix, iy, col);
            }
        }
    }

    private drawSVBox() {
        for (let iy = 0; iy < this.svBoxSize; iy++) {
            const v = (this.svBoxSize - 1 - iy) / (this.svBoxSize - 1);

            for (let ix = 0; ix < this.svBoxSize; ix++) {
                const s = ix / (this.svBoxSize - 1);

                const col = hsv2rgb([this._hsv[0], s, v]);

                this.setPixel(ix + ~~((this.canvas.width - this.svBoxSize) / 2), iy + ~~((this.canvas.height - this.svBoxSize) / 2), col);
            }
        }
    }

    private drawHCursor() {
        const rd = -this._hsv[0] * Math.PI / 180;

        const xx = this.wheelRadius.min + (this.wheelRadius.max - this.wheelRadius.min) / 2;
        const yy = 0;

        const x = ~~(xx * Math.cos(rd) - yy * Math.sin(rd) + this.canvas.width / 2);
        const y = ~~(xx * Math.sin(rd) + yy * Math.cos(rd) + this.canvas.height / 2);

        this.drawInvBox(x - 4, y - 4, 9, 9);
    }

    private getHValueFromPos(x0: number, y0: number) {
        const x = x0 - this.canvas.width / 2;
        const y = y0 - this.canvas.height / 2;

        const h = (~~(-Math.atan2(y, x) * 180 / Math.PI) + 360) % 360;

        const r = ~~Math.sqrt(x * x + y * y);

        return (r >= this.wheelRadius.min && r < this.wheelRadius.max) ? h : undefined;
    }
    private drawSVCursor() {
        const left = (this.canvas.width - this.svBoxSize) / 2;
        const top = (this.canvas.height - this.svBoxSize) / 2;
        this.drawInvBox(left + ~~(this._hsv[1] * this.svBoxSize) - 4, top + ~~((1 - this._hsv[2]) * this.svBoxSize) - 4, 9, 9);
    }
    private getSVValueFromPos(x0: number, y0: number) {
        const x = ~~(x0 - (this.canvas.width - this.svBoxSize) / 2);
        const y = ~~(y0 - (this.canvas.height - this.svBoxSize) / 2);

        return (0 <= x && x < this.svBoxSize && 0 <= y && y < this.svBoxSize) ? [x / (this.svBoxSize - 1), (this.svBoxSize - 1 - y) / (this.svBoxSize - 1)] : undefined;
    }
    private updateImage() {
        const len = this.canvas.width * this.canvas.height * 4;
        for (let i = 0; i < len; i++) {
            this.imageData.data[i] = 0;
        }
        this.drawHCircle();
        this.drawSVBox();
        this.drawHCursor();
        this.drawSVCursor();
        this.context.putImageData(this.imageData, 0, 0);
    }
    public draw(context: CanvasRenderingContext2D, x: number, y: number): void {
        context.drawImage(this.canvas, x, y);
    }
    public touch(x: number, y: number): boolean {
        const ret1 = this.getHValueFromPos(x, y);
        if (ret1 != undefined) {
            this._hsv[0] = ret1;
        }
        const ret2 = this.getSVValueFromPos(x, y);
        if (ret2 != undefined) {
            [this._hsv[1], this._hsv[2]] = ret2;
        }

        if (ret1 != undefined || ret2 != undefined) {
            this.updateImage();
            return true;
        } else {
            return false;
        }
    }
}
///////////////////////////////////////////////////////////////
namespace Events {
    export type EventHandler = (...args: any[]) => boolean;
    class SingleEmitter {
        private listeners: EventHandler[];

        constructor() {
            this.listeners = [];
        }

        public clear(): SingleEmitter {
            this.listeners.length = 0;
            return this;
        }

        public on(listener: EventHandler): SingleEmitter {
            this.listeners.splice(0, 0, listener);
            return this;
        }

        public off(listener: EventHandler): SingleEmitter {
            const index = this.listeners.indexOf(listener);
            if (index !== -1) {
                this.listeners.splice(index, 1);
            }
            return this;
        }

        public fire(...args: any[]): boolean {
            const temp = this.listeners.slice();
            for (const dispatcher of temp) {
                if (dispatcher.apply(this, args)) {
                    return true;
                }
            };
            return false;
        }

        public one(listener: EventHandler): SingleEmitter {
            const func = (...args: any[]) => {
                const result = listener.apply(this, args);
                this.off(func);
                return result;
            };

            this.on(func);

            return this;
        }

    }
    export class EventEmitter {

        private listeners: Map<string, SingleEmitter>;

        constructor() {
            this.listeners = new Map<string, SingleEmitter>();
        }

        public on(eventName: string, listener: EventHandler): EventEmitter {
            if (!this.listeners.has(eventName)) {
                this.listeners.set(eventName, new SingleEmitter());
            }

            this.listeners.get(eventName).on(listener);
            return this;
        }

        public off(eventName: string, listener: EventHandler): EventEmitter {
            this.listeners.get(eventName).off(listener);
            return this;
        }

        public fire(eventName: string, ...args: any[]): boolean {
            if (this.listeners.has(eventName)) {
                const dispatcher = this.listeners.get(eventName);
                return dispatcher.fire.apply(dispatcher, args);
            }
            return false;
        }

        public one(eventName: string, listener: EventHandler): EventEmitter {
            if (!this.listeners.has(eventName)) {
                this.listeners.set(eventName, new SingleEmitter());
            }
            this.listeners.get(eventName).one(listener);

            return this;
        }

        public hasEventListener(eventName: string): boolean {
            return this.listeners.has(eventName);
        }

        public clearEventListener(eventName: string): EventEmitter {
            if (this.listeners.has(eventName)) {
                this.listeners.get(eventName).clear();
            }
            return this;
        }
    }
}
///////////////////////////////////////////////////////////////
namespace GUI {
    /**
     * コントロールコンポーネントインタフェース
     */

    export class UIEvent {
        constructor(name: string) {
            this.eventName = name;
            this.propagationStop = false;
            this.defaultPrevented = false;
        }
        private eventName: string;
        public get name(): string {
            return this.eventName;
        }
        preventDefault(): void {
            this.defaultPrevented = true;
        }
        defaultPrevented: boolean;
        public stopPropagation(): void {
            this.propagationStop = true;
        }

        private propagationStop: boolean;
        public get propagationStopped(): boolean {
            return this.propagationStop;
        }
    }
    export class UIMouseEvent extends UIEvent {
        x: number;
        y: number;
        constructor(name: string, x: number, y: number) {
            super(name);
            this.x = x;
            this.y = y;
        }
    }
    export class UISwipeEvent extends UIEvent {
        x: number;
        y: number;
        dx: number;
        dy: number;
        constructor(name: string, dx: number, dy: number, x: number, y: number) {
            super(name);
            this.x = x;
            this.y = y;
            this.dx = dx;
            this.dy = dy;
        }
    }

    export function installClickDelecate(ui: Control) {
        const hookHandler = (ev: UIMouseEvent) => {
            const x = ev.x;
            const y = ev.y;

            ev.preventDefault();
            ev.stopPropagation();

            let dx: number = 0;
            let dy: number = 0;
            const onPointerMoveHandler = (ev: UIMouseEvent) => {
                dx += Math.abs(ev.x - x);
                dy += Math.abs(ev.y - y);
                ev.preventDefault();
                ev.stopPropagation();
            };
            const onPointerUpHandler = (ev: UIMouseEvent) => {
                ui.removeEventListener("pointermove", onPointerMoveHandler);
                ui.removeEventListener("pointerup", onPointerUpHandler);
                if (dx + dy < 5) {
                    ui.dispatchEvent(new UIMouseEvent("click", ev.x, ev.y));
                }
                ev.preventDefault();
                ev.stopPropagation();
                return;
            };
            ui.addEventListener("pointermove", onPointerMoveHandler);
            ui.addEventListener("pointerup", onPointerUpHandler);

            return;
        };
        ui.addEventListener("pointerdown", hookHandler);

    }

    // UIに対するスワイプ操作を捕捉
    export function installSwipeDelegate(ui: Control) {

        const hookHandler = (ev: UIMouseEvent) => {
            let x = ev.x;
            let y = ev.y;
            if (!ui.visible || !ui.enable) {
                return;
            }

            if (!ui.isHit(x, y)) {
                return;
            }

            ev.preventDefault();
            ev.stopPropagation();

            let root = ui.root;
            let isTap = true;
            const onPointerMoveHandler = (ev: UIMouseEvent) => {
                let dx = (~~ev.x - ~~x);
                let dy = (~~ev.y - ~~y);
                x = ev.x;
                y = ev.y;
                ui.postEvent(new UISwipeEvent("swipe", dx, dy, x, y));
                ev.preventDefault();
                ev.stopPropagation();
                return;
            };
            const onPointerUpHandler = (ev: UIMouseEvent) => {
                root.removeEventListener("pointermove", onPointerMoveHandler, true);
                root.removeEventListener("pointerup", onPointerUpHandler, true);
                ev.preventDefault();
                ev.stopPropagation();
                return;
            };
            root.addEventListener("pointermove", onPointerMoveHandler, true);
            root.addEventListener("pointerup", onPointerUpHandler, true);

            ui.postEvent(new UISwipeEvent("swipe", 0, 0, x, y));
        };
        ui.addEventListener("pointerdown", hookHandler);
    }

    // スワイプorタップ
    export function installSwipeOrTapDelegate(ui: Control) {

        const hookHandler = (ev: UIMouseEvent) => {
            let x = ev.x;
            let y = ev.y;
            if (!ui.visible || !ui.enable) {
                return;
            }

            if (!ui.isHit(x, y)) {
                return;
            }

            ev.preventDefault();
            ev.stopPropagation();

            let root = ui.root;
            let isTap = true;
            let mx = 0;
            let my = 0;
            const onPointerMoveHandler = (ev: UIMouseEvent) => {
                let dx = (~~ev.x - ~~x);
                let dy = (~~ev.y - ~~y);
                mx += Math.abs(dx);
                my += Math.abs(dy);
                if (mx + my > 5) {
                    isTap = false;
                }
                if (isTap == false) {
                    x = ev.x;
                    y = ev.y;
                    ui.postEvent(new UISwipeEvent("swipe", dx, dy, x, y));
                }
                ev.preventDefault();
                ev.stopPropagation();
                return;
            };
            const onPointerUpHandler = (ev: UIMouseEvent) => {
                root.removeEventListener("pointermove", onPointerMoveHandler, true);
                root.removeEventListener("pointerup", onPointerUpHandler, true);
                if (isTap) {
                    ui.dispatchEvent(new UIMouseEvent("click", ev.x, ev.y));
                }
                ev.preventDefault();
                ev.stopPropagation();
                return;
            };
            root.addEventListener("pointermove", onPointerMoveHandler, true);
            root.addEventListener("pointerup", onPointerUpHandler, true);
        };
        ui.addEventListener("pointerdown", hookHandler);
    }
    // スワイプorタップorロングタップ
    export function installSwipeOrTapOrLongTapDelegate(ui: Control) {

        const hookHandler = (ev: UIMouseEvent) => {
            let x = ev.x;
            let y = ev.y;
            if (!ui.visible || !ui.enable) {
                return;
            }

            if (!ui.isHit(x, y)) {
                return;
            }

            ev.preventDefault();
            ev.stopPropagation();


            let root = ui.root;
            let isTap = true;
            let isLongTap = false;
            let mx = 0;
            let my = 0;
            let longTapDetectTimerHandle = 0;
            const onPointerMoveHandler = (ev: UIMouseEvent) => {
                let dx = (~~ev.x - ~~x);
                let dy = (~~ev.y - ~~y);
                mx += Math.abs(dx);
                my += Math.abs(dy);
                if (mx + my > 5) {
                    if (isTap) {
                        isTap = false;
                        window.clearTimeout(longTapDetectTimerHandle); longTapDetectTimerHandle = 0;
                        ui.postEvent(new UISwipeEvent("swipestart", 0, 0, x, y));
                    }
                }
                if (isTap == false) {
                    x = ev.x;
                    y = ev.y;
                    ui.postEvent(new UISwipeEvent(isLongTap ? "longswipe" : "swipe", dx, dy, x, y));
                }
                ev.preventDefault();
                ev.stopPropagation();
                return;
            };
            const onPointerUpHandler = (ev: UIMouseEvent) => {
                window.clearTimeout(longTapDetectTimerHandle); longTapDetectTimerHandle = 0;
                root.removeEventListener("pointermove", onPointerMoveHandler, true);
                root.removeEventListener("pointerup", onPointerUpHandler, true);
                if (isTap) {
                    ui.dispatchEvent(new UIMouseEvent("click", ev.x, ev.y));
                } else {
                    ui.postEvent(new UISwipeEvent(isLongTap ? "longswipeend" : "swipeend", 0, 0, x, y));
                }
                ev.preventDefault();
                ev.stopPropagation();
                return;
            };
            longTapDetectTimerHandle = window.setTimeout(() => {
                isLongTap = true;
                isTap = false;
                ui.postEvent(new UISwipeEvent("longswipestart", 0, 0, x, y));
            }, 500);

            root.addEventListener("pointermove", onPointerMoveHandler, true);
            root.addEventListener("pointerup", onPointerUpHandler, true);

        };
        ui.addEventListener("pointerdown", hookHandler);
    }

    type EventHandler = (event: UIEvent, ...args: any[]) => void;

    export class Control {
        public get globalPos(): IPoint {
            let x = this.left;
            let y = this.top;
            let p = this.parent;
            while (p) {
                x += p.left;
                y += p.top;
                p = p.parent
            }
            return { x: x, y: y };
        }
        public left: number;
        public top: number;
        public width: number;
        public height: number;

        public visible: boolean;
        public enable: boolean;

        public parent: Control;
        public childrens: Control[];

        public get root(): Control {
            let root = this.parent;
            while (root.parent) { root = root.parent; }
            return root;
        }


        private captureListeners: Map<string, EventHandler[]>;
        private bubbleListeners: Map<string, EventHandler[]>;

        public draw(context: CanvasRenderingContext2D): void {
            context.translate(+this.left, +this.top);
            const len = this.childrens.length;
            for (let i = len - 1; i >= 0; i--) {
                this.childrens[i].draw(context);
            }
            context.translate(-this.left, -this.top);
        }

        constructor({
            left = 0,
            top = 0,
            width = 0,
            height = 0,
            visible = true,
            enable = true
        }: {
                left?: number;
                top?: number;
                width?: number;
                height?: number;
                visible?: boolean;
                enable?: boolean;
            }) {
            this.left = left;
            this.top = top;
            this.width = width;
            this.height = height;
            this.visible = visible;
            this.enable = enable;
            this.parent = null;
            this.childrens = [];

            this.captureListeners = new Map<string, EventHandler[]>();
            this.bubbleListeners = new Map<string, EventHandler[]>();
        }

        public addEventListener(event: string, handler: EventHandler, capture: boolean = false): void {
            const target = (capture) ? this.captureListeners : this.bubbleListeners;
            if (!target.has(event)) {
                target.set(event, []);
            }
            target.get(event).push(handler);
        }

        public removeEventListener(event: string, handler: EventHandler, capture: boolean = false): void {
            const target = (capture) ? this.captureListeners : this.bubbleListeners;
            if (!target.has(event)) {
                return;
            }
            const listeners = target.get(event);
            const index = listeners.indexOf(handler);
            if (index != -1) {
                listeners.splice(index, 1);
            }
        }

        public dodraw(context: CanvasRenderingContext2D) {
            this.draw(context);
        }

        private enumEventTargets(ret: Control[]): void {
            if (!this.visible || !this.enable) {
                return;
            }
            ret.push(this);
            for (let child of this.childrens) {
                child.enumEventTargets(ret);
            }
            return;
        }

        private enumMouseEventTargets(ret: Control[], x: number, y: number): boolean {
            if (!this.visible || !this.enable) {
                return false;
            }
            ret.push(this);
            for (let child of this.childrens) {
                if (child.isHit(x, y)) {
                    if (child.enumMouseEventTargets(ret, x, y) == true) {
                        return true;
                    }
                }
            }
            return true;
        }

        public postEvent(event: UIEvent, ...args: any[]): void {
            if (this.captureListeners.has(event.name)) {
                const captureListeners = this.captureListeners.get(event.name);
                for (const listener of captureListeners) {
                    listener(event, ...args);
                    if (event.propagationStopped) {
                        return;
                    }
                }
            }
            if (this.bubbleListeners.has(event.name)) {
                const bubbleListeners = this.bubbleListeners.get(event.name);
                for (const listener of bubbleListeners) {
                    listener(event, ...args);
                    if (event.propagationStopped) { return; }
                }
            }

        }

        public dispatchEvent(event: UIEvent, ...args: any[]): void {
            const chain: Control[] = [];
            (event instanceof UIMouseEvent) ? this.enumMouseEventTargets(chain, (event as UIMouseEvent).x, (event as UIMouseEvent).y) : this.enumEventTargets(chain);
            for (let child of chain) {
                if (child.captureListeners.has(event.name)) {
                    const captureListeners = child.captureListeners.get(event.name);
                    for (const listener of captureListeners) {
                        listener(event, ...args);
                        if (event.propagationStopped) {
                            return;
                        }
                    }
                }
            }
            chain.reverse();
            for (let child of chain) {
                if (child.bubbleListeners.has(event.name)) {
                    const bubbleListeners = child.bubbleListeners.get(event.name);
                    for (const listener of bubbleListeners) {
                        listener(event, ...args);
                        if (event.propagationStopped) { return; }
                    }
                }
            }

        }

        addChild(child: Control): void {
            child.parent = this;
            this.childrens.push(child);
        }

        removeChild(child: Control): boolean {
            const index = this.childrens.indexOf(child);
            if (index != -1) {
                this.childrens.splice(index, 1);
                child.parent = null;
                return true;
            }
            return false;
        }


        /**
         * UI領域内に点(x,y)があるか判定
         * @param ui {UI}
         * @param x {number}
         * @param y {number}
         */
        isHit(x: number, y: number): boolean {
            const { x: dx, y: dy } = this.globalPos;
            return (dx <= x && x < dx + this.width) && (dy <= y && y < dy + this.height);
        }


    }
    export class TextBox extends Control {
        public text: string;
        public edgeColor: string;
        public color: string;
        public font: string;
        public fontColor: string;
        public textAlign: string;
        public textBaseline: string;
        constructor(
            {
                left = 0,
                top = 0,
                width = 0,
                height = 0,
                text = "",
                edgeColor = `rgb(128,128,128)`,
                color = `rgb(255,255,255)`,
                font = undefined,
                fontColor = `rgb(0,0,0)`,
                textAlign = "left",
                textBaseline = "top",
                visible = true,
                enable = true
            }: {
                    left: number;
                    top: number;
                    width: number;
                    height: number;
                    text?: string;
                    edgeColor?: string;
                    color?: string;
                    font?: string;
                    fontColor?: string;
                    textAlign?: string;
                    textBaseline?: string;
                    visible?: boolean;
                    enable?: boolean;
                }) {
            super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
            this.text = text;
            this.edgeColor = edgeColor;
            this.color = color;
            this.font = font;
            this.fontColor = fontColor;
            this.textAlign = textAlign;
            this.textBaseline = textBaseline;
        }
        draw(context: CanvasRenderingContext2D) {
            const a = this.left + 8;
            const b = this.left + this.width - 8;
            const c = this.left;
            const d = this.left + this.width;
            const e = this.top;
            const f = this.top + this.height;

            context.beginPath();
            context.moveTo(a, e);
            context.bezierCurveTo(c, e, c, f, a, f);
            context.lineTo(b, f);
            context.bezierCurveTo(d, f, d, e, b, e);
            context.lineTo(a, e);
            context.closePath();
            context.fillStyle = this.color;
            context.fill();
            context.beginPath();
            context.moveTo(a + 0.5, e + 0.5);
            context.bezierCurveTo(c + 0.5, e + 0.5, c + 0.5, f - 0.5, a + 0.5, f - 0.5);
            context.lineTo(b - 0.5, f - 0.5);
            context.bezierCurveTo(d - 0.5, f - 0.5, d - 0.5, e + 0.5, b - 0.5, e + 0.5);
            context.lineTo(a + 0.5, e + 0.5);
            context.closePath();
            context.strokeStyle = this.edgeColor;
            context.lineWidth = 1;
            context.stroke();
            context.font = this.font;
            context.fillStyle = this.fontColor;
            const metrics = context.measureText(this.text);
            context.textAlign = this.textAlign;
            context.textBaseline = this.textBaseline;
            context.fillTextBox(this.text, a, e + 2, this.width, this.height - 4);
        }
    }
    export class Window extends Control {
        public static defaultValue: {
            edgeColor: string;
            color: string;
            font: string;
            fontColor: string;
            textAlign: string;
            textBaseline: string;
            visible: boolean;
            enable: boolean;
        } = {
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px monospace",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
            visible: true,
            enable: true,
        };

        public text: string | (() => string);
        public edgeColor: string;
        public color: string;
        public font: string;
        public fontColor: string;
        public textAlign: string;
        public textBaseline: string;

        constructor(
            {
                left = 0,
                top = 0,
                width = 0,
                height = 0,
                text = "",
                edgeColor = Button.defaultValue.edgeColor,
                color = Button.defaultValue.color,
                font = Button.defaultValue.font,
                fontColor = Button.defaultValue.fontColor,
                textAlign = Button.defaultValue.textAlign,
                textBaseline = Button.defaultValue.textBaseline,
                visible = Button.defaultValue.visible,
                enable = Button.defaultValue.enable,
            }: {
                    left: number;
                    top: number;
                    width: number;
                    height: number;
                    text?: string | (() => string);
                    edgeColor?: string;
                    color?: string;
                    font?: string;
                    fontColor?: string;
                    textAlign?: string;
                    textBaseline?: string;
                    visible?: boolean;
                    enable?: boolean;
                }) {
            super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
            this.text = text;
            this.edgeColor = edgeColor;
            this.color = color;
            this.font = font;
            this.fontColor = fontColor;
            this.textAlign = textAlign;
            this.textBaseline = textBaseline;

            this.addEventListener("swipe",
                (event: UISwipeEvent) => {
                    this.left += event.dx;
                    this.top += event.dy;
                });
            installSwipeDelegate(this);

            this.addEventListener("pointerdown", (ev: UIMouseEvent) => {
                const index = this.parent.childrens.indexOf(this);
                if (index != 0) {
                    this.parent.childrens.splice(index, 1);
                    this.parent.childrens.unshift(this);
                }
            }, true)

        }
        draw(context: CanvasRenderingContext2D) {
            context.fillStyle = this.color;
            context.fillRect(this.left, this.top, this.width, this.height);
            context.strokeStyle = this.edgeColor;
            context.lineWidth = 1;
            context.strokeRect(this.left, this.top, this.width, this.height);
            context.font = this.font;
            context.fillStyle = this.fontColor;
            const text = (this.text instanceof Function) ? (this.text as (() => string)).call(this) : this.text
            context.textAlign = this.textAlign;
            context.textBaseline = this.textBaseline;
            context.fillTextBox(text, this.left + 1, this.top + 1, this.width - 2, this.height - 2);
            super.draw(context);
        }
    }

    export class Button extends Control {
        public static defaultValue: {
            edgeColor: string;
            color: string;
            font: string;
            fontColor: string;
            textAlign: string;
            textBaseline: string;
            visible: boolean;
            enable: boolean;
            disableEdgeColor: string;
            disableColor: string;
            disableFontColor: string;
        } = {
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px monospace",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
            visible: true,
            enable: true,
            disableEdgeColor: `rgb(34,34,34)`,
            disableColor: `rgb(133,133,133)`,
            disableFontColor: `rgb(192,192,192)`,
        };

        public text: string | (() => string);
        public edgeColor: string;
        public color: string;
        public font: string;
        public fontColor: string;
        public textAlign: string;
        public textBaseline: string;
        public disableEdgeColor: string;
        public disableColor: string;
        public disableFontColor: string;

        constructor(
            {
                left = 0,
                top = 0,
                width = 0,
                height = 0,
                text = "",
                edgeColor = Button.defaultValue.edgeColor,
                color = Button.defaultValue.color,
                font = Button.defaultValue.font,
                fontColor = Button.defaultValue.fontColor,
                textAlign = Button.defaultValue.textAlign,
                textBaseline = Button.defaultValue.textBaseline,
                visible = Button.defaultValue.visible,
                enable = Button.defaultValue.enable,
                disableEdgeColor = Button.defaultValue.disableEdgeColor,
                disableColor = Button.defaultValue.disableColor,
                disableFontColor = Button.defaultValue.disableFontColor,
            }: {
                    left: number;
                    top: number;
                    width: number;
                    height: number;
                    text?: string | (() => string);
                    edgeColor?: string;
                    color?: string;
                    font?: string;
                    fontColor?: string;
                    textAlign?: string;
                    textBaseline?: string;
                    visible?: boolean;
                    enable?: boolean;
                    disableEdgeColor?: string;
                    disableColor?: string;
                    disableFontColor?: string;
                }) {
            super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
            this.text = text;
            this.edgeColor = edgeColor;
            this.color = color;
            this.font = font;
            this.fontColor = fontColor;
            this.textAlign = textAlign;
            this.textBaseline = textBaseline;
            this.disableEdgeColor = disableEdgeColor;
            this.disableColor = disableColor;
            this.disableFontColor = disableFontColor;
            installClickDelecate(this);
        }

        draw(context: CanvasRenderingContext2D) {
            context.fillStyle = this.enable ? this.color : this.disableColor
            context.fillRect(this.left, this.top, this.width, this.height);
            context.strokeStyle = this.enable ? this.edgeColor : this.disableEdgeColor;
            context.lineWidth = 1;
            context.strokeRect(this.left, this.top, this.width, this.height);
            context.font = this.font;
            context.fillStyle = this.enable ? this.fontColor : this.disableFontColor;
            const text = (this.text instanceof Function) ? (this.text as (() => string)).call(this) : this.text
            context.textAlign = this.textAlign;
            context.textBaseline = this.textBaseline;
            context.fillTextBox(text, this.left + 1, this.top + 1, this.width - 2, this.height - 2);
            super.draw(context);
        }
    }
    export class Label extends Control {
        public static defaultValue: {
            edgeColor: string;
            color: string;
            font: string;
            fontColor: string;
            textAlign: string;
            textBaseline: string;
            visible: boolean;
            enable: boolean;
            draggable: boolean;
            disableEdgeColor: string;
            disableColor: string;
            disableFontColor: string;
        } = {
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px monospace",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
            visible: true,
            enable: true,
            draggable: false,
            disableEdgeColor: `rgb(34,34,34)`,
            disableColor: `rgb(133,133,133)`,
            disableFontColor: `rgb(192,192,192)`,
        };

        public text: string | (() => string);
        public edgeColor: string;
        public color: string;
        public font: string;
        public fontColor: string;
        public textAlign: string;
        public textBaseline: string;
        public draggable: boolean; public disableEdgeColor: string;
        public disableColor: string;
        public disableFontColor: string;

        constructor(
            {
                left = 0,
                top = 0,
                width = 0,
                height = 0,
                text = "",
                edgeColor = Label.defaultValue.edgeColor,
                color = Label.defaultValue.color,
                font = Label.defaultValue.font,
                fontColor = Label.defaultValue.fontColor,
                textAlign = Label.defaultValue.textAlign,
                textBaseline = Label.defaultValue.textBaseline,
                visible = Label.defaultValue.visible,
                enable = Label.defaultValue.enable,
                disableEdgeColor = Label.defaultValue.disableEdgeColor,
                disableColor = Label.defaultValue.disableColor,
                disableFontColor = Label.defaultValue.disableFontColor,
            }: {
                    left: number;
                    top: number;
                    width: number;
                    height: number;
                    text?: string | (() => string);
                    edgeColor?: string;
                    color?: string;
                    font?: string;
                    fontColor?: string;
                    textAlign?: string;
                    textBaseline?: string;
                    visible?: boolean;
                    enable?: boolean;
                    disableEdgeColor?: string;
                    disableColor?: string;
                    disableFontColor?: string;
                }) {
            super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
            this.text = text;
            this.edgeColor = edgeColor;
            this.color = color;
            this.font = font;
            this.fontColor = fontColor;
            this.textAlign = textAlign;
            this.textBaseline = textBaseline;
            this.disableEdgeColor = disableEdgeColor;
            this.disableColor = disableColor;
            this.disableFontColor = disableFontColor;
        }

        draw(context: CanvasRenderingContext2D) {
            context.fillStyle = this.enable ? this.color : this.disableColor
            context.fillRect(this.left, this.top, this.width, this.height);
            context.strokeStyle = this.enable ? this.edgeColor : this.disableEdgeColor;
            context.lineWidth = 1;
            context.strokeRect(this.left, this.top, this.width, this.height);
            context.font = this.font;
            context.fillStyle = this.enable ? this.fontColor : this.disableFontColor;
            const text = (this.text instanceof Function) ? (this.text as (() => string)).call(this) : this.text
            context.textAlign = this.textAlign;
            context.textBaseline = this.textBaseline;
            context.fillTextBox(text, this.left + 1, this.top + 1, this.width - 2, this.height - 2);
            super.draw(context);
        }
    }
    export class ImageButton extends Control {
        public texture: HTMLImageElement | HTMLCanvasElement | HTMLVideoElement;
        public texLeft: number;
        public texTop: number;
        public texWidth: number;
        public texHeight: number;
        public click: (x: number, y: number) => void;

        constructor(
            {
                left = 0,
                top = 0,
                width = 0,
                height = 0,
                texture = null,
                texLeft = 0,
                texTop = 0,
                texWidth = 0,
                texHeight = 0,
                visible = true,
                enable = true
            }: {
                    left: number;
                    top: number;
                    width: number;
                    height: number;
                    texture?: HTMLImageElement | HTMLCanvasElement | HTMLVideoElement;
                    texLeft?: number;
                    texTop?: number;
                    texWidth?: number;
                    texHeight?: number;
                    visible?: boolean;
                    enable?: boolean;
                }) {
            super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
            this.texture = texture;
            this.texLeft = texLeft;
            this.texTop = texTop;
            this.texWidth = texWidth;
            this.texHeight = texHeight;
            installClickDelecate(this);
        }
        draw(context: CanvasRenderingContext2D) {
            if (this.texture != null) {
                context.drawImage(
                    this.texture,
                    this.texLeft,
                    this.texTop,
                    this.texWidth,
                    this.texHeight,
                    this.left,
                    this.top,
                    this.width,
                    this.height
                );
            }
        }
    }
    export class ListBox extends Control {
        private insertCandPos: number = null;

        public lineHeight: number;
        public scrollValue: number;
        public scrollbarWidth: number;
        public space: number;
        public click: (ev: UIMouseEvent) => void;
        public dragItem: (from: number, to: number) => void;

        public drawItem: (context: CanvasRenderingContext2D, left: number, top: number, width: number, height: number, item: number) => void;
        public getItemCount: () => number;
        constructor(
            {
                left = 0,
                top = 0,
                width = 0,
                height = 0,
                lineHeight = 12,
                drawItem = () => { },
                getItemCount = () => 0,
                visible = true,
                enable = true,
                scrollbarWidth = 1,
                space = 2
            }: {
                    left: number;
                    top: number;
                    width: number;
                    height: number;
                    lineHeight?: number
                    drawItem?: (context: CanvasRenderingContext2D, left: number, top: number, width: number, height: number, item: number) => void,
                    getItemCount?: () => number,
                    visible?: boolean;
                    enable?: boolean;
                    scrollbarWidth?: number;
                    space?: number;
                }) {
            super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
            this.lineHeight = lineHeight;
            this.drawItem = drawItem;
            this.getItemCount = getItemCount;
            this.scrollValue = 0;
            this.scrollbarWidth = scrollbarWidth;
            this.space = space;
            this.addEventListener("swipe", (event: UISwipeEvent) => {
                this.scrollValue -= event.dy;
                this.update();
            });

            const mp: IPoint = {
                x: 0, y: 0
            };
            let animationTimer = 0;
            let animationPrev = 0;
            let dragItemIndex = -1;
            this.addEventListener("longswipestart", (event: UISwipeEvent) => {
                mp.x = event.x;
                mp.y = event.y;
                animationPrev = Date.now();

                {
                    const gp = this.globalPos;
                    const cursorY = ~~((mp.y - gp.y) + this.scrollValue);
                    dragItemIndex = ~~((cursorY - this.space) / (this.lineHeight + this.space));
                    if (dragItemIndex == -1) {
                        return;
                    }
                }

                const longswipeHandler = () => {
                    const delta = Date.now() - animationPrev;
                    const scrollv = ~~(delta / 10);
                    animationPrev += scrollv * 10;
                    const gp = this.globalPos;
                    if (mp.y < gp.y) {
                        this.scrollValue -= scrollv;
                        this.update();
                    } else if (mp.y > gp.y + this.height) {
                        this.scrollValue += scrollv;
                        this.update();
                    }

                    // 挿入候補位置を算出
                    const cursorY = ~~((mp.y - gp.y) + this.scrollValue);
                    const hoverItemIndex = ~~((cursorY - this.space) / (this.lineHeight + this.space));

                    if (cursorY > (hoverItemIndex + 0.5) * (this.lineHeight + this.space)) {
                        this.insertCandPos = hoverItemIndex + 1;
                    } else {
                        this.insertCandPos = hoverItemIndex;
                    }


                    const itemCount = this.getItemCount();
                    if (this.insertCandPos < 0) { this.insertCandPos = 0; }
                    if (this.insertCandPos >= itemCount) { this.insertCandPos = itemCount; }
                    //console.log(this.insertCandPos);
                    animationTimer = requestAnimationFrame(longswipeHandler);
                };
                longswipeHandler();
                this.update();
            });

            this.addEventListener("longswipe", (event: UISwipeEvent) => {
                mp.x = event.x;
                mp.y = event.y;
            });

            this.addEventListener("longswipeend", (event: UISwipeEvent) => {
                cancelAnimationFrame(animationTimer);
                animationTimer = 0;
                if (dragItemIndex != -1 && this.insertCandPos != null) {
                    this.dragItem.call(this, dragItemIndex, this.insertCandPos);
                }
                this.insertCandPos = null;
                this.update();
            });

            this.addEventListener("click", (event: UIMouseEvent) => {

                this.click.call(this, event);
            })
            installSwipeOrTapOrLongTapDelegate(this);

        }

        private contentHeight(): number {
            const itemCount = this.getItemCount();
            if (itemCount === 0) {
                return 0;
            } else {
                return (this.lineHeight + this.space) * (itemCount) + this.space;
            }
        }

        update(): void {
            const contentHeight = this.contentHeight();

            if (this.height >= contentHeight) {
                this.scrollValue = 0;
            } else if (this.scrollValue < 0) {
                this.scrollValue = 0;
            } else if (this.scrollValue > (contentHeight - this.height)) {
                this.scrollValue = contentHeight - this.height;
            }
        }
        draw(context: CanvasRenderingContext2D) {
            this.update();
            const scrollValue = ~~this.scrollValue;
            let sy = -(scrollValue % (this.lineHeight + this.space)) + this.space;

            let index = ~~(scrollValue / (this.lineHeight + this.space));
            let itemCount = this.getItemCount();
            let drawResionHeight = this.height - sy;

            context.fillStyle = "rgba(255,255,255,0.25)";
            context.fillRect(this.left, this.top, this.width, this.height);

            // 要素描画
            for (; ;) {
                if (sy >= this.height) { break; }
                if (this.insertCandPos == index) {
                    context.save();
                    context.fillStyle = "rgba(255,0,0,1)";
                    context.fillRect(this.left, this.top + sy - this.space, this.width - this.scrollbarWidth, this.space);
                    context.restore();
                }
                if (index >= itemCount) { break; }
                context.save();
                context.beginPath();
                context.rect(this.left, Math.max(this.top, this.top + sy), this.width - this.scrollbarWidth, Math.min(drawResionHeight, this.lineHeight));
                context.clip();
                this.drawItem(context, this.left, this.top + sy, this.width - this.scrollbarWidth, this.lineHeight, index);
                context.restore();
                drawResionHeight -= this.lineHeight + this.space;
                sy += this.lineHeight + this.space;
                index++;
            }

            // スクロールバー描画
            const contentHeight = this.contentHeight();
            if (contentHeight > this.height) {
                const viewSizeRate = this.height * 1.0 / contentHeight;
                const scrollBarHeight = ~~(viewSizeRate * this.height);
                const scrollBarBlankHeight = this.height - scrollBarHeight;
                const scrollPosRate = this.scrollValue * 1.0 / (contentHeight - this.height);
                const scrollBarTop = ~~(scrollBarBlankHeight * scrollPosRate);

                context.fillStyle = "rgb(128,128,128)";
                context.fillRect(this.left + this.width - this.scrollbarWidth, this.top, this.scrollbarWidth, this.height);

                context.fillStyle = "rgb(255,255,255)";
                context.fillRect(this.left + this.width - this.scrollbarWidth, this.top + scrollBarTop, this.scrollbarWidth, scrollBarHeight);
            }
        }
        getItemIndexByPosition(x: number, y: number) {
            this.update();
            if (x < 0 || this.width <= x || y < 0 || this.height <= y) {
                return -1;
            }
            const index = ~~((y + this.scrollValue - this.space) / (this.lineHeight + this.space));
            if (index < 0 || index >= this.getItemCount()) {
                return -1;
            } else {
                return index;
            }
        }
    }
    export class HorizontalSlider extends Control {
        public edgeColor: string;
        public color: string;
        public bgColor: string;
        public font: string;
        public fontColor: string;
        public textAlign: string
        public textBaseline: string
        public text: (x: number) => string;

        public value: number;
        public minValue: number;
        public maxValue: number;

        constructor(
            {
                left = 0,
                top = 0,
                width = 0,
                height = 0,
                edgeColor = `rgb(128,128,128)`,
                color = `rgb(192,192,255)`,
                bgColor = `rgb(192,192,192)`,
                font = undefined,
                fontColor = `rgb(0,0,0)`,
                textAlign = "center",
                textBaseline = "middle",
                text = (x: number) => `${x}`,
                minValue = 0,
                maxValue = 0,
                visible = true,
                enable = true,
            }: {
                    left: number;
                    top: number;
                    width: number;
                    height: number;
                    sliderWidth?: number;
                    updownButtonWidth?: number;
                    edgeColor?: string;
                    color?: string;
                    bgColor?: string;
                    font?: string;
                    textAlign?: string;
                    textBaseline?: string;
                    text?: (x: number) => string;
                    fontColor?: string;
                    minValue?: number;
                    maxValue?: number;
                    visible?: boolean;
                    enable?: boolean;
                }

        ) {
            super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
            this.edgeColor = edgeColor;
            this.color = color;
            this.bgColor = bgColor;
            this.font = font;
            this.fontColor = fontColor;
            this.textAlign = textAlign;
            this.textBaseline = textBaseline;
            this.text = text;
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.value = minValue;
            installSwipeDelegate(this);
            this.addEventListener("swipe", (event: UISwipeEvent) => {
                const { x: l, y: t } = this.globalPos;
                const x = event.x - l;
                const yy = event.y - t;
                const rangeSize = this.maxValue - this.minValue;
                if (rangeSize == 0) {
                    this.value = this.minValue;
                } else {
                    if (x <= 0) {
                        this.value = this.minValue;
                    } else if (x >= this.width) {
                        this.value = this.maxValue;
                    } else {
                        this.value = Math.trunc((x * rangeSize) / this.width) + this.minValue;
                    }
                }
                this.postEvent(new GUI.UIEvent("changed"));
                event.preventDefault();
                event.stopPropagation();
            });
        }
        draw(context: CanvasRenderingContext2D) {
            context.fillStyle = this.bgColor;
            context.fillRect(
                this.left,
                this.top,
                this.width,
                this.height
            );
            context.fillStyle = this.color;
            context.fillRect(
                this.left,
                this.top,
                ~~(this.width * (this.value - this.minValue) / (this.maxValue - this.minValue)),
                this.height
            );

            const text = this.text(this.value);
            context.fillStyle = this.fontColor;
            context.textAlign = this.textAlign;
            context.textBaseline = this.textBaseline;
            //context.globalCompositeOperation = "xor"
            context.fillText(text, this.left + ~~(this.width / 2), this.top + ~~(this.height / 2));
            //context.globalCompositeOperation = "source-over"
            context.strokeStyle = 'rgb(0,0,0)';
            context.strokeRect(
                this.left,
                this.top,
                this.width,
                this.height
            );
        }
        update() {
            const rangeSize = this.maxValue - this.minValue;
            if (rangeSize == 0) {
                this.value = this.minValue;
            } else if (this.value < this.minValue) {
                this.value = this.minValue;
            } else if (this.value >= this.maxValue) {
                this.value = this.maxValue;
            }
        }
    }
}
///////////////////////////////////////////////////////////////
class HSVColorWheelUI extends GUI.Control {
    public left: number;
    public top: number;
    public width: number;
    public height: number;
    public visible: boolean;
    public enable: boolean;
    private hsvColorWhell: HSVColorWheel;

    public changed: () => void;

    public get rgb(): RGB {
        const r = this.hsvColorWhell.rgb;
        return r;
    }
    public set rgb(value: RGB) {
        this.hsvColorWhell.rgb = value;
    }

    constructor(
        {
            left = 0,
            top = 0,
            width = 0,
            height = 0,
            visible = true,
            enable = true
        }: {
                left: number;
                top: number;
                width: number;
                height: number;
                visible?: boolean;
                enable?: boolean;
            }) {
        super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
        const radiusMax = ~~Math.min(width / 2, height / 2);
        const radiusMin = ~~Math.max(radiusMax - 20, radiusMax * 0.8);
        this.hsvColorWhell = new HSVColorWheel({ width: width, height: height, wheelRadiusMin: radiusMin, wheelRadiusMax: radiusMax, svBoxSize: radiusMax });
        GUI.installSwipeDelegate(this);

        this.addEventListener("swipe", (event: GUI.UISwipeEvent) => {
            const { x: left, y: top } = this.globalPos;
            const x = event.x - left;
            const y = event.y - top;
            if (this.hsvColorWhell.touch(x, y)) {
                this.changed();
            }
            event.preventDefault();
            event.stopPropagation();
        });

    }
    draw(context: CanvasRenderingContext2D) {
        context.save();
        context.fillStyle = "rgba(255,255,255,1)";
        context.fillRect(this.left, this.top, this.width, this.height);
        this.hsvColorWhell.draw(context, this.left, this.top);
        context.strokeStyle = "rgba(0,0,0,1)";
        context.strokeRect(this.left, this.top, this.width, this.height);
        context.restore();
    }
}
///////////////////////////////////////////////////////////////
interface Tool {
    name: string;
    compositMode: CompositMode;
    down(config: IPainterConfig, point: IPoint, currentLayer: Layer): void;
    move(config: IPainterConfig, point: IPoint, currentLayer: Layer): void;
    up(config: IPainterConfig, point: IPoint, currentLayer: Layer): void;
    draw(config: IPainterConfig, ip: ImageProcessing, imgData: Surface, finish: boolean): void;
}
///////////////////////////////////////////////////////////////
class Float32Buffer {
    protected _array: Float32Array;
    protected _length: number;
    public get capacity(): number {
        return this._array.length;
    }
    constructor(capacity: number) {
        this._array = new Float32Array(capacity);
        this._length = 0;
    }
    public grow(size: number): void {
        if (this._array.length < size) {
            const newBuffer = new Float32Array(size * 2);
            newBuffer.set(this._array);
            this._array = newBuffer;
        }
    }

    public push(...value: number[]): void {
        this.grow(this._length + value.length);
        for (let i = 0; i < value.length; i++) {
            this._array[this._length++] = value[i];
        }
    }
    public getView(): Float32Array {
        return new Float32Array(this._array.buffer, 0, this._length);
    }
    public clear(): void {
        this._length = 0;
    }
}
///////////////////////////////////////////////////////////////
class SolidPen implements Tool {
    protected joints: Float32Buffer;

    public shader: Program;
    public name: string;
    public compositMode: CompositMode;

    constructor(name: string, shader: Program, compositMode: CompositMode) {
        this.name = name;
        this.joints = new Float32Buffer(256 * 2);
        this.shader = shader;
        this.compositMode = compositMode;
    }

    public down(config: IPainterConfig, point: IPoint, currentLayer: Layer): void {
        this.joints.clear();
        this.joints.push(point.x, point.y);
    }

    public move(config: IPainterConfig, point: IPoint, currentLayer: Layer): void {
        this.joints.push(point.x, point.y);
    }

    public up(config: IPainterConfig, point: IPoint, currentLayer: Layer): void {
    }

    public draw(config: IPainterConfig, ip: ImageProcessing, imgData: Surface, finish: boolean): void {
        ip.drawLines(imgData, {
            program: this.shader,
            vertexes: this.joints.getView(),
            color: config.penColor,
            size: config.penSize,
            antialiasSize: config.antialiasSize
        }
        );

    }
}
///////////////////////////////////////////////////////////////
class CorrectionSolidPen extends SolidPen {
    protected correctedJoints: Float32Buffer;

    // 単純ガウシアン重み付け補完
    // https://www24.atwiki.jp/sigetch_2007/pages/18.html?pc_mode=1
    static g_move_avg(result: Float32Buffer, stroke: Float32Buffer, {sigma = 5, history = 20}: { sigma?: number, history?: number }): void {
        result.clear();
        const view: Float32Array = stroke.getView();
        const len: number = ~~(view.length / 2);
        const weight1: number = (Math.sqrt(2 * Math.PI) * sigma);
        const sigma2 :number = (2 * sigma * sigma);
        for (let i = 0; i < len; i++) {
            const x = view[i * 2 + 0];
            const y = view[i * 2 + 1];
            let tx = 0;
            let ty = 0;
            let scale = 0;
            const histlen = (i+1 > history) ? history : i+1;
            let histPos = i-(histlen-1);
            for (let j = 0; j < histlen; j++) {
                const weight = weight1 * Math.exp(-(histlen - 1 - j) * (histlen - 1 - j) / sigma2);
                scale = scale + weight
                tx = tx + weight * view[histPos * 2 + 0];
                ty = ty + weight * view[histPos * 2 + 1];
                histPos++;
            }
            tx /= scale;
            ty /= scale;
            result.push(tx, ty);
            //console.log(`x=${x}, y=${y}, scale=${scale},tx=${tx},ty=${ty}`);
        }
    }


    constructor(name: string, shader: Program, compositMode: CompositMode) {
        super(name, shader, compositMode);
        this.correctedJoints = new Float32Buffer(this.joints.capacity);
    }

    public draw(config: IPainterConfig, ip: ImageProcessing, imgData: Surface, finish: boolean): void {
        CorrectionSolidPen.g_move_avg(this.correctedJoints, this.joints, {});
        ip.drawLines(imgData, {
            program: this.shader,
            vertexes: this.correctedJoints.getView(),
            color: config.penColor,
            size: config.penSize,
            antialiasSize: config.antialiasSize
        }
        );
    }
}
///////////////////////////////////////////////////////////////
class RectanglePen implements Tool {
    private start: IPoint;
    private end: IPoint;

    public borderShader: Program;
    public fillShader: Program;
    public name: string;
    public compositMode: CompositMode;

    constructor(name: string, borderShader: Program, fillShader: Program, compositMode: CompositMode) {
        this.name = name;
        this.borderShader = borderShader;
        this.fillShader = fillShader;
        this.compositMode = compositMode;
        this.start = null;
        this.end = null;
    }

    public down(config: IPainterConfig, point: IPoint, currentLayer: Layer): void {
        this.start = point;
        this.end = point;
    }

    public move(config: IPainterConfig, point: IPoint, currentLayer: Layer): void {
        this.end = point;
    }

    public up(config: IPainterConfig, point: IPoint, currentLayer: Layer): void {
    }

    public draw(config: IPainterConfig, ip: ImageProcessing, imgData: Surface, finish: boolean): void {
        ip.drawLines(imgData, {
            program: this.borderShader,
            vertexes: new Float32Array([this.start.x, this.start.y, this.end.x, this.start.y, this.end.x, this.end.y, this.start.x, this.end.y, this.start.x, this.start.y]),
            color: config.penColor,
            size: config.penSize,
            antialiasSize: config.antialiasSize
        }
        );

        ip.fillRect(imgData, {
            program: this.fillShader,
            start: this.start,
            end: this.end,
            color: config.penColor,
        }
        );

    }
}
///////////////////////////////////////////////////////////////
class FloodFill implements Tool {
    public copyShader: Program;
    public name: string;
    public compositMode: CompositMode;
    public surface: Surface;
    constructor(name: string, copyShader: Program, compositMode: CompositMode) {
        this.name = name;
        this.copyShader = copyShader;
        this.compositMode = compositMode;
        this.surface = null;
    }

    public down(config: IPainterConfig, point: IPoint, currentLayer: Layer): void {
        let x = ~~point.x;
        let y = ~~point.y;
        const width = currentLayer.imageData.width;
        const height = currentLayer.imageData.height;
        const length = width * height * 4;
        const pb = new PixelBuffer(currentLayer.imageData.gl, width, height);
        const input = pb.capture(currentLayer.imageData);
        const output = new ImageData(width, height);
        output.data.fill(0);
        const checked = new Uint8Array(width * height);
        checked.fill(0);
        pb.dispose();

        const [fr, fg, fb, fa] = config.penColor;
        const start = (y * width + x) * 4;
        const [r, g, b, a] = [input[start], input[start + 1], input[start + 2], input[start + 3]];

        const q: number[] = [x, y];
        const quadWidth = width * 4;

        while (q.length) {

            const y = q.pop();
            const x = q.pop();
            const i = (y * width + x);

            let left = 0;
            let right = width - 1;

            {// left-side
                let j = i - 1;
                let off = j * 4;
                for (let xx = x - 1; xx >= 0; xx--) {
                    if (checked[j] != 1 && input[off + 0] == r && input[off + 1] == g && input[off + 2] == b && input[off + 3] == a) {
                        off -= 4;
                        j--;
                    } else {
                        left = xx + 1;
                        break;
                    }
                }
            }
            {// right-side
                let j = i + 1;
                let off = j * 4;
                for (let xx = x + 1; xx < width; xx++) {
                    if (checked[j] != 1 && input[off + 0] == r && input[off + 1] == g && input[off + 2] == b && input[off + 3] == a) {
                        off += 4;
                        j++;
                    } else {
                        right = xx - 1;
                        break;
                    }
                }
            }

            {
                let j = y * width + left;
                let ju = j - width;
                let jd = j + width;
                let off = j * 4;
                let offu = off - width * 4;
                let offd = off + width * 4;
                let flagUp = false;
                let flagDown = false;
                for (let xx = left; xx <= right; xx++) {

                    output.data[off + 0] = fr;
                    output.data[off + 1] = fg;
                    output.data[off + 2] = fb;
                    output.data[off + 3] = fa;
                    checked[j] = 1;
                    j++;
                    off += 4;

                    if (y > 0 && checked[ju] != 1 && input[offu + 0] == r && input[offu + 1] == g && input[offu + 2] == b && input[offu + 3] == a) {
                        if (flagUp == false) {
                            q.push(xx, y - 1);
                            flagUp = true;
                        }
                    } else {
                        if (flagUp == true) {
                            flagUp = false;
                        }
                    }
                    offu += 4;
                    ju++;

                    if (y < height - 1 && checked[jd] != 1 && input[offd + 0] == r && input[offd + 1] == g && input[offd + 2] == b && input[offd + 3] == a) {
                        if (flagDown == false) {
                            q.push(xx, y + 1);
                            flagDown = true;
                        }
                    } else {
                        if (flagDown == true) {
                            flagDown = false;
                        }
                    }
                    offd += 4;
                    jd++;
                }
            }


        }

        this.surface = new Surface(currentLayer.imageData.gl, output);
    }

    public move(config: IPainterConfig, point: IPoint, currentLayer: Layer): void {
    }

    public up(config: IPainterConfig, point: IPoint, currentLayer: Layer): void {
    }

    public draw(config: IPainterConfig, ip: ImageProcessing, imgData: Surface, finish: boolean): void {
        if (this.surface != null) {
            imgData.gl.fenceSync(imgData.gl.SYNC_GPU_COMMANDS_COMPLETE, 0);
            ip.applyShader(imgData, this.surface, { program: this.copyShader });
            if (finish && this.surface != null) {
                this.surface.dispose();
                this.surface = null;
            }
        }
    }
}
///////////////////////////////////////////////////////////////
interface IPainterConfig {
    scale: number;
    penSize: number;
    penCorrectionValue: number
    penColor: RGBA;
    antialiasSize: number;
    scrollX: number;
    scrollY: number;
};
enum CompositMode {
    Normal,
    Erase,
}
///////////////////////////////////////////////////////////////
interface Layer {
    imageData: Surface;
    compositMode: CompositMode;
    /*以降は作業用*/
    previewCanvas: HTMLCanvasElement;
    previewContext: CanvasRenderingContext2D;
    previewData: ImageData;
}
///////////////////////////////////////////////////////////////
class CustomPointerEvent extends CustomEvent {
    public detail: any;    // no use
    public touch: boolean;
    public mouse: boolean;
    public pointerId: number;
    public pageX: number;
    public pageY: number;
    public maskedEvent: UIEvent;
}
///////////////////////////////////////////////////////////////
module ModalDialog {
    let dialogLayerCanvas: HTMLCanvasElement;
    let dialogLayerContext: CanvasRenderingContext2D;
    let draw: (dialogLayerCanvas: HTMLCanvasElement, dialogLayerContext: CanvasRenderingContext2D) => void;
    let blockCnt = 0;

    function resize() {
        const displayWidth = dialogLayerCanvas.clientWidth;
        const displayHeight = dialogLayerCanvas.clientHeight;
        if (dialogLayerCanvas.width !== displayWidth || dialogLayerCanvas.height !== displayHeight) {
            dialogLayerCanvas.width = displayWidth;
            dialogLayerCanvas.height = displayHeight;
        }
    }

    export function init() {
        dialogLayerCanvas = document.createElement("canvas");
        dialogLayerCanvas.style.position = "absolute";
        dialogLayerCanvas.style.left = "0";
        dialogLayerCanvas.style.top = "0";
        dialogLayerCanvas.style.zIndex = "999";
        dialogLayerCanvas.style.width = "100%";
        dialogLayerCanvas.style.height = "100%";
        dialogLayerCanvas.style.backgroundColor = "rgba(0,0,0,0.5)";
        dialogLayerCanvas.style.display = "none";
        dialogLayerContext = dialogLayerCanvas.getContext("2d");
        dialogLayerContext.imageSmoothingEnabled = false;
        document.body.appendChild(dialogLayerCanvas);
        window.addEventListener("resize", () => {
            resize();
            if (dialogLayerCanvas.style.display != "none") {
                if (draw != null) {
                    draw(dialogLayerCanvas, dialogLayerContext);
                }
            }
        });
    }

    export function block() {
        if (blockCnt == 0) {
            dialogLayerCanvas.style.display = "inline";
            dialogLayerCanvas.width = dialogLayerCanvas.clientWidth;
            dialogLayerCanvas.height = dialogLayerCanvas.clientHeight;
            Input.pause = true;
        }
        blockCnt++;
    }

    export function unblock() {
        if (blockCnt > 0) {
            blockCnt--;
            if (blockCnt == 0) {
                dialogLayerCanvas.style.display = "none";
                Input.pause = false;
            }
        }
    }
    export function alert(caption: string) {
        block();
        draw = (canvas, context) => {
            const left = ~~((canvas.width - 200) / 2)
            const top = ~~((canvas.height - 100) / 2)
            context.fillStyle = "rgb(255,255,255)";
            context.fillRect(left, top, 200, 100);
        };
        draw(dialogLayerCanvas, dialogLayerContext);
        dialogLayerCanvas.style.display = "inline";
        const click = (ev: MouseEvent) => {
            const left = ~~((dialogLayerCanvas.width - 200) / 2)
            const top = ~~((dialogLayerCanvas.height - 100) / 2)
            if (left <= ev.pageX && ev.pageX < left + 200 && top <= ev.pageY && ev.pageY < top + 100) {
                dialogLayerCanvas.removeEventListener("click", click, true);
                unblock();
            }
            ev.preventDefault();
            ev.stopPropagation();
        };
        dialogLayerCanvas.addEventListener("click", click, true);
    }


}
///////////////////////////////////////////////////////////////
module Input {
    let eventEmitter: Events.EventEmitter = new Events.EventEmitter();


    export const on = eventEmitter.on.bind(eventEmitter);
    export const off = eventEmitter.off.bind(eventEmitter);
    export const one = eventEmitter.one.bind(eventEmitter);

    let isScrolling: boolean = false;
    let timeout: number = -1;
    let sDistX: number = 0;
    let sDistY: number = 0;
    let maybeClick: boolean = false;
    let maybeClickX: number = 0;
    let maybeClickY: number = 0;
    let prevTimeStamp: number = 0;
    let prevInputType: string = "";
    export let pause: boolean = false;

    export function init() {
        if (!(window as any).TouchEvent) {
            console.log("TouchEvent is not supported by your browser.");
            (window as any).TouchEvent = function () { /* this is dummy event class */ };
        }
        if (!(window as any).PointerEvent) {
            console.log("PointerEvent is not supported by your browser.");
            (window as any).PointerEvent = function () { /* this is dummy event class */ };
        }

        // add event listener to body
        document.onselectstart = () => false;
        document.oncontextmenu = () => false;
        if (document.body["pointermove"] !== undefined) {
            document.addEventListener('touchmove', evt => { evt.preventDefault(); }, false);
            document.addEventListener('touchdown', evt => { evt.preventDefault(); }, false);
            document.addEventListener('touchup', evt => { evt.preventDefault(); }, false);
            document.addEventListener('mousemove', evt => { evt.preventDefault(); }, false);
            document.addEventListener('mousedown', evt => { evt.preventDefault(); }, false);
            document.addEventListener('mouseup', evt => { evt.preventDefault(); }, false);

            document.addEventListener('pointerdown', (ev: PointerEvent) => ev.preventDefault() && (pause || eventEmitter.fire('pointerdown', ev)));
            document.addEventListener('pointermove', (ev: PointerEvent) => ev.preventDefault() && (pause || eventEmitter.fire('pointermove', ev)));
            document.addEventListener('pointerup', (ev: PointerEvent) => ev.preventDefault() && (pause || eventEmitter.fire('pointerup', ev)));
            document.addEventListener('pointerleave', (ev: PointerEvent) => ev.preventDefault() && (pause || eventEmitter.fire('pointerleave', ev)));

        } else {
            document.addEventListener('mousedown', pointerDown, false);
            document.addEventListener('touchstart', pointerDown, false);
            document.addEventListener('mouseup', pointerUp, false);
            document.addEventListener('touchend', pointerUp, false);
            document.addEventListener('mousemove', pointerMove, false);
            document.addEventListener('touchmove', pointerMove, false);
            document.addEventListener('mouseleave', pointerLeave, false);
            document.addEventListener('touchleave', pointerLeave, false);
            document.addEventListener('touchcancel', pointerUp, false);
        }

        //document.addEventListener("mousedown", (...args: any[]) => eventEmitter.fire("mousedown", ...args));
        //document.addEventListener("mousemove", (...args: any[]) => eventEmitter.fire("mousemove", ...args));
        //document.addEventListener("mouseup", (...args: any[]) => { eventEmitter.fire("mouseup", ...args); });
        document.addEventListener("wheel", (...args: any[]) => pause || eventEmitter.fire("wheel", ...args));
    }


    function checkEvent(e: /*TouchEvent | MouseEvent | PointerEvent*/ UIEvent): boolean {
        e.preventDefault();
        if (pause) {
            return false;
        }
        const istouch = e instanceof TouchEvent || (e instanceof PointerEvent && (e as PointerEvent).pointerType === "touch");
        const ismouse = e instanceof MouseEvent || ((e instanceof PointerEvent && ((e as PointerEvent).pointerType === "mouse" || (e as PointerEvent).pointerType === "pen")));
        if (istouch && prevInputType !== "touch") {
            if (e.timeStamp - prevTimeStamp >= 500) {
                prevInputType = "touch";
                prevTimeStamp = e.timeStamp;
                return true;
            } else {
                return false;
            }
        } else if (ismouse && prevInputType !== "mouse") {
            if (e.timeStamp - prevTimeStamp >= 500) {
                prevInputType = "mouse";
                prevTimeStamp = e.timeStamp;
                return true;
            } else {
                return false;
            }
        } else {
            prevInputType = istouch ? "touch" : ismouse ? "mouse" : "none";
            prevTimeStamp = e.timeStamp;
            return istouch || ismouse;
        }
    }

    function pointerDown(e: /*TouchEvent | MouseEvent | PointerEvent*/ UIEvent): boolean {
        if (checkEvent(e)) {
            const evt = makePointerEvent("down", e);
            const singleFinger: boolean = (e instanceof MouseEvent) || (e instanceof TouchEvent && (e as TouchEvent).touches.length === 1);
            if (!isScrolling && singleFinger) {
                maybeClick = true;
                maybeClickX = evt.pageX;
                maybeClickY = evt.pageY;
            }
        }
        return false;
    }

    function pointerLeave(e: /*TouchEvent | MouseEvent | PointerEvent*/ UIEvent): boolean {
        if (checkEvent(e)) {
            maybeClick = false;
            makePointerEvent("leave", e);
        }
        return false;
    }

    function pointerMove(e: /*TouchEvent | MouseEvent | PointerEvent*/ UIEvent): boolean {
        if (checkEvent(e)) {
            makePointerEvent("move", e);
        }
        return false;
    }

    function pointerUp(e: /*TouchEvent | MouseEvent | PointerEvent*/ UIEvent): boolean {
        if (checkEvent(e)) {
            const evt = makePointerEvent("up", e);
            if (maybeClick) {
                if (Math.abs(maybeClickX - evt.pageX) < 5 && Math.abs(maybeClickY - evt.pageY) < 5) {
                    if (!isScrolling ||
                        (Math.abs(sDistX - window.pageXOffset) < 5 &&
                            Math.abs(sDistY - window.pageYOffset) < 5)) {
                        makePointerEvent("click", e);
                    }
                }
            }
            maybeClick = false;
        }
        return false;
    }

    function makePointerEvent(type: string, e: /*TouchEvent | MouseEvent | PointerEvent*/ UIEvent): CustomPointerEvent {
        const evt: CustomPointerEvent = document.createEvent("CustomEvent") as CustomPointerEvent;
        const eventType = `pointer${type}`;
        evt.initCustomEvent(eventType, true, true, e);
        evt.touch = e.type.indexOf("touch") === 0;
        evt.mouse = e.type.indexOf("mouse") === 0;
        if (evt.touch) {
            const touchEvent: TouchEvent = e as TouchEvent;
            evt.pointerId = touchEvent.changedTouches[0].identifier;
            evt.pageX = touchEvent.changedTouches[0].pageX;
            evt.pageY = touchEvent.changedTouches[0].pageY;
        }
        if (evt.mouse) {
            const mouseEvent: MouseEvent = e as MouseEvent;
            evt.pointerId = 0;
            evt.pageX = mouseEvent.clientX + window.pageXOffset;
            evt.pageY = mouseEvent.clientY + window.pageYOffset;
        }
        evt.maskedEvent = e;
        eventEmitter.fire(eventType, evt);
        return evt;
    }
}
///////////////////////////////////////////////////////////////
module Painter {
    export interface SaveDataLayerConfig {
        image: string;
        compositMode: CompositMode;
    }
    export interface SaveDataConfig {
        width: number,
        height: number,
        layers: SaveDataLayerConfig[]
    };

    let parentHtmlElement: HTMLElement = null;

    let uiDispacher: GUI.Control = null;

    /**
     * UI描画用キャンバス
     */
    let uiCanvas: HTMLCanvasElement = null;
    let uiContext: CanvasRenderingContext2D = null;

    /**
     * 画像描画用キャンバス（非表示）
     */
    let imageCanvas: HTMLCanvasElement = null;
    let imageContext: CanvasRenderingContext2D = null;

    let glCanvas: HTMLCanvasElement = null;
    let glContext: WebGL2RenderingContext = null;
    let imageProcessing: ImageProcessing = null;

    /**
     * レイヤー結合結果
     */
    let imageLayerCompositedSurface: Surface = null;

    /**
     * 各レイヤー
     */
    let imageCurrentLayer: number = -1;
    let imageLayers: Layer[] = [];

    /**
     * 作業レイヤー
     */
    let workLayer: Layer = null;
    let preCompositLayer: Layer = null;

    function createLayer(): Layer {
        const previewCanvas = document.createElement("canvas");
        previewCanvas.width = previewCanvas.height = 50;
        const previewContext = previewCanvas.getContext("2d");
        const previewData = previewContext.createImageData(50, 50);

        return {
            imageData: imageProcessing.createBlankOffscreenTarget(imageLayerCompositedSurface.width, imageLayerCompositedSurface.height),
            previewCanvas: previewCanvas,
            previewContext: previewContext,
            previewData: previewData,
            compositMode: CompositMode.Normal
        }
    }

    function disposeLayer(self: Layer): void {
        self.imageData.dispose();
        self.imageData = null;
        self.previewCanvas = null;
        self.previewContext = null;
        self.previewData = null;
    }

    function UpdateLayerPreview(layer: Layer): Layer {

        //const sw = layer.imageData.width;
        //const sh = layer.imageData.height;
        //const pw = layer.previewData.width;
        //const ph = layer.previewData.height;
        //
        //const dstData = layer.previewData.data;
        //let dst = 0;
        //const pb = new PixelBuffer(layer.imageData.gl, sw, sh);
        //const srcData = pb.capture(layer.imageData);
        //
        //for (let y = 0; y < ph; y++) {
        //    const sy = ~~(y * sh / ph);
        //    for (let x = 0; x < pw; x++) {
        //        const sx = ~~(x * sw / pw);
        //        dstData[dst + 0] = srcData[(sy * sw + sx) * 4 + 0];
        //        dstData[dst + 1] = srcData[(sy * sw + sx) * 4 + 1];
        //        dstData[dst + 2] = srcData[(sy * sw + sx) * 4 + 2];
        //        dstData[dst + 3] = srcData[(sy * sw + sx) * 4 + 3];
        //        dst += 4;
        //    }
        //}
        //pb.dispose();

        //layer.previewContext.putImageData(layer.previewData, 0, 0);
        //update({ gui: true });
        //return layer;

        const sw = layer.imageData.width;
        const sh = layer.imageData.height;
        const pw = layer.previewData.width;
        const ph = layer.previewData.height;
        const pSurf = imageProcessing.createBlankOffscreenTarget(pw, ph);
        const scale = Math.min(pw / sw, ph / sh);
        const mat = Matrix3.scaling(scale, scale);
        imageProcessing.applyShader(pSurf, layer.imageData, { program: copyShader, matrix: mat });
        const dstData = layer.previewData.data;
        let dst = 0;
        const pb = new PixelBuffer(pSurf.gl, pw, ph);
        const srcData = pb.capture(pSurf);

        layer.previewContext.putImageData(new ImageData(srcData, pw, ph), 0, 0);
        update({ gui: true });

        pb.dispose();
        pSurf.dispose();

        return layer;
    }

    /**
     * 作画画面ビュー用
     */
    //let viewCanvas: HTMLCanvasElement = null;
    //let viewContext: CanvasRenderingContext2D = null;

    /**
     * オーバーレイ用（矩形選択など描画とは関係ないガイド線などを描画する）
     */
    let overlayCanvas: HTMLCanvasElement = null;
    let overlayContext: CanvasRenderingContext2D = null;

    let canvasOffsetX: number = 0;
    let canvasOffsetY: number = 0;

    let tools: Tool[] = [
    ];

    let currentTool: Tool = null;

    export let config: IPainterConfig = {
        scale: 1,
        penSize: 5,
        penCorrectionValue: 10,
        penColor: [0, 0, 0, 64],
        antialiasSize: 2,
        scrollX: 0,
        scrollY: 0,
    };

    let updateRequest: { overlay: boolean, view: boolean, gui: boolean } = { overlay: false, view: false, gui: false };
    let updateTimerId: number = NaN;

    //let penShader: Program = null;
    let alphaPenShader: Program = null;
    let normalBlendShader: Program = null;
    let eraserBlendShader: Program = null;
    let normalCopyShader: Program = null;
    let copyShader: Program = null;
    let checkerBoardShader: Program = null;
    let alphaFillShader: Program = null;

    export function init(parentHtmlElement: HTMLElement, width: number, height: number) {
        Input.init();
        ModalDialog.init();

        parentHtmlElement = parentHtmlElement;

        imageCanvas = document.createElement("canvas");
        imageCanvas.style.position = "absolute";
        imageCanvas.style.left = "0";
        imageCanvas.style.top = "0";
        imageCanvas.style.width = "100%";
        imageCanvas.style.height = "100%";
        glContext = imageCanvas.getContext('webgl2') as WebGL2RenderingContext;
        parentHtmlElement.appendChild(imageCanvas);

        imageProcessing = new ImageProcessing(glContext);

        imageLayerCompositedSurface = imageProcessing.createBlankOffscreenTarget(width, height);
        imageLayers = [createLayer()];
        imageCurrentLayer = 0;


        workLayer = createLayer();
        preCompositLayer = createLayer();

        //viewCanvas = document.createElement("canvas");
        //viewCanvas.style.position = "absolute";
        //viewCanvas.style.left = "0";
        //viewCanvas.style.top = "0";
        //viewCanvas.style.width = "100%";
        //viewCanvas.style.height = "100%";
        //viewContext = viewCanvas.getContext("2d");
        //viewContext.imageSmoothingEnabled = false;
        //parentHtmlElement.appendChild(viewCanvas);

        overlayCanvas = document.createElement("canvas");
        overlayCanvas.style.position = "absolute";
        overlayCanvas.style.left = "0";
        overlayCanvas.style.top = "0";
        overlayCanvas.style.width = "100%";
        overlayCanvas.style.height = "100%";
        overlayContext = overlayCanvas.getContext("2d");
        overlayContext.imageSmoothingEnabled = false;
        parentHtmlElement.appendChild(overlayCanvas);

        uiDispacher = new GUI.Control({});

        uiCanvas = document.createElement("canvas");
        uiCanvas.style.position = "absolute";
        uiCanvas.style.left = "0";
        uiCanvas.style.top = "0";
        uiCanvas.style.width = "100%";
        uiCanvas.style.height = "100%";
        uiContext = uiCanvas.getContext("2d");
        uiContext.imageSmoothingEnabled = false;
        parentHtmlElement.appendChild(uiCanvas);

        updateRequest = { gui: false, overlay: false, view: false };
        updateTimerId = NaN;

        //const program = Program.loadShaderById(glContext, ["2d-vertex-shader", "2d-fragment-shader"], [glContext.VERTEX_SHADER, glContext.FRAGMENT_SHADER]);
        //const program2 = Program.loadShaderById(glContext, ["2d-vertex-shader-2", "2d-fragment-shader-2"], [glContext.VERTEX_SHADER, glContext.FRAGMENT_SHADER]);
        //penShader = Program.loadShaderById(glContext, ["2d-vertex-shader-3", "2d-fragment-shader-3"], [glContext.VERTEX_SHADER, glContext.FRAGMENT_SHADER]);
        normalBlendShader = Program.loadShaderById(glContext, ["2d-vertex-shader-4", "2d-fragment-shader-4"], [glContext.VERTEX_SHADER, glContext.FRAGMENT_SHADER]);
        eraserBlendShader = Program.loadShaderById(glContext, ["2d-vertex-shader-5", "2d-fragment-shader-5"], [glContext.VERTEX_SHADER, glContext.FRAGMENT_SHADER]);
        copyShader = Program.loadShaderById(glContext, ["2d-vertex-shader-2", "2d-fragment-shader-2"], [glContext.VERTEX_SHADER, glContext.FRAGMENT_SHADER]);
        checkerBoardShader = Program.loadShaderById(glContext, ["2d-vertex-shader-6", "2d-fragment-shader-6"], [glContext.VERTEX_SHADER, glContext.FRAGMENT_SHADER]);
        alphaPenShader = Program.loadShaderById(glContext, ["2d-vertex-shader-7", "2d-fragment-shader-7"], [glContext.VERTEX_SHADER, glContext.FRAGMENT_SHADER]);
        alphaFillShader = Program.loadShaderById(glContext, ["2d-vertex-shader-8", "2d-fragment-shader-8"], [glContext.VERTEX_SHADER, glContext.FRAGMENT_SHADER]);

        updateCompositLayer();

        tools.push(
            new SolidPen("SolidPen", alphaPenShader, CompositMode.Normal),
            new CorrectionSolidPen("SolidPen2", alphaPenShader, CompositMode.Normal),
            new SolidPen("Eraser", alphaPenShader, CompositMode.Erase),
            new RectanglePen("Rect", alphaPenShader, alphaFillShader, CompositMode.Normal),
            new FloodFill("Fill", copyShader, CompositMode.Normal)
        );

        window.addEventListener("resize", () => resize());
        setupMouseEvent();

        {
            const uiWheelWindow = new GUI.Window({
                left: 0,
                top: 0,
                width: 150,
                height: 150 + 12 + 2 + 12 - 1
            });

            const uiWindowLabel = new GUI.Label({
                left: 0,
                top: 0,
                width: 150,
                height: 12 + 2,
                text: "ColorWheel",
            });
            const uiHSVColorWheel = new HSVColorWheelUI({
                left: uiWindowLabel.left,
                top: uiWindowLabel.top + uiWindowLabel.height - 1,
                width: uiWindowLabel.width,
                height: uiWindowLabel.width,
            });
            uiHSVColorWheel.changed = () => {
                const ret = uiHSVColorWheel.rgb;
                ret[3] = config.penColor[3];
                config.penColor = <RGBA>ret;
                update({ gui: true });
            }
            uiHSVColorWheel.rgb = config.penColor;
            uiWheelWindow.addChild(uiHSVColorWheel);

            const uiAlphaSlider = new GUI.HorizontalSlider({
                left: uiWindowLabel.left,
                top: uiHSVColorWheel.top + uiHSVColorWheel.height - 1,
                width: uiHSVColorWheel.width,
                color: 'rgb(255,255,255)',
                fontColor: 'rgb(0,0,0)',
                text: (x) => `alpha:${x}`,
                height: 13,
                minValue: 0,
                maxValue: 255,
            });
            uiAlphaSlider.value = config.penColor[3];
            uiAlphaSlider.addEventListener("changed", () => {
                config.penColor[3] = uiAlphaSlider.value;
                update({ gui: true });
            });
            uiWheelWindow.addChild(uiAlphaSlider);


            uiWheelWindow.addChild(uiWindowLabel);
            uiDispacher.addChild(uiWheelWindow);
        }
        {
            const uiInfoWindow = new GUI.Window({
                left: 0,
                top: 0,
                width: 150,
                height: 48 + 2,
            });

            // Information Window
            const uiInfoWindowTitle = new GUI.Label({
                left: 0,
                top: 0,
                width: 150,
                height: 12 + 2,
                text: 'Info'
            });
            const uiInfoWindowBody = new GUI.Button({
                left: uiInfoWindowTitle.left,
                top: uiInfoWindowTitle.top + uiInfoWindowTitle.height - 1,
                width: uiInfoWindowTitle.width,
                color: 'rgb(255,255,255)',
                fontColor: 'rgb(0,0,0)',
                height: 12 * 3 + 2,
                text: () => [`scale = ${config.scale}`, `penSize = ${config.penSize}`, `antialiasSize = ${config.antialiasSize}`, `penColor = [${config.penColor.join(",")}]`].join("\n"),
            });
            uiInfoWindow.addChild(uiInfoWindowTitle)
            uiInfoWindow.addChild(uiInfoWindowBody)
            uiDispacher.addChild(uiInfoWindow);
        }
        {
            const uiPenConfigWindow = new GUI.Window({
                left: 0,
                top: 0,
                width: 150,
                height: 12 + 2 + 24,
            });

            // Information Window
            const uiPenConfigWindowTitle = new GUI.Label({
                left: 0,
                top: 0,
                width: 150,
                height: 12 + 2,
                text: 'PenConfig'
            });
            uiPenConfigWindow.addChild(uiPenConfigWindowTitle);
            const uiPenSizeSlider = new GUI.HorizontalSlider({
                left: uiPenConfigWindowTitle.left,
                top: uiPenConfigWindowTitle.top + uiPenConfigWindowTitle.height - 1,
                width: uiPenConfigWindowTitle.width,
                color: 'rgb(255,255,255)',
                fontColor: 'rgb(0,0,0)',
                text: (x) => `size:${x}`,
                height: 13,
                minValue: 1,
                maxValue: 100,
            });
            uiPenSizeSlider.addEventListener("changed", () => {
                config.penSize = uiPenSizeSlider.value;
                update({ gui: true });
            });
            uiPenSizeSlider.value = config.penSize;
            uiPenConfigWindow.addChild(uiPenSizeSlider);
            const uiAntialiasSizeSlider = new GUI.HorizontalSlider({
                left: uiPenConfigWindowTitle.left,
                top: uiPenSizeSlider.top + uiPenSizeSlider.height - 1,
                width: uiPenConfigWindowTitle.width,
                color: 'rgb(255,255,255)',
                fontColor: 'rgb(0,0,0)',
                text: (x) => `antialias:${x}`,
                height: 13,
                minValue: 0,
                maxValue: 100,
            });
            uiAntialiasSizeSlider.addEventListener("changed", () => {
                config.antialiasSize = uiAntialiasSizeSlider.value;
                update({ gui: true });
            });
            uiAntialiasSizeSlider.value = config.antialiasSize;
            uiPenConfigWindow.addChild(uiAntialiasSizeSlider);
            const uiPenCorrectionSlider = new GUI.HorizontalSlider({
                left: uiPenConfigWindowTitle.left,
                top: uiAntialiasSizeSlider.top + uiAntialiasSizeSlider.height - 1,
                width: uiPenConfigWindowTitle.width,
                color: 'rgb(255,255,255)',
                fontColor: 'rgb(0,0,0)',
                text: (x) => `correct:${x}`,
                height: 13,
                minValue: 1,
                maxValue: 20,
            });
            uiPenCorrectionSlider.addEventListener("changed", () => {
                config.penCorrectionValue = uiPenCorrectionSlider.value;
                update({ gui: true });
            });
            uiPenCorrectionSlider.value = config.penCorrectionValue;
            uiPenConfigWindow.height = uiPenCorrectionSlider.top + uiPenCorrectionSlider.height - 1;
            uiPenConfigWindow.addChild(uiPenCorrectionSlider);
            uiDispacher.addChild(uiPenConfigWindow);
        }
        {
            const uiToolboxWindow = new GUI.Window({
                left: 0,
                top: 0,
                width: 150,
                height: 12 + 2 + 13 - 1,
            });

            // Information Window
            const uiToolboxWindowTitle = new GUI.Label({
                left: 0,
                top: 0,
                width: 150,
                height: 12 + 2,
                text: 'Tools'
            });
            uiToolboxWindow.addChild(uiToolboxWindowTitle);
            let top = uiToolboxWindowTitle.top + uiToolboxWindowTitle.height - 1;
            const toolButtons: GUI.Button[] = [];
            for (const tool of tools) {
                const uiToolBtn = new GUI.Button({
                    left: uiToolboxWindowTitle.left,
                    top: top,
                    width: uiToolboxWindowTitle.width,
                    color: 'rgb(255,255,255)',
                    fontColor: 'rgb(0,0,0)',
                    height: 13,
                    text: tool.name
                });
                top += 13 - 1;
                uiToolBtn.addEventListener("click", () => {
                    for (const toolButton of toolButtons) {
                        toolButton.color = (uiToolBtn == toolButton) ? 'rgb(192,255,255)' : 'rgb(255,255,255)';
                    }
                    currentTool = tool;
                    update({ gui: true });
                });
                uiToolboxWindow.addChild(uiToolBtn);
                toolButtons.push(uiToolBtn);
            }
            const uiSaveButton = new GUI.Button({
                left: uiToolboxWindowTitle.left,
                top: top,
                width: uiToolboxWindowTitle.width,
                color: 'rgb(255,255,255)',
                fontColor: 'rgb(0,0,0)',
                height: 13,
                text: "save imagedata",
            });
            top += 13 - 1;
            uiSaveButton.addEventListener("click", () => {
                ModalDialog.block();
                const zip = new JSZip();
                const config: SaveDataConfig = {
                    width: imageLayerCompositedSurface.width,
                    height: imageLayerCompositedSurface.height,
                    layers: []
                };
                for (let i = 0; i < imageLayers.length; i++) {
                    const layer = imageLayers[i];
                    config.layers.push({ image: `${i}.bmp`, compositMode: layer.compositMode });
                }

                zip.file("config.json", JSON.stringify(config));
                const img = zip.folder("layers");
                for (let i = 0; i < imageLayers.length; i++) {
                    const layer = imageLayers[i];
                    img.file(`${i}.bmp`, saveAsBmp(layer.imageData), { binary: true });
                }
                zip.generateAsync({
                    type: "blob",
                    compression: "DEFLATE",
                    compressionOptions: {
                        level: 9
                    }
                })
                    .then(
                    (blob) => {
                        saveFileToLocal("savedata.zip", blob);
                        ModalDialog.unblock();
                    },
                    (reson) => {
                        ModalDialog.unblock();
                        ModalDialog.alert("save failed.");
                    }
                    );

            });
            uiToolboxWindow.addChild(uiSaveButton);
            const uiLoadButton = new GUI.Button({
                left: uiToolboxWindowTitle.left,
                top: top,
                width: uiToolboxWindowTitle.width,
                color: 'rgb(255,255,255)',
                fontColor: 'rgb(0,0,0)',
                height: 13,
                text: "load imagedata"
            });
            top += 13 - 1;
            uiLoadButton.addEventListener("click", () => {
                ModalDialog.block();

                loadFileFromLocal("application/*").then((file) => {
                    if (file == null) {
                        return Promise.reject("cannot load fle");
                    }
                    const zip = new JSZip();
                    return zip.loadAsync(file).then((zip) => {
                        if (!zip.files["config.json"]) {
                            return Promise.reject("config.json not found");
                        }
                        return zip.files["config.json"].async("string").then((data) => {
                            const config: SaveDataConfig = JSON.parse(data) as SaveDataConfig;
                            if (config == null) {
                                return Promise.reject("config.json is invalid");
                            }
                            if (config.layers.every(x => zip.files[`layers/${x.image}`] != null) == false) {
                                return Promise.reject("layer data not found.");
                            }
                            return Promise.all(
                                config.layers.map(x => zip.files[`layers/${x.image}`].async("arraybuffer").then(img => loadFromBmp(imageProcessing, img, { reqWidth: config.width, reqHeight: config.height })))
                            ).then(datas => {
                                if (datas.some(x => x == null)) {
                                    return Promise.reject("layer data is invalid.");
                                } else {
                                    //imageCanvas.width = config.width;
                                    //imageCanvas.height = config.height;
                                    imageLayers.forEach(disposeLayer);
                                    imageLayers.length = 0;

                                    for (let i = 0; i < datas.length; i++) {
                                        const layer = createLayer();
                                        imageLayers.push(layer);
                                        layer.imageData = <any>(datas[i]);
                                        layer.compositMode = config.layers[i].compositMode;
                                        UpdateLayerPreview(layer);
                                    }

                                    imageCurrentLayer = 0;
                                    imageLayerCompositedSurface.dispose();
                                    imageLayerCompositedSurface = imageProcessing.createBlankOffscreenTarget(width, height);
                                    disposeLayer(workLayer);
                                    workLayer = createLayer();
                                    disposeLayer(preCompositLayer);
                                    preCompositLayer = createLayer();
                                    updateCompositLayer();
                                    update({ gui: true, view: true, overlay: true });
                                    return Promise.resolve();
                                }
                            });
                        });
                    });
                }).then(
                    () => {
                        ModalDialog.unblock();
                    },
                    (reson) => {
                        ModalDialog.unblock();
                        ModalDialog.alert("load failed.");
                    }
                    );
            });
            uiToolboxWindow.addChild(uiLoadButton);
            uiToolboxWindow.height = top;
            uiDispacher.addChild(uiToolboxWindow);
        }
        {
            const uiLayerWindow = new GUI.Window({
                left: 0,
                top: 0,
                width: 150,
                height: 12 + 2,
            });

            // Information Window
            const uiLayerWindowTitle = new GUI.Label({
                left: 0,
                top: 0,
                width: 150,
                height: 12 + 2,
                text: 'Layer'
            });
            uiLayerWindow.addChild(uiLayerWindowTitle);
            const uiLayerWindowAddLayerBtn = new GUI.Button({
                left: uiLayerWindowTitle.left,
                top: 13,
                width: 50,
                height: 12 + 2,
                text: 'add',
            });
            uiLayerWindowAddLayerBtn.addEventListener("click", () => {
                imageLayers.splice(imageCurrentLayer, 0, createLayer());
                update({ gui: true, view: true });
            });
            uiLayerWindow.addChild(uiLayerWindowAddLayerBtn);

            const uiLayerWindowCopyLayerBtn = new GUI.Button({
                left: uiLayerWindowTitle.left + 50,
                top: 13,
                width: 50,
                height: 12 + 2,
                text: 'copy'
            });
            uiLayerWindowCopyLayerBtn.addEventListener("click", () => {
                const copiedLayer = createLayer();
                copiedLayer.compositMode = imageLayers[imageCurrentLayer].compositMode;
                imageProcessing.applyShader(copiedLayer.imageData, imageLayers[imageCurrentLayer].imageData, { program: copyShader });
                imageLayers.splice(imageCurrentLayer, 0, copiedLayer);
                UpdateLayerPreview(copiedLayer);
                updateCompositLayer();
                update({ gui: true, view: true });
            });
            uiLayerWindow.addChild(uiLayerWindowCopyLayerBtn);

            const uiLayerWindowDeleteLayerBtn = new GUI.Button({
                left: uiLayerWindowTitle.left + 100,
                top: 13,
                width: 50,
                height: 12 + 2,
                text: 'delete'
            });
            uiLayerWindowDeleteLayerBtn.addEventListener("click", () => {
                if (imageLayers.length == 1) {
                    return;
                }
                const deleted: Layer[] = imageLayers.splice(imageCurrentLayer, 1);
                if (imageCurrentLayer == imageLayers.length) {
                    imageCurrentLayer -= 1;
                }
                deleted.forEach(disposeLayer);
                updateCompositLayer();
                update({ gui: true, view: true });
            });
            uiLayerWindow.addChild(uiLayerWindowDeleteLayerBtn);

            const uiLayerListBox = new GUI.ListBox({
                left: uiLayerWindowTitle.left,
                top: uiLayerWindowAddLayerBtn.top + uiLayerWindowAddLayerBtn.height - 1,
                width: uiLayerWindowTitle.width,
                height: 48 * 4,
                lineHeight: 48,
                getItemCount: () => imageLayers.length,
                drawItem: (context: CanvasRenderingContext2D, left: number, top: number, width: number, height: number, item: number) => {
                    if (item == imageCurrentLayer) {
                        context.fillStyle = `rgb(24,196,195)`;
                    } else {
                        context.fillStyle = `rgb(133,133,133)`;
                    }
                    context.fillRect(left, top, width, height);
                    context.strokeStyle = `rgb(12,34,98)`;
                    context.lineWidth = 1;
                    context.fillStyle = `rgb(255,255,255)`;
                    context.fillRect(left, top, 50, 50);
                    context.drawImage(imageLayers[item].previewCanvas, 0, 0, 50, 50, left, top, 50, 50);
                    context.strokeRect(left, top, 50, 50);
                    context.strokeRect(left, top, width, height);
                }
            });
            uiLayerListBox.click = (ev: GUI.UIMouseEvent) => {
                const { x: gx, y: gy } = uiLayerListBox.globalPos;
                const x = ev.x - gx;
                const y = ev.y - gy;
                const select = uiLayerListBox.getItemIndexByPosition(x, y);
                imageCurrentLayer = select == -1 ? imageCurrentLayer : select;
                update({ gui: true });
            };
            uiLayerListBox.dragItem = (from: number, to: number) => {
                if (from == to || from + 1 == to) {
                    // 動かさない
                } else {
                    const target = imageLayers[from];
                    imageLayers.splice(from, 1);
                    if (from < to) {
                        to -= 1;
                    }
                    imageLayers.splice(to, 0, target);
                    updateCompositLayer();
                    update({ gui: true, view: true });
                }
            }
            const orgUpdate = uiLayerListBox.update.bind(uiLayerListBox);
            uiLayerListBox.update = function () {
                orgUpdate();
                update({ gui: true });
            }
            uiLayerWindow.addChild(uiLayerListBox);
            uiLayerWindow.height = uiLayerListBox.top + uiLayerListBox.height;
            uiDispacher.addChild(uiLayerWindow);
        }

        resize();
    }

    function getCompositShader(mode: CompositMode) {
        if (mode == CompositMode.Erase) {
            return eraserBlendShader;
        } else {
            return normalBlendShader;
        }
    }

    function updateCompositLayer() {
        imageLayerCompositedSurface.clear();
        imageProcessing.applyShaderUpdate(imageLayerCompositedSurface, { program: checkerBoardShader });
        for (let i = imageLayers.length - 1; i >= 0; i--) {
            if (i == imageCurrentLayer) {
                preCompositLayer.imageData.clear();
                imageProcessing.composit(preCompositLayer.imageData, workLayer.imageData, imageLayers[i].imageData, getCompositShader(workLayer.compositMode));
                imageProcessing.compositUpdate(imageLayerCompositedSurface, preCompositLayer.imageData, normalBlendShader);
            } else {
                imageProcessing.compositUpdate(imageLayerCompositedSurface, imageLayers[i].imageData, normalBlendShader);
            }
        }
    }

    function setupMouseEvent() {
        Input.on("pointerdown", (e: CustomPointerEvent) => {
            e.preventDefault();
            const p = pointToClient({ x: e.pageX, y: e.pageY });
            const uiev = new GUI.UIMouseEvent("pointerdown", p.x, p.y);
            uiDispacher.dispatchEvent(uiev);
            if (uiev.defaultPrevented) {
                update({ gui: true });
                return true;
            }
            if ((e.mouse && (e.detail as MouseEvent).button === 0) ||
                (e.touch && (e.detail as TouchEvent).touches.length <= 2)) {
                if ((e.mouse && (e.detail as MouseEvent).ctrlKey) || (e.touch && (e.detail as TouchEvent).touches.length == 2)) {
                    let scrollStartPos: IPoint = pointToClient({ x: e.pageX, y: e.pageY });

                    const onScrolling = (e: CustomPointerEvent) => {
                        e.preventDefault();
                        const p = pointToClient({ x: e.pageX, y: e.pageY });
                        config.scrollX += p.x - scrollStartPos.x;
                        config.scrollY += p.y - scrollStartPos.y;
                        scrollStartPos = p;
                        update({ view: true });
                        return true;
                    };
                    const onScrollEnd = (e: CustomPointerEvent) => {
                        e.preventDefault();
                        Input.off("pointermove", onScrolling);
                        const p = pointToClient({ x: e.pageX, y: e.pageY });
                        config.scrollX += p.x - scrollStartPos.x;
                        config.scrollY += p.y - scrollStartPos.y;
                        scrollStartPos = p;
                        update({ view: true });
                        return true;
                    };
                    Input.on("pointermove", onScrolling);
                    Input.one("pointerup", onScrollEnd);
                    return true;
                } else {
                    const onPenMove = (e: CustomPointerEvent) => {
                        if (currentTool) {
                            const p = pointToCanvas({ x: e.pageX, y: e.pageY });
                            currentTool.move(config, p, imageLayers[imageCurrentLayer]);
                            workLayer.imageData.clear();
                            workLayer.compositMode = currentTool.compositMode;
                            currentTool.draw(config, imageProcessing, workLayer.imageData, false);
                            updateCompositLayer();
                            update({ view: true });
                        }
                        return true;
                    };
                    const onPenUp = (e: CustomPointerEvent) => {
                        Input.off("pointermove", onPenMove);
                        if (currentTool) {
                            const p = pointToCanvas({ x: e.pageX, y: e.pageY });
                            currentTool.up(config, p, imageLayers[imageCurrentLayer]);
                            workLayer.imageData.clear();
                            currentTool.draw(config, imageProcessing, workLayer.imageData, true);
                            imageProcessing.compositUpdate(imageLayers[imageCurrentLayer].imageData, workLayer.imageData, getCompositShader(workLayer.compositMode));
                            workLayer.imageData.clear();
                            UpdateLayerPreview(imageLayers[imageCurrentLayer]);
                            updateCompositLayer();
                            update({ view: true });
                        }
                        return true;
                    };

                    if (currentTool) {
                        Input.on("pointermove", onPenMove);
                        Input.one("pointerup", onPenUp);
                        const p = pointToCanvas({ x: e.pageX, y: e.pageY });
                        currentTool.down(config, p, imageLayers[imageCurrentLayer]);
                        workLayer.imageData.clear();
                        workLayer.compositMode = currentTool.compositMode;
                        currentTool.draw(config, imageProcessing, workLayer.imageData, false);
                        updateCompositLayer();
                        update({ view: true });
                    }
                    return true;
                }
            }
            return false;
        });
        Input.on("pointermove", (e: CustomPointerEvent) => {
            e.preventDefault();
            const p = pointToClient({ x: e.pageX, y: e.pageY });
            const uiev = new GUI.UIMouseEvent("pointermove", p.x, p.y);
            uiDispacher.dispatchEvent(uiev);
            if (uiev.defaultPrevented) {
                update({ gui: true });
                return true;
            }
            return false;
        });
        Input.on("pointerup", (e: CustomPointerEvent) => {
            e.preventDefault();
            const p = pointToClient({ x: e.pageX, y: e.pageY });
            const uiev = new GUI.UIMouseEvent("pointerup", p.x, p.y);
            uiDispacher.dispatchEvent(uiev);
            if (uiev.defaultPrevented) {
                update({ gui: true });
                return true;
            }
            return false;
        });
        Input.on("wheel", (e: WheelEvent) => {
            e.preventDefault();
            if (e.ctrlKey) {
                if (e.deltaY < 0) {
                    if (config.scale < 16) {
                        config.scrollX = (config.scrollX * (config.scale + 1) / (config.scale));
                        config.scrollY = (config.scrollY * (config.scale + 1) / (config.scale));
                        config.scale += 1;
                        canvasOffsetX = ~~((imageCanvas.width - imageLayerCompositedSurface.width * config.scale) / 2);
                        canvasOffsetY = ~~((imageCanvas.height - imageLayerCompositedSurface.height * config.scale) / 2);
                        update({ overlay: true, view: true, gui: true });
                        return true;
                    }
                } else if (e.deltaY > 0) {
                    if (config.scale > 1) {
                        config.scrollX = (config.scrollX * (config.scale - 1) / (config.scale));
                        config.scrollY = (config.scrollY * (config.scale - 1) / (config.scale));
                        config.scale -= 1;
                        canvasOffsetX = ~~((imageCanvas.width - imageLayerCompositedSurface.width * config.scale) / 2);
                        canvasOffsetY = ~~((imageCanvas.height - imageLayerCompositedSurface.height * config.scale) / 2);
                        update({ overlay: true, view: true, gui: true });
                        return true;
                    }
                }
            }
            return false;
        });
    }

    function resizeCanvas(canvas: HTMLCanvasElement): boolean {
        const displayWidth = canvas.clientWidth;
        const displayHeight = canvas.clientHeight;
        if (canvas.width !== displayWidth || canvas.height !== displayHeight) {
            canvas.width = displayWidth;
            canvas.height = displayHeight;
            return true;
        } else {
            return false;
        }
    }

    export function resize() {
        const ret1 = resizeCanvas(imageCanvas);
        const ret2 = resizeCanvas(overlayCanvas);
        const ret3 = resizeCanvas(uiCanvas);
        if (ret1 || ret2) {
            canvasOffsetX = ~~((imageCanvas.width - imageLayerCompositedSurface.width * config.scale) / 2);
            canvasOffsetY = ~~((imageCanvas.height - imageLayerCompositedSurface.height * config.scale) / 2);
        }
        update({ view: (ret1 || ret2), gui: ret3 });
    }

    export function pointToClient(point: IPoint): IPoint {
        const cr = imageCanvas.getBoundingClientRect();
        const sx = (point.x - (cr.left + window.pageXOffset));
        const sy = (point.y - (cr.top + window.pageYOffset));
        return { x: sx, y: sy };
    }
    export function pointToCanvas(point: IPoint): IPoint {
        const p = pointToClient(point);
        p.x = ~~((p.x - canvasOffsetX - config.scrollX) / config.scale);
        p.y = ~~((p.y - canvasOffsetY - config.scrollY) / config.scale);
        return p;
    }


    let prev: number = NaN;
    function benchmark(msg?: string) {
        const t = Date.now();
        if (msg != null && !isNaN(prev)) {
            console.log(msg, t - prev);
        }
        prev = t;
    }

    export function update({ overlay = false, view = false, gui = false }: { overlay?: boolean, view?: boolean, gui?: boolean }) {

        updateRequest.overlay = updateRequest.overlay || overlay;
        updateRequest.view = updateRequest.view || view;
        updateRequest.gui = updateRequest.gui || gui;

        if (isNaN(updateTimerId)) {
            updateTimerId = requestAnimationFrame(() => {
                if (updateRequest.overlay) {
                    updateRequest.overlay = false;
                }
                if (updateRequest.view) {
                    // 画面内領域のみを描画。スケール＋平行移動までGPUで実行
                    const viewLeft = canvasOffsetX + config.scrollX;
                    const viewTop = canvasOffsetY + config.scrollY;
                    const viewWidth = imageLayerCompositedSurface.width * config.scale;
                    const viewHeight = imageLayerCompositedSurface.height * config.scale;
                    const viewRight = viewLeft + viewWidth;
                    const viewBottom = viewTop + viewHeight;

                    const clipedLeft = Math.max(viewLeft, 0);
                    const clipedTop = Math.max(viewTop, 0);
                    const clipedRight = Math.min(viewRight, imageCanvas.width);
                    const clipedBottom = Math.min(viewBottom, imageCanvas.height);

                    const srcLeft = Math.min(viewLeft, 0);
                    const srcTop = Math.min(viewTop, 0);
                    const srcWidth = Math.min(imageCanvas.width, viewWidth + srcLeft);
                    const srcHeight = Math.min(imageCanvas.height, viewHeight + srcTop);

                    const transformMatrix = Matrix3.multiply(Matrix3.translation(viewLeft, viewTop), Matrix3.scaling(config.scale, config.scale));

                    //imageCanvas.style.display = "none";
                    imageCanvas.width = document.body.clientWidth;
                    imageCanvas.height = document.body.clientHeight;
                    const viewSurface = new Surface(imageProcessing.gl, imageCanvas.width, imageCanvas.height);
                    viewSurface.clear();

                    const left = clipedLeft;
                    const top = clipedTop;
                    const right = clipedLeft + srcWidth;
                    const bottom = clipedTop + srcHeight;
                    imageProcessing.drawLines(viewSurface, { program: alphaPenShader, vertexes: new Float32Array([viewLeft, viewTop, viewRight, viewTop, viewRight, viewBottom, viewLeft, viewBottom, viewLeft, viewTop]), size: 20, color: [0, 0, 0, 255], antialiasSize: 40 });
                    imageProcessing.draw(viewSurface, imageLayerCompositedSurface, {
                        program: copyShader, matrix: transformMatrix,
                        start: { x: 0, y: 0 }, end: { x: imageLayerCompositedSurface.width, y: imageLayerCompositedSurface.height },
                    });
                    imageProcessing.applyShader(null, viewSurface, { program: copyShader, matrix: Matrix3.identity() });
                    viewSurface.dispose();
                    updateRequest.view = false;
                    //imageCanvas.style.display = "inline";

                    //const s = Date.now();
                    //imageProcessing.applyShader(null, imageLayerCompositedSurface, { program: copyShader });
                    //viewCanvas.style.display = "none";
                    //viewContext.clearRect(0, 0, viewCanvas.width, viewCanvas.height);
                    //viewContext.fillStyle = "rgb(198,208,224)";
                    //viewContext.fillRect(0, 0, viewCanvas.width, viewCanvas.height);
                    //viewContext.fillStyle = "rgb(255,255,255)";
                    //viewContext.shadowOffsetX = viewContext.shadowOffsetY = 0;
                    //viewContext.shadowColor = "rgb(0,0,0)";
                    //viewContext.shadowBlur = 10;
                    //viewContext.fillRect(canvasOffsetX + config.scrollX, canvasOffsetY + config.scrollY, imageCanvas.width * config.scale, imageCanvas.height * config.scale);
                    //viewContext.shadowBlur = 0;
                    //viewContext.imageSmoothingEnabled = false;
                    //viewContext.drawImage(imageCanvas, 0, 0, imageCanvas.width, imageCanvas.height, canvasOffsetX + config.scrollX, canvasOffsetY + config.scrollY, imageCanvas.width * config.scale, imageCanvas.height * config.scale);
                    //updateRequest.view = false;
                    //viewCanvas.style.display = "inline";
                    //console.log("update", Date.now() - s);
                }
                if (updateRequest.gui) {
                    uiContext.clearRect(0, 0, uiCanvas.width, uiCanvas.height);
                    uiContext.imageSmoothingEnabled = false;
                    uiDispacher.draw(uiContext);
                    updateRequest.gui = false;
                }
                updateTimerId = NaN;
            });
        }
    }
}
///////////////////////////////////////////////////////////////
window.addEventListener("load", () => {
    Painter.init(document.body, 1024, 1024);
});
///////////////////////////////////////////////////////////////

