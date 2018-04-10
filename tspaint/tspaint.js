// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />
/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.dom.d.ts" />
/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.es2016.d.ts" />
"use strict";
///////////////////////////////////////////////////////////////
function saveFileToLocal(filename, blob) {
    //const blob = new Blob([new Uint8Array(imageLayerCompositedImgData.saveAsBmp())], { type: 'image/bmp' });
    const blobURL = window.URL.createObjectURL(blob, { oneTimeOnly: true });
    const element = document.createElement("a");
    element.href = blobURL;
    element.style.display = "none";
    element.download = filename;
    document.body.appendChild(element);
    setTimeout(() => {
        element.click();
        document.body.removeChild(element);
    });
}
function loadFileFromLocal(accept) {
    return new Promise((resolve, reject) => {
        const element = document.createElement("input");
        element.type = "file";
        element.style.display = "none";
        element.accept = accept;
        document.body.appendChild(element);
        element.onclick = () => {
            const handler = () => {
                element.onclick = null;
                window.onfocus = null;
                document.body.removeChild(element);
                const file = (element.files.length != 0) ? element.files[0] : null;
                if (file == null) {
                    reject("cannot load file.");
                    return;
                }
                else {
                    resolve(file);
                    return;
                }
            };
            window.onfocus = handler;
        };
        setTimeout(() => { element.click(); });
    });
}
;
CanvasRenderingContext2D.prototype.strokeRectOriginal = CanvasRenderingContext2D.prototype.strokeRect;
CanvasRenderingContext2D.prototype.strokeRect = function (x, y, w, h) {
    this.strokeRectOriginal(x + 0.5, y + 0.5, w - 1, h - 1);
};
CanvasRenderingContext2D.prototype.drawTextBox = function (text, left, top, width, height, drawTextPred) {
    const metrics = this.measureText(text);
    const lineHeight = this.measureText("あ").width;
    const lines = text.split(/\n/);
    let offY = 0;
    lines.forEach((x, i) => {
        const metrics = this.measureText(x);
        const sublines = [];
        if (metrics.width > width) {
            let len = 1;
            while (x.length > 0) {
                const metrics = this.measureText(x.substr(0, len));
                if (metrics.width > width) {
                    sublines.push(x.substr(0, len - 1));
                    x = x.substring(len - 1);
                    len = 1;
                }
                else if (len == x.length) {
                    sublines.push(x);
                    break;
                }
                else {
                    len++;
                }
            }
        }
        else {
            sublines.push(x);
        }
        sublines.forEach((x) => {
            drawTextPred(x, left + 1, top + offY + 1);
            offY += (lineHeight + 1);
        });
    });
};
CanvasRenderingContext2D.prototype.fillTextBox = function (text, left, top, width, height) {
    this.drawTextBox(text, left, top, width, height, this.fillText.bind(this));
};
CanvasRenderingContext2D.prototype.strokeTextBox = function (text, left, top, width, height) {
    this.drawTextBox(text, left, top, width, height, this.strokeText.bind(this));
};
function rgb2hsv([r, g, b]) {
    const max = Math.max(r, g, b);
    const min = Math.min(r, g, b);
    let h = max - min;
    if (h > 0.0) {
        if (max == r) {
            h = (g - b) / h;
            if (h < 0.0) {
                h += 6.0;
            }
        }
        else if (max == g) {
            h = 2.0 + (b - r) / h;
        }
        else {
            h = 4.0 + (r - g) / h;
        }
    }
    h /= 6.0;
    let s = (max - min);
    if (max != 0.0) {
        s /= max;
    }
    let v = max;
    return [~~(h * 360), s, v];
}
function hsv2rgb([h, s, v]) {
    if (s == 0) {
        const vv = ~~(v * 255 + 0.5);
        return [vv, vv, vv];
    }
    else {
        const t = ((h * 6) % 360) / 360.0;
        const c1 = v * (1 - s);
        const c2 = v * (1 - s * t);
        const c3 = v * (1 - s * (1 - t));
        let r = 0;
        let g = 0;
        let b = 0;
        switch (~~(h / 60)) {
            case 0:
                r = v;
                g = c3;
                b = c1;
                break;
            case 1:
                r = c2;
                g = v;
                b = c1;
                break;
            case 2:
                r = c1;
                g = v;
                b = c3;
                break;
            case 3:
                r = c1;
                g = c2;
                b = v;
                break;
            case 4:
                r = c3;
                g = c1;
                b = v;
                break;
            case 5:
                r = v;
                g = c1;
                b = c2;
                break;
            default: throw new Error();
        }
        const rr = ~~(r * 255 + 0.5);
        const gg = ~~(g * 255 + 0.5);
        const bb = ~~(b * 255 + 0.5);
        return [rr, gg, bb];
    }
}
var Matrix3;
(function (Matrix3) {
    function projection(width, height) {
        // Note: This matrix flips the Y axis so that 0 is at the top.
        return [
            2 / width, 0, 0,
            0, 2 / height, 0,
            -1, -1, 1
        ];
    }
    Matrix3.projection = projection;
    function identity() {
        return [
            1, 0, 0,
            0, 1, 0,
            0, 0, 1
        ];
    }
    Matrix3.identity = identity;
    function translation(tx, ty) {
        return [
            1, 0, 0,
            0, 1, 0,
            tx, ty, 1
        ];
    }
    Matrix3.translation = translation;
    function rotation(angleInRadians) {
        const c = Math.cos(angleInRadians);
        const s = Math.sin(angleInRadians);
        return [
            c, -s, 0,
            s, c, 0,
            0, 0, 1
        ];
    }
    Matrix3.rotation = rotation;
    function scaling(sx, sy) {
        return [
            sx, 0, 0,
            0, sy, 0,
            0, 0, 1
        ];
    }
    Matrix3.scaling = scaling;
    function multiply(a, b) {
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
    Matrix3.multiply = multiply;
})(Matrix3 || (Matrix3 = {}));
///////////////////////////////////////////////////////////////
class Shader {
    constructor(gl, shader, shaderType) {
        this.gl = gl;
        this.shader = shader;
        this.shaderType = shaderType;
    }
    static create(gl, shaderSource, shaderType) {
        const shader = gl.createShader(shaderType);
        gl.shaderSource(shader, shaderSource);
        gl.compileShader(shader);
        const compiled = gl.getShaderParameter(shader, gl.COMPILE_STATUS);
        if (!compiled) {
            const lastError = gl.getShaderInfoLog(shader);
            console.error("*** Error compiling shader '" + shader + "':" + lastError);
            gl.deleteShader(shader);
            return null;
        }
        else {
            return new Shader(gl, shader, shaderType);
        }
    }
    static loadShaderById(gl, id, optShaderType) {
        const shaderScript = document.getElementById(id);
        if (!shaderScript) {
            throw ("*** Error: unknown element `" + id + "`");
        }
        const shaderSource = shaderScript.text;
        let shaderType = optShaderType;
        if (!optShaderType) {
            if (shaderScript.type === "x-shader/x-vertex") {
                shaderType = gl.VERTEX_SHADER;
            }
            else if (shaderScript.type === "x-shader/x-fragment") {
                shaderType = gl.FRAGMENT_SHADER;
            }
            else if (shaderType !== gl.VERTEX_SHADER && shaderType !== gl.FRAGMENT_SHADER) {
                throw ("*** Error: unknown shader type");
            }
        }
        return Shader.create(gl, shaderSource, shaderType);
    }
    dispose() {
        this.gl.deleteShader(this.shader);
        this.shader = null;
        this.gl = null;
        this.shaderType = NaN;
    }
}
///////////////////////////////////////////////////////////////
class Program {
    constructor(gl, program) {
        this.gl = gl;
        this.program = program;
    }
    static create(gl, shaders, optAttribs, optLocations) {
        const program = gl.createProgram();
        shaders.forEach(shader => {
            gl.attachShader(program, shader);
        });
        if (optAttribs) {
            optAttribs.forEach((attrib, ndx) => {
                gl.bindAttribLocation(program, optLocations ? optLocations[ndx] : ndx, attrib);
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
    static loadShaderById(gl, ids, shaderTypes, optAttribs, optLocations) {
        const shaders = [];
        for (let ii = 0; ii < ids.length; ++ii) {
            shaders.push(Shader.loadShaderById(gl, ids[ii], shaderTypes[ii]));
        }
        return Program.create(gl, shaders.map((x) => x.shader), optAttribs, optLocations);
    }
    dispose() {
        this.gl.deleteProgram(this.program);
        this.program = null;
        this.gl = null;
    }
}
///////////////////////////////////////////////////////////////
class Surface {
    get gl() {
        return this._gl;
    }
    get texture() {
        return this._texture;
    }
    get framebuffer() {
        return this._framebuffer;
    }
    get width() {
        return this._width;
    }
    get height() {
        return this._height;
    }
    constructor(gl, srcImageOrWidth, height) {
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
            const img = srcImageOrWidth;
            this._width = img.width;
            this._height = img.height;
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
        }
        else if (srcImageOrWidth instanceof ImageData) {
            const img = srcImageOrWidth;
            this._width = img.width;
            this._height = img.height;
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
        }
        else {
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, srcImageOrWidth, height, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
            this._width = srcImageOrWidth;
            this._height = height;
        }
        // フレームバッファを生成し、テクスチャと関連づけ
        this._framebuffer = gl.createFramebuffer();
        gl.bindFramebuffer(gl.FRAMEBUFFER, this._framebuffer);
        gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, this._texture, 0);
    }
    clear() {
        const gl = this._gl;
        gl.bindFramebuffer(gl.FRAMEBUFFER, this.framebuffer);
        gl.viewport(0, 0, this._width, this._height);
        gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT | gl.STENCIL_BUFFER_BIT);
    }
    dispose() {
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
class PixelBuffer {
    constructor(gl, width, height) {
        this.gl = gl;
        this.width = width;
        this.height = height;
        this.pbo = gl.createBuffer();
        this.data = new Uint8ClampedArray(this.width * this.height * 4);
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, this.pbo);
        gl.bufferData(gl.PIXEL_PACK_BUFFER, this.width * this.height * 4, gl.STREAM_READ);
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, null);
    }
    capture(src) {
        const gl = this.gl;
        gl.bindFramebuffer(gl.FRAMEBUFFER, src.framebuffer);
        // フレームバッファをピクセルバッファにロード
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, this.pbo);
        gl.readPixels(0, 0, this.width, this.height, gl.RGBA, gl.UNSIGNED_BYTE, 0);
        // ピクセルバッファからCPU側の配列にロード
        gl.fenceSync(gl.SYNC_GPU_COMMANDS_COMPLETE, 0);
        gl.getBufferSubData(gl.PIXEL_PACK_BUFFER, 0, this.data);
        gl.bindBuffer(gl.PIXEL_PACK_BUFFER, null);
        return this.data;
    }
    dispose() {
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
    constructor(gl) {
        this.gl = gl;
        // 頂点バッファ（座標）を作成
        this.positionBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
        // 頂点バッファ（テクスチャ座標）を作成
        this.texcoordBuffer = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
    }
    // フィルタカーネルの重み合計（重み合計が0以下の場合は1とする）
    static computeKernelWeight(kernel) {
        const weight = kernel.reduce((prev, curr) => prev + curr);
        return weight <= 0 ? 1 : weight;
    }
    setVertexPosition(array) {
        const gl = this.gl;
        gl.bindBuffer(gl.ARRAY_BUFFER, this.positionBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(array), gl.STREAM_DRAW);
    }
    setTexturePosition(array) {
        const gl = this.gl;
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(array), gl.STREAM_DRAW);
    }
    createBlankOffscreenTarget(width, height) {
        return new Surface(this.gl, width, height);
    }
    createOffscreenTargetFromImage(image) {
        return new Surface(this.gl, image);
    }
    static createRectangle(x1, y1, x2, y2, target) {
        if (target) {
            Array.prototype.push.call(target, x1, y1, x2, y1, x1, y2, x1, y2, x2, y1, x2, y2);
            return target;
        }
        else {
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
    applyKernel(dst, src, { kernel = null, program = null }) {
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
        gl.vertexAttribPointer(positionLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        // シェーダのテクスチャ座標Attributeを有効化
        gl.enableVertexAttribArray(texcoordLocation);
        // 頂点バッファ（テクスチャ座標）を設定
        this.setTexturePosition(ImageProcessing.createRectangle(0, 0, 1, 1));
        // テクスチャ座標Attributeの位置情報を設定
        gl.vertexAttribPointer(texcoordLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
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
    applyShader(dst, src, { program = null }) {
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
        gl.vertexAttribPointer(positionLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        // シェーダのテクスチャ座標Attributeを有効化
        gl.enableVertexAttribArray(texcoordLocation);
        // 頂点バッファ（テクスチャ座標）を設定
        this.setTexturePosition(ImageProcessing.createRectangle(0, 0, 1, 1));
        // テクスチャ座標Attributeの位置情報を設定
        gl.vertexAttribPointer(texcoordLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
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
        }
        else {
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
    applyShaderUpdate(dst, { program = null }) {
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
        gl.vertexAttribPointer(positionLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
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
        }
        else {
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
    drawLines(dst, { program = null, vertexes = null, size = null, color = null, antialiasSize = null }) {
        const gl = this.gl;
        const tmp = this.createBlankOffscreenTarget(dst.width, dst.height);
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
        gl.vertexAttribPointer(positionLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        // シェーダのテクスチャ座標Attributeを有効化
        gl.enableVertexAttribArray(texcoordLocation);
        // テクスチャ座標Attributeの位置情報を設定
        // テクスチャ座標は後で設定
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
        gl.vertexAttribPointer(texcoordLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
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
            const left = Math.min(x1, x2) - size;
            const top = Math.min(y1, y2) - size;
            const right = Math.max(x1, x2) + size;
            const bottom = Math.max(y1, y2) + size;
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
    fillRect(dst, { program = null, start = null, end = null, color = null }) {
        const gl = this.gl;
        const tmp = this.createBlankOffscreenTarget(dst.width, dst.height);
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
        gl.vertexAttribPointer(positionLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        // シェーダのテクスチャ座標Attributeを有効化
        gl.enableVertexAttribArray(texcoordLocation);
        // テクスチャ座標Attributeの位置情報を設定
        // テクスチャ座標は後で設定
        gl.bindBuffer(gl.ARRAY_BUFFER, this.texcoordBuffer);
        gl.vertexAttribPointer(texcoordLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
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
        // レンダリング範囲をフィードバック
        gl.copyTexSubImage2D(gl.TEXTURE_2D, 0, left, top, left, top, right - left, bottom - top);
        tmp.dispose();
    }
    composit(dst, front, back, program) {
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
        gl.vertexAttribPointer(positionLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        // シェーダのテクスチャ座標Attributeを有効化
        gl.enableVertexAttribArray(texcoordLocation);
        // 頂点バッファ（テクスチャ座標）を設定
        this.setTexturePosition(ImageProcessing.createRectangle(0, 0, 1, 1));
        // テクスチャ座標Attributeの位置情報を設定
        gl.vertexAttribPointer(texcoordLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
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
        const st = gl.checkFramebufferStatus(gl.FRAMEBUFFER);
        if (st !== gl.FRAMEBUFFER_COMPLETE) {
            console.log(st);
        }
        // オフスクリーンレンダリングを実行
        gl.drawArrays(gl.TRIANGLES, 0, 6);
        // 適用完了
    }
    compositUpdate(dst, src, program, updateSrc = false) {
        const gl = this.gl;
        // arrtibute変数の位置を取得
        const positionLocation = gl.getAttribLocation(program.program, "a_position");
        const texcoordLocation = gl.getAttribLocation(program.program, "a_texCoord");
        // uniform変数の位置を取得
        const matrixLocation = gl.getUniformLocation(program.program, "u_matrix");
        const texture0Lication = gl.getUniformLocation(program.program, 'u_front');
        const texture1Lication = gl.getUniformLocation(program.program, 'u_back');
        // 出力先を生成
        const tmp = this.createBlankOffscreenTarget(dst.width, dst.height);
        tmp.clear();
        // シェーダを設定
        gl.useProgram(program.program);
        // シェーダの頂点座標Attributeを有効化
        gl.enableVertexAttribArray(positionLocation);
        // 頂点バッファ（座標）を設定
        this.setVertexPosition(ImageProcessing.createRectangle(0, 0, src.width, src.height));
        // 頂点座標Attributeの位置情報を設定
        gl.vertexAttribPointer(positionLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
        );
        // シェーダのテクスチャ座標Attributeを有効化
        gl.enableVertexAttribArray(texcoordLocation);
        // 頂点バッファ（テクスチャ座標）を設定
        this.setTexturePosition(ImageProcessing.createRectangle(0, 0, 1, 1));
        // テクスチャ座標Attributeの位置情報を設定
        gl.vertexAttribPointer(texcoordLocation, 2, // 2 components per iteration
        gl.FLOAT, // the data is 32bit floats
        false, // don't normalize the data
        0, // 0 = move forward size * sizeof(type) each iteration to get the next position
        0 // start at the beginning of the buffer
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
        const st = gl.checkFramebufferStatus(gl.FRAMEBUFFER);
        if (st !== gl.FRAMEBUFFER_COMPLETE) {
            console.log(st);
        }
        // オフスクリーンレンダリングを実行
        gl.drawArrays(gl.TRIANGLES, 0, 6);
        // 適用完了
        // レンダリング範囲をフィードバック
        if (updateSrc) {
            gl.activeTexture(gl.TEXTURE1);
            gl.bindTexture(gl.TEXTURE_2D, null);
            gl.activeTexture(gl.TEXTURE0);
            gl.bindTexture(gl.TEXTURE_2D, src.texture);
        }
        else {
            gl.activeTexture(gl.TEXTURE1);
            gl.bindTexture(gl.TEXTURE_2D, null);
            gl.activeTexture(gl.TEXTURE0);
            gl.bindTexture(gl.TEXTURE_2D, dst.texture);
        }
        gl.copyTexSubImage2D(gl.TEXTURE_2D, 0, 0, 0, 0, 0, dst.width, dst.height);
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        // 出力先を破棄
        tmp.dispose();
    }
}
///////////////////////////////////////////////////////////////
const sizeOfBitmapFileHeader = 14;
const sizeOfBitmapInfoHeader = 40;
function saveAsBmp(imageData) {
    const pixelData = new PixelBuffer(imageData.gl, imageData.width, imageData.height);
    const pixels = pixelData.capture(imageData);
    const sizeOfImageData = imageData.width * imageData.height * 4;
    pixelData.dispose();
    const bitmapData = new ArrayBuffer(sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader + sizeOfImageData);
    //
    // BITMAPFILEHEADER
    //
    const viewOfBitmapFileHeader = new DataView(bitmapData, 0, sizeOfBitmapFileHeader);
    viewOfBitmapFileHeader.setUint16(0, 0x4D42, true); // bfType : 'BM'
    viewOfBitmapFileHeader.setUint32(2, sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader + sizeOfImageData, true); // bfSize :  sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER) + sizeof(IMAGEDATA)
    viewOfBitmapFileHeader.setUint16(6, 0x0000, true); // bfReserved1 : 0
    viewOfBitmapFileHeader.setUint16(8, 0x0000, true); // bfReserved2 : 0
    viewOfBitmapFileHeader.setUint32(10, sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader, true); // bfOffBits : sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER)
    //
    // BITMAPINFOHEADER
    //
    const viewOfBitmapInfoHeader = new DataView(bitmapData, sizeOfBitmapFileHeader, sizeOfBitmapInfoHeader);
    viewOfBitmapInfoHeader.setUint32(0, sizeOfBitmapInfoHeader, true); // biSize : sizeof(BITMAPINFOHEADER)
    viewOfBitmapInfoHeader.setUint32(4, imageData.width, true); // biWidth : data.width
    viewOfBitmapInfoHeader.setUint32(8, imageData.height, true); // biHeight : data.height
    viewOfBitmapInfoHeader.setUint16(12, 1, true); // biPlanes : 1
    viewOfBitmapInfoHeader.setUint16(14, 32, true); // biBitCount : 32
    viewOfBitmapInfoHeader.setUint32(16, 0, true); // biCompression : 0
    viewOfBitmapInfoHeader.setUint32(20, sizeOfImageData, true); // biSizeImage : sizeof(IMAGEDATA)
    viewOfBitmapInfoHeader.setUint32(24, 0, true); // biXPixPerMeter : 0
    viewOfBitmapInfoHeader.setUint32(28, 0, true); // biYPixPerMeter : 0
    viewOfBitmapInfoHeader.setUint32(32, 0, true); // biClrUsed : 0
    viewOfBitmapInfoHeader.setUint32(36, 0, true); // biCirImportant : 0
    //
    // IMAGEDATA
    //
    const viewOfBitmapPixelData = new DataView(bitmapData, sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader, imageData.width * imageData.height * 4);
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
}
;
function loadFromBmp(ip, bitmapData, { reqWidth = -1, reqHeight = -1 }) {
    const dataLength = bitmapData.byteLength;
    //
    // BITMAPFILEHEADER
    //
    const reqSize = (reqWidth >= 0 && reqHeight >= 0) ? reqWidth * reqHeight * 4 : -1;
    const viewOfBitmapFileHeader = new DataView(bitmapData, 0, sizeOfBitmapFileHeader);
    const bfType = viewOfBitmapFileHeader.getUint16(0, true); // bfType : 'BM'
    const bfSize = viewOfBitmapFileHeader.getUint32(2, true); // bfSize :  sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER) + sizeof(IMAGEDATA)
    const bfReserved1 = viewOfBitmapFileHeader.getUint16(6, true); // bfReserved1 : 0
    const bfReserved2 = viewOfBitmapFileHeader.getUint16(8, true); // bfReserved2 : 0
    const bfOffBits = viewOfBitmapFileHeader.getUint32(10, true); // bfOffBits : sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER)
    if ((bfType != 0x4D42) ||
        (bfSize < sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader) || (bfSize != dataLength) || (reqSize != -1 && bfSize < sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader + reqSize) ||
        (bfReserved1 != 0) ||
        (bfReserved2 != 0) ||
        (bfOffBits < sizeOfBitmapFileHeader + sizeOfBitmapInfoHeader) || (bfOffBits >= dataLength) || (reqSize != -1 && bfOffBits > dataLength - reqSize)) {
        return null;
    }
    //
    // BITMAPINFOHEADER
    //
    const viewOfBitmapInfoHeader = new DataView(bitmapData, sizeOfBitmapFileHeader, sizeOfBitmapInfoHeader);
    const biSize = viewOfBitmapInfoHeader.getUint32(0, true); // biSize : sizeof(BITMAPINFOHEADER)
    const biWidth = viewOfBitmapInfoHeader.getUint32(4, true); // biWidth : this.width
    const biHeight = viewOfBitmapInfoHeader.getUint32(8, true); // biHeight : this.height
    const biPlanes = viewOfBitmapInfoHeader.getUint16(12, true); // biPlanes : 1
    const biBitCount = viewOfBitmapInfoHeader.getUint16(14, true); // biBitCount : 32
    const biCompression = viewOfBitmapInfoHeader.getUint32(16, true); // biCompression : 0
    const biSizeImage = viewOfBitmapInfoHeader.getUint32(20, true); // biSizeImage : this.width * this.height * 4
    const biXPixPerMeter = viewOfBitmapInfoHeader.getUint32(24, true); // biXPixPerMeter : 0
    const biYPixPerMeter = viewOfBitmapInfoHeader.getUint32(28, true); // biYPixPerMeter : 0
    const biClrUsed = viewOfBitmapInfoHeader.getUint32(32, true); // biClrUsed : 0
    const biCirImportant = viewOfBitmapInfoHeader.getUint32(36, true); // biCirImportant : 0
    if ((biSize != sizeOfBitmapInfoHeader) ||
        (reqWidth >= 0 && biWidth != reqWidth) ||
        (reqHeight >= 0 && biHeight != reqHeight) ||
        (biPlanes != 1) ||
        (biBitCount != 32) ||
        (biSizeImage != biWidth * biHeight * 4) || (reqSize >= 0 && biSizeImage != reqSize) ||
        (biXPixPerMeter != 0) ||
        (biYPixPerMeter != 0) ||
        (biClrUsed != 0) ||
        (biCirImportant != 0)) {
        return null;
    }
    //
    // IMAGEDATA
    //
    const pixeldata = new Uint8ClampedArray(biWidth * biHeight * 4);
    const viewOfBitmapPixelData = new DataView(bitmapData, bfOffBits, biWidth * biHeight * 4);
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
class HSVColorWheel {
    constructor({ width = 0, height = 0, wheelRadiusMin = 76, wheelRadiusMax = 96, svBoxSize = 100 }) {
        this._hsv = [0, 0, 0];
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
    get hsv() {
        return this._hsv.slice();
    }
    set hsv(v) {
        this._hsv = v.slice();
    }
    get rgb() {
        return hsv2rgb(this._hsv);
    }
    set rgb(v) {
        this._hsv = rgb2hsv(v);
    }
    getPixel(x, y) {
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
    setPixel(x, y, color) {
        if (x < 0 || this.canvas.width <= x || y < 0 || this.canvas.height <= y) {
            return;
        }
        const index = (~~y * this.canvas.width + ~~x) * 4;
        this.imageData.data[index + 0] = color[0];
        this.imageData.data[index + 1] = color[1];
        this.imageData.data[index + 2] = color[2];
        this.imageData.data[index + 3] = 255;
    }
    xorPixel(x, y) {
        if (x < 0 || this.canvas.width <= x || y < 0 || this.canvas.height <= y) {
            return;
        }
        const index = (~~y * this.canvas.width + ~~x) * 4;
        this.imageData.data[index + 0] = 255 ^ this.imageData.data[index + 0];
        this.imageData.data[index + 1] = 255 ^ this.imageData.data[index + 1];
        this.imageData.data[index + 2] = 255 ^ this.imageData.data[index + 2];
    }
    drawInvBox(x, y, w, h) {
        for (let yy = 0; yy < h; yy++) {
            this.xorPixel(x + 0, y + yy);
            this.xorPixel(x + w - 1, y + yy);
        }
        for (let xx = 1; xx < w - 1; xx++) {
            this.xorPixel(x + xx, y + 0);
            this.xorPixel(x + xx, y + h - 1);
        }
    }
    drawHCircle() {
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
    drawSVBox() {
        for (let iy = 0; iy < this.svBoxSize; iy++) {
            const v = (this.svBoxSize - 1 - iy) / (this.svBoxSize - 1);
            for (let ix = 0; ix < this.svBoxSize; ix++) {
                const s = ix / (this.svBoxSize - 1);
                const col = hsv2rgb([this._hsv[0], s, v]);
                this.setPixel(ix + ~~((this.canvas.width - this.svBoxSize) / 2), iy + ~~((this.canvas.height - this.svBoxSize) / 2), col);
            }
        }
    }
    drawHCursor() {
        const rd = -this._hsv[0] * Math.PI / 180;
        const xx = this.wheelRadius.min + (this.wheelRadius.max - this.wheelRadius.min) / 2;
        const yy = 0;
        const x = ~~(xx * Math.cos(rd) - yy * Math.sin(rd) + this.canvas.width / 2);
        const y = ~~(xx * Math.sin(rd) + yy * Math.cos(rd) + this.canvas.height / 2);
        this.drawInvBox(x - 4, y - 4, 9, 9);
    }
    getHValueFromPos(x0, y0) {
        const x = x0 - this.canvas.width / 2;
        const y = y0 - this.canvas.height / 2;
        const h = (~~(-Math.atan2(y, x) * 180 / Math.PI) + 360) % 360;
        const r = ~~Math.sqrt(x * x + y * y);
        return (r >= this.wheelRadius.min && r < this.wheelRadius.max) ? h : undefined;
    }
    drawSVCursor() {
        const left = (this.canvas.width - this.svBoxSize) / 2;
        const top = (this.canvas.height - this.svBoxSize) / 2;
        this.drawInvBox(left + ~~(this._hsv[1] * this.svBoxSize) - 4, top + ~~((1 - this._hsv[2]) * this.svBoxSize) - 4, 9, 9);
    }
    getSVValueFromPos(x0, y0) {
        const x = ~~(x0 - (this.canvas.width - this.svBoxSize) / 2);
        const y = ~~(y0 - (this.canvas.height - this.svBoxSize) / 2);
        return (0 <= x && x < this.svBoxSize && 0 <= y && y < this.svBoxSize) ? [x / (this.svBoxSize - 1), (this.svBoxSize - 1 - y) / (this.svBoxSize - 1)] : undefined;
    }
    updateImage() {
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
    draw(context, x, y) {
        context.drawImage(this.canvas, x, y);
    }
    touch(x, y) {
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
        }
        else {
            return false;
        }
    }
}
///////////////////////////////////////////////////////////////
var Events;
(function (Events) {
    class SingleEmitter {
        constructor() {
            this.listeners = [];
        }
        clear() {
            this.listeners.length = 0;
            return this;
        }
        on(listener) {
            this.listeners.splice(0, 0, listener);
            return this;
        }
        off(listener) {
            const index = this.listeners.indexOf(listener);
            if (index !== -1) {
                this.listeners.splice(index, 1);
            }
            return this;
        }
        fire(...args) {
            const temp = this.listeners.slice();
            for (const dispatcher of temp) {
                if (dispatcher.apply(this, args)) {
                    return true;
                }
            }
            ;
            return false;
        }
        one(listener) {
            const func = (...args) => {
                const result = listener.apply(this, args);
                this.off(func);
                return result;
            };
            this.on(func);
            return this;
        }
    }
    class EventEmitter {
        constructor() {
            this.listeners = new Map();
        }
        on(eventName, listener) {
            if (!this.listeners.has(eventName)) {
                this.listeners.set(eventName, new SingleEmitter());
            }
            this.listeners.get(eventName).on(listener);
            return this;
        }
        off(eventName, listener) {
            this.listeners.get(eventName).off(listener);
            return this;
        }
        fire(eventName, ...args) {
            if (this.listeners.has(eventName)) {
                const dispatcher = this.listeners.get(eventName);
                return dispatcher.fire.apply(dispatcher, args);
            }
            return false;
        }
        one(eventName, listener) {
            if (!this.listeners.has(eventName)) {
                this.listeners.set(eventName, new SingleEmitter());
            }
            this.listeners.get(eventName).one(listener);
            return this;
        }
        hasEventListener(eventName) {
            return this.listeners.has(eventName);
        }
        clearEventListener(eventName) {
            if (this.listeners.has(eventName)) {
                this.listeners.get(eventName).clear();
            }
            return this;
        }
    }
    Events.EventEmitter = EventEmitter;
})(Events || (Events = {}));
///////////////////////////////////////////////////////////////
var GUI;
(function (GUI) {
    /**
     * コントロールコンポーネントインタフェース
     */
    class UIEvent {
        constructor(name) {
            this.eventName = name;
            this.propagationStop = false;
            this.defaultPrevented = false;
        }
        get name() {
            return this.eventName;
        }
        preventDefault() {
            this.defaultPrevented = true;
        }
        stopPropagation() {
            this.propagationStop = true;
        }
        get propagationStopped() {
            return this.propagationStop;
        }
    }
    GUI.UIEvent = UIEvent;
    class UIMouseEvent extends UIEvent {
        constructor(name, x, y) {
            super(name);
            this.x = x;
            this.y = y;
        }
    }
    GUI.UIMouseEvent = UIMouseEvent;
    class UISwipeEvent extends UIEvent {
        constructor(name, dx, dy, x, y) {
            super(name);
            this.x = x;
            this.y = y;
            this.dx = dx;
            this.dy = dy;
        }
    }
    GUI.UISwipeEvent = UISwipeEvent;
    function installClickDelecate(ui) {
        const hookHandler = (ev) => {
            const x = ev.x;
            const y = ev.y;
            ev.preventDefault();
            ev.stopPropagation();
            let dx = 0;
            let dy = 0;
            const onPointerMoveHandler = (ev) => {
                dx += Math.abs(ev.x - x);
                dy += Math.abs(ev.y - y);
                ev.preventDefault();
                ev.stopPropagation();
            };
            const onPointerUpHandler = (ev) => {
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
    GUI.installClickDelecate = installClickDelecate;
    // UIに対するスワイプ操作を捕捉
    function installSwipeDelegate(ui) {
        const hookHandler = (ev) => {
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
            const onPointerMoveHandler = (ev) => {
                let dx = (~~ev.x - ~~x);
                let dy = (~~ev.y - ~~y);
                x = ev.x;
                y = ev.y;
                ui.postEvent(new UISwipeEvent("swipe", dx, dy, x, y));
                ev.preventDefault();
                ev.stopPropagation();
                return;
            };
            const onPointerUpHandler = (ev) => {
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
    GUI.installSwipeDelegate = installSwipeDelegate;
    // スワイプorタップ
    function installSwipeOrTapDelegate(ui) {
        const hookHandler = (ev) => {
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
            const onPointerMoveHandler = (ev) => {
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
            const onPointerUpHandler = (ev) => {
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
    GUI.installSwipeOrTapDelegate = installSwipeOrTapDelegate;
    // スワイプorタップorロングタップ
    function installSwipeOrTapOrLongTapDelegate(ui) {
        const hookHandler = (ev) => {
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
            const onPointerMoveHandler = (ev) => {
                let dx = (~~ev.x - ~~x);
                let dy = (~~ev.y - ~~y);
                mx += Math.abs(dx);
                my += Math.abs(dy);
                if (mx + my > 5) {
                    if (isTap) {
                        isTap = false;
                        window.clearTimeout(longTapDetectTimerHandle);
                        longTapDetectTimerHandle = 0;
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
            const onPointerUpHandler = (ev) => {
                window.clearTimeout(longTapDetectTimerHandle);
                longTapDetectTimerHandle = 0;
                root.removeEventListener("pointermove", onPointerMoveHandler, true);
                root.removeEventListener("pointerup", onPointerUpHandler, true);
                if (isTap) {
                    ui.dispatchEvent(new UIMouseEvent("click", ev.x, ev.y));
                }
                else {
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
    GUI.installSwipeOrTapOrLongTapDelegate = installSwipeOrTapOrLongTapDelegate;
    class Control {
        constructor({ left = 0, top = 0, width = 0, height = 0, visible = true, enable = true }) {
            this.left = left;
            this.top = top;
            this.width = width;
            this.height = height;
            this.visible = visible;
            this.enable = enable;
            this.parent = null;
            this.childrens = [];
            this.captureListeners = new Map();
            this.bubbleListeners = new Map();
        }
        get globalPos() {
            let x = this.left;
            let y = this.top;
            let p = this.parent;
            while (p) {
                x += p.left;
                y += p.top;
                p = p.parent;
            }
            return { x: x, y: y };
        }
        get root() {
            let root = this.parent;
            while (root.parent) {
                root = root.parent;
            }
            return root;
        }
        draw(context) {
            context.translate(+this.left, +this.top);
            const len = this.childrens.length;
            for (let i = len - 1; i >= 0; i--) {
                this.childrens[i].draw(context);
            }
            context.translate(-this.left, -this.top);
        }
        addEventListener(event, handler, capture = false) {
            const target = (capture) ? this.captureListeners : this.bubbleListeners;
            if (!target.has(event)) {
                target.set(event, []);
            }
            target.get(event).push(handler);
        }
        removeEventListener(event, handler, capture = false) {
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
        dodraw(context) {
            this.draw(context);
        }
        enumEventTargets(ret) {
            if (!this.visible || !this.enable) {
                return;
            }
            ret.push(this);
            for (let child of this.childrens) {
                child.enumEventTargets(ret);
            }
            return;
        }
        enumMouseEventTargets(ret, x, y) {
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
        postEvent(event, ...args) {
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
                    if (event.propagationStopped) {
                        return;
                    }
                }
            }
        }
        dispatchEvent(event, ...args) {
            const chain = [];
            (event instanceof UIMouseEvent) ? this.enumMouseEventTargets(chain, event.x, event.y) : this.enumEventTargets(chain);
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
                        if (event.propagationStopped) {
                            return;
                        }
                    }
                }
            }
        }
        addChild(child) {
            child.parent = this;
            this.childrens.push(child);
        }
        removeChild(child) {
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
        isHit(x, y) {
            const { x: dx, y: dy } = this.globalPos;
            return (dx <= x && x < dx + this.width) && (dy <= y && y < dy + this.height);
        }
    }
    GUI.Control = Control;
    class TextBox extends Control {
        constructor({ left = 0, top = 0, width = 0, height = 0, text = "", edgeColor = `rgb(128,128,128)`, color = `rgb(255,255,255)`, font = undefined, fontColor = `rgb(0,0,0)`, textAlign = "left", textBaseline = "top", visible = true, enable = true }) {
            super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
            this.text = text;
            this.edgeColor = edgeColor;
            this.color = color;
            this.font = font;
            this.fontColor = fontColor;
            this.textAlign = textAlign;
            this.textBaseline = textBaseline;
        }
        draw(context) {
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
    GUI.TextBox = TextBox;
    class Window extends Control {
        constructor({ left = 0, top = 0, width = 0, height = 0, text = "", edgeColor = Button.defaultValue.edgeColor, color = Button.defaultValue.color, font = Button.defaultValue.font, fontColor = Button.defaultValue.fontColor, textAlign = Button.defaultValue.textAlign, textBaseline = Button.defaultValue.textBaseline, visible = Button.defaultValue.visible, enable = Button.defaultValue.enable, }) {
            super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
            this.text = text;
            this.edgeColor = edgeColor;
            this.color = color;
            this.font = font;
            this.fontColor = fontColor;
            this.textAlign = textAlign;
            this.textBaseline = textBaseline;
            this.addEventListener("swipe", (event) => {
                this.left += event.dx;
                this.top += event.dy;
            });
            installSwipeDelegate(this);
            this.addEventListener("pointerdown", (ev) => {
                const index = this.parent.childrens.indexOf(this);
                if (index != 0) {
                    this.parent.childrens.splice(index, 1);
                    this.parent.childrens.unshift(this);
                }
            }, true);
        }
        draw(context) {
            context.fillStyle = this.color;
            context.fillRect(this.left, this.top, this.width, this.height);
            context.strokeStyle = this.edgeColor;
            context.lineWidth = 1;
            context.strokeRect(this.left, this.top, this.width, this.height);
            context.font = this.font;
            context.fillStyle = this.fontColor;
            const text = (this.text instanceof Function) ? this.text.call(this) : this.text;
            context.textAlign = this.textAlign;
            context.textBaseline = this.textBaseline;
            context.fillTextBox(text, this.left + 1, this.top + 1, this.width - 2, this.height - 2);
            super.draw(context);
        }
    }
    Window.defaultValue = {
        edgeColor: `rgb(12,34,98)`,
        color: `rgb(24,133,196)`,
        font: "10px monospace",
        fontColor: `rgb(255,255,255)`,
        textAlign: "left",
        textBaseline: "top",
        visible: true,
        enable: true,
    };
    GUI.Window = Window;
    class Button extends Control {
        constructor({ left = 0, top = 0, width = 0, height = 0, text = "", edgeColor = Button.defaultValue.edgeColor, color = Button.defaultValue.color, font = Button.defaultValue.font, fontColor = Button.defaultValue.fontColor, textAlign = Button.defaultValue.textAlign, textBaseline = Button.defaultValue.textBaseline, visible = Button.defaultValue.visible, enable = Button.defaultValue.enable, disableEdgeColor = Button.defaultValue.disableEdgeColor, disableColor = Button.defaultValue.disableColor, disableFontColor = Button.defaultValue.disableFontColor, }) {
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
        draw(context) {
            context.fillStyle = this.enable ? this.color : this.disableColor;
            context.fillRect(this.left, this.top, this.width, this.height);
            context.strokeStyle = this.enable ? this.edgeColor : this.disableEdgeColor;
            context.lineWidth = 1;
            context.strokeRect(this.left, this.top, this.width, this.height);
            context.font = this.font;
            context.fillStyle = this.enable ? this.fontColor : this.disableFontColor;
            const text = (this.text instanceof Function) ? this.text.call(this) : this.text;
            context.textAlign = this.textAlign;
            context.textBaseline = this.textBaseline;
            context.fillTextBox(text, this.left + 1, this.top + 1, this.width - 2, this.height - 2);
            super.draw(context);
        }
    }
    Button.defaultValue = {
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
    GUI.Button = Button;
    class Label extends Control {
        constructor({ left = 0, top = 0, width = 0, height = 0, text = "", edgeColor = Label.defaultValue.edgeColor, color = Label.defaultValue.color, font = Label.defaultValue.font, fontColor = Label.defaultValue.fontColor, textAlign = Label.defaultValue.textAlign, textBaseline = Label.defaultValue.textBaseline, visible = Label.defaultValue.visible, enable = Label.defaultValue.enable, disableEdgeColor = Label.defaultValue.disableEdgeColor, disableColor = Label.defaultValue.disableColor, disableFontColor = Label.defaultValue.disableFontColor, }) {
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
        draw(context) {
            context.fillStyle = this.enable ? this.color : this.disableColor;
            context.fillRect(this.left, this.top, this.width, this.height);
            context.strokeStyle = this.enable ? this.edgeColor : this.disableEdgeColor;
            context.lineWidth = 1;
            context.strokeRect(this.left, this.top, this.width, this.height);
            context.font = this.font;
            context.fillStyle = this.enable ? this.fontColor : this.disableFontColor;
            const text = (this.text instanceof Function) ? this.text.call(this) : this.text;
            context.textAlign = this.textAlign;
            context.textBaseline = this.textBaseline;
            context.fillTextBox(text, this.left + 1, this.top + 1, this.width - 2, this.height - 2);
            super.draw(context);
        }
    }
    Label.defaultValue = {
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
    GUI.Label = Label;
    class ImageButton extends Control {
        constructor({ left = 0, top = 0, width = 0, height = 0, texture = null, texLeft = 0, texTop = 0, texWidth = 0, texHeight = 0, visible = true, enable = true }) {
            super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
            this.texture = texture;
            this.texLeft = texLeft;
            this.texTop = texTop;
            this.texWidth = texWidth;
            this.texHeight = texHeight;
            installClickDelecate(this);
        }
        draw(context) {
            if (this.texture != null) {
                context.drawImage(this.texture, this.texLeft, this.texTop, this.texWidth, this.texHeight, this.left, this.top, this.width, this.height);
            }
        }
    }
    GUI.ImageButton = ImageButton;
    class ListBox extends Control {
        constructor({ left = 0, top = 0, width = 0, height = 0, lineHeight = 12, drawItem = () => { }, getItemCount = () => 0, visible = true, enable = true, scrollbarWidth = 1, space = 2 }) {
            super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
            this.insertCandPos = null;
            this.lineHeight = lineHeight;
            this.drawItem = drawItem;
            this.getItemCount = getItemCount;
            this.scrollValue = 0;
            this.scrollbarWidth = scrollbarWidth;
            this.space = space;
            this.addEventListener("swipe", (event) => {
                this.scrollValue -= event.dy;
                this.update();
            });
            const mp = {
                x: 0, y: 0
            };
            let animationTimer = 0;
            let animationPrev = 0;
            let dragItemIndex = -1;
            this.addEventListener("longswipestart", (event) => {
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
                    }
                    else if (mp.y > gp.y + this.height) {
                        this.scrollValue += scrollv;
                        this.update();
                    }
                    // 挿入候補位置を算出
                    const cursorY = ~~((mp.y - gp.y) + this.scrollValue);
                    const hoverItemIndex = ~~((cursorY - this.space) / (this.lineHeight + this.space));
                    if (cursorY > (hoverItemIndex + 0.5) * (this.lineHeight + this.space)) {
                        this.insertCandPos = hoverItemIndex + 1;
                    }
                    else {
                        this.insertCandPos = hoverItemIndex;
                    }
                    const itemCount = this.getItemCount();
                    if (this.insertCandPos < 0) {
                        this.insertCandPos = 0;
                    }
                    if (this.insertCandPos >= itemCount) {
                        this.insertCandPos = itemCount;
                    }
                    //console.log(this.insertCandPos);
                    animationTimer = requestAnimationFrame(longswipeHandler);
                };
                longswipeHandler();
                this.update();
            });
            this.addEventListener("longswipe", (event) => {
                mp.x = event.x;
                mp.y = event.y;
            });
            this.addEventListener("longswipeend", (event) => {
                cancelAnimationFrame(animationTimer);
                animationTimer = 0;
                if (dragItemIndex != -1 && this.insertCandPos != null) {
                    this.dragItem.call(this, dragItemIndex, this.insertCandPos);
                }
                this.insertCandPos = null;
                this.update();
            });
            this.addEventListener("click", (event) => {
                this.click.call(this, event);
            });
            installSwipeOrTapOrLongTapDelegate(this);
        }
        contentHeight() {
            const itemCount = this.getItemCount();
            if (itemCount === 0) {
                return 0;
            }
            else {
                return (this.lineHeight + this.space) * (itemCount) + this.space;
            }
        }
        update() {
            const contentHeight = this.contentHeight();
            if (this.height >= contentHeight) {
                this.scrollValue = 0;
            }
            else if (this.scrollValue < 0) {
                this.scrollValue = 0;
            }
            else if (this.scrollValue > (contentHeight - this.height)) {
                this.scrollValue = contentHeight - this.height;
            }
        }
        draw(context) {
            this.update();
            const scrollValue = ~~this.scrollValue;
            let sy = -(scrollValue % (this.lineHeight + this.space)) + this.space;
            let index = ~~(scrollValue / (this.lineHeight + this.space));
            let itemCount = this.getItemCount();
            let drawResionHeight = this.height - sy;
            context.fillStyle = "rgba(255,255,255,0.25)";
            context.fillRect(this.left, this.top, this.width, this.height);
            // 要素描画
            for (;;) {
                if (sy >= this.height) {
                    break;
                }
                if (this.insertCandPos == index) {
                    context.save();
                    context.fillStyle = "rgba(255,0,0,1)";
                    context.fillRect(this.left, this.top + sy - this.space, this.width - this.scrollbarWidth, this.space);
                    context.restore();
                }
                if (index >= itemCount) {
                    break;
                }
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
        getItemIndexByPosition(x, y) {
            this.update();
            if (x < 0 || this.width <= x || y < 0 || this.height <= y) {
                return -1;
            }
            const index = ~~((y + this.scrollValue - this.space) / (this.lineHeight + this.space));
            if (index < 0 || index >= this.getItemCount()) {
                return -1;
            }
            else {
                return index;
            }
        }
    }
    GUI.ListBox = ListBox;
    class HorizontalSlider extends Control {
        constructor({ left = 0, top = 0, width = 0, height = 0, edgeColor = `rgb(128,128,128)`, color = `rgb(192,192,255)`, bgColor = `rgb(192,192,192)`, font = undefined, fontColor = `rgb(0,0,0)`, textAlign = "center", textBaseline = "middle", text = (x) => `${x}`, minValue = 0, maxValue = 0, visible = true, enable = true, }) {
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
            this.addEventListener("swipe", (event) => {
                const { x: l, y: t } = this.globalPos;
                const x = event.x - l;
                const yy = event.y - t;
                const rangeSize = this.maxValue - this.minValue;
                if (rangeSize == 0) {
                    this.value = this.minValue;
                }
                else {
                    if (x <= 0) {
                        this.value = this.minValue;
                    }
                    else if (x >= this.width) {
                        this.value = this.maxValue;
                    }
                    else {
                        this.value = Math.trunc((x * rangeSize) / this.width) + this.minValue;
                    }
                }
                this.postEvent(new GUI.UIEvent("changed"));
                event.preventDefault();
                event.stopPropagation();
            });
        }
        draw(context) {
            context.fillStyle = this.bgColor;
            context.fillRect(this.left, this.top, this.width, this.height);
            context.fillStyle = this.color;
            context.fillRect(this.left, this.top, ~~(this.width * (this.value - this.minValue) / (this.maxValue - this.minValue)), this.height);
            const text = this.text(this.value);
            context.fillStyle = this.fontColor;
            context.textAlign = this.textAlign;
            context.textBaseline = this.textBaseline;
            //context.globalCompositeOperation = "xor"
            context.fillText(text, this.left + ~~(this.width / 2), this.top + ~~(this.height / 2));
            //context.globalCompositeOperation = "source-over"
            context.strokeStyle = 'rgb(0,0,0)';
            context.strokeRect(this.left, this.top, this.width, this.height);
        }
        update() {
            const rangeSize = this.maxValue - this.minValue;
            if (rangeSize == 0) {
                this.value = this.minValue;
            }
            else if (this.value < this.minValue) {
                this.value = this.minValue;
            }
            else if (this.value >= this.maxValue) {
                this.value = this.maxValue;
            }
        }
    }
    GUI.HorizontalSlider = HorizontalSlider;
})(GUI || (GUI = {}));
///////////////////////////////////////////////////////////////
class HSVColorWheelUI extends GUI.Control {
    get rgb() {
        const r = this.hsvColorWhell.rgb;
        return r;
    }
    set rgb(value) {
        this.hsvColorWhell.rgb = value;
    }
    constructor({ left = 0, top = 0, width = 0, height = 0, visible = true, enable = true }) {
        super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
        const radiusMax = ~~Math.min(width / 2, height / 2);
        const radiusMin = ~~Math.max(radiusMax - 20, radiusMax * 0.8);
        this.hsvColorWhell = new HSVColorWheel({ width: width, height: height, wheelRadiusMin: radiusMin, wheelRadiusMax: radiusMax, svBoxSize: radiusMax });
        GUI.installSwipeDelegate(this);
        this.addEventListener("swipe", (event) => {
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
    draw(context) {
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
class SolidPen {
    constructor(name, shader, compositMode) {
        this.name = name;
        this.joints = [];
        this.shader = shader;
        this.compositMode = compositMode;
    }
    down(config, point, currentLayer) {
        this.joints.length = 0;
        this.joints.push(point.x, point.y);
    }
    move(config, point, currentLayer) {
        this.joints.push(point.x, point.y);
    }
    up(config, point, currentLayer) {
    }
    draw(config, ip, imgData, finish) {
        ip.drawLines(imgData, {
            program: this.shader,
            vertexes: new Float32Array(this.joints),
            color: config.penColor,
            size: config.penSize,
            antialiasSize: config.antialiasSize
        });
    }
}
///////////////////////////////////////////////////////////////
class CorrectionSolidPen extends SolidPen {
    // http://jsdo.it/kikuchy/zsWz
    movingAverage(m, start) {
        const ret = { x: 0, y: 0 };
        for (let i = 0; i < m; i++) {
            ret.x += this.joints[start * 2 + 0];
            ret.y += this.joints[start * 2 + 1];
            start++;
        }
        ret.x = ~~(ret.x / m);
        ret.y = ~~(ret.y / m);
        return ret;
    }
    ;
    constructor(name, shader, compositMode) {
        super(name, shader, compositMode);
    }
    draw(config, ip, imgData, finish) {
        if (~~(this.joints.length / 2) <= config.penCorrectionValue) {
            ip.drawLines(imgData, {
                program: this.shader,
                vertexes: new Float32Array(this.joints),
                color: config.penColor,
                size: config.penSize,
                antialiasSize: config.antialiasSize
            });
        }
        else {
            const corrected = [];
            let prev = this.movingAverage(config.penCorrectionValue, 0);
            corrected.push(prev.x, prev.y);
            const len = ~~(this.joints.length / 2) - config.penCorrectionValue;
            for (let i = 1; i < len; i++) {
                const avg = this.movingAverage(config.penCorrectionValue, i);
                corrected.push(avg.x, avg.y);
                prev = avg;
            }
            ip.drawLines(imgData, {
                program: this.shader,
                vertexes: new Float32Array(corrected),
                color: config.penColor,
                size: config.penSize,
                antialiasSize: config.antialiasSize
            });
        }
    }
}
///////////////////////////////////////////////////////////////
class RectanglePen {
    constructor(name, borderShader, fillShader, compositMode) {
        this.name = name;
        this.borderShader = borderShader;
        this.fillShader = fillShader;
        this.compositMode = compositMode;
        this.start = null;
        this.end = null;
    }
    down(config, point, currentLayer) {
        this.start = point;
        this.end = point;
    }
    move(config, point, currentLayer) {
        this.end = point;
    }
    up(config, point, currentLayer) {
    }
    draw(config, ip, imgData, finish) {
        ip.drawLines(imgData, {
            program: this.borderShader,
            vertexes: new Float32Array([this.start.x, this.start.y, this.end.x, this.start.y, this.end.x, this.end.y, this.start.x, this.end.y, this.start.x, this.start.y]),
            color: config.penColor,
            size: config.penSize,
            antialiasSize: config.antialiasSize
        });
        ip.fillRect(imgData, {
            program: this.fillShader,
            start: this.start,
            end: this.end,
            color: config.penColor,
        });
    }
}
///////////////////////////////////////////////////////////////
class FloodFill {
    constructor(name, copyShader, compositMode) {
        this.name = name;
        this.copyShader = copyShader;
        this.compositMode = compositMode;
        this.surface = null;
    }
    down(config, point, currentLayer) {
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
        const fillcolor = config.penColor;
        const i = (y * width + x) * 4;
        const targetcolor = [input[i], input[i + 1], input[i + 2], input[i + 3]];
        const q = [i];
        while (q.length) {
            const j = q.pop();
            if (checked[~~(j / 4)] == 0) {
                if (input[j + 0] == targetcolor[0] && input[j + 1] == targetcolor[1] && input[j + 2] == targetcolor[2] && input[j + 3] == targetcolor[3]) {
                    output.data[j + 0] = fillcolor[0];
                    output.data[j + 1] = fillcolor[1];
                    output.data[j + 2] = fillcolor[2];
                    output.data[j + 3] = fillcolor[3];
                    if (j >= width * 4) {
                        q.push(j - width * 4);
                    }
                    if (j < length - width * 4) {
                        q.push(j + width * 4);
                    }
                    if (j >= 4) {
                        q.push(j - 4);
                    }
                    if (j < length - 4) {
                        q.push(j + 4);
                    }
                }
                checked[~~(j / 4)] = 1;
            }
        }
        this.surface = new Surface(currentLayer.imageData.gl, output);
    }
    move(config, point, currentLayer) {
    }
    up(config, point, currentLayer) {
    }
    draw(config, ip, imgData, finish) {
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
;
var CompositMode;
(function (CompositMode) {
    CompositMode[CompositMode["Normal"] = 0] = "Normal";
    CompositMode[CompositMode["Erase"] = 1] = "Erase";
})(CompositMode || (CompositMode = {}));
///////////////////////////////////////////////////////////////
class CustomPointerEvent extends CustomEvent {
}
///////////////////////////////////////////////////////////////
var ModalDialog;
(function (ModalDialog) {
    let dialogLayerCanvas;
    let dialogLayerContext;
    let draw;
    let blockCnt = 0;
    function resize() {
        const displayWidth = dialogLayerCanvas.clientWidth;
        const displayHeight = dialogLayerCanvas.clientHeight;
        if (dialogLayerCanvas.width !== displayWidth || dialogLayerCanvas.height !== displayHeight) {
            dialogLayerCanvas.width = displayWidth;
            dialogLayerCanvas.height = displayHeight;
        }
    }
    function init() {
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
    ModalDialog.init = init;
    function block() {
        if (blockCnt == 0) {
            dialogLayerCanvas.style.display = "inline";
            dialogLayerCanvas.width = dialogLayerCanvas.clientWidth;
            dialogLayerCanvas.height = dialogLayerCanvas.clientHeight;
            Input.pause = true;
        }
        blockCnt++;
    }
    ModalDialog.block = block;
    function unblock() {
        if (blockCnt > 0) {
            blockCnt--;
            if (blockCnt == 0) {
                dialogLayerCanvas.style.display = "none";
                Input.pause = false;
            }
        }
    }
    ModalDialog.unblock = unblock;
    function alert(caption) {
        block();
        draw = (canvas, context) => {
            const left = ~~((canvas.width - 200) / 2);
            const top = ~~((canvas.height - 100) / 2);
            context.fillStyle = "rgb(255,255,255)";
            context.fillRect(left, top, 200, 100);
        };
        draw(dialogLayerCanvas, dialogLayerContext);
        dialogLayerCanvas.style.display = "inline";
        const click = (ev) => {
            const left = ~~((dialogLayerCanvas.width - 200) / 2);
            const top = ~~((dialogLayerCanvas.height - 100) / 2);
            if (left <= ev.pageX && ev.pageX < left + 200 && top <= ev.pageY && ev.pageY < top + 100) {
                dialogLayerCanvas.removeEventListener("click", click, true);
                unblock();
            }
            ev.preventDefault();
            ev.stopPropagation();
        };
        dialogLayerCanvas.addEventListener("click", click, true);
    }
    ModalDialog.alert = alert;
})(ModalDialog || (ModalDialog = {}));
///////////////////////////////////////////////////////////////
var Input;
(function (Input) {
    let eventEmitter = new Events.EventEmitter();
    Input.on = eventEmitter.on.bind(eventEmitter);
    Input.off = eventEmitter.off.bind(eventEmitter);
    Input.one = eventEmitter.one.bind(eventEmitter);
    let isScrolling = false;
    let timeout = -1;
    let sDistX = 0;
    let sDistY = 0;
    let maybeClick = false;
    let maybeClickX = 0;
    let maybeClickY = 0;
    let prevTimeStamp = 0;
    let prevInputType = "";
    Input.pause = false;
    function init() {
        if (!window.TouchEvent) {
            console.log("TouchEvent is not supported by your browser.");
            window.TouchEvent = function () { };
        }
        if (!window.PointerEvent) {
            console.log("PointerEvent is not supported by your browser.");
            window.PointerEvent = function () { };
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
            document.addEventListener('pointerdown', (ev) => ev.preventDefault() && (Input.pause || eventEmitter.fire('pointerdown', ev)));
            document.addEventListener('pointermove', (ev) => ev.preventDefault() && (Input.pause || eventEmitter.fire('pointermove', ev)));
            document.addEventListener('pointerup', (ev) => ev.preventDefault() && (Input.pause || eventEmitter.fire('pointerup', ev)));
            document.addEventListener('pointerleave', (ev) => ev.preventDefault() && (Input.pause || eventEmitter.fire('pointerleave', ev)));
        }
        else {
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
        document.addEventListener("wheel", (...args) => Input.pause || eventEmitter.fire("wheel", ...args));
    }
    Input.init = init;
    function checkEvent(e) {
        e.preventDefault();
        if (Input.pause) {
            return false;
        }
        const istouch = e instanceof TouchEvent || (e instanceof PointerEvent && e.pointerType === "touch");
        const ismouse = e instanceof MouseEvent || ((e instanceof PointerEvent && (e.pointerType === "mouse" || e.pointerType === "pen")));
        if (istouch && prevInputType !== "touch") {
            if (e.timeStamp - prevTimeStamp >= 500) {
                prevInputType = "touch";
                prevTimeStamp = e.timeStamp;
                return true;
            }
            else {
                return false;
            }
        }
        else if (ismouse && prevInputType !== "mouse") {
            if (e.timeStamp - prevTimeStamp >= 500) {
                prevInputType = "mouse";
                prevTimeStamp = e.timeStamp;
                return true;
            }
            else {
                return false;
            }
        }
        else {
            prevInputType = istouch ? "touch" : ismouse ? "mouse" : "none";
            prevTimeStamp = e.timeStamp;
            return istouch || ismouse;
        }
    }
    function pointerDown(e) {
        if (checkEvent(e)) {
            const evt = makePointerEvent("down", e);
            const singleFinger = (e instanceof MouseEvent) || (e instanceof TouchEvent && e.touches.length === 1);
            if (!isScrolling && singleFinger) {
                maybeClick = true;
                maybeClickX = evt.pageX;
                maybeClickY = evt.pageY;
            }
        }
        return false;
    }
    function pointerLeave(e) {
        if (checkEvent(e)) {
            maybeClick = false;
            makePointerEvent("leave", e);
        }
        return false;
    }
    function pointerMove(e) {
        if (checkEvent(e)) {
            makePointerEvent("move", e);
        }
        return false;
    }
    function pointerUp(e) {
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
    function makePointerEvent(type, e) {
        const evt = document.createEvent("CustomEvent");
        const eventType = `pointer${type}`;
        evt.initCustomEvent(eventType, true, true, e);
        evt.touch = e.type.indexOf("touch") === 0;
        evt.mouse = e.type.indexOf("mouse") === 0;
        if (evt.touch) {
            const touchEvent = e;
            evt.pointerId = touchEvent.changedTouches[0].identifier;
            evt.pageX = touchEvent.changedTouches[0].pageX;
            evt.pageY = touchEvent.changedTouches[0].pageY;
        }
        if (evt.mouse) {
            const mouseEvent = e;
            evt.pointerId = 0;
            evt.pageX = mouseEvent.clientX + window.pageXOffset;
            evt.pageY = mouseEvent.clientY + window.pageYOffset;
        }
        evt.maskedEvent = e;
        eventEmitter.fire(eventType, evt);
        return evt;
    }
})(Input || (Input = {}));
///////////////////////////////////////////////////////////////
var Painter;
(function (Painter) {
    ;
    let parentHtmlElement = null;
    let uiDispacher = null;
    /**
     * UI描画用キャンバス
     */
    let uiCanvas = null;
    let uiContext = null;
    /**
     * 画像描画用キャンバス（非表示）
     */
    let imageCanvas = null;
    let imageContext = null;
    let glCanvas = null;
    let glContext = null;
    let imageProcessing = null;
    /**
     * レイヤー結合結果
     */
    let imageLayerCompositedSurface = null;
    /**
     * 各レイヤー
     */
    let imageCurrentLayer = -1;
    let imageLayers = [];
    /**
     * 作業レイヤー
     */
    let workLayer = null;
    function createLayer() {
        const previewCanvas = document.createElement("canvas");
        previewCanvas.width = previewCanvas.height = 50;
        const previewContext = previewCanvas.getContext("2d");
        const previewData = previewContext.createImageData(50, 50);
        return {
            imageData: imageProcessing.createBlankOffscreenTarget(imageCanvas.width, imageCanvas.height),
            previewCanvas: previewCanvas,
            previewContext: previewContext,
            previewData: previewData,
            compositMode: CompositMode.Normal
        };
    }
    function disposeLayer(self) {
        self.imageData.dispose();
        self.imageData = null;
        self.previewCanvas = null;
        self.previewContext = null;
        self.previewData = null;
    }
    function UpdateLayerPreview(layer) {
        const sw = layer.imageData.width;
        const sh = layer.imageData.height;
        const pw = layer.previewData.width;
        const ph = layer.previewData.height;
        const dstData = layer.previewData.data;
        let dst = 0;
        const pb = new PixelBuffer(layer.imageData.gl, sw, sh);
        const srcData = pb.capture(layer.imageData);
        for (let y = 0; y < ph; y++) {
            const sy = ~~(y * sh / ph);
            for (let x = 0; x < pw; x++) {
                const sx = ~~(x * sw / pw);
                dstData[dst + 0] = srcData[(sy * sw + sx) * 4 + 0];
                dstData[dst + 1] = srcData[(sy * sw + sx) * 4 + 1];
                dstData[dst + 2] = srcData[(sy * sw + sx) * 4 + 2];
                dstData[dst + 3] = srcData[(sy * sw + sx) * 4 + 3];
                dst += 4;
            }
        }
        pb.dispose();
        layer.previewContext.putImageData(layer.previewData, 0, 0);
        update({ gui: true });
        return layer;
    }
    /**
     * 作画画面ビュー用
     */
    let viewCanvas = null;
    let viewContext = null;
    /**
     * オーバーレイ用（矩形選択など描画とは関係ないガイド線などを描画する）
     */
    let overlayCanvas = null;
    let overlayContext = null;
    let canvasOffsetX = 0;
    let canvasOffsetY = 0;
    let tools = [];
    let currentTool = null;
    Painter.config = {
        scale: 1,
        penSize: 5,
        penCorrectionValue: 10,
        penColor: [0, 0, 0, 64],
        antialiasSize: 2,
        scrollX: 0,
        scrollY: 0,
    };
    let updateRequest = { overlay: false, view: false, gui: false };
    let updateTimerId = NaN;
    //let penShader: Program = null;
    let alphaPenShader = null;
    let normalBlendShader = null;
    let eraserBlendShader = null;
    let copyShader = null;
    let checkerBoardShader = null;
    let alphaFillShader = null;
    function init(parentHtmlElement, width, height) {
        Input.init();
        ModalDialog.init();
        parentHtmlElement = parentHtmlElement;
        imageCanvas = document.createElement("canvas");
        imageCanvas.width = width;
        imageCanvas.height = height;
        glContext = imageCanvas.getContext('webgl2');
        imageProcessing = new ImageProcessing(glContext);
        imageLayers = [createLayer()];
        imageCurrentLayer = 0;
        imageLayerCompositedSurface = imageProcessing.createBlankOffscreenTarget(width, height);
        workLayer = createLayer();
        viewCanvas = document.createElement("canvas");
        viewCanvas.style.position = "absolute";
        viewCanvas.style.left = "0";
        viewCanvas.style.top = "0";
        viewCanvas.style.width = "100%";
        viewCanvas.style.height = "100%";
        viewContext = viewCanvas.getContext("2d");
        viewContext.imageSmoothingEnabled = false;
        parentHtmlElement.appendChild(viewCanvas);
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
        tools.push(new SolidPen("SolidPen", alphaPenShader, CompositMode.Normal), new CorrectionSolidPen("SolidPen2", alphaPenShader, CompositMode.Normal), new SolidPen("Eraser", alphaPenShader, CompositMode.Erase), new RectanglePen("Rect", alphaPenShader, alphaFillShader, CompositMode.Normal), new FloodFill("Fill", copyShader, CompositMode.Normal));
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
                ret[3] = Painter.config.penColor[3];
                Painter.config.penColor = ret;
                update({ gui: true });
            };
            uiHSVColorWheel.rgb = Painter.config.penColor;
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
            uiAlphaSlider.value = Painter.config.penColor[3];
            uiAlphaSlider.addEventListener("changed", () => {
                Painter.config.penColor[3] = uiAlphaSlider.value;
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
                text: () => [`scale = ${Painter.config.scale}`, `penSize = ${Painter.config.penSize}`, `antialiasSize = ${Painter.config.antialiasSize}`, `penColor = [${Painter.config.penColor.join(",")}]`].join("\n"),
            });
            uiInfoWindow.addChild(uiInfoWindowTitle);
            uiInfoWindow.addChild(uiInfoWindowBody);
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
                Painter.config.penSize = uiPenSizeSlider.value;
                update({ gui: true });
            });
            uiPenSizeSlider.value = Painter.config.penSize;
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
                Painter.config.antialiasSize = uiAntialiasSizeSlider.value;
                update({ gui: true });
            });
            uiAntialiasSizeSlider.value = Painter.config.antialiasSize;
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
                Painter.config.penCorrectionValue = uiPenCorrectionSlider.value;
                update({ gui: true });
            });
            uiPenCorrectionSlider.value = Painter.config.penCorrectionValue;
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
            const toolButtons = [];
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
                const config = {
                    width: imageCanvas.width,
                    height: imageCanvas.height,
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
                    .then((blob) => {
                    saveFileToLocal("savedata.zip", blob);
                    ModalDialog.unblock();
                }, (reson) => {
                    ModalDialog.unblock();
                    ModalDialog.alert("save failed.");
                });
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
                            const config = JSON.parse(data);
                            if (config == null) {
                                return Promise.reject("config.json is invalid");
                            }
                            if (config.layers.every(x => zip.files[`layers/${x.image}`] != null) == false) {
                                return Promise.reject("layer data not found.");
                            }
                            return Promise.all(config.layers.map(x => zip.files[`layers/${x.image}`].async("arraybuffer").then(img => loadFromBmp(imageProcessing, img, { reqWidth: config.width, reqHeight: config.height })))).then(datas => {
                                if (datas.some(x => x == null)) {
                                    return Promise.reject("layer data is invalid.");
                                }
                                else {
                                    imageCanvas.width = config.width;
                                    imageCanvas.height = config.height;
                                    imageLayers.forEach(disposeLayer);
                                    imageLayers.length = 0;
                                    for (let i = 0; i < datas.length; i++) {
                                        const layer = createLayer();
                                        imageLayers.push(layer);
                                        layer.imageData = (datas[i]);
                                        layer.compositMode = config.layers[i].compositMode;
                                        UpdateLayerPreview(layer);
                                    }
                                    imageCurrentLayer = 0;
                                    imageLayerCompositedSurface.dispose();
                                    imageLayerCompositedSurface = imageProcessing.createBlankOffscreenTarget(width, height);
                                    disposeLayer(workLayer);
                                    workLayer = createLayer();
                                    updateCompositLayer();
                                    update({ gui: true, view: true, overlay: true });
                                    return Promise.resolve();
                                }
                            });
                        });
                    });
                }).then(() => {
                    ModalDialog.unblock();
                }, (reson) => {
                    ModalDialog.unblock();
                    ModalDialog.alert("load failed.");
                });
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
                const deleted = imageLayers.splice(imageCurrentLayer, 1);
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
                drawItem: (context, left, top, width, height, item) => {
                    if (item == imageCurrentLayer) {
                        context.fillStyle = `rgb(24,196,195)`;
                    }
                    else {
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
            uiLayerListBox.click = (ev) => {
                const { x: gx, y: gy } = uiLayerListBox.globalPos;
                const x = ev.x - gx;
                const y = ev.y - gy;
                const select = uiLayerListBox.getItemIndexByPosition(x, y);
                imageCurrentLayer = select == -1 ? imageCurrentLayer : select;
                update({ gui: true });
            };
            uiLayerListBox.dragItem = (from, to) => {
                if (from == to || from + 1 == to) {
                    // 動かさない
                }
                else {
                    const target = imageLayers[from];
                    imageLayers.splice(from, 1);
                    if (from < to) {
                        to -= 1;
                    }
                    imageLayers.splice(to, 0, target);
                    updateCompositLayer();
                    update({ gui: true, view: true });
                }
            };
            const orgUpdate = uiLayerListBox.update.bind(uiLayerListBox);
            uiLayerListBox.update = function () {
                orgUpdate();
                update({ gui: true });
            };
            uiLayerWindow.addChild(uiLayerListBox);
            uiLayerWindow.height = uiLayerListBox.top + uiLayerListBox.height;
            uiDispacher.addChild(uiLayerWindow);
        }
        resize();
    }
    Painter.init = init;
    function getCompositShader(mode) {
        if (mode == CompositMode.Erase) {
            return eraserBlendShader;
        }
        else {
            return normalBlendShader;
        }
    }
    function updateCompositLayer() {
        imageLayerCompositedSurface.clear();
        imageProcessing.applyShaderUpdate(imageLayerCompositedSurface, { program: checkerBoardShader });
        for (let i = imageLayers.length - 1; i >= 0; i--) {
            if (i == imageCurrentLayer) {
                imageProcessing.compositUpdate(imageLayers[i].imageData, workLayer.imageData, getCompositShader(workLayer.compositMode), true);
                imageProcessing.compositUpdate(imageLayerCompositedSurface, workLayer.imageData, normalBlendShader);
            }
            else {
                imageProcessing.compositUpdate(imageLayerCompositedSurface, imageLayers[i].imageData, normalBlendShader);
            }
        }
    }
    function setupMouseEvent() {
        Input.on("pointerdown", (e) => {
            e.preventDefault();
            const p = pointToClient({ x: e.pageX, y: e.pageY });
            const uiev = new GUI.UIMouseEvent("pointerdown", p.x, p.y);
            uiDispacher.dispatchEvent(uiev);
            if (uiev.defaultPrevented) {
                update({ gui: true });
                return true;
            }
            if ((e.mouse && e.detail.button === 0) ||
                (e.touch && e.detail.touches.length <= 2)) {
                if ((e.mouse && e.detail.ctrlKey) || (e.touch && e.detail.touches.length == 2)) {
                    let scrollStartPos = pointToClient({ x: e.pageX, y: e.pageY });
                    const onScrolling = (e) => {
                        e.preventDefault();
                        const p = pointToClient({ x: e.pageX, y: e.pageY });
                        Painter.config.scrollX += p.x - scrollStartPos.x;
                        Painter.config.scrollY += p.y - scrollStartPos.y;
                        scrollStartPos = p;
                        update({ view: true });
                        return true;
                    };
                    const onScrollEnd = (e) => {
                        e.preventDefault();
                        Input.off("pointermove", onScrolling);
                        const p = pointToClient({ x: e.pageX, y: e.pageY });
                        Painter.config.scrollX += p.x - scrollStartPos.x;
                        Painter.config.scrollY += p.y - scrollStartPos.y;
                        scrollStartPos = p;
                        update({ view: true });
                        return true;
                    };
                    Input.on("pointermove", onScrolling);
                    Input.one("pointerup", onScrollEnd);
                    return true;
                }
                else {
                    const onPenMove = (e) => {
                        if (currentTool) {
                            const p = pointToCanvas({ x: e.pageX, y: e.pageY });
                            currentTool.move(Painter.config, p, imageLayers[imageCurrentLayer]);
                            workLayer.imageData.clear();
                            workLayer.compositMode = currentTool.compositMode;
                            currentTool.draw(Painter.config, imageProcessing, workLayer.imageData, false);
                            updateCompositLayer();
                            update({ view: true });
                        }
                        return true;
                    };
                    const onPenUp = (e) => {
                        Input.off("pointermove", onPenMove);
                        if (currentTool) {
                            const p = pointToCanvas({ x: e.pageX, y: e.pageY });
                            currentTool.up(Painter.config, p, imageLayers[imageCurrentLayer]);
                            workLayer.imageData.clear();
                            currentTool.draw(Painter.config, imageProcessing, workLayer.imageData, true);
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
                        currentTool.down(Painter.config, p, imageLayers[imageCurrentLayer]);
                        workLayer.imageData.clear();
                        workLayer.compositMode = currentTool.compositMode;
                        currentTool.draw(Painter.config, imageProcessing, workLayer.imageData, false);
                        updateCompositLayer();
                        update({ view: true });
                    }
                    return true;
                }
            }
            return false;
        });
        Input.on("pointermove", (e) => {
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
        Input.on("pointerup", (e) => {
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
        Input.on("wheel", (e) => {
            e.preventDefault();
            if (e.ctrlKey) {
                if (e.deltaY < 0) {
                    if (Painter.config.scale < 16) {
                        Painter.config.scrollX = (Painter.config.scrollX * (Painter.config.scale + 1) / (Painter.config.scale));
                        Painter.config.scrollY = (Painter.config.scrollY * (Painter.config.scale + 1) / (Painter.config.scale));
                        Painter.config.scale += 1;
                        canvasOffsetX = ~~((viewCanvas.width - imageCanvas.width * Painter.config.scale) / 2);
                        canvasOffsetY = ~~((viewCanvas.height - imageCanvas.height * Painter.config.scale) / 2);
                        update({ overlay: true, view: true, gui: true });
                        return true;
                    }
                }
                else if (e.deltaY > 0) {
                    if (Painter.config.scale > 1) {
                        Painter.config.scrollX = (Painter.config.scrollX * (Painter.config.scale - 1) / (Painter.config.scale));
                        Painter.config.scrollY = (Painter.config.scrollY * (Painter.config.scale - 1) / (Painter.config.scale));
                        Painter.config.scale -= 1;
                        canvasOffsetX = ~~((viewCanvas.width - imageCanvas.width * Painter.config.scale) / 2);
                        canvasOffsetY = ~~((viewCanvas.height - imageCanvas.height * Painter.config.scale) / 2);
                        update({ overlay: true, view: true, gui: true });
                        return true;
                    }
                }
            }
            return false;
        });
    }
    function resizeCanvas(canvas) {
        const displayWidth = canvas.clientWidth;
        const displayHeight = canvas.clientHeight;
        if (canvas.width !== displayWidth || canvas.height !== displayHeight) {
            canvas.width = displayWidth;
            canvas.height = displayHeight;
            return true;
        }
        else {
            return false;
        }
    }
    function resize() {
        const ret1 = resizeCanvas(viewCanvas);
        const ret2 = resizeCanvas(overlayCanvas);
        const ret3 = resizeCanvas(uiCanvas);
        if (ret1 || ret2) {
            canvasOffsetX = ~~((viewCanvas.width - imageCanvas.width * Painter.config.scale) / 2);
            canvasOffsetY = ~~((viewCanvas.height - imageCanvas.height * Painter.config.scale) / 2);
        }
        update({ view: (ret1 || ret2), gui: ret3 });
    }
    Painter.resize = resize;
    function pointToClient(point) {
        const cr = viewCanvas.getBoundingClientRect();
        const sx = (point.x - (cr.left + window.pageXOffset));
        const sy = (point.y - (cr.top + window.pageYOffset));
        return { x: sx, y: sy };
    }
    Painter.pointToClient = pointToClient;
    function pointToCanvas(point) {
        const p = pointToClient(point);
        p.x = ~~((p.x - canvasOffsetX - Painter.config.scrollX) / Painter.config.scale);
        p.y = ~~((p.y - canvasOffsetY - Painter.config.scrollY) / Painter.config.scale);
        return p;
    }
    Painter.pointToCanvas = pointToCanvas;
    function update({ overlay = false, view = false, gui = false }) {
        updateRequest.overlay = updateRequest.overlay || overlay;
        updateRequest.view = updateRequest.view || view;
        updateRequest.gui = updateRequest.gui || gui;
        if (isNaN(updateTimerId)) {
            updateTimerId = requestAnimationFrame(() => {
                if (updateRequest.overlay) {
                    updateRequest.overlay = false;
                }
                if (updateRequest.view) {
                    imageProcessing.applyShader(null, imageLayerCompositedSurface, { program: copyShader });
                    viewContext.clearRect(0, 0, viewCanvas.width, viewCanvas.height);
                    viewContext.fillStyle = "rgb(198,208,224)";
                    viewContext.fillRect(0, 0, viewCanvas.width, viewCanvas.height);
                    viewContext.fillStyle = "rgb(255,255,255)";
                    viewContext.shadowOffsetX = viewContext.shadowOffsetY = 0;
                    viewContext.shadowColor = "rgb(0,0,0)";
                    viewContext.shadowBlur = 10;
                    viewContext.fillRect(canvasOffsetX + Painter.config.scrollX, canvasOffsetY + Painter.config.scrollY, imageCanvas.width * Painter.config.scale, imageCanvas.height * Painter.config.scale);
                    viewContext.shadowBlur = 0;
                    viewContext.imageSmoothingEnabled = false;
                    viewContext.drawImage(imageCanvas, 0, 0, imageCanvas.width, imageCanvas.height, canvasOffsetX + Painter.config.scrollX, canvasOffsetY + Painter.config.scrollY, imageCanvas.width * Painter.config.scale, imageCanvas.height * Painter.config.scale);
                    updateRequest.view = false;
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
    Painter.update = update;
})(Painter || (Painter = {}));
///////////////////////////////////////////////////////////////
window.addEventListener("load", () => {
    Painter.init(document.body, 512, 512);
});
//# sourceMappingURL=tspaint.js.map