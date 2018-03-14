/// <reference path="SpriteAnimation.ts" />
"use strict";

namespace Charactor {

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

    export async function loadCharactorConfigFromFile(path: string, loadStartCallback: () => void, loadEndCallback: () => void): Promise<CharactorConfig> {
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

    export abstract class CharactorBase extends SpriteAnimation.Animator implements IPoint {
        public x: number;
        public y: number;

        constructor(x: number, y: number, spriteSheet: SpriteAnimation.SpriteSheet) {
            super(spriteSheet);
            this.x = x;
            this.y = y;
        }
    }

    export class Player extends CharactorBase {
        private static configFilePath: string = "./assets/charactor/charactor.json";

        public static playerConfigs: Map<string, CharactorConfig> = new Map<string, CharactorConfig>();
        public static async loadCharactorConfigs(loadStartCallback: () => void, loadEndCallback: () => void): Promise<void> {
            loadStartCallback();
            const configPaths: string[] = await ajax(Player.configFilePath, "json").then((x) => x.response as string[]);
            loadStartCallback();
            const rootDirectory = getDirectory(Player.configFilePath);
            const configs = await Promise.all(configPaths.map(x => loadCharactorConfigFromFile(rootDirectory + '/' + x, loadStartCallback, loadEndCallback) ));
            Player.playerConfigs = configs.reduce((s, x) => s.set(x.id, x), new Map<string, CharactorConfig>());
            return;
        }

        public charactorConfig: CharactorConfig;

        constructor(config: {
            charactorId: string;
            x: number;
            y: number;
        }) {
            const charactorConfig = Player.playerConfigs.get(config.charactorId);
            super(config.x, config.y, charactorConfig.sprite);
            this.charactorConfig = charactorConfig;

            this.hp = 100;
            this.hpMax = 100;
            this.mp = 100;
            this.mpMax = 100;
            this.gold = 0;

            this.equips = [
                { name: "竹刀", atk: 5, def: 0 },
                { name: "体操着", atk: 0, def: 3 },
                { name: "ブルマ", atk: 0, def: 2 },
            ];
        }

        public hp: number;
        public hpMax: number;

        public mp: number;
        public mpMax: number;

        public gold: number;

        public equips: EquipableItem[];

        public get atk() {
            return this.equips.reduce((s, x) => s += x.atk, 0);
        }
        public get def() {
            return this.equips.reduce((s, x) => s += x.atk, 0);
        }

    }

    interface EquipableItem {
        name:string;
        atk: number;
        def: number;
    }

    export class Monster extends CharactorBase {
        public x: number;
        public y: number;
        public life: number;
        public maxLife: number;
        public atk: number;
        public def: number;

        private static configFilePath: string = "./assets/monster/monster.json";

        public static monsterConfigs: Map<string, CharactorConfig> = new Map<string, CharactorConfig>();
        public static async loadCharactorConfigs(loadStartCallback: () => void, loadEndCallback: () => void): Promise<void> {
            const configPaths: string[] = await ajax(Monster.configFilePath, "json").then((x) => x.response as string[]);
            const rootDirectory = getDirectory(Monster.configFilePath);
            loadStartCallback();
            const configs = await Promise.all(configPaths.map(x => loadCharactorConfigFromFile(rootDirectory + '/' + x, loadStartCallback, loadEndCallback)));
            loadEndCallback();
            Monster.monsterConfigs = configs.reduce((s, x) => s.set(x.id, x), new Map<string, CharactorConfig>());
            return;
        }

        public charactorConfig: CharactorConfig;

        constructor(config: {
            charactorId: string;
            x: number,
            y: number,
            life: number;
            maxLife: number;
            atk: number;
            def: number;
        }) {
            const charactorConfig = Monster.monsterConfigs.get(config.charactorId);
            super(config.x, config.y, charactorConfig.sprite);
            this.charactorConfig = charactorConfig;
            this.life = config.life;
            this.maxLife = config.maxLife;
            this.atk = config.atk;
            this.def = config.def;
        }
    }

}
