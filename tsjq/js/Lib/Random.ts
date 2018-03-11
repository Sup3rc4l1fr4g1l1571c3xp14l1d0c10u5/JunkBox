"use strict";

class XorShift {
    private seeds: { x: number; y: number; z: number; w: number };
    private randCount: number;
    private generator: IterableIterator<number>;

    constructor(
        w: number = 0 | Date.now(),
        x?: number,
        y?: number,
        z?: number
    ) {
        if (x === undefined) { x = (0 | (w << 13)); }
        if (y === undefined) { y = (0 | ((w >>> 9) ^ (x << 6))); }
        if (z === undefined) { z = (0 | (y >>> 7)); }
        this.seeds = { x: x >>> 0, y: y >>> 0, z: z >>> 0, w: w >>> 0 };
        // Object.defineProperty(this, "seeds", { writable: false });
        this.randCount = 0;
        this.generator = this.randGen(w, x, y, z);
    }

    private *randGen(w: number, x: number, y: number, z: number) {
        let t: number;
        for (; ;) {
            t = x ^ (x << 11);
            x = y;
            y = z;
            z = w;
            yield w = ((w ^ (w >>> 19)) ^ (t ^ (t >>> 8))) >>> 0;
        }
    }

    public rand(): number {
        this.randCount = 0 | this.randCount + 1;
        return this.generator.next().value;
    }

    public randInt(min: number = 0, max: number = 0x7FFFFFFF): number {
        return 0 | this.rand() % (max + 1 - min) + min;
    }

    public randFloat(min: number = 0, max: number = 1): number {
        return Math.fround(this.rand() % 0xFFFF / 0xFFFF) * (max - min) + min;
    }

    public shuffle<T>(target: T[]): T[] {
        const arr = target.concat();
        for (let i = 0; i <= arr.length - 2; i = 0 | i + 1) {
            const r = this.randInt(i, arr.length - 1);
            const tmp = arr[i];
            arr[i] = arr[r];
            arr[r] = tmp;
        }
        return arr;
    }

    public getWeightedValue(data: { [key: string]: number }): string {
        const keys = Object.keys(data);
        const total = keys.reduce((s, x) => s + data[x], 0);
        const random = this.randInt(0, total);

        let part = 0;
        for (const id of keys) {
            part += data[id];
            if (random < part) {
                return id;
            }
        }

        return keys[keys.length - 1];
    }

    private static defaults: { x: number; y: number; z: number; w: number } = {
        x: 123456789,
        y: 362436069,
        z: 521288629,
        w: 88675123
    };

    public static default(): XorShift {
        return new XorShift(
            XorShift.defaults.w,
            XorShift.defaults.x,
            XorShift.defaults.y,
            XorShift.defaults.z
        );
    }
}
