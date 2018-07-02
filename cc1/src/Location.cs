using System.Collections.Generic;
using System.Linq;


namespace AnsiCParser {
    /// <summary>
    /// ソースコード中の位置情報
    /// </summary>
    public struct Location {
        private static List<string> FilePathTable { get; } = new List<string>();

        /// <summary>
        /// 論理ソースファイルパス
        /// </summary>
        public string FilePath {
            get {
                return FilePathTable[FilePathIndex];
            }
        }

        private int FilePathIndex;

        /// <summary>
        /// 論理ソースファイル上の行番号
        /// </summary>
        public int Line {
            get;
        }

        /// <summary>
        /// 論理ソースファイル上の桁番号
        /// </summary>
        public int Column {
            get;
        }

        /// <summary>
        /// 物理ソースファイル上の位置
        /// </summary>
        public int Position {
            get;
        }

        public static Location Empty { get; } = new Location("", 1, 1, 0);

        public Location(string filepath, int line, int column, int position) {
            FilePathIndex = FilePathTable.IndexOf(filepath);
            if (FilePathIndex == -1) {
                FilePathIndex = FilePathTable.Count;
                FilePathTable.Add(filepath);
            }
            Line = line;
            Column = column;
            Position = position;
        }

        public override string ToString() {
            return $"{FilePath} ({Line},{Column})";
        }
    }
}
