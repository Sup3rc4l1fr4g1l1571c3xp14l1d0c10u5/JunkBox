window.addEventListener("load", function () {
    "use strict";

    // utility
    function times(step: number): number[] {
        return [...Array(step).keys()];
    }

    const Font = {
        rom: [
            [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],    /* Ascii 0 */
            [0x3c, 0x42, 0xa5, 0x81, 0xbd, 0x42, 0x3c, 0x00],    /* Ascii 1 */
            [0x3c, 0x7e, 0xdb, 0xff, 0xc3, 0x7e, 0x3c, 0x00],    /* Ascii 2 */
            [0x00, 0xee, 0xfe, 0xfe, 0x7c, 0x38, 0x10, 0x00],    /* Ascii 3 */
            [0x10, 0x38, 0x7c, 0xfe, 0x7c, 0x38, 0x10, 0x00],    /* Ascii 4 */
            [0x00, 0x3c, 0x18, 0xff, 0xff, 0x08, 0x18, 0x00],    /* Ascii 5 */
            [0x10, 0x38, 0x7c, 0xfe, 0xfe, 0x10, 0x38, 0x00],    /* Ascii 6 */
            [0x00, 0x00, 0x18, 0x3c, 0x18, 0x00, 0x00, 0x00],    /* Ascii 7 */
            [0xff, 0xff, 0xe7, 0xc3, 0xe7, 0xff, 0xff, 0xff],    /* Ascii 8 */
            [0x00, 0x3c, 0x42, 0x81, 0x81, 0x42, 0x3c, 0x00],    /* Ascii 9 */
            [0xff, 0xc3, 0xbd, 0x7e, 0x7e, 0xbd, 0xc3, 0xff],    /* Ascii 10 */
            [0x1f, 0x07, 0x0d, 0x7c, 0xc6, 0xc6, 0x7c, 0x00],    /* Ascii 11 */
            [0x00, 0x7e, 0xc3, 0xc3, 0x7e, 0x18, 0x7e, 0x18],    /* Ascii 12 */
            [0x04, 0x06, 0x07, 0x04, 0x04, 0xfc, 0xf8, 0x00],    /* Ascii 13 */
            [0x0c, 0x0a, 0x0d, 0x0b, 0xf9, 0xf9, 0x1f, 0x1f],    /* Ascii 14 */
            [0x00, 0x92, 0x7c, 0x44, 0xc6, 0x7c, 0x92, 0x00],    /* Ascii 15 */
            [0x00, 0x00, 0x60, 0x78, 0x7e, 0x78, 0x60, 0x00],    /* Ascii 16 */
            [0x00, 0x00, 0x06, 0x1e, 0x7e, 0x1e, 0x06, 0x00],    /* Ascii 17 */
            [0x18, 0x7e, 0x18, 0x18, 0x18, 0x18, 0x7e, 0x18],    /* Ascii 18 */
            [0x66, 0x66, 0x66, 0x66, 0x66, 0x00, 0x66, 0x00],    /* Ascii 19 */
            [0xff, 0xb6, 0x76, 0x36, 0x36, 0x36, 0x36, 0x00],    /* Ascii 20 */
            [0x7e, 0xc1, 0xdc, 0x22, 0x22, 0x1f, 0x83, 0x7e],    /* Ascii 21 */
            [0x00, 0x00, 0x00, 0x7e, 0x7e, 0x00, 0x00, 0x00],    /* Ascii 22 */
            [0x18, 0x7e, 0x18, 0x18, 0x7e, 0x18, 0x00, 0xff],    /* Ascii 23 */
            [0x18, 0x7e, 0x18, 0x18, 0x18, 0x18, 0x18, 0x00],    /* Ascii 24 */
            [0x18, 0x18, 0x18, 0x18, 0x18, 0x7e, 0x18, 0x00],    /* Ascii 25 */
            [0x00, 0x04, 0x06, 0xff, 0x06, 0x04, 0x00, 0x00],    /* Ascii 26 */
            [0x00, 0x20, 0x60, 0xff, 0x60, 0x20, 0x00, 0x00],    /* Ascii 27 */
            [0x00, 0x00, 0x00, 0xc0, 0xc0, 0xc0, 0xff, 0x00],    /* Ascii 28 */
            [0x00, 0x24, 0x66, 0xff, 0x66, 0x24, 0x00, 0x00],    /* Ascii 29 */
            [0x00, 0x00, 0x10, 0x38, 0x7c, 0xfe, 0x00, 0x00],    /* Ascii 30 */
            [0x00, 0x00, 0x00, 0xfe, 0x7c, 0x38, 0x10, 0x00],    /* Ascii 31 */
            [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],    /*   */
            [0x30, 0x30, 0x30, 0x30, 0x30, 0x00, 0x30, 0x00],    /* ! */
            [0x66, 0x66, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00],    /* " */
            [0x6c, 0x6c, 0xfe, 0x6c, 0xfe, 0x6c, 0x6c, 0x00],    /* # */
            [0x10, 0x7c, 0xd2, 0x7c, 0x86, 0x7c, 0x10, 0x00],    /* $ */
            [0xf0, 0x96, 0xfc, 0x18, 0x3e, 0x72, 0xde, 0x00],    /* % */
            [0x30, 0x48, 0x30, 0x78, 0xce, 0xcc, 0x78, 0x00],    /* & */
            [0x0c, 0x0c, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00],    /* ' */
            [0x10, 0x60, 0xc0, 0xc0, 0xc0, 0x60, 0x10, 0x00],    /* ( */
            [0x10, 0x0c, 0x06, 0x06, 0x06, 0x0c, 0x10, 0x00],    /* ) */
            [0x00, 0x54, 0x38, 0xfe, 0x38, 0x54, 0x00, 0x00],    /* * */
            [0x00, 0x18, 0x18, 0x7e, 0x18, 0x18, 0x00, 0x00],    /* + */
            [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x70],    /* , */
            [0x00, 0x00, 0x00, 0x7e, 0x00, 0x00, 0x00, 0x00],    /* - */
            [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x18, 0x00],    /* . */
            [0x02, 0x06, 0x0c, 0x18, 0x30, 0x60, 0xc0, 0x00],    /* / */
            [0x7c, 0xc6, 0xc6, 0xc6, 0xc6, 0xc6, 0x7c, 0x00],    /* 0 */
            [0x18, 0x38, 0x78, 0x18, 0x18, 0x18, 0x3c, 0x00],    /* 1 */
            [0x7c, 0xc6, 0x06, 0x0c, 0x30, 0x60, 0xfe, 0x00],    /* 2 */
            [0x7c, 0xc6, 0x06, 0x3c, 0x06, 0xc6, 0x7c, 0x00],    /* 3 */
            [0x0e, 0x1e, 0x36, 0x66, 0xfe, 0x06, 0x06, 0x00],    /* 4 */
            [0xfe, 0xc0, 0xc0, 0xfc, 0x06, 0x06, 0xfc, 0x00],    /* 5 */
            [0x7c, 0xc6, 0xc0, 0xfc, 0xc6, 0xc6, 0x7c, 0x00],    /* 6 */
            [0xfe, 0x06, 0x0c, 0x18, 0x30, 0x60, 0x60, 0x00],    /* 7 */
            [0x7c, 0xc6, 0xc6, 0x7c, 0xc6, 0xc6, 0x7c, 0x00],    /* 8 */
            [0x7c, 0xc6, 0xc6, 0x7e, 0x06, 0xc6, 0x7c, 0x00],    /* 9 */
            [0x00, 0x30, 0x00, 0x00, 0x00, 0x30, 0x00, 0x00],    /* : */
            [0x00, 0x30, 0x00, 0x00, 0x00, 0x30, 0x20, 0x00],    /* ; */
            [0x00, 0x1c, 0x30, 0x60, 0x30, 0x1c, 0x00, 0x00],    /* < */
            [0x00, 0x00, 0x7e, 0x00, 0x7e, 0x00, 0x00, 0x00],    /* = */
            [0x00, 0x70, 0x18, 0x0c, 0x18, 0x70, 0x00, 0x00],    /* > */
            [0x7c, 0xc6, 0x0c, 0x18, 0x30, 0x00, 0x30, 0x00],    /* ? */
            [0x7c, 0x82, 0x9a, 0xaa, 0xaa, 0x9e, 0x7c, 0x00],    /* @ */
            [0x38, 0x6c, 0xc6, 0xc6, 0xfe, 0xc6, 0xc6, 0x00],    /* A */
            [0xfc, 0xc6, 0xc6, 0xfc, 0xc6, 0xc6, 0xfc, 0x00],    /* B */
            [0x7c, 0xc6, 0xc6, 0xc0, 0xc0, 0xc6, 0x7c, 0x00],    /* C */
            [0xf8, 0xcc, 0xc6, 0xc6, 0xc6, 0xcc, 0xf8, 0x00],    /* D */
            [0xfe, 0xc0, 0xc0, 0xfc, 0xc0, 0xc0, 0xfe, 0x00],    /* E */
            [0xfe, 0xc0, 0xc0, 0xfc, 0xc0, 0xc0, 0xc0, 0x00],    /* F */
            [0x7c, 0xc6, 0xc0, 0xce, 0xc6, 0xc6, 0x7e, 0x00],    /* G */
            [0xc6, 0xc6, 0xc6, 0xfe, 0xc6, 0xc6, 0xc6, 0x00],    /* H */
            [0x78, 0x30, 0x30, 0x30, 0x30, 0x30, 0x78, 0x00],    /* I */
            [0x1e, 0x06, 0x06, 0x06, 0xc6, 0xc6, 0x7c, 0x00],    /* J */
            [0xc6, 0xcc, 0xd8, 0xf0, 0xd8, 0xcc, 0xc6, 0x00],    /* K */
            [0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xc0, 0xfe, 0x00],    /* L */
            [0xc6, 0xee, 0xfe, 0xd6, 0xc6, 0xc6, 0xc6, 0x00],    /* M */
            [0xc6, 0xe6, 0xf6, 0xde, 0xce, 0xc6, 0xc6, 0x00],    /* N */
            [0x7c, 0xc6, 0xc6, 0xc6, 0xc6, 0xc6, 0x7c, 0x00],    /* O */
            [0xfc, 0xc6, 0xc6, 0xfc, 0xc0, 0xc0, 0xc0, 0x00],    /* P */
            [0x7c, 0xc6, 0xc6, 0xc6, 0xc6, 0xc6, 0x7c, 0x06],    /* Q */
            [0xfc, 0xc6, 0xc6, 0xfc, 0xc6, 0xc6, 0xc6, 0x00],    /* R */
            [0x78, 0xcc, 0x60, 0x30, 0x18, 0xcc, 0x78, 0x00],    /* S */
            [0xfc, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x00],    /* T */
            [0xc6, 0xc6, 0xc6, 0xc6, 0xc6, 0xc6, 0x7c, 0x00],    /* U */
            [0xc6, 0xc6, 0xc6, 0xc6, 0xc6, 0x6c, 0x38, 0x00],    /* V */
            [0xc6, 0xc6, 0xc6, 0xd6, 0xfe, 0xee, 0xc6, 0x00],    /* W */
            [0xc6, 0xc6, 0x6c, 0x38, 0x6c, 0xc6, 0xc6, 0x00],    /* X */
            [0xc3, 0xc3, 0x66, 0x3c, 0x18, 0x18, 0x18, 0x00],    /* Y */
            [0xfe, 0x0c, 0x18, 0x30, 0x60, 0xc0, 0xfe, 0x00],    /* Z */
            [0x3c, 0x30, 0x30, 0x30, 0x30, 0x30, 0x3c, 0x00],    /* [ */
            [0xc0, 0x60, 0x30, 0x18, 0x0c, 0x06, 0x03, 0x00],    /* \ */
            [0x3c, 0x0c, 0x0c, 0x0c, 0x0c, 0x0c, 0x3c, 0x00],    /* ] */
            [0x00, 0x38, 0x6c, 0xc6, 0x00, 0x00, 0x00, 0x00],    /* ^ */
            [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xff],    /* _ */
            [0x30, 0x30, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00],    /* ` */
            [0x00, 0x00, 0x7c, 0x06, 0x7e, 0xc6, 0x7e, 0x00],    /* a */
            [0xc0, 0xc0, 0xfc, 0xc6, 0xc6, 0xe6, 0xdc, 0x00],    /* b */
            [0x00, 0x00, 0x7c, 0xc6, 0xc0, 0xc0, 0x7e, 0x00],    /* c */
            [0x06, 0x06, 0x7e, 0xc6, 0xc6, 0xce, 0x76, 0x00],    /* d */
            [0x00, 0x00, 0x7c, 0xc6, 0xfe, 0xc0, 0x7e, 0x00],    /* e */
            [0x1e, 0x30, 0x7c, 0x30, 0x30, 0x30, 0x30, 0x00],    /* f */
            [0x00, 0x00, 0x7e, 0xc6, 0xce, 0x76, 0x06, 0x7c],    /* g */
            [0xc0, 0xc0, 0xfc, 0xc6, 0xc6, 0xc6, 0xc6, 0x00],    /* h */
            [0x18, 0x00, 0x38, 0x18, 0x18, 0x18, 0x3c, 0x00],    /* i */
            [0x18, 0x00, 0x38, 0x18, 0x18, 0x18, 0x18, 0xf0],    /* j */
            [0xc0, 0xc0, 0xcc, 0xd8, 0xf0, 0xd8, 0xcc, 0x00],    /* k */
            [0x38, 0x18, 0x18, 0x18, 0x18, 0x18, 0x3c, 0x00],    /* l */
            [0x00, 0x00, 0xcc, 0xfe, 0xd6, 0xc6, 0xc6, 0x00],    /* m */
            [0x00, 0x00, 0xfc, 0xc6, 0xc6, 0xc6, 0xc6, 0x00],    /* n */
            [0x00, 0x00, 0x7c, 0xc6, 0xc6, 0xc6, 0x7c, 0x00],    /* o */
            [0x00, 0x00, 0xfc, 0xc6, 0xc6, 0xe6, 0xdc, 0xc0],    /* p */
            [0x00, 0x00, 0x7e, 0xc6, 0xc6, 0xce, 0x76, 0x06],    /* q */
            [0x00, 0x00, 0x6e, 0x70, 0x60, 0x60, 0x60, 0x00],    /* r */
            [0x00, 0x00, 0x7c, 0xc0, 0x7c, 0x06, 0xfc, 0x00],    /* s */
            [0x30, 0x30, 0x7c, 0x30, 0x30, 0x30, 0x1c, 0x00],    /* t */
            [0x00, 0x00, 0xc6, 0xc6, 0xc6, 0xc6, 0x7e, 0x00],    /* u */
            [0x00, 0x00, 0xc6, 0xc6, 0xc6, 0x6c, 0x38, 0x00],    /* v */
            [0x00, 0x00, 0xc6, 0xc6, 0xd6, 0xfe, 0x6c, 0x00],    /* w */
            [0x00, 0x00, 0xc6, 0x6c, 0x38, 0x6c, 0xc6, 0x00],    /* x */
            [0x00, 0x00, 0xc6, 0xc6, 0xce, 0x76, 0x06, 0x7c],    /* y */
            [0x00, 0x00, 0xfc, 0x18, 0x30, 0x60, 0xfc, 0x00],    /* z */
            [0x0e, 0x18, 0x18, 0x70, 0x18, 0x18, 0x0e, 0x00],    /* { */
            [0x18, 0x18, 0x18, 0x00, 0x18, 0x18, 0x18, 0x00],    /* | */
            [0xe0, 0x30, 0x30, 0x1c, 0x30, 0x30, 0xe0, 0x00],    /* } */
            [0x00, 0x00, 0x70, 0x9a, 0x0e, 0x00, 0x00, 0x00],    /* ~ */
            [0x00, 0x00, 0x18, 0x3c, 0x66, 0xff, 0x00, 0x00]     /* Ascii 127 */
        ]
    };

    interface IButtonStatus {
        Hold: number;
        Period: number;
        Timer: number;
        Status: boolean;
        PrevStatus: boolean;
        Result: boolean;
    }

    const _btn_status: IButtonStatus[] = times(32).map(() => { return { Hold: 0, Period: 0, Timer: 0, Status: false, PrevStatus: false, Result: false }; });

    function _btn_set(id: number, status: boolean): void {
        _btn_status[id].Status = status;
    }

    function _btn_result(id: number): boolean {
        return _btn_status[id].Result;
    }

    function _btns_update(): void {
        for (let i = 0; i < _btn_status.length; i++) {
            const btn = _btn_status[i];
            if (btn.Status) {
                if (btn.PrevStatus == false) {
                    btn.Timer = btn.Hold;
                    btn.Result = true;
                } else {
                    if (btn.Timer == 0) {
                        btn.Result = true;
                        btn.Timer = btn.Period;
                    } else {
                        btn.Result = false;
                        btn.Timer -= 1;
                    }
                }
            } else {
                btn.Result = false;
            }
            btn.PrevStatus = btn.Status;
            poke1(gamepads, i, btn.PrevStatus ? 1 : 0);
        }
    }

    function api_btn(id: number): boolean {
        const btn = _btn_status[id & 0x1F];
        return btn.PrevStatus;
    }

    function api_btnp(id: number, hold: number = 0, period: number = 0): boolean {
        const _hold = ~~hold;
        const _period = ~~period;
        const btn = _btn_status[id & 0x1F];
        if (btn.Hold != _hold || btn.Period != _period) {
            btn.Hold = _hold;
            btn.Period = _period;
            btn.Timer = 0;
            btn.PrevStatus = btn.Status = btn.Result = false;
            return false;
        } else {
            return btn.Result;
        }
    }

    // PICO-8 : 128x128 =  8192 byte
    // TIC-80 : 240x136 = 16320 byte
    // NDS    : 256x192 = 24576 byte
    // NES    : 256x240 = 30720 byte
    // STM32F7 DISCO : 408x272
    const SCREEN_WIDTH = 240;
    const SCREEN_HEIGHT = 136;
    const SPRITE_SIZE = 8;

    // 240x136 cells, 1920x1088 pixels (240*8 x 136*8)
    const MAP_WIDTH = (~~(SCREEN_WIDTH / SPRITE_SIZE)) * SPRITE_SIZE;
    const MAP_HEIGHT = (~~(SCREEN_HEIGHT / SPRITE_SIZE)) * SPRITE_SIZE;

    const SCREEN = 0x00000;
    const SCREEN_SIZE = 0x03FC0;
    const PALETTE = 0x03FC0;
    const PALETTE_SIZE = 0x00030;
    const PALETTE_MAP = 0x03FF0;
    const PALETTE_MAP_SIZE = 0x00008;
    const BORDER = 0x03FF8;
    const BORDER_SIZE = 0x00001;
    const SCREEN_OFFSET = 0x03FF9;
    const SCREEN_OFFSET_SIZE = 0x00002;
    const MOUSE_CURSOR = 0x03FFB;
    const MOUSE_CURSOR_SIZE = 0x00001;
    const RESERVED1 = 0x03FFC;
    const RESERVED1_SIZE = 0x00004;
    const TILES = 0x04000;
    const TILES_SIZE = 0x02000;
    const SPRITES = 0x06000;
    const SPRITES_SIZE = 0x02000;
    const MAP = 0x08000;
    const MAP_SIZE = 0x07F80;
    const GAMEPADS = 0x0FF80;
    const GAMEPADS_SIZE = 0x00004;
    const MOUSE = 0x0FF84;
    const MOUSE_SIZE = 0x00004;

    const ram = new Uint8Array(1024 * 80);	// 80kb
    const screen = ram.subarray(SCREEN, SCREEN + SCREEN_SIZE);
    const palette = ram.subarray(PALETTE, PALETTE + PALETTE_SIZE);
    const palette_map = ram.subarray(PALETTE_MAP, PALETTE_MAP + PALETTE_MAP_SIZE);
    const border = ram.subarray(BORDER, BORDER + BORDER_SIZE);
    const screen_offset = ram.subarray(SCREEN_OFFSET, SCREEN_OFFSET + SCREEN_OFFSET_SIZE);
    const mouse_cursor = ram.subarray(MOUSE_CURSOR, MOUSE_CURSOR + MOUSE_CURSOR_SIZE);
    const tiles = ram.subarray(TILES, TILES + TILES_SIZE);
    const sprites = ram.subarray(SPRITES, SPRITES + SPRITES_SIZE);
    const map = ram.subarray(MAP, MAP + MAP_SIZE);
    const gamepads = ram.subarray(GAMEPADS, GAMEPADS + GAMEPADS_SIZE);
    const mouse = ram.subarray(MOUSE, MOUSE + MOUSE_SIZE);

    function poke(address: number, value: number): void {
        ram[~~address] = value;
    }

    function peek(address: number): number {
        return ram[~~address];
    }

    function poke4(ram: Uint8Array, index: number, value: number): void {
        const address = (index >> 1);
        if (index & 1) {
            ram[address] = (ram[address] & 0x0F) | ((value & 0x0F) << 4);
        } else {
            ram[address] = (ram[address] & 0xF0) | ((value & 0x0F) << 0);
        }
    }

    function peek4(ram: Uint8Array, index: number): number {
        const address = (index >> 1);
        if (index & 1) {
            return (ram[address] & 0xF0) >> 4;
        } else {
            return (ram[address] & 0x0F) >> 0;
        }
    }

    function api_poke4(index: number, value: number): void {
        poke4(ram, index, value);
    }

    function api_peek4(index: number): number {
        return peek4(ram, index);
    }

    function poke1(ram: Uint8Array, index: number, value: number): void {
        const address = (index >> 3);
        const bit = (7 - index & 0x07);
        ram[address] = (ram[address] & (~(1 << bit))) | ((value & 0x01) << bit);
    }

    function peek1(ram: Uint8Array, index: number): number {
        const address = (index >> 3);
        const bit = (7 - index & 0x07);
        return (ram[address] >> bit) & 0x01;
    }

    function api_memcpy(toaddr: number, fromaddr: number, len: number): void {
        if (len <= 0) {
            return;
        }
        ram.copyWithin(toaddr, fromaddr, fromaddr + len);
    }

    function api_memset(addr: number, val: number, len: number): void {
        if (len <= 0) {
            return;
        }
        ram.fill(val, addr, addr + len);
    }

    function set_palette_map(mapid: number, palid: number): void {
        poke4(palette_map, (mapid & 0x0F), (palid & 0x0F));
    }

    function get_palette_map(mapid: number): number {
        return peek4(palette_map, (mapid & 0x0F));
    }

    function set_palette(palid: number, rgb: number): void {
        const address = (palid & 0x0F) * 3;
        palette[address + 0] = (rgb >> 16) & 0xFF;
        palette[address + 1] = (rgb >> 8) & 0xFF;
        palette[address + 2] = (rgb >> 0) & 0xFF;
    }

    function get_palette(palid: number): number {
        const address = (palid & 0x0F) * 3;
        return (palette[address + 0] << 16) | (palette[address + 1] << 8) | palette[address + 2];
    }

    function set_pixel(x: number, y: number, color: number): void {
        if (clip.t <= y && y < clip.b && clip.l <= x && x < clip.r) {
            poke4(screen, ~~y * SCREEN_WIDTH + ~~x, color & 0x0F);
        }
    }

    function get_pixel(x: number, y: number): number {
        if (0 <= y && y < SCREEN_HEIGHT && 0 <= x && x < SCREEN_WIDTH) {
            return peek4(screen, ~~y * SCREEN_WIDTH + ~~x);
        } else {
            return 0;
        }
    }

    function api_pix(x: number, y: number, color?: number): number {
        if (color == undefined) {
            return get_pixel(x, y);
        } else {
            set_pixel(x, y, color);
            return 0;
        }
    }

    function api_cls(color: number = 0): void {
        if (clip.l == 0 && clip.t == 0 && clip.r == SCREEN_WIDTH && clip.b == SCREEN_HEIGHT) {
            color = (color & 0x0F);
            const byte = (color << 4) | color;
            screen.fill(byte);
        } else {
            api_rect(clip.l, clip.t, clip.r - clip.l, clip.b - clip.t, color);
        }
    }

    function api_rect(x: number, y: number, w: number, h: number, color: number): void {
        x = ~~x; y = ~~y; w = ~~w; h = ~~h; color = ~~color;
        for (let j = y; j < y + h; j++) {
            _lineH(x, j, w, color);
        }
    }

    function api_rect_border(x: number, y: number, w: number, h: number, color: number): void {
        x = ~~x; y = ~~y; w = ~~w; h = ~~h; color = ~~color;
        _lineH(x, y, w, color);
        _lineH(x, y + h - 1, w, color);
        _lineV(x, y, h, color);
        _lineV(x + w - 1, y, h, color);
    }

    function api_line(x0: number, y0: number, x1: number, y1: number, color: number): void {
        _line(x0, y0, x1, y1, (x, y) => set_pixel(x, y, color));
    }

    function _lineH(x: number, y: number, w: number, color: number): void {
        if (y < clip.t || y >= clip.b) { return; }
        const final_color = get_palette_map(color);
        const xl = Math.max(x, clip.l);
        const xr = Math.min(x + w, clip.r);
        for (let i = xl; i < xr; i++) {
            poke4(screen, y * SCREEN_WIDTH + i, color);
        }
    }

    function _lineV(x: number, y: number, h: number, color: number): void {
        if (x < clip.l || x >= clip.r) { return; }
        const yl = y < clip.t ? clip.t : y;
        const yr = y + h >= clip.b ? clip.b : y + h;

        for (let i = yl; i < yr; ++i) {
            poke4(screen, i * SCREEN_WIDTH + x, color);
        }
    }

    function _drawTile(ram: Uint8Array, id: number, x: number, y: number, colorkey: number = -1, scale: number = 1, flip: number = 0, rotate: number = 0): void {
        id = ~~id; x = ~~x; y = ~~y; colorkey = ~~colorkey; scale = ~~scale;

        let orientation = flip & 0x03;
        switch (rotate & 0x03) {
            case 0: break;
            case 1: orientation ^= 0x01; orientation |= 0x04; break;
            case 2: orientation ^= 0x03; break;
            case 3: orientation ^= 0x02; orientation |= 0x04; break;
        }

        for (let py = 0; py < 8; py++ , y += scale) {
            let xx = x;
            for (let px = 0; px < 8; px++ , xx += scale) {
                const ix = (orientation & 0x01) ? (8 - px - 1) : px;
                const iy = (orientation & 0x02) ? (8 - py - 1) : py;
                const i = (orientation & 0x04) ? (ix * 8 + iy) : (iy * 8 + ix);
                const pixel = peek4(ram, 64 * id + i);
                if (pixel != colorkey) {
                    api_rect(xx, y, scale, scale, pixel);
                }
            }
        }
    }
    function api_spr(id: number, x: number, y: number, colorkey: number = -1, scale: number = 1, flip: number = 0, rotate: number = 0): void {
        _drawTile(sprites, id, x, y, colorkey, scale, flip, rotate);
    }

    function _line(x0: number, y0: number, x1: number, y1: number, putPixelHandler: (x: number, y: number) => void): void {
        x0 = ~~x0; x1 = ~~x1; y0 = ~~y0; y1 = ~~y1;

        const dx = Math.abs(x1 - x0);
        const sx = x0 < x1 ? 1 : -1;
        const dy = Math.abs(y1 - y0);
        const sy = y0 < y1 ? 1 : -1;
        let err = (dx > dy ? dx : -dy) >> 1;

        for (; ;) {
            putPixelHandler(x0, y0);
            if (x0 == x1 && y0 == y1) { break; }
            const e2 = err;
            if (e2 > -dx) { err -= dy; x0 += sx; }
            if (e2 < dy) { err += dx; y0 += sy; }
        }
    }

    const clip: {
        l: number,
        r: number,
        t: number,
        b: number,
    } = {
            l: 0,
            r: SCREEN_WIDTH,
            t: 0,
            b: SCREEN_HEIGHT,
        };

    function api_clip(x: number = 0, y: number = 0, w: number = SCREEN_WIDTH, h: number = SCREEN_HEIGHT): void {

        clip.l = ~~x;
        clip.t = ~~y;
        clip.r = ~~x + ~~w;
        clip.b = ~~y + ~~h;

        if (clip.l < 0) { clip.l = 0; }
        if (clip.t < 0) { clip.t = 0; }
        if (clip.r > SCREEN_WIDTH) { clip.r = SCREEN_WIDTH; }
        if (clip.b > SCREEN_HEIGHT) { clip.b = SCREEN_HEIGHT; }
    }

    function api_tri(x1: number, y1: number, x2: number, y2: number, x3: number, y3: number, color: number): void {
        x1 = ~~x1; y1 = ~~y1; x2 = ~~x2; y2 = ~~y2; color = ~~color;

        _initSidesBuffer();
        _line(x1, y1, x2, y2, _setSidePixel);
        _line(x2, y2, x3, y3, _setSidePixel);
        _line(x3, y3, x1, y1, _setSidePixel);
        const final_color = get_palette_map(color);
        const yt = Math.max(clip.t, Math.min(y1, Math.min(y2, y3)));
        const yb = Math.min(clip.b, Math.max(y1, Math.max(y2, y3)) + 1);

        for (let y = yt; y < yb; y++) {
            const xl = Math.max(_sideBuffer[y].Left, clip.l);
            const xr = Math.min(_sideBuffer[y].Right + 1, clip.r);
            _lineH(xl, y, xr - xl, final_color);
        }
    }

    interface ISideBuffer {
        Left: number;
        Right: number;
    }

    const _sideBuffer: ISideBuffer[] = times(SCREEN_HEIGHT).map(() => { return { Left: 0, Right: 0 }; });

    function _initSidesBuffer(): void {
        for (let i = 0; i < SCREEN_HEIGHT; i++) {
            _sideBuffer[i].Left = SCREEN_WIDTH;
            _sideBuffer[i].Right = -1;
        }
    }

    function _setSidePixel(x: number, y: number): void {
        if (y >= 0 && y < SCREEN_HEIGHT) {
            if (x < _sideBuffer[y].Left) { _sideBuffer[y].Left = x; }
            if (x > _sideBuffer[y].Right) { _sideBuffer[y].Right = x; }
        }
    }

    function api_circle(xm: number, ym: number, radius: number, color: number): void {
        xm = ~~xm; ym = ~~ym; radius = ~~radius; color = ~~color;

        let r = radius;
        let x = -r;
        let y = 0;
        let err = 2 - 2 * r;

        _initSidesBuffer();
        do {
            _setSidePixel(xm - x, ym + y);
            _setSidePixel(xm - y, ym - x);
            _setSidePixel(xm + x, ym - y);
            _setSidePixel(xm + y, ym + x);

            r = err;
            if (r <= y) { err += ++y * 2 + 1; }
            if (r > x || err > y) { err += ++x * 2 + 1; }
        } while (x < 0);

        const yt = Math.max(ym - radius, clip.t);
        const yb = Math.min(ym + radius + 1, clip.b);
        for (let y = yt; y < yb; y++) {
            const xl = Math.max(_sideBuffer[y].Left, clip.l);
            const xr = Math.min(_sideBuffer[y].Right + 1, clip.r);
            _lineH(xl, y, xr - xl, color);
        }
    }

    function api_circle_border(xm: number, ym: number, radius: number, color: number): void {
        xm = ~~xm; ym = ~~ym; radius = ~~radius; color = ~~color;

        let r = radius;
        let x = -r;
        let y = 0;
        let err = 2 - 2 * r;

        do {
            set_pixel(xm - x, ym + y, color);
            set_pixel(xm - y, ym - x, color);
            set_pixel(xm + x, ym - y, color);
            set_pixel(xm + y, ym + x, color);

            r = err;
            if (r <= y) { err += ++y * 2 + 1; }
            if (r > x || err > y) { err += ++x * 2 + 1; }
        } while (x < 0);
    }

    function api_print(text: string, x: number = 0, y: number = 0, color: number = 15, fixed: boolean = false, scale: number = 1): number {
        x = ~~x; y = ~~y; color = ~~color; scale = ~~scale;

        let max_x = x;
        const sx = x;
        for (let i = 0; i < text.length; i++) {
            const code = text.charCodeAt(i);
            if (code == 0x10) {
                max_x = Math.max(max_x, x);
                x = sx;
                y += 8 * scale;
            } else {
                const image = Font.rom[code & 0x7F];
                for (let j = 0; j < image.length; j++) {
                    let scanline = image[j];
                    for (let i = 0; i < 8; i++) {
                        if (scanline & 0x80) { api_rect(x + i * scale, y + j * scale, scale, scale, color); }
                        scanline <<= 1;
                    }
                }
                x += 8 * scale;
            }
        }
        return Math.max(max_x, x) - sx;

    }

    function api_font(text: string, x: number, y: number, colorkey: number, char_width: number, char_height: number, fixed: boolean = false, scale: number = 1): number {
        x = ~~x; y = ~~y; colorkey = ~~colorkey; char_width = ~~char_width; char_height = ~~char_height; scale = ~~scale;

        let max_x = x;
        const sx = x;
        for (let i = 0; i < text.length; i++) {
            const code = text.charCodeAt(i);
            if (code == 0x0A) { // 0x0A = '\n'
                max_x = Math.max(max_x, x);
                x = sx;
                y += char_height * scale;
            } else {
                api_spr(code, x, y, colorkey, scale, 0, 0);
                x += 8 * scale;
            }
        }
        return Math.max(max_x, x) - sx;
    }

    function api_rand(...args: number[]): number {
        if (args.length == 0) {
            return Math.random();
        }
        if (args.length == 1 && typeof (args[0]) == "number") {
            if (Number.isInteger(args[0])) {
                return ~~(Math.random() * args[0]);
            } else {
                return Math.random() * args[0];
            }
        }
        if (args.length == 2 && typeof (args[0]) == "number" && typeof (args[1]) == "number") {
            if (Number.isInteger(args[0]) && Number.isInteger(args[1])) {
                return ~~(Math.random() * (args[1] - args[0])) + args[0];
            } else {
                return Math.random() * (args[1] - args[0]) + args[0];
            }
        }
        throw new Error();
    }

    function api_map(x: number = 0, y: number = 0, w: number = 30, h: number = 17, sx: number = 0, sy: number = 0, colorkey: number = -1, scale: number = 1, remap: (id: number, x: number, y: number) => [/*id*/number, /*flip*/number, /*rotate*/number] = null) {
        scale = ~~scale;
        x = ~~x;
        y = ~~y;
        h = ~~h;
        w = ~~w;
        const size = 8 * scale;

        for (let j = y, jj = sy; j < y + h; j++ , jj += size) {
            for (let i = x, ii = sx; i < x + w; i++ , ii += size) {
                let mi = i;
                let mj = j;

                while (mi < 0) { mi += MAP_WIDTH; }
                while (mj < 0) { mj += MAP_HEIGHT; }
                while (mi >= MAP_WIDTH) { mi -= MAP_WIDTH; }
                while (mj >= MAP_HEIGHT) { mj -= MAP_HEIGHT; }

                const index = mi + mj * MAP_WIDTH;
                const data = map[index];
                let tile: [/*id*/number, /*flip*/number, /*rotate*/number] = remap ? remap(data, mi, mj) : [/*id*/data, /*flip*/0, /*rotate*/0];

                _drawTile(tiles, tile[0], ii, jj, colorkey, scale, tile[1], tile[2]);
            }
        }
    }

    function api_map_set(x: number, y: number, value: number): void {
        if (x < 0 || x >= MAP_WIDTH || y < 0 || y >= MAP_HEIGHT) {
            return;
        }

        map[~~y * MAP_WIDTH + ~~x] = value;
    }

    function api_map_get(x: number, y: number): number {
        if (x < 0 || x >= MAP_WIDTH || y < 0 || y >= MAP_HEIGHT) {
            return;
        }

        return map[~~y * MAP_WIDTH + ~~x];
    }

    interface IEvalContext {
        // Special Functions
        TIC: () => void;
        scanline: (line: number) => void;

        // Functions

        print: (text: string, x?: number, y?: number, color?: number, fixed?: boolean, scale?: number) => number; // Print string with system font
        font: (text: string, x: number, y: number, colorkey: number, char_width: number, char_height: number, fixed?: boolean, scale?: number) => number; // Print string with font defined in foreground sprites
        clip: (x?: number, y?: number, w?: number, h?: number) => void; // Set screen clipping region
        cls: (color?: number) => void; // Clear the screen
        pix: (...args: number[]) => number; // Set/Get pixel color on the screen
        line: (x0: number, y0: number, x1: number, y1: number, color: number) => void; // Draw line
        rect: (x: number, y: number, w: number, h: number, color: number) => void; // Draw filled rectangle
        rectb: (x: number, y: number, w: number, h: number, color: number) => void; // Draw rectangle border
        circ: (xm: number, ym: number, radius: number, color: number) => void; // Draw filled circle
        circb: (xm: number, ym: number, radius: number, color: number) => void; // Draw circle border
        spr: (id: number, x: number, y: number, colorkey?: number, scale?: number, flip?: number, rotate?: number) => void; // Draw sprite by ID, can rotate or flip
        btn: (id: number) => boolean; // Get gamepad button state in current frame
        btnp: (id: number, hold?: number, period?: number) => boolean; // Get gamepad button state according to previous frame
        sfx: any; // Play SFX by ID on specific channel
        key: any; // Get keybaord button state in current frame
        keyp: any; // Get keyboard button state according to previous frame
        map: (x?: number, y?: number, w?: number, h?: number, sx?: number, sy?: number, colorkey?: number, scale?: number, remap?: (id: number, x: number, y: number) => [/*id*/number, /*flip*/number, /*rotate*/number]) => void; // Draw map region on the screen
        mget: (x: number, y: number) => number; // Get map tile index
        mset: (x: number, y: number, value: number) => void; // Set map tile index
        music: any; // Play music track by ID
        peek: (address: number) => number; // Read a byte value from RAM
        poke: (address: number, value: number) => void; // Write a byte value to RAM
        peek4: (address: number) => number; // Read a half byte value from RAM
        poke4: (address: number, value: number) => void; // Write a half byte value to RAM
        reset: () => void; // Reset game to initial state (0.60)
        memcpy: (toaddr: number, fromaddr: number, len: number) => void; // Copy bytes in RAM ( name is memcpy but behavior is memmove )
        memset: (addr: number, val: number, len: number) => void; // Set byte values in RAM
        pmem: any; // Save integer value to persistent memory
        trace: any; // Trace string to the Console
        time: () => number; // Returns how many ticks passed from game started
        mouse: any; // Get XY and press state of mouse/touch
        sync: any; // Copy modified sprites/map to the cartridge
        tri: (x1: number, y1: number, x2: number, y2: number, x3: number, y3: number, color: number) => void; // Draw filled triangle
        textri: any; // Draw triangle filled with texture
        exit: () => void; // Interrupt program and return to console

        // math functions
        pi: number;
        abs: (v: number) => number;
        cos: (v: number) => number;
        sin: (v: number) => number;
        tan: (v: number) => number;
        floor: (v: number) => number;
        rand: (...args: number[]) => number;
    };


    function createEvalContext(): IEvalContext {
        return {
            TIC: function () { },
            scanline: function (line) { },

            print: api_print,
            font: api_font,
            clip: api_clip,
            cls: api_cls,
            pix: api_pix,
            line: api_line,
            rect: api_rect,
            rectb: api_rect_border,
            circ: api_circle,
            circb: api_circle_border,
            spr: api_spr,
            btn: api_btn,
            btnp: api_btnp,
            sfx: null,
            key: null,
            keyp: null,
            map: api_map,
            mget: api_map_get,
            mset: api_map_set,
            music: null,
            peek: peek,
            poke: poke,
            peek4: api_peek4,
            poke4: api_poke4,
            reset: reset,
            memcpy: api_memcpy,
            memset: api_memset,
            pmem: null,
            trace: null,
            time: api_time,
            mouse: null,
            sync: null,
            tri: api_tri,
            textri: null,
            exit: null,

            pi: Math.PI,
            abs: Math.abs,
            cos: Math.cos,
            sin: Math.sin,
            tan: Math.tan,
            floor: Math.floor,
            rand: api_rand,
        };
    };

    let evalContext: IEvalContext = null;
    let tic = 0;

    function reset(): void {
        ram.fill(0);
        api_clip(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);
        set_palette(0, 0x140C1C);	// Black
        set_palette(1, 0x442434);	// Dark Red
        set_palette(2, 0x30346D);	// Dark Blue
        set_palette(3, 0x4E4A4F);	// Dark Gray
        set_palette(4, 0x854C30);	// Brown
        set_palette(5, 0x346524);	// Dark Green
        set_palette(6, 0xD04648);	// Red
        set_palette(7, 0x757161);	// Light Gray
        set_palette(8, 0x597DCE);	// Light Blue
        set_palette(9, 0xD27D2C);	// Orange
        set_palette(10, 0x8595A1);	// Blue/Gray
        set_palette(11, 0x6DAA2C);	// Light Green
        set_palette(12, 0xD2AA99);	// Peach
        set_palette(13, 0x6DC2CA);	// Cyan
        set_palette(14, 0xDAD45E);	// Yellow
        set_palette(15, 0xDEEED6);	// White

        for (let i = 0; i < 16; i++) {
            set_palette_map(i, i);
        }
        tic = 0;

        // copy font to sprite 
        for (let i = 0; i < Font.rom.length; i++) {
            for (let j = 0; j < 8; j++) {
                const line = Font.rom[i][j];
                for (let k = 0; k < 8; k++) {
                    poke4(tiles, i * 64 + j * 8 + k, (line & (0x80 >> k)) ? 0x0F : 0x00);
                }
            }
        }

        evalContext = createEvalContext();
    }

    const _canvas: HTMLCanvasElement = <HTMLCanvasElement>document.getElementById("glcanvas");
    const _context: CanvasRenderingContext2D = _canvas.getContext("2d");
    const _backbuffer: ImageData = _context.createImageData(SCREEN_WIDTH, SCREEN_HEIGHT);

    function _flip(): void {
        let i = 0;
        for (let y = 0; y < SCREEN_HEIGHT; y++) {
            evalContext.scanline(y);
            for (let x = 0; x < SCREEN_WIDTH; x++) {
                const pixel = peek4(screen, i);
                const palette_no = peek4(palette_map, pixel);
                const palette_address = palette_no * 3;
                const color_r = palette[palette_address + 0];
                const color_g = palette[palette_address + 1];
                const color_b = palette[palette_address + 2];
                const backbuffer_address = i * 4;
                _backbuffer.data[backbuffer_address + 0] = color_r;
                _backbuffer.data[backbuffer_address + 1] = color_g;
                _backbuffer.data[backbuffer_address + 2] = color_b;
                _backbuffer.data[backbuffer_address + 3] = 0xFF;
                i++;
            }
        }
        _context.putImageData(_backbuffer, 0, 0);
    }


    function animationFrameFunc(): void {
        _btns_update();
        evalContext.TIC();
        _flip();
        tic++;
        window.requestAnimationFrame(animationFrameFunc);
    }

    function api_time(): number {
        return tic;
    }

    function evalInContext(scr: string, context: Object) {
        return (new Function("with(this) { " + scr + " }")).call(context);
    }

    reset();
    animationFrameFunc();


    const KeyMap: { [key: string]: number } = {
        "ArrowUp": 0,
        "ArrowDown": 1,
        "ArrowLeft": 2,
        "ArrowRight": 3,
        "z": 4,
        "x": 5,
        "a": 6,
        "s": 7,
    };


    window.addEventListener("keydown", (e) => {
        const key_id = KeyMap[e.key];
        if (key_id != undefined) {
            _btn_set(key_id, true);
        }
    });
    window.addEventListener("keyup", (e) => {
        const key_id = KeyMap[e.key];
        if (key_id != undefined) {
            _btn_set(key_id, false);
        }
    });

    // demo loader 

    const domDemoSelect = <HTMLSelectElement>document.getElementById('demos');
    const domCodeArea = <HTMLTextAreaElement>document.getElementById('code')
    const domDemoScriptTags = Array.from(document.getElementsByTagName("script")).filter(x => x.type == "text/tic80");
    const demos = domDemoScriptTags.forEach((x) => {
        const opt = document.createElement("option");
        opt.value = x.id;
        opt.innerText = x.dataset["name"];
        domDemoSelect.appendChild(opt);
    });
    domDemoSelect.addEventListener("change", () => {
        const val = domDemoSelect.value;
        domCodeArea.value = document.getElementById(val).innerHTML;
    });
    document.getElementById('run').addEventListener("click", () => {
        reset();
        evalInContext(domCodeArea.value, evalContext);
    });
});
module Uare {
    const _mouse: boolean[] = [];
    export function mouse_is_down(id: number) {
        return _mouse[id];
    }
    export function mouse_down(id: number, state:boolean) {
        return _mouse[id] = state;
    }

