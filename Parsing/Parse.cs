using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Parsing;

namespace CParser2
{
    public class Grammer
    {
        //rule

        public static Parser<char> Space = Combinator.AnyChar(" \r\n\t\v\f");

        public static Parser<string> Ident =
            from _1 in Combinator.AnyChar((x) => (('a' <= x && x <= 'z') || ('A' <= x && x <= 'Z') || (x == '_'))).String()
            from _2 in Combinator.AnyChar((x) => (('a' <= x && x <= 'z') || ('A' <= x && x <= 'Z') || (x == '_') || ('0' <= x && x <= '9'))).Many().String()
            select _1 + _2;

        public static Parser<string> Keyword(string s)
        {
            return Ident.Where(x => x == s).Skip(Space.Option());
        }
        public static Parser<string> Symbol(string s)
        {
            return Combinator.Token(s).Skip(Space.Option());
        }

        public static Parser<string> SIZEOF = Keyword("sizeof");
        public static Parser<string> TYPEDEF = Keyword("typedef");
        public static Parser<string> EXTERN = Keyword("extern");
        public static Parser<string> STATIC = Keyword("static");
        public static Parser<string> AUTO = Keyword("auto");
        public static Parser<string> REGISTER = Keyword("register");
        public static Parser<string> INLINE = Keyword("inline");
        public static Parser<string> RESTRICT = Keyword("restrict");
        public static Parser<string> CHAR = Keyword("char");
        public static Parser<string> SHORT = Keyword("short");
        public static Parser<string> INT = Keyword("int");
        public static Parser<string> LONG = Keyword("long");
        public static Parser<string> SIGNED = Keyword("signed");
        public static Parser<string> UNSIGNED = Keyword("unsigned");
        public static Parser<string> FLOAT = Keyword("float");
        public static Parser<string> DOUBLE = Keyword("double");
        public static Parser<string> CONST = Keyword("const");
        public static Parser<string> VOLATILE = Keyword("volatile");
        public static Parser<string> VOID = Keyword("void");
        public static Parser<string> BOOL = Keyword("_Bool");
        public static Parser<string> COMPLEX = Keyword("_Complex");
        public static Parser<string> IMAGINARY = Keyword("_Imaginary");
        public static Parser<string> STRUCT = Keyword("struct");
        public static Parser<string> UNION = Keyword("union");
        public static Parser<string> ENUM = Keyword("enum");
        public static Parser<string> CASE = Keyword("case");
        public static Parser<string> DEFAULT = Keyword("default");
        public static Parser<string> IF = Keyword("if");
        public static Parser<string> ELSE = Keyword("else");
        public static Parser<string> SWITCH = Keyword("switch");
        public static Parser<string> WHILE = Keyword("while");
        public static Parser<string> DO = Keyword("do");
        public static Parser<string> FOR = Keyword("for");
        public static Parser<string> GOTO = Keyword("goto");
        public static Parser<string> CONTINUE = Keyword("continue");
        public static Parser<string> BREAK = Keyword("break");
        public static Parser<string> RETURN = Keyword("return");
        public static Parser<string> NULL = Keyword("null");

        public static Parser<string> IDENTIFIER = Ident;
        public static Parser<string> TYPEDEF_NAME = Ident; 


        //#
        //# Expressions
        //#
        //primary_expression
        //    : IDENTIFIER
        //      {
        //        checkpoint(val[0].location)
        //        result = ObjectSpecifier.new(val[0])
        //        result.head_token = result.tail_token = val[0]
        //      }
        //    | CONSTANT
        //      {
        //        checkpoint(val[0].location)
        //        result = ConstantSpecifier.new(val[0])
        //        result.head_token = result.tail_token = val[0]
        //      }
        //    | STRING_LITERAL
        //      {
        //        checkpoint(val[0].location)
        //        result = StringLiteralSpecifier.new(val[0])
        //        result.head_token = result.tail_token = val[0]
        //      }
        //    | NULL
        //      {
        //        checkpoint(val[0].location)
        //        result = NullConstantSpecifier.new(val[0])
        //        result.head_token = result.tail_token = val[0]
        //      }
        //    | "(" expression ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = GroupedExpression.new(val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[2]
        //      }
        //    | "(" compound_statement ")"
        //      {
        //        checkpoint(val[0].location)
        //        E(:E0013, val[0].location)
        //        result = ErrorExpression.new(val[0])
        //        result.head_token = val[0]
        //        result.tail_token = val[2]
        //      }
        //    ;

