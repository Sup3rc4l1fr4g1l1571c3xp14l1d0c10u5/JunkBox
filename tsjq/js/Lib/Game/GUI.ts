/// <reference path="eventdispatcher.ts" />
namespace Game.GUI {
    export interface UI {
        left: number;
        top: number;
        width: number;
        height: number;
        draw: () => void;
        regist: (dispatcher : UIDispatcher) => void;
        unregist: (dispatcher : UIDispatcher) => void;
    }

    export interface ClickableUI {
        click : (x:number, y:number) => void;
    }

    export interface SwipableUI {
        swipe : (dx:number, dy:number,x:number, y:number) => void;
    }

    export function isHit(ui: UI, x: number, y: number): boolean {
        const dx = x - ui.left;
        const dy = y - ui.top;
        return (0 <= dx && dx < ui.width) && (0 <= dy && dy < ui.height)
    }

    type EventHandler = (...args : any[]) => void;
    type CancelHandler = () => void;

    export class UIDispatcher extends Dispatcher.EventDispatcher {
        private uiTable : Map<UI, Map<string, EventHandler[]>>;

        constructor() {
            super();
            this.uiTable = new Map<UI, Map<string, EventHandler[]>>();
        }

        public add(ui: UI) : void  {
            if (this.uiTable.has(ui)) {
                return;
            }
            this.uiTable.set(ui, new Map<string, EventHandler[]>());
            ui.regist(this);
        }

        public remove(ui: UI) : void  {
            if (!this.uiTable.has(ui)) {
                return;
            }
            ui.unregist(this);
            const eventTable : Map<string, EventHandler[]> = this.uiTable.get(ui);
            this.uiTable.set(ui,null);
            eventTable.forEach((values,key) => {
                values.forEach((value) => this.off(key,value));
            });
            this.uiTable.delete(ui);
        }

        private registUiEvent(ui: UI, event:string, handler : EventHandler) : void  {
            if (!this.uiTable.has(ui)) {
                return;
            }
            const eventTable : Map<string, EventHandler[]> = this.uiTable.get(ui);
            if (!eventTable.has(event)) {
                eventTable.set(event, []);
            }
            const events = eventTable.get(event);
            events.push(handler);
            this.on(event, handler);
        }
        private unregistUiEvent(ui: UI, event: string,  handler : EventHandler) : void  {
            if (!this.uiTable.has(ui)) {
                return;
            }
            const eventTable : Map<string, EventHandler[]> = this.uiTable.get(ui);
            if (!eventTable.has(event)) {
                return;
            }
            const events = eventTable.get(event);
            const index = events.indexOf(handler);
            if (index != -1) {
                events.splice(index,1);
            }
            this.off(event, handler);
        }

        public draw() : void {
            this.uiTable.forEach((value, key) => key.draw());
        }

        // UIに対するクリック/タップ操作を捕捉
        public onClick(ui: UI, handler: (x: number, y: number) => void): CancelHandler {
            const hookHandler = (x: number, y: number) => {
                if (!Game.getScreen().pagePointContainScreen(x, y)) {
                    return;
                }
                const [cx, cy] = Game.getScreen().pagePointToScreenPoint(x, y);
                if (!isHit(ui, cx, cy)) {
                    return;
                }

                let dx: number = 0;
                let dy: number = 0;
                const onPointerMoveHandler = (x: number, y: number) => {
                    const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                    dx += Math.abs(_x - cx);
                    dy += Math.abs(_y - cy);
                };
                const onPointerUpHandler = (x: number, y: number) => {
                    this.off("pointermove", onPointerMoveHandler);
                    this.off("pointerup", onPointerUpHandler);
                    if (dx + dy < 5) {
                        const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                        handler(_x - ui.left, _y - ui.top);
                    }
                };
                this.on("pointermove", onPointerMoveHandler);
                this.on("pointerup", onPointerUpHandler);

            };
            this.registUiEvent(ui, "pointerdown", hookHandler);
            return () => this.unregistUiEvent(ui, "pointerdown", hookHandler);
        }

        //UI外のタップ/クリック操作を捕捉
        public onNcClick(ui: UI, handler: (x: number, y: number) => void): CancelHandler {
            const hookHandler = (x: number, y: number) => {
                if (!Game.getScreen().pagePointContainScreen(x, y)) {
                    return;
                }
                const [cx, cy] = Game.getScreen().pagePointToScreenPoint(x, y);
                if (isHit(ui, cx, cy)) {
                    return;
                }

                let dx: number = 0;
                let dy: number = 0;
                const onPointerMoveHandler = (x: number, y: number) => {
                    const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                    dx += Math.abs(_x - cx);
                    dy += Math.abs(_y - cy);
                };
                const onPointerUpHandler = (x: number, y: number) => {
                    this.off("pointermove", onPointerMoveHandler);
                    this.off("pointerup", onPointerUpHandler);
                    if (dx + dy < 5) {
                        const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                        handler(_x - ui.left, _y - ui.top);
                    }
                };
                this.on("pointermove", onPointerMoveHandler);
                this.on("pointerup", onPointerUpHandler);

            };
            this.registUiEvent(ui, "pointerdown", hookHandler);
            return () => this.unregistUiEvent(ui, "pointerdown", hookHandler);
        }

