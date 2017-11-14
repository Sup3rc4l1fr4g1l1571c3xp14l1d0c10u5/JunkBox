//#define TraceParser

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Parsing {

    /// <summary>
    /// 入力ソースを表現するクラス
    /// </summary>
    public class Source {

        /// <summary>
        /// 読み取った入力を格納するバッファ
        /// </summary>
        private StringBuilder Buffer {
            get;
        }

        /// <summary>
        /// 入力ストリーム
        /// </summary>
        private TextReader Reader {
            get;
        }

        /// <summary>
        /// 入力の終端に到達したら真
        /// </summary>
        public bool Eof {
            get; private set;
        }

        /// <summary>
        /// 入力ソース名
        /// </summary>
        public string Name {
            get;
        }

        public Source(string name, TextReader reader) {
            if (name == null) {
                throw new ArgumentNullException(nameof(name));
            }
            if (reader == null) {
                throw new ArgumentNullException(nameof(reader));
            }
            Name = name;
            Reader = reader;
            Buffer = new StringBuilder();
            Eof = false;
        }

        public int this[int index] {
            get {
                if (Buffer.Length <= index) {
                    if (Eof == false) {
                        var ch = Reader.Read();
                        if (ch == -1) {
                            Eof = true;
                            return -1;
                        }
                        Buffer.Append((char)ch);
                    } else {
                        return -1;
                    }
                }
                return Buffer[index];
            }
        }

        public bool StartsWith(int start, string str) {
            for (var i = 0; i < str.Length; i++) {
                if (this[i + start] != str[i]) {
                    return false;
                }
            }
            return true;
        }

        public string Substring(int start, int length) {
            var sb = new StringBuilder();
            for (var i = 0; i < length; i++) {
                var ch = this[i + start];
                if (this[i + start] == -1) {
                    return null;
                }
                sb.Append((char)ch);
            }
            return sb.ToString();
        }
    }


    /// <summary>
    ///     パーサの位置情報
    /// </summary>
    /// <remarks>
    /// パーサ中で非常に細かく生成・削除・コピーされるため、値型かつ16byte以下のサイズになるようにしてパフォーマンスを稼いでいる。
    /// </remarks>
    public struct Position : IComparer<Position>, IEquatable<Position> {
        /// <summary>
        /// 入力ソース上の位置
        /// </summary>
        public int Index {
            get;
        }

        /// <summary>
        /// 見かけ上のファイル行
        /// </summary>
        public int Row {
            get;
        }

        /// <summary>
        /// 見かけ上のファイル列
        /// </summary>
        public int Column {
            get;
        }

        /// <summary>
        /// 見かけ上のファイル名
        /// </summary>
        public string FileName {
            get {
                return FileNameCache.ElementAtOrDefault(FileNameIndex);
            }
        }

        /// <summary>
        /// ファイル名キャッシュテーブルのID
        /// </summary>
        private ushort FileNameIndex {
            get;
        }

        /// <summary>
        /// 直前の一文字
        /// </summary>
        private char PrevChar {
            get;
        }

        /// <summary>
        /// ファイル名をキャッシュしておく静的領域（ファイル名は最大）
        /// </summary>
        private static readonly List<string> FileNameCache = new List<string>();

        /// <summary>
        /// 空の位置情報を示す唯一の値
        /// </summary>
        public static Position Empty { get; } = new Position(0, "", 1, 1, '\0');

        public int Compare(Position x, Position y) {
            return x.Index.CompareTo(y.Index);
        }

        public bool Equals(Position other) {
            return Index == other.Index;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is Position && Equals((Position)obj);
        }

        public override int GetHashCode() {
            return Index;
        }

        private Position(int index, string filename, int row, int column, char prevChar) {
            Index = index;
            Row = row;
            Column = column;
            PrevChar = prevChar;
            var cachedIndex = FileNameCache.IndexOf(filename);
            if (cachedIndex == -1) {
                FileNameIndex = (ushort)FileNameCache.Count;
                FileNameCache.Add(filename);
            } else {
                FileNameIndex = (ushort)cachedIndex;
            }
        }
        private Position(int index, ushort filenameIndex, int row, int column, char prevChar) {
            Index = index;
            FileNameIndex = filenameIndex;
            Row = row;
            Column = column;
            PrevChar = prevChar;
        }

        public override string ToString() {
            return $"{FileName} ({Row}:{Column})";
        }

        public Position Reposition(string filename, int row, int column) {
            return new Position(Index, filename, row, column, PrevChar);
        }

        public Position Inc(string substr) {
            var row = Row;
            var col = Column;
            var index = Index;
            var prevChar = PrevChar;

            foreach (var t in substr) {
                switch (t) {
                    case '\n':
                        if (prevChar != '\r') {
                            row++;
                            col = 1;
                        }
                        break;
                    case '\r':
                        row++;
                        col = 1;
                        break;
                    default:
                        col += 1;
                        break;
                }
                index += 1;
                prevChar = t;
            }
            return new Position(index, FileNameIndex, row, col, prevChar);
        }

        public Position Inc(char t) {
            var row = Row;
            var col = Column;
            var index = Index;
            var prevChar = PrevChar;

            switch (t) {
                case '\n':
                    if (prevChar != '\r') {
                        row++;
                        col = 1;
                    }
                    break;
                case '\r':
                    row++;
                    col = 1;
                    break;
                default:
                    col += 1;
                    break;
            }
            index += 1;
            prevChar = t;

            return new Position(index, FileNameIndex, row, col, prevChar);
        }

    }

    /// <summary>
    ///     パーサコンビネータの結果
    /// </summary>

    public abstract class Result<T> {
        public sealed class Some : Result<T> {
            public Some(T value, Position position, object state) : base() {
                Position = position;
                Value = value;
                State = state;
            }

            public override bool Success {
                get {
                    return true;
                }
            }
            public override Position Position {
                get;
            }
            public override object State {
                get;
            }

            public override T Value {
                get;
            }

        }

        public sealed class None : Result<T> {
            private None() : base() {
            }

            public override bool Success {
                get {
                    return false;
                }
            }
            public override Position Position {
                get {
                    return Position.Empty;
                }
            }

            public override T Value {
                get {
                    return default(T);
                }
            }

            public override object State {
                get {
                    return null;
                }
            }

            public static None Instance { get; } = new None();
        }

        /// <summary>
        ///     パーサがマッチした場合は真、それ以外の場合は偽となる
        /// </summary>
        public abstract bool Success {
            get;
        }

        /// <summary>
        ///     パーサがマッチした際は次の読み取り位置を示す
        /// </summary>
        public abstract Position Position {
            get;
        }

        /// <summary>
        ///     パーサがマッチした際の結果を示す
        /// </summary>
        public abstract T Value {
            get;
        }

        /// <summary>
        ///     パーサの状態を示すオブジェクト
        /// </summary>
        public abstract object State {
            get;
        }

        /// <summary>
        ///     コンストラクタ
        /// </summary>
        protected Result() {
        }

        public static Result<T> Accept(T value, Position position, object state) {
            return new Some(value, position, state);
        }

        public static Result<T> Reject() {
            return None.Instance;
        }

    }

    /// <summary>
    /// パーサが使うグローバルなコンテキスト
    /// </summary>
    public class Context {
        /// <summary>
        /// 入力ソース
        /// </summary>
        public Source source {
            get;
        }

        /// <summary>
        /// 最も読み進めることができた失敗位置
        /// </summary>
        public Position failedPosition {
            get; private set;
        }

        public Context(Source source) {
            this.source = source;
            failedPosition = Position.Empty.Reposition(source.Name, 1, 1);
        }

        /// <summary>
        /// 失敗位置を記録
        /// </summary>
        /// <param name="position"></param>
        public void MarkFailed(Position position) {
            if (position.Index > failedPosition.Index) {
                failedPosition = position;
            }
        }
    }

    /// <summary>
    ///     パーサを表すデリゲート 
    /// </summary>
    /// <typeparam name="T">パース結果型</typeparam>
    /// <param name="context">パーサのコンテキスト</param>
    /// <param name="position">現在の位置</param>
    /// <param name="state"></param>
    /// <returns></returns>
    public delegate Result<T> Parser<T>(Context context, Position position, object state);

    /// <summary>
    /// パーサに合致する範囲を示す型。字句解析で使うとメモリの無駄が減る。
    /// </summary>
    public struct MatchRegion {
        public Position Start {
            get;
        }
        public Position End {
            get;
        }
        public MatchRegion(Position start, Position end) {
            Start = start;
            End = end;
        }
    }

    /// <summary>
    ///     パーサコンビネータ
    /// </summary>
    public static class Combinator {

        /// <summary>
        ///     常に成功/失敗する空のパーサ
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static Parser<T> Empty<T>(bool success) {
            if (success) {
                return (context, position, state) => Result<T>.Accept(default(T), position, state);
            } else {
                return (context, position, state) => {
                    context.MarkFailed(position);
                    return Result<T>.Reject();
                };
            }
        }

        /// <summary>
        ///     単純な文字列を受理するパーサを生成
        /// </summary>
        /// <param name="str">受理する文字列</param>
        /// <returns>パーサ</returns>
        public static Parser<string> Token(string str) {
            if (str == null) {
                throw new ArgumentNullException(nameof(str));
            }
            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                if (context.source.StartsWith(position.Index, str)) {
                    return Result<string>.Accept(str, position.Inc(str), state);
                } else {
                    context.MarkFailed(position);
                    return Result<string>.Reject();
                }
            };
        }

        public static Parser<MatchRegion> TokenRegion(string str) {
            if (str == null) {
                throw new ArgumentNullException(nameof(str));
            }
            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                if (context.source.StartsWith(position.Index, str)) {
                    var newPosition = position.Inc(str);
                    return Result<MatchRegion>.Accept(new MatchRegion(position, newPosition), newPosition, state);
                } else {
                    context.MarkFailed(position);
                    return Result<MatchRegion>.Reject();
                }
            };
        }

        /// <summary>
        ///     パーサparserが受理する文字列の繰り返しを受理できるパーサを生成する
        /// </summary>
        /// <param name="parser">パーサ</param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>パーサ</returns>
        public static Parser<IReadOnlyList<T>> Many<T>(this Parser<T> parser, int min = -1, int max = -1) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (min >= 0 && max >= 0 && min > max) {
                throw new ArgumentException("min < max");
            }

            return ((context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }

                var result = new List<T>();

                var currentPosition = position;
                var currentState = state;

                for (;;) {
                    var parsed = parser(context, currentPosition, currentState);

                    if (!parsed.Success) {
                        // 読み取りに失敗
                        break;
                    } else {
                        // 読み取りに成功
                        result.Add(parsed.Value); // 結果を格納
                        currentPosition = parsed.Position; // 読み取り位置を更新する
                        currentState = parsed.State;
                    }
                }

                if (min >= 0 && result.Count < min || max >= 0 && result.Count > max) {
                    return Result<IReadOnlyList<T>>.Reject();
                } else {
                    return Result<IReadOnlyList<T>>.Accept(result, currentPosition, currentState);
                }
            });
        }

        /// <summary>
        ///     パーサparserが受理する文字列の繰り返しを受理できるパーサを生成する
        /// </summary>
        /// <param name="parser">パーサ</param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>パーサ</returns>
        public static Parser<MatchRegion> ManyRegion(this Parser<MatchRegion> parser, int min = -1, int max = -1) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (min >= 0 && max >= 0 && min > max) {
                throw new ArgumentException("min < max");
            }

            return ((context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }

                var start = position;
                var resultCount = 0;

                var currentPosition = position;
                var currentState = state;

                for (;;) {
                    var parsed = parser(context, currentPosition, currentState);

                    if (!parsed.Success) {
                        // 読み取りに失敗
                        break;
                    } else {
                        // 読み取りに成功
                        resultCount++; // 結果を格納
                        currentPosition = parsed.Position; // 読み取り位置を更新する
                        currentState = parsed.State;
                    }
                }

                if (min >= 0 && resultCount < min || max >= 0 && resultCount > max) {
                    return Result<MatchRegion>.Reject();
                } else {
                    return Result<MatchRegion>.Accept(new MatchRegion(start, currentPosition), currentPosition, currentState);
                }
            });
        }

        /// <summary>
        ///     パーサ列parsersを先頭から順に受理するか調べ、最初に受理したパーサの結果を返すパーサを生成する
        /// </summary>
        /// <param name="parsers"></param>
        /// <returns></returns>
        public static Parser<T> Choice<T>(params Parser<T>[] parsers) {
            if (parsers == null) {
                throw new ArgumentNullException(nameof(parsers));
            }
            if (parsers.Any(x => x == null)) {
                throw new ArgumentNullException(nameof(parsers));
            }

            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }

                foreach (var parser in parsers) {
                    var parsed = parser(context, position, state);
                    if (parsed.Success) {
                        return Result<T>.Accept(parsed.Value, parsed.Position, parsed.State);
                    }
                }

                return Result<T>.Reject();
            };
        }

        /// <summary>
        ///     パーサ列parsersを連結したパーサを生成する
        /// </summary>
        /// <param name="parsers"></param>
        /// <returns></returns>
        public static Parser<IReadOnlyList<T>> Seq<T>(params Parser<T>[] parsers) {
            if (parsers == null) {
                throw new ArgumentNullException(nameof(parsers));
            }
            if (parsers.Any(x => x == null)) {
                throw new ArgumentNullException(nameof(parsers));
            }

            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var result = new List<T>();

                var currentPosition = position;
                var currentState = state;

                foreach (var parser in parsers) {
                    var parsed = parser(context, currentPosition, currentState);

                    if (parsed.Success) {
                        result.Add(parsed.Value);
                        currentPosition = parsed.Position;
                        currentState = parsed.State;
                    } else {
                        return Result<IReadOnlyList<T>>.Reject();
                    }
                }
                return Result<IReadOnlyList<T>>.Accept(result, currentPosition, currentState);
            };
        }

        public static Parser<MatchRegion> Seq(params Parser<MatchRegion>[] parsers) {
            if (parsers == null) {
                throw new ArgumentNullException(nameof(parsers));
            }
            if (parsers.Any(x => x == null)) {
                throw new ArgumentNullException(nameof(parsers));
            }

            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var currentPosition = position;
                var currentState = state;

                foreach (var parser in parsers) {
                    var parsed = parser(context, currentPosition, currentState);

                    if (parsed.Success) {
                        currentPosition = parsed.Position;
                        currentState = parsed.State;
                    } else {
                        return Result<MatchRegion>.Reject();
                    }
                }
                return Result<MatchRegion>.Accept(new MatchRegion(position, currentPosition), currentPosition, currentState);
            };
        }

        /// <summary>
        ///     任意の一文字に一致するパーサを生成する
        /// </summary>
        /// <returns></returns>
        public static Parser<char> AnyChar() {
            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var ch = context.source[position.Index];
                if (ch == -1) {
                    context.MarkFailed(position);
                    return Result<char>.Reject();
                } else {
                    return Result<char>.Accept((char)ch, position.Inc((char)ch), state);
                }
            };
        }
        public static Parser<MatchRegion> AnyCharRegion() {
            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var ch = context.source[position.Index];
                if (ch == -1) {
                    context.MarkFailed(position);
                    return Result<MatchRegion>.Reject();
                } else {
                    var newPosition = position.Inc((char)ch);
                    return Result<MatchRegion>.Accept(new MatchRegion(position, newPosition), newPosition, state);
                }
            };
        }

        /// <summary>
        ///     str中の一文字に一致するパーサを生成する
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Parser<char> AnyChar(string str) {
            if (str == null) {
                throw new ArgumentNullException(nameof(str));
            }
            var dict = new HashSet<char>(str.ToCharArray());

            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var ch = context.source[position.Index];
                if (ch != -1 && dict.Contains((char)ch)) {
                    return Result<char>.Accept((char)ch, position.Inc((char)ch), state);
                } else {
                    context.MarkFailed(position);
                    return Result<char>.Reject();
                }
            };
        }

        public static Parser<MatchRegion> AnyCharRegion(string str) {
            if (str == null) {
                throw new ArgumentNullException(nameof(str));
            }
            var dict = new HashSet<char>(str.ToCharArray());

            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var ch = context.source[position.Index];
                if (ch != -1 && dict.Contains((char)ch)) {
                    var newPosition = position.Inc((char)ch);
                    return Result<MatchRegion>.Accept(new MatchRegion(position, newPosition), newPosition, state);
                } else {
                    context.MarkFailed(position);
                    return Result<MatchRegion>.Reject();
                }
            };
        }

        public static Parser<MatchRegion> AnyCharsRegion(string str, int min = -1, int max = -1) {
            if (str == null) {
                throw new ArgumentNullException(nameof(str));
            }
            if (min >= 0 && max >= 0 && min > max) {
                throw new ArgumentException("min < max");
            }
            var dict = new HashSet<char>(str.ToCharArray());

            return ((context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }

                var result = new StringBuilder();

                var currentPosition = position.Index;

                for (;;) {
                    var ch = context.source[currentPosition];
                    if (ch == -1 || !dict.Contains((char)ch)) {
                        // 読み取りに失敗
                        break;
                    } else {
                        // 読み取りに成功
                        currentPosition++;
                        result.Append((char)ch); // 結果を格納
                    }
                }

                if (min >= 0 && result.Length < min || max >= 0 && result.Length > max) {
                    return Result<MatchRegion>.Reject();
                } else {
                    var newPosition = position.Inc(result.ToString());

                    return Result<MatchRegion>.Accept(new MatchRegion(position, newPosition), newPosition, state);
                }
            });
        }

        /// <summary>
        ///     述語 pred が真になる一文字に一致するパーサを生成する
        /// </summary>
        /// <param name="pred"></param>
        /// <returns></returns>
        public static Parser<char> AnyChar(Func<char, bool> pred) {
            if (pred == null) {
                throw new ArgumentNullException(nameof(pred));
            }

            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var ch = context.source[position.Index];
                if (ch != -1 && pred((char)ch)) {
                    return Result<char>.Accept((char)ch, position.Inc((char)ch), state);
                } else {
                    context.MarkFailed(position);
                    return Result<char>.Reject();
                }
            };
        }

        public static Parser<MatchRegion> AnyCharRegion(Func<char, bool> pred) {
            if (pred == null) {
                throw new ArgumentNullException(nameof(pred));
            }

            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var ch = context.source[position.Index];
                if (ch != -1 && pred((char)ch)) {
                    var newPosition = position.Inc((char)ch);
                    return Result<MatchRegion>.Accept(new MatchRegion(position, newPosition), newPosition, state);
                } else {
                    context.MarkFailed(position);
                    return Result<MatchRegion>.Reject();
                }
            };
        }

        public static Parser<MatchRegion> AnyCharsRegion(Func<char, bool> pred, int min = -1, int max = -1) {
            if (pred == null) {
                throw new ArgumentNullException(nameof(pred));
            }
            if (min >= 0 && max >= 0 && min > max) {
                throw new ArgumentException("min < max");
            }

            return ((context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }

                var result = new StringBuilder();

                var currentPosition = position.Index;

                for (;;) {
                    var ch = context.source[currentPosition];
                    if (ch == -1 || !pred((char)ch)) {
                        // 読み取りに失敗
                        break;
                    } else {
                        // 読み取りに成功
                        currentPosition++;
                        result.Append((char)ch); // 結果を格納
                    }
                }

                if (min >= 0 && result.Length < min || max >= 0 && result.Length > max) {
                    return Result<MatchRegion>.Reject();
                } else {
                    var newPosition = position.Inc(result.ToString());

                    return Result<MatchRegion>.Accept(new MatchRegion(position, newPosition), newPosition, state);
                }
            });
        }

        /// <summary>
        ///     EOFにマッチするパーサ
        /// </summary>
        /// <returns></returns>
        public static Parser<char> EoF() {
            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var ch = context.source[position.Index];
                if (ch != -1) {
                    context.MarkFailed(position);
                    return Result<char>.Reject();
                } else {
                    return Result<char>.Accept((char)ch, position, state);
                }
            };
        }

        /// <summary>
        /// メモ化用のキー
        /// </summary>
        private struct MemoizeKey : IEquatable<MemoizeKey> {
            private readonly int _position;
            private readonly object _state;

            public MemoizeKey(int position, object state) {
                _position = position;
                _state = state;
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj))
                    return false;
                return obj is MemoizeKey && Equals((MemoizeKey)obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return (_position * 397) ^ (_state != null ? _state.GetHashCode() : 0);
                }
            }

            public bool Equals(MemoizeKey other) {
                return _position == other._position && Equals(_state, other._state);
            }
        }

        /// <summary>
        ///     パーサをメモ化するパーサを生成
        /// </summary>
        /// <param name="parser">メモ化するパーサ</param>
        /// <returns></returns>
        public static Parser<T> Memoize<T>(this Parser<T> parser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            Dictionary<MemoizeKey, Result<T>> memoization = new Dictionary<MemoizeKey, Result<T>>();

            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }

                var key = new MemoizeKey(position.Index, state);
                Result<T> parsed;
                if (memoization.TryGetValue(key, out parsed)) {
                    return parsed;
                }

                parsed = parser(context, position, state);
                memoization.Add(key, parsed);
                return parsed;
            };
        }

        public static Dictionary<string, List<Tuple<Position, bool>>> TraceInfo = new Dictionary<string, List<Tuple<Position, bool>>>();
        public static Stack<string> TraceStack = new Stack<string>();

        /// <summary>
        ///     パーサのトレース（デバッグ・チューニング用）
        /// </summary>
        /// <param name="parser">パーサ</param>
        /// <returns></returns>
        public static Parser<T> Trace<T>(string caption, Parser<T> parser) {
#if TraceParser
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }

            TraceInfo[caption] = new List<Tuple<Position, bool>>();

            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }

                TraceStack.Push(caption);
                var parsed = parser(context, position, state);
                TraceInfo[caption].Add(Tuple.Create(position, parsed.Success));
                TraceStack.Pop();
                return parsed;
            };
