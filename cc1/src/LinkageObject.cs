using System.Collections.Generic;

namespace AnsiCParser {
    /// <summary>
    /// リンケージ情報オブジェクト
    /// </summary>
    public class LinkageObject {

        public LinkageObject(string ident, CType type, LinkageKind linakge) {
            Ident = ident;
            Type = type;
            Linkage = linakge;
        }

        /// <summary>
        /// 名前
        /// </summary>
        public string Ident {
            get;
        }

        /// <summary>
        /// 型
        /// </summary>
        public CType Type {
            get;
        }

        /// <summary>
        /// リンケージ
        /// </summary>
        public LinkageKind Linkage {
            get;
        }

        /// <summary>
        /// 仮宣言
        /// </summary>
        public List<SyntaxTree.Declaration> TentativeDefinitions { get; } = new List<SyntaxTree.Declaration>();

        /// <summary>
        /// 本宣言
        /// </summary>
        public SyntaxTree.Declaration Definition { get; set; }
    }
}