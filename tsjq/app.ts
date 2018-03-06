/// <reference path="../../../../../../Program Files (x86)/Microsoft SDKs/TypeScript/2.5/lib.dom.d.ts" />
/// <reference path="../../../../../../Program Files (x86)/Microsoft SDKs/TypeScript/2.5/lib.es2016.d.ts" />
/// <reference path="./js/Game/Array.ts" />
/// <reference path="./js/Game/EventDispatcher.ts" />
/// <reference path="./js/Game/Video.ts" />
/// <reference path="./js/Game/Sound.ts" />
/// <reference path="./js/Game/Input.ts" />
/// <reference path="./js/Game/Timer.ts" />
/// <reference path="./js/Game/Scene.ts" />
/// <reference path="./js/Dungeon.ts" />
"use strict";

/*
//
//interface IteratorResult<T> {
//    //done: boolean;
//    //value: T;
//}

//interface Iterator<T> {
//    next(value?: any): IteratorResult<T>;
//    return?(value?: any): IteratorResult<T>;
//    throw?(e?: any): IteratorResult<T>;
//}

//interface Generator extends Iterator<any> { }

//interface GeneratorFunction {
//    /**
//     * Creates a new Generator object.
//     * @param args A list of arguments the function accepts.
//     */
//    new(...args: any[]): Generator;
//    /**
//     * Creates a new Generator object.
//     * @param args A list of arguments the function accepts.
//     */
//    (...args: any[]): Generator;
//    /**
//     * The length of the arguments.
//     */
//    length: number;
//    /**
//     * Returns the name of the function.
//     */
//    name: string;
//    /**
//     * A reference to the prototype.
//     */
//    prototype: Generator;
//}

//interface GeneratorFunctionConstructor {
//    /**
//     * Creates a new Generator function.
//     * @param args A list of arguments the function accepts.
//     */
//    new(...args: string[]): GeneratorFunction;
//    /**
//     * Creates a new Generator function.
//     * @param args A list of arguments the function accepts.
//     */
//    (...args: string[]): GeneratorFunction;
//    /**
//     * The length of the arguments.
//     */
//    length: number;
//    /**
//     * Returns the name of the function.
//     */
//    name: string;
//    /**
//     * A reference to the prototype.
//     */
//    //prototype: GeneratorFunction;
//}

//interface CanvasRenderingContext2D {
//    mozImageSmoothingEnabled: boolean;
//    imageSmoothingEnabled: boolean;
//    webkitImageSmoothingEnabled: boolean;
//    ellipse: (x: number, y: number, radiusX: number, radiusY: number, rotation: number, startAngle: number, endAngle: number, anticlockwise?: boolean) => void;
//}
//*/


var consolere: Console;

module Game {
    consolere.log("remote log start");

    // Global Variables
    var video: Video = null;
    var sceneManager: Scene.SceneManager = null;
    var input: Input.InputDispatcher = null;
    var timer: Timer.AnimationTimer = null;
    var sound: Sound.SoundManager = null;

    //

