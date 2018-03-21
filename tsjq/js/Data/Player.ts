"use strict";

namespace Data.Player {

    interface EquipData {
        wepon1: Item.ItemBoxEntry;
        armor1: Item.ItemBoxEntry;
        armor2: Item.ItemBoxEntry;
        accessory1: Item.ItemBoxEntry;
        accessory2: Item.ItemBoxEntry;
    };

    export interface Data {
        id: string;
        hp: number;
        mp: number;
        equips: EquipData;
    }
}
