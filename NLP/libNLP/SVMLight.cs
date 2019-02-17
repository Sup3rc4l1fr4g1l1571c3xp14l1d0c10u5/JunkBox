using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using libNLP.Extentions;

namespace libNLP {
    /// <summary>
    /// SVM-Light形式のデータを読み取る
    /// </summary>
    /// <typeparam name="TFeature">特徴を示す型</typeparam>
    public static class SVMLight {
        /// <summary>
        /// SVM-Light形式のデータをファイルから読み取る
        /// </summary>
        /// <param name="file">読み取り対象ファイル</param>
        /// <param name="deserializer">特徴のデシリアライザ</param>
        /// <returns></returns>
        public static IEnumerable<Tuple<int, Dictionary<TFeature, double>>> ParseAllData<TFeature>(string file, Func<string, TFeature> deserializer) {
            return System.IO.File.ReadLines(file).Apply(lines => ParseAllData(lines, deserializer));
        }

        /// <summary>
        /// SVM-Light形式のデータをシーケンスから読み取る
        /// </summary>
        /// <param name="lines">読み取るシーケンス</param>
        /// <param name="deserializer">特徴のデシリアライザ</param>
        /// <returns></returns>
        public static IEnumerable<Tuple<int, Dictionary<TFeature, double>>> ParseAllData<TFeature>(IEnumerable<string> lines, Func<string, TFeature> deserializer) {
            return lines.Select(x => ParseData(x, deserializer));
        }

        /// <summary>
        /// SVM-Light形式のデータを一つ読み取る
        /// </summary>
        /// <param name="line">１データ</param>
        /// <param name="deserializer">特徴のデシリアライザ</param>
        /// <returns></returns>
        public static Tuple<int, Dictionary<TFeature, double>> ParseData<TFeature>(string line, Func<string, TFeature> deserializer) {
            return line.Trim()
                       .Split("#".ToArray(), 2).ElementAtOrDefault(0)
                       .Split(" ".ToCharArray())
                       .Apply(x => Tuple.Create(
                                        int.Parse(x[0]),
                                        x.Skip(1)
                                            .Select(y => y.Split(":".ToArray(), 2))
                                            .ToDictionary(y => deserializer(y[0]), y => double.Parse(y[1]))
                                        )
                                );
        }
    }
}
