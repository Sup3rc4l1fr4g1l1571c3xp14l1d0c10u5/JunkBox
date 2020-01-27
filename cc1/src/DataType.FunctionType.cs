using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AnsiCParser.SyntaxTree;

namespace AnsiCParser {

    namespace DataType {
        /// <summary>
        ///     関数型
        /// </summary>
        public class FunctionType : CType {
            public Scope<TaggedType> PrototypeTaggedScope {
                get;
            }
            public Scope<Declaration> PrototypeIdentScope {
                get;
            }

            public FunctionType(List<ArgumentInfo> arguments, bool hasVariadic, CType resultType, Scope<TaggedType> prototypeTaggedScope = null, Scope<Declaration> prototypeIdentScope = null) {
                // 6.7.5.3 関数宣言子（関数原型を含む）
                // 制約 
                // 関数宣言子は，関数型又は配列型を返却値の型として指定してはならない。(返却値の型が確定するFixupメソッド中で行う)
                // 仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。
                // 関数定義の一部でない関数宣言子における識別子並びは，空でなければならない。(これは関数定義/宣言中で行う。)
                // 関数定義の一部である関数宣言子の仮引数型並びにある仮引数は，型調整後に不完全型をもってはならない。(これは関数定義/宣言中で行う。)
                // 

                Arguments = arguments?.ToArray();
                ResultType = resultType;
                HasVariadic = hasVariadic;
                PrototypeTaggedScope = prototypeTaggedScope;
                PrototypeIdentScope = prototypeIdentScope;
            }

            public override CType Duplicate() {
                var ret = new FunctionType(Arguments?.Select(x => x.Duplicate()).ToList(), HasVariadic, ResultType.Duplicate(),PrototypeTaggedScope,PrototypeIdentScope);
                return ret;
            }


            /// <summary>
            ///     引数の情報
            ///     nullの場合、int foo(); のように識別子並びが空であることを示す。
            ///     空の場合、int foo(void); のように唯一のvoidであることを示す。
            ///     一つ以上の要素を持つ場合、int foo(int, double); のように引数を持つことを示す。また、引数リストにvoid型の要素は含まれない。
            ///     仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。
            /// </summary>
            public ArgumentInfo[] Arguments {
                get; private set;
            }

            /// <summary>
            /// 引数情報の設定
            /// </summary>
            /// <param name="value"></param>
            public void SetArguments(ArgumentInfo[] value) {
                if (value != null) {
                    // 6.7.5.3 関数宣言子（関数原型を含む）
                    // 制約 
                    // 仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。
                    foreach (var arg in value) {
                        if (arg.StorageClass != StorageClassSpecifier.None && arg.StorageClass != StorageClassSpecifier.Register) {
                            throw new CompilerException.SpecificationErrorException(arg.Range, "仮引数宣言に記憶域クラス指定子として，register 以外のものを指定してはならない。");
                        }
                        if (arg.Type.IsFunctionType()) {
                            // 仮引数を“～型を返却する関数”とする宣言は，6.3.2.1 の規定に従い，“～型を返却する関数へのポインタ”に型調整する。
                            Logger.Warning(arg.Range, "仮引数は“～型を返却する関数”として宣言されていますが，6.3.2.1 の規定に従い，“～型を返却する関数へのポインタ”に型調整します。");
                            arg.Type = CreatePointer(arg.Type);
                        }
                    }
                    // 意味規則
                    // 並びの中の唯一の項目が void 型で名前のない仮引数であるという特別な場合，関数が仮引数をもたないことを指定する。
                    if (value.Any(x => x.Type.IsVoidType())) {
                        if (value.Length != 1) {
                            throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "仮引数宣言並びがvoid 型を含むが唯一ではない。");
                        }
                        if (value.First().Ident != null) {
                            throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "仮引数宣言並び中のvoid型が名前を持っている。");
                        }
                        // 空で置き換える。
                        value = new ArgumentInfo[0];
                    }
                }

                Arguments = value;
            }

            /// <summary>
            ///     戻り値型
            /// </summary>
            public CType ResultType {
                get; private set;
            }

            /// <summary>
            ///     可変長引数の有無
            /// </summary>
            public bool HasVariadic {
                get;
            }

            public override void Fixup(CType type) {
                if (ResultType is StubType) {
                    ResultType = type;
                } else {
                    ResultType.Fixup(type);
                }
                // 関数宣言子は，関数型又は配列型を返却値の型として指定してはならない。
                if (ResultType.IsFunctionType() || ResultType.IsArrayType()) {
                    throw new CompilerException.SpecificationErrorException(Location.Empty, Location.Empty, "関数宣言子は，関数型又は配列型を返却値の型として指定してはならない。");
                }
            }

