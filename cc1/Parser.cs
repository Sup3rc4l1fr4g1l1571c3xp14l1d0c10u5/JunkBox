using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {
    /// <summary>
    /// パーサ
    /// </summary>
    public class Parser {
        /// <summary>
        /// 名前空間(ステートメント ラベル)
        /// </summary>
        private Scope<SyntaxTree.Statement.GenericLabeledStatement> label_scope = Scope<SyntaxTree.Statement.GenericLabeledStatement>.Empty.Extend();

        /// <summary>
        /// 名前空間(構造体、共用体、列挙体のタグ名)
        /// </summary>
        private Scope<CType.TaggedType> _tagScope = Scope<CType.TaggedType>.Empty.Extend();

        /// <summary>
        /// 名前空間(通常の識別子（変数、関数、引数、列挙定数)
        /// </summary>
        private Scope<IdentifierScopeValue> _identScope = Scope<IdentifierScopeValue>.Empty.Extend();

        /// <summary>
        /// 名前空間(Typedef名)
        /// </summary>
        private Scope<SyntaxTree.Declaration.TypeDeclaration> _typedefScope = Scope<SyntaxTree.Declaration.TypeDeclaration>.Empty.Extend();

        // 構造体または共用体のメンバーについてはそれぞれの宣言オブジェクトに付与される

        /// <summary>
        /// break命令についてのスコープ
        /// </summary>
        private readonly Stack<SyntaxTree.Statement> _breakScope = new Stack<SyntaxTree.Statement>();

        /// <summary>
        /// continue命令についてのスコープ
        /// </summary>
        private readonly Stack<SyntaxTree.Statement> _continueScope = new Stack<SyntaxTree.Statement>();

        //
        // lex spec
        //


        private Lexer lexer {
            get;
        }


        public Parser(string s) {
            lexer = new Lexer(s, "<built-in>");

            // GCCの組み込み型の設定
            _typedefScope.Add("__builtin_va_list", new SyntaxTree.Declaration.TypeDeclaration("__builtin_va_list", CType.CreatePointer(new CType.BasicType(TypeSpecifier.Void))));

        }


        public void Parse() {
            var ret = translation_unit();
            Console.WriteLine(Cell.PrettyPrint(ret.Accept(new SyntaxTreeDumpVisitor(), null)));
        }


        private void EoF() {
            if (!lexer.is_eof()) {
                throw new Exception();
            }
        }




        private bool is_ENUMERATION_CONSTANT() {
            if (!is_IDENTIFIER(false)) {
                return false;
            }
            var ident = lexer.current_token();
            IdentifierScopeValue v;
            if (_identScope.TryGetValue(ident.Raw, out v) == false) {
                return false;
            }
            return (v as IdentifierScopeValue.EnumValue)?.ParentType.Members.First(x => x.Ident == ident.Raw) != null;
        }

        private CType.TaggedType.EnumType.MemberInfo ENUMERATION_CONSTANT() {
            var ident = IDENTIFIER(false);
            IdentifierScopeValue v;
            if (_identScope.TryGetValue(ident, out v) == false) {
                throw new Exception();
            }
            if (!(v is IdentifierScopeValue.EnumValue)) {
                throw new Exception();
            }
            var el = ((IdentifierScopeValue.EnumValue)v).ParentType.Members.First(x => x.Ident == ident);
            return el;
        }

        private bool is_CHARACTER_CONSTANT() {
            return lexer.current_token().Kind == Token.TokenKind.STRING_CONSTANT;
        }

        private string CHARACTER_CONSTANT() {
            if (is_CHARACTER_CONSTANT() == false) {
                throw new Exception();
            }
            var ret = lexer.current_token().Raw;
            lexer.next_token();
            return ret;
        }

        private bool is_FLOATING_CONSTANT() {
            return lexer.current_token().Kind == Token.TokenKind.FLOAT_CONSTANT;
        }
        private SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant FLOATING_CONSTANT() {
            if (is_FLOATING_CONSTANT() == false) {
                throw new Exception();
            }
            var raw = lexer.current_token().Raw;
            var m = Lexer.ParseFloat(raw);
            var value = Convert.ToDouble(m.Item1);
            CType.BasicType.TypeKind type;
            switch (m.Item2) {
                case "F":
                    type = CType.BasicType.TypeKind.Float;
                    break;
                case "L":
                    type = CType.BasicType.TypeKind.LongDouble;
                    break;
                case "":
                    type = CType.BasicType.TypeKind.Double;
                    break;
                default:
                    throw new Exception();
            }
            lexer.next_token();
            return new SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant(raw, value, type);
        }

        private bool is_INTEGER_CONSTANT() {
            return lexer.current_token().Kind == Token.TokenKind.HEXIMAL_CONSTANT | lexer.current_token().Kind == Token.TokenKind.OCTAL_CONSTANT | lexer.current_token().Kind == Token.TokenKind.DECIAML_CONSTANT;
        }
        private SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant INTEGER_CONSTANT() {
            if (is_INTEGER_CONSTANT() == false) {
                throw new Exception();
            }
            string raw = lexer.current_token().Raw;
            string body;
            string suffix;
            int radix;
            CType.BasicType.TypeKind[] candidates;

            switch (lexer.current_token().Kind) {
                case Token.TokenKind.HEXIMAL_CONSTANT: {
                        var m = Lexer.ParseHeximal(raw);
                        body = m.Item1;
                        suffix = m.Item2;
                        radix = 16;
                        switch (suffix) {
                            case "LLU":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "LL":
                                candidates = new[] { CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "LU":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "L":
                                candidates = new[] { CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "U":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "":
                                candidates = new[] { CType.BasicType.TypeKind.SignedInt, CType.BasicType.TypeKind.UnsignedInt, CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            default:
                                throw new Exception();
                        }

                        break;
                    }
                case Token.TokenKind.OCTAL_CONSTANT: {
                        var m = Lexer.ParseOctal(raw);
                        body = m.Item1;
                        suffix = m.Item2;
                        radix = 8;
                        switch (suffix) {
                            case "LLU":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "LL":
                                candidates = new[] { CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "LU":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "L":
                                candidates = new[] { CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "U":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "":
                                candidates = new[] { CType.BasicType.TypeKind.SignedInt, CType.BasicType.TypeKind.UnsignedInt, CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            default:
                                throw new Exception();
                        }
                        break;
                    }
                case Token.TokenKind.DECIAML_CONSTANT: {
                        var m = Lexer.ParseDecimal(raw);
                        body = m.Item1;
                        suffix = m.Item2;
                        radix = 10;
                        switch (suffix) {
                            case "LLU":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "LL":
                                candidates = new[] { CType.BasicType.TypeKind.SignedLongLongInt };
                                break;
                            case "UL":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "L":
                                candidates = new[] { CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt };
                                break;
                            case "U":
                                candidates = new[] { CType.BasicType.TypeKind.UnsignedInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt };
                                break;
                            case "":
                                candidates = new[] { CType.BasicType.TypeKind.SignedInt, CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt };
                                break;
                            default:
                                throw new Exception();
                        }
                        break;
                    }
                default:
                    throw new Exception();

            }

            var originalSigned = Convert.ToInt64(body, radix);
            var originalUnsigned = Convert.ToUInt64(body, radix);
            Int64 value = 0;

            CType.BasicType.TypeKind selectedType = 0;
            System.Diagnostics.Debug.Assert(candidates.Length > 0);
            foreach (var candidate in candidates) {
                switch (candidate) {
                    case CType.BasicType.TypeKind.SignedInt: {
                            var v = Convert.ToInt32(body, radix);
                            if (v == originalSigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.TypeKind.UnsignedInt: {
                            var v = Convert.ToUInt32(body, radix);
                            if (v == originalUnsigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.TypeKind.SignedLongInt: {
                            var v = Convert.ToInt32(body, radix);
                            if (v == originalSigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.TypeKind.UnsignedLongInt: {
                            var v = Convert.ToUInt32(body, radix);
                            if (v == originalUnsigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.TypeKind.SignedLongLongInt: {
                            var v = Convert.ToInt64(body, radix);
                            if (v == originalSigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    case CType.BasicType.TypeKind.UnsignedLongLongInt: {
                            var v = Convert.ToUInt64(body, radix);
                            if (v == originalUnsigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    default:
                        throw new Exception();
                }
                selectedType = candidate;
                break;
            }

            lexer.next_token();

            return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(raw, value, selectedType);

        }

        private bool is_STRING() {
            return lexer.current_token().Kind == Token.TokenKind.STRING_LITERAL;
        }
        private string STRING() {
            if (is_STRING() == false) {
                throw new Exception();
            }
            var ret = lexer.current_token().Raw;
            lexer.next_token();
            return ret;
        }

        //
        // Grammers
        //


        /// <summary>
        /// 6.9 外部定義(翻訳単位)
        /// </summary>
        /// <returns></returns>
        public SyntaxTree.TranslationUnit translation_unit() {
            var ret = new SyntaxTree.TranslationUnit();
            while (is_external_declaration(null, TypeSpecifier.None)) {
                ret.declarations.AddRange(external_declaration());
            }
            EoF();
            return ret;
        }

        /// <summary>
        /// 6.9 外部定義(外部宣言となりえるか？)
        /// </summary>
        /// <returns></returns>
        private bool is_external_declaration(CType baseType, TypeSpecifier typeSpecifier) {
            return (is_declaration_specifier(baseType, typeSpecifier) || lexer.Peek(';') || is_declarator());
        }

        /// <summary>
        /// 6.9 外部定義(外部宣言)
        /// </summary>
        /// <returns></returns>
        private List<SyntaxTree.Declaration> external_declaration() {
            CType baseType = null;
            StorageClassSpecifier storageClass = StorageClassSpecifier.None;
            TypeSpecifier typeSpecifier = TypeSpecifier.None;
            TypeQualifier typeQualifier = TypeQualifier.None;
            FunctionSpecifier functionSpecifier = FunctionSpecifier.None;
            while (is_declaration_specifier(baseType, typeSpecifier)) {
                declaration_specifier(ref baseType, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier);
            }
            if (typeSpecifier != TypeSpecifier.None) {
                if (baseType != null) {
                    throw new Exception("");
                } else {
                    baseType = new CType.BasicType(typeSpecifier);
                }
            } else if (baseType == null) {
                baseType = new CType.BasicType(TypeSpecifier.None);
            }
            baseType = baseType.WrapTypeQualifier(typeQualifier);

            var ret = new List<SyntaxTree.Declaration>();


            if (!is_declarator()) {
                if (!baseType.IsStructureType() && !baseType.IsEnumeratedType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "空の宣言は使用できません。");
                } else if (baseType.IsStructureType() && (baseType.Unwrap() as CType.TaggedType.StructUnionType).IsAnonymous) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "無名構造体/共用体が宣言されていますが、そのインスタンスを定義していません。");
                } else {
                    lexer.Read(';');
                    return ret;
                }
            } else {
                for (; ; ) {
                    string ident = "";
                    List<CType> stack = new List<CType>() { new CType.StubType() };
                    declarator(ref ident, stack, 0);
                    var type = CType.Resolve(baseType, stack);
                    if (lexer.Peek('=', ',', ';')) {
                        // 宣言

                        SyntaxTree.Declaration decl = func_or_var_or_typedef_declaration(ident, type, storageClass, functionSpecifier);

                        ret.Add(decl);


                        if (lexer.ReadIf(',')) {
                            continue;
                        }
                        break;
                    } else if (type.IsFunctionType()) {
                        ret.Add(function_definition(ident, type.Unwrap() as CType.FunctionType, storageClass, functionSpecifier));
                        return ret;
                    } else {
                        throw new Exception("");
                    }

                }
                lexer.Read(';');
                return ret;
            }

        }

        /// <summary>
        /// 6.9.1　関数定義
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <returns></returns>
        /// <remarks>
        /// 制約
        /// - 関数定義で宣言する識別子（その関数の名前）の型が関数型であることは，その関数定義の宣言子 の部分で指定しなければならない
        /// - 関数の返却値の型は，配列型以外のオブジェクト型又は void 型でなければならない。
        /// - 宣言指定子列の中に記憶域クラス指定子がある場合，それは extern 又は static のいずれかでなければならない。
        /// - 宣言子が仮引数型並びを含む場合，それぞれの仮引数の宣言は識別子を含まなければならない。
        ///   ただし，仮引数型並びが void 型の仮引数一つだけから成る特別な場合を除く。この場合は，識別子があってはならず，更に宣言子の後ろに宣言並びが続いてはならない。
        /// </remarks>
        private SyntaxTree.Declaration function_definition(string ident, CType.FunctionType type, StorageClassSpecifier storageClass, FunctionSpecifier functionSpecifier) {

            // K&Rにおける宣言並びがある場合は読み取る。
            var argmuents = is_declaration() ? declaration() : null;

            // 宣言並びがある場合は仮引数宣言を検証
            if (argmuents != null) {
                foreach (var arg in argmuents) {
                    if (!(arg is SyntaxTree.Declaration.VariableDeclaration)) {
                        throw new Exception("古いスタイルの関数宣言における宣言並び中に仮引数宣言以外がある");
                    }
                    if ((arg as SyntaxTree.Declaration.VariableDeclaration).Init != null) {
                        throw new Exception("古いスタイルの関数宣言における仮引数宣言が初期化式を持っている。");
                    }
                    if ((arg as SyntaxTree.Declaration.VariableDeclaration).StorageClass != StorageClassSpecifier.Register && (arg as SyntaxTree.Declaration.VariableDeclaration).StorageClass != StorageClassSpecifier.None) {
                        throw new Exception("古いスタイルの関数宣言における仮引数宣言が、register 以外の記憶クラス指定子を伴っている。");
                    }
                }
            }

            if (type.Arguments == null) {
                // 識別子並び・仮引数型並びなし
                if (argmuents != null) {
                    throw new Exception("K&R形式の関数定義だが、識別子並びが空なのに、宣言並びがある");
                } else {
                    // ANSIにおける引数をとらない関数(void)
                    // 警告を出すこと。
                    type.Arguments = new CType.FunctionType.ArgumentInfo[0];
                }
            } else if (type.Arguments.Any(x => (x.Type as CType.BasicType)?.Kind == CType.BasicType.TypeKind.KAndRImplicitInt)) {

                // ANSI形式の仮引数並びとの共存は不可能
                if (type.Arguments.Any(x => (x.Type as CType.BasicType)?.Kind != CType.BasicType.TypeKind.KAndRImplicitInt)) {
                    throw new Exception("関数定義中でK&R形式の識別子並びとANSI形式の仮引数型並びが混在している");
                }


                // 標準化前のK＆R初版においては、引数には規定の実引数拡張が適用され、char, short型はintに、float型は double型に拡張される。つまり、引数として渡せる整数型はint/小数型はdoubleのみ。
                //  -> int f(x,y,z) char x; float y; short* z; {...} は int f(int x, double y, short *z) { ... } になる(short*はポインタ型なので拡張されない)
                //
                // gccは非標準拡張として関数プロトタイプ宣言の後に同じ型のK&R型の関数定義が登場すると、プロトタイプ宣言を使ってK&Rの関数定義を書き換える。（"info gcc" -> "C Extension" -> "Function Prototypes"）
                //  -> int f(char, float, short*); が事前にあると int f(x,y,z) char x; float y; short* z; {...} は int f(char x, float y, short* z) { ... } になる。（プロトタイプが無い場合は従来通り？）
                // 
                // これらより、
                // K&R形式の仮引数定義の場合、規定の実引数拡張前後で型が食い違う引数宣言はエラーにする。

                // 紛らわしい例の場合
                // int f();
                // void foo(void) { f(3.14f); }
                // int f (x) floar x; { ... }
                //
                // int f(); 引数の情報がない関数のプロトタイプなので、実引数には規定の実引数拡張が適用され、引数として渡せる整数型はint/小数型はdoubleのみ。
                // f(3.14f); は引数の情報がない関数のプロトタイプなので引数の型・数はチェックせず、既定の実引数拡張により引数はdouble型に変換される。（規定の実引数拡張で型が変化するなら警告を出したほうがいいよね）
                // int f(x) float x; {...} は 規定の実引数拡張により inf f(x) double x; {... }相当となる。（ので警告出したほうがいいよね）
                // なので、全体でみると型の整合性はとれている。

                // 関数原型を含まない型で関数を定義し，かつ拡張後の実引数の型が，拡張後の仮引数の型と適合しない場合，その動作は未定義とする。
                // 上の例でf(1) とすると、呼び出し側は引数をint型で渡すのに、受け取り側はdoubleで受け取るためバグの温床になる。
                // 自動検査するには型推論するしかない


                // K&R形式の識別子並びに宣言並びの型情報を規定の実引数拡張を伴って反映させる。
                // 宣言並びを名前引きできる辞書に変換
                var dic = argmuents.Cast<SyntaxTree.Declaration.VariableDeclaration>().ToDictionary(x => x.Ident, x => x);
                // 型宣言側の仮引数
                var mapped = type.Arguments.Select(x => {
                    if (dic.ContainsKey(x.Ident)) {
                        var dapType = dic[x.Ident].Type.DefaultArgumentPromotion();
                        if (CType.IsEqual(dapType, dic[x.Ident].Type) == false) {
                            throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"{ident}は規定の実引数拡張で型が変化します。");
                        }
                        return new CType.FunctionType.ArgumentInfo(x.Ident, x.StorageClass, dic[x.Ident].Type.DefaultArgumentPromotion());
                    } else {
                        return new CType.FunctionType.ArgumentInfo(x.Ident, x.StorageClass, CType.CreateSignedInt().DefaultArgumentPromotion());
                    }
                }).ToList();



                type.Arguments = mapped.ToArray();

            } else {
                // ANSI形式の仮引数型並びのみなので何もしない
            }

            // 関数が定義済みの場合は、再定義のチェックを行う
            IdentifierScopeValue iv;
            if (_identScope.TryGetValue(ident, out iv)) {
                if (iv.IsFunction() == false) {
                    throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"{ident}は既に関数型以外で宣言済み");
                }
                if ((iv.ToFunction().Ty as CType.FunctionType).Arguments != null) {
                    if (CType.IsEqual(iv.ToFunction().Ty, type) == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, "再定義型の不一致");
                    }
                } else {
                    // 仮引数が省略されているため、引数の数や型はチェックしない
                    Console.WriteLine($"仮引数が省略されて宣言された関数 {ident} の実態を宣言しています。");
                }
                if (iv.ToFunction().Body != null) {
                    throw new Exception("関数はすでに本体を持っている。");
                }

            }
            var funcdecl = new SyntaxTree.Declaration.FunctionDeclaration(ident, type, storageClass, functionSpecifier);

            // 環境に名前を追加
            _identScope.Add(ident, new IdentifierScopeValue.Declaration(funcdecl));

            // 各スコープを積む
            _tagScope = _tagScope.Extend();
            _typedefScope = _typedefScope.Extend();
            _identScope = _identScope.Extend();

            if (type.Arguments != null) {
                foreach (var arg in type.Arguments) {
                    _identScope.Add(arg.Ident, new IdentifierScopeValue.Declaration(new SyntaxTree.Declaration.ArgumentDeclaration(arg.Ident, arg.Type, arg.StorageClass)));
                }
            }

            // 関数本体（複文）を解析
            funcdecl.Body = compound_statement();

            //各スコープから出る
            _identScope = _identScope.Parent;
            _typedefScope = _typedefScope.Parent;
            _tagScope = _tagScope.Parent;

            return funcdecl;
        }

        /// <summary>
        /// 6.9.2　外部オブジェクト定義、もしくは、宣言
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <param name="functionSpecifier"></param>
        /// <returns></returns>
        private SyntaxTree.Declaration func_or_var_or_typedef_declaration(string ident, CType type, StorageClassSpecifier storageClass, FunctionSpecifier functionSpecifier) {
            SyntaxTree.Declaration decl;

            if (functionSpecifier != FunctionSpecifier.None) {
                throw new Exception("inlineは関数定義に対してのみ使える。");
            }
            if (storageClass == StorageClassSpecifier.Auto || storageClass == StorageClassSpecifier.Register) {
                throw new Exception("宣言に対して利用できない記憶クラス指定子が指定されている。");
            }


            if (lexer.ReadIf('=')) {
                // 初期化式を伴うので、初期化付きの変数宣言

                if (storageClass == StorageClassSpecifier.Typedef || storageClass == StorageClassSpecifier.Auto || storageClass == StorageClassSpecifier.Register) {
                    throw new Exception("変数宣言には指定できない記憶クラス指定子が指定されている。");
                }

                if (type.IsFunctionType()) {
                    throw new Exception("関数宣言に初期値を指定している");
                }
                if (ident == "tktk") {

                }
                var init = initializer(type);
                decl = new SyntaxTree.Declaration.VariableDeclaration(ident, type, storageClass, init);
                // 環境に初期値付き変数を追加
                _identScope.Add(ident, new IdentifierScopeValue.Declaration(decl));
            } else {
                // 初期化式を伴わないため、関数宣言、変数宣言、Typedef宣言のどれか

                if (storageClass == StorageClassSpecifier.Auto || storageClass == StorageClassSpecifier.Register) {
                    throw new Exception("ファイル有効範囲での関数宣言、変数宣言、Typedef宣言で指定できない記憶クラス指定子が指定されている。");
                }

                CType.FunctionType ft;
                if (type.IsFunctionType(out ft) && ft.Arguments != null) {
                    // 6.7.5.3 関数宣言子（関数原型を含む）
                    // 関数定義の一部でない関数宣言子における識別子並びは，空でなければならない。
                    // 脚注　関数宣言でK&Rの関数定義のように int f(a,b,c); と書くことはダメということ。int f(); ならOK
                    // K&R の記法で宣言を記述した場合、引数のTypeはnull
                    // ANSIの記法で宣言を記述した場合、引数のTypeは非null
                    if (ft.Arguments.Any(x => x.Type == null)) {
                        throw new Exception("関数定義の一部でない関数宣言子における識別子並びは，空でなければならない。");
                    }
                }

                if (storageClass == StorageClassSpecifier.Typedef) {
                    // typedef 宣言
                    SyntaxTree.Declaration.TypeDeclaration tdecl;
                    bool current;
                    if (_typedefScope.TryGetValue(ident, out tdecl, out current)) {
                        if (current == true) {
                            throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "型が再定義された。（型の再定義はC11以降の機能。）");
                        }
                    }
                    tdecl = new SyntaxTree.Declaration.TypeDeclaration(ident, type);
                    decl = tdecl;
                    _typedefScope.Add(ident, tdecl);
                } else if (type.IsFunctionType()) {
                    // 関数宣言
                    IdentifierScopeValue iv;
                    if (_identScope.TryGetValue(ident, out iv)) {
                        if (iv.IsFunction() == false) {
                            throw new Exception("関数型以外で宣言済み");
                        }
                        if (CType.IsEqual(iv.ToFunction().Ty, type) == false) {
                            throw new Exception("再定義型の不一致");
                        }
                        // Todo: 型の合成
                    }
                    decl = new SyntaxTree.Declaration.FunctionDeclaration(ident, type, storageClass, functionSpecifier);
                    _identScope.Add(ident, new IdentifierScopeValue.Declaration(decl));
                } else {
                    // 変数宣言
                    IdentifierScopeValue iv;
                    if (_identScope.TryGetValue(ident, out iv)) {
                        if (iv.IsVariable() == false) {
                            throw new Exception("変数型以外で宣言済み");
                        }
                        if (CType.IsEqual(iv.ToVariable().Type, type) == false) {
                            throw new Exception("再定義型の不一致");
                        }
                        // Todo: 型の合成
                    }
                    decl = new SyntaxTree.Declaration.VariableDeclaration(ident, type, storageClass, null);
                    _identScope.Add(ident, new IdentifierScopeValue.Declaration(decl));
                }
            }
            return decl;
        }

        /// <summary>
        /// 6.7 宣言となりうるか？
        /// </summary>
        /// <returns></returns>
        private bool is_declaration() {
            return is_declaration_specifiers(null, TypeSpecifier.None);
        }

        /// <summary>
        /// 6.7 宣言(宣言)
        /// </summary>
        /// <returns></returns>
        private List<SyntaxTree.Declaration> declaration() {

            // 宣言指定子列 
            StorageClassSpecifier storageClass;
            CType baseType = declaration_specifiers(out storageClass);

            // 初期化宣言子並び
            List<SyntaxTree.Declaration> decls = null;
            if (!lexer.Peek(';')) {
                // 一つ以上の初期化宣言子
                decls = new List<SyntaxTree.Declaration>();
                decls.Add(init_declarator(baseType, storageClass));
                while (lexer.ReadIf(',')) {
                    decls.Add(init_declarator(baseType, storageClass));
                }
            }
            lexer.Read(';');
            return decls;
        }

        /// <summary>
        /// 6.7 宣言(宣言指定子列になりうるか)
        /// </summary>
        /// <param name="sc"></param>
        /// <returns></returns>
        private bool is_declaration_specifiers(CType type, TypeSpecifier typeSpecifier) {
            return is_declaration_specifier(type, typeSpecifier);
        }

        /// <summary>
        /// 6.7 宣言(宣言指定子列)
        /// </summary>
        /// <param name="sc"></param>
        /// <returns></returns>
        private CType declaration_specifiers(out StorageClassSpecifier sc) {
            CType baseType = null;
            StorageClassSpecifier storageClass = StorageClassSpecifier.None;
            TypeSpecifier typeSpecifier = TypeSpecifier.None;
            TypeQualifier typeQualifier = TypeQualifier.None;
            FunctionSpecifier functionSpecifier = FunctionSpecifier.None;

            declaration_specifier(ref baseType, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier);

            while (is_declaration_specifier(baseType, typeSpecifier)) {
                declaration_specifier(ref baseType, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier);
            }

            if (functionSpecifier != FunctionSpecifier.None) {
                throw new Exception("inlineは関数定義でのみ使える。");
            }

            if (typeSpecifier != TypeSpecifier.None) {
                if (baseType != null) {
                    throw new Exception("");
                } else {
                    baseType = new CType.BasicType(typeSpecifier);
                }
            } else if (baseType == null) {
                baseType = new CType.BasicType(TypeSpecifier.None);
            }
            sc = storageClass;

            baseType = baseType.WrapTypeQualifier(typeQualifier);
            return baseType;
        }

        /// <summary>
        /// 6.7 宣言(宣言指定子要素になりうるか)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeSpecifier"></param>
        /// <returns></returns>
        private bool is_declaration_specifier(CType type, TypeSpecifier typeSpecifier) {
            return (is_storage_class_specifier() ||
                (is_type_specifier() && type == null) ||
                (is_struct_or_union_specifier() && type == null) ||
                (is_enum_specifier() && type == null) ||
                (is_TYPEDEF_NAME() && type == null && typeSpecifier == TypeSpecifier.None) ||
                is_type_qualifier() ||
                is_function_specifier());
        }

        /// <summary>
        /// 6.7 宣言(宣言指定子要素)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <param name="typeSpecifier"></param>
        /// <param name="typeQualifier"></param>
        /// <param name="functionSpecifier"></param>
        private void declaration_specifier(ref CType type, ref StorageClassSpecifier storageClass, ref TypeSpecifier typeSpecifier, ref TypeQualifier typeQualifier, ref FunctionSpecifier functionSpecifier) {
            if (is_storage_class_specifier()) {
                storageClass = storageClass.Marge(storage_class_specifier());
            } else if (is_type_specifier()) {
                typeSpecifier = typeSpecifier.Marge(type_specifier());
            } else if (is_struct_or_union_specifier()) {
                if (type != null) {
                    throw new Exception("");
                }
                type = struct_or_union_specifier();
            } else if (is_enum_specifier()) {
                if (type != null) {
                    throw new Exception("");
                }
                type = enum_specifier();
            } else if (is_TYPEDEF_NAME()) {
                SyntaxTree.Declaration.TypeDeclaration value;
                if (_typedefScope.TryGetValue(lexer.current_token().Raw, out value) == false) {
                    throw new Exception();
                }
                if (type != null) {
                    if (CType.IsEqual(type, value.Type) == false) {
                        throw new Exception("");
                    }
                }
                type = new CType.TypedefedType(lexer.current_token().Raw, value.Type);
                lexer.next_token();
            } else if (is_type_qualifier()) {
                typeQualifier.Marge(type_qualifier());
            } else if (is_function_specifier()) {
                functionSpecifier.Marge(function_specifier());
            } else {
                throw new Exception("");
            }
        }

        /// <summary>
        /// 6.7 宣言 (初期化宣言子となりうるか)
        /// </summary>
        /// <returns></returns>
        private bool is_init_declarator() {
            return is_declarator();
        }

        /// <summary>
        /// 6.7 宣言 (初期化宣言子)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <returns></returns>
        private SyntaxTree.Declaration init_declarator(CType type, StorageClassSpecifier storageClass) {
            // 宣言子
            string ident = "";
            List<CType> stack = new List<CType>() { new CType.StubType() };
            declarator(ref ident, stack, 0);
            type = CType.Resolve(type, stack);

            SyntaxTree.Declaration decl;
            if (lexer.ReadIf('=')) {
                // 初期化子を伴う関数宣言

                if (storageClass == StorageClassSpecifier.Typedef) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "初期化子を伴う変数宣言に指定することができない記憶クラス指定子 typedef が指定されている。");
                }

                if (type.IsFunctionType()) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "関数型を持つ宣言子に対して初期化子を設定しています。");
                }
                if (ident == "tktk") {

                }
                var init = initializer(type);

                // 再宣言の確認
                IdentifierScopeValue iv;
                bool isCurrent;
                if (_identScope.TryGetValue(ident, out iv, out isCurrent) && isCurrent == true) {
                    if (iv.IsVariable() == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"{ident}は既に変数以外として宣言されています。");
                    }
                    if (CType.IsEqual(iv.ToVariable().Type, type) == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"既に宣言されている変数{ident}と型が一致しないため再宣言できません。");
                    }
                    if (iv.ToVariable().Init != null) {
                        throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, $"変数{ident}は既に初期化子を伴って宣言されている。");
                    }
                    iv.ToVariable().Init = init;
                    decl = iv.ToVariable();
                } else {
                    if (iv != null) {
                        // 警告！名前を隠した！
                    }
                    decl = new SyntaxTree.Declaration.VariableDeclaration(ident, type, storageClass, init);
                    // 識別子スコープに変数宣言を追加
                    _identScope.Add(ident, new IdentifierScopeValue.Declaration(decl));
                }
            } else if (storageClass == StorageClassSpecifier.Typedef) {
                // 型宣言名

                // 再宣言の確認
                SyntaxTree.Declaration.TypeDeclaration tdecl;
                bool isCurrent;
                if (_typedefScope.TryGetValue(ident, out tdecl, out isCurrent)) {
                    if (isCurrent) {
                        throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, $"{ident} は既に型宣言名として宣言されています。");
                    }
                }
                tdecl = new SyntaxTree.Declaration.TypeDeclaration(ident, type);
                decl = tdecl;
                _typedefScope.Add(ident, tdecl);
            } else if (type.IsFunctionType()) {
                // 再宣言の確認
                IdentifierScopeValue iv;
                if (_identScope.TryGetValue(ident, out iv)) {
                    if (iv.IsFunction() == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"{ident}は既に関数以外として宣言されています。");
                    }
                    if (CType.IsEqual(iv.ToFunction().Ty, type) == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"既に宣言されている関数{ident}と型が一致しないため再宣言できません。");
                    }
                    if (storageClass != StorageClassSpecifier.Static && storageClass == StorageClassSpecifier.None && storageClass != StorageClassSpecifier.Extern) {
                        throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, $"関数宣言に指定することができない記憶クラス指定子 {(storageClass == StorageClassSpecifier.Register ? "register" : storageClass == StorageClassSpecifier.Typedef ? "typedef" : storageClass.ToString())} が指定されている。");
                    }
                    if (storageClass == StorageClassSpecifier.Static && iv.ToFunction().StorageClass == StorageClassSpecifier.Static) {
                        // お互いが static なので再宣言可能
                    } else if ((storageClass == StorageClassSpecifier.Extern || storageClass == StorageClassSpecifier.None) &&
                               (iv.ToFunction().StorageClass == StorageClassSpecifier.Extern || iv.ToFunction().StorageClass == StorageClassSpecifier.None)) {
                        // お互いが extern もしくは 指定なし なので再宣言可能
                    } else {
                        throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, $"既に宣言されている関数{ident}と記憶指定クラスが一致しないため再宣言できません。");
                    }
                    decl = iv.ToFunction();
                } else {
                    decl = new SyntaxTree.Declaration.FunctionDeclaration(ident, type, storageClass, FunctionSpecifier.None);
                    // 識別子スコープに関数宣言を追加
                    _identScope.Add(ident, new IdentifierScopeValue.Declaration(decl));
                }
            } else {
                if (storageClass == StorageClassSpecifier.Typedef) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "初期化子を伴う変数宣言に指定することができない記憶クラス指定子 typedef が指定されている。");
                }

                if (type.IsFunctionType()) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "関数型を持つ宣言子に対して初期化子を設定しています。");
                }

                // 再宣言の確認
                IdentifierScopeValue iv;
                if (_identScope.TryGetValue(ident, out iv)) {
                    if (iv.IsVariable() == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"{ident}は既に変数以外として宣言されています。");
                    }
                    if (CType.IsEqual(iv.ToVariable().Type, type) == false) {
                        throw new CompilerException.TypeMissmatchError(lexer.current_token().Start, lexer.current_token().End, $"既に宣言されている変数{ident}と型が一致しないため再宣言できません。");
                    }
                    decl = iv.ToVariable();
                } else {
                    decl = new SyntaxTree.Declaration.VariableDeclaration(ident, type, storageClass, null);
                    // 識別子スコープに変数宣言を追加
                    _identScope.Add(ident, new IdentifierScopeValue.Declaration(decl));
                }
            }
            return decl;
        }

        /// <summary>
        /// 6.7.1 記憶域クラス指定子になりうるか
        /// </summary>
        /// <returns></returns>
        private bool is_storage_class_specifier() {
            return lexer.Peek(Token.TokenKind.AUTO, Token.TokenKind.REGISTER, Token.TokenKind.STATIC, Token.TokenKind.EXTERN, Token.TokenKind.TYPEDEF);
        }

        /// <summary>
        /// 6.7.1 記憶域クラス指定子
        /// </summary>
        /// <returns></returns>
        private StorageClassSpecifier storage_class_specifier() {
            switch (lexer.current_token().Kind) {
                case Token.TokenKind.AUTO:
                    lexer.next_token();
                    return StorageClassSpecifier.Auto;
                case Token.TokenKind.REGISTER:
                    lexer.next_token();
                    return StorageClassSpecifier.Register;
                case Token.TokenKind.STATIC:
                    lexer.next_token();
                    return StorageClassSpecifier.Static;
                case Token.TokenKind.EXTERN:
                    lexer.next_token();
                    return StorageClassSpecifier.Extern;
                case Token.TokenKind.TYPEDEF:
                    lexer.next_token();
                    return StorageClassSpecifier.Typedef;
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// 6.7.2 型指定子になりうるか
        /// </summary>
        /// <returns></returns>
        private bool is_type_specifier() {
            return lexer.Peek(Token.TokenKind.VOID, Token.TokenKind.CHAR, Token.TokenKind.INT, Token.TokenKind.FLOAT, Token.TokenKind.DOUBLE, Token.TokenKind.SHORT, Token.TokenKind.LONG, Token.TokenKind.SIGNED, Token.TokenKind.UNSIGNED);
        }

        /// <summary>
        /// 匿名型に割り当てる名前を生成するためのカウンター
        /// </summary>
        private int anony = 0;

        /// <summary>
        /// 6.7.2 型指定子
        /// </summary>
        /// <returns></returns>
        private TypeSpecifier type_specifier() {
            switch (lexer.current_token().Kind) {
                case Token.TokenKind.VOID:
                    lexer.next_token();
                    return TypeSpecifier.Void;
                case Token.TokenKind.CHAR:
                    lexer.next_token();
                    return TypeSpecifier.Char;
                case Token.TokenKind.INT:
                    lexer.next_token();
                    return TypeSpecifier.Int;
                case Token.TokenKind.FLOAT:
                    lexer.next_token();
                    return TypeSpecifier.Float;
                case Token.TokenKind.DOUBLE:
                    lexer.next_token();
                    return TypeSpecifier.Double;
                case Token.TokenKind.SHORT:
                    lexer.next_token();
                    return TypeSpecifier.Short;
                case Token.TokenKind.LONG:
                    lexer.next_token();
                    return TypeSpecifier.Long;
                case Token.TokenKind.SIGNED:
                    lexer.next_token();
                    return TypeSpecifier.Signed;
                case Token.TokenKind.UNSIGNED:
                    lexer.next_token();
                    return TypeSpecifier.Unsigned;
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// 6.7.2.1 構造体指定子及び共用体指定子になりうるか
        /// </summary>
        /// <returns></returns>
        private bool is_struct_or_union_specifier() {
            return lexer.Peek(Token.TokenKind.STRUCT, Token.TokenKind.UNION);
        }

        /// <summary>
        /// 6.7.2.1 構造体指定子及び共用体指定子（構造体共用体指定子）
        /// </summary>
        /// <returns></returns>
        private CType struct_or_union_specifier() {
            var kind = lexer.current_token().Kind == Token.TokenKind.STRUCT ? CType.TaggedType.StructUnionType.StructOrUnion.Struct : CType.TaggedType.StructUnionType.StructOrUnion.Union;

            // 構造体共用体
            lexer.Read(Token.TokenKind.STRUCT, Token.TokenKind.UNION);

            // 識別子の有無で分岐
            if (is_IDENTIFIER(true)) {

                var ident = IDENTIFIER(true);

                // 波括弧の有無で分割
                if (lexer.ReadIf('{')) {
                    // 識別子を伴う完全型の宣言
                    CType.TaggedType tagType;
                    CType.TaggedType.StructUnionType structUnionType;
                    if (_tagScope.TryGetValue(ident, out tagType) == false) {
                        // タグ名前表に無い場合は新しく追加する。
                        structUnionType = new CType.TaggedType.StructUnionType(kind, ident, false);
                        _tagScope.Add(ident, structUnionType);
                    } else if (!(tagType is CType.TaggedType.StructUnionType)) {
                        throw new Exception($"構造体/共用体 {ident} は既に列挙型として定義されています。");
                    } else if ((tagType as CType.TaggedType.StructUnionType).Kind != kind) {
                        throw new Exception($"構造体/共用体 {ident} は既に定義されていますが、構造体/共用体の種別が一致しません。");
                    } else if ((tagType as CType.TaggedType.StructUnionType).Members != null) {
                        throw new Exception($"構造体/共用体 {ident} は既に完全型として定義されています。");
                    } else {
                        // 不完全型として定義されているので完全型にするために書き換え対象とする
                        structUnionType = (tagType as CType.TaggedType.StructUnionType);
                    }
                    // メンバ宣言並びを解析する
                    structUnionType.Members = struct_declarations();
                    lexer.Read('}');
                    return structUnionType;
                } else {
                    // 不完全型の宣言
                    CType.TaggedType tagType;
                    if (_tagScope.TryGetValue(ident, out tagType) == false) {
                        // タグ名前表に無い場合は新しく追加する。
                        tagType = new CType.TaggedType.StructUnionType(kind, ident, false);
                        _tagScope.Add(ident, tagType);
                    } else if (!(tagType is CType.TaggedType.StructUnionType)) {
                        throw new Exception($"構造体/共用体 {ident} は既に列挙型として定義されています。");
                    } else if ((tagType as CType.TaggedType.StructUnionType).Kind != kind) {
                        throw new Exception($"構造体/共用体 {ident} は既に定義されていますが、構造体/共用体の種別が一致しません。");
                    } else {
                        // 既に定義されているものが完全型・不完全型問わず何もしない。
                    }
                    return tagType;
                }
            } else {
                // 識別子を伴わない匿名の完全型の宣言

                // 名前を生成
                var ident = $"${kind}_{anony++}";

                // 型情報を生成する
                var structUnionType = new CType.TaggedType.StructUnionType(kind, ident, true);

                // タグ名前表に追加する
                _tagScope.Add(ident, structUnionType);

                // メンバ宣言並びを解析する
                lexer.Read('{');
                structUnionType.Members = struct_declarations();
                lexer.Read('}');
                return structUnionType;
            }
        }

        /// <summary>
        /// 6.7.2.1 構造体指定子及び共用体指定子(メンバ宣言並び)
        /// </summary>
        /// <returns></returns>
        private List<CType.TaggedType.StructUnionType.MemberInfo> struct_declarations() {
            var items = new List<CType.TaggedType.StructUnionType.MemberInfo>();
            items.AddRange(struct_declaration());
            while (is_struct_declaration()) {
                items.AddRange(struct_declaration());
            }
            return items;
        }

        /// <summary>
        /// 6.7.2.1 構造体指定子及び共用体指定子(メンバ宣言)となりうるか
        /// </summary>
        /// <returns></returns>
        private bool is_struct_declaration() {
            return is_specifier_qualifiers();
        }

        /// <summary>
        /// 6.7.2.1 構造体指定子及び共用体指定子(メンバ宣言)
        /// </summary>
        /// <returns></returns>
        private List<CType.TaggedType.StructUnionType.MemberInfo> struct_declaration() {
            CType baseType = specifier_qualifiers();
            var ret = struct_declarator_list(baseType);
            lexer.Read(';');
            return ret;
        }

        /// <summary>
        /// 6.7.2.1 型指定子型修飾子並びとなりうるか
        /// </summary>
        /// <returns></returns>
        private bool is_specifier_qualifiers() {
            return is_specifier_qualifier(null, TypeSpecifier.None);
        }

        /// <summary>
        /// 6.7.2.1 型指定子型修飾子並び
        /// </summary>
        /// <returns></returns>
        private CType specifier_qualifiers() {
            CType baseType = null;
            TypeSpecifier typeSpecifier = TypeSpecifier.None;
            TypeQualifier typeQualifier = TypeQualifier.None;

            // 型指定子もしくは型修飾子を読み取る。
            if (is_specifier_qualifier(null, TypeSpecifier.None) == false) {
                if (is_storage_class_specifier()) {
                    // 記憶クラス指定子（文法上は無くてよい。エラーメッセージ表示のために用意。）
                    throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"記憶クラス指定子 { lexer.current_token().ToString() } は使えません。");
                }
                throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, "型指定子もしくは型修飾子以外の要素がある。");
            }
            specifier_qualifier(ref baseType, ref typeSpecifier, ref typeQualifier);
            while (is_specifier_qualifier(baseType, typeSpecifier)) {
                specifier_qualifier(ref baseType, ref typeSpecifier, ref typeQualifier);
            }

            if (baseType != null) {
                // 型指定子部に構造体共用体指定子、列挙型指定子、型定義名が出現する場合
                if (typeSpecifier != TypeSpecifier.None) {
                    // 6.7.2 型指定子「型指定子の並びは，次に示すもののいずれか一つでなければならない。」中で構造体共用体指定子、列挙型指定子、型定義名はそれ単体のみで使うことが規定されているため、
                    // 構造体共用体指定子、列挙型指定子、型定義名のいずれかとそれら以外の型指定子が組み合わせられている場合はエラーとする。
                    // なお、構造体共用体指定子、列挙型指定子、型定義名が複数回利用されている場合は specifier_qualifier 内でエラーとなる。
                    // （歴史的な話：K&R では typedef は 別名(alias)扱いだったため、typedef int INT; unsingned INT x; は妥当だった）
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "型指定子・型修飾子並び中で構造体共用体指定子、列挙型指定子、型定義名のいずれかと、それら以外の型指定子が組み合わせられている。");
                }
            } else {
                // 型指定子部に構造体共用体指定子、列挙型指定子、型定義名が出現しない場合
                if (typeSpecifier == TypeSpecifier.None) {
                    // 6.7.2 それぞれの宣言の宣言指定子列の中で，又はそれぞれの構造体宣言及び型名の型指定子型修飾子並びの中で，少なくとも一つの型指定子を指定しなければならない。
                    // とあるため、宣言指定子を一つも指定しないことは許されない。
                    // （歴史的な話：K&R では 宣言指定子を省略すると int 扱い）
                    // ToDo: C90は互換性の観点からK&R動作も残されているので選択できるようにする
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "それぞれの宣言の宣言指定子列の中で，又はそれぞれの構造体宣言及び型名の型指定子型修飾子並びの中で，少なくとも一つの型指定子を指定しなければならない。");
                } else {
                    baseType = new CType.BasicType(typeSpecifier);
                }
            }

            // 型修飾子を適用
            baseType = baseType.WrapTypeQualifier(typeQualifier);

            return baseType;
        }

        /// <summary>
        /// 6.7.2.1 型指定子型修飾子並び（型指定子もしくは型修飾子となりうるか？）
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeSpecifier"></param>
        /// <returns></returns>
        private bool is_specifier_qualifier(CType type, TypeSpecifier typeSpecifier) {
            return (
                (is_type_specifier() && type == null) ||
                (is_struct_or_union_specifier() && type == null) ||
                (is_enum_specifier() && type == null) ||
                (is_TYPEDEF_NAME() && type == null && typeSpecifier == TypeSpecifier.None) ||
                is_type_qualifier());
        }

        /// <summary>
        /// 6.7.2.1 型指定子型修飾子並び（型指定子もしくは型修飾子）
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeSpecifier"></param>
        /// <returns></returns>
        private void specifier_qualifier(ref CType type, ref TypeSpecifier typeSpecifier, ref TypeQualifier typeQualifier) {
            if (is_type_specifier()) {
                // 型指定子（予約語）
                typeSpecifier = typeSpecifier.Marge(type_specifier());
            } else if (is_struct_or_union_specifier()) {
                // 型指定子（構造体指定子もしくは共用体指定子）
                if (type != null) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "型指定子・型修飾子並び中で構造体共用体指定子、列挙型指定子、型定義名が２つ以上使用されている。");
                }
                type = struct_or_union_specifier();
            } else if (is_enum_specifier()) {
                // 型指定子（列挙型指定子）
                if (type != null) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "型指定子・型修飾子並び中で構造体共用体指定子、列挙型指定子、型定義名が２つ以上使用されている。");
                }
                type = enum_specifier();
            } else if (is_TYPEDEF_NAME()) {
                // 型指定子（型定義名）
                SyntaxTree.Declaration.TypeDeclaration value;
                if (_typedefScope.TryGetValue(lexer.current_token().Raw, out value) == false) {
                    throw new CompilerException.UndefinedIdentifierErrorException(lexer.current_token().Start, lexer.current_token().End, $"型名 {lexer.current_token().Raw} は定義されていません。");
                }
                if (type != null) {
                    throw new CompilerException.SpecificationErrorException(lexer.current_token().Start, lexer.current_token().End, "型指定子・型修飾子並び中で構造体共用体指定子、列挙型指定子、型定義名が２つ以上使用されている。");
                }
                type = new CType.TypedefedType(lexer.current_token().Raw, value.Type);
                lexer.next_token();
            } else if (is_type_qualifier()) {
                // 型修飾子
                typeQualifier.Marge(type_qualifier());
            } else {
                throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, $"型指定子型修飾子は 型指定子の予約語, 構造体指定子もしくは共用体指定子, 列挙型指定子, 型定義名 型修飾子の何れかですが、 { lexer.current_token().ToString() } はそのいずれでもありません。（本処理系の実装に誤りがあると思います。）");
            }
        }

        /// <summary>
        /// 6.7.2.1 型指定子型修飾子並び（メンバ宣言子並び）
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private List<CType.TaggedType.StructUnionType.MemberInfo> struct_declarator_list(CType type) {
            var ret = new List<CType.TaggedType.StructUnionType.MemberInfo>();
            ret.Add(struct_declarator(type));
            while (lexer.ReadIf(',')) {
                ret.Add(struct_declarator(type));
            }
            return ret;
        }

        /// <summary>
        /// 6.7.2.1 型指定子型修飾子並び（メンバ宣言子）
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private CType.TaggedType.StructUnionType.MemberInfo struct_declarator(CType type) {
            string ident = null;
            if (is_declarator()) {
                // 宣言子
                List<CType> stack = new List<CType>() { new CType.StubType() };
                declarator(ref ident, stack, 0);
                type = CType.Resolve(type, stack);

                // ビットフィールド部分(opt)
                SyntaxTree.Expression expr = null;
                if (lexer.ReadIf(':')) {
                    expr = constant_expression();
                }

                return new CType.TaggedType.StructUnionType.MemberInfo(ident, type, expr == null ? (int?)null : Evaluator.ConstantEval(expr));
            } else if (lexer.ReadIf(':')) {
                // ビットフィールド部分(must)
                SyntaxTree.Expression expr = constant_expression();

                return new CType.TaggedType.StructUnionType.MemberInfo(ident, type, expr == null ? (int?)null : Evaluator.ConstantEval(expr));
            } else {
                throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"構造体/共用体のメンバ宣言子では、宣言子とビットフィールド部の両方を省略することはできません。無名構造体/共用体を使用できるのは規格上はC11からです。(C11 6.7.2.1で規定)。");
            }
        }

        /// <summary>
        /// 6.7.2.2 列挙型指定子となりうるか
        /// </summary>
        /// <returns></returns>
        private bool is_enum_specifier() {
            return lexer.Peek(Token.TokenKind.ENUM);
        }

        /// <summary>
        /// 6.7.2.2 列挙型指定子
        /// </summary>
        /// <returns></returns>
        private CType enum_specifier() {
            lexer.Read(Token.TokenKind.ENUM);

            if (is_IDENTIFIER(true)) {
                var ident = IDENTIFIER(true);
                CType.TaggedType etype;
                if (_tagScope.TryGetValue(ident, out etype) == false) {
                    // タグ名前表に無い場合は新しく追加する。
                    etype = new CType.TaggedType.EnumType(ident, false);
                    _tagScope.Add(ident, etype);
                } else if (!(etype is CType.TaggedType.EnumType)) {
                    throw new Exception($"列挙型 {ident} は既に構造体/共用体として定義されています。");
                } else {

                }
                if (lexer.ReadIf('{')) {
                    if ((etype as CType.TaggedType.EnumType).Members != null) {
                        throw new Exception($"列挙型 {ident} は既に完全型として定義されています。");
                    } else {
                        // 不完全型として定義されているので完全型にするために書き換え対象とする
                        (etype as CType.TaggedType.EnumType).Members = enumerator_list(etype as CType.TaggedType.EnumType);
                        lexer.Read('}');
                    }
                }
                return etype;
            } else {
                var ident = $"$enum_{anony++}";
                var etype = new CType.TaggedType.EnumType(ident, true);
                _tagScope.Add(ident, etype);
                lexer.Read('{');
                enumerator_list(etype);
                lexer.Read('}');
                return etype;
            }
        }

        /// <summary>
        /// 6.7.2.2 列挙型指定子（列挙子並び）
        /// </summary>
        /// <param name="enumType"></param>
        private List<CType.TaggedType.EnumType.MemberInfo> enumerator_list(CType.TaggedType.EnumType enumType) {
            var ret = new List<CType.TaggedType.EnumType.MemberInfo>();
            enumType.Members = ret;
            var e = enumerator(enumType, 0);
            _identScope.Add(e.Ident, new IdentifierScopeValue.EnumValue(enumType, e.Ident));
            ret.Add(e);
            while (lexer.ReadIf(',')) {
                var i = e.Value + 1;
                if (is_enumerator() == false) {
                    break;
                }
                e = enumerator(enumType, i);
                _identScope.Add(e.Ident, new IdentifierScopeValue.EnumValue(enumType, e.Ident));
                ret.Add(e);
            }
            return ret;
        }

        /// <summary>
        /// 6.7.2.2 列挙型指定子（列挙子となりうるか）
        /// </summary>
        /// <returns></returns>
        private bool is_enumerator() {
            return is_IDENTIFIER(false);
        }

        /// <summary>
        /// 6.7.2.2 列挙型指定子（列挙子）
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private CType.TaggedType.EnumType.MemberInfo enumerator(CType.TaggedType.EnumType enumType, int i) {
            var ident = IDENTIFIER(false);
            if (lexer.ReadIf('=')) {
                var expr = constant_expression();
                i = Evaluator.ConstantEval(expr);
            }
            return new CType.TaggedType.EnumType.MemberInfo(enumType, ident, i);
        }

        /// <summary>
        /// 6.7.3 型修飾子となりうるか
        /// </summary>
        /// <returns></returns>
        private bool is_type_qualifier() {
            return lexer.Peek(Token.TokenKind.CONST, Token.TokenKind.VOLATILE, Token.TokenKind.RESTRICT, Token.TokenKind.NEAR, Token.TokenKind.FAR);
        }

        /// <summary>
        /// 6.7.3 型修飾子
        /// </summary>
        /// <returns></returns>
        private TypeQualifier type_qualifier() {
            switch (lexer.current_token().Kind) {
                case Token.TokenKind.CONST:
                    lexer.next_token();
                    return TypeQualifier.Const;
                case Token.TokenKind.VOLATILE:
                    lexer.next_token();
                    return TypeQualifier.Volatile;
                case Token.TokenKind.RESTRICT:
                    lexer.next_token();
                    return TypeQualifier.Restrict;
                case Token.TokenKind.NEAR:
                    lexer.next_token();
                    return TypeQualifier.Near;
                case Token.TokenKind.FAR:
                    lexer.next_token();
                    return TypeQualifier.Far;
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// 6.7.4 関数指定子となりうるか
        /// </summary>
        /// <returns></returns>
        private bool is_function_specifier() {
            return lexer.Peek(Token.TokenKind.INLINE);
        }

        /// <summary>
        /// 6.7.4 関数指定子
        /// </summary>
        /// <returns></returns>
        private FunctionSpecifier function_specifier() {
            switch (lexer.current_token().Kind) {
                case Token.TokenKind.INLINE:
                    lexer.next_token();
                    return FunctionSpecifier.Inline;
                default:
                    throw new Exception();
            }
        }

        private bool is_TYPEDEF_NAME() {
            //return lexer.current_token().Kind == Token.TokenKind.TYPE_NAME;
            return lexer.current_token().Kind == Token.TokenKind.IDENTIFIER && _typedefScope.ContainsKey(lexer.current_token().Raw);
        }

        /// <summary>
        /// 6.7.5 宣言子となりうるか
        /// </summary>
        /// <returns></returns>
        private bool is_declarator() {
            return is_pointer() || is_direct_declarator();
        }

        /// <summary>
        /// 6.7.5 宣言子
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void declarator(ref string ident, List<CType> stack, int index) {
            if (is_pointer()) {
                pointer(stack, index);
            }
            direct_declarator(ref ident, stack, index);
        }

        /// <summary>
        /// 6.7.5 宣言子(直接宣言子となりうるか)
        /// </summary>
        /// <returns></returns>
        private bool is_direct_declarator() {
            return lexer.Peek('(') || is_IDENTIFIER(true);
        }

        /// <summary>
        /// 6.7.5 宣言子(直接宣言子の前半部分)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void direct_declarator(ref string ident, List<CType> stack, int index) {
            if (lexer.ReadIf('(')) {
                stack.Add(new CType.StubType());
                declarator(ref ident, stack, index + 1);
                lexer.Read(')');
            } else {
                ident = lexer.current_token().Raw;
                lexer.next_token();
            }
            more_direct_declarator(stack, index);
        }

        /// <summary>
        /// 6.7.5 宣言子(直接宣言子の後半部分)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void more_direct_declarator(List<CType> stack, int index) {
            if (lexer.ReadIf('[')) {
                // 6.7.5.2 配列宣言子
                // ToDo: AnsiC範囲のみ対応
                int len = -1;
                if (lexer.Peek(']') == false) {
                    var expr = constant_expression();
                    len = Evaluator.ConstantEval(expr);
                }
                lexer.Read(']');
                more_direct_declarator(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            } else if (lexer.ReadIf('(')) {
                // 6.7.5.3 関数宣言子（関数原型を含む）
                if (lexer.ReadIf(')')) {
                    // k&r or ANSI empty parameter list
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                    more_direct_declarator(stack, index);
                } else if (is_identifier_list()) {
                    // K&R parameter name list
                    var args = identifier_list().Select(x => new CType.FunctionType.ArgumentInfo(x, StorageClassSpecifier.None, (CType)new CType.BasicType(TypeSpecifier.None))).ToList();
                    lexer.Read(')');
                    stack[index] = new CType.FunctionType(args, false, stack[index]);
                    more_direct_declarator(stack, index);
                } else {
                    // ANSI parameter list
                    bool vargs = false;
                    var args = parameter_type_list(ref vargs);
                    lexer.Read(')');
                    stack[index] = new CType.FunctionType(args, vargs, stack[index]);
                    more_direct_declarator(stack, index);

                }
            } else {
                //_epsilon_
            }
        }

        /// <summary>
        /// 6.7.5 宣言子(仮引数型並びとなりうるか)
        /// </summary>
        /// <returns></returns>
        private bool is_parameter_type_list() {
            return is_parameter_declaration();
        }

        /// <summary>
        /// 6.7.5 宣言子(仮引数型並び)
        /// </summary>
        /// <returns></returns>
        private List<CType.FunctionType.ArgumentInfo> parameter_type_list(ref bool vargs) {
            var items = new List<CType.FunctionType.ArgumentInfo>();
            items.Add(parameter_declaration());
            while (lexer.ReadIf(',')) {
                if (lexer.ReadIf(Token.TokenKind.ELLIPSIS)) {
                    vargs = true;
                    break;
                } else {
                    items.Add(parameter_declaration());
                }
            }
            return items;
        }

        /// <summary>
        /// 6.7.5 宣言子(仮引数並びとなりうるか)
        /// </summary>
        /// <returns></returns>
        public bool is_parameter_declaration() {
            return is_declaration_specifier(null, TypeSpecifier.None);
        }

        /// <summary>
        /// 6.7.5 宣言子(仮引数並び)
        /// </summary>
        /// <returns></returns>
        private CType.FunctionType.ArgumentInfo parameter_declaration() {
            StorageClassSpecifier storageClass;
            CType baseType = declaration_specifiers(out storageClass);

            if (is_declarator_or_abstract_declarator()) {
                string ident = "";
                List<CType> stack = new List<CType>() { new CType.StubType() };
                declarator_or_abstract_declarator(ref ident, stack, 0);
                var type = CType.Resolve(baseType, stack);
                return new CType.FunctionType.ArgumentInfo(ident, storageClass, type);
            } else {
                return new CType.FunctionType.ArgumentInfo((string)null, storageClass, baseType);
            }

        }

        /// <summary>
        /// 6.7.5 宣言子(宣言子もしくは抽象宣言子となりうるか)
        /// </summary>
        /// <returns></returns>
        private bool is_declarator_or_abstract_declarator() {
            return is_pointer() || is_direct_declarator_or_direct_abstract_declarator();
        }

        /// <summary>
        /// 6.7.5 宣言子(宣言子もしくは抽象宣言子)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
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

        /// <summary>
        /// 6.7.5 宣言子(直接宣言子もしくは直接抽象宣言子となりうるか)
        /// </summary>
        /// <returns></returns>
        private bool is_direct_declarator_or_direct_abstract_declarator() {
            return is_IDENTIFIER(true) || lexer.Peek('(', '[');
        }

        /// <summary>
        /// 6.7.5 宣言子(直接宣言子もしくは直接抽象宣言子の前半)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void direct_declarator_or_direct_abstract_declarator(ref string ident, List<CType> stack, int index) {
            if (is_IDENTIFIER(true)) {
                ident = IDENTIFIER(true);
                more_dd_or_dad(stack, index);
            } else if (lexer.ReadIf('(')) {
                if (lexer.Peek(')')) {
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
                lexer.Read(')');
                more_dd_or_dad(stack, index);
            } else if (lexer.ReadIf('[')) {
                int len = -1;
                if (lexer.Peek(']') == false) {
                    var expr = constant_expression();
                    len = Evaluator.ConstantEval(expr);
                }
                lexer.Read(']');
                more_dd_or_dad(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            } else {
                throw new Exception();
            }

        }

        /// <summary>
        /// 6.7.5 宣言子(直接宣言子もしくは直接抽象宣言子の後半)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void more_dd_or_dad(List<CType> stack, int index) {
            if (lexer.ReadIf('(')) {
                if (lexer.Peek(')')) {
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
                    var args = identifier_list().Select(x => new CType.FunctionType.ArgumentInfo(x, StorageClassSpecifier.None, (CType)new CType.BasicType(TypeSpecifier.None))).ToList();
                    stack[index] = new CType.FunctionType(args, false, stack[index]);
                }
                lexer.Read(')');
                more_dd_or_dad(stack, index);
            } else if (lexer.ReadIf('[')) {
                int len = -1;
                if (lexer.Peek(']') == false) {
                    var expr = constant_expression();
                    len = Evaluator.ConstantEval(expr);
                }
                lexer.Read(']');
                more_dd_or_dad(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            } else {
                // _epsilon_
            }
        }

        /// <summary>
        /// 6.7.5 宣言子(識別子並びとなりうるか)
        /// </summary>
        /// <returns></returns>
        private bool is_identifier_list() {
            return is_IDENTIFIER(false);
        }

        /// <summary>
        /// 6.7.5 宣言子(識別子並び)
        /// </summary>
        /// <returns></returns>
        private List<string> identifier_list() {
            var items = new List<string>();
            items.Add(IDENTIFIER(false));
            while (lexer.ReadIf(',')) {
                items.Add(IDENTIFIER(false));
            }
            return items;
        }

        /// <summary>
        /// 6.7.5.1 ポインタ宣言子となりうるか
        /// </summary>
        /// <returns></returns>
        private bool is_pointer() {
            return lexer.Peek('*');
        }

        /// <summary>
        /// 6.7.5.1 ポインタ宣言子
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void pointer(List<CType> stack, int index) {
            lexer.Read('*');
            stack[index] = CType.CreatePointer(stack[index]);
            TypeQualifier typeQualifier = TypeQualifier.None;
            while (is_type_qualifier()) {
                typeQualifier = typeQualifier.Marge(type_qualifier());
            }
            stack[index] = stack[index].WrapTypeQualifier(typeQualifier);

            if (is_pointer()) {
                pointer(stack, index);
            }
        }

        /// <summary>
        /// 6.7.6 型名(型名)となりうるか？
        /// </summary>
        /// <returns></returns>
        private bool is_type_name() {
            return is_specifier_qualifiers();
        }

        /// <summary>
        /// 6.7.6 型名(型名)
        /// </summary>
        /// <returns></returns>
        private CType type_name() {
            CType baseType = specifier_qualifiers();
            if (is_abstract_declarator()) {
                List<CType> stack = new List<CType>() { new CType.StubType() };
                abstract_declarator(stack, 0);
                baseType = CType.Resolve(baseType, stack);
            }
            return baseType;
        }

        /// <summary>
        /// 6.7.6 型名(抽象宣言子)となりうるか？
        /// </summary>
        /// <returns></returns>
        private bool is_abstract_declarator() {
            return (is_pointer() || is_direct_abstract_declarator());
        }

        /// <summary>
        /// 6.7.6 型名(抽象宣言子)
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 6.7.6 型名(直接抽象宣言子を構成する前半の要素)となりうるか？
        /// </summary>
        /// <returns></returns>
        private bool is_direct_abstract_declarator() {
            return lexer.Peek('(', '[');
        }

        /// <summary>
        /// 6.7.6 型名(直接抽象宣言子を構成する前半の要素)
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void direct_abstract_declarator(List<CType> stack, int index) {
            if (lexer.ReadIf('(')) {
                if (is_abstract_declarator()) {
                    stack.Add(new CType.StubType());
                    abstract_declarator(stack, index + 1);
                } else if (lexer.Peek(')') == false) {
                    // ansi args
                    bool vargs = false;
                    var items = parameter_type_list(ref vargs);
                } else {
                    // k&r or ansi
                }
                lexer.Read(')');
                more_direct_abstract_declarator(stack, index);
            } else {
                lexer.Read('[');
                int len = -1;
                if (lexer.Peek(']') == false) {
                    var expr = constant_expression();
                    len = Evaluator.ConstantEval(expr);
                }
                lexer.Read(']');
                more_direct_abstract_declarator(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            }
        }

        /// <summary>
        /// 6.7.6 型名(直接抽象宣言子を構成する後半の要素)
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void more_direct_abstract_declarator(List<CType> stack, int index) {
            if (lexer.ReadIf('[')) {
                int len = -1;
                if (lexer.Peek(']') == false) {
                    var expr = constant_expression();
                    len = Evaluator.ConstantEval(expr);
                }
                lexer.Read(']');
                more_direct_abstract_declarator(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            } else if (lexer.ReadIf('(')) {
                if (lexer.Peek(')') == false) {
                    bool vargs = false;
                    var items = parameter_type_list(ref vargs);
                    stack[index] = new CType.FunctionType(items, vargs, stack[index]);
                } else {
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                }
                lexer.Read(')');
                more_direct_abstract_declarator(stack, index);
            } else {
                // _epsilon_
            }
        }

        /// <summary>
        /// 6.7.7 型定義(型定義名)
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private bool is_typedefed_type(string v) {
            return _typedefScope.ContainsKey(v);
        }

        private void CheckInitializerExpression(CType type, SyntaxTree.Initializer ast) {
            if (type.IsArrayType()) {
                // 配列型の場合
                var arrayType = type.Unwrap() as CType.ArrayType;
                CheckInitializerArray(arrayType, ast);
                return;
            } else if (type.IsStructureType()) {
                // 構造体型の場合
                var arrayType = type.Unwrap() as CType.TaggedType.StructUnionType;
                CheckInitializerStruct(arrayType, ast);
                return;
            } else if (type.IsUnionType()) {
                // 共用体型の場合
                var arrayType = type.Unwrap() as CType.TaggedType.StructUnionType;
                CheckInitializerUnion(arrayType, ast);
                return;
            } else {
                var expr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression;
                if (type.IsAggregateType() || type.IsUnionType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "集成体型又は共用体型をもつオブジェクトに対する初期化子は，要素又は名前付きメンバに対する初期化子並びを波括弧で囲んだものでなければならない。");
                }
                // 代入式を生成して検証
                var dummyVar = new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression("<dummy>", new SyntaxTree.Declaration.VariableDeclaration("<dummy>", type, StorageClassSpecifier.None, null));
                var assign = new SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression("=", dummyVar, expr);
                // 例外が起きないなら代入できる
            }
        }

        private void CheckInitializerArray(CType.ArrayType type, SyntaxTree.Initializer ast) {
            if (ast is SyntaxTree.Initializer.SimpleInitializer) {
                var expr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression;
                if (type.IsAggregateType() || type.IsUnionType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "集成体型又は共用体型をもつオブジェクトに対する初期化子は，要素又は名前付きメンバに対する初期化子並びを波括弧で囲んだものでなければならない。");
                }
                // 代入式を生成して検証
                var dummyVar = new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression("<dummy>", new SyntaxTree.Declaration.VariableDeclaration("<dummy>", type, StorageClassSpecifier.None, null));
                var assign = new SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression("=", dummyVar, expr);
                // 例外が起きないなら代入できる
            } else if (ast is SyntaxTree.Initializer.ComplexInitializer) {
                var inits = (ast as SyntaxTree.Initializer.ComplexInitializer).Ret;
                if (type.Length == -1) {
                    type.Length = inits.Count;
                } else if (type.Length < inits.Count) {
                    throw new Exception("要素数が違う");
                }
                // 要素数分回す
                for (var i = 0; type.Length == -1 || i < inits.Count; i++) {
                    CheckInitializerExpression(type.BaseType, inits[i]);
                }
            }
        }
        private void CheckInitializerStruct(CType.TaggedType.StructUnionType type, SyntaxTree.Initializer ast) {
            if (ast is SyntaxTree.Initializer.SimpleInitializer) {
                var expr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression;
                if (type.IsAggregateType() || type.IsUnionType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "集成体型又は共用体型をもつオブジェクトに対する初期化子は，要素又は名前付きメンバに対する初期化子並びを波括弧で囲んだものでなければならない。");
                }
                // 代入式を生成して検証
                var dummyVar = new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression("<dummy>", new SyntaxTree.Declaration.VariableDeclaration("<dummy>", type, StorageClassSpecifier.None, null));
                var assign = new SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression("=", dummyVar, expr);
                // 例外が起きないなら代入できる
            } else if (ast is SyntaxTree.Initializer.ComplexInitializer) {
                var inits = (ast as SyntaxTree.Initializer.ComplexInitializer).Ret;
                if (type.Members.Count < inits.Count) {
                    throw new Exception("要素数が違う");
                }
                // 要素数分回す
                // Todo: ビットフィールド
                for (var i = 0; i < inits.Count; i++) {
                    CheckInitializerExpression(type.Members[i].Type, inits[i]);
                }
            } else {
                throw new Exception("");
            }
        }
        private void CheckInitializerUnion(CType.TaggedType.StructUnionType type, SyntaxTree.Initializer ast) {
            if (ast is SyntaxTree.Initializer.SimpleInitializer) {
                var expr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression;
                if (type.IsAggregateType() || type.IsUnionType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "集成体型又は共用体型をもつオブジェクトに対する初期化子は，要素又は名前付きメンバに対する初期化子並びを波括弧で囲んだものでなければならない。");
                }
                // 代入式を生成して検証
                var dummyVar = new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression("<dummy>", new SyntaxTree.Declaration.VariableDeclaration("<dummy>", type, StorageClassSpecifier.None, null));
                var assign = new SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression("=", dummyVar, expr);
                // 例外が起きないなら代入できる
            } else if (ast is SyntaxTree.Initializer.ComplexInitializer) {
                var inits = (ast as SyntaxTree.Initializer.ComplexInitializer).Ret;
                // 最初の要素とのみチェック
                CheckInitializerExpression(type.Members[0].Type, ast);
            } else {
                throw new Exception("");
            }
        }
        private void CheckInitializerList(CType type, SyntaxTree.Initializer ast) {
            if (type.IsArrayType()) {
                // 配列型の場合
                var arrayType = type.Unwrap() as CType.ArrayType;

                if (arrayType.BaseType.IsCharacterType()) {
                    // 初期化先の型が文字配列の場合

                    if (ast is SyntaxTree.Initializer.SimpleInitializer && (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression is SyntaxTree.Expression.PrimaryExpression.StringExpression) {
                        // 文字列式で初期化
                        var strExpr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression as SyntaxTree.Expression.PrimaryExpression.StringExpression;
                        if (arrayType.Length == -1) {
                            arrayType.Length = string.Concat(strExpr.Strings).Length + 1;
                        }
                        return;
                    } else if (ast is SyntaxTree.Initializer.ComplexInitializer) {
                        // 波括弧で括られた文字列で初期化
                        if ((ast as SyntaxTree.Initializer.ComplexInitializer).Ret?.Count == 1
                            && ((ast as SyntaxTree.Initializer.ComplexInitializer).Ret[0] as SyntaxTree.Initializer.SimpleInitializer)?.AssignmentExpression is SyntaxTree.Expression.PrimaryExpression.StringExpression) {
                            var strExpr = ((ast as SyntaxTree.Initializer.ComplexInitializer).Ret[0] as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression as SyntaxTree.Expression.PrimaryExpression.StringExpression;
                            if (arrayType.Length == -1) {
                                arrayType.Length = string.Concat(strExpr.Strings).Length + 1;
                            }
                            return;
                        }
                    }
                }
                CheckInitializerArray(arrayType, ast);
                return;
            } else if (type.IsStructureType()) {
                // 構造体型の場合
                var suType = type.Unwrap() as CType.TaggedType.StructUnionType;
                CheckInitializerStruct(suType, ast);
                return;
            } else if (type.IsUnionType()) {
                // 共用体型の場合
                var suType = type.Unwrap() as CType.TaggedType.StructUnionType;
                CheckInitializerUnion(suType, ast);
                return;
            } else {
                throw new Exception();
            }
        }

        private void CheckInitializer(CType type, SyntaxTree.Initializer ast) {
            if (type.IsArrayType() || type.IsStructureType() || type.IsUnionType()) {
                CheckInitializerList(type, ast);
            } else {
                var expr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression;
                if (type.IsAggregateType() || type.IsUnionType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "集成体型又は共用体型をもつオブジェクトに対する初期化子は，要素又は名前付きメンバに対する初期化子並びを波括弧で囲んだものでなければならない。");
                }
                // 代入式を生成して検証
                var dummyVar = new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression("<dummy>", new SyntaxTree.Declaration.VariableDeclaration("<dummy>", type, StorageClassSpecifier.None, null));
                var assign = new SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression("=", dummyVar, expr);
                // 例外が起きないなら代入できる
            }
        }

        /// <summary>
        /// 6.7.8 初期化(初期化子)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Initializer initializer(CType type) {
            var init = initializer_();
            CheckInitializer(type, init);
            return init;
        }
        private SyntaxTree.Initializer initializer_() {
            if (lexer.ReadIf('{')) {
                List<SyntaxTree.Initializer> ret = null;
                if (lexer.Peek('}') == false) {
                    ret = initializer_list();
                }
                lexer.Read('}');
                return new SyntaxTree.Initializer.ComplexInitializer(ret);
            } else {
                return new SyntaxTree.Initializer.SimpleInitializer(assignment_expression());
            }
        }

        /// <summary>
        /// 6.7.8 初期化(初期化子並び)
        /// </summary>
        /// <returns></returns>
        private List<SyntaxTree.Initializer> initializer_list() {
            var ret = new List<SyntaxTree.Initializer>();
            ret.Add(initializer_());
            while (lexer.ReadIf(',')) {
                if (lexer.Peek('}')) {
                    break;
                }
                ret.Add(initializer_());
            }
            return ret;
        }

        private bool is_IDENTIFIER(bool include_type_name) {
            // return lexer.current_token().Kind == Token.TokenKind.IDENTIFIER || (include_type_name && lexer.current_token().Kind == Token.TokenKind.TYPE_NAME);
            if (include_type_name) {
                return lexer.current_token().Kind == Token.TokenKind.IDENTIFIER;
            } else {
                return lexer.current_token().Kind == Token.TokenKind.IDENTIFIER && !_typedefScope.ContainsKey(lexer.current_token().Raw);

            }
        }

        private string IDENTIFIER(bool include_type_name) {
            if (is_IDENTIFIER(include_type_name) == false) {
                throw new Exception();
            }
            var ret = lexer.current_token().Raw;
            lexer.next_token();
            return ret;
        }

        /// <summary>
        /// 6.8 文及びブロック
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement statement() {
            if ((is_IDENTIFIER(true) && lexer.is_nexttoken(':')) || lexer.Peek(Token.TokenKind.CASE, Token.TokenKind.DEFAULT)) {
                return labeled_statement();
            } else if (lexer.Peek('{')) {
                return compound_statement();
            } else if (lexer.Peek(Token.TokenKind.IF, Token.TokenKind.SWITCH)) {
                return selection_statement();
            } else if (lexer.Peek(Token.TokenKind.WHILE, Token.TokenKind.DO, Token.TokenKind.FOR)) {
                return iteration_statement();
            } else if (lexer.Peek(Token.TokenKind.GOTO, Token.TokenKind.CONTINUE, Token.TokenKind.BREAK, Token.TokenKind.RETURN)) {
                return jump_statement();
            } else if (lexer.Peek(Token.TokenKind.__ASM__)) {
                return gnu_asm_statement();
            } else {
                return expression_statement();
            }

        }

        /// <summary>
        /// 6.8.1 ラベル付き文
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement labeled_statement() {
            if (lexer.ReadIf(Token.TokenKind.CASE)) {
                var expr = constant_expression();
                lexer.Read(':');
                var stmt = statement();
                return new SyntaxTree.Statement.CaseStatement(expr, stmt);
            } else if (lexer.ReadIf(Token.TokenKind.DEFAULT)) {
                lexer.Read(':');
                var stmt = statement();
                return new SyntaxTree.Statement.DefaultStatement(stmt);
            } else {
                var ident = IDENTIFIER(true);
                lexer.Read(':');
                var stmt = statement();
                return new SyntaxTree.Statement.GenericLabeledStatement(ident, stmt);
            }
        }

        /// <summary>
        /// 6.8.2 複合文
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement compound_statement() {
            _tagScope = _tagScope.Extend();
            _typedefScope = _typedefScope.Extend();
            _identScope = _identScope.Extend();
            lexer.Read('{');
            var decls = new List<SyntaxTree.Declaration>();
            while (is_declaration()) {
                var d = declaration();
                if (d != null) {
                    decls.AddRange(d);
                }
            }
            var stmts = new List<SyntaxTree.Statement>();
            while (lexer.Peek('}') == false) {
                stmts.Add(statement());
            }
            lexer.Read('}');
            var stmt = new SyntaxTree.Statement.CompoundStatement(decls, stmts, _tagScope, _identScope);
            _identScope = _identScope.Parent;
            _typedefScope = _typedefScope.Parent;
            _tagScope = _tagScope.Parent;
            return stmt;

        }

        /// <summary>
        /// 6.8.3 式文及び空文
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement expression_statement() {
            SyntaxTree.Statement ret;
            if (!lexer.Peek(';')) {
                var expr = expression();
                ret = new SyntaxTree.Statement.ExpressionStatement(expr);
            } else {
                ret = new SyntaxTree.Statement.EmptyStatement();
            }
            lexer.Read(';');
            return ret;
        }

        /// <summary>
        /// 6.8.4 選択文
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement selection_statement() {
            if (lexer.ReadIf(Token.TokenKind.IF)) {
                lexer.Read('(');
                var cond = expression();
                lexer.Read(')');
                var then_stmt = statement();
                SyntaxTree.Statement else_stmt = null;
                if (lexer.ReadIf(Token.TokenKind.ELSE)) {
                    else_stmt = statement();
                }
                return new SyntaxTree.Statement.IfStatement(cond, then_stmt, else_stmt);
            }
            if (lexer.ReadIf(Token.TokenKind.SWITCH)) {
                lexer.Read('(');
                var cond = expression();
                lexer.Read(')');
                var ss = new SyntaxTree.Statement.SwitchStatement(cond);
                _breakScope.Push(ss);
                ss.Stmt = statement();
                _breakScope.Pop();
                return ss;
            }
            throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, $"選択文は if, switch の何れかで始まりますが、 { lexer.current_token().ToString() } はそのいずれでもありません。（本処理系の実装に誤りがあると思います。）");
        }

        /// <summary>
        /// 6.8.5 繰返し文
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement iteration_statement() {
            if (lexer.ReadIf(Token.TokenKind.WHILE)) {
                lexer.Read('(');
                var cond = expression();
                lexer.Read(')');
                var ss = new SyntaxTree.Statement.WhileStatement(cond);
                _breakScope.Push(ss);
                _continueScope.Push(ss);
                ss.Stmt = statement();
                _breakScope.Pop();
                _continueScope.Pop();
                return ss;
            }
            if (lexer.ReadIf(Token.TokenKind.DO)) {
                var ss = new SyntaxTree.Statement.DoWhileStatement();
                _breakScope.Push(ss);
                _continueScope.Push(ss);
                ss.Stmt = statement();
                _breakScope.Pop();
                _continueScope.Pop();
                lexer.Read(Token.TokenKind.WHILE);
                lexer.Read('(');
                ss.Cond = expression();
                lexer.Read(')');
                lexer.Read(';');
                return ss;
            }
            if (lexer.ReadIf(Token.TokenKind.FOR)) {
                lexer.Read('(');

                var init = lexer.Peek(';') ? (SyntaxTree.Expression)null : expression();
                lexer.Read(';');
                var cond = lexer.Peek(';') ? (SyntaxTree.Expression)null : expression();
                lexer.Read(';');
                var update = lexer.Peek(')') ? (SyntaxTree.Expression)null : expression();
                lexer.Read(')');
                var ss = new SyntaxTree.Statement.ForStatement(init, cond, update);
                _breakScope.Push(ss);
                _continueScope.Push(ss);
                ss.Stmt = statement();
                _breakScope.Pop();
                _continueScope.Pop();
                return ss;
            }
            throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, $"繰返し文は while, do, for の何れかで始まりますが、 { lexer.current_token().ToString() } はそのいずれでもありません。（本処理系の実装に誤りがあると思います。）");
        }

        /// <summary>
        ///  6.8.6 分岐文
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement jump_statement() {
            if (lexer.ReadIf(Token.TokenKind.GOTO)) {
                var label = IDENTIFIER(true);
                lexer.Read(';');
                return new SyntaxTree.Statement.GotoStatement(label);
            }
            if (lexer.ReadIf(Token.TokenKind.CONTINUE)) {
                lexer.Read(';');
                return new SyntaxTree.Statement.ContinueStatement(_continueScope.Peek());
            }
            if (lexer.ReadIf(Token.TokenKind.BREAK)) {
                lexer.Read(';');
                return new SyntaxTree.Statement.BreakStatement(_breakScope.Peek());
            }
            if (lexer.ReadIf(Token.TokenKind.RETURN)) {
                var expr = lexer.Peek(';') ? null : expression();
                //現在の関数の戻り値と型チェック
                lexer.Read(';');
                return new SyntaxTree.Statement.ReturnStatement(expr);
            }
            throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, $"分岐文は goto, continue, break, return の何れかで始まりますが、 { lexer.current_token().ToString() } はそのいずれでもありません。（本処理系の実装に誤りがあると思います。）");
        }

        /// <summary>
        /// X.X.X GCC拡張インラインアセンブラ
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement gnu_asm_statement() {
            Console.Error.WriteLine("GCC拡張インラインアセンブラ構文には対応していません。ざっくりと読み飛ばします。");

            lexer.Read(Token.TokenKind.__ASM__);
            lexer.ReadIf(Token.TokenKind.__VOLATILE__);
            lexer.Read('(');
            Stack<char> parens = new Stack<char>();
            parens.Push('(');
            while (parens.Any()) {
                if (lexer.Peek('(', '[')) {
                    parens.Push((char)lexer.current_token().Kind);
                } else if (lexer.Peek(')')) {
                    if (parens.Peek() == '(') {
                        parens.Pop();
                    } else {
                        throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"GCC拡張インラインアセンブラ構文中で 丸括弧閉じ ) が使用されていますが、対応する丸括弧開き ( がありません。");
                    }
                } else if (lexer.Peek(']')) {
                    if (parens.Peek() == '[') {
                        parens.Pop();
                    } else {
                        throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"GCC拡張インラインアセンブラ構文中で 角括弧閉じ ] が使用されていますが、対応する角括弧開き [ がありません。");
                    }
                }
                lexer.next_token();
            }
            lexer.Read(';');
            return new SyntaxTree.Statement.EmptyStatement();
            ;
        }

        /// <summary>
        /// 6.5 式
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression expression() {
            var e = assignment_expression();
            if (lexer.Peek(',')) {
                var ce = new SyntaxTree.Expression.CommaExpression();
                ce.expressions.Add(e);
                while (lexer.ReadIf(',')) {
                    e = assignment_expression();
                    ce.expressions.Add(e);
                }
                return ce;
            } else {
                return e;
            }
        }

        /// <summary>
        /// 6.5.1 一次式
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression primary_expression() {
            if (is_IDENTIFIER(false)) {
                var ident = IDENTIFIER(false);
                IdentifierScopeValue value;
                if (_identScope.TryGetValue(ident, out value) == false) {
                    Console.Error.WriteLine($"未定義の識別子{ident}が一次式として利用されています。");
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression(ident);
                }
                if (value.IsVariable()) {
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression(ident, value.ToVariable());
                }
                if (value.IsEnumValue()) {
                    var ev = value.ToEnumValue();
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant(ev);
                }
                if (value.IsFunction()) {
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression(ident, value.ToFunction());
                }
                throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, $"一次式として使える定義済み識別子は変数、列挙定数、関数の何れかですが、 { lexer.current_token().ToString() } はそのいずれでもありません。（本処理系の実装に誤りがあると思います。）");
            }
            if (is_constant()) {
                return constant();
            }
            if (is_STRING()) {
                List<string> strings = new List<string>();
                while (is_STRING()) {
                    strings.Add(STRING());
                }
                return new SyntaxTree.Expression.PrimaryExpression.StringExpression(strings);
            }
            if (lexer.ReadIf('(')) {
                if (lexer.Peek('{')) {
                    // gcc statement expression
                    var statements = compound_statement();
                    lexer.Read(')');
                    return new SyntaxTree.Expression.GccStatementExpression(statements);
                } else {
                    var expr = new SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression(expression());
                    lexer.Read(')');
                    return expr;
                }
            }
            throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"一次式となる要素があるべき場所に { lexer.current_token().ToString() } があります。");
        }

        /// <summary>
        /// 6.5.1 一次式(定数となりうるか)
        /// </summary>
        /// <returns></returns>
        private bool is_constant() {
            return is_INTEGER_CONSTANT() ||
                   is_CHARACTER_CONSTANT() ||
                   is_FLOATING_CONSTANT() ||
                   is_ENUMERATION_CONSTANT();
        }

        /// <summary>
        /// 6.5.1 一次式(定数)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression constant() {
            // 6.5.1 一次式
            // 定数は，一次式とする。その型は，その形式と値によって決まる（6.4.4 で規定する。）。

            // 整数定数
            if (is_INTEGER_CONSTANT()) {
                return INTEGER_CONSTANT();
            }

            // 文字定数
            if (is_CHARACTER_CONSTANT()) {
                var ret = CHARACTER_CONSTANT();
                return new SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant(ret);
            }

            // 浮動小数定数
            if (is_FLOATING_CONSTANT()) {
                return FLOATING_CONSTANT();
            }

            // 列挙定数
            if (is_ENUMERATION_CONSTANT()) {
                var ret = ENUMERATION_CONSTANT();
                return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant(ret);
            }

            throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"定数があるべき場所に { lexer.current_token().ToString() } があります。");
        }

        /// <summary>
        /// 6.5.2 後置演算子(後置式の前半)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression postfix_expression() {
            var expr = primary_expression();
            return more_postfix_expression(expr);

        }

        /// <summary>
        /// 6.5.2 後置演算子(後置式の後半)
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        private SyntaxTree.Expression more_postfix_expression(SyntaxTree.Expression expr) {
            if (lexer.ReadIf('[')) {
                // 6.5.2.1 配列の添字付け
                var index = expression();
                lexer.Read(']');
                return more_postfix_expression(new SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression(expr, index));
            }
            if (lexer.ReadIf('(')) {
                // 6.5.2.2 関数呼出し
                List<SyntaxTree.Expression> args = null;
                if (lexer.Peek(')') == false) {
                    args = argument_expression_list();
                } else {
                    args = new List<SyntaxTree.Expression>();
                }
                lexer.Read(')');
                // 未定義の識別子の直後に関数呼び出し用の後置演算子 '(' がある場合、
                // K&RおよびC89/90では暗黙的関数宣言 extern int 識別子(); が現在の宣言ブロックの先頭で定義されていると仮定して翻訳する
                if (expr is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression) {

                }
                return more_postfix_expression(new SyntaxTree.Expression.PostfixExpression.FunctionCallExpression(expr, args));
            }
            if (lexer.ReadIf('.')) {
                // 6.5.2.3 構造体及び共用体のメンバ
                var ident = IDENTIFIER(false);
                return more_postfix_expression(new SyntaxTree.Expression.PostfixExpression.MemberDirectAccess(expr, ident));
            }
            if (lexer.ReadIf(Token.TokenKind.PTR_OP)) {
                // 6.5.2.3 構造体及び共用体のメンバ
                var ident = IDENTIFIER(false);
                return more_postfix_expression(new SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess(expr, ident));
            }
            if (lexer.Peek(Token.TokenKind.INC_OP, Token.TokenKind.DEC_OP)) {
                // 6.5.2.4 後置増分及び後置減分演算子
                var op = SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.None;
                switch (lexer.current_token().Kind) {
                    case Token.TokenKind.INC_OP:
                        op = SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Inc;
                        break;
                    case Token.TokenKind.DEC_OP:
                        op = SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Dec;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, "たぶん実装ミスです。");
                }
                lexer.next_token();
                return more_postfix_expression(new SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression(op, expr));
            }
            // 6.5.2.5 複合リテラル
            // Todo: 未実装
            return expr;
        }

        /// <summary>
        /// 6.5.2 後置演算子(実引数並び)
        /// </summary>
        /// <returns></returns>
        private List<SyntaxTree.Expression> argument_expression_list() {
            var ret = new List<SyntaxTree.Expression>();
            ret.Add(assignment_expression());
            while (lexer.ReadIf(',')) {
                ret.Add(assignment_expression());
            }
            return ret;
        }


        /// <summary>
        /// 6.5.3 単項演算子(単項式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression unary_expression() {
            if (lexer.Peek(Token.TokenKind.INC_OP, Token.TokenKind.DEC_OP)) {
                var op = SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.None;
                switch (lexer.current_token().Kind) {
                    case Token.TokenKind.INC_OP:
                        op = SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Inc;
                        break;
                    case Token.TokenKind.DEC_OP:
                        op = SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Dec;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(lexer.current_token().Start, lexer.current_token().End, "たぶん実装ミスです。");
                }
                lexer.next_token();
                var expr = unary_expression();
                return new SyntaxTree.Expression.UnaryPrefixExpression(op, expr);
            }
            if (lexer.ReadIf('&')) {
                var expr = cast_expression();
                return new SyntaxTree.Expression.UnaryAddressExpression(expr);
            }
            if (lexer.ReadIf('*')) {
                var expr = cast_expression();
                return new SyntaxTree.Expression.UnaryReferenceExpression(expr);
            }
            if (lexer.ReadIf('+')) {
                var expr = cast_expression();
                return new SyntaxTree.Expression.UnaryPlusExpression(expr);
            }
            if (lexer.ReadIf('-')) {
                var expr = cast_expression();
                return new SyntaxTree.Expression.UnaryMinusExpression(expr);
            }
            if (lexer.ReadIf('~')) {
                var expr = cast_expression();
                return new SyntaxTree.Expression.UnaryNegateExpression(expr);
            }
            if (lexer.ReadIf('!')) {
                var expr = cast_expression();
                return new SyntaxTree.Expression.UnaryNotExpression(expr);
            }
            if (lexer.ReadIf(Token.TokenKind.SIZEOF)) {
                if (lexer.Peek('(')) {
                    // どっちにも'('が出ることが出来るのでさらに先読みする（LL(2))
                    var saveCurrent = lexer.Save();
                    lexer.Read('(');
                    if (is_type_name()) {
                        var type = type_name();
                        lexer.Read(')');
                        return new SyntaxTree.Expression.SizeofTypeExpression(type);
                    } else {
                        lexer.Restore(saveCurrent);
                        var expr = unary_expression();
                        return new SyntaxTree.Expression.SizeofExpression(expr);
                    }
                } else {
                    // 括弧がないので式
                    var expr = unary_expression();
                    return new SyntaxTree.Expression.SizeofExpression(expr);
                }
            }
            return postfix_expression();
        }

        /// <summary>
        /// 6.5.4 キャスト演算子(キャスト式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression cast_expression() {
            if (lexer.Peek('(')) {
                // どちらにも'('の出現が許されるためさらに先読みを行う。
                var saveCurrent = lexer.Save();
                lexer.Read('(');
                if (is_type_name()) {
                    var type = type_name();
                    lexer.Read(')');
                    var expr = cast_expression();
                    return new SyntaxTree.Expression.CastExpression(type, expr);
                } else {
                    lexer.Restore(saveCurrent);
                    return unary_expression();
                }
            } else {
                return unary_expression();
            }
        }

        /// <summary>
        /// 6.5.5 乗除演算子(乗除式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression multiplicitive_expression() {
            var lhs = cast_expression();
            while (lexer.Peek('*', '/', '%')) {
                SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind op;
                switch (lexer.current_token().Kind) {
                    case (Token.TokenKind)'*':
                        op = SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mul;
                        break;
                    case (Token.TokenKind)'/':
                        op = SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Div;
                        break;
                    case (Token.TokenKind)'%':
                        op = SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind.Mod;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "");
                }
                lexer.next_token();
                var rhs = cast_expression();
                lhs = new SyntaxTree.Expression.MultiplicitiveExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.6 加減演算子(加減式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression additive_expression() {
            var lhs = multiplicitive_expression();
            while (lexer.Peek('+', '-')) {
                SyntaxTree.Expression.AdditiveExpression.OperatorKind op;
                switch (lexer.current_token().Kind) {
                    case (Token.TokenKind)'+':
                        op = SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add;
                        break;
                    case (Token.TokenKind)'-':
                        op = SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "");
                }
                lexer.next_token();
                var rhs = multiplicitive_expression();
                lhs = new SyntaxTree.Expression.AdditiveExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.7 ビット単位のシフト演算子(シフト式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression shift_expression() {
            var lhs = additive_expression();
            while (lexer.Peek(Token.TokenKind.LEFT_OP, Token.TokenKind.RIGHT_OP)) {
                var op = SyntaxTree.Expression.ShiftExpression.OperatorKind.None;
                switch (lexer.current_token().Kind) {
                    case Token.TokenKind.LEFT_OP:
                        op = SyntaxTree.Expression.ShiftExpression.OperatorKind.Left;
                        break;
                    case Token.TokenKind.RIGHT_OP:
                        op = SyntaxTree.Expression.ShiftExpression.OperatorKind.Right;
                        break;
                    default:
                        throw new Exception();
                }

                lexer.next_token();
                var rhs = additive_expression();
                lhs = new SyntaxTree.Expression.ShiftExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.8 関係演算子(関係式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression relational_expression() {
            var lhs = shift_expression();
            while (lexer.Peek((Token.TokenKind)'<', (Token.TokenKind)'>', Token.TokenKind.LE_OP, Token.TokenKind.GE_OP)) {
                var op = SyntaxTree.Expression.RelationalExpression.OperatorKind.None;
                switch (lexer.current_token().Kind) {
                    case (Token.TokenKind)'<':
                        op = SyntaxTree.Expression.RelationalExpression.OperatorKind.LessThan;
                        break;
                    case (Token.TokenKind)'>':
                        op = SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterThan;
                        break;
                    case Token.TokenKind.LE_OP:
                        op = SyntaxTree.Expression.RelationalExpression.OperatorKind.LessOrEqual;
                        break;
                    case Token.TokenKind.GE_OP:
                        op = SyntaxTree.Expression.RelationalExpression.OperatorKind.GreaterOrEqual;
                        break;
                    default:
                        throw new Exception();
                }
                lexer.next_token();
                var rhs = shift_expression();
                lhs = new SyntaxTree.Expression.RelationalExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.9 等価演算子(等価式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression equality_expression() {
            var lhs = relational_expression();
            while (lexer.Peek(Token.TokenKind.EQ_OP, Token.TokenKind.NE_OP)) {
                var op = SyntaxTree.Expression.EqualityExpression.OperatorKind.None;
                switch (lexer.current_token().Kind) {
                    case Token.TokenKind.EQ_OP:
                        op = SyntaxTree.Expression.EqualityExpression.OperatorKind.Equal;
                        break;
                    case Token.TokenKind.NE_OP:
                        op = SyntaxTree.Expression.EqualityExpression.OperatorKind.NotEqual;
                        break;
                    default:
                        throw new Exception();
                }
                lexer.next_token();
                var rhs = relational_expression();
                lhs = new SyntaxTree.Expression.EqualityExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.10 ビット単位の AND 演算子(AND式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression and_expression() {
            var lhs = equality_expression();
            while (lexer.ReadIf('&')) {
                var rhs = equality_expression();
                lhs = new SyntaxTree.Expression.AndExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.11 ビット単位の排他 OR 演算子(排他OR式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression exclusive_OR_expression() {
            var lhs = and_expression();
            while (lexer.ReadIf('^')) {
                var rhs = and_expression();
                lhs = new SyntaxTree.Expression.ExclusiveOrExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.12 ビット単位の OR 演算子(OR式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression inclusive_OR_expression() {
            var lhs = exclusive_OR_expression();
            while (lexer.ReadIf('|')) {
                var rhs = exclusive_OR_expression();
                lhs = new SyntaxTree.Expression.InclusiveOrExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.13 論理 AND 演算子(論理AND式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression logical_AND_expression() {
            var lhs = inclusive_OR_expression();
            while (lexer.ReadIf(Token.TokenKind.AND_OP)) {
                var rhs = inclusive_OR_expression();
                lhs = new SyntaxTree.Expression.LogicalAndExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.14 論理 OR 演算子(論理OR式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression logical_OR_expression() {
            var lhs = logical_AND_expression();
            while (lexer.ReadIf(Token.TokenKind.OR_OP)) {
                var rhs = logical_AND_expression();
                lhs = new SyntaxTree.Expression.LogicalOrExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.15 条件演算子(条件式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression conditional_expression() {
            var cond = logical_OR_expression();
            if (lexer.ReadIf('?')) {
                var then_expr = expression();
                lexer.Read(':');
                var else_expr = conditional_expression();
                return new SyntaxTree.Expression.ConditionalExpression(cond, then_expr, else_expr);
            } else {
                return cond;
            }
        }

        /// <summary>
        /// 6.5.16 代入演算子(代入式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression assignment_expression() {
            var lhs = conditional_expression();
            if (is_assignment_operator()) {
                var op = assignment_operator();
                var rhs = assignment_expression();
                if (op == "=") {
                    lhs = new SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression(op, lhs, rhs);
                } else {
                    lhs = new SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression(op, lhs, rhs);
                }
            }
            return lhs;
        }

        /// <summary>
        ///6.5.16 代入演算子（代入演算子トークンとなりうるか？）
        /// </summary>
        /// <returns></returns>
        private bool is_assignment_operator() {
            return lexer.Peek((Token.TokenKind)'=', Token.TokenKind.MUL_ASSIGN, Token.TokenKind.DIV_ASSIGN, Token.TokenKind.MOD_ASSIGN, Token.TokenKind.ADD_ASSIGN, Token.TokenKind.SUB_ASSIGN, Token.TokenKind.LEFT_ASSIGN, Token.TokenKind.RIGHT_ASSIGN, Token.TokenKind.AND_ASSIGN, Token.TokenKind.XOR_ASSIGN, Token.TokenKind.OR_ASSIGN);
        }

        /// <summary>
        /// 6.5.16 代入演算子（代入演算子トークン）
        /// </summary>
        /// <returns></returns>
        private string assignment_operator() {
            if (is_assignment_operator() == false) {
                throw new CompilerException.SyntaxErrorException(lexer.current_token().Start, lexer.current_token().End, $"代入演算子があるべき場所に { lexer.current_token().ToString() } があります。");
            }
            var ret = lexer.current_token().Raw;
            lexer.next_token();
            return ret;
        }

        /// <summary>
        /// 6.6 定数式
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression constant_expression() {
            // 補足説明  
            // 定数式は，実行時ではなく翻訳時に評価することができる。したがって，定数を使用してよいところならばどこでも使用してよい。
            //
            // 制約
            // - 定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。
            //   ただし，定数式が評価されない部分式(sizeof演算子のオペランド等)に含まれている場合を除く。
            // - 定数式を評価した結果は，その型で表現可能な値の範囲内にある定数でなければならない。
            // 

            // ToDo: 初期化子中の定数式の扱いを実装
            return conditional_expression();

        }

    }
}