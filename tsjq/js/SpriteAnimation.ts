"use strict";

namespace SpriteAnimation {
    export abstract class Animator {

        public dir: number;
        public animDir: number;
        public animFrame: number;
        public offx: number;
        public offy: number;
        public animName: string;

        constructor(public spriteSheet: SpriteSheet) {
            this.offx = 0;
            this.offy = 0;
            this.dir = 5;
            this.animDir = 2;
            this.animFrame = 0;
            this.animName = "idle";
        }

        public setDir(dir: number) {
            if (dir === 0) {
                return;
            }

            this.dir = dir;
            switch (dir) {
                case 1: {
                    if (this.animDir === 4) { this.animDir = 4; } else if (this.animDir === 2) { this.animDir = 2; } else if (this.animDir === 8) { this.animDir = 2; } else if (this.animDir === 6) { this.animDir = 4; }
                    break;
                }
                case 3: {
                    if (this.animDir === 4) { this.animDir = 6; } else if (this.animDir === 2) { this.animDir = 2; } else if (this.animDir === 8) { this.animDir = 2; } else if (this.animDir === 6) { this.animDir = 2; }
                    break;
                }
                case 9: {
                    if (this.animDir === 4) { this.animDir = 6; } else if (this.animDir === 2) { this.animDir = 8; } else if (this.animDir === 8) { this.animDir = 8; } else if (this.animDir === 6) { this.animDir = 6; }
                    break;
                }
                case 7: {
                    if (this.animDir === 4) { this.animDir = 4; } else if (this.animDir === 2) { this.animDir = 8; } else if (this.animDir === 8) { this.animDir = 8; } else if (this.animDir === 6) { this.animDir = 4; }
                    break;
                }
                case 5: {
                    break;
                }
                default: {
                    this.animDir = dir;
                    break;
                }
            }
        }

        private static animationName: { [key: number]: string } = {
            2: "move_down",
            4: "move_left",
            5: "idle",
            6: "move_right",
            8: "move_up",
        };

        public setAnimation(type: string, rate: number) {
            if (rate > 1) {
                rate = 1;
            }
            if (rate < 0) {
                rate = 0;
            }

            if (type === "move" || type === "action") {
                if (type === "move") {
                    this.offx = ~~(Array2D.DIR8[this.dir].x * 24 * rate);
                    this.offy = ~~(Array2D.DIR8[this.dir].y * 24 * rate);
                } else if (type === "action") {
                    this.offx = ~~(Array2D.DIR8[this.dir].x * 12 * Math.sin(rate * Math.PI));
                    this.offy = ~~(Array2D.DIR8[this.dir].y * 12 * Math.sin(rate * Math.PI));
                }
                this.animName = Animator.animationName[this.animDir];

            } else if (type === "dead") {
                this.animName = "dead";
                this.offx = 0;
                this.offy = 0;
            } else {
                return;
            }
            const animDefs = this.spriteSheet.getAnimation(this.animName);
            const totalWeight = animDefs.reduce((s, x) => s + x.time, 0);
            const targetRate = rate * totalWeight;
            let sum = 0;
            for (let i = 0; i < animDefs.length; i++) {
                const next = sum + animDefs[i].time;
                if (sum <= targetRate && targetRate < next) {
                    this.animFrame = i;
                    return;
                }
                sum = next;
            }
            this.animFrame = animDefs.length - 1;
        }
    }

    // スプライトシート
    export interface ISpriteSheet {
            source: {[key:number] : string};
            sprite: { 
                [key: number]:  ISprite
            };
            animation: {
                [key: string]: IAnimation[]
            };
    }
    export interface ISprite {
        source: number;
        left:number;
        top:number;
        width:number;
        height:number;
        offsetX:number;
        offsetY:number;
    }
    export interface IAnimation {
        sprite:number;
        time:number;
        offsetX: number;
        offsetY:number;
    }

    // スプライトシート
    export class SpriteSheet {
        public source: Map<number, HTMLImageElement>;
        public sprite: Map<number, ISprite>;
        public animation: Map<string, IAnimation[]>;
        constructor({ source = null, sprite = null, animation = null }: { source: Map<number, HTMLImageElement>; sprite: Map<number, ISprite>; animation: Map<string, IAnimation[]> }) {
            this.source = source;
            this.sprite = sprite;
            this.animation = animation;
        }

        public getAnimation(animName: string): IAnimation[] {
            return this.animation.get(animName);
        }

        public getAnimationFrame(animName: string, animFrame: number): IAnimation {
            return this.animation.get(animName)[animFrame];
        }

        public gtetSprite(id: number): ISprite {
            return this.sprite.get(id);
        }

        public getSpriteImage(sprite: ISprite): HTMLImageElement {
            return this.source.get(sprite.source);
        }

        private static async loadImage(imageSrc: string): Promise<HTMLImageElement> {
            return new Promise<HTMLImageElement>((resolve, reject) => {
                const img = new Image();
                img.src = imageSrc;
                img.onload = () => {
                    resolve(img);
                };
                img.onerror = () => { 
                    reject(imageSrc + "のロードに失敗しました。"); 
                };
            });
        }

        public static async Create(
            ss: ISpriteSheet,
            loadStartCallback: () => void,
            loadEndCallback: () => void
        ) : Promise<SpriteSheet> {
            const source: Map<number, HTMLImageElement> = new Map<number, HTMLImageElement>();
            const sprite: Map<number, ISprite> = new Map<number, ISprite>();
            const animation: Map<string, IAnimation[]> = new Map<string, IAnimation[]>();


            {
            const keys = Object.keys(ss.source);
            for (let i = 0; i < keys.length; i++) {
                const key = ~~keys[i];
                const imageSrc: string = ss.source[key];
                loadStartCallback();
                const image: HTMLImageElement = await SpriteSheet.loadImage(imageSrc).catch(() => null);
                loadEndCallback();
                source.set(key, image);
            }
                }
            {
            const keys = Object.keys(ss.sprite);
            for (let i = 0; i < keys.length; i++) {
                const key = ~~keys[i];
                sprite.set(key, ss.sprite[key]);
            }
                }
            {
            const keys = Object.keys(ss.animation);
            for (let i = 0; i < keys.length; i++) {
                const key = keys[i];
                animation.set(key, ss.animation[key]);
            }
                }
            return new SpriteSheet({source:source, sprite:sprite, animation:animation});

        }
    }

}
