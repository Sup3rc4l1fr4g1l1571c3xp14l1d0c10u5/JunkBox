﻿module PegKit {
    class CodeWriter {

        private indent: number;
        private codes: string[];

        constructor() {
            this.indent = 0;
            this.codes = [];
        }

        up(): CodeWriter { this.indent += 1; return this; }

        down(): CodeWriter { this.indent -= 1; return this; }

        writeLine(...code: string[]): CodeWriter {
            let space = "";
            for (let i = 0; i < this.indent; i++) {
                space += "  ";
            }
            code.forEach(x => this.codes.push(space + x));
            return this;
        }

        toString(): string {
            return this.codes.join("\n");
        }

        append(cw: CodeWriter) {
            this.codes.push(...cw.codes);
            return this;
        }
    };

    interface ICodeGenerator {
        generate(g: Grammar): void;
        toString(): string;
    }

    class JavascriptGenerator implements ICodeGenerator {
        private code: CodeWriter;
        private ruleCode: CodeWriter;
        private actionCode: CodeWriter;
        private labelId: number;
        private varId: number;
        private actionId: number;
        private captures: { [key: string]: number }[];

        private escapeString(str: string) {
            const s = JSON.stringify(str);
            return s.substr(1, s.length - 2);
        }

        private allocLabel(): number {
            return this.labelId++;
        }

        private allocAction(): number {
            return this.actionId++;
        }

        private allocVar(): number {
            return this.varId++;
        }

        constructor() {
            this.ruleCode = null;
            this.actionCode = null;
            this.code = new CodeWriter();

            this.labelId = 1;
            this.varId = 1;
            this.actionId = 1;
            this.captures = [];
        }

        public toString(): string {
            return this.code.toString();
        }

        public generate(g: Grammar): void {
            this.code.writeLine(`(function (){`);
            this.code.up();
            this.code.writeLine(...
                `function decodeValue(x) {
    if (x == null) { return x; }
    switch (x.type) {
        case "Value": {
            if ((typeof x.value === "object") && (x.value["type"] == "Value" || x.value["type"] == "EmptyValueList" || x.value["type"] == "ValueList")) {
                return decodeValue(x.value);
            } else {
                return x.value;
            }
        }
        case "EmptyValueList": {
            return [];
        }
        case "Nil": {
            return null;
        }
        case "ValueList": {
            const ret = [];
            while (x.type != "EmptyValueList") {
                ret.push(decodeValue(x.value));
                x = x.next;
            }
            return ret.reverse();
        }
        default: {
            return x;
        }
    }
}

function IsCharClass(char, inverted, parts, ignoreCase) {
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
`.split(/\n/));

            const keys = Object.keys(g);
            for (const key of keys) {
                this.actionCode = new CodeWriter();
                this.actionCode.up();
                this.ruleCode = new CodeWriter();
                this.ruleCode.up();
                this.generateOne(key, g[key]);
                this.code.append(this.actionCode).append(this.ruleCode);
                this.actionCode = null;
                this.ruleCode = null;
            }

            this.code.writeLine(`return {`);
            this.code.up();
            Object.keys(g).map(x => `${x}: $${x}`).join(",\n").split("\n").forEach(x => this.code.writeLine(x));
            this.code.down();
            this.code.writeLine(`};`);

            this.code.down();
            this.code.writeLine(`})();`);
        }

        private generateOne(ruleName: string, ast: Ast): void {
            const successLabel = this.allocLabel();
            const failLabel = this.allocLabel();
            this.ruleCode.writeLine(`/* rule: ${ruleName} */`);
            this.ruleCode.writeLine(`function $${ruleName}(str, ctx) {`);
            this.ruleCode.up();
            this.ruleCode.writeLine(`let label = null;`);
            this.ruleCode.writeLine(`let i = 0;`);
            this.ruleCode.down().writeLine(`goto:`).up();
            this.ruleCode.writeLine(`for (;;) {`);
            this.ruleCode.up();
            this.ruleCode.writeLine(`i++; if (i > 1000) { throw new Error(); } `);
            this.ruleCode.writeLine(`switch (label) {`);
            this.ruleCode.up();
            this.ruleCode.up();
            this.ruleCode.down().writeLine(`case null:`).up();
            this.captures.push({});
            this.visit(ruleName, ast, successLabel, failLabel);
            this.captures.pop();
            this.labelDef(successLabel);
            this.ruleCode.writeLine(`return ctx;`);
            this.labelDef(failLabel);
            this.ruleCode.writeLine(`return null;`);
            this.ruleCode.down();
            this.ruleCode.down();
            this.ruleCode.writeLine(`}`);
            this.ruleCode.down();
            this.ruleCode.writeLine(`}`);
            this.ruleCode.down();
            this.ruleCode.writeLine(`}`);
            this.ruleCode.writeLine(``);
        }

        private visit(ruleName: string, ast: Ast, successLabel: number, failLabel: number): void {
            switch (ast.type) {
                case "Char": this.onChar(ruleName, ast, successLabel, failLabel); break;
                case "CharClass": this.onCharClass(ruleName, ast, successLabel, failLabel); break;
                case "AnyChar": this.onAnyChar(ruleName, ast, successLabel, failLabel); break;
                case "Str": this.onStr(ruleName, ast, successLabel, failLabel); break;
                case "Sequence": this.onSequence(ruleName, ast, successLabel, failLabel); break;
                case "Choice": this.onChoice(ruleName, ast, successLabel, failLabel); break;
                case "Optional": this.onOptional(ruleName, ast, successLabel, failLabel); break;
                case "ZeroOrMore": this.onZeroOrMore(ruleName, ast, successLabel, failLabel); break;
                case "OneOrMore": this.onOneOrMore(ruleName, ast, successLabel, failLabel); break;
                case "AndPredicate": this.onAndPredicate(ruleName, ast, successLabel, failLabel); break;
                case "NotPredicate": this.onNotPredicate(ruleName, ast, successLabel, failLabel); break;
                case "RuleRef": this.onRuleRef(ruleName, ast, successLabel, failLabel); break;
                case "Labeled": this.onLabeled(ruleName, ast, successLabel, failLabel); break;
                case "Action": this.onAction(ruleName, ast, successLabel, failLabel); break;
                case "Text": this.onText(ruleName, ast, successLabel, failLabel); break;
                default: throw new Error(`Ast "${ast}" is not supported in Rule ${ruleName}`);
            }
        }

        private labelDef(label: number) {
            this.ruleCode.down().writeLine(`case "L${label}":`).up();
        }

        private jumpTo(label: number) {
            this.ruleCode.writeLine(`label = "L${label}"; continue goto;`);
        }

        private onChar(ruleName: string, ast: { type: "Char", char: string }, successLabel: number, failLabel: number) {
            this.ruleCode.writeLine(`if (str[ctx.sp] != "${this.escapeString(ast.char)}") {`);
            this.ruleCode.up();
            this.jumpTo(failLabel);
            this.ruleCode.down();
            this.ruleCode.writeLine(`} else {`);
            this.ruleCode.up();
            this.ruleCode.writeLine(`ctx = { sp:ctx.sp+1, value:{ type:"Value", start: ctx.sp, end:ctx.sp+1, value:str[ctx.sp]}};`);
            this.jumpTo(successLabel);
            this.ruleCode.down();
            this.ruleCode.writeLine(`}`);
        }

        private onCharClass(ruleName: string, ast: { type: "CharClass", inverted: boolean, parts: { begin: string, end: string }[], ignoreCase: boolean }, successLabel: number, failLabel: number) {
            this.ruleCode.writeLine(`if (IsCharClass(str[ctx.sp],${ast.inverted},${JSON.stringify(ast.parts)},${ast.ignoreCase}) == false) {`);
            this.ruleCode.up();
            this.jumpTo(failLabel);
            this.ruleCode.down();
            this.ruleCode.writeLine(`} else {`);
            this.ruleCode.up();
            this.ruleCode.writeLine(`ctx = { sp:ctx.sp+1, value:{ type:"Value", start: ctx.sp, end:ctx.sp+1, value:str[ctx.sp]}};`);
            this.jumpTo(successLabel);
            this.ruleCode.down();
            this.ruleCode.writeLine(`}`);
        }

        private onAnyChar(ruleName: string, ast: { type: "AnyChar" }, successLabel: number, failLabel: number) {
            this.ruleCode.writeLine(`if (str.length <= ctx.sp) {`);
            this.ruleCode.up();
            this.jumpTo(failLabel);
            this.ruleCode.down();
            this.ruleCode.writeLine(`} else {`);
            this.ruleCode.up();
            this.ruleCode.writeLine(`ctx = { sp:ctx.sp+1, value:{ type:"Value", start: ctx.sp, end:ctx.sp+1, value:str[ctx.sp]}};`);
            this.jumpTo(successLabel);
            this.ruleCode.down();
            this.ruleCode.writeLine(`}`);
        }

        private onStr(ruleName: string, ast: { type: "Str", str: string }, successLabel: number, failLabel: number) {
            this.ruleCode.writeLine(`if (str.substr(ctx.sp, ${ast.str.length}) != "${this.escapeString(ast.str)}") {`);
            this.ruleCode.up();
            this.jumpTo(failLabel);
            this.ruleCode.down();
            this.ruleCode.writeLine(`} else {`);
            this.ruleCode.up();
            this.ruleCode.writeLine(`ctx = { sp:ctx.sp+${ast.str.length}, value:{ type:"Value", start: ctx.sp, end:ctx.sp+${ast.str.length}, value:str.substr(ctx.sp, ${ast.str.length}) }}; `);
            this.jumpTo(successLabel);
            this.ruleCode.down();
            this.ruleCode.writeLine(`}`);
        }

        private onSequence(ruleName: string, ast: { type: "Sequence"; childs: Ast[] }, successLabel: number, failLabel: number) {
            const junctionLabel = this.allocLabel();
            const tempVar1 = this.allocVar();
            const tempVar2 = this.allocVar();

            this.ruleCode.writeLine(`var temp${tempVar2} = ctx;`);
            this.ruleCode.writeLine(`var temp${tempVar1} = { type: "EmptyValueList" };`);

            for (const child of ast.childs) {
                const nextLabel = this.allocLabel();
                this.visit(ruleName, child, nextLabel, junctionLabel);
                this.labelDef(nextLabel);
                this.ruleCode.writeLine(`temp${tempVar1} = { type: "ValueList", value:ctx.value, next:temp${tempVar1}};`);
                this.ruleCode.writeLine(`ctx = { sp:ctx.sp, value:null};`);// need this?
            }
            this.ruleCode.writeLine(`ctx = { sp:ctx.sp, value:temp${tempVar1}};`);
            this.jumpTo(successLabel);
            this.labelDef(junctionLabel);
            this.ruleCode.writeLine(`ctx = temp${tempVar2};`);
            this.jumpTo(failLabel);
        }

        private onChoice(ruleName: string, ast: { type: "Choice"; childs: Ast[] }, successLabel: number, failLabel: number) {
            for (const child of ast.childs) {
                const nextLabel = this.allocLabel();
                this.visit(ruleName, child, successLabel, nextLabel);
                this.labelDef(nextLabel);
            }
            this.jumpTo(failLabel);
        }

        private onOptional(ruleName: string, ast: { type: "Optional"; child: Ast }, successLabel: number, failLabel: number) {
            const tempVar = this.allocVar();
            const junctionLabel = this.allocLabel();
            this.ruleCode.writeLine(`var temp${tempVar} = ctx;`);
            this.visit(ruleName, ast.child, successLabel, junctionLabel);
            this.labelDef(junctionLabel);
            this.ruleCode.writeLine(`ctx = { sp:temp${tempVar}.sp, value: { type: "Nil" } };`);
            this.jumpTo(successLabel);
        }

        private onZeroOrMore(ruleName: string, ast: { type: "ZeroOrMore"; child: Ast }, successLabel: number, failLabel: number) {
            const loopLabel = this.allocLabel();
            const succLabel = this.allocLabel();
            const junctionLabel = this.allocLabel();
            const tempVar1 = this.allocVar();
            const tempVar2 = this.allocVar();
            const tempVar3 = this.allocVar();
            this.ruleCode.writeLine(`var temp${tempVar1} = ctx;`);
            this.ruleCode.writeLine(`var temp${tempVar2} = { type: "EmptyValueList" };`);
            this.labelDef(loopLabel);
            this.ruleCode.writeLine(`var temp${tempVar3} = ctx;`);
            this.ruleCode.writeLine(`ctx = { sp:ctx.sp, value:null};`);
            this.visit(ruleName, ast.child, succLabel, junctionLabel);
            this.labelDef(succLabel);
            this.ruleCode.writeLine(`temp${tempVar2} = {type: "ValueList", value: ctx.value, next: temp${tempVar2}};`);
            this.ruleCode.writeLine(`ctx = { sp:ctx.sp, value:null};`);
            this.jumpTo(loopLabel);
            this.labelDef(junctionLabel);
            this.ruleCode.writeLine(`ctx = { sp:temp${tempVar3}.sp, value: { type: "Value", start: temp${tempVar1}.sp, end:temp${tempVar3}.sp, value:temp${tempVar2} }};`);
            this.jumpTo(successLabel);
        }

        private onOneOrMore(ruleName: string, ast: { type: "OneOrMore"; child: Ast }, successLabel: number, failLabel: number) {
            const rollbackLabel = this.allocLabel();
            const loopLabel = this.allocLabel();
            const junctionLabel = this.allocLabel();
            const tempVar1 = this.allocVar();
            const tempVar2 = this.allocVar();
            const tempVar3 = this.allocVar();
            this.ruleCode.writeLine(`var temp${tempVar1} = ctx;`);
            this.ruleCode.writeLine(`var temp${tempVar2} = { type: "EmptyValueList" };`);
            this.ruleCode.writeLine(`ctx = { sp: ctx.sp, value: null };`);
            this.visit(ruleName, ast.child, loopLabel, rollbackLabel);
            this.labelDef(rollbackLabel);
            this.ruleCode.writeLine(`ctx = temp${tempVar1};`);
            this.jumpTo(failLabel);
            this.labelDef(loopLabel);

            this.ruleCode.writeLine(`temp${tempVar2} = {type: "ValueList", value: ctx.value, next: temp${tempVar2}};`);
            this.ruleCode.writeLine(`var temp${tempVar3} = {sp:ctx.sp, value: temp${tempVar2}};`);
            this.ruleCode.writeLine(`ctx = { sp:ctx.sp, value:null};`);
            this.visit(ruleName, ast.child, loopLabel, junctionLabel);
            this.labelDef(junctionLabel);
            this.ruleCode.writeLine(`ctx = { sp: temp${tempVar3}.sp, value: { type: "Value", start: temp${tempVar1}.sp, end:temp${tempVar3}.sp, value:temp${tempVar2} }};`);
            this.jumpTo(successLabel);
        }

        private onAndPredicate(ruleName: string, ast: { type: "AndPredicate"; child: Ast }, successLabel: number, failLabel: number) {
            const tempVar = this.allocVar();
            const junctionLabel = this.allocLabel();
            this.ruleCode.writeLine(`var temp${tempVar} = ctx;`);
            this.visit(ruleName, ast.child, junctionLabel, failLabel);
            this.labelDef(junctionLabel);
            this.ruleCode.writeLine(`ctx = temp${tempVar};`);
            this.jumpTo(successLabel);
        }

        private onNotPredicate(ruleName: string, ast: { type: "NotPredicate"; child: Ast }, successLabel: number, failLabel: number) {
            const tempVar = this.allocVar();
            const junctionLabel = this.allocLabel();
            this.ruleCode.writeLine(`var temp${tempVar} = ctx;`);
            this.visit(ruleName, ast.child, failLabel, junctionLabel);
            this.labelDef(junctionLabel);
            this.ruleCode.writeLine(`ctx = temp${tempVar};`);
            this.jumpTo(successLabel);
        }

        private onRuleRef(ruleName: string, ast: { type: "RuleRef"; rule: string }, successLabel: number, failLabel: number) {
            const tempVar = this.allocVar();
            this.ruleCode.writeLine(`var temp${tempVar} = $${ast.rule}(str, ctx);`);
            this.ruleCode.writeLine(`if (temp${tempVar} == null) {`);
            this.ruleCode.up();
            this.jumpTo(failLabel);
            this.ruleCode.down();
            this.ruleCode.writeLine(`} else {`);
            this.ruleCode.up();
            this.ruleCode.writeLine(`ctx = temp${tempVar};`);
            this.jumpTo(successLabel);
            this.ruleCode.down();
            this.ruleCode.writeLine(`}`);
        }

        private onLabeled(ruleName: string, ast: { type: "Labeled"; name: string; child: Ast }, successLabel: number, failLabel: number) {
            const captureVar = this.allocVar();
            const junctionLabel = this.allocLabel();
            this.visit(ruleName, ast.child, junctionLabel, failLabel);
            this.labelDef(junctionLabel);
            this.ruleCode.writeLine(`var capture${captureVar} = ctx.value;`);
            this.captures[this.captures.length - 1][ast.name] = captureVar;
            this.jumpTo(successLabel);
        }

        private onAction(ruleName: string, ast: { type: "Action"; child: Ast, code: string }, successLabel: number, failLabel: number) {
            const actionId = this.allocAction();
            const junctionLabel = this.allocLabel();
            this.visit(ruleName, ast.child, junctionLabel, failLabel);
            this.labelDef(junctionLabel);
            this.actionCode.writeLine(`function action${actionId}(${Object.keys(this.captures[this.captures.length - 1]).map(x => x).join(", ")}) {`);
            this.actionCode.writeLine(...ast.code.replace(/^[\r\n]*|[\r\n]*$/, '').split(/\r?\n/));
            this.actionCode.writeLine(`}`);
            this.actionCode.writeLine(``);
            this.ruleCode.writeLine(`ctx = { sp:ctx.sp, value: { type:"Value", start: ctx.value.start, end:ctx.value.end, value: action${actionId}(${Object.keys(this.captures[this.captures.length - 1]).map(x => `decodeValue(capture${this.captures[this.captures.length - 1][x]})`).join(", ")}) }};`);
            this.jumpTo(successLabel);
        }

        private onText(ruleName: string, ast: { type: "Text"; child: Ast }, successLabel: number, failLabel: number) {
            const junctionLabel = this.allocLabel();
            this.visit(ruleName, ast.child, junctionLabel, failLabel);
            this.labelDef(junctionLabel);
            this.ruleCode.writeLine(`ctx = { sp:ctx.sp, value: { type:"Value", start: ctx.value.start, end:ctx.value.end, value: str.substring(ctx.value.start, ctx.value.end) }};`);
            this.jumpTo(successLabel);
        }
    }

    type IR
        = { type: "Char", char: string, successLabel: number, failLabel: number }
        | { type: "CharClass", inverted: boolean, parts: { begin: string, end: string }[], ignoreCase: boolean, successLabel: number, failLabel: number }
        | { type: "AnyChar", successLabel: number, failLabel: number }
        | { type: "Str", str: string, successLabel: number, failLabel: number }
        | { type: "PushContext" }
        | { type: "PushArray" }
        | { type: "Label", id: number }
        | { type: "Rule", name: string }
        | { type: "Jump", id: number }
        | { type: "Nip" }
        | { type: "Append" }
        | { type: "Pop" }
        | { type: "PopContext" }
        | { type: "PushNil" }
        | { type: "Call", name: string }
        | { type: "Test", successLabel: number, failLabel: number }
        | { type: "Capture", name: string }
        | { type: "Action", code: string, captures: string[] }
        | { type: "Text" }
        | { type: "Return", success: boolean }
        | { type: "Break", comment: string }

    class IRGenerator implements ICodeGenerator {
        private irCodes: IR[];

        private labelId: number;
        private captures: { [key: string]: number }[];

        private allocLabel(): number {
            return this.labelId++;
        }

        private Char(char: string, successLabel: number, failLabel: number) {
            this.irCodes.push({ type: "Char", char: char, successLabel: successLabel, failLabel: failLabel });
        }

        private CharClass(inverted: boolean, parts: { begin: string, end: string }[], ignoreCase: boolean, successLabel: number, failLabel: number) {
            this.irCodes.push({ type: "CharClass", inverted: inverted, parts: parts, ignoreCase: ignoreCase, successLabel: successLabel, failLabel: failLabel });
        }

        private AnyChar(successLabel: number, failLabel: number) {
            this.irCodes.push({ type: "AnyChar", successLabel: successLabel, failLabel: failLabel });
        }

        private Str(str: string, successLabel: number, failLabel: number) {
            this.irCodes.push({ type: "Str", str: str, successLabel: successLabel, failLabel: failLabel });
        }

        // push();
        private PushContext(): void {
            this.irCodes.push({ type: "PushContext" });
        }

        // push([]);
        private PushArray(): void {
            this.irCodes.push({ type: "PushArray" });
        }

        // v = pop(); pop(); push(v);
        private Nip(): void {
            this.irCodes.push({ type: "Nip" });
        }

        private Pop(): void {
            this.irCodes.push({ type: "Pop" });
        }

        private PopContext(): void {
            this.irCodes.push({ type: "PopContext" });
        }

        private Append(): void {
            this.irCodes.push({ type: "Append" });
        }

        private PushNil(): void {
            this.irCodes.push({ type: "PushNil" });
        }

        private Text(): void {
            this.irCodes.push({ type: "Text" });
        }

        private Label(id: number): void {
            this.irCodes.push({ type: "Label", id: id });
        }

        private Jump(id: number): void {
            this.irCodes.push({ type: "Jump", id: id });
        }

        private Call(name: string): void {
            this.irCodes.push({ type: "Call", name: name });
        }

        private Test(successLabel: number, failLabel: number) {
            this.irCodes.push({ type: "Test", successLabel: successLabel, failLabel: failLabel });
        }

        private Capture(name: string) {
            this.irCodes.push({ type: "Capture", name: name });
        }

        private Action(code: string, captures: string[]) {
            this.irCodes.push({ type: "Action", code: code, captures: captures });
        }

        private Rule(name:string) {
            this.irCodes.push({ type: "Rule", name: name });
        }

        private Return(success: boolean) {
            this.irCodes.push({ type: "Return", success: success });
        }

        constructor() {
            this.irCodes = [];

            this.labelId = 1;
            this.captures = [];
        }

        public toString(): string {
            return JSON.stringify(this.irCodes, null, 4);
        }

        public generate(g: Grammar): void {
            const keys = Object.keys(g);
            for (const key of keys) {
                this.generateOne(key, g[key]);
            }
        }

        private generateOne(ruleName: string, ast: Ast): void {
            const successLabel = this.allocLabel();
            const failLabel = this.allocLabel();
            this.captures.push({});
            this.Rule(ruleName);
            this.visit(ruleName, ast, successLabel, failLabel);
            this.captures.pop();
            this.Label(successLabel);
            this.Return(true);
            this.Label(failLabel);
            this.Return(false);
        }

        private visit(ruleName: string, ast: Ast, successLabel: number, failLabel: number): void {
            switch (ast.type) {
                case "Char": this.onChar(ruleName, ast, successLabel, failLabel); break;
                case "CharClass": this.onCharClass(ruleName, ast, successLabel, failLabel); break;
                case "AnyChar": this.onAnyChar(ruleName, ast, successLabel, failLabel); break;
                case "Str": this.onStr(ruleName, ast, successLabel, failLabel); break;
                case "Sequence": this.onSequence(ruleName, ast, successLabel, failLabel); break;
                case "Choice": this.onChoice(ruleName, ast, successLabel, failLabel); break;
                case "Optional": this.onOptional(ruleName, ast, successLabel, failLabel); break;
                case "ZeroOrMore": this.onZeroOrMore(ruleName, ast, successLabel, failLabel); break;
                case "OneOrMore": this.onOneOrMore(ruleName, ast, successLabel, failLabel); break;
                case "AndPredicate": this.onAndPredicate(ruleName, ast, successLabel, failLabel); break;
                case "NotPredicate": this.onNotPredicate(ruleName, ast, successLabel, failLabel); break;
                case "RuleRef": this.onRuleRef(ruleName, ast, successLabel, failLabel); break;
                case "Labeled": this.onLabeled(ruleName, ast, successLabel, failLabel); break;
                case "Action": this.onAction(ruleName, ast, successLabel, failLabel); break;
                case "Text": this.onText(ruleName, ast, successLabel, failLabel); break;
                default: throw new Error(`Ast "${ast}" is not supported in Rule ${ruleName}`);
            }
        }

        private onChar(ruleName: string, ast: { type: "Char", char: string }, successLabel: number, failLabel: number) {
            this.Char(ast.char, successLabel, failLabel);
        }

        private onCharClass(ruleName: string, ast: { type: "CharClass", inverted: boolean, parts: { begin: string, end: string }[], ignoreCase: boolean }, successLabel: number, failLabel: number) {
            this.CharClass(ast.inverted, ast.parts, ast.ignoreCase, successLabel, failLabel);
        }

        private onAnyChar(ruleName: string, ast: { type: "AnyChar" }, successLabel: number, failLabel: number) {
            this.AnyChar(successLabel, failLabel);
        }

        private onStr(ruleName: string, ast: { type: "Str", str: string }, successLabel: number, failLabel: number) {
            this.Str(ast.str, successLabel, failLabel);
        }

        private onSequence(ruleName: string, ast: { type: "Sequence"; childs: Ast[] }, successLabel: number, failLabel: number) {
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

        private onChoice(ruleName: string, ast: { type: "Choice"; childs: Ast[] }, successLabel: number, failLabel: number) {
            for (const child of ast.childs) {
                const nextLabel = this.allocLabel();
                this.visit(ruleName, child, successLabel, nextLabel);
                this.Label(nextLabel);
            }
            this.Jump(failLabel);
        }

        private onOptional(ruleName: string, ast: { type: "Optional"; child: Ast }, successLabel: number, failLabel: number) {
            const succLabel = this.allocLabel();
            const junctionLabel = this.allocLabel();
            this.PushContext();
            this.visit(ruleName, ast.child, succLabel, junctionLabel);
            this.Label(succLabel);
            this.Nip();
            this.Jump(successLabel);
            this.Label(junctionLabel);
            this.PopContext();
            this.PushNil();
            this.Jump(successLabel);
        }

        private onZeroOrMore(ruleName: string, ast: { type: "ZeroOrMore"; child: Ast }, successLabel: number, failLabel: number) {
            const loopLabel = this.allocLabel();
            const succLabel = this.allocLabel();
            const junctionLabel = this.allocLabel();
            this.PushContext();
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
            this.Nip();
            this.Jump(successLabel);
        }

        private onOneOrMore(ruleName: string, ast: { type: "OneOrMore"; child: Ast }, successLabel: number, failLabel: number) {
            const rollbackLabel = this.allocLabel();
            const loopLabel = this.allocLabel();
            const junctionLabel = this.allocLabel();
            this.PushContext();
            this.PushArray();
            this.PushContext();
            this.visit(ruleName, ast.child, loopLabel, rollbackLabel);
            this.Label(rollbackLabel);
            this.Pop();
            this.Pop();
            this.PopContext();
            this.Jump(failLabel);
            this.Label(loopLabel);
            this.Nip();
            this.Append();
            this.PushContext();
            this.visit(ruleName, ast.child, loopLabel, junctionLabel);
            this.Label(junctionLabel);
            this.PopContext();
            this.Nip();
            this.Jump(successLabel);
        }

        private onAndPredicate(ruleName: string, ast: { type: "AndPredicate"; child: Ast }, successLabel: number, failLabel: number) {
            const junctionLabel = this.allocLabel();
            this.PushContext();
            this.visit(ruleName, ast.child, junctionLabel, failLabel);
            this.Label(junctionLabel);
            this.Pop();
            this.PopContext();
            this.Jump(successLabel);
        }

        private onNotPredicate(ruleName: string, ast: { type: "NotPredicate"; child: Ast }, successLabel: number, failLabel: number) {
            const junctionLabel = this.allocLabel();
            const junctionLabel2 = this.allocLabel();
            this.PushContext();
            this.visit(ruleName, ast.child, junctionLabel2, junctionLabel);
            this.Label(junctionLabel);
            this.Pop();
            this.PopContext();
            this.Jump(failLabel);
            this.Label(junctionLabel);
            this.PopContext();
            this.Jump(successLabel);
        }

        private onRuleRef(ruleName: string, ast: { type: "RuleRef"; rule: string }, successLabel: number, failLabel: number) {
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

        private onLabeled(ruleName: string, ast: { type: "Labeled"; name: string; child: Ast }, successLabel: number, failLabel: number) {
            const junctionLabel = this.allocLabel();
            this.visit(ruleName, ast.child, junctionLabel, failLabel);
            this.Label(junctionLabel);
            this.Capture(ast.name);
            this.Jump(successLabel);
            this.captures[this.captures.length - 1][ast.name] = this.captures[this.captures.length - 1].Count;
        }

        private onAction(ruleName: string, ast: { type: "Action"; child: Ast, code: string }, successLabel: number, failLabel: number) {
            const junctionLabel = this.allocLabel();
            this.visit(ruleName, ast.child, junctionLabel, failLabel);
            this.Label(junctionLabel);
            this.Action(ast.code, Object.keys(this.captures[this.captures.length - 1]));
            this.Jump(successLabel);
        }

        private onText(ruleName: string, ast: { type: "Text"; child: Ast }, successLabel: number, failLabel: number) {
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
    }

    export type Ast
        = Char
        | CharClass
        | AnyChar
        | Str
        | Sequence
        | Choice
        | Optional
        | ZeroOrMore
        | OneOrMore
        | AndPredicate
        | NotPredicate
        | RuleRef
        | Labeled
        | Action
        | Text
        ;

    export type Char = { type: "Char", char: string };
    export type CharClass = { type: "CharClass", inverted: boolean, parts: { begin: string, end: string }[], ignoreCase: boolean };
    export type AnyChar = { type: "AnyChar" };
    export type Str = { type: "Str", str: string };
    export type Sequence = { type: "Sequence", childs: Ast[] };
    export type Choice = { type: "Choice", childs: Ast[] };
    export type Optional = { type: "Optional", child: Ast };
    export type ZeroOrMore = { type: "ZeroOrMore", child: Ast };
    export type OneOrMore = { type: "OneOrMore", child: Ast };
    export type AndPredicate = { type: "AndPredicate", child: Ast };
    export type NotPredicate = { type: "NotPredicate", child: Ast };
    export type RuleRef = { type: "RuleRef", rule: string };
    export type Labeled = { type: "Labeled", name: string, child: Ast };
    export type Action = { type: "Action", child: Ast, code: string };
    export type Text = { type: "Text", child: Ast };

    export type Grammar = { [key: string]: Ast };

    export function compileGrammar(g: Grammar): string {
        let generator = new JavascriptGenerator();
        generator.generate(g);
        return generator.toString();
    }

    export function compileGrammar2(g: Grammar): string {
        let generator = new IRGenerator();
        generator.generate(g);
        return generator.toString();
    }
}

window.onload = () => {

    const builtinGrammar: PegKit.Grammar =
        { "default": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Labeled", "name": "g", "child": { "type": "RuleRef", "rule": "Grammer" } }, "code": " console.log(JSON.stringify(g)); return g; " }] }, "Grammer": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "RuleRef", "rule": "__" }, { "type": "Labeled", "name": "xs", "child": { "type": "OneOrMore", "child": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Labeled", "name": "r", "child": { "type": "RuleRef", "rule": "Rule" } }, { "type": "RuleRef", "rule": "__" }] }, "code": " return r; " }] } } }] }, "code": " return xs.reduce((s,[name,body]) => { s[name] = body; return s; }, {}); " }] }, "Rule": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Labeled", "name": "ruleName", "child": { "type": "RuleRef", "rule": "Identifier" } }, { "type": "RuleRef", "rule": "__" }, { "type": "Str", "str": "=" }, { "type": "RuleRef", "rule": "__" }, { "type": "Labeled", "name": "ruleBody", "child": { "type": "RuleRef", "rule": "Expression" } }, { "type": "RuleRef", "rule": "EOS" }] }, "code": " return [ruleName, ruleBody]; " }] }, "Expression": { "type": "Choice", "childs": [{ "type": "RuleRef", "rule": "ChoiceExpression" }] }, "ChoiceExpression": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "ActionExpression" } }, { "type": "Labeled", "name": "xs", "child": { "type": "ZeroOrMore", "child": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "RuleRef", "rule": "__" }, { "type": "Str", "str": "/" }, { "type": "RuleRef", "rule": "__" }, { "type": "Labeled", "name": "e", "child": { "type": "RuleRef", "rule": "ActionExpression" } }] }, "code": " return e; " }] } } }] }, "code": " return { type: \"Choice\", childs: xs.reduce((s,x) => { s.push(x); return s; },[x]) }; " }] }, "ActionExpression": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Labeled", "name": "expr", "child": { "type": "RuleRef", "rule": "SequenceExpression" } }, { "type": "Labeled", "name": "code", "child": { "type": "Optional", "child": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "RuleRef", "rule": "_" }, { "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "CodeBlock" } }] }, "code": " return x; " }] } } }] }, "code": " return (code == null) ? expr : { type: \"Action\", child: expr, code: code }; " }] }, "SequenceExpression": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "LabeledExpression" } }, { "type": "Labeled", "name": "xs", "child": { "type": "ZeroOrMore", "child": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "RuleRef", "rule": "_" }, { "type": "Labeled", "name": "e", "child": { "type": "RuleRef", "rule": "LabeledExpression" } }] }, "code": " return e; " }] } } }] }, "code": " return (xs.length == 0) ? x : { type: \"Sequence\", childs: xs.reduce((s,x) => { s.push(x); return s; },[x]) }; " }] }, "LabeledExpression": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Labeled", "name": "label", "child": { "type": "Optional", "child": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Labeled", "name": "name", "child": { "type": "RuleRef", "rule": "Identifier" } }, { "type": "RuleRef", "rule": "_" }, { "type": "Str", "str": ":" }, { "type": "RuleRef", "rule": "_" }] }, "code": " return name; " }] } } }, { "type": "Labeled", "name": "expression", "child": { "type": "RuleRef", "rule": "PrefixedExpression" } }] }, "code": " return (label == null) ? expression : { type: \"Labeled\", name: label, child: expression }; " }] }, "PrefixedExpression": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Labeled", "name": "operator", "child": { "type": "Optional", "child": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "PrefixedOperator" } }, { "type": "RuleRef", "rule": "_" }] }, "code": " return x; " }] } } }, { "type": "Labeled", "name": "expression", "child": { "type": "RuleRef", "rule": "SuffixedExpression" } }] }, "code": " return (operator == null) ? expression : { type: operator, child: expression }; " }] }, "PrefixedOperator": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Str", "str": "&" }, "code": " return \"AndPredicate\"; " }, { "type": "Action", "child": { "type": "Str", "str": "!" }, "code": " return \"NotPredicate\"; " }, { "type": "Action", "child": { "type": "Str", "str": "$" }, "code": " return \"Text\"; " }] }, "SuffixedExpression": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Labeled", "name": "expression", "child": { "type": "RuleRef", "rule": "PrimaryExpression" } }, { "type": "Labeled", "name": "operator", "child": { "type": "Optional", "child": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "RuleRef", "rule": "_" }, { "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "SuffixedOperator" } }] }, "code": " return x; " }] } } }] }, "code": " return (operator == null) ? expression : { type: operator, child: expression }; " }] }, "SuffixedOperator": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Str", "str": "?" }, "code": " return \"Optional\"; " }, { "type": "Action", "child": { "type": "Str", "str": "*" }, "code": " return \"ZeroOrMore\"; " }, { "type": "Action", "child": { "type": "Str", "str": "+" }, "code": " return \"OneOrMore\"; " }] }, "PrimaryExpression": { "type": "Choice", "childs": [{ "type": "RuleRef", "rule": "LiteralMatcher" }, { "type": "RuleRef", "rule": "CharacterClassMatcher" }, { "type": "RuleRef", "rule": "AnyMatcher" }, { "type": "RuleRef", "rule": "RuleReferenceExpression" }, { "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Str", "str": "(" }, { "type": "RuleRef", "rule": "_" }, { "type": "Labeled", "name": "e", "child": { "type": "RuleRef", "rule": "Expression" } }, { "type": "RuleRef", "rule": "_" }, { "type": "Str", "str": ")" }] }, "code": " return e; " }] }, "LiteralMatcher": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "StringLiteral" } }, "code": " return { type: \"Str\", str: x }; " }] }, "StringLiteral": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Str", "str": "\"" }, { "type": "Labeled", "name": "chars", "child": { "type": "ZeroOrMore", "child": { "type": "RuleRef", "rule": "DoubleStringCharacter" } } }, { "type": "Str", "str": "\"" }] }, "code": " return chars.join(\"\"); " }, { "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Str", "str": "'" }, { "type": "Labeled", "name": "chars", "child": { "type": "ZeroOrMore", "child": { "type": "RuleRef", "rule": "SingleStringCharacter" } } }, { "type": "Str", "str": "'" }] }, "code": " return chars.join(\"\"); " }] }, "DoubleStringCharacter": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "NotPredicate", "child": { "type": "Choice", "childs": [{ "type": "Str", "str": "\"" }, { "type": "Str", "str": "\\" }] } }, { "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "SourceCharacter" } }] }, "code": "return x; " }, { "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Str", "str": "\\" }, { "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "EscapeSequence" } }] }, "code": "return x; " }] }, "SingleStringCharacter": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "NotPredicate", "child": { "type": "Choice", "childs": [{ "type": "Str", "str": "'" }, { "type": "Str", "str": "\\" }, { "type": "RuleRef", "rule": "LineTerminator" }] } }, { "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "SourceCharacter" } }] }, "code": " return x; " }, { "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Str", "str": "\\" }, { "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "SourceCharacter" } }] }, "code": " return x; " }, { "type": "RuleRef", "rule": "LineContinuation" }] }, "EscapeSequence": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Str", "str": "'" }, "code": " return \"'\";  " }, { "type": "Action", "child": { "type": "Str", "str": "\"" }, "code": " return \"\\\"\"; " }, { "type": "Action", "child": { "type": "Str", "str": "\\" }, "code": " return \"\\\\\"; " }, { "type": "Action", "child": { "type": "Str", "str": "b" }, "code": " return \"\\b\"; " }, { "type": "Action", "child": { "type": "Str", "str": "f" }, "code": " return \"\\f\"; " }, { "type": "Action", "child": { "type": "Str", "str": "n" }, "code": " return \"\\n\"; " }, { "type": "Action", "child": { "type": "Str", "str": "r" }, "code": " return \"\\r\"; " }, { "type": "Action", "child": { "type": "Str", "str": "t" }, "code": " return \"\\t\"; " }, { "type": "Action", "child": { "type": "Str", "str": "v" }, "code": " return \"\\v\"; " }] }, "CharacterClassMatcher": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Str", "str": "[" }, { "type": "Labeled", "name": "inverted", "child": { "type": "Optional", "child": { "type": "Str", "str": "^" } } }, { "type": "Labeled", "name": "parts", "child": { "type": "ZeroOrMore", "child": { "type": "RuleRef", "rule": "CharacterPart" } } }, { "type": "Str", "str": "]" }, { "type": "Labeled", "name": "ignoreCase", "child": { "type": "Optional", "child": { "type": "Str", "str": "i" } } }] }, "code": " return { type:\"CharClass\", inverted: inverted, parts:parts, ignoreCase:ignoreCase }; " }] }, "CharacterPart": { "type": "Choice", "childs": [{ "type": "RuleRef", "rule": "ClassCharacterRange" }, { "type": "Action", "child": { "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "ClassCharacter" } }, "code": " return { begin:x, end:x}; " }] }, "ClassCharacterRange": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Labeled", "name": "begin", "child": { "type": "RuleRef", "rule": "ClassCharacter" } }, { "type": "Str", "str": "-" }, { "type": "Labeled", "name": "end", "child": { "type": "RuleRef", "rule": "ClassCharacter" } }] }, "code": " return { begin:begin, end:end }; " }] }, "ClassCharacter": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "NotPredicate", "child": { "type": "Choice", "childs": [{ "type": "Str", "str": "]" }, { "type": "Str", "str": "\\" }, { "type": "RuleRef", "rule": "LineTerminator" }] } }, { "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "SourceCharacter" } }] }, "code": " return x; " }, { "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Str", "str": "\\" }, { "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "EscapeSequence" } }] }, "code": " return x; " }, { "type": "RuleRef", "rule": "LineContinuation" }] }, "LineContinuation": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Str", "str": "\\" }, { "type": "RuleRef", "rule": "LineTerminatorSequence" }] }, "code": " return \"\"; " }] }, "AnyMatcher": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Str", "str": "." }, "code": " return { type: \"AnyChar\" }; " }] }, "RuleReferenceExpression": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Labeled", "name": "name", "child": { "type": "RuleRef", "rule": "Identifier" } }, "code": " return { type: \"RuleRef\", rule: name }; " }] }, "CodeBlock": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Str", "str": "{" }, { "type": "Labeled", "name": "x", "child": { "type": "RuleRef", "rule": "Code" } }, { "type": "Str", "str": "}" }] }, "code": " return x; " }, { "type": "Action", "child": { "type": "Str", "str": "{" }, "code": " error(\"Unbalanced brace.\"); " }] }, "Code": { "type": "Choice", "childs": [{ "type": "Text", "child": { "type": "ZeroOrMore", "child": { "type": "Choice", "childs": [{ "type": "OneOrMore", "child": { "type": "Choice", "childs": [{ "type": "Sequence", "childs": [{ "type": "NotPredicate", "child": { "type": "CharClass", "inverted": null, "parts": [{ "begin": "{", "end": "{" }, { "begin": "}", "end": "}" }], "ignoreCase": null } }, { "type": "RuleRef", "rule": "SourceCharacter" }] }] } }, { "type": "Sequence", "childs": [{ "type": "Str", "str": "{" }, { "type": "RuleRef", "rule": "Code" }, { "type": "Str", "str": "}" }] }] } } }] }, "SourceCharacter": { "type": "Choice", "childs": [{ "type": "AnyChar" }] }, "LineTerminator": { "type": "Choice", "childs": [{ "type": "CharClass", "inverted": null, "parts": [{ "begin": "\r", "end": "\r" }, { "begin": "\n", "end": "\n" }], "ignoreCase": null }] }, "LineTerminatorSequence": { "type": "Choice", "childs": [{ "type": "Sequence", "childs": [{ "type": "Optional", "child": { "type": "Str", "str": "\r" } }, { "type": "Str", "str": "\n" }] }] }, "Identifier": { "type": "Choice", "childs": [{ "type": "Action", "child": { "type": "Sequence", "childs": [{ "type": "Labeled", "name": "x", "child": { "type": "CharClass", "inverted": null, "parts": [{ "begin": "A", "end": "Z" }, { "begin": "a", "end": "z" }, { "begin": "_", "end": "_" }], "ignoreCase": null } }, { "type": "Labeled", "name": "xs", "child": { "type": "ZeroOrMore", "child": { "type": "CharClass", "inverted": null, "parts": [{ "begin": "A", "end": "Z" }, { "begin": "a", "end": "z" }, { "begin": "0", "end": "9" }, { "begin": "_", "end": "_" }], "ignoreCase": null } } }] }, "code": " return String.prototype.concat(x,...xs); " }] }, "__": { "type": "Choice", "childs": [{ "type": "ZeroOrMore", "child": { "type": "CharClass", "inverted": null, "parts": [{ "begin": " ", "end": " " }, { "begin": "\r", "end": "\r" }, { "begin": "\n", "end": "\n" }, { "begin": "\t", "end": "\t" }], "ignoreCase": null } }] }, "_": { "type": "Choice", "childs": [{ "type": "ZeroOrMore", "child": { "type": "CharClass", "inverted": null, "parts": [{ "begin": " ", "end": " " }, { "begin": "\t", "end": "\t" }], "ignoreCase": null } }] }, "EOS": { "type": "Choice", "childs": [{ "type": "Sequence", "childs": [{ "type": "ZeroOrMore", "child": { "type": "CharClass", "inverted": null, "parts": [{ "begin": " ", "end": " " }, { "begin": "\t", "end": "\t" }], "ignoreCase": null } }, { "type": "CharClass", "inverted": null, "parts": [{ "begin": "\r", "end": "\r" }, { "begin": "\n", "end": "\n" }], "ignoreCase": null }] }] } }
        ;
    const builtinParser: { [key: string]: (str: string, ctx: IContext) => IContext | null } = eval(PegKit.compileGrammar(builtinGrammar));

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

    type IValue = Value | ValueList | Nil | EmptyValueList;
    interface Value { type: "Value", start: number, end: number, value: IValue | any };
    interface ValueList { type: "ValueList", value: IValue, next: ValueList | EmptyValueList };
    interface Nil { type: "Nil" };
    interface EmptyValueList { type: "EmptyValueList" };

    interface IContext { sp: number, value: IValue | null };

    let oldGrammarValue: string = "";
    let oldAstValue: string = "";
    let oldCodeValue: string = "";
    let oldInputValue: string = "";
    let parseTimer: number | null = null;
    let compileTimer: number | null = null;
    let buildTimer: number | null = null;
    let runTimer: number | null = null;
    let parser: { [key: string]: (str: string, ctx: IContext) => IContext | null } = null;

    function parse() {
        oldGrammarValue = domGrammar.value;
        const ret = builtinParser["default"](oldGrammarValue, { sp: 0, value: null });
        if (ret != null && ret.value.type === "Value") {
            domAst.value = JSON.stringify(ret.value.value, null, 4);
        } else {
            domAst.value = "Parse Error";
        }
        parseTimer = null;
        return ret != null;
    }

    function compile() {
        oldAstValue = domAst.value;
        domCode.value = PegKit.compileGrammar(JSON.parse(oldAstValue));
        domIr.value = PegKit.compileGrammar2(JSON.parse(oldAstValue));
        compileTimer = null;
        return true;
    }

    function build() {
        oldCodeValue = domCode.value;
        parser = eval(oldCodeValue);
        buildTimer = null;
        return true;
    }

    function decodeValue(x: IValue): any {
        switch (x.type) {
            case "Value": {
                if ((typeof x.value === "object") && (x.value.type === "Value" || x.value.type === "EmptyValueList" || x.value.type === "ValueList")) {
                    return decodeValue(<IValue>x.value);
                } else {
                    return x.value;
                }
            }
            case "EmptyValueList": {
                return [];
            }
            case "Nil": {
                return null;
            }
            case "ValueList": {
                const ret = [];
                while (x.type != "EmptyValueList") {
                    ret.push(decodeValue(x.value));
                    x = x.next;
                }
                return ret.reverse();
            }
        }
    }

    function run() {
        if (parser == null) { return; }
        oldInputValue = domInput.value;
        const ret = parser["default"](oldInputValue, { sp: 0, value: null });
        domOutput.value = JSON.stringify(ret, null, 4);
        console.log(decodeValue(ret.value));
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
  / RuleReferenceExpression
  / "(" _ e:Expression _ ")" { return e; }

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

/*
    const ruleTable = ir.map((x,i) => [x,i]).filter(([x,i])=> x.type == "Rule").reduce((s,[x,i]) => (s[x.name] = i, s), {});
    const labelTable = ir.map((x,i) => [x,i]).filter(([x,i])=> x.type == "Label").reduce((s,[x,i]) => (s[x.id] = i, s), {});
    function cons(car,cdr) {
        if (cdr == null || cdr.type != "cons") { throw new Error("not cons"); }
        return { type:"cons", car:car, cdr:cdr};
    }
    const Nil = Object.freeze({ type:"cons", car:null, cdr: null });
    function atom(v) {
        return { type:"atom", value:v };
    }
    function assoc(key,list)  {
        while (list != Nil) {
            const [k,v] = list.car;
            if (k == key) { return v; }
            list = list.cdr;
        }
    }
    function decode(v) {
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

    function IsCharClass(char, inverted, parts, ignoreCase) {
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

    function step(insts, str, context) {
        let [pc, index, capture, stack, env] = context;

        const ir = insts[pc];
        console.log(ir,context);
        switch (ir.type) {
            case "Char": {
                if (index < 0 || str.length <= index) {
                    pc = labelTable[ir.failLabel];
                    return [pc, index, capture, stack, env];
                }
                const v = str[index]
                if (v == ir.char) {
                    stack = cons(atom(v),stack);
                    index += 1;
                    pc = labelTable[ir.successLabel];
                } else {
                    pc = labelTable[ir.failLabel];
                }
                return [pc, index, capture, stack, env];
            }
            case "AnyChar": {
                if (index < 0 || str.length <= index) {
                    pc = labelTable[ir.failLabel];
                    return [pc, index, capture, stack, env];
                }
                stack = cons(atom(v),stack);
                index += 1;
                pc = labelTable[ir.successLabel];
                return [pc, index, capture, stack, env];
            }
            case "CharClass": {
                if (index < 0 || str.length <= index) {
                    pc = labelTable[ir.failLabel];
                    return [pc, index, capture, stack, env];
                }
                const v = str[index]
                if (isCharClass(v, ir.inverted, ir.parts, ir.ignoreCase)) {
                    stack = cons(atom(v),stack);
                    index += 1;
                    pc = labelTable[ir.successLabel];
                } else {
                    pc = labelTable[ir.failLabel];
                }
                return [pc, index, capture, stack, env];
            }
            case "Str": {
                if (index < 0 || str.length <= index + ir.str.length) {
                    pc = labelTable[ir.failLabel];
                    return [pc, index, capture, stack, env];
                }
                const v = str.substr(index, ir.str.length);
                if (v == ir.str) {
                    stack = cons(atom(v),stack);
                    index += ir.str.length;
                    pc = labelTable[ir.successLabel];
                } else {
                    pc = labelTable[ir.failLabel];
                }
                return [pc, index, capture, stack, env];
            }
            case "Label": {
                return [pc+1, index, capture, stack, env];
            }
            case "Break": {
                return [pc+1, index, capture, stack, env];
            }
            case "Rule": {
                return [pc+1, index, capture, stack, env];
            }
            case "Nip": {
                return [pc+1, index, capture, cons(stack.car, stack.cdr.cdr), env];
            }
            case "Jump": {
                return [labelTable[ir.id], index, capture, stack, env];
            }
            case "Call": {
                return [ruleTable[ir.name], index, Nil, stack, cons([pc+1,capture,stack], env)];
            }
            case "PushContext": {
                return [pc+1, index, capture, cons(context, stack), env];
            }
            case "PopContext": {
                let [pc2, index2, capture2, stack2] = stack.car;
                return [pc+1, index2, capture2, stack2, env];
            }
            case "Pop": {
                return [pc+1, index, capture, stack.cdr, env];
            }
            case "Append": {
                return [pc+1, index, capture, cons(cons(stack.car, stack.cdr.car), stack.cdr.cdr), env];
            }
            case "Capture": {
                return [pc+1, index, cons([ir.name, stack.car],capture), stack, env];
            }
            case "Text": {
                const [pc2, index2, capture2, stack2] = stack.car;
                const sub = str.substr(index2, index-index2);
                console.log(str,index2,index,sub);
                return [pc+1, index, capture, cons(atom(sub),stack.cdr), env];
            }
            case "Action": {
                const pred = eval(`(function() { return function (${ir.captures.join(", ")}) { ${ir.code} }; })()`);
                const args = ir.captures.map(x => decode(assoc(x, capture)));
                console.log("!", ir.captures.map(x => assoc(x, capture)), ir.captures.map(x => decode(assoc(x, capture))));
                const ret = pred(...args);
                return [pc+1, index, capture, cons(atom(ret), stack), env];
            }
            case "PushArray": {
                return [pc+1, index, capture, cons(Nil,stack), env];
            }
            case "Test": {
                const [flag,v] = stack.car;
                if (flag) {
                    pc = labelTable[ir.successLabel];
                    return [pc+1, index, capture, cons(v,stack.cdr), env];
                } else {
                    pc = labelTable[ir.failLabel];
                    return [pc+1, index, capture, stack.cdr, env];
                }
            }
            case "Return": {
                if (ir.success) {
                    const [retpc, retcap, retstack] = env.car;
                    const ret = stack.car;
                    return [retpc, index, retcap, cons([true,ret],retstack), env.cdr];
                } else {
                    const [retpc, retcap, retstack] = env.car;
                    return [retpc, index, retcap, cons([false,null],retstack), env.cdr];
                }
            }
            default:{
                throw new Error(ir.type);
            }
        }
    }

    let context = [0, 0, Nil, Nil, cons([-1,Nil, Nil], Nil)];
    for (let i=0; i<10000 && context[0] >= 0; i++) {
        context = step(ir, "123+456", context)
    }
    {
        const [pc, index, capture, stack, env] = context;
        const [flag,v] = stack.car;
        if (flag) {
            console.log(decode(v))
            return decode(v);
        } else {
            return undefined;
        }
    }


*/