        //postfix_expression
        //    : primary_expression
        //    | postfix_expression "[" expression "]"
        //      {
        //        checkpoint(val[0].location)
        //        result = ArraySubscriptExpression.new(val[0], val[2], val[1])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[3]
        //      }
        //    | postfix_expression "(" ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = FunctionCallExpression.new(val[0], [], val[1])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2]
        //      }
        //    | postfix_expression "(" argument_expression_list ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = FunctionCallExpression.new(val[0], val[2], val [1])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[3]
        //      }
        //    | postfix_expression "." IDENTIFIER
        //      {
        //        checkpoint(val[0].location)
        //        result = MemberAccessByValueExpression.new(val[0], val[2], val[1])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2]
        //      }
        //    | postfix_expression "->" IDENTIFIER
        //      {
        //        checkpoint(val[0].location)
        //        result = MemberAccessByPointerExpression.new(val[0], val[2], val[1])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2]
        //      }
        //    | postfix_expression "." CONSTANT
        //      {
        //        checkpoint(val[0].location)
        //        result = BitAccessByValueExpression.new(val[0], val[2], val[1])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2]
        //      }
        //    | postfix_expression "->" CONSTANT
        //      {
        //        checkpoint(val[0].location)
        //        result = BitAccessByPointerExpression.new(val[0], val[2], val[1])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2]
        //      }
        //    | postfix_expression "++"
        //      {
        //        checkpoint(val[0].location)
        //        result = PostfixIncrementExpression.new(val[1], val[0])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[1]
        //      }
        //    | postfix_expression "--"
        //      {
        //        checkpoint(val[0].location)
        //        result = PostfixDecrementExpression.new(val[1], val[0])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[1]
        //      }
        //    | "(" type_name ")" "{" initializer_list "}"
        //      {
        //        checkpoint(val[0].location)
        //        result = CompoundLiteralExpression.new(val[1], val[4], val[0])
        //        result.head_token = val[0]
        //        result.tail_token = val[5]
        //      }
        //    | "(" type_name ")" "{" initializer_list "," "}"
        //      {
        //        checkpoint(val[0].location)
        //        result = CompoundLiteralExpression.new(val[1], val[4], val[0])
        //        result.head_token = val[0]
        //        result.tail_token = val[6]
        //      }
        //    ;

        //argument_expression_list
        //    : assignment_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = val
        //      }
        //    | argument_expression_list "," assignment_expression
        //      {
        //        checkpoint(val[0].first.location)
        //        result = val[0].push(val[2])
        //      }
        //    ;

        //unary_expression
        //    : postfix_expression
        //    | "++" unary_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = PrefixIncrementExpression.new(val[0], val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[1].tail_token
        //      }
        //    | "--" unary_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = PrefixDecrementExpression.new(val[0], val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[1].tail_token
        //      }
        //    | "&" cast_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = AddressExpression.new(val[0], val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[1].tail_token
        //      }
        //    | "*" cast_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = IndirectionExpression.new(val[0], val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[1].tail_token
        //      }
        //    | unary_arithmetic_operator cast_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = UnaryArithmeticExpression.new(val[0], val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[1].tail_token
        //      }
        //    | SIZEOF unary_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = SizeofExpression.new(val[0], val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[1].tail_token
        //      }
        //    | SIZEOF "(" type_name ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = SizeofTypeExpression.new(val[0], val[2])
        //        result.head_token = val[0]
        //        result.tail_token = val[3]
        //      }
        //    | ALIGNOF unary_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = AlignofExpression.new(val[0], val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[1].tail_token
        //      }
        //    | ALIGNOF "(" type_name ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = AlignofTypeExpression.new(val[0], val[2])
        //        result.head_token = val[0]
        //        result.tail_token = val[3]
        //      }
        //    | "&&" unary_expression
        //      {
        //        checkpoint(val[0].location)
        //        E(:E0014, val[0].location, val[0].value)
        //        result = ErrorExpression.new(val[0])
        //        result.head_token = val[0]
        //        result.tail_token = val[1].tail_token
        //      }
        //    ;

        //unary_arithmetic_operator
        //    : "+"
        //    | "-"
        //    | "~"
        //    | "!"
        //    ;

        //cast_expression
        //    : unary_expression
        //    | "(" type_name ")" cast_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = CastExpression.new(val[1], val[3])
        //        result.head_token = val[0]
        //        result.tail_token = val[3].tail_token
        //      }
        //    ;

        //multiplicative_expression
        //    : cast_expression
        //    | multiplicative_expression "*" cast_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = MultiplicativeExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    | multiplicative_expression "/" cast_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = MultiplicativeExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    | multiplicative_expression "%" cast_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = MultiplicativeExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //additive_expression
        //    : multiplicative_expression
        //    | additive_expression "+" multiplicative_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = AdditiveExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    | additive_expression "-" multiplicative_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = AdditiveExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //shift_expression
        //    : additive_expression
        //    | shift_expression "<<" additive_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = ShiftExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    | shift_expression ">>" additive_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = ShiftExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //relational_expression
        //    : shift_expression
        //    | relational_expression "<" shift_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = RelationalExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    | relational_expression ">" shift_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = RelationalExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    | relational_expression "<=" shift_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = RelationalExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    | relational_expression ">=" shift_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = RelationalExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //equality_expression
        //    : relational_expression
        //    | equality_expression "==" relational_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = EqualityExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    | equality_expression "!=" relational_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = EqualityExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //and_expression
        //    : equality_expression
        //    | and_expression "&" equality_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = AndExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //exclusive_or_expression
        //    : and_expression
        //    | exclusive_or_expression "^" and_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = ExclusiveOrExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //inclusive_or_expression
        //    : exclusive_or_expression
        //    | inclusive_or_expression "|" exclusive_or_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = InclusiveOrExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //logical_and_expression
        //    : inclusive_or_expression
        //    | logical_and_expression "&&" inclusive_or_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = LogicalAndExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //logical_or_expression
        //    : logical_and_expression
        //    | logical_or_expression "||" logical_and_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = LogicalOrExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //conditional_expression
        //    : logical_or_expression
        //    | logical_or_expression "?" expression ":" conditional_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = ConditionalExpression.new(val[0], val[2], val[4], val[1])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[4].tail_token
        //      }
        //    ;

