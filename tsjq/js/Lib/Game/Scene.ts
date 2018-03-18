"use strict";

namespace Game {
    export namespace Scene {
        export class Scene {
            private manager: SceneManager;
            private state: Iterator<any>;
            private init: (data: any) => Iterator<any>;

            constructor(manager: SceneManager, init: (data: any) => Iterator<any>) {
                this.manager = manager;
                this.state = null;
                this.init = init;
                this.update = () => { };
                this.draw = () => { };
                this.leave = () => { };
                this.suspend = () => { };
                this.resume = () => { };
            }

            public next(...args: any[]): any {
                this.update = this.state.next.apply(this.state, args).value;
            }

            public enter(...data: any[]): void {
                this.state = this.init.apply(this, data);
                this.next();
            }

            public update: (delta: number, now: number) => void;

            public draw: () => void;

            public leave: () => void;

            public suspend: () => void;

            public resume: () => void;

        }

        export class SceneManager {
            private sceneStack: Scene[];

            constructor() {
                this.sceneStack = [];
            }

            public push<T>(sceneDef: (data?: T) => IterableIterator<any>, arg? : T): void {
                if (this.peek() != null && this.peek().suspend != null) {
                    this.peek().suspend();
                }
                this.sceneStack.push(new Scene(this, sceneDef));
                if (this.peek() != null && this.peek().enter != null) {
                    this.peek().enter.call(this.peek(), arg);
                }
            }

            public pop(): void {
                if (this.sceneStack.length === 0) {
                    throw new Error("there is no scene.");
                }
                if (this.peek() != null) {
                    const p = this.sceneStack.pop();
                    if (p.leave != null) {
                        p.leave();
                    }
                }

                if (this.peek() != null && this.peek().resume != null) {
                    this.peek().resume();
                }
            }

            public peek(): Scene {
                if (this.sceneStack.length > 0) {
                    return this.sceneStack[this.sceneStack.length - 1];
                } else {
                    return null;
                }
            }

            public update(...args: any[]): SceneManager {
                if (this.peek() != null && this.peek().update != null) {
                    this.peek().update.apply(this.peek(), args);
                }
                return this;
            }

            public draw(): SceneManager {
                if (this.peek() != null && this.peek().draw != null) {
                    this.peek().draw.apply(this.peek());
                }
                return this;
            }
        }

    }
}
