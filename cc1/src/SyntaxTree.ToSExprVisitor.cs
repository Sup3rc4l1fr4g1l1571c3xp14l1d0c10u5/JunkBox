using System;
using System.Linq;
using AnsiCParser.DataType;

namespace AnsiCParser.SyntaxTree {
    public class ToSExprVisitor : IVisitor<Lisp.Pair, Lisp.Pair> {
        private static Lisp.Pair LocationRangeToCons(LocationRange lr) {
            return Lisp.Util.makeCons(
                Lisp.Util.makeList(
                    Lisp.Util.makeStr(lr.Start.FilePath),
                    Lisp.Util.makeNum(lr.Start.Position),
                    Lisp.Util.makeNum(lr.Start.Line),
                    Lisp.Util.makeNum(lr.Start.Column)
                ),
                Lisp.Util.makeList(
                    Lisp.Util.makeStr(lr.End.FilePath),
                    Lisp.Util.makeNum(lr.End.Position),
                    Lisp.Util.makeNum(lr.End.Line),
                    Lisp.Util.makeNum(lr.End.Column)
                )
            );
        }


        public Lisp.Pair OnAdditiveExpression(Expression.AdditiveExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym(self.Op == Expression.AdditiveExpression.OperatorKind.Add ? "add-expr" : "sub-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Lisp.Pair OnAndExpression(Expression.BitExpression.AndExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("and-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Lisp.Pair OnArgumentDeclaration(Declaration.ArgumentDeclaration self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("argument-declaration"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeStr(self.Ident),
            Lisp.Util.makeSym(self.StorageClass.ToString().ToLower())
            );
        }

        public Lisp.Pair OnArgumentExpression(Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("arg-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeStr(self.Ident)
            );
        }

        public Lisp.Pair OnArrayAssignInitializer(Initializer.ArrayAssignInitializer self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("array-assign-init"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeList(self.Inits.Select(x => x.Accept(this, value)).Cast<object>().ToArray())
            );
        }

        public Lisp.Pair OnArraySubscriptingExpression(Expression.PostfixExpression.ArraySubscriptingExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("array-subscript-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Target.Accept(this, value),
            self.Index.Accept(this, value)
            );
        }

        public Lisp.Pair OnBreakStatement(Statement.BreakStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("break-stmt"),
            LocationRangeToCons(self.LocationRange)
            );
        }

        public Lisp.Pair OnCaseStatement(Statement.CaseStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("case-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Expr.Accept(this, value),
            self.Stmt.Accept(this, value)
            );
        }

        public Lisp.Pair OnCastExpression(Expression.CastExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("cast-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value)
            );
        }

        public Lisp.Pair OnCharacterConstant(Expression.PrimaryExpression.Constant.CharacterConstant self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("char-const"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeStr(self.Str)
            );
        }

        public Lisp.Pair OnCommaExpression(Expression.CommaExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("comma-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeList(self.Expressions.Select(x => x.Accept(this, value)).Cast<object>().ToArray())
            );
        }

        public Lisp.Pair OnComplexInitializer(Initializer.ComplexInitializer self, Lisp.Pair value) {
            throw new Exception("来ないはず");
        }

        public Lisp.Pair OnCompoundAssignmentExpression(Expression.AssignmentExpression.CompoundAssignmentExpression self, Lisp.Pair value) {
            string ops = "";
            switch (self.Op) {
                case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.ADD_ASSIGN:
                    ops = "add-assign-expr";
                    break;
                case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.SUB_ASSIGN:
                    ops = "sub-assign-expr";
                    break;
                case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MUL_ASSIGN:
                    ops = "mul-assign-expr";
                    break;
                case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.DIV_ASSIGN:
                    ops = "div-assign-expr";
                    break;
                case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MOD_ASSIGN:
                    ops = "mod-assign-expr";
                    break;
                case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.AND_ASSIGN:
                    ops = "and-assign-expr";
                    break;
                case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.OR_ASSIGN:
                    ops = "or-assign-expr";
                    break;
                case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.XOR_ASSIGN:
                    ops = "xor-assign-expr";
                    break;
                case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.LEFT_ASSIGN:
                    ops = "shl-assign-expr";
                    break;
                case Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.RIGHT_ASSIGN:
                    ops = "shr-assign-expr";
                    break;
            }
            return Lisp.Util.makeList(
            Lisp.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Lisp.Pair OnCompoundStatement(Statement.CompoundStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("compound-stmt"),
            LocationRangeToCons(self.LocationRange),
            Lisp.Util.makeList(
                self.Decls
                    .Select(x => x.Accept(this, value))
                    .Concat(self.Stmts.Select(x => x.Accept(this, value)))
                    .Cast<object>()
                    .ToArray()
            )
            );
        }

        public Lisp.Pair OnConditionalExpression(Expression.ConditionalExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("cond-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.CondExpr.Accept(this, value),
            self.ThenExpr.Accept(this, value),
            self.ElseExpr.Accept(this, value)
            );
        }

        public Lisp.Pair OnContinueStatement(Statement.ContinueStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("continue-stmt"),
            LocationRangeToCons(self.LocationRange)
            );
        }

        public Lisp.Pair OnDefaultStatement(Statement.DefaultStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("default-stmt"),
            LocationRangeToCons(self.LocationRange)
            );
        }

