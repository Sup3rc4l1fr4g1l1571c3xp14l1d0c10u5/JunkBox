"use strict";

namespace Particle {

    export interface IParticle {
        update(): boolean;
        draw(camera: MapData.Camera): void;
    }

    export function createShowDamageSprite(start: number, damage: string, getpos: () => IPoint): Particle.IParticle {
        let elapse = 0;
        const fontWidth: number = 5;
        const fontHeight: number = 7;
        return {
            update: (): boolean => {
                elapse = Game.getTimer().now - start;
                return (elapse > 500);
            },
            draw: (camera: MapData.Camera): void => {
                const { x: sx, y: sy } = getpos();

                const xx = sx - camera.left;
                const yy = sy - camera.top;

                const len = damage.length;
                const offx = -(len) * (fontWidth - 1) / 2;
                const offy = 0;
                for (let i = 0; i < damage.length; i++) {
                    const rad = Math.min(elapse - i * 20, 200);
                    if (rad < 0) {
                        continue;
                    }
                    const dy = Math.sin(rad * Math.PI / 200) * - 7; // 7 = 跳ね上がる高さ
                    if (0 <= xx + (i + 1) * fontWidth &&
                        xx + (i + 0) * fontWidth < Game.getScreen().offscreenWidth &&
                        0 <= yy + (1 * fontHeight) &&
                        yy + (0 * fontHeight) < Game.getScreen().offscreenHeight) {
                        const [fx, fy] = Font7px.charDic[damage[i]];
                        Game.getScreen().drawImage(
                            Game.getScreen().texture("font7px"),
                            fx,
                            fy,
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

}