        //assignment_expression
        //    : conditional_expression
        //    | cast_expression "=" assignment_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = SimpleAssignmentExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    | cast_expression compound_assignment_operator assignment_expression
        //      {
        //        checkpoint(val[0].location)
        //        result = CompoundAssignmentExpression.new(val[1], val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //compound_assignment_operator
        //    : "*="
        //    | "/="
        //    | "%="
        //    | "+="
        //    | "-="
        //    | "<<="
        //    | ">>="
        //    | "&="
        //    | "^="
        //    | "|="
        //    ;

        //expression
        //    : assignment_expression
        //    | expression "," assignment_expression
        //      {
        //        checkpoint(val[0].location)
        //        case val[0]
        //        when CommaSeparatedExpression
        //          result = val[0].push(val[2])
        //        else
        //          result = CommaSeparatedExpression.new(val[0]).push(val[2])
        //        end
        //      }
        //    ;

        //constant_expression
        //    : conditional_expression
        //    ;

        //#
        //# Declarations
        //#
        //declaration
        //    : declaration_specifiers ";"
        //      {
        //        checkpoint(val[0].location)
        //        @lexer.enable_identifier_translation
        //        result = Declaration.new(val[0], [], @sym_tbl)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[1]
        //      }
        //    | declaration_specifiers init_declarator_list ";"
        //      {
        //        checkpoint(val[0].location)
        //        @lexer.enable_identifier_translation
        //        result = Declaration.new(val[0], val[1], @sym_tbl)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2]
        //        result.items.each do |item|
        //          case item
        //          when TypedefDeclaration
        //            @lexer.add_typedef_name(item.identifier)
        //          when FunctionDeclaration, VariableDeclaration, VariableDefinition
        //            @lexer.add_object_name(item.identifier)
        //          end
        //        end
        //      }
        //    ;

        //global_declaration
        //    : declaration
        //    | init_declarator_list ";"
        //      {
        //        checkpoint(val[0].first.location)
        //        result = Declaration.new(nil, val[0], @sym_tbl)
        //        result.head_token = val[0].first.head_token
        //        result.tail_token = val[1]
        //      }
        //    | ";"
        //      {
        //        # NOTE: To accept extra semicolons in the global scope.
        //        E(:E0018, val[0].location)
        //        result = nil
        //      }
        //    ;

        public static Parser<DeclarationSpecifiers> declaration_specifiers = Combinator
            .Choice<Action<DeclarationSpecifiers>>(
                storage_class_specifier.Select(x => (Action<DeclarationSpecifiers>)(y => { y.storage_class_specifier = x; })),
                type_specifier.Select(x => (Action<DeclarationSpecifiers>)(y => { y.type_specifiers.Add(x); })),
                type_qualifier.Select(x => (Action<DeclarationSpecifiers>) (y => { y.type_qualifiers.Add(x); })),
                function_specifier.Select(x => (Action<DeclarationSpecifiers>) (y => { y.function_specifier = x; }))
            ).Many(1).Select(x => x.Aggregate(new DeclarationSpecifiers(), (s, y) =>
            {
                y(s);
                return s;
            }));

        //init_declarator_list
        //    : init_declarator
        //      {
        //        checkpoint(val[0].location)
        //        @lexer.disable_identifier_translation
        //        result = val
        //      }
        //    | init_declarator_list "," init_declarator
        //      {
        //        checkpoint(val[0].first.location)
        //        @lexer.disable_identifier_translation
        //        result = val[0].push(val[2])
        //      }
        //    ;

        //init_declarator
        //    : declarator
        //      {
        //        checkpoint(val[0].location)
        //        result = InitDeclarator.new(val[0], nil, nil)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[0].tail_token
        //      }
        //    | declarator "=" initializer
        //      {
        //        checkpoint(val[0].location)
        //        result = InitDeclarator.new(val[0], val[2], val[1])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        public static Parser<string> storage_class_specifier = Combinator.Choice(
            TYPEDEF,
            EXTERN,
            STATIC,
            AUTO,
            REGISTER
        );

        public static Parser<TypeSpecifier> type_specifier = Combinator.Choice(
            Combinator.Choice(VOID, CHAR, SHORT, INT, LONG, FLOAT, DOUBLE, SIGNED, UNSIGNED, BOOL, COMPLEX, IMAGINARY, TYPEDEF_NAME).Select(x => new StandardTypeSpecifier(x)),
            struct_or_union_specifier,
            enum_specifier
        );

        public static Parser<StructSpecifier> struct_or_union_specifier = Combinator.Choice(
            from _1 in STRUCT
            from _2 in IDENTIFIER.Option()
            from _3 in Symbol("{").Then(struct_declaration_list).Skip(Symbol("}"))
            select new StructSpecifier(_2 ?? create_anon_tag_name(_1), _3, _2 != null),
            from _1 in UNION
            from _2 in IDENTIFIER.Option()
            from _3 in Symbol("{").Then(struct_declaration_list).Skip(Symbol("}"))
            select new UnionSpecifier(_2 ?? create_anon_tag_name(_1), _3, _2 != null),
            from _1 in STRUCT
            from _2 in IDENTIFIER
            select new StructSpecifier(_2, null, false),
            from _1 in UNION
            from _2 in IDENTIFIER
            select new UnionSpecifier(_2, null, false)
        );

        public static Parser<StructDeclaration[]> struct_declaration_list = struct_declaration.Many();

