"use strict";

class Array2D {

    public static DIR8: IVector[] = [
        { x: +Number.MAX_SAFE_INTEGER, y: +Number.MAX_SAFE_INTEGER },   // 0
        { x: -1, y: +1 },
        { x: +0, y: +1 },
        { x: +1, y: +1 },
        { x: -1, y: +0 },
        { x: +0, y: +0 },
        { x: +1, y: +0 },
        { x: -1, y: -1 },
        { x: +0, y: -1 },
        { x: +1, y: -1 }
    ];

    private /*@readonly@*/ arrayWidth: number;
    private /*@readonly@*/ arrayHeight: number;
    private matrixBuffer: number[];

    public get width(): number {
        return this.arrayWidth;
    }

    public get height(): number {
        return this.arrayHeight;
    }

    public value(x: number, y: number, value?: number): number {
        if (0 > x || x >= this.arrayWidth || 0 > y || y >= this.arrayHeight) {
            return 0;
        }
        if (value !== undefined) {
            this.matrixBuffer[y * this.arrayWidth + x] = value;
        }
        return this.matrixBuffer[y * this.arrayWidth + x];
    }

    constructor(width: number, height: number, fill?: number) {
        this.arrayWidth = width;
        this.arrayHeight = height;
        if (fill === undefined) {
            this.matrixBuffer = new Array<number>(width * height);
        } else {
            this.matrixBuffer = new Array<number>(width * height).fill(fill);
        }
    }

    public fill(value: number): this {
        this.matrixBuffer.fill(value);
        return this;
    }

    public dup(): Array2D {
        const m = new Array2D(this.width, this.height);
        m.matrixBuffer = this.matrixBuffer.slice();
        return m;
    }

    public static createFromArray(array: number[][], fill?: number): Array2D {
        const h = array.length;
        const w = Math.max.apply(Math, array.map(x => x.length));
        const matrix = new Array2D(w, h, fill);
        array.forEach((vy, y) => vy.forEach((vx, x) => matrix.value(x, y, vx)));
        return matrix;
    }

    public toString(): string {
        const lines: string[] = [];
        for (let y = 0; y < this.height; y++) {
            lines[y] = `|${this.matrixBuffer.slice((y + 0) * this.arrayWidth, (y + 1) * this.arrayWidth).join(", ")}|`;
        }
        return lines.join("\r\n");
    }

}
