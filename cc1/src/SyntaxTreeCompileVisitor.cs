using System;
using System.Collections.Generic;
using System.Linq;
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
         *  - 関数の戻り値は EAXに格納できるサイズならば EAX に格納される。EAXに格納できないサイズならば、戻り値を格納する領域のアドレスを引数の上に積み、EAXを使わない。（※）
         *  - 呼び出された側の関数ではEAX, ECX, EDXのレジスタの元の値を保存することなく使用してよい。
         *    呼び出し側の関数では必要ならば呼び出す前にそれらのレジスタをスタック上などに保存する。
         *  - スタックポインタの処理は呼び出し側で行う。
         *  - 引数・戻り値領域の開放は呼び出し側で行う
         *  
         */

        /*
         * コード生成:
         *  - スタック計算機で行う
         */

        public class Value {
            public enum ValueKind {
                Void,       // 結果はない
                Register,   // 式の結果はレジスタ上の値である（値はレジスタ Register に入っている）
                Temp,       // 式の結果はスタック上の値である（値はスタックの一番上にある）
                IntConst,   // 式の結果は整数定数値である
                FloatConst, // 式の結果は浮動小数点定数値である
                GlobalVar,  // 式の結果はグローバル変数参照である
                LocalVar,   // 式の結果はローカル変数参照である
                Address,    // 式の結果はアドレス参照である（アドレス値はスタックの一番上にある）
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
        string ReturnTarget = null;
        private Dictionary<string, int> arguments;

        /// <summary>
        /// 文字列リテラルなどの静的データ
        /// </summary>
        List<Tuple<string,byte[]>> dataBlock = new List<Tuple<string,byte[]>>();

        int n = 0;

        private string LAlloc() {
            return $"L{n++}";
        }

        private void discard(Value v) {
            if (v.Kind == Value.ValueKind.Temp || v.Kind == Value.ValueKind.Address) {
                Console.WriteLine("popl %eax");  // discard
            }
        }


        public Value OnArgumentDeclaration(SyntaxTree.Declaration.ArgumentDeclaration self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnFunctionDeclaration(SyntaxTree.Declaration.FunctionDeclaration self, Value value) {
            if (self.Body != null) {
                // 引数表
                var ft = (self.Type as CType.FunctionType);
                int offset = 4; // prev return position

                // 戻り値領域
                if (!ft.ResultType.IsVoidType() && ft.ResultType.Sizeof() > 4) {
                    offset += (ft.ResultType.Sizeof() + 3) & ~3;
                }
                // 引数（先頭から）
                arguments = new Dictionary<string, int>();
                foreach (var arg in ft.Arguments) {
                    offset += (arg.Type.Sizeof() + 3) & ~3;
                    arguments.Add(arg.Ident, offset);
                    
                }


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
            if (self.LinkageObject.Linkage != LinkageKind.NoLinkage) {
                Console.WriteLine($".section .bss");
                Console.WriteLine($".comm {self.LinkageObject.LinkageId}, {self.LinkageObject.Type.Sizeof()}");
            } else {
                if (self.Init != null) {
                    return self.Init.Accept(this, value);
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
                var target = self.Lhs.Accept(this, value);
                var index = self.Rhs.Accept(this, value);

                Load(index, "%ecx");

                if (target.Kind == Value.ValueKind.LocalVar) {
                    Console.WriteLine($"leal {-target.Offset}(%ebp), %eax");
                } else if (target.Kind == Value.ValueKind.GlobalVar) {
                    Console.WriteLine($"leal {target.Label}, %eax");
                } else if (target.Kind == Value.ValueKind.Address) {
                    Console.WriteLine($"popl %eax");
                } else {
                    throw new NotImplementedException();
                }

                Console.WriteLine($"imull ${self.Type.Sizeof()}, %ecx, %ecx");
                Console.WriteLine($"leal (%eax, %ecx), %eax");
                Console.WriteLine($"pushl %eax");

                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (self.Lhs.Type.IsIntegerType() && self.Rhs.Type.IsPointerType()) {
                var target = self.Lhs.Accept(this, value);
                var index = self.Rhs.Accept(this, value);

                Load(index, "%ecx");

                if (target.Kind == Value.ValueKind.LocalVar) {
                    Console.WriteLine($"leal {-target.Offset}(%ebp), %eax");
                } else if (target.Kind == Value.ValueKind.GlobalVar) {
                    Console.WriteLine($"leal {target.Label}+{target.Offset}, %eax");
                } else if (target.Kind == Value.ValueKind.Address) {
                    Console.WriteLine($"popl %eax");
                } else {
                    throw new NotImplementedException();
                }

                Console.WriteLine($"imull ${self.Type.Sizeof()}, %ecx, %ecx");
                Console.WriteLine($"leal (%eax, %ecx), %eax");
                Console.WriteLine($"pushl %eax");

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
            Console.WriteLine($"pushl %edi");

            var rhs = self.Rhs.Accept(this, value);
            var lhs = self.Lhs.Accept(this, value);


            if (lhs.Kind == Value.ValueKind.LocalVar) {
                Console.WriteLine($"leal {-lhs.Offset}(%ebp), %edi");
            } else if (lhs.Kind == Value.ValueKind.GlobalVar) {
                Console.WriteLine($"leal {lhs.Label}+{lhs.Offset}, %edi");
            } else if (lhs.Kind == Value.ValueKind.Address) {
                Console.WriteLine($"popl %edi");
            } else {
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
                    } else {
                        Console.WriteLine($"mull %ecx");
                    }
                    Console.WriteLine($"mov %eax, %ecx");
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.DIV_ASSIGN:
                    Console.WriteLine($"mov (%edi), %eax");
                    if (self.Type.IsSignedIntegerType()) {
                        Console.WriteLine($"idivl %ecx");
                    } else {
                        Console.WriteLine($"divl %ecx");
                    }
                    Console.WriteLine($"mov %eax, %ecx");
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MOD_ASSIGN:
                    Console.WriteLine($"mov (%edi), %eax");
                    if (self.Type.IsSignedIntegerType()) {
                        Console.WriteLine($"idivl %ecx");
                    } else {
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
                    } else {
                        Console.WriteLine($"shll (%edi), %ecx");
                    }
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.RIGHT_ASSIGN:
                    if (self.Type.IsSignedIntegerType()) {
                        Console.WriteLine($"sarl (%edi), %ecx");
                    } else {
                        Console.WriteLine($"shrl (%edi), %ecx");
                    }
                    break;
                default:
                    throw new Exception("来ないはず");
            }
            Console.WriteLine($"movl %ecx, (%edi)");

            Console.WriteLine($"popl %edi");

            Console.WriteLine($"pushl %ecx");
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnSimpleAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, Value value) {
            var lhs = self.Lhs.Accept(this, value);
            var rhs = self.Rhs.Accept(this, value);

            rhs = Load(rhs, "%ecx");

            var dst = "";
            switch (lhs.Kind) {
                case Value.ValueKind.LocalVar:
                    Console.WriteLine($"leal {-lhs.Offset}(%ebp), %eax");
                    break;
                case Value.ValueKind.GlobalVar:
                    dst = lhs.Label;
                    Console.WriteLine($"leal {lhs.Label}+{lhs.Offset}, %eax");
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
                    break;
                case 2:
                    Console.WriteLine($"movw %cx, (%eax)");
                    break;
                case 4:
                    Console.WriteLine($"movl %ecx, (%eax)");
                    break;
                default:
                    throw new NotImplementedException();
            }

            Console.WriteLine($"pushl %ecx");

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
                    }
                    else if (selfty.Kind == CType.BasicType.TypeKind.SignedShortInt) {
                        Console.WriteLine($"movswl %ax, %eax");
                    }
                    else if (selfty.Kind == CType.BasicType.TypeKind.SignedInt || selfty.Kind == CType.BasicType.TypeKind.SignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax"); // nothing to do;
                    }
                    else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    }
                    else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    }
                    else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedInt || selfty.Kind == CType.BasicType.TypeKind.UnsignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax"); // nothing to do;
                    }
                    else {
                        throw new NotImplementedException();
                    }
                }
                else {
                    if (selfty.Kind == CType.BasicType.TypeKind.Char || selfty.Kind == CType.BasicType.TypeKind.SignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    }
                    else if (selfty.Kind == CType.BasicType.TypeKind.SignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    }
                    else if (selfty.Kind == CType.BasicType.TypeKind.SignedInt || selfty.Kind == CType.BasicType.TypeKind.SignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax"); // nothing to do;
                    }
                    else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    }
                    else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    }
                    else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedInt || selfty.Kind == CType.BasicType.TypeKind.UnsignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax"); // nothing to do;
                    }
                    else {
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
            throw new NotImplementedException();
        }

        public Value OnEqualityExpression(SyntaxTree.Expression.EqualityExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnExclusiveOrExpression(SyntaxTree.Expression.ExclusiveOrExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnGccStatementExpression(SyntaxTree.Expression.GccStatementExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnInclusiveOrExpression(SyntaxTree.Expression.InclusiveOrExpression self, Value value) {
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
                        Console.WriteLine($"movl %eax, %eax");  // nothing to do;
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedInt || selfty.Kind == CType.BasicType.TypeKind.UnsignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax");  // nothing to do;
                    } else {
                        throw new NotImplementedException();
                    }
                } else {
                    if (selfty.Kind == CType.BasicType.TypeKind.Char || selfty.Kind == CType.BasicType.TypeKind.SignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.SignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.SignedInt || selfty.Kind == CType.BasicType.TypeKind.SignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax");  // nothing to do;
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedInt || selfty.Kind == CType.BasicType.TypeKind.UnsignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax");  // nothing to do;
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
            throw new NotImplementedException();
        }

        public Value OnLogicalOrExpression(SyntaxTree.Expression.LogicalOrExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnMultiplicitiveExpression(SyntaxTree.Expression.MultiplicitiveExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnArraySubscriptingExpression(SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression self, Value value) {
            var target = self.Target.Accept(this, value);
            var index = self.Index.Accept(this, value);

            Load(index, "%ecx");
            if (target.Kind == Value.ValueKind.LocalVar) {
                Console.WriteLine($"leal {-target.Offset}(%ebp), %eax");
            } else if (target.Kind == Value.ValueKind.GlobalVar) {
                Console.WriteLine($"leal {target.Label}+{target.Offset}, %eax");
            } else if (target.Kind == Value.ValueKind.Address || target.Kind == Value.ValueKind.Temp) {
                Console.WriteLine($"popl %eax");
            } else {
                throw new NotImplementedException();
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
                }
                else {
                    // 戻り値はスタックの上にあるはず
                    //Console.WriteLine($"subl ${_argSize}, %esp");
                    //Console.WriteLine($"movl ${retSize / 4}, %ecx");
                    //Console.WriteLine($"movl (%esp), %esi");
                    //Console.WriteLine($"leal ${(retSize + argSize + bakSize)}(%esp), %edi");
                    //Console.WriteLine($"cld");
                    //Console.WriteLine($"rep movsd");
                    //Console.WriteLine($"addl ${retSize + argSize}, %esp");
                }
            }

            // 戻り値がeaxに入らないならスタック上に領域を確保
            if (resultSize > 4) {
                Console.WriteLine($"subl ${resultSize}, %esp");
            }

            var func = self.Expr.Accept(this, value);
            Load(func, "%eax");
            Console.WriteLine($"call *%eax");
            if (resultSize > 4) {
                // 戻り値をコピー
                Console.WriteLine($"movl ${resultSize / 4}, %ecx");
                Console.WriteLine($"movl (%esp), %esi");
                Console.WriteLine($"leal ${(argSize + bakSize)}(%esp), %edi");
                Console.WriteLine($"cld");
                Console.WriteLine($"rep movsd");
                Console.WriteLine($"addl ${resultSize + argSize}, %esp");
            } else if (resultSize > 0) {
                // 戻り値をコピー
                Console.WriteLine($"movl %eax, {(argSize + bakSize)}(%esp)");
                Console.WriteLine($"addl ${argSize}, %esp");
            }
            Console.WriteLine($"popl %edx");
            Console.WriteLine($"popl %ecx");
            Console.WriteLine($"popl %eax");

            return new Value() { Kind = resultSize == 0 ? Value.ValueKind.Void : Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnMemberDirectAccess(SyntaxTree.Expression.PostfixExpression.MemberDirectAccess self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnMemberIndirectAccess(SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnUnaryPostfixExpression(SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            // load address
            if (ret.Kind == Value.ValueKind.LocalVar) {
                Console.WriteLine($"leal {-ret.Offset}(%ebp), %eax");
            } else if (ret.Kind == Value.ValueKind.GlobalVar) {
                Console.WriteLine($"leal {ret.Label}+{ret.Offset}, %eax");
            } else if (ret.Kind == Value.ValueKind.Address || ret.Kind == Value.ValueKind.Temp) {
                Console.WriteLine($"popl %eax");
            } else {
                throw new NotImplementedException();
            }

            var op = "";
            switch (self.Op) {
                case SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Dec:
                    op = "dec";
                    break;
                case SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Inc:
                    op = "inc";
                    break;
                default:
                    throw new NotImplementedException();
            }
            // load value
            switch (ret.Type.Sizeof()) {
                case 4:
                    Console.WriteLine($"movl (%eax), %ecx");
                    Console.WriteLine($"{op}l (%eax)");
                    break;
                case 2:
                    Console.WriteLine($"movw (%eax), %ecx");
                    Console.WriteLine($"{op}w (%eax)");
                    break;
                case 1:
                    Console.WriteLine($"movb (%eax), %ecx");
                    Console.WriteLine($"{op}b (%eax)");
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

        public Value OnEnumerationConstant(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnFunctionExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, Value value) {
            return new Value() { Kind = Value.ValueKind.GlobalVar, Type = self.Type, Label= self.Decl.LinkageObject.LinkageId };
        }

        public Value OnUndefinedIdentifierExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnArgumentExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, Value value) {
            return new Value() { Kind = Value.ValueKind.LocalVar, Type = self.Type, Offset = -arguments[self.Ident] };
        }

        public Value OnVariableExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, Value value) {
            int offset;
            if (localScope.TryGetValue(self.Ident, out offset)) {
                return new Value() { Kind = Value.ValueKind.LocalVar, Type = self.Type, Offset = offset };
            } else {
                return new Value() { Kind = Value.ValueKind.GlobalVar, Type = self.Type, Offset = 0, Label = self.Decl.LinkageObject.LinkageId };
            }
        }

        public Value OnStringExpression(SyntaxTree.Expression.PrimaryExpression.StringExpression self, Value value) {
            int no = dataBlock.Count;
            var label = $"D{no}";
            dataBlock.Add(Tuple.Create(label, self.Value.ToArray()));
            return new Value() { Kind = Value.ValueKind.GlobalVar, Type = self.Type, Offset = 0, Label = label };
        }

        public Value OnRelationalExpression(SyntaxTree.Expression.RelationalExpression self, Value value) {
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
                    Console.WriteLine("setge	%al");
                    break;
                case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessOrEqual:
                    Console.WriteLine("setle	%al");
                    break;
                default:
                    throw new NotImplementedException();
            }
            Console.WriteLine("movzbl %al, %eax");
            Console.WriteLine($"pushl %eax");
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
        }

        public Value OnShiftExpression(SyntaxTree.Expression.ShiftExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnSizeofExpression(SyntaxTree.Expression.SizeofExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnSizeofTypeExpression(SyntaxTree.Expression.SizeofTypeExpression self, Value value) {
            throw new NotImplementedException();
        }



        public Value OnTypeConversionExpression(SyntaxTree.Expression.TypeConversionExpression self, Value value) {
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
                        Console.WriteLine($"movl %eax, %eax");  // nothing to do;
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedInt || selfty.Kind == CType.BasicType.TypeKind.UnsignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax");  // nothing to do;
                    } else {
                        throw new NotImplementedException();
                    }
                } else {
                    if (selfty.Kind == CType.BasicType.TypeKind.Char || selfty.Kind == CType.BasicType.TypeKind.SignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.SignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.SignedInt || selfty.Kind == CType.BasicType.TypeKind.SignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax");  // nothing to do;
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedChar) {
                        Console.WriteLine($"movzbl %al, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedShortInt) {
                        Console.WriteLine($"movzwl %ax, %eax");
                    } else if (selfty.Kind == CType.BasicType.TypeKind.UnsignedInt || selfty.Kind == CType.BasicType.TypeKind.UnsignedLongInt) {
                        Console.WriteLine($"movl %eax, %eax");  // nothing to do;
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
                if (ret.Kind == Value.ValueKind.LocalVar) {
                    Console.WriteLine($"leal {-ret.Offset}(%ebp), %eax");
                    Console.WriteLine($"pushl %eax");
                    ret = new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
                    return ret;
                } else if (ret.Kind == Value.ValueKind.GlobalVar) {
                    Console.WriteLine($"leal {ret.Label}+{ret.Offset}, %eax");
                    Console.WriteLine($"pushl %eax");
                    ret = new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
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
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnUnaryAddressExpression(SyntaxTree.Expression.UnaryAddressExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            if (operand.Kind == Value.ValueKind.LocalVar) {
                Console.WriteLine($"leal {-operand.Offset}(%ebp), %eax");
                Console.WriteLine($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (operand.Kind == Value.ValueKind.GlobalVar) {
                Console.WriteLine($"leal {operand.Label}+{operand.Offset}, %eax");
                Console.WriteLine($"pushl %eax");
                return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
            } else if (operand.Kind == Value.ValueKind.Temp) {
                return operand;
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnUnaryMinusExpression(SyntaxTree.Expression.UnaryMinusExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            operand = Load(operand, "%eax");
            Console.WriteLine($"negl %eax");
            Console.WriteLine($"pushl %eax");
            return new Value() { Kind = Value.ValueKind.Temp, Type = self.Type };
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
            throw new NotImplementedException();
        }

        public Value OnUnaryPrefixExpression(SyntaxTree.Expression.UnaryPrefixExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnUnaryReferenceExpression(SyntaxTree.Expression.UnaryReferenceExpression self, Value value) {
            var ret= self.Expr.Accept(this, value);
            Load(ret, "%eax");
            Console.WriteLine($"pushl %eax");
            return new Value() { Kind = Value.ValueKind.Address, Type = self.Type };
        }

        public Value OnComplexInitializer(SyntaxTree.Initializer.ComplexInitializer self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnSimpleInitializer(SyntaxTree.Initializer.SimpleInitializer self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnBreakStatement(SyntaxTree.Statement.BreakStatement self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnCaseStatement(SyntaxTree.Statement.CaseStatement self, Value value) {
            throw new NotImplementedException();
        }

        Scope<int> localScope = Scope<int>.Empty;
        int localScopeTotalSize = 0;

        public Value OnCompoundStatement(SyntaxTree.Statement.CompoundStatement self, Value value) {
            localScope = localScope.Extend();
            var prevLocalScopeSize = localScopeTotalSize;

            int localScopeSize = 0;
            foreach (var x in self.Decls) {
                if (x.LinkageObject.Linkage == LinkageKind.NoLinkage) {
                    localScopeSize += (x.LinkageObject.Type.Sizeof() + 3) & ~3;
                    localScope.Add(x.Ident, localScopeTotalSize + localScopeSize);
                } else {
                    localScope.Add(x.Ident, -1);
                }
            }
            Console.WriteLine($"andl $-16, %esp");
            Console.WriteLine($"subl ${localScopeSize}, %esp");

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
            throw new NotImplementedException();
        }

        public Value OnDefaultStatement(SyntaxTree.Statement.DefaultStatement self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnDoWhileStatement(SyntaxTree.Statement.DoWhileStatement self, Value value) {
            throw new NotImplementedException();
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
            discard(stmt);

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
            }
            else {
                var junctionLabel = LAlloc();

                Console.WriteLine($"je {junctionLabel}");

                var thenRet = self.ThenStmt.Accept(this, value);
                discard(thenRet);
                Console.WriteLine($"{junctionLabel}:");
            }


            return new Value() { Kind = Value.ValueKind.Void };
        }

        private Value Load(Value value, string register) {
            var ValueType = value.Type;
            CType elementType;
            switch (value.Kind) {
                case Value.ValueKind.IntConst: {
                        string op = "";
                        if (register != "%eax") {
                            Console.WriteLine($"pushl %eax");
                        }
                        if (ValueType.IsSignedIntegerType()) {
                            switch (ValueType.Sizeof()) {
                                case 1:
                                    Console.WriteLine($"movb ${value.IntConst}, %al");
                                    Console.WriteLine($"movsbl %al, {register}");
                                    break;
                                case 2:
                                    Console.WriteLine($"movw ${value.IntConst}, %ax");
                                    Console.WriteLine($"movswl %al, {register}");
                                    break;
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
                    Console.WriteLine($"popl {register}");
                    return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = register };
                case Value.ValueKind.FloatConst:
                    throw new NotImplementedException();
                case Value.ValueKind.Register:
                    throw new NotImplementedException();
                case Value.ValueKind.LocalVar:
                case Value.ValueKind.GlobalVar:
                case Value.ValueKind.Address: {
                        string src = "";
                        switch (value.Kind) {
                            case Value.ValueKind.LocalVar:
                                src = $"{-value.Offset}(%ebp)";
                                break;
                            case Value.ValueKind.GlobalVar:
                                src = $"{value.Label}+{value.Offset}";
                                break;
                            case Value.ValueKind.Address:
                                Console.WriteLine($"popl {register}");
                                src = $"({register})";
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        string op = "";
                        if (ValueType.IsSignedIntegerType()) {
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
                        } else if (ValueType.IsPointerType() || ValueType.IsArrayType()) {
                            op = "movl";
                        } else {
                            throw new NotImplementedException();
                        }
                        Console.WriteLine($"{op} {src}, {register}");
                        return new Value() { Kind = Value.ValueKind.Register, Type = ValueType, Register = register };
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public Value OnReturnStatement(SyntaxTree.Statement.ReturnStatement self, Value value) {
            if (!self.Expr.Type.IsVoidType()) {
                value = self.Expr.Accept(this, value);
                value = Load(value, "%ecx");
                if (value.Type.IsSignedIntegerType()) {
                    switch (value.Type.Sizeof()) {
                        case 1: Console.WriteLine($"movsbl %cl, %eax"); break;
                        case 2: Console.WriteLine($"movswl %cx, %eax"); break;
                        case 4: Console.WriteLine($"movl %ecx, %eax"); break;
                        default: throw new NotImplementedException();
                    }
                } else {
                    switch (value.Type.Sizeof()) {
                        case 1: Console.WriteLine($"movzbl %cl, %eax"); break;
                        case 2: Console.WriteLine($"movxwl %cx, %eax"); break;
                        case 4: Console.WriteLine($"movl %ecx, %eax"); break;
                        default: throw new NotImplementedException();
                    }
                }
            }
            return value;
        }

        public Value OnSwitchStatement(SyntaxTree.Statement.SwitchStatement self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnWhileStatement(SyntaxTree.Statement.WhileStatement self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnTranslationUnit(SyntaxTree.TranslationUnit self, Value value) {
            foreach (var obj in self.LinkageTable) {
                obj.Value.Definition?.Accept(this, value);
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
}