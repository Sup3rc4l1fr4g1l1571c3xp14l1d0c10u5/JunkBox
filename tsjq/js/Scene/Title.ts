        namespace Scene {
    export function* title(): IterableIterator<any> {
        // setup
        let showClickOrTap = false;

        const fade = new Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);

        this.draw = () => {
            const w = Game.getScreen().offscreenWidth;
            const h = Game.getScreen().offscreenHeight;
            Game.getScreen().save();
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
            update: (e) => { showClickOrTap = (~~(Game.getTimer().now / 500) % 2) === 0; },
            check: () => true,
            end: () => {
                Game.getSound().reqPlayChannel("title");
                this.next();
            },
        });

        yield waitTimeout({
            timeout: 1000,
            update: (e) => { showClickOrTap = (~~(Game.getTimer().now / 50) % 2) === 0; },
            end: () => this.next(),
        });

        yield waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeOut(); },
            update: (e) => { fade.update(e); showClickOrTap = (~~(Game.getTimer().now / 50) % 2) === 0; },
            end: () => {
                Data.SaveData.load();
                Game.getSceneManager().push(corridor);
                this.next();
            },
        });
    }
}