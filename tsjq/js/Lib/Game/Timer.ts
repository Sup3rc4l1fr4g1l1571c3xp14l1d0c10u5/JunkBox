"use strict";

namespace Game {
    export namespace Timer {
        export class AnimationTimer extends Dispatcher.SingleDispatcher {
            private animationFrameId: number;
            private prevTime: number;
            public get now() : number {
                return this.prevTime;
            }

            constructor() {
                super();
                this.animationFrameId = NaN;
                this.prevTime = NaN;
                this.tick = this.tick.bind(this);
            }

            public start(): boolean {
                if (!isNaN(this.animationFrameId)) {
                    this.stop();
                }
                this.animationFrameId = requestAnimationFrame(this.tick);
                return !isNaN(this.animationFrameId);
            }

            public stop(): void {
                if (!isNaN(this.animationFrameId)) {
                    cancelAnimationFrame(this.animationFrameId);
                    this.animationFrameId = NaN;
                }
            }

            private tick(ts: number): void {
                requestAnimationFrame(this.tick);
                if (!isNaN(this.prevTime)) {
                    this.fire();
                }
                this.prevTime = ts;
            }
        }
    }
}
