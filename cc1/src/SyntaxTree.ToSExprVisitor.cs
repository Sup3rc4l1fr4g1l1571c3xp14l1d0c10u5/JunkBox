using System;
using System.Linq;
using AnsiCParser.DataType;

namespace AnsiCParser.SyntaxTree {
    public class ToSExprVisitor : IVisitor<Schene.Pair, Schene.Pair> {
        private static Schene.Pair LocationRangeToCons(LocationRange lr) {
            return Schene.Util.makeCons(
                Schene.Util.makeList(
                    Schene.Util.makeStr(lr.Start.FilePath),
                    //Schene.Util.makeNum(lr.Start.Position),
                    Schene.Util.makeNum(lr.Start.Line),
                    Schene.Util.makeNum(lr.Start.Column)
                ),
                Schene.Util.makeList(
                    Schene.Util.makeStr(lr.End.FilePath),
                    //Schene.Util.makeNum(lr.End.Position),
                    Schene.Util.makeNum(lr.End.Line),
                    Schene.Util.makeNum(lr.End.Column)
                )
            );
        }


        public Schene.Pair OnAdditiveExpression(Expression.AdditiveExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym(self.Op == Expression.AdditiveExpression.OperatorKind.Add ? "add-expr" : "sub-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Schene.Pair OnAndExpression(Expression.BitExpression.AndExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("and-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Schene.Pair OnArgumentDeclaration(Declaration.ArgumentDeclaration self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("argument-declaration"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeStr(self.Ident),
            Schene.Util.makeSym(self.StorageClass.ToString().ToLower())
            );
        }

        public Schene.Pair OnArgumentExpression(Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("arg-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeStr(self.Ident)
            );
        }

        public Schene.Pair OnArrayAssignInitializer(Initializer.ArrayAssignInitializer self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("array-assign-init"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeList(self.Inits.Select(x => x.Accept(this, value)).Cast<object>().ToArray())
            );
        }

        public Schene.Pair OnArraySubscriptingExpression(Expression.PostfixExpression.ArraySubscriptingExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("array-subscript-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Target.Accept(this, value),
            self.Index.Accept(this, value)
            );
        }

        public Schene.Pair OnBreakStatement(Statement.BreakStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("break-stmt"),
            LocationRangeToCons(self.LocationRange)
            );
        }

        public Schene.Pair OnCaseStatement(Statement.CaseStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("case-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Expr.Accept(this, value),
            self.Stmt.Accept(this, value)
            );
        }

        public Schene.Pair OnCastExpression(Expression.CastExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("cast-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value)
            );
        }

        public Schene.Pair OnCharacterConstant(Expression.PrimaryExpression.Constant.CharacterConstant self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("char-const"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeStr(self.Str)
            );
        }

        public Schene.Pair OnCommaExpression(Expression.CommaExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("comma-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeList(self.Expressions.Select(x => x.Accept(this, value)).Cast<object>().ToArray())
            );
        }

        public Schene.Pair OnComplexInitializer(Initializer.ComplexInitializer self, Schene.Pair value) {
            throw new Exception("来ないはず");
        }

        public Schene.Pair OnCompoundAssignmentExpression(Expression.AssignmentExpression.CompoundAssignmentExpression self, Schene.Pair value) {
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
            return Schene.Util.makeList(
            Schene.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Schene.Pair OnCompoundStatementC89(Statement.CompoundStatementC89 self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("compound-stmt-c89"),
            LocationRangeToCons(self.LocationRange),
            Schene.Util.makeList(
                self.Decls
                    .Select(x => x.Accept(this, value))
                    .Concat(self.Stmts.Select(x => x.Accept(this, value)))
                    .Cast<object>()
                    .ToArray()
            )
            );
        }
        public Schene.Pair OnCompoundStatementC99(Statement.CompoundStatementC99 self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("compound-stmt-c99"),
            LocationRangeToCons(self.LocationRange),
            Schene.Util.makeList(
                self.DeclsOrStmts
                    .Select(x => x.Accept(this, value))
                    .Cast<object>()
                    .ToArray()
            )
            );
        }

        public Schene.Pair OnConditionalExpression(Expression.ConditionalExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("cond-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.CondExpr.Accept(this, value),
            self.ThenExpr.Accept(this, value),
            self.ElseExpr.Accept(this, value)
            );
        }

        public Schene.Pair OnContinueStatement(Statement.ContinueStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("continue-stmt"),
            LocationRangeToCons(self.LocationRange)
            );
        }

