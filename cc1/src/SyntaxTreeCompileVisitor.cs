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
                Address,    // 式の結果はアドレス参照である（スタックの一番上に参照先のアドレス値がある）
            }

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

        /// <summary>
        /// 文字列リテラルなどの静的データ
        /// </summary>
        List<Tuple<string, byte[]>> dataBlock = new List<Tuple<string, byte[]>>();

        int n = 0;

        private string LAlloc() {
            return $"L{n++}";
        }

        private void discard(Value v) {
            if (v.Kind == Value.ValueKind.Temp || v.Kind == Value.ValueKind.Address) {
                Console.WriteLine($"addl ${(v.Type.Sizeof() + 3) & ~3}, %esp");  // discard
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

                Console.WriteLine($".section .text");
                Console.WriteLine($".globl {self.LinkageObject.LinkageId}");
                Console.WriteLine($"{self.LinkageObject.LinkageId}:");
                Console.WriteLine($"pushl %ebp");
                Console.WriteLine($"movl %esp, %ebp");
                self.Body.Accept(this, value);
                Console.WriteLine($"movl %ebp, %esp");
                Console.WriteLine($"popl %ebp");
                Console.WriteLine($"ret");
            }
            return value;
        }

        public Value OnTypeDeclaration(SyntaxTree.Declaration.TypeDeclaration self, Value value) {
            throw new NotImplementedException();
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
            if (self.Lhs.Type.IsIntegerType() && self.Rhs.Type.IsIntegerType()) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                Load(rhs, "%ecx"); // rhs
                Load(lhs, "%eax"); // lhs
                switch (self.Op) {
                    case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add:
                        Console.WriteLine($"addl %ecx, %eax");
                        break;
                    case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub:
                        Console.WriteLine($"subl %ecx, %eax");
                        break;
                    default:
                        throw new NotImplementedException();
                }
                Console.WriteLine($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (self.Lhs.Type.IsPointerType() && self.Rhs.Type.IsIntegerType()) {
                CType elemType;
                self.Lhs.Type.IsPointerType(out elemType);
                var target = self.Lhs.Accept(this, value);
                var index = self.Rhs.Accept(this, value);

                if (index.Kind == Value.ValueKind.IntConst) {
                    switch (target.Kind) {
                        case Value.ValueKind.Var:
                            return new Value() { Kind = Value.ValueKind.Ref, Type = self.Type, Label = target.Label, Offset = (int)(target.Offset + index.IntConst * elemType.Sizeof()) };
                        case Value.ValueKind.Ref:
                            return new Value() { Kind = Value.ValueKind.Ref, Type = target.Type, Label = target.Label, Offset = (int)(target.Offset + index.IntConst * elemType.Sizeof()) };

                        default:
                            break;
                    }
                }
                Load(index, "%ecx");
                if (self.Op == SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub) {
                    Console.WriteLine($"negl %ecx");
                }

                switch (target.Kind) {
                    case Value.ValueKind.Var:
                        if (target.Label == null) {
                            Console.WriteLine($"movl {target.Offset}(%ebp), %eax");
                        } else {
                            Console.WriteLine($"movl {target.Label}+{target.Offset}, %eax");
                        }
                        break;
                    case Value.ValueKind.Ref:
                        if (target.Label == null) {
                            Console.WriteLine($"leal {target.Offset}(%ebp), %eax");
                        } else {
                            Console.WriteLine($"leal {target.Label}+{target.Offset}, %eax");
                        }
                        break;
                    case Value.ValueKind.Address:
                        Console.WriteLine($"popl %eax");
                        Console.WriteLine($"movl (%eax), %eax");
                        break;
                    case Value.ValueKind.Temp:
                        Console.WriteLine($"popl %eax");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                Console.WriteLine($"imull ${(self.Type as CType.PointerType).BaseType.Sizeof()}, %ecx, %ecx");
                Console.WriteLine($"leal (%eax, %ecx), %eax");
                Console.WriteLine($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (self.Lhs.Type.IsIntegerType() && self.Rhs.Type.IsPointerType()) {
                var target = self.Rhs.Accept(this, value);
                var index = self.Lhs.Accept(this, value);

                Load(index, "%ecx");
                if (self.Op == SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub) {
                    Console.WriteLine($"negl %ecx");
                }

                switch (target.Kind) {
                    case Value.ValueKind.Var:
                        if (target.Label == null) {
                            Console.WriteLine($"movl {target.Offset}(%ebp), %eax");
                        } else {
                            Console.WriteLine($"movl {target.Label}+{target.Offset}, %eax");
                        }
                        break;
                    case Value.ValueKind.Ref:
                        if (target.Label == null) {
                            Console.WriteLine($"leal {target.Offset}(%ebp), %eax");
                        } else {
                            Console.WriteLine($"leal {target.Label}+{target.Offset}, %eax");
                        }
                        break;
                    case Value.ValueKind.Address:
                        Console.WriteLine($"popl %eax");
                        Console.WriteLine($"movl (%eax), %eax");
                        break;
                    case Value.ValueKind.Temp:
                        Console.WriteLine($"popl %eax");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                Console.WriteLine($"imull ${self.Type.Sizeof()}, %ecx, %ecx");
                Console.WriteLine($"leal (%eax, %ecx), %eax");
                Console.WriteLine($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if ((self.Lhs.Type.IsRealFloatingType() || self.Lhs.Type.IsIntegerType()) &&
                       (self.Rhs.Type.IsRealFloatingType() || self.Rhs.Type.IsIntegerType())) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);

                LoadF(rhs);
                LoadF(lhs);

                switch (self.Op) {
                    case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add:
                        Console.WriteLine($"faddp");
                        break;
                    case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub:
                        Console.WriteLine($"fsubp");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (self.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                    Console.WriteLine($"sub $4, %esp");
                    Console.WriteLine($"fstps (%esp)");
                } else if (self.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                    Console.WriteLine($"sub $8, %esp");
                    Console.WriteLine($"fstpl (%esp)");
                } else {
                    throw new NotImplementedException();
                }

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnAndExpression(SyntaxTree.Expression.AndExpression self, Value value) {
            var lhs = self.Lhs.Accept(this, value);
            var rhs = self.Rhs.Accept(this, value);
            if (self.Type.IsIntegerType()) {
                Load(rhs, "%eax"); // rhs
                Load(lhs, "%ecx"); // lhs
                Console.WriteLine($"andl %ecx, %eax");
                Console.WriteLine($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnCompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression self, Value value) {
            if (self.Type.IsIntegerType()) {
                Console.WriteLine($"pushl %edi");

                var rhs = self.Rhs.Accept(this, value);
                var lhs = self.Lhs.Accept(this, value);

                switch (lhs.Kind) {
                    case Value.ValueKind.Var:
                        if (lhs.Label == null) {
                            Console.WriteLine($"leal {lhs.Offset}(%ebp), %edi");
                        }
                        else {
                            Console.WriteLine($"leal {lhs.Label}+{lhs.Offset}, %edi");
                        }

                        break;
                    case Value.ValueKind.Ref:
                        if (lhs.Label == null) {
                            Console.WriteLine($"leal {lhs.Offset}(%ebp), %edi");
                        }
                        else {
                            Console.WriteLine($"leal {lhs.Label}+{lhs.Offset}, %edi");
                        }

                        break;
                    case Value.ValueKind.Address:
                        Console.WriteLine($"popl %edi");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                Load(rhs, "%ecx"); // rhs

                switch (self.Op) {
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.ADD_ASSIGN:
                        Console.WriteLine($"addl (%edi), %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.SUB_ASSIGN:
                        Console.WriteLine($"subl (%edi), %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MUL_ASSIGN:
                        Console.WriteLine($"mov (%edi), %eax");
                        if (self.Type.IsSignedIntegerType()) {
                            Console.WriteLine($"imull %ecx");
                        }
                        else {
                            Console.WriteLine($"mull %ecx");
                        }

                        Console.WriteLine($"mov %eax, %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.DIV_ASSIGN:
                        Console.WriteLine($"mov (%edi), %eax");
                        Console.WriteLine($"cltd");
                        if (self.Type.IsSignedIntegerType()) {
                            Console.WriteLine($"idivl %ecx");
                        }
                        else {
                            Console.WriteLine($"divl %ecx");
                        }

                        Console.WriteLine($"mov %eax, %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MOD_ASSIGN:
                        Console.WriteLine($"mov (%edi), %eax");
                        if (self.Type.IsSignedIntegerType()) {
                            Console.WriteLine($"idivl %ecx");
                        }
                        else {
                            Console.WriteLine($"divl %ecx");
                        }

                        Console.WriteLine($"mov %edx, %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.AND_ASSIGN:
                        Console.WriteLine($"andl (%edi), %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.OR_ASSIGN:
                        Console.WriteLine($"orl (%edi), %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.XOR_ASSIGN:
                        Console.WriteLine($"xorl (%edi), %ecx");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.LEFT_ASSIGN:
                        if (self.Type.IsSignedIntegerType()) {
                            Console.WriteLine($"sall (%edi), %ecx");
                        }
                        else {
                            Console.WriteLine($"shll (%edi), %ecx");
                        }

                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.RIGHT_ASSIGN:
                        if (self.Type.IsSignedIntegerType()) {
                            Console.WriteLine($"sarl (%edi), %ecx");
                        }
                        else {
                            Console.WriteLine($"shrl (%edi), %ecx");
                        }

                        break;
                    default:
                        throw new Exception("来ないはず");
                }

                Console.WriteLine($"movl %ecx, (%edi)");

                Console.WriteLine($"popl %edi");

                Console.WriteLine($"pushl %ecx");
                return new Value() {Kind = Value.ValueKind.Temp, Type = self.Type};
            } else if (self.Type.IsPointerType()) {
                Console.WriteLine($"pushl %edi");

                var rhs = self.Rhs.Accept(this, value);
                var lhs = self.Lhs.Accept(this, value);
                CType ty = lhs.Type.GetBasePointerType();


                switch (lhs.Kind) {
                    case Value.ValueKind.Var:
                        if (lhs.Label == null) {
                            Console.WriteLine($"leal {lhs.Offset}(%ebp), %edi");
                        } else {
                            Console.WriteLine($"leal {lhs.Label}+{lhs.Offset}, %edi");
                        }
                        break;
                    case Value.ValueKind.Ref:
                        if (lhs.Label == null) {
                            Console.WriteLine($"leal {lhs.Offset}(%ebp), %edi");
                        } else {
                            Console.WriteLine($"leal {lhs.Label}+{lhs.Offset}, %edi");
                        }
                        break;
                    case Value.ValueKind.Address:
                        Console.WriteLine($"popl %edi");
                        break;
                    default:
                        throw new NotImplementedException();
                }
                Load(rhs, "%ecx"); // rhs

                switch (self.Op) {
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.ADD_ASSIGN:
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.SUB_ASSIGN:
                        Console.WriteLine($"negl %ecx");
                        break;
                    default:
                        throw new Exception("来ないはず");
                }
                Console.WriteLine($"imull ${ty.Sizeof()}, %ecx, %ecx");
                Console.WriteLine($"movl (%edi), %eax");
                Console.WriteLine($"addl %ecx, %eax");
                Console.WriteLine($"movl %eax, (%edi)");

                Console.WriteLine($"popl %edi");

                Console.WriteLine($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (self.Type.IsRealFloatingType()) {

                var rhs = self.Rhs.Accept(this, value);
                var lhs = self.Lhs.Accept(this, value);

                switch (lhs.Kind) {
                    case Value.ValueKind.Var:
                        if (lhs.Label == null) {
                            Console.WriteLine($"leal {lhs.Offset}(%ebp), %eax");
                        } else {
                            Console.WriteLine($"leal {lhs.Label}+{lhs.Offset}, %eax");
                        }
                        break;
                    case Value.ValueKind.Ref:
                        if (lhs.Label == null) {
                            Console.WriteLine($"leal {lhs.Offset}(%ebp), %eax");
                        } else {
                            Console.WriteLine($"leal {lhs.Label}+{lhs.Offset}, %eax");
                        }
                        break;
                    case Value.ValueKind.Address:
                        Console.WriteLine($"popl %eax");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                LoadF(rhs); // rhs
                {
                    string op = "";
                    if (lhs.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Float)) {
                        op = "flds";
                    } else {
                        op = "fldl";
                    }

                    Console.WriteLine($"{op} (%eax)");
                }

                var op1 = "";
                var op2 = "";

                if (self.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                    Console.WriteLine($"subl $4, %esp");
                    op1 = "fsts";
                    op2 = "fstps";
                } else {
                    Console.WriteLine($"subl $8, %esp");
                    op1 = "fstl";
                    op2 = "fstpl";
                }
                Console.WriteLine($"movl %ecx, (%eax)");
                switch (self.Op) {
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.ADD_ASSIGN:
                        Console.WriteLine($"faddp");
                        Console.WriteLine($"{op1} (%eax)");
                        Console.WriteLine($"{op2} (%esp)");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.SUB_ASSIGN:
                        Console.WriteLine($"fsubp");
                        Console.WriteLine($"{op1} (%eax)");
                        Console.WriteLine($"{op2} (%esp)");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MUL_ASSIGN:
                        Console.WriteLine($"fmulp");
                        Console.WriteLine($"{op1} (%eax)");
                        Console.WriteLine($"{op2} (%esp)");
                        break;
                    case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.DIV_ASSIGN:
                        Console.WriteLine($"fdivp");
                        Console.WriteLine($"{op1} (%eax)");
                        Console.WriteLine($"{op2} (%esp)");
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
            var lhs = self.Lhs.Accept(this, value);
            var rhs = self.Rhs.Accept(this, value);

            rhs = Load(rhs, "%ecx");

            var dst = "";
            switch (lhs.Kind) {
                case Value.ValueKind.Var:
                    if (lhs.Label == null) {
                        Console.WriteLine($"leal {lhs.Offset}(%ebp), %eax");
                    } else {
                        Console.WriteLine($"leal {lhs.Label}+{lhs.Offset}, %eax");
                    }
                    break;
                case Value.ValueKind.Ref:
                    if (lhs.Label == null) {
                        Console.WriteLine($"leal {lhs.Offset}(%ebp), %eax");
                    } else {
                        Console.WriteLine($"leal {lhs.Label}+{lhs.Offset}, %eax");
                    }
                    break;
                case Value.ValueKind.Address:
                    Console.WriteLine($"popl %eax");
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (self.Type.Sizeof()) {
                case 1:
                    Console.WriteLine($"movb %cl, (%eax)");
                    Console.WriteLine($"pushl (%eax)");
                    break;
                case 2:
                    Console.WriteLine($"movw %cx, (%eax)");
                    Console.WriteLine($"pushl (%eax)");
                    break;
                case 4:
                    Console.WriteLine($"movl %ecx, (%eax)");
                    Console.WriteLine($"pushl (%eax)");
                    break;
                default:
                    //                    Console.WriteLine($"pushl %ecx");
                    Console.WriteLine($"movl %ecx, %esi");
                    Console.WriteLine($"movl ${self.Type.Sizeof()}, %ecx");
                    Console.WriteLine($"movl %eax, %edi");
                    Console.WriteLine($"cld");
                    Console.WriteLine($"rep movsb");
                    //                    Console.WriteLine($"pop %ecx");
                    // スタックトップにrhsの値があるのでpushは不要
                    break;
            }


            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnCastExpression(SyntaxTree.Expression.CastExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            if (ret.Type.IsIntegerType() && self.Type.IsIntegerType()) {
                var retty = ret.Type.Unwrap() as CType.BasicType;
                var selfty = self.Type.Unwrap() as CType.BasicType;

                Load(ret, "%eax");
                if (retty.IsSignedIntegerType()) {
                    if (selfty.Kind == CType.BasicType.TypeKind.Char || selfty.Kind == CType.BasicType.TypeKind.SignedChar) {
                        Console.WriteLine($"movsbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.SignedShortInt) {
                        Console.WriteLine($"movswl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.SignedInt || selfty.Kind == CType.BasicType.TypeKind.SignedLongInt) {
                        //Console.WriteLine($"movl %eax, %eax"); // do nothing;
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedInt || selfty.Kind == CType.BasicType.TypeKind.UnsignedLongInt) {
                        // Console.WriteLine($"movl %eax, %eax"); // do nothing;
                    } else {
                        throw new NotImplementedException();
                    }
                } else {
                    if (selfty.Kind == CType.BasicType.TypeKind.Char || selfty.Kind == CType.BasicType.TypeKind.SignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.SignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.SignedInt || selfty.Kind == CType.BasicType.TypeKind.SignedLongInt) {
                        //Console.WriteLine($"movl %eax, %eax"); // do nothing;
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedInt || selfty.Kind == CType.BasicType.TypeKind.UnsignedLongInt) {
                        //Console.WriteLine($"movl %eax, %eax"); // do nothing;
                    } else {
                        throw new NotImplementedException();
                    }
                }

                Console.WriteLine($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (ret.Type.IsPointerType() && self.Type.IsPointerType()) {
                return new Value(ret) { Type = self.Type };
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnCommaExpression(SyntaxTree.Expression.CommaExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnConditionalExpression(SyntaxTree.Expression.ConditionalExpression self, Value value) {
            var cond = self.CondExpr.Accept(this, value);
            Load(cond, "%eax");
            Console.WriteLine($"cmpl $0, %eax");

            var elseLabel = LAlloc();
            var junctionLabel = LAlloc();

            Console.WriteLine($"je {elseLabel}");

            var thenRet = self.ThenExpr.Accept(this, value);
            if (cond.Type.IsVoidType()) {
                discard(thenRet);
            } else if (thenRet.Kind == Value.ValueKind.Temp) {
                // すでにスタック上にある
            } else {
                if (thenRet.Type.IsIntegerType()) {
                    Load(thenRet, "%eax");
                    Console.WriteLine($"pushl %eax");
                } else if (thenRet.Type.IsRealFloatingType()) {
                    LoadF(thenRet);
                    if (thenRet.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                        Console.WriteLine($"sub $4, %esp");
                        Console.WriteLine($"fstps (%esp)");
                    } else if (thenRet.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                        Console.WriteLine($"sub $8, %esp");
                        Console.WriteLine($"fstpl (%esp)");
                    } else {
                        throw new Exception("");
                    }
                }
                else {
                    switch (thenRet.Kind) {
                        case Value.ValueKind.Var:
                            if (thenRet.Label == null) {
                                Console.WriteLine($"movl {thenRet.Offset}(%ebp), %eax");
                            }
                            else {
                                Console.WriteLine($"movl {thenRet.Label}+{thenRet.Offset}, %eax");
                            }

                            break;
                        case Value.ValueKind.Ref:
                            if (thenRet.Label == null) {
                                Console.WriteLine($"leal {thenRet.Offset}(%ebp), %eax");
                            }
                            else {
                                Console.WriteLine($"leal {thenRet.Label}+{thenRet.Offset}, %eax");
                            }

                            break;
                        case Value.ValueKind.Address:
                            Console.WriteLine($"popl %eax");
                            break;
                        default:
                            throw new Exception("");
                    }

                    switch (self.Type.Sizeof()) {
                        case 1:
                            Console.WriteLine($"movzbl (%eax), %eax");
                            Console.WriteLine($"pushl %eax");
                            break;
                        case 2:
                            Console.WriteLine($"movzwl (%eax), %eax");
                            Console.WriteLine($"pushl %eax");
                            break;
                        case 4:
                            Console.WriteLine($"movl (%eax), %eax");
                            Console.WriteLine($"pushl %eax");
                            break;
                        default:
                            Console.WriteLine($"subl {(thenRet.Type.Sizeof() + 3) & (~3)}, %esp");
                            Console.WriteLine($"movl %eax, %esi");
                            Console.WriteLine($"movl ${thenRet.Type.Sizeof()}, %ecx");
                            Console.WriteLine($"movl %esp, %edi");
                            Console.WriteLine($"cld");
                            Console.WriteLine($"rep movsb");
                            break;
                    }
                }
            }


            Console.WriteLine($"jmp {junctionLabel}");
            Console.WriteLine($"{elseLabel}:");
            var elseRet = self.ElseExpr.Accept(this, value);
            if (cond.Type.IsVoidType()) {
                discard(elseRet);
            } else if (elseRet.Kind == Value.ValueKind.Temp) {
                // すでにスタック上にある
            } else {
                if (elseRet.Type.IsIntegerType()) {
                    Load(elseRet, "%eax");
                    Console.WriteLine($"pushl %eax");
                } else if (elseRet.Type.IsRealFloatingType()) {
                    LoadF(elseRet);
                    if (elseRet.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                        Console.WriteLine($"sub $4, %esp");
                        Console.WriteLine($"fstps (%esp)");
                    } else if (elseRet.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                        Console.WriteLine($"sub $8, %esp");
                        Console.WriteLine($"fstpl (%esp)");
                    } else {
                        throw new Exception("");
                    }
                } else {
                    switch (elseRet.Kind) {
                        case Value.ValueKind.Var:
                            if (elseRet.Label == null) {
                                Console.WriteLine($"movl {elseRet.Offset}(%ebp), %eax");
                            } else {
                                Console.WriteLine($"movl {elseRet.Label}+{elseRet.Offset}, %eax");
                            }

                            break;
                        case Value.ValueKind.Ref:
                            if (elseRet.Label == null) {
                                Console.WriteLine($"leal {elseRet.Offset}(%ebp), %eax");
                            } else {
                                Console.WriteLine($"leal {elseRet.Label}+{elseRet.Offset}, %eax");
                            }

                            break;
                        case Value.ValueKind.Address:
                            Console.WriteLine($"popl %eax");
                            break;
                        default:
                            throw new Exception("");
                    }

                    switch (self.Type.Sizeof()) {
                        case 1:
                            Console.WriteLine($"movzbl (%eax), %eax");
                            Console.WriteLine($"pushl %eax");
                            break;
                        case 2:
                            Console.WriteLine($"movzwl (%eax), %eax");
                            Console.WriteLine($"pushl %eax");
                            break;
                        case 4:
                            Console.WriteLine($"movl (%eax), %eax");
                            Console.WriteLine($"pushl %eax");
                            break;
                        default:
                            Console.WriteLine($"subl {(elseRet.Type.Sizeof() + 3) & (~3)}, %esp");
                            Console.WriteLine($"movl %eax, %esi");
                            Console.WriteLine($"movl ${elseRet.Type.Sizeof()}, %ecx");
                            Console.WriteLine($"movl %esp, %edi");
                            Console.WriteLine($"cld");
                            Console.WriteLine($"rep movsb");
                            break;
                    }
                }
            }
            Console.WriteLine($"{junctionLabel}:");

            if (cond.Type.IsVoidType()) {
                return new Value() { Kind = Value.ValueKind.Void };
            }
            else {
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
                Load(rhs, "%eax");
                Load(lhs, "%ecx");
                Console.WriteLine($"cmpl %ecx, %eax");

                Console.WriteLine($"{op} %al");
                Console.WriteLine($"movzbl %al, %eax");
                Console.WriteLine($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if ((self.Lhs.Type.IsRealFloatingType() || self.Lhs.Type.IsIntegerType()) &&
                       (self.Rhs.Type.IsRealFloatingType() || self.Rhs.Type.IsIntegerType())) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);

                LoadF(rhs);
                LoadF(lhs);

                Console.WriteLine($"fcomip");
                Console.WriteLine($"fstp %st(0)");

                Console.WriteLine($"{op} %al");
                Console.WriteLine($"movzbl %al, %eax");
                Console.WriteLine($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };

            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnExclusiveOrExpression(SyntaxTree.Expression.ExclusiveOrExpression self, Value value) {
            if (self.Lhs.Type.IsIntegerType() && self.Rhs.Type.IsIntegerType()) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                Load(rhs, "%ecx"); // rhs
                Load(lhs, "%eax"); // lhs
                Console.WriteLine($"xorl %ecx, %eax");
                Console.WriteLine($"pushl %eax");

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
                Load(rhs, "%ecx"); // rhs
                Load(lhs, "%eax"); // lhs
                Console.WriteLine($"orl %ecx, %eax");
                Console.WriteLine($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            }

            throw new NotImplementedException();
        }

        public Value OnIntegerPromotionExpression(SyntaxTree.Expression.IntegerPromotionExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            if (ret.Type.IsIntegerType() && self.Type.IsIntegerType()) {
                var retty = ret.Type.Unwrap() as CType.BasicType;
                var selfty = self.Type.Unwrap() as CType.BasicType;

                Load(ret, "%eax");
                if (retty.IsSignedIntegerType()) {
                    if (selfty.Kind == CType.BasicType.TypeKind.Char || selfty.Kind == CType.BasicType.TypeKind.SignedChar) {
                        Console.WriteLine($"movsbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.SignedShortInt) {
                        Console.WriteLine($"movswl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.SignedInt || selfty.Kind == CType.BasicType.TypeKind.SignedLongInt) {
                        //Console.WriteLine($"movl %eax, %eax");  // do nothing;
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedInt || selfty.Kind == CType.BasicType.TypeKind.UnsignedLongInt) {
                        //Console.WriteLine($"movl %eax, %eax");  // do nothing;
                    } else {
                        throw new NotImplementedException();
                    }
                } else {
                    if (selfty.Kind == CType.BasicType.TypeKind.Char || selfty.Kind == CType.BasicType.TypeKind.SignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.SignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.SignedInt || selfty.Kind == CType.BasicType.TypeKind.SignedLongInt) {
                        //Console.WriteLine($"movl %eax, %eax");  // do nothing;
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedInt || selfty.Kind == CType.BasicType.TypeKind.UnsignedLongInt) {
                        //Console.WriteLine($"movl %eax, %eax");  // do nothing;
                    } else {
                        throw new NotImplementedException();
                    }
                }
                Console.WriteLine($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnLogicalAndExpression(SyntaxTree.Expression.LogicalAndExpression self, Value value) {
            var labelFalse = LAlloc();
            var labelJunction = LAlloc();
            var lhs = self.Lhs.Accept(this, value);
            Load(lhs, "%eax");
            Console.WriteLine($"cmpl $0, %eax");
            Console.WriteLine($"je {labelFalse}");
            var rhs = self.Rhs.Accept(this, value);
            Load(rhs, "%eax");
            Console.WriteLine($"cmpl $0, %eax");
            Console.WriteLine($"je {labelFalse}");
            Console.WriteLine($"pushl $1");
            Console.WriteLine($"jmp {labelJunction}");
            Console.WriteLine($"{labelFalse}:");
            Console.WriteLine($"pushl $0");
            Console.WriteLine($"{labelJunction}:");
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnLogicalOrExpression(SyntaxTree.Expression.LogicalOrExpression self, Value value) {
            var labelTrue = LAlloc();
            var labelJunction = LAlloc();
            var lhs = self.Lhs.Accept(this, value);
            Load(lhs, "%eax");
            Console.WriteLine($"cmpl $0, %eax");
            Console.WriteLine($"jne {labelTrue}");
            var rhs = self.Rhs.Accept(this, value);
            Load(rhs, "%eax");
            Console.WriteLine($"cmpl $0, %eax");
            Console.WriteLine($"jne {labelTrue}");
            Console.WriteLine($"pushl $0");
            Console.WriteLine($"jmp {labelJunction}");
            Console.WriteLine($"{labelTrue}:");
            Console.WriteLine($"pushl $1");
            Console.WriteLine($"{labelJunction}:");
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnMultiplicitiveExpression(SyntaxTree.Expression.MultiplicitiveExpression self, Value value) {
            if (self.Lhs.Type.IsIntegerType() && self.Rhs.Type.IsIntegerType()) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);
                Load(rhs, "%ecx"); // rhs
                Load(lhs, "%eax"); // lhs
                switch (self.Op) {
                    case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mul:
                        if (self.Type.IsSignedIntegerType()) {
                            Console.WriteLine($"imull %ecx");
                        } else {
                            Console.WriteLine($"mull %ecx");
                        }
                        Console.WriteLine($"push %eax");
                        break;
                    case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Div:
                        Console.WriteLine($"cltd");
                        if (self.Type.IsSignedIntegerType()) {
                            Console.WriteLine($"idivl %ecx");
                        } else {
                            Console.WriteLine($"divl %ecx");
                        }
                        Console.WriteLine($"push %eax");
                        break;
                    case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mod:
                        Console.WriteLine($"cltd");
                        if (self.Type.IsSignedIntegerType()) {
                            Console.WriteLine($"idivl %ecx");
                        } else {
                            Console.WriteLine($"divl %ecx");
                        }
                        Console.WriteLine($"push %edx");
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
                        Console.WriteLine($"fmulp");
                        break;
                    case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Div:
                        Console.WriteLine($"fdivp");
                        break;
                    case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mod:
                        throw new NotImplementedException();
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (self.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                    Console.WriteLine($"sub $4, %esp");
                    Console.WriteLine($"fstps (%esp)");
                } else if (self.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                    Console.WriteLine($"sub $8, %esp");
                    Console.WriteLine($"fstpl (%esp)");
                } else {
                    throw new NotImplementedException();
                }

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            }
            throw new NotImplementedException();
        }

        public Value OnArraySubscriptingExpression(SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression self, Value value) {
            var target = self.Target.Accept(this, value);
            var index = self.Index.Accept(this, value);

            Load(index, "%ecx");
            if (target.Type.IsPointerType()) {
                switch (target.Kind) {
                    case Value.ValueKind.Var:
                        if (target.Label == null) {
                            Console.WriteLine($"movl {target.Offset}(%ebp), %eax");
                        } else {
                            Console.WriteLine($"movl {target.Label}+{target.Offset}, %eax");
                        }
                        break;
                    case Value.ValueKind.Ref:
                        if (target.Label == null) {
                            Console.WriteLine($"leal {target.Offset}(%ebp), %eax");
                        } else {
                            Console.WriteLine($"leal {target.Label}+{target.Offset}, %eax");
                        }
                        break;
                    case Value.ValueKind.Address:
                        Console.WriteLine($"popl %eax");
                        Console.WriteLine($"movl (%eax), %eax");
                        break;
                    case Value.ValueKind.Temp:
                        Console.WriteLine($"popl %eax");
                        break;
                    default:
                        throw new NotImplementedException();
                }
            } else {
                switch (target.Kind) {
                    case Value.ValueKind.Var:
                        if (target.Label == null) {
                            Console.WriteLine($"leal {target.Offset}(%ebp), %eax");
                        } else {
                            Console.WriteLine($"leal {target.Label}+{target.Offset}, %eax");
                        }
                        break;
                    case Value.ValueKind.Ref:
                        if (target.Label == null) {
                            Console.WriteLine($"leal {target.Offset}(%ebp), %eax");
                        } else {
                            Console.WriteLine($"leal {target.Label}+{target.Offset}, %eax");
                        }
                        break;
                    case Value.ValueKind.Address:
                    case Value.ValueKind.Temp:
                        Console.WriteLine($"popl %eax");
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
            Console.WriteLine($"imull ${self.Type.Sizeof()}, %ecx, %ecx");
            Console.WriteLine($"leal (%eax, %ecx), %eax");
            Console.WriteLine($"pushl %eax");

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
                Console.WriteLine($"subl ${resultSize}, %esp");
            }

            int bakSize = 4 * 3;
            Console.WriteLine($"pushl %eax");
            Console.WriteLine($"pushl %ecx");
            Console.WriteLine($"pushl %edx");

            int argSize = 0;

            // 引数を右側（末尾側）からスタックに積む
            foreach (var x in self.Args.Reverse<SyntaxTree.Expression>()) {
                var _argSize = (x.Type.Sizeof() + 3) & ~3;

                argSize += _argSize;

                var a = x.Accept(this, value);
                if (x.Type.Sizeof() <= 4) {
                    Load(a, "%eax");
                    Console.WriteLine($"push %eax");
                } else if (a.Kind == Value.ValueKind.FloatConst) {
                    if (a.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Float)) {
                        var bytes = BitConverter.GetBytes((float)a.FloatConst);
                        var dword = BitConverter.ToUInt32(bytes, 0);
                        Console.WriteLine($"pushl ${dword}");
                    } else if (a.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Double)) {
                        var bytes = BitConverter.GetBytes(a.FloatConst);
                        var qwordlo = BitConverter.ToUInt32(bytes, 0);
                        var qwordhi = BitConverter.ToUInt32(bytes, 4);
                        Console.WriteLine($"pushl ${qwordhi}");
                        Console.WriteLine($"pushl ${qwordlo}");
                    } else {
                        throw new NotImplementedException();
                    }
                    break;
                } else if (a.Kind == Value.ValueKind.Var) {
                    Console.WriteLine($"subl ${_argSize}, %esp");
                    Console.WriteLine($"movl ${a.Type.Sizeof()}, %ecx");
                    if (a.Label == null) {
                        Console.WriteLine($"leal {a.Offset}(%ebp), %esi");
                    } else {
                        Console.WriteLine($"leal {a.Label}+{a.Offset}, %esi");
                    }
                    Console.WriteLine($"movl %esp, %edi");
                    Console.WriteLine($"cld");
                    Console.WriteLine($"rep movsb");

                } else if (a.Kind == Value.ValueKind.Temp) {
                    // 式の値はスタックの上にあるので積み直し不要

                } else {
                    throw new NotImplementedException();
                }
            }

            // 戻り値が浮動小数点数ではなく、eaxにも入らないならスタック上に格納先アドレスを積む
            if (resultSize > 4 && !funcType.ResultType.IsRealFloatingType()) {
                Console.WriteLine($"leal {argSize + bakSize}(%esp), %eax");
                Console.WriteLine($"push %eax");
            }

            var func = self.Expr.Accept(this, value);
            Load(func, "%eax");
            Console.WriteLine($"call *%eax");

            if (resultSize > 4) {
                if (funcType.ResultType.IsRealFloatingType()) {
                    // 浮動小数点数はFPUスタック上にある
                    if (self.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                        Console.WriteLine($"fstps {(argSize + bakSize)}(%esp)");
                    } else if (self.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                        Console.WriteLine($"fstpl {(argSize + bakSize)}(%esp)");
                    } else {
                        throw new NotImplementedException();
                    }
                    Console.WriteLine($"addl ${argSize}, %esp");
                } else {
                    // 戻り値は格納先アドレスに入れられているはず
                    Console.WriteLine($"addl ${argSize + 4}, %esp");
                }
            } else if (resultSize > 0) {
                // 戻り値をコピー(4バイト以下)
                Console.WriteLine($"movl %eax, {(argSize + bakSize)}(%esp)");
                Console.WriteLine($"addl ${argSize}, %esp");
            } else {
                // void型？
                Console.WriteLine($"addl ${argSize}, %esp");
            }

            Console.WriteLine($"popl %edx");
            Console.WriteLine($"popl %ecx");
            Console.WriteLine($"popl %eax");

            return new Value() { Kind = resultSize == 0 ? Value.ValueKind.Void : Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnMemberDirectAccess(SyntaxTree.Expression.PostfixExpression.MemberDirectAccess self, Value value) {
            var ret = self.Expr.Accept(this, value);
            var offset = 0;
            var st = self.Expr.Type as CType.TaggedType.StructUnionType;
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
            switch (ret.Kind) {
                case Value.ValueKind.Var:
                    if (ret.Label == null) {
                        Console.WriteLine($"leal {ret.Offset + offset}(%ebp), %eax");
                    } else {
                        Console.WriteLine($"leal {ret.Label}+{ret.Offset + offset}, %eax");
                    }
                    Console.WriteLine($"pushl %eax");
                    return new Value() { Kind = Value.ValueKind.Address, Type = self.Type };
                case Value.ValueKind.Ref:
                    if (ret.Label == null) {
                        Console.WriteLine($"leal {ret.Offset + offset}(%ebp), %eax");
                    } else {
                        Console.WriteLine($"leal {ret.Label}+{ret.Offset + offset}, %eax");
                    }
                    Console.WriteLine($"pushl %eax");
                    return new Value() { Kind = Value.ValueKind.Address, Type = self.Type };
                    break;
                case Value.ValueKind.Address:
                    Console.WriteLine($"popl %eax");
                    Console.WriteLine($"addl ${offset}, %eax");
                    Console.WriteLine($"pushl %eax");
                    return new Value() { Kind = Value.ValueKind.Address, Type = self.Type };
                default:
                    throw new Exception("");
            }
            throw new NotImplementedException();
        }

        public Value OnMemberIndirectAccess(SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess self, Value value) {
            var ret = self.Expr.Accept(this, value);
            var offset = 0;
            var st = (self.Expr.Type as CType.TaggedType.PointerType).BaseType as CType.TaggedType.StructUnionType;
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
            switch (ret.Kind) {
                case Value.ValueKind.Var:
                    if (ret.Label == null) {
                        Console.WriteLine($"movl {ret.Offset}(%ebp), %eax");
                    } else {
                        Console.WriteLine($"movl {ret.Label}+{ret.Offset}, %eax");
                    }
                    Console.WriteLine($"addl ${offset}, %eax");
                    Console.WriteLine($"pushl %eax");
                    return new Value() { Kind = Value.ValueKind.Address, Type = self.Type };
                case Value.ValueKind.Ref:
                    if (ret.Label == null) {
                        Console.WriteLine($"movl {ret.Offset}(%ebp), %eax");
                    } else {
                        Console.WriteLine($"movl {ret.Label}+{ret.Offset}, %eax");
                    }
                    Console.WriteLine($"addl ${offset}, %eax");
                    Console.WriteLine($"pushl %eax");
                    return new Value() { Kind = Value.ValueKind.Address, Type = self.Type };
                    break;
                case Value.ValueKind.Address:
                    Console.WriteLine($"popl %eax");
                    Console.WriteLine($"movl (%eax), %eax");
                    Console.WriteLine($"addl ${offset}, %eax");
                    Console.WriteLine($"pushl %eax");
                    return new Value() { Kind = Value.ValueKind.Address, Type = self.Type };
                default:
                    throw new Exception("");
            }
            throw new NotImplementedException();
        }

        public Value OnUnaryPostfixExpression(SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            // load address
            switch (ret.Kind) {
                case Value.ValueKind.Var:
                    if (ret.Label == null) {
                        Console.WriteLine($"leal {ret.Offset}(%ebp), %eax");
                    } else {
                        Console.WriteLine($"leal {ret.Label}+{ret.Offset}, %eax");
                    }
                    break;
                case Value.ValueKind.Ref:
                    if (ret.Label == null) {
                        Console.WriteLine($"leal {ret.Offset}(%ebp), %eax");
                    } else {
                        Console.WriteLine($"leal {ret.Label}+{ret.Offset}, %eax");
                    }
                    break;
                case Value.ValueKind.Address:
                case Value.ValueKind.Temp:
                    Console.WriteLine($"popl %eax");
                    break;
                default:
                    throw new NotImplementedException();
            }

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
                    Console.WriteLine($"movl (%eax), %ecx");
                    Console.WriteLine($"{op}l ${size}, (%eax)");
                    break;
                case 2:
                    Console.WriteLine($"movw (%eax), %cx");
                    if (ret.Type.IsSignedIntegerType()) {
                        Console.WriteLine($"movswl %cx, %ecx");
                    } else {
                        Console.WriteLine($"movzwl %cx, %ecx");
                    }
                    Console.WriteLine($"{op}w ${size}, (%eax)");
                    break;
                case 1:
                    Console.WriteLine($"movb (%eax), %cl");
                    if (ret.Type.IsSignedIntegerType()) {
                        Console.WriteLine($"movsbl %cl, %ecx");
                    } else {
                        Console.WriteLine($"movzbl %cl, %ecx");
                    }
                    Console.WriteLine($"{op}b ${size}, (%eax)");
                    break;
                default:
                    throw new NotImplementedException();
            }
            Console.WriteLine($"push %ecx");
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
            if (self.Lhs.Type.IsIntegerType() && self.Rhs.Type.IsIntegerType()) {

                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);

                Load(rhs, "%ecx");
                Load(lhs, "%eax");
                Console.WriteLine("cmpl %ecx, %eax");

                switch (self.Op) {
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterThan:
                        Console.WriteLine("setg	%al");
                        break;
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessThan:
                        Console.WriteLine("setl	%al");
                        break;
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterOrEqual:
                        Console.WriteLine("setge %al");
                        break;
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessOrEqual:
                        Console.WriteLine("setle %al");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                Console.WriteLine("movzbl %al, %eax");
                Console.WriteLine($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if ((self.Lhs.Type.IsRealFloatingType() || self.Lhs.Type.IsIntegerType()) &&
                       (self.Rhs.Type.IsRealFloatingType() || self.Rhs.Type.IsIntegerType())) {
                var lhs = self.Lhs.Accept(this, value);
                var rhs = self.Rhs.Accept(this, value);

                LoadF(rhs);
                LoadF(lhs);
                Console.WriteLine($"fcomip");
                Console.WriteLine($"fstp %st(0)");
                switch (self.Op) {
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterThan:
                        Console.WriteLine("seta	%al");
                        break;
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessThan:
                        Console.WriteLine("setb	%al");
                        break;
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterOrEqual:
                        Console.WriteLine("setae %al");
                        break;
                    case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessOrEqual:
                        Console.WriteLine("setbe %al");
                        break;
                    default:
                        throw new NotImplementedException();
                }

                Console.WriteLine($"movzbl %al, %eax");
                Console.WriteLine($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };

            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnShiftExpression(SyntaxTree.Expression.ShiftExpression self, Value value) {
            var lhs = self.Lhs.Accept(this, value);
            var rhs = self.Rhs.Accept(this, value);

            Load(rhs, "%ecx");
            Load(lhs, "%eax");

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
            Console.WriteLine($"{op} %cl, %eax");
            Console.WriteLine($"pushl %eax");
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
            if (ret.Type.IsIntegerType() && self.Type.IsIntegerType()) {
                var retty = ret.Type.Unwrap() as CType.BasicType;
                CType.BasicType.TypeKind selftykind;
                if (self.Type.Unwrap() is CType.BasicType) {
                    selftykind = (self.Type.Unwrap() as CType.BasicType).Kind;
                } else if (self.Type.Unwrap() is CType.TaggedType.EnumType) {
                    selftykind = CType.BasicType.TypeKind.SignedInt;
                } else {
                    throw new NotImplementedException();
                }

                Load(ret, "%eax");
                if (ret.Type.IsSignedIntegerType()) {
                    if (selftykind == CType.BasicType.TypeKind.Char || selftykind == CType.BasicType.TypeKind.SignedChar) {
                        Console.WriteLine($"movsbl %al, %eax");
                    } else if (selftykind == CType.BasicType.TypeKind.SignedShortInt) {
                        Console.WriteLine($"movswl %ax, %eax");
                    } else if (selftykind == CType.BasicType.TypeKind.SignedInt || selftykind == CType.BasicType.TypeKind.SignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax");  // do nothing;
                    } else if (selftykind == CType.BasicType.TypeKind.UnsignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selftykind == CType.BasicType.TypeKind.UnsignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selftykind == CType.BasicType.TypeKind.UnsignedInt || selftykind == CType.BasicType.TypeKind.UnsignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax");  // do nothing;
                    } else {
                        throw new NotImplementedException();
                    }
                } else {
                    if (selftykind == CType.BasicType.TypeKind.Char || selftykind == CType.BasicType.TypeKind.SignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selftykind == CType.BasicType.TypeKind.SignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selftykind == CType.BasicType.TypeKind.SignedInt || selftykind == CType.BasicType.TypeKind.SignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax");  // do nothing;
                    } else if (selftykind == CType.BasicType.TypeKind.UnsignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selftykind == CType.BasicType.TypeKind.UnsignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selftykind == CType.BasicType.TypeKind.UnsignedInt || selftykind == CType.BasicType.TypeKind.UnsignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax");  // do nothing;
                    } else {
                        throw new NotImplementedException();
                    }
                }
                Console.WriteLine($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (ret.Type.IsPointerType() && self.Type.IsPointerType()) {
                return ret;
            } else if (ret.Type.IsArrayType() && self.Type.IsPointerType()) {
                // 手抜き
                if (ret.Kind == Value.ValueKind.Var) {
                    ret = new Value() { Kind = Value.ValueKind.Ref, Type = self.Type, Label = ret.Label, Offset = ret.Offset };
                    return ret;
                } else if (ret.Kind == Value.ValueKind.Ref) {
                    ret = new Value() { Kind = Value.ValueKind.Ref, Type = self.Type, Label = ret.Label, Offset = ret.Offset };
                    return ret;
                } else if (ret.Kind == Value.ValueKind.Address) {
                    ret = new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
                    return ret;
                } else {
                    throw new NotImplementedException();
                }
            } else if (ret.Type.IsArrayType() && self.Type.IsArrayType()) {
                return ret;
            } else if (ret.Type.IsPointerType() && self.Type.IsArrayType()) {
                throw new NotImplementedException();
            } else if (ret.Type.IsIntegerType() && self.Type.IsPointerType()) {
                return ret;
            } else if (ret.Type.IsPointerType() && self.Type.IsIntegerType()) {
                return ret;
            } else if (ret.Type.IsBasicType(CType.BasicType.TypeKind.Float) && self.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                LoadF(ret);
                Console.WriteLine($"sub $8, %esp");
                Console.WriteLine($"fstpl (%esp)");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (ret.Type.IsBasicType(CType.BasicType.TypeKind.Double) && self.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                LoadF(ret);
                Console.WriteLine($"sub $4, %esp");
                Console.WriteLine($"fstps (%esp)");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (ret.Type.IsBasicType(CType.BasicType.TypeKind.Double) && self.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                LoadF(ret);
                Console.WriteLine($"sub $8, %esp");
                Console.WriteLine($"fstpl (%esp)");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (ret.Type.IsBasicType(CType.BasicType.TypeKind.Float) && self.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                LoadF(ret);
                Console.WriteLine($"sub $4, %esp");
                Console.WriteLine($"fstps (%esp)");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (ret.Type.IsRealFloatingType() && self.Type.IsIntegerType()) {
                LoadF(ret);

                // double -> unsigned char
                // 	movzwl <value>, %eax
                //  movzbl %al, %eax
                if (self.Type.IsSignedIntegerType()) {
                    switch (self.Type.Sizeof()) {
                        case 1:
                            Console.WriteLine($"sub $4, %esp");
                            Console.WriteLine($"fistps (%esp)");
                            Console.WriteLine($"movzwl (%esp), %eax");
                            Console.WriteLine($"movsbl %al, %eax");
                            Console.WriteLine($"movl %eax, (%esp)");
                            break;
                        case 2:
                            Console.WriteLine($"sub $4, %esp");
                            Console.WriteLine($"fistps (%esp)");
                            Console.WriteLine($"movzwl (%esp), %eax");
                            Console.WriteLine($"movl %eax, (%esp)");
                            break;
                        case 4:
                            Console.WriteLine($"sub $4, %esp");
                            Console.WriteLine($"fistpl (%esp)");
                            Console.WriteLine($"movl (%esp), %eax");
                            Console.WriteLine($"movl %eax, (%esp)");
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                } else {
                    switch (self.Type.Sizeof()) {
                        case 1:
                            Console.WriteLine($"sub $4, %esp");
                            Console.WriteLine($"fistps (%esp)");
                            Console.WriteLine($"movzwl (%esp), %eax");
                            Console.WriteLine($"movzbl %al, %eax");
                            Console.WriteLine($"movl %eax, (%esp)");
                            break;
                        case 2:
                            Console.WriteLine($"sub $4, %esp");
                            Console.WriteLine($"fistpl (%esp)");
                            Console.WriteLine($"movl (%esp), %eax");
                            Console.WriteLine($"movzwl %ax, %eax");
                            Console.WriteLine($"movl %eax, (%esp)");
                            break;
                        case 4:
                            // fistpq  16(%esp)
                            // movl    16(%esp), % eax
                            Console.WriteLine($"sub $8, %esp");
                            Console.WriteLine($"fistpq (%esp)");
                            Console.WriteLine($"movl (%esp), %eax");
                            Console.WriteLine($"add $4, %esp");
                            Console.WriteLine($"movl %eax, (%esp)");
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                }

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (ret.Type.IsIntegerType() && self.Type.IsRealFloatingType()) {
                var retty = ret.Type.Unwrap() as CType.BasicType;
                CType.BasicType.TypeKind selftykind;
                if (self.Type.Unwrap() is CType.BasicType) {
                    selftykind = (self.Type.Unwrap() as CType.BasicType).Kind;
                } else if (self.Type.Unwrap() is CType.TaggedType.EnumType) {
                    selftykind = CType.BasicType.TypeKind.SignedInt;
                } else {
                    throw new NotImplementedException();
                }

                LoadF(ret);
                if (self.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                    Console.WriteLine($"sub $4, %esp");
                    Console.WriteLine($"fstps (%esp)");
                } else if (self.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                    Console.WriteLine($"sub $8, %esp");
                    Console.WriteLine($"fstpl (%esp)");
                } else {
                    throw new NotImplementedException();
                }
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };

            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnUnaryAddressExpression(SyntaxTree.Expression.UnaryAddressExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            if (operand.Kind == Value.ValueKind.Var) {
                if (operand.Label == null) {
                    Console.WriteLine($"leal {operand.Offset}(%ebp), %eax");
                } else {
                    Console.WriteLine($"leal {operand.Label}+{operand.Offset}, %eax");
                }
                Console.WriteLine($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (operand.Kind == Value.ValueKind.Ref) {
                if (operand.Label == null) {
                    Console.WriteLine($"leal {operand.Offset}(%ebp), %eax");
                } else {
                    Console.WriteLine($"leal {operand.Label}+{operand.Offset}, %eax");
                }
                Console.WriteLine($"pushl %eax");
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
                operand = Load(operand, "%eax");
                Console.WriteLine($"negl %eax");
                Console.WriteLine($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (operand.Type.IsRealFloatingType()) {
                LoadF(operand);
                Console.WriteLine($"fchs");
                if (self.Type.IsBasicType(CType.BasicType.TypeKind.Float)) {
                    Console.WriteLine($"sub $4, %esp");
                    Console.WriteLine($"fstps (%esp)");
                } else if (self.Type.IsBasicType(CType.BasicType.TypeKind.Double)) {
                    Console.WriteLine($"sub $8, %esp");
                    Console.WriteLine($"fstpl (%esp)");
                } else {
                    throw new NotImplementedException();
                }
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnUnaryNegateExpression(SyntaxTree.Expression.UnaryNegateExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            operand = Load(operand, "%eax");
            Console.WriteLine($"notl %eax");
            Console.WriteLine($"pushl %eax");
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnUnaryNotExpression(SyntaxTree.Expression.UnaryNotExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            operand = Load(operand, "%eax");
            Console.WriteLine($"cmpl $0, %eax");
            Console.WriteLine($"sete %al");
            Console.WriteLine($"movzbl %al, %eax");
            Console.WriteLine($"pushl %eax");
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnUnaryPlusExpression(SyntaxTree.Expression.UnaryPlusExpression self, Value value) {
            return self.Expr.Accept(this, value);
        }

        public Value OnUnaryPrefixExpression(SyntaxTree.Expression.UnaryPrefixExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            // load address
            if (ret.Kind == Value.ValueKind.Var) {
                if (ret.Label == null) {
                    Console.WriteLine($"leal {ret.Offset}(%ebp), %eax");
                } else {
                    Console.WriteLine($"leal {ret.Label}+{ret.Offset}, %eax");
                }
            } else if (ret.Kind == Value.ValueKind.Ref) {
                if (ret.Label == null) {
                    Console.WriteLine($"leal {ret.Offset}(%ebp), %eax");
                } else {
                    Console.WriteLine($"leal {ret.Label}+{ret.Offset}, %eax");
                }
            } else if (ret.Kind == Value.ValueKind.Address || ret.Kind == Value.ValueKind.Temp) {
                Console.WriteLine($"popl %eax");
            } else {
                throw new NotImplementedException();
            }

            string op = "";
            switch (self.Op) {
                case SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Inc:
                    op = "add";
                    break;
                case SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Dec:
                    op = "add";
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
                    Console.WriteLine($"movl (%eax), %ecx");
                    Console.WriteLine($"{op}l ${size}, (%eax)");
                    break;
                case 2:
                    Console.WriteLine($"movw (%eax), %cx");
                    if (ret.Type.IsSignedIntegerType()) {
                        Console.WriteLine($"movswl %cx, %ecx");
                    } else {
                        Console.WriteLine($"movzwl %cx, %ecx");
                    }
                    Console.WriteLine($"{op}w ${size}, (%eax)");
                    break;
                case 1:
                    Console.WriteLine($"movb (%eax), %cl");
                    if (ret.Type.IsSignedIntegerType()) {
                        Console.WriteLine($"movsbl %cl, %ecx");
                    } else {
                        Console.WriteLine($"movzbl %cl, %ecx");
                    }
                    Console.WriteLine($"{op}b ${size}, (%eax)");
                    break;
                default:
                    throw new NotImplementedException();
            }
            Console.WriteLine($"push (%eax)");
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };

        }

        public Value OnUnaryReferenceExpression(SyntaxTree.Expression.UnaryReferenceExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            Load(ret, "%eax");
            Console.WriteLine($"pushl %eax");
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
                        Console.WriteLine($"leal {value.Offset}(%ebp), %eax");
                    } else {
                        throw new NotImplementedException();
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            switch (self.Type.Sizeof()) {
                case 1:
                    rhs = Load(rhs, "%ecx");
                    Console.WriteLine($"movb %cl, (%eax)");
                    break;
                case 2:
                    rhs = Load(rhs, "%ecx");
                    Console.WriteLine($"movw %cx, (%eax)");
                    break;
                case 4:
                    rhs = Load(rhs, "%ecx");
                    Console.WriteLine($"movl %ecx, (%eax)");
                    break;
                default: {
                        switch (rhs.Kind) {
                            case Value.ValueKind.FloatConst: {
                                    var bytes = BitConverter.GetBytes(rhs.FloatConst);
                                    var qwordlo = BitConverter.ToUInt32(bytes, 0);
                                    var qwordhi = BitConverter.ToUInt32(bytes, 4);
                                    Console.WriteLine($"movl ${qwordlo}, 0(%eax)");
                                    Console.WriteLine($"movl ${qwordhi}, 4(%eax)");
                                    return new Value() { Kind = Value.ValueKind.Void };
                                }
                            case Value.ValueKind.Var:
                                if (value.Label == null) {
                                    Console.WriteLine($"leal {value.Offset}(%ebp), %esi");
                                } else {
                                    Console.WriteLine($"leal {value.Label}+{value.Offset}, %esi");
                                }
                                break;
                            case Value.ValueKind.Ref:
                                if (rhs.Label == null) {
                                    Console.WriteLine($"leal {rhs.Offset}(%ebp), %esi");
                                } else {
                                    Console.WriteLine($"leal {rhs.Label}+{rhs.Offset}, %esi");
                                }
                                break;
                            case Value.ValueKind.Address:
                                Console.WriteLine($"popl %esi");
                                break;
                            case Value.ValueKind.Temp:
                                Console.WriteLine($"leal (%esp), %esi");
                                Console.WriteLine($"pushl %ecx");
                                Console.WriteLine($"movl ${self.Type.Sizeof()}, %ecx");
                                Console.WriteLine($"movl %eax, %edi");
                                Console.WriteLine($"cld");
                                Console.WriteLine($"rep movsb");
                                Console.WriteLine($"pop %ecx");
                                Console.WriteLine($"add ${self.Type.Sizeof()}, %esp");
                                return new Value() { Kind = Value.ValueKind.Void };
                            default:
                                throw new NotImplementedException();
                        }
                        Console.WriteLine($"pushl %ecx");
                        Console.WriteLine($"movl ${self.Type.Sizeof()}, %ecx");
                        Console.WriteLine($"movl %eax, %edi");
                        Console.WriteLine($"cld");
                        Console.WriteLine($"rep movsb");
                        Console.WriteLine($"pop %ecx");
                        break;
                    }
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
            Console.WriteLine($"jmp {label}");
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnCaseStatement(SyntaxTree.Statement.CaseStatement self, Value value) {
            var label = _switchLabelTableStack.Peek()[self];
            Console.WriteLine($"{label}:");
            self.Stmt.Accept(this, value);
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
            Console.WriteLine($"andl $-16, %esp");
            Console.WriteLine($"subl ${localScopeSize}, %esp");

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
            Console.WriteLine($"jmp {label}");
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnDefaultStatement(SyntaxTree.Statement.DefaultStatement self, Value value) {
            var label = _switchLabelTableStack.Peek()[self];
            Console.WriteLine($"{label}:");
            self.Stmt.Accept(this, value);
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnDoWhileStatement(SyntaxTree.Statement.DoWhileStatement self, Value value) {
            var labelContinue = LAlloc();
            var labelBreak = LAlloc();

            // Check Loop Condition
            Console.WriteLine($"{labelContinue}:");
            ContinueTarget.Push(labelContinue);
            BreakTarget.Push(labelBreak);
            var stmt = self.Stmt.Accept(this, value);
            discard(stmt);
            ContinueTarget.Pop();
            BreakTarget.Pop();

            var ret = self.Cond.Accept(this, value);
            Load(ret, "%eax");
            Console.WriteLine($"cmpl $0, %eax");
            Console.WriteLine($"jne {labelContinue}");
            Console.WriteLine($"{labelBreak}:");
            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnEmptyStatement(SyntaxTree.Statement.EmptyStatement self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnExpressionStatement(SyntaxTree.Statement.ExpressionStatement self, Value value) {
            var ret = self.Expr.Accept(this, value);
            discard(ret);
            return value;
        }

        public Value OnForStatement(SyntaxTree.Statement.ForStatement self, Value value) {
            // Initialize
            var ret = self.Init.Accept(this, value);
            discard(ret);

            var labelHead = LAlloc();
            var labelContinue = LAlloc();
            var labelBreak = LAlloc();

            // Check Loop Condition
            Console.WriteLine($"{labelHead}:");
            ret = self.Cond.Accept(this, value);
            Load(ret, "%eax");
            Console.WriteLine($"cmpl $0, %eax");
            Console.WriteLine($"je {labelBreak}");
            ContinueTarget.Push(labelContinue);
            BreakTarget.Push(labelBreak);
            var stmt = self.Stmt.Accept(this, value);
            discard(stmt);
            ContinueTarget.Pop();
            BreakTarget.Pop();

            Console.WriteLine($"{labelContinue}:");
            var update = self.Update.Accept(this, value);
            discard(update);

            Console.WriteLine($"jmp {labelHead}");
            Console.WriteLine($"{labelBreak}:");

            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnGenericLabeledStatement(SyntaxTree.Statement.GenericLabeledStatement self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnGotoStatement(SyntaxTree.Statement.GotoStatement self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnIfStatement(SyntaxTree.Statement.IfStatement self, Value value) {
            var cond = self.Cond.Accept(this, value);
            Load(cond, "%eax");
            Console.WriteLine($"cmpl $0, %eax");

            if (self.ElseStmt != null) {
                var elseLabel = LAlloc();
                var junctionLabel = LAlloc();

                Console.WriteLine($"je {elseLabel}");

                var thenRet = self.ThenStmt.Accept(this, value);
                discard(thenRet);
                Console.WriteLine($"jmp {junctionLabel}");
                Console.WriteLine($"{elseLabel}:");
                var elseRet = self.ElseStmt.Accept(this, value);
                discard(elseRet);
                Console.WriteLine($"{junctionLabel}:");
            } else {
                var junctionLabel = LAlloc();

                Console.WriteLine($"je {junctionLabel}");

                var thenRet = self.ThenStmt.Accept(this, value);
                discard(thenRet);
                Console.WriteLine($"{junctionLabel}:");
            }


            return new Value() { Kind = Value.ValueKind.Void };
        }

        /// <summary>
        /// 値を指定したレジスタにロードする。レジスタに入らないサイズはエラーになる
        /// </summary>
        /// <param name="value"></param>
        /// <param name="register"></param>
        /// <returns></returns>
        private Value Load(Value value, string register) {
            var ValueType = value.Type;
            CType elementType;
            switch (value.Kind) {
                case Value.ValueKind.IntConst: {
                        // 定数値をレジスタにロードする。
                        string op = "";
                        if (register != "%eax") {
                            Console.WriteLine($"pushl %eax");
                        }
                        if (ValueType.IsSignedIntegerType() || ValueType.IsBasicType(CType.BasicType.TypeKind.Char)) {
                            switch (ValueType.Sizeof()) {
                                case 1:
                                    Console.WriteLine($"movb ${value.IntConst}, %al");
                                    Console.WriteLine($"movsbl %al, {register}");
                                    break;
                                case 2:
                                    Console.WriteLine($"movw ${value.IntConst}, %ax");
                                    Console.WriteLine($"movswl %al, {register}");
                                    break;
                                case 3:
                                case 4:
                                    Console.WriteLine($"movl ${value.IntConst}, {register}");
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        } else {
                            switch (ValueType.Sizeof()) {
                                case 1:
                                    Console.WriteLine($"movb ${value.IntConst}, %al");
                                    Console.WriteLine($"movzbl %al, {register}");
                                    break;
                                case 2:
                                    Console.WriteLine($"movw ${value.IntConst}, %ax");
                                    Console.WriteLine($"movzwl %al, {register}");
                                    break;
                                case 3:
                                case 4:
                                    Console.WriteLine($"movl ${value.IntConst}, {register}");
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        if (register != "%eax") {
                            Console.WriteLine($"popl %eax");
                        }
                        return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = register };
                    }
                case Value.ValueKind.Temp:
                    if (ValueType.Sizeof() <= 4) {
                        // スタックトップの値をレジスタにロード
                        Console.WriteLine($"popl {register}");
                        return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = register };

                    } else {
                        // スタックトップのアドレスをレジスタにロード
                        Console.WriteLine($"mov %esp, {register}");
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
                                Console.WriteLine($"popl {register}");
                                src = $"({register})";
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        string op = "";
                        if (ValueType.IsSignedIntegerType() || ValueType.IsBasicType(CType.BasicType.TypeKind.Char) || ValueType.IsEnumeratedType()) {
                            switch (ValueType.Sizeof()) {
                                case 1: op = "movsbl"; break;
                                case 2: op = "movswl"; break;
                                case 4: op = "movl"; break;
                                default: throw new NotImplementedException();
                            }
                        } else if (ValueType.IsUnsignedIntegerType()) {
                            switch (ValueType.Sizeof()) {
                                case 1: op = "movzbl"; break;
                                case 2: op = "movzwl"; break;
                                case 4: op = "movl"; break;
                                default: throw new NotImplementedException();
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
                        Console.WriteLine($"{op} {src}, {register}");
                        return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = register };
                    }
                    break;
                case Value.ValueKind.Ref:
                    if (value.Label == null) {
                        Console.WriteLine($"leal {value.Offset}(%ebp), {register}");
                    } else {
                        Console.WriteLine($"leal {value.Label}+{value.Offset}, {register}");
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
        private void LoadF(Value rhs) {
            switch (rhs.Kind) {
                case Value.ValueKind.IntConst:
                    Console.WriteLine($"pushl ${rhs.IntConst}");
                    Console.WriteLine($"fild (%esp)");
                    Console.WriteLine($"addl $4, %esp");
                    break;
                case Value.ValueKind.FloatConst:
                    if (rhs.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Float)) {
                        var bytes = BitConverter.GetBytes((float)rhs.FloatConst);
                        var dword = BitConverter.ToUInt32(bytes, 0);
                        Console.WriteLine($"pushl ${dword}");
                        Console.WriteLine($"flds (%esp)");
                        Console.WriteLine($"addl $4, %esp");
                    } else if (rhs.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Double)) {
                        var bytes = BitConverter.GetBytes(rhs.FloatConst);
                        var qwordlo = BitConverter.ToUInt32(bytes, 0);
                        var qwordhi = BitConverter.ToUInt32(bytes, 4);
                        Console.WriteLine($"pushl ${qwordhi}");
                        Console.WriteLine($"pushl ${qwordlo}");
                        Console.WriteLine($"fldl (%esp)");
                        Console.WriteLine($"addl $8, %esp");
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
                            Console.WriteLine($"{op} {rhs.Offset}(%ebp)");
                        } else {
                            Console.WriteLine($"{op} {rhs.Label}+{rhs.Offset}");
                        }
                        break;
                    }
                case Value.ValueKind.Temp:
                    if (rhs.Type.Unwrap().IsBasicType(CType.BasicType.TypeKind.Float)) {
                        Console.WriteLine($"flds (%esp)");
                        Console.WriteLine($"addl $4, %esp");
                    } else {
                        Console.WriteLine($"fldl (%esp)");
                        Console.WriteLine($"addl $8, %esp");
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public Value OnReturnStatement(SyntaxTree.Statement.ReturnStatement self, Value value) {
            if (self.Expr != null) {
                value = self.Expr.Accept(this, value);
                if (value.Type.IsSignedIntegerType()) {
                    value = Load(value, "%ecx");
                    switch (value.Type.Sizeof()) {
                        case 1: Console.WriteLine($"movsbl %cl, %eax"); break;
                        case 2: Console.WriteLine($"movswl %cx, %eax"); break;
                        case 4: Console.WriteLine($"movl %ecx, %eax"); break;
                        default: throw new NotImplementedException();
                    }
                } else if (value.Type.IsUnsignedIntegerType()) {
                    value = Load(value, "%ecx");
                    switch (value.Type.Sizeof()) {
                        case 1: Console.WriteLine($"movzbl %cl, %eax"); break;
                        case 2: Console.WriteLine($"movxwl %cx, %eax"); break;
                        case 4: Console.WriteLine($"movl %ecx, %eax"); break;
                        default: throw new NotImplementedException();
                    }
                } else if (value.Type.IsRealFloatingType()) {
                    LoadF(value);
                } else {
                    if (value.Type.Sizeof() <= 4) {
                        value = Load(value, "%eax");
                    } else {
                        switch (value.Kind) {
                            case Value.ValueKind.Var:
                                if (value.Label == null) {
                                    Console.WriteLine($"leal {value.Offset}(%ebp), %esi");
                                } else {
                                    Console.WriteLine($"leal {value.Label}+{value.Offset}, %esi");
                                }
                                break;
                            case Value.ValueKind.Ref:
                                if (value.Label == null) {
                                    Console.WriteLine($"leal {value.Offset}(%ebp), %esi");
                                } else {
                                    Console.WriteLine($"leal {value.Label}+{value.Offset}, %esi");
                                }
                                break;
                            case Value.ValueKind.Temp:
                                Console.WriteLine($"movl %esp, %esi");
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        Console.WriteLine($"movl ${value.Type.Sizeof()}, %ecx");
                        Console.WriteLine($"movl 8(%ebp), %edi");
                        Console.WriteLine($"cld");
                        Console.WriteLine($"rep movsb");
                        if (value.Kind == Value.ValueKind.Temp) {
                            Console.WriteLine($"addl {(value.Type.Sizeof() + 3) & ~3}, %esp");
                        }
                    }

                }
            }
            Console.WriteLine($"movl %ebp, %esp");
            Console.WriteLine($"popl %ebp");
            Console.WriteLine($"ret");
            return new Value() { Kind = Value.ValueKind.Void };
        }

        private Stack<Dictionary<SyntaxTree.Statement, string>> _switchLabelTableStack = new Stack<Dictionary<SyntaxTree.Statement, string>>();

        public Value OnSwitchStatement(SyntaxTree.Statement.SwitchStatement self, Value value) {
            var labelBreak = LAlloc();

            var ret = self.Cond.Accept(this, value);
            Load(ret, "%eax");

            var labelDic = new Dictionary<SyntaxTree.Statement, string>();
            foreach (var caseLabel in self.CaseLabels) {
                var caseValue = caseLabel.Value;
                var label = LAlloc();
                labelDic.Add(caseLabel, label);
                Console.WriteLine($"cmp ${caseValue}, %eax");
                Console.WriteLine($"je {label}");
            }
            if (self.DefaultLabel != null) {
                var label = LAlloc();
                labelDic.Add(self.DefaultLabel, label);
                Console.WriteLine($"jmp {label}");
            } else {
                Console.WriteLine($"jmp {labelBreak}");
            }

            _switchLabelTableStack.Push(labelDic);
            BreakTarget.Push(labelBreak);
            var stmt = self.Stmt.Accept(this, value);
            discard(stmt);
            BreakTarget.Pop();
            _switchLabelTableStack.Pop();
            Console.WriteLine($"{labelBreak}:");

            return new Value() { Kind = Value.ValueKind.Void };
        }

        public Value OnWhileStatement(SyntaxTree.Statement.WhileStatement self, Value value) {
            var labelContinue = LAlloc();
            var labelBreak = LAlloc();

            // Check Loop Condition
            Console.WriteLine($"{labelContinue}:");
            var ret = self.Cond.Accept(this, value);
            Load(ret, "%eax");
            Console.WriteLine($"cmpl $0, %eax");
            Console.WriteLine($"je {labelBreak}");
            ContinueTarget.Push(labelContinue);
            BreakTarget.Push(labelBreak);
            var stmt = self.Stmt.Accept(this, value);
            discard(stmt);
            ContinueTarget.Pop();
            BreakTarget.Pop();

            Console.WriteLine($"jmp {labelContinue}");
            Console.WriteLine($"{labelBreak}:");

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
            Console.WriteLine($".data");
            foreach (var data in dataBlock) {
                Console.WriteLine($"{data.Item1}:");
                foreach (var b in data.Item2) {
                    Console.WriteLine($".byte {b}");
                }
            }

            return value;
        }

    }

    public class FileScopeInitializerVisitor : SyntaxTreeVisitor.IVisitor<SyntaxTreeCompileVisitor.Value, SyntaxTreeCompileVisitor.Value> {
        private SyntaxTreeCompileVisitor syntaxTreeCompileVisitor;

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
                Console.WriteLine($".section .data");
                self.Init.Accept(this, value);
                Console.WriteLine($".align 4");
                Console.WriteLine($"{self.LinkageObject.LinkageId}:");
                foreach (var val in this.Values) {
                    var v = val.Accept(this, value);
                    switch (v.Kind) {
                        case SyntaxTreeCompileVisitor.Value.ValueKind.IntConst:
                            switch (v.Type.Sizeof()) {
                                case 1: Console.WriteLine($".byte {(byte)v.IntConst}"); break;
                                case 2: Console.WriteLine($".word {(ushort)v.IntConst}"); break;
                                case 4: Console.WriteLine($".long {(uint)v.IntConst}"); break;
                                default: throw new ArgumentOutOfRangeException();
                            }
                            break;
                        case SyntaxTreeCompileVisitor.Value.ValueKind.FloatConst:
                            throw new NotImplementedException();
                            break;
                        case SyntaxTreeCompileVisitor.Value.ValueKind.Var:
                            if (v.Label == null) {
                                throw new Exception("ファイルスコープオブジェクトの参照では無い。");
                            }
                            Console.WriteLine($".long {v.Label}+{v.Offset}"); break;
                            break;
                        case SyntaxTreeCompileVisitor.Value.ValueKind.Ref:
                            if (v.Label == null) {
                                throw new Exception("ファイルスコープオブジェクトの参照では無い。");
                            }
                            Console.WriteLine($".long {v.Label}+{v.Offset}"); break;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            } else {
                Console.WriteLine($".section .bss");
                Console.WriteLine($".align 4");
                Console.WriteLine($".comm {self.LinkageObject.LinkageId}, {self.LinkageObject.Type.Sizeof()}");
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
            return new SyntaxTreeCompileVisitor.Value() { Kind = SyntaxTreeCompileVisitor.Value.ValueKind.Ref, Label = self.Identifier.Ident, Offset = (int)self.Offset.Value, Type = self.Type };
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
            throw new NotImplementedException();
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