using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {
    public class SyntaxTreeEvaluateVisitor : SyntaxTreeVisitor.IVisitor<object, SyntaxTreeEvaluateVisitor.Value> {
        public class Value {
        }

        /// <summary>
        /// 6.5.6 加減演算子
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object OnAdditiveExpression(SyntaxTree.Expression.AdditiveExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnAndExpression(SyntaxTree.Expression.AndExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnArgumentDeclaration(SyntaxTree.Declaration.ArgumentDeclaration self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnArgumentExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnArraySubscriptingExpression(SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnBreakStatement(SyntaxTree.Statement.BreakStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnCaseStatement(SyntaxTree.Statement.CaseStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        /// <summary>
        /// 6.5.4 キャスト演算子
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object OnCastExpression(SyntaxTree.Expression.CastExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            // todo: cast
            return self.Expr.Accept(this, value);
        }

        public object OnCharacterConstant(SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnCommaExpression(SyntaxTree.Expression.CommaExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnComplexInitializer(SyntaxTree.Initializer.ComplexInitializer self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnCompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression self, SyntaxTreeEvaluateVisitor.Value value) {
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

        public object OnCompoundStatement(SyntaxTree.Statement.CompoundStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnConditionalExpression(SyntaxTree.Expression.ConditionalExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnContinueStatement(SyntaxTree.Statement.ContinueStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnDefaultStatement(SyntaxTree.Statement.DefaultStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnDoWhileStatement(SyntaxTree.Statement.DoWhileStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnEmptyStatement(SyntaxTree.Statement.EmptyStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnEnclosedInParenthesesExpression(SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnEnumerationConstant(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnEqualityExpression(SyntaxTree.Expression.EqualityExpression self, SyntaxTreeEvaluateVisitor.Value value) {
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

        public object OnExclusiveOrExpression(SyntaxTree.Expression.ExclusiveOrExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnExpressionStatement(SyntaxTree.Statement.ExpressionStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnFloatingConstant(SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnForStatement(SyntaxTree.Statement.ForStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnFunctionCallExpression(SyntaxTree.Expression.PostfixExpression.FunctionCallExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnFunctionDeclaration(SyntaxTree.Declaration.FunctionDeclaration self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnFunctionExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnGccStatementExpression(SyntaxTree.Expression.GccStatementExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnGenericLabeledStatement(SyntaxTree.Statement.GenericLabeledStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnGotoStatement(SyntaxTree.Statement.GotoStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnIfStatement(SyntaxTree.Statement.IfStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnInclusiveOrExpression(SyntaxTree.Expression.InclusiveOrExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnIntegerConstant(SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnIntegerPromotionExpression(SyntaxTree.Expression.IntegerPromotionExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnLogicalAndExpression(SyntaxTree.Expression.LogicalAndExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnLogicalOrExpression(SyntaxTree.Expression.LogicalOrExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnMemberDirectAccess(SyntaxTree.Expression.PostfixExpression.MemberDirectAccess self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnMemberIndirectAccess(SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        /// <summary>
        /// 6.5.5 乗除演算子
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object OnMultiplicitiveExpression(SyntaxTree.Expression.MultiplicitiveExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnRelationalExpression(SyntaxTree.Expression.RelationalExpression self, SyntaxTreeEvaluateVisitor.Value value) {
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

        public object OnReturnStatement(SyntaxTree.Statement.ReturnStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnShiftExpression(SyntaxTree.Expression.ShiftExpression self, SyntaxTreeEvaluateVisitor.Value value) {
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

        public object OnSimpleAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnSimpleInitializer(SyntaxTree.Initializer.SimpleInitializer self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        /// <summary>
        /// 6.5.3.4 sizeof 演算子(式に対するsizeof)
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object OnSizeofExpression(SyntaxTree.Expression.SizeofExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        /// <summary>
        /// 6.5.3.4 sizeof 演算子(型に対するsizeof)
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object OnSizeofTypeExpression(SyntaxTree.Expression.SizeofTypeExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnStringExpression(SyntaxTree.Expression.PrimaryExpression.StringExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnSwitchStatement(SyntaxTree.Statement.SwitchStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }
        public class Code {
        }
        public class Heap {
            public byte[] block;
            public int Length {
            get {
                    return block.Length;
                }
            }
            public Heap(int v) {
                this.block = new byte[v];
            }
        }

        List<Tuple<string, Heap>> HeapBlock = new List<Tuple<string, Heap>>();
        List<Tuple<string, SyntaxTree.Declaration, List<Code>>> TextBlock = new List<Tuple<string, SyntaxTree.Declaration, List<Code>>>();
        List<Code> TextSegment = new List<Code>();


        public object OnTranslationUnit(SyntaxTree.TranslationUnit self, SyntaxTreeEvaluateVisitor.Value value) {
            foreach (var obj in self.LinkageTable) {
                if (obj.Value.Type.IsFunctionType()) {
                    TextBlock.Add(Tuple.Create(obj.Value.LinkageId, obj.Value.Definition, new List<Code>()));
                } else if (obj.Value.Type.IsObjectType()) {
                    HeapBlock.Add(Tuple.Create(obj.Value.LinkageId, new Heap(obj.Value.Type.Sizeof())));
                } else {
                    Console.WriteLine($"{obj.Value.LinkageId} = ???");
                }
            }
            foreach (var obj in self.LinkageTable) {
                if (obj.Value.Type.IsFunctionType() && !(obj.Value.Type is CType.TypedefedType)) {
                    obj.Value.Definition.Accept(this, value);
                }
            }
            Console.WriteLine("TextBlock:");
            foreach (var tb in TextBlock) {
                Console.WriteLine($"{tb.Item1} = {tb.Item2}");
            }
            Console.WriteLine("HeapBlock:");
            foreach (var hb in HeapBlock) {
                Console.WriteLine($"{hb.Item1} = {hb.Item2.Length} byte");
            }
            return null;
        }

        public object OnTypeConversionExpression(SyntaxTree.Expression.TypeConversionExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnTypeDeclaration(SyntaxTree.Declaration.TypeDeclaration self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        /// <summary>
        /// 6.5.3.2 アドレス及び間接演算子
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object OnUnaryAddressExpression(SyntaxTree.Expression.UnaryAddressExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        /// <summary>
        /// 6.5.3.3 単項算術演算子(単項マイナス)
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object OnUnaryMinusExpression(SyntaxTree.Expression.UnaryMinusExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        /// <summary>
        /// 6.5.3.3 単項算術演算子(単項ビット否定)
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object OnUnaryNegateExpression(SyntaxTree.Expression.UnaryNegateExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        /// <summary>
        /// 6.5.3.3 単項算術演算子(単項論理否定)
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object OnUnaryNotExpression(SyntaxTree.Expression.UnaryNotExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        /// <summary>
        /// 6.5.3.3 単項算術演算子(単項プラス)
        /// </summary>
        /// <param name="self"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public object OnUnaryPlusExpression(SyntaxTree.Expression.UnaryPlusExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnUnaryPostfixExpression(SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression self, SyntaxTreeEvaluateVisitor.Value value) {
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

        public object OnUnaryPrefixExpression(SyntaxTree.Expression.UnaryPrefixExpression self, SyntaxTreeEvaluateVisitor.Value value) {
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

        public object OnUnaryReferenceExpression(SyntaxTree.Expression.UnaryReferenceExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnUndefinedIdentifierExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnVariableDeclaration(SyntaxTree.Declaration.VariableDeclaration self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnVariableExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }

        public object OnWhileStatement(SyntaxTree.Statement.WhileStatement self, SyntaxTreeEvaluateVisitor.Value value) {
            return null;
        }
    }
}