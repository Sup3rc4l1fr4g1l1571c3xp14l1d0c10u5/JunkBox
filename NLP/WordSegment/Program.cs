using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using libNLP;
using libNLP.Extentions;

namespace WordSegment {
    class Program {
        static void Main(string[] args) {
            //{
            //    var word = "1234567890";
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, -2, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, -1, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, 0, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, 1, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, 2, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, 3, "4567") == 4);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, 4, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, 5, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, 6, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, 7, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, 8, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, 9, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, 10, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, 11, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.StartWith(word, 12, "4567") == 0);

            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word, -2, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word, -1, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word,  0, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word,  1, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word,  2, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word,  3, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word,  4, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word,  5, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word,  6, "4567") == 4);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word,  7, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word,  8, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word,  9, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word, 10, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word, 11, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.EndWith(word, 12, "4567") == 0);

            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word, -2, "4567") == -1);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word, -1, "4567") == -1);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word,  0, "4567") == -1);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word,  1, "4567") == -1);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word,  2, "4567") == -1);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word,  3, "4567") == 0);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word,  4, "4567") == 1);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word,  5, "4567") == 2);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word,  6, "4567") == 3);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word,  7, "4567") == -1);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word,  8, "4567") == -1);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word,  9, "4567") == -1);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word, 10, "4567") == -1);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word, 11, "4567") == -1);
            //    System.Diagnostics.Debug.Assert(StringUtil.InsetWith(word, 12, "4567") == -1);

            //}
            {
                string mode = "predict";
                string model = null;
                int epoch = -1;
                bool help = false;

                var files = new OptionParser()
                    .Regist("-help", action: () => { help = true; })
                    .Regist("-mode", argc: 1, action: (v) => { mode = v[0]; }, validation: (v) => v[0] == "train" || v[0] == "predict" || v[0] == "test")
                    .Regist("-model", argc: 1, action: (v) => { model = v[0]; }, validation: (v) => String.IsNullOrWhiteSpace(v[0]) == false)
                    .Regist("-epoch", argc: 1, action: (v) => { epoch = int.Parse(v[0]); }, validation: (v) => { int x; return int.TryParse(v[0], out x) == true && x > 0; })
                    .Parse(args);

                if (help) {
                    Console.Error.WriteLine("Usage:");
                    Console.Error.WriteLine("  WordSegment -mode=train -epoch=<epoch-num> -model=<model> <teature-data-file> ...");
                    Console.Error.WriteLine("  WordSegment -mode=test -model=<model> <teature-data-file> ...");
                    Console.Error.WriteLine("  WordSegment -mode=predict -model=<model> <text-file> ...");
                    Console.Error.WriteLine("  WordSegment -help");
                    return;
                }

                if (String.IsNullOrWhiteSpace(model)) {
                    Console.Error.WriteLine("-modelパラメータが不正です。");
                    return;
                }
                foreach (var file in files) {
                    if (!System.IO.File.Exists(file)) {
                        Console.Error.WriteLine($"ファイル{file}が存在しません。");
                        return;
                    }
                }

                if (mode == "train") {
                    // 教師データを用いて線形SVMで分かち書きを学習
                    var wseg = new WordSegmenter();
                    var result = wseg.Train(
                        epoch, 
                        files.SelectMany(x => System.IO.File.ReadLines(x).Apply(WordSegmenter.CreateTeachingData))
                    );
                    wseg.Save(model);
                    // 学習結果を表示
                    Console.WriteLine(result);
                    Console.WriteLine("学習が完了しました。");
                } else if (mode == "test") {
                    var wseg = WordSegmenter.Load(model);
                    var result = wseg.Benchmark(files.SelectMany(x => System.IO.File.ReadLines(x).Apply(WordSegmenter.CreateTeachingData)));
                    Console.WriteLine(result);
                } else if (mode == "predict") {
                    var wseg = WordSegmenter.Load(model);
                    foreach (var words in files.SelectMany(x => System.IO.File.ReadLines(x).Where(y => String.IsNullOrWhiteSpace(y) == false).Select(y => wseg.Segmentation(y.Trim())))) {
                        foreach (var word in words) {
                            Console.WriteLine(word);
                        }
                        Console.WriteLine("EOS");
                    }
                } else {
                    Console.Error.WriteLine("-modeに不正な処理モードが指定されました。");
                    return;
                }
            }
        }
    }

    public static class StringUtil {
        /// <summary>
        /// 文字列 str の 位置 index を開始地点として文字列 word が存在するか？
        /// </summary>
        /// <param name="str"></param>
        /// <param name="index"></param>
        /// <param name="word"></param>
        /// <param name="maxLen"></param>
        /// <returns></returns>
        public static int StartWith(string str, int index, string word) {
            int n = 0;
            foreach (var ch in word) {
                if (0 > index + n || index + n >= str.Length) {
                    break;
                }
                if (str[index+n] != ch) { break; }
                n++;
            }
            return n;
        }
        /// <summary>
        /// 文字列 str の 位置 index を終了地点として文字列 word が存在するか？
        /// </summary>
        /// <param name="str"></param>
        /// <param name="index"></param>
        /// <param name="word"></param>
        /// <param name="maxLen"></param>
        /// <returns></returns>
        public static int EndWith(string str, int index, string word) {
            int n = 0;
            if ((index < 0) || (index < 0) || (index < word.Length)) {
                return n;
            }
            foreach (var ch in word.Reverse()) {
                if (0 > index - n || index - n >= str.Length) {
                    break;
                }
                if (str[index - n] != ch) { break; }
                n++;
            }
            return n;
        }
        public static int InsetWith(string str, int index, string word) {

            for (int i = 0; i < word.Length ; i++) {
                if (StartWith(str, index - i, word) > 0) {
                    return i;
                }
            }
            return -1;
        }
    }
}
