/// <reference path="../lib/game/eventdispatcher.ts" />
namespace Scene {

    function* shopBuyItem() {
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

        const captionMonay = new Game.GUI.Button({
            left: 135,
            top: 46,
            width: 108,
            height: 16,
            text: `所持金：${('            ' + 150 + 'G').substr(-13)}`,
        });
        dispatcher.add(captionMonay);

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

        const itemlist: string[] = [
            "みかん",
            "りんご",
            "バナナ",
            "オレンジ",
            "パイナップル",
            "ぼんたん",
            "キウイ",
            "パパイヤ",
            "マンゴー",
            "ココナッツ",
            "ぶどう",
            "なし",
            "あけび",
            "ドラゴンフルーツ",
        ];

        let selectedItem = -1;
        const listBox = new Game.GUI.ListBox({
            left: 8,
            top: 46,
            width: 112,
            height: 10 * 16,
            lineHeight: 16,
            getItemCount: () => itemlist.length,
            drawItem: (left: number, top: number, width: number, height: number, index: number) => {
                if (selectedItem == index) {
                    Game.getScreen().fillStyle = `rgb(24,196,195)`;
                } else {
                    Game.getScreen().fillStyle = `rgb(24,133,196)`;
                }
                Game.getScreen().fillRect(left - 0.5, top + 1 - 0.5, width, height - 2);
                Game.getScreen().strokeStyle = `rgb(12,34,98)`;
                Game.getScreen().lineWidth = 1;
                Game.getScreen().strokeRect(left - 0.5, top + 1 - 0.5, width, height - 2);
                Game.getScreen().font = "10px 'PixelMplus10-Regular'";
                Game.getScreen().fillStyle = `rgb(255,255,255)`;
                Game.getScreen().textAlign = "left";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(itemlist[index], left + 9, top + 3);
            }
        });
        dispatcher.add(listBox);

        listBox.click = (x: number, y: number) => {
            selectedItem = listBox.getItemIndexByPosition(x, y);
            Game.getSound().reqPlayChannel("cursor");
        };


        this.draw = () => {
            Game.getScreen().drawImage(
                Game.getScreen().texture("shop/bg"),
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight,
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight
            );
            Game.getScreen().drawImage(
                Game.getScreen().texture("shop/J11"),
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight,
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight
            );
            dispatcher.draw();
        }

        yield (delta: number, ms: number) => {
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
                this.next();
            }
        };


        Game.getSceneManager().pop();
    }

    export function* shop() {
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
            Game.getSceneManager().push(shopBuyItem);
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
            Game.getSound().reqPlayChannel("cursor");
        };

        const captionMonay = new Game.GUI.Button({
            left: 135,
            top: 46,
            width: 108,
            height: 16,
            text: `所持金：${('            ' + GameData.Money + ' G').substr(-13)}`,
        });
        dispatcher.add(captionMonay);

        const hoverSlider = new Game.GUI.HorizontalSlider({
            left: 135 + 14,
            top: 80,
            width: 108 - 28,
            height: 16,
            sliderWidth: 5,
            updownButtonWidth: 10,
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            minValue: 0,
            maxValue: 100,
        });
        dispatcher.add(hoverSlider);
        const btnSliderDown = new Game.GUI.Button({
            left: 135,
            top: 80,
            width: 14,
            height: 16,
            text: "－",
        });
        dispatcher.add(btnSliderDown);
        btnSliderDown.click = (x: number, y: number) => {
            hoverSlider.value -= 1;
            hoverSlider.update();
            Game.getSound().reqPlayChannel("cursor");
        };
        const btnSliderUp = new Game.GUI.Button({
            left: 243 - 14,
            top: 80,
            width: 14,
            height: 16,
            text: "＋",
        });
        dispatcher.add(btnSliderUp);
        btnSliderUp.click = (x: number, y: number) => {
            hoverSlider.value += 1;
            hoverSlider.update();
            Game.getSound().reqPlayChannel("cursor");
        };
        const captionBuyCount = new Game.GUI.Button({
            left: 135,
            top: 64,
            width: 108,
            height: 16,
            text: () => `購入数：${('           ' + hoverSlider.value + '個').substr(-12)}`,
        });
        dispatcher.add(captionBuyCount);

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
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight,
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight
            );
            Game.getScreen().drawImage(
                Game.getScreen().texture("shop/J11"),
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight,
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight
            );
            dispatcher.draw();
            fade.draw();
        }
                
        yield waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeIn(); },
            update: (e) => { fade.update(e); },
            end: () => {
                fade.stop();
                this.next();
            },
        });

        yield (delta: number, ms: number) => {
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
                this.next();
            }
        };

        yield waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeOut(); },
            update: (e) => { fade.update(e); },
            end: () => {
                fade.stop();
                this.next();
            },
        });

        Game.getSceneManager().pop();
    }
}