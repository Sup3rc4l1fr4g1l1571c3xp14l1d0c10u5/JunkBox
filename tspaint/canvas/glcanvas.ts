"use strict";

namespace GUI {

    export class Control {
        public get left(): number {
            return this._left;
        }
        public set left(value: number) {
            this._left = value;
        }
        public get top(): number {
            return this._top;
        }
        public set top(value: number) {
            this._top = value;
        }
        public get width(): number {
            return this._width;
        }
        public set width(value: number) {
            this._width = value;
        }
        public get height(): number {
            return this._height;
        }
        public set height(value: number) {
            this._height = value;
        }

        private _left: number;
        private _top: number;
        private _width: number;
        private _height: number;

        constructor(param: { left: number; top: number; width: number; height: number; }) {
            this._left = param.left;
            this._top = param.top;
            this._width = param.width;
            this._height = param.height;
        }
    }

    export class Window extends Control {
        static DefaultBackgroundColor: string = "rgb(224,224,224)";
        static DefaultActiveTitleBarColor: string = "rgb(192,192,255)";
        static DefaultInactiveTitleBarColor: string = "rgb(192,192,192)";
        static DefaultTitleBarTextColor: string = "rgb(0,0,0)";
        static TitleBatHeight: number = 12;

        private get caption(): string {
            return this._caption;
        }
        private set caption(value: string) {
            this._caption = value;
        }
        private get backgroundColor(): string {
            return this._backgroundColor;
        }
        private set backgroundColor(value: string) {
            this._backgroundColor = value;
        }
        private get activeTitleBarColor(): string {
            return this._activeTitleBarColor;
        }
        private set activeTitleBarColor(value: string) {
            this._activeTitleBarColor = value;
        }
        private get inactiveTitleBarColor(): string {
            return this._inactiveTitleBarColor;
        }
        private set inactiveTitleBarColor(value: string) {
            this._inactiveTitleBarColor = value;
        }
        private get titleBarTextColor(): string {
            return this._titleBarTextColor;
        }
        private set titleBarTextColor(value: string) {
            this._titleBarTextColor = value;
        }
        private _caption: string;
        private _backgroundColor: string;
        private _activeTitleBarColor: string;
        private _inactiveTitleBarColor: string;
        private _titleBarTextColor: string;

        constructor(param: { caption: string, left: number; top: number; width: number; height: number; }) {
            super(param);
            this._backgroundColor = Window.DefaultBackgroundColor;
            this._activeTitleBarColor = Window.DefaultActiveTitleBarColor;
            this._inactiveTitleBarColor = Window.DefaultInactiveTitleBarColor;
            this._titleBarTextColor = Window.DefaultTitleBarTextColor;
            this._caption = param.caption;
        }
        public draw(context: CanvasRenderingContext2D) {
            context.save();
            context.translate(this.left, this.top);
            if (activeWindow == this) {
                context.fillStyle = this.activeTitleBarColor;
            } else {
                context.fillStyle = this.inactiveTitleBarColor;
            }
            context.fillRect(0, 0, this.width, Window.TitleBatHeight);
            context.textBaseline = "top";
            context.fillStyle = this.titleBarTextColor;
            context.fillText(this._caption, 0, 0, this.width);
            context.fillStyle = this.backgroundColor;
            context.fillRect(0, Window.TitleBatHeight, this.width, this.height - Window.TitleBatHeight);

            context.restore();
        }
    }

    const windows: Window[] = [];
    let activeWindow: Window;

    export function createWindow(param: { caption: string, left: number; top: number; width: number; height: number; }) {
        const window = new Window(param);
        windows.push(window);
        activeWindow = window;
        return window;
    }

