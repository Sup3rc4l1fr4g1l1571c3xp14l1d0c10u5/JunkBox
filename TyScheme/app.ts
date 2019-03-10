module Scheme {

    type Symbol = { type: "symbol", value: string };
    type Cell = { type: "cell", car: Value, cdr: Value };
    type String = { type: "string", value: string };
    type Number = { type: "number", value: number };
    type Boolean = { type: "bool", value: boolean };
    type Primitive = { type: "primitive", value: ((arg: Value) => Value) };
    type Value = Symbol | Cell | String | Number | Boolean | Primitive;

    const Nil: Cell = { type: "cell", car: null, cdr: null };
    const False: Boolean = { type: "bool", value: false };
    const True: Boolean = { type: "bool", value: true };

    function escapeString(str: string) : string {
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

    export function toString(value: Value | Error): string {
        if (value instanceof Error) {
            return `<error:${value.message}>`;
        }
        if (value === Nil  ) { return '()' };
        if (value === False) { return '#f' };
        if (value === True) { return '#t' };
        switch (value.type) {
            case "symbol": return value.value;
            case "cell": return `${toString(value.car)} ${(value.cdr.type == "cell" && value.cdr !== Nil) ? "" : "." } ${toString(value.cdr)}`;
            case "string": return `"${escapeString(value.value)}"`;
            case "number": return value.value.toString();
            case "bool": return value.value ? '#t' : '#f';
            case "primitive": return `<primitive:${value.value.name}>`;
        }
    }

    function car(x: Value): Value {
        if (ispair(x) == False || isnull(x) === True) { throw new Error(`Error: Attempt to apply car on ${x}`); }
        return (<Cell>x).car;
    }

    function cdr(x: Value): Value {
        if (ispair(x) == False || isnull(x) === True) { throw new Error(`Error: Attempt to apply cdr on ${x}`); }
        return (<Cell>x).cdr;
    }

    function cadr(x) { return car(cdr(x)); }
    function caddr(x) { return car(cdr(cdr(x))); }
    function cadddr(x) { return car(cdr(cdr(cdr(x)))); }
    function cddr(x) { return cdr(cdr(x)); }
    function cdddr(x) { return cdr(cdr(cdr(x))); }

    export function cons(x: Value, y: Value): Cell { return { type: "cell", car: x, cdr: y }; }

    function isnull(x: Value): Boolean { return x === Nil ? True : False; }
    function issymbol(x: Value): Boolean { return x.type == "symbol" ? True : False; }

    export function nil(): Cell { return Nil; }
    export function symbol(x: string): Symbol { return { type: "symbol", value: x }; }
    export function number(x: number): Number { return { type: "number", value: x }; }
    export function string(x: string): String { return { type: "string", value: x }; }
    export function boolean(x: boolean): Boolean { return x ? True : False; }

    function ispair(x: Value): Boolean { return x.type == "cell" ? True : False; }
    export function list(...xs: Value[]): Cell { return xs.reduceRight<Cell>((s, x) => cons(x, s), nil()); }
    function list2(...xs: Value[]): Cell {
        const tail = xs.splice(xs.length - 2, 2);
        return xs.reduceRight<Cell>((s, x) => cons(x, s), cons(tail[0], tail[1]));
    }
    function length(x: Value): number {
        for (let i = 0; ; i++) {
            if (ispair(x) === False) {
                throw new Error();
            }
            if (isnull(x) === True) {
                return i;
            }
            x = cdr(x);
        }
    }
    function list_ref(x: Value, i: number): Value {
        while (i > 0) {
            x = cdr(x);
        }
        return x;
    }
    function primitive(v: any): Primitive {
        return { type: "primitive", value: v };
    }
    function eq(x: Value, y: Value): Boolean {
        if (x.type == y.type) {
            switch (x.type) {
                case "symbol": return (x.value == (<Symbol>y).value) ? True : False;
                case "cell": return (x === y) ? True : False;
                case "number": return (x.value === (<Number>y).value) ? True : False;
                case "string": return (x.value === (<String>y).value) ? True : False;
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
    function is_self_evaluation(expr: Value) {
        return (ispair(expr) === False) && (issymbol(expr) === False);

    }

    // S 式をコンパイルする
    function compile(expr: Value) {
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
            list(symbol("car"), symbol("primitive"), primitive(car)),
            list(symbol("cdr"), symbol("primitive"), primitive(cdr)),
            list(symbol("cons"), symbol("primitive"), primitive(cons)),
            list(symbol("eq?"), symbol("primitive"), primitive(eq)),
            list(symbol("pair?"), symbol("primitive"), primitive(ispair)),
            list(symbol("+"), symbol("primitive"), primitive((xs) => {
                let v = 0;
                while (isnull(xs) === False) {
                    v += (<Number>car(xs)).value;
                    xs = cdr(xs);
                }
                return number(v);
            }))
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
            throw new Error(`unbound variable: ${sym}`);
        }
    }

    // 仮想マシンでコードを実行する
    function vm(s: Value, e: Value, c: Value, d: Value): Value {
        let halt = false;
        setTimeout(x => halt = true, 10000);
        while (!halt) {
            const v = <Symbol>car(c); c = cdr(c);
            switch (v.value) {
                case "ld": {
                    const pos = car(c); c = cdr(c);
                    s = cons(get_lvar(e, (<Number>car(pos)).value, (<Number>cdr(pos)).value), s);
                    e = e;
                    c = cdr(c);
                    d = d;
                    continue;
                }
                case "ldc": {
                    s = cons(car(c), s);
                    e = e;
                    c = cdr(c);
                    d = d;
                    continue;
                }
                case "ldg": {
                    s = cons(get_gvar(car(c)), s);
                    e = e;
                    c = cdr(c);
                    d = d;
                    continue;
                }
                case "ldf": {
                    s = cons(list(symbol("closure"), car(c), e), s);
                    e = e;
                    c = cdr(c);
                    d = d;
                    continue;
                }
                case "app": {
                    const clo = car(s);
                    const lvar = cadr(s);
                    if (eq(car(clo), symbol("primitive")) === True) {
                        const ret = (<Primitive>cadr(clo)).value(lvar);
                        s = cons(ret, cddr(s));
                        e = e;
                        c = c;
                        d = d;
                        continue;
                    } else {
                        s = nil();
                        e = cons(lvar, caddr(clo));
                        c = cadr(clo);
                        d = cons(list(cddr(s), e, c), d);
                        continue;
                    }
                }
                case "rtn": {
                    const save = car(d);
                    s = cons(car(s), cdr(save));
                    e = cadr(save);
                    c = caddr(save);
                    d = cdr(d);
                    continue;
                }
                case "sel": {
                    const t_clause = car(c);
                    const e_clause = cadr(c);
                    const v = (<Boolean>car(s)).value;
                    if (v) {
                        s = cdr(s);
                        e = e;
                        c = t_clause;
                        d = cons(cddr(c), d);
                    } else {
                        s = cdr(s);
                        e = e;
                        c = e_clause;
                        d = cons(cddr(c), d);
                    }
                    continue;
                }
                case "join": {
                    s = s;
                    e = e;
                    c = car(d);
                    d = cdr(d);
                    continue;
                }
                case "pop": {
                    s = cdr(s);
                    e = e;
                    c = c;
                    d = d;
                    continue;
                }
                case "args": {
                    for (let n = (<Number>car(c)).value, a = Nil; ; n--) {
                        if (n == 0) {
                            s = cons(a, s);
                            e = e;
                            c = cdr(c);
                            d = d;
                            break;
                        }
                        a = cons(car(s), a)
                        s = cdr(s);
                    }
                    continue;
                }
                case "def": {
                    const sym = car(c);
                    global_environment = cons(cons(sym, car(s)), global_environment);
                    s = cons(sym, cdr(s));
                    e = e;
                    c = cdr(c);
                    d = d;
                    continue;
                }
                case "stop": {
                    return car(s);
                }
                default:
                    {
                        throw new Error("unknown opcode");
                    }
            }
        }
        throw new Error("timeout");
    }



    export function Run(expr: Value): Value {
        return vm(Nil, Nil, compile(expr), Nil);
    }



    class SyntaxError extends Error {
        constructor(...args) {
            super(...args);
        }
        public line: number;
        public col: number;
    }

    export function parse(stream: string): Value | Error {
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

        function syntax_error(msg: string): SyntaxError {
            const e = new SyntaxError('Syntax error: ' + msg);
            e.line = this._line + 1;
            e.col = this._col + 1;
            return e;
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
                }

                if (next == '"') {
                    consume_char();
                    break;
                }

                if (next == '\\') {
                    consume_char();
                    next = peek_char();

                    if (next == 'r') {
                        consume_char();
                        buf += '\r';
                    } else if (next == 't') {
                        consume_char();
                        buf += '\t';
                    } else if (next == 'n') {
                        consume_char();
                        buf += '\n';
                    } else if (next == 'f') {
                        consume_char();
                        buf += '\f';
                    } else if (next == 'b') {
                        consume_char();
                        buf += '\b';
                    } else {
                        buf += consume_char();
                    }
                }
            }

            // wrap in object to make strings distinct from symbols
            return string(buf);
        }

        function parse_atom(): Value | Error | null{
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

        function parse_quoted(): Cell | Error {
            let q = consume_char();
            let quote = quotes_map[q];

            if (quote == "unquote" && peek_char() == "@") {
                consume_char();
                quote = "unquote-splicing";
                q = ',@';
            }

            // ignore whitespace
            until_char(not_whitespace_or_end);
            const quotedExpr = parse_expr();

            if (quotedExpr instanceof Error) {
                return quotedExpr;
            }

            // nothing came after '
            if (quotedExpr === null) {
                return syntax_error('Unexpected `' + peek_char() + '` after `' + q + '`');
            }

            return list(symbol(quote), quotedExpr);
        }

        function parse_expr(): Value | Error | null {
            // ignore whitespace
            until_char(not_whitespace_or_end);

            if (quotes.test(peek_char())) {
                return parse_quoted();
            }

            const expr = peek_char() == '(' ? parse_list() : parse_atom();
            
            // ignore whitespace
            until_char(not_whitespace_or_end);

            return expr;
        }

        function parse_list() : Cell | Error {
            if (peek_char() != '(') {
                return syntax_error('Expected `(` - saw `' + peek_char() + '` instead.');
            }

            consume_char();

            const ls = [];
            let v = parse_expr();

            if (v instanceof Error) {
                return v;
            }

            if (v !== null) {
                ls.push(v);

                while ((v = parse_expr()) !== null) {
                    if (v instanceof Error) { return v; }
                    ls.push(v);
                }
            }

            if (peek_char() != ')') {
                return syntax_error('Expected `)` - saw: `' + peek_char() + '`');
            }

            // consume that closing paren
            consume_char();

            return ls.reduceRight((s,x) => cons(x,s), nil());
        }

        const expression = parse_expr();

        if (expression instanceof Error) {
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
    const el = document.getElementById('content');
    console.log(Scheme.toString(Scheme.parse("(+ 5 10)")));
    console.log(Scheme.list(Scheme.symbol("+"), Scheme.number(1), Scheme.number(2)));
    console.log(Scheme.Run(Scheme.list(Scheme.symbol("+"), Scheme.number(1), Scheme.number(2))));
};
