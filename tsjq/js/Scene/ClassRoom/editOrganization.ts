/// <reference path="../../SpriteAnimation.ts" />
namespace Scene.ClassRoom {
    export class EditOrganization implements Game.Scene.Scene {
        draw() {}
        update() {}

        constructor() {
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
                Data.SaveData.save();
                Game.getSound().reqPlayChannel("cursor");
            };

            const charactors = Data.Charactor.keys().map(x => new StatusSprite(Data.SaveData.findCharactorById(x)));
            let team = [
                Data.Charactor.keys().findIndex(x => x == Data.SaveData.forwardCharactor),
                Data.Charactor.keys().findIndex(x => x == Data.SaveData.backwardCharactor)
            ];
            let selectedSide: number = -1;
            let selectedCharactorIndex: number = -1;
            let anim = 0;
            const charactorListBox = new Game.GUI.ListBox({
                left: 131,
                top: 46,
                width: 112 + 1,
                height: 4 * 48,
                lineHeight: 48,
                getItemCount: () => charactors.length,
                drawItem: (left: number, top: number, width: number, height: number, index: number) => {
                    drawStatusSprite(charactors[index],
                        team.includes(index)
                        ? DrawMode.Disable
                        : (selectedCharactorIndex == index)
                        ? DrawMode.Selected
                        : DrawMode.Normal,
                        left,
                        top,
                        width,
                        height,
                        anim);
                }
            });
            dispatcher.add(charactorListBox);

            charactorListBox.click = (x: number, y: number) => {
                const select = charactorListBox.getItemIndexByPosition(x, y);
                if (team.includes(select)) {
                    return;
                }
                selectedCharactorIndex = selectedCharactorIndex == select ? null : select;
                Game.getSound().reqPlayChannel("cursor");
            };

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

                drawStatusSprite(team[0] == -1 ? null : charactors[team[0]],
                    selectedSide == 0 ? DrawMode.Selected : DrawMode.Normal,
                    forwardBtn.left,
                    forwardBtn.top + 12,
                    forwardBtn.width,
                    48,
                    anim);
            };
            forwardBtn.click = (x: number, y: number) => {
                selectedSide = selectedSide == 0 ? -1 : 0;
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

                drawStatusSprite(team[1] == -1 ? null : charactors[team[1]],
                    selectedSide == 1 ? DrawMode.Selected : DrawMode.Normal,
                    backwordBtn.left,
                    backwordBtn.top + 12,
                    backwordBtn.width,
                    48,
                    anim);
            };
            backwordBtn.click = (x: number, y: number) => {
                selectedSide = selectedSide == 1 ? -1 : 1;
                Game.getSound().reqPlayChannel("cursor");
            };
            dispatcher.add(backwordBtn);

            this.draw = () => {
                Game.getScreen().drawImage(
                    Game.getScreen().texture("classroom"),
                    0,
                    0,
                    Game.getScreen().offscreenWidth,
                    Game.getScreen().offscreenHeight,
                    0,
                    0,
                    Game.getScreen().offscreenWidth,
                    Game.getScreen().offscreenHeight
                );
                dispatcher.draw();
            };

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
                if (selectedSide != -1 && selectedCharactorIndex != -1) {
                    team[selectedSide] = selectedCharactorIndex;
                    selectedSide = -1;
                    selectedCharactorIndex = -1;
                }
                if (exitScene) {
                    Data.SaveData.forwardCharactor = team[0] == -1 ? null : charactors[team[0]].data.id;
                    Data.SaveData.backwardCharactor = team[1] == -1 ? null : charactors[team[1]].data.id;
                    Game.getSceneManager().pop();
                }
            };
        }
    }

}