"use strict";

module Dungeon {


    type LayerConfig = {
        texture: string;    // マップチップテクスチャ
        chip: { [key: number]: { x: number; y: number } };  // マップチップ
        chips: Matrix;      // マップデータ
    }

    // マップ描画時の視点・視野情報
    export class Camera {
        width: number;
        height: number;
        left: number;
        top: number;
        right: number;
        bottom: number;
        localPx: number;
        localPy: number;
        chipX: number;
        chipY: number;
        chipOffX: number;
        chipOffY: number;
    }

    // 経路探索
    type PathFindObj = {
        x: number;
        y: number;
        prev: PathFindObj;
        g: number;
        distance: number;
    }

    // マス目
    export class Matrix {
        private readonly matrixWidth: number;
        private readonly matrixHeight: number;
        private matrixBuffer: number[];

        public get width(): number {
            return this.matrixWidth;
        }

        public get height(): number {
            return this.matrixHeight;
        }

        public value(x: number, y: number, value?: number): number {
            if (0 > x || x >= this.matrixWidth || 0 > y || y >= this.matrixHeight) {
                return 0;
            }
            if (value != undefined) {
                this.matrixBuffer[y * this.matrixWidth + x] = value;
            }
            return this.matrixBuffer[y * this.matrixWidth + x];
        }

