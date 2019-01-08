using System.Collections.Generic;
using AnsiCParser.SyntaxTree;

namespace AnsiCParser {
    public partial class Parser {
        /// <summary>
        /// リンケージオブジェクト表
        /// </summary>
        public class LinkageObjectTable {
            /// <summary>
            /// リンケージオブジェクトの名前引き表(外部結合・内部結合解決用)
            /// </summary>
            private readonly Dictionary<string, LinkageObject> _linkageTable = new Dictionary<string, LinkageObject>();

            /// <summary>
            /// リンケージオブジェクトリスト
            /// </summary>
            public List<LinkageObject> LinkageObjects { get; } = new List<LinkageObject>();

            /// <summary>
            /// リンケージオブジェクトの生成とリンケージ表への登録
            /// </summary>
            /// <param name="ident"></param>
            /// <param name="linkage"></param>
            /// <param name="decl"></param>
            /// <param name="isDefine"></param>
            /// <returns></returns>
            public LinkageObject RegistLinkageObject(Token ident, LinkageKind linkage, Declaration decl, bool isDefine) {
                switch (linkage) {
                    case LinkageKind.ExternalLinkage:
                    case LinkageKind.InternalLinkage: {
                        // 外部もしくは内部結合なので再定義チェック
                        LinkageObject value;
                        if (_linkageTable.TryGetValue(decl.Ident, out value)) {
                            if (value.Linkage != linkage) {
                                throw new CompilerException.SpecificationErrorException(ident.Range, "オブジェクトのリンケージが以前のオブジェクトと一致しない");
                            }

                            if (Specification.IsCompatible(value.Type, decl.Type) == false) {
                                throw new CompilerException.TypeMissmatchError(ident.Start, ident.End, "オブジェクトの型が以前のオブジェクトと一致しない");
                            }

                            if (value.Definition != null && isDefine) {
                                throw new CompilerException.SpecificationErrorException(ident.Range, "オブジェクトは既に実体をもっています");
                            }
                        }
                        else {
                            value = new LinkageObject(decl.Ident, decl.Type, linkage);
                            _linkageTable[decl.Ident] = value;
                            LinkageObjects.Add(value);
                        }

                        if (!isDefine) {
                            // 仮定義
                            value.TentativeDefinitions.Add(decl);
                        }
                        else {
                            // 本定義
                            if (value.Definition != null) {
                                // 上記判定の結果、ここにはこないはず
                                throw new CompilerException.InternalErrorException(ident.Start, ident.End, "リンケージオブジェクトの本定義が再度行われた（本処理系の不具合です。）");
                            }

                            value.Definition = decl;
                        }

                        return value;
                    }

                    case LinkageKind.NoLinkage: {
                        // 無結合なので再定義チェックはしない。(名前表上で再定義のチェックは終わっているはず。)
                        var value = new LinkageObject(decl.Ident, decl.Type, linkage);
                        // static の場合は、staticなリンケージ名を生成して使う
                        if (decl.StorageClass == AnsiCParser.StorageClassSpecifier.Static) {
                            value.Definition = decl;
                            value.LinkageId = $"{decl.Ident}.{LinkageObjects.Count}";
                            LinkageObjects.Add(value);
                        }

                        return value;
                    }

                    default:
                        throw new CompilerException.SpecificationErrorException(ident.Range, "リンケージが指定されていません。");
                }
            }

        }
    }
}