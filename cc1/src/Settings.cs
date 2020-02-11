using System;

namespace AnsiCParser {
    public static class Settings {
        /// <summary>
        /// 構造体メンバのアライメント値
        /// </summary>
        public static int PackSize { get; set; } = 0;

        /// <summary>
        /// Ｃ言語規格
        /// </summary>
        public enum CLanguageStandard {
            None,
            C89,
            C99    // 完全実装ではない
        }

        /// <summary>
        /// コンパイラが受理するＣ言語規格
        /// </summary>
        public static CLanguageStandard LanguageStandard { get; set; } = CLanguageStandard.C99;

    }
}
