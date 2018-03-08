"use strict";

interface Array<T> {
    shuffle(): Array<T>;
}

Object.defineProperties(
    Array.prototype,
    {
        "shuffle": {
            enumerable: false,
            configurable: false,
            writable: false,
            value: function () {
                const self = this.slice();
                for (let i = self.length - 1; i > 0; i--) {
                    const r = Math.floor(Math.random() * (i + 1));
                    const tmp = self[i];
                    self[i] = self[r];
                    self[r] = tmp;
                }
                return self;
            }
        }
    }
);

interface Number {
    times<T>(pred : (i: number) => T) : IterableIterator<T>;
}

Object.defineProperties(
    Number.prototype,
    {
        "times": {
            enumerable: false,
            configurable: false,
            writable: false,
            value: function () {
                for (var i = 0; i < this; i++) {
                }
            }
        }
    }
);