            /// <summary>
            /// 型のバイトサイズを取得（ビットフィールドの場合、元の型のサイズ）
            /// </summary>
            /// <returns></returns>
            public override int Sizeof() {
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "関数型のサイズは取得できません。（C言語規約上では、関数識別子はポインタ型に変換されているはずなので、これは本処理系に誤りがあることを示しています。）");
            }

            /// <summary>
            /// 型の境界調整（アラインメント）を取得（ビットフィールドの場合、元の型のアラインメント）
            /// </summary>
            /// <returns></returns>
            public override int Alignof() {
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "関数型のアラインメントは取得できません。（C言語規約上では、関数識別子はポインタ型に変換されているはずなので、これは本処理系に誤りがあることを示しています。）");
            }

            public enum FunctionStyle {
                OldStyle,       // 古い形式の関数宣言型（引数部が識別子並び）
                NewStyle,       // 新しい形式の関数宣言型（引数部が仮引数型並び）
                AmbiguityStyle, // 引数が省略されており曖昧
                InvalidStyle,   // 不正な形式
            }

            public FunctionStyle GetFunctionStyle() {
                var candidate = FunctionStyle.AmbiguityStyle;
                if (Arguments == null) {
                    return candidate;
                }
                foreach (var x in Arguments) {
                    if (x.Type.IsBasicType(BasicType.TypeKind.KAndRImplicitInt)) {
                        // 型が省略されている＝識別子並びの要素
                        Debug.Assert(!String.IsNullOrEmpty(x.Ident?.Raw));
                        if (candidate == FunctionStyle.AmbiguityStyle || candidate == FunctionStyle.OldStyle) {
                            // 現在の候補が古い形式、もしくは、曖昧形式なら、現在の候補を古い形式として継続判定
                            candidate = FunctionStyle.OldStyle;
                        } else {
                            // それ以外の場合は、不正な形式なので打ち切り。
                            // 識別子並び中に型名を記述してしまった場合もこのエラーになる
                            return FunctionStyle.InvalidStyle;
                        }
                    } else {
                        // 型が省略されていない＝仮引数型並びの要素
                        if (candidate == FunctionStyle.AmbiguityStyle || candidate == FunctionStyle.NewStyle) {
                            // 現在の候補が新しい形式、もしくは、曖昧形式なら、現在の候補を新しい形式として継続判定
                            candidate = FunctionStyle.NewStyle;
                        } else {
                            // それ以外の場合は、不正な形式なので打ち切り。
                            // 識別子並び中に型名を記述してしまった場合もこのエラーになる
                            return FunctionStyle.InvalidStyle;
                        }
                    }
                }
                return candidate;
            }

            public class ArgumentInfo {

                public ArgumentInfo(LocationRange range, Token ident, StorageClassSpecifier storageClass, CType type) {
                    Ident = ident;
                    StorageClass = storageClass;
                    // 6.7.5.3 関数宣言子（関数原型を含む）
                    // 制約
                    // 仮引数を“～型の配列”とする宣言は，“～型への修飾されたポインタ”に型調整する。
                    // そのときの型修飾子は，配列型派生の[及び]の間で指定したものとする。
                    // 配列型派生の[及び]の間にキーワード static がある場合，その関数の呼出しの際に対応する実引数の値は，大きさを指定する式で指定される数以上の要素をもつ配列の先頭要素を指していなければならない。
                    CType elementType;
                    if (type.IsArrayType(out elementType)) {
                        //ToDo: 及び。の間の型修飾子、static について実装
                        Logger.Warning(range.Start, range.End, "仮引数は“～型の配列”として宣言されていますが、6.7.5.3 関数宣言子の制約に従って“～型への修飾されたポインタ”に型調整されます。");
                        type = CreatePointer(elementType);
                    }
                    Type = type;
                    Range = range;
                }

                public Token Ident {
                    get; set;
                }
                public LocationRange Range {
                    get; set;
                }
                public StorageClassSpecifier StorageClass {
                    get;
                }

                // 関数型として外から見える引数型
                public CType Type {
                    get; set;
                }

                public ArgumentInfo Duplicate() {
                    return new ArgumentInfo(Range, Ident, StorageClass, Type.Duplicate());
                }
            }
        }
    }

}