        public static Parser<StructDeclaration> struct_declaration =
            from _1 in specifier_qualifier_list
            from _2 in struct_declarator_list.Option().Select(x => x ?? new StructDeclarator[0]).ToArray()
            from _3 in Symbol(";")
            select new StructDeclaration(_1, _2);

        //specifier_qualifier_list
        //    : specifier_qualifier_list type_specifier
        //      {
        //        checkpoint(val[0].location)
        //        result = val[0]
        //        result.type_specifiers.push(val[1])
        //        result.tail_token = val[1].tail_token
        //      }
        //    | type_specifier
        //      {
        //        checkpoint(val[0].location)
        //        result = SpecifierQualifierList.new
        //        result.type_specifiers.push(val[0])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[0].tail_token
        //      }
        //    | specifier_qualifier_list type_qualifier
        //      {
        //        checkpoint(val[0].location)
        //        result = val[0]
        //        result.type_qualifiers.push(val[1])
        //        result.tail_token = val[1]
        //      }
        //    | type_qualifier
        //      {
        //        checkpoint(val[0].location)
        //        result = SpecifierQualifierList.new
        //        result.type_qualifiers.push(val[0])
        //        result.head_token = result.tail_token = val[0]
        //      }
        //    ;

        //struct_declarator_list
        //    : struct_declarator
        //      {
        //        checkpoint(val[0].location)
        //        result = val
        //      }
        //    | struct_declarator_list "," struct_declarator
        //      {
        //        checkpoint(val[0].first.location)
        //        result = val[0].push(val[2])
        //      }
        //    ;

        //struct_declarator
        //    : declarator
        //      {
        //        checkpoint(val[0].location)
        //        result = StructDeclarator.new(val[0], nil)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[0].tail_token
        //      }
        //    | ":" constant_expression
        //      {
        //        checkpoint(val[0].location)
        //        val[1].full = true
        //        result = StructDeclarator.new(nil, val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[1].tail_token
        //      }
        //    | declarator ":" constant_expression
        //      {
        //        checkpoint(val[0].location)
        //        val[2].full = true
        //        result = StructDeclarator.new(val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //enum_specifier
        //    : ENUM "{" enumerator_list "}"
        //      {
        //        checkpoint(val[0].location)
        //        result = EnumSpecifier.new(create_anon_tag_name(val[0]),
        //                                   val[2], nil, true)
        //        result.head_token = val[0]
        //        result.tail_token = val[3]
        //        result.enumerators.each do |enum|
        //          @lexer.add_enumerator_name(enum.identifier)
        //        end
        //      }
        //    | ENUM IDENTIFIER "{" enumerator_list "}"
        //      {
        //        checkpoint(val[0].location)
        //        result = EnumSpecifier.new(val[1], val[3])
        //        result.head_token = val[0]
        //        result.tail_token = val[4]
        //        result.enumerators.each do |enum|
        //          @lexer.add_enumerator_name(enum.identifier)
        //        end
        //      }
        //    | ENUM "{" enumerator_list "," "}"
        //      {
        //        checkpoint(val[0].location)
        //        result = EnumSpecifier.new(create_anon_tag_name(val[0]),
        //                                   val[2], val[3], true)
        //        result.head_token = val[0]
        //        result.tail_token = val[4]
        //        result.enumerators.each do |enum|
        //          @lexer.add_enumerator_name(enum.identifier)
        //        end
        //      }
        //    | ENUM IDENTIFIER "{" enumerator_list "," "}"
        //      {
        //        checkpoint(val[0].location)
        //        result = EnumSpecifier.new(val[1], val[3], val[4])
        //        result.head_token = val[0]
        //        result.tail_token = val[5]
        //        result.enumerators.each do |enum|
        //          @lexer.add_enumerator_name(enum.identifier)
        //        end
        //      }
        //    | ENUM IDENTIFIER
        //      {
        //        checkpoint(val[0].location)
        //        result = EnumSpecifier.new(val[1], nil)
        //        result.head_token = val[0]
        //        result.tail_token = val[1]
        //      }
        //    ;

        //enumerator_list
        //    : enumerator
        //      {
        //        checkpoint(val[0].location)
        //        result = val
        //      }
        //    | enumerator_list "," enumerator
        //      {
        //        checkpoint(val[0].first.location)
        //        result = val[0].push(val[2])
        //      }
        //    ;

        //enumerator
        //    : enumerator_name
        //      {
        //        checkpoint(val[0].location)
        //        sym = @sym_tbl.create_new_symbol(EnumeratorName, val[0])
        //        result = Enumerator.new(val[0], nil, sym)
        //        result.head_token = result.tail_token = val[0]
        //      }
        //    | enumerator_name "=" constant_expression
        //      {
        //        checkpoint(val[0].location)
        //        val[2].full = true
        //        sym = @sym_tbl.create_new_symbol(EnumeratorName, val[0])
        //        result = Enumerator.new(val[0], val[2], sym)
        //        result.head_token = val[0]
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //enumerator_name
        //    : IDENTIFIER
        //    | TYPEDEF_NAME
        //      {
        //        result = val[0].class.new(:IDENTIFIER, val[0].value, val[0].location)
        //      }
        //    ;

        public static Parser<string> type_qualifier = Combinator.Choice(CONST, VOLATILE, RESTRICT);

        public static Parser<string> function_specifier = INLINE;

