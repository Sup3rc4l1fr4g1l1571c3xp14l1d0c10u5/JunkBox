using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

namespace diff {

    public class Program {
        public static void Main(string[] args) {
            var a = new string [] { "this", "is", "a", "pen", "." };
            var b = new string[] { "is", "this", "a", "pen", "?" };

            var commands = Diff<string>.diff(a, b);
            var writer = new Diff<string>.UnifiedFormatWriter();
            writer.Write(a, b, commands);
            Console.WriteLine(writer.ToString());

        }
    }

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
            private static IEnumerable<ChunkRegion> GetChunkRegion(T[] source, T[] dest, List<EditCommand> editCommands, int contextSize) {
                int sourceStartIndex = 0;
                int destStartIndex = 0;
                int sourceLength = 0;
                int destLength = 0;
                foreach (var editCommand in editCommands) {
                    switch (editCommand.Type) {
                        case EditCommand.CommandType.Insert: {
                                destLength++;
                                break;
                            }
                        case EditCommand.CommandType.Delete: {
                                sourceLength++;
                                break;
                            }
                        case EditCommand.CommandType.Copy: {
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
            public void Write(T[] source, T[] dest, List<EditCommand> editCommand) {
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
        }

        /// <summary>
        /// 編集コマンド
        /// </summary>
        public class EditCommand {
            /// <summary>
            /// 編集コマンドの種別を示す列挙型
            /// </summary>
            public enum CommandType {
                /// <summary>
                /// 挿入
                /// </summary>
                Insert,

                /// <summary>
                /// 削除
                /// </summary>
                Delete,

                /// <summary>
                /// コピー
                /// </summary>
                Copy
            }

            /// <summary>
            /// 編集コマンドの種別
            /// </summary>
            public CommandType Type { get; }

            /// <summary>
            /// 編集コマンドのオペランド
            /// </summary>
            public T Value { get; }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="type">編集コマンドの種別</param>
            /// <param name="value">編集コマンドのオペランド</param>
            public EditCommand(CommandType type, T value) { this.Type = type; this.Value = value; }

            /// <summary>
            /// 文字列化
            /// </summary>
            /// <returns></returns>
            public override string ToString() {
                return $"{Type}: {Value.ToString()}";
            }
        }

        /// <summary>
        /// 編集グラフの探索経路を構成する地点情報
        /// </summary>
        public class Path {
            /// <summary>
            /// ひとつ前の地点を示すパスバッファの位置
            /// </summary>
            public int pre { get; }

            /// <summary>
            /// グラフ上のX位置
            /// </summary>
            public int x { get; }

            /// <summary>
            /// グラフ上のY位置
            /// </summary>
            public int y { get; }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="pre"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            public Path(int pre, int x, int y) { this.pre = pre; this.x = x; this.y = y; }
        }

        /// <summary>
        /// 二つのデータ列の編集コマンドを算出する
        /// </summary>
        /// <param name="source">変更前データ</param>
        /// <param name="dest">変更後データ</param>
        /// <returns></returns>
        public static List<EditCommand> diff(T[] source, T[] dest) {
            if (source.Length <= dest.Length) {
                return Diff<T>.diff_onp(source, dest, EditCommand.CommandType.Delete, EditCommand.CommandType.Insert);
            } else {
                return Diff<T>.diff_onp(dest, source, EditCommand.CommandType.Insert, EditCommand.CommandType.Delete);
            }
        }

        /// <summary>
        /// 編集グラフを斜めに移動できる（＝source,dest共に同じデータである）限りグラフを斜めに移動する
        /// </summary>
        /// <param name="fp"></param>
        /// <param name="lst"></param>
        /// <param name="path"></param>
        /// <param name="k"></param>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <param name="m"></param>
        /// <param name="n"></param>
        private static void snake(Dictionary<int, int> fp, Dictionary<int, int> lst, List<Path> path, int k, T[] source, T[] dest, int m, int n) {
            var y = fp[k - 1] + 1;
            int pre;
            if (y > fp[k + 1]) {
                pre = lst[k - 1];
            } else {
                y = fp[k + 1];
                pre = lst[k + 1];
            }
            var x = y - k;
            while (x < m && y < n && Object.Equals(source[x], dest[y])) {
                x++;
                y++;
            }
            fp[k] = y;
            lst[k] = path.Count;
            path.Add(new Path(pre, x, y));
        }

        private static List<EditCommand> diff_onp(T[] source, T[] dest, EditCommand.CommandType del, EditCommand.CommandType ins) {
            var m = source.Length;
            var n = dest.Length;
            var delta = n - m;
            var fp = new Dictionary<int,int>();
            var lst = new Dictionary<int, int>();
            var path = new List<Path>();
            var result = new List<EditCommand>();
            for (var i = -m - 1; i <= n + 1; i++) { fp[i] = -1; lst[i] = -1; }
            for (var p = 0; p <= m; p++) {
                for (var k = -p; k < delta; k++) {
                    snake(fp, lst, path, k, source, dest, m, n);
                }
                for (var k = delta + p; k > delta; k--) {
                    snake(fp, lst, path, k, source, dest, m, n);
                }
                snake(fp, lst, path, delta, source, dest, m, n);
                if (fp[delta] >= n) {
                    var pt = lst[delta];
                    var list = new List<Path>();
                    while (pt >= 0) {
                        list.Add(path[pt]);
                        pt = path[pt].pre;
                    }
                    var x0 = 0;
                    var y0 = 0;
                    for (var i = list.Count - 1; i >= 0; i--) {
                        var x1 = list[i].x;
                        var y1 = list[i].y;
                        while (x0 < x1 || y0 < y1) {
                            if (y1 - x1 > y0 - x0) {
                                result.Add(new EditCommand(ins, dest[y0++]));
                            } else if (y1 - x1 < y0 - x0) {
                                result.Add(new EditCommand(del, source[x0++]));
                            } else {
                                result.Add(new EditCommand(EditCommand.CommandType.Copy, source[x0++]));
                                y0++;
                            }
                        }
                    }
                    return result;
                }
            }
            throw new Exception();
        }

    }

}
