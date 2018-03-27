namespace Scene.Dungeon {
    export class GameOver implements Game.Scene.Scene {
        draw() {}
        update() {}

        constructor(opt: { player: Unit.Party, floor: number, upperdraw: () => void }) {
            const fade = new Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            let fontAlpha: number = 0;
            this.draw = () => {
                opt.upperdraw();
                fade.draw();
                Game.getScreen().fillStyle = `rgba(255,255,255,${fontAlpha})`;
                Game.getScreen().font = "20px 'PixelMplus10-Regular'";
                const shape = Game.getScreen().measureText(`GAME OVER`);
                Game.getScreen().fillText(`GAME OVER`,
                    (Game.getScreen().offscreenWidth - shape.width) / 2,
                    (Game.getScreen().offscreenHeight - 20) / 2);
            }

            this.update = waitFadeOut(
                fade,
                () => {
                    this.draw = () => {
                        Game.getScreen().fillStyle = `rgb(0,0,0)`;
                        Game.getScreen().fillRect(
                            0,
                            0,
                            Game.getScreen().offscreenWidth,
                            Game.getScreen().offscreenHeight);
                        Game.getScreen().fillStyle = `rgba(255,255,255,${fontAlpha})`;
                        Game.getScreen().font = "20px 'PixelMplus10-Regular'";
                        const shape = Game.getScreen().measureText(`GAME OVER`);
                        Game.getScreen().fillText(`GAME OVER`,
                            (Game.getScreen().offscreenWidth - shape.width) / 2,
                            (Game.getScreen().offscreenHeight - 20) / 2);
                    };

                    this.update = waitTimeout(
                        500,
                        () => this.update = waitClick(() => {
                            Game.getSceneManager().pop();
                            Game.getSceneManager().push(new Title());
                        }),
                        (e) => { fontAlpha = Math.min(e, 500) / 500; }
                    );
                }
            );
            return;
        }
    }
}
