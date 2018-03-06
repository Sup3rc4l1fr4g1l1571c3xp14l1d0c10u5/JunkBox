"use strict";

module Game {
    export module Scene {
        class Scene {
            private manager: SceneManager;
            private state: Generator;
            private init: (data: any) => Generator;

            constructor(manager: SceneManager, init: (data: any) => Generator) {
                this.manager = manager;
                this.state = null;
                this.init = init;
                this.update = null;
                this.draw = null;
                this.leave = null;
                this.suspend = null;
                this.resume = null;
            }

            next(...data: any[]): any {
                return this.state.next.apply(this.state, data);
            }

            push(id: string, param: any = {}): void { this.manager.push(id, param); }

            pop(): void { this.manager.pop(); }

            // virtual methods
            enter(...data: any[]): void {
                this.state = this.init.apply(this, data);
                this.next(null);
            }

            update: (delta: number, now: number) => void;

            draw: () => void;

            leave: () => void;

            suspend: () => void;

            resume: () => void;

        }

        export class SceneManager {
            private sceneStack: Scene[];
            private scenes: Map<string, (data: any) => IterableIterator<any>>;

            constructor(scenes: { [name: string]: (data: any) => IterableIterator<any> }) {
                this.scenes = new Map<string, (data: any) => IterableIterator<any>>();
                this.sceneStack = [];
                Object.keys(scenes).forEach((key) => this.scenes.set(key, scenes[key]));
            }

            public push(id: string, ...param: any[]): SceneManager {
                const sceneDef: (data: any) => IterableIterator<any> = this.scenes.get(id);
                if (this.scenes.has(id) === false) {
                    throw new Error(`scene ${id} is not defined.`);
                }
                if (this.peek() != null && this.peek().suspend != null) {
                    this.peek().suspend();
                }
                this.sceneStack.push(new Scene(this, sceneDef));
                this.peek().enter.apply(this.peek(), param);
                return this;
            }

            public pop(): SceneManager {
                if (this.sceneStack.length === 0) {
                    throw new Error("there is no scene.");
                }
                if (this.peek() != null) {
                    var p = this.sceneStack.pop();
                    if (p.leave != null) {
                        p.leave();
                    }
                }

                if (this.peek() != null && this.peek().resume != null) {
                    this.peek().resume();
                }
                return this;
            }

            public peek(): Scene {
                if (this.sceneStack.length > 0) {
                    return this.sceneStack[this.sceneStack.length - 1];
                } else {
                    return null;
                }
            }

            public update(...args: any[]): SceneManager {
                if (this.sceneStack.length === 0) {
                    throw new Error("there is no scene.");
                }
                this.peek().update.apply(this.peek(), args);
                return this;
            }

            public draw(): SceneManager {
                if (this.sceneStack.length === 0) {
                    throw new Error("there is no scene.");
                }
                this.peek().draw.apply(this.peek());
                return this;
            }
        }

    }
}
