/// <reference path="../../lib/game/eventdispatcher.ts" />
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

        let selectedItem = -1;
        const listBox = new Game.GUI.ListBox({
            left: 8,
            top: 46,
            width: 112+1,
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
                Game.getScreen().fillText(itemData.price + "G", left + 112-3, top + 3);
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
                    return `数量：${('  ' + hoverSlider.value).substr(-2)} / 在庫：${('  ' + Data.SaveData.shopStockList[selectedItem].count).substr(-2)}\n価格：${('  ' + (Data.Item.get(Data.SaveData.shopStockList[selectedItem].id).price * hoverSlider.value)).substr(-8) + "G"}`;
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
                if ((hoverSlider.value > 0) && (Data.SaveData.shopStockList[selectedItem].count >= hoverSlider.value) && (itemData.price * hoverSlider.value <= Data.SaveData.money)) {
                    Data.SaveData.money -= itemData.price * hoverSlider.value;
                    if (itemData.stackable) {
                        const index = Data.SaveData.itemBox.findIndex(x => x.id == itemData.id);
                        if (index === -1) {
                            Data.SaveData.itemBox.push({ id: Data.SaveData.shopStockList[selectedItem].id, condition: Data.SaveData.shopStockList[selectedItem].condition, count: hoverSlider.value });
                        } else {
                            Data.SaveData.itemBox[index].count += hoverSlider.value;
                        }
                        Data.SaveData.shopStockList[selectedItem].count -= hoverSlider.value;
                    } else {
                        for (let i = 0; i < hoverSlider.value; i++) {
                            Data.SaveData.itemBox.push({ id: Data.SaveData.shopStockList[selectedItem].id, condition: Data.SaveData.shopStockList[selectedItem].condition, count: 1 });
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


        hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = captionBuyCount.visible = btnDoBuy.visible = btnItemData.visible = btnDescription.visible = false;


        this.draw = () => {
            Game.getScreen().drawImage(
                Game.getScreen().texture("shop/bg"),
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight,
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight
            );
            Game.getScreen().drawImage(
                Game.getScreen().texture("shop/J11"),
                0, 0, 127, 141,
                113, 83, 127, 141
            );
            dispatcher.draw();
        }

        yield () => {
            if (Game.getInput().isDown()) {
                dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isMove()) {
                dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isUp()) {
                dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
            }
            hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = captionBuyCount.visible = btnDoBuy.visible = btnItemData.visible = btnDescription.visible = (selectedItem != -1);
            btnDoBuy.enable = ((selectedItem != -1) && (hoverSlider.value > 0) && (Data.SaveData.shopStockList[selectedItem].count >= hoverSlider.value) && (Data.Item.get(Data.SaveData.shopStockList[selectedItem].id).price * hoverSlider.value <= Data.SaveData.money));

            if (exitScene) {
                this.next();
            }
        };


        Game.getSceneManager().pop();
    }

    function* shopSellItem() {
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
                Game.getScreen().fillText(itemData.price + "G", left + 112-3, top + 3);
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
                    return `数量：${('  ' + hoverSlider.value).substr(-2)} / 所有：${('  ' + Data.SaveData.itemBox[selectedItem].count).substr(-2)}\n価格：${('  ' + (Data.Item.get(Data.SaveData.itemBox[selectedItem].id).price * hoverSlider.value)).substr(-8) + "G"}`;
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
                    const shopStockIndex = Data.SaveData.shopStockList.findIndex(x => x.id == Data.SaveData.itemBox[selectedItem].id);
                    if (shopStockIndex == -1) {
                        let newstock: Data.Item.ItemBoxEntry = Object.assign({}, Data.SaveData.itemBox[selectedItem]);
                        newstock.condition = "";
                        newstock.count = hoverSlider.value;
                        for (let i = 0; i < Data.SaveData.shopStockList.length; i++){
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


        hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = captionSellCount.visible = btnDoSell.visible = btnItemData.visible = btnDescription.visible = false;


        this.draw = () => {
            Game.getScreen().drawImage(
                Game.getScreen().texture("shop/bg"),
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight,
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight
            );
            Game.getScreen().drawImage(
                Game.getScreen().texture("shop/J11"),
                0, 0, 127, 141,
                113, 83, 127, 141
            );
            dispatcher.draw();
        }

        yield () => {
            if (Game.getInput().isDown()) {
                dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isMove()) {
                dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isUp()) {
                dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
            }
            captionSellCount.visible = btnDoSell.visible = btnItemData.visible = btnDescription.visible = (selectedItem != -1); 
            hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = (selectedItem != -1) && (Data.Item.get(Data.SaveData.itemBox[selectedItem].id).stackable);
            if ((selectedItem != -1) && (!Data.Item.get(Data.SaveData.itemBox[selectedItem].id).stackable)) {
                hoverSlider.value = 1;
            }
            btnDoSell.enable = ((selectedItem != -1) && (hoverSlider.value > 0) && (Data.SaveData.itemBox[selectedItem].count >= hoverSlider.value));

            if (exitScene) {
                this.next();
            }
        };


        Game.getSceneManager().pop();
        return;
    }

    function* talkScene() {
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
        yield waitTimeout({
            timeout: 3000,
            init: () => { fade.startFadeIn(); },
            end: () => {
                this.next();
            },
        });
        yield waitTimeout({
            timeout: 500,
            update: (e) => { fade.update(e); },
            end: () => {
                fade.stop();
                this.next();
            },
        });
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
        for (const text of texts) {
            yield waitClick({
                start: (e) => { caption.text = text; },
                end: () => { this.next(); },
            });
        }
        yield waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeOut(); },
            update: (e) => { fade.update(e); },
            end: () => {
                this.next();
            },
        });
        yield waitTimeout({
            timeout: 1000,
            end: () => {
                this.next();
            },
        });

        Data.SaveData.itemBox.push({ id: 304, condition: "", count: 1 });
        Data.SaveData.money = 0;
        Data.SaveData.save();
        Game.getSceneManager().pop();
            Game.getSound().reqPlayChannel("classroom", true);
        return;
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
            Game.getSceneManager().push(shopSellItem);
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
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight,
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight
            );
            Game.getScreen().drawImage(
                Game.getScreen().texture("shop/J11"),
                127*((momyu >= 5000) ? 1 : 0), 0, 127, 141,
                113, 83, 127, 141
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

        yield () => {
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
        if (momyu > 0) {
            Game.getSound().reqPlayChannel("meka_ge_reji_op01");
            Data.SaveData.money -= momyu;
            if (Data.SaveData.money <= -50000) {
                this.draw = () => {
                    Game.getScreen().drawImage(
                        Game.getScreen().texture("shop/bg"),
                        0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight,
                        0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight
                    );
                    Game.getScreen().drawImage(
                        Game.getScreen().texture("shop/J11"),
                        0, 141, 127, 141,
                        113, 83, 127, 141
                    );
                    dispatcher.draw();
                    fade.draw();
                };
            }
            yield waitTimeout({
                timeout: 1000,
                end: () => {
                    this.next();
                },
            });
        }

        if (Data.SaveData.money <= -50000) {
            Game.getSound().reqStopChannel("classroom");
            Game.getSound().reqPlayChannel("sen_ge_gusya01");
                
            let rad = 0;
            this.draw = () => {
                Game.getScreen().translate(0, Math.sin(rad)*Math.cos(rad/4)*100)
                Game.getScreen().drawImage(
                    Game.getScreen().texture("shop/bg"),
                    0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight,
                    0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight
                );
                Game.getScreen().drawImage(
                    Game.getScreen().texture("shop/J11"),
                    0, 141, 127, 141,
                    113, 83, 127, 141
                );
                dispatcher.draw();
                fade.draw();
            };
            yield waitTimeout({
                timeout: 1000,
                init: () => { fade.startFadeOut(); },
                update: (e) => { 
                    rad = e * Math.PI / 25;
                    fade.update(e); 
                },
                end: () => {
                    fade.stop();
                    this.next();
                },
            });

            Game.getSceneManager().pop();
            Game.getSceneManager().push(talkScene);
        } else {
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
}