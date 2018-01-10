namespace AnsiCParser {
    public static class SyntaxTreeVisitor {
        public interface IVisitor<out TResult, in TArg> {
            TResult OnArgumentDeclaration(SyntaxTree.Declaration.ArgumentDeclaration self, TArg value);
            TResult OnFunctionDeclaration(SyntaxTree.Declaration.FunctionDeclaration self, TArg value);
            TResult OnTypeDeclaration(SyntaxTree.Declaration.TypeDeclaration self, TArg value);
            TResult OnVariableDeclaration(SyntaxTree.Declaration.VariableDeclaration self, TArg value);
            TResult OnAdditiveExpression(SyntaxTree.Expression.AdditiveExpression self, TArg value);
            TResult OnAndExpression(SyntaxTree.Expression.AndExpression self, TArg value);
            TResult OnCompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression self, TArg value);
            TResult OnSimpleAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, TArg value);
            TResult OnCastExpression(SyntaxTree.Expression.CastExpression self, TArg value);
            TResult OnCommaExpression(SyntaxTree.Expression.CommaExpression self, TArg value);
            TResult OnConditionalExpression(SyntaxTree.Expression.ConditionalExpression self, TArg value);
            TResult OnEqualityExpression(SyntaxTree.Expression.EqualityExpression self, TArg value);
            TResult OnExclusiveOrExpression(SyntaxTree.Expression.ExclusiveOrExpression self, TArg value);
            TResult OnGccStatementExpression(SyntaxTree.Expression.GccStatementExpression self, TArg value);
            TResult OnInclusiveOrExpression(SyntaxTree.Expression.InclusiveOrExpression self, TArg value);
            TResult OnIntegerPromotionExpression(SyntaxTree.Expression.IntegerPromotionExpression self, TArg value);
            TResult OnLogicalAndExpression(SyntaxTree.Expression.LogicalAndExpression self, TArg value);
            TResult OnLogicalOrExpression(SyntaxTree.Expression.LogicalOrExpression self, TArg value);
            TResult OnMultiplicitiveExpression(SyntaxTree.Expression.MultiplicitiveExpression self, TArg value);
            TResult OnArraySubscriptingExpression(SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression self, TArg value);
            TResult OnFunctionCallExpression(SyntaxTree.Expression.PostfixExpression.FunctionCallExpression self, TArg value);
            TResult OnMemberDirectAccess(SyntaxTree.Expression.PostfixExpression.MemberDirectAccess self, TArg value);
            TResult OnMemberIndirectAccess(SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess self, TArg value);
            TResult OnUnaryPostfixExpression(SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression self, TArg value);
            TResult OnCharacterConstant(SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant self, TArg value);
            TResult OnFloatingConstant(SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant self, TArg value);
            TResult OnIntegerConstant(SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant self, TArg value);
            TResult OnEnclosedInParenthesesExpression(SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression self, TArg value);
            TResult OnAddressConstantExpression(SyntaxTree.Expression.PrimaryExpression.AddressConstantExpression self, TArg value);
            TResult OnEnumerationConstant(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, TArg value);
            TResult OnFunctionExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, TArg value);
            TResult OnUndefinedIdentifierExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, TArg value);
            TResult OnArgumentExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, TArg value);
            TResult OnVariableExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, TArg value);
            TResult OnStringExpression(SyntaxTree.Expression.PrimaryExpression.StringExpression self, TArg value);
            TResult OnRelationalExpression(SyntaxTree.Expression.RelationalExpression self, TArg value);
            TResult OnShiftExpression(SyntaxTree.Expression.ShiftExpression self, TArg value);
            TResult OnSizeofExpression(SyntaxTree.Expression.SizeofExpression self, TArg value);
            TResult OnSizeofTypeExpression(SyntaxTree.Expression.SizeofTypeExpression self, TArg value);
            TResult OnTypeConversionExpression(SyntaxTree.Expression.TypeConversionExpression self, TArg value);
            TResult OnUnaryAddressExpression(SyntaxTree.Expression.UnaryAddressExpression self, TArg value);
            TResult OnUnaryMinusExpression(SyntaxTree.Expression.UnaryMinusExpression self, TArg value);
            TResult OnUnaryNegateExpression(SyntaxTree.Expression.UnaryNegateExpression self, TArg value);
            TResult OnUnaryNotExpression(SyntaxTree.Expression.UnaryNotExpression self, TArg value);
            TResult OnUnaryPlusExpression(SyntaxTree.Expression.UnaryPlusExpression self, TArg value);
            TResult OnUnaryPrefixExpression(SyntaxTree.Expression.UnaryPrefixExpression self, TArg value);
            TResult OnUnaryReferenceExpression(SyntaxTree.Expression.UnaryReferenceExpression self, TArg value);
            TResult OnComplexInitializer(SyntaxTree.Initializer.ComplexInitializer self, TArg value);
            TResult OnSimpleInitializer(SyntaxTree.Initializer.SimpleInitializer self, TArg value);
            TResult OnSimpleAssignInitializer(SyntaxTree.Initializer.SimpleAssignInitializer self, TArg value);
            TResult OnArrayAssignInitializer(SyntaxTree.Initializer.ArrayAssignInitializer self, TArg value);
            TResult OnStructUnionAssignInitializer(SyntaxTree.Initializer.StructUnionAssignInitializer self, TArg value);
            TResult OnBreakStatement(SyntaxTree.Statement.BreakStatement self, TArg value);
            TResult OnCaseStatement(SyntaxTree.Statement.CaseStatement self, TArg value);
            TResult OnCompoundStatement(SyntaxTree.Statement.CompoundStatement self, TArg value);
            TResult OnContinueStatement(SyntaxTree.Statement.ContinueStatement self, TArg value);
            TResult OnDefaultStatement(SyntaxTree.Statement.DefaultStatement self, TArg value);
            TResult OnDoWhileStatement(SyntaxTree.Statement.DoWhileStatement self, TArg value);
            TResult OnEmptyStatement(SyntaxTree.Statement.EmptyStatement self, TArg value);
            TResult OnExpressionStatement(SyntaxTree.Statement.ExpressionStatement self, TArg value);
            TResult OnForStatement(SyntaxTree.Statement.ForStatement self, TArg value);
            TResult OnGenericLabeledStatement(SyntaxTree.Statement.GenericLabeledStatement self, TArg value);
            TResult OnGotoStatement(SyntaxTree.Statement.GotoStatement self, TArg value);
            TResult OnIfStatement(SyntaxTree.Statement.IfStatement self, TArg value);
            TResult OnReturnStatement(SyntaxTree.Statement.ReturnStatement self, TArg value);
            TResult OnSwitchStatement(SyntaxTree.Statement.SwitchStatement self, TArg value);
            TResult OnWhileStatement(SyntaxTree.Statement.WhileStatement self, TArg value);
            TResult OnTranslationUnit(SyntaxTree.TranslationUnit self, TArg value);
        }

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
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.AndExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
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
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.ExclusiveOrExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnExclusiveOrExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.GccStatementExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
            return visitor.OnGccStatementExpression(self, value);
        }
        public static TResult Accept<TResult, TArg>(this SyntaxTree.Expression.InclusiveOrExpression self, IVisitor<TResult, TArg> visitor, TArg value) {
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