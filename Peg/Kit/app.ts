module PegKit {

    export type IR
        = { type: "Char", char: string, successLabel: number, failLabel: number, loc: string }
        | { type: "CharClass", inverted: boolean, parts: { begin: string, end: string }[], ignoreCase: boolean, successLabel: number, failLabel: number, loc: string }
        | { type: "AnyChar", successLabel: number, failLabel: number, loc: string }
        | { type: "Str", str: string, successLabel: number, failLabel: number, loc: string }
        | { type: "PushContext", loc: string }
        | { type: "PushArray", loc: string }
        | { type: "PushPosition", loc: string }
        | { type: "Label", id: number, loc: string }
        | { type: "Rule", name: string, loc: string }
        | { type: "Jump", id: number, loc: string }
        | { type: "Nip", loc: string }
        | { type: "Append", loc: string }
        | { type: "Pop", loc: string }
        | { type: "PopContext", loc: string }
        | { type: "PushNull", loc: string }
        | { type: "Call", name: string, loc: string }
        | { type: "Test", successLabel: number, failLabel: number, loc: string }
        | { type: "Capture", name: string, loc: string }
        | { type: "PushCapture", loc: string }
        | { type: "PopCapture", loc: string }
        | { type: "Action", code: string, captures: string[], loc: string }
        | { type: "Text", loc: string }
        | { type: "Return", success: boolean, loc: string }
        | { type: "Comment", comment: string, loc: string }

    export class Compiler {
        private irCodes: IR[];
        private labelId: number;
        private captures: { [key: string]: number }[];

        private allocLabel(): number {
            return this.labelId++;
        }

        private pushCaptureScope(): void {
            this.captures.push({});
        }

        private popCaptureScope(): void {
            this.captures.pop();
        }

        private addCaptureLabel(name: string): void {
            this.captures[this.captures.length - 1][name] = this.captures[this.captures.length - 1].Count;
        }

        private currentCaptures(): string[] {
            return Object.keys(this.captures[this.captures.length - 1]);
        }

        private Char(char: string, successLabel: number, failLabel: number): void {
            this.irCodes.push({ type: "Char", char: char, successLabel: successLabel, failLabel: failLabel, loc: new Error().stack.split(/\n/)[1] });
        }

        private CharClass(inverted: boolean, parts: { begin: string, end: string }[], ignoreCase: boolean, successLabel: number, failLabel: number): void {
            this.irCodes.push({ type: "CharClass", inverted: inverted, parts: parts, ignoreCase: ignoreCase, successLabel: successLabel, failLabel: failLabel, loc: new Error().stack.split(/\n/)[1] });
        }

        private AnyChar(successLabel: number, failLabel: number): void {
            this.irCodes.push({ type: "AnyChar", successLabel: successLabel, failLabel: failLabel, loc: new Error().stack.split(/\n/)[1] });
        }

        private Str(str: string, successLabel: number, failLabel: number): void {
            this.irCodes.push({ type: "Str", str: str, successLabel: successLabel, failLabel: failLabel, loc: new Error().stack.split(/\n/)[1] });
        }

        private PushContext(): void {
            this.irCodes.push({ type: "PushContext", loc: new Error().stack.split(/\n/)[1] });
        }

        private PushArray(): void {
            this.irCodes.push({ type: "PushArray", loc: new Error().stack.split(/\n/)[1] });
        }

        private PushPosition(): void {
            this.irCodes.push({ type: "PushPosition", loc: new Error().stack.split(/\n/)[1] });
        }

        private Nip(): void {
            this.irCodes.push({ type: "Nip", loc: new Error().stack.split(/\n/)[1] });
        }

        private Pop(): void {
            this.irCodes.push({ type: "Pop", loc: new Error().stack.split(/\n/)[1] });
        }

        private PopContext(): void {
            this.irCodes.push({ type: "PopContext", loc: new Error().stack.split(/\n/)[1] });
        }

        private Append(): void {
            this.irCodes.push({ type: "Append", loc: new Error().stack.split(/\n/)[1] });
        }

        private PushNull(): void {
            this.irCodes.push({ type: "PushNull", loc: new Error().stack.split(/\n/)[1] });
        }

        private Text(): void {
            this.irCodes.push({ type: "Text", loc: new Error().stack.split(/\n/)[1] });
        }

        private Label(id: number): void {
            this.irCodes.push({ type: "Label", id: id, loc: new Error().stack.split(/\n/)[1] });
        }

        private Jump(id: number): void {
            this.irCodes.push({ type: "Jump", id: id, loc: new Error().stack.split(/\n/)[1] });
        }

        private Call(name: string): void {
            this.irCodes.push({ type: "Call", name: name, loc: new Error().stack.split(/\n/)[1] });
        }

        private Test(successLabel: number, failLabel: number): void {
            this.irCodes.push({ type: "Test", successLabel: successLabel, failLabel: failLabel, loc: new Error().stack.split(/\n/)[1] });
        }

        private Capture(name: string): void {
            this.irCodes.push({ type: "Capture", name: name, loc: new Error().stack.split(/\n/)[1] });
        }

        private PushCapture(): void {
            this.irCodes.push({ type: "PushCapture", loc: new Error().stack.split(/\n/)[1] });
        }

        private PopCapture(): void {
            this.irCodes.push({ type: "PopCapture", loc: new Error().stack.split(/\n/)[1] });
        }

        private Action(code: string, captures: string[]): void {
            this.irCodes.push({ type: "Action", code: code, captures: captures, loc: new Error().stack.split(/\n/)[1] });
        }

        private Rule(name: string): void {
            this.irCodes.push({ type: "Rule", name: name, loc: new Error().stack.split(/\n/)[1] });
        }

        private Return(success: boolean): void {
            this.irCodes.push({ type: "Return", success: success, loc: new Error().stack.split(/\n/)[1] });
        }

        private Comment(comment: string): void {
            this.irCodes.push({ type: "Comment", comment: comment, loc: new Error().stack.split(/\n/)[1] });
        }

        constructor() {
            this.irCodes = [];
            this.labelId = 1;
            this.captures = [];
        }

        public compileGrammar(g: Ast.Grammar): IR[] {
            const keys = Object.keys(g);
            for (const key of keys) {
                this.compileRule(key, g[key]);
            }
            return this.irCodes;
        }

        private compileRule(ruleName: string, ast: Ast.Type): void {
            const successLabel = this.allocLabel();
            const failLabel = this.allocLabel();
            this.pushCaptureScope();
            this.Rule(ruleName);
            this.visit(ruleName, ast, successLabel, failLabel);
            this.popCaptureScope();
            this.Label(successLabel);
            this.Return(true);
            this.Label(failLabel);
            this.Return(false);
        }

        private visit(ruleName: string, ast: Ast.Type, successLabel: number, failLabel: number): void {
            switch (ast.type) {
                case "Char": this.onChar(ruleName, ast, successLabel, failLabel); break;
                case "CharClass": this.onCharClass(ruleName, ast, successLabel, failLabel); break;
                case "AnyChar": this.onAnyChar(ruleName, ast, successLabel, failLabel); break;
                case "Str": this.onStr(ruleName, ast, successLabel, failLabel); break;
                case "Sequence": this.onSequence(ruleName, ast, successLabel, failLabel); break;
                case "Choice": this.onChoice(ruleName, ast, successLabel, failLabel); break;
                case "Optional": this.onOptional(ruleName, ast, successLabel, failLabel); break;
                case "Group": this.onGroup(ruleName, ast, successLabel, failLabel); break;
                case "ZeroOrMore": this.onZeroOrMore(ruleName, ast, successLabel, failLabel); break;
                case "OneOrMore": this.onOneOrMore(ruleName, ast, successLabel, failLabel); break;
                case "AndPredicate": this.onAndPredicate(ruleName, ast, successLabel, failLabel); break;
                case "NotPredicate": this.onNotPredicate(ruleName, ast, successLabel, failLabel); break;
                case "RuleRef": this.onRuleRef(ruleName, ast, successLabel, failLabel); break;
                case "Labeled": this.onLabeled(ruleName, ast, successLabel, failLabel); break;
                case "Action": this.onAction(ruleName, ast, successLabel, failLabel); break;
                case "Text": this.onText(ruleName, ast, successLabel, failLabel); break;
                case "Position": this.onPosition(ruleName, ast, successLabel, failLabel); break;
                default: throw new Error(`Ast.Type "${ast}" is not supported in Rule ${ruleName}`);
            }
        }

        private onChar(ruleName: string, ast: Ast.Char, successLabel: number, failLabel: number): void {
            this.Char(ast.char, successLabel, failLabel);
        }

        private onCharClass(ruleName: string, ast: Ast.CharClass, successLabel: number, failLabel: number): void {
            this.CharClass(ast.inverted, ast.parts, ast.ignoreCase, successLabel, failLabel);
        }

        private onAnyChar(ruleName: string, ast: Ast.AnyChar, successLabel: number, failLabel: number): void {
            this.AnyChar(successLabel, failLabel);
        }

        private onStr(ruleName: string, ast: Ast.Str, successLabel: number, failLabel: number): void {
            this.Str(ast.str, successLabel, failLabel);
        }

        private onSequence(ruleName: string, ast: Ast.Sequence, successLabel: number, failLabel: number): void {
            const junctionLabel = this.allocLabel();

            this.PushContext();
            this.PushArray();

            for (const child of ast.childs) {
                const nextLabel = this.allocLabel();
                this.visit(ruleName, child, nextLabel, junctionLabel);
                this.Label(nextLabel);
                this.Append();
            }
            this.Nip();
            this.Jump(successLabel);
            this.Label(junctionLabel);

            this.Pop();
            this.PopContext();
            this.Jump(failLabel);
        }

        private onChoice(ruleName: string, ast: Ast.Choice, successLabel: number, failLabel: number): void {
            for (const child of ast.childs) {
                const nextLabel = this.allocLabel();
                this.visit(ruleName, child, successLabel, nextLabel);
                this.Label(nextLabel);
            }
            this.Jump(failLabel);
        }

        private onOptional(ruleName: string, ast: Ast.Optional, successLabel: number, failLabel: number): void {
            const succLabel = this.allocLabel();
            const junctionLabel = this.allocLabel();
            this.PushContext();
            this.visit(ruleName, ast.child, succLabel, junctionLabel);
            this.Label(succLabel);
            this.Nip();
            this.Jump(successLabel);
            this.Label(junctionLabel);
            this.PopContext();
            this.PushNull();
            this.Jump(successLabel);
        }

        private onGroup(ruleName: string, ast: Ast.Group, successLabel: number, failLabel: number): void {
            const succLabel = this.allocLabel();
            const junctionLabel = this.allocLabel();
            this.pushCaptureScope();

            this.PushCapture();
            this.visit(ruleName, ast.child, succLabel , junctionLabel);
            this.Label(succLabel);
            this.PopCapture();
            this.Jump(successLabel);
            this.Label(junctionLabel);
            this.PopCapture();
            this.Jump(failLabel);

            this.popCaptureScope();
        }

        private onZeroOrMore(ruleName: string, ast: Ast.ZeroOrMore, successLabel: number, failLabel: number): void {
            const loopLabel = this.allocLabel();
            const succLabel = this.allocLabel();
            const junctionLabel = this.allocLabel();
            this.PushArray();
            this.Label(loopLabel);
            this.PushContext();
            this.visit(ruleName, ast.child, succLabel, junctionLabel);
            this.Label(succLabel);
            this.Nip();
            this.Append();
            this.Jump(loopLabel);
            this.Label(junctionLabel);
            this.PopContext();
            this.Jump(successLabel);
        }

        private onOneOrMore(ruleName: string, ast: Ast.OneOrMore, successLabel: number, failLabel: number): void {
            const rollbackLabel = this.allocLabel();
            const loopLabel = this.allocLabel();
            const junctionLabel = this.allocLabel();
            this.PushArray();
            this.PushContext();
            this.visit(ruleName, ast.child, loopLabel, rollbackLabel);
            this.Label(rollbackLabel);
            this.PopContext();
            this.Pop();
            this.Jump(failLabel);
            this.Label(loopLabel);
            this.Nip();
            this.Append();
            this.PushContext();
            this.visit(ruleName, ast.child, loopLabel, junctionLabel);
            this.Label(junctionLabel);
            this.PopContext();
            this.Jump(successLabel);
        }

        private onAndPredicate(ruleName: string, ast: Ast.AndPredicate, successLabel: number, failLabel: number): void {
            const junctionLabel = this.allocLabel();
            const junctionLabel2 = this.allocLabel();
            this.PushContext();
            this.visit(ruleName, ast.child, junctionLabel, junctionLabel2);
            this.Label(junctionLabel);
            this.Pop();
            this.PopContext();
            this.PushNull();
            this.Jump(successLabel);
            this.Label(junctionLabel2);
            this.PopContext();
            this.Jump(failLabel);
        }

        private onNotPredicate(ruleName: string, ast: Ast.NotPredicate, successLabel: number, failLabel: number): void {
            const junctionLabel = this.allocLabel();
            const junctionLabel2 = this.allocLabel();
            this.PushContext();
            this.visit(ruleName, ast.child, junctionLabel2, junctionLabel);
            this.Label(junctionLabel2);
            this.Pop();
            this.PopContext();
            this.Jump(failLabel);
            this.Label(junctionLabel);
            this.PopContext();
            this.PushNull();
            this.Jump(successLabel);
        }

        private onRuleRef(ruleName: string, ast: Ast.RuleRef, successLabel: number, failLabel: number): void {
            const junctionLabel = this.allocLabel();
            const junctionLabel2 = this.allocLabel();
            this.PushContext();
            this.Call(ast.rule);
            this.Test(junctionLabel, junctionLabel2);
            this.Label(junctionLabel);
            this.Nip();
            this.Jump(successLabel);
            this.Label(junctionLabel2);
            this.PopContext();
            this.Jump(failLabel);
        }

        private onLabeled(ruleName: string, ast: Ast.Labeled, successLabel: number, failLabel: number): void {
            const junctionLabel = this.allocLabel();
            this.visit(ruleName, ast.child, junctionLabel, failLabel);
            this.Label(junctionLabel);
            this.Capture(ast.name);
            this.Jump(successLabel);
            this.addCaptureLabel(ast.name);
        }

        private onAction(ruleName: string, ast: Ast.Action, successLabel: number, failLabel: number): void {
            const junctionLabel = this.allocLabel();
            this.visit(ruleName, ast.child, junctionLabel, failLabel);
            this.Label(junctionLabel);
            this.Action(ast.code, this.currentCaptures());
            this.Jump(successLabel);
        }

        private onText(ruleName: string, ast: Ast.Text, successLabel: number, failLabel: number): void {
            const junctionLabel = this.allocLabel();
            const junctionLabel2 = this.allocLabel();
            this.PushContext();
            this.visit(ruleName, ast.child, junctionLabel, junctionLabel2);
            this.Label(junctionLabel);
            this.Pop();
            this.Text();
            this.Jump(successLabel);
            this.Label(junctionLabel2);
            this.Pop();
            this.Jump(failLabel);
        }

        private onPosition(ruleName: string, ast: Ast.Position, successLabel: number, failLabel: number): void {
            this.PushPosition();
        }

        public static compile(grammar: Ast.Grammar): IR[] {
            let compiler = new Compiler();
            return compiler.compileGrammar(grammar);
        }
    }

    export module Runtime {


        type Cell = Cont | Ret | Cons | Atom;
        interface Cont { type: "cont", context: Context };
        interface Ret { type: "ret", flag: boolean, value: Cell };
        interface Cons { type: "cons", car: Cell, cdr: Cons };
        interface Atom { type: "atom", value: any };

        type Dic = { key: string, value: Cell, next: Dic | null }

        type Env = { pc: number, capture: Dic, sp: number, next: Env | null }
        type CharRangePart = { begin: string, end: string };
        type Position = { index: number, line: number, column: number };

        interface Context {
            pc: number;
            pos: Position;
            capture: Dic;
            sp: number;
            env: Env
        };

        export type Parser = (ruleName: string, input: string) => any;

        const generateParser: (ir: IR[]) => Parser = function (ir: IR[]): Parser {
            const ruleTable = ir.reduce((s, x, i) => { if (x.type == "Rule") { s[x.name] = i; } return s; }, <{ [key: string]: number }>{});
            const labelTable = ir.reduce((s, x, i) => { if (x.type == "Label") { s[x.id] = i; } return s; }, <{ [key: string]: number }>{});

            function cons(a: Cell, d: Cons): Cons {
                if (d == null || d.type != "cons") { throw new Error("not cons"); }
                return { type: "cons", car: a, cdr: d };
            }

            const Nil: Cons = Object.freeze(<Cons>{ type: "cons", car: null, cdr: null });

            function atom(v: any): Atom {
                return { type: "atom", value: v };
            }

            function cont(c: Context): Cont {
                return { type: "cont", context: { pc: c.pc, pos: c.pos, capture: c.capture, sp: c.sp, env: c.env } };
            }

            function ret(flag: boolean, value: Cell): Ret {
                return { type: "ret", flag: flag, value: value };
            }

            function assoc(key: string, dic: Dic): Cell {
                while (dic != null) {
                    if (dic.key == key) { return dic.value; }
                    dic = dic.next;
                }
                return undefined;
            }

            function add(key: string, value: Cell, dic: Dic): Dic {
                return { key: key, value: value, next: dic };
            }

            function decode(v: Cell): any {
                if (v.type == "atom") { return v.value; }
                if (v.type == "cons") {
                    const ret = [];
                    while (v != Nil) {
                        ret.push(decode(v.car));
                        v = v.cdr;
                    }
                    return ret.reverse();
                }
                throw new Error();
            }

            function isCharClass(char: string, inverted: boolean, parts: CharRangePart[], ignoreCase: boolean) {
                if (char == undefined) { return false; }
                let ret = false;
                if (ignoreCase) {
                    const charCode = char.toLowerCase().charCodeAt(0);
                    ret = parts.some(x => x.begin.toLowerCase().charCodeAt(0) <= charCode && charCode <= x.end.toLowerCase().charCodeAt(0));
                } else {
                    const charCode = char.charCodeAt(0);
                    ret = parts.some(x => x.begin.charCodeAt(0) <= charCode && charCode <= x.end.charCodeAt(0));
                }
                if (inverted) { ret = !ret; }
                return ret;
            }

            function incPos(pos: Position, v: string) {
                let { index: index, line: line, column: column } = pos;
                let n = 0;
                while (v.length > n) {
                    switch (v[n]) {
                        case "\r": {
                            if (v.length > n + 1 && v[n + 1] == "\n") {
                                n += 2; line += 1; column = 1;
                                continue;
                            } else {
                                n += 1; line += 1; column = 1;
                                continue;
                            }
                        }
                        case "\n": {
                            n += 1; line += 1; column = 1;
                            continue;
                        }
                        default: {
                            n += 1;
                            column += 1;
                            continue;
                        }
                    }
                }
                return { index: index + n, line: line, column: column };
            }

            function step(insts: PegKit.IR[], stack: Cell[], str: string, context: Context): Context {
                let { pc: pc, pos: pos, capture: capture, sp: sp, env: env } = context;

                const ir = insts[pc];
                switch (ir.type) {
                    case "Char": {
                        if (pos.index < 0 || str.length <= pos.index) {
                            pc = labelTable[ir.failLabel];
                            return { pc: pc, pos: pos, capture: capture, sp: sp, env: env };
                        }
                        const v = str[pos.index]
                        if (v == ir.char) {
                            stack[sp] = atom(v);
                            sp = sp + 1;
                            pos = incPos(pos, v);
                            pc = labelTable[ir.successLabel];
                        } else {
                            pc = labelTable[ir.failLabel];
                        }
                        return { pc: pc, pos: pos, capture: capture, sp: sp, env: env };
                    }
                    case "AnyChar": {
                        if (pos.index < 0 || str.length <= pos.index) {
                            pc = labelTable[ir.failLabel];
                            return { pc: pc, pos: pos, capture: capture, sp: sp, env: env };
                        }
                        const v = str[pos.index];
                        stack[sp] = atom(v);
                        sp += 1;
                        pos = incPos(pos, v);
                        pc = labelTable[ir.successLabel];
                        return { pc: pc, pos: pos, capture: capture, sp: sp, env: env };
                    }
                    case "CharClass": {
                        if (pos.index < 0 || str.length <= pos.index) {
                            pc = labelTable[ir.failLabel];
                            return { pc: pc, pos: pos, capture: capture, sp: sp, env: env };
                        }
                        const v = str[pos.index]
                        if (isCharClass(v, ir.inverted, ir.parts, ir.ignoreCase)) {
                            stack[sp] = atom(v);
                            sp += 1;
                            pos = incPos(pos, v);
                            pc = labelTable[ir.successLabel];
                        } else {
                            pc = labelTable[ir.failLabel];
                        }
                        return { pc: pc, pos: pos, capture: capture, sp: sp, env: env };
                    }
                    case "Str": {
                        if (pos.index < 0 || str.length <= pos.index + ir.str.length) {
                            pc = labelTable[ir.failLabel];
                            return { pc: pc, pos: pos, capture: capture, sp: sp, env: env };
                        }
                        const v = str.substr(pos.index, ir.str.length);
                        if (v == ir.str) {
                            stack[sp] = atom(v);
                            sp += 1;
                            pos = incPos(pos, v);
                            pc = labelTable[ir.successLabel];
                        } else {
                            pc = labelTable[ir.failLabel];
                        }
                        return { pc: pc, pos: pos, capture: capture, sp: sp, env: env };
                    }
                    case "Label":
                    case "Comment":
                    case "Rule": {
                        return { pc: pc + 1, pos: pos, capture: capture, sp: sp, env: env };
                    }
                    case "Nip": {
                        stack[sp - 2] = stack[sp - 1];
                        return { pc: pc + 1, pos: pos, capture: capture, sp: sp - 1, env: env };
                    }
                    case "Jump": {
                        return { pc: labelTable[ir.id], pos: pos, capture: capture, sp: sp, env: env };
                    }
                    case "Call": {
                        return { pc: ruleTable[ir.name], pos: pos, capture: null, sp: sp, env: { pc: pc + 1, capture: capture, sp: sp, next: env } };
                    }
                    case "PushContext": {
                        stack[sp] = cont(context);
                        return { pc: pc + 1, pos: pos, capture: capture, sp: sp + 1, env: env };
                    }
                    case "PushNull": {
                        stack[sp] = atom(null);
                        return { pc: pc + 1, pos: pos, capture: capture, sp: sp + 1, env: env };
                    }
                    case "PopContext": {
                        const ctx = stack[sp - 1];
                        if (ctx.type != "cont") {
                            throw new Error("stack value is not context.");
                        }
                        let { pc: pc2, pos: pos2, capture: capture2, sp: sp2, env: env2 } = ctx.context;
                        return { pc: pc + 1, pos: pos2, capture: capture2, sp: sp2, env: env };
                    }
                    case "Pop": {
                        return { pc: pc + 1, pos: pos, capture: capture, sp: sp - 1, env: env };
                    }
                    case "Append": {
                        const list = stack[sp - 2];
                        if (list.type != "cons") { throw new Error(); }
                        stack[sp - 2] = cons(stack[sp - 1], list);
                        return { pc: pc + 1, pos: pos, capture: capture, sp: sp - 1, env: env };
                    }
                    case "Capture": {
                        return { pc: pc + 1, pos: pos, capture: add(ir.name, stack[sp - 1], capture), sp: sp, env: env };
                    }
                    case "PushCapture": {
                        return { pc: pc + 1, pos: pos, capture: null, sp: sp, env: { pc: 0, capture: capture, sp: 0, next: env } };
                    }
                    case "PopCapture": {
                        const { pc: retpc, capture: retcap, sp: retstack, next: next } = env;
                        return { pc: pc + 1, pos: pos, capture: retcap, sp: sp, env: next };
                    }
                    case "Text": {
                        const ctx = stack[sp - 1];
                        if (ctx.type != "cont") {
                            throw new Error();
                        }
                        const { pc: pc2, pos: pos2, capture: capture2, sp: sp2, env: env } = ctx.context;
                        const sub = str.substr(pos2.index, pos.index - pos2.index);
                        stack[sp - 1] = atom(sub);
                        return { pc: pc + 1, pos: pos, capture: capture, sp: sp, env: env };
                    }
                    case "Action": {
                        const pred = eval(`(function() { return function (${ir.captures.join(", ")}) { ${ir.code} }; })()`);
                        const args = ir.captures.map(x => {
                            const v = assoc(x, capture);
                            if (v == undefined) {
                                console.log(capture, ir);
                                throw new Error(`${x} is not captured.`);
                            }
                            return decode(v);
                        });
                        const ret = pred(...args);
                        stack[sp - 1] = atom(ret);
                        return { pc: pc + 1, pos: pos, capture: capture, sp: sp, env: env };
                    }
                    case "PushArray": {
                        stack[sp] = Nil;
                        return { pc: pc + 1, pos: pos, capture: capture, sp: sp + 1, env: env };
                    }
                    case "PushPosition": {
                        stack[sp] = atom({ index: pos.index, line: pos.line, column: pos.column });
                        return { pc: pc + 1, pos: pos, capture: capture, sp: sp + 1, env: env };
                    }
                    case "Test": {
                        const ret = stack[sp - 1];
                        if (ret.type != "ret") {
                            throw new Error();
                        }
                        if (ret.flag) {
                            pc = labelTable[ir.successLabel];
                            stack[sp - 1] = ret.value;
                            return { pc: pc + 1, pos: pos, capture: capture, sp: sp, env: env };
                        } else {
                            pc = labelTable[ir.failLabel];
                            return { pc: pc + 1, pos: pos, capture: capture, sp: sp - 1, env: env };
                        }
                    }
                    case "Return": {
                        const { pc: retpc, capture: retcap, sp: retstack, next: next } = env;
                        if (ir.success) {
                            const result = stack[sp - 1];
                            stack[retstack] = ret(true, result);
                            return { pc: retpc, pos: pos, capture: retcap, sp: retstack + 1, env: next };
                        } else {
                            stack[retstack] = ret(false, null);
                            return { pc: retpc, pos: pos, capture: retcap, sp: retstack + 1, env: next };
                        }
                    }
                    default: {
                        throw new Error();
                    }
                }
            }

            return function (ruleName, str) {
                let context: Context = { pc: ruleTable[ruleName], pos: { index: 0, line: 1, column: 1 }, capture: null, sp: 0, env: { pc: -1, capture: null, sp: 0, next: null } };
                const stack: Cell[] = [];
                for (let i = 0; context.pc >= 0; i++) {
                    context = step(ir, stack, str, context)
                    if (i >= 500000) {
                        throw new Error("loop limit.")
                    }
                }
                {
                    const { pc: pc, pos: pos, capture: capture, sp: sp, env: env } = context;
                    const v = stack[sp - 1];
                    if (v.type != "ret") {
                        throw new Error();
                    }
                    if (v.flag) {
                        return decode(v.value);
                    } else {
                        return undefined;
                    }
                }
            };

        }

        export function generateParserCode(ir: IR[]): string {
            return `
(function () {
    const generateParser = ${generateParser.toString()};
    const ir = ${JSON.stringify(ir, null, 4)};
    return generateParser(ir);
})();
`
        }
    }

    export module Ast {
        export type Type
            = Char
            | CharClass
            | AnyChar
            | Str
            | Sequence
            | Choice
            | Optional
            | Group
            | ZeroOrMore
            | OneOrMore
            | AndPredicate
            | NotPredicate
            | RuleRef
            | Labeled
            | Action
            | Text
            | Position
            ;

        export type Char = { type: "Char", char: string };
        export type CharClass = { type: "CharClass", inverted: boolean, parts: { begin: string, end: string }[], ignoreCase: boolean };
        export type AnyChar = { type: "AnyChar" };
        export type Str = { type: "Str", str: string };
        export type Sequence = { type: "Sequence", childs: Ast.Type[] };
        export type Choice = { type: "Choice", childs: Ast.Type[] };
        export type Optional = { type: "Optional", child: Ast.Type };
        export type Group = { type: "Group", child: Ast.Type };
        export type ZeroOrMore = { type: "ZeroOrMore", child: Ast.Type };
        export type OneOrMore = { type: "OneOrMore", child: Ast.Type };
        export type AndPredicate = { type: "AndPredicate", child: Ast.Type };
        export type NotPredicate = { type: "NotPredicate", child: Ast.Type };
        export type RuleRef = { type: "RuleRef", rule: string };
        export type Labeled = { type: "Labeled", name: string, child: Ast.Type };
        export type Action = { type: "Action", child: Ast.Type, code: string };
        export type Text = { type: "Text", child: Ast.Type };
        export type Position = { type: "Position" };

        export type Grammar = { [key: string]: Ast.Type };

    }

    export module Parser {
        const builtinGrammar: PegKit.Ast.Grammar =
        {
            "default": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Labeled",
                            "name": "g",
                            "child": {
                                "type": "RuleRef",
                                "rule": "Grammer"
                            }
                        },
                        "code": " console.log(JSON.stringify(g)); return g; "
                    }
                ]
            },
            "Grammer": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "RuleRef",
                                    "rule": "__"
                                },
                                {
                                    "type": "Labeled",
                                    "name": "xs",
                                    "child": {
                                        "type": "OneOrMore",
                                        "child": {
                                            "type": "Group",
                                            "child": {
                                                "type": "Choice",
                                                "childs": [
                                                    {
                                                        "type": "Action",
                                                        "child": {
                                                            "type": "Sequence",
                                                            "childs": [
                                                                {
                                                                    "type": "Labeled",
                                                                    "name": "r",
                                                                    "child": {
                                                                        "type": "RuleRef",
                                                                        "rule": "Rule"
                                                                    }
                                                                },
                                                                {
                                                                    "type": "RuleRef",
                                                                    "rule": "__"
                                                                }
                                                            ]
                                                        },
                                                        "code": " return r; "
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                }
                            ]
                        },
                        "code": " return xs.reduce((s,[name,body]) => { s[name] = body; return s; }, {}); "
                    }
                ]
            },
            "Rule": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Labeled",
                                    "name": "ruleName",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "Identifier"
                                    }
                                },
                                {
                                    "type": "RuleRef",
                                    "rule": "__"
                                },
                                {
                                    "type": "Str",
                                    "str": "="
                                },
                                {
                                    "type": "RuleRef",
                                    "rule": "__"
                                },
                                {
                                    "type": "Labeled",
                                    "name": "ruleBody",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "Expression"
                                    }
                                },
                                {
                                    "type": "RuleRef",
                                    "rule": "EOS"
                                }
                            ]
                        },
                        "code": " return [ruleName, ruleBody]; "
                    }
                ]
            },
            "Expression": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "RuleRef",
                        "rule": "ChoiceExpression"
                    }
                ]
            },
            "ChoiceExpression": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Labeled",
                                    "name": "x",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "ActionExpression"
                                    }
                                },
                                {
                                    "type": "Labeled",
                                    "name": "xs",
                                    "child": {
                                        "type": "ZeroOrMore",
                                        "child": {
                                            "type": "Group",
                                            "child": {
                                                "type": "Choice",
                                                "childs": [
                                                    {
                                                        "type": "Action",
                                                        "child": {
                                                            "type": "Sequence",
                                                            "childs": [
                                                                {
                                                                    "type": "RuleRef",
                                                                    "rule": "__"
                                                                },
                                                                {
                                                                    "type": "Str",
                                                                    "str": "/"
                                                                },
                                                                {
                                                                    "type": "RuleRef",
                                                                    "rule": "__"
                                                                },
                                                                {
                                                                    "type": "Labeled",
                                                                    "name": "e",
                                                                    "child": {
                                                                        "type": "RuleRef",
                                                                        "rule": "ActionExpression"
                                                                    }
                                                                }
                                                            ]
                                                        },
                                                        "code": " return e; "
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                }
                            ]
                        },
                        "code": " return { type: \"Choice\", childs: xs.reduce((s,x) => { s.push(x); return s; },[x]) }; "
                    }
                ]
            },
            "ActionExpression": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Labeled",
                                    "name": "expr",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "SequenceExpression"
                                    }
                                },
                                {
                                    "type": "Labeled",
                                    "name": "code",
                                    "child": {
                                        "type": "Optional",
                                        "child": {
                                            "type": "Group",
                                            "child": {
                                                "type": "Choice",
                                                "childs": [
                                                    {
                                                        "type": "Action",
                                                        "child": {
                                                            "type": "Sequence",
                                                            "childs": [
                                                                {
                                                                    "type": "RuleRef",
                                                                    "rule": "_"
                                                                },
                                                                {
                                                                    "type": "Labeled",
                                                                    "name": "x",
                                                                    "child": {
                                                                        "type": "RuleRef",
                                                                        "rule": "CodeBlock"
                                                                    }
                                                                }
                                                            ]
                                                        },
                                                        "code": " return x; "
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                }
                            ]
                        },
                        "code": " return (code == null) ? expr : { type: \"Action\", child: expr, code: code }; "
                    }
                ]
            },
            "SequenceExpression": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Labeled",
                                    "name": "x",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "LabeledExpression"
                                    }
                                },
                                {
                                    "type": "Labeled",
                                    "name": "xs",
                                    "child": {
                                        "type": "ZeroOrMore",
                                        "child": {
                                            "type": "Group",
                                            "child": {
                                                "type": "Choice",
                                                "childs": [
                                                    {
                                                        "type": "Action",
                                                        "child": {
                                                            "type": "Sequence",
                                                            "childs": [
                                                                {
                                                                    "type": "RuleRef",
                                                                    "rule": "_"
                                                                },
                                                                {
                                                                    "type": "Labeled",
                                                                    "name": "e",
                                                                    "child": {
                                                                        "type": "RuleRef",
                                                                        "rule": "LabeledExpression"
                                                                    }
                                                                }
                                                            ]
                                                        },
                                                        "code": " return e; "
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                }
                            ]
                        },
                        "code": " return (xs.length == 0) ? x : { type: \"Sequence\", childs: xs.reduce((s,x) => { s.push(x); return s; },[x]) }; "
                    }
                ]
            },
            "LabeledExpression": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Labeled",
                                    "name": "label",
                                    "child": {
                                        "type": "Optional",
                                        "child": {
                                            "type": "Group",
                                            "child": {
                                                "type": "Choice",
                                                "childs": [
                                                    {
                                                        "type": "Action",
                                                        "child": {
                                                            "type": "Sequence",
                                                            "childs": [
                                                                {
                                                                    "type": "Labeled",
                                                                    "name": "name",
                                                                    "child": {
                                                                        "type": "RuleRef",
                                                                        "rule": "Identifier"
                                                                    }
                                                                },
                                                                {
                                                                    "type": "RuleRef",
                                                                    "rule": "_"
                                                                },
                                                                {
                                                                    "type": "Str",
                                                                    "str": ":"
                                                                },
                                                                {
                                                                    "type": "RuleRef",
                                                                    "rule": "_"
                                                                }
                                                            ]
                                                        },
                                                        "code": " return name; "
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                },
                                {
                                    "type": "Labeled",
                                    "name": "expression",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "PrefixedExpression"
                                    }
                                }
                            ]
                        },
                        "code": " return (label == null) ? expression : { type: \"Labeled\", name: label, child: expression }; "
                    }
                ]
            },
            "PrefixedExpression": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Labeled",
                                    "name": "operator",
                                    "child": {
                                        "type": "Optional",
                                        "child": {
                                            "type": "Group",
                                            "child": {
                                                "type": "Choice",
                                                "childs": [
                                                    {
                                                        "type": "Action",
                                                        "child": {
                                                            "type": "Sequence",
                                                            "childs": [
                                                                {
                                                                    "type": "Labeled",
                                                                    "name": "x",
                                                                    "child": {
                                                                        "type": "RuleRef",
                                                                        "rule": "PrefixedOperator"
                                                                    }
                                                                },
                                                                {
                                                                    "type": "RuleRef",
                                                                    "rule": "_"
                                                                }
                                                            ]
                                                        },
                                                        "code": " return x; "
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                },
                                {
                                    "type": "Labeled",
                                    "name": "expression",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "SuffixedExpression"
                                    }
                                }
                            ]
                        },
                        "code": " return (operator == null) ? expression : { type: operator, child: expression }; "
                    }
                ]
            },
            "PrefixedOperator": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "&"
                        },
                        "code": " return \"AndPredicate\"; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "!"
                        },
                        "code": " return \"NotPredicate\"; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "$"
                        },
                        "code": " return \"Text\"; "
                    }
                ]
            },
            "SuffixedExpression": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Labeled",
                                    "name": "expression",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "PrimaryExpression"
                                    }
                                },
                                {
                                    "type": "Labeled",
                                    "name": "operator",
                                    "child": {
                                        "type": "Optional",
                                        "child": {
                                            "type": "Group",
                                            "child": {
                                                "type": "Choice",
                                                "childs": [
                                                    {
                                                        "type": "Action",
                                                        "child": {
                                                            "type": "Sequence",
                                                            "childs": [
                                                                {
                                                                    "type": "RuleRef",
                                                                    "rule": "_"
                                                                },
                                                                {
                                                                    "type": "Labeled",
                                                                    "name": "x",
                                                                    "child": {
                                                                        "type": "RuleRef",
                                                                        "rule": "SuffixedOperator"
                                                                    }
                                                                }
                                                            ]
                                                        },
                                                        "code": " return x; "
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                }
                            ]
                        },
                        "code": " return (operator == null) ? expression : { type: operator, child: expression }; "
                    }
                ]
            },
            "SuffixedOperator": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "?"
                        },
                        "code": " return \"Optional\"; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "*"
                        },
                        "code": " return \"ZeroOrMore\"; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "+"
                        },
                        "code": " return \"OneOrMore\"; "
                    }
                ]
            },
            "PrimaryExpression": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "RuleRef",
                        "rule": "LiteralMatcher"
                    },
                    {
                        "type": "RuleRef",
                        "rule": "CharacterClassMatcher"
                    },
                    {
                        "type": "RuleRef",
                        "rule": "AnyMatcher"
                    },
                    {
                        "type": "RuleRef",
                        "rule": "AnyMatcher"
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "#"
                        },
                        "code": " return { type: \"Position\" }; "
                    },
                    {
                        "type": "RuleRef",
                        "rule": "RuleReferenceExpression"
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Str",
                                    "str": "("
                                },
                                {
                                    "type": "RuleRef",
                                    "rule": "_"
                                },
                                {
                                    "type": "Labeled",
                                    "name": "e",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "Expression"
                                    }
                                },
                                {
                                    "type": "RuleRef",
                                    "rule": "_"
                                },
                                {
                                    "type": "Str",
                                    "str": ")"
                                }
                            ]
                        },
                        "code": " return { type: \"Group\", child:e }; "
                    }
                ]
            },
            "LiteralMatcher": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Labeled",
                            "name": "x",
                            "child": {
                                "type": "RuleRef",
                                "rule": "StringLiteral"
                            }
                        },
                        "code": " return { type: \"Str\", str: x }; "
                    }
                ]
            },
            "StringLiteral": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Str",
                                    "str": "\""
                                },
                                {
                                    "type": "Labeled",
                                    "name": "chars",
                                    "child": {
                                        "type": "ZeroOrMore",
                                        "child": {
                                            "type": "RuleRef",
                                            "rule": "DoubleStringCharacter"
                                        }
                                    }
                                },
                                {
                                    "type": "Str",
                                    "str": "\""
                                }
                            ]
                        },
                        "code": " return chars.join(\"\"); "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Str",
                                    "str": "'"
                                },
                                {
                                    "type": "Labeled",
                                    "name": "chars",
                                    "child": {
                                        "type": "ZeroOrMore",
                                        "child": {
                                            "type": "RuleRef",
                                            "rule": "SingleStringCharacter"
                                        }
                                    }
                                },
                                {
                                    "type": "Str",
                                    "str": "'"
                                }
                            ]
                        },
                        "code": " return chars.join(\"\"); "
                    }
                ]
            },
            "DoubleStringCharacter": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "NotPredicate",
                                    "child": {
                                        "type": "Group",
                                        "child": {
                                            "type": "Choice",
                                            "childs": [
                                                {
                                                    "type": "Str",
                                                    "str": "\""
                                                },
                                                {
                                                    "type": "Str",
                                                    "str": "\\"
                                                }
                                            ]
                                        }
                                    }
                                },
                                {
                                    "type": "Labeled",
                                    "name": "x",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "SourceCharacter"
                                    }
                                }
                            ]
                        },
                        "code": " return x; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Str",
                                    "str": "\\"
                                },
                                {
                                    "type": "Labeled",
                                    "name": "x",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "EscapeSequence"
                                    }
                                }
                            ]
                        },
                        "code": " return x; "
                    }
                ]
            },
            "SingleStringCharacter": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "NotPredicate",
                                    "child": {
                                        "type": "Group",
                                        "child": {
                                            "type": "Choice",
                                            "childs": [
                                                {
                                                    "type": "Str",
                                                    "str": "'"
                                                },
                                                {
                                                    "type": "Str",
                                                    "str": "\\"
                                                },
                                                {
                                                    "type": "RuleRef",
                                                    "rule": "LineTerminator"
                                                }
                                            ]
                                        }
                                    }
                                },
                                {
                                    "type": "Labeled",
                                    "name": "x",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "SourceCharacter"
                                    }
                                }
                            ]
                        },
                        "code": " return x; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Str",
                                    "str": "\\"
                                },
                                {
                                    "type": "Labeled",
                                    "name": "x",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "SourceCharacter"
                                    }
                                }
                            ]
                        },
                        "code": " return x; "
                    },
                    {
                        "type": "RuleRef",
                        "rule": "LineContinuation"
                    }
                ]
            },
            "EscapeSequence": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "'"
                        },
                        "code": " return \"'\";  "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "\""
                        },
                        "code": " return \"\\\"\"; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "\\"
                        },
                        "code": " return \"\\\\\"; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "b"
                        },
                        "code": " return \"\\b\"; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "f"
                        },
                        "code": " return \"\\f\"; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "n"
                        },
                        "code": " return \"\\n\"; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "r"
                        },
                        "code": " return \"\\r\"; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "t"
                        },
                        "code": " return \"\\t\"; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "v"
                        },
                        "code": " return \"\\v\"; "
                    }
                ]
            },
            "CharacterClassMatcher": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Str",
                                    "str": "["
                                },
                                {
                                    "type": "Labeled",
                                    "name": "inverted",
                                    "child": {
                                        "type": "Optional",
                                        "child": {
                                            "type": "Str",
                                            "str": "^"
                                        }
                                    }
                                },
                                {
                                    "type": "Labeled",
                                    "name": "parts",
                                    "child": {
                                        "type": "ZeroOrMore",
                                        "child": {
                                            "type": "RuleRef",
                                            "rule": "CharacterPart"
                                        }
                                    }
                                },
                                {
                                    "type": "Str",
                                    "str": "]"
                                },
                                {
                                    "type": "Labeled",
                                    "name": "ignoreCase",
                                    "child": {
                                        "type": "Optional",
                                        "child": {
                                            "type": "Str",
                                            "str": "i"
                                        }
                                    }
                                }
                            ]
                        },
                        "code": " return { type:\"CharClass\", inverted: inverted, parts:parts, ignoreCase:ignoreCase }; "
                    }
                ]
            },
            "CharacterPart": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "RuleRef",
                        "rule": "ClassCharacterRange"
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Labeled",
                            "name": "x",
                            "child": {
                                "type": "RuleRef",
                                "rule": "ClassCharacter"
                            }
                        },
                        "code": " return { begin:x, end:x}; "
                    }
                ]
            },
            "ClassCharacterRange": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Labeled",
                                    "name": "begin",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "ClassCharacter"
                                    }
                                },
                                {
                                    "type": "Str",
                                    "str": "-"
                                },
                                {
                                    "type": "Labeled",
                                    "name": "end",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "ClassCharacter"
                                    }
                                }
                            ]
                        },
                        "code": " return { begin:begin, end:end }; "
                    }
                ]
            },
            "ClassCharacter": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "NotPredicate",
                                    "child": {
                                        "type": "Group",
                                        "child": {
                                            "type": "Choice",
                                            "childs": [
                                                {
                                                    "type": "Str",
                                                    "str": "]"
                                                },
                                                {
                                                    "type": "Str",
                                                    "str": "\\"
                                                },
                                                {
                                                    "type": "RuleRef",
                                                    "rule": "LineTerminator"
                                                }
                                            ]
                                        }
                                    }
                                },
                                {
                                    "type": "Labeled",
                                    "name": "x",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "SourceCharacter"
                                    }
                                }
                            ]
                        },
                        "code": " return x; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Str",
                                    "str": "\\"
                                },
                                {
                                    "type": "Labeled",
                                    "name": "x",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "EscapeSequence"
                                    }
                                }
                            ]
                        },
                        "code": " return x; "
                    },
                    {
                        "type": "RuleRef",
                        "rule": "LineContinuation"
                    }
                ]
            },
            "LineContinuation": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Str",
                                    "str": "\\"
                                },
                                {
                                    "type": "RuleRef",
                                    "rule": "LineTerminatorSequence"
                                }
                            ]
                        },
                        "code": " return \"\"; "
                    }
                ]
            },
            "AnyMatcher": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "."
                        },
                        "code": " return { type: \"AnyChar\" }; "
                    }
                ]
            },
            "RuleReferenceExpression": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Labeled",
                            "name": "name",
                            "child": {
                                "type": "RuleRef",
                                "rule": "Identifier"
                            }
                        },
                        "code": " return { type: \"RuleRef\", rule: name }; "
                    }
                ]
            },
            "CodeBlock": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Str",
                                    "str": "{"
                                },
                                {
                                    "type": "Labeled",
                                    "name": "x",
                                    "child": {
                                        "type": "RuleRef",
                                        "rule": "Code"
                                    }
                                },
                                {
                                    "type": "Str",
                                    "str": "}"
                                }
                            ]
                        },
                        "code": " return x; "
                    },
                    {
                        "type": "Action",
                        "child": {
                            "type": "Str",
                            "str": "{"
                        },
                        "code": " error(\"Unbalanced brace.\"); "
                    }
                ]
            },
            "Code": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Text",
                        "child": {
                            "type": "ZeroOrMore",
                            "child": {
                                "type": "Group",
                                "child": {
                                    "type": "Choice",
                                    "childs": [
                                        {
                                            "type": "OneOrMore",
                                            "child": {
                                                "type": "Group",
                                                "child": {
                                                    "type": "Choice",
                                                    "childs": [
                                                        {
                                                            "type": "Sequence",
                                                            "childs": [
                                                                {
                                                                    "type": "NotPredicate",
                                                                    "child": {
                                                                        "type": "CharClass",
                                                                        "inverted": null,
                                                                        "parts": [
                                                                            {
                                                                                "begin": "{",
                                                                                "end": "{"
                                                                            },
                                                                            {
                                                                                "begin": "}",
                                                                                "end": "}"
                                                                            }
                                                                        ],
                                                                        "ignoreCase": null
                                                                    }
                                                                },
                                                                {
                                                                    "type": "RuleRef",
                                                                    "rule": "SourceCharacter"
                                                                }
                                                            ]
                                                        }
                                                    ]
                                                }
                                            }
                                        },
                                        {
                                            "type": "Sequence",
                                            "childs": [
                                                {
                                                    "type": "Str",
                                                    "str": "{"
                                                },
                                                {
                                                    "type": "RuleRef",
                                                    "rule": "Code"
                                                },
                                                {
                                                    "type": "Str",
                                                    "str": "}"
                                                }
                                            ]
                                        }
                                    ]
                                }
                            }
                        }
                    }
                ]
            },
            "SourceCharacter": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "AnyChar"
                    }
                ]
            },
            "LineTerminator": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "CharClass",
                        "inverted": null,
                        "parts": [
                            {
                                "begin": "\r",
                                "end": "\r"
                            },
                            {
                                "begin": "\n",
                                "end": "\n"
                            }
                        ],
                        "ignoreCase": null
                    }
                ]
            },
            "LineTerminatorSequence": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Sequence",
                        "childs": [
                            {
                                "type": "Optional",
                                "child": {
                                    "type": "Str",
                                    "str": "\r"
                                }
                            },
                            {
                                "type": "Str",
                                "str": "\n"
                            }
                        ]
                    }
                ]
            },
            "Identifier": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Action",
                        "child": {
                            "type": "Sequence",
                            "childs": [
                                {
                                    "type": "Labeled",
                                    "name": "x",
                                    "child": {
                                        "type": "CharClass",
                                        "inverted": null,
                                        "parts": [
                                            {
                                                "begin": "A",
                                                "end": "Z"
                                            },
                                            {
                                                "begin": "a",
                                                "end": "z"
                                            },
                                            {
                                                "begin": "_",
                                                "end": "_"
                                            }
                                        ],
                                        "ignoreCase": null
                                    }
                                },
                                {
                                    "type": "Labeled",
                                    "name": "xs",
                                    "child": {
                                        "type": "ZeroOrMore",
                                        "child": {
                                            "type": "CharClass",
                                            "inverted": null,
                                            "parts": [
                                                {
                                                    "begin": "A",
                                                    "end": "Z"
                                                },
                                                {
                                                    "begin": "a",
                                                    "end": "z"
                                                },
                                                {
                                                    "begin": "0",
                                                    "end": "9"
                                                },
                                                {
                                                    "begin": "_",
                                                    "end": "_"
                                                }
                                            ],
                                            "ignoreCase": null
                                        }
                                    }
                                }
                            ]
                        },
                        "code": " return String.prototype.concat(x,...xs); "
                    }
                ]
            },
            "__": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "ZeroOrMore",
                        "child": {
                            "type": "CharClass",
                            "inverted": null,
                            "parts": [
                                {
                                    "begin": " ",
                                    "end": " "
                                },
                                {
                                    "begin": "\r",
                                    "end": "\r"
                                },
                                {
                                    "begin": "\n",
                                    "end": "\n"
                                },
                                {
                                    "begin": "\t",
                                    "end": "\t"
                                }
                            ],
                            "ignoreCase": null
                        }
                    }
                ]
            },
            "_": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "ZeroOrMore",
                        "child": {
                            "type": "CharClass",
                            "inverted": null,
                            "parts": [
                                {
                                    "begin": " ",
                                    "end": " "
                                },
                                {
                                    "begin": "\t",
                                    "end": "\t"
                                }
                            ],
                            "ignoreCase": null
                        }
                    }
                ]
            },
            "EOS": {
                "type": "Choice",
                "childs": [
                    {
                        "type": "Sequence",
                        "childs": [
                            {
                                "type": "ZeroOrMore",
                                "child": {
                                    "type": "CharClass",
                                    "inverted": null,
                                    "parts": [
                                        {
                                            "begin": " ",
                                            "end": " "
                                        },
                                        {
                                            "begin": "\t",
                                            "end": "\t"
                                        }
                                    ],
                                    "ignoreCase": null
                                }
                            },
                            {
                                "type": "CharClass",
                                "inverted": null,
                                "parts": [
                                    {
                                        "begin": "\r",
                                        "end": "\r"
                                    },
                                    {
                                        "begin": "\n",
                                        "end": "\n"
                                    }
                                ],
                                "ignoreCase": null
                            }
                        ]
                    }
                ]
            }
        }            ;
        const builtinParser: Runtime.Parser = eval(Runtime.generateParserCode(Compiler.compile(builtinGrammar)));

        export function Parse(input: string): PegKit.Ast.Grammar {
            return builtinParser("default", input);
        }
    }
}

