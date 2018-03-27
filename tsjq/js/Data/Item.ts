/// <reference path="../Unit/Party.ts" />
"use strict";

namespace Data {
    export namespace Item {
        export enum Kind {
            Wepon,
            Armor1,
            Armor2,
            Accessory,
            Tool,
            Treasure,
        }

        export interface Data {
            id: number;
            name: string;
            price: number;
            kind: Kind;
            description: string;
            stackable: boolean;
            hp: number;
            mp: number;
            atk: number;
            def: number;
            buffs: (datas: Unit.BuffData[]) => void;
            useToPlayer?: (target: Unit.MemberStatus) => boolean;
            useToParty?: (targets: Unit.Party) => boolean;
        }

        export interface ItemBoxEntry {
            id: number;
            condition: string;
            count: number;
        }

        const Datas: Data[] = [
            /* Wepon */
            {
                id: 1,
                name: "竹刀",
                price: 300,
                kind: Kind.Wepon,
                description: "授業用なので少しボロイ",
                hp: 0,
                mp: 0,
                atk: 3,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },
            {
                id: 2,
                name: "鉄パイプ",
                price: 500,
                kind: Kind.Wepon,
                description: "手ごろな大きさと重さで扱いやすい",
                hp: 0,
                mp: 0,
                atk: 5,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },
            {
                id: 3,
                name: "バット",
                price: 700,
                kind: Kind.Wepon,
                description: "目指せ場外ホームラン",
                hp: 0,
                mp: 0,
                atk: 7,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },

            /* Armor1 */
            {
                id: 101,
                name: "水着",
                price: 200,
                kind: Kind.Armor1,
                description: "動きやすいが防御はやや不安",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 1,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },
            {
                id: 102,
                name: "制服",
                price: 400,
                kind: Kind.Armor1,
                description: "学校指定のものらしい",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 2,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },
            {
                id: 103,
                name: "体操着",
                price: 600,
                kind: Kind.Armor1,
                description: "胸部が窮屈と不評",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 3,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },

            /* Armor2 */
            {
                id: 201,
                name: "スカート",
                price: 200,
                kind: Kind.Armor2,
                description: "エッチな風さんですぅ",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 1,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },
            {
                id: 202,
                name: "ブルマ",
                price: 400,
                kind: Kind.Armor2,
                description: "歳がバレますわ！",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 2,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },
            {
                id: 203,
                name: "ズボン",
                price: 600,
                kind: Kind.Armor2,
                description: "足が細く見えます。",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 3,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },

            /* Accessory */
            {
                id: 301,
                name: "ヘアバンド",
                price: 2000,
                kind: Kind.Accessory,
                description: "デコ！",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 1,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },
            {
                id: 302,
                name: "メガネ",
                price: 2000,
                kind: Kind.Accessory,
                description: "メガネは不人気",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 1,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },
            {
                id: 303,
                name: "靴下",
                price: 2000,
                kind: Kind.Accessory,
                description: "色も長さも様々",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 1,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },
            {
                id: 304,
                name: "欠けた歯車",
                price: 0,
                kind: Kind.Accessory,
                description: "ナニカサレタヨウダ…",
                hp: 0,
                mp: 0,
                atk: 1,
                def: 1,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },
            {
                id: 305,
                name: "南蛮仮面",
                price: 0,
                kind: Kind.Accessory,
                description: "南蛮渡来の仮面。これを付けると誰かわからなくなる。",
                hp: 0,
                mp: 10,
                atk: 0,
                def: 1,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },
            {
                id: 306,
                name: "悪魔の核(レプリカ)",
                price: 0,
                kind: Kind.Accessory,
                description: "カチャカチャするとペカーと光る。",
                hp: 0,
                mp: 0,
                atk: 2,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: false
            },
            {
                id: 307,
                name: "アンクレット",
                price: 5000,
                kind: Kind.Accessory,
                description: "少し素早くなる。",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 1,
                buffs: (datas: Unit.BuffData[]) => datas.push(Unit.AdditiveTurn(3)),
                stackable: false
            },
            {
                id: 308,
                name: "巨女の小手",
                price: 5000,
                kind: Kind.Accessory,
                description: "力が沸いてきますがお腹も空きます。",
                hp: 0,
                mp: 0,
                atk: 3,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => datas.push(Unit.MPDecTime(5)),
                stackable: false
            },

            /* Tool */
            {
                id: 501,
                name: "イモメロン",
                price: 100,
                kind: Kind.Tool,
                description: "空腹時にどうぞ。",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: true,
                useToPlayer: (target: Unit.MemberStatus) => {
                    if (target.mp < target.mpMax) {
                        target.mp = Math.min(target.mpMax, target.mp + 10);
                        return true;
                    } else {
                        return false;
                    }
                }
            },
            {
                id: 502,
                name: "プリングルス",
                price: 890,
                kind: Kind.Tool,
                description: "あの歌舞伎役者もおすすめ",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: true,
                useToPlayer: (target: Unit.MemberStatus) => {
                    if (target.mp < target.mpMax || target.hp < target.hpMax) {
                        target.mp = Math.min(target.mpMax, target.mp + 50);
                        target.hp = Math.min(target.hpMax, target.hp + 39);
                        return true;
                    } else {
                        return false;
                    }
                }
            },
            {
                id: 503,
                name: "バンテリン",
                price: 931,
                kind: Kind.Tool,
                description: "肩にも腰にも効いてくれる。ありがたい…",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: true,
                useToPlayer: (target: Unit.MemberStatus) => {
                    if (target.mp < target.mpMax || target.hp < target.hpMax) {
                        target.mp = Math.min(target.mpMax, target.mp + 50);
                        target.hp = Math.min(target.hpMax, target.hp + 43);
                        return true;
                    } else {
                        return false;
                    }
                }
            },
            {
                id: 504,
                name: "サラダチキン",
                price: 1000,
                kind: Kind.Tool,
                description: "恐らくこのハーブはダメかと…",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: true,
                useToPlayer: (target: Unit.MemberStatus) => {
                    if (target.mp < target.mpMax || target.hp < target.hpMax) {
                        target.mp = Math.min(target.mpMax, target.mp + 50);
                        target.hp = Math.min(target.hpMax, target.hp + 50);
                        return true;
                    } else {
                        return false;
                    }
                }
            },
            {
                id: 505,
                name: "カレーメシ",
                price: 1000,
                kind: Kind.Tool,
                description: "ジャスティス！",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: true,
                useToPlayer: (target: Unit.MemberStatus) => {
                    if (target.mp < target.mpMax || target.hp < target.hpMax) {
                        target.mp = Math.min(target.mpMax, target.mp + 50);
                        target.hp = Math.min(target.hpMax, target.hp + 50);
                        return true;
                    } else {
                        return false;
                    }
                }
            },
            {
                id: 506,
                name: "ヌカコーラ",
                price: 1000,
                kind: Kind.Tool,
                description: "ヌカッとさわやか！",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: true,
                useToPlayer: (target: Unit.MemberStatus) => {
                    if (target.mp < target.mpMax || target.hp < target.hpMax) {
                        target.mp = Math.min(target.mpMax, target.mp + 50);
                        target.hp = Math.min(target.hpMax, target.hp + 50);
                        return true;
                    } else {
                        return false;
                    }
                }
            },
            {
                id: 507,
                name: "ハンバーグ",
                price: 300,
                kind: Kind.Tool,
                description: "三つ並べると何かに見えるようだ",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: true,
                useToPlayer: (target: Unit.MemberStatus) => {
                    if (target.hp < target.hpMax) {
                        target.hp = Math.min(target.hpMax, target.hp + 30);
                        return true;
                    } else {
                        return false;
                    }
                }
            },
            {
                id: 508,
                name: "ストロングゼロ",
                price: 955,
                kind: Kind.Tool,
                description: "耐えられない。呑む。",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: true
            },
            {
                id: 601,
                name: "携帯電話",
                price: 2000,
                kind: Kind.Tool,
                description: "いきなり呼び出されても安心",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: true,
                useToParty: (party: Unit.Party) => {
                    Game.getSceneManager().pop();
                    Game.getSceneManager().push(new Scene.Corridor());
                    return true;
                }
            },
            {
                id: 901,
                name: "猛毒薬",
                price: 0,
                kind: Kind.Tool,
                description: "死んでも一緒だよ･･･",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: true,
                useToParty: (party: Unit.Party) => {
                    party.getForward().hp = 0;
                    party.getBackward().hp = 0;
                    return true;
                }
            },

            /* Treasure */
            {
                id: 1001,
                name: "粘つく液体",
                price: 50,
                kind: Kind.Treasure,
                description: "すっごくネバネバしている。",
                hp: 0,
                mp: 0,
                atk: 0,
                def: 0,
                buffs: (datas: Unit.BuffData[]) => {},
                stackable: true,
            },
        ];

        const Table: Map<number, Data> = Datas.reduce((s, x) => s.set(x.id, x), new Map<number, Data>());

        export function keys(): number[] {
            return Array.from(Table.keys());
        }

        export function get(id: number): Data {
            return Table.get(id);
        }
    }
}
