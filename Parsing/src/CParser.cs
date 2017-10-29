using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Parsing;

namespace CParser2 {
    public static class CParser
    {
        public class ParserStatus {
            public LinkedList<Tuple<string, SyntaxNode>> typedefed_list { get; }

            public ParserStatus()
            {
                this.typedefed_list = LinkedList<Tuple<string, SyntaxNode>>.Empty;
            }
            public ParserStatus(LinkedList<Tuple<string, SyntaxNode>> typedefed_list)
            {
                this.typedefed_list = typedefed_list;
            }
        }

        #region Tokenize rules

        public static readonly Parser<string> new_line =
            Combinator.Choice(
                Combinator.Token("\r\n"),
                Combinator.Token("\r"),
                Combinator.Token("\n")
            );

        public static readonly Parser<string> whitespace =
            Combinator.AnyChar(" \t\v\r\n\f").String();

        public static readonly Parser<string> block_comment = 
            from _1 in Combinator.Token("/*")
            from _2 in Combinator.Token("*/").Not().Then(Combinator.AnyChar()).Many().String()
            from _3 in Combinator.Token("*/")
            select _1 + _2 + _3;

        public static readonly Parser<string> line_comment = from _1 in Combinator.Token("//")
            from _2 in new_line.Not().Then(Combinator.AnyChar()).Many().String()
            from _3 in new_line
            select _1 + _2 + _3;

        public static readonly Parser<string> comment = Combinator.Choice(
            block_comment, 
            line_comment
        );

        public static readonly Parser<string> space = Combinator.Choice(
            new_line, whitespace, comment
        );

        public static readonly Parser<string> spaces = space.Many(1).Select(String.Concat);
        public static readonly Parser<string> isspace = space.Option();
        public static readonly Parser<string> isspaces = space.Many().Select(String.Concat);

        public static readonly Parser<char> digit = Combinator.AnyChar("0123456789");
        public static readonly Parser<string> digits = digit.Many(1).String();
        public static readonly Parser<string> isdigits = digit.Many().String();

        public static readonly Parser<char> alpha = Combinator.AnyChar(x => ('a' <= x && x <= 'z') || ('A' <= x && x <= 'Z'));
        public static readonly Parser<char> xdigit = Combinator.AnyChar("0123456789ABCDEFabcdef");

        public static readonly Parser<string> exponent = 
            from _1 in Combinator.AnyChar("eE").String()
            from _2 in Combinator.AnyChar("+-").Option().String()
            from _3 in digit.Many(1).String()
            select _1 + _2 + _3;

        public static readonly Parser<char> float_size = Combinator.AnyChar("fFlL");
        public static readonly Parser<string> int_size = Combinator.AnyChar("uUlL").Many(1).String();

        public static readonly Parser<string> isexponent = exponent.Option();
        public static readonly Parser<char> isfloat_size = float_size.Option();
        public static readonly Parser<string> isint_size = int_size.Option();

        public static readonly Parser<string> identifier =
            from _0 in isspaces
            from _1 in alpha
            from _2 in Combinator.Choice(alpha, digit).Many().String()
            from _3 in isspaces
            select _1 + _2;

        public static readonly Parser<string> hex_constant =
            from _1 in Combinator.Choice(Combinator.Token("0x"), Combinator.Token("0X"))
            from _2 in xdigit.Many(1).String()
            from _3 in isint_size
            from _4 in isspaces
            select _1 + _2 + _3;

        public static readonly Parser<string> octal_constant =
            from _1 in Combinator.Token("0")
            from _2 in digits
            from _3 in isint_size
            from _4 in isspaces
            select _1 + _2 + _3;

        public static readonly Parser<string> decimal_constant =
            from _1 in digits
            from _2 in isint_size
            from _3 in isspaces
            select _1 + _2;

        public static readonly Parser<string> string_constant =
            from _0 in isspaces
            from _1 in Combinator.Token("L").Option()
            from _2 in Combinator.Token("'")
            from _3 in Combinator.Choice(
                Combinator.Token("\\").Then(Combinator.AnyChar()).Select(x => $@"\{x}"),
                Combinator.AnyChar(@"'").Not().Then(Combinator.AnyChar()).String()
            ).Many(1)
            from _4 in Combinator.Token("'")
            from _5 in isspaces
            select _1 + _2 + _3 + 4;

        public static readonly Parser<string> float_constant =
            Combinator.Choice(
                from _1 in digits
                from _2 in exponent
                from _3 in isfloat_size.String()
                select _1 + _2 + _3
                ,
                from _1 in isdigits
                from _2 in Combinator.Token(".")
                from _3 in digits
                from _4 in isexponent
                from _5 in isfloat_size.String()
                select _1 + _2 + _3 + _4 + _5
                ,
                from _1 in digits
                from _2 in Combinator.Token(".")
                from _3 in isdigits
                from _4 in isexponent
                from _5 in isfloat_size.String()
                select _1 + _2 + _3 + _4 + _5
            );

        public static readonly Parser<string> constant =
            isspaces.Then(
                Combinator.Choice(
                    hex_constant,
                    octal_constant,
                    decimal_constant,
                    float_constant,
                    string_constant
                )
            ).Skip(isspaces);

