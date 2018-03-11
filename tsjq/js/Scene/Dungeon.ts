namespace Scene {
    enum TurnState {
        WaitInput,      // �v���C���[�̍s������(���͑҂�)
        PlayerAction,   // �v���C���[�̍s�����s
        PlayerActionRunning,
        EnemyAI,        // �G�̍s������
        EnemyAction,    // �G�̍s�����s
        EnemyActionRunning,
        EnemyDead,          // �G�̎��S
        EnemyDeadRunning,   // ���S���s
        Move,           // �ړ����s�i�G���������ɍs���j
        MoveRunning,    // �ړ����s�i�G���������ɍs���j
        TurnEnd,   // �^�[���I��
    }

    export function* dungeon(param: { player: Charactor.Player, floor: number }): IterableIterator<any> {
        const player = param.player;
        const floor = param.floor;

        // �}�b�v�T�C�Y�Z�o
        const mapChipW = 30 + floor * 3;
        const mapChipH = 30 + floor * 3;

        // �}�b�v��������
        const mapchipsL1 = new Array2D(mapChipW, mapChipH);
        const layout = Dungeon.generate(
            mapChipW,
            mapChipH,
            (x, y, v) => { mapchipsL1.value(x, y, v ? 0 : 1); });

        // ����
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

        // �����͐�����ɃV���b�t�����Ă���̂ł��̂܂܎��o��

        const rooms = layout.rooms.slice();

        // �J�n�ʒu
        const startPos = rooms[0].getCenter();
        player.x = startPos.x;
        player.y = startPos.y;

        // �K�i�ʒu
        const stairsPos = rooms[1].getCenter();
        mapchipsL1.value(stairsPos.x, stairsPos.y, 10);

        // �����X�^�[�z�u
        let monsters = rooms.splice(2).map((x) => {
            return new Charactor.Monster({
                charactorId: Charactor.Monster.monsterConfigs.get("slime").id,
                x: x.getLeft(),
                y: x.getTop(),
                life: 10
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

        // �J�������X�V
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
                    // �e
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

                    // �����X�^�[
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

                        // �L�����N�^�[
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
            });

            // �X�v���C�g
            sprites.forEach((x) => x.draw(map.camera));

            // �t�F�[�h
            fade.draw();
            Game.getScreen().restore();

            // �o�[�`�����W���C�X�e�B�b�N�̕`��
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

        // �^�[���̏�ԁi�t�F�[�Y�j
        const turnStateStack: Array<[TurnState, any]> = [[TurnState.WaitInput, null]];

        let playerTactics: any = {};
        const monstersTactics: any[] = [];
        yield (delta: number, ms: number) => {
            stateloop: for (; ;) {
                switch (turnStateStack[0][0]) {
                    case TurnState.WaitInput:
                        {
                            // �L�[���͑҂�
                            if (pad.isTouching === false || pad.distance <= 0.4) {
                                player.setAnimation("move", 0);
                                break stateloop;
                            }

                            // �L�[���͂��ꂽ�̂Ńv���C���[�̈ړ�����(5)�͈ړ����Ȃ��B

                            const playerMoveDir = pad.dir8;

                            // �u�s��(Action)�v�Ɓu�ړ�(Move)�v�̎��ʂ��s��

                            // �ړ��悪�N���s�\�̏ꍇ�͑ҋ@�Ƃ���
                            const { x, y } = Array2D.DIR8[playerMoveDir];
                            if (map.layer[0].chips.value(player.x + x, player.y + y) !== 1 && map.layer[0].chips.value(player.x + x, player.y + y) !== 10) {
                                player.setDir(playerMoveDir);
                                break stateloop;
                            }

                            // �ړ���ɓG������ꍇ�́u�s��(Action)�v�A���Ȃ��ꍇ�́u�ړ�(Move)�v
                            const targetMonster =
                                monsters.findIndex((monster) => (monster.x === player.x + x) &&
                                    (monster.y === player.y + y));
                            if (targetMonster !== -1) {
                                // �ړ���ɓG�����遁�u�s��(Action)�v

                                playerTactics = {
                                    type: "action",
                                    moveDir: playerMoveDir,
                                    targetMonster: targetMonster,
                                    startTime: ms,
                                    actionTime: 250,
                                };

                                // �v���C���[�̍s���A�G�̍s���̌���A�G�̍s�������A�ړ����s�̏��ōs��
                                turnStateStack.unshift(
                                    [TurnState.PlayerAction, null],
                                    [TurnState.EnemyAI, null],
                                    [TurnState.EnemyAction, 0],
                                    [TurnState.Move, null],
                                    [TurnState.TurnEnd, null],
                                );
                                continue stateloop;
                            } else {
                                // �ړ���ɓG�͂��Ȃ����u�ړ�(Move)�v

                                playerTactics = {
                                    type: "move",
                                    moveDir: playerMoveDir,
                                    startTime: ms,
                                    actionTime: 250,
                                };

                                // �G�̍s���̌���A�ړ����s�A�G�̍s�������A�̏��ōs���B
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
                        // �v���C���[�̍s���J�n
                        turnStateStack[0][0] = TurnState.PlayerActionRunning;
                        turnStateStack[0][1] = 0;
                        player.setDir(playerTactics.moveDir);
                        player.setAnimation("action", 0);
                        // fallthrough
                    }
                    case TurnState.PlayerActionRunning: {
                        // �v���C���[�̍s����
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
                                    // �G�����S��Ԃɂ���
                                    // explosion
                                    Game.getSound().reqPlayChannel("explosion");
                                    // ���S���������荞�݂ōs�킹��
                                    turnStateStack.splice(1, 0, [TurnState.EnemyDead, playerTactics.targetMonster, 0]);
                                }
                            }
                        }
                        if (rate >= 1) {
                            // �v���C���[�̍s���I��
                            turnStateStack.shift();
                            player.setAnimation("move", 0);
                        }
                        break stateloop;
                    }
                    case TurnState.EnemyAI: {
                        // �G�̍s���̌���

                        // �v���C���[���ړ�����ꍇ�A�ړ���ɂ���Ƒz�肵�ēG�̍s�������肷��
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

                        // �s��(Action)�ƈړ�(Move)�͕������Ȃ��ƈړ��œG���d�Ȃ�

                        // �s��(Action)����G������
                        monsters.forEach((monster, i) => {
                            if (monster.life <= 0) {
                                // ���S��ԂȂ̂ŉ������Ȃ�
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
                                // �ړ���̃v���C���[�ʒu�͌��݈ʒu�ɗאڂ��Ă���̂ŁA�s��(Action)��I��
                                const dir = Array2D.DIR8.findIndex((x) => x.x === dx && x.y === dy);
                                // �G�S�̂̈ړ��s�\���W�Ɏ�����ݒ�
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

                        // �ړ�(Move)����G�̈ړ�������肷��
                        // �ŗǂ̈ړ���Ɉړ��O�̃L�����N�^�[�����݂��邱�Ƃ��l�����Ĉړ��������������Ȃ��Ȃ�܂Ōv�Z���J��Ԃ��B
                        let changed = true;
                        while (changed) {
                            changed = false;
                            monsters.forEach((monster, i) => {
                                const dx = px - monster.x;
                                const dy = py - monster.y;
                                if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) {
                                    if (monstersTactics[i] == null) {
                                        console.error("Action���ׂ��G�̓��삪���肵�Ă��Ȃ�");
                                    }
                                    return;
                                } else if (monstersTactics[i] == null) {
                                    // �ړ���̃v���C���[�ʒu�͌��݈ʒu�ɗאڂ��Ă��Ȃ��̂ŁA�ړ�(Move)��I��
                                    // �Ƃ肠���������킹�헪�œ���

                                    // �ړ���̌��\����ŗǂ̈ړ����I��
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
                                            // �G�S�̂̈ړ��s�\���W�Ɏ�����ݒ�
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
                                    // �ړ��悪�S���ړ��s�\�������̂őҋ@��I��
                                    // �G�S�̂̈ړ��s�\���W�Ɏ�����ݒ�
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
                        // �G�̍s���̌���̏I��
                        turnStateStack.shift();
                        continue stateloop;
                    }
                    case TurnState.EnemyAction: {
                        // �G�̍s���J�n
                        let enemyId = turnStateStack[0][1];
                        while (enemyId < monstersTactics.length) {
                            if (monstersTactics[enemyId].type !== "action") {
                                enemyId++;
                            } else {
                                break;
                            }
                        }
                        // �ړ��ƈႢ�A�s���̏ꍇ�͂P�L�����Âs�����s���B
                        if (enemyId < monstersTactics.length) {
                            monstersTactics[enemyId].startTime = ms;
                            monsters[enemyId].setDir(monstersTactics[enemyId].moveDir);
                            monsters[enemyId].setAnimation("action", 0);
                            turnStateStack[0][0] = TurnState.EnemyActionRunning;
                            turnStateStack[0][1] = enemyId;
                            turnStateStack[0][2] = 0;
                            continue stateloop;
                        } else {
                            // �����������G�����Ȃ�
                            turnStateStack.shift();
                            continue stateloop;
                        }
                    }
                    case TurnState.EnemyActionRunning: {
                        // �G�̍s����
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
                            // �s���I���B���̓G��
                            monsters[enemyId].setAnimation("move", 0);
                            turnStateStack[0][0] = TurnState.EnemyAction;
                            turnStateStack[0][1] = enemyId + 1;
                        }
                        break stateloop;
                    }
                    case TurnState.EnemyDead:
                        {
                            // �G�̎��S�J�n
                            turnStateStack[0][0] = TurnState.EnemyDeadRunning;
                            const enemyId = turnStateStack[0][1];
                            turnStateStack[0][2] = ms;
                            Game.getSound().reqPlayChannel("explosion");
                            monsters[enemyId].setAnimation("dead", 0);
                            // fall through;
                        }
                    case TurnState.EnemyDeadRunning:
                        {
                            // �G�̎��S
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
                        // �ړ��J�n
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
                        // �ړ����s
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
                                    finish = false; // �s���I�����Ă��Ȃ��t���O���Z�b�g
                                }
                            }
                        });
                        if (playerTactics.type === "move") {
                            const rate = (ms - playerTactics.startTime) / playerTactics.actionTime;
                            player.setDir(playerTactics.moveDir);
                            player.setAnimation("move", rate);
                            if (rate < 1) {
                                finish = false; // �s���I�����Ă��Ȃ��t���O���Z�b�g
                            }
                        }
                        if (finish) {
                            // �s���I��
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

                            // ���݈ʒu�̃}�b�v�`�b�v���擾
                            const chip = map.layer[0].chips.value(~~player.x, ~~player.y);
                            if (chip === 10) {
                                // �K�i�Ȃ̂Ŏ��̊K�w�Ɉړ�������B
                                this.next("nextfloor");
                            }

                        }
                        break stateloop;
                    }
                    case TurnState.TurnEnd: {
                        // �^�[���I��
                        turnStateStack.shift();
                        monsters = monsters.filter(x => x.life > 0);
                        break stateloop;
                    }
                }
                break;
            }

            // �J�������X�V
            map.update({
                viewpoint: {
                    x: (player.x * map.gridsize.width + player.offx) + map.gridsize.width / 2,
                    y: (player.y * map.gridsize.height + player.offy) + map.gridsize.height / 2,
                },
                viewwidth: Game.getScreen().offscreenWidth,
                viewheight: Game.getScreen().offscreenHeight,
            },
            );

            // �X�v���C�g���X�V
            sprites = sprites.filter((x) => {
                return !x.update(delta, ms);
            });

            updateLighting((v: number) => v === 1 || v === 10);

            if (Game.getInput().isClick() && Game.getScreen().pagePointContainScreen(Game.getInput().pageX, Game.getInput().pageY)) {
                Game.getSceneManager().push(mapview, { map: map, player: player });
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
        Game.getSceneManager().push(dungeon, { player, floor: floor + 1 });

    }

}