    let context: CanvasRenderingContext2D;

    export class Uare {
        drawContent: (self: Uare, alpha: number) => void;
        text: any;
        border: any;
        icon: any;
        static elements: Uare[] = [];
        static z: number = 1;
        static hz: number = null;
        static holdt: any = { obj: null };
        static c: boolean = undefined;

        static withinBounds(x: number, y: number, x1: number, y1: number, x2: number, y2: number): boolean {
            return x > x1 && x < x2 && y > y1 && y < y2;
        }
        static lerp(a: number, b: number, k: number): number {
            if (a == b) {
                return a;
            } else {
                if (Math.abs(a - b) < 0.005) {
                    return b;
                } else {
                    return a * (1 - k) + b * k;
                }
            }
        }


        x: number;
        y: number;
        width: number;
        height: number;
        hold: boolean;
        holdColor: any;
        hover: boolean;
        hoverColor: any;
        color: any;
        click: boolean;
        active: boolean;
        drag: { enabled: boolean, fixed: { x: number, y: number }, bounds: { x: number, y: number }[] };
        visible: boolean;
        vAlpha: number;
        center: boolean;
        track: any;
        content: { scroll: { x: number, y: number }, width: number, height: number, wrap: boolean };
        type: string;
        z: number;
        l: number;
        elements:Uare[];