        public static readonly Parser<string> auto_keyword = isspaces.Then(Combinator.Token("auto").Skip(isspaces));
        public static readonly Parser<string> break_keyword = isspaces.Then(Combinator.Token("break").Skip(isspaces));
        public static readonly Parser<string> case_keyword = isspaces.Then(Combinator.Token("case").Skip(isspaces));
        public static readonly Parser<string> char_keyword = isspaces.Then(Combinator.Token("char").Skip(isspaces));
        public static readonly Parser<string> const_keyword = isspaces.Then(Combinator.Token("const").Skip(isspaces));
        public static readonly Parser<string> continue_keyword = isspaces.Then(Combinator.Token("continue").Skip(isspaces));
        public static readonly Parser<string> default_keyword = isspaces.Then(Combinator.Token("default").Skip(isspaces));
        public static readonly Parser<string> do_keyword = isspaces.Then(Combinator.Token("do").Skip(isspaces));
        public static readonly Parser<string> double_keyword = isspaces.Then(Combinator.Token("double").Skip(isspaces));
        public static readonly Parser<string> else_keyword = isspaces.Then(Combinator.Token("else").Skip(isspaces));
        public static readonly Parser<string> enum_keyword = isspaces.Then(Combinator.Token("enum").Skip(isspaces));
        public static readonly Parser<string> extern_keyword = isspaces.Then(Combinator.Token("extern").Skip(isspaces));
        public static readonly Parser<string> float_keyword = isspaces.Then(Combinator.Token("float").Skip(isspaces));
        public static readonly Parser<string> for_keyword = isspaces.Then(Combinator.Token("for").Skip(isspaces));
        public static readonly Parser<string> goto_keyword = isspaces.Then(Combinator.Token("goto").Skip(isspaces));
        public static readonly Parser<string> if_keyword = isspaces.Then(Combinator.Token("if").Skip(isspaces));
        public static readonly Parser<string> int_keyword = isspaces.Then(Combinator.Token("int").Skip(isspaces));
        public static readonly Parser<string> long_keyword = isspaces.Then(Combinator.Token("long").Skip(isspaces));
        public static readonly Parser<string> register_keyword = isspaces.Then(Combinator.Token("register").Skip(isspaces));
        public static readonly Parser<string> return_keyword = isspaces.Then(Combinator.Token("return").Skip(isspaces));
        public static readonly Parser<string> short_keyword = isspaces.Then(Combinator.Token("short").Skip(isspaces));
        public static readonly Parser<string> signed_keyword = isspaces.Then(Combinator.Token("signed").Skip(isspaces));
        public static readonly Parser<string> sizeof_keyword = isspaces.Then(Combinator.Token("sizeof").Skip(isspaces));
        public static readonly Parser<string> static_keyword = isspaces.Then(Combinator.Token("static").Skip(isspaces));
        public static readonly Parser<string> struct_keyword = isspaces.Then(Combinator.Token("struct").Skip(isspaces));
        public static readonly Parser<string> switch_keyword = isspaces.Then(Combinator.Token("switch").Skip(isspaces));
        public static readonly Parser<string> typedef_keyword = isspaces.Then(Combinator.Token("typedef").Skip(isspaces));
        public static readonly Parser<string> union_keyword = isspaces.Then(Combinator.Token("union").Skip(isspaces));
        public static readonly Parser<string> unsigned_keyword = isspaces.Then(Combinator.Token("unsigned").Skip(isspaces));
        public static readonly Parser<string> void_keyword = isspaces.Then(Combinator.Token("void").Skip(isspaces));
        public static readonly Parser<string> volatile_keyword = isspaces.Then(Combinator.Token("volatile").Skip(isspaces));
        public static readonly Parser<string> while_keyword = isspaces.Then(Combinator.Token("while").Skip(isspaces));

        public static readonly Parser<string> bool_keyword = isspaces.Then(Combinator.Token("_Bool").Skip(isspaces));
        public static readonly Parser<string> complex_keyword = isspaces.Then(Combinator.Token("_Complex").Skip(isspaces));
        public static readonly Parser<string> imaginary_keyword = isspaces.Then(Combinator.Token("_Imaginary").Skip(isspaces));
        public static readonly Parser<string> restrict_keyword = isspaces.Then(Combinator.Token("restrict").Skip(isspaces));
        public static readonly Parser<string> inline_keyword = isspaces.Then(Combinator.Token("inline").Skip(isspaces));

    public static readonly Parser<string> typedef_name = Combinator.Choice(
            auto_keyword,
            break_keyword,
            case_keyword,
            char_keyword,
            const_keyword,
            continue_keyword,
            default_keyword,
            do_keyword,
            double_keyword,
            else_keyword,
            enum_keyword,
            extern_keyword,
            float_keyword,
            for_keyword,
            goto_keyword,
            if_keyword,
            int_keyword,
            long_keyword,
            register_keyword,
            return_keyword,
            short_keyword,
            signed_keyword,
            sizeof_keyword,
            static_keyword,
            struct_keyword,
            switch_keyword,
            typedef_keyword,
            union_keyword,
            unsigned_keyword,
            void_keyword,
            volatile_keyword,
            while_keyword,
            bool_keyword,
            complex_keyword,
            imaginary_keyword,
            restrict_keyword,
            inline_keyword
        ).Not().Then(identifier).Where((x,s) => LinkedList.First(y => (y.Item1 == x), ((ParserStatus)s).typedefed_list) != null);


        public static readonly Parser<string> ellipsis = isspaces.Then(Combinator.Token("...").Skip(isspaces));
        public static readonly Parser<string> semicolon = isspaces.Then(Combinator.Token(";").Skip(isspaces));
        public static readonly Parser<string> comma = isspaces.Then(Combinator.Token(",").Skip(isspaces));
        public static readonly Parser<string> colon = isspaces.Then(Combinator.Token(":").Skip(isspaces));
        public static readonly Parser<string> left_paren = isspaces.Then(Combinator.Token("(").Skip(isspaces));
        public static readonly Parser<string> right_paren = isspaces.Then(Combinator.Token(")").Skip(isspaces));
        public static readonly Parser<string> member_access = isspaces.Then(Combinator.Token(".").Skip(isspaces));
        public static readonly Parser<string> question_mark = isspaces.Then(Combinator.Token("?").Skip(isspaces));

        public static readonly Parser<string> string_literal =
            from _0 in isspace
            from _1 in Combinator.Token("L").Option()
            from _2 in Combinator.Token("\"")
            from _3 in Combinator.Choice(
                Combinator.Token("\\").Then(Combinator.AnyChar()).Select(x => $@"\{x}"),
                Combinator.AnyChar("\"").Not().Then(Combinator.AnyChar()).String()
            ).Many(1)
            from _4 in Combinator.Token("\"")
            from _5 in isspaces
            select _1 + _2 + _3 + 4;

        public static readonly Parser<string> left_brace =
            isspaces.Then(Combinator.Choice(Combinator.Token("{"), Combinator.Token("<%")).Skip(isspaces));

        public static readonly Parser<string> right_brace =
            isspaces.Then(Combinator.Choice(Combinator.Token("}"), Combinator.Token("%>")).Skip(isspaces));

        public static readonly Parser<string> left_bracket =
            isspaces.Then(Combinator.Choice(Combinator.Token("["), Combinator.Token("<:")).Skip(isspaces));

        public static readonly Parser<string> right_bracket =
            isspaces.Then(Combinator.Choice(Combinator.Token("]"), Combinator.Token(":>")).Skip(isspaces));


