using System;

namespace AnsiCParser {
    /// <summary>
    /// 関数指定子
    /// </summary>
    [Flags]
    public enum FunctionSpecifier {
        None = 0x0000,
        Inline = 0x0001
    }

    public static class FunctionSpecifierExt {
        public static FunctionSpecifier Marge(this FunctionSpecifier self, FunctionSpecifier other) {
            return self | other;
        }
    }

}