        constructor(width: number, height: number, fill?: number) {
            this.matrixWidth = width;
            this.matrixHeight = height;
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

        public dup(): Matrix {
            const m = new Matrix(this.width, this.height);
            m.matrixBuffer = this.matrixBuffer.slice();
            return m;
        }

        public static createFromArray(array: number[][], fill?: number): Matrix {
            const h = array.length;
            const w = Math.max.apply(Math, array.map(x => x.length));
            var matrix = new Matrix(w, h, fill);
            array.forEach((vy, y) => vy.forEach((vx, x) => matrix.value(x, y, vx)));
            return matrix;
        }

        public toString(): string {
            const lines: string[] = [];
            for (let y = 0; y < this.height; y++) {
                lines[y] = `|${this.matrixBuffer.slice((y + 0) * this.matrixWidth, (y + 1) * this.matrixWidth).join(", ")}|`;
            }
            return lines.join("\r\n");
        }

        private static dir4 = [
            [0, -1],
            [1, 0],
            [0, 1],
            [-1, 0]
        ];

        private static dir8 = [
            [0, -1],
            [1, 0],
            [0, 1],
            [-1, 0]
        ];

        // 基点からの重み距離算出
        public propagation(
            sx: number,
            sy: number,
            value: number,
            costs: (value: number) => number,
            opts: { left: number, top: number, right: number, bottom: number, timeout: number, topology: number }
        ) {
            opts = Object.assign({ left: 0, top: 0, right: this.width, bottom: this.height, timeout: 1000, topology: 8 },
                opts);
            const temp = new Matrix(this.width, this.height, 0);
            const topology = opts.topology;
            var dirs: number[][];
            if (topology === 4) {
                dirs = Matrix.dir4;
            } else if (topology === 8) {
                dirs = Matrix.dir8;
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
        public pathfind(
            fromX: number,
            fromY: number,
            toX: number,
            toY: number,
            costs: number[],
            opts: { topology: number }
        ): number[][] {
            opts = Object.assign({ topology: 8 }, opts);
            var topology = opts.topology;
            var dirs: number[][];
            if (topology === 4) {
                dirs = Matrix.dir4;
            } else if (topology === 8) {
                dirs = Matrix.dir8;
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
                    const result: number[][] = [];
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
        pathfindByPropergation(fromX: number,
            fromY: number,
            toX: number,
            toY: number,
            propagation: Matrix,
            opts: { topology: number }): number[][] {
            opts = Object.assign({ topology: 8 }, opts);
            const topology = opts.topology;
            let dirs: number[][];
            if (topology === 4) {
                dirs = Matrix.dir4;
            } else if (topology === 8) {
                dirs = Matrix.dir8;
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

    // ダンジョンデータ
    export class DungeonData {
        width: number;
        height: number;
        gridsize: { width: number; height: number };
        layer: { [key: number]: LayerConfig };
        lighting: Matrix;
        visibled: Matrix;

        camera: Camera;

        constructor(config: {
            width: number;
            height: number;
            gridsize: { width: number; height: number };
            layer: { [key: number]: LayerConfig };
        }
        ) {
            this.width = config.width;
            this.height = config.height;
            this.gridsize = config.gridsize;
            this.layer = config.layer;
            this.camera = new Camera();
            this.lighting = new Matrix(this.width, this.height, 0);
            this.visibled = new Matrix(this.width, this.height, 0);
        }

        clearLighting(): DungeonData {
            this.lighting.fill(0);
            return this;
        }

        // update camera
        update(param: {
            viewpoint: { x: number; y: number };
            viewwidth: number;
            viewheight: number;
        }) {
            var mapWidth = this.width * this.gridsize.width;
            var mapHeight = this.height * this.gridsize.height;

            // マップ上でのカメラの注視点
            var mapPx = param.viewpoint.x;
            var mapPy = param.viewpoint.y;

            // カメラの視野の幅・高さ
            this.camera.width = param.viewwidth;
            this.camera.height = param.viewheight;

            // カメラの注視点が中心となるようなカメラの視野
            this.camera.left = ~~(mapPx - this.camera.width / 2);
            this.camera.top = ~~(mapPy - this.camera.height / 2);
            this.camera.right = this.camera.left + this.camera.width;
            this.camera.bottom = this.camera.top + this.camera.height;

            // 視野をマップ内に補正
            if ((this.camera.left < 0) && (this.camera.right - this.camera.left < mapWidth)) {
                this.camera.right -= this.camera.left;
                this.camera.left = 0;
            } else if ((this.camera.right >= mapWidth) && (this.camera.left - (this.camera.right - mapWidth) >= 0)) {
                this.camera.left -= (this.camera.right - mapWidth);
                this.camera.right = mapWidth - 1;
            }
            if ((this.camera.top < 0) && (this.camera.bottom - this.camera.top < mapHeight)) {
                this.camera.bottom -= this.camera.top;
                this.camera.top = 0;
            } else if ((this.camera.bottom >= mapHeight) && (this.camera.top - (this.camera.bottom - mapHeight) >= 0)) {
                this.camera.top -= (this.camera.bottom - mapHeight);
                this.camera.bottom = mapHeight - 1;
            }

            // 視野の左上位置を原点とした注視点を算出
            this.camera.localPx = mapPx - this.camera.left;
            this.camera.localPy = mapPy - this.camera.top;

            // 視野の左上位置に対応するマップチップ座標を算出
            this.camera.chipX = ~~(this.camera.left / this.gridsize.width);
            this.camera.chipY = ~~(this.camera.top / this.gridsize.height);

            // 視野の左上位置をにマップチップをおいた場合のスクロールによるズレ量を算出
            this.camera.chipOffX = -(this.camera.left % this.gridsize.width);
            this.camera.chipOffY = -(this.camera.top % this.gridsize.height);

        }

        draw(layerDrawHook) {
            // 描画開始
            var gridw = this.gridsize.width;
            var gridh = this.gridsize.height;
            var yy = ~~(this.camera.height / gridh + 1);
            var xx = ~~(this.camera.width / gridw + 1);

            Object.keys(this.layer).forEach((key) => {
                var l = ~~key;
                for (let y = -1; y < yy; y++) {
                    for (let x = -1; x < xx; x++) {
                        const chipid = this.layer[l].chips.value(x + this.camera.chipX, y + this.camera.chipY) || 0;
                        if (this.layer[l].chip[chipid]) {
                            Game.getScreen().drawImage(
                                Game.getScreen().texture(this.layer[l].texture),
                                this.layer[l].chip[chipid].x,
                                this.layer[l].chip[chipid].y,
                                gridw,
                                gridh,
                                0 + x * gridw + this.camera.chipOffX + gridw / 2,
                                0 + y * gridh + this.camera.chipOffY + gridh / 2,
                                gridw,
                                gridh
                            );
                        }
                    }
                }

                // レイヤー描画フック
                layerDrawHook(l, this.camera.localPx, this.camera.localPy);
            });

            // 照明描画
            for (let y = -1; y < yy; y++) {
                for (let x = -1; x < xx; x++) {
                    let light = this.lighting.value(x + this.camera.chipX, y + this.camera.chipY) / 100;
                    if (light > 1) {
                        light = 1;
                    } else if (light < 0) {
                        light = 0;
                    }
                    Game.getScreen().fillStyle = `rgba(0,0,0,${1 - light})`;
                    Game.getScreen().fillRect(
                        0 + x * gridw + this.camera.chipOffX + gridw / 2,
                        0 + y * gridh + this.camera.chipOffY + gridh / 2,
                        gridw,
                        gridh
                    );
                }
            }

        }
    }

    //ダンジョン自動生成
    export module Generator　 {

        abstract class Feature {
            isValid(isWallCallback, canBeDugCallback) { };
            create(digCallback) { };
            debug() { };
            static createRandomAt(x, y, dx, dy, options) { };
        }

        function getUniformInt(lowerBound, upperBound) {
            const max = Math.max(lowerBound, upperBound);
            const min = Math.min(lowerBound, upperBound);
            return Math.floor(Math.random() * (max - min + 1)) + min;
        }

        function getWeightedValue(data) {
            let total = 0;

            for (var id in data) {
                total += data[id];
            }
            const random = Math.random() * total;

            let part = 0;
            for (var id in data) {
                part += data[id];
                if (random < part) {
                    return id;
                }
            }

            return id;
        }

        class Room extends Feature {
            _x1: number;
            _y1: number;
            _x2: number;
            _y2: number;
            _doors: { [key: string]: number }; // key = corrd

            constructor(x1: number, y1: number, x2: number, y2: number, doorX?: number, doorY?: number) {
                super();
                this._x1 = x1;
                this._y1 = y1;
                this._x2 = x2;
                this._y2 = y2;
                this._doors = {};
                if (arguments.length > 4) {
                    this.addDoor(doorX, doorY);
                }
            }

            static createRandomAt(x, y, dx, dy, options) {
                const minw = options.roomWidth[0];
                const maxw = options.roomWidth[1];
                const width = getUniformInt(minw, maxw);

                const minh = options.roomHeight[0];
                const maxh = options.roomHeight[1];
                const height = getUniformInt(minh, maxh);

                if (dx === 1) { /* to the right */
                    let y2 = y - Math.floor(Math.random() * height);
                    return new Room(x + 1, y2, x + width, y2 + height - 1, x, y);
                }

                if (dx === -1) { /* to the left */
                    let y2 = y - Math.floor(Math.random() * height);
                    return new Room(x - width, y2, x - 1, y2 + height - 1, x, y);
                }

                if (dy === 1) { /* to the bottom */
                    let x2 = x - Math.floor(Math.random() * width);
                    return new Room(x2, y + 1, x2 + width - 1, y + height, x, y);
                }

                if (dy === -1) { /* to the top */
                    let x2 = x - Math.floor(Math.random() * width);
                    return new Room(x2, y - height, x2 + width - 1, y - 1, x, y);
                }

                throw new Error("dx or dy must be 1 or -1");

            };

            static createRandomCenter(cx, cy, options) {
                const minw = options.roomWidth[0];
                const maxw = options.roomWidth[1];
                const width = getUniformInt(minw, maxw);

                const minh = options.roomHeight[0];
                const maxh = options.roomHeight[1];
                const height = getUniformInt(minh, maxh);

                const x1 = cx - Math.floor(Math.random() * width);
                const y1 = cy - Math.floor(Math.random() * height);
                const x2 = x1 + width - 1;
                const y2 = y1 + height - 1;

                return new Room(x1, y1, x2, y2);
            };

            static createRandom(availWidth, availHeight, options) {
                const minw = options.roomWidth[0];
                const maxw = options.roomWidth[1];
                const width = getUniformInt(minw, maxw);

                const minh = options.roomHeight[0];
                const maxh = options.roomHeight[1];
                const height = getUniformInt(minh, maxh);

                const left = availWidth - width - 1;
                const top = availHeight - height - 1;

                const x1 = 1 + Math.floor(Math.random() * left);
                const y1 = 1 + Math.floor(Math.random() * top);
                const x2 = x1 + width - 1;
                const y2 = y1 + height - 1;

                return new Room(x1, y1, x2, y2);
            };

            addDoor(x, y) {
                this._doors[x + "," + y] = 1;
                return this;
            };

            getDoors(callback) {
                for (let key in this._doors) {
                    const parts = key.split(",");
                    callback(parseInt(parts[0]), parseInt(parts[1]));
                }
                return this;
            };

            clearDoors() {
                this._doors = {};
                return this;
            };

            addDoors(isWallCallback) {
                const left = this._x1 - 1;
                const right = this._x2 + 1;
                const top = this._y1 - 1;
                const bottom = this._y2 + 1;

                for (let x = left; x <= right; x++) {
                    for (let y = top; y <= bottom; y++) {
                        if (x != left && x != right && y != top && y != bottom) {
                            continue;
                        }
                        if (isWallCallback(x, y)) {
                            continue;
                        }

                        this.addDoor(x, y);
                    }
                }

                return this;
            };

            debug() {
                consolere.log("room", this._x1, this._y1, this._x2, this._y2);
            };

            isValid(isWallCallback, canBeDugCallback) {
                const left = this._x1 - 1;
                const right = this._x2 + 1;
                const top = this._y1 - 1;
                const bottom = this._y2 + 1;

                for (let x = left; x <= right; x++) {
                    for (let y = top; y <= bottom; y++) {
                        if (x === left || x === right || y === top || y === bottom) {
                            if (!isWallCallback(x, y)) {
                                return false;
                            }
                        } else {
                            if (!canBeDugCallback(x, y)) {
                                return false;
                            }
                        }
                    }
                }

                return true;
            };

            /**
             * @param {function} digCallback Dig callback with a signature (x, y, value). Values: 0 = empty, 1 = wall, 2 = door. Multiple doors are allowed.
             */
            create(digCallback) {
                const left = this._x1 - 1;
                const right = this._x2 + 1;
                const top = this._y1 - 1;
                const bottom = this._y2 + 1;

                let value = 0;
                for (let x = left; x <= right; x++) {
                    for (let y = top; y <= bottom; y++) {
                        if (x + "," + y in this._doors) {
                            value = 2;
                        } else if (x === left || x === right || y === top || y === bottom) {
                            value = 1;
                        } else {
                            value = 0;
                        }
                        digCallback(x, y, value);
                    }
                }
            };

            getCenter() {
                return [Math.round((this._x1 + this._x2) / 2), Math.round((this._y1 + this._y2) / 2)];
            };

            getLeft() {
                return this._x1;
            };

            getRight() {
                return this._x2;
            };

            getTop() {
                return this._y1;
            };

            getBottom() {
                return this._y2;
            };

        };

        class Corridor extends Feature {
            _startX: number;
            _startY: number;
            _endX: number;
            _endY: number;
            _endsWithAWall: boolean;

            constructor(startX: number, startY: number, endX: number, endY: number) {
                super();
                this._startX = startX;
                this._startY = startY;
                this._endX = endX;
                this._endY = endY;
                this._endsWithAWall = true;
            }

            static createRandomAt(x, y, dx, dy, options) {
                const min = options.corridorLength[0];
                const max = options.corridorLength[1];
                const length = getUniformInt(min, max);

                return new Corridor(x, y, x + dx * length, y + dy * length);
            };

            debug() {
                consolere.log("corridor", this._startX, this._startY, this._endX, this._endY);
            };

            isValid(isWallCallback, canBeDugCallback) {
                const sx = this._startX;
                const sy = this._startY;
                let dx = this._endX - sx;
                let dy = this._endY - sy;
                let length = 1 + Math.max(Math.abs(dx), Math.abs(dy));

                if (dx) {
                    dx = dx / Math.abs(dx);
                }
                if (dy) {
                    dy = dy / Math.abs(dy);
                }
                const nx = dy;
                const ny = -dx;

                let ok = true;
                for (let i = 0; i < length; i++) {
                    const x = sx + i * dx;
                    const y = sy + i * dy;

                    if (!canBeDugCallback(x, y)) {
                        ok = false;
                    }
                    if (!isWallCallback(x + nx, y + ny)) {
                        ok = false;
                    }
                    if (!isWallCallback(x - nx, y - ny)) {
                        ok = false;
                    }

                    if (!ok) {
                        length = i;
                        this._endX = x - dx;
                        this._endY = y - dy;
                        break;
                    }
                }

                /**
                 * If the length degenerated, this corridor might be invalid
                 */

                /* not supported */
                if (length === 0) {
                    return false;
                }

                /* length 1 allowed only if the next space is empty */
                if (length === 1 && isWallCallback(this._endX + dx, this._endY + dy)) {
                    return false;
                }

                /**
                 * We do not want the corridor to crash into a corner of a room;
                 * if any of the ending corners is empty, the N+1th cell of this corridor must be empty too.
                 * 
                 * Situation:
                 * #######1
                 * .......?
                 * #######2
                 * 
                 * The corridor was dug from left to right.
                 * 1, 2 - problematic corners, ? = N+1th cell (not dug)
                 */
                const firstCornerBad = !isWallCallback(this._endX + dx + nx, this._endY + dy + ny);
                const secondCornerBad = !isWallCallback(this._endX + dx - nx, this._endY + dy - ny);
                this._endsWithAWall = isWallCallback(this._endX + dx, this._endY + dy);
                if ((firstCornerBad || secondCornerBad) && this._endsWithAWall) {
                    return false;
                }

                return true;
            };

            create(digCallback) {
                const sx = this._startX;
                const sy = this._startY;
                let dx = this._endX - sx;
                let dy = this._endY - sy;
                const length = 1 + Math.max(Math.abs(dx), Math.abs(dy));

                if (dx) {
                    dx = dx / Math.abs(dx);
                }
                if (dy) {
                    dy = dy / Math.abs(dy);
                }
                //const nx = dy;
                //const ny = -dx;

                for (let i = 0; i < length; i++) {
                    const x = sx + i * dx;
                    const y = sy + i * dy;
                    digCallback(x, y, 0);
                }

                return true;
            };

            createPriorityWalls(priorityWallCallback) {
                if (!this._endsWithAWall) {
                    return;
                }

                const sx = this._startX;
                const sy = this._startY;

                let dx = this._endX - sx;
                let dy = this._endY - sy;
                if (dx) {
                    dx = dx / Math.abs(dx);
                }
                if (dy) {
                    dy = dy / Math.abs(dy);
                }
                const nx = dy;
                const ny = -dx;

                priorityWallCallback(this._endX + dx, this._endY + dy);
                priorityWallCallback(this._endX + nx, this._endY + ny);
                priorityWallCallback(this._endX - nx, this._endY - ny);
            };
        }

        type MapOption = {
            roomWidth?: number[];
            roomHeight?: number[];
            corridorLength?: number[];
            dugPercentage?: number;
            timeLimit?: number;
        };

        class Map {
            _dug: number;
            _map: any[];

            _options: MapOption;
            _width: number;
            _height: number;
            _rooms: any[];
            _corridors: any[];
            _features: { Room: number; Corridor: number; };
            _featureAttempts: number;
            _walls: {};

            constructor(width: number, height: number, option: MapOption) {
                this._width = width;
                this._height = height;
                this._rooms = []; /* list of all rooms */
                this._corridors = [];
                this._options = {
                    roomWidth: [3, 9], /* room minimum and maximum width */
                    roomHeight: [3, 5], /* room minimum and maximum height */
                    corridorLength: [3, 10], /* corridor minimum and maximum length */
                    dugPercentage: 0.2, /* we stop after this percentage of level area has been dug out */
                    timeLimit: 1000 /* we stop after this much time has passed (msec) */
                };
                Object.assign(this._options, option);

                this._features = {
                    Room: 4,
                    Corridor: 4
                };
                this._featureAttempts = 20; /* how many times do we try to create a feature on a suitable wall */
                this._walls = {}; /* these are available for digging */

                this._digCallback = this._digCallback.bind(this);
                this._canBeDugCallback = this._canBeDugCallback.bind(this);
                this._isWallCallback = this._isWallCallback.bind(this);
                this._priorityWallCallback = this._priorityWallCallback.bind(this);
            }
            create(callback) {
                this._rooms = [];
                this._corridors = [];
                this._map = this._fillMap(1);
                this._walls = {};
                this._dug = 0;
                const area = (this._width - 2) * (this._height - 2);

                this._firstRoom();

                const t1 = Date.now();

                do {
                    const t2 = Date.now();
                    if (t2 - t1 > this._options.timeLimit) {
                        break;
                    }

                    /* find a good wall */
                    const wall = this._findWall();
                    if (!wall) {
                        break;
                    } /* no more walls */

                    const parts = wall.split(",");
                    const x = parseInt(parts[0]);
                    const y = parseInt(parts[1]);
                    const dir = this._getDiggingDirection(x, y);
                    if (!dir) {
                        continue;
                    } /* this wall is not suitable */

                    //		consolere.log("wall", x, y);

                    /* try adding a feature */
                    let featureAttempts = 0;
                    do {
                        featureAttempts++;
                        if (this._tryFeature(x, y, dir[0], dir[1])) { /* feature added */
                            //if (this._rooms.length + this._corridors.length == 2) { this._rooms[0].addDoor(x, y); } /* first room oficially has doors */
                            this._removeSurroundingWalls(x, y);
                            this._removeSurroundingWalls(x - dir[0], y - dir[1]);
                            break;
                        }
                    } while (featureAttempts < this._featureAttempts);

                    var priorityWalls = 0;
                    for (let id in this._walls) {
                        if (this._walls[id] > 1) {
                            priorityWalls++;
                        }
                    }

                } while (this._dug / area < this._options.dugPercentage || priorityWalls
                ); /* fixme number of priority walls */

                this._addDoors();

                if (callback) {
                    for (let i = 0; i < this._width; i++) {
                        for (let j = 0; j < this._height; j++) {
                            callback(i, j, this._map[i][j]);
                        }
                    }
                }

                this._walls = {};
                this._map = null;

                return this;
            };

            _digCallback(x, y, value) {
                if (value == 0 || value == 2) { /* empty */
                    this._map[x][y] = 0;
                    this._dug++;
                } else { /* wall */
                    this._walls[x + "," + y] = 1;
                }
            };

            _isWallCallback(x, y) {
                if (x < 0 || y < 0 || x >= this._width || y >= this._height) {
                    return false;
                }
                return (this._map[x][y] == 1);
            };

            _canBeDugCallback(x, y) {
                if (x < 1 || y < 1 || x + 1 >= this._width || y + 1 >= this._height) {
                    return false;
                }
                return (this._map[x][y] == 1);
            };

            _priorityWallCallback(x, y) {
                this._walls[x + "," + y] = 2;
            };

            _findWall() {
                const prio1: string[] = [];
                const prio2: string[] = [];
                for (let id in this._walls) {
                    const prio = this._walls[id];
                    if (prio === 2) {
                        prio2.push(id);
                    } else {
                        prio1.push(id);
                    }
                }

                const arr = (prio2.length ? prio2 : prio1);
                if (!arr.length) {
                    return null;
                } /* no walls :/ */

                const id2 = arr.sort()[Math.floor(Math.random() * arr.length)]; // sort to make the order deterministic
                delete this._walls[id2];

                return id2;
            };

            _firstRoom() {
                const cx = Math.floor(this._width / 2);
                const cy = Math.floor(this._height / 2);
                const room = Room.createRandomCenter(cx, cy, this._options);
                this._rooms.push(room);
                room.create(this._digCallback);
            };

            _fillMap(value: number) {
                const map: number[][] = [];
                for (let i = 0; i < this._width; i++) {
                    map.push([]);
                    for (let j = 0; j < this._height; j++) {
                        map[i].push(value);
                    }
                }
                return map;
            };

            FeatureClass = { Room: Room, Corridor: Corridor };

            _tryFeature(x, y, dx, dy) {
                const featureType = getWeightedValue(this._features);
                const feature = this.FeatureClass[featureType].createRandomAt(x, y, dx, dy, this._options);

                if (!feature.isValid(this._isWallCallback, this._canBeDugCallback)) {
                    //		consolere.log("not valid");
                    //		feature.debug();
                    return false;
                }

                feature.create(this._digCallback);
                //	feature.debug();

                if (feature instanceof Room) {
                    this._rooms.push(feature);
                }
                if (feature instanceof Corridor) {
                    feature.createPriorityWalls(this._priorityWallCallback);
                    this._corridors.push(feature);
                }

                return true;
            };

            _removeSurroundingWalls(cx, cy) {
                const deltas = this._ROTDIRS4;

                for (let i = 0; i < deltas.length; i++) {
                    const delta = deltas[i];
                    var x = cx + delta[0];
                    var y = cy + delta[1];
                    delete this._walls[x + "," + y];
                    var x = cx + 2 * delta[0];
                    var y = cy + 2 * delta[1];
                    delete this._walls[x + "," + y];
                }
            };

            _ROTDIRS4 = [
                [0, -1],
                [1, 0],
                [0, 1],
                [-1, 0]
            ];

            _getDiggingDirection(cx, cy) {
                if (cx <= 0 || cy <= 0 || cx >= this._width - 1 || cy >= this._height - 1) {
                    return null;
                }

                let result = null;
                const deltas = this._ROTDIRS4;

                for (let i = 0; i < deltas.length; i++) {
                    const delta = deltas[i];
                    const x = cx + delta[0];
                    const y = cy + delta[1];

                    if (!this._map[x][y]) { /* there already is another empty neighbor! */
                        if (result) {
                            return null;
                        }
                        result = delta;
                    }
                }

                /* no empty neighbor */
                if (!result) {
                    return null;
                }

                return [-result[0], -result[1]];
            };

            _addDoors() {
                var data = this._map;
                const isWallCallback = (x, y) => {
                    return (data[x][y] == 1);
                };
                for (let i = 0; i < this._rooms.length; i++) {
                    const room = this._rooms[i];
                    room.clearDoors();
                    room.addDoors(isWallCallback);
                }
            };

            getRooms() {
                return this._rooms;
            };

            getCorridors() {
                return this._corridors;
            };
        }

        export function create(w: number, h: number, callback: (x: number, y: number, value: number) => void): Map {
            return new Map(w, h, {}).create(callback);
        }
    }

}
