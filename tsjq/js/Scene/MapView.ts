namespace Scene {
    export function* mapview(data: { map: Dungeon.DungeonData, player: Charactor.Player }): IterableIterator<any> {
        this.draw = () => {
            Game.getScreen().save();
            Game.getScreen().fillStyle = "rgb(0,0,0)";
            Game.getScreen().fillRect(0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);

            const offx = ~~((Game.getScreen().offscreenWidth - data.map.width * 5) / 2);
            const offy = ~~((Game.getScreen().offscreenHeight - data.map.height * 5) / 2);

            // ミニマップを描画
            for (let y = 0; y < data.map.height; y++) {
                for (let x = 0; x < data.map.width; x++) {
                    const chip = data.map.layer[0].chips.value(x, y);
                    let color = "rgb(52,12,0)";
                    switch (chip) {
                        case 1:
                            color = "rgb(179,116,39)";
                            break;
                        case 10:
                            color = "rgb(255,0,0)";
                            break;
                    }
                    Game.getScreen().fillStyle = color;
                    Game.getScreen().fillRect(offx + x * 5, offy + y * 5, 5, 5);

                    let light = 1 - data.map.visibled.value(x, y) / 100;
                    if (light > 1) {
                        light = 1;
                    } else if (light < 0) {
                        light = 0;
                    }
                    Game.getScreen().fillStyle = `rgba(0,0,0,${light})`;
                    Game.getScreen().fillRect(offx + x * 5, offy + y * 5, 5, 5);
                }
            }

            Game.getScreen().fillStyle = "rgb(0,255,0)";
            Game.getScreen().fillRect(offx + data.player.x * 5, offy + data.player.y * 5, 5, 5);
            Game.getScreen().restore();
        };
        yield waitClick({ end: () => this.next() });
        Game.getSceneManager().pop();
    }
}