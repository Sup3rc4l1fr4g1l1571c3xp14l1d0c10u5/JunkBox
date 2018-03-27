"use strict";

abstract class DropItem  {
    constructor(public x:number, public y:number) {
    }
    abstract draw(cx:number,cy:number) : void;
    abstract take() : void;
}

class GoldBug extends DropItem  {
    private left : number;
    private top : number;
    constructor(x:number, y:number, public gold : number) {
        super(x, y);
        this.left = ((gold < 500) ? 0 : (gold < 10000) ? 1 : 2) * 24;
        this.top  = 0;
    }
    draw(cx: number, cy: number) {
        Game.getScreen().drawImage(
            Game.getScreen().texture("drops"),
            this.left, this.top, 24, 24,
            cx, cy, 24, 24
        );
    }
    take() {
        Game.getSound().reqPlayChannel("coin");
        Data.SaveData.money += this.gold;
    }
}

class ItemBug extends DropItem  {
    constructor(x:number, y:number, public items : Data.Item.ItemBoxEntry[]) {
        super(x, y);
    }
    draw(cx: number, cy: number) {
        Game.getScreen().drawImage(
            Game.getScreen().texture("drops"),
            0, 24, 24, 24,
            cx, cy, 24, 24
        );
    }
    take() {
        Game.getSound().reqPlayChannel("open");
        // ‚»‚Ì‚¤‚¿ã©‚Æ‚©ì‚é‚×‚«‚¾‚ë‚¤
        Data.SaveData.itemBox.push(...this.items);
    }
}
