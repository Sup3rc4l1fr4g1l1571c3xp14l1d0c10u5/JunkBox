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


        constructor(monsterId: string) {
            const data = Data.Monster.get(monsterId);
            super(0, 0, data.sprite);
            this.life = data.status.hp;
            this.maxLife = data.status.hp;
            this.atk = data.status.atk;
            this.def = data.status.def;
        }
    }

}
