using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.AccessControl;
using System.Text.RegularExpressions;

namespace Parsing {

    public struct Position {
        public bool Equals(Position other)
        {
            return string.Equals(Source, other.Source) && Index == other.Index && Row == other.Row && Column == other.Column;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Position && Equals((Position) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Source != null ? Source.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Index;
                hashCode = (hashCode * 397) ^ Row;
                hashCode = (hashCode * 397) ^ Column;
                return hashCode;
            }
        }

        public Position(string source, int index, int row, int column)
        {
            Source = source;
            Index = index;
            Row = row;
            Column = column;
        }

        public Position Inc(string str) {
            var source = Source;
            var index = Index;
            var row = Row;
            var column = Column;

            foreach (var ch in str) { 
                if (ch == '\n') {
                    row++;
                    column = 1;
                } else {
                    column += 1;
                }
                index++;
            }
            return new Position(Source, index, row, column);
        }

        public Position Inc(char ch) {
            var source = Source;
            var index = Index;
            var row = Row;
            var column = Column;

            if (ch == '\n') {
                row++;
                column = 1;
            } else {
                column += 1;
            }
            return new Position(Source,Index+1,row,column);
        }

        public string Source { get; }
        public int Index { get; }
        public int Row { get; }
        public int Column { get; }

        public override string ToString()
        {
            return $"{Source} ({Row}:{Column})";
        }

        public static Position Max(Position p1, Position p2)
        {
            return p1.Index >= p2.Index ? p1 : p2;
        }
    }

    /// <summary>
    /// �p�[�T�R���r�l�[�^�̌���
    /// </summary>
    public class Result<T> {
        /// <summary>
        /// �p�[�T���}�b�`�����ꍇ�͐^�A����ȊO�̏ꍇ�͋U�ƂȂ�
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// �p�[�T���}�b�`�����ۂ͎��̓ǂݎ��ʒu������
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// �p�[�T�����s�����ʒu�̂����ł��ǂݐi�߂����̂�����
        /// </summary>
        public Position FailedPosition { get; }

        /// <summary>
        /// �p�[�T���}�b�`�����ۂ̌��ʂ�����
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// �R���X�g���N�^
        /// </summary>
        /// <param name="success">�p�[�T���}�b�`�����ꍇ�͐^�A����ȊO�̏ꍇ�͋U</param>
        /// <param name="value">�p�[�T���}�b�`�����ۂ̎��̓ǂݎ��ʒu</param>
        /// <param name="position">�p�[�T���}�b�`�����ۂ̒l</param>
        public Result(bool success, T value, Position position, Position failedPosition) {
            Success = success;
            Value = value;
            Position = position;
            FailedPosition = failedPosition;
        }

        /// <summary>
        /// other�Ƃ̔�r���s��
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected bool Equals(Result<T> other) {
            if (Success != other.Success) {
                return false;
            }
            if (!Position.Equals(other.Position)) {
                return false;
            }
            var value = Value as IStructuralEquatable;
            if (value != null) {
                return value.Equals(other.Value, StructuralComparisons.StructuralEqualityComparer);
            } else {
                return Object.Equals(Value, other.Value);
            }
        }

