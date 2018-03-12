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
        }
    }

    export class Monster extends CharactorBase {
        public x: number;
        public y: number;
        public life: number;
        public maxLife: number;

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
        }) {
            const charactorConfig = Monster.monsterConfigs.get(config.charactorId);
            super(config.x, config.y, charactorConfig.sprite);
            this.charactorConfig = charactorConfig;
            this.life = config.life;
            this.maxLife = config.maxLife;
        }
    }

}
