/// <reference path="../SpriteAnimation.ts" />
namespace Scene {
    class StatusSprite extends SpriteAnimation.Animator {
        constructor(public data: GameData.PlayerData) {
            super(data.config.sprite);
        }
    }

    function drawStatusSprite(charactorData : StatusSprite, selected:boolean, left:number, top:number, width:number, height:number, anim:number) {
                if (selected) {
                    Game.getScreen().fillStyle = `rgb(24,196,195)`;
                } else {
                    Game.getScreen().fillStyle = `rgb(24,133,196)`;
                }
                Game.getScreen().fillRect(left - 0.5, top + 1 - 0.5, width, height - 2);
                Game.getScreen().strokeStyle = `rgb(12,34,98)`;
                Game.getScreen().lineWidth = 1;
                Game.getScreen().strokeRect(left - 0.5, top + 1 - 0.5, width, height - 2);

                if (charactorData != null) {
                    charactorData.setDir(2);
                    charactorData.setAnimation("move", anim/1000);
                        const animFrame = charactorData.spriteSheet.getAnimationFrame(charactorData.animName, charactorData.animFrame);
                        const sprite = charactorData.spriteSheet.gtetSprite(animFrame.sprite);

                        // キャラクター
                        Game.getScreen().drawImage(
                            charactorData.spriteSheet.getSpriteImage(sprite),
                            sprite.left,
                            sprite.top,
                            sprite.width,
                            sprite.height,
                            left-4,
                            top,
                            sprite.width,
                            sprite.height
                        );

                    Game.getScreen().font = "10px 'PixelMplus10-Regular'";
                    Game.getScreen().fillStyle = `rgb(255,255,255)`;
                    Game.getScreen().textAlign = "left";
                    Game.getScreen().textBaseline = "top";
                    Game.getScreen().fillText(charactorData.data.config.name, left + 48-8, top + 3+ 12*0);
                    Game.getScreen().fillText(`HP:${charactorData.data.hp} MP:${charactorData.data.mp}`, left + 48-8, top + 3 + 12*1);
                    Game.getScreen().fillText(`ATK:${charactorData.data.equips.reduce<GameData.EquipableItemData,number>((s, [v,k]) => s+v.atk,0)} DEF:${charactorData.data.equips.reduce<GameData.EquipableItemData,number>((s,[v,k]) => s+v.def,0)}`, left + 48-8, top + 12*2);
                }
    }