        /// <summary>
        /// obj�Ƃ̔�r���s��
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != GetType()) {
                return false;
            }
            return Equals((Result<T>)obj);
        }

        /// <summary>
        /// �C���X�^���X�̃n�b�V���l��Ԃ�
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            unchecked {
                var hashCode = Success.GetHashCode();
                hashCode = (hashCode * 397) ^ Position.GetHashCode();
                hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    /// <summary>
    /// �p�[�T��\���f���Q�[�g
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="target"></param>
    /// <param name="position"></param>
    /// <returns></returns>
    public delegate Result<T> Parser<T>(string target, Position position, Position faledPosition);

    /// <summary>
    /// �p�[�T�R���r�l�[�^
    /// </summary>
    public static class Combinator {

        public static Parser<T> Empty<T>() {
            return (target, position, failedPosition) => new Result<T>(false, default(T), position, Position.Max(failedPosition, position));
        }

        /// <summary>
        /// �P���ȕ�������󗝂���p�[�T�𐶐�
        /// </summary>
        /// <param name="str">�󗝂��镶����</param>
        /// <returns>�p�[�T</returns>
        public static Parser<string> Token(string str) {
            var len = str.Length;
            return (target, position, failedPosition) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                if (position.Index >= target.Length || position.Index + len > target.Length) {
                    return new Result<string>(false, null, position, Position.Max(position,failedPosition));
                } else if (target.Substring(position.Index, len) == str) {
                    return new Result<string>(true, str, position.Inc(str), failedPosition);
                } else {
                    return new Result<string>(false, null, position, Position.Max(position, failedPosition));
                }
            };
        }

        /// <summary>
        /// �p�[�Tparser���󗝂��镶����̌J��Ԃ����󗝂ł���p�[�T�𐶐�����
        /// </summary>
        /// <param name="parser">�p�[�T</param>
        /// <returns>�p�[�T</returns>
        public static Parser<T[]> Many<T>(this Parser<T> parser, int min = -1, int max = -1) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }

            return (target, position, failedPosition) => {
                var result = new List<T>();

                var pos = position;
                for (;;) {
                    var parsed = parser(target, pos, failedPosition);
                    if (!parsed.Success) {
                        break;
                    }
                    result.Add(parsed.Value); // ���ʂ��i�[
                    pos = parsed.Position; // �ǂݎ��ʒu���X�V����
                }
                var ret = result.ToArray();
                if ((min >= 0 && ret.Length < min) || (max >= 0 && ret.Length > max)) {
                    return new Result<T[]>(false, new T[0], position, Position.Max(position, failedPosition));
                } else {
                    return new Result<T[]>(true, result.ToArray(), pos, failedPosition);
                }
            };
        }

        /// <summary>
        /// �p�[�T��parsers��擪���珇�Ɏ󗝂��邩���ׁA�ŏ��Ɏ󗝂����p�[�T�̌��ʂ�Ԃ��p�[�T�𐶐�����
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

            return (target, position, failedPosition) => {
                foreach (var parser in parsers) {
                    Debug.Assert(parser != null);
                    var parsed = parser(target, position,failedPosition);
                    Debug.Assert(parsed != null);
                    if (parsed.Success) {
                        return parsed;
                    }
                }

                return new Result<T>(false, default(T), position, Position.Max(position, failedPosition));
            };
        }

        /// <summary>
        /// �p�[�T��parsers��A�������p�[�T�𐶐�����
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

            return (target, position, failedPosition) => {
                var result = new List<T>();
                foreach (var parser in parsers) {
                    Debug.Assert(parser != null);
                    var parsed = parser(target, position,failedPosition);
                    Debug.Assert(parsed != null);

                    if (parsed.Success) {
                        result.Add(parsed.Value);
                        position = parsed.Position;
                    } else {
                        return new Result<T[]>(false, null, parsed.Position, Position.Max(position, failedPosition));
                    }
                }
                return new Result<T[]>(true, result.ToArray(), position, failedPosition);
            };
        }

        /// <summary>
        /// ���K�\����p����p�[�T�𐶐�����
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static Parser<string> Regex(string pattern, RegexOptions options = 0) {
            if (pattern == null) {
                throw new ArgumentNullException(nameof(pattern));
            }
            Regex regexp;
            try {
                regexp = new Regex("^(?:" + pattern + ")", options);
            } catch (Exception e) {
                throw new ArgumentException(@"Invalid regular expression or options value.", e);
            }

            return (target, position, failedPosition) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                if (position.Index >= target.Length) {
                    return new Result<string>(false, null, position, Position.Max(position, failedPosition));
                }

                var match = regexp.Match(target.Substring(position.Index));
                if (match.Success) {
                    return new Result<string>(true, match.Value, position.Inc(match.Value), failedPosition);
                } else {
                    return new Result<string>(false, null, position, Position.Max(position, failedPosition));
                }
            };
        }


        /// <summary>
        /// �C�ӂ̈ꕶ���Ɉ�v����p�[�T�𐶐�����
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Parser<char> AnyChar() {
            return (target, position, failedPosition) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                if (position.Index >= target.Length) {
                    return new Result<char>(false, default(char), position, Position.Max(position, failedPosition));
                }
                var ch = target[position.Index];
                return new Result<char>(true, ch, position.Inc(ch), failedPosition);
            };
        }

        /// <summary>
        /// str���̈ꕶ���Ɉ�v����p�[�T�𐶐�����
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Parser<char> AnyChar(string str) {
            if (str == null) {
                throw new ArgumentNullException(nameof(str));
            }
            var dict = new HashSet<char>(str.ToCharArray());

            return (target, position, failedPosition) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                if (position.Index >= target.Length) {
                    return new Result<char>(false, default(char), position, Position.Max(position, failedPosition));
                }
                var ch = target[position.Index];
                if (dict.Contains(ch)) {
                    return new Result<char>(true, ch, position.Inc(ch), failedPosition);
                } else {
                    return new Result<char>(false, default(char), position, Position.Max(position, failedPosition));
                }
            };
        }

        /// <summary>
        /// �ċA�p�[�T�p�̒x���]���p�[�T�𐶐�
        /// </summary>
        /// <param name="fn">�x���]������p�[�T</param>
        /// <returns></returns>
        public static Parser<T> Lazy<T>(Func<Parser<T>> fn) {
            if (fn == null) {
                throw new ArgumentNullException(nameof(fn));
            }

            Parser<T> parser = null;
            return (target, position, failedPosition) => {
                if (parser == null) {
                    parser = fn();
                    if (parser == null) {
                        throw new Exception("fn() result is null.");
                    }
                }
                return parser(target, position, failedPosition);
            };
        }

        /// <summary>
        /// �p�[�T���I�v�V�����Ƃ��Ĉ����p�[�T�𐶐�����
        /// </summary>
        /// <param name="parser">�I�v�V�����Ƃ��Ĉ����p�[�T</param>
        /// <returns></returns>
        public static Parser<T> Option<T>(this Parser<T> parser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            return (target, position, failedPosition) => {
                var parsed = parser(target, position, failedPosition);
                if (parsed.Success) {
                    return parsed;
                } else {
                    return new Result<T>(true, default(T), position, failedPosition);
                }
            };
        }

        /// <summary>
        /// �p�[�T����������󗝂����ꍇ�A���̌��ʂɏq��֐�fn��K�p���ĕό`����p�[�T
        /// </summary>
        /// <param name="parser">�]���������p�[�T</param>
        /// <param name="fn">���ʂɓK�p����q��֐�</param>
        /// <returns></returns>
        public static Parser<TOutput> Map<TInput, TOutput>(this Parser<TInput> parser,
            Func<TInput, TOutput> fn) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (fn == null) {
                throw new ArgumentNullException(nameof(fn));
            }

            return (target, position, failedPosition) => {
                var parsed = parser(target, position, failedPosition);
                if (parsed.Success) {
                    return new Result<TOutput>(true, fn(parsed.Value), parsed.Position, parsed.FailedPosition);
                } else {
                    return new Result<TOutput>(false, default(TOutput), position, Position.Max(position, parsed.FailedPosition));
                }
            };
        }

        /// <summary>
        /// �p�[�T�̌��ʂɏq��֐�fn��K�p���ĕ]������p�[�T�𐶐�����B���̃p�[�T�͐�ǂ݂Ƃ��ē��삷��B
        /// </summary>
        /// <param name="parser">�]���������p�[�T</param>
        /// <param name="fn">���ʂɓK�p����q��֐�</param>
        /// <returns></returns>
        public static Parser<T> Filter<T>(this Parser<T> parser,
            Func<T, bool> fn) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            if (fn == null) {
                throw new ArgumentNullException(nameof(fn));
            }

            return (target, position, failedPosition) => {
                var parsed = parser(target, position, failedPosition);
                if (parsed.Success)
                {
                    var ret = fn(parsed.Value);
                    if (ret)
                    {
                        return new Result<T>(true, parsed.Value, parsed.Position, parsed.FailedPosition);
                    } else {
                        return new Result<T>(ret, parsed.Value, parsed.Position, Position.Max(parsed.Position, parsed.FailedPosition));
                    }
                } else {
                    return parsed;
                }
            };
        }

        /// <summary>
        /// �p�[�T�̌��ʂ��^�Ȃ�U���A�U�Ȃ�^��Ԃ��p�[�T�𐶐�����
        /// </summary>
        /// <param name="parser">�]���������p�[�T</param>
        /// <returns></returns>
        public static Parser<T> Not<T>(this Parser<T> parser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }

            return (target, position, failedPosition) => {
                var parsed = parser(target, position, failedPosition);
                if (parsed.Success == false) {
                    return new Result<T>(true, default(T), position, failedPosition);
                } else {
                    return new Result<T>(false, default(T), position, Position.Max(position, failedPosition));
                }
            };
        }

        public static Parser<T2> Then<T1, T2>(this Parser<T1> self, Parser<T2> parser) {
            if (self == null) {
                throw new ArgumentNullException(nameof(self));
            }

            return (target, position, failedPosition) => {
                var parsed = self(target, position, failedPosition);
                if (parsed.Success == false) {
                    return new Result<T2>(false, default(T2), position, Position.Max(position, parsed.FailedPosition));
                }
                return parser(target, parsed.Position, Position.Max(position, parsed.FailedPosition));
            };
        }

        public static Parser<T1[]> Repeat1<T1, T2>(this Parser<T1> self, Parser<T2> separate)
        {
            return 
                from _1 in self
                from _2 in separate.Then(self).Many()
                select new[] {_1}.Concat(_2).ToArray();
        }


        // monad

        public static Parser<T2> Select<T1, T2>(this Parser<T1> parser, Func<T1, T2> selector) {
            return Map(parser, selector);
        }

        public static Parser<T2> SelectMany<T1, T, T2>(this Parser<T1> parser, Func<T1, Parser<T>> selector, Func<T1, T, T2> projector) {
            return (target, position, failedPosition) => {
                var res = parser(target, position, failedPosition);
                if (!res.Success) {
                    return new Result<T2>(false, default(T2), position, Position.Max(position, res.FailedPosition));
                }

                var tmp = selector(res.Value)(target, res.Position, failedPosition);
                if (!tmp.Success) {
                    return new Result<T2>(false, default(T2), position, tmp.FailedPosition);
                }
                return new Result<T2>(true, projector(res.Value, tmp.Value), tmp.Position, tmp.FailedPosition);
            };
        }

        public static Parser<T> Where<T>(this Parser<T> parser, Func<T, bool> selector) {
            return (target, position, failedPosition) => {
                var res = parser(target, position, failedPosition);
                if (!res.Success || !selector(res.Value)) {
                    return new Result<T>(false, default(T), position, res.FailedPosition);
                } else {
                    return new Result<T>(true, res.Value, res.Position, res.FailedPosition);
                }
            };
        }
        public static Parser<T> Just<T>(this Parser<T> parser, T value) {
            return (target, position, failedPosition) => {
                var res = parser(target, position, failedPosition);
                if (!res.Success || !res.Value.Equals(value)) {
                    return new Result<T>(false, default(T), position, res.FailedPosition);
                } else {
                    return new Result<T>(true, res.Value, res.Position, res.FailedPosition);
                }
            };
        }
    }
}
