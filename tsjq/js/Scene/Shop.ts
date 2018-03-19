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

        const itemlist: GameData.ItemData[] = [
            { name: "竹刀", price: 300, kind: GameData.ItemKind.Wepon, description: "471.", atk: 3, def: 0, condition: "", stackable: false },
            { name: "鉄パイプ", price: 500, kind: GameData.ItemKind.Wepon, description: "819.", atk: 5, def: 0, condition: "", stackable: false },
            { name: "バット", price: 700, kind: GameData.ItemKind.Wepon, description: "89.", atk: 7, def: 0, condition: "", stackable: false },
            { name: "水着", price: 200, kind: GameData.ItemKind.Armor1, description: "3.", atk: 0, def: 1, condition: "", stackable: false },
            { name: "制服", price: 400, kind: GameData.ItemKind.Armor1, description: "1.", atk: 0, def: 2, condition: "", stackable: false },
            { name: "体操着", price: 600, kind: GameData.ItemKind.Armor1, description: "2.", atk: 0, def: 3, condition: "", stackable: false },
            { name: "スカート", price: 200, kind: GameData.ItemKind.Armor2, description: "3.", atk: 0, def: 1, condition: "", stackable: false },
            { name: "ブルマ", price: 400, kind: GameData.ItemKind.Armor2, description: "1.", atk: 0, def: 2, condition: "", stackable: false },
            { name: "ズボン", price: 600, kind: GameData.ItemKind.Armor2, description: "2.", atk: 0, def: 3, condition: "", stackable: false },
            { name: "ヘアバンド", price: 2000, kind: GameData.ItemKind.Accessory, description: "2.", atk: 0, def: 1, condition: "", stackable: false },
            { name: "メガネ", price: 2000, kind: GameData.ItemKind.Accessory, description: "2.", atk: 0, def: 1, condition: "", stackable: false },
            { name: "靴下", price: 2000, kind: GameData.ItemKind.Accessory, description: "2.", atk: 0, def: 1, condition: "", stackable: false },
            { name: "イモメロン", price: 100, kind: GameData.ItemKind.Tool, description: "food.", effect: (data) => { }, stackable: true},
            { name: "プリングルス", price: 890, kind: GameData.ItemKind.Tool, description: "140546.", effect: (data) => { }, stackable: true },
            { name: "バンテリン", price: 931, kind: GameData.ItemKind.Tool, description: "931.", effect: (data) => { }, stackable: true },
            { name: "サラダチキン", price: 1000, kind: GameData.ItemKind.Tool, description: "dmkt.", effect: (data) => { }, stackable: true },
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
                Game.getScreen().fillText(itemlist[index].name, left + 3, top + 3);
                Game.getScreen().textAlign = "right";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(itemlist[index].price + "G", left + 112, top + 3);
            }
        });
        dispatcher.add(listBox);

        listBox.click = (x: number, y: number) => {
            selectedItem = listBox.getItemIndexByPosition(x, y);
            Game.getSound().reqPlayChannel("cursor");
        };

        const captionMonay = new Game.GUI.Button({
            left: 135,
            top: 46,
            width: 108,
            height: 16,
            text: () => `所持金：${('            ' + GameData.Money + ' G').substr(-13)}`,
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
            text: () => `購入数：${('  ' + hoverSlider.value).substr(-2) + "/" + ('  ' + (selectedItem == -1 ? 0 : itemlist[selectedItem].price * hoverSlider.value)).substr(-8)+"G"}`,
        });
        dispatcher.add(captionBuyCount);

        const btnDoBuy = new Game.GUI.Button({
            left: 135,
            top: 110,
            width: 112,
            height: 16,
            text: "購入",
        });
        dispatcher.add(btnDoBuy);

        btnDoBuy.click = (x: number, y: number) => {
            if ((selectedItem != -1) && (hoverSlider.value > 0) && (itemlist[selectedItem].price * hoverSlider.value <= GameData.Money)) {
                GameData.Money -= itemlist[selectedItem].price * hoverSlider.value;
                if (itemlist[selectedItem].stackable) {
                    var index = GameData.ItemBox.findIndex(x => x.item.name == itemlist[selectedItem].name);
                    if (index == -1) {
                        GameData.ItemBox.push({ item: itemlist[selectedItem], count: hoverSlider.value });
                    } else {
                        GameData.ItemBox[index].count += hoverSlider.value;
                    }
                } else {
                    for (let i = 0; i < hoverSlider.value; i++) {
                        GameData.ItemBox.push({ item: itemlist[selectedItem], count: 1 });
                    }
                }
                selectedItem = -1;
                hoverSlider.value = 0;
            }
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
            Game.getSound().reqPlayChannel("cursor");
        };

 
        hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = captionBuyCount.visible = btnDoBuy.visible = false;


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
            hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = captionBuyCount.visible = btnDoBuy.visible = (selectedItem != -1);
            btnDoBuy.enable = ((selectedItem != -1) && (hoverSlider.value > 0) && (itemlist[selectedItem].price * hoverSlider.value <= GameData.Money));

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
            width: 112,
            height: 10 * 16,
            lineHeight: 16,
            getItemCount: () => GameData.ItemBox.length,
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
                Game.getScreen().fillText(GameData.ItemBox[index].item.name, left + 3, top + 3);
                Game.getScreen().textAlign = "right";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(GameData.ItemBox[index].item.price + "G", left + 112, top + 3);
            }
        });
        dispatcher.add(listBox);

        listBox.click = (x: number, y: number) => {
            selectedItem = listBox.getItemIndexByPosition(x, y);
            Game.getSound().reqPlayChannel("cursor");
        };

        const captionMonay = new Game.GUI.Button({
            left: 135,
            top: 46,
            width: 108,
            height: 16,
            text: () => `所持金：${('            ' + GameData.Money + ' G').substr(-13)}`,
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
        const captionSellCount = new Game.GUI.Button({
            left: 135,
            top: 64,
            width: 108,
            height: 16,
            text: () => `売却数：${('  ' + hoverSlider.value).substr(-2) + "/" + ('  ' + (selectedItem == -1 ? 0 : GameData.ItemBox[selectedItem].item.price * hoverSlider.value)).substr(-8) + "G"}`,
        });
        dispatcher.add(captionSellCount);

        const btnDoSell = new Game.GUI.Button({
            left: 135,
            top: 110,
            width: 112,
            height: 16,
            text: "売却",
        });
        dispatcher.add(btnDoSell);

        btnDoSell.click = (x: number, y: number) => {
            if ((selectedItem != -1) && (hoverSlider.value > 0) && (GameData.ItemBox[selectedItem].item.price * hoverSlider.value <= GameData.Money)) {
                GameData.Money += GameData.ItemBox[selectedItem].item.price * hoverSlider.value;
                if (GameData.ItemBox[selectedItem].item.stackable && GameData.ItemBox[selectedItem].count > hoverSlider.value) {
                    GameData.ItemBox[selectedItem].count -= hoverSlider.value;
                } else {
                    GameData.ItemBox.splice(selectedItem, 1);
                }
                selectedItem = -1;
                hoverSlider.value = 0;
            }
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
            Game.getSound().reqPlayChannel("cursor");
        };


        hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = captionSellCount.visible = btnDoSell.visible = false;


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
            captionSellCount.visible = btnDoSell.visible = (selectedItem != -1); 
            hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = (selectedItem != -1) && (GameData.ItemBox[selectedItem].item.stackable);
            if ((selectedItem != -1) && (!GameData.ItemBox[selectedItem].item.stackable)) {
                hoverSlider.value = 1;
            }
            btnDoSell.enable = ((selectedItem != -1) && (hoverSlider.value > 0) && (GameData.ItemBox[selectedItem].count >= hoverSlider.value));

            if (exitScene) {
                this.next();
            }
        };


        Game.getSceneManager().pop();
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

        GameData.Money = 0; 
        Game.getSceneManager().pop();
            Game.getSound().reqPlayChannel("classroom", true);

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
            left: 135,
            top: 46,
            width: 108,
            height: 16,
            text: () =>  `所持金：${('            ' + GameData.Money + ' G').substr(-13)}`,
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
            GameData.Money -= momyu;
            yield waitTimeout({
                timeout: 1000,
                end: () => {
                    this.next();
                },
            });
        }

        if (GameData.Money <= -50000) {
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