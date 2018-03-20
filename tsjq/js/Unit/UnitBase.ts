/// <reference path="../SpriteAnimation.ts" />
/// <reference path="../Lib/Type.ts" />
"use strict";

namespace Unit {
    export abstract class UnitBase extends SpriteAnimation.Animator implements IPoint {
        public x: number;
        public y: number;

        constructor(x: number, y: number, spriteSheet: SpriteAnimation.SpriteSheet) {
            super(spriteSheet);
            this.x = x;
            this.y = y;
        }
    }
}
