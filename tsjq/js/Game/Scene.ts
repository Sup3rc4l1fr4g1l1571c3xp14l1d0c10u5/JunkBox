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
                this.update = () => { };
                this.draw = () => {};
                this.leave = () => { };
                this.suspend = () => { };
                this.resume = () => { };
            }

            next(...args:any[]): any {
                this.update = this.state.next.apply(this.state, args).value;
            }

            push(id: string, param: any = {}): void { this.manager.push(id, param); }

            pop(): void { this.manager.pop(); }

            // virtual methods
            enter(...data: any[]): void {
                this.state = this.init.apply(this, data);
                this.next();
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
            private requestQueue: (() => void)[];

            constructor(scenes: { [name: string]: (data: any) => IterableIterator<any> }) {
                this.scenes = new Map<string, (data: any) => IterableIterator<any>>();
                this.sceneStack = [];
                this.requestQueue = [];
                Object.keys(scenes).forEach((key) => this.scenes.set(key, scenes[key]));
            }

            public push(id: string, ...param: any[]): void {
                const sceneDef: (data: any) => IterableIterator<any> = this.scenes.get(id);
                if (this.scenes.has(id) === false) {
                    throw new Error(`scene ${id} is not defined.`);
                }
                if (this.peek() != null && this.peek().suspend != null) {
                    this.peek().suspend();
                }
                this.sceneStack.push(new Scene(this, sceneDef));
                this.peek().enter.apply(this.peek(), param);
            }

            //public push(id: string, ...param: any[]): SceneManager {
            //    this.requestQueue.push(() => this._push.apply(this, arguments));
            //    return this;
            //}


            public pop(): void {
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
            }

            //public pop(): SceneManager {
            //    this.requestQueue.push(() => this._pop());
            //    return this;
            //}

            public peek(): Scene {
                if (this.sceneStack.length > 0) {
                    return this.sceneStack[this.sceneStack.length - 1];
                } else {
                    return null;
                }
            }

            public update(...args: any[]): SceneManager {
                //var tmp = this.requestQueue;
                //this.requestQueue = [];
                //tmp.forEach((x) => x());
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
