/// <reference path="../SpriteAnimation.ts" />
/// <reference path="./UnitBase.ts" />
"use strict";

namespace Unit {
    export class Monster extends UnitBase {
        public x: number;
        public y: number;
        public life: number;
        public maxLife: number;
        public atk: number;
        public def: number;
        public dropRate: {[key:number]:((x:number, y:number) => DropItem)}; // d‚Ý•t‚«

        constructor(monsterId: string) {
            const data = Data.Monster.get(monsterId);
            super(0, 0, data.sprite);
            this.life = data.status.hp;
            this.maxLife = data.status.hp;
            this.atk = data.status.atk;
            this.def = data.status.def;
            this.dropRate = {};
        }
        getDrop(): ((x:number, y:number) => DropItem) {
            const keys = Object.keys(this.dropRate);
            const random = (Math.random() * 100);
            console.log("drop random=", random);
            let part = 0;
            for (const id of keys) {
                part += ~~id;
                if (random < part) {
                    return this.dropRate[~~id];
                }
            }
            return null;
        }
    }

}
