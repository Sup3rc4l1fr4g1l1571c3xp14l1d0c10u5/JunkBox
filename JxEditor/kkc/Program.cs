using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KKC1
{
    class Program
    {

        static void Main(string[] args)
        {
            {
                {
                    string data = @"
<s>/<s> 一/いち	1
<s>/<s> 幹事/かんじ	1
<s>/<s> 良/よ	1
い/い 感じ/かんじ	1
う/う <s>/<s>	1
かな/かな 漢字/かんじ	1
の/の かな/かな	1
を/を 行/おこな	1
一/いち 行/ぎょう	1
幹事/かんじ 席/せき	1
感じ/かんじ <s>/<s>	1
漢字/かんじ 変換/へんかん	1
行/おこな う/う	1
行/ぎょう の/の	1
席/せき <s>/<s>	1
変換/へんかん を/を	1
良/よ い/い	1
";
                   
                    {
                        var model = KKC2Loader.ReadModelFromString(data, new KKC2Loader.Config() {
                            ParseLine = (line) => {
                                if (String.IsNullOrWhiteSpace(line)) { return null; }
                                    var fields = Regex.Split(line, @"\t+");
                                var freq = int.Parse(fields[1]);
                                var words = fields[0].Split(' ').Select(y => y.Split('/')).Select(y => new KKC2Loader.YomiKaki(y[1], y[0])).ToArray();
                                return new KKC2Loader.NGramCorpus(freq, words);
                            }
                        });
                        KKC2 kkc2 = new KKC2(model.Item1, model.Item2);
                        foreach (var line in System.IO.File.ReadLines("./test/06-pron.txt"))
                        {
                            var tags = kkc2.Convert(line);
                            Console.WriteLine(String.Join(" ", tags));
                        }
                    }
                    {
                        var model = KKC2Loader.ReadModelFromFile(@"corpus/2-gram.fwk", Encoding.UTF8, new KKC2Loader.Config()
                        {
                            BreakToken = "BT",
                            ParseLine = (line) => {
                                if (String.IsNullOrWhiteSpace(line)) { return null; }
                                var fields = Regex.Split(line.Trim(), @" ");
                                var freq = int.Parse(fields[0]);
                                var words = fields.Skip(1).Take(2).Select(y => y.Split('/')).Select(y => new KKC2Loader.YomiKaki(y.Length >= 2 ? y[1] : y[0], y[0])).ToArray();
                                return new KKC2Loader.NGramCorpus(freq, words);
                            }
                        });
                        KKC2 kkc2 = new KKC2(model.Item1, model.Item2);
                        foreach (var line in System.IO.File.ReadLines("./test/06-pron.txt"))
                        {
                            var tags = kkc2.Convert(line);
                            Console.WriteLine(String.Join(" ", tags));
                        }
                    }
                }

            }
            {
                Bigram lm = Bigram.CreateFromFile("test/06-word.txt", Encoding.UTF8, (line) => Regex.Split(line, @"\s+"));
                lm.SaveToFile("test/06-lm.output.txt", Encoding.UTF8);

                Hmm tm = Hmm.CreateFromFile("test/06-pronword.txt", Encoding.UTF8, (line) => Regex.Split(line, @"\s+"), (word) => { var tmp = word.Split('_'); return Tuple.Create(tmp[0], tmp[1]); });
                tm.SaveToFile("test/06-tm.output.txt", Encoding.UTF8);

                {
                    Console.WriteLine($"em:");
                    foreach (var kv in tm.emit)
                    {
                        Console.WriteLine($"{kv.Key}: {kv.Value}");
                    }
                    Console.WriteLine($"lm:");
                    foreach (var kv in lm.lm)
                    {
                        Console.WriteLine($"{kv.Key}: {kv.Value}");
                    }
                }

                KKC2 kkc2 = new KKC2(tm, lm);

                foreach (var line in System.IO.File.ReadLines("./test/06-pron.txt"))
                {
                    var tags = kkc2.Convert(line);
                    Console.WriteLine(String.Join(" ", tags));
                }

            }
            //{
            //    Bigram lm = Bigram.CreateFromFile("data/wiki-ja-train.word", Encoding.UTF8, (line) => Regex.Split(line, @"\s+"));
            //    lm.SaveToFile("data/wiki-ja-train-lm.output.txt", Encoding.UTF8);

            //    Hmm tm = Hmm.CreateFromFile("data/wiki-ja-train.pron_word", Encoding.UTF8, (line) => Regex.Split(line, @"\s+"), (word) => { var tmp = word.Split('_'); return Tuple.Create(tmp[0], tmp[1]); });
            //    tm.SaveToFile("data/wiki-ja-train-tm.output.txt", Encoding.UTF8);

            //    KKC2 kkc2 = new KKC2(tm, lm);

            //    foreach (var line in System.IO.File.ReadLines("./data/wiki-ja-test.pron"))
            //    {
            //        var tags = kkc2.Convert(line);
            //        Console.WriteLine(String.Join(" ", tags));
            //    }

            //}
            {
                // 単語 bi-gram を構築
                Bigram lm = Bigram.CreateFromFile("corpus/L.wordkkci", Encoding.UTF8, (line) => Regex.Split(line, @"\s+").Select(x => x.Split('/')[0]).ToArray());
                lm.SaveToFile("corpus/lm.output.txt", Encoding.UTF8);

                Hmm tm = Hmm.CreateFromFile("corpus/L.wordkkci", Encoding.UTF8, (line) => Regex.Split(line, @"\s+"), (word) => { var tmp = word.Split('/'); return Tuple.Create(tmp[1], tmp[0]); });
                tm.SaveToFile("corpus/tm.output.txt", Encoding.UTF8);

                KKC2 kkc2 = new KKC2(tm, lm);

                foreach (var line in System.IO.File.ReadLines("./corpus/T.kkci"))
                {
                    var tags = kkc2.Convert(line);
                    Console.WriteLine(String.Join(" ", tags));
                }

            }
            //{
            //    var corpus = System.IO.File.ReadAllLines("dict/corpus.txt", Encoding.UTF8).Shuffle();
            //    var teatures = corpus.Take(corpus.Count * 9 / 10).ToArray();
            //    var tests = corpus.Skip(corpus.Count * 1 / 10).ToArray();

            //    Bigram lm = Bigram.CreateFromString(String.Join("\r\n", teatures), (line) => Regex.Split(line, @"\s+").Select(x => x.Split('/')[0]).ToArray());
            //    lm.SaveToFile("dict/lm.output.txt", Encoding.UTF8);

            //    Hmm tm = Hmm.CreateFromString(String.Join("\r\n", teatures), (line) => Regex.Split(line, @"\s+"), (word) => { var tmp = word.Split('/'); return Tuple.Create(tmp[1], tmp[0]); });
            //    tm.SaveToFile("dict/tm.output.txt", Encoding.UTF8);

            //    KKC2 kkc2 = new KKC2(tm, lm);
            //    Gradews checker = new Gradews();

            //    foreach (var line in tests.Select((line) => Regex.Split(line, @"\s+")))
            //    {
            //        var tokens = line.Select(x => x.Split('/')).ToArray();
            //        var tags = kkc2.Convert(String.Concat(tokens.Select(x => x[1])));
            //        Console.WriteLine(String.Join(" ", tags));
            //        checker.Comparer(String.Join(" ", tags), String.Join(" ", tokens.Select(x => x[0])));

            //    }
            //    Console.WriteLine($"SentAccura: {checker.SentAccura:G}");
            //    Console.WriteLine($"WordPrec: {checker.WordPrec:G}");
            //    Console.WriteLine($"WordRec: {checker.WordRec:G}");
            //    Console.WriteLine($"Fmeas: {checker.Fmeas:G}");
            //    Console.WriteLine($"BoundAccuracy: {checker.BoundAccuracy:G}");

            //}
            // check arguments
            if (args.Length < 1)
            {
                return;
            }


            var _LC = args[0];


            var kkc = new KKC();
            kkc.CreateModelFromFile(_LC, Encoding.UTF8);
            kkc.Build();


            kkc.DumpModel();

            {
                string sent;
                foreach (var line in System.IO.File.ReadLines("./corpus/T.kkci"))
                // while ((line = Console.ReadLine()) != null)
                {
                    var result = kkc.KKConv(line); // 変換エンジンを呼ぶ
                    Console.WriteLine(String.Join(" ", result)); // 変換結果の表示
                }
            }

        }
    }

    class KKC
    {
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
        private static Trie<char, string> Dict;

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


        public KKC()
        {
            //  言語モデル PairFreq の生成
            PairFreq = new Dictionary<string, int>();
            PairFreq[BT] = 0;
            PairFreq[UT] = 0;

            // 仮名漢字変換辞書 Dict の作成
            Dict = new Trie<char, string>();
        }

        public void CreateModelFromFile(string _LC, Encoding enc)
        {
            // 言語モデル PairFreq の初期化
            PairFreq.Clear();
            PairFreq[BT] = 0;
            PairFreq[UT] = 0;

            // 単語/入力記号列 => 頻度（出現数）
            foreach (var line in System.IO.File.ReadLines(_LC, enc))
            {
                // 各組の出現数をインクリメント
                foreach (var item in System.Text.RegularExpressions.Regex.Split(line, @"\s+"))
                {
                    if (PairFreq.ContainsKey(item) == false)
                    {
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
        private void Smoothing()
        {
            Freq = 0; // f() = Σf(word/kkci)
            var keys = PairFreq.Keys.ToList();
            foreach (var pair in keys)
            {
                var freq = PairFreq[pair];
                Freq += freq;
                if (freq == 1)
                {
                    // 頻度が１の場合
                    PairFreq[UT] += freq; // f(UT) に加算して
                    PairFreq.Remove(pair); // f(pair) を消去
                }
            }
        }

        /// <summary>
        /// 辞書構築
        /// </summary>
        public void Build()
        {
            Smoothing();

            Dict = new Trie<char, string>();

            // f(∀pair) > 0 に対するループ
            foreach (var pair in PairFreq.Keys)
            {
                // 特殊記号は辞書にいれない
                if ((pair == BT) || (pair == UT))
                {
                    continue;
                }

                // 入力記号列部分
                var _kkci = pair.Split('/')[1];

                // Dict[kkci] に追加
                Dict.Add(_kkci.ToCharArray(), pair);
            }
        }

        /// <summary>
        /// ラティス構造のノード
        /// </summary>
        class Node
        {
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

            public Node(Node prev, string pair, double logP)
            {
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

        public string[] KKConv(string sent)
        {

            // 解析位置 posi の最大値
            var POSI = sent.Length;

            // ビタビアルゴリズムの探索表を作成
            var VTable = Enumerable.Range(0, POSI + 1).Select(x => new List<Node>()).ToArray();

            // DP左端
            VTable[0].Add(new Node(null, BT, 0.0));

            for (var posi = 1; posi <= POSI; posi++)
            {      // 解析位置(辞書引き右端)
                for (var from = 0; from < posi; from++)
                {   // 開始位置(辞書引き左端)
                    // 入力の部分文字列
                    var sublen = posi - from;
                    var kkci = sent.Substring(from, sublen);

                    // 既知語の場合
                    var entries = Dict[kkci];
                    if (entries != null)
                    {
                        // 既知語のループ
                        foreach (var pair in entries)
                        {
                            // 最良のノード(の初期値)
                            var best = new Node(null, null, 0.0);
                            foreach (var node in VTable[from])
                            {
                                var logP = node.logP - Math.Log((float)PairFreq[pair] / Freq);
                                if ((best.pair == null) || (logP < best.logP))
                                {
                                    best = new Node(node, pair, logP);
                                }
                            }

                            if (best.pair != null)
                            {
                                // 最良のノードがある場合、表に best を挿入
                                VTable[posi].Add(best);
                            }
                        }
                    }

                    // 未知語によるノード生成
                    if (sublen <= UTMAXLEN)
                    {

                        // 最良のノード(の初期値)
                        var best = new Node(null, null, 0);

                        foreach (var node in VTable[from])
                        {
                            var logP = node.logP - Math.Log((float)PairFreq[UT] / Freq) + (sublen + 1) * _CharLogP;   // 入力記号と単語末の BT の生成
                            if ((best.pair == null) || (logP < best.logP))
                            {
                                var pair = kkci + "/" + UT;
                                best = new Node(node, pair, logP);
                            }
                        }
                        if (best.pair != null)
                        {
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
                foreach (var node in VTable[POSI])
                {
                    var logP = node.logP - Math.Log((float)PairFreq[BT] / Freq);
                    if ((best.pair == null) || (logP < best.logP))
                    {
                        best = new Node(node, BT, logP);
                    }
                }

                // 逆向きの探索と変換結果の表示

                // 右端のノードからノードを左向きにたどり、pair を結果に記憶していく
                var result = new List<string>();
                for (var node = best; node.prev != null; node = node.prev)
                {
                    result.Insert(0, node.pair);
                }

                return result.ToArray();
            }
        }

        public void DumpModel()
        {
            Console.WriteLine("Dictionary:");
            foreach (var kv in Dict)
            {
                Console.WriteLine($"{new string(kv.Key.ToArray())} => [{String.Join(", ", kv.Value.ToArray())}]");
            }
        }
    }
    class Trie<TKey, TValue> : IEnumerable<KeyValuePair<IEnumerable<TKey>, IEnumerable<TValue>>>
    {
        public Trie<TKey, TValue> Next { get; private set; }
        public Trie<TKey, TValue> Child { get; private set; }
        public TKey Key { get; private set; }



        private List<TValue> Values { get; }

        public bool IsTail { get { return Child == null && Next == null; } }
        public Trie()
        {
            this.Next = null;
            this.Child = null;
            this.Key = default(TKey);
            this.Values = new List<TValue>();
        }

        public void Add(IEnumerable<TKey> keys, TValue val)
        {
            var node = this;
            Trie<TKey, TValue> parent = null;
            using (var it = keys.GetEnumerator())
            {
                while (it.MoveNext())
                {
                    retry:
                    if (node.IsTail)
                    {
                        node.Child = new Trie<TKey, TValue>();
                        node.Next = new Trie<TKey, TValue>();
                        node.Key = it.Current;
                        parent = node;
                        node = node.Child;
                        continue;
                    }
                    else if (node.Key.Equals(it.Current))
                    {
                        parent = node;
                        node = node.Child;
                        continue;
                    }
                    else
                    {
                        parent = node;
                        node = node.Next;
                        goto retry;
                    }
                }
                if (parent != null)
                {
                    parent.Values.Add(val);
                }
            }
        }

        public List<TValue> this[IEnumerable<TKey> keys]
        {
            get { return Find(keys)?.Values; }
        }

        public Trie<TKey, TValue> Find(IEnumerable<TKey> keys)
        {
            var node = this;
            if (node.IsTail) { return null; }
            using (var it = keys.GetEnumerator())
            {
                if (!it.MoveNext()) { return null; }
                for (;;)
                {
                    if (node.Key.Equals(it.Current))
                    {
                        if (it.MoveNext() == false)
                        {
                            return node;
                        }
                        else if (node.Child.IsTail)
                        {
                            return null;
                        }
                        else
                        {
                            node = node.Child;
                            continue;
                        }
                    }
                    else if (node.Next.IsTail)
                    {
                        return null;
                    }
                    else
                    {
                        node = node.Next;
                        continue;
                    }
                }
            }
        }

        public IEnumerator<KeyValuePair<IEnumerable<TKey>, IEnumerable<TValue>>> GetEnumerator()
        {
            Stack<Trie<TKey, TValue>> stack = new Stack<Trie<TKey, TValue>>();
            List<TKey> keystack = new List<TKey>();
            stack.Push(this);
            while (stack.Count != 0)
            {
                var node = stack.Pop();
                if (node == null) { keystack.RemoveAt(keystack.Count - 1); continue; }
                if (node.IsTail) { continue; }
                keystack.Add(node.Key);
                if (node.Values.Count > 0)
                {
                    yield return new KeyValuePair<IEnumerable<TKey>, IEnumerable<TValue>>(keystack, node.Values);
                }
                if (!node.Next.IsTail)
                {
                    stack.Push(node.Next);
                    stack.Push(null);
                }
                if (!node.Child.IsTail)
                {
                    stack.Push(null);
                    stack.Push(node.Child);
                }
            }

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class Bigram
    {
        public const string SOF = "<s>";
        public const string EOF = "<s>";
        public const char SEPARATOR = ' ';

        public SortedDictionary<string, Tuple<int, int>> lm { get; }

        public static Bigram CreateFromFile(string file, Encoding enc, Func<string, string[]> splitter)
        {
            using (var sr = new System.IO.StreamReader(file, enc))
            {
                return CreateFromStream(sr, splitter);
            }
        }

        public static Bigram CreateFromString(string str, Func<string, string[]> splitter)
        {
            using (var ms = new System.IO.MemoryStream(Encoding.Unicode.GetBytes(str)))
            using (var sr = new System.IO.StreamReader(ms, Encoding.Unicode))
            {
                return CreateFromStream(sr, splitter);
            }
        }

        public static Bigram CreateFromStream(System.IO.StreamReader sr, Func<string, string[]> splitter)
        {

            // create map counts, context_counts
            var counts = new Dictionary<string, int>();
            var context_counts = new Dictionary<string, int>();

            // for each line in the training_file
            for (var line = ""; (line = sr.ReadLine()) != null;)
            {
                line = line.Trim();
                // split line into an array of words
                var words = splitter(line).ToList();
                // append "</s>" to the end and "<s>" to the beginning of words
                words.Insert(0, SOF);
                words.Add(EOF);
                // for each i in 1 to length(words)-1
                for (var i = 1; i < words.Count; i++)    //注：<s> の後に始まる
                {
                    // # 2-gram の分子と分母を加算
                    // counts["words[i-1] words[i]"] += 1   
                    counts[words[i - 1] + SEPARATOR + words[i]] = counts.Get(words[i - 1] + SEPARATOR + words[i]) + 1;
                    // context_counts["words[i-1]"] += 1   
                    context_counts[words[i - 1]] = context_counts.Get(words[i - 1]) + 1;
                    // counts["words[i]"] += 1   
                    counts[words[i]] = counts.Get(words[i]) + 1;
                    // context_counts[""] += 1   
                    context_counts[""] = context_counts.Get("") + 1;
                }
            }

            var lm = new Dictionary<string, Tuple<int, int>>();

            // for each ngram, count in counts
            foreach (var kv in counts)
            {
                var ngram = kv.Key;
                var count = kv.Value;
                // split ngram into an array of words # "wi1 wi" => ["wi1", "wi"]
                var words = ngram.Split(SEPARATOR).ToList();
                // remove the last element of words
                words.RemoveAt(words.Count - 1);
                // join words into context
                var context = string.Join("" + SEPARATOR, words);
                // probability = counts [ngram] / context_counts[context]
                var probability = (double)counts[ngram] / context_counts[context];
                // save
                lm[ngram] = Tuple.Create(counts[ngram], context_counts[context]);
            }

            return new Bigram(lm);
        }


        private Bigram(IDictionary<string, Tuple<int, int>> lm)
        {
            this.lm = new SortedDictionary<string, Tuple<int, int>>(lm);
        }

        public void SaveToFile(string file, Encoding enc)
        {
            using (var sr = new System.IO.StreamWriter(file, false, enc))
            {
                SaveToStream(sr);
            }
        }
        public void SaveToStream(System.IO.TextWriter tw)
        {
            foreach (var kv in lm)
            {
                tw.WriteLine($"{kv.Key}\t{kv.Value.Item1}\t{kv.Value.Item2}");
            }
        }
    }

    public class Hmm
    {
        public const string SOF = "<s>";
        public const string EOF = "<s>";
        public const char SEPARATOR = ' ';

        public SortedDictionary<string, Tuple<int,int>> transition { get; }
        public SortedDictionary<string, Tuple<int, int>> emit { get; }

        public static Hmm CreateFromFile(string file, Encoding enc, Func<string, string[]> lineSplitter, Func<string, Tuple<string, string>> wordSplitter)
        {
            using (var sr = new System.IO.StreamReader(file, enc))
            {
                return CreateFromStream(sr, lineSplitter, wordSplitter);
            }
        }
        public static Hmm CreateFromString(string str, Func<string, string[]> lineSplitter, Func<string, Tuple<string, string>> wordSplitter)
        {
            using (var ms = new System.IO.MemoryStream(Encoding.Unicode.GetBytes(str)))
            using (var sr = new System.IO.StreamReader(ms, Encoding.Unicode))
            {
                return CreateFromStream(sr, lineSplitter, wordSplitter);
            }
        }

        public static Hmm CreateFromStream(System.IO.StreamReader sr, Func<string, string[]> lineSplitter, Func<string, Tuple<string, string>> wordSplitter)
        {
            var emit = new Dictionary<string, int>();
            var transition = new Dictionary<string, int>();
            var context = new Dictionary<string, int>();

            for (var line = ""; (line = sr.ReadLine()) != null;)
            {
                line = line.Trim();
                var previous = SOF;
                context[previous] = context.Get(previous) + 1;
                var wordtags = lineSplitter(line);
                foreach (var wordtag in wordtags)
                {
                    var ret = wordSplitter(wordtag);
                    var word = ret.Item1;
                    var tag = ret.Item2;
                    transition[previous + SEPARATOR + tag] = transition.Get(previous + SEPARATOR + tag) + 1;
                    context[tag] = context.Get(tag) + 1;
                    emit[tag + SEPARATOR + word] = emit.Get(tag + SEPARATOR + word) + 1;
                    previous = tag;
                }
                transition[previous + SEPARATOR + EOF] = transition.Get(previous + SEPARATOR + EOF) + 1;
            }

            var T = transition.ToDictionary((kv) => kv.Key, (kv) => Tuple.Create(kv.Value,context[kv.Key.Split(SEPARATOR)[0]]));

            var E = emit.ToDictionary((kv) => kv.Key, (kv) => Tuple.Create(kv.Value,context[kv.Key.Split(SEPARATOR)[0]]));

            return new Hmm(T, E);
        }


        private Hmm(IDictionary<string, Tuple<int, int>> transition, IDictionary<string, Tuple<int, int>> emit)
        {
            this.transition = new SortedDictionary<string, Tuple<int, int>>(transition);
            this.emit = new SortedDictionary<string, Tuple<int, int>>(emit);
        }

        public void SaveToFile(string file, Encoding enc)
        {
            using (var sr = new System.IO.StreamWriter(file, false, enc))
            {
                SaveToStream(sr);
            }
        }
        public void SaveToStream(System.IO.TextWriter tw)
        {
            foreach (var kv in transition)
            {
                tw.WriteLine($"T\t{kv.Key}\t{kv.Value:G}");
            }
            foreach (var kv in emit)
            {
                tw.WriteLine($"E\t{kv.Key}\t{kv.Value:G}");
            }
        }
    }

    public class Gradews
    {
        private int tots;
        private int cors;
        private int totb;
        private int corb;
        private int refw;
        private int testw;
        private int corw;

        public Gradews()
        {
            reset();
        }
        public void reset()
        {
            tots = 0;
            cors = 0;
            totb = 0;
            corb = 0;
            refw = 0;
            testw = 0;
            corw = 0;
        }
        public void Comparer(string _ref, string _test)
        {
            tots++;
            if (_ref == _test)
            {
                cors++;
            }
            var rarr = _ref.ToCharArray().ToList();
            var tarr = _test.ToCharArray().ToList();
            var rb = new List<bool>();
            var tb = new List<bool>();
            while (rarr.Count > 0 && tarr.Count > 0)
            {
                var rs = (rarr[0] == ' ');
                if (rs) { rarr.RemoveAt(0); }
                rb.Add(rs);
                var ts = (tarr[0] == ' ');
                if (ts) { tarr.RemoveAt(0); }
                tb.Add(ts);

                tarr.RemoveAt(0);
                rarr.RemoveAt(0);
            }
            System.Diagnostics.Debug.Assert(rb.Count == tb.Count);

            // total boundaries
            this.totb += rb.Count;
            // correct boundaries
            rb.Count.Times().ToList().ForEach((i) => { if (rb[i] == tb[i]) { corb++; } });

            // find word counts
            var rlens = rb.ToList();
            refw += rlens.Count;
            var tlens = tb.ToList();
            testw += @tlens.Count;
            // print "$ref\n@rlens\n$test\n@tlens\n";
            // find word matches
            var rlast = 0;
            var tlast = 0;
            while (rlens.Any() && tlens.Any())
            {
                if (rlast == tlast)
                {
                    if (rlens[0] == tlens[0]) { corw++; }
                }
                if (rlast <= tlast)
                {
                    rlast += rlens[0] ? 1 : 0;
                    rlens.RemoveAt(0);
                }
                if (tlast < rlast)
                {
                    tlast += tlens[0] ? 1 : 0;
                    tlens.RemoveAt(0);
                }
            }
        }
        public double SentAccura { get { return (double)cors / tots; } }
        public double WordPrec { get { return (double)corw / testw; } }
        public double WordRec { get { return (double)corw / refw; } }
        public double Fmeas { get { return (2.0 * WordPrec * WordRec) / (WordPrec + WordRec); } }
        public double BoundAccuracy { get { return (double)corb / totb; } }
    }

    public class StringComare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return String.Compare(x, y, StringComparison.Ordinal);
        }
    }

    static class KKC2Loader
    {
        public struct YomiKaki
        {
            public string Yomi { get; }
            public string Kaki { get; }
            public YomiKaki(string yomi, string kaki)
            {
                this.Yomi = yomi;
                this.Kaki = kaki;
            }
            public override bool Equals(object obj)
            {
                if (obj == null) { return false; }
                if (!(obj is YomiKaki)) { return false; }
                var other = (YomiKaki)obj;
                return (other.Yomi == this.Yomi && other.Kaki == this.Kaki);
            }
            public override int GetHashCode()
            {
                return this.Yomi.GetHashCode() ^ this.Kaki.GetHashCode();
            }
        }
        public interface INGramCorpus
        {
            /// <summary>
            /// 頻度
            /// </summary>
            int Frequency { get; }

            /// <summary>
            /// 単語列
            /// </summary>
            YomiKaki[] Words { get; }
        }
        public class NGramCorpus : INGramCorpus
        {
            /// <summary>
            /// 頻度
            /// </summary>
            public int Frequency { get; }

            /// <summary>
            /// 単語列
            /// </summary>
            public YomiKaki[] Words { get; }

            public NGramCorpus(int frequency, YomiKaki[] words)
            {
                this.Frequency = frequency;
                this.Words = words;
            }
        }

        public class Config
        {
            /// <summary>
            /// N-gram の文頭、文末を表す特殊な記号
            /// </summary>
            public string BreakToken = "<s>";
            public string FieldSeparatorPattern = @"\t+";
            public char SEPARATOR = ' ';
            public Func<string, INGramCorpus> ParseLine = null;
        }

        public static Tuple<SortedDictionary<string, Tuple<int, int>>, SortedDictionary<string, Tuple<int, int>>> ReadModelFromString(string value, Config config, string lineSplitPattern = @"\r?\n")
        {
            return ReadModel(Regex.Split(value, lineSplitPattern).ToArray(), config);
        }
        public static Tuple<SortedDictionary<string, Tuple<int, int>>, SortedDictionary<string, Tuple<int, int>>> ReadModelFromFile(string path, Encoding enc, Config config)
        {
            return ReadModel(System.IO.File.ReadLines(path, enc), config);
        }

        public static Tuple<SortedDictionary<string, Tuple<int, int>>, SortedDictionary<string, Tuple<int, int>>> ReadModel(IEnumerable<string> path, Config config)
        {
            SortedDictionary<string, Tuple<int, int>> em;
            SortedDictionary<string, Tuple<int, int>> lm;
            {

                var counts = new Dictionary<string, int>();
                var context_counts = new Dictionary<string, int>();

                var emit = new Dictionary<string, int>();
                var context = new Dictionary<string, int>();

                foreach (var line in path)
                {
                    var entry = config.ParseLine(line);
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    var words = entry.Words.Select(x => x.Kaki).ToArray();
                    {
                        // 2gram 
                        var freq = entry.Frequency;
                        // 2-gram の分子と分母を加算
                        counts[words[0] + config.SEPARATOR + words[1]] = counts.Get(words[0] + config.SEPARATOR + words[1][0]) + freq;
                        context_counts[words[0]] = context_counts.Get(words[0]) + freq;
                        counts[words[1]] = counts.Get(words[1]) + freq;
                        context_counts[""] = context_counts.Get("") + freq;
                    }
                    {
                        // hmm
                        var word = entry.Words.Last();
                        if (word.Kaki != config.BreakToken)
                        {
                            var key = word.Kaki + config.SEPARATOR + word.Yomi;
                            emit[key] = emit.Get(key) + 1;
                        }
                        context[word.Kaki] = context.Get(word.Kaki) + 1;
                    }
                }

                {
                    // n-gram
                    lm = new SortedDictionary<string, Tuple<int, int>>( new StringComare());

                    // for each ngram, count in counts
                    foreach (var kv in counts)
                    {
                        var ngram = kv.Key;
                        var count = kv.Value;
                        // split ngram into an array of words # "wi1 wi" => ["wi1", "wi"]
                        var words = ngram.Split(config.SEPARATOR).ToList();
                        // remove the last element of words
                        words.RemoveAt(words.Count - 1);
                        // join words into context
                        var _context = string.Join("" + config.SEPARATOR, words);
                        // probability = counts [ngram] / context_counts[context]
                        var probability = (double)counts[ngram] / context_counts[_context];
                        // save
                        lm[ngram] = Tuple.Create(counts[ngram], context_counts[_context]);
                    }
                }
                {
                    // hmm
                    em = new SortedDictionary<string, Tuple<int, int>>(emit.ToDictionary((kv) => kv.Key, (kv) => Tuple.Create(kv.Value, context[kv.Key.Split(config.SEPARATOR)[0]])), new StringComare());
                }
                if (false) {
                    Console.WriteLine($"em:");
                    foreach (var kv in em)
                    {
                        Console.WriteLine($"{kv.Key}: {kv.Value}");
                    }
                    Console.WriteLine($"lm:");
                    foreach (var kv in lm)
                    {
                        Console.WriteLine($"{kv.Key}: {kv.Value}");
                    }
                }
            }
            return Tuple.Create(em, lm);
        }
    }

    class KKC2
    {
        public const string SOF = "<s>";
        public const string EOS = "<s>";
        public const char SEPARATOR = ' ';
        private const double UT = 0.000001;
        private readonly SortedDictionary<string, double> emission;
        private readonly Dictionary<string, List<Tuple<string, double>>> transition;

        public KKC2(Hmm tm, Bigram lm) : this(tm.emit, lm.lm)
        {
            
        }
        public KKC2(SortedDictionary<string, Tuple<int, int>> em, SortedDictionary<string, Tuple<int, int>> lm, char skey = ' ') {
            this.transition = new Dictionary<string, List<Tuple<string, double>>>();
            foreach (var kv in em)
            {
                var wc = kv.Key.Split(skey);
                var context = wc[0];
                var word = wc[1];
                var prob = kv.Value;
                if (transition.ContainsKey(word) == false)
                {
                    transition[word] = new List<Tuple<string, double>>();
                }
                transition[word].Add(Tuple.Create(context, (double)prob.Item1/ prob.Item2));
            }

            this.emission = new SortedDictionary<string, double>(lm.ToDictionary(x => x.Key, x => (double)x.Value.Item1 / x.Value.Item2), new StringComare ());

        }

        public string[] Convert(string line)
        {
            // forward
            line = line.Trim();
            var l = line.Length;
            var best_score = (l + 2).Times().Select((_) => new Dictionary<string, double>()).ToArray();
            var best_edge = (l + 2).Times().Select((_) => new Dictionary<string, Tuple<int, string>>()).ToArray();
            best_score[0][SOF] = 0;
            best_edge[0][SOF] = null;
            for (var end = 1; end <= l; end++)
            {
                for (var begin = 0; begin < end; begin++)
                {
                    var pron = line.Substring(begin, end - begin);
                    var my_tm = transition.Get(pron, () => new List<Tuple<string, double>>());
                    if (my_tm.Any() == false && pron.Length == 1)
                    {
                        my_tm.Add(Tuple.Create(pron, UT));
                    }
                    foreach (var kv1 in my_tm)
                    {
                        var curr_word = kv1.Item1;
                        var tm_prob = kv1.Item2;
                        foreach (var kv2 in best_score[begin])
                        {
                            var prev_word = kv2.Key;
                            var prev_score = kv2.Value;
                            var curr_score = prev_score - Math.Log(tm_prob * emission.Get(prev_word + SEPARATOR + curr_word, () => UT));
                            if (best_score[end].ContainsKey(curr_word) == false || curr_score < best_score[end][curr_word])
                            {
                                best_score[end][curr_word] = curr_score;
                                best_edge[end][curr_word] = Tuple.Create(begin, prev_word);
                            }

                        }
                    }
                }
            }
            {
                // process EOS
                var curr_word = EOS;
                var end = line.Length + 1;
                var begin = line.Length;
                foreach (var kv in best_score[begin])
                {
                    var prev_word = kv.Key;
                    var prev_score = kv.Value;
                    var curr_score = prev_score - Math.Log(prev_score * emission.Get(prev_word + SEPARATOR + curr_word, () => UT));
                    if (best_score[end].ContainsKey(curr_word) == false || curr_score < best_score[end][curr_word])
                    {
                        best_score[end][curr_word] = curr_score;
                        best_edge[end][curr_word] = Tuple.Create(begin, prev_word);
                    }
                }

                // backward
                var tags = new List<string>();
                var next_edge = best_edge[end][curr_word];
                while (next_edge.Item1 != 0 && next_edge.Item2 != SOF)
                {
                    var position = next_edge.Item1;
                    var tag = next_edge.Item2;
                    tags.Add(tag);
                    next_edge = best_edge[position][tag];
                }
                tags.Reverse();

                return tags.ToArray();
            }
        }
    }

    public static class Ext
    {
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, Func<TValue> defaultValue = null)
        {
            TValue result;
            if (self.TryGetValue(key, out result))
            {
                return result;
            }
            else if (defaultValue != null)
            {
                return defaultValue();
            }
            else
            {
                return default(TValue);
            }
        }
        public static IEnumerable<int> Times(this int self)
        {
            return Enumerable.Range(0, self);
        }
        public static IList<T> Shuffle<T>(this IList<T> self)
        {
            var a = self.ToArray();
            Random r = new Random();
            for (var i = a.Length - 1; i >= 1; i--)
            {
                var j = r.Next(i + 1);
                var tmp = a[j];
                a[j] = a[i];
                a[i] = tmp;
            }
            return a;
        }
    }
}
