module PegKit {
    class CodeWriter {

        private indent: number;
        private codes: string[];

        constructor() {
            this.indent = 0;
            this.codes = [];
        }

        up() : CodeWriter{ this.indent += 1; return this; }

        down() : CodeWriter{ this.indent -= 1; return this; }

        writeLine(code:string) : CodeWriter {
            let space = "";
            for (let i = 0; i < this.indent; i++) {
                space += "  ";
            }
            this.codes.push(space + code);
            return this;
        }

        toString() : string {
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

        toString() : string {
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

        generate_one(name:string, t: Ast) : void {
            const successLabel = this.allocLabel();
            const failLabel = this.allocLabel();
            this.code.writeLine(`// rule ${name}`);
            this.code.writeLine(`function $${name}(str, ctx) {`);
            this.code.up();
            this.code.writeLine(`let label = null;`);
            this.code.down().writeLine(`goto: for (;;)`).up();
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
            this.code.down();
            this.code.writeLine(`}`);
            this.code.writeLine(``);
        }

        visit(t: Ast, successLabel: number, failLabel: number) : void {
            switch (t.type) {
                case "Char": this.onChar(t,successLabel, failLabel); break;
                case "Str":this.onStr(t,successLabel, failLabel); break;
                case "Sequence":this.onSequence(t,successLabel, failLabel); break;
                case "Choice":this.onChoice(t,successLabel, failLabel); break;
                case "Optional":this.onOptional(t,successLabel, failLabel); break;
                case "ZeroOrMore":this.onZeroOrMore(t,successLabel, failLabel); break;
                case "OneOrMore":this.onOneOrMore(t,successLabel, failLabel); break;
                case "AndPredicate":this.onAndPredicate(t,successLabel, failLabel); break;
                case "NotPredicate":this.onNotPredicate(t,successLabel, failLabel); break;
                case "Call":this.onCall(t,successLabel, failLabel); break;
                default: throw new Error("not supported");
            }
        }

        onChar(t: { type: "Char", char: string }, successLabel: number, failLabel: number) {
            this.code.writeLine(`if (str[ctx.sp] != "${this.escapeString(t.char)}") { label = "L${failLabel}"; continue goto; } else { ctx = {sp:ctx.sp+1}; label = "L${successLabel}"; continue goto; }`);
        }

        onStr(t: { type: "Str", str: string }, successLabel: number, failLabel: number) {
            this.code.writeLine(`if (str.substr(ctx.sp, ${t.str.length}) != "${this.escapeString(t.str)}") { label = "L${failLabel}"; continue goto; } else { ctx = {sp:ctx.sp+${t.str.length}}; label = "L${successLabel}"; continue goto; }`);
        }

        onSequence(t: { type: "Sequence";items: Ast[] }, successLabel: number, failLabel: number) {
            for (const item of t.items) {
                const nextLabel = this.allocLabel();
                this.visit(item, nextLabel, failLabel);
                this.code.down().writeLine(`case "L${nextLabel}":`).up();
            }
            this.code.writeLine(`label = "L${successLabel}"; continue goto;`);
        }

        onChoice(t: { type: "Choice"; items: Ast[] }, successLabel: number, failLabel: number) {
            for (const item of t.items) {
                const nextLabel = this.allocLabel();
                this.visit(item, successLabel, nextLabel);
                this.code.down().writeLine(`case "L${nextLabel}":`).up();
            }
            this.code.writeLine(`label = "L${failLabel}"; continue goto;`);
        }
        onOptional(t: { type: "Optional"; item: Ast }, successLabel: number, failLabel: number) {
            const tempVar = this.allocVar();
            const junctionLabel = this.allocLabel();
            this.code.writeLine(`var temp${tempVar} = ctx;`);
            this.visit(t.item, successLabel, junctionLabel);
            this.code.down().writeLine(`case "L${junctionLabel}":`).up();
            this.code.writeLine(`ctx = temp${tempVar};`);
            this.code.writeLine(`label = "L${successLabel}"; continue goto;`);
        }
        onZeroOrMore(t: { type: "ZeroOrMore"; item: Ast }, successLabel: number, failLabel: number) {
            const loopLabel = this.allocLabel();
            const tempVar = this.allocVar();
            const junctionLabel = this.allocLabel();
            this.code.down().writeLine(`case "L${loopLabel}":`).up();
            this.code.writeLine(`var temp${tempVar} = ctx;`);
            this.visit(t.item, loopLabel, junctionLabel);
            this.code.down().writeLine(`case "L${junctionLabel}":`).up();
            this.code.writeLine(`ctx = temp${tempVar};`);
            this.code.writeLine(`label = "L${successLabel}"; continue goto;`);
        }
        onOneOrMore(t: { type: "OneOrMore"; item: Ast }, successLabel: number, failLabel: number) {
            const loopLabel = this.allocLabel();
            const tempVar = this.allocVar();
            const junctionLabel = this.allocLabel();
            this.visit(t.item, loopLabel, failLabel);
            this.code.down().writeLine(`case "L${loopLabel}":`).up();
            this.code.writeLine(`var temp${tempVar} = ctx;`);
            this.visit(t.item, loopLabel, junctionLabel);
            this.code.down().writeLine(`case "L${junctionLabel}":`).up();
            this.code.writeLine(`ctx = temp${tempVar};`);
            this.code.writeLine(`label = "L${successLabel}"; continue goto;`);
        }
        onAndPredicate(t: { type: "AndPredicate"; item: Ast }, successLabel: number, failLabel: number) {
            const tempVar = this.allocVar();
            const junctionLabel = this.allocLabel();
            this.code.writeLine(`var temp${tempVar} = ctx;`);
            this.visit(t.item, junctionLabel, failLabel);
            this.code.down().writeLine(`case "L${junctionLabel}":`).up();
            this.code.writeLine(`ctx = temp${tempVar};`);
            this.code.writeLine(`label = "L${successLabel}"; continue goto;`);
        }
        onNotPredicate(t: { type: "NotPredicate"; item: Ast }, successLabel: number, failLabel: number) {
            const tempVar = this.allocVar();
            const junctionLabel = this.allocLabel();
            this.code.writeLine(`var temp${tempVar} = ctx;`);
            this.visit(t.item, failLabel, junctionLabel);
            this.code.down().writeLine(`case "L${junctionLabel}":`).up();
            this.code.writeLine(`ctx = temp${tempVar};`);
            this.code.writeLine(`label = "L${successLabel}"; continue goto;`);
        }
        onCall(t: { type: "Call"; rule: String  }, successLabel: number, failLabel: number) {
            const tempVar = this.allocVar();
            this.code.writeLine(`var temp${tempVar} = $${t.rule}(str, ctx);`);
            this.code.writeLine(`if (temp${tempVar} == null) { label = "L${failLabel}"; continue goto; } else { ctx = temp${tempVar}; label = "L${successLabel}"; continue goto; }`);
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
                { type: "Call", rule: "digits" },
                {
                    type: "ZeroOrMore",
                    item: {
                        type: "Sequence",
                        items: [
                            { type: "Char", char: "," },
                            { type: "Call", rule: "digits" }
                        ]
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

    domAst.value = JSON.stringify(ast,null,4);

    interface IContext { sp: number };

    let oldAstValue: string = "";
    let oldCodeValue: string = "";
    let oldInputValue: string = "";
    let compileTimer: number | null = null;
    let buildTimer: number | null = null;
    let parseTimer: number | null = null;
    let parser: { [key: string]: (str: string, ctx: IContext ) => IContext|null} = null;

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

    function parse() {
        if (parser == null) { return; }
        oldInputValue = domInput.value;
        domOutput.value = JSON.stringify(parser["default"](oldInputValue, { sp: 0 }),null,4);
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
