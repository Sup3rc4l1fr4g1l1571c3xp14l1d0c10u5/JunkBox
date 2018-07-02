namespace AnsiCParser {
    namespace DataType {
        /// <summary>
        ///     スタブ型（型の解決中でのみ用いる他の型が入る穴）
        /// </summary>
        public class StubType : CType {
            public override int Sizeof() {
                throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "スタブ型のサイズを取得しようとしました。（想定では発生しないはずですが、本実装の型解決処理にどうやら誤りがあるようです。）。");
            }
            public override CType Duplicate() {
                return new StubType();
            }

        }
    }

}
