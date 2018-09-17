///// <reference path="C:/Program Files (x86)/Microsoft SDKs/TypeScript/3.0/lib.es6.d.ts" />
//// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />

//interface XMLHttpRequest {
//    responseURL: string;
//}

interface HTMLElementEventMap {
    "touchstart": TouchEvent,
    "touchmove": TouchEvent,
    "touchend": TouchEvent,
    "touchcancel": TouchEvent,
}

interface Window {
    whenEvent<T>(event: string): Promise<T>;
}

Window.prototype.whenEvent = function (event: string): Promise<any> {
    return new Promise<any>((resolve, reject) => {
        const fire = (e: Event) => {
            resolve(e);
            this.addEventListener(event, fire, false);
        }
        this.addEventListener(event, fire, false);
    });
}

module Editor {
    "use strict";

    function assert(x: boolean): void {
        if (!x) {
            throw new Error("assert failed");
        }
    }

    const enum ControlKey {
        CHK_NONE = 0x0000,
        CHK_SHIFT_L = 0x0001,
        CHK_SHIFT_R = 0x0002,
        CHK_CTRL_L = 0x0004,
        CHK_CTRL_R = 0x0008,
        CHK_ALT_L = 0x0010,
        CHK_ALT_R = 0x0020,
        CHK_WIN_L = 0x0040,
        CHK_WIN_R = 0x0080,

        CHK_SCRLK = 0x0100,
        CHK_NUMLK = 0x0200,
        CHK_CAPSLK = 0x0400,

        CHK_SHIFT = CHK_SHIFT_L | CHK_SHIFT_R,
        CHK_CTRL = CHK_CTRL_L | CHK_CTRL_R,
        CHK_ALT = CHK_ALT_L | CHK_ALT_R,
        CHK_WIN = CHK_WIN_L | CHK_WIN_R,

        CHK_MASK_SCAL = CHK_SHIFT | CHK_CTRL | CHK_ALT | CHK_WIN

    };
    const enum VirtualKey {
        VKEY_NONE = 0x00,
        VKEY_LBUTTON = 0x01,
        VKEY_RBUTTON = 0x02,
        VKEY_CANCEL = 0x03,
        VKEY_MBUTTON = 0x04,
        VKEY_XBUTTON1 = 0x05,
        VKEY_XBUTTON2 = 0x06,
        VKEY_UNDEFINED_0x07 = 0x07,
        VKEY_BACK = 0x08,
        VKEY_TAB = 0x09,
        VKEY_RESERVED_0x0A = 0x0A,
        VKEY_RESERVED_0x0B = 0x0B,
        VKEY_CLEAR = 0x0C,
        VKEY_RETURN = 0x0D,
        VKEY_UNDEFINED_0x0E = 0x0E,
        VKEY_UNDEFINED_0x0F = 0x0F,
        VKEY_SHIFT = 0x10,
        VKEY_CONTROL = 0x11,
        VKEY_MENU = 0x12,
        VKEY_PAUSE = 0x13,
        VKEY_CAPITAL = 0x14,
        VKEY_KANA = 0x15,
        VKEY_HANGUEL = 0x15,
        VKEY_HANGUL = 0x15,
        VKEY_UNDEFINED_0x16 = 0x16,
        VKEY_JUNJA = 0x17,
        VKEY_FINAL = 0x18,
        VKEY_HANJA = 0x19,
        VKEY_KANJI = 0x19,
        VKEY_UNDEFINED_0x1A = 0x1A,
        VKEY_ESCAPE = 0x1B,
        VKEY_CONVERT = 0x1C,
        VKEY_NONCONVERT = 0x1D,
        VKEY_ACCEPT = 0x1E,
        VKEY_MODECHANGE = 0x1F,
        VKEY_SPACE = 0x20,
        VKEY_PRIOR = 0x21,
        VKEY_NEXT = 0x22,
        VKEY_END = 0x23,
        VKEY_HOME = 0x24,
        VKEY_LEFT = 0x25,
        VKEY_UP = 0x26,
        VKEY_RIGHT = 0x27,
        VKEY_DOWN = 0x28,
        VKEY_SELECT = 0x29,
        VKEY_PRINT = 0x2A,
        VKEY_EXECUTE = 0x2B,
        VKEY_SNAPSHOT = 0x2C,
        VKEY_INSERT = 0x2D,
        VKEY_DELETE = 0x2E,
        VKEY_HELP = 0x2F,
        VKEY_NUM0 = 0x30,
        VKEY_NUM1 = 0x31,
        VKEY_NUM2 = 0x32,
        VKEY_NUM3 = 0x33,
        VKEY_NUM4 = 0x34,
        VKEY_NUM5 = 0x35,
        VKEY_NUM6 = 0x36,
        VKEY_NUM7 = 0x37,
        VKEY_NUM8 = 0x38,
        VKEY_NUM9 = 0x39,
        VKEY_UNDEFINED_0x3A = 0x3A,
        VKEY_UNDEFINED_0x3B = 0x3B,
        VKEY_UNDEFINED_0x3C = 0x3C,
        VKEY_UNDEFINED_0x3D = 0x3D,
        VKEY_UNDEFINED_0x3E = 0x3E,
        VKEY_UNDEFINED_0x3F = 0x3F,
        VKEY_UNDEFINED_0x40 = 0x40,
        VKEY_KEY_A = 0x41,
        VKEY_KEY_B = 0x42,
        VKEY_KEY_C = 0x43,
        VKEY_KEY_D = 0x44,
        VKEY_KEY_E = 0x45,
        VKEY_KEY_F = 0x46,
        VKEY_KEY_G = 0x47,
        VKEY_KEY_H = 0x48,
        VKEY_KEY_I = 0x49,
        VKEY_KEY_J = 0x4A,
        VKEY_KEY_K = 0x4B,
        VKEY_KEY_L = 0x4C,
        VKEY_KEY_M = 0x4D,
        VKEY_KEY_N = 0x4E,
        VKEY_KEY_O = 0x4F,
        VKEY_KEY_P = 0x50,
        VKEY_KEY_Q = 0x51,
        VKEY_KEY_R = 0x52,
        VKEY_KEY_S = 0x53,
        VKEY_KEY_T = 0x54,
        VKEY_KEY_U = 0x55,
        VKEY_KEY_V = 0x56,
        VKEY_KEY_W = 0x57,
        VKEY_KEY_X = 0x58,
        VKEY_KEY_Y = 0x59,
        VKEY_KEY_Z = 0x5A,
        VKEY_LWIN = 0x5B,
        VKEY_RWIN = 0x5C,
        VKEY_APPS = 0x5D,
        VKEY_RESERVED_0x5E = 0x5E,
        VKEY_SLEEP = 0x5F,
        VKEY_NUMPAD0 = 0x60,
        VKEY_NUMPAD1 = 0x61,
        VKEY_NUMPAD2 = 0x62,
        VKEY_NUMPAD3 = 0x63,
        VKEY_NUMPAD4 = 0x64,
        VKEY_NUMPAD5 = 0x65,
        VKEY_NUMPAD6 = 0x66,
        VKEY_NUMPAD7 = 0x67,
        VKEY_NUMPAD8 = 0x68,
        VKEY_NUMPAD9 = 0x69,
        VKEY_MULTIPLY = 0x6A,
        VKEY_ADD = 0x6B,
        VKEY_SEPARATOR = 0x6C,
        VKEY_SUBTRACT = 0x6D,
        VKEY_DECIMAL = 0x6E,
        VKEY_DIVIDE = 0x6F,
        VKEY_F1 = 0x70,
        VKEY_F2 = 0x71,
        VKEY_F3 = 0x72,
        VKEY_F4 = 0x73,
        VKEY_F5 = 0x74,
        VKEY_F6 = 0x75,
        VKEY_F7 = 0x76,
        VKEY_F8 = 0x77,
        VKEY_F9 = 0x78,
        VKEY_F10 = 0x79,
        VKEY_F11 = 0x7A,
        VKEY_F12 = 0x7B,
        VKEY_F13 = 0x7C,
        VKEY_F14 = 0x7D,
        VKEY_F15 = 0x7E,
        VKEY_F16 = 0x7F,
        VKEY_F17 = 0x80,
        VKEY_F18 = 0x81,
        VKEY_F19 = 0x82,
        VKEY_F20 = 0x83,
        VKEY_F21 = 0x84,
        VKEY_F22 = 0x85,
        VKEY_F23 = 0x86,
        VKEY_F24 = 0x87,
        VKEY_UNASSIGNED_0x88 = 0x88,
        VKEY_UNASSIGNED_0x89 = 0x89,
        VKEY_UNASSIGNED_0x8A = 0x8A,
        VKEY_UNASSIGNED_0x8B = 0x8B,
        VKEY_UNASSIGNED_0x8C = 0x8C,
        VKEY_UNASSIGNED_0x8D = 0x8D,
        VKEY_UNASSIGNED_0x8E = 0x8E,
        VKEY_UNASSIGNED_0x8F = 0x8F,
        VKEY_NUMLOCK = 0x90,
        VKEY_SCROLL = 0x91,
        VKEY_LSHIFT = 0xA0,
        VKEY_RSHIFT = 0xA1,
        VKEY_LCONTROL = 0xA2,
        VKEY_RCONTROL = 0xA3,
        VKEY_LMENU = 0xA4,
        VKEY_RMENU = 0xA5,
        VKEY_BROWSER_BACK = 0xA6,
        VKEY_BROWSER_FORWARD = 0xA7,
        VKEY_BROWSER_REFRESH = 0xA8,
        VKEY_BROWSER_STOP = 0xA9,
        VKEY_BROWSER_SEARCH = 0xAA,
        VKEY_BROWSER_FAVORITES = 0xAB,
        VKEY_BROWSER_HOME = 0xAC,
        VKEY_VOLUME_MUTE = 0xAD,
        VKEY_VOLUME_DOWN = 0xAE,
        VKEY_VOLUME_UP = 0xAF,
        VKEY_MEDIA_NEXT_TRACK = 0xB0,
        VKEY_MEDIA_PREV_TRACK = 0xB1,
        VKEY_MEDIA_STOP = 0xB2,
        VKEY_MEDIA_PLAY_PAUSE = 0xB3,
        VKEY_LAUNCH_MAIL = 0xB4,
        VKEY_LAUNCH_MEDIA_SELECT = 0xB5,
        VKEY_LAUNCH_APP1 = 0xB6,
        VKEY_LAUNCH_APP2 = 0xB7,
        VKEY_RESERVED_0xB8 = 0xB8,
        VKEY_RESERVED_0xB9 = 0xB9,
        VKEY_OEM_1 = 0xBA,
        VKEY_OEM_PLUS = 0xBB,
        VKEY_OEM_COMMA = 0xBC,
        VKEY_OEM_MINUS = 0xBD,
        VKEY_OEM_PERIOD = 0xBE,
        VKEY_OEM_2 = 0xBF,
        VKEY_OEM_3 = 0xC0,
        VKEY_RESERVED_0xC1 = 0xC1,
        VKEY_RESERVED_0xC2 = 0xC2,
        VKEY_RESERVED_0xC3 = 0xC3,
        VKEY_RESERVED_0xC4 = 0xC4,
        VKEY_RESERVED_0xC5 = 0xC5,
        VKEY_RESERVED_0xC6 = 0xC6,
        VKEY_RESERVED_0xC7 = 0xC7,
        VKEY_RESERVED_0xC8 = 0xC8,
        VKEY_RESERVED_0xC9 = 0xC9,
        VKEY_RESERVED_0xCA = 0xCA,
        VKEY_RESERVED_0xCB = 0xCB,
        VKEY_RESERVED_0xCC = 0xCC,
        VKEY_RESERVED_0xCD = 0xCD,
        VKEY_RESERVED_0xCE = 0xCE,
        VKEY_RESERVED_0xCF = 0xCF,
        VKEY_RESERVED_0xD0 = 0xD0,
        VKEY_RESERVED_0xD1 = 0xD1,
        VKEY_RESERVED_0xD2 = 0xD2,
        VKEY_RESERVED_0xD3 = 0xD3,
        VKEY_RESERVED_0xD4 = 0xD4,
        VKEY_RESERVED_0xD5 = 0xD5,
        VKEY_RESERVED_0xD6 = 0xD6,
        VKEY_RESERVED_0xD7 = 0xD7,
        VKEY_UNASSIGNED_0xD8 = 0xD8,
        VKEY_UNASSIGNED_0xD9 = 0xD9,
        VKEY_UNASSIGNED_0xDA = 0xDA,
        VKEY_OEM_4 = 0xDB,
        VKEY_OEM_5 = 0xDC,
        VKEY_OEM_6 = 0xDD,
        VKEY_OEM_7 = 0xDE,
        VKEY_OEM_8 = 0xDF,
        VKEY_RESERVED_0xE0 = 0xE0,
        VKEY_OEM_SPECIFIC_0xE1 = 0xE1,
        VKEY_OEM_102 = 0xE2,
        VKEY_OEM_SPECIFIC_0xE3 = 0xE3,
        VKEY_OEM_SPECIFIC_0xE4 = 0xE4,
        VKEY_PROCESSKEY = 0xE5,
        VKEY_OEM_SPECIFIC_0xE6 = 0xE6,
        VKEY_PACKE = 0xE7,
        VKEY_UNASSIGNED_0xE8 = 0xE8,
        VKEY_OEM_SPECIFIC_0xE9 = 0xE9,
        VKEY_OEM_SPECIFIC_0xEA = 0xEA,
        VKEY_OEM_SPECIFIC_0xEB = 0xEB,
        VKEY_OEM_SPECIFIC_0xEC = 0xEC,
        VKEY_OEM_SPECIFIC_0xED = 0xED,
        VKEY_OEM_SPECIFIC_0xEE = 0xEE,
        VKEY_OEM_SPECIFIC_0xEF = 0xEF,
        VKEY_OEM_SPECIFIC_0xF0 = 0xF0,
        VKEY_OEM_SPECIFIC_0xF1 = 0xF1,
        VKEY_OEM_SPECIFIC_0xF2 = 0xF2,
        VKEY_OEM_SPECIFIC_0xF3 = 0xF3,
        VKEY_OEM_SPECIFIC_0xF4 = 0xF4,
        VKEY_OEM_SPECIFIC_0xF5 = 0xF5,
        VKEY_ATTN = 0xF6,
        VKEY_CRSEL = 0xF7,
        VKEY_EXSEL = 0xF8,
        VKEY_EREOF = 0xF9,
        VKEY_PLAY = 0xFA,
        VKEY_ZOOM = 0xFB,
        VKEY_NONAME = 0xFC,
        VKEY_PA1 = 0xFD,
        VKEY_OEM_CLEAR = 0xFE,
    };

