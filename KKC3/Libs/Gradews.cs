using System.Collections.Generic;
using System.Linq;

namespace KKC3 {
    public class Gradews {
        /// <summary>
        /// 総データ数
        /// </summary>
        private int Totals;
        
        /// <summary>
        /// 正解数
        /// </summary>
        private int Corrects;
        
        /// <summary>
        /// 境界総数
        /// </summary>
        private int TotalBoundaries;
        
        /// <summary>
        /// 正解した境界総数
        /// </summary>
        private int corb;
        
        /// <summary>
        /// 教師データの単語総数
        /// </summary>
        private int refw;
        
        /// <summary>
        /// テストデータの単語総数
        /// </summary>
        private int testw;

        /// <summary>
        /// 区切り位置の一致総数
        /// </summary>
        private int corw;

        public Gradews() {
            reset();
        }

        public void reset() {
            Totals = 0;
            Corrects = 0;
            TotalBoundaries = 0;
            corb = 0;
            refw = 0;
            testw = 0;
            corw = 0;
        }

        public void Comparer(string _ref, string _test) {
            Totals++;
            if (_ref == _test) {
                Corrects++;
            }  
            // 境界を求める
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

            // 境界数の総合計を更新
            this.TotalBoundaries += rb.Count;
            // 正解の単語境界数を更新
            Enumerable.Range(0, rb.Count).ToList().ForEach((i) => { if (rb[i] == tb[i]) { corb++; } });

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
                if (tlast <= rlast) {
                    tlast += tlens[0] ? 1 : 0;
                    tlens.RemoveAt(0);
                }
            }
        }

        /// <summary>
        /// 正解率
        /// </summary>
        public double SentAccura { get { return (double)Corrects / Totals; } }

        /// <summary>
        /// 適合率
        /// </summary>
        public double WordPrec { get { return (double)corw / testw; } }
        
        /// <summary>
        /// 再現率
        /// </summary>
        public double WordRec { get { return (double)corw / refw; } }
        
        /// <summary>
        /// F-尺度
        /// </summary>
        public double Fmeas { get { return (2.0 * WordPrec * WordRec) / (WordPrec + WordRec); } }
        
        /// <summary>
        /// 区切り位置の正解率
        /// </summary>
        public double BoundAccuracy { get { return (double)corb / TotalBoundaries; } }
    }
}