        public Lisp.Pair OnDoWhileStatement(Statement.DoWhileStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("do-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Stmt.Accept(this, value),
            self.Cond.Accept(this, value)
            );
        }

        public Lisp.Pair OnEmptyStatement(Statement.EmptyStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("empty-stmt"),
            LocationRangeToCons(self.LocationRange)
            );
        }

        public Lisp.Pair OnEnclosedInParenthesesExpression(Expression.PrimaryExpression.EnclosedInParenthesesExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("enclosed-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.ParenthesesExpression.Accept(this, value)
            );
        }

        public Lisp.Pair OnAddressConstantExpression(Expression.PrimaryExpression.AddressConstantExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("address-constant"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Identifier.Accept(this, value),
            self.Offset.Accept(this, value)
            );
        }

        public Lisp.Pair OnEnumerationConstant(Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("enum-const"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeStr(self.Ident), Lisp.Util.makeNum(self.Info.Value)
            );
        }

        public Lisp.Pair OnEqualityExpression(Expression.EqualityExpression self, Lisp.Pair value) {
            var ops = "";
            switch (self.Op) {
                case Expression.EqualityExpression.OperatorKind.Equal:
                    ops = "equal-expr";
                    break;
                case Expression.EqualityExpression.OperatorKind.NotEqual:
                    ops = "notequal-expr";
                    break;
            }
            return Lisp.Util.makeList(
            Lisp.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Lisp.Pair OnExclusiveOrExpression(Expression.BitExpression.ExclusiveOrExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("xor-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Lisp.Pair OnExpressionStatement(Statement.ExpressionStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("expr-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Expr.Accept(this, value)
            );
        }

        public Lisp.Pair OnFloatingConstant(Expression.PrimaryExpression.Constant.FloatingConstant self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("float-const"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeStr(self.Str),
            Lisp.Util.makeFloat(self.Value)
            );
        }

        public Lisp.Pair OnForStatement(Statement.ForStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("for-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Init?.Accept(this, value) ?? Lisp.Util.Nil,
            self.Cond?.Accept(this, value) ?? Lisp.Util.Nil,
            self.Update?.Accept(this, value) ?? Lisp.Util.Nil,
            self.Stmt?.Accept(this, value) ?? Lisp.Util.Nil
            );
        }

        public Lisp.Pair OnFunctionCallExpression(Expression.PostfixExpression.FunctionCallExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("call-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value),
            Lisp.Util.makeList(self.Args.Select(x => x.Accept(this, value)).Cast<object>().ToArray())
            );
        }

        public Lisp.Pair OnFunctionDeclaration(Declaration.FunctionDeclaration self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("func-decl"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeStr(self.Ident),
            Lisp.Util.makeSym(self.StorageClass.ToString().ToLower()),
            Lisp.Util.makeSym(self.FunctionSpecifier.ToString().ToLower()),
            Lisp.Util.makeStr($"{self.LinkageObject.LinkageId}#{self.LinkageObject.Id}"),
            self.Body?.Accept(this, value) ?? Lisp.Util.Nil
            );
        }

        public Lisp.Pair OnFunctionExpression(Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("func-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeStr(self.Ident)
            );
        }

        public Lisp.Pair OnGccStatementExpression(Expression.GccStatementExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("gcc-stmt-expr"),
            LocationRangeToCons(self.LocationRange)
            );
        }

        public Lisp.Pair OnGenericLabeledStatement(Statement.GenericLabeledStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("label-stmt"),
            LocationRangeToCons(self.LocationRange),
            Lisp.Util.makeStr(self.Ident),
            self.Stmt.Accept(this, value)
            );
        }

