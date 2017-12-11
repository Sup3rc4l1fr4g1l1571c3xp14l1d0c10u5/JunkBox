using System.Collections.Generic;

namespace AnsiCParser {
    public class LabelScopeValue {
        /// <summary>
        /// ラベル参照地点
        /// </summary>
        public List<SyntaxTree.Statement.GotoStatement> Reference { get; }
        /// <summary>
        /// ラベル定義
        /// </summary>
        public SyntaxTree.Statement.GenericLabeledStatement Declaration { get; private set; }
        public LabelScopeValue() {
            Reference = new List<SyntaxTree.Statement.GotoStatement>();
            Declaration = null;
        }
        /// <summary>
        /// ラベルの参照地点を追加
        /// </summary>
        /// <param name="gotoStmt"></param>
        public void AddReference(SyntaxTree.Statement.GotoStatement gotoStmt) {
            if (Declaration != null) {
                gotoStmt.Target = Declaration;
            }
            Reference.Add(gotoStmt);
        }
        /// <summary>
        /// ラベルの定義地点を設定
        /// </summary>
        /// <param name="labeledStmt"></param>
        public void SetDeclaration(SyntaxTree.Statement.GenericLabeledStatement labeledStmt) {
            if (Declaration != null) {
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty,"ラベルの定義地点を再定義しようとした。");
            } else {
                Declaration = labeledStmt;
                Reference.ForEach(x => {
                    if (x.Target != null) {
                        throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "既に参照先の決まっているgoto文の参照先を再定義しようとした。");
                    }
                    x.Target = labeledStmt;
                });
            }
        }
    }
}