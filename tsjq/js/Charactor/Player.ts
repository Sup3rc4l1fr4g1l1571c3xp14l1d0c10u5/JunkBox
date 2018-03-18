/// <reference path="../SpriteAnimation.ts" />
/// <reference path="../GameData.ts" />
/// <reference path="./CharactorBase.ts" />
"use strict";

namespace Charactor {
    interface MemberStatus {
        id:string;
        name:string;
        spriteSheet: SpriteAnimation.SpriteSheet;
        equips: GameData.EquipableItem[];
        hp: number;
        mp: number;
        hpMax: number;
        mpMax: number;
    }
    export class Player extends CharactorBase {
        public members: MemberStatus[] = [];
            public active : number;
        public getForward(): MemberStatus {
            return this.members[this.active == 0 ? 0 : 1];
        }
        public getBackward(): MemberStatus {
            return this.members[this.active == 0 ? 1 : 0];
        }
        public get spriteSheet(): SpriteAnimation.SpriteSheet {
            return this.members[this.active].spriteSheet;
        }
        public set spriteSheet(value: SpriteAnimation.SpriteSheet) {
        }
        constructor() {
            super(0, 0, GameData.getPlayerData(GameData.forwardCharactor).config.sprite);
            const forward = GameData.getPlayerData(GameData.forwardCharactor);
            this.active = 0;
            this.members[0] = {
                id:GameData.forwardCharactor,
                name:forward.config.name,
                spriteSheet : forward.config.sprite,
                equips : forward.equips.slice(),
                mp : forward.mp,
                hp : forward.hp,
                mpMax : forward.mp,
                hpMax : forward.hp
            };
            const backward = GameData.getPlayerData(GameData.backwardCharactor);
            if (backward != null) {
                this.members[1] = {
                id:GameData.backwardCharactor,
                    spriteSheet : backward.config.sprite,
                name:backward.config.name,
                    equips : backward.equips.slice(),
                    mp : backward.mp,
                    hp : backward.hp,
                    mpMax : backward.mp,
                    hpMax : backward.hp
                };
            }
        }

        public get atk() {
            return this.members[this.active].equips.reduce((s, x) => s += x.atk, 0);
        }
        public get def() {
            return this.members[this.active].equips.reduce((s, x) => s += x.def, 0);
        }

    }
}
