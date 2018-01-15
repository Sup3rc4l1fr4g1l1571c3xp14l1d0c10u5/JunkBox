using System;
using System.Collections.Generic;

namespace AnsiCParser {
    public static class Logger {

        // エラー

        private static void ErrorBanner() {
            Console.Error.Write("**error: ");
        }
        public static void Error(string msg) {
            ErrorBanner();
            Console.Error.WriteLine(msg);
        }
        public static void Error(Location loc, string msg) {
            ErrorBanner();
            Console.Error.Write($"{loc}: ");
            Console.Error.WriteLine(msg);
        }
        public static void Error(Location start, Location end, string msg) {
            ErrorBanner();
            if (start.FilePath == end.FilePath) {
                Console.Error.Write($"{start}-({end.Line},{end.Column}): ");
            } else {
                Console.Error.Write($"{start}-{end}: ");
            }
            Console.Error.WriteLine(msg);
        }

        // 警告

        private static void WarningBanner() {
            Console.Error.Write("**warning: ");
        }
        public static void Warning(string msg) {
            WarningBanner();
            Console.Error.WriteLine(msg);
        }
        public static void Warning(Location loc, string msg) {
            WarningBanner();
            Console.Error.Write($"{loc}: ");
            Console.Error.WriteLine(msg);
        }
        public static void Warning(Location start, Location end, string msg) {
            WarningBanner();
            if (start.FilePath == end.FilePath) {
                Console.Error.Write($"{start}-({end.Line},{end.Column}): ");
            } else {
                Console.Error.Write($"{start}-{end}: ");
            }
            Console.Error.WriteLine(msg);
        }
    }


}