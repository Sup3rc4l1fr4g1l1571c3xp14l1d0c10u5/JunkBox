module PegKit {
    class CodeWriter {

        private indent: number;
        private codes: string[];

        constructor() {
            this.indent = 0;
            this.codes = [];
        }

        up(): CodeWriter { this.indent += 1; return this; }

        down(): CodeWriter { this.indent -= 1; return this; }

        writeLine(code: string): CodeWriter {
            let space = "";
            for (let i = 0; i < this.indent; i++) {
                space += "  ";
            }
            this.codes.push(space + code);
            return this;
        }

        toString(): string {
            return this.codes.join("\n");
        }
    };

    interface ICodeGenerator {
        generate(g: Grammar): void;
    }

    class JavascriptGenerator implements ICodeGenerator {
        private code: CodeWriter;
        private labelId: number;
        private tempVarId: number;

        private escapeString(str: string) {
            const s = JSON.stringify(str);
            return s.substr(1, s.length - 2);
        }

        public allocLabel(): number {
            return this.labelId++;
        }

        public allocVar(): number {
            return this.tempVarId++;
        }

        constructor() {
            this.code = new CodeWriter();
            this.labelId = 1;
            this.tempVarId = 1;
        }

        toString(): string {
            return this.code.toString();
        }

        generate(g: Grammar): void {
            this.code.writeLine(`(function (){`);
            this.code.up();
            for (const key of Object.keys(g)) {
                this.generate_one(key, g[key]);
            }

            this.code.writeLine(`return {`);
            this.code.up();
            Object.keys(g).map(x => `${x}: $${x}`).join(",\n").split("\n").forEach(x => this.code.writeLine(x));
            this.code.down();
            this.code.writeLine(`};`);

            this.code.down();
            this.code.writeLine(`})();`);
        }

        generate_one(name: string, t: Ast): void {
            const successLabel = this.allocLabel();
            const failLabel = this.allocLabel();
            this.code.writeLine(`// rule ${name}`);
            this.code.writeLine(`function $${name}(str, ctx) {`);
            this.code.up();
            this.code.writeLine(`let label = null;`);
            this.code.writeLine(`let i = 0;`);
            this.code.down().writeLine(`goto: for (;;) {`).up();
            this.code.writeLine(`i++; if (i > 1000) { throw new Error(); } `);
            this.code.writeLine(`switch (label) {`);
            this.code.up();
            this.code.down().writeLine(`case null:`).up();
            this.visit(t, successLabel, failLabel);
            this.code.down().writeLine(`case "L${successLabel}":`).up();
            this.code.writeLine(`return ctx;`);
            this.code.down().writeLine(`case "L${failLabel}":`).up();
            this.code.writeLine(`return null;`);
            this.code.down();
            this.code.writeLine(`}`);
            this.code.writeLine(`}`);
            this.code.down();
            this.code.writeLine(`}`);
            this.code.writeLine(``);
        }

        visit(t: Ast, successLabel: number, failLabel: number): void {
            switch (t.type) {
                case "Char": this.onChar(t, successLabel, failLabel); break;
                case "Str": this.onStr(t, successLabel, failLabel); break;
                case "Sequence": this.onSequence(t, successLabel, failLabel); break;
                case "Choice": this.onChoice(t, successLabel, failLabel); break;
                case "Optional": this.onOptional(t, successLabel, failLabel); break;
                case "ZeroOrMore": this.onZeroOrMore(t, successLabel, failLabel); break;
                case "OneOrMore": this.onOneOrMore(t, successLabel, failLabel); break;
                case "AndPredicate": this.onAndPredicate(t, successLabel, failLabel); break;
                case "NotPredicate": this.onNotPredicate(t, successLabel, failLabel); break;
                case "Call": this.onCall(t, successLabel, failLabel); break;
                case "Labeled": this.onLabeled(t, successLabel, failLabel); break;
                default: throw new Error("not supported");
            }
        }

        labelDef(label: number) {
            this.code.down().writeLine(`case "L${label}":`).up();
        }

        jumpTo(label: number) {
            this.code.writeLine(`label = "L${label}"; continue goto;`);
        }

        onChar(t: { type: "Char", char: string }, successLabel: number, failLabel: number) {
            this.code.writeLine(`if (str[ctx.sp] != "${this.escapeString(t.char)}") {`);
            this.code.up();
            this.jumpTo(failLabel);
            this.code.down();
            this.code.writeLine(`} else {`);
            this.code.up();
            this.code.writeLine(`ctx = {sp:ctx.sp+1, value:{type:"Value", start:ctx.sp, end:ctx.sp+1, value:str[ctx.sp]}};`);
            this.jumpTo(successLabel);
            this.code.down();
            this.code.writeLine(`}`);
        }

        onStr(t: { type: "Str", str: string }, successLabel: number, failLabel: number) {
            this.code.writeLine(`if (str.substr(ctx.sp, ${t.str.length}) != "${this.escapeString(t.str)}") {`);
            this.code.up();
            this.jumpTo(failLabel);
            this.code.down();
            this.code.writeLine(`} else {`);
            this.code.up();
            this.code.writeLine(`ctx = {sp:ctx.sp+${t.str.length}, value:{start:ctx.sp, end:ctx.sp+${t.str.length}, value:str.substr(ctx.sp, ${t.str.length}) }}}; `);
            this.jumpTo(successLabel);
            this.code.down();
            this.code.writeLine(`}`);
        }

        onSequence(t: { type: "Sequence"; items: Ast[] }, successLabel: number, failLabel: number) {
            const junctionLabel = this.allocLabel();
            const tempVar1 = this.allocVar();
            const tempVar2 = this.allocVar();

            this.code.writeLine(`var temp${tempVar1} = {type:"EmptyValueList"};`);
            this.code.writeLine(`var temp${tempVar2} = ctx;`);
            for (const item of t.items) {
                const nextLabel = this.allocLabel();
                this.visit(item, nextLabel, junctionLabel);
                this.labelDef(nextLabel);
                this.code.writeLine(`temp${tempVar1} = {type:"ValueList", value:ctx.value, next:temp${tempVar1}};`);
                this.code.writeLine(`ctx = {sp:ctx.sp, value:null};`);
            }
            this.code.writeLine(`ctx = {sp:ctx.sp, value:temp${tempVar1}};`);
            this.jumpTo(successLabel);
            this.labelDef(junctionLabel);
            this.code.writeLine(`ctx = temp${tempVar2};`);
            this.jumpTo(failLabel);
        }

        onChoice(t: { type: "Choice"; items: Ast[] }, successLabel: number, failLabel: number) {
            for (const item of t.items) {
                const nextLabel = this.allocLabel();
                this.visit(item, successLabel, nextLabel);
                this.labelDef(nextLabel);
            }
            this.jumpTo(failLabel);
        }

        onOptional(t: { type: "Optional"; item: Ast }, successLabel: number, failLabel: number) {
            const tempVar = this.allocVar();
            const junctionLabel = this.allocLabel();
            this.code.writeLine(`var temp${tempVar} = ctx;`);
            this.visit(t.item, successLabel, junctionLabel);
            this.labelDef(junctionLabel);
            this.code.writeLine(`ctx = temp${tempVar};`);
            this.jumpTo(successLabel);
        }
        onZeroOrMore(t: { type: "ZeroOrMore"; item: Ast }, successLabel: number, failLabel: number) {
            const loopLabel = this.allocLabel();
            const succLabel = this.allocLabel();
            const junctionLabel = this.allocLabel();
            const tempVar1 = this.allocVar();
            const tempVar2 = this.allocVar();
            const tempVar3 = this.allocVar();
            this.code.writeLine(`var temp${tempVar1} = ctx;`);
            this.code.writeLine(`var temp${tempVar2} = {type:"EmptyValueList"};`);
            this.labelDef(loopLabel);
            this.code.writeLine(`var temp${tempVar3} = ctx;`);
            this.code.writeLine(`ctx = {sp:ctx.sp, value:null};`);
            this.visit(t.item, succLabel, junctionLabel);
            this.labelDef(succLabel);
            this.code.writeLine(`temp${tempVar2} ={type: "ValueList", value:ctx.value, next: temp${tempVar2}};`);
            this.code.writeLine(`ctx = {sp:ctx.sp, value:null};`);
            this.jumpTo(loopLabel);
            this.labelDef(junctionLabel);
            this.code.writeLine(`ctx = {sp:temp${tempVar3}.sp, value:{type: "Value", start:temp${tempVar1}.sp, end:temp${tempVar3}.sp, value:temp${tempVar2} }};`);
            this.jumpTo(successLabel);
        }
        onOneOrMore(t: { type: "OneOrMore"; item: Ast }, successLabel: number, failLabel: number) {
            const rollbackLabel = this.allocLabel();
            const loopLabel = this.allocLabel();
            const junctionLabel = this.allocLabel();
            const tempVar1 = this.allocVar();
            const tempVar2 = this.allocVar();
            const tempVar3 = this.allocVar();
            this.code.writeLine(`var temp${tempVar1} = ctx;`);
            this.code.writeLine(`var temp${tempVar2} = {type:"EmptyValueList"};`);
            this.code.writeLine(`var temp${tempVar3} = ctx;`);
            this.code.writeLine(`ctx = {sp:ctx.sp, value:null};`);
            this.visit(t.item, loopLabel, rollbackLabel);
            this.labelDef(rollbackLabel);
            this.code.writeLine(`ctx = temp${tempVar1};`);
            this.jumpTo(failLabel);
            this.labelDef(loopLabel);
            this.code.writeLine(`temp${tempVar2} = {type: "ValueList", value:ctx.value, next: temp${tempVar2}};`);
            this.code.writeLine(`temp${tempVar3} = {sp:ctx.sp, value:temp${tempVar2}};`);
            this.code.writeLine(`ctx = {sp:ctx.sp, value:null};`);
            this.visit(t.item, loopLabel, junctionLabel);
            this.labelDef(junctionLabel);
            this.code.writeLine(`ctx = {sp:temp${tempVar3}.sp, value:{type: "Value", start:temp${tempVar1}.sp, end:temp${tempVar3}.sp, value:temp${tempVar2} }};`);
            this.jumpTo(successLabel);
        }
        onAndPredicate(t: { type: "AndPredicate"; item: Ast }, successLabel: number, failLabel: number) {
            const tempVar = this.allocVar();
            const junctionLabel = this.allocLabel();
            this.code.writeLine(`var temp${tempVar} = ctx;`);
            this.visit(t.item, junctionLabel, failLabel);
            this.labelDef(junctionLabel);
            this.code.writeLine(`ctx = temp${tempVar};`);
            this.jumpTo(successLabel);
        }
        onNotPredicate(t: { type: "NotPredicate"; item: Ast }, successLabel: number, failLabel: number) {
            const tempVar = this.allocVar();
            const junctionLabel = this.allocLabel();
            this.code.writeLine(`var temp${tempVar} = ctx;`);
            this.visit(t.item, failLabel, junctionLabel);
            this.labelDef(junctionLabel);
            this.code.writeLine(`ctx = temp${tempVar};`);
            this.jumpTo(successLabel);
        }
        onCall(t: { type: "Call"; rule: String }, successLabel: number, failLabel: number) {
            const tempVar = this.allocVar();
            this.code.writeLine(`var temp${tempVar} = $${t.rule}(str, ctx);`);
            this.code.writeLine(`if (temp${tempVar} == null) {`);
            this.code.up();
            this.jumpTo(failLabel);
            this.code.down();
            this.code.writeLine(`} else {`);
            this.code.up();
            this.code.writeLine(`ctx = temp${tempVar};`);
            this.jumpTo(successLabel);
            this.code.down();
            this.code.writeLine(`}`);
        }
        onLabeled(t: { type: "Labeled"; name: String; item: Ast }, successLabel: number, failLabel: number) {
            const junctionLabel = this.allocLabel();
            this.visit(t.item, junctionLabel, failLabel);
            this.labelDef(junctionLabel);
            this.code.writeLine(`var capture${t.name} = ctx.value;`);
            this.jumpTo(successLabel);
        }
    }

    export type Ast =
        { type: "Char", char: string }
        | { type: "Str", str: string }
        | { type: "Sequence", items: Ast[] }
        | { type: "Choice", items: Ast[] }
        | { type: "Optional", item: Ast }
        | { type: "ZeroOrMore", item: Ast }
        | { type: "OneOrMore", item: Ast }
        | { type: "AndPredicate", item: Ast }
        | { type: "NotPredicate", item: Ast }
        | { type: "Call", rule: String }
        | { type: "Labeled", name: String, item: Ast }
        ;

    export type Grammar = { [key: string]: Ast };

    export function compileGrammar(g: Grammar): string {
        let generator = new JavascriptGenerator();
        generator.generate(g);
        return generator.toString();
    }
}
window.onload = () => {
    const ast: PegKit.Grammar = {
        default: {
            type: "Call",
            rule: "csv"
        },
        csv: {
            type: "Sequence",
            items: [
                {
                    type: "Labeled",
                    name: "x",
                    item: { type: "Call", rule: "digits" },
                },
                {
                    type: "Labeled",
                    name: "xs",
                    item: {
                        type: "ZeroOrMore",
                        item: {
                            type: "Sequence",
                            items: [
                                { type: "Char", char: "," },
                                { type: "Call", rule: "digits" }
                            ]
                        }
                    }
                }
            ]
        },
        digits: {
            type: "OneOrMore",
            item: { type: "Call", rule: "digit" }
        },
        digit: {
            type: "Choice",
            items: [
                { type: "Char", char: "0" },
                { type: "Char", char: "1" },
                { type: "Char", char: "2" },
                { type: "Char", char: "3" },
                { type: "Char", char: "4" },
                { type: "Char", char: "5" },
                { type: "Char", char: "6" },
                { type: "Char", char: "7" },
                { type: "Char", char: "8" },
                { type: "Char", char: "9" }
            ]
        }
    };

    const domAst = <HTMLTextAreaElement>document.getElementById('ast');
    const domCode = <HTMLTextAreaElement>document.getElementById('code');
    const domInput = <HTMLTextAreaElement>document.getElementById('input');
    const domOutput = <HTMLTextAreaElement>document.getElementById('output');

    function addChangeEvent<K extends keyof HTMLElementEventMap>(dom: HTMLElement, listener: (this: HTMLElement, ev: HTMLElementEventMap[K]) => any): void {
        const events = ["change", "mousedown", "mouseup", "click", "keydown", "keyup", "keypress"];
        events.forEach(x => dom.addEventListener(x, listener));
    }

    domAst.value = JSON.stringify(ast, null, 4);

    type IValue = Value | ValueList | EmptyValueList;
    interface Value { type: "Value", start: number, end: number, value: IValue | any };
    interface ValueList { type: "ValueList", value: IValue, next: ValueList | EmptyValueList };
    interface EmptyValueList { type: "EmptyValueList" };

    interface IContext { sp: number, value: IValue | null };

    let oldAstValue: string = "";
    let oldCodeValue: string = "";
    let oldInputValue: string = "";
    let compileTimer: number | null = null;
    let buildTimer: number | null = null;
    let parseTimer: number | null = null;
    let parser: { [key: string]: (str: string, ctx: IContext) => IContext | null } = null;

    function compile() {
        oldAstValue = domAst.value;
        domCode.value = PegKit.compileGrammar(JSON.parse(oldAstValue));
        compileTimer = null;
        return true;
    }

    function build() {
        oldCodeValue = domCode.value;
        parser = eval(oldCodeValue);
        buildTimer = null;
        return true;
    }

    function isEntry(x: any) {
        return (x != null) && (typeof x === "object") && (Object.keys(x).length === 4) && (typeof x["start"] === "number") && (typeof x["end"] === "number") && (typeof x["value"] !== "undefined") && (typeof x["next"] !== "undefined");
    }
    function rbuild(x: IValue): any {
        switch (x.type) {
            case "Value": {
                if ((typeof x.value === "object") && (x.value["type"] == "Value" || x.value["type"] == "EmptyValueList" || x.value["type"] == "ValueList")) {
                    return rbuild(x.value);
                } else {
                    return x.value;
                }
            }
            case "EmptyValueList": {
                return [];
            }
            case "ValueList": {
                const ret = [];
                while (x.type != "EmptyValueList") {
                    ret.push(rbuild(x.value));
                    x = x.next;
                }
                return ret.reverse();
            }
        }
    }

    function parse() {
        if (parser == null) { return; }
        oldInputValue = domInput.value;
        const ret = parser["default"](oldInputValue, { sp: 0, value: null });
        domOutput.value = JSON.stringify(ret, null, 4);
        console.log(rbuild(ret.value));
        parseTimer = null;
        return true;
    }

    function compileAndBuildAndParse() {
        return compile() && build() && parse();
    }
    function buildAndParse() {
        return build() && parse();
    }

    addChangeEvent(domAst, () => {
        const nothingChanged = domAst.value == oldAstValue;
        if (nothingChanged) { return; }
        if (compileTimer !== null) {
            clearTimeout(compileTimer);
            compileTimer = null;
        }
        compileTimer = setTimeout(compileAndBuildAndParse, 500);
    });

    addChangeEvent(domCode, () => {
        const nothingChanged = domCode.value == oldCodeValue;
        if (nothingChanged) { return; }
        if (buildTimer !== null) {
            clearTimeout(buildTimer);
            buildTimer = null;
        }
        buildTimer = setTimeout(buildAndParse, 500);
    });

    addChangeEvent(domInput, () => {
        const nothingChanged = domInput.value == oldInputValue;
        if (nothingChanged) { return; }
        if (parseTimer !== null) {
            clearTimeout(parseTimer);
            parseTimer = null;
        }
        parseTimer = setTimeout(parse, 500);
    });
};
