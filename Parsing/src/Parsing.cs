#define TraceParser

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
//using System.Text.RegularExpressions;


namespace Parsing {
    public class Source {
        /// <summary>
        /// 読み取った入力を格納するバッファ
        /// </summary>
        private StringBuilder Buffer { get; }

        /// <summary>
        /// 入力ストリーム
        /// </summary>
        private TextReader Reader { get; }

        /// <summary>
        /// 入力の終端に到達したら真
        /// </summary>
        public bool Eof { get; private set; }

        /// <summary>
        /// 入力ソース名
        /// </summary>
        public string Name { get; } // 名前


        public Source(string name, TextReader reader) {
            if (name == null) {throw new ArgumentNullException(nameof(name)); }
            if (reader == null) {throw new ArgumentNullException(nameof(reader)); }
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

    }


    /// <summary>
    ///     パーサの位置情報
    /// </summary>
    public class Position : IComparer<Position> {
        public int Index { get; } // 文字列上の位置
        public int Row { get; } // 行
        public int Column { get; } // 列
        private char PrevChar { get; }

        public int Compare(Position x, Position y) {
            if (x == null && y == null) { return 0; }
            if (x == null             ) { return 1; }
            if (             y == null) { return -1; }
            return x.Index.CompareTo(y.Index);
        }

        public static bool operator >(Position lhs, Position rhs) => lhs.Index > rhs.Index;
        public static bool operator <(Position lhs, Position rhs) => lhs.Index < rhs.Index;

        public override bool Equals(object obj) {
            return (obj as Position)?.Index == Index;
        }

        public override int GetHashCode() {
            return Index;
        }

        private Position(int index, int row, int column, char prevChar) {
            Index = index;
            Row = row;
            Column = column;
            PrevChar = prevChar;
        }

        public override string ToString() {
            return $"({Row}:{Column})";
        }

        public static Position Empty { get; } = new Position(0, 1, 1, '\0');

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
            return new Position(index, row, col, prevChar);
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

            return new Position(index, row, col, prevChar);
        }
    }

    /// <summary>
    ///     パーサコンビネータの結果
    /// </summary>
    public class Result<T> {
        /// <summary>
        ///     パーサがマッチした場合は真、それ以外の場合は偽となる
        /// </summary>
        public bool Success { get; }

        /// <summary>
        ///     パーサがマッチした際は次の読み取り位置を示す
        /// </summary>
        public Position Position { get; }

        /// <summary>
        ///     パーサを最も読み住めることができた位置
        /// </summary>
        public Position FailedPosition { get; }

        /// <summary>
        ///     パーサがマッチした際の結果を示す
        /// </summary>
        public T Value { get; }

        /// <summary>
        ///     パーサの状態を示すオブジェクト
        /// </summary>
        public object Status { get; }

        /// <summary>
        ///     コンストラクタ
        /// </summary>
        /// <param name="success">パーサがマッチした場合は真、それ以外の場合は偽</param>
        /// <param name="value">パーサがマッチした際の次の読み取り位置</param>
        /// <param name="position">パーサがマッチした際の値</param>
        /// <param name="failedPosition"></param>
        /// <param name="status"></param>
        public Result(bool success, T value, Position position, Position failedPosition, object status) {
            Success = success;
            Value = value;
            Position = position;
            FailedPosition = failedPosition;
            Status = status;
        }

        public static Result<T> Accept(T value, Position position, Position failedPosition, object status) {
            return new Result<T>(true, value, position, failedPosition, status);
        }

        public static Result<T> Reject(T value, Position position, Position failedPosition, object status) {
            return new Result<T>(false, value, position, failedPosition, status);
        }

    }

