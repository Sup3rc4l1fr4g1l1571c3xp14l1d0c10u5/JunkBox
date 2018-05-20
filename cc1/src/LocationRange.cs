namespace AnsiCParser {

    /// <summary>
    /// ソースコード中の範囲を示す位置情報
    /// </summary>
    public class LocationRange {

        /// <summary>
        /// 組込み型等の位置を示す
        /// </summary>
        public static LocationRange Builtin {
            get {
                var location = new Location("<built-in>", 1, 1, 0);
                return new LocationRange(location, location);
            }
        }

        /// <summary>
        /// 範囲の開始地点
        /// </summary>
        public Location Start { get; }

        /// <summary>
        /// 範囲の終了地点
        /// </summary>
        public Location End { get; }

        /// <summary>
        /// 空の位置情報
        /// </summary>
        public static readonly LocationRange Empty = new LocationRange(Location.Empty);

        public LocationRange(Location start) : this(start,start){}

        public LocationRange(Location start, Location end) {
            System.Diagnostics.Debug.Assert(start.FilePath != null);
            System.Diagnostics.Debug.Assert(end.FilePath != null);
            Start = start;
            End = end;
        }

        public LocationRange(LocationRange other) {
            System.Diagnostics.Debug.Assert(other.Start.FilePath != null);
            System.Diagnostics.Debug.Assert(other.End.FilePath != null);
            Start = other.Start;
            End = other.End;
        }

        public override string ToString() {
            if (Start.FilePath == End.FilePath) {
                return ($"{Start}-({End.Line},{End.Column})");
            } else {
                return ($"{Start}-{End}");
            }
        }
    }
}
