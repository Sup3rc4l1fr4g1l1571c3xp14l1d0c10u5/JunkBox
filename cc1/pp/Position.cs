namespace CSCPP
{
    /// <summary>
    /// ファイル上の位置情報
    /// </summary>
    public class Position
    {

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

        public override string ToString()
        {
            return $"{FileName} ({Line}, {Column})";
        }

        public Position(string name, long line, int column)
        {
            System.Diagnostics.Debug.Assert(name != null);
            //System.Diagnostics.Debug.Assert(line > 0);
            //System.Diagnostics.Debug.Assert(column > 0);
            FileName = name;
            Line = line;
            Column = column;
        }

        public bool Equals(Position other) {
            return (this.FileName == other.FileName) && (this.Line == other.Line) && (this.Column == other.Column);
        }

        public override bool Equals(object obj) {
            if (obj == null) { return false; }
            if (ReferenceEquals(obj, this)) { return true; }
            if (!(obj is Position)) { return false; }
            return Equals((Position)obj);
        }
        public override int GetHashCode() {
            return FileName.GetHashCode();
        }
    };
}