        onClick: () => void;
        onCleanRelease: () => void;
        onRelease: () => void;
        onHold: () => void;
        onStartHover: () => void;
        onHover: () => void;
        onReleaseHover: () => void;

        constructor(t: string | any = {}, f?: any) {

            if (f == undefined) { f = t; t = "button"; }

            this.x = 0;
            this.y = 0;
            this.width = 0;
            this.height = 0;
            this.hover = false;
            this.hold = null;
            this.click = false;
            this.active = true;
            this.drag = { enabled: false, fixed: null, bounds: null };
            this.visible = true;
            this.vAlpha = 1;
            this.center = false;
            this.content = { scroll: { x: 0, y: 0 }, width: undefined, height: undefined, wrap: undefined };
            this.l = 0.2;
            Object.assign(this, f);
            if (this.content.width == undefined) {
                this.content.width = this.width;
            }
            if (this.content.height == undefined) {
                this.content.height = this.height;
            }

            this.type = t;
            this.z = Uare.z;

            Uare.z = Uare.z + 1;
            Uare.hz = Uare.z;

            Uare.elements.push(this);

            return this;
        }
        static newButton(f?: any) {
            return new Uare("button", f);
        }
        static newStyle(f?: any) {
            return f;
        }
        static newIcon(f?: any) {
            return f;
        }
        static newGroup() {
            return new Uare("group", { elements: {} });
        }

