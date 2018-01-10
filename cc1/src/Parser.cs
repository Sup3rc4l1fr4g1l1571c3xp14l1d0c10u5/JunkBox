using System;
using System.Collections.Generic;
using System.Linq;

namespace AnsiCParser {

    /// <summary>
    /// パーサ
    /// </summary>
    public class Parser {

        /// <summary>
        /// 言語レベル
        /// </summary>
        public enum LanguageMode {
            None,
            C89,
            C99,    // 完全実装ではない
        }

        /// <summary>
        /// 言語レベルの選択
        /// </summary>
        private LanguageMode Mode = LanguageMode.C89;

        /// <summary>
        /// 名前空間(ステートメント ラベル)
        /// </summary>
        private Scope<LabelScopeValue> _labelScope = Scope<LabelScopeValue>.Empty.Extend();

        /// <summary>
        /// 名前空間(構造体、共用体、列挙体のタグ名)
        /// </summary>
        private Scope<CType.TaggedType> _tagScope = Scope<CType.TaggedType>.Empty.Extend();

        /// <summary>
        /// 名前空間(通常の識別子（変数、関数、引数、列挙定数、Typedef名)
        /// </summary>
        private Scope<SyntaxTree.Declaration> _identScope = Scope<SyntaxTree.Declaration>.Empty.Extend();

        /// <summary>
        /// リンケージオブジェクト表(外部結合・内部結合解決用)
        /// </summary>
        private readonly Dictionary<string, LinkageObject> _linkageTable = new Dictionary<string, LinkageObject>();

        /// <summary>
        /// リンケージオブジェクトリスト
        /// </summary>
        private readonly List<LinkageObject> _linkageList = new List<LinkageObject>();

