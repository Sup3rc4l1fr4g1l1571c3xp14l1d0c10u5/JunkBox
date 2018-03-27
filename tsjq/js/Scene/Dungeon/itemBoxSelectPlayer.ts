namespace Scene.Dungeon {
    export class ItemBoxSelectPlayer implements Game.Scene.Scene {
        draw() {}
        update() {}
        
        constructor(opt: {
            selectedItem: number;
            player: Unit.Party;
            floor: number;
            upperdraw: () => void;
        }) {
            let anim = 0;
            const dispatcher = new Game.GUI.UIDispatcher();
            const caption = new Game.GUI.TextBox({
                left: 1,
                top: 1,
                width: 250,
                height: 14,
                text: "道具箱：対象を選んでください。",
                edgeColor: `rgb(12,34,98)`,
                color: `rgb(24,133,196)`,
                font: "10px 'PixelMplus10-Regular'",
                fontColor: `rgb(255,255,255)`,
                textAlign: "left",
                textBaseline: "top",
            });
            dispatcher.add(caption);


            let exitScene = false;
            const team: StatusSprite[] = [
                new StatusSprite(opt.player.getForward()), new StatusSprite(opt.player.getBackward())
            ];
            const forwardBtn = new Game.GUI.ImageButton({
                left: 8,
                top: 46,
                width: 112,
                height: 48,
                texture: null,
                texLeft: 0,
                texTop: 0,
                texWidth: 0,
                texHeight: 0
            });
            forwardBtn.draw = () => {
                Game.getScreen().fillStyle = `rgb(24,133,196)`;
                Game.getScreen().fillRect(forwardBtn.left, forwardBtn.top, forwardBtn.width, 13);
                Game.getScreen().strokeStyle = `rgb(12,34,98)`;
                Game.getScreen().lineWidth = 1;
                Game.getScreen().strokeRect(forwardBtn.left, forwardBtn.top, forwardBtn.width, 13);
                Game.getScreen().font = "10px 'PixelMplus10-Regular'";
                Game.getScreen().fillStyle = `rgb(255,255,255)`;
                Game.getScreen().textAlign = "left";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText("前衛", forwardBtn.left + 2, forwardBtn.top + 2);

                drawStatusSprite(team[0],
                    DrawMode.Normal,
                    forwardBtn.left,
                    forwardBtn.top + 12,
                    forwardBtn.width,
                    48,
                    anim);
            };
            forwardBtn.click = (x: number, y: number) => {
                if (opt.selectedItem != -1) {
                    const itemId = Data.SaveData.itemBox[opt.selectedItem].id;
                    const itemData = Data.Item.get(itemId);
                    if (itemData != null && itemData.useToPlayer != null) {
                        const ret = itemData.useToPlayer(team[0].data);
                        if (ret == true) {
                            if (Data.SaveData.itemBox[opt.selectedItem].count > 0) {
                                Data.SaveData.itemBox[opt.selectedItem].count -= 1;
                            } else {
                                Data.SaveData.itemBox[opt.selectedItem].count = 0;
                            }
                            if (Data.SaveData.itemBox[opt.selectedItem].count == 0) {
                                exitScene = true;
                                Data.SaveData.itemBox.splice(opt.selectedItem, 1);
                                opt.selectedItem = -1;
                            }
                        }
                    }
                }
                Game.getSound().reqPlayChannel("cursor");
            };
            dispatcher.add(forwardBtn);

            const backwordBtn = new Game.GUI.ImageButton({
                left: 8,
                top: 46 + 70,
                width: 112,
                height: 60,
                texture: null,
                texLeft: 0,
                texTop: 0,
                texWidth: 0,
                texHeight: 0
            });
            backwordBtn.draw = () => {
                Game.getScreen().fillStyle = `rgb(24,133,196)`;
                Game.getScreen().fillRect(backwordBtn.left, backwordBtn.top, backwordBtn.width, 13);
                Game.getScreen().strokeStyle = `rgb(12,34,98)`;
                Game.getScreen().lineWidth = 1;
                Game.getScreen().strokeRect(backwordBtn.left, backwordBtn.top, backwordBtn.width, 13);
                Game.getScreen().font = "10px 'PixelMplus10-Regular'";
                Game.getScreen().fillStyle = `rgb(255,255,255)`;
                Game.getScreen().textAlign = "left";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText("後衛", backwordBtn.left + 2, backwordBtn.top + 2);

                drawStatusSprite(team[1],
                    DrawMode.Normal,
                    backwordBtn.left,
                    backwordBtn.top + 12,
                    backwordBtn.width,
                    48,
                    anim);
            };
            backwordBtn.click = (x: number, y: number) => {
                if (opt.selectedItem != -1) {
                    const itemId = Data.SaveData.itemBox[opt.selectedItem].id;
                    const itemData = Data.Item.get(itemId);
                    if (itemData != null && itemData.useToPlayer != null) {
                        const ret = itemData.useToPlayer(team[1].data);
                        if (ret == true) {
                            if (Data.SaveData.itemBox[opt.selectedItem].count > 0) {
                                Data.SaveData.itemBox[opt.selectedItem].count -= 1;
                            } else {
                                Data.SaveData.itemBox[opt.selectedItem].count = 0;
                            }
                            if (Data.SaveData.itemBox[opt.selectedItem].count == 0) {
                                exitScene = true;
                                Data.SaveData.itemBox.splice(opt.selectedItem, 1);
                                opt.selectedItem = -1;
                            }
                        }
                    }
                }
                Game.getSound().reqPlayChannel("cursor");
            };
            dispatcher.add(backwordBtn);

            const captionMonay = new Game.GUI.Button({
                left: 131,
                top: 46 - 28,
                width: 112,
                height: 16,
                text: () => `所持金：${('            ' + Data.SaveData.money + ' G').substr(-13)}`,
            });
            dispatcher.add(captionMonay);

            const btnExit = new Game.GUI.Button({
                left: 131,
                top: 110,
                width: 112,
                height: 16,
                text: "戻る",
            });
            dispatcher.add(btnExit);

            btnExit.click = (x: number, y: number) => {
                exitScene = true;
                Game.getSound().reqPlayChannel("cursor");
            };

            const captionItemCount = new Game.GUI.Button({
                left: 131,
                top: 64,
                width: 112,
                height: 14,
                text: () => {
                    if (opt.selectedItem == -1) {
                        return '';
                    } else {
                        return `所有：${('  ' + Data.SaveData.itemBox[opt.selectedItem].count).substr(-2)}個`;
                    }
                },
            });
            dispatcher.add(captionItemCount);

            const btnItemData = new Game.GUI.Button({
                left: 131,
                top: 142,
                width: 112,
                height: 60,
                text: () => {
                    if (opt.selectedItem == -1) {
                        return "";
                    }
                    const itemData = Data.Item.get(Data.SaveData.itemBox[opt.selectedItem].id);
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
                    if (opt.selectedItem == -1) {
                        return "";
                    }
                    const itemData = Data.Item.get(Data.SaveData.itemBox[opt.selectedItem].id);
                    return itemData.description;
                },
            });
            dispatcher.add(btnDescription);


            this.draw = () => {
                opt.upperdraw();
                dispatcher.draw();
            }

            this.update = () => {
                anim = Game.getTimer().now % 1000;
                if (Game.getInput().isDown()) {
                    dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
                }
                if (Game.getInput().isMove()) {
                    dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
                }
                if (Game.getInput().isUp()) {
                    dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
                }
                btnItemData.visible = btnDescription.visible = captionItemCount.visible = (opt.selectedItem != -1);
                if (exitScene) {
                    Game.getSceneManager().pop();
                }
            };


        }
    }
}