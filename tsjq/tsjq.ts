/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.dom.d.ts" />
/// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/2.6/lib.es2016.d.ts" />
/// <reference path="./js/CSSFontLoadingAPI.d.ts" />
/// <reference path="./js/Lib/Random.ts" />
/// <reference path="./js/Lib/Type.ts" />
/// <reference path="./js/Lib/Game/Ajax.ts" />
/// <reference path="./js/Lib/Game/Path.ts" />
/// <reference path="./js/Lib/Game/EventDispatcher.ts" />
/// <reference path="./js/Lib/Game/Video.ts" />
/// <reference path="./js/Lib/Game/Sound.ts" />
/// <reference path="./js/Lib/Game/Input.ts" />
/// <reference path="./js/Lib/Game/Timer.ts" />
/// <reference path="./js/Lib/Game/Scene.ts" />
/// <reference path="./js/Lib/Game/Console.ts" />
/// <reference path="./js/Lib/Game/Game.ts" />
/// <reference path="./js/lib/game/GUI.ts" />
/// <reference path="./js/Array2D.ts" />
/// <reference path="./js/PathFinder.ts" />
/// <reference path="./js/MapData.ts" />
/// <reference path="./js/SpriteAnimation.ts" />
/// <reference path="./js/Unit/UnitBase.ts" />
/// <reference path="./js/Unit/Monster.ts" />
/// <reference path="./js/Unit/Player.ts" />
/// <reference path="./js/Scene/Boot.ts" />
/// <reference path="./js/Scene/ClassRoom/top.ts" />
/// <reference path="./js/Scene/Dungeon/Top.ts" />
/// <reference path="./js/Scene/Helper.ts" />
/// <reference path="./js/Scene/Dungeon/MapView.ts" />
/// <reference path="./js/Scene/Title.ts" />
/// <reference path="./js/Scene/Shop/Shop.ts" />
/// <reference path="./js/Particle.ts" />
/// <reference path="./js/Font7px.ts" />
/// <reference path="./js/Data/Item.ts" />
/// <reference path="./js/Data/Charactor.ts" />
/// <reference path="./js/Data/Monster.ts" />
/// <reference path="./js/Data/Player.ts" />
/// <reference path="./js/Data/SaveData.ts" />
/// <reference path="./js/DropItem.ts" />
"use strict";
// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />

window.onload = () => {
    Game.create({
        title: "TSJQ",
        video: {
            id: "glcanvas",
            offscreenWidth: 252,
            offscreenHeight: 252,
            scaleX: 1,
            scaleY: 1,
        }
    }).then(() => {
        Game.getSceneManager().push(new Scene.BootScene());
        Game.getTimer().on((delta, now) => {
            Game.getInput().endCapture();
            Game.getSceneManager().update(delta, now);
            Game.getInput().startCapture();
            Game.getSound().playChannel();
            Game.getScreen().begin();
            Game.getSceneManager().draw();
            Game.getScreen().end();
        });
        Game.getTimer().start();
    });
};
