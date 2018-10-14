using System.Collections.Generic;

namespace AnsiCParser {
    /// <summary>
    /// ソースコード中の位置情報
    /// </summary>
    public class Location {
        /// <summary>
        /// 論理ソースファイルパス
        /// </summary>
        private static List<string> FilePathTable { get; } = new List<string>();

        /// <summary>
        /// 論理ソースファイルパス
        /// </summary>
        public string FilePath {
            get {
                return FilePathTable[_filePathIndex];
            }
        }

        /// <summary>
        /// 論理ソースファイルパス表のID
        /// </summary>
        private readonly int _filePathIndex;

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

        /// <summary>
        /// 空の位置情報
        /// </summary>
        public static Location Empty { get; } = new Location("", 1, 1, 0);

        public Location(string filepath, int line, int column, int position) {
            _filePathIndex = FilePathTable.IndexOf(filepath);
            if (_filePathIndex == -1) {
                _filePathIndex = FilePathTable.Count;
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
