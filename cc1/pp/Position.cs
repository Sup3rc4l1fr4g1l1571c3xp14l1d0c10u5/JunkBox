namespace CSCPP {
    /// <summary>
    /// ファイル上の位置情報
    /// </summary>
    public struct Position {

        /// <summary>
        /// ファイル名
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// 行番号
        /// </summary>
        public long Line { get; }

        /// <summary>
        /// 列番号
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// 内部を示す位置情報
        /// </summary>
        public static Position Empty { get; } = new Position("<cscpp>", 1, 1);

        public override string ToString() {
            return $"{FileName} ({Line}, {Column})";
        }

        public Position(string name, long line, int column) {
            System.Diagnostics.Debug.Assert(name != null);
            FileName = name;
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