namespace AnsiCParser {
    public static partial class SyntaxTreeVisitor {
        private static TResult AcceptInner<TResult, TArg>(dynamic ast, IVisitor<TResult, TArg> visitor, TArg value) {
            return Accept<TResult, TArg>(ast, visitor, value);
        }

        public static TResult Accept<TResult, TArg>(this SyntaxTree syntaxTree, IVisitor<TResult, TArg> visitor, TArg value) {
            return AcceptInner(syntaxTree, visitor, value);
        }

        public static TResult Accept<TResult, TArg>(this SyntaxTree.Declaration.ArgumentDeclaration self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnArgumentDeclaration(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Declaration.FunctionDeclaration self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnFunctionDeclaration(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Declaration.TypeDeclaration self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnTypeDeclaration(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Declaration.VariableDeclaration self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnVariableDeclaration(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.AdditiveExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnAdditiveExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.BitExpression.AndExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnAndExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCompoundAssignmentExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnSimpleAssignmentExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.CastExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCastExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.CommaExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCommaExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.ConditionalExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnConditionalExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.EqualityExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnEqualityExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.BitExpression.ExclusiveOrExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnExclusiveOrExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.GccStatementExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnGccStatementExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.BitExpression.InclusiveOrExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnInclusiveOrExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.IntegerPromotionExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnIntegerPromotionExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.LogicalAndExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnLogicalAndExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.LogicalOrExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnLogicalOrExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.MultiplicitiveExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnMultiplicitiveExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnArraySubscriptingExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PostfixExpression.FunctionCallExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnFunctionCallExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PostfixExpression.MemberDirectAccess self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnMemberDirectAccess(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnMemberIndirectAccess(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryPostfixExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCharacterConstant(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnFloatingConstant(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnIntegerConstant(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnEnclosedInParenthesesExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PrimaryExpression.AddressConstantExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnAddressConstantExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnEnumerationConstant(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnFunctionExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUndefinedIdentifierExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnArgumentExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnVariableExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.PrimaryExpression.StringExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnStringExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.RelationalExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnRelationalExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.ShiftExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnShiftExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.SizeofExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnSizeofExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.SizeofTypeExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnSizeofTypeExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.TypeConversionExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnTypeConversionExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.UnaryAddressExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryAddressExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.UnaryMinusExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryMinusExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.UnaryNegateExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryNegateExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.UnaryNotExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryNotExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.UnaryPlusExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryPlusExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.UnaryPrefixExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryPrefixExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.UnaryReferenceExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnUnaryReferenceExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Initializer.ComplexInitializer self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnComplexInitializer(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Initializer.SimpleInitializer self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnSimpleInitializer(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Initializer.SimpleAssignInitializer self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnSimpleAssignInitializer(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Initializer.ArrayAssignInitializer self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnArrayAssignInitializer(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Initializer.StructUnionAssignInitializer self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnStructUnionAssignInitializer(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.BreakStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnBreakStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.CaseStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCaseStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.CompoundStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnCompoundStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.ContinueStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnContinueStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.DefaultStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnDefaultStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.DoWhileStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnDoWhileStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.EmptyStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnEmptyStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.ExpressionStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnExpressionStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.ForStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnForStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.GenericLabeledStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnGenericLabeledStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.GotoStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnGotoStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.IfStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnIfStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.ReturnStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnReturnStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.SwitchStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnSwitchStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Statement.WhileStatement self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnWhileStatement(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.TranslationUnit self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnTranslationUnit(self, value);
        }
    }
}