        //declarator
        //    : pointer direct_declarator
        //      {
        //        checkpoint(val[0].first.location)
        //        result = val[1]
        //        result.pointer = val[0]
        //        result.head_token = val[0].first
        //        result.full = true
        //      }
        //    | direct_declarator
        //      {
        //        checkpoint(val[0].location)
        //        result = val[0]
        //        result.full = true
        //      }
        //    ;

        //direct_declarator
        //    : IDENTIFIER
        //      {
        //        checkpoint(val[0].location)
        //        @lexer.enable_identifier_translation
        //        result = IdentifierDeclarator.new(val[0])
        //        result.head_token = result.tail_token = val[0]
        //      }
        //    | "(" declarator ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = GroupedDeclarator.new(val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[2]
        //      }
        //    | direct_declarator "[" type_qualifier_list assignment_expression "]"
        //      {
        //        checkpoint(val[0].location)
        //        val[3].full = true
        //        result = ArrayDeclarator.new(val[0], val[3])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[4]
        //      }
        //    | direct_declarator "[" type_qualifier_list "]"
        //      {
        //        checkpoint(val[0].location)
        //        result = ArrayDeclarator.new(val[0], nil)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[3]
        //      }
        //    | direct_declarator "[" assignment_expression "]"
        //      {
        //        checkpoint(val[0].location)
        //        val[2].full = true
        //        result = ArrayDeclarator.new(val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[3]
        //      }
        //    | direct_declarator
        //      "[" STATIC type_qualifier_list assignment_expression "]"
        //      {
        //        checkpoint(val[0].location)
        //        val[4].full = true
        //        result = ArrayDeclarator.new(val[0], val[4])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[5]
        //      }
        //    | direct_declarator
        //      "[" type_qualifier_list STATIC assignment_expression "]"
        //      {
        //        checkpoint(val[0].location)
        //        val[4].full = true
        //        result = ArrayDeclarator.new(val[0], val[4])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[5]
        //      }
        //    | direct_declarator "[" type_qualifier_list "*" "]"
        //      {
        //        checkpoint(val[0].location)
        //        result = ArrayDeclarator.new(val[0], nil)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[4]
        //      }
        //    | direct_declarator "[" "*" "]"
        //      {
        //        checkpoint(val[0].location)
        //        result = ArrayDeclarator.new(val[0], nil)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[3]
        //      }
        //    | direct_declarator "[" "]"
        //      {
        //        checkpoint(val[0].location)
        //        result = ArrayDeclarator.new(val[0], nil)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2]
        //      }
        //    | direct_declarator "(" { @lexer.enable_identifier_translation }
        //      parameter_type_list ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = AnsiFunctionDeclarator.new(val[0], val[3])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[4]
        //      }
        //    | direct_declarator "(" identifier_list ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = KandRFunctionDeclarator.new(val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[3]
        //      }
        //    | direct_declarator "(" ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = AbbreviatedFunctionDeclarator.new(val[0])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2]
        //      }
        //    ;

        //pointer
        //    : "*"
        //      {
        //        checkpoint(val[0].location)
        //        result = val
        //      }
        //    | "*" type_qualifier_list
        //      {
        //        checkpoint(val[0].location)
        //        result = val[1].unshift(val[0])
        //      }
        //    | "*" pointer
        //      {
        //        checkpoint(val[0].location)
        //        result = val[1].unshift(val[0])
        //      }
        //    | "*" type_qualifier_list pointer
        //      {
        //        checkpoint(val[0].location)
        //        result = val[1].unshift(val[0]).concat(val[2])
        //      }
        //    ;

        //type_qualifier_list
        //    : type_qualifier
        //      {
        //        checkpoint(val[0].location)
        //        result = val
        //      }
        //    | type_qualifier_list type_qualifier
        //      {
        //        checkpoint(val[0].first.location)
        //        result = val[0].push(val[1])
        //      }
        //    ;

        //parameter_type_list
        //    : parameter_list
        //      {
        //        checkpoint(val[0].first.location)
        //        result = ParameterTypeList.new(val[0], false)
        //        result.head_token = val[0].first.head_token
        //        result.tail_token = val[0].last.tail_token
        //      }
        //    | parameter_list "," "..."
        //      {
        //        checkpoint(val[0].first.location)
        //        result = ParameterTypeList.new(val[0], true)
        //        result.head_token = val[0].first.head_token
        //        result.tail_token = val[2]
        //      }
        //    ;

        //parameter_list
        //    : parameter_declaration
        //      {
        //        checkpoint(val[0].location)
        //        result = val
        //      }
        //    | parameter_list "," parameter_declaration
        //      {
        //        checkpoint(val[0].first.location)
        //        result = val[0].push(val[2])
        //      }
        //    ;

        //parameter_declaration
        //    : declaration_specifiers declarator
        //      {
        //        checkpoint(val[0].location)
        //        @lexer.enable_identifier_translation
        //        result = ParameterDeclaration.new(val[0], val[1])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[1].tail_token
        //      }
        //    | declaration_specifiers abstract_declarator
        //      {
        //        checkpoint(val[0].location)
        //        @lexer.enable_identifier_translation
        //        result = ParameterDeclaration.new(val[0], val[1])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[1].tail_token
        //      }
        //    | declaration_specifiers
        //      {
        //        checkpoint(val[0].location)
        //        @lexer.enable_identifier_translation
        //        result = ParameterDeclaration.new(val[0], nil)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[0].tail_token
        //      }
        //    ;

