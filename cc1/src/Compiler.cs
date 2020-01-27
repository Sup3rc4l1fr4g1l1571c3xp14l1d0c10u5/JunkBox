using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnsiCParser.DataType;
using AnsiCParser.SyntaxTree;

namespace AnsiCParser {
    public class Compiler {


        /// <summary>
        /// コード生成時の値(計算結果)を示すオブジェクト
        /// </summary>
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

            /// <summary>
            /// コンストラクタ
            /// </summary>
            public Value() {
            }


            /// <summary>
            /// コピーコンストラクタ
            /// </summary>
            /// <param name="ret"></param>
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
        /// コード生成器(i386向け)
        /// </summary>
        protected class CodeGenerator {
            public class Code {
                public string Body {
                    get; set;
                }

                public Code(string body) {
                    Body = body;
                }

                public override string ToString() {
                    return Body;
                }
            }

            /// <summary>
            /// continue命令の移動先ラベルが格納されるスタック
            /// </summary>
            public readonly Stack<string> ContinueTarget = new Stack<string>();

            /// <summary>
            /// break命令の移動先ラベルが格納されるスタック
            /// </summary>
            public readonly Stack<string> BreakTarget = new Stack<string>();

            /// <summary>
            /// 現在の関数の引数とスタック位置を示す辞書
            /// </summary>
            public readonly Dictionary<string, int> Arguments = new Dictionary<string, int>();

            /// <summary>
            /// 現在の関数中の汎用ラベルを示す辞書
            /// </summary>
            public readonly Dictionary<string, string> GenericLabels = new Dictionary<string, string>();

            /// <summary>
            /// 文字列リテラルなどの静的データ
            /// </summary>
            public readonly List<Tuple<string, byte[]>> DataBlock = new List<Tuple<string, byte[]>>();

            public List<Code> Codes { get; } = new List<Code>();

            private readonly Stack<Value> _stack = new Stack<Value>();

            public int StackSIze {
                get {
                    return _stack.Count;
                }
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

            public int GetStackDepth() {
                return _stack.Count();
            }
            public void CheckStackDepth(int i) {
                System.Diagnostics.Debug.Assert(i == _stack.Count());
            }


            public void Discard() {
                Value v = Pop();
                if (v.Kind == Value.ValueKind.Temp) {
                    Emit($"addl ${StackAlign(v.Type.Sizeof())}, %esp"); // discard temp value
                } else if (v.Kind == Value.ValueKind.Address) {
                    Emit("addl $4, %esp"); // discard pointer
                }
            }

            public void Dup(int index) {
                var sdp = GetStackDepth();
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
                        } else {
                            Emit($"leal {skipsize}(%esp), %esi");
                            Emit($"leal {-size}(%esp), %esp");
                            Emit("movl %esp, %edi");
                            Emit($"movl ${size}, %ecx");
                            Emit("cld");
                            Emit("rep movsb");
                        }

                        Push(new Value { Kind = Value.ValueKind.Temp, Type = v.Type, StackPos = _stack.Count });
                    } else if (v.Kind == Value.ValueKind.Address) {
                        Emit($"leal {skipsize}(%esp), %esi");
                        Emit("push (%esi)");
                        Push(new Value { Kind = Value.ValueKind.Address, Type = v.Type, StackPos = _stack.Count });
                    } else {
                        throw new Exception();
                    }
                } else {
                    Push(v);
                }
                CheckStackDepth(sdp + 1);
            }

