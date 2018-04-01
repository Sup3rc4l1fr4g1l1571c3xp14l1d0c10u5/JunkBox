/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.dom.d.ts" />
/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.es2016.d.ts" />
// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />
"use strict";
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
ImageData.prototype.copyFrom = function (imageData) {
    if (this.width != imageData.width || this.height != imageData.height) {
        throw new Error("size missmatch");
    }
    const len = this.width * this.height * 4;
    for (let i = 0; i < len; i++) {
        this.data[i] = imageData.data[i];
    }
};
ImageData.prototype.clear = function () {
    const len = this.width * this.height * 4;
    for (let i = 0; i < len; i++) {
        this.data[i] = 0x00;
    }
};
ImageData.prototype.pointSet = function (x0, y0, color) {
    if (0 <= x0 && x0 < this.width && 0 <= y0 && y0 < this.height) {
        const off = (y0 * this.width + x0) * 4;
        this.data[off + 0] = color[0];
        this.data[off + 1] = color[1];
        this.data[off + 2] = color[2];
        this.data[off + 3] = color[3];
    }
};
var CompositMode;
(function (CompositMode) {
    CompositMode[CompositMode["Normal"] = 0] = "Normal";
    CompositMode[CompositMode["Add"] = 1] = "Add";
    CompositMode[CompositMode["Sub"] = 2] = "Sub";
    CompositMode[CompositMode["Mul"] = 3] = "Mul";
    CompositMode[CompositMode["Screen"] = 4] = "Screen";
})(CompositMode || (CompositMode = {}));
ImageData.prototype.composition = function (...srcs) {
    const layerLen = srcs.length;
    // precheck
    for (let i = 0; i < layerLen; i++) {
        if (this.width != srcs[i].imageData.width || this.height != srcs[i].imageData.height) {
            throw new Error("size missmatch");
        }
    }
    // operation
    const dataLen = this.height * this.width * 4;
    for (let i = 0; i < layerLen; i++) {
        const dstData = this.data;
        const srcData = srcs[i].imageData.data;
        switch (srcs[i].compositMode) {
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
                        rr = ~~((sr * sa + dr * da * (1.0 - sa)) / na + 0.5);
                        rg = ~~((sg * sa + dg * da * (1.0 - sa)) / na + 0.5);
                        rb = ~~((sb * sa + db * da * (1.0 - sa)) / na + 0.5);
                    }
                    dstData[j + 0] = rr; //(rr < 0) ? 0 : (rr > 255) ? 255 : rr;  // Math.max(0,Math.min(255,rr)); // Math.max/min is too slow in firefox 54.0.1 :-( 
                    dstData[j + 1] = rg; //(rg < 0) ? 0 : (rg > 255) ? 255 : rg;  // Math.max(0,Math.min(255,rg));
                    dstData[j + 2] = rb; //(rb < 0) ? 0 : (rb > 255) ? 255 : rb;  // Math.max(0,Math.min(255,rb));
                    dstData[j + 3] = ra; //(ra < 0) ? 0 : (ra > 255) ? 255 : ra;  // Math.max(0,Math.min(255,ra));
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
var TsPaint;
(function (TsPaint) {
    let Events;
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
    let GUI;
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
                        ui.dispatchEvent(new UIMouseEvent("click", ev.x - ui.left, ev.y - ui.top));
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
                if (!isHit(ui, x, y)) {
                    return;
                }
                ev.preventDefault();
                ev.stopPropagation();
                let root = ui.parent;
                while (root.parent) {
                    root = root.parent;
                }
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
                    if (isHit(child, x, y)) {
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
                }
            }
        }
        Control.MOUSE_EVENT_NAME = ["pointerdown", "pointermove", "pointerup"];
        GUI.Control = Control;
        /**
         * UI領域内に点(x,y)があるか判定
         * @param ui {UI}
         * @param x {number}
         * @param y {number}
         */
        function isHit(ui, x, y) {
            const { x: dx, y: dy } = ui.globalPos;
            return (dx <= x && x < dx + ui.width) && (dy <= y && y < dy + ui.height);
        }
        GUI.isHit = isHit;
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
                this.addEventListener("click", (event) => this.click.call(this, event));
                installSwipeDelegate(this);
                installClickDelecate(this);
            }
            contentHeight() {
                const itemCount = this.getItemCount();
                if (itemCount === 0) {
                    return 0;
                }
                else {
                    return this.lineHeight + (this.lineHeight + this.space) * (itemCount - 1);
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
                let sy = -(scrollValue % (this.lineHeight + this.space));
                let index = ~~(scrollValue / (this.lineHeight + this.space));
                let itemCount = this.getItemCount();
                let drawResionHeight = this.height - sy;
                context.fillStyle = "rgba(255,255,255,0.25)";
                context.fillRect(this.left, this.top, this.width, this.height);
                for (;;) {
                    if (sy >= this.height) {
                        break;
                    }
                    if (index >= itemCount) {
                        break;
                    }
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
            getItemIndexByPosition(x, y) {
                this.update();
                if (x < 0 || this.width <= x || y < 0 || this.height <= y) {
                    return -1;
                }
                const index = ~~((y + this.scrollValue) / (this.lineHeight + this.space));
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
            constructor({ left = 0, top = 0, width = 0, height = 0, sliderWidth = 5, edgeColor = `rgb(128,128,128)`, color = `rgb(255,255,255)`, bgColor = `rgb(192,192,192)`, font = undefined, fontColor = `rgb(0,0,0)`, minValue = 0, maxValue = 0, visible = true, enable = true, }) {
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
                this.addEventListener("swipe", (event) => {
                    const { x: l, y: t } = this.globalPos;
                    const x = event.x - l;
                    const yy = event.y - t;
                    const rangeSize = this.maxValue - this.minValue;
                    if (rangeSize == 0) {
                        this.value = this.minValue;
                    }
                    else {
                        if (x <= this.sliderWidth / 2) {
                            this.value = this.minValue;
                        }
                        else if (x >= this.width - this.sliderWidth / 2) {
                            this.value = this.maxValue;
                        }
                        else {
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
            draw(context) {
                const lineWidth = this.width - this.sliderWidth;
                context.fillStyle = this.bgColor;
                context.fillRect(this.left, this.top, this.width, this.height);
                context.fillStyle = this.color;
                context.strokeStyle = this.edgeColor;
                context.fillRect(this.left + ~~(lineWidth * (this.value - this.minValue) / (this.maxValue - this.minValue)), this.top, this.sliderWidth, this.height);
                context.strokeRect(this.left + ~~(lineWidth * (this.value - this.minValue) / (this.maxValue - this.minValue)), this.top, this.sliderWidth, this.height);
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
    let IPoint;
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
    let Brushes;
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
                    let scan = 0;
                    for (let yy = -r; yy <= r; yy++) {
                        for (let xx = -r; xx <= r; xx++) {
                            if (mask[scan++] != 0) {
                                imgData.pointSet(x0 + xx, y0 + yy, color);
                            }
                        }
                    }
                };
            }
        }
        Brushes.createSolidBrush = createSolidBrush;
    })(Brushes || (Brushes = {}));
    let Topology;
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
    class HSVColorWheel {
        constructor({ width = 0, height = 0, wheelRadiusMin = 76, wheelRadiusMax = 96, svBoxSize = 100 }) {
            this.hsv_h = 0;
            this.hsv_s = 0;
            this.hsv_v = 0;
            this.canvas = document.createElement("canvas");
            this.canvas.width = width;
            this.canvas.height = height;
            this.context = this.canvas.getContext("2d");
            this.imageData = this.context.createImageData(this.canvas.width, this.canvas.height);
            this.wheelRadius = { min: wheelRadiusMin, max: wheelRadiusMax };
            this.svBoxSize = svBoxSize;
            this.hsv_h = 0;
            this.hsv_s = 0;
            this.hsv_v = 0;
            this.updateImage();
        }
        get H() { return this.hsv_h; }
        set H(v) { this.hsv_h = (Math.sign(v) < 0 ? 360 : 0) + (~~v % 360); }
        get S() { return this.hsv_s; }
        set S(v) { this.hsv_s = (v < 0) ? 0 : (v > 1) ? 1 : v; }
        get V() { return this.hsv_v; }
        set V(v) { this.hsv_v = (v < 0) ? 0 : (v > 1) ? 1 : v; }
        get rgb() {
            return HSVColorWheel.hsv2rgb(this.hsv_h, this.hsv_s, this.hsv_v);
        }
        set rgb(v) {
            [this.hsv_h, this.hsv_s, this.hsv_v] = HSVColorWheel.rgb2hsv(v[0], v[1], v[2]);
        }
        static rgb2hsv(r, g, b) {
            let max = r > g ? r : g;
            max = max > b ? max : b;
            let min = r < g ? r : g;
            min = min < b ? min : b;
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
        static hsv2rgb(h, s, v) {
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
                    const col = HSVColorWheel.hsv2rgb(h, 1.0, 1.0);
                    this.setPixel(ix, iy, col);
                }
            }
        }
        drawSVBox() {
            for (let iy = 0; iy < this.svBoxSize; iy++) {
                const v = (this.svBoxSize - 1 - iy) / (this.svBoxSize - 1);
                for (let ix = 0; ix < this.svBoxSize; ix++) {
                    const s = ix / (this.svBoxSize - 1);
                    const col = HSVColorWheel.hsv2rgb(this.hsv_h, s, v);
                    this.setPixel(ix + ~~((this.canvas.width - this.svBoxSize) / 2), iy + ~~((this.canvas.height - this.svBoxSize) / 2), col);
                }
            }
        }
        drawHCursor() {
            const rd = -this.hsv_h * Math.PI / 180;
            const xx = this.wheelRadius.min + (this.wheelRadius.max - this.wheelRadius.min) / 2;
            const yy = 0;
            const x = ~~(xx * Math.cos(rd) - yy * Math.sin(rd) + this.canvas.width / 2);
            const y = ~~(xx * Math.sin(rd) + yy * Math.cos(rd) + this.canvas.height / 2);
            this.drawInvBox(x - 4, y - 4, 9, 9);
        }
        getGValue(x0, y0) {
            const x = x0 - this.canvas.width / 2;
            const y = y0 - this.canvas.height / 2;
            const h = (~~(-Math.atan2(y, x) * 180 / Math.PI) + 360) % 360;
            const r = ~~Math.sqrt(x * x + y * y);
            return (r >= this.wheelRadius.min && r < this.wheelRadius.max) ? h : undefined;
        }
        drawSVCursor() {
            const left = (this.canvas.width - this.svBoxSize) / 2;
            const top = (this.canvas.height - this.svBoxSize) / 2;
            this.drawInvBox(left + ~~(this.hsv_s * this.svBoxSize) - 4, top + ~~((1 - this.hsv_v) * this.svBoxSize) - 4, 9, 9);
        }
        getSVValue(x0, y0) {
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
            const ret1 = this.getGValue(x, y);
            if (ret1 != undefined) {
                this.hsv_h = ret1;
            }
            const ret2 = this.getSVValue(x, y);
            if (ret2 != undefined) {
                [this.hsv_s, this.hsv_v] = ret2;
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
                const { x: dx, y: dy } = this.globalPos;
                const x = event.x - dx;
                const y = event.y - dy;
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
    /**
     * フリーハンド
     */
    class SolidPen {
        constructor() {
            this.joints = [];
        }
        down(point) {
            this.joints.length = 0;
            this.joints.push(point);
        }
        move(point) {
            this.joints.push(point);
        }
        up(point) {
        }
        draw(config, imgData) {
            const brush = Brushes.createSolidBrush(imgData, config.penColor, config.penSize);
            if (this.joints.length === 1) {
                Topology.drawLine(this.joints[0], this.joints[0], brush);
            }
            else {
                for (let i = 1; i < this.joints.length; i++) {
                    Topology.drawLine(this.joints[i - 1], this.joints[i - 0], brush);
                }
            }
        }
    }
    ;
    class Painter {
        constructor(parentHtmlElement, width, height) {
            this.canvasOffsetX = 0;
            this.canvasOffsetY = 0;
            this.currentTool = new SolidPen();
            this.config = {
                scale: 1,
                penSize: 5,
                penColor: [0, 0, 0, 64],
                scrollX: 0,
                scrollY: 0,
            };
            this.updateTimerId = NaN;
            this.isScrolling = false;
            this.timeout = -1;
            this.sDistX = 0;
            this.sDistY = 0;
            this.maybeClick = false;
            this.maybeClickX = 0;
            this.maybeClickY = 0;
            this.prevTimeStamp = 0;
            this.prevInputType = "";
            this.parentHtmlElement = parentHtmlElement;
            this.eventEmitter = new Events.EventEmitter();
            this.imageCanvas = document.createElement("canvas");
            this.imageCanvas.width = width;
            this.imageCanvas.height = height;
            this.imageContext = this.imageCanvas.getContext("2d");
            this.imageContext.imageSmoothingEnabled = false;
            this.imageLayerImgData = this.imageContext.createImageData(width, height);
            this.imageLayerCompositedImgData = this.imageContext.createImageData(width, height);
            this.workLayerImgData = this.imageContext.createImageData(width, height);
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
            document.onselectstart = () => false;
            document.oncontextmenu = () => false;
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
                    this.config.penColor = ret;
                    this.update({ gui: true });
                };
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
                uiInfoWindow.addChild(uiInfoWindowTitle);
                uiInfoWindow.addChild(uiInfoWindowBody);
                this.uiDispacher.addChild(uiInfoWindow);
            }
            {
                const uiInfoWindow = new GUI.Window({
                    left: 0,
                    top: 0,
                    width: 150,
                    height: 12 + 2 + 13 - 1,
                });
                // Information Window
                const uiInfoWindowTitle = new GUI.Label({
                    left: 0,
                    top: 0,
                    width: 150,
                    height: 12 + 2,
                    text: 'BrushSize'
                });
                uiInfoWindow.addChild(uiInfoWindowTitle);
                const uiAlphaSlider = new GUI.HorizontalSlider({
                    left: uiInfoWindowTitle.left,
                    top: uiInfoWindowTitle.top + uiInfoWindowTitle.height - 1,
                    width: uiInfoWindowTitle.width,
                    color: 'rgb(255,255,255)',
                    fontColor: 'rgb(0,0,0)',
                    height: 13,
                    minValue: 1,
                    maxValue: 100,
                });
                uiAlphaSlider.addEventListener("changed", () => {
                    this.config.penSize = uiAlphaSlider.value;
                    this.update({ gui: true });
                });
                uiInfoWindow.addChild(uiAlphaSlider);
                this.uiDispacher.addChild(uiInfoWindow);
            }
            this.resize();
        }
        setupMouseEvent() {
            this.eventEmitter.on("pointerdown", (e) => {
                e.preventDefault();
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                const uiev = new GUI.UIMouseEvent("pointerdown", p.x, p.y);
                this.uiDispacher.dispatchEvent(uiev);
                if (uiev.defaultPrevented) {
                    this.update({ gui: true });
                    return true;
                }
                if ((e.mouse && e.detail.button === 0) ||
                    (e.touch && e.detail.touches.length <= 2)) {
                    if ((e.mouse && e.detail.ctrlKey) || (e.touch && e.detail.touches.length == 2)) {
                        let scrollStartPos = this.pointToClient({ x: e.pageX, y: e.pageY });
                        const onScrolling = (e) => {
                            e.preventDefault();
                            const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                            this.config.scrollX += p.x - scrollStartPos.x;
                            this.config.scrollY += p.y - scrollStartPos.y;
                            scrollStartPos = p;
                            this.update({ view: true });
                            return true;
                        };
                        const onScrollEnd = (e) => {
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
                    }
                    else {
                        const onPenMove = (e) => {
                            const p = this.pointToCanvas({ x: e.pageX, y: e.pageY });
                            this.currentTool.move(p);
                            this.workLayerImgData.clear();
                            this.currentTool.draw(this.config, this.workLayerImgData);
                            this.imageLayerCompositedImgData.copyFrom(this.imageLayerImgData);
                            this.imageLayerCompositedImgData.composition({ imageData: this.workLayerImgData, compositMode: CompositMode.Normal });
                            this.update({ view: true });
                            return true;
                        };
                        const onPenUp = (e) => {
                            this.eventEmitter.off("pointermove", onPenMove);
                            const p = this.pointToCanvas({ x: e.pageX, y: e.pageY });
                            this.currentTool.up(p);
                            this.workLayerImgData.clear();
                            this.currentTool.draw(this.config, this.workLayerImgData);
                            this.imageLayerImgData.composition({ imageData: this.workLayerImgData, compositMode: CompositMode.Normal });
                            this.imageLayerCompositedImgData.copyFrom(this.imageLayerImgData);
                            this.update({ view: true });
                            return true;
                        };
                        this.eventEmitter.on("pointermove", onPenMove);
                        this.eventEmitter.one("pointerup", onPenUp);
                        const p = this.pointToCanvas({ x: e.pageX, y: e.pageY });
                        this.currentTool.down(p);
                        this.workLayerImgData.clear();
                        this.currentTool.draw(this.config, this.workLayerImgData);
                        this.update({ view: true });
                        return true;
                    }
                }
                return false;
            });
            this.eventEmitter.on("pointermove", (e) => {
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
            this.eventEmitter.on("pointerup", (e) => {
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
            this.eventEmitter.on("wheel", (e) => {
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
                    }
                    else if (e.deltaY > 0) {
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
                document.body.addEventListener('touchmove', evt => { evt.preventDefault(); }, false);
                document.body.addEventListener('touchdown', evt => { evt.preventDefault(); }, false);
                document.body.addEventListener('touchup', evt => { evt.preventDefault(); }, false);
                document.body.addEventListener('mousemove', evt => { evt.preventDefault(); }, false);
                document.body.addEventListener('mousedown', evt => { evt.preventDefault(); }, false);
                document.body.addEventListener('mouseup', evt => { evt.preventDefault(); }, false);
                document.body.addEventListener('pointerdown', (ev) => this.eventEmitter.fire('pointerdown', ev));
                document.body.addEventListener('pointermove', (ev) => this.eventEmitter.fire('pointermove', ev));
                document.body.addEventListener('pointerup', (ev) => this.eventEmitter.fire('pointerup', ev));
                document.body.addEventListener('pointerleave', (ev) => this.eventEmitter.fire('pointerleave', ev));
            }
            else {
                document.body.addEventListener('mousedown', this.pointerDown.bind(this), false);
                document.body.addEventListener('touchstart', this.pointerDown.bind(this), false);
                document.body.addEventListener('mouseup', this.pointerUp.bind(this), false);
                document.body.addEventListener('touchend', this.pointerUp.bind(this), false);
                document.body.addEventListener('mousemove', this.pointerMove.bind(this), false);
                document.body.addEventListener('touchmove', this.pointerMove.bind(this), false);
                document.body.addEventListener('mouseleave', this.pointerLeave.bind(this), false);
                document.body.addEventListener('touchleave', this.pointerLeave.bind(this), false);
                document.body.addEventListener('touchcancel', this.pointerUp.bind(this), false);
            }
            document.addEventListener("mousedown", (...args) => this.eventEmitter.fire("mousedown", ...args));
            document.addEventListener("mousemove", (...args) => this.eventEmitter.fire("mousemove", ...args));
            document.addEventListener("mouseup", (...args) => { this.eventEmitter.fire("mouseup", ...args); });
            document.addEventListener("wheel", (...args) => this.eventEmitter.fire("wheel", ...args));
        }
        checkEvent(e) {
            e.preventDefault();
            const istouch = e instanceof TouchEvent || (e instanceof PointerEvent && e.pointerType === "touch");
            const ismouse = e instanceof MouseEvent || ((e instanceof PointerEvent && (e.pointerType === "mouse" || e.pointerType === "pen")));
            if (istouch && this.prevInputType !== "touch") {
                if (e.timeStamp - this.prevTimeStamp >= 500) {
                    this.prevInputType = "touch";
                    this.prevTimeStamp = e.timeStamp;
                    return true;
                }
                else {
                    return false;
                }
            }
            else if (ismouse && this.prevInputType !== "mouse") {
                if (e.timeStamp - this.prevTimeStamp >= 500) {
                    this.prevInputType = "mouse";
                    this.prevTimeStamp = e.timeStamp;
                    return true;
                }
                else {
                    return false;
                }
            }
            else {
                this.prevInputType = istouch ? "touch" : ismouse ? "mouse" : "none";
                this.prevTimeStamp = e.timeStamp;
                return istouch || ismouse;
            }
        }
        pointerDown(e) {
            if (this.checkEvent(e)) {
                const evt = this.makePointerEvent("down", e);
                const singleFinger = (e instanceof MouseEvent) || (e instanceof TouchEvent && e.touches.length === 1);
                if (!this.isScrolling && singleFinger) {
                    this.maybeClick = true;
                    this.maybeClickX = evt.pageX;
                    this.maybeClickY = evt.pageY;
                }
            }
            return false;
        }
        pointerLeave(e) {
            if (this.checkEvent(e)) {
                this.maybeClick = false;
                this.makePointerEvent("leave", e);
            }
            return false;
        }
        pointerMove(e) {
            if (this.checkEvent(e)) {
                this.makePointerEvent("move", e);
            }
            return false;
        }
        pointerUp(e) {
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
        makePointerEvent(type, e) {
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
            this.eventEmitter.fire(eventType, evt);
            return evt;
        }
        static resizeCanvas(canvas) {
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
        resize() {
            const ret1 = Painter.resizeCanvas(this.viewCanvas);
            const ret2 = Painter.resizeCanvas(this.overlayCanvas);
            const ret3 = Painter.resizeCanvas(this.uiCanvas);
            if (ret1 || ret2) {
                this.canvasOffsetX = ~~((this.viewCanvas.width - this.imageCanvas.width * this.config.scale) / 2);
                this.canvasOffsetY = ~~((this.viewCanvas.height - this.imageCanvas.height * this.config.scale) / 2);
            }
            this.update({ view: (ret1 || ret2), gui: ret3 });
        }
        pointToClient(point) {
            const cr = this.viewCanvas.getBoundingClientRect();
            const sx = (point.x - (cr.left + window.pageXOffset));
            const sy = (point.y - (cr.top + window.pageYOffset));
            return { x: sx, y: sy };
        }
        pointToCanvas(point) {
            const p = this.pointToClient(point);
            p.x = ~~((p.x - this.canvasOffsetX - this.config.scrollX) / this.config.scale);
            p.y = ~~((p.y - this.canvasOffsetY - this.config.scrollY) / this.config.scale);
            return p;
        }
        update({ overlay = false, view = false, gui = false }) {
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
    }
    window.addEventListener("load", () => {
        new Painter(document.body, 512, 512);
    });
})(TsPaint || (TsPaint = {}));
//# sourceMappingURL=tspaint.js.map