        //identifier_list
        //    : IDENTIFIER
        //      {
        //        checkpoint(val[0].location)
        //        result = val
        //      }
        //    | identifier_list "," IDENTIFIER
        //      {
        //        checkpoint(val[0].first.location)
        //        result = val[0].push(val[2])
        //      }
        //    ;

        //type_name
        //    : specifier_qualifier_list
        //      {
        //        checkpoint(val[0].location)
        //        @lexer.enable_identifier_translation
        //        result = TypeName.new(val[0], nil, @sym_tbl)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[0].tail_token
        //      }
        //    | specifier_qualifier_list abstract_declarator
        //      {
        //        checkpoint(val[0].location)
        //        @lexer.enable_identifier_translation
        //        result = TypeName.new(val[0], val[1], @sym_tbl)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[1].tail_token
        //      }
        //    ;

        //abstract_declarator
        //    : pointer
        //      {
        //        checkpoint(val[0].first.location)
        //        @lexer.enable_identifier_translation
        //        result = PointerAbstractDeclarator.new(nil, val[0])
        //        result.head_token = val[0].first
        //        result.tail_token = val[0].last
        //        result.full = true
        //      }
        //    | pointer direct_abstract_declarator
        //      {
        //        checkpoint(val[0].first.location)
        //        @lexer.enable_identifier_translation
        //        result = PointerAbstractDeclarator.new(val[1], val[0])
        //        result.head_token = val[0].first
        //        result.tail_token = val[1].tail_token
        //        result.full = true
        //      }
        //    | direct_abstract_declarator
        //      {
        //        checkpoint(val[0].location)
        //        result = val[0]
        //        result.full = true
        //      }
        //    ;

        //direct_abstract_declarator
        //    : "(" abstract_declarator ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = GroupedAbstractDeclarator.new(val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[2]
        //      }
        //    | "[" "]"
        //      {
        //        checkpoint(val[0].location)
        //        result = ArrayAbstractDeclarator.new(nil, nil)
        //        result.head_token = val[0]
        //        result.tail_token = val[1]
        //      }
        //    | "[" assignment_expression "]"
        //      {
        //        checkpoint(val[0].location)
        //        val[1].full = true
        //        result = ArrayAbstractDeclarator.new(nil, val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[2]
        //      }
        //    | direct_abstract_declarator "[" "]"
        //      {
        //        checkpoint(val[0].location)
        //        result = ArrayAbstractDeclarator.new(val[0], nil)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2]
        //      }
        //    | direct_abstract_declarator "[" assignment_expression "]"
        //      {
        //        checkpoint(val[0].location)
        //        val[2].full = true
        //        result = ArrayAbstractDeclarator.new(val[0], val[2])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[3]
        //      }
        //    | "[" "*" "]"
        //      {
        //        checkpoint(val[0].location)
        //        result = ArrayAbstractDeclarator.new(nil, nil)
        //        result.head_token = val[0]
        //        result.tail_token = val[2]
        //      }
        //    | direct_abstract_declarator "[" "*" "]"
        //      {
        //        checkpoint(val[0].location)
        //        result = ArrayAbstractDeclarator.new(val[0], nil)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[3]
        //      }
        //    | "(" ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = FunctionAbstractDeclarator.new(nil, nil)
        //        result.head_token = val[0]
        //        result.tail_token = val[1]
        //      }
        //    | "(" { @lexer.enable_identifier_translation } parameter_type_list ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = FunctionAbstractDeclarator.new(nil, val[2])
        //        result.head_token = val[0]
        //        result.tail_token = val[3]
        //      }
        //    | direct_abstract_declarator "(" ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = FunctionAbstractDeclarator.new(val[0], nil)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2]
        //      }
        //    | direct_abstract_declarator "(" { @lexer.enable_identifier_translation }
        //      parameter_type_list ")"
        //      {
        //        checkpoint(val[0].location)
        //        result = FunctionAbstractDeclarator.new(val[0], val[3])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[4]
        //      }
        //    ;

        //initializer
        //    : assignment_expression
        //      {
        //        checkpoint(val[0].location)
        //        val[0].full = true
        //        result = Initializer.new(val[0], nil)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[0].tail_token
        //      }
        //    | "{" "}"
        //      {
        //        checkpoint(val[0].location)
        //        result = Initializer.new(nil, nil)
        //        result.head_token = val[0]
        //        result.tail_token = val[1]
        //      }
        //    | "{" initializer_list "}"
        //      {
        //        checkpoint(val[0].location)
        //        result = Initializer.new(nil, val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[2]
        //      }
        //    | "{" initializer_list "," "}"
        //      {
        //        checkpoint(val[0].location)
        //        result = Initializer.new(nil, val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[3]
        //      }
        //    ;

        //initializer_list
        //    : initializer
        //      {
        //        checkpoint(val[0].location)
        //        result = val
        //      }
        //    | designation initializer
        //      {
        //        checkpoint(val[1].location)
        //        result = [val[1]]
        //      }
        //    | initializer_list "," initializer
        //      {
        //        checkpoint(val[0].first.location)
        //        result = val[0].push(val[2])
        //      }
        //    | initializer_list "," designation initializer
        //      {
        //        checkpoint(val[0].first.location)
        //        result = val[0].push(val[3])
        //      }
        //    ;

        //designation
        //    : designator_list "="
        //    ;

