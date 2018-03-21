namespace Scene {
    enum TurnState {
        WaitInput, // プレイヤーの行動決定(入力待ち)
        PlayerAction, // プレイヤーの行動実行
        PlayerActionRunning,
        EnemyAI, // 敵の行動決定
        EnemyAction, // 敵の行動実行
        EnemyActionRunning,
        EnemyDead, // 敵の死亡
        EnemyDeadRunning, // 死亡実行
        Move, // 移動実行（敵味方同時に行う）
        MoveRunning, // 移動実行（敵味方同時に行う）
        TurnEnd, // ターン終了
    }

    export function* dungeon(param: { saveData: Data.SaveData.SaveData, player: Unit.Player, floor: number }): IterableIterator < any > {
        const player = param.player;
        const floor = param.floor;

        // マップサイズ算出
        const mapChipW = 30 + floor * 3;
        const mapChipH = 30 + floor * 3;

        // マップ自動生成
        const mapchipsL1 = new Array2D(mapChipW, mapChipH);
        const layout = Dungeon.generate(
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

        // 部屋は生成後にシャッフルしているのでそのまま取り出す

        const rooms = layout.rooms.slice();

        // 開始位置
        const startPos = rooms[0].getCenter();
        player.x = startPos.x;
        player.y = startPos.y;

        // 階段位置
        const stairsPos = rooms[1].getCenter();
        mapchipsL1.value(stairsPos.x, stairsPos.y, 10);

        // モンスター配置
        let monsters = rooms.splice(2).map((x) => {
            const monster = new Unit.Monster("slime");
                monster.x = x.getLeft();
                monster.y = x.getTop();
                monster.life = monster.maxLife = floor + 5;
                monster.atk = ~~(floor * 2);
                monster.def = ~~(floor / 3) + 1;
                return monster;
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
                    chips: mapchipsL1,
                },
                1: {
                    texture: "mapchip",
                    chip: {
                        0: { x: 96, y: 72 },
                    },
                    chips: mapchipsL2,
                },
            },
        });

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
            Game.getSound().reqStopChannel("dungeon");
        };
        this.resume = () => {
            onPointerHook();
            Game.getSound().reqPlayChannel("dungeon", true);
        };
        this.leave = () => {
            offPointerHook();
            Game.getSound().reqStopChannel("dungeon");
        };

        const updateLighting = (iswalkable: (x: number) => boolean) => {
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
            Game.getSceneManager().push(mapview, { map: map, player: player });
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
            Game.getSceneManager().push(itemView, { saveData: param.saveData, player: player, floor: floor, upperdraw: this.draw });
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
            Game.getSceneManager().push(statusView, { player: player, floor: floor, upperdraw: this.draw });
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

                    // モンスター
                    const camera: Dungeon.Camera = map.camera;
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
                            cameraLocalPy - sprite.height / 2 + /*player.offy + */sprite.offsetY + animFrame.offsetY,
                            sprite.width,
                            sprite.height
                        );
                    }
                }
                if (l === 1) {
                    // インフォメーションの描画

                    // モンスター体力
                    const camera: Dungeon.Camera = map.camera;
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
                            cameraLocalPx - map.gridsize.width / 2 + /*player.offx + */sprite.offsetX + animFrame.offsetX,
                            cameraLocalPy - sprite.height / 2 + /*player.offy + */sprite.offsetY + animFrame.offsetY + sprite.height - 1,
                            map.gridsize.width,
                            1
                        );
                        Game.getScreen().fillStyle = 'rgb(0,255,0)';
                        Game.getScreen().fillRect(
                            cameraLocalPx - map.gridsize.width / 2 + /*player.offx + */sprite.offsetX + animFrame.offsetX,
                            cameraLocalPy - sprite.height / 2 + /*player.offy + */sprite.offsetY + animFrame.offsetY + sprite.height - 1,
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
            Font7px.draw7pxFont(`${('   ' + floor).substr(-3)}F | MP:${player.getForward().mp}/${player.getForward().mpMax}`, 0, 6 * 1);
            Font7px.draw7pxFont(`     | GOLD:${param.saveData.Money}`, 0, 6 * 2);
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

        yield waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeIn(); },
            update: (e) => {
                fade.update(e);
                updateLighting((v: number) => v === 1 || v === 10);
            },
            end: () => { this.next(); },
        });

        onPointerHook();

        // ターンの状態（フェーズ）
        const turnContext: TurnContext = {
            ms: 0,
            pad: pad,
            player: player,
            monsters: monsters,
            map: map,
            tactics: {
                player: {},
                monsters: []
            },
            sprites: particles,
            scene: this,
        };

        const turnStateStack: IterableIterator<any>[] = [];
        turnStateStack.unshift(WaitInput(turnStateStack, turnContext));

        yield (delta: number, ms: number) => {

            // ターン進行
            turnContext.ms = ms;
            while (turnStateStack[0].next().done) { }

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
            particles.removeIf((x) => x.update(delta, ms));

            updateLighting((v: number) => v === 1 || v === 10);

            if (player.getForward().hp === 0) {
                if (player.getBackward().hp !== 0) {
                    player.active = player.active == 0 ? 1 : 0;
                } else {
                    // ターン強制終了
                    Game.getSceneManager().pop();
                    Game.getSceneManager().push(gameOver, { saveData : param.saveData, player: player, floor: floor, upperdraw: this.draw });
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

            //if (Game.getInput().isClick() &&
            //    Game.getScreen().pagePointContainScreen(Game.getInput().pageX, Game.getInput().pageY)) {
            //    //Game.getSceneManager().push(mapview, { map: map, player: player });
            //    Game.getSceneManager().push(statusView, { player: player, floor: floor, upperdraw: this.draw });
            //}

        };
        Game.getSound().reqPlayChannel("kaidan");

        yield waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeOut(); },
            update: (e) => {
                fade.update(e);
                updateLighting((v: number) => v === 1 || v === 10);
            },
            end: () => { this.next(); },
        });

        yield waitTimeout({
            timeout: 500,
            end: () => { this.next(); },
        });

        Game.getSceneManager().pop();
        Game.getSceneManager().push(dungeon, { saveData: param.saveData, player: player, floor: floor + 1 });

    }

    interface TurnContext {
        ms: number;
        pad: Game.Input.VirtualStick;
        player: Unit.Player;
        monsters: Unit.Monster[];
        map: Dungeon.DungeonData;
        tactics: {
            player: any;
            monsters: any[];
        };
        sprites: Particle.IParticle[];
        scene: Game.Scene.Scene;
    };

    function* WaitInput(turnStateStack: IterableIterator<boolean>[], context: TurnContext): IterableIterator<any> {
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
                    PlayerAction(turnStateStack, context),
                    EnemyAI(turnStateStack, context),
                    EnemyAction(turnStateStack, context),
                    Move(turnStateStack, context),
                    TurnEnd(turnStateStack, context)
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
                    EnemyAI(turnStateStack, context),
                    Move(turnStateStack, context),
                    EnemyAction(turnStateStack, context),
                    TurnEnd(turnStateStack, context)
                );
                return;
            }
        }
    }

    function* PlayerAction(turnStateStack: IterableIterator<boolean>[], context: TurnContext): IterableIterator<any> {
        // プレイヤーの行動
        const startTime = context.ms;
        context.player.setDir(context.tactics.player.moveDir);
        context.player.setAnimation("action", 0);
        let acted = false;
        for (; ;) {
            const rate = (context.ms - startTime) / context.tactics.player.actionTime;
            context.player.setAnimation("action", rate);
            if (rate >= 0.5 && acted == false) {
                acted = true;
                const targetMonster: Unit.Monster = context.monsters[context.tactics.player.targetMonster];
                Game.getSound().reqPlayChannel("atack");
                const dmg = ~~(context.player.atk - targetMonster.def);

                context.sprites.push(Particle.createShowDamageSprite(
                    context.ms,
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
                        // explosion
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
        const start = context.ms;
        Game.getSound().reqPlayChannel("explosion");
        context.monsters[enemyId].setAnimation("dead", 0);
        for (; ;) {
            const diff = context.ms - start;
            context.monsters[enemyId].setAnimation("dead", diff / 250);
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
            context.tactics.monsters[enemyId].startTime = context.ms;
            context.monsters[enemyId].setDir(context.tactics.monsters[enemyId].moveDir);
            context.monsters[enemyId].setAnimation("action", 0);
            yield* EnemyDoAction(turnStateStack, context, enemyId);
        }
        // もう動かす敵がいない
        turnStateStack.shift();
        return;
    }
    function* EnemyDoAction(turnStateStack: IterableIterator<boolean>[], context: TurnContext, enemyId: number): IterableIterator<any> {
        const startTime = context.ms;
        let acted = false;
        for (; ;) {
            const rate = (context.ms - startTime) / context.tactics.monsters[enemyId].actionTime;
            context.monsters[enemyId].setAnimation("action", rate);
            if (rate >= 0.5 && acted == false) {
                acted = true;
                Game.getSound().reqPlayChannel("atack");
                const dmg = ~~(context.monsters[enemyId].atk - context.player.def);
                context.sprites.push(Particle.createShowDamageSprite(
                    context.ms,
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
        const start = context.ms;
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
                    const rate = (context.ms - start) / monsterTactic.actionTime;
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
                const rate = (context.ms - start) / context.tactics.player.actionTime;
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
        turnStateStack.shift();

        // 死亡したモンスターを消去
        context.monsters.removeIf(x => x.life == 0);

        // 前衛はターン経過によるMP消費が発生する
        if (context.player.getForward().mp > 0) {
            context.player.getForward().mp -= 1;
            // HPが減少している場合はMPを消費してHPを回復
            if (context.player.getForward().hp < context.player.getForward().hpMax && context.player.getForward().mp > 0) {
                context.player.getForward().hp += 1;
                context.player.getForward().mp -= 1;
            }
        } else if (context.player.getForward().hp > 1) {
            // mpが無い場合はhpが減少
            context.player.getForward().hp -= 1;
        }

        // 後衛はターン経過によるMP消費が無い
        // HPが減少している場合はMPを消費してHPを回復
        if (context.player.getBackward().hp < context.player.getBackward().hpMax && context.player.getBackward().mp > 0) {
            context.player.getBackward().hp += 1;
            context.player.getBackward().mp -= 1;
        }


        // 現在位置のマップチップを取得
        const chip = context.map.layer[0].chips.value(~~context.player.x, ~~context.player.y);
        if (chip === 10) {
            // 階段なので次の階層に移動させる。
            context.scene.next("nextfloor");
            yield;
        } else {
            turnStateStack.unshift(WaitInput(turnStateStack, context));
        }
        return;
    }

    function showStatusText(str: string, x:number, y:number): void {
        const fontWidth: number = 5;
        const fontHeight: number = 7;

        const len = str.length;
        for (let i = 0; i < str.length; i++) {
            const [fx, fy] = Font7px.charDic[str[i]];
            Game.getScreen().drawImage(
                Game.getScreen().texture("font7wpx"),
                fx,
                fy,
                fontWidth,
                fontHeight,
                (x + (i + 0) * (fontWidth - 1)),
                (y + (0) * fontHeight),
                fontWidth,
                fontHeight
            );
        }
    }

    function* statusView(opt: { saveData: Data.SaveData.SaveData, player: Unit.Player, floor: number, upperdraw: () => void }): IterableIterator<any> {
        const closeButton = {
            x: Game.getScreen().offscreenWidth - 20,
            y: 20,
            radius: 10
        };

        this.draw = () => {
            opt.upperdraw();
            //Game.getScreen().fillStyle = 'rgba(255,255,255,0.5)';
            //Game.getScreen().fillRect(20,
            //    20,
            //    Game.getScreen().offscreenWidth - 40,
            //    Game.getScreen().offscreenHeight - 40);

            //// 閉じるボタン
            //Game.getScreen().save();
            //Game.getScreen().beginPath();
            //Game.getScreen().strokeStyle = 'rgba(255,255,255,1)';
            //Game.getScreen().lineWidth = 6;
            //Game.getScreen().ellipse(closeButton.x, closeButton.y, closeButton.radius, closeButton.radius, 0, 0, 360);
            //Game.getScreen().moveTo(closeButton.x - Math.sqrt(2) * closeButton.radius / 2,
            //    closeButton.y - Math.sqrt(2) * closeButton.radius / 2);
            //Game.getScreen().lineTo(closeButton.x + Math.sqrt(2) * closeButton.radius / 2,
            //    closeButton.y + Math.sqrt(2) * closeButton.radius / 2);
            //Game.getScreen().moveTo(closeButton.x - Math.sqrt(2) * closeButton.radius / 2,
            //    closeButton.y + Math.sqrt(2) * closeButton.radius / 2);
            //Game.getScreen().lineTo(closeButton.x + Math.sqrt(2) * closeButton.radius / 2,
            //    closeButton.y - Math.sqrt(2) * closeButton.radius / 2);
            //Game.getScreen().stroke();
            //Game.getScreen().strokeStyle = 'rgba(128,255,255,1)';
            //Game.getScreen().lineWidth = 3;
            //Game.getScreen().stroke();
            //Game.getScreen().restore();

            // ステータス(前衛)
            {
            const left = ~~((Game.getScreen().offscreenWidth - 190) / 2);
            const top = ~~((Game.getScreen().offscreenHeight - 121*2) / 2);
            Game.getScreen().drawImage(
                Game.getScreen().texture("status"),
                0, 0, 190, 121,
                left,
                top,
                190, 121
            );
            Game.getScreen().font = "10px 'PixelMplus10-Regular'";
            Game.getScreen().fillStyle = `rgb(0,0,0)`;
            Game.getScreen().textAlign = "left";
            Game.getScreen().textBaseline = "top";
            Game.getScreen().fillText(opt.player.getForward().name, left + 110, top + 36);
            showStatusText(`${opt.player.getForward().hp}/${opt.player.getForward().hpMax}`,left+85,top+56);
            showStatusText(`${opt.player.getForward().mp}/${opt.player.getForward().mpMax}`, left + 145, top + 56);
            showStatusText(`${opt.player.getForward().equips.reduce<Data.Item.ItemBoxEntry, number>((s, [v, k]) => s + (v == null ? 0 : Data.Item.get(v.id).atk), 0)}`, left + 85, top + 64);
            showStatusText(`${opt.player.getForward().equips.reduce<Data.Item.ItemBoxEntry, number>((s, [v, k]) => s + (v == null ? 0 : Data.Item.get(v.id).def), 0)}`,left+145,top+64);
            }
            // 後衛
            {
            const left = ~~((Game.getScreen().offscreenWidth - 190) / 2);
            const top = ~~((Game.getScreen().offscreenHeight - 121*2) / 2) + 121;
            Game.getScreen().drawImage(
                Game.getScreen().texture("status"),
                0, 0, 190, 121,
                left,
                top,
                190, 121
            );
            Game.getScreen().font = "10px 'PixelMplus10-Regular'";
            Game.getScreen().fillStyle = `rgb(0,0,0)`;
            Game.getScreen().textAlign = "left";
            Game.getScreen().textBaseline = "top";
            Game.getScreen().fillText(opt.player.getBackward().name, left + 110, top + 36);
            showStatusText(`${opt.player.getBackward().hp}/${opt.player.getBackward().hpMax}`,left+85,top+56);
            showStatusText(`${opt.player.getBackward().mp}/${opt.player.getBackward().mpMax}`,left+145,top+56);
            showStatusText(`${opt.player.getBackward().equips.reduce<Data.Item.ItemBoxEntry, number>((s, [v, k]) => s + (v == null ? 0 : Data.Item.get(v.id).atk),0)}`,left+85,top+64);
            showStatusText(`${opt.player.getBackward().equips.reduce<Data.Item.ItemBoxEntry, number>((s, [v, k]) => s + (v == null ? 0 : Data.Item.get(v.id).def),0)}`,left+145,top+64);
            }
            //opt.player.equips.forEach((e, i) => {
            //    Game.getScreen().fillText(`${e.name}`, left + 12, top + 144 + 12 * i);
            //})


        }
        yield waitClick({
            end: (x, y) => {
                this.next();
            }
        });
        Game.getSceneManager().pop();
        return;
    }

    function* itemView(opt : {
        saveData: Data.SaveData.SaveData;
        player: Unit.Player;
        floor: number;
        upperdraw: () => void;
    }) : IterableIterator<any> {
        const dispatcher = new Game.GUI.UIDispatcher();

        const caption = new Game.GUI.TextBox({
            left: 1,
            top: 1,
            width: 250,
            height: 14,
            text: "道具箱",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.add(caption);

        let selectedItem = -1;
        const listBox = new Game.GUI.ListBox({
            left: 8,
            top: 46-28,
            width: 112 + 1,
            height: 11 * 16,
            lineHeight: 16,
            getItemCount: () => opt.saveData.ItemBox.length,
            drawItem: (left: number, top: number, width: number, height: number, index: number) => {
                const itemData = Data.Item.get(opt.saveData.ItemBox[index].id);
                if (selectedItem == index) {
                    Game.getScreen().fillStyle = `rgb(24,196,195)`;
                } else {
                    Game.getScreen().fillStyle = `rgb(24,133,196)`;
                }
                Game.getScreen().fillRect(left, top, width, height);
                Game.getScreen().strokeStyle = `rgb(12,34,98)`;
                Game.getScreen().lineWidth = 1;
                Game.getScreen().strokeRect(left, top, width, height);
                Game.getScreen().font = "10px 'PixelMplus10-Regular'";
                Game.getScreen().fillStyle = `rgb(255,255,255)`;
                Game.getScreen().textAlign = "left";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(itemData.name, left + 3, top + 3);
                Game.getScreen().textAlign = "right";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(itemData.price + "G", left + 112-3, top + 3);
            }
        });
        dispatcher.add(listBox);

        listBox.click = (x: number, y: number) => {
            selectedItem = listBox.getItemIndexByPosition(x, y);
            Game.getSound().reqPlayChannel("cursor");
        };

        const captionMonay = new Game.GUI.Button({
            left: 131,
            top: 46-28,
            width: 112,
            height: 16,
            text: () => `所持金：${('            ' + opt.saveData.Money + ' G').substr(-13)}`,
        });
        dispatcher.add(captionMonay);

        //const hoverSlider = new Game.GUI.HorizontalSlider({
        //    left: 131 + 14,
        //    top: 80,
        //    width: 112 - 28,
        //    height: 16,
        //    sliderWidth: 5,
        //    updownButtonWidth: 10,
        //    edgeColor: `rgb(12,34,98)`,
        //    color: `rgb(24,133,196)`,
        //    font: "10px 'PixelMplus10-Regular'",
        //    fontColor: `rgb(255,255,255)`,
        //    minValue: 0,
        //    maxValue: 100,
        //});
        //dispatcher.add(hoverSlider);
        //const btnSliderDown = new Game.GUI.Button({
        //    left: 131,
        //    top: 80,
        //    width: 14,
        //    height: 16,
        //    text: "－",
        //});
        //dispatcher.add(btnSliderDown);
        //btnSliderDown.click = (x: number, y: number) => {
        //    hoverSlider.value -= 1;
        //    hoverSlider.update();
        //    Game.getSound().reqPlayChannel("cursor");
        //};
        //const btnSliderUp = new Game.GUI.Button({
        //    left: 243 - 14,
        //    top: 80,
        //    width: 14,
        //    height: 16,
        //    text: "＋",
        //});
        //dispatcher.add(btnSliderUp);
        //btnSliderUp.click = (x: number, y: number) => {
        //    hoverSlider.value += 1;
        //    hoverSlider.update();
        //    Game.getSound().reqPlayChannel("cursor");
        //};
        //const captionSellCount = new Game.GUI.Button({
        //    left: 131,
        //    top: 64,
        //    width: 112,
        //    height: 24,
        //    text: () => {
        //        if (selectedItem == -1) {
        //            return '';
        //        } else {
        //            return `数量：${('  ' + hoverSlider.value).substr(-2)} / 所有：${('  ' + saveData.ItemBox[selectedItem].count).substr(-2)}\n価格：${('  ' + (Data.Item.get(saveData.ItemBox[selectedItem].id).price * hoverSlider.value)).substr(-8) + "G"}`;
        //        }
        //    },
        //});
        //dispatcher.add(captionSellCount);

        const btnDoUse = new Game.GUI.Button({
            left: 131,
            top: 110,
            width: 112,
            height: 16,
            text: "使用",
        });
        dispatcher.add(btnDoUse);

        btnDoUse.click = (x: number, y: number) => {
            if (selectedItem !== -1) {
                const itemData = Data.Item.get(opt.saveData.ItemBox[selectedItem].id);
                if (itemData.useToPlayer != null) {
                    // プレイヤー選択画面にこのアイテムを渡して一時遷移
                }
            }
            Game.getSound().reqPlayChannel("cursor");
        };

        const btnItemData = new Game.GUI.Button({
            left: 131,
            top: 142,
            width: 112,
            height: 60,
            text: () => {
                if (selectedItem == -1) {
                    return "";
                }
                const itemData = Data.Item.get(opt.saveData.ItemBox[selectedItem].id);
                switch (itemData.kind) {
                    case Data.Item.Kind.Wepon:
                        return `種別：武器\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.Kind.Armor1:
                        return `種別：防具・上半身\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.Kind.Armor2:
                        return `種別：防具・下半身\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.Kind.Accessory:
                        return `種別：アクセサリ\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.Kind.Tool:
                        return `種別：道具`;
                    case Data.Item.Kind.Treasure:
                        return `種別：その他`;
                    default:
                        return "";
                }
            },
        });
        dispatcher.add(btnItemData);

        const btnDescription = new Game.GUI.Button({
            left: 131,
            top: 212,
            width: 112,
            height: 36,
            text: () => {
                if (selectedItem == -1) {
                    return "";
                }
                const itemData = Data.Item.get(opt.saveData.ItemBox[selectedItem].id);
                return itemData.description;
            },
        });
        dispatcher.add(btnDescription);

        const btnExit = new Game.GUI.Button({
            left: 8,
            top: 16 * 11 + 46,
            width: 112,
            height: 16,
            text: "戻る",
        });
        dispatcher.add(btnExit);

        let exitScene = false;
        btnExit.click = (x: number, y: number) => {
            exitScene = true;
            Game.getSound().reqPlayChannel("cursor");
        };


        btnDoUse.visible = btnItemData.visible = btnDescription.visible = false;


        this.draw = () => {
            opt.upperdraw();
            dispatcher.draw();
        }

        yield (delta: number, ms: number) => {
            if (Game.getInput().isDown()) {
                dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isMove()) {
                dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isUp()) {
                dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
            }
            btnItemData.visible = btnDescription.visible = (selectedItem != -1);
            btnDoUse.visible = (selectedItem != -1) && Data.Item.get(opt.saveData.ItemBox[selectedItem].id).useToPlayer != null;
             if (exitScene) {
                this.next();
            }
        };


        Game.getSceneManager().pop();
        return;        
    }

    function* gameOver(opt: { saveData: Data.SaveData.SaveData, player: Unit.Player, floor: number, upperdraw: () => void })  : IterableIterator<any> {
        const fade = new Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
        let fontAlpha: number = 0;
        this.draw = () => {
            opt.upperdraw();
            fade.draw();
            Game.getScreen().fillStyle = `rgba(255,255,255,${fontAlpha})`;
            Game.getScreen().font = "20px 'PixelMplus10-Regular'";
            const shape = Game.getScreen().measureText(`GAME OVER`);
            Game.getScreen().fillText(`GAME OVER`, (Game.getScreen().offscreenWidth - shape.width) / 2, (Game.getScreen().offscreenHeight - 20) / 2);
        }
        yield waitTimeout({
            timeout: 500,
            start: (e, ms) => { fade.startFadeOut(); },
            update: (e, ms) => { fade.update(ms); },
            end: (x, y) => { this.next(); }
        });
        yield waitTimeout({
            timeout: 500,
            start: (e, ms) => { fontAlpha = 0; },
            update: (e, ms) => { fontAlpha = e / 500; },
            end: (x, y) => { fontAlpha = 1; this.next(); }
        });
        yield waitClick({
            end: (x, y) => {
                this.next();
            }
        });
        Game.getSceneManager().pop();
        Game.getSceneManager().push(title);
        return;
    }
}

