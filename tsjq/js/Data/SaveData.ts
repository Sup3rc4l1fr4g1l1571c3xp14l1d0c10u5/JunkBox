"use strict";

namespace Data.SaveData {
    
    interface ISaveData {
        ItemBox: Data.Item.ItemBoxEntry[];
        Money: number;
        charactorDatas: Data.Player.IPlayerData[];

        forwardCharactor: string;
        backwardCharactor: string;
    }

    export class SaveData implements ISaveData {
        ItemBox: Data.Item.ItemBoxEntry[] = [];
        Money: number = 10000;
        charactorDatas: Data.Player.IPlayerData[] = [];
        forwardCharactor: string = null;
        backwardCharactor: string = null;
        shopStockList : Data.Item.ItemBoxEntry[] = [
            { id: 1, condition: "", count: 5 },
            { id: 2, condition: "", count: 5 },
            { id: 3, condition: "", count: 5 },
            { id: 101, condition: "", count: 5 },
            { id: 102, condition: "", count: 5 },
            { id: 103, condition: "", count: 5 },
            { id: 201, condition: "", count: 5 },
            { id: 202, condition: "", count: 5 },
            { id: 203, condition: "", count: 5 },
            { id: 301, condition: "", count: 5 },
            { id: 302, condition: "", count: 5 },
            { id: 303, condition: "", count: 5 },
            { id: 501, condition: "", count: 5 },
            { id: 502, condition: "", count: 5 },
            { id: 503, condition: "", count: 5 },
            { id: 504, condition: "", count: 5 },
        ];
        
        findCharactorById(id: string) {
            let ret = this.charactorDatas.find(x => x.id == id);
            if (ret == null) {
                ret =  {
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
                this.charactorDatas.push(ret);
                this.saveGameData();
            }
            return new Data.Player.PlayerData(ret);
        }

        saveGameData() {
            window.localStorage.setItem("SaveData", JSON.stringify(this));
        }

        loadGameData() : boolean {
            const dataStr: string = window.localStorage.getItem("SaveData");
            if (dataStr == null) {
                return false;
            }
            const data: SaveData = JSON.parse(dataStr);
            const result = Object.assign(this, data);
            return true;
        }

    }




}

