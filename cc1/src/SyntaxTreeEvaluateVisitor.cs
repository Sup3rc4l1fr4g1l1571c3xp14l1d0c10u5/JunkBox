using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;

namespace AnsiCParser {
    public class SyntaxTreeEvaluateVisitor : SyntaxTreeVisitor.IVisitor<SyntaxTreeEvaluateVisitor.Value, SyntaxTreeEvaluateVisitor.Value> {

        /*
         * +----+----------------------+
         * | SP | 未使用領域           |
         * +----+----------------------+
         * | +n | ローカル変数領域     |
         * +----+----------------------+
         * | ・・・・・・・            |
         * +----+----------------------+
         * | -1 | ローカル変数領域     |
         * +----+----------------------+
         * | BP | 以前のベースポインタ |
         * +----+----------------------+
         * | +1 | 引数[n-1]            |
         * +----+----------------------+
         * | +2 | 引数[n-2]            |
         * +----+----------------------+
         * | ・・・・・・・            |
         * +----+----------------------+
         * | +n | 引数[0]              |
         * +----+----------------------+
         * 
         */

        public class Value {
            public enum ValueKind {
                IntValue,
                FloatValue,
                GlobalVar,
                LocalVar,
                BinOp,
                UnaryOp,

            }

            public ValueKind Kind;

            public CType Type;

            // IntValue
            public long IntValue;

            // FloatValue
            public double FloatValue;

            // GlobalVar
            // LocalVar
            public string Label;
            public int Offset;  // offset of Basepointer

            // BinOp
            public string BinOp;
            public Value Left;
            public Value Right;

            // UnaryOp
            public string UnaryOp;
            public Value Operand;


        }


        public Value OnArgumentDeclaration(SyntaxTree.Declaration.ArgumentDeclaration self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnFunctionDeclaration(SyntaxTree.Declaration.FunctionDeclaration self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnTypeDeclaration(SyntaxTree.Declaration.TypeDeclaration self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnVariableDeclaration(SyntaxTree.Declaration.VariableDeclaration self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnAdditiveExpression(SyntaxTree.Expression.AdditiveExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnAndExpression(SyntaxTree.Expression.AndExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnCompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnSimpleAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnCastExpression(SyntaxTree.Expression.CastExpression self, Value value) {
            throw new NotImplementedException();
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
            return new Value() { Kind = Value.ValueKind.FloatValue, Type=self.Type, FloatValue = self.Value };
        }

        public Value OnIntegerConstant(SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant self, Value value) {
            return new Value() {Kind = Value.ValueKind.IntValue, Type = self.Type, IntValue = self.Value};
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
            throw new NotImplementedException();
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

        public Value OnTypeConversionExpression(SyntaxTree.Expression.TypeConversionExpression self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnUnaryAddressExpression(SyntaxTree.Expression.UnaryAddressExpression self, Value value) {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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

        public Value OnCompoundStatement(SyntaxTree.Statement.CompoundStatement self, Value value) {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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

        public Value OnReturnStatement(SyntaxTree.Statement.ReturnStatement self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnSwitchStatement(SyntaxTree.Statement.SwitchStatement self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnWhileStatement(SyntaxTree.Statement.WhileStatement self, Value value) {
            throw new NotImplementedException();
        }

        public Value OnTranslationUnit(SyntaxTree.TranslationUnit self, Value value) {
            throw new NotImplementedException();
        }
    }
}