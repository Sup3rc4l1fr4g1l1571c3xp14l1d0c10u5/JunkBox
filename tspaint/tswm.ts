/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/3.1/lib.dom.d.ts" />
/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/3.1/lib.es2016.d.ts" />

module tswm {
    class Component {

        public get screenX(): number { return this._x + (this.parent == null ? 0 : this.parent.screenX); }
        public get screenY(): number { return this._y + (this.parent == null ? 0 : this.parent.screenY); }

        private _x: number;
        public get x(): number { return this._x; }
        public set x(value: number) { this._x = value; }

        private _y: number;
        public get y(): number { return this._y; }
        public set y(value: number) { this._y = value; }

        private _width: number;
        public get width(): number { return this._width; }
        public set width(value: number) { this._width = value; }

        private _height: number;
        public get height(): number { return this._height; }
        public set height(value: number) { this._height = value; }

        private _visible: boolean;
        public get visible(): boolean { return this._visible; }
        public set visible(value: boolean) { value ? this.show() : this.hide(); }

        private _parent: Component;
        public get parent(): Component { return this._parent; }

        private _name: string;
        public get name(): string { return this._name; }

        public childs: Component[];

        private _focus: boolean;
        public get focus(): boolean { return this._focus; }

        constructor(name: string, x: number, y: number, width: number, height: number) {
            this._name = name;
            this.childs = [];
            this._visible = false;
            this._x = x;
            this._y = y;
            this._width = width;
            this._height = height;
            this._focus = false;
        }

        public addChild(x: Component): void {
            this.childs.unshift(x);
            x._parent = this;
        }

        private _moveChildToTop(child: Component): void {
            const idx = this.childs.indexOf(child);
            if (idx != -1) {
                this.childs.splice(idx, 1);
                this.childs.unshift(child);
            }
        }

        public moveTop(): void {
            if (this.parent != null) {
                this.parent._moveChildToTop(this);
            }
        }

        public draw(ctx: CanvasRenderingContext2D): void {
            if (this.visible == false) { return; }
            for (let i = this.childs.length - 1; i >= 0; i--) {
                ctx.save();
                ctx.translate(this.x, this.y);
                this.childs[i].draw(ctx);
                ctx.restore();
            }
        }

        public on(msg: string, e: any): boolean {
            if (this.visible == false) { return false; }
            if (this.childs.some(x => x.on(msg, e))) { return true; }
            return this.messageProc(msg, e);
        }

        public messageProc(msg: string, e: any): boolean {
            return false;
        }

        public include(x: number, y: number): boolean {
            return this.visible && this.screenX <= x && x < this.screenX + this.width && this.screenY <= y && y < this.screenY + this.height;
        }

        public includeChilds(x: number, y: number): boolean {
            return this.childs.some(child => child.visible && (child.include(x, y) || child.includeChilds(x, y)));
        }

        public lostFocus(): boolean {
            if (this._focus) {
                this._focus = false;
                console.log("lost focus " + this.name);
                return true;
            }
            return false;
        }

        public getFocus(): boolean {
            if (!this._focus) {
                this._focus = true;
                console.log("get focus " + this.name);
                return true;
            }
            return false;
        }

 
        public show(): void {
            this._visible = true;
        }

        public hide(): void {
            if (this._focus) {
                this.lostFocus();
            }
            this._visible = false;
        }
    }

    class RootWindow extends Component {
        constructor(name: string) {
            super(name, 0, 0, 0, 0);
        }
        public include(x: number, y: number): boolean {
            return true;
        }
    }

    class Window extends Component {

        constructor(name: string, x: number, y: number, width: number, height: number) {
            super(name, x, y, width, height);
        }

        public draw(ctx: CanvasRenderingContext2D): void {
            if (this.visible == false) { return; }
            ctx.save();
            ctx.fillStyle = "rgb(128,255,255)";
            ctx.beginPath();
            ctx.rect(this.x, this.y, this.width, this.height);
            ctx.fill();
            ctx.stroke();
            ctx.restore();
            for (let i = this.childs.length - 1; i >= 0; i--) {
                ctx.save();
                ctx.translate(this.x, this.y);
                this.childs[i].draw(ctx);
                ctx.restore();
            }
        }

