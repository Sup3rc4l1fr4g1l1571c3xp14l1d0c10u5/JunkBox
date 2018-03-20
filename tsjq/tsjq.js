"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
class Array2D {
    constructor(width, height, fill) {
        this.arrayWidth = width;
        this.arrayHeight = height;
        if (fill === undefined) {
            this.matrixBuffer = new Array(width * height);
        }
        else {
            this.matrixBuffer = new Array(width * height).fill(fill);
        }
    }
    get width() {
        return this.arrayWidth;
    }
    get height() {
        return this.arrayHeight;
    }
    value(x, y, value) {
        if (0 > x || x >= this.arrayWidth || 0 > y || y >= this.arrayHeight) {
            return 0;
        }
        if (value !== undefined) {
            this.matrixBuffer[y * this.arrayWidth + x] = value;
        }
        return this.matrixBuffer[y * this.arrayWidth + x];
    }
    fill(value) {
        this.matrixBuffer.fill(value);
        return this;
    }
    dup() {
        const m = new Array2D(this.width, this.height);
        m.matrixBuffer = this.matrixBuffer.slice();
        return m;
    }
    static createFromArray(array, fill) {
        const h = array.length;
        const w = Math.max.apply(Math, array.map(x => x.length));
        const matrix = new Array2D(w, h, fill);
        array.forEach((vy, y) => vy.forEach((vx, x) => matrix.value(x, y, vx)));
        return matrix;
    }
    toString() {
        const lines = [];
        for (let y = 0; y < this.height; y++) {
            lines[y] = `|${this.matrixBuffer.slice((y + 0) * this.arrayWidth, (y + 1) * this.arrayWidth).join(", ")}|`;
        }
        return lines.join("\r\n");
    }
}
Array2D.DIR8 = [
    { x: +Number.MAX_SAFE_INTEGER, y: +Number.MAX_SAFE_INTEGER },
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
class XorShift {
    constructor(w = 0 | Date.now(), x, y, z) {
        if (x === undefined) {
            x = (0 | (w << 13));
        }
        if (y === undefined) {
            y = (0 | ((w >>> 9) ^ (x << 6)));
        }
        if (z === undefined) {
            z = (0 | (y >>> 7));
        }
        this.seeds = { x: x >>> 0, y: y >>> 0, z: z >>> 0, w: w >>> 0 };
        // Object.defineProperty(this, "seeds", { writable: false });
        this.randCount = 0;
        this.generator = this.randGen(w, x, y, z);
    }
    *randGen(w, x, y, z) {
        let t;
        for (;;) {
            t = x ^ (x << 11);
            x = y;
            y = z;
            z = w;
            yield w = ((w ^ (w >>> 19)) ^ (t ^ (t >>> 8))) >>> 0;
        }
    }
    rand() {
        this.randCount = 0 | this.randCount + 1;
        return this.generator.next().value;
    }
    randInt(min = 0, max = 0x7FFFFFFF) {
        return 0 | this.rand() % (max + 1 - min) + min;
    }
    randFloat(min = 0, max = 1) {
        return Math.fround(this.rand() % 0xFFFF / 0xFFFF) * (max - min) + min;
    }
    shuffle(target) {
        const arr = target.concat();
        for (let i = 0; i <= arr.length - 2; i = 0 | i + 1) {
            const r = this.randInt(i, arr.length - 1);
            const tmp = arr[i];
            arr[i] = arr[r];
            arr[r] = tmp;
        }
        return arr;
    }
    getWeightedValue(data) {
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
    static default() {
        return new XorShift(XorShift.defaults.w, XorShift.defaults.x, XorShift.defaults.y, XorShift.defaults.z);
    }
}
XorShift.defaults = {
    x: 123456789,
    y: 362436069,
    z: 521288629,
    w: 88675123
};
var Dungeon;
(function (Dungeon) {
    const rand = XorShift.default();
    // マップ描画時の視点・視野情報
    class Camera {
    }
    Dungeon.Camera = Camera;
    // ダンジョンデータ
    class DungeonData {
        constructor(config) {
            this.width = config.width;
            this.height = config.height;
            this.gridsize = config.gridsize;
            this.layer = config.layer;
            this.camera = new Camera();
            this.lighting = new Array2D(this.width, this.height, 0);
            this.visibled = new Array2D(this.width, this.height, 0);
        }
        clearLighting() {
            this.lighting.fill(0);
            return this;
        }
        // update camera
        update(param) {
            const mapWidth = this.width * this.gridsize.width;
            const mapHeight = this.height * this.gridsize.height;
            // マップ上でのカメラの注視点
            const mapPx = param.viewpoint.x;
            const mapPy = param.viewpoint.y;
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
            }
            else if ((this.camera.right >= mapWidth) && (this.camera.left - (this.camera.right - mapWidth) >= 0)) {
                this.camera.left -= (this.camera.right - mapWidth);
                this.camera.right = mapWidth - 1;
            }
            if ((this.camera.top < 0) && (this.camera.bottom - this.camera.top < mapHeight)) {
                this.camera.bottom -= this.camera.top;
                this.camera.top = 0;
            }
            else if ((this.camera.bottom >= mapHeight) && (this.camera.top - (this.camera.bottom - mapHeight) >= 0)) {
                this.camera.top -= (this.camera.bottom - mapHeight);
                this.camera.bottom = mapHeight - 1;
            }
            // 視野の左上位置を原点とした注視点を算出
            this.camera.localPx = mapPx - this.camera.left;
            this.camera.localPy = mapPy - this.camera.top;
            // 視野の四隅位置に対応するマップチップ座標を算出
            this.camera.chipLeft = ~~(this.camera.left / this.gridsize.width);
            this.camera.chipTop = ~~(this.camera.top / this.gridsize.height);
            this.camera.chipRight = ~~((this.camera.right + (this.gridsize.width - 1)) / this.gridsize.width);
            this.camera.chipBottom = ~~((this.camera.bottom + (this.gridsize.height - 1)) / this.gridsize.height);
            // 視野の左上位置をにマップチップをおいた場合のスクロールによるズレ量を算出
            this.camera.chipOffX = -(this.camera.left % this.gridsize.width);
            this.camera.chipOffY = -(this.camera.top % this.gridsize.height);
        }
        draw(layerDrawHook) {
            // 描画開始
            const gridw = this.gridsize.width;
            const gridh = this.gridsize.height;
            Object.keys(this.layer).forEach((key) => {
                const l = ~~key;
                for (let y = this.camera.chipTop; y <= this.camera.chipBottom; y++) {
                    for (let x = this.camera.chipLeft; x <= this.camera.chipRight; x++) {
                        const chipid = this.layer[l].chips.value(x, y) || 0;
                        if (this.layer[l].chip[chipid]) {
                            const xx = (x - this.camera.chipLeft) * gridw;
                            const yy = (y - this.camera.chipTop) * gridh;
                            Game.getScreen().drawImage(Game.getScreen().texture(this.layer[l].texture), this.layer[l].chip[chipid].x, this.layer[l].chip[chipid].y, gridw, gridh, 0 + xx + this.camera.chipOffX, 0 + yy + this.camera.chipOffY, gridw, gridh);
                        }
                    }
                }
                // レイヤー描画フック
                layerDrawHook(l, this.camera.localPx, this.camera.localPy);
            });
            // 明度描画
            for (let y = this.camera.chipTop; y <= this.camera.chipBottom; y++) {
                for (let x = this.camera.chipLeft; x <= this.camera.chipRight; x++) {
                    let light = this.lighting.value(x, y) / 100;
                    if (light > 1) {
                        light = 1;
                    }
                    else if (light < 0) {
                        light = 0;
                    }
                    const xx = (x - this.camera.chipLeft) * gridw;
                    const yy = (y - this.camera.chipTop) * gridh;
                    Game.getScreen().fillStyle = `rgba(0,0,0,${1 - light})`;
                    Game.getScreen().fillRect(0 + xx + this.camera.chipOffX, 0 + yy + this.camera.chipOffY, gridw, gridh);
                }
            }
        }
    }
    Dungeon.DungeonData = DungeonData;
    // ダンジョン構成要素基底クラス
    class Feature {
    }
    // 部屋
    class Room extends Feature {
        constructor(left, top, right, bottom, door) {
            super();
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.doors = new Map();
            if (door !== undefined) {
                this.addDoor(door.x, door.y);
            }
        }
        static createRandomAt(x, y, dx, dy, options) {
            const minw = options.roomWidth.min;
            const maxw = options.roomWidth.max;
            const width = options.random.randInt(minw, maxw);
            const minh = options.roomHeight.min;
            const maxh = options.roomHeight.max;
            const height = options.random.randInt(minh, maxh);
            if (dx === 1) {
                const y2 = y - options.random.randInt(0, height - 1);
                return new Room(x + 1, y2, x + width, y2 + height - 1, { x: x, y: y });
            }
            if (dx === -1) {
                const y2 = y - options.random.randInt(0, height - 1);
                return new Room(x - width, y2, x - 1, y2 + height - 1, { x: x, y: y });
            }
            if (dy === 1) {
                const x2 = x - options.random.randInt(0, width - 1);
                return new Room(x2, y + 1, x2 + width - 1, y + height, { x: x, y: y });
            }
            if (dy === -1) {
                const x2 = x - options.random.randInt(0, width - 1);
                return new Room(x2, y - height, x2 + width - 1, y - 1, { x: x, y: y });
            }
            throw new Error("dx or dy must be 1 or -1");
        }
        static createRandomCenter(cx, cy, options) {
            const minw = options.roomWidth.min;
            const maxw = options.roomWidth.max;
            const width = options.random.randInt(minw, maxw);
            const minh = options.roomHeight.min;
            const maxh = options.roomHeight.max;
            const height = options.random.randInt(minh, maxh);
            const x1 = cx - options.random.randInt(0, width - 1);
            const y1 = cy - options.random.randInt(0, height - 1);
            const x2 = x1 + width - 1;
            const y2 = y1 + height - 1;
            return new Room(x1, y1, x2, y2);
        }
        static createRandom(availWidth, availHeight, options) {
            const minw = options.roomWidth.min;
            const maxw = options.roomWidth.max;
            const width = options.random.randInt(minw, maxw);
            const minh = options.roomHeight.min;
            const maxh = options.roomHeight.max;
            const height = options.random.randInt(minh, maxh);
            const left = availWidth - width - 1;
            const top = availHeight - height - 1;
            const x1 = 1 + options.random.randInt(0, left - 1);
            const y1 = 1 + options.random.randInt(0, top - 1);
            const x2 = x1 + width - 1;
            const y2 = y1 + height - 1;
            return new Room(x1, y1, x2, y2);
        }
        addDoor(x, y) {
            this.doors.set(x + "," + y, 1);
            return this;
        }
        getDoors(callback) {
            for (const key of Object.keys(this.doors)) {
                const parts = key.split(",");
                callback({ x: parseInt(parts[0], 10), y: parseInt(parts[1], 10) });
            }
            return this;
        }
        clearDoors() {
            this.doors.clear();
            return this;
        }
        addDoors(isWallCallback) {
            const left = this.left - 1;
            const right = this.right + 1;
            const top = this.top - 1;
            const bottom = this.bottom + 1;
            for (let x = left; x <= right; x++) {
                for (let y = top; y <= bottom; y++) {
                    if (x !== left && x !== right && y !== top && y !== bottom) {
                        continue;
                    }
                    if (isWallCallback(x, y)) {
                        continue;
                    }
                    this.addDoor(x, y);
                }
            }
            return this;
        }
        debug() {
            console.log("room", this.left, this.top, this.right, this.bottom);
        }
        isValid(isWallCallback, canBeDugCallback) {
            const left = this.left - 1;
            const right = this.right + 1;
            const top = this.top - 1;
            const bottom = this.bottom + 1;
            for (let x = left; x <= right; x++) {
                for (let y = top; y <= bottom; y++) {
                    if (x === left || x === right || y === top || y === bottom) {
                        if (!isWallCallback(x, y)) {
                            return false;
                        }
                    }
                    else {
                        if (!canBeDugCallback(x, y)) {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        /**
         * @param {function} digCallback Dig callback with a signature (x, y, value). Values: 0 = empty, 1 = wall, 2 = door. Multiple doors are allowed.
         */
        create(digCallback) {
            const left = this.left - 1;
            const right = this.right + 1;
            const top = this.top - 1;
            const bottom = this.bottom + 1;
            for (let x = left; x <= right; x++) {
                for (let y = top; y <= bottom; y++) {
                    let value;
                    if (this.doors.has(x + "," + y)) {
                        value = 2;
                    }
                    else if (x === left || x === right || y === top || y === bottom) {
                        value = 1;
                    }
                    else {
                        value = 0;
                    }
                    digCallback(x, y, value);
                }
            }
        }
        getCenter() {
            return { x: Math.round((this.left + this.right) / 2), y: Math.round((this.top + this.bottom) / 2) };
        }
        getLeft() {
            return this.left;
        }
        getRight() {
            return this.right;
        }
        getTop() {
            return this.top;
        }
        getBottom() {
            return this.bottom;
        }
    }
    // 通路
    class Corridor extends Feature {
        constructor(startX, startY, endX, endY) {
            super();
            this.startX = startX;
            this.startY = startY;
            this.endX = endX;
            this.endY = endY;
            this.endsWithAWall = true;
        }
        static createRandomAt(x, y, dx, dy, options) {
            const min = options.corridorLength.min;
            const max = options.corridorLength.max;
            const length = options.random.randInt(min, max);
            return new Corridor(x, y, x + dx * length, y + dy * length);
        }
        debug() {
            console.log("corridor", this.startX, this.startY, this.endX, this.endY);
        }
        isValid(isWallCallback, canBeDugCallback) {
            const sx = this.startX;
            const sy = this.startY;
            let dx = this.endX - sx;
            let dy = this.endY - sy;
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
                    this.endX = x - dx;
                    this.endY = y - dy;
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
            if (length === 1 && isWallCallback(this.endX + dx, this.endY + dy)) {
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
            const firstCornerBad = !isWallCallback(this.endX + dx + nx, this.endY + dy + ny);
            const secondCornerBad = !isWallCallback(this.endX + dx - nx, this.endY + dy - ny);
            this.endsWithAWall = isWallCallback(this.endX + dx, this.endY + dy);
            if ((firstCornerBad || secondCornerBad) && this.endsWithAWall) {
                return false;
            }
            return true;
        }
        create(digCallback) {
            const sx = this.startX;
            const sy = this.startY;
            let dx = this.endX - sx;
            let dy = this.endY - sy;
            const length = 1 + Math.max(Math.abs(dx), Math.abs(dy));
            if (dx) {
                dx = dx / Math.abs(dx);
            }
            if (dy) {
                dy = dy / Math.abs(dy);
            }
            for (let i = 0; i < length; i++) {
                const x = sx + i * dx;
                const y = sy + i * dy;
                digCallback(x, y, 0);
            }
            return true;
        }
        createPriorityWalls(priorityWallCallback) {
            if (!this.endsWithAWall) {
                return;
            }
            const sx = this.startX;
            const sy = this.startY;
            let dx = this.endX - sx;
            let dy = this.endY - sy;
            if (dx) {
                dx = dx / Math.abs(dx);
            }
            if (dy) {
                dy = dy / Math.abs(dy);
            }
            const nx = dy;
            const ny = -dx;
            priorityWallCallback(this.endX + dx, this.endY + dy);
            priorityWallCallback(this.endX + nx, this.endY + ny);
            priorityWallCallback(this.endX - nx, this.endY - ny);
        }
    }
    class Generator {
        constructor(width, height, { random = new XorShift(), roomWidth = { min: 3, max: 9 }, /* room minimum and maximum width */ roomHeight = { min: 3, max: 5 }, /* room minimum and maximum height */ corridorLength = { min: 3, max: 10 }, /* corridor minimum and maximum length */ dugPercentage = 0.2, /* we stop after this percentage of level area has been dug out */ loopLimit = 100000, }) {
            this.width = width;
            this.height = height;
            this.rooms = []; /* list of all rooms */
            this.corridors = [];
            this.options = {
                random: random,
                roomWidth: roomWidth,
                roomHeight: roomHeight,
                corridorLength: corridorLength,
                dugPercentage: dugPercentage,
                loopLimit: loopLimit,
            };
            this.features = {
                Room: 4,
                Corridor: 4,
            };
            this.featureAttempts = 20; /* how many times do we try to create a feature on a suitable wall */
            this.walls = new Map(); /* these are available for digging */
            this.digCallback = this.digCallback.bind(this);
            this.canBeDugCallback = this.canBeDugCallback.bind(this);
            this.isWallCallback = this.isWallCallback.bind(this);
            this.priorityWallCallback = this.priorityWallCallback.bind(this);
        }
        create(callback) {
            this.rooms = [];
            this.corridors = [];
            this.map = this.fillMap(1);
            this.walls.clear();
            this.dug = 0;
            const area = (this.width - 2) * (this.height - 2);
            this.firstRoom();
            let t1 = 0;
            let priorityWalls = 0;
            do {
                if (t1++ > this.options.loopLimit) {
                    break;
                }
                /* find a good wall */
                const wall = this.findWall();
                if (!wall) {
                    break;
                } /* no more walls */
                const parts = wall.split(",");
                const x = parseInt(parts[0]);
                const y = parseInt(parts[1]);
                const dir = this.getDiggingDirection(x, y);
                if (!dir) {
                    continue;
                } /* this wall is not suitable */
                // consolere.log("wall", x, y);
                /* try adding a feature */
                let featureAttempts = 0;
                do {
                    featureAttempts++;
                    if (this.tryFeature(x, y, dir.x, dir.y)) {
                        // if (this._rooms.length + this._corridors.length === 2) { this._rooms[0].addDoor(x, y); } /* first room oficially has doors */
                        this.removeSurroundingWalls(x, y);
                        this.removeSurroundingWalls(x - dir.x, y - dir.y);
                        break;
                    }
                } while (featureAttempts < this.featureAttempts);
                priorityWalls = 0;
                for (const [, value] of this.walls) {
                    if (value > 1) {
                        priorityWalls++;
                    }
                }
            } while ((this.dug / area) < this.options.dugPercentage || priorityWalls); /* fixme number of priority walls */
            this.addDoors();
            if (callback) {
                for (let i = 0; i < this.width; i++) {
                    for (let j = 0; j < this.height; j++) {
                        callback(i, j, this.map[i][j]);
                    }
                }
            }
            this.walls.clear();
            this.map = null;
            this.rooms = this.options.random.shuffle(this.rooms);
            return this;
        }
        digCallback(x, y, value) {
            if (value === 0 || value === 2) {
                this.map[x][y] = 0;
                this.dug++;
            }
            else {
                this.walls.set(x + "," + y, 1);
            }
        }
        isWallCallback(x, y) {
            if (x < 0 || y < 0 || x >= this.width || y >= this.height) {
                return false;
            }
            return (this.map[x][y] === 1);
        }
        canBeDugCallback(x, y) {
            if (x < 1 || y < 1 || x + 1 >= this.width || y + 1 >= this.height) {
                return false;
            }
            return (this.map[x][y] === 1);
        }
        priorityWallCallback(x, y) {
            this.walls.set(x + "," + y, 2);
        }
        findWall() {
            const prio1 = [];
            const prio2 = [];
            for (const [id, prio] of this.walls) {
                if (prio === 2) {
                    prio2.push(id);
                }
                else {
                    prio1.push(id);
                }
            }
            const arr = (prio2.length ? prio2 : prio1);
            if (!arr.length) {
                return null;
            } /* no walls :/ */
            const id2 = arr.sort()[this.options.random.randInt(0, arr.length - 1)]; // sort to make the order deterministic
            this.walls.delete(id2);
            return id2;
        }
        firstRoom() {
            const cx = Math.floor(this.width / 2);
            const cy = Math.floor(this.height / 2);
            const room = Room.createRandomCenter(cx, cy, this.options);
            this.rooms.push(room);
            room.create(this.digCallback);
        }
        fillMap(value) {
            const map = [];
            for (let i = 0; i < this.width; i++) {
                map.push([]);
                for (let j = 0; j < this.height; j++) {
                    map[i].push(value);
                }
            }
            return map;
        }
        tryFeature(x, y, dx, dy) {
            const featureType = this.options.random.getWeightedValue(this.features);
            const feature = Generator.featureCreateMethodTable[featureType](x, y, dx, dy, this.options);
            if (!feature.isValid(this.isWallCallback, this.canBeDugCallback)) {
                return false;
            }
            feature.create(this.digCallback);
            if (feature instanceof Room) {
                this.rooms.push(feature);
            }
            if (feature instanceof Corridor) {
                feature.createPriorityWalls(this.priorityWallCallback);
                this.corridors.push(feature);
            }
            return true;
        }
        removeSurroundingWalls(cx, cy) {
            const deltas = Generator.rotdirs4;
            for (const delta of deltas) {
                const x1 = cx + delta.x;
                const y1 = cy + delta.y;
                this.walls.delete(x1 + "," + y1);
                const x2 = cx + 2 * delta.x;
                const y2 = cy + 2 * delta.y;
                this.walls.delete(x2 + "," + y2);
            }
        }
        getDiggingDirection(cx, cy) {
            if (cx <= 0 || cy <= 0 || cx >= this.width - 1 || cy >= this.height - 1) {
                return null;
            }
            let result = null;
            const deltas = Generator.rotdirs4;
            for (const delta of deltas) {
                const x = cx + delta.x;
                const y = cy + delta.y;
                if (!this.map[x][y]) {
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
            return { x: -result.x, y: -result.y };
        }
        addDoors() {
            const data = this.map;
            const isWallCallback = (x, y) => {
                return (data[x][y] === 1);
            };
            for (const room of this.rooms) {
                room.clearDoors();
                room.addDoors(isWallCallback);
            }
        }
        getRooms() {
            return this.rooms;
        }
        getCorridors() {
            return this.corridors;
        }
    }
    Generator.featureCreateMethodTable = { "Room": Room.createRandomAt, "Corridor": Corridor.createRandomAt };
    Generator.rotdirs4 = [
        { x: 0, y: -1 },
        { x: 1, y: 0 },
        { x: 0, y: 1 },
        { x: -1, y: 0 }
    ];
    function generate(w, h, callback) {
        return new Generator(w, h, { random: rand }).create(callback);
    }
    Dungeon.generate = generate;
})(Dungeon || (Dungeon = {}));
function ajax(uri, type) {
    return new Promise((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        xhr.responseType = type;
        xhr.open("GET", uri, true);
        xhr.onerror = (ev) => {
            reject(ev);
        };
        xhr.onload = () => {
            resolve(xhr);
        };
        xhr.send();
    });
}
var Game;
(function (Game) {
    class ConsoleView {
        constructor() {
            const log = console.log.bind(console);
            const error = console.error.bind(console);
            const warn = console.warn.bind(console);
            const table = console.table ? console.table.bind(console) : null;
            const toString = (x) => (x instanceof Error) ? x.message : (typeof x === 'string' ? x : JSON.stringify(x));
            const outer = document.createElement('div');
            outer.id = 'console';
            const div = document.createElement('div');
            outer.appendChild(div);
            const printToDiv = (stackTraceObject, ...args) => {
                const msg = Array.prototype.slice.call(args, 0)
                    .map(toString)
                    .join(' ');
                const text = div.textContent;
                const trace = stackTraceObject ? stackTraceObject.stack.split(/\n/)[1] : "";
                div.textContent = text + trace + ": " + msg + '\n';
                while (div.clientHeight > document.body.clientHeight) {
                    const lines = div.textContent.split(/\n/);
                    lines.shift();
                    div.textContent = lines.join('\n');
                }
            };
            console.log = (...args) => {
                log.apply(null, args);
                const dupargs = Array.prototype.slice.call(args, 0);
                dupargs.unshift(new Error());
                printToDiv.apply(null, dupargs);
            };
            console.error = (...args) => {
                error.apply(null, args);
                const dupargs = Array.prototype.slice.call(args, 0);
                dupargs.unshift('ERROR:');
                dupargs.unshift(new Error());
                printToDiv.apply(null, dupargs);
            };
            console.warn = (...args) => {
                warn.apply(null, args);
                const dupargs = Array.prototype.slice.call(args, 0);
                dupargs.unshift('WARNING:');
                dupargs.unshift(new Error());
                printToDiv.apply(null, dupargs);
            };
            console.table = (...args) => {
                if (typeof table === 'function') {
                    table.apply(null, args);
                }
                const objArr = args[0];
                const keys = (typeof objArr[0] !== 'undefined') ? Object.keys(objArr[0]) : [];
                const numCols = keys.length;
                const len = objArr.length;
                const $table = document.createElement('table');
                const $head = document.createElement('thead');
                let $tdata = document.createElement('td');
                $tdata.innerHTML = 'Index';
                $head.appendChild($tdata);
                for (let k = 0; k < numCols; k++) {
                    $tdata = document.createElement('td');
                    $tdata.innerHTML = keys[k];
                    $head.appendChild($tdata);
                }
                $table.appendChild($head);
                for (let i = 0; i < len; i++) {
                    const $line = document.createElement('tr');
                    $tdata = document.createElement('td');
                    $tdata.innerHTML = "" + i;
                    $line.appendChild($tdata);
                    for (let j = 0; j < numCols; j++) {
                        $tdata = document.createElement('td');
                        $tdata.innerHTML = objArr[i][keys[j]];
                        $line.appendChild($tdata);
                    }
                    $table.appendChild($line);
                }
                div.appendChild($table);
            };
            window.addEventListener('error', (err) => {
                printToDiv(null, 'EXCEPTION:', err.message + '\n  ' + err.filename, err.lineno + ':' + err.colno);
            });
            document.body.appendChild(outer);
        }
        static install() {
            if (!this.instance) {
                this.instance = new ConsoleView();
                return true;
            }
            else {
                return false;
            }
        }
    }
    Game.ConsoleView = ConsoleView;
})(Game || (Game = {}));
var Dispatcher;
(function (Dispatcher) {
    class SingleDispatcher {
        constructor() {
            this.listeners = [];
        }
        clear() {
            this.listeners.length = 0;
            return this;
        }
        on(listener) {
            this.listeners.push(listener);
            return this;
        }
        off(listener) {
            const index = this.listeners.indexOf(listener);
            if (index !== -1) {
                this.listeners.splice(index, 1);
            }
            return this;
        }
        fire(...args) {
            const temp = this.listeners.slice();
            temp.forEach((dispatcher) => dispatcher.apply(this, args));
            return this;
        }
        one(listener) {
            var func = (...args) => {
                var result = listener.apply(this, args);
                this.off(func);
                return result;
            };
            this.on(func);
            return this;
        }
    }
    Dispatcher.SingleDispatcher = SingleDispatcher;
    class EventDispatcher {
        constructor() {
            this.listeners = new Map();
        }
        on(eventName, listener) {
            if (!this.listeners.has(eventName)) {
                this.listeners.set(eventName, new SingleDispatcher());
            }
            this.listeners.get(eventName).on(listener);
            return this;
        }
        off(eventName, listener) {
            this.listeners.get(eventName).off(listener);
            return this;
        }
        fire(eventName, ...args) {
            if (this.listeners.has(eventName)) {
                const dispatcher = this.listeners.get(eventName);
                dispatcher.fire.apply(dispatcher, args);
            }
            return this;
        }
        one(eventName, listener) {
            if (!this.listeners.has(eventName)) {
                this.listeners.set(eventName, new SingleDispatcher());
            }
            this.listeners.get(eventName).one(listener);
            return this;
        }
        hasEventListener(eventName) {
            return this.listeners.has(eventName);
        }
        clearEventListener(eventName) {
            if (this.listeners.has(eventName)) {
                this.listeners.get(eventName).clear();
            }
            return this;
        }
    }
    Dispatcher.EventDispatcher = EventDispatcher;
})(Dispatcher || (Dispatcher = {}));
var Game;
(function (Game) {
    let video = null;
    let sceneManager = null;
    let inputDispacher = null;
    let timer = null;
    let soundManager = null;
    function create(config) {
        return new Promise((resolve, reject) => {
            try {
                Game.ConsoleView.install();
                document.title = config.title;
                video = new Game.Video(config.video);
                video.imageSmoothingEnabled = false;
                sceneManager = new Game.Scene.SceneManager();
                timer = new Game.Timer.AnimationTimer();
                inputDispacher = new Game.Input.InputManager();
                soundManager = new Game.Sound.SoundManager();
                resolve();
            }
            catch (e) {
                reject(e);
            }
        });
    }
    Game.create = create;
    function getScreen() {
        return video;
    }
    Game.getScreen = getScreen;
    function getTimer() {
        return timer;
    }
    Game.getTimer = getTimer;
    function getSceneManager() {
        return sceneManager;
    }
    Game.getSceneManager = getSceneManager;
    function getInput() {
        return inputDispacher;
    }
    Game.getInput = getInput;
    function getSound() {
        return soundManager;
    }
    Game.getSound = getSound;
})(Game || (Game = {}));
/// <reference path="eventdispatcher.ts" />
var Game;
(function (Game) {
    var GUI;
    (function (GUI) {
        function isHit(ui, x, y) {
            const dx = x - ui.left;
            const dy = y - ui.top;
            return (0 <= dx && dx < ui.width) && (0 <= dy && dy < ui.height);
        }
        GUI.isHit = isHit;
        class UIDispatcher extends Dispatcher.EventDispatcher {
            constructor() {
                super();
                this.uiTable = new Map();
            }
            add(ui) {
                if (this.uiTable.has(ui)) {
                    return;
                }
                this.uiTable.set(ui, new Map());
                ui.regist(this);
            }
            remove(ui) {
                if (!this.uiTable.has(ui)) {
                    return;
                }
                ui.unregist(this);
                const eventTable = this.uiTable.get(ui);
                this.uiTable.set(ui, null);
                eventTable.forEach((values, key) => {
                    values.forEach((value) => this.off(key, value));
                });
                this.uiTable.delete(ui);
            }
            registUiEvent(ui, event, handler) {
                if (!this.uiTable.has(ui)) {
                    return;
                }
                const eventTable = this.uiTable.get(ui);
                if (!eventTable.has(event)) {
                    eventTable.set(event, []);
                }
                const events = eventTable.get(event);
                events.push(handler);
                this.on(event, handler);
            }
            unregistUiEvent(ui, event, handler) {
                if (!this.uiTable.has(ui)) {
                    return;
                }
                const eventTable = this.uiTable.get(ui);
                if (!eventTable.has(event)) {
                    return;
                }
                const events = eventTable.get(event);
                const index = events.indexOf(handler);
                if (index != -1) {
                    events.splice(index, 1);
                }
                this.off(event, handler);
            }
            draw() {
                this.uiTable.forEach((value, key) => {
                    if (key.visible) {
                        key.draw();
                    }
                });
            }
            // UIに対するクリック/タップ操作を捕捉
            onClick(ui, handler) {
                const hookHandler = (x, y) => {
                    if (!ui.visible || !ui.enable) {
                        return;
                    }
                    if (!Game.getScreen().pagePointContainScreen(x, y)) {
                        return;
                    }
                    const [cx, cy] = Game.getScreen().pagePointToScreenPoint(x, y);
                    if (!isHit(ui, cx, cy)) {
                        return;
                    }
                    let dx = 0;
                    let dy = 0;
                    const onPointerMoveHandler = (x, y) => {
                        const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                        dx += Math.abs(_x - cx);
                        dy += Math.abs(_y - cy);
                    };
                    const onPointerUpHandler = (x, y) => {
                        this.off("pointermove", onPointerMoveHandler);
                        this.off("pointerup", onPointerUpHandler);
                        if (dx + dy < 5) {
                            const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                            handler(_x - ui.left, _y - ui.top);
                        }
                    };
                    this.on("pointermove", onPointerMoveHandler);
                    this.on("pointerup", onPointerUpHandler);
                };
                this.registUiEvent(ui, "pointerdown", hookHandler);
                return () => this.unregistUiEvent(ui, "pointerdown", hookHandler);
            }
            //UI外のタップ/クリック操作を捕捉
            onNcClick(ui, handler) {
                const hookHandler = (x, y) => {
                    if (!ui.visible || !ui.enable) {
                        return;
                    }
                    if (!Game.getScreen().pagePointContainScreen(x, y)) {
                        return;
                    }
                    const [cx, cy] = Game.getScreen().pagePointToScreenPoint(x, y);
                    if (isHit(ui, cx, cy)) {
                        return;
                    }
                    let dx = 0;
                    let dy = 0;
                    const onPointerMoveHandler = (x, y) => {
                        const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                        dx += Math.abs(_x - cx);
                        dy += Math.abs(_y - cy);
                    };
                    const onPointerUpHandler = (x, y) => {
                        this.off("pointermove", onPointerMoveHandler);
                        this.off("pointerup", onPointerUpHandler);
                        if (dx + dy < 5) {
                            const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                            handler(_x - ui.left, _y - ui.top);
                        }
                    };
                    this.on("pointermove", onPointerMoveHandler);
                    this.on("pointerup", onPointerUpHandler);
                };
                this.registUiEvent(ui, "pointerdown", hookHandler);
                return () => this.unregistUiEvent(ui, "pointerdown", hookHandler);
            }
            // UIに対するスワイプ操作を捕捉
            onSwipe(ui, handler) {
                const hookHandler = (x, y) => {
                    if (!ui.visible || !ui.enable) {
                        return;
                    }
                    if (!Game.getScreen().pagePointContainScreen(x, y)) {
                        return;
                    }
                    let [cx, cy] = Game.getScreen().pagePointToScreenPoint(x, y);
                    if (!isHit(ui, cx, cy)) {
                        return;
                    }
                    const onPointerMoveHandler = (x, y) => {
                        const [_x, _y] = Game.getScreen().pagePointToScreenPoint(x, y);
                        let dx = (~~_x - ~~cx);
                        let dy = (~~_y - ~~cy);
                        cx = _x;
                        cy = _y;
                        handler(dx, dy, _x - ui.left, _y - ui.top);
                    };
                    const onPointerUpHandler = (x, y) => {
                        this.off("pointermove", onPointerMoveHandler);
                        this.off("pointerup", onPointerUpHandler);
                    };
                    this.on("pointermove", onPointerMoveHandler);
                    this.on("pointerup", onPointerUpHandler);
                    handler(0, 0, cx - ui.left, cy - ui.top);
                };
                this.registUiEvent(ui, "pointerdown", hookHandler);
                return () => this.unregistUiEvent(ui, "pointerdown", hookHandler);
            }
        }
        GUI.UIDispatcher = UIDispatcher;
        class TextBox {
            constructor({ left = 0, top = 0, width = 0, height = 0, text = "", edgeColor = `rgb(128,128,128)`, color = `rgb(255,255,255)`, font = undefined, fontColor = `rgb(0,0,0)`, textAlign = "left", textBaseline = "top", visible = true, enable = true }) {
                this.left = left;
                this.top = top;
                this.width = width;
                this.height = height;
                this.text = text;
                this.edgeColor = edgeColor;
                this.color = color;
                this.font = font;
                this.fontColor = fontColor;
                this.textAlign = textAlign;
                this.textBaseline = textBaseline;
                this.visible = visible;
                this.enable = enable;
            }
            draw() {
                const a = this.left + 8;
                const b = this.left + this.width - 8;
                const c = this.left;
                const d = this.left + this.width;
                const e = this.top;
                const f = this.top + this.height;
                Game.getScreen().beginPath();
                Game.getScreen().moveTo(a - 0.5, e - 0.5);
                Game.getScreen().bezierCurveTo(c - 0.5, e - 0.5, c - 0.5, f - 0.5, a - 0.5, f - 0.5);
                Game.getScreen().lineTo(b - 0.5, f - 0.5);
                Game.getScreen().bezierCurveTo(d - 0.5, f - 0.5, d - 0.5, e - 0.5, b - 0.5, e - 0.5);
                Game.getScreen().lineTo(a - 0.5, e - 0.5);
                Game.getScreen().closePath();
                Game.getScreen().fillStyle = this.color;
                Game.getScreen().fill();
                Game.getScreen().strokeStyle = this.edgeColor;
                Game.getScreen().lineWidth = 1;
                Game.getScreen().stroke();
                Game.getScreen().font = this.font;
                Game.getScreen().fillStyle = this.fontColor;
                const metrics = Game.getScreen().measureText(this.text);
                Game.getScreen().textAlign = this.textAlign;
                Game.getScreen().textBaseline = this.textBaseline;
                Game.getScreen().fillTextBox(this.text, a, e, this.width, this.height);
            }
            regist(dispatcher) { }
            unregist(dispatcher) { }
        }
        GUI.TextBox = TextBox;
        class Button {
            constructor({ left = 0, top = 0, width = 0, height = 0, text = "", edgeColor = Button.defaultValue.edgeColor, color = Button.defaultValue.color, font = Button.defaultValue.font, fontColor = Button.defaultValue.fontColor, textAlign = Button.defaultValue.textAlign, textBaseline = Button.defaultValue.textBaseline, visible = Button.defaultValue.visible, enable = Button.defaultValue.enable, disableEdgeColor = Button.defaultValue.disableEdgeColor, disableColor = Button.defaultValue.disableColor, disableFontColor = Button.defaultValue.disableFontColor, }) {
                this.left = left;
                this.top = top;
                this.width = width;
                this.height = height;
                this.text = text;
                this.edgeColor = edgeColor;
                this.color = color;
                this.font = font;
                this.fontColor = fontColor;
                this.textAlign = textAlign;
                this.textBaseline = textBaseline;
                this.visible = visible;
                this.enable = enable;
                this.click = () => { };
                this.disableEdgeColor = disableEdgeColor;
                this.disableColor = disableColor;
                this.disableFontColor = disableFontColor;
            }
            draw() {
                Game.getScreen().fillStyle = this.enable ? this.color : this.disableColor;
                Game.getScreen().fillRect(this.left - 0.5, this.top - 0.5, this.width, this.height);
                Game.getScreen().strokeStyle = this.enable ? this.edgeColor : this.disableEdgeColor;
                Game.getScreen().lineWidth = 1;
                Game.getScreen().strokeRect(this.left - 0.5, this.top - 0.5, this.width, this.height);
                Game.getScreen().font = this.font;
                Game.getScreen().fillStyle = this.enable ? this.fontColor : this.disableFontColor;
                const text = (this.text instanceof Function) ? this.text.call(this) : this.text;
                Game.getScreen().textAlign = this.textAlign;
                Game.getScreen().textBaseline = this.textBaseline;
                Game.getScreen().fillTextBox(text, this.left + 1, this.top + 1, this.width - 2, this.height - 2);
            }
            regist(dispatcher) {
                const cancelHandler = dispatcher.onClick(this, (...args) => this.click.apply(this, args));
                this.unregist = (d) => cancelHandler();
            }
            unregist(dispatcher) { }
        }
        Button.defaultValue = {
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
            visible: true,
            enable: true,
            disableEdgeColor: `rgb(34,34,34)`,
            disableColor: `rgb(133,133,133)`,
            disableFontColor: `rgb(192,192,192)`,
        };
        GUI.Button = Button;
        class ImageButton {
            constructor({ left = 0, top = 0, width = 0, height = 0, texture = null, texLeft = 0, texTop = 0, texWidth = 0, texHeight = 0, visible = true, enable = true }) {
                this.left = left;
                this.top = top;
                this.width = width;
                this.height = height;
                this.texture = texture;
                this.texLeft = texLeft;
                this.texTop = texTop;
                this.texWidth = texWidth;
                this.texHeight = texHeight;
                this.visible = visible;
                this.enable = enable;
                this.click = () => { };
            }
            draw() {
                if (this.texture != null) {
                    Game.getScreen().drawImage(Game.getScreen().texture(this.texture), this.texLeft, this.texTop, this.texWidth, this.texHeight, this.left, this.top, this.width, this.height);
                }
            }
            regist(dispatcher) {
                const cancelHandler = dispatcher.onClick(this, (...args) => this.click.apply(this, args));
                this.unregist = (d) => cancelHandler();
            }
            unregist(dispatcher) { }
        }
        GUI.ImageButton = ImageButton;
        class ListBox {
            constructor({ left = 0, top = 0, width = 0, height = 0, lineHeight = 12, drawItem = () => { }, getItemCount = () => 0, visible = true, enable = true, scrollbarWidth = 1 }) {
                this.left = left;
                this.top = top;
                this.width = width;
                this.height = height;
                this.lineHeight = lineHeight;
                this.drawItem = drawItem;
                this.getItemCount = getItemCount;
                this.scrollValue = 0;
                this.visible = visible;
                this.enable = enable;
                this.scrollbarWidth = scrollbarWidth;
                this.click = () => { };
            }
            update() {
                var contentHeight = this.getItemCount() * this.lineHeight;
                if (this.height >= contentHeight) {
                    this.scrollValue = 0;
                }
                else if (this.scrollValue < 0) {
                    this.scrollValue = 0;
                }
                else if (this.scrollValue > (contentHeight - this.height)) {
                    this.scrollValue = contentHeight - this.height;
                }
            }
            draw() {
                let sy = -(~~this.scrollValue % this.lineHeight);
                let index = ~~((~~this.scrollValue) / this.lineHeight);
                let itemCount = this.getItemCount();
                let drawResionHeight = this.height - sy;
                for (;;) {
                    if (sy >= this.height) {
                        break;
                    }
                    if (index >= itemCount) {
                        break;
                    }
                    Game.getScreen().save();
                    Game.getScreen().beginPath();
                    Game.getScreen().rect(this.left - 1, Math.max(this.top, this.top + sy), this.width + 1 - this.scrollbarWidth, Math.min(drawResionHeight, this.lineHeight));
                    Game.getScreen().clip();
                    this.drawItem(this.left, this.top + sy, this.width - this.scrollbarWidth, this.lineHeight, index);
                    Game.getScreen().restore();
                    drawResionHeight -= this.lineHeight;
                    sy += this.lineHeight;
                    index++;
                }
                const contentHeight = this.lineHeight * itemCount;
                if (contentHeight > this.height) {
                    const viewSizeRate = this.height * 1.0 / contentHeight;
                    const scrollBarHeight = viewSizeRate * this.height;
                    const scrollBarBlankHeight = this.height - scrollBarHeight;
                    const scrollPosRate = this.scrollValue * 1.0 / (contentHeight - this.height);
                    const scrollBarTop = (scrollBarBlankHeight * scrollPosRate);
                    Game.getScreen().fillStyle = "rgb(128,128,128)";
                    Game.getScreen().fillRect(this.left + this.width - this.scrollbarWidth, this.top, this.scrollbarWidth, this.height);
                    Game.getScreen().fillStyle = "rgb(255,255,255)";
                    Game.getScreen().fillRect(this.left + this.width - this.scrollbarWidth, this.top + ~~scrollBarTop, this.scrollbarWidth, ~~scrollBarHeight);
                }
            }
            getItemIndexByPosition(x, y) {
                if (x < 0 || this.width <= x || y < 0 || this.height <= y) {
                    return -1;
                }
                const index = ~~((y + this.scrollValue) / this.lineHeight);
                if (index < 0 || index >= this.getItemCount()) {
                    return -1;
                }
                else {
                    return index;
                }
            }
            regist(dispatcher) {
                const cancelHandlers = [
                    dispatcher.onSwipe(this, (deltaX, deltaY) => {
                        this.scrollValue -= deltaY;
                        this.update();
                    }),
                    dispatcher.onClick(this, (...args) => this.click.apply(this, args))
                ];
                this.unregist = (d) => cancelHandlers.forEach(x => x());
            }
            unregist(dispatcher) { }
        }
        GUI.ListBox = ListBox;
        class HorizontalSlider {
            constructor({ left = 0, top = 0, width = 0, height = 0, sliderWidth = 5, edgeColor = `rgb(128,128,128)`, color = `rgb(255,255,255)`, bgColor = `rgb(192,192,192)`, font = undefined, fontColor = `rgb(0,0,0)`, minValue = 0, maxValue = 0, visible = true, enable = true, }) {
                this.left = left;
                this.top = top;
                this.width = width;
                this.height = height;
                this.sliderWidth = sliderWidth;
                this.edgeColor = edgeColor;
                this.color = color;
                this.bgColor = bgColor;
                this.font = font;
                this.fontColor = fontColor;
                this.minValue = minValue;
                this.maxValue = maxValue;
                this.value = minValue;
                this.visible = visible;
                this.enable = enable;
            }
            draw() {
                const lineWidth = this.width - this.sliderWidth;
                Game.getScreen().fillStyle = this.bgColor;
                Game.getScreen().fillRect(this.left - 0.5, this.top - 0.5, this.width, this.height);
                Game.getScreen().fillStyle = this.color;
                Game.getScreen().strokeStyle = this.edgeColor;
                Game.getScreen().fillRect(this.left + ~~(lineWidth * (this.value - this.minValue) / (this.maxValue - this.minValue)) - 0.5, this.top - 0.5, this.sliderWidth, this.height);
                Game.getScreen().strokeRect(this.left + ~~(lineWidth * (this.value - this.minValue) / (this.maxValue - this.minValue)) - 0.5, this.top - 0.5, this.sliderWidth, this.height);
            }
            update() {
                const rangeSize = this.maxValue - this.minValue;
                if (rangeSize == 0) {
                    this.value = this.minValue;
                }
                else if (this.value < this.minValue) {
                    this.value = this.minValue;
                }
                else if (this.value >= this.maxValue) {
                    this.value = this.maxValue;
                }
            }
            regist(dispatcher) {
                var cancelHandler = dispatcher.onSwipe(this, (dx, dy, x, y) => {
                    const rangeSize = this.maxValue - this.minValue;
                    if (rangeSize == 0) {
                        this.value = this.minValue;
                    }
                    else {
                        if (x <= this.sliderWidth / 2) {
                            this.value = this.minValue;
                        }
                        else if (x >= this.width - this.sliderWidth / 2) {
                            this.value = this.maxValue;
                        }
                        else {
                            const width = this.width - this.sliderWidth;
                            const xx = x - ~~(this.sliderWidth / 2);
                            this.value = Math.trunc((xx * rangeSize) / width) + this.minValue;
                        }
                    }
                });
                this.unregist = (d) => cancelHandler();
            }
            unregist(dispatcher) { }
        }
        GUI.HorizontalSlider = HorizontalSlider;
    })(GUI = Game.GUI || (Game.GUI = {}));
})(Game || (Game = {}));
var Game;
(function (Game) {
    let Input;
    (function (Input) {
        class CustomPointerEvent extends CustomEvent {
        }
        let PointerChangeStatus;
        (function (PointerChangeStatus) {
            PointerChangeStatus[PointerChangeStatus["Down"] = 0] = "Down";
            PointerChangeStatus[PointerChangeStatus["Up"] = 1] = "Up";
            PointerChangeStatus[PointerChangeStatus["Leave"] = 2] = "Leave";
        })(PointerChangeStatus || (PointerChangeStatus = {}));
        class InputManager extends Dispatcher.EventDispatcher {
            constructor() {
                super();
                if (!window.TouchEvent) {
                    console.log("TouchEvent is not supported by your browser.");
                    window.TouchEvent = function () { };
                }
                if (!window.PointerEvent) {
                    console.log("PointerEvent is not supported by your browser.");
                    window.PointerEvent = function () { };
                }
                this.isScrolling = false;
                this.timeout = 0;
                this.sDistX = 0;
                this.sDistY = 0;
                this.maybeClick = false;
                this.maybeClickX = 0;
                this.maybeClickY = 0;
                this.prevTimeStamp = 0;
                this.prevInputType = "none";
                window.addEventListener("scroll", () => {
                    if (!this.isScrolling) {
                        this.sDistX = window.pageXOffset;
                        this.sDistY = window.pageYOffset;
                    }
                    this.isScrolling = true;
                    clearTimeout(this.timeout);
                    this.timeout = setTimeout(() => {
                        this.isScrolling = false;
                        this.sDistX = 0;
                        this.sDistY = 0;
                    }, 100);
                });
                // add event listener to body
                document.onselectstart = () => false;
                document.oncontextmenu = () => false;
                if (document.body["pointermove"] !== undefined) {
                    document.body.addEventListener('touchmove', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('touchdown', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('touchup', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('mousemove', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('mousedown', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('mouseup', evt => { evt.preventDefault(); }, false);
                    document.body.addEventListener('pointerdown', (ev) => this.fire('pointerdown', ev));
                    document.body.addEventListener('pointermove', (ev) => this.fire('pointermove', ev));
                    document.body.addEventListener('pointerup', (ev) => this.fire('pointerup', ev));
                    document.body.addEventListener('pointerleave', (ev) => this.fire('pointerleave', ev));
                }
                else {
                    document.body.addEventListener('mousedown', this.pointerDown.bind(this), false);
                    document.body.addEventListener('touchstart', this.pointerDown.bind(this), false);
                    document.body.addEventListener('mouseup', this.pointerUp.bind(this), false);
                    document.body.addEventListener('touchend', this.pointerUp.bind(this), false);
                    document.body.addEventListener('mousemove', this.pointerMove.bind(this), false);
                    document.body.addEventListener('touchmove', this.pointerMove.bind(this), false);
                    document.body.addEventListener('mouseleave', this.pointerLeave.bind(this), false);
                    document.body.addEventListener('touchleave', this.pointerLeave.bind(this), false);
                    document.body.addEventListener('touchcancel', this.pointerUp.bind(this), false);
                }
                this.capture = false;
                this.lastPageX = 0;
                this.lastPageY = 0;
                this.downup = 0;
                this.status = PointerChangeStatus.Leave;
                this.clicked = false;
                this.lastDownPageX = 0;
                this.lastDownPageY = 0;
                this.draglen = 0;
                this.captureHandler = this.captureHandler.bind(this);
                this.on('pointerdown', this.captureHandler);
                this.on('pointermove', this.captureHandler);
                this.on('pointerup', this.captureHandler);
                this.on('pointerleave', this.captureHandler);
            }
            get pageX() {
                return this.lastPageX;
            }
            get pageY() {
                return this.lastPageY;
            }
            isDown() {
                return this.downup === 1;
            }
            isPush() {
                return this.downup > 1;
            }
            isUp() {
                return this.downup === -1;
            }
            isMove() {
                return (~~this.startPageX !== ~~this.lastPageX) || (~~this.startPageY !== ~~this.lastPageY);
            }
            isClick() {
                return this.clicked;
            }
            isRelease() {
                return this.downup < -1;
            }
            startCapture() {
                this.capture = true;
                this.startPageX = ~~this.lastPageX;
                this.startPageY = ~~this.lastPageY;
            }
            endCapture() {
                this.capture = false;
                if (this.status === PointerChangeStatus.Down) {
                    if (this.downup < 1) {
                        this.downup = 1;
                    }
                    else {
                        this.downup += 1;
                    }
                }
                else if (this.status === PointerChangeStatus.Up) {
                    if (this.downup > -1) {
                        this.downup = -1;
                    }
                    else {
                        this.downup -= 1;
                    }
                }
                else {
                    this.downup = 0;
                }
                this.clicked = false;
                if (this.downup === -1) {
                    if (this.draglen < 5) {
                        this.clicked = true;
                    }
                }
                else if (this.downup === 1) {
                    this.lastDownPageX = this.lastPageX;
                    this.lastDownPageY = this.lastPageY;
                    this.draglen = 0;
                }
                else if (this.downup > 1) {
                    this.draglen = Math.max(this.draglen, Math.sqrt((this.lastDownPageX - this.lastPageX) * (this.lastDownPageX - this.lastPageX) + (this.lastDownPageY - this.lastPageY) * (this.lastDownPageY - this.lastPageY)));
                }
            }
            captureHandler(e) {
                if (this.capture === false) {
                    return;
                }
                switch (e.type) {
                    case "pointerdown":
                        this.status = PointerChangeStatus.Down;
                        break;
                    case "pointerup":
                        this.status = PointerChangeStatus.Up;
                        break;
                    case "pointerleave":
                        this.status = PointerChangeStatus.Leave;
                        break;
                    case "pointermove":
                        break;
                }
                this.lastPageX = e.pageX;
                this.lastPageY = e.pageY;
            }
            checkEvent(e) {
                e.preventDefault();
                const istouch = e instanceof TouchEvent || (e instanceof PointerEvent && e.pointerType === "touch");
                const ismouse = e instanceof MouseEvent || ((e instanceof PointerEvent && (e.pointerType === "mouse" || e.pointerType === "pen")));
                if (istouch && this.prevInputType !== "touch") {
                    if (e.timeStamp - this.prevTimeStamp >= 500) {
                        this.prevInputType = "touch";
                        this.prevTimeStamp = e.timeStamp;
                        return true;
                    }
                    else {
                        return false;
                    }
                }
                else if (ismouse && this.prevInputType !== "mouse") {
                    if (e.timeStamp - this.prevTimeStamp >= 500) {
                        this.prevInputType = "mouse";
                        this.prevTimeStamp = e.timeStamp;
                        return true;
                    }
                    else {
                        return false;
                    }
                }
                else {
                    this.prevInputType = istouch ? "touch" : ismouse ? "mouse" : "none";
                    this.prevTimeStamp = e.timeStamp;
                    return istouch || ismouse;
                }
            }
            pointerDown(e) {
                if (this.checkEvent(e)) {
                    const evt = this.makePointerEvent("down", e);
                    const singleFinger = (e instanceof MouseEvent) || (e instanceof TouchEvent && e.touches.length === 1);
                    if (!this.isScrolling && singleFinger) {
                        this.maybeClick = true;
                        this.maybeClickX = evt.pageX;
                        this.maybeClickY = evt.pageY;
                    }
                }
                return false;
            }
            pointerLeave(e) {
                if (this.checkEvent(e)) {
                    this.maybeClick = false;
                    this.makePointerEvent("leave", e);
                }
                return false;
            }
            pointerMove(e) {
                if (this.checkEvent(e)) {
                    this.makePointerEvent("move", e);
                }
                return false;
            }
            pointerUp(e) {
                if (this.checkEvent(e)) {
                    const evt = this.makePointerEvent("up", e);
                    if (this.maybeClick) {
                        if (Math.abs(this.maybeClickX - evt.pageX) < 5 && Math.abs(this.maybeClickY - evt.pageY) < 5) {
                            if (!this.isScrolling ||
                                (Math.abs(this.sDistX - window.pageXOffset) < 5 &&
                                    Math.abs(this.sDistY - window.pageYOffset) < 5)) {
                                this.makePointerEvent("click", e);
                            }
                        }
                    }
                    this.maybeClick = false;
                }
                return false;
            }
            makePointerEvent(type, e) {
                const evt = document.createEvent("CustomEvent");
                const eventType = `pointer${type}`;
                evt.initCustomEvent(eventType, true, true, {});
                evt.touch = e.type.indexOf("touch") === 0;
                evt.mouse = e.type.indexOf("mouse") === 0;
                if (evt.touch) {
                    const touchEvent = e;
                    evt.pointerId = touchEvent.changedTouches[0].identifier;
                    evt.pageX = touchEvent.changedTouches[0].pageX;
                    evt.pageY = touchEvent.changedTouches[0].pageY;
                }
                if (evt.mouse) {
                    const mouseEvent = e;
                    evt.pointerId = 0;
                    evt.pageX = mouseEvent.clientX + window.pageXOffset;
                    evt.pageY = mouseEvent.clientY + window.pageYOffset;
                }
                evt.maskedEvent = e;
                this.fire(eventType, evt);
                return evt;
            }
        }
        Input.InputManager = InputManager;
        class VirtualStick {
            constructor(x = 120, y = 120, radius = 40) {
                this.isTouching = false;
                this.x = x;
                this.y = y;
                this.cx = 0;
                this.cy = 0;
                this.radius = radius;
                this.distance = 0;
                this.angle = 0;
                this.id = -1;
            }
            get dir4() {
                switch (~~((this.angle + 360 + 45) / 90) % 4) {
                    case 0: return 6; // left
                    case 1: return 2; // up
                    case 2: return 4; // right
                    case 3: return 8; // down
                }
                return 5; // neutral
            }
            get dir8() {
                const d = ~~((this.angle + 360 + 22.5) / 45) % 8;
                switch (d) {
                    case 0: return 6; // right
                    case 1: return 3; // right-down
                    case 2: return 2; // down
                    case 3: return 1; // left-down
                    case 4: return 4; // left
                    case 5: return 7; // left-up
                    case 6: return 8; // up
                    case 7: return 9; // right-up
                }
                return 5; // neutral
            }
            isHit(x, y) {
                const dx = x - this.x;
                const dy = y - this.y;
                return ((dx * dx) + (dy * dy)) <= this.radius * this.radius;
            }
            onpointingstart(id) {
                if (this.id !== -1) {
                    return false;
                }
                this.isTouching = true;
                this.cx = 0;
                this.cy = 0;
                this.angle = 0;
                this.distance = 0;
                this.id = id;
                return true;
            }
            onpointingend(id) {
                if (this.id !== id) {
                    return false;
                }
                this.isTouching = false;
                this.cx = 0;
                this.cy = 0;
                this.angle = 0;
                this.distance = 0;
                this.id = -1;
                return true;
            }
            onpointingmove(id, x, y) {
                if (this.isTouching === false) {
                    return false;
                }
                if (id !== this.id) {
                    return false;
                }
                let dx = x - this.x;
                let dy = y - this.y;
                let len = Math.sqrt((dx * dx) + (dy * dy));
                if (len > 0) {
                    dx /= len;
                    dy /= len;
                    if (len > this.radius) {
                        len = this.radius;
                    }
                    this.angle = Math.atan2(dy, dx) * 180 / Math.PI;
                    this.distance = len * 1.0 / this.radius;
                    this.cx = dx * len;
                    this.cy = dy * len;
                }
                else {
                    this.cx = 0;
                    this.cy = 0;
                    this.angle = 0;
                    this.distance = 0;
                }
                return true;
            }
        }
        Input.VirtualStick = VirtualStick;
    })(Input = Game.Input || (Game.Input = {}));
})(Game || (Game = {}));
function getDirectory(path) {
    return path.substring(0, path.lastIndexOf("/"));
}
function normalizePath(path) {
    return path.split("/").reduce((s, x) => {
        if (x === "..") {
            if (s.length > 1) {
                s.pop();
            }
            else {
                throw new Error("bad path");
            }
        }
        else if (x === ".") {
            if (s.length === 0) {
                s.push(x);
            }
        }
        else {
            s.push(x);
        }
        return s;
    }, new Array()).join("/");
}
var Game;
(function (Game) {
    let Scene;
    (function (Scene_1) {
        class Scene {
            constructor(manager, init) {
                this.manager = manager;
                this.state = null;
                this.init = init;
                this.update = () => { };
                this.draw = () => { };
                this.leave = () => { };
                this.suspend = () => { };
                this.resume = () => { };
            }
            next(...args) {
                this.update = this.state.next.apply(this.state, args).value;
            }
            enter(...data) {
                this.state = this.init.apply(this, data);
                this.next();
            }
        }
        Scene_1.Scene = Scene;
        class SceneManager {
            constructor() {
                this.sceneStack = [];
            }
            push(sceneDef, arg) {
                if (this.peek() != null && this.peek().suspend != null) {
                    this.peek().suspend();
                }
                this.sceneStack.push(new Scene(this, sceneDef));
                if (this.peek() != null && this.peek().enter != null) {
                    this.peek().enter.call(this.peek(), arg);
                }
            }
            pop() {
                if (this.sceneStack.length === 0) {
                    throw new Error("there is no scene.");
                }
                if (this.peek() != null) {
                    const p = this.sceneStack.pop();
                    if (p.leave != null) {
                        p.leave();
                    }
                }
                if (this.peek() != null && this.peek().resume != null) {
                    this.peek().resume();
                }
            }
            peek() {
                if (this.sceneStack.length > 0) {
                    return this.sceneStack[this.sceneStack.length - 1];
                }
                else {
                    return null;
                }
            }
            update(...args) {
                if (this.peek() != null && this.peek().update != null) {
                    this.peek().update.apply(this.peek(), args);
                }
                return this;
            }
            draw() {
                if (this.peek() != null && this.peek().draw != null) {
                    this.peek().draw.apply(this.peek());
                }
                return this;
            }
        }
        Scene_1.SceneManager = SceneManager;
    })(Scene = Game.Scene || (Game.Scene = {}));
})(Game || (Game = {}));
var Game;
(function (Game) {
    let Sound;
    (function (Sound) {
        class ManagedSoundChannel {
            constructor() {
                this.audioBufferNode = null;
                this.playRequest = false;
                this.stopRequest = false;
                this.loopPlay = false;
            }
            reset() {
                this.audioBufferNode = null;
                this.playRequest = false;
                this.stopRequest = false;
                this.loopPlay = false;
            }
        }
        class UnmanagedSoundChannel {
            constructor(sound, buffer) {
                this.isEnded = true;
                this.bufferSource = null;
                this.buffer = null;
                this.sound = null;
                this.buffer = buffer;
                this.sound = sound;
                this.reset();
            }
            reset() {
                this.stop();
                this.bufferSource = this.sound.createBufferSource(this.buffer);
                this.bufferSource.onended = () => this.isEnded = true;
            }
            loopplay() {
                if (this.isEnded) {
                    this.bufferSource.loop = true;
                    this.bufferSource.start(0);
                    this.isEnded = false;
                }
            }
            play() {
                if (this.isEnded) {
                    this.bufferSource.loop = false;
                    this.bufferSource.start(0);
                    this.isEnded = false;
                }
            }
            stop() {
                if (!this.isEnded) {
                    this.bufferSource.stop(0);
                    this.bufferSource.disconnect();
                    this.isEnded = true;
                }
            }
        }
        class SoundManager {
            constructor() {
                this.bufferSourceIdCount = 0;
                if (window.AudioContext) {
                    console.log("Use AudioContext.");
                    this.audioContext = new window.AudioContext();
                }
                else if (window.webkitAudioContext) {
                    console.log("Use webkitAudioContext.");
                    this.audioContext = new window.webkitAudioContext();
                }
                else {
                    console.error("Neither AudioContext nor webkitAudioContext is supported by your browser.");
                    throw new Error("Neither AudioContext nor webkitAudioContext is supported by your browser.");
                }
                this.channels = new Map();
                this.bufferSourceIdCount = 0;
                this.playingBufferSources = new Map();
                this.reset();
                const touchEventHooker = () => {
                    // A small hack to unlock AudioContext on mobile safari.
                    const buffer = this.audioContext.createBuffer(1, (this.audioContext.sampleRate / 100), this.audioContext.sampleRate);
                    const channel = buffer.getChannelData(0);
                    channel.fill(0);
                    const src = this.audioContext.createBufferSource();
                    src.buffer = buffer;
                    src.connect(this.audioContext.destination);
                    src.start(this.audioContext.currentTime);
                    document.body.removeEventListener('touchstart', touchEventHooker);
                };
                document.body.addEventListener('touchstart', touchEventHooker);
            }
            createBufferSource(buffer) {
                const bufferSource = this.audioContext.createBufferSource();
                bufferSource.buffer = buffer;
                bufferSource.connect(this.audioContext.destination);
                return bufferSource;
            }
            loadSound(file) {
                return ajax(file, "arraybuffer").then(xhr => {
                    return new Promise((resolve, reject) => {
                        this.audioContext.decodeAudioData(xhr.response, (audioBufferNode) => {
                            resolve(audioBufferNode);
                        }, () => {
                            reject(new Error(`cannot decodeAudioData : ${file} `));
                        });
                    });
                });
                //
                // decodeAudioData dose not return 'Promise Object 'on mobile safari :-(
                // Therefore, these codes will not work ...
                //
                // const xhr: XMLHttpRequest = await ajax(file, "arraybuffer");
                // var audioBufferNode = await this.audioContext.decodeAudioData(xhr.response);
                // return audioBufferNode;
            }
            loadSoundToChannel(file, channelId) {
                return __awaiter(this, void 0, void 0, function* () {
                    const audioBufferNode = yield this.loadSound(file);
                    const channel = new ManagedSoundChannel();
                    channel.audioBufferNode = audioBufferNode;
                    this.channels.set(channelId, channel);
                    return;
                });
            }
            loadSoundsToChannel(config, startCallback = () => { }, endCallback = () => { }) {
                return Promise.all(Object.keys(config).map((channelId) => {
                    startCallback(channelId);
                    const ret = this.loadSoundToChannel(config[channelId], channelId).then(() => endCallback(channelId));
                    return ret;
                })).then(() => { });
            }
            createUnmanagedSoundChannel(file) {
                return this.loadSound(file)
                    .then((audioBufferNode) => new UnmanagedSoundChannel(this, audioBufferNode));
            }
            reqPlayChannel(channelId, loop = false) {
                const channel = this.channels.get(channelId);
                if (channel) {
                    channel.playRequest = true;
                    channel.loopPlay = loop;
                }
            }
            reqStopChannel(channelId) {
                const channel = this.channels.get(channelId);
                if (channel) {
                    channel.stopRequest = true;
                }
            }
            playChannel() {
                this.channels.forEach((c, i) => {
                    if (c.stopRequest) {
                        c.stopRequest = false;
                        if (c.audioBufferNode == null) {
                            return;
                        }
                        this.playingBufferSources.forEach((value, key) => {
                            if (value.id === i) {
                                const srcNode = value.buffer;
                                srcNode.stop();
                                srcNode.disconnect();
                                this.playingBufferSources.set(key, null);
                                this.playingBufferSources.delete(key);
                            }
                        });
                    }
                    if (c.playRequest) {
                        c.playRequest = false;
                        if (c.audioBufferNode == null) {
                            return;
                        }
                        const src = this.audioContext.createBufferSource();
                        if (src == null) {
                            throw new Error("createBufferSourceに失敗。");
                        }
                        const bufferid = this.bufferSourceIdCount++;
                        this.playingBufferSources.set(bufferid, { id: i, buffer: src });
                        src.buffer = c.audioBufferNode;
                        src.loop = c.loopPlay;
                        src.connect(this.audioContext.destination);
                        src.onended = () => {
                            src.stop(0);
                            src.disconnect();
                            this.playingBufferSources.set(bufferid, null);
                            this.playingBufferSources.delete(bufferid);
                            src.onended = null; // If you forget this null assignment, the AudioBufferSourceNode object will not be destroyed and a memory leak will occur. :-(
                        };
                        src.start(0);
                    }
                });
            }
            stop() {
                const oldPlayingBufferSources = this.playingBufferSources;
                this.playingBufferSources = new Map();
                oldPlayingBufferSources.forEach((value, key) => {
                    const s = value.buffer;
                    if (s != null) {
                        s.stop(0);
                        s.disconnect();
                        oldPlayingBufferSources.set(key, null);
                        oldPlayingBufferSources.delete(key);
                    }
                });
            }
            reset() {
                this.channels.clear();
                this.playingBufferSources.clear();
            }
        }
        Sound.SoundManager = SoundManager;
    })(Sound = Game.Sound || (Game.Sound = {}));
})(Game || (Game = {}));
var Game;
(function (Game) {
    let Timer;
    (function (Timer) {
        class AnimationTimer extends Dispatcher.SingleDispatcher {
            constructor() {
                super();
                this.animationFrameId = NaN;
                this.prevTime = NaN;
            }
            start() {
                if (!isNaN(this.animationFrameId)) {
                    this.stop();
                }
                this.animationFrameId = requestAnimationFrame(this.tick.bind(this));
                return !isNaN(this.animationFrameId);
            }
            stop() {
                if (!isNaN(this.animationFrameId)) {
                    cancelAnimationFrame(this.animationFrameId);
                    this.animationFrameId = NaN;
                }
            }
            tick(ts) {
                requestAnimationFrame(this.tick.bind(this));
                if (!isNaN(this.prevTime)) {
                    const delta = ts - this.prevTime;
                    this.fire(delta, ts);
                }
                this.prevTime = ts;
            }
        }
        Timer.AnimationTimer = AnimationTimer;
    })(Timer = Game.Timer || (Game.Timer = {}));
})(Game || (Game = {}));
var Game;
(function (Game) {
    class Video {
        constructor(config) {
            this.canvasElement = document.getElementById(config.id);
            if (!this.canvasElement) {
                throw new Error("your browser is not support canvas.");
            }
            this.id = config.id;
            this.offscreenWidth = config.offscreenWidth;
            this.offscreenHeight = config.offscreenHeight;
            this.scaleX = config.scaleX;
            this.scaleY = config.scaleY;
            this.canvasElement.width = this.offscreenWidth * this.scaleX;
            this.canvasElement.height = this.offscreenHeight * this.scaleY;
            this.canvasRenderingContext2D = this.canvasElement.getContext("2d");
            if (!this.canvasRenderingContext2D) {
                throw new Error("your browser is not support CanvasRenderingContext2D.");
            }
            this.images = new Map();
            this.arc = this.canvasRenderingContext2D.arc.bind(this.canvasRenderingContext2D);
            this.arcTo = this.canvasRenderingContext2D.arcTo.bind(this.canvasRenderingContext2D);
            this.beginPath = this.canvasRenderingContext2D.beginPath.bind(this.canvasRenderingContext2D);
            this.bezierCurveTo = this.canvasRenderingContext2D.bezierCurveTo.bind(this.canvasRenderingContext2D);
            this.clearRect = this.canvasRenderingContext2D.clearRect.bind(this.canvasRenderingContext2D);
            this.clip = this.canvasRenderingContext2D.clip.bind(this.canvasRenderingContext2D);
            this.closePath = this.canvasRenderingContext2D.closePath.bind(this.canvasRenderingContext2D);
            this.createImageData = this.canvasRenderingContext2D.createImageData.bind(this.canvasRenderingContext2D);
            this.createLinearGradient = this.canvasRenderingContext2D.createLinearGradient.bind(this.canvasRenderingContext2D);
            this.createPattern = this.canvasRenderingContext2D.createPattern.bind(this.canvasRenderingContext2D);
            this.createRadialGradient = this.canvasRenderingContext2D.createRadialGradient.bind(this.canvasRenderingContext2D);
            this.drawImage = this.canvasRenderingContext2D.drawImage.bind(this.canvasRenderingContext2D);
            this.fill = this.canvasRenderingContext2D.fill.bind(this.canvasRenderingContext2D);
            this.fillRect = this.canvasRenderingContext2D.fillRect.bind(this.canvasRenderingContext2D);
            this.fillText = this.canvasRenderingContext2D.fillText.bind(this.canvasRenderingContext2D);
            this.getImageData = this.canvasRenderingContext2D.getImageData.bind(this.canvasRenderingContext2D);
            this.getLineDash = this.canvasRenderingContext2D.getLineDash.bind(this.canvasRenderingContext2D);
            this.isPointInPath = this.canvasRenderingContext2D.isPointInPath.bind(this.canvasRenderingContext2D);
            this.lineTo = this.canvasRenderingContext2D.lineTo.bind(this.canvasRenderingContext2D);
            this.measureText = this.canvasRenderingContext2D.measureText.bind(this.canvasRenderingContext2D);
            this.moveTo = this.canvasRenderingContext2D.moveTo.bind(this.canvasRenderingContext2D);
            this.putImageData = this.canvasRenderingContext2D.putImageData.bind(this.canvasRenderingContext2D);
            this.quadraticCurveTo = this.canvasRenderingContext2D.quadraticCurveTo.bind(this.canvasRenderingContext2D);
            this.rect = this.canvasRenderingContext2D.rect.bind(this.canvasRenderingContext2D);
            this.restore = this.canvasRenderingContext2D.restore.bind(this.canvasRenderingContext2D);
            this.rotate = this.canvasRenderingContext2D.rotate.bind(this.canvasRenderingContext2D);
            this.save = this.canvasRenderingContext2D.save.bind(this.canvasRenderingContext2D);
            this.scale = this.canvasRenderingContext2D.scale.bind(this.canvasRenderingContext2D);
            this.setLineDash = this.canvasRenderingContext2D.setLineDash.bind(this.canvasRenderingContext2D);
            this.setTransform = this.canvasRenderingContext2D.setTransform.bind(this.canvasRenderingContext2D);
            this.stroke = this.canvasRenderingContext2D.stroke.bind(this.canvasRenderingContext2D);
            this.strokeRect = this.canvasRenderingContext2D.strokeRect.bind(this.canvasRenderingContext2D);
            this.strokeText = this.canvasRenderingContext2D.strokeText.bind(this.canvasRenderingContext2D);
            this.transform = this.canvasRenderingContext2D.transform.bind(this.canvasRenderingContext2D);
            this.translate = this.canvasRenderingContext2D.translate.bind(this.canvasRenderingContext2D);
            this.ellipse = this.canvasRenderingContext2D.ellipse.bind(this.canvasRenderingContext2D);
        }
        //
        get canvas() { return this.canvasRenderingContext2D.canvas; }
        get fillStyle() { return this.canvasRenderingContext2D.fillStyle; }
        set fillStyle(value) { this.canvasRenderingContext2D.fillStyle = value; }
        get font() { return this.canvasRenderingContext2D.font; }
        set font(value) { this.canvasRenderingContext2D.font = value; }
        get globalAlpha() { return this.canvasRenderingContext2D.globalAlpha; }
        set globalAlpha(value) { this.canvasRenderingContext2D.globalAlpha = value; }
        get globalCompositeOperation() { return this.canvasRenderingContext2D.globalCompositeOperation; }
        set globalCompositeOperation(value) { this.canvasRenderingContext2D.globalCompositeOperation = value; }
        get lineCap() { return this.canvasRenderingContext2D.lineCap; }
        set lineCap(value) { this.canvasRenderingContext2D.lineCap = value; }
        get lineDashOffset() { return this.canvasRenderingContext2D.lineDashOffset; }
        set lineDashOffset(value) { this.canvasRenderingContext2D.lineDashOffset = value; }
        get lineJoin() { return this.canvasRenderingContext2D.lineJoin; }
        set lineJoin(value) { this.canvasRenderingContext2D.lineJoin = value; }
        get lineWidth() { return this.canvasRenderingContext2D.lineWidth; }
        set lineWidth(value) { this.canvasRenderingContext2D.lineWidth = value; }
        get miterLimit() { return this.canvasRenderingContext2D.miterLimit; }
        set miterLimit(value) { this.canvasRenderingContext2D.miterLimit = value; }
        // get msFillRule(): string { return this.context.msFillRule; }
        // set msFillRule(value: string) { this.context.msFillRule = value; }
        get shadowBlur() { return this.canvasRenderingContext2D.shadowBlur; }
        set shadowBlur(value) { this.canvasRenderingContext2D.shadowBlur = value; }
        get shadowColor() { return this.canvasRenderingContext2D.shadowColor; }
        set shadowColor(value) { this.canvasRenderingContext2D.shadowColor = value; }
        get shadowOffsetX() { return this.canvasRenderingContext2D.shadowOffsetX; }
        set shadowOffsetX(value) { this.canvasRenderingContext2D.shadowOffsetX = value; }
        get shadowOffsetY() { return this.canvasRenderingContext2D.shadowOffsetY; }
        set shadowOffsetY(value) { this.canvasRenderingContext2D.shadowOffsetY = value; }
        get strokeStyle() { return this.canvasRenderingContext2D.strokeStyle; }
        set strokeStyle(value) { this.canvasRenderingContext2D.strokeStyle = value; }
        get textAlign() { return this.canvasRenderingContext2D.textAlign; }
        set textAlign(value) { this.canvasRenderingContext2D.textAlign = value; }
        get textBaseline() { return this.canvasRenderingContext2D.textBaseline; }
        set textBaseline(value) { this.canvasRenderingContext2D.textBaseline = value; }
        get imageSmoothingEnabled() {
            if ('imageSmoothingEnabled' in this.canvasRenderingContext2D) {
                return this.canvasRenderingContext2D.imageSmoothingEnabled;
            }
            if ('mozImageSmoothingEnabled' in this.canvasRenderingContext2D) {
                return this.canvasRenderingContext2D.mozImageSmoothingEnabled;
            }
            if ('webkitImageSmoothingEnabled' in this.canvasRenderingContext2D) {
                return this.canvasRenderingContext2D.webkitImageSmoothingEnabled;
            }
            return false;
        }
        set imageSmoothingEnabled(value) {
            if ('imageSmoothingEnabled' in this.canvasRenderingContext2D) {
                this.canvasRenderingContext2D.imageSmoothingEnabled = value;
                return;
            }
            if ('mozImageSmoothingEnabled' in this.canvasRenderingContext2D) {
                this.canvasRenderingContext2D.mozImageSmoothingEnabled = value;
                return;
            }
            if ('webkitImageSmoothingEnabled' in this.canvasRenderingContext2D) {
                this.canvasRenderingContext2D.webkitImageSmoothingEnabled = value;
                return;
            }
        }
        drawTextBox(text, left, top, width, height, drawTextPred) {
            const metrics = this.measureText(text);
            const lineHeight = this.measureText("あ").width;
            const lines = text.split(/\n/);
            let offY = 0;
            lines.forEach((x, i) => {
                const metrics = this.measureText(x);
                const sublines = [];
                if (metrics.width > width) {
                    let len = 1;
                    while (x.length > 0) {
                        const metrics = this.measureText(x.substr(0, len));
                        if (metrics.width > width) {
                            sublines.push(x.substr(0, len - 1));
                            x = x.substring(len - 1);
                            len = 1;
                        }
                        else if (len == x.length) {
                            sublines.push(x);
                            break;
                        }
                        else {
                            len++;
                        }
                    }
                }
                else {
                    sublines.push(x);
                }
                sublines.forEach((x) => {
                    drawTextPred(x, left + 1, top + offY + 1);
                    offY += (lineHeight + 1);
                });
            });
        }
        fillTextBox(text, left, top, width, height) {
            this.drawTextBox(text, left, top, width, height, this.fillText.bind(this));
        }
        strokeTextBox(text, left, top, width, height) {
            this.drawTextBox(text, left, top, width, height, this.strokeText.bind(this));
        }
        drawTile(image, offsetX, offsetY, sprite, spritesize, tile) {
            for (let y = 0; y < tile.height; y++) {
                for (let x = 0; x < tile.width; x++) {
                    const chip = tile.value(x, y);
                    this.drawImage(image, sprite[chip][0] * spritesize[0], sprite[chip][1] * spritesize[1], spritesize[0], spritesize[1], offsetX + x * spritesize[0], offsetY + y * spritesize[1], spritesize[0], spritesize[1]);
                }
            }
        }
        get width() {
            return this.canvasRenderingContext2D.canvas.width;
        }
        get height() {
            return this.canvasRenderingContext2D.canvas.height;
        }
        loadImage(asserts, startCallback = () => { }, endCallback = () => { }) {
            return Promise.all(Object.keys(asserts).map((x) => new Promise((resolve, reject) => {
                startCallback(x);
                const img = new Image();
                img.onload = () => {
                    this.images.set(x, img);
                    endCallback(x);
                    resolve();
                };
                img.onerror = () => {
                    const msg = `ファイル ${asserts[x]}のロードに失敗。`;
                    console.error(msg);
                    reject(msg);
                };
                img.src = asserts[x];
            }))).then(() => {
                return true;
            });
        }
        texture(id) {
            return this.images.get(id);
        }
        //
        begin() {
            Game.getScreen().save();
            Game.getScreen().clearRect(0, 0, this.width, this.height);
            Game.getScreen().scale(this.scaleX, this.scaleY);
            Game.getScreen().save();
        }
        end() {
            Game.getScreen().restore();
            Game.getScreen().restore();
        }
        pagePointToScreenPoint(x, y) {
            const cr = this.canvasRenderingContext2D.canvas.getBoundingClientRect();
            const sx = (x - (cr.left + window.pageXOffset));
            const sy = (y - (cr.top + window.pageYOffset));
            return [sx / this.scaleX, sy / this.scaleY];
        }
        pagePointContainScreen(x, y) {
            const pos = this.pagePointToScreenPoint(x, y);
            return 0 <= pos[0] && pos[0] < this.offscreenWidth && 0 <= pos[1] && pos[1] < this.offscreenHeight;
        }
    }
    Game.Video = Video;
})(Game || (Game = {}));
Array.prototype.removeIf = function (callback) {
    var i = this.length;
    while (i--) {
        if (callback(this[i], i)) {
            this.splice(i, 1);
        }
    }
};
Object.prototype.reduce = function (callback, seed) {
    Object.keys(this).forEach(key => seed = callback(seed, [this[key], key]));
    return seed;
};
var PathFinder;
(function (PathFinder) {
    const dir4 = [
        { x: 0, y: -1 },
        { x: 1, y: 0 },
        { x: 0, y: 1 },
        { x: -1, y: 0 }
    ];
    const dir8 = [
        { x: 0, y: -1 },
        { x: 1, y: 0 },
        { x: 0, y: 1 },
        { x: -1, y: 0 },
        { x: 1, y: -1 },
        { x: 1, y: 1 },
        { x: -1, y: 1 },
        { x: -1, y: -1 }
    ];
    // ダイクストラ法を用いた距離算出
    function calcDistanceByDijkstra({ array2D = null, sx = null, // 探索始点X座標
        sy = null, // 探索始点Y座標
        value = null, // 探索打ち切りの閾値
        costs = null, // ノードの重み
        left = 0, top = 0, right = undefined, bottom = undefined, timeout = 1000, topology = 8, output = undefined }) {
        if (left === undefined || left < 0) {
            right = 0;
        }
        if (top === undefined || top < 0) {
            bottom = 0;
        }
        if (right === undefined || right > array2D.width) {
            right = array2D.width;
        }
        if (bottom === undefined || bottom > array2D.height) {
            bottom = array2D.height;
        }
        if (output === undefined) {
            output = () => { };
        }
        const dirs = (topology === 8) ? dir8 : dir4;
        const work = new Array2D(array2D.width, array2D.height);
        work.value(sx, sy, value);
        output(sx, sy, value);
        const request = dirs.map(({ x, y }) => [sx + x, sy + y, value]);
        const start = Date.now();
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
            work.value(px, py, nextValue);
            output(px, py, nextValue);
            Array.prototype.push.apply(request, dirs.map(({ x, y }) => [px + x, py + y, nextValue]));
        }
    }
    PathFinder.calcDistanceByDijkstra = calcDistanceByDijkstra;
    // A*での経路探索
    function pathfind(array2D, fromX, fromY, toX, toY, costs, opts) {
        opts = Object.assign({ topology: 8 }, opts);
        const topology = opts.topology;
        let dirs;
        if (topology === 4) {
            dirs = dir4;
        }
        else if (topology === 8) {
            dirs = dir8;
        }
        else {
            throw new Error("Illegal topology");
        }
        const todo = [];
        const add = ((x, y, prev) => {
            // distance
            let distance;
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
            const obj = {
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
        const done = new Map();
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
                const result = [];
                while (item) {
                    result.push(item);
                    item = item.prev;
                }
                return result;
            }
            else {
                /* 隣接地点から移動可能地点を探す */
                for (let i = 0; i < dirs.length; i++) {
                    const dir = dirs[i];
                    const x = item.x + dir.x;
                    const y = item.y + dir.y;
                    const cost = costs[this.value(x, y)];
                    if (cost < 0) {
                        /* 侵入不可能 */
                        continue;
                    }
                    else {
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
    PathFinder.pathfind = pathfind;
    // 重み距離を使ったA*
    function pathfindByPropergation(array2D, fromX, fromY, toX, toY, propagation, { topology = 8 }) {
        let dirs;
        if (topology === 4) {
            dirs = dir4;
        }
        else if (topology === 8) {
            dirs = dir8;
        }
        else {
            throw new Error("Illegal topology");
        }
        const todo = [];
        const add = ((x, y, prev) => {
            // distance
            const distance = Math.abs(propagation.value(x, y) - propagation.value(fromX, fromY));
            const obj = {
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
        const done = new Map();
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
                const result = [];
                while (item) {
                    result.push(item);
                    item = item.prev;
                }
                return result;
            }
            else {
                /* 隣接地点から移動可能地点を探す */
                dirs.forEach((dir) => {
                    const x = item.x + dir.x;
                    const y = item.y + dir.y;
                    const pow = propagation.value(x, y);
                    if (pow === 0) {
                        /* 侵入不可能 */
                        return;
                    }
                    else {
                        /* 移動可能地点が探索済みでないなら探索キューに追加 */
                        const id = x + "," + y;
                        if (done.has(id)) {
                            return;
                        }
                        else {
                            add(x, y, item);
                        }
                    }
                });
            }
        }
        /* 始点に到達しなかったので空の経路を返す */
        return [];
    }
    PathFinder.pathfindByPropergation = pathfindByPropergation;
})(PathFinder || (PathFinder = {}));
var Scene;
(function (Scene) {
    function* boot() {
        let n = 0;
        let reqResource = 0;
        let loadedResource = 0;
        this.draw = () => {
            Game.getScreen().fillStyle = "rgb(255,255,255)";
            Game.getScreen().fillRect(0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            Game.getScreen().save();
            Game.getScreen().translate(Game.getScreen().offscreenWidth / 2, Game.getScreen().offscreenHeight / 2);
            Game.getScreen().rotate(n * Math.PI / 4);
            for (let i = 0; i < 8; i++) {
                const g = (i * 32);
                Game.getScreen().save();
                Game.getScreen().rotate(i * Math.PI / 4);
                Game.getScreen().fillStyle = `rgb(${g},${g},${g})`;
                Game.getScreen().fillRect(-5, -50, 10, 25);
                Game.getScreen().restore();
            }
            Game.getScreen().restore();
            Game.getScreen().fillStyle = "rgb(0,0,0)";
            const text = `loading ${loadedResource}/${reqResource}`;
            const size = Game.getScreen().measureText(text);
            Game.getScreen().fillText(text, Game.getScreen().offscreenWidth / 2 - size.width / 2, Game.getScreen().offscreenHeight - 20);
        };
        Promise.all([
            Game.getScreen().loadImage({
                title: "./assets/title.png",
                mapchip: "./assets/mapchip.png",
                charactor: "./assets/charactor.png",
                font7px: "./assets/font7px.png",
                font7wpx: "./assets/font7wpx.png",
                menuicon: "./assets/menuicon.png",
                status: "./assets/status.png",
                corridorbg: "./assets/corridorbg.png",
                classroom: "./assets/classroom.png",
                "shop/bg": "./assets/shop/bg.png",
                "shop/J11": "./assets/shop/J11.png",
            }, () => { reqResource++; }, () => { loadedResource++; }),
            Game.getSound().loadSoundsToChannel({
                title: "./assets/sound/title.mp3",
                dungeon: "./assets/sound/dungeon.mp3",
                classroom: "./assets/sound/classroom.mp3",
                kaidan: "./assets/sound/kaidan.mp3",
                atack: "./assets/sound/se_attacksword_1.mp3",
                explosion: "./assets/sound/explosion03.mp3",
                cursor: "./assets/sound/cursor.mp3",
                sen_ge_gusya01: "./assets/sound/sen_ge_gusya01.mp3",
                boyon1: "./assets/sound/boyon1.mp3",
                boyoyon1: "./assets/sound/boyoyon1.mp3",
                meka_ge_reji_op01: "./assets/sound/meka_ge_reji_op01.mp3"
            }, () => { reqResource++; }, () => { loadedResource++; }).catch((ev) => console.log("failed2", ev)),
            Data.Monster.SetupMonsterData(() => { reqResource++; }, () => { loadedResource++; }),
            Data.Charactor.SetupCharactorData(() => { reqResource++; }, () => { loadedResource++; }),
            Promise.resolve().then(() => {
                reqResource++;
                return new FontFace("PixelMplus10-Regular", "url(./assets/font/PixelMplus10-Regular.woff2)", {}).load();
            }).then((loadedFontFace) => {
                document.fonts.add(loadedFontFace);
                loadedResource++;
            })
        ]).then(() => {
            Game.getSceneManager().push(Scene.title, null);
            //const sd = new Data.SaveData.SaveData();
            //sd.loadGameData();
            //Game.getSceneManager().push(shop, sd);
            this.next();
        });
        yield (delta, ms) => {
            n = ~(ms / 50);
        };
    }
    Scene.boot = boot;
})(Scene || (Scene = {}));
var SpriteAnimation;
(function (SpriteAnimation) {
    class Animator {
        constructor(spriteSheet) {
            this.spriteSheet = spriteSheet;
            this.offx = 0;
            this.offy = 0;
            this.dir = 5;
            this.animDir = 2;
            this.animFrame = 0;
            this.animName = "idle";
        }
        setDir(dir) {
            if (dir === 0) {
                return;
            }
            this.dir = dir;
            switch (dir) {
                case 1: {
                    if (this.animDir === 4) {
                        this.animDir = 4;
                    }
                    else if (this.animDir === 2) {
                        this.animDir = 2;
                    }
                    else if (this.animDir === 8) {
                        this.animDir = 2;
                    }
                    else if (this.animDir === 6) {
                        this.animDir = 4;
                    }
                    break;
                }
                case 3: {
                    if (this.animDir === 4) {
                        this.animDir = 6;
                    }
                    else if (this.animDir === 2) {
                        this.animDir = 2;
                    }
                    else if (this.animDir === 8) {
                        this.animDir = 2;
                    }
                    else if (this.animDir === 6) {
                        this.animDir = 2;
                    }
                    break;
                }
                case 9: {
                    if (this.animDir === 4) {
                        this.animDir = 6;
                    }
                    else if (this.animDir === 2) {
                        this.animDir = 8;
                    }
                    else if (this.animDir === 8) {
                        this.animDir = 8;
                    }
                    else if (this.animDir === 6) {
                        this.animDir = 6;
                    }
                    break;
                }
                case 7: {
                    if (this.animDir === 4) {
                        this.animDir = 4;
                    }
                    else if (this.animDir === 2) {
                        this.animDir = 8;
                    }
                    else if (this.animDir === 8) {
                        this.animDir = 8;
                    }
                    else if (this.animDir === 6) {
                        this.animDir = 4;
                    }
                    break;
                }
                case 5: {
                    break;
                }
                default: {
                    this.animDir = dir;
                    break;
                }
            }
        }
        setAnimation(type, rate) {
            if (rate > 1) {
                rate = 1;
            }
            if (rate < 0) {
                rate = 0;
            }
            if (type === "move" || type === "action") {
                if (type === "move") {
                    this.offx = ~~(Array2D.DIR8[this.dir].x * 24 * rate);
                    this.offy = ~~(Array2D.DIR8[this.dir].y * 24 * rate);
                }
                else if (type === "action") {
                    this.offx = ~~(Array2D.DIR8[this.dir].x * 12 * Math.sin(rate * Math.PI));
                    this.offy = ~~(Array2D.DIR8[this.dir].y * 12 * Math.sin(rate * Math.PI));
                }
                this.animName = Animator.animationName[this.animDir];
            }
            else if (type === "dead") {
                this.animName = "dead";
                this.offx = 0;
                this.offy = 0;
            }
            else {
                return;
            }
            const animDefs = this.spriteSheet.getAnimation(this.animName);
            const totalWeight = animDefs.reduce((s, x) => s + x.time, 0);
            const targetRate = rate * totalWeight;
            let sum = 0;
            for (let i = 0; i < animDefs.length; i++) {
                const next = sum + animDefs[i].time;
                if (sum <= targetRate && targetRate < next) {
                    this.animFrame = i;
                    return;
                }
                sum = next;
            }
            this.animFrame = animDefs.length - 1;
        }
    }
    Animator.animationName = {
        2: "move_down",
        4: "move_left",
        5: "idle",
        6: "move_right",
        8: "move_up",
    };
    SpriteAnimation.Animator = Animator;
    // スプライトシート
    class SpriteSheet {
        constructor({ source = null, sprite = null, animation = null }) {
            this.source = source;
            this.sprite = sprite;
            this.animation = animation;
        }
        getAnimation(animName) {
            return this.animation.get(animName);
        }
        getAnimationFrame(animName, animFrame) {
            return this.animation.get(animName)[animFrame];
        }
        gtetSprite(id) {
            return this.sprite.get(id);
        }
        getSpriteImage(sprite) {
            return this.source.get(sprite.source);
        }
        static loadImage(imageSrc) {
            return __awaiter(this, void 0, void 0, function* () {
                return new Promise((resolve, reject) => {
                    const img = new Image();
                    img.src = imageSrc;
                    img.onload = () => {
                        resolve(img);
                    };
                    img.onerror = () => {
                        reject(imageSrc + "のロードに失敗しました。");
                    };
                });
            });
        }
        static Create(ss, loadStartCallback, loadEndCallback) {
            return __awaiter(this, void 0, void 0, function* () {
                const source = new Map();
                const sprite = new Map();
                const animation = new Map();
                {
                    const keys = Object.keys(ss.source);
                    for (let i = 0; i < keys.length; i++) {
                        const key = ~~keys[i];
                        const imageSrc = ss.source[key];
                        loadStartCallback();
                        const image = yield SpriteSheet.loadImage(imageSrc);
                        loadEndCallback();
                        source.set(key, image);
                    }
                }
                {
                    const keys = Object.keys(ss.sprite);
                    for (let i = 0; i < keys.length; i++) {
                        const key = ~~keys[i];
                        sprite.set(key, ss.sprite[key]);
                    }
                }
                {
                    const keys = Object.keys(ss.animation);
                    for (let i = 0; i < keys.length; i++) {
                        const key = keys[i];
                        animation.set(key, ss.animation[key]);
                    }
                }
                return new SpriteSheet({ source: source, sprite: sprite, animation: animation });
            });
        }
    }
    SpriteAnimation.SpriteSheet = SpriteSheet;
})(SpriteAnimation || (SpriteAnimation = {}));
/// <reference path="../SpriteAnimation.ts" />
var Scene;
(function (Scene) {
    let DrawMode;
    (function (DrawMode) {
        DrawMode[DrawMode["Normal"] = 0] = "Normal";
        DrawMode[DrawMode["Selected"] = 1] = "Selected";
        DrawMode[DrawMode["Disable"] = 2] = "Disable";
    })(DrawMode || (DrawMode = {}));
    class StatusSprite extends SpriteAnimation.Animator {
        constructor(data) {
            super(data.config.sprite);
            this.data = data;
        }
    }
    function drawStatusSprite(charactorData, drawMode, left, top, width, height, anim) {
        if (drawMode == DrawMode.Selected) {
            Game.getScreen().fillStyle = `rgb(24,196,195)`;
        }
        else if (drawMode == DrawMode.Disable) {
            Game.getScreen().fillStyle = `rgb(133,133,133)`;
        }
        else {
            Game.getScreen().fillStyle = `rgb(24,133,196)`;
        }
        Game.getScreen().fillRect(left - 0.5, top + 1 - 0.5, width, height - 2);
        Game.getScreen().strokeStyle = `rgb(12,34,98)`;
        Game.getScreen().lineWidth = 1;
        Game.getScreen().strokeRect(left - 0.5, top + 1 - 0.5, width, height - 2);
        if (charactorData != null) {
            charactorData.setDir(2);
            charactorData.setAnimation("move", anim / 1000);
            const animFrame = charactorData.spriteSheet.getAnimationFrame(charactorData.animName, charactorData.animFrame);
            const sprite = charactorData.spriteSheet.gtetSprite(animFrame.sprite);
            // キャラクター
            Game.getScreen().drawImage(charactorData.spriteSheet.getSpriteImage(sprite), sprite.left, sprite.top, sprite.width, sprite.height, left - 4, top, sprite.width, sprite.height);
            Game.getScreen().font = "10px 'PixelMplus10-Regular'";
            Game.getScreen().fillStyle = `rgb(255,255,255)`;
            Game.getScreen().textAlign = "left";
            Game.getScreen().textBaseline = "top";
            Game.getScreen().fillText(charactorData.data.config.name, left + 48 - 8, top + 3 + 12 * 0);
            Game.getScreen().fillText(`HP:${charactorData.data.hp} MP:${charactorData.data.mp}`, left + 48 - 8, top + 3 + 12 * 1);
            Game.getScreen().fillText(`ATK:${charactorData.data.equips.reduce((s, [v, k]) => s + (v == null ? 0 : Data.Item.findItemDataById(v.id).atk), 0)} DEF:${charactorData.data.equips.reduce((s, [v, k]) => s + (v == null ? 0 : Data.Item.findItemDataById(v.id).def), 0)}`, left + 48 - 8, top + 12 * 2);
        }
    }
    function* organization(saveData) {
        const dispatcher = new Game.GUI.UIDispatcher();
        const caption = new Game.GUI.TextBox({
            left: 1,
            top: 1,
            width: 250,
            height: 42,
            text: "編成\n迷宮探索時の前衛と後衛を選択してください。",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.add(caption);
        const btnExit = new Game.GUI.Button({
            left: 8,
            top: 16 * 11 + 46,
            width: 112,
            height: 16,
            text: "戻る",
        });
        dispatcher.add(btnExit);
        let exitScene = false;
        btnExit.click = (x, y) => {
            exitScene = true;
            saveData.saveGameData();
            Game.getSound().reqPlayChannel("cursor");
        };
        const charactors = Data.Charactor.getPlayerIds().map(x => new StatusSprite(saveData.findCharactorById(x)));
        let team = [Data.Charactor.getPlayerIds().findIndex(x => x == saveData.forwardCharactor), Data.Charactor.getPlayerIds().findIndex(x => x == saveData.backwardCharactor)];
        let selectedSide = -1;
        let selectedCharactor = -1;
        let anim = 0;
        const charactorListBox = new Game.GUI.ListBox({
            left: 131,
            top: 46,
            width: 112,
            height: 4 * 48,
            lineHeight: 48,
            getItemCount: () => charactors.length,
            drawItem: (left, top, width, height, index) => {
                drawStatusSprite(charactors[index], team.includes(index) ? DrawMode.Disable : (selectedCharactor == index) ? DrawMode.Selected : DrawMode.Normal, left, top, width, height, anim);
            }
        });
        dispatcher.add(charactorListBox);
        charactorListBox.click = (x, y) => {
            const select = charactorListBox.getItemIndexByPosition(x, y);
            if (team.includes(select)) {
                return;
            }
            selectedCharactor = selectedCharactor == select ? null : select;
            Game.getSound().reqPlayChannel("cursor");
        };
        const forwardBtn = new Game.GUI.ImageButton({
            left: 8,
            top: 46,
            width: 112,
            height: 48,
            texture: null,
            texLeft: 0,
            texTop: 0,
            texWidth: 0,
            texHeight: 0
        });
        forwardBtn.draw = () => {
            Game.getScreen().fillStyle = `rgb(24,133,196)`;
            Game.getScreen().fillRect(forwardBtn.left - 0.5, forwardBtn.top - 0.5, forwardBtn.width, 13);
            Game.getScreen().strokeStyle = `rgb(12,34,98)`;
            Game.getScreen().lineWidth = 1;
            Game.getScreen().strokeRect(forwardBtn.left - 0.5, forwardBtn.top - 0.5, forwardBtn.width, 13);
            Game.getScreen().font = "10px 'PixelMplus10-Regular'";
            Game.getScreen().fillStyle = `rgb(255,255,255)`;
            Game.getScreen().textAlign = "left";
            Game.getScreen().textBaseline = "top";
            Game.getScreen().fillText("前衛", forwardBtn.left + 1, forwardBtn.top + 1);
            drawStatusSprite(team[0] == -1 ? null : charactors[team[0]], selectedSide == 0 ? DrawMode.Selected : DrawMode.Normal, forwardBtn.left, forwardBtn.top + 12, forwardBtn.width, 48, anim);
        };
        forwardBtn.click = (x, y) => {
            selectedSide = selectedSide == 0 ? -1 : 0;
            Game.getSound().reqPlayChannel("cursor");
        };
        dispatcher.add(forwardBtn);
        const backwordBtn = new Game.GUI.ImageButton({
            left: 8,
            top: 46 + 70,
            width: 112,
            height: 60,
            texture: null,
            texLeft: 0,
            texTop: 0,
            texWidth: 0,
            texHeight: 0
        });
        backwordBtn.draw = () => {
            Game.getScreen().fillStyle = `rgb(24,133,196)`;
            Game.getScreen().fillRect(backwordBtn.left - 0.5, backwordBtn.top - 0.5, backwordBtn.width, 13);
            Game.getScreen().strokeStyle = `rgb(12,34,98)`;
            Game.getScreen().lineWidth = 1;
            Game.getScreen().strokeRect(backwordBtn.left - 0.5, backwordBtn.top - 0.5, backwordBtn.width, 13);
            Game.getScreen().font = "10px 'PixelMplus10-Regular'";
            Game.getScreen().fillStyle = `rgb(255,255,255)`;
            Game.getScreen().textAlign = "left";
            Game.getScreen().textBaseline = "top";
            Game.getScreen().fillText("後衛", backwordBtn.left + 1, backwordBtn.top + 1);
            drawStatusSprite(team[1] == -1 ? null : charactors[team[1]], selectedSide == 1 ? DrawMode.Selected : DrawMode.Normal, backwordBtn.left, backwordBtn.top + 12, backwordBtn.width, 48, anim);
        };
        backwordBtn.click = (x, y) => {
            selectedSide = selectedSide == 1 ? -1 : 1;
            Game.getSound().reqPlayChannel("cursor");
        };
        dispatcher.add(backwordBtn);
        this.draw = () => {
            Game.getScreen().drawImage(Game.getScreen().texture("classroom"), 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight, 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            dispatcher.draw();
        };
        yield (delta, ms) => {
            anim = ms % 1000;
            if (Game.getInput().isDown()) {
                dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isMove()) {
                dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isUp()) {
                dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (selectedSide != -1 && selectedCharactor != -1) {
                team[selectedSide] = selectedCharactor;
                selectedSide = -1;
                selectedCharactor = -1;
            }
            if (exitScene) {
                saveData.forwardCharactor = team[0] == -1 ? null : charactors[team[0]].data.id;
                saveData.backwardCharactor = team[1] == -1 ? null : charactors[team[1]].data.id;
                this.next();
            }
        };
        Game.getSceneManager().pop();
    }
    function* equipEdit(saveData) {
        const dispatcher = new Game.GUI.UIDispatcher();
        const caption = new Game.GUI.TextBox({
            left: 1,
            top: 1,
            width: 250,
            height: 42,
            text: "装備変更",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.add(caption);
        const btnExit = new Game.GUI.Button({
            left: 8,
            top: 16 * 11 + 46,
            width: 112,
            height: 16,
            text: "戻る",
        });
        dispatcher.add(btnExit);
        let exitScene = false;
        btnExit.click = (x, y) => {
            exitScene = true;
            saveData.saveGameData();
            Game.getSound().reqPlayChannel("cursor");
        };
        const charactors = Data.Charactor.getPlayerIds().map(x => new StatusSprite(saveData.findCharactorById(x)));
        let team = [Data.Charactor.getPlayerIds().findIndex(x => x == saveData.forwardCharactor), Data.Charactor.getPlayerIds().findIndex(x => x == saveData.backwardCharactor)];
        let selectedCharactor = -1;
        let selectedEquipPosition = -1;
        let anim = 0;
        const charactorListBox = new Game.GUI.ListBox({
            left: 131,
            top: 46,
            width: 112,
            height: 4 * 48,
            lineHeight: 48,
            getItemCount: () => charactors.length,
            drawItem: (left, top, width, height, index) => {
                drawStatusSprite(charactors[index], selectedCharactor == index ? DrawMode.Selected : DrawMode.Normal, left, top, width, height, anim);
            }
        });
        dispatcher.add(charactorListBox);
        charactorListBox.click = (x, y) => {
            const select = charactorListBox.getItemIndexByPosition(x, y);
            selectedCharactor = selectedCharactor == select ? null : select;
            Game.getSound().reqPlayChannel("cursor");
        };
        let selectedItem = -1;
        const itemLists = [];
        let updateItemList = () => {
            var newItemLists = saveData.ItemBox.map((x, i) => {
                if (x == null) {
                    return -1;
                }
                const itemData = Data.Item.findItemDataById(x.id);
                switch (selectedEquipPosition) {
                    case 0:
                        return (itemData.kind == Data.Item.ItemKind.Wepon) ? i : -1;
                    case 1:
                        return (itemData.kind == Data.Item.ItemKind.Armor1) ? i : -1;
                    case 2:
                        return (itemData.kind == Data.Item.ItemKind.Armor2) ? i : -1;
                    case 3:
                    case 4:
                        return (itemData.kind == Data.Item.ItemKind.Accessory) ? i : -1;
                    default:
                        return -1;
                }
            }).filter(x => x != -1);
            itemLists.length = 0;
            Array.prototype.push.apply(itemLists, newItemLists);
        };
        const itemListBox = new Game.GUI.ListBox({
            left: 131,
            top: 46,
            width: 112,
            height: 12 * 16,
            lineHeight: 16,
            getItemCount: () => itemLists.length,
            drawItem: (left, top, width, height, index) => {
                if (selectedItem == index) {
                    Game.getScreen().fillStyle = `rgb(24,196,195)`;
                }
                else {
                    Game.getScreen().fillStyle = `rgb(24,133,196)`;
                }
                Game.getScreen().fillRect(left - 0.5, top + 1 - 0.5, width, height - 2);
                Game.getScreen().strokeStyle = `rgb(12,34,98)`;
                Game.getScreen().lineWidth = 1;
                Game.getScreen().strokeRect(left - 0.5, top + 1 - 0.5, width, height - 2);
                Game.getScreen().font = "10px 'PixelMplus10-Regular'";
                Game.getScreen().fillStyle = `rgb(255,255,255)`;
                Game.getScreen().textAlign = "left";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(Data.Item.findItemDataById(saveData.ItemBox[itemLists[index]].id).name, left + 3, top + 3);
            }
        });
        dispatcher.add(itemListBox);
        itemListBox.click = (x, y) => {
            const select = itemListBox.getItemIndexByPosition(x, y);
            Game.getSound().reqPlayChannel("cursor");
            if (select == -1) {
                return;
            }
            switch (selectedEquipPosition) {
                case 0:
                    if (charactors[selectedCharactor].data.equips.wepon1 != null) {
                        const oldItem = charactors[selectedCharactor].data.equips.wepon1;
                        charactors[selectedCharactor].data.equips.wepon1 = saveData.ItemBox[itemLists[select]];
                        saveData.ItemBox[itemLists[select]] = oldItem;
                    }
                    else {
                        charactors[selectedCharactor].data.equips.wepon1 = saveData.ItemBox[itemLists[select]];
                        saveData.ItemBox.splice(itemLists[select], 1);
                    }
                    updateItemList();
                    break;
                case 1:
                    if (charactors[selectedCharactor].data.equips.armor1 != null) {
                        const oldItem = charactors[selectedCharactor].data.equips.armor1;
                        charactors[selectedCharactor].data.equips.armor1 = saveData.ItemBox[itemLists[select]];
                        saveData.ItemBox[itemLists[select]] = oldItem;
                    }
                    else {
                        charactors[selectedCharactor].data.equips.armor1 = saveData.ItemBox[itemLists[select]];
                        saveData.ItemBox.splice(itemLists[select], 1);
                    }
                    updateItemList();
                    break;
                case 2:
                    if (charactors[selectedCharactor].data.equips.armor2 != null) {
                        const oldItem = charactors[selectedCharactor].data.equips.armor2;
                        charactors[selectedCharactor].data.equips.armor2 = saveData.ItemBox[itemLists[select]];
                        saveData.ItemBox[itemLists[select]] = oldItem;
                    }
                    else {
                        charactors[selectedCharactor].data.equips.armor2 = saveData.ItemBox[itemLists[select]];
                        saveData.ItemBox.splice(itemLists[select], 1);
                    }
                    updateItemList();
                    break;
                case 3:
                    if (charactors[selectedCharactor].data.equips.accessory1 != null) {
                        const oldItem = charactors[selectedCharactor].data.equips.accessory1;
                        charactors[selectedCharactor].data.equips.accessory1 = saveData.ItemBox[itemLists[select]];
                        saveData.ItemBox[itemLists[select]] = oldItem;
                    }
                    else {
                        charactors[selectedCharactor].data.equips.accessory1 = saveData.ItemBox[itemLists[select]];
                        saveData.ItemBox.splice(itemLists[select], 1);
                    }
                    updateItemList();
                    break;
                case 4:
                    if (charactors[selectedCharactor].data.equips.accessory2 != null) {
                        const oldItem = charactors[selectedCharactor].data.equips.accessory2;
                        charactors[selectedCharactor].data.equips.accessory2 = saveData.ItemBox[itemLists[select]];
                        saveData.ItemBox[itemLists[select]] = oldItem;
                    }
                    else {
                        charactors[selectedCharactor].data.equips.accessory2 = saveData.ItemBox[itemLists[select]];
                        saveData.ItemBox.splice(itemLists[select], 1);
                    }
                    updateItemList();
                    break;
                default:
                    break;
            }
        };
        const statusViewBtn = new Game.GUI.ImageButton({
            left: 8,
            top: 46,
            width: 112,
            height: 48,
            texture: null,
            texLeft: 0,
            texTop: 0,
            texWidth: 0,
            texHeight: 0
        });
        statusViewBtn.draw = () => {
            drawStatusSprite(charactors[selectedCharactor], DrawMode.Normal, statusViewBtn.left, statusViewBtn.top, statusViewBtn.width, 48, anim);
        };
        statusViewBtn.click = () => {
            Game.getSound().reqPlayChannel("cursor");
            selectedEquipPosition = -1;
        };
        dispatcher.add(statusViewBtn);
        const btnWepon1 = new Game.GUI.Button({
            left: 8,
            top: 16 * 0 + 46 + 50,
            width: 112,
            height: 16,
            text: () => (selectedCharactor == -1 || charactors[selectedCharactor].data.equips.wepon1 == null) ? "(武器)" : Data.Item.findItemDataById(charactors[selectedCharactor].data.equips.wepon1.id).name,
        });
        dispatcher.add(btnWepon1);
        btnWepon1.click = () => {
            Game.getSound().reqPlayChannel("cursor");
            selectedEquipPosition = selectedEquipPosition == 0 ? -1 : 0;
            updateItemList();
        };
        const btnArmor1 = new Game.GUI.Button({
            left: 8,
            top: 16 * 1 + 46 + 50,
            width: 112,
            height: 16,
            text: () => (selectedCharactor == -1 || charactors[selectedCharactor].data.equips.armor1 == null) ? "(防具・上半身)" : Data.Item.findItemDataById(charactors[selectedCharactor].data.equips.armor1.id).name,
        });
        dispatcher.add(btnArmor1);
        btnArmor1.click = () => {
            Game.getSound().reqPlayChannel("cursor");
            selectedEquipPosition = selectedEquipPosition == 1 ? -1 : 1;
            updateItemList();
        };
        const btnArmor2 = new Game.GUI.Button({
            left: 8,
            top: 16 * 2 + 46 + 50,
            width: 112,
            height: 16,
            text: () => (selectedCharactor == -1 || charactors[selectedCharactor].data.equips.armor2 == null) ? "(防具・下半身)" : Data.Item.findItemDataById(charactors[selectedCharactor].data.equips.armor2.id).name,
        });
        dispatcher.add(btnArmor2);
        btnArmor2.click = () => {
            Game.getSound().reqPlayChannel("cursor");
            selectedEquipPosition = selectedEquipPosition == 2 ? -1 : 2;
            updateItemList();
        };
        const btnAccessory1 = new Game.GUI.Button({
            left: 8,
            top: 16 * 3 + 46 + 50,
            width: 112,
            height: 16,
            text: () => (selectedCharactor == -1 || charactors[selectedCharactor].data.equips.accessory1 == null) ? "(アクセサリ１)" : Data.Item.findItemDataById(charactors[selectedCharactor].data.equips.accessory1.id).name,
        });
        dispatcher.add(btnAccessory1);
        btnAccessory1.click = () => {
            Game.getSound().reqPlayChannel("cursor");
            selectedEquipPosition = selectedEquipPosition == 3 ? -1 : 3;
            updateItemList();
        };
        const btnAccessory2 = new Game.GUI.Button({
            left: 8,
            top: 16 * 4 + 46 + 50,
            width: 112,
            height: 16,
            text: () => (selectedCharactor == -1 || charactors[selectedCharactor].data.equips.accessory2 == null) ? "(アクセサリ２)" : Data.Item.findItemDataById(charactors[selectedCharactor].data.equips.accessory2.id).name,
        });
        dispatcher.add(btnAccessory2);
        btnAccessory2.click = () => {
            Game.getSound().reqPlayChannel("cursor");
            selectedEquipPosition = selectedEquipPosition == 4 ? -1 : 4;
            updateItemList();
        };
        this.draw = () => {
            Game.getScreen().drawImage(Game.getScreen().texture("classroom"), 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight, 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            dispatcher.draw();
        };
        itemListBox.visible = selectedEquipPosition != -1;
        charactorListBox.visible = selectedEquipPosition == -1;
        btnWepon1.visible = btnArmor1.visible = btnArmor2.visible = btnAccessory1.visible = btnAccessory2.visible = selectedCharactor != -1;
        yield (delta, ms) => {
            anim = ms % 1000;
            if (Game.getInput().isDown()) {
                dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isMove()) {
                dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isUp()) {
                dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
            }
            itemListBox.visible = selectedEquipPosition != -1;
            charactorListBox.visible = selectedEquipPosition == -1;
            btnWepon1.visible = btnArmor1.visible = btnArmor2.visible = btnAccessory1.visible = btnAccessory2.visible = selectedCharactor != -1;
            if (exitScene) {
                this.next();
            }
        };
        Game.getSceneManager().pop();
    }
    function* classroom(saveData) {
        const fade = new Scene.Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
        const dispatcher = new Game.GUI.UIDispatcher();
        const caption = new Game.GUI.TextBox({
            left: 1,
            top: 1,
            width: 250,
            height: 42,
            text: "教室\n探索ペアの編成や生徒の確認ができます。",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.add(caption);
        const btnOrganization = new Game.GUI.Button({
            left: 8,
            top: 20 * 0 + 46,
            width: 112,
            height: 16,
            text: "編成",
        });
        dispatcher.add(btnOrganization);
        btnOrganization.click = (x, y) => {
            Game.getSceneManager().push(organization, saveData);
            Game.getSound().reqPlayChannel("cursor");
        };
        const btnEquip = new Game.GUI.Button({
            left: 8,
            top: 20 * 1 + 46,
            width: 112,
            height: 16,
            text: "装備変更",
        });
        dispatcher.add(btnEquip);
        btnEquip.click = (x, y) => {
            Game.getSceneManager().push(equipEdit, saveData);
            Game.getSound().reqPlayChannel("cursor");
        };
        const btnItemBox = new Game.GUI.Button({
            left: 8,
            top: 20 * 2 + 46,
            width: 112,
            height: 16,
            text: "道具箱",
            visible: false
        });
        dispatcher.add(btnItemBox);
        btnItemBox.click = (x, y) => {
            Game.getSound().reqPlayChannel("cursor");
        };
        const btnExit = new Game.GUI.Button({
            left: 8,
            top: 16 * 11 + 46,
            width: 112,
            height: 16,
            text: "戻る",
        });
        dispatcher.add(btnExit);
        let exitScene = false;
        btnExit.click = (x, y) => {
            exitScene = true;
            saveData.saveGameData();
            Game.getSound().reqPlayChannel("cursor");
        };
        this.draw = () => {
            Game.getScreen().drawImage(Game.getScreen().texture("classroom"), 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight, 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            dispatcher.draw();
            fade.draw();
        };
        yield Scene.waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeIn(); },
            update: (e) => { fade.update(e); },
            end: () => {
                fade.stop();
                this.next();
            },
        });
        yield (delta, ms) => {
            if (Game.getInput().isDown()) {
                dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isMove()) {
                dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isUp()) {
                dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (exitScene) {
                this.next();
            }
        };
        yield Scene.waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeOut(); },
            update: (e) => { fade.update(e); },
            end: () => {
                fade.stop();
                this.next();
            },
        });
        Game.getSceneManager().pop();
    }
    Scene.classroom = classroom;
})(Scene || (Scene = {}));
/// <reference path="../lib/game/eventdispatcher.ts" />
var Scene;
(function (Scene) {
    function* corridor(saveData) {
        const dispatcher = new Game.GUI.UIDispatcher();
        const fade = new Scene.Fade(Game.getScreen().offscreenHeight, Game.getScreen().offscreenHeight);
        let selected = null;
        const caption = new Game.GUI.TextBox({
            left: 1,
            top: 1,
            width: 250,
            height: 42,
            text: "移動先を選択してください。",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.add(caption);
        const btnBuy = new Game.GUI.Button({
            left: 8,
            top: 20 * 0 + 46,
            width: 112,
            height: 16,
            text: "教室",
        });
        dispatcher.add(btnBuy);
        btnBuy.click = (x, y) => {
            Game.getSound().reqPlayChannel("cursor");
            selected = () => Game.getSceneManager().push(Scene.classroom, saveData);
        };
        const btnSell = new Game.GUI.Button({
            left: 8,
            top: 20 * 1 + 46,
            width: 112,
            height: 16,
            text: "購買部",
        });
        dispatcher.add(btnSell);
        btnSell.click = (x, y) => {
            Game.getSound().reqPlayChannel("cursor");
            selected = () => Game.getSceneManager().push(Scene.shop, saveData);
        };
        const btnDungeon = new Game.GUI.Button({
            left: 8,
            top: 20 * 3 + 46,
            width: 112,
            height: 16,
            text: "迷宮",
        });
        dispatcher.add(btnDungeon);
        btnDungeon.click = (x, y) => {
            Game.getSound().reqPlayChannel("cursor");
            Game.getSound().reqStopChannel("classroom");
            selected = () => {
                Game.getSceneManager().pop();
                Game.getSound().reqStopChannel("classroom");
                Game.getSceneManager().push(Scene.dungeon, { saveData: saveData, player: new Unit.Player(saveData.findCharactorById(saveData.forwardCharactor), saveData.findCharactorById(saveData.backwardCharactor)), floor: 1 });
            };
        };
        btnDungeon.enable = saveData.forwardCharactor != null;
        this.draw = () => {
            Game.getScreen().drawImage(Game.getScreen().texture("corridorbg"), 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight, 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            dispatcher.draw();
            fade.draw();
        };
        Game.getSound().reqPlayChannel("classroom", true);
        for (;;) {
            yield Scene.waitTimeout({
                timeout: 500,
                init: () => { fade.startFadeIn(); },
                update: (e) => { fade.update(e); },
                end: () => {
                    fade.stop();
                    this.next();
                },
            });
            yield (delta, ms) => {
                if (Game.getInput().isDown()) {
                    dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
                }
                if (Game.getInput().isMove()) {
                    dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
                }
                if (Game.getInput().isUp()) {
                    dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
                }
                btnDungeon.enable = saveData.forwardCharactor != null;
                if (selected != null) {
                    this.next();
                }
            };
            yield Scene.waitTimeout({
                timeout: 500,
                init: () => { fade.startFadeOut(); },
                update: (e) => { fade.update(e); },
                end: () => {
                    fade.stop();
                    this.next();
                },
            });
            selected();
            selected = null;
        }
    }
    Scene.corridor = corridor;
})(Scene || (Scene = {}));
var Scene;
(function (Scene) {
    let TurnState;
    (function (TurnState) {
        TurnState[TurnState["WaitInput"] = 0] = "WaitInput";
        TurnState[TurnState["PlayerAction"] = 1] = "PlayerAction";
        TurnState[TurnState["PlayerActionRunning"] = 2] = "PlayerActionRunning";
        TurnState[TurnState["EnemyAI"] = 3] = "EnemyAI";
        TurnState[TurnState["EnemyAction"] = 4] = "EnemyAction";
        TurnState[TurnState["EnemyActionRunning"] = 5] = "EnemyActionRunning";
        TurnState[TurnState["EnemyDead"] = 6] = "EnemyDead";
        TurnState[TurnState["EnemyDeadRunning"] = 7] = "EnemyDeadRunning";
        TurnState[TurnState["Move"] = 8] = "Move";
        TurnState[TurnState["MoveRunning"] = 9] = "MoveRunning";
        TurnState[TurnState["TurnEnd"] = 10] = "TurnEnd";
    })(TurnState || (TurnState = {}));
    function* dungeon(param) {
        const player = param.player;
        const floor = param.floor;
        // マップサイズ算出
        const mapChipW = 30 + floor * 3;
        const mapChipH = 30 + floor * 3;
        // マップ自動生成
        const mapchipsL1 = new Array2D(mapChipW, mapChipH);
        const layout = Dungeon.generate(mapChipW, mapChipH, (x, y, v) => { mapchipsL1.value(x, y, v ? 0 : 1); });
        // 装飾
        for (let y = 1; y < mapChipH; y++) {
            for (let x = 0; x < mapChipW; x++) {
                mapchipsL1.value(x, y - 1, mapchipsL1.value(x, y) === 1 && mapchipsL1.value(x, y - 1) === 0
                    ? 2
                    : mapchipsL1.value(x, y - 1));
            }
        }
        const mapchipsL2 = new Array2D(mapChipW, mapChipH);
        for (let y = 0; y < mapChipH; y++) {
            for (let x = 0; x < mapChipW; x++) {
                mapchipsL2.value(x, y, (mapchipsL1.value(x, y) === 0) ? 0 : 1);
            }
        }
        // 部屋は生成後にシャッフルしているのでそのまま取り出す
        const rooms = layout.rooms.slice();
        // 開始位置
        const startPos = rooms[0].getCenter();
        player.x = startPos.x;
        player.y = startPos.y;
        // 階段位置
        const stairsPos = rooms[1].getCenter();
        mapchipsL1.value(stairsPos.x, stairsPos.y, 10);
        // モンスター配置
        let monsters = rooms.splice(2).map((x) => {
            var monster = new Unit.Monster("slime");
            monster.x = x.getLeft();
            monster.y = x.getTop();
            monster.life = monster.maxLife = floor + 5;
            monster.atk = ~~(floor * 2);
            monster.def = ~~(floor / 3) + 1;
            return monster;
        });
        const map = new Dungeon.DungeonData({
            width: mapChipW,
            height: mapChipW,
            gridsize: { width: 24, height: 24 },
            layer: {
                0: {
                    texture: "mapchip",
                    chip: {
                        1: { x: 48, y: 0 },
                        2: { x: 96, y: 96 },
                        10: { x: 96, y: 0 },
                    },
                    chips: mapchipsL1,
                },
                1: {
                    texture: "mapchip",
                    chip: {
                        0: { x: 96, y: 72 },
                    },
                    chips: mapchipsL2,
                },
            },
        });
        // カメラを更新
        map.update({
            viewpoint: {
                x: (player.x * map.gridsize.width + player.offx) + map.gridsize.width / 2,
                y: (player.y * map.gridsize.height + player.offy) + map.gridsize.height / 2,
            },
            viewwidth: Game.getScreen().offscreenWidth,
            viewheight: Game.getScreen().offscreenHeight,
        });
        Game.getSound().reqPlayChannel("dungeon", true);
        // assign virtual pad
        const pad = new Game.Input.VirtualStick();
        const pointerdown = (ev) => {
            if (pad.onpointingstart(ev.pointerId)) {
                const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
                pad.x = pos[0];
                pad.y = pos[1];
            }
        };
        const pointermove = (ev) => {
            const pos = Game.getScreen().pagePointToScreenPoint(ev.pageX, ev.pageY);
            pad.onpointingmove(ev.pointerId, pos[0], pos[1]);
        };
        const pointerup = (ev) => {
            pad.onpointingend(ev.pointerId);
        };
        const onPointerHook = () => {
            Game.getInput().on("pointerdown", pointerdown);
            Game.getInput().on("pointermove", pointermove);
            Game.getInput().on("pointerup", pointerup);
            Game.getInput().on("pointerleave", pointerup);
        };
        const offPointerHook = () => {
            Game.getInput().off("pointerdown", pointerdown);
            Game.getInput().off("pointermove", pointermove);
            Game.getInput().off("pointerup", pointerup);
            Game.getInput().off("pointerleave", pointerup);
        };
        this.suspend = () => {
            offPointerHook();
            Game.getSound().reqStopChannel("dungeon");
        };
        this.resume = () => {
            onPointerHook();
            Game.getSound().reqPlayChannel("dungeon", true);
        };
        this.leave = () => {
            offPointerHook();
            Game.getSound().reqStopChannel("dungeon");
        };
        const updateLighting = (iswalkable) => {
            map.clearLighting();
            PathFinder.calcDistanceByDijkstra({
                array2D: map.layer[0].chips,
                sx: player.x,
                sy: player.y,
                value: 140,
                costs: (v) => iswalkable(v) ? 20 : 50,
                output: (x, y, v) => {
                    map.lighting.value(x, y, v);
                    if (map.visibled.value(x, y) < v) {
                        map.visibled.value(x, y, v);
                    }
                },
            });
        };
        const fade = new Scene.Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
        let particles = [];
        const dispatcher = new Game.GUI.UIDispatcher();
        const mapButton = new Game.GUI.ImageButton({
            left: 141 + 22 * 0,
            top: 0,
            width: 23,
            height: 19,
            texture: "menuicon",
            texLeft: 23 * 0,
            texTop: 0,
            texWidth: 23,
            texHeight: 19
        });
        dispatcher.add(mapButton);
        mapButton.click = (x, y) => {
            Game.getSceneManager().push(Scene.mapview, { map: map, player: player });
        };
        const itemButton = new Game.GUI.ImageButton({
            left: 141 + 22 * 1,
            top: 0,
            width: 23,
            height: 19,
            texture: "menuicon",
            texLeft: 23 * 1,
            texTop: 0,
            texWidth: 23,
            texHeight: 19
        });
        dispatcher.add(itemButton);
        itemButton.click = (x, y) => {
            //Game.getSceneManager().push(mapview, { map: map, player: player });
        };
        const equipButton = new Game.GUI.ImageButton({
            left: 141 + 22 * 2,
            top: 0,
            width: 23,
            height: 19,
            texture: "menuicon",
            texLeft: 23 * 2,
            texTop: 0,
            texWidth: 23,
            texHeight: 19
        });
        dispatcher.add(equipButton);
        equipButton.click = (x, y) => {
            //Game.getSceneManager().push(mapview, { map: map, player: player });
        };
        const statusButton = new Game.GUI.ImageButton({
            left: 141 + 22 * 3,
            top: 0,
            width: 23,
            height: 19,
            texture: "menuicon",
            texLeft: 23 * 3,
            texTop: 0,
            texWidth: 23,
            texHeight: 19
        });
        dispatcher.add(statusButton);
        statusButton.click = (x, y) => {
            Game.getSceneManager().push(statusView, { player: player, floor: floor, upperdraw: this.draw });
        };
        const otherButton = new Game.GUI.ImageButton({
            left: 141 + 22 * 4,
            top: 0,
            width: 23,
            height: 19,
            texture: "menuicon",
            texLeft: 23 * 4,
            texTop: 0,
            texWidth: 23,
            texHeight: 19
        });
        dispatcher.add(otherButton);
        otherButton.click = (x, y) => {
            //Game.getSceneManager().push(statusView, { player: player, floor:floor, upperdraw: this.draw });
        };
        this.draw = () => {
            Game.getScreen().save();
            Game.getScreen().fillStyle = "rgb(255,255,255)";
            Game.getScreen().fillRect(0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            map.draw((l, cameraLocalPx, cameraLocalPy) => {
                if (l === 0) {
                    // 影
                    Game.getScreen().fillStyle = "rgba(0,0,0,0.25)";
                    Game.getScreen().beginPath();
                    Game.getScreen().ellipse(cameraLocalPx, cameraLocalPy + 7, 12, 3, 0, 0, Math.PI * 2);
                    Game.getScreen().fill();
                    // モンスター
                    const camera = map.camera;
                    monsters.forEach((monster) => {
                        const xx = monster.x - camera.chipLeft;
                        const yy = monster.y - camera.chipTop;
                        if ((0 <= xx && xx < Game.getScreen().offscreenWidth / 24) &&
                            (0 <= yy && yy < Game.getScreen().offscreenHeight / 24)) {
                            const animFrame = monster.spriteSheet.getAnimationFrame(monster.animName, monster.animFrame);
                            const sprite = monster.spriteSheet.gtetSprite(animFrame.sprite);
                            const dx = xx * map.gridsize.width +
                                camera.chipOffX +
                                monster.offx +
                                sprite.offsetX +
                                animFrame.offsetX;
                            const dy = yy * map.gridsize.height +
                                camera.chipOffY +
                                monster.offy +
                                sprite.offsetY +
                                animFrame.offsetY;
                            Game.getScreen().drawImage(monster.spriteSheet.getSpriteImage(sprite), sprite.left, sprite.top, sprite.width, sprite.height, dx, dy, sprite.width, sprite.height);
                        }
                    });
                    {
                        const animFrame = player.spriteSheet.getAnimationFrame(player.animName, player.animFrame);
                        const sprite = player.spriteSheet.gtetSprite(animFrame.sprite);
                        // キャラクター
                        Game.getScreen().drawImage(player.spriteSheet.getSpriteImage(sprite), sprite.left, sprite.top, sprite.width, sprite.height, cameraLocalPx - sprite.width / 2 + /*player.offx + */ sprite.offsetX + animFrame.offsetX, cameraLocalPy - sprite.height / 2 + /*player.offy + */ sprite.offsetY + animFrame.offsetY, sprite.width, sprite.height);
                    }
                }
                if (l === 1) {
                    // インフォメーションの描画
                    // モンスター体力
                    const camera = map.camera;
                    monsters.forEach((monster) => {
                        const xx = monster.x - camera.chipLeft;
                        const yy = monster.y - camera.chipTop;
                        if ((0 <= xx && xx < Game.getScreen().offscreenWidth / 24) &&
                            (0 <= yy && yy < Game.getScreen().offscreenHeight / 24)) {
                            const animFrame = monster.spriteSheet.getAnimationFrame(monster.animName, monster.animFrame);
                            const sprite = monster.spriteSheet.gtetSprite(animFrame.sprite);
                            const dx = xx * map.gridsize.width +
                                camera.chipOffX +
                                monster.offx +
                                sprite.offsetX +
                                animFrame.offsetX;
                            const dy = yy * map.gridsize.height +
                                camera.chipOffY +
                                monster.offy +
                                sprite.offsetY +
                                animFrame.offsetY;
                            Game.getScreen().fillStyle = 'rgb(255,0,0)';
                            Game.getScreen().fillRect(dx, dy + sprite.height - 1, map.gridsize.width, 1);
                            Game.getScreen().fillStyle = 'rgb(0,255,0)';
                            Game.getScreen().fillRect(dx, dy + sprite.height - 1, ~~(map.gridsize.width * monster.life / monster.maxLife), 1);
                        }
                    });
                    {
                        const animFrame = player.spriteSheet.getAnimationFrame(player.animName, player.animFrame);
                        const sprite = player.spriteSheet.gtetSprite(animFrame.sprite);
                        // キャラクター体力
                        Game.getScreen().fillStyle = 'rgb(255,0,0)';
                        Game.getScreen().fillRect(cameraLocalPx - map.gridsize.width / 2 + /*player.offx + */ sprite.offsetX + animFrame.offsetX, cameraLocalPy - sprite.height / 2 + /*player.offy + */ sprite.offsetY + animFrame.offsetY + sprite.height - 1, map.gridsize.width, 1);
                        Game.getScreen().fillStyle = 'rgb(0,255,0)';
                        Game.getScreen().fillRect(cameraLocalPx - map.gridsize.width / 2 + /*player.offx + */ sprite.offsetX + animFrame.offsetX, cameraLocalPy - sprite.height / 2 + /*player.offy + */ sprite.offsetY + animFrame.offsetY + sprite.height - 1, ~~(map.gridsize.width * player.getForward().hp / player.getForward().hpMax), 1);
                    }
                }
            });
            // スプライト
            particles.forEach((x) => x.draw(map.camera));
            // 情報
            Font7px.draw7pxFont(`     | HP:${player.getForward().hp}/${player.getForward().hpMax}`, 0, 6 * 0);
            Font7px.draw7pxFont(`${('   ' + floor).substr(-3)}F | MP:${player.getForward().mp}/${player.getForward().mpMax}`, 0, 6 * 1);
            Font7px.draw7pxFont(`     | GOLD:${param.saveData.Money}`, 0, 6 * 2);
            //menuicon
            // UI
            dispatcher.draw();
            // フェード
            fade.draw();
            Game.getScreen().restore();
            // バーチャルジョイスティックの描画
            if (pad.isTouching) {
                Game.getScreen().fillStyle = "rgba(255,255,255,0.25)";
                Game.getScreen().beginPath();
                Game.getScreen().ellipse(pad.x, pad.y, pad.radius * 1.2, pad.radius * 1.2, 0, 0, Math.PI * 2);
                Game.getScreen().fill();
                Game.getScreen().beginPath();
                Game.getScreen().ellipse(pad.x + pad.cx, pad.y + pad.cy, pad.radius, pad.radius, 0, 0, Math.PI * 2);
                Game.getScreen().fill();
            }
        };
        yield Scene.waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeIn(); },
            update: (e) => {
                fade.update(e);
                updateLighting((v) => v === 1 || v === 10);
            },
            end: () => { this.next(); },
        });
        onPointerHook();
        // ターンの状態（フェーズ）
        const turnContext = {
            ms: 0,
            pad: pad,
            player: player,
            monsters: monsters,
            map: map,
            tactics: {
                player: {},
                monsters: []
            },
            sprites: particles,
            scene: this,
        };
        const turnStateStack = [];
        turnStateStack.unshift(WaitInput(turnStateStack, turnContext));
        let playerTactics = {};
        const monstersTactics = [];
        yield (delta, ms) => {
            // ターン進行
            turnContext.ms = ms;
            while (turnStateStack[0].next().done) { }
            // カメラを更新
            map.update({
                viewpoint: {
                    x: (player.x * map.gridsize.width + player.offx) + map.gridsize.width / 2,
                    y: (player.y * map.gridsize.height + player.offy) + map.gridsize.height / 2,
                },
                viewwidth: Game.getScreen().offscreenWidth,
                viewheight: Game.getScreen().offscreenHeight
            });
            // スプライトを更新
            particles.removeIf((x) => x.update(delta, ms));
            updateLighting((v) => v === 1 || v === 10);
            if (player.getForward().hp === 0) {
                if (player.getBackward().hp !== 0) {
                    player.active = player.active == 0 ? 1 : 0;
                }
                else {
                    // ターン強制終了
                    Game.getSceneManager().pop();
                    Game.getSceneManager().push(gameOver, { saveData: param.saveData, player: player, floor: floor, upperdraw: this.draw });
                    return;
                }
            }
            // ui 
            if (Game.getInput().isDown()) {
                dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isMove()) {
                dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isUp()) {
                dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
            }
            //if (Game.getInput().isClick() &&
            //    Game.getScreen().pagePointContainScreen(Game.getInput().pageX, Game.getInput().pageY)) {
            //    //Game.getSceneManager().push(mapview, { map: map, player: player });
            //    Game.getSceneManager().push(statusView, { player: player, floor: floor, upperdraw: this.draw });
            //}
        };
        Game.getSound().reqPlayChannel("kaidan");
        yield Scene.waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeOut(); },
            update: (e) => {
                fade.update(e);
                updateLighting((v) => v === 1 || v === 10);
            },
            end: () => { this.next(); },
        });
        yield Scene.waitTimeout({
            timeout: 500,
            end: () => { this.next(); },
        });
        Game.getSceneManager().pop();
        Game.getSceneManager().push(dungeon, { saveData: param.saveData, player: player, floor: floor + 1 });
    }
    Scene.dungeon = dungeon;
    ;
    function* WaitInput(turnStateStack, context) {
        for (;;) {
            // キー入力待ち
            if (context.pad.isTouching === false || context.pad.distance <= 0.4) {
                context.player.setAnimation("move", 0);
                yield;
                continue;
            }
            // キー入力された
            const playerMoveDir = context.pad.dir8;
            // 「行動(Action)」と「移動(Move)」の識別を行う
            // 移動先が侵入不可能の場合は待機とする
            const { x, y } = Array2D.DIR8[playerMoveDir];
            if (context.map.layer[0].chips.value(context.player.x + x, context.player.y + y) !== 1 &&
                context.map.layer[0].chips.value(context.player.x + x, context.player.y + y) !== 10) {
                context.player.setDir(playerMoveDir);
                yield;
                continue;
            }
            turnStateStack.shift();
            // 移動先に敵がいる場合は「行動(Action)」、いない場合は「移動(Move)」
            const targetMonster = context.monsters.findIndex((monster) => (monster.x === context.player.x + x) && (monster.y === context.player.y + y));
            if (targetMonster !== -1) {
                // 移動先に敵がいる＝「行動(Action)」
                context.tactics.player = {
                    type: "action",
                    moveDir: playerMoveDir,
                    targetMonster: targetMonster,
                    startTime: 0,
                    actionTime: 250,
                };
                // プレイヤーの行動、敵の行動の決定、敵の行動処理、移動実行の順で行う
                turnStateStack.unshift(PlayerAction(turnStateStack, context), EnemyAI(turnStateStack, context), EnemyAction(turnStateStack, context), Move(turnStateStack, context), TurnEnd(turnStateStack, context));
                return;
            }
            else {
                // 移動先に敵はいない＝「移動(Move)」
                context.tactics.player = {
                    type: "move",
                    moveDir: playerMoveDir,
                    startTime: 0,
                    actionTime: 250,
                };
                // 敵の行動の決定、移動実行、敵の行動処理、の順で行う。
                turnStateStack.unshift(EnemyAI(turnStateStack, context), Move(turnStateStack, context), EnemyAction(turnStateStack, context), TurnEnd(turnStateStack, context));
                return;
            }
        }
    }
    function* PlayerAction(turnStateStack, context) {
        // プレイヤーの行動
        const startTime = context.ms;
        context.player.setDir(context.tactics.player.moveDir);
        context.player.setAnimation("action", 0);
        let acted = false;
        for (;;) {
            const rate = (context.ms - startTime) / context.tactics.player.actionTime;
            context.player.setAnimation("action", rate);
            if (rate >= 0.5 && acted == false) {
                acted = true;
                const targetMonster = context.monsters[context.tactics.player.targetMonster];
                Game.getSound().reqPlayChannel("atack");
                const dmg = ~~(context.player.atk - targetMonster.def);
                context.sprites.push(Particle.createShowDamageSprite(context.ms, dmg > 0 ? ("" + dmg) : "MISS!!", () => {
                    return {
                        x: targetMonster.offx + targetMonster.x * context.map.gridsize.width + context.map.gridsize.width / 2,
                        y: targetMonster.offy + targetMonster.y * context.map.gridsize.height + context.map.gridsize.height / 2
                    };
                }));
                if (targetMonster.life > 0 && dmg > 0) {
                    targetMonster.life -= dmg;
                    if (targetMonster.life <= 0) {
                        targetMonster.life = 0;
                        // 敵を死亡状態にする
                        // explosion
                        Game.getSound().reqPlayChannel("explosion");
                        // 死亡処理を割り込みで行わせる
                        turnStateStack.splice(1, 0, EnemyDead(turnStateStack, context, context.tactics.player.targetMonster));
                    }
                }
            }
            if (rate >= 1) {
                // プレイヤーの行動終了
                turnStateStack.shift();
                context.player.setAnimation("move", 0);
                return;
            }
            yield;
        }
    }
    function* EnemyDead(turnStateStack, context, enemyId) {
        // 敵の死亡
        const start = context.ms;
        Game.getSound().reqPlayChannel("explosion");
        context.monsters[enemyId].setAnimation("dead", 0);
        for (;;) {
            const diff = context.ms - start;
            context.monsters[enemyId].setAnimation("dead", diff / 250);
            if (diff >= 250) {
                turnStateStack.shift();
                return;
            }
            yield;
        }
    }
    function* EnemyAI(turnStateStack, context) {
        // 敵の行動の決定
        // 移動不可能地点を書き込む配列
        const cannotMoveMap = new Array2D(context.map.width, context.map.height, 0);
        context.tactics.monsters.length = context.monsters.length;
        context.tactics.monsters.fill(null);
        context.monsters.forEach((monster, i) => {
            // 敵全体の移動不能座標に自分を設定
            cannotMoveMap.value(monster.x, monster.y, 1);
        });
        // プレイヤーが移動する場合、移動先にいると想定して敵の行動を決定する
        let px = context.player.x;
        let py = context.player.y;
        if (context.tactics.player.type === "move") {
            const off = Array2D.DIR8[context.tactics.player.moveDir];
            px += off.x;
            py += off.y;
        }
        cannotMoveMap.value(px, py, 1);
        // 行動(Action)と移動(Move)は分離しないと移動で敵が重なる
        // 行動(Action)する敵を決定
        context.monsters.forEach((monster, i) => {
            if (monster.life <= 0) {
                // 死亡状態なので何もしない
                context.tactics.monsters[i] = {
                    type: "dead",
                    moveDir: 5,
                    startTime: 0,
                    actionTime: 250,
                };
                cannotMoveMap.value(monster.x, monster.y, 0);
                return;
            }
            const dx = px - monster.x;
            const dy = py - monster.y;
            if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) {
                // 移動先のプレイヤー位置は現在位置に隣接しているので、行動(Action)を選択
                const dir = Array2D.DIR8.findIndex((x) => x.x === dx && x.y === dy);
                // 移動不能座標に変化は無し
                context.tactics.monsters[i] = {
                    type: "action",
                    moveDir: dir,
                    startTime: 0,
                    actionTime: 250,
                };
                return;
            }
            else {
                return; // skip
            }
        });
        // 移動(Move)する敵の移動先を決定する
        // 最良の移動先にキャラクターが存在することを考慮して移動処理が発生しなくなるまで計算を繰り返す。
        let changed = true;
        while (changed) {
            changed = false;
            context.monsters.forEach((monster, i) => {
                const dx = px - monster.x;
                const dy = py - monster.y;
                if (Math.abs(dx) <= 1 && Math.abs(dy) <= 1) {
                    if (context.tactics.monsters[i] == null) {
                        console.error("Actionすべき敵の動作が決定していない");
                    }
                    return;
                }
                else if (context.tactics.monsters[i] == null) {
                    // 移動先のプレイヤー位置は現在位置に隣接していないので、移動(Move)を選択
                    // とりあえず軸合わせ戦略で動く
                    // 移動先の候補表から最良の移動先を選ぶ
                    const cands = [
                        [Math.sign(dx), Math.sign(dy)],
                        (Math.abs(dx) > Math.abs(dy)) ? [0, Math.sign(dy)] : [Math.sign(dx), 0],
                        (Math.abs(dx) > Math.abs(dy)) ? [Math.sign(dx), 0] : [0, Math.sign(dy)],
                    ];
                    for (let j = 0; j < cands.length; j++) {
                        const [cx, cy] = cands[j];
                        const tx = monster.x + cx;
                        const ty = monster.y + cy;
                        if ((cannotMoveMap.value(tx, ty) === 0) && (context.map.layer[0].chips.value(tx, ty) === 1 || context.map.layer[0].chips.value(tx, ty) === 10)) {
                            const dir = Array2D.DIR8.findIndex((x) => x.x === cx && x.y === cy);
                            // 移動不能座標を変更
                            cannotMoveMap.value(monster.x, monster.y, 0);
                            cannotMoveMap.value(tx, ty, 1);
                            context.tactics.monsters[i] = {
                                type: "move",
                                moveDir: dir,
                                startTime: 0,
                                actionTime: 250,
                            };
                            changed = true;
                            return;
                        }
                    }
                    // 移動先が全部移動不能だったので待機を選択
                    // 敵全体の移動不能座標に自分を設定
                    cannotMoveMap.value(monster.x, monster.y, 1);
                    context.tactics.monsters[i] = {
                        type: "idle",
                        moveDir: 5,
                        startTime: 0,
                        actionTime: 250,
                    };
                    changed = true;
                    return;
                }
            });
        }
        // 敵の行動の決定の終了
        turnStateStack.shift();
    }
    function* EnemyAction(turnStateStack, context) {
        // 敵の行動開始
        for (let enemyId = 0; enemyId < context.tactics.monsters.length; enemyId++) {
            if (context.tactics.monsters[enemyId].type !== "action") {
                continue;
            }
            context.tactics.monsters[enemyId].startTime = context.ms;
            context.monsters[enemyId].setDir(context.tactics.monsters[enemyId].moveDir);
            context.monsters[enemyId].setAnimation("action", 0);
            yield* EnemyDoAction(turnStateStack, context, enemyId);
        }
        // もう動かす敵がいない
        turnStateStack.shift();
        return;
    }
    function* EnemyDoAction(turnStateStack, context, enemyId) {
        const startTime = context.ms;
        let acted = false;
        for (;;) {
            const rate = (context.ms - startTime) / context.tactics.monsters[enemyId].actionTime;
            context.monsters[enemyId].setAnimation("action", rate);
            if (rate >= 0.5 && acted == false) {
                acted = true;
                Game.getSound().reqPlayChannel("atack");
                const dmg = ~~(context.monsters[enemyId].atk - context.player.def);
                context.sprites.push(Particle.createShowDamageSprite(context.ms, dmg > 0 ? ("" + dmg) : "MISS!!", () => {
                    return {
                        x: context.player.offx + context.player.x * context.map.gridsize.width + context.map.gridsize.width / 2,
                        y: context.player.offy + context.player.y * context.map.gridsize.height + context.map.gridsize.height / 2
                    };
                }));
                if (context.player.getForward().hp > 0 && dmg > 0) {
                    context.player.getForward().hp -= dmg;
                    if (context.player.getForward().hp <= 0) {
                        context.player.getForward().hp = 0;
                    }
                }
            }
            if (rate >= 1) {
                // 行動終了。次の敵へ
                context.monsters[enemyId].setAnimation("action", 0);
                return;
            }
            yield;
        }
    }
    function* Move(turnStateStack, context) {
        // 移動開始
        const start = context.ms;
        context.tactics.monsters.forEach((monsterTactic, i) => {
            if (monsterTactic.type === "move") {
                context.monsters[i].setDir(monsterTactic.moveDir);
                context.monsters[i].setAnimation("move", 0);
            }
        });
        if (context.tactics.player.type === "move") {
            context.player.setDir(context.tactics.player.moveDir);
            context.player.setAnimation("move", 0);
        }
        for (;;) {
            // 移動実行
            let finish = true;
            context.tactics.monsters.forEach((monsterTactic, i) => {
                if (monsterTactic == null) {
                    return;
                }
                if (monsterTactic.type === "move") {
                    const rate = (context.ms - start) / monsterTactic.actionTime;
                    context.monsters[i].setDir(monsterTactic.moveDir);
                    context.monsters[i].setAnimation("move", rate);
                    if (rate < 1) {
                        finish = false; // 行動終了していないフラグをセット
                    }
                    else {
                        context.monsters[i].x += Array2D.DIR8[monsterTactic.moveDir].x;
                        context.monsters[i].y += Array2D.DIR8[monsterTactic.moveDir].y;
                        context.monsters[i].offx = 0;
                        context.monsters[i].offy = 0;
                        context.monsters[i].setAnimation("move", 0);
                    }
                }
            });
            if (context.tactics.player.type === "move") {
                const rate = (context.ms - start) / context.tactics.player.actionTime;
                context.player.setDir(context.tactics.player.moveDir);
                context.player.setAnimation("move", rate);
                if (rate < 1) {
                    finish = false; // 行動終了していないフラグをセット
                }
                else {
                    context.player.x += Array2D.DIR8[context.tactics.player.moveDir].x;
                    context.player.y += Array2D.DIR8[context.tactics.player.moveDir].y;
                    context.player.offx = 0;
                    context.player.offy = 0;
                    context.player.setAnimation("move", 0);
                }
            }
            if (finish) {
                // 行動終了
                turnStateStack.shift();
                return;
            }
            yield;
        }
    }
    function* TurnEnd(turnStateStack, context) {
        turnStateStack.shift();
        // 死亡したモンスターを消去
        context.monsters.removeIf(x => x.life == 0);
        // 前衛はターン経過によるMP消費が発生する
        if (context.player.getForward().mp > 0) {
            context.player.getForward().mp -= 1;
            // HPが減少している場合はMPを消費してHPを回復
            if (context.player.getForward().hp < context.player.getForward().hpMax && context.player.getForward().mp > 0) {
                context.player.getForward().hp += 1;
                context.player.getForward().mp -= 1;
            }
        }
        else if (context.player.getForward().hp > 1) {
            // mpが無い場合はhpが減少
            context.player.getForward().hp -= 1;
        }
        // 後衛はターン経過によるMP消費が無い
        // HPが減少している場合はMPを消費してHPを回復
        if (context.player.getBackward().hp < context.player.getBackward().hpMax && context.player.getBackward().mp > 0) {
            context.player.getBackward().hp += 1;
            context.player.getBackward().mp -= 1;
        }
        // 現在位置のマップチップを取得
        const chip = context.map.layer[0].chips.value(~~context.player.x, ~~context.player.y);
        if (chip === 10) {
            // 階段なので次の階層に移動させる。
            context.scene.next("nextfloor");
            yield;
        }
        else {
            turnStateStack.unshift(WaitInput(turnStateStack, context));
        }
        return;
    }
    function showStatusText(str, x, y) {
        const fontWidth = 5;
        const fontHeight = 7;
        const len = str.length;
        for (let i = 0; i < str.length; i++) {
            const [fx, fy] = Font7px.charDic[str[i]];
            Game.getScreen().drawImage(Game.getScreen().texture("font7wpx"), fx, fy, fontWidth, fontHeight, (x + (i + 0) * (fontWidth - 1)), (y + (0) * fontHeight), fontWidth, fontHeight);
        }
    }
    function* statusView(opt) {
        var closeButton = {
            x: Game.getScreen().offscreenWidth - 20,
            y: 20,
            radius: 10
        };
        this.draw = () => {
            opt.upperdraw();
            //Game.getScreen().fillStyle = 'rgba(255,255,255,0.5)';
            //Game.getScreen().fillRect(20,
            //    20,
            //    Game.getScreen().offscreenWidth - 40,
            //    Game.getScreen().offscreenHeight - 40);
            //// 閉じるボタン
            //Game.getScreen().save();
            //Game.getScreen().beginPath();
            //Game.getScreen().strokeStyle = 'rgba(255,255,255,1)';
            //Game.getScreen().lineWidth = 6;
            //Game.getScreen().ellipse(closeButton.x, closeButton.y, closeButton.radius, closeButton.radius, 0, 0, 360);
            //Game.getScreen().moveTo(closeButton.x - Math.sqrt(2) * closeButton.radius / 2,
            //    closeButton.y - Math.sqrt(2) * closeButton.radius / 2);
            //Game.getScreen().lineTo(closeButton.x + Math.sqrt(2) * closeButton.radius / 2,
            //    closeButton.y + Math.sqrt(2) * closeButton.radius / 2);
            //Game.getScreen().moveTo(closeButton.x - Math.sqrt(2) * closeButton.radius / 2,
            //    closeButton.y + Math.sqrt(2) * closeButton.radius / 2);
            //Game.getScreen().lineTo(closeButton.x + Math.sqrt(2) * closeButton.radius / 2,
            //    closeButton.y - Math.sqrt(2) * closeButton.radius / 2);
            //Game.getScreen().stroke();
            //Game.getScreen().strokeStyle = 'rgba(128,255,255,1)';
            //Game.getScreen().lineWidth = 3;
            //Game.getScreen().stroke();
            //Game.getScreen().restore();
            // ステータス(前衛)
            {
                const left = ~~((Game.getScreen().offscreenWidth - 190) / 2);
                const top = ~~((Game.getScreen().offscreenHeight - 121 * 2) / 2);
                Game.getScreen().drawImage(Game.getScreen().texture("status"), 0, 0, 190, 121, left, top, 190, 121);
                Game.getScreen().font = "10px 'PixelMplus10-Regular'";
                Game.getScreen().fillStyle = `rgb(0,0,0)`;
                Game.getScreen().textAlign = "left";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(opt.player.getForward().name, left + 110, top + 36);
                showStatusText(`${opt.player.getForward().hp}/${opt.player.getForward().hpMax}`, left + 85, top + 56);
                showStatusText(`${opt.player.getForward().mp}/${opt.player.getForward().mpMax}`, left + 145, top + 56);
                showStatusText(`${opt.player.getForward().equips.reduce((s, [v, k]) => s + (v == null ? 0 : Data.Item.findItemDataById(v.id).atk), 0)}`, left + 85, top + 64);
                showStatusText(`${opt.player.getForward().equips.reduce((s, [v, k]) => s + (v == null ? 0 : Data.Item.findItemDataById(v.id).def), 0)}`, left + 145, top + 64);
            }
            // 後衛
            {
                const left = ~~((Game.getScreen().offscreenWidth - 190) / 2);
                const top = ~~((Game.getScreen().offscreenHeight - 121 * 2) / 2) + 121;
                Game.getScreen().drawImage(Game.getScreen().texture("status"), 0, 0, 190, 121, left, top, 190, 121);
                Game.getScreen().font = "10px 'PixelMplus10-Regular'";
                Game.getScreen().fillStyle = `rgb(0,0,0)`;
                Game.getScreen().textAlign = "left";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(opt.player.getBackward().name, left + 110, top + 36);
                showStatusText(`${opt.player.getBackward().hp}/${opt.player.getBackward().hpMax}`, left + 85, top + 56);
                showStatusText(`${opt.player.getBackward().mp}/${opt.player.getBackward().mpMax}`, left + 145, top + 56);
                showStatusText(`${opt.player.getBackward().equips.reduce((s, [v, k]) => s + (v == null ? 0 : Data.Item.findItemDataById(v.id).atk), 0)}`, left + 85, top + 64);
                showStatusText(`${opt.player.getBackward().equips.reduce((s, [v, k]) => s + (v == null ? 0 : Data.Item.findItemDataById(v.id).def), 0)}`, left + 145, top + 64);
            }
            //opt.player.equips.forEach((e, i) => {
            //    Game.getScreen().fillText(`${e.name}`, left + 12, top + 144 + 12 * i);
            //})
        };
        yield Scene.waitClick({
            end: (x, y) => {
                this.next();
            }
        });
        Game.getSceneManager().pop();
    }
    function* gameOver(opt) {
        const fade = new Scene.Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
        let fontAlpha = 0;
        this.draw = () => {
            opt.upperdraw();
            fade.draw();
            Game.getScreen().fillStyle = `rgba(255,255,255,${fontAlpha})`;
            Game.getScreen().font = "20px 'PixelMplus10-Regular'";
            const shape = Game.getScreen().measureText(`GAME OVER`);
            Game.getScreen().fillText(`GAME OVER`, (Game.getScreen().offscreenWidth - shape.width) / 2, (Game.getScreen().offscreenHeight - 20) / 2);
        };
        yield Scene.waitTimeout({
            timeout: 500,
            start: (e, ms) => { fade.startFadeOut(); },
            update: (e, ms) => { fade.update(ms); },
            end: (x, y) => { this.next(); }
        });
        yield Scene.waitTimeout({
            timeout: 500,
            start: (e, ms) => { fontAlpha = 0; },
            update: (e, ms) => { fontAlpha = e / 500; },
            end: (x, y) => { fontAlpha = 1; this.next(); }
        });
        yield Scene.waitClick({
            end: (x, y) => {
                this.next();
            }
        });
        Game.getSceneManager().pop();
        Game.getSceneManager().push(Scene.title);
    }
})(Scene || (Scene = {}));
var Scene;
(function (Scene) {
    class Fade {
        constructor(w, h) {
            this.startTime = -1;
            this.started = false;
            this.w = w;
            this.h = h;
            this.mode = "";
        }
        startFadeOut() {
            this.started = true;
            this.startTime = -1;
            this.rate = 0;
            this.mode = "fadeout";
        }
        startFadeIn() {
            this.started = true;
            this.startTime = -1;
            this.rate = 1;
            this.mode = "fadein";
        }
        stop() {
            this.started = false;
            this.startTime = -1;
        }
        update(ms) {
            if (this.started === false) {
                return;
            }
            if (this.startTime === -1) {
                this.startTime = ms;
            }
            this.rate = (ms - this.startTime) / 500;
            if (this.rate < 0) {
                this.rate = 0;
            }
            else if (this.rate > 1) {
                this.rate = 1;
            }
            if (this.mode === "fadein") {
                this.rate = 1 - this.rate;
            }
        }
        draw() {
            if (this.started) {
                Game.getScreen().fillStyle = `rgba(0,0,0,${this.rate})`;
                Game.getScreen().fillRect(0, 0, this.w, this.h);
            }
        }
    }
    Scene.Fade = Fade;
    function waitTimeout({ timeout, init = () => { }, start = () => { }, update = () => { }, end = () => { }, }) {
        let startTime = -1;
        init();
        return (delta, ms) => {
            if (startTime === -1) {
                startTime = ms;
                start(0, ms);
            }
            const elapsed = ms - startTime;
            if (elapsed >= timeout) {
                end(elapsed, ms);
            }
            else {
                update(elapsed, ms);
            }
        };
    }
    Scene.waitTimeout = waitTimeout;
    function waitClick({ update = () => { }, start = () => { }, check = () => true, end = () => { }, }) {
        let startTime = -1;
        return (delta, ms) => {
            if (startTime === -1) {
                startTime = ms;
                start(0, ms);
            }
            const elapsed = ms - startTime;
            if (Game.getInput().isClick()) {
                const pX = Game.getInput().pageX;
                const pY = Game.getInput().pageY;
                if (Game.getScreen().pagePointContainScreen(pX, pY)) {
                    const pos = Game.getScreen().pagePointToScreenPoint(pX, pY);
                    const xx = pos[0];
                    const yy = pos[1];
                    if (check(xx, yy, elapsed, ms)) {
                        end(xx, yy, elapsed, ms);
                        return;
                    }
                }
            }
            update(elapsed, ms);
        };
    }
    Scene.waitClick = waitClick;
})(Scene || (Scene = {}));
var Scene;
(function (Scene) {
    function* mapview(data) {
        this.draw = () => {
            Game.getScreen().save();
            Game.getScreen().fillStyle = "rgb(0,0,0)";
            Game.getScreen().fillRect(0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            const offx = ~~((Game.getScreen().offscreenWidth - data.map.width * 5) / 2);
            const offy = ~~((Game.getScreen().offscreenHeight - data.map.height * 5) / 2);
            // ミニマップを描画
            for (let y = 0; y < data.map.height; y++) {
                for (let x = 0; x < data.map.width; x++) {
                    const chip = data.map.layer[0].chips.value(x, y);
                    let color = "rgb(52,12,0)";
                    switch (chip) {
                        case 1:
                            color = "rgb(179,116,39)";
                            break;
                        case 10:
                            color = "rgb(255,0,0)";
                            break;
                    }
                    Game.getScreen().fillStyle = color;
                    Game.getScreen().fillRect(offx + x * 5, offy + y * 5, 5, 5);
                    let light = 1 - data.map.visibled.value(x, y) / 100;
                    if (light > 1) {
                        light = 1;
                    }
                    else if (light < 0) {
                        light = 0;
                    }
                    Game.getScreen().fillStyle = `rgba(0,0,0,${light})`;
                    Game.getScreen().fillRect(offx + x * 5, offy + y * 5, 5, 5);
                }
            }
            Game.getScreen().fillStyle = "rgb(0,255,0)";
            Game.getScreen().fillRect(offx + data.player.x * 5, offy + data.player.y * 5, 5, 5);
            Game.getScreen().restore();
        };
        yield Scene.waitClick({ end: () => this.next() });
        Game.getSceneManager().pop();
    }
    Scene.mapview = mapview;
})(Scene || (Scene = {}));
/// <reference path="../lib/game/eventdispatcher.ts" />
var Scene;
(function (Scene) {
    function* shopBuyItem(saveData) {
        const dispatcher = new Game.GUI.UIDispatcher();
        const caption = new Game.GUI.TextBox({
            left: 1,
            top: 1,
            width: 250,
            height: 42,
            text: "購買部\nさまざまな武器・アイテムの購入ができます。",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.add(caption);
        let selectedItem = -1;
        const listBox = new Game.GUI.ListBox({
            left: 8,
            top: 46,
            width: 112,
            height: 10 * 16,
            lineHeight: 16,
            getItemCount: () => saveData.shopStockList.length,
            drawItem: (left, top, width, height, index) => {
                const itemData = Data.Item.findItemDataById(saveData.shopStockList[index].id);
                if (selectedItem == index) {
                    Game.getScreen().fillStyle = `rgb(24,196,195)`;
                }
                else {
                    Game.getScreen().fillStyle = `rgb(24,133,196)`;
                }
                Game.getScreen().fillRect(left - 0.5, top + 1 - 0.5, width, height - 2);
                Game.getScreen().strokeStyle = `rgb(12,34,98)`;
                Game.getScreen().lineWidth = 1;
                Game.getScreen().strokeRect(left - 0.5, top + 1 - 0.5, width, height - 2);
                Game.getScreen().font = "10px 'PixelMplus10-Regular'";
                Game.getScreen().fillStyle = `rgb(255,255,255)`;
                Game.getScreen().textAlign = "left";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(itemData.name, left + 3, top + 3);
                Game.getScreen().textAlign = "right";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(itemData.price + "G", left + 112 - 3, top + 3);
            }
        });
        dispatcher.add(listBox);
        listBox.click = (x, y) => {
            selectedItem = listBox.getItemIndexByPosition(x, y);
            Game.getSound().reqPlayChannel("cursor");
        };
        const captionMonay = new Game.GUI.Button({
            left: 131,
            top: 46,
            width: 112,
            height: 16,
            text: () => `所持金：${('            ' + saveData.Money + ' G').substr(-13)}`,
        });
        dispatcher.add(captionMonay);
        const hoverSlider = new Game.GUI.HorizontalSlider({
            left: 131 + 14,
            top: 90,
            width: 112 - 28,
            height: 16,
            sliderWidth: 5,
            updownButtonWidth: 10,
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            minValue: 0,
            maxValue: 99,
        });
        dispatcher.add(hoverSlider);
        const btnSliderDown = new Game.GUI.Button({
            left: 131,
            top: 90,
            width: 14,
            height: 16,
            text: "－",
        });
        dispatcher.add(btnSliderDown);
        btnSliderDown.click = (x, y) => {
            hoverSlider.value -= 1;
            hoverSlider.update();
            Game.getSound().reqPlayChannel("cursor");
        };
        const btnSliderUp = new Game.GUI.Button({
            left: 243 - 14,
            top: 90,
            width: 14,
            height: 16,
            text: "＋",
        });
        dispatcher.add(btnSliderUp);
        btnSliderUp.click = (x, y) => {
            hoverSlider.value += 1;
            hoverSlider.update();
            Game.getSound().reqPlayChannel("cursor");
        };
        const captionBuyCount = new Game.GUI.Button({
            left: 131,
            top: 64,
            width: 112,
            height: 24,
            text: () => {
                if (selectedItem == -1) {
                    return '';
                }
                else {
                    return `数量：${('  ' + hoverSlider.value).substr(-2)} / 在庫：${('  ' + saveData.shopStockList[selectedItem].count).substr(-2)}\n価格：${('  ' + (Data.Item.findItemDataById(saveData.shopStockList[selectedItem].id).price * hoverSlider.value)).substr(-8) + "G"}`;
                }
            },
        });
        dispatcher.add(captionBuyCount);
        const btnDoBuy = new Game.GUI.Button({
            left: 131,
            top: 110,
            width: 112,
            height: 16,
            text: "購入",
        });
        dispatcher.add(btnDoBuy);
        btnDoBuy.click = (x, y) => {
            if (selectedItem != -1) {
                const itemData = Data.Item.findItemDataById(saveData.shopStockList[selectedItem].id);
                if ((hoverSlider.value > 0) && (saveData.shopStockList[selectedItem].count >= hoverSlider.value) && (itemData.price * hoverSlider.value <= saveData.Money)) {
                    saveData.Money -= itemData.price * hoverSlider.value;
                    if (itemData.stackable) {
                        var index = saveData.ItemBox.findIndex(x => x.id == itemData.id);
                        if (index == -1) {
                            saveData.ItemBox.push({ id: saveData.shopStockList[selectedItem].id, condition: saveData.shopStockList[selectedItem].condition, count: hoverSlider.value });
                        }
                        else {
                            saveData.ItemBox[index].count += hoverSlider.value;
                        }
                        saveData.shopStockList[selectedItem].count -= hoverSlider.value;
                    }
                    else {
                        for (let i = 0; i < hoverSlider.value; i++) {
                            saveData.ItemBox.push({ id: saveData.shopStockList[selectedItem].id, condition: saveData.shopStockList[selectedItem].condition, count: 1 });
                        }
                        saveData.shopStockList[selectedItem].count -= hoverSlider.value;
                    }
                    if (saveData.shopStockList[selectedItem].count <= 0) {
                        saveData.shopStockList.splice(selectedItem, 1);
                    }
                    selectedItem = -1;
                    hoverSlider.value = 0;
                    saveData.saveGameData();
                    Game.getSound().reqPlayChannel("meka_ge_reji_op01");
                }
            }
            //Game.getSound().reqPlayChannel("cursor");
        };
        const btnItemData = new Game.GUI.Button({
            left: 131,
            top: 142,
            width: 112,
            height: 60,
            text: () => {
                if (selectedItem == -1) {
                    return "";
                }
                const itemData = Data.Item.findItemDataById(saveData.shopStockList[selectedItem].id);
                switch (itemData.kind) {
                    case Data.Item.ItemKind.Wepon:
                        return `種別：武器\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Armor1:
                        return `種別：防具・上半身\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Armor2:
                        return `種別：防具・下半身\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Accessory:
                        return `種別：アクセサリ\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Tool:
                        return `種別：道具`;
                    case Data.Item.ItemKind.Treasure:
                        return `種別：その他`;
                    default:
                        return "";
                }
            },
        });
        dispatcher.add(btnItemData);
        const btnDescription = new Game.GUI.Button({
            left: 131,
            top: 212,
            width: 112,
            height: 36,
            text: () => {
                if (selectedItem == -1) {
                    return "";
                }
                const itemData = Data.Item.findItemDataById(saveData.shopStockList[selectedItem].id);
                return itemData.description;
            },
        });
        dispatcher.add(btnDescription);
        const btnExit = new Game.GUI.Button({
            left: 8,
            top: 16 * 11 + 46,
            width: 112,
            height: 16,
            text: "戻る",
        });
        dispatcher.add(btnExit);
        let exitScene = false;
        btnExit.click = (x, y) => {
            exitScene = true;
            Game.getSound().reqPlayChannel("cursor");
        };
        hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = captionBuyCount.visible = btnDoBuy.visible = btnItemData.visible = btnDescription.visible = false;
        this.draw = () => {
            Game.getScreen().drawImage(Game.getScreen().texture("shop/bg"), 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight, 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            Game.getScreen().drawImage(Game.getScreen().texture("shop/J11"), 0, 0, 127, 141, 113, 83, 127, 141);
            dispatcher.draw();
        };
        yield (delta, ms) => {
            if (Game.getInput().isDown()) {
                dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isMove()) {
                dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isUp()) {
                dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
            }
            hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = captionBuyCount.visible = btnDoBuy.visible = btnItemData.visible = btnDescription.visible = (selectedItem != -1);
            btnDoBuy.enable = ((selectedItem != -1) && (hoverSlider.value > 0) && (saveData.shopStockList[selectedItem].count >= hoverSlider.value) && (Data.Item.findItemDataById(saveData.shopStockList[selectedItem].id).price * hoverSlider.value <= saveData.Money));
            if (exitScene) {
                this.next();
            }
        };
        Game.getSceneManager().pop();
    }
    function* shopSellItem(saveData) {
        const dispatcher = new Game.GUI.UIDispatcher();
        const caption = new Game.GUI.TextBox({
            left: 1,
            top: 1,
            width: 250,
            height: 42,
            text: "購買部\nさまざまな武器・アイテムの購入ができます。",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.add(caption);
        let selectedItem = -1;
        const listBox = new Game.GUI.ListBox({
            left: 8,
            top: 46,
            width: 112,
            height: 10 * 16,
            lineHeight: 16,
            getItemCount: () => saveData.ItemBox.length,
            drawItem: (left, top, width, height, index) => {
                const itemData = Data.Item.findItemDataById(saveData.ItemBox[index].id);
                if (selectedItem == index) {
                    Game.getScreen().fillStyle = `rgb(24,196,195)`;
                }
                else {
                    Game.getScreen().fillStyle = `rgb(24,133,196)`;
                }
                Game.getScreen().fillRect(left - 0.5, top + 1 - 0.5, width, height - 2);
                Game.getScreen().strokeStyle = `rgb(12,34,98)`;
                Game.getScreen().lineWidth = 1;
                Game.getScreen().strokeRect(left - 0.5, top + 1 - 0.5, width, height - 2);
                Game.getScreen().font = "10px 'PixelMplus10-Regular'";
                Game.getScreen().fillStyle = `rgb(255,255,255)`;
                Game.getScreen().textAlign = "left";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(itemData.name, left + 3, top + 3);
                Game.getScreen().textAlign = "right";
                Game.getScreen().textBaseline = "top";
                Game.getScreen().fillText(itemData.price + "G", left + 112 - 3, top + 3);
            }
        });
        dispatcher.add(listBox);
        listBox.click = (x, y) => {
            selectedItem = listBox.getItemIndexByPosition(x, y);
            Game.getSound().reqPlayChannel("cursor");
        };
        const captionMonay = new Game.GUI.Button({
            left: 131,
            top: 46,
            width: 112,
            height: 16,
            text: () => `所持金：${('            ' + saveData.Money + ' G').substr(-13)}`,
        });
        dispatcher.add(captionMonay);
        const hoverSlider = new Game.GUI.HorizontalSlider({
            left: 131 + 14,
            top: 80,
            width: 112 - 28,
            height: 16,
            sliderWidth: 5,
            updownButtonWidth: 10,
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            minValue: 0,
            maxValue: 100,
        });
        dispatcher.add(hoverSlider);
        const btnSliderDown = new Game.GUI.Button({
            left: 131,
            top: 80,
            width: 14,
            height: 16,
            text: "－",
        });
        dispatcher.add(btnSliderDown);
        btnSliderDown.click = (x, y) => {
            hoverSlider.value -= 1;
            hoverSlider.update();
            Game.getSound().reqPlayChannel("cursor");
        };
        const btnSliderUp = new Game.GUI.Button({
            left: 243 - 14,
            top: 80,
            width: 14,
            height: 16,
            text: "＋",
        });
        dispatcher.add(btnSliderUp);
        btnSliderUp.click = (x, y) => {
            hoverSlider.value += 1;
            hoverSlider.update();
            Game.getSound().reqPlayChannel("cursor");
        };
        const captionSellCount = new Game.GUI.Button({
            left: 131,
            top: 64,
            width: 112,
            height: 24,
            text: () => {
                if (selectedItem == -1) {
                    return '';
                }
                else {
                    return `数量：${('  ' + hoverSlider.value).substr(-2)} / 所有：${('  ' + saveData.ItemBox[selectedItem].count).substr(-2)}\n価格：${('  ' + (Data.Item.findItemDataById(saveData.ItemBox[selectedItem].id).price * hoverSlider.value)).substr(-8) + "G"}`;
                }
            },
        });
        dispatcher.add(captionSellCount);
        const btnDoSell = new Game.GUI.Button({
            left: 131,
            top: 110,
            width: 112,
            height: 16,
            text: "売却",
        });
        dispatcher.add(btnDoSell);
        btnDoSell.click = (x, y) => {
            if (selectedItem != -1) {
                const itemData = Data.Item.findItemDataById(saveData.ItemBox[selectedItem].id);
                if ((hoverSlider.value > 0) && (saveData.ItemBox[selectedItem].count >= hoverSlider.value)) {
                    saveData.Money += itemData.price * hoverSlider.value;
                    const shopStockIndex = saveData.shopStockList.findIndex(x => x.id == saveData.ItemBox[selectedItem].id);
                    if (shopStockIndex == -1) {
                        let newstock = Object.assign({}, saveData.ItemBox[selectedItem]);
                        newstock.condition = "";
                        newstock.count = hoverSlider.value;
                        for (let i = 0; i < saveData.shopStockList.length; i++) {
                            if (saveData.shopStockList[i].id > newstock.id) {
                                saveData.shopStockList.splice(i, 0, newstock);
                                newstock = null;
                                break;
                            }
                        }
                        if (newstock != null) {
                            saveData.shopStockList.push(newstock);
                        }
                    }
                    else {
                        saveData.shopStockList[shopStockIndex].count += hoverSlider.value;
                    }
                    if (itemData.stackable && saveData.ItemBox[selectedItem].count > hoverSlider.value) {
                        saveData.ItemBox[selectedItem].count -= hoverSlider.value;
                    }
                    else {
                        saveData.ItemBox.splice(selectedItem, 1);
                    }
                    selectedItem = -1;
                    hoverSlider.value = 0;
                    saveData.saveGameData();
                    Game.getSound().reqPlayChannel("meka_ge_reji_op01");
                }
            }
            //Game.getSound().reqPlayChannel("cursor");
        };
        const btnItemData = new Game.GUI.Button({
            left: 131,
            top: 142,
            width: 112,
            height: 60,
            text: () => {
                if (selectedItem == -1) {
                    return "";
                }
                const itemData = Data.Item.findItemDataById(saveData.ItemBox[selectedItem].id);
                switch (itemData.kind) {
                    case Data.Item.ItemKind.Wepon:
                        return `種別：武器\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Armor1:
                        return `種別：防具・上半身\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Armor2:
                        return `種別：防具・下半身\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Accessory:
                        return `種別：アクセサリ\nATK:${itemData.atk} | DEF:${itemData.def}`;
                    case Data.Item.ItemKind.Tool:
                        return `種別：道具`;
                    case Data.Item.ItemKind.Treasure:
                        return `種別：その他`;
                    default:
                        return "";
                }
            },
        });
        dispatcher.add(btnItemData);
        const btnDescription = new Game.GUI.Button({
            left: 131,
            top: 212,
            width: 112,
            height: 36,
            text: () => {
                if (selectedItem == -1) {
                    return "";
                }
                const itemData = Data.Item.findItemDataById(saveData.ItemBox[selectedItem].id);
                return itemData.description;
            },
        });
        dispatcher.add(btnDescription);
        const btnExit = new Game.GUI.Button({
            left: 8,
            top: 16 * 11 + 46,
            width: 112,
            height: 16,
            text: "戻る",
        });
        dispatcher.add(btnExit);
        let exitScene = false;
        btnExit.click = (x, y) => {
            exitScene = true;
            Game.getSound().reqPlayChannel("cursor");
        };
        hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = captionSellCount.visible = btnDoSell.visible = btnItemData.visible = btnDescription.visible = false;
        this.draw = () => {
            Game.getScreen().drawImage(Game.getScreen().texture("shop/bg"), 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight, 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            Game.getScreen().drawImage(Game.getScreen().texture("shop/J11"), 0, 0, 127, 141, 113, 83, 127, 141);
            dispatcher.draw();
        };
        yield (delta, ms) => {
            if (Game.getInput().isDown()) {
                dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isMove()) {
                dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isUp()) {
                dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
            }
            captionSellCount.visible = btnDoSell.visible = btnItemData.visible = btnDescription.visible = (selectedItem != -1);
            hoverSlider.visible = btnSliderDown.visible = btnSliderUp.visible = (selectedItem != -1) && (Data.Item.findItemDataById(saveData.ItemBox[selectedItem].id).stackable);
            if ((selectedItem != -1) && (!Data.Item.findItemDataById(saveData.ItemBox[selectedItem].id).stackable)) {
                hoverSlider.value = 1;
            }
            btnDoSell.enable = ((selectedItem != -1) && (hoverSlider.value > 0) && (saveData.ItemBox[selectedItem].count >= hoverSlider.value));
            if (exitScene) {
                this.next();
            }
        };
        Game.getSceneManager().pop();
    }
    function* talkScene(saveData) {
        const fade = new Scene.Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
        const dispatcher = new Game.GUI.UIDispatcher();
        const caption = new Game.GUI.TextBox({
            left: 1,
            top: Game.getScreen().offscreenHeight - 42,
            width: 250,
            height: 42,
            text: "",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.add(caption);
        this.draw = () => {
            dispatcher.draw();
            fade.draw();
        };
        yield Scene.waitTimeout({
            timeout: 3000,
            init: () => { fade.startFadeIn(); },
            end: () => {
                this.next();
            },
        });
        yield Scene.waitTimeout({
            timeout: 500,
            update: (e) => { fade.update(e); },
            end: () => {
                fade.stop();
                this.next();
            },
        });
        const texts = [
            "【声Ａ】\nこれが今度の実験体かしら。",
            "【声Ｂ】\nはい、資料によると芋女のＪＫとのことですわ。",
            "【声Ａ】\nということは、例のルートから･･･ですわね。",
            "【声Ｂ】\n負債は相当な額だったそうですわ。",
            "【声Ａ】\n夢破れたりですわね、ふふふ…。\nでも、この実験で生まれ変わりますわ。",
            "【声Ｂ】\n生きていれば…ですわね、うふふふふふ…。",
            "【声Ａ】\nそういうことですわね。では、始めましょうか。",
            "【声Ｂ】\nはい、お姉さま。",
        ];
        for (const text of texts) {
            yield Scene.waitClick({
                start: (e) => { caption.text = text; },
                end: () => { this.next(); },
            });
        }
        yield Scene.waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeOut(); },
            update: (e) => { fade.update(e); },
            end: () => {
                this.next();
            },
        });
        yield Scene.waitTimeout({
            timeout: 1000,
            end: () => {
                this.next();
            },
        });
        saveData.ItemBox.push({ id: 304, condition: "", count: 1 });
        saveData.Money = 0;
        saveData.saveGameData();
        Game.getSceneManager().pop();
        Game.getSound().reqPlayChannel("classroom", true);
    }
    function* shop(saveData) {
        const fade = new Scene.Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
        const dispatcher = new Game.GUI.UIDispatcher();
        const caption = new Game.GUI.TextBox({
            left: 1,
            top: 1,
            width: 250,
            height: 42,
            text: "購買部\nさまざまな武器・アイテムの購入ができます。",
            edgeColor: `rgb(12,34,98)`,
            color: `rgb(24,133,196)`,
            font: "10px 'PixelMplus10-Regular'",
            fontColor: `rgb(255,255,255)`,
            textAlign: "left",
            textBaseline: "top",
        });
        dispatcher.add(caption);
        const btnBuy = new Game.GUI.Button({
            left: 8,
            top: 20 * 0 + 46,
            width: 112,
            height: 16,
            text: "アイテム購入",
        });
        dispatcher.add(btnBuy);
        btnBuy.click = (x, y) => {
            Game.getSceneManager().push(shopBuyItem, saveData);
            Game.getSound().reqPlayChannel("cursor");
        };
        const btnSell = new Game.GUI.Button({
            left: 8,
            top: 20 * 1 + 46,
            width: 112,
            height: 16,
            text: "アイテム売却",
        });
        dispatcher.add(btnSell);
        btnSell.click = (x, y) => {
            Game.getSceneManager().push(shopSellItem, saveData);
            Game.getSound().reqPlayChannel("cursor");
        };
        const captionMonay = new Game.GUI.Button({
            left: 131,
            top: 46,
            width: 112,
            height: 16,
            text: () => `所持金：${('            ' + saveData.Money + ' G').substr(-13)}`,
        });
        dispatcher.add(captionMonay);
        const btnMomyu = new Game.GUI.ImageButton({
            left: 151,
            top: 179,
            width: 61,
            height: 31,
            texture: null
        });
        dispatcher.add(btnMomyu);
        let momyu = 0;
        btnMomyu.click = (x, y) => {
            if (Math.random() > 0.5) {
                Game.getSound().reqPlayChannel("boyon1");
            }
            else {
                Game.getSound().reqPlayChannel("boyoyon1");
            }
            momyu += 500;
        };
        const btnExit = new Game.GUI.Button({
            left: 8,
            top: 16 * 11 + 46,
            width: 112,
            height: 16,
            text: "戻る",
        });
        dispatcher.add(btnExit);
        let exitScene = false;
        btnExit.click = (x, y) => {
            exitScene = true;
            Game.getSound().reqPlayChannel("cursor");
        };
        this.draw = () => {
            Game.getScreen().drawImage(Game.getScreen().texture("shop/bg"), 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight, 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
            Game.getScreen().drawImage(Game.getScreen().texture("shop/J11"), 127 * ((momyu >= 5000) ? 1 : 0), 0, 127, 141, 113, 83, 127, 141);
            dispatcher.draw();
            fade.draw();
        };
        yield Scene.waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeIn(); },
            update: (e) => { fade.update(e); },
            end: () => {
                fade.stop();
                this.next();
            },
        });
        yield (delta, ms) => {
            if (Game.getInput().isDown()) {
                dispatcher.fire("pointerdown", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isMove()) {
                dispatcher.fire("pointermove", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (Game.getInput().isUp()) {
                dispatcher.fire("pointerup", Game.getInput().pageX, Game.getInput().pageY);
            }
            if (exitScene) {
                this.next();
            }
        };
        if (momyu > 0) {
            Game.getSound().reqPlayChannel("meka_ge_reji_op01");
            saveData.Money -= momyu;
            yield Scene.waitTimeout({
                timeout: 1000,
                end: () => {
                    this.next();
                },
            });
        }
        if (saveData.Money <= -50000) {
            Game.getSound().reqStopChannel("classroom");
            Game.getSound().reqPlayChannel("sen_ge_gusya01");
            let rad = 0;
            this.draw = () => {
                Game.getScreen().translate(0, Math.sin(rad) * Math.cos(rad / 4) * 100);
                Game.getScreen().drawImage(Game.getScreen().texture("shop/bg"), 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight, 0, 0, Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
                Game.getScreen().drawImage(Game.getScreen().texture("shop/J11"), 0, 141, 127, 141, 113, 83, 127, 141);
                dispatcher.draw();
                fade.draw();
            };
            yield Scene.waitTimeout({
                timeout: 1000,
                init: () => { fade.startFadeOut(); },
                update: (e) => {
                    rad = e * Math.PI / 25;
                    fade.update(e);
                },
                end: () => {
                    fade.stop();
                    this.next();
                },
            });
            Game.getSceneManager().pop();
            Game.getSceneManager().push(talkScene, saveData);
        }
        else {
            yield Scene.waitTimeout({
                timeout: 500,
                init: () => { fade.startFadeOut(); },
                update: (e) => { fade.update(e); },
                end: () => {
                    fade.stop();
                    this.next();
                },
            });
            Game.getSceneManager().pop();
        }
    }
    Scene.shop = shop;
})(Scene || (Scene = {}));
var Scene;
(function (Scene) {
    function* title() {
        // setup
        let showClickOrTap = false;
        const fade = new Scene.Fade(Game.getScreen().offscreenWidth, Game.getScreen().offscreenHeight);
        this.draw = () => {
            const w = Game.getScreen().offscreenWidth;
            const h = Game.getScreen().offscreenHeight;
            Game.getScreen().save();
            Game.getScreen().fillStyle = "rgb(255,255,255)";
            Game.getScreen().fillRect(0, 0, w, h);
            Game.getScreen().drawImage(Game.getScreen().texture("title"), 0, 0, 192, 72, w / 2 - 192 / 2, 50, 192, 72);
            if (showClickOrTap) {
                Game.getScreen().drawImage(Game.getScreen().texture("title"), 0, 72, 168, 24, w / 2 - 168 / 2, h - 50, 168, 24);
            }
            fade.draw();
            Game.getScreen().restore();
        };
        yield Scene.waitClick({
            update: (e, ms) => { showClickOrTap = (~~(ms / 500) % 2) === 0; },
            check: () => true,
            end: () => {
                Game.getSound().reqPlayChannel("title");
                this.next();
            },
        });
        yield Scene.waitTimeout({
            timeout: 1000,
            update: (e, ms) => { showClickOrTap = (~~(ms / 50) % 2) === 0; },
            end: () => this.next(),
        });
        yield Scene.waitTimeout({
            timeout: 500,
            init: () => { fade.startFadeOut(); },
            update: (e, ms) => { fade.update(e); showClickOrTap = (~~(ms / 50) % 2) === 0; },
            end: () => {
                const saveData = new Data.SaveData.SaveData();
                saveData.loadGameData();
                Game.getSceneManager().push(Scene.corridor, saveData);
                this.next();
            },
        });
    }
    Scene.title = title;
})(Scene || (Scene = {}));
var Unit;
(function (Unit) {
    class UnitBase extends SpriteAnimation.Animator {
        constructor(x, y, spriteSheet) {
            super(spriteSheet);
            this.x = x;
            this.y = y;
        }
    }
    Unit.UnitBase = UnitBase;
})(Unit || (Unit = {}));
var Unit;
(function (Unit) {
    class Monster extends Unit.UnitBase {
        constructor(monsterId) {
            var data = Data.Monster.getMonsterData(monsterId);
            super(0, 0, data.sprite);
            this.life = data.status.hp;
            this.maxLife = data.status.hp;
            this.atk = data.status.atk;
            this.def = data.status.def;
        }
    }
    Unit.Monster = Monster;
})(Unit || (Unit = {}));
//  =((100+N58*N59)-(100+N61*N62)) * (1 + N60 - N63) / 10
var Unit;
(function (Unit) {
    class Player extends Unit.UnitBase {
        constructor(forward, backward) {
            super(0, 0, forward.config.sprite);
            this.members = [];
            this.active = 0;
            this.members[0] = {
                id: forward.id,
                name: forward.config.name,
                spriteSheet: forward.config.sprite,
                equips: Object.assign({}, forward.equips),
                mp: forward.mp,
                hp: forward.hp,
                mpMax: forward.mp,
                hpMax: forward.hp
            };
            if (backward != null) {
                this.members[1] = {
                    id: backward.id,
                    spriteSheet: backward.config.sprite,
                    name: backward.config.name,
                    equips: Object.assign({}, backward.equips),
                    mp: backward.mp,
                    hp: backward.hp,
                    mpMax: backward.mp,
                    hpMax: backward.hp
                };
            }
        }
        getForward() {
            return this.members[this.active === 0 ? 0 : 1];
        }
        getBackward() {
            return this.members[this.active === 0 ? 1 : 0];
        }
        get spriteSheet() {
            return this.members[this.active].spriteSheet;
        }
        set spriteSheet(value) {
        }
        get atk() {
            return this.members[this.active].equips.reduce((s, [v, k]) => s += (v == null ? 0 : Data.Item.findItemDataById(v.id).atk), 0);
        }
        get def() {
            return this.members[this.active].equips.reduce((s, [v, k]) => s += (v == null ? 0 : Data.Item.findItemDataById(v.id).def), 0);
        }
    }
    Unit.Player = Player;
})(Unit || (Unit = {}));
var Particle;
(function (Particle) {
    function createShowDamageSprite(start, damage, getpos) {
        let elapse = 0;
        const fontWidth = 5;
        const fontHeight = 7;
        return {
            update: (delta, ms) => {
                elapse = ms - start;
                return (elapse > 500);
            },
            draw: (camera) => {
                const { x: sx, y: sy } = getpos();
                const xx = sx - camera.left;
                const yy = sy - camera.top;
                const len = damage.length;
                const offx = -(len) * (fontWidth - 1) / 2;
                const offy = 0;
                for (let i = 0; i < damage.length; i++) {
                    const rad = Math.min(elapse - i * 20, 200);
                    if (rad < 0) {
                        continue;
                    }
                    const dy = Math.sin(rad * Math.PI / 200) * -7; // 7 = 跳ね上がる高さ
                    if (0 <= xx + (i + 1) * fontWidth &&
                        xx + (i + 0) * fontWidth < Game.getScreen().offscreenWidth &&
                        0 <= yy + (1 * fontHeight) &&
                        yy + (0 * fontHeight) < Game.getScreen().offscreenHeight) {
                        const [fx, fy] = Font7px.charDic[damage[i]];
                        Game.getScreen().drawImage(Game.getScreen().texture("font7px"), fx, fy, fontWidth, fontHeight, (xx + (i + 0) * (fontWidth - 1)) + offx, (yy + (0) * fontHeight) + offy + dy, fontWidth, fontHeight);
                    }
                }
            },
        };
    }
    Particle.createShowDamageSprite = createShowDamageSprite;
})(Particle || (Particle = {}));
var Font7px;
(function (Font7px) {
    Font7px.charDic = {
        " ": [0, 0],
        "!": [5, 0],
        "\"": [10, 0],
        "#": [15, 0],
        "$": [20, 0],
        "%": [25, 0],
        "&": [30, 0],
        "'": [35, 0],
        "(": [40, 0],
        ")": [45, 0],
        "*": [50, 0],
        "+": [55, 0],
        ",": [60, 0],
        "-": [65, 0],
        ".": [70, 0],
        "/": [75, 0],
        "0": [0, 7],
        "1": [5, 7],
        "2": [10, 7],
        "3": [15, 7],
        "4": [20, 7],
        "5": [25, 7],
        "6": [30, 7],
        "7": [35, 7],
        "8": [40, 7],
        "9": [45, 7],
        ":": [50, 7],
        ";": [55, 7],
        "<": [60, 7],
        "=": [65, 7],
        ">": [70, 7],
        "?": [75, 7],
        "@": [0, 14],
        "A": [5, 14],
        "B": [10, 14],
        "C": [15, 14],
        "D": [20, 14],
        "E": [25, 14],
        "F": [30, 14],
        "G": [35, 14],
        "H": [40, 14],
        "I": [45, 14],
        "J": [50, 14],
        "K": [55, 14],
        "L": [60, 14],
        "M": [65, 14],
        "N": [70, 14],
        "O": [75, 14],
        "P": [0, 21],
        "Q": [5, 21],
        "R": [10, 21],
        "S": [15, 21],
        "T": [20, 21],
        "U": [25, 21],
        "V": [30, 21],
        "W": [35, 21],
        "X": [40, 21],
        "Y": [45, 21],
        "Z": [50, 21],
        "[": [55, 21],
        "\\": [60, 21],
        "]": [65, 21],
        "^": [70, 21],
        "_": [75, 21],
        "`": [0, 28],
        "a": [5, 28],
        "b": [10, 28],
        "c": [15, 28],
        "d": [20, 28],
        "e": [25, 28],
        "f": [30, 28],
        "g": [35, 28],
        "h": [40, 28],
        "i": [45, 28],
        "j": [50, 28],
        "k": [55, 28],
        "l": [60, 28],
        "m": [65, 28],
        "n": [70, 28],
        "o": [75, 28],
        "p": [0, 35],
        "q": [5, 35],
        "r": [10, 35],
        "s": [15, 35],
        "t": [20, 35],
        "u": [25, 35],
        "v": [30, 35],
        "w": [35, 35],
        "x": [40, 35],
        "y": [45, 35],
        "z": [50, 35],
        "{": [55, 35],
        "|": [60, 35],
        "}": [65, 35],
        "~": [70, 35]
    };
    function draw7pxFont(str, x, y) {
        const fontWidth = 5;
        const fontHeight = 7;
        let sx = x;
        let sy = y;
        for (let i = 0; i < str.length; i++) {
            const ch = str[i];
            if (ch === "\n") {
                sy += fontHeight;
                sx = x;
                continue;
            }
            const [fx, fy] = Font7px.charDic[str[i]];
            Game.getScreen().drawImage(Game.getScreen().texture("font7px"), fx, fy, fontWidth, fontHeight, sx, sy, fontWidth, fontHeight);
            sx += fontWidth - 1;
        }
    }
    Font7px.draw7pxFont = draw7pxFont;
})(Font7px || (Font7px = {}));
var Data;
(function (Data) {
    var Item;
    (function (Item) {
        let ItemKind;
        (function (ItemKind) {
            ItemKind[ItemKind["Wepon"] = 0] = "Wepon";
            ItemKind[ItemKind["Armor1"] = 1] = "Armor1";
            ItemKind[ItemKind["Armor2"] = 2] = "Armor2";
            ItemKind[ItemKind["Accessory"] = 3] = "Accessory";
            ItemKind[ItemKind["Tool"] = 4] = "Tool";
            ItemKind[ItemKind["Treasure"] = 5] = "Treasure";
        })(ItemKind = Item.ItemKind || (Item.ItemKind = {}));
        const ItemTable = [
            /* Wepon */
            { id: 1, name: "竹刀", price: 300, kind: ItemKind.Wepon, description: "授業用なので少しボロイ", hp: 0, mp: 0, atk: 3, def: 0, effects: (data) => { }, stackable: false },
            { id: 2, name: "鉄パイプ", price: 500, kind: ItemKind.Wepon, description: "手ごろな大きさと重さで扱いやすい", hp: 0, mp: 0, atk: 5, def: 0, effects: (data) => { }, stackable: false },
            { id: 3, name: "バット", price: 700, kind: ItemKind.Wepon, description: "目指せ場外ホームラン", hp: 0, mp: 0, atk: 7, def: 0, effects: (data) => { }, stackable: false },
            /* Armor1 */
            { id: 101, name: "水着", price: 200, kind: ItemKind.Armor1, description: "動きやすいが防御はやや不安", hp: 0, mp: 0, atk: 0, def: 1, effects: (data) => { }, stackable: false },
            { id: 102, name: "制服", price: 400, kind: ItemKind.Armor1, description: "学校指定です", hp: 0, mp: 0, atk: 0, def: 2, effects: (data) => { }, stackable: false },
            { id: 103, name: "体操着", price: 600, kind: ItemKind.Armor1, description: "胸部が窮屈と不評", hp: 0, mp: 0, atk: 0, def: 3, effects: (data) => { }, stackable: false },
            /* Armor2 */
            { id: 201, name: "スカート", price: 200, kind: ItemKind.Armor2, description: "エッチな風さんですぅ", hp: 0, mp: 0, atk: 0, def: 1, effects: (data) => { }, stackable: false },
            { id: 202, name: "ブルマ", price: 400, kind: ItemKind.Armor2, description: "歳がバレますわ！", hp: 0, mp: 0, atk: 0, def: 2, effects: (data) => { }, stackable: false },
            { id: 203, name: "ズボン", price: 600, kind: ItemKind.Armor2, description: "足が細く見えます。", hp: 0, mp: 0, atk: 0, def: 3, effects: (data) => { }, stackable: false },
            /* Accessory */
            { id: 301, name: "ヘアバンド", price: 2000, kind: ItemKind.Accessory, description: "デコ！", hp: 0, mp: 0, atk: 0, def: 1, effects: (data) => { }, stackable: false },
            { id: 302, name: "メガネ", price: 2000, kind: ItemKind.Accessory, description: "メガネは不人気", hp: 0, mp: 0, atk: 0, def: 1, effects: (data) => { }, stackable: false },
            { id: 303, name: "靴下", price: 2000, kind: ItemKind.Accessory, description: "色も長さも様々", hp: 0, mp: 0, atk: 0, def: 1, effects: (data) => { }, stackable: false },
            { id: 304, name: "欠けた歯車", price: 0, kind: ItemKind.Accessory, description: "ナニカサレタヨウダ…", hp: 0, mp: 0, atk: 1, def: 1, effects: (data) => { }, stackable: false },
            /* Tool */
            { id: 501, name: "イモメロン", price: 100, kind: ItemKind.Tool, description: "空腹時にどうぞ", hp: 0, mp: 0, atk: 0, def: 0, effects: (data) => { }, stackable: true },
            { id: 502, name: "プリングルス", price: 890, kind: ItemKind.Tool, description: "歌舞伎役者もおすすめ", hp: 0, mp: 0, atk: 0, def: 0, effects: (data) => { }, stackable: true },
            { id: 503, name: "バンテリン", price: 931, kind: ItemKind.Tool, description: "ありがたい…", hp: 0, mp: 0, atk: 0, def: 0, effects: (data) => { }, stackable: true },
            { id: 504, name: "サラダチキン", price: 1000, kind: ItemKind.Tool, description: "このハーブはダメかと…", hp: 0, mp: 0, atk: 0, def: 0, effects: (data) => { }, stackable: true },
        ];
        function findItemDataById(id) {
            const idx = ItemTable.findIndex(x => x.id == id);
            return ItemTable[idx];
        }
        Item.findItemDataById = findItemDataById;
    })(Item = Data.Item || (Data.Item = {}));
})(Data || (Data = {}));
var Data;
(function (Data) {
    var Charactor;
    (function (Charactor) {
        ;
        const charactorTable = [
            {
                id: "_u01",
                name: "ウ1",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u01/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u02",
                name: "ウ2",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u02/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u03",
                name: "ウ3",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u03/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u04",
                name: "ウ4",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u04/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u05",
                name: "ウ5",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u05/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u06",
                name: "ウ6",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u06/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u07",
                name: "ウ7",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u07/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u08",
                name: "ウ8",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u08/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u09",
                name: "ウ9",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u09/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u10",
                name: "ウ10",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u10/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u11",
                name: "ウ11",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u11/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u12",
                name: "ウ12",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u12/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u13",
                name: "ウ13",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u13/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u14",
                name: "ウ14",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u14/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u15",
                name: "ウ15",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u15/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u16",
                name: "ウ16",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u16/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u17",
                name: "ウ17",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u17/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u18",
                name: "ウ18",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u18/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u19",
                name: "ウ19",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u19/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u20",
                name: "ウ20",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u20/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u21",
                name: "ウ21",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u21/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u22",
                name: "ウ22",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u22/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u23",
                name: "ウ23",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u23/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u24",
                name: "ウ24",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u24/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u25",
                name: "ウ25",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u25/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u26",
                name: "ウ26",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u26/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u27",
                name: "ウ27",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u27/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u28",
                name: "ウ28",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u28/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u29",
                name: "ウ29",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u29/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
            {
                id: "_u30",
                name: "ウ30",
                status: {
                    hp: 100,
                    mp: 100,
                    atk: 0,
                    def: 0
                },
                sprite: {
                    source: {
                        0: "./assets/charactor/_u30/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        1: { source: 0, left: 48, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        2: { source: 0, left: 96, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        3: { source: 0, left: 144, top: 0, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        4: { source: 0, left: 0, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        5: { source: 0, left: 48, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        6: { source: 0, left: 96, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        7: { source: 0, left: 144, top: 48, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        8: { source: 0, left: 0, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        9: { source: 0, left: 48, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        10: { source: 0, left: 96, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        11: { source: 0, left: 144, top: 96, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        12: { source: 0, left: 0, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        13: { source: 0, left: 48, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        14: { source: 0, left: 96, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 },
                        15: { source: 0, left: 144, top: 144, width: 48, height: 48, offsetX: 0, offsetY: -12 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                    }
                }
            },
        ];
        const charactorData = new Map();
        function configToData(config, loadStartCallback, loadEndCallback) {
            return __awaiter(this, void 0, void 0, function* () {
                const id = config.id;
                const name = config.name;
                const status = config.status;
                const sprite = yield SpriteAnimation.SpriteSheet.Create(config.sprite, loadStartCallback, loadEndCallback);
                return { id: id, name: name, status: status, sprite: sprite };
            });
        }
        function SetupCharactorData(loadStartCallback, loadEndCallback) {
            return __awaiter(this, void 0, void 0, function* () {
                const datas = yield Promise.all(charactorTable.map(x => configToData(x, loadStartCallback, loadEndCallback)));
                datas.forEach(x => charactorData.set(x.id, x));
            });
        }
        Charactor.SetupCharactorData = SetupCharactorData;
        function getPlayerIds() {
            return Array.from(charactorData.keys());
        }
        Charactor.getPlayerIds = getPlayerIds;
        function getPlayerConfig(id) {
            return charactorData.get(id);
        }
        Charactor.getPlayerConfig = getPlayerConfig;
    })(Charactor = Data.Charactor || (Data.Charactor = {}));
})(Data || (Data = {}));
var Data;
(function (Data) {
    var Monster;
    (function (Monster) {
        ;
        const monsterConfig = [
            {
                id: "slime",
                name: "スライム",
                status: {
                    hp: 5,
                    atk: 3,
                    def: 1,
                    gold: 10,
                },
                sprite: {
                    source: {
                        0: "./assets/monster/slime/walk.png"
                    },
                    sprite: {
                        0: { source: 0, left: 0, top: 0, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        1: { source: 0, left: 24, top: 0, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        2: { source: 0, left: 48, top: 0, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        3: { source: 0, left: 72, top: 0, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        4: { source: 0, left: 0, top: 24, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        5: { source: 0, left: 24, top: 24, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        6: { source: 0, left: 48, top: 24, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        7: { source: 0, left: 72, top: 24, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        8: { source: 0, left: 0, top: 48, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        9: { source: 0, left: 24, top: 48, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        10: { source: 0, left: 48, top: 48, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        11: { source: 0, left: 72, top: 48, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        12: { source: 0, left: 0, top: 72, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        13: { source: 0, left: 24, top: 72, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        14: { source: 0, left: 48, top: 72, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        15: { source: 0, left: 72, top: 72, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        16: { source: 0, left: 0, top: 120, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        17: { source: 0, left: 24, top: 120, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        18: { source: 0, left: 48, top: 120, width: 24, height: 24, offsetX: 0, offsetY: 0 },
                        19: { source: 0, left: 72, top: 120, width: 24, height: 24, offsetX: 0, offsetY: 0 }
                    },
                    animation: {
                        idle: [
                            { sprite: 0, time: 1, offsetX: 0, offsetY: 0 }
                        ],
                        move_down: [
                            { sprite: 0, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 1, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 2, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 3, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_left: [
                            { sprite: 4, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 5, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 6, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 7, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_up: [
                            { sprite: 8, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 9, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 10, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 11, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        move_right: [
                            { sprite: 12, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 13, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 14, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 15, time: 0.25, offsetX: 0, offsetY: 0 }
                        ],
                        dead: [
                            { sprite: 16, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 17, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 18, time: 0.25, offsetX: 0, offsetY: 0 },
                            { sprite: 19, time: 0.25, offsetX: 0, offsetY: 0 }
                        ]
                    }
                }
            }
        ];
        const monsterData = new Map();
        function configToData(config, loadStartCallback, loadEndCallback) {
            return __awaiter(this, void 0, void 0, function* () {
                const id = config.id;
                const name = config.name;
                const status = config.status;
                const sprite = yield SpriteAnimation.SpriteSheet.Create(config.sprite, loadStartCallback, loadEndCallback);
                return { id: id, name: name, status: status, sprite: sprite };
            });
        }
        function SetupMonsterData(loadStartCallback, loadEndCallback) {
            return __awaiter(this, void 0, void 0, function* () {
                const datas = yield Promise.all(monsterConfig.map(x => configToData(x, loadStartCallback, loadEndCallback)));
                datas.forEach(x => monsterData.set(x.id, x));
            });
        }
        Monster.SetupMonsterData = SetupMonsterData;
        function getMonsterIds() {
            return Array.from(monsterData.keys());
        }
        Monster.getMonsterIds = getMonsterIds;
        function getMonsterData(id) {
            return monsterData.get(id);
        }
        Monster.getMonsterData = getMonsterData;
    })(Monster = Data.Monster || (Data.Monster = {}));
})(Data || (Data = {}));
var Data;
(function (Data) {
    var Player;
    (function (Player) {
        ;
        class PlayerData {
            constructor(playerData) {
                this.playerData = playerData;
            }
            get id() { return this.playerData.id; }
            set id(value) { this.playerData.id = value; }
            get hp() { return this.playerData.hp; }
            set hp(value) { this.playerData.hp = value; }
            get mp() { return this.playerData.mp; }
            set mp(value) { this.playerData.mp = value; }
            get equips() { return this.playerData.equips; }
            set equips(value) { this.playerData.equips = value; }
            get config() {
                return Data.Charactor.getPlayerConfig(this.id);
            }
        }
        Player.PlayerData = PlayerData;
    })(Player = Data.Player || (Data.Player = {}));
})(Data || (Data = {}));
var Data;
(function (Data) {
    var SaveData;
    (function (SaveData_1) {
        class SaveData {
            constructor() {
                this.ItemBox = [];
                this.Money = 10000;
                this.charactorDatas = [];
                this.forwardCharactor = null;
                this.backwardCharactor = null;
                this.shopStockList = [
                    { id: 1, condition: "", count: 5 },
                    { id: 2, condition: "", count: 5 },
                    { id: 3, condition: "", count: 5 },
                    { id: 101, condition: "", count: 5 },
                    { id: 102, condition: "", count: 5 },
                    { id: 103, condition: "", count: 5 },
                    { id: 201, condition: "", count: 5 },
                    { id: 202, condition: "", count: 5 },
                    { id: 203, condition: "", count: 5 },
                    { id: 301, condition: "", count: 5 },
                    { id: 302, condition: "", count: 5 },
                    { id: 303, condition: "", count: 5 },
                    { id: 501, condition: "", count: 5 },
                    { id: 502, condition: "", count: 5 },
                    { id: 503, condition: "", count: 5 },
                    { id: 504, condition: "", count: 5 },
                ];
            }
            findCharactorById(id) {
                let ret = this.charactorDatas.find(x => x.id == id);
                if (ret == null) {
                    ret = {
                        id: id,
                        hp: 100,
                        mp: 100,
                        equips: {
                            wepon1: null,
                            armor1: null,
                            armor2: null,
                            accessory1: null,
                            accessory2: null,
                        }
                    };
                    this.charactorDatas.push(ret);
                    this.saveGameData();
                }
                return new Data.Player.PlayerData(ret);
            }
            saveGameData() {
                window.localStorage.setItem("SaveData", JSON.stringify(this));
            }
            loadGameData() {
                const dataStr = window.localStorage.getItem("SaveData");
                if (dataStr == null) {
                    return false;
                }
                const data = JSON.parse(dataStr);
                const result = Object.assign(this, data);
                return true;
            }
        }
        SaveData_1.SaveData = SaveData;
    })(SaveData = Data.SaveData || (Data.SaveData = {}));
})(Data || (Data = {}));
// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />
window.onload = () => {
    Game.create({
        title: "TSJQ",
        video: {
            id: "glcanvas",
            offscreenWidth: 252,
            offscreenHeight: 252,
            scaleX: 2,
            scaleY: 2,
        }
    }).then(() => {
        Game.getSceneManager().push(Scene.boot, null);
        Game.getTimer().on((delta, now) => {
            Game.getInput().endCapture();
            Game.getSceneManager().update(delta, now);
            Game.getInput().startCapture();
            Game.getSound().playChannel();
            Game.getScreen().begin();
            Game.getSceneManager().draw();
            Game.getScreen().end();
        });
        Game.getTimer().start();
    });
};
//# sourceMappingURL=tsjq.js.map