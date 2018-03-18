"use strict";

interface IPoint {
    x: number;
    y: number;
}

interface IVector {
    x: number;
    y: number;
}

interface IRange {
    min: number;
    max: number;
}

interface Array<T> {
    removeIf(callback:(item:T, index:number) => boolean):void;
}

Array.prototype.removeIf = function<T>(callback:(item:T, index:number) => boolean) : void {
    var i = this.length;
    while (i--) {
        if (callback(this[i], i)) {
            this.splice(i, 1);
        }
    }
};
