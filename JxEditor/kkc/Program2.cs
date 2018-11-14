using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ngram {
    class Program {
        public class Node {
            public int index;
            public Tuple<string, string, string> ngram;
            public double score;
            public int prev;
        }

        public static Dictionary<string, int> ToNgramDic(string[] trainers, int n) {
            var dic = new Dictionary<string, int>();
            string head = Enumerable.Repeat("$ ",n-1).Apply(String.Concat);
            string tail = Enumerable.Repeat(" $",n-1).Apply(String.Concat);
            foreach (var train in trainers) {
                foreach (var token in (head + train + tail).Split(' ').EachCons(n).Select(y => string.Join(" ", y))) {
                    dic.Update(token, (k, v) => v + 1, (k) => 1);
                }
            }
            dic[Enumerable.Repeat('$',n-1).Apply(x => String.Join(" ",x))] = 1;
            return dic;
        }

        static void Main(string[] args) {
            var trainers = new[] {
                "きょう は いい てんき です 。",
"きょうと は てんき が わるい 。",
"きょう の てんき は はれ です 。",
                "とうきょう とっきょ きょかきょく の きょかきょく いん の とっきょ は きょか され ない。"
            };

            var unigramDic = ToNgramDic(trainers, 1);
            var unigramCnt = unigramDic.Sum(x => x.Value);
            var bigramDic = ToNgramDic(trainers,2);
            var trigramDic = ToNgramDic(trainers, 3);

            Func<string, string, string, double> P = (prev2, prev1, current) => {
                var triKey = prev2 + " " + prev1 + " " + current;
                var biKey = prev2 + " " + prev1;
                int triCnt, biCnt, uniCnt;
                if (trigramDic.TryGetValue(triKey, out triCnt) && bigramDic.TryGetValue(biKey, out biCnt)) {
                    return (((double)triCnt) / biCnt);
                } else if (bigramDic.TryGetValue(biKey, out biCnt) && unigramDic.TryGetValue(current, out uniCnt)) {
                    return (((double)biCnt) / uniCnt);
                } else if (unigramDic.TryGetValue(current, out uniCnt)) {
                    return (((double)uniCnt) / unigramCnt);
                } else {
                    return 0;
                }
            };


            var input = "とうきょうときょうとのきょうはどっちだ。";

            var matches = new Dictionary<int, Node>();
            matches[0] = new Node() { index = -1, ngram = Tuple.Create("$", "$", "$"), score = 1, prev = -1 };
            //for (var i = 0; i < input.Length; i++) {
            //    matches[i+1] = new Node() { index = i, ngram = Tuple.Create((i-2 < 0) ? "$" : ""+input[i-2], (i-1 < 0) ? "$" : ""+input[i-1], ""+input[i]), score = 0, prev = i };
            //}

            for (var i = 0; i < input.Length; i++) {
                if (matches.ContainsKey(i)) {
                    var match = matches[i];
                    var s = match.index + match.ngram.Item3.Length;
                    foreach (var r in input.ToSubstrings(s, input.Length)) {
                        var sub = input.Substring(r.Item1, r.Item2);
                        var rate = match.score * P(match.ngram.Item2, match.ngram.Item3, sub);
                        if (rate > 0) {
                            if (matches.ContainsKey(r.Item1+r.Item2) == false || matches[r.Item1+r.Item2].score < rate) {
                                matches[r.Item1+r.Item2] = new Node() {index = r.Item1, ngram = Tuple.Create(match.ngram.Item2, match.ngram.Item3, sub), score = rate, prev = r.Item1};
                            }
                        }
                    }
                }
            }

            {
                var ret = new List<string>();
                var ans = matches[input.Length];
                while (ans != null) {
                    ret.Add(ans.ngram.Item3);
                    ans = ans.prev == -1 ? null : matches[ans.prev];
                }

                ret.Reverse();
                Console.WriteLine(String.Join(" ",ret));
             }
        }
    }

    public static class Ext {
        public static IEnumerable<Tuple<int, int>> ToSubstrings(this string self, int start, int end) {
            for (var length = 1; length <= end-start; length++) {
                yield return Tuple.Create(start, length);
            }
        }
        public static Dictionary<TKey, TValue> Update<TKey, TValue>(this Dictionary<TKey, TValue> self, TKey key, Func<TKey, TValue, TValue> update, Func<TKey, TValue> create) {
            TValue v;
            if (self.TryGetValue(key, out v)) {
                self[key] = update(key, v);
            } else {
                self[key] = create(key);
            }
            return self;
        }

        public static TOutput Apply<TInput, TOutput>(this TInput self, Func<TInput, TOutput> pred) {
            return pred(self);
        }
        public static T Tap<T>(this T self, Action<T> action) {
            action(self);
            return self;
        }
        public static IEnumerable<T[]> EachCons<T>(this IList<T> n, int cnt) {
            var e = Math.Max(n.Count - cnt, 0);
            for (var i = 0; i <= e; i++) {
                yield return n.Skip(i).Take(cnt).ToArray();
            }
        }
    }
}
