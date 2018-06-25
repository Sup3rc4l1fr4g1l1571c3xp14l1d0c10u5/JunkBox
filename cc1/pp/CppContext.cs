using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CSCPP
{
    /// <summary>
    /// プリプロセッサのコンテキスト
    /// </summary>
    public static class CppContext {
        /// <summary>
        /// オリジナルの起動時引数
        /// </summary>
        public static string[] OriginalArguments = new string[0];

        /// <summary>
        /// プリプロセス対象ファイル
        /// </summary>
        public static string   TargetFilePath { get; set; }= "-";

        /// <summary>
        /// 警告フラグ
        /// </summary>
        public static HashSet<Warning> Warnings { get; } = new HashSet<Warning>();

        /// <summary>
        /// 機能フラグ
        /// </summary>
        public static HashSet<Feature> Features { get; } = new HashSet<Feature>();

        /// <summary>
        /// 冗長出力フラグ
        /// </summary>
        public static HashSet<Verbose> Verboses { get; } = new HashSet<Verbose>();

        /// <summary>
        /// オプションスイッチ
        /// </summary>
        public static HashSet<string> Switchs { get; } = new HashSet<string>();

        /// <summary>
        /// レポートフラグ
        /// </summary>
        public static HashSet<Report> Reports { get; } = new HashSet<Report>();

        /// <summary>
        /// 出力器(マクロ展開位置の正確な取得に使用)
        /// </summary>
        public static Program.CppWriter CppWriter { get; set; } = null;

        /// <summary>
        /// マクロ展開情報
        /// </summary>
        public static List<Tuple<Token, Macro, int, int, int, int>> ExpandLog { get; } = new List<Tuple<Token, Macro, int, int, int, int>>();

        /// <summary>
        /// プリプロセスエラー数
        /// </summary>
        public static int ErrorCount { get; private set; }

        /// <summary>
        /// プリプロセス警告数
        /// </summary>
        public static int WarningCount { get; private set; }

        /// <summary>
        /// デフォルト文字コード
        /// </summary>
        public static System.Text.Encoding DefaultEncoding { get; set; } = System.Text.Encoding.Default;

        /// <summary>
        /// 文字コードの自動認識
        /// </summary>
        public static bool AutoDetectEncoding { get; set; }

        /// <summary>
        /// エラーメッセージ出力
        /// </summary>
        /// <param name="caption">エラーの見出し</param>
        /// <param name="pos">エラー発生位置</param>
        /// <param name="message">エラーメッセージ</param>
        private static void OutputError(Position pos, string caption, string message) {
            Console.Error.Write(pos.ToString());
            Console.Error.WriteLine($" : ** {caption} ** : {message}");
        }

        /// <summary>
        /// 内部エラーメッセージ出力
        /// </summary>
        /// <param name="tok"></param>
        /// <param name="message"></param>
        public static void InternalError(Token tok, string message)
        {
            Debug.Assert(tok != null);
            OutputError(tok.Position, "INTERNAL-ERROR", message);
            ErrorCount++;
        }

        /// <summary>
        /// エラーメッセージ出力
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            OutputError(Position.Empty, "ERROR", message);
            ErrorCount++;
        }

        /// <summary>
        /// エラーメッセージ出力
        /// </summary>
        /// <param name="tok"></param>
        /// <param name="message"></param>
        public static void Error(Token tok, string message)
        {
            Debug.Assert(tok != null);
            Error(tok.Position, message);
        }

        /// <summary>
        /// エラーメッセージ出力
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="message"></param>
        public static void Error(Position pos, string message)
        {
            OutputError(pos, "ERROR", message);
            ErrorCount++;
        }

        /// <summary>
        /// 警告メッセージ出力
        /// </summary>
        /// <param name="tok"></param>
        /// <param name="message"></param>
        public static void Warning(Token tok, string message)
        {
            Debug.Assert(tok != null);
            if (Warnings.Contains(CSCPP.Warning.Error)) {
                Error(tok, message);
            } else {
                OutputError(tok.Position, "WARNING", message);
                WarningCount++;
            }
        }

        /// <summary>
        /// 警告メッセージ出力
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="message"></param>
        public static void Warning(Position pos, string message) {
            if (Warnings.Contains(CSCPP.Warning.Error)) {
                Error(pos, message);
            } else {
                OutputError(pos, "WARNING", message);
                WarningCount++;
            }
        }

        /// <summary>
        /// 警告メッセージ出力
        /// </summary>
        /// <param name="message"></param>
        public static void Warning(string message) {
            if (Warnings.Contains(CSCPP.Warning.Error)) {
                Error(message);
            } else {
                OutputError(Position.Empty, "WARNING", message);
                WarningCount++;
            }
       }

        /// <summary>
        /// メッセージ出力
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="message"></param>
        public static void Notify(Position pos, string message) {
            OutputError(pos, "NOTIFY", message);
        }

    }
}