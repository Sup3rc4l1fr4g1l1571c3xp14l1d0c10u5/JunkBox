
namespace NeuralNetwork {

    export class Matrix {
        private _row: number;
        private _col: number;
        private _values: Float32Array;

        public get row() : number { return this._row; }
        public get col() : number { return this._col; }
        public toBlob() {
            return new Blob([this._values.buffer]);
        }
        private constructor(row: number, col: number, values: Float32Array) {
            if (row < 0 || col < 0 || values.length !== row * col) {
                throw new Error("out of index");
            }
            this._row = row;
            this._col = col;
            this._values = values;
        }

        static create(row: number, col: number, gen: (row: number, col: number) => number) {
            const ret: Float32Array = new Float32Array(row * col);
            let n = 0;
            for (let i = 0; i < row; i++) {
                for (let j = 0; j < col; j++) {
                    ret[n++] = gen(i, j);
                }
            }
            return new Matrix(row, col, ret);
        }
        static buildFromArray(row: number, col: number, values: number[]) {
            return new Matrix(row, col, new Float32Array(values));
        }
        static buildFromFloat32Array(row: number, col: number, values: Float32Array) {
            return new Matrix(row, col, new Float32Array(values));
        }

        clone(): Matrix {
            return new Matrix(this._row, this._col, new Float32Array(this._values));
        }

        toArray() : number[] {
            return Array.from(this._values);
        }

        toString() : string {
            let lines = [];
            for (let j = 0; j < this._row; j++) {
                let line = [];
                for (let i = 0; i < this._col; i++) {
                    line.push(this._values[this.at(j, i)]);
                }
                lines.push(`[${line.join(", ")}]`);
            }
            return `[${lines.join(", ")}]`;
        }

        at(row: number, col: number): number{
            if (row < 0 || this._row <= row || col < 0 || this._col <= col) {
                throw new Error("out of index");
            }
            return this._col * row + col;
        }

        getRow(row: number): number[] {
            if (row < 0 || this._row <= row) {
                throw new Error("out of index");
            }
            let ret = [];
            for (let j = 0; j < this._col; j++) {
                ret.push(this._values[this.at(row, j)]);
            }
            return ret;
        }

        setRow(row: number, values: number[]) : Matrix {
            if (row < 0 || this._row <= row) {
                throw new Error("out of index");
            }
            if (this._col !== values.length) {
                throw new Error("column length mismatch");
            }
            for (let j = 0; j < this._col; j++) {
                this._values[this.at(row, j)] = values[j];
            }
            return this;
        }

        getCol(col: number): number[] {
            if (col < 0 || this._col <= col) {
                throw new Error("out of index");
            }
            let ret = [];
            for (let j = 0; j < this._row; j++) {
                ret.push(this._values[this.at(j, col)]);
            }
            return ret;
        }

        setCol(col: number, values: number[]) : Matrix {
            if (col < 0 || this._col <= col) {
                throw new Error("out of index");
            }
            if (this._row !== values.length) {
                throw new Error("row length mismatch");
            }
            for (let j = 0; j < this._row; j++) {
                this._values[this.at(j, col)] = values[j];
            }
            return this;
        }

        map(func: (v: number) => number): Matrix {
            return new Matrix(this._row, this._col, this._values.map(func));
        }

        static equal(m1: Matrix, m2: Matrix): boolean {
            if (m1._row !== m2._row || m1._col !== m2._col) {
                return false;
            }
            return m1._values.every((x, i) => x === m2._values[i]);
        }

        static add(m1: Matrix, m2: Matrix): Matrix {
            if (m1._row !== m2._row || m1._col !== m2._col) {
                throw new Error("dimension mismatch");
            }
            const size = m1._row * m1._col;
            const ret: Float32Array = new Float32Array(size);
            for (let i = 0; i < size; i++) {
                ret[i] = m1._values[i] + m2._values[i];
            }
            return new Matrix(m1._row, m1._col, ret);
        }

        static sub(m1: Matrix, m2: Matrix): Matrix{
            if (m1._row !== m2._row || m1._col !== m2._col) {
                throw new Error("dimension mismatch");
            }
            const size = m1._row * m1._col;
            const ret: Float32Array = new Float32Array(size);
            for (let i = 0; i < size; i++) {
                ret[i] = m1._values[i] - m2._values[i];
            }
            return new Matrix(m1._row, m1._col, ret);
        }