        /// <summary>
        /// リンケージオブジェクトの生成とリンケージ表への登録
        /// </summary>
        /// <param name="linkage"></param>
        /// <param name="decl"></param>
        /// <param name="isDefine"></param>
        /// <returns></returns>
        private LinkageObject AddLinkageObject(LinkageKind linkage, SyntaxTree.Declaration decl, bool isDefine) {
            if (linkage == LinkageKind.ExternalLinkage || linkage == LinkageKind.InternalLinkage) {
                // 外部もしくは内部結合なので再定義チェック
            } else if (linkage == LinkageKind.NoLinkage) {
                // 無結合なので再定義チェックしない
                var v = new LinkageObject(decl.Ident, decl.Type, linkage);
                if (decl.StorageClass == AnsiCParser.StorageClassSpecifier.Static) {
                    v.Definition = decl;
                    v.LinkageId = $"{decl.Ident}.{_linkageList.Count}";
                    _linkageList.Add(v);
                }
                return v;
            } else {
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "リンケージが指定されていません。");
            }
            LinkageObject value;
            if (_linkageTable.TryGetValue(decl.Ident, out value)) {
                if (value.Linkage != linkage) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "オブジェクトのリンケージが以前のオブジェクトと一致しない");
                }
                if (Specification.IsCompatible(value.Type, decl.Type) == false) {
                    throw new CompilerException.TypeMissmatchError(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "オブジェクトの型が以前のオブジェクトと一致しない");
                }
                if (value.Definition != null && isDefine) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "オブジェクトは既に実体をもっています");
                }
            } else {
                value = new LinkageObject(decl.Ident, decl.Type, linkage);
                _linkageTable[decl.Ident] = value;
                _linkageList.Add(value);
            }

            if (!isDefine) {
                // 仮定義
                value.TentativeDefinitions.Add(decl);
            } else if (value.Definition == null) {
                // 本定義
                value.Definition = decl;
            }

            return value;
        }


        // 構造体または共用体のメンバーについてはそれぞれのオブジェクトが管理する

        /// <summary>
        /// break命令についてのスコープ
        /// </summary>
        private readonly Stack<SyntaxTree.Statement> _breakScope = new Stack<SyntaxTree.Statement>();

        /// <summary>
        /// continue命令についてのスコープ
        /// </summary>
        private readonly Stack<SyntaxTree.Statement> _continueScope = new Stack<SyntaxTree.Statement>();

        /// <summary>
        /// switch文についてのスコープ
        /// </summary>
        private readonly Stack<SyntaxTree.Statement.SwitchStatement> _switchScope = new Stack<SyntaxTree.Statement.SwitchStatement>();

        /// <summary>
        /// 暗黙的宣言の挿入を行うクロージャー
        /// </summary>
        private readonly Stack<Func<SyntaxTree.Declaration, SyntaxTree.Declaration>> _insertImplictDeclarationOperatorStack = new Stack<Func<SyntaxTree.Declaration, SyntaxTree.Declaration>>();

        private SyntaxTree.Declaration AddImplictFunctionDeclaration(string ident, CType type) {
            var storageClass = AnsiCParser.StorageClassSpecifier.Extern;
            var functionSpecifier = AnsiCParser.FunctionSpecifier.None;
            var funcDecl = FunctionDeclaration(ident, type, storageClass, functionSpecifier, ScopeKind.FileScope, false);
            return _insertImplictDeclarationOperatorStack.Peek().Invoke(funcDecl);
        }

        private SyntaxTree.Declaration AddImplictTypeDeclaration(string ident, CType type) {
            var typeDecl = new SyntaxTree.Declaration.TypeDeclaration(ident, type);
            return _insertImplictDeclarationOperatorStack.Peek().Invoke(typeDecl);
        }

        /// <summary>
        /// 字句解析器
        /// </summary>
        private readonly Lexer _lexer;

        public Parser(string s) {
            _lexer = new Lexer(s, "<built-in>");

            // GCCの組み込み型の設定
            _identScope.Add("__builtin_va_list", new SyntaxTree.Declaration.TypeDeclaration("__builtin_va_list", CType.CreatePointer(new CType.BasicType(AnsiCParser.TypeSpecifier.Void))));

        }

        /// <summary>
        /// 構文解析と意味解析を行い、構文木を返す
        /// </summary>
        /// <returns></returns>
        public SyntaxTree Parse() {
            return TranslationUnit();
        }


        private void EoF() {
            if (!_lexer.is_eof()) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "ファイルが正しく終端していません。");
            }
        }

        /// <summary>
        /// 6.2 結合(リンケージ)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        private LinkageKind ResolveLinkage(string ident, CType type, StorageClassSpecifier storageClass, ScopeKind scope) {
            // 記憶域クラス指定からリンケージを求める
            switch (storageClass) {
                case AnsiCParser.StorageClassSpecifier.Auto:
                case AnsiCParser.StorageClassSpecifier.Register:
                    if (scope == ScopeKind.FileScope) {
                        // 記憶域クラス指定子 auto 及び register が，外部宣言の宣言指定子列の中に現れてはならない。
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "記憶域クラス指定子 auto 及び register が，外部宣言の宣言指定子列の中に現れてはならない。");
                    } else {
                        // 6.2.2 識別子の結合
                        // オブジェクト又は関数以外を宣言する識別子，関数仮引数を宣言する識別子，及び記憶域クラス指定子 extern を伴わないブロック有効範囲のオブジェクトを宣言する識別子は，無結合とする。
                        // auto/register 指定はどちらも 記憶域クラス指定子 extern を伴わないブロック有効範囲のオブジェクトの宣言なので無結合
                        return LinkageKind.NoLinkage;
                    }
                case AnsiCParser.StorageClassSpecifier.Typedef:
                    // 6.2.2 識別子の結合
                    // オブジェクト又は関数以外を宣言する識別子，関数仮引数を宣言する識別子，及び記憶域クラス指定子 extern を伴わないブロック有効範囲のオブジェクトを宣言する識別子は，無結合とする。
                    // typedef は オブジェクト又は関数以外を宣言する識別子 なので無結合
                    return LinkageKind.NoLinkage;
                case AnsiCParser.StorageClassSpecifier.Static:
                    if (type.IsFunctionType()) {
                        if (scope == ScopeKind.BlockScope) {
                            // 6.2.2 識別子の結合
                            // 関数宣言が記憶域クラス指定子 static をもつことができるのは，ファイル有効範囲をもつ宣言の場合だけである（6.7.1 参照）
                            throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "関数宣言が記憶域クラス指定子 static をもつことができるのは，ファイル有効範囲をもつ宣言の場合だけである");
                        }
                    }
                    if (scope == ScopeKind.FileScope && (type.IsObjectType() || type.IsFunctionType())) {
                        // 6.2.2 識別子の結合
                        // オブジェクト又は関数に対するファイル有効範囲の識別子の宣言が記憶域クラス指定子 static を含む場合，その識別子は，内部結合をもつ
                        return LinkageKind.InternalLinkage;
                    } else if (scope == ScopeKind.BlockScope) {
                        // 記憶域クラス指定子 extern を伴わないブロック有効範囲のオブジェクトを宣言する識別子は，無結合とする
                        return LinkageKind.NoLinkage;  // 無結合
                    } else {
                        throw new Exception("オブジェクト又は関数に対するファイル有効範囲の識別子の宣言、もしくは、記憶域クラス指定子 extern を伴わないブロック有効範囲のオブジェクト、のどちらでもない識別子の宣言で static が使用されている。");
                    }
                case AnsiCParser.StorageClassSpecifier.None:
                    // 6.2.2 識別子の結合
                    // 関数の識別子の宣言が記憶域クラス指定子をもたない場合，その結合は，記憶域クラス指定子 externを伴って宣言された場合と同じ規則で決定する。
                    // オブジェクトの識別子の宣言がファイル有効範囲をもち，かつ記憶域クラス指定子をもたない場合，その識別子の結合は，外部結合とする
                    // オブジェクト又は関数以外を宣言する識別子，関数仮引数を宣言する識別子，及び記憶域クラス指定子 extern を伴わないブロック有効範囲のオブジェクトを宣言する識別子は，無結合とする。
                    // 整理すると 
                    // 記憶域クラス指定子をもたない場合
                    //  -> 関数の識別子の宣言の場合、記憶域クラス指定子 externを伴って宣言された場合と同じ規則で決定
                    //  -> オブジェクトの識別子の宣言がファイル有効範囲場合，その識別子の結合は，外部結合とする
                    if (type.IsFunctionType()) {
                        //  -> 関数の識別子の宣言の場合、記憶域クラス指定子 externを伴って宣言された場合と同じ規則で決定
                        goto case AnsiCParser.StorageClassSpecifier.Extern;
                    } else {
                        if (scope == ScopeKind.FileScope) {
                            //  -> オブジェクトの識別子の宣言がファイル有効範囲場合，その識別子の結合は，外部結合とする
                            return LinkageKind.ExternalLinkage;// 外部結合
                        } else {
                            // 記憶域クラス指定子 extern を伴わないブロック有効範囲のオブジェクトを宣言する識別子は，無結合とする
                            return LinkageKind.NoLinkage;// 無結合
                        }

                    }
                case AnsiCParser.StorageClassSpecifier.Extern: {
                        // 識別子が，その識別子の以前の宣言が可視である有効範囲において，記憶域クラス指定子 extern を伴って宣言される場合，次のとおりとする。
                        SyntaxTree.Declaration iv;
                        if (_identScope.TryGetValue(ident, out iv)) {
                            // - 以前の宣言において内部結合又は外部結合が指定されているならば，新しい宣言における識別子は，以前の宣言と同じ結合をもつ。
                            if (iv.LinkageObject.Linkage == LinkageKind.ExternalLinkage || iv.LinkageObject.Linkage == LinkageKind.InternalLinkage) {
                                return iv.LinkageObject.Linkage;
                            } else if (iv.LinkageObject.Linkage == LinkageKind.NoLinkage) {
                                // - 可視である以前の宣言がない場合，又は以前の宣言が無結合である場合，この識別子は外部結合をもつ。
                                // => 以前の宣言が無結合なのでこの識別子は外部結合をもつ。
                                return LinkageKind.ExternalLinkage;// 外部結合
                            } else {
                                throw new Exception("");
                            }
                        } else {
                            // - 可視である以前の宣言がない場合，又は以前の宣言が無結合である場合，この識別子は外部結合をもつ。
                            // => 可視である以前の宣言がないのでこの識別子は外部結合をもつ。
                            return LinkageKind.ExternalLinkage;// 外部結合
                        }
                    }
                default:
                    throw new Exception();
            }
        }


        #region 6.4.4 定数

        /// <summary>
        /// 6.4.4.1 整数定数となりうるか？
        /// </summary>
        /// <returns></returns>
        private bool IsIntegerConstant() {
            return _lexer.CurrentToken().Kind == Token.TokenKind.HEXIMAL_CONSTANT
                 | _lexer.CurrentToken().Kind == Token.TokenKind.OCTAL_CONSTANT
                 | _lexer.CurrentToken().Kind == Token.TokenKind.DECIAML_CONSTANT;
        }

        // 整数定数の型は，次の表の対応する並びの中で，その値を表現できる最初の型とする。
        // +--------------------+-------------------------+-------------------------+
        // |       接尾語       |         10進定数        |  8進定数 又は 16進定数  |
        // +--------------------+-------------------------+-------------------------+
        // |        なし        | int                     | int                     |
        // |                    | long int                | unsigned int            |
        // |                    | long long int           | long int                |
        // |                    |                         | unsigned long int       |
        // |                    |                         | long long int           |
        // |                    |                         | unsigned long long int  |
        // +--------------------+-------------------------+-------------------------+
        // |      u 又は U      | unsigned int            | unsigned int            |
        // |                    | unsigned long int       | unsigned long int       |
        // |                    | unsigned long long int  | unsigned long long int  |
        // +--------------------+-------------------------+-------------------------+
        // |      l 又は L      | long int                | long int                |
        // |                    | long long int           | unsigned long int       |
        // |                    |                         | long long int           |
        // |                    |                         | unsigned long long int  |
        // +--------------------+-------------------------+-------------------------+
        // |  u 又は U 及び     | unsigned long int       | unsigned long int       |
        // |  l 又は L の両方   | unsigned long long int  | unsigned long long int  |
        // +--------------------+-------------------------+-------------------------+
        // |     ll 又は LL     | long long int           | long long int           |
        // |                    |                         | unsigned long long int  |
        // +--------------------+-------------------------+-------------------------+
        // | u 又は U 及び      | unsigned long long int  | unsigned long long int  |
        // | ll 又は LL の両方  |                         |                         |
        // +--------------------+-------------------------+-------------------------+

        /// <summary>
        /// 8進定数 又は 16進定数の型候補表
        /// </summary>
        private static readonly Dictionary<string, CType.BasicType.TypeKind[]> CandidateTypeTableHexOrOct = new Dictionary<string, CType.BasicType.TypeKind[]> {
            {"LLU", new[] { CType.BasicType.TypeKind.UnsignedLongLongInt }},
            {"LL", new[] { CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt }},
            {"LU", new[] { CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt }},
            {"L", new[] { CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt }},
            {"U", new[] { CType.BasicType.TypeKind.UnsignedInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt }},
            {"", new[] { CType.BasicType.TypeKind.SignedInt, CType.BasicType.TypeKind.UnsignedInt, CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt }},
        };

        /// <summary>
        /// 10進定数の型候補表
        /// </summary>
        private static readonly Dictionary<string, CType.BasicType.TypeKind[]> CandidateTypeTableDigit = new Dictionary<string, CType.BasicType.TypeKind[]> {
            { "LLU", new[] { CType.BasicType.TypeKind.UnsignedLongLongInt } },
            { "LL", new[] { CType.BasicType.TypeKind.SignedLongLongInt } },
            { "LU", new[] { CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt } },
            { "L", new[] { CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt } },
            { "U", new[] { CType.BasicType.TypeKind.UnsignedInt, CType.BasicType.TypeKind.UnsignedLongInt, CType.BasicType.TypeKind.UnsignedLongLongInt } },
            { "", new[] { CType.BasicType.TypeKind.SignedInt, CType.BasicType.TypeKind.SignedLongInt, CType.BasicType.TypeKind.SignedLongLongInt } },
        };

        /// <summary>
        /// 6.4.4.1 整数定数
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant IntegerConstant() {
            if (IsIntegerConstant() == false) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"整数定数があるべき場所に {_lexer.CurrentToken().Raw } があります。");
            }

            // 10進定数は基数 10，8進定数は基数 8，16進定数は基数 16 で値を計算する。
            // 字句的に先頭の 数字が最も重みが大きい。
            string raw = _lexer.CurrentToken().Raw;
            string body;
            string suffix;
            int radix;
            Dictionary<string, CType.BasicType.TypeKind[]> candidateTable;
            switch (_lexer.CurrentToken().Kind) {
                case Token.TokenKind.HEXIMAL_CONSTANT: {
                        // 16進定数
                        var m = Lexer.ParseHeximal(raw);
                        body = m.Item1;
                        suffix = m.Item2;
                        radix = 16;
                        candidateTable = CandidateTypeTableHexOrOct;
                        break;
                    }
                case Token.TokenKind.OCTAL_CONSTANT: {
                        // 8進定数
                        var m = Lexer.ParseOctal(raw);
                        body = m.Item1;
                        suffix = m.Item2;
                        radix = 8;
                        candidateTable = CandidateTypeTableHexOrOct;
                        break;
                    }
                case Token.TokenKind.DECIAML_CONSTANT: {
                        // 10進定数
                        var m = Lexer.ParseDecimal(raw);
                        body = m.Item1;
                        suffix = m.Item2;
                        radix = 10;
                        candidateTable = CandidateTypeTableDigit;
                        break;
                    }
                default:
                    throw new Exception();

            }

            CType.BasicType.TypeKind[] candidates;
            if (candidateTable.TryGetValue(suffix.ToUpper(), out candidates) == false) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "整数定数のサフィックスが不正です。");
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
                        throw new CompilerException.InternalErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"整数定数の型変換候補が不正です。");
                }
                selectedType = candidate;
                break;
            }

            _lexer.NextToken();

            return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(raw, value, selectedType);
        }

        /// <summary>
        /// 6.4.4.2 浮動小数点定数となりうるか？
        /// </summary>
        /// <returns></returns>
        private bool IsFloatingConstant() {
            return _lexer.CurrentToken().Kind == Token.TokenKind.FLOAT_CONSTANT;
        }

        /// <summary>
        /// 6.4.4.2 浮動小数点定数
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant FloatingConstant() {
            if (IsFloatingConstant() == false) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"浮動小数点定数があるべき場所に {_lexer.CurrentToken().Raw } があります。");
            }
            var raw = _lexer.CurrentToken().Raw;
            var m = Lexer.ParseFloat(raw);
            var value = Convert.ToDouble(m.Item1);
            CType.BasicType.TypeKind type;
            switch (m.Item2.ToUpper()) {
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
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"浮動小数点定数のサフィックスが不正です。");
            }
            _lexer.NextToken();
            return new SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant(raw, value, type);
        }

        /// <summary>
        /// 6.4.4.3 列挙定数となりうるか？
        /// </summary>
        /// <returns></returns>
        private bool IsEnumerationConstant() {
            if (!IsIdentifier(false)) {
                return false;
            }
            var ident = _lexer.CurrentToken();
            SyntaxTree.Declaration.EnumMemberDeclaration value;
            if (_identScope.TryGetValue(ident.Raw, out value) == false) {
                return false;
            }
            return value.MemberInfo.ParentType.Members.First(x => x.Ident == ident.Raw) != null;
        }

        /// <summary>
        /// 6.4.4.3 列挙定数
        /// </summary>
        /// <returns></returns>
        private CType.TaggedType.EnumType.MemberInfo EnumerationConstant() {
            var ident = Identifier(false);
            SyntaxTree.Declaration.EnumMemberDeclaration value;
            if (_identScope.TryGetValue(ident, out value) == false) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"列挙定数 { _lexer.CurrentToken().Raw } は未宣言です。");
            } else {
            }
            var el = value.MemberInfo.ParentType.Members.First(x => x.Ident == ident);
            return el;
        }

        /// <summary>
        /// 6.4.4.4 文字定数となりうるか？
        /// </summary>
        /// <returns></returns>
        private bool IsCharacterConstant() {
            return _lexer.CurrentToken().Kind == Token.TokenKind.STRING_CONSTANT;
        }

        /// <summary>
        /// 6.4.4.4 文字定数
        /// </summary>
        /// <returns></returns>
        private string CharacterConstant() {
            if (IsCharacterConstant() == false) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"文字定数があるべき場所に {_lexer.CurrentToken().Raw } があります。");
            }
            var ret = _lexer.CurrentToken().Raw;
            _lexer.NextToken();
            return ret;
        }

        /// <summary>
        /// 6.4.5 文字列リテラルとなりうるか？
        /// </summary>
        /// <returns></returns>
        private bool IsStringLiteral() {
            return _lexer.CurrentToken().Kind == Token.TokenKind.STRING_LITERAL;
        }

        /// <summary>
        /// 6.4.5 文字列リテラル
        /// </summary>
        /// <returns></returns>
        private string StringLiteral() {
            if (IsStringLiteral() == false) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"文字列リテラルがあるべき場所に {_lexer.CurrentToken().Raw } があります。");
            }
            var ret = _lexer.CurrentToken().Raw;
            _lexer.NextToken();
            return ret;
        }

        #endregion

        /// <summary>
        /// 6.9 外部定義(翻訳単位)
        /// </summary>
        /// <returns></returns>
        public SyntaxTree.TranslationUnit TranslationUnit() {
            var ret = new SyntaxTree.TranslationUnit();
            _insertImplictDeclarationOperatorStack.Push((decl) => {// ToDo: 共通化
                ret.Declarations.Add(decl);
                return decl;
            });
            while (IsExternalDeclaration(null, AnsiCParser.TypeSpecifier.None)) {
                ret.Declarations.AddRange(ExternalDeclaration());
            }
            _insertImplictDeclarationOperatorStack.Pop();
            EoF();

            // 翻訳単位が，ある識別子に対する仮定義を一つ以上含み，かつその識別子に対する外部定義を含まない場合，その翻訳単位に，翻訳単位の終わりの時点での合成型，
            // 及び 0 に等しい初期化子をもったその識別子のファイル有効範囲の宣言がある場合と同じ規則で動作する。
            foreach (var entry in this._linkageList) {
                if (entry.Definition == null) {
                    if (entry.TentativeDefinitions.First().StorageClass != AnsiCParser.StorageClassSpecifier.Extern) {
                        entry.Definition = entry.TentativeDefinitions[0];
                        entry.TentativeDefinitions.RemoveAt(0);
                    }
                }
            }
            ret.LinkageTable = this._linkageList;
            return ret;
        }

        /// <summary>
        /// 6.9 外部定義(外部宣言となりえるか？)
        /// </summary>
        /// <returns></returns>
        private bool IsExternalDeclaration(CType baseType, TypeSpecifier typeSpecifier) {
            return IsDeclarationSpecifier(baseType, typeSpecifier)
                 || _lexer.PeekToken(';')
                 || IsDeclarator();
        }

        /// <summary>
        /// 6.9 外部定義(外部宣言)
        /// </summary>
        /// <returns></returns>
        private List<SyntaxTree.Declaration> ExternalDeclaration() {
            return ReadDeclaration(ScopeKind.FileScope);
        }

        /// <summary>
        /// 6.9.1　関数定義
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <param name="functionSpecifier"></param>
        /// <returns></returns>
        /// <remarks>
        /// 制約
        /// - 関数定義で宣言する識別子（その関数の名前）の型が関数型であることは，その関数定義の宣言子 の部分で指定しなければならない
        /// - 関数の返却値の型は，配列型以外のオブジェクト型又は void 型でなければならない。
        /// - 宣言指定子列の中に記憶域クラス指定子がある場合，それは extern 又は static のいずれかでなければならない。
        /// - 宣言子が仮引数型並びを含む場合，それぞれの仮引数の宣言は識別子を含まなければならない。
        ///   ただし，仮引数型並びが void 型の仮引数一つだけから成る特別な場合を除く。この場合は，識別子があってはならず，更に宣言子の後ろに宣言並びが続いてはならない。
        /// </remarks>
        private SyntaxTree.Declaration FunctionDefinition(string ident, CType type, StorageClassSpecifier storageClass, FunctionSpecifier functionSpecifier) {

            // 関数定義で宣言する識別子（その関数の名前）の型が関数型であることは，その関数定義の宣言子の部分で指定しなければならない
            // (これは，関数定義の型を typedef から受け継ぐことはできないことを意味する。)。
            if (type.UnwrapTypeQualifier() is CType.TypedefedType) {
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "関数定義で宣言する識別子（その関数の名前）の型が関数型であることは，その関数定義の宣言子の部分で指定しなければならない。");
            }
            var ftype = type.Unwrap() as CType.FunctionType;

            // 宣言並びがあるなら読み取る
            var argmuents = OldStyleFunctionArgumentDeclarations();

            if (ftype.Arguments == null && argmuents != null) {
                // 識別子並び: なし
                // 宣言並び: あり
                // -> 仮引数用の宣言並びがあるのに、識別子並びがない
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "識別子並びが空なのに、宣言並びがある。");
            } else if (ftype.Arguments == null && argmuents == null) {
                // 識別子並び: なし
                // 仮引数型並び: なし
                // -> ANSI形式で引数無しの関数定義（関数宣言時とは意味が違う）
                // ToDo:警告を出すこと。
                ftype.Arguments = new CType.FunctionType.ArgumentInfo[0];
            } else if (ftype.Arguments != null && argmuents != null) {
                // 識別子並びあり、仮引数並びあり。

                // 関数宣言が古い形式であることを確認
                if (ftype.GetFunctionStyle() != CType.FunctionType.FunctionStyle.OldStyle) {
                    // 仮引数型並び、もしくは識別子並びの省略、もしくは識別子並びと仮引数型並びの混在である。
                    // 識別子並び中に型名を記述してしまった場合もこのエラーになる
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "関数定義中でK&R形式の識別子並びとANSI形式の仮引数型並びが混在している");
                }

                // 識別子並びの中に無いものが宣言並びにあるか調べる
                if (argmuents.Select(x => x.Item1).Except(ftype.Arguments.Select(x => x.Ident)).Any()) {
                    // 識別子並び中の要素以外が宣言並びにある。
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "識別子並び中の要素以外が宣言並びにある。");
                }

                // K&R形式の識別子並びに宣言並びの型情報を規定の実引数拡張を伴って反映させる。
                // 宣言並びを名前引きできる辞書に変換
                var dic = argmuents.ToDictionary(x => x.Item1, x => x);

                // 型宣言側の仮引数
                var mapped = ftype.Arguments.Select(x => {
                    if (dic.ContainsKey(x.Ident)) {
                        var dapType = dic[x.Ident].Item2.DefaultArgumentPromotion();
                        if (CType.IsEqual(dapType, dic[x.Ident].Item2) == false) {
                            throw new CompilerException.TypeMissmatchError(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"{ident}は規定の実引数拡張で型が変化します。");
                        }
                        return new CType.FunctionType.ArgumentInfo(x.Ident, dic[x.Ident].Item3, dic[x.Ident].Item2.DefaultArgumentPromotion());
                    } else {
                        return new CType.FunctionType.ArgumentInfo(x.Ident, AnsiCParser.StorageClassSpecifier.None, CType.CreateSignedInt().DefaultArgumentPromotion());
                    }
                }).ToList();

                ftype.Arguments = mapped.ToArray();

            } else if (ftype.Arguments != null && argmuents == null) {
                // 識別子並びあり

                // 関数宣言の形式を確認
                switch (ftype.GetFunctionStyle()) {
                    case CType.FunctionType.FunctionStyle.OldStyle:
                        // 関数宣言が古い形式である
                        var mapped = ftype.Arguments.Select(x => {
                            return new CType.FunctionType.ArgumentInfo(x.Ident, AnsiCParser.StorageClassSpecifier.None, CType.CreateSignedInt().DefaultArgumentPromotion());
                        }).ToList();
                        ftype.Arguments = mapped.ToArray();
                        break;
                    case CType.FunctionType.FunctionStyle.NewStyle:
                    case CType.FunctionType.FunctionStyle.AmbiguityStyle:
                        // 関数宣言が新しい形式、もしくは曖昧な形式
                        break;
                    default:
                        // 識別子並びと仮引数型並びの混在である。
                        // 識別子並び中に型名を記述してしまった場合もこのエラーになる
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "関数定義中でK&R形式の識別子並びとANSI形式の仮引数型並びが混在している");
                }

            } else {
                throw new Exception("ありえない");
            }

            // 6.9.1 関数定義

            // 宣言部分を読み取る
            var funcdecl = FunctionDeclaration(ident, ftype, storageClass, functionSpecifier, ScopeKind.FileScope, true);

            // 各スコープを積む
            _tagScope = _tagScope.Extend();
            _identScope = _identScope.Extend();
            _labelScope = _labelScope.Extend();

            // 引数をスコープに追加
            if (ftype.Arguments != null) {
                foreach (var arg in ftype.Arguments) {
                    _identScope.Add(arg.Ident, new SyntaxTree.Declaration.ArgumentDeclaration(arg.Ident, arg.Type, arg.StorageClass));    // 引数は無結合
                }
            }


            // 関数本体（複文）を解析
            // 引数変数を複文スコープに入れるために、関数側でスコープを作り、複文側でスコープを作成しないようにする。
            funcdecl.Body = CompoundStatement(skipCreateNewScope: true);


            // 未定義ラベルと未使用ラベルを調査
            foreach (var scopeValue in _labelScope.GetEnumertor()) {
                if (scopeValue.Item2.Declaration == null && scopeValue.Item2.References.Any()) {
                    // 未定義のラベルが使われている。
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"未定義のラベル {scopeValue.Item1} が使用されています。");
                }
                if (scopeValue.Item2.Declaration != null && !scopeValue.Item2.References.Any()) {
                    // 未定義のラベルが使われている。
                    Console.Error.WriteLine($"ラベル {scopeValue.Item1} は定義されていますがどこからも参照されていません。");
                }
            }

            //各スコープから出る
            _labelScope = _labelScope.Parent;
            _identScope = _identScope.Parent;
            _tagScope = _tagScope.Parent;

            return funcdecl;
        }


        /// <summary>
        /// 6.9.1　関数定義(宣言並び)
        /// </summary>
        /// <returns></returns>
        private List<Tuple<string, CType, StorageClassSpecifier>> OldStyleFunctionArgumentDeclarations() {
            if (IsOldStyleFunctionArgumentDeclaration()) {
                // 宣言並びがあるので読み取る
                var decls = new List<Tuple<string, CType, StorageClassSpecifier>>();
                while (IsOldStyleFunctionArgumentDeclaration()) {
                    decls = OldStyleFunctionArgumentDeclaration(decls);
                }
                return decls;
            } else {
                // 宣言並びがない
                return null;
            }
        }

        /// <summary>
        /// 6.9.1　関数定義(K&amp;Rの宣言要素となりうるか)
        /// </summary>
        /// <returns></returns>
        private bool IsOldStyleFunctionArgumentDeclaration() {
            return IsDeclarationSpecifiers(null, AnsiCParser.TypeSpecifier.None);
        }

        /// <summary>
        /// 6.9.1　関数定義(宣言並びを構成する宣言)
        /// </summary>
        /// <returns></returns>
        private List<Tuple<string, CType, StorageClassSpecifier>> OldStyleFunctionArgumentDeclaration(List<Tuple<string, CType, StorageClassSpecifier>> decls) {

            // 宣言子が識別子並びを含む場合，宣言並びの中の各宣言は，少なくとも一つの宣言子をもたなければならず，それらの宣言子は，識別子並びに含まれる識別子の宣言でなければならない。
            //   -> 「宣言並びの中の各宣言は，少なくとも一つの宣言子をもつ」についてはOldStyleFunctionArgumentDeclarationSpecifiers中でチェック
            //　 -> 「それらの宣言子は，識別子並びに含まれる識別子の宣言でなければならない。」は呼び出し元のFunctionDefinition中でチェック。
            // そして，識別子並びの中のすべての識別子を宣言しなければならない。
            //   -> 呼び出し元のFunctionDefinition中でチェック。
            // 型定義名として宣言された識別子を仮引数として再宣言してはならない。
            // 宣言並びの中の宣言は，register 以外の記憶域クラス指定子及び初期化を含んではならない。

            // 宣言並び中の宣言指定子列 
            StorageClassSpecifier storageClass;
            CType baseType = OldStyleFunctionArgumentDeclarationSpecifiers(out storageClass);
            if (!(storageClass == AnsiCParser.StorageClassSpecifier.Register || storageClass == AnsiCParser.StorageClassSpecifier.None)) {
                // 6.9.1 
                // 制約 宣言並びの中の宣言は，register 以外の記憶域クラス指定子及び初期化を含んではならない。
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "宣言並びの中の宣言は，register 以外の記憶域クラス指定子を含んではならない。");
            }

            // 初期化宣言子並び
            if (!_lexer.PeekToken(';')) {
                // 一つ以上の初期化宣言子がある？
                do {
                    var declaration = OldStyleFunctionArgumentInitDeclarator(baseType, storageClass);
                    // 宣言子並びは無結合なので再定義できない。
                    if (decls.Any(x => x.Item1 == declaration.Item1)) {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"宣言並び中で識別子{declaration.Item1}が再定義されました。");
                    }
                    decls.Add(declaration);
                } while (_lexer.ReadTokenIf(','));
            }
            _lexer.ReadToken(';');
            return decls;
        }

        /// <summary>
        /// 6.9.1　関数定義(宣言並びを構成する宣言の初期化宣言子)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <returns></returns>
        private Tuple<string, CType, StorageClassSpecifier> OldStyleFunctionArgumentInitDeclarator(CType type, StorageClassSpecifier storageClass) {
            // 宣言子
            string ident = "";
            List<CType> stack = new List<CType>() { new CType.StubType() };
            Declarator(ref ident, stack, 0);
            type = CType.Resolve(type, stack);

            if (CType.CheckContainOldStyleArgument(type)) {
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "関数型中に型の無い仮引数名があります");
            }

            if (_lexer.ReadTokenIf('=')) {
                // 初期化子を伴う宣言
                // 宣言並びの中の宣言は初期化を含んではならない
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "宣言並びの中の宣言は初期化を含んではならない。");
            } else if (storageClass != AnsiCParser.StorageClassSpecifier.Register && storageClass != AnsiCParser.StorageClassSpecifier.None) {
                // 6.9.1 
                // 制約 宣言並びの中の宣言は，register 以外の記憶域クラス指定子及び初期化を含んではならない。
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "宣言並びの中の宣言は，register 以外の記憶域クラス指定子を含んではならない。");
            } else if (type.IsFunctionType()) {
                // 仮引数を“～型を返却する関数”とする宣言は，6.3.2.1 の規定に従い，“～型を返却する関数へのポインタ”に型調整する。
                Console.Error.WriteLine($"仮引数{ident}は“～型を返却する関数”として宣言されていますが，6.3.2.1 の規定に従い，“～型を返却する関数へのポインタ”に型調整します。");
                return Tuple.Create(ident, (CType)CType.CreatePointer(type), storageClass);
            } else {
                return Tuple.Create(ident, type, storageClass);
            }
        }

        /// <summary>
        /// 6.9.1　関数定義(古い形式の関数定義における宣言指定子列)
        /// </summary>
        /// <param name="sc"></param>
        /// <returns></returns>
        private CType OldStyleFunctionArgumentDeclarationSpecifiers(out StorageClassSpecifier sc) {
            CType baseType = null;
            StorageClassSpecifier storageClass = AnsiCParser.StorageClassSpecifier.None;
            FunctionSpecifier functionSpecifier = AnsiCParser.FunctionSpecifier.None;

            if (ReadDeclarationSpecifiers(ref baseType, ref storageClass, ref functionSpecifier, ReadDeclarationSpecifierPartFlag.DeclarationSpecifiers) < 1) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "記憶クラス指定子 /型指定子/型修飾子が一つ以上指定されている必要がある。");
            }
            System.Diagnostics.Debug.Assert(functionSpecifier == AnsiCParser.FunctionSpecifier.None);

            sc = storageClass;
            return baseType;
        }

        /// <summary>
        /// 関数宣言、変数宣言、もしくは型定義のいずれかのケースを解析する。関数定義は含まれない
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <param name="functionSpecifier"></param>
        /// <param name="scopeKind"></param>
        /// <returns></returns>
        private SyntaxTree.Declaration FunctionOrVariableOrTypedefDeclaration(
            string ident, 
            CType type, 
            StorageClassSpecifier storageClass, 
            FunctionSpecifier functionSpecifier,
            ScopeKind scopeKind
        ) {
            if (_lexer.ReadTokenIf('=')) {
                // 初期化子を伴うので変数の定義
                return VariableDeclaration(ident, type, storageClass, scopeKind, true);
            } else {
                // 初期化式を伴わないため、関数宣言、Typedef宣言、変数の仮定義のどれか
                // オブジェクトに対する識別子の宣言が仮定義であり，内部結合をもつ場合，その宣言の型は不完全型であってはならない。

                CType.FunctionType functionType;
                if (type.IsFunctionType(out functionType) && functionType.GetFunctionStyle() == CType.FunctionType.FunctionStyle.OldStyle) {
                    // 6.7.5.3 関数宣言子（関数原型を含む）
                    // 関数定義の一部でない関数宣言子における識別子並びは，空でなければならない。
                    // 脚注　関数宣言で古い形式は使えない。
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "関数定義の一部でない関数宣言子における識別子並びは，空でなければならない。");
                }

                if (storageClass == AnsiCParser.StorageClassSpecifier.Typedef) {
                    // 型宣言名
                    return TypedefDeclaration(ident, type);
                } else if (type.IsFunctionType()) {
                    // 関数宣言
                    return FunctionDeclaration(ident, type, storageClass, functionSpecifier, scopeKind, false);
                } else {
                    // 変数の仮定義(tentative definition)
                    return VariableDeclaration(ident, type, storageClass, scopeKind, false);
                }
            }
        }

        /// <summary>
        /// 6.7 宣言となりうるか？
        /// </summary>
        /// <returns></returns>
        private bool IsDeclaration() {
            return IsDeclarationSpecifiers(null, AnsiCParser.TypeSpecifier.None);
        }

        /// <summary>
        /// 6.7 宣言(宣言)
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// これは複文中に登場する宣言の解析なので、ローカルスコープとして解析する
        /// </remarks>
        private List<SyntaxTree.Declaration> Declaration() {
            return ReadDeclaration(ScopeKind.BlockScope);
        }

        /// <summary>
        /// 6.7 宣言(宣言指定子列になりうるか)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeSpecifier"></param>
        /// <returns></returns>
        private bool IsDeclarationSpecifiers(CType type, TypeSpecifier typeSpecifier) {
            return IsDeclarationSpecifier(type, typeSpecifier);
        }

        /// <summary>
        /// 6.7 宣言(宣言指定子列)
        /// </summary>
        /// <param name="sc"></param>
        /// <returns></returns>
        private CType DeclarationSpecifiers(out StorageClassSpecifier sc) {
            CType baseType = null;
            StorageClassSpecifier storageClass = AnsiCParser.StorageClassSpecifier.None;
            FunctionSpecifier functionSpecifier = AnsiCParser.FunctionSpecifier.None;

            if (ReadDeclarationSpecifiers(ref baseType, ref storageClass, ref functionSpecifier, ReadDeclarationSpecifierPartFlag.DeclarationSpecifiers) < 1) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "記憶クラス指定子 /型指定子/型修飾子が一つ以上指定されている必要がある。");
            }
            System.Diagnostics.Debug.Assert(functionSpecifier == AnsiCParser.FunctionSpecifier.None);

            sc = storageClass;
            return baseType;
        }

        /// <summary>
        /// 6.7 宣言(宣言指定子要素になりうるか)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeSpecifier"></param>
        /// <returns></returns>
        private bool IsDeclarationSpecifier(CType type, TypeSpecifier typeSpecifier) {
            return (IsStorageClassSpecifier() ||
                (IsTypeSpecifier() && type == null) ||
                (IsStructOrUnionSpecifier() && type == null) ||
                (IsEnumSpecifier() && type == null) ||
                (IsTypedefName() && type == null && typeSpecifier == AnsiCParser.TypeSpecifier.None) ||
                IsTypeQualifier() ||
                IsFunctionSpecifier());
        }

        /// <summary>
        /// 6.7.1 記憶域クラス指定子になりうるか
        /// </summary>
        /// <returns></returns>
        private bool IsStorageClassSpecifier() {
            return _lexer.PeekToken(Token.TokenKind.AUTO, Token.TokenKind.REGISTER, Token.TokenKind.STATIC, Token.TokenKind.EXTERN, Token.TokenKind.TYPEDEF);
        }

        /// <summary>
        /// 6.7.1 記憶域クラス指定子
        /// </summary>
        /// <returns></returns>
        private StorageClassSpecifier StorageClassSpecifier() {
            switch (_lexer.CurrentToken().Kind) {
                case Token.TokenKind.AUTO:
                    _lexer.NextToken();
                    return AnsiCParser.StorageClassSpecifier.Auto;
                case Token.TokenKind.REGISTER:
                    _lexer.NextToken();
                    return AnsiCParser.StorageClassSpecifier.Register;
                case Token.TokenKind.STATIC:
                    _lexer.NextToken();
                    return AnsiCParser.StorageClassSpecifier.Static;
                case Token.TokenKind.EXTERN:
                    _lexer.NextToken();
                    return AnsiCParser.StorageClassSpecifier.Extern;
                case Token.TokenKind.TYPEDEF:
                    _lexer.NextToken();
                    return AnsiCParser.StorageClassSpecifier.Typedef;
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// 6.7.2 型指定子になりうるか
        /// </summary>
        /// <returns></returns>
        private bool IsTypeSpecifier() {
            return _lexer.PeekToken(Token.TokenKind.VOID, Token.TokenKind.CHAR, Token.TokenKind.INT, Token.TokenKind.FLOAT, Token.TokenKind.DOUBLE, Token.TokenKind.SHORT, Token.TokenKind.LONG, Token.TokenKind.SIGNED, Token.TokenKind.UNSIGNED);
        }

        /// <summary>
        /// 匿名型に割り当てる名前を生成するためのカウンター
        /// </summary>
        private int anony = 0;

        /// <summary>
        /// 6.7.2 型指定子
        /// </summary>
        /// <returns></returns>
        private TypeSpecifier TypeSpecifier() {
            switch (_lexer.CurrentToken().Kind) {
                case Token.TokenKind.VOID:
                    _lexer.NextToken();
                    return AnsiCParser.TypeSpecifier.Void;
                case Token.TokenKind.CHAR:
                    _lexer.NextToken();
                    return AnsiCParser.TypeSpecifier.Char;
                case Token.TokenKind.INT:
                    _lexer.NextToken();
                    return AnsiCParser.TypeSpecifier.Int;
                case Token.TokenKind.FLOAT:
                    _lexer.NextToken();
                    return AnsiCParser.TypeSpecifier.Float;
                case Token.TokenKind.DOUBLE:
                    _lexer.NextToken();
                    return AnsiCParser.TypeSpecifier.Double;
                case Token.TokenKind.SHORT:
                    _lexer.NextToken();
                    return AnsiCParser.TypeSpecifier.Short;
                case Token.TokenKind.LONG:
                    _lexer.NextToken();
                    return AnsiCParser.TypeSpecifier.Long;
                case Token.TokenKind.SIGNED:
                    _lexer.NextToken();
                    return AnsiCParser.TypeSpecifier.Signed;
                case Token.TokenKind.UNSIGNED:
                    _lexer.NextToken();
                    return AnsiCParser.TypeSpecifier.Unsigned;
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// 6.7.2.1 構造体指定子及び共用体指定子になりうるか
        /// </summary>
        /// <returns></returns>
        private bool IsStructOrUnionSpecifier() {
            return _lexer.PeekToken(Token.TokenKind.STRUCT, Token.TokenKind.UNION);
        }

        /// <summary>
        /// 6.7.2.1 構造体指定子及び共用体指定子（構造体共用体指定子）
        /// </summary>
        /// <returns></returns>
        private CType StructOrUnionSpecifier() {
            // 構造体/共用体
            var kind = _lexer.ReadToken(Token.TokenKind.STRUCT, Token.TokenKind.UNION).Kind == Token.TokenKind.STRUCT ? CType.TaggedType.StructUnionType.StructOrUnion.Struct : CType.TaggedType.StructUnionType.StructOrUnion.Union;

            // 識別子の有無で分岐
            if (IsIdentifier(true)) {

                var ident = Identifier(true);

                // 波括弧の有無で分割
                if (_lexer.ReadTokenIf('{')) {
                    // 識別子を伴う完全型の宣言
                    CType.TaggedType tagType;
                    CType.TaggedType.StructUnionType structUnionType;
                    if (_tagScope.TryGetValue(ident, out tagType) == false) {
                        // タグ名前表に無い場合は新しく追加する。
                        structUnionType = new CType.TaggedType.StructUnionType(kind, ident, false);
                        _tagScope.Add(ident, structUnionType);
                        AddImplictTypeDeclaration(ident, structUnionType);
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
                    structUnionType.Members = StructDeclarations();
                    _lexer.ReadToken('}');

                    return structUnionType;
                } else {
                    // 不完全型の宣言
                    CType.TaggedType tagType;
                    if (_tagScope.TryGetValue(ident, out tagType) == false) {
                        // タグ名前表に無い場合は新しく追加する。
                        tagType = new CType.TaggedType.StructUnionType(kind, ident, false);
                        _tagScope.Add(ident, tagType);
                        AddImplictTypeDeclaration(ident, tagType);
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
                AddImplictTypeDeclaration(ident, structUnionType);

                // メンバ宣言並びを解析する
                _lexer.ReadToken('{');
                structUnionType.Members = StructDeclarations();
                _lexer.ReadToken('}');
                return structUnionType;
            }
        }

        /// <summary>
        /// 6.7.2.1 構造体指定子及び共用体指定子(メンバ宣言並び)
        /// </summary>
        /// <returns></returns>
        private List<CType.TaggedType.StructUnionType.MemberInfo> StructDeclarations() {
            var items = new List<CType.TaggedType.StructUnionType.MemberInfo>();
            items.AddRange(StructDeclaration());
            while (IsStructDeclaration()) {
                items.AddRange(StructDeclaration());
            }
            return items;
        }

        /// <summary>
        /// 6.7.2.1 構造体指定子及び共用体指定子(メンバ宣言)となりうるか
        /// </summary>
        /// <returns></returns>
        private bool IsStructDeclaration() {
            return IsSpecifierQualifiers();
        }

        /// <summary>
        /// 6.7.2.1 構造体指定子及び共用体指定子(メンバ宣言)
        /// </summary>
        /// <returns></returns>
        private List<CType.TaggedType.StructUnionType.MemberInfo> StructDeclaration() {
            CType baseType = SpecifierQualifiers();
            var ret = StructDeclaratorList(baseType);
            _lexer.ReadToken(';');
            return ret;
        }

        /// <summary>
        /// 6.7.2.1 型指定子型修飾子並びとなりうるか
        /// </summary>
        /// <returns></returns>
        private bool IsSpecifierQualifiers() {
            return IsSpecifierQualifier(null, AnsiCParser.TypeSpecifier.None);
        }

        /// <summary>
        /// 6.7.2.1 型指定子型修飾子並び
        /// </summary>
        /// <returns></returns>
        private CType SpecifierQualifiers() {
            CType baseType = null;
            StorageClassSpecifier storageClass = AnsiCParser.StorageClassSpecifier.None;
            FunctionSpecifier functionSpecifier = AnsiCParser.FunctionSpecifier.None;
            if (ReadDeclarationSpecifiers(ref baseType, ref storageClass, ref functionSpecifier, ReadDeclarationSpecifierPartFlag.SpecifierQualifiers) < 1) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "型指定子/型修飾子が一つ以上指定されている必要がある。");
            }
            System.Diagnostics.Debug.Assert(storageClass == AnsiCParser.StorageClassSpecifier.None);
            System.Diagnostics.Debug.Assert(functionSpecifier == AnsiCParser.FunctionSpecifier.None);

            return baseType;
        }

        /// <summary>
        /// 6.7.2.1 型指定子型修飾子並び（型指定子もしくは型修飾子となりうるか？）
        /// </summary>
        /// <param name="type"></param>
        /// <param name="typeSpecifier"></param>
        /// <returns></returns>
        private bool IsSpecifierQualifier(CType type, TypeSpecifier typeSpecifier) {
            return (
                (IsTypeSpecifier() && type == null) ||
                (IsStructOrUnionSpecifier() && type == null) ||
                (IsEnumSpecifier() && type == null) ||
                (IsTypedefName() && type == null && typeSpecifier == AnsiCParser.TypeSpecifier.None) ||
                IsTypeQualifier());
        }

        /// <summary>
        /// 6.7.2.1 型指定子型修飾子並び（メンバ宣言子並び）
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private List<CType.TaggedType.StructUnionType.MemberInfo> StructDeclaratorList(CType type) {
            var ret = new List<CType.TaggedType.StructUnionType.MemberInfo>();
            ret.Add(StructDeclarator(type));
            while (_lexer.ReadTokenIf(',')) {
                ret.Add(StructDeclarator(type));
            }
            return ret;
        }

        /// <summary>
        /// 6.7.2.1 型指定子型修飾子並び（メンバ宣言子）
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private CType.TaggedType.StructUnionType.MemberInfo StructDeclarator(CType type) {
            if (IsDeclarator()) {
                // 宣言子
                List<CType> stack = new List<CType>() { new CType.StubType() };
                string ident = null;
                Declarator(ref ident, stack, 0);
                type = CType.Resolve(type, stack);
                if (CType.CheckContainOldStyleArgument(type)) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "関数型中に型の無い仮引数名があります");
                }

                // ビットフィールド部分(opt)
                int? size = null;
                if (_lexer.ReadTokenIf(':')) {
                    var expr = ConstantExpression();
                    var ret = Evaluator.ConstantEval(expr);
                    size = (int?)(ret as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)?.Value;
                    if (size.HasValue == false || size < 0) {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "ビットフィールドには０以上の整数を指定してください。");
                    }
                }

                return new CType.TaggedType.StructUnionType.MemberInfo(ident, type, size);
            } else if (_lexer.ReadTokenIf(':')) {
                // ビットフィールド部分(must)
                int? size = null;
                var expr = ConstantExpression();
                var ret = Evaluator.ConstantEval(expr);
                size = (int?)(ret as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)?.Value;
                if (size.HasValue == false || size < 0) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "ビットフィールドには０以上の整数を指定してください。");
                }

                return new CType.TaggedType.StructUnionType.MemberInfo(null, type, size);
            } else {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "構造体/共用体のメンバ宣言子では、宣言子とビットフィールド部の両方を省略することはできません。無名構造体/共用体を使用できるのは規格上はC11からです。(C11 6.7.2.1で規定)。");
            }
        }

        /// <summary>
        /// 6.7.2.2 列挙型指定子となりうるか
        /// </summary>
        /// <returns></returns>
        private bool IsEnumSpecifier() {
            return _lexer.PeekToken(Token.TokenKind.ENUM);
        }

        /// <summary>
        /// 6.7.2.2 列挙型指定子
        /// </summary>
        /// <returns></returns>
        private CType EnumSpecifier() {
            _lexer.ReadToken(Token.TokenKind.ENUM);

            if (IsIdentifier(true)) {
                var ident = Identifier(true);
                CType.TaggedType taggedType;
                if (_tagScope.TryGetValue(ident, out taggedType) == false) {
                    // タグ名前表に無い場合は新しく追加する。
                    taggedType = new CType.TaggedType.EnumType(ident, false);
                    _tagScope.Add(ident, taggedType);
                    AddImplictTypeDeclaration(ident, taggedType);

                } else if (!(taggedType is CType.TaggedType.EnumType)) {
                    throw new Exception($"列挙型 {ident} は既に構造体/共用体として定義されています。");
                } else {

                }
                if (_lexer.ReadTokenIf('{')) {
                    if ((taggedType as CType.TaggedType.EnumType).Members != null) {
                        throw new Exception($"列挙型 {ident} は既に完全型として定義されています。");
                    } else {
                        // 不完全型として定義されているので完全型にするために書き換え対象とする
                        (taggedType as CType.TaggedType.EnumType).Members = EnumeratorList(taggedType as CType.TaggedType.EnumType);
                        _lexer.ReadToken('}');
                    }
                }
                return taggedType;
            } else {
                var ident = $"$enum_{anony++}";
                var etype = new CType.TaggedType.EnumType(ident, true);
                _tagScope.Add(ident, etype);
                AddImplictTypeDeclaration(ident, etype);
                _lexer.ReadToken('{');
                EnumeratorList(etype);
                _lexer.ReadToken('}');
                return etype;
            }
        }

        /// <summary>
        /// 6.7.2.2 列挙型指定子（列挙子並び）
        /// </summary>
        /// <param name="enumType"></param>
        private List<CType.TaggedType.EnumType.MemberInfo> EnumeratorList(CType.TaggedType.EnumType enumType) {
            var ret = new List<CType.TaggedType.EnumType.MemberInfo>();
            enumType.Members = ret;
            var e = Enumerator(enumType, 0);
            var decl = new SyntaxTree.Declaration.EnumMemberDeclaration(e);
            _identScope.Add(e.Ident, decl);
            ret.Add(e);
            while (_lexer.ReadTokenIf(',')) {
                var i = e.Value + 1;
                if (IsEnumerator() == false) {
                    if (Mode == LanguageMode.C89) {
                        Console.Error.WriteLine("列挙子並びの末尾のコンマを付けることができるのは c99 以降です。c89では使えません。");
                    }
                    break;
                }
                e = Enumerator(enumType, i);
                decl = new SyntaxTree.Declaration.EnumMemberDeclaration(e);
                _identScope.Add(e.Ident, decl);
                ret.Add(e);
            }
            return ret;
        }

        /// <summary>
        /// 6.7.2.2 列挙型指定子（列挙子となりうるか）
        /// </summary>
        /// <returns></returns>
        private bool IsEnumerator() {
            return IsIdentifier(false);
        }

        /// <summary>
        /// 6.7.2.2 列挙型指定子（列挙子）
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private CType.TaggedType.EnumType.MemberInfo Enumerator(CType.TaggedType.EnumType enumType, int i) {
            var ident = Identifier(false);
            if (_lexer.ReadTokenIf('=')) {
                var expr = ConstantExpression();
                var ret = Evaluator.ConstantEval(expr);
                int? size = (int?)(ret as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)?.Value;
                if (size.HasValue == false || size < 0) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "列挙定数には整数を指定してください。");
                }
                i = size.Value;
            }
            return new CType.TaggedType.EnumType.MemberInfo(enumType, ident, i);
        }

        /// <summary>
        /// 6.7.3 型修飾子となりうるか
        /// </summary>
        /// <returns></returns>
        private bool IsTypeQualifier() {
            return _lexer.PeekToken(Token.TokenKind.CONST, Token.TokenKind.VOLATILE, Token.TokenKind.RESTRICT, Token.TokenKind.NEAR, Token.TokenKind.FAR);
        }

        /// <summary>
        /// 6.7.3 型修飾子
        /// </summary>
        /// <returns></returns>
        private TypeQualifier TypeQualifier() {
            switch (_lexer.CurrentToken().Kind) {
                case Token.TokenKind.CONST:
                    _lexer.NextToken();
                    return AnsiCParser.TypeQualifier.Const;
                case Token.TokenKind.VOLATILE:
                    _lexer.NextToken();
                    return AnsiCParser.TypeQualifier.Volatile;
                case Token.TokenKind.RESTRICT:
                    _lexer.NextToken();
                    return AnsiCParser.TypeQualifier.Restrict;
                case Token.TokenKind.NEAR:
                    _lexer.NextToken();
                    return AnsiCParser.TypeQualifier.Near;
                case Token.TokenKind.FAR:
                    _lexer.NextToken();
                    return AnsiCParser.TypeQualifier.Far;
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// 6.7.4 関数指定子となりうるか
        /// </summary>
        /// <returns></returns>
        private bool IsFunctionSpecifier() {
            return _lexer.PeekToken(Token.TokenKind.INLINE);
        }

        /// <summary>
        /// 6.7.4 関数指定子
        /// </summary>
        /// <returns></returns>
        private FunctionSpecifier FunctionSpecifier() {
            switch (_lexer.CurrentToken().Kind) {
                case Token.TokenKind.INLINE:
                    _lexer.NextToken();
                    return AnsiCParser.FunctionSpecifier.Inline;
                default:
                    throw new Exception();
            }
        }

        /// <summary>
        /// 型名となりうるか
        /// </summary>
        /// <returns></returns>
        private bool IsTypedefName() {
            SyntaxTree.Declaration.TypeDeclaration typeDeclaration;
            return _lexer.CurrentToken().Kind == Token.TokenKind.IDENTIFIER
                && _identScope.TryGetValue(_lexer.CurrentToken().Raw, out typeDeclaration);
        }

        /// <summary>
        /// 6.7.5 宣言子となりうるか
        /// </summary>
        /// <returns></returns>
        private bool IsDeclarator() {
            return IsPointer() || IsDirectDeclarator();
        }

        /// <summary>
        /// 6.7.5 宣言子
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void Declarator(ref string ident, List<CType> stack, int index) {
            if (IsPointer()) {
                Pointer(stack, index);
            }
            DirectDeclarator(ref ident, stack, index);
        }

        /// <summary>
        /// 6.7.5 宣言子(直接宣言子となりうるか)
        /// </summary>
        /// <returns></returns>
        private bool IsDirectDeclarator() {
            return _lexer.PeekToken('(') || IsIdentifier(true);
        }

        /// <summary>
        /// 6.7.5 宣言子(直接宣言子の前半部分)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void DirectDeclarator(ref string ident, List<CType> stack, int index) {
            if (IsDirectDeclarator() == false) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"宣言子が来るべき場所に{_lexer.CurrentToken().Raw}があります。");
            }
            if (_lexer.ReadTokenIf('(')) {
                stack.Add(new CType.StubType());
                Declarator(ref ident, stack, index + 1);
                _lexer.ReadToken(')');
            } else {
                ident = _lexer.CurrentToken().Raw;
                _lexer.NextToken();
            }
            MoreDirectDeclarator(stack, index);
        }

        /// <summary>
        /// 6.7.5 宣言子(直接宣言子の後半部分)
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void MoreDirectDeclarator(List<CType> stack, int index) {
            if (_lexer.ReadTokenIf('[')) {
                // 6.7.5.2 配列宣言子
                // ToDo: AnsiC範囲のみ対応
                int len = -1;
                if (_lexer.PeekToken(']') == false) {
                    var expr = ConstantExpression();
                    var ret = Evaluator.ConstantEval(expr);
                    var size = (int?)(ret as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)?.Value;
                    if (size.HasValue == false || size < 0) {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "配列の要素数には０以上の整数を指定してください。");
                    }
                    len = size.Value;
                }
                _lexer.ReadToken(']');
                MoreDirectDeclarator(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            } else if (_lexer.ReadTokenIf('(')) {
                // 6.7.5.3 関数宣言子（関数原型を含む）
                if (_lexer.ReadTokenIf(')')) {
                    // 識別子並びの省略
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                    MoreDirectDeclarator(stack, index);
                } else if (is_identifier_list()) {
                    // 識別子並び
                    var args = IdentifierList().Select(x => new CType.FunctionType.ArgumentInfo(x, AnsiCParser.StorageClassSpecifier.None, new CType.BasicType(AnsiCParser.TypeSpecifier.None))).ToList();
                    _lexer.ReadToken(')');
                    stack[index] = new CType.FunctionType(args, false, stack[index]);
                    MoreDirectDeclarator(stack, index);
                } else {
                    // 仮引数型並び
                    bool vargs = false;
                    var args = ParameterTypeList(ref vargs);
                    _lexer.ReadToken(')');
                    stack[index] = new CType.FunctionType(args, vargs, stack[index]);
                    MoreDirectDeclarator(stack, index);

                }
            } else {
                //_epsilon_
            }
        }

        /// <summary>
        /// 6.7.5 宣言子(仮引数型並びとなりうるか)
        /// </summary>
        /// <returns></returns>
        private bool IsParameterTypeList() {
            return IsParameterDeclaration();
        }

        /// <summary>
        /// 6.7.5 宣言子(仮引数型並び)
        /// </summary>
        /// <returns></returns>
        private List<CType.FunctionType.ArgumentInfo> ParameterTypeList(ref bool vargs) {
            var items = new List<CType.FunctionType.ArgumentInfo>();
            items.Add(ParameterDeclaration());
            while (_lexer.ReadTokenIf(',')) {
                if (_lexer.ReadTokenIf(Token.TokenKind.ELLIPSIS)) {
                    vargs = true;
                    break;
                } else {
                    items.Add(ParameterDeclaration());
                }
            }
            return items;
        }

        /// <summary>
        /// 6.7.5 宣言子(仮引数並びとなりうるか)
        /// </summary>
        /// <returns></returns>
        private bool IsParameterDeclaration() {
            return IsDeclarationSpecifier(null, AnsiCParser.TypeSpecifier.None);
        }

        /// <summary>
        /// 6.7.5 宣言子(仮引数並び)
        /// </summary>
        /// <returns></returns>
        private CType.FunctionType.ArgumentInfo ParameterDeclaration() {
            StorageClassSpecifier storageClass;
            CType baseType = DeclarationSpecifiers(out storageClass);


            // 6.7.5.3 関数宣言子（関数原型を含む)
            // 制約 仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。
            if (storageClass != AnsiCParser.StorageClassSpecifier.None && storageClass != AnsiCParser.StorageClassSpecifier.Register) {
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。");
            }

            string ident = null;
            if (IsDeclaratorOrAbstractDeclarator()) {
                List<CType> stack = new List<CType>() { new CType.StubType() };
                DeclaratorOrAbstractDeclarator(ref ident, stack, 0);
                baseType = CType.Resolve(baseType, stack);
            }
            if (CType.CheckContainOldStyleArgument(baseType)) {
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "関数型中に型の無い仮引数名があります");
            }
            return new CType.FunctionType.ArgumentInfo(ident, storageClass, baseType);

        }

        /// <summary>
        /// 6.7.5 宣言子(宣言子もしくは抽象宣言子となりうるか)
        /// </summary>
        /// <returns></returns>
        private bool IsDeclaratorOrAbstractDeclarator() {
            return IsPointer() || IsDirectDeclaratorOrDirectAbstractDeclarator();
        }

        /// <summary>
        /// 6.7.5 宣言子(宣言子もしくは抽象宣言子)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void DeclaratorOrAbstractDeclarator(ref string ident, List<CType> stack, int index) {
            if (IsPointer()) {
                Pointer(stack, index);
                if (IsDirectDeclaratorOrDirectAbstractDeclarator()) {
                    DirectDeclaratorOrDirectAbstractDeclarator(ref ident, stack, index);
                }
            } else {
                DirectDeclaratorOrDirectAbstractDeclarator(ref ident, stack, index);
            }
        }

        /// <summary>
        /// 6.7.5 宣言子(直接宣言子もしくは直接抽象宣言子となりうるか)
        /// </summary>
        /// <returns></returns>
        private bool IsDirectDeclaratorOrDirectAbstractDeclarator() {
            return IsIdentifier(true) || _lexer.PeekToken('(', '[');
        }

        /// <summary>
        /// 6.7.5 宣言子(直接宣言子もしくは直接抽象宣言子の前半)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void DirectDeclaratorOrDirectAbstractDeclarator(ref string ident, List<CType> stack, int index) {
            if (IsIdentifier(true)) {
                // 識別子
                ident = Identifier(true);
                MoreDdOrDad(stack, index);
            } else if (_lexer.ReadTokenIf('(')) {
                if (_lexer.PeekToken(')')) {
                    // 識別子並びの省略
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                } else if (IsParameterTypeList()) {
                    // 仮引数型並び
                    bool vargs = false;
                    var args = ParameterTypeList(ref vargs);
                    stack[index] = new CType.FunctionType(args, vargs, stack[index]);
                } else {
                    // 直接宣言子 中の '(' 宣言子 ')'  もしくは 直接抽象宣言子 中の '(' 抽象宣言子 ')'
                    stack.Add(new CType.StubType());
                    DeclaratorOrAbstractDeclarator(ref ident, stack, index + 1);
                }
                _lexer.ReadToken(')');
                MoreDdOrDad(stack, index);
            } else if (_lexer.ReadTokenIf('[')) {
                int len = -1;
                if (_lexer.PeekToken(']') == false) {
                    var expr = ConstantExpression();
                    var ret = Evaluator.ConstantEval(expr);
                    var size = (int?)(ret as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)?.Value;
                    if (size.HasValue == false || size < 0) {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "配列の要素数には０以上の整数を指定してください。");
                    }
                    len = size.Value;
                }
                _lexer.ReadToken(']');
                MoreDdOrDad(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            } else {
                throw new Exception();
            }

        }

        /// <summary>
        /// 6.7.5 宣言子(直接宣言子もしくは直接抽象宣言子の後半)
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void MoreDdOrDad(List<CType> stack, int index) {
            if (_lexer.ReadTokenIf('(')) {
                if (_lexer.PeekToken(')')) {
                    // 識別子並びの省略
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                } else if (IsParameterTypeList()) {
                    // 仮引数型並び
                    bool vargs = false;
                    var args = ParameterTypeList(ref vargs);
                    stack[index] = new CType.FunctionType(args, vargs, stack[index]);
                } else {
                    // 識別子並び
                    var args = IdentifierList().Select(x => new CType.FunctionType.ArgumentInfo(x, AnsiCParser.StorageClassSpecifier.None, new CType.BasicType(AnsiCParser.TypeSpecifier.None))).ToList();
                    stack[index] = new CType.FunctionType(args, false, stack[index]);
                }
                _lexer.ReadToken(')');
                MoreDdOrDad(stack, index);
            } else if (_lexer.ReadTokenIf('[')) {
                int len = -1;
                if (_lexer.PeekToken(']') == false) {
                    var expr = ConstantExpression();
                    var ret = Evaluator.ConstantEval(expr);
                    var size = (int?)(ret as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)?.Value;
                    if (size.HasValue == false || size < 0) {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "配列の要素数には０以上の整数を指定してください。");
                    }
                    len = size.Value;
                }
                _lexer.ReadToken(']');
                MoreDdOrDad(stack, index);
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
            return IsIdentifier(false);
        }

        /// <summary>
        /// 6.7.5 宣言子(識別子並び)
        /// </summary>
        /// <returns></returns>
        private List<string> IdentifierList() {
            var items = new List<string>();
            items.Add(Identifier(false));
            while (_lexer.ReadTokenIf(',')) {
                items.Add(Identifier(false));
            }
            return items;
        }

        /// <summary>
        /// 6.7.5.1 ポインタ宣言子となりうるか
        /// </summary>
        /// <returns></returns>
        private bool IsPointer() {
            return _lexer.PeekToken('*');
        }

        /// <summary>
        /// 6.7.5.1 ポインタ宣言子
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void Pointer(List<CType> stack, int index) {
            _lexer.ReadToken('*');
            stack[index] = CType.CreatePointer(stack[index]);
            TypeQualifier typeQualifier = AnsiCParser.TypeQualifier.None;
            while (IsTypeQualifier()) {
                typeQualifier = typeQualifier.Marge(TypeQualifier());
            }
            stack[index] = stack[index].WrapTypeQualifier(typeQualifier);

            if (IsPointer()) {
                Pointer(stack, index);
            }
        }

        /// <summary>
        /// 6.7.6 型名(型名)となりうるか？
        /// </summary>
        /// <returns></returns>
        private bool IsTypeName() {
            return IsSpecifierQualifiers();
        }

        /// <summary>
        /// 6.7.6 型名(型名)
        /// </summary>
        /// <returns></returns>
        private CType TypeName() {
            CType baseType = SpecifierQualifiers();
            if (IsAbstractDeclarator()) {
                List<CType> stack = new List<CType>() { new CType.StubType() };
                AbstractDeclarator(stack, 0);
                baseType = CType.Resolve(baseType, stack);
            }
            return baseType;
        }

        /// <summary>
        /// 6.7.6 型名(抽象宣言子)となりうるか？
        /// </summary>
        /// <returns></returns>
        private bool IsAbstractDeclarator() {
            return (IsPointer() || IsDirectAbstractDeclarator());
        }

        /// <summary>
        /// 6.7.6 型名(抽象宣言子)
        /// </summary>
        /// <returns></returns>
        private void AbstractDeclarator(List<CType> stack, int index) {
            if (IsPointer()) {
                Pointer(stack, index);
                if (IsDirectAbstractDeclarator()) {
                    DirectAbstractDeclarator(stack, index);
                }
            } else {
                DirectAbstractDeclarator(stack, index);
            }
        }

        /// <summary>
        /// 6.7.6 型名(直接抽象宣言子を構成する前半の要素)となりうるか？
        /// </summary>
        /// <returns></returns>
        private bool IsDirectAbstractDeclarator() {
            return _lexer.PeekToken('(', '[');
        }

        /// <summary>
        /// 6.7.6 型名(直接抽象宣言子を構成する前半の要素)
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void DirectAbstractDeclarator(List<CType> stack, int index) {
            if (_lexer.ReadTokenIf('(')) {
                if (IsAbstractDeclarator()) {
                    stack.Add(new CType.StubType());
                    AbstractDeclarator(stack, index + 1);
                } else if (_lexer.PeekToken(')') == false) {
                    // ansi args
                    bool vargs = false;
                    var args = ParameterTypeList(ref vargs);
                    stack[index] = new CType.FunctionType(args, vargs, stack[index]);
                } else {
                    // k&r or ansi
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                }
                _lexer.ReadToken(')');
                MoreDirectAbstractDeclarator(stack, index);
            } else {
                _lexer.ReadToken('[');
                int len = -1;
                if (_lexer.PeekToken(']') == false) {
                    var expr = ConstantExpression();
                    var ret = Evaluator.ConstantEval(expr);
                    var size = (int?)(ret as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)?.Value;
                    if (size.HasValue == false || size < 0) {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "配列の要素数には０以上の整数を指定してください。");
                    }
                    len = size.Value;
                }
                _lexer.ReadToken(']');
                MoreDirectAbstractDeclarator(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            }
        }

        /// <summary>
        /// 6.7.6 型名(直接抽象宣言子を構成する後半の要素)
        /// </summary>
        /// <param name="stack"></param>
        /// <param name="index"></param>
        private void MoreDirectAbstractDeclarator(List<CType> stack, int index) {
            if (_lexer.ReadTokenIf('[')) {
                int len = -1;
                if (_lexer.PeekToken(']') == false) {
                    var expr = ConstantExpression();
                    var ret = Evaluator.ConstantEval(expr);
                    var size = (int?)(ret as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)?.Value;
                    if (size.HasValue == false || size < 0) {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "配列の要素数には０以上の整数を指定してください。");
                    }
                    len = size.Value;
                }
                _lexer.ReadToken(']');
                MoreDirectAbstractDeclarator(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            } else if (_lexer.ReadTokenIf('(')) {
                if (_lexer.PeekToken(')') == false) {
                    bool vargs = false;
                    var items = ParameterTypeList(ref vargs);
                    stack[index] = new CType.FunctionType(items, vargs, stack[index]);
                } else {
                    stack[index] = new CType.FunctionType(null, false, stack[index]);
                }
                _lexer.ReadToken(')');
                MoreDirectAbstractDeclarator(stack, index);
            } else {
                // _epsilon_
            }
        }

        /// <summary>
        /// 6.7.7 型定義(型定義名)
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private bool IsTypedefedType(string v) {
            SyntaxTree.Declaration.TypeDeclaration typeDeclaration;
            return _identScope.TryGetValue(v, out typeDeclaration);
        }

        #region 6.7.8 初期化(初期化式の型検査)
        public static class InitializerChecker {
        /// <summary>
        /// 単純初期化式
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ast"></param>
        private static SyntaxTree.Initializer CheckInitializerSimple(CType type, SyntaxTree.Initializer.SimpleInitializer ast) {
            var expr = ast.AssignmentExpression;
            if (type.IsAggregateType() || type.IsUnionType()) {
                throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "集成体型又は共用体型をもつオブジェクトに対する初期化子は，要素又は名前付きメンバに対する初期化子並びを波括弧で囲んだものでなければならない。");
            }

            // 単純代入の規則を適用して検証
            var assign = SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression.ApplyAssignmentRule(type, expr);

            return new SyntaxTree.Initializer.SimpleAssignInitializer(type, assign);

        }

        private static SyntaxTree.Initializer CheckInitializerExpression(CType type, SyntaxTree.Initializer ast) {
            if (type.IsArrayType()) {
                // 配列型の場合
                var arrayType = type.Unwrap() as CType.ArrayType;
                return CheckInitializerArray(arrayType, ast);
            } else if (type.IsStructureType()) {
                // 構造体型の場合
                var arrayType = type.Unwrap() as CType.TaggedType.StructUnionType;
                return CheckInitializerStruct(arrayType, ast);
            } else if (type.IsUnionType()) {
                // 共用体型の場合
                var arrayType = type.Unwrap() as CType.TaggedType.StructUnionType;
                return CheckInitializerUnion(arrayType, ast);
            } else {
                return CheckInitializerSimple(type, ast as SyntaxTree.Initializer.SimpleInitializer);
            }
        }

        /// <summary>
        /// 配列型の初期化式
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ast"></param>
        private static SyntaxTree.Initializer CheckInitializerArray(CType.ArrayType type, SyntaxTree.Initializer ast) {
            if (ast is SyntaxTree.Initializer.SimpleInitializer) {
                return CheckInitializerSimple(type, ast as SyntaxTree.Initializer.SimpleInitializer);
            } else if (ast is SyntaxTree.Initializer.ComplexInitializer) {
                var inits = (ast as SyntaxTree.Initializer.ComplexInitializer).Ret;
                if (type.Length == -1) {
                    type.Length = inits.Count;
                } else if (type.Length < inits.Count) {
                    throw new Exception("要素数が領域サイズよりも大きい");
                }
                // 要素数分回す
                List<SyntaxTree.Initializer> assigns = new List<SyntaxTree.Initializer>();
                for (var i = 0; type.Length == -1 || i < inits.Count; i++) {
                        assigns.Add(CheckInitializerExpression(type.BaseType, inits[i]));
                }
                // ToDo:足りない分は0で埋める
                return new SyntaxTree.Initializer.ArrayAssignInitializer(type, assigns);
            }
                throw new Exception("");
        }

        /// <summary>
        /// 構造体型の初期化式
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ast"></param>
        private static SyntaxTree.Initializer CheckInitializerStruct(CType.TaggedType.StructUnionType type, SyntaxTree.Initializer ast) {
            if (ast is SyntaxTree.Initializer.SimpleInitializer) {
                return CheckInitializerSimple(type, ast as SyntaxTree.Initializer.SimpleInitializer);
            } else if (ast is SyntaxTree.Initializer.ComplexInitializer) {
                var inits = (ast as SyntaxTree.Initializer.ComplexInitializer).Ret;
                if (type.Members.Count < inits.Count) {
                    throw new Exception("要素数が違う");
                }
                // 要素数分回す
                // Todo: ビットフィールド
                List<SyntaxTree.Initializer> assigns = new List<SyntaxTree.Initializer>();
                for (var i = 0; i < inits.Count; i++) {
                        assigns.Add(CheckInitializerExpression(type.Members[i].Type, inits[i]));
                }
                return new SyntaxTree.Initializer.StructUnionAssignInitializer(type, assigns);
            }
                throw new Exception("");
        }

        /// <summary>
        /// 共用体型の初期化式
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ast"></param>
        private static SyntaxTree.Initializer CheckInitializerUnion(CType.TaggedType.StructUnionType type, SyntaxTree.Initializer ast) {
            if (ast is SyntaxTree.Initializer.SimpleInitializer) {
                return CheckInitializerSimple(type, ast as SyntaxTree.Initializer.SimpleInitializer);
            } else if (ast is SyntaxTree.Initializer.ComplexInitializer) {
                    // 最初の要素とのみチェック
                    return CheckInitializerExpression(type.Members[0].Type, ast);
            } else {
                throw new Exception("");
            }
        }

        /// <summary>
        /// 初期化式列
        /// </summary>
        /// <param name="type"></param>
        /// <param name="ast"></param>
        private static SyntaxTree.Initializer CheckInitializerList(CType type, SyntaxTree.Initializer ast) {
            if (type.IsArrayType()) {
                // 配列型の場合
                var arrayType = type.Unwrap() as CType.ArrayType;

                if (arrayType.BaseType.IsCharacterType()) {

                    if (ast is SyntaxTree.Initializer.SimpleInitializer && (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression is SyntaxTree.Expression.PrimaryExpression.StringExpression) {
                        // 初期化先の型が文字配列の場合は、文字列式を文字配列式と見なして初期化するので、配列型を書き換える
                        var strExpr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression as SyntaxTree.Expression.PrimaryExpression.StringExpression;
                        if (arrayType.Length == -1) {
                            arrayType.Length = strExpr.Value.Count;
                        }
                        return new SyntaxTree.Initializer.SimpleAssignInitializer(arrayType, strExpr);

                    } else if (ast is SyntaxTree.Initializer.ComplexInitializer) {
                        // 波括弧で括られた文字列で初期化も同様
                        if ((ast as SyntaxTree.Initializer.ComplexInitializer).Ret?.Count == 1
                            && ((ast as SyntaxTree.Initializer.ComplexInitializer).Ret[0] as SyntaxTree.Initializer.SimpleInitializer)?.AssignmentExpression is SyntaxTree.Expression.PrimaryExpression.StringExpression) {
                            var strExpr = ((ast as SyntaxTree.Initializer.ComplexInitializer).Ret[0] as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression as SyntaxTree.Expression.PrimaryExpression.StringExpression;
                            if (arrayType.Length == -1) {
                                arrayType.Length = strExpr.Value.Count + 1;
                            }
                                return new SyntaxTree.Initializer.SimpleAssignInitializer(arrayType, strExpr);
                            }
                        }
                }
                return CheckInitializerArray(arrayType, ast);
            } else if (type.IsStructureType()) {
                // 構造体型の場合
                var suType = type.Unwrap() as CType.TaggedType.StructUnionType;
                return CheckInitializerStruct(suType, ast);
            } else if (type.IsUnionType()) {
                // 共用体型の場合
                var suType = type.Unwrap() as CType.TaggedType.StructUnionType;
                return CheckInitializerUnion(suType, ast);
            } else {
                throw new Exception();
            }
        }

        public static SyntaxTree.Initializer CheckInitializer(CType type, SyntaxTree.Initializer ast) {
            if (type.IsArrayType() || type.IsStructureType() || type.IsUnionType()) {
                return CheckInitializerList(type, ast);
            } else {
                var expr = (ast as SyntaxTree.Initializer.SimpleInitializer).AssignmentExpression;
                if (type.IsAggregateType() || type.IsUnionType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "集成体型又は共用体型をもつオブジェクトに対する初期化子は，要素又は名前付きメンバに対する初期化子並びを波括弧で囲んだものでなければならない。");
                }
                // 単純代入の規則を適用して検証
                var assign = SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression.ApplyAssignmentRule(type, expr);
                    return new SyntaxTree.Initializer.SimpleAssignInitializer(type, assign);
                }
            }
        }

        #endregion

        /// <summary>
        /// 6.7.8 初期化
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Initializer Initializer(CType type) {
            var init = InitializerItem();
            return InitializerChecker.CheckInitializer(type, init);
        }

        /// <summary>
        /// 6.7.8 初期化(初期化子)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Initializer InitializerItem() {
            if (_lexer.ReadTokenIf('{')) {
                List<SyntaxTree.Initializer> ret = null;
                if (_lexer.PeekToken('}') == false) {
                    ret = InitializerList();
                }
                _lexer.ReadToken('}');
                return new SyntaxTree.Initializer.ComplexInitializer(ret);
            } else {
                return new SyntaxTree.Initializer.SimpleInitializer(AssignmentExpression());
            }
        }

        /// <summary>
        /// 6.7.8 初期化(初期化子並び)
        /// </summary>
        /// <returns></returns>
        private List<SyntaxTree.Initializer> InitializerList() {
            var ret = new List<SyntaxTree.Initializer>();
            ret.Add(InitializerItem());
            while (_lexer.ReadTokenIf(',')) {
                if (_lexer.PeekToken('}')) {
                    break;
                }
                ret.Add(InitializerItem());
            }
            return ret;
        }

        private bool IsIdentifier(bool includeTypeName) {
            if (includeTypeName) {
                return _lexer.CurrentToken().Kind == Token.TokenKind.IDENTIFIER;
            } else {
                return _lexer.CurrentToken().Kind == Token.TokenKind.IDENTIFIER && !IsTypedefedType(_lexer.CurrentToken().Raw);
            }
        }

        private string Identifier(bool includeTypeName) {
            if (IsIdentifier(includeTypeName) == false) {
                throw new Exception();
            }
            var ret = _lexer.CurrentToken().Raw;
            _lexer.NextToken();
            return ret;
        }

        /// <summary>
        /// 6.8 文及びブロック
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement Statement() {
            if ((IsIdentifier(true) && _lexer.PeekNextToken(':')) || _lexer.PeekToken(Token.TokenKind.CASE, Token.TokenKind.DEFAULT)) {
                return LabeledStatement();
            } else if (_lexer.PeekToken('{')) {
                return CompoundStatement();
            } else if (_lexer.PeekToken(Token.TokenKind.IF, Token.TokenKind.SWITCH)) {
                return SelectionStatement();
            } else if (_lexer.PeekToken(Token.TokenKind.WHILE, Token.TokenKind.DO, Token.TokenKind.FOR)) {
                return IterationStatement();
            } else if (_lexer.PeekToken(Token.TokenKind.GOTO, Token.TokenKind.CONTINUE, Token.TokenKind.BREAK, Token.TokenKind.RETURN)) {
                return JumpStatement();
            } else if (_lexer.PeekToken(Token.TokenKind.__ASM__)) {
                return GnuAsmStatement();
            } else {
                return ExpressionStatement();
            }
        }

        /// <summary>
        /// 6.8.1 ラベル付き文
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement LabeledStatement() {
            if (_lexer.ReadTokenIf(Token.TokenKind.CASE)) {
                if (_switchScope.Any() == false) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"caseラベルがswitch文外にあります。");
                }
                var expr = ConstantExpression();
                var value = Evaluator.ConstantEval(expr);
                if (value.Type.IsIntegerType() == false) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"caseラベルの値が整数定数値ではありません。");
                }
                long v = 0;
                if (value is SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant) {
                    v = (value as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant).Value;
                } else if (value is SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant) {
                    v = (value as SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant).Value;
                } else if (value is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant) {
                    v = (value as SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant).Info.Value;
                } else {
                    throw new Exception("");
                }

                _lexer.ReadToken(':');
                var stmt = Statement();
                var caseStatement =  new SyntaxTree.Statement.CaseStatement(expr, v, stmt);
                _switchScope.Peek().AddCaseStatement(caseStatement);
                return caseStatement;
            } else if (_lexer.ReadTokenIf(Token.TokenKind.DEFAULT)) {
                if (_switchScope.Any() == false) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"defaultラベルがswitch文外にあります。");
                }
                _lexer.ReadToken(':');
                var stmt = Statement();
                var defaultStatement = new SyntaxTree.Statement.DefaultStatement(stmt);
                _switchScope.Peek().SetDefaultLabel(defaultStatement);
                return defaultStatement;
            } else {
                var ident = Identifier(true);
                _lexer.ReadToken(':');
                var stmt = Statement();
                LabelScopeValue value;
                if (_labelScope.TryGetValue(ident, out value) == false) {
                    // ラベルの前方参照なので仮登録する。
                    value = new LabelScopeValue();
                    _labelScope.Add(ident, value);
                } else if (value.Declaration != null) {
                    // 既に宣言済みなのでエラー
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"ラベル{ident}はすでに宣言されています。");
                }
                var labelStmt = new SyntaxTree.Statement.GenericLabeledStatement(ident, stmt);
                value.SetDeclaration(labelStmt);
                return labelStmt;
            }
        }

        /// <summary>
        /// 6.8.2 複合文
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement.CompoundStatement CompoundStatement(bool skipCreateNewScope = false) {
            if (skipCreateNewScope == false) {
                _tagScope = _tagScope.Extend();
                _identScope = _identScope.Extend();
            }
            _lexer.ReadToken('{');
            var decls = new List<SyntaxTree.Declaration>();

            _insertImplictDeclarationOperatorStack.Push((decl) => {// ToDo: 共通化
                decls.Add(decl);
                return decl;
            });

            while (IsDeclaration()) {
                var d = Declaration();
                if (d != null) {
                    decls.AddRange(d);
                }
            }
            var stmts = new List<SyntaxTree.Statement>();
            while (_lexer.PeekToken('}') == false) {
                stmts.Add(Statement());
            }
            _lexer.ReadToken('}');

            _insertImplictDeclarationOperatorStack.Pop();

            var stmt = new SyntaxTree.Statement.CompoundStatement(decls, stmts, _tagScope, _identScope);
            if (skipCreateNewScope == false) {
                _identScope = _identScope.Parent;
                _tagScope = _tagScope.Parent;
            }
            return stmt;

        }

        /// <summary>
        /// 6.8.3 式文及び空文
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement ExpressionStatement() {
            SyntaxTree.Statement ret;
            if (!_lexer.PeekToken(';')) {
                var expr = Expression();
                ret = new SyntaxTree.Statement.ExpressionStatement(expr);
            } else {
                ret = new SyntaxTree.Statement.EmptyStatement();
            }
            _lexer.ReadToken(';');
            return ret;
        }

        /// <summary>
        /// 6.8.4 選択文
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement SelectionStatement() {
            if (_lexer.ReadTokenIf(Token.TokenKind.IF)) {
                _lexer.ReadToken('(');
                var cond = Expression();
                _lexer.ReadToken(')');
                var thenStmt = Statement();
                SyntaxTree.Statement elseStmt = null;
                if (_lexer.ReadTokenIf(Token.TokenKind.ELSE)) {
                    elseStmt = Statement();
                }
                return new SyntaxTree.Statement.IfStatement(cond, thenStmt, elseStmt);
            }
            if (_lexer.ReadTokenIf(Token.TokenKind.SWITCH)) {
                _lexer.ReadToken('(');
                var cond = Expression();
                _lexer.ReadToken(')');
                var ss = new SyntaxTree.Statement.SwitchStatement(cond);
                _breakScope.Push(ss);
                _switchScope.Push(ss);
                ss.Stmt = Statement();
                _switchScope.Pop();
                _breakScope.Pop();
                return ss;
            }
            throw new CompilerException.InternalErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"選択文は if, switch の何れかで始まりますが、 { _lexer.CurrentToken() } はそのいずれでもありません。（本処理系の実装に誤りがあると思います。）");
        }

        /// <summary>
        /// 6.8.5 繰返し文
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement IterationStatement() {
            if (_lexer.ReadTokenIf(Token.TokenKind.WHILE)) {
                _lexer.ReadToken('(');
                var cond = Expression();
                _lexer.ReadToken(')');
                var ss = new SyntaxTree.Statement.WhileStatement(cond);
                _breakScope.Push(ss);
                _continueScope.Push(ss);
                ss.Stmt = Statement();
                _breakScope.Pop();
                _continueScope.Pop();
                return ss;
            }
            if (_lexer.ReadTokenIf(Token.TokenKind.DO)) {
                var ss = new SyntaxTree.Statement.DoWhileStatement();
                _breakScope.Push(ss);
                _continueScope.Push(ss);
                ss.Stmt = Statement();
                _breakScope.Pop();
                _continueScope.Pop();
                _lexer.ReadToken(Token.TokenKind.WHILE);
                _lexer.ReadToken('(');
                ss.Cond = Expression();
                _lexer.ReadToken(')');
                _lexer.ReadToken(';');
                return ss;
            }
            if (_lexer.ReadTokenIf(Token.TokenKind.FOR)) {
                _lexer.ReadToken('(');

                var init = _lexer.PeekToken(';') ? (SyntaxTree.Expression)null : Expression();
                _lexer.ReadToken(';');
                var cond = _lexer.PeekToken(';') ? (SyntaxTree.Expression)null : Expression();
                _lexer.ReadToken(';');
                var update = _lexer.PeekToken(')') ? (SyntaxTree.Expression)null : Expression();
                _lexer.ReadToken(')');
                var ss = new SyntaxTree.Statement.ForStatement(init, cond, update);
                _breakScope.Push(ss);
                _continueScope.Push(ss);
                ss.Stmt = Statement();
                _breakScope.Pop();
                _continueScope.Pop();
                return ss;
            }
            throw new CompilerException.InternalErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"繰返し文は while, do, for の何れかで始まりますが、 { _lexer.CurrentToken() } はそのいずれでもありません。（本処理系の実装に誤りがあると思います。）");
        }

        /// <summary>
        ///  6.8.6 分岐文
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement JumpStatement() {
            if (_lexer.ReadTokenIf(Token.TokenKind.GOTO)) {
                var label = Identifier(true);
                _lexer.ReadToken(';');
                LabelScopeValue value;
                if (_labelScope.TryGetValue(label, out value) == false) {
                    // ラベルの前方参照なので仮登録する。
                    value = new LabelScopeValue();
                    _labelScope.Add(label, value);
                }
                var gotoStmt = new SyntaxTree.Statement.GotoStatement(label);
                value.AddReference(gotoStmt);
                return gotoStmt;
            }
            if (_lexer.ReadTokenIf(Token.TokenKind.CONTINUE)) {
                _lexer.ReadToken(';');
                if (_continueScope.Any() == false || _continueScope.Peek() == null) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "ループ文の外で continue 文が使われています。");
                }
                return new SyntaxTree.Statement.ContinueStatement(_continueScope.Peek());
            }
            if (_lexer.ReadTokenIf(Token.TokenKind.BREAK)) {
                _lexer.ReadToken(';');
                if (_breakScope.Any() == false || _breakScope.Peek() == null) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "ループ文/switch文の外で break 文が使われています。");
                }
                return new SyntaxTree.Statement.BreakStatement(_breakScope.Peek());
            }
            if (_lexer.ReadTokenIf(Token.TokenKind.RETURN)) {
                var expr = _lexer.PeekToken(';') ? null : Expression();
                //現在の関数の戻り値と型チェック
                _lexer.ReadToken(';');
                return new SyntaxTree.Statement.ReturnStatement(expr);
            }
            throw new CompilerException.InternalErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"分岐文は goto, continue, break, return の何れかで始まりますが、 { _lexer.CurrentToken() } はそのいずれでもありません。（本処理系の実装に誤りがあると思います。）");
        }

        /// <summary>
        /// X.X.X GCC拡張インラインアセンブラ
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Statement GnuAsmStatement() {
            Console.Error.WriteLine("GCC拡張インラインアセンブラ構文には対応していません。ざっくりと読み飛ばします。");

            _lexer.ReadToken(Token.TokenKind.__ASM__);
            _lexer.ReadTokenIf(Token.TokenKind.__VOLATILE__);
            _lexer.ReadToken('(');
            Stack<char> parens = new Stack<char>();
            parens.Push('(');
            while (parens.Any()) {
                if (_lexer.PeekToken('(', '[')) {
                    parens.Push((char)_lexer.CurrentToken().Kind);
                } else if (_lexer.PeekToken(')')) {
                    if (parens.Peek() == '(') {
                        parens.Pop();
                    } else {
                        throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "GCC拡張インラインアセンブラ構文中で 丸括弧閉じ ) が使用されていますが、対応する丸括弧開き ( がありません。");
                    }
                } else if (_lexer.PeekToken(']')) {
                    if (parens.Peek() == '[') {
                        parens.Pop();
                    } else {
                        throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "GCC拡張インラインアセンブラ構文中で 角括弧閉じ ] が使用されていますが、対応する角括弧開き [ がありません。");
                    }
                }
                _lexer.NextToken();
            }
            _lexer.ReadToken(';');
            return new SyntaxTree.Statement.EmptyStatement();
        }

        /// <summary>
        /// 6.5 式
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression Expression() {
            var e = AssignmentExpression();
            if (_lexer.PeekToken(',')) {
                var ce = new SyntaxTree.Expression.CommaExpression();
                ce.Expressions.Add(e);
                while (_lexer.ReadTokenIf(',')) {
                    e = AssignmentExpression();
                    ce.Expressions.Add(e);
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
        private SyntaxTree.Expression PrimaryExpression() {
            if (IsIdentifier(false)) {
                var ident = Identifier(false);
                SyntaxTree.Declaration value;
                if (_identScope.TryGetValue(ident, out value) == false) {
                    Console.Error.WriteLine($"未定義の識別子{ident}が一次式として利用されています。");
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression(ident);
                }
                if (value is SyntaxTree.Declaration.VariableDeclaration) {
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression(ident, value as SyntaxTree.Declaration.VariableDeclaration);
                }
                if (value is SyntaxTree.Declaration.ArgumentDeclaration) {
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression(ident, value as SyntaxTree.Declaration.ArgumentDeclaration);
                }
                if (value is SyntaxTree.Declaration.EnumMemberDeclaration) {
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant((value as SyntaxTree.Declaration.EnumMemberDeclaration).MemberInfo);
                }
                if (value is SyntaxTree.Declaration.FunctionDeclaration) {
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression(ident, value as SyntaxTree.Declaration.FunctionDeclaration);
                }
                throw new CompilerException.InternalErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"一次式として使える定義済み識別子は変数、列挙定数、関数の何れかですが、 { _lexer.CurrentToken() } はそのいずれでもありません。（本処理系の実装に誤りがあると思います。）");
            }
            if (IsConstant()) {
                return Constant();
            }
            if (IsStringLiteral()) {
                List<string> strings = new List<string>();
                while (IsStringLiteral()) {
                    strings.Add(StringLiteral());
                }
                return new SyntaxTree.Expression.PrimaryExpression.StringExpression(strings);
            }
            if (_lexer.ReadTokenIf('(')) {
                if (_lexer.PeekToken('{')) {
                    // gcc statement expression
                    var statements = CompoundStatement();
                    _lexer.ReadToken(')');
                    return new SyntaxTree.Expression.GccStatementExpression(statements, null);// todo: implement type
                } else {
                    var expr = new SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression(Expression());
                    _lexer.ReadToken(')');
                    return expr;
                }
            }
            throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"一次式となる要素があるべき場所に { _lexer.CurrentToken() } があります。");
        }

        /// <summary>
        /// 6.5.1 一次式(定数となりうるか)
        /// </summary>
        /// <returns></returns>
        private bool IsConstant() {
            return IsIntegerConstant() ||
                   IsCharacterConstant() ||
                   IsFloatingConstant() ||
                   IsEnumerationConstant();
        }

        /// <summary>
        /// 6.5.1 一次式(定数)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression Constant() {
            // 6.5.1 一次式
            // 定数は，一次式とする。その型は，その形式と値によって決まる（6.4.4 で規定する。）。

            // 整数定数
            if (IsIntegerConstant()) {
                return IntegerConstant();
            }

            // 文字定数
            if (IsCharacterConstant()) {
                var ret = CharacterConstant();
                return new SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant(ret);
            }

            // 浮動小数定数
            if (IsFloatingConstant()) {
                return FloatingConstant();
            }

            // 列挙定数
            if (IsEnumerationConstant()) {
                var ret = EnumerationConstant();
                return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant(ret);
            }

            throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"定数があるべき場所に { _lexer.CurrentToken() } があります。");
        }

        /// <summary>
        /// 6.5.2 後置演算子(後置式の前半)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression PostfixExpression() {
            var expr = PrimaryExpression();
            return MorePostfixExpression(expr);
        }

        /// <summary>
        /// 6.5.2 後置演算子(後置式の後半)
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        private SyntaxTree.Expression MorePostfixExpression(SyntaxTree.Expression expr) {
            if (_lexer.ReadTokenIf('[')) {
                // 6.5.2.1 配列の添字付け
                var index = Expression();
                _lexer.ReadToken(']');
                return MorePostfixExpression(new SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression(expr, index));
            }
            if (_lexer.ReadTokenIf('(')) {
                // 6.5.2.2 関数呼出し
                var args = _lexer.PeekToken(')') == false ? ArgumentExpressionList() : new List<SyntaxTree.Expression>();
                _lexer.ReadToken(')');
                // 未定義の識別子の直後に関数呼び出し用の後置演算子 '(' がある場合、
                if (expr is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression) {
                    // K&RおよびC89/90では暗黙的関数宣言 extern int 識別子(); が現在の宣言ブロックの先頭で定義されていると仮定して翻訳する
                    if (Mode == LanguageMode.C89) {
                        var identExpr = expr as SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression;
                        var decl = AddImplictFunctionDeclaration(identExpr.Ident, new CType.FunctionType(null, false, CType.CreateSignedInt()));
                        expr = new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression(identExpr.Ident, decl as SyntaxTree.Declaration.FunctionDeclaration);
                    } else {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "未定義の識別子を関数として用いています。");
                    }
                }
                return MorePostfixExpression(new SyntaxTree.Expression.PostfixExpression.FunctionCallExpression(expr, args));
            }
            if (_lexer.ReadTokenIf('.')) {
                // 6.5.2.3 構造体及び共用体のメンバ
                var ident = Identifier(false);
                return MorePostfixExpression(new SyntaxTree.Expression.PostfixExpression.MemberDirectAccess(expr, ident));
            }
            if (_lexer.ReadTokenIf(Token.TokenKind.PTR_OP)) {
                // 6.5.2.3 構造体及び共用体のメンバ
                var ident = Identifier(false);
                return MorePostfixExpression(new SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess(expr, ident));
            }
            if (_lexer.PeekToken(Token.TokenKind.INC_OP, Token.TokenKind.DEC_OP)) {
                // 6.5.2.4 後置増分及び後置減分演算子
                var op = SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.None;
                switch (_lexer.CurrentToken().Kind) {
                    case Token.TokenKind.INC_OP:
                        op = SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Inc;
                        break;
                    case Token.TokenKind.DEC_OP:
                        op = SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Dec;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "たぶん実装ミスです。");
                }
                _lexer.NextToken();
                return MorePostfixExpression(new SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression(op, expr));
            }
            // 6.5.2.5 複合リテラル
            {
                // Todo: 実装
            }
            return expr;
        }

        /// <summary>
        /// 6.5.2 後置演算子(実引数並び)
        /// </summary>
        /// <returns></returns>
        private List<SyntaxTree.Expression> ArgumentExpressionList() {
            var ret = new List<SyntaxTree.Expression>();
            ret.Add(ArgumentExpression());
            while (_lexer.ReadTokenIf(',')) {
                ret.Add(ArgumentExpression());
            }
            return ret;
        }

        /// <summary>
        /// 6.5.2 後置演算子(実引数)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression ArgumentExpression() {
            var ret = AssignmentExpression();

            // 6.3.2.1: 配列型の場合はポインタ型に変換する
            CType baseType;
            if (ret.Type.IsArrayType(out baseType)) {
                ret = new SyntaxTree.Expression.TypeConversionExpression(CType.CreatePointer(baseType), ret);
            }

            return ret;
        }

        /// <summary>
        /// 6.5.3 単項演算子(単項式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression UnaryExpression() {
            if (_lexer.PeekToken(Token.TokenKind.INC_OP, Token.TokenKind.DEC_OP)) {
                SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind op;
                switch (_lexer.CurrentToken().Kind) {
                    case Token.TokenKind.INC_OP:
                        op = SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Inc;
                        break;
                    case Token.TokenKind.DEC_OP:
                        op = SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Dec;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "たぶん実装ミスです。");
                }
                _lexer.NextToken();
                var expr = UnaryExpression();
                return new SyntaxTree.Expression.UnaryPrefixExpression(op, expr);
            }
            if (_lexer.ReadTokenIf('&')) {
                var expr = CastExpression();
                return new SyntaxTree.Expression.UnaryAddressExpression(expr);
            }
            if (_lexer.ReadTokenIf('*')) {
                var expr = CastExpression();
                return new SyntaxTree.Expression.UnaryReferenceExpression(expr);
            }
            if (_lexer.ReadTokenIf('+')) {
                var expr = CastExpression();
                return new SyntaxTree.Expression.UnaryPlusExpression(expr);
            }
            if (_lexer.ReadTokenIf('-')) {
                var expr = CastExpression();
                return new SyntaxTree.Expression.UnaryMinusExpression(expr);
            }
            if (_lexer.ReadTokenIf('~')) {
                var expr = CastExpression();
                return new SyntaxTree.Expression.UnaryNegateExpression(expr);
            }
            if (_lexer.ReadTokenIf('!')) {
                var expr = CastExpression();
                return new SyntaxTree.Expression.UnaryNotExpression(expr);
            }
            if (_lexer.ReadTokenIf(Token.TokenKind.SIZEOF)) {
                if (_lexer.PeekToken('(')) {
                    // どっちにも'('が出ることが出来るのでさらに先読みする（LL(2))
                    var saveCurrent = _lexer.Save();
                    _lexer.ReadToken('(');
                    if (IsTypeName()) {
                        var type = TypeName();
                        _lexer.ReadToken(')');
                        return new SyntaxTree.Expression.SizeofTypeExpression(type);
                    } else {
                        _lexer.Restore(saveCurrent);
                        var expr = UnaryExpression();
                        return new SyntaxTree.Expression.SizeofExpression(expr);
                    }
                } else {
                    // 括弧がないので式
                    var expr = UnaryExpression();
                    return new SyntaxTree.Expression.SizeofExpression(expr);
                }
            }
            return PostfixExpression();
        }

        /// <summary>
        /// 6.5.4 キャスト演算子(キャスト式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression CastExpression() {
            if (_lexer.PeekToken('(')) {
                // どちらにも'('の出現が許されるためさらに先読みを行う。
                var saveCurrent = _lexer.Save();
                _lexer.ReadToken('(');
                if (IsTypeName()) {
                    var type = TypeName();
                    _lexer.ReadToken(')');
                    var expr = CastExpression();
                    return new SyntaxTree.Expression.CastExpression(type, expr);
                } else {
                    _lexer.Restore(saveCurrent);
                    return UnaryExpression();
                }
            } else {
                return UnaryExpression();
            }
        }

        /// <summary>
        /// 6.5.5 乗除演算子(乗除式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression MultiplicitiveExpression() {
            var lhs = CastExpression();
            while (_lexer.PeekToken('*', '/', '%')) {
                SyntaxTree.Expression.MultiplicitiveExpression.OperatorKind op;
                switch (_lexer.CurrentToken().Kind) {
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
                _lexer.NextToken();
                var rhs = CastExpression();
                lhs = new SyntaxTree.Expression.MultiplicitiveExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.6 加減演算子(加減式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression AdditiveExpression() {
            var lhs = MultiplicitiveExpression();
            while (_lexer.PeekToken('+', '-')) {
                SyntaxTree.Expression.AdditiveExpression.OperatorKind op;
                switch (_lexer.CurrentToken().Kind) {
                    case (Token.TokenKind)'+':
                        op = SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add;
                        break;
                    case (Token.TokenKind)'-':
                        op = SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "");
                }
                _lexer.NextToken();
                var rhs = MultiplicitiveExpression();
                lhs = new SyntaxTree.Expression.AdditiveExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.7 ビット単位のシフト演算子(シフト式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression ShiftExpression() {
            var lhs = AdditiveExpression();
            while (_lexer.PeekToken(Token.TokenKind.LEFT_OP, Token.TokenKind.RIGHT_OP)) {
                SyntaxTree.Expression.ShiftExpression.OperatorKind op;
                switch (_lexer.CurrentToken().Kind) {
                    case Token.TokenKind.LEFT_OP:
                        op = SyntaxTree.Expression.ShiftExpression.OperatorKind.Left;
                        break;
                    case Token.TokenKind.RIGHT_OP:
                        op = SyntaxTree.Expression.ShiftExpression.OperatorKind.Right;
                        break;
                    default:
                        throw new Exception();
                }

                _lexer.NextToken();
                var rhs = AdditiveExpression();
                lhs = new SyntaxTree.Expression.ShiftExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.8 関係演算子(関係式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression RelationalExpression() {
            var lhs = ShiftExpression();
            while (_lexer.PeekToken((Token.TokenKind)'<', (Token.TokenKind)'>', Token.TokenKind.LE_OP, Token.TokenKind.GE_OP)) {
                SyntaxTree.Expression.RelationalExpression.OperatorKind op;
                switch (_lexer.CurrentToken().Kind) {
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
                _lexer.NextToken();
                var rhs = ShiftExpression();
                lhs = new SyntaxTree.Expression.RelationalExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.9 等価演算子(等価式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression EqualityExpression() {
            var lhs = RelationalExpression();
            while (_lexer.PeekToken(Token.TokenKind.EQ_OP, Token.TokenKind.NE_OP)) {
                SyntaxTree.Expression.EqualityExpression.OperatorKind op;
                switch (_lexer.CurrentToken().Kind) {
                    case Token.TokenKind.EQ_OP:
                        op = SyntaxTree.Expression.EqualityExpression.OperatorKind.Equal;
                        break;
                    case Token.TokenKind.NE_OP:
                        op = SyntaxTree.Expression.EqualityExpression.OperatorKind.NotEqual;
                        break;
                    default:
                        throw new Exception();
                }
                _lexer.NextToken();
                var rhs = RelationalExpression();
                lhs = new SyntaxTree.Expression.EqualityExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.10 ビット単位の AND 演算子(AND式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression AndExpression() {
            var lhs = EqualityExpression();
            while (_lexer.ReadTokenIf('&')) {
                var rhs = EqualityExpression();
                lhs = new SyntaxTree.Expression.AndExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.11 ビット単位の排他 OR 演算子(排他OR式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression ExclusiveOrExpression() {
            var lhs = AndExpression();
            while (_lexer.ReadTokenIf('^')) {
                var rhs = AndExpression();
                lhs = new SyntaxTree.Expression.ExclusiveOrExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.12 ビット単位の OR 演算子(OR式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression InclusiveOrExpression() {
            var lhs = ExclusiveOrExpression();
            while (_lexer.ReadTokenIf('|')) {
                var rhs = ExclusiveOrExpression();
                lhs = new SyntaxTree.Expression.InclusiveOrExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.13 論理 AND 演算子(論理AND式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression LogicalAndExpression() {
            var lhs = InclusiveOrExpression();
            while (_lexer.ReadTokenIf(Token.TokenKind.AND_OP)) {
                var rhs = InclusiveOrExpression();
                lhs = new SyntaxTree.Expression.LogicalAndExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.14 論理 OR 演算子(論理OR式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression LogicalOrExpression() {
            var lhs = LogicalAndExpression();
            while (_lexer.ReadTokenIf(Token.TokenKind.OR_OP)) {
                var rhs = LogicalAndExpression();
                lhs = new SyntaxTree.Expression.LogicalOrExpression(lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.15 条件演算子(条件式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression ConditionalExpression() {
            var condExpr = LogicalOrExpression();
            if (_lexer.ReadTokenIf('?')) {
                var thenExpr = Expression();
                _lexer.ReadToken(':');
                var elseExpr = ConditionalExpression();
                return new SyntaxTree.Expression.ConditionalExpression(condExpr, thenExpr, elseExpr);
            } else {
                return condExpr;
            }
        }

        /// <summary>
        /// 6.5.16 代入演算子(代入式)
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression AssignmentExpression() {
            var lhs = ConditionalExpression();
            if (IsAssignmentOperator()) {
                var op = AssignmentOperator();
                var rhs = AssignmentExpression();

                
                switch (op) {
                    case "=":
                        lhs = new SyntaxTree.Expression.AssignmentExpression.SimpleAssignmentExpression(lhs, rhs);
                        break;
                    case "+=":
                        lhs = new SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.ADD_ASSIGN, lhs, rhs);
                        break;
                    case "-=":
                        lhs = new SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.SUB_ASSIGN, lhs, rhs);
                        break;
                    case "*=":
                        lhs = new SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MUL_ASSIGN, lhs, rhs);
                        break;
                    case "/=":
                        lhs = new SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.DIV_ASSIGN, lhs, rhs);
                        break;
                    case "%=":
                        lhs = new SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.MOD_ASSIGN, lhs, rhs);
                        break;
                    case "&=":
                        lhs = new SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.AND_ASSIGN, lhs, rhs);
                        break;
                    case "^=":
                        lhs = new SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.XOR_ASSIGN, lhs, rhs);
                        break;
                    case "|=":
                        lhs = new SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.OR_ASSIGN, lhs, rhs);
                        break;
                    case "<<=":
                        lhs = new SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.LEFT_ASSIGN, lhs, rhs);
                        break;
                    case ">>=":
                        lhs = new SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression(SyntaxTree.Expression.AssignmentExpression.CompoundAssignmentExpression.OperatorKind.RIGHT_ASSIGN, lhs, rhs);
                        break;
                    default:
                        throw new Exception("来ないはず");
                }
            }
            return lhs;
        }

        /// <summary>
        ///6.5.16 代入演算子（代入演算子トークンとなりうるか？）
        /// </summary>
        /// <returns></returns>
        private bool IsAssignmentOperator() {
            return _lexer.PeekToken((Token.TokenKind)'=', Token.TokenKind.MUL_ASSIGN, Token.TokenKind.DIV_ASSIGN, Token.TokenKind.MOD_ASSIGN, Token.TokenKind.ADD_ASSIGN, Token.TokenKind.SUB_ASSIGN, Token.TokenKind.LEFT_ASSIGN, Token.TokenKind.RIGHT_ASSIGN, Token.TokenKind.AND_ASSIGN, Token.TokenKind.XOR_ASSIGN, Token.TokenKind.OR_ASSIGN);
        }

        /// <summary>
        /// 6.5.16 代入演算子（代入演算子トークン）
        /// </summary>
        /// <returns></returns>
        private string AssignmentOperator() {
            if (IsAssignmentOperator() == false) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"代入演算子があるべき場所に { _lexer.CurrentToken() } があります。");
            }
            var ret = _lexer.CurrentToken().Raw;
            _lexer.NextToken();
            return ret;
        }

        /// <summary>
        /// 6.6 定数式
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression ConstantExpression() {
            // 補足説明  
            // 定数式は，実行時ではなく翻訳時に評価することができる。したがって，定数を使用してよいところならばどこでも使用してよい。
            //
            // 制約
            // - 定数式は，代入，増分，減分，関数呼出し又はコンマ演算子を含んではならない。
            //   ただし，定数式が評価されない部分式(sizeof演算子のオペランド等)に含まれている場合を除く。
            // - 定数式を評価した結果は，その型で表現可能な値の範囲内にある定数でなければならない。
            // 

            // ToDo: 初期化子中の定数式の扱いを実装
            return ConditionalExpression();

        }

        //
        // 以降は実装上の利便性のために定義
        //


#region 各宣言で登場する記憶クラス指定子/型指定子/型修飾子/関数修飾子の読み取り処理を共通化

        [Flags]
        private enum ReadDeclarationSpecifierPartFlag {
            None = 0x00,
            /// <summary>
            /// 記憶クラス指定子の出現を認める
            /// </summary>
            StorageClassSpecifier = 0x01,

            /// <summary>
            /// 型指定子の出現を認める
            /// </summary>
            TypeSpecifier = 0x02,

            /// <summary>
            /// 型修飾子の出現を認める
            /// </summary>
            TypeQualifier = 0x04,

            /// <summary>
            /// 関数修飾子の出現を認める
            /// </summary>
            FunctionSpecifier = 0x08,

            /// <summary>
            /// DeclarationSpecifiers の文法に従った動作を行うフラグ
            /// </summary>
            DeclarationSpecifiers = StorageClassSpecifier | TypeSpecifier | TypeQualifier,

            /// <summary>
            /// SpecifierQualifiers の文法に従った動作を行うフラグ
            /// </summary>
            SpecifierQualifiers = TypeSpecifier | TypeQualifier,

            /// <summary>
            /// ExternalDeclaration の文法に従った動作を行うフラグ
            /// </summary>
            ExternalDeclaration = StorageClassSpecifier | TypeSpecifier | TypeQualifier | FunctionSpecifier,
        }

        /// <summary>
        /// 各宣言で登場する記憶クラス指定子/型指定子/型修飾子/関数修飾子などの共通読み取り処理。flagsで動作を指定できる。
        /// </summary>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <param name="functionSpecifier"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        private int ReadDeclarationSpecifiers(
            ref CType type,
            ref StorageClassSpecifier storageClass,
            ref FunctionSpecifier functionSpecifier,
            ReadDeclarationSpecifierPartFlag flags
        ) {

            // 読み取り
            int n = 0;
            TypeSpecifier typeSpecifier = AnsiCParser.TypeSpecifier.None;
            TypeQualifier typeQualifier = AnsiCParser.TypeQualifier.None;
            while (IsDeclarationSpecifierPart(type, typeSpecifier, flags)) {
                ReadDeclarationSpecifierPart(ref type, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier, flags);
                n++;
            }

            // 構築

            if (type != null) {
                // 型指定子部に構造体共用体指定子、列挙型指定子、型定義名が出現する場合
                if (typeSpecifier != AnsiCParser.TypeSpecifier.None) {
                    // 6.7.2 型指定子「型指定子の並びは，次に示すもののいずれか一つでなければならない。」中で構造体共用体指定子、列挙型指定子、型定義名はそれ単体のみで使うことが規定されているため、
                    // 構造体共用体指定子、列挙型指定子、型定義名のいずれかとそれら以外の型指定子が組み合わせられている場合はエラーとする。
                    // なお、構造体共用体指定子、列挙型指定子、型定義名が複数回利用されている場合は SpecifierQualifier 内でエラーとなる。
                    // （歴史的な話：K&R では typedef は 別名(alias)扱いだったため、typedef int INT; unsigned INT x; は妥当だった）
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "型指定子・型修飾子並び中で構造体共用体指定子、列挙型指定子、型定義名のいずれかと、それら以外の型指定子が組み合わせられている。");
                }
            } else {
                // 型指定子部に構造体共用体指定子、列挙型指定子、型定義名が出現しない場合
                if (typeSpecifier == AnsiCParser.TypeSpecifier.None) {
                    // 歴史的な話：K&R では 宣言指定子を省略すると int 扱い
                    if (Mode == LanguageMode.C89) {
                        // C90では互換性の観点からK&R動作が使える。
                        Console.Error.WriteLine("型が省略された宣言は、暗黙的に signed int 型と見なします。");
                        type = new CType.BasicType(CType.BasicType.TypeKind.SignedInt);
                    } else {
                        // C99以降では
                        // 6.7.2 それぞれの宣言の宣言指定子列の中で，又はそれぞれの構造体宣言及び型名の型指定子型修飾子並びの中で，少なくとも一つの型指定子を指定しなければならない。
                        // とあるため、宣言指定子を一つも指定しないことは許されない。
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "C99以降ではそれぞれの宣言の宣言指定子列の中で，又はそれぞれの構造体宣言及び型名の型指定子型修飾子並びの中で，少なくとも一つの型指定子を指定しなければならない。");
                    }
                } else {
                    type = new CType.BasicType(typeSpecifier);
                }
            }

            // 型修飾子を適用
            type = type.WrapTypeQualifier(typeQualifier);

            return n;
        }

        private bool IsDeclarationSpecifierPart(CType type, TypeSpecifier typeSpecifier, ReadDeclarationSpecifierPartFlag flags) {
            if (IsStorageClassSpecifier()) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.StorageClassSpecifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"ここでは記憶クラス指定子 { _lexer.CurrentToken() } は使えません。");
                }
                return true;
            }
            if (IsTypeSpecifier() && type == null) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeSpecifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"ここでは型指定子 { _lexer.CurrentToken() } は使えません。");
                }
                return true;
            }
            if (IsStructOrUnionSpecifier() && type == null) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeSpecifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"ここでは構造体共用体指定子 { _lexer.CurrentToken() } は使えません。");
                }
                return true;
            }
            if (IsEnumSpecifier() && type == null) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeSpecifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"ここでは列挙型指定子 { _lexer.CurrentToken() } は使えません。");
                }
                return true;
            }
            if (IsTypedefName() && type == null && typeSpecifier == AnsiCParser.TypeSpecifier.None) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeSpecifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"ここでは型定義名 { _lexer.CurrentToken() } は使えません。");
                }
                return true;
            }
            if (IsTypeQualifier()) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeQualifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"ここでは型修飾子 { _lexer.CurrentToken() } は使えません。");
                }
                return true;
            }
            if (IsFunctionSpecifier()) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.FunctionSpecifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"ここでは関数修飾子 { _lexer.CurrentToken() } は使えません。");
                }
                return true;
            }
            return false;
        }

        private void ReadDeclarationSpecifierPart(
            ref CType type,
            ref StorageClassSpecifier storageClass,
            ref TypeSpecifier typeSpecifier,
            ref TypeQualifier typeQualifier,
            ref FunctionSpecifier functionSpecifier,
            ReadDeclarationSpecifierPartFlag flags
        ) {
            if (flags.HasFlag(ReadDeclarationSpecifierPartFlag.StorageClassSpecifier) && IsStorageClassSpecifier()) {
                // 記憶クラス指定子
                storageClass = storageClass.Marge(StorageClassSpecifier());
            } else if (flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeSpecifier) && IsTypeSpecifier()) {
                // 型指定子（予約語）
                typeSpecifier = typeSpecifier.Marge(TypeSpecifier());
            } else if (flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeSpecifier) && IsStructOrUnionSpecifier()) {
                // 型指定子（構造体指定子もしくは共用体指定子）
                if (type != null) {
                    throw new Exception("");
                }
                type = StructOrUnionSpecifier();
            } else if (flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeSpecifier) && IsEnumSpecifier()) {
                // 型指定子（列挙型指定子）
                if (type != null) {
                    throw new Exception("");
                }
                type = EnumSpecifier();
            } else if (flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeSpecifier) && IsTypedefName()) {
                // 型指定子（型定義名）
                SyntaxTree.Declaration.TypeDeclaration value;
                if (_identScope.TryGetValue(_lexer.CurrentToken().Raw, out value) == false) {
                    throw new Exception();
                }
                if (type != null) {
                    throw new Exception("");
                }
                type = new CType.TypedefedType(_lexer.CurrentToken().Raw, value.Type);
                _lexer.NextToken();
            } else if (flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeQualifier) && IsTypeQualifier()) {
                // 型修飾子
                typeQualifier.Marge(TypeQualifier());
            } else if (flags.HasFlag(ReadDeclarationSpecifierPartFlag.FunctionSpecifier) && IsFunctionSpecifier()) {
                // 関数修飾子
                functionSpecifier.Marge(FunctionSpecifier());
            } else {
                throw new Exception("");
            }
        }

