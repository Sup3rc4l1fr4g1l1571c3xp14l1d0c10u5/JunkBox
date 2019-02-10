using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace svm_fobos {
    /// <summary>
    /// 専門用語抽出
    /// </summary>
    public static class TermExtractor {
        /// <summary>
        /// 形態素解析結果から複合語を返す
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static List<string> Find(List<Tuple<string, string[]>> data) {
            var cmp_nouns = new List<string>();
            var must = false;  // 次の語が名詞でなければならない場合は真
            var terms = new List<string>(); // 複合語リスト作成用の作業用配列
                                            // 単名詞の連結処理
            foreach (var noun_value in data) {
                var noun = noun_value.Item1;
                var value = noun_value.Item2;
                var part_of_speach = value[0];
                var cl_1 = value[1];
                var cl_2 = value[2];
                if ((part_of_speach == "名詞" && cl_1 == "普通名詞") ||
                        (part_of_speach == "名詞" && cl_1 == "接尾" && cl_2 == "一般") ||
                        (part_of_speach == "名詞" && cl_1 == "接尾" && cl_2 == "サ変接続") ||
                        (part_of_speach == "名詞" && cl_1 == "固有名詞") ||
                        (part_of_speach == "記号" && cl_1 == "アルファベット") ||
                        (part_of_speach == "名詞" && cl_1 == "サ変接続" && !Regex.IsMatch(@"[!\""#$%&'\(\)*+,-./{\|}:;<>\[\]\?!]$", noun))) {
                    terms.Add(noun);
                    must = false;
                } else if (
                            (part_of_speach == "名詞" && cl_1 == "形容動詞語幹") ||
                            (part_of_speach == "名詞" && cl_1 == "ナイ形容詞語幹")) {
                    terms.Add(noun);
                    must = true;
                } else if (part_of_speach == "名詞" && cl_1 == "接尾" && cl_2 == "形容動詞語幹") {
                    terms.Add(noun);
                    must = true;
                } else if (part_of_speach == "動詞") {
                    terms.Clear();
                } else {
                    if (!must) {
                        _increase(cmp_nouns, terms);
                    }
                    must = false;
                    terms.Clear();
                }
            }

            if (!must) {
                _increase(cmp_nouns, terms);
            }
            return cmp_nouns;

        }
        private static HashSet<string> SETSUBI = new HashSet<string>() { "など", "ら", "上", "内", "型", "間", "中", "毎" };
        private static void _increase(List<string> cmp_nouns, List<string> terms) {
            // 専門用語リストへ、整形して追加
            // 語頭の不要な語の削除
            if (terms.Count > 1) {
                if (terms[0] == "本") {
                    terms.RemoveAt(0);
                }
            }
            if (terms.Any()) {
                // 語尾の余分な語の削除
                var end = terms.Last();
                if (SETSUBI.Contains(end) || Regex.IsMatch(@"\s+$", end)) {
                    terms.RemoveAt(terms.Count - 1);
                }
            }
            if (terms.Any()) {
                var cmp_noun = string.Join(" ", terms);
                cmp_nouns.Add(cmp_noun);
                terms.Clear();
            }
        }
    }
}
