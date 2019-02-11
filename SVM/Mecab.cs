
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace svm_fobos {
    /// <summary>
    /// Mecabバインダ
    /// </summary>
    internal static class Mecab {
        /// <summary>
        /// Mecabパス
        /// </summary>
        public static string ExePath { get; set; } = @"C:\mecab\bin\mecab.exe";

        /// <summary>
        /// Mecabを実行して実行結果を読み取る
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal static IEnumerable<string> Run(string arg, string input = null) {
            if (System.IO.File.Exists(ExePath) == false) {
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
        /// Mecabの出力1行を解析する
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        internal static Tuple<string, string[]> ParseLine(string line) {
            return line.Split("\t".ToArray(), 2).Apply(x => Tuple.Create(x.ElementAtOrDefault(0, ""), x.ElementAtOrDefault(1, "").Split(",".ToArray())));
        }
    }
}