            public void Add(CType type) {
                var sdp = GetStackDepth();
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("addl %eax, %ecx");
                        Emit("adcl %ebx, %edx");
                        Emit("pushl %edx");
                        Emit("pushl %ecx");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        Emit("addl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush(); // rhs
                    FpuPush(); // lhs
                    Emit("faddp");
                    FpuPop(type);
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsIntegerType()) {
                    CType elemType = type.GetBasePointerType();

                    if (rhs.Kind == Value.ValueKind.IntConst && lhs.Kind == Value.ValueKind.Ref) {
                        Pop();
                        Pop();
                        Push(new Value { Kind = Value.ValueKind.Ref, Type = type, Label = lhs.Label, Offset = (int)(lhs.Offset + rhs.IntConst * elemType.Sizeof()) });
                    } else {
                        if (rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                            LoadI64("%ecx", "%edx"); // rhs(loのみ使う)
                        } else {
                            LoadI32("%ecx"); // rhs = index
                        }

                        LoadPointer("%eax"); // lhs = base
                        Emit($"imull ${elemType.Sizeof()}, %ecx, %ecx"); // index *= sizeof(base[0])
                        Emit("addl %ecx, %eax"); // base += index
                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else if (lhs.Type.IsIntegerType() && rhs.Type.IsPointerType()) {
                    CType elemType = type.GetBasePointerType();

                    if (lhs.Kind == Value.ValueKind.IntConst && rhs.Kind == Value.ValueKind.Ref) {
                        Pop();
                        Pop();
                        Push(new Value { Kind = Value.ValueKind.Ref, Type = type, Label = rhs.Label, Offset = (int)(rhs.Offset + lhs.IntConst * elemType.Sizeof()) });
                    } else {
                        LoadPointer("%ecx"); // rhs = base
                        if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                            LoadI64("%eax", "%edx"); // lhs(loのみ使う)
                        } else {
                            LoadI32("%eax"); // lhs = index
                        }

                        Emit($"imull ${elemType.Sizeof()}, %ecx, %ecx"); // index *= sizeof(base[0])
                        Emit("addl %ecx, %eax"); // base += index
                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else if (lhs.Type.IsComplexType() && rhs.Type.IsComplexType()) {
                    FpuPush();
                    FpuPush();

                    Emit("faddp %st(0), %st(2)");
                    Emit("faddp %st(0), %st(2)");

                    FpuPop(type);
                } else if (lhs.Type.IsRealFloatingType() && rhs.Type.IsComplexType()) {
                    FpuPush();  // rhs
                    FpuPush();  // lhs
                    FpuPushZero();

                    Emit("faddp %st(0), %st(2)");
                    Emit("faddp %st(0), %st(2)");

                    FpuPop(type);
                } else if (lhs.Type.IsComplexType() && rhs.Type.IsRealFloatingType()) {
                    FpuPush();  // rhs
                    FpuPushZero();
                    FpuPush();  // lhs 

                    Emit("faddp %st(0), %st(2)");
                    Emit("faddp %st(0), %st(2)");

                    FpuPop(type);
                } else {
                    throw new Exception("");
                }
                CheckStackDepth(sdp - 1);
            }

            public void Sub(CType type) {
                var sdp = GetStackDepth();
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("subl %eax, %ecx");
                        Emit("sbbl %ebx, %edx");
                        Emit("pushl %edx");
                        Emit("pushl %ecx");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        Emit("subl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush(); // rhs
                    FpuPush(); // lhs
                    Emit("fsubp");
                    FpuPop(type);
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsIntegerType()) {
                    CType elemType = type.GetBasePointerType();

                    if (rhs.Kind == Value.ValueKind.IntConst && lhs.Kind == Value.ValueKind.Ref) {
                        Pop();
                        Pop();
                        Push(new Value { Kind = Value.ValueKind.Ref, Type = type, Label = lhs.Label, Offset = (int)(lhs.Offset - rhs.IntConst * elemType.Sizeof()) });
                    } else {
                        if (rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                            LoadI64("%ecx", "%edx"); // rhs(loのみ使う)
                        } else {
                            LoadI32("%ecx"); // rhs = index
                        }

                        LoadPointer("%eax"); // lhs = base
                        Emit($"imull ${elemType.Sizeof()}, %ecx, %ecx"); // index *= sizeof(base[0])
                        Emit("subl %ecx, %eax"); // base += index
                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else if (lhs.Type.IsIntegerType() && rhs.Type.IsPointerType()) {
                    CType elemType = type.GetBasePointerType();

                    if (lhs.Kind == Value.ValueKind.IntConst && rhs.Kind == Value.ValueKind.Ref) {
                        Pop();
                        Pop();
                        Push(new Value { Kind = Value.ValueKind.Ref, Type = type, Label = rhs.Label, Offset = (int)(rhs.Offset - lhs.IntConst * elemType.Sizeof()) });
                    } else {
                        LoadPointer("%ecx"); // rhs = base
                        if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                            LoadI64("%eax", "%edx"); // lhs(loのみ使う)
                        } else {
                            LoadI32("%eax"); // lhs = index
                        }

                        Emit($"imull ${elemType.Sizeof()}, %ecx, %ecx"); // index *= sizeof(base[0])
                        Emit("subl %ecx, %eax"); // base += index
                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    CType elemType = lhs.Type.GetBasePointerType();

                    if (lhs.Kind == Value.ValueKind.Ref && rhs.Kind == Value.ValueKind.Ref && lhs.Label == rhs.Label) {
                        Pop();
                        Pop();
                        Push(new Value { Kind = Value.ValueKind.IntConst, Type = CType.CreatePtrDiffT(), IntConst = ((lhs.Offset - rhs.Offset) / elemType.Sizeof()) });
                    } else {
                        // (rhs - lhs) / sizeof(elemType)
                        LoadPointer("%ecx"); // rhs = ptr
                        LoadPointer("%eax"); // lhs = ptr
                        Emit("subl %ecx, %eax");
                        Emit("cltd");
                        Emit($"movl ${elemType.Sizeof()}, %ecx");
                        Emit("idivl %ecx");
                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else if (lhs.Type.IsComplexType() && rhs.Type.IsComplexType()) {
                    FpuPush();    // rhs [c + di]
                    FpuPush();    // lhs [a + bi]

                    // %st(0) = b
                    // %st(1) = a
                    // %st(2) = d
                    // %st(3) = c 

                    Emit("fsubp %st(0), %st(2)");   // [a,b-d,c]
                    Emit("fsubp %st(0), %st(2)");   // [b-d,a-c]

                    FpuPop(type);
                } else if (lhs.Type.IsRealFloatingType() && rhs.Type.IsComplexType()) {
                    FpuPush();    // rhs [c + di]
                    FpuPush();    // lhs [a]
                    FpuPushZero();// b = 0

                    // %st(0) = b
                    // %st(1) = a
                    // %st(2) = d
                    // %st(3) = c 

                    Emit("fsubp %st(0), %st(2)");
                    Emit("fsubp %st(0), %st(2)");

                    FpuPop(type);
                } else if (lhs.Type.IsComplexType() && rhs.Type.IsRealFloatingType()) {
                    FpuPush();  // rhs
                    FpuPushZero();
                    FpuPush();  // lhs 

                    Emit("fsubp %st(0), %st(2)");
                    Emit("fsubp %st(0), %st(2)");

                    FpuPop(type);
                } else {
                    throw new Exception("");
                }
                CheckStackDepth(sdp - 1);
            }

            public void Mul(CType type) {
                var sdp = GetStackDepth();
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
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

                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        if (type.IsUnsignedIntegerType()) {
                            Emit("mull %ecx");
                        } else {
                            Emit("imull %ecx");
                        }

                        Emit("push %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();

                    Emit("fmulp");

                    FpuPop(type);
                } else if (lhs.Type.IsComplexType() && rhs.Type.IsComplexType()) {
                    FpuPush();    // rhs [c + di]
                    FpuPush();    // lhs [a + bi]

                    // %st(0) = b
                    // %st(1) = a
                    // %st(2) = d
                    // %st(3) = c 

                    // [a+bi]*[c+di] = (a×c)-(b×d)+(a×d+b×c)i

                    Emit("fld %st(1)");             // [a,b,a,d,c]
                    Emit("fmul %st(4), %st(0)");    // [a*c,b,a,d,c]
                    Emit("fld %st(1)");             // [b,a*c,b,a,d,c]
                    Emit("fmul %st(4), %st(0)");    // [b*d,a*c,b,a,d,c]
                    Emit("fsubrp %st(0), %st(1)");  // [(a*c)-(b*d),b,a,d,c]
                    Emit("fld %st(2)");             // [a,(a*c)-(b*d),b,a,d,c]
                    Emit("fmul %st(4),  %st(0)");   // [a*d,(a*c)-(b*d),b,a,d,c]
                    Emit("fld %st(2)");             // [b,a*d,(a*c)-(b*d),b,a,d,c]
                    Emit("fmul %st(6),  %st(0)");   // [b*c,a*d,(a*c)-(b*d),b,a,d,c]
                    Emit("faddp");                  // [(a*d)+(b*c),(a*c)-(b*d),b,a,d,c]

                    FpuPop(type);
                    FpuDiscard();
                    FpuDiscard();
                    FpuDiscard();
                    FpuDiscard();
                } else if (lhs.Type.IsRealFloatingType() && rhs.Type.IsComplexType()) {
                    FpuPush();  // rhs
                    FpuPush();  // lhs
                    Emit("fld %st(0)");

                    Emit("fmulp %st(0), %st(2)");
                    Emit("fmulp %st(0), %st(2)");

                    FpuPop(type);
                } else if (lhs.Type.IsComplexType() && rhs.Type.IsRealFloatingType()) {
                    FpuPush();  // rhs
                    Emit("fld %st(0)");
                    FpuPush();  // lhs 

                    Emit("fmulp %st(0), %st(2)");
                    Emit("fmulp %st(0), %st(2)");

                    FpuPop(type);
                } else {
                    throw new NotImplementedException();
                }
                CheckStackDepth(sdp - 1);
            }

            public void Div(CType type) {
                var sdp = GetStackDepth();
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (type.IsBasicType(BasicType.TypeKind.SignedLongLongInt)) {
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
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else if (type.IsBasicType(BasicType.TypeKind.UnsignedLongLongInt)) {
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
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        if (type.IsUnsignedIntegerType()) {
                            Emit("movl $0, %edx");
                            Emit("divl %ecx");
                        } else {
                            Emit("cltd");
                            Emit("idivl %ecx");
                        }

                        Emit("push %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();

                    Emit("fdivp");

                    FpuPop(type);
                } else if (lhs.Type.IsComplexType() && rhs.Type.IsComplexType()) {
                    FpuPush();    // rhs [c + di]
                    FpuPush();    // lhs [a + bi]

                    // %st(0) = b
                    // %st(1) = a
                    // %st(2) = d
                    // %st(3) = c 

                    // ((a×c+b×d)+(b×c-a×d)i) / (c*c+d*d)

                    Emit("fld %st(3)");             // [c,b,a,d,c]
                    Emit("fmul %st(2), %st(0)");    // [a*c,b,a,d,c]
                    Emit("fld %st(3)");             // [d,a*c,b,a,d,c]
                    Emit("fmul %st(2), %st(0)");    // [b*d,a*c,b,a,d,c]
                    Emit("faddp");                  // [(a*c)+(b*d),b,a,d,c]
                    Emit("fxch %st(2)");            // [a,b,(a*c)+(b*d),d,c]
                    Emit("fmul %st(3), %st(0)");    // [a*d,b,(a*c)+(b*d),d,c]
                    Emit("fxch %st(1)");            // [b,a*d,(a*c)+(b*d),d,c]
                    Emit("fmul %st(4), %st(0)");    // [b*c,a*d,(a*c)+(b*d),d,c]
                    Emit("fsubp");                  // [(b*c)-(a*d),(a*c)+(b*d),d,c]
                    Emit("fxch %st(2)");            // [d,(a*c)+(b*d),(b*c)-(a*d),c]
                    Emit("fmul %st(0), %st(0)");    // [d*d,(a*c)+(b*d),(b*c)-(a*d),c]
                    Emit("fxch %st(3)");            // [c,(a*c)+(b*d),(b*c)-(a*d),d*d]
                    Emit("fmul %st(0), %st(0)");    // [c*c,(a*c)+(b*d),(b*c)-(a*d),d*d]
                    Emit("faddp %st(0), %st(3)");   // [(a*c)+(b*d),(b*c)-(a*d),(c*c)+(d*d)]
                    Emit("fxch %st(2)");            // [(c*c)+(d*d),(b*c)-(a*d),(a*c)+(b*d)]
                    Emit("fdivr %st(0), %st(2)");   // [(c*c)+(d*d),(b*c)-(a*d),((a*c)+(b*d))/((c*c)+(d*d))]
                    Emit("fdivrp %st(0), %st(1)");  // [((b*c)-(a*d))/((c*c)+(d*d)),((a*c)+(b*d))/((c*c)+(d*d))]

                    FpuPop(type);
                } else if (lhs.Type.IsRealFloatingType() && rhs.Type.IsComplexType()) {
                    FpuPush();    // rhs [c + di]
                    FpuPush();    // lhs a

                    // %st(0) = a
                    // %st(1) = d
                    // %st(2) = c 

                    // (a×c)-(a×d)i / (c*c+d*d)
                    Emit("fld %st(2)");             // [c,a,d,c]
                    Emit("fmul %st(0),  %st(0)");   // [c*c,a,d,c]
                    Emit("fld %st(2)");             // [d,c*c,a,d,c]
                    Emit("fmul %st(0),  %st(0)");   // [d*d,c*c,a,d,c]
                    Emit("faddp");                  // [d*d+c*c,a,d,c]
                    Emit("fxch %st(1)");            // [a,d*d+c*c,d,c]
                    Emit("fmul %st(0),  %st(3)");   // [a,d*d+c*c,d,a*c]
                    Emit("fchs");                   // [-a,d*d+c*c,d,a*c]
                    Emit("fmulp %st(0),  %st(2)");  // [d*d+c*c,d*-a,a*c]

                    Emit("fdivr %st(0), %st(2)");    // [d*d+c*c,d*-a,(a*c) / (d*d+c*c)]
                    Emit("fdivrp %st(0), %st(1)");   // [(d*-a) / (d*d+c*c),(a*c) / (d*d+c*c)]

                    FpuPop(type);
                } else if (lhs.Type.IsComplexType() && rhs.Type.IsRealFloatingType()) {
                    FpuPush();    // rhs c
                    FpuPush();    // lhs [a + bi]

                    // %st(0) = b
                    // %st(1) = a
                    // %st(2) = c 

                    // (a/c)+(b/c)i

                    Emit("fld %st(2)");            // [c,b,a,c]
                    Emit("fdivrp %st(0), %st(2)"); // [b,a/c,c]
                    Emit("fdivp %st(0), %st(2)");  // [a/c,b/c]
                    Emit("fxch %st(1)");            // [b/c,a/c]

                    FpuPop(type);
                } else {
                    throw new NotImplementedException();
                }
                CheckStackDepth(sdp - 1);
            }

            public void Mod(CType type) {
                var sdp = GetStackDepth();
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (type.IsBasicType(BasicType.TypeKind.SignedLongLongInt)) {
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
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else if (type.IsBasicType(BasicType.TypeKind.UnsignedLongLongInt)) {
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
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%ecx"); // rhs
                        LoadI32("%eax"); // lhs
                        if (type.IsUnsignedIntegerType()) {
                            Emit("movl $0, %edx");
                            Emit("divl %ecx");
                        } else {
                            Emit("cltd");
                            Emit("idivl %ecx");
                        }

                        Emit("push %edx");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else {
                    throw new NotImplementedException();
                }
                CheckStackDepth(sdp - 1);
            }

            public void And(CType type) {
                var sdp = GetStackDepth();
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("andl %ebx, %edx");
                        Emit("andl %ecx, %eax");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%eax"); // rhs
                        LoadI32("%ecx"); // lhs
                        Emit("andl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else {
                    throw new NotImplementedException();
                }
                CheckStackDepth(sdp - 1);
            }

            public void Or(CType type) {
                var sdp = GetStackDepth();
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("orl %ebx, %edx");
                        Emit("orl %ecx, %eax");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%eax"); // rhs
                        LoadI32("%ecx"); // lhs
                        Emit("orl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else {
                    throw new NotImplementedException();
                }
                CheckStackDepth(sdp - 1);
            }

            public void Xor(CType type) {
                var sdp = GetStackDepth();
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt) || rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%ebx"); // rhs
                        LoadI64("%ecx", "%edx"); // lhs
                        Emit("xorl %ebx, %edx");
                        Emit("xorl %ecx, %eax");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
                        LoadI32("%eax"); // rhs
                        LoadI32("%ecx"); // lhs
                        Emit("xorl %ecx, %eax");
                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else {
                    throw new NotImplementedException();
                }
                CheckStackDepth(sdp - 1);
            }

            public void Shl(CType type) {
                var sdp = GetStackDepth();
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)/* ||
                        rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)*/) {
#if false
                        LoadI64("%ecx", "%ebx"); // rhs
#else
                        CastTo(CType.CreateChar()); // rhsを signed char にキャスト
                        Emit("pop %ecx"); Pop(); // rhs
#endif

                        LoadI64("%eax", "%edx"); // lhs
                        Emit("shldl %cl, %eax, %edx");
                        if (lhs.Type.IsUnsignedIntegerType()) {
                            Emit("shll %cl, %eax");
                        } else {
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
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
#if false
                        LoadI32("%ecx"); // rhs
#else
                        CastTo(CType.CreateChar()); // rhsを signed char にキャスト
                        Emit("pop %ecx"); Pop(); // rhs
#endif
                        LoadI32("%eax"); // lhs
                        if (lhs.Type.IsUnsignedIntegerType()) {
                            Emit("shll %cl, %eax");
                        } else {
                            Emit("sall %cl, %eax");
                        }

                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else {
                    throw new NotImplementedException();
                }
                CheckStackDepth(sdp - 1);
            }

            public void Shr(CType type) {
                var sdp = GetStackDepth();
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt) /*|| 
                        rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)*/) {
#if false
                        LoadI64("%ecx", "%ebx"); // rhs
#else
                        CastTo(CType.CreateChar()); // rhsを signed char にキャスト
                        Emit("pop %ecx"); Pop(); // rhs
#endif
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
                        } else {
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

                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    } else {
#if false
                        LoadI32("%ecx"); // rhs
#else
                        CastTo(CType.CreateChar()); // rhsを signed char にキャスト
                        Emit("pop %ecx"); Pop(); // rhs
#endif
                        LoadI32("%eax"); // lhs
                        if (lhs.Type.IsUnsignedIntegerType()) {
                            Emit("shrl %cl, %eax");
                        } else {
                            Emit("sarl %cl, %eax");
                        }

                        Emit("pushl %eax");
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                    }
                } else {
                    throw new NotImplementedException();
                }
                CheckStackDepth(sdp - 1);
            }

            public void Assign(CType type) {
                var sdp = GetStackDepth();
                var lhs = Peek(0);
                var rhs = Peek(1);

                BitFieldType bft;
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
                                } else {
                                    // ビットフィールドの最上位ビットより上をを消す
                                    Emit($"andl ${srcMask}, %ecx");

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
                                } else {
                                    // ビットフィールドの最上位ビットより上をを消す
                                    Emit($"andl ${srcMask}, %ecx");
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
                                } else {
                                    // ビットフィールドの最上位ビットより上をを消す
                                    Emit($"andl ${srcMask}, %ecx");
                                }
                                Emit("pushl %ecx");

                                // ビットマスク処理と位置合わせをする
                                Emit($"andl ${srcMask}, %ecx");
                                Emit($"shll ${offsetBit}, %ecx");

                                // フィールドが属する領域を読み出してフィールドの範囲のビットを消す
                                Emit($"movl (%eax), %edx");
                                Emit($"andl ${(UInt32)dstMask}, %edx");
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
                            Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                            break;
                        case 2:
                            LoadI32("%ecx");
                            Emit("movw %cx, (%eax)");
                            Emit("pushl %ecx");
                            Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                            break;
                        case 3:
                        case 4:
                            LoadI32("%ecx");
                            Emit("movl %ecx, (%eax)");
                            Emit("pushl %ecx");
                            Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                            break;
                        default:
                            LoadValueToStack(rhs.Type);
                            Emit("movl %esp, %esi");
                            Emit($"movl ${type.Sizeof()}, %ecx");
                            Emit("movl %eax, %edi");
                            Emit("cld");
                            Emit("rep movsb");
                            Pop();
                            Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                            break;
                    }
                }
                CheckStackDepth(sdp - 1);
            }

            public void Eq(CType type) {
                var sdp = GetStackDepth();
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
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
                    } else {
                        LoadI32("%eax");
                        LoadI32("%ecx");
                        Emit("cmpl %ecx, %eax");

                        Emit("sete %al");
                        Emit("movzbl %al, %eax");
                        Emit("pushl %eax");
                    }

                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    LoadI32("%eax");
                    LoadI32("%ecx");
                    Emit("cmpl %ecx, %eax");
                    Emit("sete %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();

                    Emit("fcomip");
                    Emit("fstp %st(0)");

                    Emit("sete %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");

                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
                    throw new NotImplementedException();
                }
                CheckStackDepth(sdp - 1);
            }

            public void Ne(CType type) {
                var sdp = GetStackDepth();
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
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
                    } else {
                        LoadI32("%eax");
                        LoadI32("%ecx");
                        Emit("cmpl %ecx, %eax");

                        Emit("setne %al");
                        Emit("movzbl %al, %eax");
                        Emit("pushl %eax");
                    }

                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    LoadI32("%eax");
                    LoadI32("%ecx");
                    Emit("cmpl %ecx, %eax");
                    Emit("setne %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();

                    Emit("fcomip");
                    Emit("fstp %st(0)");

                    Emit("setne %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");

                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
                    throw new NotImplementedException($"lhs={lhs.Type} rhs={rhs.Type}");
                }
                CheckStackDepth(sdp - 1);
            }

            public void Label(string label) {
                Emit($"{label}:");
            }

            public void Jmp(string label) {
                Emit($"jmp {label}");
            }

            public void JmpFalse(string label) {
                var sdp = GetStackDepth();
                if (Peek(0).Type.IsRealFloatingType()) {
                    var value = new Value { Kind = Value.ValueKind.IntConst, Type = CType.CreateSignedInt(), IntConst = 0 };
                    Push(value);
                    Ne(CType.CreateSignedInt());
                }
                CheckStackDepth(sdp);
                LoadI32("%eax");
                Emit("cmpl $0, %eax");
                Emit($"je {label}");
                CheckStackDepth(sdp - 1);
            }

            public void JmpTrue(string label) {
                var sdp = GetStackDepth();
                if (Peek(0).Type.IsRealFloatingType()) {
                    var value = new Value { Kind = Value.ValueKind.IntConst, Type = CType.CreateSignedInt(), IntConst = 0 };
                    Push(value);
                    Ne(CType.CreateSignedInt());
                }
                CheckStackDepth(sdp);
                LoadI32("%eax");
                Emit("cmpl $0, %eax");
                Emit($"jne {label}");
                CheckStackDepth(sdp - 1);
            }

            public void EmitLoadTrue() {
                Emit("pushl $1");
            }

            public void EmitLoadFalse() {
                Emit("pushl $0");
            }

            private void CastIntValueToInt(CType type) {
                Value ret = Peek(0);
                Debug.Assert(ret.Type.IsIntegerType() && type.IsIntegerType());

                BasicType.TypeKind selftykind;
                if (type.IsBasicType()) {
                    selftykind = (type.Unwrap() as BasicType).Kind;
                } else if (type.IsEnumeratedType()) {
                    selftykind = BasicType.TypeKind.SignedInt;
                } else {
                    throw new NotImplementedException();
                }

                if (ret.Type.IsBasicType(BasicType.TypeKind.UnsignedLongLongInt, BasicType.TypeKind.SignedLongLongInt)) {
                    // 64bit型からのキャスト
                    LoadI64("%eax", "%ecx");
                    /* 符号の有無にかかわらず、切り捨てでよい？ */
                    switch (selftykind) {
                        case BasicType.TypeKind.Char:
                        case BasicType.TypeKind.SignedChar:
                            Emit("movsbl %al, %eax");
                            Emit("pushl %eax");
                            break;
                        case BasicType.TypeKind.SignedShortInt:
                            Emit("cwtl");
                            Emit("pushl %eax");
                            break;
                        case BasicType.TypeKind.SignedInt:
                        case BasicType.TypeKind.SignedLongInt:
                            // do nothing;
                            Emit("pushl %eax");
                            break;
                        case BasicType.TypeKind.SignedLongLongInt:
                            Emit("pushl %ecx");
                            Emit("pushl %eax");
                            break;
                        case BasicType.TypeKind.UnsignedChar:
                            Emit("movzbl %al, %eax");
                            Emit("pushl %eax");
                            break;
                        case BasicType.TypeKind.UnsignedShortInt:
                            Emit("movzwl %ax, %eax");
                            Emit("pushl %eax");
                            break;
                        case BasicType.TypeKind.UnsignedInt:
                        case BasicType.TypeKind.UnsignedLongInt:
                            // do nothing;
                            Emit("pushl %eax");
                            break;
                        case BasicType.TypeKind.UnsignedLongLongInt:
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
                        case BasicType.TypeKind.Char:
                        case BasicType.TypeKind.SignedChar:
                            Emit("movsbl %al, %eax");
                            Emit("pushl %eax");
                            break;
                        case BasicType.TypeKind.SignedShortInt:
                            Emit("movswl %ax, %eax");
                            Emit("pushl %eax");
                            break;
                        case BasicType.TypeKind.SignedInt:
                        case BasicType.TypeKind.SignedLongInt:
                            // do nothing;
                            Emit("pushl %eax");
                            break;
                        case BasicType.TypeKind.SignedLongLongInt:
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
                        case BasicType.TypeKind.UnsignedChar:
                            Emit("movzbl %al, %eax");
                            Emit("pushl %eax");
                            break;
                        case BasicType.TypeKind.UnsignedShortInt:
                            Emit("movzwl %ax, %eax");
                            Emit("pushl %eax");
                            break;
                        case BasicType.TypeKind.UnsignedInt:
                        case BasicType.TypeKind.UnsignedLongInt:
                            // do nothing;
                            Emit("pushl %eax");
                            break;
                        case BasicType.TypeKind.UnsignedLongLongInt:
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
                        case BasicType.TypeKind._Bool:
                            // do nothing;
                            Emit("pushl %eax");
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }

                Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
            }

            private void CastPointerValueToPointer(CType type) {
                Value ret = Peek(0);
                Debug.Assert(ret.Type.IsPointerType() && type.IsPointerType());
                Pop();
                Push(new Value(ret) { Type = type });
            }

            private void CastArrayValueToPointer(CType type) {
                Value ret = Peek(0);
                Debug.Assert(ret.Type.IsArrayType() && type.IsPointerType());
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
                Debug.Assert(ret.Type.IsArrayType() && type.IsArrayType());
                Pop();
                Push(new Value(ret) { Type = type });
            }

            private void CastIntValueToPointer(CType type) {
                Value ret = Peek(0);
                Debug.Assert(ret.Type.IsIntegerType() && type.IsPointerType());
                // Todo: 64bit int value -> 32bit pointer value の実装
                Pop();
                Push(new Value(ret) { Type = type });
            }

            private void CastPointerValueToInt(CType type) {
                Value ret = Peek(0);
                Debug.Assert(ret.Type.IsPointerType() && type.IsIntegerType());
                // Todo: 32bit pointer value -> 64bit int value の実装
                Pop();
                Push(new Value(ret) { Type = type });
            }

            private void CastFloatingValueToFloating(CType type) {
                Value ret = Peek(0);
                Debug.Assert(ret.Type.IsRealFloatingType() && type.IsRealFloatingType());
                FpuPush();
                FpuPop(type);
            }

            private void CastFloatingValueToInt(CType type) {
                Value ret = Peek(0);
                Debug.Assert(ret.Type.IsRealFloatingType() && type.IsIntegerType());
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
                Debug.Assert(ret.Type.IsIntegerType() && type.IsRealFloatingType());
                FpuPush();
                FpuPop(type);
            }
            private void CastComplexTypeToOtherComplexType(CType type) {
                Value ret = Peek(0);
                Debug.Assert(ret.Type.IsComplexType() && type.IsComplexType());
                FpuPush();
                FpuPop(type);
            }
            private void CastComplexTypeToRealFloatingType(CType type) {
                Value ret = Peek(0);
                Debug.Assert(ret.Type.IsComplexType() && type.IsRealFloatingType());
                FpuPush();      // real->imagの順で積まれる
                FpuDiscard();   // imagを捨てる
                FpuPop(type);   // realを読む
            }
            private void CastRealFloatingTypeToComplexType(CType type) {
                Value ret = Peek(0);
                Debug.Assert(ret.Type.IsRealFloatingType() && type.IsComplexType());
                FpuPush();      // realを積む
                FpuPushZero();  // imagを積む
                FpuPop(type);   // 読む
            }

            public void CastTo(CType type) {
                Value ret = Peek(0);
                if (type.IsBoolType()) {
                    // Boolは専用のルールに従ってキャストする
                    if (ret.Type.IsComplexType()) {
                        FpuPush();      // real->imagの順で積まれる
                        Emit("fldz");   // ST(0) == 0   
                        Emit("fcomi %st(1), %st(0)");   // ST(0) == 0   
                        Emit("setne %al");
                        Emit("fcomip %st(2), %st(0)");   // ST(0) == 0   
                        Emit("setne %ah");
                        Emit("fstp %st(0)");
                        Emit("fstp %st(0)");
                        Emit("orb %al, %ah");
                        Emit("movzbl %al, %eax");
                        Emit("pushl %eax");

                        Push(new Value { Kind = Value.ValueKind.Temp, Type = CType.CreateBool(), StackPos = _stack.Count });
                    } else if (ret.Type.IsRealFloatingType()) {
                        var value = new Value { Kind = Value.ValueKind.FloatConst, Type = CType.CreateDouble(), FloatConst = 0.0 };
                        Push(value);
                        Ne(CType.CreateBool());
                    } else if (ret.Type.IsIntegerType() || ret.Type.IsPointerType()) {
                        var value = new Value { Kind = Value.ValueKind.IntConst, Type = ret.Type, IntConst = 0 };
                        Push(value);
                        Ne(CType.CreateBool());
                    } else {
                        throw new NotImplementedException();
                    }
                } else if (ret.Type.IsIntegerType() && type.IsIntegerType()) {
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
                } else if (ret.Type.IsComplexType() && type.IsComplexType()) {
                    CastComplexTypeToOtherComplexType(type);
                } else if (ret.Type.IsComplexType() && type.IsRealFloatingType()) {
                    CastComplexTypeToRealFloatingType(type);
                } else if (ret.Type.IsRealFloatingType() && type.IsComplexType()) {
                    CastRealFloatingTypeToComplexType(type);
                } else if (ret.Type.IsUnionType() && type.IsUnionType()) {
                    // キャスト不要
                } else if (type.IsVoidType()) {
                    // キャスト不要（というより、無視？）
                } else {
                    throw new NotImplementedException($"cast {ret.Type.ToString()} to {type.ToString()} is not implemented.");
                }
            }

            public void ArraySubscript(CType type) {
                var index = Peek(0);
                var target = Peek(1);
                LoadI32("%ecx");
                if (target.Type.IsPointerType()) {
                    LoadPointer("%eax");
                } else {
                    LoadVariableAddress("%eax");
                }

                Emit($"imull ${type.Sizeof()}, %ecx, %ecx");
                Emit("leal (%eax, %ecx), %eax");
                Emit("pushl %eax");

                Push(new Value { Kind = Value.ValueKind.Address, Type = type, StackPos = _stack.Count });
            }

            public void Call(CType type, FunctionType funcType, int argnum, Action<CodeGenerator> fun, Action<CodeGenerator, int> args) {
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
                } else {
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
                if (resultSize > 4 && !funcType.ResultType.IsRealFloatingType() && !funcType.ResultType.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                    Emit($"leal {argSize + bakSize}(%esp), %eax");
                    Emit("push %eax");
                }

                fun(this);
                LoadI32("%eax");
                Emit("call *%eax");

                if (funcType.ResultType.IsRealFloatingType()) {
                    // 浮動小数点数型の結果はFPUスタック上にある
                    if (funcType.ResultType.IsBasicType(BasicType.TypeKind.Float)) {
                        Emit($"fstps {(argSize + bakSize)}(%esp)");
                    } else if (funcType.ResultType.IsBasicType(BasicType.TypeKind.Double)) {
                        Emit($"fstpl {(argSize + bakSize)}(%esp)");
                    } else {
                        throw new NotImplementedException();
                    }

                    Emit($"addl ${argSize}, %esp");
                } else if (funcType.ResultType.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                    // long long型の結果はedx:eaxに格納される
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

                Emit("popl %edx");
                Emit("popl %ecx");
                Emit("popl %eax");

                Debug.Assert(_stack.Count >= argnum);
                for (int i = 0; i < argnum; i++) {
                    Pop(); // args
                }

                Push(new Value { Kind = resultSize == 0 ? Value.ValueKind.Void : Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
            }

            private int GetMemberOffset(CType type, string member) {
                var st = type.Unwrap() as TaggedType.StructUnionType;
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

                Push(new Value { Kind = Value.ValueKind.Address, Type = type, StackPos = _stack.Count });
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
                Push(new Value { Kind = Value.ValueKind.Address, Type = type, StackPos = _stack.Count });
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
                            BasicType.TypeKind kind;
                            if (valueType.Unwrap() is BasicType) {
                                kind = (valueType.Unwrap() as BasicType).Kind;
                            } else if (valueType.IsEnumeratedType()) {
                                kind = BasicType.TypeKind.SignedInt;
                            } else if (valueType.IsPointerType()) {
                                kind = BasicType.TypeKind.UnsignedInt;
                            } else {
                                throw new Exception("整数定数値の型が不正です");
                            }
                            switch (kind) {
                                case BasicType.TypeKind.Char:
                                case BasicType.TypeKind.SignedChar:
                                    Emit($"movb ${value.IntConst}, {ToByteReg(register)}");
                                    Emit($"movsbl {ToByteReg(register)}, {register}");
                                    break;
                                case BasicType.TypeKind.UnsignedChar:
                                    Emit($"movb ${value.IntConst}, {ToByteReg(register)}");
                                    Emit($"movzbl {ToByteReg(register)}, {register}");
                                    break;
                                case BasicType.TypeKind.SignedShortInt:
                                    Emit($"movw ${value.IntConst}, {ToWordReg(register)}");
                                    Emit($"movswl {ToWordReg(register)}, {register}");
                                    break;
                                case BasicType.TypeKind.UnsignedShortInt:
                                    Emit($"movw ${value.IntConst}, {ToWordReg(register)}");
                                    Emit($"movzwl {ToWordReg(register)}, {register}");
                                    break;
                                case BasicType.TypeKind.SignedInt:
                                case BasicType.TypeKind.SignedLongInt:
                                    Emit($"movl ${value.IntConst}, {register}");
                                    break;
                                case BasicType.TypeKind.UnsignedInt:
                                case BasicType.TypeKind.UnsignedLongInt:
                                    Emit($"movl ${value.IntConst}, {register}");
                                    break;
                                case BasicType.TypeKind._Bool:
                                    Emit($"movl ${value.IntConst}, {register}");
                                    break;
                                case BasicType.TypeKind.SignedLongLongInt:
                                case BasicType.TypeKind.UnsignedLongLongInt:
                                default:
                                    throw new Exception("32bitレジスタにロードできない定数値です。");
                            }


                            return;
                        }
                    case Value.ValueKind.Temp: {
                            //if (valueType.Sizeof() <= 4) {
                            //    // スタックトップの値をレジスタにロード
                            //    Emit($"popl {register}");
                            //    return;
                            //} else {
                            //    throw new NotImplementedException();
                            //}
                            BasicType.TypeKind kind;
                            if (valueType.Unwrap() is BasicType) {
                                kind = (valueType.Unwrap() as BasicType).Kind;
                            } else if (valueType.IsEnumeratedType()) {
                                kind = BasicType.TypeKind.SignedInt;
                            } else if (valueType.IsPointerType()) {
                                kind = BasicType.TypeKind.UnsignedInt;
                            } else {
                                throw new Exception("整数定数値の型が不正です");
                            }
                            switch (kind) {
                                case BasicType.TypeKind.Char:
                                case BasicType.TypeKind.SignedChar:
                                    Emit($"popl {register}");
                                    Emit($"movsbl {ToByteReg(register)}, {register}");
                                    break;
                                case BasicType.TypeKind.UnsignedChar:
                                    Emit($"popl {register}");
                                    Emit($"movzbl {ToByteReg(register)}, {register}");
                                    break;
                                case BasicType.TypeKind.SignedShortInt:
                                    Emit($"popl {register}");
                                    Emit($"movswl {ToWordReg(register)}, {register}");
                                    break;
                                case BasicType.TypeKind.UnsignedShortInt:
                                    Emit($"popl {register}");
                                    Emit($"movzwl {ToWordReg(register)}, {register}");
                                    break;
                                case BasicType.TypeKind.SignedInt:
                                case BasicType.TypeKind.SignedLongInt:
                                    Emit($"popl {register}");
                                    break;
                                case BasicType.TypeKind.UnsignedInt:
                                case BasicType.TypeKind.UnsignedLongInt:
                                    Emit($"popl {register}");
                                    break;
                                case BasicType.TypeKind._Bool:
                                    Emit($"popl {register}");
                                    break;
                                case BasicType.TypeKind.SignedLongLongInt:
                                case BasicType.TypeKind.UnsignedLongLongInt:
                                default:
                                    if (valueType.Sizeof() <= 4) {
                                        Emit($"popl {register}");
                                        break;
                                    }
                                    throw new Exception("32bitレジスタにロードできないテンポラリ値です。");
                            }

                            return;
                        }
                    case Value.ValueKind.FloatConst: {
                            BasicType.TypeKind kind = (valueType.Unwrap() as BasicType).Kind;
                            // 一時的
                            if (kind == BasicType.TypeKind.Float) {
                                var bytes = BitConverter.GetBytes((float)value.FloatConst);
                                var dword = BitConverter.ToUInt32(bytes, 0);
                                Emit($"movl ${dword}, {register}");
                            } else {
                                throw new Exception("32bitレジスタにfloat型以外の浮動小数定数値はロードできません。");
                            }
                            return;
                        }
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
                                    throw new NotImplementedException("こないはず");
                            }

                            string op;
                            // ビットフィールドは特別扱い
                            BitFieldType bft;
                            if (valueType.IsBitField(out bft)) {
                                switch (bft.Type.Sizeof()) {
                                    case 1: {
                                            int offsetBit = bft.BitOffset;
                                            UInt32 srcMask = (UInt32)((1U << bft.BitWidth) - 1);
                                            UInt32 dstMask = ~(srcMask << (offsetBit));

                                            // フィールドが属する領域を読み出し右詰してから、無関係のビットを消す
                                            var byteReg = ToByteReg(register);
                                            Emit($"movb {src}, {byteReg}");
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
                                            Emit($"movw {src}, {wordReg}");
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
                                            Emit($"movl {src}, {register}");
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
                                if (valueType.IsSignedIntegerType() || valueType.IsBasicType(BasicType.TypeKind.Char) || valueType.IsEnumeratedType()) {
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
                                } else if (valueType.IsBasicType(BasicType.TypeKind.Float)) {
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
                            if (valueType.IsSignedIntegerType() || valueType.IsBasicType(BasicType.TypeKind.Char)) {
                                switch (valueType.Sizeof()) {
                                    //case 1:
                                    //    //Emit($"movb ${value.IntConst}, {ToByteReg(regLo)}");
                                    //    //Emit($"movsbl {ToByteReg(regLo)}, {regLo}");
                                    //    //Emit($"movl $0, {regHi}");
                                    //    break;
                                    //case 2:
                                    //    Emit($"movw ${value.IntConst}, {ToWordReg(regLo)}");
                                    //    Emit($"movswl {ToWordReg(regLo)}, {regLo}");
                                    //    Emit($"movl $0, {regHi}");
                                    //    break;
                                    //case 4:
                                    //    Emit($"movl ${value.IntConst}, {regLo}");
                                    //    Emit($"movl $0, {regHi}");
                                    //    break;
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
                            } else {
                                switch (valueType.Sizeof()) {
                                    //case 1:
                                    //    Emit($"movb ${value.IntConst}, {ToByteReg(regLo)}");
                                    //    Emit($"movzbl {ToByteReg(regLo)}, {regLo}");
                                    //    Emit($"movl $0, {regHi}");
                                    //    break;
                                    //case 2:
                                    //    Emit($"movw ${value.IntConst}, {ToWordReg(regLo)}");
                                    //    Emit($"movzwl {ToWordReg(regLo)}, {regLo}");
                                    //    Emit($"movl $0, {regHi}");
                                    //    break;
                                    //case 4:
                                    //    Emit($"movl ${value.IntConst}, {regLo}");
                                    //    Emit($"movl $0, {regHi}");
                                    //    break;
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
                        //if (valueType.Sizeof() <= 4) {
                        //    // スタックトップの値をレジスタにロード
                        //    Emit($"popl {regLo}");
                        //    Emit($"movl $0, {regHi}");
                        //    return;
                        //} else 
                        if (valueType.Sizeof() == 8) {
                            // スタックトップの値をレジスタにロード
                            Emit($"popl {regLo}");
                            Emit($"popl {regHi}");
                            return;
                        } else {
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

                            if (valueType.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt, BasicType.TypeKind.Double)) {
                                Emit($"movl {src(0)}, {regLo}");
                                Emit($"movl {src(4)}, {regHi}");
                            } else {
                                //string op;
                                //if (valueType.IsSignedIntegerType() || valueType.IsBasicType(BasicType.TypeKind.Char) || valueType.IsEnumeratedType()) {
                                //    switch (valueType.Sizeof()) {
                                //        case 1:
                                //            op = "movsbl";
                                //            break;
                                //        case 2:
                                //            op = "movswl";
                                //            break;
                                //        case 4:
                                //            op = "movl";
                                //            break;
                                //        default:
                                //            throw new NotImplementedException();
                                //    }
                                //} else if (valueType.IsUnsignedIntegerType()) {
                                //    switch (valueType.Sizeof()) {
                                //        case 1:
                                //            op = "movzbl";
                                //            break;
                                //        case 2:
                                //            op = "movzwl";
                                //            break;
                                //        case 4:
                                //            op = "movl";
                                //            break;
                                //        default:
                                //            throw new NotImplementedException();
                                //    }
                                //} else if (valueType.IsBasicType(BasicType.TypeKind.Float)) {
                                //    op = "movl";
                                //} else if (valueType.IsPointerType() || valueType.IsArrayType()) {
                                //    op = "movl";
                                //} else if (valueType.IsStructureType()) {
                                //    op = "leal";
                                //} else {
                                //    throw new NotImplementedException();
                                //}

                                //Emit($"{op} {src(0)}, {regLo}");
                                //Emit($"movl $0, {regHi}");
                                throw new NotImplementedException();
                            }

                            return;
                        }
                    case Value.ValueKind.Ref:
                        //Emit($"leal {VarRefToAddrExpr(value)}, {regLo}");
                        //Emit($"movl $0, {regHi}");
                        //return;
                        throw new NotImplementedException();
                    default:
                        throw new NotImplementedException();
                }
            }
            /// <summary>
            ///     FPUスタック上に+0.0をロードする
            /// </summary>
            private void FpuPushZero() {
                Emit("fldz");
            }
            private void FpuPushOne() {
                Emit("fld1");
            }
            /// <summary>
            ///     FPUスタック上に値をロードする
            /// </summary>
            private void FpuPush() {
                var rhs = Peek(0);
                if (rhs.Kind != Value.ValueKind.Temp) {
                    LoadValueToStack(rhs.Type);
                }

                rhs = Peek(0);
                if (rhs.Type.IsBasicType(BasicType.TypeKind.Float)) {
                    rhs = Pop();
                    Emit("flds (%esp)");
                    Emit("addl $4, %esp");
                } else if (rhs.Type.IsBasicType(BasicType.TypeKind.Double)) {
                    rhs = Pop();
                    Emit("fldl (%esp)");
                    Emit("addl $8, %esp");
                } else if (rhs.Type.IsComplexType(BasicType.TypeKind.Float)) {
                    rhs = Pop();
                    Emit("flds (%esp)");
                    Emit("addl $4, %esp");
                    Emit("flds (%esp)");
                    Emit("addl $4, %esp");
                } else if (rhs.Type.IsComplexType(BasicType.TypeKind.Double)) {
                    rhs = Pop();
                    Emit("fldl (%esp)");
                    Emit("addl $8, %esp");
                    Emit("fldl (%esp)");
                    Emit("addl $8, %esp");
                } else if (rhs.Type.IsIntegerType()) {
                    if (rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt)) {
                        rhs = Pop();
                        Emit("fildq (%esp)");
                        Emit("addl $8, %esp");
                    } else if (rhs.Type.IsBasicType(BasicType.TypeKind.UnsignedLongLongInt)) {
                        rhs = Pop();
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
                    } else if (rhs.Type.IsBasicType(BasicType.TypeKind.UnsignedLongInt, BasicType.TypeKind.UnsignedInt, BasicType.TypeKind._Bool)) {
                        rhs = Pop();
                        Emit("pushl $0");
                        Emit("pushl 4(%esp)");
                        Emit("fildq (%esp)");
                        Emit("addl $12, %esp");
                    } else {
                        CastIntValueToInt(CType.CreateSignedInt());
                        rhs = Pop();
                        Emit("fildl (%esp)");
                        Emit("addl $4, %esp");
                    }
                } else {
                    throw new NotImplementedException();
                }
            }

            /// <summary>
            ///     FPUスタックの一番上の値をポップし、CPUスタックの一番上に積む
            /// </summary>
            /// <param name="ty"></param>
            public void FpuPop(CType ty) {
                if (ty.IsBasicType(BasicType.TypeKind.Float)) {
                    Emit("sub $4, %esp");
                    Emit("fstps (%esp)");
                    Push(new Value { Kind = Value.ValueKind.Temp, Type = ty, StackPos = _stack.Count });
                } else if (ty.IsBasicType(BasicType.TypeKind.Double)) {
                    Emit("sub $8, %esp");
                    Emit("fstpl (%esp)");
                    Push(new Value { Kind = Value.ValueKind.Temp, Type = ty, StackPos = _stack.Count });
                } else if (ty.IsComplexType(BasicType.TypeKind.Float)) {
                    Emit("sub $4, %esp");
                    Emit("fstps (%esp)");
                    Emit("sub $4, %esp");
                    Emit("fstps (%esp)");
                    Push(new Value { Kind = Value.ValueKind.Temp, Type = ty, StackPos = _stack.Count });
                } else if (ty.IsComplexType(BasicType.TypeKind.Double)) {
                    Emit("sub $8, %esp");
                    Emit("fstpl (%esp)");
                    Emit("sub $8, %esp");
                    Emit("fstpl (%esp)");
                    Push(new Value { Kind = Value.ValueKind.Temp, Type = ty, StackPos = _stack.Count });
                } else {
                    throw new NotImplementedException();
                }
            }
            /// <summary>
            ///     FPUスタックの一番上の値を捨てる
            /// </summary>
            /// <param name="ty"></param>
            public void FpuDiscard() {
                Emit("fstp %st(0)");
            }

            /// <summary>
            ///     ポインタをロード
            /// </summary>
            /// <param name="reg"></param>
            public void LoadPointer(string reg) {
                Value target = Pop();
                Debug.Assert(target.Type.IsPointerType());

                switch (target.Kind) {
                    case Value.ValueKind.IntConst:
                        // 定数値の場合 => mov命令で定数値をロード
                        Emit($"movl ${unchecked((int)target.IntConst)}, {reg}");
                        break;
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
                            if (valueType.IsSignedIntegerType() || valueType.IsBasicType(BasicType.TypeKind.Char)) {
                                switch (valueType.Sizeof()) {
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
                                switch (valueType.Sizeof()) {
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
                            Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        }
                        break;
                    case Value.ValueKind.Temp:
                        break;
                    case Value.ValueKind.FloatConst: {
                            if (value.Type.Unwrap().IsBasicType(BasicType.TypeKind.Float)) {
                                var bytes = BitConverter.GetBytes((float)value.FloatConst);
                                var dword = BitConverter.ToUInt32(bytes, 0);
                                Emit($"pushl ${dword}");
                            } else if (value.Type.Unwrap().IsBasicType(BasicType.TypeKind.Double)) {
                                var bytes = BitConverter.GetBytes(value.FloatConst);
                                var qwordlo = BitConverter.ToUInt32(bytes, 0);
                                var qwordhi = BitConverter.ToUInt32(bytes, 4);
                                Emit($"pushl ${qwordhi}");
                                Emit($"pushl ${qwordlo}");
                            } else {
                                throw new NotImplementedException();
                            }

                            Pop();
                            Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
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

                            if (value.Type.IsBitField()) {
                                var bft = (BitFieldType)value.Type;
                                // ビットフィールドなので
                                switch (bft.Sizeof()) {
                                    case 1:
                                        Emit($"movb (%esi), %al");
                                        Emit($"shlb ${7 - (bft.BitOffset + bft.BitWidth)}, %al");
                                        if (bft.IsSignedIntegerType()) {
                                            Emit($"sarb ${8 - (bft.BitOffset + bft.BitWidth) + bft.BitOffset}, %al");
                                        } else {
                                            Emit($"shrb ${8 - (bft.BitOffset + bft.BitWidth) + bft.BitOffset}, %al");
                                        }
                                        if (type.IsSignedIntegerType()) {
                                            Emit("movsbl %al, %eax");
                                        } else {
                                            Emit("movzbl %al, %eax");
                                        }
                                        break;
                                    case 2:
                                        Emit($"movw (%esi), %ax");
                                        Emit($"shlw ${16 - (bft.BitOffset + bft.BitWidth)}, %ax");
                                        if (bft.IsSignedIntegerType()) {
                                            Emit($"sarw ${16 - (bft.BitOffset + bft.BitWidth) + bft.BitOffset}, %ax");
                                        } else {
                                            Emit($"shrw ${16 - (bft.BitOffset + bft.BitWidth) + bft.BitOffset}, %ax");
                                        }
                                        if (type.IsSignedIntegerType()) {
                                            Emit("movswl %al, %eax");
                                        } else {
                                            Emit("movzwl %al, %eax");
                                        }
                                        break;
                                    case 4:
                                        Emit($"movl (%esi), %eax");
                                        Emit($"shll ${32 - (bft.BitOffset + bft.BitWidth)}, %eax");
                                        if (bft.IsSignedIntegerType()) {
                                            Emit($"sarl ${32 - (bft.BitOffset + bft.BitWidth) + bft.BitOffset}, %eax");
                                        } else {
                                            Emit($"shrl ${32 - (bft.BitOffset + bft.BitWidth) + bft.BitOffset}, %eax");
                                        }
                                        break;
                                    case 8:
                                    default:
                                        throw new NotSupportedException();
                                }
                                Emit($"push %eax");
                            } else {

                                // コピー先を確保
                                Emit($"subl ${StackAlign(valueType.Sizeof())}, %esp");
                                Emit("movl %esp, %edi");

                                // 転送
                                Emit("pushl %ecx");
                                Emit($"movl ${valueType.Sizeof()}, %ecx");
                                Emit("cld");
                                Emit("rep movsb");
                                Emit("popl %ecx");
                            }
                            Pop();
                            Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        }
                        break;
                    case Value.ValueKind.Ref: {
                            // ラベル参照
                            Emit($"leal {VarRefToAddrExpr(value)}, %esi");
                            Emit("pushl %esi");
                            Pop();
                            Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            private void PostfixOp(CType type, string op) {
                if (type.IsBoolType()) {

                    LoadVariableAddress("%eax");

                    // load value
                    switch (type.Sizeof()) {
                        case 8:
                            var subop = (op == "add") ? "adc" : "sbb";
                            Emit("pushl 4(%eax)");
                            Emit("pushl 0(%eax)");
                            if (op == "add") {
                                Emit($"movl $1, 0(%eax)");
                                Emit($"movl $0, 4(%eax)");
                            } else {
                                Emit($"xorl $1, 0(%eax)");
                                Emit($"movl $0, 4(%eax)");
                            }
                            break;
                        case 4:
                            Emit("pushl (%eax)");
                            if (op == "add") {
                                Emit($"movl $1, (%eax)");
                            } else {
                                Emit($"xorl $1, (%eax)");
                            }
                            break;
                        case 2:
                            Emit("movw (%eax), %cx");
                            Emit($"movzwl %cx, %ecx");
                            Emit($"pushl %ecx");

                            if (op == "add") {
                                Emit($"movw $1, (%eax)");
                            } else {
                                Emit($"xorw $1, (%eax)");
                            }
                            break;
                        case 1:
                            Emit("movb (%eax), %cl");
                            Emit($"movzbl %cl, %ecx");
                            Emit($"pushl %ecx");
                            if (op == "add") {
                                Emit($"movb $1, (%eax)");
                            } else {
                                Emit($"xorb $1, (%eax)");
                            }
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });

                } else if (type.IsIntegerType() || type.IsPointerType()) {
                    CType baseType;
                    int size;
                    if (type.IsPointerType(out baseType) && !baseType.IsVoidType()) {
                        size = baseType.Sizeof();
                    } else {
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
                            } else {
                                Emit("movzwl %cx, %ecx");
                            }

                            Emit("pushl %ecx");
                            Emit($"{op}w ${size}, (%eax)");
                            break;
                        case 1:
                            Emit("movb (%eax), %cl");
                            if (type.IsSignedIntegerType()) {
                                Emit("movsbl %cl, %ecx");
                            } else {
                                Emit("movzbl %cl, %ecx");
                            }

                            Emit("pushl %ecx");
                            Emit($"{op}b ${size}, (%eax)");
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else if (type.IsRealFloatingType()) {
                    Emit("fld1");
                    LoadVariableAddress("%eax");
                    if (type.IsBasicType(BasicType.TypeKind.Float)) {
                        Emit("flds (%eax)");
                        Emit("sub $4, %esp");
                        Emit("fsts (%esp)");
                    } else if (type.IsBasicType(BasicType.TypeKind.Double)) {
                        Emit("fldl (%eax)");
                        Emit("sub $8, %esp");
                        Emit("fstl (%esp)");
                    } else {
                        throw new NotImplementedException();
                    }

                    Emit($"f{op}p");
                    if (type.IsBasicType(BasicType.TypeKind.Float)) {
                        Emit("fstps (%eax)");
                    } else if (type.IsBasicType(BasicType.TypeKind.Double)) {
                        Emit("fstpl (%eax)");
                    } else {
                        throw new NotImplementedException();
                    }

                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
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
                        Push(new Value { Kind = Value.ValueKind.Ref, Type = type, Label = ret.Label, Offset = (int)(ret.Offset + offset) });
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
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare64("ja", "jb", "ja");
                        } else {
                            GenCompare64("jg", "jl", "ja");
                        }
                    } else {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare32("seta");
                        } else {
                            GenCompare32("setg");
                        }
                    }
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    GenCompare32("seta");
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();
                    Emit("fcomip");
                    Emit("fstp %st(0)");
                    Emit("seta %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                } else {
                    throw new NotImplementedException();
                }

                Push(new Value { Kind = Value.ValueKind.Temp, Type = type });
            }

            public void LessThan(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare64("jb", "ja", "jb");
                        } else {
                            GenCompare64("jl", "jg", "jb");
                        }
                    } else {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare32("setb");
                        } else {
                            GenCompare32("setl");
                        }
                    }
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    GenCompare32("setb");
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();
                    Emit("fcomip");
                    Emit("fstp %st(0)");
                    Emit("setb %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                } else {
                    throw new NotImplementedException();
                }

                Push(new Value { Kind = Value.ValueKind.Temp, Type = type });
            }

            public void GreatOrEqual(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare64("ja", "jb", "jae");
                        } else {
                            GenCompare64("jg", "jl", "jae");
                        }
                    } else {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare32("setae");
                        } else {
                            GenCompare32("setge");
                        }
                    }
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    GenCompare32("setae");
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();
                    Emit("fcomip");
                    Emit("fstp %st(0)");
                    Emit("setae	%al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                } else {
                    throw new NotImplementedException();
                }

                Push(new Value { Kind = Value.ValueKind.Temp, Type = type });
            }

            public void LessOrEqual(CType type) {
                var rhs = Peek(0);
                var lhs = Peek(1);

                if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                    if (lhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt) ||
                        rhs.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare64("jb", "ja", "jbe");
                        } else {
                            GenCompare64("jl", "jg", "jbe");
                        }
                    } else {
                        if (lhs.Type.IsUnsignedIntegerType() && rhs.Type.IsUnsignedIntegerType()) {
                            GenCompare32("setbe");
                        } else {
                            GenCompare32("setle");
                        }
                    }
                } else if (lhs.Type.IsPointerType() && rhs.Type.IsPointerType()) {
                    GenCompare32("setbe");
                } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                           (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                    FpuPush();
                    FpuPush();
                    Emit("fcomip");
                    Emit("fstp %st(0)");
                    Emit("setbe %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                } else {
                    throw new NotImplementedException();
                }

                Push(new Value { Kind = Value.ValueKind.Temp, Type = type });
            }

            public void Address(CType type) {
                var operand = Peek(0);

                switch (operand.Kind) {
                    case Value.ValueKind.Var:
                    case Value.ValueKind.Ref:
                        Emit($"leal {VarRefToAddrExpr(operand)}, %eax");
                        Emit("pushl %eax");
                        Pop();
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type });
                        break;
                    case Value.ValueKind.Address:
                        Pop();
                        Push(new Value { Kind = Value.ValueKind.Temp, Type = type });
                        break;
                    case Value.ValueKind.Temp:
                        // nothing
                        break;
                    case Value.ValueKind.IntConst:
                        Pop();
                        Push(new Value { Kind = Value.ValueKind.IntConst, Type = type, IntConst = operand.IntConst });
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            public void UnaryMinus(CType type) {
                var operand = Peek(0);
                if (operand.Type.IsIntegerType()) {
                    if (operand.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%edx");
                        Emit("negl	%eax");
                        Emit("adcl	$0, %edx");
                        Emit("negl	%edx");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                    } else {
                        LoadI32("%eax");
                        Emit("negl %eax");
                        Emit("pushl %eax");
                    }

                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type });
                } else if (operand.Type.IsRealFloatingType()) {
                    FpuPush();
                    Emit("fchs");
                    FpuPop(type);
                    //Push(new Value() { Kind = Value.ValueKind.Temp, Type = type });
                } else {
                    throw new NotImplementedException();
                }
            }

            public void UnaryBitNot(CType type) {
                var operand = Peek(0);
                if (operand.Type.IsIntegerType()) {
                    if (operand.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                        LoadI64("%eax", "%edx");
                        Emit("notl	%eax");
                        Emit("notl	%edx");
                        Emit("pushl %edx");
                        Emit("pushl %eax");
                    } else {
                        LoadI32("%eax");
                        Emit("notl %eax");
                        Emit("pushl %eax");
                    }

                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type });
                } else {
                    throw new NotImplementedException();
                }
            }

            public void UnaryLogicalNot(CType type) {
                var operand = Peek(0);
                if (operand.Type.IsRealFloatingType()) {
                    var value = new Value { Kind = Value.ValueKind.IntConst, Type = CType.CreateSignedInt(), IntConst = 0 };
                    Push(value);
                    Eq(CType.CreateSignedInt());
                } else if (operand.Type.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                    LoadI64("%eax", "%edx");
                    Emit("orl %edx, %eax");
                    Emit("cmpl $0, %eax");
                    Emit("sete %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type });
                } else {
                    LoadI32("%eax");
                    Emit("cmpl $0, %eax");
                    Emit("sete %al");
                    Emit("movzbl %al, %eax");
                    Emit("pushl %eax");
                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type });
                }

            }

            private void PrefixOp(CType type, string op) {
                if (type.IsBoolType()) {

                    LoadVariableAddress("%eax");

                    // load value
                    switch (type.Sizeof()) {
                        case 8:
                            var subop = (op == "add") ? "adc" : "sbb";
                            if (op == "add") {
                                Emit($"movl $1, 0(%eax)");
                                Emit($"movl $0, 4(%eax)");
                            } else {
                                Emit($"xorl $1, 0(%eax)");
                                Emit($"movl $0, 4(%eax)");
                            }
                            Emit("pushl 4(%eax)");
                            Emit("pushl 0(%eax)");
                            break;
                        case 4:
                            if (op == "add") {
                                Emit($"movl $1, (%eax)");
                            } else {
                                Emit($"xorl $1, (%eax)");
                            }
                            Emit("pushl (%eax)");
                            break;
                        case 2:
                            if (op == "add") {
                                Emit($"movw $1, (%eax)");
                            } else {
                                Emit($"xorw $1, (%eax)");
                            }
                            Emit("movw (%eax), %cx");
                            Emit($"movzwl %cx, %ecx");
                            Emit($"pushl %ecx");
                            break;
                        case 1:
                            if (op == "add") {
                                Emit($"movb $1, (%eax)");
                            } else {
                                Emit($"xorb $1, (%eax)");
                            }
                            Emit("movb (%eax), %cl");
                            Emit($"movzbl %cl, %ecx");
                            Emit($"pushl %ecx");
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });

                } else if (type.IsIntegerType() || type.IsPointerType()) {
                    CType baseType;
                    int size;
                    if (type.IsPointerType(out baseType) && !baseType.IsVoidType()) {
                        size = baseType.Sizeof();
                    } else {
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
                            } else {
                                Emit("movzwl (%eax), %ecx");
                            }

                            Emit("pushl %ecx");
                            break;
                        case 1:
                            Emit($"{op}b ${size}, (%eax)");
                            if (type.IsSignedIntegerType()) {
                                Emit("movsbl (%eax), %ecx");
                            } else {
                                Emit("movzbl (%eax), %ecx");
                            }

                            Emit("pushl %ecx");
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type });
                } else if (type.IsRealFloatingType()) {
                    Emit("fld1");
                    LoadVariableAddress("%eax");
                    if (type.IsBasicType(BasicType.TypeKind.Float)) {
                        Emit("flds (%eax)");
                    } else if (type.IsBasicType(BasicType.TypeKind.Double)) {
                        Emit("fldl (%eax)");
                    } else {
                        throw new NotImplementedException();
                    }

                    Emit($"f{op}p");
                    if (type.IsBasicType(BasicType.TypeKind.Float)) {
                        Emit("sub $4, %esp");
                        Emit("fsts (%esp)");
                        Emit("fstps (%eax)");
                    } else if (type.IsBasicType(BasicType.TypeKind.Double)) {
                        Emit("sub $8, %esp");
                        Emit("fstl (%esp)");
                        Emit("fstpl (%eax)");
                    } else {
                        throw new NotImplementedException();
                    }

                    Push(new Value { Kind = Value.ValueKind.Temp, Type = type, StackPos = _stack.Count });
                } else {
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
                Push(new Value { Kind = Value.ValueKind.Address, Type = type });
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
                    } else if (value.Type.IsUnsignedIntegerType()) {
                        switch (value.Type.Sizeof()) {
                            case 1:
                                LoadI32("%eax");
                                Emit("movzbl %al, %eax");
                                break;
                            case 2:
                                LoadI32("%eax");
                                Emit("movzwl %ax, %eax");
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
                    } else if (value.Type.IsRealFloatingType()) {
                        FpuPush();
                    } else {
                        if (value.Type.Sizeof() <= 4) {
                            LoadI32("%eax");
                        } else {
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
                if (condType.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
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
                } else {
                    Emit($"cmp ${caseValue}, %eax");
                    Emit($"je {label}");
                }
            }

            public void Switch(CType condType, Action<CodeGenerator> p) {
                if (condType.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt)) {
                    LoadI64("%eax", "%edx");
                } else {
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





        protected class SyntaxTreeCompileVisitor : SyntaxTree.IVisitor<Value, Value> {
            /*
             * コード生成:
             *  - スタック計算機
             *  - とにかく元ASTに忠実なコードを作る
             *
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

            /// <summary>
            /// コンパイラコンテキスト
            /// </summary>
            private readonly CodeGenerator _context;

            public SyntaxTreeCompileVisitor(CodeGenerator context) {
                _context = context;
            }

            public Value OnArgumentDeclaration(Declaration.ArgumentDeclaration self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnFunctionDeclaration(Declaration.FunctionDeclaration self, Value value) {
                if (self.Body != null) {
                    // 引数表
                    var ft = self.Type as FunctionType;
                    int offset = 8; // prev return position

                    // 戻り値領域へのポインタ
                    if (!ft.ResultType.IsVoidType() && ft.ResultType.Sizeof() > 4 && (!ft.ResultType.IsRealFloatingType() && !ft.ResultType.IsBasicType(BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt))) {
                        offset += 4;
                    }

                    // 引数（先頭から）
                    _context.Arguments.Clear();
                    var vars = new List<string>();
                    if (ft.Arguments != null) {
                        if (ft.Arguments.Length == 1 && ft.Arguments[0].Type.IsVoidType()) {
                            // skip
                            vars.Add($"#   type=void");
                        } else {
                            foreach (var arg in ft.Arguments) {
                                vars.Add($"#   name={arg.Ident.Raw}, type={arg.Type.ToString()}, address={offset}(%ebp)");
                                _context.Arguments.Add(arg.Ident.Raw, offset);
                                offset += CodeGenerator.StackAlign(arg.Type.Sizeof());
                            }
                        }
                    }

                    // ラベル
                    _context.GenericLabels.Clear();
                    _context.Emit("");
                    _context.Emit("# function: ");
                    _context.Emit($"#   {self.Ident}");
                    _context.Emit("# args: ");
                    vars.ForEach(x => _context.Emit(x));
                    _context.Emit("# return:");
                    _context.Emit($"#   {ft.ResultType.ToString()}");
                    _context.Emit("# location:");
                    _context.Emit($"#   {self.LocationRange}");
                    _context.Emit(".section .text");
                    if (self.LinkageObject.Linkage == LinkageKind.ExternalLinkage) {
                        _context.Emit($".globl {self.LinkageObject.LinkageId}");
                    }
                    _context.Emit($"{self.LinkageObject.LinkageId}:");
                    _context.Emit("pushl %ebp");
                    _context.Emit("movl %esp, %ebp");
                    _context.Emit("pushl %ebx");
                    _context.Emit("pushl %esi");
                    _context.Emit("pushl %edi");
                    var c = _context.Emit(".error \"Stack size is need backpatch.\""); // スタックサイズは仮置き
                    _localScopeTotalSize = 4 * 3;   // %ebx,%esi,%edi分
                    _maxLocalScopeTotalSize = 4 * 3;
                    self.Body.Accept(this, value);  // 本体のコード生成を実行
                    c.Body = $"subl ${_maxLocalScopeTotalSize - 4 * 3}, %esp # alloc stack"; // スタックサイズをバックパッチ
                    _context.Emit("popl %edi");
                    _context.Emit("popl %esi");
                    _context.Emit("popl %ebx");
                    _context.Emit("movl %ebp, %esp");
                    _context.Emit("popl %ebp");
                    _context.Emit("ret");
                    _context.Emit("");
                }

                return value;
            }

            public Value OnTypeDeclaration(Declaration.TypeDeclaration self, Value value) {
                // なにもしない
                return value;
            }

            public Value OnVariableDeclaration(Declaration.VariableDeclaration self, Value value) {
                // ブロックスコープ変数
                if (self.LinkageObject.Linkage == LinkageKind.NoLinkage && self.StorageClass != StorageClassSpecifier.Static) {
                    if (self.Init != null) {
                        Tuple<string, int> offset;
                        if (_localScope.TryGetValue(self.Ident, out offset) == false) {
                            throw new Exception("初期化対象変数が見つからない。");
                        }

                        _context.Emit($"# {self.LocationRange}");
                        return self.Init.Accept(this, new Value { Kind = Value.ValueKind.Var, Label = offset.Item1, Offset = offset.Item2, Type = self.Type });
                    }
                }

                return value;
            }

            public Value OnAdditiveExpression(Expression.AdditiveExpression self, Value value) {
                self.Lhs.Accept(this, value);
                self.Rhs.Accept(this, value);
                switch (self.Op) {
                    case Expression.AdditiveExpression.OperatorKind.Add:
                        _context.Add(self.Type);
                        break;
                    case Expression.AdditiveExpression.OperatorKind.Sub:
                        _context.Sub(self.Type);
                        break;
                }

                return value;
            }

            public Value OnAndExpression(Expression.BitExpression.AndExpression self, Value value) {
                self.Lhs.Accept(this, value);
                self.Rhs.Accept(this, value);
                _context.And(self.Type);
                return value;
            }

            public Value OnCompoundAssignmentExpression(Expression.AssignmentExpression.CompoundAssignmentExpression self, Value value) {
                self.Lhs.Accept(this, value);
                _context.Dup(0);
                self.Rhs.Accept(this, value);
                switch (self.Op) {
                    case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.ADD_ASSIGN:
                        _context.Add(self.Type);
                        break;
                    case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.SUB_ASSIGN:
                        _context.Sub(self.Type);
                        break;
                    case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MUL_ASSIGN:
                        _context.Mul(self.Type);
                        break;
                    case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.DIV_ASSIGN:
                        _context.Div(self.Type);
                        break;
                    case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MOD_ASSIGN:
                        _context.Mod(self.Type);
                        break;
                    case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.AND_ASSIGN:
                        _context.And(self.Type);
                        break;
                    case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.OR_ASSIGN:
                        _context.Or(self.Type);
                        break;
                    case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.XOR_ASSIGN:
                        _context.Xor(self.Type);
                        break;
                    case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.LEFT_ASSIGN:
                        _context.Shl(self.Type);
                        break;
                    case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.RIGHT_ASSIGN:
                        _context.Shr(self.Type);
                        break;
                    default:
                        throw new Exception("来ないはず");
                }
                if (self.Type.IsBoolType()) {
                    // _Bool型は特別扱い
                    _context.CastTo(CType.CreateBool());
                }

                _context.Dup(1);
                _context.Assign(self.Type);
                _context.Discard();
                return value;
            }

            public Value OnSimpleAssignmentExpression(Expression.AssignmentExpression.SimpleAssignmentExpression self, Value value) {
                self.Rhs.Accept(this, value);
                self.Lhs.Accept(this, value);

                _context.Assign(self.Type);
                return value;
            }

            public Value OnCastExpression(Expression.CastExpression self, Value value) {
                self.Expr.Accept(this, value);
                _context.CastTo(self.Type);
                return value;
            }

            public Value OnCommaExpression(Expression.CommaExpression self, Value value) {
                bool needDiscard = false;
                foreach (var e in self.Expressions) {
                    if (needDiscard) {
                        _context.Discard(); // スタック上の結果を捨てる
                    }
                    e.Accept(this, value);
                    needDiscard = !e.Type.IsVoidType();
                }
                return value;
            }

            public Value OnConditionalExpression(Expression.ConditionalExpression self, Value value) {
                self.CondExpr.Accept(this, value);

                var elseLabel = _context.LabelAlloc();
                var junctionLabel = _context.LabelAlloc();

                _context.JmpFalse(elseLabel);

                self.ThenExpr.Accept(this, value);
                if (self.Type.IsVoidType()) {
                    _context.Discard(); // スタック上の結果を捨てる
                } else {
                    _context.LoadValueToStack(self.Type);
                    _context.Pop();
                }

                _context.Jmp(junctionLabel);
                _context.Label(elseLabel);

                self.ElseExpr.Accept(this, value);
                if (self.Type.IsVoidType()) {
                    // スタック上の結果を捨てる
                    _context.Discard();
                } else {
                    _context.LoadValueToStack(self.Type);
                    _context.Pop();
                }

                _context.Label(junctionLabel);

                if (self.Type.IsVoidType()) {
                    _context.Push(new Value { Kind = Value.ValueKind.Void });
                } else {
                    _context.Push(new Value { Kind = Value.ValueKind.Temp, Type = self.Type });
                }

                return value;
            }

            public Value OnEqualityExpression(Expression.EqualityExpression self, Value value) {
                self.Lhs.Accept(this, value);
                self.Rhs.Accept(this, value);
                switch (self.Op) {
                    case Expression.EqualityExpression.OperatorKind.Equal:
                        _context.Eq(self.Type);
                        break;
                    case Expression.EqualityExpression.OperatorKind.NotEqual:
                        _context.Ne(self.Type);
                        break;
                    case Expression.EqualityExpression.OperatorKind.None:
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return value;
            }

            public Value OnExclusiveOrExpression(Expression.BitExpression.ExclusiveOrExpression self, Value value) {
                self.Lhs.Accept(this, value);
                self.Rhs.Accept(this, value);
                _context.Xor(self.Type);
                return value;
            }

            public Value OnGccStatementExpression(Expression.GccStatementExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnInclusiveOrExpression(Expression.BitExpression.InclusiveOrExpression self, Value value) {
                self.Lhs.Accept(this, value);
                self.Rhs.Accept(this, value);
                _context.Or(self.Type);
                return value;
            }

            public Value OnIntegerPromotionExpression(Expression.IntegerPromotionExpression self, Value value) {
                self.Expr.Accept(this, value);
                _context.CastTo(self.Type);
                return value;
            }

            public Value OnLogicalAndExpression(Expression.LogicalAndExpression self, Value value) {
                var labelFalse = _context.LabelAlloc();
                var labelJunction = _context.LabelAlloc();
                self.Lhs.Accept(this, value);
                _context.JmpFalse(labelFalse);
                self.Rhs.Accept(this, value);
                _context.JmpFalse(labelFalse);
                _context.EmitLoadTrue();
                _context.Jmp(labelJunction);
                _context.Label(labelFalse);
                _context.EmitLoadFalse();
                _context.Label(labelJunction);
                _context.Push(new Value { Kind = Value.ValueKind.Temp, Type = self.Type });
                return value;
            }

            public Value OnLogicalOrExpression(Expression.LogicalOrExpression self, Value value) {
                var labelTrue = _context.LabelAlloc();
                var labelJunction = _context.LabelAlloc();
                self.Lhs.Accept(this, value);
                _context.JmpTrue(labelTrue);
                self.Rhs.Accept(this, value);
                _context.JmpTrue(labelTrue);
                _context.EmitLoadFalse();
                _context.Jmp(labelJunction);
                _context.Label(labelTrue);
                _context.EmitLoadTrue();
                _context.Label(labelJunction);
                _context.Push(new Value { Kind = Value.ValueKind.Temp, Type = self.Type });
                return value;
            }

            public Value OnMultiplicativeExpression(Expression.MultiplicativeExpression self, Value value) {
                switch (self.Op) {
                    case Expression.MultiplicativeExpression.OperatorKind.Mul:
                        self.Lhs.Accept(this, value);
                        self.Rhs.Accept(this, value);
                        _context.Mul(self.Type);
                        break;
                    case Expression.MultiplicativeExpression.OperatorKind.Div:
                        self.Lhs.Accept(this, value);
                        self.Rhs.Accept(this, value);
                        _context.Div(self.Type);
                        break;
                    case Expression.MultiplicativeExpression.OperatorKind.Mod:
                        self.Lhs.Accept(this, value);
                        self.Rhs.Accept(this, value);
                        _context.Mod(self.Type);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                return value;
            }

            public Value OnArraySubscriptingExpression(Expression.PostfixExpression.ArraySubscriptingExpression self, Value value) {
                self.Target.Accept(this, value);
                self.Index.Accept(this, value);
                _context.ArraySubscript(self.Type);
                return value;
            }

            public Value OnFunctionCallExpression(Expression.PostfixExpression.FunctionCallExpression self, Value value) {
                /*
                 *  - 関数への引数は右から左の順でスタックに積まれる。
                 *    - 引数にはベースポインタ相対でアクセスする
                 *  - 関数の戻り値は EAXに格納できるサイズならば EAX に格納される。EAXに格納できないサイズならば、戻り値を格納する領域のアドレスを引数の上に積み、EAXを使わない。（※）
                 *  - 呼び出された側の関数ではEAX, ECX, EDXのレジスタの元の値を保存することなく使用してよい。
                 *    呼び出し側の関数では必要ならば呼び出す前にそれらのレジスタをスタック上などに保存する。
                 *  - スタックポインタの処理は呼び出し側で行う。  
                 */
                var funcType = self.Expr.Type.GetBasePointerType().Unwrap() as FunctionType;

                _context.Call(self.Type, funcType, self.Args.Count, g => {
                    self.Expr.Accept(this, value);
                }, (g, i) => {
                    self.Args[i].Accept(this, value);
                });


                // 戻り値が構造体型/共用体型の場合、スタック上に配置すると不味いのでテンポラリ変数を確保してコピーする
                var obj = _context.Peek(0);
                if (obj.Kind == Value.ValueKind.Temp && (obj.Type.IsStructureType() || obj.Type.IsUnionType())) {
                    int size = CodeGenerator.StackAlign(obj.Type.Sizeof());
                    _localScopeTotalSize += size;
                    var ident = $"<temp:{_localScope.Count()}>";
                    var tp = Tuple.Create((string)null, -_localScopeTotalSize);
                    _localScope.Add(ident, tp);
                    _context.Emit($"# temp  : name={ident} address={-_localScopeTotalSize}(%ebp) type={obj.Type.ToString()}");

                    if (size <= 4) {
                        _context.Emit($"leal {-_localScopeTotalSize}(%ebp), %esi");
                        _context.Emit("pop (%esi)");
                    } else {
                        _context.Emit($"movl %esp, %esi");
                        _context.Emit($"addl ${size}, %esp");
                        _context.Emit($"leal {-_localScopeTotalSize}(%ebp), %edi");
                        _context.Emit($"movl ${size}, %ecx");
                        _context.Emit("cld");
                        _context.Emit("rep movsb");
                    }
                    _context.Pop();
                    _context.Push(new Value { Kind = Value.ValueKind.Var, Type = obj.Type, Label = tp.Item1, Offset = tp.Item2 });

                }

                return value;
            }

            public Value OnMemberDirectAccess(Expression.PostfixExpression.MemberDirectAccess self, Value value) {
                self.Expr.Accept(this, value);
                _context.DirectMember(self.Type, self.Ident.Raw);
                return value;
            }

            public Value OnMemberIndirectAccess(Expression.PostfixExpression.MemberIndirectAccess self, Value value) {
                self.Expr.Accept(this, value);
                _context.IndirectMember(self.Type, self.Ident.Raw);
                return value;
            }

            public Value OnUnaryPostfixExpression(Expression.PostfixExpression.UnaryPostfixExpression self, Value value) {
                self.Expr.Accept(this, value);
                switch (self.Op) {
                    case Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Dec:
                        _context.PostDec(self.Type);
                        break;
                    case Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Inc:
                        _context.PostInc(self.Type);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                return value;
            }

            public Value OnCharacterConstant(Expression.PrimaryExpression.Constant.CharacterConstant self, Value value) {
                _context.Push(new Value { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.Value });
                return value;
            }

            public Value OnFloatingConstant(Expression.PrimaryExpression.Constant.FloatingConstant self, Value value) {
                _context.Push(new Value { Kind = Value.ValueKind.FloatConst, Type = self.Type, FloatConst = self.Value });
                return value;
            }

            public Value OnIntegerConstant(Expression.PrimaryExpression.Constant.IntegerConstant self, Value value) {
                _context.Push(new Value { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.Value });
                return value;
            }

            public Value OnEnclosedInParenthesesExpression(Expression.PrimaryExpression.EnclosedInParenthesesExpression self, Value value) {
                return self.ParenthesesExpression.Accept(this, value);
            }

            public Value OnAddressConstantExpression(Expression.PrimaryExpression.AddressConstantExpression self, Value value) {
                self.Identifier.Accept(this, value);
                _context.CalcConstAddressOffset(self.Type, self.Offset.Value);
                return value;
            }

            public Value OnEnumerationConstant(Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, Value value) {
                _context.Push(new Value { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.Info.Value });
                return value;
            }

            public Value OnFunctionExpression(Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, Value value) {
                _context.Push(new Value { Kind = Value.ValueKind.Ref, Type = self.Type, Label = self.Decl.LinkageObject.LinkageId, Offset = 0 });
                return value;
            }

            public Value OnUndefinedIdentifierExpression(Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnArgumentExpression(Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, Value value) {
                _context.Push(new Value { Kind = Value.ValueKind.Var, Type = self.Type, Label = null, Offset = _context.Arguments[self.Ident] });
                return value;
            }

            public Value OnVariableExpression(Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, Value value) {
                if (self.Decl.LinkageObject.Linkage == LinkageKind.NoLinkage) {
                    Tuple<string, int> offset;
                    if (_localScope.TryGetValue(self.Ident, out offset)) {
                        _context.Push(new Value { Kind = Value.ValueKind.Var, Type = self.Type, Label = offset.Item1, Offset = offset.Item2 });
                    } else {
                        throw new Exception("");
                    }
                } else {
                    _context.Push(new Value { Kind = Value.ValueKind.Var, Type = self.Type, Label = self.Decl.LinkageObject.LinkageId, Offset = 0 });
                }

                return value;
            }

            public Value OnStringExpression(Expression.PrimaryExpression.StringExpression self, Value value) {
                int no = _context.DataBlock.Count;
                var label = $"D{no}";
                _context.DataBlock.Add(Tuple.Create(label, self.Value.ToArray()));
                _context.Push(new Value { Kind = Value.ValueKind.Ref, Type = self.Type, Offset = 0, Label = label });
                return value;
            }

            public Value OnRelationalExpression(Expression.RelationalExpression self, Value value) {
                self.Lhs.Accept(this, value);
                self.Rhs.Accept(this, value);
                switch (self.Op) {
                    case Expression.RelationalExpression.OperatorKind.GreaterThan:
                        _context.GreatThan(self.Type);
                        break;
                    case Expression.RelationalExpression.OperatorKind.LessThan:
                        _context.LessThan(self.Type);
                        break;
                    case Expression.RelationalExpression.OperatorKind.GreaterOrEqual:
                        _context.GreatOrEqual(self.Type);
                        break;
                    case Expression.RelationalExpression.OperatorKind.LessOrEqual:
                        _context.LessOrEqual(self.Type);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                return value;
            }

            public Value OnShiftExpression(Expression.ShiftExpression self, Value value) {
                self.Lhs.Accept(this, value);
                self.Rhs.Accept(this, value);

                switch (self.Op) {
                    case Expression.ShiftExpression.OperatorKind.Left:
                        _context.Shl(self.Type);
                        break;
                    case Expression.ShiftExpression.OperatorKind.Right:
                        _context.Shr(self.Type);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                return value;
            }

            public Value OnSizeofExpression(Expression.SizeofExpression self, Value value) {
                // todo: C99可変長配列型
                _context.Push(new Value { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.ExprOperand.Type.Sizeof() });
                return value;
            }

            public Value OnSizeofTypeExpression(Expression.SizeofTypeExpression self, Value value) {
                // todo: C99可変長配列型
                _context.Push(new Value { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.TypeOperand.Sizeof() });
                return value;
            }


            public Value OnTypeConversionExpression(Expression.TypeConversionExpression self, Value value) {
                self.Expr.Accept(this, value);
                _context.CastTo(self.Type);
                return value;
            }

            public Value OnUnaryAddressExpression(Expression.UnaryAddressExpression self, Value value) {
                self.Expr.Accept(this, value);
                _context.Address(self.Type);
                return value;
            }

            public Value OnUnaryMinusExpression(Expression.UnaryMinusExpression self, Value value) {
                self.Expr.Accept(this, value);
                _context.UnaryMinus(self.Type);
                return value;
            }

            public Value OnUnaryNegateExpression(Expression.UnaryNegateExpression self, Value value) {
                self.Expr.Accept(this, value);
                _context.UnaryBitNot(self.Type);
                return value;
            }

            public Value OnUnaryNotExpression(Expression.UnaryNotExpression self, Value value) {
                self.Expr.Accept(this, value);
                _context.UnaryLogicalNot(self.Type);
                return value;
            }

            public Value OnUnaryPlusExpression(Expression.UnaryPlusExpression self, Value value) {
                self.Expr.Accept(this, value);
                return value;
            }

            public Value OnUnaryPrefixExpression(Expression.UnaryPrefixExpression self, Value value) {
                self.Expr.Accept(this, value);

                switch (self.Op) {
                    case Expression.UnaryPrefixExpression.OperatorKind.Inc:
                        _context.PreInc(self.Type);
                        break;
                    case Expression.UnaryPrefixExpression.OperatorKind.Dec:
                        _context.PreDec(self.Type);
                        break;
                    default:
                        throw new Exception("来ないはず");
                }

                return value;
            }

            public Value OnUnaryReferenceExpression(Expression.UnaryReferenceExpression self, Value value) {
                CType bt;
                self.Expr.Accept(this, value);
                if (self.Expr.Type.IsPointerType(out bt) && bt.IsFunctionType()) {
                    // そのままの値を返す
                } else {
                    _context.Reference(self.Type);
                }
                return value;
            }

            public Value OnComplexInitializer(Initializer.ComplexInitializer self, Value value) {
                throw new Exception("来ないはず");
            }

            public Value OnSimpleInitializer(Initializer.SimpleInitializer self, Value value) {
                throw new Exception("来ないはず");
            }

            public Value OnSimpleAssignInitializer(Initializer.SimpleAssignInitializer self, Value value) {
                self.Expr.Accept(this, value);
                _context.Push(value);
                _context.Assign(self.Type);
                _context.Discard();
                return value;
            }

            public Value OnArrayAssignInitializer(Initializer.ArrayAssignInitializer self, Value value) {
                var elementSize = self.Type.ElementType.Sizeof();
                var v = new Value(value) { Type = self.Type.ElementType };
                var writed = 0;
                foreach (var init in self.Inits) {
                    init.Accept(this, v);
                    switch (v.Kind) {
                        case Value.ValueKind.Var:
                            if (v.Label == null) {
                                v.Offset += elementSize;
                                writed += elementSize;
                            } else {
                                throw new NotImplementedException();
                            }

                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }

                while (self.Type.Sizeof() > writed) {
                    var sz = Math.Min((self.Type.Sizeof() - writed), 4);
                    switch (sz) {
                        case 4:
                            _context.Push(new Value() { IntConst = 0, Kind = Value.ValueKind.IntConst, Type = CType.CreateUnsignedInt() });
                            _context.Push(v);
                            _context.Assign(CType.CreateUnsignedInt());
                            _context.Discard();
                            break;
                        case 3:
                        case 2:
                            _context.Push(new Value() { IntConst = 0, Kind = Value.ValueKind.IntConst, Type = CType.CreateUnsignedShortInt() });
                            _context.Push(v);
                            _context.Assign(CType.CreateUnsignedShortInt());
                            _context.Discard();
                            break;
                        case 1:
                            _context.Push(new Value() { IntConst = 0, Kind = Value.ValueKind.IntConst, Type = CType.CreateUnsignedChar() });
                            _context.Push(v);
                            _context.Assign(CType.CreateUnsignedChar());
                            _context.Discard();
                            break;
                    }
                    v.Offset += sz;
                    writed += sz;
                }

                return new Value { Kind = Value.ValueKind.Void };
            }

            public Value OnStructUnionAssignInitializer(Initializer.StructUnionAssignInitializer self, Value value) {
                // value に初期化先変数位置が入っているので戦闘から順にvalueを適切に設定して再帰呼び出しすればいい。
                // 共用体は初期化式が一つのはず
                Value v = new Value(value);
                var baseoffset = v.Offset;
                var writed = 0;
                foreach (var member in self.Type.Members.Zip(self.Inits, Tuple.Create)) {
                    v.Offset = baseoffset + member.Item1.Offset;
                    member.Item2.Accept(this, v);
                    BitFieldType bft;
                    if (member.Item1.Type.IsBitField(out bft)) {
                        writed = member.Item1.Offset + (bft.BitOffset + bft.BitWidth + 7) / 8;
                    } else {
                        writed = member.Item1.Offset + member.Item1.Type.Sizeof();
                    }
                }
                v.Offset = baseoffset + writed;
                while (self.Type.Sizeof() > writed) {
                    var sz = Math.Min((self.Type.Sizeof() - writed), 4);
                    switch (sz) {
                        case 4:
                            _context.Push(new Value() { IntConst = 0, Kind = Value.ValueKind.IntConst, Type = CType.CreateUnsignedInt() });
                            _context.Push(v);
                            _context.Assign(CType.CreateUnsignedInt());
                            _context.Discard();
                            break;
                        case 3:
                        case 2:
                            _context.Push(new Value() { IntConst = 0, Kind = Value.ValueKind.IntConst, Type = CType.CreateUnsignedShortInt() });
                            _context.Push(v);
                            _context.Assign(CType.CreateUnsignedShortInt());
                            _context.Discard();
                            break;
                        case 1:
                            _context.Push(new Value() { IntConst = 0, Kind = Value.ValueKind.IntConst, Type = CType.CreateUnsignedChar() });
                            _context.Push(v);
                            _context.Assign(CType.CreateUnsignedChar());
                            _context.Discard();
                            break;
                    }
                    v.Offset += sz;
                    writed += sz;
                }
                return new Value { Kind = Value.ValueKind.Void };
            }

            public Value OnBreakStatement(Statement.BreakStatement self, Value value) {
                _context.Emit($"# {self.LocationRange}");
                var label = _context.BreakTarget.Peek();
                _context.Jmp(label);
                return new Value { Kind = Value.ValueKind.Void };
            }

            public Value OnCaseStatement(Statement.CaseStatement self, Value value) {
                _context.Emit($"# {self.LocationRange}");
                var label = _switchLabelTableStack.Peek()[self];
                _context.Label(label);
                self.Stmt.Accept(this, value);
                return new Value { Kind = Value.ValueKind.Void };
            }

            private Scope<Tuple<string, int>> _localScope = Scope<Tuple<string, int>>.Empty;
            private int _localScopeTotalSize;
            private int _maxLocalScopeTotalSize;

            public Value OnCompoundStatementC89(Statement.CompoundStatementC89 self, Value value) {
                _context.Emit($"# {self.LocationRange}");

                _localScope = _localScope.Extend();
                var prevLocalScopeSize = _localScopeTotalSize;

                _context.Emit("# enter scope");
                foreach (var x in self.Decls.Reverse<Declaration>()) {
                    if (x.LinkageObject.Linkage == LinkageKind.NoLinkage) {
                        if (x.StorageClass == StorageClassSpecifier.Static) {
                            // static
                            _localScope.Add(x.Ident, Tuple.Create(x.LinkageObject.LinkageId, 0));
                            _context.Emit($"# static: name={x.Ident} linkid={x.LinkageObject.LinkageId} type={x.Type.ToString()}");
                        } else {
                            _localScopeTotalSize += CodeGenerator.StackAlign(x.LinkageObject.Type.Sizeof());
                            _localScope.Add(x.Ident, Tuple.Create((string)null, -_localScopeTotalSize));
                            _context.Emit($"# auto  : name={x.Ident} address={-_localScopeTotalSize}(%ebp) type={x.Type.ToString()}");
                        }
                    } else if (x.LinkageObject.Linkage == LinkageKind.ExternalLinkage) {
                        _context.Emit($"# extern: name={x.Ident} linkid={x.LinkageObject.LinkageId} type={x.Type.ToString()}");
                        // externなのでスキップ
                    } else if (x.LinkageObject.Linkage == LinkageKind.InternalLinkage) {
                        _context.Emit($"# internal(filescope): name={x.Ident} linkid={x.LinkageObject.LinkageId} type={x.Type.ToString()}");
                        // externなのでスキップ
                    } else {
                        throw new NotImplementedException();
                    }
                }

                foreach (var x in self.Decls) {
                    x.Accept(this, value);
                }

                foreach (var x in self.Stmts) {
                    x.Accept(this, value);
                }

                if (_maxLocalScopeTotalSize < _localScopeTotalSize) {
                    _maxLocalScopeTotalSize = _localScopeTotalSize;
                }

                _context.Emit("# leave scope");

                _localScopeTotalSize = prevLocalScopeSize;
                _localScope = _localScope.Parent;
                return value;
            }
            public Value OnCompoundStatementC99(Statement.CompoundStatementC99 self, Value value) {
                _context.Emit($"# {self.LocationRange}");

                _localScope = _localScope.Extend();
                var prevLocalScopeSize = _localScopeTotalSize;

                _context.Emit("# enter scope");
                foreach (var x in self.DeclsOrStmts.Where(x => x is Declaration).Cast<Declaration>().Reverse<Declaration>()) {
                    if (x.LinkageObject.Linkage == LinkageKind.NoLinkage) {
                        if (x.StorageClass == StorageClassSpecifier.Static) {
                            // static
                            _localScope.Add(x.Ident, Tuple.Create(x.LinkageObject.LinkageId, 0));
                            _context.Emit($"# static: name={x.Ident} linkid={x.LinkageObject.LinkageId} type={x.Type.ToString()}");
                        } else {
                            _localScopeTotalSize += CodeGenerator.StackAlign(x.LinkageObject.Type.Sizeof());
                            _localScope.Add(x.Ident, Tuple.Create((string)null, -_localScopeTotalSize));
                            _context.Emit($"# auto  : name={x.Ident} address={-_localScopeTotalSize}(%ebp) type={x.Type.ToString()}");
                        }
                    } else if (x.LinkageObject.Linkage == LinkageKind.ExternalLinkage) {
                        _context.Emit($"# extern: name={x.Ident} linkid={x.LinkageObject.LinkageId} type={x.Type.ToString()}");
                        // externなのでスキップ
                    } else if (x.LinkageObject.Linkage == LinkageKind.InternalLinkage) {
                        _context.Emit($"# internal(filescope): name={x.Ident} linkid={x.LinkageObject.LinkageId} type={x.Type.ToString()}");
                        // externなのでスキップ
                    } else {
                        throw new NotImplementedException();
                    }
                }

                foreach (var x in self.DeclsOrStmts) {
                    x.Accept(this, value);
                }

                if (_maxLocalScopeTotalSize < _localScopeTotalSize) {
                    _maxLocalScopeTotalSize = _localScopeTotalSize;
                }

                _context.Emit("# leave scope");

                _localScopeTotalSize = prevLocalScopeSize;
                _localScope = _localScope.Parent;
                return value;
            }

            public Value OnContinueStatement(Statement.ContinueStatement self, Value value) {
                _context.Emit($"# {self.LocationRange}");
                var label = _context.ContinueTarget.Peek();
                _context.Jmp(label);
                return new Value { Kind = Value.ValueKind.Void };
            }

            public Value OnDefaultStatement(Statement.DefaultStatement self, Value value) {
                _context.Emit($"# {self.LocationRange}");
                var label = _switchLabelTableStack.Peek()[self];
                _context.Label(label);
                self.Stmt.Accept(this, value);
                return new Value { Kind = Value.ValueKind.Void };
            }

            public Value OnDoWhileStatement(Statement.DoWhileStatement self, Value value) {
                _context.Emit($"# {self.LocationRange}");
                var labelHead = _context.LabelAlloc();
                var labelContinue = _context.LabelAlloc();
                var labelBreak = _context.LabelAlloc();

                // Check Loop Condition
                _context.Label(labelHead);
                _context.ContinueTarget.Push(labelContinue);
                _context.BreakTarget.Push(labelBreak);
                self.Stmt.Accept(this, value);
                _context.ContinueTarget.Pop();
                _context.BreakTarget.Pop();

                _context.Label(labelContinue);
                self.Cond.Accept(this, value);
                _context.JmpTrue(labelHead);
                _context.Label(labelBreak);
                return new Value { Kind = Value.ValueKind.Void };
            }

            public Value OnEmptyStatement(Statement.EmptyStatement self, Value value) {
                return new Value { Kind = Value.ValueKind.Void };
            }

            public Value OnExpressionStatement(Statement.ExpressionStatement self, Value value) {
                _context.Emit($"# {self.LocationRange}");
                self.Expr.Accept(this, value);
                _context.Discard();
                return value;
            }

            public Value OnForStatement(Statement.ForStatement self, Value value) {
                _context.Emit($"# {self.LocationRange}");
                // Initialize
                if (self.Init != null) {
                    self.Init.Accept(this, value);
                    _context.Discard();
                }

                var labelHead = _context.LabelAlloc();
                var labelContinue = _context.LabelAlloc();
                var labelBreak = _context.LabelAlloc();

                // Check Loop Condition
                _context.Label(labelHead);
                if (self.Cond != null) {
                    self.Cond.Accept(this, value);
                    _context.JmpFalse(labelBreak);
                }

                _context.ContinueTarget.Push(labelContinue);
                _context.BreakTarget.Push(labelBreak);
                self.Stmt.Accept(this, value);
                _context.ContinueTarget.Pop();
                _context.BreakTarget.Pop();

                _context.Label(labelContinue);
                if (self.Update != null) {
                    self.Update.Accept(this, value);
                    _context.Discard();
                }

                _context.Jmp(labelHead);
                _context.Label(labelBreak);

                return new Value { Kind = Value.ValueKind.Void };
            }

            public Value OnGenericLabeledStatement(Statement.GenericLabeledStatement self, Value value) {
                _context.Emit($"# {self.LocationRange}");
                if (_context.GenericLabels.ContainsKey(self.Ident) == false) {
                    _context.GenericLabels[self.Ident] = _context.LabelAlloc();
                }

                _context.Label(_context.GenericLabels[self.Ident]);
                self.Stmt.Accept(this, value);
                return new Value { Kind = Value.ValueKind.Void };
            }

            public Value OnGotoStatement(Statement.GotoStatement self, Value value) {
                _context.Emit($"# {self.LocationRange}");
                if (_context.GenericLabels.ContainsKey(self.Label) == false) {
                    _context.GenericLabels[self.Label] = _context.LabelAlloc();
                }

                _context.Jmp(_context.GenericLabels[self.Label]);
                return new Value { Kind = Value.ValueKind.Void };
            }

            public Value OnIfStatement(Statement.IfStatement self, Value value) {
                _context.Emit($"# {self.LocationRange}");
                self.Cond.Accept(this, value);
                if (self.ElseStmt != null) {
                    var elseLabel = _context.LabelAlloc();
                    var junctionLabel = _context.LabelAlloc();

                    _context.JmpFalse(elseLabel);

                    self.ThenStmt.Accept(this, value);
                    _context.Jmp(junctionLabel);
                    _context.Label(elseLabel);
                    self.ElseStmt.Accept(this, value);
                    _context.Label(junctionLabel);
                } else {
                    var junctionLabel = _context.LabelAlloc();

                    _context.JmpFalse(junctionLabel);

                    self.ThenStmt.Accept(this, value);
                    _context.Label(junctionLabel);
                }


                return new Value { Kind = Value.ValueKind.Void };
            }

            public Value OnReturnStatement(Statement.ReturnStatement self, Value value) {
                _context.Emit($"# {self.LocationRange}");
                self.Expr?.Accept(this, value);
                _context.Return(self.Expr?.Type);

                return new Value { Kind = Value.ValueKind.Void };
            }

            private readonly Stack<Dictionary<Statement, string>> _switchLabelTableStack = new Stack<Dictionary<Statement, string>>();

            public Value OnSwitchStatement(Statement.SwitchStatement self, Value value) {
                _context.Emit($"# {self.LocationRange}");
                var labelBreak = _context.LabelAlloc();

                self.Cond.Accept(this, value);

                var labelDic = new Dictionary<Statement, string>();
                _context.Switch(self.Cond.Type, g => {
                    foreach (var caseLabel in self.CaseLabels) {
                        var caseValue = caseLabel.Value;
                        var label = _context.LabelAlloc();
                        labelDic.Add(caseLabel, label);
                        _context.Case(self.Cond.Type, caseValue, label);
                    }
                });
                if (self.DefaultLabel != null) {
                    var label = _context.LabelAlloc();
                    labelDic.Add(self.DefaultLabel, label);
                    _context.Jmp(label);
                } else {
                    _context.Jmp(labelBreak);
                }

                _switchLabelTableStack.Push(labelDic);
                _context.BreakTarget.Push(labelBreak);
                self.Stmt.Accept(this, value);
                _context.BreakTarget.Pop();
                _switchLabelTableStack.Pop();
                _context.Label(labelBreak);

                return new Value { Kind = Value.ValueKind.Void };
            }

            public Value OnWhileStatement(Statement.WhileStatement self, Value value) {
                _context.Emit($"# {self.LocationRange}");
                var labelContinue = _context.LabelAlloc();
                var labelBreak = _context.LabelAlloc();

                // Check Loop Condition
                _context.Label(labelContinue);
                self.Cond.Accept(this, value);
                _context.JmpFalse(labelBreak);
                _context.ContinueTarget.Push(labelContinue);
                _context.BreakTarget.Push(labelBreak);
                self.Stmt.Accept(this, value);
                _context.ContinueTarget.Pop();
                _context.BreakTarget.Pop();

                _context.Jmp(labelContinue);
                _context.Label(labelBreak);

                return new Value { Kind = Value.ValueKind.Void };
            }

            public Value OnTranslationUnit(TranslationUnit self, Value value) {
                foreach (var obj in self.LinkageTable) {
                    if (obj.Definition is Declaration.VariableDeclaration) {
                        var visitor = new FileScopeInitializerVisitor(_context);
                        obj.Definition?.Accept(visitor, value);
                    }
                }

                foreach (var obj in self.LinkageTable) {
                    if (!(obj.Definition is Declaration.VariableDeclaration)) {
                        obj.Definition?.Accept(this, value);
                    }
                }

                foreach (var data in _context.DataBlock) {
                    _context.Data(data.Item1, data.Item2);
                }

                return value;
            }

            public void WriteCode(StreamWriter writer) {
                _context.Codes.ForEach(x => writer.WriteLine(x.ToString()));
            }
        }

        protected class FileScopeInitializerVisitor : SyntaxTree.IVisitor<Value, Value> {
            private readonly CodeGenerator _context;

            public void Emit(string s) {
                _context.Emit(s);
            }

            /// <summary>
            /// 初期化情報
            /// </summary>
            private struct ValueEntry {
                public int ByteOffset { get; }
                public sbyte BitOffset { get; }
                public sbyte BitSize { get; }
                public Expression Expr { get; }

                public ValueEntry(int byteOffset, sbyte bitOffset, sbyte bitSize, Expression expr) {
                    this.ByteOffset = byteOffset;
                    this.BitOffset = bitOffset;
                    this.BitSize = bitSize;
                    this.Expr = expr;
                }
            }

            /// <summary>
            /// 初期化式を元に作成した初期化対象情報
            /// </summary>
            private readonly List<ValueEntry> _initValues = new List<ValueEntry>();

            /// <summary>
            /// 初期化式の対象バイトオフセット
            /// </summary>
            private int _currentOffsetByte = 0;

            public FileScopeInitializerVisitor(CodeGenerator context) {
                _context = context;
            }

            public Value OnArgumentDeclaration(Declaration.ArgumentDeclaration self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnFunctionDeclaration(Declaration.FunctionDeclaration self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnTypeDeclaration(Declaration.TypeDeclaration self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnVariableDeclaration(Declaration.VariableDeclaration self, Value value) {
                // ファイルスコープ変数
                if (self.Init != null) {
                    Emit(".section .data");
                    self.Init.Accept(this, value);
                    Emit(".align 4");
                    if (self.LinkageObject.Linkage == LinkageKind.ExternalLinkage) {
                        _context.Emit($".globl {self.LinkageObject.LinkageId}");
                    }
                    Emit($"{self.LinkageObject.LinkageId}:");
                    foreach (var val in _initValues) {
                        var byteOffset = val.ByteOffset;
                        var bitOffset = val.BitOffset;
                        var bitSize = val.BitSize;
                        var cvalue = val.Expr.Accept(this, value);
                        switch (cvalue.Kind) {
                            case Value.ValueKind.IntConst:
                                switch (cvalue.Type.Sizeof()) {
                                    case 1:
                                        Emit($".byte {(byte)cvalue.IntConst}");
                                        break;
                                    case 2:
                                        Emit($".word {(ushort)cvalue.IntConst}");
                                        break;
                                    case 4:
                                        Emit($".long {(uint)cvalue.IntConst}");
                                        break;
                                    case 8:
                                        Emit($".long {(UInt64)cvalue.IntConst & 0xFFFFFFFFUL}, {(UInt64)(cvalue.IntConst >> 32) & 0xFFFFFFFFUL}");
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                break;
                            case Value.ValueKind.FloatConst:
                                switch (cvalue.Type.Sizeof()) {
                                    case 4: {
                                            var dwords = BitConverter.ToUInt32(BitConverter.GetBytes((float)cvalue.FloatConst), 0);
                                            Emit($".long {dwords}");
                                            break;
                                        }
                                    case 8: {
                                            var lo = BitConverter.ToUInt32(BitConverter.GetBytes((double)cvalue.FloatConst), 0);
                                            var hi = BitConverter.ToUInt32(BitConverter.GetBytes((double)cvalue.FloatConst), 4);

                                            Emit($".long {lo}, {hi}");
                                            break;
                                        }
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                break;
                            case Value.ValueKind.Var:
                            case Value.ValueKind.Ref:
                                if (cvalue.Label == null) {
                                    throw new Exception("ファイルスコープオブジェクトの参照では無い。");
                                }

                                Emit($".long {cvalue.Label}+{cvalue.Offset}");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                } else {
                    if (self.LinkageObject.Linkage == LinkageKind.ExternalLinkage) {
                        Emit($".comm {self.LinkageObject.LinkageId}, {self.LinkageObject.Type.Sizeof()}, 4");
                    } else {
                        Emit($".lcomm {self.LinkageObject.LinkageId}, {self.LinkageObject.Type.Sizeof()}, 4");
                    }
                }

                return value;
            }

            public Value OnAdditiveExpression(Expression.AdditiveExpression self, Value value) {
                var lv = self.Lhs.Accept(this, value);
                var rv = self.Rhs.Accept(this, value);
                if (lv.Kind == Value.ValueKind.IntConst && rv.Kind == Value.ValueKind.IntConst) {
                    lv.IntConst += rv.IntConst;
                    return lv;
                }
                if (lv.Kind == Value.ValueKind.Ref && rv.Kind == Value.ValueKind.IntConst) {
                    lv.Offset += (int)rv.IntConst;
                    return lv;
                }
                if (lv.Kind == Value.ValueKind.IntConst && rv.Kind == Value.ValueKind.Ref) {
                    lv.Offset += (int)rv.IntConst;
                    return lv;
                }
                throw new NotImplementedException();
            }

            public Value OnAndExpression(Expression.BitExpression.AndExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnCompoundAssignmentExpression(Expression.AssignmentExpression.CompoundAssignmentExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnSimpleAssignmentExpression(Expression.AssignmentExpression.SimpleAssignmentExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnCastExpression(Expression.CastExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnCommaExpression(Expression.CommaExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnConditionalExpression(Expression.ConditionalExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnEqualityExpression(Expression.EqualityExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnExclusiveOrExpression(Expression.BitExpression.ExclusiveOrExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnGccStatementExpression(Expression.GccStatementExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnInclusiveOrExpression(Expression.BitExpression.InclusiveOrExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnIntegerPromotionExpression(Expression.IntegerPromotionExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnLogicalAndExpression(Expression.LogicalAndExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnLogicalOrExpression(Expression.LogicalOrExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnMultiplicativeExpression(Expression.MultiplicativeExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnArraySubscriptingExpression(Expression.PostfixExpression.ArraySubscriptingExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnFunctionCallExpression(Expression.PostfixExpression.FunctionCallExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnMemberDirectAccess(Expression.PostfixExpression.MemberDirectAccess self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnMemberIndirectAccess(Expression.PostfixExpression.MemberIndirectAccess self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnUnaryPostfixExpression(Expression.PostfixExpression.UnaryPostfixExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnCharacterConstant(Expression.PrimaryExpression.Constant.CharacterConstant self, Value value) {
                return new Value { Kind = Value.ValueKind.IntConst, IntConst = self.Value, Type = self.Type };
            }

            public Value OnFloatingConstant(Expression.PrimaryExpression.Constant.FloatingConstant self, Value value) {
                return new Value { Kind = Value.ValueKind.FloatConst, FloatConst = self.Value, Type = self.Type };
            }

            public Value OnIntegerConstant(Expression.PrimaryExpression.Constant.IntegerConstant self, Value value) {
                return new Value { Kind = Value.ValueKind.IntConst, IntConst = self.Value, Type = self.Type };
            }

            public Value OnEnclosedInParenthesesExpression(Expression.PrimaryExpression.EnclosedInParenthesesExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnAddressConstantExpression(Expression.PrimaryExpression.AddressConstantExpression self, Value value) {
                if (self.Identifier is Expression.PrimaryExpression.IdentifierExpression.FunctionExpression) {
                    var f = self.Identifier as Expression.PrimaryExpression.IdentifierExpression.FunctionExpression;
                    return new Value { Kind = Value.ValueKind.Ref, Label = f.Decl.LinkageObject.LinkageId, Offset = (int)self.Offset.Value, Type = self.Type };
                }

                if (self.Identifier is Expression.PrimaryExpression.IdentifierExpression.VariableExpression) {
                    var f = self.Identifier as Expression.PrimaryExpression.IdentifierExpression.VariableExpression;
                    return new Value { Kind = Value.ValueKind.Ref, Label = f.Decl.LinkageObject.LinkageId, Offset = (int)self.Offset.Value, Type = self.Type };
                }
                if (self.Identifier == null) {
                    return new Value { Kind = Value.ValueKind.IntConst, IntConst = (int)self.Offset.Value, Type = self.Type };
                }

                throw new NotImplementedException();
            }

            public Value OnEnumerationConstant(Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnFunctionExpression(Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnUndefinedIdentifierExpression(Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnArgumentExpression(Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnVariableExpression(Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnStringExpression(Expression.PrimaryExpression.StringExpression self, Value value) {
                int no = _context.DataBlock.Count;
                var label = $"D{no}";
                _context.DataBlock.Add(Tuple.Create(label, self.Value.ToArray()));
                return new Value { Kind = Value.ValueKind.Ref, Label = label, Offset = 0, Type = self.Type };
            }

            public Value OnRelationalExpression(Expression.RelationalExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnShiftExpression(Expression.ShiftExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnSizeofExpression(Expression.SizeofExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnSizeofTypeExpression(Expression.SizeofTypeExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnTypeConversionExpression(Expression.TypeConversionExpression self, Value value) {
                var v = self.Expr.Accept(this, value);
                v.Type = self.Type;
                return v;
            }

            public Value OnUnaryAddressExpression(Expression.UnaryAddressExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnUnaryMinusExpression(Expression.UnaryMinusExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnUnaryNegateExpression(Expression.UnaryNegateExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnUnaryNotExpression(Expression.UnaryNotExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnUnaryPlusExpression(Expression.UnaryPlusExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnUnaryPrefixExpression(Expression.UnaryPrefixExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnUnaryReferenceExpression(Expression.UnaryReferenceExpression self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnComplexInitializer(Initializer.ComplexInitializer self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnSimpleInitializer(Initializer.SimpleInitializer self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnSimpleAssignInitializer(Initializer.SimpleAssignInitializer self, Value value) {
                var ret = ExpressionEvaluator.Eval(self.Expr);
                if (self.Type.IsBitField()) {
                    var bft = self.Type as BitFieldType;
                    if (ret is Expression.PrimaryExpression.Constant.IntegerConstant) {
                        _initValues.Add(new ValueEntry(_currentOffsetByte, bft.BitOffset, bft.BitWidth, ret));
                        if (bft.BitOffset + bft.BitWidth == bft.Sizeof() * 8) {
                            _currentOffsetByte += bft.Sizeof();
                        }
                    } else {
                        throw new Exception("ビットフィールドに代入できない値が使われている。");
                    }
                } else {
                    _initValues.Add(new ValueEntry(_currentOffsetByte, -1, -1, ret));
                    _currentOffsetByte += self.Type.Sizeof();
                }
                return value;
            }

            public Value OnArrayAssignInitializer(Initializer.ArrayAssignInitializer self, Value value) {

                foreach (var s in self.Inits) {
                    s.Accept(this, value);
                }

                var arrayType = self.Type.Unwrap() as ArrayType;
                var filledSize = (arrayType.Length - self.Inits.Count) * arrayType.ElementType.Sizeof();
                while (filledSize > 0) {
                    if (filledSize >= 4) {
                        _initValues.Add(new ValueEntry(_currentOffsetByte, -1, -1, (Expression)new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, BasicType.TypeKind.UnsignedLongInt)));
                        _currentOffsetByte += 4;
                        filledSize -= 4;
                    } else if (filledSize >= 2) {
                        _initValues.Add(new ValueEntry(_currentOffsetByte, -1, -1, (Expression)new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, BasicType.TypeKind.UnsignedShortInt)));
                        _currentOffsetByte += 2;
                        filledSize -= 2;
                    } else if (filledSize >= 1) {
                        _initValues.Add(new ValueEntry(_currentOffsetByte, -1, -1, (Expression)new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, BasicType.TypeKind.UnsignedChar)));
                        _currentOffsetByte += 1;
                        filledSize -= 1;
                    }
                }

                return value;
            }

            public Value OnStructUnionAssignInitializer(Initializer.StructUnionAssignInitializer self, Value value) {
                var start = _initValues.Count;

                foreach (var s in self.Inits) {
                    s.Accept(this, value);
                }

                var suType = self.Type.Unwrap() as TaggedType.StructUnionType;
                if (suType.IsStructureType()) {
                    foreach (var x in suType.Members.Skip(self.Inits.Count)) {
                        if (x.Type.IsBitField()) {
                            var bft = x.Type as BitFieldType;
                            var bt = bft.Type as BasicType;
                            _initValues.Add(new ValueEntry(_currentOffsetByte, bft.BitOffset, bft.BitWidth, (Expression)new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, bt.Kind)));
                            if (bft.BitOffset + bft.BitWidth == bft.Sizeof() * 8) {
                                _currentOffsetByte += bft.Sizeof();
                            }
                        } else {
                            var fillSize = x.Type.Sizeof();
                            while (fillSize > 0) {
                                if (fillSize >= 4) {
                                    fillSize -= 4;
                                    _initValues.Add(new ValueEntry(_currentOffsetByte, -1, -1, (Expression)new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, BasicType.TypeKind.UnsignedLongInt)));
                                    _currentOffsetByte += 4;
                                } else if (fillSize >= 2) {
                                    fillSize -= 2;
                                    _initValues.Add(new ValueEntry(_currentOffsetByte, -1, -1, (Expression)new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, BasicType.TypeKind.UnsignedShortInt)));
                                    _currentOffsetByte += 4;
                                } else if (fillSize >= 1) {
                                    fillSize -= 1;
                                    _initValues.Add(new ValueEntry(_currentOffsetByte, -1, -1, (Expression)new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, "0", 0, BasicType.TypeKind.UnsignedChar)));
                                    _currentOffsetByte += 4;
                                }
                            }
                        }
                    }
                }

                var end = _initValues.Count;

                var i = start;
                while (i < _initValues.Count) {
                    var val = _initValues[i];
                    var byteOffset = val.ByteOffset;
                    var bitOffset = val.BitOffset;
                    var bitSize = val.BitSize;
                    var expr = val.Expr;
                    if (bitSize == -1) {
                        i++;
                        continue;
                    }
                    ulong v = 0;

                    while (i < _initValues.Count && _initValues[i].ByteOffset == byteOffset) {
                        var cvalue = _initValues[i].Expr.Accept(this, value);
                        var bOffset = _initValues[i].BitOffset;
                        var bSize = _initValues[i].BitSize;
                        if (cvalue.Kind != Value.ValueKind.IntConst) {
                            // ビットフィールド中に定数式以外が使われている。
                            throw new Exception("ビットフィールドに対応する初期化子の要素が定数ではありません。");
                        }
                        ulong bits = 0;
                        switch (cvalue.Type.Sizeof()) {
                            case 1:
                                if (Specification.IsUnsignedIntegerType(cvalue.Type)) {
                                    bits = (ulong)(((byte)(((sbyte)cvalue.IntConst) << (8 - bSize))) >> (8 - bSize));
                                } else {
                                    bits = (ulong)(((byte)(((byte)cvalue.IntConst) << (8 - bSize))) >> (8 - bSize));
                                }
                                break;
                            case 2:
                                if (Specification.IsUnsignedIntegerType(cvalue.Type)) {
                                    bits = (ulong)(((ushort)(((short)cvalue.IntConst) << (16 - bSize))) >> (16 - bSize));
                                } else {
                                    bits = (ulong)(((ushort)(((ushort)cvalue.IntConst) << (16 - bSize))) >> (16 - bSize));
                                }
                                break;
                            case 4:
                                if (Specification.IsUnsignedIntegerType(cvalue.Type)) {
                                    bits = (ulong)(((uint)(((int)cvalue.IntConst) << (32 - bSize))) >> (32 - bSize));
                                } else {
                                    bits = (ulong)(((uint)(((uint)cvalue.IntConst) << (32 - bSize))) >> (32 - bSize));
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        v |= bits << bOffset;
                        _initValues.RemoveAt(i);
                    }
                    _initValues.Insert(i, new ValueEntry(byteOffset, -1, -1, (Expression)new Expression.PrimaryExpression.Constant.IntegerConstant(self.LocationRange, v.ToString(), (long)v, (expr.Type as BasicType).Kind)));

                    i++;

                }
                return value;
            }

            public Value OnBreakStatement(Statement.BreakStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnCaseStatement(Statement.CaseStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnCompoundStatementC89(Statement.CompoundStatementC89 self, Value value) {
                throw new NotImplementedException();
            }
            public Value OnCompoundStatementC99(Statement.CompoundStatementC99 self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnContinueStatement(Statement.ContinueStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnDefaultStatement(Statement.DefaultStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnDoWhileStatement(Statement.DoWhileStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnEmptyStatement(Statement.EmptyStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnExpressionStatement(Statement.ExpressionStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnForStatement(Statement.ForStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnGenericLabeledStatement(Statement.GenericLabeledStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnGotoStatement(Statement.GotoStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnIfStatement(Statement.IfStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnReturnStatement(Statement.ReturnStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnSwitchStatement(Statement.SwitchStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnWhileStatement(Statement.WhileStatement self, Value value) {
                throw new NotImplementedException();
            }

            public Value OnTranslationUnit(TranslationUnit self, Value value) {
                throw new NotImplementedException();
            }
        }

        public void Compile(Ast ret, StreamWriter o) {
            var v = new Value();
            var context = new CodeGenerator();
            var visitor = new SyntaxTreeCompileVisitor(context);
            ret.Accept(visitor, v);
            visitor.WriteCode(o);
        }
    }
}
