"use strict";

namespace Data.Player{

    interface IEquipData {
        wepon1: Data.Item.ItemBoxEntry;
        armor1: Data.Item.ItemBoxEntry;
        armor2: Data.Item.ItemBoxEntry;
        accessory1: Data.Item.ItemBoxEntry;
        accessory2: Data.Item.ItemBoxEntry;
    };

    export interface IPlayerData {
        id: string;
        hp: number;
        mp: number;
        equips: IEquipData;
    }

    export class PlayerData implements IPlayerData {
        public playerData: IPlayerData;
        public get id(): string { return this.playerData.id; }
        public set id(value: string) { this.playerData.id = value; }
        public get hp(): number { return this.playerData.hp; }
        public set hp(value: number) { this.playerData.hp = value; }
        public get mp(): number { return this.playerData.mp; }
        public set mp(value: number) { this.playerData.mp = value; }
        public get equips(): IEquipData { return this.playerData.equips; }
        public set equips(value: IEquipData) { this.playerData.equips = value; }

        constructor(playerData:IPlayerData) {
            this.playerData = playerData;
        }

        public get config(): Data.Charactor.CharactorData {
            return Data.Charactor.getPlayerConfig(this.id);
        }

    }
}
