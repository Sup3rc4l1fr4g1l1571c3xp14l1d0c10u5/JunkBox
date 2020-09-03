using System;
using System.Collections.Generic;

namespace AnsiCParser {
    public static class Settings {
        /// <summary>
        /// 構造体メンバのパッキングアライメント値（0はデフォルト。0以外は構造体のメンバのアライメントを指定したものとなり、ビットフィールドは隙間なしになる。）
        /// </summary>
        public static int PackSize { get; set; } = 0;
        public static Stack<int> PackSizeStack { get; set; } = new Stack<int>();

        /// <summary>
        /// 構造体のアライメント値（構造体自体のサイズのアライメント）
        /// </summary>
        public static int AlignSize { get; set; } = 0;

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
