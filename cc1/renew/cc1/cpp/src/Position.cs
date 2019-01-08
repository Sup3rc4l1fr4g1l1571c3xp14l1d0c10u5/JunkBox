namespace CSCPP
{
    /// <summary>
    /// ファイル位置情報
    /// </summary>
    public sealed class Position
    {

        /// <summary>
        /// ファイル名
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// 行番号
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// 列番号
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// 内部を示す位置情報
        /// </summary>
        public static Position Empty { get; } = new Position("<cscpp>", 1, 1);

        /// <summary>
        /// 文字列化
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return $"{FileName} ({Line}, {Column})";
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="line"></param>
        /// <param name="column"></param>
        public Position(string fileName, int line, int column) {
            System.Diagnostics.Debug.Assert(fileName != null);
            FileName = fileName;
            Line = line;
            Column = column;
        }

        public bool Equals(Position other) {
            return (FileName == other.FileName) && (Line == other.Line) && (Column == other.Column);
        }

        public override bool Equals(object obj) {
            if (obj == null) { return false; }
            if (!(obj is Position)) { return false; }
            return Equals((Position)obj);
        }
        public override int GetHashCode() {
            return FileName.GetHashCode();
        }
    };
}