        updateSelf(dt: number, mx: number, my: number, e: string) {

            let alphaTarget = this.visible ? 1 : 0;
            if (this.vAlpha != alphaTarget) {
                this.vAlpha = Uare.lerp(this.vAlpha, alphaTarget, this.l);
            }

            let mlc = (e != "s") && mouse_is_down(1);

            let rwb = Uare.withinBounds(mx, my, this.x, this.y, this.x + this.width, this.y + this.height);

            if (this.center) {
                rwb = Uare.withinBounds(mx, my, this.x - this.width * 0.5, this.y - this.height * 0.5, this.x + this.width * 0.5, this.y + this.height * 0.5);
            }

            let wb = e != "s" && rwb;

            let thover = this.hover;
            let thold = this.hold;

            this.hover = wb || (this.drag.enabled && Uare.holdt && Uare.holdt.obj == this);

            this.hold = (e == "c" && wb) || (mlc && this.hold) || ((wb && e != "r" && this.hold));

            if (e == "c" && wb && this.onClick) {
                this.onClick();
            } else if ((e == "r" && wb && thold) && this.onCleanRelease) {
                this.onCleanRelease();
            } else if (((e == "r" && wb && thold) || (this.hold && !wb)) && this.onRelease) {
                this.onRelease();
            } else if (this.hold && this.onHold) {
                this.onHold();
            } else if (!thover && this.hover && this.onStartHover) {
                this.onStartHover();
            } else if (this.hover && this.onHover) {
                this.onHover();
            } else if (thover && !this.hover && this.onReleaseHover) {
                this.onReleaseHover();
            }

            if (this.hold && (!wb || this.drag.enabled) && !Uare.holdt) {
                this.hold = this.drag.enabled;
                Uare.holdt = { obj: this, d: { x: this.x - mx, y: this.y - my } };
            } else if (!this.hold && wb && (Uare.holdt && Uare.holdt.obj == this)) {
                this.hold = true;
                Uare.holdt = null;
            }

            if (Uare.holdt && Uare.holdt.obj == this && this.drag.enabled) {
                this.x = (!this.drag.fixed || !this.drag.fixed.x) ? mx + Uare.holdt.d.x : this.x;
                this.y = (!this.drag.fixed || !this.drag.fixed.y) ? my + Uare.holdt.d.y : this.y;
                if (this.drag.bounds) {
                    if (this.drag.bounds[0]) {
                        this.x = (this.drag.bounds[0].x && this.x < this.drag.bounds[0].x) ? this.drag.bounds[0].x : this.x;
                        this.y = (this.drag.bounds[0].y && this.y < this.drag.bounds[0].y) ? this.drag.bounds[0].y : this.y;
                    }
                    if (this.drag.bounds[1]) {
                        this.x = (this.drag.bounds[1].x && this.x > this.drag.bounds[1].x) ? this.drag.bounds[1].x : this.x;
                        this.y = (this.drag.bounds[1].y && this.y > this.drag.bounds[1].y) ? this.drag.bounds[1].y : this.y;
                    }
                }
                if (this.track) {
                    this.anchor(this.track.ref);
                }
            }

            return wb;

        }

