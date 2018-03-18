/// <reference path="../SpriteAnimation.ts" />
/// <reference path="../GameData.ts" />
/// <reference path="./CharactorBase.ts" />
"use strict";

namespace Charactor {
    export class Monster extends CharactorBase {
        public x: number;
        public y: number;
        public life: number;
        public maxLife: number;
        public atk: number;
        public def: number;


        constructor(monsterId: string) {
            var data = GameData.getMonsterConfig(monsterId);
            super(0, 0, data.sprite);
            this.life = data.hp;
            this.maxLife = data.hp;
            this.atk = data.atk;
            this.def = data.def;
        }
    }

}
