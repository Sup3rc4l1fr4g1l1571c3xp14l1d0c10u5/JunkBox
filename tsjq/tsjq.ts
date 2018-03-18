/// <reference path="../../../../../../Program Files (x86)/Microsoft SDKs/TypeScript/2.5/lib.dom.d.ts" />
/// <reference path="../../../../../../Program Files (x86)/Microsoft SDKs/TypeScript/2.5/lib.es2016.d.ts" />
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
/// <reference path="./js/Dungeon.ts" />
/// <reference path="./js/SpriteAnimation.ts" />
/// <reference path="./js/charactor/charactorbase.ts" />
/// <reference path="./js/charactor/monster.ts" />
/// <reference path="./js/charactor/player.ts" />
/// <reference path="./js/Scene/Boot.ts" />
/// <reference path="./js/Scene/ClassRoom.ts" />
/// <reference path="./js/Scene/Dungeon.ts" />
/// <reference path="./js/Scene/Helper.ts" />
/// <reference path="./js/Scene/MapView.ts" />
/// <reference path="./js/Scene/Title.ts" />
/// <reference path="./js/Scene/Shop.ts" />
/// <reference path="./js/Sprite.ts" />
"use strict";
// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />

window.onload = () => {
    Game.create({
        title: "TSJQ",
        video: {
            id: "glcanvas",
            offscreenWidth: 252,
            offscreenHeight: 252,
            scaleX: 2,
            scaleY: 2,
        }
    }).then(() => {
        Game.getSceneManager().push(Scene.boot, null);
        Game.getTimer().on((delta, now, id) => {
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