    export enum TouchEventType {
        Up = 0,
        Down = 1,
        Move = 2,
    }
    enum TouchStatus {
        StillUp = 0,
        Down = 1,
        StillDown = 2,
        Move = 3,
        StillMove = 4,
        Up = 5,
    }
    let touchX: number = 0;
    let touchY: number = 0;
    let touchState: TouchStatus = TouchStatus.StillUp;
    export function setTouch(x: number, y: number, eventType: TouchEventType) {
        switch (touchState) {
            case TouchStatus.StillUp:
                switch (eventType) {
                    case TouchEventType.Up: 
                        touchState = TouchStatus.StillUp; 
                        return;
                    case TouchEventType.Down: 
                        touchX = x;
                        touchY = y;
                        touchState = TouchStatus.Down; 
                        return;
                    case TouchEventType.Move: 
                        return;
                    default: 
                        return;
                }
            case TouchStatus.Down:
                        touchX = x;
                        touchY = y;
                switch (eventType) {
                    case TouchEventType.Up: touchState = TouchStatus.Up; return;
                    case TouchEventType.Down: touchState = TouchStatus.StillDown; return;
                    case TouchEventType.Move: touchState = TouchStatus.Move; return;
                    default: return;
                }
            case TouchStatus.StillDown:
                touchX = x;
                touchY = y;
                switch (eventType) {
                    case TouchEventType.Up: touchState = TouchStatus.Down; return;
                    case TouchEventType.Down: touchState = TouchStatus.StillDown; return;
                    case TouchEventType.Move: touchState = TouchStatus.Move; return;
                    default: return;
                }
            case TouchStatus.Move:
                touchX = x;
                touchY = y;
                switch (eventType) {
                    case TouchEventType.Up: touchState = TouchStatus.Up; return;
                    case TouchEventType.Down: touchState = TouchStatus.StillMove; return;
                    case TouchEventType.Move: touchState = TouchStatus.StillMove; return;
                    default: return;
                }
            case TouchStatus.StillMove:
                touchX = x;
                touchY = y;
                switch (eventType) {
                    case TouchEventType.Up: touchState = TouchStatus.Up; return;
                    case TouchEventType.Down: touchState = TouchStatus.StillMove; return;
                    case TouchEventType.Move: touchState = TouchStatus.StillMove; return;
                    default: return;
                }
            case TouchStatus.Up:
                switch (eventType) {
                    case TouchEventType.Up: touchState = TouchStatus.StillUp; return;
                    case TouchEventType.Down: 
                touchX = x;
                touchY = y;
                        touchState = TouchStatus.Down; 
                        return;
                    case TouchEventType.Move: 
                touchX = x;
                touchY = y;
                        touchState = TouchStatus.StillUp; 
                        return;
                    default: return;
                }
        }
    }

    function windowHitTest(x: number, y: number) {
        for (let i = windows.length - 1; i >= 0; i--) {
            const window = windows[i];
            const dx = x - window.left;
            const dy = y - window.top;
            if (0 <= dx && dx < window.width && 0 <= dy && dy <= window.height) {
                console.log(dx,dy);
                if (activeWindow != window) {
                    activeWindow = window;
                    windows.splice(i, 1);
                    windows.push(window);
                    return true;
                } else {
                    return false;;
                }
            }
        }
        return false;
    }

    export function update(context: CanvasRenderingContext2D) {
        if (touchState == TouchStatus.Down) {
            windowHitTest(touchX, touchY);
        }
        for (let i = windows.length - 1; i >= 0; i--) {
            windows[i].draw(context);
        }
    }

}

function main() {
    const image = new Image();
    image.src = "leaves.jpg";
    image.onload = () => { render(image); }
}
function render(image: HTMLImageElement) {
    const canvas = document.getElementById("canvas") as HTMLCanvasElement;
    const ctx = canvas.getContext('2d') as CanvasRenderingContext2D;

    if (!ctx) {
        console.error("context does not exist");
    }

    const win1 = GUI.createWindow({ caption: "foo", left: 100, top: 50, width: 150, height: 80 });
    const win2 = GUI.createWindow({ caption: "bar", left: 300, top: 100, width: 150, height: 80 });

    let mousex = 0;
    let mousey = 0;
    document.addEventListener("mousedown", (e) => GUI.setTouch(e.pageX, e.pageY, GUI.TouchEventType.Down));
    document.addEventListener("mousemove", (e) => GUI.setTouch(e.pageX, e.pageY, GUI.TouchEventType.Move));
    document.addEventListener("mouseup", (e) => GUI.setTouch(e.pageX, e.pageY, GUI.TouchEventType.Up));

    const update = () => {
        if (canvas.width != canvas.clientWidth || canvas.height != canvas.clientHeight) {
            canvas.width = canvas.clientWidth;
            canvas.height = canvas.clientHeight;
        }
        GUI.update(ctx);
        window.requestAnimationFrame(update);
    }
    update();

}
window.onload = () => {
    main();
};
