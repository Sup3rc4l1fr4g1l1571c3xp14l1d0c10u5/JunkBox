Array.prototype.startsWith = function (a, index) {
    for (var i = 0; i < a.length; i++) {
        if (this[index + i] !== a) {
            return false;
        }
    }
    return true;
};
var Re1;
(function (Re1) {
    "use strict";
    var Regexp = /** @class */ (function () {
        function Regexp(type, left, right) {
            this.type = type;
            this.left = left;
            this.right = right;
            this.groupId = 0;
            this.ch = "";
        }
        Regexp.compile = function (str) {
            var strIndex = 0;
            var nparen = 0;
            return line();
            function line() {
                var parsed_regexp = alt();
                EOL();
                return parsed_regexp;
            }
            function alt() {
                var $1 = concat();
                while (isToken('|')) {
                    token('|');
                    $1 = new Regexp(1 /* Alt */, $1, concat());
                }
                return $1;
            }
            function concat() {
                var $1 = repeat();
                while (isRepeat()) {
                    $1 = new Regexp(2 /* Cat */, $1, repeat());
                }
                return $1;
            }
            function isRepeat() {
                return (['|', '*', '+', '?', ')', ':'].some(function (x) { return isToken(x); }) == false) && (isToken() == true);
            }
            function repeat() {
                var $1 = single();
                if (isToken('*')) {
                    token('*');
                    $1 = new Regexp(3 /* Star */, $1, null);
                }
                else if (isToken('+')) {
                    token('+');
                    $1 = new Regexp(4 /* Plus */, $1, null);
                }
                else if (isToken('?')) {
                    token('?');
                    $1 = new Regexp(5 /* Quest */, $1, null);
                }
                if (isToken('?')) {
                    token('?');
                    $1.groupId = 1;
                }
                return $1;
            }
            function count() {
                return ++nparen;
            }
            function single() {
                if (isToken('(')) {
                    token('(');
                    if (isToken('?')) {
                        token('?');
                        token(':');
                        var $4 = alt();
                        token(')');
                        return $4;
                    }
                    else {
                        var $2 = count();
                        var $3 = alt();
                        token(')');
                        var $$ = new Regexp(6 /* Paren */, $3, null);
                        $$.groupId = $2;
                        return $$;
                    }
                }
                if (isToken('.')) {
                    token('.');
                    return new Regexp(7 /* Dot */, null, null);
                }
                if (isToken('|', '*', '+', '?', '(', ')', ':', '.') == false && isToken() == true) {
                    var $$ = new Regexp(8 /* Lit */, null, null);
                    $$.ch = token();
                    return $$;
                }
                throw new Error();
            }
            function CHAR() {
                if (strIndex == str.length) {
                    throw new Error();
                }
                return token();
            }
            function EOL() {
                if (strIndex != str.length) {
                    throw new Error(strIndex + "/" + str.length);
                }
            }
            function token(tok) {
                if (tok == undefined) {
                    if (strIndex == str.length) {
                        return "";
                    }
                    else {
                        return str[strIndex++];
                    }
                }
                else {
                    if (str.startsWith(tok, strIndex)) {
                        strIndex += tok.length;
                        return tok;
                    }
                    else {
                        throw new Error(tok + " not found in " + str + ", strIndex=" + strIndex);
                    }
                }
            }
            function isToken() {
                var toks = [];
                for (var _i = 0; _i < arguments.length; _i++) {
                    toks[_i] = arguments[_i];
                }
                if (toks.length == 0) {
                    if (strIndex == str.length) {
                        return false;
                    }
                    else {
                        return true;
                    }
                }
                else {
                    for (var _a = 0, toks_1 = toks; _a < toks_1.length; _a++) {
                        var tok = toks_1[_a];
                        if (str.startsWith(tok, strIndex)) {
                            return true;
                        }
                    }
                    return false;
                }
            }
        };
        Regexp.prototype.toString = function () {
            switch (this.type) {
                default: return "???";
                case 1 /* Alt */: return "Alt(" + this.left.toString() + ", " + this.right.toString() + ")";
                case 2 /* Cat */: return "Cat(" + this.left.toString() + ", " + this.right.toString() + ")";
                case 8 /* Lit */: return "Lit(" + this.ch + ")";
                case 7 /* Dot */: return "Dot";
                case 6 /* Paren */: return "Paren(" + this.groupId + ", " + this.left.toString() + ")";
                case 3 /* Star */: return (this.groupId ? "Ng" : "") + "Star(" + this.left.toString() + ")";
                case 4 /* Plus */: return (this.groupId ? "Ng" : "") + "Plus(" + this.left.toString() + ")";
                case 5 /* Quest */: return (this.groupId ? "Ng" : "") + "Quest(" + this.left.toString() + ")";
            }
        };
        return Regexp;
    }());
    var Inst = /** @class */ (function () {
        function Inst(p) {
            this.opcode = (p && p.opcode) ? p.opcode : 0;
            this.ch = (p && p.ch) ? p.ch : "";
            this.saveSlot = (p && p.saveSlot) ? p.saveSlot : 0;
            this.x = (p && p.x) ? p.x : null;
            this.y = (p && p.y) ? p.y : null;
            this.gen = (p && p.gen) ? p.gen : 0;
        }
        return Inst;
    }());
    var Prog = /** @class */ (function () {
        function Prog() {
            this.start = [];
            this.len = 0;
        }
        Prog.compile = function (r) {
            var n = count(r) + 1;
            var p = new Prog();
            var pc = 0;
            p.start[pc++] = new Inst({ opcode: 6 /* Save */, saveSlot: 0 });
            emit(r);
            p.start[pc++] = new Inst({ opcode: 6 /* Save */, saveSlot: 1 });
            p.start[pc++] = new Inst({ opcode: 2 /* Match */ });
            p.len = pc;
            return p;
            function count(r) {
                switch (r.type) {
                    default: throw new Error("bad count");
                    case 1 /* Alt */: return 2 + count(r.left) + count(r.right);
                    case 2 /* Cat */: return count(r.left) + count(r.right);
                    case 8 /* Lit */:
                    case 7 /* Dot */: return 1;
                    case 6 /* Paren */: return 2 + count(r.left);
                    case 5 /* Quest */: return 1 + count(r.left);
                    case 3 /* Star */: return 2 + count(r.left);
                    case 4 /* Plus */: return 1 + count(r.left);
                }
            }
            function emit(r) {
                switch (r.type) {
                    default:
                        throw new Error("bad emit");
                    case 1 /* Alt */: {
                        var p1_1 = pc++;
                        p.start[p1_1] = new Inst({ opcode: 4 /* Split */ });
                        p.start[p1_1].x = pc;
                        emit(r.left);
                        var p2_1 = pc++;
                        p.start[p2_1] = new Inst({ opcode: 3 /* Jmp */ });
                        p.start[p1_1].y = pc;
                        emit(r.right);
                        p.start[p2_1].x = pc;
                        break;
                    }
                    case 2 /* Cat */: {
                        emit(r.left);
                        emit(r.right);
                        break;
                    }
                    case 8 /* Lit */: {
                        p.start[pc++] = new Inst({ opcode: 1 /* Char */, ch: r.ch });
                        break;
                    }
                    case 7 /* Dot */: {
                        p.start[pc++] = new Inst({ opcode: 5 /* Any */ });
                        break;
                    }
                    case 6 /* Paren */: {
                        p.start[pc++] = new Inst({ opcode: 6 /* Save */, saveSlot: 2 * r.groupId + 0 });
                        emit(r.left);
                        p.start[pc++] = new Inst({ opcode: 6 /* Save */, saveSlot: 2 * r.groupId + 1 });
                        break;
                    }
                    case 5 /* Quest */: {
                        var p1_2 = pc++;
                        p.start[p1_2] = new Inst({ opcode: 4 /* Split */ });
                        p.start[p1_2].x = pc;
                        emit(r.left);
                        p.start[p1_2].y = pc;
                        if (r.groupId) { // non-greedy
                            var t = p.start[p1_2].x;
                            p.start[p1_2].x = p.start[p1_2].y;
                            p.start[p1_2].y = t;
                        }
                        break;
                    }
                    case 3 /* Star */: {
                        var p1_3 = pc++;
                        p.start[p1_3] = new Inst({ opcode: 4 /* Split */ });
                        p.start[p1_3].x = pc;
                        emit(r.left);
                        var p2_2 = pc++;
                        p.start[p2_2] = new Inst({ opcode: 3 /* Jmp */ });
                        p.start[p2_2].x = p1_3;
                        p.start[p1_3].y = pc;
                        if (r.groupId) { // non-greedy
                            var t = p.start[p1_3].x;
                            p.start[p1_3].x = p.start[p1_3].y;
                            p.start[p1_3].y = t;
                        }
                        break;
                    }
                    case 4 /* Plus */:
                        var p1 = pc;
                        emit(r.left);
                        var p2 = pc++;
                        p.start[p2] = new Inst({ opcode: 4 /* Split */ });
                        p.start[p2].x = p1;
                        p.start[p2].y = pc;
                        if (r.groupId) { // non-greedy
                            var t = p.start[p2].x;
                            p.start[p2].x = p.start[p2].y;
                            p.start[p2].y = t;
                        }
                        break;
                }
            }
        };
        Prog.prototype.toString = function () {
            function _2d(n) {
                var s = "00" + n.toString();
                return s.substr(s.length - 2, 2);
            }
            var ret = [];
            for (var pc = 0; pc < this.len; pc++) {
                switch (this.start[pc].opcode) {
                    default:
                        throw new Error("printprog");
                    case 4 /* Split */:
                        ret.push(_2d(pc) + ". split " + this.start[pc].x + ", " + this.start[pc].y);
                        break;
                    case 3 /* Jmp */:
                        ret.push(_2d(pc) + ". jmp " + this.start[pc].x);
                        break;
                    case 1 /* Char */:
                        ret.push(_2d(pc) + ". char " + this.start[pc].ch);
                        break;
                    case 5 /* Any */:
                        ret.push(_2d(pc) + ". any");
                        break;
                    case 2 /* Match */:
                        ret.push(_2d(pc) + ". match");
                        break;
                    case 6 /* Save */:
                        ret.push(_2d(pc) + ". save " + this.start[pc].saveSlot);
                }
            }
            return ret.join("\r\n");
        };
        Prog.prototype.pikevm = function (input) {
            var clist = new ThreadList(this.start.length);
            var nlist = new ThreadList(this.start.length);
            var gen = 1;
            var self = this;
            addthread(clist, new Thread(0, null), 0);
            var matched = null;
            for (var sp = 0;; sp++) {
                if (clist.count == 0) {
                    break;
                }
                gen++;
                loop: for (var i = 0; i < clist.count; i++) {
                    var pc = clist.threads[i].pc;
                    var sub = clist.threads[i].sub;
                    switch (this.start[pc].opcode) {
                        case 1 /* Char */:
                            if (input[sp] != this.start[pc].ch) {
                                break;
                            }
                        case 5 /* Any */:
                            if (input.length == sp) {
                                break;
                            }
                            addthread(nlist, new Thread(pc + 1, sub), sp + 1);
                            break;
                        case 2 /* Match */:
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
                var subp = [];
                for (var m = matched; m != null; m = m.next) {
                    if (subp[m.index] == undefined) {
                        subp[m.index] = m.value;
                    }
                }
                var result = [];
                for (var i = 0; i < subp.length; i += 2) {
                    result.push({ start: subp[i + 0], end: subp[i + 1] });
                }
                return result;
            }
            else {
                return null;
            }
            function addthread(threadList, thread, sp) {
                if (self.start[thread.pc].gen == gen) {
                    return; // already on list
                }
                self.start[thread.pc].gen = gen;
                switch (self.start[thread.pc].opcode) {
                    default:
                        threadList.threads[threadList.count] = thread;
                        threadList.count++;
                        break;
                    case 3 /* Jmp */:
                        addthread(threadList, new Thread(self.start[thread.pc].x, thread.sub), sp);
                        break;
                    case 4 /* Split */:
                        addthread(threadList, new Thread(self.start[thread.pc].x, thread.sub), sp);
                        addthread(threadList, new Thread(self.start[thread.pc].y, thread.sub), sp);
                        break;
                    case 6 /* Save */:
                        addthread(threadList, new Thread(thread.pc + 1, new Sub(self.start[thread.pc].saveSlot, sp, thread.sub)), sp);
                        break;
                }
            }
        };
        return Prog;
    }());
    var Sub = /** @class */ (function () {
        function Sub(index, value, next) {
            this.index = index;
            this.value = value;
            this.next = next;
        }
        return Sub;
    }());
    ;
    var Thread = /** @class */ (function () {
        function Thread(pc, sub) {
            this.pc = pc;
            this.sub = sub;
        }
        return Thread;
    }());
    var ThreadList = /** @class */ (function () {
        function ThreadList(capacity) {
            this.threads = [];
            this.count = 0;
            for (var i = 0; i < capacity; i++) {
                this.threads[i] = new Thread(null, null);
            }
        }
        ThreadList.prototype.clear = function () {
            this.count = 0;
        };
        ThreadList.swap = function (x, y) {
            var threads = x.threads;
            var count = x.count;
            x.threads = y.threads;
            x.count = y.count;
            y.threads = threads;
            y.count = count;
        };
        return ThreadList;
    }());
    function test() {
        var testcases = [
            { regexp: "abcdefg", text: "abcdefg", expect: [{ start: 0, end: 7 }] },
            { regexp: "a(bcdef)g", text: "abcdefg", expect: [{ start: 0, end: 7 }, { start: 1, end: 6 }] },
            { regexp: "ab.d.fg", text: "abcdefg", expect: [{ start: 0, end: 7 }] },
            { regexp: "a.+g", text: "abcdefg", expect: [{ start: 0, end: 7 }] },
            { regexp: "ab(.+)fg", text: "abcdefg", expect: [{ start: 0, end: 7 }, { start: 2, end: 5 }] },
            { regexp: "ab((cd)+)", text: "abcdcdcdfg", expect: [{ start: 0, end: 8 }, { start: 2, end: 8 }, { start: 6, end: 8 }] },
            { regexp: "ab((cd)+?)", text: "abcdcdcdfg", expect: [{ start: 0, end: 4 }, { start: 2, end: 4 }, { start: 6, end: 4 }] },
        ];
        for (var _i = 0, testcases_1 = testcases; _i < testcases_1.length; _i++) {
            var _a = testcases_1[_i], regexp = _a.regexp, text = _a.text, expect = _a.expect;
            try {
                var prog = Prog.compile(Regexp.compile(regexp));
                var result = prog.pikevm(text);
                var equal = true;
                if (result.length != expect.length) {
                    equal = false;
                }
                else {
                    for (var i = 0; i < result.length; i++) {
                        if ((result[i].start != expect[i].start) || (result[i].end != expect[i].end)) {
                            equal = false;
                            break;
                        }
                    }
                }
                console.log(equal ? 'success' : 'failed', 'regexp=', regexp, 'text=', text, 'expect=', expect, 'result=', result);
            }
            catch (e) {
                console.log('exception', 'regexp=', regexp, 'text=', text, 'expect=', expect, 'exception=', e);
            }
        }
    }
    window.onload = function () {
        test();
        document.getElementById('check').onclick = function () {
            var regexp = document.getElementById('regexp').value;
            var text = document.getElementById('text').value;
            var dom_result = document.getElementById('result');
            try {
                var prog = Prog.compile(Regexp.compile(regexp));
                dom_result.innerText = "[" + prog.pikevm(text).map(function (x, i) { return i + ": [" + x.start + " ... " + x.end + "]"; }).join('\r\n') + "]";
            }
            catch (e) {
                dom_result.innerText = e.toString();
            }
        };
    };
})(Re1 || (Re1 = {}));
//# sourceMappingURL=app.js.map