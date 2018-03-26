"use strict";

namespace Game {
    export namespace Scene {
        export interface Scene {
            update(): void;
            draw(): void;
        }

        export class SceneManager {
            private sceneStack: Scene[];

            constructor() {
                this.sceneStack = [];
            }

            public push<T>(scene : Scene) : void {
                this.sceneStack.push(scene);
            }

            public pop(scene:Scene = null): void {
                if (this.sceneStack.length === 0) {
                    throw new Error("there is no scene.");
                }
                if (scene != null && this.peek() != scene) {
                    return;
                }
                if (this.peek() != null) {
                    this.sceneStack.pop();
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
