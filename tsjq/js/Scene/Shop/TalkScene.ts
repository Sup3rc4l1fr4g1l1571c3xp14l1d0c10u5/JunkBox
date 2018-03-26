/// <reference path="../../lib/game/eventdispatcher.ts" />
namespace Scene {
    export class TalkScene implements Game.Scene.Scene {
        draw() { }

        update() { }

        constructor() {
            const fade = new Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            const dispatcher = new Game.GUI.UIDispatcher();
            const caption = new Game.GUI.TextBox({
                left: 1,
                top: Game.getScreen().offscreenHeight - 42,
                width: 250,
                height: 42,
                text: "",
                edgeColor: `rgb(12,34,98)`,
                color: `rgb(24,133,196)`,
                font: "10px 'PixelMplus10-Regular'",
                fontColor: `rgb(255,255,255)`,
                textAlign: "left",
                textBaseline: "top",
            });
            dispatcher.add(caption);

            this.draw = () => {
                dispatcher.draw();
                fade.draw();
            }

            const fadein = () => this.update = waitFadeIn(fade, () => talk(0));

            const talk = (index: number) => {
                const texts: string[] = [
                    "【声Ａ】\nこれが今度の実験体かしら。",
                    "【声Ｂ】\nはい、資料によると芋女のＪＫとのことですわ。",
                    "【声Ａ】\nということは、例のルートから･･･ですわね。",
                    "【声Ｂ】\n負債は相当な額だったそうですわ。",
                    "【声Ａ】\n夢破れたりですわね、ふふふ…。\nでも、この実験で生まれ変わりますわ。",
                    "【声Ｂ】\n生きていれば…ですわね、うふふふふふ…。",
                    "【声Ａ】\nそういうことですわね。では、始めましょうか。",
                    "【声Ｂ】\nはい、お姉さま。",
                ];
                caption.text = texts[index];

                this.update = () => {
                    if (Game.getInput().isClick()) {
                        if (++index === texts.length) {
                            fadeout();
                        } else {
                            caption.text = texts[index];
                        }
                    }
                }
            };

            const fadeout = () => this.update = waitFadeOut(fade, () => wait());

            const wait = () => this.update = waitTimeout(500,
                () => {
                    Data.SaveData.itemBox.push({ id: 304, condition: "", count: 1 });
                    Data.SaveData.money = 0;
                    Data.SaveData.save();
                    Game.getSceneManager().pop();
                    Game.getSound().reqPlayChannel("classroom", true);
                    this.update = () => {};
                });

            fadein();

        }
    }
}
