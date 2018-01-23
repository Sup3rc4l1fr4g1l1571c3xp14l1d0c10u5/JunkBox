using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Security.Policy;

namespace AnsiCParser {
    public class SyntaxTreeCompileVisitor : SyntaxTreeVisitor.IVisitor<SyntaxTreeCompileVisitor.Value, SyntaxTreeCompileVisitor.Value> {

        /*
         * +------+----------------------+
         * | SP   | 未使用領域           |
         * +------+----------------------+
         * | +n   | ローカル変数領域     |
         * +------+----------------------+
         * | ・・・・・・・・            |
         * +------+----------------------+
         * | -1   | ローカル変数領域     |
         * +------+----------------------+
         * | BP   | 以前のSP             |
         * +------+----------------------+
         * | (※戻り値格納先アドレス)    |
         * +------+----------------------+
         * | +1   | 引数[0]              |
         * +------+----------------------+
         * | ・・・・・・・・            |
         * +------+----------------------+
         * | +n-1 | 引数[n-2]            |
         * +------+----------------------+
         * | +n   | 引数[n-1]            |
         * +------+----------------------+
         * 
         */

        /*
         * Calling Convention: cdecl
         *  - 関数への引数は右から左の順でスタックに積まれる。
         *    - 引数にはベースポインタ相対でアクセスする
         *  - 関数の戻り値は EAXに格納できるサイズならば EAX に格納される。
         *    EAXに格納できないサイズならば、戻り値を格納する領域のアドレスを引数の上に積み、EAXを使わない。（※）
         *    浮動小数点数の場合はFPUスタックのトップに結果をセットする
         *  - 呼び出された側の関数ではEAX, ECX, EDXのレジスタの元の値を保存することなく使用してよい。
         *    呼び出し側の関数では必要ならば呼び出す前にそれらのレジスタをスタック上などに保存する。
         *  - スタックポインタの処理は呼び出し側で行う。
         *  - 引数・戻り値領域の開放は呼び出し側で行う
         *  
         */

        /*
         * コード生成:
         *  - 基本はスタック計算機
         */

        public class Value {
            public enum ValueKind {
                Void,       // 結果はない
                Register,   // 式の結果はレジスタ上の値である（値はレジスタ Register に入っている）
                Temp,       // 式の結果はスタック上の値である（値はスタックの一番上にある）
                IntConst,   // 式の結果は整数定数値である
                FloatConst, // 式の結果は浮動小数点定数値である
                Var,        // 式の結果は変数参照、もしくは引数参照である（アドレス値の示す先が値である）
                Ref,        // 式の結果はオブジェクト参照である(アドレス値自体が値である)
                Address,    // 式の結果はアドレス参照である（スタックの一番上に参照先自体が積まれているような想定。実際には参照先のアドレス値がある）
            }

            // v は (Var v+0) となる
            // &v は (Var v+0)を (Ref v+0)に書き換える
            // &v + 1 は (Ref v+1) となる
            // int *p とした場合の p は (Temp)となる 
            // p[3] は (Temp)となる
            // str[] = {...} とした場合の str は (Ref str+0)となる
            // str[3] = (Ref str+3)となる

            public ValueKind Kind;

            public CType Type;

            // Register
            public string Register;

            // IntConst
            public long IntConst;

            // FloatConst
            public double FloatConst;

            // GlobalVar
            // LocalVar
            // Ref
            public string Label;
            public int Offset;

            // Temp/Address
            public int StackPos;


            public Value() {
            }

            public Value(Value ret) {
                Kind = ret.Kind;
                Type = ret.Type;
                Register = ret.Register;
                IntConst = ret.IntConst;
                FloatConst = ret.FloatConst;
                Label = ret.Label;
                Offset = ret.Offset;
                StackPos = ret.StackPos;
            }

        }

        public class Generator {
            public List<Code> Codes = new List<Code>();
            private readonly Stack<Value> _stack = new Stack<Value>();


            public Code Emit(string body) {
                var code = new Code(body);
                Codes.Add(code);
                return code;
            }


            public void Push(Value v) {
                _stack.Push(v);
            }
            public Value Pop() {
                return _stack.Pop();
            }

            public Value Peek(int i) {
                return _stack.ElementAt(i);
            }


            public void Discard() {
                Value v = Pop();
                if (v.Kind == Value.ValueKind.Temp || v.Kind == Value.ValueKind.Address) {
                    Emit($"addl ${(v.Type.Sizeof() + 3) & ~3}, %esp");  // discard
                }
            }

            public void Dup(int n) {
                var v = Peek(n);
                if (v.Kind == Value.ValueKind.Temp || v.Kind == Value.ValueKind.Address) {
                    int skipsize = 0;
                    for (int i = 0; i < n; i++) {
                        var v2 = Peek(i);
                        if (v2.Kind == Value.ValueKind.Temp) {
                            skipsize += (v2.Type.Sizeof() + 3) & (~3);
                        } else if (v2.Kind == Value.ValueKind.Address) {
                            skipsize += (v2.Type.Sizeof() + 3) & (~3);
                        } else {
                        }
                    }

                    if (v.Kind == Value.ValueKind.Temp) {
                        int size = (v.Type.Sizeof() + 3) & (~3);
                        if (size <= 4) {
                            Emit($"leal ${skipsize}(%esp), %esi");
                            Emit($"push (%esi)");
                        } else {
                            Emit($"leal ${skipsize}(%esp), %esi");
                            Emit($"leal {-size}(%esp), %esp");
                            Emit("movl %esp, %edi");
                            Emit($"movl ${size}, %ecx");
                            Emit("cld");
                            Emit("rep movsb");
                        }
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = v.Type, StackPos = _stack.Count });
                    } else if (v.Kind == Value.ValueKind.Address) {
                        Emit($"leal ${skipsize}(%esp), %esi");
                        Emit("push (%esi)");
                        Push(new Value() { Kind = Value.ValueKind.Address, Type = v.Type, StackPos = _stack.Count });
                    } else {
                        throw new Exception();
                    }
                } else {
                    Push(v);
                }
            }

