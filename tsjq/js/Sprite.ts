interface ISprite {
    update(ms: number, delta: number): boolean;
    draw(camera: Dungeon.Camera): void;
}

const charDic: { [key: string]: [number, number] } = {
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

function draw7pxFont(str: string, x: number, y: number) {
    const fontWidth: number = 5;
    const fontHeight: number = 7;
    let sx: number = x;
    let sy : number = y;
    for (let i = 0; i < str.length; i++) {
        const ch = str[i];
        if (ch === "\n") {
            sy += fontHeight;
            sx = x;
            continue;
        }
        const [fx, fy] = charDic[str[i]];
        Game.getScreen().drawImage(
            Game.getScreen().texture("font7px"),
            fx,
            fy,
            fontWidth,
            fontHeight,
            sx,
            sy,
            fontWidth,
            fontHeight
        );
        sx += fontWidth - 1;
    }
}

function createShowDamageSprite(start: number, dmg: number, getpos: () => IPoint): ISprite {
    const damage = "" + ~~dmg;
    let elapse = 0;
    const fontWidth: number = 5;
    const fontHeight: number = 7;
    return {
        update: (delta: number, ms: number): boolean => {
            elapse = ms - start;
            return (elapse > 500);
        },
        draw: (camera: Dungeon.Camera): void => {
            const { x: sx, y: sy } = getpos();

            const xx = sx - camera.left;
            const yy = sy - camera.top;

            const len = damage.length;
            const offx = -(len) * (fontWidth - 1) / 2;
            const offy = 0;
            for (let i = 0; i < damage.length; i++) {
                const rad = Math.min(elapse - i * 20, 200);
                if (rad < 0) { continue; }
                const dy = Math.sin(rad * Math.PI / 200) * - 7; // 7 = 跳ね上がる高さ
                if (0 <= xx + (i + 1) * fontWidth && xx + (i + 0) * fontWidth < Game.getScreen().offscreenWidth &&
                    0 <= yy + (1 * fontHeight) && yy + (0 * fontHeight) < Game.getScreen().offscreenHeight) {
                    const [fx, fy] = charDic[damage[i]];
                    Game.getScreen().drawImage(
                        Game.getScreen().texture("font7px"),
                        fx,
                        fy,
                        fontWidth,
                        fontHeight,
                        (xx + (i + 0) * (fontWidth - 1)) + offx,
                        (yy + (0) * fontHeight) + offy + dy,
                        fontWidth,
                        fontHeight
                    );
                }
            }
        },
    };
}
