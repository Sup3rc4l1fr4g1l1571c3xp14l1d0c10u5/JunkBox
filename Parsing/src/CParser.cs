using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Parsing;

namespace CParser2 {
    public static class CParser {
        /*
                public class ParserStatus {
                    private LinkedList<Tuple<string, SyntaxNode>> typedefed_list {
                        get;
                    }
                    private ParserStatus prev_scope {
                        get;
                    }

                    private ParserStatus(
                        LinkedList<Tuple<string, SyntaxNode>> typedefed_list,
                        ParserStatus prev_scope
                    ) {
                        this.typedefed_list = typedefed_list;
                        this.prev_scope = prev_scope;
                    }

                    public ParserStatus() : this(LinkedList<Tuple<string, SyntaxNode>>.Empty, null) { }

                    public ParserStatus Extend(Tuple<string, SyntaxNode> typedef_entry) {
                        return new ParserStatus(
                            typedefed_list: LinkedList.Extend(typedef_entry, this.typedefed_list),
                            prev_scope: this.prev_scope
                        );
                    }
                    public ParserStatus PushScope() {
                        return new ParserStatus(
                            typedefed_list: LinkedList<Tuple<string, SyntaxNode>>.Empty,
                            prev_scope: this
                        );
                    }
                    public ParserStatus PopScope() {
                        return this.prev_scope;
                    }

                    internal Tuple<string, SyntaxNode> FindTypedef(string x) {
                        var p = this;
                        while (p != null) {
                            var ret = LinkedList.First(y => (y.Item1 == x), p.typedefed_list);
                            if (ret != null) {
                                return ret;
                            }
                            p = p.prev_scope;
                        }
                        return null;
                    }
                }

        */
        public class ParserStatus {
            private List<Tuple<string, SyntaxNode>> typedefed_list {
                get;
            }
            private int typedefed_list_bottom;
            private int typedefed_list_top;

            private ParserStatus prev_scope {
                get;
            }

            private ParserStatus(
                List<Tuple<string, SyntaxNode>> typedefed_list,
                int typedefed_list_bottom,
                int typedefed_list_top,
                ParserStatus prev_scope
            ) {
                this.typedefed_list = typedefed_list;
                this.prev_scope = prev_scope;
                this.typedefed_list_bottom = typedefed_list_bottom;
                this.typedefed_list_top = typedefed_list_top;
            }

            public ParserStatus() : this(new List<Tuple<string, SyntaxNode>>(),0,0,null) { }

            public ParserStatus Extend(Tuple<string, SyntaxNode> typedef_entry) {
                if (this.typedefed_list.Count <= typedefed_list_top) {
                    this.typedefed_list.Add(typedef_entry);
                } else {
                    this.typedefed_list[typedefed_list_top] = typedef_entry;
                }
                return new ParserStatus(
                    typedefed_list: this.typedefed_list,
                    typedefed_list_bottom: this.typedefed_list_bottom,
                    typedefed_list_top: this.typedefed_list_top+1,
                    prev_scope: this.prev_scope
                );
            }

            public ParserStatus PushScope() {
                return new ParserStatus(
                    typedefed_list: this.typedefed_list,
                    typedefed_list_bottom: this.typedefed_list_top,
                    typedefed_list_top: this.typedefed_list_top,
                    prev_scope: this
                );
            }

            public ParserStatus PopScope() {
                return this.prev_scope;
            }

            public Tuple<string, SyntaxNode> FindTypedef(string x) {
                // FindLast/FindLastIndexを使うと述語関数呼び出しのオーバーヘッドが大きいのでべた書きしている
                var i = this.typedefed_list_top;
                while (i > 0) {
                    i--;
                    var p = this.typedefed_list[i];
                    if (p.Item1 == x) {
                        return p;
                    }
                }
                return null;
            }
        }

        #region Tokenize rules

        public static readonly Parser<MatchRegion> digit =
            Combinator.Trace("digit",
                Combinator.AnyCharRegion("0123456789")
            );

        public static readonly Parser<MatchRegion> digits =
            Combinator.Trace("digits",
                Combinator.AnyCharsRegion("0123456789", min:1)
            );

        public static readonly Parser<MatchRegion> isdigits =
            Combinator.Trace("isdigits",
                Combinator.AnyCharsRegion("0123456789")
            );

        public static readonly Parser<MatchRegion> new_line =
            Combinator.Trace("new_line",
                Combinator.Choice(
                    Combinator.TokenRegion("\r\n"),
                    Combinator.TokenRegion("\r"),
                    Combinator.TokenRegion("\n")
                )
            );

        public static readonly Parser<MatchRegion> whitespaces =
            Combinator.Trace("whitespaces",
                Combinator.AnyCharsRegion(" \t\v\f", min:1)
            );

        public static readonly Parser<MatchRegion> block_comment =
            Combinator.Trace("block_comment",
                Combinator.Seq(
                    Combinator.TokenRegion("/*"),
                    Combinator.TokenRegion("*/").Not().Then(Combinator.AnyCharRegion()).ManyRegion(),
                    Combinator.TokenRegion("*/")
                )
            );

        public static readonly Parser<MatchRegion> line_comment =
            Combinator.Trace("line_comment",
                Combinator.Seq(
                    Combinator.TokenRegion("//"),
                    new_line.Not().Then(Combinator.AnyCharRegion()).ManyRegion(),
                    new_line
                )
            );

        public static readonly Parser<MatchRegion> comment =
            Combinator.Trace("comment",
                Combinator.Choice(
                    block_comment,
                    line_comment
                )
            );

        public static readonly Parser<MatchRegion> directive_space =
            Combinator.Trace("directive_space",
                Combinator.Choice(block_comment, whitespaces)
            );

        public static readonly Parser<MatchRegion> pragma_line_directive =
            Combinator.Trace("pragma_line_directive",
                    from _1 in directive_space.ManyRegion().Then(Combinator.TokenRegion("line"))
                    from _2 in directive_space.ManyRegion(1).Then(digits).String()
                    from _3 in directive_space.ManyRegion(1).Then(Combinator.TokenRegion("\""))
                    from _4 in
                        Combinator.Choice(
                            Combinator.TokenRegion("\\").Then(Combinator.AnyCharRegion()),
                            Combinator.AnyCharRegion("\"").Not().Then(Combinator.AnyCharRegion())
                        ).ManyRegion(1).String()
                    from _5 in Combinator.TokenRegion("\"")
                    from _6 in directive_space.ManyRegion().Then(new_line)
                    from _7 in Combinator.Reposition((position) => position.Reposition(_4, int.Parse(_2), 1))
                    select new MatchRegion(_1.Start, _6.End)
            );

        public static readonly Parser<MatchRegion> pragma_gccline_directive =
            Combinator.Trace("pragma_gccline_directive",
                    from _1 in directive_space.ManyRegion(1)
                    from _2 in digits.String()
                    from _3 in directive_space.ManyRegion(1).Then(Combinator.TokenRegion("\""))
                    from _4 in
                        Combinator.Choice(
                            Combinator.TokenRegion("\\").Then(Combinator.AnyCharRegion()),
                            Combinator.AnyCharRegion("\"").Not().Then(Combinator.AnyCharRegion())
                        ).ManyRegion(1).String()
                    from _5 in Combinator.TokenRegion("\"")
                    from _6 in directive_space.ManyRegion().Then(new_line)
                    from _7 in Combinator.Reposition((position) => position.Reposition(_4, int.Parse(_2), 1))
                    select new MatchRegion(_1.Start, _6.End)
            );

        public static readonly Parser<MatchRegion> pragma_unknowndirective =
            Combinator.Trace("pragma_unknowndirective",
                Combinator.Seq(
                    new_line.Not().Then(Combinator.AnyCharRegion()).ManyRegion(),
                    new_line
                )
            );

        public static readonly Parser<MatchRegion> directive =
            Combinator.Trace("directive",
                from _1 in Combinator.Position()
                where _1.Column == 1
                from _2 in directive_space.Many().Then(Combinator.Token("#"))
                from _3 in Combinator.Choice(pragma_line_directive, pragma_gccline_directive, pragma_unknowndirective)
                select _3
            );

        public static readonly Parser<MatchRegion> space =
            Combinator.Trace("space",
                Combinator.Choice(
                    directive,
                    new_line,
                    whitespaces,
                    comment
                )
            );

        public static readonly Parser<MatchRegion> isspaces =
            Combinator.Trace("isspaces",
                space.ManyRegion()
            ).Memoize();

        public static readonly Parser<MatchRegion> identpart_x =
            Combinator.Trace("identpart_x",
                Combinator.AnyCharRegion(x => ('a' <= x && x <= 'z') || ('A' <= x && x <= 'Z') || (x == '_'))
            );

        public static readonly Parser<MatchRegion> identpart_xs =
            Combinator.Trace("identpart_xs",
                Combinator.AnyCharsRegion(x => ('a' <= x && x <= 'z') || ('A' <= x && x <= 'Z') || (x == '_') || ('0' <= x && x <= '9'),min:1)
            );

        public static readonly Parser<MatchRegion> xdigits =
            Combinator.Trace("xdigits",
                Combinator.AnyCharsRegion("0123456789ABCDEFabcdef")
            );

        public static readonly Parser<MatchRegion> exponent =
            Combinator.Trace("exponent",
                Combinator.Seq(
                    Combinator.AnyCharRegion("eE"),
                    Combinator.AnyCharRegion("+-").Option(),
                    digits
                )
            );