        public Lisp.Pair OnGotoStatement(Statement.GotoStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("goto-stmt"),
            LocationRangeToCons(self.LocationRange),
            Lisp.Util.makeStr(self.Label)
            );
        }

        public Lisp.Pair OnIfStatement(Statement.IfStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("if-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Cond.Accept(this, value),
            self.ThenStmt?.Accept(this, value) ?? Lisp.Util.Nil,
            self.ElseStmt?.Accept(this, value) ?? Lisp.Util.Nil
            );
        }

        public Lisp.Pair OnInclusiveOrExpression(Expression.BitExpression.InclusiveOrExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("or-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Lisp.Pair OnIntegerConstant(Expression.PrimaryExpression.Constant.IntegerConstant self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("int-const"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeStr(self.Str),
            Lisp.Util.makeNum(self.Value)
            );
        }

        public Lisp.Pair OnIntegerPromotionExpression(Expression.IntegerPromotionExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("intpromot-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value)
            );
        }

        public Lisp.Pair OnLogicalAndExpression(Expression.LogicalAndExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("logic-and-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Lisp.Pair OnLogicalOrExpression(Expression.LogicalOrExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("logic-or-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Lisp.Pair OnMemberDirectAccess(Expression.PostfixExpression.MemberDirectAccess self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("member-direct-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value),
            Lisp.Util.makeStr(self.Ident.Raw)
            );
        }

        public Lisp.Pair OnMemberIndirectAccess(Expression.PostfixExpression.MemberIndirectAccess self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("member-indirect-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value),
            Lisp.Util.makeStr(self.Ident.Raw)
            );
        }

        public Lisp.Pair OnMultiplicitiveExpression(Expression.MultiplicitiveExpression self, Lisp.Pair value) {
            var ops = "";
            switch (self.Op) {
                case Expression.MultiplicitiveExpression.OperatorKind.Mul:
                    ops = "mul-expr";
                    break;
                case Expression.MultiplicitiveExpression.OperatorKind.Div:
                    ops = "div-expr";
                    break;
                case Expression.MultiplicitiveExpression.OperatorKind.Mod:
                    ops = "mod-expr";
                    break;
            }
            return Lisp.Util.makeList(
            Lisp.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value), self.Rhs.Accept(this, value)
            );
        }

        public Lisp.Pair OnRelationalExpression(Expression.RelationalExpression self, Lisp.Pair value) {
            var ops = "";
            switch (self.Op) {
                case Expression.RelationalExpression.OperatorKind.LessThan:
                    ops = "less-expr";
                    break;
                case Expression.RelationalExpression.OperatorKind.LessOrEqual:
                    ops = "lesseq-expr";
                    break;
                case Expression.RelationalExpression.OperatorKind.GreaterThan:
                    ops = "great-expr";
                    break;
                case Expression.RelationalExpression.OperatorKind.GreaterOrEqual:
                    ops = "greateq-expr";
                    break;
            }
            return Lisp.Util.makeList(
            Lisp.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Lisp.Pair OnReturnStatement(Statement.ReturnStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("ret-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Expr?.Accept(this, value) ?? Lisp.Util.Nil
            );
        }

        public Lisp.Pair OnShiftExpression(Expression.ShiftExpression self, Lisp.Pair value) {
            var ops = "";
            switch (self.Op) {
                case Expression.ShiftExpression.OperatorKind.Left:
                    ops = "shl-expr";
                    break;
                case Expression.ShiftExpression.OperatorKind.Right:
                    ops = "shr-expr";
                    break;
            }
            return Lisp.Util.makeList(
            Lisp.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Lisp.Pair OnSimpleAssignInitializer(Initializer.SimpleAssignInitializer self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("simple-assign-init"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value)
            );
        }

        public Lisp.Pair OnSimpleAssignmentExpression(Expression.AssignmentExpression.SimpleAssignmentExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("assign-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Lisp.Pair OnSimpleInitializer(Initializer.SimpleInitializer self, Lisp.Pair value) {
            throw new Exception("来ないはず");
        }

        public Lisp.Pair OnSizeofExpression(Expression.SizeofExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("sizeof-expr-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.ExprOperand.Accept(this, value)
            );
        }

        public Lisp.Pair OnSizeofTypeExpression(Expression.SizeofTypeExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("sizeof-type-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.TypeOperand.Accept(new DataType.ToSExprVisitor(), null)
            );
        }

        public Lisp.Pair OnStringExpression(Expression.PrimaryExpression.StringExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("string-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeList(self.Strings.Select(Lisp.Util.makeStr).ToArray())
            );
        }

        public Lisp.Pair OnStructUnionAssignInitializer(Initializer.StructUnionAssignInitializer self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("struct-union-assign-init"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeList(self.Inits.Select(x => x.Accept(this, value)).Cast<object>().ToArray()));
        }

        public Lisp.Pair OnSwitchStatement(Statement.SwitchStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("switch-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Cond.Accept(this, value),
            self.Stmt.Accept(this, value));
        }

        public Lisp.Pair OnTranslationUnit(TranslationUnit self, Lisp.Pair value) {
            return Lisp.Util.makeCons(
                Lisp.Util.makeSym("translation-unit"),
                LocationRangeToCons(self.LocationRange),
                Lisp.Util.makeList(
                    Lisp.Util.makeSym("linkage-table"),
                    Lisp.Util.makeList(
                        self.LinkageTable.Select(x =>
                            Lisp.Util.makeList(
                                Lisp.Util.makeStr($"{x.LinkageId}#{x.Id}"),
                                LocationRangeToCons((x.Definition ?? x.TentativeDefinitions[0]).LocationRange),
                                Lisp.Util.makeSym(x.Linkage.ToString()),
                                x.Type.Accept(new DataType.ToSExprVisitor(), value)
                            )
                        ).Cast<object>().ToArray()
                    )
                ),
                Lisp.Util.makeList(self.Declarations.Select(x => x.Accept(this, value)).Cast<object>().ToArray())
            );
        }

        public Lisp.Pair OnTypeConversionExpression(Expression.TypeConversionExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("type-conv"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Lisp.Pair OnTypeDeclaration(Declaration.TypeDeclaration self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("type-decl"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeStr(self.Ident));
        }

        public Lisp.Pair OnUnaryAddressExpression(Expression.UnaryAddressExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("addr-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Lisp.Pair OnUnaryMinusExpression(Expression.UnaryMinusExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("unary-minus-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Lisp.Pair OnUnaryNegateExpression(Expression.UnaryNegateExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("unary-neg-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Lisp.Pair OnUnaryNotExpression(Expression.UnaryNotExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("unary-not-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Lisp.Pair OnUnaryPlusExpression(Expression.UnaryPlusExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("unary-plus-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Lisp.Pair OnUnaryPostfixExpression(Expression.PostfixExpression.UnaryPostfixExpression self, Lisp.Pair value) {
            var ops = "";
            switch (self.Op) {
                case Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Inc:
                    ops = "post-inc-expr";
                    break;
                case Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Dec:
                    ops = "post-inc-expr";
                    break;
            }
            return Lisp.Util.makeList(
            Lisp.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Lisp.Pair OnUnaryPrefixExpression(Expression.UnaryPrefixExpression self, Lisp.Pair value) {
            var ops = "";
            switch (self.Op) {
                case Expression.UnaryPrefixExpression.OperatorKind.Inc:
                    ops = "pre-inc-expr";
                    break;
                case Expression.UnaryPrefixExpression.OperatorKind.Dec:
                    ops = "pre-inc-expr";
                    break;
            }
            return Lisp.Util.makeList(
            Lisp.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value)
            );
        }

        public Lisp.Pair OnUnaryReferenceExpression(Expression.UnaryReferenceExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("ref-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value)
            );
        }

        public Lisp.Pair OnUndefinedIdentifierExpression(Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("undef-ident-expr"),
            LocationRangeToCons(self.LocationRange),
            Lisp.Util.makeStr(self.Ident)
            );
        }

        public Lisp.Pair OnVariableDeclaration(Declaration.VariableDeclaration self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("var-decl"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeStr(self.Ident),
            Lisp.Util.makeSym(self.StorageClass.ToString().ToLower()),
            Lisp.Util.makeStr(self.LinkageObject == null ? "" : $"{self.LinkageObject.LinkageId}#{self.LinkageObject.Id}"),
            self.Init?.Accept(this, value) ?? Lisp.Util.Nil
            );
        }

        public Lisp.Pair OnVariableExpression(Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("var-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Lisp.Util.makeStr(self.Ident)
            );
        }

        public Lisp.Pair OnWhileStatement(Statement.WhileStatement self, Lisp.Pair value) {
            return Lisp.Util.makeList(
            Lisp.Util.makeSym("while-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Cond.Accept(this, value),
            self.Stmt.Accept(this, value));
        }

    }
}
