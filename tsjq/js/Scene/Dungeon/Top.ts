/// <reference path="./GameOver.ts" />
/// <reference path="./StatusView.ts" />
/// <reference path="./ItemBoxSelectPlayer.ts" />
/// <reference path="./ItemBoxSelectItem.ts" />
/// <reference path="./MapView.ts" />
/// <reference path="./StatusSprite.ts" />
"use strict";

namespace Scene.Dungeon {

    interface TurnContext {
        floor: number;
        pad: Game.Input.VirtualStick;
        player: Unit.Player;
        monsters: Unit.Monster[];
        map: MapData;
        drops: DropItem[];
        tactics: {
            player: any;
            monsters: any[];
        };
        sprites: Particle.IParticle[];
        scene: Game.Scene.Scene;
        elapsedTurn: number;
    };


    export class Top implements Game.Scene.Scene {
        draw() {}

        update() {}
        onPointerHook() {}
        offPointerHook() {}
        constructor(param: { player: Unit.Player, floor: number }) {

            const player = param.player;
            const floor = param.floor;

            // マップ生成
            const map = MapData.Generator.generate({
                floor: floor,
                gridsize: { width: 24, height: 24 },
                layer: {
                    0: {
                        texture: "mapchip",
                        chip: {
                            1: { x: 48, y: 0 },
                            2: { x: 96, y: 96 },
                            10: { x: 96, y: 0 },
                        },
                        chips: null,
                    },
                    1: {
                        texture: "mapchip",
                        chip: {
                            0: { x: 96, y: 72 },
                        },
                        chips: null,
                    },
                }
            });

            param.player.x = map.startPos.x;
            param.player.y = map.startPos.y;

            // モンスター配置
            let monsters = map.rooms.splice(2).map((x) => {
                const monster = new Unit.Monster("slime");
                monster.x = x.left;
                monster.y = x.top;
                monster.life = monster.maxLife = floor + 5;
                monster.atk = ~~(floor * 2);
                monster.def = ~~(floor / 3) + 1;
                return monster;
            });

            // ドロップアイテム等の情報
            const drops: DropItem[] = [];

            // カメラを更新
            map.update({
                viewpoint: {
                    x: (player.x * map.gridsize.width + player.offx) + map.gridsize.width / 2,
                    y: (player.y * map.gridsize.height + player.offy) + map.gridsize.height / 2,
                },
                viewwidth: Game.getScreen().offscreenWidth,
                viewheight: Game.getScreen().offscreenHeight,
            });

            Game.getSound().reqPlayChannel("dungeon", true);

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

            this.onPointerHook = () => {
                Game.getInput().on("pointerdown", pointerdown);
                Game.getInput().on("pointermove", pointermove);
                Game.getInput().on("pointerup", pointerup);
                Game.getInput().on("pointerleave", pointerup);
            };
            this.offPointerHook = () => {
                Game.getInput().off("pointerdown", pointerdown);
                Game.getInput().off("pointermove", pointermove);
                Game.getInput().off("pointerup", pointerup);
                Game.getInput().off("pointerleave", pointerup);
            };

            //this.suspend = () => {
            //    offPointerHook();
            //    Game.getSound().reqStopChannel("dungeon");
            //};
            //this.resume = () => {
            //    onPointerHook();
            //    Game.getSound().reqPlayChannel("dungeon", true);
            //};
            //this.leave = () => {
            //    offPointerHook();
            //    Game.getSound().reqStopChannel("dungeon");
            //};

            function updateLighting(iswalkable: (x: number) => boolean): void {
                map.clearLighting();
                PathFinder.calcDistanceByDijkstra({
                    array2D: map.layer[0].chips,
                    sx: player.x,
                    sy: player.y,
                    value: 140,
                    costs: (v) => iswalkable(v) ? 20 : 50,
                    output: (x, y, v) => {
                        map.lighting.value(x, y, v);
                        if (map.visibled.value(x, y) < v) {
                            map.visibled.value(x, y, v);
                        }
                    },
                });
            };

            const fade = new Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);

            const particles: Particle.IParticle[] = [];

            const dispatcher: Game.GUI.UIDispatcher = new Game.GUI.UIDispatcher();