        static scalarMultiplication(m: Matrix, s: number): Matrix{
            return new Matrix(m._row, m._col, m._values.map((x) => x * s));
        }

        static dotProduct(m1: Matrix, m2: Matrix): Matrix{
            if (m1._col !== m2._row) {
                throw new Error("dimension mismatch");
            }
            const ret: Float32Array = new Float32Array(m1._row * m2._col);
            let n = 0;
            const dp = 1;
            const dq = m2.col;
            for (let i = 0; i < m1._row; i++) {
                for (let j = 0; j < m2._col; j++) {
                    let sum = 0;
                    let p = m1.at(i, 0); 
                    let q = m2.at(0, j); 
                    for (let k = 0; k < m1._col; k++) {
                        sum += m1._values[p] * m2._values[q];
                        p += dp;
                        q += dq;
                    }
                    ret[n++] = sum;
                }
            }
            return new Matrix(m1._row, m2._col, ret);
        }

        static hadamardProduct(m1: Matrix, m2: Matrix): Matrix{
            if (m1._row !== m2._row || m1._col !== m2._col) {
                throw new Error("dimension mismatch");
            }
            const size = m1._row * m1._col;
            const ret: Float32Array = new Float32Array(size);
            for (let i = 0; i < size; i++) {
                ret[i] = m1._values[i] * m2._values[i];
            }
            return new Matrix(m1._row, m1._col, ret);
        }

        static transpose(m: Matrix): Matrix{
            const size = m._values.length;
            const ret: Float32Array = new Float32Array(size);
            let p = 0; const dq = m.col;
            for (let j = 0; j < m._col; j++) {
                let q = m.at(0, j);
                for (let i = 0; i < m._row; i++) {
                    ret[p] = m._values[q];
                    p += 1;
                    q += dq;
                }
            }
            return new Matrix(m._col, m._row, ret);
        }

        static test() {
            function arrayEqual(x, y) {
                return x.length === y.length && x.every((_, i) => x[i] === y[i]);
            }
            // hadamardProduct test
            {
                const m1 = Matrix.buildFromArray(3, 2, [2, 4, 6, 8, 10, 12]);
                const m2 = Matrix.buildFromArray(3, 2, [3, 5, 7, 9, 11, 13]);
                const m3 = Matrix.hadamardProduct(m1, m2);
                const m4 = Matrix.buildFromArray(3, 2, [6, 20, 42, 72, 110, 156]);
                if (!Matrix.equal(m3, m4)) { return false; }
                console.log("hadamardProduct: ok");
            }

            // scalarMultiplication test
            {
                const m1 = Matrix.buildFromArray(3, 2, [2, 4, 6, 8, 10, 12]);
                const m2 = Matrix.scalarMultiplication(m1, -4);
                const m3 = Matrix.buildFromArray(3, 2, [2, 4, 6, 8, 10, 12].map(x => x * -4));
                if (!Matrix.equal(m2, m3)) { return false; }
                console.log("scalarMultiplication: ok");
            }

            // dotProduct test
            {
                const m1 = Matrix.buildFromArray(3, 2, [2, 4, 6, 8, 10, 12]);
                const m2 = Matrix.buildFromArray(2, 3, [3, 5, 7, 13, 11, 9]);
                const m3 = Matrix.dotProduct(m1, m2);
                const m4 = Matrix.buildFromArray(3, 3, [58, 54, 50, 122, 118, 114, 186, 182, 178]);
                if (!Matrix.equal(m3, m4)) { return false; }
                console.log("dotProduct: ok");
            }

            // transpose test
            {
                const m1 = Matrix.buildFromArray(3, 2, [2, 4, 6, 8, 10, 12]);
                const m2 = Matrix.transpose(m1);
                const m3 = Matrix.buildFromArray(2, 3, [2, 6, 10, 4, 8, 12]);
                if (!Matrix.equal(m2, m3)) { return false; }
                console.log("transpose: ok");
            }

            // toString test
            {
                const s1 = Matrix.buildFromArray(3, 2, [2, 4, 6, 8, 10, 12]).toString();
                const s2 = "[[2, 4], [6, 8], [10, 12]]";
                if (s1 !== s2) { return false; }
                console.log("toString: ok");
            }

            // getRow test
            {
                const m1 = Matrix.buildFromArray(2, 3, [3, 5, 7, 13, 11, 9]);
                const r0 = m1.getRow(0);
                const r1 = m1.getRow(1);
                if (!arrayEqual(r0, [3, 5, 7])) { return false; }
                if (!arrayEqual(r1, [13, 11, 9])) { return false; }
                console.log("getRow: ok");
            }

            // getCol test
            {
                const m1 = Matrix.buildFromArray(2, 3, [3, 5, 7, 13, 11, 9]);
                const c0 = m1.getCol(0);
                const c1 = m1.getCol(1);
                const c2 = m1.getCol(2);
                if (!arrayEqual(c0, [3, 13])) { return false; }
                if (!arrayEqual(c1, [5, 11])) { return false; }
                if (!arrayEqual(c2, [7, 9])) { return false; }
                console.log("getCol: ok");
            }

            // setRow test
            {
                const m1 = Matrix.buildFromArray(2, 3, [1, 2, 3, 4, 5, 6]);
                m1.setRow(0, [7, 8, 9]);
                m1.setRow(1, [10, 11, 12]);
                if (!arrayEqual(m1.toArray(), [7, 8, 9, 10, 11, 12])) { return false; }
                console.log("setRow: ok");
            }

            // setCol test
            {
                const m1 = Matrix.buildFromArray(2, 3, [1, 2, 3, 4, 5, 6]);
                m1.setCol(0, [7, 8]);
                m1.setCol(1, [9, 10]);
                m1.setCol(2, [11, 12]);
                if (!arrayEqual(m1.toArray(), [7, 9, 11, 8, 10, 12])) { return false; }
                console.log("setCol: ok");
            }

            return true;
        }
    }

