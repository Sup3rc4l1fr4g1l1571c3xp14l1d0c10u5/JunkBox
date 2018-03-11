"use strict";

namespace Dungeon {

    const rand: XorShift = XorShift.default();

    interface ILayerConfig {
        texture: string; // マップチップテクスチャ
        chip: { [key: number]: { x: number; y: number } }; // マップチップ
        chips: Array2D; // マップデータ
    }

    // マップ描画時の視点・視野情報
    export class Camera {
        public width: number;
        public height: number;
        public left: number;
        public top: number;
        public right: number;
        public bottom: number;
        public localPx: number;
        public localPy: number;
        public chipLeft: number;
        public chipTop: number;
        public chipRight: number;
        public chipBottom: number;
        public chipOffX: number;
        public chipOffY: number;
    }

    // ダンジョンデータ
    export class DungeonData {
        public width: number;
        public height: number;
        public gridsize: { width: number; height: number };
        public layer: { [key: number]: ILayerConfig };
        public lighting: Array2D;
        public visibled: Array2D;

        public camera: Camera;

        constructor(config: {
            width: number;
            height: number;
            gridsize: { width: number; height: number };
            layer: { [key: number]: ILayerConfig };
        }) {
            this.width = config.width;
            this.height = config.height;
            this.gridsize = config.gridsize;
            this.layer = config.layer;
            this.camera = new Camera();
            this.lighting = new Array2D(this.width, this.height, 0);
            this.visibled = new Array2D(this.width, this.height, 0);
        }

        public clearLighting(): DungeonData {
            this.lighting.fill(0);
            return this;
        }

        // update camera
        public update(param: {
            viewpoint: { x: number; y: number };
            viewwidth: number;
            viewheight: number;
        }): void {
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

            // 視野の四隅位置に対応するマップチップ座標を算出
            this.camera.chipLeft = ~~(this.camera.left / this.gridsize.width);
            this.camera.chipTop = ~~(this.camera.top / this.gridsize.height);
            this.camera.chipRight = ~~((this.camera.right + (this.gridsize.width - 1)) / this.gridsize.width);
            this.camera.chipBottom = ~~((this.camera.bottom + (this.gridsize.height - 1)) / this.gridsize.height);

            // 視野の左上位置をにマップチップをおいた場合のスクロールによるズレ量を算出
            this.camera.chipOffX = -(this.camera.left % this.gridsize.width);
            this.camera.chipOffY = -(this.camera.top % this.gridsize.height);

        }

        public draw(layerDrawHook: (layerId: number, offsetX: number, offsetY: number) => void): void {
            // 描画開始
            const gridw = this.gridsize.width;
            const gridh = this.gridsize.height;

            Object.keys(this.layer).forEach((key) => {
                const l = ~~key;
                for (let y = this.camera.chipTop; y <= this.camera.chipBottom; y++) {
                    for (let x = this.camera.chipLeft; x <= this.camera.chipRight; x++) {
                        const chipid = this.layer[l].chips.value(x, y) || 0;
                        if (/*(chipid === 1 || chipid === 10) && */this.layer[l].chip[chipid]) {
                            const xx = (x - this.camera.chipLeft) * gridw;
                            const yy = (y - this.camera.chipTop) * gridh;
                            if (!Game.pmode) {
                                Game.getScreen().drawImage(
                                    Game.getScreen().texture(this.layer[l].texture),
                                    this.layer[l].chip[chipid].x,
                                    this.layer[l].chip[chipid].y,
                                    gridw,
                                    gridh,
                                    0 + xx + this.camera.chipOffX,
                                    0 + yy + this.camera.chipOffY,
                                    gridw,
                                    gridh
                                );

                            } else {
                                Game.getScreen().fillStyle = `rgba(185,122,87,1)`;
                                Game.getScreen().strokeStyle = `rgba(0,0,0,1)`;
                                Game.getScreen().fillRect(
                                    0 + xx + this.camera.chipOffX,
                                    0 + yy + this.camera.chipOffY,
                                    gridw,
                                    gridh
                                );
                                Game.getScreen().strokeRect(
                                    0 + xx + this.camera.chipOffX,
                                    0 + yy + this.camera.chipOffY,
                                    gridw,
                                    gridh
                                );
                            }
                        }
                    }
                }

                // レイヤー描画フック
                layerDrawHook(l, this.camera.localPx, this.camera.localPy);
            });
            // 照明描画
            for (let y = this.camera.chipTop; y <= this.camera.chipBottom; y++) {
                for (let x = this.camera.chipLeft; x <= this.camera.chipRight; x++) {
                    let light = this.lighting.value(x, y) / 100;
                    if (light > 1) {
                        light = 1;
                    } else if (light < 0) {
                        light = 0;
                    }
                    const xx = (x - this.camera.chipLeft) * gridw;
                    const yy = (y - this.camera.chipTop) * gridh;
                    Game.getScreen().fillStyle = `rgba(0,0,0,${1 - light})`;
                    Game.getScreen().fillRect(
                        0 + xx + this.camera.chipOffX,
                        0 + yy + this.camera.chipOffY,
                        gridw,
                        gridh
                    );
                }
            }

        }
    }

