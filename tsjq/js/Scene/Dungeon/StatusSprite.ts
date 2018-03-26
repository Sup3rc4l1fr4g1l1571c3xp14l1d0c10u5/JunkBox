namespace Scene.Dungeon {
    export enum DrawMode {
        Normal,
        Selected,
        Disable
    }

    export class StatusSprite extends SpriteAnimation.Animator {
        constructor(public data: Unit.MemberStatus) {
            super(data.spriteSheet);
        }
    }

    export function drawStatusSprite(charactorData: StatusSprite, drawMode: DrawMode, left: number, top: number, width: number, height: number, anim: number) {
        if (drawMode == DrawMode.Selected) {
            Game.getScreen().fillStyle = `rgb(24,196,195)`;
        } else if (drawMode == DrawMode.Disable) {
            Game.getScreen().fillStyle = `rgb(133,133,133)`;
        } else {
            Game.getScreen().fillStyle = `rgb(24,133,196)`;
        }
        Game.getScreen().fillRect(left, top, width, height);
        Game.getScreen().strokeStyle = `rgb(12,34,98)`;
        Game.getScreen().lineWidth = 1;
        Game.getScreen().strokeRect(left, top, width, height);

        if (charactorData != null) {
            charactorData.setDir(2);
            charactorData.setAnimation("move", anim / 1000);
            const animFrame = charactorData.spriteSheet.getAnimationFrame(charactorData.animName, charactorData.animFrame);
            const sprite = charactorData.spriteSheet.gtetSprite(animFrame.sprite);

            // キャラクター
            Game.getScreen().drawImage(
                charactorData.spriteSheet.getSpriteImage(sprite),
                sprite.left,
                sprite.top,
                sprite.width,
                sprite.height,
                left - 4,
                top,
                sprite.width,
                sprite.height
            );

            Game.getScreen().font = "10px 'PixelMplus10-Regular'";
            Game.getScreen().fillStyle = `rgb(255,255,255)`;
            Game.getScreen().textAlign = "left";
            Game.getScreen().textBaseline = "top";
            Game.getScreen().fillText(Data.Charactor.get(charactorData.data.id).name, left + 48 - 8, top + 3 + 12 * 0);
            Game.getScreen().fillText(`HP:${charactorData.data.hp}/${charactorData.data.hpMax}`, left + 48 - 8, top + 3 + 12 * 1);
            Game.getScreen().fillText(`MP:${charactorData.data.mp}/${charactorData.data.mpMax}`, left + 48 - 8, top + 3 + 12 * 2);
            Game.getScreen().fillText(`ATK:${charactorData.data.equips.reduce<Data.Item.ItemBoxEntry, number>((s, [v, k]) => s + (v == null ? 0 : Data.Item.get(v.id).atk), 0)} DEF:${charactorData.data.equips.reduce<Data.Item.ItemBoxEntry, number>((s, [v, k]) => s + (v == null ? 0 : Data.Item.get(v.id).def), 0)}`, left + 48 - 8, top + 12 * 3);
        }
    }

}