        public static readonly Parser<MatchRegion> isexponent =
            Combinator.Trace("isexponent",
                exponent.Option()
            );

        public static readonly Parser<MatchRegion> float_size =
            Combinator.Trace("float_size",
                Combinator.AnyCharRegion("fFlL")
            );

        public static readonly Parser<MatchRegion> isfloat_size =
            Combinator.Trace("isfloat_size",
                float_size.Option()
            );

        public static readonly Parser<MatchRegion> int_size =
            Combinator.Trace("int_size",
                Combinator.AnyCharRegion("uUlL").ManyRegion(1)
            );

        public static readonly Parser<MatchRegion> isint_size =
            Combinator.Trace("isint_size",
                int_size.Option()
            );

        public static readonly Parser<string> identifier =
            Combinator.Trace("identifier",
                from _1 in isspaces
                from _2 in Combinator.Seq(identpart_x,identpart_xs.Option()).String()
                from _3 in isspaces
                select _2
            ).Memoize();

        public static readonly Parser<MatchRegion> hex_constant =
            Combinator.Trace("hex_constant",
                Combinator.Seq(
                    Combinator.Choice(Combinator.TokenRegion("0x"), Combinator.TokenRegion("0X")),
                    xdigits,
                    isint_size
                )
            );

        public static readonly Parser<MatchRegion> octal_constant =
            Combinator.Trace("octal_constant",
                Combinator.Seq(
                    Combinator.TokenRegion("0"),
                    digits,
                    isint_size
                )
            );

        public static readonly Parser<MatchRegion> decimal_constant =
            Combinator.Trace("decimal_constant",
                Combinator.Seq(
                    digits,
                    isint_size
                )
            );

        public static readonly Parser<MatchRegion> string_constant =
            Combinator.Trace("string_constant",
                Combinator.Seq(
                    Combinator.TokenRegion("L").Option(),
                    Combinator.TokenRegion("'"),
                    Combinator.Choice(
                        Combinator.Seq(Combinator.TokenRegion("\\"), Combinator.AnyCharRegion()),
                        Combinator.AnyCharRegion(@"'").Not().Then(Combinator.AnyCharRegion())
                    ).ManyRegion(1),
                    Combinator.TokenRegion("'")
                )
            );

        public static readonly Parser<MatchRegion> float_constant =
            Combinator.Trace("float_constant",
                Combinator.Choice(
                    Combinator.Seq(
                        digits,
                        exponent,
                        isfloat_size
                    )
                    ,
                    Combinator.Seq(
                        isdigits,
                        Combinator.TokenRegion("."),
                        digits,
                        isexponent,
                        isfloat_size
                    )
                    ,
                    Combinator.Seq(
                        digits,
                        Combinator.TokenRegion("."),
                        isdigits,
                        isexponent,
                        isfloat_size
                    )
                )
            );

        public static readonly Parser<MatchRegion> preprocessing_number =
            Combinator.Trace("preprocessing_number",
                Combinator.Seq(
                    Combinator.Seq(
                        Combinator.TokenRegion(".").Option(), 
                        digit
                    ),
                    Combinator.Choice(
                        Combinator.Seq(
                            Combinator.AnyCharRegion("eEpP"),
                            Combinator.AnyCharRegion("+-")
                        ),
                        Combinator.TokenRegion("."),
                        identpart_xs
                    ).ManyRegion()
                )
            );


        public static readonly Parser<string> constant =
            Combinator.Trace("constant",
                from _1 in isspaces
                from _2 in Combinator.Choice(
                    string_constant,
                    preprocessing_number.Refinement(
                        Combinator.Choice(
                            float_constant,
                            hex_constant,
                            octal_constant,
                            decimal_constant
                        )
                    )
                ).String()
                from _3 in isspaces
                select _2
            ).Memoize();

        public static readonly Parser<IReadOnlyList<string>> string_literal =
            Combinator.Trace("string_literal",
                from _1 in isspaces
                from _2 in (
                    from __1 in Combinator.Seq(
                        Combinator.TokenRegion("L").Option(),
                        Combinator.TokenRegion("\""),
                        Combinator.Choice(
                            Combinator.Seq(Combinator.TokenRegion("\\"), Combinator.AnyCharRegion()),
                            Combinator.AnyCharRegion("\"").Not().Then(Combinator.AnyCharRegion())
                        ).ManyRegion(),
                        Combinator.TokenRegion("\"")
                    ).String()
                    from __2 in isspaces
                    select __1
                ).Many(1)
                from _3 in isspaces
                select _2
            );


        public static readonly Parser<string> auto_keyword = Combinator.Trace("auto_keyword", identifier.Where(x => x == "auto"));
        public static readonly Parser<string> break_keyword = Combinator.Trace("break_keyword", identifier.Where(x => x == "break"));
        public static readonly Parser<string> case_keyword = Combinator.Trace("case_keyword", identifier.Where(x => x == "case"));
        public static readonly Parser<string> char_keyword = Combinator.Trace("char_keyword", identifier.Where(x => x == "char"));
        public static readonly Parser<string> const_keyword = Combinator.Trace("const_keyword", identifier.Where(x => x == "const"));
        public static readonly Parser<string> continue_keyword = Combinator.Trace("continue_keyword", identifier.Where(x => x == "continue"));
        public static readonly Parser<string> default_keyword = Combinator.Trace("default_keyword", identifier.Where(x => x == "default"));
        public static readonly Parser<string> do_keyword = Combinator.Trace("do_keyword", identifier.Where(x => x == "do"));
        public static readonly Parser<string> double_keyword = Combinator.Trace("double_keyword", identifier.Where(x => x == "double"));
        public static readonly Parser<string> else_keyword = Combinator.Trace("else_keyword", identifier.Where(x => x == "else"));
        public static readonly Parser<string> enum_keyword = Combinator.Trace("enum_keyword", identifier.Where(x => x == "enum"));
        public static readonly Parser<string> extern_keyword = Combinator.Trace("extern_keyword", identifier.Where(x => x == "extern"));
        public static readonly Parser<string> float_keyword = Combinator.Trace("float_keyword", identifier.Where(x => x == "float"));
        public static readonly Parser<string> for_keyword = Combinator.Trace("for_keyword", identifier.Where(x => x == "for"));
        public static readonly Parser<string> goto_keyword = Combinator.Trace("goto_keyword", identifier.Where(x => x == "goto"));
        public static readonly Parser<string> if_keyword = Combinator.Trace("if_keyword", identifier.Where(x => x == "if"));
        public static readonly Parser<string> int_keyword = Combinator.Trace("int_keyword", identifier.Where(x => x == "int"));
        public static readonly Parser<string> long_keyword = Combinator.Trace("long_keyword", identifier.Where(x => x == "long"));
        public static readonly Parser<string> register_keyword = Combinator.Trace("register_keyword", identifier.Where(x => x == "register"));
        public static readonly Parser<string> return_keyword = Combinator.Trace("return_keyword", identifier.Where(x => x == "return"));
        public static readonly Parser<string> short_keyword = Combinator.Trace("short_keyword", identifier.Where(x => x == "short"));
        public static readonly Parser<string> signed_keyword = Combinator.Trace("signed_keyword", identifier.Where(x => x == "signed"));
        public static readonly Parser<string> sizeof_keyword = Combinator.Trace("sizeof_keyword", identifier.Where(x => x == "sizeof"));
        public static readonly Parser<string> static_keyword = Combinator.Trace("static_keyword", identifier.Where(x => x == "static"));
        public static readonly Parser<string> struct_keyword = Combinator.Trace("struct_keyword", identifier.Where(x => x == "struct"));
        public static readonly Parser<string> switch_keyword = Combinator.Trace("switch_keyword", identifier.Where(x => x == "switch"));
        public static readonly Parser<string> typedef_keyword = Combinator.Trace("typedef_keyword", identifier.Where(x => x == "typedef"));
        public static readonly Parser<string> union_keyword = Combinator.Trace("union_keyword", identifier.Where(x => x == "union"));
        public static readonly Parser<string> unsigned_keyword = Combinator.Trace("unsigned_keyword", identifier.Where(x => x == "unsigned"));
        public static readonly Parser<string> void_keyword = Combinator.Trace("void_keyword", identifier.Where(x => x == "void"));
        public static readonly Parser<string> volatile_keyword = Combinator.Trace("volatile_keyword", identifier.Where(x => x == "volatile"));
        public static readonly Parser<string> while_keyword = Combinator.Trace("while_keyword", identifier.Where(x => x == "while"));

        public static readonly Parser<string> bool_keyword = Combinator.Trace("bool_keyword", identifier.Where(x => x == "_Bool"));
        public static readonly Parser<string> complex_keyword = Combinator.Trace("complex_keyword", identifier.Where(x => x == "_Complex"));
        public static readonly Parser<string> imaginary_keyword = Combinator.Trace("imaginary_keyword", identifier.Where(x => x == "_Imaginary"));
        public static readonly Parser<string> restrict_keyword = Combinator.Trace("restrict_keyword", identifier.Where(x => x == "restrict"));
        public static readonly Parser<string> inline_keyword = Combinator.Trace("inline_keyword", identifier.Where(x => x == "inline"));

        public static readonly Parser<string> builtin_va_list_keyword = Combinator.Trace("builtin_va_list_keyword", identifier.Where(x => x == "__builtin_va_list"));

