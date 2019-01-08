namespace AnsiCParser {
    /// <summary>
    /// 記憶クラス指定子
    /// </summary>
    public enum StorageClassSpecifier {
        None,
        Auto,
        Register,
        Static,
        Extern,
        Typedef
    }
    public static partial class StorageClassSpecifierExt {
        public static StorageClassSpecifier Marge(this StorageClassSpecifier self, StorageClassSpecifier other) {
            if (self == StorageClassSpecifier.None) {
                return other;
            } else if (other == StorageClassSpecifier.None) {
                return self;
            } else {
                if (self != other) {
                    throw new System.Exception("");
                } else {
                    return self;
                }
            }
        }
    }
}
