using System;
using System.Globalization;
using System.Linq;

namespace AnsiCParser {
    public class SyntaxTreeDumpVisitor : SyntaxTreeVisitor.IVisitor<Cell, Cell> {
        private static Cell LocationRangeToCons(LocationRange lr) {
                return Util.makeCons(
                    Util.makeList(
                        Util.makeStr(lr.Start.FilePath), 
                        Util.makeNum(lr.Start.Position), 
                        Util.makeNum(lr.Start.Line), 
                        Util.makeNum(lr.Start.Column)
                    ),
                    Util.makeList(
                        Util.makeStr(lr.End.FilePath), 
                        Util.makeNum(lr.End.Position), 
                        Util.makeNum(lr.End.Line), 
                        Util.makeNum(lr.End.Column)
                    )
                );
            }

        
        public Cell OnAdditiveExpression(SyntaxTree.Expression.AdditiveExpression self, Cell value) {
            return Util.makeCons(
                Util.makeSym(self.Op == SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add ? "add-expr" : "sub-expr"), 
                LocationRangeToCons(self.LocationRange),
                self.Type.Accept(new CTypeDumpVisitor(), null), 
                self.Lhs.Accept(this, value), 
                self.Rhs.Accept(this, value)
            );
        }

        public Cell OnAndExpression(SyntaxTree.Expression.BitExpression.AndExpression self, Cell value) {
            return Util.makeCons(
                Util.makeSym("and-expr"), 
                LocationRangeToCons(self.LocationRange), 
                self.Type.Accept(new CTypeDumpVisitor(), null), 
                self.Lhs.Accept(this, value), 
                self.Rhs.Accept(this, value)
            );
        }

        public Cell OnArgumentDeclaration(SyntaxTree.Declaration.ArgumentDeclaration self, Cell value) {
            return Util.makeCons(
                Util.makeSym("argument-declaration"), 
                LocationRangeToCons(self.LocationRange), 
                self.Type.Accept(new CTypeDumpVisitor(), null), 
                Util.makeStr(self.Ident), 
                Util.makeStr(self.StorageClass.ToString())
            );
        }

        public Cell OnArgumentExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, Cell value) {
            return Util.makeCons(
                Util.makeSym("arg-expr"), 
                LocationRangeToCons(self.LocationRange), 
                self.Type.Accept(new CTypeDumpVisitor(), null), 
                Util.makeStr(self.Ident)
            );
        }

        public Cell OnArrayAssignInitializer(SyntaxTree.Initializer.ArrayAssignInitializer self, Cell value) {
            return Util.makeCons(
                Util.makeSym("array-assign-init"), 
                LocationRangeToCons(self.LocationRange), 
                self.Type.Accept(new CTypeDumpVisitor(), null), 
                Util.makeList(self.Inits.Select(x => x.Accept(this, value)).ToArray())
            );
        }

        public Cell OnArraySubscriptingExpression(SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression self, Cell value) {
            return Util.makeCons(
                Util.makeSym("array-subscript-expr"), 
                LocationRangeToCons(self.LocationRange), 
                self.Type.Accept(new CTypeDumpVisitor(), null), 
                self.Target.Accept(this, value), 
                self.Index.Accept(this, value)
            );
        }

        public Cell OnBreakStatement(SyntaxTree.Statement.BreakStatement self, Cell value) {
            return Util.makeCons(
                Util.makeSym("break-stmt"), 
                LocationRangeToCons(self.LocationRange)
            );
        }

        public Cell OnCaseStatement(SyntaxTree.Statement.CaseStatement self, Cell value) {
            return Util.makeCons(
                Util.makeSym("case-stmt"), 
                LocationRangeToCons(self.LocationRange), 
                self.Expr.Accept(this, value), 
                self.Stmt.Accept(this, value)
            );
        }

        public Cell OnCastExpression(SyntaxTree.Expression.CastExpression self, Cell value) {
            return Util.makeCons(
                Util.makeSym("cast-expr"), 
                LocationRangeToCons(self.LocationRange), 
                self.Type.Accept(new CTypeDumpVisitor(), null), 
                self.Expr.Accept(this, value)
            );
        }