        public static readonly HashSet<string> reserved_words = new HashSet<string>() {
            "auto",
            "break",
            "case",
            "char",
            "const",
            "continue",
            "default",
            "do",
            "double",
            "else",
            "enum",
            "extern",
            "float",
            "for",
            "goto",
            "if",
            "int",
            "long",
            "register",
            "return",
            "short",
            "signed",
            "sizeof",
            "static",
            "struct",
            "switch",
            "typedef",
            "union",
            "unsigned",
            "void",
            "volatile",
            "while",
            "_Bool",
            "_Complex",
            "_Imaginary",
            "restrict",
            "inline",
            "__builtin_va_list",
        };

        public static readonly Parser<string> ellipsis = Combinator.Trace("ellipsis", Combinator.Quote(isspaces, Combinator.Token("..."), isspaces));
        public static readonly Parser<string> semicolon = Combinator.Trace("semicolon", Combinator.Quote(isspaces, Combinator.Token(";"), isspaces));
        public static readonly Parser<string> comma = Combinator.Trace("comma", Combinator.Quote(isspaces, Combinator.Token(","), isspaces));
        public static readonly Parser<string> colon = Combinator.Trace("colon", Combinator.Quote(isspaces, Combinator.Token(":"), isspaces));
        public static readonly Parser<string> left_paren = Combinator.Trace("left_paren", Combinator.Quote(isspaces, Combinator.Token("("), isspaces));
        public static readonly Parser<string> right_paren = Combinator.Trace("right_paren", Combinator.Quote(isspaces, Combinator.Token(")"), isspaces));
        public static readonly Parser<string> member_access = Combinator.Trace("member_access", Combinator.Quote(isspaces, Combinator.Token("."), isspaces));
        public static readonly Parser<string> question_mark = Combinator.Trace("question_mark", Combinator.Quote(isspaces, Combinator.Token("?"), isspaces));

        public static readonly Parser<string> left_brace = Combinator.Trace("left_brace", Combinator.Quote(isspaces, Combinator.Choice(Combinator.Token("{"), Combinator.Token("<%")), isspaces));
        public static readonly Parser<string> right_brace = Combinator.Trace("right_brace", Combinator.Quote(isspaces, Combinator.Choice(Combinator.Token("}"), Combinator.Token("%>")), isspaces));

        public static readonly Parser<string> left_bracket = Combinator.Trace("left_bracket", Combinator.Quote(isspaces, Combinator.Choice(Combinator.Token("["), Combinator.Token("<:")), isspaces));
        public static readonly Parser<string> right_bracket = Combinator.Trace("right_bracket", Combinator.Quote(isspaces, Combinator.Choice(Combinator.Token("]"), Combinator.Token(":>")), isspaces));

        public static readonly Parser<string> right_shift_assign = Combinator.Trace("right_shift_assign", Combinator.Quote(isspaces, Combinator.Token(">>="), isspaces));
        public static readonly Parser<string> left_shift_assign = Combinator.Trace("left_shift_assign", Combinator.Quote(isspaces, Combinator.Token("<<="), isspaces));

        public static readonly Parser<string> add_assign = Combinator.Trace("add_assign", Combinator.Quote(isspaces, Combinator.Token("+="), isspaces));
        public static readonly Parser<string> subtract_assign = Combinator.Trace("subtract_assign", Combinator.Quote(isspaces, Combinator.Token("-="), isspaces));
        public static readonly Parser<string> multiply_assign = Combinator.Trace("multiply_assign", Combinator.Quote(isspaces, Combinator.Token("*="), isspaces));
        public static readonly Parser<string> divide_assign = Combinator.Trace("divide_assign", Combinator.Quote(isspaces, Combinator.Token("/="), isspaces));
        public static readonly Parser<string> modulus_assign = Combinator.Trace("modulus_assign", Combinator.Quote(isspaces, Combinator.Token("%="), isspaces));
        public static readonly Parser<string> binary_and_assign = Combinator.Trace("binary_and_assign", Combinator.Quote(isspaces, Combinator.Token("&="), isspaces));
        public static readonly Parser<string> xor_assign = Combinator.Trace("xor_assign", Combinator.Quote(isspaces, Combinator.Token("^="), isspaces));
        public static readonly Parser<string> binary_or_assign = Combinator.Trace("binary_or_assign", Combinator.Quote(isspaces, Combinator.Token("|="), isspaces));
        public static readonly Parser<string> inc = Combinator.Trace("inc", Combinator.Quote(isspaces, Combinator.Token("++"), isspaces));
        public static readonly Parser<string> dec = Combinator.Trace("dec", Combinator.Quote(isspaces, Combinator.Token("--"), isspaces));
        public static readonly Parser<string> pointer_access = Combinator.Trace("pointer_access", Combinator.Quote(isspaces, Combinator.Token("->"), isspaces));
        public static readonly Parser<string> logical_and = Combinator.Trace("logical_and", Combinator.Quote(isspaces, Combinator.Token("&&"), isspaces));
        public static readonly Parser<string> logical_or = Combinator.Trace("logical_or", Combinator.Quote(isspaces, Combinator.Token("||"), isspaces));
        public static readonly Parser<string> less_equal = Combinator.Trace("less_equal", Combinator.Quote(isspaces, Combinator.Token("<="), isspaces));
        public static readonly Parser<string> greater_equal = Combinator.Trace("greater_equal", Combinator.Quote(isspaces, Combinator.Token(">="), isspaces));
        public static readonly Parser<string> equal = Combinator.Trace("equal", Combinator.Quote(isspaces, Combinator.Token("=="), isspaces));
        public static readonly Parser<string> not_equal = Combinator.Trace("not_equal", Combinator.Quote(isspaces, Combinator.Token("!="), isspaces));

        public static readonly Parser<string> assign = Combinator.Trace("assign", Combinator.Quote(isspaces, Combinator.Token("=").Skip(Combinator.AnyChar("=").Not()), isspaces));
        public static readonly Parser<string> add = Combinator.Trace("add", Combinator.Quote(isspaces, Combinator.Token("+").Skip(Combinator.AnyChar("+=").Not()), isspaces));
        public static readonly Parser<string> subtract = Combinator.Trace("subtract", Combinator.Quote(isspaces, Combinator.Token("-").Skip(Combinator.AnyChar("-=").Not()), isspaces));
        public static readonly Parser<string> multiply = Combinator.Trace("multiply", Combinator.Quote(isspaces, Combinator.Token("*").Skip(Combinator.AnyChar("=").Not()), isspaces));
        public static readonly Parser<string> divide = Combinator.Trace("divide", Combinator.Quote(isspaces, Combinator.Token("/").Skip(Combinator.AnyChar("=").Not()), isspaces));
        public static readonly Parser<string> modulus = Combinator.Trace("modulus", Combinator.Quote(isspaces, Combinator.Token("%").Skip(Combinator.AnyChar("=").Not()), isspaces));
        public static readonly Parser<string> less = Combinator.Trace("less", Combinator.Quote(isspaces, Combinator.Token("<").Skip(Combinator.AnyChar("<=").Not()), isspaces));
        public static readonly Parser<string> greater = Combinator.Trace("greater", Combinator.Quote(isspaces, Combinator.Token(">").Skip(Combinator.AnyChar(">=").Not()), isspaces));
        public static readonly Parser<string> negate = Combinator.Trace("negate", Combinator.Quote(isspaces, Combinator.Token("!").Skip(Combinator.AnyChar("=").Not()), isspaces));
        public static readonly Parser<string> binary_or = Combinator.Trace("binary_or", Combinator.Quote(isspaces, Combinator.Token("|").Skip(Combinator.AnyChar("|=").Not()), isspaces));
        public static readonly Parser<string> binary_and = Combinator.Trace("binary_and", Combinator.Quote(isspaces, Combinator.Token("&").Skip(Combinator.AnyChar("&=").Not()), isspaces));
        public static readonly Parser<string> xor = Combinator.Trace("xor", Combinator.Quote(isspaces, Combinator.Token("^").Skip(Combinator.AnyChar("=").Not()), isspaces));
        public static readonly Parser<string> left_shift = Combinator.Trace("left_shift", Combinator.Quote(isspaces, Combinator.Token("<<").Skip(Combinator.AnyChar("=").Not()), isspaces));
        public static readonly Parser<string> right_shift = Combinator.Trace("right_shift", Combinator.Quote(isspaces, Combinator.Token(">>").Skip(Combinator.AnyChar("=").Not()), isspaces));
        public static readonly Parser<string> inverse = Combinator.Trace("inverse", Combinator.Quote(isspaces, Combinator.Token("~").Skip(Combinator.AnyChar("=").Not()), isspaces));

        public static readonly Parser<string> IDENTIFIER =
            Combinator.Trace("IDENTIFIER",
                identifier.Where(x => reserved_words.Contains(x) == false)
            );

        public static readonly Parser<string> TYPEDEF_NAME =
            Combinator.Trace("TYPEDEF_NAME",
                identifier.Where((x, s) => reserved_words.Contains(x) == false && ((ParserStatus)s).FindTypedef(x) != null)
            );

        public static readonly Parser<string> CONSTANT = constant;

        #endregion

        #region Syntax rules

        //#
        //# Expressions
        //#

