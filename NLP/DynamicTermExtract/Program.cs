using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using libNLP;
using libNLP.Extentions;

namespace AdaptiveTermExtract {
    class Program {
        static void Main(string[] args) {
            string mode = "predict";
            string model = null;
            string dict = null;
            int epoch = -1;
            bool help = false;

            var files = new OptionParser()
                .Regist("-help", action: () => { help = true; })
                .Regist("-mode", argc: 1, action: (v) => { mode = v[0]; }, validation: (v) => v[0] == "train" || v[0] == "predict")
                .Regist("-model", argc: 1, action: (v) => { model = v[0]; }, validation: (v) => String.IsNullOrWhiteSpace(v[0]) == false)
                .Regist("-dict", argc: 1, action: (v) => { dict = v[0]; }, validation: (v) => String.IsNullOrWhiteSpace(v[0]) == false)
                .Regist("-epoch", argc: 1, action: (v) => { epoch = int.Parse(v[0]); }, validation: (v) => { int x; return int.TryParse(v[0], out x) == true && x > 0; })
                .Parse(args);

            if (help) {
                Console.Error.WriteLine("Usage:");
                Console.Error.WriteLine("  AdaptiveTermExtract -mode=train -epoch=<epoch-num> -model=<model> -dict=<dict> <teature-data-file> ...");
                Console.Error.WriteLine("  AdaptiveTermExtract -mode=predict -model=<model> <segmented-text-file> ...");
                Console.Error.WriteLine("  AdaptiveTermExtract -help");
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
            if (dict != null && !System.IO.File.Exists(dict)) {
                Console.Error.WriteLine($"ファイル{dict}が存在しません。");
                return;
            }

            if (mode == "train") {
                var words = files.SelectMany(x => System.IO.File.ReadLines(x).Select(Mecab.ParseLine).Split(y => y.Item1 == "EOS"));
                var termExtractor = new AdaptiveTermExtractor();
                termExtractor.Learn(
                    epoch,
                    words,
                    dict != null ? System.IO.File.ReadLines(dict).Select(Mecab.ParseLine).Split(y => y.Item1 == "EOS").ToList() : null
                );
                termExtractor.Save(model);
                Console.WriteLine("学習が完了しました。");
            } else if (mode == "predict") {
                var termExtractor = AdaptiveTermExtractor.Load(model);
                var items = files.SelectMany(x => System.IO.File.ReadLines(x).Select(Mecab.ParseLine).Split(y => y.Item1 == "EOS"));
                foreach (var item in items) {
                    termExtractor.Extract(item).Select(x => String.Join("\t", x)).Apply(x => String.Join("\r\n", x)).Tap(Console.WriteLine);
                }
            } else {
                Console.Error.WriteLine("-modeに不正な処理モードが指定されました。");
                return;
            }
        }
    }
}
