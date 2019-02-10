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
        internal static IEnumerable<string> Run(string file, string input=null) {
            if (System.IO.File.Exists(ExePath) == false) {
                throw new System.IO.FileNotFoundException(ExePath);
            }
            if (System.IO.File.Exists(file) == false) {
                throw new System.IO.FileNotFoundException(file);
            }
            using (var process = new Process()) {
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.FileName = Mecab.ExePath;
                process.StartInfo.Arguments = file;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                process.ErrorDataReceived += (s, e) => { };//Console.Error.WriteLine(e.Data);
                if (process.Start()) {
                    process.BeginErrorReadLine();
                    if (input != null) {
                        var bytes = Encoding.UTF8.GetBytes(input);
                        process.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
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

        internal static Tuple<string,string[]> ParseLine(string line) {
            var kv = line.Split("\t".ToArray(), 2);
            if (kv[0] == "EOS") {
                return Tuple.Create(kv[0], new string [] { });
            } else {
                var m = kv[0];
                var f = kv[1].Split(",".ToArray());
                return Tuple.Create(m, f);
            }
        }
    }
}