        updateTrack(dt: number) {
            if (this.track) {
                this.x = this.track.ref.x + this.track.d.x;
                this.y = this.track.ref.y + this.track.d.y;
            }
        }


        drawSelf() {

            let tempX = this.x;
            let tempY = this.y;
            if (this.center) {
                tempX = this.x - this.width * .5;
                tempY = this.y - this.height * .5;
            }

            context.fillStyle = this.alphaColor((this.hold && this.holdColor) ? this.holdColor : (this.hover && this.hoverColor) ? this.hoverColor : this.color);
            context.fillRect(tempX, tempY, this.width, this.height);

            if (this.border && this.border.color && this.border.size) {
                context.strokeStyle = this.alphaColor((this.hold && this.holdColor) ? this.holdColor : (this.hover && this.hoverColor) ? this.hoverColor : this.border.color);
                context.lineWidth = this.border.size;
                context.strokeRect(tempX, tempY, this.width, this.height);
            }

            if (this.icon && this.icon.source.type && this.icon.source.content) {
                context.strokeStyle = this.alphaColor((this.hold && this.holdColor) ? this.holdColor : (this.hover && this.hoverColor) ? this.hoverColor : this.icon.hoverColor ? this.icon.hoverColor : this.icon.color);
                context.save();
                let offset = this.icon.offset || { x: 0, y: 0 };
                context.translate((tempX + (this.center ? 0 : this.width * .5) + offset.x), (tempY + (this.center ? 0 : this.height * 0.5) + offset.y));

                if (this.icon.source.type == "polygon") {
                    for (let i = 0; i < this.icon.source.content.length; i++) {
                        context.beginPath();
                        for (let j = 0; j < this.icon.source.content[i].length; j++) {
                            const c: any = this.icon.source.content[j];
                            if (j == 0) {
                                context.moveTo(c.x, c.y);
                            } else {
                                context.lineTo(c.x, c.y);
                            }
                        }
                        context.closePath();
                    }
                } else if (this.icon.source.type == "image") {
                    context.drawImage(this.icon.source.content, 0, 0);
                }
                context.restore();
            }

            if (this.text && this.text.display && this.text.color) {
                context.fillStyle = this.alphaColor((this.hold && this.text.holdColor) ? this.text.holdColor : (this.hover && this.text.hoverColor) ? this.text.hoverColor : this.text.color);
                context.font = this.text.font;
                let offset = this.text.offset || { x: 0, y: 0 };
                context.fillText(
                    this.text.display,
                    this.x - (this.center ? this.width * 0.5 : 0) + offset.x,
                    this.y + (this.center ? 0 : this.height * 0.5) + offset.y, this.width);//, this.text.align);
            }

            if (this.content && this.drawContent) {
                this.renderContent();
            }

        }

