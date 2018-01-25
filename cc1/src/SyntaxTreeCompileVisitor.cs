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


            private int n = 0;

            public string LAlloc() {
                return $".L{n++}";
            }

            public static int Align(int x) {
                return (x + 3) & (~3);
            }

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
                    Emit($"addl ${Align(v.Type.Sizeof())}, %esp");  // discard
                }
            }

            public void Dup(int n) {
                var v = Peek(n);
                if (v.Kind == Value.ValueKind.Temp || v.Kind == Value.ValueKind.Address) {
                    int skipsize = 0;
                    for (int i = 0; i < n; i++) {
                        var v2 = Peek(i);
                        if (v2.Kind == Value.ValueKind.Temp || v2.Kind == Value.ValueKind.Address) {
                            skipsize += Align(v2.Type.Sizeof());
                        }
                    }

                    if (v.Kind == Value.ValueKind.Temp) {
                        int size = Align(v.Type.Sizeof());
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
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx");    // rhs
                        LoadI64("%ecx", "%edx");    // lhs
                        Emit("addl %eax, %ecx");
                        Emit("adcl %ebx, %edx");
                        Emit("pushl %edx");
                        Emit("pushl %ecx");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        Emit("addl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
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
                        if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                            LoadI64("%ecx", "%edx");    // rhs(loのみ使う)
                        } else {
                            LoadI32("%ecx");  // rhs = index
                        }
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
                        LoadP("%ecx");    // rhs = base
                        if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                            LoadI64("%eax", "%edx");    // lhs(loのみ使う)
                        } else {
                            LoadI32("%eax");  // lhs = index
                        }
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
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx");    // rhs
                        LoadI64("%ecx", "%edx");    // lhs
                        Emit("subl %eax, %ecx");
                        Emit("sbbl %ebx, %edx");
                        Emit("pushl %edx");
                        Emit("pushl %ecx");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        Emit("subl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
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
                        if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                            LoadI64("%ecx", "%edx");    // rhs(loのみ使う)
                        } else {
                            LoadI32("%ecx");  // rhs = index
                        }
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
                        LoadP("%ecx");    // rhs = base
                        if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                            LoadI64("%eax", "%edx");    // lhs(loのみ使う)
                        } else {
                            LoadI32("%eax");  // lhs = index
                        }
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
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx");    // rhs
                        LoadI64("%ecx", "%edx");    // lhs
                        Emit("pushl %edx"); // 12(%esp) : lhs.hi
                        Emit("pushl %ecx"); //  8(%esp) : lhs.lo
                        Emit("pushl %ebx"); //  4(%esp) : rhs.hi
                        Emit("pushl %eax"); //  0(%esp) : rhs.lo

                        Emit("movl 4(%esp), %eax");     // rhs.hi
                        Emit("movl %eax, %ecx");
                        Emit("imull 8(%esp), %ecx");    // lhs.lo
                        Emit("movl 12(%esp), %eax");    // lhs.hi
                        Emit("imull 0(%esp), %eax");    // rhs.lo
                        Emit("addl %eax, %ecx");
                        Emit("movl 8(%esp), %eax");     // lhs.lo
                        Emit("mull 0(%esp)");           // rhs.lo
                        Emit("addl %edx, %ecx");
                        Emit("movl %ecx, %edx");

                        Emit("addl $16, %esp");
                        Emit("pushl %edx");
                        Emit("pushl %eax");

                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        if (type.IsSignedIntegerType()) {
                            Emit("imull %ecx");
                        } else {
                            Emit("mull %ecx");
                        }
                        Emit("push %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }

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
                    if (type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt)) {
                        LoadI64("%eax", "%ebx");    // rhs
                        LoadI64("%ecx", "%edx");    // lhs
                        Emit("pushl %ebx"); // 12(%esp) : rhs.hi
                        Emit("pushl %eax"); //  8(%esp) : rhs.lo
                        Emit("pushl %edx"); //  4(%esp) : lhs.hi
                        Emit("pushl %ecx"); //  0(%esp) : lhs.lo
                        Emit("call ___divdi3");
                        Emit("addl $16, %esp");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else if (type.IsBasicType(CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx");    // rhs
                        LoadI64("%ecx", "%edx");    // lhs
                        Emit("pushl %ebx"); // 12(%esp) : rhs.hi
                        Emit("pushl %eax"); //  8(%esp) : rhs.lo
                        Emit("pushl %edx"); //  4(%esp) : lhs.hi
                        Emit("pushl %ecx"); //  0(%esp) : lhs.lo
                        Emit("call ___udivdi3");
                        Emit("addl $16, %esp");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        Emit("cltd");
                        if (type.IsSignedIntegerType()) {
                            Emit("idivl %ecx");
                        } else {
                            Emit("divl %ecx");
                        }
                        Emit("push %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
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
                    if (type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt)) {
                        LoadI64("%eax", "%ebx");    // rhs
                        LoadI64("%ecx", "%edx");    // lhs
                        Emit("pushl %ebx"); // 12(%esp) : rhs.hi
                        Emit("pushl %eax"); //  8(%esp) : rhs.lo
                        Emit("pushl %edx"); //  4(%esp) : lhs.hi
                        Emit("pushl %ecx"); //  0(%esp) : lhs.lo
                        Emit("call ___moddi3");
                        Emit("addl $16, %esp");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else if (type.IsBasicType(CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx");    // rhs
                        LoadI64("%ecx", "%edx");    // lhs
                        Emit("pushl %ebx"); // 12(%esp) : rhs.hi
                        Emit("pushl %eax"); //  8(%esp) : rhs.lo
                        Emit("pushl %edx"); //  4(%esp) : lhs.hi
                        Emit("pushl %ecx"); //  0(%esp) : lhs.lo
                        Emit("call ___umoddi3");
                        Emit("addl $16, %esp");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        Emit("cltd");
                        if (type.IsSignedIntegerType()) {
                            Emit("idivl %ecx");
                        } else {
                            Emit("divl %ecx");
                        }
                        Emit("push %edx");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else {
                    throw new NotImplementedException();
                }
            }

            public void And(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx");    // rhs
                        LoadI64("%ecx", "%edx");    // lhs
                        Emit("andl %ebx, %edx");
                        Emit("andl %ecx, %eax");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%eax"); // rhs
                        LoadI32("%ecx"); // lhs
                        Emit("andl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else {
                    throw new NotImplementedException();
                }

            }

            public void Or(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx");    // rhs
                        LoadI64("%ecx", "%edx");    // lhs
                        Emit("orl %ebx, %edx");
                        Emit("orl %ecx, %eax");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%eax"); // rhs
                        LoadI32("%ecx"); // lhs
                        Emit("orl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else {
                    throw new NotImplementedException();
                }
            }

            public void Xor(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx");    // rhs
                        LoadI64("%ecx", "%edx");    // lhs
                        Emit("xorl %ebx, %edx");
                        Emit("xorl %ecx, %eax");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%eax"); // rhs
                        LoadI32("%ecx"); // lhs
                        Emit("xorl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else {
                    throw new NotImplementedException();
                }

            }
            public void Shl(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%ecx", "%ebx"); // rhs
                        LoadI64("%eax", "%edx"); // lhs
                        Emit("shldl %cl, %eax, %edx");
                        if (lhs.Type.IsUnsignedIntegerType()) {
                            Emit("shll %cl, %eax");
                        } else {
                            Emit("sall %cl, %eax");
                        }
                        Emit("testb $32, %cl");
                        var l = LAlloc();
                        Emit($"je {l}");
                        Emit("movl %eax, %edx");
                        Emit("xorl %eax, %eax");
                        Label(l);
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        if (lhs.Type.IsUnsignedIntegerType()) {
                            Emit("shll %cl, %eax");
                        } else {
                            Emit("sall %cl, %eax");
                        }
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else {
                    throw new NotImplementedException();
                }
            }

            public void Shr(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%ecx", "%ebx"); // rhs
                        LoadI64("%eax", "%edx"); // lhs
                        Emit("shrdl %cl, %edx, %eax");
                        if (lhs.Type.IsUnsignedIntegerType()) {
                            Emit("shrl %cl, %edx");
                            Emit("testb $32, %cl");
                            var l = LAlloc();
                            Emit($"je {l}");
                            Emit("movl %edx, %eax");
                            Emit("xorl %edx, %edx");
                            Label(l);
                            Emit("pushl %edx");
                            Emit("pushl %eax");
                        } else {
                            Emit("sarl %cl, %edx");
                            Emit("testb $32, %cl");
                            var l = LAlloc();
                            Emit($"je {l}");
                            Emit("movl %edx, %eax");
                            Emit("sarl $31, %edx");
                            Label(l);
                            Emit("pushl %edx");
                            Emit("pushl %eax");
                        }
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        if (lhs.Type.IsUnsignedIntegerType()) {
                            Emit("shrl %cl, %eax");
                        } else {
                            Emit("sarl %cl, %eax");
                        }
                        Emit("pushl %eax");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
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
                        rhs = LoadI32("%ecx");
                        Emit($"movb %cl, (%eax)");
                        Emit($"pushl %ecx");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        break;
                    case 2:
                        rhs = LoadI32("%ecx");
                        Emit($"movw %cx, (%eax)");
                        Emit($"pushl %ecx");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        break;
                    case 3:
                    case 4:
                        rhs = LoadI32("%ecx");    // ToDo: float のコピーにLoadI32を転用しているのを修正
                        Emit($"movl %ecx, (%eax)");
                        Emit($"pushl %ecx");
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        break;
                    default:
                        LoadS(rhs.Type);
                        Emit($"movl %esp, %esi");
                        Emit($"movl ${type.Sizeof()}, %ecx");
                        Emit($"movl %eax, %edi");
                        Emit($"cld");
                        Emit($"rep movsb");
                        Pop();
                        Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        break;
                }

            }

            public void Eq(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx");
                        LoadI64("%ecx", "%edx");
                        var lFalse = LAlloc();
                        Emit($"cmp %eax, %ecx");
                        Emit($"movl $0, %eax");
                        Emit($"jne {lFalse}");
                        Emit($"cmp %ebx, %edx");
                        Emit($"jne {lFalse}");
                        Emit($"movl $1, %eax");
                        Label(lFalse);
                        Emit($"pushl %eax");
                    } else {
                        LoadI32("%eax");
                        LoadI32("%ecx");
                        Emit($"cmpl %ecx, %eax");

                        Emit($"sete %al");
                        Emit($"movzbl %al, %eax");
                        Emit($"pushl %eax");
                    }
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    LoadI32("%eax");
                    LoadI32("%ecx");
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

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx");
                        LoadI64("%ecx", "%edx");
                        var lFalse = LAlloc();
                        Emit($"cmp %eax, %ecx");
                        Emit($"movl $1, %eax");
                        Emit($"jne {lFalse}");
                        Emit($"cmp %ebx, %edx");
                        Emit($"jne {lFalse}");
                        Emit($"movl $0, %eax");
                        Label(lFalse);
                        Emit($"pushl %eax");
                    } else {
                        LoadI32("%eax");
                        LoadI32("%ecx");
                        Emit($"cmpl %ecx, %eax");

                        Emit($"setne %al");
                        Emit($"movzbl %al, %eax");
                        Emit($"pushl %eax");
                    }
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    LoadI32("%eax");
                    LoadI32("%ecx");
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
                LoadI32("%eax");
                Emit($"cmpl $0, %eax");
                Emit($"je {label}");
            }
            public void jmp_true(string label) {
                LoadI32("%eax");
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
                    if (ret.Type.IsBasicType(CType.BasicType.TypeKind.UnsignedLongLongInt, CType.BasicType.TypeKind.SignedLongLongInt)) {
                        LoadI64("%eax", "%ecx");
                        if (ret.Type.IsSignedIntegerType()) {
                            if (selftykind == CType.BasicType.TypeKind.Char || selftykind == CType.BasicType.TypeKind.SignedChar) {
                                Emit($"movsbl %al, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.SignedShortInt) {
                                Emit($"cwtl");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.SignedInt || selftykind == CType.BasicType.TypeKind.SignedLongInt) {
                                //Emil($"movl %eax, %eax");  // do nothing;
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.SignedLongLongInt) {
                                Emit($"pushl %ecx");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedChar) {
                                Emit($"movzbl %al, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedShortInt) {
                                Emit($"movzwl %ax, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedInt || selftykind == CType.BasicType.TypeKind.UnsignedLongInt) {
                                //Emil($"movl %eax, %eax");  // do nothing;
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedLongLongInt) {
                                Emit($"pushl %ecx");
                                Emit($"pushl %eax");
                            } else {
                                throw new NotImplementedException();
                            }
                        } else {
                            if (selftykind == CType.BasicType.TypeKind.Char || selftykind == CType.BasicType.TypeKind.SignedChar) {
                                Emit($"movsbl %al, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.SignedShortInt) {
                                Emit($"cwtl");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.SignedInt || selftykind == CType.BasicType.TypeKind.SignedLongInt) {
                                //Emil($"movl %eax, %eax");  // do nothing;
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.SignedLongLongInt) {
                                Emit($"pushl %ecx");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedChar) {
                                Emit($"movzbl %al, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedShortInt) {
                                Emit($"movzwl %ax, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedInt || selftykind == CType.BasicType.TypeKind.UnsignedLongInt) {
                                //Emil($"movl %eax, %eax");  // do nothing;
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedLongLongInt) {
                                Emit($"pushl %ecx");
                                Emit($"pushl %eax");
                            } else {
                                throw new NotImplementedException();
                            }
                        }
                    } else {
                        LoadI32("%eax");
                        if (ret.Type.IsSignedIntegerType()) {
                            if (selftykind == CType.BasicType.TypeKind.Char || selftykind == CType.BasicType.TypeKind.SignedChar) {
                                Emit($"movsbl %al, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.SignedShortInt) {
                                Emit($"movswl %ax, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.SignedInt || selftykind == CType.BasicType.TypeKind.SignedLongInt) {
                                //Emil($"movl %eax, %eax");  // do nothing;
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.SignedLongLongInt) {
                                Emit($"pushl $0");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedChar) {
                                Emit($"movzbl %al, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedShortInt) {
                                Emit($"movzwl %ax, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedInt || selftykind == CType.BasicType.TypeKind.UnsignedLongInt) {
                                //Emil($"movl %eax, %eax");  // do nothing;
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedLongLongInt) {
                                Emit($"movl %eax, %edx");
                                Emit($"sarl $31, %edx");
                                Emit($"pushl %edx");
                                Emit($"pushl %eax");
                            } else {
                                throw new NotImplementedException();
                            }
                        } else {
                            if (selftykind == CType.BasicType.TypeKind.Char || selftykind == CType.BasicType.TypeKind.SignedChar) {
                                Emit($"movzbl %al, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.SignedShortInt) {
                                Emit($"movzwl %ax, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.SignedInt || selftykind == CType.BasicType.TypeKind.SignedLongInt) {
                                //Emil($"movl %eax, %eax");  // do nothing;
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.SignedLongLongInt) {
                                Emit($"pushl $0");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedChar) {
                                Emit($"movzbl %al, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedShortInt) {
                                Emit($"movzwl %ax, %eax");
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedInt || selftykind == CType.BasicType.TypeKind.UnsignedLongInt) {
                                //Emil($"movl %eax, %eax");  // do nothing;
                                Emit($"pushl %eax");
                            } else if (selftykind == CType.BasicType.TypeKind.UnsignedLongLongInt) {
                                Emit($"pushl $0");
                                Emit($"pushl %eax");
                            } else {
                                throw new NotImplementedException();
                            }
                        }
                    }
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
                        // sp+[0..1] -> [fpucwの退避値]
                        // sp+[2..3] -> [(精度=単精度, 丸め=ゼロ方向への丸め)を設定したfpucw]
                        // sp+[4..7] -> [浮動小数点数の整数への変換結果、その後は、int幅での変換結果]
                        switch (type.Sizeof()) {
                            case 1:
                                Emit($"sub $8, %esp");
                                Emit($"fnstcw 0(%esp)");
                                Emit($"movzwl 0(%esp), %eax");
                                Emit($"movb	$12, %ah");
                                Emit($"movw	%ax, 2(%esp)");
                                Emit($"fldcw 2(%esp)");
                                Emit($"fistps 4(%esp)");
                                Emit($"fldcw 0(%esp)");
                                Emit($"movzwl 4(%esp), %eax");
                                Emit($"movsbl %al, %eax");
                                Emit($"movl %eax, 4(%esp)");
                                Emit($"add $4, %esp");
                                break;
                            case 2:
                                Emit($"sub $8, %esp");
                                Emit($"fnstcw 0(%esp)");
                                Emit($"movzwl 0(%esp), %eax");
                                Emit($"movb	$12, %ah");
                                Emit($"movw	%ax, 2(%esp)");
                                Emit($"fldcw 2(%esp)");
                                Emit($"fistps 4(%esp)");
                                Emit($"fldcw 0(%esp)");
                                Emit($"movzwl 4(%esp), %eax");
                                Emit($"cwtl");
                                Emit($"movl %eax, 4(%esp)");
                                Emit($"add $4, %esp");
                                break;
                            case 4:
                                Emit($"sub $8, %esp");
                                Emit($"fnstcw 0(%esp)");
                                Emit($"movzwl 0(%esp), %eax");
                                Emit($"movb $12, %ah");
                                Emit($"movw	%ax, 2(%esp)");
                                Emit($"fldcw 2(%esp)");
                                Emit($"fistpl 4(%esp)");
                                Emit($"fldcw 0(%esp)");
                                //Emit($"movl 4(%esp), %eax");
                                //Emit($"movl %eax, 4(%esp)");
                                Emit($"add $4, %esp");
                                break;
                            case 8:
                                Emit($"sub $12, %esp");
                                Emit($"fnstcw 0(%esp)");
                                Emit($"movzwl 0(%esp), %eax");
                                Emit($"movb $12, %ah");
                                Emit($"movw %ax, 2(%esp)");
                                Emit($"fldcw 2(%esp)");
                                Emit($"fistpq 4(%esp)");
                                Emit($"fldcw 0(%esp)");
                                Emit($"add $4, %esp");
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    } else {
                        switch (type.Sizeof()) {
                            case 1:
                                Emit($"sub $8, %esp");
                                Emit($"fnstcw 0(%esp)");
                                Emit($"movzwl 0(%esp), %eax");
                                Emit($"movb $12, %ah");
                                Emit($"movw %ax, 2(%esp)");
                                Emit($"fldcw 2(%esp)");
                                Emit($"fistps 4(%esp)");
                                Emit($"fldcw 0(%esp)");
                                Emit($"movzwl 4(%esp), %eax");
                                Emit($"movzbl %al, %eax");
                                Emit($"movl %eax, 4(%esp)");
                                Emit($"add $4, %esp");
                                break;
                            case 2:
                                Emit($"sub $8, %esp");
                                Emit($"fnstcw (%esp)");
                                Emit($"movzwl (%esp), %eax");
                                Emit($"movb $12, %ah");
                                Emit($"movw %ax, 2(%esp)");
                                Emit($"fldcw 2(%esp)");
                                Emit($"fistps 4(%esp)");
                                Emit($"fldcw 0(%esp)");
                                Emit($"movzwl 4(%esp), %eax");
                                Emit($"cwtl");
                                Emit($"movl %eax, 4(%esp)");
                                Emit($"add $4, %esp");
                                break;
                            case 4:
                                Emit($"sub $12, %esp");
                                Emit($"fnstcw (%esp)");
                                Emit($"movzwl (%esp), %eax");
                                Emit($"movb $12, %ah");
                                Emit($"movw %ax, 2(%esp)");
                                Emit($"fldcw 2(%esp)");
                                Emit($"fistpq 4(%esp)");
                                Emit($"fldcw 0(%esp)");
                                Emit($"movl 4(%esp), %eax");
                                Emit($"movl %eax, 8(%esp)");
                                Emit($"add $8, %esp");
                                break;
                            case 8:
                                Emit($"sub $8, %esp");
                                Emit($"fstpl (%esp)");
                                Emit($"call	___fixunsdfdi");
                                Emit($"add $8, %esp");
                                Emit($"pushl %edx");
                                Emit($"pushl %eax");
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
                LoadI32("%ecx");
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
                } else {
                    resultSize = Align(funcType.ResultType.Sizeof());
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
                    var _argSize = Align(ao.Type.Sizeof());
                    argSize += _argSize;
                }

                // 戻り値が浮動小数点数およびlonglongではなく、eaxにも入らないならスタック上に格納先アドレスを積む
                if (resultSize > 4 && !funcType.ResultType.IsRealFloatingType() && !funcType.ResultType.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                    Emit($"leal {argSize + bakSize}(%esp), %eax");
                    Emit($"push %eax");
                }

                fun(this);
                LoadI32("%eax");
                Emit($"call *%eax");

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
                } else if (funcType.ResultType.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                    // longlongはedx:eax
                    Emit($"mov %eax, {(argSize + bakSize + 0)}(%esp)");
                    Emit($"mov %edx, {(argSize + bakSize + 4)}(%esp)");
                    Emit($"addl ${argSize}, %esp");
                } else if (resultSize > 4) {
                    // 戻り値は格納先アドレスに入れられているはず
                    Emit($"addl ${argSize + 4}, %esp");
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
            /// 整数値もしくはポインタ値を指定した32ビットレジスタにロードする。レジスタに入らないサイズはエラーになる
            /// </summary>
            /// <param name="value"></param>
            /// <param name="register"></param>
            /// <returns></returns>
            private Value LoadI32(string register) {
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

            private Value LoadI64(string reg_lo, string reg_hi) {
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
                                        Emit($"movb ${value.IntConst}, {ToByteReg(reg_lo)}");
                                        Emit($"movsbl {ToByteReg(reg_lo)}, {reg_lo}");
                                        Emit($"movl $0, {reg_hi}");
                                        break;
                                    case 2:
                                        Emit($"movw ${value.IntConst}, {ToWordReg(reg_lo)}");
                                        Emit($"movswl {ToWordReg(reg_lo)}, {reg_lo}");
                                        Emit($"movl $0, {reg_hi}");
                                        break;
                                    case 4:
                                        Emit($"movl ${value.IntConst}, {reg_lo}");
                                        Emit($"movl $0, {reg_hi}");
                                        break;
                                    case 8: {
                                            var bytes = BitConverter.GetBytes(value.IntConst);
                                            var lo = BitConverter.ToUInt32(bytes, 0);
                                            var hi = BitConverter.ToUInt32(bytes, 4);
                                            Emit($"movl ${lo}, {reg_lo}");
                                            Emit($"movl ${hi}, {reg_hi}");
                                            break;
                                        }
                                    default:
                                        throw new NotImplementedException();
                                }
                            } else {
                                switch (ValueType.Sizeof()) {
                                    case 1:
                                        Emit($"movb ${value.IntConst}, {ToByteReg(reg_lo)}");
                                        Emit($"movzbl {ToByteReg(reg_lo)}, {reg_lo}");
                                        Emit($"movl $0, {reg_hi}");
                                        break;
                                    case 2:
                                        Emit($"movw ${value.IntConst}, {ToWordReg(reg_lo)}");
                                        Emit($"movzwl {ToWordReg(reg_lo)}, {reg_lo}");
                                        Emit($"movl $0, {reg_hi}");
                                        break;
                                    case 4:
                                        Emit($"movl ${value.IntConst}, {reg_lo}");
                                        Emit($"movl $0, {reg_hi}");
                                        break;
                                    case 8: {
                                            var bytes = BitConverter.GetBytes(value.IntConst);
                                            var lo = BitConverter.ToUInt32(bytes, 0);
                                            var hi = BitConverter.ToUInt32(bytes, 4);
                                            Emit($"movl ${lo}, {reg_lo}");
                                            Emit($"movl ${hi}, {reg_hi}");
                                            break;
                                        }
                                    default:
                                        throw new NotImplementedException();
                                }
                            }
                            return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = "high={{reg_hi}, lo={reg_lo}" };
                        }
                    case Value.ValueKind.Temp:
                        if (ValueType.Sizeof() <= 4) {
                            // スタックトップの値をレジスタにロード
                            Emit($"popl {reg_lo}");
                            Emit($"movl $0, {reg_hi}");
                            return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = "high={{reg_hi}, lo={reg_lo}" };
                        } else if (ValueType.Sizeof() == 8) {
                            // スタックトップの値をレジスタにロード
                            Emit($"popl {reg_lo}");
                            Emit($"popl {reg_hi}");
                            return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = "high={{reg_hi}, lo={reg_lo}" };

                        } else {
                            throw new NotImplementedException();
                        }
                    case Value.ValueKind.FloatConst:
                        throw new NotImplementedException();
                    case Value.ValueKind.Register:
                        throw new NotImplementedException();
                    case Value.ValueKind.Var:
                    case Value.ValueKind.Address: {
                            // 変数値もしくは参照値をレジスタにロード
                            Func<int, string> src = null;
                            switch (value.Kind) {
                                case Value.ValueKind.Var:
                                    if (value.Label == null) {
                                        // ローカル変数のアドレスはebp相対
                                        src = (i) => $"{value.Offset + i}(%ebp)";
                                    } else {
                                        // グローバル変数のアドレスはラベル絶対
                                        src = (i) => $"{value.Label}+{value.Offset + i}";
                                    }
                                    break;
                                case Value.ValueKind.Address:
                                    // アドレス参照のアドレスはスタックトップの値
                                    Emit($"popl {reg_hi}");
                                    src = (i) => $"{i}({reg_hi})";
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }

                            if (ValueType.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt, CType.BasicType.TypeKind.Double)) {
                                Emit($"movl {src(0)}, {reg_lo}");
                                Emit($"movl {src(4)}, {reg_hi}");
                            } else {
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
                                Emit($"{op} {src(0)}, {reg_lo}");
                                Emit($"movl 0, {reg_hi}");
                            }
                            return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = "high={{reg_hi}, lo={reg_lo}" };
                        }
                        break;
                    case Value.ValueKind.Ref:
                        if (value.Label == null) {
                            Emit($"leal {value.Offset}(%ebp), {reg_lo}");
                        } else {
                            Emit($"leal {value.Label}+{value.Offset}, {reg_lo}");
                        }
                        Emit($"movl 0, {reg_hi}");
                        return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = "high={{reg_hi}, lo={reg_lo}" };
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
                        if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt, CType.BasicType.TypeKind.Double)) {
                            var bytes = BitConverter.GetBytes(rhs.IntConst);
                            var lo = BitConverter.ToUInt32(bytes, 0);
                            var hi = BitConverter.ToUInt32(bytes, 4);
                            Emit($"pushl ${hi}");
                            Emit($"pushl ${lo}");
                            Emit($"filq (%esp)");
                            Emit($"addl $8, %esp");
                        } else {
                            Emit($"pushl ${rhs.IntConst}");
                            Emit($"fild (%esp)");
                            Emit($"addl $4, %esp");
                        }
                        break;
                    case Value.ValueKind.FloatConst:
                        if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                            var bytes = BitConverter.GetBytes((float)rhs.FloatConst);
                            var dword = BitConverter.ToUInt32(bytes, 0);
                            Emit($"pushl ${dword}");
                            Emit($"flds (%esp)");
                            Emit($"addl $4, %esp");
                        } else if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
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
                            if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                                op = "flds";
                            } else if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                                op = "fldl";
                            } else {
                                throw new NotImplementedException();
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
                        } else if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                            Emit($"fldl (%esp)");
                            Emit($"addl $8, %esp");
                        } else {
                            throw new NotImplementedException();
                        }
                        break;
                    case Value.ValueKind.Address:
                        Emit($"pushl %eax");
                        Emit($"leal 4(%esp), %eax");
                        Emit($"movl (%eax), %eax");
                        if (rhs.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Float)) {
                            Emit($"flds (%eax)");
                        } else if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                            Emit($"fldl (%eax)");
                        } else {
                            throw new NotImplementedException();
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
                                    case 8: {
                                            var bytes = BitConverter.GetBytes(value.IntConst);
                                            var lo = BitConverter.ToUInt32(bytes, 0);
                                            var hi = BitConverter.ToUInt32(bytes, 4);
                                            Emit($"pushl ${hi}");
                                            Emit($"pushl ${lo}");
                                            break;
                                        }
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
                                    case 8: {
                                            var bytes = BitConverter.GetBytes(value.IntConst);
                                            var lo = BitConverter.ToUInt32(bytes, 0);
                                            var hi = BitConverter.ToUInt32(bytes, 4);
                                            Emit($"pushl ${hi}");
                                            Emit($"pushl ${lo}");
                                            break;
                                        }
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
                            Emit($"subl ${Align(ValueType.Sizeof())}, %esp");
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
                    case 8:
                        var subop = (op == "add") ? "adc" : "sbb";
                        Emit($"pushl 4(%eax)");
                        Emit($"pushl 0(%eax)");
                        Emit($"{op}l ${size}, 0(%eax)");
                        Emit($"{subop}l $0, 4(%eax)");
                        break;
                    case 4:
                        Emit($"pushl (%eax)");
                        Emit($"{op}l ${size}, (%eax)");
                        break;
                    case 2:
                        Emit($"movw (%eax), %cx");
                        if (type.IsSignedIntegerType()) {
                            Emit($"movswl %cx, %ecx");
                        } else {
                            Emit($"movzwl %cx, %ecx");
                        }
                        Emit($"pushl %ecx");
                        Emit($"{op}w ${size}, (%eax)");
                        break;
                    case 1:
                        Emit($"movb (%eax), %cl");
                        if (type.IsSignedIntegerType()) {
                            Emit($"movsbl %cl, %ecx");
                        } else {
                            Emit($"movzbl %cl, %ecx");
                        }
                        Emit($"pushl %ecx");
                        Emit($"{op}b ${size}, (%eax)");
                        break;
                    default:
                        throw new NotImplementedException();
                }
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

            private void gen_cmp_I32(string cmp) {
                LoadI32("%ecx");  // rhs
                LoadI32("%eax");  // lhs
                Emit("cmpl %ecx, %eax");
                Emit($"{cmp} %al");
                Emit("movzbl %al, %eax");
                Emit($"pushl %eax");
            }

            private void gen_cmp_I64(string cmp1, string cmp2, string cmp3) {
                var lTrue = LAlloc();
                var lFalse = LAlloc();
                // 
                LoadI64("%eax", "%ebx");  // rhs
                LoadI64("%ecx", "%edx");  // lhs
                Emit("cmpl %ebx, %edx");
                Emit("movl $1, %edx");
                Emit($"{cmp1} {lTrue}");
                Emit($"{cmp2} {lFalse}");
                Emit("cmpl %eax, %ecx");
                Emit($"{cmp3} {lTrue}");
                Label(lFalse);
                Emit("movl  $0, %edx");
                Label(lTrue);
                Emit("pushl %edx");
            }

            public void gt(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            gen_cmp_I64("ja", "jb", "ja");
                        } else {
                            gen_cmp_I64("jg", "jl", "ja");
                        }
                    } else {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            gen_cmp_I32("seta");
                        } else {
                            gen_cmp_I32("setg");
                        }
                    }
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    gen_cmp_I32("seta");
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();
                    LoadF();
                    Emit($"fcomip");
                    Emit($"fstp %st(0)");
                    Emit("seta %al");
                    Emit("movzbl %al, %eax");
                    Emit($"pushl %eax");
                } else {
                    throw new NotImplementedException();
                }
                Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
            }

            public void lt(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            gen_cmp_I64("jb", "ja", "jb");
                        } else {
                            gen_cmp_I64("jl", "jg", "jb");
                        }
                    } else {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            gen_cmp_I32("setb");
                        } else {
                            gen_cmp_I32("setl");
                        }
                    }
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    gen_cmp_I32("setb");
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();
                    LoadF();
                    Emit($"fcomip");
                    Emit($"fstp %st(0)");
                    Emit("setb %al");
                    Emit("movzbl %al, %eax");
                    Emit($"pushl %eax");
                } else {
                    throw new NotImplementedException();
                }
                Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
            }

            public void ge(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            gen_cmp_I64("ja", "jb", "jae");
                        } else {
                            gen_cmp_I64("jg", "jl", "jae");
                        }
                    } else {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            gen_cmp_I32("setae");
                        } else {
                            gen_cmp_I32("setge");
                        }
                    }
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    gen_cmp_I32("setae");
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();
                    LoadF();
                    Emit($"fcomip");
                    Emit($"fstp %st(0)");
                    Emit("setae	%al");
                    Emit("movzbl %al, %eax");
                    Emit($"pushl %eax");
                } else {
                    throw new NotImplementedException();
                }
                Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
            }

            public void le(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            gen_cmp_I64("jb", "ja", "jbe");
                        } else {
                            gen_cmp_I64("jl", "jg", "jbe");
                        }
                    } else {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            gen_cmp_I32("setbe");
                        } else {
                            gen_cmp_I32("setle");
                        }
                    }
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    gen_cmp_I32("setbe");
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    LoadF();
                    LoadF();
                    Emit($"fcomip");
                    Emit($"fstp %st(0)");
                    Emit("setbe %al");
                    Emit("movzbl %al, %eax");
                    Emit($"pushl %eax");
                } else {
                    throw new NotImplementedException();
                }
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
                    if (operand.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%edx");
                        Emit($"negl	%eax");
                        Emit($"adcl	$0, %edx");
                        Emit($"negl	%edx");
                        Emit($"pushl %edx");
                        Emit($"pushl %eax");
                    } else {
                        LoadI32("%eax");
                        Emit($"negl %eax");
                        Emit($"pushl %eax");
                    }

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
                    if (operand.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%edx");
                        Emit($"notl	%eax");
                        Emit($"notl	%edx");
                        Emit($"pushl %edx");
                        Emit($"pushl %eax");
                    } else {
                        LoadI32("%eax");
                        Emit($"notl %eax");
                        Emit($"pushl %eax");
                    }
                    Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
                } else {
                    throw new NotImplementedException();
                }
            }

            public void unot(CType type) {
                var operand = Peek(0);
                if (operand.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                    LoadI64("%eax", "%edx");
                    Emit($"orl %edx, %eax");
                    Emit($"cmpl $0, %eax");
                    Emit($"sete %al");
                    Emit($"movzbl %al, %eax");
                    Emit($"pushl %eax");
                } else {
                    LoadI32("%eax");
                    Emit($"cmpl $0, %eax");
                    Emit($"sete %al");
                    Emit($"movzbl %al, %eax");
                    Emit($"pushl %eax");
                }
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
                    case 8:
                        var subop = (op == "add") ? "adc" : "sbb";
                        Emit($"{op}l ${size}, 0(%eax)");
                        Emit($"{subop}l $0, 4(%eax)");
                        Emit($"pushl 4(%eax)");
                        Emit($"pushl 0(%eax)");
                        break;
                    case 4:
                        Emit($"{op}l ${size}, (%eax)");
                        Emit($"pushl (%eax)");
                        break;
                    case 2:
                        Emit($"{op}w ${size}, (%eax)");
                        if (type.IsSignedIntegerType()) {
                            Emit($"movswl (%eax), %ecx");
                        } else {
                            Emit($"movzwl (%eax), %ecx");
                        }
                        Emit($"pushl %ecx");
                        break;
                    case 1:
                        Emit($"{op}b ${size}, (%eax)");
                        if (type.IsSignedIntegerType()) {
                            Emit($"movsbl (%eax), %ecx");
                        } else {
                            Emit($"movzbl (%eax), %ecx");
                        }
                        Emit($"pushl %ecx");
                        break;
                    default:
                        throw new NotImplementedException();
                }
                Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
            }
            public void pinc(CType type) {
                preop(type, "add");
            }
            public void pdec(CType type) {
                preop(type, "sub");
            }

            public void refe(CType type) {
                LoadI32("%eax");
                Emit($"pushl %eax");
                Push(new Value() { Kind = Value.ValueKind.Address, Type = type });

            }

            public void ret(CType type) {
                if (type != null) {
                    var value = Peek(0);
                    if (value.Type.IsSignedIntegerType()) {
                        switch (value.Type.Sizeof()) {
                            case 1:
                                value = LoadI32("%eax");
                                Emit($"movsbl %al, %eax");
                                break;
                            case 2:
                                value = LoadI32("%eax");
                                Emit($"movswl %ax, %eax");
                                break;
                            case 4:
                                value = LoadI32("%eax");
                                break;
                            case 8:
                                value = LoadI64("%eax", "%edx");
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    } else if (value.Type.IsUnsignedIntegerType()) {
                        switch (value.Type.Sizeof()) {
                            case 1:
                                value = LoadI32("%eax");
                                Emit($"movzbl %al, %eax");
                                break;
                            case 2:
                                value = LoadI32("%eax");
                                Emit($"movxwl %ax, %eax");
                                break;
                            case 4:
                                value = LoadI32("%eax");
                                break;
                            case 8:
                                value = LoadI64("%eax", "%edx");
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    } else if (value.Type.IsRealFloatingType()) {
                        LoadF();
                    } else {
                        if (value.Type.Sizeof() <= 4) {
                            LoadI32("%eax");
                        } else {
                            LoadVA("%esi");
                            Emit($"movl ${value.Type.Sizeof()}, %ecx");
                            Emit($"movl 8(%ebp), %edi");
                            Emit($"cld");
                            Emit($"rep movsb");
                            if (value.Kind == Value.ValueKind.Temp) {
                                Emit($"addl {Align(value.Type.Sizeof())}, %esp");
                            }
                        }

                    }
                }

                Emit($"popl %ebx");
                Emit($"movl %ebp, %esp");
                Emit($"popl %ebp");
                Emit($"ret");
            }

            public void when(CType condType, long caseValue, string label) {
                if (condType.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                    var bytes = BitConverter.GetBytes(caseValue);
                    var lo = BitConverter.ToUInt32(bytes, 0);
                    var hi = BitConverter.ToUInt32(bytes, 4);
                    var lFalse = LAlloc();
                    Emit($"cmp ${lo}, %eax");
                    Emit($"jne {lFalse}");
                    Emit($"cmp ${hi}, %edx");
                    Emit($"jne {lFalse}");
                    Emit($"jmp {label}");
                    Label(lFalse);
                } else {
                    Emit($"cmp ${caseValue}, %eax");
                    Emit($"je {label}");
                }
            }

            public void case_of(CType condType, Action<Generator> p) {
                if (condType.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                    LoadI64("%eax", "%edx");
                } else {
                    LoadI32("%eax");
                }
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

                // 戻り値領域へのポインタ
                if ((!ft.ResultType.IsVoidType()) && ft.ResultType.Sizeof() > 4 && !ft.ResultType.IsRealFloatingType()) {
                    offset += 4;
                }
                // 引数（先頭から）
                arguments = new Dictionary<string, int>();
                var vars = new List<string>();
                foreach (var arg in ft.Arguments) {
                    vars.Add($"// <Argument Name=\"{arg.Ident.Raw}\" Offset=\"{offset}\" />");
                    arguments.Add(arg.Ident.Raw, offset);
                    offset += Generator.Align(arg.Type.Sizeof());
                }

                // ラベル
                genericlabels = new Dictionary<string, string>();

                g.Emit($".section .text");
                g.Emit($".globl {self.LinkageObject.LinkageId}");
                g.Emit($"{self.LinkageObject.LinkageId}:");
                vars.ForEach(x => g.Emit(x));
                g.Emit($"pushl %ebp");
                g.Emit($"movl %esp, %ebp");
                g.Emit($"pushl %ebx");
                var c = g.Emit(".error \"Stack size is need backpatch.\"");// スタックサイズは仮置き
                localScopeTotalSize = 4;    // pushl %ebx分
                maxLocalScopeTotalSize = 4;
                self.Body.Accept(this, value);
                c.Body = $"subl ${maxLocalScopeTotalSize}, %esp";   // スタックサイズをバックパッチ
                g.Emit($"popl %ebx");
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

            var elseLabel = g.LAlloc();
            var junctionLabel = g.LAlloc();

            g.jmp_false(elseLabel);

            var thenRet = self.ThenExpr.Accept(this, value);
            if (self.Type.IsVoidType()) {
                // スタック上の結果を捨てる
                g.Discard();
            } else {
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
            } else {
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
            var labelFalse = g.LAlloc();
            var labelJunction = g.LAlloc();
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
            var labelTrue = g.LAlloc();
            var labelJunction = g.LAlloc();
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
                        localScopeTotalSize += Generator.Align(x.LinkageObject.Type.Sizeof());
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
            var labelContinue = g.LAlloc();
            var labelBreak = g.LAlloc();

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

            var labelHead = g.LAlloc();
            var labelContinue = g.LAlloc();
            var labelBreak = g.LAlloc();

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
                genericlabels[self.Ident] = g.LAlloc();
            }
            g.Label(genericlabels[self.Ident]);
            self.Stmt.Accept(this, value);
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnGotoStatement(SyntaxTree.Statement.GotoStatement self, Value value) {
            if (genericlabels.ContainsKey(self.Label) == false) {
                genericlabels[self.Label] = g.LAlloc();
            }
            g.Jmp(genericlabels[self.Label]);
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnIfStatement(SyntaxTree.Statement.IfStatement self, Value value) {
            var cond = self.Cond.Accept(this, value);
            if (self.ElseStmt != null) {
                var elseLabel = g.LAlloc();
                var junctionLabel = g.LAlloc();

                g.jmp_false(elseLabel);

                var thenRet = self.ThenStmt.Accept(this, value);
                g.Jmp(junctionLabel);
                g.Label(elseLabel);
                var elseRet = self.ElseStmt.Accept(this, value);
                g.Label(junctionLabel);
            } else {
                var junctionLabel = g.LAlloc();

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
            var labelBreak = g.LAlloc();

            var ret = self.Cond.Accept(this, value);

            var labelDic = new Dictionary<SyntaxTree.Statement, string>();
            g.case_of(self.Cond.Type, (_g) => {
                foreach (var caseLabel in self.CaseLabels) {
                    var caseValue = caseLabel.Value;
                    var label = g.LAlloc();
                    labelDic.Add(caseLabel, label);
                    g.when(self.Cond.Type, caseValue, label);
                }
            });
            if (self.DefaultLabel != null) {
                var label = g.LAlloc();
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
            var labelContinue = g.LAlloc();
            var labelBreak = g.LAlloc();

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
                            switch (v.Type.Sizeof()) {
                                case 4: {
                                    var dwords = BitConverter.ToUInt32(BitConverter.GetBytes((float)v.FloatConst),0);
                                    Emit($".long {dwords}");
                                    break;

                                }
                                case 8: {
                                    var lo = BitConverter.ToUInt32(BitConverter.GetBytes((float)v.FloatConst), 0);
                                    var hi = BitConverter.ToUInt32(BitConverter.GetBytes((float)v.FloatConst), 0);

                                    Emit($".long {lo}, {hi}");
                                    break;
                                }
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
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
            return new SyntaxTreeCompileVisitor.Value() { Kind = SyntaxTreeCompileVisitor.Value.ValueKind.FloatConst, FloatConst = self.Value, Type = self.Type };
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

                var filledSize = suType.Members.Skip(self.Inits.Count).Aggregate(0, (s, x) => s + x.Type.Sizeof());
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

