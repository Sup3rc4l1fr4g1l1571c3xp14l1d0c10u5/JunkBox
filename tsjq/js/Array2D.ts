"use strict";

class Array2D {
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
        if (value != undefined) {
            this.matrixBuffer[y * this.arrayWidth + x] = value;
        }
        return this.matrixBuffer[y * this.arrayWidth + x];
    }

    constructor(width: number, height: number, fill?: number) {
        this.arrayWidth = width;
        this.arrayHeight = height;
        if (fill == undefined) {
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
        var matrix = new Array2D(w, h, fill);
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

module PathFinder {

    const dir4 = [
        [0, -1],
        [1, 0],
        [0, 1],
        [-1, 0]
    ];

    const dir8 = [
        [0, -1],
        [1, 0],
        [0, 1],
        [-1, 0],
        [1, -1],
        [1, 1],
        [-1, 1],
        [-1, -1]
    ];

    // 経路探索
    type PathFindObj = {
        x: number;
        y: number;
        prev: PathFindObj;
        g: number;
        distance: number;
    }


    // 基点からの重み距離算出
    export function propagation(
        array2D: Array2D,
        sx: number,
        sy: number,
        value: number,
        costs: (value: number) => number,
        opts: { left: number, top: number, right: number, bottom: number, timeout: number, topology: number }
    ) {
        opts = Object.assign({ left: 0, top: 0, right: this.width, bottom: this.height, timeout: 1000, topology: 8 },
            opts);
        const temp = new Array2D(this.width, this.height, 0);
        const topology = opts.topology;
        var dirs: number[][];
        if (topology === 4) {
            dirs = dir4;
        } else if (topology === 8) {
            dirs = dir8;
        } else {
            throw new Error("Illegal topology");
        }

        temp.value(sx, sy, value);
        const request = dirs.map(([ox, oy]) => [sx + ox, sy + oy, value]);

        var start = Date.now();
        while (request.length !== 0 && (Date.now() - start) < opts.timeout) {
            var [x, y, currentValue] = request.shift();
            if (opts.top > y || y >= opts.bottom || opts.left > x || x >= opts.right) {
                continue;
            }

            const cost = costs(this.value(x, y));
            if (cost < 0 || currentValue < cost) {
                continue;
            }

            currentValue -= cost;

            const targetPower = temp.value(x, y);
            if (currentValue <= targetPower) {
                continue;
            }

            temp.value(x, y, currentValue);

            Array.prototype.push.apply(request, dirs.map(([ox, oy]) => [x + ox, y + oy, currentValue]));
        }
        return temp;
    }

    // A*での経路探索
    export function pathfind(
        array2D: Array2D,
        fromX: number,
        fromY: number,
        toX: number,
        toY: number,
        costs: number[],
        opts?: { topology: number }
    ): [number, number][] {
        opts = Object.assign({ topology: 8 }, opts);
        var topology = opts.topology;
        var dirs: number[][];
        if (topology === 4) {
            dirs = dir4;
        } else if (topology === 8) {
            dirs = dir8;
        } else {
            throw new Error("Illegal topology");
        }


        var todo: PathFindObj[] = [];
        const add = ((x: number, y: number, prev: PathFindObj): void => {

            // distance
            var distance: number;
            switch (topology) {
                case 4:
                    distance = (Math.abs(x - fromX) + Math.abs(y - fromY));
                    break;
                case 8:
                    distance = Math.min(Math.abs(x - fromX), Math.abs(y - fromY));
                    break;
                default:
                    throw new Error("Illegal topology");
            }

            var obj: PathFindObj = {
                x: x,
                y: y,
                prev: prev,
                g: (prev ? prev.g + 1 : 0),
                distance: distance
            };

            /* insert into priority queue */

            var f = obj.g + obj.distance;
            for (let i = 0; i < todo.length; i++) {
                const item = todo[i];
                const itemF = item.g + item.distance;
                if (f < itemF || (f === itemF && distance < item.distance)) {
                    todo.splice(i, 0, obj);
                    return;
                }
            }

            todo.push(obj);
        });

        // set start position 
        add(toX, toY, null);

        const done: Map<string, PathFindObj> = new Map<string, PathFindObj>();
        while (todo.length) {
            let item = todo.shift();
            {
                const id = item.x + "," + item.y;

                if (done.has(id)) {
                    /* 探索済みなので探索しない */
                    continue;
                }
                done.set(id, item);
            }

            if (item.x === fromX && item.y === fromY) {
                /* 始点に到達したので経路を生成して返す */
                const result: [number,number][] = [];
                while (item) {
                    result.push([item.x, item.y]);
                    item = item.prev;
                }
                return result;
            } else {

                /* 隣接地点から移動可能地点を探す */
                for (let i = 0; i < dirs.length; i++) {
                    const dir = dirs[i];
                    const x = item.x + dir[0];
                    const y = item.y + dir[1];
                    const cost = costs[this.value(x, y)];

                    if (cost < 0) {
                        /* 侵入不可能 */
                        continue;
                    } else {
                        /* 移動可能地点が探索済みでないなら探索キューに追加 */
                        const id = x + "," + y;
                        if (done.has(id)) {
                            continue;
                        }
                        add(x, y, item);
                    }
                }
            }
        }

        /* 始点に到達しなかったので空の経路を返す */
        return [];
    }

    // 重み距離を使ったA*
    export function pathfindByPropergation(
        array2D: Array2D,
        fromX: number,
        fromY: number,
        toX: number,
        toY: number,
        propagation: Array2D,
        opts: { topology: number }): number[][] {
        opts = Object.assign({ topology: 8 }, opts);
        const topology = opts.topology;
        let dirs: number[][];
        if (topology === 4) {
            dirs = dir4;
        } else if (topology === 8) {
            dirs = dir8;
        } else {
            throw new Error("Illegal topology");
        }

        var todo: PathFindObj[] = [];
        const add = ((x: number, y: number, prev: PathFindObj): void => {

            // distance
            var distance = Math.abs(propagation.value(x, y) - propagation.value(fromX, fromY));
            var obj: PathFindObj = {
                x: x,
                y: y,
                prev: prev,
                g: (prev ? prev.g + 1 : 0),
                distance: distance
            };

            /* insert into priority queue */

            var f = obj.g + obj.distance;
            for (let i = 0; i < todo.length; i++) {
                const item = todo[i];
                const itemF = item.g + item.distance;
                if (f < itemF || (f === itemF && distance < item.distance)) {
                    todo.splice(i, 0, obj);
                    return;
                }
            }

            todo.push(obj);
        });

        // set start position 
        add(toX, toY, null);

        const done: Map<string, PathFindObj> = new Map<string, PathFindObj>();
        while (todo.length) {
            let item = todo.shift();
            {
                const id = item.x + "," + item.y;
                if (done.has(id)) {
                    /* 探索済みなので探索しない */
                    continue;
                }
                done.set(id, item);
            }

            if (item.x === fromX && item.y === fromY) {
                /* 始点に到達したので経路を生成して返す */
                const result: number[][] = [];
                while (item) {
                    result.push([item.x, item.y]);
                    item = item.prev;
                }
                return result;
            } else {

                /* 隣接地点から移動可能地点を探す */
                dirs.forEach((dir) => {
                    const x = item.x + dir[0];
                    const y = item.y + dir[1];
                    const pow = propagation.value(x, y);

                    if (pow === 0) {
                        /* 侵入不可能 */
                        return;
                    } else {
                        /* 移動可能地点が探索済みでないなら探索キューに追加 */
                        var id = x + "," + y;
                        if (done.has(id)) {
                            return;
                        } else {
                            add(x, y, item);
                        }
                    }
                });
            }
        }

        /* 始点に到達しなかったので空の経路を返す */
        return [];
    }

}
