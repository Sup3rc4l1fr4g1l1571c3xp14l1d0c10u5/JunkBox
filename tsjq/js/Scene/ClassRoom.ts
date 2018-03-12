namespace Scene {
    export function* classroom(): IterableIterator<any> {
        let selectedCharactor = -1;
        let selectedCharactorDir = 0;
        let selectedCharactorOffY = 0;
        const fade = new Fade(Game.getScreen().offscreenHeight, Game.getScreen().offscreenHeight);

        this.draw = () => {
            const w = Game.getScreen().offscreenWidth;
            const h = Game.getScreen().offscreenHeight;
            Game.getScreen().save();
            Game.getScreen().fillStyle = "rgb(255,255,255)";
            Game.getScreen().fillRect(0, 0, w, h);

            // 床
            for (let y = 0; y < ~~((w + 23) / 24); y++) {
                for (let x = 0; x < ~~((w + 23) / 24); x++) {
                    Game.getScreen().drawImage(
                        Game.getScreen().texture("mapchip"),
                        0, 0, 24, 24,
                        x * 24, y * 24, 24, 24,
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
                        24,
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
                36,
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
                        47,
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
                        24,
                    );

                }
            }

            fade.draw();
            Game.getScreen().restore();
        };

        {
            Game.getSound().reqPlayChannel("classroom", true);
            yield waitTimeout({
                timeout: 500,
                init: () => { fade.startFadeIn(); },
                update: (e) => { fade.update(e); },
                end: () => {
                    fade.stop();
                    this.next();
                },
            });
        }

        yield waitClick({
            check: (x: number, y: number) => {
                const xx = ~~((x - 12) / 36);
                const yy = ~~((y - 24) / (48 - 7));
                return (0 <= xx && xx < 6 && 0 <= yy && yy < 5);
            },
            end: (x: number, y: number) => {
                Game.getSound().reqPlayChannel("title");
                const xx = ~~((x - 12) / 36);
                const yy = ~~((y - 24) / (48 - 7));
                selectedCharactor = yy * 6 + xx;
                this.next();
            },
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
            end: () => { this.next(); },
        });

        yield waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeOut(); },
            update: (e) => { fade.update(e); },
            end: () => { this.next(); },
        });
        const player = new Charactor.Player({
            charactorId: Charactor.Player.playerConfigs.get("_u" + ("0" + (selectedCharactor + 1)).substr(-2)).id,
            x: 0,
            y: 0,
        });
        Game.getSound().reqStopChannel("classroom");
        Game.getSceneManager().pop();
        Game.getSceneManager().push(dungeon, { player: player, floor: 1 });
    }

}