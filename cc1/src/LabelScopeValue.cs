using System.Collections.Generic;

namespace AnsiCParser {
    public class LabelScopeValue {
        /// <summary>
        /// ラベルの宣言地点
        /// </summary>
        public SyntaxTree.Statement.GenericLabeledStatement Declaration {
            get;
            internal set;
        }

        /// <summary>
        /// ラベルの参照地点
        /// </summary>
        public List<SyntaxTree.Statement.GotoStatement> References {
            get;
        } = new List<SyntaxTree.Statement.GotoStatement>();

        public void SetDeclaration(SyntaxTree.Statement.GenericLabeledStatement labelStmt) {
            if (Declaration != null) {
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "ラベルの宣言地点は既に設定済みです。（本処理系の誤りです。）");
            }
            Declaration = labelStmt;
            foreach (var reference in References) {
                reference.Target = labelStmt;
            }
        }

        public void AddReference(SyntaxTree.Statement.GotoStatement gotoStmt) {
            References.Add(gotoStmt);
            if (Declaration != null) {
                gotoStmt.Target = Declaration;
            }
        }

    }
}