        public getFocus(): boolean {
            if (super.getFocus()) {
                this.moveTop();
                return true;
            }
            return false;
        }
    }

    class Button extends Component {
        private isDown: boolean;

        constructor(name: string, x: number, y: number, width: number, height: number) {
            super(name, x, y, width, height);
            this.isDown = false;
        }

        public draw(ctx: CanvasRenderingContext2D): void {
            if (this.visible == false) { return; }
            ctx.save();
            ctx.fillStyle = "rgb(128,128,255)";
            ctx.beginPath();
            ctx.rect(this.x, this.y, this.width, this.height);
            ctx.fill();
            ctx.stroke();
            ctx.restore();
            for (let i = this.childs.length - 1; i >= 0; i--) {
                ctx.save();
                ctx.translate(this.x, this.y);
                this.childs[i].draw(ctx);
                ctx.restore();
            }
        }

        public messageProc(msg: string, e: any): boolean {
            switch (msg) {
                case "mousedown":
                    if (this.include(e.mouseX, e.mouseY)) {
                        if (this.isDown == false) {
                            this.isDown = true;
                        }
                        return true;
                    } else {
                        return false;
                    }
                case "mousemove":
                    if (this.isDown == true) {
                        return true;
                    } else {
                        return false;
                    }
                case "mouseup":
                    if (this.isDown == true) {
                        this.isDown = false;
                        return true;
                    } else {
                        return false;
                    }
                default:
                    return false;
            }
        }
    }

    class Draggable extends Window {
        private dragStart: boolean;
        private mouseX: number;
        private mouseY: number;

        constructor(name: string, x: number, y: number, width: number, height: number) {
            super(name, x, y, width, height);
            this.dragStart = false;
            this.mouseX = Number.NaN;
            this.mouseY = Number.NaN;
        }

        public messageProc(msg: string, e: any): boolean {
            switch (msg) {
                case "mousedown":
                    if (this.include(e.mouseX, e.mouseY)) {
                        if (this.dragStart == false) {
                            this.dragStart = true;
                            this.mouseX = e.mouseX;
                            this.mouseY = e.mouseY;
                        }
                        return true;
                    } else {
                        return false;
                    }
                case "mousemove":
                    if (this.dragStart == true) {
                        this.x += (e.mouseX - this.mouseX);
                        this.y += (e.mouseY - this.mouseY);
                        this.mouseX = e.mouseX;
                        this.mouseY = e.mouseY;
                        return true;
                    } else {
                        return false;
                    }
                case "mouseup":
                    if (this.dragStart == true) {
                        this.dragStart = false;
                        return true;
                    } else {
                        return false;
                    }
                default:
                    return false;
            }
        }
    }

    class DropDown extends Window {
        private expanded: boolean;

        constructor(name: string, x: number, y: number, width: number, height: number) {
            super(name, x, y, width, height);
            this.expanded = false;
        }

        public lostFocus(): boolean {
            if (this.expanded) {
                this.expanded = false;
                this.childs.forEach(x => x.hide());
            }
            return super.lostFocus();
        }

        public messageProc(msg: string, e: any): boolean {
            switch (msg) {
                case "mousedown":
                    if (this.include(e.mouseX, e.mouseY)) {
                        if (this.expanded) {
                            this.childs.forEach(x => x.hide());
                            this.expanded = false;
                        } else {
                            this.childs.forEach(x => x.show());
                            this.expanded = true;
                        }
                        return true;
                    } else {
                        return false;
                    }
                default:
                    return false;
            }
        }
    }
    class DropDownItem extends Window {
        constructor(name: string, x: number, y: number, width: number, height: number) {
            super(name, x, y, width, height);
        }
        public messageProc(msg: string, e: any): boolean {
            switch (msg) {
                case "mouseup":
                    if (this.include(e.mouseX, e.mouseY)) {
                        //this.parent.lostFocus();
                        return true;
                    } else {
                        return false;
                    }
                default:
                    return false;
            }
        }
    }

    class WindowManager {
        private rootWindow: RootWindow;
        private focusList: Component[];

        public constructor() {
            this.rootWindow = new RootWindow("<root>");
            this.rootWindow.visible = true;
            this.focusList = [this.rootWindow];
        }

