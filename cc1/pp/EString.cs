using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSCPP {
    public class EString {
        public System.Text.Encoding Encoding {
            get;
        }
        private byte[] data {
            get;
        }
        private int wordSize {
            get;
        }

        public EString(byte[] bytes, System.Text.Encoding encoding = null) {
            encoding = encoding ?? System.Text.Encoding.Default;
            this.Encoding = encoding;
            this.wordSize = (encoding.CodePage == System.Text.Encoding.Unicode.CodePage || encoding.CodePage == System.Text.Encoding.BigEndianUnicode.CodePage) ? 2
                          : (encoding.CodePage == System.Text.Encoding.UTF32.CodePage) ? 4
                          : 1;
            this.data = bytes;

        }

        public EString(string str, System.Text.Encoding encoding = null) {
            encoding = encoding ?? System.Text.Encoding.Default;
            this.Encoding = encoding;
            this.wordSize = (encoding.CodePage == System.Text.Encoding.Unicode.CodePage || encoding.CodePage == System.Text.Encoding.BigEndianUnicode.CodePage) ? 2
                          : (encoding.CodePage == System.Text.Encoding.UTF32.CodePage) ? 4
                          : 1;
            var chars = str.ToCharArray();
            var bytes = new byte[encoding.GetEncoder().GetByteCount(chars, 0, chars.Length, true)];
            encoding.GetEncoder().GetBytes(chars, 0, chars.Length, bytes, 0, true);
            this.data = bytes;

        }

        public static EString FromCStyle(string str, Action<bool, string> errorCallback, System.Text.Encoding encoding = null) {
            encoding = encoding ?? System.Text.Encoding.Default;
            var wordSize = (encoding.CodePage == System.Text.Encoding.Unicode.CodePage || encoding.CodePage == System.Text.Encoding.BigEndianUnicode.CodePage) ? 2
                          : (encoding.CodePage == System.Text.Encoding.UTF32.CodePage) ? 4
                          : 1;
            return new EString(encoding, wordSize, ParseCStyle(encoding, wordSize, str, errorCallback));
        }

        private EString(System.Text.Encoding encoding, int wordSize, byte[] datas) {
            this.Encoding = encoding;
            this.wordSize = wordSize;
            this.data = datas;
        }
        public EString encode(System.Text.Encoding newEncoding) {
            return new EString(newEncoding, wordSize, System.Text.Encoding.Convert(this.Encoding, newEncoding, this.data));
        }

        public override int GetHashCode() {
            return this.data.GetHashCode();
        }

        public override bool Equals(object obj) {
            return (obj is EString) ? Equals(this, (obj as EString)) : (obj is String) ? Equals(this, (obj as String)) : false;
        }

        public static bool Equals(EString self, EString other) {
            if ((object)other == null) {
                return false;
            }
            if (ReferenceEquals(self, other)) {
                return true;
            }
            return self.data.SequenceEqual(other.data);
        }
        public static bool Equals(EString self, String other) {
            if ((object)other == null) {
                return false;
            }
            var chars = other.ToCharArray();
            var bytes = new byte[self.Encoding.GetEncoder().GetByteCount(chars, 0, chars.Length, true)];
            self.Encoding.GetEncoder().GetBytes(chars, 0, chars.Length, bytes, 0, true);
            return self.data.SequenceEqual(bytes);
        }

        public static bool operator ==(EString self, EString other) {
            return Equals(self, other);
        }
        public static bool operator !=(EString self, EString other) {
            return !Equals(self, other);
        }
        public static bool operator ==(EString self, String other) {
            return Equals(self, other);
        }
        public static bool operator !=(EString self, String other) {
            return !Equals(self, other);
        }

        private IEnumerable<Tuple<int, int>> GetRangeEnumerator() {
            var dec = this.Encoding.GetDecoder();
            char[] chars = new char[2];
            int bytesUsed;
            int charsUsed;
            bool completed;
            var i = 0;
            while (i < this.data.Length) {
                var start = i;
                var n = 1;
                for (;;) {
                    if (this.data.Length < i + n) {
                        yield break;
                    }
                    var c = dec.GetCharCount(this.data, i, n);
                    if (c != 0) {
                        dec.Convert(this.data, i, this.data.Length - i, chars, 0, 2, true, out bytesUsed, out charsUsed, out completed);
                        yield return Tuple.Create(start, bytesUsed);
                        i += bytesUsed;
                        break;
                    }
                    n++;
                }
            }
        }

        public IEnumerable<string> GetCharEnumerator() {
            return GetRangeEnumerator().Select(x => {
                try {
                    return new string(this.Encoding.GetChars(this.data, x.Item1, x.Item2));
                } catch (DecoderFallbackException) {
                    return this.data.Skip(x.Item1).Take(x.Item2).Aggregate(new StringBuilder(@"\x"), (s, y) => s.Append($"{y:2X}")).ToString();
                };
            });
        }
        public IEnumerable<byte[]> GetCodeEnumerator() {
            return GetRangeEnumerator().Select(x => data.Skip(x.Item1).Take(x.Item2).ToArray());
        }

        public string CharAt(int index) {
            return GetCharEnumerator().AsEnumerable().ElementAtOrDefault(index);
        }
        public byte[] CodeAt(int index) {
            return GetCodeEnumerator().AsEnumerable().ElementAtOrDefault(index);
        }

        private static byte[] ParseCStyle(System.Text.Encoding enc, int wordSize, string str, Action<bool, string> errorCallback) {
            List<byte> datas = new List<byte>();
            for (var i = 0; i < str.Length; i++) {
                char ch1 = str[i];

                if (char.IsHighSurrogate(ch1)) {
                    if (i + 1 < str.Length) {
                        char ch2 = str[i + 1];
                        if (char.IsLowSurrogate(ch2)) {
                            i++;
                            datas.AddRange(enc.GetBytes(new[] { ch1, ch2 }));
                            continue;
                        }
                    }
                    datas.AddRange(enc.GetBytes(new[] { ch1 }));
                    continue;
                } else if (ch1 == '\\') {
                    if (i + 1 >= str.Length) {
                        errorCallback(true, "不正なエスケープシーケンス");
                        datas.AddRange(enc.GetBytes(new[] { ch1 }));
                        continue;
                    }

                    char ch2 = str[i + 1];
                    switch (ch2) {
                        case '\'':
                        case '"':
                        case '?':
                        case '\\': {
                                i += 1;
                                datas.AddRange(enc.GetBytes(new char[] { (char)ch2 }));
                                continue;
                            }
                        case 'a':
                            i += 1;
                            datas.AddRange(enc.GetBytes(new char[] { (char)'\a' }));
                            continue;
                        case 'b':
                            i += 1;
                            datas.AddRange(enc.GetBytes(new char[] { (char)'\b' }));
                            continue;
                        case 'f':
                            i += 1;
                            datas.AddRange(enc.GetBytes(new char[] { (char)'\f' }));
                            continue;
                        case 'n':
                            i += 1;
                            datas.AddRange(enc.GetBytes(new char[] { (char)'\n' }));
                            continue;
                        case 'r':
                            i += 1;
                            datas.AddRange(enc.GetBytes(new char[] { (char)'\r' }));
                            continue;
                        case 't':
                            i += 1;
                            datas.AddRange(enc.GetBytes(new char[] { (char)'\t' }));
                            continue;
                        case 'v':
                            i += 1;
                            datas.AddRange(enc.GetBytes(new char[] { (char)'\v' }));
                            continue;
                        case 'x': {
                                if (i + 2 >= str.Length) {
                                    errorCallback(false, $"\\x に続く文字がありません。\\xを x として読みます。");
                                    i += 1;
                                    datas.AddRange(enc.GetBytes(new[] { (char)'x' }));
                                    continue;
                                }
                                int c3 = str[i + 2];
                                if (!"0123456789ABCDEFabcdef".Contains((char)c3)) {
                                    errorCallback(false, $"\\x に続く文字 {(char)c3} は16進数表記で使える文字ではありません。\\xを x として読みます。");
                                    i += 1;
                                    datas.AddRange(enc.GetBytes(new[] { (char)'x' }));
                                    continue;
                                }

                                {
                                    UInt32 r = 0;
                                    bool over = false;
                                    int j;
                                    for (j = 0; j < wordSize * 2; j++) {
                                        if (i + j + 2 >= str.Length) {
                                            break;
                                        }
                                        if (over == false && r >= 1UL << (8 * wordSize)) {
                                            over = true;
                                            errorCallback(false, $"16進数文字表記 \\{str} は 文字定数の表現範囲({8 * wordSize}bit整数値)を超えます。 ");
                                        }
                                        var c4 = str[i + j + 2];
                                        if ('0' <= c4 && c4 <= '9') {
                                            r = (r << 4) | (UInt32)(c4 - '0');
                                            continue;
                                        }
                                        if ('a' <= c4 && c4 <= 'f') {
                                            r = (r << 4) | (UInt32)(c4 - 'a' + 10);
                                            continue;
                                        }
                                        if ('A' <= c4 && c4 <= 'F') {
                                            r = (r << 4) | (UInt32)(c4 - 'A' + 10);
                                            continue;
                                        }
                                        break;
                                    }
                                    i += j + 1;
                                    datas.AddRange(BitConverter.GetBytes(r).Take(wordSize));
                                    continue;
                                }
                            }
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7': {
                                UInt32 r = 0;
                                int j;
                                for (j = 0; j < 3; j++) {
                                    if (i + j + 1 >= str.Length) {
                                        break;
                                    }
                                    int c3 = str[i + j + 1];
                                    if ('0' <= c3 && c3 <= '7') {
                                        r = (r << 3) | (UInt32)(c3 - '0');
                                    } else {
                                        break;
                                    }
                                }
                                i += j;
                                datas.Add((byte)r);
                                continue;
                            }
                        default: {
                                i += 1;
                                datas.AddRange(enc.GetBytes(new[] { ch2 }));
                                continue;
                            }
                    }


                } else {
                    datas.AddRange(enc.GetBytes(new[] { ch1 }));
                    continue;
                }
            }
            return datas.ToArray();
        }
    }

}