    export function create(config: {
        title: string;
        screen: { id: string; scale: number };
        scene: { [name: string]: (data: any) => IterableIterator<any> }
    }) {
        return new Promise<void>((resolve, reject) => {
            try {
                document.title = config.title;
                video = new Video(config.screen.id);
                video.imageSmoothingEnabled = false;
                sceneManager = new Scene.SceneManager(config.scene);
                timer = new Timer.AnimationTimer();
                input = new Input.InputDispatcher();
                sound = new Sound.SoundManager();

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

    export function getInput(): Input.InputDispatcher {
        return input;
    }

    export function getSound(): Sound.SoundManager {
        return sound;
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
        // 通常方向の設定
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

class FadeOut {
    started: boolean;
    startTime: number;
    rate: number;
    w: number;
    h: number;
    constructor(w: number, h: number) {
        this.startTime = -1;
        this.started = false;
        this.w = w;
        this.h = h;
    }
    start() {
        this.started = true;
        this.startTime = -1;
        this.rate = 0;
    }
    stop() {
        this.started = false;
        this.startTime = -1;
        this.rate = 0;
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
    }
    draw() {
        if (this.started) {
            Game.getScreen().fillStyle = `rgba(0,0,0,${this.rate})`;
            Game.getScreen().fillRect(0, 0, this.w, this.h);
        }
    }
}
class FadeIn {
    started: boolean;
    startTime: number;
    rate: number;
    w: number;
    h: number;
    constructor(w: number, h: number) {
        this.startTime = -1;
        this.started = false;
        this.w = w;
        this.h = h;
    }
    start() {
        this.started = true;
        this.startTime = -1;
        this.rate = 0;
    }
    stop() {
        this.started = false;
        this.startTime = -1;
        this.rate = 0;
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
    }
    draw() {
        if (this.started) {
            Game.getScreen().fillStyle = `rgba(0,0,0,${1 - this.rate})`;
            Game.getScreen().fillRect(0, 0, this.w, this.h);
        }
    }
}

window.onload = () => {

    function waitTimeout(param: { timeout: number; action: (elapsed: number) => void; onend: (elapsed: number) => void }) {
        var startTime = -1;
        return (delta: number, ms: number) => {
            if (startTime === -1) {
                startTime = ms;
            }
            const elapsed = ms - startTime;
            if (elapsed >= param.timeout) {
                param.onend(elapsed);
            } else {
                param.action(elapsed);
            }
        };
    };


    Game.create({
        title: "TSJQ",
        screen: {
            id: "glcanvas",
            scale: 2,
        },
        scene: {
            title: function* (data) {
                console.log("state start", data);
                // setup 
                var show_click_or_tap = false;

                var fadeOut = new FadeOut(Game.getScreen().width / 2, Game.getScreen().height / 2);

                this.draw = () => {
                    const w = Game.getScreen().width / 2;
                    const h = Game.getScreen().height / 2;
                    Game.getScreen().save();
                    Game.getScreen().scale(2, 2);
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
                    fadeOut.draw();
                    Game.getScreen().restore();
                };

                { /* wait_click */
                    this.update = (delta: number, ms: number) => {
                        show_click_or_tap = (~~(ms / 500) % 2) === 0;
                    };
                    var pointerclick =
                        (ev: PointerEvent): void => {
                            if (Game.getScreen().pagePointContainScreen(ev.pageX, ev.pageY)) {
                                Game.getInput().off("pointerclick", pointerclick);
                                this.next();
                            }
                        };

                    Game.getInput().on("pointerclick", pointerclick);
                    yield;

                    Game.getSound().reqPlayChannel(0);
                    Game.getSound().playChannel();
                }
                { // brink 
                    this.update = waitTimeout({
                        timeout: 1000,
                        action: (e) => { show_click_or_tap = (~~(e / 50) % 2) === 0 },
                        onend: (e) => this.next()
                    });
                    yield;
                }
                { // Fade out
                    fadeOut.start();
                    this.update = waitTimeout({
                        timeout: 500,
                        action: (e) => { fadeOut.update(e); show_click_or_tap = (~~(e / 50) % 2) === 0 },
                        onend: (e) => {
                            Game.getSceneManager().push("classroom");
                            this.next();
                        }
                    });
                    yield;
                }
            },
            classroom: function* (data) {
                var selectedCharactor = -1;
                var selectedCharactorDir = 0;
                var selectedCharactorOffY = 0;
                var fadeIn = new FadeIn(Game.getScreen().width / 2, Game.getScreen().height / 2);
                var fadeOut = new FadeOut(Game.getScreen().width / 2, Game.getScreen().height / 2);

                this.draw = () => {
                    var w = Game.getScreen().width / 2;
                    var h = Game.getScreen().height / 2;
                    Game.getScreen().save();
                    Game.getScreen().scale(2, 2);
                    Game.getScreen().clearRect(0, 0, w, h);
                    Game.getScreen().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().fillRect(0, 0, w, h);

                    // 床
                    for (var y = 0; y < ~~((w + 23) / 24); y++) {
                        for (var x = 0; x < ~~((w + 23) / 24); x++) {
                            Game.getScreen().drawImage(
                                Game.getScreen().texture("mapchip"),
                                0, 0, 24, 24,
                                x * 24, y * 24, 24, 24
                            );
                        }
                    }
                    // 壁
                    for (var y = 0; y < 2; y++) {
                        for (var x = 0; x < ~~((w + 23) / 24); x++) {
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
                                ((selectedCharactor != id) ? 0 : (188 * (selectedCharactorDir % 4))),
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

                    fadeOut.draw();
                    fadeIn.draw();
                    Game.getScreen().restore();
                };

                {
                    fadeIn.start();
                    Game.getSound().reqPlayChannel(2, true);
                    Game.getSound().playChannel();
                    this.update = waitTimeout({
                        timeout: 500,
                        action: (e) => { fadeIn.update(e); },
                        onend: (e) => {
                            fadeIn.stop();
                            this.next();
                        }
                    });
                    yield;
                }

                var onpointerclick = (ev) => {
                    if (Game.getScreen().pagePointContainScreen(ev.pageX, ev.pageY)) {
                        const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
                        const xx = ~~((pos[0] / 2 - 12) / 36);
                        const yy = ~~((pos[1] / 2 - 24) / (48 - 7));
                        if (0 <= xx && xx < 6 && 0 <= yy && yy < 5) {
                            selectedCharactor = yy * 6 + xx;
                            this.next()
                        }
                    }
                };
                Game.getInput().on("pointerclick", onpointerclick);
                this.update = (delta: number, ms: number) => { };
                yield;

                selectedCharactorDir = 0;
                selectedCharactorOffY = 0;
                Game.getInput().off("pointerclick", onpointerclick);
                Game.getSound().reqPlayChannel(0);
                Game.getSound().playChannel();

                this.update = waitTimeout({
                    timeout: 1800,
                    action: (e) => {
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
                    onend: (e) => { this.next(); }
                });
                yield;

                fadeOut.start();
                this.update = waitTimeout({
                    timeout: 500,
                    action: (e) => { fadeOut.update(e); },
                    onend: (e) => { this.next(); }
                });
                yield;

                const player = new Player({
                    charactor: selectedCharactor,
                    x: 0,
                    y: 0,
                });
                Game.getSound().reqStopChannel(2);
                Game.getSound().playChannel();
                Game.getSceneManager().pop();
                Game.getSceneManager().push("dungeon", { player: player, floor: 1 });
            },
            dungeon: function* (param) {


                // マップサイズ算出
                const mapChipW = 30 + param.floor * 3;
                const mapChipH = 30 + param.floor * 3;

                // マップ自動生成
                const mapchipsL1 = new Dungeon.Matrix(mapChipW, mapChipH);
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
                const mapchipsL2 = new Dungeon.Matrix(mapChipW, mapChipH);
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

                var scale = 2;

                // カメラを更新
                map.update({
                    viewpoint: {
                        x: (param.player.x * 24 + param.player.offx) + param.player._sprite_width / 2,
                        y: (param.player.y * 24 + param.player.offy) + param.player._sprite_height / 2
                    },
                    viewwidth: Game.getScreen().width / scale,
                    viewheight: Game.getScreen().height / scale,
                });

                Game.getSound().reqPlayChannel(1, true);
                Game.getSound().playChannel();

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
                var pointerclick = (ev: PointerEvent): void => {
                    if (Game.getScreen().pagePointContainScreen(ev.pageX, ev.pageY)) {
                        Game.getInput().off("pointerclick", pointerclick);
                        Game.getSceneManager().push("mapview", { map: map, player: param.player });
                    }
                };

                var onPointerHook = () => {
                    Game.getInput().on("pointerdown", pointerdown);
                    Game.getInput().on("pointermove", pointermove);
                    Game.getInput().on("pointerup", pointerup);
                    Game.getInput().on("pointerleave", pointerup);
                    Game.getInput().on("pointerclick", pointerclick);
                };
                var offPointerHook = () => {
                    Game.getInput().off("pointerdown", pointerdown);
                    Game.getInput().off("pointermove", pointermove);
                    Game.getInput().off("pointerup", pointerup);
                    Game.getInput().off("pointerleave", pointerup);
                    Game.getInput().off("pointerclick", pointerclick);
                };

                this.suspend = () => {
                    offPointerHook();
                    Game.getSound().reqStopChannel(1);
                    Game.getSound().playChannel();
                };
                this.resume = () => {
                    onPointerHook();
                    Game.getSound().reqPlayChannel(1, true);
                    Game.getSound().playChannel();
                };
                this.leave = () => {
                    offPointerHook();
                    Game.getSound().reqStopChannel(1);
                    Game.getSound().playChannel();
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
                var start_ms = -1;
                var fade_rate = 1;
                var scale = 2;

                this.draw = () => {

                    Game.getScreen().save();
                    Game.getScreen().scale(scale, scale);
                    Game.getScreen().clearRect(0, 0, Game.getScreen().width / 2, Game.getScreen().height / 2);
                    Game.getScreen().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().fillRect(0, 0, Game.getScreen().width / 2, Game.getScreen().height / 2);

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
                                cameraLocalPy - param.player._sprite_width / 2 - 12,
                                param.player._sprite_width,
                                param.player._sprite_height
                            );

                        }
                    });


                    // フェード
                    if (fade_rate > 0) {
                        Game.getScreen().fillStyle = `rgba(0,0,0,${fade_rate})`;
                        Game.getScreen().fillRect(0,
                            0,
                            Game.getScreen().width / scale,
                            Game.getScreen().height / scale);
                    }
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

                this.update = (delta: number, ms: number) => {
                    if (start_ms === -1) {
                        start_ms = ms;
                    }
                    update_lighting((x, y) => ((map.layer[0].chips.value(x, y) === 1) ||
                        (map.layer[0].chips.value(x, y) === 10)));
                    const elapsed = ms - start_ms;
                    if (elapsed <= 500) {
                        fade_rate = 1 - elapsed / 500;
                    } else {
                        this.next();
                    }
                };
                yield;

                onPointerHook();
                fade_rate = 0;
                start_ms = -1;

                // キャラクターキュー
                var matrix_move = new Dungeon.Matrix(map.width, map.height, 0);
                var move_queue = [];

                move_queue.push({
                    owner: param.player,
                    movedir: "",
                    get_input() {
                        if (pad.isTouching && pad.distance > 0.4) {
                            const dirToAngle = { 0: "left", 1: "up", 2: "right", 3: "down" };
                            const xy = [[-1,0],[0,-1],[1,0],[0,1]];
                            var d = ~~((pad.angle + 180 + 45) / 90) % 4;
                            this.movedir = dirToAngle[d];
                            matrix_move.fill(0);
                            matrix_move.value(param.player.x + xy[d][0], param.player.y + xy[d][1], 1);
                        } else {
                            this.movedir = "";
                        }
                    },
                    moving(delta, ms) {
                        param.player.update(
                            delta,
                            ms,
                            {
                                moveDir: this.movedir,
                                moveCheckCallback: (p, x, y) => (map.layer[0].chips.value(x, y) == 1) ||
                                    (map.layer[0].chips.value(x, y) == 10)
                            });
                        return (param.player.movemode == "idle");
                    },
                    update(delta, ms) {
                        param.player.update(
                            delta,
                            ms,
                            {
                                moveDir: "idle",
                                moveCheckCallback: (p, x, y) => (map.layer[0].chips.value(x, y) == 1) ||
                                    (map.layer[0].chips.value(x, y) == 10)
                            });
                    },

                });

                monsters.forEach((monster) => {
                    move_queue.push({
                        owner: monster,
                        start: -1,
                        movedir:"",
                        get_input() {
                            this.start = -1;
                            var s = [["left", -1, 0], ["up", 0, -1], ["right", 1, 0], ["down", 0, 1], ["idle", 0, 0]].filter(x => map.layer[0].chips.value(monster.x + <number>x[1], monster.y + <number>x[2]) == 1 && matrix_move.value(monster.x + <number>x[1], monster.y + <number>x[2]) != 1).shuffle()[0];
                            matrix_move.value(monster.x + <number>s[1], monster.y + <number>s[2], 1);
                            this.movedir = s[0];
                        },
                        moving(delta, ms) {
                            if (this.start == -1) {
                                this.start = ms;
                            }
                            var e = ms - this.start;
                            var dx = 0;
                            var dy = 0;
                            switch (this.movedir)
                            {
                            case "left":  dx = -1; break;
                            case "up":    dy = -1; break;
                            case "right": dx =  1; break;
                            case "down":  dy =  1; break;
                            }
                            if (e >= 250) {
                                monster.x += dx;
                                monster.y += dy;
                                monster.dx = 0;
                                monster.dy = 0;
                                monster.update(delta, ms);
                                return true;
                            } else {
                                monster.dx = dx * 24 * e / 250;
                                monster.dy = dy * 24 * e / 250;
                                monster.update(delta, ms);
                                return false;
                            }
                        },
                        update(delta, ms, dir) {
                            monster.update(delta, ms);
                        },

                    });
                });

                var move_queue_state = "wait_input";
                var move_dir = "";

                this.update = (delta: number, ms: number) => {
/*
                    // プレイヤーを更新
                    let dir = "idle";
                    if (pad.isTouching && pad.distance > 0.4) {
                        const dirToAngle = { 0: "left", 1: "up", 2: "right", 3: "down" };
                        dir = dirToAngle[~~((pad.angle + 180 + 45) / 90) % 4];
                    }

                    param.player.update(delta,
                        ms,
                        {
                            moveDir: dir,
                            moveCheckCallback: (p, x, y) => (map.layer[0].chips.value(x, y) == 1) ||
                                (map.layer[0].chips.value(x, y) == 10)
                        });

                    // モンスターを更新
                    monsters.forEach((x) => x.update(delta, ms));
*/

                    switch (move_queue_state) {
                    case "wait_input":
                        move_queue[0].get_input.call(move_queue[0]);
                        if (move_queue[0].movedir != "") {
                            for (var i = 1; i < move_queue.length; i++) {
                                move_queue[i].get_input.call(move_queue[i]);
                            }
                            move_queue_state = "moving";
                        } else {
                            for (var i = 0; i < move_queue.length; i++) {
                                move_queue[i].update.call(move_queue[i], delta, ms);
                            }
                        }
                        break;
                    case "moving":
                        console.log("--");
                        for (var i = 0; i < move_queue.length; i++) {
                            if (move_queue[i].movedir != "") {
                                if (move_queue[i].moving.call(move_queue[i], delta, ms)) {
                                    move_queue[i].movedir = ""
                                    console.log(i, "finish");
                                }
                                console.log(i, "move");
                            } else {
                                move_queue[i].update.call(move_queue[i], delta, ms);
                                console.log(i, "idle");
                            }
                        }
                        if (move_queue.every(x => x.movedir == "")) {
                            move_queue_state = "wait_input";

                            // 現在位置のマップチップを取得
                            const chip = map.layer[0].chips.value(~~param.player.x, ~~param.player.y);
                            if (chip === 10) {
                                // 階段なので次の階層に移動させる。
                                this.next();
                            }

                            // プレイヤー位置のモンスターを破壊
                            monsters = monsters.filter((monster) => {
                                if ((monster.x === param.player.x) && (monster.y === param.player.y)) {
                                    consolere.log(param.player.x, param.player.y, monster.x, monster.y);
                                    var n = move_queue.findIndex((x) => x.owner == monster);
                                    move_queue.splice(n, 1);
                                    return false;
                                } else {
                                    return true;
                                }
                            });
                        } else {
                            for (var i = 1; i < move_queue.length; i++) {
                                move_queue[i].update.call(move_queue[i],delta, ms);
                            }
                        }
                        break;
                    }
                    const scale = 2;

                    // カメラを更新
                    map.update({
                        viewpoint: {
                            x: (param.player.x * 24 + param.player.offx) +
                            param.player._sprite_width / 2,
                            y: (param.player.y * 24 + param.player.offy) +
                            param.player._sprite_height / 2
                        },
                        viewwidth: Game.getScreen().width / scale,
                        viewheight: Game.getScreen().height / scale,
                    }
                    );

                    update_lighting((x, y) => (map.layer[0].chips.value(x, y) === 1) ||
                        (map.layer[0].chips.value(x, y) === 10));


                };
                yield;

                Game.getSound().reqPlayChannel(3);
                Game.getSound().playChannel();
                start_ms = -1;
                this.update = (delta: number, ms: number) => {
                    const scale = 2;
                    if (start_ms === -1) {
                        start_ms = ms;
                    }
                    const elapsed = ms - start_ms;
                    update_lighting((x, y) => (map.layer[0].chips.value(x, y) === 1) ||
                        (map.layer[0].chips.value(x, y) === 10));

                    if (elapsed <= 500) {
                        fade_rate = (elapsed / 500);
                    }
                    if (elapsed >= 1000) {
                        param.floor++;
                        this.next();
                    }
                };
                yield;
                Game.getSceneManager().pop();
                Game.getSceneManager().push("dungeon", param);

            },
            mapview: function* (data) {
                var pointerclick = (ev: PointerEvent): void => {
                    if (Game.getScreen().pagePointContainScreen(ev.pageX, ev.pageY)) {
                        this.next();
                    }
                };
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
                            Game.getScreen().fillRect(offx + x * 5, offy + y * 5, 5, 5, );

                            var light = 1 - data.map.visibled.value(x, y) / 100;
                            if (light > 1) {
                                light = 1;
                            } else if (light < 0) {
                                light = 0;
                            }
                            Game.getScreen().fillStyle = `rgba(0,0,0,${light})`;
                            Game.getScreen().fillRect(offx + x * 5, offy + y * 5, 5, 5, );
                        }
                    }

                    Game.getScreen().fillStyle = "rgb(0,255,0)";
                    Game.getScreen().fillRect(offx + data.player.x * 5, offy + data.player.y * 5, 5, 5, );
                    Game.getScreen().restore();
                };
                Game.getInput().on("pointerclick", pointerclick);
                this.update = (delta, ms) => { };
                yield;
                Game.getInput().off("pointerclick", pointerclick);
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

            var n = ~(ms / 200);
            Game.getScreen().translate(Game.getScreen().width / 2, Game.getScreen().height / 2);
            Game.getScreen().rotate(n * Math.PI / 4);
            for (let i = 0; i < 8; i++) {
                const g = (i * 32);
                Game.getScreen().save();
                Game.getScreen().rotate(i * Math.PI / 4);
                Game.getScreen().fillStyle = `rgb(${g},${g},${g})`;
                Game.getScreen().fillRect(-10, -100, 20, 50);
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
            Game.getSceneManager().update(delta, now);
            Game.getSceneManager().draw();
        });
        Game.getTimer().start();
    });
};
