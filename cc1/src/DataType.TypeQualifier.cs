using System;

namespace AnsiCParser {
    namespace DataType {
        /// <summary>
        /// 型修飾子
        /// </summary>
        [Flags]
        public enum TypeQualifier : UInt16 {
            None = 0x0000,
            Const = 0x0001,
            Volatile = 0x002,
            Restrict = 0x0004,
            Near = 0x0010,
            Far = 0x0020,
            Invalid = 0x1000,
        }
    }
    public static class TypeQualifierExt {
        public static DataType.TypeQualifier Marge(this DataType.TypeQualifier self, DataType.TypeQualifier other) {
            return self | other;
        }
    }
}