    function openBinary(url: string): Promise<ArrayBuffer> {
        return new Promise<ArrayBuffer>((resolve, reject) => {
            const xhr = new XMLHttpRequest();
            xhr.open('GET', url, true);
            xhr.responseType = 'blob';
            xhr.onload = () => {
                const fr = new FileReader();
                fr.onload = () => {
                    resolve(<ArrayBuffer>fr.result);
                }
                fr.onerror = () => {
                    reject(fr.error);
                }
                fr.readAsArrayBuffer(xhr.response);

            };
            xhr.onerror = (e) => {
                reject(e);
            };
            xhr.send();
        });
    }

    export namespace MNist {
        export class Image {
            private _index: number;
            private _width: number;
            private _height: number;
            private _data: Uint8Array;
            get data(): Uint8Array { return this._data; }
            getNormalizedVector(): number[] { return Array.from(this.data).map(x => x / 255.0 * 0.99 + 0.01); }
            constructor(index: number, width: number, height: number, data: Uint8Array) {
                this._index = index;
                this._width = width;
                this._height = height;
                this._data = data;
            }
        }
        export class Label {
            private _index: number;
            private _data: number;
            get data(): number { return this._data; }
            toNormalizedVector(n: number): number[] { return new Array(n).fill(0).map((_, i) => i === this.data ? 0.99 : 0.01); }
            constructor(index: number, data: number) {
                this._index = index;
                this._data = data;
            }
        }
        export function loadImageData(path: string): Promise<Image[]> {
            return openBinary(path)
                .then((ab) => {
                    const view = new DataView(ab);
                    if (view.getUint32(0) !== 0x00000803) { throw new Error("bad file format"); }
                    const count = view.getUint32(4);
                    const width = view.getUint32(8);
                    const height = view.getUint32(12);
                    const size = width * height;
                    const ret: Image[] = [];
                    for (let i = 0; i < count; i++) {
                        ret.push(new Image(i, width, height, new Uint8Array(ab, 16 + i * size, size)));
                    }
                    return ret;
                });
        }
        export function loadLabelData(path: string): Promise<Label[]> {
            return openBinary(path)
                .then((ab) => {
                    const view = new DataView(ab);
                    if (view.getUint32(0) !== 0x00000801) { throw new Error("bad file format"); }
                    const count = view.getUint32(4);
                    const ret: Label[] = [];
                    for (let i = 0; i < count; i++) {
                        ret.push(new Label(i, view.getUint8(8 + i)));
                    }
                    return ret;
                });
        }
    }

    function times<T>(n: number, predicates: (n: number) => T) : T[] {
        const ret = new Array(n);
        for (let i = 0; i < n; i++) {
            ret[i] = predicates(i);
        }
        return ret;
    }

