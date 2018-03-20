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
        { id: 1, name: "�|��", price: 300, kind: ItemKind.Wepon, description: "���Ɨp�Ȃ̂ŏ����{���C", hp: 0, mp: 0, atk: 3, def: 0, effects: (data: any) => { }, stackable: false },
        { id: 2, name: "�S�p�C�v", price: 500, kind: ItemKind.Wepon, description: "�育��ȑ傫���Əd���ň����₷��", hp: 0, mp: 0, atk: 5, def: 0, effects: (data: any) => { }, stackable: false },
        { id: 3, name: "�o�b�g", price: 700, kind: ItemKind.Wepon, description: "�ڎw����O�z�[������", hp: 0, mp: 0, atk: 7, def: 0, effects: (data: any) => { }, stackable: false },

        /* Armor1 */
        { id: 101, name: "����", price: 200, kind: ItemKind.Armor1, description: "�����₷�����h��͂��s��", hp: 0, mp: 0, atk: 0, def: 1, effects: (data: any) => { }, stackable: false },
        { id: 102, name: "����", price: 400, kind: ItemKind.Armor1, description: "�w�Z�w��ł�", hp: 0, mp: 0, atk: 0, def: 2, effects: (data: any) => { }, stackable: false },
        { id: 103, name: "�̑���", price: 600, kind: ItemKind.Armor1, description: "�����������ƕs�]", hp: 0, mp: 0, atk: 0, def: 3, effects: (data: any) => { }, stackable: false },

        /* Armor2 */
        { id: 201, name: "�X�J�[�g", price: 200, kind: ItemKind.Armor2, description: "�G�b�`�ȕ�����ł���", hp: 0, mp: 0, atk: 0, def: 1, effects: (data: any) => { }, stackable: false },
        { id: 202, name: "�u���}", price: 400, kind: ItemKind.Armor2, description: "�΂��o���܂���I", hp: 0, mp: 0, atk: 0, def: 2, effects: (data: any) => { }, stackable: false },
        { id: 203, name: "�Y�{��", price: 600, kind: ItemKind.Armor2, description: "�����ׂ������܂��B", hp: 0, mp: 0, atk: 0, def: 3, effects: (data: any) => { }, stackable: false },

        /* Accessory */
        { id: 301, name: "�w�A�o���h", price: 2000, kind: ItemKind.Accessory, description: "�f�R�I", hp: 0, mp: 0, atk: 0, def: 1, effects: (data: any) => { }, stackable: false },
        { id: 302, name: "���K�l", price: 2000, kind: ItemKind.Accessory, description: "���K�l�͕s�l�C", hp: 0, mp: 0, atk: 0, def: 1, effects: (data: any) => { }, stackable: false },
        { id: 303, name: "�C��", price: 2000, kind: ItemKind.Accessory, description: "�F���������l�X", hp: 0, mp: 0, atk: 0, def: 1, effects: (data: any) => { }, stackable: false },
        { id: 304, name: "����������", price: 0, kind: ItemKind.Accessory, description: "�i�j�J�T���^���E�_�c", hp: 0, mp: 0, atk: 1, def: 1, effects: (data: any) => { }, stackable: false },

        /* Tool */
        { id: 501, name: "�C��������", price: 100, kind: ItemKind.Tool, description: "�󕠎��ɂǂ���", hp: 0, mp: 0, atk: 0, def: 0, effects: (data: any) => { }, stackable: true },
        { id: 502, name: "�v�����O���X", price: 890, kind: ItemKind.Tool, description: "�̕�����҂���������", hp: 0, mp: 0, atk: 0, def: 0, effects: (data: any) => { }, stackable: true },
        { id: 503, name: "�o���e����", price: 931, kind: ItemKind.Tool, description: "���肪�����c", hp: 0, mp: 0, atk: 0, def: 0, effects: (data: any) => { }, stackable: true },
        { id: 504, name: "�T���_�`�L��", price: 1000, kind: ItemKind.Tool, description: "���̃n�[�u�̓_�����Ɓc", hp: 0, mp: 0, atk: 0, def: 0, effects: (data: any) => { }, stackable: true },
    ];

    export function findItemDataById(id: number): ItemData {
        const idx = ItemTable.findIndex(x => x.id == id);
        return ItemTable[idx];
    }
}
