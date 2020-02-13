using System.Collections.Generic;
using System.Linq;
using AnsiCParser.DataType;

namespace AnsiCParser.SyntaxTree {
    public abstract class Statement : Ast {
        /// <summary>
        /// Goto文
        /// </summary>
        public class GotoStatement : Statement {
            /// <summary>
            /// 参照ラベル名
            /// </summary>
            public string Label {
                get;
            }
            /// <summary>
            /// 参照先のラベル付き文(ラベル名前表で挿入する)
            /// </summary>
            public GenericLabeledStatement Target {
                get; set;
            }

            public GotoStatement(LocationRange locationRange, string label) : base(locationRange) {
                Label = label;
                Target = null;
            }
        }

        /// <summary>
        /// Continue文
        /// </summary>
        public class ContinueStatement : Statement {
            public Statement Stmt {
                get;
            }

            public ContinueStatement(LocationRange locationRange, Statement stmt) : base(locationRange) {
                Stmt = stmt;
            }
        }

        /// <summary>
        /// Break文
        /// </summary>
        public class BreakStatement : Statement {
            public Statement Stmt {
                get;
            }

            public BreakStatement(LocationRange locationRange, Statement stmt) : base(locationRange) {
                Stmt = stmt;
            }
        }

        /// <summary>
        /// Return文
        /// </summary>
        public class ReturnStatement : Statement {
            public Expression Expr {
                get;
            }

            public ReturnStatement(LocationRange locationRange, Expression expr) : base(locationRange) {
                Expr = expr;
            }
        }

        /// <summary>
        /// While文
        /// </summary>
        public class WhileStatement : Statement {
            public Expression Cond {
                get;
            }
            public Statement Stmt {
                get; set;
            }

            public WhileStatement(LocationRange locationRange, Expression cond) : base(locationRange) {
                CType elemType;
                if (cond.Type.IsArrayType(out elemType)) { cond= Expression.TypeConversionExpression.Apply(locationRange, CType.CreatePointer(elemType), cond); }
                Cond = Specification.TypeConvert(CType.CreateBool(), cond);
                ;
            }
        }

        /// <summary>
        /// Do-While文
        /// </summary>
        public class DoWhileStatement : Statement {
            public Statement Stmt {
                get; set;
            }
            public Expression Cond {
                get; set;
            }

            public DoWhileStatement(LocationRange locationRange) : base(locationRange) {
            }
        }

        /// <summary>
        /// For文
        /// </summary>
        public class ForStatement : Statement {
            public Expression Init {
                get;
            }
            public Expression Cond {
                get;
            }
            public Expression Update {
                get;
            }
            public Statement Stmt {
                get; set;
            }

            public ForStatement(LocationRange locationRange, Expression init, Expression cond, Expression update) : base(locationRange) {
                Init = init;
                if (cond != null) {
                    CType elemType;
                    if (cond.Type.IsArrayType(out elemType)) { cond = Expression.TypeConversionExpression.Apply(locationRange, CType.CreatePointer(elemType), cond); }
                    Cond = Specification.TypeConvert(CType.CreateBool(), cond);

                } else {
                    Cond = null;
                }
                Update = update;
            }
        }

        /// <summary>
        /// If文
        /// </summary>
        public class IfStatement : Statement {
            public Expression Cond {
                get;
            }
            public Statement ThenStmt {
                get;
            }
            public Statement ElseStmt {
                get;
            }

            public IfStatement(LocationRange locationRange, Expression cond, Statement thenStmt, Statement elseStmt) : base(locationRange) {
                CType elemType;
                if (cond.Type.IsArrayType(out elemType)) { cond = Expression.TypeConversionExpression.Apply(locationRange, CType.CreatePointer(elemType), cond); }
                Cond = Specification.TypeConvert(CType.CreateBool(), cond);
                ThenStmt = thenStmt;
                ElseStmt = elseStmt;
            }
        }

        /// <summary>
        /// Switch文
        /// </summary>
        public class SwitchStatement : Statement {
            public Expression Cond {
                get;
            }
            public Statement Stmt {
                get; set;
            }
            public List<CaseStatement> CaseLabels {
                get;
            }
            public DefaultStatement DefaultLabel {
                get; private set;
            }

