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
                    Game.getScreen().fillText(`ATK:${charactorData.data.equips.reduce((s,x) => s+x.atk,0)} DEF:${charactorData.data.equips.reduce((s,x) => s+x.def,0)}`, left + 48-8, top + 12*2);
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

        const charactors = GameData.getPlayerIds().map(x => new StatusSprite(GameData.getPlayerData(x, true)));
        let team = [GameData.getPlayerIds().findIndex(x => x == GameData.forwardCharactor),GameData.getPlayerIds().findIndex(x => x == GameData.backwardCharactor)];
        let selectedSize :number= -1;
        let selectedItem :number = -1;
        let anim = 0;
        const listBox = new Game.GUI.ListBox({
            left: 131,
            top: 46,
            width: 112,
            height: 4 * 48,
            lineHeight: 48,
            getItemCount: () => charactors.length,
            drawItem: (left: number, top: number, width: number, height: number, index: number) => {
                drawStatusSprite(charactors[index], selectedItem == index, left, top, width, height, anim);
            }
        });
        dispatcher.add(listBox);

        listBox.click = (x: number, y: number) => {
            const select = listBox.getItemIndexByPosition(x, y);
            selectedItem = selectedItem == select ? null : select;
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

            drawStatusSprite(team[0] == -1 ? null : charactors[team[0]], selectedSize == 0, forwardBtn.left, forwardBtn.top+12, forwardBtn.width, 48, anim);
        };
        forwardBtn.click = (x: number, y: number) => {
            selectedSize = selectedSize == 0 ? -1 : 0;
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

            drawStatusSprite(team[1] == -1 ? null : charactors[team[1]], selectedSize == 1, backwordBtn.left, backwordBtn.top+12, backwordBtn.width, 48, anim);
        };
        backwordBtn.click = (x: number, y: number) => {
            selectedSize = selectedSize == 1 ? -1 : 1;
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
            if (selectedSize != -1 && selectedItem != -1) {
                team[selectedSize] = selectedItem;
                selectedSize = -1;
                selectedItem = -1;
            }
            if (exitScene) {
                GameData.forwardCharactor  = team[0] == -1 ? null : charactors[team[0]].data.id;
                GameData.backwardCharactor = team[1] == -1 ? null : charactors[team[1]].data.id;
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
            Game.getSound().reqPlayChannel("cursor");
        };

        const btnItemBox = new Game.GUI.Button({
            left: 8,
            top: 20 * 2 + 46,
            width: 112,
            height: 16,
            text: "道具箱",
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