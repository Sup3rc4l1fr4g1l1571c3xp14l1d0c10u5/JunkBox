using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser.SyntaxTree {
    public static class VisitorExt {
        private static TResult AcceptInner<TResult, TArg>(dynamic ast, IVisitor<TResult, TArg> visitor, TArg value) {
            return Accept<TResult, TArg>(ast, visitor, value);
        }
        public static TResult Accept<TResult, TArg>(this Ast ast, IVisitor<TResult, TArg> visitor, TArg value) {
            return AcceptInner(ast, visitor, value);
        }
        public static TResult Accept<TResult, TArg>(this Declaration.ArgumentDeclaration self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnArgumentDeclaration(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Declaration.FunctionDeclaration self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnFunctionDeclaration(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Declaration.TypeDeclaration self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnTypeDeclaration(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Declaration.VariableDeclaration self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnVariableDeclaration(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.AdditiveExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnAdditiveExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.BitExpression.AndExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnAndExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.AssignmentExpression.CompoundAssignmentExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCompoundAssignmentExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.AssignmentExpression.SimpleAssignmentExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnSimpleAssignmentExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.CastExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCastExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.CommaExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCommaExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.ConditionalExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnConditionalExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.EqualityExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnEqualityExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.BitExpression.ExclusiveOrExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnExclusiveOrExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.GccStatementExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnGccStatementExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.BitExpression.InclusiveOrExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnInclusiveOrExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.IntegerPromotionExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnIntegerPromotionExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.LogicalAndExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnLogicalAndExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.LogicalOrExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnLogicalOrExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.MultiplicativeExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnMultiplicativeExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PostfixExpression.ArraySubscriptingExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnArraySubscriptingExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PostfixExpression.FunctionCallExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnFunctionCallExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PostfixExpression.MemberDirectAccess self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnMemberDirectAccess(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PostfixExpression.MemberIndirectAccess self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnMemberIndirectAccess(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PostfixExpression.UnaryPostfixExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryPostfixExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PrimaryExpression.Constant.CharacterConstant self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCharacterConstant(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PrimaryExpression.Constant.FloatingConstant self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnFloatingConstant(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PrimaryExpression.Constant.IntegerConstant self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnIntegerConstant(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PrimaryExpression.CompoundLiteralExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCompoundLiteralExpression(self, value);
        }
        //public static TResult Accept<TResult, TArg>(this Expression.PrimaryExpression.EnclosedInParenthesesExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
        //    return visitor.OnEnclosedInParenthesesExpression(self, value);
        //}
        public static TResult Accept<TResult, TArg>(this Expression.PrimaryExpression.AddressConstantExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnAddressConstantExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnEnumerationConstant(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnFunctionExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUndefinedIdentifierExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnArgumentExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnVariableExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.PrimaryExpression.StringExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnStringExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.RelationalExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnRelationalExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.ShiftExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnShiftExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.SizeofExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnSizeofExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.SizeofTypeExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnSizeofTypeExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.AlignofExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnAlignofExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.TypeConversionExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnTypeConversionExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.UnaryAddressExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryAddressExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.UnaryMinusExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryMinusExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.UnaryNegateExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryNegateExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.UnaryNotExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryNotExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.UnaryPlusExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryPlusExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.UnaryPrefixExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryPrefixExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Expression.UnaryReferenceExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryReferenceExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Initializer.ComplexInitializer self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnComplexInitializer(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Initializer.SimpleInitializer self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnSimpleInitializer(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Initializer.DesignatedInitializer self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnDesignatedInitializer(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Initializer.ConcreteInitializer self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnConcreteInitializer(self, value);
        }
        //public static TResult Accept<TResult, TArg>(this Initializer.SimpleAssignInitializer self, IVisitor<TResult, TArg> visitor, TArg value) {
        //    return visitor.OnSimpleAssignInitializer(self, value);
        //}
        //public static TResult Accept<TResult, TArg>(this Initializer.ArrayAssignInitializer self, IVisitor<TResult, TArg> visitor, TArg value) {
        //    return visitor.OnArrayAssignInitializer(self, value);
        //}
        //public static TResult Accept<TResult, TArg>(this Initializer.StructUnionAssignInitializer self, IVisitor<TResult, TArg> visitor, TArg value) {
        //    return visitor.OnStructUnionAssignInitializer(self, value);
        //}
        public static TResult Accept<TResult, TArg>(this Statement.BreakStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnBreakStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.CaseStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCaseStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.CompoundStatementC89 self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCompoundStatementC89(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.CompoundStatementC99 self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCompoundStatementC99(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.ContinueStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnContinueStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.DefaultStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnDefaultStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.DoWhileStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnDoWhileStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.EmptyStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnEmptyStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.ExpressionStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnExpressionStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.ForStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnForStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.GenericLabeledStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnGenericLabeledStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.GotoStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnGotoStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.IfStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnIfStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.ReturnStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnReturnStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.SwitchStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnSwitchStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this Statement.WhileStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnWhileStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this TranslationUnit self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnTranslationUnit(self, value);
        }
    }
}
