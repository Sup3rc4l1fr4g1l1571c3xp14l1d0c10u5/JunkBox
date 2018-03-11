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
    }

    export function waitTimeout({
        timeout,
        init = () => { },
        start = () => { },
        update = () => { },
        end = () => { },
    }: {
            timeout: number;
            init?: () => void;
            start?: (elapsed: number, ms: number) => void;
            update?: (elapsed: number, ms: number) => void;
            end?: (elapsed: number, ms: number) => void;
        }) {
        let startTime = -1;
        init();
        return (delta: number, ms: number) => {
            if (startTime === -1) {
                startTime = ms;
                start(0, ms);
            }
            const elapsed = ms - startTime;
            if (elapsed >= timeout) {
                end(elapsed, ms);
            } else {
                update(elapsed, ms);
            }
        };
    }

    export function waitClick({
        update = () => { },
        start = () => { },
        check = () => true,
        end = () => { },
    }: {
            update?: (elapsed: number, ms: number) => void;
            start?: (elapsed: number, ms: number) => void;
            check?: (x: number, y: number, elapsed: number, ms: number) => boolean;
            end?: (x: number, y: number, elapsed: number, ms: number) => void;
        }) {
        let startTime = -1;
        return (delta: number, ms: number) => {
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
}