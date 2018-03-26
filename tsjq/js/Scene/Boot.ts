namespace Scene {
    export class BootScene implements Game.Scene.Scene {
        private reqResource: number = 0;
        private loadedResource: number = 0;

        constructor() {}

        draw() {
            const n = ~~(Game.getTimer().now / 50);
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

            const text = `loading ${this.loadedResource}/${this.reqResource}`;
            const size = Game.getScreen().measureText(text);
            Game.getScreen().fillText(text,
                Game.getScreen().offscreenWidth / 2 - size.width / 2,
                Game.getScreen().offscreenHeight - 20);
        };

        update() {
            Promise.all([
                Game.getScreen().loadImage({
                        title: "./assets/title.png",
                        mapchip: "./assets/mapchip.png",
                        charactor: "./assets/charactor.png",
                        font7px: "./assets/font7px.png",
                        font7wpx: "./assets/font7wpx.png",
                        menuicon: "./assets/menuicon.png",
                        status: "./assets/status.png",
                        corridorbg: "./assets/corridorbg.png",
                        classroom: "./assets/classroom.png",
                        "drops": "./assets/drops.png",
                        "shop/bg": "./assets/shop/bg.png",
                        "shop/J11": "./assets/shop/J11.png",
                    },
                    () => { this.reqResource++; },
                    () => { this.loadedResource++; },
                ),
                Game.getSound().loadSoundsToChannel({
                        title: "./assets/sound/title.mp3",
                        dungeon: "./assets/sound/dungeon.mp3",
                        classroom: "./assets/sound/classroom.mp3",
                        kaidan: "./assets/sound/kaidan.mp3",
                        atack: "./assets/sound/se_attacksword_1.mp3",
                        explosion: "./assets/sound/explosion03.mp3",
                        cursor: "./assets/sound/cursor.mp3",
                        sen_ge_gusya01: "./assets/sound/sen_ge_gusya01.mp3",
                        boyon1: "./assets/sound/boyon1.mp3",
                        boyoyon1: "./assets/sound/boyoyon1.mp3",
                        meka_ge_reji_op01: "./assets/sound/meka_ge_reji_op01.mp3",
                        coin: "./assets/sound/Cash_Register-Drawer01-1.mp3",
                        open: "./assets/sound/locker-open1.mp3"
                    },
                    () => { this.reqResource++; },
                    () => { this.loadedResource++; },
                ).catch((ev) => console.log("failed2", ev)),
                Data.Monster.initialize(() => { this.reqResource++; }, () => { this.loadedResource++; }),
                Data.Charactor.initialize(() => { this.reqResource++; }, () => { this.loadedResource++; }),
                Promise.resolve().then(() => {
                    this.reqResource++;
                    return new FontFace("PixelMplus10-Regular", "url(./assets/font/PixelMplus10-Regular.woff2)", {})
                        .load();
                }).then((loadedFontFace) => {
                    document.fonts.add(loadedFontFace);
                    this.loadedResource++;
                })
            ]).then(() => {
                Game.getSceneManager().push(new Title());

                //const sd = new IData.SaveData.SaveData();
                //sd.loadGameData();
                //Game.getSceneManager().push(shop, sd);
                //this.next();
            });

            this.update = () => {};
            return;
        }

    }
}
