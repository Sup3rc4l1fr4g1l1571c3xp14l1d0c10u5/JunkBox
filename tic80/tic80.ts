module TIC80 {
    "use strict";


    // config

    // PICO-8 : 128x128 =  8192 byte
    // TIC-80 : 240x136 = 16320 byte
    // NDS    : 256x192 = 24576 byte
    // NES    : 256x240 = 30720 byte
    // STM32F7 DISCO : 408x272
    const SCREEN_WIDTH = 256;
    const SCREEN_HEIGHT = 192;

    const SPRITE_SIZE = 8;

    // 240x136 cells, 1920x1088 pixels (240*8 x 136*8)
    const MAP_WIDTH = (~~(SCREEN_WIDTH / SPRITE_SIZE)) * SPRITE_SIZE;
    const MAP_HEIGHT = (~~(SCREEN_HEIGHT / SPRITE_SIZE)) * SPRITE_SIZE;

    const MemoryMap: { [key: string]: { size: number; } } = {
        screen: { size: /*0x03FC0*/(SCREEN_WIDTH * SCREEN_HEIGHT) >> 1 },
        palette: { size: 0x00030 },
        palette_map: { size: 0x00008 },
        border: { size: 0x00001 },
        screen_offset: { size: 0x00002 },
        mouse_cursor: { size: 0x00001 },
        tiles: { size: 0x02000 },
        sprites: { size: 0x02000 },
        map: { size: 0x07F80 },
        gamepads: { size: 0x00004 },
        mouse: { size: 0x00004 },
    };

    const Registry: { [key: string]: any } = {
        SCREEN_W: SCREEN_WIDTH,
        SCREEN_H: SCREEN_HEIGHT,
        SPR_W: SPRITE_SIZE,
        SPR_H: SPRITE_SIZE,
        MAP_W: MAP_WIDTH,
        MAP_H: MAP_HEIGHT,
    };

    // utility
    function times(step: number): number[] {
        return [...Array(step).keys()];
    }


    // Ascii Font rom
    // 128 * 8 byte = 1024 byte = 1 kb
    module FontRom {
        export const rom: number[][] = [
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

    export function Initialize() {
        const Api = {};

        const Button = {
            _btn_status: times(32).map(() => { return { Hold: 0, Period: 0, Timer: -1, Status: false, PrevStatus: false, Result: false }; }),

            _btn_set(id: number, status: boolean): void {
                Button._btn_status[id & 0x1F].Status = status;
            },

            _btns_update(): void {
                Button._btn_status.forEach((btn, i) => {
                    if (btn.Status) {
                        if (btn.Timer == 0 || btn.Timer == btn.Hold) {
                            btn.Result = true;
                        } else {
                            btn.Result = false;
                        }

                        if (btn.Timer == btn.Hold + btn.Period) {
                            btn.Timer = btn.Hold;
                        } else {
                            btn.Timer += 1;
                        }
                    } else {
                        btn.Timer = 0;
                        btn.Result = false;
                    }
                    btn.PrevStatus = btn.Status;
                    Memory.poke1(Memory._memory_map["gamepads"], i, btn.PrevStatus ? 1 : 0);
                });
            },

            _btn(id: number): boolean {
                return Button._btn_status[id & 0x1F].Result;
            },

            _btnp(id: number): boolean {
                const btn = Button._btn_status[id & 0x1F];
                return btn.Result;
            },
            _setbtnp(id: number, hold: number = 0, period: number = 0): void {
                const _hold = ~~hold;
                const _period = ~~period;
                const btn = Button._btn_status[id & 0x1F];
                btn.Hold = _hold;
                btn.Period = _period;
                btn.Timer = 0;
                btn.PrevStatus = btn.Status = btn.Result = false;
            }
        };

        Object.assign(Api, {
            btn: Button._btn,
            btnp: Button._btnp,
            setbtnp: Button._setbtnp,
        });


        const Memory: {
            ram: Uint8Array;
            _memory_map: { [string: string]: Uint8Array };
            _memory_layout: { [string: string]: [number, number] };


            init: (MemoryMap: { [key: string]: { size: number; } }) => void;
            minfo: (name: string) => [number, number];
            poke: (ram: Uint8Array, index: number, value: number) => void;
            peek: (ram: Uint8Array, index: number) => number;
            poke4: (ram: Uint8Array, index: number, value: number) => void;
            peek4: (ram: Uint8Array, index: number) => number;
            poke1: (ram: Uint8Array, index: number, value: number) => void;
            peek1: (ram: Uint8Array, index: number) => number;
        } = {
                ram: null,
                _memory_map: {},
                _memory_layout: {},
                init: (MemoryMap: { [key: string]: { size: number; } }) => {
                    let ram_size = 0;
                    for (const key of Object.keys(MemoryMap)) {
                        Memory._memory_layout[key] = [ram_size, ram_size + MemoryMap[key].size];
                        ram_size += MemoryMap[key].size;
                    }
                    Memory.ram = new Uint8Array(ram_size);
                    for (const key of Object.keys(Memory._memory_layout)) {
                        let [start, size] = Memory._memory_layout[key];
                        Memory._memory_map[key] = Memory.ram.subarray(start, size);
                    }
                },
                minfo: (name: string): [number, number] => {
                    return Memory._memory_layout[name];
                },

                poke: (ram: Uint8Array, index: number, value: number): void => {
                    ram[~~index] = value;
                },

                peek: (ram: Uint8Array, index: number): number => {
                    return ram[~~index];
                },

                poke4: (ram: Uint8Array, index: number, value: number): void => {
                    const address = (index >> 1);
                    if (index & 1) {
                        ram[address] = (ram[address] & 0x0F) | ((value & 0x0F) << 4);
                    } else {
                        ram[address] = (ram[address] & 0xF0) | ((value & 0x0F) << 0);
                    }
                },

                peek4: (ram: Uint8Array, index: number): number => {
                    const address = (index >> 1);
                    if (index & 1) {
                        return (ram[address] & 0xF0) >> 4;
                    } else {
                        return (ram[address] & 0x0F) >> 0;
                    }
                },

                poke1: (ram: Uint8Array, index: number, value: number): void => {
                    const address = (index >> 3);
                    const bit = (7 - index & 0x07);
                    ram[address] = (ram[address] & (~(1 << bit))) | ((value & 0x01) << bit);
                },

                peek1: (ram: Uint8Array, index: number): number => {
                    const address = (index >> 3);
                    const bit = (7 - index & 0x07);
                    return (ram[address] >> bit) & 0x01;
                },
            }

        Memory.init(MemoryMap);

        Object.assign(Api, {
            poke: (address: number, value: number): void => {
                Memory.ram[~~address] = value;
            },

            peek: (address: number): number => {
                return Memory.ram[~~address];
            },

            poke4: (index: number, value: number): void => {
                Memory.poke4(Memory.ram, index, value);
            },

            peek4: (index: number): number => {
                return Memory.peek4(Memory.ram, index);
            },
            poke1: (index: number, value: number): void => {
                Memory.poke1(Memory.ram, index, value);
            },

            peek1: (index: number): number => {
                return Memory.peek1(Memory.ram, index);
            },
            memcpy: (toaddr: number, fromaddr: number, len: number): void => {
                if (len <= 0) {
                    return;
                }
                Memory.ram.copyWithin(toaddr, fromaddr, fromaddr + len);
            },

            memset: (addr: number, val: number, len: number): void => {
                if (len <= 0) {
                    return;
                }
                Memory.ram.fill(val, addr, addr + len);
            },
            minfo: (name: string): [number, number] => {
                return Memory.minfo(name);
            }
        });

        interface ISideBuffer {
            Left: number;
            Right: number;
        }

        const Vram: {
            set_palette_map: (mapid: number, palid: number) => void;
            get_palette_map: (mapid: number) => number;
            _set_palette: (mapid: number, rgb: number) => void;
            _get_palette: (mapid: number) => number;
            _set_pixel: (x: number, y: number, color: number) => void;
            _get_pixel: (x: number, y: number) => number;
            _rect: (x: number, y: number, w: number, h: number, color: number) => void;
            _rect_border: (x: number, y: number, w: number, h: number, color: number) => void;
            _line: (x0: number, y0: number, x1: number, y1: number, putPixelHandler: (x: number, y: number) => void) => void;
            _lineH: (x: number, y: number, w: number, color: number) => void;
            _lineV: (x: number, y: number, h: number, color: number) => void;
            _sideBuffer: ISideBuffer[];
            _initSidesBuffer: () => void;
            _setSidePixel: (x: number, y: number) => void;
            clip: { l: number, r: number, t: number, b: number };
            _clip: (x: number, y: number, w: number, h: number) => void;
            _tri: (x1: number, y1: number, x2: number, y2: number, x3: number, y3: number, color: number) => void;
            _circle: (xm: number, ym: number, radius: number, color: number) => void;
            _circle_border: (xm: number, ym: number, radius: number, color: number) => void;
        } = {
                set_palette_map: (mapid: number, palid: number): void => {
                    const palette_map = Memory._memory_map["palette_map"];
                    Memory.poke4(palette_map, mapid, palid);
                },

                get_palette_map: (mapid: number): number => {
                    const palette_map = Memory._memory_map["palette_map"];
                    return Memory.peek4(palette_map, (mapid & 0x0F));
                },

                _set_palette: (palid: number, rgb: number): void => {
                    const address = (palid & 0x0F) * 3;
                    const palette = Memory._memory_map["palette"];
                    palette[address + 0] = (rgb >> 16) & 0xFF;
                    palette[address + 1] = (rgb >> 8) & 0xFF;
                    palette[address + 2] = (rgb >> 0) & 0xFF;
                },

                _get_palette: (palid: number): number => {
                    const address = (palid & 0x0F) * 3;
                    const palette = Memory._memory_map["palette"];
                    return (palette[address + 0] << 16) | (palette[address + 1] << 8) | palette[address + 2];
                },
                _set_pixel: (x: number, y: number, color: number): void => {
                    if (Vram.clip.t <= y && y < Vram.clip.b && Vram.clip.l <= x && x < Vram.clip.r) {
                        const screen = Memory._memory_map["screen"];
                        Memory.poke4(screen, ~~y * SCREEN_WIDTH + ~~x, color);
                    }
                },

                _get_pixel: (x: number, y: number): number => {
                    if (0 <= y && y < SCREEN_HEIGHT && 0 <= x && x < SCREEN_WIDTH) {
                        const screen = Memory._memory_map["screen"];
                        return Memory.peek4(screen, ~~y * SCREEN_WIDTH + ~~x);
                    } else {
                        return 0;
                    }
                },

                _rect: (x: number, y: number, w: number, h: number, color: number): void => {
                    x = ~~x; y = ~~y; w = ~~w; h = ~~h; color = ~~color;
                    for (let j = y; j < y + h; j++) {
                        Vram._lineH(x, j, w, color);
                    }
                },

                _rect_border: (x: number, y: number, w: number, h: number, color: number): void => {
                    x = ~~x; y = ~~y; w = ~~w; h = ~~h;
                    Vram._lineH(x, y, w, color);
                    Vram._lineH(x, y + h - 1, w, color);
                    Vram._lineV(x, y, h, color);
                    Vram._lineV(x + w - 1, y, h, color);
                },
                _line: (x0: number, y0: number, x1: number, y1: number, putPixelHandler: (x: number, y: number) => void): void => {
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
                },
                _lineH: (x: number, y: number, w: number, color: number): void => {
                    if (y < Vram.clip.t || y >= Vram.clip.b) { return; }
                    const xl = Math.max(x, Vram.clip.l);
                    const xr = Math.min(x + w, Vram.clip.r);
                    const screen = Memory._memory_map["screen"];
                    for (let i = xl; i < xr; i++) {
                        Memory.poke4(screen, y * SCREEN_WIDTH + i, color);
                    }
                },

                _lineV: (x: number, y: number, h: number, color: number): void => {
                    if (x < Vram.clip.l || x >= Vram.clip.r) { return; }
                    const yl = y < Vram.clip.t ? Vram.clip.t : y;
                    const yr = y + h >= Vram.clip.b ? Vram.clip.b : y + h;
                    const screen = Memory._memory_map["screen"];
                    for (let i = yl; i < yr; ++i) {
                        Memory.poke4(screen, i * SCREEN_WIDTH + x, color);
                    }
                },

                _sideBuffer: times(SCREEN_HEIGHT).map(() => { return { Left: 0, Right: 0 }; }),

                _initSidesBuffer: (): void => {
                    for (let i = 0; i < SCREEN_HEIGHT; i++) {
                        Vram._sideBuffer[i].Left = SCREEN_WIDTH;
                        Vram._sideBuffer[i].Right = -1;
                    }
                },

                _setSidePixel: (x: number, y: number): void => {
                    if (y >= 0 && y < SCREEN_HEIGHT) {
                        if (x < Vram._sideBuffer[y].Left) { Vram._sideBuffer[y].Left = x; }
                        if (x > Vram._sideBuffer[y].Right) { Vram._sideBuffer[y].Right = x; }
                    }
                },
                clip: {
                    l: 0,
                    r: SCREEN_WIDTH,
                    t: 0,
                    b: SCREEN_HEIGHT,
                },
                _clip: (x: number = 0, y: number = 0, w: number = SCREEN_WIDTH, h: number = SCREEN_HEIGHT): void => {

                    Vram.clip.l = ~~x;
                    Vram.clip.t = ~~y;
                    Vram.clip.r = ~~x + ~~w;
                    Vram.clip.b = ~~y + ~~h;

                    if (Vram.clip.l < 0) { Vram.clip.l = 0; }
                    if (Vram.clip.t < 0) { Vram.clip.t = 0; }
                    if (Vram.clip.r > SCREEN_WIDTH) { Vram.clip.r = SCREEN_WIDTH; }
                    if (Vram.clip.b > SCREEN_HEIGHT) { Vram.clip.b = SCREEN_HEIGHT; }
                },
                _tri: (x1: number, y1: number, x2: number, y2: number, x3: number, y3: number, color: number): void => {
                    x1 = ~~x1; y1 = ~~y1; x2 = ~~x2; y2 = ~~y2;

                    Vram._initSidesBuffer();
                    Vram._line(x1, y1, x2, y2, Vram._setSidePixel);
                    Vram._line(x2, y2, x3, y3, Vram._setSidePixel);
                    Vram._line(x3, y3, x1, y1, Vram._setSidePixel);
                    const final_color = Vram.get_palette_map(color);
                    const yt = Math.max(Vram.clip.t, Math.min(y1, Math.min(y2, y3)));
                    const yb = Math.min(Vram.clip.b, Math.max(y1, Math.max(y2, y3)) + 1);

                    for (let y = yt; y < yb; y++) {
                        const xl = Math.max(Vram._sideBuffer[y].Left, Vram.clip.l);
                        const xr = Math.min(Vram._sideBuffer[y].Right + 1, Vram.clip.r);
                        Vram._lineH(xl, y, xr - xl, final_color);
                    }
                },
                _circle: (xm: number, ym: number, radius: number, color: number): void => {
                    xm = ~~xm; ym = ~~ym; radius = ~~radius; color = ~~color;

                    let r = radius;
                    let x = -r;
                    let y = 0;
                    let err = 2 - 2 * r;

                    Vram._initSidesBuffer();
                    do {
                        Vram._setSidePixel(xm - x, ym + y);
                        Vram._setSidePixel(xm - y, ym - x);
                        Vram._setSidePixel(xm + x, ym - y);
                        Vram._setSidePixel(xm + y, ym + x);

                        r = err;
                        if (r <= y) { err += ++y * 2 + 1; }
                        if (r > x || err > y) { err += ++x * 2 + 1; }
                    } while (x < 0);

                    const yt = Math.max(ym - radius, Vram.clip.t);
                    const yb = Math.min(ym + radius + 1, Vram.clip.b);
                    for (let y = yt; y < yb; y++) {
                        const xl = Math.max(Vram._sideBuffer[y].Left, Vram.clip.l);
                        const xr = Math.min(Vram._sideBuffer[y].Right + 1, Vram.clip.r);
                        Vram._lineH(xl, y, xr - xl, color);
                    }
                },
                _circle_border: (xm: number, ym: number, radius: number, color: number): void => {
                    xm = ~~xm; ym = ~~ym; radius = ~~radius;

                    let r = radius;
                    let x = -r;
                    let y = 0;
                    let err = 2 - 2 * r;

                    do {
                        Vram._set_pixel(xm - x, ym + y, color);
                        Vram._set_pixel(xm - y, ym - x, color);
                        Vram._set_pixel(xm + x, ym - y, color);
                        Vram._set_pixel(xm + y, ym + x, color);

                        r = err;
                        if (r <= y) { err += ++y * 2 + 1; }
                        if (r > x || err > y) { err += ++x * 2 + 1; }
                    } while (x < 0);
                }
            };

        function api_pix(x: number, y: number, color?: number): number {
            if (color == undefined) {
                return Vram._get_pixel(x, y);
            } else {
                const final_color = Vram.get_palette_map(color);
                Vram._set_pixel(x, y, final_color);
                return 0;
            }
        }


        function api_cls(color: number = 0): void {
            const final_color = Vram.get_palette_map(color);
            if (Vram.clip.l == 0 && Vram.clip.t == 0 && Vram.clip.r == SCREEN_WIDTH && Vram.clip.b == SCREEN_HEIGHT) {
                const byte = (final_color << 4) | final_color;
                const screen = Memory._memory_map["screen"];
                screen.fill(byte);
            } else {
                Vram._rect(Vram.clip.l, Vram.clip.t, Vram.clip.r - Vram.clip.l, Vram.clip.b - Vram.clip.t, final_color);
            }
        }

        function api_rect(x: number, y: number, w: number, h: number, color: number): void {
            const final_color = Vram.get_palette_map(color);
            Vram._rect(x, y, w, h, final_color);
        }

        function api_rect_border(x: number, y: number, w: number, h: number, color: number): void {
            const final_color = Vram.get_palette_map(color);
            Vram._rect_border(x, y, w, h, final_color);
        }

        function api_line(x0: number, y0: number, x1: number, y1: number, color: number): void {
            const final_color = Vram.get_palette_map(color);
            Vram._line(x0, y0, x1, y1, (x, y) => Vram._set_pixel(x, y, final_color));
        }

        function api_clip(x: number = 0, y: number = 0, w: number = SCREEN_WIDTH, h: number = SCREEN_HEIGHT): void {
            Vram._clip(x, y, w, h);
        }

        function api_tri(x1: number, y1: number, x2: number, y2: number, x3: number, y3: number, color: number): void {
            const final_color = Vram.get_palette_map(color);
            Vram._tri(x1, y1, x2, y1, x3, y3, final_color);
        }

        function api_circle(xm: number, ym: number, radius: number, color: number): void {
            const final_color = Vram.get_palette_map(color);
            Vram._circle(xm, ym, radius, final_color);
        }

        function api_circle_border(xm: number, ym: number, radius: number, color: number): void {
            const final_color = Vram.get_palette_map(color);
            Vram._circle_border(xm, ym, radius, final_color);
        }
        Object.assign(Api, {
            clip: api_clip,
            cls: api_cls,
            pix: api_pix,
            line: api_line,
            rect: api_rect,
            rectb: api_rect_border,
            circ: api_circle,
            circb: api_circle_border,
            tri: api_tri,
            trib: null,
            textri: null,

        });

        const Sprite = {
            _drawTile: (ram: Uint8Array, id: number, x: number, y: number, colorkey: number = -1, scale: number = 1, flip: number = 0, rotate: number = 0, drawrect: (x: number, y: number, width: number, height: number, color: number) => void = null): void => {
                id = ~~id; x = ~~x; y = ~~y; colorkey = ~~colorkey; scale = ~~scale;
                drawrect = drawrect || Vram._rect;

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
                        const pixel = Memory.peek4(ram, 64 * id + i);
                        if (pixel != colorkey) {
                            drawrect(xx, y, scale, scale, pixel);
                        }
                    }
                }
            },
        }

        function api_spr(id: number, x: number, y: number, colorkey: number = -1, scale: number = 1, flip: number = 0, rotate: number = 0): void {
            const sprites = Memory._memory_map["sprites"];
            Sprite._drawTile(sprites, id, x, y, colorkey, scale, flip, rotate, api_rect);
        }

        Object.assign(Api, {
            spr: api_spr,
        });

        const Text: {
            print: (text: string, x?: number, y?: number, color?: number, fixed?: boolean, scale?: number) => number;
            font: (text: string, x: number, y: number, colorkey: number, char_width: number, char_height: number, fixed?: boolean, scale?: number) => number;
        } = {
                print: (text: string, x: number = 0, y: number = 0, color: number = 15, fixed: boolean = false, scale: number = 1): number => {
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
                            const image = FontRom.rom[code & 0x7F];
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

                },
                font: (text: string, x: number, y: number, colorkey: number, char_width: number, char_height: number, fixed: boolean = false, scale: number = 1): number => {
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
            };
        Object.assign(Api, {
            print: Text.print,
            font: Text.font,
        });

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
            const map = Memory._memory_map["map"];
            const tiles = Memory._memory_map["tiles"];

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

                    Sprite._drawTile(tiles, tile[0], ii, jj, colorkey, scale, tile[1], tile[2], api_rect);
                }
            }
        }

        function api_map_set(x: number, y: number, value: number): void {
            if (x < 0 || x >= MAP_WIDTH || y < 0 || y >= MAP_HEIGHT) {
                return;
            }

            const map = Memory._memory_map["map"];
            map[~~y * MAP_WIDTH + ~~x] = value;
        }

        function api_map_get(x: number, y: number): number {
            if (x < 0 || x >= MAP_WIDTH || y < 0 || y >= MAP_HEIGHT) {
                return;
            }

            const map = Memory._memory_map["map"];
            return map[~~y * MAP_WIDTH + ~~x];
        }

        function createEvalContext(): any {
            return Object.assign({
                TIC: function () { },
                scanline: function (line: number) { },
                sfx: null,
                key: null,
                keyp: null,
                map: api_map,
                mget: api_map_get,
                mset: api_map_set,
                music: null,
                reset: reset,
                pmem: null,
                trace: null,
                time: api_time,
                mouse: null,
                sync: null,
                exit: null,

                pi: Math.PI,
                abs: Math.abs,
                cos: Math.cos,
                sin: Math.sin,
                tan: Math.tan,
                floor: Math.floor,
                rand: api_rand,

            }, Api);
        };

        let evalContext: any = null;
        let tic = 0;

        function reset(): void {
            Memory.ram.fill(0);
            api_clip(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);
            Vram._set_palette(0, 0x140C1C);	// Black
            Vram._set_palette(1, 0x442434);	// Dark Red
            Vram._set_palette(2, 0x30346D);	// Dark Blue
            Vram._set_palette(3, 0x4E4A4F);	// Dark Gray
            Vram._set_palette(4, 0x854C30);	// Brown
            Vram._set_palette(5, 0x346524);	// Dark Green
            Vram._set_palette(6, 0xD04648);	// Red
            Vram._set_palette(7, 0x757161);	// Light Gray
            Vram._set_palette(8, 0x597DCE);	// Light Blue
            Vram._set_palette(9, 0xD27D2C);	// Orange
            Vram._set_palette(10, 0x8595A1);	// Blue/Gray
            Vram._set_palette(11, 0x6DAA2C);	// Light Green
            Vram._set_palette(12, 0xD2AA99);	// Peach
            Vram._set_palette(13, 0x6DC2CA);	// Cyan
            Vram._set_palette(14, 0xDAD45E);	// Yellow
            Vram._set_palette(15, 0xDEEED6);	// White

            for (let i = 0; i < 16; i++) {
                Vram.set_palette_map(i, i);
            }
            tic = 0;

            // copy font to sprite 
            const tiles = Memory._memory_map["tiles"];
            for (let i = 0; i < FontRom.rom.length; i++) {
                for (let j = 0; j < 8; j++) {
                    const line = FontRom.rom[i][j];
                    for (let k = 0; k < 8; k++) {
                        Memory.poke4(tiles, i * 64 + j * 8 + k, (line & (0x80 >> k)) ? 0x0F : 0x00);
                    }
                }
            }

            evalContext = createEvalContext();
            Object.assign(evalContext, Registry);
        }

        const _canvas: HTMLCanvasElement = document.createElement("canvas");
        _canvas.width = SCREEN_WIDTH;
        _canvas.height = SCREEN_HEIGHT;
        document.getElementById("wrapper").appendChild(_canvas);
        const _context: CanvasRenderingContext2D = _canvas.getContext("2d");

        const _backbuffer: ImageData = _context.createImageData(SCREEN_WIDTH, SCREEN_HEIGHT);

        function _flip(): void {
            let i = 0;
            let backbuffer_address = 0;
            const screen = Memory._memory_map["screen"];
            const palette = Memory._memory_map["palette"];
            const fb = _backbuffer.data;
            for (let y = 0; y < SCREEN_HEIGHT; y++) {
                evalContext.scanline(y);
                for (let x = 0; x < SCREEN_WIDTH; x++) {
                    const palette_address = Memory.peek4(screen, i) * 3;
                    fb[backbuffer_address + 0] = palette[palette_address + 0];
                    fb[backbuffer_address + 1] = palette[palette_address + 1];
                    fb[backbuffer_address + 2] = palette[palette_address + 2];
                    fb[backbuffer_address + 3] = 0xFF;
                    i++;
                    backbuffer_address += 4;
                }
            }
            _context.putImageData(_backbuffer, 0, 0);
        }


        function animationFrameFunc(): void {
            Button._btns_update();
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
                Button._btn_set(key_id, true);
            }
        });
        window.addEventListener("keyup", (e) => {
            const key_id = KeyMap[e.key];
            if (key_id != undefined) {
                Button._btn_set(key_id, false);
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

        //var ctx2 = (<HTMLCanvasElement>document.getElementById("glcanvas2")).getContext("2d");
        //Uare.Uare.demo3(ctx2);
    }
}
window.addEventListener("load", TIC80.Initialize);

module Uare {

    export type Color = [number, number, number, number];
    export interface Point { x: number; y: number; };

    const _mouse: boolean[] = [false, false, false];
    const _mousepos: Point = { x: 0, y: 0 };

    function mouse_is_down(id: number) {
        return _mouse[id];
    }

    export function mouse_down(id: number, state: boolean) {
        return _mouse[id] = state;
    }
    export function mouse_pos(x: number, y: number) {
        _mousepos.x = x;
        _mousepos.y = y;
    }

    export interface IUIStyle {
        width: number;
        height: number;
        color: Color;
        holdColor: Color;
        hoverColor: Color;
        border: IUIBorderStyle;
        text: IUITextStyle;
    }
    export interface IUITextStyle {
        color: Color;
        holdColor: Color;
        hoverColor: Color;
        font: string;
        offset: Point;
        align: string;
    };
    export interface IUIBorderStyle {
        color: Color;
        holdColor: Color;
        hoverColor: Color;
        size: number;
    };
    export interface IUIIcon {
        type: string;
        content: Point[][] | HTMLImageElement;
    }
    export interface IUIStyleOpt {
        width?: number;
        height?: number;
        color?: Color;
        holdColor?: Color;
        hoverColor?: Color;
        border?: IUIBorderStyleOpt;
        text?: IUITextStyleOpt;
    }

    export interface IUITextStyleOpt {
        color?: Color;
        holdColor?: Color;
        hoverColor?: Color;
        font?: string;
        offset?: Point;
        align?: string;
    };
    export interface IUIBorderStyleOpt {
        color?: Color;
        holdColor?: Color;
        hoverColor?: Color;
        size?: number;
    };

    export class Uare implements IUIStyle {
        static elements: Uare[] = [];
        static z: number = 1;
        static hz: number = null;
        static holdt: { obj: Uare, d: Point } = null;
        static clicked: boolean = false;

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
        static newButton(f?: any) {
            return new Button(f);
        }
        static newStyle(f?: IUIStyleOpt): IUIStyleOpt {
            return f;
        }
        static newIcon(f?: IUIIcon) {
            return f;
        }
        static newGroup() {
            return new Group();
        }
        static update(dt: number, x?: number, y?: number) {

            if (x != undefined && y != undefined) {
                let mouseEventName = "n";
                let down = mouse_is_down(0);
                if (Uare.clicked == true && down == false) {
                    Uare.clicked = false;
                    mouseEventName = "r";
                    Uare.holdt = null;
                } else if (Uare.clicked == false && down == true) {
                    Uare.clicked = true;
                    mouseEventName = "c";
                    Uare.holdt = null;
                }

                let focused = false;

                const updateQueue: Uare[] = Uare.elements.concat();
                updateQueue.sort((a, b) => b.z - a.z);

                for (const elemt of updateQueue) {
                    if (elemt) {
                        if (elemt.updateSelf(dt, x, y, ((focused || (Uare.holdt && Uare.holdt.obj != elemt)) || !elemt.active) ? "s" : mouseEventName)) {
                            focused = true
                        }
                    }
                }
                for (let i = Uare.elements.length - 1; i >= 0; i--) {
                    if (Uare.elements[i]) {
                        Uare.elements[i].updateTrack(dt);
                    }
                }
            }
        }

        static draw(context: CanvasRenderingContext2D) {
            context.clearRect(0, 0, context.canvas.width, context.canvas.height);
            Uare.elements.filter(x => x.draw != null).sort((a, b) => a.z - b.z).forEach(x => x.drawSelf(context));
        }
        static clear() {
            Uare.elements.length = 0;
        }


        drawContent: (self: Uare, alpha: number) => void;
        icon: {
            source: IUIIcon;
            color: Color;
            holdColor: Color;
            hoverColor: Color;
            offset: Point
        };

        // basic property 
        x: number;
        y: number;

        // style property
        width: number;
        height: number;
        color: Color;
        holdColor: Color;
        hoverColor: Color;
        border: IUIBorderStyle;
        text: IUITextStyle & { display: string };

        // status
        active: boolean;
        hold: boolean;
        hover: boolean;
        //click: boolean;
        drag: { enabled: boolean, fixed: Point, bounds: Point[] };
        visible: boolean;
        vAlpha: number;
        center: boolean;
        track: { ref: Uare, d: Point };
        content: { scroll: Point, width: number, height: number, wrap: boolean };
        type: string;
        z: number;
        lerp: number;

        onClick: (x: number, y: number) => void;
        onCleanRelease: (x: number, y: number) => void;
        onRelease: (x: number, y: number) => void;
        onHold: (x: number, y: number) => void;
        onStartHover: (x: number, y: number) => void;
        onHover: (x: number, y: number) => void;
        onReleaseHover: (x: number, y: number) => void;

        draw(context: CanvasRenderingContext2D) { }

        constructor(t: string, f: any = {}) {

            this.x = 0;
            this.y = 0;
            this.width = 0;
            this.height = 0;
            this.hover = false;
            this.hold = null;
            //this.click = false;
            this.active = true;
            this.drag = { enabled: false, fixed: null, bounds: null };
            this.visible = true;
            this.vAlpha = 1;
            this.center = false;
            this.content = { scroll: { x: 0, y: 0 }, width: undefined, height: undefined, wrap: undefined };
            this.lerp = 0.2;
            this.color = [0, 0, 0, 0];
            this._assign(this, f);
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

        updateSelf(dt: number, mx: number, my: number, e: string) {

            let alphaTarget = this.visible ? 1 : 0;
            if (this.vAlpha != alphaTarget) {
                this.vAlpha = Uare.lerp(this.vAlpha, alphaTarget, this.lerp);
            }

            let mlc = (e != "s") && mouse_is_down(0);

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
                this.onClick(mx, my);
            } else if ((e == "r" && wb && thold) && this.onCleanRelease) {
                this.onCleanRelease(mx, my);
            } else if (((e == "r" && wb && thold) || (this.hold && !wb)) && this.onRelease) {
                this.onRelease(mx, my);
            } else if (this.hold && this.onHold) {
                this.onHold(mx, my);
            } else if (!thover && this.hover && this.onStartHover) {
                this.onStartHover(mx, my);
            } else if (this.hover && this.onHover) {
                this.onHover(mx, my);
            } else if (thover && !this.hover && this.onReleaseHover) {
                this.onReleaseHover(mx, my);
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


        drawSelf(context: CanvasRenderingContext2D) {

            let tempX = this.x;
            let tempY = this.y;
            if (this.center) {
                tempX = this.x - this.width * .5;
                tempY = this.y - this.height * .5;
            }

            context.fillStyle = this.alphaColor((this.hold && this.holdColor) ? this.holdColor : (this.hover && this.hoverColor) ? this.hoverColor : this.color);
            context.fillRect(tempX, tempY, this.width, this.height);

            if (this.border && this.border.color && this.border.size) {
                context.strokeStyle = this.alphaColor((this.hold && this.border.holdColor) ? this.border.holdColor : (this.hover && this.border.hoverColor) ? this.border.hoverColor : this.border.color);
                context.lineWidth = this.border.size;
                context.strokeRect(tempX, tempY, this.width, this.height);
            }

            if (this.icon && this.icon.source.type && this.icon.source.content) {
                context.save();
                context.strokeStyle = this.alphaColor((this.hold && this.icon.holdColor) ? this.icon.holdColor : (this.hover && this.icon.hoverColor) ? this.icon.hoverColor : this.icon.color);
                let offset = this.icon.offset || { x: 0, y: 0 };
                context.translate((tempX + (this.center ? 0 : this.width * .5) + offset.x), (tempY + (this.center ? 0 : this.height * 0.5) + offset.y));

                if (this.icon.source.type == "polygon") {
                    const content = <Point[][]>this.icon.source.content;
                    for (let i = 0; i < content.length; i++) {
                        context.beginPath();
                        for (let j = 0; j < content[i].length; j++) {
                            const c: Point = content[i][j];
                            if (j == 0) {
                                context.moveTo(c.x, c.y);
                            } else {
                                context.lineTo(c.x, c.y);
                            }
                        }
                        context.closePath();
                        context.stroke();
                    }
                } else if (this.icon.source.type == "image") {
                    context.drawImage(<HTMLImageElement>this.icon.source.content, 0, 0);
                }
                context.restore();
            }

            if (this.text && this.text.display && this.text.color) {
                context.save();
                context.fillStyle = this.alphaColor((this.hold && this.text.holdColor) ? this.text.holdColor : (this.hover && this.text.hoverColor) ? this.text.hoverColor : this.text.color);
                context.font = this.text.font;
                let offset = this.text.offset || { x: 0, y: 0 };
                context.textAlign = "center";
                context.textBaseline = "middle";
                let ox: number = this.width * 0.5;
                let oy: number = this.height * 0.5;
                const aligns = (this.text.align || "").split("-");
                for (const align of aligns) {
                    switch (align) {
                        case "left":
                            context.textAlign = "left";
                            ox = 0;
                            break;
                        case "center":
                            context.textAlign = "center";
                            ox = this.width * 0.5;
                            break;
                        case "right":
                            context.textAlign = "right";
                            ox = this.width;
                            break;
                        case "top":
                            context.textBaseline = "top";
                            oy = 0;
                            break;
                        case "middle":
                            context.textBaseline = "middle";
                            oy = this.height * 0.5;
                            break;
                        case "bottom":
                            context.textBaseline = "bottom";
                            oy = this.height;
                            break;
                    }
                }
                context.fillText(
                    this.text.display,
                    this.x + ox + offset.x,
                    this.y + oy + offset.y,
                    this.width
                );
                context.restore();
            }

            if (this.content && this.drawContent) {
                this.renderContent(context);
            }

        }

        renderContent(context: CanvasRenderingContext2D) {
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

        setScroll(f: Point) {
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

        _assign(d: any, s: any): any {
            switch (Object.prototype.toString.call(s)) {
                case "[object Array]":
                    if (d == null) { d = []; }
                    for (var key in s) {
                        d[key] = this._assign(d[key], s[key]);
                    }
                    return d;
                case "[object Object]":
                    if (d == null) { d = {}; }
                    for (var key in s) {
                        d[key] = this._assign(d[key], s[key]);
                    }
                    return d;
                default:
                    if (typeof (s) === 'function') {
                        return s;
                    } else {
                        return new s.constructor(s.valueOf())
                    }
            }
        }

        style(s: IUIStyleOpt) {

            this._assign(this, s);
            return this
        }

        anchor(other: Uare) {
            this.track = { ref: other, d: { x: this.x - other.x, y: this.y - other.y } };

            return this;
        }

        group(group: Group) {
            group.elements.push(this);
            return this
        }

        setActive(bool: boolean) {
            this.active = bool;
        }

        enable() { return this.setActive(true); }

        disable() { return this.setActive(false); }

        getActive() { if (this.active != null) { return this.active; } }

        setVisible(bool: boolean, lerp: number = 0) {

            this.visible = bool;
            this.lerp = lerp;
            if (lerp == 0) { this.vAlpha = bool ? 1 : 0; }

        }

        show(lerp: number = 0) { return this.setVisible(true, lerp); }

        hide(lerp: number = 0) { return this.setVisible(false, lerp); }

        getVisible() { return this.visible; }

        setDragBounds(bounds: { x: number, y: number }[]) {
            this.drag.bounds = bounds;
        }

        setHorizontalRange(n: number) {
            this.x = this.drag.bounds[0].x + (this.drag.bounds[1].x - this.drag.bounds[0].x) * n;
        }

        setVerticalRange(n: number) {
            this.y = this.drag.bounds[0].y + (this.drag.bounds[1].y - this.drag.bounds[0].y) * n;
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
            this.z = index;
            if (index > Uare.hz) { Uare.hz = index; }
        }

        toFront() {
            if ((this.z < Uare.hz) || (this.type == "group")) { return this.setIndex(Uare.hz + 1); }
        }

        getIndex() { return this.z }

        alphaColor(col: Color) {
            return "rgba(" + col[0] + "," + col[1] + "," + col[2] + "," + (col[3] / 255.0) + ")";
        }

        remove() {
            let self = this;
            for (let i = Uare.elements.length - 1; i > 0; i--) {
                if (Uare.elements[i] == self) { Uare.elements.splice(i, 1); self = null; }
            }
        }

        static start(context: CanvasRenderingContext2D, update?: ((dt: number, x: number, y: number) => void)) {
            let prev = Date.now();
            window.addEventListener("mousemove", (e) => mouse_pos(e.x, e.y));
            window.addEventListener("mousedown", (e) => mouse_down(e.button, true));
            window.addEventListener("mouseup", (e) => mouse_down(e.button, false));

            function loop() {
                const now = Date.now();
                const delta = now - prev;
                prev = now;
                if (update) { update(delta, _mousepos.x, _mousepos.y); }
                Uare.update(delta, _mousepos.x, _mousepos.y);
                Uare.draw(context);
                window.requestAnimationFrame(loop);
            }
            loop();
        }

        static demo1(context: CanvasRenderingContext2D) {
            const font = "12px 'Times New Roman'";
            const myStyle = Uare.newStyle({

                width: 400,
                height: 60,


                color: [200, 200, 200, 255],

                hoverColor: [150, 150, 150, 255],

                holdColor: [100, 100, 100, 255],


                border: {
                    color: [255, 255, 255, 255],

                    hoverColor: [200, 200, 200, 255],

                    holdColor: [150, 150, 150, 255],

                    size: 5
                },

                text: {
                    color: [200, 0, 0, 255],

                    hoverColor: [150, 0, 0, 255],

                    holdColor: [255, 255, 255, 255],

                    font: font,

                    align: "center",

                    offset: {
                        x: 0,
                        y: 0    //-30
                    }
                },

            });
            const WWIDTH = 640;
            const WHEIGHT = 480;

            const myButton1 = new Button({
                text: { display: "button" },
                x: WWIDTH * .5 - 200,
                y: WHEIGHT * .5 - 200
            }).style(myStyle);

            const myButton2 = new Button({
                text: { display: "hover" },
                x: WWIDTH * .5 - 200,
                y: WHEIGHT * .5 - 80,
                width: 180
            }).style(myStyle);

            const myButton3 = new Button({
                text: { display: "click" },
                x: WWIDTH * .5 + 20,
                y: WHEIGHT * .5 - 80,
                width: 180,
                onClick: function () { myButton3.y = WHEIGHT * .5 - 80 + 2; },
                onRelease: function () { myButton3.y = WHEIGHT * .5 - 80 - 2; }
            }).style(myStyle)

            Uare.start(context);
        }

        static demo2(context: CanvasRenderingContext2D) {
            const WWIDTH = 640;
            const WHEIGHT = 480;

            const font = "48px 'Times New Roman'";
            const myStyle = Uare.newStyle({
                width: 400,
                height: 60,

                // color
                color: [200, 200, 200, 255],
                hoverColor: [150, 150, 150, 255],
                holdColor: [100, 100, 100, 255],

                // border
                border: {
                    color: [255, 255, 255, 255],
                    hoverColor: [200, 200, 200, 255],
                    holdColor: [150, 150, 150, 255],
                    size: 5
                },

                // text

                text: {
                    color: [200, 0, 0, 255],
                    hoverColor: [150, 0, 0, 255],
                    holdColor: [255, 255, 255, 255],
                    font: font,
                    align: "center",
                },

            });

            function outsideDropdown(mx: number, my: number) {
                return (mx <= WWIDTH * .5 - 200 || mx >= WWIDTH * .5 + 200 || my <= hoverMe.y || my >= drop2.y + drop2.height);
            }
            const dropdownIcon = Uare.newIcon({
                type: "polygon",

                content: [
                    [
                        { x: -10, y: -10 },
                        { x: 0, y: 0 },
                        { x: 10, y: -10 }
                    ]
                ]
            });

            const hoverMe = new Button({

                text: {
                    display: "hover me"
                },
                x: WWIDTH * .5 - 200,
                y: WHEIGHT * .5 - 200,
                icon: {
                    source: dropdownIcon,
                    color: [100, 100, 100, 255],
                    hoverColor: [255, 255, 255, 255],
                    offset: {
                        x: 170,
                        y: 6
                    }
                },

                onHover: function () {
                    dropdownGroup.show(.5);
                    dropdownGroup.enable();
                },
                onReleaseHover: function (x: number, y: number) {
                    if (outsideDropdown(x, y)) {
                        dropdownGroup.hide(.5);
                        dropdownGroup.disable();
                    }
                }
            }).style(myStyle);

            const dropdownGroup = Uare.newGroup();

            const dropbg = new Button({
                color: [100, 100, 100, 255],

                x: WWIDTH * .5 - 201,
                y: WHEIGHT * .5 - 140,

                width: 402,
                height: 60 * 2
            }).group(dropdownGroup);

            const drop1 = new Button({

                text: {
                    display: "hello"
                },
                border: {
                    size: 1
                },
                x: WWIDTH * .5 - 200,
                y: WHEIGHT * .5 - 140 + 60 * 0,

            }).style(myStyle).group(dropdownGroup)

            const drop2 = new Button({

                text: {
                    display: "there"
                },
                border: {
                    size: 1
                },
                x: WWIDTH * .5 - 200,
                y: WHEIGHT * .5 - 140 + 60 * 1,
            }).style(myStyle).group(dropdownGroup)

            hoverMe.toFront();
            dropdownGroup.hide();
            Uare.start(context, (dt, x, y) => {
                if (dropdownGroup.getActive() && outsideDropdown(x, y)) {
                    dropdownGroup.hide(.5);
                    dropdownGroup.disable();
                }
            });
        }
        static demo3(context: CanvasRenderingContext2D) {
            const WWIDTH = 640;
            const WHEIGHT = 480;

            function createWindow(x: number, y: number) {
                let group = Uare.newGroup()
                const font = "48px 'Times New Roman'";

                let top = new Button({
                    x: x,
                    y: y,
                    width: 250,
                    height: 30,

                    drag: {
                        enabled: true
                    },

                    color: [160, 160, 160, 255],
                    hoverColor: [140, 140, 140, 255],

                    text: {
                        display: "sample text",

                        font: font,

                        offset: {
                            x: 16,
                            y: -16
                        },

                        color: [255, 255, 255, 255],
                    },

                    onClick: () => group.toFront(),
                }).group(group).style(borderStyle);

                let close = new Button({
                    x: x + 200,
                    y: y,

                    width: 50,
                    height: 30,

                    color: [200, 200, 200, 255],
                    hoverColor: [200, 180, 180, 255],
                    holdColor: [200, 160, 160, 255],

                    icon: {
                        source: closeIcon,

                        color: [220, 0, 0, 255],

                        hoverColor: [250, 0, 0, 255],

                        holdColor: [150, 0, 0, 255],

                    },

                    onCleanRelease: () => { group.setActive(false); group.setVisible(false, .5); },

                }).anchor(top).group(group).style(borderStyle);

                let content = new Button({
                    x: x,
                    y: y + 30,

                    width: 250,
                    height: 300,

                    color: [255, 255, 255, 255],

                }).anchor(top).group(group).style(borderStyle);

                return { top: top, close: close, content: content, group: group };
            }

            const font = "48px 'Times New Roman'";

            const closeIcon = Uare.newIcon({
                type: "polygon",
                content: [
                    [
                        { x: -6, y: -4 },
                        { x: -4, y: -6 },
                        { x: 6, y: 4 },
                        { x: 4, y: 6 }
                    ],
                    [
                        { x: 6, y: -4 },
                        { x: 4, y: -6 },
                        { x: -6, y: 4 },
                        { x: -4, y: 6 }
                    ],
                ]
            });

            const borderStyle = Uare.newStyle({
                border: {
                    color: [100, 100, 100, 255],
                    size: 2
                }
            });

            const windows: any = [];
            for (var i = 0; i < 10; i++) {
                windows[i] = createWindow(i * 50, i * 18);
            }

            const open = new Button({

                x: WWIDTH * .5 - 200,
                y: WHEIGHT - 80,
                width: 400,
                height: 60,
                color: [0, 0, 0, 255],
                hoverColor: [100, 100, 100, 255],
                holdColor: [100, 0, 0, 255],

                border: {
                    color: [255, 255, 255, 255],
                    size: 5
                },

                text: {
                    display: "open all windows",
                    font: font,
                    color: [255, 255, 255, 255],
                    align: "center",
                    offset: {
                        x: 0,
                        y: -20
                    }
                },

                onCleanRelease: () => { for (const window of windows) { window.group.show(); window.group.enable(); } },
            });


            open.toFront();

            Uare.start(context);
        }
    }

    class Button extends Uare {
        constructor(f: any = {}) {
            super("button", f);
        }
    }

    class Group extends Uare {
        elements: Uare[];
        constructor() {
            super("group");
            this.elements = [];
        }
        setActive(bool: boolean) {
            this.elements.forEach(x => x.setActive(bool));
        }
        setVisible(bool: boolean, lerp: number = 0) {
            for (let i = 0; i < this.elements.length; i++) {
                this.elements[i].setVisible(bool, lerp);
            }

        }
        setIndex(index: number) {
            let lowest: number;
            for (let i = 0; i < this.elements.length; i++) {
                if (!lowest || this.elements[i].z < lowest) { lowest = this.elements[i].z; }
            }
            for (let i = 0; i < this.elements.length; i++) {
                let ti = this.elements[i].z - lowest + index;
                this.elements[i].setIndex(ti);
            }
        }
    }
}