        public static readonly Parser<SyntaxNode.Expression> primary_expression =
            Combinator.Trace("primary_expression",
                Combinator.Choice(
                    IDENTIFIER.Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PrimaryExpression.ObjectSpecifier(x)),
                    CONSTANT.Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PrimaryExpression.ConstantSpecifier(x)),
                    string_literal.Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PrimaryExpression.StringLiteralSpecifier(x)),
                    //NULL.Select(x => (SyntaxNode.Expression)new NullConstantSpecifier(x)),
                    Combinator.Quote(left_paren, Combinator.Lazy(() => expression), right_paren).Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PrimaryExpression.GroupedExpression(x)),
                    Combinator.Quote(left_paren, Combinator.Lazy(() => compound_statement), right_paren).Select(x => (SyntaxNode.Expression)new SyntaxNode.Expression.ErrorExpression(x))
                )
            );

        public static readonly Parser<SyntaxNode.Expression> postfix_expression =
            Combinator.Trace("postfix_expression",
                from _1 in Combinator.Choice(
                    from _2 in Combinator.Quote(left_paren, Combinator.Lazy(() => type_name), right_paren)
                    from _5 in Combinator.Quote(left_brace, Combinator.Lazy(() => initializer_list).Skip(comma.Option()), right_brace)
                    select (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.CompoundLiteralExpression(_2, _5)
                    ,
                    primary_expression
                )
                from _9 in Combinator.Choice(
                    from _10 in Combinator.Quote(left_bracket, Combinator.Lazy(() => expression), right_bracket)
                    select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.ArraySubscriptExpression(x, _10))
                    ,
                    from _10 in Combinator.Quote(left_paren, Combinator.Lazy(() => argument_expression_list), right_paren)
                    select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.FunctionCallExpression(x, _10))
                    ,
                    from _10 in member_access
                    from _11 in IDENTIFIER
                    select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.MemberAccessByValueExpression(x, _11))
                    ,
                    from _10 in pointer_access
                    from _11 in IDENTIFIER
                    select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.MemberAccessByPointerExpression(x, _11))
                    ,
                    //from _10 in member_access
                    //from _11 in CONSTANT
                    //select (Func<SyntaxNode.Expression, SyntaxNode.Expression>) (x => (SyntaxNode.Expression) new BitAccessByValueExpression(x, _11))
                    //,
                    //from _10 in pointer_access
                    //from _11 in CONSTANT
                    //select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x => (SyntaxNode.Expression)new BitAccessByPointerExpression(x, _11))
                    //,
                    from _10 in inc
                    select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.PostfixIncrementExpression(x))
                    ,
                    from _10 in dec
                    select (Func<SyntaxNode.Expression, SyntaxNode.Expression>)(x => (SyntaxNode.Expression)new SyntaxNode.Expression.PostfixExpression.PostfixDecrementExpression(x))
                ).Many()
                select _9.Aggregate(_1, (s, x) => x(s))
            );

        public static readonly Parser<SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression.OperatorKind> unary_arithmetic_operator =
            Combinator.Trace("unary_arithmetic_operator",
                Combinator.Choice(
                    add.Select(x => SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression.OperatorKind.Add), 
                    subtract.Select(x => SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression.OperatorKind.Subtract), 
                    inverse.Select(x => SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression.OperatorKind.Inverse), 
                    negate.Select(x => SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression.OperatorKind.Negate)
                )
            );

        public static readonly Parser<SyntaxNode.Expression> unary_expression =
            Combinator.Trace("unary_expression",
                
                    Combinator.Choice(
                        from _1 in inc
                        from _2 in Combinator.Lazy(() => unary_expression)
                        select  (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.PrefixIncrementExpression(_2)
                        ,
                        from _1 in dec
                        from _2 in Combinator.Lazy(() => unary_expression)
                        select (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.PrefixDecrementExpression(_2)
                        ,
                        from _1 in binary_and
                        from _2 in Combinator.Lazy(() => cast_expression)
                        select (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.AddressExpression(_2)
                        ,
                        from _1 in multiply
                        from _2 in Combinator.Lazy(() => cast_expression)
                        select (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.IndirectionExpression(_2)
                        ,
                        from _1 in unary_arithmetic_operator
                        from _2 in Combinator.Lazy(() => cast_expression)
                        select (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.UnaryArithmeticExpression(_1, _2)
                        ,
                        from _1 in sizeof_keyword
                        from _2 in Combinator.Quote(left_paren, Combinator.Lazy(() => type_name), right_paren)
                        select (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.SizeofTypeExpression(_2)
                        ,
                        from _1 in sizeof_keyword
                        from _2 in Combinator.Lazy(() => unary_expression)
                        select (SyntaxNode.Expression)new SyntaxNode.Expression.UnaryExpression.SizeofExpression(_2)
                        ,
                        //alighof_keyword.Then(unary_expression).Select(x => (SyntaxNode.Expression)new AlignofExpression("alignof", x)),
                        //from _1 in alighof_keyword from _2 in left_paren from _3 in type_name from _4 in right_paren select (SyntaxNode.Expression)new AlignofTypeExpression(_1, _3)
                        postfix_expression
                    )
            );

        public static readonly Parser<SyntaxNode.Expression> cast_expression =
            Combinator.Trace("cast_expression",
                from _1 in Combinator.Quote(left_paren, Combinator.Lazy(() => type_name), right_paren).Many()
                from _2 in unary_expression
                select _1.Reverse().Aggregate(_2, (s, x) => new SyntaxNode.Expression.CastExpression(x, s))
            ).Memoize();

        public static readonly Parser<SyntaxNode.Expression> multiplicative_expression =
            Combinator.Trace("multiplicative_expression",
                from _1 in cast_expression
                from _2 in (
                    from _3 in Combinator.Choice(
                        multiply.Select(x => SyntaxNode.Expression.BinaryExpression.MultiplicativeExpression.OperatorKind.multiply), 
                        divide.Select(x => SyntaxNode.Expression.BinaryExpression.MultiplicativeExpression.OperatorKind.divide), 
                        modulus.Select(x => SyntaxNode.Expression.BinaryExpression.MultiplicativeExpression.OperatorKind.modulus)
                    )
                    from _4 in cast_expression
                    select Tuple.Create(_3, _4)
                ).Many()
                select _2.Aggregate(_1, (s, x) => new SyntaxNode.Expression.BinaryExpression.MultiplicativeExpression(x.Item1, s, x.Item2))
            );

        public static readonly Parser<SyntaxNode.Expression> additive_expression =
            Combinator.Trace("additive_expression",
                from _1 in multiplicative_expression
                from _2 in (
                    from _3 in Combinator.Choice(
                        add.Select(x => SyntaxNode.Expression.BinaryExpression.AdditiveExpression.OperatorKind.add), 
                        subtract.Select(x => SyntaxNode.Expression.BinaryExpression.AdditiveExpression.OperatorKind.subtract)
                    )
                    from _4 in multiplicative_expression
                    select Tuple.Create(_3, _4)
                ).Many()
                select _2.Aggregate(_1, (s, x) => new SyntaxNode.Expression.BinaryExpression.AdditiveExpression(x.Item1, s, x.Item2))
            );

        public static readonly Parser<SyntaxNode.Expression> shift_expression =
            Combinator.Trace("shift_expression",
                from _1 in additive_expression
                from _2 in (
                    from _3 in Combinator.Choice(
                        left_shift.Select(x => SyntaxNode.Expression.BinaryExpression.ShiftExpression.OperatorKind.left_shift), 
                        right_shift.Select(x => SyntaxNode.Expression.BinaryExpression.ShiftExpression.OperatorKind.right_shift)
                    )
                    from _4 in additive_expression
                    select Tuple.Create(_3, _4)
                ).Many()
                select _2.Aggregate(_1, (s, x) => new SyntaxNode.Expression.BinaryExpression.ShiftExpression(x.Item1, s, x.Item2))
            );

        public static readonly Parser<SyntaxNode.Expression> relational_expression =
            Combinator.Trace("relational_expression",
                from _1 in shift_expression
                from _2 in (
                    from _3 in Combinator.Choice(
                        less_equal.Select(x => SyntaxNode.Expression.BinaryExpression.RelationalExpression.OperatorKind.less_equal), 
                        less.Select(x => SyntaxNode.Expression.BinaryExpression.RelationalExpression.OperatorKind.less), 
                        greater_equal.Select(x => SyntaxNode.Expression.BinaryExpression.RelationalExpression.OperatorKind.greater_equal), 
                        greater.Select(x => SyntaxNode.Expression.BinaryExpression.RelationalExpression.OperatorKind.greater)
                    )
                    from _4 in shift_expression
                    select Tuple.Create(_3, _4)
                ).Many()
                select _2.Aggregate(_1, (s, x) => new SyntaxNode.Expression.BinaryExpression.RelationalExpression(x.Item1, s, x.Item2))
            );

        public static readonly Parser<SyntaxNode.Expression> equality_expression =
            Combinator.Trace("equality_expression",
                from _1 in relational_expression
                from _2 in (
                    from _3 in Combinator.Choice(
                        equal.Select(x => SyntaxNode.Expression.BinaryExpression.EqualityExpression.OperatorKind.equal), 
                        not_equal.Select(x => SyntaxNode.Expression.BinaryExpression.EqualityExpression.OperatorKind.not_equal)
                    )
                    from _4 in relational_expression
                    select Tuple.Create(_3, _4)
                ).Many()
                select _2.Aggregate(_1, (s, x) => new SyntaxNode.Expression.BinaryExpression.EqualityExpression(x.Item1, s, x.Item2))
            );

        public static readonly Parser<SyntaxNode.Expression> and_expression =
            Combinator.Trace("and_expression",
                equality_expression.Separate(binary_and, min: 1).Select(x => x.Aggregate((s, y) => new SyntaxNode.Expression.BinaryExpression.AndExpression(s, y)))
            );

        public static readonly Parser<SyntaxNode.Expression> exclusive_or_expression =
            Combinator.Trace("exclusive_or_expression",
                and_expression.Separate(xor, min: 1).Select(x => x.Aggregate((s, y) => new SyntaxNode.Expression.BinaryExpression.ExclusiveOrExpression(s, y)))
            );

        public static readonly Parser<SyntaxNode.Expression> inclusive_or_expression =
            Combinator.Trace("inclusive_or_expression",
                exclusive_or_expression.Separate(binary_or, min: 1).Select(x => x.Aggregate((s, y) => new SyntaxNode.Expression.BinaryExpression.InclusiveOrExpression(s, y)))
            );

        public static readonly Parser<SyntaxNode.Expression> logical_and_expression =
            Combinator.Trace("logical_and_expression",
                inclusive_or_expression.Separate(logical_and, min: 1).Select(x => x.Aggregate((s, y) => new SyntaxNode.Expression.BinaryExpression.LogicalAndExpression(s, y)))
            );

        public static readonly Parser<SyntaxNode.Expression> logical_or_expression =
            Combinator.Trace("logical_or_expression",
                logical_and_expression.Separate(logical_or, min: 1).Select(x => x.Aggregate((s, y) => new SyntaxNode.Expression.BinaryExpression.LogicalOrExpression(s, y)))
            );

        public static readonly Parser<SyntaxNode.Expression> conditional_expression =
            Combinator.Trace("conditional_expression",
                Combinator.Choice(
                    from _1 in logical_or_expression
                    from _2 in (
                        from _3 in question_mark
                        from _4 in expression
                        from _5 in colon
                        from _6 in conditional_expression
                        select (SyntaxNode.Expression)new SyntaxNode.Expression.BinaryExpression.ConditionalExpression(_1, _4, _6)
                    ).Option()
                    select _2 ?? _1
                )
            ).Memoize();

        public static readonly Parser<SyntaxNode.Expression> assignment_expression =
            Combinator.Trace("assignment_expression",
                Combinator.Choice<SyntaxNode.Expression>(
                    from _1 in cast_expression
                    from _2 in Combinator.Choice<SyntaxNode.Expression>(
                        from __1 in assign
                        from __2 in assignment_expression
                        select (SyntaxNode.Expression)new SyntaxNode.Expression.BinaryExpression.SimpleAssignmentExpression(_1, __2)
                        ,
                        from __1 in Combinator.Choice(
                            multiply_assign.Select(x => SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.multiply_assign), 
                            divide_assign.Select(x => SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.divide_assign), 
                            modulus_assign.Select(x => SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.modulus_assign), 
                            add_assign.Select(x => SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.add_assign), 
                            subtract_assign.Select(x => SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.subtract_assign),
                            left_shift_assign.Select(x => SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.left_shift_assign), 
                            right_shift_assign.Select(x => SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.right_shift_assign), 
                            binary_and_assign.Select(x => SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.binary_and_assign), 
                            binary_or_assign.Select(x => SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.binary_or_assign), 
                            xor_assign.Select(x => SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression.OperatorKind.xor_assign)
                        )
                        from __2 in assignment_expression
                        select (SyntaxNode.Expression)new SyntaxNode.Expression.BinaryExpression.CompoundAssignmentExpression(__1, _1, __2)
                    )
                    select _2
                    ,
                    Combinator.Empty<SyntaxNode.Expression>(true).Then(conditional_expression)
                )
            ).Memoize();

        public static readonly Parser<IReadOnlyList<SyntaxNode.Expression>> argument_expression_list =
            Combinator.Trace("argument_expression_list",
                assignment_expression.Separate(comma)
            );

        public static readonly Parser<SyntaxNode.Expression> expression =
            Combinator.Trace("expression",
                assignment_expression.Separate(comma, min: 1).Select(x => (x.Count == 1) ? x[0] : (SyntaxNode.Expression)new SyntaxNode.Expression.CommaSeparatedExpression(x))
            ).Memoize();

        public static readonly Parser<SyntaxNode.Expression> constant_expression =
            Combinator.Trace("constant_expression",
                    conditional_expression
            ).Memoize();

        //#
        //# Declarations
        //#

        public static readonly Parser<SyntaxNode.Declaration> declaration =
            Combinator.Trace("declaration",
                Combinator.Lazy(() =>
                    from _1 in declaration_specifiers
                    from _2 in init_declarator_list.Option().Select(x => x ?? new SyntaxNode.InitDeclarator[0])
                    from _3 in semicolon
                    let _4 = new SyntaxNode.Declaration(_1, _2)
                    from _5 in Combinator.Action(x => {
                        var ps = (ParserStatus)x;
                        foreach (var item in _4.items) {
                            if (item is SyntaxNode.TypeDeclaration.TypedefDeclaration) {
                                var typedefname = ((SyntaxNode.TypeDeclaration.TypedefDeclaration)item).identifier;
                                ps = ps.Extend(Tuple.Create(typedefname, item));
                            }
                        }
                        return ps;
                    })
                    select _4
                )
            ).Memoize();

        public static readonly Parser<SyntaxNode.Declaration> global_declaration =
            Combinator.Trace("global_declaration",
                Combinator.Lazy(() =>
                    Combinator.Choice(
                        declaration,
                        init_declarator_list.Skip(semicolon).Select(x => new SyntaxNode.Declaration(null, x)),
                        semicolon.Select(x => (SyntaxNode.Declaration)null) // # NOTE: To accept extra semicolons in the global scope.
                    )
                )
            );

        public static readonly Parser<SyntaxNode.DeclarationSpecifiers> declaration_specifiers =
            Combinator.Trace("declaration_specifiers",
                Combinator.Lazy(() =>
                    from _1 in Combinator.Empty<int>(true)
                    let  _2 = new SyntaxNode.DeclarationSpecifiers()
                    from _3 in 
                        Combinator.Choice(
                            storage_class_specifier.Select(x => { _2.storage_class_specifier = x; return 0; }),
                            type_specifier.Select(x => { _2.type_specifiers.Add(x); return 0; }),
                            type_qualifier.Select(x => {_2.type_qualifiers.Add(x); return 0; }),
                            function_specifier.Select(x => { _2.function_specifier = x; return 0; })
                        ).Many(1)
                    select _2
                )
            ).Memoize();

        public static readonly Parser<SyntaxNode.InitDeclarator> init_declarator =
            Combinator.Trace("init_declarator",
                Combinator.Lazy(() =>
                    from _1 in declarator
                    from _2 in assign.Then(initializer).Option()
                    select new SyntaxNode.InitDeclarator(_1, _2)
                )
            );

        public static readonly Parser<IReadOnlyList<SyntaxNode.InitDeclarator>> init_declarator_list =
            Combinator.Trace("init_declarator_list",
                init_declarator.Separate(comma, min: 1)
            );



        public static readonly Parser<SyntaxNode.StorageClassSpecifierKind> storage_class_specifier =
            Combinator.Trace("storage_class_specifier",
                Combinator.Choice(
                    typedef_keyword.Select(x => SyntaxNode.StorageClassSpecifierKind.typedef_keyword),
                    extern_keyword.Select(x => SyntaxNode.StorageClassSpecifierKind.extern_keyword),
                    static_keyword.Select(x => SyntaxNode.StorageClassSpecifierKind.static_keyword),
                    auto_keyword.Select(x => SyntaxNode.StorageClassSpecifierKind.auto_keyword),
                    register_keyword.Select(x => SyntaxNode.StorageClassSpecifierKind.register_keyword)
                )
            );

        public static readonly Parser<SyntaxNode.TypeSpecifier> type_specifier =
            Combinator.Trace("type_specifier",
                Combinator.Choice(
                    Combinator.Choice(
                        void_keyword.Select(x => SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.void_keyword),
                        char_keyword.Select(x => SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.char_keyword),
                        short_keyword.Select(x => SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.short_keyword),
                        int_keyword.Select(x => SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.int_keyword),
                        long_keyword.Select(x => SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.long_keyword),
                        float_keyword.Select(x => SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.float_keyword),
                        double_keyword.Select(x => SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.double_keyword),
                        signed_keyword.Select(x => SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.signed_keyword),
                        unsigned_keyword.Select(x => SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.unsigned_keyword),
                        bool_keyword.Select(x => SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.bool_keyword),
                        complex_keyword.Select(x => SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.complex_keyword),
                        imaginary_keyword.Select(x => SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.imaginary_keyword),
                        builtin_va_list_keyword.Select(x => SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.builtin_va_list_keyword)
                    ).Select(x => (SyntaxNode.TypeSpecifier)new SyntaxNode.TypeSpecifier.StandardTypeSpecifier(x)),
                    TYPEDEF_NAME.Select(x => (SyntaxNode.TypeSpecifier)new SyntaxNode.TypeSpecifier.TypedefTypeSpecifier(x)),
                    Combinator.Lazy(() => struct_or_union_specifier).Select(x => x),
                    Combinator.Lazy(() => enum_specifier).Select(x => (SyntaxNode.TypeSpecifier)x)
                )
            ).Memoize();

        public static readonly Parser<SyntaxNode.TypeQualifierKind> type_qualifier =
            Combinator.Trace("type_qualifier",
                Combinator.Choice(
                    const_keyword.Select(x => SyntaxNode.TypeQualifierKind.const_keyword),
                    volatile_keyword.Select(x => SyntaxNode.TypeQualifierKind.volatile_keyword),
                    restrict_keyword.Select(x => SyntaxNode.TypeQualifierKind.restrict_keyword)
                )
            );


        public static readonly Parser<SyntaxNode.SpecifierQualifierList> specifier_qualifier_list =
            Combinator.Trace("specifier_qualifier_list",
                from _1 in Combinator.Empty<int>(true)
                let _2 = new SyntaxNode.SpecifierQualifierList()
                from _3 in 
                    Combinator.Choice(
                        type_specifier.Select(x => { _2.type_specifiers.Add(x); return 0; }),
                        type_qualifier.Select(x => { _2.type_qualifiers.Add(x); return 0; })
                    ).Many(1)
                select _2
            ).Memoize();

        public static readonly Parser<SyntaxNode.StructDeclarator> struct_declarator =
            Combinator.Trace("struct_declarator",
                from _1 in Combinator.Lazy(() => declarator.Option())
                from _2 in colon.Then(constant_expression).Option()
                select new SyntaxNode.StructDeclarator(_1, _2)
            );

        public static readonly Parser<IReadOnlyList<SyntaxNode.StructDeclarator>> struct_declarator_list =
            Combinator.Trace("struct_declarator_list",
                struct_declarator.Separate(comma, min: 1)
            );

        public static readonly Parser<SyntaxNode.StructDeclaration> struct_declaration =
            Combinator.Trace("struct_declaration",
                from _1 in specifier_qualifier_list
                from _2 in struct_declarator_list.Option().Select(x => x ?? new SyntaxNode.StructDeclarator[0])
                from _3 in semicolon
                select new SyntaxNode.StructDeclaration(_1, _2)
            );

        public static readonly Parser<IReadOnlyList<SyntaxNode.StructDeclaration>> struct_declaration_list =
            Combinator.Trace("struct_declaration_list",
                from _3 in left_brace
                from _4 in struct_declaration.Many()
                from _5 in right_brace
                select _4
            ).Memoize();

        static ulong anonCounter;

        private static string create_anon_tag_name(string _1) {
            return $"${_1}_{anonCounter++}";
        }

        public static readonly Parser<SyntaxNode.TypeSpecifier> struct_or_union_specifier =
            Combinator.Trace("struct_or_union_specifier",
                Combinator.Choice(
                    from _1 in struct_keyword
                    from _2 in IDENTIFIER.Option()
                    from _4 in struct_declaration_list.Option()
                    where _2 != null || _4 != null
                    select (SyntaxNode.TypeSpecifier)new SyntaxNode.TypeSpecifier.StructSpecifier(_2 ?? create_anon_tag_name(_1), _4, _2 != null),
                    from _1 in union_keyword
                    from _2 in IDENTIFIER.Option()
                    from _4 in struct_declaration_list.Option()
                    where _2 != null || _4 != null
                    select (SyntaxNode.TypeSpecifier)new SyntaxNode.TypeSpecifier.UnionSpecifier(_2 ?? create_anon_tag_name(_1), _4, _2 != null)
                )
            );

        public static readonly Parser<string> enumerator_name =
            Combinator.Trace("enumerator_name", IDENTIFIER );

        public static readonly Parser<SyntaxNode.TypeSpecifier.EnumSpecifier.Enumerator> enumerator =
            Combinator.Trace("enumerator",
                from _1 in enumerator_name
                from _2 in assign.Then(constant_expression).Option()
                select new SyntaxNode.TypeSpecifier.EnumSpecifier.Enumerator(_1, _2)
            );


        public static readonly Parser<IReadOnlyList<SyntaxNode.TypeSpecifier.EnumSpecifier.Enumerator>> enumerator_list =
            Combinator.Trace("enumerator_list",
                enumerator.Separate(comma, min: 1)
            );

        public static readonly Parser<SyntaxNode.TypeSpecifier.EnumSpecifier> enum_specifier =
            Combinator.Trace("enum_specifier",
                from _1 in enum_keyword
                from _2 in Combinator.Choice(
                    from _3 in IDENTIFIER
                    from _4 in Combinator.Option(
                        from _5 in left_brace
                        from _6 in enumerator_list
                        from _7 in comma.Option()
                        from _8 in right_brace
                        select new SyntaxNode.TypeSpecifier.EnumSpecifier(_3, _6, _7 != null, false)
                    )
                    select _4 ?? new SyntaxNode.TypeSpecifier.EnumSpecifier(_3, null, false, false)
                    ,
                    from _5 in left_brace
                    from _6 in enumerator_list
                    from _7 in comma.Option()
                    from _8 in right_brace
                    select new SyntaxNode.TypeSpecifier.EnumSpecifier(create_anon_tag_name(_1), _6, _7 != null, true)
                )
                select _2
            );


        public static readonly Parser<IReadOnlyList<SyntaxNode.TypeQualifierKind>> type_qualifier_list =
            Combinator.Trace("type_qualifier_list",
                type_qualifier.Many(1)
            ).Memoize();

        public static readonly Parser<SyntaxNode.DeclarationSpecifiers.FuntionSpecifierKind> function_specifier =
            Combinator.Trace("function_specifier",
                inline_keyword.Select(x => SyntaxNode.DeclarationSpecifiers.FuntionSpecifierKind.inline_keyword)
            );

        public static T eval<T>(Func<T> pred) {
            return pred();
        }

        public static readonly Parser<SyntaxNode.Declarator> declarator =
            Combinator.Trace("declarator",
                Combinator.Lazy(() =>
                    from _1 in pointer.Option()
                    from _2 in direct_declarator
                    select eval(() => {
                        _2.pointer = _1;
                        _2.full = true;
                        return _2;
                    })
                )
            ).Memoize();

        public static readonly Parser<Func<SyntaxNode.Declarator, SyntaxNode.Declarator>> direct_array_declarator =
            Combinator.Trace("direct_array_declarator",
                from _1 in left_bracket
                from _2 in Combinator.Choice(
                        from _5 in type_qualifier_list
                        from _6 in assignment_expression
                        select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => {
                            _6.full = true;
                            return new SyntaxNode.Declarator.ArrayDeclarator(x, _6);
                        })
                        ,
                        from _5 in type_qualifier_list
                        select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => {
                            return new SyntaxNode.Declarator.ArrayDeclarator(x, null);
                        })
                        ,
                        from _6 in assignment_expression
                        select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => {
                            _6.full = true;
                            return new SyntaxNode.Declarator.ArrayDeclarator(x, _6);
                        })
                        ,
                        from _5 in static_keyword
                        from _6 in type_qualifier_list
                        from _7 in assignment_expression
                        select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => {
                            _7.full = true;
                            return new SyntaxNode.Declarator.ArrayDeclarator(x, _7);
                        })
                        ,
                        from _5 in type_qualifier_list
                        from _6 in static_keyword
                        from _7 in assignment_expression
                        select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => {
                            _7.full = true;
                            return new SyntaxNode.Declarator.ArrayDeclarator(x, _7);
                        })
                        ,
                        from _5 in type_qualifier_list
                        from _6 in multiply
                        select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => {
                            return new SyntaxNode.Declarator.ArrayDeclarator(x, null);
                        })
                        ,
                        from _5 in multiply
                        select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => {
                            return new SyntaxNode.Declarator.ArrayDeclarator(x, null);
                        })
                        ,
                        from _5 in Combinator.Empty<Func<SyntaxNode.Declarator, SyntaxNode.Declarator>>(true)
                        select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => {
                            return new SyntaxNode.Declarator.ArrayDeclarator(x, null);
                        })
                    )
                from _8 in right_bracket
                select _2
            ).Memoize();

        public static readonly Parser<Func<SyntaxNode.Declarator, SyntaxNode.Declarator>> direct_function_declarator =
            Combinator.Trace("direct_function_declarator",
                from _1 in left_paren
                from _2 in Combinator.Choice(
                    from _3 in parameter_type_list
                    select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => new SyntaxNode.Declarator.FunctionDeclarator.AnsiFunctionDeclarator(x, _3))
                    ,
                    from _3 in identifier_list
                    select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => new SyntaxNode.Declarator.FunctionDeclarator.KandRFunctionDeclarator(x, _3))
                    ,
                    from _3 in Combinator.Empty<Func<SyntaxNode.Declarator, SyntaxNode.Declarator>>(true)
                    select (Func<SyntaxNode.Declarator, SyntaxNode.Declarator>)(x => new SyntaxNode.Declarator.FunctionDeclarator.AbbreviatedFunctionDeclarator(x))
                )
                from _4 in right_paren
                select _2
            ).Memoize();

        public static readonly Parser<SyntaxNode.Declarator> direct_declarator =
            Combinator.Trace("direct_declarator",
                from _1 in Combinator.Choice(
                    IDENTIFIER.Select(x => (SyntaxNode.Declarator)new SyntaxNode.Declarator.IdentifierDeclarator(x)),
                    Combinator.Quote(left_paren, declarator,right_paren).Select(x => (SyntaxNode.Declarator)new SyntaxNode.Declarator.GroupedDeclarator(x))
                )
                from _2 in Combinator.Choice(
                    direct_array_declarator,
                    direct_function_declarator
                ).Many()
                select _2.Aggregate(_1, (s, x) => x(s))
            ).Memoize();
