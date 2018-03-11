interface ISprite {
    update(ms: number, delta: number): boolean;
    draw(camera: Dungeon.Camera): void;
}

function createShowDamageSprite(start: number, dmg: number, getpos: () => IPoint): ISprite {
    const damage = "" + ~~dmg;
    let elapse = 0;
    const fontWidth: number = 5;
    const fontHeight: number = 7;
    return {
        update: (delta: number, ms: number): boolean => {
            elapse = ms - start;
            return (elapse > 500);
        },
        draw: (camera: Dungeon.Camera): void => {
            const { x: sx, y: sy } = getpos();

            const xx = sx - camera.left;
            const yy = sy - camera.top;

            const len = damage.length;
            const offx = -(len) * (fontWidth - 1) / 2;
            const offy = 0;
            for (let i = 0; i < damage.length; i++) {
                const rad = Math.min(elapse - i * 20, 200);
                if (rad < 0) { continue; }
                const dy = Math.sin(rad * Math.PI / 200) * - 7; // 7 = ’µ‚Ëã‚ª‚é‚‚³
                if (0 <= xx + (i + 1) * fontWidth && xx + (i + 0) * fontWidth < Game.getScreen().offscreenWidth &&
                    0 <= yy + (1 * fontHeight) && yy + (0 * fontHeight) < Game.getScreen().offscreenHeight) {
                    Game.getScreen().drawImage(
                        Game.getScreen().texture("font7px"),
                        ~~damage[i] * fontWidth,
                        0,
                        fontWidth,
                        fontHeight,
                        (xx + (i + 0) * (fontWidth - 1)) + offx,
                        (yy + (0) * fontHeight) + offy + dy,
                        fontWidth,
                        fontHeight
                    );
                }
            }
        },
    };
}
