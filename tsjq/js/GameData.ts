namespace GameData {
    

    export enum ItemKind {
        Wepon,
        Armor1,
        Armor2,
        Accessory,
        Tool,
        Treasure,
    }

    interface ItemBase {
        name: string;
        price: number;
        kind: ItemKind;
        description: string;
        stackable: boolean;
    }
    export interface WeponData extends ItemBase {
        atk: number;
        def: number;
        condition: string;
    }
    export interface ArmorData extends ItemBase {
        atk: number;
        def: number;
        condition: string;
    }
    export interface AccessoryData extends ItemBase {
        atk: number;
        def: number;
        condition: string;
    }
    export interface ToolData extends ItemBase {
        effect: (...args: any[]) => void;
    }
    export interface TreasureData extends ItemBase {
    }

    export type ItemData = WeponData | ArmorData | AccessoryData | ToolData | TreasureData;
    export type EquipableItemData = WeponData | ArmorData | AccessoryData;


    export interface ItemBoxEntry {
        item: ItemData;
        count : number;
    }

    export const ItemBox : ItemBoxEntry [] = [];
    export let Money : number = 10000;

    //
    //
    //

    interface ICharactorConfigJson {
        id: string;
        name: string;
        sprite: string;
    }

    interface ICharactorConfig {
        id: string;
        name: string;
        sprite: SpriteAnimation.SpriteSheet;
    }

    async function loadCharactorConfigFromFile(path: string, loadStartCallback: () => void, loadEndCallback: () => void): Promise<CharactorConfig> {
        const configDirectory = path.substring(0, path.lastIndexOf("/"));

        // キャラクタの設定ファイルを読み取る
        loadStartCallback();
        const charactorConfigJson: ICharactorConfigJson = await ajax(path, "json").then(x => x.response as ICharactorConfigJson);
        loadEndCallback();

        const spriteSheetPath: string = configDirectory + "/" + charactorConfigJson.sprite;
        const sprite: SpriteAnimation.SpriteSheet = await SpriteAnimation.loadSpriteSheet(spriteSheetPath, loadStartCallback, loadEndCallback);

        return new CharactorConfig({
            id: charactorConfigJson.id,
            name: charactorConfigJson.name,
            sprite: sprite,
            configDirectory: configDirectory
        });
    }

    //
    //
    //

    export class CharactorConfig implements ICharactorConfig {
        constructor( {
            id = "",
            name = "",
            sprite = null,
            configDirectory = "",
        } : {
            id: string;
            name: string;
            sprite: SpriteAnimation.SpriteSheet;
            configDirectory: string;
        }) {
            this.id = id;
            this.name = name;
            this.sprite = sprite;
            this.configDirectory = configDirectory;
        }

        public id: string;
        public name: string;
        public sprite: SpriteAnimation.SpriteSheet;
        public configDirectory: string;
    }

    async function loadCharactorConfigs(target:Map<string,CharactorConfig >, path:string, loadStartCallback: () => void, loadEndCallback: () => void): Promise<void> {
        loadStartCallback();
        const configPaths: string[] = await ajax(path, "json").then((x) => x.response as string[]);
        loadStartCallback();
        const rootDirectory = getDirectory(path);
        const configs = await Promise.all(configPaths.map(x => loadCharactorConfigFromFile(rootDirectory + '/' + x, loadStartCallback, loadEndCallback) ));

        target.clear();
        configs.forEach((x) => target.set(x.id, x));
        return;
    }

    const playerConfigFilePath: string = "./assets/charactor/charactor.json";
    const playerConfigs: Map<string, CharactorConfig> = new Map<string, CharactorConfig>();

    export function getPlayerIds(): string[] {
        const keys :string [] = [];
        for (const [key,value] of playerConfigs) {
            keys.push(key);
        }
        return keys;
    }

    class MonsterConfig extends CharactorConfig {
        public hp: number;
        public mp: number;
        public atk: number;
        public def: number;
        public gold: number;
    }

    const monsterConfigFilePath: string = "./assets/monster/monster.json";
    const monsterConfigs: Map<string, MonsterConfig> = new Map<string, MonsterConfig>();

    export async function loadConfigs(loadStartCallback: () => void, loadEndCallback: () => void) : Promise<void> {
        return Promise.all([
            loadCharactorConfigs(playerConfigs, playerConfigFilePath, loadStartCallback, loadEndCallback),
            loadCharactorConfigs(monsterConfigs, monsterConfigFilePath, loadStartCallback, loadEndCallback),
        ]).then(x => {});
    }

    //
    //
    //
    
    interface IPlayerData {
        id:string ;
        hp: number;
        mp: number;
        equips: {
            wepon1?: EquipableItemData;
            armor1?: EquipableItemData;
            armor2?: EquipableItemData;
            accessory1?: EquipableItemData;
            accessory2?: EquipableItemData;
        };
    }

    export class PlayerData implements IPlayerData{
        public id:string ;
        public hp: number;
        public mp: number;
        public equips: {
            wepon1?: EquipableItemData;
            armor1?: EquipableItemData;
            armor2?: EquipableItemData;
            accessory1?: EquipableItemData;
            accessory2?: EquipableItemData;
        };
        public get config(): CharactorConfig {
            return playerConfigs.get(this.id);
        }
        public toObject(): IPlayerData {
            return {
                id: this.id,
                hp: this.hp,
                mp: this.mp,
                equips: this.equips
            };
        }
        public static fromObject(obj:IPlayerData): PlayerData{
            const data = new PlayerData();
                data.id= obj.id;
                data.hp= obj.hp;
                data.mp= obj.mp;
                data.equips= obj.equips;
        return data;
        }
    }
    const charactorDatas: Map<string, PlayerData> = new Map<string, PlayerData>();

    export function getPlayerData(id: string) : PlayerData {
        let ret = charactorDatas.get(id);
        if (ret == null) {
            let cfg = playerConfigs.get(id);
            if (cfg != null) {
                ret = new PlayerData();
                ret.equips = {};
                ret.hp = 100;
                ret.mp = 100;
                ret.id = id;
                charactorDatas.set(id,ret);
            }
        }
        return ret;
    }
    export function setPlayerData(id: string, data:PlayerData ) : void {
        charactorDatas.set(id, data);
    }

    // 前衛
    export let forwardCharactor:string = null;
    // 後衛
    export let backwardCharactor:string = null;

    //
    //
    //
    
    export function getMonsterConfig(id: string) : MonsterConfig {
        return monsterConfigs.get(id);
    }

    //
    //
    //

    interface SaveData {
        itemBox: ItemBoxEntry[];
        money: number;
        charactorDatas : IPlayerData[];
    }

    export function saveGameData() {
        const charactorDataHash: IPlayerData[] = [];
        charactorDatas.forEach((v,k) => charactorDataHash.push(v.toObject()))
        const data : SaveData = {
            itemBox: ItemBox,
            money: Money,
            charactorDatas: charactorDataHash
        };
        window.localStorage.setItem("GameData", JSON.stringify(data));
    }

    export function loadGameData() : boolean {
        const dataStr :string = window.localStorage.getItem("GameData");
        if (dataStr == null) {
            return false;
        }
        const data : SaveData = JSON.parse(dataStr);
        charactorDatas.clear();
        data.charactorDatas.forEach((v) => {
            charactorDatas.set(v.id, PlayerData.fromObject(v));
        })
        ItemBox.length = 0;
        Array.prototype.push.apply(ItemBox, data.itemBox);
        Money = data.money;
        return true;
    }


}

