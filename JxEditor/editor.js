// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />
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
            for (let i = 0; i < Keyboard.KeycodeBufferSize; i++) {
                this.inputBuffer[i] = { ctrlkey: 0 /* CHK_NONE */, keycode: 0 };
            }
            //全キー離した状態
            this.virtualKeyStatus.fill(0);
        }
        UpdateCtrlKeyState(vk, breakflag) {
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
            if (breakflag) {
                this.ctrlKeyStatus = this.ctrlKeyStatus & (~k);
            }
            else {
                this.ctrlKeyStatus = this.ctrlKeyStatus | k;
            }
        }
        // NumLock,CapsLock,ScrollLockの状態更新
        UpdateLockKeyState(vk) {
            switch (vk) {
                case 145 /* VKEY_SCROLL */:
                    this.ctrlKeyStatus ^= 256 /* CHK_SCRLK */;
                    break;
                case 144 /* VKEY_NUMLOCK */:
                    this.ctrlKeyStatus ^= 512 /* CHK_NUMLK */;
                    break;
                case 20 /* VKEY_CAPITAL */:
                    if ((this.ctrlKeyStatus & 3 /* CHK_SHIFT */) == 0)
                        return;
                    this.ctrlKeyStatus ^= 1024 /* CHK_CAPSLK */;
                    break;
                default:
                    return;
            }
        }
        // vkが SHIFT,ALT,WIN,CTRLか判定
        isShiftkey(vk) {
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
        pushKeyStatus(vk, breakflag) {
            if (this.isShiftkey(vk)) {
                if (breakflag == false && this.virtualKeyStatus.getBit(vk)) {
                    return; // キーリピートの場合、無視
                }
                this.UpdateCtrlKeyState(vk, breakflag); //SHIFT系キーのフラグ処理
            }
            else if (breakflag == false && this.isLockKey(vk)) {
                if (this.virtualKeyStatus.getBit(vk)) {
                    return; //キーリピートの場合、無視
                }
                this.UpdateLockKeyState(vk); //NumLock、CapsLock、ScrollLock反転処理
            }
            //キーコードに対する押下状態配列を更新
            if (breakflag) {
                this.virtualKeyStatus.setBit(vk, 0);
                return;
            }
            this.virtualKeyStatus.setBit(vk, 1);
            if ((this.inputBufferWriteIndex + 1 == this.inputBufferReadIndex) ||
                (this.inputBufferWriteIndex == Keyboard.KeycodeBufferSize - 1) && (this.inputBufferReadIndex == 0)) {
                return; //バッファがいっぱいの場合無視
            }
            this.inputBuffer[this.inputBufferWriteIndex].keycode = vk;
            this.inputBuffer[this.inputBufferWriteIndex].ctrlkey = this.ctrlKeyStatus;
            this.inputBufferWriteIndex++;
            if (this.inputBufferWriteIndex == Keyboard.KeycodeBufferSize) {
                this.inputBufferWriteIndex = 0;
            }
        }
        // キーバッファからキーを一つ読み取る
        // 押されていなければfalseを返す
        readKey() {
            this.currentAsciiCode = 0x00;
            this.currentVKeyCode = 0x0000;
            this.currentCtrlKeys = 0 /* CHK_NONE */;
            if (this.inputBufferWriteIndex == this.inputBufferReadIndex) {
                return false;
            }
            const k = this.inputBuffer[this.inputBufferReadIndex++];
            this.currentVKeyCode = k.keycode;
            this.currentCtrlKeys = k.ctrlkey;
            if (this.inputBufferReadIndex == Keyboard.KeycodeBufferSize) {
                this.inputBufferReadIndex = 0;
            }
            if (k.ctrlkey & (12 /* CHK_CTRL */ | 48 /* CHK_ALT */ | 192 /* CHK_WIN */)) {
                return true;
            }
            let k2 = 0;
            if (k.keycode >= 65 /* VKEY_KEY_A */ && k.keycode <= 90 /* VKEY_KEY_Z */) {
                if (((k.ctrlkey & 3 /* CHK_SHIFT */) != 0) != ((k.ctrlkey & 1024 /* CHK_CAPSLK */) != 0)) {
                    //SHIFTまたはCapsLock（両方ではない）
                    k2 = Keyboard.vk2asc2[k.keycode];
                }
                else {
                    k2 = Keyboard.vk2asc1[k.keycode];
                }
            }
            else if (k.keycode >= 96 /* VKEY_NUMPAD0 */ && k.keycode <= 111 /* VKEY_DIVIDE */) {
                //テンキー関連
                if ((k.ctrlkey & (3 /* CHK_SHIFT */ | 512 /* CHK_NUMLK */)) == 512 /* CHK_NUMLK */) {
                    //NumLock（SHIFT＋NumLockは無効）
                    k2 = Keyboard.vk2asc2[k.keycode];
                }
                else {
                    k2 = Keyboard.vk2asc1[k.keycode];
                }
            }
            else {
                if (k.ctrlkey & 3 /* CHK_SHIFT */) {
                    k2 = Keyboard.vk2asc2[k.keycode];
                }
                else {
                    k2 = Keyboard.vk2asc1[k.keycode];
                }
            }
            this.currentAsciiCode = k2;
            return true;
        }
        GetCurrentCtrlKeys() {
            return this.currentCtrlKeys;
        }
        GetCurrentVKeyCode() {
            return this.currentVKeyCode;
        }
        GetCurrentAsciiCode() {
            return this.currentAsciiCode;
        }
    }
    Keyboard.KeycodeBufferSize = 16;
    Keyboard.vk2asc1 = new Uint8Array([
        // 仮想キーコードからASCIIコードへの変換テーブル（SHIFTなし）
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
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
    ].map(x => (typeof (x) == "string") ? x.codePointAt(0) : x));
    Keyboard.vk2asc2 = new Uint8Array([
        // 仮想キーコードからASCIIコードへの変換テーブル（SHIFTあり）
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
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
    ].map(x => (typeof (x) == "string") ? x.codePointAt(0) : x));
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
            this.gapSize = gapSize || 64;
            if (this.gapSize <= 0) {
                throw new RangeError("gapSize must be > 0");
            }
            this.buffer = new Uint32Array(this.gapSize);
            this.gapStart = 0;
            this.gapEnd = this.gapSize;
        }
        Dispose() {
            this.buffer = null;
            this.gapStart = this.gapEnd = 0;
        }
        *[Symbol.iterator]() {
            for (let i = 0; i < this.gapStart; i++) {
                yield this.buffer[i];
            }
            for (let i = this.gapEnd; i < this.buffer.length; i++) {
                yield this.buffer[i];
            }
        }
        get length() {
            return this.buffer.length - (this.gapEnd - this.gapStart);
        }
        get(ix) {
            if (ix >= this.length) {
                return undefined;
            }
            if (ix >= this.gapStart) {
                ix += (this.gapEnd - this.gapStart);
            }
            return this.buffer[ix];
        }
        set(ix, value) {
            if (ix >= this.length) {
                return;
            }
            if (ix >= this.gapStart) {
                ix += (this.gapEnd - this.gapStart);
            }
            this.buffer[ix] = value;
        }
        grow(newsize) {
            const gapSize = newsize - this.buffer.length + (this.gapEnd - this.gapStart);
            const newBuffer = new Uint32Array(newsize);
            for (let i = 0; i < this.gapStart; i++) {
                newBuffer[i] = this.buffer[i];
            }
            for (let i = this.gapEnd; i < this.buffer.length; i++) {
                newBuffer[i + gapSize] = this.buffer[i];
            }
            this.buffer = newBuffer;
            this.gapEnd = this.gapStart + gapSize;
        }
        insert(ix, value) {
            if (ix < 0) {
                throw new RangeError("insert index must be >= 0");
            }
            if (ix > this.length) {
                throw new RangeError("insert index must be <= length (for now)");
            }
            if (this.gapStart === this.gapEnd) {
                this.grow(this.buffer.length + this.gapSize);
            }
            this.moveGap(ix);
            this.buffer[this.gapStart++] = value;
        }
        insertAll(ix, values) {
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
            this.gapStart = ix;
            this.gapEnd += len;
            if (this.gapEnd > this.buffer.length) {
                this.gapEnd = this.buffer.length;
            }
            return true;
        }
        deleteBefore(ix, len) {
            if (ix === 0 || ix > this.length) {
                return false;
            }
            this.moveGap(ix);
            this.gapStart -= len;
            if (this.gapStart < 0) {
                this.gapStart = 0;
            }
            return true;
        }
        clear() {
            this.gapStart = 0;
            this.gapEnd = this.buffer.length;
        }
        asArray() {
            const newBuffer = new Uint32Array(this.length);
            let n = 0;
            for (let i = 0; i < this.gapStart; i++, n++) {
                newBuffer[n] = this.buffer[i];
            }
            for (let i = this.gapEnd; i < this.buffer.length; i++, n++) {
                newBuffer[n] = this.buffer[i];
            }
            return newBuffer;
        }
        moveGap(ix) {
            if (ix < this.gapStart) {
                const gapSize = (this.gapEnd - this.gapStart);
                const delta = this.gapStart - ix;
                this.buffer.copyWithin(this.gapEnd - delta, ix, this.gapStart);
                //for (let i = delta - 1; i >= 0; --i) {
                //    this.buffer[this.gapEnd - delta + i] = this.buffer[ix + i];
                //}
                this.gapStart -= delta;
                this.gapEnd -= delta;
            }
            else if (ix > this.gapStart) {
                const gapSize = (this.gapEnd - this.gapStart);
                const delta = ix - this.gapStart;
                this.buffer.copyWithin(this.gapStart, this.gapEnd, this.gapEnd + delta);
                //for (let i = 0; i < delta; ++i) {
                //    this.buffer[this.gapStart + i] = this.buffer[this.gapEnd + i];
                //}
                this.gapStart += delta;
                this.gapEnd += delta;
            }
        }
        append(that) {
            this.moveGap(this.length);
            this.grow(this.length + that.length);
            for (let i = 0; i < that.length; ++i) {
                this.buffer[this.gapStart++] = that.get(i);
            }
        }
        reduce(callbackfn, initialValue) {
            let currentIndex = 0;
            let previousValue = initialValue;
            for (const currentValue of this) {
                previousValue = callbackfn(previousValue, currentValue, currentIndex++, this);
            }
            return previousValue;
        }
        find(predicate) {
            let index = 0;
            for (const value of this) {
                if (predicate(value, index, this) === true) {
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
        Dispose() {
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
    class BMPFont {
        constructor(buffer) {
            const it = new DataViewIterator(buffer, true);
            const fourCC = it.getUint32();
            if (fourCC != 0x46504D42) {
                it.Dispose();
                throw new TypeError("bad file format.");
            }
            this.FontWidth = it.getUint32();
            this.FontHeight = it.getUint32();
            this.PixelSize = it.getUint32();
            const widthTableSize = it.getUint32();
            const pixelTableSize = it.getUint32();
            this.WidthTable = Array(widthTableSize);
            this.PixelTable = Array(pixelTableSize);
            for (let i = 0; i < widthTableSize; i++) {
                this.WidthTable[i] = {
                    RangeStart: it.getUint32(),
                    RangeEnd: it.getUint32(),
                    Width: it.getUint8(),
                };
            }
            for (let i = 0; i < pixelTableSize; i++) {
                const RangeStart = it.getUint32();
                const RangeEnd = it.getUint32();
                const Pixels = [];
                for (let j = RangeStart; j <= RangeEnd; j++) {
                    Pixels.push(it.getBytes(this.PixelSize));
                }
                this.PixelTable[i] = {
                    RangeStart: RangeStart,
                    RangeEnd: RangeEnd,
                    Pixels: Pixels,
                };
            }
            it.Dispose();
        }
        static loadFont(url) {
            return new Promise((resolve, reject) => {
                const xhr = new XMLHttpRequest();
                xhr.open('GET', url, true);
                xhr.onload = () => {
                    if (xhr.readyState === 4 && ((xhr.status === 200) || (xhr.responseURL.startsWith("file://") === true && xhr.status === 0))) {
                        resolve(xhr.response);
                    }
                    else {
                        reject(new Error(xhr.statusText));
                    }
                };
                xhr.onerror = () => { reject(new Error(xhr.statusText)); };
                xhr.responseType = 'arraybuffer';
                xhr.send(null);
            }).then((arraybuffer) => {
                return new BMPFont(arraybuffer);
            });
        }
        static search(table, codePoint) {
            let start = 0;
            let end = table.length - 1;
            while (start <= end) {
                const mid = ((end - start) >> 1) + start;
                if (table[mid].RangeStart > codePoint) {
                    end = mid - 1;
                }
                else if (table[mid].RangeEnd < codePoint) {
                    start = mid + 1;
                }
                else {
                    return table[mid];
                }
            }
            return undefined;
        }
        getWidth(codePoint) {
            if (0x00 == codePoint) {
                return 0;
            }
            if (0x01 <= codePoint && codePoint <= 0x1F) {
                return 1; // control code
            }
            const ret = BMPFont.search(this.WidthTable, codePoint);
            if (ret === undefined) {
                return undefined;
            }
            return ret.Width;
        }
        getPixelWidth(codePoint, defaultWidth) {
            const ret = this.getWidth(codePoint);
            if (ret === undefined) {
                return defaultWidth;
            }
            return ret * this.FontWidth;
        }
        getPixel(codePoint) {
            const ret = BMPFont.search(this.PixelTable, codePoint);
            if (ret === undefined) {
                return undefined;
            }
            return ret.Pixels[codePoint - ret.RangeStart];
        }
        measureStr(str) {
            return this.measureUtf32(Unicode.strToUtf32(str));
        }
        measureUtf32(utf32Str) {
            let w = 0;
            let h = 0;
            let xx = 0;
            let yy = 0;
            const size = this.FontHeight;
            for (const utf32Ch of utf32Str) {
                const width = this.getPixelWidth(utf32Ch);
                if (width === undefined) {
                    xx += this.FontWidth;
                    w = Math.max(w, xx);
                    h = Math.max(h, yy + this.FontHeight);
                }
                else if (utf32Ch == 0x0A) {
                    yy += size;
                    xx = 0;
                }
                else {
                    xx += width;
                    w = Math.max(w, xx);
                    h = Math.max(h, yy + this.FontHeight);
                }
            }
            return [w, h];
        }
        drawStr(x, y, str, pset) {
            this.drawUtf32(x, y, Unicode.strToUtf32(str), pset);
        }
        drawUtf32(x, y, utf32Str, pset) {
            let xx = x;
            let yy = y;
            const size = this.FontHeight;
            const scanline = this.FontHeight;
            for (const utf32Ch of utf32Str) {
                const width = this.getPixelWidth(utf32Ch);
                if (width === undefined) {
                    xx += this.FontWidth;
                    continue;
                }
                if (utf32Ch == 0x0A) {
                    yy += size;
                    xx = x;
                    continue;
                }
                else {
                    const pixel = this.getPixel(utf32Ch);
                    if (pixel) {
                        let pSrc = 0;
                        for (let j = 0; j < size; j++) {
                            let p = pSrc;
                            for (let i = 0; i < size; i++, p++) {
                                if (pixel[p >> 3] & (0x80 >> (p & 0x07))) {
                                    pset(xx + i, yy + j);
                                }
                            }
                            pSrc += scanline;
                        }
                    }
                    xx += width;
                }
            }
        }
    }
    class text_buffer_cursor_t {
        constructor(line, row = 0) {
            this.use = true;
            this.line = line;
            this.row = row;
        }
        Duplicate() {
            return this.line.parent.DuplicateCursor(this);
        }
        Dispose() {
            this.line.parent.DisposeCursor(this);
        }
        CheckValid() {
            assert(this.use);
            assert(this.line != null);
            assert(0 <= this.row);
            assert(this.row <= this.line.buffer.length);
        }
        static Equal(x, y) {
            if ((x.use == false) || (y.use == false)) {
                return false;
            }
            return (x.line == y.line) && (x.row == y.row);
        }
        CopyTo(other) {
            if (this.use == false || other.use == false) {
                throw new Error();
            }
            other.line = this.line;
            other.row = this.row;
        }
    }
    class text_buffer_line_t {
        constructor(parent) {
            this.parent = parent;
            this.buffer = new GapBuffer();
            this.prev = null;
            this.next = null;
        }
        Dispose() {
            this.parent = null;
            this.buffer.Dispose();
            this.buffer = null;
            this.prev = null;
            this.next = null;
        }
    }
    class TextBuffer {
        getTotalLength() {
            return this.TotalLength;
        }
        clear() {
            this.Buffers.next = null;
            this.Buffers.prev = null;
            this.Buffers.buffer.clear();
            this.TotalLength = 0;
            for (const cur of this.Cur) {
                if (cur.use == false) {
                    this.Cur.delete(cur);
                    continue;
                }
                cur.line = this.Buffers;
                cur.row = 0;
            }
            ;
        }
        AllocateCursor() {
            var newCursor = new text_buffer_cursor_t(this.Buffers);
            this.Cur.add(newCursor);
            return newCursor;
        }
        DuplicateCursor(cursor) {
            if ((!this.Cur.has(cursor)) || (cursor.use == false)) {
                throw new Error();
            }
            var newCursor = new text_buffer_cursor_t(cursor.line, cursor.row);
            this.Cur.add(newCursor);
            return newCursor;
        }
        DisposeCursor(cursor) {
            if ((!this.Cur.has(cursor)) || (cursor.use == false)) {
                throw new Error();
            }
            this.Cur.delete(cursor);
            cursor.use = false;
            cursor.line = null;
            cursor.row = -1;
        }
        constructor() {
            this.Buffers = new text_buffer_line_t(this);
            this.TotalLength = 0;
            this.Cur = new Set();
        }
        checkValidCursor(cursor) {
            if ((!this.Cur.has(cursor)) || (cursor.use == false)) {
                throw new Error();
            }
            cursor.CheckValid();
        }
        DeleteCharacatorOnCursor(cursor) {
            if ((!this.Cur.has(cursor)) || (cursor.use == false)) {
                throw new Error();
            }
            if (cursor.line.buffer.length == cursor.row) {
                // 末尾文字を削除＝後ろの行があれば削除
                if (cursor.line.next != null) {
                    const next = cursor.line.next;
                    cursor.line.buffer.append(next.buffer);
                    this.RemoveLine(next);
                    this.TotalLength--;
                }
            }
            else {
                // カーソルの位置の文字を削除
                if (cursor.line.buffer.deleteAfter(cursor.row, 1) == false) {
                    return;
                }
                this.TotalLength--;
            }
        }
        RemoveLine(buf) {
            if (buf.prev == null && buf.next == null) {
                // １行だけ存在する場合
                // １行目をクリア
                buf.buffer.clear();
                // 行内にあるカーソルを先頭に移動させる
                for (const cur of this.Cur) {
                    if (cur.use == false) {
                        this.Cur.delete(cur);
                        continue;
                    }
                    if (cur.line != buf) {
                        continue;
                    }
                    // 最初の行に対する行削除なので最初の行に設定
                    cur.row = 0;
                }
            }
            else {
                // ２行以上存在する場合
                // １行目が削除対象の場合、先頭行を２行目にずらす。
                if (this.Buffers == buf && buf.next != null) {
                    this.Buffers = buf.next;
                }
                // リンクリストから削除行を削除する
                if (buf.next != null) {
                    buf.next.prev = buf.prev;
                }
                if (buf.prev != null) {
                    buf.prev.next = buf.next;
                }
                // 削除した行内にあるカーソルは移動させる
                for (const cur of this.Cur) {
                    if (cur.use == false) {
                        this.Cur.delete(cur);
                        continue;
                    }
                    if (cur.line != buf) {
                        continue;
                    }
                    if (buf.next != null) {
                        //   次の行がある場合：次の行の行頭に移動する
                        cur.line = buf.next;
                        cur.row = 0;
                    }
                    else if (buf.prev != null) {
                        //   次の行がなく前の行がある場合：前の行の行末に移動する。
                        cur.line = buf.prev;
                        cur.row = buf.prev.buffer.length;
                    }
                    else {
                        //   どちらもない場合：最初の行に対する行削除なので最初の行に設定
                        cur.line = this.Buffers;
                        cur.row = 0;
                    }
                }
                // 行情報を破棄
                buf.Dispose();
            }
        }
        DeleteArea(s, len) {
            const start = this.DuplicateCursor(s);
            // １文字づつ消す
            while (len > 0) {
                this.DeleteCharacatorOnCursor(start);
                len--;
            }
            this.DisposeCursor(start);
        }
        TakeCharacatorOnCursor(cursor) {
            if ((!this.Cur.has(cursor)) || (cursor.use == false)) {
                throw new Error();
            }
            if (cursor.line.buffer.length == cursor.row) {
                if (cursor.line.next == null) {
                    return 0x00; // 終端なのでヌル文字を返す
                }
                else {
                    return 0x0A; // 改行文字を返す
                }
            }
            return cursor.line.buffer.get(cursor.row);
        }
        InsertCharacatorOnCursor(cursor, ch) {
            this.checkValidCursor(cursor);
            if (ch == 0x0A) // 0x0A = \n
             {
                // 改行は特別扱い
                // 新しい行バッファを確保
                const nb = new text_buffer_line_t(this);
                // 現在カーソルがあるバッファの後ろに連結
                const buf = cursor.line;
                nb.prev = buf;
                nb.next = buf.next;
                if (buf.next != null) {
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
                for (const cur of this.Cur) {
                    if (cur.use == false) {
                        this.Cur.delete(cur);
                        continue;
                    }
                    if (cur === cursor) {
                        continue;
                    }
                    if (cur.line == buf && cur.row > len) {
                        cur.line = nb;
                        cur.row -= len;
                    }
                }
                // 行を増やす＝改行挿入なので文字は増えていないが仮想的に1文字使っていることにする
                this.TotalLength++;
            }
            else {
                cursor.line.buffer.insert(cursor.row, ch);
                this.TotalLength++;
            }
            return true;
        }
        // カーソル位置の文字を上書きする
        OverwriteCharacatorOnCursor(cursor, ch) {
            this.checkValidCursor(cursor);
            if (cursor.line.buffer.length == cursor.row || ch == 0x0A) {
                this.InsertCharacatorOnCursor(cursor, ch);
            }
            else {
                this.TotalLength++;
                cursor.line.buffer.set(cursor.row, ch);
            }
            return true;
        }
        // テキストバッファカーソルを１文字前に移動させる
        // falseはcursorが元々先頭であったことを示す。
        CursorBackward(cursor) {
            this.checkValidCursor(cursor);
            if (cursor.row > 0) {
                // カーソル位置がバッファ先頭以外の場合
                cursor.row--;
                this.checkValidCursor(cursor);
                return true;
            }
            else {
                // カーソル位置がバッファ先頭の場合は前の行に動かす
                if (cursor.line.prev != null) {
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
        CursorForward(cursor) {
            this.checkValidCursor(cursor);
            if (cursor.row < cursor.line.buffer.length) {
                // カーソル位置がバッファ先頭以外の場合
                cursor.row++;
                this.checkValidCursor(cursor);
                return true;
            }
            else {
                // カーソル位置がバッファ末尾の場合は次の行に動かす
                if (cursor.line.next != null) {
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
        CompareCursor(c1, c2) {
            this.checkValidCursor(c1);
            this.checkValidCursor(c2);
            if (c1.line == c2.line) {
                if (c1.row < c2.row) {
                    return -1;
                }
                if (c1.row == c2.row) {
                    return 0;
                }
                if (c1.row > c2.row) {
                    return 1;
                }
            }
            for (let p = this.Buffers; p != null; p = p.next) {
                if (p == c1.line) {
                    return -1;
                }
                if (p == c2.line) {
                    return 1;
                }
            }
            throw new Error(); // 不正なカーソルが渡された
        }
        // カーソルが同一位置を示しているか判定
        // CompareCursorより高速
        EqualCursor(c1, c2) {
            this.checkValidCursor(c1);
            this.checkValidCursor(c2);
            if (c1.line == c2.line) {
                if (c1.row == c2.row) {
                    return true;
                }
            }
            return false;
        }
        // テキスト全体の先頭かどうか判定
        StartOfString(c) {
            this.checkValidCursor(c);
            return (c.line.prev == null && c.row == 0);
        }
        EndOfString(c) {
            this.checkValidCursor(c);
            if (c.line.buffer.length == c.row && c.line.next == null) {
                return true;
            }
            return false;
        }
        // カーソルを行頭に移動
        MoveToBeginningOfLine(cur) {
            this.checkValidCursor(cur);
            cur.row = 0;
        }
        MoveToBeginningOfDocument(cur) {
            this.checkValidCursor(cur);
            cur.line = this.Buffers;
            cur.row = 0;
        }
        // 区間[start, end)の文字数をカウントする。endは含まれないので注意
        strlen(start, end) {
            this.checkValidCursor(start);
            this.checkValidCursor(end);
            let n = 0;
            const c = this.DuplicateCursor(start);
            for (;;) {
                if (this.EqualCursor(c, end)) {
                    break;
                }
                this.CursorForward(c);
                n++;
            }
            this.DisposeCursor(c);
            return n;
        }
    }
    class TextVRAM {
        get Width() { return this.width; }
        get Height() { return this.height; }
        constructor(font, width, height) {
            this.font = font;
            this.width = width;
            this.height = height;
            this.ColorPalette = new Uint32Array(16);
            for (let i = 0; i < 8; i++) {
                this.SetPaletteColor(i, (255 * (i >> 2)), (255 * (i & 1)), (255 * ((i >> 1) & 1)));
            }
            for (let i = 8; i < 16; i++) {
                //8以上は全て白に初期化
                this.SetPaletteColor(i, 255, 255, 255);
            }
            this.Pixels = new Uint32Array(this.width * this.height * 2);
            this.BackgroundColor = 0;
            this.TextColor = 7;
            this.CursorPtr = 0;
            this.ClearScreen();
        }
        ClearScreen() {
            this.Pixels.fill(0);
        }
        SetPaletteColor(index, r, g, b) {
            const color = (0xFF << 24) | (r << 16) | (g << 8) | (b << 0);
            this.ColorPalette[index & 0x0F] = color;
        }
        GetCursorPtr() {
            return this.CursorPtr;
        }
        GetCursorPosition() {
            return {
                X: ~~(this.CursorPtr % this.width),
                Y: ~~(this.CursorPtr / this.width)
            };
        }
        SetCursorPosition(x, y) {
            if (x < 0 || x >= this.width || y < 0 || y >= this.height) {
                return;
            }
            this.CursorPtr = y * this.width + x;
        }
        GetVramPtr() { return this.Pixels; }
        GetPalettePtr() { return this.ColorPalette; }
        GetBackgroundColor() { return this.BackgroundColor; }
        SetBackgroundColor(color) { this.BackgroundColor = color; }
        FillBackgroundColor(x, y, length, palette) {
            if (x < 0 || x >= this.width || y < 0 || y >= this.height) {
                return;
            }
            let n = y * this.width + x;
            while (length-- > 0) {
                const index = n * 2 + 1;
                this.Pixels[index] = (this.Pixels[index] & 0xFFFFFF0F) | (palette << 4);
                n++;
            }
        }
        GetTextColor() { return this.TextColor; }
        SetTextColor(color) { this.TextColor = color; }
        FillTextColor(pos, length, palette) {
            if (pos.X < 0 || pos.X >= this.width || pos.Y < 0 || pos.Y >= this.height) {
                return;
            }
            let n = pos.Y * this.width + pos.X;
            while (length-- > 0) {
                const index = n * 2 + 1;
                this.Pixels[index] = (this.Pixels[index] & 0xFFFFFFF0) | (palette << 0);
                n++;
            }
        }
        Scroll() {
            this.Pixels.copyWithin(0, this.width * 2, this.Pixels.length - this.width * 2);
            this.Pixels.fill(0, this.Pixels.length - this.width * 2, this.Pixels.length);
        }
        putch(n) {
            //カーソル位置にテキストコードnを1文字表示し、カーソルを1文字進める
            //画面最終文字表示してもスクロールせず、次の文字表示時にスクロールする
            let sz = this.font.getWidth(n);
            if (this.CursorPtr < 0 || this.CursorPtr > this.width * this.height) {
                // VRAM外への描画
                return;
            }
            if (this.CursorPtr + sz - 1 >= this.width * this.height) {
                // 画面末尾での描画
                this.Scroll();
                this.CursorPtr = this.width * (this.height - 1);
            }
            if (n == 0x0A) {
                //改行
                this.CursorPtr += this.width - (this.CursorPtr % this.width);
            }
            else {
                // 残り空きセル数を取得
                const rest = this.width - (this.CursorPtr % this.width);
                if (rest < sz) {
                    // 文字を挿入すると画面端をはみ出す場合、その位置に空白入れて改行したことにする
                    this.Pixels[this.CursorPtr * 2 + 0] = 0;
                    // 文字色と背景色はそのままにしておくほうがいいのかな
                    //this.Pixels[this.CursorPtr*2+1] = (this.Pixels[this.CursorPtr*2+1] & 0xFFFFFFF0) | (this.TextColor << 0);
                    //this.Pixels[this.CursorPtr*2+1] = (this.Pixels[this.CursorPtr*2+1] & 0xFFFFFF0F) | (this.BackgroundColor << 4);
                    this.CursorPtr += this.width - (this.CursorPtr % this.width);
                    this.putch(n);
                }
                else {
                    // 文字を挿入する
                    this.Pixels[this.CursorPtr * 2 + 0] = n;
                    this.Pixels[this.CursorPtr * 2 + 1] = (this.Pixels[this.CursorPtr * 2 + 1] & 0xFFFFFFF0) | (this.TextColor << 0);
                    this.Pixels[this.CursorPtr * 2 + 1] = (this.Pixels[this.CursorPtr * 2 + 1] & 0xFFFFFF0F) | (this.BackgroundColor << 4);
                    this.CursorPtr++;
                    sz--;
                    while (sz > 0) {
                        this.Pixels[this.CursorPtr * 2 + 0] = 0;
                        this.Pixels[this.CursorPtr * 2 + 1] = 0;
                        this.CursorPtr++;
                        sz--;
                    }
                }
            }
        }
        puts(str) {
            for (let i = 0; i < str.length; i++) {
                this.putch(str[i]);
            }
        }
        //カーソル位置に符号なし整数nを10進数表示
        putdigit(n) {
            const n1 = ~~(n / 10);
            let d = 1;
            while (n1 >= d) {
                d *= 10;
            }
            while (d != 0) {
                this.putch(0x30 + ~~(n / d));
                n %= d;
                d = ~~(d / 10);
            }
        }
        //カーソル位置に符号なし整数nをe桁の10進数表示（前の空き桁部分はスペースで埋める）
        putdigit2(n, e) {
            if (e == 0) {
                return;
            }
            const n1 = ~~(n / 10);
            let d = 1;
            e--;
            while (e > 0 && n1 >= d) {
                d *= 10;
                e--;
            }
            if (e == 0 && n1 > d) {
                n %= d * 10;
            }
            for (; e > 0; e--) {
                this.putch(0x20);
            }
            while (d != 0) {
                this.putch(0x30 + ~~(n / d));
                n %= d;
                d = ~~(d / 10);
            }
        }
        // カーソル位置p, 画面幅widthにおいて、 Utf32文字列strの範囲[head,cur) を入力した場合の入力後のカーソル位置を求める
        CalcCursorPosition(str, head, cur, p, width) {
            for (;;) {
                if (head == cur) {
                    return;
                }
                // １文字先（移動先）を読む
                if (str.length == head) {
                    // 1文字先が文末なので打ち切り
                    return;
                }
                const c1 = str[head];
                const w1 = this.font.getWidth(head++);
                // ２文字先を読む（折り返し判定のため）
                const w2 = this.font.getWidth(head++);
                // 全角文字の回り込みを考慮して改行が必要か判定
                if (c1 == 0x0A) {
                    p.X = 0;
                    p.Y++;
                }
                else if (p.X + w1 + w2 > width) {
                    p.X = 0;
                    p.Y++;
                }
                else {
                    p.X += w1;
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
            this.vram = null;
            this.insertMode = true;
            /**
            * 現在のカーソル位置に対応するテキスト位置
            * [0] は現在位置を示す値
            * [1] はsave_cursorで退避された値
            */
            this.Cursor = [null, null];
            /**
            * 現在表示中の画面の左上に対応するテキスト位置
            * [0] は現在位置を示す値
            * [1] はsave_cursorで退避された値
            */
            this.DisplayLeftTop = [null, null];
            /**
            * 画面上でのカーソルの位置
            * [0] は現在位置を示す値
            * [1] はsave_cursorで退避された値
            */
            this.CursorScreenPos = [null, null];
            //上下移動時の仮カーソルX座標
            this.cx2 = 0;
            // 範囲選択時のカーソルのスタート位置
            this.SelectStart = null;
            // 範囲選択時のカーソルのスタート座標（画面上）
            this.SelectStartCursorScreenPos = null;
            //保存後に変更されたかを表すフラグ
            this.edited = false;
            //ファイルアクセス用バッファ
            this.filebuf = "";
            //編集中のファイル名
            this.CurrentFileName = "";
            // 列挙したファイル表示用
            this.filenames = [];
            // クリップボードデータ
            this.clipboard = null;
            ///////////////////
            this.menuStr = Unicode.strToUtf32("F1:LOAD F2:SAVE   F4:NEW ");
            this.font = font;
            this.vram = vram;
            // テキストバッファの初期化
            this.textBuffer = new TextBuffer();
            // カーソルをリセット
            this.Cursor = [this.textBuffer.AllocateCursor(), null];
            this.DisplayLeftTop = [this.textBuffer.AllocateCursor(), null];
            this.CursorScreenPos = [{ X: 0, Y: 0 }, { X: 0, Y: 0 }];
            this.SelectStart = null;
            this.SelectStartCursorScreenPos = { X: 0, Y: 0 };
            // カーソル位置を先頭に
            this.cursor_top();
            //編集済みフラグクリア
            this.edited = false;
            // 挿入モードに設定
            this.insertMode = true;
        }
        GetCursorScreenPosX() { return this.CursorScreenPos[0].X; }
        GetCursorScreenPosY() { return this.CursorScreenPos[0].Y; }
        get EDITOR_SCREEN_HEIGHT() {
            return this.vram.Height - 1;
        }
        clear() {
            if (this.SelectStart != null) {
                this.SelectStart.Dispose();
                this.SelectStart = null;
            }
            this.CursorScreenPos = [{ X: 0, Y: 0 }, { X: 0, Y: 0 }];
            // カーソル位置を先頭に
            this.cursor_top();
            //編集済みフラグクリア
            this.edited = false;
            // 挿入モードに設定
            this.insertMode = true;
            // バッファクリア（カーソルも全部補正される）
            this.textBuffer.clear();
        }
        /**
         * 画面上のカーソル位置と再算出した位置情報が一致しているか検査（デバッグ用）
         */
        ValidateDisplayCursorPos() {
            const p = this.calc_screen_pos_from_line_head(this.DisplayLeftTop[0], this.Cursor[0]);
            assert((p.X == this.CursorScreenPos[0].X) && (p.Y == this.CursorScreenPos[0].Y));
        }
        // 画面上の位置と折り返しを考慮に入れながらカーソルと画面上の位置を連動させてひとつ進める
        iterate_cursor_with_screen_pos(p, cur) {
            // １文字先（移動先）を読む
            const c1 = this.textBuffer.TakeCharacatorOnCursor(cur);
            if (c1 == 0x0000) {
                // 1文字先が文末なので打ち切り
                return false;
            }
            const w1 = this.font.getWidth(c1);
            // ２文字先を読む（後述の折り返し判定のため）
            this.textBuffer.CursorForward(cur);
            const c2 = this.textBuffer.TakeCharacatorOnCursor(cur);
            const w2 = this.font.getWidth(c2);
            // タブ・折り返しを考慮して文字の位置を決定
            if (c1 == 0x0A) {
                // １文字目が改行文字の場合は次行に移動
                p.X = 0;
                p.Y++;
            }
            else if (c1 == 0x09) {
                // タブ文字の場合
                const tabSize = 4 - (p.X % 4);
                // タブの位置に応じて処理変更
                if (p.X + tabSize >= this.vram.Width) {
                    // タブで行末まで埋まる
                    p.X = 0;
                    p.Y++;
                }
                else {
                    // 行末までは埋まらないのでタブを入れる
                    p.X += tabSize;
                    // 次の文字を改めてチェックして折り返すかどうか考える
                    if (p.X + w2 > this.vram.Width) {
                        p.X = 0;
                        p.Y++;
                    }
                }
            }
            else if (p.X + w1 + w2 > this.vram.Width) {
                // 一文字目、二文字目両方を受け入れると行末を超える場合
                p.X = 0;
                p.Y++;
            }
            else {
                p.X += w1;
            }
            return true;
        }
        // headのスクリーン座標を(0,0)とした場合のcurのスクリーン座標位置を算出
        calc_screen_pos_from_line_head(head, cur) {
            head.CheckValid();
            cur.CheckValid();
            const p = { X: 0, Y: 0 };
            const h = head.Duplicate();
            while (text_buffer_cursor_t.Equal(h, cur) == false) {
                if (this.iterate_cursor_with_screen_pos(p, h) == false) {
                    break;
                }
            }
            h.Dispose();
            return p;
        }
        // 画面原点を基準に現在の行の画面上での最左位置に対応する位置にカーソルを移動
        move_cursor_to_display_line_head(pos, cur) {
            cur.CheckValid();
            const p = { X: 0, Y: 0 };
            const c = this.DisplayLeftTop[0].Duplicate();
            const cprev = this.DisplayLeftTop[0].Duplicate();
            // タブや折り返しなどがあるため、単純に逆順に辿ることができない場合もあって場合分けすると面倒くさい
            //画面原点に対応するバッファカーソルは常に計算済みなのでそこからの距離を求める
            let result = false;
            for (;;) {
                const pprev = { X: p.X, Y: p.Y };
                c.CopyTo(cprev);
                if (p.Y == pos.Y && p.X == 0) {
                    pos.X = p.X;
                    pos.Y = p.Y;
                    c.CopyTo(cur);
                    result = true;
                    break;
                }
                else if (this.iterate_cursor_with_screen_pos(p, c) == false) {
                    pos.X = pprev.X;
                    pos.Y = pprev.Y;
                    cprev.CopyTo(cur);
                    result = false;
                    break;
                }
                else if (p.Y == pos.Y) {
                    assert(p.X == 0);
                    pos.X = p.X;
                    pos.Y = p.Y;
                    c.CopyTo(cur);
                    result = true;
                    break;
                }
            }
            c.Dispose();
            cprev.Dispose();
            return result;
        }
        // 画面上で現在行の末尾まで移動
        move_cursor_to_display_line_tail(pos, cur) {
            cur.CheckValid();
            const p = { X: pos.X, Y: pos.Y };
            const c = cur.Duplicate();
            const cprev = cur.Duplicate();
            let result = false;
            for (;;) {
                const pprev = { X: p.X, Y: p.Y };
                c.CopyTo(cprev);
                if (this.iterate_cursor_with_screen_pos(p, c) == false) {
                    pos.X = p.X;
                    pos.Y = p.Y;
                    c.CopyTo(cur);
                    result = false;
                    break;
                }
                else if (p.Y == pos.Y + 1) {
                    pos.X = pprev.X;
                    pos.Y = pprev.Y;
                    cprev.CopyTo(cur);
                    result = true;
                    break;
                }
            }
            c.Dispose();
            cprev.Dispose();
            return result;
        }
        // 画面上で現在の文字の手前まで移動
        move_cursor_to_prev(pos, cur) {
            cur.CheckValid();
            const p = { X: 0, Y: 0 };
            const pprev = { X: 0, Y: 0 };
            const c = this.DisplayLeftTop[0].Duplicate();
            const cprev = this.DisplayLeftTop[0].Duplicate();
            while (!(p.Y == pos.Y && p.X == pos.X)) {
                pprev.X = p.X;
                pprev.Y = p.Y;
                c.CopyTo(cprev);
                assert(this.iterate_cursor_with_screen_pos(p, c));
            }
            pos.X = pprev.X;
            pos.Y = pprev.Y;
            cprev.CopyTo(cur);
            c.Dispose();
            cprev.Dispose();
            return true;
        }
        move_cursor_to_display_pos(target, rp, c) {
            const p = { X: 0, Y: 0 };
            const pp = { X: 0, Y: 0 };
            this.DisplayLeftTop[0].CopyTo(c);
            const pc = this.DisplayLeftTop[0].Duplicate();
            let ret = false;
            for (;;) {
                if (p.Y == target.Y && p.X >= target.X) {
                    rp.X = p.X;
                    rp.Y = p.Y;
                    ret = true;
                    break;
                }
                if (p.Y > target.Y) {
                    rp.X = pp.X;
                    rp.Y = pp.Y;
                    pc.CopyTo(c);
                    ret = true;
                    break;
                }
                pp.X = p.X;
                pp.Y = p.Y;
                c.CopyTo(pc);
                if (this.iterate_cursor_with_screen_pos(p, c) == false) {
                    // 文書末に到達
                    rp.X = pp.X;
                    rp.Y = pp.Y;
                    pc.CopyTo(c);
                    ret = false;
                    break;
                }
            }
            pc.Dispose();
            return ret;
        }
        // カーソル位置で１文字上書きする
        OverwriteCharactor(ch) {
            if (this.textBuffer.OverwriteCharacatorOnCursor(this.Cursor[0], ch) == false) {
                return false;
            }
            this.CursorScreenPos[0] = this.calc_screen_pos_from_line_head(this.DisplayLeftTop[0], this.Cursor[0]);
            this.ValidateDisplayCursorPos();
            return true;
        }
        // カーソル位置に１文字挿入する
        InsertCharactor(ch) {
            if (this.textBuffer.InsertCharacatorOnCursor(this.Cursor[0], ch) == false) {
                return false;
            }
            this.CursorScreenPos[0] = this.calc_screen_pos_from_line_head(this.DisplayLeftTop[0], this.Cursor[0]);
            this.ValidateDisplayCursorPos();
            return true;
        }
        // 画面を１行下にスクロールする
        screen_scrolldown() {
            const c = this.DisplayLeftTop[0].Duplicate();
            const p = { X: 0, Y: 0 };
            for (;;) {
                if (p.Y == 1) {
                    c.CopyTo(this.DisplayLeftTop[0]);
                    this.CursorScreenPos[0].Y -= 1;
                    c.Dispose();
                    return true;
                }
                if (this.iterate_cursor_with_screen_pos(p, c) == false) {
                    // 下の行がないのでどうしようもない
                    c.Dispose();
                    return false;
                }
            }
        }
        screen_scrollup() {
            // カーソルの画面Ｙ座標が最上列なので左上座標の更新が必要
            const lt = this.DisplayLeftTop[0].Duplicate();
            if (this.textBuffer.CursorBackward(lt)) {
                // 左上から１文字戻る＝画面上における前の行の末尾にバッファ上のカーソルを移動
                // 行頭位置を算出
                const c = lt.Duplicate();
                this.textBuffer.MoveToBeginningOfLine(c);
                // 行頭位置を(0,0)点としてカーソルltの画面上位置を計算
                const lp = this.calc_screen_pos_from_line_head(c, lt);
                // ltの画面位置を超えない位置の行頭をサーチ
                const p = { X: 0, Y: 0 };
                while (p.Y != lp.Y) {
                    this.iterate_cursor_with_screen_pos(p, c);
                }
                // 見つかったところが新しい左上位置
                c.CopyTo(this.DisplayLeftTop[0]);
                this.CursorScreenPos[0].Y += 1;
                this.ValidateDisplayCursorPos();
                c.Dispose();
                lt.Dispose();
                return true;
            }
            else {
                // this.DisplayLeftTopはバッファ先頭なので戻れない
                lt.Dispose();
                return false;
            }
        }
        ////////////////////
        // カーソルを1つ左に移動
        cursor_left_() {
            if (this.CursorScreenPos[0].X == 0 && this.CursorScreenPos[0].Y == 0) {
                if (this.screen_scrollup() == false) {
                    // 上にスクロールできない＝先頭なので戻れない
                    return;
                }
            }
            this.move_cursor_to_prev(this.CursorScreenPos[0], this.Cursor[0]);
            this.cx2 = this.CursorScreenPos[0].X;
        }
        // カーソルを1つ左に移動
        cursor_left() {
            this.ValidateDisplayCursorPos();
            this.cursor_left_();
            this.ValidateDisplayCursorPos();
        }
        // カーソルを右に移動
        cursor_right_() {
            if (this.iterate_cursor_with_screen_pos(this.CursorScreenPos[0], this.Cursor[0])) {
                if (this.CursorScreenPos[0].Y == this.EDITOR_SCREEN_HEIGHT) {
                    // カーソルを進めることに成功したらカーソル位置が画面最下段からはみ出たので
                    // 画面を一行下にスクロールする
                    this.screen_scrolldown();
                }
            }
            this.cx2 = this.CursorScreenPos[0].X;
        }
        // カーソルを右に移動
        cursor_right() {
            this.ValidateDisplayCursorPos();
            this.cursor_right_();
            this.ValidateDisplayCursorPos();
        }
        cursor_up_() {
            if (this.CursorScreenPos[0].Y == 0) {
                if (this.screen_scrollup() == false) {
                    // 画面を上にスクロールできない＝戻れないので終了
                    return;
                }
            }
            this.ValidateDisplayCursorPos();
            // 左上の補正が終った
            // 左上位置から画面上で１行上の位置をサーチする
            const target = { X: this.cx2, Y: this.CursorScreenPos[0].Y - 1 };
            const p = { X: 0, Y: 0 };
            const c = this.textBuffer.AllocateCursor();
            this.move_cursor_to_display_pos(target, p, c);
            c.CopyTo(this.Cursor[0]);
            this.CursorScreenPos[0] = p;
            c.Dispose();
            this.ValidateDisplayCursorPos();
        }
        cursor_up() {
            this.ValidateDisplayCursorPos();
            this.cursor_up_();
            this.ValidateDisplayCursorPos();
        }
        cursor_down_() {
            // 左上位置からサーチする
            const target = { X: this.cx2, Y: this.CursorScreenPos[0].Y + 1 };
            const p = { X: 0, Y: 0 };
            const c = this.textBuffer.AllocateCursor();
            if (this.move_cursor_to_display_pos(target, p, c) == false) {
                if (p.Y != target.Y) {
                    // 次の行に移動できていない＝最下段の行で下を押した
                    c.Dispose();
                    return;
                }
            }
            c.CopyTo(this.Cursor[0]);
            this.CursorScreenPos[0] = p;
            c.Dispose();
            // 画面を一行下にスクロールが必要か？
            if (this.CursorScreenPos[0].Y == this.EDITOR_SCREEN_HEIGHT) {
                // 必要
                if (this.screen_scrolldown() == false) {
                    // 移動できない＝スクロールできないので終り
                    return;
                }
            }
            this.ValidateDisplayCursorPos();
        }
        cursor_down() {
            this.ValidateDisplayCursorPos();
            this.cursor_down_();
            this.ValidateDisplayCursorPos();
        }
        // カーソルを行頭に移動する
        cursor_home_() {
            this.move_cursor_to_display_line_head(this.CursorScreenPos[0], this.Cursor[0]);
            this.cx2 = 0;
        }
        cursor_home() {
            this.ValidateDisplayCursorPos();
            this.cursor_home_();
            this.ValidateDisplayCursorPos();
        }
        // カーソルを行末に移動する
        cursor_end_() {
            this.move_cursor_to_display_line_tail(this.CursorScreenPos[0], this.Cursor[0]);
            this.cx2 = this.CursorScreenPos[0].X;
        }
        cursor_end() {
            this.ValidateDisplayCursorPos();
            this.cursor_end_();
            this.ValidateDisplayCursorPos();
        }
        cursor_pageup_() {
            //PageUpキー
            //最上行が最下行になるまでスクロール
            //出力：下記変数を移動先の値に変更
            //cursorbp,cursorix バッファ上のカーソル位置
            //cx,cx2
            //cy
            //disptopbp,disptopix 画面左上のバッファ上の位置
            const cy_old = this.CursorScreenPos[0].Y;
            while (this.CursorScreenPos[0].Y > 0) {
                this.cursor_up(); // cy==0になるまでカーソルを上に移動
            }
            let i = 0;
            const prev = this.textBuffer.AllocateCursor();
            for (i = 0; i < this.EDITOR_SCREEN_HEIGHT - 1; i++) {
                //画面行数-1行分カーソルを上に移動
                this.DisplayLeftTop[0].CopyTo(prev);
                this.cursor_up();
                if (this.textBuffer.EqualCursor(prev, this.DisplayLeftTop[0])) {
                    break; //最上行で移動できなかった場合抜ける
                }
            }
            prev.Dispose();
            //元のY座標までカーソルを下に移動、1行も動かなかった場合は最上行に留まる
            if (i > 0) {
                while (this.CursorScreenPos[0].Y < cy_old) {
                    this.cursor_down();
                }
            }
        }
        cursor_pageup() {
            this.ValidateDisplayCursorPos();
            this.cursor_pageup_();
            this.ValidateDisplayCursorPos();
        }
        cursor_pagedown_() {
            //PageDownキー
            //最下行が最上行になるまでスクロール
            //出力：下記変数を移動先の値に変更
            //cursorbp,cursorix バッファ上のカーソル位置
            //cx,cx2
            //cy
            //disptopbp,disptopix 画面左上のバッファ上の位置
            const cy_old = this.CursorScreenPos[0].Y;
            while (this.CursorScreenPos[0].Y < this.EDITOR_SCREEN_HEIGHT - 1) {
                // cy==EDITWIDTH-1になるまでカーソルを下に移動
                const y = this.CursorScreenPos[0].Y;
                this.cursor_down();
                if (y == this.CursorScreenPos[0].Y) {
                    break; // バッファ最下行で移動できなかった場合抜ける
                }
            }
            let i = 0;
            const prev = this.textBuffer.AllocateCursor();
            for (i = 0; i < this.EDITOR_SCREEN_HEIGHT - 1; i++) {
                //画面行数-1行分カーソルを下に移動
                this.DisplayLeftTop[0].CopyTo(prev);
                this.cursor_down();
                if (this.textBuffer.EqualCursor(prev, this.DisplayLeftTop[0])) {
                    break; //最下行で移動できなかった場合抜ける
                }
            }
            prev.Dispose();
            //下端からさらに移動した行数分、カーソルを上に移動、1行も動かなかった場合は最下行に留まる
            if (i > 0) {
                while (this.CursorScreenPos[0].Y > cy_old) {
                    this.cursor_up();
                }
            }
        }
        cursor_pagedown() {
            this.ValidateDisplayCursorPos();
            this.cursor_pagedown_();
            this.ValidateDisplayCursorPos();
        }
        cursor_top() {
            //カーソルをテキストバッファの先頭に移動
            this.textBuffer.MoveToBeginningOfDocument(this.Cursor[0]);
            //範囲選択モード解除
            if (this.SelectStart != null) {
                this.SelectStart.Dispose();
                this.SelectStart = null;
            }
            // 画面の左上位置をリセット
            this.Cursor[0].CopyTo(this.DisplayLeftTop[0]);
            // 画面上カーソル位置をリセット
            this.CursorScreenPos[0].X = 0;
            this.CursorScreenPos[0].Y = 0;
            // 最後の横移動位置をリセット
            this.cx2 = 0;
        }
        ///////////////////
        // 選択範囲をクリップボードにコピー
        Clipboard_CopyTo() {
            const bp1 = this.textBuffer.AllocateCursor();
            const bp2 = this.textBuffer.AllocateCursor();
            //範囲選択モードの場合、開始位置と終了の前後判断して
            //bp1,ix1を開始位置、bp2,ix2を終了位置に設定
            if (this.CursorScreenPos[0].Y < this.SelectStartCursorScreenPos.Y ||
                (this.CursorScreenPos[0].Y == this.SelectStartCursorScreenPos.Y && this.CursorScreenPos[0].X < this.SelectStartCursorScreenPos.X)) {
                this.Cursor[0].CopyTo(bp1);
                this.SelectStart.CopyTo(bp2);
            }
            else {
                this.SelectStart.CopyTo(bp1);
                this.Cursor[0].CopyTo(bp2);
            }
            const pd = [];
            while (this.textBuffer.EqualCursor(bp1, bp2) == false) {
                pd.push(this.textBuffer.TakeCharacatorOnCursor(bp1));
                this.textBuffer.CursorForward(bp1);
            }
            this.clipboard = new Uint32Array(pd);
            bp1.Dispose();
            bp2.Dispose();
        }
        Clipboard_PasteFrom() {
            // クリップボードから貼り付け
            for (let i = 0; i < this.clipboard.length; i++) {
                if (this.InsertCharactor(this.clipboard[i]) == false) {
                    break;
                }
                this.cursor_right(); //画面上、バッファ上のカーソル位置を1つ後ろに移動
            }
        }
        ///////////////////
        //範囲選択モード開始時のカーソル開始位置グローバル変数設定
        set_areamode() {
            if (this.SelectStart != null) {
                this.SelectStart.Dispose();
                this.SelectStart = null;
            }
            this.SelectStart = this.Cursor[0].Duplicate();
            this.SelectStartCursorScreenPos.X = this.CursorScreenPos[0].X;
            this.SelectStartCursorScreenPos.Y = this.CursorScreenPos[0].Y;
        }
        countarea() {
            if (this.SelectStart == null) {
                return 0;
            }
            //テキストバッファの指定範囲の文字数をカウント
            //範囲は(cursorbp,cursorix)と(SelectStart.Buffer,SelectStart.Index)で指定
            //後ろ側の一つ前の文字までをカウント
            //選択範囲の開始位置と終了の前後を判断して開始位置と終了位置を設定
            if (this.CursorScreenPos[0].Y < this.SelectStartCursorScreenPos.Y ||
                (this.CursorScreenPos[0].Y == this.SelectStartCursorScreenPos.Y && this.CursorScreenPos[0].X < this.SelectStartCursorScreenPos.X)) {
                return this.textBuffer.strlen(this.Cursor[0], this.SelectStart);
            }
            else {
                return this.textBuffer.strlen(this.SelectStart, this.Cursor[0]);
            }
        }
        // テキストバッファの指定範囲を削除
        deletearea() {
            if (this.SelectStart == null) {
                return;
            }
            //範囲は(cursorbp,cursorix)と(SelectStart.Buffer,SelectStart.Index)で指定
            //後ろ側の一つ前の文字までを削除
            //削除後のカーソル位置は選択範囲の先頭にし、範囲選択モード解除する
            const n = this.countarea(); //選択範囲の文字数カウント
            //範囲選択の開始位置と終了位置の前後を判断してカーソルを開始位置に設定
            if (this.CursorScreenPos[0].Y > this.SelectStartCursorScreenPos.Y || (this.CursorScreenPos[0].Y == this.SelectStartCursorScreenPos.Y && this.CursorScreenPos[0].X > this.SelectStartCursorScreenPos.X)) {
                this.SelectStart.CopyTo(this.Cursor[0]);
                this.CursorScreenPos[0].X = this.SelectStartCursorScreenPos.X;
                this.CursorScreenPos[0].Y = this.SelectStartCursorScreenPos.Y;
            }
            this.cx2 = this.CursorScreenPos[0].X;
            //範囲選択モード解除
            if (this.SelectStart != null) {
                this.SelectStart.Dispose();
                this.SelectStart = null;
            }
            // 始点からn文字削除
            this.textBuffer.DeleteArea(this.Cursor[0], n);
            this.CursorScreenPos[0] = this.calc_screen_pos_from_line_head(this.DisplayLeftTop[0], this.Cursor[0]);
        }
        checkResetSelectArea() {
            if (this.SelectStart != null &&
                this.CursorScreenPos[0].X == this.SelectStartCursorScreenPos.X &&
                this.CursorScreenPos[0].Y == this.SelectStartCursorScreenPos.Y) {
                //選択範囲の開始と終了が重なったら範囲選択モード解除
                this.SelectStart.Dispose();
                this.SelectStart = null;
            }
        }
        ///////////////////
        //カーソル関連グローバル変数を一時避難
        save_cursor() {
            assert(this.Cursor[1] == null);
            assert(this.DisplayLeftTop[1] == null);
            this.Cursor[1] = this.Cursor[0].Duplicate();
            this.DisplayLeftTop[1] = this.DisplayLeftTop[0].Duplicate();
            this.CursorScreenPos[1].X = this.CursorScreenPos[0].X;
            this.CursorScreenPos[1].Y = this.CursorScreenPos[0].Y;
        }
        //カーソル関連グローバル変数を一時避難場所から戻す
        restore_cursor() {
            assert(this.Cursor[0] != null);
            assert(this.DisplayLeftTop[0] != null);
            assert(this.Cursor[1] != null);
            assert(this.DisplayLeftTop[1] != null);
            this.Cursor[0].Dispose();
            this.Cursor[0] = this.Cursor[1];
            this.Cursor[1] = null;
            this.DisplayLeftTop[0].Dispose();
            this.DisplayLeftTop[0] = this.DisplayLeftTop[1];
            this.DisplayLeftTop[1] = null;
            this.CursorScreenPos[0].X = this.CursorScreenPos[1].X;
            this.CursorScreenPos[1].X = 0;
            this.CursorScreenPos[0].Y = this.CursorScreenPos[1].Y;
            this.CursorScreenPos[1].Y = 0;
        }
        discard_saved_cursor() {
            if (this.Cursor[1] != null) {
                this.Cursor[1].Dispose();
                this.Cursor[1] = null;
            }
            if (this.DisplayLeftTop[1] != null) {
                this.DisplayLeftTop[1].Dispose();
                this.DisplayLeftTop[1] = null;
            }
            this.CursorScreenPos[1].X = 0;
            this.CursorScreenPos[1].Y = 0;
        }
        ///////////////////
        redraw() {
            let cl = COLOR_NORMALTEXT;
            let select_start;
            let select_end;
            if (this.SelectStart == null) {
                //範囲選択モードでない場合
                select_start = null;
                select_end = null;
            }
            else {
                //範囲選択モードの場合、開始位置と終了の前後判断して
                //bp1 を開始位置、bp2 を終了位置に設定
                if (this.CursorScreenPos[0].Y < this.SelectStartCursorScreenPos.Y ||
                    (this.CursorScreenPos[0].Y == this.SelectStartCursorScreenPos.Y && this.CursorScreenPos[0].X < this.SelectStartCursorScreenPos.X)) {
                    select_start = this.Cursor[0].Duplicate();
                    select_end = this.SelectStart.Duplicate();
                }
                else {
                    select_start = this.SelectStart.Duplicate();
                    select_end = this.Cursor[0].Duplicate();
                }
            }
            this.vram.SetTextColor(COLOR_NORMALTEXT);
            this.vram.SetBackgroundColor(COLOR_NORMALTEXT_BG);
            // テキストVRAMへの書き込み
            // 必要なら割り込み禁止とか使うこと
            this.vram.ClearScreen();
            const vp = this.vram.GetVramPtr();
            const bp = this.DisplayLeftTop[0].Duplicate();
            const sp = { X: 0, Y: 0 };
            while (sp.Y < this.EDITOR_SCREEN_HEIGHT) {
                // 選択範囲の始点/終点に到達してたら色設定変更
                if (this.SelectStart != null) {
                    if (this.textBuffer.EqualCursor(bp, select_start)) {
                        cl = COLOR_AREASELECTTEXT;
                    }
                    if (this.textBuffer.EqualCursor(bp, select_end)) {
                        cl = COLOR_NORMALTEXT;
                    }
                }
                const ch = this.textBuffer.TakeCharacatorOnCursor(bp);
                const index = (sp.Y * this.vram.Width + sp.X) * 2;
                vp[index + 0] = ch;
                vp[index + 1] = (vp[index + 1] & 0xFFFFFFF0) | cl;
                //vp[sp.Y * VWIDTH_X + sp.X].bgcolor = bc;
                if (this.iterate_cursor_with_screen_pos(sp, bp) == false) {
                    break;
                }
            }
            bp.Dispose();
            if (select_start != null) {
                select_start.Dispose();
            }
            if (select_end != null) {
                select_end.Dispose();
            }
            //EnableInterrupt();
        }
        ///////////////////
        save(start, write, end, context) {
            let ret = false;
            if (start(context)) {
                ret = true;
                const bp = this.textBuffer.AllocateCursor();
                while (this.textBuffer.EndOfString(bp) == false) {
                    const ch = this.textBuffer.TakeCharacatorOnCursor(bp);
                    if (ch == 0x0000) {
                        break;
                    }
                    if (write(context, ch) == false) {
                        ret = false;
                        break;
                    }
                    this.textBuffer.CursorForward(bp);
                }
                bp.Dispose();
            }
            end(context);
            return ret;
        }
        load(start, read, end, context) {
            let ret = false;
            if (start(context)) {
                ret = true;
                this.clear();
                let ch = 0;
                while ((ch = read(context)) != 0) {
                    this.InsertCharactor(ch);
                    this.cursor_right(); //画面上、バッファ上のカーソル位置を1つ後ろに移動
                }
            }
            end(context);
            return ret;
        }
        displaybottomline() {
            //エディター画面最下行の表示
            this.vram.SetCursorPosition(0, this.vram.Height - 1);
            this.vram.SetTextColor(COLOR_BOTTOMLINE);
            this.vram.SetBackgroundColor(COLOR_BOTTOMLINE_BG);
            this.vram.puts(this.menuStr);
            this.vram.putdigit2(this.textBuffer.getTotalLength(), 5);
            this.vram.FillBackgroundColor(0, this.vram.Height - 1, this.vram.Width, COLOR_BOTTOMLINE_BG);
        }
        ///////////////////
        normal_code_process(k) {
            // 通常文字入力処理
            // k:入力された文字コード
            this.edited = true; //編集済みフラグを設定
            if (this.insertMode || k == 0x0A || this.SelectStart != null) { // 挿入モードの場合
                // 選択範囲を削除
                if (this.SelectStart != null) {
                    this.deletearea();
                }
                if (this.InsertCharactor(k)) {
                    this.cursor_right(); //画面上、バッファ上のカーソル位置を1つ後ろに移動
                }
            }
            else { //上書きモード
                if (this.OverwriteCharactor(k)) {
                    this.cursor_right(); //画面上、バッファ上のカーソル位置を1つ後ろに移動
                }
            }
        }
        control_code_process(k, sh) {
            // 制御文字入力処理
            // k:制御文字の仮想キーコード
            // sh:シフト関連キー状態
            this.save_cursor(); //カーソル関連変数退避（カーソル移動できなかった場合戻すため）
            switch (k) {
                case 37 /* VKEY_LEFT */:
                case 100 /* VKEY_NUMPAD4 */:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((sh & 3 /* CHK_SHIFT */) == 0 || (k == 100 /* VKEY_NUMPAD4 */) && (sh & 512 /* CHK_NUMLK */)) {
                        if (this.SelectStart != null) {
                            this.SelectStart.Dispose();
                            this.SelectStart = null;
                        }
                    }
                    else if (this.SelectStart == null) {
                        this.set_areamode(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    if (sh & 12 /* CHK_CTRL */) {
                        //CTRL＋左矢印でHome
                        this.cursor_home();
                        break;
                    }
                    this.cursor_left();
                    //if (SelectStart.Buffer != NULL && (this.DisplayLeftTop[0].Buffer != this.DisplayLeftTop[1].Buffer || this.DisplayLeftTop[0].Index != this.DisplayLeftTop[1].Index)) {
                    if (this.SelectStart != null && !this.textBuffer.EqualCursor(this.DisplayLeftTop[0], this.DisplayLeftTop[1])) {
                        //範囲選択モードで画面スクロールがあった場合
                        if (this.SelectStartCursorScreenPos.Y < this.EDITOR_SCREEN_HEIGHT - 1) {
                            this.SelectStartCursorScreenPos.Y++; //範囲スタート位置もスクロール
                        }
                        else {
                            this.restore_cursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                        }
                    }
                    break;
                case 39 /* VKEY_RIGHT */:
                case 102 /* VKEY_NUMPAD6 */:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((sh & 3 /* CHK_SHIFT */) == 0 || (k == 102 /* VKEY_NUMPAD6 */) && (sh & 512 /* CHK_NUMLK */)) {
                        if (this.SelectStart != null) {
                            this.SelectStart.Dispose();
                            this.SelectStart = null;
                        }
                    }
                    else if (this.SelectStart == null) {
                        this.set_areamode(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    if (sh & 12 /* CHK_CTRL */) {
                        //CTRL＋右矢印でEnd
                        this.cursor_end();
                        break;
                    }
                    this.cursor_right();
                    if (this.SelectStart != null && (this.textBuffer.EqualCursor(this.DisplayLeftTop[0], this.DisplayLeftTop[1]) == false)) {
                        //範囲選択モードで画面スクロールがあった場合
                        if (this.SelectStartCursorScreenPos.Y > 0)
                            this.SelectStartCursorScreenPos.Y--; //範囲スタート位置もスクロール
                        else
                            this.restore_cursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                    }
                    break;
                case 38 /* VKEY_UP */:
                case 104 /* VKEY_NUMPAD8 */:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((sh & 3 /* CHK_SHIFT */) == 0 || (k == 104 /* VKEY_NUMPAD8 */) && (sh & 512 /* CHK_NUMLK */)) {
                        if (this.SelectStart != null) {
                            this.SelectStart.Dispose();
                            this.SelectStart = null;
                        }
                    }
                    else if (this.SelectStart == null) {
                        this.set_areamode(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    this.cursor_up();
                    if (this.SelectStart != null && (this.textBuffer.EqualCursor(this.DisplayLeftTop[0], this.DisplayLeftTop[1]) == false)) {
                        //範囲選択モードで画面スクロールがあった場合
                        if (this.SelectStartCursorScreenPos.Y < this.EDITOR_SCREEN_HEIGHT - 1) {
                            this.SelectStartCursorScreenPos.Y++; //範囲スタート位置もスクロール
                        }
                        else {
                            this.restore_cursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                        }
                    }
                    break;
                case 40 /* VKEY_DOWN */:
                case 98 /* VKEY_NUMPAD2 */:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((sh & 3 /* CHK_SHIFT */) == 0 || (k == 98 /* VKEY_NUMPAD2 */) && (sh & 512 /* CHK_NUMLK */)) {
                        if (this.SelectStart != null) {
                            this.SelectStart.Dispose();
                            this.SelectStart = null;
                        }
                    }
                    else if (this.SelectStart == null) {
                        this.set_areamode(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    this.cursor_down();
                    if (this.SelectStart != null && (this.textBuffer.EqualCursor(this.DisplayLeftTop[0], this.DisplayLeftTop[1]) == false)) {
                        //範囲選択モードで画面スクロールがあった場合
                        if (this.SelectStartCursorScreenPos.Y > 0)
                            this.SelectStartCursorScreenPos.Y--; //範囲スタート位置もスクロール
                        else
                            this.restore_cursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                    }
                    break;
                case 36 /* VKEY_HOME */:
                case 103 /* VKEY_NUMPAD7 */:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((sh & 3 /* CHK_SHIFT */) == 0 || (k == 103 /* VKEY_NUMPAD7 */) && (sh & 512 /* CHK_NUMLK */)) {
                        if (this.SelectStart != null) {
                            this.SelectStart.Dispose();
                            this.SelectStart = null;
                        }
                    }
                    else if (this.SelectStart == null) {
                        this.set_areamode(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    this.cursor_home();
                    break;
                case 35 /* VKEY_END */:
                case 97 /* VKEY_NUMPAD1 */:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((sh & 3 /* CHK_SHIFT */) == 0 || (k == 97 /* VKEY_NUMPAD1 */) && (sh & 512 /* CHK_NUMLK */)) {
                        if (this.SelectStart != null) {
                            this.SelectStart.Dispose();
                            this.SelectStart = null;
                        }
                    }
                    else if (this.SelectStart == null) {
                        this.set_areamode(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    this.cursor_end();
                    break;
                case 33 /* VKEY_PRIOR */: // PageUpキー
                case 105 /* VKEY_NUMPAD9 */:
                    //シフト＋PageUpは無効（NumLock＋シフト＋「9」除く）
                    if ((sh & 3 /* CHK_SHIFT */) && ((k != 105 /* VKEY_NUMPAD9 */) || ((sh & 512 /* CHK_NUMLK */) == 0))) {
                        break;
                    }
                    //範囲選択モード解除
                    if (this.SelectStart != null) {
                        this.SelectStart.Dispose();
                        this.SelectStart = null;
                    }
                    this.cursor_pageup();
                    break;
                case 34 /* VKEY_NEXT */: // PageDownキー
                case 99 /* VKEY_NUMPAD3 */:
                    //シフト＋PageDownは無効（NumLock＋シフト＋「3」除く）
                    if ((sh & 3 /* CHK_SHIFT */) && ((k != 99 /* VKEY_NUMPAD3 */) || ((sh & 512 /* CHK_NUMLK */) == 0))) {
                        break;
                    }
                    //範囲選択モード解除
                    if (this.SelectStart != null) {
                        this.SelectStart.Dispose();
                        this.SelectStart = null;
                    }
                    this.cursor_pagedown();
                    break;
                case 46 /* VKEY_DELETE */: //Deleteキー
                case 110 /* VKEY_DECIMAL */: //テンキーの「.」
                    this.edited = true; //編集済みフラグ
                    if (this.SelectStart != null) {
                        this.deletearea(); //選択範囲を削除
                    }
                    else {
                        this.textBuffer.DeleteCharacatorOnCursor(this.Cursor[0]);
                        this.CursorScreenPos[0] = this.calc_screen_pos_from_line_head(this.DisplayLeftTop[0], this.Cursor[0]);
                    }
                    break;
                case 8 /* VKEY_BACK */: //BackSpaceキー
                    this.edited = true; //編集済みフラグ
                    if (this.SelectStart != null) {
                        this.deletearea(); //選択範囲を削除
                        break;
                    }
                    if (this.textBuffer.StartOfString(this.Cursor[0])) {
                        break; //バッファ先頭では無視
                    }
                    this.cursor_left();
                    this.textBuffer.DeleteCharacatorOnCursor(this.Cursor[0]);
                    this.CursorScreenPos[0] = this.calc_screen_pos_from_line_head(this.DisplayLeftTop[0], this.Cursor[0]);
                    break;
                case 45 /* VKEY_INSERT */:
                case 96 /* VKEY_NUMPAD0 */:
                    this.insertMode = !this.insertMode; //挿入モード、上書きモードを切り替え
                    break;
                case 67 /* VKEY_KEY_C */:
                    //CTRL+C、クリップボードにコピー
                    if (this.SelectStart != null && (sh & 12 /* CHK_CTRL */)) {
                        this.Clipboard_CopyTo();
                    }
                    break;
                case 88 /* VKEY_KEY_X */:
                    //CTRL+X、クリップボードに切り取り
                    if (this.SelectStart != null && (sh & 12 /* CHK_CTRL */)) {
                        this.Clipboard_CopyTo();
                        this.deletearea(); //選択範囲の削除
                        this.edited = true; //編集済みフラグ
                    }
                    break;
                case 86 /* VKEY_KEY_V */:
                    //CTRL+V、クリップボードから貼り付け
                    if ((sh & 12 /* CHK_CTRL */) == 0)
                        break;
                    if (this.clipboard == null || this.clipboard.length == 0)
                        break;
                    this.edited = true; //編集済みフラグ
                    if (this.SelectStart != null) {
                        //範囲選択している時は削除してから貼り付け
                        this.deletearea(); //選択範囲を削除
                        this.Clipboard_PasteFrom(); //クリップボード貼り付け
                    }
                    else {
                        this.Clipboard_PasteFrom(); //クリップボード貼り付け
                    }
                    break;
                case 83 /* VKEY_KEY_S */:
                    //CTRL+S、SDカードに保存
                    if ((sh & 12 /* CHK_CTRL */) == 0)
                        break;
                case 113 /* VKEY_F2 */: //F2キー
                    //this.save_as(); //ファイル名を付けて保存
                    break;
                case 79 /* VKEY_KEY_O */:
                    //CTRL+O、ファイル読み込み
                    if ((sh & 12 /* CHK_CTRL */) == 0)
                        break;
                case 112 /* VKEY_F1 */: //F1キー
                    //F1キー、ファイル読み込み
                    //this.selectfile();	//ファイルを選択して読み込み
                    break;
                case 78 /* VKEY_KEY_N */:
                    //CTRL+N、新規作成
                    if ((sh & 12 /* CHK_CTRL */) == 0)
                        break;
                case 115 /* VKEY_F4 */: //F4キー
                    //this.newtext(); //新規作成
                    break;
            }
            this.discard_saved_cursor();
        }
    }
    window
        .whenEvent('load')
        .then(() => BMPFont.loadFont("font.bmpf"))
        .then((bmpFont) => {
        const root = document.getElementById("editor");
        const boundingRect = root.getBoundingClientRect();
        var vkeyboard = new VirtualKeyboard(root, keypad_layout);
        const canvas = document.createElement("canvas");
        root.appendChild(canvas);
        canvas.width = ~~boundingRect.width;
        canvas.height = ~~boundingRect.height - vkeyboard.height;
        canvas.style.position = "absolute";
        canvas.style.left = "0px";
        canvas.style.top = "0px";
        canvas.style.width = canvas.width + "px";
        canvas.style.height = canvas.height + "px";
        const context = canvas.getContext("2d");
        const imageData = context.createImageData(~~canvas.width, ~~canvas.height);
        imageData.data32 = new Uint32Array(imageData.data.buffer);
        const textVram = new TextVRAM(bmpFont, ~~(canvas.width / bmpFont.FontWidth), ~~(canvas.height / bmpFont.FontHeight));
        textVram.SetPaletteColor(COLOR_BOTTOMLINE_BG, 0x00, 0x20, 0x80);
        const textEditor = new TextEditor(bmpFont, textVram);
        const keyboard = new Keyboard();
        //*
        textEditor.load((s) => { s.str = Unicode.strToUtf32(document.getElementById('demo').innerHTML); return true; }, (s) => s.str.length > s.index ? s.str[s.index++] : 0, (s) => true, { str: null, index: 0 });
        //*/
        window.addEventListener('keydown', (e) => {
            if (e.repeat == false) {
                keyboard.pushKeyStatus(e.keyCode, false);
            }
            e.stopPropagation();
            e.preventDefault();
        });
        vkeyboard.addEventListener('keydown', (caption, keycode, repeat) => {
            if (repeat == false) {
                keyboard.pushKeyStatus(keycode, false);
            }
        });
        window.addEventListener('keyup', (e) => {
            keyboard.pushKeyStatus(e.keyCode, true);
            e.stopPropagation();
            e.preventDefault();
        });
        vkeyboard.addEventListener('keyup', (caption, keycode, repeat) => {
            if (repeat == false) {
                keyboard.pushKeyStatus(keycode, true);
            }
        });
        function loop() {
            textEditor.redraw();
            textEditor.displaybottomline();
            textVram.SetCursorPosition(textEditor.GetCursorScreenPosX(), textEditor.GetCursorScreenPosY());
            textVram.SetTextColor(COLOR_NORMALTEXT);
            textVram.SetBackgroundColor(COLOR_NORMALTEXT_BG);
            while (keyboard.readKey() && keyboard.GetCurrentVKeyCode()) {
                let k1 = keyboard.GetCurrentAsciiCode();
                const k2 = keyboard.GetCurrentVKeyCode();
                const sh = keyboard.GetCurrentCtrlKeys(); //sh:シフト関連キー状態
                //Enter押下は単純に改行文字を入力とする
                if (k2 == 13 /* VKEY_RETURN */ || k2 == 108 /* VKEY_SEPARATOR */) {
                    k1 = 0x0A;
                }
                if (k1 != 0) {
                    //通常文字が入力された場合
                    textEditor.normal_code_process(k1);
                }
                else {
                    //制御文字が入力された場合
                    textEditor.control_code_process(k2, sh);
                }
                textEditor.checkResetSelectArea();
            }
            {
                const palPtr = textVram.GetPalettePtr();
                const vramPtr = textVram.GetVramPtr();
                const cursorPos = textVram.GetCursorPosition();
                imageData.data32.fill(0xFF000000);
                let idx = 0;
                for (let y = 0; y < textVram.Height; y++) {
                    for (let x = 0; x < textVram.Width; x++) {
                        const ch = vramPtr[idx + 0];
                        const color = palPtr[(vramPtr[idx + 1] >> 0) & 0x0F];
                        const bgColor = palPtr[(vramPtr[idx + 1] >> 4) & 0x0F];
                        const fontWidth = bmpFont.getPixelWidth(ch);
                        const fontHeight = bmpFont.FontHeight;
                        const fontPixel = bmpFont.getPixel(ch);
                        const left = x * bmpFont.FontWidth;
                        const top = y * bmpFont.FontHeight;
                        const size = bmpFont.FontHeight;
                        if (x == cursorPos.X && y == cursorPos.Y) {
                            imageData.fillRect(left, top, fontWidth != 0 ? fontWidth : bmpFont.FontWidth, fontHeight, palPtr[COLOR_CURSOR]);
                        }
                        else {
                            imageData.fillRect(left, top, fontWidth, fontHeight, bgColor);
                        }
                        if (fontPixel !== undefined) {
                            for (let j = 0; j < size; j++) {
                                for (let i = 0; i < size; i++) {
                                    if (fontPixel.getBit(j * size + i)) {
                                        imageData.setPixel(left + i, top + j, color);
                                    }
                                }
                            }
                        }
                        idx += 2;
                    }
                }
                context.putImageData(imageData, 0, 0);
            }
            //
            vkeyboard.render(bmpFont);
            window.requestAnimationFrame(loop);
        }
        window.requestAnimationFrame(loop);
        //////////
    });
    class VirtualKeyboard {
        addEventListener(event, handler) {
            if (!this.events.has(event)) {
                this.events.set(event, []);
            }
            this.events.get(event).push(handler);
        }
        fire(event, str, keycode, repeat) {
            if (!this.events.has(event)) {
                return;
            }
            this.events.get(event).forEach((ev) => ev(str, keycode, repeat));
        }
        resize() {
            const boundingRect = this.root.getBoundingClientRect();
            const cell_w = boundingRect.width / this.total_width - 2;
            const cell_h = boundingRect.height / this.total_height - 2;
            const cell_size = Math.min(cell_w, cell_h);
            this.width = boundingRect.width;
            this.height = this.total_height * cell_size;
            this.left = boundingRect.left;
            this.top = boundingRect.top + boundingRect.height - this.height;
            this.canvas.width = this.width;
            this.canvas.height = this.height;
            this.canvas.style.position = "absolute";
            this.canvas.style.left = "0px";
            this.canvas.style.top = (boundingRect.height - this.height) + "px";
        }
        constructor(root, layout) {
            const widths = layout.map((block) => Math.max(...block.map(line => line.buttons.reduce((s, x) => s + x.width, 0))));
            this.events = new Map();
            this.total_width = widths.reduce((s, x) => s + x, 0);
            this.total_height = Math.max(...layout.map((block) => block.reduce((s, line) => s + line.height, 0)));
            this.buttons = [];
            this.root = root;
            this.canvas = document.createElement("canvas");
            this.context = this.canvas.getContext("2d");
            this.root.appendChild(this.canvas);
            this.toggleShift = false;
            this.changed = true;
            this.pointers = new Map();
            this.resize();
            this.imageData = this.context.createImageData(this.width, this.height);
            this.imageData.data32 = new Uint32Array(this.imageData.data.buffer);
            let left_base = 0;
            for (let i = 0; i < layout.length; i++) {
                const block = layout[i];
                const block_width_pixel = this.width * (widths[i]) / this.total_width;
                const block_height = block.reduce((s, x) => s + x.height, 0);
                let top_step = 0;
                block.forEach((line, y) => {
                    const top = this.height * top_step / block_height;
                    const height = this.height * line.height / block_height;
                    const line_width = line.buttons.reduce((s, x) => s + x.width, 0);
                    let left_step = left_base;
                    line.buttons.forEach((button, x) => {
                        const left = block_width_pixel * left_step / line_width;
                        const width = block_width_pixel * button.width / line_width;
                        button.rect = { left: Math.trunc(left) + 1, top: Math.trunc(top) + 1, width: Math.ceil(width - 2), height: Math.ceil(height - 2) };
                        button.index = this.buttons.length;
                        this.buttons.push(button);
                        left_step += button.width;
                    });
                    top_step += line.height;
                });
                left_base += widths[i];
            }
            this.hitAreaMap = new Uint8Array(this.width * this.height);
            this.hitAreaMap.fill(0xFF);
            for (let button of this.buttons) {
                const buttonWidth = button.rect.width;
                if (button.keycode != 0 /* VKEY_NONE */) {
                    let pos = button.rect.left + button.rect.top * this.width;
                    for (let y = 0; y < button.rect.height; y++) {
                        this.hitAreaMap.fill(button.index, pos, pos + buttonWidth);
                        pos += this.width;
                    }
                }
            }
            this.canvas.addEventListener("mousedown", (e) => {
                const br = this.canvas.getBoundingClientRect();
                this.onTouchDown(e.button, e.pageX - ~~br.left, e.pageY - ~~br.top);
                e.preventDefault();
                e.stopPropagation();
            });
            this.canvas.addEventListener("mouseup", (e) => {
                const br = this.canvas.getBoundingClientRect();
                this.onTouchUp(e.button, e.pageX - ~~br.left, e.pageY - ~~br.top);
                e.preventDefault();
                e.stopPropagation();
            });
            this.canvas.addEventListener("mousemove", (e) => {
                const br = this.canvas.getBoundingClientRect();
                this.onTouchMove(e.button, e.pageX - ~~br.left, e.pageY - ~~br.top);
                e.preventDefault();
                e.stopPropagation();
            });
            this.canvas.addEventListener("touchstart", (e) => {
                const br = this.canvas.getBoundingClientRect();
                for (const t of e.touches) {
                    this.onTouchDown(t.identifier, t.pageX - ~~br.left, t.pageY - ~~br.top);
                }
                e.preventDefault();
                e.stopPropagation();
            });
            this.canvas.addEventListener("touchend", (e) => {
                const br = this.canvas.getBoundingClientRect();
                for (const t of e.touches) {
                    this.onTouchUp(t.identifier, t.pageX - ~~br.left, t.pageY - ~~br.top);
                }
                e.preventDefault();
                e.stopPropagation();
            });
            this.canvas.addEventListener("touchmove", (e) => {
                const br = this.canvas.getBoundingClientRect();
                for (const t of e.touches) {
                    this.onTouchMove(t.identifier, t.pageX - ~~br.left, t.pageY - ~~br.top);
                }
                e.preventDefault();
                e.stopPropagation();
            });
        }
        onTouchDown(finger, x, y) {
            const keyid = this.hitAreaMap[y * this.width + x];
            if (keyid == 0xFF) {
                return;
            }
            if (this.pointers.has(finger)) {
                return;
            }
            this.pointers.set(finger, keyid);
            const pushedKey = this.buttons[keyid];
            this.changed = true;
            this.toggleShift = false;
            for (let [k, v] of this.pointers) {
                if (v == 0xFF) {
                    continue;
                }
                if (this.buttons[v].keycode == 16 /* VKEY_SHIFT */ ||
                    this.buttons[v].keycode == 161 /* VKEY_RSHIFT */ ||
                    this.buttons[v].keycode == 160 /* VKEY_LSHIFT */) {
                    this.toggleShift = true;
                    this.changed = true;
                    break;
                }
            }
            this.fire("keydown", pushedKey.captions[this.toggleShift ? 1 : 0], pushedKey.keycode, false);
        }
        onTouchUp(finger, x, y) {
            const keyid = this.hitAreaMap[y * this.width + x];
            if (keyid == 0xFF) {
                return;
            }
            if (!this.pointers.has(finger)) {
                return;
            }
            this.pointers.delete(finger);
            const pushedKey = this.buttons[keyid];
            this.changed = true;
            this.toggleShift = false;
            for (let [k, v] of this.pointers) {
                if (v == 0xFF) {
                    continue;
                }
                if (this.buttons[v].keycode == 16 /* VKEY_SHIFT */ ||
                    this.buttons[v].keycode == 161 /* VKEY_RSHIFT */ ||
                    this.buttons[v].keycode == 160 /* VKEY_LSHIFT */) {
                    this.toggleShift = true;
                    this.changed = true;
                    break;
                }
            }
            this.fire("keyup", pushedKey.captions[this.toggleShift ? 1 : 0], pushedKey.keycode, false);
        }
        onTouchMove(finger, x, y) {
            if (!this.pointers.has(finger)) {
                return;
            }
            const prevKey = this.pointers.get(finger);
            const keyid = this.hitAreaMap[y * this.width + x];
            if (keyid == prevKey) {
                return;
            }
            this.pointers.set(finger, keyid);
            let upKey = null;
            let downKey = null;
            if (prevKey != 0xFF) {
                upKey = this.buttons[prevKey];
                this.changed = true;
            }
            if (keyid != 0xFF) {
                downKey = this.buttons[keyid];
                this.changed = true;
            }
            this.toggleShift = false;
            for (let [k, v] of this.pointers) {
                if (v == 0xFF) {
                    continue;
                }
                if (this.buttons[v].keycode == 16 /* VKEY_SHIFT */ ||
                    this.buttons[v].keycode == 161 /* VKEY_RSHIFT */ ||
                    this.buttons[v].keycode == 160 /* VKEY_LSHIFT */) {
                    this.toggleShift = true;
                    this.changed = true;
                    break;
                }
            }
            if (upKey) {
                this.fire("keyup", upKey.captions[this.toggleShift ? 1 : 0], upKey.keycode, false);
            }
            if (downKey) {
                this.fire("keydown", downKey.captions[this.toggleShift ? 1 : 0], downKey.keycode, false);
            }
        }
        render(bmpFont) {
            if (this.changed == false) {
                return;
            }
            const captionIndex = this.toggleShift ? 1 : 0;
            this.imageData.data32.fill(0xFFFFFFFF);
            keypad_layout.forEach((block) => {
                block.forEach((line) => {
                    line.buttons.forEach((btn) => {
                        if (btn.keycode != 0 /* VKEY_NONE */) {
                            this.imageData.drawRect(btn.rect.left, btn.rect.top, btn.rect.width, btn.rect.height, 0xFF000000);
                            const text = btn.captions[captionIndex];
                            if (text != null && text != "") {
                                const [w, h] = bmpFont.measureStr(text);
                                const offx = (btn.rect.width - w) >> 1;
                                const offy = (btn.rect.height - h) >> 1;
                                bmpFont.drawStr(btn.rect.left + offx, btn.rect.top + offy, text, (x, y) => this.imageData.setPixel(x, y, 0xFF000000));
                            }
                        }
                    });
                });
            });
            this.context.putImageData(this.imageData, 0, 0);
            this.changed = false;
        }
    }
    const keypad_layout = [
        [
            {
                "height": 2,
                "buttons": [
                    { "id": "ESC", "fontsize": 0.8, "width": 2, "captions": ["ESC", "ESC"], "keycode": 27 /* VKEY_ESCAPE */ },
                    { "id": "", "fontsize": 0.8, "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ },
                    { "id": "F1", "fontsize": 0.8, "width": 2, "captions": ["F1", "F1"], "keycode": 112 /* VKEY_F1 */ },
                    { "id": "F2", "fontsize": 0.8, "width": 2, "captions": ["F2", "F2"], "keycode": 113 /* VKEY_F2 */ },
                    { "id": "F3", "fontsize": 0.8, "width": 2, "captions": ["F3", "F3"], "keycode": 114 /* VKEY_F3 */ },
                    { "id": "F4", "fontsize": 0.8, "width": 2, "captions": ["F4", "F4"], "keycode": 115 /* VKEY_F4 */ },
                    { "id": "", "fontsize": 0.8, "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ },
                    { "id": "F5", "fontsize": 0.8, "width": 2, "captions": ["F5", "F5"], "keycode": 116 /* VKEY_F5 */ },
                    { "id": "F6", "fontsize": 0.8, "width": 2, "captions": ["F6", "F6"], "keycode": 117 /* VKEY_F6 */ },
                    { "id": "F7", "fontsize": 0.8, "width": 2, "captions": ["F7", "F7"], "keycode": 118 /* VKEY_F7 */ },
                    { "id": "F8", "fontsize": 0.8, "width": 2, "captions": ["F8", "F8"], "keycode": 119 /* VKEY_F8 */ },
                    { "id": "", "fontsize": 0.8, "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ },
                    { "id": "F9", "fontsize": 0.8, "width": 2, "captions": ["F9", "F9"], "keycode": 120 /* VKEY_F9 */ },
                    { "id": "F10", "fontsize": 0.8, "width": 2, "captions": ["F10", "F10"], "keycode": 121 /* VKEY_F10 */ },
                    { "id": "F11", "fontsize": 0.8, "width": 2, "captions": ["F11", "F11"], "keycode": 122 /* VKEY_F11 */ },
                    { "id": "F12", "fontsize": 0.8, "width": 2, "captions": ["F12", "F12"], "keycode": 123 /* VKEY_F12 */ }
                ]
            },
            {
                "height": 1,
                "buttons": [
                    { "id": "", "fontsize": 0.8, "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "`", "fontsize": 1, "width": 2, "captions": ["`", "~"], "keycode": 192 /* VKEY_OEM_3 */ },
                    { "id": "1", "fontsize": 1, "width": 2, "captions": ["1", "!"], "keycode": 49 /* VKEY_NUM1 */ },
                    { "id": "2", "fontsize": 1, "width": 2, "captions": ["2", "@"], "keycode": 50 /* VKEY_NUM2 */ },
                    { "id": "3", "fontsize": 1, "width": 2, "captions": ["3", "#"], "keycode": 51 /* VKEY_NUM3 */ },
                    { "id": "4", "fontsize": 1, "width": 2, "captions": ["4", "$"], "keycode": 52 /* VKEY_NUM4 */ },
                    { "id": "5", "fontsize": 1, "width": 2, "captions": ["5", "%"], "keycode": 53 /* VKEY_NUM5 */ },
                    { "id": "6", "fontsize": 1, "width": 2, "captions": ["6", "^"], "keycode": 54 /* VKEY_NUM6 */ },
                    { "id": "7", "fontsize": 1, "width": 2, "captions": ["7", "&"], "keycode": 55 /* VKEY_NUM7 */ },
                    { "id": "8", "fontsize": 1, "width": 2, "captions": ["8", "*"], "keycode": 56 /* VKEY_NUM8 */ },
                    { "id": "9", "fontsize": 1, "width": 2, "captions": ["9", "("], "keycode": 57 /* VKEY_NUM9 */ },
                    { "id": "0", "fontsize": 1, "width": 2, "captions": ["0", ")"], "keycode": 48 /* VKEY_NUM0 */ },
                    { "id": "-", "fontsize": 1, "width": 2, "captions": ["-", "_"], "keycode": 189 /* VKEY_OEM_MINUS */ },
                    { "id": "=", "fontsize": 1, "width": 2, "captions": ["=", "+"], "keycode": 187 /* VKEY_OEM_PLUS */ },
                    { "id": "BackSpace", "fontsize": 0.8, "width": 4, "captions": ["back", "back"], "keycode": 8 /* VKEY_BACK */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "Tab", "fontsize": 0.8, "width": 3, "captions": ["tab", "tab"], "keycode": 9 /* VKEY_TAB */ },
                    { "id": "q", "fontsize": 1, "width": 2, "captions": ["q", "Q"], "keycode": 81 /* VKEY_KEY_Q */ },
                    { "id": "w", "fontsize": 1, "width": 2, "captions": ["w", "W"], "keycode": 87 /* VKEY_KEY_W */ },
                    { "id": "e", "fontsize": 1, "width": 2, "captions": ["e", "E"], "keycode": 69 /* VKEY_KEY_E */ },
                    { "id": "r", "fontsize": 1, "width": 2, "captions": ["r", "R"], "keycode": 82 /* VKEY_KEY_R */ },
                    { "id": "t", "fontsize": 1, "width": 2, "captions": ["t", "T"], "keycode": 84 /* VKEY_KEY_T */ },
                    { "id": "y", "fontsize": 1, "width": 2, "captions": ["y", "Y"], "keycode": 89 /* VKEY_KEY_Y */ },
                    { "id": "u", "fontsize": 1, "width": 2, "captions": ["u", "U"], "keycode": 85 /* VKEY_KEY_U */ },
                    { "id": "i", "fontsize": 1, "width": 2, "captions": ["i", "I"], "keycode": 73 /* VKEY_KEY_I */ },
                    { "id": "o", "fontsize": 1, "width": 2, "captions": ["o", "O"], "keycode": 79 /* VKEY_KEY_O */ },
                    { "id": "p", "fontsize": 1, "width": 2, "captions": ["p", "P"], "keycode": 80 /* VKEY_KEY_P */ },
                    { "id": "[", "fontsize": 1, "width": 2, "captions": ["[", "{"], "keycode": 219 /* VKEY_OEM_4 */ },
                    { "id": "]", "fontsize": 1, "width": 2, "captions": ["]", "}"], "keycode": 221 /* VKEY_OEM_6 */ },
                    { "id": "\\", "fontsize": 1, "width": 3, "captions": ["\\", "|"], "keycode": 220 /* VKEY_OEM_5 */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "CapsLock", "fontsize": 0.8, "width": 4, "repeat": false, "captions": ["caps", "caps"], "keycode": 20 /* VKEY_CAPITAL */ },
                    { "id": "a", "fontsize": 1, "width": 2, "captions": ["a", "A"], "keycode": 65 /* VKEY_KEY_A */ },
                    { "id": "s", "fontsize": 1, "width": 2, "captions": ["s", "S"], "keycode": 83 /* VKEY_KEY_S */ },
                    { "id": "d", "fontsize": 1, "width": 2, "captions": ["d", "D"], "keycode": 68 /* VKEY_KEY_D */ },
                    { "id": "f", "fontsize": 1, "width": 2, "captions": ["f", "F"], "keycode": 70 /* VKEY_KEY_F */ },
                    { "id": "g", "fontsize": 1, "width": 2, "captions": ["g", "G"], "keycode": 71 /* VKEY_KEY_G */ },
                    { "id": "h", "fontsize": 1, "width": 2, "captions": ["h", "H"], "keycode": 72 /* VKEY_KEY_H */ },
                    { "id": "j", "fontsize": 1, "width": 2, "captions": ["j", "J"], "keycode": 74 /* VKEY_KEY_J */ },
                    { "id": "k", "fontsize": 1, "width": 2, "captions": ["k", "K"], "keycode": 75 /* VKEY_KEY_K */ },
                    { "id": "l", "fontsize": 1, "width": 2, "captions": ["l", "L"], "keycode": 76 /* VKEY_KEY_L */ },
                    { "id": ";", "fontsize": 1, "width": 2, "captions": [";", ":"], "keycode": 186 /* VKEY_OEM_1 */ },
                    { "id": "'", "fontsize": 1, "width": 2, "captions": ["'", "\""], "keycode": 222 /* VKEY_OEM_7 */ },
                    { "id": "Enter", "fontsize": 0.8, "width": 4, "captions": ["enter", "enter"], "keycode": 13 /* VKEY_RETURN */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "LShift", "fontsize": 0.8, "width": 5, "repeat": false, "captions": ["shift", "shift"], "keycode": 160 /* VKEY_LSHIFT */ },
                    { "id": "z", "fontsize": 1, "width": 2, "captions": ["z", "Z"], "keycode": 90 /* VKEY_KEY_Z */ },
                    { "id": "x", "fontsize": 1, "width": 2, "captions": ["x", "X"], "keycode": 88 /* VKEY_KEY_X */ },
                    { "id": "c", "fontsize": 1, "width": 2, "captions": ["c", "C"], "keycode": 67 /* VKEY_KEY_C */ },
                    { "id": "v", "fontsize": 1, "width": 2, "captions": ["v", "V"], "keycode": 86 /* VKEY_KEY_V */ },
                    { "id": "b", "fontsize": 1, "width": 2, "captions": ["b", "B"], "keycode": 66 /* VKEY_KEY_B */ },
                    { "id": "n", "fontsize": 1, "width": 2, "captions": ["n", "N"], "keycode": 78 /* VKEY_KEY_N */ },
                    { "id": "m", "fontsize": 1, "width": 2, "captions": ["m", "M"], "keycode": 77 /* VKEY_KEY_M */ },
                    { "id": ",", "fontsize": 1, "width": 2, "captions": [",", "<"], "keycode": 188 /* VKEY_OEM_COMMA */ },
                    { "id": ".", "fontsize": 1, "width": 2, "captions": [".", ">"], "keycode": 190 /* VKEY_OEM_PERIOD */ },
                    { "id": "/", "fontsize": 1, "width": 2, "captions": ["/", "?"], "keycode": 191 /* VKEY_OEM_2 */ },
                    { "id": "RShift", "fontsize": 0.8, "width": 5, "repeat": false, "captions": ["shift", "shift"], "keycode": 161 /* VKEY_RSHIFT */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "LCtrl", "fontsize": 0.8, "width": 3, "repeat": false, "captions": ["ctrl", "ctrl"], "keycode": 162 /* VKEY_LCONTROL */ },
                    { "id": "Fn", "fontsize": 0.8, "width": 2, "repeat": false, "captions": ["fn", "fn"], "keycode": 0 /* VKEY_NONE */ },
                    { "id": "LOs", "fontsize": 0.8, "width": 2, "repeat": false, "captions": ["os", "os"], "keycode": 91 /* VKEY_LWIN */ },
                    { "id": "LAlt", "fontsize": 0.8, "width": 2, "repeat": false, "captions": ["alt", "alt"], "keycode": 18 /* VKEY_MENU */ },
                    { "id": "Space", "fontsize": 0.8, "width": 12, "captions": ["space", "space"], "keycode": 32 /* VKEY_SPACE */ },
                    { "id": "RAlt", "fontsize": 0.8, "width": 3, "repeat": false, "captions": ["alt", "alt"], "keycode": 18 /* VKEY_MENU */ },
                    { "id": "ROs", "fontsize": 0.8, "width": 2, "repeat": false, "captions": ["os", "os"], "keycode": 92 /* VKEY_RWIN */ },
                    { "id": "Menu", "fontsize": 0.8, "width": 2, "repeat": false, "captions": ["menu", "menu"], "keycode": 165 /* VKEY_RMENU */ },
                    { "id": "RCtrl", "fontsize": 0.8, "width": 3, "repeat": false, "captions": ["ctrl", "ctrl"], "keycode": 163 /* VKEY_RCONTROL */ }
                ]
            }
        ], [
            {
                "height": 2,
                "buttons": [
                    { "id": "PrintScreen", "fontsize": 0.8, "width": 2, "repeat": false, "captions": ["PrtSc", "PrtSc"], "keycode": 42 /* VKEY_PRINT */ },
                    { "id": "ScrollLock", "fontsize": 0.8, "width": 2, "repeat": false, "captions": ["ScrLk", "ScrLk"], "keycode": 145 /* VKEY_SCROLL */ },
                    { "id": "Pause", "fontsize": 0.8, "width": 2, "repeat": false, "captions": ["Pause", "Pause"], "keycode": 19 /* VKEY_PAUSE */ }
                ]
            },
            {
                "height": 1,
                "buttons": [
                    { "id": "", "fontsize": 0.8, "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "Insert", "fontsize": 0.8, "width": 2, "captions": ["Ins", "Ins"], "keycode": 45 /* VKEY_INSERT */ },
                    { "id": "Home", "fontsize": 0.8, "width": 2, "captions": ["Home", "Home"], "keycode": 36 /* VKEY_HOME */ },
                    { "id": "PageUp", "fontsize": 0.8, "width": 2, "captions": ["PgUp", "PgUp"], "keycode": 33 /* VKEY_PRIOR */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "Delete", "fontsize": 0.8, "width": 2, "captions": ["Del", "Del"], "keycode": 46 /* VKEY_DELETE */ },
                    { "id": "End", "fontsize": 0.8, "width": 2, "captions": ["End", "End"], "keycode": 35 /* VKEY_END */ },
                    { "id": "PageDown", "fontsize": 0.8, "width": 2, "captions": ["PgDn", "PgDn"], "keycode": 34 /* VKEY_NEXT */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "", "fontsize": 0.8, "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "", "fontsize": 0.8, "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ },
                    { "id": "ArrowUp", "fontsize": 0.8, "width": 2, "captions": ["↑", "↑"], "keycode": 38 /* VKEY_UP */ },
                    { "id": "", "fontsize": 0.8, "width": 2, "captions": [null, null], "keycode": 0 /* VKEY_NONE */ }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "ArrowLeft", "fontsize": 0.8, "width": 2, "captions": ["←", "←"], "keycode": 37 /* VKEY_LEFT */ },
                    { "id": "ArrowDown", "fontsize": 0.8, "width": 2, "captions": ["↓", "↓"], "keycode": 40 /* VKEY_DOWN */ },
                    { "id": "ArrowRight", "fontsize": 0.8, "width": 2, "captions": ["→", "→"], "keycode": 39 /* VKEY_RIGHT */ }
                ]
            }
        ]
    ];
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
ImageData.prototype.setPixel = function (x, y, color) {
    if (x < 0) {
        return;
    }
    if (x >= this.width) {
        return;
    }
    if (y < 0) {
        return;
    }
    if (y >= this.height) {
        return;
    }
    this.data32[y * this.width + x] = color;
};
ImageData.prototype.fillRect = function (x, y, w, h, color) {
    if (x < 0) {
        w += x;
        x = 0;
    }
    if (x + w > this.width) {
        w = this.width - x;
    }
    if (w <= 0) {
        return;
    }
    if (y < 0) {
        h += y;
        y = 0;
    }
    if (y + h > this.height) {
        h = this.height - y;
    }
    if (h <= 0) {
        return;
    }
    const width = this.width;
    let start = y * width + x;
    let end = start + w;
    for (let j = 0; j < h; j++) {
        this.data32.fill(color, start, end);
        start += width;
        end += width;
    }
};
ImageData.prototype.drawRect = function (x, y, w, h, color) {
    if (x < 0) {
        w += x;
        x = 0;
    }
    if (x + w > this.width) {
        w = this.width - x;
    }
    if (w <= 0) {
        return;
    }
    if (y < 0) {
        h += y;
        y = 0;
    }
    if (y + h > this.height) {
        h = this.height - y;
    }
    if (h <= 0) {
        return;
    }
    const width = this.width;
    let start = y * width + x;
    let end = start + w;
    this.data32.fill(color, start, end);
    for (let j = 0; j < h - 1; j++) {
        start += width;
        end += width;
        this.data32[start] = color;
        this.data32[end - 1] = color;
    }
    this.data32.fill(color, start, end);
};
//# sourceMappingURL=editor.js.map