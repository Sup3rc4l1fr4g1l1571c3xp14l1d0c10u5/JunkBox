/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/3.1/lib.dom.d.ts" />
/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/3.1/lib.es2016.d.ts" />

module tswm {
    class Component {
        private _left: number;
        public get left(): number { return this._left + (this.parent == null ? 0 : this.parent.left); }
        public set left(value: number) { this._left = value - (this.parent == null ? 0 : this.parent.left); }

        private _top: number;
        public get top(): number { return this._top + (this.parent == null ? 0 : this.parent.top); }
        public set top(value: number) { this._top = value - (this.parent == null ? 0 : this.parent.top); }

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
        public hasFocus: boolean;

        constructor(name: string, left: number, top: number, width: number, height: number) {
            this._name = name;
            this.childs = [];
            this._visible = false;
            this._left = left;
            this._top = top;
            this._width = width;
            this._height = height;
            this.hasFocus = false;
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
                this.childs[i].draw(ctx);
            }
        }

        public on(msg: string, e: any): boolean {
            if (this.visible == false) { return false; }
            return this.childs.some(x => x.on(msg, e));
        }

        public include(x: number, y: number): boolean {
            return this.visible && this.left <= x && x < this.left + this.width && this.top <= y && y < this.top + this.height;
        }

        public includeChilds(x: number, y: number): boolean {
            return this.childs.some(child => child.visible && (child.include(x, y) || child.includeChilds(x, y)));
        }

        public lostFocus(): boolean {
            if (this.hasFocus) {
                this.hasFocus = false;
                console.log("lost focus " + this.name);
                return true;
            }
            return false;
        }

        public getFocus(): boolean {
            if (!this.hasFocus) {
                this.hasFocus = true;
                console.log("get focus " + this.name);
                return true;
            }
            return false;
        }

        public createFocusList(x: number, y: number, list: Component[]): boolean {
            if (this.include(x, y) || this.includeChilds(x, y)) {
                list.push(this);
                for (const child of this.childs) {
                    if (child.include(x, y) || child.includeChilds(x, y)) {
                        child.createFocusList(x, y, list);
                        break;
                    }
                }
                return true;
            }
            return false;
        }

        public show(): void {
            this._visible = true;
        }

        public hide(): void {
            if (this.hasFocus) {
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

        constructor(name: string, left: number, top: number, width: number, height: number) {
            super(name, left, top, width, height);
        }

        public draw(ctx: CanvasRenderingContext2D): void {
            if (this.visible == false) { return; }
            ctx.save();
            ctx.fillStyle = "rgb(128,255,255)";
            ctx.beginPath();
            ctx.rect(this.left, this.top, this.width, this.height);
            ctx.fill();
            ctx.stroke();
            ctx.restore();
            for (let i = this.childs.length - 1; i >= 0; i--) {
                this.childs[i].draw(ctx);
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

    class Draggable extends Window {
        private dragStart: boolean;
        private mouseX: number;
        private mouseY: number;

        constructor(name: string, left: number, top: number, width: number, height: number) {
            super(name, left, top, width, height);
            this.dragStart = false;
            this.mouseX = Number.NaN;
            this.mouseY = Number.NaN;
        }

        public on(msg: string, e: any): boolean {
            if (this.visible == false) { return false; }
            if (this.childs.find(x => x.on(msg, e))) { return true; }
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
                        this.left += (e.mouseX - this.mouseX);
                        this.top += (e.mouseY - this.mouseY);
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

        constructor(name: string, left: number, top: number, width: number, height: number) {
            super(name, left, top, width, height);
            this.expanded = false;
        }

        public lostFocus(): boolean {
            if (this.expanded) {
                this.expanded = false;
                this.childs.forEach(x => x.hide());
            }
            return super.lostFocus();
        }

        public on(msg: string, e: any): boolean {
            if (this.visible == false) { return false; }
            if (this.childs.find(x => x.on(msg, e))) { return true; }
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
        constructor(name: string, left: number, top: number, width: number, height: number) {
            super(name, left, top, width, height);
        }
        public on(msg: string, e: any): boolean {
            if (this.visible == false) { return false; }
            if (this.childs.find(x => x.on(msg, e))) { return true; }
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

    window.onload = () => {

        const canvas: HTMLCanvasElement = <HTMLCanvasElement>document.getElementById("canvas");
        const context: CanvasRenderingContext2D = canvas.getContext("2d");

        const rootWindow = new RootWindow("<root>");
        rootWindow.visible = true;
        let canvasBounds: ClientRect | DOMRect = null;

        function postMessage(target: Component, msg: string, e: any): boolean {
            return target.on(msg, e);
        }

        window.addEventListener("resize", resizeCanvas);
        const focusList = [rootWindow];
        canvas.addEventListener("mousedown", e => {
            const x = e.pageX - canvasBounds.left;
            const y = e.pageY - canvasBounds.top;
            const traced: Component[] = [];
            rootWindow.createFocusList(x, y, traced);

            if (!(traced.length == focusList.length && traced[traced.length - 1] == focusList[focusList.length - 1])) {
                const len = Math.min(traced.length, focusList.length);
                let same = 0;
                for (same = 0; same < len; same++) {
                    if (focusList[same] != traced[same]) {
                        break;
                    }
                }

                for (let i = focusList.length - 1; i >= same; i--) {
                    const tail = focusList[i];
                    tail.lostFocus();
                    focusList.pop();
                }
                for (let i = same; i < traced.length; i++) {
                    const tail = traced[i];
                    focusList.push(tail);
                }
                focusList[focusList.length - 1].getFocus();
                for (const target of focusList) {
                    target.moveTop();
                }
            }
            postMessage(focusList[focusList.length - 1], "mousedown", { mouseX: e.pageX - canvasBounds.left, mouseY: e.pageY - canvasBounds.top });
        });
        canvas.addEventListener("mousemove", e => postMessage(rootWindow, "mousemove", { mouseX: e.pageX - canvasBounds.left, mouseY: e.pageY - canvasBounds.top }));
        canvas.addEventListener("mouseup", e => postMessage(rootWindow, "mouseup", { mouseX: e.pageX - canvasBounds.left, mouseY: e.pageY - canvasBounds.top }));

        function resizeCanvas(): void {
            canvasBounds = canvas.getBoundingClientRect();
            canvas.width = canvasBounds.width;
            canvas.height = canvasBounds.height;
        }
        resizeCanvas();

        const w1 = new Draggable("<w1>", 15, 20, 30, 50);
        rootWindow.addChild(w1);
        w1.show();

        const w3 = new Window("<w3>", 95, 20, 30, 50);
        rootWindow.addChild(w3);
        w3.show();

        const m1 = new DropDown("<m1>", 55, 20, 30, 50);
        rootWindow.addChild(m1);
        m1.visible = true;

        const mm1 = new DropDownItem("<mm1>", 0, 50, 30, 50);
        m1.addChild(mm1);

        const mm2 = new DropDownItem("<mm2>", 0, 100, 30, 50);
        m1.addChild(mm2);

        function updateAnimationFrame(): void {
            context.clearRect(0, 0, canvas.width, canvas.height);
            rootWindow.draw(context);
            requestAnimationFrame(updateAnimationFrame);
        }
        updateAnimationFrame();


    };
}
