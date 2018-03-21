/// <reference path="../SpriteAnimation.ts" />
/// <reference path="./UnitBase.ts" />
"use strict";

//  =((100+N58*N59)-(100+N61*N62)) * (1 + N60 - N63) / 10

namespace Unit {
    export interface MemberStatus {
        id: string;
        name: string;
        spriteSheet: SpriteAnimation.SpriteSheet;
        equips: {
            wepon1?: Data.Item.ItemBoxEntry;
            armor1?: Data.Item.ItemBoxEntry;
            armor2?: Data.Item.ItemBoxEntry;
            accessory1?: Data.Item.ItemBoxEntry;
            accessory2?: Data.Item.ItemBoxEntry;
        };
        hp: number;
        mp: number;
        hpMax: number;
        mpMax: number;
    }
    export class Player extends UnitBase {
        public members: MemberStatus[] = [];
        public active: number;
        public getForward(): MemberStatus {
            return this.members[this.active === 0 ? 0 : 1];
        }
        public getBackward(): MemberStatus {
            return this.members[this.active === 0 ? 1 : 0];
        }
        public get spriteSheet(): SpriteAnimation.SpriteSheet {
            return this.members[this.active].spriteSheet;
        }
        public set spriteSheet(value: SpriteAnimation.SpriteSheet) {
        }
        constructor(forward: Data.Player.Data, backward: Data.Player.Data) {
            super(0, 0, Data.Charactor.get(forward.id).sprite);
            const forwardConfig = Data.Charactor.get(forward.id);
            this.active = 0;
            this.members[0] = {
                id: forward.id,
                name: forwardConfig.name,
                spriteSheet: forwardConfig.sprite,
                equips: Object.assign({}, forward.equips),
                mp: forward.mp,
                hp: forward.hp,
                mpMax: forward.mp,
                hpMax: forward.hp
            };
            if (backward != null) {
                const backwardConfig = Data.Charactor.get(backward.id);
                this.members[1] = {
                    id: backward.id,
                    spriteSheet: backwardConfig.sprite,
                    name: backwardConfig.name,
                    equips: Object.assign({}, backward.equips),
                    mp: backward.mp,
                    hp: backward.hp,
                    mpMax: backward.mp,
                    hpMax: backward.hp
                };
            }
        }

        public get atk() {
            return this.members[this.active].equips.reduce<Data.Item.ItemBoxEntry, number>((s, [v, k]) => s += (v == null ? 0 : Data.Item.get(v.id).atk), 0);
        }
        public get def() {
            return this.members[this.active].equips.reduce<Data.Item.ItemBoxEntry, number>((s, [v, k]) => s += (v == null ? 0 : Data.Item.get(v.id).def), 0);
        }

    }
}