window.onload = () => {


    const domGrammar = <HTMLTextAreaElement>document.getElementById('grammar');
    const domAst = <HTMLTextAreaElement>document.getElementById('ast');
    const domCode = <HTMLTextAreaElement>document.getElementById('code');
    const domIr = <HTMLTextAreaElement>document.getElementById('ir');
    const domInput = <HTMLTextAreaElement>document.getElementById('input');
    const domOutput = <HTMLTextAreaElement>document.getElementById('output');

    function addChangeEvent<K extends keyof HTMLElementEventMap>(dom: HTMLElement, listener: (this: HTMLElement, ev: HTMLElementEventMap[K]) => any): void {
        const events = ["change", "mousedown", "mouseup", "click", "keydown", "keyup", "keypress"];
        events.forEach(x => dom.addEventListener(x, listener));
    }

    let oldGrammarValue: string = "";
    let oldAstValue: string = "";
    let oldCodeValue: string = "";
    let oldInputValue: string = "";
    let parseTimer: number | null = null;
    let compileTimer: number | null = null;
    let buildTimer: number | null = null;
    let runTimer: number | null = null;
    let parser: (ruleName: string, input: string) => any;

    function parse() {
        oldGrammarValue = domGrammar.value;
        const ret = PegKit.Parser.Parse(oldGrammarValue);
        if (ret != undefined) {
            domAst.value = JSON.stringify(ret, null, 4);
        } else {
            domAst.value = "Parse Error";
        }
        parseTimer = null;
        return ret != null;
    }

    function compile() {
        oldAstValue = domAst.value;
        const ir = PegKit.Compiler.compile(JSON.parse(oldAstValue))
        domIr.value = JSON.stringify(ir, null, 4);
        domCode.value = PegKit.Runtime.generateParserCode(ir);
        compileTimer = null;
        return true;
    }

    function build() {
        oldCodeValue = domCode.value;
        parser = eval(oldCodeValue);
        buildTimer = null;
        return true;
    }


    function run() {
        if (parser == null) { return; }
        oldInputValue = domInput.value;
        const ret = parser("default", oldInputValue);
        domOutput.value = JSON.stringify(ret, null, 4);
        runTimer = null;
        return true;
    }

    function ParseAndCompileAndBuildAndRun() {
        return parse() && compile() && build() && run();
    }
    function compileAndBuildAndRun() {
        return compile() && build() && run();
    }
    function buildAndRun() {
        return build() && run();
    }

    addChangeEvent(domGrammar, () => {
        const nothingChanged = domGrammar.value == oldGrammarValue;
        if (nothingChanged) { return; }
        if (parseTimer !== null) {
            clearTimeout(parseTimer);
            parseTimer = null;
        }
        parseTimer = setTimeout(ParseAndCompileAndBuildAndRun, 500);
    });

    addChangeEvent(domAst, () => {
        const nothingChanged = domAst.value == oldAstValue;
        if (nothingChanged) { return; }
        if (compileTimer !== null) {
            clearTimeout(compileTimer);
            compileTimer = null;
        }
        compileTimer = setTimeout(compileAndBuildAndRun, 500);
    });

    addChangeEvent(domCode, () => {
        const nothingChanged = domCode.value == oldCodeValue;
        if (nothingChanged) { return; }
        if (buildTimer !== null) {
            clearTimeout(buildTimer);
            buildTimer = null;
        }
        buildTimer = setTimeout(buildAndRun, 500);
    });

    addChangeEvent(domInput, () => {
        const nothingChanged = domInput.value == oldInputValue;
        if (nothingChanged) { return; }
        if (runTimer !== null) {
            clearTimeout(runTimer);
            runTimer = null;
        }
        runTimer = setTimeout(run, 500);
    });
};

