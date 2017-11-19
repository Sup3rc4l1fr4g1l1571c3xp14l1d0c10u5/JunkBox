using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AnsiCParser {
    class Program {
        static void Main(string[] args) {
            var grammer = new Grammer();
            string input = @"
int main ( void ) {
    unsigned int buffer [ 234 ] ;
    print ( ""hello,world"" ) ;
    return 0 ;
}
";
            grammer.Parse(Regex.Split(input, @"\s+").Where(x => x.Length > 0).ToList());
        }
    }

    public abstract class CType {
        public List<string> storage_class_specifier { get; } = new List<string>();
        public List<string> type_specifier { get; } = new List<string>();
        public List<string> type_qualifier { get; } = new List<string>();

        public class BasicType : CType {
            protected override void Fixup(CType ty) { }


            public override int Sizeof() {
                // 適当
                if (type_specifier.Contains("void")) {
                    return 0;
                }
                if (type_specifier.Contains("char")) {
                    return 1;
                }
                if (type_specifier.Contains("short")) {
                    return 2;
                }
                if (type_specifier.Contains("int")) {
                    if (type_specifier.Contains("short")) {
                        return 2;
                    }
                    if (type_specifier.Contains("long")) {
                        return 4;
                    }
                    return 4;
                }
                if (type_specifier.Contains("long")) {
                    if (type_specifier.Contains("double")) {
                        return 12;  // 80bit
                    }
                    return 4;
                }
                if (type_specifier.Contains("double")) {
                    return 8;
                }
                if (type_specifier.Contains("float")) {
                    return 4;
                }
                throw new Exception();
            }

        }

        public bool is_typedef() {
            return storage_class_specifier.Contains("typedef");
        }
        public bool is_function() {
            return this is CType.FunctionType;
        }
        public bool is_variable() {
            return !is_typedef() && !is_function();
        }

        public static CType resolv_type(CType baseType, List<CType> stack) {
            return stack.Aggregate(baseType, (s, x) => { x.Fixup(s); return x; });
        }

        protected virtual void Fixup(CType ty) {
            throw new NotImplementedException();
        }
        public abstract int Sizeof();

        public abstract class TaggedType : CType {
            public class StructUnionType : TaggedType {
                private string struct_or_union;
                private string ident;
                private bool v;
                public List<Tuple<string, CType, AST.Expression>> struct_declarations { get; internal set; }

                public StructUnionType(string struct_or_union, string ident, bool v) {
                    this.struct_or_union = struct_or_union;
                    this.ident = ident;
                    this.v = v;
                }

                protected override void Fixup(CType ty) {
                    for (var i = 0; i < struct_declarations.Count; i++) {
                        var struct_declaration = struct_declarations[i];
                        if (struct_declaration.Item2 is StubType) {
                            struct_declarations[i] = Tuple.Create(struct_declaration.Item1, ty, struct_declaration.Item3);
                        } else {
                            struct_declaration.Item2.Fixup(ty);
                        }
                    }

                    foreach (var struct_declaration in struct_declarations) {
                        struct_declaration.Item2.Fixup(ty);
                    }
                }

                public override int Sizeof() {
                    // ビットフィールドは未実装
                    if (struct_or_union == "struct") {
                        return struct_declarations.Sum(x => x.Item2.Sizeof());
                    } else {
                        return struct_declarations.Max(x => x.Item2.Sizeof());
                    }
                }
            }

            public class EnumType : TaggedType {
                public string ident;
                public bool IsAnonymous;
                public List<Tuple<string, int>> enumerator_list { get; set; }

                public EnumType(string ident, bool is_anonymous) {
                    this.ident = ident;
                    this.IsAnonymous = is_anonymous;
                }

                protected override void Fixup(CType ty) {
                }

                public override int Sizeof() {
                    return 4;
                }
            }
        }

        internal class StubType : CType {
            public override int Sizeof() {
                throw new Exception();
            }
        }

        internal class FunctionType : CType {
            private List<Tuple<string, CType>> args;
            private CType type;
            private bool vargs;

            public FunctionType(List<Tuple<string, CType>> args, bool vargs, CType type) {
                this.args = args;
                this.type = type;
                this.vargs = vargs;
            }
            protected override void Fixup(CType ty) {
                for (var i = 0; i < args.Count; i++) {
                    var arg = args[i];
                    if (arg.Item2 is StubType) {
                        args[i] = Tuple.Create(arg.Item1, ty);
                    } else {
                        arg.Item2.Fixup(ty);
                    }
                }
                if (type is StubType) {
                    type = ty;
                } else {
                    type.Fixup(ty);
                }
            }
            public override int Sizeof() {
                return 4;
            }
        }

        internal class PointerType : CType {
            private CType cType;

            public PointerType(CType cType) {
                this.cType = cType;
            }
            protected override void Fixup(CType ty) {
                if (cType is StubType) {
                    cType = ty;
                } else {
                    cType.Fixup(ty);
                }
            }
            public override int Sizeof() {
                return 4;
            }
        }

        public class ArrayType : CType {
            private int len;
            private CType cType;

            public ArrayType(int len, CType cType) {
                this.len = len;
                this.cType = cType;
            }
            protected override void Fixup(CType ty) {
                if (cType is StubType) {
                    cType = ty;
                } else {
                    cType.Fixup(ty);
                }
            }
            public override int Sizeof() {
                return (len < 0) ? 4 : cType.Sizeof() * (len);
            }
        }

        public class TypedefedType : CType {
            private string v;
            private CType cType;
            public TypedefedType(string v, CType ctype) {
                this.v = v;
                this.cType = ctype;
            }
            public override int Sizeof() {
                return this.cType.Sizeof();
            }
        }

    }

    public abstract class AST {
        public abstract class Expression : AST {
            public abstract int Eval();
            public class CommaExpression : Expression {
                public List<AST.Expression> expressions { get; } = new List<AST.Expression>();
                public override int Eval() {
                    return expressions.Aggregate(0, (s, x) => x.Eval());
                }
            }

            public class AssignmentExpression : Expression {
                public string Op { get; }
                public Expression Lhs { get; }
                public Expression Rhs { get; }

                public AssignmentExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
                public override int Eval() {
                    throw new Exception();
                }

            }

            public class ConditionalExpression : Expression {
                public Expression Cond { get; }
                public Expression ThenExpr { get; }
                public Expression ElseExpr { get; }

                public ConditionalExpression(Expression cond, Expression thenExpr, Expression elseExpr) {
                    Cond = cond;
                    ThenExpr = thenExpr;
                    ElseExpr = elseExpr;
                }
                public override int Eval() {
                    var cond = Cond.Eval();
                    if (cond != 0) {
                        return ThenExpr.Eval();
                    } else {
                        return ElseExpr.Eval();
                    }
                }
            }

            public class LogicalOrExpression : Expression {
                public Expression Lhs { get; }
                public Expression Rhs { get; }

                public LogicalOrExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }
                public override int Eval() {
                    var lhs = Lhs.Eval();
                    if (lhs == 0) {
                        return Rhs.Eval() == 0 ? 0 : 1;
                    } else {
                        return 1;
                    }
                }
            }

            public class LogicalAndExpression : Expression {
                public Expression Lhs { get; }
                public Expression Rhs { get; }

                public LogicalAndExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }
                public override int Eval() {
                    var lhs = Lhs.Eval();
                    if (lhs != 0) {
                        return Rhs.Eval() == 0 ? 0 : 1;
                    } else {
                        return 0;
                    }
                }
            }

            public class InclusiveOrExpression : Expression {
                public Expression Lhs { get; }
                public Expression Rhs { get; }

                public InclusiveOrExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }
                public override int Eval() {
                    var lhs = Lhs.Eval();
                    var rhs = Rhs.Eval();
                    return lhs | rhs;
                }
            }

            public class ExclusiveOrExpression : Expression {
                public Expression Lhs { get; }
                public Expression Rhs { get; }

                public ExclusiveOrExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }
                public override int Eval() {
                    var lhs = Lhs.Eval();
                    var rhs = Rhs.Eval();
                    return lhs ^ rhs;
                }
            }

            public class AndExpression : Expression {
                public Expression Lhs { get; }
                public Expression Rhs { get; }

                public AndExpression(Expression lhs, Expression rhs) {
                    Lhs = lhs;
                    Rhs = rhs;
                }
                public override int Eval() {
                    var lhs = Lhs.Eval();
                    var rhs = Rhs.Eval();
                    return lhs & rhs;
                }
            }

            public class EqualityExpression : Expression {
                public string Op { get; }
                public Expression Lhs { get; }
                public Expression Rhs { get; }

                public EqualityExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
                public override int Eval() {
                    var lhs = Lhs.Eval();
                    var rhs = Rhs.Eval();
                    switch (Op) {
                        case "==":
                            return lhs == rhs ? 1 : 0;
                        case "!=":
                            return lhs != rhs ? 1 : 0;
                        default:
                            throw new Exception();
                    }
                }
            }

            public class RelationalExpression : Expression {
                public string Op { get; }
                public Expression Lhs { get; }
                public Expression Rhs { get; }

                public RelationalExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
                public override int Eval() {
                    var lhs = Lhs.Eval();
                    var rhs = Rhs.Eval();
                    switch (Op) {
                        case "<":
                            return lhs < rhs ? 1 : 0;
                        case ">":
                            return lhs > rhs ? 1 : 0;
                        case "<=":
                            return lhs <= rhs ? 1 : 0;
                        case ">=":
                            return lhs >= rhs ? 1 : 0;
                        default:
                            throw new Exception();
                    }
                }
            }

            public class ShiftExpression : Expression {
                public string Op { get; }
                public Expression Lhs { get; }
                public Expression Rhs { get; }

                public ShiftExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
                public override int Eval() {
                    var lhs = Lhs.Eval();
                    var rhs = Rhs.Eval();
                    switch (Op) {
                        case "<<":
                            return lhs << rhs;
                        case ">>":
                            return lhs >> rhs;
                        default:
                            throw new Exception();
                    }
                }
            }

            public class AdditiveExpression : Expression {
                public string Op { get; }
                public Expression Lhs { get; }
                public Expression Rhs { get; }

                public AdditiveExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
                public override int Eval() {
                    var lhs = Lhs.Eval();
                    var rhs = Rhs.Eval();
                    switch (Op) {
                        case "+":
                            return lhs + rhs;
                        case "-":
                            return lhs - rhs;
                        default:
                            throw new Exception();
                    }
                }
            }

            public class MultiplicitiveExpression : Expression {
                public string Op { get; }
                public Expression Lhs { get; }
                public Expression Rhs { get; }

                public MultiplicitiveExpression(string op, Expression lhs, Expression rhs) {
                    Op = op;
                    Lhs = lhs;
                    Rhs = rhs;
                }
                public override int Eval() {
                    var lhs = Lhs.Eval();
                    var rhs = Rhs.Eval();
                    switch (Op) {
                        case "*":
                            return lhs * rhs;
                        case "/":
                            return lhs / rhs;
                        case "%":
                            return lhs % rhs;
                        default:
                            throw new Exception();
                    }
                }
            }

            public class CastExpression : Expression {
                public CType Ty { get; }
                public Expression Expr { get; }

                public CastExpression(CType ty, Expression expr) {
                    Ty = ty;
                    Expr = expr;
                }
                public override int Eval() {
                    // キャストは未実装
                    return Expr.Eval();
                }
            }

            public class UnaryPrefixExpression : Expression {
                public string Op { get; }
                public Expression Expr { get; }

                public UnaryPrefixExpression(string op, Expression expr) {
                    Op = op;
                    Expr = expr;
                }
                public override int Eval() {
                    throw new Exception();
                }
            }

            public class UnaryAddressExpression : Expression {
                public Expression Expr { get; }

                public UnaryAddressExpression(Expression expr) {
                    Expr = expr;
                }
                public override int Eval() {
                    throw new Exception();
                }
            }

            public class UnaryReferenceExpression : Expression {
                public Expression Expr { get; }

                public UnaryReferenceExpression(Expression expr) {
                    Expr = expr;
                }
                public override int Eval() {
                    throw new Exception();
                }
            }

            public class UnaryPlusExpression : Expression {
                public Expression Expr { get; }

                public UnaryPlusExpression(Expression expr) {
                    Expr = expr;
                }
                public override int Eval() {
                    return Expr.Eval();
                }
            }

            public class UnaryMinusExpression : Expression {
                public Expression Expr { get; }

                public UnaryMinusExpression(Expression expr) {
                    Expr = expr;
                }
                public override int Eval() {
                    return -Expr.Eval();
                }
            }

            public class UnaryNegateExpression : Expression {
                public Expression Expr { get; }

                public UnaryNegateExpression(Expression expr) {
                    Expr = expr;
                }
                public override int Eval() {
                    return ~Expr.Eval();
                }
            }

            public class UnaryNotExpression : Expression {
                public Expression Expr { get; }

                public UnaryNotExpression(Expression expr) {
                    Expr = expr;
                }
                public override int Eval() {
                    return Expr.Eval() == 0 ? 1 : 0;
                }
            }

            public class SizeofTypeExpression : Expression {
                public CType Ty { get; }

                public SizeofTypeExpression(CType ty) {
                    Ty = ty;
                }
                public override int Eval() {
                    return Ty.Sizeof();
                }
            }

            public class SizeofExpression : Expression {
                public Expression Expr { get; }

                public SizeofExpression(Expression expr) {
                    Expr = expr;
                }
                public override int Eval() {
                    // 未実装につきintサイズ固定
                    return 4;
                }
            }

            public class ArrayIndexExpression : Expression {
                public Expression Expr { get; }
                public Expression Index { get; }

                public ArrayIndexExpression(Expression expr, Expression index) {
                    Expr = expr;
                    Index = index;
                }
                public override int Eval() {
                    throw new Exception();
                }
            }

            public class FunctionCallExpression : Expression {
                public Expression Expr { get; }
                public List<Expression> Args { get; }

                public FunctionCallExpression(Expression expr, List<Expression> args) {
                    Expr = expr;
                    Args = args;
                }
                public override int Eval() {
                    throw new Exception();
                }
            }

            public class MemberDirectAccess : Expression {
                public Expression Expr { get; }
                public string Ident { get; }

                public MemberDirectAccess(Expression expr, string ident) {
                    Expr = expr;
                    Ident = ident;
                }
                public override int Eval() {
                    throw new Exception();
                }
            }

            public class MemberIndirectAccess : Expression {
                public Expression Expr { get; }
                public string Ident { get; }

                public MemberIndirectAccess(Expression expr, string ident) {
                    Expr = expr;
                    Ident = ident;
                }
                public override int Eval() {
                    throw new Exception();
                }
            }

            public class UnaryPostfixExpression : Expression {
                public string Op { get; }
                public Expression Expr { get; }

                public UnaryPostfixExpression(string op, Expression expr) {
                    Op = op;
                    Expr = expr;
                }
                public override int Eval() {
                    throw new Exception();
                }
            }

            public class IdentifierExpression : Expression {
                public string Ident { get; }

                public IdentifierExpression(string ident) {
                    Ident = ident;
                }
                public override int Eval() {
                    throw new Exception();
                }
            }

            public class StringExpression : Expression {
                public List<string> Strings { get; }

                public StringExpression(List<string> strings) {
                    Strings = strings;
                }
                public override int Eval() {
                    throw new Exception();
                }
            }

            public class IntegerConstant : Expression {
                public string Ret { get; }

                public IntegerConstant(string ret) {
                    Ret = ret;
                }
                public override int Eval() {
                    return Convert.ToInt32(Ret, 10);
                }
            }

            public class CharacterConstant : Expression {
                public string Ret { get; }

                public CharacterConstant(string ret) {
                    Ret = ret;
                }
                public override int Eval() {
                    return Ret[1];
                }
            }

            public class FloatingConstant : Expression {
                public string Ret { get; }

                public FloatingConstant(string ret) {
                    Ret = ret;
                }
                public override int Eval() {
                    // 未実装
                    throw new Exception();
                }
            }

            public class EnumerationConstant : Expression {
                public Tuple<string, int> Ret { get; }

                public EnumerationConstant(Tuple<string, int> ret) {
                    Ret = ret;
                }
                public override int Eval() {
                    // 未実装
                    return Ret.Item2;
                }
            }

        }

        public class Statement : AST {
            public class GotoStatement : Statement {
                public string Label { get; }

                public GotoStatement(string label) {
                    Label = label;
                }
            }

            public class ContinueStatement : Statement {
                public Statement Value { get; }

                public ContinueStatement(Statement value) {
                    Value = value;
                }
            }

            public class BreakStatement : Statement {
                public Statement Value { get; }

                public BreakStatement(Statement value) {
                    Value = value;
                }
            }

            public class ReturnStatement : Statement {
                public Expression Expr { get; }

                public ReturnStatement(Expression expr) {
                    Expr = expr;
                }
            }

            public class WhileStatement : Statement {
                public Expression Cond { get; }
                public Statement statement { get; set; }

                public WhileStatement(Expression cond) {
                    Cond = cond;
                }
            }

            public class DoWhileStatement : Statement {
                public Statement statement { get; set; }
                public Expression cond { get; set; }
            }

            public class ForStatement : Statement {
                public Expression Init { get; }
                public Expression Cond { get; }
                public Expression Update { get; }
                public Statement statement { get; set; }

                public ForStatement(Expression init, Expression cond, Expression update) {
                    Init = init;
                    Cond = cond;
                    Update = update;
                }
            }

            public class IfStatement : Statement {
                public Expression Cond { get; }
                public Statement ThenStmt { get; }
                public Statement ElseStmt { get; }

                public IfStatement(Expression cond, Statement thenStmt, Statement elseStmt) {
                    Cond = cond;
                    ThenStmt = thenStmt;
                    ElseStmt = elseStmt;
                }
            }

            public class SwitchStatement : Statement {
                public Expression Cond { get; }
                public Statement statement { get; set; }

                public SwitchStatement(Expression cond) {
                    Cond = cond;
                }
            }

            public class CompoundStatement : Statement {
                public List<Declaration> Decls { get; }
                public List<Statement> Stmts { get; }
                public Scope<CType.TaggedType> TagScope { get; }
                public Scope<Grammer.IdentifierValue> IdentScope { get; }

                public CompoundStatement(List<Declaration> decls, List<Statement> stmts, Scope<CType.TaggedType> tagScope, Scope<Grammer.IdentifierValue> identScope) {
                    Decls = decls;
                    Stmts = stmts;
                    TagScope = tagScope;
                    IdentScope = identScope;
                }
            }

            public class EmptyStatement : Statement {
            }

            public class ExpressionStatement : Statement {
                public Expression Expr { get; }

                public ExpressionStatement(Expression expr) {
                    Expr = expr;
                }
            }

            public class CaseStatement : Statement {
                public Expression Expr { get; }
                public Statement Stmt { get; }

                public CaseStatement(Expression expr, Statement stmt) {
                    Expr = expr;
                    Stmt = stmt;
                }
            }

            public class DefaultStatement : Statement {
                public Statement Stmt { get; }

                public DefaultStatement(Statement stmt) {
                    Stmt = stmt;
                }
            }

            public class GenericLabeledStatement : Statement {
                public string Ident { get; }
                public Statement Stmt { get; }

                public GenericLabeledStatement(string ident, Statement stmt) {
                    Ident = ident;
                    Stmt = stmt;
                }
            }
        }

        public abstract class Initializer : AST {
            public class CompilxInitializer : Initializer {
                public List<Initializer> Ret { get; }

                public CompilxInitializer(List<Initializer> ret) {
                    Ret = ret;
                }
            }

            public class SimpleInitializer : Initializer {
                public Expression AssignmentExpression { get; }

                public SimpleInitializer(Expression assignmentExpression) {
                    AssignmentExpression = assignmentExpression;
                }
            }
        }

        public class Declaration {
            //public string Ident { get; }
            //public CType Ty { get; }
            //public Initializer Init { get; }

            //public Declaration(string ident, CType ty, Initializer init) {
            //    this.Ident = ident;
            //    this.Ty = ty;
            //    this.Init = init;
            //}

            public class FunctionDeclaration : Declaration {

                public string Ident { get; }
                public CType Ty { get; }
                public Statement Body { get; }

                public FunctionDeclaration(string ident, CType ty, Statement body) {
                    Ident = ident;
                    Ty = ty;
                    Body = body;
                }

                public FunctionDeclaration(string ident, CType ty) {
                    Ident = ident;
                    Ty = ty;
                    Body = null;
                }
            }

            public class VariableDeclaration : Declaration {
                public string Ident { get; }
                public CType Ctype { get; }
                public Initializer Init { get; }

                public VariableDeclaration(string ident, CType ctype, Initializer init) {
                    Ident = ident;
                    Ctype = ctype;
                    Init = init;
                }
            }

            public class TypeDeclaration : Declaration {
                public string Ident { get; }
                public CType Ctype { get; }

                public TypeDeclaration(string ident, CType ctype) {
                    Ident = ident;
                    Ctype = ctype;
                }
            }
        }
    }

    public class LinkedList<TValue> {
        public TValue Value { get; }
        public LinkedList<TValue> Parent { get; }
        public static LinkedList<TValue> Empty { get; } = new LinkedList<TValue>();

        private LinkedList(TValue value, LinkedList<TValue> parent) {
            this.Value = Value;
            this.Parent = parent;
        }
        private LinkedList() {
            this.Value = default(TValue);
            this.Parent = null;
        }
        public LinkedList<TValue> Extend(TValue value) {
            return new LinkedList<TValue>(value, this);
        }

        public bool Contains(TValue v) {
            var it = this;
            while (it != Empty) {
                if (it.Value.Equals(v)) {
                    return true;
                }
            }
            return false;
        }
    }
    public class LinkedDictionary<TKey, TValue> {
        public TKey Key { get; }
        public TValue Value { get; }
        public LinkedDictionary<TKey, TValue> Parent { get; }
        public static LinkedDictionary<TKey, TValue> Empty { get; } = new LinkedDictionary<TKey, TValue>();

        private LinkedDictionary(TKey key, TValue value, LinkedDictionary<TKey, TValue> parent) {
            this.Key = Key;
            this.Value = Value;
            this.Parent = parent;
        }
        private LinkedDictionary() {
            this.Key = default(TKey);
            this.Value = default(TValue);
            this.Parent = null;
        }
        public LinkedDictionary<TKey, TValue> Extend(TKey key, TValue value) {
            return new LinkedDictionary<TKey, TValue>(key, value, this);
        }

        public bool ContainsKey(TKey key) {
            var it = this;
            while (it != Empty) {
                if (it.Key != null && it.Key.Equals(key)) {
                    return true;
                }
                it = it.Parent;
            }
            return false;
        }
        public bool TryGetValue(TKey key, out TValue value) {
            var it = this;
            while (it != Empty) {
                if (it.Key != null && it.Key.Equals(key)) {
                    value = it.Value;
                    return true;
                }
                it = it.Parent;
            }
            value = default(TValue);
            return false;
        }
    }

    public class Scope<TValue> {
        public static Scope<TValue> Empty { get; } = new Scope<TValue>();

        private LinkedDictionary<string, TValue> entries = LinkedDictionary<string, TValue>.Empty;
        public Scope<TValue> Parent { get; } = Empty;

        private Scope() { }
        protected Scope(Scope<TValue> Parent) {
            this.Parent = Parent;
        }
        public Scope<TValue> Extend() {
            return new Scope<TValue>(this);
        }

        public void Add(string ident, TValue value) {
            this.entries = this.entries.Extend(ident, value);
        }

        public bool ContainsKey(string v) {
            var it = this;
            while (it != Empty) {
                if (it.entries.ContainsKey(v)) {
                    return true;
                }
                it = it.Parent;
            }
            return false;
        }

        public bool TryGetValue(string v, out TValue value) {
            var it = this;
            while (it != Empty) {
                if (it.entries.TryGetValue(v, out value)) {
                    return true;
                }
                it = it.Parent;
            }
            value = default(TValue);
            return false;
        }
    }


    public class Grammer {
        public abstract class IdentifierValue {
            public virtual bool IsEnumValue() { return false; }
            public virtual Tuple<string, CType, int> ToEnumValue() { throw new Exception(""); }
            public virtual bool IsTypedefedType() { return false; }
            public virtual CType ToTypedefedType() { throw new Exception(""); }
            public virtual bool IsVariable() { return false; }
            public virtual bool IsFunction() { return false; }

            public class EnumValue : IdentifierValue {
                public override bool IsEnumValue() { return false; }
                public override Tuple<string, CType, int> ToEnumValue() {
                    return Tuple.Create(ident, (CType)ctype, ctype.enumerator_list.FindIndex(x => x.Item1 == ident));
                }
                public CType.TaggedType.EnumType ctype { get; }
                public string ident { get; }
                public EnumValue(CType.TaggedType.EnumType ctype, string ident) {
                    this.ctype = ctype;
                    this.ident = ident;
                }
            }

            public class Declaration : IdentifierValue {
                public override bool IsTypedefedType() { return Decl is AST.Declaration.TypeDeclaration; }
                public override CType ToTypedefedType() { return (Decl as AST.Declaration.TypeDeclaration).Ctype; }
                public override bool IsVariable() { return Decl is AST.Declaration.VariableDeclaration; }
                public override bool IsFunction() { return Decl is AST.Declaration.FunctionDeclaration; }
                public AST.Declaration Decl { get; }

                public Declaration(AST.Declaration decl) {
                    Decl = decl;
                }
            }
        }

        private Scope<CType.TaggedType> tag_scope = Scope<CType.TaggedType>.Empty;
        private Scope<IdentifierValue> ident_scope = Scope<IdentifierValue>.Empty;

        private LinkedList<AST.Statement> break_scope = LinkedList<AST.Statement>.Empty;
        private LinkedList<AST.Statement> continue_scope = LinkedList<AST.Statement>.Empty;

        private List<string> tokens = new List<string>();
        private int current = 0;

        public Grammer() {
            this.tokens.AddRange(tokens);
        }
        public void Parse(List<string> tokens) {
            this.tokens = tokens.ToList();
            current = 0;
            translation_unit();
        }

        private void next_token() {
            current++;
        }
        private bool eof() {
            return tokens.Count <= current;
        }

        private string current_token() {
            if (eof()) {
                throw new Exception();
            }
            return tokens[current];
        }

        private void token(params string[] s) {
            if (s.Contains(current_token())) {
                next_token();
                return;
            }
            throw new Exception();
        }
        private bool istoken(params string[] s) {
            if (eof()) {
                return false;
            }
            return s.Contains(current_token());
        }

        private bool is_nexttoken(params string[] s) {
            if (tokens.Count <= current + 1) {
                return false;
            }
            return s.Contains(tokens[current + 1]);
        }

        private bool is_identifier() {
            if (istoken(
                    "auto", "register", "static", "extern", "typedef",
                    "void", "char", "int", "float", "double",
                    "short", "long",
                    "signed", "unsigned",
                    "struct", "union",
                    "enum",
                    "const", "volatile",
                    "for", "while", "do",
                    "if", "else",
                    "switch", "case", "default",
                    "continue", "break", "goto",
                    "return"
                )) {
                return false;
            }
            return Regex.IsMatch(current_token(), "^[A-Za-z_][A-Za-z0-9_]*$");
        }

        private static readonly string D = $@"[0-9]";
        private static readonly string L = $@"[a-zA-Z_]";
        private static readonly string H = $@"[a-fA-F0-9]";
        private static readonly string E = $@"[Ee][+-]?{D}+";
        private static readonly string FS = $@"(f|F|l|L)";
        private static readonly string IS = $@"(u|U|l|L)*";
        private static readonly Regex RegexPreprocessingNumber = new Regex($@"^(\.?\d([eEpP][\+\-]|\.|({L}|{D}|_))*)$");
        private static readonly Regex RegexFlating = new Regex($@"^({D}+{E}|{D}*\.{D}+({E})?|{D}+\.{D}*({E})?){FS}?$");
        private static readonly Regex RegexHeximal = new Regex($@"^0[xX]{H}+{IS}?$");
        private static readonly Regex RegexDecimal = new Regex($@"^{D}+{IS}?$");
        private static readonly Regex RegexOctal = new Regex($@"^0{D}+{IS}?$");
        private static readonly Regex RegexChar = new Regex($@"^L?'(\.|[^\'])+'$");
        private static readonly Regex RegexStringLiteral = new Regex($@"^L?""(\.|[^\""])*""$");


        private bool is_ENUMERATION_CONSTANT() {
            if (!is_identifier()) {
                return false;
            }
            var ident = current_token();
            IdentifierValue v;
            if (ident_scope.TryGetValue(ident, out v) == false) {
                return false;
            }
            if (!(v is IdentifierValue.EnumValue)) {
                return false;
            }
            return (v as IdentifierValue.EnumValue).ctype.enumerator_list.First(x => x.Item1 == ident) != null;
        }

        private Tuple<string, int> ENUMERATION_CONSTANT() {
            var ident = IDENTIFIER();
            IdentifierValue v;
            if (ident_scope.TryGetValue(ident, out v) == false) {
                throw new Exception();
            }
            if (!(v is IdentifierValue.EnumValue)) {
                throw new Exception();
            }
            return (v as IdentifierValue.EnumValue).ctype.enumerator_list.First(x => x.Item1 == ident);
        }

        private bool is_CHARACTER_CONSTANT() {
            if (eof()) {
                return false;
            }
            return RegexChar.IsMatch(current_token());
        }

        private string CHARACTER_CONSTANT() {
            if (is_CHARACTER_CONSTANT() == false) {
                throw new Exception();
            }
            var ret = current_token();
            next_token();
            return ret;
        }

        private bool IsPreprocessingNumber() {
            if (eof()) {
                return false;
            }
            return RegexPreprocessingNumber.IsMatch(current_token());
        }


        private bool is_FLOATING_CONSTANT() {
            return IsPreprocessingNumber() && RegexFlating.IsMatch(current_token());
        }
        private string FLOATING_CONSTANT() {
            if (is_FLOATING_CONSTANT() == false) {
                throw new Exception();
            }
            var ret = current_token();
            next_token();
            return ret;
        }

        private bool is_INTEGER_CONSTANT() {
            return IsPreprocessingNumber() && (RegexHeximal.IsMatch(current_token()) || RegexOctal.IsMatch(current_token()) || RegexDecimal.IsMatch(current_token()));
        }
        private string INTEGER_CONSTANT() {
            if (is_INTEGER_CONSTANT() == false) {
                throw new Exception();
            }
            var ret = current_token();
            next_token();
            return ret;
        }

        private bool is_STRING() {
            if (eof()) {
                return false;
            }
            return RegexStringLiteral.IsMatch(current_token());
        }
        private string STRING() {
            if (is_STRING() == false) {
                throw new Exception();
            }
            var ret = current_token();
            next_token();
            return ret;
        }

        //
        //
        //


        public void translation_unit() {
            while (is_external_declaration()) {
                external_declaration();
            }
            __FinishParse();
        }

        private void __FinishParse() {
            // skip space and check eof
        }

        private bool is_external_declaration() {
            return (is_declaration_specifier() || istoken(";") || is_declarator());
        }

        private void external_declaration() {
            CType baseType = new CType.BasicType();
            while (is_declaration_specifier()) {
                declaration_specifier(baseType);
            }

            if (!is_declarator()) {
                token(";");
                return;
            } else {
                string ident = "";
                List<CType> stack = new List<CType>() { new CType.StubType() };
                declarator(ref ident, stack, 0);
                var ctype = CType.resolv_type(baseType, stack);
                if (istoken("=", ",")) {
                    // 初期化式を含む各種定義・宣言
                    for (; ; ) {
                        if (istoken("=")) {
                            token("=");
                            initializer();
                        }
                        if (istoken(",")) {
                            token(",");
                            continue;
                        }
                        break;
                    }
                    token(";");
                    return;
                } else {
                    // 関数定義

                    // K&Rの引数型宣言
                    var argmuents = is_declaration() ? declaration() : null;

                    // ctypeがK&R型の宣言ならここでctypeの引数部分とargumentsを照合してマージする。


                    var stmts = compound_statement();

                    var funcdecl = new AST.Declaration.FunctionDeclaration(ident, ctype, stmts);
                }
            }

        }

        private bool is_declaration() {
            return is_declaration_specifier();
        }
        private List<AST.Declaration> declaration() {
            CType baseType = new CType.BasicType();
            baseType = declaration_specifier(baseType);
            while (is_declaration_specifier()) {
                baseType = declaration_specifier(baseType);
            }
            List<AST.Declaration> decls = null;
            if (!istoken(";")) {
                decls = new List<AST.Declaration>();
                decls.Add(init_declarator(baseType));
                while (istoken(",")) {
                    token(",");
                    decls.Add(init_declarator(baseType));
                }
            }
            token(";");
            // 変数・関数・型の宣言を区別して構文木を生成する
            return decls;
        }
        private bool is_declaration_specifier() {
            return (is_storage_class_specifier() ||
                is_type_specifier() ||
                is_struct_or_union_specifier() ||
                is_enum_specifier() ||
                is_TYPEDEF_NAME() ||
                is_type_qualifier());
        }
        private CType declaration_specifier(CType ctype) {
            if (is_storage_class_specifier()) {
                ctype.storage_class_specifier.Add(current_token());
                next_token();
            } else if (is_type_specifier()) {
                ctype.type_specifier.Add(current_token());
                next_token();
            } else if (is_struct_or_union_specifier()) {
                return struct_or_union_specifier();
            } else if (is_enum_specifier()) {
                return enum_specifier();
            } else if (is_TYPEDEF_NAME()) {
                IdentifierValue value;
                if (ident_scope.TryGetValue(current_token(), out value) == false || value.IsTypedefedType() == false) {
                    throw new Exception();
                }
                ctype = new CType.TypedefedType(current_token(), value.ToTypedefedType());
                next_token();
            } else if (is_type_qualifier()) {
                ctype.type_qualifier.Add(current_token());
                next_token();
            } else {
                throw new Exception("");
            }
            return ctype;
        }
        private bool is_storage_class_specifier() {
            return istoken("auto", "register", "static", "extern", "typedef");
        }
        private bool is_type_specifier() {
            return istoken("void", "char", "int", "float", "double",
                           "short", "long",
                           "signed", "unsigned");
        }
        private bool is_struct_or_union_specifier() {
            return istoken("struct", "union");
        }
        private bool is_enum_specifier() {
            return istoken("enum");
        }
        private bool is_TYPEDEF_NAME() {
            return is_identifier() && is_typedefed_type(current_token());
        }

        private bool is_typedefed_type(string v) {
            return ident_scope.ContainsKey(v);
        }

        private bool is_type_qualifier() {
            return istoken("const", "volatile");
        }

        private int anony = 0;

        private CType struct_or_union_specifier() {
            var struct_or_union = current_token();
            token("struct", "union");

            if (is_identifier()) {
                var ident = IDENTIFIER();
                var ctype = new CType.TaggedType.StructUnionType(struct_or_union, ident, false);
                // ctype を tag に 登録
                tag_scope.Add(ident, ctype);
                if (istoken("{")) {
                    token("{");
                    ctype.struct_declarations = struct_declarations();
                    token("}");
                }
                return ctype;
            } else {
                var ident = $"${struct_or_union}_{anony++}";
                var ctype = new CType.TaggedType.StructUnionType(struct_or_union, ident, true);
                // ctype を tag に 登録
                tag_scope.Add(ident, ctype);
                token("{");
                ctype.struct_declarations = struct_declarations();
                token("}");
                return ctype;
            }
        }
        private List<Tuple<string, CType, AST.Expression>> struct_declarations() {
            var items = new List<Tuple<string, CType, AST.Expression>>();
            items.AddRange(struct_declaration());
            while (is_struct_declaration()) {
                items.AddRange(struct_declaration());
            }
            return items;
        }

        private bool is_struct_declaration() {
            return is_specifier_qualifier();
        }
        private List<Tuple<string, CType, AST.Expression>> struct_declaration() {
            CType ctype = new CType.BasicType();
            ctype = specifier_qualifier(ctype);
            while (is_specifier_qualifier()) {
                ctype = specifier_qualifier(ctype);
            }
            var ret = struct_declarator_list(ctype);
            token(";");
            return ret;
        }
        private bool is_init_declarator() {
            return is_declarator();
        }

        private AST.Declaration init_declarator(CType ctype) {
            string ident = "";
            List<CType> stack = new List<CType>() { new CType.StubType() };
            declarator(ref ident, stack, 0);
            ctype = CType.resolv_type(ctype, stack);
            AST.Declaration decl;
            if (istoken("=")) {
                // 変数じゃないとダメよ
                if (!ctype.is_variable()) {
                    throw new Exception("");
                }
                token("=");
                var init = initializer();
                decl = new AST.Declaration.VariableDeclaration(ident, ctype, init);
            } else if (ctype.is_typedef()) {
                decl = new AST.Declaration.TypeDeclaration(ident, ctype);
            } else if (ctype.is_function()) {
                decl = new AST.Declaration.FunctionDeclaration(ident, ctype);
            } else {
                decl = new AST.Declaration.VariableDeclaration(ident, ctype, null);
            }
            ident_scope.Add(ident, new IdentifierValue.Declaration(decl));
            return decl;
        }

        private bool is_specifier_qualifier() {
            return (is_type_specifier() ||
                is_struct_or_union_specifier() ||
                is_enum_specifier() ||
                is_TYPEDEF_NAME() ||
                is_type_qualifier());
        }

        private CType specifier_qualifier(CType ctype) {
            if (is_type_specifier()) {
                ctype.type_specifier.Add(current_token());
                next_token();
                return ctype;
            } else if (is_struct_or_union_specifier()) {
                return struct_or_union_specifier();
            } else if (is_enum_specifier()) {
                return enum_specifier();
            } else if (is_TYPEDEF_NAME()) {
                IdentifierValue value;
                if (ident_scope.TryGetValue(current_token(), out value) == false || value.IsTypedefedType() == false) {
                    throw new Exception();
                }
                ctype = new CType.TypedefedType(current_token(), value.ToTypedefedType());
                next_token();
                return ctype;
            } else if (is_type_qualifier()) {
                ctype.type_qualifier.Add(current_token());
                next_token();
                return ctype;
            } else {
                throw new Exception("");
            }
            return null;
        }
        private List<Tuple<string, CType, AST.Expression>> struct_declarator_list(CType ctype) {
            var ret = new List<Tuple<string, CType, AST.Expression>>();
            ret.Add(struct_declarator(ctype));
            while (istoken(",")) {
                token(",");
                ret.Add(struct_declarator(ctype));
            }
            return ret;
        }
        private Tuple<string, CType, AST.Expression> struct_declarator(CType ctype) {
            Tuple<string, CType> decl = null;
            string ident = null;
            if (is_declarator()) {
                List<CType> stack = new List<CType>() { new CType.StubType() };
                declarator(ref ident, stack, 0);
                ctype = CType.resolv_type(ctype, stack);
            }
            AST.Expression expr = null;
            if (istoken(":")) {
                token(":");
                expr = constant_expression();
            }
            return Tuple.Create(ident, ctype, expr);

        }
        private CType enum_specifier() {
            token("enum");

            if (is_identifier()) {
                var ident = IDENTIFIER();
                var ctype = new CType.TaggedType.EnumType(ident, false);
                // ctype を tag に 登録
                tag_scope.Add(ident, ctype);
                if (istoken("{")) {
                    token("{");
                    ctype.enumerator_list = enumerator_list(ctype);
                    token("}");
                }
                return ctype;
            } else {
                var ident = $"$enum_{anony++}";
                var ctype = new CType.TaggedType.EnumType(ident, true);
                token("{");
                ctype.enumerator_list = enumerator_list(ctype);
                token("}");
                return ctype;
            }
        }
        private List<Tuple<string, int>> enumerator_list(CType.TaggedType.EnumType ctype) {
            var ret = new List<Tuple<string, int>>();
            var e = enumerator(ctype, 0);
            ident_scope.Add(e.Item1, new IdentifierValue.EnumValue(ctype, e.Item1));
            ret.Add(e);
            while (istoken(",")) {
                var i = e.Item2 + 1;
                token(",");
                e = enumerator(ctype, i);
                ident_scope.Add(e.Item1, new IdentifierValue.EnumValue(ctype, e.Item1));
                ret.Add(e);
            }
            return ret;
        }
        private Tuple<string, int> enumerator(CType ctype, int i) {
            var ident = IDENTIFIER();
            if (istoken("=")) {
                token("=");
                var expr = constant_expression();
                i = expr.Eval();
            }
            return Tuple.Create(ident, i);
        }
        private bool is_declarator() {
            return is_pointer() || is_direct_declarator();
        }
        private void declarator(ref string ident, List<CType> stack, int index) {
            if (is_pointer()) {
                pointer(stack, index);
            }
            direct_declarator(ref ident, stack, index);
        }

        private bool is_direct_declarator() {
            return istoken("(") || is_identifier();
        }
        private void direct_declarator(ref string ident, List<CType> stack, int index) {
            if (istoken("(")) {
                token("(");
                stack.Add(new CType.StubType());
                declarator(ref ident, stack, index + 1);
                token(")");
            } else {
                ident = IDENTIFIER();
            }
            more_direct_declarator(stack, index);
        }
        private void more_direct_declarator(List<CType> stack, int index) {
            if (istoken("[")) {
                token("[");
                // array
                int len = -1;
                if (istoken("]") == false) {
                    var expr = constant_expression();
                    len = expr.Eval();
                }
                token("]");
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_direct_declarator(stack, index);
            } else if (istoken("(")) {
                token("(");
                if (istoken(")")) {
                    // k&r or ANSI empty parameter list
                    token(")");
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                    more_direct_declarator(stack, index);
                } else if (is_identifier_list()) {
                    // K&R parameter name list
                    var args = identifier_list().Select(x => Tuple.Create(x, (CType)new CType.BasicType())).ToList();
                    token(")");
                    stack[index] = new CType.FunctionType(args, false, stack[index]);
                    more_direct_declarator(stack, index);
                } else {
                    // ANSI parameter list
                    bool vargs = false;
                    var args = parameter_type_list(ref vargs);
                    token(")");
                    stack[index] = new CType.FunctionType(args, vargs, stack[index]);
                    more_direct_declarator(stack, index);

                }
            } else {
                //_epsilon_
            }
        }

        private bool is_pointer() {
            return istoken("*");
        }

        private void pointer(List<CType> stack, int index) {
            token("*");
            stack[index] = new CType.PointerType(stack[index]);
            while (is_type_qualifier()) {
                stack[index].type_qualifier.Add(current_token());
            }
            pointer(stack, index);
        }
        private bool is_parameter_type_list() {
            return is_parameter_declaration();
        }
        private List<Tuple<string, CType>> parameter_type_list(ref bool vargs) {
            var items = new List<Tuple<string, CType>>();
            items.Add(parameter_declaration());
            while (istoken(",")) {
                token(",");
                if (istoken("...")) {
                    token("...");
                    vargs = true;
                    break;
                } else {
                    items.Add(parameter_declaration());
                }
            }
            return items;
        }
        public bool is_parameter_declaration() {
            return is_declaration_specifier();
        }
        private Tuple<string, CType> parameter_declaration() {
            CType baseType = new CType.BasicType();
            baseType = declaration_specifier(baseType);
            while (is_declaration_specifier()) {
                baseType = declaration_specifier(baseType);
            }
            if (is_declarator_or_abstract_declarator()) {
                string ident = "";
                List<CType> stack = new List<CType>();
                declarator_or_abstract_declarator(ref ident, stack, 0);
                var ctype = CType.resolv_type(baseType, stack);
                return Tuple.Create(ident, ctype);
            } else {
                return Tuple.Create((string)null, baseType);
            }

        }

        private bool is_declarator_or_abstract_declarator() {
            return is_pointer() || is_direct_declarator_or_direct_abstract_declarator();
        }
        private void declarator_or_abstract_declarator(ref string ident, List<CType> stack, int index) {
            if (is_pointer()) {
                pointer(stack, index);
                if (is_direct_declarator_or_direct_abstract_declarator()) {
                    direct_declarator_or_direct_abstract_declarator(ref ident, stack, index);
                }
            } else {
                direct_declarator_or_direct_abstract_declarator(ref ident, stack, index);
            }
        }

        private bool is_direct_declarator_or_direct_abstract_declarator() {
            return is_identifier() || istoken("(", "[");
        }

        private void direct_declarator_or_direct_abstract_declarator(ref string ident, List<CType> stack, int index) {
            if (is_identifier()) {
                ident = IDENTIFIER();
                more_dd_or_dad(stack, index);
            } else if (istoken("(")) {
                token("(");
                if (istoken(")")) {
                    // function?
                } else if (is_parameter_type_list()) {
                    // function 
                    // ANSI parameter list
                    bool vargs = false;
                    var args = parameter_type_list(ref vargs);
                } else {
                    stack.Add(new CType.StubType());
                    declarator_or_abstract_declarator(ref ident, stack, index + 1);
                }
                token(")");
                more_dd_or_dad(stack, index);
            } else if (istoken("[")) {
                token("[");
                int len = -1;
                if (istoken("[") == false) {
                    var expr = constant_expression();
                    len = expr.Eval();
                }
                token("]");
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_dd_or_dad(stack, index);
            } else {
                throw new Exception();
            }

        }
        private void more_dd_or_dad(List<CType> stack, int index) {
            if (istoken("(")) {
                token("(");
                if (istoken(")")) {
                    // function?
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                } else if (is_parameter_type_list()) {
                    // function 
                    // ANSI parameter list
                    bool vargs = false;
                    var args = parameter_type_list(ref vargs);
                    stack[index] = new CType.FunctionType(args, vargs, stack[index]);
                } else {
                    // K&R parameter name list
                    var args = identifier_list().Select(x => Tuple.Create(x, (CType)new CType.BasicType())).ToList();
                    stack[index] = new CType.FunctionType(args, false, stack[index]);
                }
                token(")");
                more_dd_or_dad(stack, index);
            } else if (istoken("[")) {
                token("[");
                int len = -1;
                if (istoken("[") == false) {
                    var expr = constant_expression();
                    len = expr.Eval();
                }
                token("]");
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_dd_or_dad(stack, index);
            } else {
                // _epsilon_
            }
        }

        private bool is_identifier_list() {
            return is_identifier();
        }

        private List<string> identifier_list() {
            var items = new List<string>();
            items.Add(IDENTIFIER());
            while (istoken(",")) {
                token(",");
                items.Add(IDENTIFIER());
            }
            return items;
        }


        private AST.Initializer initializer() {
            if (istoken("{")) {
                token("{");
                var ret = initializer_list();
                if (istoken(",")) {
                    token(",");
                }
                token("}");
                return new AST.Initializer.CompilxInitializer(ret);
            } else {
                return new AST.Initializer.SimpleInitializer(assignment_expression());
            }
        }
        private List<AST.Initializer> initializer_list() {
            var ret = new List<AST.Initializer>();
            ret.Add(initializer());
            while (istoken(",")) {
                token(",");
                ret.Add(initializer());
            }
            return ret;
        }

        private bool is_type_name() {
            return is_specifier_qualifier();
        }

        private CType type_name() {
            CType baseType = new CType.BasicType();
            baseType = specifier_qualifier(baseType);
            while (is_specifier_qualifier()) {
                baseType = specifier_qualifier(baseType);
            }
            if (is_abstract_declarator()) {
                List<CType> stack = new List<CType>() { new CType.StubType() };
                abstract_declarator(stack, 0);
                baseType = CType.resolv_type(baseType, stack);
            }
            return baseType;
        }
        private bool is_abstract_declarator() {
            return (is_pointer() || is_direct_abstract_declarator());
        }
        private void abstract_declarator(List<CType> stack, int index) {
            if (is_pointer()) {
                pointer(stack, index);
                if (is_direct_abstract_declarator()) {
                    direct_abstract_declarator(stack, index);
                }
            } else {
                direct_abstract_declarator(stack, index);
            }
        }
        private bool is_direct_abstract_declarator() {
            return istoken("(", "[");
        }

        private void direct_abstract_declarator(List<CType> stack, int index) {
            if (istoken("(")) {
                token("(");
                if (is_abstract_declarator()) {
                    stack.Add(new CType.StubType());
                    abstract_declarator(stack, index + 1);
                } else if (istoken(")") == false) {
                    // ansi args
                    bool vargs = false;
                    var items = parameter_type_list(ref vargs);
                } else {
                    // k&r or ansi
                }
                token(")");
                more_direct_abstract_declarator(stack, index);
            } else {
                token("[");
                int len = -1;
                if (istoken("]") == false) {
                    var expr = constant_expression();
                    len = expr.Eval();
                }
                token("]");
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_direct_abstract_declarator(stack, index);
            }
        }

        private void more_direct_abstract_declarator(List<CType> stack, int index) {
            if (istoken("[")) {
                token("[");
                int len = -1;
                if (istoken("]") == false) {
                    var expr = constant_expression();
                    len = expr.Eval();
                }
                token("]");
                stack[index] = new CType.ArrayType(len, stack[index]);
                more_direct_abstract_declarator(stack, index);
            } else if (istoken("(")) {
                token("(");
                if (istoken(")") == false) {
                    bool vargs = false;
                    var items = parameter_type_list(ref vargs);
                    stack[index] = new CType.FunctionType(items, vargs, stack[index]);
                } else {
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                }
                token(")");
                more_direct_abstract_declarator(stack, index);
            } else {
                // _epsilon_
            }
        }

        private string IDENTIFIER() {
            if (is_identifier() == false) {
                throw new Exception();
            }
            var ret = current_token();
            next_token();
            return ret;
        }


        private AST.Statement statement() {
            if ((is_identifier() && is_nexttoken(":")) || istoken("case", "default")) {
                return labeled_statement();
            } else if (istoken("{")) {
                return compound_statement();
            } else if (istoken("if", "switch")) {
                return selection_statement();
            } else if (istoken("while", "do", "for")) {
                return iteration_statement();
            } else if (istoken("goto", "continue", "break", "return")) {
                return jump_statement();
            } else {
                return expression_statement();
            }

        }

        private AST.Statement labeled_statement() {
            if (istoken("case")) {
                token("case");
                var expr = constant_expression();
                var stmt = statement();
                return new AST.Statement.CaseStatement(expr, stmt);
            } else if (istoken("default")) {
                token("default");
                var stmt = statement();
                return new AST.Statement.DefaultStatement(stmt);
            } else {
                var ident = IDENTIFIER();
                var stmt = statement();
                return new AST.Statement.GenericLabeledStatement(ident, stmt);
            }
        }
        private AST.Statement expression_statement() {
            AST.Statement ret;
            if (!istoken(";")) {
                var expr = expression();
                ret = new AST.Statement.ExpressionStatement(expr);
            } else {
                ret = new AST.Statement.EmptyStatement();
            }
            token(";");
            return ret;
        }
        private AST.Statement compound_statement() {
            tag_scope = tag_scope.Extend();
            ident_scope = ident_scope.Extend();
            token("{");
            var decls = new List<AST.Declaration>();
            while (is_declaration()) {
                decls.AddRange(declaration());
            }
            var stmts = new List<AST.Statement>();
            while (istoken("}") == false) {
                stmts.Add(statement());
            }
            token("}");
            var stmt = new AST.Statement.CompoundStatement(decls, stmts, tag_scope, ident_scope);
            ident_scope = ident_scope.Parent;
            tag_scope = tag_scope.Parent;
            return stmt;

        }
        private AST.Statement selection_statement() {
            if (istoken("if")) {
                token("if");
                token("(");
                var cond = expression();
                token(")");
                var then_stmt = statement();
                AST.Statement else_stmt = null;
                if (istoken("else")) {
                    token("else");
                    else_stmt = statement();
                }
                return new AST.Statement.IfStatement(cond, then_stmt, else_stmt);
            }
            if (istoken("switch")) {
                token("switch");
                token("(");
                var cond = expression();
                token(")");
                var ss = new AST.Statement.SwitchStatement(cond);
                break_scope = break_scope.Extend(ss);
                ss.statement = statement();
                break_scope = break_scope.Parent;
                return ss;
            }
            throw new Exception();
        }
        private AST.Statement iteration_statement() {
            if (istoken("while")) {
                token("while");
                token("(");
                var cond = expression();
                token(")");
                var ss = new AST.Statement.WhileStatement(cond);
                break_scope = break_scope.Extend(ss);
                continue_scope = continue_scope.Extend(ss);
                ss.statement = statement();
                break_scope = break_scope.Parent;
                continue_scope = continue_scope.Parent;
                return ss;
            }
            if (istoken("do")) {
                token("do");
                var ss = new AST.Statement.DoWhileStatement();
                break_scope = break_scope.Extend(ss);
                continue_scope = continue_scope.Extend(ss);
                ss.statement = statement();
                break_scope = break_scope.Parent;
                continue_scope = continue_scope.Parent;
                token("while");
                token("(");
                ss.cond = expression();
                token(")");
                token(";");
                return ss;
            }
            if (istoken("for")) {
                token("for");
                token("(");

                var init = istoken(";") ? (AST.Expression)null : expression();
                token(";");
                var cond = istoken(";") ? (AST.Expression)null : expression();
                token(";");
                var update = istoken(")") ? (AST.Expression)null : expression();
                token(")");
                var ss = new AST.Statement.ForStatement(init, cond, update);
                break_scope = break_scope.Extend(ss);
                continue_scope = continue_scope.Extend(ss);
                ss.statement = statement();
                break_scope = break_scope.Parent;
                continue_scope = continue_scope.Parent;
                return ss;
            }
            throw new Exception();

        }
        private AST.Statement jump_statement() {
            if (istoken("goto")) {
                token("goto");
                var label = IDENTIFIER();
                token(";");
                return new AST.Statement.GotoStatement(label);
            }
            if (istoken("continue")) {
                token("continue");
                token(";");
                return new AST.Statement.ContinueStatement(continue_scope.Value);
            }
            if (istoken("break")) {
                token("break");
                token(";");
                return new AST.Statement.BreakStatement(break_scope.Value);
            }
            if (istoken("return")) {
                token("return");
                var expr = istoken(";") ? null : expression();
                //現在の関数の戻り値と型チェック
                token(";");
                return new AST.Statement.ReturnStatement(expr);
            }
            throw new Exception();
        }
        private AST.Expression expression() {
            var e = assignment_expression();
            if (istoken(",")) {
                var ce = new AST.Expression.CommaExpression();
                ce.expressions.Add(e);
                while (istoken(",")) {
                    token(",");
                    e = assignment_expression();
                    ce.expressions.Add(e);
                }
                return ce;
            } else {
                return e;
            }
        }
        private AST.Expression assignment_expression() {
            var lhs = conditional_expression();
            if (is_assignment_operator()) {
                var op = assignment_operator();
                var rhs = assignment_expression();
                return new AST.Expression.AssignmentExpression(op, lhs, rhs);
            } else {
                return lhs;
            }

        }

        private bool is_assignment_operator() {
            return istoken("=", "*=", "/=", "%=", "+=", "-=", "<<=", ">>=", "&=", "^=", "|=");
        }

        private string assignment_operator() {
            if (is_assignment_operator() == false) {
                throw new Exception();
            }
            var ret = current_token();
            next_token();
            return ret;
        }
        private AST.Expression conditional_expression() {
            var cond = logical_OR_expression();
            if (istoken("?")) {
                token("?");
                var then_expr = expression();
                var else_expr = conditional_expression();
                return new AST.Expression.ConditionalExpression(cond, then_expr, else_expr);
            } else {
                return cond;
            }
        }
        private AST.Expression constant_expression() {
            // 定数式かどうか調べる必要がある
            return conditional_expression();

        }
        private AST.Expression logical_OR_expression() {
            var lhs = logical_AND_expression();
            if (istoken("||")) {
                token("||");
                var rhs = logical_AND_expression();
                return new AST.Expression.LogicalOrExpression(lhs, rhs);
            } else {
                return lhs;
            }
        }
        private AST.Expression logical_AND_expression() {
            var lhs = inclusive_OR_expression();
            if (istoken("&&")) {
                token("&&");
                var rhs = inclusive_OR_expression();
                return new AST.Expression.LogicalAndExpression(lhs, rhs);
            } else {
                return lhs;
            }
        }
        private AST.Expression inclusive_OR_expression() {
            var lhs = exclusive_OR_expression();
            if (istoken("|")) {
                token("|");
                var rhs = exclusive_OR_expression();
                return new AST.Expression.InclusiveOrExpression(lhs, rhs);
            } else {
                return lhs;
            }
        }
        private AST.Expression exclusive_OR_expression() {
            var lhs = and_expression();
            if (istoken("^")) {
                token("^");
                var rhs = and_expression();
                return new AST.Expression.ExclusiveOrExpression(lhs, rhs);
            } else {
                return lhs;
            }
        }
        private AST.Expression and_expression() {
            var lhs = equality_expression();
            if (istoken("&")) {
                token("&");
                var rhs = equality_expression();
                return new AST.Expression.AndExpression(lhs, rhs);
            } else {
                return lhs;
            }
        }
        private AST.Expression equality_expression() {
            var lhs = relational_expression();
            if (istoken("==", "!=")) {
                var op = current_token(); next_token();
                var rhs = relational_expression();
                return new AST.Expression.EqualityExpression(op, lhs, rhs);
            } else {
                return lhs;
            }
        }
        private AST.Expression relational_expression() {
            var lhs = shift_expression();
            if (istoken("<", ">", "<=", ">=")) {
                var op = current_token(); next_token();
                var rhs = shift_expression();
                return new AST.Expression.RelationalExpression(op, lhs, rhs);
            } else {
                return lhs;
            }
        }
        private AST.Expression shift_expression() {
            var lhs = additive_expression();
            if (istoken("<<", ">>")) {
                var op = current_token(); next_token();
                var rhs = additive_expression();
                return new AST.Expression.ShiftExpression(op, lhs, rhs);
            } else {
                return lhs;
            }

        }
        private AST.Expression additive_expression() {
            var lhs = multiplicitive_expression();
            if (istoken("+", "-")) {
                var op = current_token(); next_token();
                var rhs = multiplicitive_expression();
                return new AST.Expression.AdditiveExpression(op, lhs, rhs);
            } else {
                return lhs;
            }
        }
        private AST.Expression multiplicitive_expression() {
            var lhs = cast_expression();
            if (istoken("*", "/", "%")) {
                var op = current_token(); next_token();
                var rhs = cast_expression();
                return new AST.Expression.MultiplicitiveExpression(op, lhs, rhs);
            } else {
                return lhs;
            }
        }
        private AST.Expression cast_expression() {
            if (istoken("(")) {
                token("(");
                var ty = type_name();
                token(")");
                var expr = cast_expression();
                return new AST.Expression.CastExpression(ty, expr);
            } else {
                return unary_expression();
            }
        }

        private AST.Expression unary_expression() {
            if (istoken("++", "--")) {
                var op = current_token();
                next_token();
                var expr = unary_expression();
                return new AST.Expression.UnaryPrefixExpression(op, expr);
            }
            if (istoken("&")) {
                next_token();
                var expr = unary_expression();
                return new AST.Expression.UnaryAddressExpression(expr);
            }
            if (istoken("*")) {
                next_token();
                var expr = unary_expression();
                return new AST.Expression.UnaryReferenceExpression(expr);
            }
            if (istoken("+")) {
                next_token();
                var expr = unary_expression();
                return new AST.Expression.UnaryPlusExpression(expr);
            }
            if (istoken("-")) {
                next_token();
                var expr = unary_expression();
                return new AST.Expression.UnaryMinusExpression(expr);
            }
            if (istoken("~")) {
                next_token();
                var expr = unary_expression();
                return new AST.Expression.UnaryNegateExpression(expr);
            }
            if (istoken("!")) {
                next_token();
                var expr = unary_expression();
                return new AST.Expression.UnaryNotExpression(expr);
            }
            if (istoken("sizeof")) {
                next_token();
                if (istoken("(")) {
                    // どっちにも'('が出ることが出来るのでさらに先読みする（LL(2))
                    var saveCurrent = current;
                    token("(");
                    if (is_type_name()) {
                        var ty = type_name();
                        token(")");
                        return new AST.Expression.SizeofTypeExpression(ty);
                    } else {
                        current = saveCurrent;
                        var expr = unary_expression();
                        return new AST.Expression.SizeofExpression(expr);
                    }
                }
            }
            return postfix_expression();
        }

        private AST.Expression postfix_expression() {
            var expr = primary_expression();
            return more_postfix_expression(expr);

        }
        private AST.Expression more_postfix_expression(AST.Expression expr) {
            if (istoken("[")) {
                token("[");
                var index = expression();
                token("]");
                return more_postfix_expression(new AST.Expression.ArrayIndexExpression(expr, index));
            }
            if (istoken("(")) {
                token("(");
                List<AST.Expression> args = null;
                if (istoken(")") == false) {
                    args = argument_expression_list();
                } else {
                    args = new List<AST.Expression>();
                }
                token(")");
                return more_postfix_expression(new AST.Expression.FunctionCallExpression(expr, args));
            }
            if (istoken(".")) {
                token(".");
                var ident = IDENTIFIER();
                return more_postfix_expression(new AST.Expression.MemberDirectAccess(expr, ident));
            }
            if (istoken("->")) {
                token("->");
                var ident = IDENTIFIER();
                return more_postfix_expression(new AST.Expression.MemberIndirectAccess(expr, ident));
            }
            if (istoken("++", "--")) {
                var op = current_token();
                next_token();
                return more_postfix_expression(new AST.Expression.UnaryPostfixExpression(op, expr));
            }
            return expr;
        }
        private AST.Expression primary_expression() {
            if (is_identifier()) {
                var ident = IDENTIFIER();
                return new AST.Expression.IdentifierExpression(ident);
            }
            if (is_constant()) {
                return constant();
            }
            if (is_STRING()) {
                List<string> strings = new List<string>();
                while (is_STRING()) {
                    strings.Add(STRING());
                }
                return new AST.Expression.StringExpression(strings);
            }
            if (istoken("(")) {
                token("(");
                var expr = expression();
                token(")");
                return expr;
            }
            throw new Exception();
        }
        private List<AST.Expression> argument_expression_list() {
            var ret = new List<AST.Expression>();
            ret.Add(assignment_expression());
            while (istoken(",")) {
                token(",");
                ret.Add(assignment_expression());
            }
            return ret;
        }

        private bool is_constant() {
            return is_INTEGER_CONSTANT() ||
                   is_CHARACTER_CONSTANT() ||
                   is_FLOATING_CONSTANT() ||
                   is_ENUMERATION_CONSTANT();
        }

        private AST.Expression constant() {
            if (is_INTEGER_CONSTANT()) {
                var ret = INTEGER_CONSTANT();
                return new AST.Expression.IntegerConstant(ret);
            }
            if (is_CHARACTER_CONSTANT()) {
                var ret = CHARACTER_CONSTANT();
                return new AST.Expression.CharacterConstant(ret);
            }
            if (is_FLOATING_CONSTANT()) {
                var ret = FLOATING_CONSTANT();
                return new AST.Expression.FloatingConstant(ret);
            }
            if (is_ENUMERATION_CONSTANT()) {
                var ret = ENUMERATION_CONSTANT();
                return new AST.Expression.EnumerationConstant(ret);
            }
            throw new Exception();
        }

    }

}

