namespace AnsiCParser {
    namespace DataType {
        /// <summary>
        ///     スタブ型（型の解決中でのみ用いる他の型が入る穴）
        /// </summary>
        public class StubType : CType {

            /// <summary>
            /// 型のバイトサイズを取得（ビットフィールドの場合、元の型のサイズ）
            /// </summary>
            /// <returns></returns>
            public override int SizeOf() {
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "スタブ型のサイズを取得しようとしました。（想定では発生しないはずですが、本実装の型解決処理にどうやら誤りがあるようです。）。");
            }

            /// <summary>
            /// 型の境界調整（アラインメント）を取得（ビットフィールドの場合、元の型のアラインメント）
            /// </summary>
            /// <returns></returns>
            public override int AlignOf() {
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "スタブ型のアラインメントを取得しようとしました。（想定では発生しないはずですが、本実装の型解決処理にどうやら誤りがあるようです。）。");
            }

            public override CType Duplicate() {
                return new StubType();
            }

        }
    }

}