    function* organization(): IterableIterator<any> {
        const dispatcher = new Game.GUI.UIDispatcher();

        const caption = new Game.GUI.TextBox({
            left: 1,
            top: 1,
            width: 250,
            height: 42,
            text: "編成\n迷宮探索時の前衛と後衛を選択してください。",
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
            Game.getSound().reqPlayChannel("cursor");
        };

        const charactors = GameData.getPlayerIds().map(x => new StatusSprite(GameData.getPlayerData(x)));
        let team = [GameData.getPlayerIds().findIndex(x => x == GameData.forwardCharactor),GameData.getPlayerIds().findIndex(x => x == GameData.backwardCharactor)];
        let selectedSide :number= -1;
        let selectedCharactor :number = -1;
        let anim = 0;
        const charactorListBox = new Game.GUI.ListBox({
            left: 131,
            top: 46,
            width: 112,
            height: 4 * 48,
            lineHeight: 48,
            getItemCount: () => charactors.length,
            drawItem: (left: number, top: number, width: number, height: number, index: number) => {
                drawStatusSprite(charactors[index], selectedCharactor == index, left, top, width, height, anim);
            }
        });
        dispatcher.add(charactorListBox);

        charactorListBox.click = (x: number, y: number) => {
            const select = charactorListBox.getItemIndexByPosition(x, y);
            selectedCharactor = selectedCharactor == select ? null : select;
            Game.getSound().reqPlayChannel("cursor");
        };

        const forwardBtn = new Game.GUI.ImageButton({
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
        forwardBtn.draw = () => {
            Game.getScreen().fillStyle = `rgb(24,133,196)`;
            Game.getScreen().fillRect(forwardBtn.left - 0.5, forwardBtn.top + 1 - 0.5, forwardBtn.width, 12);
            Game.getScreen().strokeStyle = `rgb(12,34,98)`;
            Game.getScreen().lineWidth = 1;
            Game.getScreen().strokeRect(forwardBtn.left - 0.5, forwardBtn.top + 1 - 0.5, forwardBtn.width, 12);
            Game.getScreen().font = "10px 'PixelMplus10-Regular'";
            Game.getScreen().fillStyle = `rgb(255,255,255)`;
            Game.getScreen().textAlign = "left";
            Game.getScreen().textBaseline = "top";
            Game.getScreen().fillText("前衛", forwardBtn.left+1, forwardBtn.top + 1);

            drawStatusSprite(team[0] == -1 ? null : charactors[team[0]], selectedSide == 0, forwardBtn.left, forwardBtn.top+12, forwardBtn.width, 48, anim);
        };
        forwardBtn.click = (x: number, y: number) => {
            selectedSide = selectedSide == 0 ? -1 : 0;
            Game.getSound().reqPlayChannel("cursor");
        };
        dispatcher.add(forwardBtn);

        const backwordBtn = new Game.GUI.ImageButton({
            left: 8,
            top: 46+70,
            width: 112,
            height: 60,
            texture : null,
            texLeft : 0,
            texTop:0,
            texWidth:0,
            texHeight:0
        });
        backwordBtn.draw = () => {
            Game.getScreen().fillStyle = `rgb(24,133,196)`;
            Game.getScreen().fillRect(backwordBtn.left - 0.5, backwordBtn.top + 1 - 0.5, backwordBtn.width, 12);
            Game.getScreen().strokeStyle = `rgb(12,34,98)`;
            Game.getScreen().lineWidth = 1;
            Game.getScreen().strokeRect(backwordBtn.left - 0.5, backwordBtn.top + 1 - 0.5, backwordBtn.width, 12);
            Game.getScreen().font = "10px 'PixelMplus10-Regular'";
            Game.getScreen().fillStyle = `rgb(255,255,255)`;
            Game.getScreen().textAlign = "left";
            Game.getScreen().textBaseline = "top";
            Game.getScreen().fillText("後衛", backwordBtn.left+1, backwordBtn.top + 1);

            drawStatusSprite(team[1] == -1 ? null : charactors[team[1]], selectedSide == 1, backwordBtn.left, backwordBtn.top+12, backwordBtn.width, 48, anim);
        };
        backwordBtn.click = (x: number, y: number) => {
            selectedSide = selectedSide == 1 ? -1 : 1;
            Game.getSound().reqPlayChannel("cursor");
        };
        dispatcher.add(backwordBtn);

        this.draw = () => {
            Game.getScreen().drawImage(
                Game.getScreen().texture("classroom"),
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight,
                0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight
            );
            dispatcher.draw();
        };

        yield (delta: number, ms: number) => {
            anim = ms % 1000;
            if (Game.getInput().isDown()) {
                dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isMove()) {
                dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isUp()) {
                dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (selectedSide != -1 && selectedCharactor != -1) {
                team[selectedSide] = selectedCharactor;
                selectedSide = -1;
                selectedCharactor = -1;
            }
            if (exitScene) {
                GameData.forwardCharactor  = team[0] == -1 ? null : charactors[team[0]].data.id;
                GameData.backwardCharactor = team[1] == -1 ? null : charactors[team[1]].data.id;
                this.next();
            }
        };


        Game.getSceneManager().pop();
    }

    function* equipEdit(): IterableIterator<any> {
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
            Game.getSound().reqPlayChannel("cursor");
        };

        const charactors = GameData.getPlayerIds().map(x => new StatusSprite(GameData.getPlayerData(x)));
        let team = [GameData.getPlayerIds().findIndex(x => x == GameData.forwardCharactor),GameData.getPlayerIds().findIndex(x => x == GameData.backwardCharactor)];
        let selectedCharactor :number = -1;
        let selectedEquipPosition :number = -1;
        let anim = 0;
        const charactorListBox = new Game.GUI.ListBox({
            left: 131,
            top: 46,
            width: 112,
            height: 4 * 48,
            lineHeight: 48,
            getItemCount: () => charactors.length,
            drawItem: (left: number, top: number, width: number, height: number, index: number) => {
                drawStatusSprite(charactors[index], selectedCharactor == index, left, top, width, height, anim);
            }
        });
        dispatcher.add(charactorListBox);
        charactorListBox.click = (x: number, y: number) => {
            const select = charactorListBox.getItemIndexByPosition(x, y);
            selectedCharactor = selectedCharactor == select ? null : select;
            Game.getSound().reqPlayChannel("cursor");
        };

        let selectedItem = -1;
        const itemLists : number[] = [];

        let updateItemList = () => {
            var newItemLists = GameData.ItemBox.map((x,i) => {
                switch (selectedEquipPosition) {
                    case 0:
                        return (x.item.kind == GameData.ItemKind.Wepon) ? i : -1;
                    case 1:
                        return (x.item.kind == GameData.ItemKind.Armor1) ? i : -1;
                    case 2:
                        return (x.item.kind == GameData.ItemKind.Armor2) ? i : -1;
                    case 3:
                    case 4:
                        return (x.item.kind == GameData.ItemKind.Accessory) ? i : -1;
                    default:
                        return -1;
                }
            }).filter(x => x != -1);
            itemLists.length = 0;
            Array.prototype.push.apply(itemLists, newItemLists);
        };

        const itemListBox = new Game.GUI.ListBox({
            left: 131,
            top: 46,
            width: 112,
            height: 12 * 16,
            lineHeight: 16,
            getItemCount: () => itemLists.length,
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
                Game.getScreen().fillText(GameData.ItemBox[itemLists[index]].item.name, left + 3, top + 3);
            }
        });
        dispatcher.add(itemListBox);
        itemListBox.click = (x,y) => {
            const select = itemListBox.getItemIndexByPosition(x, y);
            Game.getSound().reqPlayChannel("cursor");
            if (select == -1) {
                return;
            }
                switch (selectedEquipPosition) {
                    case 0:
                        if (charactors[selectedCharactor].data.equips.wepon1 != null) {
                            const oldItem = charactors[selectedCharactor].data.equips.wepon1;
                            charactors[selectedCharactor].data.equips.wepon1 = GameData.ItemBox[itemLists[select]].item as GameData.EquipableItemData;
                            GameData.ItemBox[itemLists[select]].item = oldItem;
                        } else {
                            charactors[selectedCharactor].data.equips.wepon1 = GameData.ItemBox[itemLists[select]].item as GameData.EquipableItemData;
                            GameData.ItemBox.splice(itemLists[select],1);
                        }
                        updateItemList();
                        break;
                    case 1:
                        if (charactors[selectedCharactor].data.equips.armor1 != null) {
                            const oldItem = charactors[selectedCharactor].data.equips.armor1;
                            charactors[selectedCharactor].data.equips.armor1 = GameData.ItemBox[itemLists[select]].item as GameData.EquipableItemData;
                            GameData.ItemBox[itemLists[select]].item = oldItem;
                        } else {
                            charactors[selectedCharactor].data.equips.armor1 = GameData.ItemBox[itemLists[select]].item as GameData.EquipableItemData;
                            GameData.ItemBox.splice(itemLists[select],1);
                        }
                        updateItemList();
                        break;
                    case 2:
                        if (charactors[selectedCharactor].data.equips.armor2 != null) {
                            const oldItem = charactors[selectedCharactor].data.equips.armor2;
                            charactors[selectedCharactor].data.equips.armor2 = GameData.ItemBox[itemLists[select]].item as GameData.EquipableItemData;
                            GameData.ItemBox[itemLists[select]].item = oldItem;
                        } else {
                            charactors[selectedCharactor].data.equips.armor2 = GameData.ItemBox[itemLists[select]].item as GameData.EquipableItemData;
                            GameData.ItemBox.splice(itemLists[select],1);
                        }
                        updateItemList();
                        break;
                    case 3:
                        if (charactors[selectedCharactor].data.equips.accessory1 != null) {
                            const oldItem = charactors[selectedCharactor].data.equips.accessory1;
                            charactors[selectedCharactor].data.equips.accessory1 = GameData.ItemBox[itemLists[select]].item as GameData.EquipableItemData;
                            GameData.ItemBox[itemLists[select]].item = oldItem;
                        } else {
                            charactors[selectedCharactor].data.equips.accessory1 = GameData.ItemBox[itemLists[select]].item as GameData.EquipableItemData;
                            GameData.ItemBox.splice(itemLists[select],1);
                        }
                        updateItemList();
                        break;
                    case 4:
                        if (charactors[selectedCharactor].data.equips.accessory2 != null) {
                            const oldItem = charactors[selectedCharactor].data.equips.accessory2;
                            charactors[selectedCharactor].data.equips.accessory2 = GameData.ItemBox[itemLists[select]].item as GameData.EquipableItemData;
                            GameData.ItemBox[itemLists[select]].item = oldItem;
                        } else {
                            charactors[selectedCharactor].data.equips.accessory2 = GameData.ItemBox[itemLists[select]].item as GameData.EquipableItemData;
                            GameData.ItemBox.splice(itemLists[select],1);
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
            drawStatusSprite(charactors[selectedCharactor], false, statusViewBtn.left, statusViewBtn.top, statusViewBtn.width, 48, anim);
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
            text: () => (selectedCharactor == -1 || charactors[selectedCharactor].data.equips.wepon1 == null)  ? "(武器)" : charactors[selectedCharactor].data.equips.wepon1.name,
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
            text: () => (selectedCharactor == -1 || charactors[selectedCharactor].data.equips.armor1 == null) ? "(防具・上半身)" : charactors[selectedCharactor].data.equips.armor1.name,
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
            text: () => (selectedCharactor == -1 || charactors[selectedCharactor].data.equips.armor2 == null) ? "(防具・下半身)" : charactors[selectedCharactor].data.equips.armor2.name,
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
            text: () => (selectedCharactor == -1 || charactors[selectedCharactor].data.equips.accessory1 == null) ? "(アクセサリ１)" : charactors[selectedCharactor].data.equips.accessory1.name,
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
            text: () => (selectedCharactor == -1 || charactors[selectedCharactor].data.equips.accessory2 == null) ? "(アクセサリ２)" : charactors[selectedCharactor].data.equips.accessory2.name,
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
        btnWepon1.visible = btnArmor1.visible = btnArmor2.visible = btnAccessory1.visible = btnAccessory2.visible = selectedCharactor != -1;

        yield (delta: number, ms: number) => {
            anim = ms % 1000;
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
            btnWepon1.visible = btnArmor1.visible = btnArmor2.visible = btnAccessory1.visible = btnAccessory2.visible = selectedCharactor != -1;
            if (exitScene) {
                this.next();
            }
        };


        Game.getSceneManager().pop();
    }

    export function* classroom(): IterableIterator<any> {
         const fade = new Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);

        const dispatcher = new Game.GUI.UIDispatcher();

        const caption = new Game.GUI.TextBox({
            left: 1,
            top: 1,
            width: 250,
            height: 42,
            text: "教室\n探索ペアの編成や生徒の確認ができます。",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.add(caption);

        const btnOrganization = new Game.GUI.Button({
            left: 8,
            top: 20 * 0 + 46,
            width: 112,
            height: 16,
            text: "編成",
        });
        dispatcher.add(btnOrganization);
        btnOrganization.click = (x: number, y: number) => {
            Game.getSceneManager().push(organization);
            Game.getSound().reqPlayChannel("cursor");
        };

        const btnEquip = new Game.GUI.Button({
            left: 8,
            top: 20 * 1 + 46,
            width: 112,
            height: 16,
            text: "装備変更",
        });
        dispatcher.add(btnEquip);
        btnEquip.click = (x: number, y: number) => {
            Game.getSceneManager().push(equipEdit);
            Game.getSound().reqPlayChannel("cursor");
        };

        const btnItemBox = new Game.GUI.Button({
            left: 8,
            top: 20 * 2 + 46,
            width: 112,
            height: 16,
            text: "道具箱",
            visible: false
        });
        dispatcher.add(btnItemBox);
        btnItemBox.click = (x: number, y: number) => {
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

        this.draw = () => {
            Game.getScreen().drawImage(
                Game.getScreen().texture("classroom"),
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