        // UIに対するスワイプ操作を捕捉
        public onSwipe(ui: UI, handler: (dx: number, dy: number, x?:number,y?:number) => void): CancelHandler {

            const hookHandler = (x: number, y: number) => {
                if (!Game.getScreen().pagePointContainScreen(x, y)) {
                    return;
                }
                let [cx, cy] = Game.getScreen().pagePointToScreenPoint(x, y);
                if (!isHit(ui, cx, cy)) {
                    return;
                }

                const onPointerMoveHandler = (x: number, y: number) => {
                    const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                    let dx = (~~_x - ~~cx);
                    let dy = (~~_y - ~~cy);
                    cx = _x;
                    cy = _y;
                    handler(dx, dy, _x-ui.left, _y-ui.top);
                };
                const onPointerUpHandler = (x: number, y: number) => {
                    this.off("pointermove", onPointerMoveHandler);
                    this.off("pointerup", onPointerUpHandler);
                };
                this.on("pointermove", onPointerMoveHandler);
                this.on("pointerup", onPointerUpHandler);

                handler(0, 0, cx-ui.left, cy-ui.top);
            };
            this.registUiEvent(ui, "pointerdown", hookHandler);
            return () => this.unregistUiEvent(ui, "pointerdown", hookHandler);
        }


    }

    export class TextBox implements UI {
        public text: string;
        public edgeColor: string;
        public color: string;
        public font: string;
        public fontColor: string;
        public textAlign: string;
        public textBaseline: string;
        constructor(public left: number, public top: number, public width: number, public height: number,
            {
                text = "",
                edgeColor = `rgb(128,128,128)`,
                color = `rgb(255,255,255)`,
                font = undefined,
                fontColor = `rgb(0,0,0)`,
                textAlign = "left",
                textBaseline = "top",
            }: {
                    text?: string;
                    edgeColor?: string;
                    color?: string;
                    font?: string;
                    fontColor?: string;
                    textAlign?: string;
                    textBaseline?: string;
                }) {
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
            this.text.split(/\n/).forEach((x: string, i: number) => {
                Game.getScreen().fillText(x, this.left + 8, this.top + i * (10 + 1) + 8);
            });

        }
        regist(dispatcher : UIDispatcher) {}
        unregist(dispatcher : UIDispatcher) {}
    }

    export class Button implements UI, ClickableUI {
        public text: string | (() => string);
        public edgeColor: string;
        public color: string;
        public font: string;
        public fontColor: string;
        public textAlign: string;
        public textBaseline: string;
        public click : (x:number,y:number) => void;

        constructor(public left: number, public top: number, public width: number, public height: number,
            {
                text = "button",
                edgeColor = `rgb(128,128,128)`,
                color = `rgb(255,255,255)`,
                font = undefined,
                fontColor = `rgb(0,0,0)`,
                textAlign = "left",
                textBaseline = "top",
            }: {
                    text?: string | (() => string);
                    edgeColor?: string;
                    color?: string;
                    font?: string;
                    fontColor?: string;
                    textAlign?: string;
                    textBaseline?: string;
                }) {
            this.text = text;
            this.edgeColor = edgeColor;
            this.color = color;
            this.font = font;
            this.fontColor = fontColor;
            this.textAlign = textAlign;
            this.textBaseline = textBaseline;
            this.click = () => {};
        }
        draw() {
            Game.getScreen().fillStyle = this.color;
            Game.getScreen().fillRect(this.left - 0.5, this.top - 0.5, this.width, this.height);
            Game.getScreen().strokeStyle = this.edgeColor;
            Game.getScreen().lineWidth = 1;
            Game.getScreen().strokeRect(this.left - 0.5, this.top - 0.5, this.width, this.height);
            Game.getScreen().font = this.font;
            Game.getScreen().fillStyle = this.fontColor;
            const text = (this.text instanceof Function) ? (this.text as Function).call(this) : this.text
            const metrics = Game.getScreen().measureText(text);
            Game.getScreen().textAlign = this.textAlign;
            Game.getScreen().textBaseline = this.textBaseline;
            text.split(/\n/).forEach((x: string, i: number) => {
                Game.getScreen().fillText(x, this.left + 2, this.top + i * (10 + 1) + 2);
            });
        }
        regist(dispatcher : UIDispatcher) {
            const cancelHandler = dispatcher.onClick(this, (...args:any[]) => this.click.apply(this,args));
            this.unregist = (d) => cancelHandler();

        }
        unregist(dispatcher : UIDispatcher) {}
    }

