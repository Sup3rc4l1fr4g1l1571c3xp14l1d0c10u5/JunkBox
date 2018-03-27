/// <reference path="../SpriteAnimation.ts" />
/// <reference path="./UnitBase.ts" />
"use strict";

namespace Unit {
    export enum BuffKind {
        AdditiveTurn,   // 追加行動力。5%=1ターンに5%獲得。100%になると追加行動を獲得
        MPDecTime,      // MP減少力。10%=1ターンに10%減少。100%になるとMPが1減少。
    }

    interface AdditiveTurnWork {
        inverval:number;
    }
    interface MPDecTimeWork {
        inverval:number;
    }

    export interface BuffData {
        /**
         * 有効時間
         */
        availableTime:number;   
        /**
         * 効果種別
         */
        kind : BuffKind;
        /**
         * ワークデータ
         */
        workdata : any;
    }
    export function AdditiveTurn(interval: number) : BuffData {
        return {
            availableTime: -1,
            kind: Unit.BuffKind.AdditiveTurn,
            workdata: {
                inverval: interval,
            } as AdditiveTurnWork
    };
    }
    export function MPDecTime(interval: number) : BuffData {
        return {
            availableTime: -1,
            kind: Unit.BuffKind.MPDecTime,
            workdata: {
                inverval: interval,
            } as MPDecTimeWork
    };
    }


    export interface MemberStatus {
        id: string;
        name: string;
        spriteSheet: SpriteAnimation.SpriteSheet;
        hp: number;
        mp: number;
        hpMax: number;
        mpMax: number;
        atk: number;
        def: number;

        // 追加ターン獲得力
        additiveTurn: number;

        // mp減少力
        mpDecTime: number;

        buffs: BuffData[];
    }

    function reduceEquips<T>(equipData: Data.Player.EquipData, pred:(s: T, x: Data.Item.Data) => T, seed:T): T {
        return equipData.reduce<Data.Item.ItemBoxEntry, T>((s, [v, k]) => (v == null) ? s : pred(s, Data.Item.get(v.id)),seed);
    }

    function setupMemberStatus(id: string): MemberStatus {
        const forward = Data.Charactor.get(id);
        const memberData = Data.SaveData.findCharactorById(id);
        const ret = {
            id: id,
            name: forward.name,
            spriteSheet: forward.sprite,
            mp: forward.status.mp + reduceEquips(memberData.equips, (s, x) => s + x.mp, 0),
            hp: forward.status.hp + reduceEquips(memberData.equips, (s, x) => s + x.hp, 0),
            mpMax: forward.status.mp + reduceEquips(memberData.equips, (s, x) => s + x.mp, 0),
            hpMax: forward.status.hp + reduceEquips(memberData.equips, (s, x) => s + x.hp, 0), 
            atk: reduceEquips(memberData.equips, (s, x) => s + x.atk, 0),
            def: reduceEquips(memberData.equips, (s, x) => s + x.def, 0),
            additiveTurn: 0,
            mpDecTime: 10,
            buffs: reduceEquips(memberData.equips, (s, x) => { x.buffs(s); return s; }, [] as BuffData[]),
        };
        updateMemberStatus(ret);
        return ret;
    }
    export function updateMemberStatus(memberStatus: MemberStatus): void {
        const forward = Data.Charactor.get(memberStatus.id);
        const memberData = Data.SaveData.findCharactorById(memberStatus.id);

        memberStatus.mpMax = forward.status.mp + reduceEquips(memberData.equips, (s, x) => s + x.mp, 0);
        memberStatus.hpMax = forward.status.hp + reduceEquips(memberData.equips, (s, x) => s + x.hp, 0);
        memberStatus.atk = reduceEquips(memberData.equips, (s, x) => s + x.atk, 0);
        memberStatus.def = reduceEquips(memberData.equips, (s, x) => s + x.def, 0);
        memberStatus.additiveTurn = 0;
        memberStatus.mpDecTime = 10;
        memberStatus.buffs.removeIf(x => {
            if (x.availableTime > 0) {
                x.availableTime--;
            }
            if (x.availableTime > 0 || x.availableTime == -1) {
                switch (x.kind) {
                    case BuffKind.AdditiveTurn:
                        memberStatus.additiveTurn += (x.workdata as AdditiveTurnWork).inverval;
                        break;
                    case BuffKind.MPDecTime:
                        memberStatus.additiveTurn += (x.workdata as MPDecTimeWork).inverval;
                        break;
                }
            }
            return (x.availableTime) == 0;
        });
    }


    /**
     * パーティ
     */
    export class Party extends UnitBase {
        public members: MemberStatus[] = [];
        public active: number;

        public additiveTurnValue:number;
        public mpDecTimeValue:number;

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
            this.members[0] = setupMemberStatus(forward.id);
            if (backward != null) {
                this.members[1] = setupMemberStatus(backward.id);
            } else {
                this.members[1] = null;
            }
            this.additiveTurnValue = 0;
            this.mpDecTimeValue = 0;
        }


        public get atk() {
            return this.members[this.active].atk;
        }
        public get def() {
            return this.members[this.active].def;
        }
        public get additiveTurn() {
            return Math.max(0, this.members[0].additiveTurn + (this.members[1] == null ? 0 : this.members[1].additiveTurn));
        }
        public get mpDecTime() {
            return Math.max(1, this.members[0].mpDecTime + (this.members[1] == null ? 0 : this.members[1].mpDecTime));
        }

    }
}