#endregion


#region 関数宣言部（関数定義時も含む）の解析と名前表への登録を共通化

        private SyntaxTree.Declaration.FunctionDeclaration FunctionDeclaration(string ident, CType type, StorageClassSpecifier storageClass, FunctionSpecifier functionSpecifier, ScopeKind scope, bool isDefine) {
            if (scope == ScopeKind.BlockScope && isDefine) {
                throw new CompilerException.InternalErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "ブロックスコープ内で関数定義をしようとしている。（おそらく本処理系の実装ミス）");
            }

            if (scope == ScopeKind.BlockScope && !isDefine) {
                // ブロックスコープ中での関数宣言の場合

                // 6.7.1 記憶域クラス指定子
                // 関数の識別子がブロック有効範囲で宣言される場合，extern 以外の明示的な記憶域クラス指定子をもってはならない。

                if (storageClass != AnsiCParser.StorageClassSpecifier.None
                    && storageClass != AnsiCParser.StorageClassSpecifier.Extern) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "ローカルスコープ中での関数宣言に指定することができない記憶クラス指定子 extern が指定されている。");
                }
            }

            // 記憶域クラス指定からリンケージを求める(関数の場合は、外部結合もしくは内部結合のどれかとなり、無結合はない)
            LinkageKind linkage = ResolveLinkage(ident, type, storageClass, scope);
            System.Diagnostics.Debug.Assert(linkage == LinkageKind.ExternalLinkage || linkage == LinkageKind.InternalLinkage);

            // その識別子の以前の宣言が可視であるか？
            SyntaxTree.Declaration iv;
            bool isCurrent;
            if (_identScope.TryGetValue(ident, out iv, out isCurrent)) {
                // 以前の宣言が可視である

                // 6.7 宣言
                // 識別子が無結合である場合，その識別子の宣言（宣言子又は型指定子の中の）が同じ有効範囲及び同じ名前空間の中で，二つ以上あってはならない。
                // （捕捉：「識別子が無結合である場合」は以前の宣言の識別子にも適用される。つまり、一度でも無結合であると宣言された識別子については再宣言できない。）
                // 参考文献: https://stackoverflow.com/questions/7239911/block-scope-linkage-c-standard
                if ((linkage == LinkageKind.NoLinkage || iv.LinkageObject.Linkage == LinkageKind.NoLinkage) && isCurrent == true) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "識別子が無結合である場合，その識別子の宣言（宣言子又は型指定子の中の）が同じ有効範囲及び同じ名前空間の中で，二つ以上あってはならない。");
                }

                // 翻訳単位の中で同じ識別子が内部結合と外部結合の両方で現れた場合，その動作は未定義とする。
                if ((iv.LinkageObject.Linkage == LinkageKind.InternalLinkage && linkage == LinkageKind.ExternalLinkage)
                    || (iv.LinkageObject.Linkage == LinkageKind.ExternalLinkage && linkage == LinkageKind.InternalLinkage)) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"翻訳単位の中で同じ識別子{ident}が内部結合と外部結合の両方で現れました。この場合の動作は未定義です。");
                }

                var prevType = type;
                // 型適合のチェック
                if (Specification.IsCompatible(iv.Type.Unwrap(), type.Unwrap()) == false) {
                    throw new CompilerException.TypeMissmatchError(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"既に宣言されている{ident}と型が適合しないため再宣言できません。");
                }

                // 合成型を生成
                type = CType.CompositeType(iv.Type.Unwrap(), type.Unwrap());
                System.Diagnostics.Debug.Assert(type != null);

                if (scope == ScopeKind.FileScope && isDefine) {
                    var prevFd = (iv as SyntaxTree.Declaration.FunctionDeclaration);
                    if (prevFd.Body != null) {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "すでに本体を持っている関数を再定義しています。");
                    }
                    var compoundFt = type.Unwrap() as CType.FunctionType;
                    var ft = (prevType.Unwrap() as CType.FunctionType);
                    for (var i=0; i < ft.Arguments.Length; i++) {
                        compoundFt.Arguments[i].Ident = ft.Arguments[i].Ident;
                    }
                }
            } else {
                // 以前の宣言は可視ではない
            }

            var funcDelc = new SyntaxTree.Declaration.FunctionDeclaration(ident, type, storageClass, functionSpecifier);

            // 結合スコープにオブジェクトを追加
            funcDelc.LinkageObject = AddLinkageObject(linkage, funcDelc, (scope == ScopeKind.FileScope && isDefine));
            _identScope.Add(ident, funcDelc);

            return funcDelc;

        }
