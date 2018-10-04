using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC1 {
    class Program {
        // 入力記号集合
        static readonly char [] LATINU = new[] { 'Ａ', 'Ｂ', 'Ｃ', 'Ｄ', 'Ｅ', 'Ｆ', 'Ｇ', 'Ｈ', 'Ｉ', 'Ｊ', 'Ｋ', 'Ｌ', 'Ｍ', 'Ｎ', 'Ｏ', 'Ｐ', 'Ｑ', 'Ｒ', 'Ｓ', 'Ｔ', 'Ｕ', 'Ｖ', 'Ｗ', 'Ｘ', 'Ｙ', 'Ｚ' };
        static readonly char [] NUMBER = new[] { '０', '１', '２', '３', '４', '５', '６', '７', '８', '９' };
        static readonly char [] HIRAGANA = new[] { 'ぁ', 'あ', 'ぃ', 'い', 'ぅ', 'う', 'ぇ', 'え', 'ぉ', 'お', 'か', 'が', 'き', 'ぎ', 'く', 'ぐ', 'け', 'げ', 'こ', 'ご', 'さ', 'ざ', 'し', 'じ', 'す', 'ず', 'せ', 'ぜ', 'そ', 'ぞ', 'た', 'だ', 'ち', 'ぢ', 'っ', 'つ', 'づ', 'て', 'で', 'と', 'ど', 'な', 'に', 'ぬ', 'ね', 'の', 'は', 'ば', 'ぱ', 'ひ', 'び', 'ぴ', 'ふ', 'ぶ', 'ぷ', 'へ', 'べ', 'ぺ', 'ほ', 'ぼ', 'ぽ', 'ま', 'み', 'む', 'め', 'も', 'ゃ', 'や', 'ゅ', 'ゆ', 'ょ', 'よ', 'ら', 'り', 'る', 'れ', 'ろ', 'ゎ', 'わ', 'ゐ', 'ゑ', 'を', 'ん' };
        static readonly char [] OTHERS = new[] {
            'ヴ', 'ヵ', 'ヶ',// 片仮名のみの文字
            'ー','＝','￥','｀','「','」','；','’','、','。',
            // '／', // => ・ (if US101)
            'ー','＝','￥','｀','「','」','；','’','、','。',
            '！','＠','＃','＄','％','＾','＆','＊','（','）','＿','＋','｜','～','｛','｝','：','”','＜','＞','？',
            '・' //  for JP106 keyboard

        };
        static readonly HashSet<char> _KKCInput = new HashSet<char>(LATINU.Concat(NUMBER).Concat(HIRAGANA).Concat(OTHERS));

        private const string _BT = "BT"; // 文末記号
        private const string _UT = "UT"; // 未知語記号
        private const int _UTMAXLEN = 4; // 未知語の最大長
        
        private static Dictionary<string, List<string>> _Dict;
        private static int _Freq;
        private static Dictionary<string, int> _PairFreq;
        private static double _CharLogP;

        static void Main(string[] args) {
            // check arguments
            if (args.Length < 1) {
                return;

            }
            var _LC = args[0];


            _CharLogP = Math.Log(1 + _KKCInput.Count); // 入力記号0-gram確率の負対数値

            //  言語モデル %PairFreq の生成
            _PairFreq = new Dictionary<string, int>();
            _PairFreq[_BT] = 0;
            _PairFreq[_UT] = 0;

            // 単語/入力記号列 => 頻度
            Console.Error.WriteLine($"Reading {_LC} ... ");
            foreach (var line in System.IO.File.ReadLines(_LC)) {
                // 各組の頻度をインクリメント
                foreach (var item in System.Text.RegularExpressions.Regex.Split(line, @"\s+")) {
                    if (_PairFreq.ContainsKey(item) == false) {
                        _PairFreq[item] = 0;
                    }

                    _PairFreq[item]++;
                }

                // 文末記号の頻度をインクリメント
                _PairFreq[_BT]++;
            }

            Console.Error.WriteLine("done");

            // スムージング

            _Freq = 0; // f() = Σf(word/kkci)
            var keys = _PairFreq.Keys.ToList();
            foreach (var pair in keys) {
                var freq = _PairFreq[pair];
                _Freq += freq;
                if (freq == 1) {
                    // 頻度が１の場合
                    _PairFreq[_UT] += freq; // f(UT) に加算して
                    _PairFreq.Remove(pair); // f(pair) を消去
                }
            }
            // 仮名漢字変換辞書 %Dict の作成

            _Dict = new Dictionary<string, List<string>>(); // KKCI => <Word, KKCI>+
            foreach (var pair in _PairFreq.Keys) {
                // f(∀pair) > 0 に対するループ
                if ((pair == _BT) || (pair == _UT)) {
                    continue;
                }

                ; // 特殊記号は辞書にいれない
                var _kkci = pair.Split('/')[1]; // 入力記号列部分
                if (_Dict.ContainsKey(_kkci) == false) {
                    _Dict[_kkci] = new List<string>();
                } // 必要なら $Dict{$kkci} の初期化

                _Dict[_kkci].Add(pair); // dict(KKCI) に追加
            }


            // main
            // 以下入力記号が 2[byte] からなることを仮定
            // 仮名漢字変換の本体
            string sent;
            while ((sent = Console.ReadLine()) != null) {
                // テストコーパスのループ
                var result = KKConv(sent); // 変換エンジンを呼ぶ
                Console.WriteLine(String.Join(" ", result)); // 変換結果の表示
            }

        }

        class Node {
            public Node prev { get; }
            public string pair { get; }
            public double logP { get; }

            public Node(Node prev, string pair, double logP) {
                this.prev = prev;
                this.pair = pair;
                this.logP = logP;
            }
        }

        // 仮名漢字変換
        // 注意点 : NODE = <PREV, $pair, $logP>;
        // パラメータはグローバル変数として与えられる
        static string[] KKConv(string sent) {
            // (@_ == 1) || die;
            var POSI = sent.Length / 2;                      // 解析位置 $posi の最大値

            var VTable = Enumerable.Range(0, POSI + 2).Select(x => new List<Node>()).ToArray();          // Viterbi Table
            VTable[0].Add(new Node(null, _BT, 0.0));               // DP左端

            for (var posi = 1; posi <= POSI; posi++) {         // 解析位置(辞書引き右端)
                for (var from = 0; from < posi; from++) {      // 開始位置(辞書引き左端)
                    var kkci = sent.Substring(2 * from, 2 * (posi - from));
                    foreach (var pair in _Dict[kkci]) {          // 既知語のループ
                        var best = new Node(null, "NULL", 0.0);          // 最良のノード(の初期値)
                        foreach (var node in VTable[from]) {
                            var logP = node.logP - Math.Log(_PairFreq[pair] / _Freq);
                            if ((best.pair == "NULL") || (logP == best.logP)) {
                                best = new Node(node, pair, logP);
                            }
                        }
                        if (best.pair != "NULL") {     // 最良のノードがある場合
                            VTable[posi].Add(best);    // @best をコピーして参照を記憶
                        }
                    }
                    if ((posi - from) <= _UTMAXLEN) {          // 未知語によるノード生成
                        var best = new Node(null, "NULL", 0);          // 最良のノード(の初期値)
                        foreach (var node in VTable[from]) {
                            var logP = node.logP - Math.Log(_PairFreq[_UT] / _Freq)
                                + (posi - from + 1) * _CharLogP;   // 入力記号と単語末の BT の生成
                            if ((best.pair == "NULL") || (logP < best.logP)) {
                                var pair = kkci + "/" + _UT;
                                best = new Node(node, pair, logP);
                            }
                        }
                        if (best.pair != "NULL") {     // 最良のノードがある場合
                            VTable[posi].Add(best);    // @best をコピーして参照を記憶
                        }
                    }
                }
            }

            {
                var best = new Node(null, "NULL", 0); // 最良のノード(の初期値)
                //print STDERR scalar(@{$VTable[$posi-1]}), "\n";
                foreach (var node in VTable[POSI + 1]) {
                    // $BT への遷移
                    var logP = node.logP - Math.Log(_PairFreq[_BT] / _Freq);
                    if ((best.pair == "NULL") || (logP < best.logP)) {
                        best = new Node(node, _BT, logP);
                    }
                }
                // 逆向きの探索と変換結果の表示
                var result = new List<string>();                                 // 結果 <word, kkci>+
                for (var node = best.prev; node.prev != null; node = node.prev) {                    // 右端のノードからノードを左向きにたどる
                    result.Insert(0, node.pair);                // $pair を配列に記憶していく
                    node = node.prev;
                }

                return result.ToArray();
            }

        }


    }
}
