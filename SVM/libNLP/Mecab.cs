using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using libNLP.Extentions;

namespace libNLP {
    /// <summary>
    /// Mecabバインダ
    /// </summary>
    public static class Mecab {
        /// <summary>
        /// Mecabのパス
        /// </summary>
        public static string ExePath { get; set; } = @"C:\mecab\bin\mecab.exe";

        /// <summary>
        /// Mecabが存在するかチェック
        /// </summary>
        /// <returns></returns>
        public static bool Exists() {
            return System.IO.File.Exists(Mecab.ExePath);
        }

        /// <summary>
        /// Mecabを実行して実行結果を行単位で読み取る
        /// </summary>
        /// <param name="arg">mecabの起動パラメータ</param>
        /// <param name="input">mecabの標準入力に与えるデータ(nullの場合は標準入力を即座に閉じる)</param>
        /// <returns>mecabの出力結果を行単位で返す列挙子</returns>
        public static IEnumerable<string> Run(string arg, string input = null) {
            if (Exists() == false) {
                throw new System.IO.FileNotFoundException(ExePath);
            }
            using (var process = new Process()) {
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.FileName = Mecab.ExePath;
                process.StartInfo.Arguments = arg;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                process.ErrorDataReceived += (s, e) => { };
                if (process.Start()) {
                    process.BeginErrorReadLine();
                    if (input != null) {
                        var buffer = Encoding.UTF8.GetBytes(input + "\r\n");
                        process.StandardInput.BaseStream.Write(buffer, 0, buffer.Length);
                    }
                    process.StandardInput.Close();
                    while (process.StandardOutput.BaseStream.CanRead) {
                        string line = process.StandardOutput.ReadLine();
                        if (line == null) { break; }
                        yield return line;
                    }
                    process.WaitForExit();
                }
            }
        }

        /// <summary>
        /// Mecabの標準的な出力１行分を解析する
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static Tuple<string, string[]> ParseLine(string line) {
            return line.Split("\t".ToArray(), 2).Apply(x => Tuple.Create(x.ElementAtOrDefault(0, ""), x.ElementAtOrDefault(1, "").Split(",".ToArray())));
        }
    }
}
