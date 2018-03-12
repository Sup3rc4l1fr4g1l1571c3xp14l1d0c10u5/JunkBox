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

    namespace Json {
        // スプライトシート
        export interface ISpriteSheet {
            source: { [id: string]: string };
            sprite: { [id: string]: ISprite };
            animation: { [id: string]: IAnimation[] };
        }

        // スプライト定義
        export interface ISprite {
            source: string;
            left: number;
            top: number;
            width: number;
            height: number;
            offsetX: number;
            offsetY: number;
        }

        // アニメーション定義
        export interface IAnimation {
            sprite: string;
            time: number;
            offsetX: number;
            offsetY: number;
        }
    }

    // スプライトシート
    export class SpriteSheet {
        public source: Map<string, HTMLImageElement>;
        public sprite: Map<string, Sprite>;
        public animation: Map<string, Animation[]>;
        constructor({ source = null, sprite = null, animation = null }: { source: Map<string, HTMLImageElement>; sprite: Map<string, Sprite>; animation: Map<string, Animation[]> }) {
            this.source = source;
            this.sprite = sprite;
            this.animation = animation;
        }

        public getAnimation(animName: string): Animation[] {
            return this.animation.get(animName);
        }

        public getAnimationFrame(animName: string, animFrame: number): Animation {
            return this.animation.get(animName)[animFrame];
        }

        public gtetSprite(spriteName: string): Sprite {
            return this.sprite.get(spriteName);
        }

        public getSpriteImage(sprite: Sprite): HTMLImageElement {
            return this.source.get(sprite.source);
        }
    }

    // スプライト定義
    class Sprite {
        public source: string;
        public left: number;
        public top: number;
        public width: number;
        public height: number;
        public offsetX: number;
        public offsetY: number;

        constructor(sprite: Json.ISprite) {
            this.source = sprite.source;
            this.left = sprite.left;
            this.top = sprite.top;
            this.width = sprite.width;
            this.height = sprite.height;
            this.offsetX = sprite.offsetX;
            this.offsetY = sprite.offsetY;
        }

    }

    // アニメーション定義
    class Animation {
        public sprite: string;
        public time: number;
        public offsetX: number;
        public offsetY: number;

        constructor(animation: Json.IAnimation) {
            this.sprite = animation.sprite;
            this.time = animation.time;
            this.offsetX = animation.offsetX;
            this.offsetY = animation.offsetY;
        }
    }

    async function loadImage(imageSrc: string): Promise<HTMLImageElement> {
        return new Promise<HTMLImageElement>((resolve, reject) => {
            const img = new Image();
            img.src = imageSrc;
            img.onload = () => {
                resolve(img);
            };
            img.onerror = () => { reject(imageSrc + "のロードに失敗しました。"); };
        });
    }

    export async function loadSpriteSheet(
        spriteSheetPath: string,
        loadStartCallback: () => void,
        loadEndCallback: () => void
    ): Promise<SpriteSheet> {

        const spriteSheetDir: string = getDirectory(spriteSheetPath);
        loadStartCallback();
        const spriteSheetJson: Json.ISpriteSheet = await ajax(spriteSheetPath, "json").then(y => y.response as Json.ISpriteSheet);
        loadEndCallback();
        if (spriteSheetJson == null) {
            throw new Error(spriteSheetPath + " is invalid json.");
        }

        const source: Map<string, HTMLImageElement> = new Map<string, HTMLImageElement>();
        {
            const keys = Object.keys(spriteSheetJson.source);
            for (let i = 0; i < keys.length; i++) {
                const key = keys[i];
                const imageSrc: string = spriteSheetDir + '/' + spriteSheetJson.source[key];
                loadStartCallback();
                const image: HTMLImageElement = await loadImage(imageSrc);
                loadEndCallback();
                source.set(key, image);
            }
        }

        const sprite: Map<string, Sprite> = new Map<string, Sprite>();
        {
            const keys = Object.keys(spriteSheetJson.sprite);
            for (let i = 0; i < keys.length; i++) {
                const key = keys[i];
                sprite.set(key, new Sprite(spriteSheetJson.sprite[key]));
            }
        }

        const animation: Map<string, Animation[]> = new Map<string, Animation[]>();
        {
            const keys = Object.keys(spriteSheetJson.animation);
            for (let i = 0; i < keys.length; i++) {
                const key = keys[i];
                const value = spriteSheetJson.animation[key].map(x => new Animation(x));
                animation.set(key, value);
            }
        }

        const spriteSheet: SpriteSheet = new SpriteSheet({
            source: source,
            sprite: sprite,
            animation: animation,
        });

        return spriteSheet;
    }

}
