/// <reference path="../SpriteAnimation.ts" />
/// <reference path="./UnitBase.ts" />
"use strict";

namespace Unit {
    export interface MemberStatus {
        id: string;
        name: string;
        spriteSheet: SpriteAnimation.SpriteSheet;
        equips: Data.Player.EquipData;
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
                mp: forward.mp + Player.reduceEquips(forward.equips, (s, x) => s + x.mp, 0),
                hp: forward.hp + Player.reduceEquips(forward.equips, (s, x) => s + x.hp, 0),
                mpMax: forward.mp + Player.reduceEquips(forward.equips, (s, x) => s + x.mp, 0),
                hpMax: forward.hp + Player.reduceEquips(forward.equips, (s, x) => s + x.hp, 0)
            };
            if (backward != null) {
                const backwardConfig = Data.Charactor.get(backward.id);
                this.members[1] = {
                    id: backward.id,
                    spriteSheet: backwardConfig.sprite,
                    name: backwardConfig.name,
                    equips: Object.assign({}, backward.equips),
                    mp: backward.mp + Player.reduceEquips(backward.equips, (s, x) => s + x.mp, 0),
                    hp: backward.hp + Player.reduceEquips(backward.equips, (s, x) => s + x.hp, 0),
                    mpMax: backward.mp + Player.reduceEquips(backward.equips, (s, x) => s + x.mp, 0),
                    hpMax: backward.hp + Player.reduceEquips(backward.equips, (s, x) => s + x.hp, 0)
                };
            }
        }

        private static reduceEquips<T>(equipData: Data.Player.EquipData, pred:(s: T, x: Data.Item.Data) => T, seed:T): T {
            return equipData.reduce<Data.Item.ItemBoxEntry, T>((s, [v, k]) => (v == null) ? s : pred(s, Data.Item.get(v.id)),seed);
        }

        public get atk() {
            return Player.reduceEquips(this.members[this.active].equips, (s, x) => s + x.atk, 0);
        }
        public get def() {
            return Player.reduceEquips(this.members[this.active].equips, (s, x) => s + x.def, 0);
        }

    }
}
