
namespace AnsiCParser {
    /// <summary>
    /// �\�[�X�R�[�h���̈ʒu���
    /// </summary>
    public class Location {

        /// <summary>
        /// �_���\�[�X�t�@�C���p�X
        /// </summary>
        public string FilePath {
            get;
        }

        /// <summary>
        /// �_���\�[�X�t�@�C����̍s�ԍ�
        /// </summary>
        public int Line {
            get;
        }

        /// <summary>
        /// �_���\�[�X�t�@�C����̌��ԍ�
        /// </summary>
        public int Column {
            get;
        }

        /// <summary>
        /// �����\�[�X�t�@�C����̈ʒu
        /// </summary>
        public int Position {
            get;
        }

        public static Location Empty { get; } = new Location("", 1, 1, 0);

        public Location(string filepath, int line, int column, int position) {
            FilePath = filepath;
            Line = line;
            Column = column;
            Position = position;
        }

        public override string ToString() {
            return $"{FilePath}({Line},{Column})";
        }
    }

}