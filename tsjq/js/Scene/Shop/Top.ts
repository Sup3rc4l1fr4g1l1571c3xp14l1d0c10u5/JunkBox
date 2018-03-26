/// <reference path="./ShopSellItem.ts" />
/// <reference path="./ShopBuyItem.ts" />
/// <reference path="./TalkScene.ts" />
/// <reference path="../../lib/game/eventdispatcher.ts" />
namespace Scene {

    export class Shop implements Game.Scene.Scene {
        draw() { }

        update() { }

        constructor() {
            const fade = new Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);

            const dispatcher = new Game.GUI.UIDispatcher();

            const caption = new Game.GUI.TextBox({
                left: 1,
                top: 1,
                width: 250,
                height: 42,
                text: "購買部\nさまざまな武器・アイテムの購入ができます。",
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
                text: "アイテム購入",
            });
            dispatcher.add(btnBuy);
            btnBuy.click = (x: number, y: number) => {
                Game.getSceneManager().push(new ShopBuyItem());
                Game.getSound().reqPlayChannel("cursor");
            };

            const btnSell = new Game.GUI.Button({
                left: 8,
                top: 20 * 1 + 46,
                width: 112,
                height: 16,
                text: "アイテム売却",
            });
            dispatcher.add(btnSell);
            btnSell.click = (x: number, y: number) => {
                Game.getSceneManager().push(new ShopSellItem());
                Game.getSound().reqPlayChannel("cursor");
            };

            const captionMonay = new Game.GUI.Button({
                left: 131,
                top: 46,
                width: 112,
                height: 16,
                text: () => `所持金：${('            ' + Data.SaveData.money + ' G').substr(-13)}`,
            });
            dispatcher.add(captionMonay);

            const btnMomyu = new Game.GUI.ImageButton({
                left: 151,
                top: 179,
                width: 61,
                height: 31,
                texture: null
            });
            dispatcher.add(btnMomyu);

            let momyu = 0;
            btnMomyu.click = (x: number, y: number) => {
                if (Math.random() > 0.5) {
                    Game.getSound().reqPlayChannel("boyon1");
                } else {
                    Game.getSound().reqPlayChannel("boyoyon1");
                }
                momyu += 500;
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
                Game.getSound().reqPlayChannel("cursor");
            };

            this.draw = () => {
                Game.getScreen().drawImage(
                    Game.getScreen().texture("shop/bg"),
                    0,
                    0,
                    Game.getScreen().offscreenWidth,
                    Game.getScreen().offscreenHeight,
                    0,
                    0,
                    Game.getScreen().offscreenWidth,
                    Game.getScreen().offscreenHeight
                );
                Game.getScreen().drawImage(
                    Game.getScreen().texture("shop/J11"),
                    127 * ((momyu >= 5000) ? 1 : 0),
                    0,
                    127,
                    141,
                    113,
                    83,
                    127,
                    141
                );
                dispatcher.draw();
                fade.draw();
            }

            const fadeIn = () => this.update = waitFadeIn(fade, waitInput);

            const waitInput = () => this.update = () => {
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
                    updateExit();
                }

            };

            const updateExit = () => {
                if (momyu > 0) {
                    Game.getSound().reqPlayChannel("meka_ge_reji_op01");
                    Data.SaveData.money -= momyu;
                    momyu = 0;
                    if (Data.SaveData.money <= -50000) {
                        this.draw = () => {
                            Game.getScreen().drawImage(
                                Game.getScreen().texture("shop/bg"),
                                0,
                                0,
                                Game.getScreen().offscreenWidth,
                                Game.getScreen().offscreenHeight,
                                0,
                                0,
                                Game.getScreen().offscreenWidth,
                                Game.getScreen().offscreenHeight
                            );
                            Game.getScreen().drawImage(
                                Game.getScreen().texture("shop/J11"),
                                0,
                                141,
                                127,
                                141,
                                113,
                                83,
                                127,
                                141
                            );
                            dispatcher.draw();
                            fade.draw();
                        };
                    }
                    this.update = waitTimeout(500,smashExit);

                } else {
                    this.update = waitFadeOut(fade, () => Game.getSceneManager().pop());
                }
            };

            const smashExit = () => {

                Game.getSound().reqStopChannel("classroom");
                Game.getSound().reqPlayChannel("sen_ge_gusya01");

                let rad = 0;
                this.draw = () => {
                    Game.getScreen().translate(0, Math.sin(rad) * Math.cos(rad / 4) * 100)
                    Game.getScreen().drawImage(
                        Game.getScreen().texture("shop/bg"),
                        0,
                        0,
                        Game.getScreen().offscreenWidth,
                        Game.getScreen().offscreenHeight,
                        0,
                        0,
                        Game.getScreen().offscreenWidth,
                        Game.getScreen().offscreenHeight
                    );
                    Game.getScreen().drawImage(
                        Game.getScreen().texture("shop/J11"),
                        0,
                        141,
                        127,
                        141,
                        113,
                        83,
                        127,
                        141
                    );
                    dispatcher.draw();
                    fade.draw();
                };
                fade.startFadeOut();
                this.update = () => {
                    fade.update(Game.getTimer().now);
                    rad = Game.getTimer().now * Math.PI / 25;
                    if (fade.isFinish()) {
                        fade.stop();
                        Game.getSceneManager().pop();
                        Game.getSceneManager().push(new TalkScene());
                        this.update = () => {};
                    }
                };
            };


            fadeIn();

        }
    }
}