using System;

namespace AnsiCParser {
    /// <summary>
    /// コンパイラ例外の基底クラス
    /// </summary>
    public abstract class CompilerException : Exception {
        /// <summ　ary>
        /// 例外発生時のトークン開始位置
        /// </summary>
        public Location Start {
            get;
        }

        /// <summary>
        /// 例外発生時のトークン終了位置
        /// </summary>
        public Location End {
            get;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="start">例外発生時のトークン開始位置</param>
        /// <param name="end">例外発生時のトークン終了開始</param>
        /// <param name="message">メッセージ</param>
        protected CompilerException(Location start, Location end, string message) : base(message) {
            Start = start;
            End = end;
        }

        /// <summary>
        /// 構文エラー
        /// </summary>
        public class SyntaxErrorException : CompilerException {

            public SyntaxErrorException(Location start, Location end, string message) : base(start, end, message) {
            }
            public SyntaxErrorException(LocationRange range, string message) : base(range.Start, range.End, message) {
            }
        }

        /// <summary>
        /// 未定義識別子・タグ名エラー
        /// </summary>
        public class UndefinedIdentifierErrorException : CompilerException {

            public UndefinedIdentifierErrorException(Location start, Location end, string message) : base(start, end, message) {
            }
            public UndefinedIdentifierErrorException(LocationRange range, string message) : base(range.Start, range.End, message) {
            }
        }

        /// <summary>
        /// 仕様違反エラー
        /// </summary>
        public class SpecificationErrorException : CompilerException {
            public SpecificationErrorException(Location start, Location end, string message) : base(start, end, message) {
            }
            public SpecificationErrorException(LocationRange range, string message) : base(range.Start, range.End, message) {
            }
        }

        /// <summary>
        /// 型不整合エラー
        /// </summary>
        public class TypeMissmatchError : CompilerException {
            public TypeMissmatchError(Location start, Location end, string message) : base(start, end, message) {
            }
            public TypeMissmatchError(LocationRange range, string message) : base(range.Start, range.End, message) {
            }
        }

        /// <summary>
        /// コンパイラ内部エラー
        /// </summary>
        public class InternalErrorException : CompilerException {
            public InternalErrorException(Location start, Location end, string message) : base(start, end, message) {
            }
            public InternalErrorException(LocationRange range, string message) : base(range.Start, range.End, message) {
            }
        }
    }
}