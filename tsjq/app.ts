/// <reference path="../../../../../../Program Files (x86)/Microsoft SDKs/TypeScript/2.5/lib.dom.d.ts" />
/// <reference path="../../../../../../Program Files (x86)/Microsoft SDKs/TypeScript/2.5/lib.es2016.d.ts" />
/// <reference path="./js/Game/Array.ts" />
/// <reference path="./js/Game/EventDispatcher.ts" />
/// <reference path="./js/Game/Video.ts" />
/// <reference path="./js/Game/Sound.ts" />
/// <reference path="./js/Game/Input.ts" />
/// <reference path="./js/Game/Timer.ts" />
/// <reference path="./js/Game/Scene.ts" />
/// <reference path="./js/Array2D.ts" />
/// <reference path="./js/Dungeon.ts" />
"use strict";

// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />

enum TurnState {
    WaitInput, // プレイヤーの行動決定(入力待ち)
    PlayerAction,   // プレイヤーの行動実行
    PlayerActionRunning,
    EnemyAI,        // 敵の行動決定
    EnemyAction,    // 敵の行動実行
    EnemyActionRunning,
    Move,           // 移動実行（敵味方同時に行う）
    MoveRunning,    // 移動実行（敵味方同時に行う）
    TurnEnd,   // ターン終了
}

var consolere: Console;

namespace Game {
    export const pmode: boolean = false;
    consolere.log("remote log start");

    // Global Variables
    let video: Video = null;
    let sceneManager: Scene.SceneManager = null;
    let inputDispacher: Input.InputManager = null;
    let timer: Timer.AnimationTimer = null;
    let soundManager: Sound.SoundManager = null;

    //

    export function create(config: {
        title: string;
        screen: { id: string; };
        scene: { [name: string]: (data: any) => IterableIterator<any> }
    }) {
        return new Promise<void>((resolve, reject) => {
            try {
                document.title = config.title;
                video = new Video(config.screen.id);
                video.imageSmoothingEnabled = false;
                sceneManager = new Scene.SceneManager(config.scene);
                timer = new Timer.AnimationTimer();
                inputDispacher = new Input.InputManager();
                soundManager = new Sound.SoundManager();

                resolve();
            } catch (e) {
                reject(e);
            }
        });
    }

    export function getScreen(): Video {
        return video;
    }

    export function getTimer(): Timer.AnimationTimer {
        return timer;
    }

    export function getSceneManager(): Scene.SceneManager {
        return sceneManager;
    }

    export function getInput(): Input.InputManager {
        return inputDispacher;
    }

    export function getSound(): Sound.SoundManager {
        return soundManager;
    }

}

abstract class Animator {

    public dir: number;
    public animDir: number;
    public animFrame: number;
    public offx: number;
    public offy: number;

    constructor(public sprite: [number, number][][], public spriteWidth: number, public spriteHeight: number) {
        this.offx = 0;
        this.offy = 0;
        this.dir = 5;
        this.animDir = 2;
        this.animFrame = 0;
    }

    public setDir(dir: number) {
        if (dir === 0) {
            return;
        }

        this.dir = dir;
        switch (dir) {
            case 1: {
                if (this.animDir === 4) { this.animDir = 4; }
                else if (this.animDir === 2) { this.animDir = 2; }
                else if (this.animDir === 8) { this.animDir = 2; }
                else if (this.animDir === 6) { this.animDir = 4; }
                break;
            }
            case 3: {
                if (this.animDir === 4) { this.animDir = 6; }
                else if (this.animDir === 2) { this.animDir = 2; }
                else if (this.animDir === 8) { this.animDir = 2; }
                else if (this.animDir === 6) { this.animDir = 2; }
                break;
            }
            case 9: {
                if (this.animDir === 4) { this.animDir = 6; }
                else if (this.animDir === 2) { this.animDir = 8; }
                else if (this.animDir === 8) { this.animDir = 8; }
                else if (this.animDir === 6) { this.animDir = 6; }
                break;
            }
            case 7: {
                if (this.animDir === 4) { this.animDir = 4; }
                else if (this.animDir === 2) { this.animDir = 8; }
                else if (this.animDir === 8) { this.animDir = 8; }
                else if (this.animDir === 6) { this.animDir = 4; }
                break;
            }
            case 5: {
                break;
            }
            default: {
                this.animDir = dir;
                break;
            }
        }
    }
    public setAnimation(type: string, rate: number) {
        if (rate > 1) {
            rate = 1;
        }
        if (rate < 0) {
            rate = 0;
        }
        if (type === "move") {
            this.offx = ~~(Array2D.DIR8[this.dir][0] * 24 * rate);
            this.offy = ~~(Array2D.DIR8[this.dir][1] * 24 * rate);
        } else if (type === "action") {
            this.offx = ~~(Array2D.DIR8[this.dir][0] * 12 * Math.sin(rate * Math.PI));
            this.offy = ~~(Array2D.DIR8[this.dir][1] * 12 * Math.sin(rate * Math.PI));
        }
        this.animFrame = ~~(rate * this.sprite[this.animDir].length) % this.sprite[this.animDir].length;
    }
}