    function normRand() {
        const r1 = Math.random();
        const r2 = Math.random();
        return (Math.sqrt(-2.0 * Math.log(r1)) * Math.cos(2.0 * Math.PI * r2)) * 0.1;
    }

    interface IActivationFunction {
        activate(val: number): number;
        derivate(val: number): number;

    }
    class SigmoidFunction implements IActivationFunction {
            activate(val: number): number {
                return 1.0 / (1.0 + Math.exp(-val));
            }

        derivate(val: number): number {
            return (1.0 - val) * val;
        }
    }

    export class NeuralNetwork {
        private _layers: number[];
        private _outputs: Matrix[];
        private _errors: Matrix[];
        private _weights: Matrix[];
        private _activationFunction: IActivationFunction = new SigmoidFunction();

        get output(): number[] { return this._outputs[this._outputs.length - 1].getRow(0); }
        get maxOutputIndex(): number { return this.output.reduce(([mv, mi], x, i) => (mv < x) ? [x, i] : [mv, mi], [Number.MIN_SAFE_INTEGER, -1])[1]; }
        constructor(...layers: number[]) {
            this._layers = layers;
            this._outputs = this._layers.map(x => Matrix.create(1, x, () => 0.0));
            this._errors  = this._layers.map(x => Matrix.create(1, x, () => 0.0));
            this._weights = times(this._layers.length - 1, (n) => Matrix.create(this._layers[n], this._layers[n + 1], () => normRand()));
        }

        public prediction(data: number[]): NeuralNetwork {
            if (data.length !== this._layers[0]) {
                throw new Error("length mismatch");
            }
            this._outputs[0].setRow(0,data);
            for (let i = 1; i < this._layers.length; i++) {
                this._outputs[i] = Matrix.dotProduct(this._outputs[i - 1], this._weights[i - 1]).map(x => this._activationFunction.activate(x));
            }
            return this;
        }

        public train(data: number[], label:number[], alpha:number): NeuralNetwork {
            if (data.length !== this._layers[0]) {
                throw new Error("length mismatch");
            }
            if (label.length !== this._layers[this._layers.length - 1]) {
                throw new Error("length mismatch");
            }

            this.prediction(data);

            const labelMatrix = Matrix.buildFromArray(1, label.length, label);
            this._errors[this._outputs.length - 1] = Matrix.sub(labelMatrix, this._outputs[this._outputs.length - 1]);
            for (let i = this._outputs.length - 2; i >= 0; i--) {
                this._errors[i] = Matrix.dotProduct(this._errors[i + 1], Matrix.transpose(this._weights[i]));
            }

            for (let i = 0; i < this._layers.length - 1; i++) {
                const m = this._outputs[i + 1].map(x => this._activationFunction.derivate(x));
                this._weights[i] = Matrix.add(this._weights[i], Matrix.scalarMultiplication(Matrix.dotProduct(Matrix.transpose(this._outputs[i]), Matrix.hadamardProduct(m, this._errors[i + 1])), alpha));
            }


            return this;
        }

        public toBlob() {
            return new Blob([new Blob([new Uint32Array([this._layers.length].concat(this._layers))])].concat(this._weights.map(x => x.toBlob())));
        }
        public static fromBlob(blob) {
            return new Promise<NeuralNetwork>((resolve, reject) => {
                const fr = new FileReader();
                fr.onload = () => {
                    const view = new DataView(<ArrayBuffer>fr.result);
                    const layerNum = view.getUint32(0, true);
                    const layers : number[] = [];
                    for (let i = 0; i < layerNum; i++) {
                        layers.push(view.getUint32(4+i*4, true));
                    }
                    let offset = 4 + layerNum * 4;
                    //const layerNum = 3;
                    //const layers = [28*28,100,10];
                    //let offset = 0;
                    const weights: Matrix[] = [];
                    for (let i = 0; i < layerNum-1; i++) {
                        const row = layers[i];
                        const col = layers[i+1];
                        const size = row * col;

                        weights.push(Matrix.buildFromFloat32Array(row,col,new Float32Array(view.buffer, offset, size)));
                        offset += 4*size;
                    }
                    const ret = new NeuralNetwork(...layers);
                    ret._weights = weights;
                    resolve(ret);
                }
                fr.onerror = () => {
                    reject(fr.error);
                }
                fr.readAsArrayBuffer(blob);
            });

        }
    }

}