        private createFocusList(self: Component, x: number, y: number, list: Component[]): boolean {
            let ret: boolean = false;
            loop:
            for (; ;) {
                if (self.include(x, y) || self.includeChilds(x, y)) {
                    ret = true;
                    list.push(self);
                    for (const child of self.childs) {
                        if (child.include(x, y) || child.includeChilds(x, y)) {
                            self = child;
                            continue loop;
                        }
                    }
                }
                return ret;
            }
        }

        public addChild(child: Component): void {
            this.rootWindow.addChild(child);
        }

        public draw(context: CanvasRenderingContext2D): void {
            this.rootWindow.draw(context);
        }

        public on(msg: string, e: any) : boolean {
            switch (msg) {
                case "mousedown": {
                    const traced: Component[] = [];
                    this.createFocusList(this.rootWindow, e.mouseX, e.mouseY, traced);

                    if (!(traced.length == this.focusList.length && traced[traced.length - 1] == this.focusList[this.focusList.length - 1])) {
                        const len = Math.min(traced.length, this.focusList.length);
                        let same = 0;
                        for (same = 0; same < len; same++) {
                            if (this.focusList[same] != traced[same]) {
                                break;
                            }
                        }

                        for (let i = this.focusList.length - 1; i >= same; i--) {
                            const tail = this.focusList[i];
                            tail.lostFocus();
                            this.focusList.pop();
                        }
                        for (let i = same; i < traced.length; i++) {
                            const tail = traced[i];
                            this.focusList.push(tail);
                        }
                        this.focusList[this.focusList.length - 1].getFocus();
                        for (const target of this.focusList) {
                            target.moveTop();
                        }
                    }
                    return this.focusList[this.focusList.length - 1].on("mousedown", { mouseX: e.mouseX, mouseY: e.mouseY });
                }
                case "mousemove": {
                    return this.focusList[this.focusList.length - 1].on("mousemove", { mouseX: e.mouseX, mouseY: e.mouseY });
                }
                case "mouseup": {
                    return this.focusList[this.focusList.length - 1].on("mouseup", { mouseX: e.mouseX, mouseY: e.mouseY });
                }
            }
            return false;
        }
    }

    window.onload = () => {

        const canvas: HTMLCanvasElement = <HTMLCanvasElement>document.getElementById("canvas");
        const context: CanvasRenderingContext2D = canvas.getContext("2d");

        let canvasBounds: ClientRect | DOMRect = null;
        function resizeCanvas(): void {
            canvasBounds = canvas.getBoundingClientRect();
            canvas.width = canvasBounds.width;
            canvas.height = canvasBounds.height;
        }
        window.addEventListener("resize", resizeCanvas);
        resizeCanvas();

        const windowManager = new WindowManager();
        canvas.addEventListener("mousedown", e => windowManager.on("mousedown", { mouseX: e.pageX - canvasBounds.left, mouseY: e.pageY - canvasBounds.top }));
        canvas.addEventListener("mousemove", e => windowManager.on("mousemove", { mouseX: e.pageX - canvasBounds.left, mouseY: e.pageY - canvasBounds.top }));
        canvas.addEventListener("mouseup"  , e => windowManager.on("mouseup"  , { mouseX: e.pageX - canvasBounds.left, mouseY: e.pageY - canvasBounds.top }));


        const w1 = new Draggable("<w1>", 15, 20, 100, 100);
        windowManager.addChild(w1);
        w1.show();

        const ww1 = new Button("<ww1>", 0, 50, 100, 50);
        w1.addChild(ww1);
        ww1.show();

        const w3 = new Window("<w3>", 95, 20, 30, 50);
        windowManager.addChild(w3);
        w3.show();

        const m1 = new DropDown("<m1>", 55, 20, 30, 50);
        windowManager.addChild(m1);
        m1.visible = true;

        const mm1 = new DropDownItem("<mm1>", 0, 50, 30, 50);
        m1.addChild(mm1);

        const mm2 = new DropDownItem("<mm2>", 0, 100, 30, 50);
        m1.addChild(mm2);

        function updateAnimationFrame(): void {
            context.clearRect(0, 0, canvas.width, canvas.height);
            windowManager.draw(context);
            requestAnimationFrame(updateAnimationFrame);
        }
        updateAnimationFrame();


    };
}
