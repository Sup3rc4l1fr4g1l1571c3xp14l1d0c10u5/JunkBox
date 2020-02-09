using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AnsiCParser.DataType;
using AnsiCParser.SyntaxTree;

namespace AnsiCParser {

    /// <summary>
    /// パーサ
    /// </summary>
    public partial class Parser {

        /// <summary>
        /// 言語レベル
        /// </summary>
        public enum LanguageMode {
            None,
            C89,
            C99    // 完全実装ではない
        }

        /// <summary>
        /// 言語レベルの選択
        /// </summary>
        public static LanguageMode _mode { get; } = LanguageMode.C89;
        /// <summary>
        /// 名前空間(ステートメント ラベル)
        /// </summary>
        private Scope<LabelScopeValue> _labelScope = Scope<LabelScopeValue>.Empty.Extend();

        /// <summary>
        /// 名前空間(構造体、共用体、列挙体のタグ名)
        /// </summary>
        private Scope<TaggedType> _tagScope = Scope<TaggedType>.Empty.Extend();

        /// <summary>
        /// 名前空間(通常の識別子（変数、関数、引数、列挙定数、Typedef名)
        /// </summary>
        private Scope<Declaration> _identScope = Scope<Declaration>.Empty.Extend();

        /// <summary>
        /// リンケージオブジェクト表
        /// </summary>
        private readonly LinkageObjectTable _linkageObjectTable = new LinkageObjectTable();

        /// <summary>
        /// break命令についてのスコープ
        /// </summary>
        private readonly Stack<Statement> _breakScope = new Stack<Statement>();

        /// <summary>
        /// continue命令についてのスコープ
        /// </summary>
        private readonly Stack<Statement> _continueScope = new Stack<Statement>();

        /// <summary>
        /// switch文についてのスコープ
        /// </summary>
        private readonly Stack<Statement.SwitchStatement> _switchScope = new Stack<Statement.SwitchStatement>();

        /// <summary>
        /// 現在解析中の関数定義
        /// </summary>
        private Declaration.FunctionDeclaration _currentFuncDecl;


        /// <summary>
        /// 暗黙的宣言の挿入対象となる宣言部
        /// </summary>
        private readonly Stack<List</*Declaration*/Ast>> _insertImplicitDeclarationOperatorStack = new Stack<List</*Declaration*/Ast>>();

        private int stringLiteralLabelCnt = 0;

        private string allocStringLabel() {
            return $"str@${stringLiteralLabelCnt++}";
        }

        /// <summary>
        ///  暗黙的関数宣言を挿入
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private Declaration AddImplicitFunctionDeclaration(Token ident, CType type) {
            var storageClass = AnsiCParser.StorageClassSpecifier.Extern;
            var functionSpecifier = AnsiCParser.FunctionSpecifier.None;
            var funcDecl = FunctionDeclaration(ident, type, storageClass, functionSpecifier, ScopeKind.FileScope, false);
            _insertImplicitDeclarationOperatorStack.Peek().Add(funcDecl);
            return funcDecl;
        }

        /// <summary>
        ///  暗黙的型宣言を挿入
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="type"></param>
        private void AddImplicitTypeDeclaration(Token ident, CType type) {
            var typeDecl = new Declaration.TypeDeclaration(ident.Range, ident.Raw, type);
            _insertImplicitDeclarationOperatorStack.Peek().Add(typeDecl);
        }

        /// <summary>
        /// 字句解析器
        /// </summary>
        private readonly Lexer _lexer;

        public Parser(string source, string fileName = "<built-in>") {
            _lexer = new Lexer(source, fileName);

            _identScope.Add("__builtin_va_list",
                new Declaration.TypeDeclaration(
                    LocationRange.Builtin,
                    "__builtin_va_list",
                    CType.CreatePointer(CType.CreateVoid())
                )
            );
        }

        /// <summary>
        /// 構文解析と意味解析を行い、構文木を返す
        /// </summary>
        /// <returns></returns>
        public Ast Parse() {
            return TranslationUnit();
        }


        /// <summary>
        /// 6.2 結合(リンケージ)
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        private static LinkageKind ResolveLinkage(Token ident, CType type, StorageClassSpecifier storageClass, ScopeKind scope, Scope<Declaration> identScope, bool hasInitializer) {
            // 記憶域クラス指定からリンケージを求める
            switch (storageClass) {
                case AnsiCParser.StorageClassSpecifier.Auto:
                case AnsiCParser.StorageClassSpecifier.Register:
                    if (scope == ScopeKind.FileScope) {
                        // 記憶域クラス指定子 auto 及び register が，外部宣言の宣言指定子列の中に現れてはならない。
                        throw new CompilerException.SpecificationErrorException(ident.Range, "記憶域クラス指定子 auto 及び register が，外部宣言の宣言指定子列の中に現れてはならない。");
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
                            throw new CompilerException.SpecificationErrorException(ident.Range, "関数宣言が記憶域クラス指定子 static をもつことができるのは，ファイル有効範囲をもつ宣言の場合だけである");
                        }
                    }
                    if (scope == ScopeKind.FileScope && (type.IsObjectType() || type.IsFunctionType() || (type.IsArrayType() && hasInitializer))) {
                        // 6.2.2 識別子の結合
                        // オブジェクト又は関数に対するファイル有効範囲の識別子の宣言が記憶域クラス指定子 static を含む場合，その識別子は，内部結合をもつ
                        return LinkageKind.InternalLinkage;
                    } else if (scope == ScopeKind.BlockScope) {
                        // 記憶域クラス指定子 extern を伴わないブロック有効範囲のオブジェクトを宣言する識別子は，無結合とする
                        return LinkageKind.NoLinkage;  // 無結合
                    } else {
                        throw new CompilerException.SpecificationErrorException(ident.Range, "オブジェクト又は関数に対するファイル有効範囲の識別子の宣言、もしくは、記憶域クラス指定子 extern を伴わないブロック有効範囲のオブジェクト、のどちらでもない識別子の宣言で static が使用されている。");
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
                        }

                        // 記憶域クラス指定子 extern を伴わないブロック有効範囲のオブジェクトを宣言する識別子は，無結合とする
                        return LinkageKind.NoLinkage;// 無結合

                    }
                case AnsiCParser.StorageClassSpecifier.Extern: {
                        // 識別子が，その識別子の以前の宣言が可視である有効範囲において，記憶域クラス指定子 extern を伴って宣言される場合，次のとおりとする。
                        Declaration iv;
                        if (identScope.TryGetValue(ident.Raw, out iv)) {
                            switch (iv.LinkageObject.Linkage) {
                                case LinkageKind.ExternalLinkage:
                                case LinkageKind.InternalLinkage:
                                    // - 以前の宣言において内部結合又は外部結合が指定されているならば，新しい宣言における識別子は，以前の宣言と同じ結合をもつ。
                                    return iv.LinkageObject.Linkage;
                                case LinkageKind.NoLinkage:
                                    // - 可視である以前の宣言がない場合，又は以前の宣言が無結合である場合，この識別子は外部結合をもつ。
                                    // => 以前の宣言が無結合なのでこの識別子は外部結合をもつ。
                                    return LinkageKind.ExternalLinkage;// 外部結合
                                default:
                                    throw new Exception("");
                            }
                        }

                        // - 可視である以前の宣言がない場合，又は以前の宣言が無結合である場合，この識別子は外部結合をもつ。
                        // => 可視である以前の宣言がないのでこの識別子は外部結合をもつ。
                        return LinkageKind.ExternalLinkage;// 外部結合
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
            switch (_lexer.CurrentToken().Kind) {
                case Token.TokenKind.HEXIMAL_CONSTANT:
                case Token.TokenKind.OCTAL_CONSTANT:
                case Token.TokenKind.DECIAML_CONSTANT:
                    return true;
                default:
                    return false;
            }
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
        private static readonly Dictionary<string, BasicType.TypeKind[]> CandidateTypeTableHexOrOct = new Dictionary<string, BasicType.TypeKind[]> {
            {"LLU", new[] { BasicType.TypeKind.UnsignedLongLongInt }},
            {"LL", new[] { BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt }},
            {"LU", new[] { BasicType.TypeKind.UnsignedLongInt, BasicType.TypeKind.UnsignedLongLongInt }},
            {"L", new[] { BasicType.TypeKind.SignedLongInt, BasicType.TypeKind.UnsignedLongInt, BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt }},
            {"U", new[] { BasicType.TypeKind.UnsignedInt, BasicType.TypeKind.UnsignedLongInt, BasicType.TypeKind.UnsignedLongLongInt }},
            {"", new[] { BasicType.TypeKind.SignedInt, BasicType.TypeKind.UnsignedInt, BasicType.TypeKind.SignedLongInt, BasicType.TypeKind.UnsignedLongInt, BasicType.TypeKind.SignedLongLongInt, BasicType.TypeKind.UnsignedLongLongInt }}
        };

        /// <summary>
        /// 10進定数の型候補表
        /// </summary>
        private static readonly Dictionary<string, BasicType.TypeKind[]> CandidateTypeTableDigit = new Dictionary<string, BasicType.TypeKind[]> {
            { "LLU", new[] { BasicType.TypeKind.UnsignedLongLongInt } },
            { "LL", new[] { BasicType.TypeKind.SignedLongLongInt } },
            { "LU", new[] { BasicType.TypeKind.UnsignedLongInt, BasicType.TypeKind.UnsignedLongLongInt } },
            { "L", new[] { BasicType.TypeKind.SignedLongInt, BasicType.TypeKind.SignedLongLongInt } },
            { "U", new[] { BasicType.TypeKind.UnsignedInt, BasicType.TypeKind.UnsignedLongInt, BasicType.TypeKind.UnsignedLongLongInt } },
            { "", new[] { BasicType.TypeKind.SignedInt, BasicType.TypeKind.SignedLongInt, BasicType.TypeKind.SignedLongLongInt } }
        };

        /// <summary>
        /// 6.4.4.1 整数定数
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant IntegerConstant() {
            if (IsIntegerConstant() == false) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"整数定数があるべき場所に {_lexer.CurrentToken().Raw } があります。");
            }

            // 10進定数は基数 10，8進定数は基数 8，16進定数は基数 16 で値を計算する。
            // 字句的に先頭の 数字が最も重みが大きい。
            var token = _lexer.CurrentToken();
            string body;
            string suffix;
            int radix;
            Dictionary<string, BasicType.TypeKind[]> candidateTable;
            switch (token.Kind) {
                case Token.TokenKind.HEXIMAL_CONSTANT: {
                        // 16進定数
                        var m = Lexer.ParseHeximal(token.Raw);
                        body = m.Item1;
                        suffix = m.Item2;
                        radix = 16;
                        candidateTable = CandidateTypeTableHexOrOct;
                        break;
                    }
                case Token.TokenKind.OCTAL_CONSTANT: {
                        // 8進定数
                        var m = Lexer.ParseOctal(token.Raw);
                        body = m.Item1;
                        suffix = m.Item2;
                        radix = 8;
                        candidateTable = CandidateTypeTableHexOrOct;
                        break;
                    }
                case Token.TokenKind.DECIAML_CONSTANT: {
                        // 10進定数
                        var m = Lexer.ParseDecimal(token.Raw);
                        body = m.Item1;
                        suffix = m.Item2;
                        radix = 10;
                        candidateTable = CandidateTypeTableDigit;
                        break;
                    }
                default:
                    throw new Exception();

            }

            BasicType.TypeKind[] candidates;
            if (candidateTable.TryGetValue(suffix.ToUpper(), out candidates) == false) {
                throw new CompilerException.SyntaxErrorException(token.Range, "整数定数のサフィックスが不正です。");
            }


            var originalSigned = Lexer.ToInt64(token.Range, body, radix);
            var originalUnsigned = Lexer.ToUInt64(token.Range, body, radix);

            Int64 value = 0;

            BasicType.TypeKind selectedType = 0;
            Debug.Assert(candidates.Length > 0);
#if false
            foreach (var candidate in candidates) {
                switch (candidate) {
                    case BasicType.TypeKind.SignedInt: {
                            //try {
                            //var v = Lexer.ToInt32(token.Range, body, radix);
                            var v = unchecked((Int32)originalSigned);
                            if (v == originalSigned) {
                                value = v;
                                break;
                            }
                            //} catch (OverflowException) {
                            //    // 大きすぎる。
                            //}
                            continue;
                        }
                    case BasicType.TypeKind.UnsignedInt: {
                            //try {
                            //var v = Lexer.ToUInt32(token.Range, body, radix);
                            var v = unchecked((UInt32)originalUnsigned);
                            if (v == originalUnsigned) {
                                value = v;
                                break;
                            }
                            //} catch (OverflowException) {
                            //    // 大きすぎる。
                            //}
                            continue;
                        }
                    case BasicType.TypeKind.SignedLongInt: {
                            //try {
                            //var v = Lexer.ToInt32(token.Range, body, radix);
                            var v = (Int64)unchecked((Int32)originalSigned);
                            if (v == originalSigned) {
                                value = v;
                                break;
                            }
                            //} catch (OverflowException) {
                            //    // 大きすぎる。
                            //}
                            continue;
                        }
                    case BasicType.TypeKind.UnsignedLongInt: {
                            //try {
                            //var v = Lexer.ToUInt32(token.Range, body, radix);
                            var v = (UInt64)unchecked((UInt32)originalUnsigned);
                            if (v == originalUnsigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            //} catch (OverflowException) {
                            //    // 大きすぎる。
                            //}
                            continue;
                        }
                    case BasicType.TypeKind.SignedLongLongInt: {
                            //var v = Lexer.ToInt64(token.Range, body, radix);
                            var v = unchecked((Int64)originalSigned);
                            if (v == originalSigned) {
                                value = v;
                                break;
                            }
                            continue;
                        }
                    case BasicType.TypeKind.UnsignedLongLongInt: {
                            //var v = Lexer.ToUInt64(token.Range, body, radix);
                            var v = unchecked((UInt64)originalUnsigned);
                            if (v == originalUnsigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    default:
                        throw new CompilerException.InternalErrorException(token.Start, token.End, "整数定数の型変換候補が不正です。");
                }
                selectedType = candidate;
                break;
            }
#else
            foreach (var candidate in candidates) {
                switch (candidate) {
                    case BasicType.TypeKind.SignedInt: {
                            //try {
                            //var v = Lexer.ToInt32(token.Range, body, radix);
                            var v = unchecked((Int32)originalSigned);
                            if (v == originalSigned && v >= 0) {
                                value = v;
                                break;
                            } else {
                                // オーバーフローした
                            }
                            //} catch (OverflowException) {
                            //    // 大きすぎる。
                            //}
                            continue;
                        }
                    case BasicType.TypeKind.UnsignedInt: {
                            //try {
                            //var v = Lexer.ToUInt32(token.Range, body, radix);
                            var v = unchecked((UInt32)originalUnsigned);
                            if (v == originalUnsigned) {
                                value = v;
                                break;
                            } else {
                                // オーバーフローした
                            }
                            //} catch (OverflowException) {
                            //    // 大きすぎる。
                            //}
                            continue;
                        }
                    case BasicType.TypeKind.SignedLongInt: {
                            //try {
                            //var v = Lexer.ToInt32(token.Range, body, radix);
                            var v = unchecked((Int32)originalSigned);
                            if (v == originalSigned && v >= 0) {
                                value = v;
                                break;
                            } else {
                                // オーバーフローした
                            }
                            //} catch (OverflowException) {
                            //    // 大きすぎる。
                            //}
                            continue;
                        }
                    case BasicType.TypeKind.UnsignedLongInt: {
                            //try {
                            //var v = Lexer.ToUInt32(token.Range, body, radix);
                            var v = unchecked((UInt32)originalUnsigned);
                            if (v == originalUnsigned) {
                                value = v;
                                break;
                            } else {
                                // オーバーフローした
                            }
                            //} catch (OverflowException) {
                            //    // 大きすぎる。
                            //}
                            continue;
                        }
                    case BasicType.TypeKind.SignedLongLongInt: {
                            //var v = Lexer.ToInt64(token.Range, body, radix);
                            var v = unchecked((Int64)originalSigned);
                            if (v == originalSigned && v >= 0) {
                                value = v;
                                break;
                            }
                            continue;
                        }
                    case BasicType.TypeKind.UnsignedLongLongInt: {
                            //var v = Lexer.ToUInt64(token.Range, body, radix);
                            var v = unchecked((UInt64)originalUnsigned);
                            if (v == originalUnsigned) {
                                value = unchecked((Int64)v);
                                break;
                            }
                            continue;
                        }
                    default:
                        throw new CompilerException.InternalErrorException(token.Start, token.End, "整数定数の型変換候補が不正です。");
                }
                selectedType = candidate;
                break;
            }
#endif
            _lexer.NextToken();

            return new SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant(token.Range, token.Raw, value, selectedType);
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
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"浮動小数点定数があるべき場所に {_lexer.CurrentToken().Raw } があります。");
            }
            var token = _lexer.CurrentToken();
            var m = Lexer.ScanFloat(token.Raw);
            var value = m.Item1 == 10 ? Convert.ToDouble(m.Item2) : Lexer.ParseHeximalFloat(token.Range, m.Item2, m.Item3, m.Item4);
            BasicType.TypeKind type;
            switch (m.Item4.ToUpper()) {
                case "F":
                    type = BasicType.TypeKind.Float;
                    break;
                case "L":
                    type = BasicType.TypeKind.LongDouble;
                    break;
                case "":
                    type = BasicType.TypeKind.Double;
                    break;
                default:
                    throw new CompilerException.SyntaxErrorException(token.Range, "浮動小数点定数のサフィックスが不正です。");
            }
            _lexer.NextToken();
            return new SyntaxTree.Expression.PrimaryExpression.Constant.FloatingConstant(token.Range, token.Raw, value, type);
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
            Declaration.EnumMemberDeclaration value;
            if (_identScope.TryGetValue(ident.Raw, out value) == false) {
                return false;
            }
            return value.MemberInfo.ParentType.Members.First(x => x.Ident.Raw == ident.Raw) != null;
        }

