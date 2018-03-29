/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.dom.d.ts" />
/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.es2016.d.ts" />
// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />

"use strict";

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
};


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

type RGBA = [number, number, number, number];

interface ImageData {
    clear: () => void;
    copyFrom: (imageData:ImageData) => void;
    composition: (...srcs: {imageData:ImageData, compositMode:CompositMode}[]) => void;
}

ImageData.prototype.copyFrom = function (imageData:ImageData): void {
    if (this.width != imageData.width || this.height != imageData.height) {
        throw new Error("size missmatch")
    }
    const len = this.width * this.height * 4;
    for (let i = 0; i < len; i++) {
        this.data[i] = imageData.data[i];
    }
};
ImageData.prototype.clear = function (): void {
    const len = this.width * this.height * 4;
    for (let i = 0; i < len; i++) {
        this.data[i] = 0x00;
    }
};
enum CompositMode {
    Normal,
    Add,
    Sub,
    Mul,
    Screen
}
ImageData.prototype.composition = function (...srcs: {imageData:ImageData, compositMode:CompositMode}[]) : void {
    const layerLen = srcs.length;
    // precheck
    for (let i = 0; i < layerLen; i++) {
        if (this.width != srcs[i].imageData.width || this.height != srcs[i].imageData.height) {
            throw new Error("size missmatch")
        }
    }

    const dataLen = this.height * this.width * 4;

    // clear this
    //for (let j = 0; j < dataLen; j += 4) {
    //    this.data[j + 0] = 0;
    //    this.data[j + 1] = 0;
    //    this.data[j + 2] = 0;
    //    this.data[j + 3] = 0;
    //}

    // operation
    for (let i = 0; i < layerLen; i++) {
        const dstData = this.data;
        const srcData = srcs[i].imageData.data;
        switch (srcs[i].compositMode) {
            case CompositMode.Normal:
                for (let j = 0; j < dataLen; j+=4) {
                    let sr = srcData[j + 0];
                    let sg = srcData[j + 1];
                    let sb = srcData[j + 2];
                    let sa = srcData[j + 3] / 255;

                    let dr = dstData[j + 0];
                    let dg = dstData[j + 1];
                    let db = dstData[j + 2];
                    let da = dstData[j + 3] / 255;

                    let na = sa + da - (sa * da);

                    let ra = ~~(na * 255 + 0.5);
                    let rr = dr;
                    let rg = dg;
                    let rb = db;

                    if (na > 0) {
                        rr = ~~((sr * sa + dr * da * (1.0 - sa)) / ra + 0.5);
                        rg = ~~((sg * sa + dg * da * (1.0 - sa)) / ra + 0.5);
                        rb = ~~((sb * sa + db * da * (1.0 - sa)) / ra + 0.5);
                    }

                    dstData[j + 0] = Math.max(0,Math.min(255,rr));
                    dstData[j + 1] = Math.max(0,Math.min(255,rg));
                    dstData[j + 2] = Math.max(0,Math.min(255,rb));
                    dstData[j + 3] = Math.max(0,Math.min(255,ra));
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

        }
    }
};


namespace TsPaint {
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

    namespace GUI {
        /**
         * コントロールコンポーネントインタフェース
         */
        export interface UI {
            left: number;
            top: number;
            width: number;
            height: number;
            visible: boolean;
            enable: boolean;
            draw: (context: CanvasRenderingContext2D) => void;
            regist: (dispatcher: UIDispatcher) => void;
            unregist: (dispatcher: UIDispatcher) => void;
        }

        /**
         * クリック操作インタフェース
         */
        export interface ClickableUI {
            click: (x: number, y: number) => void;
        }

        /**
         * スワイプ操作インタフェース
         */
        export interface SwipableUI {
            swipe: (dx: number, dy: number, x: number, y: number) => void;
        }

        /**
         * UI領域内に点(x,y)があるか判定
         * @param ui {UI}
         * @param x {number}
         * @param y {number}
         */
        export function isHit(ui: UI, x: number, y: number): boolean {
            const dx = x - ui.left;
            const dy = y - ui.top;
            return (0 <= dx && dx < ui.width) && (0 <= dy && dy < ui.height)
        }

        type CancelHandler = () => void;

        export class UIDispatcher extends Events.EventEmitter {
            private uiTable: Map<UI, Map<string, Events.EventHandler[]>>;

            constructor() {
                super();
                this.uiTable = new Map<UI, Map<string, Events.EventHandler[]>>();
            }

            public add(ui: UI): void {
                if (this.uiTable.has(ui)) {
                    return;
                }
                this.uiTable.set(ui, new Map<string, Events.EventHandler[]>());
                ui.regist(this);
            }

            public remove(ui: UI): void {
                if (!this.uiTable.has(ui)) {
                    return;
                }
                ui.unregist(this);
                const eventTable: Map<string, Events.EventHandler[]> = this.uiTable.get(ui);
                this.uiTable.set(ui, null);
                eventTable.forEach((values, key) => {
                    values.forEach((value) => this.off(key, value));
                });
                this.uiTable.delete(ui);
            }

            private registUiEvent(ui: UI, event: string, handler: Events.EventHandler): void {
                if (!this.uiTable.has(ui)) {
                    return;
                }
                const eventTable: Map<string, Events.EventHandler[]> = this.uiTable.get(ui);
                if (!eventTable.has(event)) {
                    eventTable.set(event, []);
                }
                const events = eventTable.get(event);
                events.push(handler);
                this.on(event, handler);
            }
            private unregistUiEvent(ui: UI, event: string, handler: Events.EventHandler): void {
                if (!this.uiTable.has(ui)) {
                    return;
                }
                const eventTable: Map<string, Events.EventHandler[]> = this.uiTable.get(ui);
                if (!eventTable.has(event)) {
                    return;
                }
                const events = eventTable.get(event);
                const index = events.indexOf(handler);
                if (index != -1) {
                    events.splice(index, 1);
                }
                this.off(event, handler);
            }

            public draw(context: CanvasRenderingContext2D): void {
                this.uiTable.forEach((value, key) => {
                    if (key.visible) {
                        key.draw(context);
                    }
                });
            }

            // UIに対するクリック/タップ操作を捕捉
            public onClick(ui: UI, handler: (x: number, y: number) => void): CancelHandler {
                const hookHandler = (x: number, y: number) => {
                    if (!ui.visible || !ui.enable) {
                        return false;
                    }
                    if (!isHit(ui, x, y)) {
                        return false;
                    }

                    let dx: number = 0;
                    let dy: number = 0;
                    const onPointerMoveHandler = (_x: number, _y: number) => {
                        dx += Math.abs(_x - x);
                        dy += Math.abs(_y - y);
                        return true;
                    };
                    const onPointerUpHandler = (_x: number, _y: number) => {
                        this.off("pointermove", onPointerMoveHandler);
                        this.off("pointerup", onPointerUpHandler);
                        if (dx + dy < 5) {
                            handler(_x - ui.left, _y - ui.top);
                        }
                        return true;
                    };
                    this.on("pointermove", onPointerMoveHandler);
                    this.on("pointerup", onPointerUpHandler);
                    return true;
                };
                this.registUiEvent(ui, "pointerdown", hookHandler);
                return () => this.unregistUiEvent(ui, "pointerdown", hookHandler);
            }

            //UI外のタップ/クリック操作を捕捉
            public onNcClick(ui: UI, handler: (x: number, y: number) => void): CancelHandler {
                const hookHandler = (x: number, y: number) => {
                    if (!ui.visible || !ui.enable) {
                        return false;
                    }
                    if (isHit(ui, x, y)) {
                        return false;
                    }

                    let dx: number = 0;
                    let dy: number = 0;
                    const onPointerMoveHandler = (_x: number, _y: number) => {
                        dx += Math.abs(_x - x);
                        dy += Math.abs(_y - y);
                        return true;
                    };
                    const onPointerUpHandler = (_x: number, _y: number) => {
                        this.off("pointermove", onPointerMoveHandler);
                        this.off("pointerup", onPointerUpHandler);
                        if (dx + dy < 5) {
                            handler(_x - ui.left, _y - ui.top);
                        }
                        return true;
                    };
                    this.on("pointermove", onPointerMoveHandler);
                    this.on("pointerup", onPointerUpHandler);
                    return true;
                };
                this.registUiEvent(ui, "pointerdown", hookHandler);
                return () => this.unregistUiEvent(ui, "pointerdown", hookHandler);
            }

            // UIに対するスワイプ操作を捕捉
            public onSwipe(ui: UI, handler: (dx: number, dy: number, x?: number, y?: number) => void): CancelHandler {

                const hookHandler = (x: number, y: number) => {
                    if (!ui.visible || !ui.enable) {
                        return false;
                    }
                    if (!isHit(ui, x, y)) {
                        return false;
                    }

                    const onPointerMoveHandler = (_x: number, _y: number) => {
                        let dx = (~~_x - ~~x);
                        let dy = (~~_y - ~~y);
                        x = _x;
                        y = _y;
                        handler(dx, dy, _x - ui.left, _y - ui.top);
                        return true;
                    };
                    const onPointerUpHandler = (x: number, y: number) => {
                        this.off("pointermove", onPointerMoveHandler);
                        this.off("pointerup", onPointerUpHandler);
                        return true;
                    };
                    this.on("pointermove", onPointerMoveHandler);
                    this.on("pointerup", onPointerUpHandler);

                    handler(0, 0, x - ui.left, y - ui.top);
                    return true;
                };
                this.registUiEvent(ui, "pointerdown", hookHandler);
                return () => this.unregistUiEvent(ui, "pointerdown", hookHandler);
            }


        }

        export class TextBox implements UI {
            public left: number;
            public top: number;
            public width: number;
            public height: number;
            public text: string;
            public edgeColor: string;
            public color: string;
            public font: string;
            public fontColor: string;
            public textAlign: string;
            public textBaseline: string;
            public visible: boolean;
            public enable: boolean;
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
                this.left = left;
                this.top = top;
                this.width = width;
                this.height = height;
                this.text = text;
                this.edgeColor = edgeColor;
                this.color = color;
                this.font = font;
                this.fontColor = fontColor;
                this.textAlign = textAlign;
                this.textBaseline = textBaseline;
                this.visible = visible;
                this.enable = enable;
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
            regist(dispatcher: UIDispatcher) { }
            unregist(dispatcher: UIDispatcher) { }
        }

        export class Button implements UI, ClickableUI {
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

            public left: number;
            public top: number;
            public width: number;
            public height: number;
            public text: string | (() => string);
            public edgeColor: string;
            public color: string;
            public font: string;
            public fontColor: string;
            public textAlign: string;
            public textBaseline: string;
            public visible: boolean;
            public enable: boolean;
            public draggable: boolean;
            public click: (x: number, y: number) => void;
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
                    draggable = Button.defaultValue.draggable,
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
                        draggable?: boolean;
                        disableEdgeColor?: string;
                        disableColor?: string;
                        disableFontColor?: string;
                    }) {
                this.left = left;
                this.top = top;
                this.width = width;
                this.height = height;
                this.text = text;
                this.edgeColor = edgeColor;
                this.color = color;
                this.font = font;
                this.fontColor = fontColor;
                this.textAlign = textAlign;
                this.textBaseline = textBaseline;
                this.visible = visible;
                this.enable = enable;
                this.draggable = draggable;
                this.click = () => { };
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

            }
            regist(dispatcher: UIDispatcher) {
                const cancelHandler1 = dispatcher.onClick(this, (...args: any[]) => this.click.apply(this, args));
                const cancelHandler2 = dispatcher.onSwipe(this, (deltaX: number, deltaY: number) => { if (this.draggable) { this.left += deltaX; this.top += deltaY; dispatcher.fire("update"); } });
                this.unregist = (d) => {
                    cancelHandler1();
                    cancelHandler2();
                }

            }
            unregist(dispatcher: UIDispatcher) { }
        }
        export class ImageButton implements UI, ClickableUI {
            public left: number;
            public top: number;
            public width: number;
            public height: number;
            public texture: HTMLImageElement | HTMLCanvasElement | HTMLVideoElement;
            public texLeft: number;
            public texTop: number;
            public texWidth: number;
            public texHeight: number;
            public visible: boolean;
            public enable: boolean;
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
                this.left = left;
                this.top = top;
                this.width = width;
                this.height = height;
                this.texture = texture;
                this.texLeft = texLeft;
                this.texTop = texTop;
                this.texWidth = texWidth;
                this.texHeight = texHeight;
                this.visible = visible;
                this.enable = enable;
                this.click = () => { };
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
            regist(dispatcher: UIDispatcher) {
                const cancelHandler = dispatcher.onClick(this, (...args: any[]) => this.click.apply(this, args));
                this.unregist = (d) => cancelHandler();

            }
            unregist(dispatcher: UIDispatcher) { }
        }
        export class ListBox implements UI, ClickableUI {
            public left: number;
            public top: number;
            public width: number;
            public height: number;
            public lineHeight: number;
            public scrollValue: number;
            public visible: boolean;
            public enable: boolean;
            public scrollbarWidth: number;
            public space: number;
            public click: (x: number, y: number) => void;

            public drawItem: (left: number, top: number, width: number, height: number, item: number) => void;
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
                        drawItem?: (left: number, top: number, width: number, height: number, item: number) => void,
                        getItemCount?: () => number,
                        visible?: boolean;
                        enable?: boolean;
                        scrollbarWidth?: number;
                        space?: number;
                    }) {
                this.left = left;
                this.top = top;
                this.width = width;
                this.height = height;
                this.lineHeight = lineHeight;
                this.drawItem = drawItem;
                this.getItemCount = getItemCount;
                this.scrollValue = 0;
                this.visible = visible;
                this.enable = enable;
                this.scrollbarWidth = scrollbarWidth;
                this.space = space;
                this.click = () => { }
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
                    this.drawItem(this.left, this.top + sy, this.width - this.scrollbarWidth, this.lineHeight, index);
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
            regist(dispatcher: UIDispatcher) {
                const cancelHandlers = [
                    dispatcher.onSwipe(this, (deltaX: number, deltaY: number) => {
                        this.scrollValue -= deltaY;
                        this.update();
                    }),
                    dispatcher.onClick(this, (...args: any[]) => this.click.apply(this, args))
                ];
                this.unregist = (d) => cancelHandlers.forEach(x => x());

            }
            unregist(dispatcher: UIDispatcher) { }
        }
        export class HorizontalSlider implements UI {
            public left: number;
            public top: number;
            public width: number;
            public height: number;
            public sliderWidth: number;
            public edgeColor: string;
            public color: string;
            public bgColor: string;
            public font: string;
            public fontColor: string;

            public value: number;
            public minValue: number;
            public maxValue: number;
            public visible: boolean;
            public enable: boolean;

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
                this.left = left;
                this.top = top;
                this.width = width;
                this.height = height;
                this.sliderWidth = sliderWidth;
                this.edgeColor = edgeColor;
                this.color = color;
                this.bgColor = bgColor;
                this.font = font;
                this.fontColor = fontColor;
                this.minValue = minValue;
                this.maxValue = maxValue;
                this.value = minValue;
                this.visible = visible;
                this.enable = enable;
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
            regist(dispatcher: UIDispatcher) {
                const cancelHandler = dispatcher.onSwipe(this, (dx, dy, x, y) => {
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
                });
                this.unregist = (d) => cancelHandler();

            }
            unregist(dispatcher: UIDispatcher) { }
        }

    }

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

    class SolidPen {
        private joints: IPoint[];
        private points: Set<string>;

        constructor() {
            this.joints = [];
            this.points = new Set<string>();
        }

        public down(point: IPoint) {
            this.joints.length = 0;
            this.joints.push(point);
        }

        public move(point: IPoint) {
            this.joints.push(point);
        }

        public up(point: IPoint) {
        }

        // https://github.com/miloyip/line
        private bresenham({x: x0, y: y0}: IPoint, {x: x1, y: y1}: IPoint, pset: (x: number, y: number) => void) {
            const dx: number = Math.abs(x1 - x0);
            const sx: number = (x0 < x1) ? 1 : -1;
            const dy: number = Math.abs(y1 - y0);
            const sy: number = y0 < y1 ? 1 : -1;
            let err: number = ~~((dx > dy ? dx : -dy) / 2);

            for (; ;) {
                pset(x0, y0);
                if (x0 === x1 && y0 === y1) {
                    break;
                }
                const e2 = err;
                if (e2 > -dx) { err -= dy; x0 += sx; }
                if (e2 < dy) { err += dx; y0 += sy; }
            }
        }

        public draw(config: IPainterConfig, imgData: ImageData) {
            const color = config.penColor;
            const pset = (x0: number, y0: number) => {
                if (0 <= x0 && x0 < imgData.width && 0 <= y0 && y0 < imgData.height) {
                    const off = (y0 * imgData.width + x0) * 4;
                    imgData.data[off + 0] = color[0];
                    imgData.data[off + 1] = color[1];
                    imgData.data[off + 2] = color[2];
                    imgData.data[off + 3] = color[3];
                }
            };
            if (this.joints.length === 1) {
                this.bresenham(this.joints[0], this.joints[0], pset);
            } else {
                for (let i = 1; i < this.joints.length; i++) {
                    this.bresenham(this.joints[i - 1], this.joints[i - 0], pset);
                }
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

    class Painter {
        private root: HTMLElement;

        private eventEmitter: Events.EventEmitter

        private uiDispacher: GUI.UIDispatcher;

        private uiCanvas: HTMLCanvasElement;
        private uiContext: CanvasRenderingContext2D;

        private imageCanvas: HTMLCanvasElement;
        private imageContext: CanvasRenderingContext2D;
        private imageImgData: ImageData;
        private compositedImgData: ImageData;

        private viewCanvas: HTMLCanvasElement;
        private viewContext: CanvasRenderingContext2D;

        private previewCanvas: HTMLCanvasElement;
        private previewContext: CanvasRenderingContext2D;

        private workCanvas: HTMLCanvasElement;
        private workContext: CanvasRenderingContext2D;
        private workImgData: ImageData;

        private canvasOffsetX: number = 0;
        private canvasOffsetY: number = 0;

        private pen: SolidPen = new SolidPen();

        private config: IPainterConfig = {
            scale: 1,
            penSize: 5,
            penColor: [0, 0, 0, 64],
            scrollX: 0,
            scrollY: 0,
        };

        private updateRequest: { preview: boolean, view: boolean, gui: boolean };
        private updateTimerId: number = NaN;

        constructor(root: HTMLElement, width: number, height: number) {
            this.root = root;

            this.eventEmitter = new Events.EventEmitter();

            this.imageCanvas = document.createElement("canvas");
            this.imageCanvas.width = width;
            this.imageCanvas.height = height;
            this.imageContext = this.imageCanvas.getContext("2d");
            this.imageContext.imageSmoothingEnabled = false;
            this.imageImgData = this.imageContext.createImageData(width, height);
            this.compositedImgData = this.imageContext.createImageData(width, height);

            this.workCanvas = document.createElement("canvas");
            this.workCanvas.width = width;
            this.workCanvas.height = height;
            this.workContext = this.workCanvas.getContext("2d");
            this.workContext.imageSmoothingEnabled = false;
            this.workImgData = this.workContext.createImageData(width, height);

            this.viewCanvas = document.createElement("canvas");
            this.viewCanvas.style.position = "absolute";
            this.viewCanvas.style.left = "0";
            this.viewCanvas.style.top = "0";
            this.viewCanvas.style.width = "100%";
            this.viewCanvas.style.height = "100%";
            this.viewContext = this.viewCanvas.getContext("2d");
            this.viewContext.imageSmoothingEnabled = false;
            root.appendChild(this.viewCanvas);

            this.previewCanvas = document.createElement("canvas");
            this.previewCanvas.style.position = "absolute";
            this.previewCanvas.style.left = "0";
            this.previewCanvas.style.top = "0";
            this.previewCanvas.style.width = "100%";
            this.previewCanvas.style.height = "100%";
            this.previewContext = this.previewCanvas.getContext("2d");
            this.previewContext.imageSmoothingEnabled = false;
            root.appendChild(this.previewCanvas);

            this.uiDispacher = new GUI.UIDispatcher();
            this.uiCanvas = document.createElement("canvas");
            this.uiCanvas.style.position = "absolute";
            this.uiCanvas.style.left = "0";
            this.uiCanvas.style.top = "0";
            this.uiCanvas.style.width = "100%";
            this.uiCanvas.style.height = "100%";
            this.uiContext = this.uiCanvas.getContext("2d");
            this.uiContext.imageSmoothingEnabled = false;
            root.appendChild(this.uiCanvas);

            this.updateRequest = { gui: false, preview: false, view: false };
            this.updateTimerId = NaN;

            this.uiDispacher.on("update", () => { this.update({ gui: true }); return true; });

            window.addEventListener("resize", () => this.resize());
            this.setupMouseEvent();

            {
                let count = 0;
                const uiButton = new GUI.Button({
                    left: 0,
                    top: 0,
                    width: 150,
                    height: 12 * 3,
                    draggable: true,
                    text: () => [`scale = ${this.config.scale}`, `penSize = ${this.config.penSize}`, `penColor = [${this.config.penColor.join(",")}]`].join("\n"),
                });
                uiButton.click = (x, y) => {
                    count++;
                    this.update({ gui: true });
                }
                this.uiDispacher.add(uiButton);
            }

            this.resize();
        }

        private setupMouseEvent() {
            this.eventEmitter.on("mousedown", (e: MouseEvent) => {
                e.preventDefault();
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                if (this.uiDispacher.fire("pointerdown", p.x, p.y)) {
                    return true;
                }
                if (e.button === 0) {
                    if (e.ctrlKey) {
                        let scrollStartPos: IPoint = this.pointToClient({ x: e.pageX, y: e.pageY });

                        const onScrolling = (e: MouseEvent) => {
                            e.preventDefault();
                            const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                            this.config.scrollX += p.x - scrollStartPos.x;
                            this.config.scrollY += p.y - scrollStartPos.y;
                            scrollStartPos = p;
                            this.update({ preview: true, view: true });
                            return true;
                        };
                        const onScrollEnd = (e: MouseEvent) => {
                            e.preventDefault();
                            this.eventEmitter.off("mousemove", onScrolling);
                            const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                            this.config.scrollX += p.x - scrollStartPos.x;
                            this.config.scrollY += p.y - scrollStartPos.y;
                            scrollStartPos = p;
                            this.update({ preview: true, view: true });
                            return true;
                        };
                        this.eventEmitter.on("mousemove", onScrolling);
                        this.eventEmitter.one("mouseup", onScrollEnd);
                        return true;
                    } else {
                        const onPenMove = (e: MouseEvent) => {
                            const p = this.pointToCanvas({ x: e.pageX, y: e.pageY });
                            this.pen.move(p);
                            this.workImgData.clear();
                            this.pen.draw(this.config, this.workImgData);
                            this.compositedImgData.copyFrom(this.imageImgData);
                            this.compositedImgData.composition(
                                {imageData: this.workImgData, compositMode: CompositMode.Normal}
                            );
                            this.update({ view: true });
                            return true;
                        };
                        const onPenUp = (e: MouseEvent) => {
                            this.eventEmitter.off("mousemove", onPenMove);
                            const p = this.pointToCanvas({ x: e.pageX, y: e.pageY });
                            this.pen.up(p);
                            this.workImgData.clear();
                            this.pen.draw(this.config, this.workImgData);
                            this.imageImgData.composition(
                                {imageData: this.workImgData, compositMode: CompositMode.Normal}
                            );
                            this.compositedImgData.copyFrom(this.imageImgData);
                            this.update({ view: true });
                            return true;
                        };

                        this.eventEmitter.on("mousemove", onPenMove);
                        this.eventEmitter.one("mouseup", onPenUp);
                        const p = this.pointToCanvas({ x: e.pageX, y: e.pageY });
                        this.pen.down(p);
                        this.workImgData.clear();
                        this.pen.draw(this.config, this.workImgData);
                        this.update({ view: true });
                        return true;
                    }
                }
                return false;
            });
            this.eventEmitter.on("mousemove", (e: MouseEvent) => {
                e.preventDefault();
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                if (this.uiDispacher.fire("pointermove", p.x, p.y)) {
                    return true;
                }
                return false;
            });
            this.eventEmitter.on("mouseup", (e: MouseEvent) => {
                e.preventDefault();
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                if (this.uiDispacher.fire("pointerup", p.x, p.y)) {
                    return true;
                }
                return false;
            });
            this.eventEmitter.on("wheel", (e: WheelEvent) => {
                e.preventDefault();
                if (e.ctrlKey) {
                    if (e.deltaY > 0) {
                        if (this.config.scale < 16) {
                            this.config.scrollX = (this.config.scrollX * (this.config.scale + 1) / (this.config.scale));
                            this.config.scrollY = (this.config.scrollY * (this.config.scale + 1) / (this.config.scale));
                            this.config.scale += 1;
                            this.canvasOffsetX = ~~((this.viewCanvas.width - this.imageCanvas.width * this.config.scale) / 2);
                            this.canvasOffsetY = ~~((this.viewCanvas.height - this.imageCanvas.height * this.config.scale) / 2);
                            this.update({ preview: true, view: true, gui: true });
                            return true;
                        }
                    } else if (e.deltaY < 0) {
                        if (this.config.scale > 1) {
                            this.config.scrollX = (this.config.scrollX * (this.config.scale - 1) / (this.config.scale));
                            this.config.scrollY = (this.config.scrollY * (this.config.scale - 1) / (this.config.scale));
                            this.config.scale -= 1;
                            this.canvasOffsetX = ~~((this.viewCanvas.width - this.imageCanvas.width * this.config.scale) / 2);
                            this.canvasOffsetY = ~~((this.viewCanvas.height - this.imageCanvas.height * this.config.scale) / 2);
                            this.update({ preview: true, view: true, gui: true });
                            return true;
                        }
                    }
                }
                return false;
            });

            document.addEventListener("mousedown", (...args: any[]) => this.eventEmitter.fire("mousedown", ...args));
            document.addEventListener("mousemove", (...args: any[]) => this.eventEmitter.fire("mousemove", ...args));
            document.addEventListener("mouseup", (...args: any[]) => this.eventEmitter.fire("mouseup", ...args));
            document.addEventListener("wheel", (...args: any[]) => this.eventEmitter.fire("wheel", ...args));
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
            const ret2 = Painter.resizeCanvas(this.previewCanvas);
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

        public update({ preview = false, view = false, gui = false }: { preview?: boolean, view?: boolean, gui?: boolean }) {

            this.updateRequest.preview = this.updateRequest.preview || preview;
            this.updateRequest.view = this.updateRequest.view || view;
            this.updateRequest.gui = this.updateRequest.gui || gui;

            if (isNaN(this.updateTimerId)) {
                this.updateTimerId = requestAnimationFrame(() => {
                    if (this.updateRequest.preview) {
                        this.workContext.putImageData(this.workImgData, 0, 0);
                        this.previewContext.clearRect(0, 0, this.previewCanvas.width, this.previewCanvas.height);
                        this.previewContext.imageSmoothingEnabled = false;
                        this.previewContext.drawImage(this.workCanvas, 0, 0, this.workCanvas.width, this.workCanvas.height, this.canvasOffsetX + this.config.scrollX, this.canvasOffsetY + this.config.scrollY, this.workCanvas.width * this.config.scale, this.workCanvas.height * this.config.scale);
                        this.updateRequest.preview = false;
                    }
                    if (this.updateRequest.view) {
                        this.imageContext.putImageData(this.compositedImgData, 0, 0);
                        this.previewContext.clearRect(0, 0, this.previewCanvas.width, this.previewCanvas.height);
                        this.viewContext.clearRect(0, 0, this.viewCanvas.width, this.viewCanvas.height);
                        this.viewContext.fillStyle = "rgb(198,208,224)";
                        this.viewContext.fillRect(0, 0, this.viewCanvas.width, this.viewCanvas.height);
                        this.viewContext.fillStyle = "rgb(255,255,255)";
                        this.viewContext.shadowOffsetX = this.viewContext.shadowOffsetY = 0;
                        this.viewContext.shadowColor = "rgb(0,0,0)";
                        this.viewContext.shadowBlur = 10;
                        this.viewContext.fillRect(this.canvasOffsetX + this.config.scrollX, this.canvasOffsetY + this.config.scrollY, this.imageCanvas.width * this.config.scale, this.imageCanvas.height * this.config.scale);
                        this.viewContext.shadowBlur = 0;
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

    window.addEventListener("load", () => {
        new Painter(document.body, 512, 512);
    });
}

