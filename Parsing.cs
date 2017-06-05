using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Parsing {
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
        public int Position { get; }

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
        public Result(bool success, T value, int position) {
            Success = success;
            Value = value;
            Position = position;
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
            if (Position != other.Position) {
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
                hashCode = (hashCode * 397) ^ Position;
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
    public delegate Result<T> Parser<T>(string target, int position);

    /// <summary>
    /// �p�[�T�R���r�l�[�^
    /// </summary>
    public static class Combinator {

        public static Parser<T> Empty<T>() {
            return (target, position) => new Result<T>(false, default(T), position);
        }

        /// <summary>
        /// �P���ȕ�������󗝂���p�[�T�𐶐�
        /// </summary>
        /// <param name="str">�󗝂��镶����</param>
        /// <returns>�p�[�T</returns>
        public static Parser<string> Token(string str) {
            var len = str.Length;
            return (target, position) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                if (position >= target.Length || position + len > target.Length) {
                    return new Result<string>(false, null, position);
                } else if (target.Substring(position, len) == str) {
                    return new Result<string>(true, str, position + len);
                } else {
                    return new Result<string>(false, null, position);
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

            return (target, position) => {
                var result = new List<T>();

                var pos = position;
                for (;;) {
                    var parsed = parser(target, pos);
                    if (!parsed.Success) {
                        break;
                    }
                    result.Add(parsed.Value); // ���ʂ��i�[
                    pos = parsed.Position; // �ǂݎ��ʒu���X�V����
                }
                var ret = result.ToArray();
                if ((min >= 0 && ret.Length < min) || (max >= 0 && ret.Length > max))
                {
                    return new Result<T[]>(false, new T[0], position);
                } else
                {
                    return new Result<T[]>(true, result.ToArray(), pos);
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

            return (target, position) => {
                foreach (var parser in parsers) {
                    Debug.Assert(parser != null);
                    var parsed = parser(target, position);
                    Debug.Assert(parsed != null);
                    if (parsed.Success) {
                        return parsed;
                    }
                }

                return new Result<T>(false, default(T), position);
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

            return (target, position) => {
                var result = new List<T>();
                foreach (var parser in parsers) {
                    Debug.Assert(parser != null);
                    var parsed = parser(target, position);
                    Debug.Assert(parsed != null);

                    if (parsed.Success) {
                        result.Add(parsed.Value);
                        position = parsed.Position;
                    } else {
                        return new Result<T[]>(false, null, parsed.Position);
                    }
                }
                return new Result<T[]>(true, result.ToArray(), position);
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

            return (target, position) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                if (position >= target.Length) {
                    return new Result<string>(false, null, position);
                }

                var match = regexp.Match(target.Substring(position));
                if (match.Success) {
                    position += match.Length;
                    return new Result<string>(true, match.Value, position);
                } else {
                    return new Result<string>(false, null, position);
                }
            };
        }

        /// <summary>
        /// str���̈ꕶ���Ɉ�v����p�[�T�𐶐�����
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Parser<string> Char(string str) {
            if (str == null) {
                throw new ArgumentNullException(nameof(str));
            }
            var dict = new HashSet<char>(str.ToCharArray());

            return (target, position) => {
                if (target == null) {
                    throw new ArgumentNullException(nameof(target));
                }
                if (position >= target.Length) {
                    return new Result<string>(false, null, position);
                }
                var ch = target[position];
                if (dict.Contains(ch)) {
                    return new Result<string>(true, $"{ch}", position + 1);
                } else {
                    return new Result<string>(false, null, position);
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
            return (target, position) => {
                if (parser == null) {
                    parser = fn();
                    if (parser == null) {
                        throw new Exception("fn() result is null.");
                    }
                }
                return parser(target, position);
            };
        }

        /// <summary>
        /// �p�[�T���I�v�V�����Ƃ��Ĉ����p�[�T�𐶐�����
        /// </summary>
        /// <param name="parser">�I�v�V�����Ƃ��Ĉ����p�[�T</param>
        /// <returns></returns>
        public static Parser<T> Option<T>(Parser<T> parser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }
            return (target, position) => {
                var parsed = parser(target, position);
                if (parsed.Success) {
                    return parsed;
                } else {
                    return new Result<T>(true, default(T), position);
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

            return (target, position) => {
                var parsed = parser(target, position);
                if (parsed.Success) {
                    return new Result<TOutput>(parsed.Success, fn(parsed.Value), parsed.Position);
                } else {
                    return new Result<TOutput>(false, default(TOutput), position);
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

            return (target, position) => {
                var parsed = parser(target, position);
                if (parsed.Success) {
                    return new Result<T>(fn(parsed.Value), parsed.Value, parsed.Position);
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
        public static Parser<T> Not<T>(Parser<T> parser) {
            if (parser == null) {
                throw new ArgumentNullException(nameof(parser));
            }

            return (target, position) => {
                var parsed = parser(target, position);
                if (parsed.Success == false)
                {
                    return new Result<T>(true, default(T), position);
                }
                else
                {
                    return new Result<T>(false, default(T), position);
                }
            };
        }

        public static Parser<T2> Then<T1, T2>(this Parser<T1> self, Parser<T2> parser) {
            if (self == null) {
                throw new ArgumentNullException(nameof(self));
            }

            return (target, position) => {
                var parsed = self(target, position);
                if (parsed.Success == false) {
                    return new Result<T2>(false, default(T2), position);
                }
                return parser(target, parsed.Position);
            };
        }

        public static Parser<T2> Select<T1, T2>(this Parser<T1> parser, Func<T1, T2> selector) {
            return (target, position) => {
                var res = parser(target, position);
                if (!res.Success) {
                    return new Result<T2>(false, default(T2), position);
                } else {
                    return new Result<T2>(true, selector(res.Value), res.Position);
                }
            };
        }

        public static Parser<T2> SelectMany<T1, T, T2>(this Parser<T1> parser, Func<T1, Parser<T>> selector, Func<T1, T, T2> projector) {
            return (target, position) => {
                var res = parser(target, position);
                if (!res.Success) {
                    return new Result<T2>(false, default(T2), position);
                }

                var tmp = selector(res.Value)(target, res.Position);
                if (!tmp.Success) {
                    return new Result<T2>(false, default(T2), position);
                }
                return new Result<T2>(true, projector(res.Value, tmp.Value), tmp.Position);
            };
        }

        public static Parser<T> Where<T>(this Parser<T> parser, Func<T, bool> selector) {
            return (target, position) => {
                var res = parser(target, position);
                if (!res.Success || !selector(res.Value)) {
                    return new Result<T>(false, default(T), position);
                } else {
                    return new Result<T>(true, res.Value, res.Position);
                }
            };
        }
    }
}