class Player extends Animator {

    public charactor: number;
    public x: number;
    public y: number;

    constructor(config: {
        charactor: number;
        x: number;
        y: number;
    }) {
        if (!Game.pmode) {
            super([], 47, 47);
        } else {
            super([], 24, 24);
        }
        this.charactor = config.charactor;
        this.x = config.x;
        this.y = config.y;

        this.changeCharactor(this.charactor);
    }

    private changeCharactor(charactor: number) {
        this.charactor = charactor;
        if (!Game.pmode) {
            const psbasex = (this.charactor % 2) * 752;
            const psbasey = ~~(this.charactor / 2) * 47;
            this.sprite[2] = [[0, 0], [1, 0], [2, 0], [3, 0]].map(xy => [
                psbasex + this.spriteWidth * xy[0], psbasey + this.spriteHeight * xy[1]
            ] as [number, number]);
            this.sprite[4] = [[4, 0], [5, 0], [6, 0], [7, 0]].map(xy => [
                psbasex + this.spriteWidth * xy[0], psbasey + this.spriteHeight * xy[1]
            ] as [number, number]);
            this.sprite[8] = [[8, 0], [9, 0], [10, 0], [11, 0]].map(xy => [
                psbasex + this.spriteWidth * xy[0], psbasey + this.spriteHeight * xy[1]
            ] as [number, number]);
            this.sprite[6] = [[12, 0], [13, 0], [14, 0], [15, 0]].map(xy => [
                psbasex + this.spriteWidth * xy[0], psbasey + this.spriteHeight * xy[1]
            ] as [number, number]);
        } else {
            const animdir: number[] = [4, 8, 6, 2];
            const sprites: [number, number][][] = [];
            for (let i = 0; i < 4; i++) {
                const spr: [number, number][] = [];
                for (let j = 0; j < 4; j++) {
                    spr[j] = [j * 24, i * 24];
                }
                sprites[animdir[i]] = spr;
            }
            this.sprite = sprites;
        }
    }
}

class Monster extends Animator {
    public x: number;
    public y: number;

    constructor(config: {
        _sprite: [number, number][][],
        _sprite_width: number,
        _sprite_height: number,
        x: number,
        y: number
    }) {
        super(config._sprite, config._sprite_width, config._sprite_height);
        this.x = config.x;
        this.y = config.y;
    }
}

class Fade {
    private started: boolean;
    private startTime: number;
    private rate: number;
    private w: number;
    private h: number;
    private mode: string;

    constructor(w: number, h: number) {
        this.startTime = -1;
        this.started = false;
        this.w = w;
        this.h = h;
        this.mode = "";
    }
    public startFadeOut() {
        this.started = true;
        this.startTime = -1;
        this.rate = 0;
        this.mode = "fadeout";
    }
    public startFadeIn() {
        this.started = true;
        this.startTime = -1;
        this.rate = 1;
        this.mode = "fadein";
    }
    public stop() {
        this.started = false;
        this.startTime = -1;
    }
    public update(ms: number) {
        if (this.started === false) {
            return;
        }
        if (this.startTime === -1) {
            this.startTime = ms;
        }
        this.rate = (ms - this.startTime) / 500;
        if (this.rate < 0) {
            this.rate = 0;
        } else if (this.rate > 1) {
            this.rate = 1;
        }
        if (this.mode === "fadein") {
            this.rate = 1 - this.rate;
        }
    }
    public draw() {
        if (this.started) {
            Game.getScreen().fillStyle = `rgba(0,0,0,${this.rate})`;
            Game.getScreen().fillRect(0, 0, this.w, this.h);
        }
    }
}