        renderContent() {
            context.save();
            let tx = this.x;
            let ty = this.y;
            if (this.center) { tx = this.x - this.width * .5; ty = this.y - this.height * .5; }
            context.translate(tx - this.content.scroll.x * (this.content.width - this.width), ty - this.content.scroll.y * (this.content.height - this.height));
            if (this.content && this.content.wrap) {
                context.rect(tx, ty, this.width, this.height);
                context.clip();
            }
            this.drawContent(this, this.vAlpha * 255);
            if (this.content && this.content.wrap) {
                //context.clip();
            }
            context.restore();
        }

        setContent(f: (self: Uare, alpha: number) => void) {
            this.drawContent = f;
        }

        setContentDimensions(w: number, h: number) {
            if (this.content) {
                this.content.width = w;
                this.content.height = h;
            }
        }

        setScroll(f: { x: number, y: number }) {
            f.x = f.x || 0
            f.y = f.y || 0
            if (this.content) {
                f.x = (f.x < 0) ? 0 : (f.x > 1) ? 1 : f.x;
                f.y = (f.y < 0) ? 0 : (f.y > 1) ? 1 : f.y;
                this.content.scroll.x = f.x || this.content.scroll.x;
                this.content.scroll.y = f.y || this.content.scroll.y;
            }
        }