#endregion

        private SyntaxTree.Declaration.TypeDeclaration TypedefDeclaration(string ident, CType type) {
            // 型宣言名

            // 6.7 宣言
            // 識別子が無結合である場合，その識別子の宣言（宣言子又は型指定子の中の）が同じ有効範囲及び同じ名前空間の中で，二つ以上あってはならない。

            // 型名は無結合なので同じ有効範囲及び同じ名前空間の中で，二つ以上あってはならない。

            SyntaxTree.Declaration iv;
            bool isCurrent;
            if (_identScope.TryGetValue(ident, out iv, out isCurrent)) {

                if (isCurrent) {
                    // 型名は無結合であるため、再宣言できない
                    if (iv is SyntaxTree.Declaration.TypeDeclaration) {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"{ident} は既に型宣言名として宣言されています。（型の再定義はC11以降の機能。）");
                    } else {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"{ident} は宣言済みです。");
                    }
                }
            }
            var typeDecl = new SyntaxTree.Declaration.TypeDeclaration(ident, type);
            _identScope.Add(ident,typeDecl);
            return typeDecl;
        }


        private enum ScopeKind {
            BlockScope,
            FileScope
        }

        private SyntaxTree.Declaration.VariableDeclaration VariableDeclaration(string ident, CType type, StorageClassSpecifier storageClass, ScopeKind scope, bool hasInitializer) {

            // 記憶域クラス指定からリンケージを求める
            LinkageKind linkage = ResolveLinkage(ident, type, storageClass, scope);

            // 初期化子を持つか？
            SyntaxTree.Initializer initializer = null;
            if (hasInitializer) {

                if (type.IsFunctionType()) {
                    // 関数型は変数のように初期化できない。なぜなら、それは関数宣言だから。
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "関数型を持つ宣言子に対して初期化子を設定しています。");
                }

                // 6.7.8 初期化
                // 識別子の宣言がブロック有効範囲をもち，かつ識別子が外部結合又は内部結合をもつ場合，その宣言にその識別子に対する初期化子があってはならない。
                if ((scope == ScopeKind.BlockScope) && (linkage == LinkageKind.InternalLinkage || linkage == LinkageKind.ExternalLinkage)) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "識別子の宣言がブロック有効範囲をもち，かつ識別子が外部結合又は内部結合をもつ場合，その宣言にその識別子に対する初期化子があってはならない。");
                }

                // 初期化子を読み取る
                initializer = Initializer(type);

            }


            // その識別子の以前の宣言が可視であるか？
            SyntaxTree.Declaration iv;
            bool isCurrent;
            if (_identScope.TryGetValue(ident, out iv, out isCurrent)) {
                // 以前の宣言が可視である

                // 6.7 宣言
                // 識別子が無結合である場合，その識別子の宣言（宣言子又は型指定子の中の）が同じ有効範囲及び同じ名前空間の中で，二つ以上あってはならない。
                // （捕捉：「識別子が無結合である場合」は以前の宣言の識別子にも適用される。つまり、一度でも無結合であると宣言された識別子については再宣言できない。）
                // 参考文献: https://stackoverflow.com/questions/7239911/block-scope-linkage-c-standard
                if ((linkage == LinkageKind.NoLinkage || iv.LinkageObject.Linkage == LinkageKind.NoLinkage) && isCurrent == true) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "識別子が無結合である場合，その識別子の宣言（宣言子又は型指定子の中の）が同じ有効範囲及び同じ名前空間の中で，二つ以上あってはならない。");
                }

                // 翻訳単位の中で同じ識別子が内部結合と外部結合の両方で現れた場合，その動作は未定義とする。
                if ((iv.LinkageObject.Linkage == LinkageKind.InternalLinkage && linkage == LinkageKind.ExternalLinkage)
                    || (iv.LinkageObject.Linkage == LinkageKind.ExternalLinkage && linkage == LinkageKind.InternalLinkage)) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"翻訳単位の中で同じ識別子{ident}が内部結合と外部結合の両方で現れました。この場合の動作は未定義です。");
                }

                if (linkage != LinkageKind.NoLinkage) {
                    // 以前の宣言が変数定義でないならばエラー
                    if (iv is SyntaxTree.Declaration.VariableDeclaration == false) {
                        throw new CompilerException.TypeMissmatchError(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"{ident}は既に変数以外として宣言されています。");
                    }

                    // 型適合のチェック
                    if (Specification.IsCompatible(iv.Type.Unwrap(), type.Unwrap()) == false) {
                        throw new CompilerException.TypeMissmatchError(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, $"既に宣言されている変数{ident}と型が適合しないため再宣言できません。");
                    }

                    // 合成型を生成
                    type = CType.CompositeType(iv.Type.Unwrap(), type.Unwrap());
                    System.Diagnostics.Debug.Assert(type != null);

                }
                // 前の変数を隠すことができるので、新しい宣言を作成

            } else {
                // 以前の宣言は可視ではない、つまり、未定義なので新たに変数宣言を作成
            }

            // 新たに変数宣言を作成
            var varDecl = new SyntaxTree.Declaration.VariableDeclaration(ident, type, storageClass, initializer);
            varDecl.LinkageObject = AddLinkageObject(linkage, varDecl, varDecl.Init != null);
            _identScope.Add(ident, varDecl);
            return varDecl;
        }


        /// <summary>
        /// 宣言の読み取りの共通処理
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        private List<SyntaxTree.Declaration> ReadDeclaration(ScopeKind scope) {

            // 宣言指定子列 
        	CType baseType = null;
            StorageClassSpecifier storageClass = AnsiCParser.StorageClassSpecifier.None;
            FunctionSpecifier functionSpecifier = AnsiCParser.FunctionSpecifier.None;
            if (scope == ScopeKind.FileScope) {
                // ファイルスコープでの宣言
                if (ReadDeclarationSpecifiers(ref baseType, ref storageClass, ref functionSpecifier, ReadDeclarationSpecifierPartFlag.ExternalDeclaration) < 1) {
                    Console.Error.WriteLine("記憶クラス指定子 /型指定子/型修飾子が省略されています。");
                }

                // 記憶域クラス指定子 auto 及び register が，外部宣言の宣言指定子列の中に現れてはならない。
                if (storageClass == AnsiCParser.StorageClassSpecifier.Auto || storageClass == AnsiCParser.StorageClassSpecifier.Register) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "記憶域クラス指定子 auto 及び register が，外部宣言の宣言指定子列の中に現れてはならない。");
                }

            } else if (scope == ScopeKind.BlockScope) {
                // ブロックスコープでの宣言
                if (ReadDeclarationSpecifiers(ref baseType, ref storageClass, ref functionSpecifier, ReadDeclarationSpecifierPartFlag.DeclarationSpecifiers) < 1) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "記憶クラス指定子 /型指定子/型修飾子が一つ以上指定されている必要がある。");
                }
            } else {
                throw new Exception();
            }


        	// 宣言子並び
            var decls = new List<SyntaxTree.Declaration>();
            if (!IsDeclarator()) {
                // 宣言子が続いていない場合
                // 例: int; 

            	if (functionSpecifier != AnsiCParser.FunctionSpecifier.None) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "inlineは関数定義に対してのみ使える。");
                }


                // 匿名ではないタグ付き型（構造体/共用体/列挙型）の宣言は許可するが、
                // それ以外の宣言についてはエラーを出力する
                if (!baseType.IsStructureType() && !baseType.IsUnionType() && !baseType.IsEnumeratedType()) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "空の宣言は使用できません。");
                }
                CType.TaggedType.StructUnionType suType;
                if ((baseType.IsStructureType(out suType) || baseType.IsUnionType(out suType)) && suType.IsAnonymous) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "無名構造体/共用体が宣言されていますが、そのインスタンスを定義していません。");
                }
                if (CType.CheckContainOldStyleArgument(baseType)) {
                    throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "関数型中に型の無い仮引数名があります");
                }
                _lexer.ReadToken(';');
                return decls;
            } else {
            	string ident = "";
                var stack = new List<CType>() { new CType.StubType() };
                Declarator(ref ident, stack, 0);
                var type = CType.Resolve(baseType, stack);
                
                if (_lexer.PeekToken('=', ',', ';')) {
                    // 外部オブジェクト定義

                    if (CType.CheckContainOldStyleArgument(type)) {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "関数型中に型の無い仮引数名があります");
                    }

                    if (functionSpecifier != AnsiCParser.FunctionSpecifier.None) {
                        throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "inlineは関数定義に対してのみ使える。");
                    }

                    if (scope == ScopeKind.FileScope) {
                        // 記憶域クラス指定子 auto 及び register が，外部宣言の宣言指定子列の中に現れてはならない。
                        if (storageClass == AnsiCParser.StorageClassSpecifier.Auto || storageClass == AnsiCParser.StorageClassSpecifier.Register) {
                            throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "記憶域クラス指定子 auto 及び register が，外部宣言の宣言指定子列の中に現れてはならない。");
                        }
                    }

                    // ファイル有効範囲のオブジェクトの識別子を，初期化子を使わず，かつ，記憶域クラス指定子なしか又は記憶域クラス指定子 static で宣言する場合，そのオブジェクトの識別子の宣言を仮定義（tentativedefinition）という。
                    // 翻訳単位が，ある識別子に対する仮定義を一つ以上含み，かつその識別子に対する外部定義を含まない場合，その翻訳単位に，翻訳単位の終わりの時点での合成型，及び 0 に等しい初期化子をもったその識別子のファイル有効範囲の宣言がある場合と同じ規則で動作する。
                    //
                    // 整理すると
                    // 仮定義（tentative　definition）とは
                    //  - ファイルスコープで宣言した識別子で、初期化子（初期値）を使っていない
                    //  - 記憶域クラス指定子がなし、もしくは static のもの
                    // 仮定義された識別子に対する外部定義を含まない場合、翻訳単位の末尾に合成型で0の初期化子付き宣言があるとして扱う

                    for (;;) {
                        var decl = FunctionOrVariableOrTypedefDeclaration(ident, type, storageClass, AnsiCParser.FunctionSpecifier.None, scope);
                        decls.Add(decl);

                        if (_lexer.ReadTokenIf(',')) {

                            ident = "";
                            stack = new List<CType>() { new CType.StubType() };
                            Declarator(ref ident, stack, 0);
                            type = CType.Resolve(baseType, stack);
                            continue;
                        }
                        _lexer.ReadToken(';');
                        break;
                    }
                } else if (type.IsFunctionType()) {
                    // 関数定義
                    if (scope == ScopeKind.FileScope) {
                        // ファイルスコープでは関数定義は可能
                        SyntaxTree.Declaration decl = FunctionDefinition(ident, type, storageClass, functionSpecifier);
	                    decls.Add(decl);
                	} else {
                        // ブロックスコープでは関数定義は不可能
                        throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "ブロックスコープ中では関数定義を行うことはできません。");
                    }
                } else {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Start, _lexer.CurrentToken().End, "文法エラーです。");
                }
                return decls;
            }
        }

    }
}
