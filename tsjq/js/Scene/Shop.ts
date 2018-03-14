/// <reference path="../lib/game/eventdispatcher.ts" />
namespace Scene {

    function* shopBuyItem(opt: { player: Charactor.Player }) {
        const uiComponents: Game.GUI.UI[] = [];
        const dispatcher = new Game.GUI.UIDispatcher();

        const caption = new Game.GUI.TextBox(1, 1, 250, 42, {
            text: "購買部\nさまざまな武器・アイテムの購入ができます。",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });

        const captionMonay = new Game.GUI.Button(135, 46, 108, 16, {
            text: `所持金：${('            ' + 150 + 'G').substr(-13)}`,
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });

        let exitScene = false;

        const btnExit = new Game.GUI.Button(8, 16 * 9 + 46, 112, 16, {
            text: "戻る",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.onClick(btnExit, (x: number, y: number) => {
            exitScene = true;
            Game.getSound().reqPlayChannel("cursor");
        });

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
        ]

        let selectedItem = -1;
        const listBox = new Game.GUI.ListBox(8, 46, 112, 8 * 16, {
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
                const metrics = Game.getScreen().measureText(itemlist[index]);
                Game.getScreen().textAlign = "left";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(itemlist[index], left + 9, top + 3);
            }
        });

        dispatcher.onSwipe(listBox, (deltaX: number, deltaY: number) => {
            listBox.scrollValue -= deltaY;
            listBox.update();
        });
        dispatcher.onClick(listBox, (x: number, y: number) => {
            selectedItem = listBox.getItemIndexByPosition(x, y);
            Game.getSound().reqPlayChannel("cursor");
        });


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
            )
            caption.draw();
            btnExit.draw();
            listBox.draw();
            captionMonay.draw();
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

    export function* shop(opt: { player: Charactor.Player }) {
        const uiComponents: Game.GUI.UI[] = [];
        const dispatcher = new Game.GUI.UIDispatcher();

        const caption = new Game.GUI.TextBox(1, 1, 250, 42, {
            text: "購買部\nさまざまな武器・アイテムの購入ができます。",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });

        const btnBuy = new Game.GUI.Button(8, 20 * 0 + 46, 112, 16, {
            text: "アイテム購入",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.onClick(btnBuy, (x: number, y: number) => {
            Game.getSceneManager().push(shopBuyItem, opt);
            Game.getSound().reqPlayChannel("cursor");
        });

        const btnSell = new Game.GUI.Button(8, 20 * 1 + 46, 112, 16, {
            text: "アイテム売却",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.onClick(btnBuy, (x: number, y: number) => {
            Game.getSound().reqPlayChannel("cursor");
        });

        const captionMonay = new Game.GUI.Button(135, 46, 108, 16, {
            text: `所持金：${('            ' + 150 + 'G').substr(-13)}`,
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });

        const hoverSlider = new Game.GUI.HorizontalSlider(135+14, 80, 108-28, 16, {
            sliderWidth : 5,
            updownButtonWidth : 10,
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            minValue: 0,
            maxValue: 100,
        });
        dispatcher.onSwipe(hoverSlider, (dx: number, dy: number, x: number, y: number) => {
            hoverSlider.swipe(x);
            hoverSlider.update();
        });
        const btnSliderDown = new Game.GUI.Button(135, 80, 14, 16, {
            text: "－",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.onClick(btnSliderDown, (x: number, y: number) => {
            hoverSlider.value -= 1;
            hoverSlider.update();
            Game.getSound().reqPlayChannel("cursor");
        });
        const btnSliderUp = new Game.GUI.Button(243-14, 80, 14, 16, {
            text: "＋",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.onClick(btnSliderUp, (x: number, y: number) => {
            hoverSlider.value += 1;
            hoverSlider.update();
            Game.getSound().reqPlayChannel("cursor");
        });
        const captionBuyCount = new Game.GUI.Button(135, 64, 108, 16, {
            text: () => `購入数：${('           ' + hoverSlider.value + '個').substr(-12)}`,
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });

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
            )
            caption.draw();
            btnBuy.draw();
            btnSell.draw();
            captionMonay.draw();
            hoverSlider.draw();
            btnSliderDown.draw();
            btnSliderUp.draw();
            captionBuyCount.draw();
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
        };


        Game.getSceneManager().pop();
        Game.getSceneManager().push(title);
    }
}