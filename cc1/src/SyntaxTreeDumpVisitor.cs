using System.Linq;

namespace AnsiCParser {
    public class SyntaxTreeDumpVisitor : SyntaxTreeVisitor.IVisitor<Cell, Cell> {
        public Cell OnAdditiveExpression(SyntaxTree.Expression.AdditiveExpression self, Cell value) {
            return Cell.Create(self.Op == SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add ? "add-expr" : "sub-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnAndExpression(SyntaxTree.Expression.AndExpression self, Cell value) {
            return Cell.Create("and-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnArgumentDeclaration(SyntaxTree.Declaration.ArgumentDeclaration self, Cell value) {
            return Cell.Create("argument-declaration", self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident, self.StorageClass.ToString());
        }

        public Cell OnArgumentExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, Cell value) {
            return Cell.Create("arg-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident);
        }

        public Cell OnArraySubscriptingExpression(SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression self, Cell value) {
            return Cell.Create("array-subscript-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Target.Accept(this, value), self.Index.Accept(this, value));
        }

        public Cell OnBreakStatement(SyntaxTree.Statement.BreakStatement self, Cell value) {
            return Cell.Create("break-stmt");
        }

        public Cell OnCaseStatement(SyntaxTree.Statement.CaseStatement self, Cell value) {
            return Cell.Create("case-stmt", self.Expr.Accept(this, value), self.Stmt.Accept(this, value));
        }

        public Cell OnCastExpression(SyntaxTree.Expression.CastExpression self, Cell value) {
            return Cell.Create("cast-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnCharacterConstant(SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant self, Cell value) {
            return Cell.Create("char-const", self.Type.Accept(new CTypeDumpVisitor(), null), self.Str);
        }

        public Cell OnCommaExpression(SyntaxTree.Expression.CommaExpression self, Cell value) {
            return Cell.Create("comma-expr", self.Type.Accept(new CTypeDumpVisitor(), null), Cell.Create(self.Expressions.Select(x => x.Accept(this, value)).ToArray()));
        }

        public Cell OnComplexInitializer(SyntaxTree.Initializer.ComplexInitializer self, Cell value) {
            return Cell.Create("complex-init", Cell.Create(self.Ret.Select(x => x.Accept(this, value)).ToArray()));
        }

        public Cell OnCompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression self, Cell value) {
            string ops = "";
            switch (self.Op) {
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.ADD_ASSIGN:
                    ops = "add-assign-expr";
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.SUB_ASSIGN:
                    ops = "sub-assign-expr";
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MUL_ASSIGN:
                    ops = "mul-assign-expr";
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.DIV_ASSIGN:
                    ops = "div-assign-expr";
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MOD_ASSIGN:
                    ops = "mod-assign-expr";
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.AND_ASSIGN:
                    ops = "and-assign-expr";
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.OR_ASSIGN:
                    ops = "or-assign-expr";
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.XOR_ASSIGN:
                    ops = "xor-assign-expr";
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.LEFT_ASSIGN:
                    ops = "shl-assign-expr";
                    break;
                case SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.RIGHT_ASSIGN:
                    ops = "shr-assign-expr";
                    break;
            }
            return Cell.Create(ops, self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnCompoundStatement(SyntaxTree.Statement.CompoundStatement self, Cell value) {
            return Cell.Create("compound-stmt", Cell.Create(self.Decls.Select(x => x.Accept(this, value)).Concat(self.Stmts.Select(x => x.Accept(this, value))).ToArray()));
        }

        public Cell OnConditionalExpression(SyntaxTree.Expression.ConditionalExpression self, Cell value) {
            return Cell.Create("cond-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.CondExpr.Accept(this, value), self.ThenExpr.Accept(this, value), self.ElseExpr.Accept(this, value));
        }

        public Cell OnContinueStatement(SyntaxTree.Statement.ContinueStatement self, Cell value) {
            return Cell.Create("continue-stmt");
        }

        public Cell OnDefaultStatement(SyntaxTree.Statement.DefaultStatement self, Cell value) {
            return Cell.Create("default-stmt");
        }

        public Cell OnDoWhileStatement(SyntaxTree.Statement.DoWhileStatement self, Cell value) {
            return Cell.Create("do-stmt", self.Stmt.Accept(this, value), self.Cond.Accept(this, value));
        }

        public Cell OnEmptyStatement(SyntaxTree.Statement.EmptyStatement self, Cell value) {
            return Cell.Create("empty-stmt");
        }

        public Cell OnEnclosedInParenthesesExpression(SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression self, Cell value) {
            return Cell.Create("enclosed-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.ParenthesesExpression.Accept(this, value));
        }

        public Cell OnEnumerationConstant(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, Cell value) {
            return Cell.Create("enum-const", self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident, self.Info.Value.ToString());
        }

        public Cell OnEqualityExpression(SyntaxTree.Expression.EqualityExpression self, Cell value) {
            var ops = "";
            switch (self.Op) {
                case SyntaxTree.Expression.EqualityExpression.OperatorKind.Equal:
                    ops = "equal-expr";
                    break;
                case SyntaxTree.Expression.EqualityExpression.OperatorKind.NotEqual:
                    ops = "notequal-expr";
                    break;
            }
            return Cell.Create(ops, self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnExclusiveOrExpression(SyntaxTree.Expression.ExclusiveOrExpression self, Cell value) {
            return Cell.Create("xor-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnExpressionStatement(SyntaxTree.Statement.ExpressionStatement self, Cell value) {
            return Cell.Create("expr-stmt", self.Expr.Accept(this, value));
        }

        public Cell OnFloatingConstant(SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant self, Cell value) {
            return Cell.Create("float-const", self.Type.Accept(new CTypeDumpVisitor(), null), self.Str, self.Value.ToString());
        }

        public Cell OnForStatement(SyntaxTree.Statement.ForStatement self, Cell value) {
            return Cell.Create("for-stmt", self.Init?.Accept(this, value) ?? Cell.Nil, self.Cond?.Accept(this, value) ?? Cell.Nil, self.Update?.Accept(this, value) ?? Cell.Nil, self.Stmt?.Accept(this, value) ?? Cell.Nil);
        }

        public Cell OnFunctionCallExpression(SyntaxTree.Expression.PostfixExpression.FunctionCallExpression self, Cell value) {
            return Cell.Create("call-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value), Cell.Create(self.Args.Select(x => x.Accept(this, value)).ToArray()));
        }

        public Cell OnFunctionDeclaration(SyntaxTree.Declaration.FunctionDeclaration self, Cell value) {
            return Cell.Create("func-decl", self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident, self.StorageClass.ToString(), self.FunctionSpecifier.ToString(), self.LinkageObject.LinkageId, self.Body?.Accept(this, value) ?? Cell.Nil);
        }

        public Cell OnFunctionExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, Cell value) {
            return Cell.Create("func-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident);
        }

        public Cell OnGccStatementExpression(SyntaxTree.Expression.GccStatementExpression self, Cell value) {
            return Cell.Create("gcc-stmt-expr");
        }

        public Cell OnGenericLabeledStatement(SyntaxTree.Statement.GenericLabeledStatement self, Cell value) {
            return Cell.Create("label-stmt", self.Ident, self.Stmt.Accept(this, value));
        }

        public Cell OnGotoStatement(SyntaxTree.Statement.GotoStatement self, Cell value) {
            return Cell.Create("goto-stmt", self.Label);
        }

        public Cell OnIfStatement(SyntaxTree.Statement.IfStatement self, Cell value) {
            return Cell.Create("if-stmt", self.Cond.Accept(this, value), self.ThenStmt?.Accept(this, value) ?? Cell.Nil, self.ElseStmt?.Accept(this, value) ?? Cell.Nil);
        }

        public Cell OnInclusiveOrExpression(SyntaxTree.Expression.InclusiveOrExpression self, Cell value) {
            return Cell.Create("or-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnIntegerConstant(SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant self, Cell value) {
            return Cell.Create("int-const", self.Type.Accept(new CTypeDumpVisitor(), null), self.Str, $"\"{self.Value.ToString()}\"");
        }

        public Cell OnIntegerPromotionExpression(SyntaxTree.Expression.IntegerPromotionExpression self, Cell value) {
            return Cell.Create("intpromot-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnLogicalAndExpression(SyntaxTree.Expression.LogicalAndExpression self, Cell value) {
            return Cell.Create("logic-and-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnLogicalOrExpression(SyntaxTree.Expression.LogicalOrExpression self, Cell value) {
            return Cell.Create("logic-or-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnMemberDirectAccess(SyntaxTree.Expression.PostfixExpression.MemberDirectAccess self, Cell value) {
            return Cell.Create("member-direct-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value), self.Ident);
        }

        public Cell OnMemberIndirectAccess(SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess self, Cell value) {
            return Cell.Create("member-indirect-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value), self.Ident);
        }

        public Cell OnMultiplicitiveExpression(SyntaxTree.Expression.MultiplicitiveExpression self, Cell value) {
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
            return Cell.Create(ops, self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnRelationalExpression(SyntaxTree.Expression.RelationalExpression self, Cell value) {
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
            return Cell.Create(ops, self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnReturnStatement(SyntaxTree.Statement.ReturnStatement self, Cell value) {
            return Cell.Create("ret-stmt", self.Expr?.Accept(this, value) ?? Cell.Nil);
        }

        public Cell OnShiftExpression(SyntaxTree.Expression.ShiftExpression self, Cell value) {
            var ops = "";
            switch (self.Op) {
                case SyntaxTree.Expression.ShiftExpression.OperatorKind.Left:
                    ops = "shl-expr";
                    break;
                case SyntaxTree.Expression.ShiftExpression.OperatorKind.Right:
                    ops = "shr-expr";
                    break;
            }
            return Cell.Create(ops, self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnSimpleAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, Cell value) {
            return Cell.Create("assign-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnSimpleInitializer(SyntaxTree.Initializer.SimpleInitializer self, Cell value) {
            return Cell.Create("simple-init", self.AssignmentExpression.Accept(this, value));
        }

        public Cell OnSizeofExpression(SyntaxTree.Expression.SizeofExpression self, Cell value) {
            return Cell.Create("sizeof-expr-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.ExprOperand.Accept(this, value));
        }

        public Cell OnSizeofTypeExpression(SyntaxTree.Expression.SizeofTypeExpression self, Cell value) {
            return Cell.Create("sizeof-type-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.TypeOperand.Accept(new CTypeDumpVisitor(), null));
        }

        public Cell OnStringExpression(SyntaxTree.Expression.PrimaryExpression.StringExpression self, Cell value) {
            return Cell.Create("string-expr", self.Type.Accept(new CTypeDumpVisitor(), null), Cell.Create(self.Strings.ToArray()));
        }

        public Cell OnSwitchStatement(SyntaxTree.Statement.SwitchStatement self, Cell value) {
            return Cell.Create("switch-expr", self.Cond.Accept(this, value), self.Stmt.Accept(this, value));
        }

        public Cell OnTranslationUnit(SyntaxTree.TranslationUnit self, Cell value) {
            return Cell.Create("translation-unit",
                Cell.Create("linkage-table",
                    Cell.Create(self.LinkageTable.Select(x => Cell.Create(x.Value.LinkageId, x.Value.Linkage.ToString(), x.Value.Type.Accept(new CTypeDumpVisitor(), value))).ToArray())
                ),
                Cell.Create(self.Declarations.Select(x => x.Accept(this, value)).ToArray())
            );
        }

        public Cell OnTypeConversionExpression(SyntaxTree.Expression.TypeConversionExpression self, Cell value) {
            return Cell.Create("type-conv", self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnTypeDeclaration(SyntaxTree.Declaration.TypeDeclaration self, Cell value) {
            return Cell.Create("type-decl", self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident);
        }

        public Cell OnUnaryAddressExpression(SyntaxTree.Expression.UnaryAddressExpression self, Cell value) {
            return Cell.Create("addr-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUnaryMinusExpression(SyntaxTree.Expression.UnaryMinusExpression self, Cell value) {
            return Cell.Create("unary-minus-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUnaryNegateExpression(SyntaxTree.Expression.UnaryNegateExpression self, Cell value) {
            return Cell.Create("unary-neg-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUnaryNotExpression(SyntaxTree.Expression.UnaryNotExpression self, Cell value) {
            return Cell.Create("unary-not-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUnaryPlusExpression(SyntaxTree.Expression.UnaryPlusExpression self, Cell value) {
            return Cell.Create("unary-plus-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUnaryPostfixExpression(SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression self, Cell value) {
            var ops = "";
            switch (self.Op) {
                case SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Inc:
                    ops = "post-inc-expr";
                    break;
                case SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Dec:
                    ops = "post-inc-expr";
                    break;
            }
            return Cell.Create(ops, self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUnaryPrefixExpression(SyntaxTree.Expression.UnaryPrefixExpression self, Cell value) {
            var ops = "";
            switch (self.Op) {
                case SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Inc:
                    ops = "pre-inc-expr";
                    break;
                case SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Dec:
                    ops = "pre-inc-expr";
                    break;
            }
            return Cell.Create(ops, self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUnaryReferenceExpression(SyntaxTree.Expression.UnaryReferenceExpression self, Cell value) {
            return Cell.Create("ref-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUndefinedIdentifierExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, Cell value) {
            return Cell.Create("undef-ident-expr", self.Ident);
        }

        public Cell OnVariableDeclaration(SyntaxTree.Declaration.VariableDeclaration self, Cell value) {
            return Cell.Create("var-decl", self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident, self.StorageClass.ToString(), self.LinkageObject?.LinkageId ?? "", self.Init?.Accept(this, value) ?? Cell.Nil);
        }

        public Cell OnVariableExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, Cell value) {
            return Cell.Create("var-expr", self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident);
        }

        public Cell OnWhileStatement(SyntaxTree.Statement.WhileStatement self, Cell value) {
            return Cell.Create("while-stmt", self.Cond.Accept(this, value), self.Stmt.Accept(this, value));
        }
    }
}