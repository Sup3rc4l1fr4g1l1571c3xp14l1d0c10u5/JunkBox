using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

namespace CParser2 {

    public class WriterVisitor {
        public WriterVisitor() {
            
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
            return $"{self.@operator}({ self.operand.Accept<string>(this)})";
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
            return self.literal;
        }
        public string Visit(SyntaxNode.Expression.PrimaryExpression.GroupedExpression self) {
            return $"({ self.expression.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.PostfixExpression.ArraySubscriptExpression self) {
            return $"{ self.expression.Accept<string>(this)}[{self.array_subscript.Accept<string>(this)}]";
        }
        public string Visit(SyntaxNode.Expression.PostfixExpression.FunctionCallExpression self) {
            return $"{ self.expression.Accept<string>(this)}({string.Join(", ", self.argument_expressions.Select(x => x.Accept<string>(this)))})";
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
            return $"(type_name) {{ {string.Join(", ", self.initializers.Select(x => ($"{x.Item1.Accept<string>(this)} = {x.Item2.Accept<string>(this)}")))} }})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.op} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.SimpleAssignmentExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.op} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.LogicalOrExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.op} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.LogicalAndExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.op} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.InclusiveOrExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.op} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.ExclusiveOrExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.op} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.AndExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.op} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.EqualityExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.op} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.RelationalExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.op} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.ShiftExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.op} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.AdditiveExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.op} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.BinaryExpression.MultiplicativeExpression self) {
            return $"({ self.lhs_operand.Accept<string>(this)} {self.op} { self.rhs_operand.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.ConditionalExpression self) {
            return $"({ self.condition.Accept<string>(this)} ? {self.then_expression.Accept<string>(this)} : { self.else_expression.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Expression.PostfixExpression.CommaSeparatedExpression self) {
            return $"({string.Join(", ", self.exprs.Select(x => $"{x.Accept<string>(this)}"))})";
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
            return String.Concat(items);
        }
        public string Visit(SyntaxNode.FunctionDeclaration self) {
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.init_declarator.Accept<string>(this)};\r\n";
        }
        public string Visit(SyntaxNode.VariableDeclaration self) {
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.declarator.Accept<string>(this)};\r\n";
        }
        public string Visit(SyntaxNode.Definition.FunctionDefinition.KandRFunctionDefinition self) {
            var pars = (self.parameterDefinition != null) ? "\r\n" + string.Concat(self.parameterDefinition.Select(x => x.Accept<string>(this) + ";\r\n")) : "";
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.declarator.Accept<string>(this)}{pars}{self.function_body.Accept<string>(this)}\r\n";
            //return $"{self.declaration_specifiers.Accept<string>(this)} {self.declarator.Accept<string>(this)}{pars}{self.function_body.Accept<string>(this)}\r\n";
        }
        public string Visit(SyntaxNode.Definition.FunctionDefinition.AnsiFunctionDefinition self) {
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.declarator.Accept<string>(this)} {self.function_body.Accept<string>(this)}\r\n";
            //return $"{self.declaration_specifiers.Accept<string>(this)} {self.declarator.Accept<string>(this)}({string.Join(", ", self.parameterDefinition.Select(x => x.Accept<string>(this)))}) {self.function_body.Accept<string>(this)}\r\n";
        }
        public string Visit(SyntaxNode.Definition.VariableDefinition self) {
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.init_declarator.Accept<string>(this)};\r\n";
        }
        public string Visit(SyntaxNode.Definition.ParameterDefinition self) {
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.declarator.Accept<string>(this)}";
        }
        public string Visit(SyntaxNode.TypeDeclaration.TypedefDeclaration self) {
            return $"{self.declaration_specifiers.Accept<string>(this)} {self.init_declarator.Accept<string>(this)};\r\n";
        }
        public string Visit(SyntaxNode.TypeDeclaration.StructTypeDeclaration self) {
            var ss = self.struct_specifier;
            if (ss.struct_declarations != null) {
                var items = ss.struct_declarations.Select(x => x.Accept<string>(this)).ToList();
                return $"struct {self.identifier} {{\r\n" + string.Concat(items) + "};\r\n";
            } else {
                return $"struct {self.identifier};\r\n";
            }
        }
        public string Visit(SyntaxNode.TypeDeclaration.UnionTypeDeclaration self) {
            var ss = self.union_specifier;
            if (ss.struct_declarations != null) {
                var items = ss.struct_declarations.Select(x => x.Accept<string>(this)).ToList();
                return $"union {self.identifier} {{\r\n" + string.Concat(items) + "};\r\n";
            } else {
                return $"union {self.identifier};\r\n";
            }
        }
        public string Visit(SyntaxNode.TypeDeclaration.EnumTypeDeclaration self) {
            var ss = self.enum_specifier;
            if (ss.enumerators != null) {
                var items = ss.enumerators.Select(x => x.Accept<string>(this)).ToList();
                return $"enum {self.identifier} {{\r\n" + string.Concat(items) + "};\r\n";
            } else {
                return $"enum {self.identifier};\r\n";
            }
        }
        public string Visit(SyntaxNode.DeclarationSpecifiers self) {
            List<string> specs = new List<string>();
            if (self.storage_class_specifier != null) { specs.Add(self.storage_class_specifier); }
            if (self.type_qualifiers != null) { specs.AddRange(self.type_qualifiers); }
            if (self.type_specifiers != null) { specs.AddRange(self.type_specifiers.Select(x => x.Accept<string>(this))); }
            if (self.function_specifier != null) { specs.Add(self.function_specifier); }
            return String.Join(" ", specs);
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
            return $"{self.identifier}";
        }
        public string Visit(SyntaxNode.TypeDeclaration.TypeSpecifier.TypedefTypeSpecifier self) {
            return $"{self.identifier}";
        }
        public string Visit(SyntaxNode.StructDeclaration self) {
            var items = new List<string>();
            if (self.items != null) {
                items.AddRange(self.items.Select(x => x.Accept<string>(this) + ";\r\n"));
            }
            return String.Concat(items);
        }
        public string Visit(SyntaxNode.MemberDeclaration self) {
            return $"{ self.specifier_qualifier_list.Accept<string>(this)} { self.struct_declarator.Accept<string>(this)}";
        }
        public string Visit(SyntaxNode.SpecifierQualifierList self) {
            var items = new List<string>();
            if (self.type_qualifiers != null) {
                items.AddRange(self.type_qualifiers);
            }
            if (self.type_specifiers != null) {
                items.AddRange(self.type_specifiers.Select(x => x.Accept<string>(this)));
            }
            return String.Join(" ", items);
        }
        public string Visit(SyntaxNode.StructDeclarator self) {
            if (self.expression != null) {
                return $"{self.declarator.Accept<string>(this)} : {self.expression.Accept<string>(this)}";
            } else {
                return $"{self.declarator.Accept<string>(this)}";
            }
        }
        public string Visit(SyntaxNode.EnumSpecifier self) {
            string items = "";
            if (self.enumerators != null) {
                items = " {\r\n" + String.Concat(self.enumerators.Select(x => x.Accept<string>(this) + ",\r\n")) + "}";
            }
            return $"enum {self.identifier}{items};";
        }
        public string Visit(SyntaxNode.Enumerator self) {
            return self.identifier + ((self.expression != null) ? " = " + self.expression.Accept<string>(this) : "");
        }
        public string Visit(SyntaxNode.Declarator.GroupedDeclarator self) {
            return $"({self.@base.Accept<string>(this)})";
        }
        public string Visit(SyntaxNode.Declarator.IdentifierDeclarator self) {
            var ptr = (self.pointer) != null ? (String.Join(" ", self.pointer) + " ") : "";
            return ptr + self.identifier;
        }
        public string Visit(SyntaxNode.Declarator.ArrayDeclarator self) {
            var ptr  = (self.pointer == null) ? "" : (String.Join(" ", self.pointer) + " ");
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
                parts.AddRange(self.pointer);
            }
            if (self.@base != null) {
                parts.Add(self.@base.Accept<string>(this));
            }
            return string.Join(" ", parts);
        }
        public string Visit(SyntaxNode.Declarator.AbstractDeclarator.GroupedAbstractDeclarator self) {
            string p = (self.pointer != null) ? (String.Concat(self.pointer)) : "";
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
            return $"default:\r\n"+self.statement.Accept<string>(this);
        }
        public string Visit(SyntaxNode.Statement.LabeledStatement.CaseLabeledStatement self) {
            return $"case {self.expression.Accept<string>(this)}:\r\n" + self.statement.Accept<string>(this);
        }
        public string Visit(SyntaxNode.Statement.LabeledStatement.GenericLabeledStatement self) {
            return $"{self.label}:\r\n" + self.statement.Accept<string>(this);
        }
        public string Visit(SyntaxNode.Statement.CompoundStatement self) {
            var items = new List<string>();
            items.Add("{");
            if (self.block_items != null) { items.AddRange(self.block_items.Select(x => x.Accept<string>(this))); }
            items.Add("}");
            return String.Join("\r\n", items);
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
            return self.external_declarations != null ? String.Concat(self.external_declarations.Select(x => x.Accept<string>(this))) : "";
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
                inits.AddRange(self.initializers.Select(x => (x.Item1 != null ? x.Item1.Accept<string>(this) + " = "  : "") + x.Item2.Accept<string>(this) + ",\r\n"));
            }
            return self.expression.Accept<string>(this) + (inits.Any() ? "{" + String.Concat(inits) + "}" : "");
        }
        public string Visit(SyntaxNode.Initializer.Designator.MemberDesignator self) {
            return "." + self.identifier;
        }
        public string Visit(SyntaxNode.Initializer.Designator.IndexDesignator self) {
            return $"[{self.expression.Accept<string>(this)}]";
        }

    }

    [DataContract]
    public abstract class SyntaxNode {

        [DataContract]
        public abstract class Expression : SyntaxNode {

            public bool full {
                get; set;
            }

            [DataContract]
            public abstract class UnaryExpression : Expression {

                [DataContract]
                public class PrefixIncrementExpression : UnaryExpression {

                    [DataMember]
                    public Expression operand {
                        get; private set;
                    }

                    public PrefixIncrementExpression(Expression operand) {

                        this.operand = operand;
                    }
                }

                [DataContract]
                public class PrefixDecrementExpression : UnaryExpression {

                    [DataMember]
                    public Expression operand {
                        get; private set;
                    }

                    public PrefixDecrementExpression(Expression operand) {

                        this.operand = operand;
                    }
                }

                [DataContract]
                public class AddressExpression : UnaryExpression {

                    [DataMember]
                    public Expression operand {
                        get; private set;
                    }


                    public AddressExpression(Expression operand) {

                        this.operand = operand;
                    }
                }

                [DataContract]
                public class IndirectionExpression : UnaryExpression {

                    [DataMember]
                    public Expression operand {
                        get; private set;
                    }


                    public IndirectionExpression(Expression operand) {

                        this.operand = operand;
                    }
                }

                [DataContract]
                public class UnaryArithmeticExpression : UnaryExpression {

                    [DataMember]
                    public string @operator {
                        get; private set;
                    }
                    [DataMember]
                    public Expression operand {
                        get; private set;
                    }


                    public UnaryArithmeticExpression(string @operator, Expression operand) {

                        this.@operator = @operator;
                        this.operand = operand;
                    }
                }

                [DataContract]
                public class SizeofExpression : UnaryExpression {

                    [DataMember]
                    public Expression operand {
                        get; private set;
                    }


                    public SizeofExpression(Expression operand) {

                        this.operand = operand;
                    }
                }

                [DataContract]
                public class SizeofTypeExpression : UnaryExpression {

                    [DataMember]
                    public TypeName operand {
                        get; private set;
                    }


                    public SizeofTypeExpression(TypeName operand) {

                        this.operand = operand;
                    }
                }

            }

            [DataContract]
            public abstract class PrimaryExpression : Expression {

                [DataContract]
                public class ObjectSpecifier : PrimaryExpression {

                    [DataMember]
                    public string identifier {
                        get; private set;
                    }

                    public ObjectSpecifier(string identifier) {

                        this.identifier = identifier;
                    }
                }

                [DataContract]
                public class ConstantSpecifier : PrimaryExpression {

                    [DataMember]
                    public string constant {
                        get; private set;
                    }

                    public ConstantSpecifier(string constant) {

                        this.constant = constant;
                    }
                }

                [DataContract]
                public class StringLiteralSpecifier : PrimaryExpression {

                    [DataMember]
                    public string literal {
                        get; private set;
                    }

                    public StringLiteralSpecifier(string literal) {

                        this.literal = literal;
                    }
                }

                [DataContract]
                public class GroupedExpression : PrimaryExpression {

                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }

                    public GroupedExpression(Expression expression) {

                        this.expression = expression;
                    }
                }

            }

            [DataContract]
            public abstract class PostfixExpression : Expression {

                [DataContract]
                public class ArraySubscriptExpression : PostfixExpression {

                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }
                    [DataMember]
                    public Expression array_subscript {
                        get; private set;
                    }

                    public ArraySubscriptExpression(Expression expression, Expression arraySubscript) {

                        this.expression = expression;
                        array_subscript = arraySubscript;
                    }
                }

                [DataContract]
                public class FunctionCallExpression : PostfixExpression {

                    [DataMember]
                    public IReadOnlyList<Expression> argument_expressions {
                        get; private set;
                    }
                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }

                    public FunctionCallExpression(Expression expression, IReadOnlyList<Expression> argumentExpressions) {

                        this.expression = expression;
                        argument_expressions = argumentExpressions;
                    }
                }

                [DataContract]
                public class MemberAccessByValueExpression : PostfixExpression {

                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }
                    [DataMember]
                    public string identifier {
                        get; private set;
                    }

                    public MemberAccessByValueExpression(Expression expression, string identifier) {

                        this.expression = expression;
                        this.identifier = identifier;
                    }
                }

                [DataContract]
                public class MemberAccessByPointerExpression : PostfixExpression {

                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }
                    [DataMember]
                    public string identifier {
                        get; private set;
                    }

                    public MemberAccessByPointerExpression(Expression expression, string identifier) {

                        this.expression = expression;
                        this.identifier = identifier;
                    }
                }

                [DataContract]
                public class PostfixIncrementExpression : PostfixExpression {

                    [DataMember]
                    public Expression operand {
                        get; private set;
                    }

                    public PostfixIncrementExpression(Expression operand) {

                        this.operand = operand;
                    }
                }

                [DataContract]
                public class PostfixDecrementExpression : PostfixExpression {

                    [DataMember]
                    public Expression operand {
                        get; private set;
                    }

                    public PostfixDecrementExpression(Expression x) {

                        operand = x;
                    }
                }

                [DataContract]
                public class CompoundLiteralExpression : PostfixExpression {

                    [DataMember]
                    public TypeName type_name {
                        get; private set;
                    }
                    [DataMember]
                    public IReadOnlyList<Tuple<IReadOnlyList<SyntaxNode.Initializer.Designator>, SyntaxNode.Initializer>> initializers {
                        get; private set;
                    }

                    public CompoundLiteralExpression(TypeName typeName, IReadOnlyList<Tuple<IReadOnlyList<SyntaxNode.Initializer.Designator>, SyntaxNode.Initializer>> initializers) {

                        this.type_name = typeName;
                        this.initializers = initializers;
                    }
                }

            }

            [DataContract]
            public abstract class BinaryExpression : Expression {

                [DataMember]
                public string op {
                    get; private set;
                }
                [DataMember]
                public Expression lhs_operand {
                    get; private set;
                }
                [DataMember]
                public Expression rhs_operand {
                    get; private set;
                }

                protected BinaryExpression(string op, Expression lhs_operand, Expression rhs_operand) {

                    this.op = op;
                    this.lhs_operand = lhs_operand;
                    this.rhs_operand = rhs_operand;
                }

                [DataContract]
                public class CompoundAssignmentExpression : BinaryExpression {

                    public CompoundAssignmentExpression(string op, Expression lhs_operand, Expression rhs_operand) : base(op, lhs_operand, rhs_operand) {
                    }
                }

                [DataContract]
                public class SimpleAssignmentExpression : BinaryExpression {

                    public SimpleAssignmentExpression(string op, Expression lhs_operand, Expression rhs_operand) : base(op, lhs_operand, rhs_operand) {
                    }
                }

                [DataContract]
                public class LogicalOrExpression : BinaryExpression {

                    public LogicalOrExpression(string op, Expression lhs_operand, Expression rhs_operand) : base(op, lhs_operand, rhs_operand) {
                    }
                }

                [DataContract]
                public class LogicalAndExpression : BinaryExpression {

                    public LogicalAndExpression(string op, Expression lhs_operand, Expression rhs_operand) : base(op, lhs_operand, rhs_operand) {
                    }
                }

                [DataContract]
                public class InclusiveOrExpression : BinaryExpression {

                    public InclusiveOrExpression(string op, Expression lhs_operand, Expression rhs_operand) : base(op, lhs_operand, rhs_operand) {
                    }
                }

                [DataContract]
                public class ExclusiveOrExpression : BinaryExpression {

                    public ExclusiveOrExpression(string op, Expression lhs_operand, Expression rhs_operand) : base(op, lhs_operand, rhs_operand) {
                    }
                }

                [DataContract]
                public class AndExpression : BinaryExpression {

                    public AndExpression(string op, Expression lhs_operand, Expression rhs_operand) : base(op, lhs_operand, rhs_operand) {
                    }
                }

                [DataContract]
                public class EqualityExpression : BinaryExpression {

                    public EqualityExpression(string op, Expression lhs_operand, Expression rhs_operand) : base(op, lhs_operand, rhs_operand) {
                    }
                }

                [DataContract]
                public class RelationalExpression : BinaryExpression {

                    public RelationalExpression(string op, Expression lhs_operand, Expression rhs_operand) : base(op, lhs_operand, rhs_operand) {
                    }
                }

                [DataContract]
                public class ShiftExpression : BinaryExpression {

                    public ShiftExpression(string op, Expression lhs_operand, Expression rhs_operand) : base(op, lhs_operand, rhs_operand) {
                    }
                }

                [DataContract]
                public class AdditiveExpression : BinaryExpression {

                    public AdditiveExpression(string op, Expression lhs_operand, Expression rhs_operand) : base(op, lhs_operand, rhs_operand) {
                    }
                }

                [DataContract]
                public class MultiplicativeExpression : BinaryExpression {

                    public MultiplicativeExpression(string op, Expression lhs_operand, Expression rhs_operand) : base(op, lhs_operand, rhs_operand) {
                    }
                }
            }

            [DataContract]
            public class ConditionalExpression : Expression {

                [DataMember]
                public Expression condition {
                    get; private set;
                }
                [DataMember]
                public Expression then_expression {
                    get; private set;
                }
                [DataMember]
                public Expression else_expression {
                    get; private set;
                }

                public ConditionalExpression(Expression condition, Expression thenExpression, Expression elseExpression) {

                    this.condition = condition;
                    this.then_expression = thenExpression;
                    this.else_expression = elseExpression;
                }
            }

            [DataContract]
            public class CommaSeparatedExpression : Expression {

                [DataMember]
                public IReadOnlyList<Expression> exprs {
                    get; private set;
                }

                public CommaSeparatedExpression(IReadOnlyList<Expression> exprs) {

                    this.exprs = exprs;
                }
            }

            [DataContract]
            public class CastExpression : Expression {

                [DataMember]
                public Expression operand {
                    get; private set;
                }
                [DataMember]
                public TypeName type_name {
                    get; private set;
                }

                public CastExpression(TypeName typeName, Expression operand) {

                    this.type_name = typeName;
                    this.operand = operand;
                }
            }

            [DataContract]
            public class ErrorExpression : Expression {

                [DataMember]
                public Statement statement {
                    get; private set;
                }

                public ErrorExpression(Statement x) {
                    this.statement = x;
                }
            }
        }

        [DataContract]
        public class Declaration : SyntaxNode {

            [DataMember]
            public DeclarationSpecifiers declaration_specifiers {
                get; private set;
            }
            [DataMember]
            public IReadOnlyList<InitDeclarator> init_declarators {
                get; private set;
            }
            [DataMember]
            public IReadOnlyList<SyntaxNode> items {
                get; private set;
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

            private static List<Definition.VariableDefinition> build_variable_definition(DeclarationSpecifiers dcl_specs, IReadOnlyList<InitDeclarator> init_dcrs) {

                var var_defs = new List<Definition.VariableDefinition>();
                if (dcl_specs != null && (dcl_specs.storage_class_specifier == "extern" || dcl_specs.storage_class_specifier == "typedef") ) {

                    return var_defs;
                }

                foreach (var init_dcr in init_dcrs) {

                    if (init_dcr.declarator.isvariable) {

                        var_defs.Add(new Definition.VariableDefinition(dcl_specs, init_dcr));
                    }
                }
                return var_defs;
            }


            private static List<VariableDeclaration> build_variable_declaration(DeclarationSpecifiers dcl_specs, IReadOnlyList<InitDeclarator> init_dcrs) {

                var var_dcls = new List<VariableDeclaration>();
                if (dcl_specs == null || dcl_specs.storage_class_specifier != "extern") {

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

            private static List<FunctionDeclaration> build_function_declaration(DeclarationSpecifiers dcl_specs, IReadOnlyList<InitDeclarator> init_dcrs) {

                var func_dcls = new List<FunctionDeclaration>();
                if (dcl_specs != null && dcl_specs.storage_class_specifier == "typedef") {

                    return func_dcls;
                }

                foreach (var init_dcr in init_dcrs) {

                    if (init_dcr.declarator.isfunction()) {

                        func_dcls.Add(new FunctionDeclaration(dcl_specs, init_dcr));
                    }
                }
                return func_dcls;
            }

            private static List<TypeDeclaration> build_type_declaration(DeclarationSpecifiers dcl_specs, IReadOnlyList<InitDeclarator> init_dcrs) {

                var type_dcls = new List<TypeDeclaration>();
                if (dcl_specs == null) {

                    return type_dcls;
                }
                dcl_specs.type_specifiers.ForEach(type_spec => {

                    var builder = new TypeDeclarationBuilder();
                    type_spec.Accept(builder);
                    type_dcls.AddRange(builder.type_declarations);
                });

                var sc = dcl_specs.storage_class_specifier;

                if (sc == "typedef") {

                    foreach (var init_dcr in init_dcrs) {

                        var id = init_dcr.declarator.identifier;
                        type_dcls.Add(new TypeDeclaration.TypedefDeclaration(dcl_specs, init_dcr));
                    }
                }

                return type_dcls;
            }
        }

        public class TypeDeclarationBuilder {

            public TypeDeclarationBuilder() {

                type_declarations = new List<TypeDeclaration>();
            }

            public List<TypeDeclaration> type_declarations {
                get; 
            }

            public void Visit(TypeSpecifier.StandardTypeSpecifier node) {

            }

            public void Visit(TypeSpecifier.TypedefTypeSpecifier node) {

            }

            public void Visit(TypeSpecifier.StructSpecifier node) {

                if (node.struct_declarations != null) {

                    foreach (var child in node.struct_declarations) {
                        child.Accept(this);
                    }
                    type_declarations.Add(new TypeDeclaration.StructTypeDeclaration(node));
                }
            }

            public void Visit(TypeSpecifier.UnionSpecifier node) {

                if (node.struct_declarations != null) {

                    foreach (var child in node.struct_declarations) {
                        child.Accept(this);
                    }
                    type_declarations.Add(new TypeDeclaration.UnionTypeDeclaration(node));
                }
            }

            public void Visit(EnumSpecifier node) {

                if (node.enumerators != null) {

                    type_declarations.Add(new TypeDeclaration.EnumTypeDeclaration(node));
                }
            }

            //private void  Visit(TypeDeclarationBuilder node) {

            //}

            public void Visit(StructDeclaration node) {

                node.specifier_qualifier_list.Accept(this);
            }

            public void Visit(SpecifierQualifierList node) {

                foreach (var child in node.type_specifiers) {
                    child.Accept(this);
                };
            }
        }


        [DataContract]
        public class FunctionDeclaration : SyntaxNode {

            [DataMember]
            public DeclarationSpecifiers declaration_specifiers {
                get; private set;
            }
            [DataMember]
            public InitDeclarator init_declarator {
                get; private set;
            }
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

        [DataContract]
        public class VariableDeclaration : SyntaxNode {

            [DataMember]
            public DeclarationSpecifiers declaration_specifiers {
                get; private set;
            }
            [DataMember]
            public Declarator declarator {
                get; private set;
            }

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

        [DataContract]
        public abstract class Definition : SyntaxNode {

            protected Definition(DeclarationSpecifiers dcl_specs) {

                declaration_specifiers = dcl_specs;
            }

            [DataMember]
            public DeclarationSpecifiers declaration_specifiers {
                get; private set;
            }

            [DataContract]
            public abstract class FunctionDefinition : Definition {

                [DataMember]
                public Declarator declarator {
                    get; private set;
                }
                [DataMember]
                public IReadOnlyList<ParameterDefinition> parameterDefinition {
                    get; private set;
                }
                [DataMember]
                public Statement function_body {
                    get; private set;
                }

                protected FunctionDefinition(DeclarationSpecifiers dcl_specs, Declarator dcr, IReadOnlyList<ParameterDefinition> param_defs, Statement compound_stmt) : base(dcl_specs) {

                    declarator = dcr;
                    parameterDefinition = param_defs;
                    function_body = compound_stmt;
                }

                [DataContract]
                public class KandRFunctionDefinition : FunctionDefinition {

                    public KandRFunctionDefinition(DeclarationSpecifiers dcl_specs, Declarator dcr, List<Declaration> dcls,
                        Statement compound_stmt) : base(dcl_specs, dcr, create_parameters(dcr.identifier_list, dcls), compound_stmt) {

                    }

                    public IReadOnlyList<string> identifier_list {

                        get {
                            return declarator.identifier_list;
                        }
                    }

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

                    private static VariableDefinition find_variable_definition(List<Declaration> dcls, string name) {

                        foreach (var dcl in dcls) {

                            foreach (var var_def in dcl.items.Where(item => item is VariableDefinition).Cast<VariableDefinition>()) {

                                if (var_def.identifier == name) {

                                    return var_def;
                                }
                            }
                        }
                        {

                            var dcl = implicit_parameter_definition(name);
                            dcls.Add(dcl);
                            Debug.Assert(dcl.items.First() is VariableDefinition);
                            return dcl.items.First() as VariableDefinition;
                        }
                    }

                    private static ParameterDefinition variable_definition_to_parameter_definition(VariableDefinition var_def) {

                        var dcl_specs = var_def.declaration_specifiers;
                        var dcr = var_def.init_declarator.declarator;
                        var param_def = new ParameterDefinition(dcl_specs, dcr);
                        return param_def;
                    }

                    private static Declaration implicit_parameter_definition(string id) {

                        var init_dcr = new InitDeclarator(new Declarator.IdentifierDeclarator(id), null);
                        return new Declaration(null, new[] { init_dcr });
                    }

                }

                [DataContract]
                public class AnsiFunctionDefinition : FunctionDefinition {


                    public AnsiFunctionDefinition(DeclarationSpecifiers dcl_specs, Declarator dcr, Statement compound_stmt)
                        : base(dcl_specs, dcr, create_parameters(dcr.innermost_parameter_type_list), compound_stmt) {

                    }

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

            [DataContract]
            public class VariableDefinition : Definition {

                [DataMember]
                public InitDeclarator init_declarator {
                    get; private set;
                }

                public VariableDefinition(DeclarationSpecifiers dcl_specs, InitDeclarator init_dcr) : base(dcl_specs) {

                    init_declarator = init_dcr;
                }

                public string identifier {

                    get {
                        return init_declarator.declarator.identifier;
                    }
                }

            }

            [DataContract]
            public class ParameterDefinition : Definition {

                [DataMember]
                public Declarator declarator {
                    get; private set;
                }

                public ParameterDefinition(DeclarationSpecifiers dcl_specs, Declarator dcr) : base(dcl_specs) {

                    declarator = dcr;
                }

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

        [DataContract]
        public abstract class TypeDeclaration : SyntaxNode {

            [DataMember]
            public abstract string identifier {
                get; protected set;
            }

            [DataContract]
            public class TypedefDeclaration : TypeDeclaration {

                [DataMember]
                public DeclarationSpecifiers declaration_specifiers {
                    get; private set;
                }
                [DataMember]
                public InitDeclarator init_declarator {
                    get; private set;
                }
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

            [DataContract]
            public class StructTypeDeclaration : TypeDeclaration {

                public override string identifier {
                    get {
                        return struct_specifier.identifier;
                    }
                    protected set {
                    }
                }
                [DataMember]
                public TypeSpecifier.StructSpecifier struct_specifier {
                    get; private set;
                }

                public StructTypeDeclaration(TypeSpecifier.StructSpecifier node) {

                    struct_specifier = node;
                }
            }

            [DataContract]
            public class UnionTypeDeclaration : TypeDeclaration {

                public override string identifier {
                    get {
                        return union_specifier.identifier;
                    }
                    protected set {
                    }
                }
                [DataMember]
                public TypeSpecifier.UnionSpecifier union_specifier {
                    get; private set;
                }

                public UnionTypeDeclaration(TypeSpecifier.UnionSpecifier node) {

                    union_specifier = node;
                }
            }

            [DataContract]
            public class EnumTypeDeclaration : TypeDeclaration {

                public override string identifier {
                    get {
                        return enum_specifier.identifier;
                    }
                    protected set {}
                }
                [DataMember]
                public EnumSpecifier enum_specifier {
                    get; private set;
                }

                public EnumTypeDeclaration(EnumSpecifier node) {

                    enum_specifier = node;
                }
            }
        }

        [DataContract]
        public class DeclarationSpecifiers {
            [DataMember]
            public string storage_class_specifier {
                get; internal set;
            }
            [DataMember]
            public List<TypeSpecifier> type_specifiers {
                get; private set;
            }
            [DataMember]
            public List<string> type_qualifiers {
                get; private set;
            }
            [DataMember]
            public string function_specifier {
                get; internal set;
            }
            public DeclarationSpecifiers() {
                type_specifiers = new List<TypeSpecifier>();
                type_qualifiers = new List<string>();
            }
            public bool isexplicitly_typed {
                get {
                    return !isimplicitly_typed;
                }
            }
            public bool isimplicitly_typed {
                get {
                    return !type_specifiers.Any();
                }
            }

        }

        [DataContract]
        public class InitDeclarator {

            [DataMember]
            public Declarator declarator {
                get; private set;
            }
            [DataMember]
            public Initializer initializer {
                get; private set;
            }

            public InitDeclarator(Declarator _1, Initializer _2) {

                declarator = _1;
                initializer = _2;
            }
        }

        [DataContract]
        public abstract class TypeSpecifier {

            [DataContract]
            public class StructSpecifier : TypeSpecifier {

                [DataMember]
                public string identifier {
                    get; private set;
                }
                [DataMember]
                public bool v2 {
                    get; private set;
                }
                [DataMember]
                public IReadOnlyList<StructDeclaration> struct_declarations {
                    get; private set;
                }

                public StructSpecifier(string v1, IReadOnlyList<StructDeclaration> _3, bool v2) {

                    identifier = v1;
                    struct_declarations = _3;
                    this.v2 = v2;
                }
            }

            [DataContract]
            public class UnionSpecifier : TypeSpecifier {

                [DataMember]
                public string identifier {
                    get; private set;
                }
                [DataMember]
                public bool v2 {
                    get; private set;
                }
                [DataMember]
                public IReadOnlyList<StructDeclaration> struct_declarations {
                    get; private set;
                }

                public UnionSpecifier(string v1, IReadOnlyList<StructDeclaration> _3, bool v2) {

                    identifier = v1;
                    struct_declarations = _3;
                    this.v2 = v2;
                }
            }

            [DataContract]
            public class StandardTypeSpecifier : TypeSpecifier {

                [DataMember]
                public string identifier {
                    get; private set;
                }

                public StandardTypeSpecifier(string s) {

                    identifier = s;
                }
            }

            [DataContract]
            public class TypedefTypeSpecifier : TypeSpecifier {

                [DataMember]
                public string identifier {
                    get; private set;
                }

                public TypedefTypeSpecifier(string s) {

                    identifier = s;
                }
            }
        }

        [DataContract]
        public class StructDeclaration {

            [DataMember]
            public SpecifierQualifierList specifier_qualifier_list {
                get; private set;
            }
            [DataMember]
            public IReadOnlyList<StructDeclarator> struct_declarators {
                get; private set;
            }
            [DataMember]
            public IReadOnlyList<MemberDeclaration> items {
                get; private set;
            }

            public StructDeclaration(SpecifierQualifierList _1, IReadOnlyList<StructDeclarator> _2) {

                specifier_qualifier_list = _1;
                struct_declarators = _2;
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

        [DataContract]
        public class MemberDeclaration {

            [DataMember]
            public SpecifierQualifierList specifier_qualifier_list {
                get; private set;
            }

            [DataMember]
            public StructDeclarator struct_declarator {
                get; private set;
            }

            //[DataMember] 
            // public StructDeclarator type { 
            //    get; 
            //}

            public MemberDeclaration(SpecifierQualifierList spec_qual_list, StructDeclarator struct_dcr) {

                specifier_qualifier_list = spec_qual_list;
                struct_declarator = struct_dcr;
            }

            public string identifier() {

                if (struct_declarator != null && struct_declarator.declarator != null) {

                    return struct_declarator.declarator.identifier;
                }
                return null;
            }

        }

        [DataContract]
        public class SpecifierQualifierList {
            [DataMember]
            public List<TypeSpecifier> type_specifiers {
                get; private set;
            }
            [DataMember]
            public List<string> type_qualifiers {
                get; private set;
            }
            public SpecifierQualifierList() {
                type_specifiers = new List<TypeSpecifier>();
                type_qualifiers = new List<string>();
            }
        }

        [DataContract]
        public class StructDeclarator : SyntaxNode {

            [DataMember]
            public Declarator declarator {
                get; private set;
            }
            [DataMember]
            public Expression expression {
                get; private set;
            }

            public StructDeclarator(Declarator _1, Expression _2) {

                declarator = _1;
                expression = _2;
            }
        }

        [DataContract]
        public class EnumSpecifier : TypeSpecifier {

            [DataMember]
            public string identifier {
                get; private set;
            }
            [DataMember]
            public IReadOnlyList<Enumerator> enumerators {
                get; private set;
            }
            [DataMember]
            public bool trailing_comma {
                get; private set;
            }
            [DataMember]
            public bool anonymous {
                get; private set;
            }

            public EnumSpecifier(string identifier, IReadOnlyList<Enumerator> enumerators, bool trailingComma, bool anonymous) {

                this.identifier = identifier;
                this.enumerators = enumerators;
                this.trailing_comma = trailingComma;
                this.anonymous = anonymous;
            }
        }

        [DataContract]
        public class Enumerator {

            [DataMember]
            public string identifier {
                get; private set;
            }
            [DataMember]
            public Expression expression {
                get; private set;
            }

            public Enumerator(string identifier, Expression expression) {

                this.identifier = identifier;
                this.expression = expression;
            }
        }

        [DataContract]
        public abstract class Declarator {

            [DataMember]
            public Declarator @base {
                get; private set;
            }

            [DataMember]
            public bool full {
                get; set;
            }

            [DataMember]
            public IReadOnlyList<string> pointer {
                get; set;
            }

            [DataMember]
            public abstract string identifier {
                get; protected set;
            }

            [DataMember]
            public abstract IReadOnlyList<string> identifier_list {
                get; protected set;
            }

            [DataMember]
            public abstract ParameterTypeList innermost_parameter_type_list {
                get; protected set;
            }

            public abstract bool isfunction(Stack<string> stack = null);

            public virtual bool isabstract {

                get {
                    return false;
                }
            }

            public bool isvariable {

                get {

                    return !isfunction();
                }
            }

            protected Declarator(Declarator @base) {
                this.@base = @base;
            }

            [DataContract]
            public class GroupedDeclarator : Declarator {

                public override string identifier {

                    get {
                        return @base.identifier;
                    }
                    protected set {
                    }
                }

                public override IReadOnlyList<string> identifier_list {

                    get {
                        return @base.identifier_list;
                    }
                    protected set {
                    }
                }

                public override bool isfunction(Stack<string> stack = null) {

                    stack = stack ?? new Stack<string>();
                    if (pointer != null && pointer.Any()) {
                        stack.Push("pointer");
                    }
                    @base.isfunction(stack);
                    return stack.FirstOrDefault() == "function";
                }


                public GroupedDeclarator(Declarator @base) : base(@base) {
                }

                public override ParameterTypeList innermost_parameter_type_list {

                    get {
                        return @base.innermost_parameter_type_list;
                    }
                    protected set {
                    }
                }
            }

            [DataContract]
            public class IdentifierDeclarator : Declarator {

                [DataMember]
                public override string identifier {
                    get; protected set;
                }

                public override IReadOnlyList<string> identifier_list {

                    get {
                        return null;
                    }
                    protected set {
                    }
                }

                public override bool isfunction(Stack<string> stack = null) {

                    stack = stack ?? new Stack<string>();
                    if (pointer != null && pointer.Any()) {
                        stack.Push("pointer");
                    }
                    return stack.FirstOrDefault() == "function";
                }


                public IdentifierDeclarator(string x): base(null) { 
                    identifier = x;
                }
                public override ParameterTypeList innermost_parameter_type_list {

                    get {

                        return null;
                    }
                    protected set {
                    }
                }
            }

            [DataContract]
            public class ArrayDeclarator : Declarator {

                public override string identifier {

                    get {
                        return @base.identifier;
                    }
                    protected set {
                    }
                }

                public override IReadOnlyList<string> identifier_list {

                    get {
                        return @base.identifier_list;
                    }
                    protected set {
                    }
                }

                [DataMember]
                public Expression size_expression {
                    get; private set;
                }

                public override bool isfunction(Stack<string> stack = null) {

                    stack = stack ?? new Stack<string>();
                    if (pointer != null && pointer.Any()) {
                        stack.Push("pointer");
                    }
                    stack.Push("array");
                    @base.isfunction(stack);
                    return stack.FirstOrDefault() == "function";
                }

                public ArrayDeclarator(Declarator @base, Expression _4) : base(@base) {
                    size_expression = _4;
                }
                public override ParameterTypeList innermost_parameter_type_list {

                    get {
                        return @base.innermost_parameter_type_list;
                    }
                    protected set {
                    }

                }
            }

            [DataContract]
            public abstract class FunctionDeclarator : Declarator {

                public override string identifier {

                    get {
                        return @base.identifier;
                    }
                    protected set {
                    }
                }


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

                [DataContract]
                public class AbbreviatedFunctionDeclarator : FunctionDeclarator {

                    public override IReadOnlyList<string> identifier_list {

                        get {
                            return @base.identifier_list;
                        }
                        protected set {
                        }

                    }

                    public AbbreviatedFunctionDeclarator(Declarator @base) : base(@base) {

                    }
                    public override ParameterTypeList innermost_parameter_type_list {

                        get {
                            return @base.innermost_parameter_type_list;
                        }
                        protected set {
                        }

                    }

                }

                [DataContract]
                public class KandRFunctionDeclarator : FunctionDeclarator {

                    [DataMember]
                    public override IReadOnlyList<string> identifier_list {
                        get; protected set;
                    }

                    public KandRFunctionDeclarator(Declarator @base, IReadOnlyList<string> _4) : base(@base) {

                        identifier_list = _4;
                    }
                    public override ParameterTypeList innermost_parameter_type_list {

                        get {
                            return @base.innermost_parameter_type_list;
                        }
                        protected set {
                        }

                    }

                }

                [DataContract]
                public class AnsiFunctionDeclarator : FunctionDeclarator {

                    [DataMember]
                    public ParameterTypeList parameter_type_list {
                        get; private set;
                    }

                    public override IReadOnlyList<string> identifier_list {

                        get {
                            return @base.identifier_list;
                        }
                        protected set {
                        }

                    }

                    public AnsiFunctionDeclarator(Declarator @base, ParameterTypeList parameterTypeList) : base(@base) {

                        parameter_type_list = parameterTypeList;
                    }
                    public override ParameterTypeList innermost_parameter_type_list {

                        get {
                            return @base.innermost_parameter_type_list ?? parameter_type_list;
                        }
                        protected set {
                        }

                    }
                }

            }

            [DataContract]
            public abstract class AbstractDeclarator : Declarator {
                protected AbstractDeclarator(Declarator @base) : base(@base) {
                }

                public override string identifier {

                    get {
                        return null;
                    }
                    protected set {
                    }

                }

                public override bool isabstract {

                    get {
                        return true;
                    }
                }

                [DataContract]
                public class FunctionAbstractDeclarator : AbstractDeclarator {

                    public override bool isfunction(Stack<string> stack = null) {

                        stack = stack ?? new Stack<string>();
                        stack.Push("function");
                        @base.isfunction(stack);
                        return stack.FirstOrDefault() == "function";
                    }

                    [DataMember]
                    public ParameterTypeList parameter_type_list {
                        get; private set;
                    }

                    public override IReadOnlyList<string> identifier_list {

                        get {
                            return @base.identifier_list;
                        }
                        protected set {
                        }

                    }

                    public FunctionAbstractDeclarator(AbstractDeclarator @base, ParameterTypeList p2): base(@base) {
                        parameter_type_list = p2;
                    }
                    public override ParameterTypeList innermost_parameter_type_list {

                        get {
                            return @base.innermost_parameter_type_list ?? parameter_type_list;
                        }
                        protected set {
                        }

                    }
                }

                [DataContract]
                public class ArrayAbstractDeclarator : AbstractDeclarator {

                    public override bool isfunction(Stack<string> stack = null) {

                        stack = stack ?? new Stack<string>();
                        stack.Push("array");
                        @base.isfunction(stack);
                        return stack.FirstOrDefault() == "function";
                    }

                    [DataMember]
                    public Expression size_expression {
                        get; private set;
                    }

                    public override IReadOnlyList<string> identifier_list {
                        get {
                            return @base?.identifier_list;
                        }
                        protected set {
                        }

                    }

                    public ArrayAbstractDeclarator(AbstractDeclarator @base, Expression p2) : base(@base) {

                        this.size_expression = p2;
                    }

                    public override ParameterTypeList innermost_parameter_type_list {
                        get {
                            return @base?.innermost_parameter_type_list;
                        }
                        protected set {
                        }

                    }
                }

                [DataContract]
                public class GroupedAbstractDeclarator : AbstractDeclarator {

                    public override bool isfunction(Stack<string> stack = null) {

                        return @base.isfunction(null);
                    }

                    public override IReadOnlyList<string> identifier_list {

                        get {
                            return @base.identifier_list;
                        }
                        protected set {
                        }

                    }

                    public GroupedAbstractDeclarator(AbstractDeclarator @base) : base(@base) {
                    }

                    public override ParameterTypeList innermost_parameter_type_list {

                        get {
                            return @base.innermost_parameter_type_list;
                        }
                        protected set {
                        }

                    }
                }

                [DataContract]
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

                    public PointerAbstractDeclarator(AbstractDeclarator @base, IReadOnlyList<string> _1) : base(@base) {
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

        [DataContract]
        public class ParameterTypeList : SyntaxNode {

            [DataMember]
            public bool have_va_list {
                get; private set;
            }
            [DataMember]
            public IReadOnlyList<ParameterDeclaration> parameters {
                get; private set;
            }

            public ParameterTypeList(IReadOnlyList<ParameterDeclaration> parameters, bool haveVaList) {

                this.parameters = parameters;
                have_va_list = haveVaList;
            }
        }

        [DataContract]
        public class ParameterDeclaration : SyntaxNode {

            [DataMember]
            public DeclarationSpecifiers declaration_specifiers {
                get; private set;
            }
            [DataMember]
            public Declarator declarator {
                get; private set;
            }

            public ParameterDeclaration(DeclarationSpecifiers declarationSpecifiers, Declarator declarator) {

                declaration_specifiers = declarationSpecifiers;
                this.declarator = declarator;
            }
        }

        [DataContract]
        public abstract class Statement : SyntaxNode {

            [DataContract]
            public class ErrorStatement : Statement {

            }

            [DataContract]
            public abstract class LabeledStatement : Statement {

                [DataContract]
                public class DefaultLabeledStatement : LabeledStatement {

                    [DataMember]
                    public Statement statement {
                        get; private set;
                    }

                    public DefaultLabeledStatement(Statement statement) {

                        this.statement = statement;
                    }
                }

                [DataContract]
                public class CaseLabeledStatement : LabeledStatement {

                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }
                    [DataMember]
                    public Statement statement {
                        get; private set;
                    }

                    public CaseLabeledStatement(Expression expression, Statement statement) {

                        this.expression = expression;
                        this.statement = statement;
                    }
                }

                [DataContract]
                public class GenericLabeledStatement : LabeledStatement {

                    [DataMember]
                    public string label {
                        get; private set;
                    }
                    [DataMember]
                    public Statement statement {
                        get; private set;
                    }

                    public GenericLabeledStatement(string label, Statement statement) {

                        this.label = label;
                        this.statement = statement;
                    }
                }

            }

            [DataContract]
            public class CompoundStatement : Statement {

                [DataMember]
                public IReadOnlyList<SyntaxNode> block_items {
                    get; private set;
                }

                public CompoundStatement(IReadOnlyList<SyntaxNode> blockItems) {

                    this.block_items = blockItems;
                }
            }

            [DataContract]
            public class ExpressionStatement : Statement {

                [DataMember]
                public Expression expression {
                    get; private set;
                }

                public ExpressionStatement(Expression expression) {

                    this.expression = expression;
                }
            }

            [DataContract]
            public abstract class SelectionStatement : Statement {

                [DataContract]
                public class IfStatement : SelectionStatement {

                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }
                    [DataMember]
                    public Statement then_statement {
                        get; private set;
                    }
                    [DataMember]
                    public Statement else_statement {
                        get; private set;
                    }

                    public IfStatement(Expression expression, Statement thenStatement, Statement elseStatement) {

                        this.expression = expression;
                        this.then_statement = thenStatement;
                        this.else_statement = elseStatement;
                    }
                }

                [DataContract]
                public class SwitchStatement : SelectionStatement {

                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }
                    [DataMember]
                    public Statement statement {
                        get; private set;
                    }

                    public SwitchStatement(Expression expression, Statement statement) {

                        this.expression = expression;
                        this.statement = statement;
                    }
                }

            }

            [DataContract]
            public abstract class IterationStatement : Statement {

                [DataContract]
                public class C99ForStatement : IterationStatement {

                    [DataMember]
                    public Declaration declaration {
                        get; private set;
                    }
                    [DataMember]
                    public Statement condition_statement {
                        get; private set;
                    }
                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }
                    [DataMember]
                    public Statement body_statement {
                        get; private set;
                    }

                    public C99ForStatement(Declaration declaration, Statement condition_statement, Expression expression, Statement body_statement) {

                        this.declaration = declaration;
                        this.condition_statement = condition_statement;
                        this.expression = expression;
                        this.body_statement = body_statement;
                    }
                }

                [DataContract]
                public class ForStatement : IterationStatement {

                    [DataMember]
                    public Statement initial_statement {
                        get; private set;
                    }
                    [DataMember]
                    public Statement condition_statement {
                        get; private set;
                    }
                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }
                    [DataMember]
                    public Statement body_statement {
                        get; private set;
                    }

                    public ForStatement(Statement initial_statement, Statement condition_statement, Expression expression, Statement body_statement) {

                        this.initial_statement = initial_statement;
                        this.condition_statement = condition_statement;
                        this.expression = expression;
                        this.body_statement = body_statement;
                    }
                }

                [DataContract]
                public class DoStatement : IterationStatement {

                    [DataMember]
                    public Statement statement {
                        get; private set;
                    }
                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }

                    public DoStatement(Statement statement, Expression expression) {

                        this.statement = statement;
                        this.expression = expression;
                    }
                }

                [DataContract]
                public class WhileStatement : IterationStatement {

                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }
                    [DataMember]
                    public Statement statement {
                        get; private set;
                    }

                    public WhileStatement(Expression expression, Statement statement) {

                        this.expression = expression;
                        this.statement = statement;
                    }
                }


            }

            [DataContract]
            public abstract class JumpStatement : Statement {

                [DataContract]
                public class ReturnStatement : JumpStatement {

                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }

                    public ReturnStatement(Expression expression) {

                        this.expression = expression;
                    }
                }

                [DataContract]
                public class BreakStatement : JumpStatement {

                }

                [DataContract]
                public class ContinueStatement : JumpStatement {

                }

                [DataContract]
                public class GotoStatement : JumpStatement {

                    [DataMember]
                    public string identifier {
                        get; private set;
                    }

                    public GotoStatement(string identifier) {

                        this.identifier = identifier;
                    }
                }

            }
        }

        [DataContract]
        public class TranslationUnit : SyntaxNode {

            [DataMember]
            public IReadOnlyList<SyntaxNode> external_declarations {
                get; private set;
            }

            public TranslationUnit(IReadOnlyList<SyntaxNode> externalDeclarations) {

                external_declarations = externalDeclarations;
            }
        }

        [DataContract]
        public class TypeName : SyntaxNode {

            [DataMember]
            public SpecifierQualifierList specifier_qualifier_list {
                get; private set;
            }
            [DataMember]
            public Declarator.AbstractDeclarator abstract_declarator {
                get; private set;
            }
            [DataMember]
            public TypeDeclaration type_declaration {
                get; private set;
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

        [DataContract]
        public class Initializer : SyntaxNode {

            [DataMember]
            public Expression expression {
                get; private set;
            }
            [DataMember]
            public IReadOnlyList<Tuple<IReadOnlyList<Designator>, Initializer>> initializers {
                get; private set;
            }

            public Initializer(Expression expression, IReadOnlyList<Tuple<IReadOnlyList<Designator>, Initializer>> initializers) {

                this.expression = expression;
                this.initializers = initializers;
            }

            [DataContract]
            public abstract class Designator {

                [DataContract]
                public class MemberDesignator : Designator {

                    [DataMember]
                    public string identifier {
                        get; private set;
                    }

                    public MemberDesignator(string identifier) {

                        this.identifier = identifier;
                    }
                }
                [DataContract]
                public class IndexDesignator : Designator {

                    [DataMember]
                    public Expression expression {
                        get; private set;
                    }

                    public IndexDesignator(Expression expression) {

                        this.expression = expression;
                    }
                }
            }
        }

        //
        //
        //

        public void Save(string fileName) {
            //XmlSerializerオブジェクトを作成

            List<Type> known_types = new List<Type>();
            List<Type> detected_types = new List<Type>() { typeof(SyntaxNode) };

            while (detected_types.Any()) {
                known_types.AddRange(detected_types);
                detected_types = detected_types.SelectMany(x => x.GetNestedTypes()).Where(x => x.GetCustomAttributes().Any(y => y is DataContractAttribute)).ToList();
            }

            //オブジェクトの型を指定する
            DataContractSerializer serializer = new DataContractSerializer(typeof(SyntaxNode), known_types.ToArray());

            //書き込むファイルを開く（UTF-8 BOM無し）
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new System.Text.UTF8Encoding(false);
            settings.Indent = true;
            using (XmlWriter xw = XmlWriter.Create(fileName, settings)) {
                //シリアル化し、XMLファイルに保存する
                serializer.WriteObject(xw, this);
                //ファイルを閉じる
                xw.Close();
            }
        }

    }

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
                if (x.ReturnType.Equals(typeof(TResult)) == false) {
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
