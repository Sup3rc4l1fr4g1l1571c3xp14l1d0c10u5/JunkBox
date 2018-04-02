/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.dom.d.ts" />
/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.es2016.d.ts" />
// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />

"use strict";

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
interface ImageData {
    clear: () => void;
    copyFrom: (imageData: ImageData) => void;
    composition: (src: { imageData: ImageData, compositMode: CompositMode }) => void;
    pointSet: (x0: number, y0: number, color: RGBA) => void;
    putMask: (left:number,top:number,mask:Uint8Array, w:number,h:number,color:RGBA) => void;
    saveAsBmp: () => ArrayBuffer;
}
enum CompositMode {
    Normal,
    Add,
    Sub,
    SubAlpha,
    Mul,
    Screen
}
ImageData.prototype.copyFrom = function (imageData: ImageData): void {
    if (this.width != imageData.width || this.height != imageData.height) {
        throw new Error("size missmatch")
    }
    this.data.set(imageData.data);
};
ImageData.prototype.clear = function (): void {
    this.data.fill(0);
};
ImageData.prototype.pointSet = function (x0: number, y0: number, color: RGBA): void {
    if (0 <= x0 && x0 < this.width && 0 <= y0 && y0 < this.height) {
        const off = (y0 * this.width + x0) * 4;
        this.data[off + 0] = color[0];
        this.data[off + 1] = color[1];
        this.data[off + 2] = color[2];
        this.data[off + 3] = color[3];
    }
};
ImageData.prototype.putMask = function(left: number, top: number, mask: Uint8Array, w: number, h: number, color: RGBA) {
    const dst = { left: left, top: top, right: left+w, bottom: top+h };
    const src = { left: 0, top: 0, right: w, bottom: h };

    if (dst.left < 0) {
        src.left += (-dst.left);
        dst.left = 0;
    }
    if (dst.right >= this.width) {
        src.right -= (this.width-dst.right);
        dst.right = this.width;
    } 
    if (dst.top < 0) {
        src.top += (-dst.top);
        dst.top = 0;
    }
    if (dst.bottom >= this.height) {
        src.bottom -= (this.height-dst.bottom);
        dst.bottom = this.height;
    }
    const width = dst.right - dst.left;
    const height = dst.bottom - dst.top;
    const dstData = this.data;

    for (let y = 0; y < height; y++) {
        let offDst = ((dst.top + y) * this.width + dst.left) * 4;
        let offSrc = (y + src.top) * w + src.left;
        for (let x = 0; x < width; x++) {
            if (mask[offSrc]) {
                dstData[offDst + 0] = color[0];
                dstData[offDst + 1] = color[1];
                dstData[offDst + 2] = color[2];
                dstData[offDst + 3] = color[3];
            }

            offSrc += 1;
            offDst += 4;
        }
    }
}