        //designator_list
        //    : designator
        //    | designator_list designator
        //    ;

        //designator
        //    : "[" constant_expression "]"
        //    | "." IDENTIFIER
        //    ;

        //#
        //# Statements
        //#
        //statement
        //    : labeled_statement
        //    | compound_statement
        //    | expression_statement
        //    | selection_statement
        //    | iteration_statement
        //    | jump_statement
        //    ;

        //labeled_statement
        //    : label_name ":" statement
        //      {
        //        checkpoint(val[0].location)
        //        result = GenericLabeledStatement.new(val[0], val[2])
        //        result.head_token = val[0]
        //        result.tail_token = val[2].tail_token
        //      }
        //    | CASE constant_expression ":" statement
        //      {
        //        checkpoint(val[0].location)
        //        val[1].full = true
        //        result = CaseLabeledStatement.new(val[1], val[3])
        //        result.head_token = val[0]
        //        result.tail_token = val[3].tail_token
        //      }
        //    | DEFAULT ":" statement
        //      {
        //        checkpoint(val[0].location)
        //        result = DefaultLabeledStatement.new(val[2])
        //        result.head_token = val[0]
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //label_name
        //    : IDENTIFIER
        //    | TYPEDEF_NAME
        //      {
        //        result = val[0].class.new(:IDENTIFIER, val[0].value, val[0].location)
        //      }
        //    ;

        //compound_statement
        //    : "{" "}"
        //      {
        //        checkpoint(val[0].location)
        //        result = CompoundStatement.new([])
        //        result.head_token = val[0]
        //        result.tail_token = val[1]
        //      }
        //    | "{" { @lexer.enter_scope } block_item_list { @lexer.leave_scope } "}"
        //      {
        //        checkpoint(val[0].location)
        //        result = CompoundStatement.new(val[2])
        //        result.head_token = val[0]
        //        result.tail_token = val[4]
        //      }
        //    ;

        //block_item_list
        //    : block_item
        //      {
        //        checkpoint(val[0].location)
        //        result = val
        //      }
        //    | block_item_list block_item
        //      {
        //        checkpoint(val[0].first.location)
        //        result = val[0].push(val[1])
        //      }
        //    ;

        //block_item
        //    : declaration
        //    | statement
        //    | local_function_definition
        //    ;

        //expression_statement
        //    : ";"
        //      {
        //        checkpoint(val[0].location)
        //        result = ExpressionStatement.new(nil)
        //        result.head_token = result.tail_token = val[0]
        //      }
        //    | expression ";"
        //      {
        //        checkpoint(val[0].location)
        //        val[0].full = true
        //        result = ExpressionStatement.new(val[0])
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[1]
        //      }
        //    ;

        //selection_statement
        //    : IF "(" expression ")" statement
        //      {
        //        checkpoint(val[0].location)
        //        val[2].full = true
        //        result = IfStatement.new(val[2], val[4], val[3])
        //        result.head_token = val[0]
        //        result.tail_token = val[4].tail_token
        //      }
        //    | IF "(" expression ")" statement ELSE statement
        //      {
        //        checkpoint(val[0].location)
        //        val[2].full = true
        //        result = IfElseStatement.new(val[2], val[4], val[6], val[3], val[5])
        //        result.head_token = val[0]
        //        result.tail_token = val[6].tail_token
        //      }
        //    | SWITCH "(" expression ")" statement
        //      {
        //        checkpoint(val[0].location)
        //        val[2].full = true
        //        result = SwitchStatement.new(val[2], val[4])
        //        result.head_token = val[0]
        //        result.tail_token = val[4].tail_token
        //      }
        //    ;

        //iteration_statement
        //    : WHILE "(" expression ")" statement
        //      {
        //        checkpoint(val[0].location)
        //        val[2].full = true
        //        result = WhileStatement.new(val[2], val[4], val[3])
        //        result.head_token = val[0]
        //        result.tail_token = val[4].tail_token
        //      }
        //    | DO statement WHILE "(" expression ")" ";"
        //      {
        //        checkpoint(val[0].location)
        //        val[4].full = true
        //        result = DoStatement.new(val[1], val[4], val[0], val[2])
        //        result.head_token = val[0]
        //        result.tail_token = val[6]
        //      }
        //    | FOR "(" expression_statement expression_statement ")" statement
        //      {
        //        checkpoint(val[0].location)
        //        result = ForStatement.new(val[2], val[3], nil, val[5], val[4])
        //        result.head_token = val[0]
        //        result.tail_token = val[5].tail_token
        //      }
        //    | FOR "(" expression_statement expression_statement expression ")"
        //      statement
        //      {
        //        checkpoint(val[0].location)
        //        val[4].full = true
        //        result = ForStatement.new(val[2], val[3], val[4], val[6], val[5])
        //        result.head_token = val[0]
        //        result.tail_token = val[6].tail_token
        //      }
        //    | FOR "(" declaration expression_statement ")" statement
        //      {
        //        checkpoint(val[0].location)
        //        result = C99ForStatement.new(val[2], val[3], nil, val[5], val[4])
        //        result.head_token = val[0]
        //        result.tail_token = val[5].tail_token
        //      }
        //    | FOR "(" declaration expression_statement expression ")" statement
        //      {
        //        checkpoint(val[0].location)
        //        val[4].full = true
        //        result = C99ForStatement.new(val[2], val[3], val[4], val[6], val[5])
        //        result.head_token = val[0]
        //        result.tail_token = val[6].tail_token
        //      }
        //    ;