        getScroll() {
            if (this.content) {
                return { x: this.content.scroll.x, y: this.content.scroll.y };
            }
        }


        update(dt:number, x?:number, y?:number) {

            if (x && y) {
                let e = "n";
                let c = mouse_is_down(1);
                if (Uare.c && !c) {
                    Uare.c = false;
                    e = "r";
                    Uare.holdt = null;
                } else if (!Uare.c && c) {
                    Uare.c = true;
                    e = "c";
                    Uare.holdt = null;
                }

                let focused = false;

                let updateQueue = [];

                for (let i = 0; i < Uare.elements.length; i++) {
                    updateQueue.push(Uare.elements[i]);
                }

                updateQueue.sort((a, b) => a.z - b.z);

                for (let i = 0; i < updateQueue.length; i++) {
                    let elemt = updateQueue[i];
                    if (elemt) {
                        if (elemt.updateSelf(dt, x, y, ((focused || (Uare.holdt && Uare.holdt.obj != elemt)) || !elemt.active) ? "s" : e)) {
                            focused = true
                        }
                    }
                }
                for (let i = Uare.elements.length = 1; i >= 0; i--) {
                    if (Uare.elements[i]) {
                        Uare.elements[i].updateTrack(dt)
                    }
                }
            }
        }

        draw() {
            let drawQueue = [];

            for (let i = 0; i < Uare.elements.length; i++) {
                if (Uare.elements[i].draw) {
                    drawQueue.push(Uare.elements[i]);
                }
            }

            drawQueue.sort((a, b) => a.z - b.z);

            for (let i = 0; i < drawQueue.length; i++) {
                drawQueue[i].drawSelf();
            }
        }

