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

module Game {
    consolere.log("remote log start");

    // Global Variables
    var video: Video = null;
    var sceneManager: Scene.SceneManager = null;
    var inputDispacher: Input.InputManager = null;
    var timer: Timer.AnimationTimer = null;
    var soundManager: Sound.SoundManager = null;

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
    };

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

class Player {
    _sprite: {
        down: number[][];
        left: number[][];
        up: number[][];
        right: number[][];
    };
    _sprite_width: number;
    _sprite_height: number;

    public charactor: number;
    public x: number;
    public y: number;
    offx: number;
    offy: number;
    dir: string;
    movemode: string;
    movems: number;
    anim: number;
    animstep: number;
    movestep: number;

    constructor(config: {
        charactor: number;
        x: number;
        y: number;
    }) {
        this.charactor = config.charactor;
        this.x = config.x;
        this.y = config.y;
        this.offx = 0;
        this.offy = 0;
        this.dir = "down";
        this.movemode = "idle";
        this.movems = 0;
        this.anim = 0;

        // 移動時間とアニメーション時間(どちらもms単位)
        // ダッシュ相当の設定
        //this.movestep = 150;
        //this.animstep = 150;
        // 通常の設定
        this.movestep = 250;
        this.animstep = 250;

        this.changeCharactor(this.charactor);
    }

    changeCharactor(charactor: number) {
        this.charactor = charactor;

        var psbasex = (this.charactor % 2) * 752;
        var psbasey = ~~(this.charactor / 2) * 47;
        this._sprite_width = 47;
        this._sprite_height = 47;
        this._sprite = {
            down: [[0, 0], [1, 0], [2, 0], [3, 0]].map(xy => [
                psbasex + this._sprite_width * xy[0], psbasey + this._sprite_height * xy[1]
            ]),
            left: [[4, 0], [5, 0], [6, 0], [7, 0]].map(xy => [
                psbasex + this._sprite_width * xy[0], psbasey + this._sprite_height * xy[1]
            ]),
            up: [[8, 0], [9, 0], [10, 0], [11, 0]].map(xy => [
                psbasex + this._sprite_width * xy[0], psbasey + this._sprite_height * xy[1]
            ]),
            right: [[12, 0], [13, 0], [14, 0], [15, 0]].map(xy => [
                psbasex + this._sprite_width * xy[0], psbasey + this._sprite_height * xy[1]
            ]),
        };
    }

    update(delta: number,
        ms: number,
        opts: { moveDir: string; moveCheckCallback: (player: Player, x: number, y: number) => boolean }) {

        if (this.movemode == "idle") {
            switch (opts.moveDir) {
                case "left":
                    this.dir = "left";
                    if (opts.moveCheckCallback(this, this.x - 1, this.y)) {
                        this.movemode = "move-left";
                        this.movems = this.movems == 0 ? this.movestep : this.movems;
                    } else {
                        this.anim = 0;
                        this.movems = 0;
                    }
                    break;
                case "up":
                    this.dir = "up";
                    if (opts.moveCheckCallback(this, this.x, this.y - 1)) {
                        this.movemode = "move-up";
                        this.movems = this.movems == 0 ? this.movestep : this.movems;
                    } else {
                        this.anim = 0;
                        this.movems = 0;
                    }
                    break;
                case "right":
                    this.dir = "right";
                    if (opts.moveCheckCallback(this, this.x + 1, this.y)) {
                        this.movemode = "move-right";
                        this.movems = this.movems == 0 ? this.movestep : this.movems;
                    } else {
                        this.anim = 0;
                        this.movems = 0;
                    }
                    break;
                case "down":
                    this.dir = "down";
                    if (opts.moveCheckCallback(this, this.x, this.y + 1)) {
                        this.movemode = "move-down";
                        this.movems = this.movems == 0 ? this.movestep : this.movems;
                    } else {
                        this.anim = 0;
                        this.movems = 0;
                    }
                    break;
                default:
                    this.movemode = "idle";
                    this.anim = 0;
                    this.movems = 0;
                    return true;
            }
        } else if (this.movemode == "move-right") {

            this.movems -= delta;
            this.anim += delta;
            if (this.movems <= 0) {
                this.x += 1;
                this.movemode = "idle";
                this.movems += this.movestep;
            }
            this.offx = 24 * (1 - this.movems / this.movestep);
        } else if (this.movemode == "move-left") {
            this.movems -= delta;
            this.anim += delta;
            if (this.movems <= 0) {
                this.x -= 1;
                this.movemode = "idle";
                this.movems += this.movestep;
            }
            this.offx = -24 * (1 - this.movems / this.movestep);
        } else if (this.movemode == "move-down") {
            this.movems -= delta;
            this.anim += delta;
            if (this.movems <= 0) {
                this.y += 1;
                this.movemode = "idle";
                this.movems += this.movestep;
            }
            this.offy = 24 * (1 - this.movems / this.movestep);
        } else if (this.movemode == "move-up") {
            this.movems -= delta;
            this.anim += delta;
            if (this.movems <= 0) {
                this.y -= 1;
                this.movemode = "idle";
                this.movems += this.movestep;
            }
            this.offy = -24 * (1 - this.movems / this.movestep);
        }
        if (this.anim >= this.animstep * 4) {
            this.anim -= this.animstep * 4;
        }

    }