        //jump_statement
        //    : GOTO label_name ";"
        //      {
        //        checkpoint(val[0].location)
        //        result = GotoStatement.new(val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[2]
        //      }
        //    | GOTO "*" expression ";"
        //      {
        //        checkpoint(val[0].location)
        //        E(:E0015, val[1].location, val[1].value)
        //        result = ErrorStatement.new(val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[3]
        //      }
        //    | CONTINUE ";"
        //      {
        //        checkpoint(val[0].location)
        //        result = ContinueStatement.new
        //        result.head_token = val[0]
        //        result.tail_token = val[1]
        //      }
        //    | BREAK ";"
        //      {
        //        checkpoint(val[0].location)
        //        result = BreakStatement.new
        //        result.head_token = val[0]
        //        result.tail_token = val[1]
        //      }
        //    | RETURN ";"
        //      {
        //        checkpoint(val[0].location)
        //        result = ReturnStatement.new(nil)
        //        result.head_token = val[0]
        //        result.tail_token = val[1]
        //      }
        //    | RETURN expression ";"
        //      {
        //        checkpoint(val[0].location)
        //        val[1].full = true
        //        result = ReturnStatement.new(val[1])
        //        result.head_token = val[0]
        //        result.tail_token = val[2]
        //      }
        //    ;

        //#
        //# External definitions
        //#
        public Parser<TranslationUnit> translation_unit = external_declaration.Many().Select(x => new TranslationUnit(x));

public Parser<ExternalDeclaration> external_declaration = Combinator.Choice(function_definition, global_declaration);

        public Parser<FunctionDefinition> function_definition =
            Combinator.Choice(
                (
                    from _1 in declaration_specifiers
                    from _2 in declarator
                    from _3 in declaration_list
                    from _4 in compound_statement
                    select new FunctionDefinition.KandRFunctionDefinition(_1, _2, _3, _4)
                ),
                (
                    from _1 in declaration_specifiers
                    from _2 in declarator
                    from _3 in compound_statement
                    select (_2 is AnsiFunctionDeclarator) ? new FunctionDefinition.AnsiFunctionDefinition(_1, _2, _3)
                         : (_2 is KandRFunctionDeclarator) ? new FunctionDefinition.KandRFunctionDefinition(_1, _2, new[] { }, _3)
                         : (_2 is AbbreviatedFunctionDeclarator) ? new FunctionDefinition.AnsiFunctionDefinition(_1, _2, _3)
                         : null
                ),
                (
                    from _1 in declarator
                    from _2 in declaration_list
                    from _3 in compound_statement
                    select new FunctionDefinition.KandRFunctionDefinition(null,_1, _2, _3)
                ),
                (
                    from _1 in declarator 
                    from _2 in compound_statement
                    select (_1 is AnsiFunctionDeclarator) ? new FunctionDefinition.AnsiFunctionDefinition(null, _1, _2)
                         : (_1 is KandRFunctionDeclarator) ? new FunctionDefinition.KandRFunctionDefinition(null, _1, new[] { }, _2)
                         : (_1 is AbbreviatedFunctionDeclarator) ? new FunctionDefinition.AnsiFunctionDefinition(null, _1, _2)
                         : null
                )
            );

        //local_function_definition
        //    : declaration_specifiers declarator declaration_list compound_statement
        //      {
        //        checkpoint(val[0].location)
        //        result = KandRFunctionDefinition.new(val[0], val[1], val[2], val[3],
        //                                             @sym_tbl)
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[3].tail_token
        //      }
        //    | declaration_specifiers declarator compound_statement
        //      {
        //        checkpoint(val[0].location)
        //        case val[1]
        //        when AnsiFunctionDeclarator
        //          result = AnsiFunctionDefinition.new(val[0], val[1], val[2], @sym_tbl)
        //        when KandRFunctionDeclarator
        //          result = KandRFunctionDefinition.new(val[0], val[1], [], val[2],
        //                                               @sym_tbl)
        //        when AbbreviatedFunctionDeclarator
        //          result = AnsiFunctionDefinition.new(val[0], val[1], val[2], @sym_tbl)
        //        end
        //        result.head_token = val[0].head_token
        //        result.tail_token = val[2].tail_token
        //      }
        //    ;

        //declaration_list
        //    : declaration
        //      {
        //        checkpoint(val[0].location)
        //        result = val
        //      }
        //    | declaration_list declaration
        //      {
        //        checkpoint(val[0].first.location)
        //        result = val[0].push(val[1])
        //      }
        //    ;

        //end
    }

    public class StructSpecifier : TypeSpecifier
    {
    }
    public class UnionSpecifier : TypeSpecifier
    {
    }

    public class StandardTypeSpecifier: TypeSpecifier
    {
        public StandardTypeSpecifier(string s)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class TypeSpecifier
    {
    }

    public class DeclarationSpecifiers
    {
    }

    public abstract class FunctionDefinition
    {
        public class KandRFunctionDefinition : FunctionDefinition
        {
            public KandRFunctionDefinition(object o, object o1, object o2, object o3)
            {
                throw new NotImplementedException();
            }
        }

        public class AnsiFunctionDefinition
        {
            public AnsiFunctionDefinition(object o, object o1, object o2)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class ExternalDeclaration
    {
    }

    public class TranslationUnit { }

}
