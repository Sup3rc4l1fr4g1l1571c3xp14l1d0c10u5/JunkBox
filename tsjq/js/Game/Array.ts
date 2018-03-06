"use strict";

interface Array<T> {
    shuffle(): Array<T>;
}

Array.prototype.shuffle = function () {
    const self = this.slice();
    for (let i = self.length - 1; i > 0; i--) {
        const r = Math.floor(Math.random() * (i + 1));
        const tmp = self[i];
        self[i] = self[r];
        self[r] = tmp;
    }
    return self;
}
