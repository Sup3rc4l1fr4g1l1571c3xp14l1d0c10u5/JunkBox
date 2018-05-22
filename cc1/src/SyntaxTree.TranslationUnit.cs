using System.Collections.Generic;

namespace AnsiCParser {
    public abstract partial class SyntaxTree {
        /// <summary>
        /// 翻訳単位
        /// </summary>
        public class TranslationUnit : SyntaxTree {
            /// <summary>
            /// 結合オブジェクト表
            /// </summary>
            public List<LinkageObject> LinkageTable;

            public List<Declaration> Declarations { get; } = new List<Declaration>();

            public TranslationUnit(LocationRange locationRange) : base(locationRange) {
            }
        }
    }
}