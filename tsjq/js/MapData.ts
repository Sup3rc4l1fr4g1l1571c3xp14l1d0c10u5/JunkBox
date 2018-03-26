/// <reference path="lib/random.ts" />
"use strict";

/**
 * ダンジョンデータ
 */
class MapData {
    /**
     * 基本的なレイアウト。レイヤー情報はここから生成される。
     */
    public layout: MapData.Generator.IDungeonLayout;

    /**
     * 幅（グリッド単位）
     */
    public get width(): number {
        return this.layout.width;
    }

    /**
     * 高さ（グリッド単位）
     */
    public get height(): number {
        return this.layout.height;
    }

    /**
     * 部屋情報
     */
    public get rooms(): MapData.Generator.IRoom[] {
        return this.layout.rooms;
    }

    /**
     * １グリッドのピクセルサイズ
     */
    public gridsize: { width: number; height: number };

    /**
     * レイヤー情報
     */
    public layer: { [key: number]: MapData.ILayerConfig };

    /**
     * 明度マップ
     */
    public lighting: Array2D;

    /**
     * ミニマップ等で使う可視範囲マップ
     */
    public visibled: Array2D;

    /**
     * スタート位置
     */
    public startPos: IPoint;

    /**
     * 階段位置
     */
    public stairsPos: IPoint;

    /**
     * カメラ
     */
    public camera: MapData.Camera;

    constructor({
        layout = null,
        gridsize = { width: 24, height: 24 },
        layer = {},
        startPos = { x: 0, y: 0 },
        stairsPos = { x: 0, y: 0 },
    }: {
            layout: MapData.Generator.IDungeonLayout;
            gridsize: { width: number; height: number };
            layer: { [key: number]: MapData.ILayerConfig };
            startPos: IPoint;
            stairsPos: IPoint;
        }) {
        this.layout = layout;
        this.gridsize = gridsize;
        this.layer = layer;
        this.camera = new MapData.Camera();
        this.lighting = new Array2D(this.width, this.height, 0);
        this.visibled = new Array2D(this.width, this.height, 0);
        this.startPos = startPos;
        this.stairsPos = stairsPos;
    }

