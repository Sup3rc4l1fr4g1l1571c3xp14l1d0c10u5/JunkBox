using System.Collections.Generic;

namespace AnsiCParser {
    /// <summary>
    /// ソースコード中の位置情報
    /// </summary>
    public struct Location {
        /// <summary>
        /// 論理ソースファイルパス
        /// </summary>
        private static List<string> FilePathTable { get; } = new List<string>();
        private static Dictionary<string,int> FilePathMap { get; } = new Dictionary<string, int>();

        /// <summary>
        /// ファイル位置表現の値
        /// [fileId(3byte)][line(3byte)][column(2byte)]
        /// </summary>
        private ulong EncodedValue { get; }
        
        /// <summary>
        /// 論理ソースファイルパス
        /// </summary>
        public string FilePath {
            get {
                return FilePathTable[FilePathIndex];
            }
        }

        /// <summary>
        /// 論理ソースファイルパス表のID
        /// </summary>
        private int FilePathIndex {
            get { return (int)((uint)(EncodedValue >> (5*8)) & 0xFFFFFFU); }
        }

        /// <summary>
        /// 論理ソースファイル上の行番号
        /// </summary>
        public int Line {
            get { return (int)((uint)(EncodedValue >> (2*8)) & 0xFFFFFFU); }
        }

        /// <summary>
        /// 論理ソースファイル上の桁番号
        /// </summary>
        public int Column {
            get { return (int)((uint)EncodedValue & 0xFFFFU); }
        }

        ///// <summary>
        ///// 物理ソースファイル上の位置
        ///// </summary>
        //public int Position {
        //    get;
        //}

        /// <summary>
        /// 空の位置情報
        /// </summary>
        public static Location Empty { get; } = new Location("", 1, 1/*, 0*/);

        public Location(string filepath, int line, int column/*, int position*/) {
            int filePathIndex;
            if (!FilePathMap.TryGetValue(filepath, out filePathIndex)) {
                filePathIndex = FilePathTable.Count;
                FilePathMap[filepath] = filePathIndex;
                FilePathTable.Add(filepath);
            }

            EncodedValue = ((ulong)(filePathIndex & 0xFFFFFF) << (5 * 8))|((ulong)(line & 0xFFFFFF) << (2*8))|((ulong)column & 0xFFFF);
            //Position = position;
        }

        public override string ToString() {
            return $"{FilePath} ({Line},{Column})";
        }
    }
}