(function () {
    //NeuralNetwork.Matrix.test();
    let nnet: NeuralNetwork.NeuralNetwork = null;
    window.onload = () => {
        const canWrite = <HTMLCanvasElement> document.getElementById("write");
        const conWrite = canWrite.getContext("2d");
        const btnTrain = document.getElementById("train");
        const btnClear = document.getElementById("clear");
        const btnSave = document.getElementById("save");
        const btnLoad = document.getElementById("load");
        const divOutput = document.getElementById("output");
        const canSample = <HTMLCanvasElement> document.getElementById("sample");
        const conSample = canSample.getContext("2d");
        (function() {
            function resample(imageData: ImageData, width: number, height: number) {
                function scale(v1: number, v2: number, rate: number) {
                    return Math.min(255, Math.max(0, v1 * (1.0 - rate) + v2 * rate));
                }

                function scaleRGBA(v1: number, v2: number, rate: number) {
                    const a = scale((v1 >> 24) & 0xFF, (v2 >> 24) & 0xFF, rate);
                    const r = scale((v1 >> 16) & 0xFF, (v2 >> 16) & 0xFF, rate);
                    const g = scale((v1 >> 8) & 0xFF, (v2 >> 8) & 0xFF, rate);
                    const b = scale((v1 >> 0) & 0xFF, (v2 >> 0) & 0xFF, rate);
                    return (a << 24) | (r << 16) | (g << 8) | (b << 0);
                }

                if (imageData.width === width && imageData.height === height) {
                    return imageData;
                }
                const newImage = new ImageData(width, height);
                const src = new Uint32Array(imageData.data.buffer);
                const dst = new Uint32Array(newImage.data.buffer);
                for (let y = 0; y < height; y++) {
                    for (let x = 0; x < width; x++) {
                        const sx = x * imageData.width / width;
                        const sy = y * imageData.height / height;
                        const x1 = Math.max(0, Math.trunc(sx));
                        const y1 = Math.max(0, Math.trunc(sy));
                        const x2 = Math.min(imageData.width - 1, Math.trunc(sx) + 1);
                        const y2 = Math.min(imageData.height - 1, Math.trunc(sy) + 1);
                        const rateX = sx - x1;
                        const rateY = sy - y1;
                        const step1X = scaleRGBA(src[y1 * imageData.width + x1], src[y1 * imageData.width + x2], rateX);
                        const step1Y = scaleRGBA(src[y2 * imageData.width + x1], src[y2 * imageData.width + x2], rateX);
                        const ret = scaleRGBA(step1X, step1Y, rateY);
                        dst[y * width + x] = ret;
                    }
                }
                return newImage;
            }

            function gray(imageData: ImageData) {
                const ret : number[] = [];
                for (let y = 0; y < imageData.height; y++) {
                    for (let x = 0; x < imageData.width; x++) {
                        let sum = 0;
                        for (let i = 0; i < 2; i++) {
                            sum += (255 - imageData.data[(y * imageData.width + x) * 4 + i]);
                        }
                        ret.push(~~(sum / 3));
                    }
                }
                return ret;
            }


            let px = 0;
            let py = 0;
            let down = false;
            let cRect = canWrite.getBoundingClientRect();
            let left = cRect.left;
            let top = cRect.top;
            let width = cRect.width;
            let height = cRect.height;
            btnClear.addEventListener("click",
                () => {
                    conWrite.fillStyle = "rgb(255,255,255)";
                    conWrite.fillRect(0, 0, width, height);
                });

            canWrite.addEventListener("mousedown",
                (e) => {
                    if (!down) {
                        down = true;
                        px = e.pageX - left;
                        py = e.pageY - top;
                    }
                });
            canWrite.addEventListener("mousemove",
                (e) => {
                    if (down) {
                        conWrite.beginPath();
                        conWrite.lineWidth = 20;
                        conWrite.moveTo(px, py);
                        px = e.pageX - left;
                        py = e.pageY - top;
                        conWrite.lineTo(px, py);
                        conWrite.closePath();
                        conWrite.stroke();
                    }
                });
            canWrite.addEventListener("mouseup",
                () => {
                    if (down) {
                        down = false;
                        const img = resample(conWrite.getImageData(0, 0, width, height), 28, 28);
                        conSample.putImageData(img, 0, 0);
                        if (nnet != null) {
                            const gimg = gray(img);
                            nnet.prediction(gimg.map(x => x / 255.0 * 0.99 + 0.01));
                            divOutput.innerHTML = nnet.output.map((x, i) => [x, i]).sort((x, y) => x[0] - y[0]).map(x => `${x[1]}: ${x[0]}`).join("<br/>");
                        }
                    }
                });

        })();
        btnSave.addEventListener("click", () => {
            const blob = new Blob([ nnet.toBlob() ], { "type" : "application/octet-stream" });
            (<HTMLLinkElement>btnSave).href = window.URL.createObjectURL(blob);
        });
        btnLoad.addEventListener("click", () => {
            const input = <HTMLInputElement>document.createElement('input');
            input.type = 'file';
            input.accept = '.*, application/octet-stream';
            input.onchange = (event) => {
                NeuralNetwork.NeuralNetwork.fromBlob((<HTMLInputElement>event.target).files[0]).then(x => {
                    nnet = x;
                    divOutput.innerText = "loaded";
                });
            };
            input.click();
        });
        btnTrain.addEventListener("click", () => {
            console.log("loading.");
            Promise.all([
                NeuralNetwork.MNist.loadImageData("train-images.idx3-ubyte"),
                NeuralNetwork.MNist.loadLabelData("train-labels.idx1-ubyte"),
                NeuralNetwork.MNist.loadImageData("t10k-images.idx3-ubyte"),
                NeuralNetwork.MNist.loadLabelData("t10k-labels.idx1-ubyte")
            ])
                .then(([trainImages, trainLabels, testImages, testLabels]) => {
                    console.log("loaded.");
                    conWrite.clearRect(0, 0, 256, 256);
                    conWrite.lineWidth = 1;
                    let gx = 0;
                    let gy = 0;
                    let ms = +Date.now();
                    const samples = new Array(testLabels.length).fill(0).map((_, i) => i).sort(() => Math.random() - 0.5).slice(0, 100);
                    const nn = new NeuralNetwork.NeuralNetwork(28 * 28, 100, 10);
                    let i = 0;
                    trainLoop();

                    function evalLoop() {
                        let ok = 0;
                        let j = 0;
                        function loop() {
                            while (ms + 100 > +Date.now()) {
                                if (j < samples.length) {
                                    nn.prediction(testImages[samples[j]].getNormalizedVector());
                                    ok += (nn.maxOutputIndex === testLabels[samples[j]].data) ? 1 : 0;
                                    j++;
                                } else {
                                    let xx = ~~(i * 256 / trainImages.length);
                                    let yy = ~~(256 - 250 * ok / samples.length);
                                    if (gx < xx) {
                                        conWrite.beginPath();
                                        conWrite.moveTo(gx, gy);
                                        conWrite.lineTo(xx, yy);
                                        conWrite.stroke();
                                        gx = xx;
                                        gy = yy;
                                    }
                                    console.log(`${i}: ${ok}/${samples.length} (${100 * ok / samples.length}%)`);
                                    ms = +Date.now();
                                    setTimeout(trainLoop, 0);
                                    return;
                                }
                            }
                            ms = +Date.now();
                            setTimeout(loop, 0);
                        }
                        loop();
                    }
                    function trainLoop() {
                        function loop() {
                            while (ms + 100 > +Date.now()) {
                                if (i < trainImages.length) {
                                    nn.train(
                                        trainImages[i].getNormalizedVector(),
                                        trainLabels[i].toNormalizedVector(10),
                                        0.1
                                    );
                                    i++;
                                    if ((i % 100) === 0) {
                                        ms = +Date.now();
                                        evalLoop();
                                        return;
                                    }
                                } else {
                                    setTimeout(overall, 0);
                                    return;
                                }
                            }
                            ms = +Date.now();
                            setTimeout(loop, 0);
                        }
                        loop();
                    }
                    function overall() {
                        let ok = 0;
                        let j = 0;
                        function loop() {
                            while (ms + 100 > +Date.now()) {
                                if (j < testLabels.length) {
                                    nn.prediction(testImages[j].getNormalizedVector());
                                    ok += (nn.maxOutputIndex === testLabels[j].data) ? 1 : 0;
                                    j++;
                                } else {
                                    console.log(`$overall: ${ok}/${testLabels.length} (${100 * ok / testLabels.length}%)`);
                                    nnet = nn;
                                    return;
                                }
                            }
                            ms = +Date.now();
                            setTimeout(loop, 0);
                        }
                        loop();
                    }
                });
        });
    }
})();

