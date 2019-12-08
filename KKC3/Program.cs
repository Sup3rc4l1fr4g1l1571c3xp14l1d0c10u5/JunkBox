﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KKC3 {
    class Program {
        static void Main(string[] args) {
            // 辞書を作成
            var myargs = new[] {
             "-i", "tsv", @"..\..\data\corpus\*.txt",
             "-i", "unidic", @"c:\mecab\lib\mecab\dic\unidic\*.csv"
            };
            CreateDictionary.Run(myargs);

            // 辞書検索の実行
            //SearchDictionary.Run(args);

            // 学習を実行
            //Train.Run(args);

            // 検査を実行
            //Validation.Run(args);

            // 交差検証を実行
            //CrossValidation.Run(args);

            // かな漢字変換を実行
            KanaKanji.Run(args);
        }
    }
}
