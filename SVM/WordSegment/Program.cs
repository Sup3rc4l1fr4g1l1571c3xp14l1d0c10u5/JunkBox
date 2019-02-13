using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using libNLP;
using libNLP.Extentions;

namespace WordSegment {
    class Program {
        static void Main(string[] args) {
            {
                string mode = "predict";
                string model = null;
                int epoch = -1;
                bool help = false;

                var files = new OptionParser()
                    .Regist("-help", action: () => { help = true; })
                    .Regist("-mode", argc: 1, action: (v) => { mode = v[0]; }, validation: (v) => v[0] == "train" || v[0] == "predict")
                    .Regist("-model", argc: 1, action: (v) => { model = v[0]; }, validation: (v) => String.IsNullOrWhiteSpace(v[0]) == false)
                    .Regist("-epoch", argc: 1, action: (v) => { epoch = int.Parse(v[0]); }, validation: (v) => { int x; return int.TryParse(v[0], out x) == true && x > 0; })
                    .Parse(args);

                if (help) {
                    Console.Error.WriteLine("Usage:");
                    Console.Error.WriteLine("  WordSegment -mode=train -epoch=<epoch-num> -model=<model> <teature-data-file> ...");
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
                    var result = wseg.Train(epoch, files.SelectMany(x => System.IO.File.ReadLines(x).Apply(WordSegmenter.CreateTeachingData)));
                    wseg.Save(model);
                    // 学習結果を表示
                    Console.WriteLine(result);
                    Console.WriteLine("学習が完了しました。");
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
}