            public SwitchStatement(LocationRange locationRange, Expression cond) : base(locationRange) {
                CType elemType;
                if (cond.Type.IsArrayType(out elemType)) { cond = Expression.TypeConversionExpression.Apply(locationRange, CType.CreatePointer(elemType), cond); }
                Cond = cond;
                CaseLabels = new List<CaseStatement>();
                DefaultLabel = null;
            }

            public void AddCaseStatement(CaseStatement caseStatement) {
                if (CaseLabels.Any(x => x.Value == caseStatement.Value)) {
                    throw new CompilerException.SpecificationErrorException(caseStatement.LocationRange.Start, caseStatement.LocationRange.End, "caseラベルの値は既に使われています。");
                }
                CaseLabels.Add(caseStatement);
            }
            public void SetDefaultLabel(DefaultStatement defaultStatement) {
                if (DefaultLabel != null) {
                    throw new CompilerException.SpecificationErrorException(defaultStatement.LocationRange.Start, defaultStatement.LocationRange.End, "defaultラベルは既に使われています。");
                }
                DefaultLabel = defaultStatement;
            }
        }

        /// <summary>
        /// 複文(C89)
        /// </summary>
        public class CompoundStatementC89 : Statement {
            public List<Declaration> Decls {
                get;
            }
            public List<Statement> Stmts {
                get;
            }
            public Scope<TaggedType> TagScope {
                get;
            }
            public Scope<Declaration> IdentScope {
                get;
            }

            public CompoundStatementC89(LocationRange locationRange, List<Declaration> decls, List<Statement> stmts, Scope<TaggedType> tagScope, Scope<Declaration> identScope) : base(locationRange) {
                Decls = decls;
                Stmts = stmts;
                TagScope = tagScope;
                IdentScope = identScope;
            }
        }
        /// <summary>
        /// 複文(C99)
        /// </summary>
        public class CompoundStatementC99 : Statement {
            public List<Ast/*Declaration|Statement*/> DeclsOrStmts {
                get;
            }
            public Scope<TaggedType> TagScope {
                get;
            }
            public Scope<Declaration> IdentScope {
                get;
            }

            public CompoundStatementC99(LocationRange locationRange, List<Declaration> decls, List<Ast> declsOrStmts, Scope<TaggedType> tagScope, Scope<Declaration> identScope) : base(locationRange) {
                DeclsOrStmts = decls.Select(x => (Ast)x).Concat(declsOrStmts).ToList();
                TagScope = tagScope;
                IdentScope = identScope;
            }
        }

        /// <summary>
        /// 空文
        /// </summary>
        public class EmptyStatement : Statement {
            public EmptyStatement(LocationRange locationRange) : base(locationRange) {
            }
        }

        /// <summary>
        /// 式文
        /// </summary>
        public class ExpressionStatement : Statement {
            public Expression Expr {
                get;
            }

            public ExpressionStatement(LocationRange locationRange, Expression expr) : base(locationRange) {
                Expr = expr;
            }
        }

        /// <summary>
        /// Caseラベル付き文
        /// </summary>
        public class CaseStatement : Statement {
            public Expression Expr {
                get;
            }
            public long Value {
                get;
            }
            public Statement Stmt {
                get;
            }

            public CaseStatement(LocationRange locationRange, Expression expr, long value, Statement stmt) : base(locationRange) {
                Expr = expr;
                Value = value;
                Stmt = stmt;
            }
        }

        /// <summary>
        /// Defaultラベル付き文
        /// </summary>

        public class DefaultStatement : Statement {
            public Statement Stmt {
                get;
            }

            public DefaultStatement(LocationRange locationRange, Statement stmt) : base(locationRange) {
                Stmt = stmt;
            }
        }

        /// <summary>
        /// ラベル付き文
        /// </summary>
        public class GenericLabeledStatement : Statement {
            public string Ident {
                get;
            }
            public Statement Stmt {
                get;
            }

            public GenericLabeledStatement(LocationRange locationRange, string ident, Statement stmt) : base(locationRange) {
                Ident = ident;
                Stmt = stmt;
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="locationRange"></param>
        protected Statement(LocationRange locationRange) : base(locationRange) {
        }
    }
}
