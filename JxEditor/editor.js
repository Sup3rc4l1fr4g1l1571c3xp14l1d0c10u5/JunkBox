///// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/3.0/lib.es6.d.ts" />
//// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />
Window.prototype.whenEvent = function (event) {
    return new Promise((resolve, reject) => {
        const fire = (e) => {
            resolve(e);
            this.addEventListener(event, fire, false);
        };
        this.addEventListener(event, fire, false);
    });
};
var Editor;
(function (Editor) {
    "use strict";
    function assert(x) {
        if (!x) {
            throw new Error("assert failed");
        }
    }
    ;
    ;
    class Keyboard {
        constructor() {
            //入力バッファの書き込み先頭ポインタ
            this.inputBufferWriteIndex = 0;
            //入力バッファの読み出し先頭ポインタ
            this.inputBufferReadIndex = 0;
            // /シフト、コントロールキー等の状態
            this.ctrlKeyStatus = 0 /* CHK_NONE */;
            // 仮想キーコードに対応するキーの状態（Onの時1）
            this.virtualKeyStatus = new Uint8Array(256 / 8);
            // readで読み取ったキーの情報
            this.currentCtrlKeys = 0 /* CHK_NONE */;
            this.currentVKeyCode = 0;
            this.currentAsciiCode = 0;
            // キーボードシステム初期化
            this.inputBufferWriteIndex = 0;
            this.inputBufferReadIndex = 0;
            this.ctrlKeyStatus = 512 /* CHK_NUMLK */; // NumLock 初期状態はONとする
            this.inputBuffer = [];
            for (let i = 0; i < Keyboard.keycodeBufferSize; i++) {
                this.inputBuffer[i] = { ctrlKey: 0 /* CHK_NONE */, vKeycode: 0 };
            }
            //全キー離した状態
            this.virtualKeyStatus.fill(0);
        }
        updateCtrlKeyState(vk, isDown) {
            // SHIFT,ALT,CTRL,Winキーの押下状態を更新
            let k = 0;
            switch (vk) {
                case 16 /* VKEY_SHIFT */:
                case 160 /* VKEY_LSHIFT */:
                    k = 1 /* CHK_SHIFT_L */;
                    break;
                case 161 /* VKEY_RSHIFT */:
                    k = 2 /* CHK_SHIFT_R */;
                    break;
                case 17 /* VKEY_CONTROL */:
                case 162 /* VKEY_LCONTROL */:
                    k = 4 /* CHK_CTRL_L */;
                    break;
                case 163 /* VKEY_RCONTROL */:
                    k = 8 /* CHK_CTRL_R */;
                    break;
                case 18 /* VKEY_MENU */:
                case 164 /* VKEY_LMENU */:
                    k = 16 /* CHK_ALT_L */;
                    break;
                case 165 /* VKEY_RMENU */:
                    k = 32 /* CHK_ALT_R */;
                    break;
                case 91 /* VKEY_LWIN */:
                    k = 64 /* CHK_WIN_L */;
                    break;
                case 92 /* VKEY_RWIN */:
                    k = 128 /* CHK_WIN_R */;
                    break;
            }
            if (!isDown) {
                this.ctrlKeyStatus = this.ctrlKeyStatus & (~k);
            }
            else {
                this.ctrlKeyStatus = this.ctrlKeyStatus | k;
            }
        }
        // NumLock,CapsLock,ScrollLockの状態更新
        updateLockKeyState(vk) {
            switch (vk) {
                case 145 /* VKEY_SCROLL */:
                    this.ctrlKeyStatus ^= 256 /* CHK_SCRLK */;
                    break;
                case 144 /* VKEY_NUMLOCK */:
                    this.ctrlKeyStatus ^= 512 /* CHK_NUMLK */;
                    break;
                case 20 /* VKEY_CAPITAL */:
                    if ((this.ctrlKeyStatus & 3 /* CHK_SHIFT */) === 0)
                        return;
                    this.ctrlKeyStatus ^= 1024 /* CHK_CAPSLK */;
                    break;
                default:
                    return;
            }
        }
        // vkが SHIFT,ALT,WIN,CTRLか判定
        isSpecialKey(vk) {
            switch (vk) {
                case 16 /* VKEY_SHIFT */:
                case 160 /* VKEY_LSHIFT */:
                case 161 /* VKEY_RSHIFT */:
                case 17 /* VKEY_CONTROL */:
                case 162 /* VKEY_LCONTROL */:
                case 163 /* VKEY_RCONTROL */:
                case 18 /* VKEY_MENU */:
                case 164 /* VKEY_LMENU */:
                case 165 /* VKEY_RMENU */:
                case 91 /* VKEY_LWIN */:
                case 92 /* VKEY_RWIN */:
                    return true;
                default:
                    return false;
            }
        }
        // vkがNumLock,SCRLock,CapsLockか判定
        isLockKey(vk) {
            switch (vk) {
                case 145 /* VKEY_SCROLL */:
                case 144 /* VKEY_NUMLOCK */:
                case 20 /* VKEY_CAPITAL */:
                    return true;
                default:
                    return false;
            }
        }
        pushKeyStatus(vk, isDown) {
            if (this.isSpecialKey(vk)) {
                if (isDown && this.virtualKeyStatus.getBit(vk)) {
                    return; // キーリピートの場合、無視
                }
                this.updateCtrlKeyState(vk, isDown); //SHIFT系キーのフラグ処理
            }
            else if (isDown && this.isLockKey(vk)) {
                if (this.virtualKeyStatus.getBit(vk)) {
                    return; //キーリピートの場合、無視
                }
                this.updateLockKeyState(vk); //NumLock、CapsLock、ScrollLock反転処理
            }
            //キーコードに対する押下状態配列を更新
            if (!isDown) {
                this.virtualKeyStatus.setBit(vk, 0);
                return;
            }
            this.virtualKeyStatus.setBit(vk, 1);
            if ((this.inputBufferWriteIndex + 1 === this.inputBufferReadIndex) ||
                (this.inputBufferWriteIndex === Keyboard.keycodeBufferSize - 1) && (this.inputBufferReadIndex === 0)) {
                return; //バッファがいっぱいの場合無視
            }
            this.inputBuffer[this.inputBufferWriteIndex].vKeycode = vk;
            this.inputBuffer[this.inputBufferWriteIndex].ctrlKey = this.ctrlKeyStatus;
            this.inputBufferWriteIndex++;
            if (this.inputBufferWriteIndex === Keyboard.keycodeBufferSize) {
                this.inputBufferWriteIndex = 0;
            }
        }
        // キーバッファからキーを一つ読み取る
        // 押されていなければfalseを返す
        readKey() {
            this.currentAsciiCode = 0x00;
            this.currentVKeyCode = 0x0000;
            this.currentCtrlKeys = 0 /* CHK_NONE */;
            if (this.inputBufferWriteIndex === this.inputBufferReadIndex) {
                return false;
            }
            const k = this.inputBuffer[this.inputBufferReadIndex++];
            this.currentVKeyCode = k.vKeycode;
            this.currentCtrlKeys = k.ctrlKey;
            if (this.inputBufferReadIndex === Keyboard.keycodeBufferSize) {
                this.inputBufferReadIndex = 0;
            }
            if (k.ctrlKey & (12 /* CHK_CTRL */ | 48 /* CHK_ALT */ | 192 /* CHK_WIN */)) {
                return true;
            }
            let k2;
            if (k.vKeycode >= 65 /* VKEY_KEY_A */ && k.vKeycode <= 90 /* VKEY_KEY_Z */) {
                if (((k.ctrlKey & 3 /* CHK_SHIFT */) !== 0) !== ((k.ctrlKey & 1024 /* CHK_CAPSLK */) !== 0)) {
                    //SHIFTまたはCapsLock（両方ではない）
                    k2 = Keyboard.vk2asc2[k.vKeycode];
                }
                else {
                    k2 = Keyboard.vk2asc1[k.vKeycode];
                }
            }
            else if (k.vKeycode >= 96 /* VKEY_NUMPAD0 */ && k.vKeycode <= 111 /* VKEY_DIVIDE */) {
                //テンキー関連
                if ((k.ctrlKey & (3 /* CHK_SHIFT */ | 512 /* CHK_NUMLK */)) === 512 /* CHK_NUMLK */) {
                    //NumLock（SHIFT＋NumLockは無効）
                    k2 = Keyboard.vk2asc2[k.vKeycode];
                }
                else {
                    k2 = Keyboard.vk2asc1[k.vKeycode];
                }
            }
            else {
                if (k.ctrlKey & 3 /* CHK_SHIFT */) {
                    k2 = Keyboard.vk2asc2[k.vKeycode];
                }
                else {
                    k2 = Keyboard.vk2asc1[k.vKeycode];
                }
            }
            this.currentAsciiCode = k2;
            return true;
        }
        getCurrentCtrlKeys() {
            return this.currentCtrlKeys;
        }
        getCurrentVKeyCode() {
            return this.currentVKeyCode;
        }
        getCurrentAsciiCode() {
            return this.currentAsciiCode;
        }
    }
    Keyboard.keycodeBufferSize = 16;
    Keyboard.vk2asc1 = new Uint8Array([
        // 仮想キーコードからASCIIコードへの変換テーブル（SHIFTなし）
        0, 0, 0, 0, 0, 0, 0, 0, 0, "\t", 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        ' ', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 0, 0, 0, 0, 0, 0,
        0, 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
        'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, '*', '+', 0, '-', 0, '/',
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ':', ';', ',', '-', '.', '/',
        '@', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, '[', '\\', ']', '^', 0,
        0, 0, '\\', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
    ].map(x => (typeof (x) === "string") ? x.codePointAt(0) : x));
    Keyboard.vk2asc2 = new Uint8Array([
        // 仮想キーコードからASCIIコードへの変換テーブル（SHIFTあり）
        0, 0, 0, 0, 0, 0, 0, 0, 0, "\t", 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        ' ', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, '!', '"', '#', '$', '%', '&', 0x27, '(', ')', 0, 0, 0, 0, 0, 0,
        0, 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
        'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 0, 0, 0, 0, 0,
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '*', '+', 0, '-', '.', '/',
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, '*', '+', '<', '=', '>', '?',
        '`', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, '{', '|', '}', '~', 0,
        0, 0, '_', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
    ].map(x => (typeof (x) === "string") ? x.codePointAt(0) : x));
    let Unicode;
    (function (Unicode) {
        function isHighSurrogate(x) {
            return 0xD800 <= x && x <= 0xDBFF;
        }
        function isLoSurrogate(x) {
            return 0xDC00 <= x && x <= 0xDFFF;
        }
        function isSurrogatePair(high, low) {
            return isHighSurrogate(high) && isLoSurrogate(low);
        }
        function utf16ToUtf32Char(high, low) {
            if (0x0000 <= high && high <= 0xD7FF) {
                return [1, high];
            }
            else if (0xE000 <= high && high <= 0xFFFF) {
                return [1, high];
            }
            else if (0xD800 <= high && high <= 0xDBFF && 0xDC00 <= low && low <= 0xDFFF) {
                return [2, ((((high >> 6) & 0x0F) + 1) << 16) | ((high & 0x3F) << 10) | (low & 0x3FF)];
            }
            else {
                return undefined;
            }
        }
        function utf32ToUtf16Char(code) {
            if (0x0000 <= code && code <= 0xD7FF) {
                return [code, undefined];
            }
            else if (0xD800 <= code && code <= 0xDFFF) {
                return undefined;
            }
            else if (0xE000 <= code && code <= 0xFFFF) {
                return [code, undefined];
            }
            else if (0x10000 <= code && code <= 0x10FFFF) {
                const cp = code - 0x10000;
                const high = 0xD800 | (cp >> 10);
                const low = 0xDC00 | (cp & 0x3FF);
                return [high, low];
            }
            else {
                return undefined;
            }
        }
        function stringToUtf16(str) {
            const utf16 = [];
            for (let i = 0; i < str.length; i++) {
                utf16.push(str.charCodeAt(i));
            }
            return new Uint16Array(utf16);
        }
        function utf16ToUtf32(utf16) {
            const utf32 = [];
            for (let i = 0; i < utf16.length;) {
                const [size, code] = utf16ToUtf32Char(utf16[i], utf16[i + 1]);
                if (size === undefined) {
                    return undefined;
                }
                utf32.push(code);
                i += size;
            }
            return new Uint32Array(utf32);
        }
        Unicode.utf16ToUtf32 = utf16ToUtf32;
        function utf32ToUtf16(utf32) {
            const utf16 = [];
            for (let i = 0; i < utf32.length; i++) {
                const [high, low] = utf32ToUtf16Char(utf32[i]);
                if (high === undefined) {
                    return undefined;
                }
                utf16.push(high);
                if (low !== undefined) {
                    utf16.push(low);
                }
            }
            return new Uint16Array(utf16);
        }
        Unicode.utf32ToUtf16 = utf32ToUtf16;
        function utf16ToString(utf16) {
            return String.fromCharCode.apply(null, utf16);
        }
        Unicode.utf16ToString = utf16ToString;
        function strToUtf32(str) {
            return utf16ToUtf32(stringToUtf16(str));
        }
        Unicode.strToUtf32 = strToUtf32;
        function utf32ToStr(utf32) {
            return utf16ToString(utf32ToUtf16(utf32));
        }
        Unicode.utf32ToStr = utf32ToStr;
    })(Unicode || (Unicode = {}));
    class GapBuffer {
        constructor(gapSize) {
            this._gapSize = gapSize || 64;
            if (this._gapSize <= 0) {
                throw new RangeError("gapSize must be > 0");
            }
            this._buffer = new Uint32Array(this._gapSize);
            this._gapStart = 0;
            this._gapEnd = this._gapSize;
        }
        dispose() {
            this._buffer = null;
            this._gapStart = this._gapEnd = 0;
        }
        *[Symbol.iterator]() {
            for (let i = 0; i < this._gapStart; i++) {
                yield this._buffer[i];
            }
            for (let i = this._gapEnd; i < this._buffer.length; i++) {
                yield this._buffer[i];
            }
        }
        get length() {
            return this._buffer.length - (this._gapEnd - this._gapStart);
        }
        get(ix) {
            if (ix >= this.length) {
                return undefined;
            }
            if (ix >= this._gapStart) {
                ix += (this._gapEnd - this._gapStart);
            }
            return this._buffer[ix];
        }
        set(ix, value) {
            if (ix >= this.length) {
                return;
            }
            if (ix >= this._gapStart) {
                ix += (this._gapEnd - this._gapStart);
            }
            this._buffer[ix] = value;
        }
        grow(newSize) {
            const gapSize = newSize - this._buffer.length + (this._gapEnd - this._gapStart);
            const newBuffer = new Uint32Array(newSize);
            for (let i = 0; i < this._gapStart; i++) {
                newBuffer[i] = this._buffer[i];
            }
            for (let i = this._gapEnd; i < this._buffer.length; i++) {
                newBuffer[i + gapSize] = this._buffer[i];
            }
            this._buffer = newBuffer;
            this._gapEnd = this._gapStart + gapSize;
        }
        insert(ix, value) {
            if (ix < 0) {
                throw new RangeError("insert index must be >= 0");
            }
            if (ix > this.length) {
                throw new RangeError("insert index must be <= length (for now)");
            }
            if (this._gapStart === this._gapEnd) {
                this.grow(this._buffer.length + this._gapSize);
            }
            this.moveGap(ix);
            this._buffer[this._gapStart++] = value;
        }
        insertMany(ix, values) {
            // TODO: this should be optimised
            for (let i = 0; i < values.length; ++i) {
                this.insert(ix + i, values[i]);
            }
        }
        deleteAfter(ix, len) {
            if (ix >= this.length) {
                return false;
            }
            this.moveGap(ix);
            this._gapStart = ix;
            this._gapEnd += len;
            if (this._gapEnd > this._buffer.length) {
                this._gapEnd = this._buffer.length;
            }
            return true;
        }
        deleteBefore(ix, len) {
            if (ix === 0 || ix > this.length) {
                return false;
            }
            this.moveGap(ix);
            this._gapStart -= len;
            if (this._gapStart < 0) {
                this._gapStart = 0;
            }
            return true;
        }
        clear() {
            this._gapStart = 0;
            this._gapEnd = this._buffer.length;
        }
        asArray() {
            const newBuffer = new Uint32Array(this.length);
            let n = 0;
            for (let i = 0; i < this._gapStart; i++, n++) {
                newBuffer[n] = this._buffer[i];
            }
            for (let i = this._gapEnd; i < this._buffer.length; i++, n++) {
                newBuffer[n] = this._buffer[i];
            }
            return newBuffer;
        }
        moveGap(ix) {
            if (ix < this._gapStart) {
                const delta = this._gapStart - ix;
                this._buffer.copyWithin(this._gapEnd - delta, ix, this._gapStart);
                this._gapStart -= delta;
                this._gapEnd -= delta;
            }
            else if (ix > this._gapStart) {
                const delta = ix - this._gapStart;
                this._buffer.copyWithin(this._gapStart, this._gapEnd, this._gapEnd + delta);
                this._gapStart += delta;
                this._gapEnd += delta;
            }
        }
        append(that) {
            this.moveGap(this.length);
            this.grow(this.length + that.length);
            for (let i = 0; i < that.length; ++i) {
                this._buffer[this._gapStart++] = that.get(i);
            }
        }
        reduce(callback, initialValue) {
            let currentIndex = 0;
            let previousValue = initialValue;
            for (const currentValue of this) {
                previousValue = callback(previousValue, currentValue, currentIndex++, this);
            }
            return previousValue;
        }
        find(predicate) {
            let index = 0;
            for (const value of this) {
                if (predicate(value, index, this)) {
                    return value;
                }
            }
            return undefined;
        }
    }
    class DataViewIterator {
        constructor(buffer, littleEndian = false) {
            this.littleEndian = littleEndian;
            this.dataView = new DataView(buffer);
            this.byteOffset = 0;
        }
        dispose() {
            this.littleEndian = false;
            this.dataView = null;
            this.byteOffset = 0;
        }
        getUint32() {
            const ret = this.dataView.getUint32(this.byteOffset, this.littleEndian);
            this.byteOffset += 4;
            return ret;
        }
        getUint8() {
            const ret = this.dataView.getUint8(this.byteOffset);
            this.byteOffset += 1;
            return ret;
        }
        getBytes(len) {
            const ret = new Uint8Array(this.dataView.buffer, this.byteOffset, len);
            this.byteOffset += len;
            return ret;
        }
    }
    class BitmapFont {
        get width() { return this._fontWidth; }
        get height() { return this._fontHeight; }
        constructor(buffer) {
            const it = new DataViewIterator(buffer, true);
            const fourCc = it.getUint32();
            if (fourCc !== 0x46504D42) {
                it.dispose();
                throw new TypeError("bad file format.");
            }
            this._fontWidth = it.getUint32();
            this._fontHeight = it.getUint32();
            this._pixelSize = it.getUint32();
            const widthTableSize = it.getUint32();
            const pixelTableSize = it.getUint32();
            this._widthTable = Array(widthTableSize);
            this._pixelTable = Array(pixelTableSize);
            for (let i = 0; i < widthTableSize; i++) {
                this._widthTable[i] = {
                    rangeStart: it.getUint32(),
                    rangeEnd: it.getUint32(),
                    width: it.getUint8(),
                };
            }
            for (let i = 0; i < pixelTableSize; i++) {
                const rangeStart = it.getUint32();
                const rangeEnd = it.getUint32();
                const pixels = [];
                for (let j = rangeStart; j <= rangeEnd; j++) {
                    pixels.push(it.getBytes(this._pixelSize));
                }
                this._pixelTable[i] = {
                    rangeStart: rangeStart,
                    rangeEnd: rangeEnd,
                    pixels: pixels,
                };
            }
            it.dispose();
        }
        static loadFont(url, progress = undefined) {
            return new Promise((resolve, reject) => {
                const xhr = new XMLHttpRequest();
                xhr.open('GET', url, true);
                xhr.onload = () => {
                    if (xhr.readyState === 4 && ((xhr.status === 200) || (xhr.responseURL.startsWith("file://") && xhr.status === 0))) {
                        resolve(xhr.response);
                    }
                    else {
                        reject(new Error(xhr.statusText));
                    }
                };
                if (progress !== null && progress !== undefined) {
                    xhr.onprogress = (e) => progress(e.loaded, e.total);
                }
                xhr.onerror = () => { reject(new Error(xhr.statusText)); };
                xhr.responseType = 'arraybuffer';
                xhr.send(null);
            }).then((arraybuffer) => {
                return new BitmapFont(arraybuffer);
            });
        }
        static search(table, codePoint) {
            let start = 0;
            let end = table.length - 1;
            while (start <= end) {
                const mid = ((end - start) >> 1) + start;
                if (table[mid].rangeStart > codePoint) {
                    end = mid - 1;
                }
                else if (table[mid].rangeEnd < codePoint) {
                    start = mid + 1;
                }
                else {
                    return table[mid];
                }
            }
            return undefined;
        }
        getWidth(codePoint) {
            if (0x00 === codePoint) {
                return 0;
            }
            if (0x01 <= codePoint && codePoint <= 0x1F) {
                return 1; // control code
            }
            const ret = BitmapFont.search(this._widthTable, codePoint);
            if (ret === undefined || ret === null) {
                return undefined;
            }
            return ret.width;
        }
        getPixelWidth(codePoint, defaultWidth) {
            const ret = this.getWidth(codePoint);
            if (ret === undefined || ret === null) {
                return defaultWidth;
            }
            return ret * this._fontWidth;
        }
        getPixel(codePoint) {
            const ret = BitmapFont.search(this._pixelTable, codePoint);
            if (ret === undefined || ret === null) {
                return undefined;
            }
            return ret.pixels[codePoint - ret.rangeStart];
        }
        measureStr(str) {
            return this.measureUtf32(Unicode.strToUtf32(str));
        }
        measureUtf32(utf32Str) {
            let w = 0;
            let h = 0;
            let xx = 0;
            let yy = 0;
            const size = this._fontHeight;
            for (const utf32Ch of utf32Str) {
                const width = this.getPixelWidth(utf32Ch);
                if (width === undefined) {
                    xx += this._fontWidth;
                    w = Math.max(w, xx);
                    h = Math.max(h, yy + this._fontHeight);
                }
                else if (utf32Ch === 0x0A) {
                    yy += size;
                    xx = 0;
                }
                else {
                    xx += width;
                    w = Math.max(w, xx);
                    h = Math.max(h, yy + this._fontHeight);
                }
            }
            return [w, h];
        }
        drawStr(x, y, str, drawChar) {
            this.drawUtf32(x, y, Unicode.strToUtf32(str), drawChar);
        }
        drawUtf32(x, y, utf32Str, drawChar) {
            let xx = ~~x;
            let yy = ~~y;
            const size = this._fontHeight;
            for (const utf32Ch of utf32Str) {
                const width = this.getPixelWidth(utf32Ch);
                if (width === undefined) {
                    xx += this._fontWidth;
                    continue;
                }
                if (utf32Ch === 0x0A) {
                    yy += size;
                    xx = x;
                    continue;
                }
                else {
                    const pixel = this.getPixel(utf32Ch);
                    if (pixel) {
                        drawChar(xx, yy, size, pixel);
                    }
                    xx += width;
                }
            }
        }
    }
    class TextBufferCursor {
        constructor(line, row = 0) {
            this.use = true;
            this.line = line;
            this.row = row;
        }
        duplicate() {
            return this.line.parent.duplicateCursor(this);
        }
        dispose() {
            this.line.parent.disposeCursor(this);
        }
        checkValid() {
            assert(this.use);
            assert(this.line !== null);
            assert(0 <= this.row);
            assert(this.row <= this.line.buffer.length);
        }
        static equal(x, y) {
            if ((x.use === false) || (y.use === false)) {
                return false;
            }
            return (x.line === y.line) && (x.row === y.row);
        }
        copyTo(other) {
            if (this.use === false || other.use === false) {
                throw new Error();
            }
            other.line = this.line;
            other.row = this.row;
        }
    }
    class TextBufferLine {
        constructor(parent) {
            this.parent = parent;
            this.buffer = new GapBuffer();
            this.prev = null;
            this.next = null;
        }
        dispose() {
            this.parent = null;
            this.buffer.dispose();
            this.buffer = null;
            this.prev = null;
            this.next = null;
        }
    }
    class TextBuffer {
        getTotalLength() {
            return this.totalLength;
        }
        clear() {
            this.lines.next = null;
            this.lines.prev = null;
            this.lines.buffer.clear();
            this.totalLength = 0;
            for (const cur of this.cursors) {
                if (cur.use === false) {
                    this.cursors.delete(cur);
                    continue;
                }
                cur.line = this.lines;
                cur.row = 0;
            }
            ;
        }
        allocateCursor() {
            var newCursor = new TextBufferCursor(this.lines);
            this.cursors.add(newCursor);
            return newCursor;
        }
        duplicateCursor(cursor) {
            if ((!this.cursors.has(cursor)) || (cursor.use === false)) {
                throw new Error();
            }
            var newCursor = new TextBufferCursor(cursor.line, cursor.row);
            this.cursors.add(newCursor);
            return newCursor;
        }
        disposeCursor(cursor) {
            if ((!this.cursors.has(cursor)) || (cursor.use === false)) {
                throw new Error();
            }
            this.cursors.delete(cursor);
            cursor.use = false;
            cursor.line = null;
            cursor.row = -1;
        }
        constructor() {
            this.lines = new TextBufferLine(this);
            this.totalLength = 0;
            this.cursors = new Set();
        }
        checkValidCursor(cursor) {
            if ((!this.cursors.has(cursor)) || (cursor.use === false)) {
                throw new Error();
            }
            cursor.checkValid();
        }
        deleteCharacterOnCursor(cursor) {
            if ((!this.cursors.has(cursor)) || (cursor.use === false)) {
                throw new Error();
            }
            if (cursor.line.buffer.length === cursor.row) {
                // 末尾文字を削除＝後ろの行があれば削除
                if (cursor.line.next !== null) {
                    const next = cursor.line.next;
                    cursor.line.buffer.append(next.buffer);
                    this.removeLine(next);
                    this.totalLength--;
                }
            }
            else {
                // カーソルの位置の文字を削除
                if (cursor.line.buffer.deleteAfter(cursor.row, 1) === false) {
                    return;
                }
                this.totalLength--;
            }
        }
        removeLine(buf) {
            if (buf.prev === null && buf.next === null) {
                // １行だけ存在する場合
                // １行目をクリア
                buf.buffer.clear();
                // 行内にあるカーソルを先頭に移動させる
                for (const cur of this.cursors) {
                    if (cur.use === false) {
                        this.cursors.delete(cur);
                        continue;
                    }
                    if (cur.line !== buf) {
                        continue;
                    }
                    // 最初の行に対する行削除なので最初の行に設定
                    cur.row = 0;
                }
            }
            else {
                // ２行以上存在する場合
                // １行目が削除対象の場合、先頭行を２行目にずらす。
                if (this.lines === buf && buf.next !== null) {
                    this.lines = buf.next;
                }
                // リンクリストから削除行を削除する
                if (buf.next !== null) {
                    buf.next.prev = buf.prev;
                }
                if (buf.prev !== null) {
                    buf.prev.next = buf.next;
                }
                // 削除した行内にあるカーソルは移動させる
                for (const cur of this.cursors) {
                    if (cur.use === false) {
                        this.cursors.delete(cur);
                        continue;
                    }
                    if (cur.line !== buf) {
                        continue;
                    }
                    if (buf.next !== null) {
                        //   次の行がある場合：次の行の行頭に移動する
                        cur.line = buf.next;
                        cur.row = 0;
                    }
                    else if (buf.prev !== null) {
                        //   次の行がなく前の行がある場合：前の行の行末に移動する。
                        cur.line = buf.prev;
                        cur.row = buf.prev.buffer.length;
                    }
                    else {
                        //   どちらもない場合：最初の行に対する行削除なので最初の行に設定
                        cur.line = this.lines;
                        cur.row = 0;
                    }
                }
                // 行情報を破棄
                buf.dispose();
            }
        }
        deleteMany(s, len) {
            const start = this.duplicateCursor(s);
            // １文字づつ消す
            while (len > 0) {
                this.deleteCharacterOnCursor(start);
                len--;
            }
            this.disposeCursor(start);
        }
        takeCharacterOnCursor(cursor) {
            if ((!this.cursors.has(cursor)) || (cursor.use === false)) {
                throw new Error();
            }
            if (cursor.line.buffer.length === cursor.row) {
                if (cursor.line.next === null) {
                    return 0x00; // 終端なのでヌル文字を返す
                }
                else {
                    return 0x0A; // 改行文字を返す
                }
            }
            return cursor.line.buffer.get(cursor.row);
        }
        insertCharacterOnCursor(cursor, ch) {
            this.checkValidCursor(cursor);
            if (ch === 0x0A) // 0x0A = \n
             {
                // 改行は特別扱い
                // 新しい行バッファを確保
                const nb = new TextBufferLine(this);
                // 現在カーソルがあるバッファの後ろに連結
                const buf = cursor.line;
                nb.prev = buf;
                nb.next = buf.next;
                if (buf.next !== null) {
                    buf.next.prev = nb;
                }
                buf.next = nb;
                // カーソル位置から行末までを新しい行バッファに移動させる
                const len = cursor.row;
                const size = buf.buffer.length - len;
                for (var i = 0; i < size; i++) {
                    nb.buffer.insert(i, buf.buffer.get(len + i));
                }
                buf.buffer.deleteAfter(len, size);
                // 移動させた範囲にあるカーソルを新しい位置に変更する
                for (const cur of this.cursors) {
                    if (cur.use === false) {
                        this.cursors.delete(cur);
                        continue;
                    }
                    if (cur === cursor) {
                        continue;
                    }
                    if (cur.line === buf && cur.row > len) {
                        cur.line = nb;
                        cur.row -= len;
                    }
                }
                // 行を増やす＝改行挿入なので文字は増えていないが仮想的に1文字使っていることにする
                this.totalLength++;
            }
            else {
                cursor.line.buffer.insert(cursor.row, ch);
                this.totalLength++;
            }
            return true;
        }
        // カーソル位置の文字を上書きする
        overwriteCharacterOnCursor(cursor, ch) {
            this.checkValidCursor(cursor);
            if (cursor.line.buffer.length === cursor.row || ch === 0x0A) {
                this.insertCharacterOnCursor(cursor, ch);
            }
            else {
                this.totalLength++;
                cursor.line.buffer.set(cursor.row, ch);
            }
            return true;
        }
        // テキストバッファカーソルを１文字前に移動させる
        // falseはcursorが元々先頭であったことを示す。
        cursorBackward(cursor) {
            this.checkValidCursor(cursor);
            if (cursor.row > 0) {
                // カーソル位置がバッファ先頭以外の場合
                cursor.row--;
                this.checkValidCursor(cursor);
                return true;
            }
            else {
                // カーソル位置がバッファ先頭の場合は前の行に動かす
                if (cursor.line.prev !== null) {
                    cursor.line = cursor.line.prev;
                    cursor.row = cursor.line.buffer.length;
                    this.checkValidCursor(cursor);
                    return true;
                }
                else {
                    return false;
                }
            }
        }
        // テキストバッファカーソルを１文字後ろに移動させる
        // falseはcursorが元々終端であったことを示す。
        cursorForward(cursor) {
            this.checkValidCursor(cursor);
            if (cursor.row < cursor.line.buffer.length) {
                // カーソル位置がバッファ先頭以外の場合
                cursor.row++;
                this.checkValidCursor(cursor);
                return true;
            }
            else {
                // カーソル位置がバッファ末尾の場合は次の行に動かす
                if (cursor.line.next !== null) {
                    cursor.line = cursor.line.next;
                    cursor.row = 0;
                    this.checkValidCursor(cursor);
                    return true;
                }
                else {
                    return false;
                }
            }
        }
        // カーソルを文字列中の位置関係で比較する
        // c1がc2より前にある場合は -1 、c2がc1より前にある場合は1、同じ位置にある場合は0を返す
        compareCursor(c1, c2) {
            this.checkValidCursor(c1);
            this.checkValidCursor(c2);
            if (c1.line === c2.line) {
                if (c1.row < c2.row) {
                    return -1;
                }
                if (c1.row === c2.row) {
                    return 0;
                }
                if (c1.row > c2.row) {
                    return 1;
                }
            }
            for (let p = this.lines; p !== null; p = p.next) {
                if (p === c1.line) {
                    return -1;
                }
                if (p === c2.line) {
                    return 1;
                }
            }
            throw new Error(); // 不正なカーソルが渡された
        }
        // カーソルが同一位置を示しているか判定
        // CompareCursorより高速
        isEqualCursor(c1, c2) {
            this.checkValidCursor(c1);
            this.checkValidCursor(c2);
            if (c1.line === c2.line) {
                if (c1.row === c2.row) {
                    return true;
                }
            }
            return false;
        }
        // カーソル位置が文書の先頭か判定
        isBeginningOfDocument(c) {
            this.checkValidCursor(c);
            return (c.line.prev === null && c.row === 0);
        }
        isEndOfDocument(c) {
            this.checkValidCursor(c);
            if (c.line.buffer.length === c.row && c.line.next === null) {
                return true;
            }
            return false;
        }
        // カーソルを行頭に移動
        moveToBeginningOfLine(cur) {
            this.checkValidCursor(cur);
            cur.row = 0;
        }
        // カーソルを文書の先頭に移動
        moveToBeginningOfDocument(cur) {
            this.checkValidCursor(cur);
            cur.line = this.lines;
            cur.row = 0;
        }
        // 区間[start, end)の文字数をカウントする。endは含まれないので注意
        rangeLength(start, end) {
            this.checkValidCursor(start);
            this.checkValidCursor(end);
            let n = 0;
            const c = this.duplicateCursor(start);
            for (;;) {
                if (this.isEqualCursor(c, end)) {
                    break;
                }
                this.cursorForward(c);
                n++;
            }
            this.disposeCursor(c);
            return n;
        }
    }
    class TextVram {
        get width() { return this._width; }
        get height() { return this._height; }
        constructor(font, width, height) {
            this._font = font;
            this._width = width;
            this._height = height;
            this._palette = new Uint32Array(16);
            for (let i = 0; i < 8; i++) {
                this.setPaletteColor(i, (255 * (i >> 2)), (255 * (i & 1)), (255 * ((i >> 1) & 1)));
            }
            for (let i = 8; i < 16; i++) {
                //8以上は全て白に初期化
                this.setPaletteColor(i, 255, 255, 255);
            }
            this._pixels = new Uint32Array(this._width * this._height * 2);
            this._backgroundColor = 0;
            this._textColor = 7;
            this._cursorIndex = 0;
            this.clear();
        }
        resize(w, h) {
            this._width = w;
            this._height = h;
            this._pixels = new Uint32Array(this._width * this._height * 2);
            this._cursorIndex = 0;
            this.clear();
        }
        clear() {
            this._pixels.fill(0);
        }
        setPaletteColor(index, r, g, b) {
            const color = (0xFF << 24) | (r << 16) | (g << 8) | (b << 0);
            this._palette[index & 0x0F] = color;
        }
        getPaletteColor(index) {
            return this._palette[index & 0x0F];
        }
        getCursorIndex() {
            return this._cursorIndex;
        }
        getCursorPosition() {
            return {
                x: ~~(this._cursorIndex % this._width),
                y: ~~(this._cursorIndex / this._width)
            };
        }
        setCursorPosition(x, y) {
            if (x < 0 || x >= this._width || y < 0 || y >= this._height) {
                return;
            }
            this._cursorIndex = y * this._width + x;
        }
        getPixels() { return this._pixels; }
        getPalette() { return this._palette; }
        getBackgroundColor() { return this._backgroundColor; }
        setBackgroundColor(color) { this._backgroundColor = color; }
        fillBackgroundColor(x, y, length, palette) {
            if (x < 0 || x >= this._width || y < 0 || y >= this._height) {
                return;
            }
            let n = y * this._width + x;
            while (length-- > 0) {
                const index = n * 2 + 1;
                this._pixels[index] = (this._pixels[index] & 0xFFFFFF0F) | (palette << 4);
                n++;
            }
        }
        getTextColor() { return this._textColor; }
        setTextColor(color) { this._textColor = color; }
        fillTextColor(pos, length, palette) {
            if (pos.x < 0 || pos.x >= this._width || pos.y < 0 || pos.y >= this._height) {
                return;
            }
            let n = pos.y * this._width + pos.x;
            while (length-- > 0) {
                const index = n * 2 + 1;
                this._pixels[index] = (this._pixels[index] & 0xFFFFFFF0) | (palette << 0);
                n++;
            }
        }
        scrollUp() {
            this._pixels.copyWithin(0, this._width * 2, this._pixels.length - this._width * 2);
            this._pixels.fill(0, this._pixels.length - this._width * 2, this._pixels.length);
        }
        putChar(utf32) {
            //カーソル位置にテキストコードnを1文字表示し、カーソルを1文字進める
            //画面最終文字表示してもスクロールせず、次の文字表示時にスクロールする
            let sz = this._font.getWidth(utf32);
            if (this._cursorIndex < 0 || this._cursorIndex > this._width * this._height) {
                // 画面外への描画
                return;
            }
            if (this._cursorIndex + sz - 1 >= this._width * this._height) {
                // 画面末尾での描画
                this.scrollUp();
                this._cursorIndex = this._width * (this._height - 1);
            }
            if (utf32 === 0x0A) {
                //改行
                this._cursorIndex += this._width - (this._cursorIndex % this._width);
            }
            else {
                // 残り空きセル数を取得
                const rest = this._width - (this._cursorIndex % this._width);
                if (rest < sz) {
                    // 文字を挿入すると画面端をはみ出す場合、その位置に空白入れて改行したことにする
                    this._pixels[this._cursorIndex * 2 + 0] = 0;
                    // 文字色と背景色はそのままにしておくほうがいいのかな
                    // this.pixels[this.cursorIndex * 2 + 1] = (this.pixels[this.cursorIndex * 2 + 1] & 0xFFFFFFF0) | (this.textColor << 0);
                    // this.pixels[this.cursorIndex * 2 + 1] = (this.pixels[this.cursorIndex * 2 + 1] & 0xFFFFFF0F) | (this.backgroundColor << 4);
                    this._cursorIndex += this._width - (this._cursorIndex % this._width);
                    this.putChar(utf32);
                }
                else {
                    // 文字を挿入する
                    this._pixels[this._cursorIndex * 2 + 0] = utf32;
                    this._pixels[this._cursorIndex * 2 + 1] = (this._pixels[this._cursorIndex * 2 + 1] & 0xFFFFFFF0) | (this._textColor << 0);
                    this._pixels[this._cursorIndex * 2 + 1] = (this._pixels[this._cursorIndex * 2 + 1] & 0xFFFFFF0F) | (this._backgroundColor << 4);
                    this._cursorIndex++;
                    sz--;
                    while (sz > 0) {
                        this._pixels[this._cursorIndex * 2 + 0] = 0;
                        this._pixels[this._cursorIndex * 2 + 1] = 0;
                        this._cursorIndex++;
                        sz--;
                    }
                }
            }
        }
        putStr(utf32Str) {
            for (let i = 0; i < utf32Str.length; i++) {
                this.putChar(utf32Str[i]);
            }
        }
        //カーソル位置に符号なし整数nを10進数表示
        putDigit(n) {
            const n1 = ~~(n / 10);
            let d = 1;
            while (n1 >= d) {
                d *= 10;
            }
            while (d !== 0) {
                this.putChar(0x30 + ~~(n / d));
                n %= d;
                d = ~~(d / 10);
            }
        }
        //カーソル位置に符号なし整数nをe桁の10進数表示（前の空き桁部分はスペースで埋める）
        putDigit2(n, e) {
            if (e === 0) {
                return;
            }
            const n1 = ~~(n / 10);
            let d = 1;
            e--;
            while (e > 0 && n1 >= d) {
                d *= 10;
                e--;
            }
            if (e === 0 && n1 > d) {
                n %= d * 10;
            }
            for (; e > 0; e--) {
                this.putChar(0x20);
            }
            while (d !== 0) {
                this.putChar(0x30 + ~~(n / d));
                n %= d;
                d = ~~(d / 10);
            }
        }
        // カーソル位置p, 画面幅widthにおいて、 Utf32文字列strの範囲[head,cur) を入力した場合の入力後のカーソル位置を求める
        calcCursorPosition(str, head, cur, p, width) {
            for (;;) {
                if (head === cur) {
                    return;
                }
                // １文字先（移動先）を読む
                if (str.length === head) {
                    // 1文字先が文末なので打ち切り
                    return;
                }
                const c1 = str[head];
                const w1 = this._font.getWidth(head++);
                // ２文字先を読む（折り返し判定のため）
                const w2 = this._font.getWidth(head++);
                // 全角文字の回り込みを考慮して改行が必要か判定
                if (c1 === 0x0A) {
                    p.x = 0;
                    p.y++;
                }
                else if (p.x + w1 + w2 > width) {
                    p.x = 0;
                    p.y++;
                }
                else {
                    p.x += w1;
                }
            }
        }
    }
    const COLOR_NORMALTEXT = 7; //通常テキスト色
    const COLOR_NORMALTEXT_BG = 0; //通常テキスト背景
    const COLOR_ERRORTEXT = 4; //エラーメッセージテキスト色
    const COLOR_AREASELECTTEXT = 4; //範囲選択テキスト色
    const COLOR_BOTTOMLINE = 5; //画面最下行のテキスト色
    const COLOR_CURSOR = 6; //カーソル色
    const COLOR_BOTTOMLINE_BG = 8; //画面最下行背景
    class TextEditor {
        constructor(font, vram) {
            this.textBuffer = null;
            this.font = null;
            this.textVram = null;
            this.insertMode = true;
            this.needUpdate = false;
            /**
            * 現在のカーソル位置に対応するテキスト位置
            * [0] は現在位置を示す値
            * [1] はsave_cursorで退避された値
            */
            this.currentCursor = [null, null];
            /**
            * 現在表示中の画面の左上に対応するテキスト位置
            * [0] は現在位置を示す値
            * [1] はsave_cursorで退避された値
            */
            this.displayLeftTopCursor = [null, null];
            /**
            * 画面上でのカーソルの位置
            * [0] は現在位置を示す値
            * [1] はsave_cursorで退避された値
            */
            this.currentCursorPosition = [null, null];
            //上下移動時の仮カーソルX座標
            this.cachedCursorScreenPosX = 0;
            // 範囲選択時のカーソルのスタート位置
            this.selectRangeStartCursor = null;
            // 範囲選択時のカーソルのスタート座標（画面上）
            this.selectStartCursorScreenPos = null;
            //保存後に変更されたかを表すフラグ
            this.edited = false;
            // クリップボードデータ
            this.clipboard = null;
            ///////////////////
            this.menuStr = Unicode.strToUtf32("F1:LOAD F2:SAVE   F4:NEW ");
            this.font = font;
            this.textVram = vram;
            // テキストバッファの初期化
            this.textBuffer = new TextBuffer();
            // カーソルをリセット
            this.currentCursor = [this.textBuffer.allocateCursor(), null];
            this.displayLeftTopCursor = [this.textBuffer.allocateCursor(), null];
            this.currentCursorPosition = [{ x: 0, y: 0 }, { x: 0, y: 0 }];
            this.selectRangeStartCursor = null;
            this.selectStartCursorScreenPos = { x: 0, y: 0 };
            // カーソル位置を先頭に
            this.cursorMoveToBeginningOfDocument();
            //編集済みフラグクリア
            this.edited = false;
            // 挿入モードに設定
            this.insertMode = true;
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }
        getCursorScreenPosX() { return this.currentCursorPosition[0].x; }
        getCursorScreenPosY() { return this.currentCursorPosition[0].y; }
        get editorScreenHeight() {
            return this.textVram.height - 1;
        }
        resize(w, h) {
            // ディスプレイの左上位置に対応する行の位置カーソルを取得
            const leftTopCursor = this.displayLeftTopCursor[0].duplicate();
            leftTopCursor.row = 0;
            // 現在の位置のカーソルを取得
            const currentCursor = this.currentCursor[0].duplicate();
            // vramをリサイズ
            this.textVram.resize(w, h);
            // 左上に対応する行のカーソル位置に設定
            leftTopCursor.copyTo(this.displayLeftTopCursor[0]);
            leftTopCursor.copyTo(this.currentCursor[0]);
            this.currentCursorPosition[0].x = 0;
            this.currentCursorPosition[0].y = 0;
            // 仮想的なカーソル位置を求める。
            const screenPos = this.calcScreenPosFromLineHead(leftTopCursor, currentCursor);
            //// カーソルのY位置が画面中央に来るような位置を求めてスクロールする。
            const scrollY = (screenPos.y <= (h >> 1)) ? 0 : screenPos.y - (h >> 1);
            for (let i = 0; i < scrollY; i++) {
                if (this.screenScrollDown() === false) {
                    break;
                }
                screenPos.y--;
            }
            this.currentCursorPosition[0].x = screenPos.x;
            this.currentCursorPosition[0].y = screenPos.y;
            currentCursor.copyTo(this.currentCursor[0]);
            currentCursor.dispose();
            leftTopCursor.dispose();
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }
        clear() {
            if (this.selectRangeStartCursor !== null) {
                this.selectRangeStartCursor.dispose();
                this.selectRangeStartCursor = null;
            }
            this.currentCursorPosition = [{ x: 0, y: 0 }, { x: 0, y: 0 }];
            // カーソル位置を先頭に
            this.cursorMoveToBeginningOfDocument();
            //編集済みフラグクリア
            this.edited = false;
            // 挿入モードに設定
            this.insertMode = true;
            // バッファクリア（カーソルも全部補正される）
            this.textBuffer.clear();
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }
        /**
         * 画面上のカーソル位置と再算出した位置情報が一致しているか検査（デバッグ用）
         */
        validateDisplayCursorPos() {
            const p = this.calcScreenPosFromLineHead(this.displayLeftTopCursor[0], this.currentCursor[0]);
            assert((p.x === this.currentCursorPosition[0].x) && (p.y === this.currentCursorPosition[0].y));
        }
        // 画面上の位置と折り返しを考慮に入れながらカーソルと画面上の位置を連動させてひとつ進める
        iterateCursorWithScreenPos(p, cur) {
            // １文字先（移動先）を読む
            const c1 = this.textBuffer.takeCharacterOnCursor(cur);
            if (c1 === 0x0000) {
                // 1文字先が文末なので打ち切り
                return false;
            }
            const w1 = this.font.getWidth(c1);
            // ２文字先を読む（後述の折り返し判定のため）
            this.textBuffer.cursorForward(cur);
            const c2 = this.textBuffer.takeCharacterOnCursor(cur);
            const w2 = this.font.getWidth(c2);
            // タブ・折り返しを考慮して文字の位置を決定
            if (c1 === 0x0A) {
                // １文字目が改行文字の場合は次行に移動
                p.x = 0;
                p.y++;
            }
            else if (c1 === 0x09) {
                // タブ文字の場合
                const tabSize = 4 - (p.x % 4);
                // タブの位置に応じて処理変更
                if (p.x + tabSize >= this.textVram.width) {
                    // タブで行末まで埋まる
                    p.x = 0;
                    p.y++;
                }
                else {
                    // 行末までは埋まらないのでタブを入れる
                    p.x += tabSize;
                    // 次の文字を改めてチェックして折り返すかどうか考える
                    if (p.x + w2 > this.textVram.width) {
                        p.x = 0;
                        p.y++;
                    }
                }
            }
            else if (p.x + w1 + w2 > this.textVram.width) {
                // 一文字目、二文字目両方を受け入れると行末を超える場合
                p.x = 0;
                p.y++;
            }
            else {
                p.x += w1;
            }
            return true;
        }
        // headのスクリーン座標を(0,0)とした場合のcurのスクリーン座標位置を算出
        calcScreenPosFromLineHead(head, cur) {
            head.checkValid();
            cur.checkValid();
            const p = { x: 0, y: 0 };
            const h = head.duplicate();
            while (TextBufferCursor.equal(h, cur) === false) {
                if (this.iterateCursorWithScreenPos(p, h) === false) {
                    break;
                }
            }
            h.dispose();
            return p;
        }
        // 画面原点を基準に現在の行の画面上での最左位置に対応する位置にカーソルを移動
        moveCursorToDisplayLineHead(textPosition, textBufferCursor) {
            textBufferCursor.checkValid();
            const pos = { x: 0, y: 0 };
            const prevPos = { x: 0, y: 0 };
            const cur = this.displayLeftTopCursor[0].duplicate();
            const prevCur = this.displayLeftTopCursor[0].duplicate();
            // タブや折り返しなどがあるため、単純に逆順に辿ることができない場合もあって場合分けすると面倒くさい
            //画面原点に対応するバッファカーソルは常に計算済みなのでそこからの距離を求める
            let result;
            for (;;) {
                prevPos.x = pos.x;
                prevPos.y = pos.y;
                cur.copyTo(prevCur);
                if (pos.y === textPosition.y && pos.x === 0) {
                    textPosition.x = pos.x;
                    textPosition.y = pos.y;
                    cur.copyTo(textBufferCursor);
                    result = true;
                    break;
                }
                else if (this.iterateCursorWithScreenPos(pos, cur) === false) {
                    textPosition.x = prevPos.x;
                    textPosition.y = prevPos.y;
                    prevCur.copyTo(textBufferCursor);
                    result = false;
                    break;
                }
                else if (pos.y === textPosition.y) {
                    assert(pos.x === 0);
                    textPosition.x = pos.x;
                    textPosition.y = pos.y;
                    cur.copyTo(textBufferCursor);
                    result = true;
                    break;
                }
            }
            cur.dispose();
            prevCur.dispose();
            return result;
        }
        // 画面上で現在行の末尾まで移動
        moveCursorToDisplayLineTail(textPosition, textBufferCursor) {
            textBufferCursor.checkValid();
            const pos = { x: textPosition.x, y: textPosition.y };
            const prevPos = { x: 0, y: 0 };
            const cur = textBufferCursor.duplicate();
            const prevCur = textBufferCursor.duplicate();
            let result;
            for (;;) {
                prevPos.x = pos.x;
                prevPos.y = pos.y;
                cur.copyTo(prevCur);
                if (this.iterateCursorWithScreenPos(pos, cur) === false) {
                    textPosition.x = pos.x;
                    textPosition.y = pos.y;
                    cur.copyTo(textBufferCursor);
                    result = false;
                    break;
                }
                else if (pos.y === textPosition.y + 1) {
                    textPosition.x = prevPos.x;
                    textPosition.y = prevPos.y;
                    prevCur.copyTo(textBufferCursor);
                    result = true;
                    break;
                }
            }
            cur.dispose();
            prevCur.dispose();
            this.needUpdate = true;
            return result;
        }
        // 画面上で現在の文字の手前まで移動
        moveCursorToPrev(textPosition, textBufferCursor) {
            textBufferCursor.checkValid();
            const pos = { x: 0, y: 0 };
            const prevPos = { x: 0, y: 0 };
            const cur = this.displayLeftTopCursor[0].duplicate();
            const prevCur = this.displayLeftTopCursor[0].duplicate();
            while (!(pos.y === textPosition.y && pos.x === textPosition.x)) {
                prevPos.x = pos.x;
                prevPos.y = pos.y;
                cur.copyTo(prevCur);
                assert(this.iterateCursorWithScreenPos(pos, cur));
            }
            textPosition.x = prevPos.x;
            textPosition.y = prevPos.y;
            prevCur.copyTo(textBufferCursor);
            cur.dispose();
            prevCur.dispose();
            return true;
        }
        // 指定したディスプレイ位置になるまでカーソル位置を進める
        moveCursorToDisplayPos(targetTextPosition, resultTextPosition, resultTextBufferCursor) {
            const pos = { x: 0, y: 0 };
            const prevPos = { x: 0, y: 0 };
            const cur = this.displayLeftTopCursor[0].duplicate();
            const prevCur = this.displayLeftTopCursor[0].duplicate();
            let ret;
            for (;;) {
                if (pos.y === targetTextPosition.y && pos.x >= targetTextPosition.x) {
                    resultTextPosition.x = pos.x;
                    resultTextPosition.y = pos.y;
                    ret = true;
                    break;
                }
                if (pos.y > targetTextPosition.y) {
                    resultTextPosition.x = prevPos.x;
                    resultTextPosition.y = prevPos.y;
                    prevCur.copyTo(cur);
                    ret = true;
                    break;
                }
                prevPos.x = pos.x;
                prevPos.y = pos.y;
                cur.copyTo(prevCur);
                if (this.iterateCursorWithScreenPos(pos, cur) === false) {
                    // 文書末に到達
                    resultTextPosition.x = prevPos.x;
                    resultTextPosition.y = prevPos.y;
                    prevCur.copyTo(cur);
                    ret = false;
                    break;
                }
            }
            cur.copyTo(resultTextBufferCursor);
            cur.dispose();
            prevCur.dispose();
            return ret;
        }
        // カーソル位置で１文字上書きする
        overwriteCharacter(utf32) {
            if (this.textBuffer.overwriteCharacterOnCursor(this.currentCursor[0], utf32) === false) {
                return false;
            }
            this.currentCursorPosition[0] = this.calcScreenPosFromLineHead(this.displayLeftTopCursor[0], this.currentCursor[0]);
            this.validateDisplayCursorPos();
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
            return true;
        }
        // カーソル位置に１文字挿入する
        insertCharacter(utf32) {
            if (this.textBuffer.insertCharacterOnCursor(this.currentCursor[0], utf32) === false) {
                return false;
            }
            this.currentCursorPosition[0] = this.calcScreenPosFromLineHead(this.displayLeftTopCursor[0], this.currentCursor[0]);
            this.validateDisplayCursorPos();
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
            return true;
        }
        // 画面を１行下にスクロールする
        screenScrollDown() {
            const c = this.displayLeftTopCursor[0].duplicate();
            const p = { x: 0, y: 0 };
            for (;;) {
                if (p.y === 1) {
                    c.copyTo(this.displayLeftTopCursor[0]);
                    this.currentCursorPosition[0].y -= 1;
                    c.dispose();
                    return true;
                }
                if (this.iterateCursorWithScreenPos(p, c) === false) {
                    // 下の行がないのでどうしようもない
                    c.dispose();
                    return false;
                }
            }
        }
        // 画面を１行上にスクロールする
        screenScrollUp() {
            // カーソルの画面Ｙ座標が最上列なので左上座標の更新が必要
            const lt = this.displayLeftTopCursor[0].duplicate();
            if (this.textBuffer.cursorBackward(lt)) {
                // 左上から１文字戻る＝画面上における前の行の末尾にバッファ上のカーソルを移動
                // 行頭位置を算出
                const c = lt.duplicate();
                this.textBuffer.moveToBeginningOfLine(c);
                // 行頭位置を(0,0)点としてカーソルltの画面上位置を計算
                const lp = this.calcScreenPosFromLineHead(c, lt);
                // ltの画面位置を超えない位置の行頭をサーチ
                const p = { x: 0, y: 0 };
                while (p.y !== lp.y) {
                    this.iterateCursorWithScreenPos(p, c);
                }
                // 見つかったところが新しい左上位置
                c.copyTo(this.displayLeftTopCursor[0]);
                this.currentCursorPosition[0].y += 1;
                this.validateDisplayCursorPos();
                c.dispose();
                lt.dispose();
                return true;
            }
            else {
                // this.DisplayLeftTopはバッファ先頭なので戻れない
                lt.dispose();
                return false;
            }
        }
        ////////////////////
        // カーソルを1つ左に移動
        cursorMoveLeftBody() {
            if (this.currentCursorPosition[0].x === 0 && this.currentCursorPosition[0].y === 0) {
                if (this.screenScrollUp() === false) {
                    // 上にスクロールできない＝先頭なので戻れない
                    return;
                }
            }
            this.moveCursorToPrev(this.currentCursorPosition[0], this.currentCursor[0]);
            this.cachedCursorScreenPosX = this.currentCursorPosition[0].x;
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }
        // カーソルを1つ左に移動
        cursorMoveLeft() {
            this.validateDisplayCursorPos();
            this.cursorMoveLeftBody();
            this.validateDisplayCursorPos();
        }
        // カーソルを右に移動
        cursorMoveRightBody() {
            if (this.iterateCursorWithScreenPos(this.currentCursorPosition[0], this.currentCursor[0])) {
                if (this.currentCursorPosition[0].y === this.editorScreenHeight) {
                    // カーソルを進めることに成功したらカーソル位置が画面最下段からはみ出たので
                    // 画面を一行下にスクロールする
                    this.screenScrollDown();
                    // 画面更新が必要な状態に設定
                    this.needUpdate = true;
                }
            }
            this.cachedCursorScreenPosX = this.currentCursorPosition[0].x;
        }
        // カーソルを右に移動
        cursorMoveRight() {
            this.validateDisplayCursorPos();
            this.cursorMoveRightBody();
            this.validateDisplayCursorPos();
        }
        cursorMoveUpBody() {
            if (this.currentCursorPosition[0].y === 0) {
                if (this.screenScrollUp() === false) {
                    // 画面を上にスクロールできない＝戻れないので終了
                    return;
                }
            }
            this.validateDisplayCursorPos();
            // 左上の補正が終った
            // 左上位置から画面上で１行上の位置をサーチする
            const target = { x: this.cachedCursorScreenPosX, y: this.currentCursorPosition[0].y - 1 };
            const p = { x: 0, y: 0 };
            const c = this.textBuffer.allocateCursor();
            this.moveCursorToDisplayPos(target, p, c);
            c.copyTo(this.currentCursor[0]);
            this.currentCursorPosition[0] = p;
            c.dispose();
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
            this.validateDisplayCursorPos();
        }
        cursorMoveUp() {
            this.validateDisplayCursorPos();
            this.cursorMoveUpBody();
            this.validateDisplayCursorPos();
        }
        cursorMoveDownBody() {
            // 左上位置からサーチする
            const target = { x: this.cachedCursorScreenPosX, y: this.currentCursorPosition[0].y + 1 };
            const p = { x: 0, y: 0 };
            const c = this.textBuffer.allocateCursor();
            if (this.moveCursorToDisplayPos(target, p, c) === false) {
                if (p.y !== target.y) {
                    // 次の行に移動できていない＝最下段の行で下を押した
                    c.dispose();
                    return;
                }
            }
            c.copyTo(this.currentCursor[0]);
            this.currentCursorPosition[0] = p;
            c.dispose();
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
            // 画面を一行下にスクロールが必要か？
            if (this.currentCursorPosition[0].y === this.editorScreenHeight) {
                // 必要
                if (this.screenScrollDown() === false) {
                    // 移動できない＝スクロールできないので終り
                    return;
                }
            }
            this.validateDisplayCursorPos();
        }
        cursorMoveDown() {
            this.validateDisplayCursorPos();
            this.cursorMoveDownBody();
            this.validateDisplayCursorPos();
        }
        // カーソルを行頭に移動する
        cursorMoveToLineHeadBody() {
            this.moveCursorToDisplayLineHead(this.currentCursorPosition[0], this.currentCursor[0]);
            this.cachedCursorScreenPosX = 0;
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }
        cursorMoveToLineHead() {
            this.validateDisplayCursorPos();
            this.cursorMoveToLineHeadBody();
            this.validateDisplayCursorPos();
        }
        // カーソルを行末に移動する
        cursorMoveToLineTailBody() {
            this.moveCursorToDisplayLineTail(this.currentCursorPosition[0], this.currentCursor[0]);
            this.cachedCursorScreenPosX = this.currentCursorPosition[0].x;
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }
        cursorMoveToLineTail() {
            this.validateDisplayCursorPos();
            this.cursorMoveToLineTailBody();
            this.validateDisplayCursorPos();
        }
        cursorMovePageUpBody() {
            //PageUpキー
            //最上行が最下行になるまでスクロール
            //出力：下記変数を移動先の値に変更
            //cursorbp,cursorix バッファ上のカーソル位置
            //cx,cx2
            //cy
            //disptopbp,disptopix 画面左上のバッファ上の位置
            const prevCursorPosition = this.currentCursorPosition[0].y;
            while (this.currentCursorPosition[0].y > 0) {
                this.cursorMoveUp(); // cy==0になるまでカーソルを上に移動
            }
            let i;
            const prev = this.textBuffer.allocateCursor();
            for (i = 0; i < this.editorScreenHeight - 1; i++) {
                //画面行数-1行分カーソルを上に移動
                this.displayLeftTopCursor[0].copyTo(prev);
                this.cursorMoveUp();
                if (this.textBuffer.isEqualCursor(prev, this.displayLeftTopCursor[0])) {
                    break; //最上行で移動できなかった場合抜ける
                }
            }
            prev.dispose();
            //元のY座標までカーソルを下に移動、1行も動かなかった場合は最上行に留まる
            if (i > 0) {
                while (this.currentCursorPosition[0].y < prevCursorPosition) {
                    this.cursorMoveDown();
                }
            }
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }
        cursorMovePageUp() {
            this.validateDisplayCursorPos();
            this.cursorMovePageUpBody();
            this.validateDisplayCursorPos();
        }
        cursorMovePageDownBody() {
            //PageDownキー
            //最下行が最上行になるまでスクロール
            //出力：下記変数を移動先の値に変更
            //cursorbp,cursorix バッファ上のカーソル位置
            //cx,cx2
            //cy
            //disptopbp,disptopix 画面左上のバッファ上の位置
            const prevCursorPosition = this.currentCursorPosition[0].y;
            while (this.currentCursorPosition[0].y < this.editorScreenHeight - 1) {
                // cy===EDITWIDTH-1になるまでカーソルを下に移動
                const y = this.currentCursorPosition[0].y;
                this.cursorMoveDown();
                if (y === this.currentCursorPosition[0].y) {
                    break; // バッファ最下行で移動できなかった場合抜ける
                }
            }
            let i;
            const prev = this.textBuffer.allocateCursor();
            for (i = 0; i < this.editorScreenHeight - 1; i++) {
                //画面行数-1行分カーソルを下に移動
                this.displayLeftTopCursor[0].copyTo(prev);
                this.cursorMoveDown();
                if (this.textBuffer.isEqualCursor(prev, this.displayLeftTopCursor[0])) {
                    break; //最下行で移動できなかった場合抜ける
                }
            }
            prev.dispose();
            //下端からさらに移動した行数分、カーソルを上に移動、1行も動かなかった場合は最下行に留まる
            if (i > 0) {
                while (this.currentCursorPosition[0].y > prevCursorPosition) {
                    this.cursorMoveUp();
                }
            }
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }
        cursorMovePageDown() {
            this.validateDisplayCursorPos();
            this.cursorMovePageDownBody();
            this.validateDisplayCursorPos();
        }
        cursorMoveToBeginningOfDocument() {
            //カーソルをテキストバッファの先頭に移動
            this.textBuffer.moveToBeginningOfDocument(this.currentCursor[0]);
            //範囲選択モード解除
            if (this.selectRangeStartCursor !== null) {
                this.selectRangeStartCursor.dispose();
                this.selectRangeStartCursor = null;
            }
            // 画面の左上位置をリセット
            this.currentCursor[0].copyTo(this.displayLeftTopCursor[0]);
            // 画面上カーソル位置をリセット
            this.currentCursorPosition[0].x = 0;
            this.currentCursorPosition[0].y = 0;
            // 最後の横移動位置をリセット
            this.cachedCursorScreenPosX = 0;
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }
        ///////////////////
        // 選択範囲をクリップボードにコピー
        copyToClipboard() {
            const bp1 = this.textBuffer.allocateCursor();
            const bp2 = this.textBuffer.allocateCursor();
            //範囲選択モードの場合、開始位置と終了の前後判断して
            //bp1,ix1を開始位置、bp2,ix2を終了位置に設定
            if (this.currentCursorPosition[0].y < this.selectStartCursorScreenPos.y ||
                (this.currentCursorPosition[0].y === this.selectStartCursorScreenPos.y && this.currentCursorPosition[0].x < this.selectStartCursorScreenPos.x)) {
                this.currentCursor[0].copyTo(bp1);
                this.selectRangeStartCursor.copyTo(bp2);
            }
            else {
                this.selectRangeStartCursor.copyTo(bp1);
                this.currentCursor[0].copyTo(bp2);
            }
            const pd = [];
            while (this.textBuffer.isEqualCursor(bp1, bp2) === false) {
                pd.push(this.textBuffer.takeCharacterOnCursor(bp1));
                this.textBuffer.cursorForward(bp1);
            }
            this.clipboard = new Uint32Array(pd);
            bp1.dispose();
            bp2.dispose();
        }
        // クリップボードから貼り付け
        pasteFromClipboard() {
            // クリップボードから貼り付け
            for (let i = 0; i < this.clipboard.length; i++) {
                if (this.insertCharacter(this.clipboard[i]) === false) {
                    break;
                }
                this.cursorMoveRight(); //画面上、バッファ上のカーソル位置を1つ後ろに移動
            }
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }
        ///////////////////
        //範囲選択モード開始時のカーソル開始位置設定
        setSelectedRangeStart() {
            if (this.selectRangeStartCursor !== null) {
                this.selectRangeStartCursor.dispose();
                this.selectRangeStartCursor = null;
            }
            this.selectRangeStartCursor = this.currentCursor[0].duplicate();
            this.selectStartCursorScreenPos.x = this.currentCursorPosition[0].x;
            this.selectStartCursorScreenPos.y = this.currentCursorPosition[0].y;
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }
        getSelectedRangeLength() {
            if (this.selectRangeStartCursor === null) {
                return 0;
            }
            //テキストバッファの指定範囲の文字数をカウント
            //範囲は(cursorbp,cursorix)と(SelectStart.Buffer,SelectStart.Index)で指定
            //後ろ側の一つ前の文字までをカウント
            //選択範囲の開始位置と終了の前後を判断して開始位置と終了位置を設定
            if (this.currentCursorPosition[0].y < this.selectStartCursorScreenPos.y ||
                (this.currentCursorPosition[0].y === this.selectStartCursorScreenPos.y && this.currentCursorPosition[0].x < this.selectStartCursorScreenPos.x)) {
                return this.textBuffer.rangeLength(this.currentCursor[0], this.selectRangeStartCursor);
            }
            else {
                return this.textBuffer.rangeLength(this.selectRangeStartCursor, this.currentCursor[0]);
            }
        }
        // テキストバッファの指定範囲を削除
        deleteSelectedRange() {
            if (this.selectRangeStartCursor === null) {
                return;
            }
            //範囲は(cursorbp,cursorix)と(SelectStart.Buffer,SelectStart.Index)で指定
            //後ろ側の一つ前の文字までを削除
            //削除後のカーソル位置は選択範囲の先頭にし、範囲選択モード解除する
            const n = this.getSelectedRangeLength(); //選択範囲の文字数カウント
            //範囲選択の開始位置と終了位置の前後を判断してカーソルを開始位置に設定
            if (this.currentCursorPosition[0].y > this.selectStartCursorScreenPos.y || (this.currentCursorPosition[0].y === this.selectStartCursorScreenPos.y && this.currentCursorPosition[0].x > this.selectStartCursorScreenPos.x)) {
                this.selectRangeStartCursor.copyTo(this.currentCursor[0]);
                this.currentCursorPosition[0].x = this.selectStartCursorScreenPos.x;
                this.currentCursorPosition[0].y = this.selectStartCursorScreenPos.y;
            }
            this.cachedCursorScreenPosX = this.currentCursorPosition[0].x;
            //範囲選択モード解除
            if (this.selectRangeStartCursor !== null) {
                this.selectRangeStartCursor.dispose();
                this.selectRangeStartCursor = null;
            }
            // 始点からn文字削除
            this.textBuffer.deleteMany(this.currentCursor[0], n);
            this.currentCursorPosition[0] = this.calcScreenPosFromLineHead(this.displayLeftTopCursor[0], this.currentCursor[0]);
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }
        checkSelectedRange() {
            if (this.selectRangeStartCursor !== null &&
                this.currentCursorPosition[0].x === this.selectStartCursorScreenPos.x &&
                this.currentCursorPosition[0].y === this.selectStartCursorScreenPos.y) {
                //選択範囲の開始と終了が重なったら範囲選択モード解除
                this.selectRangeStartCursor.dispose();
                this.selectRangeStartCursor = null;
                // 画面更新が必要な状態に設定
                this.needUpdate = true;
            }
        }
        ///////////////////
        //カーソル関連グローバル変数を一時避難
        saveCursor() {
            assert(this.currentCursor[1] === null);
            assert(this.displayLeftTopCursor[1] === null);
            this.currentCursor[1] = this.currentCursor[0].duplicate();
            this.displayLeftTopCursor[1] = this.displayLeftTopCursor[0].duplicate();
            this.currentCursorPosition[1].x = this.currentCursorPosition[0].x;
            this.currentCursorPosition[1].y = this.currentCursorPosition[0].y;
        }
        //カーソル関連グローバル変数を一時避難場所から戻す
        restoreCursor() {
            assert(this.currentCursor[0] !== null);
            assert(this.displayLeftTopCursor[0] !== null);
            assert(this.currentCursor[1] !== null);
            assert(this.displayLeftTopCursor[1] !== null);
            this.currentCursor[0].dispose();
            this.currentCursor[0] = this.currentCursor[1];
            this.currentCursor[1] = null;
            this.displayLeftTopCursor[0].dispose();
            this.displayLeftTopCursor[0] = this.displayLeftTopCursor[1];
            this.displayLeftTopCursor[1] = null;
            this.currentCursorPosition[0].x = this.currentCursorPosition[1].x;
            this.currentCursorPosition[1].x = 0;
            this.currentCursorPosition[0].y = this.currentCursorPosition[1].y;
            this.currentCursorPosition[1].y = 0;
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }
        discardSavedCursor() {
            if (this.currentCursor[1] !== null) {
                this.currentCursor[1].dispose();
                this.currentCursor[1] = null;
            }
            if (this.displayLeftTopCursor[1] !== null) {
                this.displayLeftTopCursor[1].dispose();
                this.displayLeftTopCursor[1] = null;
            }
            this.currentCursorPosition[1].x = 0;
            this.currentCursorPosition[1].y = 0;
        }
        draw() {
            if (this.needUpdate === false) {
                return false;
            }
            this.textVram.setTextColor(COLOR_NORMALTEXT);
            this.textVram.setBackgroundColor(COLOR_NORMALTEXT_BG);
            // 画面更新するので更新フラグ解除
            this.needUpdate = false;
            let textColor = COLOR_NORMALTEXT;
            let selectStart;
            let selectEnd;
            if (this.selectRangeStartCursor === null) {
                //範囲選択モードでない場合
                selectStart = null;
                selectEnd = null;
            }
            else {
                //範囲選択モードの場合、開始位置と終了の前後判断して
                //bp1 を開始位置、bp2 を終了位置に設定
                if (this.currentCursorPosition[0].y < this.selectStartCursorScreenPos.y ||
                    (this.currentCursorPosition[0].y === this.selectStartCursorScreenPos.y && this.currentCursorPosition[0].x < this.selectStartCursorScreenPos.x)) {
                    selectStart = this.currentCursor[0].duplicate();
                    selectEnd = this.selectRangeStartCursor.duplicate();
                }
                else {
                    selectStart = this.selectRangeStartCursor.duplicate();
                    selectEnd = this.currentCursor[0].duplicate();
                }
            }
            this.textVram.setTextColor(COLOR_NORMALTEXT);
            this.textVram.setBackgroundColor(COLOR_NORMALTEXT_BG);
            // テキストVRAMへの書き込み
            // 必要なら割り込み禁止とか使うこと
            this.textVram.clear();
            const pixels = this.textVram.getPixels();
            const cur = this.displayLeftTopCursor[0].duplicate();
            const pos = { x: 0, y: 0 };
            while (pos.y < this.editorScreenHeight) {
                // 選択範囲の始点/終点に到達してたら色設定変更
                if (this.selectRangeStartCursor !== null) {
                    if (this.textBuffer.isEqualCursor(cur, selectStart)) {
                        textColor = COLOR_AREASELECTTEXT;
                    }
                    if (this.textBuffer.isEqualCursor(cur, selectEnd)) {
                        textColor = COLOR_NORMALTEXT;
                    }
                }
                const ch = this.textBuffer.takeCharacterOnCursor(cur);
                const index = (pos.y * this.textVram.width + pos.x) * 2;
                pixels[index + 0] = ch;
                pixels[index + 1] = (pixels[index + 1] & 0xFFFFFFF0) | (textColor << 0);
                if (this.iterateCursorWithScreenPos(pos, cur) === false) {
                    break;
                }
            }
            cur.dispose();
            if (selectStart !== null) {
                selectStart.dispose();
            }
            if (selectEnd !== null) {
                selectEnd.dispose();
            }
            //EnableInterrupt();
            //エディター画面最下行の表示
            this.textVram.setCursorPosition(0, this.textVram.height - 1);
            this.textVram.setTextColor(COLOR_BOTTOMLINE);
            this.textVram.setBackgroundColor(COLOR_BOTTOMLINE_BG);
            this.textVram.putStr(this.menuStr);
            this.textVram.putDigit2(this.textBuffer.getTotalLength(), 5);
            this.textVram.fillBackgroundColor(0, this.textVram.height - 1, this.textVram.width, COLOR_BOTTOMLINE_BG);
            return true;
        }
        ///////////////////
        save(start, write, end, context) {
            let ret = false;
            if (start(context)) {
                ret = true;
                const bp = this.textBuffer.allocateCursor();
                while (this.textBuffer.isEndOfDocument(bp) === false) {
                    const ch = this.textBuffer.takeCharacterOnCursor(bp);
                    if (ch === 0x0000) {
                        break;
                    }
                    if (write(context, ch) === false) {
                        ret = false;
                        break;
                    }
                    this.textBuffer.cursorForward(bp);
                }
                bp.dispose();
            }
            end(context);
            return ret;
        }
        load(start, read, end, context) {
            let ret = false;
            // カーソル位置を行頭に移動
            this.cursorMoveToBeginningOfDocument();
            // クリア
            this.clear();
            if (start(context)) {
                ret = true;
                this.clear();
                let ch;
                while ((ch = read(context)) !== 0) {
                    this.insertCharacter(ch);
                    this.cursorMoveRight(); //画面上、バッファ上のカーソル位置を1つ後ろに移動
                }
            }
            end(context);
            // カーソル位置を行頭に移動
            this.cursorMoveToBeginningOfDocument();
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
            return ret;
        }
        ///////////////////
        inputNormalCharacter(utf32) {
            // 通常文字入力処理
            // k:入力された文字コード
            this.edited = true; //編集済みフラグを設定
            if (this.insertMode || utf32 === 0x0A || this.selectRangeStartCursor !== null) { // 挿入モードの場合
                // 選択範囲を削除
                if (this.selectRangeStartCursor !== null) {
                    this.deleteSelectedRange();
                }
                if (this.insertCharacter(utf32)) {
                    this.cursorMoveRight(); //画面上、バッファ上のカーソル位置を1つ後ろに移動
                }
            }
            else { //上書きモード
                if (this.overwriteCharacter(utf32)) {
                    this.cursorMoveRight(); //画面上、バッファ上のカーソル位置を1つ後ろに移動
                }
            }
        }
        inputControlCharacter(virtualKey, controlKey) {
            // 制御文字入力処理
            // k:制御文字の仮想キーコード
            // sh:シフト関連キー状態
            this.saveCursor(); //カーソル関連変数退避（カーソル移動できなかった場合戻すため）
            switch (virtualKey) {
                case 37 /* VKEY_LEFT */:
                case 100 /* VKEY_NUMPAD4 */:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((controlKey & 3 /* CHK_SHIFT */) === 0 || (virtualKey === 100 /* VKEY_NUMPAD4 */) && (controlKey & 512 /* CHK_NUMLK */)) {
                        if (this.selectRangeStartCursor !== null) {
                            this.selectRangeStartCursor.dispose();
                            this.selectRangeStartCursor = null;
                        }
                    }
                    else if (this.selectRangeStartCursor === null) {
                        this.setSelectedRangeStart(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    if (controlKey & 12 /* CHK_CTRL */) {
                        //CTRL＋左矢印でHome
                        this.cursorMoveToLineHead();
                        break;
                    }
                    this.cursorMoveLeft();
                    //if (SelectStart.Buffer !== NULL && (this.DisplayLeftTop[0].Buffer !== this.DisplayLeftTop[1].Buffer || this.DisplayLeftTop[0].Index !== this.DisplayLeftTop[1].Index)) {
                    if (this.selectRangeStartCursor !== null && !this.textBuffer.isEqualCursor(this.displayLeftTopCursor[0], this.displayLeftTopCursor[1])) {
                        //範囲選択モードで画面スクロールがあった場合
                        if (this.selectStartCursorScreenPos.y < this.editorScreenHeight - 1) {
                            this.selectStartCursorScreenPos.y++; //範囲スタート位置もスクロール
                        }
                        else {
                            this.restoreCursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                        }
                    }
                    break;
                case 39 /* VKEY_RIGHT */:
                case 102 /* VKEY_NUMPAD6 */:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((controlKey & 3 /* CHK_SHIFT */) === 0 || (virtualKey === 102 /* VKEY_NUMPAD6 */) && (controlKey & 512 /* CHK_NUMLK */)) {
                        if (this.selectRangeStartCursor !== null) {
                            this.selectRangeStartCursor.dispose();
                            this.selectRangeStartCursor = null;
                        }
                    }
                    else if (this.selectRangeStartCursor === null) {
                        this.setSelectedRangeStart(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    if (controlKey & 12 /* CHK_CTRL */) {
                        //CTRL＋右矢印でEnd
                        this.cursorMoveToLineTail();
                        break;
                    }
                    this.cursorMoveRight();
                    if (this.selectRangeStartCursor !== null && (this.textBuffer.isEqualCursor(this.displayLeftTopCursor[0], this.displayLeftTopCursor[1]) === false)) {
                        //範囲選択モードで画面スクロールがあった場合
                        if (this.selectStartCursorScreenPos.y > 0)
                            this.selectStartCursorScreenPos.y--; //範囲スタート位置もスクロール
                        else
                            this.restoreCursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                    }
                    break;
                case 38 /* VKEY_UP */:
                case 104 /* VKEY_NUMPAD8 */:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((controlKey & 3 /* CHK_SHIFT */) === 0 || (virtualKey === 104 /* VKEY_NUMPAD8 */) && (controlKey & 512 /* CHK_NUMLK */)) {
                        if (this.selectRangeStartCursor !== null) {
                            this.selectRangeStartCursor.dispose();
                            this.selectRangeStartCursor = null;
                        }
                    }
                    else if (this.selectRangeStartCursor === null) {
                        this.setSelectedRangeStart(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    this.cursorMoveUp();
                    if (this.selectRangeStartCursor !== null && (this.textBuffer.isEqualCursor(this.displayLeftTopCursor[0], this.displayLeftTopCursor[1]) === false)) {
                        //範囲選択モードで画面スクロールがあった場合
                        if (this.selectStartCursorScreenPos.y < this.editorScreenHeight - 1) {
                            this.selectStartCursorScreenPos.y++; //範囲スタート位置もスクロール
                        }
                        else {
                            this.restoreCursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                        }
                    }
                    break;
                case 40 /* VKEY_DOWN */:
                case 98 /* VKEY_NUMPAD2 */:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((controlKey & 3 /* CHK_SHIFT */) === 0 || (virtualKey === 98 /* VKEY_NUMPAD2 */) && (controlKey & 512 /* CHK_NUMLK */)) {
                        if (this.selectRangeStartCursor !== null) {
                            this.selectRangeStartCursor.dispose();
                            this.selectRangeStartCursor = null;
                        }
                    }
                    else if (this.selectRangeStartCursor === null) {
                        this.setSelectedRangeStart(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    this.cursorMoveDown();
                    if (this.selectRangeStartCursor !== null && (this.textBuffer.isEqualCursor(this.displayLeftTopCursor[0], this.displayLeftTopCursor[1]) === false)) {
                        //範囲選択モードで画面スクロールがあった場合
                        if (this.selectStartCursorScreenPos.y > 0)
                            this.selectStartCursorScreenPos.y--; //範囲スタート位置もスクロール
                        else
                            this.restoreCursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                    }
                    break;
                case 36 /* VKEY_HOME */:
                case 103 /* VKEY_NUMPAD7 */:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((controlKey & 3 /* CHK_SHIFT */) === 0 || (virtualKey === 103 /* VKEY_NUMPAD7 */) && (controlKey & 512 /* CHK_NUMLK */)) {
                        if (this.selectRangeStartCursor !== null) {
                            this.selectRangeStartCursor.dispose();
                            this.selectRangeStartCursor = null;
                        }
                    }
                    else if (this.selectRangeStartCursor === null) {
                        this.setSelectedRangeStart(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    this.cursorMoveToLineHead();
                    break;
                case 35 /* VKEY_END */:
                case 97 /* VKEY_NUMPAD1 */:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((controlKey & 3 /* CHK_SHIFT */) === 0 || (virtualKey === 97 /* VKEY_NUMPAD1 */) && (controlKey & 512 /* CHK_NUMLK */)) {
                        if (this.selectRangeStartCursor !== null) {
                            this.selectRangeStartCursor.dispose();
                            this.selectRangeStartCursor = null;
                        }
                    }
                    else if (this.selectRangeStartCursor === null) {
                        this.setSelectedRangeStart(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    this.cursorMoveToLineTail();
                    break;
                case 33 /* VKEY_PRIOR */: // PageUpキー
                case 105 /* VKEY_NUMPAD9 */:
                    //シフト＋PageUpは無効（NumLock＋シフト＋「9」除く）
                    if ((controlKey & 3 /* CHK_SHIFT */) && ((virtualKey !== 105 /* VKEY_NUMPAD9 */) || ((controlKey & 512 /* CHK_NUMLK */) === 0))) {
                        break;
                    }
                    //範囲選択モード解除
                    if (this.selectRangeStartCursor !== null) {
                        this.selectRangeStartCursor.dispose();
                        this.selectRangeStartCursor = null;
                    }
                    this.cursorMovePageUp();
                    break;
                case 34 /* VKEY_NEXT */: // PageDownキー
                case 99 /* VKEY_NUMPAD3 */:
                    //シフト＋PageDownは無効（NumLock＋シフト＋「3」除く）
                    if ((controlKey & 3 /* CHK_SHIFT */) && ((virtualKey !== 99 /* VKEY_NUMPAD3 */) || ((controlKey & 512 /* CHK_NUMLK */) === 0))) {
                        break;
                    }
                    //範囲選択モード解除
                    if (this.selectRangeStartCursor !== null) {
                        this.selectRangeStartCursor.dispose();
                        this.selectRangeStartCursor = null;
                    }
                    this.cursorMovePageDown();
                    break;
                case 46 /* VKEY_DELETE */: //Deleteキー
                case 110 /* VKEY_DECIMAL */: //テンキーの「.」
                    this.edited = true; //編集済みフラグ
                    if (this.selectRangeStartCursor !== null) {
                        this.deleteSelectedRange(); //選択範囲を削除
                    }
                    else {
                        this.textBuffer.deleteCharacterOnCursor(this.currentCursor[0]);
                        this.currentCursorPosition[0] = this.calcScreenPosFromLineHead(this.displayLeftTopCursor[0], this.currentCursor[0]);
                    }
                    break;
                case 8 /* VKEY_BACK */: //BackSpaceキー
                    this.edited = true; //編集済みフラグ
                    if (this.selectRangeStartCursor !== null) {
                        this.deleteSelectedRange(); //選択範囲を削除
                        break;
                    }
                    if (this.textBuffer.isBeginningOfDocument(this.currentCursor[0])) {
                        break; //バッファ先頭では無視
                    }
                    this.cursorMoveLeft();
                    this.textBuffer.deleteCharacterOnCursor(this.currentCursor[0]);
                    this.currentCursorPosition[0] = this.calcScreenPosFromLineHead(this.displayLeftTopCursor[0], this.currentCursor[0]);
                    break;
                case 45 /* VKEY_INSERT */:
                case 96 /* VKEY_NUMPAD0 */:
                    this.insertMode = !this.insertMode; //挿入モード、上書きモードを切り替え
                    break;
                case 67 /* VKEY_KEY_C */:
                    //CTRL+C、クリップボードにコピー
                    if (this.selectRangeStartCursor !== null && (controlKey & 12 /* CHK_CTRL */)) {
                        this.copyToClipboard();
                    }
                    break;
                case 88 /* VKEY_KEY_X */:
                    //CTRL+X、クリップボードに切り取り
                    if (this.selectRangeStartCursor !== null && (controlKey & 12 /* CHK_CTRL */)) {
                        this.copyToClipboard();
                        this.deleteSelectedRange(); //選択範囲の削除
                        this.edited = true; //編集済みフラグ
                    }
                    break;
                case 86 /* VKEY_KEY_V */:
                    //CTRL+V、クリップボードから貼り付け
                    if ((controlKey & 12 /* CHK_CTRL */) === 0)
                        break;
                    if (this.clipboard === null || this.clipboard.length === 0)
                        break;
                    this.edited = true; //編集済みフラグ
                    if (this.selectRangeStartCursor !== null) {
                        //範囲選択している時は削除してから貼り付け
                        this.deleteSelectedRange(); //選択範囲を削除
                        this.pasteFromClipboard(); //クリップボード貼り付け
                    }
                    else {
                        this.pasteFromClipboard(); //クリップボード貼り付け
                    }
                    break;
                case 83 /* VKEY_KEY_S */:
                    //CTRL+S、SDカードに保存
                    if ((controlKey & 12 /* CHK_CTRL */) === 0)
                        break;
                case 113 /* VKEY_F2 */: //F2キー
                    //this.save_as(); //ファイル名を付けて保存
                    break;
                case 79 /* VKEY_KEY_O */:
                    //CTRL+O、ファイル読み込み
                    if ((controlKey & 12 /* CHK_CTRL */) === 0)
                        break;
                case 112 /* VKEY_F1 */: //F1キー
                    //F1キー、ファイル読み込み
                    //this.selectfile();	//ファイルを選択して読み込み
                    break;
                case 78 /* VKEY_KEY_N */:
                    //CTRL+N、新規作成
                    if ((controlKey & 12 /* CHK_CTRL */) === 0)
                        break;
                case 115 /* VKEY_F4 */: //F4キー
                    //this.newtext(); //新規作成
                    break;
            }
            this.discardSavedCursor();
        }
    }
    const keyboardLayout = [
        [
            {
                "height": 2,
                "buttons": [
                    { "id": "ESC", "width": 2, "captions": ["ESC", "ESC"], "keycode": 27 /* VKEY_ESCAPE */ },
                    { "id": "", "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ },
                    { "id": "F1", "width": 2, "captions": ["F1", "F1"], "keycode": 112 /* VKEY_F1 */ },
                    { "id": "F2", "width": 2, "captions": ["F2", "F2"], "keycode": 113 /* VKEY_F2 */ },
                    { "id": "F3", "width": 2, "captions": ["F3", "F3"], "keycode": 114 /* VKEY_F3 */ },
                    { "id": "F4", "width": 2, "captions": ["F4", "F4"], "keycode": 115 /* VKEY_F4 */ },
                    { "id": "", "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ },
                    { "id": "F5", "width": 2, "captions": ["F5", "F5"], "keycode": 116 /* VKEY_F5 */ },
                    { "id": "F6", "width": 2, "captions": ["F6", "F6"], "keycode": 117 /* VKEY_F6 */ },
                    { "id": "F7", "width": 2, "captions": ["F7", "F7"], "keycode": 118 /* VKEY_F7 */ },
                    { "id": "F8", "width": 2, "captions": ["F8", "F8"], "keycode": 119 /* VKEY_F8 */ },
                    { "id": "", "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ },
                    { "id": "F9", "width": 2, "captions": ["F9", "F9"], "keycode": 120 /* VKEY_F9 */ },
                    { "id": "F10", "width": 2, "captions": ["F10", "F10"], "keycode": 121 /* VKEY_F10 */ },
                    { "id": "F11", "width": 2, "captions": ["F11", "F11"], "keycode": 122 /* VKEY_F11 */ },
                    { "id": "F12", "width": 2, "captions": ["F12", "F12"], "keycode": 123 /* VKEY_F12 */ }
                ]
            },
            {
                "height": 1,
                "buttons": [
                    { "id": "", "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "`", "width": 2, "captions": ["`", "~"], "keycode": 192 /* VKEY_OEM_3 */ },
                    { "id": "1", "width": 2, "captions": ["1", "!"], "keycode": 49 /* VKEY_NUM1 */ },
                    { "id": "2", "width": 2, "captions": ["2", "@"], "keycode": 50 /* VKEY_NUM2 */ },
                    { "id": "3", "width": 2, "captions": ["3", "#"], "keycode": 51 /* VKEY_NUM3 */ },
                    { "id": "4", "width": 2, "captions": ["4", "$"], "keycode": 52 /* VKEY_NUM4 */ },
                    { "id": "5", "width": 2, "captions": ["5", "%"], "keycode": 53 /* VKEY_NUM5 */ },
                    { "id": "6", "width": 2, "captions": ["6", "^"], "keycode": 54 /* VKEY_NUM6 */ },
                    { "id": "7", "width": 2, "captions": ["7", "&"], "keycode": 55 /* VKEY_NUM7 */ },
                    { "id": "8", "width": 2, "captions": ["8", "*"], "keycode": 56 /* VKEY_NUM8 */ },
                    { "id": "9", "width": 2, "captions": ["9", "("], "keycode": 57 /* VKEY_NUM9 */ },
                    { "id": "0", "width": 2, "captions": ["0", ")"], "keycode": 48 /* VKEY_NUM0 */ },
                    { "id": "-", "width": 2, "captions": ["-", "_"], "keycode": 189 /* VKEY_OEM_MINUS */ },
                    { "id": "=", "width": 2, "captions": ["=", "+"], "keycode": 187 /* VKEY_OEM_PLUS */ },
                    { "id": "BackSpace", "width": 4, "captions": ["back", "back"], "keycode": 8 /* VKEY_BACK */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "Tab", "width": 3, "captions": ["tab", "tab"], "keycode": 9 /* VKEY_TAB */ },
                    { "id": "q", "width": 2, "captions": ["q", "Q"], "keycode": 81 /* VKEY_KEY_Q */ },
                    { "id": "w", "width": 2, "captions": ["w", "W"], "keycode": 87 /* VKEY_KEY_W */ },
                    { "id": "e", "width": 2, "captions": ["e", "E"], "keycode": 69 /* VKEY_KEY_E */ },
                    { "id": "r", "width": 2, "captions": ["r", "R"], "keycode": 82 /* VKEY_KEY_R */ },
                    { "id": "t", "width": 2, "captions": ["t", "T"], "keycode": 84 /* VKEY_KEY_T */ },
                    { "id": "y", "width": 2, "captions": ["y", "Y"], "keycode": 89 /* VKEY_KEY_Y */ },
                    { "id": "u", "width": 2, "captions": ["u", "U"], "keycode": 85 /* VKEY_KEY_U */ },
                    { "id": "i", "width": 2, "captions": ["i", "I"], "keycode": 73 /* VKEY_KEY_I */ },
                    { "id": "o", "width": 2, "captions": ["o", "O"], "keycode": 79 /* VKEY_KEY_O */ },
                    { "id": "p", "width": 2, "captions": ["p", "P"], "keycode": 80 /* VKEY_KEY_P */ },
                    { "id": "[", "width": 2, "captions": ["[", "{"], "keycode": 219 /* VKEY_OEM_4 */ },
                    { "id": "]", "width": 2, "captions": ["]", "}"], "keycode": 221 /* VKEY_OEM_6 */ },
                    { "id": "\\", "width": 3, "captions": ["\\", "|"], "keycode": 220 /* VKEY_OEM_5 */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "CapsLock", "width": 4, "repeat": false, "captions": ["caps", "caps"], "keycode": 20 /* VKEY_CAPITAL */ },
                    { "id": "a", "width": 2, "captions": ["a", "A"], "keycode": 65 /* VKEY_KEY_A */ },
                    { "id": "s", "width": 2, "captions": ["s", "S"], "keycode": 83 /* VKEY_KEY_S */ },
                    { "id": "d", "width": 2, "captions": ["d", "D"], "keycode": 68 /* VKEY_KEY_D */ },
                    { "id": "f", "width": 2, "captions": ["f", "F"], "keycode": 70 /* VKEY_KEY_F */ },
                    { "id": "g", "width": 2, "captions": ["g", "G"], "keycode": 71 /* VKEY_KEY_G */ },
                    { "id": "h", "width": 2, "captions": ["h", "H"], "keycode": 72 /* VKEY_KEY_H */ },
                    { "id": "j", "width": 2, "captions": ["j", "J"], "keycode": 74 /* VKEY_KEY_J */ },
                    { "id": "k", "width": 2, "captions": ["k", "K"], "keycode": 75 /* VKEY_KEY_K */ },
                    { "id": "l", "width": 2, "captions": ["l", "L"], "keycode": 76 /* VKEY_KEY_L */ },
                    { "id": ";", "width": 2, "captions": [";", ":"], "keycode": 186 /* VKEY_OEM_1 */ },
                    { "id": "'", "width": 2, "captions": ["'", "\""], "keycode": 222 /* VKEY_OEM_7 */ },
                    { "id": "Enter", "width": 4, "captions": ["enter", "enter"], "keycode": 13 /* VKEY_RETURN */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "LShift", "width": 5, "repeat": false, "captions": ["shift", "shift"], "keycode": 160 /* VKEY_LSHIFT */ },
                    { "id": "z", "width": 2, "captions": ["z", "Z"], "keycode": 90 /* VKEY_KEY_Z */ },
                    { "id": "x", "width": 2, "captions": ["x", "X"], "keycode": 88 /* VKEY_KEY_X */ },
                    { "id": "c", "width": 2, "captions": ["c", "C"], "keycode": 67 /* VKEY_KEY_C */ },
                    { "id": "v", "width": 2, "captions": ["v", "V"], "keycode": 86 /* VKEY_KEY_V */ },
                    { "id": "b", "width": 2, "captions": ["b", "B"], "keycode": 66 /* VKEY_KEY_B */ },
                    { "id": "n", "width": 2, "captions": ["n", "N"], "keycode": 78 /* VKEY_KEY_N */ },
                    { "id": "m", "width": 2, "captions": ["m", "M"], "keycode": 77 /* VKEY_KEY_M */ },
                    { "id": ",", "width": 2, "captions": [",", "<"], "keycode": 188 /* VKEY_OEM_COMMA */ },
                    { "id": ".", "width": 2, "captions": [".", ">"], "keycode": 190 /* VKEY_OEM_PERIOD */ },
                    { "id": "/", "width": 2, "captions": ["/", "?"], "keycode": 191 /* VKEY_OEM_2 */ },
                    { "id": "RShift", "width": 5, "repeat": false, "captions": ["shift", "shift"], "keycode": 161 /* VKEY_RSHIFT */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "LCtrl", "width": 3, "repeat": false, "captions": ["ctrl", "ctrl"], "keycode": 162 /* VKEY_LCONTROL */ },
                    { "id": "Fn", "width": 2, "repeat": false, "captions": ["fn", "fn"], "keycode": 0 /* VKEY_NONE */ },
                    { "id": "LOs", "width": 2, "repeat": false, "captions": ["os", "os"], "keycode": 91 /* VKEY_LWIN */ },
                    { "id": "LAlt", "width": 2, "repeat": false, "captions": ["alt", "alt"], "keycode": 18 /* VKEY_MENU */ },
                    { "id": "Space", "width": 12, "captions": ["space", "space"], "keycode": 32 /* VKEY_SPACE */ },
                    { "id": "RAlt", "width": 3, "repeat": false, "captions": ["alt", "alt"], "keycode": 18 /* VKEY_MENU */ },
                    { "id": "ROs", "width": 2, "repeat": false, "captions": ["os", "os"], "keycode": 92 /* VKEY_RWIN */ },
                    { "id": "Menu", "width": 2, "repeat": false, "captions": ["menu", "menu"], "keycode": 165 /* VKEY_RMENU */ },
                    { "id": "RCtrl", "width": 3, "repeat": false, "captions": ["ctrl", "ctrl"], "keycode": 163 /* VKEY_RCONTROL */ }
                ]
            }
        ], [
            {
                "height": 2,
                "buttons": [
                    { "id": "PrintScreen", "width": 2, "repeat": false, "captions": ["PrtSc", "PrtSc"], "keycode": 42 /* VKEY_PRINT */ },
                    { "id": "ScrollLock", "width": 2, "repeat": false, "captions": ["ScrLk", "ScrLk"], "keycode": 145 /* VKEY_SCROLL */ },
                    { "id": "Pause", "width": 2, "repeat": false, "captions": ["Pause", "Pause"], "keycode": 19 /* VKEY_PAUSE */ }
                ]
            },
            {
                "height": 1,
                "buttons": [
                    { "id": "", "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "Insert", "width": 2, "captions": ["Ins", "Ins"], "keycode": 45 /* VKEY_INSERT */ },
                    { "id": "Home", "width": 2, "captions": ["Home", "Home"], "keycode": 36 /* VKEY_HOME */ },
                    { "id": "PageUp", "width": 2, "captions": ["PgUp", "PgUp"], "keycode": 33 /* VKEY_PRIOR */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "Delete", "width": 2, "captions": ["Del", "Del"], "keycode": 46 /* VKEY_DELETE */ },
                    { "id": "End", "width": 2, "captions": ["End", "End"], "keycode": 35 /* VKEY_END */ },
                    { "id": "PageDown", "width": 2, "captions": ["PgDn", "PgDn"], "keycode": 34 /* VKEY_NEXT */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "", "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "", "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ },
                    { "id": "ArrowUp", "width": 2, "captions": ["↑", "↑"], "keycode": 38 /* VKEY_UP */ },
                    { "id": "", "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "ArrowLeft", "width": 2, "captions": ["←", "←"], "keycode": 37 /* VKEY_LEFT */ },
                    { "id": "ArrowDown", "width": 2, "captions": ["↓", "↓"], "keycode": 40 /* VKEY_DOWN */ },
                    { "id": "ArrowRight", "width": 2, "captions": ["→", "→"], "keycode": 39 /* VKEY_RIGHT */ }
                ]
            }
        ]
    ];
    window
        .whenEvent('load')
        .then(() => {
        const canvas = document.createElement("canvas");
        canvas.style.position = "absolute";
        canvas.style.left = "0px";
        canvas.style.top = "0px";
        canvas.style.height = "100vh";
        canvas.style.width = "100vw";
        document.body.appendChild(canvas);
        const boundRect = canvas.getBoundingClientRect();
        canvas.width = boundRect.width;
        canvas.height = boundRect.height;
        const context = canvas.getContext("2d");
        const loadingInfo = {
            _stop: false,
            _loaded: 0,
            _total: 0,
            stopAnimation() {
                this._stop = true;
                document.body.removeChild(canvas);
            },
            updateAnimation(loaded, total) {
                this._loaded = loaded;
                this._total = total;
            },
            draw() {
                context.save();
                context.font = "24px 'Times New Roman'";
                context.fillStyle = 'rgb(0,0,0)';
                context.clearRect(0, 0, canvas.width, canvas.height);
                context.textAlign = "center";
                context.textBaseline = "middle";
                context.fillText(`Now loading ${this._loaded}/${this._total}`, canvas.width / 2, canvas.height / 2);
                context.restore();
            },
        };
        const loading = () => {
            if (loadingInfo._stop === false) {
                loadingInfo.draw();
                requestAnimationFrame(loading);
            }
        };
        loading();
        return loadingInfo;
    })
        .then((loadingInfo) => BitmapFont.loadFont('font.bmpf', (l, t) => loadingInfo.updateAnimation(l, t))
        .then((bmpFont) => {
        loadingInfo.stopAnimation();
        return bmpFont;
    }))
        .then((bmpFont) => {
        const root = document.getElementById("editor");
        const virtualKeyboard = new VirtualKeyboard(root, keyboardLayout, bmpFont);
        const canvas = document.createElement("canvas");
        const context = canvas.getContext("2d");
        let imageData = null;
        root.appendChild(canvas);
        const textVram = new TextVram(bmpFont, ~~(canvas.width / bmpFont.width), ~~(canvas.height / bmpFont.height));
        textVram.setPaletteColor(COLOR_BOTTOMLINE_BG, 0x00, 0x20, 0x80);
        const textEditor = new TextEditor(bmpFont, textVram);
        const keyboard = new Keyboard();
        resize();
        //*
        textEditor.load((s) => { s.str = Unicode.strToUtf32(document.getElementById('demo').innerHTML); return true; }, (s) => s.str.length > s.index ? s.str[s.index++] : 0, (s) => true, { str: null, index: 0 });
        //*/
        window.addEventListener('keydown', (e) => {
            if (e.repeat === false) {
                keyboard.pushKeyStatus(e.keyCode, true);
            }
            e.stopPropagation();
            e.preventDefault();
        });
        virtualKeyboard.addEventListener('keydown', (caption, keycode, repeat) => {
            if (repeat === false) {
                keyboard.pushKeyStatus(keycode, true);
            }
        });
        window.addEventListener('keyup', (e) => {
            keyboard.pushKeyStatus(e.keyCode, false);
            e.stopPropagation();
            e.preventDefault();
        });
        virtualKeyboard.addEventListener('keyup', (caption, keycode, repeat) => {
            if (repeat === false) {
                keyboard.pushKeyStatus(keycode, false);
            }
        });
        function loop() {
            if (textEditor.draw()) {
                textVram.setCursorPosition(textEditor.getCursorScreenPosX(), textEditor.getCursorScreenPosY());
                const palette = textVram.getPalette();
                const pixels = textVram.getPixels();
                const cursorPos = textVram.getCursorPosition();
                imageData.data32.fill(0xFF000000);
                let idx = 0;
                for (let y = 0; y < textVram.height; y++) {
                    for (let x = 0; x < textVram.width; x++) {
                        const ch = pixels[idx + 0];
                        const color = palette[(pixels[idx + 1] >> 0) & 0x0F];
                        const bgColor = palette[(pixels[idx + 1] >> 4) & 0x0F];
                        const fontWidth = bmpFont.getPixelWidth(ch);
                        const fontHeight = bmpFont.height;
                        const fontPixel = bmpFont.getPixel(ch);
                        const left = x * bmpFont.width;
                        const top = y * bmpFont.height;
                        const size = bmpFont.height;
                        if (x === cursorPos.x && y === cursorPos.y) {
                            imageData.fillRect(left, top, fontWidth !== 0 ? fontWidth : bmpFont.width, fontHeight, palette[COLOR_CURSOR]);
                        }
                        else {
                            imageData.fillRect(left, top, fontWidth, fontHeight, bgColor);
                        }
                        if (fontPixel !== undefined) {
                            imageData.drawChar(left, top, size, fontPixel, color);
                        }
                        idx += 2;
                    }
                }
                context.putImageData(imageData, 0, 0);
            }
            //
            virtualKeyboard.render();
            while (keyboard.readKey() && keyboard.getCurrentVKeyCode()) {
                let k1 = keyboard.getCurrentAsciiCode();
                const k2 = keyboard.getCurrentVKeyCode();
                const sh = keyboard.getCurrentCtrlKeys(); //sh:シフト関連キー状態
                //Enter押下は単純に改行文字を入力とする
                if (k2 === 13 /* VKEY_RETURN */ || k2 === 108 /* VKEY_SEPARATOR */) {
                    k1 = 0x0A;
                }
                if (k1 !== 0) {
                    //通常文字が入力された場合
                    textEditor.inputNormalCharacter(k1);
                }
                else {
                    //制御文字が入力された場合
                    textEditor.inputControlCharacter(k2, sh);
                }
                textEditor.checkSelectedRange();
            }
            window.requestAnimationFrame(loop);
        }
        window.requestAnimationFrame(loop);
        window.addEventListener("resize", (e) => {
            resize();
        });
        function resize() {
            virtualKeyboard.resize();
            const boundingRect = root.getBoundingClientRect();
            canvas.width = ~~boundingRect.width;
            canvas.height = ~~boundingRect.height - virtualKeyboard.height;
            canvas.style.position = "absolute";
            canvas.style.left = "0px";
            canvas.style.top = "0px";
            canvas.style.width = canvas.width + "px";
            canvas.style.height = canvas.height + "px";
            imageData = context.createImageData(~~canvas.width, ~~canvas.height);
            imageData.data32 = new Uint32Array(imageData.data.buffer);
            textEditor.resize(~~(canvas.width / bmpFont.width), ~~(canvas.height / bmpFont.height));
        }
        //////////
    });
    class VirtualKeyboard {
        get left() { return this._left; }
        get top() { return this._top; }
        get width() { return this._width; }
        get height() { return this._height; }
        addEventListener(event, handler) {
            if (!this._events.has(event)) {
                this._events.set(event, []);
            }
            this._events.get(event).push(handler);
        }
        fire(event, str, keycode, repeat) {
            if (!this._events.has(event)) {
                return;
            }
            this._events.get(event).forEach((ev) => ev(str, keycode, repeat));
        }
        calculateLayoutAndSize() {
            if (this._layoutChangedFlag) {
                this._sectionWidthsByUnit = this._layout.map((section) => Math.max(...section.map(column => column.buttons.reduce((s, x) => s + x.width, 0))));
                this._layoutWidthByUnit = this._sectionWidthsByUnit.reduce((s, x) => s + x, 0);
                this._layoutHeightByUnit = Math.max(...this._layout.map((section) => section.reduce((s, column) => s + column.height, 0)));
                this._layoutChangedFlag = false;
                this._sizeChangedFlag = true;
                this._needRedrawFlag = true;
            }
            if (this._sizeChangedFlag) {
                const boundingRect = this._parent.getBoundingClientRect();
                const unitPixelW = Math.max(this._font.height, ~~(boundingRect.width / this._layoutWidthByUnit), 3) - 2;
                const unitPixelH = Math.max(this._font.height, ~~(boundingRect.height / this._layoutHeightByUnit), 3) - 2;
                const unitPixel = Math.min(unitPixelW, unitPixelH);
                this._width = ~~boundingRect.width;
                this._height = ~~this._layoutHeightByUnit * unitPixel;
                this._left = ~~boundingRect.left;
                this._top = ~~(boundingRect.top + boundingRect.height - this._height);
                this._canvasElement.width = this._width;
                this._canvasElement.height = this._height;
                this._canvasElement.style.position = "absolute";
                this._canvasElement.style.left = "0px";
                this._canvasElement.style.top = (~~(boundingRect.height - this._height)) + "px";
                this._canvasElement.style.width = this._width + "px";
                this._canvasElement.style.height = this._height + "px";
                this._canvasOffscreenImageData = this._canvasRenderingContext.createImageData(this._width, this._height);
                this._canvasOffscreenImageData.data32 = new Uint32Array(this._canvasOffscreenImageData.data.buffer);
                this._buttons = [];
                let leftBaseByUnit = 0;
                this._layout.forEach((section, i) => {
                    const sectionWidthByPixel = this._width * (this._sectionWidthsByUnit[i]) / this._layoutWidthByUnit;
                    const sectionHeightByUnit = section.reduce((s, x) => s + x.height, 0);
                    let topByUnit = 0;
                    section.forEach((column) => {
                        const top = this._height * topByUnit / sectionHeightByUnit;
                        const columnHeightByPixel = this._height * column.height / sectionHeightByUnit;
                        const columnWidthByUnit = column.buttons.reduce((s, x) => s + x.width, 0);
                        let leftStepByUnit = leftBaseByUnit;
                        column.buttons.forEach((button, x) => {
                            const left = sectionWidthByPixel * leftStepByUnit / columnWidthByUnit;
                            const width = sectionWidthByPixel * button.width / columnWidthByUnit;
                            button.rect = { left: Math.trunc(left) + 1, top: Math.trunc(top) + 1, width: Math.ceil(width - 2), height: Math.ceil(columnHeightByPixel - 2) };
                            button.index = this._buttons.length;
                            this._buttons.push(button);
                            leftStepByUnit += button.width;
                        });
                        topByUnit += column.height;
                    });
                    leftBaseByUnit += this._sectionWidthsByUnit[i];
                });
                this._hitAreaMap = new Uint8Array(this._width * this._height);
                this._hitAreaMap.fill(0xFF);
                for (let button of this._buttons) {
                    const buttonWidth = button.rect.width;
                    if (button.keycode !== 0 /* VKEY_NONE */) {
                        let pos = button.rect.left + button.rect.top * this._width;
                        for (let y = 0; y < button.rect.height; y++) {
                            this._hitAreaMap.fill(button.index, pos, pos + buttonWidth);
                            pos += this._width;
                        }
                    }
                }
                this._sizeChangedFlag = false;
                this._needRedrawFlag = true;
            }
        }
        resize() {
            this._sizeChangedFlag = true;
            this.calculateLayoutAndSize();
        }
        constructor(parent, layout, font) {
            this._parent = parent;
            this._layout = layout;
            this._font = font;
            this._events = new Map();
            this._shiftKeyDownFlag = false;
            this._layoutChangedFlag = true;
            this._sizeChangedFlag = true;
            this._needRedrawFlag = true;
            this._touchPoints = new Map();
            this._canvasElement = document.createElement("canvas");
            this._canvasRenderingContext = this._canvasElement.getContext("2d");
            this.calculateLayoutAndSize();
            this._parent.appendChild(this._canvasElement);
            this._canvasElement.addEventListener("mousedown", (e) => {
                e.preventDefault();
                e.stopPropagation();
                const br = this._canvasElement.getBoundingClientRect();
                this.onTouchDown(e.button, e.pageX - ~~br.left, e.pageY - ~~br.top);
            });
            this._canvasElement.addEventListener("mouseup", (e) => {
                e.preventDefault();
                e.stopPropagation();
                const br = this._canvasElement.getBoundingClientRect();
                this.onTouchUp(e.button, e.pageX - ~~br.left, e.pageY - ~~br.top);
            });
            this._canvasElement.addEventListener("mousemove", (e) => {
                e.preventDefault();
                e.stopPropagation();
                const br = this._canvasElement.getBoundingClientRect();
                this.onTouchMove(e.button, e.pageX - ~~br.left, e.pageY - ~~br.top);
            });
            this._canvasElement.addEventListener("touchstart", (e) => {
                e.preventDefault();
                e.stopPropagation();
                const br = this._canvasElement.getBoundingClientRect();
                for (let i = 0; i < e.changedTouches.length; i++) {
                    const t = e.changedTouches.item(i);
                    this.onTouchDown(t.identifier, t.pageX - ~~br.left, t.pageY - ~~br.top);
                }
            });
            this._canvasElement.addEventListener("touchend", (e) => {
                e.preventDefault();
                e.stopPropagation();
                const br = this._canvasElement.getBoundingClientRect();
                for (let i = 0; i < e.changedTouches.length; i++) {
                    const t = e.changedTouches.item(i);
                    this.onTouchUp(t.identifier, t.pageX - ~~br.left, t.pageY - ~~br.top);
                }
            });
            this._canvasElement.addEventListener("touchmove", (e) => {
                e.preventDefault();
                e.stopPropagation();
                const br = this._canvasElement.getBoundingClientRect();
                for (let i = 0; i < e.changedTouches.length; i++) {
                    const t = e.changedTouches.item(i);
                    this.onTouchMove(t.identifier, t.pageX - ~~br.left, t.pageY - ~~br.top);
                }
            });
        }
        getHitKey(x, y) {
            if ((0 <= x && x < this._width && 0 <= y && y < this._height)) {
                return this._hitAreaMap[y * this._width + x];
            }
            else {
                return 0xFF;
            }
        }
        isShiftKeyDown() {
            for (let [k, v] of this._touchPoints) {
                if (v === 0xFF) {
                    continue;
                }
                if (this._buttons[v].keycode === 16 /* VKEY_SHIFT */ ||
                    this._buttons[v].keycode === 161 /* VKEY_RSHIFT */ ||
                    this._buttons[v].keycode === 160 /* VKEY_LSHIFT */) {
                    return true;
                }
            }
            return false;
        }
        onTouchDown(finger, px, py) {
            const x = ~~px;
            const y = ~~py;
            const keyIndex = this.getHitKey(x, y);
            if (keyIndex === 0xFF) {
                return;
            }
            if (this._touchPoints.has(finger)) {
                return;
            }
            this._touchPoints.set(finger, keyIndex);
            const pushedKey = this._buttons[keyIndex];
            this._needRedrawFlag = true;
            this._shiftKeyDownFlag = this.isShiftKeyDown();
            if (this.buttonIsDown(keyIndex) === 1) {
                this.fire("keydown", pushedKey.captions[this._shiftKeyDownFlag ? 1 : 0], pushedKey.keycode, false);
            }
        }
        onTouchUp(finger, px, py) {
            const x = ~~px;
            const y = ~~py;
            const keyIndex = this.getHitKey(x, y);
            if (!this._touchPoints.has(finger)) {
                return;
            }
            this._touchPoints.delete(finger);
            if (keyIndex === 0xFF) {
                return;
            }
            const pushedKey = this._buttons[keyIndex];
            this._needRedrawFlag = true;
            this._shiftKeyDownFlag = this.isShiftKeyDown();
            if (this.buttonIsDown(keyIndex) === 0) {
                this.fire("keyup", pushedKey.captions[this._shiftKeyDownFlag ? 1 : 0], pushedKey.keycode, false);
            }
        }
        onTouchMove(finger, px, py) {
            const x = ~~px;
            const y = ~~py;
            const keyIndex = this.getHitKey(x, y);
            if (!this._touchPoints.has(finger)) {
                return;
            }
            // 押されたキーとそれまで押してたキーが同じの場合は何もしない
            const prevKey = this._touchPoints.get(finger);
            if (keyIndex === prevKey) {
                return;
            }
            this._touchPoints.set(finger, keyIndex);
            let upKey = null;
            let downKey = null;
            if (prevKey !== 0xFF) {
                upKey = this._buttons[prevKey];
            }
            if (keyIndex !== 0xFF) {
                downKey = this._buttons[keyIndex];
            }
            this._shiftKeyDownFlag = this.isShiftKeyDown();
            if (upKey && this.buttonIsDown(prevKey) === 0) {
                this.fire("keyup", upKey.captions[this._shiftKeyDownFlag ? 1 : 0], upKey.keycode, false);
                this._needRedrawFlag = true;
            }
            if (downKey && this.buttonIsDown(keyIndex) === 1) {
                this.fire("keydown", downKey.captions[this._shiftKeyDownFlag ? 1 : 0], downKey.keycode, false);
                this._needRedrawFlag = true;
            }
        }
        buttonIsDown(index) {
            let n = 0;
            for (const [k, v] of this._touchPoints.entries()) {
                if (v === index) {
                    n++;
                }
            }
            return n;
        }
        render() {
            if (this._needRedrawFlag === false) {
                return;
            }
            const captionIndex = this._shiftKeyDownFlag ? 1 : 0;
            this._canvasOffscreenImageData.data32.fill(0xFFFFFFFF);
            this._layout.forEach((block) => {
                block.forEach((line) => {
                    line.buttons.forEach((btn) => {
                        if (btn.keycode !== 0 /* VKEY_NONE */) {
                            if (this.buttonIsDown(btn.index)) {
                                this._canvasOffscreenImageData.fillRect(btn.rect.left, btn.rect.top, btn.rect.width, btn.rect.height, 0xFF000000);
                                const text = btn.captions[captionIndex];
                                if (text !== null && text !== "") {
                                    const [w, h] = this._font.measureStr(text);
                                    const offX = (btn.rect.width - w) >> 1;
                                    const offY = (btn.rect.height - h) >> 1;
                                    this._font.drawStr(btn.rect.left + offX, btn.rect.top + offY, text, (x, y, size, pixels) => this._canvasOffscreenImageData.drawChar(x, y, size, pixels, 0xFFFFFFFF));
                                }
                            }
                            else {
                                this._canvasOffscreenImageData.drawRect(btn.rect.left, btn.rect.top, btn.rect.width, btn.rect.height, 0xFF000000);
                                const text = btn.captions[captionIndex];
                                if (text !== null && text !== "") {
                                    const [w, h] = this._font.measureStr(text);
                                    const offX = (btn.rect.width - w) >> 1;
                                    const offY = (btn.rect.height - h) >> 1;
                                    this._font.drawStr(btn.rect.left + offX, btn.rect.top + offY, text, (x, y, size, pixels) => this._canvasOffscreenImageData.drawChar(x, y, size, pixels, 0xFF000000));
                                }
                            }
                        }
                    });
                });
            });
            this._canvasRenderingContext.putImageData(this._canvasOffscreenImageData, 0, 0);
            this._needRedrawFlag = false;
        }
    }
})(Editor || (Editor = {}));
Uint8Array.prototype.getBit = function (n) {
    return this[n >> 3] & (0x80 >> (n & 0x07));
};
Uint8Array.prototype.setBit = function (n, value) {
    const bit = 0x80 >> (n & 0x07);
    if (value) {
        this[n >> 3] |= bit;
    }
    else {
        this[n >> 3] &= ~bit;
    }
};
ImageData.prototype.drawChar = function (x, y, size, pixel, color) {
    const data32 = this.data32;
    const stride = this.width;
    let pSrc = 0;
    let pDst = y * stride + x;
    for (let j = 0; j < size; j++) {
        let p = pSrc;
        let q = pDst;
        for (let i = 0; i < size; i++, p++, q++) {
            if (pixel[p >> 3] & (0x80 >> (p & 0x07))) {
                data32[q] = color;
            }
        }
        pSrc += size;
        pDst += stride;
    }
};
ImageData.prototype.setPixel = function (x, y, color) {
    "use strict";
    let _x = ~~x;
    let _y = ~~y;
    if (_x < 0 || _x >= this.width || _y < 0 || _y >= this.height) {
        return;
    }
    this.data32[_y * this.width + _x] = color;
};
ImageData.prototype.fillRect = function (left, top, width, height, color) {
    "use strict";
    let l = ~~left;
    let t = ~~top;
    let w = ~~width;
    let h = ~~height;
    if (l < 0) {
        w += l;
        l = 0;
    }
    if (l + w > this.width) {
        w = this.width - l;
    }
    if (w <= 0) {
        return;
    }
    if (t < 0) {
        h += t;
        t = 0;
    }
    if (t + h > this.height) {
        h = this.height - t;
    }
    if (h <= 0) {
        return;
    }
    const stride = this.width;
    let start = t * stride + l;
    let end = start + w;
    const data32 = this.data32;
    for (let j = 0; j < h; j++) {
        data32.fill(color, start, end);
        start += stride;
        end += stride;
    }
};
ImageData.prototype.drawRect = function (left, top, width, height, color) {
    "use strict";
    let l = ~~left;
    let t = ~~top;
    let w = ~~width;
    let h = ~~height;
    if (l < 0) {
        w += l;
        l = 0;
    }
    if (l + w > this.width) {
        w = this.width - l;
    }
    if (w <= 0) {
        return;
    }
    if (t < 0) {
        h += t;
        t = 0;
    }
    if (t + h > this.height) {
        h = this.height - t;
    }
    if (h <= 0) {
        return;
    }
    const stride = this.width;
    let start = t * stride + l;
    let end = start + w;
    this.data32.fill(color, start, end);
    for (let j = 0; j < h - 1; j++) {
        start += stride;
        end += stride;
        this.data32[start] = color;
        this.data32[end - 1] = color;
    }
    this.data32.fill(color, start, end);
};
//# sourceMappingURL=editor.js.map