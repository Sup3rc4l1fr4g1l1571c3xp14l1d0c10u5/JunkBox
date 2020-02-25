using System;
using System.Collections.Generic;

namespace X86Asm.ast {
    using statement;

    /// <summary>
    /// アセンブリ言語プログラム
    /// </summary>
    public sealed class Program {

        /// <summary>
        /// プログラムに含まれる文を表すリスト
        /// </summary>
        private List<IStatement> statements { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Program() {
            statements = new List<IStatement>();
        }

        /// <summary>
        /// プログラムに含まれる文へのアクセス。
        /// </summary>
        public IEnumerable<IStatement> Statements {
            get {
                return statements;
            }
        }

        /// <summary>
        /// プログラムに文を追加
        /// </summary>
        /// <param name="statement"></param>
        public void AddStatement(IStatement statement) {
            if (statement == null) {
                throw new ArgumentNullException();
            }
            statements.Add(statement);
        }

    }

}