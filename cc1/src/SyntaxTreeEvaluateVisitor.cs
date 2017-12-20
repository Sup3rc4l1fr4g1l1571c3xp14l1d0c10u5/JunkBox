using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;

namespace AnsiCParser {
    public class SyntaxTreeEvaluateVisitor : SyntaxTreeVisitor.IVisitor<SyntaxTreeEvaluateVisitor.Value, SyntaxTreeEvaluateVisitor.Value> {

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
         *  
         */

        /*
         * コード生成:
         *  - 式の評価結果は Value で表現する
         *  - 
         */

        public class Value {
            public enum ValueKind {
                Register,   // 式の結果はレジスタ(EAX/AX/AL)上にある。
                IntConst,   // 式の結果は整数定数値である
                FloatConst, // 式の結果は浮動小数点定数値である
                GlobalVar,  // 式の結果はグローバル変数である
                LocalVar,   // 式の結果はローカル変数である
                Address,    // 式の結果は参照(参照先アドレスはEAXに格納)である
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


        public Value OnArgumentDeclaration(SyntaxTree.Declaration.ArgumentDeclaration self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnFunctionDeclaration(SyntaxTree.Declaration.FunctionDeclaration self, Value value) {
            if (self.Body != null) {
                Console.WriteLine($".section .text");
                Console.WriteLine($".globl {self.LinkageObject.LinkageId}");
                Console.WriteLine($"{self.LinkageObject.LinkageId}:");
                Console.WriteLine($"pushl %ebp");
                Console.WriteLine($"movl  %esp, %ebp");
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
            var lhs = self.Lhs.Accept(this, value);
            var rhs = self.Rhs.Accept(this, value);

            if (lhs.Type.IsIntegerType() && rhs.Type.IsIntegerType()) {
                rhs = Load(rhs);
                Console.WriteLine($"pushl %ecx");
                if (rhs.Kind == Value.ValueKind.IntConst) {
                    Console.WriteLine($"pushl ${rhs.IntConst}");
                } else if (rhs.Kind == Value.ValueKind.Register) {
                    Console.WriteLine($"pushl {rhs.Register}");
                }
                lhs = Load(lhs);
                if (lhs.Kind == Value.ValueKind.IntConst) {
                    Console.WriteLine($"movl ${lhs.IntConst}, %eax");
                }
                Console.WriteLine($"popl %ecx");
                Console.WriteLine("addl %ecx, %eax");
                Console.WriteLine($"popl %ecx");
                return new Value() { Kind = Value.ValueKind.Register, Type = self.Type, Register = "%eax" };
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnAndExpression(SyntaxTree.Expression.AndExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnCompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnSimpleAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, Value value) {
            var rhs = self.Rhs.Accept(this, value);
            var lhs = self.Lhs.Accept(this, value);

            switch (self.Type.Sizeof()) {
                case 4:
                    if (lhs.Kind == Value.ValueKind.LocalVar) {
                        rhs = Load(rhs);
                        if (rhs.Kind == Value.ValueKind.IntConst) {
                            Console.WriteLine($"movl ${rhs.IntConst}, -{lhs.Offset}(%ebp)");
                        } else if (rhs.Kind == Value.ValueKind.Register) {
                            Console.WriteLine($"movl {rhs.Register}, -{lhs.Offset}(%ebp)");
                        } else {
                            throw new NotImplementedException();
                        }
                    } else if (lhs.Kind == Value.ValueKind.GlobalVar) {
                        rhs = Load(rhs);
                        if (rhs.Kind == Value.ValueKind.IntConst) {
                            Console.WriteLine($"movl ${rhs.IntConst}, {lhs.Label}");
                        } else if (rhs.Kind == Value.ValueKind.Register) {
                            Console.WriteLine($"movl {rhs.Register}, {lhs.Label}");
                        } else {
                            throw new NotImplementedException();
                        }
                    } else if (lhs.Kind == Value.ValueKind.Address) {
                        Console.WriteLine($"push %ecx");
                        Console.WriteLine($"movl {lhs.Register}, %ecx");
                        rhs = Load(rhs);
                        if (rhs.Kind == Value.ValueKind.IntConst) {
                            Console.WriteLine($"movl ${rhs.IntConst}, (%ecx)");
                        } else if (rhs.Kind == Value.ValueKind.Register) {
                            Console.WriteLine($"movl {rhs.Register}, (%ecx)");
                        } else {
                            throw new NotImplementedException();
                        }
                        Console.WriteLine($"pop %ecx");
                    } else {
                        throw new NotImplementedException();
                    }
                    return lhs;
                case 2:
                    if (lhs.Kind == Value.ValueKind.LocalVar) {
                        rhs = Load(rhs);
                        if (rhs.Kind == Value.ValueKind.IntConst) {
                            Console.WriteLine($"movw ${rhs.IntConst}, -{lhs.Offset}(%ebp)");
                        } else if (rhs.Kind == Value.ValueKind.Register) {
                            Console.WriteLine($"movw ${rhs.Register}, -{lhs.Offset}(%ebp)");
                        } else {
                            throw new NotImplementedException();
                        }
                    } else if (lhs.Kind == Value.ValueKind.GlobalVar) {
                        rhs = Load(rhs);
                        if (rhs.Kind == Value.ValueKind.IntConst) {
                            Console.WriteLine($"movw ${rhs.IntConst}, {lhs.Label}");
                        } else if (rhs.Kind == Value.ValueKind.Register) {
                            Console.WriteLine($"movw ${rhs.Register}, {lhs.Label}");
                        } else {
                            throw new NotImplementedException();
                        }
                    } else if (lhs.Kind == Value.ValueKind.Address) {
                        Console.WriteLine($"push %ecx");
                        Console.WriteLine($"movl {lhs.Register}, %ecx");
                        rhs = Load(rhs);
                        if (rhs.Kind == Value.ValueKind.IntConst) {
                            Console.WriteLine($"movw ${rhs.IntConst}, (%ecx)");
                        } else if (rhs.Kind == Value.ValueKind.Register) {
                            Console.WriteLine($"movw {rhs.Register}, (%ecx)");
                        } else {
                            throw new NotImplementedException();
                        }
                        Console.WriteLine($"pop %ecx");
                    } else {
                        throw new NotImplementedException();
                    }
                    return lhs;
                case 1:
                    if (lhs.Kind == Value.ValueKind.LocalVar) {
                        rhs = Load(rhs);
                        if (rhs.Kind == Value.ValueKind.IntConst) {
                            Console.WriteLine($"movb ${rhs.IntConst}, -{lhs.Offset}(%ebp)");
                        } else if (rhs.Kind == Value.ValueKind.Register) {
                            Console.WriteLine($"movb {rhs.Register}, -{lhs.Offset}(%ebp)");
                        } else {
                            throw new NotImplementedException();
                        }
                    } else if (lhs.Kind == Value.ValueKind.GlobalVar) {
                        rhs = Load(rhs);
                        if (rhs.Kind == Value.ValueKind.IntConst) {
                            Console.WriteLine($"movb ${rhs.IntConst}, {lhs.Label}");
                        } else if (rhs.Kind == Value.ValueKind.Register) {
                            Console.WriteLine($"movb {rhs.Register}, {lhs.Label}");
                        } else {
                            throw new NotImplementedException();
                        }
                    } else if (lhs.Kind == Value.ValueKind.Address) {
                        Console.WriteLine($"push %ecx");
                        Console.WriteLine($"movb {lhs.Register}, %ecx");
                        rhs = Load(rhs);
                        if (rhs.Kind == Value.ValueKind.IntConst) {
                            Console.WriteLine($"movb ${rhs.IntConst}, (%ecx)");
                        } else if (rhs.Kind == Value.ValueKind.Register) {
                            Console.WriteLine($"movb {rhs.Register}, (%ecx)");
                        } else {
                            throw new NotImplementedException();
                        }
                        Console.WriteLine($"pop %ecx");
                    } else {
                        throw new NotImplementedException();
                    }
                    return lhs;
                default:
                throw new NotImplementedException();
            }
        }

        public Value OnCastExpression(SyntaxTree.Expression.CastExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            if (ret.Type.IsIntegerType() && self.Type.IsIntegerType()) {
                var retty = ret.Type.Unwrap() as CType.BasicType;
                var selfty = self.Type.Unwrap() as CType.BasicType;

                var key = Tuple.Create(retty.Kind == CType.BasicType.TypeKind.Char ? CType.BasicType.TypeKind.SignedChar : retty.Kind, selfty.Kind == CType.BasicType.TypeKind.Char ? CType.BasicType.TypeKind.SignedChar : selfty.Kind);
                Func<Value, CType, Value> func;
                if (ConvertTable.TryGetValue(key, out func) == false) {
                    throw new NotImplementedException();
                }
                return func(Load(ret), selfty);
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public Value OnFunctionCallExpression(SyntaxTree.Expression.PostfixExpression.FunctionCallExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnMemberDirectAccess(SyntaxTree.Expression.PostfixExpression.MemberDirectAccess self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnMemberIndirectAccess(SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnUnaryPostfixExpression(SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnCharacterConstant(SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnFloatingConstant(SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant self, Value value) {
            return new Value() { Kind = Value.ValueKind.FloatConst, Type=self.Type, FloatConst = self.Value };
        }

        public Value OnIntegerConstant(SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant self, Value value) {
            return new Value() {Kind = Value.ValueKind.IntConst, Type = self.Type, IntConst = self.Value};
        }

        public Value OnEnclosedInParenthesesExpression(SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnEnumerationConstant(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnFunctionExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnUndefinedIdentifierExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnArgumentExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, Value value) {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public Value OnRelationalExpression(SyntaxTree.Expression.RelationalExpression self, Value value) {
            throw new NotImplementedException();
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

        Dictionary<Tuple<CType.BasicType.TypeKind, CType.BasicType.TypeKind>, Func<Value, CType, Value>> ConvertTable = new Dictionary<Tuple<CType.BasicType.TypeKind, CType.BasicType.TypeKind>, Func<Value, CType, Value>>() {
            {Tuple.Create(CType.BasicType.TypeKind.UnsignedChar,CType.BasicType.TypeKind.SignedChar), (value, ty) => { Console.WriteLine("movzbl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%al" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.UnsignedShortInt,CType.BasicType.TypeKind.SignedChar), (value, ty) => { Console.WriteLine("movzwl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%al" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.UnsignedInt,CType.BasicType.TypeKind.SignedChar), (value, ty) => { Console.WriteLine("movl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%al" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedChar,CType.BasicType.TypeKind.SignedChar), (value, ty) => { Console.WriteLine("movzbl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%al" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedShortInt,CType.BasicType.TypeKind.SignedChar), (value, ty) => { Console.WriteLine("movzwl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%al" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedInt,CType.BasicType.TypeKind.SignedChar), (value, ty) => { Console.WriteLine("movl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%al" }; }},

            {Tuple.Create(CType.BasicType.TypeKind.UnsignedChar,CType.BasicType.TypeKind.SignedShortInt), (value, ty) => { Console.WriteLine("movzbl {0}, %eax\r\nmovzbl %al, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%ax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.UnsignedShortInt,CType.BasicType.TypeKind.SignedShortInt), (value, ty) => { Console.WriteLine("movzwl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%ax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.UnsignedInt,CType.BasicType.TypeKind.SignedShortInt), (value, ty) => { Console.WriteLine("movl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%ax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedChar,CType.BasicType.TypeKind.SignedShortInt), (value, ty) => { Console.WriteLine("movzbl {0}, %eax\r\ncbtw", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%ax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedShortInt,CType.BasicType.TypeKind.SignedShortInt), (value, ty) => { Console.WriteLine("movzwl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%ax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedInt,CType.BasicType.TypeKind.SignedShortInt), (value, ty) => { Console.WriteLine("movl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%ax" }; }},

            {Tuple.Create(CType.BasicType.TypeKind.UnsignedChar,CType.BasicType.TypeKind.SignedInt), (value, ty) => { Console.WriteLine("movzbl {0}, %eax\r\nmovzbl %al, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%eax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.UnsignedShortInt,CType.BasicType.TypeKind.SignedInt), (value, ty) => { Console.WriteLine("movzwl {0}, %eax\r\nmovzwl %al, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%eax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.UnsignedInt,CType.BasicType.TypeKind.SignedInt), (value, ty) => { Console.WriteLine("movl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%eax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedChar,CType.BasicType.TypeKind.SignedInt), (value, ty) => { Console.WriteLine("movzbl {0}, %eax\r\nmovsbl %al, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%eax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedShortInt,CType.BasicType.TypeKind.SignedInt), (value, ty) => { Console.WriteLine("movzwl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%eax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedInt,CType.BasicType.TypeKind.SignedInt), (value, ty) => { Console.WriteLine("movl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%eax" }; }},

            {Tuple.Create(CType.BasicType.TypeKind.UnsignedChar,CType.BasicType.TypeKind.UnsignedChar), (value, ty) => { Console.WriteLine("movzbl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%al" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.UnsignedShortInt,CType.BasicType.TypeKind.UnsignedChar), (value, ty) => { Console.WriteLine("movzwl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%al" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.UnsignedInt,CType.BasicType.TypeKind.UnsignedChar), (value, ty) => { Console.WriteLine("movl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%al" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedChar,CType.BasicType.TypeKind.UnsignedChar), (value, ty) => { Console.WriteLine("movzbl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%al" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedShortInt,CType.BasicType.TypeKind.UnsignedChar), (value, ty) => { Console.WriteLine("movzwl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%al" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedInt,CType.BasicType.TypeKind.UnsignedChar), (value, ty) => { Console.WriteLine("movl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%al" }; }},

            {Tuple.Create(CType.BasicType.TypeKind.UnsignedChar,CType.BasicType.TypeKind.UnsignedShortInt), (value, ty) => { Console.WriteLine("movzbl {0}, %eax\r\nmovzbl %al, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%ax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.UnsignedShortInt,CType.BasicType.TypeKind.UnsignedShortInt), (value, ty) => { Console.WriteLine("movzwl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%ax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.UnsignedInt,CType.BasicType.TypeKind.UnsignedShortInt), (value, ty) => { Console.WriteLine("movl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%ax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedChar,CType.BasicType.TypeKind.UnsignedShortInt), (value, ty) => { Console.WriteLine("movzbl {0}, %eax\r\ncbtw", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%ax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedShortInt,CType.BasicType.TypeKind.UnsignedShortInt), (value, ty) => { Console.WriteLine("movzwl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%ax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedInt,CType.BasicType.TypeKind.UnsignedShortInt), (value, ty) => { Console.WriteLine("movl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%ax" }; }},

            {Tuple.Create(CType.BasicType.TypeKind.UnsignedChar,CType.BasicType.TypeKind.UnsignedInt), (value, ty) => { Console.WriteLine("movzbl {0}, %eax\r\nmovzbl %al, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%eax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.UnsignedShortInt,CType.BasicType.TypeKind.UnsignedInt), (value, ty) => { Console.WriteLine("movzwl {0}, %eax\r\nmovzwl %al, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%eax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.UnsignedInt,CType.BasicType.TypeKind.UnsignedInt), (value, ty) => { Console.WriteLine("movl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%eax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedChar,CType.BasicType.TypeKind.UnsignedInt), (value, ty) => { Console.WriteLine("movzbl {0}, %eax\r\nmovsbl %al, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%eax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedShortInt,CType.BasicType.TypeKind.UnsignedInt), (value, ty) => { Console.WriteLine("movzwl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%eax" }; }},
            {Tuple.Create(CType.BasicType.TypeKind.SignedInt,CType.BasicType.TypeKind.UnsignedInt), (value, ty) => { Console.WriteLine("movl {0}, %eax", (value.Kind == Value.ValueKind.IntConst ? "$"+value.IntConst.ToString() : value.Register) ); return new Value() { Kind = Value.ValueKind.Register, Type = ty, Register = "%eax" }; }},

        };

        public Value OnTypeConversionExpression(SyntaxTree.Expression.TypeConversionExpression self, Value value) {
            var ret = self.Expr.Accept(this, value);
            if (ret.Type.IsIntegerType() && self.Type.IsIntegerType()) {
                var retty = ret.Type.Unwrap() as CType.BasicType;
                var selfty = self.Type.Unwrap() as CType.BasicType;

                var key = Tuple.Create(retty.Kind == CType.BasicType.TypeKind.Char ? CType.BasicType.TypeKind.SignedChar : retty.Kind, selfty.Kind == CType.BasicType.TypeKind.Char ? CType.BasicType.TypeKind.SignedChar : selfty.Kind);
                Func<Value, CType, Value> func;
                if (ConvertTable.TryGetValue(key, out func) == false) {
                    throw new NotImplementedException();
                }
                return func(Load(ret), selfty);
            } else if (ret.Type.IsPointerType() && self.Type.IsPointerType()) {
                return ret;
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnUnaryAddressExpression(SyntaxTree.Expression.UnaryAddressExpression self, Value value) {
            var operand = self.Expr.Accept(this, value);
            if (operand.Kind == Value.ValueKind.LocalVar) {
                Console.WriteLine($"leal {-operand.Offset}(%ebp), %eax");
                return new Value() { Kind = Value.ValueKind.Register, Type = self.Type, Register = "%eax" };
            } else if (operand.Kind == Value.ValueKind.GlobalVar) {
                Console.WriteLine($"leal {operand.Label}, %eax");
                return new Value() { Kind = Value.ValueKind.Register, Type = self.Type, Register = "%eax" };
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnUnaryMinusExpression(SyntaxTree.Expression.UnaryMinusExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnUnaryNegateExpression(SyntaxTree.Expression.UnaryNegateExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnUnaryNotExpression(SyntaxTree.Expression.UnaryNotExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnUnaryPlusExpression(SyntaxTree.Expression.UnaryPlusExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnUnaryPrefixExpression(SyntaxTree.Expression.UnaryPrefixExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnUnaryReferenceExpression(SyntaxTree.Expression.UnaryReferenceExpression self, Value value) {
            var operand = Load(self.Expr.Accept(this, value));
            return new Value(operand) { Kind = Value.ValueKind.Address, Type = self.Type };
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
            return self.Expr.Accept(this, value);
        }

        public Value OnForStatement(SyntaxTree.Statement.ForStatement self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnGenericLabeledStatement(SyntaxTree.Statement.GenericLabeledStatement self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnGotoStatement(SyntaxTree.Statement.GotoStatement self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnIfStatement(SyntaxTree.Statement.IfStatement self, Value value) {
            throw new NotImplementedException();
        }


        
        private Value Load(Value value) {
            if (value.Kind == Value.ValueKind.IntConst) {
                return value;
            } else if (value.Kind == Value.ValueKind.FloatConst) {
                return value;
            } else if (value.Kind == Value.ValueKind.Register) {
                return value;
            } else if (value.Kind == Value.ValueKind.LocalVar) {
                if (value.Type.Sizeof() == 4) {
                    Console.WriteLine($"movl {-value.Offset}(%ebp), %eax");
                    return new Value() { Kind = Value.ValueKind.Register, Type = value.Type, Register = "%eax" };
                } else if (value.Type.Sizeof() == 2) {
                    Console.WriteLine($"movw {-value.Offset}(%ebp), %ax");
                    return new Value() { Kind = Value.ValueKind.Register, Type = value.Type, Register = "%ax" };
                } else if (value.Type.Sizeof() == 1) {
                    Console.WriteLine($"movb {-value.Offset}(%ebp), %al");
                    return new Value() { Kind = Value.ValueKind.Register, Type = value.Type, Register = "%al" };
                } else {
                    throw new NotImplementedException();
                }
            } else if (value.Kind == Value.ValueKind.GlobalVar) {
                if (value.Type.Sizeof() == 4) {
                    Console.WriteLine($"movl {value.Label}, %eax");
                    return new Value() { Kind = Value.ValueKind.Register, Type = value.Type, Register = "%eax" };
                } else if (value.Type.Sizeof() == 2) {
                    Console.WriteLine($"movw {value.Label}, %ax");
                    return new Value() { Kind = Value.ValueKind.Register, Type = value.Type, Register = "%ax" };
                } else if (value.Type.Sizeof() == 1) {
                    Console.WriteLine($"movb {value.Label}, %al");
                    return new Value() { Kind = Value.ValueKind.Register, Type = value.Type, Register = "%al" };
                } else {
                    throw new NotImplementedException();
                }
            } else if (value.Kind == Value.ValueKind.Address) {
                if (value.Type.Sizeof() == 4) {
                    Console.WriteLine($"movl ({value.Register}), %eax");
                    return new Value() { Kind = Value.ValueKind.Register, Type = value.Type, Register = "%eax" };
                } else if (value.Type.Sizeof() == 2) {
                    Console.WriteLine($"movw ({value.Register}), %ax");
                    return new Value() { Kind = Value.ValueKind.Register, Type = value.Type, Register = "%ax" };
                } else if (value.Type.Sizeof() == 1) {
                    Console.WriteLine($"movb ({value.Register}), %al");
                    return new Value() { Kind = Value.ValueKind.Register, Type = value.Type, Register = "%al" };
                } else {
                    throw new NotImplementedException();
                }
            } else {
                throw new NotImplementedException();
            }
        }

        public Value OnReturnStatement(SyntaxTree.Statement.ReturnStatement self, Value value) {
            if (!self.Expr.Type.IsVoidType()) {
                value = self.Expr.Accept(this, value);
                if (self.Expr.Type.Sizeof() == 4) {
                    value = Load(value);
                    if (value.Kind == Value.ValueKind.IntConst) {
                        Console.WriteLine($"movl {value.IntConst}, %eax");
                    } else if (value.Kind == Value.ValueKind.Register) {
                        Console.WriteLine($"movl {value.Register}, %eax");
                    } else {
                        throw new NotImplementedException();
                    }
                } else {
                    throw new NotImplementedException();
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
                obj.Value.Definition.Accept(this, value);
            }
            return value;
        }
    }
}