        public Cell OnCharacterConstant(SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant self, Cell value) {
            return Util.makeCons(
                Util.makeSym("char-const"), 
                LocationRangeToCons(self.LocationRange), 
                self.Type.Accept(new CTypeDumpVisitor(), null), 
                Util.makeStr(self.Str)
            );
        }

        public Cell OnCommaExpression(SyntaxTree.Expression.CommaExpression self, Cell value) {
            return Util.makeCons(
                Util.makeSym("comma-expr"), 
                LocationRangeToCons(self.LocationRange), 
                self.Type.Accept(new CTypeDumpVisitor(), null), 
                Util.makeList(self.Expressions.Select(x => x.Accept(this, value)).ToArray())
            );
        }

        public Cell OnComplexInitializer(SyntaxTree.Initializer.ComplexInitializer self, Cell value) {
            throw new Exception("来ないはず");
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
            return Util.makeCons(
                Util.makeSym(ops), 
                LocationRangeToCons(self.LocationRange), 
                self.Type.Accept(new CTypeDumpVisitor(), null), 
                self.Lhs.Accept(this, value), 
                self.Rhs.Accept(this, value)
            );
        }

        public Cell OnCompoundStatement(SyntaxTree.Statement.CompoundStatement self, Cell value) {
            return Util.makeCons(Util.makeSym("compound-stmt"), LocationRangeToCons(self.LocationRange), Cell.Create(self.Decls.Select(x => (object)x.Accept(this, value)).Concat(self.Stmts.Select(x => x.Accept(this, value))).ToArray()));
        }