        style(s: any) {
            Object.assign(this, s);
            return this
        }

        anchor(other: Uare) {
            this.track = { ref: other, d: { x: this.x - other.x, y: this.y - other.y } };

            return this;
        }

        group(group: any) {
            group.elements.push(this);
            return this
        }

        setActive(bool: boolean) {
            if (this.type == "group") {
                for (let i = 0; i < this.elements.length; i++) {
                    this.elements[i].setActive(bool);
                }
            } else {
                this.active = bool
            }
        }

        enable() { return this.setActive(true) }

        disable() { return this.setActive(false) }

        getActive() { if (this.active != null) { return this.active } }

        setVisible(bool:boolean, l:number=0) {

            if (this.type == "group") {
                for (let i = 0; i < this.elements.length; i++) {
                    this.elements[i].setVisible(bool, l)
                }
            } else {
                this.visible = bool
                this.l = l
                if (l == 0) { this.vAlpha = bool ? 1 : 0 }
            }

        }

        show(l: number=0) { return this.setVisible(true, l); }

        hide(l: number=0) { return this.setVisible(false, l); }

        getVisible() { return this.visible; }

        setDragBounds(bounds: {x:number, y:number}[]) {
            this.drag.bounds = bounds
        }

        setHorizontalRange(n: number) {
            this.x = this.drag.bounds[0].x + (this.drag.bounds[1].x - this.drag.bounds[0].x) * n
        }

        setVerticalRange(n: number) {
            this.y = this.drag.bounds[0].y + (this.drag.bounds[1].y - this.drag.bounds[0].y) * n
        }

        getHorizontalRange() {
            if (!(this.drag.bounds && this.drag.bounds[0] && this.drag.bounds[1] && this.drag.bounds[0].x && this.drag.bounds[1].x)) {
                throw new Error("Element must have 2 horizontal boundaries");
            }
            return (this.x - this.drag.bounds[0].x) / (this.drag.bounds[1].x - this.drag.bounds[0].x);
        }

        getVerticalRange() {
            if (!(this.drag.bounds && this.drag.bounds[0] && this.drag.bounds[1] && this.drag.bounds[0].y && this.drag.bounds[1].y)) {
                throw new Error("Element must have 2 vertical boundaries");
            }
            return (this.y - this.drag.bounds[0].y) / (this.drag.bounds[1].y - this.drag.bounds[0].y);
        }

        setIndex(index: number) {

            if (this.type == "group") {
                let lowest: number;
                for (let i = 0; i < this.elements.length; i++) {
                    if (!lowest || this.elements[i].z < lowest) { lowest = this.elements[i].z; }
                }
                for (let i = 0; i < this.elements.length; i++) {
                    let ti = this.elements[i].z - lowest + index;
                    this.elements[i].setIndex(ti);
                }
            } else {
                this.z = index;
                if (index > Uare.hz) { Uare.hz = index; }
            }

        }

        toFront() {
            if ((this.z < Uare.hz) || (this.type == "group")) { return this.setIndex(Uare.hz + 1); }
        }

        getIndex() { return this.z }

        alphaColor(col: [number, number, number, number] | [number, number, number]) {
            var ret = [col[0], col[1], col[2], col[3] && col[3] * this.vAlpha || this.vAlpha * 255];
            return "rgba("+ret[0]+","+ret[1]+","+ret[2]+","+(ret[3]/255.0)+")";
        }

        remove() {
            let self = this;
            for (let i = Uare.elements.length - 1; i > 0; i--) {
                if (Uare.elements[i] == self) { Uare.elements.splice(i, 1); self = null; }
            }
        }

        clear() {
            for (let i = 0; i < Uare.elements.length; i++) {
                Uare.elements[i] = null;
            }

        }


    }
}
