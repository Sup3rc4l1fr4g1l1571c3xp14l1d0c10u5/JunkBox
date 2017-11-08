//#define TraceParser

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CParser2;
//using System.Text.RegularExpressions;


namespace Parsing {
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
        } // 名前


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

        public string SubString(int start, int length) {
            var sb = new StringBuilder();
            for (var i = 0; i < length; i++) {
                var ch = this[start+i];
                if (ch == -1) {
                    break;
                }
                sb.Append((char)ch);
            }
            return sb.ToString();
        }
    }


    /// <summary>
    ///     パーサの位置情報
    /// </summary>
    public class Position : IComparer<Position> {
        public int Index {
            get;
        } // 文字列上の位置
        public string FileName {
            get;
        } // 見かけ上のファイル
        public int Row {
            get;
        } // 見かけ上のファイル上の行
        public int Column {
            get;
        } // 見かけ上のファイル上の列
        private char PrevChar {
            get;
        }

        public int Compare(Position x, Position y) {
            if (x == null && y == null) {
                return 0;
            }
            if (x == null) {
                return 1;
            }
            if (y == null) {
                return -1;
            }
            return x.Index.CompareTo(y.Index);
        }

        public static bool operator >(Position lhs, Position rhs) => lhs.Index > rhs.Index;
        public static bool operator <(Position lhs, Position rhs) => lhs.Index < rhs.Index;
        public static bool operator >=(Position lhs, Position rhs) => lhs.Index >= rhs.Index;
        public static bool operator <=(Position lhs, Position rhs) => lhs.Index <= rhs.Index;

        public override bool Equals(object obj) {
            return (obj as Position)?.Index == Index;
        }

        public override int GetHashCode() {
            return Index;
        }

        private Position(int index, string filename, int row, int column, char prevChar) {
            Index = index;
            FileName = filename;
            Row = row;
            Column = column;
            PrevChar = prevChar;
        }

        public override string ToString() {
            return $"{FileName} ({Row}:{Column})";
        }

        public static Position Empty { get; } = new Position(0, "", 1, 1, '\0');
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
                        index += 1;
                        if (prevChar != '\r') {
                            row++;
                            col = 1;
                        }
                        break;
                    case '\r':
                        index += 1;
                        row++;
                        col = 1;
                        break;
                    default:
                        index += 1;
                        col += 1;
                        break;
                }
                prevChar = t;
            }
            return new Position(index, FileName, row, col, prevChar);
        }
        public Position Inc(char t) {
            var row = Row;
            var col = Column;
            var index = Index;
            var prevChar = PrevChar;

            switch (t) {
                case '\n':
                    index += 1;
                    if (prevChar != '\r') {
                        row++;
                        col = 1;
                    }
                    break;
                case '\r':
                    index += 1;
                    row++;
                    col = 1;
                    break;
                default:
                    index += 1;
                    col += 1;
                    break;
            }
            prevChar = t;

            return new Position(index, FileName, row, col, prevChar);
        }
    }

    /// <summary>
    ///     パーサコンビネータの結果
    /// </summary>

    public abstract class Result<T> {
        public class Some : Result<T> {
            public Some(T value, Position position, object status) : base(status) {
                Position = position;
                Value = value;
            }

            public override bool Success {
                get {
                    return true;
                }
            }
            public override Position Position {
                get;
            }

            public override T Value {
                get;
            }

        }
        public class None : Result<T> {
            public None(object status) : base(status) {
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
        public object Status {
            get;
        }

        /// <summary>
        ///     コンストラクタ
        /// </summary>
        /// <param name="status"></param>
        protected Result(object status) {
            Status = status;
        }

        public static Result<T> Accept(T value, Position position, object status) {
            return new Some(value, position, status);
        }

        public static Result<T> Reject(object status) {
            return new None(status);
        }

    }

    /// <summary>
    /// パーサ全体の情報
    /// </summary>
    public class Context {
        public Source target {
            get;
        }
        public Position failedPosition {
            get; private set;
        }

        public Context(Source source) {
            target = source;
            failedPosition = Position.Empty.Reposition(source.Name, 1, 1);
        }

        public void handleFailed(Position position) {
            if (position > failedPosition) {
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
    /// <param name="status"></param>
    /// <returns></returns>
    public delegate Result<T> Parser<T>(Context context, Position position, object status);
    
    /// <summary>
    ///     パーサコンビネータ
    /// </summary>
    public static class Combinator {


        public static class Parser {
            /// <summary>
            ///     常に成功/失敗する空のパーサ
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public static Parser<T> Empty<T>(bool success) {
                if (success) {
                    return (context, position, status) => Result<T>.Accept(default(T), position, status);
                } else {
                    return (context, position, status) => {
                        context.handleFailed(position);
                        return Result<T>.Reject(status);
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
                return (context, position, status) => {
                    if (context.target == null) {
                        throw new ArgumentNullException(nameof(context.target));
                    }
                    if (context.target.StartsWith(position.Index, str)) {
                        return Result<string>.Accept(str, position.Inc(str), status);
                    } else {
                        context.handleFailed(position);
                        return Result<string>.Reject(status);
                    }
                };
            }
        }

        /// <summary>
        ///     パーサparserが受理する文字列の繰り返しを受理できるパーサを生成する
        /// </summary>
        /// <param name="parser">パーサ</param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns>パーサ</returns>
        public static Parser<T[]> Many<T>(this Parser<T> parser, int min = -1, int max = -1) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (min >= 0 && max >= 0 && min > max) {
                throw new ArgumentException("min < max");
            }

            return ((context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }

                var result = new List<T>();

                var currentPosition = position;
                var currentStatus = status;

                for (;;) {
                    var parsed = parser(context, currentPosition, currentStatus);

                    if (!parsed.Success) {
                        // 読み取りに失敗
                        break;
                    } else {
                        // 読み取りに成功
                        result.Add(parsed.Value); // 結果を格納
                        currentPosition = parsed.Position; // 読み取り位置を更新する
                        currentStatus = parsed.Status;
                    }
                }

                if (min >= 0 && result.Count < min || max >= 0 && result.Count > max) {
                    return Result<T[]>.Reject(status);
                } else {
                    return Result<T[]>.Accept(result.ToArray(), currentPosition, currentStatus);
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

            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }

                foreach (var parser in parsers) {
                    var parsed = parser(context, position, status);
                    if (parsed.Success) {
                        return Result<T>.Accept(parsed.Value, parsed.Position, parsed.Status);
                    }
                }
                return Result<T>.Reject(status);
            };
        }

        /// <summary>
        ///     パーサ列parsersを連結したパーサを生成する
        /// </summary>
        /// <param name="parsers"></param>
        /// <returns></returns>
        public static Parser<T[]> Seq<T>(params Parser<T>[] parsers) {
            if (parsers == null) {
                throw new ArgumentNullException(nameof(parsers));
            }
            if (parsers.Any(x => x == null)) {
                throw new ArgumentNullException(nameof(parsers));
            }

            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                var result = new List<T>();

                var currentPosition = position;
                var currentStatus = status;

                foreach (var parser in parsers) {
                    var parsed = parser(context, currentPosition, currentStatus);

                    if (parsed.Success) {
                        result.Add(parsed.Value);
                        currentPosition = parsed.Position;
                        currentStatus = parsed.Status;
                    } else {
                        return Result<T[]>.Reject(status);
                    }
                }
                return Result<T[]>.Accept(result.ToArray(), currentPosition, currentStatus);
            };
        }

#if false /// <summary>
/// 正規表現(System.Text.RegularExpressions.Regex)を用いるパーサを生成する
/// </summary>
/// <param name="pattern">正規表現パターン</param>
/// <param name="options">正規表現オプション</param>
/// <returns></returns>
        public static Parser<string> Regex(string pattern, RegexOptions options = 0) {
            if (pattern == null) {
                throw new ArgumentNullException(nameof(pattern));
            }

            Regex regexp;
            try {
                regexp = new Regex("^(?:" + pattern + ")", options | RegexOptions.Compiled);
            } catch (Exception e) {
                throw new ArgumentException(@"Invalid regular expression or options value.", e);
            }

            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                if (position.Index >= context.target.Length) {
                    return Result<string>.Reject( null, position, failedPosition.MostFar(position));
                }

                var match = regexp.Match(context.target.Substring(position.Index));
                if (match.Success) {
                    return Result<string>.Accept( match.Value, position.Inc(match.Value), failedPosition);
                } else {
                    return Result<string>.Reject( null, position, failedPosition.MostFar(position));
                }
            };
        }
#endif

        /// <summary>
        ///     任意の一文字に一致するパーサを生成する
        /// </summary>
        /// <returns></returns>
        public static Parser<char> AnyChar() {
            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                var ch = context.target[position.Index];
                if (ch == -1) {
                    context.handleFailed(position);
                    return Result<char>.Reject(status);
                } else {
                    return Result<char>.Accept((char)ch, position.Inc((char)ch), status);
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

            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                var ch = context.target[position.Index];
                if (ch != -1 && dict.Contains((char)ch)) {
                    return Result<char>.Accept((char)ch, position.Inc((char)ch), status);
                } else {
                    context.handleFailed(position);
                    return Result<char>.Reject(status);
                }
            };
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

            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                var ch = context.target[position.Index];
                if (ch != -1 && pred((char)ch)) {
                    return Result<char>.Accept((char)ch, position.Inc((char)ch), status);
                } else {
                    context.handleFailed(position);
                    return Result<char>.Reject(status);
                }
            };
        }

        /// <summary>
        ///     EOFにマッチするパーサ
        /// </summary>
        /// <returns></returns>
        public static Parser<char> EoF() {
            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                var ch = context.target[position.Index];
                if (ch != -1) {
                    context.handleFailed(position);
                    return Result<char>.Reject(status);
                } else {
                    return Result<char>.Accept((char)ch, position, status);
                }
            };
        }

        private struct MemoizeKey {
            private readonly int _position;
            private readonly object _status;
            private readonly int _hashCode;

            public MemoizeKey(int position, object status) {
                _position = position;
                _status = status;
                _hashCode = -118752591;
                _hashCode = _hashCode * -1521134295 + EqualityComparer<int>.Default.GetHashCode(_position);
                _hashCode = _hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(_status);
            }

            public override bool Equals(object obj) {
                if (obj == null) {
                    return false;
                }
                return _position.Equals(((MemoizeKey)obj)._position) && Object.Equals(_status, ((MemoizeKey)obj)._status);
            }

            public override int GetHashCode() {
                return _hashCode;
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

            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }

                var key = new MemoizeKey(position.Index, status);
                Result<T> parsed;
                if (memoization.TryGetValue(key, out parsed)) {
                    return parsed;
                }

                parsed = parser(context, position, status);
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

            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }

                TraceStack.Push(caption);
                var parsed = parser(context, position, status);
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
            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }

                if (parser == null) {
                    parser = fn();
                    if (parser == null) {
                        throw new Exception("fn() result is null.");
                    }
                }
                var ret = parser(context, position, status);
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
            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                var parsed = parser(context, position, status);
                if (parsed.Success) {
                    return parsed;
                } else {
                    return Result<T>.Accept(default(T), position, status);
                }
            };
        }

        /// <summary>
        ///     parserでマッチした範囲に対する絞り込みを行うパーサを生成する
        /// </summary>
        /// <param name="parser">パーサ</param>
        /// <param name="reparser">絞り込むパーサ</param>
        /// <returns></returns>
        public static Parser<T> Refinement<T>(this Parser<T> parser, Parser<T> reparser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }

                var parsed = parser(context, position, status);
                if (!parsed.Success) {
                    return Result<T>.Reject(status);
                }

                // 絞り込みを行う
                var reparsed = reparser(context, position, status);
                if (!reparsed.Success || (!parsed.Position.Equals(reparsed.Position))) {
                    return Result<T>.Reject(status);
                }

                return reparsed;

            };
        }

        /// <summary>
        ///     入力を処理せずパーサの状態を覗き見するパーサを生成する（デバッグや入力の位置情報を取得するときに役に立つ）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pred"></param>
        /// <returns></returns>
        public static Parser<T> Tap<T>(Func<Context, Position, object, T> pred) {
            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                var parsed = pred(context, position, status);
                return Result<T>.Accept(parsed, position, status);
            };
        }

        /// <summary>
        ///     入力を処理せずセマンティックアクションを実行するパーサを生成する。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parser"></param>
        /// <param name="enter"></param>
        /// <param name="leave"></param>
        /// <returns></returns>
        public static Parser<T> Action<T>(
            this Parser<T> parser,
            Func<Context, Position, object, object> enter = null,
            Func<Result<T>, object> leave = null
        ) {
            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                var newstatus1 = (enter != null) ? enter.Invoke(context, position, status) : status;
                var parsed = parser(context, position, newstatus1);
                var newstatus2 = (leave != null) ? leave.Invoke(parsed) : parsed.Status;
                if (parsed.Success) {
                    return Result<T>.Accept(parsed.Value, parsed.Position, newstatus2);
                } else {
                    return Result<T>.Reject(newstatus2);
                }
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
            return (context, position, status) => {
                var parsed = parser(context, position, status);
                if (parsed.Success) {
                    return Result<TOutput>.Accept(fn(parsed.Value), parsed.Position, parsed.Status);
                } else {
                    return Result<TOutput>.Reject(status);
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

            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                var parsed = parser(context, position, status);
                if (!parsed.Success) {
                    return parsed;
                }
                if (!fn(parsed.Value)) {
                    context.handleFailed(position);
                    return Result<T>.Reject(status);
                }
                return Result<T>.Accept(parsed.Value, parsed.Position, parsed.Status);
            };
        }


        public static Parser<T> Filter<T>(this Parser<T> parser, Func<T, object, bool> fn) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (fn == null) {
                throw new ArgumentNullException(nameof(fn));
            }

            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                var parsed = parser(context, position, status);
                if (!parsed.Success || !fn(parsed.Value, parsed.Status)) {
                    return Result<T>.Reject(status);
                } else {
                    return Result<T>.Accept(parsed.Value, parsed.Position, parsed.Status);
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

            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                var parsed = parser(context, position, status);
                if (parsed.Success == false) {
                    return Result<T>.Accept(default(T), position, status);
                } else {
                    return Result<T>.Reject(status);
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
        public static Parser<T1[]> Separate<T1, T2>(this Parser<T1> parser, Parser<T2> separator, int min = -1, int max = -1) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (separator == null) {
                throw new ArgumentNullException(nameof(separator));
            }
            if (min >= 0 && max >= 0 && min > max) {
                throw new ArgumentException("min < max");
            }

            return ((context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }

                var result = new List<T1>();

                var currentPosition = position;
                var currentStatus = status;

                // 最初の要素を読み取り
                var parsed0 = parser(context, currentPosition, currentStatus);

                if (parsed0.Success) {
                    // 読み取りに成功したので情報を更新
                    result.Add(parsed0.Value); // 結果を格納
                    currentPosition = parsed0.Position; // 読み取り位置を更新する
                    currentStatus = parsed0.Status;

                    for (;;) {
                        // 区切り要素を読み取り
                        var parsed1 = separator(context, currentPosition, currentStatus);


                        if (!parsed1.Success) {
                            // 読み取りに失敗したので、読み取り処理終了
                            break;
                        }

                        // 読み取りに成功したので次の要素を読み取り
                        var parsed2 = parser(context, parsed1.Position, parsed1.Status);

                        if (!parsed2.Success) {
                            // 読み取りに失敗したので、読み取り処理終了
                            break;
                        } else {
                            // 読み取りに成功したので情報更新
                            result.Add(parsed2.Value); // 結果を格納
                            currentPosition = parsed2.Position; // 読み取り位置を更新する
                            currentStatus = parsed2.Status;
                        }
                    }

                }


                if (min >= 0 && result.Count < min || max >= 0 && result.Count > max) {
                    return Result<T1[]>.Reject(status);
                } else {
                    return Result<T1[]>.Accept(result.ToArray(), currentPosition, currentStatus);
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
            return (context, position, status) => Result<object>.Accept(null, fn(position), status);
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
            return (context, position, status) => {
                var parsed1 = parser(context, position, status);
                if (!parsed1.Success) {
                    return Result<TOutput>.Reject(status);
                }

                var parsed2 = selector(parsed1.Value)(context, parsed1.Position, parsed1.Status);
                if (!parsed2.Success) {
                    return Result<TOutput>.Reject(status);
                }
                return Result<TOutput>.Accept(projector(parsed1.Value, parsed2.Value), parsed2.Position, parsed2.Status);
            };
        }

        public static Parser<T> Where<T>(this Parser<T> parser, Func<T, bool> selector) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (selector == null) {
                throw new ArgumentNullException(nameof(selector));
            }

            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                var parsed = parser(context, position, status);
                if (!parsed.Success) {
                    return parsed;
                }
                if (!selector(parsed.Value)) {
                    context.handleFailed(position);
                    return Result<T>.Reject(status);
                }
                return Result<T>.Accept(parsed.Value, parsed.Position, parsed.Status);
            };
        }

        public static Parser<T> Where<T>(this Parser<T> parser, Func<T, object, bool> selector) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (selector == null) {
                throw new ArgumentNullException(nameof(selector));
            }

            return (context, position, status) => {
                if (context.target == null) {
                    throw new ArgumentNullException(nameof(context.target));
                }
                var parsed = parser(context, position, status);
                if (!parsed.Success) {
                    return parsed;
                }
                if (!selector(parsed.Value, parsed.Status)) {
                    context.handleFailed(position);
                    return Result<T>.Reject(status);
                }
                return Result<T>.Accept(parsed.Value, parsed.Position, parsed.Status);
            };
        }
    }

    public static class Helper {
        public static Parser<string> String(this Parser<char[]> parser) {
            return parser.Select(x => new string(x));
        }

        public static Parser<string> String(this Parser<char> parser) {
            return parser.Select(x => x == '\0' ? "" : x.ToString());
        }
    }

}