        public Cell OnConditionalExpression(SyntaxTree.Expression.ConditionalExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("cond-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.CondExpr.Accept(this, value), self.ThenExpr.Accept(this, value), self.ElseExpr.Accept(this, value));
        }

        public Cell OnContinueStatement(SyntaxTree.Statement.ContinueStatement self, Cell value) {
            return Util.makeCons(Util.makeSym("continue-stmt"), self.LocationRange);
        }

        public Cell OnDefaultStatement(SyntaxTree.Statement.DefaultStatement self, Cell value) {
            return Util.makeCons(Util.makeSym("default-stmt"), self.LocationRange);
        }

        public Cell OnDoWhileStatement(SyntaxTree.Statement.DoWhileStatement self, Cell value) {
            return Util.makeCons(Util.makeSym("do-stmt"), LocationRangeToCons(self.LocationRange), self.Stmt.Accept(this, value), self.Cond.Accept(this, value));
        }

        public Cell OnEmptyStatement(SyntaxTree.Statement.EmptyStatement self, Cell value) {
            return Util.makeCons(Util.makeSym("empty-stmt"), self.LocationRange);
        }

        public Cell OnEnclosedInParenthesesExpression(SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("enclosed-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.ParenthesesExpression.Accept(this, value));
        }

        public Cell OnAddressConstantExpression(SyntaxTree.Expression.PrimaryExpression.AddressConstantExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("address-constant"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Identifier.Accept(this, value), self.Offset.Accept(this, value));
        }

        public Cell OnEnumerationConstant(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, Cell value) {
            return Util.makeCons(Util.makeSym("enum-const"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident, self.Info.Value.ToString());
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
            return Cell.Create(ops, LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnExclusiveOrExpression(SyntaxTree.Expression.BitExpression.ExclusiveOrExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("xor-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnExpressionStatement(SyntaxTree.Statement.ExpressionStatement self, Cell value) {
            return Util.makeCons(Util.makeSym("expr-stmt"), LocationRangeToCons(self.LocationRange), self.Expr.Accept(this, value));
        }

        public Cell OnFloatingConstant(SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant self, Cell value) {
            return Util.makeCons(Util.makeSym("float-const"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Str, self.Value.ToString(CultureInfo.InvariantCulture));
        }

        public Cell OnForStatement(SyntaxTree.Statement.ForStatement self, Cell value) {
            return Util.makeCons(Util.makeSym("for-stmt"), LocationRangeToCons(self.LocationRange), self.Init?.Accept(this, value) ?? Util.Nil, self.Cond?.Accept(this, value) ?? Util.Nil, self.Update?.Accept(this, value) ?? Util.Nil, self.Stmt?.Accept(this, value) ?? Util.Nil);
        }

        public Cell OnFunctionCallExpression(SyntaxTree.Expression.PostfixExpression.FunctionCallExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("call-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value), Cell.Create(self.Args.Select(x => (object)x.Accept(this, value)).ToArray()));
        }

        public Cell OnFunctionDeclaration(SyntaxTree.Declaration.FunctionDeclaration self, Cell value) {
            return Util.makeCons(Util.makeSym("func-decl"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident, self.StorageClass.ToString(), self.FunctionSpecifier.ToString(), $"{self.LinkageObject.LinkageId}#{self.LinkageObject.Id}", self.Body?.Accept(this, value) ?? Util.Nil);
        }

        public Cell OnFunctionExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("func-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident);
        }

        public Cell OnGccStatementExpression(SyntaxTree.Expression.GccStatementExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("gcc-stmt-expr"), self.LocationRange);
        }

        public Cell OnGenericLabeledStatement(SyntaxTree.Statement.GenericLabeledStatement self, Cell value) {
            return Util.makeCons(Util.makeSym("label-stmt"), LocationRangeToCons(self.LocationRange), self.Ident, self.Stmt.Accept(this, value));
        }

        public Cell OnGotoStatement(SyntaxTree.Statement.GotoStatement self, Cell value) {
            return Util.makeCons(Util.makeSym("goto-stmt"), LocationRangeToCons(self.LocationRange), self.Label);
        }

        public Cell OnIfStatement(SyntaxTree.Statement.IfStatement self, Cell value) {
            return Util.makeCons(Util.makeSym("if-stmt"), LocationRangeToCons(self.LocationRange), self.Cond.Accept(this, value), self.ThenStmt?.Accept(this, value) ?? Util.Nil, self.ElseStmt?.Accept(this, value) ?? Util.Nil);
        }

        public Cell OnInclusiveOrExpression(SyntaxTree.Expression.BitExpression.InclusiveOrExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("or-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnIntegerConstant(SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant self, Cell value) {
            return Util.makeCons(Util.makeSym("int-const"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Str, $"\"{self.Value.ToString()}\"");
        }

        public Cell OnIntegerPromotionExpression(SyntaxTree.Expression.IntegerPromotionExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("intpromot-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnLogicalAndExpression(SyntaxTree.Expression.LogicalAndExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("logic-and-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnLogicalOrExpression(SyntaxTree.Expression.LogicalOrExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("logic-or-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnMemberDirectAccess(SyntaxTree.Expression.PostfixExpression.MemberDirectAccess self, Cell value) {
            return Util.makeCons(Util.makeSym("member-direct-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value), self.Ident.Raw);
        }

        public Cell OnMemberIndirectAccess(SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess self, Cell value) {
            return Util.makeCons(Util.makeSym("member-indirect-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value), self.Ident.Raw);
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
            return Cell.Create(ops, LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
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
            return Cell.Create(ops, LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnReturnStatement(SyntaxTree.Statement.ReturnStatement self, Cell value) {
            return Util.makeCons(Util.makeSym("ret-stmt"), LocationRangeToCons(self.LocationRange), self.Expr?.Accept(this, value) ?? Util.Nil);
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
            return Cell.Create(ops, LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnSimpleAssignInitializer(SyntaxTree.Initializer.SimpleAssignInitializer self, Cell value) {
            return Util.makeCons(Util.makeSym("simple-assign-init"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnSimpleAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("assign-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Lhs.Accept(this, value), self.Rhs.Accept(this, value));
        }

        public Cell OnSimpleInitializer(SyntaxTree.Initializer.SimpleInitializer self, Cell value) {
            throw new Exception("来ないはず");
        }

        public Cell OnSizeofExpression(SyntaxTree.Expression.SizeofExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("sizeof-expr-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.ExprOperand.Accept(this, value));
        }

        public Cell OnSizeofTypeExpression(SyntaxTree.Expression.SizeofTypeExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("sizeof-type-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.TypeOperand.Accept(new CTypeDumpVisitor(), null));
        }

        public Cell OnStringExpression(SyntaxTree.Expression.PrimaryExpression.StringExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("string-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), Cell.Create(self.Strings.Cast<object>().ToArray()));
        }

        public Cell OnStructUnionAssignInitializer(SyntaxTree.Initializer.StructUnionAssignInitializer self, Cell value) {
            return Util.makeCons(Util.makeSym("struct-union-assign-init"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), Cell.Create(self.Inits.Select(x => (object)x.Accept(this, value)).ToArray()));
        }

        public Cell OnSwitchStatement(SyntaxTree.Statement.SwitchStatement self, Cell value) {
            return Util.makeCons(Util.makeSym("switch-expr"), LocationRangeToCons(self.LocationRange), self.Cond.Accept(this, value), self.Stmt.Accept(this, value));
        }

        public Cell OnTranslationUnit(SyntaxTree.TranslationUnit self, Cell value) {
            return Util.makeCons(Util.makeSym("translation-unit"), LocationRangeToCons(self.LocationRange),
                Util.makeCons(Util.makeSym("linkage-table"), 
                    Cell.Create(self.LinkageTable.Select(x => (object)Cell.Create($"{x.LinkageId}#{x.Id}", (x.Definition ?? x.TentativeDefinitions[0]).LocationRange, x.Linkage.ToString(), x.Type.Accept(new CTypeDumpVisitor(), value))).ToArray())
                ),
                Cell.Create(self.Declarations.Select(x => (object)x.Accept(this, value)).ToArray())
            );
        }

        public Cell OnTypeConversionExpression(SyntaxTree.Expression.TypeConversionExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("type-conv"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnTypeDeclaration(SyntaxTree.Declaration.TypeDeclaration self, Cell value) {
            return Util.makeCons(Util.makeSym("type-decl"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident);
        }

        public Cell OnUnaryAddressExpression(SyntaxTree.Expression.UnaryAddressExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("addr-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUnaryMinusExpression(SyntaxTree.Expression.UnaryMinusExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("unary-minus-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUnaryNegateExpression(SyntaxTree.Expression.UnaryNegateExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("unary-neg-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUnaryNotExpression(SyntaxTree.Expression.UnaryNotExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("unary-not-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUnaryPlusExpression(SyntaxTree.Expression.UnaryPlusExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("unary-plus-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
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
            return Cell.Create(ops, LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
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
            return Cell.Create(ops, LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUnaryReferenceExpression(SyntaxTree.Expression.UnaryReferenceExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("ref-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Expr.Accept(this, value));
        }

        public Cell OnUndefinedIdentifierExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("undef-ident-expr"), LocationRangeToCons(self.LocationRange), self.Ident);
        }

        public Cell OnVariableDeclaration(SyntaxTree.Declaration.VariableDeclaration self, Cell value) {
            return Util.makeCons(Util.makeSym("var-decl"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident, self.StorageClass.ToString(), self.LinkageObject == null ? "" : $"{self.LinkageObject.LinkageId}#{self.LinkageObject.Id}", self.Init?.Accept(this, value) ?? Util.Nil);
        }

        public Cell OnVariableExpression(SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, Cell value) {
            return Util.makeCons(Util.makeSym("var-expr"), LocationRangeToCons(self.LocationRange), self.Type.Accept(new CTypeDumpVisitor(), null), self.Ident);
        }

        public Cell OnWhileStatement(SyntaxTree.Statement.WhileStatement self, Cell value) {
            return Util.makeCons(Util.makeSym("while-stmt"), LocationRangeToCons(self.LocationRange), self.Cond.Accept(this, value), self.Stmt.Accept(this, value));
        }

    }
}