    /// <summary>
    ///     パーサを表すデリゲート
    /// </summary>
    /// <typeparam name="T">パース結果型</typeparam>
    /// <param name="target">パース対象文字列</param>
    /// <param name="position">現在の位置</param>
    /// <param name="failedPosition">最も読み進めることに成功した失敗位置</param>
    /// <returns></returns>
    public delegate Result<T> Parser<T>(Source target, Position position, Position failedPosition, object status);

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
                return (target, position, failedPosition, status) => {
                    return Result<T>.Accept(default(T), position, failedPosition, status);
                };
            } else {
                return (target, position, failedPosition, status) => {
                    var fpos = (position > failedPosition) ? position : failedPosition;
                    return Result<T>.Reject(default(T), position, fpos, status);
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
            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                if (target.StartsWith(position.Index, str)) {
                    return Result<string>.Accept(str, position.Inc(str), failedPosition, status);
                } else {
                    var newFailedPosition = (position > failedPosition) ? position : failedPosition;
                    return Result<string>.Reject(null, position, newFailedPosition, status);
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
        public static Parser<T[]> Many<T>(this Parser<T> parser, int min = -1, int max = -1) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (min >= 0 && max >= 0 && min > max) {
                throw new ArgumentException("min < max");
            }

            return ((target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }

                var result = new List<T>();

                var currentPosition = position;
                var currentFailedPosition = failedPosition;
                var currentStatus = status;

                for (; ; )
                {
                    var parsed = parser(target, currentPosition, currentFailedPosition, currentStatus);

                    // 読み取り失敗位置はより読み進んでいる方を採用
                    currentFailedPosition = (parsed.FailedPosition > currentFailedPosition) ? parsed.FailedPosition : currentFailedPosition;

                    if (!parsed.Success) {
                        // 読み取りに失敗したので、読み取り開始位置と読み取り失敗位置のうち、より読み進んでいる方をManyパーサの読み取り失敗位置として採用
                        currentFailedPosition = (currentFailedPosition > currentPosition) ? currentFailedPosition : currentPosition;
                        break;
                    } else {
                        // 読み取りに成功したので情報を更新
                        result.Add(parsed.Value); // 結果を格納
                        currentPosition = parsed.Position; // 読み取り位置を更新する
                        currentStatus = parsed.Status;
                    }
                }

                if (min >= 0 && result.Count < min || max >= 0 && result.Count > max) {
                    return Result<T[]>.Reject(new T[0], position, currentFailedPosition, status);
                } else {
                    return Result<T[]>.Accept(result.ToArray(), currentPosition, currentFailedPosition, currentStatus);
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

            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }

                var lastFailedPosition = failedPosition;
                foreach (var parser in parsers) {
                    var parsed = parser(target, position, lastFailedPosition, status);
                    lastFailedPosition = (parsed.FailedPosition > lastFailedPosition) ? parsed.FailedPosition : lastFailedPosition;

                    if (parsed.Success) {
                        return Result<T>.Accept(parsed.Value, parsed.Position, lastFailedPosition, parsed.Status);
                    }
                }
                var newFailedPosition = (position > lastFailedPosition) ? position : lastFailedPosition;
                return Result<T>.Reject(default(T), position, newFailedPosition, status);
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

            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                var result = new List<T>();

                var currentPosition = position;
                var currentFailedPosition = failedPosition;
                var currentStatus = status;

                foreach (var parser in parsers) {
                    var parsed = parser(target, currentPosition, currentFailedPosition, currentStatus);
                    currentFailedPosition = parsed.FailedPosition;

                    if (parsed.Success) {
                        result.Add(parsed.Value);
                        currentPosition = parsed.Position;
                        currentStatus = parsed.Status;
                    } else {
                        if (currentPosition > currentFailedPosition) {
                            currentFailedPosition = currentPosition;
                        }
                        return Result<T[]>.Reject(null, position, currentFailedPosition, status);
                    }
                }
                return Result<T[]>.Accept(result.ToArray(), currentPosition, currentFailedPosition, currentStatus);
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

            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                if (position.Index >= target.Length) {
                    return Result<string>.Reject( null, position, failedPosition.MostFar(position));
                }

                var match = regexp.Match(target.Substring(position.Index));
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
            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                var ch = target[position.Index];
                if (ch == -1) {
                    var newFailedPosition = (position > failedPosition) ? position : failedPosition;
                    return Result<char>.Reject(default(char), position, newFailedPosition, status);
                } else {
                    return Result<char>.Accept((char)ch, position.Inc((char)ch), failedPosition, status);
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

            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                var ch = target[position.Index];
                if (ch != -1 && dict.Contains((char)ch)) {
                    return Result<char>.Accept((char)ch, position.Inc((char)ch), failedPosition, status);
                } else {
                    var newFailedPosition = (position > failedPosition) ? position : failedPosition;
                    return Result<char>.Reject(default(char), position, newFailedPosition, status);
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

            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                var ch = target[position.Index];
                if (ch != -1 && pred((char)ch)) {
                    return Result<char>.Accept((char)ch, position.Inc((char)ch), failedPosition, status);
                } else {
                    var newFailedPosition = (position > failedPosition) ? position : failedPosition;
                    return Result<char>.Reject(default(char), position, newFailedPosition, status);
                }
            };
        }

        /// <summary>
        ///     EOFにマッチするパーサ
        /// </summary>
        /// <returns></returns>
        public static Parser<char> EoF() {
            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                var ch = target[position.Index];
                if (ch != -1) {
                    var newFailedPosition = (position > failedPosition) ? position : failedPosition;
                    return Result<char>.Reject(default(char), position, newFailedPosition, status);
                } else {
                    return Result<char>.Accept((char)ch, position, failedPosition, status);
                }
            };
        }

        private class MemoizeKey {
            private readonly Source _target;
            private readonly Position _position;
            private readonly object _status;
            private readonly int _hashCode;

            public MemoizeKey(Source target, Position position, object status) {
                _target = target;
                _position = position;
                _status = status;
                _hashCode = -118752591;
                _hashCode = _hashCode * -1521134295 + EqualityComparer<Source>.Default.GetHashCode(_target);
                _hashCode = _hashCode * -1521134295 + EqualityComparer<Position>.Default.GetHashCode(_position);
                _hashCode = _hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(_status);
            }

            private bool Equals(MemoizeKey other) {
                if (other == null) {
                    return false;
                    
                }
                return _target.Equals(other._target)
                       && _position.Equals(other._position)
                       && Object.Equals(_status, other._status);
            }
            public override bool Equals(object obj) {
                if (obj == null) { return false; }
                if (ReferenceEquals(this, obj)) { return true; }
                return Equals(obj as MemoizeKey);
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

            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }

                var key = new MemoizeKey(target, position, status);
                Result<T> parsed;
                if (memoization.TryGetValue(key, out parsed)) {
                    // メモ化してもFailedPositionについてはチェック
                    var newFailedPosition = (failedPosition > parsed.FailedPosition) ? failedPosition : parsed.FailedPosition;
                    return new Result<T>(parsed.Success, parsed.Value, parsed.Position, newFailedPosition, parsed.Status);
                } else { 
                    parsed = parser(target, position, failedPosition, status);
                    memoization.Add(key, parsed);
                    return parsed;
                }
            };
        }

        public static Dictionary<string,List<Tuple<Position,bool>>> TraceInfo = new Dictionary<string, List<Tuple<Position, bool>>>();

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

            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }

                var parsed = parser(target, position, failedPosition, status);
                TraceInfo[caption].Add(Tuple.Create(position, parsed.Success));
                return parsed;
            };
