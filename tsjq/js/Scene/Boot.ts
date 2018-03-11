namespace Scene {
    export function* boot(): IterableIterator<any> {
        let n: number = 0;
        let reqResource: number = 0;
        let loadedResource: number = 0;
        this.draw = () => {
            Game.getScreen().fillStyle = "rgb(255,255,255)";
            Game.getScreen().fillRect(0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);

            Game.getScreen().save();
            Game.getScreen().translate(Game.getScreen().offscreenWidth / 2, Game.getScreen().offscreenHeight / 2);
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
            Game.getScreen().fillStyle = "rgb(0,0,0)";

            const text = `loading ${loadedResource}/${reqResource}`;
            const size = Game.getScreen().measureText(text);
            Game.getScreen().fillText(text, Game.getScreen().offscreenWidth / 2 - size.width / 2, Game.getScreen().offscreenHeight - 20);
        };
        Promise.all([
            Game.getScreen().loadImage({
                title: "./assets/title.png",
                mapchip: "./assets/mapchip.png",
                charactor: "./assets/charactor.png",
                font7px: "./assets/font7px.png",
            },
                () => { reqResource++; },
                () => { loadedResource++; },
            ),
            Game.getSound().loadSoundsToChannel({
                title: "./assets/sound/title.mp3",
                dungeon: "./assets/sound/dungeon.mp3",
                classroom: "./assets/sound/classroom.mp3",
                kaidan: "./assets/sound/kaidan.mp3",
                atack: "./assets/sound/se_attacksword_1.mp3",
                explosion: "./assets/sound/explosion03.mp3",
            },
                () => { reqResource++; },
                () => { loadedResource++; },
            ).catch((ev) => console.log("failed2", ev)),
            Charactor.Player.loadCharactorConfigs(() => { reqResource++; }, () => { loadedResource++; }),
            Charactor.Monster.loadCharactorConfigs(() => { reqResource++; }, () => { loadedResource++; })
        ]).then(() => {
            Game.getSceneManager().push(title, null);
            this.next();
        });
        yield (delta: number, ms: number) => {
            n = ~(ms / 50);
        };
    }

}