using System;

namespace AnsiCParser {
    internal static class Settings {
        public static int DefaultPackSize { get; set; } = 4;  // gcc は 8 なのに注意
        public static int PackSize { get; set; } = Settings.DefaultPackSize;
    }
}
