namespace Scene {
    enum TurnState {
        WaitInput,      // プレイヤーの行動決定(入力待ち)
        PlayerAction,   // プレイヤーの行動実行
        PlayerActionRunning,
        EnemyAI,        // 敵の行動決定
        EnemyAction,    // 敵の行動実行
        EnemyActionRunning,
        EnemyDead,          // 敵の死亡
        EnemyDeadRunning,   // 死亡実行
        Move,           // 移動実行（敵味方同時に行う）
        MoveRunning,    // 移動実行（敵味方同時に行う）
        TurnEnd,   // ターン終了
    }

    export function* dungeon(param: { player: Charactor.Player, floor: number }): IterableIterator<any> {
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
                        : mapchipsL1.value(x, y - 1),
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
            return new Charactor.Monster({
                charactorId: Charactor.Monster.monsterConfigs.get("slime").id,
                x: x.getLeft(),
                y: x.getTop(),
                life: 10,
                maxLife: 10
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

        let sprites: ISprite[] = [];

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
                        Math.PI * 2,
                    );
                    Game.getScreen().fill();

                    // モンスター
                    const camera: Dungeon.Camera = map.camera;
                    monsters.forEach((monster) => {
                        const xx = monster.x - camera.chipLeft;
                        const yy = monster.y - camera.chipTop;
                        if ((0 <= xx && xx < Game.getScreen().offscreenWidth / 24) && (0 <= yy && yy < Game.getScreen().offscreenHeight / 24)) {
                            const animFrame = monster.spriteSheet.getAnimationFrame(monster.animName, monster.animFrame);
                            const sprite = monster.spriteSheet.gtetSprite(animFrame.sprite);
                            const dx = xx * map.gridsize.width + camera.chipOffX + monster.offx + sprite.offsetX + animFrame.offsetX;
                            const dy = yy * map.gridsize.height + camera.chipOffY + monster.offy + sprite.offsetY + animFrame.offsetY;

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
                        if ((0 <= xx && xx < Game.getScreen().offscreenWidth / 24) && (0 <= yy && yy < Game.getScreen().offscreenHeight / 24)) {
                            const animFrame = monster.spriteSheet.getAnimationFrame(monster.animName, monster.animFrame);
                            const sprite = monster.spriteSheet.gtetSprite(animFrame.sprite);
                            const dx = xx * map.gridsize.width + camera.chipOffX + monster.offx + sprite.offsetX + animFrame.offsetX;
                            const dy = yy * map.gridsize.height + camera.chipOffY + monster.offy + sprite.offsetY + animFrame.offsetY;

                            Game.getScreen().fillStyle = 'rgb(255,0,0)';
                            Game.getScreen().fillRect(
                                dx,
                                dy + sprite.height - 1,
                                sprite.width,
                                1
                            );
                            Game.getScreen().fillStyle = 'rgb(0,255,0)';
                            Game.getScreen().fillRect(
                                dx,
                                dy + sprite.height - 1,
                                ~~(sprite.width * monster.life / monster.maxLife),
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
                            cameraLocalPx - sprite.width / 2 + /*player.offx + */sprite.offsetX + animFrame.offsetX,
                            cameraLocalPy - sprite.height / 2 + /*player.offy + */sprite.offsetY + animFrame.offsetY + sprite.height - 1,
                            sprite.width,
                            1
                        );
                        Game.getScreen().fillStyle = 'rgb(0,255,0)';
                        Game.getScreen().fillRect(
                            cameraLocalPx - sprite.width / 2 + /*player.offx + */sprite.offsetX + animFrame.offsetX,
                            cameraLocalPy - sprite.height / 2 + /*player.offy + */sprite.offsetY + animFrame.offsetY + sprite.height - 1,
                            ~~(sprite.width * 1 / 1),
                            1
                        );
                    }
                }
            });

            // スプライト
            sprites.forEach((x) => x.draw(map.camera));

            draw7pxFont("12F | LV:99 | HP:100 | MP:100 | GOLD:200", 0, 0);

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
                    Math.PI * 2,
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
                    Math.PI * 2,
                );
                Game.getScreen().fill();
            }

        };

        yield waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeIn(); },
            update: (e) => { fade.update(e); updateLighting((v: number) => v === 1 || v === 10); },
            end: () => { this.next(); },
        });

        onPointerHook();

        // ターンの状態（フェーズ）
        const turnStateStack: Array<[TurnState, any]> = [[TurnState.WaitInput, null]];

        let playerTactics: any = {};
        const monstersTactics: any[] = [];
        yield (delta: number, ms: number) => {
            stateloop: for (; ;) {
                switch (turnStateStack[0][0]) {
                    case TurnState.WaitInput:
                        {
                            // キー入力待ち
                            if (pad.isTouching === false || pad.distance <= 0.4) {
                                player.setAnimation("move", 0);
                                break stateloop;
                            }

                            // キー入力されたのでプレイヤーの移動方向(5)は移動しない。

                            const playerMoveDir = pad.dir8;

                            // 「行動(Action)」と「移動(Move)」の識別を行う

                            // 移動先が侵入不可能の場合は待機とする
                            const { x, y } = Array2D.DIR8[playerMoveDir];
                            if (map.layer[0].chips.value(player.x + x, player.y + y) !== 1 && map.layer[0].chips.value(player.x + x, player.y + y) !== 10) {
                                player.setDir(playerMoveDir);
                                break stateloop;
                            }

                            // 移動先に敵がいる場合は「行動(Action)」、いない場合は「移動(Move)」
                            const targetMonster =
                                monsters.findIndex((monster) => (monster.x === player.x + x) &&
                                    (monster.y === player.y + y));
                            if (targetMonster !== -1) {
                                // 移動先に敵がいる＝「行動(Action)」

                                playerTactics = {
                                    type: "action",
                                    moveDir: playerMoveDir,
                                    targetMonster: targetMonster,
                                    startTime: ms,
                                    actionTime: 250,
                                };

                                // プレイヤーの行動、敵の行動の決定、敵の行動処理、移動実行の順で行う
                                turnStateStack.unshift(
                                    [TurnState.PlayerAction, null],
                                    [TurnState.EnemyAI, null],
                                    [TurnState.EnemyAction, 0],
                                    [TurnState.Move, null],
                                    [TurnState.TurnEnd, null],
                                );
                                continue stateloop;
                            } else {
                                // 移動先に敵はいない＝「移動(Move)」

                                playerTactics = {
                                    type: "move",
                                    moveDir: playerMoveDir,
                                    startTime: ms,
                                    actionTime: 250,
                                };

                                // 敵の行動の決定、移動実行、敵の行動処理、の順で行う。
                                turnStateStack.unshift(
                                    [TurnState.EnemyAI, null],
                                    [TurnState.Move, null],
                                    [TurnState.EnemyAction, 0],
                                    [TurnState.TurnEnd, null],
                                );
                                continue stateloop;
                            }

                        }

                    case TurnState.PlayerAction: {
                        // プレイヤーの行動開始
                        turnStateStack[0][0] = TurnState.PlayerActionRunning;
                        turnStateStack[0][1] = 0;
                        player.setDir(playerTactics.moveDir);
                        player.setAnimation("action", 0);
                        // fallthrough
                    }
                    case TurnState.PlayerActionRunning: {
                        // プレイヤーの行動中
                        const rate = (ms - playerTactics.startTime) / playerTactics.actionTime;
                        player.setAnimation("action", rate);
                        if (rate > 0.5 && turnStateStack[0][1] === 0) {
                            const targetMonster: Charactor.Monster = monsters[playerTactics.targetMonster];
                            turnStateStack[0][1] = 1;
                            Game.getSound().reqPlayChannel("atack");
                            sprites.push(createShowDamageSprite(
                                ms,
                                5,
                                () => {
                                    return {
                                        x: targetMonster.offx + targetMonster.x * map.gridsize.width + map.gridsize.width / 2,
                                        y: targetMonster.offy + targetMonster.y * map.gridsize.height + map.gridsize.height / 2
                                    };
                                }
                            ));
                            if (targetMonster.life > 0) {
                                targetMonster.life -= 5;
                                if (targetMonster.life <= 0) {
                                    targetMonster.life = 0;
                                    // 敵を死亡状態にする
                                    // explosion
                                    Game.getSound().reqPlayChannel("explosion");
                                    // 死亡処理を割り込みで行わせる
                                    turnStateStack.splice(1, 0, [TurnState.EnemyDead, playerTactics.targetMonster, 0]);
                                }
                            }
                        }
                        if (rate >= 1) {
                            // プレイヤーの行動終了
                            turnStateStack.shift();
                            player.setAnimation("move", 0);
                        }
                        break stateloop;
                    }
                    case TurnState.EnemyAI: {
                        // 敵の行動の決定

                        // プレイヤーが移動する場合、移動先にいると想定して敵の行動を決定する
                        let px = player.x;
                        let py = player.y;
                        if (playerTactics.type === "move") {
                            const off = Array2D.DIR8[playerTactics.moveDir];
                            px += off.x;
                            py += off.y;
                        }

                        const cannotMoveMap = new Array2D(map.width, map.height, 0);
                        monstersTactics.length = monsters.length;
                        monstersTactics.fill(null);

                        // 行動(Action)と移動(Move)は分離しないと移動で敵が重なる

                        // 行動(Action)する敵を決定
                        monsters.forEach((monster, i) => {
                            if (monster.life <= 0) {
                                // 死亡状態なので何もしない
                                monstersTactics[i] = {
                                    type: "dead",
                                    moveDir: 5,
                                    startTime: 0,
                                    actionTime: 250,
                                };
                                return;
                            }
                            const dx = px - monster.x;
                            const dy = py - monster.y;
                            if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) {
                                // 移動先のプレイヤー位置は現在位置に隣接しているので、行動(Action)を選択
                                const dir = Array2D.DIR8.findIndex((x) => x.x === dx && x.y === dy);
                                // 敵全体の移動不能座標に自分を設定
                                cannotMoveMap.value(monster.x, monster.y, 1);
                                monstersTactics[i] = {
                                    type: "action",
                                    moveDir: dir,
                                    startTime: 0,
                                    actionTime: 250,
                                };
                                return;
                            } else {
                                return;  // skip
                            }
                        });

                        // 移動(Move)する敵の移動先を決定する
                        // 最良の移動先に移動前のキャラクターが存在することを考慮して移動処理が発生しなくなるまで計算を繰り返す。
                        let changed = true;
                        while (changed) {
                            changed = false;
                            monsters.forEach((monster, i) => {
                                const dx = px - monster.x;
                                const dy = py - monster.y;
                                if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) {
                                    if (monstersTactics[i] == null) {
                                        console.error("Actionすべき敵の動作が決定していない");
                                    }
                                    return;
                                } else if (monstersTactics[i] == null) {
                                    // 移動先のプレイヤー位置は現在位置に隣接していないので、移動(Move)を選択
                                    // とりあえず軸合わせ戦略で動く

                                    // 移動先の候補表から最良の移動先を選ぶ
                                    const cands = [
                                        [Math.sign(dx), Math.sign(dy)],
                                        (Math.abs(dx) > Math.abs(dy)) ? [0, Math.sign(dy)] : [Math.sign(dx), 0],
                                        (Math.abs(dx) > Math.abs(dy)) ? [Math.sign(dx), 0] : [0, Math.sign(dy)],
                                    ];

                                    for (let j = 0; j < 3; j++) {
                                        const [cx, cy] = cands[j];
                                        const tx = monster.x + cx;
                                        const ty = monster.y + cy;
                                        if ((cannotMoveMap.value(tx, ty) === 0) &&
                                            (map.layer[0].chips.value(tx, ty) === 1 ||
                                                map.layer[0].chips.value(tx, ty) === 10)) {
                                            const dir = Array2D.DIR8.findIndex((x) => x.x === cx && x.y === cy);
                                            // 敵全体の移動不能座標に自分を設定
                                            cannotMoveMap.value(tx, ty, 1);
                                            monstersTactics[i] = {
                                                type: "move",
                                                moveDir: dir,
                                                startTime: ms,
                                                actionTime: 250,
                                            };
                                            changed = true;
                                            return;
                                        }
                                    }
                                    // 移動先が全部移動不能だったので待機を選択
                                    // 敵全体の移動不能座標に自分を設定
                                    cannotMoveMap.value(monster.x, monster.y, 1);
                                    monstersTactics[i] = {
                                        type: "idle",
                                        moveDir: 5,
                                        startTime: ms,
                                        actionTime: 250,
                                    };
                                    changed = true;
                                    return;
                                }
                            });
                        }
                        // 敵の行動の決定の終了
                        turnStateStack.shift();
                        continue stateloop;
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
                        // 移動と違い、行動の場合は１キャラづつ行動を行う。
                        if (enemyId < monstersTactics.length) {
                            monstersTactics[enemyId].startTime = ms;
                            monsters[enemyId].setDir(monstersTactics[enemyId].moveDir);
                            monsters[enemyId].setAnimation("action", 0);
                            turnStateStack[0][0] = TurnState.EnemyActionRunning;
                            turnStateStack[0][1] = enemyId;
                            turnStateStack[0][2] = 0;
                            continue stateloop;
                        } else {
                            // もう動かす敵がいない
                            turnStateStack.shift();
                            continue stateloop;
                        }
                    }
                    case TurnState.EnemyActionRunning: {
                        // 敵の行動中
                        const enemyId = turnStateStack[0][1];

                        const rate = (ms - monstersTactics[enemyId].startTime) / monstersTactics[enemyId].actionTime;
                        monsters[enemyId].setAnimation("action", rate);
                        if (rate > 0.5 && turnStateStack[0][2] === 0) {
                            turnStateStack[0][2] = 1;
                            Game.getSound().reqPlayChannel("atack");
                            sprites.push(createShowDamageSprite(
                                ms,
                                ~~(Math.random() * 10 + 5),
                                () => {
                                    return {
                                        x: player.offx + player.x * map.gridsize.width + map.gridsize.width / 2,
                                        y: player.offy + player.y * map.gridsize.height + map.gridsize.height / 2
                                    };
                                }
                            ));
                        }
                        if (rate >= 1) {
                            // 行動終了。次の敵へ
                            monsters[enemyId].setAnimation("move", 0);
                            turnStateStack[0][0] = TurnState.EnemyAction;
                            turnStateStack[0][1] = enemyId + 1;
                        }
                        break stateloop;
                    }
                    case TurnState.EnemyDead:
                        {
                            // 敵の死亡開始
                            turnStateStack[0][0] = TurnState.EnemyDeadRunning;
                            const enemyId = turnStateStack[0][1];
                            turnStateStack[0][2] = ms;
                            Game.getSound().reqPlayChannel("explosion");
                            monsters[enemyId].setAnimation("dead", 0);
                            // fall through;
                        }
                    case TurnState.EnemyDeadRunning:
                        {
                            // 敵の死亡
                            turnStateStack[0][0] = TurnState.EnemyDeadRunning;
                            const enemyId = turnStateStack[0][1];
                            const diff = ms - turnStateStack[0][2];
                            monsters[enemyId].setAnimation("dead", diff / 250);
                            if (diff >= 250) {
                                turnStateStack.shift();
                            }
                            break stateloop;
                        }
                    case TurnState.Move: {
                        // 移動開始
                        turnStateStack[0][0] = TurnState.MoveRunning;
                        monstersTactics.forEach((monsterTactic, i: number) => {
                            if (monsterTactic.type === "move") {
                                monsters[i].setDir(monsterTactic.moveDir);
                                monsters[i].setAnimation("move", 0);
                                monstersTactics[i].startTime = ms;
                            }
                        });
                        if (playerTactics.type === "move") {
                            player.setDir(playerTactics.moveDir);
                            player.setAnimation("move", 0);
                            playerTactics.startTime = ms;
                        }
                        // fallthrough
                    }
                    case TurnState.MoveRunning: {
                        // 移動実行
                        let finish = true;
                        monstersTactics.forEach((monsterTactic, i: number) => {
                            if (monsterTactic == null) {
                                return;
                            }
                            if (monsterTactic.type === "move") {
                                const rate = (ms - monsterTactic.startTime) / monsterTactic.actionTime;
                                monsters[i].setDir(monsterTactic.moveDir);
                                monsters[i].setAnimation("move", rate);
                                if (rate < 1) {
                                    finish = false; // 行動終了していないフラグをセット
                                }
                            }
                        });
                        if (playerTactics.type === "move") {
                            const rate = (ms - playerTactics.startTime) / playerTactics.actionTime;
                            player.setDir(playerTactics.moveDir);
                            player.setAnimation("move", rate);
                            if (rate < 1) {
                                finish = false; // 行動終了していないフラグをセット
                            }
                        }
                        if (finish) {
                            // 行動終了
                            turnStateStack.shift();

                            monstersTactics.forEach((monsterTactic, i) => {
                                if (monsterTactic.type === "move") {
                                    monsters[i].x += Array2D.DIR8[monsterTactic.moveDir].x;
                                    monsters[i].y += Array2D.DIR8[monsterTactic.moveDir].y;
                                    monsters[i].offx = 0;
                                    monsters[i].offy = 0;
                                    monsters[i].setAnimation("move", 0);
                                }
                            });
                            if (playerTactics.type === "move") {
                                player.x += Array2D.DIR8[playerTactics.moveDir].x;
                                player.y += Array2D.DIR8[playerTactics.moveDir].y;
                                player.offx = 0;
                                player.offy = 0;
                                player.setAnimation("move", 0);
                            }

                            // 現在位置のマップチップを取得
                            const chip = map.layer[0].chips.value(~~player.x, ~~player.y);
                            if (chip === 10) {
                                // 階段なので次の階層に移動させる。
                                this.next("nextfloor");
                            }

                        }
                        break stateloop;
                    }
                    case TurnState.TurnEnd: {
                        // ターン終了
                        turnStateStack.shift();
                        monsters = monsters.filter(x => x.life > 0);
                        break stateloop;
                    }
                }
                break;
            }

            // カメラを更新
            map.update({
                viewpoint: {
                    x: (player.x * map.gridsize.width + player.offx) + map.gridsize.width / 2,
                    y: (player.y * map.gridsize.height + player.offy) + map.gridsize.height / 2,
                },
                viewwidth: Game.getScreen().offscreenWidth,
                viewheight: Game.getScreen().offscreenHeight,
            },
            );

            // スプライトを更新
            sprites = sprites.filter((x) => {
                return !x.update(delta, ms);
            });

            updateLighting((v: number) => v === 1 || v === 10);

            if (Game.getInput().isClick() && Game.getScreen().pagePointContainScreen(Game.getInput().pageX, Game.getInput().pageY)) {
                //Game.getSceneManager().push(mapview, { map: map, player: player });
                Game.getSceneManager().push(statusView, { player: player, floor:floor, upperdraw: this.draw });
            }

        };
        Game.getSound().reqPlayChannel("kaidan");

        yield waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeOut(); },
            update: (e) => { fade.update(e); updateLighting((v: number) => v === 1 || v === 10); },
            end: () => { this.next(); },
        });

        yield waitTimeout({
            timeout: 500,
            end: () => { this.next(); },
        });

        Game.getSceneManager().pop();
        Game.getSceneManager().push(dungeon, { player: player, floor: floor + 1 });

    }


    function* statusView(opt: { player: Charactor.Player, floor: number, upperdraw: () => void }) {
        var closeButton = {
            x: Game.getScreen().offscreenWidth - 20,
            y: 20,
            radius: 10
        };

        this.draw = () => {
            opt.upperdraw();
            Game.getScreen().fillStyle = 'rgba(255,255,255,0.5)';
            Game.getScreen().fillRect(20, 20, Game.getScreen().offscreenWidth - 40, Game.getScreen().offscreenHeight - 40);

            // 閉じるボタン
            Game.getScreen().save();
            Game.getScreen().beginPath();
            Game.getScreen().strokeStyle = 'rgba(255,255,255,1)';
            Game.getScreen().lineWidth = 6;
            Game.getScreen().ellipse(closeButton.x, closeButton.y, closeButton.radius, closeButton.radius, 0, 0, 360);
            Game.getScreen().moveTo(closeButton.x - Math.sqrt(2) * closeButton.radius / 2, closeButton.y - Math.sqrt(2) * closeButton.radius / 2);
            Game.getScreen().lineTo(closeButton.x + Math.sqrt(2) * closeButton.radius / 2, closeButton.y + Math.sqrt(2) * closeButton.radius / 2);
            Game.getScreen().moveTo(closeButton.x - Math.sqrt(2) * closeButton.radius / 2, closeButton.y + Math.sqrt(2) * closeButton.radius / 2);
            Game.getScreen().lineTo(closeButton.x + Math.sqrt(2) * closeButton.radius / 2, closeButton.y - Math.sqrt(2) * closeButton.radius / 2);
            Game.getScreen().stroke();
            Game.getScreen().strokeStyle = 'rgba(128,255,255,1)';
            Game.getScreen().lineWidth = 3;
            Game.getScreen().stroke();
            Game.getScreen().restore();

            // ステータス（ダミー）
            Game.getScreen().fillStyle = 'rgb(0,0,0)';
            Game.getScreen().font = "10px 'PixelMplus10-Regular'";
            Game.getScreen().fillText('ＬＶ：９９', 30, 30 + 11*0);
            Game.getScreen().fillText('ＨＰ：９９９', 30, 30 + 11 * 1);
            Game.getScreen().fillText('ＭＰ：９９９', 30, 30 + 11 * 2);
            Game.getScreen().fillText('筋力：９９９', 30, 30 + 11 * 3);
            Game.getScreen().fillText('体力：９９９', 30, 30 + 11 * 4);
            Game.getScreen().fillText('知性：９９９', 30, 30 + 11 * 5);
            Game.getScreen().fillText('精神：９９９', 30, 30 + 11 * 6);
            Game.getScreen().fillText('器用：９９９', 30, 30 + 11 * 7);
            Game.getScreen().fillText('敏捷：９９９', 30, 30 + 11 * 8);
            Game.getScreen().fillText('幸運：９９９', 30, 30 + 11 * 9);

            Game.getScreen().fillText('アイスソード', 30, 30 + 11 * 12);
            Game.getScreen().fillText('アイアンプレート', 30, 30 + 11 * 13);
            Game.getScreen().fillText('星の腕輪', 30, 30 + 11 * 14);
            Game.getScreen().fillText('韋駄天の靴', 30, 30 + 11 * 15);
            
        }
        yield waitClick({
            end: (x, y) => {
                this.next();
            }
        });
        Game.getSceneManager().pop();
    }

}