    export class ListBox implements UI, ClickableUI {
        public lineHeight: number;
        public scrollValue: number;
        public click: (x: number, y: number) => void;

        public drawItem: (left: number, top: number, width: number, height: number, item: number) => void;
        public getItemCount: () => number;
        constructor(public left: number, public top: number, public width: number, public height: number,
            {
                lineHeight = 12,
                drawItem = () => { },
                getItemCount = () => 0,
            }: {
                    lineHeight?: number
                    drawItem?: (left: number, top: number, width: number, height: number, item: number) => void,
                    getItemCount?: () => number,
                }) {
            this.lineHeight = lineHeight;
            this.drawItem = drawItem;
            this.getItemCount = getItemCount;
            this.scrollValue = 0;
            this.click = () => {}
        }
        update(): void {
            var contentHeight = this.getItemCount() * this.lineHeight;

            if (this.height >= contentHeight) {
                this.scrollValue = 0;
            } else if (this.scrollValue < 0) {
                this.scrollValue = 0;
            } else if (this.scrollValue > (contentHeight - this.height)) {
                this.scrollValue = contentHeight - this.height;
            }
        }
        draw() {
            let sy = -(~~this.scrollValue % this.lineHeight);
            let index = ~~((~~this.scrollValue) / this.lineHeight);
            let itemCount = this.getItemCount();
            let drawResionHeight = this.height - sy;
            for (; ;) {
                if (sy >= this.height) { break; }
                if (index >= itemCount) { break; }
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
        getItemIndexByPosition(x: number, y: number) {
            if (x < 0 || this.width <= x || y < 0 || this.height <= y) {
                return -1;
            }
            const index = ~~((y + this.scrollValue) / this.lineHeight);
            if (index < 0 || index >= this.getItemCount()) {
                return -1;
            } else {
                return index;
            }
        }
        regist(dispatcher : UIDispatcher) {
            const cancelHandlers = [
                dispatcher.onSwipe(this, (deltaX: number, deltaY: number) => {
                    this.scrollValue -= deltaY;
                    this.update();
                }),
                dispatcher.onClick(this, (...args:any[]) => this.click.apply(this,args))
            ];
            this.unregist = (d) => cancelHandlers.forEach(x => x());

        }
        unregist(dispatcher : UIDispatcher) {}
    }

    export class HorizontalSlider implements UI {
        public sliderWidth: number;
        public edgeColor: string;
        public color: string;
        public bgColor: string;
        public font: string;
        public fontColor: string;

        public value: number;
        public minValue: number;
        public maxValue: number;

        constructor(public left: number, public top: number, public width: number, public height: number,
            {
                sliderWidth = 5,
                edgeColor = `rgb(128,128,128)`,
                color = `rgb(255,255,255)`,
                bgColor = `rgb(192,192,192)`,
                font = undefined,
                fontColor = `rgb(0,0,0)`,
                minValue = 0,
                maxValue = 0,

            }: {
                sliderWidth?: number;
                updownButtonWidth?: number;
                edgeColor?: string;
                color?: string;
                bgColor?: string;
                font?: string;
                fontColor?: string;
                    minValue?: number;
                    maxValue?: number;
                }) {
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
            Game.getScreen().fillRect(
                this.left-0.5,
                this.top-0.5,
                this.width,
                this.height
            );
            Game.getScreen().fillStyle = this.color;
            Game.getScreen().strokeStyle = this.edgeColor;
            Game.getScreen().fillRect(
                this.left + ~~(lineWidth * (this.value - this.minValue) / (this.maxValue - this.minValue))-0.5,
                this.top-0.5,
                this.sliderWidth,
                this.height
            );
            Game.getScreen().strokeRect(
                this.left + ~~(lineWidth * (this.value - this.minValue) / (this.maxValue - this.minValue))-0.5,
                this.top-0.5,
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
        regist(dispatcher : UIDispatcher) {
            var cancelHandler = dispatcher.onSwipe(this, (dx, dy, x, y) => {
                const rangeSize = this.maxValue - this.minValue;
                if (rangeSize == 0) {
                    this.value = this.minValue;
                } else {
                    if (x <= this.sliderWidth/2) {
                        this.value = this.minValue;
                    } else if (x >= this.width-this.sliderWidth/2) {
                        this.value = this.maxValue;
                    } else {
                        const width = this.width - this.sliderWidth;
                        const xx = x - ~~(this.sliderWidth/2);
                        this.value = Math.trunc((xx * rangeSize) / width) + this.minValue;
                    }
                }
            });
            this.unregist = (d) => cancelHandler();

        }
        unregist(dispatcher : UIDispatcher) {}
    }


}
