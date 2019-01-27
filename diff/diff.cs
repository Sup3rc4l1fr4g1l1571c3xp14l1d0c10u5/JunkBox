using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

namespace diffcs {

    public class Program {
        public static void Main(string[] args) {
            var a = new string[] { "this", "is", "a", "pen", "." };
            var b = new string[] { "is", "this", "a", "pen", "?" };

            var commands = new Diff<string>(a, b).diff();
            var writer = new Diff<string>.UnifiedFormatWriter();
            writer.WriteHeader("from", DateTime.Now, "to", DateTime.Now);
            writer.Write(a, b, commands);
            Console.WriteLine(writer.ToString());

            DiffUnitTest();

            var reader = new Patch<string>.UnifiedFormatReader();
            using(var tr = new System.IO.StreamReader(@"C:\Users\whelp\Desktop\patch.txt")) {
                reader.Parse(tr);
            }
        }
        private static void SeqEq<T>(T[] x, T[] b) {
            if (!x.SequenceEqual(b)) {
                throw new Exception();
            }
        }
        private static void test(string name, Action act) {
            try {
                act();
                Console.WriteLine($"{name}: success.");
            } catch (Exception e) {
                Console.WriteLine($"{name}: failed. {e.ToString()}");
            }
        }
        public static void DiffUnitTest() {
            test(@"empty", () => {
                SeqEq(new Diff<string>(new string[] { }, new string[] { }).diff(), new Diff<string>.DiffResult[] { });

            });

            test(@"""a"" vs ""b""", () => {
                SeqEq(new Diff<string>(new[] { "a" }, new[] { "b" }).diff(), new[] { new Diff<string>.DiffResult(type: Diff<string>.DiffType.removed, value: "a"), new Diff<string>.DiffResult(type: Diff<string>.DiffType.added, value: "b") });
            });

            test(@"""a"" vs ""a""", () => {
                SeqEq(
                    new Diff<string>(new[] { "a" }, new[] { "a" }).diff(),
                    new[] { new Diff<string>.DiffResult(type: Diff<string>.DiffType.common, value: "a") }
                );
            });

            test(@"""a"" vs """"", () => {
                SeqEq(
                    new Diff<string>(new[] { "a" }, new string[] { }).diff(),
                    new[] { new Diff<string>.DiffResult(type: Diff<string>.DiffType.removed, value: "a") }
                );
            });

            test(@""""" vs ""a""", () => {
                SeqEq(
                    new Diff<string>(new string[] { }, new[] { "a" }).diff(),
                    new[] { new Diff<string>.DiffResult(type: Diff<string>.DiffType.added, value: "a") }
                );
            });

            test(@"""a"" vs ""a, b""", () => {
                SeqEq(
                    new Diff<string>(new[] { "a" }, new[] { "a", "b" }).diff(),
                    new[] {
                        new Diff<string>.DiffResult(type: Diff<string>.DiffType.common, value: "a"),
                        new Diff<string>.DiffResult(type: Diff<string>.DiffType.added, value: "b")
                    }
                );
            });


            test(@"""strength"" vs ""string""", () => {
                SeqEq(
                    new Diff<char>(@"strength".ToCharArray(), @"string".ToCharArray()).diff(),
                    new[] {
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.common, value: 's' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.common, value: 't' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.common, value: 'r' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.removed, value: 'e' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.added, value: 'i' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.common, value: 'n' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.common, value: 'g' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.removed, value: 't' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.removed, value: 'h' ),
                    }
                );
            });

            test(@"""strength"" vs """"", () => {
                SeqEq(
                    new Diff<char>(@"strength".ToCharArray(), @"".ToCharArray()).diff(),
                    new[] {
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.removed, value: 's' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.removed, value: 't' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.removed, value: 'r' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.removed, value: 'e' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.removed, value: 'n' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.removed, value: 'g' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.removed, value: 't' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.removed, value: 'h' ),
                    }
                );
            });