        public static readonly Parser<string> right_shift_assign = isspaces.Then(Combinator.Token(">>=").Skip(isspaces));
        public static readonly Parser<string> left_shift_assign = isspaces.Then(Combinator.Token("<<=").Skip(isspaces));
        public static readonly Parser<string> add_assign = isspaces.Then(Combinator.Token("+=").Skip(isspaces));
        public static readonly Parser<string> subtract_assign = isspaces.Then(Combinator.Token("-=").Skip(isspaces));
        public static readonly Parser<string> multiply_assign = isspaces.Then(Combinator.Token("*=").Skip(isspaces));
        public static readonly Parser<string> divide_assign = isspaces.Then(Combinator.Token("/=").Skip(isspaces));
        public static readonly Parser<string> modulus_assign = isspaces.Then(Combinator.Token("%=").Skip(isspaces));
        public static readonly Parser<string> binary_and_assign = isspaces.Then(Combinator.Token("&=").Skip(isspaces));
        public static readonly Parser<string> xor_assign = isspaces.Then(Combinator.Token("^=").Skip(isspaces));
        public static readonly Parser<string> binary_or_assign = isspaces.Then(Combinator.Token("|=").Skip(isspaces));
        public static readonly Parser<string> inc = isspaces.Then(Combinator.Token("++").Skip(isspaces));
        public static readonly Parser<string> dec = isspaces.Then(Combinator.Token("--").Skip(isspaces));
        public static readonly Parser<string> pointer_access = isspaces.Then(Combinator.Token("->").Skip(isspaces));
        public static readonly Parser<string> logical_and = isspaces.Then(Combinator.Token("&&").Skip(isspaces));
        public static readonly Parser<string> logical_or = isspaces.Then(Combinator.Token("||").Skip(isspaces));
        public static readonly Parser<string> less_equal = isspaces.Then(Combinator.Token("<=").Skip(isspaces));
        public static readonly Parser<string> greater_equal = isspaces.Then(Combinator.Token(">=").Skip(isspaces));
        public static readonly Parser<string> equal = isspaces.Then(Combinator.Token("==").Skip(isspaces));
        public static readonly Parser<string> not_equal = isspaces.Then(Combinator.Token("!=").Skip(isspaces));

        public static readonly Parser<string> assign =
            isspaces.Then(Combinator.Token("=").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces));

        public static readonly Parser<string> add =
            isspaces.Then(Combinator.Token("+").Skip(Combinator.AnyChar("+=").Not()).Skip(isspaces));

        public static readonly Parser<string> subtract =
            isspaces.Then(Combinator.Token("-").Skip(Combinator.AnyChar("-=").Not()).Skip(isspaces));

