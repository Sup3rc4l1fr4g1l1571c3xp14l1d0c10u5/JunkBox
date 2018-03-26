/// <reference path="../../lib/game/eventdispatcher.ts" />
namespace Scene {
    export class ShopSellItem implements Game.Scene.Scene {
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
                getItemCount: () => Data.SaveData.itemBox.length,
                drawItem: (left: number, top: number, width: number, height: number, index: number) => {
                    const itemData = Data.Item.get(Data.SaveData.itemBox[index].id);
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
                maxValue: 100,
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
            const captionSellCount = new Game.GUI.Button({
                left: 131,
                top: 64,
                width: 112,
                height: 24,
                text: () => {
                    if (selectedItem == -1) {
                        return '';
                    } else {
                        return `数量：${('  ' + hoverSlider.value).substr(-2)} / 所有：${
                            ('  ' + Data.SaveData.itemBox[selectedItem].count).substr(-2)}\n価格：${('  ' +
                                (Data.Item.get(Data.SaveData.itemBox[selectedItem].id).price * hoverSlider.value))
                            .substr(-8) +
                            "G"}`;
                    }
                },
            });
            dispatcher.add(captionSellCount);

            const btnDoSell = new Game.GUI.Button({
                left: 131,
                top: 110,
                width: 112,
                height: 16,
                text: "売却",
            });
            dispatcher.add(btnDoSell);

            btnDoSell.click = (x: number, y: number) => {
                if (selectedItem != -1) {
                    const itemData = Data.Item.get(Data.SaveData.itemBox[selectedItem].id);
                    if ((hoverSlider.value > 0) && (Data.SaveData.itemBox[selectedItem].count >= hoverSlider.value)) {
                        Data.SaveData.money += itemData.price * hoverSlider.value;
                        const shopStockIndex =
                            Data.SaveData.shopStockList.findIndex(x => x.id == Data.SaveData.itemBox[selectedItem].id);
                        if (shopStockIndex == -1) {
                            let newstock: Data.Item.ItemBoxEntry =
                                Object.assign({}, Data.SaveData.itemBox[selectedItem]);
                            newstock.condition = "";
                            newstock.count = hoverSlider.value;
                            for (let i = 0; i < Data.SaveData.shopStockList.length; i++) {
                                if (Data.SaveData.shopStockList[i].id > newstock.id) {
                                    Data.SaveData.shopStockList.splice(i, 0, newstock);
                                    newstock = null;
                                    break;
                                }
                            }
                            if (newstock != null) {
                                Data.SaveData.shopStockList.push(newstock);
                            }

                        } else {
                            Data.SaveData.shopStockList[shopStockIndex].count += hoverSlider.value;
                        }
                        if (itemData.stackable && Data.SaveData.itemBox[selectedItem].count > hoverSlider.value) {
                            Data.SaveData.itemBox[selectedItem].count -= hoverSlider.value;
                        } else {
                            Data.SaveData.itemBox.splice(selectedItem, 1);
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
                    const itemData = Data.Item.get(Data.SaveData.itemBox[selectedItem].id);
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
                    const itemData = Data.Item.get(Data.SaveData.itemBox[selectedItem].id);
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


            hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = captionSellCount.visible =
                btnDoSell.visible = btnItemData.visible = btnDescription.visible = false;


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
                captionSellCount.visible = btnDoSell.visible = btnItemData.visible = btnDescription.visible =
                    (selectedItem != -1);
                hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = (selectedItem != -1) &&
                    (Data.Item.get(Data.SaveData.itemBox[selectedItem].id).stackable);
                if ((selectedItem != -1) && (!Data.Item.get(Data.SaveData.itemBox[selectedItem].id).stackable)) {
                    hoverSlider.value = 1;
                }
                btnDoSell.enable = ((selectedItem != -1) &&
                    (hoverSlider.value > 0) &&
                    (Data.SaveData.itemBox[selectedItem].count >= hoverSlider.value));

                if (exitScene) {
                    Game.getSceneManager().pop();
                }
            };
        }
    }

}