            test(@""""" vs ""strength""", () => {
                SeqEq(new Diff<char>(@"".ToCharArray(), @"strength".ToCharArray()).diff(),
                    new[] {
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.added, value: 's' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.added, value: 't' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.added, value: 'r' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.added, value: 'e' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.added, value: 'n' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.added, value: 'g' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.added, value: 't' ),
                        new Diff<char>.DiffResult( type: Diff<char>.DiffType.added, value: 'h' ),
                    }
                );
            });


            test(@"""abc"", ""c"" vs ""abc"", ""bcd"", ""c""", () => {
                SeqEq(new Diff<string>(new[] { "abc", "c" }, new[] { "abc", "bcd", "c" }).diff(),
                    new[] {
                    new Diff<string>.DiffResult( type: Diff<string>.DiffType.common, value: "abc" ),
                    new Diff<string>.DiffResult( type: Diff<string>.DiffType.added, value: "bcd" ),
                    new Diff<string>.DiffResult( type: Diff<string>.DiffType.common, value: "c" ),
                    }
                );
            });
        }
    }

    /// <summary>
    /// Diffクラス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Diff<T> {
        /// <summary>
        /// EditCommand から Unified Format形式 の差分情報 を生成する Writer 
        /// </summary>
        public class UnifiedFormatWriter {
            /// <summary>
            /// 前後に付与するコンテキストサイズ
            /// </summary>
            private int ContextSize { get; }

            /// <summary>
            /// Unified Format形式 の差分情報の生成バッファ
            /// </summary>
            private StringBuilder Builder { get; }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="contextSize">前後に付与するコンテキストサイズ</param>
            public UnifiedFormatWriter(int contextSize = 3) {
                ContextSize = contextSize;
                Builder = new StringBuilder();
            }

            /// <summary>
            /// チャンク範囲
            /// </summary>
            class ChunkRegion {
                public int PrefixContextSize { get; }
                public int PostfixContextSize { get; }
                public int SourceStartIndex { get; }
                public int SourceLength { get; }
                public int DestStartIndex { get; }
                public int DestLength { get; }

                public ChunkRegion(int prefixContextSize, int postfixContextSize, int sourceStartIndex, int sourceLength, int destStartIndex, int destLength) {
                    this.PrefixContextSize = prefixContextSize;
                    this.PostfixContextSize = postfixContextSize;
                    this.SourceStartIndex = sourceStartIndex;
                    this.SourceLength = sourceLength;
                    this.DestStartIndex = destStartIndex;
                    this.DestLength = destLength;
                }
            }

            /// <summary>
            /// チャンク範囲を生成
            /// </summary>
            /// <param name="source">変更前データ列</param>
            /// <param name="dest">変更後データ列</param>
            /// <param name="sourceStartIndex">変更前範囲の開始位置</param>
            /// <param name="sourceLength">変更前範囲の長さ</param>
            /// <param name="destStartIndex">変更後範囲の開始位置</param>
            /// <param name="destLength">変更後範囲の長さ</param>
            /// <param name="contextSize">前後に付与するコンテキストサイズ</param>
            /// <returns>チャンク範囲</returns>
            private static ChunkRegion CreateChunkRegion(T[] source, T[] dest, int sourceStartIndex, int sourceLength, int destStartIndex, int destLength, int contextSize) {
                int prefixContextSize = 0;
                for (var i = 1; i <= contextSize; i++) {
                    if (sourceStartIndex - i >= 0 && destStartIndex - i >= 0 && Object.Equals(source[sourceStartIndex - i], dest[destStartIndex - i])) {
                        prefixContextSize = i;
                    } else {
                        break;
                    }
                }
                int postfixContextSize = 0;
                if ((sourceLength + contextSize) >= source.Length) {
                    postfixContextSize = source.Length - sourceLength;
                } else {
                    postfixContextSize = contextSize;
                }
                return new ChunkRegion(
                    prefixContextSize, postfixContextSize,
                    sourceStartIndex, sourceLength - sourceStartIndex,
                    destStartIndex, destLength - destStartIndex
                );
            }

            /// <summary>
            /// 編集コマンド列を先頭から解釈してチャンク範囲列を生成しながら列挙
            /// </summary>
            /// <param name="editCommands">編集コマンド列</param>
            /// <returns>チャンク範囲列</returns>
            private static IEnumerable<ChunkRegion> GetChunkRegion(T[] source, T[] dest, DiffResult[] editCommands, int contextSize) {
                int sourceStartIndex = 0;
                int destStartIndex = 0;
                int sourceLength = 0;
                int destLength = 0;
                foreach (var editCommand in editCommands) {
                    switch (editCommand.type) {
                        case DiffType.added: {
                                destLength++;
                                break;
                            }
                        case DiffType.removed: {
                                sourceLength++;
                                break;
                            }
                        case DiffType.common: {
                                if (sourceStartIndex != sourceLength || destStartIndex != destLength) {
                                    yield return CreateChunkRegion(source, dest, sourceStartIndex, sourceLength, destStartIndex, destLength, contextSize);
                                }
                                destStartIndex = ++destLength;
                                sourceStartIndex = ++sourceLength;
                                break;
                            }
                    }
                }
                if (sourceStartIndex != sourceLength || destStartIndex != destLength) {
                    yield return CreateChunkRegion(source, dest, sourceStartIndex, sourceLength, destStartIndex, destLength, contextSize);
                }
            }

            /// <summary>
            /// 差分データと編集コマンドを元にUnified Format形式での書き込みを行う。
            /// </summary>
            /// <param name="source"></param>
            /// <param name="dest"></param>
            /// <param name="editCommand"></param>
            public void Write(T[] source, T[] dest, DiffResult[] editCommand) {
                foreach (var chunkRegion in GetChunkRegion(source, dest, editCommand, ContextSize)) {
                    Builder.AppendLine($"@@ -{chunkRegion.SourceStartIndex - chunkRegion.PrefixContextSize + 1},{chunkRegion.SourceLength + chunkRegion.PrefixContextSize + chunkRegion.PostfixContextSize} +{chunkRegion.DestStartIndex - chunkRegion.PrefixContextSize + 1},{chunkRegion.DestLength + chunkRegion.PrefixContextSize + chunkRegion.PostfixContextSize} @@");
                    for (var i = chunkRegion.SourceStartIndex - chunkRegion.PrefixContextSize; i < chunkRegion.SourceStartIndex; i++) {
                        Builder.AppendLine($" {source[i]}");
                    }
                    for (var i = chunkRegion.SourceStartIndex; i < chunkRegion.SourceStartIndex + chunkRegion.SourceLength; i++) {
                        Builder.AppendLine($"-{source[i]}");
                    }
                    for (var i = chunkRegion.DestStartIndex; i < chunkRegion.DestStartIndex + chunkRegion.DestLength; i++) {
                        Builder.AppendLine($"+{dest[i]}");
                    }
                    for (var i = chunkRegion.SourceStartIndex + chunkRegion.SourceLength; i < chunkRegion.SourceStartIndex + chunkRegion.SourceLength + chunkRegion.PostfixContextSize; i++) {
                        Builder.AppendLine($" {source[i]}");
                    }
                }
            }

            /// <summary>
            /// 生成された Unified Format形式 の差分情報を取得
            /// </summary>
            /// <returns></returns>
            public override string ToString() {
                return Builder.ToString();
            }

            /// <summary>
            /// ヘッダ情報を書き込む
            /// </summary>
            /// <param name="fromFile">差分元ファイル名</param>
            /// <param name="fromFileModificationTime">差分元タイムスタンプ</param>
            /// <param name="toFile">差分先ファイル名</param>
            /// <param name="toFileModificationTime">差分先タイムスタンプ</param>
            public void WriteHeader(
                string fromFile, 
                DateTime fromFileModificationTime,
                string toFile,
                DateTime toFileModificationTime
            ) {
                Builder.AppendLine($"--- {fromFile} {fromFileModificationTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff zz00")}");
                Builder.AppendLine($"+++ {toFile} {toFileModificationTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff zz00")}");
            }
        }

        /// <summary>
        /// 編集グラフ上の位置
        /// </summary>
        private class FarthestPoint {
            public int x { get; }
            public int y { get; }

            public FarthestPoint(int x, int y) {
                this.x = x;
                this.y = y;
            }

            public override int GetHashCode() {
                return (x ^ y);
            }

            public override bool Equals(object other) {
                if (Object.ReferenceEquals(other, this)) { return true; }
                if (other == null) { return false; }
                if (!(other is FarthestPoint)) { return false; }
                var that = (FarthestPoint)other;
                return this.x == that.x && this.y == that.y;
            }

            public override string ToString() {
                return $"{{x:x{x}, y:{y}}}";
            }
        }

        /// <summary>
        /// 編集コマンドの運類
        /// </summary>
        public enum DiffType {
            none = 0,
            removed = 1,
            common = 2,
            added = 3
        }

        /// <summary>
        /// 編集コマンド
        /// </summary>
        public class DiffResult {
            public DiffType type { get; }
            public T value { get; }
            public DiffResult(DiffType type, T value) {
                this.type = type;
                this.value = value;
            }
            public override int GetHashCode() {
                return (int)type;
            }
            public override bool Equals(object other) {
                if (Object.ReferenceEquals(other, this)) { return true; }
                if (other == null) { return false; }
                if (!(other is DiffResult)) { return false; }
                var that = (DiffResult)other;
                return this.type == that.type && Object.Equals(this.value, that.value);
            }
            public override string ToString() {
                return $"{{type: {type}, value:{value.ToString()}}}";
            }
        }

        /// <summary>
        /// 編集グラフの探索経路
        /// </summary>
        private class Route {
            public int prev { get; }
            public DiffType type { get; }
            public Route(int prev, DiffType type) {
                this.prev = prev;
                this.type = type;
            }
            public override int GetHashCode() {
                return (int)type;
            }
            public override bool Equals(object other) {
                if (Object.ReferenceEquals(other, this)) { return true; }
                if (other == null) { return false; }
                if (!(other is Route)) { return false; }
                var that = (Route)other;
                return this.prev == that.prev && this.type == that.type;
            }
            public override string ToString() {
                return $"{{type: {type}, prev: {prev}}}";
            }
        }

        private T[] A { get; } // 入力列A(長いほう)
        private T[] B { get; } // 入力列B(短いほう)
        private int M { get; }  // Aの要素数
        private int N { get; }  // Bの要素数
        private bool Swapped { get; }   // 入力列A と 入力列B を入れ替えている場合は真にする 

        private int Offset { get; } // 配列fp読み書き時の下駄オフセット
        private int Delta { get; }  // 入力列の要素数の差の絶対値
        private FarthestPoint[] fp { get; }
        private List<Route> routes { get; }    // 探索経路配列

        public Diff(T[] A, T[] B) {
            this.Swapped = B.Length > A.Length;
            if (this.Swapped) {
                this.A = B;
                this.B = A;
            } else {
                this.A = A;
                this.B = B;
            }
            this.M = this.A.Length;
            this.N = this.B.Length;

            this.Offset = this.N + 1;   // -N-1..M+1にアクセスが発生するのでfpの添え字にN+1の下駄を履かせる
            this.Delta = this.M - this.N;
            this.fp = new FarthestPoint[this.M + this.N + 1 + 2]; // fp[-N-1]とfp[M+1]にアクセスが発生するのでサイズに+2の下駄を履かせる
            for (var i = 0; i < this.fp.Length; i++) {
                this.fp[i] = new FarthestPoint(y: -1, x: 0);
            }

            this.routes = new List<Route>();// 最大経路長は M * N + size
        }
        // 編集グラフの終点から始点までを辿って編集コマンド列を作る
        private DiffResult[] backTrace(FarthestPoint current) {
            var result = new List<DiffResult>();
            var a = this.M - 1;
            var b = this.N - 1;
            var prev = this.routes[current.x].prev;
            var type = this.routes[current.x].type;
            var removedCommand = (this.Swapped ? DiffType.removed : DiffType.added);
            var addedCommand = (this.Swapped ? DiffType.added : DiffType.removed);
            for (; ; ) {
                switch (type) {
                    case DiffType.none: {
                            return result.Reverse<DiffResult>().ToArray();
                        }
                    case DiffType.removed: {
                            result.Add(new DiffResult(type: removedCommand, value: this.B[b]));
                            b -= 1;
                            break;
                        }
                    case DiffType.added: {
                            result.Add(new DiffResult(type: addedCommand, value: this.A[a]));
                            a -= 1;
                            break;
                        }
                    case DiffType.common: {
                            result.Add(new DiffResult(type: DiffType.common, value: this.A[a]));
                            a -= 1;
                            b -= 1;
                            break;
                        }
                }
                var p = prev;
                prev = this.routes[p].prev;

                type = this.routes[p].type;
            }
        }

        private FarthestPoint createFP(int k) {
            var slide = this.fp[k - 1 + this.Offset];
            var down = this.fp[k + 1 + this.Offset];

            if ((slide.y == -1) && (down.y == -1)) {
                // 行き場がない場合はスタート地点へ
                return new FarthestPoint(y: 0, x: 0);
            } else if ((down.y == -1) || k == this.M || (slide.y > down.y + 1)) {
                // 編集操作は追加
                this.routes.Add(new Route(prev: slide.x, type: DiffType.added));
                return new FarthestPoint(y: slide.y, x: this.routes.Count - 1);
            } else {
                // 編集操作は削除
                this.routes.Add(new Route(prev: down.x, type: DiffType.removed));
                return new FarthestPoint(y: down.y + 1, x: this.routes.Count - 1);
            }
        }

        private FarthestPoint snake(int k) {
            if (k < -this.N || this.M < k) {
                return new FarthestPoint(y: -1, x: 0);
            }

            var fp = this.createFP(k);

            // AとBの現在の比較要素が一致している限り、共通要素として読み進める
            // 共通要素の場合は編集グラフを斜めに移動すればいい
            while (fp.y + k < this.M && fp.y < this.N && Object.Equals(this.A[fp.y + k], this.B[fp.y])) {
                this.routes.Add(new Route(prev: fp.x, type: DiffType.common));
                fp = new FarthestPoint(x: this.routes.Count - 1, y: fp.y + 1);
            }
            return fp;
        }

        public DiffResult[] diff() {

            if (this.M == 0 && this.N == 0) {
                // 空要素列同士の差分は空
                return new DiffResult[0];
            } else if (this.N == 0) {
                // 一方が空の場合は追加or削除のみ
                var cmd = this.Swapped ? DiffType.added : DiffType.removed;
                return this.A.Select(a => new DiffResult(type: cmd, value: a)).ToArray();
            }

            for (var i = 0; i < this.fp.Length; i++) {
                this.fp[i] = new FarthestPoint(y: -1, x: 0);
            }
            this.routes.Clear();
            this.routes.Add(new Route(prev: 0, type: 0)); // routes[0]は開始位置

            for (var p = 0; this.fp[this.Delta + this.Offset].y < this.N; p++) {
                for (var k = -p; k < this.Delta; ++k) {
                    this.fp[k + this.Offset] = this.snake(k);
                }
                for (var k = this.Delta + p; k > this.Delta; --k) {
                    this.fp[k + this.Offset] = this.snake(k);
                }
                this.fp[this.Delta + this.Offset] = this.snake(this.Delta);
            }

            return this.backTrace(this.fp[this.Delta + this.Offset]);
        }

    }

    /// <summary>
    /// Patchクラス
    /// </summary>
    public class Patch<T> {
        public class UnifiedFormatReader {

            private Regex regexFromFileCommand = new Regex(@"^--- (?<file>[^\t]+)\t(?<modification_time>.+)$");
            private Regex regexToFileCommand = new Regex(@"^\+\+\+ (?<file>[^\t]+)\t(?<modification_time>.+)$");
            private Regex regexHunk = new Regex(@"^@@ -(?<from_file_line_start>\d+),(?<from_file_line_count>\d+) \+(?<to_file_line_start>\d+),(?<to_file_line_count>\d+) @@$");
            private Regex regexModificationTime = new Regex(@"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2}) (?<hour>\d{2}):(?<min>\d{2}):(?<sec>\d{2})(?<msec>\.\d+)?( (?<tz>[\+\-]?\d{2}00))?$");

            private DateTime ParseModificationTime(string s) {
                var match = regexModificationTime.Match(s);
                if (!match.Success) {throw new Exception(); }
                var year = int.Parse(match.Groups["year"].Value);
                var month = int.Parse(match.Groups["month"].Value);
                var day = int.Parse(match.Groups["day"].Value);
                var hour = int.Parse(match.Groups["hour"].Value);
                var min = int.Parse(match.Groups["min"].Value);
                var sec = int.Parse(match.Groups["sec"].Value);
                var msec = (int)(float.Parse(match.Groups["msec"].Value)*1000);
                var tz = int.Parse(match.Groups["tz"].Value);

                var datetime = new DateTime(
                    year: year,
                    month: month,
                    day: day,
                    hour: hour - tz / 100,
                    minute: min - tz % 100,
                    second: sec,
                    millisecond: msec,
                    kind: DateTimeKind.Utc
                    );
                return datetime.ToLocalTime();
            }

            public void Parse(System.IO.TextReader tr) {
                string currentLine = null;
                Func<bool> ReadLine = () => (currentLine = tr.ReadLine()) != null;

                for (; ; ) {
                    if (ReadLine() == false) { break; }
                    var match1 = regexFromFileCommand.Match(currentLine);
                    if (!match1.Success) { continue; }
                    var fromFile = match1.Groups["file"].Value;
                    var fromModificationTime = match1.Groups["modification_time"].Value;

                    if (ReadLine() == false) { throw new Exception("invalid format"); }
                    var match2 = regexToFileCommand.Match(currentLine);
                    if (!match2.Success) { throw new Exception("invalid format"); }
                    var toFile = match2.Groups["file"].Value;
                    var toModificationTime = match2.Groups["modification_time"].Value;

                    Console.WriteLine($"fromFile: {fromFile}");
                    Console.WriteLine($"fromModificationTime: {fromModificationTime}, {ParseModificationTime(fromModificationTime).ToString()}");
                    Console.WriteLine($"toFile: {toFile}");
                    Console.WriteLine($"toModificationTime: {toModificationTime}, {ParseModificationTime(toModificationTime).ToString()}");

                    while (ReadLine()) {
                        var match3 = regexHunk.Match(currentLine);
                        if (!match3.Success) { continue; }

                        var fromFileLineStart = int.Parse(match3.Groups["from_file_line_start"].Value);
                        var fromFileLineCount = int.Parse(match3.Groups["from_file_line_count"].Value);
                        var toFileLineStart = int.Parse(match3.Groups["to_file_line_start"].Value);
                        var toFileLineCount = int.Parse(match3.Groups["to_file_line_count"].Value);
                        var fromLines = new List<string>();
                        var toLines = new List<string>();
                        while (fromLines.Count < fromFileLineCount && toLines.Count < toFileLineCount) {
                            if (ReadLine() == false || currentLine.Length == 0) { throw new Exception("invalid format"); }
                            switch (currentLine[0]) {
                                case ' ': {
                                        fromLines.Add(currentLine.Substring(1));
                                        toLines.Add(currentLine.Substring(1));
                                        break;
                                    }
                                case '-': {
                                        fromLines.Add(currentLine.Substring(1));
                                        break;
                                    }
                                case '+': {
                                        toLines.Add(currentLine.Substring(1));
                                        break;
                                    }
                            }
                        }
                        if (fromLines.Count != fromFileLineCount) { throw new Exception("invalid format"); }
                        if (toLines.Count != toFileLineCount) { throw new Exception("invalid format"); }

                        Console.WriteLine($"fromFileLineStart: {fromFileLineStart}");
                        Console.WriteLine($"fromFileLineCount: {fromFileLineCount}");
                        Console.WriteLine($"toFileLineStart: {toFileLineStart}");
                        Console.WriteLine($"toFileLineCount: {toFileLineCount}");
                        Console.WriteLine($"fromLines:");
                        fromLines.ForEach(Console.WriteLine);
                        Console.WriteLine($"toLines:");
                        toLines.ForEach(Console.WriteLine);

                    }
                }
            }
        }
    }

}
