﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KKC3 {
    class Program {
        static void Main(string[] args) {
            // 辞書を作成
            //var myargs = new[] {
            // "-i", "tsv", @"..\..\data\corpus\*.txt",
            // "-i", "unidic", @"c:\mecab\lib\mecab\dic\unidic\*.csv"
            //};
            //CreateDictionary.Run(myargs);

            // 辞書検索の実行
            //var myargs = new[] {
            // "-dic", "dict.tsv",
            //};
            //SearchDictionary.Run(myargs);

            // 学習を実行
            //var myargs = new[] {
            //     "-dic", "dict.tsv",
            //     "-i", @"..\..\data\Corpus\*.txt",
            //     "-o", @"learn.model"
            //};
            //Train.Run(myargs);

            // 検査を実行
            //var myargs = new[] {
            //     "-dic", "dict.tsv",
            //     "-i", @"..\..\data\Corpus\*.txt",
            //     "-model", @"learn.model"
            //};
            //Validation.Run(myargs);

            // 交差検証を実行
            //var myargs = new[] {
            //     "-dic", "dict.tsv",
            //     "-i", @"..\..\data\Corpus\*.txt",
            //};
            //CrossValidation.Run(myargs);

            // かな漢字変換を実行
            var myargs = new[] {
                 "-dic", "dict.tsv",
                 "-userdic","userdic.tsv",
                 "-model", @"learn.model"
            }; 
            KanaKanji.Run(myargs);
        }
    }
}