ImageData.prototype.composition = function (src: { imageData: ImageData, compositMode: CompositMode }): void {

    // precheck
    if (this.width != src.imageData.width || this.height != src.imageData.height) {
        throw new Error("size missmatch")
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
ImageData.prototype.saveAsBmp = function (): ArrayBuffer {

    const bitmapData: ArrayBuffer = new ArrayBuffer(14 + 40 + (this.width * this.height * 4)); // sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER) + sizeof(IMAGEDATA)

    //
    // BITMAPFILEHEADER
    //
    const viewOfBitmapFileHeader: DataView = new DataView(bitmapData, 0, 14);
    viewOfBitmapFileHeader.setUint16(0, 0x4D42, true);      // bfType : 'BM'
    viewOfBitmapFileHeader.setUint32(2, 14 + 40 + (this.width * this.height * 4), true);   // bfSize :  sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER) + sizeof(IMAGEDATA)
    viewOfBitmapFileHeader.setUint16(6, 0x0000, true);      // bfReserved1 : 0
    viewOfBitmapFileHeader.setUint16(8, 0x0000, true);      // bfReserved2 : 0
    viewOfBitmapFileHeader.setUint32(10, 14 + 40, true);    // bfOffBits : sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER)

    //
    // BITMAPINFOHEADER
    //
    const viewOfBitmapInfoHeader: DataView = new DataView(bitmapData, 14, 40);
    viewOfBitmapInfoHeader.setUint32(0, 40, true);          // biSize : sizeof(BITMAPINFOHEADER)
    viewOfBitmapInfoHeader.setUint32(4, this.width, true);  // biWidth : this.width
    viewOfBitmapInfoHeader.setUint32(8, this.height, true); // biHeight : this.height
    viewOfBitmapInfoHeader.setUint16(12, 1, true);         // biPlanes : 1
    viewOfBitmapInfoHeader.setUint16(14, 32, true);         // biBitCount : 32
    viewOfBitmapInfoHeader.setUint32(16, 0, true);         // biCompression : 0
    viewOfBitmapInfoHeader.setUint32(20, this.width * this.height * 4, true);         // biSizeImage : this.width * this.height * 4
    viewOfBitmapInfoHeader.setUint32(24, 0, true);         // biXPixPerMeter : 0
    viewOfBitmapInfoHeader.setUint32(28, 0, true);         // biYPixPerMeter : 0
    viewOfBitmapInfoHeader.setUint32(32, 0, true);         // biClrUsed : 0
    viewOfBitmapInfoHeader.setUint32(36, 0, true);         // biCirImportant : 0

    //
    // IMAGEDATA
    //
    const viewOfBitmapPixelData: DataView = new DataView(bitmapData, 54, this.width * this.height * 4);
    for (let y = 0; y < this.height; y++) {
        let scan = (this.height - 1 - y) * this.width * 4;
        let base = y * this.width * 4;
        for (let x = 0; x < this.width; x++) {
            viewOfBitmapPixelData.setUint8(base + 0, this.data[scan + 2]); // B
            viewOfBitmapPixelData.setUint8(base + 1, this.data[scan + 1]); // G
            viewOfBitmapPixelData.setUint8(base + 2, this.data[scan + 0]); // R
            viewOfBitmapPixelData.setUint8(base + 3, this.data[scan + 3]); // A
            base += 4;
            scan += 4;
        }
    }
    return bitmapData;

}
///////////////////////////////////////////////////////////////
interface IPoint {
    x: number;
    y: number;
}
namespace IPoint {
    export function rotate(point: IPoint, rad: number) {
        const cos = Math.cos(rad);
        const sin = Math.sin(rad);
        const rx = point.x * cos - point.y * sin;
        const ry = point.x * sin + point.y * cos;
        return { x: rx, y: ry };
    }
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
        this.imageData = this.context.createImageData(this.canvas.width, this.canvas.height);
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
            //this.imageData.data[index + 3],
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

namespace TsPaint {

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
                    mx += dx;
                    my += dy;
                    if (mx + my > 5) {
                        isTap = false;
                    }
                    if (isTap == false) {
                        x = ev.x;
                        y = ev.y;
                        dx = mx;
                        dy = my;
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

            public enumEventTargets(ret: Control[]): void {
                if (!this.visible || !this.enable) {
                    return;
                }
                ret.push(this);
                for (let child of this.childrens) {
                    child.enumEventTargets(ret);
                }
                return;
            }

            public enumMouseEventTargets(ret: Control[], x: number, y: number): boolean {
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
            public lineHeight: number;
            public scrollValue: number;
            public scrollbarWidth: number;
            public space: number;
            public click: (ev:UIMouseEvent) => void;

            public drawItem: (context:CanvasRenderingContext2D, left: number, top: number, width: number, height: number, item: number) => void;
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
                        drawItem?: (context:CanvasRenderingContext2D,left: number, top: number, width: number, height: number, item: number) => void,
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

                this.addEventListener("click", (event: UIMouseEvent) => {

                     this.click.call(this, event);
                })
                installSwipeOrTapDelegate(this);

            }

            private contentHeight(): number {
                const itemCount = this.getItemCount();
                if (itemCount === 0) {
                    return 0;
                } else {
                    return this.lineHeight + (this.lineHeight + this.space) * (itemCount - 1);
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
                let sy = -(scrollValue % (this.lineHeight + this.space));
                let index = ~~(scrollValue / (this.lineHeight + this.space));
                let itemCount = this.getItemCount();
                let drawResionHeight = this.height - sy;

                context.fillStyle = "rgba(255,255,255,0.25)";
                context.fillRect(this.left, this.top, this.width, this.height);

                for (; ;) {
                    if (sy >= this.height) { break; }
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
                const index = ~~((y + this.scrollValue) / (this.lineHeight + this.space));
                if (index < 0 || index >= this.getItemCount()) {
                    return -1;
                } else {
                    return index;
                }
            }
        }
        export class HorizontalSlider extends Control {
            public sliderWidth: number;
            public edgeColor: string;
            public color: string;
            public bgColor: string;
            public font: string;
            public fontColor: string;

            public value: number;
            public minValue: number;
            public maxValue: number;

            constructor(
                {
                    left = 0,
                    top = 0,
                    width = 0,
                    height = 0,
                    sliderWidth = 5,
                    edgeColor = `rgb(128,128,128)`,
                    color = `rgb(255,255,255)`,
                    bgColor = `rgb(192,192,192)`,
                    font = undefined,
                    fontColor = `rgb(0,0,0)`,
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
                        fontColor?: string;
                        minValue?: number;
                        maxValue?: number;
                        visible?: boolean;
                        enable?: boolean;
                    }

            ) {
                super({ left: left, top: top, width: width, height: height, visible: visible, enable: enable });
                this.sliderWidth = sliderWidth;
                this.edgeColor = edgeColor;
                this.color = color;
                this.bgColor = bgColor;
                this.font = font;
                this.fontColor = fontColor;
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
                        if (x <= this.sliderWidth / 2) {
                            this.value = this.minValue;
                        } else if (x >= this.width - this.sliderWidth / 2) {
                            this.value = this.maxValue;
                        } else {
                            const width = this.width - this.sliderWidth;
                            const xx = x - ~~(this.sliderWidth / 2);
                            this.value = Math.trunc((xx * rangeSize) / width) + this.minValue;
                        }
                    }
                    this.postEvent(new GUI.UIEvent("changed"));
                    event.preventDefault();
                    event.stopPropagation();
                });
            }
            draw(context: CanvasRenderingContext2D) {
                const lineWidth = this.width - this.sliderWidth;

                context.fillStyle = this.bgColor;
                context.fillRect(
                    this.left,
                    this.top,
                    this.width,
                    this.height
                );
                context.fillStyle = this.color;
                context.strokeStyle = this.edgeColor;
                context.fillRect(
                    this.left + ~~(lineWidth * (this.value - this.minValue) / (this.maxValue - this.minValue)),
                    this.top,
                    this.sliderWidth,
                    this.height
                );
                context.strokeRect(
                    this.left + ~~(lineWidth * (this.value - this.minValue) / (this.maxValue - this.minValue)),
                    this.top,
                    this.sliderWidth,
                    this.height
                );
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

    namespace Brushes {
        export type BrushPred = (x0: number, y0: number) => void;
        export function createSolidBrush(imgData: ImageData, color: RGBA, size: number): BrushPred {
            if (size <= 1) {
                return function (x0: number, y0: number): void { imgData.pointSet(x0, y0, color); };
            } else {
                const w = (size * 2 - 1);
                const h = (size * 2 - 1);
                const r = size - 1;
                const mask: Uint8Array = new Uint8Array(w * h);
                Topology.drawCircle(r, r, size, (x1, x2, y) => { for (let i = x1; i <= x2; i++) { mask[w * y + i] = 1; } });
                return function (x0: number, y0: number): void {
                    //let scan = 0;
                    imgData.putMask(x0-r, y0-r, mask, w, h, color);
                    //for (let yy = -r; yy <= r; yy++) {
                    //    for (let xx = -r; xx <= r; xx++) {
                    //        if (mask[scan++] != 0) {
                    //            imgData.pointSet(x0 + xx, y0 + yy, color);
                    //        }
                    //    }
                    //}
                };
            }
        }
    }

    namespace Topology {
        export function drawCircle(x0: number, y0: number, radius: number, hline: (x1: number, x2: number, y: number) => void): void {
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

        export function drawLine({ x: x0, y: y0 }: IPoint, { x: x1, y: y1 }: IPoint, pset: (x: number, y: number) => void): void {
            const dx: number = Math.abs(x1 - x0);
            const dy: number = Math.abs(y1 - y0);
            const sx: number = (x0 < x1) ? 1 : -1;
            const sy: number = (y0 < y1) ? 1 : -1;
            let err: number = dx - dy;

            for (; ;) {
                pset(x0, y0)
                if (x0 === x1 && y0 === y1) {
                    break;
                }
                const e2 = 2 * err;
                if (e2 > -dx) { err -= dy; x0 += sx; }
                if (e2 < dy) { err += dx; y0 += sy; }
            }
        }
    }

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

    interface Tool {
        name: string;
        compositMode: CompositMode;

        down(point: IPoint): void;
        move(point: IPoint): void;
        up(point: IPoint): void;
        draw(config: IPainterConfig, imgData: ImageData): void;

    }

    /**
     * ブロックペン
     */
    class SolidPen implements Tool {
        protected joints: IPoint[];

        public compositMode: CompositMode;
        public name: string;

        constructor(name: string, compositMode: CompositMode) {
            this.name = name;
            this.joints = [];
            this.compositMode = compositMode;
        }

        public down(point: IPoint): void {
            this.joints.length = 0;
            this.joints.push(point);
        }

        public move(point: IPoint): void {
            this.joints.push(point);
        }

        public up(point: IPoint): void {
        }

        public draw(config: IPainterConfig, imgData: ImageData): void {
            const brush = Brushes.createSolidBrush(imgData, config.penColor, config.penSize);
            if (this.joints.length === 1) {
                Topology.drawLine(this.joints[0], this.joints[0], brush);
            } else {
                for (let i = 1; i < this.joints.length; i++) {
                    Topology.drawLine(this.joints[i - 1], this.joints[i - 0], brush);
                }
            }
        }

    }
    class CorrectionSolidPen extends SolidPen {
        // http://jsdo.it/kikuchy/zsWz

        movingAverage(start: number): IPoint {
            const ret = { x: 0, y: 0 };
            for (let i = 0; i < CorrectionSolidPen.m; i++) {
                ret.x += this.joints[start].x;
                ret.y += this.joints[start].y;
                start++;
            }
            ret.x = ~~(ret.x / CorrectionSolidPen.m);
            ret.y = ~~(ret.y / CorrectionSolidPen.m);
            return ret;
        };
        static m: number = 10;

        constructor(name: string, compositMode: CompositMode) {
            super(name, compositMode);

        }

        down(point: IPoint) {
            this.joints.length = 0;
            this.joints.push(point);
        }

        move(point: IPoint) {
            this.joints.push(point);
        }

        public up(point: IPoint): void {
        }

        public draw(config: IPainterConfig, imgData: ImageData): void {
            const brush = Brushes.createSolidBrush(imgData, config.penColor, config.penSize);
            if (this.joints.length === 1) {
                Topology.drawLine(this.joints[0], this.joints[0], brush);
            } else if (this.joints.length <= CorrectionSolidPen.m) {
                for (let i = 1; i < this.joints.length; i++) {
                    Topology.drawLine(this.joints[i - 1], this.joints[i - 0], brush);
                }
            } else {
                let prev: IPoint = this.movingAverage(0);
                Topology.drawLine(this.joints[0],prev,  brush);
                const len = this.joints.length - CorrectionSolidPen.m;
                for (let i = 1; i < len ; i++) {
                    const avg = this.movingAverage(i);
                    Topology.drawLine(prev, avg, brush);
                    prev = avg;
                }
                Topology.drawLine(prev, this.joints[this.joints.length - 1], brush);
            }

        }
    }


    interface IPainterConfig {
        scale: number;
        penSize: number;
        penColor: RGBA;
        scrollX: number;
        scrollY: number;
    };

    interface Layer {
        imageData: ImageData;
        compositMode: CompositMode;
    }

    class Painter {
        private parentHtmlElement: HTMLElement;

        private eventEmitter: Events.EventEmitter

        private uiDispacher: GUI.Control;

        /**
         * UI描画用キャンバス
         */
        private uiCanvas: HTMLCanvasElement;
        private uiContext: CanvasRenderingContext2D;

        /**
         * 画像描画用キャンバス（非表示）
         */
        private imageCanvas: HTMLCanvasElement;
        private imageContext: CanvasRenderingContext2D;

        /**
         * レイヤー結合結果
         */
        private imageLayerCompositedImgData: ImageData;

        /**
         * 各レイヤー
         */
        private currentLayer: number;
        private imageLayerImgDatas: Layer[];

        /**
         * 作業レイヤー
         */
        private workLayerImgData: Layer;

        /**
         * 作画画面ビュー用
         */
        private viewCanvas: HTMLCanvasElement;
        private viewContext: CanvasRenderingContext2D;

        /**
         * オーバーレイ用（矩形選択など描画とは関係ないガイド線などを描画する）
         */
        private overlayCanvas: HTMLCanvasElement;
        private overlayContext: CanvasRenderingContext2D;

        private canvasOffsetX: number = 0;
        private canvasOffsetY: number = 0;

        private tools: Tool[] = [
            new SolidPen("SolidPen", CompositMode.Normal),
            new CorrectionSolidPen("SolidPen2", CompositMode.Normal),
            new SolidPen("Eraser", CompositMode.SubAlpha)
        ];

        private currentTool: Tool = null;

        private config: IPainterConfig = {
            scale: 1,
            penSize: 5,
            penColor: [0, 0, 0, 64],
            scrollX: 0,
            scrollY: 0,
        };

        private updateRequest: { overlay: boolean, view: boolean, gui: boolean };
        private updateTimerId: number = NaN;

        constructor(parentHtmlElement: HTMLElement, width: number, height: number) {
            this.parentHtmlElement = parentHtmlElement;

            this.eventEmitter = new Events.EventEmitter();

            this.currentLayer = 0;

            this.imageCanvas = document.createElement("canvas");
            this.imageCanvas.width = width;
            this.imageCanvas.height = height;
            this.imageContext = this.imageCanvas.getContext("2d");
            this.imageContext.imageSmoothingEnabled = false;
            this.imageLayerImgDatas = [
                { imageData: this.imageContext.createImageData(width, height), compositMode: CompositMode.Normal },
                { imageData: this.imageContext.createImageData(width, height), compositMode: CompositMode.Normal },
                ];
            this.imageLayerCompositedImgData = this.imageContext.createImageData(width, height);
            this.workLayerImgData = {
                imageData: this.imageContext.createImageData(width, height),
                compositMode: CompositMode.Normal
            };

            this.viewCanvas = document.createElement("canvas");
            this.viewCanvas.style.position = "absolute";
            this.viewCanvas.style.left = "0";
            this.viewCanvas.style.top = "0";
            this.viewCanvas.style.width = "100%";
            this.viewCanvas.style.height = "100%";
            this.viewContext = this.viewCanvas.getContext("2d");
            this.viewContext.imageSmoothingEnabled = false;
            this.parentHtmlElement.appendChild(this.viewCanvas);

            this.overlayCanvas = document.createElement("canvas");
            this.overlayCanvas.style.position = "absolute";
            this.overlayCanvas.style.left = "0";
            this.overlayCanvas.style.top = "0";
            this.overlayCanvas.style.width = "100%";
            this.overlayCanvas.style.height = "100%";
            this.overlayContext = this.overlayCanvas.getContext("2d");
            this.overlayContext.imageSmoothingEnabled = false;
            this.parentHtmlElement.appendChild(this.overlayCanvas);

            this.uiDispacher = new GUI.Control({});
            this.uiCanvas = document.createElement("canvas");
            this.uiCanvas.style.position = "absolute";
            this.uiCanvas.style.left = "0";
            this.uiCanvas.style.top = "0";
            this.uiCanvas.style.width = "100%";
            this.uiCanvas.style.height = "100%";
            this.uiContext = this.uiCanvas.getContext("2d");
            this.uiContext.imageSmoothingEnabled = false;
            this.parentHtmlElement.appendChild(this.uiCanvas);

            this.updateRequest = { gui: false, overlay: false, view: false };
            this.updateTimerId = NaN;

            this.uiDispacher.addEventListener("update", () => { this.update({ gui: true }); return true; });

            window.addEventListener("resize", () => this.resize());
            this.setupMouseEvent();

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
                    ret[3] = this.config.penColor[3];
                    this.config.penColor = <RGBA>ret;
                    this.update({ gui: true });
                }
                uiWheelWindow.addChild(uiHSVColorWheel);

                const uiAlphaSlider = new GUI.HorizontalSlider({
                    left: uiWindowLabel.left,
                    top: uiHSVColorWheel.top + uiHSVColorWheel.height - 1,
                    width: uiHSVColorWheel.width,
                    color: 'rgb(255,255,255)',
                    fontColor: 'rgb(0,0,0)',
                    height: 13,
                    minValue: 0,
                    maxValue: 255,
                });
                uiAlphaSlider.addEventListener("changed", () => {
                    this.config.penColor[3] = uiAlphaSlider.value;
                    this.update({ gui: true });
                });
                uiWheelWindow.addChild(uiAlphaSlider);


                uiWheelWindow.addChild(uiWindowLabel);
                this.uiDispacher.addChild(uiWheelWindow);
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
                    text: () => [`scale = ${this.config.scale}`, `penSize = ${this.config.penSize}`, `penColor = [${this.config.penColor.join(",")}]`].join("\n"),
                });
                uiInfoWindow.addChild(uiInfoWindowTitle)
                uiInfoWindow.addChild(uiInfoWindowBody)
                this.uiDispacher.addChild(uiInfoWindow);
            }
            {
                const uiPenSizeWindow = new GUI.Window({
                    left: 0,
                    top: 0,
                    width: 150,
                    height: 12 + 2 + 13 - 1,
                });

                // Information Window
                const uiPenSizeWindowTitle = new GUI.Label({
                    left: 0,
                    top: 0,
                    width: 150,
                    height: 12 + 2,
                    text: 'PenSize'
                });
                uiPenSizeWindow.addChild(uiPenSizeWindowTitle);
                const uiPenSizeSlider = new GUI.HorizontalSlider({
                    left: uiPenSizeWindowTitle.left,
                    top: uiPenSizeWindowTitle.top + uiPenSizeWindowTitle.height - 1,
                    width: uiPenSizeWindowTitle.width,
                    color: 'rgb(255,255,255)',
                    fontColor: 'rgb(0,0,0)',
                    height: 13,
                    minValue: 1,
                    maxValue: 100,
                });
                uiPenSizeSlider.addEventListener("changed", () => {
                    this.config.penSize = uiPenSizeSlider.value;
                    this.update({ gui: true });
                });
                uiPenSizeWindow.addChild(uiPenSizeSlider);
                this.uiDispacher.addChild(uiPenSizeWindow);
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
                for (const tool of this.tools) {
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
                        this.currentTool = tool;
                        this.update({ gui: true });
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
                    text: "save to bitmap"
                });
                top += 13 - 1;
                uiSaveButton.addEventListener("click", () => {
                    const blob = new Blob([new Uint8Array(this.imageLayerCompositedImgData.saveAsBmp())], { type: 'image/bmp' });
                    var blobURL = window.URL.createObjectURL(blob, { oneTimeOnly: true });
                    const element: HTMLAnchorElement = document.createElement("a");
                    element.href = blobURL;
                    element.style.display = "none";
                    element.download = "image.bmp"
                    document.body.appendChild(element);
                    element.click();
                    document.body.removeChild(element);
                });
                uiToolboxWindow.addChild(uiSaveButton);
                uiToolboxWindow.height = top;
                this.uiDispacher.addChild(uiToolboxWindow);
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
                const uiLayerListBox = new GUI.ListBox({
                    left: uiLayerWindowTitle.left,
                    top: 13,
                    width: uiLayerWindowTitle.width,
                    height: 48*4,
                    lineHeight: 48,
                    getItemCount: () => this.imageLayerImgDatas.length,
                    drawItem: (context : CanvasRenderingContext2D,left: number, top: number, width: number, height: number, item: number) => {
                        if (item == this.currentLayer) {
                            context.fillStyle = `rgb(24,196,195)`;
                        } else {
                            context.fillStyle = `rgb(133,133,133)`;
                        }
                        context.fillRect(left, top, width, height);
                        context.strokeStyle = `rgb(12,34,98)`;
                        context.lineWidth = 1;
                        context.strokeRect(left, top, width, height);
                    }
                });
                uiLayerListBox.click = (ev:GUI.UIMouseEvent) => {
                    const { x: gx, y: gy} = uiLayerListBox.globalPos;
                    const x = ev.x - gx;
                    const y = ev.y - gy;
                    const select = uiLayerListBox.getItemIndexByPosition(x, y);
                    this.currentLayer = select == -1 ? this.currentLayer : select;
                    this.update({ gui: true });
                };
                uiLayerWindow.addChild(uiLayerListBox);
                uiLayerWindow.height = 13+48*4-1;
                this.uiDispacher.addChild(uiLayerWindow);
            }
            this.resize();
        }

        private setupMouseEvent() {
            this.eventEmitter.on("pointerdown", (e: CustomPointerEvent) => {
                e.preventDefault();
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                const uiev = new GUI.UIMouseEvent("pointerdown", p.x, p.y);
                this.uiDispacher.dispatchEvent(uiev);
                if (uiev.defaultPrevented) {
                    this.update({ gui: true });
                    return true;
                }
                if ((e.mouse && (e.detail as MouseEvent).button === 0) ||
                    (e.touch && (e.detail as TouchEvent).touches.length <= 2)) {
                    if ((e.mouse && (e.detail as MouseEvent).ctrlKey) || (e.touch && (e.detail as TouchEvent).touches.length == 2)) {
                        let scrollStartPos: IPoint = this.pointToClient({ x: e.pageX, y: e.pageY });

                        const onScrolling = (e: CustomPointerEvent) => {
                            e.preventDefault();
                            const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                            this.config.scrollX += p.x - scrollStartPos.x;
                            this.config.scrollY += p.y - scrollStartPos.y;
                            scrollStartPos = p;
                            this.update({ view: true });
                            return true;
                        };
                        const onScrollEnd = (e: CustomPointerEvent) => {
                            e.preventDefault();
                            this.eventEmitter.off("pointermove", onScrolling);
                            const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                            this.config.scrollX += p.x - scrollStartPos.x;
                            this.config.scrollY += p.y - scrollStartPos.y;
                            scrollStartPos = p;
                            this.update({ view: true });
                            return true;
                        };
                        this.eventEmitter.on("pointermove", onScrolling);
                        this.eventEmitter.one("pointerup", onScrollEnd);
                        return true;
                    } else {
                        const onPenMove = (e: CustomPointerEvent) => {
                            if (this.currentTool) {
                                const p = this.pointToCanvas({ x: e.pageX, y: e.pageY });
                                this.currentTool.move(p);
                                this.workLayerImgData.imageData.clear();
                                this.workLayerImgData.compositMode = this.currentTool.compositMode;
                                this.currentTool.draw(this.config, this.workLayerImgData.imageData);
                                this.imageLayerCompositedImgData.clear();
                                for (let i = this.imageLayerImgDatas.length - 1; i >= 0; i--) {
                                    this.imageLayerCompositedImgData.composition(this.imageLayerImgDatas[i]);
                                    if (i == this.currentLayer) {
                                        this.imageLayerCompositedImgData.composition(this.workLayerImgData);
                                    }
                                }
                                this.update({ view: true });
                            }
                            return true;
                        };
                        const onPenUp = (e: CustomPointerEvent) => {
                            this.eventEmitter.off("pointermove", onPenMove);
                            if (this.currentTool) {
                                const p = this.pointToCanvas({ x: e.pageX, y: e.pageY });
                                this.currentTool.up(p);
                                this.workLayerImgData.imageData.clear();
                                this.workLayerImgData.compositMode = this.currentTool.compositMode;
                                this.currentTool.draw(this.config, this.workLayerImgData.imageData);
                                this.imageLayerImgDatas[this.currentLayer].imageData.composition(this.workLayerImgData);
                                this.imageLayerCompositedImgData.clear();
                                for (let i = this.imageLayerImgDatas.length - 1; i >= 0; i--) {
                                    this.imageLayerCompositedImgData.composition(this.imageLayerImgDatas[i]);
                                }

                                this.update({ view: true });
                            }
                            return true;
                        };

                        if (this.currentTool) {
                            this.eventEmitter.on("pointermove", onPenMove);
                            this.eventEmitter.one("pointerup", onPenUp);
                            const p = this.pointToCanvas({ x: e.pageX, y: e.pageY });
                            this.currentTool.down(p);
                            this.workLayerImgData.imageData.clear();
                            this.workLayerImgData.compositMode = this.currentTool.compositMode;
                            this.currentTool.draw(this.config, this.workLayerImgData.imageData);
                            this.imageLayerCompositedImgData.clear();
                            for (let i = this.imageLayerImgDatas.length - 1; i >= 0; i--) {
                                this.imageLayerCompositedImgData.composition(this.imageLayerImgDatas[i]);
                                if (i == this.currentLayer) {
                                    this.imageLayerCompositedImgData.composition(this.workLayerImgData);
                                }
                            }
                            this.update({ view: true });
                        }
                        return true;
                    }
                }
                return false;
            });
            this.eventEmitter.on("pointermove", (e: CustomPointerEvent) => {
                e.preventDefault();
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                const uiev = new GUI.UIMouseEvent("pointermove", p.x, p.y);
                this.uiDispacher.dispatchEvent(uiev);
                if (uiev.defaultPrevented) {
                    this.update({ gui: true });
                    return true;
                }
                return false;
            });
            this.eventEmitter.on("pointerup", (e: CustomPointerEvent) => {
                e.preventDefault();
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                const uiev = new GUI.UIMouseEvent("pointerup", p.x, p.y);
                this.uiDispacher.dispatchEvent(uiev);
                if (uiev.defaultPrevented) {
                    this.update({ gui: true });
                    return true;
                }
                return false;
            });
            this.eventEmitter.on("wheel", (e: WheelEvent) => {
                e.preventDefault();
                if (e.ctrlKey) {
                    if (e.deltaY < 0) {
                        if (this.config.scale < 16) {
                            this.config.scrollX = (this.config.scrollX * (this.config.scale + 1) / (this.config.scale));
                            this.config.scrollY = (this.config.scrollY * (this.config.scale + 1) / (this.config.scale));
                            this.config.scale += 1;
                            this.canvasOffsetX = ~~((this.viewCanvas.width - this.imageCanvas.width * this.config.scale) / 2);
                            this.canvasOffsetY = ~~((this.viewCanvas.height - this.imageCanvas.height * this.config.scale) / 2);
                            this.update({ overlay: true, view: true, gui: true });
                            return true;
                        }
                    } else if (e.deltaY > 0) {
                        if (this.config.scale > 1) {
                            this.config.scrollX = (this.config.scrollX * (this.config.scale - 1) / (this.config.scale));
                            this.config.scrollY = (this.config.scrollY * (this.config.scale - 1) / (this.config.scale));
                            this.config.scale -= 1;
                            this.canvasOffsetX = ~~((this.viewCanvas.width - this.imageCanvas.width * this.config.scale) / 2);
                            this.canvasOffsetY = ~~((this.viewCanvas.height - this.imageCanvas.height * this.config.scale) / 2);
                            this.update({ overlay: true, view: true, gui: true });
                            return true;
                        }
                    }
                }
                return false;
            });

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

                document.addEventListener('pointerdown', (ev: PointerEvent) => this.eventEmitter.fire('pointerdown', ev));
                document.addEventListener('pointermove', (ev: PointerEvent) => this.eventEmitter.fire('pointermove', ev));
                document.addEventListener('pointerup', (ev: PointerEvent) => this.eventEmitter.fire('pointerup', ev));
                document.addEventListener('pointerleave', (ev: PointerEvent) => this.eventEmitter.fire('pointerleave', ev));

            } else {
                document.addEventListener('mousedown', this.pointerDown.bind(this), false);
                document.addEventListener('touchstart', this.pointerDown.bind(this), false);
                document.addEventListener('mouseup', this.pointerUp.bind(this), false);
                document.addEventListener('touchend', this.pointerUp.bind(this), false);
                document.addEventListener('mousemove', this.pointerMove.bind(this), false);
                document.addEventListener('touchmove', this.pointerMove.bind(this), false);
                document.addEventListener('mouseleave', this.pointerLeave.bind(this), false);
                document.addEventListener('touchleave', this.pointerLeave.bind(this), false);
                document.addEventListener('touchcancel', this.pointerUp.bind(this), false);
            }

            document.addEventListener("mousedown", (...args: any[]) => this.eventEmitter.fire("mousedown", ...args));
            document.addEventListener("mousemove", (...args: any[]) => this.eventEmitter.fire("mousemove", ...args));
            document.addEventListener("mouseup", (...args: any[]) => { this.eventEmitter.fire("mouseup", ...args); });
            document.addEventListener("wheel", (...args: any[]) => this.eventEmitter.fire("wheel", ...args));
        }

        private isScrolling: boolean = false;
        private timeout: number = -1;
        private sDistX: number = 0;
        private sDistY: number = 0;
        private maybeClick: boolean = false;
        private maybeClickX: number = 0;
        private maybeClickY: number = 0;
        private prevTimeStamp: number = 0;
        private prevInputType: string = "";

        private checkEvent(e: /*TouchEvent | MouseEvent | PointerEvent*/ UIEvent): boolean {
            e.preventDefault();
            const istouch = e instanceof TouchEvent || (e instanceof PointerEvent && (e as PointerEvent).pointerType === "touch");
            const ismouse = e instanceof MouseEvent || ((e instanceof PointerEvent && ((e as PointerEvent).pointerType === "mouse" || (e as PointerEvent).pointerType === "pen")));
            if (istouch && this.prevInputType !== "touch") {
                if (e.timeStamp - this.prevTimeStamp >= 500) {
                    this.prevInputType = "touch";
                    this.prevTimeStamp = e.timeStamp;
                    return true;
                } else {
                    return false;
                }
            } else if (ismouse && this.prevInputType !== "mouse") {
                if (e.timeStamp - this.prevTimeStamp >= 500) {
                    this.prevInputType = "mouse";
                    this.prevTimeStamp = e.timeStamp;
                    return true;
                } else {
                    return false;
                }
            } else {
                this.prevInputType = istouch ? "touch" : ismouse ? "mouse" : "none";
                this.prevTimeStamp = e.timeStamp;
                return istouch || ismouse;
            }
        }

        private pointerDown(e: /*TouchEvent | MouseEvent | PointerEvent*/ UIEvent): boolean {
            if (this.checkEvent(e)) {
                const evt = this.makePointerEvent("down", e);
                const singleFinger: boolean = (e instanceof MouseEvent) || (e instanceof TouchEvent && (e as TouchEvent).touches.length === 1);
                if (!this.isScrolling && singleFinger) {
                    this.maybeClick = true;
                    this.maybeClickX = evt.pageX;
                    this.maybeClickY = evt.pageY;
                }
            }
            return false;
        }

        private pointerLeave(e: /*TouchEvent | MouseEvent | PointerEvent*/ UIEvent): boolean {
            if (this.checkEvent(e)) {
                this.maybeClick = false;
                this.makePointerEvent("leave", e);
            }
            return false;
        }

        private pointerMove(e: /*TouchEvent | MouseEvent | PointerEvent*/ UIEvent): boolean {
            if (this.checkEvent(e)) {
                this.makePointerEvent("move", e);
            }
            return false;
        }

        private pointerUp(e: /*TouchEvent | MouseEvent | PointerEvent*/ UIEvent): boolean {
            if (this.checkEvent(e)) {
                const evt = this.makePointerEvent("up", e);
                if (this.maybeClick) {
                    if (Math.abs(this.maybeClickX - evt.pageX) < 5 && Math.abs(this.maybeClickY - evt.pageY) < 5) {
                        if (!this.isScrolling ||
                            (Math.abs(this.sDistX - window.pageXOffset) < 5 &&
                                Math.abs(this.sDistY - window.pageYOffset) < 5)) {
                            this.makePointerEvent("click", e);
                        }
                    }
                }
                this.maybeClick = false;
            }
            return false;
        }

        private makePointerEvent(type: string, e: /*TouchEvent | MouseEvent | PointerEvent*/ UIEvent): CustomPointerEvent {
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
            this.eventEmitter.fire(eventType, evt);
            return evt;
        }
        private static resizeCanvas(canvas: HTMLCanvasElement): boolean {
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

        public resize() {
            const ret1 = Painter.resizeCanvas(this.viewCanvas);
            const ret2 = Painter.resizeCanvas(this.overlayCanvas);
            const ret3 = Painter.resizeCanvas(this.uiCanvas);
            if (ret1 || ret2) {
                this.canvasOffsetX = ~~((this.viewCanvas.width - this.imageCanvas.width * this.config.scale) / 2);
                this.canvasOffsetY = ~~((this.viewCanvas.height - this.imageCanvas.height * this.config.scale) / 2);
            }
            this.update({ view: (ret1 || ret2), gui: ret3 });
        }

        public pointToClient(point: IPoint): IPoint {
            const cr = this.viewCanvas.getBoundingClientRect();
            const sx = (point.x - (cr.left + window.pageXOffset));
            const sy = (point.y - (cr.top + window.pageYOffset));
            return { x: sx, y: sy };
        }
        public pointToCanvas(point: IPoint): IPoint {
            const p = this.pointToClient(point);
            p.x = ~~((p.x - this.canvasOffsetX - this.config.scrollX) / this.config.scale);
            p.y = ~~((p.y - this.canvasOffsetY - this.config.scrollY) / this.config.scale);
            return p;
        }

        public update({ overlay = false, view = false, gui = false }: { overlay?: boolean, view?: boolean, gui?: boolean }) {

            this.updateRequest.overlay = this.updateRequest.overlay || overlay;
            this.updateRequest.view = this.updateRequest.view || view;
            this.updateRequest.gui = this.updateRequest.gui || gui;

            if (isNaN(this.updateTimerId)) {
                this.updateTimerId = requestAnimationFrame(() => {
                    if (this.updateRequest.overlay) {
                        this.updateRequest.overlay = false;
                    }
                    if (this.updateRequest.view) {
                        this.imageContext.putImageData(this.imageLayerCompositedImgData, 0, 0);
                        this.viewContext.clearRect(0, 0, this.viewCanvas.width, this.viewCanvas.height);
                        this.viewContext.fillStyle = "rgb(198,208,224)";
                        this.viewContext.fillRect(0, 0, this.viewCanvas.width, this.viewCanvas.height);
                        this.viewContext.fillStyle = "rgb(255,255,255)";
                        this.viewContext.shadowOffsetX = this.viewContext.shadowOffsetY = 0;
                        this.viewContext.shadowColor = "rgb(0,0,0)";
                        this.viewContext.shadowBlur = 10;
                        this.viewContext.fillRect(this.canvasOffsetX + this.config.scrollX, this.canvasOffsetY + this.config.scrollY, this.imageCanvas.width * this.config.scale, this.imageCanvas.height * this.config.scale);
                        this.viewContext.shadowBlur = 0;

                        this.viewContext.fillStyle = "rgb(224,224,224)";
                        const l = this.canvasOffsetX + this.config.scrollX;
                        const t = this.canvasOffsetY + this.config.scrollY;
                        const w = this.imageCanvas.width * this.config.scale;
                        const h = this.imageCanvas.height * this.config.scale;
                        const grid: number = 8;
                        const ww = ~~((w + grid - 1) / grid);
                        const hh = ~~((h + grid - 1) / grid);
                        for (let y = 0; y < hh; y++) {
                            for (let x = 0; x < ww; x++) {
                                if ((y & 1) != (x & 1)) {
                                    this.viewContext.fillRect(l + x * grid, t + y * grid, grid, grid);
                                }
                            }
                        }
                        this.viewContext.imageSmoothingEnabled = false;
                        this.viewContext.drawImage(this.imageCanvas, 0, 0, this.imageCanvas.width, this.imageCanvas.height, this.canvasOffsetX + this.config.scrollX, this.canvasOffsetY + this.config.scrollY, this.imageCanvas.width * this.config.scale, this.imageCanvas.height * this.config.scale);
                        this.updateRequest.view = false;
                    }
                    if (this.updateRequest.gui) {
                        this.uiContext.clearRect(0, 0, this.uiCanvas.width, this.uiCanvas.height);
                        this.uiContext.imageSmoothingEnabled = false;
                        this.uiDispacher.draw(this.uiContext);
                        this.updateRequest.gui = false;
                    }
                    this.updateTimerId = NaN;
                });
            }
        }
    }
    class CustomPointerEvent extends CustomEvent {
        public detail: any;    // no use
        public touch: boolean;
        public mouse: boolean;
        public pointerId: number;
        public pageX: number;
        public pageY: number;
        public maskedEvent: UIEvent;
    }

    window.addEventListener("load", () => {
        new Painter(document.body, 512, 512);
    });
}

