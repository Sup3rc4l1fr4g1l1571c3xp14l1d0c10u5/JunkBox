using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public class SearchDictionary {
        public static void Run(string[] args) {
            string dictName = "dict.tsv";
            OptionParser op = new OptionParser();
            op.Regist(
                "-dic", 1,
                (xs) => { dictName = xs[0]; },
                (xs) => { return System.IO.File.Exists(xs[0]); }
            );
            args = op.Parse(args);

            Dict dict;
            using (var sw = new System.IO.StreamReader(dictName)) {
                dict = Dict.Load(sw);
            }
            for (;;) {
                Console.Write("> ");
                var key = Console.ReadLine();
                var n = 0;
                foreach (var entry in dict.CommonPrefixSearch(key)) {
                    Console.WriteLine($"  [{n++:D2}] {entry.Read}/{entry.Word}/{entry.Features[0]}");
                }
            }

        }
    }
}
