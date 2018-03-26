namespace Scene.Dungeon {

    function showStatusText(str: string, x:number, y:number): void {
        const fontWidth: number = 5;
        const fontHeight: number = 7;

        const len = str.length;
        for (let i = 0; i < str.length; i++) {
            const [fx, fy] = Font7px.charDic[str[i]];
            Game.getScreen().drawImage(
                Game.getScreen().texture("font7wpx"),
                fx,
                fy,
                fontWidth,
                fontHeight,
                (x + (i + 0) * (fontWidth - 1)),
                (y + (0) * fontHeight),
                fontWidth,
                fontHeight
            );
        }
    }

    export function* statusView(opt: { player: Unit.Player, floor: number, upperdraw: () => void }): IterableIterator<any> {
        const closeButton = {
            x: Game.getScreen().offscreenWidth - 20,
            y: 20,
            radius: 10
        };

        this.draw = () => {
            opt.upperdraw();
            //Game.getScreen().fillStyle = 'rgba(255,255,255,0.5)';
            //Game.getScreen().fillRect(20,
            //    20,
            //    Game.getScreen().offscreenWidth - 40,
            //    Game.getScreen().offscreenHeight - 40);

            //// 閉じるボタン
            //Game.getScreen().save();
            //Game.getScreen().beginPath();
            //Game.getScreen().strokeStyle = 'rgba(255,255,255,1)';
            //Game.getScreen().lineWidth = 6;
            //Game.getScreen().ellipse(closeButton.x, closeButton.y, closeButton.radius, closeButton.radius, 0, 0, 360);
            //Game.getScreen().moveTo(closeButton.x - Math.sqrt(2) * closeButton.radius / 2,
            //    closeButton.y - Math.sqrt(2) * closeButton.radius / 2);
            //Game.getScreen().lineTo(closeButton.x + Math.sqrt(2) * closeButton.radius / 2,
            //    closeButton.y + Math.sqrt(2) * closeButton.radius / 2);
            //Game.getScreen().moveTo(closeButton.x - Math.sqrt(2) * closeButton.radius / 2,
            //    closeButton.y + Math.sqrt(2) * closeButton.radius / 2);
            //Game.getScreen().lineTo(closeButton.x + Math.sqrt(2) * closeButton.radius / 2,
            //    closeButton.y - Math.sqrt(2) * closeButton.radius / 2);
            //Game.getScreen().stroke();
            //Game.getScreen().strokeStyle = 'rgba(128,255,255,1)';
            //Game.getScreen().lineWidth = 3;
            //Game.getScreen().stroke();
            //Game.getScreen().restore();

            // ステータス(前衛)
            {
            const left = ~~((Game.getScreen().offscreenWidth - 190) / 2);
            const top = ~~((Game.getScreen().offscreenHeight - 121*2) / 2);
            Game.getScreen().drawImage(
                Game.getScreen().texture("status"),
                0, 0, 190, 121,
                left,
                top,
                190, 121
            );
            Game.getScreen().font = "10px 'PixelMplus10-Regular'";
            Game.getScreen().fillStyle = `rgb(0,0,0)`;
            Game.getScreen().textAlign = "left";
            Game.getScreen().textBaseline = "top";
            Game.getScreen().fillText(opt.player.getForward().name, left + 110, top + 36);
            showStatusText(`${opt.player.getForward().hp}/${opt.player.getForward().hpMax}`,left+85,top+56);
            showStatusText(`${opt.player.getForward().mp}/${opt.player.getForward().mpMax}`, left + 145, top + 56);
            showStatusText(`${opt.player.getForward().equips.reduce<Data.Item.ItemBoxEntry, number>((s, [v, k]) => s + (v == null ? 0 : Data.Item.get(v.id).atk), 0)}`, left + 85, top + 64);
            showStatusText(`${opt.player.getForward().equips.reduce<Data.Item.ItemBoxEntry, number>((s, [v, k]) => s + (v == null ? 0 : Data.Item.get(v.id).def), 0)}`,left+145,top+64);
            }
            // 後衛
            {
            const left = ~~((Game.getScreen().offscreenWidth - 190) / 2);
            const top = ~~((Game.getScreen().offscreenHeight - 121*2) / 2) + 121;
            Game.getScreen().drawImage(
                Game.getScreen().texture("status"),
                0, 0, 190, 121,
                left,
                top,
                190, 121
            );
            Game.getScreen().font = "10px 'PixelMplus10-Regular'";
            Game.getScreen().fillStyle = `rgb(0,0,0)`;
            Game.getScreen().textAlign = "left";
            Game.getScreen().textBaseline = "top";
            Game.getScreen().fillText(opt.player.getBackward().name, left + 110, top + 36);
            showStatusText(`${opt.player.getBackward().hp}/${opt.player.getBackward().hpMax}`,left+85,top+56);
            showStatusText(`${opt.player.getBackward().mp}/${opt.player.getBackward().mpMax}`,left+145,top+56);
            showStatusText(`${opt.player.getBackward().equips.reduce<Data.Item.ItemBoxEntry, number>((s, [v, k]) => s + (v == null ? 0 : Data.Item.get(v.id).atk),0)}`,left+85,top+64);
            showStatusText(`${opt.player.getBackward().equips.reduce<Data.Item.ItemBoxEntry, number>((s, [v, k]) => s + (v == null ? 0 : Data.Item.get(v.id).def),0)}`,left+145,top+64);
            }
            //opt.player.equips.forEach((e, i) => {
            //    Game.getScreen().fillText(`${e.name}`, left + 12, top + 144 + 12 * i);
            //})


        }
        yield waitClick({
            end: (x, y) => {
                this.next();
            }
        });
        Game.getSceneManager().pop();
        return;
    }
}