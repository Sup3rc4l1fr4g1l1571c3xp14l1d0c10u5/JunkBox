using System;
using System.Linq;
using AnsiCParser.DataType;
using Codeplex.Data;

namespace AnsiCParser.SyntaxTree {
    public class ToJsonVisitor : IVisitor<object, object> {
        private static object LocationToJson(Location l) {
            return new {
                FilePath = l.FilePath,
                Line = l.Line,
                Column = l.Column,
            };
        }

        private static object LocationRangeToJson(LocationRange lr) {
            return new {
                Start = LocationToJson(lr.Start),
                End = LocationToJson(lr.End)
            };
        }

        public object OnAdditiveExpression(Expression.AdditiveExpression self, object value) {
            return new {
                Class = "AdditiveExpression",
                Op = self.Op.ToString(),
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Lhs = self.Lhs.Accept(this, value),
                Rhs = self.Rhs.Accept(this, value)
            };
        }

        public object OnAndExpression(Expression.BitExpression.AndExpression self, object value) {
            return new {
                Class = "AndExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Lhs = self.Lhs.Accept(this, value),
                Rhs = self.Rhs.Accept(this, value)
            };
        }

        public object OnArgumentDeclaration(Declaration.ArgumentDeclaration self, object value) {
            return new {
                Class = "ArgumentDeclaration",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Ident = self.Ident,
                StorageClass = self.StorageClass.ToString()
            };
        }

        public object OnArgumentExpression(Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression self, object value) {
            return new {
                Class = "ArgumentExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Ident = self.Ident,
            };
        }

        public object OnArrayAssignInitializer(Initializer.ArrayAssignInitializer self, object value) {
            return new {
                Class = "ArrayAssignInitializer",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Inits = self.Inits.Select(x => x.Accept(this, value)).Cast<object>().ToArray(),
            };
        }

        public object OnArraySubscriptingExpression(Expression.PostfixExpression.ArraySubscriptingExpression self, object value) {
            return new {
                Class = "ArraySubscriptingExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Target = self.Target.Accept(this, value),
                Index = self.Index.Accept(this, value)
            };
        }

        public object OnBreakStatement(Statement.BreakStatement self, object value) {
            return new {
                Class = "BreakStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
            };
        }

        public object OnCaseStatement(Statement.CaseStatement self, object value) {
            return new {
                Class = "CaseStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Expr = self.Expr.Accept(this, value),
                Stmt = self.Stmt.Accept(this, value)
            };
        }

        public object OnCastExpression(Expression.CastExpression self, object value) {
            return new {
                Class = "CastExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr.Accept(this, value)
            };
        }

        public object OnCharacterConstant(Expression.PrimaryExpression.Constant.CharacterConstant self, object value) {
            return new {
                Class = "CharacterConstant",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Str = self.Str
            };
        }

        public object OnCommaExpression(Expression.CommaExpression self, object value) {
            return new {
                Class = "CommaExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expressions = self.Expressions.Select(x => x.Accept(this, value)).ToArray()
            };
        }

        public object OnComplexInitializer(Initializer.ComplexInitializer self, object value) {
            throw new NotImplementedException("来ないはず");
        }

        public object OnCompoundAssignmentExpression(Expression.AssignmentExpression.CompoundAssignmentExpression self, object value) {
            return new {
                Class = "CompoundAssignmentExpression",
                Op = self.Op.ToString(),
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Lhs = self.Lhs.Accept(this, value),
                Rhs = self.Rhs.Accept(this, value),
            };
        }

        public object OnCompoundStatementC89(Statement.CompoundStatementC89 self, object value) {
            return new {
                Class = "CompoundStatementC89",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Decls = self.Decls
                        .Select(x => x.Accept(this, value))
                        .Concat(self.Stmts.Select(x => x.Accept(this, value)))
                        .Cast<object>()
                        .ToArray()
            };
        }
        public object OnCompoundStatementC99(Statement.CompoundStatementC99 self, object value) {
            return new {
                Class = "CompoundStatementC99",
                LocationRange = LocationRangeToJson(self.LocationRange),
                DeclsOrStmts = self.DeclsOrStmts
                        .Select(x => x.Accept(this, value))
                        .Cast<object>()
                        .ToArray()
            };
        }

