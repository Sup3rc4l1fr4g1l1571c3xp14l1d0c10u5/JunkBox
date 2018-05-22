namespace AnsiCParser {
    public static partial class SyntaxTreeVisitor {
        public interface IVisitor<out TResult, in TArg> {
            TResult OnArgumentDeclaration(SyntaxTree.Declaration.ArgumentDeclaration self, TArg value);
            TResult OnFunctionDeclaration(SyntaxTree.Declaration.FunctionDeclaration self, TArg value);
            TResult OnTypeDeclaration(SyntaxTree.Declaration.TypeDeclaration self, TArg value);
            TResult OnVariableDeclaration(SyntaxTree.Declaration.VariableDeclaration self, TArg value);
            TResult OnAdditiveExpression(SyntaxTree.Expression.AdditiveExpression self, TArg value);
            TResult OnAndExpression(SyntaxTree.Expression.BitExpression.AndExpression self, TArg value);
            TResult OnCompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression self, TArg value);
            TResult OnSimpleAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, TArg value);
            TResult OnCastExpression(SyntaxTree.Expression.CastExpression self, TArg value);
            TResult OnCommaExpression(SyntaxTree.Expression.CommaExpression self, TArg value);
            TResult OnConditionalExpression(SyntaxTree.Expression.ConditionalExpression self, TArg value);
            TResult OnEqualityExpression(SyntaxTree.Expression.EqualityExpression self, TArg value);
            TResult OnExclusiveOrExpression(SyntaxTree.Expression.BitExpression.ExclusiveOrExpression self, TArg value);
            TResult OnGccStatementExpression(SyntaxTree.Expression.GccStatementExpression self, TArg value);
            TResult OnInclusiveOrExpression(SyntaxTree.Expression.BitExpression.InclusiveOrExpression self, TArg value);
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
    }
}