#else
            return parser;
#endif
        }

        /// <summary>
        ///     遅延評価パーサを生成
        /// </summary>
        /// <param name="fn">遅延評価するパーサ</param>
        /// <returns></returns>
        public static Parser<T> Lazy<T>(Func<Parser<T>> fn) {
            if (fn == null) {
                throw new ArgumentNullException(nameof(fn));
            }

            Parser<T> parser = null;
            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }

                if (parser == null) {
                    parser = fn();
                    if (parser == null) {
                        throw new Exception("fn() result is null.");
                    }
                }
                var ret = parser(context, position, state);
                return ret;
            };
        }

        /// <summary>
        ///     パーサをオプションとして扱うパーサを生成する
        /// </summary>
        /// <param name="parser">オプションとして扱うパーサ</param>
        /// <returns></returns>
        public static Parser<T> Option<T>(this Parser<T> parser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var parsed = parser(context, position, state);
                if (parsed.Success) {
                    return parsed;
                } else {
                    return Result<T>.Accept(default(T), position, state);
                }
            };
        }

        /// <summary>
        ///     parserでマッチした範囲に対する絞り込みを行うパーサを生成する
        /// </summary>
        /// <param name="parser">パーサ</param>
        /// <param name="reparser">絞り込むパーサ(絞り込むパーサが上位のパーサが受理した範囲と同じ範囲を完全に受理したときのみ絞り込み成功となる)</param>
        /// <returns></returns>
        public static Parser<T> Refinement<T>(this Parser<T> parser, Parser<T> reparser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }

                var parsed = parser(context, position, state);
                if (!parsed.Success) {
                    return Result<T>.Reject();
                }

                // 絞り込みを行う
                var reparsed = reparser(context, position, state);
                if (!reparsed.Success || (!parsed.Position.Equals(reparsed.Position))) {
                    return Result<T>.Reject();
                }

                return reparsed;

            };
        }

        /// <summary>
        ///     パーサの位置情報を取得する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pred"></param>
        /// <returns></returns>
        public static Parser<Position> Position() {
            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                return Result<Position>.Accept(position, position, state);
            };
        }

        /// <summary>
        ///     セマンティックアクションを実行するパーサを生成する。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <returns></returns>
        public static Parser<object> Action(
            Func<object, object> action
        ) {
            if (action == null) {
                throw new ArgumentNullException(nameof(action));
            }
            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var newState = action.Invoke(state);
                return Result<object>.Accept(null, position, newState);
            };
        }

        /// <summary>
        ///     パーサが文字列を受理した場合、その結果に述語関数fnを適用して変形するパーサ
        /// </summary>
        /// <param name="parser">評価したいパーサ</param>
        /// <param name="fn">結果に適用する述語関数</param>
        /// <returns></returns>
        public static Parser<TOutput> Map<TInput, TOutput>(this Parser<TInput> parser, Func<TInput, TOutput> fn) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (fn == null) {
                throw new ArgumentNullException(nameof(fn));
            }
            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var parsed = parser(context, position, state);
                if (parsed.Success) {
                    return Result<TOutput>.Accept(fn(parsed.Value), parsed.Position, parsed.State);
                } else {
                    return Result<TOutput>.Reject();
                }
            };
        }

        /// <summary>
        ///     パーサの結果に述語関数fnを適用して評価するパーサを生成する。
        /// </summary>
        /// <param name="parser">評価したいパーサ</param>
        /// <param name="fn">結果に適用する述語関数</param>
        /// <returns></returns>
        public static Parser<T> Filter<T>(this Parser<T> parser, Func<T, bool> fn) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (fn == null) {
                throw new ArgumentNullException(nameof(fn));
            }

            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var parsed = parser(context, position, state);
                if (!parsed.Success) {
                    return parsed;
                }
                if (!fn(parsed.Value)) {
                    context.MarkFailed(position);
                    return Result<T>.Reject();
                }
                return Result<T>.Accept(parsed.Value, parsed.Position, parsed.State);
            };
        }


        public static Parser<T> Filter<T>(this Parser<T> parser, Func<T, object, bool> fn) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (fn == null) {
                throw new ArgumentNullException(nameof(fn));
            }

            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var parsed = parser(context, position, state);
                if (!parsed.Success || !fn(parsed.Value, parsed.State)) {
                    return Result<T>.Reject();
                } else {
                    return Result<T>.Accept(parsed.Value, parsed.Position, parsed.State);
                }
            };
        }

        /// <summary>
        ///     パーサの結果が真なら偽を、偽なら真を返すパーサを生成する
        /// </summary>
        /// <param name="parser">評価したいパーサ</param>
        /// <returns></returns>
        public static Parser<T> Not<T>(this Parser<T> parser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }

            return (context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }
                var parsed = parser(context, position, state);
                if (parsed.Success == false) {
                    return Result<T>.Accept(default(T), position, state);
                } else {
                    return Result<T>.Reject();
                }
            };
        }

        // 以降は利便性用

        /// <summary>
        ///     パーサの連結（selfの結果は読み捨てる）。
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="self"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Parser<T2> Then<T1, T2>(this Parser<T1> self, Parser<T2> rhs) {
            if (self == null) {
                throw new ArgumentNullException(nameof(self));
            }
            if (rhs == null) {
                throw new ArgumentNullException(nameof(rhs));
            }
            return
                from _1 in self
                from _2 in rhs
                select _2;
        }

        /// <summary>
        ///     パーサの連結（rhsの結果は読み捨てる）。
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="self"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static Parser<T1> Skip<T1, T2>(this Parser<T1> self, Parser<T2> rhs) {
            if (self == null) {
                throw new ArgumentNullException(nameof(self));
            }
            if (rhs == null) {
                throw new ArgumentNullException(nameof(rhs));
            }

            return
                from _1 in self
                from _2 in rhs
                select _1;
        }

        /// <summary>
        ///     区切り要素を挟んだパーサの繰り返し
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="parser"></param>
        /// <param name="separator">区切り要素</param>
        /// <returns></returns>
        public static Parser<IReadOnlyList<T1>> Separate<T1, T2>(this Parser<T1> parser, Parser<T2> separator, int min = -1, int max = -1) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (separator == null) {
                throw new ArgumentNullException(nameof(separator));
            }
            if (min >= 0 && max >= 0 && min > max) {
                throw new ArgumentException("min < max");
            }

            return ((context, position, state) => {
                if (context?.source == null) {
                    throw new ArgumentNullException(nameof(context.source));
                }

                var result = new List<T1>();

                var currentPosition = position;
                var currentState = state;

                // 最初の要素を読み取り
                var parsed0 = parser(context, currentPosition, currentState);

                if (parsed0.Success) {
                    // 読み取りに成功したので情報を更新
                    result.Add(parsed0.Value); // 結果を格納
                    currentPosition = parsed0.Position; // 読み取り位置を更新する
                    currentState = parsed0.State;

                    for (;;) {
                        // 区切り要素を読み取り
                        var parsed1 = separator(context, currentPosition, currentState);


                        if (!parsed1.Success) {
                            // 読み取りに失敗したので、読み取り処理終了
                            break;
                        }

                        // 読み取りに成功したので次の要素を読み取り
                        var parsed2 = parser(context, parsed1.Position, parsed1.State);

                        if (!parsed2.Success) {
                            // 読み取りに失敗したので、読み取り処理終了
                            break;
                        } else {
                            // 読み取りに成功したので情報更新
                            result.Add(parsed2.Value); // 結果を格納
                            currentPosition = parsed2.Position; // 読み取り位置を更新する
                            currentState = parsed2.State;
                        }
                    }

                }


                if (min >= 0 && result.Count < min || max >= 0 && result.Count > max) {
                    return Result<IReadOnlyList<T1>>.Reject();
                } else {
                    return Result<IReadOnlyList<T1>>.Accept(result, currentPosition, currentState);
                }
            });
        }


        /// <summary>
        /// 括弧などで括る構文を容易に記述するためのパーサ
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="left"></param>
        /// <param name="parser"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Parser<T2> Quote<T1, T2, T3>(Parser<T1> left, Parser<T2> parser, Parser<T3> right) {
            if (left == null) {
                throw new ArgumentNullException(nameof(left));
            }
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (right == null) {
                throw new ArgumentNullException(nameof(right));
            }
            return
                from _1 in left
                from _2 in parser
                from _3 in right
                select _2;
        }

        /// <summary>
        /// C言語の#lineディレクティブのようにファイル中での見かけ上の行列位置を変更するためのパーサ
        /// </summary>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static Parser<object> Reposition(Func<Position, Position> fn) {
            return (context, position, state) => Result<object>.Accept(null, fn(position), state);
        }

    }

    /// <summary>
    ///     パーサコンビネータをLINQ式で扱えるようにするための拡張メソッド
    /// </summary>
    public static class Linq {
        public static Parser<TOutput> Select<TInput, TOutput>(
            this Parser<TInput> parser,
            Func<TInput, TOutput> selector
        ) {
            return parser.Map(selector);
        }

        public static Parser<TOutput> SelectMany<TInput, T, TOutput>(
            this Parser<TInput> parser,
            Func<TInput, Parser<T>> selector,
            Func<TInput, T, TOutput> projector
        ) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (selector == null) {
                throw new ArgumentNullException(nameof(selector));
            }
            if (projector == null) {
                throw new ArgumentNullException(nameof(projector));
            }
            return (context, position, state) => {
                var parsed1 = parser(context, position, state);
                if (!parsed1.Success) {
                    return Result<TOutput>.Reject();
                }

                var parsed2 = selector(parsed1.Value)(context, parsed1.Position, parsed1.State);
                if (!parsed2.Success) {
                    return Result<TOutput>.Reject();
                }
                return Result<TOutput>.Accept(projector(parsed1.Value, parsed2.Value), parsed2.Position, parsed2.State);
            };
        }

        public static Parser<T> Where<T>(this Parser<T> parser, Func<T, bool> selector) {
            return parser.Filter(selector);
        }

        public static Parser<T> Where<T>(this Parser<T> parser, Func<T, object, bool> selector) {
            return parser.Filter(selector);
        }
    }

    public static class Helper {
        public static Parser<string> String(this Parser<char[]> parser) {
            return parser.Select(x => new string(x));
        }

        public static Parser<string> String(this Parser<IReadOnlyList<char>> parser) {
            return parser.Select(x => System.String.Concat(x));
        }

        public static Parser<string> String(this Parser<char> parser) {
            return parser.Select(x => x == '\0' ? "" : x.ToString());
        }

        public static Parser<string> String(this Parser<MatchRegion> parser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            return (context, position, state) => {
                var parsed = parser(context, position, state);
                if (!parsed.Success) {
                    return Result<string>.Reject();
                }
                var region = parsed.Value;
                var result = context.source.Substring(region.Start.Index, region.End.Index - region.Start.Index);
                return Result<string>.Accept(result, parsed.Position, parsed.State);
            };
        }

        public static T Tap<T>(this T self, Action<T> pred) {
            pred(self);
            return self;
        }
    }

}