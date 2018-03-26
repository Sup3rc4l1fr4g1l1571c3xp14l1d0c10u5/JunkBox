namespace Scene.Dungeon {
    export function* gameOver(opt: { player: Unit.Player, floor: number, upperdraw: () => void })  : IterableIterator<any> {
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
            start: (e) => { fade.startFadeOut(); },
            update: (e) => { fade.update(Game.getTimer().now); },
            end: (x) => { this.next(); }
        });
        yield waitTimeout({
            timeout: 500,
            start: (e) => { fontAlpha = 0; },
            update: (e) => { fontAlpha = e / 500; },
            end: (e) => { fontAlpha = 1; this.next(); }
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