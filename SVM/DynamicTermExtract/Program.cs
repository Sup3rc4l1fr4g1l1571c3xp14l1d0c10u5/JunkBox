using System;
using System.Data;
using System.Linq;
using libNLP;
using libNLP.Extentions;

namespace AdaptiveTermExtract {
    class Program {
        static void Main(string[] args) {
            string mode = "predict";
            string model = null;
            string epoch = null;
            string arg = null;
            string mecab = null;
            bool help = false;

            for (var i = 0; i < args.Length; i++) {
                switch (args[i + 0]) {
                    case "-help": {
                            help = true;
                            break;
                        }
                    case "-mode": {
                            mode = args.ElementAtOrDefault(i + 1);
                            if (mode == null) { Console.Error.WriteLine("-mode引数が指定されていません。"); return; }
                            i += 1;
                            break;
                        }
                    case "-model": {
                            model = args.ElementAtOrDefault(i + 1);
                            if (model == null) { Console.Error.WriteLine("-model引数が指定されていません。"); return; }
                            i += 1;
                            break;
                        }
                    case "-epoc": {
                            epoch = args.ElementAtOrDefault(i + 1);
                            if (epoch == null) { Console.Error.WriteLine("-epoc引数が指定されていません。"); return; }
                            i += 1;
                            break;
                        }
                    case "-mecab": {
                            mecab = args.ElementAtOrDefault(i + 1);
                            if (mecab == null) { Console.Error.WriteLine("-mecab引数が指定されていません。"); return; }
                            i += 1;
                            break;
                        }
                    default: {
                            arg = args.ElementAtOrDefault(i + 1);
                            if (arg == null) { Console.Error.WriteLine("引数が指定されていません。"); return; }
                            i += 1;
                            break;
                        }
                }
            }

            if (help) {
                Console.Error.WriteLine("Usage:");
                Console.Error.WriteLine("  AdaptiveTermExtract -mode=train -epoc=<epoch-num> -model=<model> [-mecab=<mecab-path>] <word-dictionary>");
                Console.Error.WriteLine("  AdaptiveTermExtract -mode=predict -model=<model> [-mecab=<mecab-path>] <text-file>");
                return;
            }
            if (mecab != null) {
                Mecab.ExePath = mecab;
            }
            if (!System.IO.File.Exists(Mecab.ExePath)) {
                Console.Error.WriteLine("mecabが存在しません。");
                return;
            }

            int _epoc;
            if (mode == "train") {
                if (epoch == null || int.TryParse(epoch, out _epoc) == false || _epoc < 0) {
                    Console.Error.WriteLine("-epochパラメータが不正です。");
                    return;
                }
                if (String.IsNullOrWhiteSpace(model)) {
                    Console.Error.WriteLine("-modelパラメータが不正です。");
                    return;
                }
                if (!System.IO.File.Exists(arg)) {
                    Console.Error.WriteLine("学習用の辞書ファイルが存在しません。");
                    return;
                }
                var words = System.IO.File.ReadAllLines(arg);
                var termExtractor = new AdaptiveTermExtractor();
                termExtractor.Learn(
                    _epoc,
                    words
                    .Apply(x => Mecab.Run("", String.Join(Environment.NewLine, x)))
                    .Select(x => Mecab.ParseLine(x))
                    .Split(x => x.Item1 == "EOS")
                );
                termExtractor.Save(model);
                Console.WriteLine("学習が完了しました。");
            } else if (mode == "predict") {
                if (String.IsNullOrWhiteSpace(model)) {
                    Console.Error.WriteLine("-modelパラメータが不正です。");
                    return;
                }
                if (!System.IO.File.Exists(arg)) {
                    Console.Error.WriteLine("処理対象ファイルが存在しません。");
                    return;
                }
                var termExtractor = AdaptiveTermExtractor.Load(model);
                var items = Mecab.Run(arg).Select(Mecab.ParseLine).Split(x => x.Item1 == "EOS");
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