        public object OnConditionalExpression(Expression.ConditionalExpression self, object value) {
            return new {
                Class = "ConditionalExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                CondExpr = self.CondExpr.Accept(this, value),
                ThenExpr = self.ThenExpr.Accept(this, value),
                ElseExpr = self.ElseExpr.Accept(this, value),
            };
        }

        public object OnContinueStatement(Statement.ContinueStatement self, object value) {
            return new {
                Class = "ContinueStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
            };
        }

        public object OnDefaultStatement(Statement.DefaultStatement self, object value) {
            return new {
                Class = "DefaultStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
            };
        }

        public object OnDoWhileStatement(Statement.DoWhileStatement self, object value) {
            return new {
                Class = "DoWhileStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Stmt = self.Stmt.Accept(this, value),
                Cond = self.Cond.Accept(this, value),
            };
        }

        public object OnEmptyStatement(Statement.EmptyStatement self, object value) {
            return new {
                Class = "EmptyStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
            };
        }

        public object OnEnclosedInParenthesesExpression(Expression.PrimaryExpression.EnclosedInParenthesesExpression self, object value) {
            return new {
                Class = "EnclosedInParenthesesExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                ParenthesesExpression = self.ParenthesesExpression.Accept(this, value),
            };
        }

        public object OnAddressConstantExpression(Expression.PrimaryExpression.AddressConstantExpression self, object value) {
            return new {
                Class = "AddressConstantExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Identifier = self.Identifier.Accept(this, value),
                Offset = self.Offset.Accept(this, value),
            };
        }

        public object OnEnumerationConstant(Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant self, object value) {
            return new {
                Class = "EnumerationConstant",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Ident = self.Ident,
                Value = self.Info.Value,
            };
        }

        public object OnEqualityExpression(Expression.EqualityExpression self, object value) {
            return new {
                Class = "EqualityExpression",
                Op = self.Op.ToString(),
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Lhs = self.Lhs.Accept(this, value),
                Rhs = self.Rhs.Accept(this, value)
            };
        }

        public object OnExclusiveOrExpression(Expression.BitExpression.ExclusiveOrExpression self, object value) {
            return new {
                Class = "ExclusiveOrExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Lhs = self.Lhs.Accept(this, value),
                Rhs = self.Rhs.Accept(this, value)
            };
        }

        public object OnExpressionStatement(Statement.ExpressionStatement self, object value) {
            return new {
                Class = "ExpressionStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Expr = self.Expr.Accept(this, value),
            };
        }

        public object OnFloatingConstant(Expression.PrimaryExpression.Constant.FloatingConstant self, object value) {
            return new {
                Class = "FloatingConstant",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Str = self.Str,
                Value = self.Value
            };
        }

        public object OnForStatement(Statement.ForStatement self, object value) {
            return new {
                Class = "ForStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Init = self.Init?.Accept(this, value),
                Cond = self.Cond?.Accept(this, value),
                Update = self.Update?.Accept(this, value),
                Stmt = self.Stmt?.Accept(this, value),
            };
        }

        public object OnFunctionCallExpression(Expression.PostfixExpression.FunctionCallExpression self, object value) {
            return new {
                Class = "FunctionCallExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr.Accept(this, value),
                Args = self.Args.Select(x => x.Accept(this, value)).ToArray()
            };
        }

        public object OnFunctionDeclaration(Declaration.FunctionDeclaration self, object value) {
            return new {
                Class = "FunctionDeclaration",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Ident = self.Ident,
                StorageClass = self.StorageClass.ToString(),
                FunctionSpecifier = self.FunctionSpecifier.ToString(),
                LinkageObject = new {
                    LinkageId = self.LinkageObject.LinkageId,
                    Id = self.LinkageObject.Id
                },
                Body = self.Body?.Accept(this, value)
            };
        }

        public object OnFunctionExpression(Expression.PrimaryExpression.IdentifierExpression.FunctionExpression self, object value) {
            return new {
                Class = "FunctionExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Ident = self.Ident
            };
        }

        public object OnGccStatementExpression(Expression.GccStatementExpression self, object value) {
            return new {
                Class = "GccStatementExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
            };
        }

        public object OnGenericLabeledStatement(Statement.GenericLabeledStatement self, object value) {
            return new {
                Class = "GenericLabeledStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Ident = self.Ident,
                Stmt = self.Stmt.Accept(this, value)
            };
        }

        public object OnGotoStatement(Statement.GotoStatement self, object value) {
            return new {
                Class = "GotoStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Label = self.Label
            };
        }

