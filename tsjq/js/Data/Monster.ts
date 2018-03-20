"use strict";

namespace Data.Monster {

    interface MonsterConfig {
        id:string;
        name:string;
        status: {
            hp: number;
            atk: number;
            def: number;
            gold: number;
        };
        sprite: SpriteAnimation.ISpriteSheet;
    };

    const monsterConfig: MonsterConfig[] = [
        {
            id: "slime",
            name: "ƒXƒ‰ƒCƒ€",
            status: {
                hp: 5,
                atk: 3,
                def: 1,
                gold: 10,
            },
            sprite: {
                source: {
                    0: "./assets/monster/slime/walk.png"
                },
                sprite: {
                    0: { source: 0, left: 0, top: 0, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    1: { source: 0, left: 24, top: 0, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    2: { source: 0, left: 48, top: 0, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    3: { source: 0, left: 72, top: 0, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    4: { source: 0, left: 0, top: 24, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    5: { source: 0, left: 24, top: 24, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    6: { source: 0, left: 48, top: 24, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    7: { source: 0, left: 72, top: 24, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    8: { source: 0, left: 0, top: 48, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    9: { source: 0, left: 24, top: 48, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    10: { source: 0, left: 48, top: 48, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    11: { source: 0, left: 72, top: 48, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    12: { source: 0, left: 0, top: 72, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    13: { source: 0, left: 24, top: 72, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    14: { source: 0, left: 48, top: 72, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    15: { source: 0, left: 72, top: 72, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    16: { source: 0, left: 0, top: 120, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    17: { source: 0, left: 24, top: 120, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    18: { source: 0, left: 48, top: 120, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                    19: { source: 0, left: 72, top: 120, width: 24, height: 24, offsetX: 0, offsetY: 0 }
                },
                animation: {
                    idle: [
                        { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                    ],
                    move_down: [
                        { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_left: [
                        { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_up: [
                        { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    move_right: [
                        { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                    ],
                    dead: [
                        { sprite: 16, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 17, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 18, time: 0.25, offsetX: 0, offsetY: 0 },
                        { sprite: 19, time: 0.25, offsetX: 0, offsetY: 0 }
                    ]
                }
            }

        }
    ];

    interface MonsterData {
        id:string;
        name:string;
        status: {
            hp: number;
            atk: number;
            def: number;
            gold: number;
        };
        sprite: SpriteAnimation.SpriteSheet;
    }

    const monsterData: Map<string,MonsterData> = new Map<string,MonsterData>();

    async function configToData(config: MonsterConfig,loadStartCallback: () => void, loadEndCallback: () => void): Promise<MonsterData> {
                const id  = config.id;
                const name  = config.name;
                const status  = config.status;
                const sprite  = await SpriteAnimation.SpriteSheet.Create(config.sprite, loadStartCallback, loadEndCallback);
        return {id:id, name:name, status:status, sprite: sprite};
    }

    export async function SetupMonsterData(loadStartCallback: () => void, loadEndCallback: () => void) : Promise<void> {
        const datas = await Promise.all(monsterConfig.map(x => configToData(x, loadStartCallback, loadEndCallback)));
        datas.forEach(x =>monsterData.set(x.id, x));
    }

    export function getMonsterIds(): string[] {
        return Array.from(monsterData.keys());
    }

    export function getMonsterData(id: string): MonsterData {
        return monsterData.get(id);
    }

}
