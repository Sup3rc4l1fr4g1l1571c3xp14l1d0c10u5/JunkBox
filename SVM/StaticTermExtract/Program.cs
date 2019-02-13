using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using libNLP;
using libNLP.Extentions;

namespace StaticTermExtract {
    class Program {
        static void Main(string[] args) {
            bool help = false;

            var files = new OptionParser()
                .Regist("-help", action: () => { help = true; })
                .Parse(args);

            if (help) {
                Console.Error.WriteLine("Usage:");
                Console.Error.WriteLine("  StaticTermExtract <mecabed-text-file> ...");
                Console.Error.WriteLine("  StaticTermExtract -help ");
                return;
            }

            foreach (var file in files) {
                if (!System.IO.File.Exists(file)) {
                    Console.Error.WriteLine($"ファイル{file}が存在しません。");
                    return;
                }
            }

            var termExtractor = new StaticTermExtractor();
            var items = files.SelectMany(x => Mecab.Run(x).Select(Mecab.ParseLine).Split(y => y.Item1 == "EOS"));
            foreach (var item in items) {
                termExtractor.Extract(item).Select(x => String.Join("\t", x)).Apply(x => String.Join("\r\n", x)).Tap(Console.WriteLine);
            }
        }
    }
}
