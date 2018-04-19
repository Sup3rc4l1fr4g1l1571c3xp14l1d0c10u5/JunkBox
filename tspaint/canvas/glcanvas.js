"use strict";
var GUI;
(function (GUI) {
    class Control {
        constructor(param) {
            this._left = param.left;
            this._top = param.top;
            this._width = param.width;
            this._height = param.height;
        }
        get left() {
            return this._left;
        }
        set left(value) {
            this._left = value;
        }
        get top() {
            return this._top;
        }
        set top(value) {
            this._top = value;
        }
        get width() {
            return this._width;
        }
        set width(value) {
            this._width = value;
        }
        get height() {
            return this._height;
        }
        set height(value) {
            this._height = value;
        }
    }
    GUI.Control = Control;
    class Window extends Control {
        constructor(param) {
            super(param);
            this._backgroundColor = Window.DefaultBackgroundColor;
            this._activeTitleBarColor = Window.DefaultActiveTitleBarColor;
            this._inactiveTitleBarColor = Window.DefaultInactiveTitleBarColor;
            this._titleBarTextColor = Window.DefaultTitleBarTextColor;
            this._caption = param.caption;
        }
        get caption() {
            return this._caption;
        }
        set caption(value) {
            this._caption = value;
        }
        get backgroundColor() {
            return this._backgroundColor;
        }
        set backgroundColor(value) {
            this._backgroundColor = value;
        }
        get activeTitleBarColor() {
            return this._activeTitleBarColor;
        }
        set activeTitleBarColor(value) {
            this._activeTitleBarColor = value;
        }
        get inactiveTitleBarColor() {
            return this._inactiveTitleBarColor;
        }
        set inactiveTitleBarColor(value) {
            this._inactiveTitleBarColor = value;
        }
        get titleBarTextColor() {
            return this._titleBarTextColor;
        }
        set titleBarTextColor(value) {
            this._titleBarTextColor = value;
        }
        draw(context) {
            context.save();
            context.translate(this.left, this.top);
            if (activeWindow == this) {
                context.fillStyle = this.activeTitleBarColor;
            }
            else {
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
    Window.DefaultBackgroundColor = "rgb(224,224,224)";
    Window.DefaultActiveTitleBarColor = "rgb(192,192,255)";
    Window.DefaultInactiveTitleBarColor = "rgb(192,192,192)";
    Window.DefaultTitleBarTextColor = "rgb(0,0,0)";
    Window.TitleBatHeight = 12;
    GUI.Window = Window;
    const windows = [];
    let activeWindow;
    function createWindow(param) {
        const window = new Window(param);
        windows.push(window);
        activeWindow = window;
        return window;
    }
    GUI.createWindow = createWindow;
    let TouchEventType;
    (function (TouchEventType) {
        TouchEventType[TouchEventType["Up"] = 0] = "Up";
        TouchEventType[TouchEventType["Down"] = 1] = "Down";
        TouchEventType[TouchEventType["Move"] = 2] = "Move";
    })(TouchEventType = GUI.TouchEventType || (GUI.TouchEventType = {}));
    let TouchStatus;
    (function (TouchStatus) {
        TouchStatus[TouchStatus["StillUp"] = 0] = "StillUp";
        TouchStatus[TouchStatus["Down"] = 1] = "Down";
        TouchStatus[TouchStatus["StillDown"] = 2] = "StillDown";
        TouchStatus[TouchStatus["Move"] = 3] = "Move";
        TouchStatus[TouchStatus["StillMove"] = 4] = "StillMove";
        TouchStatus[TouchStatus["Up"] = 5] = "Up";
    })(TouchStatus || (TouchStatus = {}));
    let touchX = 0;
    let touchY = 0;
    let touchState = TouchStatus.StillUp;
    function setTouch(x, y, eventType) {
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
                    case TouchEventType.Up:
                        touchState = TouchStatus.Up;
                        return;
                    case TouchEventType.Down:
                        touchState = TouchStatus.StillDown;
                        return;
                    case TouchEventType.Move:
                        touchState = TouchStatus.Move;
                        return;
                    default: return;
                }
            case TouchStatus.StillDown:
                touchX = x;
                touchY = y;
                switch (eventType) {
                    case TouchEventType.Up:
                        touchState = TouchStatus.Down;
                        return;
                    case TouchEventType.Down:
                        touchState = TouchStatus.StillDown;
                        return;
                    case TouchEventType.Move:
                        touchState = TouchStatus.Move;
                        return;
                    default: return;
                }
            case TouchStatus.Move:
                touchX = x;
                touchY = y;
                switch (eventType) {
                    case TouchEventType.Up:
                        touchState = TouchStatus.Up;
                        return;
                    case TouchEventType.Down:
                        touchState = TouchStatus.StillMove;
                        return;
                    case TouchEventType.Move:
                        touchState = TouchStatus.StillMove;
                        return;
                    default: return;
                }
            case TouchStatus.StillMove:
                touchX = x;
                touchY = y;
                switch (eventType) {
                    case TouchEventType.Up:
                        touchState = TouchStatus.Up;
                        return;
                    case TouchEventType.Down:
                        touchState = TouchStatus.StillMove;
                        return;
                    case TouchEventType.Move:
                        touchState = TouchStatus.StillMove;
                        return;
                    default: return;
                }
            case TouchStatus.Up:
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
                        touchX = x;
                        touchY = y;
                        touchState = TouchStatus.StillUp;
                        return;
                    default: return;
                }
        }
    }
    GUI.setTouch = setTouch;
    function windowHitTest(x, y) {
        for (let i = windows.length - 1; i >= 0; i--) {
            const window = windows[i];
            const dx = x - window.left;
            const dy = y - window.top;
            if (0 <= dx && dx < window.width && 0 <= dy && dy <= window.height) {
                console.log(dx, dy);
                if (activeWindow != window) {
                    activeWindow = window;
                    windows.splice(i, 1);
                    windows.push(window);
                    return true;
                }
                else {
                    return false;
                    ;
                }
            }
        }
        return false;
    }
    function update(context) {
        if (touchState == TouchStatus.Down) {
            windowHitTest(touchX, touchY);
        }
        for (let i = windows.length - 1; i >= 0; i--) {
            windows[i].draw(context);
        }
    }
    GUI.update = update;
})(GUI || (GUI = {}));
function main() {
    const image = new Image();
    image.src = "leaves.jpg";
    image.onload = () => { render(image); };
}
function render(image) {
    const canvas = document.getElementById("canvas");
    const ctx = canvas.getContext('2d');
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
    };
    update();
}
window.onload = () => {
    main();
};
//# sourceMappingURL=glcanvas.js.map