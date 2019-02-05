using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace svm_fobos {
    class Program {
        static void Main(string[] args) {
            {
                string[] lines = new[] {
"オートマチック|名詞|外 トランスミッション|名詞|外 （|補助記号|記号 英|名詞|漢 :|補助記号|記号 automatic|名詞|外 transmission|名詞|外 、|補助記号|記号 AT|名詞|外 ）|補助記号|記号 あるいは|接続詞|和 自動|名詞|漢 変速|名詞|漢 機|名詞|漢 （|補助記号|記号 じどう|名詞|漢 へ|助詞|和 ん|感動詞|和 そ|副詞|和 くき|名詞|和 ）|補助記号|記号 と|助詞|和 は|助詞|和 、|補助記号|記号 自動|名詞|漢 車|接尾辞|漢 や|助詞|和 オートバイ|名詞|外 の|助詞|和 変速|名詞|漢 機|名詞|漢 の|助詞|和 一種|名詞|漢 で|助詞|和 、|補助記号|記号 車速|名詞|漢 や|助詞|和 エンジン|名詞|外 の|助詞|和 回転|名詞|漢 速度|名詞|漢 に|助詞|和 応じ|動詞|混 て|助詞|和 変速|名詞|漢 比|名詞|漢 を|助詞|和 自動|名詞|漢 的|接尾辞|漢 に|助動詞|和 切り替える|動詞|和 機能|名詞|漢 を|助詞|和 備え|動詞|和 た|助動詞|和 、|補助記号|記号 トランスミッション|名詞|外 （|補助記号|記号 変速|名詞|漢 機|名詞|漢 ）|補助記号|記号 の|助詞|和 総称|名詞|漢 で|助動詞|和 ある|動詞|和 。|補助記号|記号",
"狭義|名詞|漢 に|助詞|和 は|助詞|和 変速|名詞|漢 機|名詞|漢 自体|名詞|漢 を|助詞|和 指す|動詞|和 が|助詞|和 、|補助記号|記号 発達|名詞|漢 の|助詞|和 経緯|名詞|漢 が|助詞|和 変速|名詞|漢 操作|名詞|漢 の|助詞|和 自動|名詞|漢 化|接尾辞|漢 のみ|助詞|和 なら|助動詞|和 ず|助動詞|和 、|補助記号|記号 マニュアル|名詞|外 トランスミッション|名詞|外 車|接尾辞|漢 （|補助記号|記号 以下|名詞|漢 、|補助記号|記号 MT|名詞|外 ）|補助記号|記号 から|助詞|和 クラッチ|名詞|外 ペダル|名詞|外 を|助詞|和 取り去る|動詞|和 こと|名詞|和 で|助詞|和 も|助詞|和 あっ|動詞|和 た|助動詞|和 ため|名詞|和 、|補助記号|記号 必然|名詞|漢 的|接尾辞|漢 に|助動詞|和 クラッチ|名詞|外 の|助詞|和 自動|名詞|漢 化|接尾辞|漢 を|助詞|和 伴っ|動詞|和 て|助詞|和 いる|動詞|和 。|補助記号|記号",
"その|連体詞|和 ため|名詞|和 、|補助記号|記号 広義|名詞|漢 に|助詞|和 AT|名詞|外 を|助詞|和 称する|動詞|混 場合|名詞|和 は|助詞|和 、|補助記号|記号 各種|名詞|漢 の|助詞|和 自動|名詞|漢 クラッチ|名詞|外 機構|名詞|漢 を|助詞|和 含める|動詞|和 こと|名詞|和 が|助詞|和 多い|形容詞|和 。|補助記号|記号",
"日本|名詞|固 で|助詞|和 は|助詞|和 「|補助記号|記号 オートマチック|名詞|外 トランスミッション|名詞|外 」|補助記号|記号 と|助詞|和 いう|動詞|和 呼び|動詞|和 方|接尾辞|和 が|助詞|和 長く|形容詞|和 煩雑|名詞|漢 で|助動詞|和 ある|動詞|和 こと|名詞|和 から|助詞|和 、|補助記号|記号 文章|名詞|漢 表記|名詞|漢 で|助詞|和 は|助詞|和 オート|名詞|外 ミッション|名詞|外 、|補助記号|記号 A/T|名詞|外 、|補助記号|記号 AT|名詞|外 と|助詞|和 略記|名詞|漢 さ|動詞|和 れる|助動詞|和 こと|名詞|和 が|助詞|和 多い|形容詞|和 。|補助記号|記号",
"口語|名詞|漢 で|助詞|和 は|助詞|和 オートマチック|名詞|外 ないし|接続詞|漢 は|助詞|和 オートマ|名詞|外 が|助詞|和 通用|名詞|漢 し|動詞|和 て|助詞|和 いる|動詞|和 。|補助記号|記号",
"古く|名詞|和 は|助詞|和 ノー|名詞|外 クラ|名詞|外 （|補助記号|記号 ノー|名詞|外 クラッチ|名詞|外 ペダル|名詞|外 ）|補助記号|記号 、|補助記号|記号 ノン|名詞|外 クラ|名詞|外 、|補助記号|記号 トルコン|名詞|外 、|補助記号|記号 オートミ|名詞 など|助詞|和 と|助詞|和 呼ば|動詞|和 れ|助動詞|和 た|助動詞|和 。|補助記号|記号",
"自動|名詞|漢 クラッチ|名詞|外 と|助詞|和 自動|名詞|漢 変速|名詞|漢 機構|名詞|漢 を|助詞|和 組み合わせ|動詞|和 て|助詞|和 自動|名詞|漢 車|接尾辞|漢 の|助詞|和 変速|名詞|漢 操作|名詞|漢 を|助詞|和 完全|形状詞|漢 自動|名詞|漢 化|接尾辞|漢 する|動詞|和 発想|名詞|漢 と|助詞|和 し|動詞|和 て|助詞|和 最も|副詞|和 古い|形容詞|和 例|名詞|漢 は|助詞|和 、|補助記号|記号 1905|名詞|漢 年|名詞|漢 に|助詞|和 アメリカ|名詞|固 の|助詞|和 スタート|名詞|外 バンド|名詞|外 兄弟|名詞|漢 が|助詞|和 考案|名詞|漢 し|動詞|和 た|助動詞|和 2|名詞|漢 段|名詞|漢 変速|名詞|漢 機|名詞|漢 で|助動詞|和 ある|動詞|和 。|補助記号|記号",
"これ|代名詞|和 は|助詞|和 単板|名詞|漢 クラッチ|名詞|外 2|名詞|漢 組|名詞|和 を|助詞|和 遠心|名詞|漢 力|接尾辞|漢 を|助詞|和 利用|名詞|漢 し|動詞|和 て|助詞|和 制御|名詞|漢 する|動詞|和 こと|名詞|和 で|助詞|和 自動|名詞|漢 変速|名詞|漢 さ|動詞|和 れる|助動詞|和 よう|形状詞|漢 に|助動詞|和 考え|動詞|和 られ|助動詞|和 て|助詞|和 い|動詞|和 た|助動詞|和 が|助詞|和 、|補助記号|記号 量産|名詞|漢 化|接尾辞|漢 は|助詞|和 さ|動詞|和 れ|助動詞|和 なかっ|助動詞|和 た|助動詞|和 。|補助記号|記号",
"1908|名詞|漢 年|名詞|漢 に|助詞|和 発売|名詞|漢 さ|動詞|和 れ|助動詞|和 た|助動詞|和 フォード|名詞|固 ・|補助記号|記号 モデル|名詞|外 T|記号|記号 は|助詞|和 、|補助記号|記号 大量|名詞|漢 生産|名詞|漢 技術|名詞|漢 の|助詞|和 駆使|名詞|漢 で|助詞|和 1927|名詞|和 年|名詞|漢 まで|助詞|和 の|助詞|和 19|名詞|漢 年間|名詞|漢 に|助詞|和 1|名詞|漢 ,|名詞 500|名詞|漢 万|名詞|漢 台|名詞|漢 が|助詞|和 生産|名詞|漢 さ|動詞|和 れる|助動詞|和 世界|名詞|漢 的|接尾辞|漢 な|助動詞|和 ベスト|名詞|外 セラー|名詞|外 に|助詞|和 なり|動詞|和 、|補助記号|記号 自動|名詞|漢 車|接尾辞|漢 の|助詞|和 歴史|名詞|漢 に|助詞|和 大きな|連体詞|和 足跡|名詞|和 を|助詞|和 残し|動詞|和 た|助動詞|和 が|助詞|和 、|補助記号|記号 特徴|名詞|漢 と|助詞|和 し|動詞|和 て|助詞|和 遊星|名詞|漢 歯車|名詞|和 と|助詞|和 多|形容詞|和 板|名詞|和 クラッチ|名詞|外 に|助詞|和 よる|動詞|和 前進|名詞|漢 2|名詞|漢 段|名詞|漢 、|補助記号|記号 後進|名詞|漢 1|名詞|漢 速|名詞|漢 の|助詞|和 半|名詞|漢 自動|名詞|漢 変速|名詞|漢 機|名詞|漢 を|助詞|和 標準|名詞|漢 装備|名詞|漢 し|動詞|和 て|助詞|和 い|動詞|和 た|助動詞|和 。|補助記号|記号",
"この|連体詞|和 構造|名詞|漢 は|助詞|和 1910|名詞|漢 年|名詞|漢 代|名詞|漢 まで|助詞|和 の|助詞|和 手動|名詞|漢 変速|名詞|漢 機|名詞|漢 車|接尾辞|漢 に|助詞|和 比較|名詞|漢 し|動詞|和 て|助詞|和 格段|形状詞|漢 に|助動詞|和 操作|名詞|漢 が|助詞|和 簡易|形状詞|漢 で|助動詞|和 あっ|動詞|和 た|助動詞|和 。|補助記号|記号",
"ただし|接続詞|和 、|補助記号|記号 自動|名詞|漢 車|接尾辞|漢 が|助詞|和 高速|名詞|漢 化|接尾辞|漢 ・|補助記号|記号 強力|形状詞|漢 化|接尾辞|漢 する|動詞|和 に|助詞|和 伴い|動詞|和 、|補助記号|記号 固定|名詞|漢 変速|名詞|漢 比|名詞|漢 の|助詞|和 2|名詞|漢 速|名詞|漢 変速|名詞|漢 機|名詞|漢 で|助詞|和 は|助詞|和 特に|副詞|混 高速|名詞|漢 域|接尾辞|漢 で|助詞|和 の|助詞|和 巡航|名詞|漢 に|助詞|和 おけ|動詞|和 る|助動詞|和 実用|名詞|漢 性|接尾辞|漢 が|助詞|和 得|動詞|和 られ|助動詞|和 なく|助動詞|和 なり|動詞|和 、|補助記号|記号 市場|名詞|漢 の|助詞|和 趨勢|名詞|漢 は|助詞|和 3|名詞|漢 〜|補助記号|記号 4|名詞|和 速|名詞|漢 の|助詞|和 手動|名詞|漢 変速|名詞|漢 機|名詞|漢 に|助詞|和 とっ|動詞|和 て|助詞|和 代わら|動詞|和 れ|助動詞|和 た|助動詞|和 。|補助記号|記号",
"クラッチ|名詞|外 を|助詞|和 自動|名詞|漢 化|接尾辞|漢 し|動詞|和 た|助動詞|和 4|名詞|和 段|名詞|漢 程度|名詞|漢 の|助詞|和 遊星|名詞|漢 歯車|名詞|和 式|名詞|漢 半|名詞|漢 自動|名詞|漢 変速|名詞|漢 機|名詞|漢 は|助詞|和 1920|名詞|外 年|名詞|漢 代|名詞|漢 末期|名詞|漢 から|助詞|和 出現|名詞|漢 し|動詞|和 た|助動詞|和 が|助詞|和 （|補助記号|記号 例|名詞|漢 ：|補助記号|記号 プリセレクタ・トランスミッション|名詞 ）|補助記号|記号 、|補助記号|記号 採用|名詞|漢 し|動詞|和 た|助動詞|和 事例|名詞|漢 は|助詞|和 イギリス|名詞|固 、|補助記号|記号 フランス|名詞|固 など|助詞|和 の|助詞|和 一部|名詞|漢 メーカー|名詞|外 の|助詞|和 製品|名詞|漢 に|助詞|和 留まっ|動詞|和 て|助詞|和 おり|動詞|和 、|補助記号|記号 また|接続詞|和 その|連体詞|和 作動|名詞|漢 は|助詞|和 完全|形状詞|漢 自動|名詞|漢 化|接尾辞|漢 に|助詞|和 まで|助詞|和 は|助詞|和 到達|名詞|漢 し|動詞|和 て|助詞|和 い|動詞|和 なかっ|助動詞|和 た|助動詞|和 。|補助記号|記号",
                };

                var input = "文字クラスで、文字が定義されているコードの範囲を指定します。";

                List<Tuple<int, Dictionary<string, double>>> teatures = new List<Tuple<int, Dictionary<string, double>>>();
                foreach (var line in lines) {
                    var tokens = line.Split(" ".ToArray()).Select(x => x.Split("|".ToArray())).ToList();
                    var lineText = tokens.Select(x => x[0]).Apply(x => String.Join(" ", x));
                    var index = new List<int>();
                    var words = "\u0001 " + lineText + " \uFFFE";
                    for (var i = 0; i < words.Length; i++) {
                        if (words[i] != ' ') {
                            index.Add(i);
                        }
                    }
                    for (var i = 1; i < index.Count - 1; i++) {
                        var n = index[i - 1];
                        var m = index[i + 0];
                        var l = index[i + 1];
                        teatures.Add(
                            Tuple.Create(
                                (m + 1 != l) ? +1 : -1,
                                new Dictionary<string, double>()
                                    .Apply(x => AppendFeatures(x, -1, words[n]))
                                    .Apply(x => AppendFeatures(x,  0, words[m]))
                                    .Apply(x => AppendFeatures(x,  1, words[l]))
                            )
                        );
                    }
                }
                var svm1 = new LinerSVM<string>();
                for (var i = 0; i < 100; i++) {
                    foreach (var kv in teatures) {
                        svm1.Train(kv.Item2, kv.Item1, 0.06, 0.005);
                    }
                    svm1.Regularize(0.005);
                }
                Console.WriteLine(TestResult.Test(svm1, teatures));
                {
                    var line = "\t" + input + "\v";
                    for (var i = 1; i < line.Length - 1; i++) {
                        Console.Write(line[i]);
                        if (svm1.Predict(
                               new Dictionary<string, double>()
                                    .Apply(x => AppendFeatures(x, -1, line[i - 1]))
                                    .Apply(x => AppendFeatures(x, 0, line[i + 0]))
                                    .Apply(x => AppendFeatures(x, 1, line[i + 1]))
                            ) >= 0) {
                            Console.Write(" ");
                        }
                    }
                }
                System.Console.ReadKey();
            }
            //{
            //    var rand = new Random();
            //    var data = LinerSVM<int>.ReadDataFromFile("news20.binary", int.Parse).OrderBy(x => rand.Next()).ToList();
            //    var tests = data.Take(data.Count / 10).ToList();
            //    var teatures = data.Skip(data.Count / 10).ToList();
            //    var svm1 = new LinerSVM<int>();
            //    for (var i = 0; i < 10; i++) {
            //        foreach (var kv in teatures) {
            //            svm1.Train(kv.Item2, kv.Item1, 0.06, 0.005);
            //        }
            //        Console.WriteLine($"RemovedFeature: {svm1.Regularize(0.005)}");
            //        Console.WriteLine(TestResult.Test(svm1, tests));
            //    }
            //}
        }

        private static bool isKanji(char v) {
            return ("々〇〻".IndexOf(v) != -1) || ('\u3400' <= v && v <= '\u9FFF') || ('\uF900' <= v && v <= '\uFAFF') || ('\uD840' <= v && v <= '\uD87F') || ('\uDC00' <= v && v <= '\uDFFF');
        }
        private static bool isHiragana(char v) {
            return ('\u3041' <= v && v <= '\u3096');
        }
        private static bool isKatakana(char v) {
            return ('\u30A1' <= v && v <= '\u30FA');
        }
        private static bool isAlpha(char v) {
            return (char.IsLower(v) || char.IsUpper(v));
        }
        private static bool isDigit(char v) {
            return char.IsDigit(v);
        }
        private static Dictionary<string, double> AppendFeatures(Dictionary<string, double> fv, int nGramIndex, char v) {
            var sIndex = nGramIndex.ToString("+0;-0");
            fv.Add($"3G{sIndex} {v}", 1);

            fv.Add($"D{sIndex} ", isDigit(v) ? +1 : -1);
            fv.Add($"A{sIndex} ", isAlpha(v) ? +1 : -1);
            fv.Add($"S{sIndex} ", char.IsSymbol(v) ? +1 : -1);
            fv.Add($"H{sIndex} ", isHiragana(v) ? +1 : -1);
            fv.Add($"K{sIndex} ", isKatakana(v) ? +1 : -1);
            fv.Add($"J{sIndex} ", isKanji(v) ? +1 : -1);

            return fv;
        }

    }

    public static class Extention {
        /// <summary>
        /// selfに関数predを適用します
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="self"></param>
        /// <param name="pred"></param>
        /// <returns></returns>
        public static T2 Apply<T1, T2>(this T1 self, Func<T1, T2> pred) {
            return pred(self);
        }

        /// <summary>
        /// 辞書型から指定したキーを持つ要素を取得し返します。指定したキーを持つ要素が見つからない場合は代用値を返します。
        /// </summary>
        /// <typeparam name="TKey">ディクショナリ内のキーの型</typeparam>
        /// <typeparam name="TValue">ディクショナリ内の値の型</typeparam>
        /// <param name="self">辞書型</param>
        /// <param name="key">取得または設定する要素のキー</param>
        /// <param name="defaultValue">指定したキーを持つ要素が見つからない場合の代用値</param>
        /// <returns>キーが見つかった場合は、指定したキーに関連付けられている値。それ以外の場合は defaultValue パラメーターで指定されている代用値。</returns>
        public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, TValue defaultValue) {
            TValue value;
            if (self.TryGetValue(key, out value)) {
                return value;
            } else {
                return defaultValue;
            }
        }

        /// <summary>
        /// 要素を重複ありで n 要素ずつに区切った列挙子を返します。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static IEnumerable<T[]> EachCons<T>(this IEnumerable<T> self, int n) {
            var ret = new Queue<T>(n);
            foreach (var next in self) {
                if (ret.Count == n) {
                    yield return ret.ToArray();
                    ret.Dequeue();
                }
                ret.Enqueue(next);
            }
        }
    }

    /// <summary>
    /// FOBOSを用いた線形SVM
    /// </summary>
    /// <typeparam name="TFeature">特徴を示す型</typeparam>
    public class LinerSVM<TFeature> {

        /// <summary>
        /// libsvm/libliner形式のデータの読み取り
        /// </summary>
        /// <param name="file">読み取り対象ファイル</param>
        /// <param name="deserializer">特徴のデシリアライザ</param>
        /// <returns></returns>
        public static IEnumerable<Tuple<int, Dictionary<TFeature, double>>> ReadDataFromFile(string file, Func<string, TFeature> deserializer) {
            return System.IO.File.ReadLines(file)
                                    .Select(x => x.Trim().Split(" ".ToCharArray()))
                                    .Select(x => Tuple.Create(
                                                    int.Parse(x[0]),
                                                    x.Skip(1)
                                                     .Select(y => y.Split(":".ToArray(), 2))
                                                     .ToDictionary(y => deserializer(y[0]), y => double.Parse(y[1]))
                                                 )
                                            );
        }

        /// <summary>
        /// 重みベクトル
        /// </summary>
        private Dictionary<TFeature, double> Weight { get; }

        /// <summary>
        /// パラメータa の値を b 分だけ0に近づけるが、近づけて符号が変わったら0でクリッピングする
        /// </summary>
        /// <param name="a">パラメータ</param>
        /// <param name="b">近づける量（0以上の実数）</param>
        /// <returns></returns>
        private static double ClipByZero(double a, double b) {
            return Math.Sign(a) * Math.Max(0, Math.Abs(a) - b);
        }

        /// <summary>
        /// 行列積(w += y * x * eta)
        /// </summary>
        /// <param name="w"></param>
        /// <param name="fv"></param>
        /// <param name="y"></param>
        /// <param name="eta"></param>
        private static void MulAdd(Dictionary<TFeature, double> w, Dictionary<TFeature, double> fv, int y, double eta) {
            foreach (var kv in fv) {
                var key = kv.Key;
                var x_i = kv.Value;
                w[key] = w.GetValue(key, 0) + y * x_i * eta;
            }
        }

        /// <summary>
        /// L1正則化
        /// </summary>
        /// <param name="lambda_hat"></param>
        private int L1Regularize(double lambda_hat) {
            var keys = Weight.Keys.ToList();
            var removed = 0;
            foreach (var key in keys) {
                var aaa = Weight[key];
                Weight[key] = ClipByZero(Weight[key], lambda_hat);
                if (Math.Abs(aaa) < lambda_hat) {
                    Weight.Remove(key);
                    removed++;
                }
            }
            return removed;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public LinerSVM() {
            this.Weight = new Dictionary<TFeature, double>();
        }

        /// <summary>
        /// 特徴ベクトルの内積を求める
        /// </summary>
        /// <param name="fv1">特徴ベクトル1</param>
        /// <param name="fv2">特徴ベクトル2</param>
        /// <returns>内積</returns>
        public static double DotProduct(Dictionary<TFeature, double> fv1, Dictionary<TFeature, double> fv2) {
            double m = 0;
            foreach (var kv in fv2) {
                var key = kv.Key;
                var x_i = kv.Value;
                m += x_i * fv1.GetValue(key, 0);
            }
            return m;
        }

        /// <summary>
        /// 識別を実行
        /// </summary>
        /// <param name="fv">特徴ベクトル</param>
        /// <returns></returns>
        public double Predict(Dictionary<TFeature, double> fv) {
            return DotProduct(Weight, fv);
        }

        /// <summary>
        /// 学習を実行
        /// </summary>
        /// <param name="fv">特徴ベクトル</param>
        /// <param name="y">正解ラベル(+1/-1)</param>
        /// <param name="eta"></param>
        /// <param name="lambda"></param>
        public void Train(Dictionary<TFeature, double> fv, int y, double eta, double lambda_hat) {
            if (Predict(fv) * y < 1.0) {
                // 損失関数 l(w) が最も小さくなる方向へ重みベクトル w を更新
                MulAdd(Weight, fv, y, eta);
            }
        }

        /// <summary>
        /// 正則化を適用
        /// </summary>
        /// <param name="lambda_hat"></param>
        public int Regularize(double lambda_hat) {
            // 更新した重みパラメータをできるだけ動かさずにL1正則化を行う
            return L1Regularize(lambda_hat);
        }

    };

    public class TestResult {
        public static TestResult Test<TFeature>(LinerSVM<TFeature> svm, List<Tuple<int, Dictionary<TFeature, double>>> fvs) {
            var truePositive = 0;
            var falsePositive = 0;
            var falseNegative = 0;
            var trueNegative = 0;
            foreach (var fv in fvs) {
                var prediction = fv.Item1 < 0;
                var fact = svm.Predict(fv.Item2) < 0;
                if (prediction) {
                    if (fact) {
                        truePositive++;
                    } else {
                        falsePositive++;
                    }
                } else {
                    if (fact) {
                        falseNegative++;
                    } else {
                        trueNegative++;
                    }
                }
            }
            return new TestResult(truePositive, falsePositive, falseNegative, trueNegative);
        }
        private TestResult(int truePositive, int falsePositive, int falseNegative, int trueNegative) {
            this.TruePositive = truePositive;
            this.FalsePositive = falsePositive;
            this.FalseNegative = falseNegative;
            this.TrueNegative = trueNegative;
        }

        public double Accuracy => (double)(TruePositive + TrueNegative) / (TruePositive + FalsePositive + FalseNegative + TrueNegative);
        public double Precision => (double)(TruePositive) / (TruePositive + FalsePositive);
        public double Recall => (double)(TruePositive) / (TruePositive + FalseNegative);
        public double FMeasure => (2 * Recall * Precision) / (Recall + Precision);

        public int Total => TruePositive + FalsePositive + FalseNegative + TrueNegative;

        public int TruePositive { get; }
        public int FalsePositive { get; }
        public int FalseNegative { get; }
        public int TrueNegative { get; }

        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendLine($"Total: {Total}");
            sb.AppendLine($"  TP/FP/FN/TN: {TruePositive}/{FalsePositive}/{FalseNegative}/{TrueNegative}");
            sb.AppendLine($"  Accuracy   : {Accuracy}");
            sb.AppendLine($"  Precision  : {Precision}");
            sb.AppendLine($"  Recall     : {Recall}");
            sb.AppendLine($"  F-Measure  : {FMeasure}");
            return sb.ToString();
        }

    }

}