    public clearLighting(): MapData {
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


/**
 * ダンジョンデータ
 */
namespace MapData {

    /**
     * 乱数器
     */
    const rand: XorShift = XorShift.default();

    /**
     * レイヤー情報
     */
    export interface ILayerConfig {
        /**
         * レイヤーの描画に使うマップチップのテクスチャ名
         */
        texture: string;

        /**
         * マップチップ番号に対応する画像の切り出し位置（左上座標）
         */
        chip: { [key: number]: { x: number; y: number } };


        /**
         * マップデータ
         */
        chips: Array2D;
    }

    /**
     * マップ描画時の視点・視野情報
     */
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

    /**
     * ダンジョンレイアウト生成モジュール
     */
    export namespace Generator {
        /**
         * チップ種別
         */
        export enum ChipKind {
            Empty, // 空（通路）
            Wall, // 壁（通行不能）
            Door, // ドア（閉じている）
            Stairs, // 階段
        };

        /**
         * 部屋情報
         */
        export interface IRoom {
            left: number,
            top: number,
            right: number,
            bottom: number,
            doors: Set<string>;
        }

        /**
         * 廊下情報
         */
        interface ICorridor {
            startX: number;
            startY: number;
            endX: number;
            endY: number;
        }

        /**
         * レイアウト情報
         */
        export interface IDungeonLayout {
            width: number;
            height: number;
            rooms: IRoom[];
            map: Array2D;
        }

        function Generate(
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
                }
        ): IDungeonLayout {
            const rotdirs4: IVector[] = [
                { x: 0, y: -1 } as IVector,
                { x: 1, y: 0 } as IVector,
                { x: 0, y: 1 } as IVector,
                { x: -1, y: 0 } as IVector
            ];

            // 部屋リスト
            const rooms: IRoom[] = [];

            // 廊下リスト
            const corridors: ICorridor[] = [];

            // マップデータ配列
            const map: Array2D = new Array2D(width, height, ChipKind.Wall);
            const featuretWeighTable = {
                Room: 4,
                Corridor: 4,
            };

            // 一つの壁に対して試みることができる要素生成試行回数
            const featureAttemptsLimit: number = 20;

            // 掘削量カウンタ
            let dug: number = 0;

            // 掘削可能範囲の面積
            const area = (width - 2) * (height - 2);

            /**
             * 壁表(通路の終端である壁の位置と優先度の表)
             */
            const walls = new Map<string, number>();

            // 掘削を行う(valueは座標x,yのチップ属性)
            function digCallback(x: number, y: number, value: ChipKind): void {
                switch (value) {
                    case ChipKind.Empty:
                    case ChipKind.Door:
                        {
                            /* empty */
                            map.value(x, y, ChipKind.Empty);
                            dug++;
                            break;
                        }
                    case ChipKind.Wall:
                    default:
                        {
                            /* wall */
                            walls.set(x + "," + y, 1);
                        }
                }
            };

            // 掘削可能か判定
            function canBeDugCallback(x: number, y: number): boolean {
                if (x < 1 || y < 1 || x + 1 >= width || y + 1 >= height) {
                    // マップ最外周は問答無用で掘削不可能
                    return false;
                } else {
                    // 壁なら掘削可能。それ以外は掘削不可能。
                    return (map.value(x, y) === ChipKind.Wall);
                }
            };

            // 壁か判定
            function isWallCallback(x: number, y: number): boolean {
                if (x < 0 || y < 0 || x >= width || y >= height) {
                    // マップ外は壁ではない
                    return false;
                } else {
                    return (map.value(x, y) === ChipKind.Wall);
                }
            };

            /**
             * 優先的に部屋や通路の生成地点としたい壁の情報を壁表に登録
             * @param x {number}
             * @param y {number}
             */
            function priorityWallCallback(x: number, y: number): void {
                walls.set(x + "," + y, 2);
            };

            function takePriorityWall(): [number, number] {
                const prio1: string[] = [];
                const prio2: string[] = [];
                for (const [id, prio] of walls) {
                    if (prio === 2) {
                        prio2.push(id);
                    } else {
                        prio1.push(id);
                    }
                }

                const arr = (prio2.length ? prio2 : prio1);
                if (!arr.length) {
                    // 壁は一つもない
                    return null;
                }

                const wall = arr.sort()[random.randInt(0, arr.length - 1)];
                walls.delete(wall);

                const parts: string[] = wall.split(",");
                const x: number = parseInt(parts[0]);
                const y: number = parseInt(parts[1]);
                return [x, y];
            }

            /**
             * 大きさや左上位置はランダムだが座標cx,cyを内部に含む部屋を生成。
             * @param cx {number}
             * @param cy {number}
             */
            function createRoomRandomCenter(cx: number, cy: number): IRoom {
                const minw = roomWidth.min;
                const maxw = roomWidth.max;
                const width = random.randInt(minw, maxw);

                const minh = roomHeight.min;
                const maxh = roomHeight.max;
                const height = random.randInt(minh, maxh);

                const x1 = cx - random.randInt(0, width - 1);
                const y1 = cy - random.randInt(0, height - 1);
                const x2 = x1 + width - 1;
                const y2 = y1 + height - 1;

                return { left: x1, top: y1, right: x2, bottom: y2, doors: new Set<string>() };
            };

            /**
             * 部屋データをマップに反映させる（実際に掘削する）
             * @param room {IRoom} 
             */
            function digRoom(room: IRoom): void {
                const left = room.left - 1;
                const right = room.right + 1;
                const top = room.top - 1;
                const bottom = room.bottom + 1;

                for (let x = left; x <= right; x++) {
                    for (let y = top; y <= bottom; y++) {
                        let value: number;
                        if (room.doors.has(x + "," + y)) {
                            value = ChipKind.Door;
                        } else if (x === left || x === right || y === top || y === bottom) {
                            value = ChipKind.Wall;
                        } else {
                            value = ChipKind.Empty;
                        }
                        digCallback(x, y, value);
                    }
                }
            };

            /**
             * 指定した地点から十時方向で距離２以内に存在する壁情報を壁表から消す
             * @param cx {number}
             * @param cy {number}
             */
            function removeSurroundingWalls(cx: number, cy: number): void {
                for (const delta of rotdirs4) {
                    const x1: number = cx + delta.x;
                    const y1: number = cy + delta.y;
                    walls.delete(x1 + "," + y1);
                    const x2: number = cx + 2 * delta.x;
                    const y2: number = cy + 2 * delta.y;
                    walls.delete(x2 + "," + y2);
                }
            }


            // 最初の部屋をマップの中心付近に生成する
            {
                const cx: number = Math.floor(width / 2);
                const cy: number = Math.floor(height / 2);
                const room: IRoom = createRoomRandomCenter(cx, cy);
                //console.log("firstroom is ", room);
                digRoom(room);
                rooms.push(room);
            }

            let t1: number = 0;

            let priorityWalls = 0;
            trycreate: do {
                if (t1++ > loopLimit) {
                    break trycreate;
                }

                /* 生成位置とする壁を一つ取り出す */
                const wall: [number, number] = takePriorityWall();
                if (wall == null) {
                    break trycreate;
                }
                const [x, y] = wall;

                // 壁の隣接セル（上下左右）に一つだけ空き（掘削済み）セルがある場合、その空き（掘削済み）セルと反対側を掘削方向とする
                // 一つも空きがない、もしくは２つ以上の空きがある場合や、掘削できない地点の場合は壁を選択しなおす
                if (x <= 0 || y <= 0 || x >= width - 1 || y >= height - 1) {
                    // 外周部の壁は掘削できない
                    continue trycreate;
                }

                let dir: IVector = null;
                for (const delta of rotdirs4) {
                    const cx = x + delta.x;
                    const cy = y + delta.y;

                    if (map.value(cx, cy) === ChipKind.Empty) {
                        // 隣接セルに空きがあった
                        if (dir != null) {
                            // 二つ以上の空きがあった
                            //console.log("  -> dir is not null ","dir=",dir," ","delta=",delta);
                            continue trycreate;
                        }
                        dir = { x: delta.x, y: delta.y };
                    }
                }
                if (dir == null) {
                    // 隣接セルに一つも空きがない
                    //console.log("  -> dir is null");
                    continue trycreate;
                } else {
                    // 掘削方向＝見つかった方向の反対なのでベクトルを反転させる
                    dir.x *= -1;
                    dir.y *= -1;
                }
                //console.log("dig dir ", dir);

                // 選ばれた要素生成を試みる
                let featureAttempts = 0;
                tryCreateFeature: do {
                    featureAttempts++;
                    switch (random.getWeightedValue(featuretWeighTable)) {
                        case "Room":
                            {
                                /*
                                 * 部屋を生成する（ここで生成する部屋の情報はは内部空間）
                                 */

                                const minw = roomWidth.min;
                                const maxw = roomWidth.max;
                                const width = random.randInt(minw, maxw);

                                const minh = roomHeight.min;
                                const maxh = roomHeight.max;
                                const height = random.randInt(minh, maxh);

                                let room: IRoom = null;
                                if (dir.x === 1) { // 壁の右に部屋を作る
                                    const y2 = y - random.randInt(0, height - 1);
                                    room = {
                                        left: x + 1,
                                        top: y2,
                                        right: x + width,
                                        bottom: y2 + height - 1,
                                        doors: new Set<string>([x + "," + y])
                                    };
                                } else if (dir.x === -1) { // 壁の左に部屋を作る
                                    const y2 = y - random.randInt(0, height - 1);
                                    room = {
                                        left: x - width,
                                        top: y2,
                                        right: x - 1,
                                        bottom: y2 + height - 1,
                                        doors: new Set<string>([x + "," + y])
                                    };
                                } else if (dir.y === 1) { // 壁の上に部屋を作る
                                    const x2 = x - random.randInt(0, width - 1);
                                    room = {
                                        left: x2,
                                        top: y + 1,
                                        right: x2 + width - 1,
                                        bottom: y + height,
                                        doors: new Set<string>([x + "," + y])
                                    };
                                } else if (dir.y === -1) { // 壁の下に部屋を作る
                                    const x2 = x - random.randInt(0, width - 1);
                                    room = {
                                        left: x2,
                                        top: y - height,
                                        right: x2 + width - 1,
                                        bottom: y - 1,
                                        doors: new Set<string>([x + "," + y])
                                    };
                                } else {
                                    throw new Error("dx or dy must be 1 or -1");
                                }
                                {
                                    // 生成した部屋が実際に掘削可能かチェック
                                    // 部屋は以下の条件を満たす必要がある
                                    //   ・外周部については、壁以外の空間に被っていない
                                    //   ・内部については、掘削できる空間である
                                    const rleft = room.left - 1;
                                    const rright = room.right + 1;
                                    const rtop = room.top - 1;
                                    const rbottom = room.bottom + 1;

                                    for (let x = rleft; x <= rright; x++) {
                                        for (let y = rtop; y <= rbottom; y++) {
                                            if (x === rleft || x === rright || y === rtop || y === rbottom) {
                                                if (!isWallCallback(x, y)) {
                                                    //外周部に壁ではない要素がある
                                                    continue tryCreateFeature;
                                                }
                                            } else {
                                                if (!canBeDugCallback(x, y)) {
                                                    // 内部に掘削できない要素がある
                                                    continue tryCreateFeature;
                                                }
                                            }
                                        }
                                    }
                                }

                                // 妥当なので実際の掘削を実行
                                digRoom(room);

                                rooms.push(room);
                                //console.log("create ", "Room", " ", room);
                                break;
                            }
                        case "Corridor":
                            {
                                /*
                                 * 通路を生成
                                 * 前提条件：掘削方向(dir)は上下左右のいずれか（dir.x, dir.y がどちらかが１でどちらかが０）
                                 */
                                const min = corridorLength.min;
                                const max = corridorLength.max;
                                const len = random.randInt(min, max);
                                const corridor = { startX: x, startY: y, endX: x + dir.x * len, endY: y + dir.y * len };

                                /*
                                 * 生成した通路が妥当かチェック
                                 * 以下のアルゴリズムは「前提条件：掘削方向(dir)は上下左右のいずれか」が成立することを前提とした縦横どちらかに掘り進む場合の両対応アルゴリズム
                                 */

                                // 通路の開始位置
                                const sx: number = corridor.startX;
                                const sy: number = corridor.startY;

                                // 通路の終点までの距離+1が掘削量
                                let length: number =
                                    1 + Math.max(Math.abs(corridor.endX - sx), Math.abs(corridor.endY - sy));

                                // 終点方向
                                const dx: number = Math.sign(corridor.endX - sx);
                                const dy: number = Math.sign(corridor.endY - sy);

                                // 掘削方向に対する両脇座標オフセット
                                // 右に掘削中＝(dx,dy)=(1,0)のとき、(nx,ny)は(0,1)となり下方向、(-nx, -ny)は(0,-1)となり上方向になる。
                                const nx: number = dy;
                                const ny: number = -dx;

                                // 始点から掘削可能な限り掘削する
                                let ok = true;
                                for (let i = 0; i < length; i++) {
                                    const tx = sx + i * dx;
                                    const ty = sy + i * dy;

                                    if (!canBeDugCallback(tx, ty)) {
                                        // 掘削不能
                                        ok = false;
                                    }
                                    if (!isWallCallback(tx + nx, ty + ny) || !isWallCallback(tx - nx, ty - ny)) {
                                        // 両脇が壁では無い
                                        ok = false;
                                    }

                                    if (!ok) {
                                        // 掘削不能ならば、掘削できる範囲が通路になるように補正
                                        length = i;
                                        corridor.endX = tx - dx;
                                        corridor.endY = ty - dy;
                                        break;
                                    }
                                }

                                /**
                                 * 掘削長をチェックして不正なら通路生成自体を失敗とする
                                 */

                                // 長さが０の通路＝掘削できないので不正
                                if (length === 0) {
                                    continue tryCreateFeature;
                                }

                                // 長さが１の場合、通路の先が空きスペースに繋がっていなければ不正とする
                                if (length === 1 && isWallCallback(corridor.endX + dx, corridor.endY + dy)) {
                                    continue tryCreateFeature;
                                }

                                // 以下のように通路が部屋の角に斜めに接触してしまうケースを回避したい。
                                // ###########
                                // --->#######
                                // ####......#
                                // ####......#
                                // ####......#
                                // ###########
                                // 
                                // これは通路の終点からさらに一歩進んだ地点が壁の場合、その両脇も壁でないといけないことにする
                                // というルールをチェックすることで対応できる
                                //
                                const firstCornerBad = !isWallCallback(corridor.endX + dx + nx, corridor.endY + dy + ny);
                                const secondCornerBad = !isWallCallback(corridor.endX + dx - nx, corridor.endY + dy - ny);
                                const endsWithAWall = isWallCallback(corridor.endX + dx, corridor.endY + dy);
                                if ((firstCornerBad || secondCornerBad) && endsWithAWall) {
                                    continue tryCreateFeature;
                                }

                                // 掘削
                                for (let i = 0; i < length; i++) {
                                    const tx = sx + i * dx;
                                    const ty = sy + i * dy;
                                    map.value(tx, ty, 0);
                                    dug++;
                                }

                                if (endsWithAWall) {
                                    // 通路終端が壁の場合、隣接している壁を部屋や通路を優先的に生成する壁としてマークする
                                    priorityWallCallback(corridor.endX + dx, corridor.endY + dy);
                                    priorityWallCallback(corridor.endX + nx, corridor.endY + ny);
                                    priorityWallCallback(corridor.endX - nx, corridor.endY - ny);
                                }

                                // 生成完了
                                corridors.push(corridor);
                                //console.log("create ", "Corridor", " ", corridor);
                                break;
                            }
                        default:
                            {
                                continue tryCreateFeature;
                            }
                    }

                    // 要素が追加されたので、始点から見て以下の!に部分に掘削開始地点となりうる壁情報があるならその壁情報を削除する
                    // 実際に壁を削除するわけではなく、!の地点から新しい通路や部屋を生成しないようにする
                    // 以下の図は通路の場合の一例だが、部屋についても同様の判定を行っている
                    //
                    // ########
                    // ###!!###
                    // ###!!###
                    // #!!!@...
                    // ###!!###
                    // ###!!###
                    // ########
                    //

                    removeSurroundingWalls(x, y);
                    removeSurroundingWalls(x - dir.x, y - dir.y);
                    break;
                } while (featureAttempts < featureAttemptsLimit);

                // 部屋の生成を優先する壁情報の数を数える
                priorityWalls = 0;
                for (const [, value] of walls) {
                    if (value > 1) {
                        priorityWalls++;
                    }
                }

                // 掘削済み地点が規定の割合を超える、もしくは部屋の生成を優先する壁情報が０になったら生成ループ打ち切り
            } while ((dug / area) < dugPercentage || priorityWalls);

            // 以上で部屋と通路の配置が完了

            // ドアを作る
            for (const room of rooms) {
                const left = room.left - 1;
                const right = room.right + 1;
                const top = room.top - 1;
                const bottom = room.bottom + 1;
                // 部屋の外周をスキャンして壁では無い部分にドアをセットする
                room.doors.clear();
                for (let x = left; x <= right; x++) {
                    for (let y = top; y <= bottom; y++) {
                        if (x !== left && x !== right && y !== top && y !== bottom) {
                            continue;
                        }
                        if (map.value(x, y) === ChipKind.Wall) {
                            continue;
                        }
                        room.doors.add(x + "," + y);
                    }
                }
            }

            // 以上で生成終了

            return {
                width: width,
                height: height,
                rooms: random.shuffle(rooms), // 一応部屋はシャッフルしておく
                map: map,
            };

        }


        export function generate(
            {
            floor = 0,
                gridsize = { width: 24, height: 24 },
                layer = {}
        }: {
                    floor: number,
                    gridsize: { width: number; height: number; },
                    layer: { [key: number]: MapData.ILayerConfig }
                }): MapData {
            // マップサイズ算出（てきとう）
            const mapChipW = 30 + floor * 3;
            const mapChipH = 30 + floor * 3;

            // レイアウト生成
            const layout = Generate(mapChipW, mapChipH, { random: rand });

            /*
             *  レイヤー１（移動可能部）のマップチップレイアウトを生成
             */

            const mapchipsL1: Array2D = new Array2D(layout.width, layout.height);

            // 移動可能部分の地面
            for (let y = 0; y < layout.height; y++) {
                for (let x = 0; x < layout.width; x++) {
                    mapchipsL1.value(x, y, layout.map.value(x, y) !== MapData.Generator.ChipKind.Empty ? 0 : 1)
                }
            }
            // 地面の上に壁チップを置いて壁っぽく見せる
            for (let y = 1; y < mapChipH; y++) {
                for (let x = 0; x < mapChipW; x++) {
                    mapchipsL1.value(x,
                        y - 1,
                        mapchipsL1.value(x, y) === 1 && mapchipsL1.value(x, y - 1) === 0
                            ? 2
                            : mapchipsL1.value(x, y - 1)
                    );
                }
            }

            /*
             *  レイヤー２（装飾部）のマップチップレイアウトを生成
             */
            const mapchipsL2 = new Array2D(mapChipW, mapChipH);
            for (let y = 0; y < mapChipH; y++) {
                for (let x = 0; x < mapChipW; x++) {
                    mapchipsL2.value(x, y, (mapchipsL1.value(x, y) === MapData.Generator.ChipKind.Empty) ? 0 : 1);
                }
            }

            /*
             * 部屋はシャッフルされているのでここではシャッフルせず最初の部屋から順に階段などに割り当てる
             */

            // 最初の部屋をマップ内のスタート地点とする
            const startPos: IPoint = {
                x: ~~((layout.rooms[0].left + layout.rooms[0].right) / 2),
                y: ~~((layout.rooms[0].top + layout.rooms[0].bottom) / 2)
            };

            // 次の部屋をマップ内の降り階段地点とする
            const stairsPos: IPoint = {
                x: ~~((layout.rooms[1].left + layout.rooms[1].right) / 2),
                y: ~~((layout.rooms[1].top + layout.rooms[1].bottom) / 2)
            };

            mapchipsL1.value(stairsPos.x, stairsPos.y, 10);

            layer[0].chips = mapchipsL1;
            layer[1].chips = mapchipsL2;

            return new MapData({
                layout: layout,
                gridsize: gridsize,
                layer: layer,
                startPos: startPos,
                stairsPos: stairsPos
            });

        }
    }
}