    getAnimFrame() {
        return ~~(((~~this.anim) + this.animstep - 1) / this.animstep) % 4;
    }
}

class Monster {
    public x: number;
    public y: number;
    public dx: number;
    public dy: number;
    public anim: number;
    public startms: number;
    public draw: (x: number, y: number, offx: number, offy: number) => void;
    public update: (delta: number, ms: number) => void;

    constructor(config: {
        x: number,
        y: number,
        anim: number,
        startms: number,
        draw: (x: number, y: number, offx: number, offy: number) => void,
        update: (delta: number, ms: number) => void
    }) {
        this.x = config.x;
        this.y = config.y;
        this.anim = config.anim;
        this.startms = config.startms;
        this.draw = config.draw.bind(this);
        this.update = config.update.bind(this);
    }
}

class Fade {
    started: boolean;
    startTime: number;
    rate: number;
    w: number;
    h: number;
    mode: string;

    constructor(w: number, h: number) {
        this.startTime = -1;
        this.started = false;
        this.w = w;
        this.h = h;
        this.mode = "";
    }
    startFadeOut() {
        this.started = true;
        this.startTime = -1;
        this.rate = 0;
        this.mode = "fadeout";
    }
    startFadeIn() {
        this.started = true;
        this.startTime = -1;
        this.rate = 1;
        this.mode = "fadein";
    }
    stop() {
        this.started = false;
        this.startTime = -1;
    }
    update(ms: number) {
        if (this.started == false) {
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
    draw() {
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
        var startTime = -1;
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
    };

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
        var startTime = -1;
        return (delta: number, ms: number) => {
            if (startTime === -1) {
                startTime = ms;
                start(0, ms);
            }
            var elapsed = ms - startTime;
            if (Game.getInput().isClick()) {
                var pX = Game.getInput().pageX;
                var pY = Game.getInput().pageY;
                if (Game.getScreen().pagePointContainScreen(pX, pY)) {
                    const pos = Game.getScreen().pagePointToScreenPoint(pX, pY);
                    var xx = pos[0];
                    var yy = pos[1];
                    if (check(xx, yy, elapsed, ms)) {
                        end(xx, yy, elapsed, ms);
                        return;
                    }
                }
            }
            update(elapsed, ms);
        };
    };


    Game.create({
        title: "TSJQ",
        screen: {
            id: "glcanvas",
        },
        scene: {
            title: function* (data) {
                console.log("state start", data);
                // setup 
                var show_click_or_tap = false;

                var fade = new Fade(Game.getScreen().width, Game.getScreen().height);

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
                    if (show_click_or_tap) {
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
                    update: (e, ms) => { show_click_or_tap = (~~(ms / 500) % 2) === 0; },
                    check: () => true,
                    end: () => {
                        Game.getSound().reqPlayChannel(0);
                        this.next();
                    }
                });

                yield waitTimeout({
                    timeout: 1000,
                    update: (e, ms) => { show_click_or_tap = (~~(ms / 50) % 2) === 0; },
                    end: () => this.next()
                });

                yield waitTimeout({
                    timeout: 500,
                    init: () => { fade.startFadeOut(); },
                    update: (e, ms) => { fade.update(e); show_click_or_tap = (~~(ms / 50) % 2) === 0; },
                    end: () => {
                        Game.getSceneManager().push("classroom");
                        this.next();
                    }
                });

            },
            classroom: function* () {
                var selectedCharactor = -1;
                var selectedCharactorDir = 0;
                var selectedCharactorOffY = 0;
                var fade = new Fade(Game.getScreen().width, Game.getScreen().height);

                this.draw = () => {
                    var w = Game.getScreen().width;
                    var h = Game.getScreen().height;
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
                    for (var y = 0; y < 5; y++) {
                        for (var x = 0; x < 6; x++) {
                            var id = y * 6 + x;
                            Game.getScreen().drawImage(
                                Game.getScreen().texture("charactor"),
                                752 * (id % 2) +
                                ((selectedCharactor !== id) ? 0 : (188 * (selectedCharactorDir % 4))),
                                47 * ~~(id / 2),
                                47,
                                47,
                                12 + x * 36,
                                24 + y * (48 - 7) - ((selectedCharactor != id) ? 0 : (selectedCharactorOffY)),
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
                console.log("dungeon");
            },
            dungeon: function* (param) {


                // マップサイズ算出
                const mapChipW = 30 + param.floor * 3;
                const mapChipH = 30 + param.floor * 3;

                // マップ自動生成
                const mapchipsL1 = new Array2D(mapChipW, mapChipH);
                const dungeon = Dungeon.Generator.create(
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
                var rooms = dungeon._rooms.shuffle();

                // 開始位置
                var startPos = rooms[0].getCenter();
                param.player.x = startPos[0];
                param.player.y = startPos[1];

                // 階段位置
                var stairsPos = rooms[1].getCenter();
                mapchipsL1.value(stairsPos[0], stairsPos[1], 10);

                // モンスター配置
                var monsters = rooms.splice(2).map(x => {
                    return new Monster({
                        x: x.getLeft(), //pos[0],
                        y: x.getTop(), //pos[1],
                        anim: 0,
                        startms: -1,
                        update(delta, ms) {
                            if (this.startms == -1) {
                                this.startms = ms;
                            }
                            this.anim = ~~((ms - this.startms) / 160) % 4;
                        },
                        draw(x: number, y: number, offx: number, offy: number) {
                            const xx = this.x - x;
                            const yy = this.y - y;
                            if (0 <= xx &&
                                xx < Game.getScreen().width / 24 &&
                                0 <= yy &&
                                yy < Game.getScreen().height / 24) {
                                Game.getScreen().drawImage(
                                    Game.getScreen().texture("monster"),
                                    this.anim * 24,
                                    0,
                                    24,
                                    24,
                                    xx * 24 + offx + 12 + this.dx,
                                    yy * 24 + offy + 12 + this.dy,
                                    24,
                                    24
                                );
                            }

                        }
                    });
                });

                var map = new Dungeon.DungeonData({
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
                        x: (param.player.x * 24 + param.player.offx) + param.player._sprite_width / 2,
                        y: (param.player.y * 24 + param.player.offy) + param.player._sprite_height / 2
                    },
                    viewwidth: Game.getScreen().width,
                    viewheight: Game.getScreen().height,
                });

                Game.getSound().reqPlayChannel(1, true);

                // assign virtual pad
                var pad = new Game.Input.VirtualStick();

                var pointerdown = (ev: PointerEvent): void => {
                    if (pad.onpointingstart(ev.pointerId)) {
                        const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
                        pad.x = pos[0];
                        pad.y = pos[1];
                    }
                };
                var pointermove = (ev: PointerEvent): void => {
                    const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
                    pad.onpointingmove(ev.pointerId, pos[0], pos[1]);
                };
                var pointerup = (ev: PointerEvent): void => {
                    pad.onpointingend(ev.pointerId);
                };

                var onPointerHook = () => {
                    Game.getInput().on("pointerdown", pointerdown);
                    Game.getInput().on("pointermove", pointermove);
                    Game.getInput().on("pointerup", pointerup);
                    Game.getInput().on("pointerleave", pointerup);
                };
                var offPointerHook = () => {
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

                var update_lighting = (iswalkable) => {
                    var calc_lighting = (x, y, power, dec, dec2, setted) => {
                        if (0 > x || x >= map.width) {
                            return;
                        }
                        if (0 > y || y >= map.height) {
                            return;
                        }
                        if (power <= map.lighting.value(x, y)) {
                            return;
                        }

                        setted[x + "," + y] = true;
                        map.lighting.value(x, y, Math.max(map.lighting.value(x, y), power));
                        map.visibled.value(x,
                            y,
                            Math.max(map.lighting.value(x, y), map.visibled.value(x, y)));
                        if (!iswalkable(x, y)) {
                            power -= dec2;
                        } else {
                            power -= dec;
                        }

                        calc_lighting(x + 0, y - 1, power, dec, dec2, setted);
                        calc_lighting(x - 1, y + 0, power, dec, dec2, setted);
                        calc_lighting(x + 1, y + 0, power, dec, dec2, setted);
                        calc_lighting(x + 0, y + 1, power, dec, dec2, setted);

                    };
                    map.clearLighting();
                    calc_lighting(param.player.x, param.player.y, 140, 20, 50, {});
                };

                var fade = new Fade(Game.getScreen().width, Game.getScreen().height);

                this.draw = () => {

                    Game.getScreen().save();
                    Game.getScreen().clearRect(0, 0, Game.getScreen().width, Game.getScreen().height);
                    Game.getScreen().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().fillRect(0, 0, Game.getScreen().width, Game.getScreen().height);

                    map.draw((l, cameraLocalPx, cameraLocalPy) => {
                        if (l == 0) {
                            const animf = param.player.getAnimFrame();
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
                            var camera: Dungeon.Camera = map.camera;
                            monsters.forEach((x) => x.draw(camera.chipX,
                                camera.chipY,
                                camera.chipOffX,
                                camera.chipOffY));

                            // キャラクター
                            Game.getScreen().drawImage(
                                Game.getScreen().texture("charactor"),
                                param.player._sprite[param.player.dir][animf][0],
                                param.player._sprite[param.player.dir][animf][1],
                                param.player._sprite_width,
                                param.player._sprite_height,
                                cameraLocalPx - param.player._sprite_width / 2,
                                cameraLocalPy - param.player._sprite_height / 2 - 12,
                                param.player._sprite_width,
                                param.player._sprite_height
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
                    update: (e) => { fade.update(e); update_lighting((x, y) => ((map.layer[0].chips.value(x, y) === 1) || (map.layer[0].chips.value(x, y) === 10))) },
                    end: (e) => { this.next(); }
                });

                onPointerHook();


                // ターンの状態（フェーズ）
                var turnStateStack: [TurnState, any][] = [[TurnState.WaitInput, null]];
                const moveOffsetTable: [number, number][] = [
                    [0, 0],
                    [-1, -1],
                    [-1, 0],
                    [-1, +1],
                    [0, -1],
                    [0, 0],
                    [0, +1],
                    [+1, -1],
                    [+1, 0],
                    [+1, +1],
                ];


                var playerTactics: any = {};
                var monstersTactics: any[] = [];
                yield (delta: number, ms: number) => {
                    switch (turnStateStack[0][0]) {
                        case TurnState.WaitInput:
                            {
                                // キー入力待ち
                                if (pad.isTouching === false || pad.distance <= 0.4) {
                                    this.player.setAnimation('idle', 0);
                                    break;
                                }

                                // キー入力されたのでプレイヤーの移動方向(5)は移動しない。

                                var playerMoveDir = pad.dir4;

                                // 「行動(Action)」と「移動(Move)」の識別を行う

                                // 移動先が侵入不可能の場合は移動処理キャンセル
                                var [ox, oy] = moveOffsetTable[playerMoveDir];
                                if (map.layer[0].chips.value(this.player.x + ox, this.player.y + oy) === 0) {
                                    this.player.setDir(playerMoveDir);
                                    break;
                                }

                                // 移動先に敵がいる場合は「行動(Action)」、いない場合は「移動(Move)」
                                const targetMonster =
                                    monsters.findIndex((monster) => (monster.x === this.player.x + ox) &&
                                        (monster.y === this.player.y + ox));
                                if (targetMonster !== -1) {
                                    // 移動先に敵がいる＝「行動(Action)」

                                    playerTactics = {
                                        type: "action",
                                        moveDir: playerMoveDir,
                                        targetMonster: playerMoveDir,
                                        startTime: 0,
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
                                        startTime: 0,
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
                            this.player.setDir(playerTactics.moveDir);
                            this.player.setAnimation('atack', 0);
                            break;
                        }
                        case TurnState.PlayerActionRunning: {
                            // プレイヤーの行動中
                            let rate = (ms - playerTactics.startTime) / playerTactics.actionTime;
                            this.player.setAnimation('atack', rate);
                            if (rate >= 1) {
                                // プレイヤーの行動終了
                                turnStateStack.shift();
                            }
                            break;
                        }
                        case TurnState.EnemyAI: {
                            // 敵の行動の決定

                            // プレイヤーが移動する場合、移動先にいると想定して敵の行動を決定する
                            let px = this.player.x;
                            let py = this.player.y;
                            if (playerTactics.type === "move") {
                                let off = moveOffsetTable[playerTactics.moveDir];
                                px += off[0];
                                py += off[1];
                            }

                            monstersTactics = monsters.map((monster) => {
                                let dx = px - monster.x;
                                let dy = py - monster.y;
                                if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) {
                                    // 移動先のプレイヤー位置は現在位置に隣接しているので、行動(Action)を選択
                                    let dir = moveOffsetTable.findIndex((x) => x[0] === dx && x[1] === dy);
                                    return {
                                        type: "action",
                                        moveDir: dir,
                                        startTime: 0,
                                        actionTime: 250
                                    };
                                } else {
                                    // 移動先のプレイヤー位置は現在位置に隣接していないので、移動(Move)を選択
                                    // とりあえず軸合わせで動く
                                    if (Math.abs(dx) < Math.abs(dy)) {
                                        if (dx !== 0) {
                                            dx = Math.sign(dx);
                                        }
                                    } else if (Math.abs(dy) < Math.abs(dx)) {
                                        if (dy !== 0) {
                                            dy = Math.sign(dy);
                                        }
                                    }

                                    let dir = moveOffsetTable.findIndex((x) => x[0] === dx && x[1] === dy);
                                    return {
                                        type: "move",
                                        moveDir: dir,
                                        startTime: 0,
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
                                monsters[enemyId].setDir(monstersTactics[enemyId].moveDir);
                                monsters[enemyId].setAnimation('atack', 0);
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
                            let enemyId = turnStateStack[0][1];

                            let rate = (ms - monstersTactics[enemyId].startTime) / monstersTactics[enemyId].actionTime;
                            monsters[enemyId].setAnimation('atack', rate);
                            if (rate >= 1) {
                                // 行動終了。次の敵へ
                                turnStateStack[0][0] = TurnState.EnemyAction;
                                turnStateStack[0][1] = enemyId + 1;
                                turnStateStack.shift();
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
                                }
                            });
                            if (playerTactics.type === "move") {
                                this.player.setDir(playerTactics.moveDir);
                                this.player.setAnimation('move', 0);
                            }
                            break;
                        }
                        case TurnState.MoveRunning: {
                            // 移動実行  
                            let finish = false;
                            monstersTactics.forEach((monsterTactic, i) => {
                                if (monsterTactic.type === "move") {
                                    let rate = (ms - monsterTactic.startTime) / monsterTactic.actionTime;
                                    monsters[i].setDir(monsterTactic.moveDir);
                                    monsters[i].setAnimation('move', rate);
                                    if (rate < 1) {
                                        finish = false; // 行動終了していないフラグをセット
                                    }
                                }
                            });
                            if (playerTactics.type === "move") {
                                let rate = (ms - playerTactics.startTime) / playerTactics.actionTime;
                                this.player.setDir(playerTactics.moveDir);
                                this.player.setAnimation('move', rate);
                                if (rate < 1) {
                                    finish = false; // 行動終了していないフラグをセット
                                }
                            }
                            if (finish) {
                                // 行動終了
                                turnStateStack.shift();

                                // 現在位置のマップチップを取得
                                const chip = map.layer[0].chips.value(~~param.player.x, ~~param.player.y);
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
                    };



                        // カメラを更新
                        map.update({
                            viewpoint: {
                                x: (param.player.x * 24 + param.player.offx) +
                                param.player._sprite_width / 2,
                                y: (param.player.y * 24 + param.player.offy) +
                                param.player._sprite_height / 2
                            },
                            viewwidth: Game.getScreen().width,
                            viewheight: Game.getScreen().height,
                        }
                        );

                        update_lighting((x, y) => (map.layer[0].chips.value(x, y) === 1) ||
                            (map.layer[0].chips.value(x, y) === 10));

                        if (Game.getInput().isClick() && Game.getScreen().pagePointContainScreen(Game.getInput().pageX, Game.getInput().pageY)) {
                            Game.getSceneManager().push("mapview", { map: map, player: param.player });
                        }

                    };
                    Game.getSound().reqPlayChannel(3);

                    yield waitTimeout({
                        timeout: 500,
                        init: () => { fade.startFadeOut(); },
                        update: (e) => { fade.update(e); update_lighting((x, y) => (map.layer[0].chips.value(x, y) === 1) || (map.layer[0].chips.value(x, y) === 10)) },
                        end: (e) => { this.next(); }
                    });

                    yield waitTimeout({
                        timeout: 500,
                        end: (e) => { param.floor++; this.next(); }
                    });

                    Game.getSceneManager().pop();
                    Game.getSceneManager().push("dungeon", param);

                },
                    mapview: function* (data) {
                        this.draw = () => {
                            Game.getScreen().save();
                            Game.getScreen().clearRect(0, 0, Game.getScreen().width, Game.getScreen().height);
                            Game.getScreen().fillStyle = "rgb(0,0,0)";
                            Game.getScreen().fillRect(0, 0, Game.getScreen().width, Game.getScreen().height);

                            var offx = ~~((Game.getScreen().width - data.map.width * 5) / 2);
                            var offy = ~~((Game.getScreen().height - data.map.height * 5) / 2);

                            // ミニマップを描画
                            for (var y = 0; y < data.map.height; y++) {
                                for (var x = 0; x < data.map.width; x++) {
                                    var chip = data.map.layer[0].chips.value(x, y);
                                    var color = "rgb(52,12,0)";
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

                                    var light = 1 - data.map.visibled.value(x, y) / 100;
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
        var anim = 0;
        var update = (ms) => {
            Game.getScreen().save();
            Game.getScreen().clearRect(0, 0, Game.getScreen().width, Game.getScreen().height);
            Game.getScreen().fillStyle = "rgb(255,255,255)";
            Game.getScreen().fillRect(0, 0, Game.getScreen().width, Game.getScreen().height);

            var n = ~(ms / 50);
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