#else
            return parser;
#endif
        }

        /// <summary>
        ///     再帰パーサ用の遅延評価パーサを生成
        /// </summary>
        /// <param name="fn">遅延評価するパーサ</param>
        /// <returns></returns>
        public static Parser<T> Lazy<T>(Func<Parser<T>> fn) {
            if (fn == null) {
                throw new ArgumentNullException(nameof(fn));
            }

            Parser<T> parser = null;
            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }

                if (parser == null) {
                    parser = fn();
                    if (parser == null) {
                        throw new Exception("fn() result is null.");
                    }
                }
                var ret = parser(target, position, failedPosition, status);
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
            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                var parsed = parser(target, position, failedPosition, status);
                if (parsed.Success) {
                    return parsed;
                } else {
                    return Result<T>.Accept(default(T), position, parsed.FailedPosition, status);
                }
            };
        }

        /// <summary>
        ///     パーサのマッチ結果に対する絞り込みを行う
        /// </summary>
        /// <param name="parser">マッチ結果を生成するパーサ</param>
        /// <param name="subParser">マッチ結果に対するパーサ</param>
        /// <returns></returns>
        public static Parser<T> Narrow<T>(this Parser<T> parser, Parser<T> subParser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (subParser == null) {
                throw new ArgumentNullException(nameof(subParser));
            }
            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }

                var parsed1 = parser(target, position, failedPosition, status);
                var newFailedPosition1 = (failedPosition > parsed1.FailedPosition) ? failedPosition : parsed1.FailedPosition;
                if (!parsed1.Success) {
                    return Result<T>.Reject(default(T), position, newFailedPosition1, status);
                }

                var parsed2 = subParser(target, position, newFailedPosition1, status);
                var newFailedPosition2 = (newFailedPosition1 > parsed2.FailedPosition) ? newFailedPosition1 : parsed2.FailedPosition;

                if (!parsed2.Success || !parsed1.Position.Equals(parsed2.Position)) {
                    return Result<T>.Reject(default(T), position, newFailedPosition2, status);
                }

                return parsed2;
            };
        }

        /// <summary>
        ///     入力を処理せずパーサの状態を覗き見するパーサを生成する（デバッグや入力の位置情報を取得するときに役に立つ）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pred"></param>
        /// <returns></returns>
        public static Parser<T> Tap<T>(Func<Source, Position, Position, object, T> pred) {
            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                var parsed = pred(target, position, failedPosition, status);
                return Result<T>.Accept(parsed, position, failedPosition, status);
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
            Func<Source, Position, Position, object, object> enter = null,
            Func<Result<T>, object> leave = null
        ) {
            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                var newstatus1 = (enter != null) ? enter.Invoke(target, position, failedPosition, status) : status;
                var parsed     = parser(target, position, failedPosition, newstatus1);
                var newstatus2 = (leave != null) ? leave.Invoke(parsed) : parsed.Status;
                return new Result<T>(parsed.Success, parsed.Value, parsed.Position, parsed.FailedPosition, newstatus2);
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
            return (target, position, failedPosition, status) => {
                var parsed = parser(target, position, failedPosition, status);
                if (parsed.Success) {
                    return Result<TOutput>.Accept(fn(parsed.Value), parsed.Position, parsed.FailedPosition, parsed.Status);
                } else {
                    var newFailedPosition = (position > parsed.FailedPosition) ? position : parsed.FailedPosition;
                    return Result<TOutput>.Reject(default(TOutput), position, newFailedPosition, status);
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

            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                var parsed = parser(target, position, failedPosition, status);
                if (!parsed.Success || !fn(parsed.Value)) {
                    var newFailedPosition = (position > parsed.FailedPosition) ? position : parsed.FailedPosition;
                    return Result<T>.Reject(default(T), position, newFailedPosition, status);
                }
                return Result<T>.Accept(parsed.Value, parsed.Position, parsed.FailedPosition, parsed.Status);
            };
        }


        public static Parser<T> Filter<T>(this Parser<T> parser, Func<T, object, bool> fn) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (fn == null) {
                throw new ArgumentNullException(nameof(fn));
            }

            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                var parsed = parser(target, position, failedPosition, status);
                if (!parsed.Success || !fn(parsed.Value, parsed.Status)) {
                    var newFailedPosition = (position > parsed.FailedPosition) ? position : parsed.FailedPosition;
                    return Result<T>.Reject(default(T), position, newFailedPosition, status);
                } else {
                    return Result<T>.Accept(parsed.Value, parsed.Position, parsed.FailedPosition, parsed.Status);
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

            return (target, position, failedPosition, status) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                var parsed = parser(target, position, failedPosition, status);
                if (parsed.Success == false) {
                    return Result<T>.Accept(default(T), position, parsed.FailedPosition, status);
                } else {
                    var newFailedPosition = (position > parsed.FailedPosition) ? position : parsed.FailedPosition;
                    return Result<T>.Reject(default(T), position, newFailedPosition, status);
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
        /// <param name="self"></param>
        /// <param name="separator">区切り要素</param>
        /// <returns></returns>
        public static Parser<T1[]> Repeat1<T1, T2>(this Parser<T1> self, Parser<T2> separator) {
            if (self == null) {
                throw new ArgumentNullException(nameof(self));
            }
            if (separator == null) {
                throw new ArgumentNullException(nameof(separator));
            }
            return
                from _1 in self
                from _2 in separator.Then(self).Many()
                select new[] { _1 }.Concat(_2).ToArray();
        }
    }

    /// <summary>
    ///     パーサコンビネータをLINQ式で扱えるようにするための拡張メソッド
    /// </summary>
    public static class CombinatorMonad {
        public static Parser<TOutput> Select<TInput, TOutput>(this Parser<TInput> parser, Func<TInput, TOutput> selector) {
            return parser.Map(selector);
        }

        public static Parser<TOutput> SelectMany<TInput, T, TOutput>(this Parser<TInput> parser,
            Func<TInput, Parser<T>> selector, Func<TInput, T, TOutput> projector) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (selector == null) {
                throw new ArgumentNullException(nameof(selector));
            }
            if (projector == null) {
                throw new ArgumentNullException(nameof(projector));
            }
            return (target, position, failedPosition, status) => {
                var parsed1 = parser(target, position, failedPosition, status);
                if (!parsed1.Success) {
                    var newFailedPosition = (position > parsed1.FailedPosition) ? position : parsed1.FailedPosition;
                    return Result<TOutput>.Reject(default(TOutput), position, newFailedPosition, status);
                }

                var parsed2 = selector(parsed1.Value)(target, parsed1.Position, parsed1.FailedPosition, parsed1.Status);
                if (!parsed2.Success) {
                    var newFailedPosition = (position > parsed2.FailedPosition) ? position : parsed2.FailedPosition;
                    return Result<TOutput>.Reject(default(TOutput), position, newFailedPosition, status);
                }
                return Result<TOutput>.Accept(projector(parsed1.Value, parsed2.Value), parsed2.Position, parsed2.FailedPosition, parsed2.Status);
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

        public static Parser<string> String(this Parser<char> parser) {
            return parser.Select(x => x == '\0' ? "" : x.ToString());
        }
    }
}