    // ダンジョン構成要素基底クラス
    abstract class Feature {
        public abstract isValid(isWallCallback: (x: number, y: number) => boolean, canBeDugCallback: (x: number, y: number) => boolean): boolean;
        public abstract create(digCallback: (x: number, y: number, value: number) => void): void;
        public abstract debug(): void;
        // public static createRandomAt(x: number, y: number, dx: number, dy: number, options: {}): Feature {}
    }

    // 部屋
    class Room extends Feature {
        private left: number;
        private top: number;
        private right: number;
        private bottom: number;
        private doors: Map<string, number>; // key = corrd

        constructor(left: number, top: number, right: number, bottom: number, door?: IPoint) {
            super();
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.doors = new Map<string, number>();
            if (door !== undefined) {
                this.addDoor(door.x, door.y);
            }
        }

        public static createRandomAt(x: number, y: number, dx: number, dy: number, options: ICreateRoomOption): Room {
            const minw = options.roomWidth.min;
            const maxw = options.roomWidth.max;
            const width = options.random.randInt(minw, maxw);

            const minh = options.roomHeight.min;
            const maxh = options.roomHeight.max;
            const height = options.random.randInt(minh, maxh);

            if (dx === 1) { /* to the right */
                const y2 = y - options.random.randInt(0, height-1);
                return new Room(x + 1, y2, x + width, y2 + height - 1, { x: x, y: y });
            }

            if (dx === -1) { /* to the left */
                const y2 = y - options.random.randInt(0, height-1);
                return new Room(x - width, y2, x - 1, y2 + height - 1, { x: x, y: y });
            }

            if (dy === 1) { /* to the bottom */
                const x2 = x - options.random.randInt(0, width-1);
                return new Room(x2, y + 1, x2 + width - 1, y + height, { x: x, y: y });
            }

            if (dy === -1) { /* to the top */
                const x2 = x - options.random.randInt(0, width-1);
                return new Room(x2, y - height, x2 + width - 1, y - 1, { x: x, y: y });
            }

            throw new Error("dx or dy must be 1 or -1");

        }

        public static createRandomCenter(cx: number, cy: number, options: ICreateRoomOption): Room {
            const minw = options.roomWidth.min;
            const maxw = options.roomWidth.max;
            const width = options.random.randInt(minw, maxw);

            const minh = options.roomHeight.min;
            const maxh = options.roomHeight.max;
            const height = options.random.randInt(minh, maxh);

            const x1 = cx - options.random.randInt(0, width-1);
            const y1 = cy - options.random.randInt(0, height-1);
            const x2 = x1 + width - 1;
            const y2 = y1 + height - 1;

            return new Room(x1, y1, x2, y2);
        }

        public static createRandom(availWidth: number, availHeight: number, options: ICreateRoomOption): Room {
            const minw = options.roomWidth.min;
            const maxw = options.roomWidth.max;
            const width = options.random.randInt(minw, maxw);

            const minh = options.roomHeight.min;
            const maxh = options.roomHeight.max;
            const height = options.random.randInt(minh, maxh);

            const left = availWidth - width - 1;
            const top = availHeight - height - 1;

            const x1 = 1 + options.random.randInt(0, left-1);
            const y1 = 1 + options.random.randInt(0, top-1);
            const x2 = x1 + width - 1;
            const y2 = y1 + height - 1;

            return new Room(x1, y1, x2, y2);
        }