            const mapButton = new Game.GUI.ImageButton({
                left: 141 + 22 * 0,
                top: 0,
                width: 23,
                height: 19,
                texture: "menuicon",
                texLeft: 23 * 0,
                texTop: 0,
                texWidth: 23,
                texHeight: 19
            });
            dispatcher.add(mapButton);
            mapButton.click = (x: number, y: number) => {
                const keep = this.update;
                this.offPointerHook();
                this.update = () => {
                    this.onPointerHook();
                    this.update = keep;
                }
                Game.getSceneManager().push(new MapView({ map: map, player: player }));
            };

            const itemButton = new Game.GUI.ImageButton({
                left: 141 + 22 * 1,
                top: 0,
                width: 23,
                height: 19,
                texture: "menuicon",
                texLeft: 23 * 1,
                texTop: 0,
                texWidth: 23,
                texHeight: 19
            });
            dispatcher.add(itemButton);
            itemButton.click = (x: number, y: number) => {
                const keep = this.update;
                this.offPointerHook();
                this.update = () => {
                    this.onPointerHook();
                    this.update = keep;
                }
                Game.getSceneManager()
                    .push(new ItemBoxSelectItem({ selectedItem : -1, player: player, floor: floor, upperdraw: this.draw }));
            };

            const equipButton = new Game.GUI.ImageButton({
                left: 141 + 22 * 2,
                top: 0,
                width: 23,
                height: 19,
                texture: "menuicon",
                texLeft: 23 * 2,
                texTop: 0,
                texWidth: 23,
                texHeight: 19
            });
            dispatcher.add(equipButton);
            equipButton.click = (x: number, y: number) => {
                //Game.getSceneManager().push(mapview, { map: map, player: player });
            };

            const statusButton = new Game.GUI.ImageButton({
                left: 141 + 22 * 3,
                top: 0,
                width: 23,
                height: 19,
                texture: "menuicon",
                texLeft: 23 * 3,
                texTop: 0,
                texWidth: 23,
                texHeight: 19
            });
            dispatcher.add(statusButton);
            statusButton.click = (x: number, y: number) => {
                const keep = this.update;
                this.offPointerHook();
                this.update = () => {
                    this.onPointerHook();
                    this.update = keep;
                }
                Game.getSceneManager().push(new StatusView({ player: player, floor: floor, upperdraw: this.draw }));
            };

            const otherButton = new Game.GUI.ImageButton({
                left: 141 + 22 * 4,
                top: 0,
                width: 23,
                height: 19,
                texture: "menuicon",
                texLeft: 23 * 4,
                texTop: 0,
                texWidth: 23,
                texHeight: 19
            });
            dispatcher.add(otherButton);
            otherButton.click = (x: number, y: number) => {
                //Game.getSceneManager().push(statusView, { player: player, floor:floor, upperdraw: this.draw });
            };

