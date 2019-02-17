using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using libNLP;
using libNLP.Extentions;

namespace PosTagging {
    class Program {
        static void Main(string[] args) {
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
                Console.Error.WriteLine("  PosTagging -mode=train -epoch=<epoch-num> -model=<model> <teature-data-file> ...");
                Console.Error.WriteLine("  PosTagging -mode=test -model=<model> <teature-data-file> ...");
                Console.Error.WriteLine("  PosTagging -mode=predict -model=<model> <segmented-text-file> ...");
                Console.Error.WriteLine("  PosTagging -help");
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
                StructuredPerceptron sp = null;
                var teatures = files.SelectMany(x => WordSegmenter.CreateTrainData(System.IO.File.ReadLines(x))).Select(x => x.Select(y => Tuple.Create(y.Item1, y.Item2)).ToArray()).ToList();
                sp = StructuredPerceptron.Train(
                    new HashSet<string>(teatures.SelectMany(x => x.Select(y => y.Item2)).Distinct()),
                    teatures,
                    epoch
                );
                using (var sw = new System.IO.StreamWriter(model)) {
                    sp.SaveToStream(sw);
                }
                Console.WriteLine(sp.Test(teatures));
                Console.WriteLine("学習が完了しました。");
            } else if (mode == "test") {
                    StructuredPerceptron sp = null;
                    var teatures = files.SelectMany(x => WordSegmenter.CreateTrainData(System.IO.File.ReadLines(x))).Select(x => x.Select(y => Tuple.Create(y.Item1, y.Item2)).ToArray()).ToList();
                using (var sr = new System.IO.StreamReader(model)) {
                    sp = StructuredPerceptron.LoadFromStream(sr);
                }
                Console.WriteLine(sp.Test(teatures));
                } else if (mode == "predict") {
                StructuredPerceptron sp;
                using (var sr = new System.IO.StreamReader(model)) {
                    sp = StructuredPerceptron.LoadFromStream(sr);
                }
                var items = files.SelectMany(x => System.IO.File.ReadLines(x)).Split(x => x == "EOS");
                foreach (var item in items) {
                    var ret = sp.Predict(item);

                    foreach (var x in ret) {
                        Console.WriteLine($"{x.Item1}\t{x.Item2}");
                    }
                }
            } else {
                Console.Error.WriteLine("-modeに不正な処理モードが指定されました。");
                return;
            }
        }
    }
}