        public Schene.Pair OnDefaultStatement(Statement.DefaultStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("default-stmt"),
            LocationRangeToCons(self.LocationRange)
            );
        }

        public Schene.Pair OnDoWhileStatement(Statement.DoWhileStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("do-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Stmt.Accept(this, value),
            self.Cond.Accept(this, value)
            );
        }

        public Schene.Pair OnEmptyStatement(Statement.EmptyStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("empty-stmt"),
            LocationRangeToCons(self.LocationRange)
            );
        }

        public Schene.Pair OnEnclosedInParenthesesExpression(Expression.PrimaryExpression.EnclosedInParenthesesExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("enclosed-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.ParenthesesExpression.Accept(this, value)
            );
        }

        public Schene.Pair OnAddressConstantExpression(Expression.PrimaryExpression.AddressConstantExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("address-constant"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Identifier.Accept(this, value),
            self.Offset.Accept(this, value)
            );
        }

        public Schene.Pair OnEnumerationConstant(Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("enum-const"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeStr(self.Ident), Schene.Util.makeNum(self.Info.Value)
            );
        }

        public Schene.Pair OnEqualityExpression(Expression.EqualityExpression self, Schene.Pair value) {
            var ops = "";
            switch (self.Op) {
                case Expression.EqualityExpression.OperatorKind.Equal:
                    ops = "equal-expr";
                    break;
                case Expression.EqualityExpression.OperatorKind.NotEqual:
                    ops = "notequal-expr";
                    break;
            }
            return Schene.Util.makeList(
            Schene.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Schene.Pair OnExclusiveOrExpression(Expression.BitExpression.ExclusiveOrExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("xor-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Schene.Pair OnExpressionStatement(Statement.ExpressionStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("expr-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Expr.Accept(this, value)
            );
        }

        public Schene.Pair OnFloatingConstant(Expression.PrimaryExpression.Constant.FloatingConstant self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("float-const"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeStr(self.Str),
            Schene.Util.makeFloat(self.Value)
            );
        }

        public Schene.Pair OnForStatement(Statement.ForStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("for-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Init?.Accept(this, value) ?? Schene.Util.Nil,
            self.Cond?.Accept(this, value) ?? Schene.Util.Nil,
            self.Update?.Accept(this, value) ?? Schene.Util.Nil,
            self.Stmt?.Accept(this, value) ?? Schene.Util.Nil
            );
        }

        public Schene.Pair OnFunctionCallExpression(Expression.PostfixExpression.FunctionCallExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("call-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value),
            Schene.Util.makeList(self.Args.Select(x => x.Accept(this, value)).Cast<object>().ToArray())
            );
        }

        public Schene.Pair OnFunctionDeclaration(Declaration.FunctionDeclaration self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("func-decl"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeStr(self.Ident),
            Schene.Util.makeSym(self.StorageClass.ToString().ToLower()),
            Schene.Util.makeSym(self.FunctionSpecifier.ToString().ToLower()),
            Schene.Util.makeStr($"{self.LinkageObject.LinkageId}#{self.LinkageObject.Id}"),
            self.Body?.Accept(this, value) ?? Schene.Util.Nil
            );
        }

        public Schene.Pair OnFunctionExpression(Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("func-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeStr(self.Ident)
            );
        }

        public Schene.Pair OnGccStatementExpression(Expression.GccStatementExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("gcc-stmt-expr"),
            LocationRangeToCons(self.LocationRange)
            );
        }

        public Schene.Pair OnGenericLabeledStatement(Statement.GenericLabeledStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("label-stmt"),
            LocationRangeToCons(self.LocationRange),
            Schene.Util.makeStr(self.Ident),
            self.Stmt.Accept(this, value)
            );
        }

        public Schene.Pair OnGotoStatement(Statement.GotoStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("goto-stmt"),
            LocationRangeToCons(self.LocationRange),
            Schene.Util.makeStr(self.Label)
            );
        }

        public Schene.Pair OnIfStatement(Statement.IfStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("if-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Cond.Accept(this, value),
            self.ThenStmt?.Accept(this, value) ?? Schene.Util.Nil,
            self.ElseStmt?.Accept(this, value) ?? Schene.Util.Nil
            );
        }

        public Schene.Pair OnInclusiveOrExpression(Expression.BitExpression.InclusiveOrExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("or-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Schene.Pair OnIntegerConstant(Expression.PrimaryExpression.Constant.IntegerConstant self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("int-const"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeStr(self.Str),
            Schene.Util.makeNum(self.Value)
            );
        }

        public Schene.Pair OnIntegerPromotionExpression(Expression.IntegerPromotionExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("intpromot-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value)
            );
        }

        public Schene.Pair OnLogicalAndExpression(Expression.LogicalAndExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("logic-and-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Schene.Pair OnLogicalOrExpression(Expression.LogicalOrExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("logic-or-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Schene.Pair OnMemberDirectAccess(Expression.PostfixExpression.MemberDirectAccess self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("member-direct-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value),
            Schene.Util.makeStr(self.Ident.Raw)
            );
        }

        public Schene.Pair OnMemberIndirectAccess(Expression.PostfixExpression.MemberIndirectAccess self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("member-indirect-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value),
            Schene.Util.makeStr(self.Ident.Raw)
            );
        }

        public Schene.Pair OnMultiplicitiveExpression(Expression.MultiplicitiveExpression self, Schene.Pair value) {
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
            return Schene.Util.makeList(
            Schene.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value), self.Rhs.Accept(this, value)
            );
        }

        public Schene.Pair OnRelationalExpression(Expression.RelationalExpression self, Schene.Pair value) {
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
            return Schene.Util.makeList(
            Schene.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Schene.Pair OnReturnStatement(Statement.ReturnStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("ret-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Expr?.Accept(this, value) ?? Schene.Util.Nil
            );
        }

        public Schene.Pair OnShiftExpression(Expression.ShiftExpression self, Schene.Pair value) {
            var ops = "";
            switch (self.Op) {
                case Expression.ShiftExpression.OperatorKind.Left:
                    ops = "shl-expr";
                    break;
                case Expression.ShiftExpression.OperatorKind.Right:
                    ops = "shr-expr";
                    break;
            }
            return Schene.Util.makeList(
            Schene.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Schene.Pair OnSimpleAssignInitializer(Initializer.SimpleAssignInitializer self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("simple-assign-init"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value)
            );
        }

        public Schene.Pair OnSimpleAssignmentExpression(Expression.AssignmentExpression.SimpleAssignmentExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("assign-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Lhs.Accept(this, value),
            self.Rhs.Accept(this, value)
            );
        }

        public Schene.Pair OnSimpleInitializer(Initializer.SimpleInitializer self, Schene.Pair value) {
            throw new Exception("来ないはず");
        }

        public Schene.Pair OnSizeofExpression(Expression.SizeofExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("sizeof-expr-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.ExprOperand.Accept(this, value)
            );
        }

        public Schene.Pair OnSizeofTypeExpression(Expression.SizeofTypeExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("sizeof-type-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.TypeOperand.Accept(new DataType.ToSExprVisitor(), null)
            );
        }

        public Schene.Pair OnStringExpression(Expression.PrimaryExpression.StringExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("string-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeList(self.Strings.Select(Schene.Util.makeStr).ToArray())
            );
        }

        public Schene.Pair OnStructUnionAssignInitializer(Initializer.StructUnionAssignInitializer self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("struct-union-assign-init"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeList(self.Inits.Select(x => x.Accept(this, value)).Cast<object>().ToArray()));
        }

        public Schene.Pair OnSwitchStatement(Statement.SwitchStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("switch-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Cond.Accept(this, value),
            self.Stmt.Accept(this, value));
        }

        public Schene.Pair OnTranslationUnit(TranslationUnit self, Schene.Pair value) {
            return Schene.Util.makeCons(
                Schene.Util.makeSym("translation-unit"),
                LocationRangeToCons(self.LocationRange),
                Schene.Util.makeList(
                    Schene.Util.makeSym("linkage-table"),
                    Schene.Util.makeList(
                        self.LinkageTable.Select(x =>
                            Schene.Util.makeList(
                                Schene.Util.makeStr($"{x.LinkageId}#{x.Id}"),
                                LocationRangeToCons((x.Definition ?? x.TentativeDefinitions[0]).LocationRange),
                                Schene.Util.makeSym(x.Linkage.ToString()),
                                x.Type.Accept(new DataType.ToSExprVisitor(), value)
                            )
                        ).Cast<object>().ToArray()
                    )
                ),
                Schene.Util.makeList(self.Declarations.Select(x => x.Accept(this, value)).Cast<object>().ToArray())
            );
        }

        public Schene.Pair OnTypeConversionExpression(Expression.TypeConversionExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("type-conv"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Schene.Pair OnTypeDeclaration(Declaration.TypeDeclaration self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("type-decl"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeStr(self.Ident));
        }

        public Schene.Pair OnUnaryAddressExpression(Expression.UnaryAddressExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("addr-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Schene.Pair OnUnaryMinusExpression(Expression.UnaryMinusExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("unary-minus-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Schene.Pair OnUnaryNegateExpression(Expression.UnaryNegateExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("unary-neg-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Schene.Pair OnUnaryNotExpression(Expression.UnaryNotExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("unary-not-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Schene.Pair OnUnaryPlusExpression(Expression.UnaryPlusExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("unary-plus-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Schene.Pair OnUnaryPostfixExpression(Expression.PostfixExpression.UnaryPostfixExpression self, Schene.Pair value) {
            var ops = "";
            switch (self.Op) {
                case Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Inc:
                    ops = "post-inc-expr";
                    break;
                case Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Dec:
                    ops = "post-inc-expr";
                    break;
            }
            return Schene.Util.makeList(
            Schene.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value));
        }

        public Schene.Pair OnUnaryPrefixExpression(Expression.UnaryPrefixExpression self, Schene.Pair value) {
            var ops = "";
            switch (self.Op) {
                case Expression.UnaryPrefixExpression.OperatorKind.Inc:
                    ops = "pre-inc-expr";
                    break;
                case Expression.UnaryPrefixExpression.OperatorKind.Dec:
                    ops = "pre-inc-expr";
                    break;
            }
            return Schene.Util.makeList(
            Schene.Util.makeSym(ops),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value)
            );
        }

        public Schene.Pair OnUnaryReferenceExpression(Expression.UnaryReferenceExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("ref-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            self.Expr.Accept(this, value)
            );
        }

        public Schene.Pair OnUndefinedIdentifierExpression(Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("undef-ident-expr"),
            LocationRangeToCons(self.LocationRange),
            Schene.Util.makeStr(self.Ident)
            );
        }

        public Schene.Pair OnVariableDeclaration(Declaration.VariableDeclaration self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("var-decl"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeStr(self.Ident),
            Schene.Util.makeSym(self.StorageClass.ToString().ToLower()),
            Schene.Util.makeStr(self.LinkageObject == null ? "" : $"{self.LinkageObject.LinkageId}#{self.LinkageObject.Id}"),
            self.Init?.Accept(this, value) ?? Schene.Util.Nil
            );
        }

        public Schene.Pair OnVariableExpression(Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("var-expr"),
            LocationRangeToCons(self.LocationRange),
            self.Type.Accept(new DataType.ToSExprVisitor(), null),
            Schene.Util.makeStr(self.Ident)
            );
        }

        public Schene.Pair OnWhileStatement(Statement.WhileStatement self, Schene.Pair value) {
            return Schene.Util.makeList(
            Schene.Util.makeSym("while-stmt"),
            LocationRangeToCons(self.LocationRange),
            self.Cond.Accept(this, value),
            self.Stmt.Accept(this, value));
        }

    }
}
