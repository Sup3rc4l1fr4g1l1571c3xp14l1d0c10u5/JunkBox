using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace KKC1 {
    class Program {

        static void Main(string[] args) {
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
                        var builder = new KKC2DictionaryBuilder(new KKC2DictionaryBuilder.Config());
                        builder.ReadModelFromString(data, (line) => {
                            if (String.IsNullOrWhiteSpace(line)) { return null; }
                            var fields = Regex.Split(line, @"\t+");
                            var freq = int.Parse(fields[1]);
                            var words = fields[0].Split(' ').Select(y => y.Split('/')).Select(y => new KKC2DictionaryBuilder.YomiKaki(y[1], y[0])).ToArray();
                            return new KKC2DictionaryBuilder.NGramCorpus(freq, words);
                        });
                        var dic = builder.Generate();
                        foreach (var kv in dic.transition) {
                            Console.WriteLine($"'{string.Concat(kv.Key)}': {{'{string.Join(", ",kv.Value.Select(x => $"{x.Item1}: {x.Item2:G}"))}'}}");
                        }
                        KKC2 kkc2 = new KKC2(builder.Generate());
                        foreach (var line in System.IO.File.ReadLines("./test/06-pron.txt")) {
                            var tags = kkc2.Convert(line);
                            Console.WriteLine(String.Join(" ", tags));
                        }
                    }
                    {
                        var builder = new KKC2DictionaryBuilder(new KKC2DictionaryBuilder.Config() {
                            BreakToken = "BT",
                        });
                        builder.ReadModelFromFile(@"corpus/2-gram.fwk", Encoding.UTF8, (line) => {
                            if (String.IsNullOrWhiteSpace(line)) { return null; }
                            var fields = Regex.Split(line.Trim(), @" ");
                            var freq = int.Parse(fields[0]);
                            var words = fields.Skip(1).Take(2).Select(y => y.Split('/')).Select(y => new KKC2DictionaryBuilder.YomiKaki(y.Length >= 2 ? y[1] : y[0], y[0])).ToArray();
                            return new KKC2DictionaryBuilder.NGramCorpus(freq, words);
                        });

                        {
                            KKC2 kkc2 = new KKC2(builder.Generate());
                            kkc2.SOF = kkc2.EOS = "BT";
                            foreach (var line in System.IO.File.ReadLines("./test/06-pron.txt")) {
                                var tags = kkc2.Convert(line);
                                Console.WriteLine(String.Join(" ", tags));
                            }
                        }

                        builder.LearnFromWords(@"よ_良 い_い かんじ_感じ".Split(' ').Select(x => x.Split('_')).Select(x => new KKC2DictionaryBuilder.YomiKaki(x[0], x[1])).ToArray());
                        builder.LearnFromWords(@"かんじ_幹事 せき_席".Split(' ').Select(x => x.Split('_')).Select(x => new KKC2DictionaryBuilder.YomiKaki(x[0], x[1])).ToArray());
                        builder.LearnFromWords(@"いち_一 ぎょう_行 の_の かな_かな かんじ_漢字 へんかん_変換 を_を おこな_行 う_う".Split(' ').Select(x => x.Split('_')).Select(x => new KKC2DictionaryBuilder.YomiKaki(x[0], x[1])).ToArray());

                        {
                            KKC2 kkc2 = new KKC2(builder.Generate());
                            kkc2.SOF = kkc2.EOS = "BT";
                            foreach (var line in System.IO.File.ReadLines("./test/06-pron.txt")) {
                                var tags = kkc2.Convert(line);
                                Console.WriteLine(String.Join(" ", tags));
                            }
                        }

                    }
                }

            }

        }
    }


    class Trie<TKey, TValue> : IEnumerable<KeyValuePair<IEnumerable<TKey>, IEnumerable<TValue>>> {
        public Dictionary<TKey, Trie<TKey, TValue>> Child { get; private set; }
        private List<TValue> Values { get; }

        public bool IsTail { get { return Child == null; } }
        public Trie() {
            Child = null;
            Values = new List<TValue>();
        }

        public void Add(IEnumerable<TKey> keys, TValue val) {
            var node = this;
            using (var it = keys.GetEnumerator()) {
                while (it.MoveNext()) {
                    if (it.Current == null) { throw new Exception();}
                    if (node.Child != null && node.Child.ContainsKey(it.Current)) {
                        node = node.Child[it.Current];
                    } else {
                        if (node.Child == null) {
                            node.Child = new Dictionary<TKey, Trie<TKey, TValue>>();
                        }
                        if (node.Child.ContainsKey(it.Current) == false) {
                            node.Child[it.Current] = new Trie<TKey, TValue>();
                        }
                        node = node.Child[it.Current];
                    }
                }
                if (node != null) {
                    node.Values.Add(val);
                }
            }
        }

        public List<TValue> this[IEnumerable<TKey> keys] {
            get { return Find(keys)?.Values; }
        }

        public Trie<TKey, TValue> Find(IEnumerable<TKey> keys) {
            var node = this;
            if (node.IsTail) { return null; }
            using (var it = keys.GetEnumerator()) {
                if (!it.MoveNext()) { return null; }
                for (; ; ) {
                    if (it.Current == null) { throw new Exception();}
                    if (node.Child != null && node.Child.ContainsKey(it.Current)) {
                        node = node.Child[it.Current];
                        if (it.MoveNext() == false) {
                            return node;
                        }
                    } else {
                        return null;
                    }
                }
            }
        }

        public IEnumerator<KeyValuePair<IEnumerable<TKey>, IEnumerable<TValue>>> GetEnumerator() {
            Stack<Dictionary<TKey, Trie<TKey, TValue>>.Enumerator?> stack = new Stack<Dictionary<TKey, Trie<TKey, TValue>>.Enumerator?>();
            List<TKey> keyStack = new List<TKey>();
            stack.Push(this.Child.GetEnumerator());
            while (stack.Count != 0) {
                var vit = stack.Pop();
                if (!vit.HasValue) {
                } else {
                    var it = vit.Value;
                    if (it.MoveNext()) {
                        keyStack.Add(it.Current.Key);
                        if (it.Current.Value.Values.Count > 0) {
                            yield return new KeyValuePair<IEnumerable<TKey>, IEnumerable<TValue>>(keyStack, it.Current.Value.Values);
                        }
                        if (!it.Current.Value.IsTail) {
                            stack.Push(it.Current.Value.Child.GetEnumerator());
                        }
                    }
                    else {
                        stack.Pop();
                        if (keyStack.Count > 0) {
                            keyStack.RemoveAt(keyStack.Count-1);
                        }
                    }
                }

            }

        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }

    public class Bigram {
        public const string SOF = "<s>";
        public const string EOF = "<s>";
        public const char SEPARATOR = ' ';

        public SortedDictionary<string, Tuple<int, int>> lm { get; }

        public static Bigram CreateFromFile(string file, Encoding enc, Func<string, string[]> splitter) {
            using (var sr = new System.IO.StreamReader(file, enc)) {
                return CreateFromStream(sr, splitter);
            }
        }

        public static Bigram CreateFromString(string str, Func<string, string[]> splitter) {
            using (var ms = new System.IO.MemoryStream(Encoding.Unicode.GetBytes(str)))
            using (var sr = new System.IO.StreamReader(ms, Encoding.Unicode)) {
                return CreateFromStream(sr, splitter);
            }
        }

        public static Bigram CreateFromStream(System.IO.StreamReader sr, Func<string, string[]> splitter) {

            // create map counts, context_counts
            var counts = new Dictionary<string, int>();
            var context_counts = new Dictionary<string, int>();

            // for each line in the training_file
            for (var line = ""; (line = sr.ReadLine()) != null;) {
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
            foreach (var kv in counts) {
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


        private Bigram(IDictionary<string, Tuple<int, int>> lm) {
            this.lm = new SortedDictionary<string, Tuple<int, int>>(lm);
        }

        public void SaveToFile(string file, Encoding enc) {
            using (var sr = new System.IO.StreamWriter(file, false, enc)) {
                SaveToStream(sr);
            }
        }
        public void SaveToStream(System.IO.TextWriter tw) {
            foreach (var kv in lm) {
                tw.WriteLine($"{kv.Key}\t{kv.Value.Item1}\t{kv.Value.Item2}");
            }
        }
    }

    public class Hmm {
        public const string SOF = "<s>";
        public const string EOF = "<s>";
        public const char SEPARATOR = ' ';

        public SortedDictionary<string, Tuple<int, int>> transition { get; }
        public SortedDictionary<string, Tuple<int, int>> emit { get; }

        public static Hmm CreateFromFile(string file, Encoding enc, Func<string, string[]> lineSplitter, Func<string, Tuple<string, string>> wordSplitter) {
            using (var sr = new System.IO.StreamReader(file, enc)) {
                return CreateFromStream(sr, lineSplitter, wordSplitter);
            }
        }
        public static Hmm CreateFromString(string str, Func<string, string[]> lineSplitter, Func<string, Tuple<string, string>> wordSplitter) {
            using (var ms = new System.IO.MemoryStream(Encoding.Unicode.GetBytes(str)))
            using (var sr = new System.IO.StreamReader(ms, Encoding.Unicode)) {
                return CreateFromStream(sr, lineSplitter, wordSplitter);
            }
        }

        public static Hmm CreateFromStream(System.IO.StreamReader sr, Func<string, string[]> lineSplitter, Func<string, Tuple<string, string>> wordSplitter) {
            var emit = new Dictionary<string, int>();
            var transition = new Dictionary<string, int>();
            var context = new Dictionary<string, int>();

            for (var line = ""; (line = sr.ReadLine()) != null;) {
                line = line.Trim();
                var previous = SOF;
                context[previous] = context.Get(previous) + 1;
                var wordtags = lineSplitter(line);
                foreach (var wordtag in wordtags) {
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

            var T = transition.ToDictionary((kv) => kv.Key, (kv) => Tuple.Create(kv.Value, context[kv.Key.Split(SEPARATOR)[0]]));

            var E = emit.ToDictionary((kv) => kv.Key, (kv) => Tuple.Create(kv.Value, context[kv.Key.Split(SEPARATOR)[0]]));

            return new Hmm(T, E);
        }


        private Hmm(IDictionary<string, Tuple<int, int>> transition, IDictionary<string, Tuple<int, int>> emit) {
            this.transition = new SortedDictionary<string, Tuple<int, int>>(transition);
            this.emit = new SortedDictionary<string, Tuple<int, int>>(emit);
        }

        public void SaveToFile(string file, Encoding enc) {
            using (var sr = new System.IO.StreamWriter(file, false, enc)) {
                SaveToStream(sr);
            }
        }
        public void SaveToStream(System.IO.TextWriter tw) {
            foreach (var kv in transition) {
                tw.WriteLine($"T\t{kv.Key}\t{kv.Value:G}");
            }
            foreach (var kv in emit) {
                tw.WriteLine($"E\t{kv.Key}\t{kv.Value:G}");
            }
        }
    }

    public class Gradews {
        private int tots;
        private int cors;
        private int totb;
        private int corb;
        private int refw;
        private int testw;
        private int corw;

        public Gradews() {
            reset();
        }
        public void reset() {
            tots = 0;
            cors = 0;
            totb = 0;
            corb = 0;
            refw = 0;
            testw = 0;
            corw = 0;
        }
        public void Comparer(string _ref, string _test) {
            tots++;
            if (_ref == _test) {
                cors++;
            }
            var rarr = _ref.ToCharArray().ToList();
            var tarr = _test.ToCharArray().ToList();
            var rb = new List<bool>();
            var tb = new List<bool>();
            while (rarr.Count > 0 && tarr.Count > 0) {
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
            while (rlens.Any() && tlens.Any()) {
                if (rlast == tlast) {
                    if (rlens[0] == tlens[0]) { corw++; }
                }
                if (rlast <= tlast) {
                    rlast += rlens[0] ? 1 : 0;
                    rlens.RemoveAt(0);
                }
                if (tlast < rlast) {
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

    public class StringOrdinalComparer : IComparer<string> {
        public int Compare(string x, string y) {
            return String.Compare(x, y, StringComparison.Ordinal);
        }
    }

    class KKC2Dictionary {
        /// <summary>
        /// ngram
        /// </summary>

        /// <summary>
        /// 漢字変換後のngram
        /// </summary>
        public SortedDictionary<string, double> emission { get; }

        /// <summary>
        /// かな漢字変換辞書 (かな -> (漢字,確率)[])
        /// </summary>
        public Trie<char, Tuple<string, double>> transition { get; }
        public KKC2Dictionary(Trie<char, Tuple<string, double>> transition, SortedDictionary<string, double> emission) {
            this.transition = transition;
            this.emission = emission;
        }

    }
    class KKC2DictionaryBuilder {
        public struct YomiKaki : IEquatable<YomiKaki> {
            public string Yomi { get; }
            public string Kaki { get; }
            public YomiKaki(string yomi, string kaki) {
                this.Yomi = yomi;
                this.Kaki = kaki;
            }
            public override bool Equals(object obj) {
                if (obj == null) { return false; }
                if (!(obj is YomiKaki)) { return false; }
                var other = (YomiKaki)obj;
                return (other.Yomi == this.Yomi && other.Kaki == this.Kaki);
            }
            public override int GetHashCode() {
                return this.Yomi.GetHashCode() ^ this.Kaki.GetHashCode();
            }
            public override string ToString() {
                return $"{Yomi}/{Kaki}";
            }

            public bool Equals(YomiKaki other) {
                return (other.Yomi == this.Yomi && other.Kaki == this.Kaki);
            }
        }
        public interface INGramCorpus {
            /// <summary>
            /// 頻度
            /// </summary>
            int Frequency { get; }

            /// <summary>
            /// 単語列
            /// </summary>
            YomiKaki[] Words { get; }
        }
        public class NGramCorpus : INGramCorpus {
            /// <summary>
            /// 頻度
            /// </summary>
            public int Frequency { get; }

            /// <summary>
            /// 単語列
            /// </summary>
            public YomiKaki[] Words { get; }

            public NGramCorpus(int frequency, YomiKaki[] words) {
                this.Frequency = frequency;
                this.Words = words;
            }
            public override string ToString() {
                return $"<NGramCorpus Frequency=\"{Frequency}\" Words=\"{string.Join(" ", Words.Select(x => x.ToString()))}\" />";
            }
        }

        public class Config {
            /// <summary>
            /// N-gram の文頭、文末を表す特殊な記号
            /// </summary>
            public string BreakToken = "<s>";
            public string FieldSeparatorPattern = @"\t+";
            public char SEPARATOR = ' ';
        }

        private Config config;
        private Dictionary<string, int> ngram_counts;
        private Dictionary<string, int> ngram_context_counts;
        private Dictionary<string, int> hmm_emit;
        private Dictionary<string, int> hmm_context;

        public KKC2DictionaryBuilder(Config config) {
            this.config = config;
            this.ngram_counts = new Dictionary<string, int>();
            this.ngram_context_counts = new Dictionary<string, int>();

            this.hmm_emit = new Dictionary<string, int>();
            this.hmm_context = new Dictionary<string, int>();
        }

        public void ReadModelFromString(string value, Func<string, INGramCorpus> ParseLine, string lineSplitPattern = @"\r?\n") {
            ReadModel(Regex.Split(value, lineSplitPattern).ToArray(), ParseLine);
        }
        public void ReadModelFromFile(string path, Encoding enc, Func<string, INGramCorpus> ParseLine) {
            ReadModel(System.IO.File.ReadLines(path, enc), ParseLine);
        }

        public void LearnFromWords(YomiKaki[] words) {
            var bt = new YomiKaki(config.BreakToken, config.BreakToken);
            var prev = bt;
            foreach (var word in words) {
                var corpus = new NGramCorpus(1, new[] { prev, word });
                LearnFromCorpus(corpus);
                prev = word;
            }
            {
                var corpus = new NGramCorpus(1, new[] { prev, bt });
                LearnFromCorpus(corpus);
            }
        }

        public void LearnFromCorpus(INGramCorpus corpus) {
            // 「書き」部を取り出す。
            var words = corpus.Words.Select(x => x.Kaki).ToArray();
            {
                // 2gram 
                var freq = corpus.Frequency;
                // 2-gram の分子と分母を加算
                ngram_counts[words[0] + config.SEPARATOR + words[1]] = ngram_counts.Get(words[0] + config.SEPARATOR + words[1]) + freq;
                ngram_context_counts[words[0]] = ngram_context_counts.Get(words[0]) + freq;
                ngram_counts[words[1]] = ngram_counts.Get(words[1]) + freq;
                ngram_context_counts[""] = ngram_context_counts.Get("") + freq;
            }
            {
                // hmm
                var word = corpus.Words.Last();
                if (word.Kaki != config.BreakToken) {
                    var key = word.Kaki + config.SEPARATOR + word.Yomi;
                    hmm_emit[key] = hmm_emit.Get(key) + 1;
                }
                hmm_context[word.Kaki] = hmm_context.Get(word.Kaki) + 1;
            }
        }
        public void ReadModel(IEnumerable<string> path, Func<string, INGramCorpus> ParseLine) {
            foreach (var line in path) {
                // １行読み取る
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                var entry = ParseLine(line);
                LearnFromCorpus(entry);
            }

        }
        public KKC2Dictionary Generate() {
            SortedDictionary<string, Tuple<int, int>> em;
            SortedDictionary<string, Tuple<int, int>> lm;

            {
                // n-gram

                // 1gram/2gramの出現確率を算出してテーブル化
                lm = new SortedDictionary<string, Tuple<int, int>>(new StringOrdinalComparer());

                // for each ngram, count in counts
                foreach (var kv in ngram_counts) {
                    var ngram = kv.Key;
                    var count = kv.Value;
                    // split ngram into an array of words # "wi1 wi" => ["wi1", "wi"]
                    var words = ngram.Split(config.SEPARATOR).ToList();
                    // remove the last element of words
                    words.RemoveAt(words.Count - 1);
                    // join words into context
                    var _context = string.Join("" + config.SEPARATOR, words);
                    // probability = counts [ngram] / context_counts[context]
                    var probability = (double)ngram_counts[ngram] / ngram_context_counts[_context];
                    // save
                    lm[ngram] = Tuple.Create(ngram_counts[ngram], ngram_context_counts[_context]);
                }
            }
            {
                // hmm
                em = new SortedDictionary<string, Tuple<int, int>>(hmm_emit.ToDictionary((kv) => kv.Key, (kv) => Tuple.Create(kv.Value, hmm_context[kv.Key.Split(config.SEPARATOR)[0]])), new StringOrdinalComparer());
            }

            {
                var transition = new Trie<char, Tuple<string, double>>();
                foreach (var kv in em) {
                    var wc = kv.Key.Split(config.SEPARATOR);
                    var context = wc[0];
                    var word = wc[1];
                    var prob = kv.Value;
                    transition.Add(word.ToCharArray(), Tuple.Create(context, (double)prob.Item1 / prob.Item2));
                }


                var emission = new SortedDictionary<string, double>(lm.ToDictionary(x => x.Key, x => (double)x.Value.Item1 / x.Value.Item2), new StringOrdinalComparer());

                return new KKC2Dictionary(transition, emission);

            }
        }
    }

    class KKC2 {
        public string SOF { get; set; } = "<s>";
        public string EOS { get; set; } = "<s>";
        public char SEPARATOR { get; set; } = ' ';
        private double UT { get; set; } = 0.000001;

        private KKC2Dictionary dict;
        public KKC2(KKC2Dictionary dict, char skey = ' ') {

            this.dict = dict;
        }

        public string[] Convert(string line) {
            // forward
            line = line.Trim();
            var l = line.Length;
            var best_score = (l + 2).Times().Select((_) => new Dictionary<string, double>()).ToArray();
            var best_edge = (l + 2).Times().Select((_) => new Dictionary<string, Tuple<int, string>>()).ToArray();
            best_score[0][SOF] = 0;
            best_edge[0][SOF] = null;
            for (var end = 1; end <= l; end++) {
                for (var begin = 0; begin < end; begin++) {
                    var pron = line.Substring(begin, end - begin);
                    var my_tm = dict.transition[pron] ?? new List<Tuple<string, double>>();
                    if (my_tm.Any() == false && pron.Length == 1) {
                        my_tm.Add(Tuple.Create(pron, UT));
                    }
                    foreach (var kv1 in my_tm) {
                        var curr_word = kv1.Item1;
                        var tm_prob = kv1.Item2;
                        foreach (var kv2 in best_score[begin]) {
                            var prev_word = kv2.Key;
                            var prev_score = kv2.Value;
                            var curr_score = prev_score - Math.Log(tm_prob * dict.emission.Get(prev_word + SEPARATOR + curr_word, () => UT));
                            if (best_score[end].ContainsKey(curr_word) == false || curr_score < best_score[end][curr_word]) {
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
                foreach (var kv in best_score[begin]) {
                    var prev_word = kv.Key;
                    var prev_score = kv.Value;
                    var curr_score = prev_score - Math.Log(prev_score * dict.emission.Get(prev_word + SEPARATOR + curr_word, () => UT));
                    if (best_score[end].ContainsKey(curr_word) == false || curr_score < best_score[end][curr_word]) {
                        best_score[end][curr_word] = curr_score;
                        best_edge[end][curr_word] = Tuple.Create(begin, prev_word);
                    }
                }

                // backward
                var tags = new List<string>();
                var next_edge = best_edge[end][curr_word];
                while (next_edge.Item1 != 0 && next_edge.Item2 != SOF) {
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

    public static class Ext {
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, Func<TValue> defaultValue = null) {
            TValue result;
            if (self.TryGetValue(key, out result)) {
                return result;
            } else if (defaultValue != null) {
                return defaultValue();
            } else {
                return default(TValue);
            }
        }
        public static IEnumerable<int> Times(this int self) {
            return Enumerable.Range(0, self);
        }
        public static IList<T> Shuffle<T>(this IList<T> self) {
            var a = self.ToArray();
            Random r = new Random();
            for (var i = a.Length - 1; i >= 1; i--) {
                var j = r.Next(i + 1);
                var tmp = a[j];
                a[j] = a[i];
                a[i] = tmp;
            }
            return a;
        }
    }
}
