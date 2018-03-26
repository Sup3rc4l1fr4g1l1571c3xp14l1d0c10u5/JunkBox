namespace Scene {
    export class Title implements Game.Scene.Scene {
        fade : Fade;
        // setup
        constructor() {
            this.fade = new Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            this.draw = this.drawWaitClick;
            this.update = this.updateWaitClick;
        }

        private drawBase(showClickOrTap : boolean) {
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
            this.fade.draw();
            Game.getScreen().restore();
        };

        draw() {}
        update() {}

        drawWaitClick() {
            this.drawBase((~~(Game.getTimer().now / 500) % 2) === 0);
        }
        drawDecide() {
            this.drawBase((~~(Game.getTimer().now / 50) % 2) === 0);
        }

        updateWaitClick() {
            if (Game.getInput().isClick()) {
                Game.getSound().reqPlayChannel("title");
                this.update = this.updateDecide;
                this.draw = this.drawDecide;
            }
        }
        updateDecide() {
            this.update = waitTimeout(500,
                waitFadeOut(
                    this.fade,
                    () => {
                    Data.SaveData.load();
                    Game.getSceneManager().push(new Corridor());
                })
            );
        }

    }
}