        public object OnIfStatement(Statement.IfStatement self, object value) {
            return new {
                Class = "IfStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Cond = self.Cond.Accept(this, value),
                ThenStmt = self.ThenStmt?.Accept(this, value),
                ElseStmt = self.ElseStmt?.Accept(this, value),
            };
        }

        public object OnInclusiveOrExpression(Expression.BitExpression.InclusiveOrExpression self, object value) {
            return new {
                Class = "InclusiveOrExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Lhs = self.Lhs.Accept(this, value),
                Rhs = self.Rhs.Accept(this, value),
            };
        }

        public object OnIntegerConstant(Expression.PrimaryExpression.Constant.IntegerConstant self, object value) {
            return new {
                Class = "IntegerConstant",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Str = self.Str,
                Value = self.Value,
            };
        }

        public object OnIntegerPromotionExpression(Expression.IntegerPromotionExpression self, object value) {
            return new {
                Class = "IntegerPromotionExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr.Accept(this, value)
            };
        }

        public object OnLogicalAndExpression(Expression.LogicalAndExpression self, object value) {
            return new {
                Class = "LogicalAndExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Lhs = self.Lhs.Accept(this, value),
                Rhs = self.Rhs.Accept(this, value)
            };
        }

        public object OnLogicalOrExpression(Expression.LogicalOrExpression self, object value) {
            return new {
                Class = "LogicalOrExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Lhs = self.Lhs.Accept(this, value),
                Rhs = self.Rhs.Accept(this, value)
            };
        }

        public object OnMemberDirectAccess(Expression.PostfixExpression.MemberDirectAccess self, object value) {
            return new {
                Class = "MemberDirectAccess",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr.Accept(this, value),
                Ident = self.Ident.Raw
            };
        }

        public object OnMemberIndirectAccess(Expression.PostfixExpression.MemberIndirectAccess self, object value) {
            return new {
                Class = "MemberIndirectAccess",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr.Accept(this, value),
                Ident = self.Ident.Raw
            };
        }

        public object OnMultiplicativeExpression(Expression.MultiplicativeExpression self, object value) {
            return new {
                Class = "MultiplicativeExpression",
                Op = self.Op.ToString(),
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Lhs = self.Lhs.Accept(this, value),
                Rhs = self.Rhs.Accept(this, value),
            };
        }

        public object OnRelationalExpression(Expression.RelationalExpression self, object value) {
            return new {
                Class = "RelationalExpression",
                Op = self.Op.ToString(),
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Lhs = self.Lhs.Accept(this, value),
                Rhs = self.Rhs.Accept(this, value),
            };
        }

        public object OnReturnStatement(Statement.ReturnStatement self, object value) {
            return new {
                Class = "ReturnStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Expr = self.Expr?.Accept(this, value),
            };
        }

        public object OnShiftExpression(Expression.ShiftExpression self, object value) {
            return new {
                Class = "ShiftExpression",
                Op = self.Op.ToString(),
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Lhs = self.Lhs.Accept(this, value),
                Rhs = self.Rhs.Accept(this, value),
            };
        }

        public object OnSimpleAssignInitializer(Initializer.SimpleAssignInitializer self, object value) {
            return new {
                Class = "SimpleAssignInitializer",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr?.Accept(this, value),
            };
        }

        public object OnSimpleAssignmentExpression(Expression.AssignmentExpression.SimpleAssignmentExpression self, object value) {
            return new {
                Class = "SimpleAssignmentExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Lhs = self.Lhs.Accept(this, value),
                Rhs = self.Rhs.Accept(this, value),
            };
        }

        public object OnSimpleInitializer(Initializer.SimpleInitializer self, object value) {
            throw new NotImplementedException("来ないはず");
        }

        public object OnSizeofExpression(Expression.SizeofExpression self, object value) {
            return new {
                Class = "SizeofExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                ExprOperand = self.ExprOperand?.Accept(this, value),
            };
        }

        public object OnSizeofTypeExpression(Expression.SizeofTypeExpression self, object value) {
            return new {
                Class = "SizeofTypeExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                TypeOperand = self.TypeOperand.Accept(new DataType.ToJsonVisitor(), null),
            };
        }

        public object OnStringExpression(Expression.PrimaryExpression.StringExpression self, object value) {
            return new {
                Class = "StringExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Strings = self.Strings.ToArray(),
            };
        }