window.onload = () => {

    function waitTimeout({
        timeout,
        init = () => { },
        start = () => { },
        update = () => { },
        end = () => { }
    }: {
            timeout: number;
            init?: () => void;
            start?: (elapsed: number, ms: number) => void;
            update?: (elapsed: number, ms: number) => void;
            end?: (elapsed: number, ms: number) => void;
        }) {
        let startTime = -1;
        init();
        return (delta: number, ms: number) => {
            if (startTime === -1) {
                startTime = ms;
                start(0, ms);
            }
            const elapsed = ms - startTime;
            if (elapsed >= timeout) {
                end(elapsed, ms);
            } else {
                update(elapsed, ms);
            }
        };
    }

    function waitClick({
            update = () => { },
        start = () => { },
        check = () => true,
        end = () => { }
        }: {
            update?: (elapsed: number, ms: number) => void;
            start?: (elapsed: number, ms: number) => void;
            check?: (x: number, y: number, elapsed: number, ms: number) => boolean;
            end?: (x: number, y: number, elapsed: number, ms: number) => void;
        }) {
        let startTime = -1;
        return (delta: number, ms: number) => {
            if (startTime === -1) {
                startTime = ms;
                start(0, ms);
            }
            const elapsed = ms - startTime;
            if (Game.getInput().isClick()) {
                const pX = Game.getInput().pageX;
                const pY = Game.getInput().pageY;
                if (Game.getScreen().pagePointContainScreen(pX, pY)) {
                    const pos = Game.getScreen().pagePointToScreenPoint(pX, pY);
                    const xx = pos[0];
                    const yy = pos[1];
                    if (check(xx, yy, elapsed, ms)) {
                        end(xx, yy, elapsed, ms);
                        return;
                    }
                }
            }
            update(elapsed, ms);
        };
    }

    Game.create({
        title: "TSJQ",
        screen: {
            id: "glcanvas",
        },
        scene: {
            title: function* (data): IterableIterator<any> {
                // setup
                let showClickOrTap = false;

                const fade = new Fade(Game.getScreen().width, Game.getScreen().height);

                this.draw = () => {
                    const w = Game.getScreen().width;
                    const h = Game.getScreen().height;
                    Game.getScreen().save();
                    Game.getScreen().clearRect(0, 0, w, h);
                    Game.getScreen().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().fillRect(0, 0, w, h);
                    Game.getScreen().drawImage(
                        Game.getScreen().texture("title"),
                        0, 0, 192, 72,
                        w / 2 - 192 / 2, 50, 192, 72
                    );
                    if (showClickOrTap) {
                        Game.getScreen().drawImage(
                            Game.getScreen().texture("title"),
                            0, 72, 168, 24,
                            w / 2 - 168 / 2, h - 50, 168, 24
                        );
                    }
                    fade.draw();
                    Game.getScreen().restore();
                };


                yield waitClick({
                    update: (e, ms) => { showClickOrTap = (~~(ms / 500) % 2) === 0; },
                    check: () => true,
                    end: () => {
                        Game.getSound().reqPlayChannel(0);
                        this.next();
                    }
                });

                yield waitTimeout({
                    timeout: 1000,
                    update: (e, ms) => { showClickOrTap = (~~(ms / 50) % 2) === 0; },
                    end: () => this.next()
                });

                yield waitTimeout({
                    timeout: 500,
                    init: () => { fade.startFadeOut(); },
                    update: (e, ms) => { fade.update(e); showClickOrTap = (~~(ms / 50) % 2) === 0; },
                    end: () => {
                        Game.getSceneManager().push("classroom");
                        this.next();
                    }
                });

            },
            classroom: function* (): IterableIterator<any> {
                let selectedCharactor = -1;
                let selectedCharactorDir = 0;
                let selectedCharactorOffY = 0;
                const fade = new Fade(Game.getScreen().width, Game.getScreen().height);

                this.draw = () => {
                    const w = Game.getScreen().width;
                    const h = Game.getScreen().height;
                    Game.getScreen().save();
                    Game.getScreen().clearRect(0, 0, w, h);
                    Game.getScreen().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().fillRect(0, 0, w, h);

                    // 床
                    for (let y = 0; y < ~~((w + 23) / 24); y++) {
                        for (let x = 0; x < ~~((w + 23) / 24); x++) {
                            Game.getScreen().drawImage(
                                Game.getScreen().texture("mapchip"),
                                0, 0, 24, 24,
                                x * 24, y * 24, 24, 24
                            );
                        }
                    }
                    // 壁
                    for (let y = 0; y < 2; y++) {
                        for (let x = 0; x < ~~((w + 23) / 24); x++) {
                            Game.getScreen().drawImage(
                                Game.getScreen().texture("mapchip"),
                                120,
                                96,
                                24,
                                24,
                                x * 24,
                                y * 24 - 23,
                                24,
                                24
                            );
                        }
                    }
                    // 黒板
                    Game.getScreen().drawImage(
                        Game.getScreen().texture("mapchip"),
                        0,
                        204,
                        72,
                        36,
                        90,
                        -12,
                        72,
                        36
                    );

                    // 各キャラと机
                    for (let y = 0; y < 5; y++) {
                        for (let x = 0; x < 6; x++) {
                            const id = y * 6 + x;
                            Game.getScreen().drawImage(
                                Game.getScreen().texture("charactor"),
                                752 * (id % 2) +
                                ((selectedCharactor !== id) ? 0 : (188 * (selectedCharactorDir % 4))),
                                47 * ~~(id / 2),
                                47,
                                47,
                                12 + x * 36,
                                24 + y * (48 - 7) - ((selectedCharactor !== id) ? 0 : (selectedCharactorOffY)),
                                47,
                                47
                            );

                            Game.getScreen().drawImage(
                                Game.getScreen().texture("mapchip"),
                                72,
                                180,
                                24,
                                24,
                                24 + x * 36,
                                48 + y * (48 - 7),
                                24,
                                24
                            );

                        }
                    }

                    fade.draw();
                    Game.getScreen().restore();
                };

                {
                    Game.getSound().reqPlayChannel(2, true);
                    yield waitTimeout({
                        timeout: 500,
                        init: () => { fade.startFadeIn(); },
                        update: (e) => { fade.update(e); },
                        end: () => {
                            fade.stop();
                            this.next();
                        }
                    });
                }

                yield waitClick({
                    check: (x: number, y: number) => {
                        const xx = ~~((x - 12) / 36);
                        const yy = ~~((y - 24) / (48 - 7));
                        return (0 <= xx && xx < 6 && 0 <= yy && yy < 5);
                    },
                    end: (x: number, y: number) => {
                        Game.getSound().reqPlayChannel(0);
                        const xx = ~~((x - 12) / 36);
                        const yy = ~~((y - 24) / (48 - 7));
                        selectedCharactor = yy * 6 + xx;
                        this.next();
                    }
                });

                yield waitTimeout({
                    timeout: 1800,
                    init: () => {
                        selectedCharactorDir = 0;
                        selectedCharactorOffY = 0;
                    },
                    update: (e) => {
                        if (0 <= e && e < 1600) {
                            // くるくる
                            selectedCharactorDir = ~~(e / 100);
                            selectedCharactorOffY = 0;
                        } else if (1600 <= e && e < 1800) {
                            // ぴょん
                            selectedCharactorDir = 0;
                            selectedCharactorOffY = Math.sin((e - 1600) * Math.PI / 200) * 20;
                        }
                    },
                    end: (e) => { this.next(); }
                });


                yield waitTimeout({
                    timeout: 500,
                    init: () => { fade.startFadeOut(); },
                    update: (e) => { fade.update(e); },
                    end: (e) => { this.next(); }
                });

                const player = new Player({
                    charactor: selectedCharactor,
                    x: 0,
                    y: 0,
                });
                Game.getSound().reqStopChannel(2);
                Game.getSceneManager().pop();
                Game.getSceneManager().push("dungeon", { player: player, floor: 1 });
            },
            dungeon: function* ({ player = null, floor = 0 }: { player: Player, floor: number }): IterableIterator<any> {


                // マップサイズ算出
                const mapChipW = 30 + floor * 3;
                const mapChipH = 30 + floor * 3;

                // マップ自動生成
                const mapchipsL1 = new Array2D(mapChipW, mapChipH);
                const dungeon = Dungeon.generate(
                    mapChipW,
                    mapChipH,
                    (x, y, v) => { mapchipsL1.value(x, y, v ? 0 : 1); });

                // 装飾
                for (let y = 1; y < mapChipH; y++) {
                    for (let x = 0; x < mapChipW; x++) {
                        mapchipsL1.value(x,
                            y - 1,
                            mapchipsL1.value(x, y) === 1 && mapchipsL1.value(x, y - 1) === 0
                                ? 2
                                : mapchipsL1.value(x, y - 1)
                        );
                    }
                }
                const mapchipsL2 = new Array2D(mapChipW, mapChipH);
                for (let y = 0; y < mapChipH; y++) {
                    for (let x = 0; x < mapChipW; x++) {
                        mapchipsL2.value(x, y, (mapchipsL1.value(x, y) === 0) ? 0 : 1);
                    }
                }

                // 部屋シャッフル
                const rooms = dungeon.rooms.shuffle();

                // 開始位置
                const startPos = rooms[0].getCenter();
                player.x = startPos[0];
                player.y = startPos[1];

                // 階段位置
                const stairsPos = rooms[1].getCenter();
                mapchipsL1.value(stairsPos[0], stairsPos[1], 10);

                // モンスター配置
                const monsters = rooms.splice(2).map(x => {
                    const sprites: [number, number][][] = [];
                    if (!Game.pmode) {
                        const animdir: number[] = [2, 4, 8, 6];
                        for (let i = 0; i < 4; i++) {
                            const spr: [number, number][] = [];
                            for (let j = 0; j < 4; j++) {
                                spr[j] = [((i + 1) * 4 + j) * 24, 0];
                            }
                            sprites[animdir[i]] = spr;
                        }
                        this._sprite = sprites;
                    } else {
                        const animdir: number[] = [4, 8, 6, 2];
                        for (let i = 0; i < 4; i++) {
                            const spr: [number, number][] = [];
                            for (let j = 0; j < 4; j++) {
                                spr[j] = [j * 24, i * 24];
                            }
                            sprites[animdir[i]] = spr;
                        }
                        this._sprite = sprites;
                    }
                    return new Monster({
                        _sprite: sprites,
                        _sprite_width: 24,
                        _sprite_height: 24,
                        x: x.getLeft(), //pos[0],
                        y: x.getTop(), //pos[1],
                    });
                });

                const map: Dungeon.DungeonData = new Dungeon.DungeonData({
                    width: mapChipW,
                    height: mapChipW,
                    gridsize: { width: 24, height: 24 },
                    layer: {
                        0: {
                            texture: "mapchip",
                            chip: {
                                1: { x: 48, y: 0 },
                                2: { x: 96, y: 96 },
                                10: { x: 96, y: 0 },
                            },
                            chips: mapchipsL1
                        },
                        1: {
                            texture: "mapchip",
                            chip: {
                                0: { x: 96, y: 72 },
                            },
                            chips: mapchipsL2
                        },
                    },
                });

                // カメラを更新
                map.update({
                    viewpoint: {
                        x: (player.x * map.gridsize.width + player.offx) + map.gridsize.width / 2,
                        y: (player.y * map.gridsize.height + player.offy) + map.gridsize.height / 2
                    },
                    viewwidth: Game.getScreen().width,
                    viewheight: Game.getScreen().height,
                });

                Game.getSound().reqPlayChannel(1, true);

                // assign virtual pad
                const pad = new Game.Input.VirtualStick();

                const pointerdown = (ev: PointerEvent): void => {
                    if (pad.onpointingstart(ev.pointerId)) {
                        const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
                        pad.x = pos[0];
                        pad.y = pos[1];
                    }
                };
                const pointermove = (ev: PointerEvent): void => {
                    const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
                    pad.onpointingmove(ev.pointerId, pos[0], pos[1]);
                };
                const pointerup = (ev: PointerEvent): void => {
                    pad.onpointingend(ev.pointerId);
                };

                const onPointerHook = () => {
                    Game.getInput().on("pointerdown", pointerdown);
                    Game.getInput().on("pointermove", pointermove);
                    Game.getInput().on("pointerup", pointerup);
                    Game.getInput().on("pointerleave", pointerup);
                };
                const offPointerHook = () => {
                    Game.getInput().off("pointerdown", pointerdown);
                    Game.getInput().off("pointermove", pointermove);
                    Game.getInput().off("pointerup", pointerup);
                    Game.getInput().off("pointerleave", pointerup);
                };

                this.suspend = () => {
                    offPointerHook();
                    Game.getSound().reqStopChannel(1);
                };
                this.resume = () => {
                    onPointerHook();
                    Game.getSound().reqPlayChannel(1, true);
                };
                this.leave = () => {
                    offPointerHook();
                    Game.getSound().reqStopChannel(1);
                };

                const updateLighting = (iswalkable: (x: number) => boolean) => {
                    map.clearLighting();
                    PathFinder.propagation({
                        array2D: map.layer[0].chips,
                        sx: player.x,
                        sy: player.y,
                        value: 140,
                        costs: (v) => iswalkable(v) ? 20 : 50,
                        output: map.lighting
                    });
                };

                const fade = new Fade(Game.getScreen().width, Game.getScreen().height);

                this.draw = () => {

                    Game.getScreen().save();
                    Game.getScreen().clearRect(0, 0, Game.getScreen().width, Game.getScreen().height);
                    Game.getScreen().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().fillRect(0, 0, Game.getScreen().width, Game.getScreen().height);

                    map.draw((l: number, cameraLocalPx: number, cameraLocalPy: number) => {
                        if (l === 0) {
                            // 影
                            Game.getScreen().fillStyle = "rgba(0,0,0,0.25)";

                            Game.getScreen().beginPath();
                            Game.getScreen().ellipse(
                                cameraLocalPx,
                                cameraLocalPy + 7,
                                12,
                                3,
                                0,
                                0,
                                Math.PI * 2
                            );
                            Game.getScreen().fill();

                            // モンスター
                            const camera: Dungeon.Camera = map.camera;
                            monsters.forEach((monster) => {
                                const xx = monster.x - camera.chipLeft;
                                const yy = monster.y - camera.chipTop;
                                if (0 <= xx &&
                                    xx < Game.getScreen().width / 24 &&
                                    0 <= yy &&
                                    yy < Game.getScreen().height / 24) {
                                    const adir = monster.animDir;
                                    const af = monster.animFrame;
                                    Game.getScreen().drawImage(
                                        Game.getScreen().texture("monster"),
                                        monster.sprite[adir][af][0],
                                        monster.sprite[adir][af][1],
                                        monster.spriteWidth,
                                        monster.spriteHeight,
                                        xx * monster.spriteWidth + camera.chipOffX + monster.offx,
                                        yy * monster.spriteWidth + camera.chipOffY + monster.offy,
                                        monster.spriteWidth,
                                        monster.spriteHeight
                                    );
                                }
                            });

                            const animf: number = player.animFrame;
                            const playersprite: [number, number][] = player.sprite[player.animDir];

                            // キャラクター
                            Game.getScreen().drawImage(
                                Game.getScreen().texture("charactor"),
                                playersprite[animf][0],
                                playersprite[animf][1],
                                player.spriteWidth,
                                player.spriteHeight,
                                cameraLocalPx - player.spriteWidth / 2,
                                cameraLocalPy - player.spriteHeight / 2 - 12,
                                player.spriteWidth,
                                player.spriteHeight
                            );

                        }
                    });


                    // フェード
                    fade.draw();
                    Game.getScreen().restore();

                    // バーチャルジョイスティックの描画
                    if (pad.isTouching) {
                        Game.getScreen().fillStyle = "rgba(255,255,255,0.25)";
                        Game.getScreen().beginPath();
                        Game.getScreen().ellipse(
                            pad.x,
                            pad.y,
                            pad.radius * 1.2,
                            pad.radius * 1.2,
                            0,
                            0,
                            Math.PI * 2
                        );
                        Game.getScreen().fill();
                        Game.getScreen().beginPath();
                        Game.getScreen().ellipse(
                            pad.x + pad.cx,
                            pad.y + pad.cy,
                            pad.radius,
                            pad.radius,
                            0,
                            0,
                            Math.PI * 2
                        );
                        Game.getScreen().fill();
                    }

                };

                yield waitTimeout({
                    timeout: 500,
                    init: () => { fade.startFadeIn(); },
                    update: (e) => { fade.update(e); updateLighting((v: number) => v === 1 || v === 10); },
                    end: (e) => { this.next(); }
                });

                onPointerHook();


                // ターンの状態（フェーズ）
                const turnStateStack: [TurnState, any][] = [[TurnState.WaitInput, null]];

                let playerTactics: any = {};
                let monstersTactics: any[] = [];
                yield (delta: number, ms: number) => {
                    switch (turnStateStack[0][0]) {
                        case TurnState.WaitInput:
                            {
                                // キー入力待ち
                                if (pad.isTouching === false || pad.distance <= 0.4) {
                                    player.setAnimation('idle', 0);
                                    break;
                                }

                                // キー入力されたのでプレイヤーの移動方向(5)は移動しない。

                                const playerMoveDir = pad.dir8;

                                // 「行動(Action)」と「移動(Move)」の識別を行う

                                // 移動先が侵入不可能の場合は待機とする
                                const [ox, oy] = Array2D.DIR8[playerMoveDir];
                                if (map.layer[0].chips.value(player.x + ox, player.y + oy) !== 1 && map.layer[0].chips.value(player.x + ox, player.y + oy) !== 10) {
                                    player.setDir(playerMoveDir);
                                    break;
                                }

                                // 移動先に敵がいる場合は「行動(Action)」、いない場合は「移動(Move)」
                                const targetMonster =
                                    monsters.findIndex((monster) => (monster.x === player.x + ox) &&
                                        (monster.y === player.y + oy));
                                if (targetMonster !== -1) {
                                    // 移動先に敵がいる＝「行動(Action)」

                                    playerTactics = {
                                        type: "action",
                                        moveDir: playerMoveDir,
                                        targetMonster: playerMoveDir,
                                        startTime: ms,
                                        actionTime: 250
                                    };

                                    // プレイヤーの行動、敵の行動の決定、敵の行動処理、移動実行の順で行う
                                    turnStateStack.unshift(
                                        [TurnState.PlayerAction, null],
                                        [TurnState.EnemyAI, null],
                                        [TurnState.EnemyAction, 0],
                                        [TurnState.Move, null],
                                        [TurnState.TurnEnd, null]
                                    );
                                    break;
                                } else {
                                    // 移動先に敵はいない＝「移動(Move)」

                                    playerTactics = {
                                        type: "move",
                                        moveDir: playerMoveDir,
                                        startTime: ms,
                                        actionTime: 250
                                    };

                                    // 敵の行動の決定、移動実行、敵の行動処理、の順で行う。
                                    turnStateStack.unshift(
                                        [TurnState.EnemyAI, null],
                                        [TurnState.Move, null],
                                        [TurnState.EnemyAction, 0],
                                        [TurnState.TurnEnd, null]
                                    );
                                    break;
                                }

                            }

                        case TurnState.PlayerAction: {
                            // プレイヤーの行動開始
                            turnStateStack[0][0] = TurnState.PlayerActionRunning;
                            player.setDir(playerTactics.moveDir);
                            player.setAnimation('action', 0);
                            break;
                        }
                        case TurnState.PlayerActionRunning: {
                            // プレイヤーの行動中
                            const rate = (ms - playerTactics.startTime) / playerTactics.actionTime;
                            player.setAnimation('action', rate);
                            if (rate >= 1) {
                                // プレイヤーの行動終了
                                turnStateStack.shift();
                            }
                            break;
                        }
                        case TurnState.EnemyAI: {
                            // 敵の行動の決定

                            // プレイヤーが移動する場合、移動先にいると想定して敵の行動を決定する
                            let px = player.x;
                            let py = player.y;
                            if (playerTactics.type === "move") {
                                const off = Array2D.DIR8[playerTactics.moveDir];
                                px += off[0];
                                py += off[1];
                            }

                            monstersTactics = monsters.map((monster) => {
                                const dx = px - monster.x;
                                const dy = py - monster.y;
                                if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) {
                                    // 移動先のプレイヤー位置は現在位置に隣接しているので、行動(Action)を選択
                                    const dir = Array2D.DIR8.findIndex((x) => x[0] === dx && x[1] === dy);
                                    return {
                                        type: "action",
                                        moveDir: dir,
                                        startTime: 0,
                                        actionTime: 250
                                    };
                                } else {
                                    // 移動先のプレイヤー位置は現在位置に隣接していないので、移動(Move)を選択
                                    // とりあえず軸合わせで動く

                                    const cands = [
                                        [Math.sign(dx), Math.sign(dy)],
                                        (Math.abs(dx) > Math.abs(dy)) ? [0, Math.sign(dy)] : [Math.sign(dx), 0],
                                        (Math.abs(dx) > Math.abs(dy)) ? [Math.sign(dx), 0] : [0, Math.sign(dy)],
                                    ];

                                    for (let i = 0; i < 3; i++) {
                                        const [cx, cy] = cands[i];
                                        if (map.layer[0].chips.value(monster.x + cx, monster.y + cy) === 1 || map.layer[0].chips.value(monster.x + cx, monster.y + cy) === 10) {
                                            const dir = Array2D.DIR8.findIndex((x) => x[0] === cx && x[1] === cy);
                                            return {
                                                type: "move",
                                                moveDir: dir,
                                                startTime: ms,
                                                actionTime: 250
                                            };
                                        }
                                    }
                                    return {
                                        type: "idle",
                                        moveDir: 5,
                                        startTime: ms,
                                        actionTime: 250
                                    };


                                }
                            });
                            // 敵の行動の決定の終了
                            turnStateStack.shift();
                            break;
                        }
                        case TurnState.EnemyAction: {
                            // 敵の行動開始
                            let enemyId = turnStateStack[0][1];
                            while (enemyId < monstersTactics.length) {
                                if (monstersTactics[enemyId].type !== "action") {
                                    enemyId++;
                                } else {
                                    break;
                                }
                            }
                            if (enemyId < monstersTactics.length) {
                                monstersTactics[enemyId].startTime = ms;
                                monsters[enemyId].setDir(monstersTactics[enemyId].moveDir);
                                monsters[enemyId].setAnimation('action', 0);
                                turnStateStack[0][0] = TurnState.EnemyActionRunning;
                                turnStateStack[0][1] = enemyId;
                            } else {
                                // もう動かす敵がいない
                                turnStateStack.shift();
                            }

                            break;
                        }
                        case TurnState.EnemyActionRunning: {
                            // 敵の行動中
                            const enemyId = turnStateStack[0][1];

                            const rate = (ms - monstersTactics[enemyId].startTime) / monstersTactics[enemyId].actionTime;
                            monsters[enemyId].setAnimation('action', rate);
                            if (rate >= 1) {
                                // 行動終了。次の敵へ
                                turnStateStack[0][0] = TurnState.EnemyAction;
                                turnStateStack[0][1] = enemyId + 1;
                            }
                            break;
                        }
                        case TurnState.Move: {
                            // 移動開始
                            turnStateStack[0][0] = TurnState.MoveRunning;
                            monstersTactics.forEach((monsterTactic, i) => {
                                if (monsterTactic.type === "move") {
                                    monsters[i].setDir(monsterTactic.moveDir);
                                    monsters[i].setAnimation('move', 0);
                                    monstersTactics[i].startTime = ms;
                                }
                            });
                            if (playerTactics.type === "move") {
                                player.setDir(playerTactics.moveDir);
                                player.setAnimation('move', 0);
                                playerTactics.startTime = ms;
                            }
                            break;
                        }
                        case TurnState.MoveRunning: {
                            // 移動実行
                            let finish = true;
                            monstersTactics.forEach((monsterTactic, i) => {
                                if (monsterTactic.type === "move") {
                                    const rate = (ms - monsterTactic.startTime) / monsterTactic.actionTime;
                                    monsters[i].setDir(monsterTactic.moveDir);
                                    monsters[i].setAnimation('move', rate);
                                    if (rate < 1) {
                                        finish = false; // 行動終了していないフラグをセット
                                    }
                                }
                            });
                            if (playerTactics.type === "move") {
                                const rate = (ms - playerTactics.startTime) / playerTactics.actionTime;
                                player.setDir(playerTactics.moveDir);
                                player.setAnimation('move', rate);
                                if (rate < 1) {
                                    finish = false; // 行動終了していないフラグをセット
                                }
                            }
                            if (finish) {
                                // 行動終了
                                turnStateStack.shift();

                                monstersTactics.forEach((monsterTactic, i) => {
                                    if (monsterTactic.type === "move") {
                                        monsters[i].x += Array2D.DIR8[monsterTactic.moveDir][0];
                                        monsters[i].y += Array2D.DIR8[monsterTactic.moveDir][1];
                                        monsters[i].offx = 0;
                                        monsters[i].offy = 0;
                                    }
                                });
                                if (playerTactics.type === "move") {
                                    player.x += Array2D.DIR8[playerTactics.moveDir][0];
                                    player.y += Array2D.DIR8[playerTactics.moveDir][1];
                                    player.offx = 0;
                                    player.offy = 0;
                                }

                                // 現在位置のマップチップを取得
                                const chip = map.layer[0].chips.value(~~player.x, ~~player.y);
                                if (chip === 10) {
                                    // 階段なので次の階層に移動させる。
                                    this.next("nextfloor");
                                }

                            }
                            break;
                        }
                        case TurnState.TurnEnd: {
                            // ターン終了
                            turnStateStack.shift();
                            break;
                        }
                    }


                    // カメラを更新
                    map.update({
                        viewpoint: {
                            x: (player.x * map.gridsize.width + player.offx) + map.gridsize.width / 2,
                            y: (player.y * map.gridsize.height + player.offy) + map.gridsize.height / 2
                        },
                        viewwidth: Game.getScreen().width,
                        viewheight: Game.getScreen().height,
                    }
                    );

                    updateLighting((v: number) => v === 1 || v === 10);

                    if (Game.getInput().isClick() && Game.getScreen().pagePointContainScreen(Game.getInput().pageX, Game.getInput().pageY)) {
                        Game.getSceneManager().push("mapview", { map: map, player: player });
                    }

                };
                Game.getSound().reqPlayChannel(3);

                yield waitTimeout({
                    timeout: 500,
                    init: () => { fade.startFadeOut(); },
                    update: (e) => { fade.update(e); updateLighting((v: number) => v === 1 || v === 10); },
                    end: (e) => { this.next(); }
                });

                yield waitTimeout({
                    timeout: 500,
                    end: (e) => { floor++; this.next(); }
                });

                Game.getSceneManager().pop();
                Game.getSceneManager().push("dungeon", { player: player, floor: floor });

            },
            mapview: function* (data) {
                this.draw = () => {
                    Game.getScreen().save();
                    Game.getScreen().clearRect(0, 0, Game.getScreen().width, Game.getScreen().height);
                    Game.getScreen().fillStyle = "rgb(0,0,0)";
                    Game.getScreen().fillRect(0, 0, Game.getScreen().width, Game.getScreen().height);

                    const offx = ~~((Game.getScreen().width - data.map.width * 5) / 2);
                    const offy = ~~((Game.getScreen().height - data.map.height * 5) / 2);

                    // ミニマップを描画
                    for (let y = 0; y < data.map.height; y++) {
                        for (let x = 0; x < data.map.width; x++) {
                            const chip = data.map.layer[0].chips.value(x, y);
                            let color = "rgb(52,12,0)";
                            switch (chip) {
                                case 1:
                                    color = "rgb(179,116,39)";
                                    break;
                                case 10:
                                    color = "rgb(255,0,0)";
                                    break;
                            }
                            Game.getScreen().fillStyle = color;
                            Game.getScreen().fillRect(offx + x * 5, offy + y * 5, 5, 5);

                            let light = 1 - data.map.visibled.value(x, y) / 100;
                            if (light > 1) {
                                light = 1;
                            } else if (light < 0) {
                                light = 0;
                            }
                            Game.getScreen().fillStyle = `rgba(0,0,0,${light})`;
                            Game.getScreen().fillRect(offx + x * 5, offy + y * 5, 5, 5);
                        }
                    }

                    Game.getScreen().fillStyle = "rgb(0,255,0)";
                    Game.getScreen().fillRect(offx + data.player.x * 5, offy + data.player.y * 5, 5, 5);
                    Game.getScreen().restore();
                };
                yield waitClick({ end: () => this.next() });
                Game.getSceneManager().pop();
            },
        }
    }).then(() => {
        let anim = 0;
        const update = (ms: number) => {
            Game.getScreen().save();
            Game.getScreen().clearRect(0, 0, Game.getScreen().width, Game.getScreen().height);
            Game.getScreen().fillStyle = "rgb(255,255,255)";
            Game.getScreen().fillRect(0, 0, Game.getScreen().width, Game.getScreen().height);

            const n = ~(ms / 50);
            Game.getScreen().translate(Game.getScreen().width / 2, Game.getScreen().height / 2);
            Game.getScreen().rotate(n * Math.PI / 4);
            for (let i = 0; i < 8; i++) {
                const g = (i * 32);
                Game.getScreen().save();
                Game.getScreen().rotate(i * Math.PI / 4);
                Game.getScreen().fillStyle = `rgb(${g},${g},${g})`;
                Game.getScreen().fillRect(-5, -50, 10, 25);
                Game.getScreen().restore();
            }
            Game.getScreen().restore();
            anim = requestAnimationFrame(update.bind(this));
        };
        anim = requestAnimationFrame(update.bind(this));

        return Promise.all([
            Game.getScreen().loadImage({
                title: "./assets/title.png",
                mapchip: "./assets/mapchip.png",
                charactor: "./assets/charactor.png",
                monster: "./assets/monster.png"
            }),
            Game.getSound().loadSoundsToChannel({
                0: "./assets/title.mp3",
                1: "./assets/dungeon.mp3",
                2: "./assets/classroom.mp3",
                3: "./assets/kaidan.mp3"
            }),
            new Promise<void>((resolve, reject) => setTimeout(() => resolve(), 5000))
        ]).then(() => {
            cancelAnimationFrame(anim);
        });
    }).then(() => {
        Game.getSceneManager().push("title");
        Game.getTimer().on((delta, now, id) => {
            Game.getInput().endCapture();
            Game.getSceneManager().update(delta, now);
            Game.getInput().startCapture();
            Game.getSound().playChannel();
            Game.getSceneManager().draw();
        });
        Game.getTimer().start();
    });
};
