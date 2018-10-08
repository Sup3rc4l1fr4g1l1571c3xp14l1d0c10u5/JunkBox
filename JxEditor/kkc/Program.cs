using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC1 {
    class Program {

        static void Main(string[] args) {
            // check arguments
            if (args.Length < 1) {
                return;
            }

            var _LC = args[0];

            KKC2.UnigramTrain("06-pronword.txt", Encoding.UTF8, "06-pron.txt", Encoding.UTF8);

            var kkc = new KKC();
            kkc.Load(_LC);
            kkc.Build();

            {
                string sent;
                while ((sent = Console.ReadLine()) != null) {
                    var result = kkc.KKConv(sent); // 変換エンジンを呼ぶ
                    Console.WriteLine(String.Join(" ", result)); // 変換結果の表示
                }
            }

        }
    }
    class KKC2 {
        // https://github.com/tmu-nlp/NLPtutorial2017

        public static void UnigramTrain(string training_file, Encoding enc1, string test_file, Encoding enc2)
        {
            //
            // create unigram
            //
            var uni_counts = new Dictionary<string, int>();
            var uni_total_count = 0;

            foreach (var line in System.IO.File.ReadLines(training_file, enc1))
            {
                // split line into an array of words
                var words = System.Text.RegularExpressions.Regex.Split(line, @"\s+").ToList();
                // append "</s>" to the end of words
                //words.Add("</s>");
                // for each word in words
                foreach (var word in words)
                {
                    uni_counts[word] = uni_counts.ContainsKey(word) ? uni_counts[word] + 1 : 1;
                    uni_total_count += 1;
                }
            }

            var tm = new Dictionary<string, Dictionary<string, double>>();
            foreach (var kv in uni_counts)
            {
                var tmp = kv.Key.Split('_');
                var yomi = tmp[0];
                var kaki = (tmp.Length > 1) ? tmp[1] : tmp[0];
                Dictionary < string, double> pron_words;
                if (tm.TryGetValue(kaki, out pron_words) == false)
                {
                    tm[kaki] = pron_words = new Dictionary<string, double>();
                }
                double word_prob;
                if (pron_words.TryGetValue(yomi, out word_prob) == false)
                {
                    word_prob = 0;
                }
                word_prob += kv.Value;
                pron_words[yomi] = word_prob;
            }

            Console.WriteLine($"tm:");
            foreach (var kv1 in tm) {
                var kaki = kv1.Key;
                foreach (var kv2 in kv1.Value) {
                    var yomi = kv2.Key;
                    var pron = kv2.Value / kv1.Value.Count;
                    Console.WriteLine($"E {kaki} {yomi} {pron:F4}");
                }
            }

            //
            // create bigram
            //

            // create map counts, context_counts
            var bi_counts = new Dictionary<string, int>();
            var bi_context_counts = new Dictionary<string, int>();

            // for each line in the training_file
            foreach (var line in System.IO.File.ReadLines(training_file, enc1))
            {
                // split line into an array of words
                var words = System.Text.RegularExpressions.Regex.Split(line, @"\s+").Select(x => x.Split('_')[1]).ToList();
                // append "</s>" to the end and "<s>" to the beginning of words
                words.Insert(0, "<s>");
                //words.Add("</s>");
                // for each i in 1 to length(words)-1
                for (var i = 1; i <= words.Count - 1; i++)    //注：<s> の後に始まる
                {
                    // # 2-gram の分子と分母を加算
                    var wi1 = words[i - 1];
                    var wi = words[i];
                    // counts["w[i-1] w[i]"] += 1   
                    var key2 = wi1 + " " + wi;
                    bi_counts[key2] = ((bi_counts.ContainsKey(key2) == false) ? 0 : bi_counts[key2]) + 1;
                    // context_counts["w[i-1]"] += 1   
                    bi_context_counts[wi1] = ((bi_context_counts.ContainsKey(wi1) == false) ? 0 : bi_context_counts[wi1]) + 1;
                    // counts["wi"] += 1   
                    bi_counts[wi] = ((bi_counts.ContainsKey(wi) == false) ? 0 : bi_counts[wi]) + 1;
                    // context_counts[""] += 1   
                    bi_context_counts[""] = ((bi_context_counts.ContainsKey("") == false) ? 0 : bi_context_counts[""]) + 1;
                }
            }

            var lm = new Dictionary<string, float>();

            // for each ngram, count in counts
            foreach (var kv in bi_counts)
            {
                var ngram = kv.Key;
                var count = kv.Value;
                // split ngram into an array of words # "wi1 wi" => ["wi1", "wi"]
                var words = System.Text.RegularExpressions.Regex.Split(ngram, @"\s+").ToList();
                // remove the last element of words
                words.RemoveAt(words.Count - 1);
                // join words into context
                var context = string.Join(" ", words);
                // probability = counts [ngram] / context_counts[context]
                var probability = (float)bi_counts[ngram] / bi_context_counts[context];
                // save
                lm[ngram] = probability;
            }

            Console.WriteLine($"lm:");
            foreach (var kv1 in lm) {
                Console.WriteLine($"{kv1.Key} {kv1.Value:F4}");
            }


            //
            // KKC
            //

            // for each line in file
            foreach (var line in System.IO.File.ReadLines(test_file, enc2))
            //for (var line = Console.ReadLine(); line != null ; line = Console.ReadLine())
            {
                string answer = null;

                var best_edge  = new List<Tuple<int, int, string>>();
                var score = new List<Dictionary<string, double>>();
                // forward step
                {
                    best_edge.Clear();
                    score.Clear();
                    best_edge.Add(null);
                    score.Add(new Dictionary<string, double>() { { "<s>", 0 } });
                    for (var end = 1; end <= line.Length; end++)
                    {
                        best_edge.Add(null);
                        score.Add(new Dictionary<string, double>());
                        for (var begin = 0; begin <= end - 1; begin++)
                        {
                            var pron = line.Substring(begin, end - begin);
                            var my_tm = (!tm.ContainsKey(pron)) && pron.Length == 1 ? new Dictionary<string, double>() { { pron, 0 } } : tm[pron];
                            foreach (var kv1 in my_tm)
                            {
                                var curr_word = kv1.Key;
                                var tm_prob = kv1.Value;
                                foreach (var kv2 in score[begin]) {
                                    var prev_word = kv2.Key;
                                    var prev_score = kv2.Value;

                                    var curr_score = prev_score + -Math.Log(tm_prob * lm[prev_word + " " + curr_word]);
                                    if (curr_score < score[end][curr_word])
                                    {
                                        score[end][curr_word] = curr_score;
                                        best_edge[end] = Tuple.Create(begin, end, curr_word);
                                    }
                                }

                            }
                        }
                    }
                }


                // backward step
                {
                    var words = new List<string>();
                    var next_edge = best_edge[best_edge.Count - 1];
                    while (next_edge != null)
                    {
                        var word = line.Substring(next_edge.Item1, next_edge.Item2 - next_edge.Item1);
                        words.Add(word);
                        next_edge = best_edge[next_edge.Item1];
                    }
                    answer = String.Join(" ", words.Reverse<string>().ToArray());
                }
                // put answer
                Console.WriteLine(answer);
            }
        }

    }



    class KKC {
        // 入力記号集合

        /// <summary>
        /// アルファベット
        /// </summary>
        static readonly char[] LATINU = new[] {
            'Ａ', 'Ｂ', 'Ｃ', 'Ｄ', 'Ｅ', 'Ｆ', 'Ｇ', 'Ｈ', 'Ｉ', 'Ｊ', 'Ｋ', 'Ｌ', 'Ｍ',
            'Ｎ', 'Ｏ', 'Ｐ', 'Ｑ', 'Ｒ', 'Ｓ', 'Ｔ', 'Ｕ', 'Ｖ', 'Ｗ', 'Ｘ', 'Ｙ', 'Ｚ'
        };

        /// <summary>
        /// 数値
        /// </summary>
        static readonly char[] NUMBER = new[] {
            '０', '１', '２', '３', '４', '５', '６', '７', '８', '９'
        };

        /// <summary>
        /// ひらがな
        /// </summary>
        static readonly char[] HIRAGANA = new[] {
            'ぁ', 'あ', 'ぃ', 'い', 'ぅ', 'う', 'ぇ', 'え', 'ぉ', 'お',
            'か', 'が', 'き', 'ぎ', 'く', 'ぐ', 'け', 'げ', 'こ', 'ご',
            'さ', 'ざ', 'し', 'じ', 'す', 'ず', 'せ', 'ぜ', 'そ', 'ぞ',
            'た', 'だ', 'ち', 'ぢ', 'っ', 'つ', 'づ', 'て', 'で', 'と', 'ど',
            'な', 'に', 'ぬ', 'ね', 'の',
            'は', 'ば', 'ぱ', 'ひ', 'び', 'ぴ', 'ふ', 'ぶ', 'ぷ', 'へ', 'べ', 'ぺ', 'ほ', 'ぼ', 'ぽ',
            'ま', 'み', 'む', 'め', 'も',
            'ゃ', 'や', 'ゅ', 'ゆ', 'ょ', 'よ',
            'ら', 'り', 'る', 'れ', 'ろ',
            'ゎ', 'わ', 'ゐ', 'ゑ', 'を', 'ん'
        };

        /// <summary>
        /// そのほかの文字種
        /// </summary>
        static readonly char[] OTHERS = new[] {
            'ヴ', 'ヵ', 'ヶ',// 片仮名のみの文字
            // '／', // => ・ (if US101)
            'ー','＝','￥','｀','「','」','；','’','、','。',
            '！','＠','＃','＄','％','＾','＆','＊','（','）',
            '＿','＋','｜','～','｛','｝','：','”','＜','＞','？',
            '・' //  for JP106 keyboard

        };

        /// <summary>
        /// 入力記号
        /// </summary>
        private static readonly HashSet<char> KKCInput = new HashSet<char>(LATINU.Concat(NUMBER).Concat(HIRAGANA).Concat(OTHERS));

        /// <summary>
        /// 文末記号
        /// </summary>
        private const string BT = "BT";

        /// <summary>
        /// 未知語記号
        /// </summary>
        private const string UT = "UT";

        /// <summary>
        /// 未知語の最大長
        /// </summary>
        private const int UTMAXLEN = 4;

        /// <summary>
        /// 仮名漢字変換辞書
        /// </summary>
        private static Dictionary<string, List<string>> Dict;

        /// <summary>
        /// 単語全体の出現頻度
        /// </summary>
        private static int Freq;

        /// <summary>
        /// 言語モデル
        /// </summary>
        private static Dictionary<string, int> PairFreq;

        /// <summary>
        /// 入力記号0-gram確率の負対数値
        /// </summary>
        private static double _CharLogP = Math.Log(1 + KKCInput.Count);


        public KKC() {
            //  言語モデル PairFreq の生成
            PairFreq = new Dictionary<string, int>();
            PairFreq[BT] = 0;
            PairFreq[UT] = 0;

            // 仮名漢字変換辞書 Dict の作成
            Dict = new Dictionary<string, List<string>>();
        }

        public void Load(string _LC) {
            // 単語/入力記号列 => 頻度（出現数）
            Console.Error.WriteLine($"Reading {_LC} ... ");
            foreach (var line in System.IO.File.ReadLines(_LC, Encoding.GetEncoding("EUC-JP"))) {
                // 各組の出現数をインクリメント
                foreach (var item in System.Text.RegularExpressions.Regex.Split(line, @"\s+")) {
                    if (PairFreq.ContainsKey(item) == false) {
                        PairFreq[item] = 0;
                    }
                    PairFreq[item]++;
                }
                // 文末記号の出現数をインクリメント
                PairFreq[BT]++;
            }
        }

        /// <summary>
        /// スムージング
        /// </summary>
        private void Smoothing() {
            Freq = 0; // f() = Σf(word/kkci)
            var keys = PairFreq.Keys.ToList();
            foreach (var pair in keys) {
                var freq = PairFreq[pair];
                Freq += freq;
                if (freq == 1) {
                    // 頻度が１の場合
                    PairFreq[UT] += freq; // f(UT) に加算して
                    PairFreq.Remove(pair); // f(pair) を消去
                }
            }
        }

        /// <summary>
        /// 辞書構築
        /// </summary>
        public void Build() {
            Smoothing();

            Dict.Clear();

            // f(∀pair) > 0 に対するループ
            foreach (var pair in PairFreq.Keys) {
                // 特殊記号は辞書にいれない
                if ((pair == BT) || (pair == UT)) {
                    continue;
                }

                // 入力記号列部分
                var _kkci = pair.Split('/')[1];

                // 必要なら Dict[kkci] の初期化
                if (Dict.ContainsKey(_kkci) == false) {
                    Dict[_kkci] = new List<string>();
                }

                // dict(KKCI) に追加
                Dict[_kkci].Add(pair);
            }
        }

        /// <summary>
        /// ラティス構造のノード
        /// </summary>
        class Node {
            /// <summary>
            /// 前のノード
            /// </summary>
            public Node prev { get; }

            /// <summary>
            /// ノードの値
            /// </summary>
            public string pair { get; }

            /// <summary>
            /// このノードまでの最小コスト
            /// </summary>
            public double logP { get; }

            public Node(Node prev, string pair, double logP) {
                this.prev = prev;
                this.pair = pair;
                this.logP = logP;
            }
        }

        /// <summary>
        /// 仮名漢字変換
        /// </summary>
        /// <param name="sent"></param
        /// <returns></returns

        public string[] KKConv(string sent) {

            // 解析位置 posi の最大値
            var POSI = sent.Length;

            // ビタビアルゴリズムの探索表を作成
            var VTable = Enumerable.Range(0, POSI + 1).Select(x => new List<Node>()).ToArray();          // Viterbi Table

            // DP左端
            VTable[0].Add(new Node(null, BT, 0.0));

            for (var posi = 1; posi <= POSI; posi++) {      // 解析位置(辞書引き右端)
                for (var from = 0; from < posi; from++) {   // 開始位置(辞書引き左端)
                    // 入力の部分文字列
                    var kkci = sent.Substring(from, (posi - from));

                    // 既知語の場合
                    if (Dict.ContainsKey(kkci)) {
                        // 既知語のループ
                        foreach (var pair in Dict[kkci]) {
                            //Console.Error.WriteLine($"{new string(Enumerable.Repeat(' ',2*from).ToArray())}{pair}({PairFreq[pair]})");

                            // 最良のノード(の初期値)
                            var best = new Node(null, null, 0.0);
                            foreach (var node in VTable[from]) {
                                var logP = node.logP - Math.Log((float)PairFreq[pair] / Freq);
                                if ((best.pair == null) || (logP < best.logP)) {
                                    best = new Node(node, pair, logP);
                                    //Console.Error.WriteLine($"{node.pair} -> {pair} ({logP:F} = {node.logP} - log({PairFreq[pair]} / {Freq}))");
                                }
                            }

                            if (best.pair != null) {
                                // 最良のノードがある場合 best を挿入
                                VTable[posi].Add(best);
                            }
                        }
                    }

                    // 未知語によるノード生成
                    if ((posi - from) <= UTMAXLEN) {

                        // 最良のノード(の初期値)
                        var best = new Node(null, null, 0);

                        foreach (var node in VTable[from]) {
                            var logP = node.logP - Math.Log((float)PairFreq[UT] / Freq) + (posi - from + 1) * _CharLogP;   // 入力記号と単語末の BT の生成
                            if ((best.pair == null) || (logP < best.logP)) {
                                var pair = kkci + "/" + UT;
                                best = new Node(node, pair, logP);
                                //Console.Error.WriteLine($"{node.pair} -> {pair} ({logP:F})");
                            }
                        }
                        if (best.pair != null) {
                            // 最良のノードがある場合 best を挿入
                            VTable[posi].Add(best);
                        }
                    }
                }
            }

            {
                // 最良のノード(の初期値)
                var best = new Node(null, null, 0);

                // 文末（BT） への遷移を追加
                foreach (var node in VTable[POSI]) {
                    var logP = node.logP - Math.Log((float)PairFreq[BT] / Freq);
                    if ((best.pair == null) || (logP < best.logP)) {
                        best = new Node(node, BT, logP);
                        //Console.Error.WriteLine($"{node.pair} -> {BT} ({logP:F})");
                    }
                }

                // 逆向きの探索と変換結果の表示

                // 右端のノードからノードを左向きにたどり、pair を結果に記憶していく
                var result = new List<string>();
                for (var node = best; node.prev != null; node = node.prev) {
                    result.Insert(0, node.pair);
                }

                return result.ToArray();
            }
        }
    }
}