            public void Add(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    LoadI("%ecx"); // rhs
                    LoadI("%eax"); // lhs
                    Emit("addl %ecx, %eax");
                    Emit("pushl %eax");
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    return;
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();    // rhs
                    LoadF();    // lhs
                    Emit("faddp");
                    StoreF(type);
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    return;
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsIntegerType()) {
                    CType elemType = type.GetBasePointerType();

                    if (rhs.Kind == Value.ValueKind.IntConst && lhs.Kind == Value.ValueKind.Ref) {
                        Pop();
                        Pop();
                        Push(new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = lhs.Label, Offset = (int)(lhs.Offset + rhs.IntConst * elemType.Sizeof()) });
                    } else {
                        LoadI("%ecx");  // rhs = index
                        LoadP("%eax");  // lhs = base
                        Emit($"imull ${elemType.Sizeof()}, %ecx, %ecx"); // index *= sizeof(base[0])
                        Emit("addl %ecx, %eax");                        // base += index
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                    return;
                } else if (lhs.Type.IsIntegerType() && rhs.Type.IsPointerType()) {
                    CType elemType = type.GetBasePointerType();

                    if (lhs.Kind == Value.ValueKind.IntConst && rhs.Kind == Value.ValueKind.Ref) {
                        Pop();
                        Pop();
                        Push(new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = rhs.Label, Offset = (int)(rhs.Offset + lhs.IntConst * elemType.Sizeof()) });
                    } else {
                        LoadI("%eax");  // rhs = base
                        LoadP("%ecx");  // lhs = index
                        Emit($"imull ${elemType.Sizeof()}, %ecx, %ecx"); // index *= sizeof(base[0])
                        Emit("addl %ecx, %eax");                        // base += index
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                    return;
                } else {
                    throw new Exception("");
                }
            }

            public void Sub(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    LoadI("%ecx"); // rhs
                    LoadI("%eax"); // lhs
                    Emit("subl %ecx, %eax");
                    Emit("pushl %eax");
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    return;
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();    // rhs
                    LoadF();    // lhs
                    Emit("fsubp");
                    StoreF(type);
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    return;
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsIntegerType()) {
                    CType elemType = type.GetBasePointerType();

                    if (rhs.Kind == Value.ValueKind.IntConst && lhs.Kind == Value.ValueKind.Ref) {
                        Pop();
                        Pop();
                        Push(new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = lhs.Label, Offset = (int)(lhs.Offset - rhs.IntConst * elemType.Sizeof()) });
                    } else {
                        LoadI("%ecx");  // rhs = index
                        LoadP("%eax");  // lhs = base
                        Emit($"imull ${elemType.Sizeof()}, %ecx, %ecx"); // index *= sizeof(base[0])
                        Emit("subl %ecx, %eax");                        // base += index
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                    return;
                } else if (lhs.Type.IsIntegerType() && rhs.Type.IsPointerType()) {
                    CType elemType = type.GetBasePointerType();

                    if (lhs.Kind == Value.ValueKind.IntConst && rhs.Kind == Value.ValueKind.Ref) {
                        Pop();
                        Pop();
                        Push(new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = rhs.Label, Offset = (int)(rhs.Offset - lhs.IntConst * elemType.Sizeof()) });
                    } else {
                        LoadP("%eax");  // rhs = base
                        LoadI("%ecx");  // lhs = index
                        Emit($"imull ${elemType.Sizeof()}, %ecx, %ecx"); // index *= sizeof(base[0])
                        Emit("subl %ecx, %eax");                        // base += index
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                    return;
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    CType elemType = lhs.Type.GetBasePointerType();

                    if (lhs.Kind == Value.ValueKind.Ref && rhs.Kind == Value.ValueKind.Ref && lhs.Label == rhs.Label) {
                        Pop();
                        Pop();
                        Push(new Value() { Kind = Value.ValueKind.IntConst, Type = CType.CreatePtrDiffT(), IntConst = ((lhs.Offset - rhs.Offset) / elemType.Sizeof()) });
                    } else {
                        LoadP("%ecx");  // rhs = ptr
                        LoadP("%eax");  // lhs = ptr
                        Emit("subl %ecx, %eax");
                        Emit("cltd");
                        Emit("movl %eax, %edx");
                        Emit($"movl ${elemType.Sizeof()}, %ecx");
                        Emit("idivl %ecx");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                    return;
                } else {
                    throw new Exception("");
                }
            }

            public void Mul(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    LoadI("%ecx"); // rhs
                    LoadI("%eax"); // lhs
                    if (type.IsSignedIntegerType()) {
                        Emit("imull %ecx");
                    } else {
                        Emit("mull %ecx");
                    }
                    Emit("push %eax");
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();
                    LoadF();

                    Emit("fmulp");

                    StoreF(type);
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
                    throw new NotImplementedException();
                }
            }

            public void Div(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    LoadI("%ecx"); // rhs
                    LoadI("%eax"); // lhs
                    Emit("cltd");
                    if (type.IsSignedIntegerType()) {
                        Emit("idivl %ecx");
                    } else {
                        Emit("divl %ecx");
                    }
                    Emit("push %eax");
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();
                    LoadF();

                    Emit("fdivp");

                    StoreF(type);
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
                    throw new NotImplementedException();
                }
            }

            public void mod(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    LoadI("%ecx"); // rhs
                    LoadI("%eax"); // lhs
                    Emit("cltd");
                    if (type.IsSignedIntegerType()) {
                        Emit("idivl %ecx");
                    } else {
                        Emit("divl %ecx");
                    }
                    Emit("push %edx");
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
                    throw new NotImplementedException();
                }
            }

            public void And(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    LoadI("%eax"); // rhs
                    LoadI("%ecx"); // lhs
                    Emit("andl %ecx, %eax");
                    Emit("pushl %eax");
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
                    throw new NotImplementedException();
                }

            }

            public void Or(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    LoadI("%eax"); // rhs
                    LoadI("%ecx"); // lhs
                    Emit("orl %ecx, %eax");
                    Emit("pushl %eax");
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
                    throw new NotImplementedException();
                }
            }

            public void Xor(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    LoadI("%eax"); // rhs
                    LoadI("%ecx"); // lhs
                    Emit("xorl %ecx, %eax");
                    Emit("pushl %eax");
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
                    throw new NotImplementedException();
                }

            }
            public void Shl(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    LoadI("%ecx"); // rhs
                    LoadI("%eax"); // lhs
                    if (lhs.Type.IsUnsignedIntegerType()) {
                        Emit("shll %cl, %eax");
                    } else {
                        Emit("sall %cl, %eax");
                    }
                    Emit("pushl %eax");
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
                    throw new NotImplementedException();
                }
            }

            public void Shr(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    LoadI("%ecx"); // rhs
                    LoadI("%eax"); // lhs
                    if (lhs.Type.IsUnsignedIntegerType()) {
                        Emit("shrl %cl, %eax");
                    } else {
                        Emit("sarl %cl, %eax");
                    }
                    Emit("pushl %eax");
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
                    throw new NotImplementedException();
                }
            }

            public void Assign(CType type) {
                var lhs = Peek(0);
                var rhs = Peek(1);

                LoadVA("%eax"); // lhs

                switch (type.Sizeof()) {
                    case 1:
                        rhs = LoadI("%ecx");
                        Emit($"movb %cl, (%eax)");
                        Emit($"pushl (%eax)");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        break;
                    case 2:
                        rhs = LoadI("%ecx");
                        Emit($"movw %cx, (%eax)");
                        Emit($"pushl (%eax)");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        break;
                    case 3:
                    case 4:
                        rhs = LoadI("%ecx");    // ToDo: float のコピーにLoadIを転用しているのを修正
                        Emit($"movl %ecx, (%eax)");
                        Emit($"pushl (%eax)");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        break;
                    default:
                        LoadS(rhs.Type);
                        Emit($"movl %esp, %esi");
                        Emit($"movl ${type.Sizeof()}, %ecx");
                        Emit($"movl %eax, %edi");
                        Emit($"cld");
                        Emit($"rep movsb");
                        //Emit($"addl ${(rhs.Type.Sizeof() + 3) & (~3)}, %esp");
                        Pop();
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        break;
                }

            }

            public void Eq(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);
                if ((lhs.Type.IsIntegerType() || lhs.Type.IsPointerType()) ||
                    (rhs.Type.IsIntegerType() || rhs.Type.IsPointerType())) {
                    LoadI("%eax");
                    LoadI("%ecx");
                    Emit($"cmpl %ecx, %eax");

                    Emit($"sete %al");
                    Emit($"movzbl %al, %eax");
                    Emit($"pushl %eax");

                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();
                    LoadF();

                    Emit($"fcomip");
                    Emit($"fstp %st(0)");

                    Emit($"sete %al");
                    Emit($"movzbl %al, %eax");
                    Emit($"pushl %eax");

                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
                    throw new NotImplementedException();
                }
            }

            public void Ne(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);
                if ((lhs.Type.IsIntegerType() || lhs.Type.IsPointerType()) ||
                    (rhs.Type.IsIntegerType() || rhs.Type.IsPointerType())) {
                    LoadI("%eax");
                    LoadI("%ecx");
                    Emit($"cmpl %ecx, %eax");

                    Emit($"setne %al");
                    Emit($"movzbl %al, %eax");
                    Emit($"pushl %eax");

                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();
                    LoadF();

                    Emit($"fcomip");
                    Emit($"fstp %st(0)");

                    Emit($"setne %al");
                    Emit($"movzbl %al, %eax");
                    Emit($"pushl %eax");

                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
                    throw new NotImplementedException();
                }
            }

            public void Label(string label) {
                Emit($"{label}:");
            }
            public void Jmp(string label) {
                Emit($"jmp {label}");
            }
            public void jmp_false(string label) {
                LoadI("%eax");
                Emit($"cmpl $0, %eax");
                Emit($"je {label}");
            }
            public void jmp_true(string label) {
                LoadI("%eax");
                Emit($"cmpl $0, %eax");
                Emit($"jne {label}");
            }
            public void emit_push_true() {
                Emit($"pushl $1");
            }
            public void emit_push_false() {
                Emit($"pushl $0");
            }

            public void cast_to(CType type) {
                Value ret = Peek(0);
                if (ret.Type.IsIntegerType() && type.IsIntegerType()) {
                    var retty = ret.Type.Unwrap() as CType.BasicType;
                    CType.BasicType.TypeKind selftykind;
                    if (type.IsBasicType()) {
                        selftykind = (type.Unwrap() as CType.BasicType).Kind;
                    } else if (type.IsEnumeratedType()) {
                        selftykind = CType.BasicType.TypeKind.SignedInt;
                    } else {
                        throw new NotImplementedException();
                    }

                    LoadI("%eax");
                    if (ret.Type.IsSignedIntegerType()) {
                        if (selftykind == CType.BasicType.TypeKind.Char || selftykind == CType.BasicType.TypeKind.SignedChar) {
                            Emit($"movsbl %al, %eax");
                        } else if (selftykind == CType.BasicType.TypeKind.SignedShortInt) {
                            Emit($"movswl %ax, %eax");
                        } else if (selftykind == CType.BasicType.TypeKind.SignedInt || selftykind == CType.BasicType.TypeKind.SignedLongInt) {
                            //Emil($"movl %eax, %eax");  // do nothing;
                        } else if (selftykind == CType.BasicType.TypeKind.UnsignedChar) {
                            Emit($"movzbl %al, %eax");
                        } else if (selftykind == CType.BasicType.TypeKind.UnsignedShortInt) {
                            Emit($"movzwl %ax, %eax");
                        } else if (selftykind == CType.BasicType.TypeKind.UnsignedInt || selftykind == CType.BasicType.TypeKind.UnsignedLongInt) {
                            //Emil($"movl %eax, %eax");  // do nothing;
                        } else {
                            throw new NotImplementedException();
                        }
                    } else {
                        if (selftykind == CType.BasicType.TypeKind.Char || selftykind == CType.BasicType.TypeKind.SignedChar) {
                            Emit($"movzbl %al, %eax");
                        } else if (selftykind == CType.BasicType.TypeKind.SignedShortInt) {
                            Emit($"movzwl %ax, %eax");
                        } else if (selftykind == CType.BasicType.TypeKind.SignedInt || selftykind == CType.BasicType.TypeKind.SignedLongInt) {
                            //Emil($"movl %eax, %eax");  // do nothing;
                        } else if (selftykind == CType.BasicType.TypeKind.UnsignedChar) {
                            Emit($"movzbl %al, %eax");
                        } else if (selftykind == CType.BasicType.TypeKind.UnsignedShortInt) {
                            Emit($"movzwl %ax, %eax");
                        } else if (selftykind == CType.BasicType.TypeKind.UnsignedInt || selftykind == CType.BasicType.TypeKind.UnsignedLongInt) {
                            //Emil($"movl %eax, %eax");  // do nothing;
                        } else {
                            throw new NotImplementedException();
                        }
                    }
                    Emit($"pushl %eax");
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if (ret.Type.IsPointerType() && type.IsPointerType()) {
                    Pop();
                    Push(new Value(ret) { Type = type });
                } else if (ret.Type.IsArrayType() && type.IsPointerType()) {
                    Pop();
                    // 手抜き
                    if (ret.Kind == Value.ValueKind.Var) {
                        ret = new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = ret.Label, Offset = ret.Offset };
                        Push(ret);
                    } else if (ret.Kind == Value.ValueKind.Ref) {
                        ret = new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = ret.Label, Offset = ret.Offset };
                        Push(ret);
                    } else if (ret.Kind == Value.ValueKind.Address) {
                        ret = new Value() { Kind = Value.ValueKind.Temp, Type = type };
                        Push(ret);
                    } else {
                        throw new NotImplementedException();
                    }
                } else if (ret.Type.IsArrayType() && type.IsArrayType()) {
                    Pop();
                    Push(new Value(ret) { Type = type });
                } else if (ret.Type.IsPointerType() && type.IsArrayType()) {
                    throw new NotImplementedException();
                } else if (ret.Type.IsIntegerType() && type.IsPointerType()) {
                    Pop();
                    Push(new Value(ret) { Type = type });
                } else if (ret.Type.IsPointerType() && type.IsIntegerType()) {
                    Pop();
                    Push(new Value(ret) { Type = type });
                } else if (ret.Type.IsRealFloatingType() && type.IsRealFloatingType()) {
                    LoadF();
                    StoreF(type);
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if (ret.Type.IsRealFloatingType() && type.IsIntegerType()) {
                    LoadF();

                    // double -> unsigned char
                    // 	movzwl <value>, %eax
                    //  movzbl %al, %eax
                    if (type.IsSignedIntegerType()) {
                        switch (type.Sizeof()) {
                            case 1:
                                Emit($"sub $4, %esp");
                                Emit($"fistps (%esp)");
                                Emit($"movzwl (%esp), %eax");
                                Emit($"movsbl %al, %eax");
                                Emit($"movl %eax, (%esp)");
                                break;
                            case 2:
                                Emit($"sub $4, %esp");
                                Emit($"fistps (%esp)");
                                Emit($"movzwl (%esp), %eax");
                                Emit($"movl %eax, (%esp)");
                                break;
                            case 4:
                                Emit($"sub $4, %esp");
                                Emit($"fistpl (%esp)");
                                Emit($"movl (%esp), %eax");
                                Emit($"movl %eax, (%esp)");
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    } else {
                        switch (type.Sizeof()) {
                            case 1:
                                Emit($"sub $4, %esp");
                                Emit($"fistps (%esp)");
                                Emit($"movzwl (%esp), %eax");
                                Emit($"movzbl %al, %eax");
                                Emit($"movl %eax, (%esp)");
                                break;
                            case 2:
                                Emit($"sub $4, %esp");
                                Emit($"fistpl (%esp)");
                                Emit($"movl (%esp), %eax");
                                Emit($"movzwl %ax, %eax");
                                Emit($"movl %eax, (%esp)");
                                break;
                            case 4:
                                // fistpq  16(%esp)
                                // movl    16(%esp), % eax
                                Emit($"sub $8, %esp");
                                Emit($"fistpq (%esp)");
                                Emit($"movl (%esp), %eax");
                                Emit($"add $4, %esp");
                                Emit($"movl %eax, (%esp)");
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                    }
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if (ret.Type.IsIntegerType() && type.IsRealFloatingType()) {
                    LoadF();
                    StoreF(type);
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });

                } else {
                    throw new NotImplementedException();
                }
            }

            public void arraySubscript(CType type) {
                var index = Peek(0);
                var target = Peek(1);
                LoadI("%ecx");
                if (target.Type.IsPointerType()) {
                    LoadP("%eax");
                } else {
                    LoadVA("%eax");
                }

                Emit($"imull ${type.Sizeof()}, %ecx, %ecx");
                Emit($"leal (%eax, %ecx), %eax");
                Emit($"pushl %eax");

                Push(new Value() { Kind = Value.ValueKind.Address, Type = type, StackPos = _stack.Count });

            }

            public void call(CType type, CType.FunctionType funcType, int argnum, Action<Generator> fun, Action<Generator, int> args) {
                /*
                 *  - 関数への引数は右から左の順でスタックに積まれる。
                 *    - 引数にはベースポインタ相対でアクセスする
                 *  - 関数の戻り値は EAXに格納できるサイズならば EAX に格納される。EAXに格納できないサイズならば、戻り値を格納する領域のアドレスを引数の上に積み、EAXを使わない。（※）
                 *  - 呼び出された側の関数ではEAX, ECX, EDXのレジスタの元の値を保存することなく使用してよい。
                 *    呼び出し側の関数では必要ならば呼び出す前にそれらのレジスタをスタック上などに保存する。
                 *  - スタックポインタの処理は呼び出し側で行う。  
                 */

                int resultSize = 0;
                if (funcType.ResultType.IsVoidType()) {
                    resultSize = 0;
                } else if (funcType.ResultType.Sizeof() > 4) {
                    resultSize = ((funcType.ResultType.Sizeof() + 3) & ~3);
                } else {
                    resultSize = 4;
                }

                // 戻り値格納先
                if (resultSize > 0) {
                    Emit($"subl ${resultSize}, %esp");
                }

                int bakSize = 4 * 3;
                Emit($"pushl %eax");
                Emit($"pushl %ecx");
                Emit($"pushl %edx");

                int argSize = 0;

                // 引数を右側（末尾側）からスタックに積む
                for (int i = argnum - 1; i >= 0; i--) {
                    args(this, i);
                    var ao = Peek(0);
                    LoadS(ao.Type);
                    var _argSize = (ao.Type.Sizeof() + 3) & ~3;
                    argSize += _argSize;
                }

                // 戻り値が浮動小数点数ではなく、eaxにも入らないならスタック上に格納先アドレスを積む
                if (resultSize > 4 && !funcType.ResultType.IsRealFloatingType()) {
                    Emit($"leal {argSize + bakSize}(%esp), %eax");
                    Emit($"push %eax");
                }

                fun(this);
                LoadI("%eax");
                Emit($"call *%eax");

                if (resultSize > 4) {
                    if (funcType.ResultType.IsRealFloatingType()) {
                        // 浮動小数点数はFPUスタック上にある
                        if (funcType.ResultType.IsBasicType(CType.BasicType.TypeKind.Float)) {
                            Emit($"fstps {(argSize + bakSize)}(%esp)");
                        } else if (funcType.ResultType.IsBasicType(CType.BasicType.TypeKind.Double)) {
                            Emit($"fstpl {(argSize + bakSize)}(%esp)");
                        } else {
                            throw new NotImplementedException();
                        }
                        Emit($"addl ${argSize}, %esp");
                    } else {
                        // 戻り値は格納先アドレスに入れられているはず
                        Emit($"addl ${argSize + 4}, %esp");
                    }
                } else if (resultSize > 0) {
                    // 戻り値をコピー(4バイト以下)
                    Emit($"movl %eax, {(argSize + bakSize)}(%esp)");
                    Emit($"addl ${argSize}, %esp");
                } else {
                    // void型？
                    Emit($"addl ${argSize}, %esp");
                }

                Emit($"popl %edx");
                Emit($"popl %ecx");
                Emit($"popl %eax");

                System.Diagnostics.Debug.Assert(_stack.Count >= argnum);
                for (int i = 0; i < argnum; i++) {
                    Pop(); // args
                }

                Push(new Value() { Kind = resultSize == 0 ? Value.ValueKind.Void : Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
            }

            public void mem(CType type, string member) {
                var obj = Peek(0);
                var st = obj.Type.Unwrap() as CType.TaggedType.StructUnionType;
                CType.TaggedType.StructUnionType.MemberInfo target = null;
                int offset = 0;
                foreach (var m in st.Members) {
                    if (m.Ident.Raw == member) {
                        target = m;
                        break;
                    }

                    if (st.Kind == CType.TaggedType.StructUnionType.StructOrUnion.Struct) {
                        offset += m.Type.Sizeof();
                    }
                }

                LoadVA("%eax");
                Emit($"addl ${offset}, %eax");
                Emit($"pushl %eax");
                Push(new Value() { Kind = Value.ValueKind.Address, Type = type, StackPos = _stack.Count });
            }
            public void imem(CType type, string member) {
                var obj = Peek(0);
                var st = obj.Type.GetBasePointerType().Unwrap() as CType.TaggedType.StructUnionType;
                CType.TaggedType.StructUnionType.MemberInfo target = null;
                int offset = 0;
                foreach (var m in st.Members) {
                    if (m.Ident.Raw == member) {
                        target = m;
                        break;
                    }

                    if (st.Kind == CType.TaggedType.StructUnionType.StructOrUnion.Struct) {
                        offset += m.Type.Sizeof();
                    }
                }

                LoadP("%eax");
                Emit($"addl ${offset}, %eax");
                Emit($"pushl %eax");
                Push(new Value() { Kind = Value.ValueKind.Address, Type = type, StackPos = _stack.Count });
            }

            protected string ToByteReg(string s) {
                switch (s) {
                    case "%eax":
                        return "%al";
                    case "%ebx":
                        return "%bl";
                    case "%ecx":
                        return "%cl";
                    case "%edx":
                        return "%dl";
                    default:
                        throw new Exception();
                }
            }
            protected string ToWordReg(string s) {
                switch (s) {
                    case "%eax":
                        return "%ax";
                    case "%ebx":
                        return "%bx";
                    case "%ecx":
                        return "%cx";
                    case "%edx":
                        return "%dx";
                    default:
                        throw new Exception();
                }
            }

            /// <summary>
            /// 整数値もしくはポインタ値を指定したレジスタにロードする。レジスタに入らないサイズはエラーになる
            /// </summary>
            /// <param name="value"></param>
            /// <param name="register"></param>
            /// <returns></returns>
            private Value LoadI(string register) {
                var value = Pop();
                var ValueType = value.Type;
                //System.Diagnostics.Debug.Assert(ValueType.IsIntegerType() || ValueType.IsPointerType() || ValueType.IsArrayType());
                CType elementType;
                switch (value.Kind) {
                    case Value.ValueKind.IntConst: {
                            // 定数値をレジスタにロードする。
                            string op = "";
                            if (ValueType.IsSignedIntegerType() || ValueType.IsBasicType(CType.BasicType.TypeKind.Char)) {
                                switch (ValueType.Sizeof()) {
                                    case 1:
                                        Emit($"movb ${value.IntConst}, {ToByteReg(register)}");
                                        Emit($"movsbl {ToByteReg(register)}, {register}");
                                        break;
                                    case 2:
                                        Emit($"movw ${value.IntConst}, {ToWordReg(register)}");
                                        Emit($"movswl {ToWordReg(register)}, {register}");
                                        break;
                                    case 3:
                                    case 4:
                                        Emit($"movl ${value.IntConst}, {register}");
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            } else {
                                switch (ValueType.Sizeof()) {
                                    case 1:
                                        Emit($"movb ${value.IntConst}, {ToByteReg(register)}");
                                        Emit($"movzbl {ToByteReg(register)}, {register}");
                                        break;
                                    case 2:
                                        Emit($"movw ${value.IntConst}, {ToWordReg(register)}");
                                        Emit($"movzwl {ToWordReg(register)}, {register}");
                                        break;
                                    case 3:
                                    case 4:
                                        Emit($"movl ${value.IntConst}, {register}");
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }
                            return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = register };
                        }
                    case Value.ValueKind.Temp:
                        if (ValueType.Sizeof() <= 4) {
                            // スタックトップの値をレジスタにロード
                            Emit($"popl {register}");
                            return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = register };

                        } else {
                            throw new NotImplementedException();
                            // スタックトップのアドレスをレジスタにロード
                            Emit($"mov %esp, {register}");
                            return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = register };
                        }
                    case Value.ValueKind.FloatConst:
                        throw new NotImplementedException();
                    case Value.ValueKind.Register:
                        throw new NotImplementedException();
                    case Value.ValueKind.Var:
                    case Value.ValueKind.Address: {
                            // 変数値もしくは参照値をレジスタにロード
                            string src = "";
                            switch (value.Kind) {
                                case Value.ValueKind.Var:
                                    if (value.Label == null) {
                                        // ローカル変数のアドレスはebp相対
                                        src = $"{value.Offset}(%ebp)";
                                    } else {
                                        // グローバル変数のアドレスはラベル絶対
                                        src = $"{value.Label}+{value.Offset}";
                                    }
                                    break;
                                case Value.ValueKind.Address:
                                    // アドレス参照のアドレスはスタックトップの値
                                    Emit($"popl {register}");
                                    src = $"({register})";
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }

                            string op = "";
                            if (ValueType.IsSignedIntegerType() || ValueType.IsBasicType(CType.BasicType.TypeKind.Char) || ValueType.IsEnumeratedType()) {
                                switch (ValueType.Sizeof()) {
                                    case 1:
                                        op = "movsbl";
                                        break;
                                    case 2:
                                        op = "movswl";
                                        break;
                                    case 4:
                                        op = "movl";
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            } else if (ValueType.IsUnsignedIntegerType()) {
                                switch (ValueType.Sizeof()) {
                                    case 1:
                                        op = "movzbl";
                                        break;
                                    case 2:
                                        op = "movzwl";
                                        break;
                                    case 4:
                                        op = "movl";
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            } else if (ValueType.IsBasicType(CType.BasicType.TypeKind.Float)) {
                                op = "movl";
                            } else if (ValueType.IsPointerType() || ValueType.IsArrayType()) {
                                op = "movl";
                            } else if (ValueType.IsStructureType()) {
                                op = "leal";
                            } else {
                                throw new NotImplementedException();
                            }
                            Emit($"{op} {src}, {register}");
                            return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = register };
                        }
                        break;
                    case Value.ValueKind.Ref:
                        if (value.Label == null) {
                            Emit($"leal {value.Offset}(%ebp), {register}");
                        } else {
                            Emit($"leal {value.Label}+{value.Offset}, {register}");
                        }
                        return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = register };
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            /// <summary>
            /// FPUスタック上に値をロードする
            /// </summary>
            /// <param name="rhs"></param>
            private void LoadF() {
                var rhs = Pop();
                switch (rhs.Kind) {
                    case Value.ValueKind.IntConst:
                        Emit($"pushl ${rhs.IntConst}");
                        Emit($"fild (%esp)");
                        Emit($"addl $4, %esp");
                        break;
                    case Value.ValueKind.FloatConst:
                        if (rhs.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Float)) {
                            var bytes = BitConverter.GetBytes((float)rhs.FloatConst);
                            var dword = BitConverter.ToUInt32(bytes, 0);
                            Emit($"pushl ${dword}");
                            Emit($"flds (%esp)");
                            Emit($"addl $4, %esp");
                        } else if (rhs.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Double)) {
                            var bytes = BitConverter.GetBytes(rhs.FloatConst);
                            var qwordlo = BitConverter.ToUInt32(bytes, 0);
                            var qwordhi = BitConverter.ToUInt32(bytes, 4);
                            Emit($"pushl ${qwordhi}");
                            Emit($"pushl ${qwordlo}");
                            Emit($"fldl (%esp)");
                            Emit($"addl $8, %esp");
                        } else {
                            throw new NotImplementedException();
                        }
                        break;
                    case Value.ValueKind.Var: {
                            string op = "";
                            if (rhs.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Float)) {
                                op = "flds";
                            } else {
                                op = "fldl";
                            }

                            if (rhs.Label == null) {
                                Emit($"{op} {rhs.Offset}(%ebp)");
                            } else {
                                Emit($"{op} {rhs.Label}+{rhs.Offset}");
                            }
                            break;
                        }
                    case Value.ValueKind.Temp:
                        if (rhs.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Float)) {
                            Emit($"flds (%esp)");
                            Emit($"addl $4, %esp");
                        } else {
                            Emit($"fldl (%esp)");
                            Emit($"addl $8, %esp");
                        }
                        break;
                    case Value.ValueKind.Address:
                        Emit($"pushl %eax");
                        Emit($"leal 4(%esp), %eax");
                        Emit($"movl (%eax), %eax");
                        if (rhs.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Float)) {
                            Emit($"flds (%eax)");
                        } else {
                            Emit($"fldl (%eax)");
                        }
                        Emit($"popl %eax");
                        Emit($"addl $4, %esp");
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            /// <summary>
            /// FPUスタックの一番上の値をポップし、CPUスタックの一番上に積む
            /// </summary>
            /// <param name="ty"></param>
            public void StoreF(CType ty) {
                if (ty.IsBasicType(CType.BasicType.TypeKind.Float)) {
                    Emit($"sub $4, %esp");
                    Emit($"fstps (%esp)");
                } else if (ty.IsBasicType(CType.BasicType.TypeKind.Double)) {
                    Emit($"sub $8, %esp");
                    Emit($"fstpl (%esp)");
                } else {
                    throw new NotImplementedException();
                }

            }

            /// <summary>
            /// ポインタをロード
            /// </summary>
            /// <param name="target"></param>
            /// <param name="reg"></param>
            public void LoadP(string reg) {
                Value target = Pop();
                System.Diagnostics.Debug.Assert(target.Type.IsPointerType());

                switch (target.Kind) {
                    case Value.ValueKind.Var:
                        // ポインタ型の変数 => 変数の値をロード
                        if (target.Label == null) {
                            Emit($"movl {target.Offset}(%ebp), {reg}");
                        } else {
                            Emit($"movl {target.Label}+{target.Offset}, {reg}");
                        }
                        break;
                    case Value.ValueKind.Ref:
                        // ラベル => ラベルのアドレスをロード
                        if (target.Label == null) {
                            Emit($"leal {target.Offset}(%ebp), {reg}");
                        } else {
                            Emit($"leal {target.Label}+{target.Offset}, {reg}");
                        }
                        break;
                    case Value.ValueKind.Address:
                        // 左辺値(LValue)参照 => スタックトップの値が参照先アドレスなので、参照先アドレスが示す値をロード
                        Emit($"popl {reg}");
                        Emit($"movl ({reg}), {reg}");
                        break;
                    case Value.ValueKind.Temp:
                        // テンポラリ値 => スタックトップの値をロード
                        Emit($"popl {reg}");
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            /// <summary>
            /// 左辺値変数のアドレスをロード
            /// </summary>
            /// <param name="lhs"></param>
            /// <param name="reg"></param>
            public void LoadVA(string reg) {
                Value lhs = Pop();
                switch (lhs.Kind) {
                    case Value.ValueKind.Var:
                        if (lhs.Label == null) {
                            Emit($"leal {lhs.Offset}(%ebp), {reg}");
                        } else {
                            Emit($"leal {lhs.Label}+{lhs.Offset}, {reg}");
                        }

                        break;
                    case Value.ValueKind.Ref:
                        if (lhs.Label == null) {
                            Emit($"leal {lhs.Offset}(%ebp), {reg}");
                        } else {
                            Emit($"leal {lhs.Label}+{lhs.Offset}, {reg}");
                        }

                        break;
                    case Value.ValueKind.Address:
                        Emit($"popl {reg}");
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            /// <summary>
            /// 値をスタックトップに積む。もともとスタックトップにある場合は何もしない
            /// </summary>
            /// <param name="value"></param>
            /// <param name="register"></param>
            /// <returns></returns>
            public void LoadS(CType type) {
                Value value = Peek(0);
                var ValueType = value.Type;
                CType elementType;
                switch (value.Kind) {
                    case Value.ValueKind.IntConst: {
                            // 定数値をスタックに積む
                            string op = "";
                            if (ValueType.IsSignedIntegerType() || ValueType.IsBasicType(CType.BasicType.TypeKind.Char)) {
                                switch (ValueType.Sizeof()) {
                                    case 1:
                                        Emit($"pushl ${unchecked((int)(sbyte)value.IntConst)}");
                                        break;
                                    case 2:
                                        Emit($"pushl ${unchecked((int)(short)value.IntConst)}");
                                        break;
                                    case 4:
                                        Emit($"pushl ${unchecked((int)value.IntConst)}");
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            } else {
                                switch (ValueType.Sizeof()) {
                                    case 1:
                                        Emit($"pushl ${unchecked((int)(byte)value.IntConst)}");
                                        break;
                                    case 2:
                                        Emit($"pushl ${unchecked((int)(ushort)value.IntConst)}");
                                        break;
                                    case 4:
                                        Emit($"pushl ${unchecked((int)(uint)value.IntConst)}");
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }
                            Pop();
                            Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        }
                        break;
                    case Value.ValueKind.Temp:
                        break;
                    case Value.ValueKind.FloatConst: {
                            if (value.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Float)) {
                                var bytes = BitConverter.GetBytes((float)value.FloatConst);
                                var dword = BitConverter.ToUInt32(bytes, 0);
                                Emit($"pushl ${dword}");
                            } else if (value.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Double)) {
                                var bytes = BitConverter.GetBytes(value.FloatConst);
                                var qwordlo = BitConverter.ToUInt32(bytes, 0);
                                var qwordhi = BitConverter.ToUInt32(bytes, 4);
                                Emit($"pushl ${qwordhi}");
                                Emit($"pushl ${qwordlo}");
                            } else {
                                throw new NotImplementedException();
                            }
                            Pop();
                            Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        }
                        break;
                    case Value.ValueKind.Register: {
                            // レジスタをスタックへ
                            Emit($"pushl ${value.Register}");
                            Pop();
                            Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        }
                        break;
                    case Value.ValueKind.Var:
                    case Value.ValueKind.Address: {


                            // コピー先アドレスをロード
                            switch (value.Kind) {
                                case Value.ValueKind.Var:
                                    // ラベル参照
                                    if (value.Label == null) {
                                        // ローカル変数のアドレスはebp相対
                                        Emit($"leal {value.Offset}(%ebp), %esi");
                                    } else {
                                        // グローバル変数のアドレスはラベル絶対
                                        Emit($"leal {value.Label}+{value.Offset}, %esi");
                                    }
                                    break;
                                case Value.ValueKind.Address:
                                    // アドレス参照のアドレスはスタックトップの値
                                    Emit($"popl %esi");
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }

                            // コピー先を確保
                            Emit($"subl ${(ValueType.Sizeof() + 3) & (~3)}, %esp");
                            Emit($"movl %esp, %edi");

                            // 転送
                            Emit($"pushl %ecx");
                            Emit($"movl ${ValueType.Sizeof()}, %ecx");
                            Emit($"cld");
                            Emit($"rep movsb");
                            Emit($"popl %ecx");

                            Pop();
                            Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        }
                        break;
                    case Value.ValueKind.Ref: {
                            // ラベル参照
                            if (value.Label == null) {
                                // ローカル変数のアドレスはebp相対
                                Emit($"leal {value.Offset}(%ebp), %esi");
                            } else {
                                // グローバル変数のアドレスはラベル絶対
                                Emit($"leal {value.Label}+{value.Offset}, %esi");
                            }
                            Emit("pushl %esi");
                            Pop();
                            Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            private void postop(CType type, string op) {

                LoadVA("%eax");

                CType baseType;
                int size;
                if (type.IsPointerType(out baseType) && !baseType.IsVoidType()) {
                    size = baseType.Sizeof();
                } else {
                    size = 1;
                }

                // load value
                switch (type.Sizeof()) {
                    case 4:
                        Emit($"movl (%eax), %ecx");
                        Emit($"{op}l ${size}, (%eax)");
                        break;
                    case 2:
                        Emit($"movw (%eax), %cx");
                        if (type.IsSignedIntegerType()) {
                            Emit($"movswl %cx, %ecx");
                        } else {
                            Emit($"movzwl %cx, %ecx");
                        }
                        Emit($"{op}w ${size}, (%eax)");
                        break;
                    case 1:
                        Emit($"movb (%eax), %cl");
                        if (type.IsSignedIntegerType()) {
                            Emit($"movsbl %cl, %ecx");
                        } else {
                            Emit($"movzbl %cl, %ecx");
                        }
                        Emit($"{op}b ${size}, (%eax)");
                        break;
                    default:
                        throw new NotImplementedException();
                }
                Emit($"push %ecx");
                Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
            }
            public void incp(CType type) {
                postop(type, "add");
            }
            public void decp(CType type) {
                postop(type, "sub");
            }

            public void const_addr_offset(CType type, long offset) {
                var ret = Pop();
                switch (ret.Kind) {
                    case Value.ValueKind.Var:
                        Push(new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = ret.Label, Offset = (int)(ret.Offset + offset) });
                        break;
                    case Value.ValueKind.Ref:
                        Push(new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = ret.Label, Offset = (int)(ret.Offset + offset) });
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            public void gt(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if ((lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) || (lhs.Type.IsPointerType() && rhs.Type.IsPointerType())) {
                    LoadI("%ecx");  // rhs
                    LoadI("%eax");  // lhs
                    Emit("cmpl %ecx, %eax");
                    if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                        Emit("seta %al");
                    } else {
                        Emit("setg %al");
                    }
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();
                    LoadF();
                    Emit($"fcomip");
                    Emit($"fstp %st(0)");
                    Emit("seta %al");
                } else {
                    throw new NotImplementedException();
                }
                Emit("movzbl %al, %eax");
                Emit($"pushl %eax");
                Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
            }

            public void lt(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if ((lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) || (lhs.Type.IsPointerType() && rhs.Type.IsPointerType())) {
                    LoadI("%ecx");  // rhs
                    LoadI("%eax");  // lhs
                    Emit("cmpl %ecx, %eax");
                    if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                        Emit("setb %al");
                    } else {
                        Emit("setl %al");
                    }
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();
                    LoadF();
                    Emit($"fcomip");
                    Emit($"fstp %st(0)");
                    Emit("setb %al");
                } else {
                    throw new NotImplementedException();
                }
                Emit("movzbl %al, %eax");
                Emit($"pushl %eax");
                Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
            }

            public void ge(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if ((lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) || (lhs.Type.IsPointerType() && rhs.Type.IsPointerType())) {
                    LoadI("%ecx");  // rhs
                    LoadI("%eax");  // lhs
                    Emit("cmpl %ecx, %eax");
                    if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                        Emit("setae %al");
                    } else {
                        Emit("setge %al");
                    }
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();
                    LoadF();
                    Emit($"fcomip");
                    Emit($"fstp %st(0)");
                    Emit("setae	%al");
                } else {
                    throw new NotImplementedException();
                }
                Emit("movzbl %al, %eax");
                Emit($"pushl %eax");
                Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
            }

            public void le(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if ((lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) || (lhs.Type.IsPointerType() && rhs.Type.IsPointerType())) {
                    LoadI("%ecx");  // rhs
                    LoadI("%eax");  // lhs
                    Emit("cmpl %ecx, %eax");
                    if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                        Emit("setbe %al");
                    } else {
                        Emit("setle %al");
                    }
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();
                    LoadF();
                    Emit($"fcomip");
                    Emit($"fstp %st(0)");
                    Emit("setbe %al");
                } else {
                    throw new NotImplementedException();
                }
                Emit("movzbl %al, %eax");
                Emit($"pushl %eax");
                Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
            }

            public void address(CType type) {
                var operand = Peek(0);

                if (operand.Kind == Value.ValueKind.Var) {
                    if (operand.Label == null) {
                        Emit($"leal {operand.Offset}(%ebp), %eax");
                    } else {
                        Emit($"leal {operand.Label}+{operand.Offset}, %eax");
                    }
                    Emit($"pushl %eax");
                    Pop();
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
                } else if (operand.Kind == Value.ValueKind.Ref) {
                    if (operand.Label == null) {
                        Emit($"leal {operand.Offset}(%ebp), %eax");
                    } else {
                        Emit($"leal {operand.Label}+{operand.Offset}, %eax");
                    }
                    Emit($"pushl %eax");
                    Pop();
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
                } else if (operand.Kind == Value.ValueKind.Address) {
                    Pop();
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
                } else if (operand.Kind == Value.ValueKind.Temp) {
                    // nothing
                } else {
                    throw new NotImplementedException();
                }
            }

            public void umin(CType type) {
                var operand = Peek(0);
                if (operand.Type.IsIntegerType()) {
                    operand = LoadI("%eax");
                    Emit($"negl %eax");
                    Emit($"pushl %eax");
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
                } else if (operand.Type.IsRealFloatingType()) {
                    LoadF();
                    Emit($"fchs");
                    StoreF(type);
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
                } else {
                    throw new NotImplementedException();
                }
            }
            public void uneg(CType type) {
                var operand = Peek(0);
                if (operand.Type.IsIntegerType()) {
                    operand = LoadI("%eax");
                    Emit($"notl %eax");
                    Emit($"pushl %eax");
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
                } else {
                    throw new NotImplementedException();
                }
            }

            public void unot(CType type) {
                var operand = Peek(0);
                LoadI("%eax");
                Emit($"cmpl $0, %eax");
                Emit($"sete %al");
                Emit($"movzbl %al, %eax");
                Emit($"pushl %eax");
                Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
            }

            private void preop(CType type, string op) {
                // load address
                LoadVA("%eax");

                CType baseType;
                int size;
                if (type.IsPointerType(out baseType) && !baseType.IsVoidType()) {
                    size = baseType.Sizeof();
                } else {
                    size = 1;
                }

                // load value
                switch (type.Sizeof()) {
                    case 4:
                        Emit($"movl (%eax), %ecx");
                        Emit($"{op}l ${size}, (%eax)");
                        break;
                    case 2:
                        Emit($"movw (%eax), %cx");
                        if (type.IsSignedIntegerType()) {
                            Emit($"movswl %cx, %ecx");
                        } else {
                            Emit($"movzwl %cx, %ecx");
                        }
                        Emit($"{op}w ${size}, (%eax)");
                        break;
                    case 1:
                        Emit($"movb (%eax), %cl");
                        if (type.IsSignedIntegerType()) {
                            Emit($"movsbl %cl, %ecx");
                        } else {
                            Emit($"movzbl %cl, %ecx");
                        }
                        Emit($"{op}b ${size}, (%eax)");
                        break;
                    default:
                        throw new NotImplementedException();
                }
                Emit($"push (%eax)");
                Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
            }
            public void pinc(CType type) {
                preop(type, "add");
            }
            public void pdec(CType type) {
                preop(type, "sub");
            }

            public void refe(CType type) {
                LoadI("%eax");
                Emit($"pushl %eax");
                Push(new Value() { Kind = Value.ValueKind.Address, Type = type });

            }

            public void ret(CType type) {
                if (type != null) {
                    var value = Peek(0);
                    if (value.Type.IsSignedIntegerType()) {
                        value = LoadI("%ecx");
                        switch (value.Type.Sizeof()) {
                            case 1:
                                Emit($"movsbl %cl, %eax");
                                break;
                            case 2:
                                Emit($"movswl %cx, %eax");
                                break;
                            case 4:
                                Emit($"movl %ecx, %eax");
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    } else if (value.Type.IsUnsignedIntegerType()) {
                        value = LoadI("%ecx");
                        switch (value.Type.Sizeof()) {
                            case 1:
                                Emit($"movzbl %cl, %eax");
                                break;
                            case 2:
                                Emit($"movxwl %cx, %eax");
                                break;
                            case 4:
                                Emit($"movl %ecx, %eax");
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    } else if (value.Type.IsRealFloatingType()) {
                        LoadF();
                    } else {
                        if (value.Type.Sizeof() <= 4) {
                            LoadI("%eax");
                        } else {
                            LoadVA("%esi");
                            Emit($"movl ${value.Type.Sizeof()}, %ecx");
                            Emit($"movl 8(%ebp), %edi");
                            Emit($"cld");
                            Emit($"rep movsb");
                            if (value.Kind == Value.ValueKind.Temp) {
                                Emit($"addl {(value.Type.Sizeof() + 3) & ~3}, %esp");
                            }
                        }

                    }
                }

                Emit($"movl %ebp, %esp");
                Emit($"popl %ebp");
                Emit($"ret");
            }

            public void when(long caseValue, string label) {
                Emit($"cmp ${caseValue}, %eax");
                Emit($"je {label}");
            }

            public void case_of(Action<Generator> p) {
                LoadI("%eax");
                p(this);
            }

            public void data(string key, byte[] value) {
                Emit($".data");
                Emit($"{key}:");
                Emit(".byte " + String.Join(" ,", value.Select(x => x.ToString())));
            }

            public void swap() {

            }
        }


        public Generator g = new Generator();

        Stack<string> ContinueTarget = new Stack<string>();
        Stack<string> BreakTarget = new Stack<string>();
        private Dictionary<string, int> arguments;
        private Dictionary<string, string> genericlabels;

        /// <summary>
        /// 文字列リテラルなどの静的データ
        /// </summary>
        public List<Tuple<string, byte[]>> dataBlock = new List<Tuple<string, byte[]>>();

        int n = 0;

        private string LAlloc() {
            return $".L{n++}";
        }

        public class Code {
            public string Body;

            public Code(string body) {
                Body = body;
            }

            public override string ToString() {
                return Body;
            }
        }

        public Value OnArgumentDeclaration(SyntaxTree.Declaration.ArgumentDeclaration self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnFunctionDeclaration(SyntaxTree.Declaration.FunctionDeclaration self, Value value) {
            if (self.Body != null) {
                // 引数表
                var ft = (self.Type as CType.FunctionType);
                int offset = 8; // prev return position

                // 戻り値領域
                if ((!ft.ResultType.IsVoidType()) && ft.ResultType.Sizeof() > 4 && !ft.ResultType.IsRealFloatingType()) {
                    offset += 4; //(ft.ResultType.Sizeof() + 3) & ~3;
                }
                // 引数（先頭から）
                arguments = new Dictionary<string, int>();
                var vars = new List<string>();
                foreach (var arg in ft.Arguments) {
                    vars.Add($"// <Argument Name=\"{arg.Ident.Raw}\" Offset=\"{offset}\" />");
                    arguments.Add(arg.Ident.Raw, offset);
                    offset += (arg.Type.Sizeof() + 3) & ~3;
                }

                // ラベル
                genericlabels = new Dictionary<string, string>();

                g.Emit($".section .text");
                g.Emit($".globl {self.LinkageObject.LinkageId}");
                g.Emit($"{self.LinkageObject.LinkageId}:");
                vars.ForEach(x => g.Emit(x));
                g.Emit($"pushl %ebp");
                g.Emit($"movl %esp, %ebp");
                var c = g.Emit(".error \"Stack size is need backpatch.\"");
                maxLocalScopeTotalSize = 0;
                self.Body.Accept(this, value);
                c.Body = $"subl ${maxLocalScopeTotalSize}, %esp";
                g.Emit($"movl %ebp, %esp");
                g.Emit($"popl %ebp");
                g.Emit($"ret");
            }
            return value;
        }

        public Value OnTypeDeclaration(SyntaxTree.Declaration.TypeDeclaration self, Value value) {
            // なにもしない
            return value;
        }

        public Value OnVariableDeclaration(SyntaxTree.Declaration.VariableDeclaration self, Value value) {
            // ブロックスコープ変数
            if (self.LinkageObject.Linkage == LinkageKind.NoLinkage && self.StorageClass != StorageClassSpecifier.Static) {
                if (self.Init != null) {
                    Tuple<string, int> offset;
                    if (localScope.TryGetValue(self.Ident, out offset) == false) {
                        throw new Exception("初期化対象変数が見つからない。");
                    }
                    return self.Init.Accept(this, new Value() { Kind = Value.ValueKind.Var, Label = offset.Item1, Offset = offset.Item2, Type = self.Type });
                }
            }
            return value;
        }

        public Value OnAdditiveExpression(SyntaxTree.Expression.AdditiveExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);
            switch (self.Op) {
                case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add:
                    g.Add(self.Type);
                    break;
                case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub:
                    g.Sub(self.Type);
                    break;
            }
            return value;
        }

        public Value OnAndExpression(SyntaxTree.Expression.AndExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);
            g.And(self.Type);
            return value;
        }

        public Value OnCompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression self, Value value) {
            self.Lhs.Accept(this, value);
            g.Dup(0);
            self.Rhs.Accept(this, value);
            switch (self.Op) {
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.ADD_ASSIGN:
                    g.Add(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.SUB_ASSIGN:
                    g.Sub(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MUL_ASSIGN:
                    g.Mul(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.DIV_ASSIGN:
                    g.Div(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MOD_ASSIGN:
                    g.mod(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.AND_ASSIGN:
                    g.And(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.OR_ASSIGN:
                    g.Or(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.XOR_ASSIGN:
                    g.Xor(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.LEFT_ASSIGN:
                    g.Shl(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.RIGHT_ASSIGN:
                    g.Shr(self.Type);
                    break;
                default:
                    throw new Exception("来ないはず");
            }
            g.Dup(1);
            g.Assign(self.Type);
            g.Discard();
            return value;
        }

        public Value OnSimpleAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, Value value) {
            var rhs = self.Rhs.Accept(this, value);
            var lhs = self.Lhs.Accept(this, value);

            g.Assign(self.Type);
            return value;
        }

        public Value OnCastExpression(SyntaxTree.Expression.CastExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            g.cast_to(self.Type);
            return value;
        }

        public Value OnCommaExpression(SyntaxTree.Expression.CommaExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnConditionalExpression(SyntaxTree.Expression.ConditionalExpression self, Value value) {
            var cond = self.CondExpr.Accept(this, value);

            var elseLabel = LAlloc();
            var junctionLabel = LAlloc();

            g.jmp_false(elseLabel);

            var thenRet = self.ThenExpr.Accept(this, value);
            if (self.Type.IsVoidType()) {
                // スタック上の結果を捨てる
                g.Discard();
            }
            else {
                g.LoadS(self.Type);
                g.Pop();
            }

            g.Jmp(junctionLabel);
            g.Label(elseLabel);

            var elseRet = self.ElseExpr.Accept(this, value);
            if (self.Type.IsVoidType()) {
                // スタック上の結果を捨てる
                g.Discard();
            } else {
                g.LoadS(self.Type);
                g.Pop();
            }
            g.Label(junctionLabel);

            if (self.Type.IsVoidType()) {
                g.Push(new Value() { Kind = Value.ValueKind.Void });
            }
            else {
                g.Push(new Value() { Kind = Value.ValueKind.Temp, Type = self.Type });
            }
            return value;
        }

        public Value OnEqualityExpression(SyntaxTree.Expression.EqualityExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);
            switch (self.Op) {
                case SyntaxTree.Expression.EqualityExpression.OperatorKind.Equal:
                    g.Eq(self.Type);
                    break;
                case SyntaxTree.Expression.EqualityExpression.OperatorKind.NotEqual:
                    g.Ne(self.Type);
                    break;
                case SyntaxTree.Expression.EqualityExpression.OperatorKind.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return value;
        }

        public Value OnExclusiveOrExpression(SyntaxTree.Expression.ExclusiveOrExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);
            g.Xor(self.Type);
            return value;
        }

        public Value OnGccStatementExpression(SyntaxTree.Expression.GccStatementExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnInclusiveOrExpression(SyntaxTree.Expression.InclusiveOrExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);
            g.Or(self.Type);
            return value;
        }

        public Value OnIntegerPromotionExpression(SyntaxTree.Expression.IntegerPromotionExpression self, Value value) {
            self.Expr.Accept(this, value);
            g.cast_to(self.Type);
            return value;
        }

        public Value OnLogicalAndExpression(SyntaxTree.Expression.LogicalAndExpression self, Value value) {
            var labelFalse = LAlloc();
            var labelJunction = LAlloc();
            self.Lhs.Accept(this, value);
            g.jmp_false(labelFalse);
            self.Rhs.Accept(this, value);
            g.jmp_false(labelFalse);
            g.emit_push_true();
            g.Jmp(labelJunction);
            g.Label(labelFalse);
            g.emit_push_false();
            g.Label(labelJunction);
            g.Push(new Value() { Kind = Value.ValueKind.Temp, Type = self.Type });
            return value;
        }

        public Value OnLogicalOrExpression(SyntaxTree.Expression.LogicalOrExpression self, Value value) {
            var labelTrue = LAlloc();
            var labelJunction = LAlloc();
            self.Lhs.Accept(this, value);
            g.jmp_true(labelTrue);
            self.Rhs.Accept(this, value);
            g.jmp_true(labelTrue);
            g.emit_push_false();
            g.Jmp(labelJunction);
            g.Label(labelTrue);
            g.emit_push_true();
            g.Label(labelJunction);
            g.Push(new Value() { Kind = Value.ValueKind.Temp, Type = self.Type });
            return value;
        }

        public Value OnMultiplicitiveExpression(SyntaxTree.Expression.MultiplicitiveExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);
            switch (self.Op) {
                case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mul:
                    g.Mul(self.Type);
                    break;
                case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Div:
                    g.Div(self.Type);
                    break;
                case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mod:
                    g.mod(self.Type);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return value;
        }

        public Value OnArraySubscriptingExpression(SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression self, Value value) {
            var target = self.Target.Accept(this, value);
            var index = self.Index.Accept(this, value);
            g.arraySubscript(self.Type);
            return value;
        }

        public Value OnFunctionCallExpression(SyntaxTree.Expression.PostfixExpression.FunctionCallExpression self, Value value) {
            /*
             *  - 関数への引数は右から左の順でスタックに積まれる。
             *    - 引数にはベースポインタ相対でアクセスする
             *  - 関数の戻り値は EAXに格納できるサイズならば EAX に格納される。EAXに格納できないサイズならば、戻り値を格納する領域のアドレスを引数の上に積み、EAXを使わない。（※）
             *  - 呼び出された側の関数ではEAX, ECX, EDXのレジスタの元の値を保存することなく使用してよい。
             *    呼び出し側の関数では必要ならば呼び出す前にそれらのレジスタをスタック上などに保存する。
             *  - スタックポインタの処理は呼び出し側で行う。  
             */
            var funcType = (self.Expr.Type.Unwrap() as CType.PointerType).BaseType.Unwrap() as CType.FunctionType;

            g.call(self.Type, funcType, self.Args.Count, (_g) => { self.Expr.Accept(this, value); }, (_g, i) => { self.Args[i].Accept(this, value); });

            //return new Value() { kIND = self.Type.IsVoidType() ? Value.ValueKind.Void : Value.ValueKind.Temp, Type = self.Type };
            return value;
        }

        public Value OnMemberDirectAccess(SyntaxTree.Expression.PostfixExpression.MemberDirectAccess self, Value value) {
            var ret = self.Expr.Accept(this, value);
            g.mem(self.Type, self.Ident.Raw);
            return value;
        }

        public Value OnMemberIndirectAccess(SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess self, Value value) {
            var ret = self.Expr.Accept(this, value);
            g.imem(self.Type, self.Ident.Raw);
            return value;

        }

        public Value OnUnaryPostfixExpression(SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            switch (self.Op) {
                case SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Dec:
                    g.decp(self.Type);
                    break;
                case SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Inc:
                    g.incp(self.Type);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return value;
        }

        public Value OnCharacterConstant(SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant self, Value value) {
            g.Push(new Value() { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.Value });
            return value;
        }

        public Value OnFloatingConstant(SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant self, Value value) {
            g.Push(new Value() { Kind = Value.ValueKind.FloatConst, Type = self.Type, FloatConst = self.Value });
            return value;
        }

        public Value OnIntegerConstant(SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant self, Value value) {
            g.Push(new Value() { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.Value });
            return value;
        }

        public Value OnEnclosedInParenthesesExpression(SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression self, Value value) {
            return self.ParenthesesExpression.Accept(this, value);
        }

        public Value OnAddressConstantExpression(SyntaxTree.Expression.PrimaryExpression.AddressConstantExpression self, Value value) {
            var ret = self.Identifier.Accept(this, value);
            g.const_addr_offset(self.Type, self.Offset.Value);
            return value;
        }

        public Value OnEnumerationConstant(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, Value value) {
            g.Push(new Value() { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.Info.Value });
            return value;
        }

        public Value OnFunctionExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, Value value) {
            g.Push(new Value() { Kind = Value.ValueKind.Ref, Type = self.Type, Label = self.Decl.LinkageObject.LinkageId, Offset = 0 });
            return value;
        }

        public Value OnUndefinedIdentifierExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnArgumentExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, Value value) {
            g.Push(new Value() { Kind = Value.ValueKind.Var, Type = self.Type, Label = null, Offset = arguments[self.Ident] });
            return value;
        }

        public Value OnVariableExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, Value value) {
            Tuple<string, int> offset;
            if (self.Decl.LinkageObject.Linkage == LinkageKind.NoLinkage) {
                if (localScope.TryGetValue(self.Ident, out offset)) {
                    g.Push(new Value() { Kind = Value.ValueKind.Var, Type = self.Type, Label = offset.Item1, Offset = offset.Item2, });
                } else {
                    throw new Exception("");
                }
            } else {
                g.Push(new Value() { Kind = Value.ValueKind.Var, Type = self.Type, Label = self.Decl.LinkageObject.LinkageId, Offset = 0 });
            }
            return value;
        }

        public Value OnStringExpression(SyntaxTree.Expression.PrimaryExpression.StringExpression self, Value value) {
            int no = dataBlock.Count;
            var label = $"D{no}";
            dataBlock.Add(Tuple.Create(label, self.Value.ToArray()));
            g.Push(new Value() { Kind = Value.ValueKind.Ref, Type = self.Type, Offset = 0, Label = label });
            return value;
        }

        public Value OnRelationalExpression(SyntaxTree.Expression.RelationalExpression self, Value value) {
            var lhs = self.Lhs.Accept(this, value);
            var rhs = self.Rhs.Accept(this, value);
            switch (self.Op) {
                case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterThan:
                    g.gt(self.Type);
                    break;
                case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessThan:
                    g.lt(self.Type);
                    break;
                case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterOrEqual:
                    g.ge(self.Type);
                    break;
                case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessOrEqual:
                    g.le(self.Type);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return value;
        }

        public Value OnShiftExpression(SyntaxTree.Expression.ShiftExpression self, Value value) {
            var lhs = self.Lhs.Accept(this, value);
            var rhs = self.Rhs.Accept(this, value);

            switch (self.Op) {
                case SyntaxTree.Expression.ShiftExpression.OperatorKind.Left:
                    g.Shl(self.Type);
                    break;
                case SyntaxTree.Expression.ShiftExpression.OperatorKind.Right:
                    g.Shr(self.Type);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return value;
        }

        public Value OnSizeofExpression(SyntaxTree.Expression.SizeofExpression self, Value value) {
            // todo: C99可変長配列型
            g.Push(new Value() { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.ExprOperand.Type.Sizeof() });
            return value;
        }

        public Value OnSizeofTypeExpression(SyntaxTree.Expression.SizeofTypeExpression self, Value value) {
            // todo: C99可変長配列型
            g.Push(new Value() { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.TypeOperand.Sizeof() });
            return value;
        }



        public Value OnTypeConversionExpression(SyntaxTree.Expression.TypeConversionExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            g.cast_to(self.Type);
            return value;
        }

        public Value OnUnaryAddressExpression(SyntaxTree.Expression.UnaryAddressExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            g.address(self.Type);
            return value;
        }

        public Value OnUnaryMinusExpression(SyntaxTree.Expression.UnaryMinusExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            g.umin(self.Type);
            return value;
        }

        public Value OnUnaryNegateExpression(SyntaxTree.Expression.UnaryNegateExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            g.uneg(self.Type);
            return value;
        }

        public Value OnUnaryNotExpression(SyntaxTree.Expression.UnaryNotExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            g.unot(self.Type);
            return value;
        }

        public Value OnUnaryPlusExpression(SyntaxTree.Expression.UnaryPlusExpression self, Value value) {
            self.Expr.Accept(this, value);
            return value;
        }

        public Value OnUnaryPrefixExpression(SyntaxTree.Expression.UnaryPrefixExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);

            switch (self.Op) {
                case SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Inc:
                    g.pinc(self.Type);
                    break;
                case SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Dec:
                    g.pdec(self.Type);
                    break;
                default:
                    throw new Exception("来ないはず");
            }

            return value;

        }

        public Value OnUnaryReferenceExpression(SyntaxTree.Expression.UnaryReferenceExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            g.refe(self.Type);
            return value;
        }

        public Value OnComplexInitializer(SyntaxTree.Initializer.ComplexInitializer self, Value value) {
            throw new Exception("来ないはず");
        }

        public Value OnSimpleInitializer(SyntaxTree.Initializer.SimpleInitializer self, Value value) {
            throw new Exception("来ないはず");
        }

        public Value OnSimpleAssignInitializer(SyntaxTree.Initializer.SimpleAssignInitializer self, Value value) {
            self.Expr.Accept(this, value);
            g.Push(value);
            g.Assign(self.Type);
            g.Discard();
            return value;
        }

        public Value OnArrayAssignInitializer(SyntaxTree.Initializer.ArrayAssignInitializer self, Value value) {
            var elementSize = self.Type.BaseType.Sizeof();
            var v = new Value(value) { Type = self.Type.BaseType };
            foreach (var init in self.Inits) {
                init.Accept(this, v);
                switch (v.Kind) {
                    case Value.ValueKind.Var:
                        if (v.Label == null) {
                            v.Offset += elementSize;
                        } else {
                            throw new NotImplementedException();
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnStructUnionAssignInitializer(SyntaxTree.Initializer.StructUnionAssignInitializer self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnBreakStatement(SyntaxTree.Statement.BreakStatement self, Value value) {
            var label = BreakTarget.Peek();
            g.Jmp(label);
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnCaseStatement(SyntaxTree.Statement.CaseStatement self, Value value) {
            var label = _switchLabelTableStack.Peek()[self];
            g.Label(label);
            self.Stmt.Accept(this, value);
            return new Value() { Kind = Value.ValueKind.Void };
        }

        Scope<Tuple<string, int>> localScope = Scope<Tuple<string, int>>.Empty;
        int localScopeTotalSize = 0;
        private int maxLocalScopeTotalSize = 0;

        public Value OnCompoundStatement(SyntaxTree.Statement.CompoundStatement self, Value value) {
            localScope = localScope.Extend();
            var prevLocalScopeSize = localScopeTotalSize;

            foreach (var x in self.Decls.Reverse<SyntaxTree.Declaration>()) {
                if (x.LinkageObject.Linkage == LinkageKind.NoLinkage) {
                    if (x.StorageClass == StorageClassSpecifier.Static) {
                        // static
                        localScope.Add(x.Ident, Tuple.Create(x.LinkageObject.LinkageId, 0));
                    } else {
                        localScopeTotalSize += (x.LinkageObject.Type.Sizeof() + 3) & ~3;
                        localScope.Add(x.Ident, Tuple.Create((string)null, -localScopeTotalSize));
                    }
                } else if (x.LinkageObject.Linkage == LinkageKind.ExternalLinkage) {
                    // externなのでスキップ
                } else {
                    throw new NotImplementedException();
                }
            }

            if (maxLocalScopeTotalSize < localScopeTotalSize) {
                maxLocalScopeTotalSize = localScopeTotalSize;
            }

            foreach (var x in self.Decls) {
                x.Accept(this, value);
            }
            foreach (var x in self.Stmts) {
                x.Accept(this, value);
            }

            localScopeTotalSize = prevLocalScopeSize;
            localScope = localScope.Parent;
            return value;
        }

        public Value OnContinueStatement(SyntaxTree.Statement.ContinueStatement self, Value value) {
            var label = ContinueTarget.Peek();
            g.Jmp(label);
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnDefaultStatement(SyntaxTree.Statement.DefaultStatement self, Value value) {
            var label = _switchLabelTableStack.Peek()[self];
            g.Label(label);
            self.Stmt.Accept(this, value);
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnDoWhileStatement(SyntaxTree.Statement.DoWhileStatement self, Value value) {
            var labelContinue = LAlloc();
            var labelBreak = LAlloc();

            // Check Loop Condition
            g.Label(labelContinue);
            ContinueTarget.Push(labelContinue);
            BreakTarget.Push(labelBreak);
            var stmt = self.Stmt.Accept(this, value);
            ContinueTarget.Pop();
            BreakTarget.Pop();

            var ret = self.Cond.Accept(this, value);
            g.jmp_true(labelContinue);
            g.Label(labelBreak);
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnEmptyStatement(SyntaxTree.Statement.EmptyStatement self, Value value) {
            return new Value() { Kind = Value.ValueKind.Void };
            //throw new NotImplementedException();
        }

        public Value OnExpressionStatement(SyntaxTree.Statement.ExpressionStatement self, Value value) {
            var ret = self.Expr.Accept(this, value);
            g.Discard();
            return value;
        }

        public Value OnForStatement(SyntaxTree.Statement.ForStatement self, Value value) {
            // Initialize
            if (self.Init != null) {
                var ret = self.Init.Accept(this, value);
                g.Discard();
            }

            var labelHead = LAlloc();
            var labelContinue = LAlloc();
            var labelBreak = LAlloc();

            // Check Loop Condition
            g.Label(labelHead);
            if (self.Cond != null) {
                var ret = self.Cond.Accept(this, value);
                g.jmp_false(labelBreak);
            }

            ContinueTarget.Push(labelContinue);
            BreakTarget.Push(labelBreak);
            var stmt = self.Stmt.Accept(this, value);
            ContinueTarget.Pop();
            BreakTarget.Pop();

            g.Label(labelContinue);
            if (self.Update != null) {
                var ret = self.Update.Accept(this, value);
                g.Discard();
            }

            g.Jmp(labelHead);
            g.Label(labelBreak);

            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnGenericLabeledStatement(SyntaxTree.Statement.GenericLabeledStatement self, Value value) {
            if (genericlabels.ContainsKey(self.Ident) == false) {
                genericlabels[self.Ident] = LAlloc();
            }
            g.Label(genericlabels[self.Ident]);
            self.Stmt.Accept(this, value);
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnGotoStatement(SyntaxTree.Statement.GotoStatement self, Value value) {
            if (genericlabels.ContainsKey(self.Label) == false) {
                genericlabels[self.Label] = LAlloc();
            }
            g.Jmp(genericlabels[self.Label]);
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnIfStatement(SyntaxTree.Statement.IfStatement self, Value value) {
            var cond = self.Cond.Accept(this, value);
            if (self.ElseStmt != null) {
                var elseLabel = LAlloc();
                var junctionLabel = LAlloc();

                g.jmp_false(elseLabel);

                var thenRet = self.ThenStmt.Accept(this, value);
                g.Jmp(junctionLabel);
                g.Label(elseLabel);
                var elseRet = self.ElseStmt.Accept(this, value);
                g.Label(junctionLabel);
            } else {
                var junctionLabel = LAlloc();

                g.jmp_false(junctionLabel);

                var thenRet = self.ThenStmt.Accept(this, value);
                g.Label(junctionLabel);
            }


            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnReturnStatement(SyntaxTree.Statement.ReturnStatement self, Value value) {
            if (self.Expr != null) {
                value = self.Expr.Accept(this, value);
            }
            g.ret(self.Expr?.Type);

            return new Value() { Kind = Value.ValueKind.Void };
        }

        private Stack<Dictionary<SyntaxTree.Statement, string>> _switchLabelTableStack = new Stack<Dictionary<SyntaxTree.Statement, string>>();

        public Value OnSwitchStatement(SyntaxTree.Statement.SwitchStatement self, Value value) {
            var labelBreak = LAlloc();

            var ret = self.Cond.Accept(this, value);

            var labelDic = new Dictionary<SyntaxTree.Statement, string>();
            g.case_of((_g) => {
                foreach (var caseLabel in self.CaseLabels) {
                    var caseValue = caseLabel.Value;
                    var label = LAlloc();
                    labelDic.Add(caseLabel, label);
                    g.when(caseValue, label);
                }
            });
            if (self.DefaultLabel != null) {
                var label = LAlloc();
                labelDic.Add(self.DefaultLabel, label);
                g.Jmp(label);
            } else {
                g.Jmp(labelBreak);
            }

            _switchLabelTableStack.Push(labelDic);
            BreakTarget.Push(labelBreak);
            var stmt = self.Stmt.Accept(this, value);
            BreakTarget.Pop();
            _switchLabelTableStack.Pop();
            g.Label(labelBreak);

            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnWhileStatement(SyntaxTree.Statement.WhileStatement self, Value value) {
            var labelContinue = LAlloc();
            var labelBreak = LAlloc();

            // Check Loop Condition
            g.Label(labelContinue);
            var ret = self.Cond.Accept(this, value);
            g.jmp_false(labelBreak);
            ContinueTarget.Push(labelContinue);
            BreakTarget.Push(labelBreak);
            var stmt = self.Stmt.Accept(this, value);
            ContinueTarget.Pop();
            BreakTarget.Pop();

            g.Jmp(labelContinue);
            g.Label(labelBreak);

            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnTranslationUnit(SyntaxTree.TranslationUnit self, Value value) {
            foreach (var obj in self.LinkageTable) {
                if (obj.Definition is SyntaxTree.Declaration.VariableDeclaration) {
                    var visitor = new FileScopeInitializerVisitor(this);
                    obj.Definition?.Accept(visitor, value);
                }
            }
            foreach (var obj in self.LinkageTable) {
                if (!(obj.Definition is SyntaxTree.Declaration.VariableDeclaration)) {
                    obj.Definition?.Accept(this, value);
                }
            }

            foreach (var data in dataBlock) {
                g.data(data.Item1, data.Item2);
            }

            return value;
        }

        public void WriteCode(StreamWriter writer) {
            g.Codes.ForEach(x => writer.WriteLine(x.ToString()));
        }
    }

    public class FileScopeInitializerVisitor : SyntaxTreeVisitor.IVisitor<SyntaxTreeCompileVisitor.Value, SyntaxTreeCompileVisitor.Value> {
        private SyntaxTreeCompileVisitor syntaxTreeCompileVisitor;
        public void Emit(string s) {
            syntaxTreeCompileVisitor.g.Emit(s);
        }
        public List<SyntaxTree.Expression> Values { get; } = new List<SyntaxTree.Expression>();

        public FileScopeInitializerVisitor(SyntaxTreeCompileVisitor syntaxTreeCompileVisitor) {
            this.syntaxTreeCompileVisitor = syntaxTreeCompileVisitor;
        }

        public SyntaxTreeCompileVisitor.Value OnArgumentDeclaration(SyntaxTree.Declaration.ArgumentDeclaration self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnFunctionDeclaration(SyntaxTree.Declaration.FunctionDeclaration self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnTypeDeclaration(SyntaxTree.Declaration.TypeDeclaration self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnVariableDeclaration(SyntaxTree.Declaration.VariableDeclaration self, SyntaxTreeCompileVisitor.Value value) {
            // ファイルスコープ変数
            if (self.Init != null) {
                Emit($".section .data");
                self.Init.Accept(this, value);
                Emit($".align 4");
                Emit($"{self.LinkageObject.LinkageId}:");
                foreach (var val in this.Values) {
                    var v = val.Accept(this, value);
                    switch (v.Kind) {
                        case SyntaxTreeCompileVisitor.Value.ValueKind.IntConst:
                            switch (v.Type.Sizeof()) {
                                case 1:
                                    Emit($".byte {(byte)v.IntConst}");
                                    break;
                                case 2:
                                    Emit($".word {(ushort)v.IntConst}");
                                    break;
                                case 4:
                                    Emit($".long {(uint)v.IntConst}");
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        case SyntaxTreeCompileVisitor.Value.ValueKind.FloatConst:
                            throw new NotImplementedException();
                            break;
                        case SyntaxTreeCompileVisitor.Value.ValueKind.Var:
                            if (v.Label == null) {
                                throw new Exception("ファイルスコープオブジェクトの参照では無い。");
                            }
                            Emit($".long {v.Label}+{v.Offset}");
                            break;
                            break;
                        case SyntaxTreeCompileVisitor.Value.ValueKind.Ref:
                            if (v.Label == null) {
                                throw new Exception("ファイルスコープオブジェクトの参照では無い。");
                            }
                            Emit($".long {v.Label}+{v.Offset}");
                            break;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            } else {
                Emit($".section .bss");
                Emit($".align 4");
                Emit($".comm {self.LinkageObject.LinkageId}, {self.LinkageObject.Type.Sizeof()}");
            }
            return value;
        }

        public SyntaxTreeCompileVisitor.Value OnAdditiveExpression(SyntaxTree.Expression.AdditiveExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnAndExpression(SyntaxTree.Expression.AndExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnCompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnSimpleAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnCastExpression(SyntaxTree.Expression.CastExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnCommaExpression(SyntaxTree.Expression.CommaExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnConditionalExpression(SyntaxTree.Expression.ConditionalExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnEqualityExpression(SyntaxTree.Expression.EqualityExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnExclusiveOrExpression(SyntaxTree.Expression.ExclusiveOrExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnGccStatementExpression(SyntaxTree.Expression.GccStatementExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnInclusiveOrExpression(SyntaxTree.Expression.InclusiveOrExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnIntegerPromotionExpression(SyntaxTree.Expression.IntegerPromotionExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnLogicalAndExpression(SyntaxTree.Expression.LogicalAndExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnLogicalOrExpression(SyntaxTree.Expression.LogicalOrExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnMultiplicitiveExpression(SyntaxTree.Expression.MultiplicitiveExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnArraySubscriptingExpression(SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnFunctionCallExpression(SyntaxTree.Expression.PostfixExpression.FunctionCallExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnMemberDirectAccess(SyntaxTree.Expression.PostfixExpression.MemberDirectAccess self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnMemberIndirectAccess(SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnUnaryPostfixExpression(SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnCharacterConstant(SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnFloatingConstant(SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnIntegerConstant(SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant self, SyntaxTreeCompileVisitor.Value value) {
            return new SyntaxTreeCompileVisitor.Value() { Kind = SyntaxTreeCompileVisitor.Value.ValueKind.IntConst, IntConst = self.Value, Type = self.Type };
        }

        public SyntaxTreeCompileVisitor.Value OnEnclosedInParenthesesExpression(SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnAddressConstantExpression(SyntaxTree.Expression.PrimaryExpression.AddressConstantExpression self, SyntaxTreeCompileVisitor.Value value) {
            if (self.Identifier is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression) {
                var f = self.Identifier as SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression;
                return new SyntaxTreeCompileVisitor.Value() { Kind = SyntaxTreeCompileVisitor.Value.ValueKind.Ref, Label = f.Decl.LinkageObject.LinkageId, Offset = (int)self.Offset.Value, Type = self.Type };
            } else if (self.Identifier is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression) {
                var f = self.Identifier as SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression;
                return new SyntaxTreeCompileVisitor.Value() { Kind = SyntaxTreeCompileVisitor.Value.ValueKind.Ref, Label = f.Decl.LinkageObject.LinkageId, Offset = (int)self.Offset.Value, Type = self.Type };
            }
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnEnumerationConstant(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnFunctionExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnUndefinedIdentifierExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnArgumentExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnVariableExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnStringExpression(SyntaxTree.Expression.PrimaryExpression.StringExpression self, SyntaxTreeCompileVisitor.Value value) {
            int no = syntaxTreeCompileVisitor.dataBlock.Count;
            var label = $"D{no}";
            syntaxTreeCompileVisitor.dataBlock.Add(Tuple.Create(label, self.Value.ToArray()));
            return new SyntaxTreeCompileVisitor.Value() { Kind = SyntaxTreeCompileVisitor.Value.ValueKind.Ref, Label = label, Offset = 0, Type = self.Type };
        }

        public SyntaxTreeCompileVisitor.Value OnRelationalExpression(SyntaxTree.Expression.RelationalExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnShiftExpression(SyntaxTree.Expression.ShiftExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnSizeofExpression(SyntaxTree.Expression.SizeofExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnSizeofTypeExpression(SyntaxTree.Expression.SizeofTypeExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnTypeConversionExpression(SyntaxTree.Expression.TypeConversionExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnUnaryAddressExpression(SyntaxTree.Expression.UnaryAddressExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnUnaryMinusExpression(SyntaxTree.Expression.UnaryMinusExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnUnaryNegateExpression(SyntaxTree.Expression.UnaryNegateExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnUnaryNotExpression(SyntaxTree.Expression.UnaryNotExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnUnaryPlusExpression(SyntaxTree.Expression.UnaryPlusExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnUnaryPrefixExpression(SyntaxTree.Expression.UnaryPrefixExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnUnaryReferenceExpression(SyntaxTree.Expression.UnaryReferenceExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnComplexInitializer(SyntaxTree.Initializer.ComplexInitializer self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnSimpleInitializer(SyntaxTree.Initializer.SimpleInitializer self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnSimpleAssignInitializer(SyntaxTree.Initializer.SimpleAssignInitializer self, SyntaxTreeCompileVisitor.Value value) {
            var ret = Evaluator.ConstantEval(self.Expr);
            Values.Add(ret);
            return value;
        }

        public SyntaxTreeCompileVisitor.Value OnArrayAssignInitializer(SyntaxTree.Initializer.ArrayAssignInitializer self, SyntaxTreeCompileVisitor.Value value) {
            foreach (var s in self.Inits) {
                s.Accept(this, value);
            }

            var arrayType = self.Type.Unwrap() as CType.ArrayType;
            var filledSize = (arrayType.Length - self.Inits.Count) * arrayType.BaseType.Sizeof();
            while (filledSize > 0) {
                if (filledSize >= 4) {
                    filledSize -= 4;
                    Values.Add(new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, CType.BasicType.TypeKind.UnsignedLongInt));
                } else if (filledSize >= 2) {
                    filledSize -= 2;
                    Values.Add(new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, CType.BasicType.TypeKind.UnsignedShortInt));
                } else if (filledSize >= 1) {
                    filledSize -= 1;
                    Values.Add(new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, CType.BasicType.TypeKind.UnsignedChar));
                }
            }

            return value;
        }

        public SyntaxTreeCompileVisitor.Value OnStructUnionAssignInitializer(SyntaxTree.Initializer.StructUnionAssignInitializer self, SyntaxTreeCompileVisitor.Value value) {
            foreach (var s in self.Inits) {
                s.Accept(this, value);
            }
            var suType = self.Type.Unwrap() as CType.TaggedType.StructUnionType;
            if (suType.IsStructureType()) {

                var filledSize = suType.Members.Skip(self.Inits.Count).Aggregate(0, (s,x) => s + x.Type.Sizeof());
                while (filledSize > 0) {
                    if (filledSize >= 4) {
                        filledSize -= 4;
                        Values.Add(new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, CType.BasicType.TypeKind.UnsignedLongInt));
                    } else if (filledSize >= 2) {
                        filledSize -= 2;
                        Values.Add(new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, CType.BasicType.TypeKind.UnsignedShortInt));
                    } else if (filledSize >= 1) {
                        filledSize -= 1;
                        Values.Add(new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, CType.BasicType.TypeKind.UnsignedChar));
                    }
                }
            }

            return value;
        }

        public SyntaxTreeCompileVisitor.Value OnBreakStatement(SyntaxTree.Statement.BreakStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnCaseStatement(SyntaxTree.Statement.CaseStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnCompoundStatement(SyntaxTree.Statement.CompoundStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnContinueStatement(SyntaxTree.Statement.ContinueStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnDefaultStatement(SyntaxTree.Statement.DefaultStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnDoWhileStatement(SyntaxTree.Statement.DoWhileStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnEmptyStatement(SyntaxTree.Statement.EmptyStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnExpressionStatement(SyntaxTree.Statement.ExpressionStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnForStatement(SyntaxTree.Statement.ForStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnGenericLabeledStatement(SyntaxTree.Statement.GenericLabeledStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnGotoStatement(SyntaxTree.Statement.GotoStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnIfStatement(SyntaxTree.Statement.IfStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnReturnStatement(SyntaxTree.Statement.ReturnStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnSwitchStatement(SyntaxTree.Statement.SwitchStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnWhileStatement(SyntaxTree.Statement.WhileStatement self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnTranslationUnit(SyntaxTree.TranslationUnit self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }
    }

}


/*
signed long long +/- signed long long
	movl	yl, %eax
	movl	yh, %edx
	movl	xl, %ecx
	movl	xh, %ebx
	
    // add
    addl	%ecx, %eax
	adcl	%ebx, %edx
	
    // sub
    subl	%ecx, %eax
	sbbl	%ebx, %edx

    // mul 筆算のアルゴリズム
	movl	xh, %eax
	imull	yl, %edx		// a:b = yl * xh
	movl	yh, %eax
	imull	xl, %eax		// c:d = yh * xl
	leal	(%edx,%eax), %ecx	// e = b + d
	movl	16(%esp), %eax		// 
	mull	24(%esp)			// f:g = xl * yl
	addl	%edx, %ecx			// e:0 + f:g
	movl	%ecx, %edx
	movl	%eax, 8(%esp)       // zl = g
	movl	%edx, 12(%esp)      // zh = f + e


	movl	%eax, zl
	movl	%edx, zh

    
     
     */