#if false
        public static readonly Parser<IReadOnlyList<string>> pointer =
            Combinator.Trace("pointer",
                from _1 in multiply.Select(x => (IReadOnlyList<string>)new [] { x })
                from _2 in Combinator.Choice(
                    from __1 in type_qualifier_list
                    from __2 in Combinator.Lazy(() => pointer)
                    select new System.Runtime.CompilerServices.ReadOnlyCollectionBuilder<string>(_1.Concat(__1).Concat(__2)).ToReadOnlyCollection() as IReadOnlyList<string>
                    ,
                    from __1 in type_qualifier_list
                    select new System.Runtime.CompilerServices.ReadOnlyCollectionBuilder<string>(_1.Concat(__1)).ToReadOnlyCollection() as IReadOnlyList<string>
                    ,
                    from __1 in Combinator.Lazy(() => pointer)
                    select new System.Runtime.CompilerServices.ReadOnlyCollectionBuilder<string>(_1.Concat(__1)).ToReadOnlyCollection() as IReadOnlyList<string>
                    ,
                    Combinator.Empty<IReadOnlyList<string>>(true).Select(x => _1)
                )
                select _2
            ).Memoize();
#else
        // multiply+ (type_qualifier_list | multiply)*
        public static readonly Parser<IReadOnlyList<SyntaxNode.TypeQualifierKindWithPointer>> pointer =
            Combinator.Trace("pointer",
                from _1 in multiply.Select(x => SyntaxNode.TypeQualifierKindWithPointer.pointer_keyword).Many(1)
                from _2 in Combinator.Choice(
                    type_qualifier.Select(x => (SyntaxNode.TypeQualifierKindWithPointer)x),
                    multiply.Select(x => SyntaxNode.TypeQualifierKindWithPointer.pointer_keyword)
                ).Many()
                select new System.Runtime.CompilerServices.ReadOnlyCollectionBuilder<SyntaxNode.TypeQualifierKindWithPointer>(_1.Concat(_2)).ToReadOnlyCollection() as IReadOnlyList<SyntaxNode.TypeQualifierKindWithPointer>
            ).Memoize();
