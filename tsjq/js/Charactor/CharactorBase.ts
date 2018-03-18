/// <reference path="../SpriteAnimation.ts" />
/// <reference path="../Lib/Type.ts" />
"use strict";

namespace Charactor {
    export abstract class CharactorBase extends SpriteAnimation.Animator implements IPoint {
        public x: number;
        public y: number;

        constructor(x: number, y: number, spriteSheet: SpriteAnimation.SpriteSheet) {
            super(spriteSheet);
            this.x = x;
            this.y = y;
        }
    }
}
