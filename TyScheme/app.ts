module Scheme {

    type Symbol    = { type: "symbol", value: string };
    type Cell      = { type: "cell", car: Value, cdr: Value };
    type String    = { type: "string", value: string };
    type Number    = { type: "number", value: number };
    type Boolean   = { type: "bool", value: boolean };
    type Primitive = { type: "primitive", value: ((arg: Value) => Value) };
    type Error     = { type: "error", subtype: string, value: string, opt: any };
    type Value = Symbol | Cell | String | Number | Boolean | Primitive | Error;

    const Nil   : Cell = { type: "cell", car: null, cdr: null };
    const False : Boolean = { type: "bool", value: false };
    const True  : Boolean = { type: "bool", value: true };

    function escapeString(str: string): string {
        let buf = '';
        for (let i = 0; i < str.length; i++) {
            switch (str[i]) {
                case "\r": buf += '\r'; break;
                case "\t": buf += '\t'; break;
                case "\n": buf += '\n'; break;
                case "\f": buf += '\f'; break;
                case "\b": buf += '\b'; break;
                case "\\": buf += '\\'; break;
                case '"': buf += '\\"'; break;
                default: buf += str[i]; break;
            }
        }
        return buf;
    }

    export function toString(value: Value, isCdr: boolean = false): string {
        if (value === Nil) { return '()' };
        if (value === False) { return '#f' };
        if (value === True) { return '#t' };
        switch (value.type) {
            case "symbol": return value.value;
            case "cell": return `${(isCdr == false) ? "(" : ""}${toString(value.car)} ${(value.cdr.type == "cell" && value.cdr !== Nil) ? "" : "."} ${toString(value.cdr, true)}${(isCdr == false) ? ")" : ""}`;
            case "string": return `"${escapeString(value.value)}"`;
            case "number": return value.value.toString();
            case "bool": return value.value ? '#t' : '#f';
            case "primitive": return `<primitive:${value.value.name}>`;
            case "error": return `<${value.subtype}:${value.value} ${value.opt == null ? "" : value.opt}>`;
        }
    }

    function car(x: Value): Value {
        if (ispair(x) == False || isnull(x) === True) { return error('runtime error', `Attempt to apply car on ${x}`); }
        return (<Cell>x).car;
    }

    function cdr(x: Value): Value {
        if (ispair(x) == False || isnull(x) === True) { return error('runtime error', `Attempt to apply cdr on ${x}`); }
        return (<Cell>x).cdr;
    }

    function cadr(x: Value): Value { return car(cdr(x)); }
    function caddr(x: Value): Value { return car(cdr(cdr(x))); }
    function cadddr(x: Value): Value { return car(cdr(cdr(cdr(x)))); }
    function cddr(x: Value): Value { return cdr(cdr(x)); }
    function cdddr(x: Value): Value { return cdr(cdr(cdr(x))); }

    function cons(x: Value, y: Value): Cell { return { type: "cell", car: x, cdr: y }; }

    function isnull(x: Value): Boolean { return x === Nil ? True : False; }
    function issymbol(x: Value): Boolean { return x.type == "symbol" ? True : False; }
    function ispair(x: Value): Boolean { return x.type == "cell" ? True : False; }
    function iserror(x: Value): Boolean { return x.type == "error" ? True : False; }

    function nil(): Cell { return Nil; }
    function symbol(x: string): Symbol { return { type: "symbol", value: x }; }
    function number(x: number): Number { return { type: "number", value: x }; }
    function string(x: string): String { return { type: "string", value: x }; }
    function boolean(x: boolean): Boolean { return x ? True : False; }
    function primitive(v: any): Primitive { return { type: "primitive", value: v }; }
    function error(subtype: string, x: string, opt: any = null): Error { return { type: "error", subtype: subtype, value: x, opt: opt } }

    function list(...xs: Value[]): Cell { return xs.reduceRight<Cell>((s, x) => cons(x, s), nil()); }
    function list2(...xs: Value[]): Cell {
        const tail = xs.splice(xs.length - 2, 2);
        return xs.reduceRight<Cell>((s, x) => cons(x, s), cons(tail[0], tail[1]));
    }

    function length(x: Value): number {
        for (let i = 0; ; i++) {
            if (ispair(x) === False) { throw new Error("bad"); }
            if (isnull(x) === True) { return i; }
            x = cdr(x);
        }
    }
    function list_ref(x: Value, i: number): Value {
        while (i > 0) {
            x = cdr(x);
        }
        return x;
    }
    function eq(x: Value, y: Value): Boolean {
        if (x.type == y.type) {
            switch (x.type) {
                case "symbol": return (x.value == (<Symbol>y).value) ? True : False;
                case "cell"  : return (x === y) ? True : False;
                case "number": return (x.value === (<Number>y).value) ? True : False;
                case "string": return (x.value === (<String>y).value) ? True : False;
                case "bool"  : return (x.value === (<Boolean>y).value) ? True : False;
            }
        }
        return False;
    }

    // 変数の位置を求める
    function position_var(sym: Value, ls: Value): number | false {
        for (let i = 0; ; i++) {
            if (isnull(ls) === True) { return false; }
            if (issymbol(ls) === True) { return eq(sym, ls) === True ? -(i + 1) : false; }
            if (eq(sym, car(ls)) === True) { return i; }
            ls = cdr(ls);
        }
    }

    // フレームと変数の位置を求める
    function location(sym: Value, ls: Value): [number, number] | false {
        for (let i = 0; ; i++) {
            if (isnull(ls) === True) { return false; }
            const j = position_var(sym, cdr(ls));
            if (j !== false) {
                return [i, j];
            } else {
                ls = cdr(ls);
            }
        }
    }

    // 自己評価フォームか
    function is_self_evaluation(expr: Value): boolean {
        return (ispair(expr) === False) && (issymbol(expr) === False);

    }

    // S 式をコンパイルする
    export function compile(expr: Value): Value {
        if (iserror(expr) === True) {
            return expr;
        }
        return comp(expr, Nil, list(symbol("stop")));
    }

    // コンパイラ本体
    function comp(expr: Value, env: Value, code: Value): Value {
        if (is_self_evaluation(expr)) {
            // 自己評価フォーム
            return list2(symbol("ldc"), expr, code);
        }
        if (issymbol(expr) === True) {
            // 変数
            const pos = location(expr, env);
            if (pos !== false) {
                // 局所変数
                return list2(symbol("ld"), cons(number(pos[0]), number(pos[1])), code);
            } else {
                // 大域変数
                return list2(symbol("ldg"), expr, code);
            }
        }
        if (eq(car(expr), symbol("quote")) === True) {
            return list2(symbol("ldc"), cadr(expr), code);
        }
        if (eq(car(expr), symbol("if")) === True) {
            const t_clause = comp(caddr(expr), env, list(symbol("join")));
            const f_clause = (isnull(cdddr(expr)) === True) ? list(symbol("ldc"), symbol("*undef"), symbol("join")) : comp(cadddr(expr), env, list(symbol("join")));
            return comp(cadr(expr), env, list2(symbol("sel"), t_clause, f_clause, code));
        }
        if (eq(car(expr), symbol("lambda")) === True) {
            const body = comp_body(cddr(expr), cons(cadr(expr), env), list(symbol("rtn")));
            return list2(symbol("ldf"), body, code);
        }
        if (eq(car(expr), symbol("define")) === True) {
            return comp(cddr(expr), env, list2(symbol("def"), cadr(expr), code));
        }
        {
            return complis(cdr(expr), env, list2(symbol("args"), number(length(cdr(expr))), comp(car(expr), env, cons(symbol("app"), code))));
        }
    }

    // body のコンパイル
    function comp_body(body: Value, env: Value, code: Value): Value {
        if (isnull(cdr(body)) === True) {
            return comp(car(body), env, code);
        } else {
            return comp(car(body), env, list2(symbol("pop"), comp_body(cdr(body), env, code)));
        }
    }

    // 引数を評価するコードを生成
    function complis(expr: Value, env: Value, code: Value): Value {
        return isnull(expr) === True ? code : comp(car(expr), env, complis(cdr(expr), env, code));
    }

    /* 仮想マシン */

    // ls の先頭から n 個の要素を取り除く
    function drop(ls: Value, n: number): Value {
        for (; n > 0; n--) {
            ls = cdr(ls);
        }
        return ls;
    }

    // 局所変数の値を求める
    function get_lvar(e: Value, i: number, j: number): Value {
        if (0 <= j) {
            return list_ref(list_ref(e, i), j);
        } else {
            return drop(list_ref(e, i), -(j + 1));
        }
    }

    // 大域変数
    let global_environment: Cell =
        list(
            cons(symbol("#t"), True),
            cons(symbol("#f"), False),

            cons(symbol("car"), list(symbol("primitive"), primitive((xs) => car(car(xs))))),
            cons(symbol("cdr"), list(symbol("primitive"), primitive((xs) => cdr(car(xs))))),
            cons(symbol("cons"), list(symbol("primitive"), primitive((xs) => cons(car(xs), cadr(xs))))),
            cons(symbol("eq?"), list(symbol("primitive"), primitive((xs) => eq(car(xs), cadr(xs))))),
            cons(symbol("pair?"), list(symbol("primitive"), primitive((xs) => ispair(car(xs))))),
            cons(symbol("+"), list(symbol("primitive"), primitive((xs) => {
                let v = 0;
                while (isnull(xs) === False) {
                    v += (<Number>car(xs)).value;
                    xs = cdr(xs);
                }
                return number(v);
            })))
        );

    function assoc(sym: Value, dic: Value): Value {
        while (isnull(dic) === False) {
            const entry = car(dic);
            const key = car(entry);
            if (eq(key, sym) === True) {
                return entry;
            }
            dic = cdr(dic);
        }
        return False;
    }

    // 大域変数の値を求める
    function get_gvar(sym: Value): Value {
        const val = assoc(sym, global_environment);
        if (val !== False) {
            return cdr(val);
        } else {
            return error('runtime error', `unbound variable: ${sym}`);
        }
    }

    interface VMContext {
        trace: boolean;
        halt: boolean;
        s: Value;
        e: Value;
        c: Value;
        d: Value;
    }

    // 仮想マシンでコードを実行する
    function vm({ trace, halt, s, e, c, d }: VMContext): VMContext {
        if (halt) {
            return {
                trace: trace, 
                halt: true,
                s: s,
                e: e,
                c: c,
                d: d
            };
        }
        const v = <Symbol>car(c);
        if (iserror(v) === True) {
            return {
                trace: trace, 
                halt: true,
                s: cons(v, s),
                e: e,
                c: c,
                d: d
            };
        }
        c = cdr(c);
        switch (v.value) {
            case "ld": {
                const pos = car(c);
                return {
                    trace: trace, 
                    halt: false,
                    s: cons(get_lvar(e, (<Number>car(pos)).value, (<Number>cdr(pos)).value), s),
                    e: e,
                    c: cdr(c),
                    d: d,
                };
            }
            case "ldc": {
                return {
                    trace: trace, 
                    halt: false,
                    s: cons(car(c), s),
                    e: e,
                    c: cdr(c),
                    d: d,
                };
            }
            case "ldg": {
                return {
                    trace: trace, 
                    halt: false,
                    s: cons(get_gvar(car(c)), s),
                    e: e,
                    c: cdr(c),
                    d: d,
                };
            }
            case "ldf": {
                return {
                    trace: trace, 
                    halt: false,
                    s: cons(list(symbol("closure"), car(c), e), s),
                    e: e,
                    c: cdr(c),
                    d: d,
                };
            }
            case "app": {
                const clo = car(s);
                const lvar = cadr(s);
                if (eq(car(clo), symbol("primitive")) === True) {
                    const ret = (<Primitive>cadr(clo)).value(lvar);
                    return {
                        trace: trace, 
                        halt: false,
                        s: cons(ret, cddr(s)),
                        e: e,
                        c: c,
                        d: d,
                    };
                } else {
                    return {
                        trace: trace, 
                        halt: false,
                        s: nil(),
                        e: cons(lvar, caddr(clo)),
                        c: cadr(clo),
                        d: cons(list(cddr(s), e, c), d),
                    };
                }
            }
            case "rtn": {
                const save = car(d);
                return {
                    trace: trace, 
                    halt: false,
                    s: cons(car(s), cdr(save)),
                    e: cadr(save),
                    c: caddr(save),
                    d: cdr(d),
                };
            }
            case "sel": {
                const t_clause = car(c);
                const e_clause = cadr(c);
                const v = (<Boolean>car(s)).value;
                return {
                    trace: trace, 
                    halt: false,
                    s: cdr(s),
                    e: e,
                    c: v ? t_clause : e_clause,
                    d: cons(cddr(c), d),
                };
            }
            case "join": {
                return {
                    trace: trace, 
                    halt: false,
                    s: s,
                    e: e,
                    c: car(d),
                    d: cdr(d),
                };
            }
            case "pop": {
                return {
                    trace: trace, 
                    halt: false,
                    s: cdr(s),
                    e: e,
                    c: c,
                    d: d,
                };
            }
            case "args": {
                for (let n = (<Number>car(c)).value, a = Nil; ; n--) {
                    if (n == 0) {
                        return {
                            trace: trace, 
                            halt: false,
                            s: cons(a, s),
                            e: e,
                            c: cdr(c),
                            d: d,
                        };
                    }
                    a = cons(car(s), a)
                    s = cdr(s);
                }
            }
            case "def": {
                const sym = car(c);
                global_environment = cons(cons(sym, car(s)), global_environment);
                return {
                    trace: trace, 
                    halt: false,
                    s: cons(sym, cdr(s)),
                    e: e,
                    c: cdr(c),
                    d: d,
                };
            }
            case "stop": {
                return {
                    trace: trace, 
                    halt: true,
                    s: s,
                    e: e,
                    c: c,
                    d: d
                };
            }
            default: {
                return {
                    trace: trace, 
                    halt: true,
                    s: cons(error('runtime error', `unknown opcode '${v.value}'`), s),
                    e: e,
                    c: c,
                    d: d
                };
            }
        }
    }

    export function run(expr: Value, timeout: number = 1000): Value {

        if (iserror(expr) === True) { return expr; }

        let run = true;
        let context: VMContext = { trace: false, halt: false, s: Nil, e: Nil, c: expr, d: Nil };

        const handle = setTimeout(() => run = false, timeout);
        while (run && !context.halt) {
            context = vm(context);
        }
        clearTimeout(handle);

        return isnull(context.s) == True ? Nil : car(context.s);
    }

    export function parse(stream: string): Value {
        const not_whitespace_or_end = /^(\S|$)/;
        const space_quote_paren_escaped_or_end = /^(\s|\\|"|'|`|,|\(|\)|$)/;
        const string_or_escaped_or_end = /^(\\|"|$)/;
        const quotes = /('|`|,)/;
        const quotes_map = {
            '\'': 'quote',
            '`': 'quasiquote',
            ',': 'unquote'
        };
        const context = {
            line: 0,
            col: 0,
            pos: 0,
            stream: stream
        };

        function syntax_error(msg: string): Error {
            return error('syntax error', msg, { line: this._line + 1, col: this._col + 1 });
        }

        function peek_char(): string {
            return (context.stream.length == context.pos) ? '' : stream[context.pos];
        }

        function consume_char(): string {
            if (context.stream.length == context.pos) { return ''; }

            let c = context.stream[context.pos];
            context.pos += 1;

            switch (c) {
                case '\r': {
                    if (peek_char() == '\n') {
                        context.pos += 1;
                        c += '\n';
                    }
                    context.line++;
                    context.col = 0;
                    break;

                }
                case '\n': {
                    context.line++;
                    context.col = 0;
                    break;
                }
                default: {
                    context.col++;
                    break;
                }
            }

            return c;
        }

        function until_char(regex: RegExp): string {
            let s = '';
            while (!regex.test(peek_char())) {
                s += consume_char();
            }
            return s;
        }

        function parse_string(): String | Error {
            // consume "
            consume_char();

            let buf = '';

            while (true) {
                buf += until_char(string_or_escaped_or_end);
                let next = peek_char();

                if (next == '') {
                    return syntax_error('Unterminated string literal');
                } else if (next == '"') {
                    consume_char();
                    break;
                } else if (next == '\\') {
                    consume_char();
                    switch (peek_char()) {
                        case 'r':
                            consume_char();
                            buf += '\r';
                            break;
                        case 't':
                            consume_char();
                            buf += '\t';
                            break;
                        case 'n':
                            consume_char();
                            buf += '\n';
                            break;
                        case 'f':
                            consume_char();
                            buf += '\f';
                            break;
                        case 'b':
                            consume_char();
                            buf += '\b';
                            break;
                        default:
                            buf += consume_char();
                            break;
                    }
                }
            }

            // wrap in object to make strings distinct from symbols
            return string(buf);
        }

        function ignore_whitespace() {
            return until_char(not_whitespace_or_end);
        }

        function parse_atom(): Value | null {
            if (peek_char() == '"') {
                return parse_string();
            }

            let atom = '';
            while (true) {
                atom += until_char(space_quote_paren_escaped_or_end);
                const next = peek_char();
                if (next == '\\') {
                    consume_char();
                    atom += consume_char();
                    continue;
                }

                break;
            }
            if (atom == '') {
                return null;
            }

            const num = Number.parseInt(atom);
            return Number.isNaN(num) ? symbol(atom) : number(num);
        }

        function parse_quoted(): Value {
            let q = consume_char();
            let quote = quotes_map[q];

            if (quote == "unquote" && peek_char() == "@") {
                consume_char();
                quote = "unquote-splicing";
                q = ',@';
            }

            ignore_whitespace();
            const quotedExpr = parse_expr();

            if (iserror(quotedExpr) === True) {
                return quotedExpr;
            }

            // nothing came after '
            if (quotedExpr === null) {
                return syntax_error('Unexpected `' + peek_char() + '` after `' + q + '`');
            }

            return list(symbol(quote), quotedExpr);
        }

        function parse_expr(): Value | null {
            ignore_whitespace();

            if (quotes.test(peek_char())) {
                return parse_quoted();
            } else {
                const expr = peek_char() == '(' ? parse_list() : parse_atom();
                ignore_whitespace()
                return expr;
            }
        }

        function parse_list(): Value {
            if (peek_char() != '(') {
                return syntax_error('Expected `(` - saw `' + peek_char() + '` instead.');
            }

            consume_char();

            const ls = [];
            let v = parse_expr();

            if (iserror(v) === True) {
                return v;
            }

            let tail = <Value>nil();
            if (v !== null) {
                ls.push(v);

                while ((v = parse_expr()) !== null) {
                    if (iserror(v) === True) { return v; }
                    if (issymbol(v) && (<Symbol>v).value === ".") { 
                        if ((v = parse_expr()) === null) { return syntax_error('no expr exists'); }
                        if (iserror(v) === True) { return v; }
                        tail = v;
                        break;
                    }
                    ls.push(v);
                }
            }

            if (peek_char() != ')') {
                return syntax_error('Expected `)` - saw: `' + peek_char() + '`');
            }

            // consume that closing paren
            consume_char();

            return ls.reduceRight((s, x) => cons(x, s), tail);
        }

        const expression = parse_expr();

        if (iserror(expression) === True) {
            return expression;
        }

        // if anything is left to parse, it's a syntax error
        if (peek_char() != '') {
            return syntax_error('Superfluous characters after expression: `' + peek_char() + '`');
        }

        return expression;
    }
}

window.onload = () => {
    const textAreaInput = document.createElement("textarea");
    const textAreaOutput = document.createElement("textarea");
    const buttonRun = document.createElement("button");
    document.body.appendChild(textAreaInput);
    document.body.appendChild(buttonRun);
    document.body.appendChild(textAreaOutput);
    buttonRun.innerHTML = "run";
    buttonRun.addEventListener("click", () => {
        const source = textAreaInput.value;
        const parsed = Scheme.parse(source);
        textAreaOutput.value = '';
        textAreaOutput.value += '<src>\r\n';
        textAreaOutput.value += Scheme.toString(parsed);
        textAreaOutput.value += '\r\n';
        textAreaOutput.value += '<compile>\r\n';
        const compiled = Scheme.compile(parsed);
        textAreaOutput.value += Scheme.toString(compiled);
        textAreaOutput.value += '\r\n';
        textAreaOutput.value += '<run>\r\n';
        const result = Scheme.run(compiled);
        textAreaOutput.value += Scheme.toString(result);
        textAreaOutput.value += '\r\n';
    });
};