/*

default
  = g:Grammer { console.log(JSON.stringify(g)); return g; }

Grammer
  = __ xs:(r:Rule __ { return r; } )+ { return xs.reduce((s,[name,body]) => { s[name] = body; return s; }, {}); }

Rule
  = ruleName:Identifier __ "=" __ ruleBody:Expression EOS { return [ruleName, ruleBody]; }

Expression
  = ChoiceExpression

ChoiceExpression
  = x:ActionExpression xs:(__ "/" __ e:ActionExpression { return e; })* { return { type: "Choice", childs: xs.reduce((s,x) => { s.push(x); return s; },[x]) }; }

ActionExpression
  = expr:SequenceExpression code:( _ x:CodeBlock { return x; } )? { return (code == null) ? expr : { type: "Action", child: expr, code: code }; }

SequenceExpression
  = x:LabeledExpression xs:( _ e:LabeledExpression { return e; } )* { return (xs.length == 0) ? x : { type: "Sequence", childs: xs.reduce((s,x) => { s.push(x); return s; },[x]) }; }

LabeledExpression
  = label:(name:Identifier _ ":" _ { return name; } )? expression:PrefixedExpression { return (label == null) ? expression : { type: "Labeled", name: label, child: expression }; }

PrefixedExpression
  = operator:(x:PrefixedOperator _ { return x; } )? expression:SuffixedExpression { return (operator == null) ? expression : { type: operator, child: expression }; }

PrefixedOperator
  = "&" { return "AndPredicate"; }
  / "!" { return "NotPredicate"; }
  / "$" { return "Text"; }

SuffixedExpression
  = expression:PrimaryExpression operator:( _ x:SuffixedOperator { return x; })? { return (operator == null) ? expression : { type: operator, child: expression }; }

SuffixedOperator
  = "?" { return "Optional"; }
  / "*" { return "ZeroOrMore"; }
  / "+" { return "OneOrMore"; }

PrimaryExpression
  = LiteralMatcher
  / CharacterClassMatcher
  / AnyMatcher
  / AnyMatcher
  / "#" { return { type: "Position" }; }
  / RuleReferenceExpression
  / "(" _ e:Expression _ ")" { return { type: "Group", child:e }; }

LiteralMatcher
  = x:StringLiteral { return { type: "Str", str: x }; }

StringLiteral
  = '"' chars:DoubleStringCharacter* '"' { return chars.join(""); }
  / "'" chars:SingleStringCharacter* "'" { return chars.join(""); }

DoubleStringCharacter
  = !('"' / "\\") x:SourceCharacter { return x; }
  / "\\" x:EscapeSequence { return x; }

SingleStringCharacter
  = !("'" / "\\" / LineTerminator) x:SourceCharacter { return x; }
  / "\\" x:SourceCharacter { return x; }
  / LineContinuation

EscapeSequence
  = "'"  { return "'";  }
  / '"'  { return "\""; }
  / "\\" { return "\\"; }
  / "b"  { return "\b"; }
  / "f"  { return "\f"; }
  / "n"  { return "\n"; }
  / "r"  { return "\r"; }
  / "t"  { return "\t"; }
  / "v"  { return "\v"; }

CharacterClassMatcher
  =  "[" inverted:"^"? parts:CharacterPart* "]" ignoreCase:"i"? { return { type:"CharClass", inverted: inverted, parts:parts, ignoreCase:ignoreCase }; }

CharacterPart
  = ClassCharacterRange
  / x:ClassCharacter { return { begin:x, end:x}; }

ClassCharacterRange
  = begin:ClassCharacter "-" end:ClassCharacter { return { begin:begin, end:end }; }

ClassCharacter
  = !("]" / "\\" / LineTerminator) x:SourceCharacter { return x; }
  / "\\" x:EscapeSequence { return x; }
  / LineContinuation

LineContinuation
  = "\\" LineTerminatorSequence { return ""; }

AnyMatcher
  = "." { return { type: "AnyChar" }; }

RuleReferenceExpression
  = name:Identifier { return { type: "RuleRef", rule: name }; }

CodeBlock
  = "{" x:Code "}" { return x; }
  / "{" { error("Unbalanced brace."); }

Code
  = $((![{}] SourceCharacter)+ / "{" Code "}")*

SourceCharacter
  = .

LineTerminator = [\r\n]
LineTerminatorSequence =  "\r"?"\n"

Identifier = x:[A-Za-z_] xs:[A-Za-z0-9_]* { return String.prototype.concat(x,...xs); }
__ = [ \r\n\t]*
_ = [ \t]*

EOS = [ \t]* [\r\n]



*/

/*
Expression
  = head:Term tail:(_ ("+" / "-") _ Term)* { console.log(tail);
      return tail.reduce(function(result, element) {
        if (element[1] === "+") { return result + element[3]; }
        if (element[1] === "-") { return result - element[3]; }
      }, head);
    }

Term
  = head:Factor tail:(_ ("*" / "/") _ Factor)* {
      return tail.reduce(function(result, element) {
        if (element[1] === "*") { return result * element[3]; }
        if (element[1] === "/") { return result / element[3]; }
      }, head);
    }

Factor
  = "(" _ expr:Expression _ ")" { return expr; }
  / Integer

Integer
  = _ x:("0"/"1"/"2"/"3"/"4"/"5"/"6"/"7"/"8"/"9")+ { return parseInt(x.join(""), 10); }

_
  = (" "/"\t"/"\n"/"\r")*

*/

