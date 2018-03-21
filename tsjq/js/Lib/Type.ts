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
    removeIf(callback: (item: T, index: number) => boolean): void;
    includes(item: T): boolean;
}

Array.prototype.removeIf = function<T>(callback:(item:T, index:number) => boolean) : void {
    let i = this.length;
    while (i--) {
        if (callback(this[i], i)) {
            this.splice(i, 1);
        }
    }
};

interface Object {
    reduce<TValue, T>(callback:(seed:T, vk:[TValue, string]) => T, seed:T):T ;
}

Object.prototype.reduce = function <TValue, T>(callback: (seed: T, vk: [TValue, string]) => T, seed: T): T {
    Object.keys(this).forEach(key => seed = callback(seed, [this[key], key]));
    return seed;
};
