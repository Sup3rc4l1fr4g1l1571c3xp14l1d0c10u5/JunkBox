/// <reference path="../../SpriteAnimation.ts" />
/// <reference path="./StatusSprite.ts" />
/// <reference path="./EditOrganization.ts" />
/// <reference path="./EditEquip.ts" />
namespace Scene.ClassRoom {
    export class Top implements Game.Scene.Scene {
        draw() {}

        update() {}

        constructor() {
            const fade = new Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);

            const dispatcher = new Game.GUI.UIDispatcher();

            const caption = new Game.GUI.TextBox({
                left: 1,
                top: 1,
                width: 250,
                height: 42,
                text: "教室\n探索ペアの編成や生徒の確認ができます。",
                edgeColor: `rgb(12,34,98)`,
                color: `rgb(24,133,196)`,
                font: "10px 'PixelMplus10-Regular'",
                fontColor: `rgb(255,255,255)`,
                textAlign: "left",
                textBaseline: "top",
            });
            dispatcher.add(caption);

            const btnOrganization = new Game.GUI.Button({
                left: 8,
                top: 20 * 0 + 46,
                width: 112,
                height: 16,
                text: "編成",
            });
            dispatcher.add(btnOrganization);
            btnOrganization.click = (x: number, y: number) => {
                Game.getSceneManager().push(new EditOrganization());
                Game.getSound().reqPlayChannel("cursor");
            };

            const btnEquip = new Game.GUI.Button({
                left: 8,
                top: 20 * 1 + 46,
                width: 112,
                height: 16,
                text: "装備変更",
            });
            dispatcher.add(btnEquip);
            btnEquip.click = (x: number, y: number) => {
                Game.getSceneManager().push(new EditEquip());
                Game.getSound().reqPlayChannel("cursor");
            };

            const btnItemBox = new Game.GUI.Button({
                left: 8,
                top: 20 * 2 + 46,
                width: 112,
                height: 16,
                text: "道具箱",
                visible: false
            });
            dispatcher.add(btnItemBox);
            btnItemBox.click = (x: number, y: number) => {
                Game.getSound().reqPlayChannel("cursor");
            };


            const btnExit = new Game.GUI.Button({
                left: 8,
                top: 16 * 11 + 46,
                width: 112,
                height: 16,
                text: "戻る",
            });
            dispatcher.add(btnExit);

            let exitScene = false;
            btnExit.click = (x: number, y: number) => {
                exitScene = true;
                Data.SaveData.save();
                Game.getSound().reqPlayChannel("cursor");
            };

            this.draw = () => {
                Game.getScreen().drawImage(
                    Game.getScreen().texture("classroom"),
                    0,
                    0,
                    Game.getScreen().offscreenWidth,
                    Game.getScreen().offscreenHeight,
                    0,
                    0,
                    Game.getScreen().offscreenWidth,
                    Game.getScreen().offscreenHeight
                );
                dispatcher.draw();
                fade.draw();
            }



            this.update = waitFadeIn(fade, () => {
                this.update = () => {
                    if (Game.getInput().isDown()) {
                        dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
                    }
                    if (Game.getInput().isMove()) {
                        dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
                    }
                    if (Game.getInput().isUp()) {
                        dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
                    }
                    if (exitScene) {
                        this.update = waitFadeOut(fade, () => {
                            this.update = () => {};
                             Game.getSceneManager().pop();
                        });
                    }
                }
            });            
        }
    }
}