        public static readonly Parser<string> multiply =
            isspaces.Then(Combinator.Token("*").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces));

        public static readonly Parser<string> divide =
            isspaces.Then(Combinator.Token("/").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces));

        public static readonly Parser<string> modulus =
            isspaces.Then(Combinator.Token("%").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces));

        public static readonly Parser<string> less =
            isspaces.Then(Combinator.Token("<").Skip(Combinator.AnyChar("<=").Not()).Skip(isspaces));

        public static readonly Parser<string> greater =
            isspaces.Then(Combinator.Token(">").Skip(Combinator.AnyChar(">=").Not()).Skip(isspaces));

        public static readonly Parser<string> negate =
            isspaces.Then(Combinator.Token("!").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces));

        public static readonly Parser<string> binary_or =
            isspaces.Then(Combinator.Token("|").Skip(Combinator.AnyChar("|=").Not()).Skip(isspaces));

        public static readonly Parser<string> binary_and =
            isspaces.Then(Combinator.Token("&").Skip(Combinator.AnyChar("&=").Not()).Skip(isspaces));

        public static readonly Parser<string> xor =
            isspaces.Then(Combinator.Token("^").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces));

        public static readonly Parser<string> left_shift =
            isspaces.Then(Combinator.Token("<<").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces));

        public static readonly Parser<string> right_shift =
            isspaces.Then(Combinator.Token(">>").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces));

        public static readonly Parser<string> inverse =
            isspaces.Then(Combinator.Token("~").Skip(Combinator.AnyChar("=").Not()).Skip(isspaces));

        #endregion

        #region Syntax rules

        public static readonly Parser<string> IDENTIFIER = identifier;
        public static readonly Parser<string> TYPEDEF_NAME = typedef_name;
        public static readonly Parser<string> CONSTANT = constant;

        //#
        //# Expressions
        //#

        public static readonly Parser<SyntaxNode.Expression> primary_expression = Combinator.Lazy(() => Combinator.Choice(
            IDENTIFIER.Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PrimaryExpression.ObjectSpecifier(x)),
            CONSTANT.Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PrimaryExpression.ConstantSpecifier(x)),
            string_literal.Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PrimaryExpression.StringLiteralSpecifier(x)),
            //NULL.Select(x => (SyntaxNode.Expression)new NullConstantSpecifier(x)),
            left_paren.Then(expression).Skip(right_paren).Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PrimaryExpression.GroupedExpression(x))
            //left_paren.Then(compound_statement).Skip(right_paren).Select(x => (SyntaxNode.Expression)new ErrorExpression(x))
        ));

        public static readonly Parser<SyntaxNode.Expression> postfix_expression = Combinator.Lazy(() =>
            from _1 in Combinator.Choice(
                from _2 in left_paren
                from _3 in type_name
                from _4 in right_paren
                from _5 in left_brace
                from _6 in initializer_list
                from _7 in comma.Option()
                from _8 in right_brace
                select (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.CompoundLiteralExpression(_3, _7),
                primary_expression
            )
            from _9 in Combinator.Choice(
                from _10 in left_bracket
                from _11 in expression
                from _12 in right_bracket
                select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.ArraySubscriptExpression(x, _11)),
                from _10 in left_paren
                from _11 in argument_expression_list.Option()
                from _12 in right_paren
                select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x =>
                    (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.FunctionCallExpression(x, _11 ?? new SyntaxNode.Expression[0])),
                from _10 in member_access
                from _11 in IDENTIFIER
                select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.MemberAccessByValueExpression(x, _11)),
                from _10 in pointer_access
                from _11 in IDENTIFIER
                select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.MemberAccessByPointerExpression(x, _11)),
                //from _10 in member_access
                //from _11 in CONSTANT
                //select (Func<SyntaxNode.Expression, SyntaxNode.Expression>) (x => (SyntaxNode.Expression) new BitAccessByValueExpression(x, _11)),
                //from _10 in pointer_access
                //from _11 in CONSTANT
                //select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x => (SyntaxNode.Expression)new BitAccessByPointerExpression(x, _11)),
                from _10 in inc
                select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.PostfixIncrementExpression(x)),
                from _10 in dec
                select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.PostfixDecrementExpression(x))
            ).Many()
            select _9.Aggregate(_1, (s, x) => x(s))
        );

        public static readonly Parser<SyntaxNode.Expression[]> argument_expression_list =
            Combinator.Lazy(() => assignment_expression.Repeat1(comma));

        public static readonly Parser<SyntaxNode.Expression> unary_expression = Combinator.Lazy(() => Combinator.Choice(
            inc.Then(unary_expression).Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.PrefixIncrementExpression("++", x)),
            dec.Then(unary_expression).Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.PrefixDecrementExpression("--", x)),
            binary_and.Then(cast_expression).Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.AddressExpression("&", x)),
            multiply.Then(cast_expression).Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.IndirectionExpression("*", x)),
            from _1 in unary_arithmetic_operator
            from _2 in cast_expression
            select (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression(_1, _2),
            sizeof_keyword.Then(unary_expression).Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.SizeofExpression("sizeof", x)),
            from _1 in sizeof_keyword
            from _2 in left_paren
            from _3 in type_name
            from _4 in right_paren
            select (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.SizeofTypeExpression(_1, _3),
            //alighof_keyword.Then(unary_expression).Select(x => (SyntaxNode.Expression)new AlignofExpression("alignof", x)),
            //from _1 in alighof_keyword from _2 in left_paren from _3 in type_name from _4 in right_paren select (SyntaxNode.Expression)new AlignofTypeExpression(_1, _3)
            postfix_expression
        ));

        public static readonly Parser<string> unary_arithmetic_operator = Combinator.Lazy(() =>
            Combinator.Choice(add, subtract, inverse, negate)
        );

        public static readonly Parser<SyntaxNode.Expression> cast_expression = Combinator.Lazy(() =>
            from _1 in left_paren.Then(type_name).Skip(right_paren).Many()
            from _2 in unary_expression
            select _1.Reverse().Aggregate(_2, (s, x) => new SyntaxNode.Expression.CastExpression(x, s))
        );

        public static readonly Parser<SyntaxNode.Expression> multiplicative_expression = Combinator.Lazy(() =>
            from _1 in cast_expression
            from _2 in (
                from _3 in Combinator.Choice(multiply, divide, modulus)
                from _4 in cast_expression
                select Tuple.Create(_3, _4)
            ).Many()
            select _2.Aggregate(_1, (s, x) => new SyntaxNode.Expression.BinaryExpression.MultiplicativeExpression(x.Item1, s, x.Item2))
        );

        public static readonly Parser<SyntaxNode.Expression> additive_expression = Combinator.Lazy(() =>
            from _1 in multiplicative_expression
            from _2 in (
                from _3 in Combinator.Choice(add, subtract)
                from _4 in multiplicative_expression
                select Tuple.Create(_3, _4)
            ).Many()
            select _2.Aggregate(_1, (s, x) => new SyntaxNode.Expression.BinaryExpression.AdditiveExpression(x.Item1, s, x.Item2))
        );

        public static readonly Parser<SyntaxNode.Expression> shift_expression = Combinator.Lazy(() =>
            from _1 in additive_expression
            from _2 in (
                from _3 in Combinator.Choice(left_shift, right_shift)
                from _4 in additive_expression
                select Tuple.Create(_3, _4)
            ).Many()
            select _2.Aggregate(_1, (s, x) => new SyntaxNode.Expression.BinaryExpression.ShiftExpression(x.Item1, s, x.Item2))
        );

        public static readonly Parser<SyntaxNode.Expression> relational_expression = Combinator.Lazy(() =>
            from _1 in shift_expression
            from _2 in (
                from _3 in Combinator.Choice(less_equal, less, greater_equal, greater)
                from _4 in shift_expression
                select Tuple.Create(_3, _4)
            ).Many()
            select _2.Aggregate(_1, (s, x) => new SyntaxNode.Expression.BinaryExpression.RelationalExpression(x.Item1, s, x.Item2))
        );

        public static readonly Parser<SyntaxNode.Expression> equality_expression = Combinator.Lazy(() =>
            from _1 in relational_expression
            from _2 in (
                from _3 in Combinator.Choice(equal, not_equal)
                from _4 in relational_expression
                select Tuple.Create(_3, _4)
            ).Many()
            select _2.Aggregate(_1, (s, x) => new SyntaxNode.Expression.BinaryExpression.EqualityExpression(x.Item1, s, x.Item2))
        );

        public static readonly Parser<SyntaxNode.Expression> and_expression = Combinator.Lazy(() =>
            equality_expression.Repeat1(binary_and).Select(x => x.Aggregate((s, y) => new SyntaxNode.Expression.BinaryExpression.AndExpression("&", s, y)))
        );

        public static readonly Parser<SyntaxNode.Expression> exclusive_or_expression = Combinator.Lazy(() =>
            and_expression.Repeat1(xor).Select(x => x.Aggregate((s, y) => new SyntaxNode.Expression.BinaryExpression.ExclusiveOrExpression("^", s, y)))
        );

        public static readonly Parser<SyntaxNode.Expression> inclusive_or_expression = Combinator.Lazy(() =>
            exclusive_or_expression.Repeat1(binary_or)
                .Select(x => x.Aggregate((s, y) => new SyntaxNode.Expression.BinaryExpression.InclusiveOrExpression("|", s, y)))
        );

        public static readonly Parser<SyntaxNode.Expression> logical_and_expression = Combinator.Lazy(() =>
            inclusive_or_expression.Repeat1(logical_and)
                .Select(x => x.Aggregate((s, y) => new SyntaxNode.Expression.BinaryExpression.LogicalAndExpression("&&", s, y)))
        );

        public static readonly Parser<SyntaxNode.Expression> logical_or_expression = Combinator.Lazy(() =>
            logical_and_expression.Repeat1(logical_or)
                .Select(x => x.Aggregate((s, y) => new SyntaxNode.Expression.BinaryExpression.LogicalOrExpression("||", s, y)))
        );

        public static readonly Parser<SyntaxNode.Expression> conditional_expression = Combinator.Lazy(() =>
            Combinator.Choice(
                from _1 in logical_or_expression
                from _2 in (
                    from _3 in question_mark
                    from _4 in expression
                    from _5 in colon
                    from _6 in conditional_expression
                    select (SyntaxNode.Expression)new SyntaxNode.Expression.BinaryExpression.ConditionalExpression(_1, _4, _6, _3)
                ).Option()
                select _2 ?? _1
            )
        );

        public static readonly Parser<SyntaxNode.Expression> assignment_expression = Combinator.Lazy(() =>
            Combinator.Choice(
                from _1 in cast_expression
                from _2 in assign
                from _3 in assignment_expression
                select (SyntaxNode.Expression)new SyntaxNode.Expression.BinaryExpression.SimpleAssignmentExpression(_2, _1, _3),
                from _1 in cast_expression
                from _2 in compound_assignment_operator
                from _3 in assignment_expression
                select (SyntaxNode.Expression)new SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression(_2, _1, _3),
                conditional_expression

            )
        );

        public static readonly Parser<string> compound_assignment_operator = Combinator.Lazy(() =>
            Combinator.Choice(
                multiply_assign, divide_assign, modulus_assign, add_assign, subtract_assign,
                left_shift_assign, right_shift_assign, binary_and_assign, binary_or_assign, xor_assign
            )
        );

        public static readonly Parser<SyntaxNode.Expression> expression = Combinator.Lazy(() =>
            assignment_expression.Repeat1(comma)
                .Select(x => (x.Length == 1) ? x[0] : (SyntaxNode.Expression)new SyntaxNode.Expression.CommaSeparatedExpression(x))
        );

        public static readonly Parser<SyntaxNode.Expression> constant_expression = Combinator.Lazy(() =>
            conditional_expression
        );

        //#
        //# Declarations
        //#

        public static readonly Parser<SyntaxNode.Declaration> declaration = Combinator.Lazy(() =>
            from _1 in declaration_specifiers
            from _2 in init_declarator_list.Option().Select(x => x ?? new SyntaxNode.InitDeclarator[0])
            from _3 in semicolon
            select new SyntaxNode.Declaration(_1, _2)
        ).Action(success: x => {
            var ps = (ParserStatus)x.Status;
            var list = ps.typedefed_list;
            foreach (var item in x.Value.items)
            {
                if (item is SyntaxNode.TypeDeclaration.TypedefDeclaration)
                {
                    list = LinkedList.Extend(Tuple.Create(((SyntaxNode.TypeDeclaration.TypedefDeclaration)item).identifier, item), ps.typedefed_list);
                }
            }
            return new ParserStatus(list);
        });

        public static readonly Parser<SyntaxNode.Declaration> global_declaration = Combinator.Lazy(() => Combinator.Choice(
                declaration,
                init_declarator_list.Skip(semicolon).Select(x => new SyntaxNode.Declaration(null, x)),
                semicolon.Select(x => (SyntaxNode.Declaration)null) // # NOTE: To accept extra semicolons in the global scope.
            )
        );

        public static readonly Parser<SyntaxNode.DeclarationSpecifiers> declaration_specifiers = Combinator.Lazy(() => Combinator
            .Choice(
                storage_class_specifier.Select(x =>
                    (Action<SyntaxNode.DeclarationSpecifiers>)(y => { y.storage_class_specifier = x; })),
                type_specifier.Select(x => (Action<SyntaxNode.DeclarationSpecifiers>)(y => { y.type_specifiers.Add(x); })),
                type_qualifier.Select(x => (Action<SyntaxNode.DeclarationSpecifiers>)(y => { y.type_qualifiers.Add(x); })),
                function_specifier.Select(x => (Action<SyntaxNode.DeclarationSpecifiers>)(y => { y.function_specifier = x; }))
            ).Many(1).Select(x => x.Aggregate(new SyntaxNode.DeclarationSpecifiers(), (s, y) =>
            {
                y(s);
                return s;
            }))
        );

        public static readonly Parser<SyntaxNode.InitDeclarator[]> init_declarator_list = Combinator.Lazy(
            () => init_declarator.Repeat1(comma)
        );

        public static readonly Parser<SyntaxNode.InitDeclarator> init_declarator = Combinator.Lazy(() =>
            from _1 in declarator
            from _2 in assign.Then(initializer).Option()
            select new SyntaxNode.InitDeclarator(_1, _2)
        );

        public static readonly Parser<string> storage_class_specifier = Combinator.Lazy(() => Combinator.Choice(
                typedef_keyword,
                extern_keyword,
                static_keyword,
                auto_keyword,
                register_keyword
            )
        );

        public static readonly Parser<SyntaxNode.TypeSpecifier> type_specifier = Combinator.Lazy(() => Combinator.Choice(
                Combinator.Choice(void_keyword, char_keyword, short_keyword, int_keyword, long_keyword, float_keyword,
                    double_keyword, signed_keyword, unsigned_keyword, bool_keyword, complex_keyword, imaginary_keyword,
                    typedef_name).Select(x => (SyntaxNode.TypeSpecifier)new SyntaxNode.TypeSpecifier.StandardTypeSpecifier(x)),
                struct_or_union_specifier.Select(x => x),
                enum_specifier.Select(x => (SyntaxNode.TypeSpecifier)x)
            )
        );

        public static readonly Parser<SyntaxNode.TypeSpecifier> struct_or_union_specifier = Combinator.Lazy(() =>
            Combinator.Choice(
                from _1 in struct_keyword
                from _2 in IDENTIFIER.Option()
                from _3 in left_brace
                from _4 in struct_declaration_list
                from _5 in right_brace
                select (SyntaxNode.TypeSpecifier)new SyntaxNode.TypeSpecifier.StructSpecifier(_2 ?? create_anon_tag_name(_1), _4, _2 != null),
                from _1 in union_keyword
                from _2 in IDENTIFIER.Option()
                from _3 in left_brace.Then(struct_declaration_list).Skip(right_brace)
                select (SyntaxNode.TypeSpecifier)new SyntaxNode.TypeSpecifier.UnionSpecifier(_2 ?? create_anon_tag_name(_1), _3, _2 != null),
                from _1 in struct_keyword
                from _2 in IDENTIFIER
                select (SyntaxNode.TypeSpecifier)new SyntaxNode.TypeSpecifier.StructSpecifier(_2, null, false),
                from _1 in union_keyword
                from _2 in IDENTIFIER
                select (SyntaxNode.TypeSpecifier)new SyntaxNode.TypeSpecifier.UnionSpecifier(_2, null, false)
            )
        );

        static ulong anonCounter;

        private static string create_anon_tag_name(string _1)
        {
            return $"_#{_1}{anonCounter++}";
        }

        public static readonly Parser<SyntaxNode.StructDeclaration[]> struct_declaration_list =
            Combinator.Lazy(() => struct_declaration.Many());

        public static readonly Parser<SyntaxNode.StructDeclaration> struct_declaration = Combinator.Lazy(() =>
            from _1 in specifier_qualifier_list
            from _2 in struct_declarator_list.Option().Select(x => x ?? new SyntaxNode.StructDeclarator[0])
            from _3 in semicolon
            select new SyntaxNode.StructDeclaration(_1, _2)
        );

        public static readonly Parser<SyntaxNode.SpecifierQualifierList> specifier_qualifier_list = Combinator.Lazy(() => Combinator.Choice(
                type_specifier.Select(x => (Action<SyntaxNode.SpecifierQualifierList>)(y => { y.type_specifiers.Add(x); })),
                type_qualifier.Select(x => (Action<SyntaxNode.SpecifierQualifierList>)(y => { y.type_qualifiers.Add(x); }))
            ).Many(1).Select(x => x.Aggregate(new SyntaxNode.SpecifierQualifierList(), (s, y) =>
            {
                y(s);
                return s;
            }))
        );

        public static readonly Parser<SyntaxNode.StructDeclarator[]> struct_declarator_list =
            Combinator.Lazy(() => struct_declarator.Repeat1(comma));

        public static readonly Parser<SyntaxNode.StructDeclarator> struct_declarator = Combinator.Lazy(() => Combinator.Choice(
                from _1 in declarator
                from _2 in colon.Then(constant_expression).Option()
                select new SyntaxNode.StructDeclarator(_1, _2),
                from _2 in colon.Then(constant_expression)
                select new SyntaxNode.StructDeclarator(null, _2)
            )
        );

        public static readonly Parser<SyntaxNode.EnumSpecifier> enum_specifier = Combinator.Lazy(() =>
            from _1 in enum_keyword
            from _2 in Combinator.Choice(
                from _3 in IDENTIFIER
                from _4 in (from _5 in left_brace
                    from _6 in enumerator_list
                    from _7 in comma.Option()
                    from _8 in right_brace
                    select new SyntaxNode.EnumSpecifier(_3, _6, _7 != null)).Option()
                select _4 ?? new SyntaxNode.EnumSpecifier(_3, null, false),

                from _5 in left_brace
                from _6 in enumerator_list
                from _7 in comma.Option()
                from _8 in right_brace
                select new SyntaxNode.EnumSpecifier(create_anon_tag_name(_1), _6, _7 != null)
            )
            select _2
        );

        public static readonly Parser<SyntaxNode.Enumerator[]> enumerator_list = Combinator.Lazy(() => enumerator.Repeat1(comma));

        public static readonly Parser<SyntaxNode.Enumerator> enumerator = Combinator.Lazy(() =>
            from _1 in enumerator_name
            from _2 in assign.Then(constant_expression).Option()
            select new SyntaxNode.Enumerator(_1, _2)
        );

        public static readonly Parser<string> enumerator_name =
            Combinator.Lazy(() => Combinator.Choice(IDENTIFIER, TYPEDEF_NAME));

        public static readonly Parser<string> type_qualifier =
            Combinator.Lazy(() => Combinator.Choice(const_keyword, volatile_keyword, restrict_keyword));

        public static readonly Parser<string> function_specifier = Combinator.Lazy(() => inline_keyword);

        public static T eval<T>(Func<T> pred)
        {
            return pred();
        }

        public static readonly Parser<SyntaxNode.Declarator> declarator = Combinator.Lazy(() =>
            from _1 in pointer.Option()
            from _2 in direct_declarator
            select eval(() =>
            {
                _2.pointer = _1;
                _2.full = true;
                return _2;
            })
        );

        public static readonly Parser<SyntaxNode.Declarator> direct_declarator = Combinator.Lazy(() =>
            from _1 in Combinator.Choice(
                IDENTIFIER.Select(x => (SyntaxNode.Declarator)new SyntaxNode.Declarator.IdentifierDeclarator(x)),
                left_paren.Then(declarator).Skip(right_paren).Select(x => (SyntaxNode.Declarator)new SyntaxNode.Declarator.GroupedDeclarator(x))
            )
            from _2 in Combinator.Choice(
                from _3 in left_bracket
                from _4 in type_qualifier_list.Then(assignment_expression)
                from _5 in right_bracket
                select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x =>
                {
                    _4.full = true;
                    return new SyntaxNode.Declarator.ArrayDeclarator(x, _4);
                }),
                from _3 in left_bracket
                from _4 in type_qualifier_list
                from _5 in right_bracket
                select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => { return new SyntaxNode.Declarator.ArrayDeclarator(x, null); }),
                from _3 in left_bracket
                from _4 in assignment_expression
                from _5 in right_bracket
                select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x =>
                {
                    _4.full = true;
                    return new SyntaxNode.Declarator.ArrayDeclarator(x, _4);
                }),
                from _3 in left_bracket
                from _4 in static_keyword.Then(type_qualifier_list).Then(assignment_expression)
                from _5 in right_bracket
                select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x =>
                {
                    _4.full = true;
                    return new SyntaxNode.Declarator.ArrayDeclarator(x, _4);
                }),
                from _3 in left_bracket
                from _4 in type_qualifier_list.Then(static_keyword).Then(assignment_expression)
                from _5 in right_bracket
                select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x =>
                {
                    _4.full = true;
                    return new SyntaxNode.Declarator.ArrayDeclarator(x, _4);
                }),
                from _3 in left_bracket
                from _4 in type_qualifier_list.Then(multiply)
                from _5 in right_bracket
                select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => { return new SyntaxNode.Declarator.ArrayDeclarator(x, null); }),
                from _3 in left_bracket
                from _4 in multiply
                from _5 in right_bracket
                select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => { return new SyntaxNode.Declarator.ArrayDeclarator(x, null); }),
                from _3 in left_bracket
                from _5 in right_bracket
                select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => { return new SyntaxNode.Declarator.ArrayDeclarator(x, null); }),
                from _3 in left_paren
                from _4 in parameter_type_list
                from _5 in right_paren
                select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => { return new SyntaxNode.Declarator.FunctionDeclarator.AnsiFunctionDeclarator(x, _4); }),
                from _3 in left_paren
                from _4 in identifier_list
                from _5 in right_paren
                select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => { return new SyntaxNode.Declarator.FunctionDeclarator.KandRFunctionDeclarator(x, _4); }),
                from _3 in left_paren
                from _5 in right_paren
                select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => { return new SyntaxNode.Declarator.FunctionDeclarator.AbbreviatedFunctionDeclarator(x); })
            ).Many()
            select _2.Aggregate(_1, (s, x) => x(s))
        );

        public static readonly Parser<string[]> pointer = Combinator.Lazy(() => Combinator.Choice(
                from _1 in multiply select new[] { _1 },
                from _1 in multiply from _2 in type_qualifier_list select _2.Concat(new[] { _1 }).ToArray(),
                from _1 in multiply from _2 in pointer select _2.Concat(new[] { _1 }).ToArray(),
                from _1 in multiply
                from _2 in type_qualifier_list
                from _3 in pointer
                select _2.Concat(new[] { _1 }).Concat(_3).ToArray()
            )
        );

        public static readonly Parser<string[]> type_qualifier_list = Combinator.Lazy(() => type_qualifier.Many(1));

        public static readonly Parser<SyntaxNode.ParameterTypeList> parameter_type_list = Combinator.Lazy(() =>
            from _1 in parameter_list
            from _2 in comma.Then(ellipsis).Option()
            select new SyntaxNode.ParameterTypeList(_1, _2 != null)
        );

        public static readonly Parser<SyntaxNode.ParameterDeclaration[]> parameter_list =
            Combinator.Lazy(() => parameter_declaration.Repeat1(comma));

        public static readonly Parser<SyntaxNode.ParameterDeclaration> parameter_declaration = Combinator.Lazy(() =>
            from _1 in declaration_specifiers
            from _2 in Combinator.Choice(declarator, abstract_declarator.Select(x => (SyntaxNode.Declarator)x))
                .Option()
            select new SyntaxNode.ParameterDeclaration(_1, _2));

        public static readonly Parser<string[]> identifier_list = Combinator.Lazy(() => IDENTIFIER.Repeat1(comma));

        public static readonly Parser<SyntaxNode.TypeName> type_name = Combinator.Lazy(() =>
            from _1 in specifier_qualifier_list
            from _2 in abstract_declarator.Option()
            select new SyntaxNode.TypeName(_1, _2));

        public static readonly Parser<SyntaxNode.Declarator.AbstractDeclarator> abstract_declarator = Combinator.Lazy(() =>
            Combinator.Choice(
                from _1 in pointer
                from _2 in direct_abstract_declarator.Option()
                select (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.PointerAbstractDeclarator(_2, _1) { full = true },
                from _2 in direct_abstract_declarator
                select eval(() =>
                {
                    _2.full = true;
                    return _2;
                })
            ));


        public static readonly Parser<SyntaxNode.Declarator.AbstractDeclarator> direct_abstract_declarator = Combinator.Lazy(() =>
            from _1 in Combinator.Choice(
                from _2 in left_paren
                from _3 in abstract_declarator
                from _4 in right_paren
                select (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.GroupedAbstractDeclarator(_3),

                from _2 in left_bracket
                from _4 in right_bracket
                select (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator(null, null),

                from _2 in left_bracket
                from _3 in assignment_expression
                from _4 in right_bracket
                select eval(() =>
                {
                    _3.full = true;
                    return (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator(null, _3);
                }),

                from _2 in left_bracket
                from _3 in multiply
                from _4 in right_bracket
                select eval(() => { return (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator(null, null); }),

                from _2 in left_paren
                from _4 in right_paren
                select (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.FunctionAbstractDeclarator(null, null),

                from _2 in left_paren
                from _3 in parameter_type_list
                from _4 in right_paren
                select (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.FunctionAbstractDeclarator(null, _3)
            )
            from _2 in Combinator.Choice(
                from _2 in left_bracket
                from _4 in right_bracket
                select (Func<SyntaxNode.Declarator.AbstractDeclarator, SyntaxNode.Declarator.AbstractDeclarator>)(x =>
                {
                    return (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator(x, null);
                }),

                from _2 in left_bracket
                from _3 in assignment_expression
                from _4 in right_bracket
                select (Func<SyntaxNode.Declarator.AbstractDeclarator, SyntaxNode.Declarator.AbstractDeclarator>)(x =>
                {
                    _3.full = true;
                    return (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator(x, _3);
                }),

                from _2 in left_bracket
                from _3 in multiply
                from _4 in right_bracket
                select (Func<SyntaxNode.Declarator.AbstractDeclarator, SyntaxNode.Declarator.AbstractDeclarator>)(x =>
                {
                    return (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator(x, null);
                }),

                from _2 in left_paren
                from _4 in right_paren
                select (Func<SyntaxNode.Declarator.AbstractDeclarator, SyntaxNode.Declarator.AbstractDeclarator>)(x =>
                {
                    return (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.FunctionAbstractDeclarator(x, null);
                }),

                from _2 in left_paren
                from _3 in parameter_type_list
                from _4 in right_paren
                select (Func<SyntaxNode.Declarator.AbstractDeclarator, SyntaxNode.Declarator.AbstractDeclarator>)(x =>
                {
                    return (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.FunctionAbstractDeclarator(x, _3);
                })

            ).Many()
            select _2.Aggregate(_1, (s, x) => x(s)));


        public static readonly Parser<SyntaxNode.Initializer> initializer = Combinator.Lazy(() => Combinator.Choice(
            // 普通の定数値初期化
            assignment_expression.Select(x =>
            {
                x.full = true;
                return new SyntaxNode.Initializer(x, null);
            }),
            // 複合初期化
            from _1 in left_brace
            from _2 in (
                from _3 in initializer_list
                from _4 in comma.Option()
                select _3
            ).Option()
            from _5 in right_brace
            select new SyntaxNode.Initializer(null, _2)
        ));

        public static readonly Parser<Tuple<SyntaxNode.Initializer.Designator[], SyntaxNode.Initializer>[]> initializer_list = Combinator.Lazy(() => (
            from _1 in designation.Option()
            from _2 in initializer
            select Tuple.Create(_1, _2)
        ).Many(1));

        public static readonly Parser<SyntaxNode.Initializer.Designator[]> designation = Combinator.Lazy(() => designator_list.Skip(assign));

        public static readonly Parser<SyntaxNode.Initializer.Designator[]> designator_list = Combinator.Lazy(() => designator.Many(1));

        public static readonly Parser<SyntaxNode.Initializer.Designator> designator = Combinator.Lazy(() => Combinator.Choice(
            left_bracket.Then(constant_expression).Skip(right_bracket).Select(x => (SyntaxNode.Initializer.Designator)new SyntaxNode.Initializer.Designator.IndexDesignator(x)),
            member_access.Then(IDENTIFIER).Select(x => (SyntaxNode.Initializer.Designator)new SyntaxNode.Initializer.Designator.MemberDesignator(x))
        ));

        //#
        //# Statements
        //#

        public static readonly Parser<SyntaxNode.Statement> statement = Combinator.Lazy(() => Combinator.Choice(
            labeled_statement,
            compound_statement,
            expression_statement,
            selection_statement,
            iteration_statement,
            jump_statement
        ));

        public static readonly Parser<SyntaxNode.Statement> labeled_statement = Combinator.Lazy(() => Combinator.Choice(
            from _1 in label_name
            from _2 in colon
            from _3 in statement
            select (SyntaxNode.Statement)new SyntaxNode.Statement.LabeledStatement.GenericLabeledStatement(_1, _3),
            from _1 in case_keyword
            from _2 in constant_expression
            from _3 in colon
            from _4 in statement
            select (SyntaxNode.Statement)new SyntaxNode.Statement.LabeledStatement.CaseLabeledStatement(_2, _4),
            from _1 in default_keyword
            from _2 in constant_expression
            from _3 in colon
            from _4 in statement
            select (SyntaxNode.Statement)new SyntaxNode.Statement.LabeledStatement.DefaultLabeledStatement(_2, _4)
        ));

        public static readonly Parser<string> label_name = Combinator.Lazy(() => Combinator.Choice(IDENTIFIER, TYPEDEF_NAME));

        public static readonly Parser<SyntaxNode.Statement> compound_statement = Combinator.Lazy(() =>
            from _1 in left_brace
            from _2 in block_item_list.Option()
            from _3 in right_brace
            select (SyntaxNode.Statement)new SyntaxNode.Statement.CompoundStatement(_2));

        public static readonly Parser<SyntaxNode[]> block_item_list = Combinator.Lazy(() => block_item.Many(1));

        public static readonly Parser<SyntaxNode> block_item = Combinator.Lazy(() =>
            Combinator.Choice(declaration.Select(x => (SyntaxNode)x),
                statement.Select(x => (SyntaxNode)x) /*, local_function_definition.Select(x => (SyntaxNode)x) */));

        public static readonly Parser<SyntaxNode.Statement> expression_statement = Combinator.Lazy(() =>
            from _1 in expression.Select(x =>
            {
                x.full = true;
                return x;
            }).Option()
            from _2 in semicolon
            select (SyntaxNode.Statement)new SyntaxNode.Statement.ExpressionStatement(_1));

        public static readonly Parser<SyntaxNode.Statement> selection_statement = Combinator.Lazy(() => Combinator.Choice(
            from _1 in if_keyword
            from _2 in left_paren
            from _3 in expression.Select(x =>
            {
                x.full = true;
                return x;
            })
            from _4 in right_paren
            from _5 in statement
            from _6 in else_keyword.Then(statement).Option()
            select (SyntaxNode.Statement)new SyntaxNode.Statement.SelectionStatement.IfStatement(_3, _5, _6),
            from _1 in switch_keyword
            from _2 in left_paren
            from _3 in expression.Select(x =>
            {
                x.full = true;
                return x;
            })
            from _4 in right_paren
            from _5 in statement
            select (SyntaxNode.Statement)new SyntaxNode.Statement.SelectionStatement.SwitchStatement(_3, _5)
        ));

        public static readonly Parser<SyntaxNode.Statement> iteration_statement = Combinator.Lazy(() => Combinator.Choice(
            from _1 in while_keyword
            from _2 in left_paren
            from _3 in expression.Select(x =>
            {
                x.full = true;
                return x;
            })
            from _4 in right_paren
            from _5 in statement
            select (SyntaxNode.Statement)new SyntaxNode.Statement.IterationStatement.WhileStatement(_3, _5),
            from _1 in do_keyword
            from _2 in statement
            from _3 in while_keyword
            from _4 in left_paren
            from _5 in expression.Select(x =>
            {
                x.full = true;
                return x;
            })
            from _6 in right_paren
            from _7 in semicolon
            select (SyntaxNode.Statement)new SyntaxNode.Statement.IterationStatement.DoStatement(_2, _5),
            from _1 in for_keyword
            from _2 in left_paren
            from _3 in expression_statement
            from _4 in expression_statement
            from _5 in expression.Select(x =>
            {
                x.full = true;
                return x;
            }).Option()
            from _6 in right_paren
            from _7 in statement
            select (SyntaxNode.Statement)new SyntaxNode.Statement.IterationStatement.ForStatement(_3, _4, _5),
            from _1 in for_keyword
            from _2 in left_paren
            from _3 in declaration
            from _4 in expression_statement
            from _5 in expression.Select(x =>
            {
                x.full = true;
                return x;
            }).Option()
            from _6 in right_paren
            from _7 in statement
            select (SyntaxNode.Statement)new SyntaxNode.Statement.IterationStatement.C99ForStatement(_3, _4, _5)
        ));

        public static readonly Parser<SyntaxNode.Statement> jump_statement = Combinator.Lazy(() => Combinator.Choice(
            from _1 in goto_keyword
            from _2 in label_name
            from _3 in semicolon
            select (SyntaxNode.Statement)new SyntaxNode.Statement.JumpStatement.GotoStatement(_2),
            //from _1 in goto_keyword
            //from _2 in multiply
            //from _3 in label_name
            //from _4 in semicolon
            //select (Statement)new ErrorStatement(_2,_3),
            from _1 in continue_keyword
            from _2 in semicolon
            select (SyntaxNode.Statement)new SyntaxNode.Statement.JumpStatement.ContinueStatement(),
            from _1 in break_keyword
            from _2 in semicolon
            select (SyntaxNode.Statement)new SyntaxNode.Statement.JumpStatement.BreakStatement(),
            from _1 in return_keyword
            from _2 in expression.Select(x =>
            {
                x.full = true;
                return x;
            }).Option()
            from _3 in semicolon
            select (SyntaxNode.Statement)new SyntaxNode.Statement.JumpStatement.ReturnStatement(_2)
        ));

        //#
        //# External definitions
        //#
        public static readonly Parser<SyntaxNode.TranslationUnit> translation_unit =
            Combinator.Lazy(() => external_declaration.Many(1).Select(x => new SyntaxNode.TranslationUnit(x)));

        public static readonly Parser<SyntaxNode> external_declaration = Combinator.Lazy(() =>
            Combinator.Choice(
                function_definition.Select(x => (SyntaxNode)x),
                global_declaration.Select(x => (SyntaxNode)x)
            )
        );

        public static readonly Parser<SyntaxNode.Definition.FunctionDefinition> function_definition = Combinator.Lazy(() =>
            Combinator.Choice(
                (
                    from _1 in declaration_specifiers
                    from _2 in declarator
                    from _3 in declaration_list
                    from _4 in compound_statement
                    select (SyntaxNode.Definition.FunctionDefinition)new SyntaxNode.Definition.FunctionDefinition.KandRFunctionDefinition(_1, _2, _3.ToList(), _4)
                ),
                (
                    from _1 in declaration_specifiers
                    from _2 in declarator
                    from _3 in compound_statement
                    select (_2 is SyntaxNode.Declarator.FunctionDeclarator.AnsiFunctionDeclarator)
                        ? (SyntaxNode.Definition.FunctionDefinition)new SyntaxNode.Definition.FunctionDefinition.AnsiFunctionDefinition(_1, _2, _3)
                        : (_2 is SyntaxNode.Declarator.FunctionDeclarator.KandRFunctionDeclarator)
                            ? (SyntaxNode.Definition.FunctionDefinition)new SyntaxNode.Definition.FunctionDefinition.KandRFunctionDefinition(_1, _2,
                                new List<SyntaxNode.Declaration>(), _3)
                            : (_2 is SyntaxNode.Declarator.FunctionDeclarator.AbbreviatedFunctionDeclarator)
                                ? (SyntaxNode.Definition.FunctionDefinition)new SyntaxNode.Definition.FunctionDefinition.AnsiFunctionDefinition(_1, _2, _3)
                                : null
                ),
                (
                    from _1 in declarator
                    from _2 in declaration_list
                    from _3 in compound_statement
                    select (SyntaxNode.Definition.FunctionDefinition)new SyntaxNode.Definition.FunctionDefinition.KandRFunctionDefinition(null, _1, _2.ToList(), _3)
                ),
                (
                    from _1 in declarator
                    from _2 in compound_statement
                    select (_1 is SyntaxNode.Declarator.FunctionDeclarator.AnsiFunctionDeclarator)
                        ? (SyntaxNode.Definition.FunctionDefinition)new SyntaxNode.Definition.FunctionDefinition.AnsiFunctionDefinition(null, _1, _2)
                        : (_1 is SyntaxNode.Declarator.FunctionDeclarator.KandRFunctionDeclarator)
                            ? (SyntaxNode.Definition.FunctionDefinition)new SyntaxNode.Definition.FunctionDefinition.KandRFunctionDefinition(null, _1,
                                new List<SyntaxNode.Declaration>(), _2)
                            : (_1 is SyntaxNode.Declarator.FunctionDeclarator.AbbreviatedFunctionDeclarator)
                                ? (SyntaxNode.Definition.FunctionDefinition)new SyntaxNode.Definition.FunctionDefinition.AnsiFunctionDefinition(null, _1, _2)
                                : null
                )
            ));

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

        public static readonly Parser<SyntaxNode.Declaration[]> declaration_list = Combinator.Lazy(() => declaration.Many(1));


        //end

        #endregion

        public static Result<SyntaxNode.TranslationUnit> Parse(TextReader reader)
        {
            var target = new Source("", reader);
            return translation_unit(target, Position.Empty, Position.Empty, new ParserStatus());
        }
    }
}