            this.draw = () => {

                Game.getScreen().save();
                Game.getScreen().fillStyle = "rgb(255,255,255)";
                Game.getScreen().fillRect(0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);

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

                        const camera: MapData.Camera = map.camera;

                        // ドロップアイテム
                        drops.forEach((drop) => {
                            const xx = drop.x - camera.chipLeft;
                            const yy = drop.y - camera.chipTop;
                            if ((0 <= xx && xx < Game.getScreen().offscreenWidth / 24) &&
                                (0 <= yy && yy < Game.getScreen().offscreenHeight / 24)) {

                                const dx = xx * map.gridsize.width + camera.chipOffX;
                                const dy = yy * map.gridsize.height + camera.chipOffY;

                                drop.draw(dx, dy);
                            }
                        });

                        // モンスター
                        monsters.forEach((monster) => {
                            const xx = monster.x - camera.chipLeft;
                            const yy = monster.y - camera.chipTop;
                            if ((0 <= xx && xx < Game.getScreen().offscreenWidth / 24) &&
                                (0 <= yy && yy < Game.getScreen().offscreenHeight / 24)) {
                                const animFrame =
                                    monster.spriteSheet.getAnimationFrame(monster.animName, monster.animFrame);
                                const sprite = monster.spriteSheet.gtetSprite(animFrame.sprite);
                                const dx = xx * map.gridsize.width +
                                    camera.chipOffX +
                                    monster.offx +
                                    sprite.offsetX +
                                    animFrame.offsetX;
                                const dy = yy * map.gridsize.height +
                                    camera.chipOffY +
                                    monster.offy +
                                    sprite.offsetY +
                                    animFrame.offsetY;

                                Game.getScreen().drawImage(
                                    monster.spriteSheet.getSpriteImage(sprite),
                                    sprite.left,
                                    sprite.top,
                                    sprite.width,
                                    sprite.height,
                                    dx,
                                    dy,
                                    sprite.width,
                                    sprite.height
                                );
                            }
                        });

                        {
                            const animFrame = player.spriteSheet.getAnimationFrame(player.animName, player.animFrame);
                            const sprite = player.spriteSheet.gtetSprite(animFrame.sprite);

                            // キャラクター
                            Game.getScreen().drawImage(
                                player.spriteSheet.getSpriteImage(sprite),
                                sprite.left,
                                sprite.top,
                                sprite.width,
                                sprite.height,
                                cameraLocalPx - sprite.width / 2 + /*player.offx + */sprite.offsetX + animFrame.offsetX,
                                cameraLocalPy -
                                sprite.height / 2 + /*player.offy + */sprite.offsetY +
                                animFrame.offsetY,
                                sprite.width,
                                sprite.height
                            );
                        }
                    }
                    if (l === 1) {
                        // インフォメーションの描画

                        // モンスター体力
                        const camera: MapData.Camera = map.camera;
                        monsters.forEach((monster) => {
                            const xx = monster.x - camera.chipLeft;
                            const yy = monster.y - camera.chipTop;
                            if ((0 <= xx && xx < Game.getScreen().offscreenWidth / 24) &&
                                (0 <= yy && yy < Game.getScreen().offscreenHeight / 24)) {
                                const animFrame =
                                    monster.spriteSheet.getAnimationFrame(monster.animName, monster.animFrame);
                                const sprite = monster.spriteSheet.gtetSprite(animFrame.sprite);
                                const dx = xx * map.gridsize.width +
                                    camera.chipOffX +
                                    monster.offx +
                                    sprite.offsetX +
                                    animFrame.offsetX;
                                const dy = yy * map.gridsize.height +
                                    camera.chipOffY +
                                    monster.offy +
                                    sprite.offsetY +
                                    animFrame.offsetY;

                                Game.getScreen().fillStyle = 'rgb(255,0,0)';
                                Game.getScreen().fillRect(
                                    dx,
                                    dy + sprite.height - 1,
                                    map.gridsize.width,
                                    1
                                );
                                Game.getScreen().fillStyle = 'rgb(0,255,0)';
                                Game.getScreen().fillRect(
                                    dx,
                                    dy + sprite.height - 1,
                                    ~~(map.gridsize.width * monster.life / monster.maxLife),
                                    1
                                );
                            }
                        });

                        {
                            const animFrame = player.spriteSheet.getAnimationFrame(player.animName, player.animFrame);
                            const sprite = player.spriteSheet.gtetSprite(animFrame.sprite);

                            // キャラクター体力
                            Game.getScreen().fillStyle = 'rgb(255,0,0)';
                            Game.getScreen().fillRect(
                                cameraLocalPx -
                                map.gridsize.width / 2 + /*player.offx + */sprite.offsetX +
                                animFrame.offsetX,
                                cameraLocalPy -
                                sprite.height / 2 + /*player.offy + */sprite.offsetY +
                                animFrame.offsetY +
                                sprite.height -
                                1,
                                map.gridsize.width,
                                1
                            );
                            Game.getScreen().fillStyle = 'rgb(0,255,0)';
                            Game.getScreen().fillRect(
                                cameraLocalPx -
                                map.gridsize.width / 2 + /*player.offx + */sprite.offsetX +
                                animFrame.offsetX,
                                cameraLocalPy -
                                sprite.height / 2 + /*player.offy + */sprite.offsetY +
                                animFrame.offsetY +
                                sprite.height -
                                1,
                                ~~(map.gridsize.width * player.getForward().hp / player.getForward().hpMax),
                                1
                            );
                        }
                    }
                });

                // スプライト
                particles.forEach((x) => x.draw(map.camera));

                // 情報
                Font7px.draw7pxFont(`     | HP:${player.getForward().hp}/${player.getForward().hpMax}`, 0, 6 * 0);
                Font7px.draw7pxFont(
                    `${('   ' + floor).substr(-3)}F | MP:${player.getForward().mp}/${player.getForward().mpMax}`,
                    0,
                    6 * 1);
                Font7px.draw7pxFont(`     | GOLD:${Data.SaveData.money}`, 0, 6 * 2);
                //menuicon

                // UI
                dispatcher.draw();

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

            const TurnMain = () => {
                this.onPointerHook();

                // ターンの状態（フェーズ）
                const turnContext: TurnContext = {
                    floor: floor,
                    pad: pad,
                    player: player,
                    monsters: monsters,
                    map: map,
                    drops: drops,
                    tactics: {
                        player: {},
                        monsters: []
                    },
                    sprites: particles,
                    scene: this,
                    elapsedTurn: 0,
                };

                const turnStateStack: IterableIterator<any>[] = [];
                turnStateStack.unshift(WaitInput.call(this, turnStateStack, turnContext));

                const common_update = () => {
                    // カメラを更新
                    map.update({
                            viewpoint: {
                                x: (player.x * map.gridsize.width + player.offx) + map.gridsize.width / 2,
                                y: (player.y * map.gridsize.height + player.offy) + map.gridsize.height / 2,
                            },
                            viewwidth: Game.getScreen().offscreenWidth,
                            viewheight: Game.getScreen().offscreenHeight
                        }
                    );

                    // スプライトを更新
                    particles.removeIf((x) => x.update());

                    updateLighting((v: number) => v === 1 || v === 10);

                    if (player.getForward().hp === 0) {
                        if (player.getBackward().hp !== 0) {
                            player.active = player.active == 0 ? 1 : 0;
                        } else {
                            // ターン強制終了
                            this.offPointerHook();
                            Game.getSound().reqStopChannel("dungeon");
                            Game.getSceneManager().pop();
                            Game.getSceneManager()
                                .push(new GameOver({ player: player, floor: floor, upperdraw: this.draw }));
                            return;
                        }
                    }

                    // ui 
                    if (Game.getInput().isDown()) {
                        dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
                    }
                    if (Game.getInput().isMove()) {
                        dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
                    }
                    if (Game.getInput().isUp()) {
                        dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
                    }

                };

                this.update = () => {
                    // ターン進行
                    while (turnStateStack.length > 0 && turnStateStack[0].next().done) {
                    }
                    common_update();
                };
            }

            this.update = waitFadeIn(fade, TurnMain, () => updateLighting((v: number) => v === 1 || v === 10));

        }
    }

    function *WaitInput(turnStateStack: IterableIterator<boolean>[], context: TurnContext): IterableIterator<any> {
        for (; ;) {
            // キー入力待ち
            if (context.pad.isTouching === false || context.pad.distance <= 0.4) {
                context.player.setAnimation("move", 0);
                yield;
                continue;
            }

            // キー入力された

            const playerMoveDir = context.pad.dir8;

            // 「行動(Action)」と「移動(Move)」の識別を行う

            // 移動先が侵入不可能の場合は待機とする
            const { x, y } = Array2D.DIR8[playerMoveDir];
            if (context.map.layer[0].chips.value(context.player.x + x, context.player.y + y) !== 1 &&
                context.map.layer[0].chips.value(context.player.x + x, context.player.y + y) !== 10) {
                context.player.setDir(playerMoveDir);
                yield;
                continue;
            }

            turnStateStack.shift();

            // 移動先に敵がいる場合は「行動(Action)」、いない場合は「移動(Move)」
            const targetMonster = context.monsters.findIndex((monster) => (monster.x === context.player.x + x) && (monster.y === context.player.y + y));
            if (targetMonster !== -1) {
                // 移動先に敵がいる＝「行動(Action)」

                context.tactics.player = {
                    type: "action",
                    moveDir: playerMoveDir,
                    targetMonster: targetMonster,
                    startTime: 0,
                    actionTime: 250,
                };

                // プレイヤーの行動、敵の行動の決定、敵の行動処理、移動実行の順で行う
                turnStateStack.unshift(
                    PlayerAction.call(this,turnStateStack, context),
                    EnemyAI.call(this,turnStateStack, context),
                    EnemyAction.call(this,turnStateStack, context),
                    Move.call(this,turnStateStack, context),
                    TurnEnd.call(this,turnStateStack, context)
                );
                return;
            } else {
                // 移動先に敵はいない＝「移動(Move)」

                context.tactics.player = {
                    type: "move",
                    moveDir: playerMoveDir,
                    startTime: 0,
                    actionTime: 250,
                };

                // 敵の行動の決定、移動実行、敵の行動処理、の順で行う。
                turnStateStack.unshift(
                    EnemyAI.call(this,turnStateStack, context),
                    Move.call(this,turnStateStack, context),
                    EnemyAction.call(this,turnStateStack, context),
                    TurnEnd.call(this,turnStateStack, context)
                );
                return;
            }
        }
    }
    function* PlayerAction(turnStateStack: IterableIterator<boolean>[], context: TurnContext): IterableIterator<any> {
        // プレイヤーの行動
        const startTime = Game.getTimer().now;
        context.player.setDir(context.tactics.player.moveDir);
        context.player.setAnimation("action", 0);
        let acted = false;
        for (; ;) {
            const rate = (Game.getTimer().now - startTime) / context.tactics.player.actionTime;
            context.player.setAnimation("action", rate);
            if (rate >= 0.5 && acted === false) {
                acted = true;
                const targetMonster: Unit.Monster = context.monsters[context.tactics.player.targetMonster];
                Game.getSound().reqPlayChannel("atack");

                const dmg = ~~(context.player.atk *(100+Math.random() * 30-15)/100 - targetMonster.def / 2);

                context.sprites.push(Particle.createShowDamageSprite(
                    Game.getTimer().now,
                    dmg > 0 ? ("" + dmg) : "MISS!!",
                    () => {
                        return {
                            x: targetMonster.offx + targetMonster.x * context.map.gridsize.width + context.map.gridsize.width / 2,
                            y: targetMonster.offy + targetMonster.y * context.map.gridsize.height + context.map.gridsize.height / 2
                        };
                    }
                ));
                if (targetMonster.life > 0 && dmg > 0) {
                    targetMonster.life -= dmg;
                    if (targetMonster.life <= 0) {
                        targetMonster.life = 0;
                        // 敵を死亡状態にする
                        Game.getSound().reqPlayChannel("explosion");
                        // 死亡処理を割り込みで行わせる
                        turnStateStack.splice(1, 0, EnemyDead(turnStateStack, context, context.tactics.player.targetMonster));
                    }
                }
            }
            if (rate >= 1) {
                // プレイヤーの行動終了
                turnStateStack.shift();
                context.player.setAnimation("move", 0);
                return;
            }
            yield;
        }
    }
    function* EnemyDead(turnStateStack: IterableIterator<boolean>[], context: TurnContext, enemyId: number): IterableIterator<any> {
        // 敵の死亡
        const start = Game.getTimer().now;
        Game.getSound().reqPlayChannel("explosion");
        const monster = context.monsters[enemyId];
        monster .setAnimation("dead", 0);

        // ドロップ作成は今のところ適当
        if (Math.random() < 0.8) {
            context.drops.push(new GoldBug(monster.x, monster.y, ~~(context.floor * (Math.random() * 9 + 1))));
        } else {
            context.drops.push(new ItemBug(monster.x, monster.y, [{ id: 1001, condition:"", count: 1}]));
        }
            

        for (; ;) {
            const diff = Game.getTimer().now - start;
            monster.setAnimation("dead", diff / 250);
            if (diff >= 250) {
                turnStateStack.shift();
                return;
            }
            yield;
        }
    }
    function* EnemyAI(turnStateStack: IterableIterator<boolean>[], context: TurnContext): IterableIterator<any> {
        // 敵の行動の決定

        // 移動不可能地点を書き込む配列
        const cannotMoveMap = new Array2D(context.map.width, context.map.height, 0);
        context.tactics.monsters.length = context.monsters.length;
        context.tactics.monsters.fill(null);
        context.monsters.forEach((monster, i) => {
            // 敵全体の移動不能座標に自分を設定
            cannotMoveMap.value(monster.x, monster.y, 1);
        });


        // プレイヤーが移動する場合、移動先にいると想定して敵の行動を決定する
        let px = context.player.x;
        let py = context.player.y;
        if (context.tactics.player.type === "move") {
            const off = Array2D.DIR8[context.tactics.player.moveDir];
            px += off.x;
            py += off.y;
        }
        cannotMoveMap.value(px, py, 1);

        // 行動(Action)と移動(Move)は分離しないと移動で敵が重なる

        // 行動(Action)する敵を決定
        context.monsters.forEach((monster, i) => {
            if (monster.life <= 0) {
                // 死亡状態なので何もしない
                context.tactics.monsters[i] = {
                    type: "dead",
                    moveDir: 5,
                    startTime: 0,
                    actionTime: 250,
                };
                cannotMoveMap.value(monster.x, monster.y, 0);
                return;
            }
            const dx = px - monster.x;
            const dy = py - monster.y;
            if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) {
                // 移動先のプレイヤー位置は現在位置に隣接しているので、行動(Action)を選択
                const dir = Array2D.DIR8.findIndex((x) => x.x === dx && x.y === dy);
                // 移動不能座標に変化は無し
                context.tactics.monsters[i] = {
                    type: "action",
                    moveDir: dir,
                    startTime: 0,
                    actionTime: 250,
                };
                return;
            } else {
                return; // skip
            }
        });

        // 移動(Move)する敵の移動先を決定する
        // 最良の移動先にキャラクターが存在することを考慮して移動処理が発生しなくなるまで計算を繰り返す。
        let changed = true;
        while (changed) {
            changed = false;
            context.monsters.forEach((monster, i) => {
                const dx = px - monster.x;
                const dy = py - monster.y;
                if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) {
                    if (context.tactics.monsters[i] == null) {
                        console.error("Actionすべき敵の動作が決定していない");
                    }
                    return;
                } else if (context.tactics.monsters[i] == null) {
                    // 移動先のプレイヤー位置は現在位置に隣接していないので、移動(Move)を選択
                    // とりあえず軸合わせ戦略で動く

                    // 移動先の候補表から最良の移動先を選ぶ
                    const cands = [
                        [Math.sign(dx), Math.sign(dy)],
                        (Math.abs(dx) > Math.abs(dy)) ? [0, Math.sign(dy)] : [Math.sign(dx), 0],
                        (Math.abs(dx) > Math.abs(dy)) ? [Math.sign(dx), 0] : [0, Math.sign(dy)],
                    ];

                    for (let j = 0; j < cands.length; j++) {
                        const [cx, cy] = cands[j];
                        const tx = monster.x + cx;
                        const ty = monster.y + cy;
                        if ((cannotMoveMap.value(tx, ty) === 0) && (context.map.layer[0].chips.value(tx, ty) === 1 || context.map.layer[0].chips.value(tx, ty) === 10)) {
                            const dir = Array2D.DIR8.findIndex((x) => x.x === cx && x.y === cy);
                            // 移動不能座標を変更
                            cannotMoveMap.value(monster.x, monster.y, 0);
                            cannotMoveMap.value(tx, ty, 1);
                            context.tactics.monsters[i] = {
                                type: "move",
                                moveDir: dir,
                                startTime: 0,
                                actionTime: 250,
                            };
                            changed = true;
                            return;
                        }
                    }
                    // 移動先が全部移動不能だったので待機を選択
                    // 敵全体の移動不能座標に自分を設定
                    cannotMoveMap.value(monster.x, monster.y, 1);
                    context.tactics.monsters[i] = {
                        type: "idle",
                        moveDir: 5,
                        startTime: 0,
                        actionTime: 250,
                    };
                    changed = true;
                    return;
                }
            });
        }
        // 敵の行動の決定の終了
        turnStateStack.shift();
        return;
    }
    function* EnemyAction(turnStateStack: IterableIterator<boolean>[], context: TurnContext): IterableIterator<any> {
        // 敵の行動開始
        for (let enemyId = 0; enemyId < context.tactics.monsters.length; enemyId++) {
            if (context.tactics.monsters[enemyId].type !== "action") {
                continue;
            }
            context.tactics.monsters[enemyId].startTime = Game.getTimer().now;
            context.monsters[enemyId].setDir(context.tactics.monsters[enemyId].moveDir);
            context.monsters[enemyId].setAnimation("action", 0);
            yield* EnemyDoAction(turnStateStack, context, enemyId);
        }
        // もう動かす敵がいない
        turnStateStack.shift();
        return;
    }
    function* EnemyDoAction(turnStateStack: IterableIterator<boolean>[], context: TurnContext, enemyId: number): IterableIterator<any> {
        const startTime = Game.getTimer().now;
        let acted = false;
        for (; ;) {
            const rate = (Game.getTimer().now - startTime) / context.tactics.monsters[enemyId].actionTime;
            context.monsters[enemyId].setAnimation("action", rate);
            if (rate >= 0.5 && acted == false) {
                acted = true;
                Game.getSound().reqPlayChannel("atack");

                const dmg = ~~(context.monsters[enemyId].atk *(100+Math.random() * 30-15)/100 - context.player.def / 2);

                context.sprites.push(Particle.createShowDamageSprite(
                    Game.getTimer().now,
                    dmg > 0 ? ("" + dmg) : "MISS!!",
                    () => {
                        return {
                            x: context.player.offx + context.player.x * context.map.gridsize.width + context.map.gridsize.width / 2,
                            y: context.player.offy + context.player.y * context.map.gridsize.height + context.map.gridsize.height / 2
                        };
                    }
                ));
                if (context.player.getForward().hp > 0 && dmg > 0) {
                    context.player.getForward().hp -= dmg;
                    if (context.player.getForward().hp <= 0) {
                        context.player.getForward().hp = 0;
                    }
                }
            }
            if (rate >= 1) {
                // 行動終了。次の敵へ
                context.monsters[enemyId].setAnimation("action", 0);
                return;
            }
            yield;
        }
    }
    function* Move(turnStateStack: IterableIterator<boolean>[], context: TurnContext): IterableIterator<any> {
        // 移動開始
        const start = Game.getTimer().now;
        context.tactics.monsters.forEach((monsterTactic: any, i: number) => {
            if (monsterTactic.type === "move") {
                context.monsters[i].setDir(monsterTactic.moveDir);
                context.monsters[i].setAnimation("move", 0);
            }
        });
        if (context.tactics.player.type === "move") {
            context.player.setDir(context.tactics.player.moveDir);
            context.player.setAnimation("move", 0);
        }

        for (; ;) {
            // 移動実行
            let finish = true;
            context.tactics.monsters.forEach((monsterTactic: any, i: number) => {
                if (monsterTactic == null) {
                    return;
                }
                if (monsterTactic.type === "move") {
                    const rate = (Game.getTimer().now - start) / monsterTactic.actionTime;
                    context.monsters[i].setDir(monsterTactic.moveDir);
                    context.monsters[i].setAnimation("move", rate);
                    if (rate < 1) {
                        finish = false; // 行動終了していないフラグをセット
                    } else {
                        context.monsters[i].x += Array2D.DIR8[monsterTactic.moveDir].x;
                        context.monsters[i].y += Array2D.DIR8[monsterTactic.moveDir].y;
                        context.monsters[i].offx = 0;
                        context.monsters[i].offy = 0;
                        context.monsters[i].setAnimation("move", 0);
                    }
                }
            });
            if (context.tactics.player.type === "move") {
                const rate = (Game.getTimer().now - start) / context.tactics.player.actionTime;
                context.player.setDir(context.tactics.player.moveDir);
                context.player.setAnimation("move", rate);
                if (rate < 1) {
                    finish = false; // 行動終了していないフラグをセット
                } else {
                    context.player.x += Array2D.DIR8[context.tactics.player.moveDir].x;
                    context.player.y += Array2D.DIR8[context.tactics.player.moveDir].y;
                    context.player.offx = 0;
                    context.player.offy = 0;
                    context.player.setAnimation("move", 0);
                }
            }
            if (finish) {
                // 行動終了
                turnStateStack.shift();
                return;
            }
            yield;
        }
    }
    function* TurnEnd(turnStateStack: IterableIterator<boolean>[], context: TurnContext): IterableIterator<any> {
        // 死亡したモンスターを消去
        context.monsters.removeIf(x => x.life == 0);

        // 前衛はターン経過によるMP消費が発生する
        context.elapsedTurn += 1;
        if (context.elapsedTurn % 10 === 0) {
            if (context.player.getForward().mp > 0) {
                context.player.getForward().mp -= 1;
            } else if (context.player.getForward().hp > 1) {
                // mpが無い場合はhpが減少
                context.player.getForward().hp -= 1;
            }
        }

        // HPが減少している場合はMPを消費してHPを回復
        if (context.player.getForward().hp < context.player.getForward().hpMax && context.player.getForward().mp > 0) {
            context.player.getForward().hp += 1;
            context.player.getForward().mp -= 1;
        }

        // 後衛はターン経過によるMP消費が無い
        // HPが減少している場合はMPを消費してHPを回復
        if (context.player.getBackward().hp < context.player.getBackward().hpMax && context.player.getBackward().mp > 0) {
            context.player.getBackward().hp += 1;
            context.player.getBackward().mp -= 1;
        }

        const px = ~~context.player.x;
        const py = ~~context.player.y;
        // 現在位置のマップチップを取得
        const chip = context.map.layer[0].chips.value(px, py);


        // ドロップやイベント処理

        // ドロップアイテム
        context.drops.removeIf((drop) => {
            if (drop.x === px && drop.y === py) {
                drop.take();
                return true;
            } else {
                return false;
            }
        });
        


        if (chip === 10) {
            turnStateStack.shift();
            turnStateStack.unshift(Stair.call(this,turnStateStack, context));
        } else {
            turnStateStack.shift();
            turnStateStack.unshift(WaitInput.call(this,turnStateStack, context));
        }
        return;
    }
    function* Stair(turnStateStack: IterableIterator<boolean>[], context: TurnContext): IterableIterator<any> {
            // 階段なので次の階層に移動させる。

            // ボタン選択を出して進むか戻るか決定させる
            let mode : string = null;
            if (context.floor % 5 === 0) {
                const dispatcher: Game.GUI.UIDispatcher = new Game.GUI.UIDispatcher();

                const caption = new Game.GUI.TextBox({
                    left: 1,
                    top: Game.getScreen().height / 2 - 50,
                    width: 250,
                    height: 42,
                    text: "上り階段と下り階段がある。\n上り階段には非常口のマークと「地上行き」の看板\nが張られている。",
                    edgeColor: `rgb(12,34,98)`,
                    color: `rgb(24,133,196)`,
                    font: "10px 'PixelMplus10-Regular'",
                    fontColor: `rgb(255,255,255)`,
                    textAlign: "left",
                    textBaseline: "top",
                });
                dispatcher.add(caption);

                const btnGotoNext = new Game.GUI.Button({
                    left: ~~(Game.getScreen().width / 4 - 40),
                    top: Game.getScreen().height / 2 + 20,
                    width: 80,
                    height: 19,
                    text: "下り階段を進む"
                });
                dispatcher.add(btnGotoNext);
                btnGotoNext.click = (x: number, y: number) => {
                    mode = "next";
                };
                const btnReturn = new Game.GUI.Button({
                    left: ~~(Game.getScreen().width * 3 / 4 - 40),
                    top: Game.getScreen().height / 2 + 20,
                    width: 80,
                    height: 19,
                    text: "上り階段を進む"
                });
                dispatcher.add(btnReturn);
                btnReturn.click = (x: number, y: number) => {
                    mode = "exit";
                };
                const orgDraw = this.draw;
                this.draw = () => {
                    orgDraw();
                    dispatcher.draw();
                };
                for (;;) {
                    if (Game.getInput().isDown()) {
                        dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
                    }
                    if (Game.getInput().isMove()) {
                        dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
                    }
                    if (Game.getInput().isUp()) {
                        dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
                    }
                    if (mode != null) {
                        break;
                    }
                    yield;
                }
            } else {
                mode = "next";
            }
            Game.getSound().reqPlayChannel("kaidan");
            const fade = new Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);

            const orgDraw = this.draw;
            this.draw = () => {
                orgDraw();
                fade.draw();
            };

            fade.startFadeOut();
            const start = Game.getTimer().now;
            while (Game.getTimer().now - start < 500) {
                fade.update(Game.getTimer().now - start);
                yield;
            }
            fade.update(500);

            while (Game.getTimer().now - start < 1000) {
                yield;
            }
            this.offPointerHook();
            Game.getSound().reqStopChannel("dungeon");
            if (mode === "next") {
                Game.getSceneManager().pop();
                Game.getSceneManager().push(new Top({ player: context.player, floor: context.floor + 1 }));
            } else {
                Game.getSceneManager().pop();
                Game.getSceneManager().push(new Scene.Corridor());
            }
        turnStateStack.shift();
        return;
    }


}

