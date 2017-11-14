using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

namespace CParser2 {

    public class StringWriteVisitor {
        public StringWriteVisitor() {

        }
        public string Write(SyntaxNode node) {
            return node.Accept<string>(this);
        }

        public string Visit(SyntaxNode.Expression.UnaryExpression.PrefixIncrementExpression self) {
            return $"(++{ self.operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.UnaryExpression.PrefixDecrementExpression self) {
            return $"(--{ self.operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.UnaryExpression.AddressExpression self) {
            return $"(&{ self.operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.UnaryExpression.IndirectionExpression self) {
            return $"(*{ self.operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression self) {
            return $"{self.@operator.ToCString()}({ self.operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.UnaryExpression.SizeofExpression self) {
            return $"sizeof({ self.operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.UnaryExpression.SizeofTypeExpression self) {
            return $"sizeof({ self.operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.PrimaryExpression.ObjectSpecifier self) {
            return self.identifier;
        }
        public string Visit(SyntaxNode.Expression.PrimaryExpression.ConstantSpecifier self) {
            return self.constant;
        }
        public string Visit(SyntaxNode.Expression.PrimaryExpression.StringLiteralSpecifier self) {
            return String.Join(" ", self.literal);
        }
        public string Visit(SyntaxNode.Expression.PrimaryExpression.GroupedExpression self) {
            return $"({ self.expression.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.PostfixExpression.ArraySubscriptExpression self) {
            return $"{ self.expression.Accept<string>(this)}[{self.array_subscript.Accept<string>(this)}]";
        }
        public string Visit(SyntaxNode.Expression.PostfixExpression.FunctionCallExpression self) {
            return $"{ self.expression.Accept<string>(this)}({string.Join(", ", self.argument_expression_list.Select(x => x.Accept<string>(this)))})";
        }
        public string Visit(SyntaxNode.Expression.PostfixExpression.MemberAccessByValueExpression self) {
            return $"{ self.expression.Accept<string>(this)}.{self.identifier}";
        }
        public string Visit(SyntaxNode.Expression.PostfixExpression.MemberAccessByPointerExpression self) {
            return $"{ self.expression.Accept<string>(this)}->{self.identifier}";
        }
        public string Visit(SyntaxNode.Expression.PostfixExpression.PostfixIncrementExpression self) {
            return $"({ self.operand.Accept<string>(this)}++)";
        }
        public string Visit(SyntaxNode.Expression.PostfixExpression.PostfixDecrementExpression self) {
            return $"({ self.operand.Accept<string>(this)}--)";
        }
        public string Visit(SyntaxNode.Expression.PostfixExpression.CompoundLiteralExpression self) {
            return $"({self.type_name.Accept<string>(this)}) {{{Environment.NewLine + string.Join("," + Environment.NewLine, self.initializers.Select(x => ($"{x.Item1.Accept<string>(this)} = {x.Item2.Accept<string>(this)}"))) + Environment.NewLine }}})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.@operator} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.SimpleAssignmentExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} = { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.LogicalOrExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} || { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.LogicalAndExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} && { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.InclusiveOrExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} | { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.ExclusiveOrExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} ^ { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.AndExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} & { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.EqualityExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.@operator.ToCString()} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.RelationalExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.@operator.ToCString()} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.ShiftExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.@operator.ToCString()} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.AdditiveExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.@operator.ToCString()} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.MultiplicativeExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.@operator.ToCString()} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.ConditionalExpression self) {
            return $"({ self.condition.Accept<string>(this)} ? {self.then_expression.Accept<string>(this)} : { self.else_expression.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.PostfixExpression.CommaSeparatedExpression self) {
            return $"({string.Join(", ", self.expressions.Select(x => $"{x.Accept<string>(this)}"))})";
        }
        public string Visit(SyntaxNode.Expression.PostfixExpression.CastExpression self) {
            var cast_type = self.type_name.Accept<string>(this);
            return $"(({cast_type}){ self.operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.PostfixExpression.ErrorExpression self) {
            return $"/* {self.statement.Accept<string>(this)} */";
        }
        public string Visit(SyntaxNode.Declaration self) {
            var items = new List<string>();
            if (self.items != null) {
                items.AddRange(self.items.Select(x => x.Accept<string>(this)));
            }
            return String.Join(Environment.NewLine, items.Select(x => x));
        }
        public string Visit(SyntaxNode.FunctionDeclaration self) {
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.init_declarator.Accept<string>(this)};";
        }
        public string Visit(SyntaxNode.VariableDeclaration self) {
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.declarator.Accept<string>(this)};";
        }
        public string Visit(SyntaxNode.Definition.FunctionDefinition.KandRFunctionDefinition self) {
            var pars = (self.parameterDefinition != null) ? Environment.NewLine + string.Concat(self.parameterDefinition.Select(x => x.Accept<string>(this) + ";" + Environment.NewLine)) : "";
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.declarator.Accept<string>(this)}{pars}{self.function_body.Accept<string>(this)}";
        }
        public string Visit(SyntaxNode.Definition.FunctionDefinition.AnsiFunctionDefinition self) {
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.declarator.Accept<string>(this)} {self.function_body.Accept<string>(this)}";
        }
        public string Visit(SyntaxNode.Definition.VariableDefinition self) {
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.init_declarator.Accept<string>(this)};";
        }
        public string Visit(SyntaxNode.Definition.ParameterDefinition self) {
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.declarator.Accept<string>(this)}";
        }
        public string Visit(SyntaxNode.TypeDeclaration.TypedefDeclaration self) {
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.init_declarator.Accept<string>(this)};";
        }

        HashSet<SyntaxNode> outputed = new HashSet<SyntaxNode>();

        public string Visit(SyntaxNode.TypeDeclaration.StructTypeDeclaration self) {
            var ss = self.struct_specifier;
            if (ss.struct_declarations != null && !outputed.Contains(self)) {
                outputed.Add(self);
                var items = ss.struct_declarations.Select(x => x.Accept<string>(this)).ToList();
                return $"struct {self.identifier} {{" + Environment.NewLine + string.Concat(items.Select(x => x + Environment.NewLine)) + "};";
            } else {
                return $"struct {self.identifier};";
            }
        }
        public string Visit(SyntaxNode.TypeDeclaration.UnionTypeDeclaration self) {
            var ss = self.union_specifier;
            if (ss.struct_declarations != null && !outputed.Contains(self)) {
                outputed.Add(self);
                var items = ss.struct_declarations.Select(x => x.Accept<string>(this)).ToList();
                return $"union {self.identifier} {{" + Environment.NewLine + string.Concat(items.Select(x => x + Environment.NewLine)) + "};";
            } else {
                return $"union {self.identifier};";
            }
        }
        public string Visit(SyntaxNode.TypeDeclaration.EnumTypeDeclaration self) {
            var ss = self.enum_specifier;
            if (ss.enumerators != null && !outputed.Contains(self)) {
                outputed.Add(self);
                var items = ss.enumerators.Select(x => x.Accept<string>(this)).ToList();
                return $"enum {self.identifier} {{" + Environment.NewLine + string.Join("," + Environment.NewLine, items) + Environment.NewLine + "};";
            } else {
                return $"enum {self.identifier};";
            }
        }
        public string Visit(SyntaxNode.DeclarationSpecifiers self) {
            List<string> specs = new List<string>();
            if (self.storage_class_specifier != SyntaxNode.StorageClassSpecifierKind.none) {
                specs.Add(self.storage_class_specifier.ToCString());
            }
            if (self.type_qualifiers != null) {
                specs.AddRange(self.type_qualifiers.Select(x => x.ToCString()));
            }
            if (self.type_specifiers != null) {
                specs.AddRange(self.type_specifiers.Select(x => x.Accept<string>(this)));
            }
            if (self.function_specifier != SyntaxNode.DeclarationSpecifiers.FuntionSpecifierKind.none) {
                specs.Add(self.function_specifier.ToCString());
            }
            return String.Join(" ", specs.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        public string Visit(SyntaxNode.TypeDeclaration.InitDeclarator self) {
            return $"{self.declarator.Accept<string>(this)}" + (self.initializer != null ? " = " + self.initializer.Accept<string>(this) : "");
        }
        public string Visit(SyntaxNode.TypeDeclaration.TypeSpecifier.StructSpecifier self) {
            return $"struct {self.identifier}";
        }
        public string Visit(SyntaxNode.TypeDeclaration.TypeSpecifier.UnionSpecifier self) {
            return $"union {self.identifier}";
        }
        public string Visit(SyntaxNode.TypeDeclaration.TypeSpecifier.StandardTypeSpecifier self) {
            return $"{self.Kind.ToCString()}";
        }
        public string Visit(SyntaxNode.TypeDeclaration.TypeSpecifier.TypedefTypeSpecifier self) {
            return $"{self.identifier}";
        }
        public string Visit(SyntaxNode.StructDeclaration self) {
            var items = new List<string>();
            if (self.items != null) {
                items.AddRange(self.items.Select(x => x.Accept<string>(this) + ";"));
            }
            return String.Join(Environment.NewLine, items);
        }
        public string Visit(SyntaxNode.MemberDeclaration self) {
            return $"{ self.specifier_qualifier_list.Accept<string>(this)} { self.struct_declarator.Accept<string>(this)}";
        }
        public string Visit(SyntaxNode.SpecifierQualifierList self) {
            var items = new List<string>();
            if (self.type_qualifiers != null) {
                items.AddRange(self.type_qualifiers.Select(x => x.ToCString()));
            }
            if (self.type_specifiers != null) {
                items.AddRange(self.type_specifiers.Select(x => x.Accept<string>(this)));
            }
            return String.Join(" ", items);
        }
        public string Visit(SyntaxNode.StructDeclarator self) {
            if (self.bitfield_expr != null) {
                return $"{self.declarator.Accept<string>(this)} : {self.bitfield_expr.Accept<string>(this)}";
            } else {
                return $"{self.declarator.Accept<string>(this)}";
            }
        }
        public string Visit(SyntaxNode.TypeSpecifier.EnumSpecifier self) {
            string items = "";
            if (self.enumerators != null) {
                items = " {" + Environment.NewLine + String.Concat(self.enumerators.Select(x => x.Accept<string>(this) + "," + Environment.NewLine)) + "}";
            }
            return $"enum {self.identifier}{items};";
        }
        public string Visit(SyntaxNode.TypeSpecifier.EnumSpecifier.Enumerator self) {
            return self.identifier + ((self.expression != null) ? " = " + self.expression.Accept<string>(this) : "");
        }
        public string Visit(SyntaxNode.Declarator.GroupedDeclarator self) {
            return $"({self.@base.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Declarator.IdentifierDeclarator self) {
            var ptr = (self.pointer) != null ? (String.Join(" ", self.pointer.Select(x => x.ToCString())) + " ") : "";
            return ptr + self.identifier;
        }
        public string Visit(SyntaxNode.Declarator.ArrayDeclarator self) {
            var ptr = (self.pointer == null) ? "" : (String.Join(" ", self.pointer.Select(x => x.ToCString())) + " ");
            var sz = (self.size_expression == null) ? "" : self.size_expression.Accept<string>(this);
            return $"{ptr}{self.@base.Accept<string>(this)}[{sz}]";
        }
        public string Visit(SyntaxNode.Declarator.FunctionDeclarator.AbbreviatedFunctionDeclarator self) {
            return $"{self.@base.Accept<string>(this)}()";
        }
        public string Visit(SyntaxNode.Declarator.FunctionDeclarator.KandRFunctionDeclarator self) {
            var pars = (self.identifier_list != null) ? string.Join(", ", self.identifier_list) : "";
            return $"{self.@base.Accept<string>(this)}({pars})";
        }
        public string Visit(SyntaxNode.Declarator.FunctionDeclarator.AnsiFunctionDeclarator self) {
            return $"{self.@base.Accept<string>(this)}{self.parameter_type_list.Accept<string>(this)}";
        }
        public string Visit(SyntaxNode.Declarator.AbstractDeclarator.FunctionAbstractDeclarator self) {
            return $"{self.@base.Accept<string>(this)}({self.parameter_type_list.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator self) {
            return $"{self.@base.Accept<string>(this)}[{self.size_expression.Accept<string>(this)}]";
        }
        public string Visit(SyntaxNode.Declarator.AbstractDeclarator.PointerAbstractDeclarator self) {
            var parts = new List<string>();
            if (self.pointer != null) {
                parts.AddRange(self.pointer.Select(x => x.ToCString()));
            }
            if (self.@base != null) {
                parts.Add(self.@base.Accept<string>(this));
            }
            return string.Join(" ", parts);
        }
        public string Visit(SyntaxNode.Declarator.AbstractDeclarator.GroupedAbstractDeclarator self) {
            string p = (self.pointer != null) ? (String.Concat(self.pointer.Select(x => x.ToCString()))) : "";
            string b = self.@base != null ? self.@base.Accept<string>(this) : "";
            return $"({b}{p})";
        }
        public string Visit(SyntaxNode.ParameterTypeList self) {
            return "(" + (self.parameters != null ? String.Join(", ", self.parameters.Select(x => x.Accept<string>(this))) + (self.have_va_list ? ", ..." : "") : "") + ")";
        }
        public string Visit(SyntaxNode.ParameterDeclaration self) {
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.declarator.Accept<string>(this)}";
        }
        public string Visit(SyntaxNode.Statement.ErrorStatement self) {
            return $"";
        }
        public string Visit(SyntaxNode.Statement.LabeledStatement.DefaultLabeledStatement self) {
            return $"default:" + Environment.NewLine + self.statement.Accept<string>(this);
        }
        public string Visit(SyntaxNode.Statement.LabeledStatement.CaseLabeledStatement self) {
            return $"case {self.expression.Accept<string>(this)}:" + Environment.NewLine + self.statement.Accept<string>(this);
        }
        public string Visit(SyntaxNode.Statement.LabeledStatement.GenericLabeledStatement self) {
            return $"{self.label}:" + Environment.NewLine + self.statement.Accept<string>(this);
        }
        public string Visit(SyntaxNode.Statement.CompoundStatement self) {
            var items = new List<string>();
            items.Add("{");
            if (self.block_items != null) {
                items.AddRange(self.block_items.Select(x => x.Accept<string>(this)));
            }
            items.Add("}");
            return String.Join(Environment.NewLine, items);
        }
        public string Visit(SyntaxNode.Statement.ExpressionStatement self) {
            return self.expression.Accept<string>(this) + ";";
        }
        public string Visit(SyntaxNode.Statement.SelectionStatement.IfStatement self) {
            var then_side = (self.then_statement != null) ? $" {self.then_statement.Accept<string>(this)}" : "";
            var else_side = (self.else_statement != null) ? $" else {self.else_statement.Accept<string>(this)}" : "";
            return $"if ({self.expression.Accept<string>(this)}) {then_side}{else_side}";
        }
        public string Visit(SyntaxNode.Statement.SelectionStatement.SwitchStatement self) {
            return $"switch ({self.expression.Accept<string>(this)}) {self.statement.Accept<string>(this)}";
        }
        public string Visit(SyntaxNode.Statement.IterationStatement.C99ForStatement self) {
            return $"for ({self.declaration.Accept<string>(this)}; {self.condition_statement.Accept<string>(this)} {self.expression.Accept<string>(this)}) {self.body_statement.Accept<string>(this)}";
        }
        public string Visit(SyntaxNode.Statement.IterationStatement.ForStatement self) {
            return $"for ({self.initial_statement.Accept<string>(this)} {self.condition_statement.Accept<string>(this)} {self.expression.Accept<string>(this)}) {self.body_statement.Accept<string>(this)}";
        }
        public string Visit(SyntaxNode.Statement.IterationStatement.DoStatement self) {
            return $"do {self.statement.Accept<string>(this)} while ({self.expression.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Statement.IterationStatement.WhileStatement self) {
            return $"while ({self.expression.Accept<string>(this)}) {self.statement.Accept<string>(this)}";
        }
        public string Visit(SyntaxNode.Statement.JumpStatement.ReturnStatement self) {
            return "return" + (self.expression != null ? (" " + self.expression.Accept<string>(this)) : "") + ";";
        }
        public string Visit(SyntaxNode.Statement.JumpStatement.BreakStatement self) {
            return "break;";
        }
        public string Visit(SyntaxNode.Statement.JumpStatement.ContinueStatement self) {
            return "continue;";
        }
        public string Visit(SyntaxNode.Statement.JumpStatement.GotoStatement self) {
            return $"goto {self.identifier};";
        }
        public string Visit(SyntaxNode.TranslationUnit self) {
            return self.external_declarations != null ? String.Join(Environment.NewLine, self.external_declarations.Select(x => x.Accept<string>(this))) : "";
        }
        public string Visit(SyntaxNode.TypeName self) {
            var parts = new List<string>();
            if (self.specifier_qualifier_list != null) {
                parts.Add(self.specifier_qualifier_list.Accept<string>(this));
            }
            if (self.type_declaration != null) {
                parts.Add(self.type_declaration.Accept<string>(this));
            }
            if (self.abstract_declarator != null) {
                parts.Add(self.abstract_declarator.Accept<string>(this));
            }

            return string.Join(" ", parts);
        }
        public string Visit(SyntaxNode.Initializer self) {
            var inits = new List<string>();
            if (self.initializers != null) {
                inits.AddRange(self.initializers.Select(x => (x.Item1 != null ? x.Item1.Accept<string>(this) + " = " : "") + x.Item2.Accept<string>(this)));
                return inits.Any() ? "{" + Environment.NewLine + String.Join("," + Environment.NewLine, inits) + Environment.NewLine + "}" : "{}";
            } else if (self.expression != null) {
                return self.expression.Accept<string>(this);
            } else {
                throw new Exception("Either initializer or expression must be specified.");
            }
        }
        public string Visit(SyntaxNode.Initializer.Designator.MemberDesignator self) {
            return "." + self.identifier;
        }
        public string Visit(SyntaxNode.Initializer.Designator.IndexDesignator self) {
            return $"[{self.expression.Accept<string>(this)}]";
        }

    }

    public abstract class SyntaxNode {

        /// <summary>
        /// 式
        /// </summary>
        public abstract class Expression : SyntaxNode {

            public bool full {
                get; set;
            }

            /// <summary>
            /// 単項演算子式
            /// </summary>
            public abstract class UnaryExpression : Expression {

                /// <summary>
                /// 前置インクリメント演算子式
                /// </summary>
                public class PrefixIncrementExpression : UnaryExpression {

                    public Expression operand {
                        get;
                    }

                    public PrefixIncrementExpression(Expression operand) {

                        this.operand = operand;
                    }
                }

                /// <summary>
                /// 前置デクリメント演算子式
                /// </summary>
                public class PrefixDecrementExpression : UnaryExpression {

                    public Expression operand {
                        get;
                    }

                    public PrefixDecrementExpression(Expression operand) {

                        this.operand = operand;
                    }
                }

                /// <summary>
                /// 参照演算子式
                /// </summary>
                public class AddressExpression : UnaryExpression {

                    public Expression operand {
                        get;
                    }


                    public AddressExpression(Expression operand) {

                        this.operand = operand;
                    }
                }

                /// <summary>
                /// 間接参照演算子式
                /// </summary>
                public class IndirectionExpression : UnaryExpression {

                    public Expression operand {
                        get;
                    }


                    public IndirectionExpression(Expression operand) {

                        this.operand = operand;
                    }
                }

                /// <summary>
                /// 単項算術演算子式
                /// </summary>
                public class UnaryArithmeticExpression : UnaryExpression {

                    public enum OperatorKind {
                        Add,
                        Subtract,
                        Inverse,
                        Negate
                    }

                    public OperatorKind @operator {
                        get;
                    }

                    public Expression operand {
                        get;
                    }

                    public UnaryArithmeticExpression(OperatorKind @operator, Expression operand) {

                        this.@operator = @operator;
                        this.operand = operand;
                    }
                }

                /// <summary>
                /// 式に対するsizeof演算子式
                /// </summary>
                public class SizeofExpression : UnaryExpression {

                    public Expression operand {
                        get;
                    }

                    public SizeofExpression(Expression operand) {

                        this.operand = operand;
                    }
                }

                /// <summary>
                /// 型に対するsizeof演算子式
                /// </summary>
                public class SizeofTypeExpression : UnaryExpression {

                    public TypeName operand {
                        get;
                    }

                    public SizeofTypeExpression(TypeName operand) {

                        this.operand = operand;
                    }
                }

            }

            /// <summary>
            /// 一次式
            /// </summary>
            public abstract class PrimaryExpression : Expression {

                /// <summary>
                /// オブジェクト指定子(識別子要素)
                /// </summary>
                public class ObjectSpecifier : PrimaryExpression {

                    public string identifier {
                        get;
                    }

                    public ObjectSpecifier(string identifier) {

                        this.identifier = identifier;
                    }
                }

                /// <summary>
                /// 定数指定子
                /// </summary>
                public class ConstantSpecifier : PrimaryExpression {

                    public string constant {
                        get;
                    }

                    public ConstantSpecifier(string constant) {

                        this.constant = constant;
                    }
                }

                /// <summary>
                /// 文字列リテラル指定子
                /// </summary>
                public class StringLiteralSpecifier : PrimaryExpression {

                    public IReadOnlyList<string> literal {
                        get;
                    }

                    public StringLiteralSpecifier(IReadOnlyList<string> literal) {

                        this.literal = literal;
                    }
                }

                /// <summary>
                /// グループ化式
                /// </summary>
                public class GroupedExpression : PrimaryExpression {

                    public Expression expression {
                        get;
                    }

                    public GroupedExpression(Expression expression) {

                        this.expression = expression;
                    }
                }

            }

            /// <summary>
            /// 後置式
            /// </summary>
            public abstract class PostfixExpression : Expression {

                /// <summary>
                /// 配列添字式
                /// </summary>
                public class ArraySubscriptExpression : PostfixExpression {

                    /// <summary>
                    /// 左辺式
                    /// </summary>
                    public Expression expression {
                        get;
                    }

                    /// <summary>
                    /// 添字式
                    /// </summary>
                    public Expression array_subscript {
                        get;
                    }

                    public ArraySubscriptExpression(Expression expression, Expression arraySubscript) {

                        this.expression = expression;
                        array_subscript = arraySubscript;
                    }
                }

                /// <summary>
                /// 関数呼び出し式
                /// </summary>
                public class FunctionCallExpression : PostfixExpression {

                    /// <summary>
                    /// 引数式列
                    /// </summary>
                    public IReadOnlyList<Expression> argument_expression_list {
                        get;
                    }

                    /// <summary>
                    /// 左辺式
                    /// </summary>
                    public Expression expression {
                        get;
                    }

                    public FunctionCallExpression(Expression expression, IReadOnlyList<Expression> argumentExpressionList) {

                        this.expression = expression;
                        argument_expression_list = argumentExpressionList;
                    }
                }

                /// <summary>
                /// メンバーアクセス演算子
                /// </summary>
                public class MemberAccessByValueExpression : PostfixExpression {

                    /// <summary>
                    /// 左辺式
                    /// </summary>
                    public Expression expression {
                        get;
                    }

                    /// <summary>
                    /// メンバー名
                    /// </summary>
                    public string identifier {
                        get;
                    }

                    public MemberAccessByValueExpression(Expression expression, string identifier) {

                        this.expression = expression;
                        this.identifier = identifier;
                    }
                }

                /// <summary>
                /// メンバー間接参照アクセス演算子
                /// </summary>
                public class MemberAccessByPointerExpression : PostfixExpression {

                    /// <summary>
                    /// 左辺式
                    /// </summary>
                    public Expression expression {
                        get;
                    }

                    /// <summary>
                    /// メンバー名
                    /// </summary>
                    public string identifier {
                        get;
                    }

                    public MemberAccessByPointerExpression(Expression expression, string identifier) {

                        this.expression = expression;
                        this.identifier = identifier;
                    }
                }

                /// <summary>
                /// 後置インクリメント演算子
                /// </summary>
                public class PostfixIncrementExpression : PostfixExpression {

                    public Expression operand {
                        get;
                    }

                    public PostfixIncrementExpression(Expression operand) {

                        this.operand = operand;
                    }
                }

                /// <summary>
                /// 後置デクリメント演算子
                /// </summary>
                public class PostfixDecrementExpression : PostfixExpression {

                    public Expression operand {
                        get;
                    }

                    public PostfixDecrementExpression(Expression x) {

                        operand = x;
                    }
                }

                /// <summary>
                /// 複合リテラル式
                /// </summary>
                public class CompoundLiteralExpression : PostfixExpression {

                    /// <summary>
                    /// 型名
                    /// </summary>
                    public TypeName type_name {
                        get;
                    }

                    /// <summary>
                    /// 初期化式
                    /// </summary>
                    public IReadOnlyList<Tuple<IReadOnlyList<SyntaxNode.Initializer.Designator>, SyntaxNode.Initializer>> initializers {
                        get;
                    }

                    public CompoundLiteralExpression(TypeName typeName, IReadOnlyList<Tuple<IReadOnlyList<SyntaxNode.Initializer.Designator>, SyntaxNode.Initializer>> initializers) {

                        this.type_name = typeName;
                        this.initializers = initializers;
                    }
                }

            }

            /// <summary>
            /// 二項演算子式
            /// </summary>
            public abstract class BinaryExpression : Expression {

                public Expression lhs_operand {
                    get;
                }
                public Expression rhs_operand {
                    get;
                }

                protected BinaryExpression(Expression lhs_operand, Expression rhs_operand) {
                    this.lhs_operand = lhs_operand;
                    this.rhs_operand = rhs_operand;
                }

                /// <summary>
                /// 複合代入式
                /// </summary>
                public class CompoundAssignmentExpression : BinaryExpression {
                    public enum OperatorKind {
                        multiply_assign, divide_assign, modulus_assign, add_assign, subtract_assign,
                        left_shift_assign, right_shift_assign, binary_and_assign, binary_or_assign, xor_assign
                    }

                    public OperatorKind @operator {
                        get;
                    }

                    public CompoundAssignmentExpression(OperatorKind op, Expression lhs_operand, Expression rhs_operand) : base(lhs_operand, rhs_operand) {
                        this.@operator = op;
                    }
                }

                /// <summary>
                /// 単純代入式
                /// </summary>
                public class SimpleAssignmentExpression : BinaryExpression {

                    public SimpleAssignmentExpression(Expression lhs_operand, Expression rhs_operand) : base(lhs_operand, rhs_operand) {
                    }
                }

                /// <summary>
                /// 論理 OR 演算子式
                /// </summary>
                public class LogicalOrExpression : BinaryExpression {

                    public LogicalOrExpression(Expression lhs_operand, Expression rhs_operand) : base(lhs_operand, rhs_operand) {
                    }
                }

                /// <summary>
                /// 論理 AND 演算子式
                /// </summary>
                public class LogicalAndExpression : BinaryExpression {

                    public LogicalAndExpression(Expression lhs_operand, Expression rhs_operand) : base(lhs_operand, rhs_operand) {
                    }
                }

                /// <summary>
                /// ビットごとの包括的 OR 演算子式
                /// </summary>
                public class InclusiveOrExpression : BinaryExpression {

                    public InclusiveOrExpression(Expression lhs_operand, Expression rhs_operand) : base(lhs_operand, rhs_operand) {
                    }
                }

                /// <summary>
                /// ビットごとの排他的 OR 演算子式
                /// </summary>
                public class ExclusiveOrExpression : BinaryExpression {

                    public ExclusiveOrExpression(Expression lhs_operand, Expression rhs_operand) : base(lhs_operand, rhs_operand) {
                    }
                }

                /// <summary>
                /// ビットごとの AND 演算子式
                /// </summary>
                public class AndExpression : BinaryExpression {

                    public AndExpression(Expression lhs_operand, Expression rhs_operand) : base(lhs_operand, rhs_operand) {
                    }
                }

                /// <summary>
                /// 等値演算子式
                /// </summary>
                public class EqualityExpression : BinaryExpression {
                    public enum OperatorKind {
                        equal, not_equal
                    }

                    public OperatorKind @operator {
                        get;
                    }

                    public EqualityExpression(OperatorKind op, Expression lhs_operand, Expression rhs_operand) : base(lhs_operand, rhs_operand) {
                        this.@operator = op;
                    }
                }

                /// <summary>
                /// 関係演算子式
                /// </summary>
                public class RelationalExpression : BinaryExpression {

                    public enum OperatorKind {
                        less_equal, less, greater_equal, greater
                    }

                    public OperatorKind @operator {
                        get;
                    }

                    public RelationalExpression(OperatorKind op, Expression lhs_operand, Expression rhs_operand) : base(lhs_operand, rhs_operand) {
                        this.@operator = op;
                    }
                }

                /// <summary>
                /// ビット処理シフト演算子
                /// </summary>
                public class ShiftExpression : BinaryExpression {
                    public enum OperatorKind {
                        left_shift, right_shift
                    }
                    public OperatorKind @operator {
                        get;
                    }

                    public ShiftExpression(OperatorKind op, Expression lhs_operand, Expression rhs_operand) : base(lhs_operand, rhs_operand) {
                        this.@operator = op;
                    }
                }

                /// <summary>
                /// 加法演算子式
                /// </summary>
                public class AdditiveExpression : BinaryExpression {
                    public enum OperatorKind {
                        add, subtract
                    }

                    public OperatorKind @operator {
                        get;
                    }

                    public AdditiveExpression(OperatorKind op, Expression lhs_operand, Expression rhs_operand) : base(lhs_operand, rhs_operand) {
                        this.@operator = op;
                    }
                }

                /// <summary>
                /// 乗算演算子式
                /// </summary>
                public class MultiplicativeExpression : BinaryExpression {
                    public enum OperatorKind {
                        multiply, divide, modulus
                    }

                    public OperatorKind @operator {
                        get;
                    }

                    public MultiplicativeExpression(OperatorKind op, Expression lhs_operand, Expression rhs_operand) : base(lhs_operand, rhs_operand) {
                        this.@operator = op;
                    }
                }
            }

            /// <summary>
            /// 条件式演算子式
            /// </summary>
            public class ConditionalExpression : Expression {

                /// <summary>
                /// 条件式
                /// </summary>
                public Expression condition {
                    get;
                }

                public Expression then_expression {
                    get;
                }

                public Expression else_expression {
                    get;
                }

                public ConditionalExpression(Expression condition, Expression thenExpression, Expression elseExpression) {

                    this.condition = condition;
                    this.then_expression = thenExpression;
                    this.else_expression = elseExpression;
                }
            }

            /// <summary>
            /// 順次評価演算子式
            /// </summary>
            public class CommaSeparatedExpression : Expression {

                public IReadOnlyList<Expression> expressions {
                    get;
                }

                public CommaSeparatedExpression(IReadOnlyList<Expression> expressions) {

                    this.expressions = expressions;
                }
            }

            /// <summary>
            /// キャスト演算子式
            /// </summary>
            public class CastExpression : Expression {

                /// <summary>
                /// キャスト対象の式
                /// </summary>
                public Expression operand {
                    get;
                }

                /// <summary>
                /// キャスト先の型
                /// </summary>
                public TypeName type_name {
                    get;
                }

                public CastExpression(TypeName typeName, Expression operand) {

                    this.type_name = typeName;
                    this.operand = operand;
                }
            }

            /// <summary>
            /// エラー式（構文解析器内部専用）
            /// </summary>
            public class ErrorExpression : Expression {

                public Statement statement {
                    get;
                }

                public ErrorExpression(Statement x) {
                    this.statement = x;
                }
            }
        }


        /// <summary>
        /// ストレージクラスの種別
        /// </summary>
        public enum StorageClassSpecifierKind {
            none,
            typedef_keyword,
            extern_keyword,
            static_keyword,
            auto_keyword,
            register_keyword
        }

        /// <summary>
        /// 型修飾子
        /// </summary>
        public enum TypeQualifierKind {
            none,
            const_keyword,
            volatile_keyword,
            restrict_keyword
        }

        /// <summary>
        /// 型修飾子(ポインタ部で使用する場合)
        /// </summary>
        public enum TypeQualifierKindWithPointer {
            none,
            const_keyword,
            volatile_keyword,
            restrict_keyword,
            pointer_keyword
        }

        /// <summary>
        /// 宣言
        /// </summary>
        public class Declaration : SyntaxNode {

            /// <summary>
            /// 宣言指定子
            /// </summary>
            public DeclarationSpecifiers declaration_specifiers {
                get;
            }

            /// <summary>
            /// 初期化子のリスト
            /// </summary>
            public IReadOnlyList<InitDeclarator> init_declarators {
                get;
            }

            /// <summary>
            /// 宣言指定子と初期化子から得られる型宣言・関数宣言・変数宣言・変数定義
            /// </summary>
            public IReadOnlyList<SyntaxNode> items {
                get;
            }

            public Declaration(DeclarationSpecifiers _1, IReadOnlyList<InitDeclarator> _2) {

                declaration_specifiers = _1;
                init_declarators = _2;
                items = build_items(_1, _2);

            }

            private IReadOnlyList<SyntaxNode> build_items(DeclarationSpecifiers dcl_specs, IReadOnlyList<InitDeclarator> init_dcrs) {

                var ret = new List<SyntaxNode>();
                ret.AddRange(build_type_declaration(dcl_specs, init_dcrs).Cast<SyntaxNode>());
                ret.AddRange(build_function_declaration(dcl_specs, init_dcrs).Cast<SyntaxNode>());
                ret.AddRange(build_variable_declaration(dcl_specs, init_dcrs).Cast<SyntaxNode>());
                ret.AddRange(build_variable_definition(dcl_specs, init_dcrs).Cast<SyntaxNode>());
                return ret.ToArray();
            }

            /// <summary>
            /// 初期化子中から変数定義を抽出
            /// </summary>
            /// <param name="dcl_specs"></param>
            /// <param name="init_dcrs"></param>
            /// <returns></returns>
            private static List<Definition.VariableDefinition> build_variable_definition(DeclarationSpecifiers dcl_specs, IReadOnlyList<InitDeclarator> init_dcrs) {

                var var_defs = new List<Definition.VariableDefinition>();
                if (dcl_specs != null && (dcl_specs.storage_class_specifier == StorageClassSpecifierKind.extern_keyword || dcl_specs.storage_class_specifier == StorageClassSpecifierKind.typedef_keyword)) {

                    return var_defs;
                }

                foreach (var init_dcr in init_dcrs) {

                    if (init_dcr.declarator.isvariable) {

                        var_defs.Add(new Definition.VariableDefinition(dcl_specs, init_dcr));
                    }
                }
                return var_defs;
            }

            /// <summary>
            /// 初期化子中から変数宣言を抽出
            /// </summary>
            /// <param name="dcl_specs"></param>
            /// <param name="init_dcrs"></param>
            /// <returns></returns>
            private static List<VariableDeclaration> build_variable_declaration(DeclarationSpecifiers dcl_specs, IReadOnlyList<InitDeclarator> init_dcrs) {

                var var_dcls = new List<VariableDeclaration>();
                if (dcl_specs == null || dcl_specs.storage_class_specifier != StorageClassSpecifierKind.extern_keyword) {

                    return var_dcls;
                }

                foreach (var init_dcr in init_dcrs) {

                    if (init_dcr.declarator.isvariable) {

                        var dcr = init_dcr.declarator;
                        var_dcls.Add(new VariableDeclaration(dcl_specs, dcr));
                    }
                }
                return var_dcls;
            }

            /// <summary>
            /// 初期化子中から関数宣言を抽出
            /// </summary>
            /// <param name="dcl_specs"></param>
            /// <param name="init_dcrs"></param>
            /// <returns></returns>
            private static List<FunctionDeclaration> build_function_declaration(DeclarationSpecifiers dcl_specs, IReadOnlyList<InitDeclarator> init_dcrs) {

                var func_dcls = new List<FunctionDeclaration>();
                if (dcl_specs != null && dcl_specs.storage_class_specifier == StorageClassSpecifierKind.typedef_keyword) {

                    return func_dcls;
                }

                foreach (var init_dcr in init_dcrs) {

                    if (init_dcr.declarator.isfunction()) {

                        func_dcls.Add(new FunctionDeclaration(dcl_specs, init_dcr));
                    }
                }
                return func_dcls;
            }

            /// <summary>
            /// 宣言指定子中からタグ付き型宣言とtypedef宣言を抽出
            /// </summary>
            /// <param name="dcl_specs"></param>
            /// <param name="init_dcrs"></param>
            /// <returns></returns>
            private static List<TypeDeclaration> build_type_declaration(DeclarationSpecifiers dcl_specs, IReadOnlyList<InitDeclarator> init_dcrs) {

                var type_dcls = new List<TypeDeclaration>();
                if (dcl_specs == null) {

                    return type_dcls;
                }

                // タグ付き型宣言を処理
                dcl_specs.type_specifiers.ForEach(type_spec => {
                    var builder = new TypeDeclarationBuilder();
                    type_spec.Accept(builder);
                    type_dcls.AddRange(builder.type_declarations);
                });

                // typedef宣言を処理
                if (dcl_specs.storage_class_specifier == StorageClassSpecifierKind.typedef_keyword) {
                    foreach (var init_dcr in init_dcrs) {
                        type_dcls.Add(new TypeDeclaration.TypedefDeclaration(dcl_specs, init_dcr));
                    }
                }

                return type_dcls;
            }
        }

        /// <summary>
        /// 型宣言中に含まれる構造体・共用体・列挙型を探して個々の型宣言を構築するヘルパークラス
        /// </summary>
        public class TypeDeclarationBuilder {

            public TypeDeclarationBuilder() {

                type_declarations = new List<TypeDeclaration>();
            }

            /// <summary>
            /// 型宣言中に含まれる構造体・共用体・列挙型を探し、個々の型宣言として生成した物
            /// </summary>
            public List<TypeDeclaration> type_declarations {
                get;
            }

            /// <summary>
            /// 基本型指定子を解析
            /// </summary>
            /// <param name="node"></param>
            public void Visit(TypeSpecifier.StandardTypeSpecifier node) {
                // なにもしない
            }

            /// <summary>
            /// typedef型指定子を解析
            /// </summary>
            /// <param name="node"></param>
            public void Visit(TypeSpecifier.TypedefTypeSpecifier node) {
                // なにもしない
            }

            /// <summary>
            /// 構造体型指定子を解析
            /// </summary>
            /// <param name="node"></param>
            public void Visit(TypeSpecifier.StructSpecifier node) {
                if (node.struct_declarations != null) {
                    // 構造体型指定子中にメンバ宣言リストがある場合

                    // メンバ宣言リストの要素を個々に解析
                    foreach (var child in node.struct_declarations) {
                        child.Accept(this);
                    }

                    // 構造体定義を生成して解析結果に追加する
                    type_declarations.Add(new TypeDeclaration.StructTypeDeclaration(node));
                }
            }

            /// <summary>
            /// 共用体型指定子を解析
            /// </summary>
            /// <param name="node"></param>
            public void Visit(TypeSpecifier.UnionSpecifier node) {
                if (node.struct_declarations != null) {
                    // 共用体型指定子中にメンバ宣言リストがある場合

                    // メンバ宣言リストの要素を個々に解析
                    foreach (var child in node.struct_declarations) {
                        child.Accept(this);
                    }

                    // 共用体定義を生成して解析結果に追加する
                    type_declarations.Add(new TypeDeclaration.UnionTypeDeclaration(node));
                }
            }

            /// <summary>
            /// 列挙型指定子を解析
            /// </summary>
            /// <param name="node"></param>
            public void Visit(TypeSpecifier.EnumSpecifier node) {

                if (node.enumerators != null) {
                    // 列挙型指定子中にメンバ宣言リストがある場合
                    // 列挙型定義を生成して解析結果に追加する
                    type_declarations.Add(new TypeDeclaration.EnumTypeDeclaration(node));
                }
            }

            /// <summary>
            /// 構造体宣言を解析
            /// </summary>
            /// <param name="node"></param>
            public void Visit(StructDeclaration node) {

                // 構造体宣言の型指定子と型修飾子のリストを解析
                node.specifier_qualifier_list.Accept(this);
            }

            /// <summary>
            /// 構造体宣言の型指定子と型修飾子のリストを解析
            /// </summary>
            /// <param name="node"></param>
            public void Visit(SpecifierQualifierList node) {

                foreach (var child in node.type_specifiers) {
                    // 型指定子と型修飾子を個々に解析
                    child.Accept(this);
                };
            }
        }

        /// <summary>
        /// 関数宣言
        /// </summary>
        public class FunctionDeclaration : SyntaxNode {

            /// <summary>
            /// 宣言指定子
            /// </summary>
            public DeclarationSpecifiers declaration_specifiers {
                get;
            }

            /// <summary>
            /// 初期宣言子
            /// </summary>
            public InitDeclarator init_declarator {
                get;
            }

            /// <summary>
            /// 宣言された関数名を初期宣言子から取得
            /// </summary>
            public string identifier {

                get {
                    return init_declarator.declarator.identifier;
                }
            }

            public FunctionDeclaration(DeclarationSpecifiers dcl_specs, InitDeclarator init_dcr) {

                declaration_specifiers = dcl_specs;
                init_declarator = init_dcr;
            }
        }

        /// <summary>
        /// 変数宣言
        /// </summary>
        public class VariableDeclaration : SyntaxNode {

            /// <summary>
            /// 宣言指定子
            /// </summary>
            public DeclarationSpecifiers declaration_specifiers {
                get;
            }

            /// <summary>
            /// 宣言子
            /// </summary>
            public Declarator declarator {
                get;
            }

            /// <summary>
            /// 宣言された関数名を宣言子から取得
            /// </summary>
            public string identifier {

                get {
                    return declarator.identifier;
                }
            }

            public VariableDeclaration(DeclarationSpecifiers dcl_specs, Declarator dcr) {

                declaration_specifiers = dcl_specs;
                declarator = dcr;
            }
        }

        /// <summary>
        /// 定義
        /// </summary>
        public abstract class Definition : SyntaxNode {

            protected Definition(DeclarationSpecifiers dcl_specs) {

                declaration_specifiers = dcl_specs;
            }

            /// <summary>
            /// 宣言指定子
            /// </summary>
            public DeclarationSpecifiers declaration_specifiers {
                get;
            }

            /// <summary>
            /// 関数定義
            /// </summary>
            public abstract class FunctionDefinition : Definition {

                /// <summary>
                /// 宣言子
                /// </summary>
                public Declarator declarator {
                    get;
                }

                /// <summary>
                /// 引数定義のリスト
                /// </summary>
                public IReadOnlyList<ParameterDefinition> parameterDefinition {
                    get;
                }

                /// <summary>
                /// 関数本体
                /// </summary>
                public Statement function_body {
                    get;
                }

                protected FunctionDefinition(DeclarationSpecifiers dcl_specs, Declarator dcr, IReadOnlyList<ParameterDefinition> param_defs, Statement compound_stmt) : base(dcl_specs) {

                    declarator = dcr;
                    parameterDefinition = param_defs;
                    function_body = compound_stmt;
                }

                /// <summary>
                /// K&amp;Rスタイルの関数定義
                /// </summary>
                public class KandRFunctionDefinition : FunctionDefinition {

                    public KandRFunctionDefinition(DeclarationSpecifiers dcl_specs, Declarator dcr, List<Declaration> dcls,
                        Statement compound_stmt) : base(dcl_specs, dcr, create_parameters(dcr.identifier_list, dcls), compound_stmt) {

                    }

                    /// <summary>
                    /// K&amp;Rスタイルの引数名リスト部を取得
                    /// </summary>
                    public IReadOnlyList<string> identifier_list {

                        get {
                            return declarator.identifier_list;
                        }
                    }

                    /// <summary>
                    /// 引数名リストと実引数宣言リストから引数宣言リストを生成する
                    /// </summary>
                    /// <param name="param_names"></param>
                    /// <param name="dcls"></param>
                    /// <returns></returns>
                    private static IReadOnlyList<ParameterDefinition> create_parameters(IReadOnlyList<string> param_names, List<Declaration> dcls) {

                        var param_defs = new List<ParameterDefinition>();
                        if (param_names == null) {

                            return param_defs;
                        }

                        foreach (var name in param_names) {

                            var var_def = find_variable_definition(dcls, name);
                            param_defs.Add(variable_definition_to_parameter_definition(var_def));
                        }
                        return param_defs;
                    }

                    /// <summary>
                    /// 変数定義から引数定義を生成して返す
                    /// </summary>
                    /// <param name="var_def"></param>
                    /// <returns></returns>
                    private static ParameterDefinition variable_definition_to_parameter_definition(VariableDefinition var_def) {

                        var dcl_specs = var_def.declaration_specifiers;
                        var dcr = var_def.init_declarator.declarator;
                        var param_def = new ParameterDefinition(dcl_specs, dcr);
                        return param_def;
                    }


                    /// <summary>
                    /// 実引数宣言リスト中から名前が name に一致する暗黙的引数宣言を探し、その中に生成されている変数定義要素を返す。
                    /// 無い場合は暗黙的引数定義と見なし、暗黙的引数定義を実引数宣言リストに追加してから、その中に生成されている変数定義要素を返す。
                    /// </summary>
                    /// <param name="dcls"></param>
                    /// <param name="name"></param>
                    /// <returns></returns>
                    private static VariableDefinition find_variable_definition(List<Declaration> dcls, string name) {

                        foreach (var dcl in dcls) {
                            foreach (var var_def in dcl.items.Where(item => item is VariableDefinition).Cast<VariableDefinition>()) {
                                if (var_def.identifier == name) {
                                    return var_def;
                                }
                            }
                        }

                        // 見つからなかった
                        {
                            var dcl = implicit_parameter_definition(name);
                            dcls.Add(dcl);
                            Debug.Assert(dcl.items.First() is VariableDefinition);
                            return dcl.items.First() as VariableDefinition;
                        }
                    }

                    /// <summary>
                    /// 暗黙的引数定義となる宣言を生成して返す。
                    /// </summary>
                    /// <param name="id"></param>
                    /// <returns></returns>
                    private static Declaration implicit_parameter_definition(string id) {

                        var init_dcr = new InitDeclarator(new Declarator.IdentifierDeclarator(id), null);
                        return new Declaration(null, new[] { init_dcr });
                    }

                }

                /// <summary>
                /// ANSIスタイルの関数定義
                /// </summary>
                public class AnsiFunctionDefinition : FunctionDefinition {


                    public AnsiFunctionDefinition(DeclarationSpecifiers dcl_specs, Declarator dcr, Statement compound_stmt)
                        : base(dcl_specs, dcr, create_parameters(dcr.innermost_parameter_type_list), compound_stmt) {

                    }

                    /// <summary>
                    /// 引数型リストから引数定義リストを作る
                    /// </summary>
                    /// <param name="param_type_list"></param>
                    /// <returns></returns>
                    private static IReadOnlyList<ParameterDefinition> create_parameters(ParameterTypeList param_type_list) {

                        var ret = new List<ParameterDefinition>();
                        if (param_type_list == null) {

                            return ret;
                        }

                        return param_type_list.parameters.Select(param_dcl => {

                            var dcl_specs = param_dcl.declaration_specifiers;
                            var dcr = param_dcl.declarator;
                            var param_def = new ParameterDefinition(dcl_specs, dcr);
                            return param_def;
                        }).ToList();

                    }
                }
            }

            /// <summary>
            /// 変数定義
            /// </summary>
            public class VariableDefinition : Definition {

                /// <summary>
                /// 初期宣言子
                /// </summary>
                public InitDeclarator init_declarator {
                    get;
                }

                public VariableDefinition(DeclarationSpecifiers dcl_specs, InitDeclarator init_dcr) : base(dcl_specs) {

                    init_declarator = init_dcr;
                }

                /// <summary>
                /// 初期宣言子中から変数名を取得
                /// </summary>
                public string identifier {
                    get {
                        return init_declarator.declarator.identifier;
                    }
                }

            }

            /// <summary>
            /// 引数定義
            /// </summary>
            public class ParameterDefinition : Definition {

                /// <summary>
                /// 宣言子
                /// </summary>
                public Declarator declarator {
                    get;
                }

                public ParameterDefinition(DeclarationSpecifiers dcl_specs, Declarator dcr) : base(dcl_specs) {

                    declarator = dcr;
                }

                /// <summary>
                /// 宣言子中から引数名を取得（抽象宣言子の場合は引数名を持たないためnullが戻る）
                /// </summary>
                public string identifier {

                    get {

                        if (declarator != null) {

                            if (declarator.isabstract) {

                                return null;
                            }
                            return declarator.identifier;
                        }
                        return null;
                    }
                }

            }

        }

        /// <summary>
        /// 型宣言
        /// </summary>
        public abstract class TypeDeclaration : SyntaxNode {

            public abstract string identifier {
                get; protected set;
            }

            /// <summary>
            /// typedef 宣言
            /// </summary>
            public class TypedefDeclaration : TypeDeclaration {

                /// <summary>
                /// 宣言指定子
                /// </summary>
                public DeclarationSpecifiers declaration_specifiers {
                    get;
                }

                /// <summary>
                /// 初期宣言子
                /// </summary>
                public InitDeclarator init_declarator {
                    get;
                }

                /// <summary>
                /// 初期宣言子中からtypedef名を取得
                /// </summary>
                public override string identifier {

                    get {
                        return init_declarator.declarator.identifier;
                    }
                    protected set {
                    }
                }

                public TypedefDeclaration(DeclarationSpecifiers dcl_specs, InitDeclarator init_dcr) {

                    declaration_specifiers = dcl_specs;
                    init_declarator = init_dcr;
                }
            }

            /// <summary>
            /// 構造体型宣言
            /// </summary>
            public class StructTypeDeclaration : TypeDeclaration {

                /// <summary>
                /// 構造体型指定子中から構造体タグ名を取得
                /// </summary>
                public override string identifier {
                    get {
                        return struct_specifier.identifier;
                    }
                    protected set {
                    }
                }

                /// <summary>
                /// 構造体型指定子
                /// </summary>
                public TypeSpecifier.StructSpecifier struct_specifier {
                    get;
                }

                public StructTypeDeclaration(TypeSpecifier.StructSpecifier node) {

                    struct_specifier = node;
                }

                public class PseudoStructTypeDeclaration : StructTypeDeclaration {

                    public PseudoStructTypeDeclaration(TypeSpecifier.StructSpecifier node) : base(node) {
                    }
                    public void mark_as_referred_by(object tok) {
                    }
                }
            }

            /// <summary>
            /// 共用体型宣言
            /// </summary>
            public class UnionTypeDeclaration : TypeDeclaration {

                /// <summary>
                /// 共用体型指定子中から共用体タグ名を取得
                /// </summary>
                public override string identifier {
                    get {
                        return union_specifier.identifier;
                    }
                    protected set {
                    }
                }

                /// <summary>
                /// 共用体型指定子
                /// </summary>
                public TypeSpecifier.UnionSpecifier union_specifier {
                    get;
                }

                public UnionTypeDeclaration(TypeSpecifier.UnionSpecifier node) {

                    union_specifier = node;
                }

                public class PseudoUnionTypeDeclaration : UnionTypeDeclaration {

                    public PseudoUnionTypeDeclaration(TypeSpecifier.UnionSpecifier node) : base(node) {
                    }
                    public void mark_as_referred_by(object tok) {
                    }
                }
            }

            /// <summary>
            /// 列挙型宣言
            /// </summary>
            public class EnumTypeDeclaration : TypeDeclaration {

                /// <summary>
                /// 列挙指定子中から共用体タグ名を取得
                /// </summary>
                public override string identifier {
                    get {
                        return enum_specifier.identifier;
                    }
                    protected set {
                    }
                }

                /// <summary>
                /// 列挙指定子
                /// </summary>
                public TypeSpecifier.EnumSpecifier enum_specifier {
                    get;
                }

                /// <summary>
                /// 列挙指定子の列挙子リスト
                /// </summary>
                /// <param name=""></param>
                /// <returns></returns>
                public IReadOnlyList<TypeSpecifier.EnumSpecifier.Enumerator> enumerators {
                    get {
                        return enum_specifier.enumerators;
                    }
                }

                public EnumTypeDeclaration(TypeSpecifier.EnumSpecifier node) {

                    enum_specifier = node;
                }

                public class PseudoEnumTypeDeclaration : EnumTypeDeclaration {
                    public PseudoEnumTypeDeclaration(TypeSpecifier.EnumSpecifier node) : base(node) {
                    }
                    public void mark_as_referred_by(object tok) {
                    }
                }
            }
        }

        /// <summary>
        /// 宣言指定子
        /// </summary>
        public class DeclarationSpecifiers {

            /// <summary>
            /// 宣言指定子に付随するストレージクラス
            /// </summary>
            public StorageClassSpecifierKind storage_class_specifier {
                get; internal set;
            }

            /// <summary>
            /// 宣言指定子に付随する型指定子のリスト
            /// </summary>
            public List<TypeSpecifier> type_specifiers {
                get;
            }

            /// <summary>
            /// 宣言指定子に付随する型修飾子のリスト
            /// </summary>
            public List<TypeQualifierKind> type_qualifiers {
                get;
            }

            /// <summary>
            /// 関数指定子
            /// </summary>
            public enum FuntionSpecifierKind {
                none,
                inline_keyword
            }

            /// <summary>
            /// 宣言指定子に付随する関数指定子
            /// </summary>
            public FuntionSpecifierKind function_specifier {
                get; internal set;
            }

            public DeclarationSpecifiers() {
                type_specifiers = new List<TypeSpecifier>();
                type_qualifiers = new List<TypeQualifierKind>();
            }

            /// <summary>
            /// 型指定子を伴う（明示的な宣言指定子）かどうか
            /// </summary>
            public bool isexplicitly_typed {
                get {
                    return !isimplicitly_typed;
                }
            }

            /// <summary>
            /// 型指定子を伴わない（暗黙的な宣言指定子）かどうか
            /// </summary>
            public bool isimplicitly_typed {
                get {
                    return !type_specifiers.Any();
                }
            }

        }

        /// <summary>
        /// 初期宣言子
        /// </summary>
        public class InitDeclarator {

            /// <summary>
            /// 宣言子
            /// </summary>
            public Declarator declarator {
                get;
            }
            public Initializer initializer {
                get;
            }

            public InitDeclarator(Declarator _1, Initializer _2) {

                declarator = _1;
                initializer = _2;
            }
        }

        /// <summary>
        /// 型指定子
        /// </summary>
        public abstract class TypeSpecifier {

            /// <summary>
            /// 構造体型指定子
            /// </summary>
            public class StructSpecifier : TypeSpecifier {


                /// <summary>
                /// タグ名
                /// </summary>
                public string identifier {
                    get;
                }

                /// <summary>
                /// 匿名型か否か
                /// </summary>
                public bool anonymous {
                    get;
                }

                /// <summary>
                /// メンバ宣言リスト（nullの場合は不完全型）
                /// </summary>
                public IReadOnlyList<StructDeclaration> struct_declarations {
                    get;
                }

                public StructSpecifier(string identifier, IReadOnlyList<StructDeclaration> struct_declarations, bool anonymous) {

                    this.identifier = identifier;
                    this.struct_declarations = struct_declarations;
                    this.anonymous = anonymous;
                }
            }

            /// <summary>
            /// 共用体型指定子
            /// </summary>
            public class UnionSpecifier : TypeSpecifier {

                /// <summary>
                /// タグ名
                /// </summary>
                public string identifier {
                    get;
                }

                /// <summary>
                /// 匿名型か否か
                /// </summary>
                public bool anonymous {
                    get;
                }

                /// <summary>
                /// メンバ宣言リスト（nullの場合は不完全型）
                /// </summary>
                public IReadOnlyList<StructDeclaration> struct_declarations {
                    get;
                }

                public UnionSpecifier(string identifier, IReadOnlyList<StructDeclaration> struct_declarations, bool anonymous) {

                    this.identifier = identifier;
                    this.struct_declarations = struct_declarations;
                    this.anonymous = anonymous;
                }
            }

            /// <summary>
            /// 基本型指定子
            /// </summary>
            public class StandardTypeSpecifier : TypeSpecifier {

                public enum StandardTypeSpecifierKind {
                    void_keyword,
                    char_keyword,
                    short_keyword,
                    int_keyword,
                    long_keyword,
                    float_keyword,
                    double_keyword,
                    signed_keyword,
                    unsigned_keyword,
                    bool_keyword,
                    complex_keyword,
                    imaginary_keyword,
                    builtin_va_list_keyword
                }

                public StandardTypeSpecifierKind Kind {
                    get;
                }

                public StandardTypeSpecifier(StandardTypeSpecifierKind s) {

                    Kind = s;
                }
            }

            /// <summary>
            /// typedef型指定子
            /// </summary>
            public class TypedefTypeSpecifier : TypeSpecifier {

                public string identifier {
                    get;
                }

                public TypedefTypeSpecifier(string s) {

                    identifier = s;
                }
            }

            /// <summary>
            /// 列挙指定子
            /// </summary>
            public class EnumSpecifier : TypeSpecifier {
                /// <summary>
                /// タグ名
                /// </summary>
                public string identifier {
                    get;
                }

                /// <summary>
                /// 列挙子リスト
                /// </summary>
                public IReadOnlyList<Enumerator> enumerators {
                    get;
                }

                /// <summary>
                /// 列挙子リストの末尾のコンマの有無
                /// </summary>
                public bool trailing_comma {
                    get;
                }

                /// <summary>
                /// 匿名型か否か
                /// </summary>
                public bool anonymous {
                    get;
                }

                public EnumSpecifier(string identifier, IReadOnlyList<Enumerator> enumerators, bool trailingComma, bool anonymous) {

                    this.identifier = identifier;
                    this.enumerators = enumerators;
                    this.trailing_comma = trailingComma;
                    this.anonymous = anonymous;
                }

                /// <summary>
                /// 列挙子
                /// </summary>
                public class Enumerator {

                    /// <summary>
                    /// 列挙子の名前
                    /// </summary>
                    public string identifier {
                        get;
                    }

                    /// <summary>
                    /// 列挙子の値
                    /// </summary>
                    public Expression expression {
                        get;
                    }

                    public Enumerator(string identifier, Expression expression) {

                        this.identifier = identifier;
                        this.expression = expression;
                    }
                }
            }
        }

        /// <summary>
        /// 構造体宣言
        /// </summary>
        public class StructDeclaration {

            /// <summary>
            /// 宣言の型指定子もしくは型修飾子リスト
            /// </summary>
            public SpecifierQualifierList specifier_qualifier_list {
                get;
            }
            /// <summary>
            /// メンバ宣言子リスト
            /// </summary>
            public IReadOnlyList<StructDeclarator> struct_member_declarators {
                get;
            }
            /// <summary>
            /// メンバ宣言リスト
            /// </summary>
            public IReadOnlyList<MemberDeclaration> items {
                get;
            }

            public StructDeclaration(SpecifierQualifierList _1, IReadOnlyList<StructDeclarator> _2) {

                specifier_qualifier_list = _1;
                struct_member_declarators = _2;
                items = build_items(_1, _2);
            }

            private IReadOnlyList<MemberDeclaration> build_items(SpecifierQualifierList spec_qual_list, IReadOnlyList<StructDeclarator> struct_dcrs) {

                // FIXME: Must support unnamed bit padding.

                if (!struct_dcrs.Any()) {

                    return new[] { new MemberDeclaration(spec_qual_list, null) };
                }
                return struct_dcrs.Select(struct_dcr => new MemberDeclaration(spec_qual_list, struct_dcr)).ToArray();
            }
        }

        public class MemberDeclaration {

            public SpecifierQualifierList specifier_qualifier_list {
                get;
            }

            public StructDeclarator struct_declarator {
                get;
            }

            public MemberDeclaration(SpecifierQualifierList spec_qual_list, StructDeclarator struct_member_dcr) {

                specifier_qualifier_list = spec_qual_list;
                struct_declarator = struct_member_dcr;
            }

            public string identifier() {

                if (struct_declarator != null && struct_declarator.declarator != null) {

                    return struct_declarator.declarator.identifier;
                }
                return null;
            }

        }

        /// <summary>
        /// 型指定子と型修飾子のリスト
        /// </summary>
        public class SpecifierQualifierList {
            /// <summary>
            /// 型指定子リスト
            /// </summary>
            public List<TypeSpecifier> type_specifiers {
                get;
            }
            /// <summary>
            /// 型修飾子リスト
            /// </summary>
            public List<TypeQualifierKind> type_qualifiers {
                get;
            }
            public SpecifierQualifierList() {
                type_specifiers = new List<TypeSpecifier>();
                type_qualifiers = new List<TypeQualifierKind>();
            }
        }

        /// <summary>
        /// 構造体宣言子
        /// </summary>
        public class StructDeclarator : SyntaxNode {

            /// <summary>
            /// 構造体宣言子に付随する宣言子
            /// </summary>
            public Declarator declarator {
                get;
            }

            /// <summary>
            /// 構造体宣言子のビットフィールドサイズ
            /// </summary>
            public Expression bitfield_expr {
                get;
            }

            public StructDeclarator(Declarator _1, Expression _2) {

                declarator = _1;
                bitfield_expr = _2;
            }
        }

        /// <summary>
        /// 宣言子
        /// </summary>
        public abstract class Declarator {
            /// <summary>
            /// 宣言子の修飾対象となる宣言子
            /// </summary>
            public Declarator @base {
                get;
            }

            public bool full {
                get; set;
            }

            /// <summary>
            /// 宣言子に付随するポインタを含む型修飾子のリスト
            /// </summary>
            public IReadOnlyList<TypeQualifierKindWithPointer> pointer {
                get; set;
            }

            /// <summary>
            /// 宣言子に付随する識別子
            /// </summary>
            public abstract string identifier {
                get; protected set;
            }

            /// <summary>
            /// 宣言子に付随する識別子リスト
            /// </summary>
            public abstract IReadOnlyList<string> identifier_list {
                get; protected set;
            }

            /// <summary>
            /// 宣言子が関数であるか判定
            /// </summary>
            /// <param name="stack"></param>
            /// <returns></returns>
            public abstract bool isfunction(Stack<string> stack = null);

            /// <summary>
            /// 宣言子に付随する最も内側にある引数型リスト
            /// </summary>
            public abstract ParameterTypeList innermost_parameter_type_list {
                get; protected set;
            }

            /// <summary>
            /// 宣言子が抽象宣言子か判定
            /// </summary>
            public virtual bool isabstract {

                get {
                    return false;
                }
            }

            /// <summary>
            ///  宣言子が変数である（＝関数ではない）か判定
            /// </summary>
            public bool isvariable {
                get {
                    return !isfunction();
                }
            }

            protected Declarator(Declarator @base) {
                this.@base = @base;
            }

            /// <summary>
            /// グループ化宣言子
            /// </summary>
            /// <remarks>丸括弧でグループ化された宣言子</remarks>
            public class GroupedDeclarator : Declarator {

                /// <summary>
                /// 宣言子に付随する識別子
                /// </summary>
                public override string identifier {

                    get {
                        return @base.identifier;
                    }
                    protected set {
                    }
                }

                /// <summary>
                /// 宣言子に付随する識別子リスト
                /// </summary>
                public override IReadOnlyList<string> identifier_list {

                    get {
                        return @base.identifier_list;
                    }
                    protected set {
                    }
                }

                /// <summary>
                /// 宣言子が関数であるか判定
                /// </summary>
                /// <param name="stack"></param>
                /// <returns></returns>
                public override bool isfunction(Stack<string> stack = null) {

                    stack = stack ?? new Stack<string>();
                    if (pointer != null && pointer.Any()) {
                        stack.Push("pointer");
                    }
                    @base.isfunction(stack);
                    return stack.FirstOrDefault() == "function";
                }

                /// <summary>
                /// 宣言子に付随する最も内側にある引数型リスト
                /// </summary>
                public override ParameterTypeList innermost_parameter_type_list {

                    get {
                        return @base.innermost_parameter_type_list;
                    }
                    protected set {
                    }
                }
                public GroupedDeclarator(Declarator @base) : base(@base) {
                }
            }

            /// <summary>
            /// 識別子宣言子
            /// </summary>
            public class IdentifierDeclarator : Declarator {

                /// <summary>
                /// 宣言子に付随する識別子
                /// </summary>
                public override string identifier {
                    get; protected set;
                }

                /// <summary>
                /// 宣言子に付随する識別子リスト（null固定）
                /// </summary>
                public override IReadOnlyList<string> identifier_list {
                    get {
                        return null;
                    }
                    protected set {
                    }
                }

                /// <summary>
                /// 宣言子が関数であるか判定
                /// </summary>
                /// <param name="stack"></param>
                /// <returns></returns>
                public override bool isfunction(Stack<string> stack = null) {

                    stack = stack ?? new Stack<string>();
                    if (pointer != null && pointer.Any()) {
                        stack.Push("pointer");
                    }
                    return stack.FirstOrDefault() == "function";
                }

                /// <summary>
                /// 宣言子に付随する最も内側にある引数型リスト（null固定）
                /// </summary>
                public override ParameterTypeList innermost_parameter_type_list {
                    get {
                        return null;
                    }
                    protected set {
                    }
                }
                public IdentifierDeclarator(string x) : base(null) {
                    identifier = x;
                }
            }

            /// <summary>
            /// 配列宣言子
            /// </summary>
            public class ArrayDeclarator : Declarator {

                /// <summary>
                /// 宣言子に付随する識別子
                /// </summary>
                public override string identifier {
                    get {
                        return @base.identifier;
                    }
                    protected set {
                    }
                }

                /// <summary>
                /// 宣言子に付随する識別子リスト
                /// </summary>
                public override IReadOnlyList<string> identifier_list {
                    get {
                        return @base.identifier_list;
                    }
                    protected set {
                    }
                }

                /// <summary>
                /// サイズ（要素数）式
                /// </summary>
                public Expression size_expression {
                    get;
                }

                /// <summary>
                /// 宣言子が関数であるか判定
                /// </summary>
                /// <param name="stack"></param>
                /// <returns></returns>
                public override bool isfunction(Stack<string> stack = null) {
                    stack = stack ?? new Stack<string>();
                    if (pointer != null && pointer.Any()) {
                        stack.Push("pointer");
                    }
                    stack.Push("array");
                    @base.isfunction(stack);
                    return stack.FirstOrDefault() == "function";
                }

                /// <summary>
                /// 宣言子に付随する最も内側にある引数型リスト
                /// </summary>
                public override ParameterTypeList innermost_parameter_type_list {
                    get {
                        return @base.innermost_parameter_type_list;
                    }
                    protected set {
                    }
                }

                public ArrayDeclarator(Declarator @base, Expression size_expression) : base(@base) {
                    this.size_expression = size_expression;
                }
            }

            /// <summary>
            /// 関数宣言子
            /// </summary>
            public abstract class FunctionDeclarator : Declarator {

                /// <summary>
                /// 宣言子に付随する識別子
                /// </summary>
                public override string identifier {
                    get {
                        return @base.identifier;
                    }
                    protected set {
                    }
                }

                /// <summary>
                /// 宣言子が関数であるか判定
                /// </summary>
                /// <param name="stack"></param>
                /// <returns></returns>
                public override bool isfunction(Stack<string> stack = null) {

                    stack = stack ?? new Stack<string>();
                    if (pointer != null && pointer.Any()) {
                        stack.Push("pointer");
                    }
                    stack.Push("function");
                    @base.isfunction(stack);
                    return stack.FirstOrDefault() == "function";
                }

                protected FunctionDeclarator(Declarator @base) : base(@base) {
                }

                /// <summary>
                /// 曖昧な関数宣言子
                /// </summary>
                public class AbbreviatedFunctionDeclarator : FunctionDeclarator {

                    /// <summary>
                    /// 宣言子に付随する識別子リスト
                    /// </summary>
                    public override IReadOnlyList<string> identifier_list {

                        get {
                            return @base.identifier_list;
                        }
                        protected set {
                        }

                    }

                    /// <summary>
                    /// 宣言子に付随する最も内側にある引数型リスト
                    /// </summary>
                    public override ParameterTypeList innermost_parameter_type_list {
                        get {
                            return @base.innermost_parameter_type_list;
                        }
                        protected set {
                        }
                    }

                    public AbbreviatedFunctionDeclarator(Declarator @base) : base(@base) {

                    }
                }

                /// <summary>
                /// K&amp;Rスタイルの関数宣言子
                /// </summary>
                public class KandRFunctionDeclarator : FunctionDeclarator {

                    /// <summary>
                    /// 宣言子に付随する最も内側にある引数型リスト
                    /// </summary>
                    public override IReadOnlyList<string> identifier_list {
                        get; protected set;
                    }

                    /// <summary>
                    /// 宣言子に付随する最も内側にある引数型リスト
                    /// </summary>
                    public override ParameterTypeList innermost_parameter_type_list {

                        get {
                            return @base.innermost_parameter_type_list;
                        }
                        protected set {
                        }

                    }

                    public KandRFunctionDeclarator(Declarator @base, IReadOnlyList<string> identifier_list) : base(@base) {
                        this.identifier_list = identifier_list;
                    }

                }

                /// <summary>
                /// ANSIスタイルの関数宣言子
                /// </summary>
                public class AnsiFunctionDeclarator : FunctionDeclarator {

                    /// <summary>
                    /// 引数型リスト
                    /// </summary>
                    public ParameterTypeList parameter_type_list {
                        get;
                    }

                    /// <summary>
                    /// 宣言子に付随する識別子リスト
                    /// </summary>
                    public override IReadOnlyList<string> identifier_list {

                        get {
                            return @base.identifier_list;
                        }
                        protected set {
                        }
                    }

                    /// <summary>
                    /// 宣言子に付随する最も内側にある引数型リスト
                    /// </summary>
                    public override ParameterTypeList innermost_parameter_type_list {
                        get {
                            return @base.innermost_parameter_type_list ?? parameter_type_list;
                        }
                        protected set {
                        }
                    }
                    public AnsiFunctionDeclarator(Declarator @base, ParameterTypeList parameterTypeList) : base(@base) {

                        parameter_type_list = parameterTypeList;
                    }

                }

            }

            /// <summary>
            /// 抽象宣言子(１つ以上のポインター、配列、または関数修飾子で構成される、識別子のない宣言子)
            /// </summary>
            public abstract class AbstractDeclarator : Declarator {
                protected AbstractDeclarator(Declarator @base) : base(@base) {
                }

                /// <summary>
                /// 宣言子に付随する識別子（null固定）
                /// </summary>
                public override string identifier {
                    get {
                        return null;
                    }
                    protected set {
                    }

                }

                /// <summary>
                /// 宣言子が抽象宣言子か判定（true固定）
                /// </summary>
                public override bool isabstract {

                    get {
                        return true;
                    }
                }

                /// <summary>
                /// 抽象関数宣言子
                /// </summary>
                public class FunctionAbstractDeclarator : AbstractDeclarator {

                    /// <summary>
                    /// 宣言子が関数であるか判定
                    /// </summary>
                    /// <param name="stack"></param>
                    /// <returns></returns>
                    public override bool isfunction(Stack<string> stack = null) {

                        stack = stack ?? new Stack<string>();
                        stack.Push("function");
                        @base.isfunction(stack);
                        return stack.FirstOrDefault() == "function";
                    }

                    /// <summary>
                    /// 引数型リスト
                    /// </summary>
                    public ParameterTypeList parameter_type_list {
                        get;
                    }

                    /// <summary>
                    /// 宣言子に付随する識別子リスト
                    /// </summary>
                    public override IReadOnlyList<string> identifier_list {

                        get {
                            return @base.identifier_list;
                        }
                        protected set {
                        }

                    }

                    /// <summary>
                    /// 宣言子に付随する最も内側にある引数型リスト
                    /// </summary>
                    public override ParameterTypeList innermost_parameter_type_list {
                        get {
                            return @base.innermost_parameter_type_list ?? parameter_type_list;
                        }
                        protected set {
                        }
                    }

                    public FunctionAbstractDeclarator(AbstractDeclarator @base, ParameterTypeList p2) : base(@base) {
                        parameter_type_list = p2;
                    }
                }

                /// <summary>
                /// 抽象配列宣言子
                /// </summary>
                public class ArrayAbstractDeclarator : AbstractDeclarator {

                    /// <summary>
                    /// 宣言子が関数であるか判定
                    /// </summary>
                    /// <param name="stack"></param>
                    /// <returns></returns>
                    public override bool isfunction(Stack<string> stack = null) {

                        stack = stack ?? new Stack<string>();
                        stack.Push("array");
                        @base.isfunction(stack);
                        return stack.FirstOrDefault() == "function";
                    }

                    /// <summary>
                    /// サイズ（要素数）式
                    /// </summary>
                    public Expression size_expression {
                        get;
                    }

                    /// <summary>
                    /// 宣言子に付随する識別子リスト
                    /// </summary>
                    public override IReadOnlyList<string> identifier_list {
                        get {
                            return @base?.identifier_list;
                        }
                        protected set {
                        }

                    }

                    /// <summary>
                    /// 宣言子に付随する最も内側にある引数型リスト
                    /// </summary>
                    public override ParameterTypeList innermost_parameter_type_list {
                        get {
                            return @base?.innermost_parameter_type_list;
                        }
                        protected set {
                        }
                    }

                    public ArrayAbstractDeclarator(AbstractDeclarator @base, Expression p2) : base(@base) {

                        this.size_expression = p2;
                    }

                }

                /// <summary>
                /// 抽象グループ化宣言子
                /// </summary>
                /// <remarks>丸括弧でグループ化された抽象宣言子</remarks>
                public class GroupedAbstractDeclarator : AbstractDeclarator {

                    /// <summary>
                    /// 宣言子が関数であるか判定
                    /// </summary>
                    /// <param name="stack"></param>
                    /// <returns></returns>
                    public override bool isfunction(Stack<string> stack = null) {

                        return @base.isfunction(null);
                    }

                    /// <summary>
                    /// 宣言子に付随する識別子リスト
                    /// </summary>
                    public override IReadOnlyList<string> identifier_list {

                        get {
                            return @base.identifier_list;
                        }
                        protected set {
                        }
                    }

                    /// <summary>
                    /// 宣言子に付随する最も内側にある引数型リスト
                    /// </summary>
                    public override ParameterTypeList innermost_parameter_type_list {

                        get {
                            return @base.innermost_parameter_type_list;
                        }
                        protected set {
                        }

                    }

                    public GroupedAbstractDeclarator(AbstractDeclarator @base) : base(@base) {
                    }

                }

                /// <summary>
                /// 抽象ポインタ宣言子
                /// </summary>
                public class PointerAbstractDeclarator : AbstractDeclarator {

                    public override bool isfunction(Stack<string> stack = null) {

                        stack = stack ?? new Stack<string>();
                        stack.Push("pointer");
                        @base.isfunction(stack);
                        return stack.FirstOrDefault() == "function";
                    }

                    public override IReadOnlyList<string> identifier_list {

                        get {
                            return @base?.identifier_list;
                        }
                        protected set {
                        }

                    }

                    public PointerAbstractDeclarator(AbstractDeclarator @base, IReadOnlyList<TypeQualifierKindWithPointer> _1) : base(@base) {
                        pointer = _1;
                    }
                    public override ParameterTypeList innermost_parameter_type_list {

                        get {
                            return @base?.innermost_parameter_type_list;
                        }
                        protected set {
                        }

                    }
                }
            }
        }

        /// <summary>
        /// 引数型リスト
        /// </summary>
        public class ParameterTypeList : SyntaxNode {

            /// <summary>
            /// 可変長引数を持つか否か
            /// </summary>
            public bool have_va_list {
                get;
            }

            /// <summary>
            /// 引数宣言のリスト
            /// </summary>
            public IReadOnlyList<ParameterDeclaration> parameters {
                get;
            }

            public ParameterTypeList(IReadOnlyList<ParameterDeclaration> parameters, bool haveVaList) {

                this.parameters = parameters;
                have_va_list = haveVaList;
            }
        }

        /// <summary>
        /// 引数宣言
        /// </summary>
        public class ParameterDeclaration : SyntaxNode {

            /// <summary>
            /// 宣言指定子
            /// </summary>
            public DeclarationSpecifiers declaration_specifiers {
                get;
            }

            /// <summary>
            /// 宣言子
            /// </summary>
            public Declarator declarator {
                get;
            }

            public ParameterDeclaration(DeclarationSpecifiers declarationSpecifiers, Declarator declarator) {

                declaration_specifiers = declarationSpecifiers;
                this.declarator = declarator;
            }
        }

        /// <summary>
        /// 文
        /// </summary>
        public abstract class Statement : SyntaxNode {

            /// <summary>
            /// エラー文（構文解析器内部専用）
            /// </summary>
            public class ErrorStatement : Statement {

            }

            /// <summary>
            /// ラベル付き文
            /// </summary>
            public abstract class LabeledStatement : Statement {

                /// <summary>
                /// defaultラベル付き文
                /// </summary>
                public class DefaultLabeledStatement : LabeledStatement {

                    public Statement statement {
                        get;
                    }

                    public DefaultLabeledStatement(Statement statement) {

                        this.statement = statement;
                    }
                }

                /// <summary>
                /// caseラベル付き文
                /// </summary>
                public class CaseLabeledStatement : LabeledStatement {

                    public Expression expression {
                        get;
                    }
                    public Statement statement {
                        get;
                    }

                    public CaseLabeledStatement(Expression expression, Statement statement) {

                        this.expression = expression;
                        this.statement = statement;
                    }
                }

                /// <summary>
                /// 一般ラベル付き文
                /// </summary>
                public class GenericLabeledStatement : LabeledStatement {

                    public string label {
                        get;
                    }
                    public Statement statement {
                        get;
                    }

                    public GenericLabeledStatement(string label, Statement statement) {

                        this.label = label;
                        this.statement = statement;
                    }
                }

            }

            /// <summary>
            /// 複文
            /// </summary>
            public class CompoundStatement : Statement {

                public IReadOnlyList<SyntaxNode> block_items {
                    get;
                }

                public CompoundStatement(IReadOnlyList<SyntaxNode> blockItems) {

                    this.block_items = blockItems;
                }
            }

            /// <summary>
            /// 式文
            /// </summary>
            public class ExpressionStatement : Statement {

                public Expression expression {
                    get;
                }

                public ExpressionStatement(Expression expression) {

                    this.expression = expression;
                }
            }

            /// <summary>
            /// 選択文
            /// </summary>
            public abstract class SelectionStatement : Statement {

                /// <summary>
                /// if文
                /// </summary>
                public class IfStatement : SelectionStatement {

                    public Expression expression {
                        get;
                    }
                    public Statement then_statement {
                        get;
                    }
                    public Statement else_statement {
                        get;
                    }

                    public IfStatement(Expression expression, Statement thenStatement, Statement elseStatement) {

                        this.expression = expression;
                        this.then_statement = thenStatement;
                        this.else_statement = elseStatement;
                    }
                }

                /// <summary>
                /// switch文
                /// </summary>
                public class SwitchStatement : SelectionStatement {

                    public Expression expression {
                        get;
                    }
                    public Statement statement {
                        get;
                    }

                    public SwitchStatement(Expression expression, Statement statement) {

                        this.expression = expression;
                        this.statement = statement;
                    }
                }

            }

            /// <summary>
            /// 反復文
            /// </summary>
            public abstract class IterationStatement : Statement {

                /// <summary>
                /// C99形式のfor文
                /// </summary>
                public class C99ForStatement : IterationStatement {

                    public Declaration declaration {
                        get;
                    }
                    public Statement condition_statement {
                        get;
                    }
                    public Expression expression {
                        get;
                    }
                    public Statement body_statement {
                        get;
                    }

                    public C99ForStatement(Declaration declaration, Statement condition_statement, Expression expression, Statement body_statement) {

                        this.declaration = declaration;
                        this.condition_statement = condition_statement;
                        this.expression = expression;
                        this.body_statement = body_statement;
                    }
                }

                /// <summary>
                /// C89形式のfor文
                /// </summary>
                public class ForStatement : IterationStatement {

                    public Statement initial_statement {
                        get;
                    }
                    public Statement condition_statement {
                        get;
                    }
                    public Expression expression {
                        get;
                    }
                    public Statement body_statement {
                        get;
                    }

                    public ForStatement(Statement initial_statement, Statement condition_statement, Expression expression, Statement body_statement) {

                        this.initial_statement = initial_statement;
                        this.condition_statement = condition_statement;
                        this.expression = expression;
                        this.body_statement = body_statement;
                    }
                }

                /// <summary>
                /// do-while文
                /// </summary>
                public class DoStatement : IterationStatement {

                    public Statement statement {
                        get;
                    }
                    public Expression expression {
                        get;
                    }

                    public DoStatement(Statement statement, Expression expression) {

                        this.statement = statement;
                        this.expression = expression;
                    }
                }


                /// <summary>
                /// while文
                /// </summary>
                public class WhileStatement : IterationStatement {

                    public Expression expression {
                        get;
                    }
                    public Statement statement {
                        get;
                    }

                    public WhileStatement(Expression expression, Statement statement) {

                        this.expression = expression;
                        this.statement = statement;
                    }
                }


            }

            /// <summary>
            /// ジャンプ文
            /// </summary>
            public abstract class JumpStatement : Statement {

                /// <summary>
                /// return文
                /// </summary>
                public class ReturnStatement : JumpStatement {

                    public Expression expression {
                        get;
                    }

                    public ReturnStatement(Expression expression) {

                        this.expression = expression;
                    }
                }

                /// <summary>
                /// break文
                /// </summary>
                public class BreakStatement : JumpStatement {

                }

                /// <summary>
                /// continue文
                /// </summary>
                public class ContinueStatement : JumpStatement {

                }

                /// <summary>
                /// goto文
                /// </summary>
                public class GotoStatement : JumpStatement {

                    public string identifier {
                        get;
                    }

                    public GotoStatement(string identifier) {

                        this.identifier = identifier;
                    }
                }

            }
        }

        /// <summary>
        /// 翻訳単位
        /// </summary>
        public class TranslationUnit : SyntaxNode {

            public IReadOnlyList<SyntaxNode> external_declarations {
                get;
            }

            public TranslationUnit(IReadOnlyList<SyntaxNode> externalDeclarations) {

                external_declarations = externalDeclarations;
            }
        }

        /// <summary>
        /// 型名
        /// </summary>
        public class TypeName : SyntaxNode {

            public SpecifierQualifierList specifier_qualifier_list {
                get;
            }
            public Declarator.AbstractDeclarator abstract_declarator {
                get;
            }
            public TypeDeclaration type_declaration {
                get;
            }

            public TypeName(SpecifierQualifierList specifierQualifierList, Declarator.AbstractDeclarator abstractDeclarator) {

                this.specifier_qualifier_list = specifierQualifierList;
                this.abstract_declarator = abstractDeclarator;
                this.type_declaration = build_type_declaration(specifierQualifierList);
            }

            private TypeDeclaration build_type_declaration(SpecifierQualifierList spec_qual_list) {
                foreach (var type_spec in spec_qual_list.type_specifiers) {

                    var builder = new TypeDeclarationBuilder();
                    type_spec.Accept(builder);
                    if (builder.type_declarations.Any()) {

                        return builder.type_declarations.First();
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 初期化子
        /// </summary>
        public class Initializer : SyntaxNode {

            /// <summary>
            /// 指示子を伴わない初期化リテラル
            /// </summary>
            /// <remarks>
            /// expression と initializers は排他
            /// </remarks>
            public Expression expression {
                get;
            }

            /// <summary>
            /// 指示子を伴う初期化リテラル
            /// </summary>
            /// <remarks>
            /// expression と initializers は排他
            /// </remarks>
            public IReadOnlyList<Tuple<IReadOnlyList<Designator>, Initializer>> initializers {
                get;
            }

            public Initializer(Expression expression, IReadOnlyList<Tuple<IReadOnlyList<Designator>, Initializer>> initializers) {
                System.Diagnostics.Debug.Assert((expression != null && initializers == null) || (expression == null && initializers != null));
                this.expression = expression;
                this.initializers = initializers;
            }

            /// <summary>
            /// 指示子
            /// </summary>
            public abstract class Designator {

                /// <summary>
                /// メンバー指示子
                /// </summary>
                public class MemberDesignator : Designator {

                    public string identifier {
                        get;
                    }

                    public MemberDesignator(string identifier) {

                        this.identifier = identifier;
                    }
                }

                /// <summary>
                /// インデクス指示子
                /// </summary>
                public class IndexDesignator : Designator {

                    public Expression expression {
                        get;
                    }

                    public IndexDesignator(Expression expression) {

                        this.expression = expression;
                    }
                }
            }
        }
    }

    public static class SyntaxNodeExt {
        public static string ToCString(this SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression.OperatorKind self) {
            switch (self) {
                case SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression.OperatorKind.Add:
                    return "+";
                case SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression.OperatorKind.Subtract:
                    return "-";
                case SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression.OperatorKind.Inverse:
                    return "~";
                case SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression.OperatorKind.Negate:
                    return "!";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }
        public static string ToCString(this SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind self) {
            switch (self) {
                case SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.multiply_assign:
                    return "*=";
                case SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.divide_assign:
                    return "/=";
                case SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.modulus_assign:
                    return "%=";
                case SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.add_assign:
                    return "+=";
                case SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.subtract_assign:
                    return "-=";
                case SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.left_shift_assign:
                    return "<<=";
                case SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.right_shift_assign:
                    return ">>=";
                case SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.binary_and_assign:
                    return "&=";
                case SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.binary_or_assign:
                    return "|=";
                case SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.xor_assign:
                    return "^=";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }
        public static string ToCString(this SyntaxNode.Expression.BinaryExpression.EqualityExpression.OperatorKind self) {
            switch (self) {
                case SyntaxNode.Expression.BinaryExpression.EqualityExpression.OperatorKind.equal:
                    return "==";
                case SyntaxNode.Expression.BinaryExpression.EqualityExpression.OperatorKind.not_equal:
                    return "!=";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }
        public static string ToCString(this SyntaxNode.Expression.BinaryExpression.RelationalExpression.OperatorKind self) {
            switch (self) {
                case SyntaxNode.Expression.BinaryExpression.RelationalExpression.OperatorKind.less_equal:
                    return "<=";
                case SyntaxNode.Expression.BinaryExpression.RelationalExpression.OperatorKind.less:
                    return "<";
                case SyntaxNode.Expression.BinaryExpression.RelationalExpression.OperatorKind.greater_equal:
                    return ">=";
                case SyntaxNode.Expression.BinaryExpression.RelationalExpression.OperatorKind.greater:
                    return ">";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }
        public static string ToCString(this SyntaxNode.Expression.BinaryExpression.ShiftExpression.OperatorKind self) {
            switch (self) {
                case SyntaxNode.Expression.BinaryExpression.ShiftExpression.OperatorKind.left_shift:
                    return "<<";
                case SyntaxNode.Expression.BinaryExpression.ShiftExpression.OperatorKind.right_shift:
                    return ">>";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }
        public static string ToCString(this SyntaxNode.Expression.BinaryExpression.AdditiveExpression.OperatorKind self) {
            switch (self) {
                case SyntaxNode.Expression.BinaryExpression.AdditiveExpression.OperatorKind.add:
                    return "+";
                case SyntaxNode.Expression.BinaryExpression.AdditiveExpression.OperatorKind.subtract:
                    return "-";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }
        public static string ToCString(this SyntaxNode.Expression.BinaryExpression.MultiplicativeExpression.OperatorKind self) {
            switch (self) {
                case SyntaxNode.Expression.BinaryExpression.MultiplicativeExpression.OperatorKind.multiply:
                    return "*";
                case SyntaxNode.Expression.BinaryExpression.MultiplicativeExpression.OperatorKind.divide:
                    return "/";
                case SyntaxNode.Expression.BinaryExpression.MultiplicativeExpression.OperatorKind.modulus:
                    return "%";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }
        public static string ToCString(this SyntaxNode.StorageClassSpecifierKind self) {
            switch (self) {
                case SyntaxNode.StorageClassSpecifierKind.none:
                    return "";
                case SyntaxNode.StorageClassSpecifierKind.typedef_keyword:
                    return "typedef";
                case SyntaxNode.StorageClassSpecifierKind.extern_keyword:
                    return "extern";
                case SyntaxNode.StorageClassSpecifierKind.static_keyword:
                    return "static";
                case SyntaxNode.StorageClassSpecifierKind.auto_keyword:
                    return "auto";
                case SyntaxNode.StorageClassSpecifierKind.register_keyword:
                    return "register";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }
        public static string ToCString(this SyntaxNode.TypeQualifierKind self) {
            switch (self) {
                case SyntaxNode.TypeQualifierKind.none:
                    return "";
                case SyntaxNode.TypeQualifierKind.const_keyword:
                    return "const";
                case SyntaxNode.TypeQualifierKind.volatile_keyword:
                    return "volatile";
                case SyntaxNode.TypeQualifierKind.restrict_keyword:
                    return "restrict";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }
        public static string ToCString(this SyntaxNode.TypeQualifierKindWithPointer self) {
            switch (self) {
                case SyntaxNode.TypeQualifierKindWithPointer.none:
                    return "";
                case SyntaxNode.TypeQualifierKindWithPointer.const_keyword:
                    return "const";
                case SyntaxNode.TypeQualifierKindWithPointer.volatile_keyword:
                    return "volatile";
                case SyntaxNode.TypeQualifierKindWithPointer.restrict_keyword:
                    return "restrict";
                case SyntaxNode.TypeQualifierKindWithPointer.pointer_keyword:
                    return "*";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }
        public static string ToCString(this SyntaxNode.DeclarationSpecifiers.FuntionSpecifierKind self) {
            switch (self) {
                case SyntaxNode.DeclarationSpecifiers.FuntionSpecifierKind.none:
                    return "";
                case SyntaxNode.DeclarationSpecifiers.FuntionSpecifierKind.inline_keyword:
                    return "inline";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }
        public static string ToCString(this SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind self) {
            switch (self) {
                case SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.void_keyword:
                    return "void";
                case SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.char_keyword:
                    return "char";
                case SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.short_keyword:
                    return "short";
                case SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.int_keyword:
                    return "int";
                case SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.long_keyword:
                    return "long";
                case SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.float_keyword:
                    return "float";
                case SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.double_keyword:
                    return "double";
                case SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.signed_keyword:
                    return "signed";
                case SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.unsigned_keyword:
                    return "unsigned";
                case SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.bool_keyword:
                    return "bool";
                case SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.complex_keyword:
                    return "_Complex";
                case SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.imaginary_keyword:
                    return "_Imaginary";
                case SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.builtin_va_list_keyword:
                    return "__builtin_va_list";
                default:
                    throw new ArgumentOutOfRangeException(nameof(self), self, null);
            }
        }

        public static string ToCString(this SyntaxNode self) {
            return self.Accept<string>(new StringWriteVisitor());
        }
        public static string ToCString(this SyntaxNode.TypeSpecifier self) {
            return self.Accept<string>(new StringWriteVisitor());
        }    }

    public static class VisitorExt {

        private static Dictionary<Tuple<int, Type, Type>, MethodInfo> Memoise = new Dictionary<Tuple<int, Type, Type>, MethodInfo>();

        public static void Accept(this object self, object visitor) {
            if (self == null) {
                return;
            }

            var memoKey = Tuple.Create(1, self.GetType(), visitor.GetType());

            MethodInfo visitMethod;

            if (Memoise.TryGetValue(memoKey, out visitMethod) == false) {
                var visitorMethods = visitor.GetType().GetMethods().Where(x => {
                    // 名前はVisit?
                    if (x.Name != "Visit") {
                        return false;
                    }

                    // 戻り値型が同じ？
                    if (x.ReturnType.Equals(typeof(void)) == false) {
                        return false;
                    }

                    // 引数は1個？
                    var parameters = x.GetParameters();
                    if (parameters.Length != 1) {
                        return false;
                    }

                    // 第1引数がselfと同じ型？
                    if (parameters[0].ParameterType.Equals(self.GetType()) == false) {
                        return false;
                    }

                    return true;
                }).ToArray();

                if (visitorMethods.Length == 0) {
                    throw new MissingMethodException($"型 {visitor.GetType().ToString()} には Action<{self.GetType().ToString()}>型の メソッド Visit が実装されていません。");
                } else if (visitorMethods.Length >= 2) {
                    throw new MissingMethodException($"型 {visitor.GetType().ToString()} には Action<{self.GetType().ToString()}>型の メソッド Visit が２個以上実装されています。");
                }

                visitMethod = visitorMethods.First();
                Memoise[memoKey] = visitMethod;
            }
            visitMethod.Invoke(visitor, new object[] { self });
        }

        public static TResult Accept<TResult, TArgument>(this object self, object visitor, TArgument arg) {
            if (self == null) {
                return default(TResult);
            }

            var memoKey = Tuple.Create(2, self.GetType(), visitor.GetType());

            MethodInfo visitMethod;

            if (Memoise.TryGetValue(memoKey, out visitMethod) == false) {
                var visitorMethods = visitor.GetType().GetMethods().Where(x => {
                    // 名前はVisit?
                    if (x.Name != "Visit") {
                        return false;
                    }

                    // 戻り値型が同じ？
                    if (x.ReturnType.Equals(typeof(TResult)) == false) {
                        return false;
                    }

                    // 引数は2個？
                    var parameters = x.GetParameters();
                    if (parameters.Length != 2) {
                        return false;
                    }

                    // 第1引数がselfと同じ型？
                    if (parameters[0].ParameterType.Equals(self.GetType()) == false) {
                        return false;
                    }

                    // 第2引数がTArgumentと同じ型？
                    if (parameters[1].ParameterType.Equals(typeof(TArgument)) == false) {
                        return false;
                    }

                    return true;
                }).ToArray();

                if (visitorMethods.Length == 0) {
                    throw new MissingMethodException($"型 {visitor.GetType().ToString()} には Func<{self.GetType().ToString()},{typeof(TArgument).ToString()},{typeof(TResult).ToString()}>型の メソッド Visit が実装されていません。");
                } else if (visitorMethods.Length >= 2) {
                    throw new MissingMethodException($"型 {visitor.GetType().ToString()} には Func<{self.GetType().ToString()},{typeof(TArgument).ToString()},{typeof(TResult).ToString()}>型の メソッド Visit が２個以上実装されています。");
                }
                visitMethod = visitorMethods.First();
                Memoise[memoKey] = visitMethod;
            }
            return (TResult)visitMethod.Invoke(visitor, new object[] { self, arg });

        }
        public static TResult Accept<TResult>(this object self, object visitor) {
            if (self == null) {
                return default(TResult);
            }
            var memoKey = Tuple.Create(3, self.GetType(), visitor.GetType());

            MethodInfo visitMethod;

            if (Memoise.TryGetValue(memoKey, out visitMethod) == false) {
                var visitorMethods = visitor.GetType().GetMethods().Where(x => {
                    // 名前はVisit?
                    if (x.Name != "Visit") {
                        return false;
                    }

                    // 戻り値型が同じ？
                    if (x.ReturnType == typeof(TResult) == false) {
                        return false;
                    }

                    // 引数は1個？
                    var parameters = x.GetParameters();
                    if (parameters.Length != 1) {
                        return false;
                    }

                    // 第1引数がselfと同じ型？
                    if (!(parameters[0].ParameterType == self.GetType())) {
                        return false;
                    }

                    return true;
                }).ToArray();

                if (visitorMethods.Length == 0) {
                    throw new MissingMethodException($"型 {visitor.GetType().ToString()} には Func<{self.GetType().ToString()},{typeof(TResult).ToString()}>型の メソッド Visit が実装されていません。");
                } else if (visitorMethods.Length >= 2) {
                    throw new MissingMethodException($"型 {visitor.GetType().ToString()} には Func<{self.GetType().ToString()},{typeof(TResult).ToString()}>型の メソッド Visit が２個以上実装されています。");
                }
                visitMethod = visitorMethods.First();
                Memoise[memoKey] = visitMethod;
            }
            return (TResult)visitMethod.Invoke(visitor, new object[] { self });
        }
    }

}
