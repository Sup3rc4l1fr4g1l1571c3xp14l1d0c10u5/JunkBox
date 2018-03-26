/// <reference path="../lib/game/eventdispatcher.ts" />
namespace Scene {

    export class Corridor implements Game.Scene.Scene {
        draw() { }
        update() { }
        constructor() {
            const dispatcher = new Game.GUI.UIDispatcher();
            const fade = new Fade(Game.getScreen().offscreenHeight, Game.getScreen().offscreenHeight);
            let selected: () => void = null;

            const caption = new Game.GUI.TextBox({
                left: 1,
                top: 1,
                width: 250,
                height: 42,
                text: "移動先を選択してください。",
                edgeColor: `rgb(12,34,98)`,
                color: `rgb(24,133,196)`,
                font: "10px 'PixelMplus10-Regular'",
                fontColor: `rgb(255,255,255)`,
                textAlign: "left",
                textBaseline: "top",
            });
            dispatcher.add(caption);

            const btnBuy = new Game.GUI.Button({
                left: 8,
                top: 20 * 0 + 46,
                width: 112,
                height: 16,
                text: "教室",
            });
            dispatcher.add(btnBuy);
            btnBuy.click = (x: number, y: number) => {
                Game.getSound().reqPlayChannel("cursor");
                selected = () => Game.getSceneManager().push(new ClassRoom.Top());
            };

            const btnSell = new Game.GUI.Button({
                left: 8,
                top: 20 * 1 + 46,
                width: 112,
                height: 16,
                text: "購買部",
            });
            dispatcher.add(btnSell);
            btnSell.click = (x: number, y: number) => {
                Game.getSound().reqPlayChannel("cursor");
                selected = () => Game.getSceneManager().push(new Shop());
            };

            const btnDungeon = new Game.GUI.Button({
                left: 8,
                top: 20 * 3 + 46,
                width: 112,
                height: 16,
                text: "迷宮",
            });
            dispatcher.add(btnDungeon);
            btnDungeon.click = (x: number, y: number) => {
                Game.getSound().reqPlayChannel("cursor");
                Game.getSound().reqStopChannel("classroom");
                selected = () => {
                    Game.getSceneManager().pop();
                    Game.getSound().reqStopChannel("classroom");
                    Game.getSceneManager().push(
                        new Dungeon.Top(
                            {
                                player: new Unit.Player(Data.SaveData.findCharactorById(Data.SaveData.forwardCharactor),
                                    Data.SaveData.findCharactorById(Data.SaveData.backwardCharactor)),
                                floor: 1
                            })
                    );
                };
            };

            btnDungeon.enable = Data.SaveData.forwardCharactor != null;

            this.draw = () => {
                Game.getScreen().drawImage(
                    Game.getScreen().texture("corridorbg"),
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

            Game.getSound().reqPlayChannel("classroom", true);

            const main = () => waitFadeIn(fade,
                () => {
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
                        btnDungeon.enable = Data.SaveData.forwardCharactor != null;
                        if (selected != null) {
                            this.update = waitFadeOut(fade,
                                () => {
                                    fade.stop();
                                    selected();
                                    selected = null;
                                    this.update = main();
                                });
                        }
                    }
                });
            this.update = main();
        }
    }
}