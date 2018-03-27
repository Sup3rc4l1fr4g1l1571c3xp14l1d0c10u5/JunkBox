namespace Scene.Dungeon {
    export class ItemBoxSelectItem implements Game.Scene.Scene{
        draw() {}

        update() {}

        constructor(opt: {
            selectedItem: number;
            player: Unit.Party;
            floor: number;
            upperdraw: () => void;
        }) {
            const dispatcher = new Game.GUI.UIDispatcher();
            const caption = new Game.GUI.TextBox({
                left: 1,
                top: 1,
                width: 250,
                height: 14,
                text: "���",
                edgeColor: `rgb(12,34,98)`,
                color: `rgb(24,133,196)`,
                font: "10px 'PixelMplus10-Regular'",
                fontColor: `rgb(255,255,255)`,
                textAlign: "left",
                textBaseline: "top",
            });
            dispatcher.add(caption);

            opt.selectedItem = -1;
            const listBox = new Game.GUI.ListBox({
                left: 8,
                top: 46 - 28,
                width: 112 + 1,
                height: 11 * 16,
                lineHeight: 16,
                getItemCount: () => Data.SaveData.itemBox.length,
                drawItem: (left: number, top: number, width: number, height: number, index: number) => {
                    const itemData = Data.Item.get(Data.SaveData.itemBox[index].id);
                    if (opt.selectedItem == index) {
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
                opt.selectedItem = listBox.getItemIndexByPosition(x, y);
                if (opt.selectedItem != -1) {
                    Game.getSound().reqPlayChannel("cursor");
                }
            };

            const captionMonay = new Game.GUI.Button({
                left: 131,
                top: 46 - 28,
                width: 112,
                height: 16,
                text: () => `�������F${('            ' + Data.SaveData.money + ' G').substr(-13)}`,
            });
            dispatcher.add(captionMonay);

            const btnDoUse = new Game.GUI.Button({
                left: 131,
                top: 110,
                width: 112,
                height: 16,
                text: "�g�p",
            });
            dispatcher.add(btnDoUse);

            btnDoUse.click = (x: number, y: number) => {
                if (opt.selectedItem !== -1) {
                    const itemData = Data.Item.get(Data.SaveData.itemBox[opt.selectedItem].id);
                    if (itemData.useToPlayer != null) {
                        // �v���C���[�I����ʂɂ��̃A�C�e����n���Ĉꎞ�J��
                        Game.getSceneManager().push(new ItemBoxSelectPlayer(opt));
                    } else if (itemData.useToParty != null) {
                        // �p�[�e�B�S�̂ɓK�p����
                        const ret = itemData.useToParty(opt.player);
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

            const captionItemCount = new Game.GUI.Button({
                left: 131,
                top: 64,
                width: 112,
                height: 14,
                text: () => {
                    if (opt.selectedItem == -1) {
                        return '';
                    } else {
                        return `���L�F${('  ' + Data.SaveData.itemBox[opt.selectedItem].count).substr(-2)}��`;
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
                            return `��ʁF����\nATK:${itemData.atk} | DEF:${itemData.def}`;
                        case Data.Item.Kind.Armor1:
                            return `��ʁF�h��E�㔼�g\nATK:${itemData.atk} | DEF:${itemData.def}`;
                        case Data.Item.Kind.Armor2:
                            return `��ʁF�h��E�����g\nATK:${itemData.atk} | DEF:${itemData.def}`;
                        case Data.Item.Kind.Accessory:
                            return `��ʁF�A�N�Z�T��\nATK:${itemData.atk} | DEF:${itemData.def}`;
                        case Data.Item.Kind.Tool:
                            return `��ʁF����`;
                        case Data.Item.Kind.Treasure:
                            return `��ʁF���̑�`;
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

            const btnExit = new Game.GUI.Button({
                left: 8,
                top: 16 * 11 + 46,
                width: 112,
                height: 16,
                text: "�߂�",
            });
            dispatcher.add(btnExit);

            let exitScene = false;
            btnExit.click = (x: number, y: number) => {
                exitScene = true;
                Game.getSound().reqPlayChannel("cursor");
            };


            btnDoUse.visible = btnItemData.visible = btnDescription.visible = captionItemCount.visible = false;


            this.draw = () => {
                opt.upperdraw();
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
                btnItemData.visible = btnDescription.visible = captionItemCount.visible = (opt.selectedItem != -1);
                btnDoUse.visible = (opt.selectedItem != -1) &&
                (Data.Item.get(Data.SaveData.itemBox[opt.selectedItem].id).useToPlayer != null ||
                    Data.Item.get(Data.SaveData.itemBox[opt.selectedItem].id).useToParty != null);
                if (exitScene) {
                    Game.getSceneManager().pop(this);
                }
            };

        }
    }
}