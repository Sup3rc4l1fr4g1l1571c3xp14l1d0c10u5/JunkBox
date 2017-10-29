using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace CParser2 {
    public abstract class SyntaxNode
    {
        public abstract class Expression : SyntaxNode
        {
            public bool full { get; set; }

            public abstract class UnaryExpression : Expression
            {
                public class PrefixIncrementExpression : UnaryExpression
                {
                    public string @operator { get; }
                    public Expression operand { get; }

                    public PrefixIncrementExpression(string @operator, Expression operand)
                    {
                        this.@operator = @operator;
                        this.operand = operand;
                    }
                }

                public class PrefixDecrementExpression : UnaryExpression
                {
                    public string @operator { get; }
                    public Expression operand { get; }

                    public PrefixDecrementExpression(string @operator, Expression operand)
                    {
                        this.@operator = @operator;
                        this.operand = operand;
                    }
                }

                public class AddressExpression : UnaryExpression
                {
                    private string v;
                    private Expression x;

                    public AddressExpression(string v, Expression x)
                    {
                        this.v = v;
                        this.x = x;
                    }
                }

                public class IndirectionExpression : UnaryExpression
                {
                    private string v;
                    private Expression x;

                    public IndirectionExpression(string v, Expression x)
                    {
                        this.v = v;
                        this.x = x;
                    }
                }

                public class UnaryArithmeticExpression : UnaryExpression
                {
                    private string _1;
                    private Expression _2;

                    public UnaryArithmeticExpression(string _1, Expression _2)
                    {
                        this._1 = _1;
                        this._2 = _2;
                    }
                }

                public class SizeofExpression : UnaryExpression
                {
                    private string v;
                    private Expression x;

                    public SizeofExpression(string v, Expression x)
                    {
                        this.v = v;
                        this.x = x;
                    }
                }

                public class SizeofTypeExpression : UnaryExpression
                {
                    private string _1;
                    private TypeName _3;

                    public SizeofTypeExpression(string _1, TypeName _3)
                    {
                        this._1 = _1;
                        this._3 = _3;
                    }
                }

            }

            public abstract class PrimaryExpression : Expression
            {
                public class ObjectSpecifier : PrimaryExpression
                {
                    private string x;

                    public ObjectSpecifier(string x)
                    {
                        this.x = x;
                    }
                }

                public class ConstantSpecifier : PrimaryExpression
                {
                    private string x;

                    public ConstantSpecifier(string x)
                    {
                        this.x = x;
                    }
                }

                public class StringLiteralSpecifier : PrimaryExpression
                {
                    private string x;

                    public StringLiteralSpecifier(string x)
                    {
                        this.x = x;
                    }
                }

                public class GroupedExpression : PrimaryExpression
                {
                    private Expression x;

                    public GroupedExpression(Expression x)
                    {
                        this.x = x;
                    }
                }

            }

            public abstract class PostfixExpression : Expression
            {
                public class ArraySubscriptExpression : PostfixExpression
                {
                    public Expression expression { get; }
                    public Expression array_subscript { get; }

                    public ArraySubscriptExpression(Expression expression, Expression arraySubscript)
                    {
                        this.expression = expression;
                        array_subscript = arraySubscript;
                    }
                }

                public class FunctionCallExpression : PostfixExpression
                {
                    public Expression[] argument_expressions { get; }
                    public Expression expression { get; }

                    public FunctionCallExpression(Expression expression, Expression[] argumentExpressions)
                    {
                        this.expression = expression;
                        argument_expressions = argumentExpressions;
                    }
                }

                public class MemberAccessByValueExpression : PostfixExpression
                {
                    public Expression expression { get; }
                    public string identifier { get; }

                    public MemberAccessByValueExpression(Expression x, string _11)
                    {
                        expression = x;
                        identifier = _11;
                    }
                }

                public class MemberAccessByPointerExpression : PostfixExpression
                {
                    public Expression expression { get; }
                    public string identifier { get; }

                    public MemberAccessByPointerExpression(Expression x, string _11)
                    {
                        expression = x;
                        identifier = _11;
                    }
                }

                public class PostfixIncrementExpression : PostfixExpression
                {
                    public Expression operand { get; }

                    public PostfixIncrementExpression(Expression operand)
                    {
                        this.operand = operand;
                    }
                }

                public class PostfixDecrementExpression : PostfixExpression
                {
                    public Expression operand { get; }

                    public PostfixDecrementExpression(Expression x)
                    {
                        operand = x;
                    }
                }

                public class CompoundLiteralExpression : PostfixExpression
                {
                    private TypeName _3;
                    private string _7;

                    public CompoundLiteralExpression(TypeName _3, string _7)
                    {
                        this._3 = _3;
                        this._7 = _7;
                    }
                }

            }

            public abstract class BinaryExpression : Expression
            {

                public class CompoundAssignmentExpression : BinaryExpression
                {
                    private Expression _1;
                    private string _2;
                    private Expression _3;

                    public CompoundAssignmentExpression(string _2, Expression _1, Expression _3)
                    {
                        this._2 = _2;
                        this._1 = _1;
                        this._3 = _3;
                    }
                }

                public class SimpleAssignmentExpression : BinaryExpression
                {
                    private Expression _1;
                    private string _2;
                    private Expression _3;

                    public SimpleAssignmentExpression(string _2, Expression _1, Expression _3)
                    {
                        this._2 = _2;
                        this._1 = _1;
                        this._3 = _3;
                    }
                }

                public class ConditionalExpression : BinaryExpression
                {
                    private Expression _1;
                    private string _3;
                    private Expression _4;
                    private Expression _6;

                    public ConditionalExpression(Expression _1, Expression _4, Expression _6, string _3)
                    {
                        this._1 = _1;
                        this._4 = _4;
                        this._6 = _6;
                        this._3 = _3;
                    }
                }

                public class LogicalOrExpression : BinaryExpression
                {
                    private Expression s;
                    private string v;
                    private Expression y;

                    public LogicalOrExpression(string v, Expression s, Expression y)
                    {
                        this.v = v;
                        this.s = s;
                        this.y = y;
                    }
                }

                public class LogicalAndExpression : BinaryExpression
                {
                    private Expression s;
                    private string v;
                    private Expression y;

                    public LogicalAndExpression(string v, Expression s, Expression y)
                    {
                        this.v = v;
                        this.s = s;
                        this.y = y;
                    }
                }

                public class InclusiveOrExpression : BinaryExpression
                {
                    private Expression s;
                    private string v;
                    private Expression y;

                    public InclusiveOrExpression(string v, Expression s, Expression y)
                    {
                        this.v = v;
                        this.s = s;
                        this.y = y;
                    }
                }

                public class ExclusiveOrExpression : BinaryExpression
                {
                    private Expression s;
                    private string v;
                    private Expression y;

                    public ExclusiveOrExpression(string v, Expression s, Expression y)
                    {
                        this.v = v;
                        this.s = s;
                        this.y = y;
                    }
                }

                public class AndExpression : BinaryExpression
                {
                    private Expression s;
                    private string v;
                    private Expression y;

                    public AndExpression(string v, Expression s, Expression y)
                    {
                        this.v = v;
                        this.s = s;
                        this.y = y;
                    }
                }

                public class EqualityExpression : BinaryExpression
                {
                    private string item1;
                    private Expression item2;
                    private Expression s;

                    public EqualityExpression(string item1, Expression s, Expression item2)
                    {
                        this.item1 = item1;
                        this.s = s;
                        this.item2 = item2;
                    }
                }

                public class RelationalExpression : BinaryExpression
                {
                    private string item1;
                    private Expression item2;
                    private Expression s;

                    public RelationalExpression(string item1, Expression s, Expression item2)
                    {
                        this.item1 = item1;
                        this.s = s;
                        this.item2 = item2;
                    }
                }

                public class ShiftExpression : BinaryExpression
                {
                    private string item1;
                    private Expression item2;
                    private Expression s;

                    public ShiftExpression(string item1, Expression s, Expression item2)
                    {
                        this.item1 = item1;
                        this.s = s;
                        this.item2 = item2;
                    }
                }

                public class AdditiveExpression : BinaryExpression
                {
                    private string item1;
                    private Expression item2;
                    private Expression s;

                    public AdditiveExpression(string item1, Expression s, Expression item2)
                    {
                        this.item1 = item1;
                        this.s = s;
                        this.item2 = item2;
                    }
                }

                public class MultiplicativeExpression : BinaryExpression
                {
                    private string item1;
                    private Expression item2;
                    private Expression s;

                    public MultiplicativeExpression(string item1, Expression s, Expression item2)
                    {
                        this.item1 = item1;
                        this.s = s;
                        this.item2 = item2;
                    }
                }

            }

            public class CommaSeparatedExpression : Expression
            {
                private Expression[] x;

                public CommaSeparatedExpression(Expression[] x)
                {
                    this.x = x;
                }
            }

            public class CastExpression : Expression
            {
                private Expression s;
                private TypeName x;

                public CastExpression(TypeName x, Expression s)
                {
                    this.x = x;
                    this.s = s;
                }
            }

        }

        public class Declaration : SyntaxNode
        {
            public DeclarationSpecifiers declaration_specifiers { get; }
            public InitDeclarator[] init_declarators { get; }
            public SyntaxNode[] items { get; }

            public Declaration(DeclarationSpecifiers _1, InitDeclarator[] _2)
            {
                declaration_specifiers = _1;
                init_declarators = _2;
                items = build_items(_1, _2);

            }

            private SyntaxNode[] build_items(DeclarationSpecifiers dcl_specs, InitDeclarator[] init_dcrs)
            {
                var ret = new List<SyntaxNode>();
                ret.AddRange(build_type_declaration(dcl_specs, init_dcrs).Cast<SyntaxNode>());
                ret.AddRange(build_function_declaration(dcl_specs, init_dcrs).Cast<SyntaxNode>());
                ret.AddRange(build_variable_declaration(dcl_specs, init_dcrs).Cast<SyntaxNode>());
                ret.AddRange(build_variable_definition(dcl_specs, init_dcrs).Cast<SyntaxNode>());
                return ret.ToArray();
            }

            private List<Definition.VariableDefinition> build_variable_definition(DeclarationSpecifiers dcl_specs,
                InitDeclarator[] init_dcrs)
            {
                var var_defs = new List<Definition.VariableDefinition>();
                if (dcl_specs == null || dcl_specs.storage_class_specifier == "extern" ||
                    dcl_specs.storage_class_specifier == "typedef")
                {
                    return var_defs;
                }

                foreach (var init_dcr in init_dcrs)
                {
                    if (init_dcr.declarator.isvariable)
                    {
                        var_defs.Add(new Definition.VariableDefinition(dcl_specs, init_dcr));
                    }
                }
                return var_defs;
            }


            private List<VariableDeclaration> build_variable_declaration(DeclarationSpecifiers dcl_specs,
                InitDeclarator[] init_dcrs)
            {
                var var_dcls = new List<VariableDeclaration>();
                if (dcl_specs == null || dcl_specs.storage_class_specifier == "extern")
                {
                    return var_dcls;
                }

                foreach (var init_dcr in init_dcrs)
                {
                    if (init_dcr.declarator.isvariable)
                    {
                        var dcr = init_dcr.declarator;
                        var_dcls.Add(new VariableDeclaration(dcl_specs, dcr));
                    }
                }
                return var_dcls;
            }

            private List<FunctionDeclaration> build_function_declaration(DeclarationSpecifiers dcl_specs,
                InitDeclarator[] init_dcrs)
            {
                var func_dcls = new List<FunctionDeclaration>();
                if (dcl_specs != null && dcl_specs.storage_class_specifier == "typedef")
                {
                    return func_dcls;
                }

                foreach (var init_dcr in init_dcrs)
                {
                    if (init_dcr.declarator.isfunction())
                    {
                        func_dcls.Add(new FunctionDeclaration(dcl_specs, init_dcr));
                    }
                }
                return func_dcls;
            }

            private List<TypeDeclaration> build_type_declaration(DeclarationSpecifiers dcl_specs, InitDeclarator[] init_dcrs)
            {
                var type_dcls = new List<TypeDeclaration>();
                if (dcl_specs == null)
                {
                    return type_dcls;
                }
                dcl_specs.type_specifiers.ForEach(type_spec =>
                {
                    var builder = new TypeDeclarationBuilder();
                    type_spec.accept(builder);
                    type_dcls.AddRange(builder.type_declarations);
                });

                var sc = dcl_specs.storage_class_specifier;

                if (sc == "typedef")
                {
                    foreach (var init_dcr in init_dcrs)
                    {
                        var id = init_dcr.declarator.identifier;
                        type_dcls.Add(new TypeDeclaration.TypedefDeclaration(dcl_specs, init_dcr));
                    }
                }

                return type_dcls;
            }

            private class TypeDeclarationBuilder
            {
                public TypeDeclarationBuilder()
                {
                    type_declarations = new List<TypeDeclaration>();
                }

                public List<TypeDeclaration> type_declarations { get; }

                public void visit_StandardTypeSpecifier(TypeSpecifier.StandardTypeSpecifier node)
                {
                }

                public void visit_TypedefTypeSpecifier(TypeSpecifier.TypedefTypeSpecifier node)
                {
                }

                public void visit_StructSpecifier(TypeSpecifier.StructSpecifier node)
                {
                    if (node.struct_declarations != null)
                    {
                        foreach (var child in node.struct_declarations) { child.accept(this); }
                        type_declarations.Add(new TypeDeclaration.StructTypeDeclaration(node));
                    }
                }

                public void visit_UnionSpecifier(TypeSpecifier.UnionSpecifier node)
                {
                    if (node.struct_declarations != null)
                    {
                        foreach (var child in node.struct_declarations) { child.accept(this); }
                        type_declarations.Add(new TypeDeclaration.UnionTypeDeclaration(node));
                    }
                }

                public void visit_EnumSpecifier(EnumSpecifier node)
                {
                    if (node.enumerators != null)
                    {
                        type_declarations.Add(new TypeDeclaration.EnumTypeDeclaration(node));
                    }
                }

                //private void  visit_typeof_type_specifier(TypeDeclarationBuilder node) {
                //}

                public void visit_StructDeclaration(StructDeclaration node)
                {
                    node.specifier_qualifier_list.accept(this);
                }

                public void visit_SpecifierQualifierList(SpecifierQualifierList node)
                {
                    foreach (var child in node.type_specifiers) { child.accept(this); };
                }
            }

        }

        public class FunctionDeclaration : SyntaxNode
        {
            public DeclarationSpecifiers declaration_specifiers { get; }
            public InitDeclarator init_declarator { get; }
            public string identifier
            {
                get { return init_declarator.declarator.identifier; }
            }

            public FunctionDeclaration(DeclarationSpecifiers dcl_specs, InitDeclarator init_dcr)
            {
                declaration_specifiers = dcl_specs;
                init_declarator = init_dcr;
            }
        }

        public class VariableDeclaration : SyntaxNode
        {
            private DeclarationSpecifiers declaration_specifiers;
            private Declarator declarator;

            public string identifier
            {
                get { return declarator.identifier; }
            }

            public VariableDeclaration(DeclarationSpecifiers dcl_specs, Declarator dcr)
            {
                declaration_specifiers = dcl_specs;
                declarator = dcr;
            }
        }

        public abstract class Definition : SyntaxNode
        {
            protected Definition(DeclarationSpecifiers dcl_specs)
            {
                declaration_specifiers = dcl_specs;
            }

            public DeclarationSpecifiers declaration_specifiers { get; }

            public abstract class FunctionDefinition : Definition
            {
                public Declarator declarator { get; }
                public ParameterDefinition[] parameterDefinition { get; }
                public Statement function_body { get; }

                protected FunctionDefinition(DeclarationSpecifiers dcl_specs, Declarator dcr, ParameterDefinition[] param_defs, Statement compound_stmt) : base(dcl_specs)
                {
                    declarator = dcr;
                    parameterDefinition = param_defs;
                    function_body = compound_stmt;
                }

                public class KandRFunctionDefinition : FunctionDefinition
                {
                    public KandRFunctionDefinition(DeclarationSpecifiers dcl_specs, Declarator dcr, List<Declaration> dcls,
                        Statement compound_stmt) : base(dcl_specs, dcr, create_parameters(dcr.identifier_list, dcls), compound_stmt)
                    {
                    }

                    public string[] identifier_list
                    {
                        get { return declarator.identifier_list; }
                    }

                    private static ParameterDefinition[] create_parameters(string[] param_names, List<Declaration> dcls)
                    {
                        var param_defs = new List<ParameterDefinition>();
                        if (param_names == null)
                        {
                            return param_defs.ToArray();
                        }

                        foreach (var name in param_names)
                        {
                            var var_def = find_variable_definition(dcls, name);
                            param_defs.Add(variable_definition_to_parameter_definition(var_def));
                        }
                        return param_defs.ToArray();
                    }

                    private static VariableDefinition find_variable_definition(List<Declaration> dcls, string name)
                    {
                        foreach (var dcl in dcls)
                        {
                            foreach (var var_def in dcl.items.Where(item => item is VariableDefinition).Cast<VariableDefinition>())
                            {
                                if (var_def.identifier == name)
                                {
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

                    private static ParameterDefinition variable_definition_to_parameter_definition(VariableDefinition var_def)
                    {
                        var dcl_specs = var_def.declaration_specifiers;
                        var dcr = var_def.init_declarator.declarator;
                        var param_def = new ParameterDefinition(dcl_specs, dcr);
                        return param_def;
                    }

                    private static Declaration implicit_parameter_definition(string id)
                    {
                        var init_dcr = new InitDeclarator(new Declarator.IdentifierDeclarator(id), null);
                        return new Declaration(null, new[] { init_dcr });
                    }

                }

                public class AnsiFunctionDefinition : FunctionDefinition
                {

                    public AnsiFunctionDefinition(DeclarationSpecifiers dcl_specs, Declarator dcr, Statement compound_stmt)
                        : base(dcl_specs, dcr, create_parameters(dcr.innermost_parameter_type_list), compound_stmt)
                    {
                    }

                    private static ParameterDefinition[] create_parameters(ParameterTypeList param_type_list)
                    {
                        var ret = new List<ParameterDefinition>();
                        if (param_type_list == null)
                        {
                            return ret.ToArray();
                        }

                        return param_type_list.parameters.Select(param_dcl =>
                        {
                            var dcl_specs = param_dcl.declaration_specifiers;
                            var dcr = param_dcl.declarator;
                            var param_def = new ParameterDefinition(dcl_specs, dcr);
                            return param_def;
                        }).ToArray();

                    }
                }
            }

            public class VariableDefinition : Definition
            {
                public InitDeclarator init_declarator { get; }

                public VariableDefinition(DeclarationSpecifiers dcl_specs, InitDeclarator init_dcr) : base(dcl_specs)
                {
                    init_declarator = init_dcr;
                }

                public string identifier
                {
                    get { return init_declarator.declarator.identifier; }
                }

            }

            public class ParameterDefinition : Definition
            {
                public Declarator declarator { get; }

                public ParameterDefinition(DeclarationSpecifiers dcl_specs, Declarator dcr) : base(dcl_specs)
                {
                    declarator = dcr;
                }

                public string identifier
                {
                    get
                    {
                        if (declarator != null)
                        {
                            if (declarator.isabstract)
                            {
                                return null;
                            }
                            return declarator.identifier;
                        }
                        return null;
                    }
                }

            }

        }

        public abstract class TypeDeclaration : SyntaxNode
        {
            public abstract string identifier { get; }

            public class TypedefDeclaration : TypeDeclaration
            {
                public DeclarationSpecifiers declaration_specifiers { get; }
                public InitDeclarator init_declarator { get; }
                public override string identifier
                {
                    get { return init_declarator.declarator.identifier; }
                }

                public TypedefDeclaration(DeclarationSpecifiers dcl_specs, InitDeclarator init_dcr)
                {
                    declaration_specifiers = dcl_specs;
                    init_declarator = init_dcr;
                }
            }

            public class StructTypeDeclaration : TypeDeclaration
            {
                public override string identifier { get { return struct_specifier.identifier; } }
                public TypeSpecifier.StructSpecifier struct_specifier { get; }

                public StructTypeDeclaration(TypeSpecifier.StructSpecifier node)
                {
                    struct_specifier = node;
                }
            }

            public class UnionTypeDeclaration : TypeDeclaration
            {
                public override string identifier { get { return union_specifier.identifier; } }
                public TypeSpecifier.UnionSpecifier union_specifier { get; }

                public UnionTypeDeclaration(TypeSpecifier.UnionSpecifier node)
                {
                    union_specifier = node;
                }
            }

            public class EnumTypeDeclaration : TypeDeclaration
            {
                public override string identifier { get { return enum_specifier.identifier; } }
                public EnumSpecifier enum_specifier { get; }

                public EnumTypeDeclaration(EnumSpecifier node)
                {
                    enum_specifier = node;
                }
            }
        }

        public class DeclarationSpecifiers
        {
            public string storage_class_specifier { get; internal set; }
            public List<TypeSpecifier> type_specifiers { get; } = new List<TypeSpecifier>();
            public List<string> type_qualifiers { get; } = new List<string>();
            public string function_specifier { get; internal set; }

        }

        public class InitDeclarator
        {
            public Declarator declarator { get; }
            public Initializer initializer { get; }

            public InitDeclarator(Declarator _1, Initializer _2)
            {
                declarator = _1;
                initializer = _2;
            }
        }

        public abstract class TypeSpecifier : Visitable
        {
            public class StructSpecifier : TypeSpecifier
            {
                public string identifier { get; }
                public bool v2 { get; }
                public StructDeclaration[] struct_declarations { get; }

                public StructSpecifier(string v1, StructDeclaration[] _3, bool v2)
                {
                    identifier = v1;
                    struct_declarations = _3;
                    this.v2 = v2;
                }
            }

            public class UnionSpecifier : TypeSpecifier
            {
                public string identifier { get; }
                public bool v2 { get; }
                public StructDeclaration[] struct_declarations { get; }

                public UnionSpecifier(string v1, StructDeclaration[] _3, bool v2)
                {
                    identifier = v1;
                    struct_declarations = _3;
                    this.v2 = v2;
                }
            }

            public class StandardTypeSpecifier : TypeSpecifier
            {
                public string identifier { get; }

                public StandardTypeSpecifier(string s)
                {
                    identifier = s;
                }
            }

            public class TypedefTypeSpecifier : TypeSpecifier
            {
                public string identifier { get; }

                public TypedefTypeSpecifier(string s)
                {
                    identifier = s;
                }
            }
        }

        public class StructDeclaration : Visitable
        {
            public SpecifierQualifierList specifier_qualifier_list { get; }
            public StructDeclarator[] struct_declarators { get; }
            public MemberDeclaration[] items { get; }

            public StructDeclaration(SpecifierQualifierList _1, StructDeclarator[] _2)
            {
                specifier_qualifier_list = _1;
                struct_declarators = _2;
                items = build_items(_1, _2);
            }

            private MemberDeclaration[] build_items(SpecifierQualifierList spec_qual_list, StructDeclarator[] struct_dcrs)
            {
                // FIXME: Must support unnamed bit padding.

                if (!struct_dcrs.Any())
                {
                    return new[] { new MemberDeclaration(spec_qual_list, null) };
                }
                return struct_dcrs.Select(struct_dcr => new MemberDeclaration(spec_qual_list, struct_dcr)).ToArray();
            }
        }

        public class MemberDeclaration
        {
            public SpecifierQualifierList specifier_qualifier_list { get; }

            public StructDeclarator struct_declarator { get; }
            //public StructDeclarator type { get; }

            public MemberDeclaration(SpecifierQualifierList spec_qual_list, StructDeclarator struct_dcr)
            {
                specifier_qualifier_list = spec_qual_list;
                struct_declarator = struct_dcr;
            }

            public string identifier()
            {
                if (struct_declarator != null && struct_declarator.declarator != null)
                {
                    return struct_declarator.declarator.identifier;
                }
                return null;
            }

        }

        public class SpecifierQualifierList : Visitable
        {
            public List<TypeSpecifier> type_specifiers { get; } = new List<TypeSpecifier>();
            public List<string> type_qualifiers { get; } = new List<string>();
        }

        public class StructDeclarator : SyntaxNode
        {
            public Declarator declarator { get; }
            public Expression expression { get; }

            public StructDeclarator(Declarator _1, Expression _2)
            {
                declarator = _1;
                expression = _2;
            }
        }

        public class EnumSpecifier : TypeSpecifier
        {
            public string identifier { get; }
            private bool v2;
            public Enumerator[] enumerators { get; }

            public EnumSpecifier(string v1, Enumerator[] _6, bool v2)
            {
                identifier = v1;
                enumerators = _6;
                this.v2 = v2;
            }
        }

        public class Enumerator
        {
            private string _1;
            private object _2;

            public Enumerator(string _1, object _2)
            {
                this._1 = _1;
                this._2 = _2;
            }
        }

        public abstract class Declarator
        {
            public abstract Declarator @base { get; }
            public bool full { get; set; }
            public string[] pointer { get; set; }
            public abstract string identifier { get; }

            public abstract string[] identifier_list { get; }

            public abstract ParameterTypeList innermost_parameter_type_list { get; }

            public abstract bool isfunction(Stack<string> stack = null);

            public virtual bool isabstract
            {
                get { return false; }
            }

            public bool isvariable
            {
                get
                {
                    return !isfunction();
                }
            }

            public class GroupedDeclarator : Declarator
            {
                public override Declarator @base { get; }

                public override string identifier
                {
                    get { return @base.identifier; }
                }

                public override string[] identifier_list
                {
                    get { return @base.identifier_list; }
                }

                public override bool isfunction(Stack<string> stack = null)
                {
                    stack = stack ?? new Stack<string>();
                    if (pointer != null && pointer.Any()) { stack.Push("pointer"); }
                    @base.isfunction(stack);
                    return stack.FirstOrDefault() == "function";
                }


                public GroupedDeclarator(Declarator x)
                {
                    @base = x;
                }

                public override ParameterTypeList innermost_parameter_type_list
                {
                    get { return @base.innermost_parameter_type_list; }
                }
            }

            public class IdentifierDeclarator : Declarator
            {
                public override Declarator @base { get { return null; } }
                public override string identifier { get; }

                public override string[] identifier_list
                {
                    get { return null; }
                }

                public override bool isfunction(Stack<string> stack = null)
                {
                    stack = stack ?? new Stack<string>();
                    if (pointer != null && pointer.Any()) { stack.Push("pointer"); }
                    return stack.FirstOrDefault() == "function";
                }


                public IdentifierDeclarator(string x)
                {
                    identifier = x;
                }
                public override ParameterTypeList innermost_parameter_type_list
                {
                    get
                    {
                        return null;
                    }
                }
            }

            public class ArrayDeclarator : Declarator
            {
                public override string identifier
                {
                    get { return @base.identifier; }
                }

                public override string[] identifier_list
                {
                    get { return @base.identifier_list; }
                }

                public override Declarator @base { get; }
                public Expression size_expression { get; }

                public override bool isfunction(Stack<string> stack = null)
                {
                    stack = stack ?? new Stack<string>();
                    if (pointer != null && pointer.Any()) { stack.Push("pointer"); }
                    stack.Push("array");
                    @base.isfunction(stack);
                    return stack.FirstOrDefault() == "function";
                }

                public ArrayDeclarator(Declarator x, Expression _4)
                {
                    @base = x;
                    size_expression = _4;
                }
                public override ParameterTypeList innermost_parameter_type_list
                {
                    get { return @base.innermost_parameter_type_list; }
                }
            }

            public abstract class FunctionDeclarator : Declarator
            {
                public override string identifier
                {
                    get { return @base.identifier; }
                }

                public override Declarator @base { get; }

                public override bool isfunction(Stack<string> stack = null)
                {
                    stack = stack ?? new Stack<string>();
                    if (pointer != null && pointer.Any()) { stack.Push("pointer"); }
                    stack.Push("function");
                    @base.isfunction(stack);
                    return stack.FirstOrDefault() == "function";
                }


                protected FunctionDeclarator(Declarator dbase)
                {
                    @base = dbase;
                }

                public class AbbreviatedFunctionDeclarator : FunctionDeclarator
                {
                    public override string[] identifier_list
                    {
                        get { return @base.identifier_list; }
                    }

                    public AbbreviatedFunctionDeclarator(Declarator dbase) : base(dbase)
                    {
                    }
                    public override ParameterTypeList innermost_parameter_type_list
                    {
                        get { return @base.innermost_parameter_type_list; }
                    }

                }

                public class KandRFunctionDeclarator : FunctionDeclarator
                {
                    public override string[] identifier_list { get; }

                    public KandRFunctionDeclarator(Declarator x, string[] _4) : base(x)
                    {
                        identifier_list = _4;
                    }
                    public override ParameterTypeList innermost_parameter_type_list
                    {
                        get { return @base.innermost_parameter_type_list; }
                    }

                }

                public class AnsiFunctionDeclarator : FunctionDeclarator
                {
                    private ParameterTypeList parameter_type_list;

                    public override string[] identifier_list
                    {
                        get { return @base.identifier_list; }
                    }

                    public AnsiFunctionDeclarator(Declarator dbase, ParameterTypeList parameterTypeList) : base(dbase)
                    {
                        parameter_type_list = parameterTypeList;
                    }
                    public override ParameterTypeList innermost_parameter_type_list
                    {
                        get { return @base.innermost_parameter_type_list ?? parameter_type_list; }
                    }
                }

            }

            public abstract class AbstractDeclarator : Declarator
            {
                public override string identifier
                {
                    get { return null; }
                }

                public override bool isabstract
                {
                    get { return true; }
                }

                public class FunctionAbstractDeclarator : AbstractDeclarator
                {
                    public override Declarator @base { get; }
                    public override bool isfunction(Stack<string> stack = null)
                    {
                        stack = stack ?? new Stack<string>();
                        stack.Push("function");
                        @base.isfunction(stack);
                        return stack.FirstOrDefault() == "function";
                    }

                    public ParameterTypeList parameter_type_list { get; }

                    public override string[] identifier_list
                    {
                        get { return @base.identifier_list; }
                    }

                    public FunctionAbstractDeclarator(AbstractDeclarator p1, ParameterTypeList p2)
                    {
                        @base = p1;
                        parameter_type_list = p2;
                    }
                    public override ParameterTypeList innermost_parameter_type_list
                    {
                        get { return @base.innermost_parameter_type_list ?? parameter_type_list; }
                    }
                }

                public class ArrayAbstractDeclarator : AbstractDeclarator
                {
                    public override Declarator @base { get; }
                    public override bool isfunction(Stack<string> stack = null)
                    {
                        stack = stack ?? new Stack<string>();
                        stack.Push("array");
                        @base.isfunction(stack);
                        return stack.FirstOrDefault() == "function";
                    }

                    public object p2 { get; }

                    public override string[] identifier_list
                    {
                        get { return @base.identifier_list; }
                    }

                    public ArrayAbstractDeclarator(AbstractDeclarator p1, object p2)
                    {
                        @base = p1;
                        this.p2 = p2;
                    }
                    public override ParameterTypeList innermost_parameter_type_list
                    {
                        get { return @base.innermost_parameter_type_list; }
                    }
                }

                public class GroupedAbstractDeclarator : AbstractDeclarator
                {
                    public override Declarator @base { get; }

                    public override bool isfunction(Stack<string> stack = null)
                    {
                        return @base.isfunction(null);
                    }

                    public override string[] identifier_list
                    {
                        get { return @base.identifier_list; }
                    }

                    public GroupedAbstractDeclarator(AbstractDeclarator _3)
                    {
                        @base = _3;
                    }
                    public override ParameterTypeList innermost_parameter_type_list
                    {
                        get { return @base.innermost_parameter_type_list; }
                    }
                }

                public class PointerAbstractDeclarator : AbstractDeclarator
                {
                    public override Declarator @base { get; }
                    public override bool isfunction(Stack<string> stack = null)
                    {
                        stack = stack ?? new Stack<string>();
                        stack.Push("pointer");
                        @base.isfunction(stack);
                        return stack.FirstOrDefault() == "function";
                    }

                    public override string[] identifier_list
                    {
                        get { return @base.identifier_list; }
                    }

                    public PointerAbstractDeclarator(AbstractDeclarator _2, string[] _1)
                    {
                        @base = _2;
                        pointer = _1;
                    }
                    public override ParameterTypeList innermost_parameter_type_list
                    {
                        get { return @base.innermost_parameter_type_list; }
                    }
                }
            }
        }

        public class ParameterTypeList : SyntaxNode
        {
            public bool have_va_list { get; }
            public ParameterDeclaration[] parameters { get; }

            public ParameterTypeList(ParameterDeclaration[] parameters, bool haveVaList)
            {
                this.parameters = parameters;
                have_va_list = haveVaList;
            }
        }

        public class ParameterDeclaration : SyntaxNode
        {
            public DeclarationSpecifiers declaration_specifiers { get; }
            public Declarator declarator { get; }

            public ParameterDeclaration(DeclarationSpecifiers declarationSpecifiers, Declarator declarator)
            {
                declaration_specifiers = declarationSpecifiers;
                this.declarator = declarator;
            }
        }

        public abstract class Statement : SyntaxNode
        {
            public class ErrorStatement : Statement
            {
            }

            public abstract class LabeledStatement : Statement
            {
                public class DefaultLabeledStatement : LabeledStatement
                {
                    private Expression _2;
                    private Statement _4;

                    public DefaultLabeledStatement(Expression _2, Statement _4)
                    {
                        this._2 = _2;
                        this._4 = _4;
                    }
                }

                public class CaseLabeledStatement : LabeledStatement
                {
                    private Expression _2;
                    private Statement _4;

                    public CaseLabeledStatement(Expression _2, Statement _4)
                    {
                        this._2 = _2;
                        this._4 = _4;
                    }
                }

                public class GenericLabeledStatement : LabeledStatement
                {
                    private string _1;
                    private Statement _3;

                    public GenericLabeledStatement(string _1, Statement _3)
                    {
                        this._1 = _1;
                        this._3 = _3;
                    }
                }

            }

            public class CompoundStatement : Statement
            {
                private SyntaxNode[] _2;

                public CompoundStatement(SyntaxNode[] _2)
                {
                    this._2 = _2;
                }
            }

            public class ExpressionStatement : Statement
            {
                private Expression _1;

                public ExpressionStatement(Expression _1)
                {
                    this._1 = _1;
                }
            }

            public abstract class SelectionStatement : Statement
            {
                public class IfStatement : SelectionStatement
                {
                    private Expression _3;
                    private Statement _5;
                    private Statement _6;

                    public IfStatement(Expression _3, Statement _5, Statement _6)
                    {
                        this._3 = _3;
                        this._5 = _5;
                        this._6 = _6;
                    }
                }

                public class SwitchStatement : SelectionStatement
                {
                    private Expression _3;
                    private Statement _5;

                    public SwitchStatement(Expression _3, Statement _5)
                    {
                        this._3 = _3;
                        this._5 = _5;
                    }
                }

            }

            public abstract class IterationStatement : Statement
            {
                public class C99ForStatement : IterationStatement
                {
                    private Declaration _3;
                    private Statement _4;
                    private Expression _5;

                    public C99ForStatement(Declaration _3, Statement _4, Expression _5)
                    {
                        this._3 = _3;
                        this._4 = _4;
                        this._5 = _5;
                    }
                }

                public class ForStatement : IterationStatement
                {
                    private Statement _3;
                    private Statement _4;
                    private Expression _5;

                    public ForStatement(Statement _3, Statement _4, Expression _5)
                    {
                        this._3 = _3;
                        this._4 = _4;
                        this._5 = _5;
                    }
                }

                public class DoStatement : IterationStatement
                {
                    private Statement _2;
                    private Expression _5;

                    public DoStatement(Statement _2, Expression _5)
                    {
                        this._2 = _2;
                        this._5 = _5;
                    }
                }

                public class WhileStatement : IterationStatement
                {
                    private Expression _3;
                    private Statement _5;

                    public WhileStatement(Expression _3, Statement _5)
                    {
                        this._3 = _3;
                        this._5 = _5;
                    }
                }


            }

            public abstract class JumpStatement : Statement
            {
                public class ReturnStatement : JumpStatement
                {
                    private Expression _2;

                    public ReturnStatement(Expression _2)
                    {
                        this._2 = _2;
                    }
                }

                public class BreakStatement : JumpStatement
                {
                }

                public class ContinueStatement : JumpStatement
                {
                }

                public class GotoStatement : JumpStatement
                {
                    private string _2;

                    public GotoStatement(string _2)
                    {
                        this._2 = _2;
                    }
                }

            }
        }

        public class TranslationUnit : SyntaxNode
        {
            public SyntaxNode[] external_declarations { get; }

            public TranslationUnit(SyntaxNode[] externalDeclarations)
            {
                external_declarations = externalDeclarations;
            }
        }

        public class TypeName : SyntaxNode
        {
            private SpecifierQualifierList _1;
            private Declarator.AbstractDeclarator _2;

            public TypeName(SpecifierQualifierList _1, Declarator.AbstractDeclarator _2)
            {
                this._1 = _1;
                this._2 = _2;
            }
        }

        public class Initializer : SyntaxNode
        {
            public Expression expression { get; }
            public Tuple<Designator[], Initializer>[] initializers { get; }

            public Initializer(Expression expression, Tuple<Designator[], Initializer>[] initializers)
            {
                this.expression = expression;
                this.initializers = initializers;
            }

            public abstract class Designator
            {
                public class MemberDesignator : Designator
                {
                    public string identifier { get; }

                    public MemberDesignator(string identifier)
                    {
                        this.identifier = identifier;
                    }
                }
                public class IndexDesignator : Designator
                {
                    public Expression expression { get; }

                    public IndexDesignator(Expression expression)
                    {
                        this.expression = expression;
                    }
                }
            }
        }

        //
        //
        //

        public abstract class Visitable
        {
            public void accept(object visitor)
            {
                var name = GetType().Name;
                visitor.GetType().InvokeMember(
                    $"visit_{name}",
                    BindingFlags.InvokeMethod,
                    null,
                    visitor,
                    new object[] { this }
                );
            }
        }
    }
}