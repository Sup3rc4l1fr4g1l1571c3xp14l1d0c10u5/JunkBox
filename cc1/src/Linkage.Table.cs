using System.Collections.Generic;
using AnsiCParser.SyntaxTree;

namespace AnsiCParser.Linkage {
    /// <summary>
    /// リンケージオブジェクト表
    /// </summary>
    public class Table {
        /// <summary>
        /// リンケージオブジェクトの名前引き表(外部結合・内部結合解決用)
        /// </summary>
        private readonly Dictionary<string, Object> _linkageTable = new Dictionary<string, Object>();

        /// <summary>
        /// リンケージオブジェクトリスト
        /// </summary>
        public List<Object> LinkageObjects { get; } = new List<Object>();

        /// <summary>
        /// リンケージオブジェクトの生成とリンケージ表への登録
        /// </summary>
        /// <param name="ident"></param>
        /// <param name="linkage"></param>
        /// <param name="decl"></param>
        /// <param name="isDefine"></param>
        /// <returns></returns>
        public Object RegistLinkageObject(Token ident, Kind linkage, Declaration decl, bool isDefine) {
            switch (linkage) {
                case Kind.ExternalLinkage:
                case Kind.InternalLinkage: {
                        // 外部もしくは内部結合なので再定義チェック
                        Object value;
                        if (_linkageTable.TryGetValue(decl.Ident, out value)) {
                            // 以前のオブジェクトが存在する
                            if (value.Linkage != linkage) {
                                throw new CompilerException.SpecificationErrorException(ident.Range, "オブジェクトのリンケージが以前のオブジェクトと一致しない");
                            }

                            if (Specification.IsCompatible(value.Type, decl.Type) == false) {
                                throw new CompilerException.TypeMissmatchError(ident.Start, ident.End, "オブジェクトの型が以前のオブジェクトと一致しない");
                            }

                            if (value.Definition != null && isDefine) {
                                throw new CompilerException.SpecificationErrorException(ident.Range, "オブジェクトは既に実体をもっています");
                            }
                        } else {
                            // 以前のオブジェクトが存在しない
                            value = Object.Create(decl, linkage);
                            _linkageTable[decl.Ident] = value;
                            LinkageObjects.Add(value);
                        }

                        if (!isDefine) {
                            // 仮定義
                            value.TentativeDefinitions.Add(decl);
                        } else {
                            // 本定義
                            if (value.Definition != null) {
                                // 上記判定の結果、ここにはこないはず
                                throw new CompilerException.InternalErrorException(ident.Start, ident.End, "リンケージオブジェクトの本定義が再度行われた（本処理系の不具合です。）");
                            }

                            value.Definition = decl;
                        }

                        return value;
                    }

                case Kind.NoLinkage: {
                        // 無結合なので再定義チェックはしない。(名前表上で再定義のチェックは終わっているはず。)

                        // static の場合は、uniqueなリンケージ名を生成して使う
                        if (decl.StorageClass == StorageClassSpecifier.Static) {
                            var value = Object.CreateUnique(decl, linkage);
                            value.Definition = decl;
                            LinkageObjects.Add(value);
                            return value;
                        } else {
                            return Object.Create(decl, linkage);
                        }
                    }

                default:
                    throw new CompilerException.SpecificationErrorException(ident.Range, "リンケージが指定されていません。");
            }
        }

    }
}