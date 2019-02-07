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
        internal static IEnumerable<string> RunMecab(string file) {
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
                process.ErrorDataReceived += (s, e) => Console.Error.WriteLine(e.Data);
                if (process.Start()) {
                    process.BeginErrorReadLine();
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

        internal static IEnumerable<Tuple<string, string, string>[]> CreateTrainDataUsingMecabUnidic(string file) {
            var temp = new List<Tuple<string, string, string>>();
            foreach (var x in RunMecab(file)) {
                var kv = x.Split("\t".ToArray(), 2);
                if (kv[0] == "EOS") {
                    if (temp.Any()) {
                        yield return temp.ToArray();
                        temp.Clear();
                    }
                } else {
                    var m = kv[0];
                    var f = kv[1].Split(",".ToArray());
                    var f0 = f.DefaultIfEmpty("不明語").ElementAtOrDefault(0);
                    var f12 = f.DefaultIfEmpty("不").ElementAtOrDefault(12);
                    temp.Add(Tuple.Create(m, f0, f12));
                }
            }
        }
    }
}
