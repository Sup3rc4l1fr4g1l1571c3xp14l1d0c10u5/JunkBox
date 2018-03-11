"use strict";

namespace Game {
    export namespace Timer {
        export class AnimationTimer extends Dispatcher.SingleDispatcher {
            private animationFrameId: number;
            private prevTime: number;

            constructor() {
                super();
                this.animationFrameId = NaN;
                this.prevTime = NaN;
            }

            public start(): boolean {
                if (!isNaN(this.animationFrameId)) {
                    this.stop();
                }
                this.animationFrameId = requestAnimationFrame(this.tick.bind(this));
                return !isNaN(this.animationFrameId);
            }

            public stop(): void {
                if (!isNaN(this.animationFrameId)) {
                    cancelAnimationFrame(this.animationFrameId);
                    this.animationFrameId = NaN;
                }
            }

            private tick(ts: number): void {
                requestAnimationFrame(this.tick.bind(this));
                if (!isNaN(this.prevTime)) {
                    const delta = ts - this.prevTime;
                    this.fire(delta, ts);
                }
                this.prevTime = ts;
            }
        }
    }
}
