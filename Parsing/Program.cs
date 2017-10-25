using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Parsing;

namespace CParser2
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    public static class CParser
    {

        #region Tokenize rules

        public static Parser<string> new_line = Combinator.Choice(Combinator.Token("\r\n"), Combinator.Token("\r"), Combinator.Token("\n"));

        public static Parser<string> space = Combinator.Choice(Combinator.Token("\r\n"), Combinator.AnyChar(" \t\v\r\n\f").String());
        public static Parser<string> spaces = space.Many(1).Select(String.Concat);
        public static Parser<string> isspace = space.Option();
        public static Parser<string> isspaces = space.Many().Select(String.Concat);

        public static Parser<char> digit = Combinator.AnyChar("0123456789");
        public static Parser<string> digits = digit.Many(1).String();
        public static Parser<string> isdigits = digit.Many().String();

        public static Parser<char> alpha = Combinator.AnyChar((x) => ('a' <= x && x <= 'z') || ('A' <= x && x <= 'Z'));
        public static Parser<char> xdigit = Combinator.AnyChar("0123456789ABCDEFabcdef");

        public static Parser<string> e = from _1 in Combinator.AnyChar("eE")
                                         from _2 in Combinator.AnyChar("+-").Option().String()
                                         from _3 in digit.Many(1).String()
                                         select _1 + _2 + _3
                                        ;
        public static Parser<char> float_size = Combinator.AnyChar("fFlL");
        public static Parser<string> int_size = Combinator.AnyChar("uUlL").Many(1).String();

        public static Parser<string> ise = e.Option();
        public static Parser<char> isfloat_size = float_size.Option();
        public static Parser<string> isint_size = int_size.Option();

        public static Parser<string> comment = Combinator.Choice(
            // block comment
            from _1 in Combinator.Token("/*")
            from _2 in Combinator.Token("*/").Not().Then(Combinator.AnyChar()).Many().String()
            from _3 in Combinator.Token("*/")
            select _1 + _2 + _3,
            // line comment
            from _1 in Combinator.Token("//")
            from _2 in new_line.Not().Then(Combinator.AnyChar()).Many().String()
            from _3 in new_line
            select _1 + _2 + _3
        );

        public static Parser<string> identifier =
            from _1 in alpha
            from _2 in Combinator.Choice(alpha, digit).Many().String()
            from _3 in isspaces
            select _1 + _2;

        public static Parser<string> typedef_name = identifier;

        public static Parser<string> hex_constant =
            from _1 in Combinator.Choice(Combinator.Token("0x"), Combinator.Token("0X"))
            from _2 in xdigit.Many(1).String()
            from _3 in isint_size
            from _4 in isspaces
            select _1 + _2 + _3;

        public static Parser<string> octal_constant =
            from _1 in Combinator.Token("0")
            from _2 in digits
            from _3 in isint_size
            from _4 in isspaces
            select _1 + _2 + _3;

        public static Parser<string> decimal_constant =
            from _1 in digits
            from _2 in isint_size
            from _3 in isspaces
            select _1 + _2;

        public static Parser<string> string_constant =
            from _1 in Combinator.Token("L").Option()
            from _2 in Combinator.Token("'")
            from _3 in Combinator.Choice(
                Combinator.Token("\\").Then(Combinator.AnyChar()).Select(x => $@"\{x}"),
                Combinator.AnyChar(@"'").Not().Then(Combinator.AnyChar()).String()
            ).Many(1)
            from _4 in Combinator.Token("'")
            from _5 in isspaces
            select _1 + _2 + _3 + 4;

        public static Parser<string> float_constant =
            Combinator.Choice(
                from _1 in digits
                from _2 in e
                from _3 in isfloat_size.String()
                select _1 + _2 + _3
                ,
                from _1 in isdigits
                from _2 in Combinator.Token(".")
                from _3 in digits
                from _4 in ise
                from _5 in isfloat_size.String()
                select _1 + _2 + _3 + _4 + _5
                ,
                from _1 in digits
                from _2 in Combinator.Token(".")
                from _3 in isdigits
                from _4 in ise
                from _5 in isfloat_size.String()
                select _1 + _2 + _3 + _4 + _5
            );

        public static Parser<string> constant =
            Combinator.Choice(
                hex_constant,
                octal_constant,
                decimal_constant,
                float_constant,
                string_constant
            );

        public static Parser<string> auto_keyword = Combinator.Token("auto").Skip(isspaces);
        public static Parser<string> break_keyword = Combinator.Token("break").Skip(isspaces);
        public static Parser<string> case_keyword = Combinator.Token("case").Skip(isspaces);
        public static Parser<string> char_keyword = Combinator.Token("char").Skip(isspaces);
        public static Parser<string> const_keyword = Combinator.Token("const").Skip(isspaces);
        public static Parser<string> continue_keyword = Combinator.Token("continue").Skip(isspaces);
        public static Parser<string> default_keyword = Combinator.Token("default").Skip(isspaces);
        public static Parser<string> do_keyword = Combinator.Token("do").Skip(isspaces);
        public static Parser<string> double_keyword = Combinator.Token("double").Skip(isspaces);
        public static Parser<string> else_keyword = Combinator.Token("else").Skip(isspaces);
        public static Parser<string> enum_keyword = Combinator.Token("enum").Skip(isspaces);
        public static Parser<string> extern_keyword = Combinator.Token("extern").Skip(isspaces);
        public static Parser<string> float_keyword = Combinator.Token("float").Skip(isspaces);
        public static Parser<string> for_keyword = Combinator.Token("for").Skip(isspaces);
        public static Parser<string> goto_keyword = Combinator.Token("goto").Skip(isspaces);
        public static Parser<string> if_keyword = Combinator.Token("if").Skip(isspaces);
        public static Parser<string> int_keyword = Combinator.Token("int").Skip(isspaces);
        public static Parser<string> long_keyword = Combinator.Token("long").Skip(isspaces);
        public static Parser<string> register_keyword = Combinator.Token("register").Skip(isspaces);
        public static Parser<string> return_keyword = Combinator.Token("return").Skip(isspaces);
        public static Parser<string> short_keyword = Combinator.Token("short").Skip(isspaces);
        public static Parser<string> signed_keyword = Combinator.Token("signed").Skip(isspaces);
        public static Parser<string> sizeof_keyword = Combinator.Token("sizeof").Skip(isspaces);
        public static Parser<string> static_keyword = Combinator.Token("static").Skip(isspaces);
        public static Parser<string> struct_keyword = Combinator.Token("struct").Skip(isspaces);
        public static Parser<string> switch_keyword = Combinator.Token("switch").Skip(isspaces);
        public static Parser<string> typedef_keyword = Combinator.Token("typedef").Skip(isspaces);
        public static Parser<string> union_keyword = Combinator.Token("union").Skip(isspaces);
        public static Parser<string> unsigned_keyword = Combinator.Token("unsigned").Skip(isspaces);
        public static Parser<string> void_keyword = Combinator.Token("void").Skip(isspaces);
        public static Parser<string> volatile_keyword = Combinator.Token("volatile").Skip(isspaces);
        public static Parser<string> while_keyword = Combinator.Token("while").Skip(isspaces);

        public static Parser<string> bool_keyword = Combinator.Token("_Bool").Skip(isspaces);
        public static Parser<string> complex_keyword = Combinator.Token("_Complex").Skip(isspaces);
        public static Parser<string> imaginary_keyword = Combinator.Token("_Imaginary").Skip(isspaces);
        public static Parser<string> restrict_keyword = Combinator.Token("restrict").Skip(isspaces);
        public static Parser<string> inline_keyword = Combinator.Token("inline").Skip(isspaces);


        public static Parser<string> ellipsis = Combinator.Token("...").Skip(isspaces);
        public static Parser<string> semicolon = Combinator.Token(";").Skip(isspaces);
        public static Parser<string> comma = Combinator.Token(",").Skip(isspaces);
        public static Parser<string> colon = Combinator.Token(":").Skip(isspaces);
        public static Parser<string> left_paren = Combinator.Token("(").Skip(isspaces);
        public static Parser<string> right_paren = Combinator.Token(")").Skip(isspaces);
        public static Parser<string> member_access = Combinator.Token(".").Skip(isspaces);
        public static Parser<string> question_mark = Combinator.Token("?").Skip(isspaces);

        public static Parser<string> string_literal =
            from _1 in Combinator.Token("L").Option()
            from _2 in Combinator.Token("\"")
            from _3 in Combinator.Choice(
                Combinator.Token("\\").Then(Combinator.AnyChar()).Select(x => $@"\{x}"),
                Combinator.AnyChar("\"").Not().Then(Combinator.AnyChar()).String()
            ).Many(1)
            from _4 in Combinator.Token("\"")
            from _5 in isspaces
            select _1 + _2 + _3 + 4;

        public static Parser<string> left_brace = Combinator.Choice(Combinator.Token("{"), Combinator.Token("<%")).Skip(isspaces);
        public static Parser<string> right_brace = Combinator.Choice(Combinator.Token("}"), Combinator.Token("%>")).Skip(isspaces);

        public static Parser<string> left_bracket = Combinator.Choice(Combinator.Token("["), Combinator.Token("<:")).Skip(isspaces);
        public static Parser<string> right_bracket = Combinator.Choice(Combinator.Token("]"), Combinator.Token(":>")).Skip(isspaces);


        public static Parser<string> right_shift_assign = Combinator.Token(">>=").Skip(isspaces);
        public static Parser<string> left_shift_assign = Combinator.Token("<<=").Skip(isspaces);
        public static Parser<string> add_assign = Combinator.Token("+=").Skip(isspaces);
        public static Parser<string> subtract_assign = Combinator.Token("-=").Skip(isspaces);
        public static Parser<string> multiply_assign = Combinator.Token("*=").Skip(isspaces);
        public static Parser<string> divide_assign = Combinator.Token("/=").Skip(isspaces);
        public static Parser<string> modulus_assign = Combinator.Token("%=").Skip(isspaces);
        public static Parser<string> binary_and_assign = Combinator.Token("&=").Skip(isspaces);
        public static Parser<string> xor_assign = Combinator.Token("^=").Skip(isspaces);
        public static Parser<string> binary_or_assign = Combinator.Token("|=").Skip(isspaces);
        public static Parser<string> inc = Combinator.Token("++").Skip(isspaces);
        public static Parser<string> dec = Combinator.Token("--").Skip(isspaces);
        public static Parser<string> pointer_access = Combinator.Token("->").Skip(isspaces);
        public static Parser<string> logical_and = Combinator.Token("&&").Skip(isspaces);
        public static Parser<string> logical_or = Combinator.Token("||").Skip(isspaces);
        public static Parser<string> less_equal = Combinator.Token("<=").Skip(isspaces);
        public static Parser<string> greater_equal = Combinator.Token(">=").Skip(isspaces);
        public static Parser<string> equal = Combinator.Token("==").Skip(isspaces);
        public static Parser<string> not_equal = Combinator.Token("!=").Skip(isspaces);
        public static Parser<string> assign = Combinator.Token("=").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces);
        public static Parser<string> add = Combinator.Token("+").Skip(Combinator.AnyChar("+=").Not()).Skip(isspaces);
        public static Parser<string> subtract = Combinator.Token("-").Skip(Combinator.AnyChar("-=").Not()).Skip(isspaces);
        public static Parser<string> multiply = Combinator.Token("*").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces);
        public static Parser<string> divide = Combinator.Token("/").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces);
        public static Parser<string> modulus = Combinator.Token("%").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces);
        public static Parser<string> less = Combinator.Token("<").Skip(Combinator.AnyChar("<=").Not()).Skip(isspaces);
        public static Parser<string> greater = Combinator.Token(">").Skip(Combinator.AnyChar(">=").Not()).Skip(isspaces);
        public static Parser<string> negate = Combinator.Token("!").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces);
        public static Parser<string> binary_or = Combinator.Token("|").Skip(Combinator.AnyChar("|=").Not()).Skip(isspaces);
        public static Parser<string> binary_and = Combinator.Token("&").Skip(Combinator.AnyChar("&=").Not()).Skip(isspaces);
        public static Parser<string> xor = Combinator.Token("^").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces);
        public static Parser<string> left_shift = Combinator.Token("<<").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces);
        public static Parser<string> right_shift = Combinator.Token(">>").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces);
        public static Parser<string> inverse = Combinator.Token("~").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces);

        #endregion

        #region Syntax rules

        public static Parser<string> IDENTIFIER = identifier;
        public static Parser<string> TYPEDEF_NAME = identifier;
        public static Parser<string> CONSTANT = constant;

        //#
        //# Expressions
        //#


        public static Parser<Expression> postfix_expression = Combinator.Choice(
            IDENTIFIER.Select(x => new ObjectSpecifier(x)),
            CONSTANT.Select(x => new ConstantSpecifier(x)),
            string_literal.Select(x => new StringLiteralSpecifier(x)),
            //NULL.Select(x => new NullConstantSpecifier(x)),
            left_paren.Then(expression).Skip(right_paren).Select(x => new GroupedExpression(x)),
            left_paren.Then(compound_statement).Skip(right_paren).Select(x => new GroupedExpression(x))
        );

        public static Parser<Expression> postfix_expression =
            from _1 in Combinator.Choice(
                primary_expression,
                from _2 in left_paren
                from _3 in type_name
                from _4 in right_paren
                from _5 in left_brace
                from _6 in initializer_list
                from _7 in comma.Option()
                from _8 in right_brace
                select new CompoundLiteralExpression(_3, _7)
            )
            from _9 in Combinator.Choice<Func<Expression, Expression>>(
                from _10 in left_bracket
                from _11 in expression
                from _12 in right_bracket
                select (Func<Expression, Expression>) (x => (Expression) new ArraySubscriptExpression(x, _11)),
                from _10 in left_paren
                from _11 in argument_expression_list.Option()
                from _12 in right_paren
                select (Func<Expression, Expression>) (x =>
                    (Expression) new FunctionCallExpression(x, _11 ?? new Expression[0])),
                from _10 in member_access
                from _11 in IDENTIFIER
                select (Func<Expression, Expression>) (x => (Expression) new MemberAccessByValueExpression(x, _11)),
                from _10 in pointer_access
                from _11 in IDENTIFIER
                //select (Func<Expression, Expression>) (x => (Expression) new MemberAccessByPointerExpression(x, _11)),
                //from _10 in member_access
                //from _11 in CONSTANT
                //select (Func<Expression, Expression>) (x => (Expression) new BitAccessByValueExpression(x, _11)),
                //from _10 in pointer_access
                //from _11 in CONSTANT
                select (Func<Expression, Expression>) (x => (Expression) new BitAccessByPointerExpression(x, _11)),
                from _10 in inc
                select (Func<Expression, Expression>) (x => (Expression) new PostfixIncrementExpression(x)),
                from _10 in dec
                select (Func<Expression, Expression>) (x => (Expression) new PostfixDecrementExpression(x))
            ).Many()
            select _9.Aggregate(_1, (s, x) => x(s));

        public static Parser<Expression[]> argument_expression_list = assignment_expression.Repeat1(comma);

        public static Parser<Expression> unary_expression = Combinator.Choice(
            postfix_expression,
            inc.Then(unary_expression).Select(x => new PrefixIncrementExpression("++", x)),
            dec.Then(unary_expression).Select(x => new PrefixIncrementExpression("--", x)),
            and_expression.Then(cast_expression).Select(x => new AddressExpression("&", x)),
            multiply.Then(cast_expression).Select(x => new IndirectionExpression("*", x)),
            from _1 in unary_arithmetic_operator from _2 in cast_expression select new UnaryArithmeticExpression(_1, _2),
            sizeof_keyword.Then(unary_expression).Select(x => new SizeofExpression("sizeof", x)),
            from _1 in sizeof_keyword from _2 in left_paren from _3 in type_name from _4 in right_paren select new SizeofTypeExpression(_1, _3),
            alighof_keyword.Then(unary_expression).Select(x => new AlignofExpression("alignof", x)),
            from _1 in alighof_keyword from _2 in left_paren from _3 in type_name from _4 in right_paren select new AlignofTypeExpression(_1, _3)
        );

        public static Parser<string> unary_arithmetic_operator =
            Combinator.Choice(add, subtract, inverse, negate);

        public static Parser<Expression> cast_expression = 
            from _1 in left_paren.Then(type_name).Skip(right_paren).Many()
            from _2 in unary_expression
            select _1.Reverse().Aggregate(_2, (s, x) => new CastExpression(x,s));

        public static Parser<Expression> multiplicative_expression =
            from _1 in cast_expression
            from _2 in (
                from _3 in Combinator.Choice(multiply, divide, modulus)
                from _4 in cast_expression
                select Tuple.Create(_3, _4)
            ).Many()
            select _2.Aggregate(_1, (s, x) => new MultiplicativeExpression(x.Item1, s, x.Item2));

        public static Parser<Expression> additive_expression =
            from _1 in multiplicative_expression
            from _2 in (
                from _3 in Combinator.Choice(add, subtract)
                from _4 in multiplicative_expression
                select Tuple.Create(_3, _4)
            ).Many()
            select _2.Aggregate(_1, (s, x) => new AdditiveExpression(x.Item1, s, x.Item2));

        public static Parser<Expression> shift_expression =
            from _1 in additive_expression
            from _2 in (
                from _3 in Combinator.Choice(left_shift, right_shift)
                from _4 in additive_expression
                select Tuple.Create(_3, _4)
            ).Many()
            select _2.Aggregate(_1, (s, x) => new ShiftExpression(x.Item1, s, x.Item2));

        public static Parser<Expression> relational_expression =
            from _1 in shift_expression
            from _2 in (
                from _3 in Combinator.Choice(less_equal, less, greater_equal, greater)
                from _4 in shift_expression
                select Tuple.Create(_3, _4)
            ).Many()
            select _2.Aggregate(_1, (s, x) => new RelationalExpression(x.Item1, s, x.Item2));

        public static Parser<Expression> equality_expression =
            from _1 in relational_expression
            from _2 in (
                from _3 in Combinator.Choice(equal, not_equal)
                from _4 in relational_expression
                select Tuple.Create(_3,_4)
            ).Many()
            select _2.Aggregate(_1, (s, x) => new EqualityExpression(x.Item1, s, x.Item2));

        public static Parser<Expression> and_expression =
            equality_expression.Repeat1(binary_and).Select(x => x.Aggregate((s, y) => new AndExpression("&", s, y)));

        public static Parser<Expression> exclusive_or_expression =
            and_expression.Repeat1(xor).Select(x => x.Aggregate((s, y) => new ExclusiveOrExpression("^", s, y)));

        public static Parser<Expression> inclusive_or_expression =
            exclusive_or_expression.Repeat1(binary_or).Select(x => x.Aggregate((s, y) => new InclusiveOrExpression("|", s, y)));

        public static Parser<Expression> logical_and_expression =
            inclusive_or_expression.Repeat1(logical_and).Select(x => x.Aggregate((s, y) => new LogicalAndExpression("&&", s, y)));

        public static Parser<Expression> logical_or_expression = 
            logical_and_expression.Repeat1(logical_or).Select(x => x.Aggregate((s,y) => new LogicalOrExpression("||",s,y)));

        public static Parser<Expression> conditional_expression = Combinator.Choice(
            from _1 in logical_or_expression
            from _2 in (
                from _3 in question_mark
                from _4 in expression
                from _5 in colon
                from _6 in conditional_expression
                select new ConditionalExpression(_1, _4, _6, _3)
            ).Option()
            select _2 ?? _1
        );

        public static Parser<Expression> assignment_expression =
            Combinator.Choice(
                conditional_expression,
                from _1 in cast_expression
                from _2 in assign
                from _3 in assignment_expression
                select new SimpleAssignmentExpression(_2, _1, _3),
                from _1 in cast_expression
                from _2 in compound_assignment_operator
                from _3 in assignment_expression
                select new CompoundAssignmentExpression(_2, _1, _3)

            );

        public static Parser<string> compound_assignment_operator =
            Combinator.Choice(
                multiply_assign, divide_assign, modulus_assign, add_assign, subtract_assign,
                left_shift_assign, right_shift_assign, binary_and_assign, binary_or_assign, xor_assign
            );

        public static Parser<Expression> expression =
            assignment_expression.Repeat1(comma).Select(x => (x.Length == 1) ? x[0] : new CommaSeparatedExpression(x) );

        public static Parser<Expression> constant_expression = conditional_expression;

        //#
        //# Declarations
        //#

        public static Parser<Declaration> declaration = Combinator.Choice(
            from _1 in declaration_specifiers
            from _2 in init_declarator_list.Option()
            from _3 in semicolon
            select new Declaration(_1, _2)
        );

        public static Parser<Declaration> global_declaration = Combinator.Choice(
            declaration,
            init_declarator_list.Skip(semicolon).Select(x => new Declaration(null, x)),
            semicolon.Select(x => (Declaration)null)    // # NOTE: To accept extra semicolons in the global scope.
        );

        public static Parser<DeclarationSpecifier> declaration_specifiers = Combinator.Choice<Action<DeclarationSpecifier>>(
                storage_class_specifier.Select(x => (Action<DeclarationSpecifier>)(y => { y.storage_class_specifier = x; })),
                type_specifier.Select(x => (Action<DeclarationSpecifier>)(y => { y.type_specifiers.Add(x); })),
                type_qualifier.Select(x => (Action<DeclarationSpecifier>)(y => { y.type_qualifiers.Add(x); })),
                function_specifier.Select(x => (Action<DeclarationSpecifier>)(y => { y.function_specifier = x; }))
            ).Many(1).Select(x => x.Aggregate(new DeclarationSpecifier(), (s, y) => {
                y(s);
                return s;
            }));

        public static Parser<InitDeclarator[]> init_declarator_list = init_declarator.Repeat1(comma);

        public static Parser<InitDeclarator> init_declarator =
            from _1 in declarator
            from _2 in assign.Then(initializer).Option()
            select new InitDeclarator(_1, _2);

        public static Parser<string> storage_class_specifier = Combinator.Choice(
            typedef_keyword,
            extern_keyword,
            static_keyword,
            auto_keyword,
            register_keyword
        );

        public static Parser<TypeSpecifier> type_specifier = Combinator.Choice<TypeSpecifier>(
            Combinator.Choice(void_keyword, char_keyword, short_keyword, int_keyword, long_keyword, float_keyword, double_keyword, signed_keyword, unsigned_keyword, bool_keyword, complex_keyword, imaginary_keyword, typedef_name).Select(x => (TypeSpecifier)new StandardTypeSpecifier(x)),
            struct_or_union_specifier.Select(x => (TypeSpecifier)x),
            enum_specifier.Select(x => (TypeSpecifier)x)
        );

        public static Parser<StructOrUnionSpecifier> struct_or_union_specifier = Combinator.Choice<StructOrUnionSpecifier>(
            from _1 in struct_keyword
            from _2 in IDENTIFIER.Option()
            from _3 in left_brace.Then(struct_declaration_list).Skip(right_brace)
            select (StructOrUnionSpecifier)new StructSpecifier(_2 ?? create_anon_tag_name(_1), _3, _2 != null),
            from _1 in union_keyword
            from _2 in IDENTIFIER.Option()
            from _3 in left_brace.Then(struct_declaration_list).Skip(right_brace)
            select (StructOrUnionSpecifier)new UnionSpecifier(_2 ?? create_anon_tag_name(_1), _3, _2 != null),
            from _1 in struct_keyword
            from _2 in IDENTIFIER
            select (StructOrUnionSpecifier)new StructSpecifier(_2, null, false),
            from _1 in union_keyword
            from _2 in IDENTIFIER
            select (StructOrUnionSpecifier)new UnionSpecifier(_2, null, false)
        );

        static ulong anonCounter = 0UL;
        private static string create_anon_tag_name(string _1)
        {
            return $"_#{_1}{anonCounter++}";
        }

        public static Parser<StructDeclaration[]> struct_declaration_list = struct_declaration.Many();

        public static Parser<StructDeclaration> struct_declaration =
            from _1 in specifier_qualifier_list
            from _2 in struct_declarator_list.Option().Select(x => x ?? new StructDeclarator[0])
            from _3 in semicolon
            select new StructDeclaration(_1, _2);

        public static Parser<Tuple<TypeSpecifier, string>[]> specifier_qualifier_list = Combinator.Choice(
            type_specifier.Select(x => new Tuple<TypeSpecifier, string>(x, null)),
            type_qualifier.Select(x => new Tuple<TypeSpecifier, string>(null, x))
        ).Many(1);

        public static Parser<StructDeclarator[]> struct_declarator_list = struct_declarator.Repeat1(comma);

        public static Parser<StructDeclarator> struct_declarator = Combinator.Choice(
            from _1 in struct_declarator
            from _2 in colon.Then(constant_expression).Option()
            select new StructDeclarator(_1, _2),
            from _2 in colon.Then(constant_expression).Option()
            select new StructDeclarator(null, _2)
        );

        public static Parser<EnumSpecifier> enum_specifier =
            from _1 in enum_keyword
            from _2 in Combinator.Choice(
                from _3 in IDENTIFIER
                from _4 in Combinator.Option(
                    from _5 in left_brace
                    from _6 in enumerator_list
                    from _7 in comma.Option()
                    from _8 in right_brace
                    select new EnumSpecifier(_3, _6, _7 != null)
                )
                select _4 ?? new EnumSpecifier(_3, null, false),

                from _5 in left_brace
                from _6 in enumerator_list
                from _7 in comma.Option()
                from _8 in right_brace
                select new EnumSpecifier(create_anon_tag_name(_1), _6, _7 != null)
            )
            select _2;

        public static Parser<Enumerator[]> enumerator_list = enumerator.Repeat1(comma);

        public static Parser<Enumerator> enumerator =
            from _1 in enumerator_name
            from _2 in assign.Then(constant_expression).Option()
            select new Enumerator(_1, _2);

        public static Parser<string> enumerator_name = Combinator.Choice(IDENTIFIER, TYPEDEF_NAME);

        public static Parser<string> type_qualifier = Combinator.Choice(const_keyword, volatile_keyword, restrict_keyword);

        public static Parser<string> function_specifier = inline_keyword;

        public static T eval<T>(Func<T> pred)
        {
            return pred();

        }

        public static Parser<Declarator> declarator =
            from _1 in pointer.Option()
            from _2 in direct_declarator
            select eval(() => { _2.pointer = _1; _2.full = true; return _2; });

        public static Parser<Declarator> direct_declarator =
            from _1 in Combinator.Choice(
                            IDENTIFIER.Select(x => (Declarator)new IdentifierDeclarator(x)),
                            left_paren.Then(declarator).Skip(right_paren).Select(x => (Declarator)new GroupedDeclarator(x))
                        )
            from _2 in Combinator.Choice<Func<Declarator, Declarator>>(
                from _3 in left_bracket
                from _4 in type_qualifier_list.Then(assignment_expression)
                from _5 in right_bracket
                select (Func<Declarator, Declarator>)((x) => { _4.full = true; return new ArrayDeclarator(x, _4); }),
                from _3 in left_bracket
                from _4 in type_qualifier_list
                from _5 in right_bracket
                select (Func<Declarator, Declarator>)((x) => { return new ArrayDeclarator(x, null); }),
                from _3 in left_bracket
                from _4 in assignment_expression
                from _5 in right_bracket
                select (Func<Declarator, Declarator>)((x) => { _4.full = true; return new ArrayDeclarator(x, _4); }),
                from _3 in left_bracket
                from _4 in static_keyword.Then(type_qualifier_list).Then(assignment_expression)
                from _5 in right_bracket
                select (Func<Declarator, Declarator>)((x) => { _4.full = true; return new ArrayDeclarator(x, _4); }),
                from _3 in left_bracket
                from _4 in type_qualifier_list.Then(static_keyword).Then(assignment_expression)
                from _5 in right_bracket
                select (Func<Declarator, Declarator>)((x) => { _4.full = true; return new ArrayDeclarator(x, _4); }),
                from _3 in left_bracket
                from _4 in type_qualifier_list.Then(multiply)
                from _5 in right_bracket
                select (Func<Declarator, Declarator>)((x) => { return new ArrayDeclarator(x, null); }),
                from _3 in left_bracket
                from _4 in multiply
                from _5 in right_bracket
                select (Func<Declarator, Declarator>)((x) => { return new ArrayDeclarator(x, null); }),
                from _3 in left_bracket
                from _5 in right_bracket
                select (Func<Declarator, Declarator>)((x) => { return new ArrayDeclarator(x, null); }),

                from _3 in left_paren
                from _4 in parameter_type_list
                from _5 in right_paren
                select (Func<Declarator, Declarator>)((x) => { return new AnsiFunctionDeclarator(x, _4); }),

                from _3 in left_paren
                from _4 in identifier_list
                from _5 in right_paren
                select (Func<Declarator, Declarator>)((x) => { return new KandRFunctionDeclarator(x, _4); }),

                from _3 in left_paren
                from _5 in right_paren
                select (Func<Declarator, Declarator>)((x) => { return new AbbreviatedFunctionDeclarator(x); })
            ).Many()
            select _2.Aggregate(_1, (s, x) => x(s));

        public static Parser<string[]> pointer = Combinator.Choice(
            from _1 in multiply select new[] { _1 },
            from _1 in multiply from _2 in type_qualifier_list select _2.Concat(new[] { _1 }).ToArray(),
            from _1 in multiply from _2 in pointer select _2.Concat(new[] { _1 }).ToArray(),
            from _1 in multiply from _2 in type_qualifier_list from _3 in pointer select _2.Concat(new[] { _1 }).Concat(_3).ToArray()
        );

        public static Parser<string[]> type_qualifier_list = type_qualifier.Many(1);

        public static Parser<ParameterTypeList> parameter_type_list =
            from _1 in parameter_list
            from _2 in comma.Then(ellipsis).Option()
            select new ParameterTypeList(_1, _2 != null);

        public static Parser<ParameterDeclaration[]> parameter_list = parameter_declaration.Repeat1(comma);

        public static Parser<ParameterDeclaration> parameter_declaration =
            from _1 in declaration_specifiers
            from _2 in Combinator.Choice<Declarator>(declarator, abstract_declarator.Select(x => (Declarator)x)).Option()
            select new ParameterDeclaration(_1, _2);

        public static Parser<string[]> identifier_list = IDENTIFIER.Repeat1(comma);

        public static Parser<TypeName> type_name =
            from _1 in specifier_qualifier_list
            from _2 in abstract_declarator.Option()
            select new TypeName(_1, _2);

        public static Parser<AbstractDeclarator> abstract_declarator =
            Combinator.Choice(
                from _1 in pointer
                from _2 in direct_abstract_declarator.Option()
                select (AbstractDeclarator)new PointerAbstractDeclarator(_2, _1) { full = true },
                from _2 in direct_abstract_declarator
                select eval(() => { _2.full = true; return _2; })
            );


        public static Parser<AbstractDeclarator> direct_abstract_declarator =
            from _1 in Combinator.Choice(
                from _2 in left_paren
                from _3 in abstract_declarator
                from _4 in right_paren
                select (AbstractDeclarator)new GroupedAbstractDeclarator(_3),

                from _2 in left_bracket
                from _4 in right_bracket
                select (AbstractDeclarator)new ArrayAbstractDeclarator(null, null),

                from _2 in left_bracket
                from _3 in assignment_expression
                from _4 in right_bracket
                select eval(() => { _3.full = true; return (AbstractDeclarator)new ArrayAbstractDeclarator(null, _3); }),

                from _2 in left_bracket
                from _3 in multiply
                from _4 in right_bracket
                select eval(() => { return (AbstractDeclarator)new ArrayAbstractDeclarator(null, null); }),

                from _2 in left_paren
                from _4 in right_paren
                select (AbstractDeclarator)new FunctionAbstractDeclarator(null, null),

                from _2 in left_paren
                from _3 in parameter_type_list
                from _4 in right_paren
                select (AbstractDeclarator)new FunctionAbstractDeclarator(null, _3)
            )
            from _2 in Combinator.Choice<Func<AbstractDeclarator, AbstractDeclarator>>(
                from _2 in left_bracket
                from _4 in right_bracket
                select (Func<AbstractDeclarator, AbstractDeclarator>)((x) => { return (AbstractDeclarator)new ArrayAbstractDeclarator(x, null); }),

                from _2 in left_bracket
                from _3 in assignment_expression
                from _4 in right_bracket
                select (Func<AbstractDeclarator, AbstractDeclarator>)((x) => { _3.full = true; return (AbstractDeclarator)new ArrayAbstractDeclarator(x, _3); }),

                from _2 in left_bracket
                from _3 in multiply
                from _4 in right_bracket
                select (Func<AbstractDeclarator, AbstractDeclarator>)((x) => { return (AbstractDeclarator)new ArrayAbstractDeclarator(x, null); }),

            from _2 in left_paren
            from _4 in right_paren
            select (Func<AbstractDeclarator, AbstractDeclarator>)((x) => { return (AbstractDeclarator)new FunctionAbstractDeclarator(x, null); }),

            from _2 in left_paren
            from _3 in parameter_type_list
            from _4 in right_paren
            select (Func<AbstractDeclarator, AbstractDeclarator>)((x) => { return (AbstractDeclarator)new FunctionAbstractDeclarator(x, _3); })

            ).Many()
            select _2.Aggregate(_2, (s, x) => x(s));


        public static Parser<Initializer> initializer = Combinator.Choice(
            // 普通の定数値初期化
            assignment_expression.Select(x => { x.full = true; return new Initializer(x, null); }),
            // 複合初期化
            from _1 in left_brace
            from _2 in (
                from _3 in initializer_list
                from _4 in comma.Option()
                select _3
            ).Option()
            from _5 in right_brace
            select new Initializer(null, _2)
        );

        public static Parser<Tuple<Designator[], Initializer>[]> initializer_list = (
            from _1 in designation.Option()
            from _2 in initializer
            select Tuple.Create(_1, _2)
        ).Many(1);

        public static Parser<Designator[]> designation = designator_list.Skip(assign);

        public static Parser<Designator[]> designator_list = designator.Many(1);
        public static Parser<Designator> designator = Combinator.Choice(
            left_bracket.Then(constant_expression).Skip(right_bracket).Select(x => (Designator)new IndexDesignator(x)),
            member_access.Then(IDENTIFIER).Select(x => (Designator)new MemberDesignator(x))
        );
        
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
        public static Parser<TranslationUnit> translation_unit = external_declaration.Many().Select(x => new TranslationUnit(x));

        public static Parser<ExternalDeclaration> external_declaration = Combinator.Choice(function_definition, global_declaration);

        public static Parser<FunctionDefinition> function_definition =
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
                         : (_2 is KandRFunctionDeclarator) ? new FunctionDefinition.KandRFunctionDefinition(_1, _2, new Declaration[] { }, _3)
                         : (_2 is AbbreviatedFunctionDeclarator) ? new FunctionDefinition.AnsiFunctionDefinition(_1, _2, _3)
                         : null
                ),
                (
                    from _1 in declarator
                    from _2 in declaration_list
                    from _3 in compound_statement
                    select new FunctionDefinition.KandRFunctionDefinition(null, _1, _2, _3)
                ),
                (
                    from _1 in declarator
                    from _2 in compound_statement
                    select (_1 is AnsiFunctionDeclarator) ? new FunctionDefinition.AnsiFunctionDefinition(null, _1, _2)
                         : (_1 is KandRFunctionDeclarator) ? new FunctionDefinition.KandRFunctionDefinition(null, _1, new Declaration[] { }, _2)
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

        public static Parser<Declaration[]> declaration_list = declaration.Many(1);

        //end
        #endregion

    }

    public class MemberDesignator : Designator
    {
        public MemberDesignator(string s)
        {
            throw new NotImplementedException();
        }
    }

    public class IndexDesignator : Designator
    {
        public IndexDesignator(Expression expression)
        {
            throw new NotImplementedException();
        }
    }

    public class Designator
    {
    }

    public class AbbreviatedFunctionDeclarator : Declarator
    {
        private Declarator x;

        public AbbreviatedFunctionDeclarator(Declarator x)
        {
            this.x = x;
        }
    }

    public class KandRFunctionDeclarator : Declarator
    {
        private Declarator x;
        private string[] _4;

        public KandRFunctionDeclarator(Declarator x, string[] _4)
        {
            this.x = x;
            this._4 = _4;
        }
    }

    public class AnsiFunctionDeclarator : Declarator
    {
        private Declarator x;
        private ParameterTypeList _4;

        public AnsiFunctionDeclarator(Declarator x, ParameterTypeList _4)
        {
            this.x = x;
            this._4 = _4;
        }
    }

    public class TypeName
    {
        private Tuple<TypeSpecifier, string>[] _1;
        private AbstractDeclarator _2;

        public TypeName(Tuple<TypeSpecifier, string>[] _1, AbstractDeclarator _2)
        {
            this._1 = _1;
            this._2 = _2;
        }
    }

    public class ArrayDeclarator : Declarator
    {
        private Declarator x;
        private Expression _4;

        public ArrayDeclarator(Declarator x, Expression _4)
        {
            this.x = x;
            this._4 = _4;
        }
    }

    public class Expression
    {
    }

    public class FunctionAbstractDeclarator : AbstractDeclarator
    {
        private AbstractDeclarator p1;
        private ParameterTypeList p2;

        public FunctionAbstractDeclarator(AbstractDeclarator p1, ParameterTypeList p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }
    }

    public class ArrayAbstractDeclarator : AbstractDeclarator
    {
        private AbstractDeclarator p1;
        private object p2;

        public ArrayAbstractDeclarator(AbstractDeclarator p1, object p2)
        {
            this.p1 = p1;
            this.p2 = p2;
        }
    }

    public class GroupedAbstractDeclarator : AbstractDeclarator
    {
        private AbstractDeclarator _3;

        public GroupedAbstractDeclarator(AbstractDeclarator _3)
        {
            this._3 = _3;
        }
    }

    public class PointerAbstractDeclarator : AbstractDeclarator
    {
        private string[] _1;
        private AbstractDeclarator _2;

        public PointerAbstractDeclarator(AbstractDeclarator _2, string[] _1)
        {
            this._2 = _2;
            this._1 = _1;
        }
    }

    public class AbstractDeclarator : Declarator
    {
    }

    public class ParameterTypeList
    {
        private bool v;
        private ParameterDeclaration[] _1;

        public ParameterTypeList(ParameterDeclaration[] _1, bool v)
        {
            this._1 = _1;
            this.v = v;
        }
    }

    public class ParameterDeclaration
    {
        private DeclarationSpecifier _1;
        private Declarator _2;

        public ParameterDeclaration(DeclarationSpecifier _1, Declarator _2)
        {
            this._1 = _1;
            this._2 = _2;
        }
    }

    public class GroupedDeclarator : Declarator
    {
        private Declarator x;

        public GroupedDeclarator(Declarator x)
        {
            this.x = x;
        }
    }

    public class IdentifierDeclarator : Declarator
    {
        private string x;

        public IdentifierDeclarator(string x)
        {
            this.x = x;
        }
    }

    public class Declarator
    {
        public bool full { get; set; }
        public string[] pointer { get; set; }
    }

    public class EnumSpecifier : TypeSpecifier
    {
        private string v1;
        private bool v2;
        private Enumerator[] _6;

        public EnumSpecifier(string v1, Enumerator[] _6, bool v2)
        {
            this.v1 = v1;
            this._6 = _6;
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

    public class StructDeclarator
    {
        private StructDeclarator _1;
        private Expression _2;

        public StructDeclarator(StructDeclarator _1, Expression _2)
        {
            this._1 = _1;
            this._2 = _2;
        }
    }

    public class StructDeclaration
    {
        private Tuple<TypeSpecifier, string>[] _1;
        private StructDeclarator[] _2;

        public StructDeclaration(Tuple<TypeSpecifier, string>[] _1, StructDeclarator[] _2)
        {
            this._1 = _1;
            this._2 = _2;
        }
    }

    public class Declaration
    {
        private DeclarationSpecifier _1;
        private InitDeclarator[] _2;

        public Declaration(DeclarationSpecifier _1, InitDeclarator[] _2)
        {
            this._1 = _1;
            this._2 = _2;
        }
    }
    public class InitDeclarator
    {
    }

    public class StructOrUnionSpecifier : TypeSpecifier
    {

    }

    public class StructSpecifier : StructOrUnionSpecifier
    {
        private string v1;
        private bool v2;
        private StructDeclaration[] _3;

        public StructSpecifier(string v1, StructDeclaration[] _3, bool v2)
        {
            this.v1 = v1;
            this._3 = _3;
            this.v2 = v2;
        }
    }

    public class UnionSpecifier : StructOrUnionSpecifier
    {
        private string v1;
        private bool v2;
        private StructDeclaration[] _3;

        public UnionSpecifier(string v1, StructDeclaration[] _3, bool v2)
        {
            this.v1 = v1;
            this._3 = _3;
            this.v2 = v2;
        }
    }

    public class StandardTypeSpecifier : TypeSpecifier
    {
        public StandardTypeSpecifier(string s)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class TypeSpecifier
    {
    }

    public class DeclarationSpecifier
    {
        public string storage_class_specifier { get; internal set; }
        internal List<TypeSpecifier> type_specifiers { get; } = new List<TypeSpecifier>();
        internal List<string> type_qualifiers { get; } = new List<string>();
        public string function_specifier { get; internal set; }

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

    public class TranslationUnit
    {
        private ExternalDeclaration[] x;

        public TranslationUnit(ExternalDeclaration[] x)
        {
            this.x = x;
        }
    }

}
