"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var Dispatcher;
(function (Dispatcher) {
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
            temp.forEach((dispatcher) => dispatcher.apply(this, args));
            return this;
        }
        one(listener) {
            var func = (...args) => {
                var result = listener.apply(this, args);
                this.off(func);
                return result;
            };
            this.on(func);
            return this;
        }
    }
    Dispatcher.SingleDispatcher = SingleDispatcher;
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
                dispatcher.fire.apply(dispatcher, args);
            }
            return this;
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
    Dispatcher.EventDispatcher = EventDispatcher;
})(Dispatcher || (Dispatcher = {}));
var Game;
(function (Game) {
    var GUI;
    (function (GUI) {
        function isHit(ui, x, y) {
            const dx = x - ui.left;
            const dy = y - ui.top;
            return (0 <= dx && dx < ui.width) && (0 <= dy && dy < ui.height);
        }
        GUI.isHit = isHit;
        class UIDispatcher extends Dispatcher.EventDispatcher {
            constructor() {
                super();
            }
            onClick(ui, handler) {
                this.on("pointerdown", (x, y) => {
                    if (!Game.getScreen().pagePointContainScreen(x, y)) {
                        return;
                    }
                    const [cx, cy] = Game.getScreen().pagePointToScreenPoint(x, y);
                    if (!isHit(ui, cx, cy)) {
                        return;
                    }
                    let dx = 0;
                    let dy = 0;
                    const onPointerMoveHandler = (x, y) => {
                        const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                        dx += Math.abs(_x - cx);
                        dy += Math.abs(_y - cy);
                    };
                    const onPointerUpHandler = (x, y) => {
                        this.off("pointermove", onPointerMoveHandler);
                        this.off("pointerup", onPointerUpHandler);
                        if (dx + dy < 5) {
                            const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                            handler(_x - ui.left, _y - ui.top);
                        }
                    };
                    this.on("pointermove", onPointerMoveHandler);
                    this.on("pointerup", onPointerUpHandler);
                });
            }
            onNcClick(ui, handler) {
                this.on("pointerdown", (x, y) => {
                    if (!Game.getScreen().pagePointContainScreen(x, y)) {
                        return;
                    }
                    const [cx, cy] = Game.getScreen().pagePointToScreenPoint(x, y);
                    if (isHit(ui, cx, cy)) {
                        return;
                    }
                    let dx = 0;
                    let dy = 0;
                    const onPointerMoveHandler = (x, y) => {
                        const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                        dx += Math.abs(_x - cx);
                        dy += Math.abs(_y - cy);
                    };
                    const onPointerUpHandler = (x, y) => {
                        this.off("pointermove", onPointerMoveHandler);
                        this.off("pointerup", onPointerUpHandler);
                        if (dx + dy < 5) {
                            const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                            handler(_x - ui.left, _y - ui.top);
                        }
                    };
                    this.on("pointermove", onPointerMoveHandler);
                    this.on("pointerup", onPointerUpHandler);
                });
            }
            onSwipe(ui, handler) {
                this.on("pointerdown", (x, y) => {
                    if (!Game.getScreen().pagePointContainScreen(x, y)) {
                        return;
                    }
                    let [cx, cy] = Game.getScreen().pagePointToScreenPoint(x, y);
                    if (!isHit(ui, cx, cy)) {
                        return;
                    }
                    const onPointerMoveHandler = (x, y) => {
                        const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                        let dx = (~~_x - ~~cx);
                        let dy = (~~_y - ~~cy);
                        cx = _x;
                        cy = _y;
                        handler(dx, dy, _x - ui.left, _y - ui.top);
                    };
                    const onPointerUpHandler = (x, y) => {
                        this.off("pointermove", onPointerMoveHandler);
                        this.off("pointerup", onPointerUpHandler);
                    };
                    this.on("pointermove", onPointerMoveHandler);
                    this.on("pointerup", onPointerUpHandler);
                    handler(0, 0, cx - ui.left, cy - ui.top);
                });
            }
        }
        GUI.UIDispatcher = UIDispatcher;
        class TextBox {
            constructor(left, top, width, height, { text = "", edgeColor = `rgb(128,128,128)`, color = `rgb(255,255,255)`, font = undefined, fontColor = `rgb(0,0,0)`, textAlign = "left", textBaseline = "top", }) {
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
            }
            draw() {
                Game.getScreen().beginPath();
                Game.getScreen().moveTo(9 - 0.5, 1 - 0.5);
                Game.getScreen().bezierCurveTo(1 - 0.5, 1 - 0.5, 1 - 0.5, 42 - 0.5, 9 - 0.5, 42 - 0.5);
                Game.getScreen().lineTo(242 - 0.5, 42 - 0.5);
                Game.getScreen().bezierCurveTo(250 - 0.5, 42 - 0.5, 250 - 0.5, 1 - 0.5, 242 - 0.5, 1 - 0.5);
                Game.getScreen().lineTo(9 - 0.5, 1 - 0.5);
                Game.getScreen().closePath();
                Game.getScreen().fillStyle = this.color;
                Game.getScreen().fill();
                Game.getScreen().strokeStyle = this.edgeColor;
                Game.getScreen().lineWidth = 1;
                Game.getScreen().stroke();
                Game.getScreen().font = this.font;
                Game.getScreen().fillStyle = this.fontColor;
                const metrics = Game.getScreen().measureText(this.text);
                Game.getScreen().textAlign = this.textAlign;
                Game.getScreen().textBaseline = this.textBaseline;
                this.text.split(/\n/).forEach((x, i) => {
                    Game.getScreen().fillText(x, this.left + 8, this.top + i * (10 + 1) + 8);
                });
            }
        }
        GUI.TextBox = TextBox;
        class Button {
            constructor(left, top, width, height, { text = "button", edgeColor = `rgb(128,128,128)`, color = `rgb(255,255,255)`, font = undefined, fontColor = `rgb(0,0,0)`, textAlign = "left", textBaseline = "top", }) {
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
            }
            draw() {
                Game.getScreen().fillStyle = this.color;
                Game.getScreen().fillRect(this.left - 0.5, this.top - 0.5, this.width, this.height);
                Game.getScreen().strokeStyle = this.edgeColor;
                Game.getScreen().lineWidth = 1;
                Game.getScreen().strokeRect(this.left - 0.5, this.top - 0.5, this.width, this.height);
                Game.getScreen().font = this.font;
                Game.getScreen().fillStyle = this.fontColor;
                const text = (this.text instanceof Function) ? this.text.call(this) : this.text;
                const metrics = Game.getScreen().measureText(text);
                Game.getScreen().textAlign = this.textAlign;
                Game.getScreen().textBaseline = this.textBaseline;
                text.split(/\n/).forEach((x, i) => {
                    Game.getScreen().fillText(x, this.left + 2, this.top + i * (10 + 1) + 2);
                });
            }
        }
        GUI.Button = Button;
        class ListBox {
            constructor(left, top, width, height, { lineHeight = 12, drawItem = () => { }, getItemCount = () => 0, }) {
                this.left = left;
                this.top = top;
                this.width = width;
                this.height = height;
                this.lineHeight = lineHeight;
                this.drawItem = drawItem;
                this.getItemCount = getItemCount;
                this.scrollValue = 0;
            }
            update() {
                var contentHeight = this.getItemCount() * this.lineHeight;
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
            draw() {
                let sy = -(~~this.scrollValue % this.lineHeight);
                let index = ~~((~~this.scrollValue) / this.lineHeight);
                let itemCount = this.getItemCount();
                let drawResionHeight = this.height - sy;
                for (;;) {
                    if (sy >= this.height) {
                        break;
                    }
                    if (index >= itemCount) {
                        break;
                    }
                    Game.getScreen().save();
                    Game.getScreen().beginPath();
                    Game.getScreen().rect(this.left - 1, Math.max(this.top, this.top + sy), this.width + 1, Math.min(drawResionHeight, this.lineHeight));
                    Game.getScreen().clip();
                    this.drawItem(this.left, this.top + sy, this.width, this.lineHeight, index);
                    Game.getScreen().restore();
                    drawResionHeight -= this.lineHeight;
                    sy += this.lineHeight;
                    index++;
                }
            }
            getItemIndexByPosition(x, y) {
                if (x < 0 || this.width <= x || y < 0 || this.height <= y) {
                    return -1;
                }
                const index = ~~((y + this.scrollValue) / this.lineHeight);
                if (index < 0 || index >= this.getItemCount()) {
                    return -1;
                }
                else {
                    return index;
                }
            }
        }
        GUI.ListBox = ListBox;
        class HorizontalSlider {
            constructor(left, top, width, height, { sliderWidth = 5, edgeColor = `rgb(128,128,128)`, color = `rgb(255,255,255)`, bgColor = `rgb(192,192,192)`, font = undefined, fontColor = `rgb(0,0,0)`, minValue = 0, maxValue = 0, }) {
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
            }
            draw() {
                const lineWidth = this.width - this.sliderWidth;
                Game.getScreen().fillStyle = this.bgColor;
                Game.getScreen().fillRect(this.left - 0.5, this.top - 0.5, this.width, this.height);
                Game.getScreen().fillStyle = this.color;
                Game.getScreen().strokeStyle = this.edgeColor;
                Game.getScreen().fillRect(this.left + ~~(lineWidth * (this.value - this.minValue) / (this.maxValue - this.minValue)) - 0.5, this.top - 0.5, this.sliderWidth, this.height);
                Game.getScreen().strokeRect(this.left + ~~(lineWidth * (this.value - this.minValue) / (this.maxValue - this.minValue)) - 0.5, this.top - 0.5, this.sliderWidth, this.height);
            }
            swipe(lx) {
                const rangeSize = this.maxValue - this.minValue;
                if (rangeSize == 0) {
                    this.value = this.minValue;
                }
                else if (lx < 0) {
                    this.value = this.minValue;
                }
                else if (lx >= this.width) {
                    this.value = this.maxValue;
                }
                else {
                    this.value = Math.trunc((lx * rangeSize) / this.width) + this.minValue;
                }
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
    })(GUI = Game.GUI || (Game.GUI = {}));
})(Game || (Game = {}));
var Scene;
(function (Scene) {
    function* shopBuyItem(opt) {
        const uiComponents = [];
        const dispatcher = new Game.GUI.UIDispatcher();
        const caption = new Game.GUI.TextBox(1, 1, 250, 42, {
            text: "�w����\n���܂��܂ȕ���E�A�C�e���̍w�����ł��܂��B",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        const captionMonay = new Game.GUI.Button(135, 46, 108, 16, {
            text: `������F${('            ' + 150 + 'G').substr(-13)}`,
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        let exitScene = false;
        const btnExit = new Game.GUI.Button(8, 16 * 9 + 46, 112, 16, {
            text: "�߂�",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.onClick(btnExit, (x, y) => {
            exitScene = true;
            Game.getSound().reqPlayChannel("cursor");
        });
        const itemlist = [
            "�݂���",
            "���",
            "�o�i�i",
            "�I�����W",
            "�p�C�i�b�v��",
            "�ڂ񂽂�",
            "�L�E�C",
            "�p�p�C��",
            "�}���S�[",
            "�R�R�i�b�c",
            "�Ԃǂ�",
            "�Ȃ�",
            "������",
            "�h���S���t���[�c",
        ];
        let selectedItem = -1;
        const listBox = new Game.GUI.ListBox(8, 46, 112, 8 * 16, {
            lineHeight: 16,
            getItemCount: () => itemlist.length,
            drawItem: (left, top, width, height, index) => {
                if (selectedItem == index) {
                    Game.getScreen().fillStyle = `rgb(24,196,195)`;
                }
                else {
                    Game.getScreen().fillStyle = `rgb(24,133,196)`;
                }
                Game.getScreen().fillRect(left - 0.5, top + 1 - 0.5, width, height - 2);
                Game.getScreen().strokeStyle = `rgb(12,34,98)`;
                Game.getScreen().lineWidth = 1;
                Game.getScreen().strokeRect(left - 0.5, top + 1 - 0.5, width, height - 2);
                Game.getScreen().font = "10px 'PixelMplus10-Regular'";
                Game.getScreen().fillStyle = `rgb(255,255,255)`;
                const metrics = Game.getScreen().measureText(itemlist[index]);
                Game.getScreen().textAlign = "left";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(itemlist[index], left + 9, top + 3);
            }
        });
        dispatcher.onSwipe(listBox, (deltaX, deltaY) => {
            listBox.scrollValue -= deltaY;
            listBox.update();
        });
        dispatcher.onClick(listBox, (x, y) => {
            selectedItem = listBox.getItemIndexByPosition(x, y);
            Game.getSound().reqPlayChannel("cursor");
        });
        this.draw = () => {
            Game.getScreen().drawImage(Game.getScreen().texture("shop/bg"), 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight, 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            Game.getScreen().drawImage(Game.getScreen().texture("shop/J11"), 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight, 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            caption.draw();
            btnExit.draw();
            listBox.draw();
            captionMonay.draw();
        };
        yield (delta, ms) => {
            if (Game.getInput().isDown()) {
                dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isMove()) {
                dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isUp()) {
                dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (exitScene) {
                this.next();
            }
        };
        Game.getSceneManager().pop();
    }
    function* shop(opt) {
        const uiComponents = [];
        const dispatcher = new Game.GUI.UIDispatcher();
        const caption = new Game.GUI.TextBox(1, 1, 250, 42, {
            text: "�w����\n���܂��܂ȕ���E�A�C�e���̍w�����ł��܂��B",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        const btnBuy = new Game.GUI.Button(8, 20 * 0 + 46, 112, 16, {
            text: "�A�C�e���w��",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.onClick(btnBuy, (x, y) => {
            Game.getSceneManager().push(shopBuyItem, opt);
            Game.getSound().reqPlayChannel("cursor");
        });
        const btnSell = new Game.GUI.Button(8, 20 * 1 + 46, 112, 16, {
            text: "�A�C�e�����p",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.onClick(btnBuy, (x, y) => {
            Game.getSound().reqPlayChannel("cursor");
        });
        const captionMonay = new Game.GUI.Button(135, 46, 108, 16, {
            text: `������F${('            ' + 150 + 'G').substr(-13)}`,
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        const hoverSlider = new Game.GUI.HorizontalSlider(135 + 14, 80, 108 - 28, 16, {
            sliderWidth: 5,
            updownButtonWidth: 10,
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            minValue: 0,
            maxValue: 100,
        });
        dispatcher.onSwipe(hoverSlider, (dx, dy, x, y) => {
            hoverSlider.swipe(x);
            hoverSlider.update();
        });
        const btnSliderDown = new Game.GUI.Button(135, 80, 14, 16, {
            text: "�|",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.onClick(btnSliderDown, (x, y) => {
            hoverSlider.value -= 1;
            hoverSlider.update();
            Game.getSound().reqPlayChannel("cursor");
        });
        const btnSliderUp = new Game.GUI.Button(243 - 14, 80, 14, 16, {
            text: "�{",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.onClick(btnSliderUp, (x, y) => {
            hoverSlider.value += 1;
            hoverSlider.update();
            Game.getSound().reqPlayChannel("cursor");
        });
        const captionBuyCount = new Game.GUI.Button(135, 64, 108, 16, {
            text: () => `�w�����F${('           ' + hoverSlider.value + '��').substr(-12)}`,
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        this.draw = () => {
            Game.getScreen().drawImage(Game.getScreen().texture("shop/bg"), 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight, 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            Game.getScreen().drawImage(Game.getScreen().texture("shop/J11"), 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight, 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            caption.draw();
            btnBuy.draw();
            btnSell.draw();
            captionMonay.draw();
            hoverSlider.draw();
            btnSliderDown.draw();
            btnSliderUp.draw();
            captionBuyCount.draw();
        };
        yield (delta, ms) => {
            if (Game.getInput().isDown()) {
                dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isMove()) {
                dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isUp()) {
                dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
            }
        };
        Game.getSceneManager().pop();
        Game.getSceneManager().push(Scene.title);
    }
    Scene.shop = shop;
})(Scene || (Scene = {}));
class XorShift {
    constructor(w = 0 | Date.now(), x, y, z) {
        if (x === undefined) {
            x = (0 | (w << 13));
        }
        if (y === undefined) {
            y = (0 | ((w >>> 9) ^ (x << 6)));
        }
        if (z === undefined) {
            z = (0 | (y >>> 7));
        }
        this.seeds = { x: x >>> 0, y: y >>> 0, z: z >>> 0, w: w >>> 0 };
        this.randCount = 0;
        this.generator = this.randGen(w, x, y, z);
    }
    *randGen(w, x, y, z) {
        let t;
        for (;;) {
            t = x ^ (x << 11);
            x = y;
            y = z;
            z = w;
            yield w = ((w ^ (w >>> 19)) ^ (t ^ (t >>> 8))) >>> 0;
        }
    }
    rand() {
        this.randCount = 0 | this.randCount + 1;
        return this.generator.next().value;
    }
    randInt(min = 0, max = 0x7FFFFFFF) {
        return 0 | this.rand() % (max + 1 - min) + min;
    }
    randFloat(min = 0, max = 1) {
        return Math.fround(this.rand() % 0xFFFF / 0xFFFF) * (max - min) + min;
    }
    shuffle(target) {
        const arr = target.concat();
        for (let i = 0; i <= arr.length - 2; i = 0 | i + 1) {
            const r = this.randInt(i, arr.length - 1);
            const tmp = arr[i];
            arr[i] = arr[r];
            arr[r] = tmp;
        }
        return arr;
    }
    getWeightedValue(data) {
        const keys = Object.keys(data);
        const total = keys.reduce((s, x) => s + data[x], 0);
        const random = this.randInt(0, total);
        let part = 0;
        for (const id of keys) {
            part += data[id];
            if (random < part) {
                return id;
            }
        }
        return keys[keys.length - 1];
    }
    static default() {
        return new XorShift(XorShift.defaults.w, XorShift.defaults.x, XorShift.defaults.y, XorShift.defaults.z);
    }
}
XorShift.defaults = {
    x: 123456789,
    y: 362436069,
    z: 521288629,
    w: 88675123
};
function ajax(uri, type) {
    return new Promise((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        xhr.responseType = type;
        xhr.open("GET", uri, true);
        xhr.onerror = (ev) => {
            reject(ev);
        };
        xhr.onload = () => {
            resolve(xhr);
        };
        xhr.send();
    });
}
function getDirectory(path) {
    return path.substring(0, path.lastIndexOf("/"));
}
function normalizePath(path) {
    return path.split("/").reduce((s, x) => {
        if (x === "..") {
            if (s.length > 1) {
                s.pop();
            }
            else {
                throw new Error("bad path");
            }
        }
        else if (x === ".") {
            if (s.length === 0) {
                s.push(x);
            }
        }
        else {
            s.push(x);
        }
        return s;
    }, new Array()).join("/");
}
var Game;
(function (Game) {
    class Video {
        constructor(config) {
            this.canvasElement = document.getElementById(config.id);
            if (!this.canvasElement) {
                throw new Error("your browser is not support canvas.");
            }
            this.id = config.id;
            this.offscreenWidth = config.offscreenWidth;
            this.offscreenHeight = config.offscreenHeight;
            this.scaleX = config.scaleX;
            this.scaleY = config.scaleY;
            this.canvasElement.width = this.offscreenWidth * this.scaleX;
            this.canvasElement.height = this.offscreenHeight * this.scaleY;
            this.canvasRenderingContext2D = this.canvasElement.getContext("2d");
            if (!this.canvasRenderingContext2D) {
                throw new Error("your browser is not support CanvasRenderingContext2D.");
            }
            this.images = new Map();
            this.arc = this.canvasRenderingContext2D.arc.bind(this.canvasRenderingContext2D);
            this.arcTo = this.canvasRenderingContext2D.arcTo.bind(this.canvasRenderingContext2D);
            this.beginPath = this.canvasRenderingContext2D.beginPath.bind(this.canvasRenderingContext2D);
            this.bezierCurveTo = this.canvasRenderingContext2D.bezierCurveTo.bind(this.canvasRenderingContext2D);
            this.clearRect = this.canvasRenderingContext2D.clearRect.bind(this.canvasRenderingContext2D);
            this.clip = this.canvasRenderingContext2D.clip.bind(this.canvasRenderingContext2D);
            this.closePath = this.canvasRenderingContext2D.closePath.bind(this.canvasRenderingContext2D);
            this.createImageData = this.canvasRenderingContext2D.createImageData.bind(this.canvasRenderingContext2D);
            this.createLinearGradient = this.canvasRenderingContext2D.createLinearGradient.bind(this.canvasRenderingContext2D);
            this.createPattern = this.canvasRenderingContext2D.createPattern.bind(this.canvasRenderingContext2D);
            this.createRadialGradient = this.canvasRenderingContext2D.createRadialGradient.bind(this.canvasRenderingContext2D);
            this.drawImage = this.canvasRenderingContext2D.drawImage.bind(this.canvasRenderingContext2D);
            this.fill = this.canvasRenderingContext2D.fill.bind(this.canvasRenderingContext2D);
            this.fillRect = this.canvasRenderingContext2D.fillRect.bind(this.canvasRenderingContext2D);
            this.fillText = this.canvasRenderingContext2D.fillText.bind(this.canvasRenderingContext2D);
            this.getImageData = this.canvasRenderingContext2D.getImageData.bind(this.canvasRenderingContext2D);
            this.getLineDash = this.canvasRenderingContext2D.getLineDash.bind(this.canvasRenderingContext2D);
            this.isPointInPath = this.canvasRenderingContext2D.isPointInPath.bind(this.canvasRenderingContext2D);
            this.lineTo = this.canvasRenderingContext2D.lineTo.bind(this.canvasRenderingContext2D);
            this.measureText = this.canvasRenderingContext2D.measureText.bind(this.canvasRenderingContext2D);
            this.moveTo = this.canvasRenderingContext2D.moveTo.bind(this.canvasRenderingContext2D);
            this.putImageData = this.canvasRenderingContext2D.putImageData.bind(this.canvasRenderingContext2D);
            this.quadraticCurveTo = this.canvasRenderingContext2D.quadraticCurveTo.bind(this.canvasRenderingContext2D);
            this.rect = this.canvasRenderingContext2D.rect.bind(this.canvasRenderingContext2D);
            this.restore = this.canvasRenderingContext2D.restore.bind(this.canvasRenderingContext2D);
            this.rotate = this.canvasRenderingContext2D.rotate.bind(this.canvasRenderingContext2D);
            this.save = this.canvasRenderingContext2D.save.bind(this.canvasRenderingContext2D);
            this.scale = this.canvasRenderingContext2D.scale.bind(this.canvasRenderingContext2D);
            this.setLineDash = this.canvasRenderingContext2D.setLineDash.bind(this.canvasRenderingContext2D);
            this.setTransform = this.canvasRenderingContext2D.setTransform.bind(this.canvasRenderingContext2D);
            this.stroke = this.canvasRenderingContext2D.stroke.bind(this.canvasRenderingContext2D);
            this.strokeRect = this.canvasRenderingContext2D.strokeRect.bind(this.canvasRenderingContext2D);
            this.strokeText = this.canvasRenderingContext2D.strokeText.bind(this.canvasRenderingContext2D);
            this.transform = this.canvasRenderingContext2D.transform.bind(this.canvasRenderingContext2D);
            this.translate = this.canvasRenderingContext2D.translate.bind(this.canvasRenderingContext2D);
            this.ellipse = this.canvasRenderingContext2D.ellipse.bind(this.canvasRenderingContext2D);
        }
        get canvas() { return this.canvasRenderingContext2D.canvas; }
        get fillStyle() { return this.canvasRenderingContext2D.fillStyle; }
        set fillStyle(value) { this.canvasRenderingContext2D.fillStyle = value; }
        get font() { return this.canvasRenderingContext2D.font; }
        set font(value) { this.canvasRenderingContext2D.font = value; }
        get globalAlpha() { return this.canvasRenderingContext2D.globalAlpha; }
        set globalAlpha(value) { this.canvasRenderingContext2D.globalAlpha = value; }
        get globalCompositeOperation() { return this.canvasRenderingContext2D.globalCompositeOperation; }
        set globalCompositeOperation(value) { this.canvasRenderingContext2D.globalCompositeOperation = value; }
        get lineCap() { return this.canvasRenderingContext2D.lineCap; }
        set lineCap(value) { this.canvasRenderingContext2D.lineCap = value; }
        get lineDashOffset() { return this.canvasRenderingContext2D.lineDashOffset; }
        set lineDashOffset(value) { this.canvasRenderingContext2D.lineDashOffset = value; }
        get lineJoin() { return this.canvasRenderingContext2D.lineJoin; }
        set lineJoin(value) { this.canvasRenderingContext2D.lineJoin = value; }
        get lineWidth() { return this.canvasRenderingContext2D.lineWidth; }
        set lineWidth(value) { this.canvasRenderingContext2D.lineWidth = value; }
        get miterLimit() { return this.canvasRenderingContext2D.miterLimit; }
        set miterLimit(value) { this.canvasRenderingContext2D.miterLimit = value; }
        get shadowBlur() { return this.canvasRenderingContext2D.shadowBlur; }
        set shadowBlur(value) { this.canvasRenderingContext2D.shadowBlur = value; }
        get shadowColor() { return this.canvasRenderingContext2D.shadowColor; }
        set shadowColor(value) { this.canvasRenderingContext2D.shadowColor = value; }
        get shadowOffsetX() { return this.canvasRenderingContext2D.shadowOffsetX; }
        set shadowOffsetX(value) { this.canvasRenderingContext2D.shadowOffsetX = value; }
        get shadowOffsetY() { return this.canvasRenderingContext2D.shadowOffsetY; }
        set shadowOffsetY(value) { this.canvasRenderingContext2D.shadowOffsetY = value; }
        get strokeStyle() { return this.canvasRenderingContext2D.strokeStyle; }
        set strokeStyle(value) { this.canvasRenderingContext2D.strokeStyle = value; }
        get textAlign() { return this.canvasRenderingContext2D.textAlign; }
        set textAlign(value) { this.canvasRenderingContext2D.textAlign = value; }
        get textBaseline() { return this.canvasRenderingContext2D.textBaseline; }
        set textBaseline(value) { this.canvasRenderingContext2D.textBaseline = value; }
        get imageSmoothingEnabled() {
            if ('imageSmoothingEnabled' in this.canvasRenderingContext2D) {
                return this.canvasRenderingContext2D.imageSmoothingEnabled;
            }
            if ('mozImageSmoothingEnabled' in this.canvasRenderingContext2D) {
                return this.canvasRenderingContext2D.mozImageSmoothingEnabled;
            }
            if ('webkitImageSmoothingEnabled' in this.canvasRenderingContext2D) {
                return this.canvasRenderingContext2D.webkitImageSmoothingEnabled;
            }
            return false;
        }
        set imageSmoothingEnabled(value) {
            if ('imageSmoothingEnabled' in this.canvasRenderingContext2D) {
                this.canvasRenderingContext2D.imageSmoothingEnabled = value;
                return;
            }
            if ('mozImageSmoothingEnabled' in this.canvasRenderingContext2D) {
                this.canvasRenderingContext2D.mozImageSmoothingEnabled = value;
                return;
            }
            if ('webkitImageSmoothingEnabled' in this.canvasRenderingContext2D) {
                this.canvasRenderingContext2D.webkitImageSmoothingEnabled = value;
                return;
            }
        }
        drawTile(image, offsetX, offsetY, sprite, spritesize, tile) {
            for (let y = 0; y < tile.height; y++) {
                for (let x = 0; x < tile.width; x++) {
                    const chip = tile.value(x, y);
                    this.drawImage(image, sprite[chip][0] * spritesize[0], sprite[chip][1] * spritesize[1], spritesize[0], spritesize[1], offsetX + x * spritesize[0], offsetY + y * spritesize[1], spritesize[0], spritesize[1]);
                }
            }
        }
        get width() {
            return this.canvasRenderingContext2D.canvas.width;
        }
        get height() {
            return this.canvasRenderingContext2D.canvas.height;
        }
        loadImage(asserts, startCallback = () => { }, endCallback = () => { }) {
            return Promise.all(Object.keys(asserts).map((x) => new Promise((resolve, reject) => {
                startCallback(x);
                const img = new Image();
                img.onload = () => {
                    this.images.set(x, img);
                    endCallback(x);
                    resolve();
                };
                img.onerror = () => {
                    const msg = `ファイル ${asserts[x]}のロードに失敗。`;
                    console.error(msg);
                    reject(msg);
                };
                img.src = asserts[x];
            }))).then(() => {
                return true;
            });
        }
        texture(id) {
            return this.images.get(id);
        }
        begin() {
            Game.getScreen().save();
            Game.getScreen().clearRect(0, 0, this.width, this.height);
            Game.getScreen().scale(this.scaleX, this.scaleY);
            Game.getScreen().save();
        }
        end() {
            Game.getScreen().restore();
            Game.getScreen().restore();
        }
        pagePointToScreenPoint(x, y) {
            const cr = this.canvasRenderingContext2D.canvas.getBoundingClientRect();
            const sx = (x - (cr.left + window.pageXOffset));
            const sy = (y - (cr.top + window.pageYOffset));
            return [sx / this.scaleX, sy / this.scaleY];
        }
        pagePointContainScreen(x, y) {
            const pos = this.pagePointToScreenPoint(x, y);
            return 0 <= pos[0] && pos[0] < this.offscreenWidth && 0 <= pos[1] && pos[1] < this.offscreenHeight;
        }
    }
    Game.Video = Video;
})(Game || (Game = {}));
var Game;
(function (Game) {
    let Sound;
    (function (Sound) {
        class ManagedSoundChannel {
            constructor() {
                this.audioBufferNode = null;
                this.playRequest = false;
                this.stopRequest = false;
                this.loopPlay = false;
            }
            reset() {
                this.audioBufferNode = null;
                this.playRequest = false;
                this.stopRequest = false;
                this.loopPlay = false;
            }
        }
        class UnmanagedSoundChannel {
            constructor(sound, buffer) {
                this.isEnded = true;
                this.bufferSource = null;
                this.buffer = null;
                this.sound = null;
                this.buffer = buffer;
                this.sound = sound;
                this.reset();
            }
            reset() {
                this.stop();
                this.bufferSource = this.sound.createBufferSource(this.buffer);
                this.bufferSource.onended = () => this.isEnded = true;
            }
            loopplay() {
                if (this.isEnded) {
                    this.bufferSource.loop = true;
                    this.bufferSource.start(0);
                    this.isEnded = false;
                }
            }
            play() {
                if (this.isEnded) {
                    this.bufferSource.loop = false;
                    this.bufferSource.start(0);
                    this.isEnded = false;
                }
            }
            stop() {
                if (!this.isEnded) {
                    this.bufferSource.stop(0);
                    this.bufferSource.disconnect();
                    this.isEnded = true;
                }
            }
        }
        class SoundManager {
            constructor() {
                this.bufferSourceIdCount = 0;
                if (window.AudioContext) {
                    console.log("Use AudioContext.");
                    this.audioContext = new window.AudioContext();
                }
                else if (window.webkitAudioContext) {
                    console.log("Use webkitAudioContext.");
                    this.audioContext = new window.webkitAudioContext();
                }
                else {
                    console.error("Neither AudioContext nor webkitAudioContext is supported by your browser.");
                    throw new Error("Neither AudioContext nor webkitAudioContext is supported by your browser.");
                }
                this.channels = new Map();
                this.bufferSourceIdCount = 0;
                this.playingBufferSources = new Map();
                this.reset();
                const touchEventHooker = () => {
                    const buffer = this.audioContext.createBuffer(1, (this.audioContext.sampleRate / 100), this.audioContext.sampleRate);
                    const channel = buffer.getChannelData(0);
                    channel.fill(0);
                    const src = this.audioContext.createBufferSource();
                    src.buffer = buffer;
                    src.connect(this.audioContext.destination);
                    src.start(this.audioContext.currentTime);
                    document.body.removeEventListener('touchstart', touchEventHooker);
                };
                document.body.addEventListener('touchstart', touchEventHooker);
            }
            createBufferSource(buffer) {
                const bufferSource = this.audioContext.createBufferSource();
                bufferSource.buffer = buffer;
                bufferSource.connect(this.audioContext.destination);
                return bufferSource;
            }
            loadSound(file) {
                return ajax(file, "arraybuffer").then(xhr => {
                    return new Promise((resolve, reject) => {
                        this.audioContext.decodeAudioData(xhr.response, (audioBufferNode) => {
                            resolve(audioBufferNode);
                        }, (ev) => {
                            reject(ev);
                        });
                    });
                });
            }
            loadSoundToChannel(file, channelId) {
                return __awaiter(this, void 0, void 0, function* () {
                    const audioBufferNode = yield this.loadSound(file);
                    const channel = new ManagedSoundChannel();
                    channel.audioBufferNode = audioBufferNode;
                    this.channels.set(channelId, channel);
                    return;
                });
            }
            loadSoundsToChannel(config, startCallback = () => { }, endCallback = () => { }) {
                return Promise.all(Object.keys(config).map((channelId) => {
                    startCallback(channelId);
                    const ret = this.loadSoundToChannel(config[channelId], channelId).then(() => endCallback(channelId));
                    return ret;
                })).then(() => { });
            }
            createUnmanagedSoundChannel(file) {
                return this.loadSound(file)
                    .then((audioBufferNode) => new UnmanagedSoundChannel(this, audioBufferNode));
            }
            reqPlayChannel(channelId, loop = false) {
                const channel = this.channels.get(channelId);
                if (channel) {
                    channel.playRequest = true;
                    channel.loopPlay = loop;
                }
            }
            reqStopChannel(channelId) {
                const channel = this.channels.get(channelId);
                if (channel) {
                    channel.stopRequest = true;
                }
            }
            playChannel() {
                this.channels.forEach((c, i) => {
                    if (c.stopRequest) {
                        c.stopRequest = false;
                        if (c.audioBufferNode == null) {
                            return;
                        }
                        this.playingBufferSources.forEach((value, key) => {
                            if (value.id === i) {
                                const srcNode = value.buffer;
                                srcNode.stop();
                                srcNode.disconnect();
                                this.playingBufferSources.set(key, null);
                                this.playingBufferSources.delete(key);
                            }
                        });
                    }
                    if (c.playRequest) {
                        c.playRequest = false;
                        if (c.audioBufferNode == null) {
                            return;
                        }
                        const src = this.audioContext.createBufferSource();
                        if (src == null) {
                            throw new Error("createBufferSourceに失敗。");
                        }
                        const bufferid = this.bufferSourceIdCount++;
                        this.playingBufferSources.set(bufferid, { id: i, buffer: src });
                        src.buffer = c.audioBufferNode;
                        src.loop = c.loopPlay;
                        src.connect(this.audioContext.destination);
                        src.onended = (() => {
                            const srcNode = src;
                            srcNode.stop(0);
                            srcNode.disconnect();
                            this.playingBufferSources.set(bufferid, null);
                            this.playingBufferSources.delete(bufferid);
                        }).bind(null, bufferid);
                        src.start(0);
                    }
                });
            }
            stop() {
                const oldPlayingBufferSources = this.playingBufferSources;
                this.playingBufferSources = new Map();
                oldPlayingBufferSources.forEach((value, key) => {
                    const s = value.buffer;
                    if (s != null) {
                        s.stop(0);
                        s.disconnect();
                        oldPlayingBufferSources.set(key, null);
                        oldPlayingBufferSources.delete(key);
                    }
                });
            }
            reset() {
                this.channels.clear();
                this.playingBufferSources.clear();
            }
        }
        Sound.SoundManager = SoundManager;
    })(Sound = Game.Sound || (Game.Sound = {}));
})(Game || (Game = {}));
var Game;
(function (Game) {
    let Input;
    (function (Input) {
        class CustomPointerEvent extends CustomEvent {
        }
        let PointerChangeStatus;
        (function (PointerChangeStatus) {
            PointerChangeStatus[PointerChangeStatus["Down"] = 0] = "Down";
            PointerChangeStatus[PointerChangeStatus["Up"] = 1] = "Up";
            PointerChangeStatus[PointerChangeStatus["Leave"] = 2] = "Leave";
        })(PointerChangeStatus || (PointerChangeStatus = {}));
        class InputManager extends Dispatcher.EventDispatcher {
            constructor() {
                super();
                if (!window.TouchEvent) {
                    console.log("TouchEvent is not supported by your browser.");
                    window.TouchEvent = function () { };
                }
                if (!window.PointerEvent) {
                    console.log("PointerEvent is not supported by your browser.");
                    window.PointerEvent = function () { };
                }
                this.isScrolling = false;
                this.timeout = 0;
                this.sDistX = 0;
                this.sDistY = 0;
                this.maybeClick = false;
                this.maybeClickX = 0;
                this.maybeClickY = 0;
                this.prevTimeStamp = 0;
                this.prevInputType = "none";
                window.addEventListener("scroll", () => {
                    if (!this.isScrolling) {
                        this.sDistX = window.pageXOffset;
                        this.sDistY = window.pageYOffset;
                    }
                    this.isScrolling = true;
                    clearTimeout(this.timeout);
                    this.timeout = setTimeout(() => {
                        this.isScrolling = false;
                        this.sDistX = 0;
                        this.sDistY = 0;
                    }, 100);
                });
                document.onselectstart = () => false;
                document.oncontextmenu = () => false;
                if (document.body["pointermove"] !== undefined) {
                    document.body.addEventListener('touchmove', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('touchdown', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('touchup', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('mousemove', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('mousedown', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('mouseup', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('pointerdown', (ev) => this.fire('pointerdown', ev));
                    document.body.addEventListener('pointermove', (ev) => this.fire('pointermove', ev));
                    document.body.addEventListener('pointerup', (ev) => this.fire('pointerup', ev));
                    document.body.addEventListener('pointerleave', (ev) => this.fire('pointerleave', ev));
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
                this.capture = false;
                this.lastPageX = 0;
                this.lastPageY = 0;
                this.downup = 0;
                this.status = PointerChangeStatus.Leave;
                this.clicked = false;
                this.lastDownPageX = 0;
                this.lastDownPageY = 0;
                this.draglen = 0;
                this.captureHandler = this.captureHandler.bind(this);
                this.on('pointerdown', this.captureHandler);
                this.on('pointermove', this.captureHandler);
                this.on('pointerup', this.captureHandler);
                this.on('pointerleave', this.captureHandler);
            }
            get pageX() {
                return this.lastPageX;
            }
            get pageY() {
                return this.lastPageY;
            }
            isDown() {
                return this.downup === 1;
            }
            isPush() {
                return this.downup > 1;
            }
            isUp() {
                return this.downup === -1;
            }
            isMove() {
                return (~~this.startPageX !== ~~this.lastPageX) || (~~this.startPageY !== ~~this.lastPageY);
            }
            isClick() {
                return this.clicked;
            }
            isRelease() {
                return this.downup < -1;
            }
            startCapture() {
                this.capture = true;
                this.startPageX = ~~this.lastPageX;
                this.startPageY = ~~this.lastPageY;
            }
            endCapture() {
                this.capture = false;
                if (this.status === PointerChangeStatus.Down) {
                    if (this.downup < 1) {
                        this.downup = 1;
                    }
                    else {
                        this.downup += 1;
                    }
                }
                else if (this.status === PointerChangeStatus.Up) {
                    if (this.downup > -1) {
                        this.downup = -1;
                    }
                    else {
                        this.downup -= 1;
                    }
                }
                else {
                    this.downup = 0;
                }
                this.clicked = false;
                if (this.downup === -1) {
                    if (this.draglen < 5) {
                        this.clicked = true;
                    }
                }
                else if (this.downup === 1) {
                    this.lastDownPageX = this.lastPageX;
                    this.lastDownPageY = this.lastPageY;
                    this.draglen = 0;
                }
                else if (this.downup > 1) {
                    this.draglen = Math.max(this.draglen, Math.sqrt((this.lastDownPageX - this.lastPageX) * (this.lastDownPageX - this.lastPageX) + (this.lastDownPageY - this.lastPageY) * (this.lastDownPageY - this.lastPageY)));
                }
            }
            captureHandler(e) {
                if (this.capture === false) {
                    return;
                }
                switch (e.type) {
                    case "pointerdown":
                        this.status = PointerChangeStatus.Down;
                        break;
                    case "pointerup":
                        this.status = PointerChangeStatus.Up;
                        break;
                    case "pointerleave":
                        this.status = PointerChangeStatus.Leave;
                        break;
                    case "pointermove":
                        break;
                }
                this.lastPageX = e.pageX;
                this.lastPageY = e.pageY;
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
                evt.initCustomEvent(eventType, true, true, {});
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
                this.fire(eventType, evt);
                return evt;
            }
        }
        Input.InputManager = InputManager;
        class VirtualStick {
            constructor(x = 120, y = 120, radius = 40) {
                this.isTouching = false;
                this.x = x;
                this.y = y;
                this.cx = 0;
                this.cy = 0;
                this.radius = radius;
                this.distance = 0;
                this.angle = 0;
                this.id = -1;
            }
            get dir4() {
                switch (~~((this.angle + 360 + 45) / 90) % 4) {
                    case 0: return 6;
                    case 1: return 2;
                    case 2: return 4;
                    case 3: return 8;
                }
                return 5;
            }
            get dir8() {
                const d = ~~((this.angle + 360 + 22.5) / 45) % 8;
                switch (d) {
                    case 0: return 6;
                    case 1: return 3;
                    case 2: return 2;
                    case 3: return 1;
                    case 4: return 4;
                    case 5: return 7;
                    case 6: return 8;
                    case 7: return 9;
                }
                return 5;
            }
            isHit(x, y) {
                const dx = x - this.x;
                const dy = y - this.y;
                return ((dx * dx) + (dy * dy)) <= this.radius * this.radius;
            }
            onpointingstart(id) {
                if (this.id !== -1) {
                    return false;
                }
                this.isTouching = true;
                this.cx = 0;
                this.cy = 0;
                this.angle = 0;
                this.distance = 0;
                this.id = id;
                return true;
            }
            onpointingend(id) {
                if (this.id !== id) {
                    return false;
                }
                this.isTouching = false;
                this.cx = 0;
                this.cy = 0;
                this.angle = 0;
                this.distance = 0;
                this.id = -1;
                return true;
            }
            onpointingmove(id, x, y) {
                if (this.isTouching === false) {
                    return false;
                }
                if (id !== this.id) {
                    return false;
                }
                let dx = x - this.x;
                let dy = y - this.y;
                let len = Math.sqrt((dx * dx) + (dy * dy));
                if (len > 0) {
                    dx /= len;
                    dy /= len;
                    if (len > this.radius) {
                        len = this.radius;
                    }
                    this.angle = Math.atan2(dy, dx) * 180 / Math.PI;
                    this.distance = len * 1.0 / this.radius;
                    this.cx = dx * len;
                    this.cy = dy * len;
                }
                else {
                    this.cx = 0;
                    this.cy = 0;
                    this.angle = 0;
                    this.distance = 0;
                }
                return true;
            }
        }
        Input.VirtualStick = VirtualStick;
    })(Input = Game.Input || (Game.Input = {}));
})(Game || (Game = {}));
var Game;
(function (Game) {
    let Timer;
    (function (Timer) {
        class AnimationTimer extends Dispatcher.SingleDispatcher {
            constructor() {
                super();
                this.animationFrameId = NaN;
                this.prevTime = NaN;
            }
            start() {
                if (!isNaN(this.animationFrameId)) {
                    this.stop();
                }
                this.animationFrameId = requestAnimationFrame(this.tick.bind(this));
                return !isNaN(this.animationFrameId);
            }
            stop() {
                if (!isNaN(this.animationFrameId)) {
                    cancelAnimationFrame(this.animationFrameId);
                    this.animationFrameId = NaN;
                }
            }
            tick(ts) {
                requestAnimationFrame(this.tick.bind(this));
                if (!isNaN(this.prevTime)) {
                    const delta = ts - this.prevTime;
                    this.fire(delta, ts);
                }
                this.prevTime = ts;
            }
        }
        Timer.AnimationTimer = AnimationTimer;
    })(Timer = Game.Timer || (Game.Timer = {}));
})(Game || (Game = {}));
var Game;
(function (Game) {
    let Scene;
    (function (Scene_1) {
        class Scene {
            constructor(manager, init) {
                this.manager = manager;
                this.state = null;
                this.init = init;
                this.update = () => { };
                this.draw = () => { };
                this.leave = () => { };
                this.suspend = () => { };
                this.resume = () => { };
            }
            next(...args) {
                this.update = this.state.next.apply(this.state, args).value;
            }
            enter(...data) {
                this.state = this.init.apply(this, data);
                this.next();
            }
        }
        class SceneManager {
            constructor() {
                this.sceneStack = [];
            }
            push(sceneDef, arg) {
                if (this.peek() != null && this.peek().suspend != null) {
                    this.peek().suspend();
                }
                this.sceneStack.push(new Scene(this, sceneDef));
                if (this.peek() != null && this.peek().enter != null) {
                    this.peek().enter.call(this.peek(), arg);
                }
            }
            pop() {
                if (this.sceneStack.length === 0) {
                    throw new Error("there is no scene.");
                }
                if (this.peek() != null) {
                    const p = this.sceneStack.pop();
                    if (p.leave != null) {
                        p.leave();
                    }
                }
                if (this.peek() != null && this.peek().resume != null) {
                    this.peek().resume();
                }
            }
            peek() {
                if (this.sceneStack.length > 0) {
                    return this.sceneStack[this.sceneStack.length - 1];
                }
                else {
                    return null;
                }
            }
            update(...args) {
                if (this.peek() != null && this.peek().update != null) {
                    this.peek().update.apply(this.peek(), args);
                }
                return this;
            }
            draw() {
                if (this.peek() != null && this.peek().draw != null) {
                    this.peek().draw.apply(this.peek());
                }
                return this;
            }
        }
        Scene_1.SceneManager = SceneManager;
    })(Scene = Game.Scene || (Game.Scene = {}));
})(Game || (Game = {}));
var Game;
(function (Game) {
    class ConsoleView {
        constructor() {
            const log = console.log.bind(console);
            const error = console.error.bind(console);
            const warn = console.warn.bind(console);
            const table = console.table ? console.table.bind(console) : null;
            const toString = (x) => (x instanceof Error) ? x.message : (typeof x === 'string' ? x : JSON.stringify(x));
            const outer = document.createElement('div');
            outer.id = 'console';
            const div = document.createElement('div');
            outer.appendChild(div);
            const printToDiv = (stackTraceObject, ...args) => {
                const msg = Array.prototype.slice.call(args, 0)
                    .map(toString)
                    .join(' ');
                const text = div.textContent;
                const trace = stackTraceObject ? stackTraceObject.stack.split(/\n/)[1] : "";
                div.textContent = text + trace + ": " + msg + '\n';
                while (div.clientHeight > document.body.clientHeight) {
                    const lines = div.textContent.split(/\n/);
                    lines.shift();
                    div.textContent = lines.join('\n');
                }
            };
            console.log = (...args) => {
                log.apply(null, args);
                const dupargs = Array.prototype.slice.call(args, 0);
                dupargs.unshift(new Error());
                printToDiv.apply(null, dupargs);
            };
            console.error = (...args) => {
                error.apply(null, args);
                const dupargs = Array.prototype.slice.call(args, 0);
                dupargs.unshift('ERROR:');
                dupargs.unshift(new Error());
                printToDiv.apply(null, dupargs);
            };
            console.warn = (...args) => {
                warn.apply(null, args);
                const dupargs = Array.prototype.slice.call(args, 0);
                dupargs.unshift('WARNING:');
                dupargs.unshift(new Error());
                printToDiv.apply(null, dupargs);
            };
            console.table = (...args) => {
                if (typeof table === 'function') {
                    table.apply(null, args);
                }
                const objArr = args[0];
                const keys = (typeof objArr[0] !== 'undefined') ? Object.keys(objArr[0]) : [];
                const numCols = keys.length;
                const len = objArr.length;
                const $table = document.createElement('table');
                const $head = document.createElement('thead');
                let $tdata = document.createElement('td');
                $tdata.innerHTML = 'Index';
                $head.appendChild($tdata);
                for (let k = 0; k < numCols; k++) {
                    $tdata = document.createElement('td');
                    $tdata.innerHTML = keys[k];
                    $head.appendChild($tdata);
                }
                $table.appendChild($head);
                for (let i = 0; i < len; i++) {
                    const $line = document.createElement('tr');
                    $tdata = document.createElement('td');
                    $tdata.innerHTML = "" + i;
                    $line.appendChild($tdata);
                    for (let j = 0; j < numCols; j++) {
                        $tdata = document.createElement('td');
                        $tdata.innerHTML = objArr[i][keys[j]];
                        $line.appendChild($tdata);
                    }
                    $table.appendChild($line);
                }
                div.appendChild($table);
            };
            window.addEventListener('error', (err) => {
                printToDiv(null, 'EXCEPTION:', err.message + '\n  ' + err.filename, err.lineno + ':' + err.colno);
            });
            document.body.appendChild(outer);
        }
        static install() {
            if (!this.instance) {
                this.instance = new ConsoleView();
                return true;
            }
            else {
                return false;
            }
        }
    }
    Game.ConsoleView = ConsoleView;
})(Game || (Game = {}));
var Game;
(function (Game) {
    Game.pmode = false;
    let video = null;
    let sceneManager = null;
    let inputDispacher = null;
    let timer = null;
    let soundManager = null;
    function create(config) {
        return new Promise((resolve, reject) => {
            try {
                Game.ConsoleView.install();
                document.title = config.title;
                video = new Game.Video(config.video);
                video.imageSmoothingEnabled = false;
                sceneManager = new Game.Scene.SceneManager();
                timer = new Game.Timer.AnimationTimer();
                inputDispacher = new Game.Input.InputManager();
                soundManager = new Game.Sound.SoundManager();
                resolve();
            }
            catch (e) {
                reject(e);
            }
        });
    }
    Game.create = create;
    function getScreen() {
        return video;
    }
    Game.getScreen = getScreen;
    function getTimer() {
        return timer;
    }
    Game.getTimer = getTimer;
    function getSceneManager() {
        return sceneManager;
    }
    Game.getSceneManager = getSceneManager;
    function getInput() {
        return inputDispacher;
    }
    Game.getInput = getInput;
    function getSound() {
        return soundManager;
    }
    Game.getSound = getSound;
})(Game || (Game = {}));
class Array2D {
    constructor(width, height, fill) {
        this.arrayWidth = width;
        this.arrayHeight = height;
        if (fill === undefined) {
            this.matrixBuffer = new Array(width * height);
        }
        else {
            this.matrixBuffer = new Array(width * height).fill(fill);
        }
    }
    get width() {
        return this.arrayWidth;
    }
    get height() {
        return this.arrayHeight;
    }
    value(x, y, value) {
        if (0 > x || x >= this.arrayWidth || 0 > y || y >= this.arrayHeight) {
            return 0;
        }
        if (value !== undefined) {
            this.matrixBuffer[y * this.arrayWidth + x] = value;
        }
        return this.matrixBuffer[y * this.arrayWidth + x];
    }
    fill(value) {
        this.matrixBuffer.fill(value);
        return this;
    }
    dup() {
        const m = new Array2D(this.width, this.height);
        m.matrixBuffer = this.matrixBuffer.slice();
        return m;
    }
    static createFromArray(array, fill) {
        const h = array.length;
        const w = Math.max.apply(Math, array.map(x => x.length));
        const matrix = new Array2D(w, h, fill);
        array.forEach((vy, y) => vy.forEach((vx, x) => matrix.value(x, y, vx)));
        return matrix;
    }
    toString() {
        const lines = [];
        for (let y = 0; y < this.height; y++) {
            lines[y] = `|${this.matrixBuffer.slice((y + 0) * this.arrayWidth, (y + 1) * this.arrayWidth).join(", ")}|`;
        }
        return lines.join("\r\n");
    }
}
Array2D.DIR8 = [
    { x: +Number.MAX_SAFE_INTEGER, y: +Number.MAX_SAFE_INTEGER },
    { x: -1, y: +1 },
    { x: +0, y: +1 },
    { x: +1, y: +1 },
    { x: -1, y: +0 },
    { x: +0, y: +0 },
    { x: +1, y: +0 },
    { x: -1, y: -1 },
    { x: +0, y: -1 },
    { x: +1, y: -1 }
];
var PathFinder;
(function (PathFinder) {
    const dir4 = [
        { x: 0, y: -1 },
        { x: 1, y: 0 },
        { x: 0, y: 1 },
        { x: -1, y: 0 }
    ];
    const dir8 = [
        { x: 0, y: -1 },
        { x: 1, y: 0 },
        { x: 0, y: 1 },
        { x: -1, y: 0 },
        { x: 1, y: -1 },
        { x: 1, y: 1 },
        { x: -1, y: 1 },
        { x: -1, y: -1 }
    ];
    function calcDistanceByDijkstra({ array2D = null, sx = null, sy = null, value = null, costs = null, left = 0, top = 0, right = undefined, bottom = undefined, timeout = 1000, topology = 8, output = undefined }) {
        if (left === undefined || left < 0) {
            right = 0;
        }
        if (top === undefined || top < 0) {
            bottom = 0;
        }
        if (right === undefined || right > array2D.width) {
            right = array2D.width;
        }
        if (bottom === undefined || bottom > array2D.height) {
            bottom = array2D.height;
        }
        if (output === undefined) {
            output = () => { };
        }
        const dirs = (topology === 8) ? dir8 : dir4;
        const work = new Array2D(array2D.width, array2D.height);
        work.value(sx, sy, value);
        output(sx, sy, value);
        const request = dirs.map(({ x, y }) => [sx + x, sy + y, value]);
        const start = Date.now();
        while (request.length !== 0 && (Date.now() - start) < timeout) {
            const [px, py, currentValue] = request.shift();
            if (top > py || py >= bottom || left > px || px >= right) {
                continue;
            }
            const cost = costs(array2D.value(px, py));
            if (cost < 0 || currentValue < cost) {
                continue;
            }
            const nextValue = currentValue - cost;
            const targetPower = work.value(px, py);
            if (nextValue <= targetPower) {
                continue;
            }
            work.value(px, py, nextValue);
            output(px, py, nextValue);
            Array.prototype.push.apply(request, dirs.map(({ x, y }) => [px + x, py + y, nextValue]));
        }
    }
    PathFinder.calcDistanceByDijkstra = calcDistanceByDijkstra;
    function pathfind(array2D, fromX, fromY, toX, toY, costs, opts) {
        opts = Object.assign({ topology: 8 }, opts);
        const topology = opts.topology;
        let dirs;
        if (topology === 4) {
            dirs = dir4;
        }
        else if (topology === 8) {
            dirs = dir8;
        }
        else {
            throw new Error("Illegal topology");
        }
        const todo = [];
        const add = ((x, y, prev) => {
            let distance;
            switch (topology) {
                case 4:
                    distance = (Math.abs(x - fromX) + Math.abs(y - fromY));
                    break;
                case 8:
                    distance = Math.min(Math.abs(x - fromX), Math.abs(y - fromY));
                    break;
                default:
                    throw new Error("Illegal topology");
            }
            const obj = {
                x: x,
                y: y,
                prev: prev,
                g: (prev ? prev.g + 1 : 0),
                distance: distance
            };
            const f = obj.g + obj.distance;
            for (let i = 0; i < todo.length; i++) {
                const item = todo[i];
                const itemF = item.g + item.distance;
                if (f < itemF || (f === itemF && distance < item.distance)) {
                    todo.splice(i, 0, obj);
                    return;
                }
            }
            todo.push(obj);
        });
        add(toX, toY, null);
        const done = new Map();
        while (todo.length) {
            let item = todo.shift();
            {
                const id = item.x + "," + item.y;
                if (done.has(id)) {
                    continue;
                }
                done.set(id, item);
            }
            if (item.x === fromX && item.y === fromY) {
                const result = [];
                while (item) {
                    result.push(item);
                    item = item.prev;
                }
                return result;
            }
            else {
                for (let i = 0; i < dirs.length; i++) {
                    const dir = dirs[i];
                    const x = item.x + dir.x;
                    const y = item.y + dir.y;
                    const cost = costs[this.value(x, y)];
                    if (cost < 0) {
                        continue;
                    }
                    else {
                        const id = x + "," + y;
                        if (done.has(id)) {
                            continue;
                        }
                        add(x, y, item);
                    }
                }
            }
        }
        return [];
    }
    PathFinder.pathfind = pathfind;
    function pathfindByPropergation(array2D, fromX, fromY, toX, toY, propagation, { topology = 8 }) {
        let dirs;
        if (topology === 4) {
            dirs = dir4;
        }
        else if (topology === 8) {
            dirs = dir8;
        }
        else {
            throw new Error("Illegal topology");
        }
        const todo = [];
        const add = ((x, y, prev) => {
            const distance = Math.abs(propagation.value(x, y) - propagation.value(fromX, fromY));
            const obj = {
                x: x,
                y: y,
                prev: prev,
                g: (prev ? prev.g + 1 : 0),
                distance: distance
            };
            const f = obj.g + obj.distance;
            for (let i = 0; i < todo.length; i++) {
                const item = todo[i];
                const itemF = item.g + item.distance;
                if (f < itemF || (f === itemF && distance < item.distance)) {
                    todo.splice(i, 0, obj);
                    return;
                }
            }
            todo.push(obj);
        });
        add(toX, toY, null);
        const done = new Map();
        while (todo.length) {
            let item = todo.shift();
            {
                const id = item.x + "," + item.y;
                if (done.has(id)) {
                    continue;
                }
                done.set(id, item);
            }
            if (item.x === fromX && item.y === fromY) {
                const result = [];
                while (item) {
                    result.push(item);
                    item = item.prev;
                }
                return result;
            }
            else {
                dirs.forEach((dir) => {
                    const x = item.x + dir.x;
                    const y = item.y + dir.y;
                    const pow = propagation.value(x, y);
                    if (pow === 0) {
                        return;
                    }
                    else {
                        const id = x + "," + y;
                        if (done.has(id)) {
                            return;
                        }
                        else {
                            add(x, y, item);
                        }
                    }
                });
            }
        }
        return [];
    }
    PathFinder.pathfindByPropergation = pathfindByPropergation;
})(PathFinder || (PathFinder = {}));
var Dungeon;
(function (Dungeon) {
    const rand = XorShift.default();
    class Camera {
    }
    Dungeon.Camera = Camera;
    class DungeonData {
        constructor(config) {
            this.width = config.width;
            this.height = config.height;
            this.gridsize = config.gridsize;
            this.layer = config.layer;
            this.camera = new Camera();
            this.lighting = new Array2D(this.width, this.height, 0);
            this.visibled = new Array2D(this.width, this.height, 0);
        }
        clearLighting() {
            this.lighting.fill(0);
            return this;
        }
        update(param) {
            const mapWidth = this.width * this.gridsize.width;
            const mapHeight = this.height * this.gridsize.height;
            const mapPx = param.viewpoint.x;
            const mapPy = param.viewpoint.y;
            this.camera.width = param.viewwidth;
            this.camera.height = param.viewheight;
            this.camera.left = ~~(mapPx - this.camera.width / 2);
            this.camera.top = ~~(mapPy - this.camera.height / 2);
            this.camera.right = this.camera.left + this.camera.width;
            this.camera.bottom = this.camera.top + this.camera.height;
            if ((this.camera.left < 0) && (this.camera.right - this.camera.left < mapWidth)) {
                this.camera.right -= this.camera.left;
                this.camera.left = 0;
            }
            else if ((this.camera.right >= mapWidth) && (this.camera.left - (this.camera.right - mapWidth) >= 0)) {
                this.camera.left -= (this.camera.right - mapWidth);
                this.camera.right = mapWidth - 1;
            }
            if ((this.camera.top < 0) && (this.camera.bottom - this.camera.top < mapHeight)) {
                this.camera.bottom -= this.camera.top;
                this.camera.top = 0;
            }
            else if ((this.camera.bottom >= mapHeight) && (this.camera.top - (this.camera.bottom - mapHeight) >= 0)) {
                this.camera.top -= (this.camera.bottom - mapHeight);
                this.camera.bottom = mapHeight - 1;
            }
            this.camera.localPx = mapPx - this.camera.left;
            this.camera.localPy = mapPy - this.camera.top;
            this.camera.chipLeft = ~~(this.camera.left / this.gridsize.width);
            this.camera.chipTop = ~~(this.camera.top / this.gridsize.height);
            this.camera.chipRight = ~~((this.camera.right + (this.gridsize.width - 1)) / this.gridsize.width);
            this.camera.chipBottom = ~~((this.camera.bottom + (this.gridsize.height - 1)) / this.gridsize.height);
            this.camera.chipOffX = -(this.camera.left % this.gridsize.width);
            this.camera.chipOffY = -(this.camera.top % this.gridsize.height);
        }
        draw(layerDrawHook) {
            const gridw = this.gridsize.width;
            const gridh = this.gridsize.height;
            Object.keys(this.layer).forEach((key) => {
                const l = ~~key;
                for (let y = this.camera.chipTop; y <= this.camera.chipBottom; y++) {
                    for (let x = this.camera.chipLeft; x <= this.camera.chipRight; x++) {
                        const chipid = this.layer[l].chips.value(x, y) || 0;
                        if (this.layer[l].chip[chipid]) {
                            const xx = (x - this.camera.chipLeft) * gridw;
                            const yy = (y - this.camera.chipTop) * gridh;
                            if (!Game.pmode) {
                                Game.getScreen().drawImage(Game.getScreen().texture(this.layer[l].texture), this.layer[l].chip[chipid].x, this.layer[l].chip[chipid].y, gridw, gridh, 0 + xx + this.camera.chipOffX, 0 + yy + this.camera.chipOffY, gridw, gridh);
                            }
                            else {
                                Game.getScreen().fillStyle = `rgba(185,122,87,1)`;
                                Game.getScreen().strokeStyle = `rgba(0,0,0,1)`;
                                Game.getScreen().fillRect(0 + xx + this.camera.chipOffX, 0 + yy + this.camera.chipOffY, gridw, gridh);
                                Game.getScreen().strokeRect(0 + xx + this.camera.chipOffX, 0 + yy + this.camera.chipOffY, gridw, gridh);
                            }
                        }
                    }
                }
                layerDrawHook(l, this.camera.localPx, this.camera.localPy);
            });
            for (let y = this.camera.chipTop; y <= this.camera.chipBottom; y++) {
                for (let x = this.camera.chipLeft; x <= this.camera.chipRight; x++) {
                    let light = this.lighting.value(x, y) / 100;
                    if (light > 1) {
                        light = 1;
                    }
                    else if (light < 0) {
                        light = 0;
                    }
                    const xx = (x - this.camera.chipLeft) * gridw;
                    const yy = (y - this.camera.chipTop) * gridh;
                    Game.getScreen().fillStyle = `rgba(0,0,0,${1 - light})`;
                    Game.getScreen().fillRect(0 + xx + this.camera.chipOffX, 0 + yy + this.camera.chipOffY, gridw, gridh);
                }
            }
        }
    }
    Dungeon.DungeonData = DungeonData;
    class Feature {
    }
    class Room extends Feature {
        constructor(left, top, right, bottom, door) {
            super();
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.doors = new Map();
            if (door !== undefined) {
                this.addDoor(door.x, door.y);
            }
        }
        static createRandomAt(x, y, dx, dy, options) {
            const minw = options.roomWidth.min;
            const maxw = options.roomWidth.max;
            const width = options.random.randInt(minw, maxw);
            const minh = options.roomHeight.min;
            const maxh = options.roomHeight.max;
            const height = options.random.randInt(minh, maxh);
            if (dx === 1) {
                const y2 = y - options.random.randInt(0, height - 1);
                return new Room(x + 1, y2, x + width, y2 + height - 1, { x: x, y: y });
            }
            if (dx === -1) {
                const y2 = y - options.random.randInt(0, height - 1);
                return new Room(x - width, y2, x - 1, y2 + height - 1, { x: x, y: y });
            }
            if (dy === 1) {
                const x2 = x - options.random.randInt(0, width - 1);
                return new Room(x2, y + 1, x2 + width - 1, y + height, { x: x, y: y });
            }
            if (dy === -1) {
                const x2 = x - options.random.randInt(0, width - 1);
                return new Room(x2, y - height, x2 + width - 1, y - 1, { x: x, y: y });
            }
            throw new Error("dx or dy must be 1 or -1");
        }
        static createRandomCenter(cx, cy, options) {
            const minw = options.roomWidth.min;
            const maxw = options.roomWidth.max;
            const width = options.random.randInt(minw, maxw);
            const minh = options.roomHeight.min;
            const maxh = options.roomHeight.max;
            const height = options.random.randInt(minh, maxh);
            const x1 = cx - options.random.randInt(0, width - 1);
            const y1 = cy - options.random.randInt(0, height - 1);
            const x2 = x1 + width - 1;
            const y2 = y1 + height - 1;
            return new Room(x1, y1, x2, y2);
        }
        static createRandom(availWidth, availHeight, options) {
            const minw = options.roomWidth.min;
            const maxw = options.roomWidth.max;
            const width = options.random.randInt(minw, maxw);
            const minh = options.roomHeight.min;
            const maxh = options.roomHeight.max;
            const height = options.random.randInt(minh, maxh);
            const left = availWidth - width - 1;
            const top = availHeight - height - 1;
            const x1 = 1 + options.random.randInt(0, left - 1);
            const y1 = 1 + options.random.randInt(0, top - 1);
            const x2 = x1 + width - 1;
            const y2 = y1 + height - 1;
            return new Room(x1, y1, x2, y2);
        }
        addDoor(x, y) {
            this.doors.set(x + "," + y, 1);
            return this;
        }
        getDoors(callback) {
            for (const key of Object.keys(this.doors)) {
                const parts = key.split(",");
                callback({ x: parseInt(parts[0], 10), y: parseInt(parts[1], 10) });
            }
            return this;
        }
        clearDoors() {
            this.doors.clear();
            return this;
        }
        addDoors(isWallCallback) {
            const left = this.left - 1;
            const right = this.right + 1;
            const top = this.top - 1;
            const bottom = this.bottom + 1;
            for (let x = left; x <= right; x++) {
                for (let y = top; y <= bottom; y++) {
                    if (x !== left && x !== right && y !== top && y !== bottom) {
                        continue;
                    }
                    if (isWallCallback(x, y)) {
                        continue;
                    }
                    this.addDoor(x, y);
                }
            }
            return this;
        }
        debug() {
            console.log("room", this.left, this.top, this.right, this.bottom);
        }
        isValid(isWallCallback, canBeDugCallback) {
            const left = this.left - 1;
            const right = this.right + 1;
            const top = this.top - 1;
            const bottom = this.bottom + 1;
            for (let x = left; x <= right; x++) {
                for (let y = top; y <= bottom; y++) {
                    if (x === left || x === right || y === top || y === bottom) {
                        if (!isWallCallback(x, y)) {
                            return false;
                        }
                    }
                    else {
                        if (!canBeDugCallback(x, y)) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        create(digCallback) {
            const left = this.left - 1;
            const right = this.right + 1;
            const top = this.top - 1;
            const bottom = this.bottom + 1;
            for (let x = left; x <= right; x++) {
                for (let y = top; y <= bottom; y++) {
                    let value;
                    if (this.doors.has(x + "," + y)) {
                        value = 2;
                    }
                    else if (x === left || x === right || y === top || y === bottom) {
                        value = 1;
                    }
                    else {
                        value = 0;
                    }
                    digCallback(x, y, value);
                }
            }
        }
        getCenter() {
            return { x: Math.round((this.left + this.right) / 2), y: Math.round((this.top + this.bottom) / 2) };
        }
        getLeft() {
            return this.left;
        }
        getRight() {
            return this.right;
        }
        getTop() {
            return this.top;
        }
        getBottom() {
            return this.bottom;
        }
    }
    class Corridor extends Feature {
        constructor(startX, startY, endX, endY) {
            super();
            this.startX = startX;
            this.startY = startY;
            this.endX = endX;
            this.endY = endY;
            this.endsWithAWall = true;
        }
        static createRandomAt(x, y, dx, dy, options) {
            const min = options.corridorLength.min;
            const max = options.corridorLength.max;
            const length = options.random.randInt(min, max);
            return new Corridor(x, y, x + dx * length, y + dy * length);
        }
        debug() {
            console.log("corridor", this.startX, this.startY, this.endX, this.endY);
        }
        isValid(isWallCallback, canBeDugCallback) {
            const sx = this.startX;
            const sy = this.startY;
            let dx = this.endX - sx;
            let dy = this.endY - sy;
            let length = 1 + Math.max(Math.abs(dx), Math.abs(dy));
            if (dx) {
                dx = dx / Math.abs(dx);
            }
            if (dy) {
                dy = dy / Math.abs(dy);
            }
            const nx = dy;
            const ny = -dx;
            let ok = true;
            for (let i = 0; i < length; i++) {
                const x = sx + i * dx;
                const y = sy + i * dy;
                if (!canBeDugCallback(x, y)) {
                    ok = false;
                }
                if (!isWallCallback(x + nx, y + ny)) {
                    ok = false;
                }
                if (!isWallCallback(x - nx, y - ny)) {
                    ok = false;
                }
                if (!ok) {
                    length = i;
                    this.endX = x - dx;
                    this.endY = y - dy;
                    break;
                }
            }
            if (length === 0) {
                return false;
            }
            if (length === 1 && isWallCallback(this.endX + dx, this.endY + dy)) {
                return false;
            }
            const firstCornerBad = !isWallCallback(this.endX + dx + nx, this.endY + dy + ny);
            const secondCornerBad = !isWallCallback(this.endX + dx - nx, this.endY + dy - ny);
            this.endsWithAWall = isWallCallback(this.endX + dx, this.endY + dy);
            if ((firstCornerBad || secondCornerBad) && this.endsWithAWall) {
                return false;
            }
            return true;
        }
        create(digCallback) {
            const sx = this.startX;
            const sy = this.startY;
            let dx = this.endX - sx;
            let dy = this.endY - sy;
            const length = 1 + Math.max(Math.abs(dx), Math.abs(dy));
            if (dx) {
                dx = dx / Math.abs(dx);
            }
            if (dy) {
                dy = dy / Math.abs(dy);
            }
            for (let i = 0; i < length; i++) {
                const x = sx + i * dx;
                const y = sy + i * dy;
                digCallback(x, y, 0);
            }
            return true;
        }
        createPriorityWalls(priorityWallCallback) {
            if (!this.endsWithAWall) {
                return;
            }
            const sx = this.startX;
            const sy = this.startY;
            let dx = this.endX - sx;
            let dy = this.endY - sy;
            if (dx) {
                dx = dx / Math.abs(dx);
            }
            if (dy) {
                dy = dy / Math.abs(dy);
            }
            const nx = dy;
            const ny = -dx;
            priorityWallCallback(this.endX + dx, this.endY + dy);
            priorityWallCallback(this.endX + nx, this.endY + ny);
            priorityWallCallback(this.endX - nx, this.endY - ny);
        }
    }
    class Generator {
        constructor(width, height, { random = new XorShift(), roomWidth = { min: 3, max: 9 }, roomHeight = { min: 3, max: 5 }, corridorLength = { min: 3, max: 10 }, dugPercentage = 0.2, loopLimit = 100000, }) {
            this.width = width;
            this.height = height;
            this.rooms = [];
            this.corridors = [];
            this.options = {
                random: random,
                roomWidth: roomWidth,
                roomHeight: roomHeight,
                corridorLength: corridorLength,
                dugPercentage: dugPercentage,
                loopLimit: loopLimit,
            };
            this.features = {
                Room: 4,
                Corridor: 4,
            };
            this.featureAttempts = 20;
            this.walls = new Map();
            this.digCallback = this.digCallback.bind(this);
            this.canBeDugCallback = this.canBeDugCallback.bind(this);
            this.isWallCallback = this.isWallCallback.bind(this);
            this.priorityWallCallback = this.priorityWallCallback.bind(this);
        }
        create(callback) {
            this.rooms = [];
            this.corridors = [];
            this.map = this.fillMap(1);
            this.walls.clear();
            this.dug = 0;
            const area = (this.width - 2) * (this.height - 2);
            this.firstRoom();
            let t1 = 0;
            let priorityWalls = 0;
            do {
                if (t1++ > this.options.loopLimit) {
                    break;
                }
                const wall = this.findWall();
                if (!wall) {
                    break;
                }
                const parts = wall.split(",");
                const x = parseInt(parts[0]);
                const y = parseInt(parts[1]);
                const dir = this.getDiggingDirection(x, y);
                if (!dir) {
                    continue;
                }
                let featureAttempts = 0;
                do {
                    featureAttempts++;
                    if (this.tryFeature(x, y, dir.x, dir.y)) {
                        this.removeSurroundingWalls(x, y);
                        this.removeSurroundingWalls(x - dir.x, y - dir.y);
                        break;
                    }
                } while (featureAttempts < this.featureAttempts);
                priorityWalls = 0;
                for (const [, value] of this.walls) {
                    if (value > 1) {
                        priorityWalls++;
                    }
                }
            } while ((this.dug / area) < this.options.dugPercentage || priorityWalls);
            this.addDoors();
            if (callback) {
                for (let i = 0; i < this.width; i++) {
                    for (let j = 0; j < this.height; j++) {
                        callback(i, j, this.map[i][j]);
                    }
                }
            }
            this.walls.clear();
            this.map = null;
            this.rooms = this.options.random.shuffle(this.rooms);
            return this;
        }
        digCallback(x, y, value) {
            if (value === 0 || value === 2) {
                this.map[x][y] = 0;
                this.dug++;
            }
            else {
                this.walls.set(x + "," + y, 1);
            }
        }
        isWallCallback(x, y) {
            if (x < 0 || y < 0 || x >= this.width || y >= this.height) {
                return false;
            }
            return (this.map[x][y] === 1);
        }
        canBeDugCallback(x, y) {
            if (x < 1 || y < 1 || x + 1 >= this.width || y + 1 >= this.height) {
                return false;
            }
            return (this.map[x][y] === 1);
        }
        priorityWallCallback(x, y) {
            this.walls.set(x + "," + y, 2);
        }
        findWall() {
            const prio1 = [];
            const prio2 = [];
            for (const [id, prio] of this.walls) {
                if (prio === 2) {
                    prio2.push(id);
                }
                else {
                    prio1.push(id);
                }
            }
            const arr = (prio2.length ? prio2 : prio1);
            if (!arr.length) {
                return null;
            }
            const id2 = arr.sort()[this.options.random.randInt(0, arr.length - 1)];
            this.walls.delete(id2);
            return id2;
        }
        firstRoom() {
            const cx = Math.floor(this.width / 2);
            const cy = Math.floor(this.height / 2);
            const room = Room.createRandomCenter(cx, cy, this.options);
            this.rooms.push(room);
            room.create(this.digCallback);
        }
        fillMap(value) {
            const map = [];
            for (let i = 0; i < this.width; i++) {
                map.push([]);
                for (let j = 0; j < this.height; j++) {
                    map[i].push(value);
                }
            }
            return map;
        }
        tryFeature(x, y, dx, dy) {
            const featureType = this.options.random.getWeightedValue(this.features);
            const feature = Generator.featureCreateMethodTable[featureType](x, y, dx, dy, this.options);
            if (!feature.isValid(this.isWallCallback, this.canBeDugCallback)) {
                return false;
            }
            feature.create(this.digCallback);
            if (feature instanceof Room) {
                this.rooms.push(feature);
            }
            if (feature instanceof Corridor) {
                feature.createPriorityWalls(this.priorityWallCallback);
                this.corridors.push(feature);
            }
            return true;
        }
        removeSurroundingWalls(cx, cy) {
            const deltas = Generator.rotdirs4;
            for (const delta of deltas) {
                const x1 = cx + delta.x;
                const y1 = cy + delta.y;
                this.walls.delete(x1 + "," + y1);
                const x2 = cx + 2 * delta.x;
                const y2 = cy + 2 * delta.y;
                this.walls.delete(x2 + "," + y2);
            }
        }
        getDiggingDirection(cx, cy) {
            if (cx <= 0 || cy <= 0 || cx >= this.width - 1 || cy >= this.height - 1) {
                return null;
            }
            let result = null;
            const deltas = Generator.rotdirs4;
            for (const delta of deltas) {
                const x = cx + delta.x;
                const y = cy + delta.y;
                if (!this.map[x][y]) {
                    if (result) {
                        return null;
                    }
                    result = delta;
                }
            }
            if (!result) {
                return null;
            }
            return { x: -result.x, y: -result.y };
        }
        addDoors() {
            const data = this.map;
            const isWallCallback = (x, y) => {
                return (data[x][y] === 1);
            };
            for (const room of this.rooms) {
                room.clearDoors();
                room.addDoors(isWallCallback);
            }
        }
        getRooms() {
            return this.rooms;
        }
        getCorridors() {
            return this.corridors;
        }
    }
    Generator.featureCreateMethodTable = { "Room": Room.createRandomAt, "Corridor": Corridor.createRandomAt };
    Generator.rotdirs4 = [
        { x: 0, y: -1 },
        { x: 1, y: 0 },
        { x: 0, y: 1 },
        { x: -1, y: 0 }
    ];
    function generate(w, h, callback) {
        return new Generator(w, h, { random: rand }).create(callback);
    }
    Dungeon.generate = generate;
})(Dungeon || (Dungeon = {}));
var SpriteAnimation;
(function (SpriteAnimation) {
    class Animator {
        constructor(spriteSheet) {
            this.spriteSheet = spriteSheet;
            this.offx = 0;
            this.offy = 0;
            this.dir = 5;
            this.animDir = 2;
            this.animFrame = 0;
            this.animName = "idle";
        }
        setDir(dir) {
            if (dir === 0) {
                return;
            }
            this.dir = dir;
            switch (dir) {
                case 1: {
                    if (this.animDir === 4) {
                        this.animDir = 4;
                    }
                    else if (this.animDir === 2) {
                        this.animDir = 2;
                    }
                    else if (this.animDir === 8) {
                        this.animDir = 2;
                    }
                    else if (this.animDir === 6) {
                        this.animDir = 4;
                    }
                    break;
                }
                case 3: {
                    if (this.animDir === 4) {
                        this.animDir = 6;
                    }
                    else if (this.animDir === 2) {
                        this.animDir = 2;
                    }
                    else if (this.animDir === 8) {
                        this.animDir = 2;
                    }
                    else if (this.animDir === 6) {
                        this.animDir = 2;
                    }
                    break;
                }
                case 9: {
                    if (this.animDir === 4) {
                        this.animDir = 6;
                    }
                    else if (this.animDir === 2) {
                        this.animDir = 8;
                    }
                    else if (this.animDir === 8) {
                        this.animDir = 8;
                    }
                    else if (this.animDir === 6) {
                        this.animDir = 6;
                    }
                    break;
                }
                case 7: {
                    if (this.animDir === 4) {
                        this.animDir = 4;
                    }
                    else if (this.animDir === 2) {
                        this.animDir = 8;
                    }
                    else if (this.animDir === 8) {
                        this.animDir = 8;
                    }
                    else if (this.animDir === 6) {
                        this.animDir = 4;
                    }
                    break;
                }
                case 5: {
                    break;
                }
                default: {
                    this.animDir = dir;
                    break;
                }
            }
        }
        setAnimation(type, rate) {
            if (rate > 1) {
                rate = 1;
            }
            if (rate < 0) {
                rate = 0;
            }
            if (type === "move" || type === "action") {
                if (type === "move") {
                    this.offx = ~~(Array2D.DIR8[this.dir].x * 24 * rate);
                    this.offy = ~~(Array2D.DIR8[this.dir].y * 24 * rate);
                }
                else if (type === "action") {
                    this.offx = ~~(Array2D.DIR8[this.dir].x * 12 * Math.sin(rate * Math.PI));
                    this.offy = ~~(Array2D.DIR8[this.dir].y * 12 * Math.sin(rate * Math.PI));
                }
                this.animName = Animator.animationName[this.animDir];
            }
            else if (type === "dead") {
                this.animName = "dead";
                this.offx = 0;
                this.offy = 0;
            }
            else {
                return;
            }
            const animDefs = this.spriteSheet.getAnimation(this.animName);
            const totalWeight = animDefs.reduce((s, x) => s + x.time, 0);
            const targetRate = rate * totalWeight;
            let sum = 0;
            for (let i = 0; i < animDefs.length; i++) {
                const next = sum + animDefs[i].time;
                if (sum <= targetRate && targetRate < next) {
                    this.animFrame = i;
                    return;
                }
                sum = next;
            }
            this.animFrame = animDefs.length - 1;
        }
    }
    Animator.animationName = {
        2: "move_down",
        4: "move_left",
        5: "idle",
        6: "move_right",
        8: "move_up",
    };
    SpriteAnimation.Animator = Animator;
    class SpriteSheet {
        constructor({ source = null, sprite = null, animation = null }) {
            this.source = source;
            this.sprite = sprite;
            this.animation = animation;
        }
        getAnimation(animName) {
            return this.animation.get(animName);
        }
        getAnimationFrame(animName, animFrame) {
            return this.animation.get(animName)[animFrame];
        }
        gtetSprite(spriteName) {
            return this.sprite.get(spriteName);
        }
        getSpriteImage(sprite) {
            return this.source.get(sprite.source);
        }
    }
    SpriteAnimation.SpriteSheet = SpriteSheet;
    class Sprite {
        constructor(sprite) {
            this.source = sprite.source;
            this.left = sprite.left;
            this.top = sprite.top;
            this.width = sprite.width;
            this.height = sprite.height;
            this.offsetX = sprite.offsetX;
            this.offsetY = sprite.offsetY;
        }
    }
    class Animation {
        constructor(animation) {
            this.sprite = animation.sprite;
            this.time = animation.time;
            this.offsetX = animation.offsetX;
            this.offsetY = animation.offsetY;
        }
    }
    function loadImage(imageSrc) {
        return __awaiter(this, void 0, void 0, function* () {
            return new Promise((resolve, reject) => {
                const img = new Image();
                img.src = imageSrc;
                img.onload = () => {
                    resolve(img);
                };
                img.onerror = () => { reject(imageSrc + "のロードに失敗しました。"); };
            });
        });
    }
    function loadSpriteSheet(spriteSheetPath, loadStartCallback, loadEndCallback) {
        return __awaiter(this, void 0, void 0, function* () {
            const spriteSheetDir = getDirectory(spriteSheetPath);
            loadStartCallback();
            const spriteSheetJson = yield ajax(spriteSheetPath, "json").then(y => y.response);
            loadEndCallback();
            if (spriteSheetJson == null) {
                throw new Error(spriteSheetPath + " is invalid json.");
            }
            const source = new Map();
            {
                const keys = Object.keys(spriteSheetJson.source);
                for (let i = 0; i < keys.length; i++) {
                    const key = keys[i];
                    const imageSrc = spriteSheetDir + '/' + spriteSheetJson.source[key];
                    loadStartCallback();
                    const image = yield loadImage(imageSrc);
                    loadEndCallback();
                    source.set(key, image);
                }
            }
            const sprite = new Map();
            {
                const keys = Object.keys(spriteSheetJson.sprite);
                for (let i = 0; i < keys.length; i++) {
                    const key = keys[i];
                    sprite.set(key, new Sprite(spriteSheetJson.sprite[key]));
                }
            }
            const animation = new Map();
            {
                const keys = Object.keys(spriteSheetJson.animation);
                for (let i = 0; i < keys.length; i++) {
                    const key = keys[i];
                    const value = spriteSheetJson.animation[key].map(x => new Animation(x));
                    animation.set(key, value);
                }
            }
            const spriteSheet = new SpriteSheet({
                source: source,
                sprite: sprite,
                animation: animation,
            });
            return spriteSheet;
        });
    }
    SpriteAnimation.loadSpriteSheet = loadSpriteSheet;
})(SpriteAnimation || (SpriteAnimation = {}));
var Charactor;
(function (Charactor) {
    function loadCharactorConfigFromFile(path, loadStartCallback, loadEndCallback) {
        return __awaiter(this, void 0, void 0, function* () {
            const configDirectory = path.substring(0, path.lastIndexOf("/"));
            loadStartCallback();
            const charactorConfigJson = yield ajax(path, "json").then(x => x.response);
            loadEndCallback();
            const spriteSheetPath = configDirectory + "/" + charactorConfigJson.sprite;
            const sprite = yield SpriteAnimation.loadSpriteSheet(spriteSheetPath, loadStartCallback, loadEndCallback);
            return new CharactorConfig({
                id: charactorConfigJson.id,
                name: charactorConfigJson.name,
                sprite: sprite,
                configDirectory: configDirectory
            });
        });
    }
    Charactor.loadCharactorConfigFromFile = loadCharactorConfigFromFile;
    class CharactorConfig {
        constructor({ id = "", name = "", sprite = null, configDirectory = "", }) {
            this.id = id;
            this.name = name;
            this.sprite = sprite;
            this.configDirectory = configDirectory;
        }
    }
    Charactor.CharactorConfig = CharactorConfig;
    class CharactorBase extends SpriteAnimation.Animator {
        constructor(x, y, spriteSheet) {
            super(spriteSheet);
            this.x = x;
            this.y = y;
        }
    }
    Charactor.CharactorBase = CharactorBase;
    class Player extends CharactorBase {
        constructor(config) {
            const charactorConfig = Player.playerConfigs.get(config.charactorId);
            super(config.x, config.y, charactorConfig.sprite);
            this.charactorConfig = charactorConfig;
            this.hp = 100;
            this.hpMax = 100;
            this.mp = 100;
            this.mpMax = 100;
            this.gold = 0;
            this.equips = [
                { name: "竹刀", atk: 5, def: 0 },
                { name: "体操着", atk: 0, def: 3 },
                { name: "ブルマ", atk: 0, def: 2 },
            ];
        }
        static loadCharactorConfigs(loadStartCallback, loadEndCallback) {
            return __awaiter(this, void 0, void 0, function* () {
                loadStartCallback();
                const configPaths = yield ajax(Player.configFilePath, "json").then((x) => x.response);
                loadStartCallback();
                const rootDirectory = getDirectory(Player.configFilePath);
                const configs = yield Promise.all(configPaths.map(x => loadCharactorConfigFromFile(rootDirectory + '/' + x, loadStartCallback, loadEndCallback)));
                Player.playerConfigs = configs.reduce((s, x) => s.set(x.id, x), new Map());
                return;
            });
        }
        get atk() {
            return this.equips.reduce((s, x) => s += x.atk, 0);
        }
        get def() {
            return this.equips.reduce((s, x) => s += x.atk, 0);
        }
    }
    Player.configFilePath = "./assets/charactor/charactor.json";
    Player.playerConfigs = new Map();
    Charactor.Player = Player;
    class Monster extends CharactorBase {
        constructor(config) {
            const charactorConfig = Monster.monsterConfigs.get(config.charactorId);
            super(config.x, config.y, charactorConfig.sprite);
            this.charactorConfig = charactorConfig;
            this.life = config.life;
            this.maxLife = config.maxLife;
            this.atk = config.atk;
            this.def = config.def;
        }
        static loadCharactorConfigs(loadStartCallback, loadEndCallback) {
            return __awaiter(this, void 0, void 0, function* () {
                const configPaths = yield ajax(Monster.configFilePath, "json").then((x) => x.response);
                const rootDirectory = getDirectory(Monster.configFilePath);
                loadStartCallback();
                const configs = yield Promise.all(configPaths.map(x => loadCharactorConfigFromFile(rootDirectory + '/' + x, loadStartCallback, loadEndCallback)));
                loadEndCallback();
                Monster.monsterConfigs = configs.reduce((s, x) => s.set(x.id, x), new Map());
                return;
            });
        }
    }
    Monster.configFilePath = "./assets/monster/monster.json";
    Monster.monsterConfigs = new Map();
    Charactor.Monster = Monster;
})(Charactor || (Charactor = {}));
var Scene;
(function (Scene) {
    function* boot() {
        let n = 0;
        let reqResource = 0;
        let loadedResource = 0;
        this.draw = () => {
            Game.getScreen().fillStyle = "rgb(255,255,255)";
            Game.getScreen().fillRect(0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            Game.getScreen().save();
            Game.getScreen().translate(Game.getScreen().offscreenWidth / 2, Game.getScreen().offscreenHeight / 2);
            Game.getScreen().rotate(n * Math.PI / 4);
            for (let i = 0; i < 8; i++) {
                const g = (i * 32);
                Game.getScreen().save();
                Game.getScreen().rotate(i * Math.PI / 4);
                Game.getScreen().fillStyle = `rgb(${g},${g},${g})`;
                Game.getScreen().fillRect(-5, -50, 10, 25);
                Game.getScreen().restore();
            }
            Game.getScreen().restore();
            Game.getScreen().fillStyle = "rgb(0,0,0)";
            const text = `loading ${loadedResource}/${reqResource}`;
            const size = Game.getScreen().measureText(text);
            Game.getScreen().fillText(text, Game.getScreen().offscreenWidth / 2 - size.width / 2, Game.getScreen().offscreenHeight - 20);
        };
        Promise.all([
            Game.getScreen().loadImage({
                title: "./assets/title.png",
                mapchip: "./assets/mapchip.png",
                charactor: "./assets/charactor.png",
                font7px: "./assets/font7px.png",
                "shop/bg": "./assets/shop/bg.png",
                "shop/J11": "./assets/shop/J11.png",
            }, () => { reqResource++; }, () => { loadedResource++; }),
            Game.getSound().loadSoundsToChannel({
                title: "./assets/sound/title.mp3",
                dungeon: "./assets/sound/dungeon.mp3",
                classroom: "./assets/sound/classroom.mp3",
                kaidan: "./assets/sound/kaidan.mp3",
                atack: "./assets/sound/se_attacksword_1.mp3",
                explosion: "./assets/sound/explosion03.mp3",
                cursor: "./assets/sound/cursor.mp3",
            }, () => { reqResource++; }, () => { loadedResource++; }).catch((ev) => console.log("failed2", ev)),
            Promise.resolve().then(() => {
                reqResource++;
                return new FontFace("PixelMplus10-Regular", "url(./assets/font/PixelMplus10-Regular.woff2)", {}).load();
            }).then((loadedFontFace) => {
                document.fonts.add(loadedFontFace);
                loadedResource++;
            })
        ]).then(() => {
            Game.getSceneManager().push(Scene.shop, null);
            this.next();
        });
        yield (delta, ms) => {
            n = ~(ms / 50);
        };
    }
    Scene.boot = boot;
})(Scene || (Scene = {}));
var Scene;
(function (Scene) {
    function* classroom() {
        let selectedCharactor = -1;
        let selectedCharactorDir = 0;
        let selectedCharactorOffY = 0;
        const fade = new Scene.Fade(Game.getScreen().offscreenHeight, Game.getScreen().offscreenHeight);
        this.draw = () => {
            const w = Game.getScreen().offscreenWidth;
            const h = Game.getScreen().offscreenHeight;
            Game.getScreen().save();
            Game.getScreen().fillStyle = "rgb(255,255,255)";
            Game.getScreen().fillRect(0, 0, w, h);
            for (let y = 0; y < ~~((w + 23) / 24); y++) {
                for (let x = 0; x < ~~((w + 23) / 24); x++) {
                    Game.getScreen().drawImage(Game.getScreen().texture("mapchip"), 0, 0, 24, 24, x * 24, y * 24, 24, 24);
                }
            }
            for (let y = 0; y < 2; y++) {
                for (let x = 0; x < ~~((w + 23) / 24); x++) {
                    Game.getScreen().drawImage(Game.getScreen().texture("mapchip"), 120, 96, 24, 24, x * 24, y * 24 - 23, 24, 24);
                }
            }
            Game.getScreen().drawImage(Game.getScreen().texture("mapchip"), 0, 204, 72, 36, 90, -12, 72, 36);
            for (let y = 0; y < 5; y++) {
                for (let x = 0; x < 6; x++) {
                    const id = y * 6 + x;
                    Game.getScreen().drawImage(Game.getScreen().texture("charactor"), 752 * (id % 2) +
                        ((selectedCharactor !== id) ? 0 : (188 * (selectedCharactorDir % 4))), 47 * ~~(id / 2), 47, 47, 12 + x * 36, 24 + y * (48 - 7) - ((selectedCharactor !== id) ? 0 : (selectedCharactorOffY)), 47, 47);
                    Game.getScreen().drawImage(Game.getScreen().texture("mapchip"), 72, 180, 24, 24, 24 + x * 36, 48 + y * (48 - 7), 24, 24);
                }
            }
            fade.draw();
            Game.getScreen().restore();
        };
        {
            Game.getSound().reqPlayChannel("classroom", true);
            yield Scene.waitTimeout({
                timeout: 500,
                init: () => { fade.startFadeIn(); },
                update: (e) => { fade.update(e); },
                end: () => {
                    fade.stop();
                    this.next();
                },
            });
        }
        yield Scene.waitClick({
            check: (x, y) => {
                const xx = ~~((x - 12) / 36);
                const yy = ~~((y - 24) / (48 - 7));
                return (0 <= xx && xx < 6 && 0 <= yy && yy < 5);
            },
            end: (x, y) => {
                Game.getSound().reqPlayChannel("title");
                const xx = ~~((x - 12) / 36);
                const yy = ~~((y - 24) / (48 - 7));
                selectedCharactor = yy * 6 + xx;
                this.next();
            },
        });
        yield Scene.waitTimeout({
            timeout: 1800,
            init: () => {
                selectedCharactorDir = 0;
                selectedCharactorOffY = 0;
            },
            update: (e) => {
                if (0 <= e && e < 1600) {
                    selectedCharactorDir = ~~(e / 100);
                    selectedCharactorOffY = 0;
                }
                else if (1600 <= e && e < 1800) {
                    selectedCharactorDir = 0;
                    selectedCharactorOffY = Math.sin((e - 1600) * Math.PI / 200) * 20;
                }
            },
            end: () => { this.next(); },
        });
        yield Scene.waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeOut(); },
            update: (e) => { fade.update(e); },
            end: () => { this.next(); },
        });
        const player = new Charactor.Player({
            charactorId: Charactor.Player.playerConfigs.get("_u" + ("0" + (selectedCharactor + 1)).substr(-2)).id,
            x: 0,
            y: 0,
        });
        Game.getSound().reqStopChannel("classroom");
        Game.getSceneManager().pop();
        Game.getSceneManager().push(Scene.dungeon, { player: player, floor: 1 });
    }
    Scene.classroom = classroom;
})(Scene || (Scene = {}));
var Scene;
(function (Scene) {
    let TurnState;
    (function (TurnState) {
        TurnState[TurnState["WaitInput"] = 0] = "WaitInput";
        TurnState[TurnState["PlayerAction"] = 1] = "PlayerAction";
        TurnState[TurnState["PlayerActionRunning"] = 2] = "PlayerActionRunning";
        TurnState[TurnState["EnemyAI"] = 3] = "EnemyAI";
        TurnState[TurnState["EnemyAction"] = 4] = "EnemyAction";
        TurnState[TurnState["EnemyActionRunning"] = 5] = "EnemyActionRunning";
        TurnState[TurnState["EnemyDead"] = 6] = "EnemyDead";
        TurnState[TurnState["EnemyDeadRunning"] = 7] = "EnemyDeadRunning";
        TurnState[TurnState["Move"] = 8] = "Move";
        TurnState[TurnState["MoveRunning"] = 9] = "MoveRunning";
        TurnState[TurnState["TurnEnd"] = 10] = "TurnEnd";
    })(TurnState || (TurnState = {}));
    function* dungeon(param) {
        const player = param.player;
        const floor = param.floor;
        const mapChipW = 30 + floor * 3;
        const mapChipH = 30 + floor * 3;
        const mapchipsL1 = new Array2D(mapChipW, mapChipH);
        const layout = Dungeon.generate(mapChipW, mapChipH, (x, y, v) => { mapchipsL1.value(x, y, v ? 0 : 1); });
        for (let y = 1; y < mapChipH; y++) {
            for (let x = 0; x < mapChipW; x++) {
                mapchipsL1.value(x, y - 1, mapchipsL1.value(x, y) === 1 && mapchipsL1.value(x, y - 1) === 0
                    ? 2
                    : mapchipsL1.value(x, y - 1));
            }
        }
        const mapchipsL2 = new Array2D(mapChipW, mapChipH);
        for (let y = 0; y < mapChipH; y++) {
            for (let x = 0; x < mapChipW; x++) {
                mapchipsL2.value(x, y, (mapchipsL1.value(x, y) === 0) ? 0 : 1);
            }
        }
        const rooms = layout.rooms.slice();
        const startPos = rooms[0].getCenter();
        player.x = startPos.x;
        player.y = startPos.y;
        const stairsPos = rooms[1].getCenter();
        mapchipsL1.value(stairsPos.x, stairsPos.y, 10);
        let monsters = rooms.splice(2).map((x) => {
            return new Charactor.Monster({
                charactorId: Charactor.Monster.monsterConfigs.get("slime").id,
                x: x.getLeft(),
                y: x.getTop(),
                life: floor + 5,
                maxLife: floor + 5,
                atk: ~~(floor * 2),
                def: ~~(floor / 3) + 1
            });
        });
        const map = new Dungeon.DungeonData({
            width: mapChipW,
            height: mapChipW,
            gridsize: { width: 24, height: 24 },
            layer: {
                0: {
                    texture: "mapchip",
                    chip: {
                        1: { x: 48, y: 0 },
                        2: { x: 96, y: 96 },
                        10: { x: 96, y: 0 },
                    },
                    chips: mapchipsL1,
                },
                1: {
                    texture: "mapchip",
                    chip: {
                        0: { x: 96, y: 72 },
                    },
                    chips: mapchipsL2,
                },
            },
        });
        map.update({
            viewpoint: {
                x: (player.x * map.gridsize.width + player.offx) + map.gridsize.width / 2,
                y: (player.y * map.gridsize.height + player.offy) + map.gridsize.height / 2,
            },
            viewwidth: Game.getScreen().offscreenWidth,
            viewheight: Game.getScreen().offscreenHeight,
        });
        Game.getSound().reqPlayChannel("dungeon", true);
        const pad = new Game.Input.VirtualStick();
        const pointerdown = (ev) => {
            if (pad.onpointingstart(ev.pointerId)) {
                const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
                pad.x = pos[0];
                pad.y = pos[1];
            }
        };
        const pointermove = (ev) => {
            const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
            pad.onpointingmove(ev.pointerId, pos[0], pos[1]);
        };
        const pointerup = (ev) => {
            pad.onpointingend(ev.pointerId);
        };
        const onPointerHook = () => {
            Game.getInput().on("pointerdown", pointerdown);
            Game.getInput().on("pointermove", pointermove);
            Game.getInput().on("pointerup", pointerup);
            Game.getInput().on("pointerleave", pointerup);
        };
        const offPointerHook = () => {
            Game.getInput().off("pointerdown", pointerdown);
            Game.getInput().off("pointermove", pointermove);
            Game.getInput().off("pointerup", pointerup);
            Game.getInput().off("pointerleave", pointerup);
        };
        this.suspend = () => {
            offPointerHook();
            Game.getSound().reqStopChannel("dungeon");
        };
        this.resume = () => {
            onPointerHook();
            Game.getSound().reqPlayChannel("dungeon", true);
        };
        this.leave = () => {
            offPointerHook();
            Game.getSound().reqStopChannel("dungeon");
        };
        const updateLighting = (iswalkable) => {
            map.clearLighting();
            PathFinder.calcDistanceByDijkstra({
                array2D: map.layer[0].chips,
                sx: player.x,
                sy: player.y,
                value: 140,
                costs: (v) => iswalkable(v) ? 20 : 50,
                output: (x, y, v) => {
                    map.lighting.value(x, y, v);
                    if (map.visibled.value(x, y) < v) {
                        map.visibled.value(x, y, v);
                    }
                },
            });
        };
        const fade = new Scene.Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
        let sprites = [];
        this.draw = () => {
            Game.getScreen().save();
            Game.getScreen().fillStyle = "rgb(255,255,255)";
            Game.getScreen().fillRect(0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            map.draw((l, cameraLocalPx, cameraLocalPy) => {
                if (l === 0) {
                    Game.getScreen().fillStyle = "rgba(0,0,0,0.25)";
                    Game.getScreen().beginPath();
                    Game.getScreen().ellipse(cameraLocalPx, cameraLocalPy + 7, 12, 3, 0, 0, Math.PI * 2);
                    Game.getScreen().fill();
                    const camera = map.camera;
                    monsters.forEach((monster) => {
                        const xx = monster.x - camera.chipLeft;
                        const yy = monster.y - camera.chipTop;
                        if ((0 <= xx && xx < Game.getScreen().offscreenWidth / 24) &&
                            (0 <= yy && yy < Game.getScreen().offscreenHeight / 24)) {
                            const animFrame = monster.spriteSheet.getAnimationFrame(monster.animName, monster.animFrame);
                            const sprite = monster.spriteSheet.gtetSprite(animFrame.sprite);
                            const dx = xx * map.gridsize.width +
                                camera.chipOffX +
                                monster.offx +
                                sprite.offsetX +
                                animFrame.offsetX;
                            const dy = yy * map.gridsize.height +
                                camera.chipOffY +
                                monster.offy +
                                sprite.offsetY +
                                animFrame.offsetY;
                            Game.getScreen().drawImage(monster.spriteSheet.getSpriteImage(sprite), sprite.left, sprite.top, sprite.width, sprite.height, dx, dy, sprite.width, sprite.height);
                        }
                    });
                    {
                        const animFrame = player.spriteSheet.getAnimationFrame(player.animName, player.animFrame);
                        const sprite = player.spriteSheet.gtetSprite(animFrame.sprite);
                        Game.getScreen().drawImage(player.spriteSheet.getSpriteImage(sprite), sprite.left, sprite.top, sprite.width, sprite.height, cameraLocalPx - sprite.width / 2 + sprite.offsetX + animFrame.offsetX, cameraLocalPy - sprite.height / 2 + sprite.offsetY + animFrame.offsetY, sprite.width, sprite.height);
                    }
                }
                if (l === 1) {
                    const camera = map.camera;
                    monsters.forEach((monster) => {
                        const xx = monster.x - camera.chipLeft;
                        const yy = monster.y - camera.chipTop;
                        if ((0 <= xx && xx < Game.getScreen().offscreenWidth / 24) &&
                            (0 <= yy && yy < Game.getScreen().offscreenHeight / 24)) {
                            const animFrame = monster.spriteSheet.getAnimationFrame(monster.animName, monster.animFrame);
                            const sprite = monster.spriteSheet.gtetSprite(animFrame.sprite);
                            const dx = xx * map.gridsize.width +
                                camera.chipOffX +
                                monster.offx +
                                sprite.offsetX +
                                animFrame.offsetX;
                            const dy = yy * map.gridsize.height +
                                camera.chipOffY +
                                monster.offy +
                                sprite.offsetY +
                                animFrame.offsetY;
                            Game.getScreen().fillStyle = 'rgb(255,0,0)';
                            Game.getScreen().fillRect(dx, dy + sprite.height - 1, map.gridsize.width, 1);
                            Game.getScreen().fillStyle = 'rgb(0,255,0)';
                            Game.getScreen().fillRect(dx, dy + sprite.height - 1, ~~(map.gridsize.width * monster.life / monster.maxLife), 1);
                        }
                    });
                    {
                        const animFrame = player.spriteSheet.getAnimationFrame(player.animName, player.animFrame);
                        const sprite = player.spriteSheet.gtetSprite(animFrame.sprite);
                        Game.getScreen().fillStyle = 'rgb(255,0,0)';
                        Game.getScreen().fillRect(cameraLocalPx - map.gridsize.width / 2 + sprite.offsetX + animFrame.offsetX, cameraLocalPy - sprite.height / 2 + sprite.offsetY + animFrame.offsetY + sprite.height - 1, map.gridsize.width, 1);
                        Game.getScreen().fillStyle = 'rgb(0,255,0)';
                        Game.getScreen().fillRect(cameraLocalPx - map.gridsize.width / 2 + sprite.offsetX + animFrame.offsetX, cameraLocalPy - sprite.height / 2 + sprite.offsetY + animFrame.offsetY + sprite.height - 1, ~~(map.gridsize.width * player.hp / player.hpMax), 1);
                    }
                }
            });
            sprites.forEach((x) => x.draw(map.camera));
            draw7pxFont(`${floor}F | HP:${player.hp}/${player.hpMax} | MP:${player.mp}/${player.mpMax} | GOLD:${player.gold}`, 0, 0);
            fade.draw();
            Game.getScreen().restore();
            if (pad.isTouching) {
                Game.getScreen().fillStyle = "rgba(255,255,255,0.25)";
                Game.getScreen().beginPath();
                Game.getScreen().ellipse(pad.x, pad.y, pad.radius * 1.2, pad.radius * 1.2, 0, 0, Math.PI * 2);
                Game.getScreen().fill();
                Game.getScreen().beginPath();
                Game.getScreen().ellipse(pad.x + pad.cx, pad.y + pad.cy, pad.radius, pad.radius, 0, 0, Math.PI * 2);
                Game.getScreen().fill();
            }
        };
        yield Scene.waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeIn(); },
            update: (e) => {
                fade.update(e);
                updateLighting((v) => v === 1 || v === 10);
            },
            end: () => { this.next(); },
        });
        onPointerHook();
        const turnStateStack = [[TurnState.WaitInput, null]];
        let playerTactics = {};
        const monstersTactics = [];
        yield (delta, ms) => {
            stateloop: for (;;) {
                switch (turnStateStack[0][0]) {
                    case TurnState.WaitInput:
                        {
                            if (pad.isTouching === false || pad.distance <= 0.4) {
                                player.setAnimation("move", 0);
                                break stateloop;
                            }
                            const playerMoveDir = pad.dir8;
                            const { x, y } = Array2D.DIR8[playerMoveDir];
                            if (map.layer[0].chips.value(player.x + x, player.y + y) !== 1 &&
                                map.layer[0].chips.value(player.x + x, player.y + y) !== 10) {
                                player.setDir(playerMoveDir);
                                break stateloop;
                            }
                            const targetMonster = monsters.findIndex((monster) => (monster.x === player.x + x) &&
                                (monster.y === player.y + y));
                            if (targetMonster !== -1) {
                                playerTactics = {
                                    type: "action",
                                    moveDir: playerMoveDir,
                                    targetMonster: targetMonster,
                                    startTime: ms,
                                    actionTime: 250,
                                };
                                turnStateStack.unshift([TurnState.PlayerAction, null], [TurnState.EnemyAI, null], [TurnState.EnemyAction, 0], [TurnState.Move, null], [TurnState.TurnEnd, null]);
                                continue stateloop;
                            }
                            else {
                                playerTactics = {
                                    type: "move",
                                    moveDir: playerMoveDir,
                                    startTime: ms,
                                    actionTime: 250,
                                };
                                turnStateStack.unshift([TurnState.EnemyAI, null], [TurnState.Move, null], [TurnState.EnemyAction, 0], [TurnState.TurnEnd, null]);
                                continue stateloop;
                            }
                        }
                    case TurnState.PlayerAction:
                        {
                            turnStateStack[0][0] = TurnState.PlayerActionRunning;
                            turnStateStack[0][1] = 0;
                            player.setDir(playerTactics.moveDir);
                            player.setAnimation("action", 0);
                        }
                    case TurnState.PlayerActionRunning:
                        {
                            const rate = (ms - playerTactics.startTime) / playerTactics.actionTime;
                            player.setAnimation("action", rate);
                            if (rate > 0.5 && turnStateStack[0][1] === 0) {
                                const targetMonster = monsters[playerTactics.targetMonster];
                                turnStateStack[0][1] = 1;
                                Game.getSound().reqPlayChannel("atack");
                                const dmg = ~~(player.atk - targetMonster.def);
                                sprites.push(createShowDamageSprite(ms, dmg > 0 ? ("" + dmg) : "MISS!!", () => {
                                    return {
                                        x: targetMonster.offx +
                                            targetMonster.x * map.gridsize.width +
                                            map.gridsize.width / 2,
                                        y: targetMonster.offy +
                                            targetMonster.y * map.gridsize.height +
                                            map.gridsize.height / 2
                                    };
                                }));
                                if (targetMonster.life > 0 && dmg > 0) {
                                    targetMonster.life -= dmg;
                                    if (targetMonster.life <= 0) {
                                        targetMonster.life = 0;
                                        Game.getSound().reqPlayChannel("explosion");
                                        turnStateStack.splice(1, 0, [TurnState.EnemyDead, playerTactics.targetMonster, 0]);
                                    }
                                }
                            }
                            if (rate >= 1) {
                                turnStateStack.shift();
                                player.setAnimation("move", 0);
                            }
                            break stateloop;
                        }
                    case TurnState.EnemyAI:
                        {
                            let px = player.x;
                            let py = player.y;
                            if (playerTactics.type === "move") {
                                const off = Array2D.DIR8[playerTactics.moveDir];
                                px += off.x;
                                py += off.y;
                            }
                            const cannotMoveMap = new Array2D(map.width, map.height, 0);
                            monstersTactics.length = monsters.length;
                            monstersTactics.fill(null);
                            monsters.forEach((monster, i) => {
                                if (monster.life <= 0) {
                                    monstersTactics[i] = {
                                        type: "dead",
                                        moveDir: 5,
                                        startTime: 0,
                                        actionTime: 250,
                                    };
                                    return;
                                }
                                const dx = px - monster.x;
                                const dy = py - monster.y;
                                if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) {
                                    const dir = Array2D.DIR8.findIndex((x) => x.x === dx && x.y === dy);
                                    cannotMoveMap.value(monster.x, monster.y, 1);
                                    monstersTactics[i] = {
                                        type: "action",
                                        moveDir: dir,
                                        startTime: 0,
                                        actionTime: 250,
                                    };
                                    return;
                                }
                                else {
                                    return;
                                }
                            });
                            let changed = true;
                            while (changed) {
                                changed = false;
                                monsters.forEach((monster, i) => {
                                    const dx = px - monster.x;
                                    const dy = py - monster.y;
                                    if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) {
                                        if (monstersTactics[i] == null) {
                                            console.error("Actionすべき敵の動作が決定していない");
                                        }
                                        return;
                                    }
                                    else if (monstersTactics[i] == null) {
                                        const cands = [
                                            [Math.sign(dx), Math.sign(dy)],
                                            (Math.abs(dx) > Math.abs(dy)) ? [0, Math.sign(dy)] : [Math.sign(dx), 0],
                                            (Math.abs(dx) > Math.abs(dy)) ? [Math.sign(dx), 0] : [0, Math.sign(dy)],
                                        ];
                                        for (let j = 0; j < 3; j++) {
                                            const [cx, cy] = cands[j];
                                            const tx = monster.x + cx;
                                            const ty = monster.y + cy;
                                            if ((cannotMoveMap.value(tx, ty) === 0) &&
                                                (map.layer[0].chips.value(tx, ty) === 1 ||
                                                    map.layer[0].chips.value(tx, ty) === 10)) {
                                                const dir = Array2D.DIR8.findIndex((x) => x.x === cx && x.y === cy);
                                                cannotMoveMap.value(tx, ty, 1);
                                                monstersTactics[i] = {
                                                    type: "move",
                                                    moveDir: dir,
                                                    startTime: ms,
                                                    actionTime: 250,
                                                };
                                                changed = true;
                                                return;
                                            }
                                        }
                                        cannotMoveMap.value(monster.x, monster.y, 1);
                                        monstersTactics[i] = {
                                            type: "idle",
                                            moveDir: 5,
                                            startTime: ms,
                                            actionTime: 250,
                                        };
                                        changed = true;
                                        return;
                                    }
                                });
                            }
                            turnStateStack.shift();
                            continue stateloop;
                        }
                    case TurnState.EnemyAction:
                        {
                            let enemyId = turnStateStack[0][1];
                            while (enemyId < monstersTactics.length) {
                                if (monstersTactics[enemyId].type !== "action") {
                                    enemyId++;
                                }
                                else {
                                    break;
                                }
                            }
                            if (enemyId < monstersTactics.length) {
                                monstersTactics[enemyId].startTime = ms;
                                monsters[enemyId].setDir(monstersTactics[enemyId].moveDir);
                                monsters[enemyId].setAnimation("action", 0);
                                turnStateStack[0][0] = TurnState.EnemyActionRunning;
                                turnStateStack[0][1] = enemyId;
                                turnStateStack[0][2] = 0;
                                continue stateloop;
                            }
                            else {
                                turnStateStack.shift();
                                continue stateloop;
                            }
                        }
                    case TurnState.EnemyActionRunning:
                        {
                            const enemyId = turnStateStack[0][1];
                            const rate = (ms - monstersTactics[enemyId].startTime) / monstersTactics[enemyId].actionTime;
                            monsters[enemyId].setAnimation("action", rate);
                            if (rate > 0.5 && turnStateStack[0][2] === 0) {
                                turnStateStack[0][2] = 1;
                                Game.getSound().reqPlayChannel("atack");
                                const dmg = ~~(monsters[enemyId].atk - player.def);
                                sprites.push(createShowDamageSprite(ms, dmg > 0 ? ("" + dmg) : "MISS!!", () => {
                                    return {
                                        x: player.offx + player.x * map.gridsize.width + map.gridsize.width / 2,
                                        y: player.offy + player.y * map.gridsize.height + map.gridsize.height / 2
                                    };
                                }));
                                if (player.hp > 0 && dmg > 0) {
                                    player.hp -= dmg;
                                    if (player.hp <= 0) {
                                        player.hp = 0;
                                    }
                                }
                            }
                            if (rate >= 1) {
                                if (player.hp == 0) {
                                    return;
                                }
                                monsters[enemyId].setAnimation("move", 0);
                                turnStateStack[0][0] = TurnState.EnemyAction;
                                turnStateStack[0][1] = enemyId + 1;
                            }
                            break stateloop;
                        }
                    case TurnState.EnemyDead:
                        {
                            turnStateStack[0][0] = TurnState.EnemyDeadRunning;
                            const enemyId = turnStateStack[0][1];
                            turnStateStack[0][2] = ms;
                            Game.getSound().reqPlayChannel("explosion");
                            monsters[enemyId].setAnimation("dead", 0);
                        }
                    case TurnState.EnemyDeadRunning:
                        {
                            turnStateStack[0][0] = TurnState.EnemyDeadRunning;
                            const enemyId = turnStateStack[0][1];
                            const diff = ms - turnStateStack[0][2];
                            monsters[enemyId].setAnimation("dead", diff / 250);
                            if (diff >= 250) {
                                turnStateStack.shift();
                            }
                            break stateloop;
                        }
                    case TurnState.Move:
                        {
                            turnStateStack[0][0] = TurnState.MoveRunning;
                            monstersTactics.forEach((monsterTactic, i) => {
                                if (monsterTactic.type === "move") {
                                    monsters[i].setDir(monsterTactic.moveDir);
                                    monsters[i].setAnimation("move", 0);
                                    monstersTactics[i].startTime = ms;
                                }
                            });
                            if (playerTactics.type === "move") {
                                player.setDir(playerTactics.moveDir);
                                player.setAnimation("move", 0);
                                playerTactics.startTime = ms;
                            }
                        }
                    case TurnState.MoveRunning:
                        {
                            let finish = true;
                            monstersTactics.forEach((monsterTactic, i) => {
                                if (monsterTactic == null) {
                                    return;
                                }
                                if (monsterTactic.type === "move") {
                                    const rate = (ms - monsterTactic.startTime) / monsterTactic.actionTime;
                                    monsters[i].setDir(monsterTactic.moveDir);
                                    monsters[i].setAnimation("move", rate);
                                    if (rate < 1) {
                                        finish = false;
                                    }
                                }
                            });
                            if (playerTactics.type === "move") {
                                const rate = (ms - playerTactics.startTime) / playerTactics.actionTime;
                                player.setDir(playerTactics.moveDir);
                                player.setAnimation("move", rate);
                                if (rate < 1) {
                                    finish = false;
                                }
                            }
                            if (finish) {
                                turnStateStack.shift();
                                monstersTactics.forEach((monsterTactic, i) => {
                                    if (monsterTactic.type === "move") {
                                        monsters[i].x += Array2D.DIR8[monsterTactic.moveDir].x;
                                        monsters[i].y += Array2D.DIR8[monsterTactic.moveDir].y;
                                        monsters[i].offx = 0;
                                        monsters[i].offy = 0;
                                        monsters[i].setAnimation("move", 0);
                                    }
                                });
                                if (playerTactics.type === "move") {
                                    player.x += Array2D.DIR8[playerTactics.moveDir].x;
                                    player.y += Array2D.DIR8[playerTactics.moveDir].y;
                                    player.offx = 0;
                                    player.offy = 0;
                                    player.setAnimation("move", 0);
                                }
                                const chip = map.layer[0].chips.value(~~player.x, ~~player.y);
                                if (chip === 10) {
                                    this.next("nextfloor");
                                }
                            }
                            break stateloop;
                        }
                    case TurnState.TurnEnd:
                        {
                            turnStateStack.shift();
                            monsters = monsters.filter(x => x.life > 0);
                            break stateloop;
                        }
                }
                break;
            }
            map.update({
                viewpoint: {
                    x: (player.x * map.gridsize.width + player.offx) + map.gridsize.width / 2,
                    y: (player.y * map.gridsize.height + player.offy) + map.gridsize.height / 2,
                },
                viewwidth: Game.getScreen().offscreenWidth,
                viewheight: Game.getScreen().offscreenHeight,
            });
            sprites = sprites.filter((x) => {
                return !x.update(delta, ms);
            });
            updateLighting((v) => v === 1 || v === 10);
            if (player.hp === 0) {
                Game.getSceneManager().pop();
                Game.getSceneManager().push(gameOver, { player: player, floor: floor, upperdraw: this.draw });
                return;
            }
            if (Game.getInput().isClick() &&
                Game.getScreen().pagePointContainScreen(Game.getInput().pageX, Game.getInput().pageY)) {
                Game.getSceneManager().push(statusView, { player: player, floor: floor, upperdraw: this.draw });
            }
        };
        Game.getSound().reqPlayChannel("kaidan");
        yield Scene.waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeOut(); },
            update: (e) => {
                fade.update(e);
                updateLighting((v) => v === 1 || v === 10);
            },
            end: () => { this.next(); },
        });
        yield Scene.waitTimeout({
            timeout: 500,
            end: () => { this.next(); },
        });
        Game.getSceneManager().pop();
        Game.getSceneManager().push(dungeon, { player: player, floor: floor + 1 });
    }
    Scene.dungeon = dungeon;
    function* statusView(opt) {
        var closeButton = {
            x: Game.getScreen().offscreenWidth - 20,
            y: 20,
            radius: 10
        };
        this.draw = () => {
            opt.upperdraw();
            Game.getScreen().fillStyle = 'rgba(255,255,255,0.5)';
            Game.getScreen().fillRect(20, 20, Game.getScreen().offscreenWidth - 40, Game.getScreen().offscreenHeight - 40);
            Game.getScreen().save();
            Game.getScreen().beginPath();
            Game.getScreen().strokeStyle = 'rgba(255,255,255,1)';
            Game.getScreen().lineWidth = 6;
            Game.getScreen().ellipse(closeButton.x, closeButton.y, closeButton.radius, closeButton.radius, 0, 0, 360);
            Game.getScreen().moveTo(closeButton.x - Math.sqrt(2) * closeButton.radius / 2, closeButton.y - Math.sqrt(2) * closeButton.radius / 2);
            Game.getScreen().lineTo(closeButton.x + Math.sqrt(2) * closeButton.radius / 2, closeButton.y + Math.sqrt(2) * closeButton.radius / 2);
            Game.getScreen().moveTo(closeButton.x - Math.sqrt(2) * closeButton.radius / 2, closeButton.y + Math.sqrt(2) * closeButton.radius / 2);
            Game.getScreen().lineTo(closeButton.x + Math.sqrt(2) * closeButton.radius / 2, closeButton.y - Math.sqrt(2) * closeButton.radius / 2);
            Game.getScreen().stroke();
            Game.getScreen().strokeStyle = 'rgba(128,255,255,1)';
            Game.getScreen().lineWidth = 3;
            Game.getScreen().stroke();
            Game.getScreen().restore();
            Game.getScreen().fillStyle = 'rgb(0,0,0)';
            Game.getScreen().font = "10px 'PixelMplus10-Regular'";
            Game.getScreen().fillText(`HP:${opt.player.hp}/${opt.player.hpMax}`, 30, 30 + 11 * 3);
            Game.getScreen().fillText(`MP:${opt.player.mp}/${opt.player.mpMax}`, 30, 30 + 11 * 4);
            Game.getScreen().fillText(`ATK：${opt.player.atk}`, 30, 30 + 11 * 6);
            Game.getScreen().fillText(`DEF：${opt.player.def}`, 30, 30 + 11 * 7);
            opt.player.equips.forEach((e, i) => {
                Game.getScreen().fillText(`${e.name}`, 30, 30 + 11 * (8 + i));
            });
        };
        yield Scene.waitClick({
            end: (x, y) => {
                this.next();
            }
        });
        Game.getSceneManager().pop();
    }
    function* gameOver(opt) {
        const fade = new Scene.Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
        let fontAlpha = 0;
        this.draw = () => {
            opt.upperdraw();
            fade.draw();
            Game.getScreen().fillStyle = `rgba(255,255,255,${fontAlpha})`;
            Game.getScreen().font = "20px 'PixelMplus10-Regular'";
            const shape = Game.getScreen().measureText(`GAME OVER`);
            Game.getScreen().fillText(`GAME OVER`, (Game.getScreen().offscreenWidth - shape.width) / 2, (Game.getScreen().offscreenHeight - 20) / 2);
        };
        yield Scene.waitTimeout({
            timeout: 500,
            start: (e, ms) => { fade.startFadeOut(); },
            update: (e, ms) => { fade.update(ms); },
            end: (x, y) => { this.next(); }
        });
        yield Scene.waitTimeout({
            timeout: 500,
            start: (e, ms) => { fontAlpha = 0; },
            update: (e, ms) => { fontAlpha = e / 500; },
            end: (x, y) => { fontAlpha = 1; this.next(); }
        });
        yield Scene.waitClick({
            end: (x, y) => {
                this.next();
            }
        });
        Game.getSceneManager().pop();
        Game.getSceneManager().push(Scene.title);
    }
})(Scene || (Scene = {}));
var Scene;
(function (Scene) {
    class Fade {
        constructor(w, h) {
            this.startTime = -1;
            this.started = false;
            this.w = w;
            this.h = h;
            this.mode = "";
        }
        startFadeOut() {
            this.started = true;
            this.startTime = -1;
            this.rate = 0;
            this.mode = "fadeout";
        }
        startFadeIn() {
            this.started = true;
            this.startTime = -1;
            this.rate = 1;
            this.mode = "fadein";
        }
        stop() {
            this.started = false;
            this.startTime = -1;
        }
        update(ms) {
            if (this.started === false) {
                return;
            }
            if (this.startTime === -1) {
                this.startTime = ms;
            }
            this.rate = (ms - this.startTime) / 500;
            if (this.rate < 0) {
                this.rate = 0;
            }
            else if (this.rate > 1) {
                this.rate = 1;
            }
            if (this.mode === "fadein") {
                this.rate = 1 - this.rate;
            }
        }
        draw() {
            if (this.started) {
                Game.getScreen().fillStyle = `rgba(0,0,0,${this.rate})`;
                Game.getScreen().fillRect(0, 0, this.w, this.h);
            }
        }
    }
    Scene.Fade = Fade;
    function waitTimeout({ timeout, init = () => { }, start = () => { }, update = () => { }, end = () => { }, }) {
        let startTime = -1;
        init();
        return (delta, ms) => {
            if (startTime === -1) {
                startTime = ms;
                start(0, ms);
            }
            const elapsed = ms - startTime;
            if (elapsed >= timeout) {
                end(elapsed, ms);
            }
            else {
                update(elapsed, ms);
            }
        };
    }
    Scene.waitTimeout = waitTimeout;
    function waitClick({ update = () => { }, start = () => { }, check = () => true, end = () => { }, }) {
        let startTime = -1;
        return (delta, ms) => {
            if (startTime === -1) {
                startTime = ms;
                start(0, ms);
            }
            const elapsed = ms - startTime;
            if (Game.getInput().isClick()) {
                const pX = Game.getInput().pageX;
                const pY = Game.getInput().pageY;
                if (Game.getScreen().pagePointContainScreen(pX, pY)) {
                    const pos = Game.getScreen().pagePointToScreenPoint(pX, pY);
                    const xx = pos[0];
                    const yy = pos[1];
                    if (check(xx, yy, elapsed, ms)) {
                        end(xx, yy, elapsed, ms);
                        return;
                    }
                }
            }
            update(elapsed, ms);
        };
    }
    Scene.waitClick = waitClick;
})(Scene || (Scene = {}));
var Scene;
(function (Scene) {
    function* mapview(data) {
        this.draw = () => {
            Game.getScreen().save();
            Game.getScreen().fillStyle = "rgb(0,0,0)";
            Game.getScreen().fillRect(0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            const offx = ~~((Game.getScreen().offscreenWidth - data.map.width * 5) / 2);
            const offy = ~~((Game.getScreen().offscreenHeight - data.map.height * 5) / 2);
            for (let y = 0; y < data.map.height; y++) {
                for (let x = 0; x < data.map.width; x++) {
                    const chip = data.map.layer[0].chips.value(x, y);
                    let color = "rgb(52,12,0)";
                    switch (chip) {
                        case 1:
                            color = "rgb(179,116,39)";
                            break;
                        case 10:
                            color = "rgb(255,0,0)";
                            break;
                    }
                    Game.getScreen().fillStyle = color;
                    Game.getScreen().fillRect(offx + x * 5, offy + y * 5, 5, 5);
                    let light = 1 - data.map.visibled.value(x, y) / 100;
                    if (light > 1) {
                        light = 1;
                    }
                    else if (light < 0) {
                        light = 0;
                    }
                    Game.getScreen().fillStyle = `rgba(0,0,0,${light})`;
                    Game.getScreen().fillRect(offx + x * 5, offy + y * 5, 5, 5);
                }
            }
            Game.getScreen().fillStyle = "rgb(0,255,0)";
            Game.getScreen().fillRect(offx + data.player.x * 5, offy + data.player.y * 5, 5, 5);
            Game.getScreen().restore();
        };
        yield Scene.waitClick({ end: () => this.next() });
        Game.getSceneManager().pop();
    }
    Scene.mapview = mapview;
})(Scene || (Scene = {}));
var Scene;
(function (Scene) {
    function* title() {
        let showClickOrTap = false;
        const fade = new Scene.Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
        this.draw = () => {
            const w = Game.getScreen().offscreenWidth;
            const h = Game.getScreen().offscreenHeight;
            Game.getScreen().save();
            Game.getScreen().fillStyle = "rgb(255,255,255)";
            Game.getScreen().fillRect(0, 0, w, h);
            Game.getScreen().drawImage(Game.getScreen().texture("title"), 0, 0, 192, 72, w / 2 - 192 / 2, 50, 192, 72);
            if (showClickOrTap) {
                Game.getScreen().drawImage(Game.getScreen().texture("title"), 0, 72, 168, 24, w / 2 - 168 / 2, h - 50, 168, 24);
            }
            fade.draw();
            Game.getScreen().restore();
        };
        yield Scene.waitClick({
            update: (e, ms) => { showClickOrTap = (~~(ms / 500) % 2) === 0; },
            check: () => true,
            end: () => {
                Game.getSound().reqPlayChannel("title");
                this.next();
            },
        });
        yield Scene.waitTimeout({
            timeout: 1000,
            update: (e, ms) => { showClickOrTap = (~~(ms / 50) % 2) === 0; },
            end: () => this.next(),
        });
        yield Scene.waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeOut(); },
            update: (e, ms) => { fade.update(e); showClickOrTap = (~~(ms / 50) % 2) === 0; },
            end: () => {
                Game.getSceneManager().push(Scene.classroom, null);
                this.next();
            },
        });
    }
    Scene.title = title;
})(Scene || (Scene = {}));
const charDic = {
    " ": [0, 0],
    "!": [5, 0],
    "\"": [10, 0],
    "#": [15, 0],
    "$": [20, 0],
    "%": [25, 0],
    "&": [30, 0],
    "'": [35, 0],
    "(": [40, 0],
    ")": [45, 0],
    "*": [50, 0],
    "+": [55, 0],
    ",": [60, 0],
    "-": [65, 0],
    ".": [70, 0],
    "/": [75, 0],
    "0": [0, 7],
    "1": [5, 7],
    "2": [10, 7],
    "3": [15, 7],
    "4": [20, 7],
    "5": [25, 7],
    "6": [30, 7],
    "7": [35, 7],
    "8": [40, 7],
    "9": [45, 7],
    ":": [50, 7],
    ";": [55, 7],
    "<": [60, 7],
    "=": [65, 7],
    ">": [70, 7],
    "?": [75, 7],
    "@": [0, 14],
    "A": [5, 14],
    "B": [10, 14],
    "C": [15, 14],
    "D": [20, 14],
    "E": [25, 14],
    "F": [30, 14],
    "G": [35, 14],
    "H": [40, 14],
    "I": [45, 14],
    "J": [50, 14],
    "K": [55, 14],
    "L": [60, 14],
    "M": [65, 14],
    "N": [70, 14],
    "O": [75, 14],
    "P": [0, 21],
    "Q": [5, 21],
    "R": [10, 21],
    "S": [15, 21],
    "T": [20, 21],
    "U": [25, 21],
    "V": [30, 21],
    "W": [35, 21],
    "X": [40, 21],
    "Y": [45, 21],
    "Z": [50, 21],
    "[": [55, 21],
    "\\": [60, 21],
    "]": [65, 21],
    "^": [70, 21],
    "_": [75, 21],
    "`": [0, 28],
    "a": [5, 28],
    "b": [10, 28],
    "c": [15, 28],
    "d": [20, 28],
    "e": [25, 28],
    "f": [30, 28],
    "g": [35, 28],
    "h": [40, 28],
    "i": [45, 28],
    "j": [50, 28],
    "k": [55, 28],
    "l": [60, 28],
    "m": [65, 28],
    "n": [70, 28],
    "o": [75, 28],
    "p": [0, 35],
    "q": [5, 35],
    "r": [10, 35],
    "s": [15, 35],
    "t": [20, 35],
    "u": [25, 35],
    "v": [30, 35],
    "w": [35, 35],
    "x": [40, 35],
    "y": [45, 35],
    "z": [50, 35],
    "{": [55, 35],
    "|": [60, 35],
    "}": [65, 35],
    "~": [70, 35]
};
function draw7pxFont(str, x, y) {
    const fontWidth = 5;
    const fontHeight = 7;
    let sx = x;
    let sy = y;
    for (let i = 0; i < str.length; i++) {
        const ch = str[i];
        if (ch === "\n") {
            sy += fontHeight;
            sx = x;
            continue;
        }
        const [fx, fy] = charDic[str[i]];
        Game.getScreen().drawImage(Game.getScreen().texture("font7px"), fx, fy, fontWidth, fontHeight, sx, sy, fontWidth, fontHeight);
        sx += fontWidth - 1;
    }
}
function createShowDamageSprite(start, damage, getpos) {
    let elapse = 0;
    const fontWidth = 5;
    const fontHeight = 7;
    return {
        update: (delta, ms) => {
            elapse = ms - start;
            return (elapse > 500);
        },
        draw: (camera) => {
            const { x: sx, y: sy } = getpos();
            const xx = sx - camera.left;
            const yy = sy - camera.top;
            const len = damage.length;
            const offx = -(len) * (fontWidth - 1) / 2;
            const offy = 0;
            for (let i = 0; i < damage.length; i++) {
                const rad = Math.min(elapse - i * 20, 200);
                if (rad < 0) {
                    continue;
                }
                const dy = Math.sin(rad * Math.PI / 200) * -7;
                if (0 <= xx + (i + 1) * fontWidth && xx + (i + 0) * fontWidth < Game.getScreen().offscreenWidth &&
                    0 <= yy + (1 * fontHeight) && yy + (0 * fontHeight) < Game.getScreen().offscreenHeight) {
                    const [fx, fy] = charDic[damage[i]];
                    Game.getScreen().drawImage(Game.getScreen().texture("font7px"), fx, fy, fontWidth, fontHeight, (xx + (i + 0) * (fontWidth - 1)) + offx, (yy + (0) * fontHeight) + offy + dy, fontWidth, fontHeight);
                }
            }
        },
    };
}
window.onload = () => {
    Game.create({
        title: "TSJQ",
        video: {
            id: "glcanvas",
            offscreenWidth: 252,
            offscreenHeight: 252,
            scaleX: 2,
            scaleY: 2,
        }
    }).then(() => {
        Game.getSceneManager().push(Scene.boot, null);
        Game.getTimer().on((delta, now, id) => {
            Game.getInput().endCapture();
            Game.getSceneManager().update(delta, now);
            Game.getInput().startCapture();
            Game.getSound().playChannel();
            Game.getScreen().begin();
            Game.getSceneManager().draw();
            Game.getScreen().end();
        });
        Game.getTimer().start();
    });
};
