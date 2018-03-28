/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.dom.d.ts" />
/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.es2016.d.ts" />
"use strict";
;
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
var TsPaint;
(function (TsPaint) {
    let Events;
    (function (Events) {
        class SingleDispatcher {
            constructor() {
                this.listeners = [];
            }
            clear() {
                this.listeners.length = 0;
                return this;
            }
            on(listener) {
                this.listeners.push(listener);
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
        class EventDispatcher {
            constructor() {
                this.listeners = new Map();
            }
            on(eventName, listener) {
                if (!this.listeners.has(eventName)) {
                    this.listeners.set(eventName, new SingleDispatcher());
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
                    this.listeners.set(eventName, new SingleDispatcher());
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
        Events.EventDispatcher = EventDispatcher;
    })(Events || (Events = {}));
    let GUI;
    (function (GUI) {
        /**
         * UI領域内に点(x,y)があるか判定
         * @param ui {UI}
         * @param x {number}
         * @param y {number}
         */
        function isHit(ui, x, y) {
            const dx = x - ui.left;
            const dy = y - ui.top;
            return (0 <= dx && dx < ui.width) && (0 <= dy && dy < ui.height);
        }
        GUI.isHit = isHit;
        class UIDispatcher extends Events.EventDispatcher {
            constructor() {
                super();
                this.uiTable = new Map();
            }
            add(ui) {
                if (this.uiTable.has(ui)) {
                    return;
                }
                this.uiTable.set(ui, new Map());
                ui.regist(this);
            }
            remove(ui) {
                if (!this.uiTable.has(ui)) {
                    return;
                }
                ui.unregist(this);
                const eventTable = this.uiTable.get(ui);
                this.uiTable.set(ui, null);
                eventTable.forEach((values, key) => {
                    values.forEach((value) => this.off(key, value));
                });
                this.uiTable.delete(ui);
            }
            registUiEvent(ui, event, handler) {
                if (!this.uiTable.has(ui)) {
                    return;
                }
                const eventTable = this.uiTable.get(ui);
                if (!eventTable.has(event)) {
                    eventTable.set(event, []);
                }
                const events = eventTable.get(event);
                events.push(handler);
                this.on(event, handler);
            }
            unregistUiEvent(ui, event, handler) {
                if (!this.uiTable.has(ui)) {
                    return;
                }
                const eventTable = this.uiTable.get(ui);
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
            draw(context) {
                this.uiTable.forEach((value, key) => {
                    if (key.visible) {
                        key.draw(context);
                    }
                });
            }
            // UIに対するクリック/タップ操作を捕捉
            onClick(ui, handler) {
                const hookHandler = (x, y) => {
                    if (!ui.visible || !ui.enable) {
                        return false;
                    }
                    if (!isHit(ui, x, y)) {
                        return false;
                    }
                    let dx = 0;
                    let dy = 0;
                    const onPointerMoveHandler = (_x, _y) => {
                        dx += Math.abs(_x - x);
                        dy += Math.abs(_y - y);
                        return true;
                    };
                    const onPointerUpHandler = (_x, _y) => {
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
            onNcClick(ui, handler) {
                const hookHandler = (x, y) => {
                    if (!ui.visible || !ui.enable) {
                        return false;
                    }
                    if (isHit(ui, x, y)) {
                        return false;
                    }
                    let dx = 0;
                    let dy = 0;
                    const onPointerMoveHandler = (_x, _y) => {
                        dx += Math.abs(_x - x);
                        dy += Math.abs(_y - y);
                        return true;
                    };
                    const onPointerUpHandler = (_x, _y) => {
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
            onSwipe(ui, handler) {
                const hookHandler = (x, y) => {
                    if (!ui.visible || !ui.enable) {
                        return false;
                    }
                    if (!isHit(ui, x, y)) {
                        return false;
                    }
                    const onPointerMoveHandler = (_x, _y) => {
                        let dx = (~~_x - ~~x);
                        let dy = (~~_y - ~~y);
                        x = _x;
                        y = _y;
                        handler(dx, dy, _x - ui.left, _y - ui.top);
                        return true;
                    };
                    const onPointerUpHandler = (x, y) => {
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
        GUI.UIDispatcher = UIDispatcher;
        class TextBox {
            constructor({ left = 0, top = 0, width = 0, height = 0, text = "", edgeColor = `rgb(128,128,128)`, color = `rgb(255,255,255)`, font = undefined, fontColor = `rgb(0,0,0)`, textAlign = "left", textBaseline = "top", visible = true, enable = true }) {
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
            regist(dispatcher) { }
            unregist(dispatcher) { }
        }
        GUI.TextBox = TextBox;
        class Button {
            constructor({ left = 0, top = 0, width = 0, height = 0, text = "", edgeColor = Button.defaultValue.edgeColor, color = Button.defaultValue.color, font = Button.defaultValue.font, fontColor = Button.defaultValue.fontColor, textAlign = Button.defaultValue.textAlign, textBaseline = Button.defaultValue.textBaseline, visible = Button.defaultValue.visible, enable = Button.defaultValue.enable, disableEdgeColor = Button.defaultValue.disableEdgeColor, disableColor = Button.defaultValue.disableColor, disableFontColor = Button.defaultValue.disableFontColor, }) {
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
                this.click = () => { };
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
            }
            regist(dispatcher) {
                const cancelHandler = dispatcher.onClick(this, (...args) => this.click.apply(this, args));
                this.unregist = (d) => cancelHandler();
            }
            unregist(dispatcher) { }
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
        class ImageButton {
            constructor({ left = 0, top = 0, width = 0, height = 0, texture = null, texLeft = 0, texTop = 0, texWidth = 0, texHeight = 0, visible = true, enable = true }) {
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
            draw(context) {
                if (this.texture != null) {
                    context.drawImage(this.texture, this.texLeft, this.texTop, this.texWidth, this.texHeight, this.left, this.top, this.width, this.height);
                }
            }
            regist(dispatcher) {
                const cancelHandler = dispatcher.onClick(this, (...args) => this.click.apply(this, args));
                this.unregist = (d) => cancelHandler();
            }
            unregist(dispatcher) { }
        }
        GUI.ImageButton = ImageButton;
        class ListBox {
            constructor({ left = 0, top = 0, width = 0, height = 0, lineHeight = 12, drawItem = () => { }, getItemCount = () => 0, visible = true, enable = true, scrollbarWidth = 1, space = 2 }) {
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
                this.click = () => { };
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
            regist(dispatcher) {
                const cancelHandlers = [
                    dispatcher.onSwipe(this, (deltaX, deltaY) => {
                        this.scrollValue -= deltaY;
                        this.update();
                    }),
                    dispatcher.onClick(this, (...args) => this.click.apply(this, args))
                ];
                this.unregist = (d) => cancelHandlers.forEach(x => x());
            }
            unregist(dispatcher) { }
        }
        GUI.ListBox = ListBox;
        class HorizontalSlider {
            constructor({ left = 0, top = 0, width = 0, height = 0, sliderWidth = 5, edgeColor = `rgb(128,128,128)`, color = `rgb(255,255,255)`, bgColor = `rgb(192,192,192)`, font = undefined, fontColor = `rgb(0,0,0)`, minValue = 0, maxValue = 0, visible = true, enable = true, }) {
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
            draw(context) {
                const lineWidth = this.width - this.sliderWidth;
                context.fillStyle = this.bgColor;
                context.fillRect(this.left, this.top, this.width, this.height);
                context.fillStyle = this.color;
                context.strokeStyle = this.edgeColor;
                context.fillRect(this.left + ~~(lineWidth * (this.value - this.minValue) / (this.maxValue - this.minValue)), this.top, this.sliderWidth, this.height);
                context.strokeRect(this.left + ~~(lineWidth * (this.value - this.minValue) / (this.maxValue - this.minValue)), this.top, this.sliderWidth, this.height);
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
            regist(dispatcher) {
                const cancelHandler = dispatcher.onSwipe(this, (dx, dy, x, y) => {
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
                });
                this.unregist = (d) => cancelHandler();
            }
            unregist(dispatcher) { }
        }
        GUI.HorizontalSlider = HorizontalSlider;
    })(GUI || (GUI = {}));
    let IPoint;
    (function (IPoint) {
        function rot(point, rad) {
            const cos = Math.cos(rad);
            const sin = Math.sin(rad);
            const rx = point.x * cos - point.y * sin;
            const ry = point.x * sin + point.y * cos;
            return { x: rx, y: ry };
        }
        IPoint.rot = rot;
    })(IPoint || (IPoint = {}));
    class SinglePen {
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
        // https://github.com/miloyip/line
        bresenham(imgData, s, e, rgba) {
            const dx = Math.abs(e.x - s.x);
            const sx = (s.x < e.x) ? 1 : -1;
            const dy = Math.abs(e.y - s.y);
            const sy = s.y < e.y ? 1 : -1;
            let err = ~~((dx > dy ? dx : -dy) / 2);
            for (;;) {
                if (0 <= s.x && s.x < imgData.width && 0 <= s.y && s.y < imgData.height) {
                    const offset = (s.y * imgData.width + s.x) * 4;
                    imgData.data[offset + 0] = rgba[0];
                    imgData.data[offset + 1] = rgba[1];
                    imgData.data[offset + 2] = rgba[2];
                    imgData.data[offset + 3] = rgba[3];
                }
                if (s.x === e.x && s.y === e.y) {
                    break;
                }
                const e2 = err;
                if (e2 > -dx) {
                    err -= dy;
                    s.x += sx;
                }
                if (e2 < dy) {
                    err += dx;
                    s.y += sy;
                }
            }
        }
        draw(config, imgData, offx, offy) {
            const joints = this.joints.map((x) => { return { x: x.x + offx, y: x.y + offy }; });
            if (joints.length === 1) {
                this.bresenham(imgData, joints[0], joints[0], [255, 0, 0, 255]);
            }
            else {
                for (let i = 1; i < joints.length; i++) {
                    this.bresenham(imgData, joints[i - 1], joints[i - 0], [255, 0, 0, 255]);
                }
            }
        }
    }
    ;
    class Painter {
        constructor(root, width, height) {
            this.canvasOffsetX = 0;
            this.canvasOffsetY = 0;
            this.pen = new SinglePen();
            this.config = {
                scale: 2,
                penSize: 5,
                fillStyle: "rgba(0,0,0,1)",
                strokeStyle: "rgba(0,0,0,1)",
                shadowColor: "rgba(0,0,0,1)",
                shadowBlur: 0,
                scrollX: 0,
                scrollY: 0,
            };
            this.root = root;
            this.canvas = document.createElement("canvas");
            this.canvas.width = width;
            this.canvas.height = height;
            this.context = this.canvas.getContext("2d");
            this.context.imageSmoothingEnabled = false;
            this.canvasWork = document.createElement("canvas");
            this.canvasWork.width = width;
            this.canvasWork.height = height;
            this.contextWork = this.canvasWork.getContext("2d");
            this.contextWork.imageSmoothingEnabled = false;
            this.canvasView = document.createElement("canvas");
            this.canvasView.style.position = "absolute";
            this.canvasView.style.left = "0";
            this.canvasView.style.top = "0";
            this.canvasView.style.width = "100%";
            this.canvasView.style.height = "100%";
            this.contextView = this.canvasView.getContext("2d");
            this.contextView.imageSmoothingEnabled = false;
            root.appendChild(this.canvasView);
            this.canvasPreview = document.createElement("canvas");
            this.canvasPreview.style.position = "absolute";
            this.canvasPreview.style.left = "0";
            this.canvasPreview.style.top = "0";
            this.canvasPreview.style.width = "100%";
            this.canvasPreview.style.height = "100%";
            this.contextPreview = this.canvasPreview.getContext("2d");
            this.contextPreview.imageSmoothingEnabled = false;
            root.appendChild(this.canvasPreview);
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
            window.addEventListener("resize", () => this.resize());
            this.setupMouseEvent();
            {
                let count = 0;
                const uiButton = new GUI.Button({
                    left: 0,
                    top: 0,
                    width: 150,
                    height: 30,
                    text: () => `demo cnt = ${count}`,
                });
                uiButton.click = (x, y) => {
                    count++;
                    this.update({ gui: true });
                };
                this.uiDispacher.add(uiButton);
            }
            this.resize();
        }
        setupMouseEvent() {
            const onPenMove = (e) => {
                e.preventDefault();
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                p.x = ~~((p.x - this.canvasOffsetX - this.config.scrollX) / this.config.scale);
                p.y = ~~((p.y - this.canvasOffsetY - this.config.scrollY) / this.config.scale);
                this.pen.move(p);
                this.contextWork.clearRect(0, 0, this.canvasWork.width, this.canvasWork.height);
                const imgData = this.contextWork.getImageData(0, 0, this.canvasWork.width, this.canvasWork.height);
                this.pen.draw(this.config, imgData, 0, 0);
                this.contextWork.putImageData(imgData, 0, 0);
                this.update({ preview: true });
            };
            const onPenUp = (e) => {
                e.preventDefault();
                document.addEventListener("mousedown", onMouseDown);
                document.removeEventListener("mousemove", onPenMove);
                document.removeEventListener("mouseup", onPenUp);
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                p.x = ~~((p.x - this.canvasOffsetX - this.config.scrollX) / this.config.scale);
                p.y = ~~((p.y - this.canvasOffsetY - this.config.scrollY) / this.config.scale);
                this.pen.up(p);
                const imgData = this.context.getImageData(0, 0, this.canvas.width, this.canvas.height);
                this.pen.draw(this.config, imgData, 0, 0);
                this.context.putImageData(imgData, 0, 0);
                this.update({ preview: true, view: true });
            };
            const onPenDown = (e) => {
                e.preventDefault();
                document.removeEventListener("mousedown", onPenDown);
                document.addEventListener("mousemove", onPenMove);
                document.addEventListener("mouseup", onPenUp);
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                p.x = ~~((p.x - this.canvasOffsetX - this.config.scrollX) / this.config.scale);
                p.y = ~~((p.y - this.canvasOffsetY - this.config.scrollY) / this.config.scale);
                this.pen.down(p);
                this.contextWork.clearRect(0, 0, this.canvasWork.width, this.canvasWork.height);
                const imgData = this.contextWork.getImageData(0, 0, this.canvasWork.width, this.canvasWork.height);
                this.pen.draw(this.config, imgData, 0, 0);
                this.contextWork.putImageData(imgData, 0, 0);
                this.update({ preview: true });
            };
            let scrollStartPos = { x: 0, y: 0 };
            const onScrolling = (e) => {
                e.preventDefault();
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                this.config.scrollX += p.x - scrollStartPos.x;
                this.config.scrollY += p.y - scrollStartPos.y;
                scrollStartPos = p;
                this.update({ preview: true, view: true });
            };
            const onScrollEnd = (e) => {
                e.preventDefault();
                document.addEventListener("mousedown", onMouseDown);
                document.removeEventListener("mousemove", onScrolling);
                document.removeEventListener("mouseup", onScrollEnd);
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                this.config.scrollX += p.x - scrollStartPos.x;
                this.config.scrollY += p.y - scrollStartPos.y;
                scrollStartPos = p;
                this.update({ preview: true, view: true });
            };
            const onScrollStart = (e) => {
                e.preventDefault();
                document.removeEventListener("mousedown", onScrollStart);
                document.addEventListener("mousemove", onScrolling);
                document.addEventListener("mouseup", onScrollEnd);
                scrollStartPos = this.pointToClient({ x: e.pageX, y: e.pageY });
            };
            const onMouseDown = (e) => {
                if (e.button === 0) {
                    const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                    if (this.uiDispacher.fire("pointerdown", p.x, p.y)) {
                        e.preventDefault();
                    }
                    else {
                        document.removeEventListener("mousedown", onMouseDown);
                        if (e.ctrlKey) {
                            onScrollStart(e);
                        }
                        else {
                            onPenDown(e);
                        }
                    }
                }
                else {
                    e.preventDefault();
                }
            };
            document.addEventListener("mousedown", onMouseDown);
            document.addEventListener("mousemove", (e) => {
                e.preventDefault();
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                this.uiDispacher.fire("pointermove", p.x, p.y);
            });
            document.addEventListener("mouseup", (e) => {
                e.preventDefault();
                const p = this.pointToClient({ x: e.pageX, y: e.pageY });
                this.uiDispacher.fire("pointerup", p.x, p.y);
            });
            document.addEventListener("wheel", (e) => {
                e.preventDefault();
                if (e.ctrlKey) {
                    if (e.wheelDelta > 0) {
                        if (this.config.scale < 16) {
                            this.config.scrollX = (this.config.scrollX * (this.config.scale + 1) / (this.config.scale));
                            this.config.scrollY = (this.config.scrollY * (this.config.scale + 1) / (this.config.scale));
                            this.config.scale += 1;
                            this.canvasOffsetX = ~~((this.canvasView.width - this.canvas.width * this.config.scale) / 2);
                            this.canvasOffsetY = ~~((this.canvasView.height - this.canvas.height * this.config.scale) / 2);
                            this.update({ preview: true, view: true });
                        }
                    }
                    else if (e.wheelDelta < 0) {
                        if (this.config.scale > 1) {
                            this.config.scrollX = (this.config.scrollX * (this.config.scale - 1) / (this.config.scale));
                            this.config.scrollY = (this.config.scrollY * (this.config.scale - 1) / (this.config.scale));
                            this.config.scale -= 1;
                            this.canvasOffsetX = ~~((this.canvasView.width - this.canvas.width * this.config.scale) / 2);
                            this.canvasOffsetY = ~~((this.canvasView.height - this.canvas.height * this.config.scale) / 2);
                            this.update({ preview: true, view: true });
                        }
                    }
                }
            });
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
            const ret1 = Painter.resizeCanvas(this.canvasView);
            const ret2 = Painter.resizeCanvas(this.canvasPreview);
            const ret3 = Painter.resizeCanvas(this.uiCanvas);
            if (ret1 || ret2) {
                this.canvasOffsetX = ~~((this.canvasView.width - this.canvas.width * this.config.scale) / 2);
                this.canvasOffsetY = ~~((this.canvasView.height - this.canvas.height * this.config.scale) / 2);
            }
            this.update({ view: (ret1 || ret2), gui: ret3 });
        }
        pointToClient(point) {
            const cr = this.canvasView.getBoundingClientRect();
            const sx = (point.x - (cr.left + window.pageXOffset));
            const sy = (point.y - (cr.top + window.pageYOffset));
            return { x: sx, y: sy };
        }
        update({ preview = false, view = false, gui = false }) {
            if (preview) {
                this.contextPreview.clearRect(0, 0, this.canvasPreview.width, this.canvasPreview.height);
                this.contextPreview.imageSmoothingEnabled = false;
                this.contextPreview.drawImage(this.canvasWork, 0, 0, this.canvasWork.width, this.canvasWork.height, this.canvasOffsetX + this.config.scrollX, this.canvasOffsetY + this.config.scrollY, this.canvasWork.width * this.config.scale, this.canvasWork.height * this.config.scale);
            }
            if (view) {
                this.contextView.clearRect(0, 0, this.canvasView.width, this.canvasView.height);
                this.contextView.fillStyle = "rgb(198,208,224)";
                this.contextView.fillRect(0, 0, this.canvasView.width, this.canvasView.height);
                this.contextView.fillStyle = "rgb(255,255,255)";
                this.contextView.fillRect(this.canvasOffsetX + this.config.scrollX, this.canvasOffsetY + this.config.scrollY, this.canvas.width * this.config.scale, this.canvas.height * this.config.scale);
                this.contextView.imageSmoothingEnabled = false;
                this.contextView.drawImage(this.canvas, 0, 0, this.canvas.width, this.canvas.height, this.canvasOffsetX + this.config.scrollX, this.canvasOffsetY + this.config.scrollY, this.canvas.width * this.config.scale, this.canvas.height * this.config.scale);
            }
            if (gui) {
                this.uiContext.clearRect(0, 0, this.uiCanvas.width, this.uiCanvas.height);
                this.uiContext.imageSmoothingEnabled = false;
                this.uiDispacher.draw(this.uiContext);
            }
        }
    }
    window.addEventListener("load", () => {
        new Painter(document.body, 128, 128);
    });
})(TsPaint || (TsPaint = {}));
//# sourceMappingURL=tspaint.js.map