namespace Scene {
    export class Fade {
        private started: boolean;
        private startTime: number;
        private rate: number;
        private w: number;
        private h: number;
        private mode: string;

        constructor(w: number, h: number) {
            this.startTime = -1;
            this.started = false;
            this.w = w;
            this.h = h;
            this.mode = "";
        }
        public startFadeOut() {
            this.started = true;
            this.startTime = -1;
            this.rate = 0;
            this.mode = "fadeout";
        }
        public startFadeIn() {
            this.started = true;
            this.startTime = -1;
            this.rate = 1;
            this.mode = "fadein";
        }
        public stop() {
            this.started = false;
            this.startTime = -1;
        }
        public update(ms: number) {
            if (this.started === false) {
                return;
            }
            if (this.startTime === -1) {
                this.startTime = ms;
            }
            this.rate = (ms - this.startTime) / 500;
            if (this.rate < 0) {
                this.rate = 0;
            } else if (this.rate > 1) {
                this.rate = 1;
            }
            if (this.mode === "fadein") {
                this.rate = 1 - this.rate;
            }
        }
        public draw() {
            if (this.started) {
                Game.getScreen().fillStyle = `rgba(0,0,0,${this.rate})`;
                Game.getScreen().fillRect(0, 0, this.w, this.h);
            }
        }
        public isFinish() : boolean {
            return (this.mode === "fadein" && this.rate === 0) || (this.mode === "fadeout" && this.rate === 1);
        }
    }

    export function waitFadeIn(fade: Fade, action: () => void, intervalAction?: (e:number) => void) : () => void {
        fade.startFadeIn();
        const start = Game.getTimer().now;
        return () => {
            const elaps = Game.getTimer().now - start;
            if (intervalAction) {
                intervalAction(elaps);
            }
            fade.update(Game.getTimer().now);
            if (fade.isFinish()) {
                action();
            }
        };
    };
    export function waitFadeOut(fade:Fade, action : () => void, intervalAction?: (e:number) => void) : () => void {
        fade.startFadeOut();
        const start = Game.getTimer().now;
        return () => {
            const elaps = Game.getTimer().now - start;
            if (intervalAction) {
                intervalAction(elaps);
            }
            fade.update(Game.getTimer().now);
            if (fade.isFinish()) {
                action();
            }
        };
    };
    export function waitTimeout(ms:number, action : () => void, intervalAction?: (e:number) => void) : () => void {
        const start = Game.getTimer().now;
        return () => {
            const elaps = Game.getTimer().now - start;
            if (intervalAction) {
                intervalAction(elaps);
            }
            if (elaps >= ms) {
                action();
            }
        };
    };
    //export function waitTimeout({
    //    timeout,
    //    init = () => { },
    //    start = () => { },
    //    update = () => { },
    //    end = () => { },
    //}: {
    //        timeout: number;
    //        init?: () => void;
    //        start?: (elapsed: number) => void;
    //        update?: (elapsed: number) => void;
    //        end?: (elapsed: number) => void;
    //    }) {
    //    let startTime = -1;
    //    init();
    //    return () => {
    //        if (startTime === -1) {
    //            startTime = Game.getTimer().now;
    //            start(Game.getTimer().now);
    //        }
    //        const elapsed = Game.getTimer().now - startTime;
    //        if (elapsed >= timeout) {
    //            end(elapsed);
    //        } else {
    //            update(elapsed);
    //        }
    //    };
    //}

    export function waitClick(action : (x:number,y:number,e:number) => void, check? : (x:number,y:number,e:number) => void, intervalAction?: (e:number) => void) : () => void {
        const start = Game.getTimer().now;
        return () => {
            const elaps = Game.getTimer().now - start;
            if (intervalAction) {
                intervalAction(elaps);
            }
            if (Game.getInput().isClick()) {
                const pX = Game.getInput().pageX;
                const pY = Game.getInput().pageY;
                if (Game.getScreen().pagePointContainScreen(pX, pY)) {
                    const pos = Game.getScreen().pagePointToScreenPoint(pX, pY);
                    const xx = pos[0];
                    const yy = pos[1];
                    if (check == null || check(xx, yy, elaps)) {
                        action(xx, yy, elaps);
                    }
                }
            }
        };
    };

    //export function waitClick({
    //    update = () => { },
    //    start = () => { },
    //    check = () => true,
    //    end = () => { },
    //}: {
    //        update?: (elapsed: number) => void;
    //        start?: (elapsed: number) => void;
    //        check?: (x: number, y: number, elapsed: number) => boolean;
    //        end?: (x: number, y: number, elapsed: number) => void;
    //    }) {
    //    let startTime = -1;
    //    return () => {
    //        if (startTime === -1) {
    //            startTime = Game.getTimer().now;
    //            start(0);
    //        }
    //        const elapsed = Game.getTimer().now - startTime;
    //        if (Game.getInput().isClick()) {
    //            const pX = Game.getInput().pageX;
    //            const pY = Game.getInput().pageY;
    //            if (Game.getScreen().pagePointContainScreen(pX, pY)) {
    //                const pos = Game.getScreen().pagePointToScreenPoint(pX, pY);
    //                const xx = pos[0];
    //                const yy = pos[1];
    //                if (check(xx, yy, elapsed)) {
    //                    end(xx, yy, elapsed);
    //                    return;
    //                }
    //            }
    //        }
    //        update(elapsed);
    //    };
    //}
}