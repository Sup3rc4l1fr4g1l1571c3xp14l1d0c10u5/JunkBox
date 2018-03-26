/// <reference path="item.ts" />
"use strict";

namespace Data {
    export class SaveData {
        private static data: SaveData.ISaveData = {
            version: null,
            itemBox: [],
            money: 10000,
            charactorDatas: [],
            forwardCharactor: null,
            backwardCharactor: null,
            shopStockList: [
                {id:1, condition: "", count: 5},
                {id:2, condition: "", count: 5},
                {id:3, condition: "", count: 5},
                {id:101, condition: "", count: 5},
                {id:102, condition: "", count: 5},
                {id:103, condition: "", count: 5},
                {id:201, condition: "", count: 5},
                {id:202, condition: "", count: 5},
                {id:203, condition: "", count: 5},
                {id:301, condition: "", count: 5},
                {id:302, condition: "", count: 5},
                {id:303, condition: "", count: 5},
                {id:501, condition: "", count: 5},
                {id:502, condition: "", count: 5},
                {id:503, condition: "", count: 5},
                {id:504, condition: "", count: 5},
                {id:505, condition: "", count: 5},
                {id:506, condition: "", count: 5},
                {id:507, condition: "", count: 5},
                {id:508, condition: "", count: 5},
                {id:601, condition: "", count: 5},
            ],
        };

        public static get money() : number { return SaveData.data.money; }
        public static set money(value : number) { SaveData.data.money = value; }

        public static get itemBox() : Item.ItemBoxEntry[] { return SaveData.data.itemBox; }
        public static set itemBox(value : Item.ItemBoxEntry[]) { SaveData.data.itemBox = value; }

        public static get forwardCharactor() : string { return SaveData.data.forwardCharactor; }
        public static set forwardCharactor(value : string) { SaveData.data.forwardCharactor = value; }

        public static get backwardCharactor() : string { return SaveData.data.backwardCharactor; }
        public static set backwardCharactor(value : string) { SaveData.data.backwardCharactor = value; }

        public static get shopStockList() : Item.ItemBoxEntry[] { return SaveData.data.shopStockList; }
        public static set shopStockList(value : Item.ItemBoxEntry[]) { SaveData.data.shopStockList = value; }

        public static findCharactorById(id: string): Player.Data {
            let ret: Player.Data = SaveData.data.charactorDatas.find(x => x.id === id);
            if (ret == null) {
                ret = {
                    id: id,
                    hp: 100,
                    mp: 100,
                    equips: {
                        wepon1: null,
                        armor1: null,
                        armor2: null,
                        accessory1: null,
                        accessory2: null,
                    }
                };
                SaveData.data.charactorDatas.push(ret);
                SaveData.save();
            }
            return ret;
        }

        public static save() : void {
            window.localStorage.setItem("SaveData", JSON.stringify(SaveData.data));
        }

        public static load(): boolean {
            const dataStr: string = window.localStorage.getItem("SaveData");
            if (dataStr == null) {
                return false;
            }
            const temp: SaveData.ISaveData = JSON.parse(dataStr);
            Object.assign(SaveData.data, temp);
            return true;
        }
    }
    export namespace SaveData {
        export interface ISaveData {
            version: string;
            itemBox: Item.ItemBoxEntry[];
            money: number;
            charactorDatas: Player.Data[];
            forwardCharactor: string;
            backwardCharactor: string;
            shopStockList: Item.ItemBoxEntry[];
        };
    }
}