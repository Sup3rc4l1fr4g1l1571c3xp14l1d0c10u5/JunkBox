// <reference path="C:/Program Files/Microsoft Visual Studio 14.0/Common7/IDE/CommonExtensions/Microsoft/TypeScript/lib.es6.d.ts" />

//interface XMLHttpRequest {
//    responseURL: string;
//}

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

    enum CTRLKEY_FLAG {
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
    enum VIRTUAL_KEY {
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
        static KEYCODEBUFSIZE = 16;

        // /シフト、コントロールキー等の状態
        private ps2shiftkey_a: CTRLKEY_FLAG = CTRLKEY_FLAG.CHK_NONE;

        private keycodebuf: {
            ctrlkey: CTRLKEY_FLAG;
            keycode: VIRTUAL_KEY;
        }[];
        private keycodebufp1: number = 0; //キーコード書き込み先頭ポインタ
        private keycodebufp2: number = 0; //キーコード読み出し先頭ポインタ
        private ps2keystatus: Uint8Array = new Uint8Array(256 / 8); // 仮想コードに相当するキーの状態（Onの時1）
        private currentCtrlKeys: CTRLKEY_FLAG = CTRLKEY_FLAG.CHK_NONE;
        private currentVKeyCode: VIRTUAL_KEY = 0;
        private currentAsciiCode: number = 0;

        static vk2asc1: Uint8Array = new Uint8Array([
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
        ].map(x => (typeof (x) == "string") ? (x as string).codePointAt(0) : (x as number)));

        static vk2asc2: Uint8Array = new Uint8Array([
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
        ].map(x => (typeof (x) == "string") ? (x as string).codePointAt(0) : (x as number)));

        private UpdateCtrlKeyState(vk: VIRTUAL_KEY, breakflag: boolean) {
            // SHIFT,ALT,CTRL,Winキーの押下状態を更新
            let k: CTRLKEY_FLAG = 0;
            switch (vk) {
                case VIRTUAL_KEY.VKEY_SHIFT:
                case VIRTUAL_KEY.VKEY_LSHIFT:
                    k = CTRLKEY_FLAG.CHK_SHIFT_L;
                    break;
                case VIRTUAL_KEY.VKEY_RSHIFT:
                    k = CTRLKEY_FLAG.CHK_SHIFT_R;
                    break;
                case VIRTUAL_KEY.VKEY_CONTROL:
                case VIRTUAL_KEY.VKEY_LCONTROL:
                    k = CTRLKEY_FLAG.CHK_CTRL_L;
                    break;
                case VIRTUAL_KEY.VKEY_RCONTROL:
                    k = CTRLKEY_FLAG.CHK_CTRL_R;
                    break;
                case VIRTUAL_KEY.VKEY_MENU:
                case VIRTUAL_KEY.VKEY_LMENU:
                    k = CTRLKEY_FLAG.CHK_ALT_L;
                    break;
                case VIRTUAL_KEY.VKEY_RMENU:
                    k = CTRLKEY_FLAG.CHK_ALT_R;
                    break;
                case VIRTUAL_KEY.VKEY_LWIN:
                    k = CTRLKEY_FLAG.CHK_WIN_L;
                    break;
                case VIRTUAL_KEY.VKEY_RWIN:
                    k = CTRLKEY_FLAG.CHK_WIN_R;
                    break;
            }
            if (breakflag) {
                this.ps2shiftkey_a = this.ps2shiftkey_a & (~k);
            }
            else {
                this.ps2shiftkey_a = this.ps2shiftkey_a | k;
            }
        }

        // NumLock,CapsLock,ScrollLockの状態更新
        private UpdateLockKeyState(vk: VIRTUAL_KEY): void {
            switch (vk) {
                case VIRTUAL_KEY.VKEY_SCROLL:
                    this.ps2shiftkey_a ^= CTRLKEY_FLAG.CHK_SCRLK;
                    break;
                case VIRTUAL_KEY.VKEY_NUMLOCK:
                    this.ps2shiftkey_a ^= CTRLKEY_FLAG.CHK_NUMLK;
                    break;
                case VIRTUAL_KEY.VKEY_CAPITAL:
                    if ((this.ps2shiftkey_a & CTRLKEY_FLAG.CHK_SHIFT) == 0) return;
                    this.ps2shiftkey_a ^= CTRLKEY_FLAG.CHK_CAPSLK;
                    break;
                default:
                    return;
            }
        }
        private isShiftkey(vk: VIRTUAL_KEY): boolean {
            // SHIFT,ALT,WIN,CTRLが押されたかのチェック
            switch (vk) {
                case VIRTUAL_KEY.VKEY_SHIFT:
                case VIRTUAL_KEY.VKEY_LSHIFT:
                case VIRTUAL_KEY.VKEY_RSHIFT:
                case VIRTUAL_KEY.VKEY_CONTROL:
                case VIRTUAL_KEY.VKEY_LCONTROL:
                case VIRTUAL_KEY.VKEY_RCONTROL:
                case VIRTUAL_KEY.VKEY_MENU:
                case VIRTUAL_KEY.VKEY_LMENU:
                case VIRTUAL_KEY.VKEY_RMENU:
                case VIRTUAL_KEY.VKEY_LWIN:
                case VIRTUAL_KEY.VKEY_RWIN:
                    return true;
                default:
                    return false;
            }
        }
        private isLockkey(vk: VIRTUAL_KEY): boolean {
            // NumLock,SCRLock,CapsLockが押されたかのチェック（押された場合-1を返す）
            switch (vk) {
                case VIRTUAL_KEY.VKEY_SCROLL:
                case VIRTUAL_KEY.VKEY_NUMLOCK:
                case VIRTUAL_KEY.VKEY_CAPITAL:
                    return true;
                default:
                    return false;
            }
        }

        PushKeyStatus(vk: VIRTUAL_KEY, breakflag: boolean) {
            if (this.isShiftkey(vk)) {
                if (breakflag == false && this.ps2keystatus.getBit(vk)) return; //キーリピートの場合、無視
                this.UpdateCtrlKeyState(vk, breakflag); //SHIFT系キーのフラグ処理
            }
            else if (breakflag == false && this.isLockkey(vk)) {
                if (this.ps2keystatus.getBit(vk)) return; //キーリピートの場合、無視
                this.UpdateLockKeyState(vk); //NumLock、CapsLock、ScrollLock反転処理
            }
            //キーコードに対する押下状態配列を更新
            if (breakflag) {
                this.ps2keystatus.setBit(vk, 0);
                return;
            }
            this.ps2keystatus.setBit(vk, 1);

            if ((this.keycodebufp1 + 1 == this.keycodebufp2) ||
                (this.keycodebufp1 == Keyboard.KEYCODEBUFSIZE - 1) && (this.keycodebufp2 == 0)) {
                return; //バッファがいっぱいの場合無視
            }
            this.keycodebuf[this.keycodebufp1].keycode = vk;
            this.keycodebuf[this.keycodebufp1].ctrlkey = this.ps2shiftkey_a;
            this.keycodebufp1++;
            if (this.keycodebufp1 == Keyboard.KEYCODEBUFSIZE) {
                this.keycodebufp1 = 0;
            }
        }

        constructor() {
            // キーボードシステム初期化
            this.keycodebufp1 = 0;
            this.keycodebufp2 = 0;
            this.ps2shiftkey_a = CTRLKEY_FLAG.CHK_NUMLK; // NumLock 初期状態はONとする
            this.keycodebuf = [];
            for (let i = 0; i < Keyboard.KEYCODEBUFSIZE; i++) {
                this.keycodebuf[i] = { ctrlkey: CTRLKEY_FLAG.CHK_NONE, keycode: 0 };
            }

            //全キー離した状態
            this.ps2keystatus.fill(0);

        }

        // キーバッファからキーを一つ読み取る
        // 押されていなければfalseを返す
        ReadKey(): boolean {

            this.currentAsciiCode = 0x00;
            this.currentVKeyCode = 0x0000;
            this.currentCtrlKeys = CTRLKEY_FLAG.CHK_NONE;

            if (this.keycodebufp1 == this.keycodebufp2) {
                return false;
            }
            const k = this.keycodebuf[this.keycodebufp2++];
            this.currentVKeyCode = k.keycode;
            this.currentCtrlKeys = k.ctrlkey;

            if (this.keycodebufp2 == Keyboard.KEYCODEBUFSIZE) {
                this.keycodebufp2 = 0;
            }

            if (k.ctrlkey & (CTRLKEY_FLAG.CHK_CTRL | CTRLKEY_FLAG.CHK_ALT | CTRLKEY_FLAG.CHK_WIN)) {
                return true;
            }

            let k2: number = 0;
            if (k.keycode >= VIRTUAL_KEY.VKEY_KEY_A && k.keycode <= VIRTUAL_KEY.VKEY_KEY_Z) {
                if (((k.ctrlkey & CTRLKEY_FLAG.CHK_SHIFT) != 0) != ((k.ctrlkey & CTRLKEY_FLAG.CHK_CAPSLK) != 0)) {
                    //SHIFTまたはCapsLock（両方ではない）
                    k2 = Keyboard.vk2asc2[k.keycode];
                }
                else {
                    k2 = Keyboard.vk2asc1[k.keycode];
                }
            }
            else if (k.keycode >= VIRTUAL_KEY.VKEY_NUMPAD0 && k.keycode <= VIRTUAL_KEY.VKEY_DIVIDE) {
                //テンキー関連
                if ((k.ctrlkey & (CTRLKEY_FLAG.CHK_SHIFT | CTRLKEY_FLAG.CHK_NUMLK)) == CTRLKEY_FLAG.CHK_NUMLK) {
                    //NumLock（SHIFT＋NumLockは無効）
                    k2 = Keyboard.vk2asc2[k.keycode];
                }
                else {
                    k2 = Keyboard.vk2asc1[k.keycode];
                }
            }
            else {
                if (k.ctrlkey & CTRLKEY_FLAG.CHK_SHIFT) {
                    k2 = Keyboard.vk2asc2[k.keycode];
                }
                else {
                    k2 = Keyboard.vk2asc1[k.keycode];
                }
            }
            this.currentAsciiCode = k2;
            return true;
        }

        GetCurrentCtrlKeys(): CTRLKEY_FLAG {
            return this.currentCtrlKeys;
        }

        Keyboard_GetCurrentVKeyCode(): VIRTUAL_KEY {
            return this.currentVKeyCode;
        }

        Keyboard_GetCurrentAsciiCode(): number {
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
        private gapStart: number;
        private gapEnd: number;
        private gapSize: number;
        private buffer: Uint32Array;

        constructor(gapSize?: number) {
            this.gapSize = gapSize || 64;
            if (this.gapSize <= 0) {
                throw new RangeError("gapSize must be > 0");
            }

            this.buffer = new Uint32Array(this.gapSize);
            this.gapStart = 0;
            this.gapEnd = this.gapSize;
        }
        Dispose(): void {
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

        get(ix: number): number {
            if (ix >= this.length) {
                return undefined;
            }
            if (ix >= this.gapStart) {
                ix += (this.gapEnd - this.gapStart);
            }
            return this.buffer[ix];
        }
        set(ix: number, value: number): void {
            if (ix >= this.length) {
                return;
            }
            if (ix >= this.gapStart) {
                ix += (this.gapEnd - this.gapStart);
            }
            this.buffer[ix] = value;
        }
        private grow(newsize: number): void {
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
        insert(ix: number, value: number): void {
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
        insertAll(ix: number, values: number[] | Uint32Array): void {
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
            this.gapStart = ix;
            this.gapEnd += len;
            if (this.gapEnd > this.buffer.length) {
                this.gapEnd = this.buffer.length;
            }
            return true;
        }
        deleteBefore(ix: number, len: number): boolean {
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
        clear(): void {
            this.gapStart = 0;
            this.gapEnd = this.buffer.length;
        }
        asArray(): Uint32Array {
            const newBuffer = new Uint32Array(this.length);
            let n = 0;
            for (let i = 0; i < this.gapStart; i++ , n++) {
                newBuffer[n] = this.buffer[i];
            }
            for (let i = this.gapEnd; i < this.buffer.length; i++ , n++) {
                newBuffer[n] = this.buffer[i];
            }
            return newBuffer;
        }
        private moveGap(ix: number): void {
            if (ix < this.gapStart) {

                const gapSize = (this.gapEnd - this.gapStart);
                const delta = this.gapStart - ix;

                this.buffer.copyWithin(this.gapEnd - delta, ix, this.gapStart);
                //for (let i = delta - 1; i >= 0; --i) {
                //    this.buffer[this.gapEnd - delta + i] = this.buffer[ix + i];
                //}

                this.gapStart -= delta;
                this.gapEnd -= delta;

            } else if (ix > this.gapStart) {

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

        append(that: GapBuffer): void {
            this.moveGap(this.length);
            this.grow(this.length + that.length);
            for (let i = 0; i < that.length; ++i) {
                this.buffer[this.gapStart++] = that.get(i);
            }
        }

        reduce<U>(callbackfn: (previousValue: U, currentValue: number, currentIndex: number, obj: GapBuffer) => U, initialValue: U): U {
            let currentIndex: number = 0;
            let previousValue = initialValue;
            for (const currentValue of this) {
                previousValue = callbackfn(previousValue, currentValue, currentIndex++, this);
            }
            return previousValue;
        }

        find(predicate: (value: number, index: number, obj: GapBuffer) => boolean): number {
            let index = 0;
            for (const value of this) {
                if (predicate(value, index, this) === true) { return value; }
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
        RangeStart: number;
        RangeEnd: number;
    }

    class BMPFont {
        FontWidth: number;
        FontHeight: number;
        PixelSize: number;
        WidthTable: (IRange & { Width: number; })[];
        PixelTable: (IRange & { Pixels: Uint8Array[]; })[];

        constructor(buffer: ArrayBuffer) {
            const it = new DataViewIterator(buffer, true);
            const fourCC = it.getUint32();
            if (fourCC != 0x46504D42) {
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
                const Pixels: Uint8Array[] = [];
                for (let j = RangeStart; j <= RangeEnd; j++) {
                    Pixels.push(it.getBytes(this.PixelSize));
                }
                this.PixelTable[i] = {
                    RangeStart: RangeStart,
                    RangeEnd: RangeEnd,
                    Pixels: Pixels,
                };
            }
        }

        static loadFont(url: string): Promise<BMPFont> {
            return new Promise<ArrayBuffer>((resolve, reject) => {
                let xhr = new XMLHttpRequest();
                xhr.open('GET', url, true);
                xhr.onload = () => {
                    if (xhr.readyState === 4 && ((xhr.status === 200) || (xhr.responseURL.startsWith("file://") === true && xhr.status === 0))) {
                        resolve(xhr.response);
                    } else {
                        reject(new Error(xhr.statusText));
                    }
                };
                xhr.onerror = () => { reject(new Error(xhr.statusText)); };
                xhr.responseType = 'arraybuffer'
                xhr.send(null);
            }).then((arraybuffer) => {
                return new BMPFont(arraybuffer);
            });
        }

        private static search<T>(table: Array<T & IRange>, codePoint: number): T & IRange {
            let start = 0;
            let end = table.length - 1;
            while (start <= end) {
                const mid = ((end - start) >> 1) + start;
                if (table[mid].RangeStart > codePoint) {
                    end = mid - 1;
                } else if (table[mid].RangeEnd < codePoint) {
                    start = mid + 1;
                } else {
                    return table[mid];
                }
            }
            return undefined;
        }
        getWidth(codePoint: number): number {
            if (0x00 == codePoint) {
                return 0;
            }
            if (0x01 <= codePoint && codePoint <= 0x1F) {
                return 1; // control code
            }
            const ret = BMPFont.search(this.WidthTable, codePoint);
            if (ret === undefined) { return undefined; }
            return ret.Width;
        }
        getPixelWidth(codePoint: number, defaultWidth?: number): number {
            const ret = this.getWidth(codePoint);
            if (ret === undefined) { return defaultWidth; }
            return ret * this.FontWidth;
        }
        getPixel(codePoint: number): Uint8Array {
            const ret = BMPFont.search(this.PixelTable, codePoint);
            if (ret === undefined) { return undefined; }
            return ret.Pixels[codePoint - ret.RangeStart];
        }
        drawStr(x: number, y: number, str: string, pset: (x: number, y: number) => void): void {
            this.drawUtf32(x, y, Unicode.strToUtf32(str), pset);
        }
        drawUtf32(x: number, y: number, utf32Str: Unicode.Utf32Str, pset: (x: number, y: number) => void): void {
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
                } else {
                    const pixel = this.getPixel(utf32Ch);
                    if (pixel) {
                        let pSrc = 0;
                        for (let j = 0; j < size; j++) {
                            let p = pSrc;
                            for (let i = 0; i < size; i++ , p++) {
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
        use: boolean;
        Buffer: text_buffer_line_t;	/* 行 */
        Index: number;		/* 列 */

        constructor(buffer: text_buffer_line_t, index: number = 0) {
            this.use = true;
            this.Buffer = buffer;
            this.Index = index;
        }
        Duplicate(): text_buffer_cursor_t {
            return this.Buffer.parent.DuplicateCursor(this);
        }
        Dispose(): void {
            this.Buffer.parent.DisposeCursor(this);
        }
        CheckValid(): void {
            assert(this.use);
            assert(this.Buffer != null);
            assert(0 <= this.Index);
            assert(this.Index <= this.Buffer.buffer.length);
        }
        static Equal(x: text_buffer_cursor_t, y: text_buffer_cursor_t): boolean {
            if ((x.use == false) || (y.use == false)) {
                return false;
            }
            return (x.Buffer == y.Buffer) && (x.Index == y.Index);
        }
        CopyTo(other: text_buffer_cursor_t): void {
            if (this.use == false || other.use == false) {
                throw new Error();
            }
            other.Buffer = this.Buffer;
            other.Index = this.Index;
        }
    }

    class text_buffer_line_t {
        parent: TextBuffer;
        buffer: GapBuffer;
        prev: text_buffer_line_t;
        next: text_buffer_line_t;
        constructor(parent: TextBuffer) {
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
        private Buffers: text_buffer_line_t;
        private Cur: Set<text_buffer_cursor_t>;
        private TotalLength: number;

        getTotalLength(): number {
            return this.TotalLength;
        }

        clear() {
            this.Buffers.next = null;
            this.Buffers.prev = null;
            this.Buffers.buffer.clear();
            this.TotalLength = 0;
            for (const cur of this.Cur) {
                if (cur.use == false) { this.Cur.delete(cur); continue; }
                cur.Buffer = this.Buffers;
                cur.Index = 0;
            };
        }

        AllocateCursor(): text_buffer_cursor_t {
            var newCursor = new text_buffer_cursor_t(this.Buffers);
            this.Cur.add(newCursor);
            return newCursor;
        }
        DuplicateCursor(cursor: text_buffer_cursor_t): text_buffer_cursor_t {
            if ((!this.Cur.has(cursor)) || (cursor.use == false)) {
                throw new Error();
            }
            var newCursor = new text_buffer_cursor_t(cursor.Buffer, cursor.Index);
            this.Cur.add(newCursor);
            return newCursor;
        }

        DisposeCursor(cursor: text_buffer_cursor_t) {
            if ((!this.Cur.has(cursor)) || (cursor.use == false)) {
                throw new Error();
            }
            this.Cur.delete(cursor);
            cursor.use = false;
            cursor.Buffer = null;
            cursor.Index = -1;
        }

        constructor() {
            this.Buffers = new text_buffer_line_t(this);
            this.TotalLength = 0;
            this.Cur = new Set<text_buffer_cursor_t>();
        }

        private checkValidCursor(cursor: text_buffer_cursor_t): void {
            if ((!this.Cur.has(cursor)) || (cursor.use == false)) {
                throw new Error();
            }
            cursor.CheckValid();
        }

        DeleteCharacatorOnCursor(cursor: text_buffer_cursor_t) {
            if ((!this.Cur.has(cursor)) || (cursor.use == false)) {
                throw new Error();
            }
            if (cursor.Buffer.buffer.length == cursor.Index) {
                // 末尾文字を削除＝後ろの行があれば削除
                if (cursor.Buffer.next != null) {
                    const next = cursor.Buffer.next;
                    cursor.Buffer.buffer.append(next.buffer);
                    this.RemoveLine(next);
                    this.TotalLength--;
                }
            }
            else {
                // カーソルの位置の文字を削除
                if (cursor.Buffer.buffer.deleteAfter(cursor.Index, 1) == false) {
                    return;
                }
                this.TotalLength--;
            }

        }

        private RemoveLine(buf: text_buffer_line_t) {
            if (buf.prev == null && buf.next == null) {
                // １行だけ存在する場合

                // １行目をクリア
                buf.buffer.clear();

                // 行内にあるカーソルを先頭に移動させる
                for (const cur of this.Cur) {
                    if (cur.use == false) { this.Cur.delete(cur); continue; }
                    if (cur.Buffer != buf) { continue; }
                    // 最初の行に対する行削除なので最初の行に設定
                    cur.Index = 0;
                }
            } else {
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
                    if (cur.use == false) { this.Cur.delete(cur); continue; }
                    if (cur.Buffer != buf) { continue; }

                    if (buf.next != null) {
                        //   次の行がある場合：次の行の行頭に移動する
                        cur.Buffer = buf.next;
                        cur.Index = 0;
                    } else if (buf.prev != null) {
                        //   次の行がなく前の行がある場合：前の行の行末に移動する。
                        cur.Buffer = buf.prev;
                        cur.Index = buf.prev.buffer.length;
                    } else {
                        //   どちらもない場合：最初の行に対する行削除なので最初の行に設定
                        cur.Buffer = this.Buffers;
                        cur.Index = 0;
                    }
                }

                // 行情報を破棄
                buf.Dispose();
            }
        }


        DeleteArea(s: text_buffer_cursor_t, len: number): void {
            const start = this.DuplicateCursor(s);

            // １文字づつ消す
            while (len > 0) {
                this.DeleteCharacatorOnCursor(start);
                len--;
            }
            this.DisposeCursor(start);
        }

        TakeCharacatorOnCursor(cursor: text_buffer_cursor_t): number {
            if ((!this.Cur.has(cursor)) || (cursor.use == false)) {
                throw new Error();
            }

            if (cursor.Buffer.buffer.length == cursor.Index) {
                if (cursor.Buffer.next == null) {
                    return 0x00; // 終端なのでヌル文字を返す
                }
                else {
                    return 0x0A;	// 改行文字を返す
                }

            }
            return cursor.Buffer.buffer.get(cursor.Index);

        }

        InsertCharacatorOnCursor(cursor: text_buffer_cursor_t, ch: number): boolean {

            this.checkValidCursor(cursor);
            if (ch == 0x0A)// 0x0A = \n
            {
                // 改行は特別扱い

                // 新しい行バッファを確保
                const nb = new text_buffer_line_t(this);

                // 現在カーソルがあるバッファの後ろに連結
                const buf = cursor.Buffer;
                nb.prev = buf;
                nb.next = buf.next;
                if (buf.next != null) {
                    buf.next.prev = nb;
                }
                buf.next = nb;

                // カーソル位置から行末までを新しい行バッファに移動させる
                const len = cursor.Index;
                const size = buf.buffer.length - len;
                for (var i = 0; i < size; i++) {
                    nb.buffer.insert(i, buf.buffer.get(len + i));
                }
                buf.buffer.deleteAfter(len, size);

                // 移動させた範囲にあるカーソルを新しい位置に変更する
                for (const cur of this.Cur) {
                    if (cur.use == false) { this.Cur.delete(cur); continue; }
                    if (cur === cursor) { continue; }

                    if (cur.Buffer == buf && cur.Index > len) {
                        cur.Buffer = nb;
                        cur.Index -= len;
                    }
                }

                // 行を増やす＝改行挿入なので文字は増えていないが仮想的に1文字使っていることにする
                this.TotalLength++;
            } else {
                cursor.Buffer.buffer.insert(cursor.Index, ch);
                this.TotalLength++;
            }
            return true;
        }

        // カーソル位置の文字を上書きする
        OverwriteCharacatorOnCursor(cursor: text_buffer_cursor_t, ch: number): boolean {
            this.checkValidCursor(cursor);
            if (cursor.Buffer.buffer.length == cursor.Index || ch == 0x0A) {
                this.InsertCharacatorOnCursor(cursor, ch);
            } else {
                this.TotalLength++;
                cursor.Buffer.buffer.set(cursor.Index, ch);
            }
            return true;
        }

        // テキストバッファカーソルを１文字前に移動させる
        // falseはcursorが元々先頭であったことを示す。
        CursorBackward(cursor: text_buffer_cursor_t): boolean {
            this.checkValidCursor(cursor);

            if (cursor.Index > 0) {
                // カーソル位置がバッファ先頭以外の場合
                cursor.Index--;

                this.checkValidCursor(cursor);
                return true;
            }
            else {
                // カーソル位置がバッファ先頭の場合は前の行に動かす
                if (cursor.Buffer.prev != null) {
                    cursor.Buffer = cursor.Buffer.prev;
                    cursor.Index = cursor.Buffer.buffer.length;
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
        CursorForward(cursor: text_buffer_cursor_t): boolean {
            this.checkValidCursor(cursor);

            if (cursor.Index < cursor.Buffer.buffer.length) {
                // カーソル位置がバッファ先頭以外の場合
                cursor.Index++;

                this.checkValidCursor(cursor);
                return true;
            }
            else {
                // カーソル位置がバッファ末尾の場合は次の行に動かす
                if (cursor.Buffer.next != null) {
                    cursor.Buffer = cursor.Buffer.next;
                    cursor.Index = 0;
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
        CompareCursor(c1: text_buffer_cursor_t, c2: text_buffer_cursor_t): number {
            this.checkValidCursor(c1);
            this.checkValidCursor(c2);

            if (c1.Buffer == c2.Buffer) {
                if (c1.Index < c2.Index) { return -1; }
                if (c1.Index == c2.Index) { return 0; }
                if (c1.Index > c2.Index) { return 1; }
            }

            for (let p = this.Buffers; p != null; p = p.next) {
                if (p == c1.Buffer) { return -1; }
                if (p == c2.Buffer) { return 1; }
            }

            throw new Error(); // 不正なカーソルが渡された
        }

        // カーソルが同一位置を示しているか判定
        // CompareCursorより高速
        EqualCursor(c1: text_buffer_cursor_t, c2: text_buffer_cursor_t): boolean {
            this.checkValidCursor(c1);
            this.checkValidCursor(c2);
            if (c1.Buffer == c2.Buffer) {
                if (c1.Index == c2.Index) { return true; }
            }
            return false;
        }

        // テキスト全体の先頭かどうか判定
        StartOfString(c: text_buffer_cursor_t): boolean {
            this.checkValidCursor(c);
            return (c.Buffer.prev == null && c.Index == 0);
        }

        EndOfString(c: text_buffer_cursor_t): boolean {
            this.checkValidCursor(c);
            if (c.Buffer.buffer.length == c.Index && c.Buffer.next == null) { return true; }
            return false;
        }

        // カーソルを行頭に移動
        MoveToBeginningOfLine(cur: text_buffer_cursor_t): void {
            this.checkValidCursor(cur);
            cur.Index = 0;
        }

        MoveToBeginningOfDocument(cur: text_buffer_cursor_t): void {
            this.checkValidCursor(cur);
            cur.Buffer = this.Buffers;
            cur.Index = 0;
        }

        // 区間[start, end)の文字数をカウントする。endは含まれないので注意
        strlen(start: text_buffer_cursor_t, end: text_buffer_cursor_t) {
            this.checkValidCursor(start);
            this.checkValidCursor(end);
            let n = 0;
            const c: text_buffer_cursor_t = this.DuplicateCursor(start);
            for (; ;) {
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

    interface screen_pos_t { X: number; Y: number }

    class TextVRAM {
        private ColorPalette: Uint32Array;
        private Pixels: Uint32Array;
        private BackgroundColor: number;
        private TextColor: number;
        private CursorPtr: number;
        private VWIDTH_X: number;
        private WIDTH_Y: number;
        private font: BMPFont;
        get Width(): number { return this.VWIDTH_X; }
        get Height(): number { return this.WIDTH_Y; }
        constructor(font: BMPFont, width: number, height: number) {
            this.font = font;
            this.VWIDTH_X = width;
            this.WIDTH_Y = height;
            this.ColorPalette = new Uint32Array(16);
            for (let i = 0; i < 8; i++) {
                this.SetPaletteColor(
                    i,
                    (255 * (i >> 2)),
                    (255 * (i & 1)),
                    (255 * ((i >> 1) & 1))
                );
            }
            for (let i = 8; i < 16; i++) {
                //8以上は全て白に初期化
                this.SetPaletteColor(
                    i,
                    255,
                    255,
                    255
                );
            }
            this.Pixels = new Uint32Array(this.VWIDTH_X * this.WIDTH_Y * 2);
            this.BackgroundColor = 0;
            this.TextColor = 7;
            this.CursorPtr = 0;
            this.ClearScreen();
        }
        ClearScreen(): void {
            this.Pixels.fill(0);
        }
        SetPaletteColor(index: number, r: number, g: number, b: number): void {
            const color = (0xFF << 24) | (r << 16) | (g << 8) | (b << 0);
            this.ColorPalette[index & 0x0F] = color;

        }
        GetCursorPtr(): number {
            return this.CursorPtr;
        }
        GetCursorPosition(): screen_pos_t {
            return {
                X: ~~(this.CursorPtr % this.VWIDTH_X),
                Y: ~~(this.CursorPtr / this.VWIDTH_X)
            };

        }
        SetCursorPosition(pos: screen_pos_t): void {
            if (pos.X < 0 || pos.X >= this.VWIDTH_X || pos.Y < 0 || pos.Y >= this.WIDTH_Y) {
                return;
            }
            this.CursorPtr = pos.Y * this.VWIDTH_X + pos.X;
        }
        GetVramPtr(): Uint32Array { return this.Pixels; }
        GetPalettePtr(): Uint32Array { return this.ColorPalette; }
        GetBackgroundColor(): number { return this.BackgroundColor; }
        SetBackgroundColor(color: number): void { this.BackgroundColor = color; }
        FillBackgroundColor(pos: screen_pos_t, length: number, palette: number): void {
            if (pos.X < 0 || pos.X >= this.VWIDTH_X || pos.Y < 0 || pos.Y >= this.WIDTH_Y) {
                return;
            }
            let n = pos.Y * this.VWIDTH_X + pos.X;
            while (length-- > 0) {
                const index = n * 2 + 1;
                this.Pixels[index] = (this.Pixels[index] & 0xFFFFFF0F) | (palette << 4);
                n++;
            }
        }
        GetTextColor(): number { return this.TextColor; }
        SetTextColor(color: number): void { this.TextColor = color; }
        FillTextColor(pos: screen_pos_t, length: number, palette: number): void {
            if (pos.X < 0 || pos.X >= this.VWIDTH_X || pos.Y < 0 || pos.Y >= this.WIDTH_Y) {
                return;
            }
            let n = pos.Y * this.VWIDTH_X + pos.X;
            while (length-- > 0) {
                const index = n * 2 + 1;
                this.Pixels[index] = (this.Pixels[index] & 0xFFFFFFF0) | (palette << 0);
                n++;
            }
        }
        Scroll(): void {
            this.Pixels.copyWithin(0, this.VWIDTH_X * 2, this.Pixels.length - this.VWIDTH_X * 2);
            this.Pixels.fill(0, this.Pixels.length - this.VWIDTH_X * 2, this.Pixels.length);
        }
        putch(n: number): void {
            //カーソル位置にテキストコードnを1文字表示し、カーソルを1文字進める
            //画面最終文字表示してもスクロールせず、次の文字表示時にスクロールする

            let sz = this.font.getWidth(n);

            if (this.CursorPtr < 0 || this.CursorPtr > this.VWIDTH_X * this.WIDTH_Y) {
                // VRAM外への描画
                return;
            }
            if (this.CursorPtr + sz - 1 >= this.VWIDTH_X * this.WIDTH_Y) {
                // 画面末尾での描画
                this.Scroll();
                this.CursorPtr = this.VWIDTH_X * (this.WIDTH_Y - 1);
            }
            if (n == 0x0A) {
                //改行
                this.CursorPtr += this.VWIDTH_X - (this.CursorPtr % this.VWIDTH_X);
            }
            else {
                // 残り空きセル数を取得
                const rest = this.VWIDTH_X - (this.CursorPtr % this.VWIDTH_X);
                if (rest < sz) {
                    // 文字を挿入すると画面端をはみ出す場合、その位置に空白入れて改行したことにする
                    this.Pixels[this.CursorPtr * 2 + 0] = 0;
                    // 文字色と背景色はそのままにしておくほうがいいのかな
                    //this.Pixels[this.CursorPtr*2+1] = (this.Pixels[this.CursorPtr*2+1] & 0xFFFFFFF0) | (this.TextColor << 0);
                    //this.Pixels[this.CursorPtr*2+1] = (this.Pixels[this.CursorPtr*2+1] & 0xFFFFFF0F) | (this.BackgroundColor << 4);
                    this.CursorPtr += this.VWIDTH_X - (this.CursorPtr % this.VWIDTH_X);
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
        puts(str: number[] | Uint32Array): void {
            for (const ch of str) {
                this.putch(ch);
            }

        }
        //カーソル位置に符号なし整数nを10進数表示
        putdigit(n: number): void {
            const n1 = ~~(n / 10);
            let d = 1;
            while (n1 >= d) { d *= 10; }
            while (d != 0) {
                this.putch(0x30 + ~~(n / d));
                n %= d;
                d = ~~(d / 10);
            }

        }
        //カーソル位置に符号なし整数nをe桁の10進数表示（前の空き桁部分はスペースで埋める）
        putdigit2(n: number, e: number): void {
            if (e == 0) {
                return;
            }
            const n1 = ~~(n / 10);
            let d = 1;
            e--;
            while (e > 0 && n1 >= d) { d *= 10; e--; }
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
        CalcCursorPosition(str: Uint32Array, head: number, cur: number, p: screen_pos_t, width: number): void {
            for (; ;) {
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
    const COLOR_NORMALTEXT_BG = 0;	 //通常テキスト背景
    const COLOR_ERRORTEXT = 4; //エラーメッセージテキスト色
    const COLOR_AREASELECTTEXT = 4; //範囲選択テキスト色
    const COLOR_BOTTOMLINE = 5; //画面最下行のテキスト色
    const COLOR_CURSOR = 6; //カーソル色
    const COLOR_BOTTOMLINE_BG = 8; //画面最下行背景

    class TextEditor {
        private textBuffer: TextBuffer = null;
        private font: BMPFont = null;
        private vram: TextVRAM = null;
        private insertMode: boolean = true;

        /**
        * 現在のカーソル位置に対応するテキスト位置
        * [0] は現在位置を示す値
        * [1] はsave_cursorで退避された値
        */
        private Cursor: [text_buffer_cursor_t, text_buffer_cursor_t] = [null, null];

        /**
        * 現在表示中の画面の左上に対応するテキスト位置
        * [0] は現在位置を示す値
        * [1] はsave_cursorで退避された値
        */
        private DisplayLeftTop: [text_buffer_cursor_t, text_buffer_cursor_t] = [null, null];

        /**
        * 画面上でのカーソルの位置
        * [0] は現在位置を示す値
        * [1] はsave_cursorで退避された値
        */
        private CursorScreenPos: [screen_pos_t, screen_pos_t] = [null, null];

        GetCursorScreenPos(): screen_pos_t { return this.CursorScreenPos[0]; }

        //上下移動時の仮カーソルX座標
        private cx2: number = 0;

        // 範囲選択時のカーソルのスタート位置
        private SelectStart: text_buffer_cursor_t = null;

        // 範囲選択時のカーソルのスタート座標（画面上）
        private SelectStartCursorScreenPos: screen_pos_t = null;

        //保存後に変更されたかを表すフラグ
        private edited: boolean = false;

        //ファイルアクセス用バッファ
        private filebuf: string = "";

        //編集中のファイル名
        private CurrentFileName: string = "";

        // 列挙したファイル表示用
        private filenames: string[] = [];

        // クリップボードデータ
        private clipboard: Uint32Array = null;

        private get EDITOR_SCREEN_HEIGHT(): number {
            return this.vram.Height - 1;
        }


        constructor(font: BMPFont, vram: TextVRAM) {
            this.font = font;
            this.vram = vram;

            // テキストバッファの初期化
            this.textBuffer = new TextBuffer();

            // カーソルをリセット
            this.Cursor = [this.textBuffer.AllocateCursor(), null];
            this.DisplayLeftTop = [this.textBuffer.AllocateCursor(), null];
            this.CursorScreenPos = [{ X: 0, Y: 0 }, { X: 0, Y: 0 }];
            this.SelectStart = null;
            this.SelectStartCursorScreenPos = {X:0,Y:0};

            // カーソル位置を先頭に
            this.cursor_top();

            //編集済みフラグクリア
            this.edited = false;
            // 挿入モードに設定
            this.insertMode = true
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
            this.insertMode = true

            // バッファクリア（カーソルも全部補正される）
            this.textBuffer.clear();
        }

        /**
         * 画面上のカーソル位置と再算出した位置情報が一致しているか検査（デバッグ用）
         */
        private ValidateDisplayCursorPos() {
            const p: screen_pos_t = this.calc_screen_pos_from_line_head(this.DisplayLeftTop[0], this.Cursor[0]);
            assert((p.X == this.CursorScreenPos[0].X) && (p.Y == this.CursorScreenPos[0].Y));
        }

        // 画面上の位置と折り返しを考慮に入れながらカーソルと画面上の位置を連動させてひとつ進める
        private iterate_cursor_with_screen_pos(p: screen_pos_t, cur: text_buffer_cursor_t): boolean {
            // １文字先（移動先）を読む
            const c1: number = this.textBuffer.TakeCharacatorOnCursor(cur);
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
            } else if (c1 == 0x09) {
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
            } else if (p.X + w1 + w2 > this.vram.Width) {
                // 一文字目、二文字目両方を受け入れると行末を超える場合
                p.X = 0;
                p.Y++;
            } else {
                p.X += w1;
            }
            return true;
        }

        // headのスクリーン座標を(0,0)とした場合のcurのスクリーン座標位置を算出
        private calc_screen_pos_from_line_head(head: text_buffer_cursor_t, cur: text_buffer_cursor_t): screen_pos_t {
            head.CheckValid();
            cur.CheckValid();

            const p: screen_pos_t = { X: 0, Y: 0 };
            const h: text_buffer_cursor_t = head.Duplicate();
            while (text_buffer_cursor_t.Equal(h, cur) == false) {
                if (this.iterate_cursor_with_screen_pos(p, h) == false) {
                    break;
                }
            }
            h.Dispose();
            return p;
        }

        // 画面原点を基準に現在の行の画面上での最左位置に対応する位置にカーソルを移動
        private move_cursor_to_display_line_head(pos: screen_pos_t, cur: text_buffer_cursor_t): boolean {
            cur.CheckValid();
            const p: screen_pos_t = { X: 0, Y: 0 };
            const c: text_buffer_cursor_t = this.DisplayLeftTop[0].Duplicate();
            const cprev: text_buffer_cursor_t = this.DisplayLeftTop[0].Duplicate();
            // タブや折り返しなどがあるため、単純に逆順に辿ることができない場合もあって場合分けすると面倒くさい
            //画面原点に対応するバッファカーソルは常に計算済みなのでそこからの距離を求める
            let result = false;
            for (; ;) {
                const pprev: screen_pos_t = { X: p.X, Y: p.Y };
                c.CopyTo(cprev);
                if (p.Y == pos.Y && p.X == 0) {
                    pos.X = p.X; pos.Y = p.Y;
                    c.CopyTo(cur);
                    result = true;
                    break;
                }
                else if (this.iterate_cursor_with_screen_pos(p, c) == false) {
                    pos.X = pprev.X; pos.Y = pprev.Y;
                    cprev.CopyTo(cur);
                    result = false;
                    break;
                }
                else if (p.Y == pos.Y) {
                    assert(p.X == 0);
                    pos.X = p.X; pos.Y = p.Y;
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
        move_cursor_to_display_line_tail(pos: screen_pos_t, cur: text_buffer_cursor_t): boolean {
            cur.CheckValid();

            const p: screen_pos_t = { X: pos.X, Y: pos.Y };
            const c: text_buffer_cursor_t = cur.Duplicate();
            const cprev: text_buffer_cursor_t = cur.Duplicate();
            let result: boolean = false;
            for (; ;) {
                const pprev: screen_pos_t = { X: p.X, Y: p.Y };
                c.CopyTo(cprev);
                if (this.iterate_cursor_with_screen_pos(p, c) == false) {
                    pos.X = p.X; pos.Y = p.Y;
                    c.CopyTo(cur);
                    result = false;
                    break;
                }
                else if (p.Y == pos.Y + 1) {
                    pos.X = pprev.X; pos.Y = pprev.Y;
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
        private move_cursor_to_prev(pos: screen_pos_t, cur: text_buffer_cursor_t): boolean {
            cur.CheckValid();
            const p: screen_pos_t = { X: 0, Y: 0 };
            const pprev: screen_pos_t = { X: 0, Y: 0 };
            const c: text_buffer_cursor_t = this.DisplayLeftTop[0].Duplicate();
            const cprev: text_buffer_cursor_t = this.DisplayLeftTop[0].Duplicate();
            while (!(p.Y == pos.Y && p.X == pos.X)) {
                pprev.X = p.X; pprev.Y = p.Y;
                c.CopyTo(cprev);
                assert(this.iterate_cursor_with_screen_pos(p, c));
            }
            pos.X = pprev.X; pos.Y = pprev.Y;
            cprev.CopyTo(cur);
            c.Dispose();
            cprev.Dispose();
            return true;
        }

        private move_cursor_to_display_pos(target: screen_pos_t, rp: screen_pos_t, c: text_buffer_cursor_t) {
            const p: screen_pos_t = { X: 0, Y: 0 };
            const pp: screen_pos_t = { X: 0, Y: 0 };
            this.DisplayLeftTop[0].CopyTo(c);
            const pc: text_buffer_cursor_t = this.DisplayLeftTop[0].Duplicate();
            let ret: boolean = false;
            for (; ;) {
                if (p.Y == target.Y && p.X >= target.X) {
                    rp.X = p.X; rp.Y = p.Y;
                    ret = true;
                    break;
                }
                if (p.Y > target.Y) {
                    rp.X = pp.X; rp.Y = pp.Y;
                    pc.CopyTo(c);
                    ret = true;
                    break;
                }
                pp.X = p.X; pp.Y = p.Y;
                c.CopyTo(pc);
                if (this.iterate_cursor_with_screen_pos(p, c) == false) {
                    // 文書末に到達
                    rp.X = pp.X; rp.Y = pp.Y;
                    pc.CopyTo(c);
                    ret = false;
                    break;
                }
            }
            pc.Dispose();
            return ret;
        }

        // カーソル位置で１文字上書きする
        private OverwriteCharactor(ch: number): boolean {
            if (this.textBuffer.OverwriteCharacatorOnCursor(this.Cursor[0], ch) == false) {
                return false;
            }
            this.CursorScreenPos[0] = this.calc_screen_pos_from_line_head(this.DisplayLeftTop[0], this.Cursor[0]);
            this.ValidateDisplayCursorPos();
            return true;
        }

        // カーソル位置に１文字挿入する
        private InsertCharactor(ch: number): boolean {
            if (this.textBuffer.InsertCharacatorOnCursor(this.Cursor[0], ch) == false) {
                return false;
            }
            this.CursorScreenPos[0] = this.calc_screen_pos_from_line_head(this.DisplayLeftTop[0], this.Cursor[0]);
            this.ValidateDisplayCursorPos();
            return true;
        }

        // 画面を１行下にスクロールする
        private screen_scrolldown(): boolean {
            const c: text_buffer_cursor_t = this.DisplayLeftTop[0].Duplicate();
            const p: screen_pos_t = { X: 0, Y: 0 };
            for (; ;) {
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

        private screen_scrollup(): boolean {
            // カーソルの画面Ｙ座標が最上列なので左上座標の更新が必要
            const lt: text_buffer_cursor_t = this.DisplayLeftTop[0].Duplicate();
            if (this.textBuffer.CursorBackward(lt)) {
                // 左上から１文字戻る＝画面上における前の行の末尾にバッファ上のカーソルを移動

                // 行頭位置を算出
                const c: text_buffer_cursor_t = lt.Duplicate();
                this.textBuffer.MoveToBeginningOfLine(c);

                // 行頭位置を(0,0)点としてカーソルltの画面上位置を計算
                const lp: screen_pos_t = this.calc_screen_pos_from_line_head(c, lt);

                // ltの画面位置を超えない位置の行頭をサーチ
                const p: screen_pos_t = { X: 0, Y: 0 };
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
        private cursor_left_(): void {
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
        cursor_left(): void {
            this.ValidateDisplayCursorPos();
            this.cursor_left_();
            this.ValidateDisplayCursorPos();
        }

        // カーソルを右に移動
        private cursor_right_(): void {
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
        cursor_right(): void {
            this.ValidateDisplayCursorPos();
            this.cursor_right_();
            this.ValidateDisplayCursorPos();
        }

        private cursor_up_(): void {
            if (this.CursorScreenPos[0].Y == 0) {
                if (this.screen_scrollup() == false) {
                    // 画面を上にスクロールできない＝戻れないので終了
                    return;
                }
            }
            this.ValidateDisplayCursorPos();

            // 左上の補正が終った
            // 左上位置から画面上で１行上の位置をサーチする
            const target: screen_pos_t = { X: this.cx2, Y: this.CursorScreenPos[0].Y - 1 };
            const p: screen_pos_t = { X: 0, Y: 0 };
            const c: text_buffer_cursor_t = this.textBuffer.AllocateCursor();

            this.move_cursor_to_display_pos(target, p, c);

            c.CopyTo(this.Cursor[0]);
            this.CursorScreenPos[0] = p;
            c.Dispose();

            this.ValidateDisplayCursorPos();
        }
        cursor_up(): void {
            this.ValidateDisplayCursorPos();
            this.cursor_up_();
            this.ValidateDisplayCursorPos();
        }
        private cursor_down_(): void {
            // 左上位置からサーチする
            const target: screen_pos_t = { X: this.cx2, Y: this.CursorScreenPos[0].Y + 1 };
            const p: screen_pos_t = { X: 0, Y: 0 };
            const c: text_buffer_cursor_t = this.textBuffer.AllocateCursor();

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
        cursor_down(): void {
            this.ValidateDisplayCursorPos();
            this.cursor_down_();
            this.ValidateDisplayCursorPos();
        }

        // カーソルを行頭に移動する
        private cursor_home_(): void {
            this.move_cursor_to_display_line_head(this.CursorScreenPos[0], this.Cursor[0]);
            this.cx2 = 0;
        }

        cursor_home(): void {
            this.ValidateDisplayCursorPos();
            this.cursor_home_();
            this.ValidateDisplayCursorPos();
        }

        // カーソルを行末に移動する
        private cursor_end_(): void {
            this.move_cursor_to_display_line_tail(this.CursorScreenPos[0], this.Cursor[0]);
            this.cx2 = this.CursorScreenPos[0].X;
        }

        cursor_end(): void {
            this.ValidateDisplayCursorPos();
            this.cursor_end_();
            this.ValidateDisplayCursorPos();
        }

        private cursor_pageup_(): void {
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

            let i: number = 0;
            const prev: text_buffer_cursor_t = this.textBuffer.AllocateCursor();
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

        cursor_pageup(): void {
            this.ValidateDisplayCursorPos();
            this.cursor_pageup_();
            this.ValidateDisplayCursorPos();
        }

        private cursor_pagedown_(): void {
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
                    break;// バッファ最下行で移動できなかった場合抜ける
                }
            }

            let i: number = 0;
            const prev: text_buffer_cursor_t = this.textBuffer.AllocateCursor();
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

        cursor_pagedown(): void {
            this.ValidateDisplayCursorPos();
            this.cursor_pagedown_();
            this.ValidateDisplayCursorPos();
        }

        cursor_top(): void {
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
        Clipboard_CopyTo(): void {
            const bp1: text_buffer_cursor_t = this.textBuffer.AllocateCursor();
            const bp2: text_buffer_cursor_t = this.textBuffer.AllocateCursor();

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

            const pd: number[] = [];
            while (this.textBuffer.EqualCursor(bp1, bp2) == false) {
                pd.push(this.textBuffer.TakeCharacatorOnCursor(bp1));
                this.textBuffer.CursorForward(bp1);
            }
            this.clipboard = new Uint32Array(pd);
            bp1.Dispose();
            bp2.Dispose();
        }

        Clipboard_PasteFrom(): void {
            // クリップボードから貼り付け
            for (let i = 0; i < this.clipboard.length; i++) {
                if (this.InsertCharactor(this.clipboard[i]) == false) {
                    break;
                }
                this.cursor_right();//画面上、バッファ上のカーソル位置を1つ後ろに移動
            }

        }


        ///////////////////

        //範囲選択モード開始時のカーソル開始位置グローバル変数設定
        set_areamode(): void {
            if (this.SelectStart != null) {
                this.SelectStart.Dispose();
                this.SelectStart = null;
            }
            this.SelectStart = this.Cursor[0].Duplicate();
            this.SelectStartCursorScreenPos.X = this.CursorScreenPos[0].X;
            this.SelectStartCursorScreenPos.Y = this.CursorScreenPos[0].Y;
        }


        private countarea(): number {
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
        private deletearea(): void {
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

        checkResetSelectArea(): void {
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
        save_cursor(): void {
            assert(this.Cursor[1] == null);
            assert(this.DisplayLeftTop[1] == null);
            this.Cursor[1] = this.Cursor[0].Duplicate();
            this.DisplayLeftTop[1] = this.DisplayLeftTop[0].Duplicate();
            this.CursorScreenPos[1].X = this.CursorScreenPos[0].X;
            this.CursorScreenPos[1].Y = this.CursorScreenPos[0].Y;
        }

        //カーソル関連グローバル変数を一時避難場所から戻す
        restore_cursor(): void {
            assert(this.Cursor[0] != null);
            assert(this.DisplayLeftTop[0] != null);
            assert(this.Cursor[1] != null);
            assert(this.DisplayLeftTop[1] != null);
            this.Cursor[0].Dispose(); this.Cursor[0] = this.Cursor[1]; this.Cursor[1] = null;
            this.DisplayLeftTop[0].Dispose(); this.DisplayLeftTop[0] = this.DisplayLeftTop[1]; this.DisplayLeftTop[1] = null;
            this.CursorScreenPos[0].X = this.CursorScreenPos[1].X; this.CursorScreenPos[1].X = 0;
            this.CursorScreenPos[0].Y = this.CursorScreenPos[1].Y; this.CursorScreenPos[1].Y = 0;
        }

        discard_saved_cursor(): void {
            if (this.Cursor[1] != null) {
                this.Cursor[1].Dispose(); this.Cursor[1] = null;
            }
            if (this.DisplayLeftTop[1] != null) {
                this.DisplayLeftTop[1].Dispose(); this.DisplayLeftTop[1] = null;
            }
            this.CursorScreenPos[1].X = 0;
            this.CursorScreenPos[1].Y = 0;
        }

        ///////////////////

        redraw(): void {
            let cl: number = COLOR_NORMALTEXT;


            let select_start: text_buffer_cursor_t;
            let select_end: text_buffer_cursor_t;

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
            const bp: text_buffer_cursor_t = this.DisplayLeftTop[0].Duplicate();
            const sp: screen_pos_t = { X: 0, Y: 0 };
            while (sp.Y < this.EDITOR_SCREEN_HEIGHT) {
                // 選択範囲の始点/終点に到達してたら色設定変更
                if (this.SelectStart != null) {
                    if (this.textBuffer.EqualCursor(bp, select_start)) { cl = COLOR_AREASELECTTEXT; }
                    if (this.textBuffer.EqualCursor(bp, select_end)) { cl = COLOR_NORMALTEXT; }
                }
                const ch = this.textBuffer.TakeCharacatorOnCursor(bp);
                vp[(sp.Y * this.vram.Width + sp.X) * 2 + 0] = ch;
                vp[(sp.Y * this.vram.Width + sp.X) * 2 + 1] = (vp[(sp.Y * this.vram.Width + sp.X) * 2 + 1] & 0xFFFFFFF0) | cl;
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

        save<T>(start: (context: T) => boolean, write: (context: T, ch: number) => boolean, end: (context: T) => void, context: T): boolean {
            let ret: boolean = false;
            if (start(context)) {
                ret = true;
                const bp: text_buffer_cursor_t = this.textBuffer.AllocateCursor();
                while (this.textBuffer.EndOfString(bp) == false) {
                    const ch = this.textBuffer.TakeCharacatorOnCursor(bp);
                    if (ch == 0x0000) { break; }
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

        load<T>(start: (context: T) => boolean, read: (context: T) => number, end: (context: T) => void, context: T): boolean {
            let ret: boolean = false;
            if (start(context)) {
                ret = true;
                this.clear();
                let ch: number = 0;
                while ((ch = read(context)) != 0) {
                    this.InsertCharactor(ch);
                    this.cursor_right();//画面上、バッファ上のカーソル位置を1つ後ろに移動
                }
            }

            end(context);
            return ret;
        }

        ///////////////////
        private menuStr = Unicode.strToUtf32("F1:LOAD F2:SAVE   F4:NEW ");
        displaybottomline(): void {
            //エディター画面最下行の表示
            const vcp: screen_pos_t = { X: 0, Y: this.vram.Height - 1 };
            this.vram.SetCursorPosition(vcp);
            this.vram.SetTextColor(COLOR_BOTTOMLINE);
            this.vram.SetBackgroundColor(COLOR_BOTTOMLINE_BG);

            this.vram.puts(this.menuStr);
            this.vram.putdigit2(this.textBuffer.getTotalLength(), 5);
            this.vram.FillBackgroundColor({ X: 0, Y: this.vram.Height - 1 }, this.vram.Width, COLOR_BOTTOMLINE_BG);
        }

        ///////////////////
        normal_code_process(k: number): void {
            // 通常文字入力処理
            // k:入力された文字コード

            this.edited = true; //編集済みフラグを設定

            if (this.insertMode || k == 0x0A || this.SelectStart != null) { // 挿入モードの場合
                // 選択範囲を削除
                if (this.SelectStart != null) {
                    this.deletearea();
                }
                if (this.InsertCharactor(k)) {
                    this.cursor_right();//画面上、バッファ上のカーソル位置を1つ後ろに移動
                }
            }
            else { //上書きモード
                if (this.OverwriteCharactor(k)) {
                    this.cursor_right();//画面上、バッファ上のカーソル位置を1つ後ろに移動
                }
            }
        }

        control_code_process(k: VIRTUAL_KEY, sh: CTRLKEY_FLAG): void {
            // 制御文字入力処理
            // k:制御文字の仮想キーコード
            // sh:シフト関連キー状態

            this.save_cursor(); //カーソル関連変数退避（カーソル移動できなかった場合戻すため）

            switch (k) {
                case VIRTUAL_KEY.VKEY_LEFT:
                case VIRTUAL_KEY.VKEY_NUMPAD4:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((sh & CTRLKEY_FLAG.CHK_SHIFT) == 0 || (k == VIRTUAL_KEY.VKEY_NUMPAD4) && (sh & CTRLKEY_FLAG.CHK_NUMLK)) {
                        if (this.SelectStart != null) {
                            this.SelectStart.Dispose();
                            this.SelectStart = null;
                        }
                    }
                    else if (this.SelectStart == null) {
                        this.set_areamode(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    if (sh & CTRLKEY_FLAG.CHK_CTRL) {
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
                case VIRTUAL_KEY.VKEY_RIGHT:
                case VIRTUAL_KEY.VKEY_NUMPAD6:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((sh & CTRLKEY_FLAG.CHK_SHIFT) == 0 || (k == VIRTUAL_KEY.VKEY_NUMPAD6) && (sh & CTRLKEY_FLAG.CHK_NUMLK)) {
                        if (this.SelectStart != null) {
                            this.SelectStart.Dispose();
                            this.SelectStart = null;
                        }
                    }
                    else if (this.SelectStart == null) {
                        this.set_areamode(); //範囲選択モードでなければ範囲選択モード開始
                    }
                    if (sh & CTRLKEY_FLAG.CHK_CTRL) {
                        //CTRL＋右矢印でEnd
                        this.cursor_end();
                        break;
                    }
                    this.cursor_right();
                    if (this.SelectStart != null && (this.textBuffer.EqualCursor(this.DisplayLeftTop[0], this.DisplayLeftTop[1]) == false)) {
                        //範囲選択モードで画面スクロールがあった場合
                        if (this.SelectStartCursorScreenPos.Y > 0) this.SelectStartCursorScreenPos.Y--; //範囲スタート位置もスクロール
                        else this.restore_cursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                    }
                    break;
                case VIRTUAL_KEY.VKEY_UP:
                case VIRTUAL_KEY.VKEY_NUMPAD8:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((sh & CTRLKEY_FLAG.CHK_SHIFT) == 0 || (k == VIRTUAL_KEY.VKEY_NUMPAD8) && (sh & CTRLKEY_FLAG.CHK_NUMLK)) {
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
                        } else {
                            this.restore_cursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                        }
                    }
                    break;
                case VIRTUAL_KEY.VKEY_DOWN:
                case VIRTUAL_KEY.VKEY_NUMPAD2:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((sh & CTRLKEY_FLAG.CHK_SHIFT) == 0 || (k == VIRTUAL_KEY.VKEY_NUMPAD2) && (sh & CTRLKEY_FLAG.CHK_NUMLK)) {
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
                        if (this.SelectStartCursorScreenPos.Y > 0) this.SelectStartCursorScreenPos.Y--; //範囲スタート位置もスクロール
                        else this.restore_cursor(); //カーソル位置を戻す（画面範囲外の範囲選択禁止）
                    }
                    break;
                case VIRTUAL_KEY.VKEY_HOME:
                case VIRTUAL_KEY.VKEY_NUMPAD7:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((sh & CTRLKEY_FLAG.CHK_SHIFT) == 0 || (k == VIRTUAL_KEY.VKEY_NUMPAD7) && (sh & CTRLKEY_FLAG.CHK_NUMLK)) {
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
                case VIRTUAL_KEY.VKEY_END:
                case VIRTUAL_KEY.VKEY_NUMPAD1:
                    //シフトキー押下していなければ範囲選択モード解除（NumLock＋シフト＋テンキーでも解除）
                    if ((sh & CTRLKEY_FLAG.CHK_SHIFT) == 0 || (k == VIRTUAL_KEY.VKEY_NUMPAD1) && (sh & CTRLKEY_FLAG.CHK_NUMLK)) {
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
                case VIRTUAL_KEY.VKEY_PRIOR: // PageUpキー
                case VIRTUAL_KEY.VKEY_NUMPAD9:
                    //シフト＋PageUpは無効（NumLock＋シフト＋「9」除く）
                    if ((sh & CTRLKEY_FLAG.CHK_SHIFT) && ((k != VIRTUAL_KEY.VKEY_NUMPAD9) || ((sh & CTRLKEY_FLAG.CHK_NUMLK) == 0))) {
                        break;
                    }
                    //範囲選択モード解除
                    if (this.SelectStart != null) {
                        this.SelectStart.Dispose();
                        this.SelectStart = null;
                    }

                    this.cursor_pageup();
                    break;
                case VIRTUAL_KEY.VKEY_NEXT: // PageDownキー
                case VIRTUAL_KEY.VKEY_NUMPAD3:
                    //シフト＋PageDownは無効（NumLock＋シフト＋「3」除く）
                    if ((sh & CTRLKEY_FLAG.CHK_SHIFT) && ((k != VIRTUAL_KEY.VKEY_NUMPAD3) || ((sh & CTRLKEY_FLAG.CHK_NUMLK) == 0))) {
                        break;
                    }
                    //範囲選択モード解除
                    if (this.SelectStart != null) {
                        this.SelectStart.Dispose();
                        this.SelectStart = null;
                    }

                    this.cursor_pagedown();
                    break;
                case VIRTUAL_KEY.VKEY_DELETE: //Deleteキー
                case VIRTUAL_KEY.VKEY_DECIMAL: //テンキーの「.」
                    this.edited = true; //編集済みフラグ
                    if (this.SelectStart != null) {
                        this.deletearea();//選択範囲を削除
                    }
                    else {
                        this.textBuffer.DeleteCharacatorOnCursor(this.Cursor[0]);
                        this.CursorScreenPos[0] = this.calc_screen_pos_from_line_head(this.DisplayLeftTop[0], this.Cursor[0]);
                    }
                    break;
                case VIRTUAL_KEY.VKEY_BACK: //BackSpaceキー
                    this.edited = true; //編集済みフラグ
                    if (this.SelectStart != null) {
                        this.deletearea();//選択範囲を削除
                        break;
                    }
                    if (this.textBuffer.StartOfString(this.Cursor[0])) {
                        break; //バッファ先頭では無視
                    }
                    this.cursor_left();
                    this.textBuffer.DeleteCharacatorOnCursor(this.Cursor[0]);
                    this.CursorScreenPos[0] = this.calc_screen_pos_from_line_head(this.DisplayLeftTop[0], this.Cursor[0]);
                    break;
                case VIRTUAL_KEY.VKEY_INSERT:
                case VIRTUAL_KEY.VKEY_NUMPAD0:
                    this.insertMode = !this.insertMode; //挿入モード、上書きモードを切り替え
                    break;
                case VIRTUAL_KEY.VKEY_KEY_C:
                    //CTRL+C、クリップボードにコピー
                    if (this.SelectStart != null && (sh & CTRLKEY_FLAG.CHK_CTRL)) {
                        this.Clipboard_CopyTo();
                    }
                    break;
                case VIRTUAL_KEY.VKEY_KEY_X:
                    //CTRL+X、クリップボードに切り取り
                    if (this.SelectStart != null && (sh & CTRLKEY_FLAG.CHK_CTRL)) {
                        this.Clipboard_CopyTo();
                        this.deletearea(); //選択範囲の削除
                        this.edited = true; //編集済みフラグ
                    }
                    break;
                case VIRTUAL_KEY.VKEY_KEY_V:
                    //CTRL+V、クリップボードから貼り付け
                    if ((sh & CTRLKEY_FLAG.CHK_CTRL) == 0) break;
                    if (this.clipboard == null || this.clipboard.length == 0) break;
                    this.edited = true; //編集済みフラグ
                    if (this.SelectStart != null) {
                        //範囲選択している時は削除してから貼り付け
                        this.deletearea();//選択範囲を削除
                        this.Clipboard_PasteFrom();//クリップボード貼り付け
                    }
                    else {
                        this.Clipboard_PasteFrom();//クリップボード貼り付け
                    }
                    break;
                case VIRTUAL_KEY.VKEY_KEY_S:
                    //CTRL+S、SDカードに保存
                    if ((sh & CTRLKEY_FLAG.CHK_CTRL) == 0) break;
                case VIRTUAL_KEY.VKEY_F2: //F2キー
                    //this.save_as(); //ファイル名を付けて保存
                    break;
                case VIRTUAL_KEY.VKEY_KEY_O:
                    //CTRL+O、ファイル読み込み
                    if ((sh & CTRLKEY_FLAG.CHK_CTRL) == 0) break;
                case VIRTUAL_KEY.VKEY_F1: //F1キー
                    //F1キー、ファイル読み込み
                    //this.selectfile();	//ファイルを選択して読み込み
                    break;
                case VIRTUAL_KEY.VKEY_KEY_N:
                    //CTRL+N、新規作成
                    if ((sh & CTRLKEY_FLAG.CHK_CTRL) == 0) break;
                case VIRTUAL_KEY.VKEY_F4: //F4キー
                    //this.newtext(); //新規作成
                    break;
            }
            this.discard_saved_cursor();
        }

    }


    window
        .whenEvent<Event>('load')
        .then<BMPFont>(() => BMPFont.loadFont("font.bmpf"))
        .then((bmpFont) => {
            const docStr = document.getElementById('demo').innerHTML;
            const canvas = document.getElementById("canvas") as HTMLCanvasElement;
            const context = canvas.getContext("2d");
            const imageData = context.createImageData(canvas.width, canvas.height);
            imageData.data32 = new Uint32Array(imageData.data.buffer);
            const textVram = new TextVRAM(bmpFont, ~~(canvas.width / bmpFont.FontWidth), ~~(canvas.height / bmpFont.FontHeight));
            textVram.SetPaletteColor(COLOR_BOTTOMLINE_BG, 0x00, 0x20, 0x80);

            const textEditor = new TextEditor(bmpFont, textVram);
            const keyboard = new Keyboard();
            /*
            textEditor.load(
                (s) => true,
                (s) => s.str.length > s.index ? s.str[s.index++] : 0,
                (s) => true,
                { str: Unicode.strToUtf32(docStr), index: 0 }
            );
            */

            window.addEventListener('keydown', (e) => {
                if (e.repeat == false) {
                    keyboard.PushKeyStatus(e.keyCode, false);
                }
                e.stopPropagation();
                e.preventDefault();
            });
            window.addEventListener('keyup', (e) => {
                keyboard.PushKeyStatus(e.keyCode, true);
                e.stopPropagation();
                e.preventDefault();
            });

            function loop() {
                textEditor.redraw();
                textEditor.displaybottomline();
                textVram.SetCursorPosition(textEditor.GetCursorScreenPos());
                textVram.SetTextColor(COLOR_NORMALTEXT);
                textVram.SetBackgroundColor(COLOR_NORMALTEXT_BG);

                while (keyboard.ReadKey() && keyboard.Keyboard_GetCurrentVKeyCode()) {
                    let k1 = keyboard.Keyboard_GetCurrentAsciiCode();
                    const k2 = keyboard.Keyboard_GetCurrentVKeyCode();
                    const sh = keyboard.GetCurrentCtrlKeys();             //sh:シフト関連キー状態
                    //Enter押下は単純に改行文字を入力とする
                    if (k2 == VIRTUAL_KEY.VKEY_RETURN || k2 == VIRTUAL_KEY.VKEY_SEPARATOR) {
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
                    for (let y = 0; y < textVram.Height; y++) {
                        for (let x = 0; x < textVram.Width; x++) {
                            const ch = vramPtr[(y * textVram.Width + x) * 2 + 0];
                            const color = (vramPtr[(y * textVram.Width + x) * 2 + 1] >> 0) & 0x0F;
                            const bgColor = (vramPtr[(y * textVram.Width + x) * 2 + 1] >> 4) & 0x0F;
                            const fontWidth = bmpFont.getPixelWidth(ch);
                            const fontHeight = bmpFont.FontHeight;
                            const fontPixel = bmpFont.getPixel(ch);
                            const left = x * bmpFont.FontWidth;
                            const top = y * bmpFont.FontHeight;
                            const size = bmpFont.FontHeight;
                            if (x == cursorPos.X && y == cursorPos.Y) {
                                imageData.fillRect(left, top, fontWidth != 0 ? fontWidth : bmpFont.FontWidth, fontHeight, palPtr[COLOR_CURSOR]);
                            } else {
                                imageData.fillRect(left, top, fontWidth, fontHeight, palPtr[bgColor]);
                            }
                            if (fontPixel !== undefined) {
                                for (let j = 0; j < size; j++) {
                                    for (let i = 0; i < size; i++) {
                                        if (fontPixel.getBit(j * size + i)) {
                                            imageData.setPixel(left + i, top + j, palPtr[color]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    context.putImageData(imageData, 0, 0);
                }
                window.requestAnimationFrame(loop);
            }
            window.requestAnimationFrame(loop);



        });

}

interface Uint8Array {
    getBit(n: number): number;
    setBit(n: number, value: number): void;
}
Uint8Array.prototype.getBit = function (n: number) {
    return this[n >> 3] & (0x80 >> (n & 0x07));
}
Uint8Array.prototype.setBit = function (n: number, value: number) {
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
}
ImageData.prototype.setPixel = function (x: number, y: number, color: number): void {
    const self: ImageData = this;

    if (x < 0) { return; }
    if (x >= self.width) { return; }
    if (y < 0) { return; }
    if (y >= self.height) { return; }

    self.data32[(y * self.width + x)] = color;
    //self.data[(y * self.width + x) * 4 + 0] = (color >> 24) & 0xFF;
    //self.data[(y * self.width + x) * 4 + 1] = (color >> 16) & 0xFF;
    //self.data[(y * self.width + x) * 4 + 2] = (color >> 8) & 0xFF;
    //self.data[(y * self.width + x) * 4 + 3] = (color >> 0) & 0xFF;

}

ImageData.prototype.fillRect = function (x: number, y: number, w: number, h: number, color: number): void {
    const self: ImageData = this;
    if (x < 0) { w += x; x = 0; }
    if (x + w > self.width) { w = self.width - x; }
    if (w <= 0) { return; }
    if (y < 0) { h += y; y = 0; }
    if (y + h > self.height) { h = self.height - y; }
    if (h <= 0) { return; }

    for (let j = y; j < y + h; j++) {
        const start = j * self.width + x;
        const end = start + w;
        self.data32.fill(color, start, end);
        //for (let i = x; i < x + w; i++) {
        //    self.data[(j * self.width + i) * 4 + 0] = (color >> 24) & 0xFF;
        //    self.data[(j * self.width + i) * 4 + 1] = (color >> 16) & 0xFF;
        //    self.data[(j * self.width + i) * 4 + 2] = (color >> 8) & 0xFF;
        //    self.data[(j * self.width + i) * 4 + 3] = (color >> 0) & 0xFF;
        //}
    }
}