        /// <summary>
        /// 6.4.4.3 列挙定数
        /// </summary>
        /// <returns></returns>
        private SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant EnumerationConstant() {
            var ident = Identifier(false);
            Declaration.EnumMemberDeclaration value;
            if (_identScope.TryGetValue(ident.Raw, out value) == false) {
                throw new CompilerException.SyntaxErrorException(ident.Range, $"列挙定数 { _lexer.CurrentToken().Raw } は未宣言です。");
            }
            var el = value.MemberInfo.ParentType.Members.First(x => x.Ident.Raw == ident.Raw);
            return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant(ident.Range, el);
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
        private SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant CharacterConstant() {
            if (IsCharacterConstant() == false) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"文字定数があるべき場所に {_lexer.CurrentToken().Raw } があります。");
            }
            var token = _lexer.CurrentToken();
            _lexer.NextToken();
            return new SyntaxTree.Expression.PrimaryExpression.Constant.CharacterConstant(token.Range, token.Raw);
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
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"文字列リテラルがあるべき場所に {_lexer.CurrentToken().Raw } があります。");
            }
            var ret = _lexer.CurrentToken().Raw;
            _lexer.NextToken();
            return ret;
        }

#endregion


        /// <summary>
        /// ファイル終端
        /// </summary>
        private void EoF() {
            if (!_lexer.IsEof()) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, "ファイルが正しく終端していません。");
            }
        }

        /// <summary>
        /// 6.9 外部定義(翻訳単位)
        /// </summary>
        /// <returns></returns>
        public TranslationUnit TranslationUnit() {
            var start = _lexer.CurrentToken().Start;
            var ret = new TranslationUnit(LocationRange.Empty);

            // 暗黙的宣言の処理スタックに追加対象の宣言リストを積む
            _insertImplicitDeclarationOperatorStack.Push(ret.Declarations);

            // 定義を全て解析
            while (IsExternalDeclaration(null, AnsiCParser.TypeSpecifier.None)) {
                ret.Declarations.AddRange(ExternalDeclaration());
            }

            // 暗黙的宣言の処理スタックから宣言リストを外す
            _insertImplicitDeclarationOperatorStack.Pop();

            // ファイル終端の確認
            EoF();

            // ソースファイル上での範囲情報を更新
            var end = _lexer.CurrentToken().End;
            ret.LocationRange = new LocationRange(start, end);

            // 6.9.2 外部オブジェクト定義
            // 翻訳単位が，ある識別子に対する仮定義を一つ以上含み，かつその識別子に対する外部定義を含まない場合，
            // その翻訳単位に，翻訳単位の終わりの時点での合成型，及び 0 に等しい初期化子をもったその識別子のファイル有効範囲の宣言がある場合と同じ規則で動作する。
            foreach (var entry in _linkageObjectTable.LinkageObjects) {
                if (entry.Definition == null) {
                    {
                        var definition = entry.TentativeDefinitions.FirstOrDefault(x => x.Type.IsIncompleteType() && !x.Type.IsNoLengthArrayType());
                        if (definition != null) {
                            //if (entry.TentativeDefinitions.Any(x => x.Type.IsIncompleteType() && !x.Type.IsNoLengthArrayType())) {
                            throw new CompilerException.SpecificationErrorException(definition.LocationRange, "不完全型のままの識別子があります。");
                        }
                    }

                    {
                        var definition = entry.TentativeDefinitions.FirstOrDefault(x => x.StorageClass != AnsiCParser.StorageClassSpecifier.Extern);
                        if (definition != null) {
                            //if (entry.TentativeDefinitions.First().StorageClass != AnsiCParser.StorageClassSpecifier.Extern) {
                            //entry.Definition = entry.TentativeDefinitions[0];
                            //entry.TentativeDefinitions.RemoveAt(0);
                            entry.Definition = definition;
                            entry.TentativeDefinitions.Remove(definition);
                        }
                    }
                }
            }

            // 翻訳単位内でのリンケージ情報を保存
            ret.LinkageTable = _linkageObjectTable.LinkageObjects;
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
        private List<Declaration> ExternalDeclaration() {
            return ReadDeclaration(ScopeKind.FileScope);
        }

        /// <summary>
        /// 6.9.1　関数定義
        /// </summary>
        /// <param name="ident">関数の識別子</param>
        /// <param name="type">関数の型</param>
        /// <param name="storageClass">記憶指定子クラス</param>
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
        private Declaration FunctionDefinition(Token ident, CType type, StorageClassSpecifier storageClass, FunctionSpecifier functionSpecifier) {

            // 関数定義で宣言する識別子（その関数の名前）の型が関数型であることは，その関数定義の宣言子の部分で指定しなければならない
            // (これは，関数定義の型を typedef から受け継ぐことはできないことを意味する。)。
            if (type.UnwrapTypeQualifier() is TypedefType) {
                throw new CompilerException.SpecificationErrorException(ident.Range, "関数定義で宣言する識別子（その関数の名前）の型が関数型であることは，その関数定義の宣言子の部分で指定しなければならない。");
            }

            var ftype = type.Unwrap() as FunctionType;
            Debug.Assert(ftype != null);

            // 宣言並びがあるなら読み取る
            var argmuents = OldStyleFunctionArgumentDeclarations();

            if (ftype.Arguments == null && argmuents != null) {
                // 識別子並び: なし
                // 宣言並び: あり
                // -> 仮引数用の宣言並びがあるのに、識別子並びがない
                throw new CompilerException.SpecificationErrorException(ident.Range, "K&R形式の関数定義において仮引数型並びがあるのに、識別並びが空です。");
            }

            if (ftype.Arguments == null && argmuents == null) {
                // 識別子並び: なし
                // 仮引数型並び: なし
                // -> ANSI形式で引数無し(引数部がvoid)の関数定義（関数宣言時とは意味が違う）
                ftype.SetArguments(new FunctionType.ArgumentInfo[0]);
            } else if (ftype.Arguments != null && argmuents != null) {
                // 識別子並びあり、仮引数並びあり。

                // 関数宣言が古い形式であることを確認
                if (ftype.GetFunctionStyle() != FunctionType.FunctionStyle.OldStyle) {
                    // 仮引数型並び、もしくは識別子並びの省略、もしくは識別子並びと仮引数型並びの混在である。
                    // 識別子並び中に型名を記述してしまった場合もこのエラーになる
                    throw new CompilerException.SpecificationErrorException(ident.Range, "関数定義中でK&R形式の識別子並びとANSI形式の仮引数型並びが混在している");
                }

                // 識別子並びの中に無いものが宣言並びにあるか調べる
                if (argmuents.Select(x => x.Item1).Except(ftype.Arguments.Select(x => x.Ident), (x, y) => x.Raw == y.Raw).Any()) {
                    // 識別子並び中の要素以外が宣言並びにある。
                    throw new CompilerException.SpecificationErrorException(ident.Range, "識別子並び中の要素以外が宣言並びにある。");
                }

                // K&R形式の識別子並びに宣言並びの型情報を既定の実引数拡張を伴って反映させる。
                // 宣言並びを名前引きできる辞書に変換
                var dic = argmuents.ToDictionary(x => x.Item1.Raw, x => x);

                // 型宣言側の仮引数
                var mapped = ftype.Arguments.Select(x => {
                    if (dic.ContainsKey(x.Ident.Raw)) {
                        var dapType = dic[x.Ident.Raw].Item2.DefaultArgumentPromotion();
                        if (CType.IsEqual(dapType, dic[x.Ident.Raw].Item2) == false) {
                            throw new CompilerException.TypeMissmatchError(x.Ident.Range, $"仮引数 {x.Ident.Raw} は既定の実引数拡張で型が変化します。");
                        }
                        return new FunctionType.ArgumentInfo(x.Range, x.Ident, dic[x.Ident.Raw].Item3, dic[x.Ident.Raw].Item2.DefaultArgumentPromotion());
                    }

                    return new FunctionType.ArgumentInfo(x.Range, x.Ident, AnsiCParser.StorageClassSpecifier.None, CType.CreateSignedInt().DefaultArgumentPromotion());
                }).ToList();

                ftype.SetArguments(mapped.ToArray());

            } else if (ftype.Arguments != null && argmuents == null) {
                // 識別子並びあり

                // 関数宣言の形式を確認
                switch (ftype.GetFunctionStyle()) {
                    case FunctionType.FunctionStyle.OldStyle:
                        // 関数宣言が古い形式である
                        ftype.SetArguments(ftype.Arguments.Select(x => new FunctionType.ArgumentInfo(x.Range, x.Ident, AnsiCParser.StorageClassSpecifier.None, CType.CreateSignedInt().DefaultArgumentPromotion())).ToArray());
                        break;
                    case FunctionType.FunctionStyle.NewStyle:
                    case FunctionType.FunctionStyle.AmbiguityStyle:
                        // 関数宣言が新しい形式、もしくは曖昧な形式
                        break;
                    default:
                        // 識別子並びと仮引数型並びの混在である。
                        // 識別子並び中に型名を記述してしまった場合もこのエラーになる
                        throw new CompilerException.SpecificationErrorException(ident.Range, "関数定義中でK&R形式の識別子並びとANSI形式の仮引数型並びが混在している");
                }

                // 関数型中に不完全型が無いことを確認する
                if (ftype.Arguments.Length == 1 && ftype.Arguments[0].Type.IsVoidType()) {
                    // 唯一の引数が void 型なので問題なし
                } else if (/*ftype.ResultType.IsIncompleteType() || */ftype.Arguments.Any(x => x.Type.IsIncompleteType())) {
                    throw new CompilerException.SpecificationErrorException(ident.Range, "関数宣言中で不完全型を直接使うことはできません。");
                }

            } else {
                throw new CompilerException.InternalErrorException(ident.Range, "ありえない。バグかなにかでは？");
            }

            // 6.9.1 関数定義

            // 宣言部分を読み取る
            var funcdecl = FunctionDeclaration(ident, ftype, storageClass, functionSpecifier, ScopeKind.FileScope, true);

            // 各スコープを積む
            _tagScope = ftype.PrototypeTaggedScope ?? _tagScope.Extend();
            _identScope = ftype.PrototypeIdentScope ?? _identScope.Extend();
            _labelScope = _labelScope.Extend();
            _currentFuncDecl = funcdecl;

            // 引数をスコープに追加
            if (ftype.Arguments != null) {
                if (ftype.Arguments.Length == 1 && ftype.Arguments[0].Type.IsVoidType()) {
                    if (ftype.Arguments[0].Ident != null) {
                        throw new CompilerException.SpecificationErrorException(ftype.Arguments[0].Range, "void 型の引数に名前を与えることはできません。");
                    }
                } else {
                    foreach (var arg in ftype.Arguments) {
                        if (arg.Ident == null) {
                            throw new CompilerException.SpecificationErrorException(arg.Range, "関数定義では引数名を省略することはできません。");
                        }
                        if (arg.Type.IsIncompleteType()) {
                            throw new CompilerException.SpecificationErrorException(arg.Range, "関数定義では引数名を省略することはできません。");
                        }
                        _identScope.Add(arg.Ident.Raw, new Declaration.ArgumentDeclaration(arg.Ident.Range, arg.Ident.Raw, arg.Type, arg.StorageClass));    // 引数は無結合
                    }
                }
            }

            // 関数本体（複文）を解析
            // 引数変数を複文スコープに入れるために、関数側でスコープを作り、複文側でスコープを作成しないようにする。
            funcdecl.Body = CompoundStatement(skipCreateNewScope: true, funcName: ident.Raw);


            // 未定義ラベルと未使用ラベルを調査
            foreach (var scopeValue in _labelScope) {
                if (scopeValue.Item2.Declaration == null && scopeValue.Item2.References.Any()) {
                    // 未定義のラベルが使われている。
                    scopeValue.Item2.References.ForEach(x => { throw new CompilerException.SpecificationErrorException(x.LocationRange, $"未定義のラベル {x.Label} が使用されています。"); });
                }
                if (scopeValue.Item2.Declaration != null && !scopeValue.Item2.References.Any()) {
                    // 未参照のラベルが使われている。
                    Logger.Warning(scopeValue.Item2.Declaration.LocationRange, $"ラベル {scopeValue.Item1} は定義されていますがどこからも参照されていません。");
                }
            }

            //各スコープから出る
            _currentFuncDecl = null;
            _labelScope = _labelScope.Parent;
            _identScope = _identScope.Parent;
            _tagScope = _tagScope.Parent;

            return funcdecl;
        }


        /// <summary>
        /// 6.9.1　関数定義(宣言並び)
        /// </summary>
        /// <returns></returns>
        private List<Tuple<Token, CType, StorageClassSpecifier>> OldStyleFunctionArgumentDeclarations() {
            if (IsOldStyleFunctionArgumentDeclaration()) {
                // 宣言並びがあるので読み取る
                var decls = new List<Tuple<Token, CType, StorageClassSpecifier>>();
                while (IsOldStyleFunctionArgumentDeclaration()) {
                    decls = OldStyleFunctionArgumentDeclaration(decls);
                }
                return decls;
            }

            // 宣言並びがない
            return null;
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
        private List<Tuple<Token, CType, StorageClassSpecifier>> OldStyleFunctionArgumentDeclaration(List<Tuple<Token, CType, StorageClassSpecifier>> decls) {

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
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Range, "宣言並びの中の宣言は，register 以外の記憶域クラス指定子を含んではならない。");
            }

            // 初期化宣言子並び
            if (!_lexer.PeekToken(';')) {
                // 一つ以上の初期化宣言子がある？
                do {
                    var declaration = OldStyleFunctionArgumentInitDeclarator(baseType, storageClass);
                    // 宣言子並びは無結合なので再定義できない。
                    if (decls.Any(x => x.Item1.Raw == declaration.Item1.Raw)) {
                        throw new CompilerException.SpecificationErrorException(declaration.Item1.Range, $"宣言並び中で識別子{declaration.Item1}が再定義されました。");
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
        private Tuple<Token, CType, StorageClassSpecifier> OldStyleFunctionArgumentInitDeclarator(CType type, StorageClassSpecifier storageClass) {
            // 宣言子
            Token ident = null;
            List<CType> stack = new List<CType> { new StubType() };
            Declarator(ref ident, stack, 0);
            type = CType.Resolve(type, stack);

            if (CType.CheckContainOldStyleArgument(type)) {
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Range, "関数型中に型の無い仮引数名があります");
            }

            if (_lexer.ReadTokenIf('=')) {
                // 初期化子を伴う宣言
                // 宣言並びの中の宣言は初期化を含んではならない
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Range, "宣言並びの中の宣言は初期化を含んではならない。");
            }

            if (storageClass != AnsiCParser.StorageClassSpecifier.Register && storageClass != AnsiCParser.StorageClassSpecifier.None) {
                // 6.9.1 
                // 制約 宣言並びの中の宣言は，register 以外の記憶域クラス指定子及び初期化を含んではならない。
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Range, "宣言並びの中の宣言は，register 以外の記憶域クラス指定子を含んではならない。");
            }

            if (type.IsFunctionType()) {
                // 仮引数を“～型を返却する関数”とする宣言は，6.3.2.1 の規定に従い，“～型を返却する関数へのポインタ”に型調整する。
                Logger.Warning(ident.Range, $"仮引数{ident}は“～型を返却する関数”として宣言されていますが，6.3.2.1 の規定に従い，“～型を返却する関数へのポインタ”に型調整します。");
                return Tuple.Create(ident, (CType)CType.CreatePointer(type), storageClass);
            }

            return Tuple.Create(ident, type, storageClass);
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
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, "記憶クラス指定子 /型指定子/型修飾子が一つ以上指定されている必要がある。");
            }
            Debug.Assert(functionSpecifier == AnsiCParser.FunctionSpecifier.None);

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
        private Declaration FunctionOrVariableOrTypedefDeclaration(
            Token ident,
            CType type,
            StorageClassSpecifier storageClass,
            FunctionSpecifier functionSpecifier,
            ScopeKind scopeKind
        ) {
            if (_lexer.ReadTokenIf('=')) {
                // 初期化子を伴うので変数の定義
                return VariableDeclaration(ident, type, storageClass, scopeKind, true);
            }
            // 初期化式を伴わないため、関数宣言、Typedef宣言、変数の仮定義のどれか
            // オブジェクトに対する識別子の宣言が仮定義であり，内部結合をもつ場合，その宣言の型は不完全型であってはならない。

            FunctionType functionType;
            if (type.IsFunctionType(out functionType) && functionType.GetFunctionStyle() == FunctionType.FunctionStyle.OldStyle) {
                // 6.7.5.3 関数宣言子（関数原型を含む）
                // 関数定義の一部でない関数宣言子における識別子並びは，空でなければならない。
                // 脚注　関数宣言で古い形式は使えない。
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, "関数定義の一部でない関数宣言子における識別子並びは，空でなければならない。");
            }

            if (storageClass == AnsiCParser.StorageClassSpecifier.Typedef) {
                // 型宣言名
                return TypedefDeclaration(ident, type);
            }

            if (type.IsFunctionType()) {
                // 関数宣言
                return FunctionDeclaration(ident, type, storageClass, functionSpecifier, scopeKind, false);
            }

            // 変数の仮定義(tentative definition)
            return VariableDeclaration(ident, type, storageClass, scopeKind, false);
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
        private List<Declaration> Declaration() {
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
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, "記憶クラス指定子 /型指定子/型修飾子が一つ以上指定されている必要がある。");
            }
            Debug.Assert(functionSpecifier == AnsiCParser.FunctionSpecifier.None);

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
                (IsTypedefName() && type == null && typeSpecifier == AnsiCParser.TypeSpecifier.None) || // typedef名 と TypeSpecifier は組み合わせられない。 typedef int I; unsigned I x; は妥当だがtypedef unsigned U; U int x; は妥当ではない。
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
            if (_mode == LanguageMode.C89) {
                return _lexer.PeekToken(
                    Token.TokenKind.VOID, Token.TokenKind.CHAR, Token.TokenKind.INT, Token.TokenKind.FLOAT, Token.TokenKind.DOUBLE, Token.TokenKind.SHORT, Token.TokenKind.LONG, Token.TokenKind.SIGNED, Token.TokenKind.UNSIGNED
                );
            } else {
                return _lexer.PeekToken(
                    Token.TokenKind.VOID, Token.TokenKind.CHAR, Token.TokenKind.INT, Token.TokenKind.FLOAT, Token.TokenKind.DOUBLE, Token.TokenKind.SHORT, Token.TokenKind.LONG, Token.TokenKind.SIGNED, Token.TokenKind.UNSIGNED,
                    Token.TokenKind._BOOL, Token.TokenKind._COMPLEX, Token.TokenKind._IMAGINARY
                );
            }
        }

        /// <summary>
        /// 匿名型に割り当てる名前を生成するためのカウンター
        /// </summary>
        private int _anonymousNameCounter;

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
                case Token.TokenKind._BOOL:
                    if (_mode == LanguageMode.C89) {
                        throw new Exception();
                    }
                    _lexer.NextToken();
                    return AnsiCParser.TypeSpecifier._Bool;
                case Token.TokenKind._COMPLEX:
                    if (_mode == LanguageMode.C89) {
                        throw new Exception();
                    }
                    _lexer.NextToken();
                    return AnsiCParser.TypeSpecifier._Complex;
                case Token.TokenKind._IMAGINARY:
                    if (_mode == LanguageMode.C89) {
                        throw new Exception();
                    }
                    _lexer.NextToken();
                    return AnsiCParser.TypeSpecifier._Imaginary;
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
            var tok = _lexer.ReadToken(Token.TokenKind.STRUCT, Token.TokenKind.UNION);
            var kind = tok.Kind == Token.TokenKind.STRUCT ? TaggedType.StructUnionType.StructOrUnion.Struct : TaggedType.StructUnionType.StructOrUnion.Union;

            // 識別子の有無で分岐
            if (IsIdentifier(true)) {

                var token = Identifier(true);
                var ident = token.Raw;

                // 波括弧の有無で分割
                if (_lexer.ReadTokenIf('{')) {
                    // 識別子を伴う完全型の宣言
                    TaggedType tagType;
                    TaggedType.StructUnionType structUnionType;
                    bool isCurrent;
                    if (_tagScope.TryGetValue(ident, out tagType, out isCurrent) == false || isCurrent == false) {
                        // タグ名前表に無い場合、もしくは外側のスコープで宣言されている場合は新しく追加する。
                        if (tagType != null && isCurrent == false) {
                            Logger.Warning(token.Range, $"構造体/共用体 タグ名 {ident} の宣言は外側のスコープで宣言されている同じタグ名の宣言を隠します。");
                        }
                        structUnionType = new TaggedType.StructUnionType(kind, ident, false);
                        _tagScope.Add(ident, structUnionType);
                        AddImplicitTypeDeclaration(token, structUnionType);
                    } else if (!(tagType is TaggedType.StructUnionType)) {
                        throw new CompilerException.SpecificationErrorException(token.Range, $"構造体/共用体 {ident} は既に列挙型として定義されています。");
                    } else if ((tagType as TaggedType.StructUnionType).Kind != kind) {
                        throw new CompilerException.SpecificationErrorException(token.Range, $"構造体/共用体 {ident} は既に定義されていますが、構造体/共用体の種別が一致しません。");
                    } else if ((tagType as TaggedType.StructUnionType).Members != null) {
                        throw new CompilerException.SpecificationErrorException(token.Range, $"構造体/共用体 {ident} は既に完全型として定義されています。");
                    } else {
                        // 不完全型として定義されているので完全型にするために書き換え対象とする
                        structUnionType = (tagType as TaggedType.StructUnionType);
                    }
                    // メンバ宣言並びを解析する
                    var ret = StructDeclarations(kind == TaggedType.StructUnionType.StructOrUnion.Union);
                    structUnionType.HasFlexibleArrayMember = ret.Item1;
                    structUnionType.Members = ret.Item2;
                    _lexer.ReadToken('}');
                    structUnionType.Build();
                    return structUnionType;
                } else {
                    // 不完全型の宣言
                    TaggedType tagType;
                    if (_tagScope.TryGetValue(ident, out tagType) == false) {
                        // タグ名前表に無い場合は新しく追加する。
                        tagType = new TaggedType.StructUnionType(kind, ident, false);
                        _tagScope.Add(ident, tagType);
                        AddImplicitTypeDeclaration(token, tagType);
                    } else if (!(tagType is TaggedType.StructUnionType)) {
                        throw new CompilerException.SpecificationErrorException(token.Range, $"構造体/共用体 {ident} は既に列挙型として定義されています。");
                    } else if ((tagType as TaggedType.StructUnionType).Kind != kind) {
                        throw new CompilerException.SpecificationErrorException(token.Range, $"構造体/共用体 {ident} は既に定義されていますが、構造体/共用体の種別が一致しません。");
                    }

                    return tagType;
                }
            } else {
                // 識別子を伴わない匿名の完全型の宣言

                // 名前を生成
                var ident = $"${kind}_{_anonymousNameCounter++}";

                // 型情報を生成する
                var structUnionType = new TaggedType.StructUnionType(kind, ident, true);

                // タグ名前表に追加する
                _tagScope.Add(ident, structUnionType);
                AddImplicitTypeDeclaration(new Token(Token.TokenKind.IDENTIFIER, _lexer.CurrentToken().Start, _lexer.CurrentToken().End, ident), structUnionType);

                // メンバ宣言並びを解析する
                _lexer.ReadToken('{');
                var ret = StructDeclarations(kind == TaggedType.StructUnionType.StructOrUnion.Union);
                structUnionType.HasFlexibleArrayMember = ret.Item1;
                structUnionType.Members = ret.Item2;
                _lexer.ReadToken('}');

                structUnionType.Build();

                return structUnionType;
            }
        }

        /// <summary>
        /// 6.7.2.1 構造体指定子及び共用体指定子(メンバ宣言並び)
        /// </summary>
        /// <returns></returns>
        private Tuple<bool, List<TaggedType.StructUnionType.MemberInfo>> StructDeclarations(bool isUnion) {
            if (_lexer.PeekToken('}')) {
                // 空の構造体/共用体を使っている。
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Range, $"空の構造体/共用体が宣言されていますが、これはC言語の標準規格では認められおらず、未定義の動作となります（ISO/IEC 9899：1999：6.2.5-20および、J.2未定義の動作を参照）。（捕捉：C++では空の構造体/共用体は認められています。）");
            }
            var items = new List<TaggedType.StructUnionType.MemberInfo>();
            items.AddRange(StructDeclaration());
            while (IsStructDeclaration()) {
                items.AddRange(StructDeclaration());
            }

            // 「フレキシブル配列メンバを持つ構造体」を構造体のメンバにはできない。
            // さらに、共用体のメンバにはできるが、その共用体を構造体のメンバにはできない。
            // つまり、構造的に「フレキシブル配列メンバを持つ構造体」は構造体のメンバにできない。
            if (isUnion == false) {
                foreach (var item in items) {
                    if (item.Type.IsContainFlexibleArrayMemberStruct()) {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Range, $"フレキシブル配列メンバを持つ構造体を構造体が含むことはできません。（これは「フレキシブル配列メンバを持つ構造体」を共用体で包み、その共用体を構造体で包むような場合も禁止しています。）");
                    }
                }
            }


            // 6.7.2.1
            // 特別な場合として，二つ以上の名前付きメンバをもつ構造体の最後のメンバは，不完全配列型をもってもよい。これをフレキシブル配列メンバ（flexible array member）と呼ぶ。
            bool hasFlexibleArrayMember = false;
            if (items.Count >= 2) {
                var lastItem = items.Last();
                if (lastItem.Type.IsArrayType()) {
                    var at = lastItem.Type as ArrayType;
                    if (at.Length == -1) {
                        if (isUnion == false) {
                            // 最後の要素がフレキシブル配列メンバである
                            hasFlexibleArrayMember = true;
                        } else {
                            throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Range, $"フレキシブル配列メンバがありますが、共用体型中では使えません。）");
                        }
                    }
                }

            }

            if (hasFlexibleArrayMember) {
                foreach (var item in items.Take(items.Count - 1)) {
                    if (item.Type.IsIncompleteType()) {
                        throw new CompilerException.SpecificationErrorException(item.Ident.Range, $"不完全型であるメンバを持つことはできません。");
                    }
                }
            } else {
                foreach (var item in items) {
                    if (item.Type.IsIncompleteType()) {
                        throw new CompilerException.SpecificationErrorException(item.Ident.Range, $"不完全型であるメンバを持つことはできません。");
                    }
                }
            }

            return Tuple.Create(hasFlexibleArrayMember, items);
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
        private List<TaggedType.StructUnionType.MemberInfo> StructDeclaration() {
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
            var start = _lexer.CurrentToken().Start;
            if (ReadDeclarationSpecifiers(ref baseType, ref storageClass, ref functionSpecifier, ReadDeclarationSpecifierPartFlag.SpecifierQualifiers) < 1) {
                var end = _lexer.CurrentToken().End;
                throw new CompilerException.SyntaxErrorException(start, end, "型指定子/型修飾子が一つ以上指定されている必要がある。");
            }
            Debug.Assert(storageClass == AnsiCParser.StorageClassSpecifier.None);
            Debug.Assert(functionSpecifier == AnsiCParser.FunctionSpecifier.None);

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
        private List<TaggedType.StructUnionType.MemberInfo> StructDeclaratorList(CType type) {
            var ret = new List<TaggedType.StructUnionType.MemberInfo>();
            ret.Add(StructDeclarator(type));
            while (_lexer.ReadTokenIf(',')) {
                ret.Add(StructDeclarator(type));
            }
            return ret;
        }

        /// <summary>
        /// 6.7.2.1 型指定子型修飾子並び（ビットフィールド長を読み取る）
        /// </summary>
        /// <returns></returns>
        private sbyte BitFieldSize() {
            var expr = ConstantExpression();
            var ret = ExpressionEvaluator.Eval(expr);
            long? size = (long?)(ret as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)?.Value;
            if (size.HasValue == false || size.Value < 0) {
                throw new CompilerException.SpecificationErrorException(expr.LocationRange, "ビットフィールド長には0以上の定数式を指定してください。");
            }
            if (size.Value > sbyte.MaxValue) {
                throw new CompilerException.SpecificationErrorException(expr.LocationRange, $"ビットフィールド長には{sbyte.MaxValue}以下の定数式を指定してください。");
            }

            return (sbyte)size.Value;
        }
        /// <summary>
        /// 6.7.2.1 型指定子型修飾子並び（メンバ宣言子）

        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private TaggedType.StructUnionType.MemberInfo StructDeclarator(CType type) {
            if (IsDeclarator()) {
                // 宣言子
                List<CType> stack = new List<CType> { new StubType() };
                Token ident = null;
                Declarator(ref ident, stack, 0);
                type = CType.Resolve(type, stack);
                if (CType.CheckContainOldStyleArgument(type)) {
                    throw new CompilerException.SpecificationErrorException(ident.Range, $"関数型中に型の無い仮引数名 {ident.Raw} があります");
                }

                // ビットフィールド部分(opt)
                sbyte? size = _lexer.ReadTokenIf(':') ? BitFieldSize() : (sbyte?)null;

                if (size.HasValue) {
                    return new TaggedType.StructUnionType.MemberInfo(ident, new BitFieldType(ident, type, 0, size.Value), 0);
                }

                return new TaggedType.StructUnionType.MemberInfo(ident, type, 0);
            }

            if (_lexer.ReadTokenIf(':')) {
                // ビットフィールド部分(must)
                sbyte size = BitFieldSize();
                return new TaggedType.StructUnionType.MemberInfo(null, new BitFieldType(null, type, 0, size), 0);
            }

            throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, "構造体/共用体のメンバ宣言子では、宣言子とビットフィールド部の両方を省略することはできません。無名構造体/共用体を使用できるのは規格上はC11からです。(C11 6.7.2.1で規定)。");
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
                TaggedType taggedType;
                bool newDecl = false;
                bool isCurrent;
                if (_tagScope.TryGetValue(ident.Raw, out taggedType, out isCurrent) == false) {
                    // タグ名前表に無い場合は新しく追加する。
                    taggedType = new TaggedType.EnumType(ident.Raw, false);
                    _tagScope.Add(ident.Raw, taggedType);
                    AddImplicitTypeDeclaration(ident, taggedType);
                    newDecl = true;
                } else if (!(taggedType is TaggedType.EnumType)) {
                    throw new CompilerException.SpecificationErrorException(ident.Range, $"列挙型 {ident.Raw} は既に構造体/共用体として定義されています。");
                }

                if (_lexer.ReadTokenIf('{')) {
                    if (isCurrent == false && newDecl == false) {
                        // 今のスコープで宣言されていないので新しく追加する。
                        taggedType = new TaggedType.EnumType(ident.Raw, false);
                        _tagScope.Add(ident.Raw, taggedType);
                        AddImplicitTypeDeclaration(ident, taggedType);
                    } else {
                        if ((taggedType as TaggedType.EnumType).Members != null) {
                            throw new CompilerException.SpecificationErrorException(ident.Range, $"列挙型 {ident.Raw} は既に完全型として定義されています。");
                        }
                    }

                    // 不完全型として定義されているので完全型にするために書き換え対象とする
                    (taggedType as TaggedType.EnumType).Members = EnumeratorList(taggedType as TaggedType.EnumType);
                    _lexer.ReadToken('}');
                } else {
                    if ((taggedType as TaggedType.EnumType).Members == null) {
                        throw new CompilerException.SpecificationErrorException(ident.Range, $"不完全型の列挙型 {ident.Raw} を使用していますが、これはC言語の標準規格では認められおらず、未定義の動作となります（ISO/IEC 9899：1999：6.7.2.3の制約を参照）。（捕捉：GNU拡張では不完全型の列挙型の前方宣言を認めています。）");
                    }
                }
                return taggedType;
            } else {
                var ident = $"$enum_{_anonymousNameCounter++}";
                var etype = new TaggedType.EnumType(ident, true);
                _tagScope.Add(ident, etype);
                AddImplicitTypeDeclaration(new Token(Token.TokenKind.IDENTIFIER, _lexer.CurrentToken().Start, _lexer.CurrentToken().Start, ident), etype);
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
        private List<TaggedType.EnumType.MemberInfo> EnumeratorList(TaggedType.EnumType enumType) {
            var ret = new List<TaggedType.EnumType.MemberInfo>();
            enumType.Members = ret;
            var e = Enumerator(enumType, 0);
            Declaration prevDecl;
            bool isCurrent;
            if (_identScope.TryGetValue(e.Ident.Raw, out prevDecl, out isCurrent) && isCurrent) {
                // 6.7.2.2 脚注(107) : したがって、同じ有効範囲で宣言された列挙定数の識別子は，互いに違っていなければならず，通常の宣言子で宣言された他の識別子とも違っていなければならない。
                throw new CompilerException.SpecificationErrorException(e.Ident.Range, $"列挙定数の識別子 {e.Ident.Raw} が宣言されていますが、識別子 {e.Ident.Raw} は既に {prevDecl.LocationRange} で使用されています。C言語の標準規格では同じ有効範囲で宣言された列挙定数の識別子は，互いに違っていなければならず，通常の宣言子で宣言された他の識別子とも違っていなければならず、再定義は認められていません。");
            }
            var decl = new Declaration.EnumMemberDeclaration(e.Ident.Range, e);
            _identScope.Add(e.Ident.Raw, decl);
            ret.Add(e);
            Token t;
            while (_lexer.ReadTokenIf(out t, ',')) {
                var i = e.Value + 1;
                if (IsEnumerator() == false) {
                    if (_mode == LanguageMode.C89) {
                        Logger.Warning(t.Range, "列挙子並びの末尾にコンマを付けることができるのは C99 以降です。C89では使えません。");
                    }
                    break;
                }
                e = Enumerator(enumType, i);
                decl = new Declaration.EnumMemberDeclaration(e.Ident.Range, e);
                _identScope.Add(e.Ident.Raw, decl);
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
        private TaggedType.EnumType.MemberInfo Enumerator(TaggedType.EnumType enumType, int i) {
            var ident = Identifier(false);
            if (_lexer.ReadTokenIf('=')) {
                var expr = ConstantExpression();
                if (expr.Type.IsIntegerType() == false) {
                    throw new CompilerException.SpecificationErrorException(expr.LocationRange, "列挙定数の値には整数値を指定してください。");
                }
                var ret = ExpressionEvaluator.Eval(expr);
                int? value = (int?)(ret as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)?.Value;
                if (value.HasValue == false) {
                    throw new CompilerException.SpecificationErrorException(expr.LocationRange, "列挙定数の値に定数演算できない値が指定されました。");
                }
                i = value.Value;
            }
            return new TaggedType.EnumType.MemberInfo(enumType, ident, i);
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
                    return DataType.TypeQualifier.Const;
                case Token.TokenKind.VOLATILE:
                    _lexer.NextToken();
                    return DataType.TypeQualifier.Volatile;
                case Token.TokenKind.RESTRICT:
                    _lexer.NextToken();
                    return DataType.TypeQualifier.Restrict;
                case Token.TokenKind.NEAR:
                    _lexer.NextToken();
                    return DataType.TypeQualifier.Near;
                case Token.TokenKind.FAR:
                    _lexer.NextToken();
                    return DataType.TypeQualifier.Far;
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
            Declaration.TypeDeclaration typeDeclaration;
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
        private void Declarator(ref Token ident, List<CType> stack, int index) {
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
        private void DirectDeclarator(ref Token ident, List<CType> stack, int index) {
            if (IsDirectDeclarator() == false) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"宣言子が来るべき場所に{_lexer.CurrentToken().Raw}があります。");
            }
            if (_lexer.ReadTokenIf('(')) {
                stack.Add(new StubType());
                Declarator(ref ident, stack, index + 1);
                _lexer.ReadToken(')');
            } else {
                ident = _lexer.CurrentToken();
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
                int len = ReadArrayLength();
                MoreDirectDeclarator(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            } else if (_lexer.ReadTokenIf('(')) {
                // 6.7.5.3 関数宣言子（関数原型を含む）
                if (_lexer.ReadTokenIf(')')) {
                    // 識別子並びの省略
                    stack[index] = new FunctionType(null, false, stack[index]);
                    MoreDirectDeclarator(stack, index);
                } else if (IsIdentifierList()) {
                    // 識別子並び
                    var args = IdentifierList().Select(x => new FunctionType.ArgumentInfo(x.Range, x, AnsiCParser.StorageClassSpecifier.None, CType.CreateKAndRImplicitInt())).ToList();
                    _lexer.ReadToken(')');
                    stack[index] = new FunctionType(args, false, stack[index]);
                    MoreDirectDeclarator(stack, index);
                } else {
                    // 仮引数型並び
                    bool vargs = false;
                    var args = ParameterTypeList(ref vargs);
                    stack[index] = new FunctionType(args.Item3, vargs, stack[index], args.Item1, args.Item2);
                    _lexer.ReadToken(')');
                    MoreDirectDeclarator(stack, index);

                }
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
        private Tuple<Scope<TaggedType>, Scope<Declaration>, List<FunctionType.ArgumentInfo>> ParameterTypeList(ref bool vargs) {
            var prototypeTagScope = _tagScope = _tagScope.Extend();
            var prototypeIdentScope = _identScope = _identScope.Extend();

            var items = new List<FunctionType.ArgumentInfo>();
            items.Add(ParameterDeclaration());
            while (_lexer.ReadTokenIf(',')) {
                if (_lexer.ReadTokenIf(Token.TokenKind.ELLIPSIS)) {
                    vargs = true;
                    break;
                }

                items.Add(ParameterDeclaration());
            }
            _tagScope = _tagScope.Parent;
            _identScope = _identScope.Parent;
            return Tuple.Create(prototypeTagScope, prototypeIdentScope, items);
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
        private FunctionType.ArgumentInfo ParameterDeclaration() {
            var start = _lexer.CurrentToken().Start;

            StorageClassSpecifier storageClass;
            CType baseType = DeclarationSpecifiers(out storageClass);

            // 6.7.5.3 関数宣言子（関数原型を含む)
            // 制約 仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。
            if (storageClass != AnsiCParser.StorageClassSpecifier.None && storageClass != AnsiCParser.StorageClassSpecifier.Register) {
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Range, "仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。");
            }

            Token ident = null;
            if (IsDeclaratorOrAbstractDeclarator()) {
                List<CType> stack = new List<CType> { new StubType() };
                DeclaratorOrAbstractDeclarator(ref ident, stack, 0);
                baseType = CType.Resolve(baseType, stack);
            }
            if (CType.CheckContainOldStyleArgument(baseType)) {
                throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Range, "関数型中に型の無い仮引数名があります");
            }
            var end = _lexer.CurrentToken().Start;

            // 関数型を関数ポインタ型に変える
            if (baseType.IsFunctionType()) {
                baseType = CType.CreatePointer(baseType);
            }
            return new FunctionType.ArgumentInfo(new LocationRange(start, end), ident, storageClass, baseType);

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
        private void DeclaratorOrAbstractDeclarator(ref Token ident, List<CType> stack, int index) {
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
        private void DirectDeclaratorOrDirectAbstractDeclarator(ref Token ident, List<CType> stack, int index) {
            if (IsIdentifier(true)) {
                // 識別子
                ident = Identifier(true);
                MoreDdOrDad(stack, index);
            } else if (_lexer.ReadTokenIf('(')) {
                if (_lexer.PeekToken(')')) {
                    // 識別子並びの省略
                    stack[index] = new FunctionType(null, false, stack[index]);
                } else if (IsParameterTypeList()) {
                    // 仮引数型並び
                    bool vargs = false;
                    var args = ParameterTypeList(ref vargs);
                    stack[index] = new FunctionType(args.Item3, vargs, stack[index], args.Item1, args.Item2);
                } else {
                    // 直接宣言子 中の '(' 宣言子 ')'  もしくは 直接抽象宣言子 中の '(' 抽象宣言子 ')'
                    stack.Add(new StubType());
                    DeclaratorOrAbstractDeclarator(ref ident, stack, index + 1);
                }
                _lexer.ReadToken(')');
                MoreDdOrDad(stack, index);
            } else if (_lexer.ReadTokenIf('[')) {
                int len = ReadArrayLength();
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
                    stack[index] = new FunctionType(null, false, stack[index]);
                } else if (IsParameterTypeList()) {
                    // 仮引数型並び
                    bool vargs = false;
                    var args = ParameterTypeList(ref vargs);
                    stack[index] = new FunctionType(args.Item3, vargs, stack[index], args.Item1, args.Item2);
                } else {
                    // 識別子並び
                    var args = IdentifierList().Select(x =>
                        new FunctionType.ArgumentInfo(x.Range, x, AnsiCParser.StorageClassSpecifier.None, CType.CreateKAndRImplicitInt())
                    ).ToList();
                    stack[index] = new FunctionType(args, false, stack[index]);
                }
                _lexer.ReadToken(')');
                MoreDdOrDad(stack, index);
            } else if (_lexer.ReadTokenIf('[')) {
                int len = -1;
                if (_lexer.PeekToken(']') == false) {
                    var expr = ConstantExpression();
                    var ret = ExpressionEvaluator.Eval(expr);
                    var size = (int?)(ret as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)?.Value;
                    if (size.HasValue == false || size < 0) {
                        throw new CompilerException.SpecificationErrorException(_lexer.CurrentToken().Range, "配列の要素数には０以上の整数を指定してください。");
                    }
                    len = size.Value;
                }
                _lexer.ReadToken(']');
                MoreDdOrDad(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            }
        }

        /// <summary>
        /// 6.7.5 宣言子(識別子並びとなりうるか)
        /// </summary>
        /// <returns></returns>
        private bool IsIdentifierList() {
            return IsIdentifier(false);
        }

        /// <summary>
        /// 6.7.5 宣言子(識別子並び)
        /// </summary>
        /// <returns></returns>
        private List<Token> IdentifierList() {
            var items = new List<Token>();
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
            TypeQualifier typeQualifier = DataType.TypeQualifier.None;
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
                List<CType> stack = new List<CType> { new StubType() };
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
                    stack.Add(new StubType());
                    AbstractDeclarator(stack, index + 1);
                } else if (_lexer.PeekToken(')') == false) {
                    // ANSI形式
                    bool vargs = false;
                    var args = ParameterTypeList(ref vargs);
                    stack[index] = new FunctionType(args.Item3, vargs, stack[index], args.Item1, args.Item2);
                } else {
                    // K&R形式もしくは引数省略されたANSI形式（いわゆる曖昧な宣言）
                    stack[index] = new FunctionType(null, false, stack[index]);
                }
                _lexer.ReadToken(')');
                MoreDirectAbstractDeclarator(stack, index);
            } else {
                _lexer.ReadToken('[');
                int len = ReadArrayLength();
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
                int len = ReadArrayLength();
                MoreDirectAbstractDeclarator(stack, index);
                stack[index] = CType.CreateArray(len, stack[index]);
            } else if (_lexer.ReadTokenIf('(')) {
                if (_lexer.PeekToken(')') == false) {
                    bool vargs = false;
                    var args = ParameterTypeList(ref vargs);
                    stack[index] = new FunctionType(args.Item3, vargs, stack[index], args.Item1, args.Item2);
                } else {
                    stack[index] = new FunctionType(null, false, stack[index]);
                }
                _lexer.ReadToken(')');
                MoreDirectAbstractDeclarator(stack, index);
            }
        }

        /// <summary>
        /// 6.7.7 型定義(型定義名)
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private bool IsTypedefType(string v) {
            Declaration.TypeDeclaration typeDeclaration;
            return _identScope.TryGetValue(v, out typeDeclaration);
        }

        /// <summary>
        /// 6.7.8 初期化
        /// </summary>
        /// <returns></returns>
        private Initializer Initializer(CType type, bool isLocalVariableInit) {
            var init = InitializerItem();
            return InitializerChecker.CheckInitializer(type, init, isLocalVariableInit);
        }

        /// <summary>
        /// 6.7.8 初期化(初期化子)
        /// </summary>
        /// <returns></returns>
        private Initializer InitializerItem() {
            Token startTok;
            if (_lexer.ReadTokenIf(out startTok, '{')) {
                List<Initializer> ret;
                if (_lexer.PeekToken('}') == false) {
                    ret = InitializerList();
                } else {
                    ret = new List<Initializer>();
                }
                var endToken = _lexer.ReadToken('}');
                return new Initializer.ComplexInitializer(new LocationRange(startTok.Start, endToken.End), ret);
            } else {
                var ret = AssignmentExpression();
                return new Initializer.SimpleInitializer(ret.LocationRange, ret);
            }
        }

        /// <summary>
        /// 6.7.8 初期化(初期化子並び)
        /// </summary>
        /// <returns></returns>
        private List<Initializer> InitializerList() {
            var ret = new List<Initializer>();
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
            }

            return _lexer.CurrentToken().Kind == Token.TokenKind.IDENTIFIER && !IsTypedefType(_lexer.CurrentToken().Raw);
        }

        private Token Identifier(bool includeTypeName) {
            if (IsIdentifier(includeTypeName) == false) {
                throw new Exception();
            }
            var ret = _lexer.CurrentToken();
            _lexer.NextToken();
            return ret;
        }

        /// <summary>
        /// 6.8 文及びブロック
        /// </summary>
        /// <returns></returns>
        private Statement Statement() {
            if ((IsIdentifier(true) && _lexer.PeekNextToken(':')) || _lexer.PeekToken(Token.TokenKind.CASE, Token.TokenKind.DEFAULT)) {
                return LabeledStatement();
            }

            if (_lexer.PeekToken('{')) {
                return CompoundStatement();
            }

            if (_lexer.PeekToken(Token.TokenKind.IF, Token.TokenKind.SWITCH)) {
                return SelectionStatement();
            }

            if (_lexer.PeekToken(Token.TokenKind.WHILE, Token.TokenKind.DO, Token.TokenKind.FOR)) {
                return IterationStatement();
            }

            if (_lexer.PeekToken(Token.TokenKind.GOTO, Token.TokenKind.CONTINUE, Token.TokenKind.BREAK, Token.TokenKind.RETURN)) {
                return JumpStatement();
            }

            if (_lexer.PeekToken(Token.TokenKind.__ASM__)) {
                return GnuAsmStatement();
            }

            return ExpressionStatement();
        }

        /// <summary>
        /// 6.8.1 ラベル付き文
        /// </summary>
        /// <returns></returns>
        private Statement LabeledStatement() {
            Token tok;
            if (_lexer.ReadTokenIf(out tok, Token.TokenKind.CASE)) {
                if (_switchScope.Any() == false) {
                    throw new CompilerException.SpecificationErrorException(tok.Range, "caseラベルがswitch文外にあります。");
                }
                var expr = ConstantExpression();
                var value = ExpressionEvaluator.Eval(expr);
                if (value.Type.IsIntegerType() == false) {
                    throw new CompilerException.SpecificationErrorException(expr.LocationRange, "caseラベルの値が整数定数値ではありません。");
                }
                long v;
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
                var caseStatement = new Statement.CaseStatement(new LocationRange(tok.Start, stmt.LocationRange.End), expr, v, stmt);
                _switchScope.Peek().AddCaseStatement(caseStatement);
                return caseStatement;
            }

            if (_lexer.ReadTokenIf(out tok, Token.TokenKind.DEFAULT)) {
                if (_switchScope.Any() == false) {
                    throw new CompilerException.SpecificationErrorException(tok.Range, "defaultラベルがswitch文外にあります。");
                }
                _lexer.ReadToken(':');
                var stmt = Statement();
                var defaultStatement = new Statement.DefaultStatement(new LocationRange(tok.Start, stmt.LocationRange.End), stmt);
                _switchScope.Peek().SetDefaultLabel(defaultStatement);
                return defaultStatement;
            } else {
                var ident = Identifier(true);
                _lexer.ReadToken(':');
                var stmt = Statement();
                LabelScopeValue value;
                if (_labelScope.TryGetValue(ident.Raw, out value) == false) {
                    // ラベルの前方参照なので仮登録する。
                    value = new LabelScopeValue();
                    _labelScope.Add(ident.Raw, value);
                } else if (value.Declaration != null) {
                    // 既に宣言済みなのでエラー
                    throw new CompilerException.SpecificationErrorException(ident.Range, $"ラベル {ident.Raw} はすでに {value.Declaration.LocationRange} で宣言されています。");
                }
                var labelStmt = new Statement.GenericLabeledStatement(new LocationRange(ident.Start, stmt.LocationRange.End), ident.Raw, stmt);
                value.SetDeclaration(labelStmt);
                return labelStmt;
            }
        }

        /// <summary>
        /// 6.8.2 複合文
        /// </summary>
        /// <returns></returns>
        private Statement CompoundStatement(bool skipCreateNewScope = false, string funcName = null) {
            if (skipCreateNewScope == false) {
                _tagScope = _tagScope.Extend();
                _identScope = _identScope.Extend();
            }
            var start = _lexer.ReadToken('{').Start;

            if (_mode == LanguageMode.C89) {
                var decls = new List<Ast/*Declaration*/>();

                _insertImplicitDeclarationOperatorStack.Push(decls);

                while (IsDeclaration()) {
                    var d = Declaration();
                    if (d != null) {
                        decls.AddRange(d);
                    }
                }
                var stmts = new List<Statement>();
                while (_lexer.PeekToken('}') == false) {
                    stmts.Add(Statement());
                }
                var end = _lexer.ReadToken('}').End;

                _insertImplicitDeclarationOperatorStack.Pop();

                var stmt = new Statement.CompoundStatementC89(new LocationRange(start, end), decls.Cast<Declaration>().ToList(), stmts, _tagScope, _identScope);
                if (skipCreateNewScope == false) {
                    _identScope = _identScope.Parent;
                    _tagScope = _tagScope.Parent;
                }
                return stmt;
            } else {
                var declsOrStmts = new List<Ast>();

                _insertImplicitDeclarationOperatorStack.Push(declsOrStmts);

                if (funcName != null) {
                    // C99 __func__ の対応（後付なので無理やりここに入れているがエレガントさの欠片もない！）
                    var tok = new Token(Token.TokenKind.IDENTIFIER, LocationRange.Builtin.Start, LocationRange.Builtin.End, "__func__");
                    var tyConstStr = new TypeQualifierType(new PointerType(new TypeQualifierType(CType.CreateChar(), DataType.TypeQualifier.Const)), DataType.TypeQualifier.Const);
                    var varDecl = new Declaration.VariableDeclaration(LocationRange.Builtin, "__func__", tyConstStr, AnsiCParser.StorageClassSpecifier.Static);
                    varDecl.Init = new Initializer.SimpleAssignInitializer(LocationRange.Builtin, tyConstStr, new SyntaxTree.Expression.PrimaryExpression.StringExpression(LocationRange.Empty, allocStringLabel(), new List<string> { "\"" + funcName + "\"" }));
                    _identScope.Add("__func__", varDecl);
                    declsOrStmts.Add(varDecl);
                    varDecl.LinkageObject = _linkageObjectTable.RegistLinkageObject(tok, LinkageKind.NoLinkage, varDecl, true);

                }

                while (_lexer.PeekToken('}') == false) {
                    if (IsDeclaration()) {
                        var d = Declaration();
                        if (d != null) {
                            declsOrStmts.AddRange(d);
                        }
                    } else {
                        declsOrStmts.Add(Statement());
                    }
                }
                var end = _lexer.ReadToken('}').End;

                _insertImplicitDeclarationOperatorStack.Pop();

                var stmt = new Statement.CompoundStatementC99(new LocationRange(start, end), declsOrStmts, _tagScope, _identScope);
                if (skipCreateNewScope == false) {
                    _identScope = _identScope.Parent;
                    _tagScope = _tagScope.Parent;
                }
                return stmt;

            }


        }

        /// <summary>
        /// 6.8.3 式文及び空文
        /// </summary>
        /// <returns></returns>
        private Statement ExpressionStatement() {
            Statement ret;
            Token tok;
            if (_lexer.PeekToken(out tok, ';')) {
                ret = new Statement.EmptyStatement(tok.Range);
            } else {
                var expr = Expression();
                ret = new Statement.ExpressionStatement(expr.LocationRange, expr);
            }
            _lexer.ReadToken(';');
            return ret;
        }

        private Statement C99StatementWrapping(Func<Statement> func) {
            _tagScope = _tagScope.Extend();
            _identScope = _identScope.Extend();
            var declsOrStmts = new List<Ast>();
            var start = _lexer.CurrentToken().Start;
            _insertImplicitDeclarationOperatorStack.Push(declsOrStmts);

            declsOrStmts.Add(func());
            var end = _lexer.CurrentToken().Start;

            _insertImplicitDeclarationOperatorStack.Pop();
            var stmt = new Statement.CompoundStatementC99(new LocationRange(start, end), declsOrStmts, _tagScope, _identScope);
            _identScope = _identScope.Parent;
            _tagScope = _tagScope.Parent;

            return stmt;

        }

        /// <summary>
        /// 6.8.4 選択文
        /// </summary>
        /// <returns></returns>
        private Statement SelectionStatement() {
            if (_mode == LanguageMode.C99) {
                return C99StatementWrapping(SelectionStatementBody);
            } else {
                return SelectionStatementBody();
            }
        }

        private Statement SelectionStatementBody() {
            Token start;
            if (_lexer.ReadTokenIf(out start, Token.TokenKind.IF)) {
                _lexer.ReadToken('(');
                var cond = Expression();
                _lexer.ReadToken(')');
                var thenStmt = Statement();
                Statement elseStmt = null;
                if (_lexer.ReadTokenIf(Token.TokenKind.ELSE)) {
                    elseStmt = Statement();
                }
                return new Statement.IfStatement(new LocationRange(start.Start, (elseStmt ?? thenStmt).LocationRange.End), cond, thenStmt, elseStmt);
            }
            if (_lexer.ReadTokenIf(out start, Token.TokenKind.SWITCH)) {
                _lexer.ReadToken('(');
                var cond = Expression();
                _lexer.ReadToken(')');
                var ss = new Statement.SwitchStatement(start.Range, cond);
                _breakScope.Push(ss);
                _switchScope.Push(ss);
                ss.Stmt = Statement();
                _switchScope.Pop();
                _breakScope.Pop();
                var end = _lexer.CurrentToken().End;
                ss.LocationRange = new LocationRange(start.Start, ss.LocationRange.End);
                return ss;
            }
            throw new CompilerException.InternalErrorException(_lexer.CurrentToken().Range, $"選択文は if, switch のいずれかで始まりますが、 { _lexer.CurrentToken().Raw } はそのいずれでもありません。（本処理系の実装に誤りがあると思います。）");
        }

        /// <summary>
        /// 6.8.5 繰返し文
        /// </summary>
        /// <returns></returns>
        private Statement IterationStatement() {
            if (_mode == LanguageMode.C99) {
                return C99StatementWrapping(IterationStatementBody);
            } else {
                return IterationStatementBody();
            }
        }

        private Statement IterationStatementBody() {
            Token start;
            if (_lexer.ReadTokenIf(out start, Token.TokenKind.WHILE)) {
                _lexer.ReadToken('(');
                var cond = Expression();
                _lexer.ReadToken(')');
                var ss = new Statement.WhileStatement(start.Range, cond);
                _breakScope.Push(ss);
                _continueScope.Push(ss);
                ss.Stmt = Statement();
                _breakScope.Pop();
                _continueScope.Pop();
                ss.LocationRange = new LocationRange(start.Range.Start, ss.Stmt.LocationRange.End);
                return ss;
            }
            if (_lexer.ReadTokenIf(out start, Token.TokenKind.DO)) {
                var ss = new Statement.DoWhileStatement(start.Range);
                _breakScope.Push(ss);
                _continueScope.Push(ss);
                ss.Stmt = Statement();
                _breakScope.Pop();
                _continueScope.Pop();
                _lexer.ReadToken(Token.TokenKind.WHILE);
                _lexer.ReadToken('(');
                ss.Cond = Specification.TypeConvert(CType.CreateBool(), Expression());
                var end = _lexer.ReadToken(')');
                _lexer.ReadToken(';');
                ss.LocationRange = new LocationRange(start, end);
                return ss;
            }
            if (_lexer.ReadTokenIf(out start, Token.TokenKind.FOR)) {
                _lexer.ReadToken('(');
                Expression init = null;
                if (_mode == LanguageMode.C99 && IsDeclaration()) {
                    _insertImplicitDeclarationOperatorStack.Peek().AddRange(Declaration());
                } else {
                    init = _lexer.PeekToken(';') ? null : Expression();
                    _lexer.ReadToken(';');
                }
                var cond = _lexer.PeekToken(';') ? null : Expression();
                _lexer.ReadToken(';');
                var update = _lexer.PeekToken(')') ? null : Expression();
                _lexer.ReadToken(')');
                var ss = new Statement.ForStatement(start.Range, init, cond, update);
                _breakScope.Push(ss);
                _continueScope.Push(ss);
                ss.Stmt = Statement();
                _breakScope.Pop();
                _continueScope.Pop();
                ss.LocationRange = new LocationRange(start.Range.Start, ss.Stmt.LocationRange.End);
                return ss;
            }
            throw new CompilerException.InternalErrorException(_lexer.CurrentToken().Range, $"繰返し文は while, do, for のいずれかで始まりますが、 { _lexer.CurrentToken().Raw } はそのいずれでもありません。（本処理系の実装に誤りがあると思います。）");
        }

        /// <summary>
        ///  6.8.6 分岐文
        /// </summary>
        /// <returns></returns>
        private Statement JumpStatement() {
            var start = _lexer.CurrentToken();

            if (_lexer.ReadTokenIf(Token.TokenKind.GOTO)) {
                // goto 文
                var label = Identifier(true);
                _lexer.ReadToken(';');
                LabelScopeValue value;
                if (_labelScope.TryGetValue(label.Raw, out value) == false) {
                    // ラベルの前方参照なので仮登録する。
                    value = new LabelScopeValue();
                    _labelScope.Add(label.Raw, value);
                }
                var gotoStmt = new Statement.GotoStatement(new LocationRange(start, label), label.Raw);
                value.AddReference(gotoStmt);
                return gotoStmt;
            }
            if (_lexer.ReadTokenIf(Token.TokenKind.CONTINUE)) {
                // continue 文
                _lexer.ReadToken(';');
                if (_continueScope.Any() == false || _continueScope.Peek() == null) {
                    throw new CompilerException.SyntaxErrorException(start.Range, "ループ文の外で continue 文が使われています。");
                }
                return new Statement.ContinueStatement(start.Range, _continueScope.Peek());
            }
            if (_lexer.ReadTokenIf(Token.TokenKind.BREAK)) {
                // break 文
                _lexer.ReadToken(';');
                if (_breakScope.Any() == false || _breakScope.Peek() == null) {
                    throw new CompilerException.SyntaxErrorException(start.Range, "ループ文/switch文の外で break 文が使われています。");
                }
                return new Statement.BreakStatement(start.Range, _breakScope.Peek());
            }
            if (_lexer.ReadTokenIf(Token.TokenKind.RETURN)) {
                // return 文
                var expr = _lexer.PeekToken(';') ? null : Expression();
                _lexer.ReadToken(';');

                // 現在の関数の戻り値と型チェック
                FunctionType ft;
                _currentFuncDecl.Type.IsFunctionType(out ft);
                if (ft.ResultType.IsVoidType()) {
                    if (expr != null) {
                        throw new CompilerException.SyntaxErrorException(new LocationRange(start.Start, expr.LocationRange.End), "戻り値型が void 型の関数中で、値を返すreturn文が使われています。");
                    } else {
                        return new Statement.ReturnStatement(start.Range, null);
                    }
                } else {
                    if (expr == null) {
                        throw new CompilerException.SyntaxErrorException(start.Range, "戻り値型が 非void 型の関数中で、値を返さないreturn文が使われています。");
                    }
                    if (CType.IsEqual(ft.ResultType, expr.Type) == false) {
                        expr = Specification.TypeConvert(ft.ResultType, expr);
                    }
                    return new Statement.ReturnStatement(new LocationRange(start.Start, expr.LocationRange.End), expr);
                }
            }
            throw new CompilerException.InternalErrorException(_lexer.CurrentToken().Range, $"分岐文は goto, continue, break, return のいずれかで始まりますが、 { start.Raw } はそのいずれでもありません。（本処理系の実装に誤りがあると思います。）");
        }

        private void GnuAsmPart() {
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
                        throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, "GCC拡張インラインアセンブラ構文中で 丸括弧閉じ ) が使用されていますが、対応する丸括弧開き ( がありません。");
                    }
                } else if (_lexer.PeekToken(']')) {
                    if (parens.Peek() == '[') {
                        parens.Pop();
                    } else {
                        throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, "GCC拡張インラインアセンブラ構文中で 角括弧閉じ ] が使用されていますが、対応する角括弧開き [ がありません。");
                    }
                }
                _lexer.NextToken();
            }
        }

        /// <summary>
        /// X.X.X GCC拡張インラインアセンブラ
        /// </summary>
        /// <returns></returns>
        private Statement GnuAsmStatement() {

            var start = _lexer.CurrentToken().Start;
            Logger.Warning(start, "GCC拡張インラインアセンブラ構文には対応していません。ざっくりと読み飛ばします。");
            GnuAsmPart();
            _lexer.ReadToken(';');
            var end = _lexer.CurrentToken().End;
            return new Statement.EmptyStatement(new LocationRange(start, end));
        }

        /// <summary>
        /// 6.5 式
        /// </summary>
        /// <returns></returns>
        private Expression Expression() {
            var e = AssignmentExpression();
            var start = e;
            if (_lexer.PeekToken(',')) {
                var ce = new SyntaxTree.Expression.CommaExpression(start.LocationRange);
                ce.Expressions.Add(e);
                while (_lexer.ReadTokenIf(',')) {
                    e = AssignmentExpression();
                    ce.Expressions.Add(e);
                }
                ce.LocationRange = new LocationRange(start.LocationRange.Start, ce.LocationRange.End);
                return ce;
            }

            return e;
        }

        /// <summary>
        /// 6.5.1 一次式
        /// </summary>
        /// <returns></returns>
        private Expression PrimaryExpression() {
            var start = _lexer.CurrentToken().Start;
            if (IsIdentifier(false)) {
                var ident = Identifier(false);
                Declaration value;
                if (_identScope.TryGetValue(ident.Raw, out value) == false) {
                    Logger.Warning(ident.Start, ident.End, $"未定義の識別子 {ident.Raw} が一次式として利用されています。");
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression(ident.Range, ident.Raw);
                }
                if (value is Declaration.VariableDeclaration) {
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.VariableExpression(ident.Range, ident.Raw, value as Declaration.VariableDeclaration);
                }
                if (value is Declaration.ArgumentDeclaration) {
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.ArgumentExpression(ident.Range, ident.Raw, value as Declaration.ArgumentDeclaration);
                }
                if (value is Declaration.EnumMemberDeclaration) {
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.EnumerationConstant(ident.Range, (value as Declaration.EnumMemberDeclaration).MemberInfo);
                }
                if (value is Declaration.FunctionDeclaration) {
                    return new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression(ident.Range, ident.Raw, value as Declaration.FunctionDeclaration);
                }
                throw new CompilerException.InternalErrorException(_lexer.CurrentToken().Range, $"一次式として使える定義済み識別子は変数、列挙定数、関数のいずれかですが、 { _lexer.CurrentToken().Raw } はそのいずれでもありません。（本処理系の実装に誤りがあると思います。）");
            }
            if (IsConstant()) {
                return Constant();
            }
            if (IsStringLiteral()) {
                List<string> strings = new List<string>();
                while (IsStringLiteral()) {
                    strings.Add(StringLiteral());
                }
                var end = _lexer.CurrentToken().End;
                return new SyntaxTree.Expression.PrimaryExpression.StringExpression(new LocationRange(start, end), allocStringLabel(), strings);
            }
            if (_lexer.ReadTokenIf('(')) {
                if (_lexer.PeekToken('{')) {
                    // gcc statement expression
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"GCC拡張構文である複文式(statement expression)には対応していません。");
                    //var statements = CompoundStatement();
                    //_lexer.ReadToken(')');
                    //var end = _lexer.CurrentToken().End;
                    //return new SyntaxTree.Expression.GccStatementExpression(new LocationRange(start, end), statements, null);// todo: implement type
                }

                var e = Expression();
                _lexer.ReadToken(')');
                var end = _lexer.CurrentToken().End;
                var expr = new SyntaxTree.Expression.PrimaryExpression.EnclosedInParenthesesExpression(new LocationRange(start, end), e);
                return expr;
            }

            if (IsDeclaration()) {
                // heuristic C89/C99 detect
                if (_mode == LanguageMode.C89) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"C89において各種宣言はブロックの先頭でのみ許されます。");
                }

                //throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"ブロック中の先頭以外での各種宣言は現在未対応です。今のところC89同様ブロックの先頭で宣言してください。");
            }

            throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"一次式となる要素があるべき場所に { _lexer.CurrentToken().Raw } があります。");

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
        private Expression Constant() {
            // 6.5.1 一次式
            // 定数は，一次式とする。その型は，その形式と値によって決まる（6.4.4 で規定する。）。

            // 整数定数
            if (IsIntegerConstant()) {
                return IntegerConstant();
            }

            // 文字定数
            if (IsCharacterConstant()) {
                return CharacterConstant();
            }

            // 浮動小数定数
            if (IsFloatingConstant()) {
                return FloatingConstant();
            }

            // 列挙定数
            if (IsEnumerationConstant()) {
                return EnumerationConstant();
            }

            throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"定数があるべき場所に { _lexer.CurrentToken().Raw } があります。");
        }

        /// <summary>
        /// 6.5.2 後置演算子(後置式の前半)
        /// </summary>
        /// <returns></returns>
        private Expression PostfixExpression() {
            var expr = PrimaryExpression();
            return MorePostfixExpression(expr);
        }

        /// <summary>
        /// 6.5.2 後置演算子(後置式の後半)
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        private Expression MorePostfixExpression(Expression expr) {
            Token tok = _lexer.CurrentToken();
            switch (tok.Kind) {
                case (Token.TokenKind)'[': {
                        // 6.5.2.1 配列の添字付け
                        _lexer.NextToken();
                        var index = Expression();
                        _lexer.ReadToken(']');
                        return MorePostfixExpression(new SyntaxTree.Expression.PostfixExpression.ArraySubscriptingExpression(tok.Range, expr, index));
                    }
                case (Token.TokenKind)'(': {
                        // 6.5.2.2 関数呼出し
                        _lexer.NextToken();
                        var args = _lexer.PeekToken(')') == false ? ArgumentExpressionList() : new List<Expression>();
                        _lexer.ReadToken(')');
                        // 未定義の識別子の直後に関数呼び出し用の後置演算子 '(' がある場合、
                        if (expr is SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression) {
                            var identExpr = expr as SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.UndefinedIdentifierExpression;
                            // K&RおよびC89/90では暗黙的関数宣言 extern int 識別子(); が現在の宣言ブロックの先頭で定義されていると仮定して翻訳する
                            if (_mode == LanguageMode.C89) {
                                var decl = AddImplicitFunctionDeclaration(new Token(Token.TokenKind.IDENTIFIER, identExpr.LocationRange.Start, identExpr.LocationRange.End, identExpr.Ident), new FunctionType(null, false, CType.CreateSignedInt()));
                                expr = new SyntaxTree.Expression.PrimaryExpression.IdentifierExpression.FunctionExpression(tok.Range, identExpr.Ident, decl as Declaration.FunctionDeclaration);
                            } else {
                                throw new CompilerException.SpecificationErrorException(expr.LocationRange, $"未定義の識別子 {identExpr.Ident} を関数として用いています。");
                            }
                        }
                        return MorePostfixExpression(new SyntaxTree.Expression.PostfixExpression.FunctionCallExpression(tok.Range, expr, args));
                    }
                case (Token.TokenKind)'.': {
                        // 6.5.2.3 構造体及び共用体のメンバ
                        _lexer.NextToken();
                        var ident = Identifier(false);
                        return MorePostfixExpression(new SyntaxTree.Expression.PostfixExpression.MemberDirectAccess(tok.Range, expr, ident));
                    }
                case Token.TokenKind.PTR_OP: {
                        // 6.5.2.3 構造体及び共用体のメンバ
                        _lexer.NextToken();
                        var ident = Identifier(false);

                        // 6.3.2.1: 配列型の場合はポインタ型に変換する
                        CType baseType;
                        if (expr.Type.IsArrayType(out baseType)) {
                            expr = SyntaxTree.Expression.TypeConversionExpression.Apply(expr.LocationRange, CType.CreatePointer(baseType), expr);
                        }

                        return MorePostfixExpression(new SyntaxTree.Expression.PostfixExpression.MemberIndirectAccess(tok.Range, expr, ident));
                    }
                case Token.TokenKind.INC_OP:
                case Token.TokenKind.DEC_OP: {
                        // 6.5.2.4 後置増分及び後置減分演算子
                        _lexer.NextToken();
                        var op = SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.None;
                        switch (tok.Kind) {
                            case Token.TokenKind.INC_OP:
                                op = SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Inc;
                                break;
                            case Token.TokenKind.DEC_OP:
                                op = SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression.OperatorKind.Dec;
                                break;
                            default:
                                throw new CompilerException.InternalErrorException(tok.Range, "たぶん実装ミスです。");
                        }
                        return MorePostfixExpression(new SyntaxTree.Expression.PostfixExpression.UnaryPostfixExpression(tok.Range, op, expr));
                    }
                // 6.5.2.5 複合リテラル
                // Todo: 実装
                default: {
                        return expr;
                    }
            }
        }

        /// <summary>
        /// 6.5.2 後置演算子(実引数並び)
        /// </summary>
        /// <returns></returns>
        private List<Expression> ArgumentExpressionList() {
            var ret = new List<Expression>();
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
        private Expression ArgumentExpression() {
            var ret = AssignmentExpression();

            // 6.3.2.1: 配列型の場合はポインタ型に変換する
            CType baseType;
            if (ret.Type.IsArrayType(out baseType)) {
                ret = SyntaxTree.Expression.TypeConversionExpression.Apply(ret.LocationRange, CType.CreatePointer(baseType), ret);
            }

            return ret;
        }

        /// <summary>
        /// 6.5.3 単項演算子(単項式)
        /// </summary>
        /// <returns></returns>
        private Expression UnaryExpression() {
            Token tok = _lexer.CurrentToken();
            switch (tok.Kind) {
                case Token.TokenKind.INC_OP: {
                        _lexer.NextToken();
                        var expr = UnaryExpression();
                        return new SyntaxTree.Expression.UnaryPrefixExpression(new LocationRange(tok.Start, expr.LocationRange.End), SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Inc, expr);
                    }
                case Token.TokenKind.DEC_OP: {
                        _lexer.NextToken();
                        var expr = UnaryExpression();
                        return new SyntaxTree.Expression.UnaryPrefixExpression(new LocationRange(tok.Start, expr.LocationRange.End), SyntaxTree.Expression.UnaryPrefixExpression.OperatorKind.Dec, expr);
                    }

                case (Token.TokenKind)'&': {
                        _lexer.NextToken();
                        var expr = CastExpression();
                        return new SyntaxTree.Expression.UnaryAddressExpression(new LocationRange(tok.Start, expr.LocationRange.End), expr);
                    }
                case (Token.TokenKind)'*': {
                        _lexer.NextToken();
                        var expr = CastExpression();
                        return new SyntaxTree.Expression.UnaryReferenceExpression(new LocationRange(tok.Start, expr.LocationRange.End), expr);
                    }
                case (Token.TokenKind)'+': {
                        _lexer.NextToken();
                        var expr = CastExpression();
                        return new SyntaxTree.Expression.UnaryPlusExpression(new LocationRange(tok.Start, expr.LocationRange.End), expr);
                    }
                case (Token.TokenKind)'-': {
                        _lexer.NextToken();
                        var expr = CastExpression();
                        return new SyntaxTree.Expression.UnaryMinusExpression(new LocationRange(tok.Start, expr.LocationRange.End), expr);
                    }
                case (Token.TokenKind)'~': {
                        _lexer.NextToken();
                        var expr = CastExpression();
                        return new SyntaxTree.Expression.UnaryNegateExpression(new LocationRange(tok.Start, expr.LocationRange.End), expr);
                    }
                case (Token.TokenKind)'!': {
                        _lexer.NextToken();
                        var expr = CastExpression();
                        return new SyntaxTree.Expression.UnaryNotExpression(new LocationRange(tok.Start, expr.LocationRange.End), expr);
                    }
                case Token.TokenKind.SIZEOF: {
                        _lexer.NextToken();
                        if (_lexer.PeekToken('(')) {
                            // どちらにも'('の出現が許されるためさらに先読みを行う。
                            _lexer.SaveContext();
                            _lexer.ReadToken('(');
                            if (IsTypeName()) {
                                _lexer.DiscardSavedContext();
                                var type = TypeName();
                                var end = _lexer.ReadToken(')');
                                return new SyntaxTree.Expression.SizeofTypeExpression(new LocationRange(tok.Start, end.End), type);
                            } else {
                                // 式
                                _lexer.RestoreSavedContext();
                                var expr = UnaryExpression();
                                return new SyntaxTree.Expression.SizeofExpression(new LocationRange(tok.Start, expr.LocationRange.End), expr);
                            }
                        } else {
                            // 括弧がないので式
                            var expr = UnaryExpression();
                            return new SyntaxTree.Expression.SizeofExpression(new LocationRange(tok.Start, expr.LocationRange.End), expr);
                        }
                    }
                default:
                    return PostfixExpression();

            }
        }

        /// <summary>
        /// 6.5.4 キャスト演算子(キャスト式)
        /// </summary>
        /// <returns></returns>
        private Expression CastExpression() {
            Token tok;
            if (_lexer.PeekToken(out tok, '(')) {
                // どちらにも'('の出現が許されるためさらに先読みを行う。
                _lexer.SaveContext();
                _lexer.ReadToken('(');
                if (IsTypeName()) {
                    _lexer.DiscardSavedContext();
                    var type = TypeName();
                    _lexer.ReadToken(')');
                    var expr = CastExpression();
                    if (expr.Type.IsArrayType()) {
                        expr = Specification.ToPointerTypeExpr(expr);
                    }
                    return new SyntaxTree.Expression.CastExpression(new LocationRange(tok.Start, expr.LocationRange.End), type, expr);
                }

                _lexer.RestoreSavedContext();
                return UnaryExpression();
            }

            return UnaryExpression();
        }

        /// <summary>
        /// 6.5.5 乗除演算子(乗除式)
        /// </summary>
        /// <returns></returns>
        private Expression MultiplicitiveExpression() {
            var lhs = CastExpression();
            Token tok;
            while (_lexer.ReadTokenIf(out tok, '*', '/', '%')) {
                SyntaxTree.Expression.MultiplicativeExpression.OperatorKind op;
                switch (tok.Kind) {
                    case (Token.TokenKind)'*':
                        op = SyntaxTree.Expression.MultiplicativeExpression.OperatorKind.Mul;
                        break;
                    case (Token.TokenKind)'/':
                        op = SyntaxTree.Expression.MultiplicativeExpression.OperatorKind.Div;
                        break;
                    case (Token.TokenKind)'%':
                        op = SyntaxTree.Expression.MultiplicativeExpression.OperatorKind.Mod;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(tok.Range, "");
                }
                var rhs = CastExpression();
                lhs = new SyntaxTree.Expression.MultiplicativeExpression(op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.6 加減演算子(加減式)
        /// </summary>
        /// <returns></returns>
        private Expression AdditiveExpression() {
            var lhs = MultiplicitiveExpression();
            Token tok;
            while (_lexer.ReadTokenIf(out tok, '+', '-')) {
                SyntaxTree.Expression.AdditiveExpression.OperatorKind op;
                switch (tok.Kind) {
                    case (Token.TokenKind)'+':
                        op = SyntaxTree.Expression.AdditiveExpression.OperatorKind.Add;
                        break;
                    case (Token.TokenKind)'-':
                        op = SyntaxTree.Expression.AdditiveExpression.OperatorKind.Sub;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(tok.Range, "");
                }
                var rhs = MultiplicitiveExpression();
                lhs = new SyntaxTree.Expression.AdditiveExpression(new LocationRange(lhs.LocationRange.Start, rhs.LocationRange.End), op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.7 ビット単位のシフト演算子(シフト式)
        /// </summary>
        /// <returns></returns>
        private Expression ShiftExpression() {
            var lhs = AdditiveExpression();
            Token tok;
            while (_lexer.ReadTokenIf(out tok, Token.TokenKind.LEFT_OP, Token.TokenKind.RIGHT_OP)) {
                SyntaxTree.Expression.ShiftExpression.OperatorKind op;
                switch (tok.Kind) {
                    case Token.TokenKind.LEFT_OP:
                        op = SyntaxTree.Expression.ShiftExpression.OperatorKind.Left;
                        break;
                    case Token.TokenKind.RIGHT_OP:
                        op = SyntaxTree.Expression.ShiftExpression.OperatorKind.Right;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(tok.Range, "");
                }

                var rhs = AdditiveExpression();
                lhs = new SyntaxTree.Expression.ShiftExpression(new LocationRange(lhs.LocationRange.Start, rhs.LocationRange.End), op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.8 関係演算子(関係式)
        /// </summary>
        /// <returns></returns>
        private Expression RelationalExpression() {
            var lhs = ShiftExpression();
            Token tok;
            while (_lexer.ReadTokenIf(out tok, (Token.TokenKind)'<', (Token.TokenKind)'>', Token.TokenKind.LE_OP, Token.TokenKind.GE_OP)) {
                SyntaxTree.Expression.RelationalExpression.OperatorKind op;
                switch (tok.Kind) {
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
                        throw new CompilerException.InternalErrorException(tok.Range, "");
                }
                var rhs = ShiftExpression();
                lhs = new SyntaxTree.Expression.RelationalExpression(new LocationRange(lhs.LocationRange.Start, rhs.LocationRange.End), op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.9 等価演算子(等価式)
        /// </summary>
        /// <returns></returns>
        private Expression EqualityExpression() {
            var lhs = RelationalExpression();
            Token tok;
            while (_lexer.ReadTokenIf(out tok, Token.TokenKind.EQ_OP, Token.TokenKind.NE_OP)) {
                SyntaxTree.Expression.EqualityExpression.OperatorKind op;
                switch (tok.Kind) {
                    case Token.TokenKind.EQ_OP:
                        op = SyntaxTree.Expression.EqualityExpression.OperatorKind.Equal;
                        break;
                    case Token.TokenKind.NE_OP:
                        op = SyntaxTree.Expression.EqualityExpression.OperatorKind.NotEqual;
                        break;
                    default:
                        throw new CompilerException.InternalErrorException(tok.Range, "");
                }
                var rhs = RelationalExpression();
                lhs = new SyntaxTree.Expression.EqualityExpression(new LocationRange(lhs.LocationRange.Start, rhs.LocationRange.End), op, lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.10 ビット単位の AND 演算子(AND式)
        /// </summary>
        /// <returns></returns>
        private Expression AndExpression() {
            var lhs = EqualityExpression();
            while (_lexer.ReadTokenIf('&')) {
                var rhs = EqualityExpression();
                lhs = new SyntaxTree.Expression.BitExpression.AndExpression(new LocationRange(lhs.LocationRange.Start, rhs.LocationRange.End), lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.11 ビット単位の排他 OR 演算子(排他OR式)
        /// </summary>
        /// <returns></returns>
        private Expression ExclusiveOrExpression() {
            var lhs = AndExpression();
            while (_lexer.ReadTokenIf('^')) {
                var rhs = AndExpression();
                lhs = new SyntaxTree.Expression.BitExpression.ExclusiveOrExpression(new LocationRange(lhs.LocationRange.Start, rhs.LocationRange.End), lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.12 ビット単位の OR 演算子(OR式)
        /// </summary>
        /// <returns></returns>
        private Expression InclusiveOrExpression() {
            var lhs = ExclusiveOrExpression();
            while (_lexer.ReadTokenIf('|')) {
                var rhs = ExclusiveOrExpression();
                lhs = new SyntaxTree.Expression.BitExpression.InclusiveOrExpression(new LocationRange(lhs.LocationRange.Start, rhs.LocationRange.End), lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.13 論理 AND 演算子(論理AND式)
        /// </summary>
        /// <returns></returns>
        private Expression LogicalAndExpression() {
            var lhs = InclusiveOrExpression();
            while (_lexer.ReadTokenIf(Token.TokenKind.AND_OP)) {
                var rhs = InclusiveOrExpression();
                lhs = new SyntaxTree.Expression.LogicalAndExpression(new LocationRange(lhs.LocationRange.Start, rhs.LocationRange.End), lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.14 論理 OR 演算子(論理OR式)
        /// </summary>
        /// <returns></returns>
        private Expression LogicalOrExpression() {
            var lhs = LogicalAndExpression();
            while (_lexer.ReadTokenIf(Token.TokenKind.OR_OP)) {
                var rhs = LogicalAndExpression();
                lhs = new SyntaxTree.Expression.LogicalOrExpression(new LocationRange(lhs.LocationRange.Start, rhs.LocationRange.End), lhs, rhs);
            }
            return lhs;
        }

        /// <summary>
        /// 6.5.15 条件演算子(条件式)
        /// </summary>
        /// <returns></returns>
        private Expression ConditionalExpression() {
            var condExpr = LogicalOrExpression();
            if (_lexer.ReadTokenIf('?')) {
                var thenExpr = Expression();
                _lexer.ReadToken(':');
                var elseExpr = ConditionalExpression();
                return new SyntaxTree.Expression.ConditionalExpression(
                    new LocationRange(condExpr.LocationRange.Start, elseExpr.LocationRange.End), condExpr, thenExpr, elseExpr);
            }

            return condExpr;
        }

        /// <summary>
        /// 6.5.16 代入演算子(代入式)
        /// </summary>
        /// <returns></returns>
        private Expression AssignmentExpression() {
            var lhs = ConditionalExpression();
            if (IsAssignmentOperator()) {
                var op = AssignmentOperator();
                var rhs = AssignmentExpression();

                switch (op.Raw) {
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
                        throw new CompilerException.InternalErrorException(op.Start, op.End, $"未定義の代入演算子 { op.Raw } があります。");
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
        private Token AssignmentOperator() {
            if (IsAssignmentOperator() == false) {
                throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"代入演算子があるべき場所に { _lexer.CurrentToken().Raw } があります。");
            }
            var ret = _lexer.CurrentToken();
            _lexer.NextToken();
            return ret;
        }

        /// <summary>
        /// 6.6 定数式
        /// </summary>
        /// <returns></returns>
        private Expression ConstantExpression() {
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
            ExternalDeclaration = StorageClassSpecifier | TypeSpecifier | TypeQualifier | FunctionSpecifier
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
            var start = _lexer.CurrentToken().Start;
            TypeSpecifier typeSpecifier = AnsiCParser.TypeSpecifier.None;
            TypeQualifier typeQualifier = DataType.TypeQualifier.None;
            while (IsDeclarationSpecifierPart(type, typeSpecifier, flags)) {
                ReadDeclarationSpecifierPart(ref type, ref storageClass, ref typeSpecifier, ref typeQualifier, ref functionSpecifier, flags);
                n++;
            }
            var end = _lexer.CurrentToken().End;

            // 構築

            if (type != null) {
                // 型指定子部に構造体共用体指定子、列挙型指定子、型定義名が出現する場合
                if (typeSpecifier != AnsiCParser.TypeSpecifier.None) {
                    // 6.7.2 型指定子「型指定子の並びは，次に示すもののいずれか一つでなければならない。」中で構造体共用体指定子、列挙型指定子、型定義名はそれ単体のみで使うことが規定されているため、
                    // 構造体共用体指定子、列挙型指定子、型定義名のいずれかとそれら以外の型指定子が組み合わせられている場合はエラーとする。
                    // なお、構造体共用体指定子、列挙型指定子、型定義名が複数回利用されている場合は SpecifierQualifier 内でエラーとなる。
                    // （歴史的な話：K&R では typedef は 別名(alias)扱いだったため、typedef int INT; unsigned INT x; は妥当だった）
                    throw new CompilerException.SpecificationErrorException(start, end, "型指定子・型修飾子並び中で構造体共用体指定子、列挙型指定子、型定義名のいずれかと、それら以外の型指定子が組み合わせられている。");
                }
            } else {
                // 型指定子部に構造体共用体指定子、列挙型指定子、型定義名が出現しない場合
                if (typeSpecifier == AnsiCParser.TypeSpecifier.None) {
                    // 歴史的な話：K&R では 宣言指定子を省略すると int 扱い
                    if (_mode == LanguageMode.C89) {
                        // C90では互換性の観点からK&R動作が使える。
                        Logger.Warning(start, end, "型が省略された宣言は、暗黙的に signed int 型と見なします。");
                        type = CType.CreateSignedInt();
                    } else {
                        // C99以降では
                        // 6.7.2 それぞれの宣言の宣言指定子列の中で，又はそれぞれの構造体宣言及び型名の型指定子型修飾子並びの中で，少なくとも一つの型指定子を指定しなければならない。
                        // とあるため、宣言指定子を一つも指定しないことは許されない。
                        throw new CompilerException.SpecificationErrorException(start, end, "C99以降ではそれぞれの宣言の宣言指定子列の中で，又はそれぞれの構造体宣言及び型名の型指定子型修飾子並びの中で，少なくとも一つの型指定子を指定しなければならない。");
                    }
                } else {
                    type = BasicType.FromTypeSpecifier(typeSpecifier);
                }
            }

            // 型修飾子を適用
            type = type.WrapTypeQualifier(typeQualifier);

            return n;
        }

        private bool IsDeclarationSpecifierPart(CType type, TypeSpecifier typeSpecifier, ReadDeclarationSpecifierPartFlag flags) {
            if (IsStorageClassSpecifier()) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.StorageClassSpecifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"ここでは記憶クラス指定子 { _lexer.CurrentToken().Raw } は使えません。");
                }
                return true;
            }
            if (IsTypeSpecifier() && type == null) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeSpecifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"ここでは型指定子 { _lexer.CurrentToken().Raw } は使えません。");
                }
                return true;
            }
            if (IsStructOrUnionSpecifier() && type == null) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeSpecifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"ここでは構造体共用体指定子 { _lexer.CurrentToken().Raw } は使えません。");
                }
                return true;
            }
            if (IsEnumSpecifier() && type == null) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeSpecifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"ここでは列挙型指定子 { _lexer.CurrentToken().Raw } は使えません。");
                }
                return true;
            }
            if (IsTypedefName() && type == null && typeSpecifier == AnsiCParser.TypeSpecifier.None) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeSpecifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"ここでは型定義名 { _lexer.CurrentToken().Raw } は使えません。");
                }
                return true;
            }
            if (IsTypeQualifier()) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeQualifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"ここでは型修飾子 { _lexer.CurrentToken().Raw } は使えません。");
                }
                return true;
            }
            if (IsFunctionSpecifier()) {
                if (!flags.HasFlag(ReadDeclarationSpecifierPartFlag.FunctionSpecifier)) {
                    throw new CompilerException.SyntaxErrorException(_lexer.CurrentToken().Range, $"ここでは関数修飾子 { _lexer.CurrentToken().Raw } は使えません。");
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
                var range = _lexer.CurrentToken().Range;
                typeSpecifier = typeSpecifier.Marge(TypeSpecifier(), range);
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
                Declaration.TypeDeclaration value;
                if (_identScope.TryGetValue(_lexer.CurrentToken().Raw, out value) == false) {
                    throw new Exception();
                }
                if (type != null) {
                    throw new Exception("");
                }
                type = new TypedefType(_lexer.CurrentToken(), value.Type);
                _lexer.NextToken();
            } else if (flags.HasFlag(ReadDeclarationSpecifierPartFlag.TypeQualifier) && IsTypeQualifier()) {
                // 型修飾子
                typeQualifier = typeQualifier.Marge(TypeQualifier());
            } else if (flags.HasFlag(ReadDeclarationSpecifierPartFlag.FunctionSpecifier) && IsFunctionSpecifier()) {
                // 関数修飾子
                functionSpecifier = functionSpecifier.Marge(FunctionSpecifier());
            } else {
                throw new Exception("");
            }
        }

#endregion


#region 関数宣言部（関数定義時も含む）の解析と名前表への登録を共通化

        private Declaration.FunctionDeclaration FunctionDeclaration(Token ident, CType type, StorageClassSpecifier storageClass, FunctionSpecifier functionSpecifier, ScopeKind scope, bool isDefine) {
            if (scope == ScopeKind.BlockScope && isDefine) {
                throw new CompilerException.InternalErrorException(ident.Start, ident.End, "ブロックスコープ内で関数定義をしようとしている。（おそらく本処理系の実装ミス）");
            }

            if (scope == ScopeKind.BlockScope && !isDefine) {
                // ブロックスコープ中での関数宣言の場合

                // 6.7.1 記憶域クラス指定子
                // 関数の識別子がブロック有効範囲で宣言される場合，extern 以外の明示的な記憶域クラス指定子をもってはならない。

                if (storageClass != AnsiCParser.StorageClassSpecifier.None
                    && storageClass != AnsiCParser.StorageClassSpecifier.Extern) {
                    throw new CompilerException.SpecificationErrorException(ident.Range, "関数の識別子がブロック有効範囲で宣言される場合，extern 以外の明示的な記憶域クラス指定子をもってはならない。");
                }
            }

            // 記憶域クラス指定からリンケージを求める(関数の場合は、外部結合もしくは内部結合のどれかとなり、無結合はない)
            LinkageKind linkage = ResolveLinkage(ident, type, storageClass, scope, _identScope, false);
            Debug.Assert(linkage == LinkageKind.ExternalLinkage || linkage == LinkageKind.InternalLinkage);

            // その識別子の以前の宣言が可視であるか？
            Declaration iv;
            bool isCurrent;
            if (_identScope.TryGetValue(ident.Raw, out iv, out isCurrent)) {
                // 以前の宣言が可視である

                // 6.7 宣言
                // 識別子が無結合である場合，その識別子の宣言（宣言子又は型指定子の中の）が同じ有効範囲及び同じ名前空間の中で，二つ以上あってはならない。
                // （捕捉：「識別子が無結合である場合」は以前の宣言の識別子にも適用される。つまり、一度でも無結合であると宣言された識別子については再宣言できない。）
                // 参考文献: https://stackoverflow.com/questions/7239911/block-scope-linkage-c-standard
                if ((linkage == LinkageKind.NoLinkage || iv.LinkageObject.Linkage == LinkageKind.NoLinkage) && isCurrent) {
                    throw new CompilerException.SpecificationErrorException(ident.Range,
                        $"{iv.LocationRange.ToString()} で 無結合 として宣言された識別子 {ident.Raw} が同じ有効範囲及び同じ名前空間の中で再度宣言されています。" +
                        "識別子が無結合である場合，その識別子の宣言（宣言子又は型指定子の中の）が同じ有効範囲及び同じ名前空間の中で，二つ以上あってはなりません。");
                }

                // 翻訳単位の中で同じ識別子が内部結合と外部結合の両方で現れた場合，その動作は未定義とする。
                if ((iv.LinkageObject.Linkage == LinkageKind.InternalLinkage && linkage == LinkageKind.ExternalLinkage)
                    || (iv.LinkageObject.Linkage == LinkageKind.ExternalLinkage && linkage == LinkageKind.InternalLinkage)) {
                    throw new CompilerException.SpecificationErrorException(ident.Range, $"{iv.LocationRange.ToString()} で {(iv.LinkageObject.Linkage == LinkageKind.InternalLinkage ? "内部結合" : "外部結合")} として宣言された識別子 {ident.Raw} が {(linkage == LinkageKind.InternalLinkage ? "内部結合" : "外部結合")} として再宣言されています。" +
                                                                                         $"翻訳単位の中で同じ識別子が内部結合と外部結合の両方で現れた場合の動作は未定義です。");
                }

                var prevType = type;
                // 型適合のチェック
                if (Specification.IsCompatible(iv.Type.Unwrap(), type.Unwrap()) == false) {
                    throw new CompilerException.TypeMissmatchError(ident.Start, ident.End, $"{iv.LocationRange.ToString()} で 既に宣言されている {ident.Raw} と型が適合しないため再宣言できません。");
                }

                // 合成型を生成
                type = CType.CompositeType(iv.Type.Unwrap(), type.Unwrap());
                Debug.Assert(type != null);

                if (scope == ScopeKind.FileScope && isDefine) {
                    var prevFd = (iv as Declaration.FunctionDeclaration);
                    if (prevFd.Body != null) {
                        throw new CompilerException.SpecificationErrorException(ident.Range, $"{iv.LocationRange.ToString()} で すでに本体を持っている関数 {ident.Raw} を再定義しています。");
                    }
                    var compoundFt = type.Unwrap() as FunctionType;
                    var ft = (prevType.Unwrap() as FunctionType);
                    for (var i = 0; i < ft.Arguments.Length; i++) {
                        compoundFt.Arguments[i].Ident = ft.Arguments[i].Ident;
                    }
                }
            }

            var funcDelc = new Declaration.FunctionDeclaration(ident.Range, ident.Raw, type, storageClass, functionSpecifier);

            // 結合スコープにオブジェクトを追加
            funcDelc.LinkageObject = _linkageObjectTable.RegistLinkageObject(ident, linkage, funcDelc, (scope == ScopeKind.FileScope && isDefine));
            _identScope.Add(ident.Raw, funcDelc);

            return funcDelc;

        }
#endregion

        private Declaration.TypeDeclaration TypedefDeclaration(Token ident, CType type) {
            // 型宣言名

            // 6.7 宣言
            // 識別子が無結合である場合，その識別子の宣言（宣言子又は型指定子の中の）が同じ有効範囲及び同じ名前空間の中で，二つ以上あってはならない。

            // 型名は無結合なので同じ有効範囲及び同じ名前空間の中で，二つ以上あってはならない。

            Declaration iv;
            bool isCurrent;
            if (_identScope.TryGetValue(ident.Raw, out iv, out isCurrent)) {

                if (isCurrent) {
                    // 型名は無結合であるため、再宣言できない
                    if (iv is Declaration.TypeDeclaration) {
                        throw new CompilerException.SpecificationErrorException(ident.Range, $"{ident.Raw} は既に型宣言名として宣言されています。（型の再定義はC11以降の機能。）");
                    }

                    throw new CompilerException.SpecificationErrorException(ident.Range, $"{ident.Raw} は宣言済みです。");
                }
            }
            var typeDecl = new Declaration.TypeDeclaration(ident.Range, ident.Raw, type);
            _identScope.Add(ident.Raw, typeDecl);
            return typeDecl;
        }


        private enum ScopeKind {
            BlockScope,
            FileScope
        }

        private Declaration.VariableDeclaration VariableDeclaration(Token ident, CType type, StorageClassSpecifier storageClass, ScopeKind scope, bool hasInitializer) {

            // 記憶域クラス指定からリンケージを求める
            LinkageKind linkage = ResolveLinkage(ident, type, storageClass, scope, _identScope, hasInitializer);



            // その識別子の以前の宣言が可視であるか？
            Declaration iv;
            bool isCurrent;
            var isVisiblePrevDecl = _identScope.TryGetValue(ident.Raw, out iv, out isCurrent);
            if (isVisiblePrevDecl) {
                // 以前の宣言が可視である

                // 6.7 宣言
                // 識別子が無結合である場合，その識別子の宣言（宣言子又は型指定子の中の）が同じ有効範囲及び同じ名前空間の中で，二つ以上あってはならない。
                // （捕捉：「識別子が無結合である場合」は以前の宣言の識別子にも適用される。つまり、一度でも無結合であると宣言された識別子については再宣言できない。）
                // 参考文献: https://stackoverflow.com/questions/7239911/block-scope-linkage-c-standard
                if ((linkage == LinkageKind.NoLinkage || iv.LinkageObject.Linkage == LinkageKind.NoLinkage) && isCurrent) {
                    throw new CompilerException.SpecificationErrorException(ident.Range, "識別子が無結合である場合，その識別子の宣言（宣言子又は型指定子の中の）が同じ有効範囲及び同じ名前空間の中で，二つ以上あってはならない。");
                }

                // 翻訳単位の中で同じ識別子が内部結合と外部結合の両方で現れた場合，その動作は未定義とする。
                if ((iv.LinkageObject.Linkage == LinkageKind.InternalLinkage && linkage == LinkageKind.ExternalLinkage)
                    || (iv.LinkageObject.Linkage == LinkageKind.ExternalLinkage && linkage == LinkageKind.InternalLinkage)) {
                    throw new CompilerException.SpecificationErrorException(ident.Range, $"翻訳単位の中で同じ識別子{ident.Raw}が内部結合と外部結合の両方で現れました。この場合の動作は未定義です。");
                }

                if (linkage != LinkageKind.NoLinkage) {
                    // 以前の宣言が変数定義でないならばエラー
                    if (iv is Declaration.VariableDeclaration == false) {
                        throw new CompilerException.TypeMissmatchError(ident.Start, ident.End, $"{ident.Raw}は既に変数以外として宣言されています。");
                    }

                    // 型適合のチェック
                    //if (Specification.IsCompatible(iv.Type.Unwrap(), type.Unwrap()) == false) {
                    if (Specification.IsCompatible(iv.Type, type) == false) {
                        throw new CompilerException.TypeMissmatchError(ident.Start, ident.End, $"既に宣言されている変数{ident.Raw}と型が適合しないため再宣言できません。");
                    }

                    // 合成型を生成
                    //type = CType.CompositeType(iv.Type.Unwrap(), type.Unwrap());
                    type = CType.CompositeType(iv.Type, type);
                    Debug.Assert(type != null);

                }
                // 前の変数を隠すことができるので、新しい宣言を作成

            }

            if (hasInitializer) {
                // 不完全型の配列は初期化式で完全型に書き換えられるため、複製する
                CType elementType;
                int len;
                if (type.Unwrap().IsArrayType(out elementType, out len) && len == -1) {
                    type = type.Duplicate();
                }

            }
            // 新たに変数宣言を作成
            var varDecl = new Declaration.VariableDeclaration(ident.Range, ident.Raw, type, storageClass/*, initializer*/);
            varDecl.LinkageObject = _linkageObjectTable.RegistLinkageObject(ident, linkage, varDecl, hasInitializer);
            _identScope.Add(ident.Raw, varDecl);

            // 初期化子を持つか？
            Initializer initializer = null;
            if (hasInitializer) {

                if (type.IsFunctionType()) {
                    // 関数型は変数のように初期化できない。なぜなら、それは関数宣言だから。
                    throw new CompilerException.SpecificationErrorException(ident.Range, "関数型を持つ宣言子に対して初期化子を設定しています。");
                }

                // 6.7.8 初期化
                // 識別子の宣言がブロック有効範囲をもち，かつ識別子が外部結合又は内部結合をもつ場合，その宣言にその識別子に対する初期化子があってはならない。
                if ((scope == ScopeKind.BlockScope) && (linkage == LinkageKind.InternalLinkage || linkage == LinkageKind.ExternalLinkage)) {
                    throw new CompilerException.SpecificationErrorException(ident.Range, "識別子の宣言がブロック有効範囲をもち，かつ識別子が外部結合又は内部結合をもつ場合，その宣言にその識別子に対する初期化子があってはならない。");
                }


                // 初期化子を読み取る
                // NoLinkageでBlockScopeかつ、storageclassがstaticで無い場合に限り、定数ではない初期化式が使える
                initializer = Initializer(type, scope == ScopeKind.BlockScope && linkage == LinkageKind.NoLinkage && storageClass != AnsiCParser.StorageClassSpecifier.Static);
                varDecl.Init = initializer;


            } else {
                //CType baseType;
                //int len;
                //if (type.IsArrayType(out baseType, out len) && len == -1) {
                //    // 長さの指定がない配列型はポインタ型に読み替える
                //    type = CType.CreatePointer(baseType);
                //}
            }

            // オブジェクトの識別子が無結合で宣言されている場合，オブジェクトの型は，その宣言子の終わりまで に，又は初期化宣言子の終わりまで（その宣言子が初期化子をもつとき）に，完全になっていなければならない。
            if (!isVisiblePrevDecl && linkage == LinkageKind.NoLinkage) {
                if (type.IsIncompleteType()) {
                    throw new CompilerException.TypeMissmatchError(ident.Start, ident.End, $"不完全型の変数 {ident.Raw} が使われています。");
                }
            }

            return varDecl;
        }


        /// <summary>
        /// 宣言の読み取りの共通処理
        /// </summary>
        /// <param name="scope"></param>
        /// <returns></returns>
        private List<Declaration> ReadDeclaration(ScopeKind scope) {

            // 宣言指定子列 
            CType baseType = null;
            StorageClassSpecifier storageClass = AnsiCParser.StorageClassSpecifier.None;
            FunctionSpecifier functionSpecifier = AnsiCParser.FunctionSpecifier.None;
            var start = _lexer.CurrentToken().Start;
            var currentIdentScope = _identScope;
            if (scope == ScopeKind.FileScope) {
                // ファイルスコープでの宣言
                if (ReadDeclarationSpecifiers(ref baseType, ref storageClass, ref functionSpecifier, ReadDeclarationSpecifierPartFlag.ExternalDeclaration) < 1) {
                    Logger.Warning(start, _lexer.CurrentToken().End, "記憶クラス指定子 /型指定子/型修飾子が省略されています。");
                }

                // 記憶域クラス指定子 auto 及び register が，外部宣言の宣言指定子列の中に現れてはならない。
                if (storageClass == AnsiCParser.StorageClassSpecifier.Auto || storageClass == AnsiCParser.StorageClassSpecifier.Register) {
                    throw new CompilerException.SpecificationErrorException(start, _lexer.CurrentToken().End, "記憶域クラス指定子 auto 及び register が，外部宣言の宣言指定子列の中に現れてはならない。");
                }

            } else if (scope == ScopeKind.BlockScope) {
                // ブロックスコープでの宣言
                if (ReadDeclarationSpecifiers(ref baseType, ref storageClass, ref functionSpecifier, ReadDeclarationSpecifierPartFlag.DeclarationSpecifiers) < 1) {
                    throw new CompilerException.SyntaxErrorException(start, _lexer.CurrentToken().End, "記憶クラス指定子 /型指定子/型修飾子が一つ以上指定されている必要がある。");
                }
            } else {
                throw new Exception();
            }

            // 宣言子並び
            var decls = new List<Declaration>();
            if (!IsDeclarator()) {
                // 宣言子が続いていない場合、
                // 例: int; 

                var end = _lexer.ReadToken(';').End;

                if (functionSpecifier != AnsiCParser.FunctionSpecifier.None) {
                    throw new CompilerException.SyntaxErrorException(start, end, "inlineは関数定義以外には使用できません。");
                }

                // 匿名ではないタグ付き型（構造体/共用体/列挙型）の宣言は許可するが、
                // それ以外の宣言についてはエラーを出力する
                if (!baseType.IsStructureType() && !baseType.IsUnionType() && !baseType.IsEnumeratedType()) {
                    throw new CompilerException.SpecificationErrorException(start, end, "空の宣言は使用できません。");
                }
                TaggedType.StructUnionType suType;
                if ((baseType.IsStructureType(out suType) || baseType.IsUnionType(out suType)) && suType.IsAnonymous) {
                    throw new CompilerException.SpecificationErrorException(start, end, "無名構造体/共用体が宣言されていますが、そのインスタンスを定義していません。");
                }
                if (CType.CheckContainOldStyleArgument(baseType)) {
                    throw new CompilerException.SpecificationErrorException(start, end, "関数型中に型の無い仮引数名があります。");
                }

                return decls;
            } else {
                Token ident = null;
                var stack = new List<CType> { new StubType() };
                Declarator(ref ident, stack, 0);
                var type = CType.Resolve(baseType, stack);
                var end = _lexer.CurrentToken().End;

                Token tok;
                if (_lexer.PeekToken(out tok, (Token.TokenKind)'=', (Token.TokenKind)',', (Token.TokenKind)';', Token.TokenKind.__ASM__)) {

                    // 外部オブジェクト定義 ( 関数プロトタイプ宣言にGCC拡張インラインアセンブラがくっついてくる場合もあるでここで読み飛ばす）
                    if (tok.Kind == Token.TokenKind.__ASM__) {
                        Logger.Warning(tok.Range, "GCC拡張インラインアセンブラ構文には対応していません。ざっくりと読み飛ばします。");
                        GnuAsmPart();
                    }

                    if (CType.CheckContainOldStyleArgument(type)) {
                        throw new CompilerException.SpecificationErrorException(start, end, "関数型中に型の無い仮引数名があります");
                    }

                    if (functionSpecifier != AnsiCParser.FunctionSpecifier.None) {
                        throw new CompilerException.SyntaxErrorException(start, end, "inlineは関数定義に対してのみ使える。");
                    }

                    if (scope == ScopeKind.FileScope) {
                        // 記憶域クラス指定子 auto 及び register が，外部宣言の宣言指定子列の中に現れてはならない。
                        if (storageClass == AnsiCParser.StorageClassSpecifier.Auto || storageClass == AnsiCParser.StorageClassSpecifier.Register) {
                            throw new CompilerException.SpecificationErrorException(start, end, "記憶域クラス指定子 auto 及び register が，外部宣言の宣言指定子列の中に現れてはならない。");
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

                    for (; ; ) {
                        var decl = FunctionOrVariableOrTypedefDeclaration(ident, type, storageClass, AnsiCParser.FunctionSpecifier.None, scope);
                        decls.Add(decl);

                        if (_lexer.ReadTokenIf(',')) {

                            ident = null;
                            stack = new List<CType> { new StubType() };
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
                        Declaration decl = FunctionDefinition(ident, type, storageClass, functionSpecifier);
                        decls.Add(decl);
                    } else {
                        // ブロックスコープでは関数定義は不可能
                        throw new CompilerException.SyntaxErrorException(start, end, "ブロックスコープ中では関数定義を行うことはできません。");
                    }
                } else {
                    throw new CompilerException.SyntaxErrorException(start, end, "文法エラーです。");
                }
                return decls;
            }
        }


        private int ReadArrayLength() {
            int len = -1;
            if (_lexer.PeekToken(']') == false) {
                var expr = ConstantExpression();
                var ret = ExpressionEvaluator.Eval(expr);
                var size = (int?)(ret as SyntaxTree.Expression.PrimaryExpression.Constant.IntegerConstant)?.Value;
                if (size.HasValue == false || size < 0) {
                    throw new CompilerException.SpecificationErrorException(expr.LocationRange, "配列の要素数には0以上の整数値を指定してください。");
                }
                len = size.Value;
            }
            _lexer.ReadToken(']');
            return len;
        }

    }
}
