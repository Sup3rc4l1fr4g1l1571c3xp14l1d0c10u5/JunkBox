/// <reference path="../SpriteAnimation.ts" />
/// <reference path="../GameData.ts" />
/// <reference path="./CharactorBase.ts" />
"use strict";

//  =((100+N58*N59)-(100+N61*N62)) * (1 + N60 - N63) / 10

namespace Charactor {
    interface MemberStatus {
        id:string;
        name:string;
        spriteSheet: SpriteAnimation.SpriteSheet;
        equips:  {
            wepon1?: GameData.EquipableItemData;
            armor1?: GameData.EquipableItemData;
            armor2?: GameData.EquipableItemData;
            accessory1?: GameData.EquipableItemData;
            accessory2?: GameData.EquipableItemData;
        };
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
                equips: Object.assign({}, forward.equips),
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
                    equips : Object.assign({}, backward.equips),
                    mp : backward.mp,
                    hp : backward.hp,
                    mpMax : backward.mp,
                    hpMax : backward.hp
                };
            }
        }

        public get atk() {
            return this.members[this.active].equips.reduce<GameData.EquipableItemData,number>((s, [v,k]) => s += v.atk, 0);
        }
        public get def() {
            return this.members[this.active].equips.reduce<GameData.EquipableItemData,number>((s, [v,k]) => s += v.def, 0);
        }

    }
}
