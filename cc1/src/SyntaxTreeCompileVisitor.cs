using System;
using System.Collections.Generic;
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
            public Value() {
            }

            public Value(Value ret) {
                this.Kind = ret.Kind;
                this.Type = ret.Type;
                this.Register = ret.Register;
                this.IntConst = ret.IntConst;
                this.FloatConst = ret.FloatConst;
                this.Label = ret.Label;
                this.Offset = ret.Offset;
            }
        }

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
        int line = 1;

        private void discard(Value v) {
            if (v.Kind == Value.ValueKind.Temp || v.Kind == Value.ValueKind.Address) {
                Emit($"addl ${(v.Type.Sizeof() + 3) & ~3}, %esp");  // discard
            }
        }

        public List<string> code = new List<string>();

        public void Emit(string code) {
            line++;
            Console.WriteLine(code);
        }


        private Value arith_add(CType type, Value lhs, Value rhs) {
            if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                LoadI(rhs, "%ecx"); // rhs
                LoadI(lhs, "%eax"); // lhs
                Emit($"addl %ecx, %eax");
                Emit($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = type };
            } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                       (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                LoadF(rhs);
                LoadF(lhs);
                Emit($"faddp");
                StoreF(type);
                return new Value() { Kind = Value.ValueKind.Temp, Type = type };
            } else {
                throw new Exception("");
            }
        }
        private Value arith_sub(CType type, Value lhs, Value rhs) {
            if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                LoadI(rhs, "%ecx"); // rhs
                LoadI(lhs, "%eax"); // lhs
                Emit($"subl %ecx, %eax");
                Emit($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = type };
            } else if ((lhs.Type.IsRealFloatingType() || lhs.Type.IsIntegerType()) &&
                       (rhs.Type.IsRealFloatingType() || rhs.Type.IsIntegerType())) {
                LoadF(rhs);
                LoadF(lhs);
                Emit($"fsubp");
                StoreF(type);
                return new Value() { Kind = Value.ValueKind.Temp, Type = type };
            } else {
                throw new Exception("");
            }
        }

        private void inc_ptr(CType type, string target, string index) {
            CType elemType = type.GetBasePointerType();
            Emit($"imull ${elemType.Sizeof()}, {index}, {index}");
            Emit($"addl {index}, {target}");
        }

        private void dec_ptr(CType type, string target, string index) {
            CType elemType = type.GetBasePointerType();
            Emit($"imull ${elemType.Sizeof()}, {index}, {index}");
            Emit($"subl {index}, {target}");
        }
        private void diff_ptr(CType type, string lhs, string rhs) {
            CType elemType = type.GetBasePointerType();
            Emit($"subl {rhs}, {lhs}");
            Emit($"cltd");
            Emit($"movl {lhs}, %edx");
            Emit($"movl ${elemType.Sizeof()}, {rhs}");
            Emit($"idivl {rhs}");
            Emit($"movl %eax, {lhs}");
        }

        private Value pointer_add(CType type, Value target, Value index) {
            CType elemType = type.GetBasePointerType();

            if (index.Kind == Value.ValueKind.IntConst) {
                switch (target.Kind) {
                    //case Value.ValueKind.Var:
                    //return new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = target.Label, Offset = (int)(target.Offset + index.IntConst * elemType.Sizeof()) };
                    case Value.ValueKind.Ref:
                        return new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = target.Label, Offset = (int)(target.Offset + index.IntConst * elemType.Sizeof()) };

                    default:
                        break;
                }
            }
            LoadI(index, "%ecx");
            LoadP(target, "%eax");
            inc_ptr(type, "%eax", "%ecx");
            Emit($"pushl %eax");

            return new Value() { Kind = Value.ValueKind.Temp, Type = type };
        }

        private Value pointer_sub(CType type, Value target, Value index) {
            CType elemType = type.GetBasePointerType();

            if (index.Kind == Value.ValueKind.IntConst) {
                switch (target.Kind) {
                    //case Value.ValueKind.Var:
                    //return new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = target.Label, Offset = (int)(target.Offset + index.IntConst * elemType.Sizeof()) };
                    case Value.ValueKind.Ref:
                        return new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = target.Label, Offset = (int)(target.Offset + index.IntConst * elemType.Sizeof()) };

                    default:
                        break;
                }
            }
            LoadI(index, "%ecx");
            LoadP(target, "%eax");
            inc_ptr(type, "%eax", "%ecx");
            Emit($"pushl %eax");

            return new Value() { Kind = Value.ValueKind.Temp, Type = type };
        }

        private Value pointer_diff(CType type, Value lhs, Value rhs) {
            LoadP(rhs, "%eax");
            LoadP(lhs, "%eax");
            diff_ptr(lhs.Type, "%eax", "%ecx");
            Emit($"pushl %eax");

            return new Value() { Kind = Value.ValueKind.Temp, Type = type };
        }

        private Value cast_to(CType type, Value ret) {
            if (ret.Type.IsIntegerType() && type.IsIntegerType()) {
                var retty = ret.Type.Unwrap() as CType.BasicType;
                CType.BasicType.TypeKind selftykind;
                if (type.Unwrap() is CType.BasicType) {
                    selftykind = (type.Unwrap() as CType.BasicType).Kind;
                } else if (type.Unwrap() is CType.TaggedType.EnumType) {
                    selftykind = CType.BasicType.TypeKind.SignedInt;
                } else {
                    throw new NotImplementedException();
                }

                LoadI(ret, "%eax");
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
                return new Value() { Kind = Value.ValueKind.Temp, Type = type };
            } else if (ret.Type.IsPointerType() && type.IsPointerType()) {
                return new Value(ret) { Type = type };
            } else if (ret.Type.IsArrayType() && type.IsPointerType()) {
                // 手抜き
                if (ret.Kind == Value.ValueKind.Var) {
                    ret = new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = ret.Label, Offset = ret.Offset };
                    return ret;
                } else if (ret.Kind == Value.ValueKind.Ref) {
                    ret = new Value() { Kind = Value.ValueKind.Ref, Type = type, Label = ret.Label, Offset = ret.Offset };
                    return ret;
                } else if (ret.Kind == Value.ValueKind.Address) {
                    ret = new Value() { Kind = Value.ValueKind.Temp, Type = type };
                    return ret;
                } else {
                    throw new NotImplementedException();
                }
            } else if (ret.Type.IsArrayType() && type.IsArrayType()) {
                return new Value(ret) { Type = type };
            } else if (ret.Type.IsPointerType() && type.IsArrayType()) {
                throw new NotImplementedException();
            } else if (ret.Type.IsIntegerType() && type.IsPointerType()) {
                return new Value(ret) { Type = type };
            } else if (ret.Type.IsPointerType() && type.IsIntegerType()) {
                return new Value(ret) { Type = type };
            } else if (ret.Type.IsRealFloatingType() && type.IsRealFloatingType()) {
                LoadF(ret);
                StoreF(type);
                return new Value() { Kind = Value.ValueKind.Temp, Type = type };
            } else if (ret.Type.IsRealFloatingType() && type.IsIntegerType()) {
                LoadF(ret);

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

                return new Value() { Kind = Value.ValueKind.Temp, Type = type };
            } else if (ret.Type.IsIntegerType() && type.IsRealFloatingType()) {
                var retty = ret.Type.Unwrap() as CType.BasicType;
                CType.BasicType.TypeKind selftykind;
                if (type.Unwrap() is CType.BasicType) {
                    selftykind = (type.Unwrap() as CType.BasicType).Kind;
                } else if (type.Unwrap() is CType.TaggedType.EnumType) {
                    selftykind = CType.BasicType.TypeKind.SignedInt;
                } else {
                    throw new NotImplementedException();
                }

                LoadF(ret);
                StoreF(type);
                return new Value() { Kind = Value.ValueKind.Temp, Type = type };

            } else {
                throw new NotImplementedException();
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
                foreach (var arg in ft.Arguments) {
                    arguments.Add(arg.Ident, offset);
                    offset += (arg.Type.Sizeof() + 3) & ~3;
                }

                // ラベル
                genericlabels = new Dictionary<string, string>();

                Emit($".section .text");
                Emit($".globl {self.LinkageObject.LinkageId}");
                Emit($"{self.LinkageObject.LinkageId}:");
                Emit($"pushl %ebp");
                Emit($"movl %esp, %ebp");
                self.Body.Accept(this, value);
                Emit($"movl %ebp, %esp");
                Emit($"popl %ebp");
                Emit($"ret");
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
                    return self.Init.Accept(this, new Value() { Kind = Value.ValueKind.Var, Label = offset.Item1, Offset = offset.Item2 });
                }
            }
            return value;
        }

        public Value OnAdditiveExpression(SyntaxTree.Expression.AdditiveExpression self, Value value) {
            if ((self.Lhs.Type.IsRealFloatingType() || self.Lhs.Type.IsIntegerType()) &&
                (self.Rhs.Type.IsRealFloatingType() || self.Rhs.Type.IsIntegerType())) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                switch (self.Op) {
                    case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add:
                        return arith_add(self.Type, lhs, rhs);
                    case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub:
                        return arith_sub(self.Type, lhs, rhs);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } else if (self.Lhs.Type.IsPointerType() && self.Rhs.Type.IsIntegerType()) {
                var target = self.Lhs.Accept(this, value);
                var index = self.Rhs.Accept(this, value);

                switch (self.Op) {
                    case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add:
                        return pointer_add(self.Type, target, index);
                    case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub:
                        return pointer_sub(self.Type, target, index);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } else if (self.Lhs.Type.IsIntegerType() && self.Rhs.Type.IsPointerType()) {
                var target = self.Rhs.Accept(this, value);
                var index = self.Lhs.Accept(this, value);

                switch (self.Op) {
                    case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add:
                        return pointer_add(self.Type, target, index);
                    case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub:
                        return pointer_sub(self.Type, target, index);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } else if (self.Lhs.Type.IsPointerType() && self.Rhs.Type.IsPointerType()) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);

                switch (self.Op) {
                    case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub:
                        return pointer_diff(self.Type, lhs, rhs);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnAndExpression(SyntaxTree.Expression.AndExpression self, Value value) {
            var lhs = self.Lhs.Accept(this, value);
            var rhs = self.Rhs.Accept(this, value);
            if (self.Type.IsIntegerType()) {
                LoadI(rhs, "%eax"); // rhs
                LoadI(lhs, "%ecx"); // lhs
                Emit($"andl %ecx, %eax");
                Emit($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnCompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression self, Value value) {
            if (self.Type.IsIntegerType()) {
                Emit($"pushl %edi");

                var rhs = self.Rhs.Accept(this, value);
                var lhs = self.Lhs.Accept(this, value);

                LoadVA(lhs, "%edi");
                LoadI(rhs, "%ecx"); // rhs

                switch (self.Op) {
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.ADD_ASSIGN:
                        Emit($"addl (%edi), %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.SUB_ASSIGN:
                        Emit($"subl (%edi), %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MUL_ASSIGN:
                        Emit($"mov (%edi), %eax");
                        if (self.Type.IsSignedIntegerType()) {
                            Emit($"imull %ecx");
                        } else {    
                            Emit($"mull %ecx");
                        }

                        Emit($"mov %eax, %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.DIV_ASSIGN:
                        Emit($"mov (%edi), %eax");
                        Emit($"cltd");
                        if (self.Type.IsSignedIntegerType()) {
                            Emit($"idivl %ecx");
                        } else {
                            Emit($"divl %ecx");
                        }

                        Emit($"mov %eax, %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MOD_ASSIGN:
                        Emit($"mov (%edi), %eax");
                        if (self.Type.IsSignedIntegerType()) {
                            Emit($"idivl %ecx");
                        } else {
                            Emit($"divl %ecx");
                        }

                        Emit($"mov %edx, %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.AND_ASSIGN:
                        Emit($"andl (%edi), %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.OR_ASSIGN:
                        Emit($"orl (%edi), %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.XOR_ASSIGN:
                        Emit($"xorl (%edi), %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.LEFT_ASSIGN:
                        if (self.Type.IsSignedIntegerType()) {
                            Emit($"sall (%edi), %ecx");
                        } else {
                            Emit($"shll (%edi), %ecx");
                        }

                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.RIGHT_ASSIGN:
                        if (self.Type.IsSignedIntegerType()) {
                            Emit($"sarl (%edi), %ecx");
                        } else {
                            Emit($"shrl (%edi), %ecx");
                        }

                        break;
                    default:
                        throw new Exception("来ないはず");
                }

                Emit($"movl %ecx, (%edi)");

                Emit($"popl %edi");

                Emit($"pushl %ecx");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (self.Type.IsPointerType()) {
                var rhs = self.Rhs.Accept(this, value);
                var lhs = self.Lhs.Accept(this, value);

                LoadVA(lhs, "%eax");    // target
                LoadI(rhs, "%ecx"); // index

                switch (self.Op) {
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.ADD_ASSIGN:
                        Emit($"movl (%eax), %edx");
                        inc_ptr(self.Type, "%edx", "%ecx");
                        Emit($"movl %edx, (%eax)");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.SUB_ASSIGN:
                        Emit($"movl (%eax), %edx");
                        dec_ptr(self.Type, "%edx", "%ecx");
                        Emit($"movl %edx, (%eax)");
                        break;
                    default:
                        throw new Exception("来ないはず");
                }
                Emit($"pushl %ecx");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (self.Type.IsRealFloatingType()) {

                var rhs = self.Rhs.Accept(this, value);
                var lhs = self.Lhs.Accept(this, value);

                LoadVA(lhs, "%eax");
                LoadF(rhs); // rhs


                {
                    string op = "";
                    if (lhs.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Float)) {
                        op = "flds";
                    } else {
                        op = "fldl";
                    }

                    Emit($"{op} (%eax)");
                }

                var op1 = "";
                var op2 = "";

                if (self.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                    Emit($"subl $4, %esp");
                    op1 = "fsts";
                    op2 = "fstps";
                } else {
                    Emit($"subl $8, %esp");
                    op1 = "fstl";
                    op2 = "fstpl";
                }
                Emit($"movl %ecx, (%eax)");
                switch (self.Op) {
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.ADD_ASSIGN:
                        Emit($"faddp");
                        Emit($"{op1} (%eax)");
                        Emit($"{op2} (%esp)");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.SUB_ASSIGN:
                        Emit($"fsubp");
                        Emit($"{op1} (%eax)");
                        Emit($"{op2} (%esp)");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MUL_ASSIGN:
                        Emit($"fmulp");
                        Emit($"{op1} (%eax)");
                        Emit($"{op2} (%esp)");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.DIV_ASSIGN:
                        Emit($"fdivp");
                        Emit($"{op1} (%eax)");
                        Emit($"{op2} (%esp)");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MOD_ASSIGN:
                        throw new NotImplementedException();
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.AND_ASSIGN:
                        throw new NotImplementedException();
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.OR_ASSIGN:
                        throw new NotImplementedException();
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.XOR_ASSIGN:
                        throw new NotImplementedException();
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.LEFT_ASSIGN:
                        throw new NotImplementedException();
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.RIGHT_ASSIGN:
                        throw new NotImplementedException();
                        break;
                    default:
                        throw new Exception("来ないはず");
                }
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnSimpleAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, Value value) {
            var rhs = self.Rhs.Accept(this, value);
            var lhs = self.Lhs.Accept(this, value);

            LoadVA(lhs, "%eax");

            switch (self.Type.Sizeof()) {
                case 1:
                    rhs = LoadI(rhs, "%ecx");
                    Emit($"movb %cl, (%eax)");
                    Emit($"pushl (%eax)");
                    break;
                case 2:
                    rhs = LoadI(rhs, "%ecx");
                    Emit($"movw %cx, (%eax)");
                    Emit($"pushl (%eax)");
                    break;
                case 3:
                case 4:
                    rhs = LoadI(rhs, "%ecx");
                    Emit($"movl %ecx, (%eax)");
                    Emit($"pushl (%eax)");
                    break;
                default:
                    LoadS(rhs);
                    Emit($"movl %esp, %esi");
                    Emit($"movl ${self.Type.Sizeof()}, %ecx");
                    Emit($"movl %eax, %edi");
                    Emit($"cld");
                    Emit($"rep movsb");
                    // スタックトップにrhsの値があるのでpushは不要
                    break;
            }


            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnCastExpression(SyntaxTree.Expression.CastExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            return cast_to(self.Type, ret);
        }

        public Value OnCommaExpression(SyntaxTree.Expression.CommaExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnConditionalExpression(SyntaxTree.Expression.ConditionalExpression self, Value value) {
            var cond = self.CondExpr.Accept(this, value);
            LoadI(cond, "%eax");
            Emit($"cmpl $0, %eax");

            var elseLabel = LAlloc();
            var junctionLabel = LAlloc();

            Emit($"je {elseLabel}");

            var thenRet = self.ThenExpr.Accept(this, value);
            if (cond.Type.IsVoidType()) {
                discard(thenRet);
            } else {
                // スタック上に結果をロードする
                LoadS(thenRet);
            }


            Emit($"jmp {junctionLabel}");
            Emit($"{elseLabel}:");
            var elseRet = self.ElseExpr.Accept(this, value);
            if (cond.Type.IsVoidType()) {
                discard(elseRet);
            } else {
                // スタック上に結果をロードする
                LoadS(elseRet);
            }
            Emit($"{junctionLabel}:");

            if (cond.Type.IsVoidType()) {
                return new Value() { Kind = Value.ValueKind.Void };
            } else {
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            }
        }

        public Value OnEqualityExpression(SyntaxTree.Expression.EqualityExpression self, Value value) {
            var op = "";
            switch (self.Op) {
                case SyntaxTree.Expression.EqualityExpression.OperatorKind.Equal:
                    op = "sete";
                    break;
                case SyntaxTree.Expression.EqualityExpression.OperatorKind.NotEqual:
                    op = "setne";
                    break;
                default:
                    throw new NotImplementedException();

            }
            if ((self.Lhs.Type.IsIntegerType() || self.Lhs.Type.IsPointerType()) ||
                (self.Rhs.Type.IsIntegerType() || self.Rhs.Type.IsPointerType())) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                LoadI(rhs, "%eax");
                LoadI(lhs, "%ecx");
                Emit($"cmpl %ecx, %eax");

                Emit($"{op} %al");
                Emit($"movzbl %al, %eax");
                Emit($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if ((self.Lhs.Type.IsRealFloatingType() || self.Lhs.Type.IsIntegerType()) &&
                       (self.Rhs.Type.IsRealFloatingType() || self.Rhs.Type.IsIntegerType())) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);

                LoadF(rhs);
                LoadF(lhs);

                Emit($"fcomip");
                Emit($"fstp %st(0)");

                Emit($"{op} %al");
                Emit($"movzbl %al, %eax");
                Emit($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };

            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnExclusiveOrExpression(SyntaxTree.Expression.ExclusiveOrExpression self, Value value) {
            if (self.Lhs.Type.IsIntegerType() && self.Rhs.Type.IsIntegerType()) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                LoadI(rhs, "%ecx"); // rhs
                LoadI(lhs, "%eax"); // lhs
                Emit($"xorl %ecx, %eax");
                Emit($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            }

            throw new NotImplementedException();
        }

        public Value OnGccStatementExpression(SyntaxTree.Expression.GccStatementExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnInclusiveOrExpression(SyntaxTree.Expression.InclusiveOrExpression self, Value value) {
            if (self.Lhs.Type.IsIntegerType() && self.Rhs.Type.IsIntegerType()) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                LoadI(rhs, "%ecx"); // rhs
                LoadI(lhs, "%eax"); // lhs
                Emit($"orl %ecx, %eax");
                Emit($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            }

            throw new NotImplementedException();
        }

        public Value OnIntegerPromotionExpression(SyntaxTree.Expression.IntegerPromotionExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            if (ret.Type.IsIntegerType() && self.Type.IsIntegerType()) {
                return cast_to(self.Type, ret);
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnLogicalAndExpression(SyntaxTree.Expression.LogicalAndExpression self, Value value) {
            var labelFalse = LAlloc();
            var labelJunction = LAlloc();
            var lhs = self.Lhs.Accept(this, value);
            LoadI(lhs, "%eax");
            Emit($"cmpl $0, %eax");
            Emit($"je {labelFalse}");
            var rhs = self.Rhs.Accept(this, value);
            LoadI(rhs, "%eax");
            Emit($"cmpl $0, %eax");
            Emit($"je {labelFalse}");
            Emit($"pushl $1");
            Emit($"jmp {labelJunction}");
            Emit($"{labelFalse}:");
            Emit($"pushl $0");
            Emit($"{labelJunction}:");
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnLogicalOrExpression(SyntaxTree.Expression.LogicalOrExpression self, Value value) {
            var labelTrue = LAlloc();
            var labelJunction = LAlloc();
            var lhs = self.Lhs.Accept(this, value);
            LoadI(lhs, "%eax");
            Emit($"cmpl $0, %eax");
            Emit($"jne {labelTrue}");
            var rhs = self.Rhs.Accept(this, value);
            LoadI(rhs, "%eax");
            Emit($"cmpl $0, %eax");
            Emit($"jne {labelTrue}");
            Emit($"pushl $0");
            Emit($"jmp {labelJunction}");
            Emit($"{labelTrue}:");
            Emit($"pushl $1");
            Emit($"{labelJunction}:");
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnMultiplicitiveExpression(SyntaxTree.Expression.MultiplicitiveExpression self, Value value) {
            if (self.Lhs.Type.IsIntegerType() && self.Rhs.Type.IsIntegerType()) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                LoadI(rhs, "%ecx"); // rhs
                LoadI(lhs, "%eax"); // lhs
                switch (self.Op) {
                    case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mul:
                        if (self.Type.IsSignedIntegerType()) {
                            Emit($"imull %ecx");
                        } else {
                            Emit($"mull %ecx");
                        }
                        Emit($"push %eax");
                        break;
                    case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Div:
                        Emit($"cltd");
                        if (self.Type.IsSignedIntegerType()) {
                            Emit($"idivl %ecx");
                        } else {
                            Emit($"divl %ecx");
                        }
                        Emit($"push %eax");
                        break;
                    case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mod:
                        Emit($"cltd");
                        if (self.Type.IsSignedIntegerType()) {
                            Emit($"idivl %ecx");
                        } else {
                            Emit($"divl %ecx");
                        }
                        Emit($"push %edx");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if ((self.Lhs.Type.IsRealFloatingType() || self.Lhs.Type.IsIntegerType()) &&
                       (self.Rhs.Type.IsRealFloatingType() || self.Rhs.Type.IsIntegerType())) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);

                LoadF(rhs);
                LoadF(lhs);

                switch (self.Op) {
                    case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mul:
                        Emit($"fmulp");
                        break;
                    case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Div:
                        Emit($"fdivp");
                        break;
                    case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mod:
                        throw new NotImplementedException();
                        break;
                    default:
                        throw new NotImplementedException();
                }

                StoreF(self.Type);

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            }
            throw new NotImplementedException();
        }

        public Value OnArraySubscriptingExpression(SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression self, Value value) {
            var target = self.Target.Accept(this, value);
            var index = self.Index.Accept(this, value);

            LoadI(index, "%ecx");
            if (target.Type.IsPointerType()) {
                LoadP(target, "%eax");
            } else {
                LoadVA(target, "%eax");
            }
            Emit($"imull ${self.Type.Sizeof()}, %ecx, %ecx");
            Emit($"leal (%eax, %ecx), %eax");
            Emit($"pushl %eax");

            return new Value() { Kind = Value.ValueKind.Address, Type = self.Type };
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

            var funcType = (self.Expr.Type as CType.PointerType).BaseType as CType.FunctionType;

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
            foreach (var x in self.Args.Reverse<SyntaxTree.Expression>()) {
                var _argSize = (x.Type.Sizeof() + 3) & ~3;
                argSize += _argSize;
                var a = x.Accept(this, value);
                LoadS(a);
            }

            // 戻り値が浮動小数点数ではなく、eaxにも入らないならスタック上に格納先アドレスを積む
            if (resultSize > 4 && !funcType.ResultType.IsRealFloatingType()) {
                Emit($"leal {argSize + bakSize}(%esp), %eax");
                Emit($"push %eax");
            }

            var func = self.Expr.Accept(this, value);
            LoadI(func, "%eax");
            Emit($"call *%eax");

            if (resultSize > 4) {
                if (funcType.ResultType.IsRealFloatingType()) {
                    // 浮動小数点数はFPUスタック上にある
                    if (self.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                        Emit($"fstps {(argSize + bakSize)}(%esp)");
                    } else if (self.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
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

            return new Value() { Kind = resultSize == 0 ? Value.ValueKind.Void : Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnMemberDirectAccess(SyntaxTree.Expression.PostfixExpression.MemberDirectAccess self, Value value) {
            var ret = self.Expr.Accept(this, value);
            var offset = 0;
            var st = self.Expr.Type.Unwrap() as CType.TaggedType.StructUnionType;
            CType.TaggedType.StructUnionType.MemberInfo target = null;
            foreach (var member in st.Members) {
                if (member.Ident == self.Ident) {
                    target = member;
                    break;
                }

                if (st.Kind == CType.TaggedType.StructUnionType.StructOrUnion.Struct) {
                    offset += member.Type.Sizeof();
                }
            }

            LoadVA(ret, "%eax");
            Emit($"addl ${offset}, %eax");
            Emit($"pushl %eax");
            return new Value() { Kind = Value.ValueKind.Address, Type = self.Type };
        }

        public Value OnMemberIndirectAccess(SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess self, Value value) {
            var ret = self.Expr.Accept(this, value);
            var offset = 0;
            var st = self.Expr.Type.GetBasePointerType().Unwrap() as CType.TaggedType.StructUnionType;
            CType.TaggedType.StructUnionType.MemberInfo target = null;
            foreach (var member in st.Members) {
                if (member.Ident == self.Ident) {
                    target = member;
                    break;
                }

                if (st.Kind == CType.TaggedType.StructUnionType.StructOrUnion.Struct) {
                    offset += member.Type.Sizeof();
                }
            }

            LoadP(ret, "%eax");
            Emit($"addl ${offset}, %eax");
            Emit($"pushl %eax");
            return new Value() { Kind = Value.ValueKind.Address, Type = self.Type };

        }

        public Value OnUnaryPostfixExpression(SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);

            LoadVA(ret, "%eax");

            var op = "";
            switch (self.Op) {
                case SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Dec:
                    op = "sub";
                    break;
                case SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Inc:
                    op = "add";
                    break;
                default:
                    throw new NotImplementedException();
            }

            CType baseType;
            int size;
            if (ret.Type.IsPointerType(out baseType) && !baseType.IsVoidType()) {
                size = baseType.Sizeof();
            } else {
                size = 1;
            }

            // load value
            switch (ret.Type.Sizeof()) {
                case 4:
                    Emit($"movl (%eax), %ecx");
                    Emit($"{op}l ${size}, (%eax)");
                    break;
                case 2:
                    Emit($"movw (%eax), %cx");
                    if (ret.Type.IsSignedIntegerType()) {
                        Emit($"movswl %cx, %ecx");
                    } else {
                        Emit($"movzwl %cx, %ecx");
                    }
                    Emit($"{op}w ${size}, (%eax)");
                    break;
                case 1:
                    Emit($"movb (%eax), %cl");
                    if (ret.Type.IsSignedIntegerType()) {
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
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnCharacterConstant(SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant self, Value value) {
            return new Value() { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.Value };
        }

        public Value OnFloatingConstant(SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant self, Value value) {
            return new Value() { Kind = Value.ValueKind.FloatConst, Type = self.Type, FloatConst = self.Value };
        }

        public Value OnIntegerConstant(SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant self, Value value) {
            return new Value() { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.Value };
        }

        public Value OnEnclosedInParenthesesExpression(SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression self, Value value) {
            return self.ParenthesesExpression.Accept(this, value);
        }

        public Value OnAddressConstantExpression(SyntaxTree.Expression.PrimaryExpression.AddressConstantExpression self, Value value) {
            var ret = self.Identifier.Accept(this, value);
            switch (ret.Kind) {
                case Value.ValueKind.Var:
                    return new Value() { Kind = Value.ValueKind.Ref, Type = self.Type, Label = ret.Label, Offset = (int)(ret.Offset + self.Offset.Value) };
                case Value.ValueKind.Ref:
                    return new Value() { Kind = Value.ValueKind.Ref, Type = self.Type, Label = ret.Label, Offset = (int)(ret.Offset + self.Offset.Value) };
                default:
                    throw new NotImplementedException();
            }
        }

        public Value OnEnumerationConstant(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, Value value) {
            return new Value() { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.Info.Value };
        }

        public Value OnFunctionExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, Value value) {
            return new Value() { Kind = Value.ValueKind.Ref, Type = self.Type, Label = self.Decl.LinkageObject.LinkageId, Offset = 0 };
        }

        public Value OnUndefinedIdentifierExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnArgumentExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, Value value) {
            return new Value() { Kind = Value.ValueKind.Var, Type = self.Type, Label = null, Offset = arguments[self.Ident] };
        }

        public Value OnVariableExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, Value value) {
            Tuple<string, int> offset;
            if (self.Decl.LinkageObject.Linkage == LinkageKind.NoLinkage) {
                if (localScope.TryGetValue(self.Ident, out offset)) {
                    return new Value() { Kind = Value.ValueKind.Var, Type = self.Type, Label = offset.Item1, Offset = offset.Item2, };
                } else {
                    throw new Exception("");
                }
            } else {
                return new Value() { Kind = Value.ValueKind.Var, Type = self.Type, Label = self.Decl.LinkageObject.LinkageId, Offset = 0 };
            }
        }

        public Value OnStringExpression(SyntaxTree.Expression.PrimaryExpression.StringExpression self, Value value) {
            int no = dataBlock.Count;
            var label = $"D{no}";
            dataBlock.Add(Tuple.Create(label, self.Value.ToArray()));
            return new Value() { Kind = Value.ValueKind.Ref, Type = self.Type, Offset = 0, Label = label };
        }

        public Value OnRelationalExpression(SyntaxTree.Expression.RelationalExpression self, Value value) {
            if ((self.Lhs.Type.IsIntegerType() && self.Rhs.Type.IsIntegerType()) || (self.Lhs.Type.IsPointerType() && self.Rhs.Type.IsPointerType())) {

                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);

                LoadI(rhs, "%ecx");
                LoadI(lhs, "%eax");
                Emit("cmpl %ecx, %eax");

                switch (self.Op) {
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterThan:
                        Emit("setg	%al");
                        break;
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessThan:
                        Emit("setl	%al");
                        break;
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterOrEqual:
                        Emit("setge %al");
                        break;
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessOrEqual:
                        Emit("setle %al");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                Emit("movzbl %al, %eax");
                Emit($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if ((self.Lhs.Type.IsRealFloatingType() || self.Lhs.Type.IsIntegerType()) &&
                       (self.Rhs.Type.IsRealFloatingType() || self.Rhs.Type.IsIntegerType())) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);

                LoadF(rhs);
                LoadF(lhs);
                Emit($"fcomip");
                Emit($"fstp %st(0)");
                switch (self.Op) {
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterThan:
                        Emit("seta	%al");
                        break;
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessThan:
                        Emit("setb	%al");
                        break;
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterOrEqual:
                        Emit("setae %al");
                        break;
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessOrEqual:
                        Emit("setbe %al");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                Emit($"movzbl %al, %eax");
                Emit($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };

            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnShiftExpression(SyntaxTree.Expression.ShiftExpression self, Value value) {
            var lhs = self.Lhs.Accept(this, value);
            var rhs = self.Rhs.Accept(this, value);

            LoadI(rhs, "%ecx");
            LoadI(lhs, "%eax");

            string op = "";
            switch (self.Op) {
                case SyntaxTree.Expression.ShiftExpression.OperatorKind.Left:
                    if (self.Type.IsSignedIntegerType()) {
                        op = "sall";
                    } else {
                        op = "shll";
                    }
                    break;
                case SyntaxTree.Expression.ShiftExpression.OperatorKind.Right:
                    if (self.Type.IsSignedIntegerType()) {
                        op = "sarl";
                    } else {
                        op = "shrl";
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
            // %eaxがサイズ違反か調べたほうがいい
            Emit($"{op} %cl, %eax");
            Emit($"pushl %eax");
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnSizeofExpression(SyntaxTree.Expression.SizeofExpression self, Value value) {
            // todo: C99可変長配列型
            return new Value() { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.ExprOperand.Type.Sizeof() };
        }

        public Value OnSizeofTypeExpression(SyntaxTree.Expression.SizeofTypeExpression self, Value value) {
            // todo: C99可変長配列型
            return new Value() { Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.TypeOperand.Sizeof() };
        }



        public Value OnTypeConversionExpression(SyntaxTree.Expression.TypeConversionExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            return cast_to(self.Type, ret);
        }

        public Value OnUnaryAddressExpression(SyntaxTree.Expression.UnaryAddressExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            if (operand.Kind == Value.ValueKind.Var) {
                if (operand.Label == null) {
                    Emit($"leal {operand.Offset}(%ebp), %eax");
                } else {
                    Emit($"leal {operand.Label}+{operand.Offset}, %eax");
                }
                Emit($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (operand.Kind == Value.ValueKind.Ref) {
                if (operand.Label == null) {
                    Emit($"leal {operand.Offset}(%ebp), %eax");
                } else {
                    Emit($"leal {operand.Label}+{operand.Offset}, %eax");
                }
                Emit($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (operand.Kind == Value.ValueKind.Address) {
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (operand.Kind == Value.ValueKind.Temp) {
                return operand;
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnUnaryMinusExpression(SyntaxTree.Expression.UnaryMinusExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            if (operand.Type.IsIntegerType()) {
                operand = LoadI(operand, "%eax");
                Emit($"negl %eax");
                Emit($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (operand.Type.IsRealFloatingType()) {
                LoadF(operand);
                Emit($"fchs");
                StoreF(self.Type);
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnUnaryNegateExpression(SyntaxTree.Expression.UnaryNegateExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            operand = LoadI(operand, "%eax");
            Emit($"notl %eax");
            Emit($"pushl %eax");
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnUnaryNotExpression(SyntaxTree.Expression.UnaryNotExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            operand = LoadI(operand, "%eax");
            Emit($"cmpl $0, %eax");
            Emit($"sete %al");
            Emit($"movzbl %al, %eax");
            Emit($"pushl %eax");
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnUnaryPlusExpression(SyntaxTree.Expression.UnaryPlusExpression self, Value value) {
            return self.Expr.Accept(this, value);
        }

        public Value OnUnaryPrefixExpression(SyntaxTree.Expression.UnaryPrefixExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            // load address
            LoadVA(ret, "%eax");

            string op = "";
            switch (self.Op) {
                case SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Inc:
                    op = "add";
                    break;
                case SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Dec:
                    op = "sub";
                    break;
                default:
                    throw new Exception("来ないはず");
            }

            CType baseType;
            int size;
            if (ret.Type.IsPointerType(out baseType) && !baseType.IsVoidType()) {
                size = baseType.Sizeof();
            } else {
                size = 1;
            }

            // load value
            switch (ret.Type.Sizeof()) {
                case 4:
                    Emit($"movl (%eax), %ecx");
                    Emit($"{op}l ${size}, (%eax)");
                    break;
                case 2:
                    Emit($"movw (%eax), %cx");
                    if (ret.Type.IsSignedIntegerType()) {
                        Emit($"movswl %cx, %ecx");
                    } else {
                        Emit($"movzwl %cx, %ecx");
                    }
                    Emit($"{op}w ${size}, (%eax)");
                    break;
                case 1:
                    Emit($"movb (%eax), %cl");
                    if (ret.Type.IsSignedIntegerType()) {
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
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };

        }

        public Value OnUnaryReferenceExpression(SyntaxTree.Expression.UnaryReferenceExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            LoadI(ret, "%eax");
            Emit($"pushl %eax");
            return new Value() { Kind = Value.ValueKind.Address, Type = self.Type };
        }

        public Value OnComplexInitializer(SyntaxTree.Initializer.ComplexInitializer self, Value value) {
            throw new Exception("来ないはず");
        }

        public Value OnSimpleInitializer(SyntaxTree.Initializer.SimpleInitializer self, Value value) {
            throw new Exception("来ないはず");
        }

        public Value OnSimpleAssignInitializer(SyntaxTree.Initializer.SimpleAssignInitializer self, Value value) {
            var rhs = self.Expr.Accept(this, value);


            var dst = "";
            switch (value.Kind) {
                case Value.ValueKind.Var:
                    if (value.Label == null) {
                        Emit($"leal {value.Offset}(%ebp), %eax");
                    } else {
                        throw new NotImplementedException();
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (self.Type.IsIntegerType()) {
                LoadI(rhs, "%ecx");
                switch (self.Type.Sizeof()) {
                    case 1:
                        Emit($"movb %cl, (%eax)");
                        break;
                    case 2:
                        Emit($"movw %cx, (%eax)");
                        break;
                    case 4:
                        Emit($"movl %ecx, (%eax)");
                        break;
                    default:
                        throw new Exception();
                }
            } else {
                switch (rhs.Kind) {
                    case Value.ValueKind.FloatConst: {
                            if (self.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                                var bytes = BitConverter.GetBytes((float)rhs.FloatConst);
                                var qwordlo = BitConverter.ToUInt32(bytes, 0);
                                Emit($"movl ${qwordlo}, 0(%eax)");
                            } else {
                                var bytes = BitConverter.GetBytes(rhs.FloatConst);
                                var qwordlo = BitConverter.ToUInt32(bytes, 0);
                                var qwordhi = BitConverter.ToUInt32(bytes, 4);
                                Emit($"movl ${qwordlo}, 0(%eax)");
                                Emit($"movl ${qwordhi}, 4(%eax)");
                            }
                            return new Value() { Kind = Value.ValueKind.Void };
                        }
                    case Value.ValueKind.Var:
                        if (rhs.Label == null) {
                            Emit($"leal {rhs.Offset}(%ebp), %esi");
                        } else {
                            Emit($"leal {rhs.Label}+{rhs.Offset}, %esi");
                        }
                        break;
                    case Value.ValueKind.Ref:
                        if (rhs.Label == null) {
                            Emit($"leal {rhs.Offset}(%ebp), %esi");
                        } else {
                            Emit($"leal {rhs.Label}+{rhs.Offset}, %esi");
                        }
                        Emit($"movl %esi, (%eax)");
                        return new Value() { Kind = Value.ValueKind.Void };
                    case Value.ValueKind.Address:
                        Emit($"popl %esi");
                        break;
                    case Value.ValueKind.Temp:
                        Emit($"movl %esp, %esi");
                        Emit($"pushl %ecx");
                        Emit($"movl ${self.Type.Sizeof()}, %ecx");
                        Emit($"movl %eax, %edi");
                        Emit($"cld");
                        Emit($"rep movsb");
                        Emit($"pop %ecx");
                        Emit($"add ${self.Type.Sizeof()}, %esp");
                        return new Value() { Kind = Value.ValueKind.Void };
                    default:
                        throw new NotImplementedException();
                }
                Emit($"pushl %ecx");
                Emit($"movl ${self.Type.Sizeof()}, %ecx");
                Emit($"movl %eax, %edi");
                Emit($"cld");
                Emit($"rep movsb");
                Emit($"pop %ecx");
            }
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnArrayAssignInitializer(SyntaxTree.Initializer.ArrayAssignInitializer self, Value value) {
            var elementSize = self.Type.BaseType.Sizeof();
            foreach (var init in self.Inits) {
                init.Accept(this, value);
                switch (value.Kind) {
                    case Value.ValueKind.Var:
                        if (value.Label == null) {
                            value.Offset += elementSize;
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
            Emit($"jmp {label}");
            // ToDo: 変数宣言を含む複文の内側から外側に脱出するとメモリリークするのを治す
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnCaseStatement(SyntaxTree.Statement.CaseStatement self, Value value) {
            var label = _switchLabelTableStack.Peek()[self];
            Emit($"{label}:");
            self.Stmt.Accept(this, value);
            // ToDo: 変数宣言を含む複文の内側から外側に脱出するとメモリリークするのを治す
            return new Value() { Kind = Value.ValueKind.Void };
        }

        Scope<Tuple<string, int>> localScope = Scope<Tuple<string, int>>.Empty;
        int localScopeTotalSize = 0;

        public Value OnCompoundStatement(SyntaxTree.Statement.CompoundStatement self, Value value) {
            localScope = localScope.Extend();
            var prevLocalScopeSize = localScopeTotalSize;

            int localScopeSize = 0;
            foreach (var x in self.Decls.Reverse<SyntaxTree.Declaration>()) {
                if (x.LinkageObject.Linkage == LinkageKind.NoLinkage) {
                    if (x.StorageClass == StorageClassSpecifier.Static) {
                        // static
                        localScope.Add(x.Ident, Tuple.Create(x.LinkageObject.LinkageId, 0));
                    } else {
                        localScopeSize += (x.LinkageObject.Type.Sizeof() + 3) & ~3;
                        localScope.Add(x.Ident, Tuple.Create((string)null, localScopeTotalSize - localScopeSize));
                    }
                } else if (x.LinkageObject.Linkage == LinkageKind.ExternalLinkage) {
                    // externなのでスキップ
                } else {
                    throw new NotImplementedException();
                    //localScope.Add(x.Ident, 0);
                }
            }
            //Emil($"andl $-16, %esp");
            Emit($"subl ${localScopeSize}, %esp");

            localScopeTotalSize -= localScopeSize;

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
            Emit($"jmp {label}");
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnDefaultStatement(SyntaxTree.Statement.DefaultStatement self, Value value) {
            var label = _switchLabelTableStack.Peek()[self];
            Emit($"{label}:");
            self.Stmt.Accept(this, value);
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnDoWhileStatement(SyntaxTree.Statement.DoWhileStatement self, Value value) {
            var labelContinue = LAlloc();
            var labelBreak = LAlloc();

            // Check Loop Condition
            Emit($"{labelContinue}:");
            ContinueTarget.Push(labelContinue);
            BreakTarget.Push(labelBreak);
            var stmt = self.Stmt.Accept(this, value);
            discard(stmt);
            ContinueTarget.Pop();
            BreakTarget.Pop();

            var ret = self.Cond.Accept(this, value);
            LoadI(ret, "%eax");
            Emit($"cmpl $0, %eax");
            Emit($"jne {labelContinue}");
            Emit($"{labelBreak}:");
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnEmptyStatement(SyntaxTree.Statement.EmptyStatement self, Value value) {
            return new Value() { Kind = Value.ValueKind.Void };
            //throw new NotImplementedException();
        }

        public Value OnExpressionStatement(SyntaxTree.Statement.ExpressionStatement self, Value value) {
            var ret = self.Expr.Accept(this, value);
            discard(ret);
            return value;
        }

        public Value OnForStatement(SyntaxTree.Statement.ForStatement self, Value value) {
            // Initialize
            if (self.Init != null) {
                var ret = self.Init.Accept(this, value);
                discard(ret);
            }

            var labelHead = LAlloc();
            var labelContinue = LAlloc();
            var labelBreak = LAlloc();

            // Check Loop Condition
            Emit($"{labelHead}:");
            if (self.Cond != null) {
                var ret = self.Cond.Accept(this, value);
                LoadI(ret, "%eax");
                Emit($"cmpl $0, %eax");
                Emit($"je {labelBreak}");
            }

            ContinueTarget.Push(labelContinue);
            BreakTarget.Push(labelBreak);
            var stmt = self.Stmt.Accept(this, value);
            discard(stmt);
            ContinueTarget.Pop();
            BreakTarget.Pop();

            Emit($"{labelContinue}:");
            if (self.Update != null) {
                var ret = self.Update.Accept(this, value);
                discard(ret);
            }

            Emit($"jmp {labelHead}");
            Emit($"{labelBreak}:");

            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnGenericLabeledStatement(SyntaxTree.Statement.GenericLabeledStatement self, Value value) {
            if (genericlabels.ContainsKey(self.Ident) == false) {
                genericlabels[self.Ident] = LAlloc();
            }
            Emit($"{genericlabels[self.Ident]}:");
            self.Stmt.Accept(this, value);
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnGotoStatement(SyntaxTree.Statement.GotoStatement self, Value value) {
            if (genericlabels.ContainsKey(self.Label) == false) {
                genericlabels[self.Label] = LAlloc();
            }
            Emit($"jmp {genericlabels[self.Label]}");
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnIfStatement(SyntaxTree.Statement.IfStatement self, Value value) {
            var cond = self.Cond.Accept(this, value);
            LoadI(cond, "%eax");
            Emit($"cmpl $0, %eax");

            if (self.ElseStmt != null) {
                var elseLabel = LAlloc();
                var junctionLabel = LAlloc();

                Emit($"je {elseLabel}");

                var thenRet = self.ThenStmt.Accept(this, value);
                discard(thenRet);
                Emit($"jmp {junctionLabel}");
                Emit($"{elseLabel}:");
                var elseRet = self.ElseStmt.Accept(this, value);
                discard(elseRet);
                Emit($"{junctionLabel}:");
            } else {
                var junctionLabel = LAlloc();

                Emit($"je {junctionLabel}");

                var thenRet = self.ThenStmt.Accept(this, value);
                discard(thenRet);
                Emit($"{junctionLabel}:");
            }


            return new Value() { Kind = Value.ValueKind.Void };
        }

        private string ToByteReg(string s) {
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
        private string ToWordReg(string s) {
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
        private Value LoadI(Value value, string register) {
            var ValueType = value.Type;
            System.Diagnostics.Debug.Assert(ValueType.IsIntegerType() || ValueType.IsPointerType() || ValueType.IsArrayType());
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
        /// 値をスタックトップに積む。もともとスタックトップにある場合は何もしない
        /// </summary>
        /// <param name="value"></param>
        /// <param name="register"></param>
        /// <returns></returns>
        private Value LoadS(Value value) {
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
                        return new Value() { Kind = Value.ValueKind.Temp, Type = ValueType };
                    }
                case Value.ValueKind.Temp:
                    // 何もしない
                    return value;
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
                        return new Value() { Kind = Value.ValueKind.Temp, Type = ValueType };
                    }
                case Value.ValueKind.Register: {
                        // レジスタをスタックへ
                        Emit($"pushl ${value.Register}");
                        return new Value() { Kind = Value.ValueKind.Temp, Type = ValueType };
                    }
                    throw new NotImplementedException();
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

                        return new Value() { Kind = Value.ValueKind.Temp, Type = ValueType };
                    }
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
                        return new Value() { Kind = Value.ValueKind.Temp, Type = ValueType };
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// FPUスタック上に値をロードする
        /// </summary>
        /// <param name="rhs"></param>
        private void LoadF(Value rhs) {
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
        public void LoadP(Value target, string reg) {
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
        public void LoadVA(Value lhs, string reg) {
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

        public Value OnReturnStatement(SyntaxTree.Statement.ReturnStatement self, Value value) {
            if (self.Expr != null) {
                value = self.Expr.Accept(this, value);
                if (value.Type.IsSignedIntegerType()) {
                    value = LoadI(value, "%ecx");
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
                    value = LoadI(value, "%ecx");
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
                    LoadF(value);
                } else {
                    if (value.Type.Sizeof() <= 4) {
                        value = LoadI(value, "%eax");
                    } else {
                        LoadVA(value, "%esi");
                        //switch (value.Kind) {
                        //    case Value.ValueKind.Var:
                        //        if (value.Label == null) {
                        //            Emil($"leal {value.Offset}(%ebp), %esi");
                        //        } else {
                        //            Emil($"leal {value.Label}+{value.Offset}, %esi");
                        //        }
                        //        break;
                        //    case Value.ValueKind.Ref:
                        //        if (value.Label == null) {
                        //            Emil($"leal {value.Offset}(%ebp), %esi");
                        //        } else {
                        //            Emil($"leal {value.Label}+{value.Offset}, %esi");
                        //        }
                        //        break;
                        //    case Value.ValueKind.Temp:
                        //        Emil($"movl %esp, %esi");
                        //        break;
                        //    default:
                        //        throw new NotImplementedException();
                        //}
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
            return new Value() { Kind = Value.ValueKind.Void };
        }

        private Stack<Dictionary<SyntaxTree.Statement, string>> _switchLabelTableStack = new Stack<Dictionary<SyntaxTree.Statement, string>>();

        public Value OnSwitchStatement(SyntaxTree.Statement.SwitchStatement self, Value value) {
            var labelBreak = LAlloc();

            var ret = self.Cond.Accept(this, value);
            LoadI(ret, "%eax");

            var labelDic = new Dictionary<SyntaxTree.Statement, string>();
            foreach (var caseLabel in self.CaseLabels) {
                var caseValue = caseLabel.Value;
                var label = LAlloc();
                labelDic.Add(caseLabel, label);
                Emit($"cmp ${caseValue}, %eax");
                Emit($"je {label}");
            }
            if (self.DefaultLabel != null) {
                var label = LAlloc();
                labelDic.Add(self.DefaultLabel, label);
                Emit($"jmp {label}");
            } else {
                Emit($"jmp {labelBreak}");
            }

            _switchLabelTableStack.Push(labelDic);
            BreakTarget.Push(labelBreak);
            var stmt = self.Stmt.Accept(this, value);
            discard(stmt);
            BreakTarget.Pop();
            _switchLabelTableStack.Pop();
            Emit($"{labelBreak}:");

            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnWhileStatement(SyntaxTree.Statement.WhileStatement self, Value value) {
            var labelContinue = LAlloc();
            var labelBreak = LAlloc();

            // Check Loop Condition
            Emit($"{labelContinue}:");
            var ret = self.Cond.Accept(this, value);
            LoadI(ret, "%eax");
            Emit($"cmpl $0, %eax");
            Emit($"je {labelBreak}");
            ContinueTarget.Push(labelContinue);
            BreakTarget.Push(labelBreak);
            var stmt = self.Stmt.Accept(this, value);
            discard(stmt);
            ContinueTarget.Pop();
            BreakTarget.Pop();

            Emit($"jmp {labelContinue}");
            Emit($"{labelBreak}:");

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
            Emit($".data");
            foreach (var data in dataBlock) {
                Emit($"{data.Item1}:");
                Console.Write($".byte ");
                Emit(String.Join(" ,", data.Item2.Select(x => x.ToString())));
            }

            return value;
        }

    }

    public class FileScopeInitializerVisitor : SyntaxTreeVisitor.IVisitor<SyntaxTreeCompileVisitor.Value, SyntaxTreeCompileVisitor.Value> {
        private SyntaxTreeCompileVisitor syntaxTreeCompileVisitor;
        public void Emit(string s) {
            syntaxTreeCompileVisitor.Emit(s);
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
                    Values.Add(new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant("0", 0, CType.BasicType.TypeKind.UnsignedLongInt));
                } else if (filledSize >= 2) {
                    filledSize -= 2;
                    Values.Add(new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant("0", 0, CType.BasicType.TypeKind.UnsignedShortInt));
                } else if (filledSize >= 1) {
                    filledSize -= 1;
                    Values.Add(new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant("0", 0, CType.BasicType.TypeKind.UnsignedChar));
                }
            }

            return value;
        }

        public SyntaxTreeCompileVisitor.Value OnStructUnionAssignInitializer(SyntaxTree.Initializer.StructUnionAssignInitializer self, SyntaxTreeCompileVisitor.Value value) {
            throw new NotImplementedException();
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
	movl	al, %ecx
	movl	ah, %ebx
	
    // add
    addl	%ecx, %eax
	adcl	%ebx, %edx
	
    // sub
    subl	%ecx, %eax
	sbbl	%ebx, %edx

	movl	%eax, zl
	movl	%edx, zh

    
     
     */