#endif

        public static readonly Parser<SyntaxNode.ParameterDeclaration> parameter_declaration =
            Combinator.Trace("parameter_declaration",
                
                from _1 in declaration_specifiers
                from _2 in Combinator.Choice(
                    declarator,
                    Combinator.Lazy(() => abstract_declarator).Select(x => (SyntaxNode.Declarator)x)
                ).Option()
                select new SyntaxNode.ParameterDeclaration(_1, _2)
            );

        public static readonly Parser<IReadOnlyList<SyntaxNode.ParameterDeclaration>> parameter_list =
            Combinator.Trace("parameter_list",
                parameter_declaration.Separate(comma, min: 1)
            );

        public static readonly Parser<SyntaxNode.ParameterTypeList> parameter_type_list =
            Combinator.Trace("parameter_type_list",
                from _1 in parameter_list
                from _2 in comma.Then(ellipsis).Option()
                select new SyntaxNode.ParameterTypeList(_1, _2 != null)
            );

        public static readonly Parser<IReadOnlyList<string>> identifier_list =
            Combinator.Trace("identifier_list",
                IDENTIFIER.Separate(comma, min: 1)
            );

        public static readonly Parser<SyntaxNode.Declarator.AbstractDeclarator> abstract_declarator =
            Combinator.Trace("abstract_declarator",
                Combinator.Lazy(() =>
                    Combinator.Choice(
                        from _1 in pointer
                        from _2 in direct_abstract_declarator.Option()
                        select (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.PointerAbstractDeclarator(_2, _1) { full = true },
                        from _2 in direct_abstract_declarator
                        select eval(() => {
                            _2.full = true;
                            return _2;
                        })
                    )
                )
            ).Memoize();

        public static readonly Parser<SyntaxNode.Declarator.AbstractDeclarator> direct_abstract_declarator_head =
            Combinator.Trace("direct_abstract_declarator_head",
                Combinator.Choice(
                    from _2 in left_paren
                    from _3 in Combinator.Choice(
                            abstract_declarator.Select(x => 
                                (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.GroupedAbstractDeclarator(x)
                            )
                            ,
                            parameter_type_list.Select(x => 
                                (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.FunctionAbstractDeclarator(null, x)
                            )
                            ,
                            Combinator.Empty<SyntaxNode.Declarator.AbstractDeclarator>(true).Select(x => 
                                (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.FunctionAbstractDeclarator(null, null)
                            )
                    )
                    from _4 in right_paren
                    select _3
                    ,
                    from _2 in left_bracket
                    from _3 in Combinator.Choice(
                                assignment_expression.Select(x => {
                                    x.full = true;
                                    return (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator(null, x);
                                })
                                ,
                                // c11ではさらに static, volatile, restrict, const が使えるようになる。
                                multiply.Select(x => 
                                    (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator(null, null)
                                )
                                ,
                                Combinator.Empty<SyntaxNode.Declarator.AbstractDeclarator>(true).Select(x => 
                                    (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator(null, null)
                                )
                    )
                    from _4 in right_bracket
                    select _3
                )
            ).Memoize();

        public static readonly Parser<Func<SyntaxNode.Declarator.AbstractDeclarator, SyntaxNode.Declarator.AbstractDeclarator>> direct_abstract_declarator_tail =
            Combinator.Trace("direct_abstract_declarator_tail",
                Combinator.Choice(
                    from _2 in left_paren
                    from _3 in Combinator.Choice(
                            parameter_type_list.Select(x =>
                                (Func<SyntaxNode.Declarator.AbstractDeclarator, SyntaxNode.Declarator.AbstractDeclarator>)( y => {
                                    return (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.FunctionAbstractDeclarator(y, x);
                                })
                            )
                            ,
                            Combinator.Empty<Func<SyntaxNode.Declarator.AbstractDeclarator, SyntaxNode.Declarator.AbstractDeclarator>>(true).Select(x =>
                                (Func<SyntaxNode.Declarator.AbstractDeclarator, SyntaxNode.Declarator.AbstractDeclarator>)( y => {
                                    return (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.FunctionAbstractDeclarator(y, null);
                                })
                            )
                    )
                    from _4 in right_paren
                    select _3
                    ,
                    from _2 in left_bracket
                    from _3 in Combinator.Choice(
                                assignment_expression.Select(x => 
                                    (Func<SyntaxNode.Declarator.AbstractDeclarator, SyntaxNode.Declarator.AbstractDeclarator>)(y => {
                                        x.full = true;
                                        return (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator(y, x);
                                    })
                                )
                                ,
                                multiply.Select(x =>
                                    (Func<SyntaxNode.Declarator.AbstractDeclarator, SyntaxNode.Declarator.AbstractDeclarator>)( y => {
                                        return (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator(y, null);
                                    })
                                )
                                ,
                                Combinator.Empty<Func<SyntaxNode.Declarator.AbstractDeclarator, SyntaxNode.Declarator.AbstractDeclarator>>(true).Select(x =>
                                    (Func<SyntaxNode.Declarator.AbstractDeclarator, SyntaxNode.Declarator.AbstractDeclarator>)(
                                        y => (SyntaxNode.Declarator.AbstractDeclarator)new SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator(y, null)
                                    )
                                )
                    )
                    from _4 in right_bracket
                    select _3
                )
            ).Memoize();

        public static readonly Parser<SyntaxNode.Declarator.AbstractDeclarator> direct_abstract_declarator =
            Combinator.Trace("direct_abstract_declarator",
                from _1 in direct_abstract_declarator_head
                from _2 in direct_abstract_declarator_tail.Many()
                select _2.Aggregate(_1, (s, x) => x(s))
            ).Memoize();


        public static readonly Parser<SyntaxNode.TypeName> type_name =
            Combinator.Trace("type_name",
                from _1 in specifier_qualifier_list
                from _2 in abstract_declarator.Option()
                select new SyntaxNode.TypeName(_1, _2)
            ).Memoize();

        public static readonly Parser<SyntaxNode.Initializer> initializer =
            Combinator.Trace("initializer",
                Combinator.Choice(
                    // 複合初期化
                    from _1 in left_brace
                    from _2 in (
                        from _3 in Combinator.Lazy(() => initializer_list)
                        from _4 in comma.Option()
                        select _3
                    ).Option()
                    from _5 in right_brace
                    select new SyntaxNode.Initializer(null, _2)
                    ,
                    // 普通の定数値初期化
                    assignment_expression.Select(x => {
                        x.full = true;
                        return new SyntaxNode.Initializer(x, null);
                    })
                )
            );

        public static readonly Parser<SyntaxNode.Initializer.Designator> designator =
            Combinator.Trace("designator",
                Combinator.Choice(
                    Combinator.Quote(left_bracket, constant_expression, right_bracket).Select(x => (SyntaxNode.Initializer.Designator)new SyntaxNode.Initializer.Designator.IndexDesignator(x)),
                    member_access.Then(IDENTIFIER).Select(x => (SyntaxNode.Initializer.Designator)new SyntaxNode.Initializer.Designator.MemberDesignator(x))
                )
            );

        public static readonly Parser<IReadOnlyList<SyntaxNode.Initializer.Designator>> designator_list =
            Combinator.Trace("designator_list",
                designator.Many(1)
            );

        public static readonly Parser<IReadOnlyList<SyntaxNode.Initializer.Designator>> designation =
            Combinator.Trace("designation",
                from _1 in designator_list
                from _2 in assign
                select _1
            );

        public static readonly Parser<IReadOnlyList<Tuple<IReadOnlyList<SyntaxNode.Initializer.Designator>, SyntaxNode.Initializer>>> initializer_list =
            Combinator.Trace("initializer_list", (
                    from _1 in designation.Option()
                    from _2 in initializer
                    select Tuple.Create(_1, _2)
                ).Separate(comma, min: 1)
            );




        //#
        //# Statements
        //#

        public static readonly Parser<SyntaxNode.Statement> statement =
            Combinator.Trace("statement",
                Combinator.Lazy(() => 
                    Combinator.Choice(
                        labeled_statement,
                        compound_statement,
                        expression_statement,
                        selection_statement,
                        iteration_statement,
                        jump_statement
                    )
                )
            );

        public static readonly Parser<string> label_name =
            Combinator.Trace("label_name", IDENTIFIER );

        public static readonly Parser<SyntaxNode.Statement> labeled_statement =
            Combinator.Trace("labeled_statement",
                Combinator.Choice(
                    from _1 in label_name
                    from _2 in colon
                    from _3 in statement
                    select (SyntaxNode.Statement) new SyntaxNode.Statement.LabeledStatement.GenericLabeledStatement(_1, _3),
                    from _1 in case_keyword
                    from _2 in constant_expression
                    from _3 in colon
                    from _4 in statement
                    select (SyntaxNode.Statement) new SyntaxNode.Statement.LabeledStatement.CaseLabeledStatement(_2, _4),
                    from _1 in default_keyword
                    from _3 in colon
                    from _4 in statement
                    select (SyntaxNode.Statement) new SyntaxNode.Statement.LabeledStatement.DefaultLabeledStatement(_4)
                )
            );

        public static readonly Parser<SyntaxNode> block_item =
            Combinator.Trace("block_item",
                Combinator.Choice(
                    declaration.Select(x => (SyntaxNode)x),
                    statement.Select(x => (SyntaxNode)x)
                    //local_function_definition.Select(x => (SyntaxNode)x)
                )
            );

        public static readonly Parser<IReadOnlyList<SyntaxNode>> block_item_list =
            Combinator.Trace("block_item_list",
                from _1 in Combinator.Action(x => {
                    var ps = (ParserStatus)x;
                    return ps.PushScope();
                })
                from _2 in block_item.Many(1)
                from _3 in Combinator.Action(x => {
                    var ps = (ParserStatus)x;
                    return ps.PopScope();
                })
                select _2

            );

        public static readonly Parser<SyntaxNode.Statement> compound_statement =
            Combinator.Trace("compound_statement",
                from _1 in left_brace
                from _2 in block_item_list.Option()
                from _3 in right_brace
                select (SyntaxNode.Statement)new SyntaxNode.Statement.CompoundStatement(_2)
            );


        public static readonly Parser<SyntaxNode.Statement> expression_statement =
            Combinator.Trace("expression_statement",
                from _1 in (
                    from __1 in expression
                    select eval(() => {
                        __1.full = true;
                        return __1;
                    })
                ).Option()
                from _2 in semicolon
                select (SyntaxNode.Statement) new SyntaxNode.Statement.ExpressionStatement(_1)
            );

        public static readonly Parser<SyntaxNode.Statement> selection_statement =
            Combinator.Trace("selection_statement",
                Combinator.Choice(
                    from _1 in if_keyword
                    from _2 in left_paren
                    from _3 in expression.Select(x => {
                        x.full = true;
                        return x;
                    })
                    from _4 in right_paren
                    from _5 in statement
                    from _6 in else_keyword.Then(statement).Option()
                    select (SyntaxNode.Statement) new SyntaxNode.Statement.SelectionStatement.IfStatement(_3, _5, _6),
                    from _1 in switch_keyword
                    from _2 in left_paren
                    from _3 in expression.Select(x => {
                        x.full = true;
                        return x;
                    })
                    from _4 in right_paren
                    from _5 in statement
                    select (SyntaxNode.Statement) new SyntaxNode.Statement.SelectionStatement.SwitchStatement(_3, _5)
                )
            );

        public static readonly Parser<SyntaxNode.Statement> iteration_statement =
            Combinator.Trace("iteration_statement",
                Combinator.Choice(
                    from _1 in while_keyword
                    from _2 in left_paren
                    from _3 in expression.Select(x => {
                        x.full = true;
                        return x;
                    })
                    from _4 in right_paren
                    from _5 in statement
                    select (SyntaxNode.Statement) new SyntaxNode.Statement.IterationStatement.WhileStatement(_3, _5)
                    ,
                    from _1 in do_keyword
                    from _2 in statement
                    from _3 in while_keyword
                    from _4 in left_paren
                    from _5 in expression.Select(x => {
                        x.full = true;
                        return x;
                    })
                    from _6 in right_paren
                    from _7 in semicolon
                    select (SyntaxNode.Statement) new SyntaxNode.Statement.IterationStatement.DoStatement(_2, _5)
                    ,
                    
                    from _1 in for_keyword
                    from _2 in left_paren
                    from _3 in Combinator.Choice(
                        from __1 in declaration
                        from __2 in expression_statement
                        from __3 in expression.Select(x => {
                            x.full = true;
                            return x;
                        }).Option()
                        from __4 in right_paren
                        from __5 in statement
                        select (SyntaxNode.Statement)new SyntaxNode.Statement.IterationStatement.C99ForStatement(__1, __2, __3, __5)
                        ,
                        from __1 in expression_statement
                        from __2 in expression_statement
                        from __3 in expression.Select(x => {
                            x.full = true;
                            return x;
                        }).Option()
                        from __4 in right_paren
                        from __5 in statement
                        select (SyntaxNode.Statement)new SyntaxNode.Statement.IterationStatement.ForStatement(__1, __2, __3, __5)
                    )
                    select _3
                )
            );

        public static readonly Parser<SyntaxNode.Statement> jump_statement =
            Combinator.Trace("jump_statement",
                Combinator.Choice(
                    from _1 in goto_keyword
                    from _2 in label_name
                    from _3 in semicolon
                    select (SyntaxNode.Statement) new SyntaxNode.Statement.JumpStatement.GotoStatement(_2),
                    //from _1 in goto_keyword
                    //from _2 in multiply
                    //from _3 in label_name
                    //from _4 in semicolon
                    //select (Statement)new ErrorStatement(_2,_3),
                    from _1 in continue_keyword
                    from _2 in semicolon
                    select (SyntaxNode.Statement) new SyntaxNode.Statement.JumpStatement.ContinueStatement(),
                    from _1 in break_keyword
                    from _2 in semicolon
                    select (SyntaxNode.Statement) new SyntaxNode.Statement.JumpStatement.BreakStatement(),
                    from _1 in return_keyword
                    from _2 in expression.Select(x => {
                        x.full = true;
                        return x;
                    }).Option()
                    from _3 in semicolon
                    select (SyntaxNode.Statement) new SyntaxNode.Statement.JumpStatement.ReturnStatement(_2)
                )
            );

        //#
        //# External definitions
        //#

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

        public static readonly Parser<IReadOnlyList<SyntaxNode.Declaration>> declaration_list =
            Combinator.Trace("declaration_list",
                declaration.Many(1)
            );

        public static readonly Parser<SyntaxNode.Definition.FunctionDefinition> function_definition =
            Combinator.Trace("function_definition",
                from _1 in declaration_specifiers.Option()
                from _2 in declarator
                from _3 in declaration_list.Option()
                from _4 in compound_statement
                select (_3 != null) 
                    ? (SyntaxNode.Definition.FunctionDefinition) new SyntaxNode.Definition.FunctionDefinition.KandRFunctionDefinition(_1, _2, _3.ToList(), _4)
                    : (_2 is SyntaxNode.Declarator.FunctionDeclarator.AnsiFunctionDeclarator)
                    ? (SyntaxNode.Definition.FunctionDefinition) new SyntaxNode.Definition.FunctionDefinition.AnsiFunctionDefinition(_1, _2, _4)
                    : (_2 is SyntaxNode.Declarator.FunctionDeclarator.KandRFunctionDeclarator)
                    ? (SyntaxNode.Definition.FunctionDefinition) new SyntaxNode.Definition.FunctionDefinition.KandRFunctionDefinition(_1, _2, new List<SyntaxNode.Declaration>(), _4)
                    : (_2 is SyntaxNode.Declarator.FunctionDeclarator.AbbreviatedFunctionDeclarator)
                    ? (SyntaxNode.Definition.FunctionDefinition) new SyntaxNode.Definition.FunctionDefinition.AnsiFunctionDefinition(_1, _2, _4)
                    : null
            );


        public static readonly Parser<SyntaxNode> external_declaration =
            Combinator.Trace("external_declaration",
                Combinator.Choice(
                    function_definition.Select(x => (SyntaxNode)x),
                    global_declaration.Select(x => (SyntaxNode)x)
                )
            );

        public static readonly Parser<SyntaxNode.TranslationUnit> translation_unit =
            Combinator.Trace("translation_unit",
                from _1 in external_declaration.Many(1)
                select new SyntaxNode.TranslationUnit(_1)
            );


#endregion

        public static T Parse<T>(TextReader reader, Func<bool, SyntaxNode.TranslationUnit, Position, Position,T> pred) {
            var target = new Context(new Source("", reader));
            var parser = (
                from _1 in translation_unit
                from _2 in isspaces
                from _3 in Combinator.EoF()
                select _1
            );
            var ret = parser(target, Position.Empty, new ParserStatus());
            return pred(ret.Success, ret.Value, ret.Position, target.failedPosition);
        }
    }

}