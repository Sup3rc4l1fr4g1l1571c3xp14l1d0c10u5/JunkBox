using System;
using System.IO;
using System.Collections.Generic;

namespace CSCPP {
    /// <summary>
    /// UTF32文字
    /// </summary>
    public struct Utf32Char {
        /// <summary>
        /// EOF相当値
        /// </summary>
        private const uint EoFValue = uint.MaxValue;

        /// <summary>
        /// 位置情報
        /// </summary>
        public Position Position { get; }

        /// <summary>
        /// 文字コード(UTF32)
        /// </summary>
        public uint Code { get; }

        /// <summary>
        /// 文字コードに対応する文字（C#の内部表現がUTF16なので文字列としている）
        /// </summary>
        private string StringValue { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="position">位置情報</param>
        /// <param name="value">文字コード</param>
        public Utf32Char(Position position, uint value) {
            Position = position;
            Code = value;
            if (value == EoFValue) {
                StringValue = "";
            } else {
                StringValue = System.Text.Encoding.UTF32.GetString(BitConverter.GetBytes(Code));
            }
        }

        public Utf32Char(Position position, char value) {
            Position = position;
            StringValue = $"{value}";
            var bytes = System.Text.Encoding.UTF32.GetBytes(StringValue);
            Code = BitConverter.ToUInt32(bytes,0);
        }

        public static explicit operator char(Utf32Char self) {
            if (self.StringValue.Length == 1) {
                return self.StringValue[0];
            } else {
                return unchecked((char)-1);
            }
        }

        public static bool operator ==(Utf32Char self, Char that) {
            return self.StringValue.Length == 1 && self.StringValue[0] == that;
        }
        public static bool operator !=(Utf32Char self, Char that) {
            return self.StringValue.Length != 1 || self.StringValue[0] != that;
        }
        public static bool operator ==(Utf32Char self, string that) {
            return self.ToString() == that;
        }
        public static bool operator !=(Utf32Char self, string that) {
            return self.ToString() != that;
        }
        public override bool Equals(object obj) {
            if (obj is Utf32Char) {
                var that = ((Utf32Char)obj);
                return Object.Equals(Position, that.Position) && (Code == that.Code);
            }
            if (obj is String) {
                var that = ((String)obj);
                return this == that;
            }
            if (obj is Char) {
                var that = ((Char)obj);
                return this == that;
            }
            return false;
        }
        public override int GetHashCode() {
            return ToString().GetHashCode();
        }
        public bool IsEof() {
            return Code == EoFValue;
        }
 
        public override string ToString() {
            return StringValue;
        }

        public bool IsDigit() {
            return (StringValue.Length == 1 && CType.IsDigit(StringValue[0]));
        }
        public bool IsAlpha() {
            return (StringValue.Length == 1 && CType.IsAlpha(StringValue[0]));
        }

        public bool IsOctal() {
            return (StringValue.Length == 1 && CType.IsOctal(StringValue[0]));
        }

        public bool IsWhiteSpace() {
            return (StringValue.Length == 1 && CType.IsWhiteSpace(StringValue[0]));
        }

        public bool IsXdigit() {
            return (StringValue.Length == 1 && CType.IsXdigit(StringValue[0]));
        }

        public bool IsAlNum() {
            return IsAlpha() || IsDigit();
        }
    }

    public class Utf32Reader : IDisposable {

        private IEnumerator<uint> Enumerator { get; set; }
        public const uint EoF = UInt32.MaxValue;
        public const uint BadCharStart = 0x80000000U;
        public const uint BadCharEnd = 0x80000001U;

        private Utf32Reader(IEnumerable<UInt32> e ) {
            Enumerator = e.GetEnumerator();
        }

        public uint Read() {
            if (Enumerator.MoveNext() == false) {
                return UInt32.MaxValue;
            } else {
                return Enumerator.Current;
            }
        }

        private static IEnumerable<UInt32> ReadUtf32FromTextReaderIterator(TextReader tr) {
            using (tr) {
                var dec = System.Text.Encoding.UTF32.GetDecoder();
                for (;;) {
                    var ch1 = tr.Read();
                    if (ch1 == -1) {
                        yield break;
                    } else if (ch1 == 0x0000FFFFU) {
                        yield return BadCharStart;   // Start of BadChar

                        for (;;) {
                            ch1 = tr.Read();
                            if (ch1 == 0x0000FFFFU) {
                                break;
                            }
                            yield return unchecked((UInt32)ch1);
                        }
                        yield return BadCharEnd;   // End   of BadChar
                    } else if (Char.IsSurrogate((char)ch1)) {
                        var ch2 = tr.Peek();
                        if (Char.IsSurrogatePair((char)ch1, (char)ch2)) {
                            tr.Read();
                            var bytes = System.Text.Encoding.UTF32.GetBytes(new[] { (char)ch1, (char)ch2 });
                            yield return BitConverter.ToUInt32(bytes, 0);
                        } else {
                            yield return BadCharStart;   // Start of BadChar
                            yield return unchecked((UInt32)ch1);
                            yield return BadCharEnd;   // End   of BadChar
                        }
                    } else {
                        yield return unchecked((UInt32)ch1);
                    }
                }
            }
        }

        internal static Utf32Reader FromTextReader(TextReader tr) {
            return new Utf32Reader(ReadUtf32FromTextReaderIterator(tr));
        }

        #region IDisposable Support
        private bool _disposedValue; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    // マネージオブジェクトを破棄
                    if (Enumerator != null) {
                        Enumerator.Dispose();
                        Enumerator = null;
                    }
                }
                _disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        public void Close() {
            Dispose(true);
        }
        #endregion
    }
}