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
    element.click();
    document.body.removeChild(element);
}
function loadFileFromLocal(accept) {
    return new Promise((resolve, reject) => {
        const element = document.createElement("input");
        element.type = "file";
        element.style.display = "none";
        element.accept = accept;
        document.body.appendChild(element);
        element.onchange = (ev) => {
            element.onchange = null;
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
        element.click();
        document.body.removeChild(element);
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
var CompositMode;
(function (CompositMode) {
    CompositMode[CompositMode["Normal"] = 0] = "Normal";
    CompositMode[CompositMode["Add"] = 1] = "Add";
    CompositMode[CompositMode["Sub"] = 2] = "Sub";
    CompositMode[CompositMode["SubAlpha"] = 3] = "SubAlpha";
    CompositMode[CompositMode["Mul"] = 4] = "Mul";
    CompositMode[CompositMode["Screen"] = 5] = "Screen";
})(CompositMode || (CompositMode = {}));
ImageData.prototype.clear = function () {
    this.data.fill(0);
};
ImageData.prototype.pointSet = function (x0, y0, [r, g, b, a]) {
    if (0 <= x0 && x0 < this.width && 0 <= y0 && y0 < this.height) {
        const off = (y0 * this.width + x0) * 4;
        this.data[off + 0] = r;
        this.data[off + 1] = g;
        this.data[off + 2] = b;
        this.data[off + 3] = a;
    }
};
ImageData.prototype.putMask = function (left, top, mask, w, h, color) {
    const dst = { left: left, top: top, right: left + w, bottom: top + h };
    const src = { left: 0, top: 0, right: w, bottom: h };
    // clipping 
    if (dst.left < 0) {
        src.left += (-dst.left);
        dst.left = 0;
    }
    if (dst.right >= this.width) {
        src.right -= (this.width - dst.right);
        dst.right = this.width;
    }
    if (dst.top < 0) {
        src.top += (-dst.top);
        dst.top = 0;
    }
    if (dst.bottom >= this.height) {
        src.bottom -= (this.height - dst.bottom);
        dst.bottom = this.height;
    }
    const width = dst.right - dst.left;
    const height = dst.bottom - dst.top;
    const dstData = this.data;
    const [r, g, b, a] = color;
    let offDst = (dst.top * this.width + dst.left) * 4;
    const stepDst = this.width * 4;
    let offSrc = (src.top) * w + src.left;
    const stepSrc = w;
    for (let y = 0; y < height; y++) {
        let scanDst = offDst;
        offDst += stepDst;
        let scanSrc = offSrc;
        offSrc += stepSrc;
        for (let x = 0; x < width; x++) {
            if (mask[scanSrc]) {
                dstData[scanDst + 0] = r;
                dstData[scanDst + 1] = g;
                dstData[scanDst + 2] = b;
                dstData[scanDst + 3] = a;
            }
            scanSrc += 1;
            scanDst += 4;
        }
    }
};
ImageData.prototype.composition = function (src) {
    // precheck
    if (this.width != src.imageData.width || this.height != src.imageData.height) {
        throw new Error("size missmatch");
    }
    // operation
    const dataLen = this.height * this.width * 4;
    const dstData = this.data;
    const srcData = src.imageData.data;
    switch (src.compositMode) {
        case CompositMode.Normal:
            for (let j = 0; j < dataLen; j += 4) {
                const sr = srcData[j + 0];
                const sg = srcData[j + 1];
                const sb = srcData[j + 2];
                const sa = srcData[j + 3] / 255;
                const dr = dstData[j + 0];
                const dg = dstData[j + 1];
                const db = dstData[j + 2];
                const da = dstData[j + 3] / 255;
                const na = sa + da - (sa * da);
                const ra = ~~(na * 255 + 0.5);
                let rr = dr;
                let rg = dg;
                let rb = db;
                if (na > 0) {
                    const dasa = da * (1.0 - sa);
                    rr = ~~((sr * sa + dr * dasa) / na + 0.5);
                    rg = ~~((sg * sa + dg * dasa) / na + 0.5);
                    rb = ~~((sb * sa + db * dasa) / na + 0.5);
                }
                dstData[j + 0] = rr;
                dstData[j + 1] = rg;
                dstData[j + 2] = rb;
                dstData[j + 3] = ra;
            }
            break;
        case CompositMode.Add:
            break;
        case CompositMode.Mul:
            break;
        case CompositMode.Screen:
            break;
        case CompositMode.Sub:
            break;
        case CompositMode.SubAlpha:
            for (let j = 0; j < dataLen; j += 4) {
                const sa = srcData[j + 3];
                const da = dstData[j + 3];
                const na = (da * (255 - sa)) / 255;
                const ra = ~~(na + 0.5);
                dstData[j + 3] = ra;
            }
            break;
    }
};
///////////////////////////////////////////////////////////////
function saveAsBmp(imageData) {
    const bitmapData = new ArrayBuffer(14 + 40 + (imageData.width * imageData.height * 4)); // sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER) + sizeof(IMAGEDATA)
    //
    // BITMAPFILEHEADER
    //
    const viewOfBitmapFileHeader = new DataView(bitmapData, 0, 14);
    viewOfBitmapFileHeader.setUint16(0, 0x4D42, true); // bfType : 'BM'
    viewOfBitmapFileHeader.setUint32(2, 14 + 40 + (imageData.width * imageData.height * 4), true); // bfSize :  sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER) + sizeof(IMAGEDATA)
    viewOfBitmapFileHeader.setUint16(6, 0x0000, true); // bfReserved1 : 0
    viewOfBitmapFileHeader.setUint16(8, 0x0000, true); // bfReserved2 : 0
    viewOfBitmapFileHeader.setUint32(10, 14 + 40, true); // bfOffBits : sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER)
    //
    // BITMAPINFOHEADER
    //
    const viewOfBitmapInfoHeader = new DataView(bitmapData, 14, 40);
    viewOfBitmapInfoHeader.setUint32(0, 40, true); // biSize : sizeof(BITMAPINFOHEADER)
    viewOfBitmapInfoHeader.setUint32(4, imageData.width, true); // biWidth : data.width
    viewOfBitmapInfoHeader.setUint32(8, imageData.height, true); // biHeight : data.height
    viewOfBitmapInfoHeader.setUint16(12, 1, true); // biPlanes : 1
    viewOfBitmapInfoHeader.setUint16(14, 32, true); // biBitCount : 32
    viewOfBitmapInfoHeader.setUint32(16, 0, true); // biCompression : 0
    viewOfBitmapInfoHeader.setUint32(20, imageData.width * imageData.height * 4, true); // biSizeImage : imageData.width * imageData.height * 4
    viewOfBitmapInfoHeader.setUint32(24, 0, true); // biXPixPerMeter : 0
    viewOfBitmapInfoHeader.setUint32(28, 0, true); // biYPixPerMeter : 0
    viewOfBitmapInfoHeader.setUint32(32, 0, true); // biClrUsed : 0
    viewOfBitmapInfoHeader.setUint32(36, 0, true); // biCirImportant : 0
    //
    // IMAGEDATA
    //
    const viewOfBitmapPixelData = new DataView(bitmapData, 14 + 40, imageData.width * imageData.height * 4);
    for (let y = 0; y < imageData.height; y++) {
        let scan = (imageData.height - 1 - y) * imageData.width * 4;
        let base = y * imageData.width * 4;
        for (let x = 0; x < imageData.width; x++) {
            viewOfBitmapPixelData.setUint8(base + 0, imageData.data[scan + 2]); // B
            viewOfBitmapPixelData.setUint8(base + 1, imageData.data[scan + 1]); // G
            viewOfBitmapPixelData.setUint8(base + 2, imageData.data[scan + 0]); // R
            viewOfBitmapPixelData.setUint8(base + 3, imageData.data[scan + 3]); // A
            base += 4;
            scan += 4;
        }
    }
    return bitmapData;
}
;
function loadFromBmp(context, bitmapData, { reqWidth = -1, reqHeight = -1 }) {
    const dataLength = bitmapData.byteLength;
    //
    // BITMAPFILEHEADER
    //
    const reqSize = (reqWidth >= 0 && reqHeight >= 0) ? reqWidth * reqHeight * 4 : -1;
    const viewOfBitmapFileHeader = new DataView(bitmapData, 0, 14);
    const bfType = viewOfBitmapFileHeader.getUint16(0, true); // bfType : 'BM'
    const bfSize = viewOfBitmapFileHeader.getUint32(2, true); // bfSize :  sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER) + sizeof(IMAGEDATA)
    const bfReserved1 = viewOfBitmapFileHeader.getUint16(6, true); // bfReserved1 : 0
    const bfReserved2 = viewOfBitmapFileHeader.getUint16(8, true); // bfReserved2 : 0
    const bfOffBits = viewOfBitmapFileHeader.getUint32(10, true); // bfOffBits : sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER)
    if ((bfType != 0x4D42) ||
        (bfSize < 14 + 40) || (bfSize != dataLength) || (reqSize != -1 && bfSize < 14 + 40 + reqSize) ||
        (bfReserved1 != 0) ||
        (bfReserved2 != 0) ||
        (bfOffBits < 14 + 40) || (bfOffBits >= dataLength) || (reqSize != -1 && bfOffBits > dataLength - reqSize)) {
        return null;
    }
    //
    // BITMAPINFOHEADER
    //
    const viewOfBitmapInfoHeader = new DataView(bitmapData, 14, 40);
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
    if ((biSize != 40) ||
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
    const imageData = context.createImageData(biWidth, biHeight);
    //
    // IMAGEDATA
    //
    const viewOfBitmapPixelData = new DataView(bitmapData, bfOffBits, biWidth * biHeight * 4);
    for (let y = 0; y < biHeight; y++) {
        let scan = (biHeight - 1 - y) * biWidth * 4;
        let base = y * biWidth * 4;
        for (let x = 0; x < biWidth; x++) {
            imageData.data[scan + 2] = viewOfBitmapPixelData.getUint8(base + 0); // B
            imageData.data[scan + 1] = viewOfBitmapPixelData.getUint8(base + 1); // G
            imageData.data[scan + 0] = viewOfBitmapPixelData.getUint8(base + 2); // R
            imageData.data[scan + 3] = viewOfBitmapPixelData.getUint8(base + 3); // A
            base += 4;
            scan += 4;
        }
    }
    return imageData;
}
var IPoint;
(function (IPoint) {
    function rotate(point, rad) {
        const cos = Math.cos(rad);
        const sin = Math.sin(rad);
        const rx = point.x * cos - point.y * sin;
        const ry = point.x * sin + point.y * cos;
        return { x: rx, y: ry };
    }
    IPoint.rotate = rotate;
})(IPoint || (IPoint = {}));
///////////////////////////////////////////////////////////////
var Topology;
(function (Topology) {
    function drawCircle(x0, y0, radius, hline) {
        let x = radius - 1;
        let y = 0;
        let dx = 1;
        let dy = 1;
        let err = dx - (radius * 2);
        while (x >= y) {
            hline(x0 - x, x0 + x, y0 + y);
            hline(x0 - y, x0 + y, y0 + x);
            hline(x0 - y, x0 + y, y0 - x);
            hline(x0 - x, x0 + x, y0 - y);
            if (err <= 0) {
                y++;
                err += dy;
                dy += 2;
            }
            if (err > 0) {
                x--;
                dx += 2;
                err += dx - (radius << 1);
            }
        }
    }
    Topology.drawCircle = drawCircle;
    function drawLine({ x: x0, y: y0 }, { x: x1, y: y1 }, pset) {
        const dx = Math.abs(x1 - x0);
        const dy = Math.abs(y1 - y0);
        const sx = (x0 < x1) ? 1 : -1;
        const sy = (y0 < y1) ? 1 : -1;
        let err = dx - dy;
        for (;;) {
            pset(x0, y0);
            if (x0 === x1 && y0 === y1) {
                break;
            }
            const e2 = 2 * err;
            if (e2 > -dx) {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dy) {
                err += dx;
                y0 += sy;
            }
        }
    }
    Topology.drawLine = drawLine;
})(Topology || (Topology = {}));
///////////////////////////////////////////////////////////////
class HSVColorWheel {
    constructor({ width = 0, height = 0, wheelRadiusMin = 76, wheelRadiusMax = 96, svBoxSize = 100 }) {
        this._hsv = [0, 0, 0];
        this.canvas = document.createElement("canvas");
        this.canvas.width = width;
        this.canvas.height = height;
        this.context = this.canvas.getContext("2d");
        this.imageData = this.context.createImageData(this.canvas.width, this.canvas.height);
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
var Brushes;
(function (Brushes) {
    function createSolidBrush(imgData, color, size) {
        if (size <= 1) {
            return function (x0, y0) { imgData.pointSet(x0, y0, color); };
        }
        else {
            const w = (size * 2 - 1);
            const h = (size * 2 - 1);
            const r = size - 1;
            const mask = new Uint8Array(w * h);
            Topology.drawCircle(r, r, size, (x1, x2, y) => { for (let i = x1; i <= x2; i++) {
                mask[w * y + i] = 1;
            } });
            return function (x0, y0) {
                imgData.putMask(x0 - r, y0 - r, mask, w, h, color);
            };
        }
    }
    Brushes.createSolidBrush = createSolidBrush;
})(Brushes || (Brushes = {}));
///////////////////////////////////////////////////////////////
class HSVColorWheelUI extends GUI.Control {
    get rgb() {
        const r = this.hsvColorWhell.rgb;
        return r;
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
    constructor(name, compositMode) {
        this.name = name;
        this.joints = [];
        this.compositMode = compositMode;
    }
    down(config, point) {
        this.joints.length = 0;
        this.joints.push(point);
    }
    move(config, point) {
        this.joints.push(point);
    }
    up(config, point) {
    }
    draw(config, imgData) {
        const brush = Brushes.createSolidBrush(imgData, config.penColor, config.penSize);
        const len = this.joints.length;
        if (len === 1) {
            Topology.drawLine(this.joints[0], this.joints[0], brush);
        }
        else {
            for (let i = 1; i < len; i++) {
                Topology.drawLine(this.joints[i - 1], this.joints[i - 0], brush);
            }
        }
    }
}
///////////////////////////////////////////////////////////////
class CorrectionSolidPen extends SolidPen {
    // http://jsdo.it/kikuchy/zsWz
    movingAverage(m, start) {
        const ret = { x: 0, y: 0 };
        for (let i = 0; i < m; i++) {
            ret.x += this.joints[start].x;
            ret.y += this.joints[start].y;
            start++;
        }
        ret.x = ~~(ret.x / m);
        ret.y = ~~(ret.y / m);
        return ret;
    }
    ;
    constructor(name, compositMode) {
        super(name, compositMode);
    }
    down(config, point) {
        this.joints.length = 0;
        this.joints.push(point);
    }
    move(config, point) {
        this.joints.push(point);
    }
    up(config, point) {
    }
    draw(config, imgData) {
        const brush = Brushes.createSolidBrush(imgData, config.penColor, config.penSize);
        if (this.joints.length === 1) {
            Topology.drawLine(this.joints[0], this.joints[0], brush);
        }
        else if (this.joints.length <= config.penCorrectionValue) {
            for (let i = 1; i < this.joints.length; i++) {
                Topology.drawLine(this.joints[i - 1], this.joints[i - 0], brush);
            }
        }
        else {
            let prev = this.movingAverage(config.penCorrectionValue, 0);
            Topology.drawLine(this.joints[0], prev, brush);
            const len = this.joints.length - config.penCorrectionValue;
            for (let i = 1; i < len; i++) {
                const avg = this.movingAverage(config.penCorrectionValue, i);
                Topology.drawLine(prev, avg, brush);
                prev = avg;
            }
            Topology.drawLine(prev, this.joints[this.joints.length - 1], brush);
        }
    }
}
;
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
            const displayWidth = dialogLayerCanvas.clientWidth;
            const displayHeight = dialogLayerCanvas.clientHeight;
            if (dialogLayerCanvas.width !== displayWidth || dialogLayerCanvas.height !== displayHeight) {
                dialogLayerCanvas.width = displayWidth;
                dialogLayerCanvas.height = displayHeight;
            }
            if (dialogLayerCanvas.style.display != "none") {
                draw(dialogLayerCanvas, dialogLayerContext);
            }
        });
    }
    ModalDialog.init = init;
    function block() {
        if (blockCnt == 0) {
            dialogLayerCanvas.width = dialogLayerCanvas.clientWidth;
            dialogLayerCanvas.height = dialogLayerCanvas.clientHeight;
            Input.pause = true;
            dialogLayerCanvas.style.display = "inline";
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
    /**
     * レイヤー結合結果
     */
    let imageLayerCompositedImgData = null;
    /**
     * 各レイヤー
     */
    let currentLayer = -1;
    let imageLayerImgDatas = [];
    /**
     * 作業レイヤー
     */
    let workLayerImgData = null;
    function createLayer() {
        const previewCanvas = document.createElement("canvas");
        previewCanvas.width = previewCanvas.height = 50;
        const previewContext = previewCanvas.getContext("2d");
        const previewData = previewContext.createImageData(50, 50);
        return {
            imageData: imageContext.createImageData(imageCanvas.width, imageCanvas.height),
            previewCanvas: previewCanvas,
            previewContext: previewContext,
            previewData: previewData,
            compositMode: CompositMode.Normal
        };
    }
    function UpdateLayerPreview(layer) {
        const sw = layer.imageData.width;
        const sh = layer.imageData.height;
        const pw = layer.previewData.width;
        const ph = layer.previewData.height;
        const dstData = layer.previewData.data;
        let dst = 0;
        const srcData = layer.imageData.data;
        for (let y = 0; y < ph; y++) {
            const sy = ~~(y * sh / ph);
            for (let x = 0; x < ph; x++) {
                const sx = ~~(x * sw / pw);
                dstData[dst + 0] = srcData[(sy * sw + sx) * 4 + 0];
                dstData[dst + 1] = srcData[(sy * sw + sx) * 4 + 1];
                dstData[dst + 2] = srcData[(sy * sw + sx) * 4 + 2];
                dstData[dst + 3] = srcData[(sy * sw + sx) * 4 + 3];
                dst += 4;
            }
        }
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
    let tools = [
        new SolidPen("SolidPen", CompositMode.Normal),
        new CorrectionSolidPen("SolidPen2", CompositMode.Normal),
        new SolidPen("Eraser", CompositMode.SubAlpha)
    ];
    let currentTool = null;
    Painter.config = {
        scale: 1,
        penSize: 5,
        penCorrectionValue: 10,
        penColor: [0, 0, 0, 64],
        scrollX: 0,
        scrollY: 0,
    };
    let updateRequest = { overlay: false, view: false, gui: false };
    let updateTimerId = NaN;
    function init(parentHtmlElement, width, height) {
        Input.init();
        ModalDialog.init();
        parentHtmlElement = parentHtmlElement;
        currentLayer = 0;
        imageCanvas = document.createElement("canvas");
        imageCanvas.width = width;
        imageCanvas.height = height;
        imageContext = imageCanvas.getContext("2d");
        imageContext.imageSmoothingEnabled = false;
        imageLayerImgDatas = [createLayer()];
        imageLayerCompositedImgData = imageContext.createImageData(width, height);
        workLayerImgData = createLayer();
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
        //uiDispacher.addEventListener("update", () => { update({ gui: true }); return true; });
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
                text: () => [`scale = ${Painter.config.scale}`, `penSize = ${Painter.config.penSize}`, `penColor = [${Painter.config.penColor.join(",")}]`].join("\n"),
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
            uiPenConfigWindow.addChild(uiPenSizeSlider);
            const uiPenCorrectionSlider = new GUI.HorizontalSlider({
                left: uiPenConfigWindowTitle.left,
                top: uiPenSizeSlider.top + uiPenSizeSlider.height - 1,
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
                    for (var toolButton of toolButtons) {
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
                for (let i = 0; i < imageLayerImgDatas.length; i++) {
                    const layer = imageLayerImgDatas[i];
                    config.layers.push({ image: `${i}.bmp`, compositMode: layer.compositMode });
                }
                zip.file("config.json", JSON.stringify(config));
                const img = zip.folder("layers");
                for (let i = 0; i < imageLayerImgDatas.length; i++) {
                    const layer = imageLayerImgDatas[i];
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
                            return Promise.all(config.layers.map(x => zip.files[`layers/${x.image}`].async("arraybuffer").then(img => loadFromBmp(imageContext, img, { reqWidth: config.width, reqHeight: config.height })))).then(datas => {
                                if (datas.some(x => x == null)) {
                                    return Promise.reject("layer data is invalid.");
                                }
                                else {
                                    imageCanvas.width = config.width;
                                    imageCanvas.height = config.height;
                                    imageLayerImgDatas.length = 0;
                                    for (var i = 0; i < datas.length; i++) {
                                        const layer = createLayer();
                                        imageLayerImgDatas.push(layer);
                                        layer.imageData = (datas[i]);
                                        layer.compositMode = config.layers[i].compositMode;
                                        UpdateLayerPreview(layer);
                                    }
                                    currentLayer = 0;
                                    imageLayerCompositedImgData = imageContext.createImageData(width, height);
                                    workLayerImgData = createLayer();
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
                imageLayerImgDatas.splice(currentLayer, 0, createLayer());
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
                copiedLayer.compositMode = imageLayerImgDatas[currentLayer].compositMode;
                copiedLayer.imageData.data.set(imageLayerImgDatas[currentLayer].imageData.data);
                imageLayerImgDatas.splice(currentLayer, 0, copiedLayer);
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
                if (imageLayerImgDatas.length == 1) {
                    return;
                }
                imageLayerImgDatas.splice(currentLayer, 1);
                if (currentLayer == imageLayerImgDatas.length) {
                    currentLayer -= 1;
                }
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
                getItemCount: () => imageLayerImgDatas.length,
                drawItem: (context, left, top, width, height, item) => {
                    if (item == currentLayer) {
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
                    context.drawImage(imageLayerImgDatas[item].previewCanvas, 0, 0, 50, 50, left, top, 50, 50);
                    context.strokeRect(left, top, 50, 50);
                    context.strokeRect(left, top, width, height);
                }
            });
            uiLayerListBox.click = (ev) => {
                const { x: gx, y: gy } = uiLayerListBox.globalPos;
                const x = ev.x - gx;
                const y = ev.y - gy;
                const select = uiLayerListBox.getItemIndexByPosition(x, y);
                currentLayer = select == -1 ? currentLayer : select;
                update({ gui: true });
            };
            uiLayerListBox.dragItem = (from, to) => {
                if (from == to || from + 1 == to) {
                    // 動かさない
                }
                else {
                    const target = imageLayerImgDatas[from];
                    imageLayerImgDatas.splice(from, 1);
                    if (from < to) {
                        to -= 1;
                    }
                    imageLayerImgDatas.splice(to, 0, target);
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
    function updateCompositLayer() {
        imageLayerCompositedImgData.clear();
        for (let i = imageLayerImgDatas.length - 1; i >= 0; i--) {
            imageLayerCompositedImgData.composition(imageLayerImgDatas[i]);
            if (i == currentLayer) {
                imageLayerCompositedImgData.composition(workLayerImgData);
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
                            currentTool.move(Painter.config, p);
                            workLayerImgData.imageData.clear();
                            workLayerImgData.compositMode = currentTool.compositMode;
                            currentTool.draw(Painter.config, workLayerImgData.imageData);
                            updateCompositLayer();
                            update({ view: true });
                        }
                        return true;
                    };
                    const onPenUp = (e) => {
                        Input.off("pointermove", onPenMove);
                        if (currentTool) {
                            const p = pointToCanvas({ x: e.pageX, y: e.pageY });
                            currentTool.up(Painter.config, p);
                            workLayerImgData.imageData.clear();
                            workLayerImgData.compositMode = currentTool.compositMode;
                            currentTool.draw(Painter.config, workLayerImgData.imageData);
                            imageLayerImgDatas[currentLayer].imageData.composition(workLayerImgData);
                            workLayerImgData.imageData.clear();
                            UpdateLayerPreview(imageLayerImgDatas[currentLayer]);
                            updateCompositLayer();
                            update({ view: true });
                        }
                        return true;
                    };
                    if (currentTool) {
                        Input.on("pointermove", onPenMove);
                        Input.one("pointerup", onPenUp);
                        const p = pointToCanvas({ x: e.pageX, y: e.pageY });
                        currentTool.down(Painter.config, p);
                        workLayerImgData.imageData.clear();
                        workLayerImgData.compositMode = currentTool.compositMode;
                        currentTool.draw(Painter.config, workLayerImgData.imageData);
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
                    imageContext.putImageData(imageLayerCompositedImgData, 0, 0);
                    viewContext.clearRect(0, 0, viewCanvas.width, viewCanvas.height);
                    viewContext.fillStyle = "rgb(198,208,224)";
                    viewContext.fillRect(0, 0, viewCanvas.width, viewCanvas.height);
                    viewContext.fillStyle = "rgb(255,255,255)";
                    viewContext.shadowOffsetX = viewContext.shadowOffsetY = 0;
                    viewContext.shadowColor = "rgb(0,0,0)";
                    viewContext.shadowBlur = 10;
                    viewContext.fillRect(canvasOffsetX + Painter.config.scrollX, canvasOffsetY + Painter.config.scrollY, imageCanvas.width * Painter.config.scale, imageCanvas.height * Painter.config.scale);
                    viewContext.shadowBlur = 0;
                    viewContext.fillStyle = "rgb(224,224,224)";
                    const l = canvasOffsetX + Painter.config.scrollX;
                    const t = canvasOffsetY + Painter.config.scrollY;
                    const w = imageCanvas.width * Painter.config.scale;
                    const h = imageCanvas.height * Painter.config.scale;
                    const grid = 8;
                    const ww = ~~((w + grid - 1) / grid);
                    const hh = ~~((h + grid - 1) / grid);
                    for (let y = 0; y < hh; y++) {
                        for (let x = 0; x < ww; x++) {
                            if ((y & 1) != (x & 1)) {
                                viewContext.fillRect(l + x * grid, t + y * grid, grid, grid);
                            }
                        }
                    }
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