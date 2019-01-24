
interface FarthestPoint {
    y: number;
    x: number;
}

const enum DiffType {
    none = 0,
    removed = 1,
    common = 2,
    added = 3
}

interface DiffResult<T> {
    type: DiffType;
    value: T;
}

interface Route {
    prev: number;
    type: DiffType;
}
class Diff<T> {
    private A: T[];
    private B: T[];
    private M: number;
    private N: number;
    private swapped: boolean;

    private offset: number;
    private delta: number;
    private fp: FarthestPoint[];
    private routes: Route[];

    constructor(A: T[], B: T[]) {
        this.swapped = B.length > A.length;
        [A, B] = this.swapped ? [B, A] : [A, B];
        this.A = A;
        this.B = B;
        this.M = A.length;
        this.N = B.length;

        this.offset = this.N + 1;   // fp[-1]�ɃA�N�Z�X����������̂�+1�̉��ʂ𗚂�����
        this.delta = this.M - this.N;
        this.fp = new Array<FarthestPoint>(this.M + this.N + 1+ 2); // fp[-1]��fp[M+N+1]�ɃA�N�Z�X����������̂�+2�̉��ʂ𗚂�����
        for (let i = 0; i < this.fp.length; i++) { this.fp[i] = { y: -1, x: 0 }; }

        this.routes = []; // �ő�o�H���� M * N + size
    }

    private backTrace(current: FarthestPoint) {
    const result = [];
    let a = this.M - 1;
        let b = this.N - 1;
        let j = this.routes[current.x].prev;
        let type = this.routes[current.x].type;
    for (; ;) {
        switch (type) {
            case DiffType.none: {
                return result;
            }
            case DiffType.removed: {
                result.unshift({ type: (this.swapped ? DiffType.removed : DiffType.added), value: this.B[b] });
                b -= 1;
                break;
            }
            case DiffType.added: {
                result.unshift({ type: (this.swapped ? DiffType.added : DiffType.removed), value: this.A[a] });
                a -= 1;
                break;
            }
            case DiffType.common: {
                result.unshift({ type: DiffType.common, value: this.A[a] });
                a -= 1;
                b -= 1;
                break;
            }
        }
        const prev = j;
        j = this.routes[prev].prev;
        type = this.routes[prev].type;
    }
}


    private createFP(slide: FarthestPoint, down: FarthestPoint, k: number): FarthestPoint {
        if ((slide.y === -1) && (down.y === -1)) {
            // �s���ꂪ�Ȃ��ꍇ�̓X�^�[�g�n�_��
            return { y: 0, x: 0 };
        }
        
        if ((down.y === -1) || k === this.M || (slide.y > down.y + 1)) {
            // �ҏW����͒ǉ�
            this.routes.push({ prev: slide.x, type: DiffType.added });
            return { y: slide.y, x: this.routes.length-1 };
        } else {
            // �ҏW����͍폜
            this.routes.push({ prev: down.x, type: DiffType.removed });
            return { y: down.y + 1, x: this.routes.length-1 };
        }
    }

    private snake(k: number, slide: FarthestPoint, down: FarthestPoint): FarthestPoint {
        if (k < -this.N || this.M < k) {
            return { y: -1, x: 0 };
        }
        const fp = this.createFP(slide, down, k);

        // A��B�̌��݂̔�r�v�f����v���Ă������A���ʗv�f�Ƃ��ēǂݐi�߂�
        // common�̏ꍇ�͕\���΂߂Ɉړ�����΂����̂ŁAx,y�Ƃ���+1����΂���
        while (fp.y + k < this.M && fp.y < this.N && this.A[fp.y + k] === this.B[fp.y]) {
            this.routes.push({ prev: fp.x, type: DiffType.common });
            fp.x = this.routes.length-1;
            fp.y += 1;
        }
        return fp;
    }

diff(): DiffResult < T > [] {

    if (this.M == 0 && this.N == 0) {
        return [];
    } else if (this.N == 0) {
        return this.A.map(a => ({ type: (this.swapped ? DiffType.added : DiffType.removed), value: a }));
    }

    for (let i = 0; i < this.fp.length; i++) { this.fp[i] = { y: -1, x: 0 }; }
    this.routes.length = 0;
    this.routes.push({ prev: 0, type: 0 }); // routes[0]�͊J�n�ʒu
    
    for (let p = 0; this.fp[this.delta + this.offset].y < this.N; p++) {
        for (let k = -p; k < this.delta; ++k) {
            this.fp[k + this.offset] = this.snake(k, this.fp[k - 1 + this.offset], this.fp[k + 1 + this.offset]);
        }
        for (let k = this.delta + p; k > this.delta; --k) {
            this.fp[k + this.offset] = this.snake(k, this.fp[k - 1 + this.offset], this.fp[k + 1 + this.offset]);
        }
        this.fp[this.delta + this.offset] = this.snake(this.delta, this.fp[this.delta - 1 + this.offset], this.fp[this.delta + 1 + this.offset]);
    }

    return this.backTrace(this.fp[this.delta + this.offset]);
}
}

