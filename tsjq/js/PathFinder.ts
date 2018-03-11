"use strict";

namespace PathFinder {
    // �o�H�T��
    type PathFindObj = {
        x: number;
        y: number;
        prev: PathFindObj;
        g: number;
        distance: number;
    }

    const dir4: IVector[] = [
        { x: 0, y: -1 },
        { x: 1, y: 0 },
        { x: 0, y: 1 },
        { x: -1, y: 0 }
    ];

    const dir8: IVector[] = [
        { x: 0, y: -1 },
        { x: 1, y: 0 },
        { x: 0, y: 1 },
        { x: -1, y: 0 },
        { x: 1, y: -1 },
        { x: 1, y: 1 },
        { x: -1, y: 1 },
        { x: -1, y: -1 }
    ];

    // �_�C�N�X�g���@��p���������Z�o
    export function calcDistanceByDijkstra({
        array2D = null,
        sx = null,      // �T���n�_X���W
        sy = null,      // �T���n�_Y���W
        value = null,   // �T���ł��؂��臒l
        costs = null,   // �m�[�h�̏d��
        left = 0, top = 0, right = undefined, bottom = undefined, timeout = 1000, topology = 8, output = undefined
    }: {
            array2D: Array2D;
            sx: number;
            sy: number;
            value: number;
            costs: (value: number) => number;
            left?: number;
            top?: number;
            right?: number;
            bottom?: number;
            timeout?: number;
            topology?: number;
            output?: (x: number, y: number, value: number) => void;

        }): void {
        if (left === undefined || left < 0) { right = 0; }
        if (top === undefined || top < 0) { bottom = 0; }
        if (right === undefined || right > array2D.width) { right = array2D.width; }
        if (bottom === undefined || bottom > array2D.height) { bottom = array2D.height; }
        if (output === undefined) { output = () => { } }

        const dirs = (topology === 8) ? dir8 : dir4;

        const work = new Array2D(array2D.width, array2D.height);

        work.value(sx, sy, value); output(sx, sy, value);

        const request = dirs.map(({ x, y }) => [sx + x, sy + y, value]);

        const  start = Date.now();
        while (request.length !== 0 && (Date.now() - start) < timeout) {
            const [px, py, currentValue] = request.shift();
            if (top > py || py >= bottom || left > px || px >= right) {
                continue;
            }

            const cost = costs(array2D.value(px, py));
            if (cost < 0 || currentValue < cost) {
                continue;
            }

            const nextValue = currentValue - cost;

            const targetPower = work.value(px, py);
            if (nextValue <= targetPower) {
                continue;
            }

            work.value(px, py, nextValue); output(px, py, nextValue);

            Array.prototype.push.apply(request, dirs.map(({ x, y }) => [px + x, py + y, nextValue]));
        }
    }

    // A*�ł̌o�H�T��
    export function pathfind(
        array2D: Array2D,
        fromX: number,
        fromY: number,
        toX: number,
        toY: number,
        costs: number[],
        opts?: { topology: number }
    ): IPoint[] {
        opts = Object.assign({ topology: 8 }, opts);
        const topology = opts.topology;
        let dirs: IVector[];
        if (topology === 4) {
            dirs = dir4;
        } else if (topology === 8) {
            dirs = dir8;
        } else {
            throw new Error("Illegal topology");
        }

        const todo: PathFindObj[] = [];
        const add = ((x: number, y: number, prev: PathFindObj): void => {

            // distance
            let distance: number;
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

            const obj: PathFindObj = {
                x: x,
                y: y,
                prev: prev,
                g: (prev ? prev.g + 1 : 0),
                distance: distance
            };

            /* insert into priority queue */

            const f = obj.g + obj.distance;
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
                    /* �T���ς݂Ȃ̂ŒT�����Ȃ� */
                    continue;
                }
                done.set(id, item);
            }

            if (item.x === fromX && item.y === fromY) {
                /* �n�_�ɓ��B�����̂Ōo�H�𐶐����ĕԂ� */
                const result: IPoint[] = [];
                while (item) {
                    result.push(item);
                    item = item.prev;
                }
                return result;
            } else {

                /* �אڒn�_����ړ��\�n�_��T�� */
                for (let i = 0; i < dirs.length; i++) {
                    const dir = dirs[i];
                    const x = item.x + dir.x;
                    const y = item.y + dir.y;
                    const cost = costs[this.value(x, y)];

                    if (cost < 0) {
                        /* �N���s�\ */
                        continue;
                    } else {
                        /* �ړ��\�n�_���T���ς݂łȂ��Ȃ�T���L���[�ɒǉ� */
                        const id = x + "," + y;
                        if (done.has(id)) {
                            continue;
                        }
                        add(x, y, item);
                    }
                }
            }
        }

        /* �n�_�ɓ��B���Ȃ������̂ŋ�̌o�H��Ԃ� */
        return [];
    }

    // �d�݋������g����A*
    export function pathfindByPropergation(
        array2D: Array2D,
        fromX: number,
        fromY: number,
        toX: number,
        toY: number,
        propagation: Array2D,
        { topology = 8 }: { topology?: number }
    ): IPoint[] {
        let dirs: IVector[];
        if (topology === 4) {
            dirs = dir4;
        } else if (topology === 8) {
            dirs = dir8;
        } else {
            throw new Error("Illegal topology");
        }

        const todo: PathFindObj[] = [];
        const add = ((x: number, y: number, prev: PathFindObj): void => {

            // distance
            const distance = Math.abs(propagation.value(x, y) - propagation.value(fromX, fromY));
            const obj: PathFindObj = {
                x: x,
                y: y,
                prev: prev,
                g: (prev ? prev.g + 1 : 0),
                distance: distance
            };

            /* insert into priority queue */

            const f = obj.g + obj.distance;
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
                    /* �T���ς݂Ȃ̂ŒT�����Ȃ� */
                    continue;
                }
                done.set(id, item);
            }

            if (item.x === fromX && item.y === fromY) {
                /* �n�_�ɓ��B�����̂Ōo�H�𐶐����ĕԂ� */
                const result: IPoint[] = [];
                while (item) {
                    result.push(item);
                    item = item.prev;
                }
                return result;
            } else {

                /* �אڒn�_����ړ��\�n�_��T�� */
                dirs.forEach((dir) => {
                    const x = item.x + dir.x;
                    const y = item.y + dir.y;
                    const pow = propagation.value(x, y);

                    if (pow === 0) {
                        /* �N���s�\ */
                        return;
                    } else {
                        /* �ړ��\�n�_���T���ς݂łȂ��Ȃ�T���L���[�ɒǉ� */
                        const id = x + "," + y;
                        if (done.has(id)) {
                            return;
                        } else {
                            add(x, y, item);
                        }
                    }
                });
            }
        }

        /* �n�_�ɓ��B���Ȃ������̂ŋ�̌o�H��Ԃ� */
        return [];
    }

}
