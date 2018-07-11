namespace AnsiCParser.SyntaxTree {

    public interface IVisitor<out TResult, in TArg> {
        TResult OnArgumentDeclaration(Declaration.ArgumentDeclaration self, TArg value);
        TResult OnFunctionDeclaration(Declaration.FunctionDeclaration self, TArg value);
        TResult OnTypeDeclaration(Declaration.TypeDeclaration self, TArg value);
        TResult OnVariableDeclaration(Declaration.VariableDeclaration self, TArg value);
        TResult OnAdditiveExpression(Expression.AdditiveExpression self, TArg value);
        TResult OnAndExpression(Expression.BitExpression.AndExpression self, TArg value);
        TResult OnCompoundAssignmentExpression(Expression.AssignmentExpression.CompoundAssignmentExpression self, TArg value);
        TResult OnSimpleAssignmentExpression(Expression.AssignmentExpression.SimpleAssignmentExpression self, TArg value);
        TResult OnCastExpression(Expression.CastExpression self, TArg value);
        TResult OnCommaExpression(Expression.CommaExpression self, TArg value);
        TResult OnConditionalExpression(Expression.ConditionalExpression self, TArg value);
        TResult OnEqualityExpression(Expression.EqualityExpression self, TArg value);
        TResult OnExclusiveOrExpression(Expression.BitExpression.ExclusiveOrExpression self, TArg value);
        TResult OnGccStatementExpression(Expression.GccStatementExpression self, TArg value);
        TResult OnInclusiveOrExpression(Expression.BitExpression.InclusiveOrExpression self, TArg value);
        TResult OnIntegerPromotionExpression(Expression.IntegerPromotionExpression self, TArg value);
        TResult OnLogicalAndExpression(Expression.LogicalAndExpression self, TArg value);
        TResult OnLogicalOrExpression(Expression.LogicalOrExpression self, TArg value);
        TResult OnMultiplicitiveExpression(Expression.MultiplicitiveExpression self, TArg value);
        TResult OnArraySubscriptingExpression(Expression.PostfixExpression.ArraySubscriptingExpression self, TArg value);
        TResult OnFunctionCallExpression(Expression.PostfixExpression.FunctionCallExpression self, TArg value);
        TResult OnMemberDirectAccess(Expression.PostfixExpression.MemberDirectAccess self, TArg value);
        TResult OnMemberIndirectAccess(Expression.PostfixExpression.MemberIndirectAccess self, TArg value);
        TResult OnUnaryPostfixExpression(Expression.PostfixExpression.UnaryPostfixExpression self, TArg value);
        TResult OnCharacterConstant(Expression.PrimaryExpression.Constant.CharacterConstant self, TArg value);
        TResult OnFloatingConstant(Expression.PrimaryExpression.Constant.FloatingConstant self, TArg value);
        TResult OnIntegerConstant(Expression.PrimaryExpression.Constant.IntegerConstant self, TArg value);
        TResult OnEnclosedInParenthesesExpression(Expression.PrimaryExpression.EnclosedInParenthesesExpression self, TArg value);
        TResult OnAddressConstantExpression(Expression.PrimaryExpression.AddressConstantExpression self, TArg value);
        TResult OnEnumerationConstant(Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, TArg value);
        TResult OnFunctionExpression(Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, TArg value);
        TResult OnUndefinedIdentifierExpression(Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, TArg value);
        TResult OnArgumentExpression(Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, TArg value);
        TResult OnVariableExpression(Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, TArg value);
        TResult OnStringExpression(Expression.PrimaryExpression.StringExpression self, TArg value);
        TResult OnRelationalExpression(Expression.RelationalExpression self, TArg value);
        TResult OnShiftExpression(Expression.ShiftExpression self, TArg value);
        TResult OnSizeofExpression(Expression.SizeofExpression self, TArg value);
        TResult OnSizeofTypeExpression(Expression.SizeofTypeExpression self, TArg value);
        TResult OnTypeConversionExpression(Expression.TypeConversionExpression self, TArg value);
        TResult OnUnaryAddressExpression(Expression.UnaryAddressExpression self, TArg value);
        TResult OnUnaryMinusExpression(Expression.UnaryMinusExpression self, TArg value);
        TResult OnUnaryNegateExpression(Expression.UnaryNegateExpression self, TArg value);
        TResult OnUnaryNotExpression(Expression.UnaryNotExpression self, TArg value);
        TResult OnUnaryPlusExpression(Expression.UnaryPlusExpression self, TArg value);
        TResult OnUnaryPrefixExpression(Expression.UnaryPrefixExpression self, TArg value);
        TResult OnUnaryReferenceExpression(Expression.UnaryReferenceExpression self, TArg value);
        TResult OnComplexInitializer(Initializer.ComplexInitializer self, TArg value);
        TResult OnSimpleInitializer(Initializer.SimpleInitializer self, TArg value);
        TResult OnSimpleAssignInitializer(Initializer.SimpleAssignInitializer self, TArg value);
        TResult OnArrayAssignInitializer(Initializer.ArrayAssignInitializer self, TArg value);
        TResult OnStructUnionAssignInitializer(Initializer.StructUnionAssignInitializer self, TArg value);
        TResult OnBreakStatement(Statement.BreakStatement self, TArg value);
        TResult OnCaseStatement(Statement.CaseStatement self, TArg value);
        TResult OnCompoundStatementC89(Statement.CompoundStatementC89 self, TArg value);
        TResult OnCompoundStatementC99(Statement.CompoundStatementC99 self, TArg value);
        TResult OnContinueStatement(Statement.ContinueStatement self, TArg value);
        TResult OnDefaultStatement(Statement.DefaultStatement self, TArg value);
        TResult OnDoWhileStatement(Statement.DoWhileStatement self, TArg value);
        TResult OnEmptyStatement(Statement.EmptyStatement self, TArg value);
        TResult OnExpressionStatement(Statement.ExpressionStatement self, TArg value);
        TResult OnForStatement(Statement.ForStatement self, TArg value);
        TResult OnGenericLabeledStatement(Statement.GenericLabeledStatement self, TArg value);
        TResult OnGotoStatement(Statement.GotoStatement self, TArg value);
        TResult OnIfStatement(Statement.IfStatement self, TArg value);
        TResult OnReturnStatement(Statement.ReturnStatement self, TArg value);
        TResult OnSwitchStatement(Statement.SwitchStatement self, TArg value);
        TResult OnWhileStatement(Statement.WhileStatement self, TArg value);
        TResult OnTranslationUnit(TranslationUnit self, TArg value);
    }
}
