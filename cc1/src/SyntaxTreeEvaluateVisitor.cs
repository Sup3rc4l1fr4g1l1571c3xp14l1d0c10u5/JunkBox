using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {
    public class SyntaxTreeEvaluateVisitor : SyntaxTreeVisitor.IVisitor<SyntaxTreeEvaluateVisitor.Value, object> {

        public class Value {
            public virtual bool IsInteger() { return false; }
            public virtual bool IsDouble() { return false; }
            public virtual bool IsPointer() { return false; }

            public class IntegerValue : Value {
                public override bool IsInteger() { return true; }
                public long Value { get; }
                public IntegerValue(long value) {
                    Value = value;
                }
            }

            public class DoubleValue : Value {
                public override bool IsDouble() { return true; }
                public double Value { get; }
                public DoubleValue(double value) {
                    Value = value;
                }
            }

            public class HeapValue : Value {
                public byte[] Block { get; }
                public HeapValue(int size) {
                    Block = new byte[size];
                }
            }

            public class PointerValue : Value {
                public override bool IsPointer() { return true; }
                public HeapValue Heap { get; }
                public long Offset { get; }
                public PointerValue(HeapValue heap, long offset) {
                    Heap = heap;
                    Offset = offset;
                }

            }
        }

        public Value OnAdditiveExpression(SyntaxTree.Expression.AdditiveExpression self, object value) {
            var lhs = self.Lhs.Accept(this, value);
            var rhs = self.Rhs.Accept(this, value);
            switch (self.Op) {
                case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add:
                    // 加算の場合，両オペランドが算術型をもつか，又は一方のオペランドがオブジェクト型へのポインタで，
                    // もう一方のオペランドの型が整数型でなければならない。（増分は 1 の加算に等しい。)
                    if (lhs.IsInteger() && rhs.IsInteger()) {
                        var ret = (lhs as Value.IntegerValue).Value + (rhs as Value.IntegerValue).Value;
                        return new Value.IntegerValue(ret);
                    } else if (lhs.IsInteger() && rhs.IsDouble()) {
                        var ret = (lhs as Value.IntegerValue).Value + (rhs as Value.DoubleValue).Value;
                        return new Value.DoubleValue(ret);
                    } else if (lhs.IsDouble() && rhs.IsInteger()) {
                        var ret = (lhs as Value.DoubleValue).Value + (rhs as Value.IntegerValue).Value;
                        return new Value.DoubleValue(ret);
                    } else if (lhs.IsInteger() && rhs.IsPointer()) {
                        var offset = (lhs as Value.IntegerValue).Value * self.Rhs.Type.Sizeof() + (rhs as Value.PointerValue).Offset;
                        return new Value.PointerValue((rhs as Value.PointerValue).Heap, offset);
                    } else if (lhs.IsPointer() && rhs.IsInteger()) {
                        var offset = (lhs as Value.PointerValue).Offset + (rhs as Value.IntegerValue).Value * self.Lhs.Type.Sizeof();
                        return new Value.PointerValue((lhs as Value.PointerValue).Heap, offset);
                    } else {
                        throw new Exception("加算の場合，両オペランドが算術型をもつか，又は一方のオペランドがオブジェクト型へのポインタで，もう一方のオペランドの型が整数型でなければならない。（増分は 1 の加算に等しい。)");
                    }
                    break;
                case SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub:
                    // 減算の場合，次のいずれかの条件を満たさなければならない。
                    // - 両オペランドが算術型をもつ。 
                    // - 両オペランドが適合するオブジェクト型の修飾版又は非修飾版へのポインタである。
                    // - 左オペランドがオブジェクト型へのポインタで，右オペランドの型が整数型である。（減分は 1 の減算に等しい。
                    if (lhs.IsInteger() && rhs.IsInteger()) {
                        var ret = (lhs as Value.IntegerValue).Value - (rhs as Value.IntegerValue).Value;
                        return new Value.IntegerValue(ret);
                    } else if (lhs.IsInteger() && rhs.IsDouble()) {
                        var ret = (lhs as Value.IntegerValue).Value - (rhs as Value.DoubleValue).Value;
                        return new Value.DoubleValue(ret);
                    } else if (lhs.IsDouble() && rhs.IsInteger()) {
                        var ret = (lhs as Value.DoubleValue).Value - (rhs as Value.IntegerValue).Value;
                        return new Value.DoubleValue(ret);
                    } else if (lhs.IsPointer() && rhs.IsPointer()) {
                        if ((lhs as Value.PointerValue).Heap != (rhs as Value.PointerValue).Heap) {
                            throw new Exception("同じ配列オブジェクトの要素か，その配列オブジェクトの最後の要素を一つ越えたところを指していなければならない");
                        }
                        var diff  = ((lhs as Value.PointerValue).Offset - (rhs as Value.PointerValue).Offset) / self.Lhs.Type.Sizeof();
                        return new Value.IntegerValue(diff);
                    } else if (lhs.IsPointer() && rhs.IsInteger()) {
                        var offset = (lhs as Value.PointerValue).Offset - (rhs as Value.IntegerValue).Value * self.Rhs.Type.Sizeof();
                        return new Value.PointerValue((lhs as Value.PointerValue).Heap, offset);
                    } else {
                        throw new Exception(@"減算の場合，次のいずれかの条件を満たさなければならない。
- 両オペランドが算術型をもつ。 
- 両オペランドが適合するオブジェクト型の修飾版又は非修飾版へのポインタである。
- 左オペランドがオブジェクト型へのポインタで，右オペランドの型が整数型である。（減分は 1 の減算に等しい。"
                            );
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        public Value OnAndExpression(SyntaxTree.Expression.AndExpression self, object value) {
            return null;
        }

        public Value OnArgumentDeclaration(SyntaxTree.Declaration.ArgumentDeclaration self, object value) {
            return null;
        }

        public Value OnArgumentExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, object value) {
            return null;
        }

        public Value OnArraySubscriptingExpression(SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression self, object value) {
            return null;
        }

        public Value OnBreakStatement(SyntaxTree.Statement.BreakStatement self, object value) {
            return null;
        }

        public Value OnCaseStatement(SyntaxTree.Statement.CaseStatement self, object value) {
            return null;
        }

        public Value OnCastExpression(SyntaxTree.Expression.CastExpression self, object value) {
            return null;
        }

        public Value OnCharacterConstant(SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant self, object value) {
            return null;
        }

        public Value OnCommaExpression(SyntaxTree.Expression.CommaExpression self, object value) {
            return null;
        }

        public Value OnComplexInitializer(SyntaxTree.Initializer.ComplexInitializer self, object value) {
            return null;
        }

        public Value OnCompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression self, object value) {
            var ops = "";
            switch (self.Op) {
                case "+=":
                    ops = "add-assign-expr";
                    break;
                case "-=":
                    ops = "sub-assign-expr";
                    break;
                case "*=":
                    ops = "mul-assign-expr";
                    break;
                case "/=":
                    ops = "div-assign-expr";
                    break;
                case "%=":
                    ops = "mod-assign-expr";
                    break;
                case "&=":
                    ops = "and-assign-expr";
                    break;
                case "|=":
                    ops = "or-assign-expr";
                    break;
                case "^=":
                    ops = "xor-assign-expr";
                    break;
                case "<<=":
                    ops = "shl-assign-expr";
                    break;
                case ">>=":
                    ops = "shr-assign-expr";
                    break;
            }
            return null;
        }

        public Value OnCompoundStatement(SyntaxTree.Statement.CompoundStatement self, object value) {
            return null;
        }

        public Value OnConditionalExpression(SyntaxTree.Expression.ConditionalExpression self, object value) {
            return null;
        }

        public Value OnContinueStatement(SyntaxTree.Statement.ContinueStatement self, object value) {
            return null;
        }

        public Value OnDefaultStatement(SyntaxTree.Statement.DefaultStatement self, object value) {
            return null;
        }

        public Value OnDoWhileStatement(SyntaxTree.Statement.DoWhileStatement self, object value) {
            return null;
        }

        public Value OnEmptyStatement(SyntaxTree.Statement.EmptyStatement self, object value) {
            return null;
        }

        public Value OnEnclosedInParenthesesExpression(SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression self, object value) {
            return null;
        }

        public Value OnEnumerationConstant(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, object value) {
            return null;
        }

        public Value OnEqualityExpression(SyntaxTree.Expression.EqualityExpression self, object value) {
            var ops = "";
            switch (self.Op) {
                case SyntaxTree.Expression.EqualityExpression.OperatorKind.Equal:
                    ops = "equal-expr";
                    break;
                case SyntaxTree.Expression.EqualityExpression.OperatorKind.NotEqual:
                    ops = "notequal-expr";
                    break;
            }
            return null;
        }

        public Value OnExclusiveOrExpression(SyntaxTree.Expression.ExclusiveOrExpression self, object value) {
            return null;
        }

        public Value OnExpressionStatement(SyntaxTree.Statement.ExpressionStatement self, object value) {
            return null;
        }

        public Value OnFloatingConstant(SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant self, object value) {
            return null;
        }

        public Value OnForStatement(SyntaxTree.Statement.ForStatement self, object value) {
            return null;
        }

        public Value OnFunctionCallExpression(SyntaxTree.Expression.PostfixExpression.FunctionCallExpression self, object value) {
            return null;
        }

        public Value OnFunctionDeclaration(SyntaxTree.Declaration.FunctionDeclaration self, object value) {
            return null;
        }

        public Value OnFunctionExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, object value) {
            return null;
        }

        public Value OnGccStatementExpression(SyntaxTree.Expression.GccStatementExpression self, object value) {
            return null;
        }

        public Value OnGenericLabeledStatement(SyntaxTree.Statement.GenericLabeledStatement self, object value) {
            return null;
        }

        public Value OnGotoStatement(SyntaxTree.Statement.GotoStatement self, object value) {
            return null;
        }

        public Value OnIfStatement(SyntaxTree.Statement.IfStatement self, object value) {
            return null;
        }

        public Value OnInclusiveOrExpression(SyntaxTree.Expression.InclusiveOrExpression self, object value) {
            return null;
        }

        public Value OnIntegerConstant(SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant self, object value) {
            return null;
        }

        public Value OnIntegerPromotionExpression(SyntaxTree.Expression.IntegerPromotionExpression self, object value) {
            return null;
        }

        public Value OnLogicalAndExpression(SyntaxTree.Expression.LogicalAndExpression self, object value) {
            return null;
        }

        public Value OnLogicalOrExpression(SyntaxTree.Expression.LogicalOrExpression self, object value) {
            return null;
        }

        public Value OnMemberDirectAccess(SyntaxTree.Expression.PostfixExpression.MemberDirectAccess self, object value) {
            return null;
        }

        public Value OnMemberIndirectAccess(SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess self, object value) {
            return null;
        }

        public Value OnMultiplicitiveExpression(SyntaxTree.Expression.MultiplicitiveExpression self, object value) {
            var ops = "";
            switch (self.Op) {
                case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mul:
                    ops = "mul-expr";
                    break;
                case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Div:
                    ops = "div-expr";
                    break;
                case SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mod:
                    ops = "mod-expr";
                    break;
            }
            return null;
        }

        public Value OnRelationalExpression(SyntaxTree.Expression.RelationalExpression self, object value) {
            var ops = "";
            switch (self.Op) {
                case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessThan:
                    ops = "less-expr";
                    break;
                case SyntaxTree.Expression.RelationalExpression.OperatorKind.LessOrEqual:
                    ops = "lesseq-expr";
                    break;
                case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterThan:
                    ops = "great-expr";
                    break;
                case SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterOrEqual:
                    ops = "greateq-expr";
                    break;
            }
            return null;
        }

        public Value OnReturnStatement(SyntaxTree.Statement.ReturnStatement self, object value) {
            return null;
        }

        public Value OnShiftExpression(SyntaxTree.Expression.ShiftExpression self, object value) {
            var ops = "";
            switch (self.Op) {
                case SyntaxTree.Expression.ShiftExpression.OperatorKind.Left:
                    ops = "shl-expr";
                    break;
                case SyntaxTree.Expression.ShiftExpression.OperatorKind.Right:
                    ops = "shr-expr";
                    break;
            }
            return null;
        }

        public Value OnSimpleAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, object value) {
            return null;
        }

        public Value OnSimpleInitializer(SyntaxTree.Initializer.SimpleInitializer self, object value) {
            return null;
        }

        public Value OnSizeofExpression(SyntaxTree.Expression.SizeofExpression self, object value) {
            return null;
        }

        public Value OnSizeofTypeExpression(SyntaxTree.Expression.SizeofTypeExpression self, object value) {
            return null;
        }

        public Value OnStringExpression(SyntaxTree.Expression.PrimaryExpression.StringExpression self, object value) {
            return null;
        }

        public Value OnSwitchStatement(SyntaxTree.Statement.SwitchStatement self, object value) {
            return null;
        }

        public Value OnTranslationUnit(SyntaxTree.TranslationUnit self, object value) {
            return null;
        }

        public Value OnTypeConversionExpression(SyntaxTree.Expression.TypeConversionExpression self, object value) {
            return null;
        }

        public Value OnTypeDeclaration(SyntaxTree.Declaration.TypeDeclaration self, object value) {
            return null;
        }

        public Value OnUnaryAddressExpression(SyntaxTree.Expression.UnaryAddressExpression self, object value) {
            return null;
        }

        public Value OnUnaryMinusExpression(SyntaxTree.Expression.UnaryMinusExpression self, object value) {
            return null;
        }

        public Value OnUnaryNegateExpression(SyntaxTree.Expression.UnaryNegateExpression self, object value) {
            return null;
        }

        public Value OnUnaryNotExpression(SyntaxTree.Expression.UnaryNotExpression self, object value) {
            return null;
        }

        public Value OnUnaryPlusExpression(SyntaxTree.Expression.UnaryPlusExpression self, object value) {
            return null;
        }

        public Value OnUnaryPostfixExpression(SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression self, object value) {
            var ops = "";
            switch (self.Op) {
                case SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Inc:
                    ops = "post-inc-expr";
                    break;
                case SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Dec:
                    ops = "post-inc-expr";
                    break;
            }
            return null;
        }

        public Value OnUnaryPrefixExpression(SyntaxTree.Expression.UnaryPrefixExpression self, object value) {
            var ops = "";
            switch (self.Op) {
                case SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Inc:
                    ops = "pre-inc-expr";
                    break;
                case SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Dec:
                    ops = "pre-inc-expr";
                    break;
            }
            return null;
        }

        public Value OnUnaryReferenceExpression(SyntaxTree.Expression.UnaryReferenceExpression self, object value) {
            return null;
        }

        public Value OnUndefinedIdentifierExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, object value) {
            return null;
        }

        public Value OnVariableDeclaration(SyntaxTree.Declaration.VariableDeclaration self, object value) {
            return null;
        }

        public Value OnVariableExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, object value) {
            return null;
        }

        public Value OnWhileStatement(SyntaxTree.Statement.WhileStatement self, object value) {
            return null;
        }
    }
}