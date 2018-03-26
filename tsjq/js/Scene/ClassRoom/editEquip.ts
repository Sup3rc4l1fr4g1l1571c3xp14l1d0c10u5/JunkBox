/// <reference path="../../SpriteAnimation.ts" />
namespace Scene.ClassRoom {
    export function* editEquip(): IterableIterator<any> {
        const dispatcher = new Game.GUI.UIDispatcher();

        const caption = new Game.GUI.TextBox({
            left: 1,
            top: 1,
            width: 250,
            height: 42,
            text: "装備変更",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.add(caption);

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
            Data.SaveData.save();
            Game.getSound().reqPlayChannel("cursor");
        };

        const charactors = Data.Charactor.keys().map(x => new StatusSprite(Data.SaveData.findCharactorById(x)));
        let team = [Data.Charactor.keys().findIndex(x => x == Data.SaveData.forwardCharactor), Data.Charactor.keys().findIndex(x => x == Data.SaveData.backwardCharactor)];
        let selectedCharactorIndex :number = -1;
        let selectedEquipPosition :number = -1;
        let anim = 0;
        const charactorListBox = new Game.GUI.ListBox({
            left: 131,
            top: 46,
            width: 112+1,
            height: 4 * 48,
            lineHeight: 48,
            getItemCount: () => charactors.length,
            drawItem: (left: number, top: number, width: number, height: number, index: number) => {
                drawStatusSprite(charactors[index], selectedCharactorIndex == index? DrawMode.Selected : DrawMode.Normal, left, top, width, height, anim);
            }
        });
        dispatcher.add(charactorListBox);
        charactorListBox.click = (x: number, y: number) => {
            const select = charactorListBox.getItemIndexByPosition(x, y);
            selectedCharactorIndex = selectedCharactorIndex == select ? -1 : select;
            Game.getSound().reqPlayChannel("cursor");
        };

        let selectedItem = -1;
        const itemLists : number[] = [];

        let updateItemList = () => {
            const newItemLists = Data.SaveData.itemBox.map((x, i) => {
                if (x == null) {
                    return -1;
                }
                const itemData = Data.Item.get(x.id);
                switch (selectedEquipPosition) {
                    case 0:
                        return (itemData.kind == Data.Item.Kind.Wepon) ? i : -1;
                    case 1:
                        return (itemData.kind == Data.Item.Kind.Armor1) ? i : -1;
                    case 2:
                        return (itemData.kind == Data.Item.Kind.Armor2) ? i : -1;
                    case 3:
                    case 4:
                        return (itemData.kind == Data.Item.Kind.Accessory) ? i : -1;
                    default:
                        return -1;
                }
            }).filter(x => x != -1);
            itemLists.length = 0;
            itemLists.push(...newItemLists);
        };

        const itemListBox = new Game.GUI.ListBox({
            left: 131,
            top: 46,
            width: 112+1,
            height: 12 * 16,
            lineHeight: 16,
            getItemCount: () => itemLists.length,
            drawItem: (left: number, top: number, width: number, height: number, index: number) => {
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
                Game.getScreen().fillText(Data.Item.get(Data.SaveData.itemBox[itemLists[index]].id).name, left + 3, top + 3);
            }
        });
        dispatcher.add(itemListBox);
        itemListBox.click = (x,y) => {
            const select = itemListBox.getItemIndexByPosition(x, y);
            Game.getSound().reqPlayChannel("cursor");
            if (select == -1 || selectedCharactorIndex == -1) {
                return;
            }
                switch (selectedEquipPosition) {
                    case 0:
                        if (charactors[selectedCharactorIndex].data.equips.wepon1 != null) {
                            const oldItem = charactors[selectedCharactorIndex].data.equips.wepon1;
                            charactors[selectedCharactorIndex].data.equips.wepon1 = Data.SaveData.itemBox[itemLists[select]];
                            Data.SaveData.itemBox[itemLists[select]] = oldItem;
                        } else {
                            charactors[selectedCharactorIndex].data.equips.wepon1 = Data.SaveData.itemBox[itemLists[select]];
                            Data.SaveData.itemBox.splice(itemLists[select],1);
                        }
                        updateItemList();
                        break;
                    case 1:
                        if (charactors[selectedCharactorIndex].data.equips.armor1 != null) {
                            const oldItem = charactors[selectedCharactorIndex].data.equips.armor1;
                            charactors[selectedCharactorIndex].data.equips.armor1 = Data.SaveData.itemBox[itemLists[select]];
                            Data.SaveData.itemBox[itemLists[select]] = oldItem;
                        } else {
                            charactors[selectedCharactorIndex].data.equips.armor1 = Data.SaveData.itemBox[itemLists[select]];
                            Data.SaveData.itemBox.splice(itemLists[select],1);
                        }
                        updateItemList();
                        break;
                    case 2:
                        if (charactors[selectedCharactorIndex].data.equips.armor2 != null) {
                            const oldItem = charactors[selectedCharactorIndex].data.equips.armor2;
                            charactors[selectedCharactorIndex].data.equips.armor2 = Data.SaveData.itemBox[itemLists[select]];
                            Data.SaveData.itemBox[itemLists[select]] = oldItem;
                        } else {
                            charactors[selectedCharactorIndex].data.equips.armor2 = Data.SaveData.itemBox[itemLists[select]];
                            Data.SaveData.itemBox.splice(itemLists[select],1);
                        }
                        updateItemList();
                        break;
                    case 3:
                        if (charactors[selectedCharactorIndex].data.equips.accessory1 != null) {
                            const oldItem = charactors[selectedCharactorIndex].data.equips.accessory1;
                            charactors[selectedCharactorIndex].data.equips.accessory1 = Data.SaveData.itemBox[itemLists[select]];
                            Data.SaveData.itemBox[itemLists[select]] = oldItem;
                        } else {
                            charactors[selectedCharactorIndex].data.equips.accessory1 = Data.SaveData.itemBox[itemLists[select]];
                            Data.SaveData.itemBox.splice(itemLists[select],1);
                        }
                        updateItemList();
                        break;
                    case 4:
                        if (charactors[selectedCharactorIndex].data.equips.accessory2 != null) {
                            const oldItem = charactors[selectedCharactorIndex].data.equips.accessory2;
                            charactors[selectedCharactorIndex].data.equips.accessory2 = Data.SaveData.itemBox[itemLists[select]];
                            Data.SaveData.itemBox[itemLists[select]] = oldItem;
                        } else {
                            charactors[selectedCharactorIndex].data.equips.accessory2 = Data.SaveData.itemBox[itemLists[select]];
                            Data.SaveData.itemBox.splice(itemLists[select],1);
                        }
                        updateItemList();
                        break;
                    default:
                        break;
                }
        };


        const statusViewBtn = new Game.GUI.ImageButton({
            left: 8,
            top: 46,
            width: 112,
            height: 48,
            texture : null,
            texLeft : 0,
            texTop:0,
            texWidth:0,
            texHeight:0
        });
        statusViewBtn.draw = () => {
            drawStatusSprite(charactors[selectedCharactorIndex], DrawMode.Normal, statusViewBtn.left, statusViewBtn.top, statusViewBtn.width, 48, anim);
        };
        statusViewBtn.click = () => {
            Game.getSound().reqPlayChannel("cursor");
            selectedEquipPosition = -1;
        };
        dispatcher.add(statusViewBtn);

        const btnWepon1 = new Game.GUI.Button({
            left: 8,
            top: 16 * 0 + 46+50,
            width: 112,
            height: 16,
            text: () => (selectedCharactorIndex == -1 || charactors[selectedCharactorIndex].data.equips.wepon1 == null) ? "(武器)" : Data.Item.get(charactors[selectedCharactorIndex].data.equips.wepon1.id).name,
        });
        dispatcher.add(btnWepon1);
        btnWepon1.click = () => {
            Game.getSound().reqPlayChannel("cursor");
            selectedEquipPosition = selectedEquipPosition == 0 ? -1 : 0;
                        updateItemList();
        }

        const btnArmor1 = new Game.GUI.Button({
            left: 8,
            top: 16 * 1 + 46+50,
            width: 112,
            height: 16,
            text: () => (selectedCharactorIndex == -1 || charactors[selectedCharactorIndex].data.equips.armor1 == null) ? "(防具・上半身)" : Data.Item.get(charactors[selectedCharactorIndex].data.equips.armor1.id).name,
        });
        dispatcher.add(btnArmor1);
        btnArmor1.click = () => {
            Game.getSound().reqPlayChannel("cursor");
            selectedEquipPosition = selectedEquipPosition == 1 ? -1 : 1;
                        updateItemList();
        }

        const btnArmor2 = new Game.GUI.Button({
            left: 8,
            top: 16 * 2 + 46+50,
            width: 112,
            height: 16,
            text: () => (selectedCharactorIndex == -1 || charactors[selectedCharactorIndex].data.equips.armor2 == null) ? "(防具・下半身)" : Data.Item.get(charactors[selectedCharactorIndex].data.equips.armor2.id).name,
        });
        dispatcher.add(btnArmor2);
        btnArmor2.click = () => {
            Game.getSound().reqPlayChannel("cursor");
            selectedEquipPosition = selectedEquipPosition == 2 ? -1 : 2;
                        updateItemList();
        }

        const btnAccessory1 = new Game.GUI.Button({
            left: 8,
            top: 16 * 3 + 46+50,
            width: 112,
            height: 16,
            text: () => (selectedCharactorIndex == -1 || charactors[selectedCharactorIndex].data.equips.accessory1 == null) ? "(アクセサリ１)" : Data.Item.get(charactors[selectedCharactorIndex].data.equips.accessory1.id).name,
        });
        dispatcher.add(btnAccessory1);
        btnAccessory1.click = () => {
            Game.getSound().reqPlayChannel("cursor");
            selectedEquipPosition = selectedEquipPosition == 3 ? -1 : 3;
                        updateItemList();
        }

        const btnAccessory2 = new Game.GUI.Button({
            left: 8,
            top: 16 * 4 + 46+50,
            width: 112,
            height: 16,
            text: () => (selectedCharactorIndex == -1 || charactors[selectedCharactorIndex].data.equips.accessory2 == null) ? "(アクセサリ２)" : Data.Item.get(charactors[selectedCharactorIndex].data.equips.accessory2.id).name,
        });
        dispatcher.add(btnAccessory2);
        btnAccessory2.click = () => {
            Game.getSound().reqPlayChannel("cursor");
            selectedEquipPosition = selectedEquipPosition == 4 ? -1 : 4;
                        updateItemList();
        }

        this.draw = () => {
            Game.getScreen().drawImage(
                Game.getScreen().texture("classroom"),
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight,
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight
            );
            dispatcher.draw();
        };

        itemListBox.visible = selectedEquipPosition != -1;
        charactorListBox.visible = selectedEquipPosition == -1;
        btnWepon1.visible = btnArmor1.visible = btnArmor2.visible = btnAccessory1.visible = btnAccessory2.visible = selectedCharactorIndex != -1;

        yield () => {
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
            itemListBox.visible = selectedEquipPosition != -1;
            charactorListBox.visible = selectedEquipPosition == -1;
            btnWepon1.visible = btnArmor1.visible = btnArmor2.visible = btnAccessory1.visible = btnAccessory2.visible = selectedCharactorIndex != -1;
            if (exitScene) {
                this.next();
            }
        };


        Game.getSceneManager().pop();
    }
}