        public addDoor(x: number, y: number): Room {
            this.doors.set(x + "," + y, 1);
            return this;
        }

        public getDoors(callback: (coord: IPoint) => void): Room {
            for (const key of Object.keys(this.doors)) {
                const parts = key.split(",");
                callback({ x: parseInt(parts[0], 10), y: parseInt(parts[1], 10) });
            }
            return this;
        }

        public clearDoors(): Room {
            this.doors.clear();
            return this;
        }

        public addDoors(isWallCallback: (x: number, y: number) => boolean): Room {
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

        public debug() {
            console.log("room", this.left, this.top, this.right, this.bottom);
        }

        public isValid(isWallCallback: (x: number, y: number) => boolean, canBeDugCallback: (x: number, y: number) => boolean): boolean {
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
                    } else {
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
        public create(digCallback: (x: number, y: number, value: number) => void): void {
            const left = this.left - 1;
            const right = this.right + 1;
            const top = this.top - 1;
            const bottom = this.bottom + 1;

            for (let x = left; x <= right; x++) {
                for (let y = top; y <= bottom; y++) {
                    let value: number;
                    if (this.doors.has(x + "," + y)) {
                        value = 2;
                    } else if (x === left || x === right || y === top || y === bottom) {
                        value = 1;
                    } else {
                        value = 0;
                    }
                    digCallback(x, y, value);
                }
            }
        }

        public getCenter(): IPoint {
            return { x: Math.round((this.left + this.right) / 2), y: Math.round((this.top + this.bottom) / 2) };
        }

        public getLeft(): number {
            return this.left;
        }

        public getRight(): number {
            return this.right;
        }

        public getTop(): number {
            return this.top;
        }

        public getBottom(): number {
            return this.bottom;
        }

    }

    // 通路
    class Corridor extends Feature {
        private startX: number;
        private startY: number;
        private endX: number;
        private endY: number;
        private endsWithAWall: boolean;

        constructor(startX: number, startY: number, endX: number, endY: number) {
            super();
            this.startX = startX;
            this.startY = startY;
            this.endX = endX;
            this.endY = endY;
            this.endsWithAWall = true;
        }

        public static createRandomAt(x: number, y: number, dx: number, dy: number, options: ICreateCorridorOption): Corridor {
            const min = options.corridorLength.min;
            const max = options.corridorLength.max;
            const length = options.random.randInt(min, max);

            return new Corridor(x, y, x + dx * length, y + dy * length);
        }

        public debug(): void {
            console.log("corridor", this.startX, this.startY, this.endX, this.endY);
        }

        public isValid(
            isWallCallback: (x: number, y: number) => boolean,
            canBeDugCallback: (x: number, y: number) => boolean
        ): boolean {
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

        public create(digCallback: (x: number, y: number, value: number) => void): boolean {
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

        public createPriorityWalls(priorityWallCallback: (x: number, y: number) => void): void {
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

    interface ICreateRoomOption {
        random: XorShift;
        roomWidth: IRange;
        roomHeight: IRange;
    }
    interface ICreateCorridorOption {
        random: XorShift;
        corridorLength: IRange;
    }

    interface IMapOption extends ICreateRoomOption, ICreateCorridorOption {
        random: XorShift;
        roomWidth: IRange;
        roomHeight: IRange;
        corridorLength: IRange;
        dugPercentage: number;
        loopLimit: number;
    }

    class Generator {
        private dug: number;
        private map: number[][];

        private options: IMapOption;
        private width: number;
        private height: number;
        public rooms: Room[];
        private corridors: Corridor[];
        private features: { [key: string]: number; };
        private featureAttempts: number;
        private walls: Map<string, number>;

        constructor(
            width: number,
            height: number,
            {
                random = new XorShift(),
                roomWidth = { min: 3, max: 9 }, /* room minimum and maximum width */
                roomHeight = { min: 3, max: 5 }, /* room minimum and maximum height */
                corridorLength = { min: 3, max: 10 }, /* corridor minimum and maximum length */
                dugPercentage = 0.2, /* we stop after this percentage of level area has been dug out */
                loopLimit = 100000, /* we stop after this much time has passed (cnt) */
            }: {
                    random?: XorShift;
                    roomWidth?: IRange;
                    roomHeight?: IRange;
                    corridorLength?: IRange;
                    dugPercentage?: number;
                loopLimit?: number;
                }) {
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
            this.walls = new Map<string, number>(); /* these are available for digging */

            this.digCallback = this.digCallback.bind(this);
            this.canBeDugCallback = this.canBeDugCallback.bind(this);
            this.isWallCallback = this.isWallCallback.bind(this);
            this.priorityWallCallback = this.priorityWallCallback.bind(this);
        }
        public create(callback: (x: number, y: number, value: number) => void): Generator {
            this.rooms = [];
            this.corridors = [];
            this.map = this.fillMap(1);
            this.walls.clear();
            this.dug = 0;
            const area = (this.width - 2) * (this.height - 2);

            this.firstRoom();

            let t1: number = 0;

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
                    if (this.tryFeature(x, y, dir.x, dir.y)) { /* feature added */
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

        private digCallback(x: number, y: number, value: number): void {
            if (value === 0 || value === 2) { /* empty */
                this.map[x][y] = 0;
                this.dug++;
            } else { /* wall */
                this.walls.set(x + "," + y, 1);
            }
        }

        private isWallCallback(x: number, y: number): boolean {
            if (x < 0 || y < 0 || x >= this.width || y >= this.height) {
                return false;
            }
            return (this.map[x][y] === 1);
        }

        private canBeDugCallback(x: number, y: number): boolean {
            if (x < 1 || y < 1 || x + 1 >= this.width || y + 1 >= this.height) {
                return false;
            }
            return (this.map[x][y] === 1);
        }

        private priorityWallCallback(x: number, y: number): void {
            this.walls.set(x + "," + y, 2);
        }

        private findWall(): string {
            const prio1: string[] = [];
            const prio2: string[] = [];
            for (const [id, prio] of this.walls) {
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

            const id2 = arr.sort()[this.options.random.randInt(0, arr.length-1)]; // sort to make the order deterministic
            this.walls.delete(id2);

            return id2;
        }

        private firstRoom() {
            const cx = Math.floor(this.width / 2);
            const cy = Math.floor(this.height / 2);
            const room = Room.createRandomCenter(cx, cy, this.options as ICreateRoomOption);
            this.rooms.push(room);
            room.create(this.digCallback);
        }

        private fillMap(value: number): number[][] {
            const map: number[][] = [];
            for (let i = 0; i < this.width; i++) {
                map.push([]);
                for (let j = 0; j < this.height; j++) {
                    map[i].push(value);
                }
            }
            return map;
        }

        private static featureCreateMethodTable: { [key: string]: (x: number, y: number, dx: number, dy: number, options: IMapOption) => Feature } = { "Room": Room.createRandomAt, "Corridor": Corridor.createRandomAt };

        private tryFeature(x: number, y: number, dx: number, dy: number): boolean {
            const featureType: string = this.options.random.getWeightedValue(this.features);
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

        private removeSurroundingWalls(cx: number, cy: number): void {
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

        private static rotdirs4: IVector[] = [
            { x: 0, y: - 1 },
            { x: 1, y: 0 },
            { x: 0, y: 1 },
            { x: -1, y: 0 }
        ];

        private getDiggingDirection(cx: number, cy: number): IVector {
            if (cx <= 0 || cy <= 0 || cx >= this.width - 1 || cy >= this.height - 1) {
                return null;
            }

            let result: IVector = null;
            const deltas = Generator.rotdirs4;

            for (const delta of deltas) {
                const x = cx + delta.x;
                const y = cy + delta.y;

                if (!this.map[x][y]) { /* there already is another empty neighbor! */
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

        private addDoors(): void {
            const data = this.map;
            const isWallCallback = (x: number, y: number): boolean => {
                return (data[x][y] === 1);
            };
            for (const room of this.rooms) {
                room.clearDoors();
                room.addDoors(isWallCallback);
            }
        }

        public getRooms(): Array<Room> {
            return this.rooms;
        }

        public getCorridors(): Array<Corridor> {
            return this.corridors;
        }
    }

    export function generate(w: number, h: number, callback: (x: number, y: number, value: number) => void): Generator {
        return new Generator(w, h, { random: rand }).create(callback);
    }

}
