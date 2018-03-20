/// <reference path="../lib/game/eventdispatcher.ts" />
namespace Scene {

    function* shopBuyItem(saveData: Data.SaveData.SaveData) {
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
            width: 112,
            height: 10 * 16,
            lineHeight: 16,
            getItemCount: () => saveData.shopStockList.length,
            drawItem: (left: number, top: number, width: number, height: number, index: number) => {
                const itemData = Data.Item.findItemDataById(saveData.shopStockList[index].id);
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
            text: () => `所持金：${('            ' + saveData.Money + ' G').substr(-13)}`,
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
                if (selectedItem == -1) {
                    return '';
                } else {
                    return `数量：${('  ' + hoverSlider.value).substr(-2)} / 在庫：${('  ' + saveData.shopStockList[selectedItem].count).substr(-2)}\n価格：${('  ' + (Data.Item.findItemDataById(saveData.shopStockList[selectedItem].id).price * hoverSlider.value)).substr(-8) + "G"}`;
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
            if (selectedItem != -1) {
                const itemData = Data.Item.findItemDataById(saveData.shopStockList[selectedItem].id);
                if ((hoverSlider.value > 0) && (saveData.shopStockList[selectedItem].count >= hoverSlider.value) && (itemData.price * hoverSlider.value <= saveData.Money)) {
                    saveData.Money -= itemData.price * hoverSlider.value;
                    if (itemData.stackable) {
                        var index = saveData.ItemBox.findIndex(x => x.id == itemData.id);
                        if (index == -1) {
                            saveData.ItemBox.push({ id: saveData.shopStockList[selectedItem].id, condition: saveData.shopStockList[selectedItem].condition, count: hoverSlider.value });
                        } else {
                            saveData.ItemBox[index].count += hoverSlider.value;
                        }
                        saveData.shopStockList[selectedItem].count -= hoverSlider.value;
                    } else {
                        for (let i = 0; i < hoverSlider.value; i++) {
                            saveData.ItemBox.push({ id: saveData.shopStockList[selectedItem].id, condition: saveData.shopStockList[selectedItem].condition, count: 1 });
                        }
                        saveData.shopStockList[selectedItem].count -= hoverSlider.value;
                    }
                    if (saveData.shopStockList[selectedItem].count <= 0) {
                        saveData.shopStockList.splice(selectedItem, 1);
                    }
                    selectedItem = -1;
                    hoverSlider.value = 0;
                    saveData.saveGameData();
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
                const itemData = Data.Item.findItemDataById(saveData.shopStockList[selectedItem].id);
                switch (itemData.kind) {
                    case Data.Item.ItemKind.Wepon:
                        return `種別：武器\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Armor1:
                        return `種別：防具・上半身\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Armor2:
                        return `種別：防具・下半身\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Accessory:
                        return `種別：アクセサリ\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Tool:
                        return `種別：道具`;
                    case Data.Item.ItemKind.Treasure:
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
                const itemData = Data.Item.findItemDataById(saveData.shopStockList[selectedItem].id);
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
            hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = captionBuyCount.visible = btnDoBuy.visible = btnItemData.visible = btnDescription.visible = (selectedItem != -1);
            btnDoBuy.enable = ((selectedItem != -1) && (hoverSlider.value > 0) && (saveData.shopStockList[selectedItem].count >= hoverSlider.value) && (Data.Item.findItemDataById(saveData.shopStockList[selectedItem].id).price * hoverSlider.value <= saveData.Money));

            if (exitScene) {
                this.next();
            }
        };


        Game.getSceneManager().pop();
    }

    function* shopSellItem(saveData: Data.SaveData.SaveData) {
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
            width: 112,
            height: 10 * 16,
            lineHeight: 16,
            getItemCount: () => saveData.ItemBox.length,
            drawItem: (left: number, top: number, width: number, height: number, index: number) => {
                const itemData = Data.Item.findItemDataById(saveData.ItemBox[index].id);
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
            text: () => `所持金：${('            ' + saveData.Money + ' G').substr(-13)}`,
        });
        dispatcher.add(captionMonay);

        const hoverSlider = new Game.GUI.HorizontalSlider({
            left: 131 + 14,
            top: 80,
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
        const captionSellCount = new Game.GUI.Button({
            left: 131,
            top: 64,
            width: 112,
            height: 24,
            text: () => {
                if (selectedItem == -1) {
                    return '';
                } else {
                    return `数量：${('  ' + hoverSlider.value).substr(-2)} / 所有：${('  ' + saveData.ItemBox[selectedItem].count).substr(-2)}\n価格：${('  ' + (Data.Item.findItemDataById(saveData.ItemBox[selectedItem].id).price * hoverSlider.value)).substr(-8) + "G"}`;
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
                const itemData = Data.Item.findItemDataById(saveData.ItemBox[selectedItem].id);
                if ((hoverSlider.value > 0) && (saveData.ItemBox[selectedItem].count >= hoverSlider.value)) {
                    saveData.Money += itemData.price * hoverSlider.value;
                    const shopStockIndex = saveData.shopStockList.findIndex(x => x.id == saveData.ItemBox[selectedItem].id);
                    if (shopStockIndex == -1) {
                        let newstock: Data.Item.ItemBoxEntry = Object.assign({}, saveData.ItemBox[selectedItem]);
                        newstock.condition = "";
                        newstock.count = hoverSlider.value;
                        for (let i = 0; i < saveData.shopStockList.length; i++){
                           if (saveData.shopStockList[i].id > newstock.id) {
                               saveData.shopStockList.splice(i, 0, newstock);
                               newstock = null;
                               break;
                           }
                        }
                    if (newstock != null) {
                            saveData.shopStockList.push(newstock);
                        }

                    } else {
                        saveData.shopStockList[shopStockIndex].count += hoverSlider.value;
                    }
                    if (itemData.stackable && saveData.ItemBox[selectedItem].count > hoverSlider.value) {
                        saveData.ItemBox[selectedItem].count -= hoverSlider.value;
                    } else {
                        saveData.ItemBox.splice(selectedItem, 1);
                    }
                    selectedItem = -1;
                    hoverSlider.value = 0;
                    saveData.saveGameData();
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
                const itemData = Data.Item.findItemDataById(saveData.ItemBox[selectedItem].id);
                switch (itemData.kind) {
                    case Data.Item.ItemKind.Wepon:
                        return `種別：武器\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Armor1:
                        return `種別：防具・上半身\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Armor2:
                        return `種別：防具・下半身\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Accessory:
                        return `種別：アクセサリ\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Tool:
                        return `種別：道具`;
                    case Data.Item.ItemKind.Treasure:
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
                const itemData = Data.Item.findItemDataById(saveData.ItemBox[selectedItem].id);
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
            captionSellCount.visible = btnDoSell.visible = btnItemData.visible = btnDescription.visible = (selectedItem != -1); 
            hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = (selectedItem != -1) && (Data.Item.findItemDataById(saveData.ItemBox[selectedItem].id).stackable);
            if ((selectedItem != -1) && (!Data.Item.findItemDataById(saveData.ItemBox[selectedItem].id).stackable)) {
                hoverSlider.value = 1;
            }
            btnDoSell.enable = ((selectedItem != -1) && (hoverSlider.value > 0) && (saveData.ItemBox[selectedItem].count >= hoverSlider.value));

            if (exitScene) {
                this.next();
            }
        };


        Game.getSceneManager().pop();
    }

    function* talkScene(saveData: Data.SaveData.SaveData) {
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

        saveData.ItemBox.push({ id: 304, condition: "", count: 1 });
        saveData.Money = 0;
        saveData.saveGameData();
        Game.getSceneManager().pop();
            Game.getSound().reqPlayChannel("classroom", true);

    }

    export function* shop(saveData: Data.SaveData.SaveData) {
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
            Game.getSceneManager().push(shopBuyItem, saveData);
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
            Game.getSceneManager().push(shopSellItem, saveData);
            Game.getSound().reqPlayChannel("cursor");
        };

        const captionMonay = new Game.GUI.Button({
            left: 131,
            top: 46,
            width: 112,
            height: 16,
            text: () => `所持金：${('            ' + saveData.Money + ' G').substr(-13)}`,
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
        if (momyu > 0) {
            Game.getSound().reqPlayChannel("meka_ge_reji_op01");
            saveData.Money -= momyu;
            yield waitTimeout({
                timeout: 1000,
                end: () => {
                    this.next();
                },
            });
        }

        if (saveData.Money <= -50000) {
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
            Game.getSceneManager().push(talkScene, saveData);
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