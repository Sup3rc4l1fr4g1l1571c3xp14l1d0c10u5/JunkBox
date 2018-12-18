using System;
using System.Collections.Generic;
using System.Linq;

namespace LibPredicate
{
    public static class Extensions
    {
        /// <summary>
        /// パイプライン演算子相当の関数
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="self"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static T2 Apply<T1, T2>(this T1 self, Func<T1, T2> predicate)
        {
            return predicate(self);
        }

        /// <summary>
        /// Tap関数
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static T Tap<T>(this T self, Action<T> predicate)
        {
            predicate(self);
            return self;
        }

        /// <summary>
        /// 正規分布乱数生成
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static double NextNormal(this Random self)
        {
            var r1 = self.NextDouble();
            var r2 = self.NextDouble();
            return (Math.Sqrt(-2.0 * Math.Log(r1)) * Math.Cos(2.0 * Math.PI * r2)) * 0.1;
        }

        /// <summary>
        /// 指定回数繰り返し
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static IEnumerable<int> Times(this int self)
        {
            return Enumerable.Range(0, self);
        }

        /// <summary>
        /// 配列向けForEach
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="predicate"></param>
        public static void ForEach<T>(this T[] self, Action<T> predicate)
        {
            foreach (var item in self)
            {
                predicate(item);
            }
        }

        /// <summary>
        /// 配列向け添え字付きForEach
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="predicate"></param>
        public static void ForEach<T>(this T[] self, Action<T, int> predicate)
        {
            var n = 0;
            foreach (var item in self)
            {
                predicate(item, n++);
            }
        }

        /// <summary>
        /// ランダムサンプリング
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="sampleCount"></param>
        /// <returns></returns>
        public static IEnumerable<T> Sample<T>(this IEnumerable<T> self, int sampleCount)
        {
            var rand = new Random();
            return self.OrderBy((_) => rand.Next()).Take(sampleCount);
        }

        /// <summary>
        /// ランダムサンプリング
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <param name="rand"></param>
        /// <param name="sampleCount"></param>
        /// <returns></returns>
        public static IEnumerable<T> Sample<T>(this IEnumerable<T> self, Random rand, int sampleCount)
        {
            return self.OrderBy((_) => rand.Next()).Take(sampleCount);
        }

        /// <summary>
        /// 二次元配列の行列挙
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        public static IEnumerable<T[]> Rows<T>(this T[,] self)
        {
            var colLength = self.GetLength(0);
            var rowLength = self.GetLength(1);
            for (var col = 0; col < colLength; col++)
            {
                yield return rowLength.Times().Select(row => self[col, row]).ToArray();
            }
        }

        /// <summary>
        /// 二次元配列の列列挙
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        public static IEnumerable<T[]> Cols<T>(this T[,] self)
        {
            var colLength = self.GetLength(0);
            var rowLength = self.GetLength(1);
            for (var row = 0; row < rowLength; row++)
            {
                yield return colLength.Times().Select(col => self[col, row]).ToArray();
            }
        }

        public static int IndexOfMax<T>(this IList<T> self) where T : IComparable
        {
            // 出力層のノードのうち、最大値を持つノードのインデックスを返す
            var max = 0;
            var maxValue = self[0];
            for (var i = 1; i < self.Count; i++)
            {
                if (maxValue.CompareTo(self[i]) < 0)
                {
                    max = i;
                    maxValue = self[i];
                }
            }
            return max;

        }
        public static int IndexOfMax<T1, T2>(this IList<T1> self, Func<T1, T2> func) where T2 : IComparable
        {
            // 出力層のノードのうち、最大値を持つノードのインデックスを返す
            var max = 0;
            var maxValue = func(self[0]);
            for (var i = 1; i < self.Count; i++)
            {
                var value = func(self[i]);
                if (maxValue.CompareTo(value) < 0)
                {
                    max = i;
                    maxValue = value;
                }
            }
            return max;

        }

        public static T2 Using<T1, T2>(this T1 self, Func<T1, T2> func) where T1 : IDisposable
        {
            using (var x = self)
            {
                return func(x);
            }
        }

        public static IEnumerable<IEnumerable<T>> each_cons<T>(this IEnumerable<T> self, int n)
        {
            using (var it = self.GetEnumerator())
            {
                Queue<T> queue = new Queue<T>();
                for (var i = 0; i < n; i++)
                {
                    if (it.MoveNext() == false) { yield break; }
                    queue.Enqueue(it.Current);
                }
                yield return queue;
                while (it.MoveNext())
                {
                    queue.Dequeue();
                    queue.Enqueue(it.Current);
                    yield return queue;
                }
            }
        }

    }
}