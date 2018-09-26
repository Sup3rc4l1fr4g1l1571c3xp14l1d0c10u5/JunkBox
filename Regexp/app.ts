interface String {
    startsWith(a: string, index: number): boolean;
}

interface Array<T> {
    startsWith(a: T[], index: number): boolean;
}

Array.prototype.startsWith = function <T>(a: T[], index: number): boolean {
    for (let i = 0; i < a.length; i++) {
        if (this[index + i] !== a) { return false; }
    }
    return true;
}

module Re1 {
    "use strict";

    const enum RegexpType {
        Alt = 1,
        Cat = 2,
        Star = 3,
        Plus = 4,
        Quest = 5,
        Paren = 6,
        Dot = 7,
        Lit = 8,
    }

    const enum InstOpCode {
        Char = 1,
        Match = 2,
        Jmp = 3,
        Split = 4,
        Any = 5,
        Save = 6,
    }

    class Regexp {
        type: RegexpType;
        left: Regexp;
        right: Regexp;
        groupId: number;
        ch: string;

        constructor(type: RegexpType, left: Regexp, right: Regexp) {
            this.type = type;
            this.left = left;
            this.right = right;
            this.groupId = 0;
            this.ch = "";
        }

        static compile(str: string): Regexp {
            let strIndex = 0;
            let nparen = 0;
            return line();

            function line(): Regexp {
                const parsed_regexp = alt();
                EOL();
                return parsed_regexp;
            }

            function alt(): Regexp {
                let $1 = concat();
                while (isToken('|')) {
                    token('|');
                    $1 = new Regexp(RegexpType.Alt, $1, concat());
                }
                return $1;
            }

            function concat(): Regexp {
                let $1 = repeat();
                while (isRepeat()) {
                    $1 = new Regexp(RegexpType.Cat, $1, repeat());
                }
                return $1;
            }

            function isRepeat(): boolean {
                return (['|', '*', '+', '?', ')', ':'].some(x => isToken(x)) == false) && (isToken() == true);
            }

            function repeat(): Regexp {
                let $1 = single();
                if (isToken('*')) {
                    token('*');
                    $1 = new Regexp(RegexpType.Star, $1, null);
                } else if (isToken('+')) {
                    token('+');
                    $1 = new Regexp(RegexpType.Plus, $1, null);
                } else if (isToken('?')) {
                    token('?');
                    $1 = new Regexp(RegexpType.Quest, $1, null);
                }
                if (isToken('?')) {
                    token('?');
                    $1.groupId = 1;
                }
                return $1;
            }

            function count(): number {
                return ++nparen;
            }

            function single(): Regexp {
                if (isToken('(')) {
                    token('(');
                    if (isToken('?')) {
                        token('?');
                        token(':');
                        const $4 = alt();
                        token(')');
                        return $4;
                    } else {
                        const $2 = count();
                        const $3 = alt();
                        token(')')
                        const $$ = new Regexp(RegexpType.Paren, $3, null);
                        $$.groupId = $2;
                        return $$;
                    }
                }
                if (isToken('.')) {
                    token('.');
                    return new Regexp(RegexpType.Dot, null, null);
                }

                if (isToken('|', '*', '+', '?', '(', ')', ':', '.') == false && isToken() == true) {
                    const $$ = new Regexp(RegexpType.Lit, null, null);
                    $$.ch = token();
                    return $$;
                }

                throw new Error();
            }

            function CHAR(): string {
                if (strIndex == str.length) {
                    throw new Error();
                }
                return token();
            }

            function EOL(): void {
                if (strIndex != str.length) {
                    throw new Error(`${strIndex}/${str.length}`);
                }
            }

            function token(tok?: string): string {
                if (tok == undefined) {
                    if (strIndex == str.length) {
                        return "";
                    } else {
                        return str[strIndex++];
                    }
                } else {
                    if (str.startsWith(tok, strIndex)) {
                        strIndex += tok.length;
                        return tok;
                    } else {
                        throw new Error(`${tok} not found in ${str}, strIndex=${strIndex}`);
                    }
                }
            }

            function isToken(...toks: string[]): boolean {
                if (toks.length == 0) {
                    if (strIndex == str.length) {
                        return false;
                    } else {
                        return true;
                    }
                } else {
                    for (const tok of toks) {
                        if (str.startsWith(tok, strIndex)) {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }

        toString(): string {
            switch (this.type) {
                default: return "???";
                case RegexpType.Alt: return `Alt(${this.left.toString()}, ${this.right.toString()})`;
                case RegexpType.Cat: return `Cat(${this.left.toString()}, ${this.right.toString()})`;
                case RegexpType.Lit: return `Lit(${this.ch})`;
                case RegexpType.Dot: return `Dot`;
                case RegexpType.Paren: return `Paren(${this.groupId}, ${this.left.toString()})`;
                case RegexpType.Star: return `${(this.groupId ? "Ng" : "")}Star(${this.left.toString()})`;
                case RegexpType.Plus: return `${(this.groupId ? "Ng" : "")}Plus(${this.left.toString()})`;
                case RegexpType.Quest: return `${(this.groupId ? "Ng" : "")}Quest(${this.left.toString()})`;
            }
        }
    }

    interface IInstParam {
        opcode: InstOpCode;
        ch?: string;
        saveSlot?: number;
        x?: number;
        y?: number;
        gen?: number;
    }

    class Inst {
        opcode: InstOpCode;
        ch: string;
        saveSlot: number;
        x: number;
        y: number;
        gen: number;
        constructor(p: IInstParam) {
            this.opcode = (p && p.opcode) ? p.opcode : 0;
            this.ch = (p && p.ch) ? p.ch : "";
            this.saveSlot = (p && p.saveSlot) ? p.saveSlot : 0;
            this.x = (p && p.x) ? p.x : null;
            this.y = (p && p.y) ? p.y : null;
            this.gen = (p && p.gen) ? p.gen : 0;
        }
    }

    class Prog {
        start: Inst[];
        len: number;
        constructor() {
            this.start = [];
            this.len = 0;
        }
        static compile(r: Regexp): Prog {
            const n = count(r) + 1;
            const p = new Prog();
            let pc = 0;
            p.start[pc++] = new Inst({ opcode: InstOpCode.Save, saveSlot: 0 });
            emit(r);
            p.start[pc++] = new Inst({ opcode: InstOpCode.Save, saveSlot: 1 });
            p.start[pc++] = new Inst({ opcode: InstOpCode.Match });
            p.len = pc;
            return p;

            function count(r: Regexp): number {
                switch (r.type) {
                    default: throw new Error("bad count");
                    case RegexpType.Alt: return 2 + count(r.left) + count(r.right);
                    case RegexpType.Cat: return count(r.left) + count(r.right);
                    case RegexpType.Lit:
                    case RegexpType.Dot: return 1;
                    case RegexpType.Paren: return 2 + count(r.left);
                    case RegexpType.Quest: return 1 + count(r.left);
                    case RegexpType.Star: return 2 + count(r.left);
                    case RegexpType.Plus: return 1 + count(r.left);
                }
            }

            function emit(r: Regexp): void {
                switch (r.type) {
                    default:
                        throw new Error("bad emit");

                    case RegexpType.Alt: {
                        const p1 = pc++;
                        p.start[p1] = new Inst({ opcode: InstOpCode.Split });
                        p.start[p1].x = pc;
                        emit(r.left);
                        const p2 = pc++;
                        p.start[p2] = new Inst({ opcode: InstOpCode.Jmp });
                        p.start[p1].y = pc;
                        emit(r.right);
                        p.start[p2].x = pc;
                        break;
                    }
                    case RegexpType.Cat: {
                        emit(r.left);
                        emit(r.right);
                        break;
                    }
                    case RegexpType.Lit: {
                        p.start[pc++] = new Inst({ opcode: InstOpCode.Char, ch: r.ch });
                        break;
                    }
                    case RegexpType.Dot: {
                        p.start[pc++] = new Inst({ opcode: InstOpCode.Any });
                        break;
                    }
                    case RegexpType.Paren: {
                        p.start[pc++] = new Inst({ opcode: InstOpCode.Save, saveSlot: 2 * r.groupId + 0 });
                        emit(r.left);
                        p.start[pc++] = new Inst({ opcode: InstOpCode.Save, saveSlot: 2 * r.groupId + 1 });
                        break;
                    }
                    case RegexpType.Quest: {
                        const p1 = pc++;
                        p.start[p1] = new Inst({ opcode: InstOpCode.Split });
                        p.start[p1].x = pc;
                        emit(r.left);
                        p.start[p1].y = pc;
                        if (r.groupId) {  // non-greedy
                            const t = p.start[p1].x;
                            p.start[p1].x = p.start[p1].y;
                            p.start[p1].y = t;
                        }
                        break;
                    }
                    case RegexpType.Star: {
                        const p1 = pc++;
                        p.start[p1] = new Inst({ opcode: InstOpCode.Split });
                        p.start[p1].x = pc;
                        emit(r.left);
                        const p2 = pc++;
                        p.start[p2] = new Inst({ opcode: InstOpCode.Jmp });
                        p.start[p2].x = p1;
                        p.start[p1].y = pc;
                        if (r.groupId) {  // non-greedy
                            const t = p.start[p1].x;
                            p.start[p1].x = p.start[p1].y;
                            p.start[p1].y = t;
                        }
                        break;
                    }
                    case RegexpType.Plus:
                        const p1 = pc;
                        emit(r.left);
                        const p2 = pc++;
                        p.start[p2] = new Inst({ opcode: InstOpCode.Split });
                        p.start[p2].x = p1;
                        p.start[p2].y = pc;
                        if (r.groupId) {  // non-greedy
                            const t = p.start[p2].x;
                            p.start[p2].x = p.start[p2].y;
                            p.start[p2].y = t;
                        }
                        break;
                }
            }
        }

        toString() {
            function _2d(n: number) {
                const s = "00" + n.toString();
                return s.substr(s.length - 2, 2);
            }
            const ret = [];
            for (let pc = 0; pc < this.len; pc++) {
                switch (this.start[pc].opcode) {
                    default:
                        throw new Error("printprog");
                    case InstOpCode.Split:
                        ret.push(`${_2d(pc)}. split ${this.start[pc].x}, ${this.start[pc].y}`);
                        break;
                    case InstOpCode.Jmp:
                        ret.push(`${_2d(pc)}. jmp ${this.start[pc].x}`);
                        break;
                    case InstOpCode.Char:
                        ret.push(`${_2d(pc)}. char ${this.start[pc].ch}`);
                        break;
                    case InstOpCode.Any:
                        ret.push(`${_2d(pc)}. any`);
                        break;
                    case InstOpCode.Match:
                        ret.push(`${_2d(pc)}. match`);
                        break;
                    case InstOpCode.Save:
                        ret.push(`${_2d(pc)}. save ${this.start[pc].saveSlot}`);
                }
            }
            return ret.join("\r\n");
        }

        pikevm(input: string): IMatchResult[] {
            let clist = new ThreadList(this.start.length);
            let nlist = new ThreadList(this.start.length);
            let gen = 1;
            const self = this;
            addthread(clist, new Thread(0, null), 0);
            let matched = null;
            for (let sp = 0; ; sp++) {
                if (clist.count == 0) {
                    break;
                }
                gen++;
                loop: for (let i = 0; i < clist.count; i++) {
                    const pc = clist.threads[i].pc;
                    const sub = clist.threads[i].sub;
                    switch (this.start[pc].opcode) {
                        case InstOpCode.Char:
                            if (input[sp] != this.start[pc].ch) {
                                break;
                            }
                        case InstOpCode.Any:
                            if (input.length == sp) {
                                break;
                            }
                            addthread(nlist, new Thread(pc + 1, sub), sp + 1);
                            break;
                        case InstOpCode.Match:
                            matched = sub;
                            break loop;
                        // Jmp, Split, Save handled in addthread, so that
                        // machine execution matches what a backtracker would do.
                        // This is discussed (but not shown as code) in
                        // Regular Expression Matching: the Virtual Machine Approach.
                    }
                }
                ThreadList.swap(clist, nlist);
                nlist.clear();
                if (sp == input.length) {
                    break;
                }
            }
            if (matched) {
                const subp: number[] = [];
                for (let m = matched; m != null; m = m.next) {
                    if (subp[m.index] == undefined) {
                        subp[m.index] = m.value;
                    }
                }
                const result: IMatchResult[] = [];
                for (let i = 0; i < subp.length; i += 2) {
                    result.push({ start: subp[i + 0], end: subp[i + 1] });
                }
                return result;
            } else {
                return null;
            }

            function addthread(threadList: ThreadList, thread: Thread, sp: number): void {
                if (self.start[thread.pc].gen == gen) {
                    return;  // already on list
                }
                self.start[thread.pc].gen = gen;

                switch (self.start[thread.pc].opcode) {
                    default:
                        threadList.threads[threadList.count] = thread;
                        threadList.count++;
                        break;
                    case InstOpCode.Jmp:
                        addthread(threadList, new Thread(self.start[thread.pc].x, thread.sub), sp);
                        break;
                    case InstOpCode.Split:
                        addthread(threadList, new Thread(self.start[thread.pc].x, thread.sub), sp);
                        addthread(threadList, new Thread(self.start[thread.pc].y, thread.sub), sp);
                        break;
                    case InstOpCode.Save:
                        addthread(threadList, new Thread(thread.pc + 1, new Sub(self.start[thread.pc].saveSlot, sp, thread.sub)), sp);
                        break;
                }
            }
        }
    }

    class Sub {
        index: number;
        value: number;
        next: Sub;
        constructor(index: number, value: number, next: Sub) {
            this.index = index;
            this.value = value;
            this.next = next;
        }
    };

    class Thread {
        pc: number;
        sub: Sub;
        constructor(pc: number, sub: Sub) {
            this.pc = pc;
            this.sub = sub;
        }
    }

    class ThreadList {
        threads: Thread[]
        count: number;
        constructor(capacity: number) {
            this.threads = [];
            this.count = 0;
            for (let i = 0; i < capacity; i++) {
                this.threads[i] = new Thread(null, null);
            }
        }
        clear(): void {
            this.count = 0;
        }
        static swap(x: ThreadList, y: ThreadList): void {
            const threads = x.threads;
            const count = x.count;
            x.threads = y.threads;
            x.count = y.count;
            y.threads = threads;
            y.count = count;
        }
    }

    interface IMatchResult {
        start: number;
        end: number
    }

    function test(): void {
        const testcases = [
            { regexp: "abcdefg", text: "abcdefg", expect: [{ start: 0, end: 7 }] },
            { regexp: "a(bcdef)g", text: "abcdefg", expect: [{ start: 0, end: 7 }, { start: 1, end: 6 }] },
            { regexp: "ab.d.fg", text: "abcdefg", expect: [{ start: 0, end: 7 }] },
            { regexp: "a.+g", text: "abcdefg", expect: [{ start: 0, end: 7 }] },
            { regexp: "ab(.+)fg", text: "abcdefg", expect: [{ start: 0, end: 7 }, { start: 2, end: 5 }] },
            { regexp: "ab((cd)+)", text: "abcdcdcdfg", expect: [{ start: 0, end: 8 }, { start: 2, end: 8 }, { start: 6, end: 8 }] },
            { regexp: "ab((cd)+?)", text: "abcdcdcdfg", expect: [{ start: 0, end: 4 }, { start: 2, end: 4 }, { start: 6, end: 4 }] },
        ];
        for (const { regexp, text, expect } of testcases) {
            try {
                const prog = Prog.compile(Regexp.compile(regexp));
                const result = prog.pikevm(text);
                let equal = true;
                if (result.length != expect.length) {
                    equal = false;
                } else {
                    for (let i = 0; i < result.length; i++) {
                        if ((result[i].start != expect[i].start) || (result[i].end != expect[i].end)) {
                            equal = false;
                            break;
                        }
                    }
                }
                console.log(equal ? 'success' : 'failed', 'regexp=', regexp, 'text=', text, 'expect=', expect, 'result=', result);
            } catch (e) {
                console.log('exception', 'regexp=', regexp, 'text=', text, 'expect=', expect, 'exception=', e);
            }
        }
    }

    window.onload = () => {
        test();
        document.getElementById('check').onclick = () => {
            const regexp = (<HTMLInputElement>document.getElementById('regexp')).value;
            const text = (<HTMLInputElement>document.getElementById('text')).value;
            const dom_result = (<HTMLSpanElement>document.getElementById('result'));
            try {
                const prog = Prog.compile(Regexp.compile(regexp));
                dom_result.innerText = `[${prog.pikevm(text).map((x, i) => `${i}: [${x.start} ... ${x.end}]` ).join('\r\n')}]`;
            } catch (e) {
                dom_result.innerText = e.toString();
            }
        };
    };
}
