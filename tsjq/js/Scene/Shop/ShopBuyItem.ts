/// <reference path="../../lib/game/eventdispatcher.ts" />
namespace Scene {
    export class ShopBuyItem implements Game.Scene.Scene {
        update() {}

        draw() {}

        constructor() {
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

            let selectedItem = -1;
            const listBox = new Game.GUI.ListBox({
                left: 8,
                top: 46,
                width: 112 + 1,
                height: 10 * 16,
                lineHeight: 16,
                getItemCount: () => Data.SaveData.shopStockList.length,
                drawItem: (left: number, top: number, width: number, height: number, index: number) => {
                    const itemData = Data.Item.get(Data.SaveData.shopStockList[index].id);
                    if (selectedItem == index) {
                        Game.getScreen().fillStyle = `rgb(24,196,195)`;
                    } else {
                        Game.getScreen().fillStyle = `rgb(24,133,196)`;
                    }
                    Game.getScreen().fillRect(left, top, width, height);
                    Game.getScreen().strokeStyle = `rgb(12,34,98)`;
                    Game.getScreen().lineWidth = 1;
                    Game.getScreen().strokeRect(left, top, width, height);
                    Game.getScreen().font = "10px 'PixelMplus10-Regular'";
                    Game.getScreen().fillStyle = `rgb(255,255,255)`;
                    Game.getScreen().textAlign = "left";
                    Game.getScreen().textBaseline = "top";
                    Game.getScreen().fillText(itemData.name, left + 3, top + 3);
                    Game.getScreen().textAlign = "right";
                    Game.getScreen().textBaseline = "top";
                    Game.getScreen().fillText(itemData.price + "G", left + 112 - 3, top + 3);
                }
            });
            dispatcher.add(listBox);

            listBox.click = (x: number, y: number) => {
                selectedItem = listBox.getItemIndexByPosition(x, y);
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

            const hoverSlider = new Game.GUI.HorizontalSlider({
                left: 131 + 14,
                top: 90,
                width: 112 - 28,
                height: 16,
                sliderWidth: 5,
                updownButtonWidth: 10,
                edgeColor: `rgb(12,34,98)`,
                color: `rgb(24,133,196)`,
                font: "10px 'PixelMplus10-Regular'",
                fontColor: `rgb(255,255,255)`,
                minValue: 0,
                maxValue: 99,
            });
            dispatcher.add(hoverSlider);
            const btnSliderDown = new Game.GUI.Button({
                left: 131,
                top: 90,
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
                top: 90,
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
                left: 131,
                top: 64,
                width: 112,
                height: 24,
                text: () => {
                    if (selectedItem === -1) {
                        return '';
                    } else {
                        return `数量：${('  ' + hoverSlider.value).substr(-2)} / 在庫：${
                            ('  ' + Data.SaveData.shopStockList[selectedItem].count).substr(-2)}\n価格：${('  ' +
                                (Data.Item.get(Data.SaveData.shopStockList[selectedItem].id).price * hoverSlider.value))
                            .substr(-8) +
                            "G"}`;
                    }
                },
            });
            dispatcher.add(captionBuyCount);

            const btnDoBuy = new Game.GUI.Button({
                left: 131,
                top: 110,
                width: 112,
                height: 16,
                text: "購入",
            });
            dispatcher.add(btnDoBuy);

            btnDoBuy.click = (x: number, y: number) => {
                if (selectedItem !== -1) {
                    const itemData = Data.Item.get(Data.SaveData.shopStockList[selectedItem].id);
                    if ((hoverSlider.value > 0) &&
                        (Data.SaveData.shopStockList[selectedItem].count >= hoverSlider.value) &&
                        (itemData.price * hoverSlider.value <= Data.SaveData.money)) {
                        Data.SaveData.money -= itemData.price * hoverSlider.value;
                        if (itemData.stackable) {
                            const index = Data.SaveData.itemBox.findIndex(x => x.id == itemData.id);
                            if (index === -1) {
                                Data.SaveData.itemBox.push({
                                    id: Data.SaveData.shopStockList[selectedItem].id,
                                    condition: Data.SaveData.shopStockList[selectedItem].condition,
                                    count: hoverSlider.value
                                });
                            } else {
                                Data.SaveData.itemBox[index].count += hoverSlider.value;
                            }
                            Data.SaveData.shopStockList[selectedItem].count -= hoverSlider.value;
                        } else {
                            for (let i = 0; i < hoverSlider.value; i++) {
                                Data.SaveData.itemBox.push({
                                    id: Data.SaveData.shopStockList[selectedItem].id,
                                    condition: Data.SaveData.shopStockList[selectedItem].condition,
                                    count: 1
                                });
                            }
                            Data.SaveData.shopStockList[selectedItem].count -= hoverSlider.value;
                        }
                        if (Data.SaveData.shopStockList[selectedItem].count <= 0) {
                            Data.SaveData.shopStockList.splice(selectedItem, 1);
                        }
                        selectedItem = -1;
                        hoverSlider.value = 0;
                        Data.SaveData.save();
                        Game.getSound().reqPlayChannel("meka_ge_reji_op01");
                    }
                }
                //Game.getSound().reqPlayChannel("cursor");
            };

            const btnItemData = new Game.GUI.Button({
                left: 131,
                top: 142,
                width: 112,
                height: 60,
                text: () => {
                    if (selectedItem == -1) {
                        return "";
                    }
                    const itemData = Data.Item.get(Data.SaveData.shopStockList[selectedItem].id);
                    switch (itemData.kind) {
                        case Data.Item.Kind.Wepon:
                            return `種別：武器\nATK:${itemData.atk} | DEF:${itemData.def}`;
                        case Data.Item.Kind.Armor1:
                            return `種別：防具・上半身\nATK:${itemData.atk} | DEF:${itemData.def}`;
                        case Data.Item.Kind.Armor2:
                            return `種別：防具・下半身\nATK:${itemData.atk} | DEF:${itemData.def}`;
                        case Data.Item.Kind.Accessory:
                            return `種別：アクセサリ\nATK:${itemData.atk} | DEF:${itemData.def}`;
                        case Data.Item.Kind.Tool:
                            return `種別：道具`;
                        case Data.Item.Kind.Treasure:
                            return `種別：その他`;
                        default:
                            return "";
                    }
                },
            });
            dispatcher.add(btnItemData);

            const btnDescription = new Game.GUI.Button({
                left: 131,
                top: 212,
                width: 112,
                height: 36,
                text: () => {
                    if (selectedItem == -1) {
                        return "";
                    }
                    const itemData = Data.Item.get(Data.SaveData.shopStockList[selectedItem].id);
                    return itemData.description;
                },
            });
            dispatcher.add(btnDescription);

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


            hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = captionBuyCount.visible =
                btnDoBuy.visible =
                btnItemData.visible = btnDescription.visible = false;


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
                    0,
                    127,
                    141,
                    113,
                    83,
                    127,
                    141
                );
                dispatcher.draw();
            }

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
                hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = captionBuyCount.visible =
                    btnDoBuy.visible = btnItemData.visible = btnDescription.visible = (selectedItem != -1);
                btnDoBuy.enable = ((selectedItem != -1) &&
                    (hoverSlider.value > 0) &&
                    (Data.SaveData.shopStockList[selectedItem].count >= hoverSlider.value) &&
                    (Data.Item.get(Data.SaveData.shopStockList[selectedItem].id).price * hoverSlider.value <=
                        Data.SaveData.money));

                if (exitScene) {
                    Game.getSceneManager().pop();
                }
            };


        }
    }

}
