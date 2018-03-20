"use strict";

namespace Data.Item {
    export enum ItemKind {
        Wepon,
        Armor1,
        Armor2,
        Accessory,
        Tool,
        Treasure,
    }

    export interface ItemData {
        id: number;
        name: string;
        price: number;
        kind: ItemKind;
        description: string;
        stackable: boolean;
        hp: number;
        mp: number;
        atk: number;
        def: number;
        effects: (...args: any[]) => void;
    }

    export interface ItemBoxEntry {
        id: number;
        condition: string;
        count: number;
    }

    const ItemTable: ItemData[] = [
        /* Wepon */
        { id: 1, name: "竹刀", price: 300, kind: ItemKind.Wepon, description: "授業用なので少しボロイ", hp: 0, mp: 0, atk: 3, def: 0, effects: (data: any) => { }, stackable: false },
        { id: 2, name: "鉄パイプ", price: 500, kind: ItemKind.Wepon, description: "手ごろな大きさと重さで扱いやすい", hp: 0, mp: 0, atk: 5, def: 0, effects: (data: any) => { }, stackable: false },
        { id: 3, name: "バット", price: 700, kind: ItemKind.Wepon, description: "目指せ場外ホームラン", hp: 0, mp: 0, atk: 7, def: 0, effects: (data: any) => { }, stackable: false },

        /* Armor1 */
        { id: 101, name: "水着", price: 200, kind: ItemKind.Armor1, description: "動きやすいが防御はやや不安", hp: 0, mp: 0, atk: 0, def: 1, effects: (data: any) => { }, stackable: false },
        { id: 102, name: "制服", price: 400, kind: ItemKind.Armor1, description: "学校指定です", hp: 0, mp: 0, atk: 0, def: 2, effects: (data: any) => { }, stackable: false },
        { id: 103, name: "体操着", price: 600, kind: ItemKind.Armor1, description: "胸部が窮屈と不評", hp: 0, mp: 0, atk: 0, def: 3, effects: (data: any) => { }, stackable: false },

        /* Armor2 */
        { id: 201, name: "スカート", price: 200, kind: ItemKind.Armor2, description: "エッチな風さんですぅ", hp: 0, mp: 0, atk: 0, def: 1, effects: (data: any) => { }, stackable: false },
        { id: 202, name: "ブルマ", price: 400, kind: ItemKind.Armor2, description: "歳がバレますわ！", hp: 0, mp: 0, atk: 0, def: 2, effects: (data: any) => { }, stackable: false },
        { id: 203, name: "ズボン", price: 600, kind: ItemKind.Armor2, description: "足が細く見えます。", hp: 0, mp: 0, atk: 0, def: 3, effects: (data: any) => { }, stackable: false },

        /* Accessory */
        { id: 301, name: "ヘアバンド", price: 2000, kind: ItemKind.Accessory, description: "デコ！", hp: 0, mp: 0, atk: 0, def: 1, effects: (data: any) => { }, stackable: false },
        { id: 302, name: "メガネ", price: 2000, kind: ItemKind.Accessory, description: "メガネは不人気", hp: 0, mp: 0, atk: 0, def: 1, effects: (data: any) => { }, stackable: false },
        { id: 303, name: "靴下", price: 2000, kind: ItemKind.Accessory, description: "色も長さも様々", hp: 0, mp: 0, atk: 0, def: 1, effects: (data: any) => { }, stackable: false },
        { id: 304, name: "欠けた歯車", price: 0, kind: ItemKind.Accessory, description: "ナニカサレタヨウダ…", hp: 0, mp: 0, atk: 1, def: 1, effects: (data: any) => { }, stackable: false },

        /* Tool */
        { id: 501, name: "イモメロン", price: 100, kind: ItemKind.Tool, description: "空腹時にどうぞ", hp: 0, mp: 0, atk: 0, def: 0, effects: (data: any) => { }, stackable: true },
        { id: 502, name: "プリングルス", price: 890, kind: ItemKind.Tool, description: "歌舞伎役者もおすすめ", hp: 0, mp: 0, atk: 0, def: 0, effects: (data: any) => { }, stackable: true },
        { id: 503, name: "バンテリン", price: 931, kind: ItemKind.Tool, description: "ありがたい…", hp: 0, mp: 0, atk: 0, def: 0, effects: (data: any) => { }, stackable: true },
        { id: 504, name: "サラダチキン", price: 1000, kind: ItemKind.Tool, description: "このハーブはダメかと…", hp: 0, mp: 0, atk: 0, def: 0, effects: (data: any) => { }, stackable: true },
    ];

    export function findItemDataById(id: number): ItemData {
        const idx = ItemTable.findIndex(x => x.id == id);
        return ItemTable[idx];
    }
}