    class Keyboard {
        private static keycodeBufferSize: number = 16;

        // 入力バッファ（リングバッファ）
        private inputBuffer: {
            ctrlKey: ControlKey;
            vKeycode: VirtualKey;
        }[];

        //入力バッファの書き込み先頭ポインタ
        private inputBufferWriteIndex: number = 0;
        //入力バッファの読み出し先頭ポインタ
        private inputBufferReadIndex: number = 0;
        // /シフト、コントロールキー等の状態
        private ctrlKeyStatus: ControlKey = ControlKey.CHK_NONE;
        // 仮想キーコードに対応するキーの状態（Onの時1）
        private virtualKeyStatus: Uint8Array = new Uint8Array(256 / 8);

        // readで読み取ったキーの情報
        private currentCtrlKeys: ControlKey = ControlKey.CHK_NONE;
        private currentVKeyCode: VirtualKey = 0;
        private currentAsciiCode: number = 0;

        static vk2asc1: Uint8Array = new Uint8Array([
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
        ].map(x => (typeof (x) === "string") ? (x as string).codePointAt(0) : (x as number)));

        static vk2asc2: Uint8Array = new Uint8Array([
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
        ].map(x => (typeof (x) === "string") ? (x as string).codePointAt(0) : (x as number)));

        private updateCtrlKeyState(vk: VirtualKey, isDown: boolean): void {
            // SHIFT,ALT,CTRL,Winキーの押下状態を更新
            let k: ControlKey = 0;
            switch (vk) {
                case VirtualKey.VKEY_SHIFT:
                case VirtualKey.VKEY_LSHIFT:
                    k = ControlKey.CHK_SHIFT_L;
                    break;
                case VirtualKey.VKEY_RSHIFT:
                    k = ControlKey.CHK_SHIFT_R;
                    break;
                case VirtualKey.VKEY_CONTROL:
                case VirtualKey.VKEY_LCONTROL:
                    k = ControlKey.CHK_CTRL_L;
                    break;
                case VirtualKey.VKEY_RCONTROL:
                    k = ControlKey.CHK_CTRL_R;
                    break;
                case VirtualKey.VKEY_MENU:
                case VirtualKey.VKEY_LMENU:
                    k = ControlKey.CHK_ALT_L;
                    break;
                case VirtualKey.VKEY_RMENU:
                    k = ControlKey.CHK_ALT_R;
                    break;
                case VirtualKey.VKEY_LWIN:
                    k = ControlKey.CHK_WIN_L;
                    break;
                case VirtualKey.VKEY_RWIN:
                    k = ControlKey.CHK_WIN_R;
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
        private updateLockKeyState(vk: VirtualKey): void {
            switch (vk) {
                case VirtualKey.VKEY_SCROLL:
                    this.ctrlKeyStatus ^= ControlKey.CHK_SCRLK;
                    break;
                case VirtualKey.VKEY_NUMLOCK:
                    this.ctrlKeyStatus ^= ControlKey.CHK_NUMLK;
                    break;
                case VirtualKey.VKEY_CAPITAL:
                    if ((this.ctrlKeyStatus & ControlKey.CHK_SHIFT) === 0) return;
                    this.ctrlKeyStatus ^= ControlKey.CHK_CAPSLK;
                    break;
                default:
                    return;
            }
        }
        // vkが SHIFT,ALT,WIN,CTRLか判定
        private isSpecialKey(vk: VirtualKey): boolean {
            switch (vk) {
                case VirtualKey.VKEY_SHIFT:
                case VirtualKey.VKEY_LSHIFT:
                case VirtualKey.VKEY_RSHIFT:
                case VirtualKey.VKEY_CONTROL:
                case VirtualKey.VKEY_LCONTROL:
                case VirtualKey.VKEY_RCONTROL:
                case VirtualKey.VKEY_MENU:
                case VirtualKey.VKEY_LMENU:
                case VirtualKey.VKEY_RMENU:
                case VirtualKey.VKEY_LWIN:
                case VirtualKey.VKEY_RWIN:
                    return true;
                default:
                    return false;
            }
        }

        // vkがNumLock,SCRLock,CapsLockか判定
        private isLockKey(vk: VirtualKey): boolean {
            switch (vk) {
                case VirtualKey.VKEY_SCROLL:
                case VirtualKey.VKEY_NUMLOCK:
                case VirtualKey.VKEY_CAPITAL:
                    return true;
                default:
                    return false;
            }
        }

        pushKeyStatus(vk: VirtualKey, isDown: boolean) {
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

        constructor() {
            // キーボードシステム初期化
            this.inputBufferWriteIndex = 0;
            this.inputBufferReadIndex = 0;
            this.ctrlKeyStatus = ControlKey.CHK_NUMLK; // NumLock 初期状態はONとする
            this.inputBuffer = [];
            for (let i = 0; i < Keyboard.keycodeBufferSize; i++) {
                this.inputBuffer[i] = { ctrlKey: ControlKey.CHK_NONE, vKeycode: 0 };
            }

            //全キー離した状態
            this.virtualKeyStatus.fill(0);

        }

        // キーバッファからキーを一つ読み取る
        // 押されていなければfalseを返す
        readKey(): boolean {

            this.currentAsciiCode = 0x00;
            this.currentVKeyCode = 0x0000;
            this.currentCtrlKeys = ControlKey.CHK_NONE;

            if (this.inputBufferWriteIndex === this.inputBufferReadIndex) {
                return false;
            }
            const k = this.inputBuffer[this.inputBufferReadIndex++];
            this.currentVKeyCode = k.vKeycode;
            this.currentCtrlKeys = k.ctrlKey;

            if (this.inputBufferReadIndex === Keyboard.keycodeBufferSize) {
                this.inputBufferReadIndex = 0;
            }

            if (k.ctrlKey & (ControlKey.CHK_CTRL | ControlKey.CHK_ALT | ControlKey.CHK_WIN)) {
                return true;
            }

            let k2: number;
            if (k.vKeycode >= VirtualKey.VKEY_KEY_A && k.vKeycode <= VirtualKey.VKEY_KEY_Z) {
                if (((k.ctrlKey & ControlKey.CHK_SHIFT) !== 0) !== ((k.ctrlKey & ControlKey.CHK_CAPSLK) !== 0)) {
                    //SHIFTまたはCapsLock（両方ではない）
                    k2 = Keyboard.vk2asc2[k.vKeycode];
                }
                else {
                    k2 = Keyboard.vk2asc1[k.vKeycode];
                }
            }
            else if (k.vKeycode >= VirtualKey.VKEY_NUMPAD0 && k.vKeycode <= VirtualKey.VKEY_DIVIDE) {
                //テンキー関連
                if ((k.ctrlKey & (ControlKey.CHK_SHIFT | ControlKey.CHK_NUMLK)) === ControlKey.CHK_NUMLK) {
                    //NumLock（SHIFT＋NumLockは無効）
                    k2 = Keyboard.vk2asc2[k.vKeycode];
                }
                else {
                    k2 = Keyboard.vk2asc1[k.vKeycode];
                }
            }
            else {
                if (k.ctrlKey & ControlKey.CHK_SHIFT) {
                    k2 = Keyboard.vk2asc2[k.vKeycode];
                }
                else {
                    k2 = Keyboard.vk2asc1[k.vKeycode];
                }
            }
            this.currentAsciiCode = k2;
            return true;
        }

        getCurrentCtrlKeys(): ControlKey {
            return this.currentCtrlKeys;
        }

        getCurrentVKeyCode(): VirtualKey {
            return this.currentVKeyCode;
        }

        getCurrentAsciiCode(): number {
            return this.currentAsciiCode;
        }
    }

    module Unicode {

        export type Char32 = number;
        export type Char16 = number;
        export type Utf16Str = Uint16Array;
        export type Utf32Str = Uint32Array;

        function isHighSurrogate(x: Char16): boolean {
            return 0xD800 <= x && x <= 0xDBFF;
        }

        function isLoSurrogate(x: Char16): boolean {
            return 0xDC00 <= x && x <= 0xDFFF;
        }

        function isSurrogatePair(high: Char16, low: Char16): boolean {
            return isHighSurrogate(high) && isLoSurrogate(low);
        }

        function utf16ToUtf32Char(high: Char16, low: Char16): [number, Char32] {
            if (0x0000 <= high && high <= 0xD7FF) {
                return [1, high];
            } else if (0xE000 <= high && high <= 0xFFFF) {
                return [1, high];
            } else if (0xD800 <= high && high <= 0xDBFF && 0xDC00 <= low && low <= 0xDFFF) {
                return [2, ((((high >> 6) & 0x0F) + 1) << 16) | ((high & 0x3F) << 10) | (low & 0x3FF)];
            } else {
                return undefined;
            }
        }

        function utf32ToUtf16Char(code: Char32): [Char16, Char16] {
            if (0x0000 <= code && code <= 0xD7FF) {
                return [code, undefined];
            } else if (0xD800 <= code && code <= 0xDFFF) {
                return undefined;
            } else if (0xE000 <= code && code <= 0xFFFF) {
                return [code, undefined];
            } else if (0x10000 <= code && code <= 0x10FFFF) {
                const cp = code - 0x10000;
                const high = 0xD800 | (cp >> 10);
                const low = 0xDC00 | (cp & 0x3FF);
                return [high, low];
            } else {
                return undefined;
            }
        }

        function stringToUtf16(str: string): Utf16Str {
            const utf16: number[] = [];
            for (let i = 0; i < str.length; i++) {
                utf16.push(str.charCodeAt(i));
            }
            return new Uint16Array(utf16);
        }

        export function utf16ToUtf32(utf16: Utf16Str): Utf32Str {
            const utf32: number[] = [];
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

        export function utf32ToUtf16(utf32: Utf32Str): Utf16Str {
            const utf16: number[] = [];
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

        export function utf16ToString(utf16: Utf16Str): string {
            return String.fromCharCode.apply(null, utf16);
        }

        export function strToUtf32(str: string): Utf32Str {
            return utf16ToUtf32(stringToUtf16(str));
        }

        export function utf32ToStr(utf32: Utf32Str): string {
            return utf16ToString(utf32ToUtf16(utf32));
        }
    }

    class GapBuffer {
        private _gapStart: number;
        private _gapEnd: number;
        private _gapSize: number;
        private _buffer: Uint32Array;

        constructor(gapSize?: number) {
            this._gapSize = gapSize || 64;
            if (this._gapSize <= 0) {
                throw new RangeError("gapSize must be > 0");
            }

            this._buffer = new Uint32Array(this._gapSize);
            this._gapStart = 0;
            this._gapEnd = this._gapSize;
        }
        dispose(): void {
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

        get(ix: number): number {
            if (ix >= this.length) {
                return undefined;
            }
            if (ix >= this._gapStart) {
                ix += (this._gapEnd - this._gapStart);
            }
            return this._buffer[ix];
        }
        set(ix: number, value: number): void {
            if (ix >= this.length) {
                return;
            }
            if (ix >= this._gapStart) {
                ix += (this._gapEnd - this._gapStart);
            }
            this._buffer[ix] = value;
        }
        private grow(newSize: number): void {
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
        insert(ix: number, value: number): void {
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
        insertMany(ix: number, values: number[] | Uint32Array): void {
            // TODO: this should be optimised
            for (let i = 0; i < values.length; ++i) {
                this.insert(ix + i, values[i]);
            }
        }
        deleteAfter(ix: number, len: number): boolean {
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
        deleteBefore(ix: number, len: number): boolean {
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
        clear(): void {
            this._gapStart = 0;
            this._gapEnd = this._buffer.length;
        }
        asArray(): Uint32Array {
            const newBuffer = new Uint32Array(this.length);
            let n = 0;
            for (let i = 0; i < this._gapStart; i++ , n++) {
                newBuffer[n] = this._buffer[i];
            }
            for (let i = this._gapEnd; i < this._buffer.length; i++ , n++) {
                newBuffer[n] = this._buffer[i];
            }
            return newBuffer;
        }
        private moveGap(ix: number): void {
            if (ix < this._gapStart) {
                const delta = this._gapStart - ix;
                this._buffer.copyWithin(this._gapEnd - delta, ix, this._gapStart);
                this._gapStart -= delta;
                this._gapEnd -= delta;
            } else if (ix > this._gapStart) {
                const delta = ix - this._gapStart;
                this._buffer.copyWithin(this._gapStart, this._gapEnd, this._gapEnd + delta);
                this._gapStart += delta;
                this._gapEnd += delta;
            }
        }

        append(that: GapBuffer): void {
            this.moveGap(this.length);
            this.grow(this.length + that.length);
            for (let i = 0; i < that.length; ++i) {
                this._buffer[this._gapStart++] = that.get(i);
            }
        }

        reduce<U>(callback: (previousValue: U, currentValue: number, currentIndex: number, obj: GapBuffer) => U, initialValue: U): U {
            let currentIndex: number = 0;
            let previousValue = initialValue;
            for (const currentValue of this) {
                previousValue = callback(previousValue, currentValue, currentIndex++, this);
            }
            return previousValue;
        }

        find(predicate: (value: number, index: number, obj: GapBuffer) => boolean): number {
            let index = 0;
            for (const value of this) {
                if (predicate(value, index, this)) { return value; }
            }
            return undefined;
        }
    }

    class DataViewIterator {
        private littleEndian: boolean;
        private dataView: DataView;
        private byteOffset: number;
        constructor(buffer: ArrayBuffer, littleEndian: boolean = false) {
            this.littleEndian = littleEndian;
            this.dataView = new DataView(buffer);
            this.byteOffset = 0;
        }
        dispose(): void {
            this.littleEndian = false;
            this.dataView = null;
            this.byteOffset = 0;
        }
        getUint32(): number {
            const ret = this.dataView.getUint32(this.byteOffset, this.littleEndian);
            this.byteOffset += 4;
            return ret;
        }
        getUint8(): number {
            const ret = this.dataView.getUint8(this.byteOffset);
            this.byteOffset += 1;
            return ret;
        }
        getBytes(len: number): Uint8Array {
            const ret = new Uint8Array(this.dataView.buffer, this.byteOffset, len);
            this.byteOffset += len;
            return ret;
        }
    }

    interface IRange {
        rangeStart: number;
        rangeEnd: number;
    }

    class BitmapFont {
        private _fontWidth: number;
        private _fontHeight: number;
        private _pixelSize: number;
        private _widthTable: (IRange & { width: number; })[];
        private _pixelTable: (IRange & { pixels: Uint8Array[]; })[];

        get width(): number { return this._fontWidth; }
        get height(): number { return this._fontHeight; }

        constructor(buffer: ArrayBuffer) {
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
                const pixels: Uint8Array[] = [];
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

        static loadFont(url: string, progress: (loaded:number, total:number) => void = undefined): Promise<BitmapFont> {
            return new Promise<ArrayBuffer>((resolve, reject) => {
                const xhr = new XMLHttpRequest();
                xhr.open('GET', url, true);
                xhr.onload = () => {
                    if (xhr.readyState === 4 && ((xhr.status === 200) || (xhr.responseURL.startsWith("file://") && xhr.status === 0))) {
                        resolve(xhr.response);
                    } else {
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

        private static search<T>(table: Array<T & IRange>, codePoint: number): T & IRange {
            let start = 0;
            let end = table.length - 1;
            while (start <= end) {
                const mid: number = ((end - start) >> 1) + start;
                if (table[mid].rangeStart > codePoint) {
                    end = mid - 1;
                } else if (table[mid].rangeEnd < codePoint) {
                    start = mid + 1;
                } else {
                    return table[mid];
                }
            }
            return undefined;
        }
        getWidth(codePoint: number): number {
            if (0x00 === codePoint) {
                return 0;
            }
            if (0x01 <= codePoint && codePoint <= 0x1F) {
                return 1; // control code
            }
            const ret = BitmapFont.search(this._widthTable, codePoint);
            if (ret === undefined || ret === null) { return undefined; }
            return ret.width;
        }
        getPixelWidth(codePoint: number, defaultWidth?: number): number {
            const ret = this.getWidth(codePoint);
            if (ret === undefined || ret === null) { return defaultWidth; }
            return ret * this._fontWidth;
        }
        getPixel(codePoint: number): Uint8Array {
            const ret = BitmapFont.search(this._pixelTable, codePoint);
            if (ret === undefined || ret === null) { return undefined; }
            return ret.pixels[codePoint - ret.rangeStart];
        }
        measureStr(str: string): [number, number] {
            return this.measureUtf32(Unicode.strToUtf32(str));
        }
        measureUtf32(utf32Str: Unicode.Utf32Str): [number, number] {
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
                } else if (utf32Ch === 0x0A) {
                    yy += size;
                    xx = 0;
                } else {
                    xx += width;
                    w = Math.max(w, xx);
                    h = Math.max(h, yy + this._fontHeight);
                }
            }
            return [w, h];
        }
        drawStr(x: number, y: number, str: string, drawChar: (x: number, y: number, size: number, pixels: Uint8Array) => void): void {
            this.drawUtf32(x, y, Unicode.strToUtf32(str), drawChar);
        }
        drawUtf32(x: number, y: number, utf32Str: Unicode.Utf32Str, drawChar: (x: number, y: number, size: number, pixels: Uint8Array) => void): void {
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
                } else {
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
        use: boolean;
        line: TextBufferLine;	/* 行 */
        row: number;		        /* 列 */

        constructor(line: TextBufferLine, row: number = 0) {
            this.use = true;
            this.line = line;
            this.row = row;
        }
        duplicate(): TextBufferCursor {
            return this.line.parent.duplicateCursor(this);
        }
        dispose(): void {
            this.line.parent.disposeCursor(this);
        }
        checkValid(): void {
            assert(this.use);
            assert(this.line !== null);
            assert(0 <= this.row);
            assert(this.row <= this.line.buffer.length);
        }
        static equal(x: TextBufferCursor, y: TextBufferCursor): boolean {
            if ((x.use === false) || (y.use === false)) {
                return false;
            }
            return (x.line === y.line) && (x.row === y.row);
        }
        copyTo(other: TextBufferCursor): void {
            if (this.use === false || other.use === false) {
                throw new Error();
            }
            other.line = this.line;
            other.row = this.row;
        }
    }

    class TextBufferLine {
        parent: TextBuffer;
        buffer: GapBuffer;
        prev: TextBufferLine;
        next: TextBufferLine;
        constructor(parent: TextBuffer) {
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
        private lines: TextBufferLine;
        private cursors: Set<TextBufferCursor>;
        private totalLength: number;

        getTotalLength(): number {
            return this.totalLength;
        }

        clear() {
            this.lines.next = null;
            this.lines.prev = null;
            this.lines.buffer.clear();
            this.totalLength = 0;
            for (const cur of this.cursors) {
                if (cur.use === false) { this.cursors.delete(cur); continue; }
                cur.line = this.lines;
                cur.row = 0;
            };
        }

        allocateCursor(): TextBufferCursor {
            var newCursor = new TextBufferCursor(this.lines);
            this.cursors.add(newCursor);
            return newCursor;
        }
        duplicateCursor(cursor: TextBufferCursor): TextBufferCursor {
            if ((!this.cursors.has(cursor)) || (cursor.use === false)) {
                throw new Error();
            }
            var newCursor = new TextBufferCursor(cursor.line, cursor.row);
            this.cursors.add(newCursor);
            return newCursor;
        }

        disposeCursor(cursor: TextBufferCursor) {
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
            this.cursors = new Set<TextBufferCursor>();
        }

        private checkValidCursor(cursor: TextBufferCursor): void {
            if ((!this.cursors.has(cursor)) || (cursor.use === false)) {
                throw new Error();
            }
            cursor.checkValid();
        }

        deleteCharacterOnCursor(cursor: TextBufferCursor) {
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

        private removeLine(buf: TextBufferLine) {
            if (buf.prev === null && buf.next === null) {
                // １行だけ存在する場合

                // １行目をクリア
                buf.buffer.clear();

                // 行内にあるカーソルを先頭に移動させる
                for (const cur of this.cursors) {
                    if (cur.use === false) { this.cursors.delete(cur); continue; }
                    if (cur.line !== buf) { continue; }
                    // 最初の行に対する行削除なので最初の行に設定
                    cur.row = 0;
                }
            } else {
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
                    if (cur.use === false) { this.cursors.delete(cur); continue; }
                    if (cur.line !== buf) { continue; }

                    if (buf.next !== null) {
                        //   次の行がある場合：次の行の行頭に移動する
                        cur.line = buf.next;
                        cur.row = 0;
                    } else if (buf.prev !== null) {
                        //   次の行がなく前の行がある場合：前の行の行末に移動する。
                        cur.line = buf.prev;
                        cur.row = buf.prev.buffer.length;
                    } else {
                        //   どちらもない場合：最初の行に対する行削除なので最初の行に設定
                        cur.line = this.lines;
                        cur.row = 0;
                    }
                }

                // 行情報を破棄
                buf.dispose();
            }
        }


        deleteMany(s: TextBufferCursor, len: number): void {
            const start = this.duplicateCursor(s);

            // １文字づつ消す
            while (len > 0) {
                this.deleteCharacterOnCursor(start);
                len--;
            }
            this.disposeCursor(start);
        }

        takeCharacterOnCursor(cursor: TextBufferCursor): number {
            if ((!this.cursors.has(cursor)) || (cursor.use === false)) {
                throw new Error();
            }

            if (cursor.line.buffer.length === cursor.row) {
                if (cursor.line.next === null) {
                    return 0x00; // 終端なのでヌル文字を返す
                }
                else {
                    return 0x0A;	// 改行文字を返す
                }

            }
            return cursor.line.buffer.get(cursor.row);

        }

        insertCharacterOnCursor(cursor: TextBufferCursor, ch: number): boolean {

            this.checkValidCursor(cursor);
            if (ch === 0x0A)// 0x0A = \n
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
                    if (cur.use === false) { this.cursors.delete(cur); continue; }
                    if (cur === cursor) { continue; }

                    if (cur.line === buf && cur.row > len) {
                        cur.line = nb;
                        cur.row -= len;
                    }
                }

                // 行を増やす＝改行挿入なので文字は増えていないが仮想的に1文字使っていることにする
                this.totalLength++;
            } else {
                cursor.line.buffer.insert(cursor.row, ch);
                this.totalLength++;
            }
            return true;
        }

        // カーソル位置の文字を上書きする
        overwriteCharacterOnCursor(cursor: TextBufferCursor, ch: number): boolean {
            this.checkValidCursor(cursor);
            if (cursor.line.buffer.length === cursor.row || ch === 0x0A) {
                this.insertCharacterOnCursor(cursor, ch);
            } else {
                this.totalLength++;
                cursor.line.buffer.set(cursor.row, ch);
            }
            return true;
        }

        // テキストバッファカーソルを１文字前に移動させる
        // falseはcursorが元々先頭であったことを示す。
        cursorBackward(cursor: TextBufferCursor): boolean {
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
        cursorForward(cursor: TextBufferCursor): boolean {
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
        compareCursor(c1: TextBufferCursor, c2: TextBufferCursor): number {
            this.checkValidCursor(c1);
            this.checkValidCursor(c2);

            if (c1.line === c2.line) {
                if (c1.row < c2.row) { return -1; }
                if (c1.row === c2.row) { return 0; }
                if (c1.row > c2.row) { return 1; }
            }

            for (let p = this.lines; p !== null; p = p.next) {
                if (p === c1.line) { return -1; }
                if (p === c2.line) { return 1; }
            }

            throw new Error(); // 不正なカーソルが渡された
        }

        // カーソルが同一位置を示しているか判定
        // CompareCursorより高速
        isEqualCursor(c1: TextBufferCursor, c2: TextBufferCursor): boolean {
            this.checkValidCursor(c1);
            this.checkValidCursor(c2);
            if (c1.line === c2.line) {
                if (c1.row === c2.row) { return true; }
            }
            return false;
        }

        // カーソル位置が文書の先頭か判定
        isBeginningOfDocument(c: TextBufferCursor): boolean {
            this.checkValidCursor(c);
            return (c.line.prev === null && c.row === 0);
        }

        isEndOfDocument(c: TextBufferCursor): boolean {
            this.checkValidCursor(c);
            if (c.line.buffer.length === c.row && c.line.next === null) { return true; }
            return false;
        }

        // カーソルを行頭に移動
        moveToBeginningOfLine(cur: TextBufferCursor): void {
            this.checkValidCursor(cur);
            cur.row = 0;
        }

        // カーソルを文書の先頭に移動
        moveToBeginningOfDocument(cur: TextBufferCursor): void {
            this.checkValidCursor(cur);
            cur.line = this.lines;
            cur.row = 0;
        }

        // 区間[start, end)の文字数をカウントする。endは含まれないので注意
        rangeLength(start: TextBufferCursor, end: TextBufferCursor) {
            this.checkValidCursor(start);
            this.checkValidCursor(end);
            let n = 0;
            const c: TextBufferCursor = this.duplicateCursor(start);
            for (; ;) {
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

    interface ITextPosition { x: number; y: number }

    class TextVram {
        private _palette: Uint32Array;
        private _pixels: Uint32Array;
        private _backgroundColor: number;
        private _textColor: number;
        private _cursorIndex: number;
        private _width: number;
        private _height: number;
        private _font: BitmapFont;
        get width(): number { return this._width; }
        get height(): number { return this._height; }
        constructor(font: BitmapFont, width: number, height: number) {
            this._font = font;
            this._width = width;
            this._height = height;
            this._palette = new Uint32Array(16);
            for (let i = 0; i < 8; i++) {
                this.setPaletteColor(
                    i,
                    (255 * (i >> 2)),
                    (255 * (i & 1)),
                    (255 * ((i >> 1) & 1))
                );
            }
            for (let i = 8; i < 16; i++) {
                //8以上は全て白に初期化
                this.setPaletteColor(
                    i,
                    255,
                    255,
                    255
                );
            }
            this._pixels = new Uint32Array(this._width * this._height * 2);
            this._backgroundColor = 0;
            this._textColor = 7;
            this._cursorIndex = 0;
            this.clear();
        }
        resize(w: number, h: number): void {
            this._width = w;
            this._height = h;
            this._pixels = new Uint32Array(this._width * this._height * 2);
            this._cursorIndex = 0;
            this.clear();
        }
        clear(): void {
            this._pixels.fill(0);
        }
        setPaletteColor(index: number, r: number, g: number, b: number): void {
            const color = (0xFF << 24) | (r << 16) | (g << 8) | (b << 0);
            this._palette[index & 0x0F] = color;

        }
        getPaletteColor(index: number): number {
            return this._palette[index & 0x0F];
        }
        getCursorIndex(): number {
            return this._cursorIndex;
        }
        getCursorPosition(): ITextPosition {
            return {
                x: ~~(this._cursorIndex % this._width),
                y: ~~(this._cursorIndex / this._width)
            };

        }
        setCursorPosition(x: number, y: number): void {
            if (x < 0 || x >= this._width || y < 0 || y >= this._height) {
                return;
            }
            this._cursorIndex = y * this._width + x;
        }
        getPixels(): Uint32Array { return this._pixels; }
        getPalette(): Uint32Array { return this._palette; }
        getBackgroundColor(): number { return this._backgroundColor; }
        setBackgroundColor(color: number): void { this._backgroundColor = color; }
        fillBackgroundColor(x: number, y: number, length: number, palette: number): void {
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
        getTextColor(): number { return this._textColor; }
        setTextColor(color: number): void { this._textColor = color; }
        fillTextColor(pos: ITextPosition, length: number, palette: number): void {
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
        scrollUp(): void {
            this._pixels.copyWithin(0, this._width * 2, this._pixels.length - this._width * 2);
            this._pixels.fill(0, this._pixels.length - this._width * 2, this._pixels.length);
        }
        putChar(utf32: number): void {
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
        putStr(utf32Str: number[] | Uint32Array): void {
            for (let i = 0; i < utf32Str.length; i++) {
                this.putChar(utf32Str[i]);
            }

        }
        //カーソル位置に符号なし整数nを10進数表示
        putDigit(n: number): void {
            const n1 = ~~(n / 10);
            let d = 1;
            while (n1 >= d) { d *= 10; }
            while (d !== 0) {
                this.putChar(0x30 + ~~(n / d));
                n %= d;
                d = ~~(d / 10);
            }

        }
        //カーソル位置に符号なし整数nをe桁の10進数表示（前の空き桁部分はスペースで埋める）
        putDigit2(n: number, e: number): void {
            if (e === 0) {
                return;
            }
            const n1 = ~~(n / 10);
            let d = 1;
            e--;
            while (e > 0 && n1 >= d) { d *= 10; e--; }
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
        calcCursorPosition(str: Uint32Array, head: number, cur: number, p: ITextPosition, width: number): void {
            for (; ;) {
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
    const COLOR_NORMALTEXT_BG = 0;	 //通常テキスト背景
    const COLOR_ERRORTEXT = 4; //エラーメッセージテキスト色
    const COLOR_AREASELECTTEXT = 4; //範囲選択テキスト色
    const COLOR_BOTTOMLINE = 5; //画面最下行のテキスト色
    const COLOR_CURSOR = 6; //カーソル色
    const COLOR_BOTTOMLINE_BG = 8; //画面最下行背景

    class TextEditor {
        private textBuffer: TextBuffer = null;
        private font: BitmapFont = null;
        private textVram: TextVram = null;
        private insertMode: boolean = true;
        private needUpdate: boolean = false;
        /**
        * 現在のカーソル位置に対応するテキスト位置
        * [0] は現在位置を示す値
        * [1] はsave_cursorで退避された値
        */
        private currentCursor: [TextBufferCursor, TextBufferCursor] = [null, null];

        /**
        * 現在表示中の画面の左上に対応するテキスト位置
        * [0] は現在位置を示す値
        * [1] はsave_cursorで退避された値
        */
        private displayLeftTopCursor: [TextBufferCursor, TextBufferCursor] = [null, null];

        /**
        * 画面上でのカーソルの位置
        * [0] は現在位置を示す値
        * [1] はsave_cursorで退避された値
        */
        private currentCursorPosition: [ITextPosition, ITextPosition] = [null, null];

        getCursorScreenPosX(): number { return this.currentCursorPosition[0].x; }
        getCursorScreenPosY(): number { return this.currentCursorPosition[0].y; }

        //上下移動時の仮カーソルX座標
        private cachedCursorScreenPosX: number = 0;

        // 範囲選択時のカーソルのスタート位置
        private selectRangeStartCursor: TextBufferCursor = null;

        // 範囲選択時のカーソルのスタート座標（画面上）
        private selectStartCursorScreenPos: ITextPosition = null;

        //保存後に変更されたかを表すフラグ
        private edited: boolean = false;

        // クリップボードデータ
        private clipboard: Uint32Array = null;

        private get editorScreenHeight(): number {
            return this.textVram.height - 1;
        }

        constructor(font: BitmapFont, vram: TextVram) {
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

        resize(w: number, h: number) {
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
        private validateDisplayCursorPos() {
            const p: ITextPosition = this.calcScreenPosFromLineHead(this.displayLeftTopCursor[0], this.currentCursor[0]);
            assert((p.x === this.currentCursorPosition[0].x) && (p.y === this.currentCursorPosition[0].y));
        }

        // 画面上の位置と折り返しを考慮に入れながらカーソルと画面上の位置を連動させてひとつ進める
        private iterateCursorWithScreenPos(p: ITextPosition, cur: TextBufferCursor): boolean {
            // １文字先（移動先）を読む
            const c1: number = this.textBuffer.takeCharacterOnCursor(cur);
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
            } else if (c1 === 0x09) {
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
            } else if (p.x + w1 + w2 > this.textVram.width) {
                // 一文字目、二文字目両方を受け入れると行末を超える場合
                p.x = 0;
                p.y++;
            } else {
                p.x += w1;
            }
            return true;
        }

        // headのスクリーン座標を(0,0)とした場合のcurのスクリーン座標位置を算出
        private calcScreenPosFromLineHead(head: TextBufferCursor, cur: TextBufferCursor): ITextPosition {
            head.checkValid();
            cur.checkValid();

            const p: ITextPosition = { x: 0, y: 0 };
            const h: TextBufferCursor = head.duplicate();
            while (TextBufferCursor.equal(h, cur) === false) {
                if (this.iterateCursorWithScreenPos(p, h) === false) {
                    break;
                }
            }
            h.dispose();
            return p;
        }

        // 画面原点を基準に現在の行の画面上での最左位置に対応する位置にカーソルを移動
        private moveCursorToDisplayLineHead(textPosition: ITextPosition, textBufferCursor: TextBufferCursor): boolean {
            textBufferCursor.checkValid();
            const pos: ITextPosition = { x: 0, y: 0 };
            const prevPos: ITextPosition = { x: 0, y: 0 };
            const cur: TextBufferCursor = this.displayLeftTopCursor[0].duplicate();
            const prevCur: TextBufferCursor = this.displayLeftTopCursor[0].duplicate();
            // タブや折り返しなどがあるため、単純に逆順に辿ることができない場合もあって場合分けすると面倒くさい
            //画面原点に対応するバッファカーソルは常に計算済みなのでそこからの距離を求める
            let result: boolean;
            for (; ;) {
                prevPos.x = pos.x; prevPos.y = pos.y;
                cur.copyTo(prevCur);
                if (pos.y === textPosition.y && pos.x === 0) {
                    textPosition.x = pos.x; textPosition.y = pos.y;
                    cur.copyTo(textBufferCursor);
                    result = true;
                    break;
                }
                else if (this.iterateCursorWithScreenPos(pos, cur) === false) {
                    textPosition.x = prevPos.x; textPosition.y = prevPos.y;
                    prevCur.copyTo(textBufferCursor);
                    result = false;
                    break;
                }
                else if (pos.y === textPosition.y) {
                    assert(pos.x === 0);
                    textPosition.x = pos.x; textPosition.y = pos.y;
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
        private moveCursorToDisplayLineTail(textPosition: ITextPosition, textBufferCursor: TextBufferCursor): boolean {
            textBufferCursor.checkValid();

            const pos: ITextPosition = { x: textPosition.x, y: textPosition.y };
            const prevPos: ITextPosition = { x: 0, y: 0 };
            const cur: TextBufferCursor = textBufferCursor.duplicate();
            const prevCur: TextBufferCursor = textBufferCursor.duplicate();
            let result: boolean;
            for (; ;) {
                prevPos.x = pos.x; prevPos.y = pos.y;
                cur.copyTo(prevCur);
                if (this.iterateCursorWithScreenPos(pos, cur) === false) {
                    textPosition.x = pos.x; textPosition.y = pos.y;
                    cur.copyTo(textBufferCursor);
                    result = false;
                    break;
                }
                else if (pos.y === textPosition.y + 1) {
                    textPosition.x = prevPos.x; textPosition.y = prevPos.y;
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
        private moveCursorToPrev(textPosition: ITextPosition, textBufferCursor: TextBufferCursor): boolean {
            textBufferCursor.checkValid();
            const pos: ITextPosition = { x: 0, y: 0 };
            const prevPos: ITextPosition = { x: 0, y: 0 };
            const cur: TextBufferCursor = this.displayLeftTopCursor[0].duplicate();
            const prevCur: TextBufferCursor = this.displayLeftTopCursor[0].duplicate();
            while (!(pos.y === textPosition.y && pos.x === textPosition.x)) {
                prevPos.x = pos.x; prevPos.y = pos.y;
                cur.copyTo(prevCur);
                assert(this.iterateCursorWithScreenPos(pos, cur));
            }
            textPosition.x = prevPos.x; textPosition.y = prevPos.y;
            prevCur.copyTo(textBufferCursor);
            cur.dispose();
            prevCur.dispose();
            return true;
        }

        // 指定したディスプレイ位置になるまでカーソル位置を進める
        private moveCursorToDisplayPos(targetTextPosition: ITextPosition, resultTextPosition: ITextPosition, resultTextBufferCursor: TextBufferCursor) {
            const pos: ITextPosition = { x: 0, y: 0 };
            const prevPos: ITextPosition = { x: 0, y: 0 };
            const cur: TextBufferCursor = this.displayLeftTopCursor[0].duplicate();
            const prevCur: TextBufferCursor = this.displayLeftTopCursor[0].duplicate();
            let ret: boolean;
            for (; ;) {
                if (pos.y === targetTextPosition.y && pos.x >= targetTextPosition.x) {
                    resultTextPosition.x = pos.x; resultTextPosition.y = pos.y;
                    ret = true;
                    break;
                }
                if (pos.y > targetTextPosition.y) {
                    resultTextPosition.x = prevPos.x; resultTextPosition.y = prevPos.y;
                    prevCur.copyTo(cur);
                    ret = true;
                    break;
                }
                prevPos.x = pos.x; prevPos.y = pos.y;
                cur.copyTo(prevCur);
                if (this.iterateCursorWithScreenPos(pos, cur) === false) {
                    // 文書末に到達
                    resultTextPosition.x = prevPos.x; resultTextPosition.y = prevPos.y;
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
        private overwriteCharacter(utf32: number): boolean {
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
        private insertCharacter(utf32: number): boolean {
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
        private screenScrollDown(): boolean {
            const c: TextBufferCursor = this.displayLeftTopCursor[0].duplicate();
            const p: ITextPosition = { x: 0, y: 0 };
            for (; ;) {
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
        private screenScrollUp(): boolean {
            // カーソルの画面Ｙ座標が最上列なので左上座標の更新が必要
            const lt: TextBufferCursor = this.displayLeftTopCursor[0].duplicate();
            if (this.textBuffer.cursorBackward(lt)) {
                // 左上から１文字戻る＝画面上における前の行の末尾にバッファ上のカーソルを移動

                // 行頭位置を算出
                const c: TextBufferCursor = lt.duplicate();
                this.textBuffer.moveToBeginningOfLine(c);

                // 行頭位置を(0,0)点としてカーソルltの画面上位置を計算
                const lp: ITextPosition = this.calcScreenPosFromLineHead(c, lt);

                // ltの画面位置を超えない位置の行頭をサーチ
                const p: ITextPosition = { x: 0, y: 0 };
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
        private cursorMoveLeftBody(): void {
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
        cursorMoveLeft(): void {
            this.validateDisplayCursorPos();
            this.cursorMoveLeftBody();
            this.validateDisplayCursorPos();
        }

        // カーソルを右に移動
        private cursorMoveRightBody(): void {
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
        cursorMoveRight(): void {
            this.validateDisplayCursorPos();
            this.cursorMoveRightBody();
            this.validateDisplayCursorPos();
        }

        private cursorMoveUpBody(): void {
            if (this.currentCursorPosition[0].y === 0) {
                if (this.screenScrollUp() === false) {
                    // 画面を上にスクロールできない＝戻れないので終了
                    return;
                }
            }
            this.validateDisplayCursorPos();

            // 左上の補正が終った
            // 左上位置から画面上で１行上の位置をサーチする
            const target: ITextPosition = { x: this.cachedCursorScreenPosX, y: this.currentCursorPosition[0].y - 1 };
            const p: ITextPosition = { x: 0, y: 0 };
            const c: TextBufferCursor = this.textBuffer.allocateCursor();

            this.moveCursorToDisplayPos(target, p, c);

            c.copyTo(this.currentCursor[0]);
            this.currentCursorPosition[0] = p;
            c.dispose();

            // 画面更新が必要な状態に設定
            this.needUpdate = true;

            this.validateDisplayCursorPos();
        }

        cursorMoveUp(): void {
            this.validateDisplayCursorPos();
            this.cursorMoveUpBody();
            this.validateDisplayCursorPos();
        }

        private cursorMoveDownBody(): void {
            // 左上位置からサーチする
            const target: ITextPosition = { x: this.cachedCursorScreenPosX, y: this.currentCursorPosition[0].y + 1 };
            const p: ITextPosition = { x: 0, y: 0 };
            const c: TextBufferCursor = this.textBuffer.allocateCursor();

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

        cursorMoveDown(): void {
            this.validateDisplayCursorPos();
            this.cursorMoveDownBody();
            this.validateDisplayCursorPos();
        }

        // カーソルを行頭に移動する
        private cursorMoveToLineHeadBody(): void {
            this.moveCursorToDisplayLineHead(this.currentCursorPosition[0], this.currentCursor[0]);
            this.cachedCursorScreenPosX = 0;
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }

        cursorMoveToLineHead(): void {
            this.validateDisplayCursorPos();
            this.cursorMoveToLineHeadBody();
            this.validateDisplayCursorPos();
        }

        // カーソルを行末に移動する
        private cursorMoveToLineTailBody(): void {
            this.moveCursorToDisplayLineTail(this.currentCursorPosition[0], this.currentCursor[0]);
            this.cachedCursorScreenPosX = this.currentCursorPosition[0].x;
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }

        cursorMoveToLineTail(): void {
            this.validateDisplayCursorPos();
            this.cursorMoveToLineTailBody();
            this.validateDisplayCursorPos();
        }

        private cursorMovePageUpBody(): void {
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

            let i: number;
            const prev: TextBufferCursor = this.textBuffer.allocateCursor();
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

        cursorMovePageUp(): void {
            this.validateDisplayCursorPos();
            this.cursorMovePageUpBody();
            this.validateDisplayCursorPos();
        }

        private cursorMovePageDownBody(): void {
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
                    break;// バッファ最下行で移動できなかった場合抜ける
                }
            }

            let i: number;
            const prev: TextBufferCursor = this.textBuffer.allocateCursor();
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

        cursorMovePageDown(): void {
            this.validateDisplayCursorPos();
            this.cursorMovePageDownBody();
            this.validateDisplayCursorPos();
        }

        cursorMoveToBeginningOfDocument(): void {
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
        copyToClipboard(): void {
            const bp1: TextBufferCursor = this.textBuffer.allocateCursor();
            const bp2: TextBufferCursor = this.textBuffer.allocateCursor();

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

            const pd: number[] = [];
            while (this.textBuffer.isEqualCursor(bp1, bp2) === false) {
                pd.push(this.textBuffer.takeCharacterOnCursor(bp1));
                this.textBuffer.cursorForward(bp1);
            }
            this.clipboard = new Uint32Array(pd);
            bp1.dispose();
            bp2.dispose();
        }

        // クリップボードから貼り付け
        pasteFromClipboard(): void {
            // クリップボードから貼り付け
            for (let i = 0; i < this.clipboard.length; i++) {
                if (this.insertCharacter(this.clipboard[i]) === false) {
                    break;
                }
                this.cursorMoveRight();//画面上、バッファ上のカーソル位置を1つ後ろに移動
            }
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }

        ///////////////////

        //範囲選択モード開始時のカーソル開始位置設定
        setSelectedRangeStart(): void {
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

        private getSelectedRangeLength(): number {
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
        private deleteSelectedRange(): void {
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

        checkSelectedRange(): void {
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
        saveCursor(): void {
            assert(this.currentCursor[1] === null);
            assert(this.displayLeftTopCursor[1] === null);
            this.currentCursor[1] = this.currentCursor[0].duplicate();
            this.displayLeftTopCursor[1] = this.displayLeftTopCursor[0].duplicate();
            this.currentCursorPosition[1].x = this.currentCursorPosition[0].x;
            this.currentCursorPosition[1].y = this.currentCursorPosition[0].y;
        }

        //カーソル関連グローバル変数を一時避難場所から戻す
        restoreCursor(): void {
            assert(this.currentCursor[0] !== null);
            assert(this.displayLeftTopCursor[0] !== null);
            assert(this.currentCursor[1] !== null);
            assert(this.displayLeftTopCursor[1] !== null);
            this.currentCursor[0].dispose(); this.currentCursor[0] = this.currentCursor[1]; this.currentCursor[1] = null;
            this.displayLeftTopCursor[0].dispose(); this.displayLeftTopCursor[0] = this.displayLeftTopCursor[1]; this.displayLeftTopCursor[1] = null;
            this.currentCursorPosition[0].x = this.currentCursorPosition[1].x; this.currentCursorPosition[1].x = 0;
            this.currentCursorPosition[0].y = this.currentCursorPosition[1].y; this.currentCursorPosition[1].y = 0;
            // 画面更新が必要な状態に設定
            this.needUpdate = true;
        }

        discardSavedCursor(): void {
            if (this.currentCursor[1] !== null) {
                this.currentCursor[1].dispose(); this.currentCursor[1] = null;
            }
            if (this.displayLeftTopCursor[1] !== null) {
                this.displayLeftTopCursor[1].dispose(); this.displayLeftTopCursor[1] = null;
            }
            this.currentCursorPosition[1].x = 0;
            this.currentCursorPosition[1].y = 0;
        }

        ///////////////////
        private menuStr = Unicode.strToUtf32("F1:LOAD F2:SAVE   F4:NEW ");

        draw(): boolean {
            if (this.needUpdate === false) {
                return false;
            }
            this.textVram.setTextColor(COLOR_NORMALTEXT);
            this.textVram.setBackgroundColor(COLOR_NORMALTEXT_BG);

            // 画面更新するので更新フラグ解除
            this.needUpdate = false;

            let textColor: number = COLOR_NORMALTEXT;

            let selectStart: TextBufferCursor;
            let selectEnd: TextBufferCursor;

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
            const cur: TextBufferCursor = this.displayLeftTopCursor[0].duplicate();
            const pos: ITextPosition = { x: 0, y: 0 };
            while (pos.y < this.editorScreenHeight) {
                // 選択範囲の始点/終点に到達してたら色設定変更
                if (this.selectRangeStartCursor !== null) {
                    if (this.textBuffer.isEqualCursor(cur, selectStart)) { textColor = COLOR_AREASELECTTEXT; }
                    if (this.textBuffer.isEqualCursor(cur, selectEnd)) { textColor = COLOR_NORMALTEXT; }
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

        save<T>(start: (context: T) => boolean, write: (context: T, ch: number) => boolean, end: (context: T) => void, context: T): boolean {
            let ret: boolean = false;
            if (start(context)) {
                ret = true;
                const bp: TextBufferCursor = this.textBuffer.allocateCursor();
                while (this.textBuffer.isEndOfDocument(bp) === false) {
                    const ch = this.textBuffer.takeCharacterOnCursor(bp);
                    if (ch === 0x0000) { break; }
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

        load<T>(start: (context: T) => boolean, read: (context: T) => number, end: (context: T) => void, context: T): boolean {
            let ret: boolean = false;

            // カーソル位置を行頭に移動
            this.cursorMoveToBeginningOfDocument();

            // クリア
            this.clear();

            if (start(context)) {
                ret = true;
                this.clear();
                let ch: number;
                while ((ch = read(context)) !== 0) {
                    this.insertCharacter(ch);
                    this.cursorMoveRight();//画面上、バッファ上のカーソル位置を1つ後ろに移動
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
        inputNormalCharacter(utf32: number): void {
            // 通常文字入力処理
            // k:入力された文字コード

            this.edited = true; //編集済みフラグを設定

            if (this.insertMode || utf32 === 0x0A || this.selectRangeStartCursor !== null) { // 挿入モードの場合
                // 選択範囲を削除
                if (this.selectRangeStartCursor !== null) {
                    this.deleteSelectedRange();
                }
                if (this.insertCharacter(utf32)) {
                    this.cursorMoveRight();//画面上、バッファ上のカーソル位置を1つ後ろに移動
                }
            }
            else { //上書きモード
                if (this.overwriteCharacter(utf32)) {
                    this.cursorMoveRight();//画面上、バッファ上のカーソル位置を1つ後ろに移動
                }
            }
        }

        inputControlCharacter(virtualKey: VirtualKey, controlKey: ControlKey): void {
            // 制御文字入力処理
            // k:制御文字の仮想キーコード
            // sh:シフト関連キー状態

            this.saveCursor(); //カーソル関連変数退避（カーソル移動できなかった場合戻すため）

            switch (virtualKey) {
                case VirtualKey.VKEY_LEFT:
                case VirtualKey.VKEY_NUMPAD4:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((controlKey & ControlKey.CHK_SHIFT) === 0 || (virtualKey === VirtualKey.VKEY_NUMPAD4) && (controlKey & ControlKey.CHK_NUMLK)) {
                        if (this.selectRangeStartCursor !== null) {
                            this.selectRangeStartCursor.dispose();
                            this.selectRangeStartCursor = null;
                        }
                    }
                    else if (this.selectRangeStartCursor === null) {
                        this.setSelectedRangeStart(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    if (controlKey & ControlKey.CHK_CTRL) {
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
                case VirtualKey.VKEY_RIGHT:
                case VirtualKey.VKEY_NUMPAD6:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((controlKey & ControlKey.CHK_SHIFT) === 0 || (virtualKey === VirtualKey.VKEY_NUMPAD6) && (controlKey & ControlKey.CHK_NUMLK)) {
                        if (this.selectRangeStartCursor !== null) {
                            this.selectRangeStartCursor.dispose();
                            this.selectRangeStartCursor = null;
                        }
                    }
                    else if (this.selectRangeStartCursor === null) {
                        this.setSelectedRangeStart(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    if (controlKey & ControlKey.CHK_CTRL) {
                        //CTRL＋右矢印でEnd
                        this.cursorMoveToLineTail();
                        break;
                    }
                    this.cursorMoveRight();
                    if (this.selectRangeStartCursor !== null && (this.textBuffer.isEqualCursor(this.displayLeftTopCursor[0], this.displayLeftTopCursor[1]) === false)) {
                        //範囲選択モードで画面スクロールがあった場合
                        if (this.selectStartCursorScreenPos.y > 0) this.selectStartCursorScreenPos.y--; //範囲スタート位置もスクロール
                        else this.restoreCursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                    }
                    break;
                case VirtualKey.VKEY_UP:
                case VirtualKey.VKEY_NUMPAD8:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((controlKey & ControlKey.CHK_SHIFT) === 0 || (virtualKey === VirtualKey.VKEY_NUMPAD8) && (controlKey & ControlKey.CHK_NUMLK)) {
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
                        } else {
                            this.restoreCursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                        }
                    }
                    break;
                case VirtualKey.VKEY_DOWN:
                case VirtualKey.VKEY_NUMPAD2:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((controlKey & ControlKey.CHK_SHIFT) === 0 || (virtualKey === VirtualKey.VKEY_NUMPAD2) && (controlKey & ControlKey.CHK_NUMLK)) {
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
                        if (this.selectStartCursorScreenPos.y > 0) this.selectStartCursorScreenPos.y--; //範囲スタート位置もスクロール
                        else this.restoreCursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                    }
                    break;
                case VirtualKey.VKEY_HOME:
                case VirtualKey.VKEY_NUMPAD7:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((controlKey & ControlKey.CHK_SHIFT) === 0 || (virtualKey === VirtualKey.VKEY_NUMPAD7) && (controlKey & ControlKey.CHK_NUMLK)) {
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
                case VirtualKey.VKEY_END:
                case VirtualKey.VKEY_NUMPAD1:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((controlKey & ControlKey.CHK_SHIFT) === 0 || (virtualKey === VirtualKey.VKEY_NUMPAD1) && (controlKey & ControlKey.CHK_NUMLK)) {
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
                case VirtualKey.VKEY_PRIOR: // PageUpキー
                case VirtualKey.VKEY_NUMPAD9:
                    //シフト＋PageUpは無効（NumLock＋シフト＋「9」除く）
                    if ((controlKey & ControlKey.CHK_SHIFT) && ((virtualKey !== VirtualKey.VKEY_NUMPAD9) || ((controlKey & ControlKey.CHK_NUMLK) === 0))) {
                        break;
                    }
                    //範囲選択モード解除
                    if (this.selectRangeStartCursor !== null) {
                        this.selectRangeStartCursor.dispose();
                        this.selectRangeStartCursor = null;
                    }

                    this.cursorMovePageUp();
                    break;
                case VirtualKey.VKEY_NEXT: // PageDownキー
                case VirtualKey.VKEY_NUMPAD3:
                    //シフト＋PageDownは無効（NumLock＋シフト＋「3」除く）
                    if ((controlKey & ControlKey.CHK_SHIFT) && ((virtualKey !== VirtualKey.VKEY_NUMPAD3) || ((controlKey & ControlKey.CHK_NUMLK) === 0))) {
                        break;
                    }
                    //範囲選択モード解除
                    if (this.selectRangeStartCursor !== null) {
                        this.selectRangeStartCursor.dispose();
                        this.selectRangeStartCursor = null;
                    }

                    this.cursorMovePageDown();
                    break;
                case VirtualKey.VKEY_DELETE: //Deleteキー
                case VirtualKey.VKEY_DECIMAL: //テンキーの「.」
                    this.edited = true; //編集済みフラグ
                    if (this.selectRangeStartCursor !== null) {
                        this.deleteSelectedRange();//選択範囲を削除
                    }
                    else {
                        this.textBuffer.deleteCharacterOnCursor(this.currentCursor[0]);
                        this.currentCursorPosition[0] = this.calcScreenPosFromLineHead(this.displayLeftTopCursor[0], this.currentCursor[0]);
                    }
                    break;
                case VirtualKey.VKEY_BACK: //BackSpaceキー
                    this.edited = true; //編集済みフラグ
                    if (this.selectRangeStartCursor !== null) {
                        this.deleteSelectedRange();//選択範囲を削除
                        break;
                    }
                    if (this.textBuffer.isBeginningOfDocument(this.currentCursor[0])) {
                        break; //バッファ先頭では無視
                    }
                    this.cursorMoveLeft();
                    this.textBuffer.deleteCharacterOnCursor(this.currentCursor[0]);
                    this.currentCursorPosition[0] = this.calcScreenPosFromLineHead(this.displayLeftTopCursor[0], this.currentCursor[0]);
                    break;
                case VirtualKey.VKEY_INSERT:
                case VirtualKey.VKEY_NUMPAD0:
                    this.insertMode = !this.insertMode; //挿入モード、上書きモードを切り替え
                    break;
                case VirtualKey.VKEY_KEY_C:
                    //CTRL+C、クリップボードにコピー
                    if (this.selectRangeStartCursor !== null && (controlKey & ControlKey.CHK_CTRL)) {
                        this.copyToClipboard();
                    }
                    break;
                case VirtualKey.VKEY_KEY_X:
                    //CTRL+X、クリップボードに切り取り
                    if (this.selectRangeStartCursor !== null && (controlKey & ControlKey.CHK_CTRL)) {
                        this.copyToClipboard();
                        this.deleteSelectedRange(); //選択範囲の削除
                        this.edited = true; //編集済みフラグ
                    }
                    break;
                case VirtualKey.VKEY_KEY_V:
                    //CTRL+V、クリップボードから貼り付け
                    if ((controlKey & ControlKey.CHK_CTRL) === 0) break;
                    if (this.clipboard === null || this.clipboard.length === 0) break;
                    this.edited = true; //編集済みフラグ
                    if (this.selectRangeStartCursor !== null) {
                        //範囲選択している時は削除してから貼り付け
                        this.deleteSelectedRange();//選択範囲を削除
                        this.pasteFromClipboard();//クリップボード貼り付け
                    }
                    else {
                        this.pasteFromClipboard();//クリップボード貼り付け
                    }
                    break;
                case VirtualKey.VKEY_KEY_S:
                    //CTRL+S、SDカードに保存
                    if ((controlKey & ControlKey.CHK_CTRL) === 0) break;
                case VirtualKey.VKEY_F2: //F2キー
                    //this.save_as(); //ファイル名を付けて保存
                    break;
                case VirtualKey.VKEY_KEY_O:
                    //CTRL+O、ファイル読み込み
                    if ((controlKey & ControlKey.CHK_CTRL) === 0) break;
                case VirtualKey.VKEY_F1: //F1キー
                    //F1キー、ファイル読み込み
                    //this.selectfile();	//ファイルを選択して読み込み
                    break;
                case VirtualKey.VKEY_KEY_N:
                    //CTRL+N、新規作成
                    if ((controlKey & ControlKey.CHK_CTRL) === 0) break;
                case VirtualKey.VKEY_F4: //F4キー
                    //this.newtext(); //新規作成
                    break;
            }
            this.discardSavedCursor();
        }

    }

    const keyboardLayout: IKeyboardLineLayout[][] = [
        [
            {
                "height": 2,
                "buttons": [
                    { "id": "ESC", "width": 2, "captions": ["ESC", "ESC"], "keycode": VirtualKey.VKEY_ESCAPE },
                    { "id": "", "width": 2, "captions": [null, null], "keycode": VirtualKey.VKEY_NONE },
                    { "id": "F1", "width": 2, "captions": ["F1", "F1"], "keycode": VirtualKey.VKEY_F1 },
                    { "id": "F2", "width": 2, "captions": ["F2", "F2"], "keycode": VirtualKey.VKEY_F2 },
                    { "id": "F3", "width": 2, "captions": ["F3", "F3"], "keycode": VirtualKey.VKEY_F3 },
                    { "id": "F4", "width": 2, "captions": ["F4", "F4"], "keycode": VirtualKey.VKEY_F4 },
                    { "id": "", "width": 2, "captions": [null, null], "keycode": VirtualKey.VKEY_NONE },
                    { "id": "F5", "width": 2, "captions": ["F5", "F5"], "keycode": VirtualKey.VKEY_F5 },
                    { "id": "F6", "width": 2, "captions": ["F6", "F6"], "keycode": VirtualKey.VKEY_F6 },
                    { "id": "F7", "width": 2, "captions": ["F7", "F7"], "keycode": VirtualKey.VKEY_F7 },
                    { "id": "F8", "width": 2, "captions": ["F8", "F8"], "keycode": VirtualKey.VKEY_F8 },
                    { "id": "", "width": 2, "captions": [null, null], "keycode": VirtualKey.VKEY_NONE },
                    { "id": "F9", "width": 2, "captions": ["F9", "F9"], "keycode": VirtualKey.VKEY_F9 },
                    { "id": "F10", "width": 2, "captions": ["F10", "F10"], "keycode": VirtualKey.VKEY_F10 },
                    { "id": "F11", "width": 2, "captions": ["F11", "F11"], "keycode": VirtualKey.VKEY_F11 },
                    { "id": "F12", "width": 2, "captions": ["F12", "F12"], "keycode": VirtualKey.VKEY_F12 }
                ]
            },
            {
                "height": 1,
                "buttons": [
                    { "id": "", "width": 2, "captions": [null, null], "keycode": VirtualKey.VKEY_NONE }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "`", "width": 2, "captions": ["`", "~"], "keycode": VirtualKey.VKEY_OEM_3 },
                    { "id": "1", "width": 2, "captions": ["1", "!"], "keycode": VirtualKey.VKEY_NUM1 },
                    { "id": "2", "width": 2, "captions": ["2", "@"], "keycode": VirtualKey.VKEY_NUM2 },
                    { "id": "3", "width": 2, "captions": ["3", "#"], "keycode": VirtualKey.VKEY_NUM3 },
                    { "id": "4", "width": 2, "captions": ["4", "$"], "keycode": VirtualKey.VKEY_NUM4 },
                    { "id": "5", "width": 2, "captions": ["5", "%"], "keycode": VirtualKey.VKEY_NUM5 },
                    { "id": "6", "width": 2, "captions": ["6", "^"], "keycode": VirtualKey.VKEY_NUM6 },
                    { "id": "7", "width": 2, "captions": ["7", "&"], "keycode": VirtualKey.VKEY_NUM7 },
                    { "id": "8", "width": 2, "captions": ["8", "*"], "keycode": VirtualKey.VKEY_NUM8 },
                    { "id": "9", "width": 2, "captions": ["9", "("], "keycode": VirtualKey.VKEY_NUM9 },
                    { "id": "0", "width": 2, "captions": ["0", ")"], "keycode": VirtualKey.VKEY_NUM0 },
                    { "id": "-", "width": 2, "captions": ["-", "_"], "keycode": VirtualKey.VKEY_OEM_MINUS },
                    { "id": "=", "width": 2, "captions": ["=", "+"], "keycode": VirtualKey.VKEY_OEM_PLUS },
                    { "id": "BackSpace", "width": 4, "captions": ["back", "back"], "keycode": VirtualKey.VKEY_BACK }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "Tab", "width": 3, "captions": ["tab", "tab"], "keycode": VirtualKey.VKEY_TAB },
                    { "id": "q", "width": 2, "captions": ["q", "Q"], "keycode": VirtualKey.VKEY_KEY_Q },
                    { "id": "w", "width": 2, "captions": ["w", "W"], "keycode": VirtualKey.VKEY_KEY_W },
                    { "id": "e", "width": 2, "captions": ["e", "E"], "keycode": VirtualKey.VKEY_KEY_E },
                    { "id": "r", "width": 2, "captions": ["r", "R"], "keycode": VirtualKey.VKEY_KEY_R },
                    { "id": "t", "width": 2, "captions": ["t", "T"], "keycode": VirtualKey.VKEY_KEY_T },
                    { "id": "y", "width": 2, "captions": ["y", "Y"], "keycode": VirtualKey.VKEY_KEY_Y },
                    { "id": "u", "width": 2, "captions": ["u", "U"], "keycode": VirtualKey.VKEY_KEY_U },
                    { "id": "i", "width": 2, "captions": ["i", "I"], "keycode": VirtualKey.VKEY_KEY_I },
                    { "id": "o", "width": 2, "captions": ["o", "O"], "keycode": VirtualKey.VKEY_KEY_O },
                    { "id": "p", "width": 2, "captions": ["p", "P"], "keycode": VirtualKey.VKEY_KEY_P },
                    { "id": "[", "width": 2, "captions": ["[", "{"], "keycode": VirtualKey.VKEY_OEM_4 },
                    { "id": "]", "width": 2, "captions": ["]", "}"], "keycode": VirtualKey.VKEY_OEM_6 },
                    { "id": "\\", "width": 3, "captions": ["\\", "|"], "keycode": VirtualKey.VKEY_OEM_5 }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "CapsLock", "width": 4, "repeat": false, "captions": ["caps", "caps"], "keycode": VirtualKey.VKEY_CAPITAL },
                    { "id": "a", "width": 2, "captions": ["a", "A"], "keycode": VirtualKey.VKEY_KEY_A },
                    { "id": "s", "width": 2, "captions": ["s", "S"], "keycode": VirtualKey.VKEY_KEY_S },
                    { "id": "d", "width": 2, "captions": ["d", "D"], "keycode": VirtualKey.VKEY_KEY_D },
                    { "id": "f", "width": 2, "captions": ["f", "F"], "keycode": VirtualKey.VKEY_KEY_F },
                    { "id": "g", "width": 2, "captions": ["g", "G"], "keycode": VirtualKey.VKEY_KEY_G },
                    { "id": "h", "width": 2, "captions": ["h", "H"], "keycode": VirtualKey.VKEY_KEY_H },
                    { "id": "j", "width": 2, "captions": ["j", "J"], "keycode": VirtualKey.VKEY_KEY_J },
                    { "id": "k", "width": 2, "captions": ["k", "K"], "keycode": VirtualKey.VKEY_KEY_K },
                    { "id": "l", "width": 2, "captions": ["l", "L"], "keycode": VirtualKey.VKEY_KEY_L },
                    { "id": ";", "width": 2, "captions": [";", ":"], "keycode": VirtualKey.VKEY_OEM_1 },
                    { "id": "'", "width": 2, "captions": ["'", "\""], "keycode": VirtualKey.VKEY_OEM_7 },
                    { "id": "Enter", "width": 4, "captions": ["enter", "enter"], "keycode": VirtualKey.VKEY_RETURN }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "LShift", "width": 5, "repeat": false, "captions": ["shift", "shift"], "keycode": VirtualKey.VKEY_LSHIFT },
                    { "id": "z", "width": 2, "captions": ["z", "Z"], "keycode": VirtualKey.VKEY_KEY_Z },
                    { "id": "x", "width": 2, "captions": ["x", "X"], "keycode": VirtualKey.VKEY_KEY_X },
                    { "id": "c", "width": 2, "captions": ["c", "C"], "keycode": VirtualKey.VKEY_KEY_C },
                    { "id": "v", "width": 2, "captions": ["v", "V"], "keycode": VirtualKey.VKEY_KEY_V },
                    { "id": "b", "width": 2, "captions": ["b", "B"], "keycode": VirtualKey.VKEY_KEY_B },
                    { "id": "n", "width": 2, "captions": ["n", "N"], "keycode": VirtualKey.VKEY_KEY_N },
                    { "id": "m", "width": 2, "captions": ["m", "M"], "keycode": VirtualKey.VKEY_KEY_M },
                    { "id": ",", "width": 2, "captions": [",", "<"], "keycode": VirtualKey.VKEY_OEM_COMMA },
                    { "id": ".", "width": 2, "captions": [".", ">"], "keycode": VirtualKey.VKEY_OEM_PERIOD },
                    { "id": "/", "width": 2, "captions": ["/", "?"], "keycode": VirtualKey.VKEY_OEM_2 },
                    { "id": "RShift", "width": 5, "repeat": false, "captions": ["shift", "shift"], "keycode": VirtualKey.VKEY_RSHIFT }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "LCtrl", "width": 3, "repeat": false, "captions": ["ctrl", "ctrl"], "keycode": VirtualKey.VKEY_LCONTROL },
                    { "id": "Fn", "width": 2, "repeat": false, "captions": ["fn", "fn"], "keycode": VirtualKey.VKEY_NONE },
                    { "id": "LOs", "width": 2, "repeat": false, "captions": ["os", "os"], "keycode": VirtualKey.VKEY_LWIN },
                    { "id": "LAlt", "width": 2, "repeat": false, "captions": ["alt", "alt"], "keycode": VirtualKey.VKEY_MENU },
                    { "id": "Space", "width": 12, "captions": ["space", "space"], "keycode": VirtualKey.VKEY_SPACE },
                    { "id": "RAlt", "width": 3, "repeat": false, "captions": ["alt", "alt"], "keycode": VirtualKey.VKEY_MENU },
                    { "id": "ROs", "width": 2, "repeat": false, "captions": ["os", "os"], "keycode": VirtualKey.VKEY_RWIN },
                    { "id": "Menu", "width": 2, "repeat": false, "captions": ["menu", "menu"], "keycode": VirtualKey.VKEY_RMENU },
                    { "id": "RCtrl", "width": 3, "repeat": false, "captions": ["ctrl", "ctrl"], "keycode": VirtualKey.VKEY_RCONTROL }
                ]
            }
        ], [
            {
                "height": 2,
                "buttons": [
                    { "id": "PrintScreen", "width": 2, "repeat": false, "captions": ["PrtSc", "PrtSc"], "keycode": VirtualKey.VKEY_PRINT },
                    { "id": "ScrollLock", "width": 2, "repeat": false, "captions": ["ScrLk", "ScrLk"], "keycode": VirtualKey.VKEY_SCROLL },
                    { "id": "Pause", "width": 2, "repeat": false, "captions": ["Pause", "Pause"], "keycode": VirtualKey.VKEY_PAUSE }
                ]
            },
            {
                "height": 1,
                "buttons": [
                    { "id": "", "width": 2, "captions": [null, null], "keycode": VirtualKey.VKEY_NONE }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "Insert", "width": 2, "captions": ["Ins", "Ins"], "keycode": VirtualKey.VKEY_INSERT },
                    { "id": "Home", "width": 2, "captions": ["Home", "Home"], "keycode": VirtualKey.VKEY_HOME },
                    { "id": "PageUp", "width": 2, "captions": ["PgUp", "PgUp"], "keycode": VirtualKey.VKEY_PRIOR }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "Delete", "width": 2, "captions": ["Del", "Del"], "keycode": VirtualKey.VKEY_DELETE },
                    { "id": "End", "width": 2, "captions": ["End", "End"], "keycode": VirtualKey.VKEY_END },
                    { "id": "PageDown", "width": 2, "captions": ["PgDn", "PgDn"], "keycode": VirtualKey.VKEY_NEXT }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "", "width": 2, "captions": [null, null], "keycode": VirtualKey.VKEY_NONE }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "", "width": 2, "captions": [null, null], "keycode": VirtualKey.VKEY_NONE },
                    { "id": "ArrowUp", "width": 2, "captions": ["↑", "↑"], "keycode": VirtualKey.VKEY_UP },
                    { "id": "", "width": 2, "captions": [null, null], "keycode": VirtualKey.VKEY_NONE }
                ]
            },
            {
                "height": 2,
                "buttons": [
                    { "id": "ArrowLeft", "width": 2, "captions": ["←", "←"], "keycode": VirtualKey.VKEY_LEFT },
                    { "id": "ArrowDown", "width": 2, "captions": ["↓", "↓"], "keycode": VirtualKey.VKEY_DOWN },
                    { "id": "ArrowRight", "width": 2, "captions": ["→", "→"], "keycode": VirtualKey.VKEY_RIGHT }
                ]
            }
        ]
    ];


    window
        .whenEvent<Event>('load')
        .then<{ stopAnimation: () => void, updateAnimation: (loaded:number, total:number) => void}>(() => {
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
                updateAnimation(loaded:number, total:number) {
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
            }
            loading();
            return loadingInfo;
        })
        .then<BitmapFont>(
            (loadingInfo) => 
                BitmapFont.loadFont('font.bmpf', (l,t) => loadingInfo.updateAnimation(l,t))
                          .then<BitmapFont>((bmpFont) => {
                    loadingInfo.stopAnimation();
                                return bmpFont;
                          })
        )
        .then((bmpFont) => {
            const root = document.getElementById("editor");

            const virtualKeyboard = new VirtualKeyboard(root, keyboardLayout, bmpFont);
            const canvas = document.createElement("canvas");
            const context = canvas.getContext("2d");
            let imageData: ImageData = null;
            root.appendChild(canvas);
            const textVram = new TextVram(bmpFont, ~~(canvas.width / bmpFont.width), ~~(canvas.height / bmpFont.height));
            textVram.setPaletteColor(COLOR_BOTTOMLINE_BG, 0x00, 0x20, 0x80);
            const textEditor = new TextEditor(bmpFont, textVram);
            const keyboard = new Keyboard();
            resize();
            //*
            textEditor.load(
                (s) => { s.str = Unicode.strToUtf32(document.getElementById('demo').innerHTML); return true; },
                (s) => s.str.length > s.index ? s.str[s.index++] : 0,
                (s) => true,
                { str: null, index: 0 }
            );
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
                            } else {
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
                    const sh = keyboard.getCurrentCtrlKeys();             //sh:シフト関連キー状態
                    //Enter押下は単純に改行文字を入力とする
                    if (k2 === VirtualKey.VKEY_RETURN || k2 === VirtualKey.VKEY_SEPARATOR) {
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

    interface IKeyboardLineLayout { height: number, buttons: IKeyboardButtonLayout[]; }
    interface IKeyboardButtonLayout { id: string, keycode: VirtualKey, captions: [string, string], width: number, repeat?: boolean, rect?: { left: number, top: number, width: number, height: number }, index?: number, }

    type KeyHandler = (str: string, keycode: VirtualKey, repeat: boolean) => void;

    class VirtualKeyboard {
        private _layout: IKeyboardLineLayout[][];
        private _buttons: IKeyboardButtonLayout[];
        private _font: BitmapFont;
        private _hitAreaMap: Uint8Array;
        private _canvasElement: HTMLCanvasElement;
        private _canvasRenderingContext: CanvasRenderingContext2D;
        private _canvasOffscreenImageData: ImageData;
        private _shiftKeyDownFlag: boolean;
        private _layoutChangedFlag: boolean;
        private _sizeChangedFlag: boolean;
        private _needRedrawFlag: boolean;
        private _parent: HTMLElement;
        private _sectionWidthsByUnit: number[];
        private _layoutWidthByUnit: number;
        private _layoutHeightByUnit: number;
        private _width: number;
        private _height: number;
        private _left: number;
        private _top: number;
        private _touchPoints: Map<number, number>;
        private _events: Map<string, KeyHandler[]>;

        get left(): number { return this._left; }
        get top(): number { return this._top; }
        get width(): number { return this._width; }
        get height(): number { return this._height; }

        addEventListener(event: string, handler: KeyHandler): void {
            if (!this._events.has(event)) {
                this._events.set(event, []);
            }
            this._events.get(event).push(handler);
        }

        private fire(event: string, str: string, keycode: VirtualKey, repeat: boolean): void {
            if (!this._events.has(event)) {
                return;
            }
            this._events.get(event).forEach((ev) => ev(str, keycode, repeat));
        }

        private calculateLayoutAndSize(): void {
            if (this._layoutChangedFlag) {
                this._sectionWidthsByUnit = this._layout.map((section) =>
                    Math.max(...section.map(column => column.buttons.reduce<number>((s, x) => s + x.width, 0)))
                );
                this._layoutWidthByUnit = this._sectionWidthsByUnit.reduce((s, x) => s + x, 0);
                this._layoutHeightByUnit = Math.max(...this._layout.map((section) => section.reduce<number>((s, column) => s + column.height, 0)));

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
                    if (button.keycode !== VirtualKey.VKEY_NONE) {
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

        public resize() {
            this._sizeChangedFlag = true;
            this.calculateLayoutAndSize();
        }

        constructor(parent: HTMLElement, layout: IKeyboardLineLayout[][], font: BitmapFont) {
            this._parent = parent;
            this._layout = layout;
            this._font = font;
            this._events = new Map<string, KeyHandler[]>();
            this._shiftKeyDownFlag = false;
            this._layoutChangedFlag = true;
            this._sizeChangedFlag = true;
            this._needRedrawFlag = true;
            this._touchPoints = new Map<number, number>();
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


        private getHitKey(x: number, y: number): number {
            if ((0 <= x && x < this._width && 0 <= y && y < this._height)) {
                return this._hitAreaMap[y * this._width + x];
            } else {
                return 0xFF;
            }
        }
        private isShiftKeyDown(): boolean {
            for (let [k, v] of this._touchPoints) {
                if (v === 0xFF) { continue; }
                if (this._buttons[v].keycode === VirtualKey.VKEY_SHIFT ||
                    this._buttons[v].keycode === VirtualKey.VKEY_RSHIFT ||
                    this._buttons[v].keycode === VirtualKey.VKEY_LSHIFT) {
                    return true;
                }
            }
            return false;
        }

        private onTouchDown(finger: number, px: number, py: number): void {
            const x = ~~px;
            const y = ~~py;
            const keyIndex = this.getHitKey(x, y);
            if (keyIndex === 0xFF) { return; }
            if (this._touchPoints.has(finger)) { return; }
            this._touchPoints.set(finger, keyIndex);
            const pushedKey = this._buttons[keyIndex];
            this._needRedrawFlag = true;
            this._shiftKeyDownFlag = this.isShiftKeyDown();

            if (this.buttonIsDown(keyIndex) === 1) {
                this.fire("keydown", pushedKey.captions[this._shiftKeyDownFlag ? 1 : 0], pushedKey.keycode, false);
            }
        }
        private onTouchUp(finger: number, px: number, py: number): void {
            const x = ~~px;
            const y = ~~py;
            const keyIndex = this.getHitKey(x, y);
            if (!this._touchPoints.has(finger)) { return; }
            this._touchPoints.delete(finger);
            if (keyIndex === 0xFF) { return; }
            const pushedKey = this._buttons[keyIndex];
            this._needRedrawFlag = true;
            this._shiftKeyDownFlag = this.isShiftKeyDown();
            if (this.buttonIsDown(keyIndex) === 0) {
                this.fire("keyup", pushedKey.captions[this._shiftKeyDownFlag ? 1 : 0], pushedKey.keycode, false);
            }
        }
        private onTouchMove(finger: number, px: number, py: number): void {
            const x = ~~px;
            const y = ~~py;
            const keyIndex = this.getHitKey(x, y);
            if (!this._touchPoints.has(finger)) { return; }

            // 押されたキーとそれまで押してたキーが同じの場合は何もしない
            const prevKey = this._touchPoints.get(finger);
            if (keyIndex === prevKey) { return; }

            this._touchPoints.set(finger, keyIndex);

            let upKey: IKeyboardButtonLayout = null;
            let downKey: IKeyboardButtonLayout = null;
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

        private buttonIsDown(index: number): number {
            let n = 0;
            for (const [k, v] of this._touchPoints.entries()) {
                if (v === index) { n++; }
            }
            return n;
        }

        render(): void {
            if (this._needRedrawFlag === false) { return; }
            const captionIndex = this._shiftKeyDownFlag ? 1 : 0;
            this._canvasOffscreenImageData.data32.fill(0xFFFFFFFF);
            this._layout.forEach((block) => {
                block.forEach((line) => {
                    line.buttons.forEach((btn) => {
                        if (btn.keycode !== VirtualKey.VKEY_NONE) {
                            if (this.buttonIsDown(btn.index)) {
                                this._canvasOffscreenImageData.fillRect(btn.rect.left, btn.rect.top, btn.rect.width, btn.rect.height, 0xFF000000);
                                const text = btn.captions[captionIndex];
                                if (text !== null && text !== "") {
                                    const [w, h] = this._font.measureStr(text);
                                    const offX = (btn.rect.width - w) >> 1;
                                    const offY = (btn.rect.height - h) >> 1;
                                    this._font.drawStr(btn.rect.left + offX, btn.rect.top + offY, text, (x, y, size, pixels) => this._canvasOffscreenImageData.drawChar(x, y, size, pixels, 0xFFFFFFFF));
                                }
                            } else {
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
}

interface Uint8Array {
    getBit(n: number): number;
    setBit(n: number, value: number): void;
}

Uint8Array.prototype.getBit = function (n: number): number {
    return this[n >> 3] & (0x80 >> (n & 0x07));
}

Uint8Array.prototype.setBit = function (n: number, value: number): void {
    const bit = 0x80 >> (n & 0x07);
    if (value) {
        this[n >> 3] |= bit;
    } else {
        this[n >> 3] &= ~bit;
    }
}

interface ImageData {
    data32: Uint32Array;
    setPixel(x: number, y: number, color: number): void;
    fillRect(x: number, y: number, w: number, h: number, color: number): void;
    drawRect(x: number, y: number, w: number, h: number, color: number): void;
    drawChar(x: number, y: number, size: number, pixel: Uint8Array, color: number): void;

}

ImageData.prototype.drawChar = function (x: number, y: number, size: number, pixel: Uint8Array, color: number): void {
    const data32 = this.data32;
    const stride = this.width;
    let pSrc = 0;
    let pDst = y * stride + x;
    for (let j = 0; j < size; j++) {
        let p = pSrc;
        let q = pDst;
        for (let i = 0; i < size; i++ , p++ , q++) {
            if (pixel[p >> 3] & (0x80 >> (p & 0x07))) {
                data32[q] = color;
            }
        }
        pSrc += size;
        pDst += stride;
    }
}

ImageData.prototype.setPixel = function (x: number, y: number, color: number): void {
    "use strict";
    let _x = ~~x;
    let _y = ~~y;
    if (_x < 0 || _x >= this.width || _y < 0 || _y >= this.height) { return; }
    this.data32[_y * this.width + _x] = color;
}

ImageData.prototype.fillRect = function (left: number, top: number, width: number, height: number, color: number): void {
    "use strict";
    let l = ~~left;
    let t = ~~top;
    let w = ~~width;
    let h = ~~height;

    if (l < 0) { w += l; l = 0; }
    if (l + w > this.width) { w = this.width - l; }
    if (w <= 0) { return; }
    if (t < 0) { h += t; t = 0; }
    if (t + h > this.height) { h = this.height - t; }
    if (h <= 0) { return; }

    const stride = this.width;
    let start = t * stride + l;
    let end = start + w;
    const data32 = this.data32;
    for (let j = 0; j < h; j++) {
        data32.fill(color, start, end);
        start += stride;
        end += stride;
    }
}
ImageData.prototype.drawRect = function (left: number, top: number, width: number, height: number, color: number): void {
    "use strict";
    let l = ~~left;
    let t = ~~top;
    let w = ~~width;
    let h = ~~height;

    if (l < 0) { w += l; l = 0; }
    if (l + w > this.width) { w = this.width - l; }
    if (w <= 0) { return; }
    if (t < 0) { h += t; t = 0; }
    if (t + h > this.height) { h = this.height - t; }
    if (h <= 0) { return; }

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
}