        public object OnStructUnionAssignInitializer(Initializer.StructUnionAssignInitializer self, object value) {
            return new {
                Class = "StructUnionAssignInitializer",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Inits = self.Inits.Select(x => x.Accept(this, value)).ToArray(),
            };
        }

        public object OnSwitchStatement(Statement.SwitchStatement self, object value) {
            return new {
                Class = "SwitchStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Cond = self.Cond.Accept(this, value),
                Stmt = self.Stmt.Accept(this, value),
            };
        }

        public object OnTranslationUnit(TranslationUnit self, object value) {
            return new {
                Class = "TranslationUnit",
                LocationRange = LocationRangeToJson(self.LocationRange),
                LinkageTable = self.LinkageTable.Select(x => {
                    return new {
                        LinkageId = x.LinkageId,
                        Id = x.Id,
                        LocationRange = LocationRangeToJson((x.Definition ?? x.TentativeDefinitions[0]).LocationRange),
                        Linkage = x.Linkage.ToString(),
                        Type = x.Type.Accept(new DataType.ToJsonVisitor(), value)
                    };
                }).ToArray(),
                Declarations = self.Declarations.Select(x => x.Accept(this, value)).ToArray(),
            };
        }

        public object OnTypeConversionExpression(Expression.TypeConversionExpression self, object value) {
            return new {
                Class = "TypeConversionExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr?.Accept(this, value),
            };
        }

        public object OnTypeDeclaration(Declaration.TypeDeclaration self, object value) {
            return new {
                Class = "TypeDeclaration",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Ident = self.Ident,
            };
        }

        public object OnUnaryAddressExpression(Expression.UnaryAddressExpression self, object value) {
            return new {
                Class = "UnaryAddressExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr.Accept(this, value)
            };
        }

        public object OnUnaryMinusExpression(Expression.UnaryMinusExpression self, object value) {
            return new {
                Class = "UnaryMinusExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr.Accept(this, value)
            };
        }

        public object OnUnaryNegateExpression(Expression.UnaryNegateExpression self, object value) {
            return new {
                Class = "UnaryNegateExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr.Accept(this, value)
            };
        }

        public object OnUnaryNotExpression(Expression.UnaryNotExpression self, object value) {
            return new {
                Class = "UnaryNotExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr.Accept(this, value)
            };
        }

        public object OnUnaryPlusExpression(Expression.UnaryPlusExpression self, object value) {
            return new {
                Class = "UnaryPlusExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr.Accept(this, value)
            };
        }

        public object OnUnaryPostfixExpression(Expression.PostfixExpression.UnaryPostfixExpression self, object value) {
            return new {
                Class = "UnaryPostfixExpression",
                Op = self.Op.ToString(),
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr.Accept(this, value)
            };
        }

        public object OnUnaryPrefixExpression(Expression.UnaryPrefixExpression self, object value) {
            return new {
                Class = "UnaryPrefixExpression",
                Op = self.Op.ToString(),
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr.Accept(this, value)
            };
        }

        public object OnUnaryReferenceExpression(Expression.UnaryReferenceExpression self, object value) {
            return new {
                Class = "UnaryReferenceExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Expr = self.Expr.Accept(this, value)
            };
        }

        public object OnUndefinedIdentifierExpression(Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression self, object value) {
            return new {
                Class = "UndefinedIdentifierExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Ident = self.Ident
            };
        }

        public object OnVariableDeclaration(Declaration.VariableDeclaration self, object value) {
            return new {
                Class = "VariableDeclaration",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Ident = self.Ident,
                StorageClass = self.StorageClass.ToString(),
                LinkageObject = self.LinkageObject == null ? null : new {
                    LinkageId = self.LinkageObject.LinkageId,
                    Id = self.LinkageObject.Id
                },
                Init = self.Init?.Accept(this, value)
            };
        }

        public object OnVariableExpression(Expression.PrimaryExpression.IdentifierExpression.VariableExpression self, object value) {
            return new {
                Class = "VariableExpression",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Type = self.Type.Accept(new DataType.ToJsonVisitor(), null),
                Ident = self.Ident
            };
        }

        public object OnWhileStatement(Statement.WhileStatement self, object value) {
            return new {
                Class = "WhileStatement",
                LocationRange = LocationRangeToJson(self.LocationRange),
                Cond = self.Cond.Accept(this, value),
                Stmt = self.Stmt.Accept(this, value),
            };
        }


    }
}