interface Tester<T> {
    deepEqual(obj1: DiffResult<T>[], obj2: DiffResult<T>[]): void;
}

function test(msg: string, lambda: (tester: Tester<string>) => void): void {
    const tester: Tester<string> = {
        deepEqual(obj1: DiffResult<string>[], obj2: DiffResult<string>[]): void {
            if (obj1.length != obj2.length) { throw new Error("obj1 != obj2"); }
            for (let i = 0; i < obj1.length; i++) {
                if ((obj1[i].type != obj2[i].type) || (obj1[i].value != obj2[i].value)) {
                    throw new Error("obj1 != obj2");
                }
            }
        }
    }

    try {
        lambda(tester);
    } catch (e) {
        console.error(msg, "failed", e);
        return;
    }
    console.log(msg, "succrss");
}

window.onload = () => {
    function diff<T>(x:T[], y:T[]) {
        return new Diff<T>(x, y).diff();
    }
    test('empty', t => {
        t.deepEqual(diff([], []), []);
    });

    test('"a" vs "b"', t => {
        t.deepEqual(diff(['a'], ['b']), [{ type: DiffType.removed, value: 'a' }, { type: DiffType.added, value: 'b' }]);
    });

    test('"a" vs "a"', t => {
        t.deepEqual(diff(['a'], ['a']), [{ type: DiffType.common, value: 'a' }]);
    });

    test('"a" vs ""', t => {
        t.deepEqual(diff(['a'], []), [{ type: DiffType.removed, value: 'a' }]);
    });

    test('"" vs "a"', t => {
        t.deepEqual(diff([], ['a']), [{ type: DiffType.added, value: 'a' }]);
    });

    test('"a" vs "a, b"', t => {
        t.deepEqual(diff(['a'], ['a', 'b']), [{ type: DiffType.common, value: 'a' }, { type: DiffType.added, value: 'b' }]);
    });

    test('"strength" vs "string"', t => {
        t.deepEqual(diff(Array.from('strength'), Array.from('string')), [
            { type: DiffType.common, value: 's' },
            { type: DiffType.common, value: 't' },
            { type: DiffType.common, value: 'r' },
            { type: DiffType.removed, value: 'e' },
            { type: DiffType.added, value: 'i' },
            { type: DiffType.common, value: 'n' },
            { type: DiffType.common, value: 'g' },
            { type: DiffType.removed, value: 't' },
            { type: DiffType.removed, value: 'h' },
        ]);
    });

    test('"strength" vs ""', t => {
        t.deepEqual(diff(Array.from('strength'), Array.from('')), [
            { type: DiffType.removed, value: 's' },
            { type: DiffType.removed, value: 't' },
            { type: DiffType.removed, value: 'r' },
            { type: DiffType.removed, value: 'e' },
            { type: DiffType.removed, value: 'n' },
            { type: DiffType.removed, value: 'g' },
            { type: DiffType.removed, value: 't' },
            { type: DiffType.removed, value: 'h' },
        ]);
    });

    test('"" vs "strength"', t => {
        t.deepEqual(diff(Array.from(''), Array.from('strength')), [
            { type: DiffType.added, value: 's' },
            { type: DiffType.added, value: 't' },
            { type: DiffType.added, value: 'r' },
            { type: DiffType.added, value: 'e' },
            { type: DiffType.added, value: 'n' },
            { type: DiffType.added, value: 'g' },
            { type: DiffType.added, value: 't' },
            { type: DiffType.added, value: 'h' },
        ]);
    });

    test('"abc", "c" vs "abc", "bcd", "c"', t => {
        t.deepEqual(diff(['abc', 'c'], ['abc', 'bcd', 'c']), [
            { type: DiffType.common, value: 'abc' },
            { type: DiffType.added, value: 'bcd' },
            { type: DiffType.common, value: 'c' },
        ]);
    });
};