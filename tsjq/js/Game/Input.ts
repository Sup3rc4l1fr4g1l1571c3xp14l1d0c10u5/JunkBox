"use strict";

module Game {
    export module Input {
        class CustomPointerEvent extends CustomEvent {
            public touch: boolean;
            public mouse: boolean;
            public pointerId: number;
            public pageX: number;
            public pageY: number;
            public maskedEvent: TouchEvent | MouseEvent;
        }
        enum PointerChangeStatus {
            Down,  Up, Leave
        }

        export class InputManager extends Dispatcher.EventDispatcher {
            private isScrolling: boolean;
            private timeout: number;
            private sDistX: number;
            private sDistY: number;
            private maybeClick: boolean;
            private maybeClickX: number;
            private maybeClickY: number;
            private prevTimeStamp: number;
            private prevInputType: string;

            private capture: boolean;
            private lastPageX: number;
            private lastPageY: number;
            private downup: number;
            private clicked: boolean;
            private lastDownPageX : number;
            private lastDownPageY: number;
            private draglen: number;

            private status: PointerChangeStatus;

            public get pageX(): number {
                return this.lastPageX;
            }
            public get pageY(): number {
                return this.lastPageY;
            }
            public isDown(): boolean {
                return this.downup == 1;
            }
            public isPush(): boolean {
                return this.downup > 1;
            }
            public isUp(): boolean {
                return this.downup == -1;
            }
            public isClick(): boolean {
                return this.clicked;
            }
            public isRelease(): boolean {
                return this.downup < -1;
            }
            public startCapture(): void {
                this.capture = true;
            }
            public endCapture(): void {
                this.capture = false;
                if (this.status == PointerChangeStatus.Down) {
                    if (this.downup < 1) { this.downup = 1; } else { this.downup += 1; }
                } else if (this.status == PointerChangeStatus.Up) {
                    if (this.downup > -1) { this.downup = -1; } else { this.downup -= 1; }
                } else {
                    this.downup = 0;
                }

                this.clicked = false;
                if (this.downup == -1) {
                    if (this.draglen < 5) {
                        this.clicked = true;
                    }
                } else if (this.downup == 1) {
                    this.lastDownPageX = this.lastPageX;
                    this.lastDownPageY = this.lastPageY;
                    this.draglen = 0;
                } else if (this.downup > 1) {
                    this.draglen = Math.max(this.draglen, Math.sqrt((this.lastDownPageX - this.lastPageX) * (this.lastDownPageX - this.lastPageX) +(this.lastDownPageY - this.lastPageY) * (this.lastDownPageY - this.lastPageY)));
                }


            }

            private captureHandler(e: CustomPointerEvent): void {
                if (this.capture == false) {
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

            constructor() {
                super();


                this.isScrolling = false;
                this.timeout = 0;
                this.sDistX = 0;
                this.sDistY = 0;
                this.maybeClick = false;
                this.maybeClickX = 0;
                this.maybeClickY = 0;
                this.prevTimeStamp = 0;
                this.prevInputType = "none";

                window.addEventListener("scroll",
                    () => {
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
                            },
                            100);
                    });

                // add event listener to body
                document.onselectstart = () => false;

                if (document.body["pointermove"]) {
                    consolere.log("pointer event is implemented");

                    document.body.addEventListener('touchmove', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('touchdown', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('touchup', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('mousemove', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('mousedown', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('mouseup', evt => { evt.preventDefault(); }, false);

                    document.body.addEventListener('pointerdown', (ev: PointerEvent) => this.fire('pointerdown', ev));
                    document.body.addEventListener('pointermove', (ev: PointerEvent) => this.fire('pointermove', ev));
                    document.body.addEventListener('pointerup', (ev: PointerEvent) => this.fire('pointerup', ev));
                    document.body.addEventListener('pointerleave', (ev: PointerEvent) => this.fire('pointerleave', ev));

                } else {
                    consolere.log('pointer event is not implemented');

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

            private checkEvent(e): boolean {
                e.preventDefault();
                const istouch = e.type.indexOf("touch") === 0;
                const ismouse = e.type.indexOf("mouse") === 0;
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

            private pointerDown(e) {
                if (this.checkEvent(e)) {
                    const evt = this.makePointerEvent("down", e);
                    const singleFinger = e["mouse"] || (e["touch"] && e.touches.length === 1);
                    if (!this.isScrolling && singleFinger) {
                        this.maybeClick = true;
                        this.maybeClickX = evt.pageX;
                        this.maybeClickY = evt.pageY;
                    }
                }
                return false;
            }

            private pointerLeave(e) {
                if (this.checkEvent(e)) {
                    this.maybeClick = false;
                    this.makePointerEvent("leave", e);
                }
                return false;
            }

            private pointerMove(e) {
                if (this.checkEvent(e)) {
                    this.makePointerEvent("move", e);
                }
                return false;
            }

            private pointerUp(e) {
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

            private makePointerEvent(type: string, e: TouchEvent | MouseEvent): CustomPointerEvent {
                const evt: CustomPointerEvent = <CustomPointerEvent>document.createEvent("CustomEvent");
                const eventType = `pointer${type}`;
                evt.initCustomEvent(eventType, true, true, {});
                evt.touch = e.type.indexOf("touch") === 0;
                evt.mouse = e.type.indexOf("mouse") === 0;
                if (evt.touch) {
                    const touchEvent: TouchEvent = <TouchEvent>e;
                    evt.pointerId = touchEvent.changedTouches[0].identifier;
                    evt.pageX = touchEvent.changedTouches[0].pageX;
                    evt.pageY = touchEvent.changedTouches[0].pageY;
                }
                if (evt.mouse) {
                    const mouseEvent: MouseEvent = <MouseEvent>e;
                    evt.pointerId = 0;
                    evt.pageX = mouseEvent.clientX + window.pageXOffset;
                    evt.pageY = mouseEvent.clientY + window.pageYOffset;
                }
                evt.maskedEvent = e;
                this.fire(eventType, evt);
                return evt;
            }
        }


        export class VirtualStick {
            isTouching: boolean;
            x: number;
            y: number;
            cx: number;
            cy: number;
            radius: number;
            distance: number;
            angle: number;
            id: number;

            get dir4(): number {
                switch (~~((this.angle + 180 + 45) / 90) % 4) {
                    case 0: return 4;   // left
                    case 1: return 8;   // up
                    case 2: return 6;   // right
                    case 3: return 2;   // down
                }
                return 5;   // neutral
            }

            get dir8(): number {
                switch (~~((this.angle + 180 + 45) / 90) % 8) {
                    case 0: return 4;   // left
                    case 1: return 7;   // left-up
                    case 2: return 8;   // up
                    case 3: return 9;   // right-up
                    case 4: return 6;   // right
                    case 5: return 3;   // right-down
                    case 6: return 6;   // down
                    case 7: return 1;   // left-down
                }
                return 5;   // neutral
            }


            constructor(x: number = 120, y: number = 120, radius: number = 40) {
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

            isHit(x: number, y: number): boolean {
                const dx = x - this.x;
                const dy = y - this.y;
                return ((dx * dx) + (dy * dy)) <= this.radius * this.radius;
            }

            onpointingstart(id: number): boolean {
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

            onpointingend(id: number): boolean {
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

            onpointingmove(id: number, x: number, y: number): boolean {
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
                } else {
                    this.cx = 0;
                    this.cy = 0;
                    this.angle = 0;
                    this.distance = 0;
                }
                return true;
            }
        }
    }
}
