using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
         *    long long型の結果はedx:eaxに格納される
         *  - 呼び出された側の関数ではEAX, ECX, EDXのレジスタの元の値を保存することなく使用してよい。
         *    呼び出し側の関数では必要ならば呼び出す前にそれらのレジスタをスタック上などに保存する。
         *    （なので、EBX, ESI, EDI は呼び出され側の先頭でスタックに退避している）
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
                Void, // 式の結果はvoidである。
                Temp, // 式の結果はスタック上の値である（値はスタックの一番上にある）
                IntConst, // 式の結果は整数定数値である
                FloatConst, // 式の結果は浮動小数点定数値である
                Var, // 式の結果は変数参照、もしくは引数参照である（アドレス値の示す先が値である）
                Ref, // 式の結果はオブジェクト参照である(アドレス値自体が値である)
                Address // 式の結果はアドレス参照である（スタックの一番上に参照先自体が積まれているような想定。実際には参照先のアドレス値がある）
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
                IntConst = ret.IntConst;
                FloatConst = ret.FloatConst;
                Label = ret.Label;
                Offset = ret.Offset;
                StackPos = ret.StackPos;
            }
        }

        /// <summary>
        ///     コード生成器(i386向け)
        /// </summary>
        public class CodeGenerator {
            public class Code {
                public string Body { get; set; }

                public Code(string body) {
                    Body = body;
                }

                public override string ToString() {
                    return Body;
                }
            }

            public List<Code> Codes { get; } = new List<Code>();

            private readonly Stack<Value> _stack = new Stack<Value>();

            public int StackSIze {
                get { return _stack.Count; }
            }

            private int _labelIndex;

            public string LabelAlloc() {
                return $".L{_labelIndex++}";
            }

            public static int StackAlign(int x) {
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
                if (v.Kind == Value.ValueKind.Temp) {
                    Emit($"addl ${StackAlign(v.Type.Sizeof())}, %esp"); // discard temp value
                }
                else if (v.Kind == Value.ValueKind.Address) {
                    Emit("addl $4, %esp"); // discard pointer
                }
            }

            public void Dup(int index) {
                var v = Peek(index);
                if (v.Kind == Value.ValueKind.Temp || v.Kind == Value.ValueKind.Address) {
                    int skipsize = 0;
                    for (int i = 0; i < index; i++) {
                        var v2 = Peek(i);
                        if (v2.Kind == Value.ValueKind.Temp || v2.Kind == Value.ValueKind.Address) {
                            skipsize += StackAlign(v2.Type.Sizeof());
                        }
                    }

                    if (v.Kind == Value.ValueKind.Temp) {
                        int size = StackAlign(v.Type.Sizeof());
                        if (size <= 4) {
                            Emit($"leal {skipsize}(%esp), %esi");
                            Emit("push (%esi)");
                        }
                        else {
                            Emit($"leal {skipsize}(%esp), %esi");
                            Emit($"leal {-size}(%esp), %esp");
                            Emit("movl %esp, %edi");
                            Emit($"movl ${size}, %ecx");
                            Emit("cld");
                            Emit("rep movsb");
                        }

                        Push(new Value {Kind = Value.ValueKind.Temp, Type = v.Type, StackPos = _stack.Count});
                    }
                    else if (v.Kind == Value.ValueKind.Address) {
                        Emit($"leal {skipsize}(%esp), %esi");
                        Emit("push (%esi)");
                        Push(new Value {Kind = Value.ValueKind.Address, Type = v.Type, StackPos = _stack.Count});
                    }
                    else {
                        throw new Exception();
                    }
                }
                else {
                    Push(v);
                }
            }

            public void Add(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("addl %eax, %ecx");
                        Emit("adcl %ebx, %edx");
                        Emit("pushl %edx");
                        Emit("pushl %ecx");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                    else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        Emit("addl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                         (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush(); // rhs
                    FpuPush(); // lhs
                    Emit("faddp");
                    FpuPop(type);
                }
                else if (lhs.Type.IsPointerType() && rhs.Type.IsIntegerType()) {
                    CType elemType = type.GetBasePointerType();

                    if (rhs.Kind == Value.ValueKind.IntConst && lhs.Kind == Value.ValueKind.Ref) {
                        Pop();
                        Pop();
                        Push(new Value {Kind = Value.ValueKind.Ref, Type = type, Label = lhs.Label, Offset = (int) (lhs.Offset + rhs.IntConst * elemType.Sizeof())});
                    }
                    else {
                        if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                            LoadI64("%ecx", "%edx"); // rhs(loのみ使う)
                        }
                        else {
                            LoadI32("%ecx"); // rhs = index
                        }

                        LoadPointer("%eax"); // lhs = base
                        Emit($"imull ${elemType.Sizeof()}, %ecx, %ecx"); // index *= sizeof(base[0])
                        Emit("addl %ecx, %eax"); // base += index
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else if (lhs.Type.IsIntegerType() && rhs.Type.IsPointerType()) {
                    CType elemType = type.GetBasePointerType();

                    if (lhs.Kind == Value.ValueKind.IntConst && rhs.Kind == Value.ValueKind.Ref) {
                        Pop();
                        Pop();
                        Push(new Value {Kind = Value.ValueKind.Ref, Type = type, Label = rhs.Label, Offset = (int) (rhs.Offset + lhs.IntConst * elemType.Sizeof())});
                    }
                    else {
                        LoadPointer("%ecx"); // rhs = base
                        if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                            LoadI64("%eax", "%edx"); // lhs(loのみ使う)
                        }
                        else {
                            LoadI32("%eax"); // lhs = index
                        }

                        Emit($"imull ${elemType.Sizeof()}, %ecx, %ecx"); // index *= sizeof(base[0])
                        Emit("addl %ecx, %eax"); // base += index
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else {
                    throw new Exception("");
                }
            }

            public void Sub(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("subl %eax, %ecx");
                        Emit("sbbl %ebx, %edx");
                        Emit("pushl %edx");
                        Emit("pushl %ecx");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                    else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        Emit("subl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                         (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush(); // rhs
                    FpuPush(); // lhs
                    Emit("fsubp");
                    FpuPop(type);
                }
                else if (lhs.Type.IsPointerType() && rhs.Type.IsIntegerType()) {
                    CType elemType = type.GetBasePointerType();

                    if (rhs.Kind == Value.ValueKind.IntConst && lhs.Kind == Value.ValueKind.Ref) {
                        Pop();
                        Pop();
                        Push(new Value {Kind = Value.ValueKind.Ref, Type = type, Label = lhs.Label, Offset = (int) (lhs.Offset - rhs.IntConst * elemType.Sizeof())});
                    }
                    else {
                        if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                            LoadI64("%ecx", "%edx"); // rhs(loのみ使う)
                        }
                        else {
                            LoadI32("%ecx"); // rhs = index
                        }

                        LoadPointer("%eax"); // lhs = base
                        Emit($"imull ${elemType.Sizeof()}, %ecx, %ecx"); // index *= sizeof(base[0])
                        Emit("subl %ecx, %eax"); // base += index
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else if (lhs.Type.IsIntegerType() && rhs.Type.IsPointerType()) {
                    CType elemType = type.GetBasePointerType();

                    if (lhs.Kind == Value.ValueKind.IntConst && rhs.Kind == Value.ValueKind.Ref) {
                        Pop();
                        Pop();
                        Push(new Value {Kind = Value.ValueKind.Ref, Type = type, Label = rhs.Label, Offset = (int) (rhs.Offset - lhs.IntConst * elemType.Sizeof())});
                    }
                    else {
                        LoadPointer("%ecx"); // rhs = base
                        if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                            LoadI64("%eax", "%edx"); // lhs(loのみ使う)
                        }
                        else {
                            LoadI32("%eax"); // lhs = index
                        }

                        Emit($"imull ${elemType.Sizeof()}, %ecx, %ecx"); // index *= sizeof(base[0])
                        Emit("subl %ecx, %eax"); // base += index
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    CType elemType = lhs.Type.GetBasePointerType();

                    if (lhs.Kind == Value.ValueKind.Ref && rhs.Kind == Value.ValueKind.Ref && lhs.Label == rhs.Label) {
                        Pop();
                        Pop();
                        Push(new Value {Kind = Value.ValueKind.IntConst, Type = CType.CreatePtrDiffT(), IntConst = ((lhs.Offset - rhs.Offset) / elemType.Sizeof())});
                    }
                    else {
                        // (rhs - lhs) / sizeof(elemType)
                        LoadPointer("%ecx"); // rhs = ptr
                        LoadPointer("%eax"); // lhs = ptr
                        Emit("subl %ecx, %eax");
                        Emit("cltd");
                        Emit($"movl ${elemType.Sizeof()}, %ecx");
                        Emit("idivl %ecx");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else {
                    throw new Exception("");
                }
            }

            public void Mul(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("pushl %edx"); // 12(%esp) : lhs.hi
                        Emit("pushl %ecx"); //  8(%esp) : lhs.lo
                        Emit("pushl %ebx"); //  4(%esp) : rhs.hi
                        Emit("pushl %eax"); //  0(%esp) : rhs.lo

                        Emit("movl 4(%esp), %eax"); // rhs.hi
                        Emit("movl %eax, %ecx");
                        Emit("imull 8(%esp), %ecx"); // lhs.lo
                        Emit("movl 12(%esp), %eax"); // lhs.hi
                        Emit("imull 0(%esp), %eax"); // rhs.lo
                        Emit("addl %eax, %ecx");
                        Emit("movl 8(%esp), %eax"); // lhs.lo
                        Emit("mull 0(%esp)"); // rhs.lo
                        Emit("addl %edx, %ecx");
                        Emit("movl %ecx, %edx");

                        Emit("addl $16, %esp");
                        Emit("pushl %edx");
                        Emit("pushl %eax");

                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                    else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        if (type.IsUnsignedIntegerType()) {
                            Emit("mull %ecx");
                        }
                        else {
                            Emit("imull %ecx");
                        }

                        Emit("push %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                         (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();

                    Emit("fmulp");

                    FpuPop(type);
                }
                else {
                    throw new NotImplementedException();
                }
            }

            public void Div(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("pushl %ebx"); // 12(%esp) : rhs.hi
                        Emit("pushl %eax"); //  8(%esp) : rhs.lo
                        Emit("pushl %edx"); //  4(%esp) : lhs.hi
                        Emit("pushl %ecx"); //  0(%esp) : lhs.lo
                        Emit("call ___divdi3");
                        Emit("addl $16, %esp");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                    else if (type.IsBasicType(CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("pushl %ebx"); // 12(%esp) : rhs.hi
                        Emit("pushl %eax"); //  8(%esp) : rhs.lo
                        Emit("pushl %edx"); //  4(%esp) : lhs.hi
                        Emit("pushl %ecx"); //  0(%esp) : lhs.lo
                        Emit("call ___udivdi3");
                        Emit("addl $16, %esp");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                    else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        if (type.IsUnsignedIntegerType()) {
                            Emit("movl $0, %edx");
                            Emit("divl %ecx");
                        }
                        else {
                            Emit("cltd");
                            Emit("idivl %ecx");
                        }

                        Emit("push %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                         (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();

                    Emit("fdivp");

                    FpuPop(type);
                }
                else {
                    throw new NotImplementedException();
                }
            }

            public void Mod(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("pushl %ebx"); // 12(%esp) : rhs.hi
                        Emit("pushl %eax"); //  8(%esp) : rhs.lo
                        Emit("pushl %edx"); //  4(%esp) : lhs.hi
                        Emit("pushl %ecx"); //  0(%esp) : lhs.lo
                        Emit("call ___moddi3");
                        Emit("addl $16, %esp");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                    else if (type.IsBasicType(CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("pushl %ebx"); // 12(%esp) : rhs.hi
                        Emit("pushl %eax"); //  8(%esp) : rhs.lo
                        Emit("pushl %edx"); //  4(%esp) : lhs.hi
                        Emit("pushl %ecx"); //  0(%esp) : lhs.lo
                        Emit("call ___umoddi3");
                        Emit("addl $16, %esp");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                    else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        if (type.IsUnsignedIntegerType()) {
                            Emit("movl $0, %edx");
                            Emit("divl %ecx");
                        }
                        else {
                            Emit("cltd");
                            Emit("idivl %ecx");
                        }

                        Emit("push %edx");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else {
                    throw new NotImplementedException();
                }
            }

            public void And(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("andl %ebx, %edx");
                        Emit("andl %ecx, %eax");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                    else {
                        LoadI32("%eax"); // rhs
                        LoadI32("%ecx"); // lhs
                        Emit("andl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else {
                    throw new NotImplementedException();
                }
            }

            public void Or(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("orl %ebx, %edx");
                        Emit("orl %ecx, %eax");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                    else {
                        LoadI32("%eax"); // rhs
                        LoadI32("%ecx"); // lhs
                        Emit("orl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else {
                    throw new NotImplementedException();
                }
            }

            public void Xor(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("xorl %ebx, %edx");
                        Emit("xorl %ecx, %eax");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                    else {
                        LoadI32("%eax"); // rhs
                        LoadI32("%ecx"); // lhs
                        Emit("xorl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else {
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
                        }
                        else {
                            Emit("sall %cl, %eax");
                        }

                        Emit("testb $32, %cl");
                        var l = LabelAlloc();
                        Emit($"je {l}");
                        Emit("movl %eax, %edx");
                        Emit("xorl %eax, %eax");
                        Label(l);
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                    else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        if (lhs.Type.IsUnsignedIntegerType()) {
                            Emit("shll %cl, %eax");
                        }
                        else {
                            Emit("sall %cl, %eax");
                        }

                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else {
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
                            var l = LabelAlloc();
                            Emit($"je {l}");
                            Emit("movl %edx, %eax");
                            Emit("xorl %edx, %edx");
                            Label(l);
                            Emit("pushl %edx");
                            Emit("pushl %eax");
                        }
                        else {
                            Emit("sarl %cl, %edx");
                            Emit("testb $32, %cl");
                            var l = LabelAlloc();
                            Emit($"je {l}");
                            Emit("movl %edx, %eax");
                            Emit("sarl $31, %edx");
                            Label(l);
                            Emit("pushl %edx");
                            Emit("pushl %eax");
                        }

                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                    else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        if (lhs.Type.IsUnsignedIntegerType()) {
                            Emit("shrl %cl, %eax");
                        }
                        else {
                            Emit("sarl %cl, %eax");
                        }

                        Emit("pushl %eax");
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                }
                else {
                    throw new NotImplementedException();
                }
            }

            public void Assign(CType type) {
                var lhs = Peek(0);
                var rhs = Peek(1);

                CType.BitFieldType bft;
                if (type.IsBitField(out bft)) {
                    // ビットフィールドへの代入の場合
                    LoadVariableAddress("%eax"); // lhs
                    switch (bft.Type.Sizeof()) {
                        case 1: {
                                int offsetBit = bft.BitOffset;
                                UInt32 srcMask = (UInt32)((1U << bft.BitWidth) - 1);
                                UInt32 dstMask = ~(srcMask << (offsetBit));
                                LoadI32("%ecx");    // 右辺式の値を取り出す

                                if (!bft.IsUnsignedIntegerType()) {
                                    // ビットフィールドの最上位ビットとそれより上を値の符号ビットで埋める
                                    Emit($"sall ${32 - bft.BitWidth}, %ecx");
                                    Emit($"sarl ${32 - bft.BitWidth}, %ecx");
                                }
                                Emit("pushl %ecx");

                                // ビットマスク処理と位置合わせをする
                                Emit($"andl ${srcMask}, %ecx");
                                Emit($"shll ${offsetBit}, %ecx");

                                // フィールドが属する領域を読み出してフィールドの範囲のビットを消す
                                Emit($"movb (%eax), %dl");
                                Emit($"andb ${(byte)dstMask}, %dl");
                                // ビットを結合させてから書き込む
                                Emit($"orb  %dl, %cl");
                                Emit($"movb %cl, (%eax)");
                                Push(new Value { Kind = Value.ValueKind.Temp, Type = bft.Type, StackPos = _stack.Count });
                                break;
                            }
                        case 2: {
                                int offsetBit = bft.BitOffset;
                                UInt32 srcMask = (UInt32)((1U << bft.BitWidth) - 1);
                                UInt32 dstMask = ~(srcMask << (offsetBit));
                                LoadI32("%ecx");    // 右辺式の値を取り出す

                                if (!bft.IsUnsignedIntegerType()) {
                                    // ビットフィールドの最上位ビットとそれより上を値の符号ビットで埋める
                                    Emit($"sall ${32 - bft.BitWidth}, %ecx");
                                    Emit($"sarl ${32 - bft.BitWidth}, %ecx");
                                }
                                Emit("pushl %ecx");

                                // ビットマスク処理と位置合わせをする
                                Emit($"andl ${srcMask}, %ecx");
                                Emit($"shll ${offsetBit}, %ecx");

                                // フィールドが属する領域を読み出してフィールドの範囲のビットを消す
                                Emit($"movw (%eax), %dx");
                                Emit($"andw ${(UInt16)dstMask}, %dx");
                                // ビットを結合させてから書き込む
                                Emit($"orw  %dx, %cx");
                                Emit($"movw %cx, (%eax)");
                                Push(new Value { Kind = Value.ValueKind.Temp, Type = bft.Type, StackPos = _stack.Count });
                                break;
                            }
                        case 3: 
                        case 4: {
                                int offsetBit = bft.BitOffset;
                                UInt32 srcMask = (UInt32)((1U << bft.BitWidth) - 1);
                                UInt32 dstMask = ~(srcMask << (offsetBit));
                                LoadI32("%ecx");    // 右辺式の値を取り出す

                                if (!bft.IsUnsignedIntegerType()) {
                                    // ビットフィールドの最上位ビットとそれより上を値の符号ビットで埋める
                                    Emit($"sall ${32 - bft.BitWidth}, %ecx");
                                    Emit($"sarl ${32 - bft.BitWidth}, %ecx");
                                }
                                Emit("pushl %ecx");

                                // ビットマスク処理と位置合わせをする
                                Emit($"andl ${srcMask}, %ecx");
                                Emit($"shll ${offsetBit}, %ecx");

                                // フィールドが属する領域を読み出してフィールドの範囲のビットを消す
                                Emit($"movl (%eax), %edx");
                                Emit($"andl ${(UInt16)dstMask}, %edx");
                                // ビットを結合させてから書き込む
                                Emit($"orl  %edx, %ecx");
                                Emit($"movl %ecx, (%eax)");
                                Push(new Value { Kind = Value.ValueKind.Temp, Type = bft.Type, StackPos = _stack.Count });
                                break;
                            }
                        default:
                            throw new NotSupportedException();
                    }
                } else {
                    LoadVariableAddress("%eax"); // lhs
                    switch (type.Sizeof()) {
                        case 1:
                            LoadI32("%ecx");
                            Emit("movb %cl, (%eax)");
                            Emit("pushl %ecx");
                            Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                            break;
                        case 2:
                            LoadI32("%ecx");
                            Emit("movw %cx, (%eax)");
                            Emit("pushl %ecx");
                            Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                            break;
                        case 3:
                        case 4:
                            LoadI32("%ecx"); // ToDo: float のコピーにLoadI32を転用しているのを修正
                            Emit("movl %ecx, (%eax)");
                            Emit("pushl %ecx");
                            Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                            break;
                        default:
                            LoadValueToStack(rhs.Type);
                            Emit("movl %esp, %esi");
                            Emit($"movl ${type.Sizeof()}, %ecx");
                            Emit("movl %eax, %edi");
                            Emit("cld");
                            Emit("rep movsb");
                            Pop();
                            Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                            break;
                    }
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
                        var lFalse = LabelAlloc();
                        Emit("cmp %eax, %ecx");
                        Emit("movl $0, %eax");
                        Emit($"jne {lFalse}");
                        Emit("cmp %ebx, %edx");
                        Emit($"jne {lFalse}");
                        Emit("movl $1, %eax");
                        Label(lFalse);
                        Emit("pushl %eax");
                    }
                    else {
                        LoadI32("%eax");
                        LoadI32("%ecx");
                        Emit("cmpl %ecx, %eax");

                        Emit("sete %al");
                        Emit("movzbl %al, %eax");
                        Emit("pushl %eax");
                    }

                    Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                }
                else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    LoadI32("%eax");
                    LoadI32("%ecx");
                    Emit("cmpl %ecx, %eax");
                    Emit("sete %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                    Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                }
                else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                         (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();

                    Emit("fcomip");
                    Emit("fstp %st(0)");

                    Emit("sete %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");

                    Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                }
                else {
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
                        var lFalse = LabelAlloc();
                        Emit("cmp %eax, %ecx");
                        Emit("movl $1, %eax");
                        Emit($"jne {lFalse}");
                        Emit("cmp %ebx, %edx");
                        Emit($"jne {lFalse}");
                        Emit("movl $0, %eax");
                        Label(lFalse);
                        Emit("pushl %eax");
                    }
                    else {
                        LoadI32("%eax");
                        LoadI32("%ecx");
                        Emit("cmpl %ecx, %eax");

                        Emit("setne %al");
                        Emit("movzbl %al, %eax");
                        Emit("pushl %eax");
                    }

                    Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                }
                else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    LoadI32("%eax");
                    LoadI32("%ecx");
                    Emit("cmpl %ecx, %eax");
                    Emit("setne %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                    Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                }
                else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                         (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();

                    Emit("fcomip");
                    Emit("fstp %st(0)");

                    Emit("setne %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");

                    Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                }
                else {
                    throw new NotImplementedException();
                }
            }

            public void Label(string label) {
                Emit($"{label}:");
            }

            public void Jmp(string label) {
                Emit($"jmp {label}");
            }

            public void JmpFalse(string label) {
                LoadI32("%eax");
                Emit("cmpl $0, %eax");
                Emit($"je {label}");
            }

            public void JmpTrue(string label) {
                LoadI32("%eax");
                Emit("cmpl $0, %eax");
                Emit($"jne {label}");
            }

            public void EmitLoadTrue() {
                Emit("pushl $1");
            }

            public void EmitLoadFalse() {
                Emit("pushl $0");
            }

            private void CastIntValueToInt(CType type) {
                Value ret = Peek(0);
                System.Diagnostics.Debug.Assert(ret.Type.IsIntegerType() && type.IsIntegerType());

                CType.BasicType.TypeKind selftykind;
                if (type.IsBasicType()) {
                    selftykind = (type.Unwrap() as CType.BasicType).Kind;
                } else if (type.IsEnumeratedType()) {
                    selftykind = CType.BasicType.TypeKind.SignedInt;
                } else {
                    throw new NotImplementedException();
                }

                if (ret.Type.IsBasicType(CType.BasicType.TypeKind.UnsignedLongLongInt, CType.BasicType.TypeKind.SignedLongLongInt)) {
                    // 64bit型からのキャスト
                    LoadI64("%eax", "%ecx");
                    /* 符号の有無にかかわらず、切り捨てでよい？ */
                    switch (selftykind) {
                        case CType.BasicType.TypeKind.Char:
                        case CType.BasicType.TypeKind.SignedChar:
                            Emit("movsbl %al, %eax");
                            Emit("pushl %eax");
                            break;
                        case CType.BasicType.TypeKind.SignedShortInt:
                            Emit("cwtl");
                            Emit("pushl %eax");
                            break;
                        case CType.BasicType.TypeKind.SignedInt:
                        case CType.BasicType.TypeKind.SignedLongInt:
                            // do nothing;
                            Emit("pushl %eax");
                            break;
                        case CType.BasicType.TypeKind.SignedLongLongInt:
                            Emit("pushl %ecx");
                            Emit("pushl %eax");
                            break;
                        case CType.BasicType.TypeKind.UnsignedChar:
                            Emit("movzbl %al, %eax");
                            Emit("pushl %eax");
                            break;
                        case CType.BasicType.TypeKind.UnsignedShortInt:
                            Emit("movzwl %ax, %eax");
                            Emit("pushl %eax");
                            break;
                        case CType.BasicType.TypeKind.UnsignedInt:
                        case CType.BasicType.TypeKind.UnsignedLongInt:
                            // do nothing;
                            Emit("pushl %eax");
                            break;
                        case CType.BasicType.TypeKind.UnsignedLongLongInt:
                            Emit("pushl %ecx");
                            Emit("pushl %eax");
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                } else {
                    // 32bit以下の型からのキャスト
                    LoadI32("%eax");
                    switch (selftykind) {
                        case CType.BasicType.TypeKind.Char:
                        case CType.BasicType.TypeKind.SignedChar:
                            Emit("movsbl %al, %eax");
                            Emit("pushl %eax");
                            break;
                        case CType.BasicType.TypeKind.SignedShortInt:
                            Emit("movswl %ax, %eax");
                            Emit("pushl %eax");
                            break;
                        case CType.BasicType.TypeKind.SignedInt:
                        case CType.BasicType.TypeKind.SignedLongInt:
                            // do nothing;
                            Emit("pushl %eax");
                            break;
                        case CType.BasicType.TypeKind.SignedLongLongInt:
                            if (ret.Type.IsUnsignedIntegerType()) {
                                Emit("pushl $0");
                                Emit("pushl %eax");
                            } else {
                                Emit("movl %eax, %edx");
                                Emit("sarl $31, %edx");
                                Emit("pushl %edx");
                                Emit("pushl %eax");
                            }

                            break;
                        case CType.BasicType.TypeKind.UnsignedChar:
                            Emit("movzbl %al, %eax");
                            Emit("pushl %eax");
                            break;
                        case CType.BasicType.TypeKind.UnsignedShortInt:
                            Emit("movzwl %ax, %eax");
                            Emit("pushl %eax");
                            break;
                        case CType.BasicType.TypeKind.UnsignedInt:
                        case CType.BasicType.TypeKind.UnsignedLongInt:
                            // do nothing;
                            Emit("pushl %eax");
                            break;
                        case CType.BasicType.TypeKind.UnsignedLongLongInt:
                            if (ret.Type.IsUnsignedIntegerType()) {
                                Emit("pushl $0");
                                Emit("pushl %eax");
                            } else {
                                Emit("movl %eax, %edx");
                                Emit("sarl $31, %edx");
                                Emit("pushl %edx");
                                Emit("pushl %eax");
                            }

                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }

                Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
            }

            private void CastPointerValueToPointer(CType type) {
                Value ret = Peek(0);
                System.Diagnostics.Debug.Assert(ret.Type.IsPointerType() && type.IsPointerType());
                Pop();
                Push(new Value(ret) { Type = type });
            }

            private void CastArrayValueToPointer(CType type) {
                Value ret = Peek(0);
                System.Diagnostics.Debug.Assert(ret.Type.IsArrayType() && type.IsPointerType());
                Pop();
                // 手抜き
                if (ret.Kind == Value.ValueKind.Var) {
                    ret = new Value { Kind = Value.ValueKind.Ref, Type = type, Label = ret.Label, Offset = ret.Offset };
                    Push(ret);
                } else if (ret.Kind == Value.ValueKind.Ref) {
                    ret = new Value { Kind = Value.ValueKind.Ref, Type = type, Label = ret.Label, Offset = ret.Offset };
                    Push(ret);
                } else if (ret.Kind == Value.ValueKind.Address) {
                    ret = new Value { Kind = Value.ValueKind.Temp, Type = type };
                    Push(ret);
                } else {
                    throw new NotImplementedException();
                }
            }

            private void CastArrayValueToArray(CType type) {
                Value ret = Peek(0);
                System.Diagnostics.Debug.Assert(ret.Type.IsArrayType() && type.IsArrayType());
                Pop();
                Push(new Value(ret) { Type = type });
            }

            private void CastIntValueToPointer(CType type) {
                Value ret = Peek(0);
                System.Diagnostics.Debug.Assert(ret.Type.IsIntegerType() && type.IsPointerType());
                // Todo: 64bit int value -> 32bit pointer value の実装
                Pop();
                Push(new Value(ret) { Type = type });
            }

            private void CastPointerValueToInt(CType type) {
                Value ret = Peek(0);
                System.Diagnostics.Debug.Assert(ret.Type.IsPointerType() && type.IsIntegerType());
                // Todo: 32bit pointer value -> 64bit int value の実装
                Pop();
                Push(new Value(ret) { Type = type });
            }

            private void CastFloatingValueToFloating(CType type) {
                Value ret = Peek(0);
                System.Diagnostics.Debug.Assert(ret.Type.IsRealFloatingType() && type.IsRealFloatingType());
                FpuPush();
                FpuPop(type);
            }

            private void CastFloatingValueToInt(CType type) {
                Value ret = Peek(0);
                System.Diagnostics.Debug.Assert(ret.Type.IsRealFloatingType() && type.IsIntegerType());
                FpuPush();

                // double -> unsigned char
                // 	movzwl <value>, %eax
                //  movzbl %al, %eax
                if (type.IsSignedIntegerType()) {
                    // sp+[0..1] -> [fpucwの退避値]
                    // sp+[2..3] -> [(精度=単精度, 丸め=ゼロ方向への丸め)を設定したfpucw]
                    // sp+[4..7] -> [浮動小数点数の整数への変換結果、その後は、int幅での変換結果]
                    switch (type.Sizeof()) {
                        case 1:
                            Emit("sub $8, %esp");
                            Emit("fnstcw 0(%esp)");
                            Emit("movzwl 0(%esp), %eax");
                            Emit("movb $12, %ah");
                            Emit("movw %ax, 2(%esp)");
                            Emit("fldcw 2(%esp)");
                            Emit("fistps 4(%esp)");
                            Emit("fldcw 0(%esp)");
                            Emit("movzwl 4(%esp), %eax");
                            Emit("movsbl %al, %eax");
                            Emit("movl %eax, 4(%esp)");
                            Emit("add $4, %esp");
                            break;
                        case 2:
                            Emit("sub $8, %esp");
                            Emit("fnstcw 0(%esp)");
                            Emit("movzwl 0(%esp), %eax");
                            Emit("movb $12, %ah");
                            Emit("movw %ax, 2(%esp)");
                            Emit("fldcw 2(%esp)");
                            Emit("fistps 4(%esp)");
                            Emit("fldcw 0(%esp)");
                            Emit("movzwl 4(%esp), %eax");
                            Emit("cwtl");
                            Emit("movl %eax, 4(%esp)");
                            Emit("add $4, %esp");
                            break;
                        case 4:
                            Emit("sub $8, %esp");
                            Emit("fnstcw 0(%esp)");
                            Emit("movzwl 0(%esp), %eax");
                            Emit("movb $12, %ah");
                            Emit("movw %ax, 2(%esp)");
                            Emit("fldcw 2(%esp)");
                            Emit("fistpl 4(%esp)");
                            Emit("fldcw 0(%esp)");
                            //Emit($"movl 4(%esp), %eax");
                            //Emit($"movl %eax, 4(%esp)");
                            Emit("add $4, %esp");
                            break;
                        case 8:
                            Emit("sub $12, %esp");
                            Emit("fnstcw 0(%esp)");
                            Emit("movzwl 0(%esp), %eax");
                            Emit("movb $12, %ah");
                            Emit("movw %ax, 2(%esp)");
                            Emit("fldcw 2(%esp)");
                            Emit("fistpq 4(%esp)");
                            Emit("fldcw 0(%esp)");
                            Emit("add $4, %esp");
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                } else {
                    switch (type.Sizeof()) {
                        case 1:
                            Emit("sub $8, %esp");
                            Emit("fnstcw 0(%esp)");
                            Emit("movzwl 0(%esp), %eax");
                            Emit("movb $12, %ah");
                            Emit("movw %ax, 2(%esp)");
                            Emit("fldcw 2(%esp)");
                            Emit("fistps 4(%esp)");
                            Emit("fldcw 0(%esp)");
                            Emit("movzwl 4(%esp), %eax");
                            Emit("movzbl %al, %eax");
                            Emit("movl %eax, 4(%esp)");
                            Emit("add $4, %esp");
                            break;
                        case 2:
                            Emit("sub $8, %esp");
                            Emit("fnstcw (%esp)");
                            Emit("movzwl (%esp), %eax");
                            Emit("movb $12, %ah");
                            Emit("movw %ax, 2(%esp)");
                            Emit("fldcw 2(%esp)");
                            Emit("fistps 4(%esp)");
                            Emit("fldcw 0(%esp)");
                            Emit("movzwl 4(%esp), %eax");
                            Emit("cwtl");
                            Emit("movl %eax, 4(%esp)");
                            Emit("add $4, %esp");
                            break;
                        case 4:
                            Emit("sub $12, %esp");
                            Emit("fnstcw (%esp)");
                            Emit("movzwl (%esp), %eax");
                            Emit("movb $12, %ah");
                            Emit("movw %ax, 2(%esp)");
                            Emit("fldcw 2(%esp)");
                            Emit("fistpq 4(%esp)");
                            Emit("fldcw 0(%esp)");
                            Emit("movl 4(%esp), %eax");
                            Emit("movl %eax, 8(%esp)");
                            Emit("add $8, %esp");
                            break;
                        case 8:
                            Emit("sub $8, %esp");
                            Emit("fstpl (%esp)");
                            Emit("call\t___fixunsdfdi");
                            Emit("add $8, %esp");
                            Emit("pushl %edx");
                            Emit("pushl %eax");
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }

                Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
            }

            private void CastIntValueToFloating(CType type) {
                Value ret = Peek(0);
                System.Diagnostics.Debug.Assert(ret.Type.IsIntegerType() && type.IsRealFloatingType());
                FpuPush();
                FpuPop(type);
            }

            public void CastTo(CType type) {
                Value ret = Peek(0);
                if (ret.Type.IsIntegerType() && type.IsIntegerType()) {
                    CastIntValueToInt(type);
                } else if (ret.Type.IsPointerType() && type.IsPointerType()) {
                    CastPointerValueToPointer(type);
                } else if (ret.Type.IsArrayType() && type.IsPointerType()) {
                    CastArrayValueToPointer(type);
                } else if (ret.Type.IsArrayType() && type.IsArrayType()) {
                    CastArrayValueToArray(type);
                } else if (ret.Type.IsPointerType() && type.IsArrayType()) {
                    throw new NotImplementedException();
                } else if (ret.Type.IsIntegerType() && type.IsPointerType()) {
                    CastIntValueToPointer(type);
                } else if (ret.Type.IsPointerType() && type.IsIntegerType()) {
                    CastPointerValueToInt(type);
                } else if (ret.Type.IsRealFloatingType() && type.IsRealFloatingType()) {
                    CastFloatingValueToFloating(type);
                } else if (ret.Type.IsRealFloatingType() && type.IsIntegerType()) {
                    CastFloatingValueToInt(type);
                } else if (ret.Type.IsIntegerType() && type.IsRealFloatingType()) {
                    CastIntValueToFloating(type);
                } else if (ret.Type.IsStructureType() && type.IsStructureType()) {
                    // キャスト不要
                } else if (ret.Type.IsUnionType() && type.IsUnionType()) {
                    // キャスト不要
                } else {
                    throw new NotImplementedException();
                }
            }

            public void ArraySubscript(CType type) {
                var index = Peek(0);
                var target = Peek(1);
                LoadI32("%ecx");
                if (target.Type.IsPointerType()) {
                    LoadPointer("%eax");
                }
                else {
                    LoadVariableAddress("%eax");
                }

                Emit($"imull ${type.Sizeof()}, %ecx, %ecx");
                Emit("leal (%eax, %ecx), %eax");
                Emit("pushl %eax");

                Push(new Value {Kind = Value.ValueKind.Address, Type = type, StackPos = _stack.Count});
            }

            public void Call(CType type, CType.FunctionType funcType, int argnum, Action<CodeGenerator> fun, Action<CodeGenerator, int> args) {
                /*
                 *  - 関数への引数は右から左の順でスタックに積まれる。
                 *    - 引数にはベースポインタ相対でアクセスする
                 *  - 関数の戻り値は EAXに格納できるサイズならば EAX に格納される。EAXに格納できないサイズならば、戻り値を格納する領域のアドレスを引数の上に積み、EAXを使わない。（※）
                 *  - 呼び出された側の関数ではEAX, ECX, EDXのレジスタの元の値を保存することなく使用してよい。
                 *    呼び出し側の関数では必要ならば呼び出す前にそれらのレジスタをスタック上などに保存する。
                 *  - スタックポインタの処理は呼び出し側で行う。  
                 */

                int resultSize;
                if (funcType.ResultType.IsVoidType()) {
                    resultSize = 0;
                }
                else {
                    resultSize = StackAlign(funcType.ResultType.Sizeof());
                    Emit($"subl ${resultSize}, %esp"); // 関数呼び出しの結果の格納先をスタックトップに確保
                }

                // EAX,ECX,EDXを保存
                int bakSize = 4 * 3;
                Emit("pushl %eax");
                Emit("pushl %ecx");
                Emit("pushl %edx");

                int argSize = 0;

                // 引数を右側（末尾側）からスタックに積む
                for (int i = argnum - 1; i >= 0; i--) {
                    args(this, i);
                    var argval = Peek(0);
                    LoadValueToStack(argval.Type);
                    argSize += StackAlign(argval.Type.Sizeof());
                }

                // 戻り値が浮動小数点数型でもlong long型ではなく、eaxに入らないサイズならスタック上に格納先アドレスを積む
                if (resultSize > 4 && !funcType.ResultType.IsRealFloatingType() && !funcType.ResultType.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                    Emit($"leal {argSize + bakSize}(%esp), %eax");
                    Emit("push %eax");
                }

                fun(this);
                LoadI32("%eax");
                Emit("call *%eax");

                if (funcType.ResultType.IsRealFloatingType()) {
                    // 浮動小数点数型の結果はFPUスタック上にある
                    if (funcType.ResultType.IsBasicType(CType.BasicType.TypeKind.Float)) {
                        Emit($"fstps {(argSize + bakSize)}(%esp)");
                    }
                    else if (funcType.ResultType.IsBasicType(CType.BasicType.TypeKind.Double)) {
                        Emit($"fstpl {(argSize + bakSize)}(%esp)");
                    }
                    else {
                        throw new NotImplementedException();
                    }

                    Emit($"addl ${argSize}, %esp");
                }
                else if (funcType.ResultType.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                    // long long型の結果はedx:eaxに格納される
                    Emit($"mov %eax, {(argSize + bakSize + 0)}(%esp)");
                    Emit($"mov %edx, {(argSize + bakSize + 4)}(%esp)");
                    Emit($"addl ${argSize}, %esp");
                }
                else if (resultSize > 4) {
                    // 戻り値は格納先アドレスに入れられているはず
                    Emit($"addl ${argSize + 4}, %esp");
                }
                else if (resultSize > 0) {
                    // 戻り値をコピー(4バイト以下)
                    Emit($"movl %eax, {(argSize + bakSize)}(%esp)");
                    Emit($"addl ${argSize}, %esp");
                }
                else {
                    // void型？
                    Emit($"addl ${argSize}, %esp");
                }

                Emit("popl %edx");
                Emit("popl %ecx");
                Emit("popl %eax");

                System.Diagnostics.Debug.Assert(_stack.Count >= argnum);
                for (int i = 0; i < argnum; i++) {
                    Pop(); // args
                }

                Push(new Value {Kind = resultSize == 0 ? Value.ValueKind.Void : Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
            }

            private int GetMemberOffset(CType type, string member) {
                var st = type.Unwrap() as CType.TaggedType.StructUnionType;
                if (st == null) {
                    throw new Exception("構造体/共用体型でない型に対してメンバの算出を試みました。");
                }
#if false
                int offset = 0;
                foreach (var m in st.Members) {
                    if (m.Ident.Raw == member) {
                        break;
                    }

                    if (st.Kind == CType.TaggedType.StructUnionType.StructOrUnion.Struct) {
                        offset += m.Type.Sizeof();
                    }
                }
                return offset;
#else
                foreach (var m in st.Members) {
                    if (m.Ident != null && m.Ident.Raw == member) {
                        return m.Offset;
                    }
                }
                throw new Exception("");
#endif
            }

            public void DirectMember(CType type, string member) {
                var obj = Peek(0);

                int offset = GetMemberOffset(obj.Type, member);
                LoadVariableAddress("%eax");
                Emit($"addl ${offset}, %eax");
                Emit("pushl %eax");
                Push(new Value {Kind = Value.ValueKind.Address, Type = type, StackPos = _stack.Count});
            }

            public void IndirectMember(CType type, string member) {
                var obj = Peek(0);
                CType baseType;
                if (obj.Type.IsPointerType(out baseType) == false) {
                    throw new Exception("構造体/共用体型へのポインタでない型に対してメンバの算出を試みました。");
                }
                int offset = GetMemberOffset(baseType, member);
                LoadPointer("%eax");
                Emit($"addl ${offset}, %eax");
                Emit("pushl %eax");
                Push(new Value {Kind = Value.ValueKind.Address, Type = type, StackPos = _stack.Count});
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

            private string VarRefToAddrExpr(Value value, int offset = 0) {
                if (value.Kind != Value.ValueKind.Var && value.Kind != Value.ValueKind.Ref) {
                    throw new Exception("変数参照、もしくは引数参照ではないオブジェクトのアドレス式を生成しようとしました。");
                }
                if (value.Label == null) {
                    // ローカル変数のアドレスはebp相対
                    return $"{value.Offset + offset}(%ebp)";
                } else {
                    // グローバル変数のアドレスはラベル絶対
                    return $"{value.Label}+{value.Offset + offset}";
                }
            }

            /// <summary>
            ///     整数値もしくはポインタ値を指定した32ビットレジスタにロードする。レジスタに入らないサイズはエラーになる
            /// </summary>
            /// <param name="register"></param>
            /// <returns></returns>
            private void LoadI32(string register) {
                var value = Pop();
                var valueType = value.Type;
                switch (value.Kind) {
                    case Value.ValueKind.IntConst: {
                        // 定数値をレジスタにロードする。
                        // 純粋な整数定数値の他に、整数定数値をポインタ型にキャストしたものもここに含む
                        CType.BasicType.TypeKind kind;
                        if (valueType.Unwrap() is CType.BasicType) {
                            kind = (valueType.Unwrap() as CType.BasicType).Kind;
                        } else if (valueType.IsEnumeratedType()) {
                            kind = CType.BasicType.TypeKind.SignedInt;
                        } else if (valueType.IsPointerType()) {
                            kind = CType.BasicType.TypeKind.UnsignedInt;
                        } else {
                            throw new Exception("整数定数値の型が不正です");
                        }
                        switch (kind) {
                            case CType.BasicType.TypeKind.Char:
                            case CType.BasicType.TypeKind.SignedChar:
                                Emit($"movb ${value.IntConst}, {ToByteReg(register)}");
                                Emit($"movsbl {ToByteReg(register)}, {register}");
                                break;
                            case CType.BasicType.TypeKind.UnsignedChar:
                                Emit($"movb ${value.IntConst}, {ToByteReg(register)}");
                                Emit($"movzbl {ToByteReg(register)}, {register}");
                                break;
                            case CType.BasicType.TypeKind.SignedShortInt:
                                Emit($"movw ${value.IntConst}, {ToWordReg(register)}");
                                Emit($"movswl {ToWordReg(register)}, {register}");
                                break;
                            case CType.BasicType.TypeKind.UnsignedShortInt:
                                Emit($"movw ${value.IntConst}, {ToWordReg(register)}");
                                Emit($"movzwl {ToWordReg(register)}, {register}");
                                break;
                            case CType.BasicType.TypeKind.SignedInt:
                            case CType.BasicType.TypeKind.SignedLongInt:
                                Emit($"movl ${value.IntConst}, {register}");
                                break;
                            case CType.BasicType.TypeKind.UnsignedInt:
                            case CType.BasicType.TypeKind.UnsignedLongInt:
                                Emit($"movl ${value.IntConst}, {register}");
                                break;
                            case CType.BasicType.TypeKind.SignedLongLongInt:
                            case CType.BasicType.TypeKind.UnsignedLongLongInt:
                            default:
                                throw new Exception("32bitレジスタにロードできない定数値です。");
                        }


                        return;
                    }
                    case Value.ValueKind.Temp:
                        if (valueType.Sizeof() <= 4) {
                            // スタックトップの値をレジスタにロード
                            Emit($"popl {register}");
                            return;
                        }
                        else {
                            throw new NotImplementedException();
                        }
                    case Value.ValueKind.FloatConst:
                        throw new NotImplementedException();
                    case Value.ValueKind.Var:
                    case Value.ValueKind.Address: {
                        // 変数値もしくは参照値をレジスタにロード
                        string src;
                        switch (value.Kind) {
                            case Value.ValueKind.Var:
                                // 変数参照はアドレス式を生成
                                src = VarRefToAddrExpr(value);
                                break;
                            case Value.ValueKind.Address:
                                // アドレス参照のアドレスはスタックトップの値
                                Emit($"popl {register}");
                                src = $"({register})";
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        string op;
                        // ビットフィールドは特別扱い
                        CType.BitFieldType bft;
                            if (valueType.IsBitField(out bft)) {
                                switch (bft.Type.Sizeof()) {
                                    case 1: {
                                            int offsetBit = bft.BitOffset;
                                            UInt32 srcMask = (UInt32)((1U << bft.BitWidth) - 1);
                                            UInt32 dstMask = ~(srcMask << (offsetBit));

                                            // フィールドが属する領域を読み出し右詰してから、無関係のビットを消す
                                            var byteReg = ToByteReg(register);
                                            Emit($"movb (%eax), {byteReg}");
                                            Emit($"shrl ${offsetBit}, {register}");
                                            Emit($"andl ${srcMask}, {register}");

                                            if (!bft.IsUnsignedIntegerType()) {
                                                // ビットフィールドの最上位ビットとそれより上を値の符号ビットで埋める
                                                Emit($"shll ${32 - bft.BitWidth}, {register}");
                                                Emit($"sarl ${32 - bft.BitWidth}, {register}");
                                            }

                                            return;
                                        }
                                    case 2: {
                                            int offsetBit = bft.BitOffset;
                                            UInt32 srcMask = (UInt32)((1U << bft.BitWidth) - 1);
                                            UInt32 dstMask = ~(srcMask << (offsetBit));

                                            // フィールドが属する領域を読み出し右詰してから、無関係のビットを消す
                                            var wordReg = ToWordReg(register);
                                            Emit($"movw (%eax), {wordReg}");
                                            Emit($"shrl ${offsetBit}, {register}");
                                            Emit($"andl ${srcMask}, {register}");

                                            if (!bft.IsUnsignedIntegerType()) {
                                                // ビットフィールドの最上位ビットとそれより上を値の符号ビットで埋める
                                                Emit($"shll ${32 - bft.BitWidth}, {register}");
                                                Emit($"sarl ${32 - bft.BitWidth}, {register}");
                                            }

                                            return;

                                        }
                                    case 3:
                                    case 4: {
                                            int offsetBit = bft.BitOffset;
                                            UInt32 srcMask = (UInt32)((1U << bft.BitWidth) - 1);
                                            UInt32 dstMask = ~(srcMask << (offsetBit));

                                            // フィールドが属する領域を読み出し右詰してから、無関係のビットを消す
                                            Emit($"movl (%eax), {register}");
                                            Emit($"shrl ${offsetBit}, {register}");
                                            Emit($"andl ${srcMask}, {register}");

                                            if (!bft.IsUnsignedIntegerType()) {
                                                // ビットフィールドの最上位ビットとそれより上を値の符号ビットで埋める
                                                Emit($"shll ${32 - bft.BitWidth}, {register}");
                                                Emit($"sarl ${32 - bft.BitWidth}, {register}");
                                            }

                                            return;
                                        }
                                    default:
                                        throw new NotSupportedException();
                                }
                            } else {
                                if (valueType.IsSignedIntegerType() || valueType.IsBasicType(CType.BasicType.TypeKind.Char) || valueType.IsEnumeratedType()) {
                                    switch (valueType.Sizeof()) {
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
                                } else if (valueType.IsUnsignedIntegerType()) {
                                    switch (valueType.Sizeof()) {
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
                                } else if (valueType.IsBasicType(CType.BasicType.TypeKind.Float)) {
                                    op = "movl";
                                } else if (valueType.IsPointerType() || valueType.IsArrayType()) {
                                    op = "movl";
                                } else if (valueType.IsStructureType()) {
                                    op = "movl";    // ここmovlでOK?
                                } else {
                                    throw new NotImplementedException();
                                }

                                Emit($"{op} {src}, {register}");
                                return;
                            }
                    }
                    case Value.ValueKind.Ref:
                        Emit($"leal {VarRefToAddrExpr(value)}, {register}");
                        return;
                    default:
                        throw new NotImplementedException();
                }
            }

            /// <summary>
            ///     整数値もしくはポインタ値を指定した32ビットレジスタ二つを使う64bit値としてロードする。レジスタに入らないサイズはエラーになる
            /// </summary>
            /// <param name="regLo"></param>
            /// <param name="regHi"></param>
            /// <returns></returns>
            private void LoadI64(string regLo, string regHi) {
                var value = Pop();
                var valueType = value.Type;

                switch (value.Kind) {
                    case Value.ValueKind.IntConst: {
                        // 定数値をレジスタにロードする。
                        if (valueType.IsSignedIntegerType() || valueType.IsBasicType(CType.BasicType.TypeKind.Char)) {
                            switch (valueType.Sizeof()) {
                                case 1:
                                    Emit($"movb ${value.IntConst}, {ToByteReg(regLo)}");
                                    Emit($"movsbl {ToByteReg(regLo)}, {regLo}");
                                    Emit($"movl $0, {regHi}");
                                    break;
                                case 2:
                                    Emit($"movw ${value.IntConst}, {ToWordReg(regLo)}");
                                    Emit($"movswl {ToWordReg(regLo)}, {regLo}");
                                    Emit($"movl $0, {regHi}");
                                    break;
                                case 4:
                                    Emit($"movl ${value.IntConst}, {regLo}");
                                    Emit($"movl $0, {regHi}");
                                    break;
                                case 8: {
                                    var bytes = BitConverter.GetBytes(value.IntConst);
                                    var lo = BitConverter.ToUInt32(bytes, 0);
                                    var hi = BitConverter.ToUInt32(bytes, 4);
                                    Emit($"movl ${lo}, {regLo}");
                                    Emit($"movl ${hi}, {regHi}");
                                    break;
                                }
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        else {
                            switch (valueType.Sizeof()) {
                                case 1:
                                    Emit($"movb ${value.IntConst}, {ToByteReg(regLo)}");
                                    Emit($"movzbl {ToByteReg(regLo)}, {regLo}");
                                    Emit($"movl $0, {regHi}");
                                    break;
                                case 2:
                                    Emit($"movw ${value.IntConst}, {ToWordReg(regLo)}");
                                    Emit($"movzwl {ToWordReg(regLo)}, {regLo}");
                                    Emit($"movl $0, {regHi}");
                                    break;
                                case 4:
                                    Emit($"movl ${value.IntConst}, {regLo}");
                                    Emit($"movl $0, {regHi}");
                                    break;
                                case 8: {
                                    var bytes = BitConverter.GetBytes(value.IntConst);
                                    var lo = BitConverter.ToUInt32(bytes, 0);
                                    var hi = BitConverter.ToUInt32(bytes, 4);
                                    Emit($"movl ${lo}, {regLo}");
                                    Emit($"movl ${hi}, {regHi}");
                                    break;
                                }
                                default:
                                    throw new NotImplementedException();
                            }
                        }

                        return;
                    }
                    case Value.ValueKind.Temp:
                        if (valueType.Sizeof() <= 4) {
                            // スタックトップの値をレジスタにロード
                            Emit($"popl {regLo}");
                            Emit($"movl $0, {regHi}");
                            return;
                        }
                        else if (valueType.Sizeof() == 8) {
                            // スタックトップの値をレジスタにロード
                            Emit($"popl {regLo}");
                            Emit($"popl {regHi}");
                            return;
                        }
                        else {
                            throw new NotImplementedException();
                        }
                    case Value.ValueKind.FloatConst:
                        throw new NotImplementedException();
                    case Value.ValueKind.Var:
                    case Value.ValueKind.Address: {
                        // 変数値もしくは参照値をレジスタにロード
                        Func<int, string> src;
                        switch (value.Kind) {
                            case Value.ValueKind.Var:
                                // 変数参照はアドレス式を生成
                                src = offset => VarRefToAddrExpr(value, offset);
                                break;
                            case Value.ValueKind.Address:
                                // アドレス参照のアドレスはスタックトップの値
                                Emit($"popl {regHi}");
                                src = offset => $"{offset}({regHi})";
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        if (valueType.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt, CType.BasicType.TypeKind.Double)) {
                            Emit($"movl {src(0)}, {regLo}");
                            Emit($"movl {src(4)}, {regHi}");
                        }
                        else {
                            string op;
                            if (valueType.IsSignedIntegerType() || valueType.IsBasicType(CType.BasicType.TypeKind.Char) || valueType.IsEnumeratedType()) {
                                switch (valueType.Sizeof()) {
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
                            }
                            else if (valueType.IsUnsignedIntegerType()) {
                                switch (valueType.Sizeof()) {
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
                            }
                            else if (valueType.IsBasicType(CType.BasicType.TypeKind.Float)) {
                                op = "movl";
                            }
                            else if (valueType.IsPointerType() || valueType.IsArrayType()) {
                                op = "movl";
                            }
                            else if (valueType.IsStructureType()) {
                                op = "leal";
                            }
                            else {
                                throw new NotImplementedException();
                            }

                            Emit($"{op} {src(0)}, {regLo}");
                            Emit($"movl $0, {regHi}");
                        }

                        return;
                    }
                    case Value.ValueKind.Ref:
                        Emit($"leal {VarRefToAddrExpr(value)}, {regLo}");
                        Emit($"movl $0, {regHi}");
                        return;
                    default:
                        throw new NotImplementedException();
                }
            }

            /// <summary>
            ///     FPUスタック上に値をロードする
            /// </summary>
            private void FpuPush() {
                var rhs = Peek(0);
                if (rhs.Kind != Value.ValueKind.Temp) {
                    LoadValueToStack(rhs.Type);
                }

                rhs = Pop();
                if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                    Emit("flds (%esp)");
                    Emit("addl $4, %esp");
                }
                else if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                    Emit("fldl (%esp)");
                    Emit("addl $8, %esp");
                }
                else if (rhs.Type.IsIntegerType()) {
                    if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt)) {
                        Emit("fildq (%esp)");
                        Emit("addl $8, %esp");
                    }
                    else if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        Emit("fildq (%esp)");
                        Emit("cmpl $0, 4(%esp)");
                        var l = LabelAlloc();
                        Emit($"jns {l}");
                        Emit("pushl $16447"); // 0000403F
                        Emit("pushl $-2147483648"); // 80000000
                        Emit("pushl $0"); // 00000000
                        Emit("fldt (%esp)");
                        Emit("addl $12, %esp");
                        Emit("faddp %st, %st(1)");
                        Label(l);
                        Emit("addl $8, %esp");
                    }
                    else if (rhs.Type.IsBasicType(CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedInt)) {
                        Emit("pushl $0");
                        Emit("pushl 4(%esp)");
                        Emit("fildq (%esp)");
                        Emit("addl $12, %esp");
                    }
                    else {
                        Emit("fildl (%esp)");
                        Emit("addl $4, %esp");
                    }
                }
                else {
                    throw new NotImplementedException();
                }
            }

            /// <summary>
            ///     FPUスタックの一番上の値をポップし、CPUスタックの一番上に積む
            /// </summary>
            /// <param name="ty"></param>
            public void FpuPop(CType ty) {
                if (ty.IsBasicType(CType.BasicType.TypeKind.Float)) {
                    Emit("sub $4, %esp");
                    Emit("fstps (%esp)");
                    Push(new Value {Kind = Value.ValueKind.Temp, Type = ty, StackPos = _stack.Count});
                }
                else if (ty.IsBasicType(CType.BasicType.TypeKind.Double)) {
                    Emit("sub $8, %esp");
                    Emit("fstpl (%esp)");
                    Push(new Value {Kind = Value.ValueKind.Temp, Type = ty, StackPos = _stack.Count});
                }
                else {
                    throw new NotImplementedException();
                }
            }

            /// <summary>
            ///     ポインタをロード
            /// </summary>
            /// <param name="reg"></param>
            public void LoadPointer(string reg) {
                Value target = Pop();
                System.Diagnostics.Debug.Assert(target.Type.IsPointerType());

                switch (target.Kind) {
                    case Value.ValueKind.Var:
                        // ポインタ型の変数 => mov命令で変数の値をロード
                        Emit($"movl {VarRefToAddrExpr(target)}, {reg}");
                        break;
                    case Value.ValueKind.Ref:
                        // ラベル => ラベルのアドレスをロード
                        Emit($"leal {VarRefToAddrExpr(target)}, {reg}");
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
            ///     左辺値変数のアドレスをロード
            /// </summary>
            /// <param name="reg"></param>
            public void LoadVariableAddress(string reg) {
                Value lhs = Pop();
                switch (lhs.Kind) {
                    case Value.ValueKind.Var:
                    case Value.ValueKind.Ref:
                        // 変数も参照も同じ扱いができる
                        Emit($"leal {VarRefToAddrExpr(lhs)}, {reg}");
                        break;
                    case Value.ValueKind.Address:
                        Emit($"popl {reg}");
                        break;
                    case Value.ValueKind.Temp:
                        Emit($"movl %esp, {reg}");
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            /// <summary>
            ///     値をスタックトップに積む。もともとスタックトップにある場合は何もしない
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            public void LoadValueToStack(CType type) {
                Value value = Peek(0);
                var valueType = value.Type;
                switch (value.Kind) {
                    case Value.ValueKind.IntConst: {
                        // 定数値をスタックに積む
                        if (valueType.IsSignedIntegerType() || valueType.IsBasicType(CType.BasicType.TypeKind.Char)) {
                            switch (valueType.Sizeof()) {
                                case 1:
                                    Emit($"pushl ${unchecked((int) (sbyte) value.IntConst)}");
                                    break;
                                case 2:
                                    Emit($"pushl ${unchecked((int) (short) value.IntConst)}");
                                    break;
                                case 4:
                                    Emit($"pushl ${unchecked((int) value.IntConst)}");
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
                        else {
                            switch (valueType.Sizeof()) {
                                case 1:
                                    Emit($"pushl ${unchecked((int) (byte) value.IntConst)}");
                                    break;
                                case 2:
                                    Emit($"pushl ${unchecked((int) (ushort) value.IntConst)}");
                                    break;
                                case 4:
                                    Emit($"pushl ${unchecked((int) (uint) value.IntConst)}");
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
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                        break;
                    case Value.ValueKind.Temp:
                        break;
                    case Value.ValueKind.FloatConst: {
                        if (value.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Float)) {
                            var bytes = BitConverter.GetBytes((float) value.FloatConst);
                            var dword = BitConverter.ToUInt32(bytes, 0);
                            Emit($"pushl ${dword}");
                        }
                        else if (value.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Double)) {
                            var bytes = BitConverter.GetBytes(value.FloatConst);
                            var qwordlo = BitConverter.ToUInt32(bytes, 0);
                            var qwordhi = BitConverter.ToUInt32(bytes, 4);
                            Emit($"pushl ${qwordhi}");
                            Emit($"pushl ${qwordlo}");
                        }
                        else {
                            throw new NotImplementedException();
                        }

                        Pop();
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                        break;
                    case Value.ValueKind.Var:
                    case Value.ValueKind.Address: {
                        // コピー元アドレスをロード
                        switch (value.Kind) {
                            case Value.ValueKind.Var:
                                    // ラベル参照
                                Emit($"leal {VarRefToAddrExpr(value)}, %esi");

                                    break;
                            case Value.ValueKind.Address:
                                // アドレス参照のアドレスはスタックトップの値
                                Emit("popl %esi");
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        // コピー先を確保
                        Emit($"subl ${StackAlign(valueType.Sizeof())}, %esp");
                        Emit("movl %esp, %edi");

                        // 転送
                        Emit("pushl %ecx");
                        Emit($"movl ${valueType.Sizeof()}, %ecx");
                        Emit("cld");
                        Emit("rep movsb");
                        Emit("popl %ecx");

                        Pop();
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                        break;
                    case Value.ValueKind.Ref: {
                            // ラベル参照
                        Emit($"leal {VarRefToAddrExpr(value)}, %esi");
                        Emit("pushl %esi");
                        Pop();
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                    }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            private void PostfixOp(CType type, string op) {
                if (type.IsIntegerType() || type.IsPointerType()) {
                    CType baseType;
                    int size;
                    if (type.IsPointerType(out baseType) && !baseType.IsVoidType()) {
                        size = baseType.Sizeof();
                    }
                    else {
                        size = 1;
                    }

                    LoadVariableAddress("%eax");

                    // load value
                    switch (type.Sizeof()) {
                        case 8:
                            var subop = (op == "add") ? "adc" : "sbb";
                            Emit("pushl 4(%eax)");
                            Emit("pushl 0(%eax)");
                            Emit($"{op}l ${size}, 0(%eax)");
                            Emit($"{subop}l $0, 4(%eax)");
                            break;
                        case 4:
                            Emit("pushl (%eax)");
                            Emit($"{op}l ${size}, (%eax)");
                            break;
                        case 2:
                            Emit("movw (%eax), %cx");
                            if (type.IsSignedIntegerType()) {
                                Emit("movswl %cx, %ecx");
                            }
                            else {
                                Emit("movzwl %cx, %ecx");
                            }

                            Emit("pushl %ecx");
                            Emit($"{op}w ${size}, (%eax)");
                            break;
                        case 1:
                            Emit("movb (%eax), %cl");
                            if (type.IsSignedIntegerType()) {
                                Emit("movsbl %cl, %ecx");
                            }
                            else {
                                Emit("movzbl %cl, %ecx");
                            }

                            Emit("pushl %ecx");
                            Emit($"{op}b ${size}, (%eax)");
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                }
                else if (type.IsRealFloatingType()) {
                    LoadVariableAddress("%eax");
                    if (type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                        Emit("flds (%eax)");
                        Emit("sub $4, %esp");
                        Emit("fsts (%esp)");
                    }
                    else if (type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                        Emit("fldl (%eax)");
                        Emit("sub $8, %esp");
                        Emit("fstl (%esp)");
                    }
                    else {
                        throw new NotImplementedException();
                    }

                    Emit("fld1");
                    Emit($"f{op}p");
                    if (type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                        Emit("fstps (%eax)");
                    }
                    else if (type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                        Emit("fstpl (%eax)");
                    }
                    else {
                        throw new NotImplementedException();
                    }

                    Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                }
                else {
                    throw new NotImplementedException();
                }
            }

            public void PostInc(CType type) {
                PostfixOp(type, "add");
            }

            public void PostDec(CType type) {
                PostfixOp(type, "sub");
            }

            public void CalcConstAddressOffset(CType type, long offset) {
                var ret = Pop();
                switch (ret.Kind) {
                    case Value.ValueKind.Var:
                    case Value.ValueKind.Ref:
                        Push(new Value {Kind = Value.ValueKind.Ref, Type = type, Label = ret.Label, Offset = (int) (ret.Offset + offset)});
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            /// <summary>
            /// 32bit以下の値同士の比較コードを生成。
            /// </summary>
            /// <param name="cmp"></param>
            private void GenCompare32(string cmp) {
                LoadI32("%ecx"); // rhs
                LoadI32("%eax"); // lhs
                Emit("cmpl %ecx, %eax");
                Emit($"{cmp} %al");
                Emit("movzbl %al, %eax");
                Emit("pushl %eax");
            }

            /// <summary>
            /// 64bit以下の値同士の比較コードを生成。
            /// </summary>
            /// <param name="cmp1"></param>
            /// <param name="cmp2"></param>
            /// <param name="cmp3"></param>
            private void GenCompare64(string cmp1, string cmp2, string cmp3) {
                var lTrue = LabelAlloc();
                var lFalse = LabelAlloc();
                // 
                LoadI64("%eax", "%ebx"); // rhs
                LoadI64("%ecx", "%edx"); // lhs
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

            public void GreatThan(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare64("ja", "jb", "ja");
                        }
                        else {
                            GenCompare64("jg", "jl", "ja");
                        }
                    }
                    else {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare32("seta");
                        }
                        else {
                            GenCompare32("setg");
                        }
                    }
                }
                else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    GenCompare32("seta");
                }
                else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                         (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();
                    Emit("fcomip");
                    Emit("fstp %st(0)");
                    Emit("seta %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                }
                else {
                    throw new NotImplementedException();
                }

                Push(new Value {Kind = Value.ValueKind.Temp, Type = type});
            }

            public void LessThan(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare64("jb", "ja", "jb");
                        }
                        else {
                            GenCompare64("jl", "jg", "jb");
                        }
                    }
                    else {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare32("setb");
                        }
                        else {
                            GenCompare32("setl");
                        }
                    }
                }
                else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    GenCompare32("setb");
                }
                else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                         (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();
                    Emit("fcomip");
                    Emit("fstp %st(0)");
                    Emit("setb %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                }
                else {
                    throw new NotImplementedException();
                }

                Push(new Value {Kind = Value.ValueKind.Temp, Type = type});
            }

            public void GreatOrEqual(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare64("ja", "jb", "jae");
                        }
                        else {
                            GenCompare64("jg", "jl", "jae");
                        }
                    }
                    else {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare32("setae");
                        }
                        else {
                            GenCompare32("setge");
                        }
                    }
                }
                else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    GenCompare32("setae");
                }
                else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                         (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();
                    Emit("fcomip");
                    Emit("fstp %st(0)");
                    Emit("setae	%al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                }
                else {
                    throw new NotImplementedException();
                }

                Push(new Value {Kind = Value.ValueKind.Temp, Type = type});
            }

            public void LessOrEqual(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare64("jb", "ja", "jbe");
                        }
                        else {
                            GenCompare64("jl", "jg", "jbe");
                        }
                    }
                    else {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare32("setbe");
                        }
                        else {
                            GenCompare32("setle");
                        }
                    }
                }
                else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    GenCompare32("setbe");
                }
                else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                         (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();
                    Emit("fcomip");
                    Emit("fstp %st(0)");
                    Emit("setbe %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                }
                else {
                    throw new NotImplementedException();
                }

                Push(new Value {Kind = Value.ValueKind.Temp, Type = type});
            }

            public void Address(CType type) {
                var operand = Peek(0);

                switch (operand.Kind) {
                    case Value.ValueKind.Var:
                    case Value.ValueKind.Ref:
                        Emit($"leal {VarRefToAddrExpr(operand)}, %eax");
                        Emit("pushl %eax");
                        Pop();
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type});
                        break;
                    case Value.ValueKind.Address:
                        Pop();
                        Push(new Value {Kind = Value.ValueKind.Temp, Type = type});
                        break;
                    case Value.ValueKind.Temp:
                        // nothing
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            public void UnaryMinus(CType type) {
                var operand = Peek(0);
                if (operand.Type.IsIntegerType()) {
                    if (operand.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%edx");
                        Emit("negl	%eax");
                        Emit("adcl	$0, %edx");
                        Emit("negl	%edx");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                    }
                    else {
                        LoadI32("%eax");
                        Emit("negl %eax");
                        Emit("pushl %eax");
                    }

                    Push(new Value {Kind = Value.ValueKind.Temp, Type = type});
                }
                else if (operand.Type.IsRealFloatingType()) {
                    FpuPush();
                    Emit("fchs");
                    FpuPop(type);
                    //Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
                }
                else {
                    throw new NotImplementedException();
                }
            }

            public void UnaryBitNot(CType type) {
                var operand = Peek(0);
                if (operand.Type.IsIntegerType()) {
                    if (operand.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%edx");
                        Emit("notl	%eax");
                        Emit("notl	%edx");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                    }
                    else {
                        LoadI32("%eax");
                        Emit("notl %eax");
                        Emit("pushl %eax");
                    }

                    Push(new Value {Kind = Value.ValueKind.Temp, Type = type});
                }
                else {
                    throw new NotImplementedException();
                }
            }

            public void UnaryLogicalNot(CType type) {
                var operand = Peek(0);
                if (operand.Type.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                    LoadI64("%eax", "%edx");
                    Emit("orl %edx, %eax");
                    Emit("cmpl $0, %eax");
                    Emit("sete %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                }
                else {
                    LoadI32("%eax");
                    Emit("cmpl $0, %eax");
                    Emit("sete %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                }

                Push(new Value {Kind = Value.ValueKind.Temp, Type = type});
            }

            private void PrefixOp(CType type, string op) {
                if (type.IsIntegerType() || type.IsPointerType()) {
                    CType baseType;
                    int size;
                    if (type.IsPointerType(out baseType) && !baseType.IsVoidType()) {
                        size = baseType.Sizeof();
                    }
                    else {
                        size = 1;
                    }

                    // load address
                    LoadVariableAddress("%eax");

                    // load value
                    switch (type.Sizeof()) {
                        case 8:
                            var subop = (op == "add") ? "adc" : "sbb";
                            Emit($"{op}l ${size}, 0(%eax)");
                            Emit($"{subop}l $0, 4(%eax)");
                            Emit("pushl 4(%eax)");
                            Emit("pushl 0(%eax)");
                            break;
                        case 4:
                            Emit($"{op}l ${size}, (%eax)");
                            Emit("pushl (%eax)");
                            break;
                        case 2:
                            Emit($"{op}w ${size}, (%eax)");
                            if (type.IsSignedIntegerType()) {
                                Emit("movswl (%eax), %ecx");
                            }
                            else {
                                Emit("movzwl (%eax), %ecx");
                            }

                            Emit("pushl %ecx");
                            break;
                        case 1:
                            Emit($"{op}b ${size}, (%eax)");
                            if (type.IsSignedIntegerType()) {
                                Emit("movsbl (%eax), %ecx");
                            }
                            else {
                                Emit("movzbl (%eax), %ecx");
                            }

                            Emit("pushl %ecx");
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    Push(new Value {Kind = Value.ValueKind.Temp, Type = type});
                }
                else if (type.IsRealFloatingType()) {
                    LoadVariableAddress("%eax");
                    if (type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                        Emit("flds (%eax)");
                    }
                    else if (type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                        Emit("fldl (%eax)");
                    }
                    else {
                        throw new NotImplementedException();
                    }

                    Emit("fld1");
                    Emit($"f{op}p");
                    if (type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                        Emit("sub $4, %esp");
                        Emit("fsts (%esp)");
                        Emit("fstps (%eax)");
                    }
                    else if (type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                        Emit("sub $8, %esp");
                        Emit("fstl (%esp)");
                        Emit("fstpl (%eax)");
                    }
                    else {
                        throw new NotImplementedException();
                    }

                    Push(new Value {Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count});
                }
                else {
                    throw new NotImplementedException();
                }
            }

            public void PreInc(CType type) {
                PrefixOp(type, "add");
            }

            public void PreDec(CType type) {
                PrefixOp(type, "sub");
            }

            public void Reference(CType type) {
                LoadI32("%eax");
                Emit("pushl %eax");
                Push(new Value {Kind = Value.ValueKind.Address, Type = type});
            }

            public void Return(CType type) {
                if (type != null) {
                    var value = Peek(0);
                    if (value.Type.IsSignedIntegerType()) {
                        switch (value.Type.Sizeof()) {
                            case 1:
                                LoadI32("%eax");
                                Emit("movsbl %al, %eax");
                                break;
                            case 2:
                                LoadI32("%eax");
                                Emit("movswl %ax, %eax");
                                break;
                            case 4:
                                LoadI32("%eax");
                                break;
                            case 8:
                                LoadI64("%eax", "%edx");
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    else if (value.Type.IsUnsignedIntegerType()) {
                        switch (value.Type.Sizeof()) {
                            case 1:
                                LoadI32("%eax");
                                Emit("movzbl %al, %eax");
                                break;
                            case 2:
                                LoadI32("%eax");
                                Emit("movxwl %ax, %eax");
                                break;
                            case 4:
                                LoadI32("%eax");
                                break;
                            case 8:
                                LoadI64("%eax", "%edx");
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    else if (value.Type.IsRealFloatingType()) {
                        FpuPush();
                    }
                    else {
                        if (value.Type.Sizeof() <= 4) {
                            LoadI32("%eax");
                        }
                        else {
                            LoadVariableAddress("%esi");
                            Emit($"movl ${value.Type.Sizeof()}, %ecx");
                            Emit("movl 8(%ebp), %edi");
                            Emit("cld");
                            Emit("rep movsb");
                            if (value.Kind == Value.ValueKind.Temp) {
                                Emit($"addl {StackAlign(value.Type.Sizeof())}, %esp");
                            }
                        }
                    }
                }

                Emit("popl %edi");
                Emit("popl %esi");
                Emit("popl %ebx");
                Emit("movl %ebp, %esp");
                Emit("popl %ebp");
                Emit("ret");
            }

            public void Case(CType condType, long caseValue, string label) {
                if (condType.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                    var bytes = BitConverter.GetBytes(caseValue);
                    var lo = BitConverter.ToUInt32(bytes, 0);
                    var hi = BitConverter.ToUInt32(bytes, 4);
                    var lFalse = LabelAlloc();
                    Emit($"cmp ${lo}, %eax");
                    Emit($"jne {lFalse}");
                    Emit($"cmp ${hi}, %edx");
                    Emit($"jne {lFalse}");
                    Emit($"jmp {label}");
                    Label(lFalse);
                }
                else {
                    Emit($"cmp ${caseValue}, %eax");
                    Emit($"je {label}");
                }
            }

            public void Switch(CType condType, Action<CodeGenerator> p) {
                if (condType.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt)) {
                    LoadI64("%eax", "%edx");
                }
                else {
                    LoadI32("%eax");
                }

                p(this);
            }

            public void Data(string key, byte[] value) {
                Emit(".data");
                Emit($"{key}:");
                Emit(".byte " + String.Join(" ,", value.Select(x => x.ToString())));
            }

        }


        public readonly CodeGenerator Generator = new CodeGenerator();

        private readonly Stack<string> _continueTarget = new Stack<string>();
        private readonly Stack<string> _breakTarget = new Stack<string>();
        private Dictionary<string, int> _arguments;
        private Dictionary<string, string> _genericLabels;

        /// <summary>
        ///     文字列リテラルなどの静的データ
        /// </summary>
        public List<Tuple<string, byte[]>> DataBlock = new List<Tuple<string, byte[]>>();


        public Value OnArgumentDeclaration(SyntaxTree.Declaration.ArgumentDeclaration self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnFunctionDeclaration(SyntaxTree.Declaration.FunctionDeclaration self, Value value) {
            if (self.Body != null) {
                // 引数表
                var ft = self.Type as CType.FunctionType;
                int offset = 8; // prev return position

                // 戻り値領域へのポインタ
                if (!ft.ResultType.IsVoidType() && ft.ResultType.Sizeof() > 4 && (!ft.ResultType.IsRealFloatingType() && !ft.ResultType.IsBasicType(CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt))) {
                    offset += 4;
                }

                // 引数（先頭から）
                _arguments = new Dictionary<string, int>();
                var vars = new List<string>();
                foreach (var arg in ft.Arguments) {
                    vars.Add($"//   name={arg.Ident.Raw}, type={arg.Type.ToString()}, address={offset}(%ebp)");
                    _arguments.Add(arg.Ident.Raw, offset);
                    offset += CodeGenerator.StackAlign(arg.Type.Sizeof());
                }

                // ラベル
                _genericLabels = new Dictionary<string, string>();
                Generator.Emit("");
                Generator.Emit("// function: ");
                Generator.Emit($"//   {self.Ident}");
                Generator.Emit("// args: ");
                vars.ForEach(x => Generator.Emit(x));
                Generator.Emit("// return:");
                Generator.Emit($"//   {ft.ResultType.ToString()}");
                Generator.Emit("// location:");
                Generator.Emit($"//   {self.LocationRange}");
                Generator.Emit(".section .text");
                Generator.Emit($".globl {self.LinkageObject.LinkageId}");
                Generator.Emit($"{self.LinkageObject.LinkageId}:");
                Generator.Emit("pushl %ebp");
                Generator.Emit("movl %esp, %ebp");
                Generator.Emit("pushl %ebx");
                Generator.Emit("pushl %esi");
                Generator.Emit("pushl %edi");
                var c = Generator.Emit(".error \"Stack size is need backpatch.\""); // スタックサイズは仮置き
                _localScopeTotalSize = 4*3; // %ebx,%esi,%edi分
                _maxLocalScopeTotalSize = 4*3;
                self.Body.Accept(this, value);
                c.Body = $"subl ${_maxLocalScopeTotalSize - 4*3}, %esp"; // スタックサイズをバックパッチ
                Generator.Emit("popl %edi");
                Generator.Emit("popl %esi");
                Generator.Emit("popl %ebx");
                Generator.Emit("movl %ebp, %esp");
                Generator.Emit("popl %ebp");
                Generator.Emit("ret");
                Generator.Emit("");
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
                    if (_localScope.TryGetValue(self.Ident, out offset) == false) {
                        throw new Exception("初期化対象変数が見つからない。");
                    }

                    Generator.Emit($"// {self.LocationRange}");
                    return self.Init.Accept(this, new Value {Kind = Value.ValueKind.Var, Label = offset.Item1, Offset = offset.Item2, Type = self.Type});
                }
            }

            return value;
        }

        public Value OnAdditiveExpression(SyntaxTree.Expression.AdditiveExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);
            switch (self.Op) {
                case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add:
                    Generator.Add(self.Type);
                    break;
                case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub:
                    Generator.Sub(self.Type);
                    break;
            }

            return value;
        }

        public Value OnAndExpression(SyntaxTree.Expression.BitExpression.AndExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);
            Generator.And(self.Type);
            return value;
        }

        public Value OnCompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression self, Value value) {
            self.Lhs.Accept(this, value);
            Generator.Dup(0);
            self.Rhs.Accept(this, value);
            switch (self.Op) {
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.ADD_ASSIGN:
                    Generator.Add(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.SUB_ASSIGN:
                    Generator.Sub(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MUL_ASSIGN:
                    Generator.Mul(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.DIV_ASSIGN:
                    Generator.Div(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MOD_ASSIGN:
                    Generator.Mod(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.AND_ASSIGN:
                    Generator.And(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.OR_ASSIGN:
                    Generator.Or(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.XOR_ASSIGN:
                    Generator.Xor(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.LEFT_ASSIGN:
                    Generator.Shl(self.Type);
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.RIGHT_ASSIGN:
                    Generator.Shr(self.Type);
                    break;
                default:
                    throw new Exception("来ないはず");
            }

            Generator.Dup(1);
            Generator.Assign(self.Type);
            Generator.Discard();
            return value;
        }

        public Value OnSimpleAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, Value value) {
            self.Rhs.Accept(this, value);
            self.Lhs.Accept(this, value);

            Generator.Assign(self.Type);
            return value;
        }

        public Value OnCastExpression(SyntaxTree.Expression.CastExpression self, Value value) {
            self.Expr.Accept(this, value);
            Generator.CastTo(self.Type);
            return value;
        }

        public Value OnCommaExpression(SyntaxTree.Expression.CommaExpression self, Value value) {
            bool needDiscard = false;
            foreach (var e in self.Expressions) {
                if (needDiscard) {
                    Generator.Discard(); // スタック上の結果を捨てる
                }
                e.Accept(this, value);
                needDiscard = !e.Type.IsVoidType();
            }
            return value;
        }

        public Value OnConditionalExpression(SyntaxTree.Expression.ConditionalExpression self, Value value) {
            self.CondExpr.Accept(this, value);

            var elseLabel = Generator.LabelAlloc();
            var junctionLabel = Generator.LabelAlloc();

            Generator.JmpFalse(elseLabel);

            self.ThenExpr.Accept(this, value);
            if (self.Type.IsVoidType()) {
                Generator.Discard(); // スタック上の結果を捨てる
            }
            else {
                Generator.LoadValueToStack(self.Type);
                Generator.Pop();
            }

            Generator.Jmp(junctionLabel);
            Generator.Label(elseLabel);

            self.ElseExpr.Accept(this, value);
            if (self.Type.IsVoidType()) {
                // スタック上の結果を捨てる
                Generator.Discard();
            }
            else {
                Generator.LoadValueToStack(self.Type);
                Generator.Pop();
            }

            Generator.Label(junctionLabel);

            if (self.Type.IsVoidType()) {
                Generator.Push(new Value {Kind = Value.ValueKind.Void});
            }
            else {
                Generator.Push(new Value {Kind = Value.ValueKind.Temp, Type = self.Type});
            }

            return value;
        }

        public Value OnEqualityExpression(SyntaxTree.Expression.EqualityExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);
            switch (self.Op) {
                case SyntaxTree.Expression.EqualityExpression.OperatorKind.Equal:
                    Generator.Eq(self.Type);
                    break;
                case SyntaxTree.Expression.EqualityExpression.OperatorKind.NotEqual:
                    Generator.Ne(self.Type);
                    break;
                case SyntaxTree.Expression.EqualityExpression.OperatorKind.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return value;
        }

        public Value OnExclusiveOrExpression(SyntaxTree.Expression.BitExpression.ExclusiveOrExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);
            Generator.Xor(self.Type);
            return value;
        }

        public Value OnGccStatementExpression(SyntaxTree.Expression.GccStatementExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnInclusiveOrExpression(SyntaxTree.Expression.BitExpression.InclusiveOrExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);
            Generator.Or(self.Type);
            return value;
        }

        public Value OnIntegerPromotionExpression(SyntaxTree.Expression.IntegerPromotionExpression self, Value value) {
            self.Expr.Accept(this, value);
            Generator.CastTo(self.Type);
            return value;
        }

        public Value OnLogicalAndExpression(SyntaxTree.Expression.LogicalAndExpression self, Value value) {
            var labelFalse = Generator.LabelAlloc();
            var labelJunction = Generator.LabelAlloc();
            self.Lhs.Accept(this, value);
            Generator.JmpFalse(labelFalse);
            self.Rhs.Accept(this, value);
            Generator.JmpFalse(labelFalse);
            Generator.EmitLoadTrue();
            Generator.Jmp(labelJunction);
            Generator.Label(labelFalse);
            Generator.EmitLoadFalse();
            Generator.Label(labelJunction);
            Generator.Push(new Value {Kind = Value.ValueKind.Temp, Type = self.Type});
            return value;
        }

        public Value OnLogicalOrExpression(SyntaxTree.Expression.LogicalOrExpression self, Value value) {
            var labelTrue = Generator.LabelAlloc();
            var labelJunction = Generator.LabelAlloc();
            self.Lhs.Accept(this, value);
            Generator.JmpTrue(labelTrue);
            self.Rhs.Accept(this, value);
            Generator.JmpTrue(labelTrue);
            Generator.EmitLoadFalse();
            Generator.Jmp(labelJunction);
            Generator.Label(labelTrue);
            Generator.EmitLoadTrue();
            Generator.Label(labelJunction);
            Generator.Push(new Value {Kind = Value.ValueKind.Temp, Type = self.Type});
            return value;
        }

        public Value OnMultiplicitiveExpression(SyntaxTree.Expression.MultiplicitiveExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);
            switch (self.Op) {
                case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mul:
                    Generator.Mul(self.Type);
                    break;
                case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Div:
                    Generator.Div(self.Type);
                    break;
                case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mod:
                    Generator.Mod(self.Type);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return value;
        }

        public Value OnArraySubscriptingExpression(SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression self, Value value) {
            self.Target.Accept(this, value);
            self.Index.Accept(this, value);
            Generator.ArraySubscript(self.Type);
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
            var funcType = self.Expr.Type.GetBasePointerType().Unwrap() as CType.FunctionType;

            Generator.Call(self.Type, funcType, self.Args.Count, g => { self.Expr.Accept(this, value); }, (g, i) => { self.Args[i].Accept(this, value); });


            // 戻り値が構造体型/共用体型の場合、スタック上に配置すると不味いのでテンポラリ変数を確保してコピーする
            var obj = Generator.Peek(0);
            if (obj.Kind == Value.ValueKind.Temp && (obj.Type.IsStructureType() || obj.Type.IsUnionType())) {
                int size = CodeGenerator.StackAlign(obj.Type.Sizeof());
                _localScopeTotalSize += size;
                var ident = $"<temp:{_localScope.Count()}>";
                var tp = Tuple.Create((string)null, -_localScopeTotalSize);
                _localScope.Add(ident, tp);
                Generator.Emit($"// temp  : name={ident} address={-_localScopeTotalSize}(%ebp) type={obj.Type.ToString()}");

                if (size <= 4) {
                    Generator.Emit($"leal {-_localScopeTotalSize}(%ebp), %esi");
                    Generator.Emit("pop (%esi)");
                } else {
                    Generator.Emit($"movl %esp, %esi");
                    Generator.Emit($"addl ${size}, %esp");
                    Generator.Emit($"leal {-_localScopeTotalSize}(%ebp), %edi");
                    Generator.Emit($"movl ${size}, %ecx");
                    Generator.Emit("cld");
                    Generator.Emit("rep movsb");
                }
                Generator.Pop();
                Generator.Push(new Value { Kind = Value.ValueKind.Var, Type = obj.Type, Label = tp.Item1, Offset = tp.Item2 });

            }

            //return new Value() { kIND = self.Type.IsVoidType() ? Value.ValueKind.Void : Value.ValueKind.Temp, Type = self.Type };
            return value;
        }

        public Value OnMemberDirectAccess(SyntaxTree.Expression.PostfixExpression.MemberDirectAccess self, Value value) {
            self.Expr.Accept(this, value);
            Generator.DirectMember(self.Type, self.Ident.Raw);
            return value;
        }

        public Value OnMemberIndirectAccess(SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess self, Value value) {
            self.Expr.Accept(this, value);
            Generator.IndirectMember(self.Type, self.Ident.Raw);
            return value;
        }

        public Value OnUnaryPostfixExpression(SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression self, Value value) {
            self.Expr.Accept(this, value);
            switch (self.Op) {
                case SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Dec:
                    Generator.PostDec(self.Type);
                    break;
                case SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Inc:
                    Generator.PostInc(self.Type);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return value;
        }

        public Value OnCharacterConstant(SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant self, Value value) {
            Generator.Push(new Value {Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.Value});
            return value;
        }

        public Value OnFloatingConstant(SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant self, Value value) {
            Generator.Push(new Value {Kind = Value.ValueKind.FloatConst, Type = self.Type, FloatConst = self.Value});
            return value;
        }

        public Value OnIntegerConstant(SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant self, Value value) {
            Generator.Push(new Value {Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.Value});
            return value;
        }

        public Value OnEnclosedInParenthesesExpression(SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression self, Value value) {
            return self.ParenthesesExpression.Accept(this, value);
        }

        public Value OnAddressConstantExpression(SyntaxTree.Expression.PrimaryExpression.AddressConstantExpression self, Value value) {
            self.Identifier.Accept(this, value);
            Generator.CalcConstAddressOffset(self.Type, self.Offset.Value);
            return value;
        }

        public Value OnEnumerationConstant(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, Value value) {
            Generator.Push(new Value {Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.Info.Value});
            return value;
        }

        public Value OnFunctionExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, Value value) {
            Generator.Push(new Value {Kind = Value.ValueKind.Ref, Type = self.Type, Label = self.Decl.LinkageObject.LinkageId, Offset = 0});
            return value;
        }

        public Value OnUndefinedIdentifierExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnArgumentExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, Value value) {
            Generator.Push(new Value {Kind = Value.ValueKind.Var, Type = self.Type, Label = null, Offset = _arguments[self.Ident]});
            return value;
        }

        public Value OnVariableExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, Value value) {
            if (self.Decl.LinkageObject.Linkage == LinkageKind.NoLinkage) {
                Tuple<string, int> offset;
                if (_localScope.TryGetValue(self.Ident, out offset)) {
                    Generator.Push(new Value {Kind = Value.ValueKind.Var, Type = self.Type, Label = offset.Item1, Offset = offset.Item2});
                }
                else {
                    throw new Exception("");
                }
            }
            else {
                Generator.Push(new Value {Kind = Value.ValueKind.Var, Type = self.Type, Label = self.Decl.LinkageObject.LinkageId, Offset = 0});
            }

            return value;
        }

        public Value OnStringExpression(SyntaxTree.Expression.PrimaryExpression.StringExpression self, Value value) {
            int no = DataBlock.Count;
            var label = $"D{no}";
            DataBlock.Add(Tuple.Create(label, self.Value.ToArray()));
            Generator.Push(new Value {Kind = Value.ValueKind.Ref, Type = self.Type, Offset = 0, Label = label});
            return value;
        }

        public Value OnRelationalExpression(SyntaxTree.Expression.RelationalExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);
            switch (self.Op) {
                case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterThan:
                    Generator.GreatThan(self.Type);
                    break;
                case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessThan:
                    Generator.LessThan(self.Type);
                    break;
                case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterOrEqual:
                    Generator.GreatOrEqual(self.Type);
                    break;
                case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessOrEqual:
                    Generator.LessOrEqual(self.Type);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return value;
        }

        public Value OnShiftExpression(SyntaxTree.Expression.ShiftExpression self, Value value) {
            self.Lhs.Accept(this, value);
            self.Rhs.Accept(this, value);

            switch (self.Op) {
                case SyntaxTree.Expression.ShiftExpression.OperatorKind.Left:
                    Generator.Shl(self.Type);
                    break;
                case SyntaxTree.Expression.ShiftExpression.OperatorKind.Right:
                    Generator.Shr(self.Type);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return value;
        }

        public Value OnSizeofExpression(SyntaxTree.Expression.SizeofExpression self, Value value) {
            // todo: C99可変長配列型
            Generator.Push(new Value {Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.ExprOperand.Type.Sizeof()});
            return value;
        }

        public Value OnSizeofTypeExpression(SyntaxTree.Expression.SizeofTypeExpression self, Value value) {
            // todo: C99可変長配列型
            Generator.Push(new Value {Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.TypeOperand.Sizeof()});
            return value;
        }


        public Value OnTypeConversionExpression(SyntaxTree.Expression.TypeConversionExpression self, Value value) {
            self.Expr.Accept(this, value);
            Generator.CastTo(self.Type);
            return value;
        }

        public Value OnUnaryAddressExpression(SyntaxTree.Expression.UnaryAddressExpression self, Value value) {
            self.Expr.Accept(this, value);
            Generator.Address(self.Type);
            return value;
        }

        public Value OnUnaryMinusExpression(SyntaxTree.Expression.UnaryMinusExpression self, Value value) {
            self.Expr.Accept(this, value);
            Generator.UnaryMinus(self.Type);
            return value;
        }

        public Value OnUnaryNegateExpression(SyntaxTree.Expression.UnaryNegateExpression self, Value value) {
            self.Expr.Accept(this, value);
            Generator.UnaryBitNot(self.Type);
            return value;
        }

        public Value OnUnaryNotExpression(SyntaxTree.Expression.UnaryNotExpression self, Value value) {
            self.Expr.Accept(this, value);
            Generator.UnaryLogicalNot(self.Type);
            return value;
        }

        public Value OnUnaryPlusExpression(SyntaxTree.Expression.UnaryPlusExpression self, Value value) {
            self.Expr.Accept(this, value);
            return value;
        }

        public Value OnUnaryPrefixExpression(SyntaxTree.Expression.UnaryPrefixExpression self, Value value) {
            self.Expr.Accept(this, value);

            switch (self.Op) {
                case SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Inc:
                    Generator.PreInc(self.Type);
                    break;
                case SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Dec:
                    Generator.PreDec(self.Type);
                    break;
                default:
                    throw new Exception("来ないはず");
            }

            return value;
        }

        public Value OnUnaryReferenceExpression(SyntaxTree.Expression.UnaryReferenceExpression self, Value value) {
            self.Expr.Accept(this, value);
            Generator.Reference(self.Type);
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
            Generator.Push(value);
            Generator.Assign(self.Type);
            Generator.Discard();
            return value;
        }

        public Value OnArrayAssignInitializer(SyntaxTree.Initializer.ArrayAssignInitializer self, Value value) {
            var elementSize = self.Type.BaseType.Sizeof();
            var v = new Value(value) {Type = self.Type.BaseType};
            foreach (var init in self.Inits) {
                init.Accept(this, v);
                switch (v.Kind) {
                    case Value.ValueKind.Var:
                        if (v.Label == null) {
                            v.Offset += elementSize;
                        }
                        else {
                            throw new NotImplementedException();
                        }

                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            return new Value {Kind = Value.ValueKind.Void};
        }

        public Value OnStructUnionAssignInitializer(SyntaxTree.Initializer.StructUnionAssignInitializer self, Value value) {
            // value に初期化先変数位置が入っているので戦闘から順にvalueを適切に設定して再帰呼び出しすればいい。
            // 共用体は初期化式が一つのはず
            Value v = new Value(value);
            foreach (var member in self.Type.Members.Zip(self.Inits,Tuple.Create)) {
                member.Item2.Accept(this, v);
                v.Offset += member.Item1.Type.Sizeof();
            }
            return new Value {Kind = Value.ValueKind.Void};
        }

        public Value OnBreakStatement(SyntaxTree.Statement.BreakStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");
            var label = _breakTarget.Peek();
            Generator.Jmp(label);
            return new Value {Kind = Value.ValueKind.Void};
        }

        public Value OnCaseStatement(SyntaxTree.Statement.CaseStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");
            var label = _switchLabelTableStack.Peek()[self];
            Generator.Label(label);
            self.Stmt.Accept(this, value);
            return new Value {Kind = Value.ValueKind.Void};
        }

        private Scope<Tuple<string, int>> _localScope = Scope<Tuple<string, int>>.Empty;
        private int _localScopeTotalSize;
        private int _maxLocalScopeTotalSize;

        public Value OnCompoundStatement(SyntaxTree.Statement.CompoundStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");

            _localScope = _localScope.Extend();
            var prevLocalScopeSize = _localScopeTotalSize;

            Generator.Emit("// enter scope");
            foreach (var x in self.Decls.Reverse<SyntaxTree.Declaration>()) {
                if (x.LinkageObject.Linkage == LinkageKind.NoLinkage) {
                    if (x.StorageClass == StorageClassSpecifier.Static) {
                        // static
                        _localScope.Add(x.Ident, Tuple.Create(x.LinkageObject.LinkageId, 0));
                        Generator.Emit($"// static: name={x.Ident} linkid={x.LinkageObject.LinkageId} type={x.Type.ToString()}");
                    }
                    else {
                        _localScopeTotalSize += CodeGenerator.StackAlign(x.LinkageObject.Type.Sizeof());
                        _localScope.Add(x.Ident, Tuple.Create((string) null, -_localScopeTotalSize));
                        Generator.Emit($"// auto  : name={x.Ident} address={-_localScopeTotalSize}(%ebp) type={x.Type.ToString()}");
                    }
                }
                else if (x.LinkageObject.Linkage == LinkageKind.ExternalLinkage) {
                    Generator.Emit($"// extern: name={x.Ident} linkid={x.LinkageObject.LinkageId} type={x.Type.ToString()}");
                    // externなのでスキップ
                }
                else {
                    throw new NotImplementedException();
                }
            }

            //if (_maxLocalScopeTotalSize < _localScopeTotalSize) {
            //    _maxLocalScopeTotalSize = _localScopeTotalSize;
            //}

            foreach (var x in self.Decls) {
                x.Accept(this, value);
            }

            foreach (var x in self.Stmts) {
                x.Accept(this, value);
            }

            if (_maxLocalScopeTotalSize < _localScopeTotalSize) {
                _maxLocalScopeTotalSize = _localScopeTotalSize;
            }

            Generator.Emit("// leave scope");

            _localScopeTotalSize = prevLocalScopeSize;
            _localScope = _localScope.Parent;
            return value;
        }

        public Value OnContinueStatement(SyntaxTree.Statement.ContinueStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");
            var label = _continueTarget.Peek();
            Generator.Jmp(label);
            return new Value {Kind = Value.ValueKind.Void};
        }

        public Value OnDefaultStatement(SyntaxTree.Statement.DefaultStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");
            var label = _switchLabelTableStack.Peek()[self];
            Generator.Label(label);
            self.Stmt.Accept(this, value);
            return new Value {Kind = Value.ValueKind.Void};
        }

        public Value OnDoWhileStatement(SyntaxTree.Statement.DoWhileStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");
            var labelContinue = Generator.LabelAlloc();
            var labelBreak = Generator.LabelAlloc();

            // Check Loop Condition
            Generator.Label(labelContinue);
            _continueTarget.Push(labelContinue);
            _breakTarget.Push(labelBreak);
            self.Stmt.Accept(this, value);
            _continueTarget.Pop();
            _breakTarget.Pop();

            self.Cond.Accept(this, value);
            Generator.JmpTrue(labelContinue);
            Generator.Label(labelBreak);
            return new Value {Kind = Value.ValueKind.Void};
        }

        public Value OnEmptyStatement(SyntaxTree.Statement.EmptyStatement self, Value value) {
            return new Value {Kind = Value.ValueKind.Void};
            //throw new NotImplementedException();
        }

        public Value OnExpressionStatement(SyntaxTree.Statement.ExpressionStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");
            self.Expr.Accept(this, value);
            Generator.Discard();
            return value;
        }

        public Value OnForStatement(SyntaxTree.Statement.ForStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");
            // Initialize
            if (self.Init != null) {
                self.Init.Accept(this, value);
                Generator.Discard();
            }

            var labelHead = Generator.LabelAlloc();
            var labelContinue = Generator.LabelAlloc();
            var labelBreak = Generator.LabelAlloc();

            // Check Loop Condition
            Generator.Label(labelHead);
            if (self.Cond != null) {
                self.Cond.Accept(this, value);
                Generator.JmpFalse(labelBreak);
            }

            _continueTarget.Push(labelContinue);
            _breakTarget.Push(labelBreak);
            self.Stmt.Accept(this, value);
            _continueTarget.Pop();
            _breakTarget.Pop();

            Generator.Label(labelContinue);
            if (self.Update != null) {
                self.Update.Accept(this, value);
                Generator.Discard();
            }

            Generator.Jmp(labelHead);
            Generator.Label(labelBreak);

            return new Value {Kind = Value.ValueKind.Void};
        }

        public Value OnGenericLabeledStatement(SyntaxTree.Statement.GenericLabeledStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");
            if (_genericLabels.ContainsKey(self.Ident) == false) {
                _genericLabels[self.Ident] = Generator.LabelAlloc();
            }

            Generator.Label(_genericLabels[self.Ident]);
            self.Stmt.Accept(this, value);
            return new Value {Kind = Value.ValueKind.Void};
        }

        public Value OnGotoStatement(SyntaxTree.Statement.GotoStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");
            if (_genericLabels.ContainsKey(self.Label) == false) {
                _genericLabels[self.Label] = Generator.LabelAlloc();
            }

            Generator.Jmp(_genericLabels[self.Label]);
            return new Value {Kind = Value.ValueKind.Void};
        }

        public Value OnIfStatement(SyntaxTree.Statement.IfStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");
            self.Cond.Accept(this, value);
            if (self.ElseStmt != null) {
                var elseLabel = Generator.LabelAlloc();
                var junctionLabel = Generator.LabelAlloc();

                Generator.JmpFalse(elseLabel);

                self.ThenStmt.Accept(this, value);
                Generator.Jmp(junctionLabel);
                Generator.Label(elseLabel);
                self.ElseStmt.Accept(this, value);
                Generator.Label(junctionLabel);
            }
            else {
                var junctionLabel = Generator.LabelAlloc();

                Generator.JmpFalse(junctionLabel);

                self.ThenStmt.Accept(this, value);
                Generator.Label(junctionLabel);
            }


            return new Value {Kind = Value.ValueKind.Void};
        }

        public Value OnReturnStatement(SyntaxTree.Statement.ReturnStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");
            self.Expr?.Accept(this, value);
            Generator.Return(self.Expr?.Type);

            return new Value {Kind = Value.ValueKind.Void};
        }

        private readonly Stack<Dictionary<SyntaxTree.Statement, string>> _switchLabelTableStack = new Stack<Dictionary<SyntaxTree.Statement, string>>();

        public Value OnSwitchStatement(SyntaxTree.Statement.SwitchStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");
            var labelBreak = Generator.LabelAlloc();

            self.Cond.Accept(this, value);

            var labelDic = new Dictionary<SyntaxTree.Statement, string>();
            Generator.Switch(self.Cond.Type, g => {
                foreach (var caseLabel in self.CaseLabels) {
                    var caseValue = caseLabel.Value;
                    var label = Generator.LabelAlloc();
                    labelDic.Add(caseLabel, label);
                    Generator.Case(self.Cond.Type, caseValue, label);
                }
            });
            if (self.DefaultLabel != null) {
                var label = Generator.LabelAlloc();
                labelDic.Add(self.DefaultLabel, label);
                Generator.Jmp(label);
            }
            else {
                Generator.Jmp(labelBreak);
            }

            _switchLabelTableStack.Push(labelDic);
            _breakTarget.Push(labelBreak);
            self.Stmt.Accept(this, value);
            _breakTarget.Pop();
            _switchLabelTableStack.Pop();
            Generator.Label(labelBreak);

            return new Value {Kind = Value.ValueKind.Void};
        }

        public Value OnWhileStatement(SyntaxTree.Statement.WhileStatement self, Value value) {
            Generator.Emit($"// {self.LocationRange}");
            var labelContinue = Generator.LabelAlloc();
            var labelBreak = Generator.LabelAlloc();

            // Check Loop Condition
            Generator.Label(labelContinue);
            self.Cond.Accept(this, value);
            Generator.JmpFalse(labelBreak);
            _continueTarget.Push(labelContinue);
            _breakTarget.Push(labelBreak);
            self.Stmt.Accept(this, value);
            _continueTarget.Pop();
            _breakTarget.Pop();

            Generator.Jmp(labelContinue);
            Generator.Label(labelBreak);

            return new Value {Kind = Value.ValueKind.Void};
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

            foreach (var data in DataBlock) {
                Generator.Data(data.Item1, data.Item2);
            }

            return value;
        }

        public void WriteCode(StreamWriter writer) {
            Generator.Codes.ForEach(x => writer.WriteLine(x.ToString()));
        }
    }

    public class FileScopeInitializerVisitor : SyntaxTreeVisitor.IVisitor<SyntaxTreeCompileVisitor.Value, SyntaxTreeCompileVisitor.Value> {
        private SyntaxTreeCompileVisitor syntaxTreeCompileVisitor;

        public void Emit(string s) {
            syntaxTreeCompileVisitor.Generator.Emit(s);
        }

        public List<Tuple<int, int, int, SyntaxTree.Expression>> Values { get; } = new List<Tuple<int, int, int, SyntaxTree.Expression>>();
        public int CurrentOffset = 0;

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
                Emit(".section .data");
                self.Init.Accept(this, value);
                Emit(".align 4");
                Emit($"{self.LinkageObject.LinkageId}:");
                foreach (var val in Values) {
                    var byteOffset = val.Item1;
                    var bitOffset = val.Item2;
                    var bitSize   = val.Item3;
                    var cvalue = val.Item4.Accept(this, value);
                    switch (cvalue.Kind) {
                        case SyntaxTreeCompileVisitor.Value.ValueKind.IntConst:
                            switch (cvalue.Type.Sizeof()) {
                                case 1:
                                    Emit($".byte {(byte) cvalue.IntConst}");
                                    break;
                                case 2:
                                    Emit($".word {(ushort) cvalue.IntConst}");
                                    break;
                                case 4:
                                    Emit($".long {(uint) cvalue.IntConst}");
                                    break;
                                case 8:
                                    Emit($".long {(UInt64) cvalue.IntConst & 0xFFFFFFFFUL}, {(UInt64) (cvalue.IntConst >> 32)  & 0xFFFFFFFFUL}");
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            break;
                        case SyntaxTreeCompileVisitor.Value.ValueKind.FloatConst:
                            switch (cvalue.Type.Sizeof()) {
                                case 4: {
                                    var dwords = BitConverter.ToUInt32(BitConverter.GetBytes((float) cvalue.FloatConst), 0);
                                    Emit($".long {dwords}");
                                    break;
                                }
                                case 8: {
                                    var lo = BitConverter.ToUInt32(BitConverter.GetBytes((double) cvalue.FloatConst), 0);
                                    var hi = BitConverter.ToUInt32(BitConverter.GetBytes((double) cvalue.FloatConst), 4);

                                    Emit($".long {lo}, {hi}");
                                    break;
                                }
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }

                            break;
                        case SyntaxTreeCompileVisitor.Value.ValueKind.Var:
                        case SyntaxTreeCompileVisitor.Value.ValueKind.Ref:
                            if (cvalue.Label == null) {
                                throw new Exception("ファイルスコープオブジェクトの参照では無い。");
                            }

                            Emit($".long {cvalue.Label}+{cvalue.Offset}");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            else {
                Emit(".section .bss");
                Emit(".align 4");
                Emit($".comm {self.LinkageObject.LinkageId}, {self.LinkageObject.Type.Sizeof()}");
            }

            return value;
        }

        public SyntaxTreeCompileVisitor.Value OnAdditiveExpression(SyntaxTree.Expression.AdditiveExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnAndExpression(SyntaxTree.Expression.BitExpression.AndExpression self, SyntaxTreeCompileVisitor.Value value) {
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

        public SyntaxTreeCompileVisitor.Value OnExclusiveOrExpression(SyntaxTree.Expression.BitExpression.ExclusiveOrExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnGccStatementExpression(SyntaxTree.Expression.GccStatementExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnInclusiveOrExpression(SyntaxTree.Expression.BitExpression.InclusiveOrExpression self, SyntaxTreeCompileVisitor.Value value) {
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
            return new SyntaxTreeCompileVisitor.Value {Kind = SyntaxTreeCompileVisitor.Value.ValueKind.IntConst, IntConst = self.Value, Type = self.Type};
        }

        public SyntaxTreeCompileVisitor.Value OnFloatingConstant(SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant self, SyntaxTreeCompileVisitor.Value value) {
            return new SyntaxTreeCompileVisitor.Value {Kind = SyntaxTreeCompileVisitor.Value.ValueKind.FloatConst, FloatConst = self.Value, Type = self.Type};
        }

        public SyntaxTreeCompileVisitor.Value OnIntegerConstant(SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant self, SyntaxTreeCompileVisitor.Value value) {
            return new SyntaxTreeCompileVisitor.Value {Kind = SyntaxTreeCompileVisitor.Value.ValueKind.IntConst, IntConst = self.Value, Type = self.Type};
        }

        public SyntaxTreeCompileVisitor.Value OnEnclosedInParenthesesExpression(SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
        }

        public SyntaxTreeCompileVisitor.Value OnAddressConstantExpression(SyntaxTree.Expression.PrimaryExpression.AddressConstantExpression self, SyntaxTreeCompileVisitor.Value value) {
            if (self.Identifier is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression) {
                var f = self.Identifier as SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression;
                return new SyntaxTreeCompileVisitor.Value {Kind = SyntaxTreeCompileVisitor.Value.ValueKind.Ref, Label = f.Decl.LinkageObject.LinkageId, Offset = (int) self.Offset.Value, Type = self.Type};
            }

            if (self.Identifier is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression) {
                var f = self.Identifier as SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression;
                return new SyntaxTreeCompileVisitor.Value { Kind = SyntaxTreeCompileVisitor.Value.ValueKind.Ref, Label = f.Decl.LinkageObject.LinkageId, Offset = (int)self.Offset.Value, Type = self.Type };
            }
            if (self.Identifier == null) {
                return new SyntaxTreeCompileVisitor.Value { Kind = SyntaxTreeCompileVisitor.Value.ValueKind.IntConst, IntConst = (int)self.Offset.Value, Type = self.Type };
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
            int no = syntaxTreeCompileVisitor.DataBlock.Count;
            var label = $"D{no}";
            syntaxTreeCompileVisitor.DataBlock.Add(Tuple.Create(label, self.Value.ToArray()));
            return new SyntaxTreeCompileVisitor.Value {Kind = SyntaxTreeCompileVisitor.Value.ValueKind.Ref, Label = label, Offset = 0, Type = self.Type};
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
            if (self.Type.IsBitField()) {
                var bft = self.Type as CType.BitFieldType;
                if (ret is SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant) {
                    Values.Add(Tuple.Create(CurrentOffset, bft.BitOffset, bft.BitWidth, ret));
                    if (bft.BitOffset + bft.BitWidth == bft.Sizeof() * 8) {
                        CurrentOffset += bft.Sizeof();
                    }
                } else {
                    throw new Exception("ビットフィールドに代入できない値が使われている。");
                }
            } else {
                Values.Add(Tuple.Create(CurrentOffset, -1, -1, ret));
                CurrentOffset += self.Type.Sizeof();
            }
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
                    Values.Add(Tuple.Create(CurrentOffset, -1, -1,(SyntaxTree.Expression)new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, CType.BasicType.TypeKind.UnsignedLongInt)));
                    CurrentOffset += 4;
                    filledSize -= 4;
                }
                else if (filledSize >= 2) {
                    Values.Add(Tuple.Create(CurrentOffset, -1, -1, (SyntaxTree.Expression)new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, CType.BasicType.TypeKind.UnsignedShortInt)));
                    CurrentOffset += 2;
                    filledSize -= 2;
                }
                else if (filledSize >= 1) {
                    Values.Add(Tuple.Create(CurrentOffset, -1, -1, (SyntaxTree.Expression)new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, CType.BasicType.TypeKind.UnsignedChar)));
                    CurrentOffset += 1;
                    filledSize -= 1;
                }
            }

            return value;
        }

        public SyntaxTreeCompileVisitor.Value OnStructUnionAssignInitializer(SyntaxTree.Initializer.StructUnionAssignInitializer self, SyntaxTreeCompileVisitor.Value value) {
            var start = Values.Count;

            foreach (var s in self.Inits) {
                s.Accept(this, value);
            }


            var suType = self.Type.Unwrap() as CType.TaggedType.StructUnionType;
            if (suType.IsStructureType()) {
                foreach (var x in suType.Members.Skip(self.Inits.Count)) {
                    if (x.Type.IsBitField()) {
                        var bft = x.Type as CType.BitFieldType;
                        var bt = bft.Type as CType.BasicType;
                        Values.Add(Tuple.Create(CurrentOffset, bft.BitOffset, bft.BitWidth, (SyntaxTree.Expression)new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, bt.Kind)));
                        if (bft.BitOffset + bft.BitWidth == bft.Sizeof() * 8) {
                            CurrentOffset += bft.Sizeof();
                        }
                    } else {
                        var fillSize = x.Type.Sizeof();
                        while (fillSize > 0) {
                            if (fillSize >= 4) {
                                fillSize -= 4;
                                Values.Add(Tuple.Create(CurrentOffset, -1, -1, (SyntaxTree.Expression)new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, CType.BasicType.TypeKind.UnsignedLongInt)));
                                CurrentOffset += 4;
                            } else if (fillSize >= 2) {
                                fillSize -= 2;
                                Values.Add(Tuple.Create(CurrentOffset, -1, -1, (SyntaxTree.Expression)new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, CType.BasicType.TypeKind.UnsignedShortInt)));
                                CurrentOffset += 4;
                            } else if (fillSize >= 1) {
                                fillSize -= 1;
                                Values.Add(Tuple.Create(CurrentOffset, -1, -1, (SyntaxTree.Expression)new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, CType.BasicType.TypeKind.UnsignedChar)));
                                CurrentOffset += 4;
                            }
                        }
                    }
                }
            }

            var end = Values.Count;

            var i = start;
            while (i < Values.Count) {
                var val = Values[i];
                var byteOffset = val.Item1;
                var bitOffset = val.Item2;
                var bitSize = val.Item3;
                var expr = val.Item4;
                if (bitSize == -1) {
                    i++;
                    continue;
                }
                ulong v = 0;

                while (i < Values.Count && Values[i].Item1 == byteOffset) {
                    var cvalue = Values[i].Item4.Accept(this, value);
                    var bOffset = Values[i].Item2;
                    var bSize = Values[i].Item3;
                    if (cvalue.Kind != SyntaxTreeCompileVisitor.Value.ValueKind.IntConst) {
                        // ビットフィールド中に定数式以外が使われている。
                        throw new Exception("ビットフィールドに対応する初期化子の要素が定数ではありません。");
                    }
                    ulong bits = 0;
                    switch (cvalue.Type.Sizeof()) {
                        case 1:
                            if (cvalue.Type.IsUnsignedIntegerType()) {
                                bits = (ulong)(((byte)(((sbyte)cvalue.IntConst) << (8 - bSize))) >> (8 - bSize));
                            } else {
                                bits = (ulong)(((byte)(((byte)cvalue.IntConst) << (8 - bSize))) >> (8 - bSize));
                            }
                            break;
                        case 2:
                            if (cvalue.Type.IsUnsignedIntegerType()) {
                                bits = (ulong)(((ushort)(((short)cvalue.IntConst) << (16 - bSize))) >> (16 - bSize));
                            } else {
                                bits = (ulong)(((ushort)(((ushort)cvalue.IntConst) << (16 - bSize))) >> (16 - bSize));
                            }
                            break;
                        case 4:
                            if (cvalue.Type.IsUnsignedIntegerType()) {
                                bits = (ulong)(((uint)(((int)cvalue.IntConst) << (32 - bSize))) >> (32 - bSize));
                            } else {
                                bits = (ulong)(((uint)(((uint)cvalue.IntConst) << (32 - bSize))) >> (32 - bSize));
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    v |= bits << bOffset;
                    Values.RemoveAt(i);
                }
                Values.Insert(i, Tuple.Create(byteOffset, -1, -1, (SyntaxTree.Expression)new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, v.ToString(), (long)v, (expr.Type as CType.BasicType).Kind)));

                i++;
                
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

