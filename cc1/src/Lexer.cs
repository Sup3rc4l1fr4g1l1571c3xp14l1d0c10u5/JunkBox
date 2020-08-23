using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnsiCParser {
    /// <summary>
    /// 字句解析器
    /// </summary>
    public class Lexer {

        /// <summary>
        /// line 指令に一致する正規表現。
        /// </summary>
        private static Regex RegexLineDirective { get; } = new Regex(@"^#\s*(line\s+)?(?<line>\d+)\s+""(?<file>(\\""|[^""])*)""(\s+(\d+)){0,3}\s*$");

        /// <summary>
        /// pragma pack 指令に一致する正規表現。
        /// </summary>
        private static Regex RegexPackDirective { get; } = new Regex(@"^#\s*pragma\s+pack\s*(\(\s*((?<packsize>\d+)|(?<op>push)(\s*,\s*(?<packsize>\d+))|(?<op>pop))\s*\)|(?<packsize>\d+))\s*$");//^#\s*(pragma\s+)(pack(\s*\(\s*(?<packsize>[0124])?\s*\)|\s*\s*(?<packsize>[0124])?\s*)?\s*)$");
        // 

        /// <summary>
        /// 10進数文字に一致する正規表現。
        /// </summary>
        private static string D { get; } = @"\d";

        /// <summary>
        /// 16進数文字に一致する正規表現。
        /// </summary>
        private static string H { get; } = @"[a-fA-F0-9]";

        /// <summary>
        /// 指数部に一致する正規表現。
        /// </summary>
        private static string E { get; } = $@"[Ee][+-]?{D}+";

        /// <summary>
        /// 浮動小数点数形式のサフィックスに一致する正規表現。
        /// </summary>
        private static string FS { get; } = @"(?:f|F|l|L)?";

        ///// <summary>
        ///// 整数数形式のサフィックスに一致する正規表現。
        ///// </summary>
        //private static string IS { get; } = @"(?:u|U|l|L)*";

        /// <summary>
        /// 10進浮動小数点形式に一致する正規表現
        /// </summary>
        private static Regex RegexDecimalFloat { get; } = new Regex($@"^(?<Body>{D}+{E}|{D}*\.{D}+(?:{E})?|{D}+\.{D}*({E})?)(?<Suffix>{FS})$", RegexOptions.Compiled);

        /// <summary>
        /// 16進浮動小数点形式に一致する正規表現
        /// </summary>
        private static Regex RegexHeximalFloat { get; } = new Regex($@"^0[xX](?<Fact>(?:{H}*?\.{H}+|{H}+\.?))[pP](?<Exp>[\+\-]?{D}+)(?<Suffix>{FS})$", RegexOptions.Compiled);

        ///// <summary>
        ///// 10進数形式に一致する正規表現
        ///// </summary>
        //private static Regex RegexDecimal { get; } = new Regex($@"^(?<Body>{D}+)(?<Suffix>{IS})$", RegexOptions.Compiled);

        ///// <summary>
        ///// 16進数形式に一致する正規表現
        ///// </summary>
        //private static Regex RegexHeximal { get; } = new Regex($@"^0[xX](?<Body>{H}+)(?<Suffix>{IS})$", RegexOptions.Compiled);

        ///// <summary>
        ///// 8進数形式に一致する正規表現
        ///// </summary>
        //private static Regex RegexOctal { get; } = new Regex($@"^(?<Body>0{D}*)(?<Suffix>{IS})$", RegexOptions.Compiled);

        /// <summary>
        /// 浮動小数点文字列の解析
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Tuple<int, string, string, string> ScanFloat(string str) {
            var m = RegexHeximalFloat.Match(str);
            if (m.Success) {
                return Tuple.Create(16, m.Groups["Fact"].Value, m.Groups["Exp"].Value, String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
            }
            m = RegexDecimalFloat.Match(str);
            if (m.Success) {
                return Tuple.Create(10, m.Groups["Body"].Value, "", String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
            }
            throw new Exception();
        }

        public class NumberScanner {

            public enum FormatType {
                Invalid,
                IntegerLiteral,
                HexLiteral,
                OctalLiteral,
                HexFloatLiteral,
                RealLiteral,
            }

            private static bool IsHexDigit(char ch) {
                return ('0' <= ch && ch <= '9') || ('a' <= ch && ch <= 'f') || ('A' <= ch && ch <= 'F');
            }
            private static bool IsDigit(char ch) {
                return ('0' <= ch && ch <= '9');
            }
            private static bool IsOctalDigit(char ch) {
                return ('0' <= ch && ch <= '7');
            }
            private static bool IsIntSuffix(char ch) {
                return ch == 'u' || ch == 'U' || ch == 'l' || ch == 'L';
            }
            private static bool IsFloatSuffix(char ch) {
                return ch == 'f' || ch == 'F' || ch == 'l' || ch == 'L';
            }

            // 10進浮動小数点形式に一致する正規表現
            //   ^(?<Body>{D}+{E}|{D}*\.{D}+(?:{E})?|{D}+\.{D}*({E})?)(?<Suffix>{FS})$
            // 16進浮動小数点形式に一致する正規表現
            //   ^0[xX](?<Fact>(?:{H}*?\.{H}+|{H}+\.?))[pP](?<Exp>[\+\-]?{D}+)(?<Suffix>{FS})$
            // 10進数形式に一致する正規表現
            //   ^(?<Body>{D}+)(?<Suffix>{IS})$
            // 16進数形式に一致する正規表現
            //   ^0[xX](?<Body>{H}+)(?<Suffix>{IS})$",RegexOptions.Compiled);
            // 8進数形式に一致する正規表現
            //   ^0(?<Body>{D}+)(?<Suffix>{IS})$

            private string _str;
            private int _index;

            private char CurrentCh { get; set; }
            private void NextCh() {
                _index++;
                CurrentCh = _str.Length > _index ? _str[_index] : '\0';
            }

            private bool Many1Digits() {
                if (IsDigit(CurrentCh) == false) {
                    return false;
                }
                while (IsDigit(CurrentCh)) {
                    NextCh();
                }
                return true;
            }
            private void ManyDigits() {
                while (IsDigit(CurrentCh)) {
                    NextCh();
                }
            }
            private bool Many1Hex() {
                if (_str.Length <= _index || IsHexDigit(CurrentCh) == false) {
                    return false;
                }
                while (IsHexDigit(CurrentCh)) {
                    NextCh();
                }
                return true;
            }

            private void ManyHex() {
                while (IsHexDigit(CurrentCh)) {
                    NextCh();
                }
            }

            private void IntSuffix() {
                while (IsIntSuffix(CurrentCh)) {
                    NextCh();
                }
            }

            //[eE][\+\-]?{D}+
            private bool E() {
                if (CurrentCh != 'e' && CurrentCh != 'E') {
                    return false;
                }
                NextCh();

                if (CurrentCh == '+' || CurrentCh == '-') {
                    NextCh();
                }
                //{D}+
                if (Many1Digits() == false) {
                    return false;
                }
                return true;
            }
            private bool OptE() {
                // ([eE][\+\-]?{D}+)?
                if (CurrentCh == 'e' || CurrentCh == 'E') {
                    NextCh();
                    if (CurrentCh == '+' || CurrentCh == '-') {
                        NextCh();
                    }
                    //{D}+
                    if (Many1Digits() == false) {
                        return false;
                    }
                }
                return true;
            }

            public FormatType GetFormat(string s) {
                this._str = s;
                this._index = -1;
                NextCh();

                if (CurrentCh == '0') {
                    NextCh();
                    if (CurrentCh == 'x' || CurrentCh == 'X') {
                        // hex digit or hex float
                        NextCh();
                        if (CurrentCh == '.') {
                            // skip
                            NextCh();
                            // {h}+
                            if (Many1Hex() == false) {
                                return FormatType.Invalid;
                            }
                        } else {
                            // {h}+
                            if (Many1Hex() == false) {
                                return FormatType.Invalid;
                            }

                            if (CurrentCh == '.') {
                                // skip
                                NextCh();
                                // {h}*
                                ManyHex();
                            } else if (CurrentCh != 'p' && CurrentCh != 'P') {
                                // {IS}
                                IntSuffix();
                                return CurrentCh == '\0' ? FormatType.HexLiteral : FormatType.Invalid;
                            }
                        }

                        // [Pp]
                        if (CurrentCh != 'p' && CurrentCh != 'P') {
                            return FormatType.Invalid;
                        }
                        NextCh();

                        // [\+\-]?
                        if (CurrentCh == '+' || CurrentCh == '-') {
                            NextCh();
                        }

                        // {D}+
                        if (Many1Digits() == false) {
                            return FormatType.Invalid;
                        }

                        // {FS}?
                        if (IsFloatSuffix(CurrentCh)) {
                            NextCh();
                        }

                        return CurrentCh == '\0' ? FormatType.HexFloatLiteral : FormatType.Invalid;
                    } else {
                        int notOct = -1;
                        while (IsDigit(CurrentCh)) {
                            if (!IsOctalDigit(CurrentCh) && notOct == -1) {
                                notOct = _index;
                            }
                            NextCh();
                        }
                        if (CurrentCh != '.' && CurrentCh != 'e' && CurrentCh != 'E') {
                            // octal
                            if (notOct != -1) {
                                return FormatType.Invalid;
                            }
                            // {IS}
                            IntSuffix();
                            return CurrentCh == '\0' ? FormatType.OctalLiteral : FormatType.Invalid;
                        } else {
                            // float
                        }
                    }
                } else if (CurrentCh == '.') {
                    // float
                    // \.
                    NextCh();

                    // {D}+
                    if (Many1Digits() == false) {
                        return FormatType.Invalid;
                    }

                    // {E}?
                    if (OptE() == false) {
                        return FormatType.Invalid;
                    }

                    // {FS}
                    if (IsFloatSuffix(CurrentCh)) {
                        NextCh();
                    }
                    return CurrentCh == '\0' ? FormatType.RealLiteral : FormatType.Invalid;

                } else {

                    // int/float
                    // {D}*
                    ManyDigits();
                    if (CurrentCh != '.' && CurrentCh != 'e' && CurrentCh != 'E') {
                        // {IS}*
                        while (IsIntSuffix(CurrentCh)) {
                            NextCh();
                        }
                        return CurrentCh == '\0' ? FormatType.IntegerLiteral : FormatType.Invalid;
                    }
                }
                // float
                if (CurrentCh == '.') {
                    NextCh();
                    // {D}*
                    ManyDigits();

                    // {E}?
                    if (OptE() == false) {
                        return FormatType.Invalid;
                    }

                    // {FS}
                    if (IsFloatSuffix(CurrentCh)) {
                        NextCh();
                    }
                    return CurrentCh == '\0' ? FormatType.RealLiteral : FormatType.Invalid;
                } else if (E()) {
                    // E()

                    // {FS}
                    if (IsFloatSuffix(CurrentCh)) {
                        NextCh();
                    }
                    return CurrentCh == '\0' ? FormatType.RealLiteral : FormatType.Invalid;

                } else {
                    return FormatType.Invalid;
                }

            }

        }

        private readonly NumberScanner _numberScanner = new NumberScanner();

        private static uint MostSignificantBit(ulong x) {
            uint n = 0;
            while (x > 1) {
                x >>= 1;
                n++;
            }
            return n;
        }

        interface IFloatFmt {
            uint Bias { get; }
            uint ExponentSize { get; }
            double Binary2Double(ulong mantissa, ulong exponent);
        }

        struct FloatFmt : IFloatFmt {
            public uint Bias {
                get { return (1 << 8) / 2 - 1; }
            }

            public uint ExponentSize {
                get { return 23; }
            }

            public double Binary2Double(ulong mantissa, ulong exponent) {
                var binary = (uint)(mantissa | (exponent << (int)ExponentSize));
                return BitConverter.ToSingle(BitConverter.GetBytes(binary), 0);
            }
        }

        struct DoubleFmt : IFloatFmt {
            public uint Bias {
                get { return (1 << 11) / 2 - 1; }
            }

            public uint ExponentSize {
                get { return 52; }
            }

            public double Binary2Double(ulong mantissa, ulong exponent) {
                var binary = (mantissa | (exponent << (int)ExponentSize));
                return BitConverter.ToDouble(BitConverter.GetBytes(binary), 0);
            }
        }


        /// <summary>
        /// 16進浮動小数点形式を double 型に変換
        /// </summary>
        /// <param name="range"></param>
        /// <param name="mantissaPart"></param>
        /// <param name="exponentPart"></param>
        /// <param name="suffixPart"></param>
        /// <returns></returns>
        public static double ParseHeximalFloat(LocationRange range, string mantissaPart, string exponentPart, string suffixPart) {
            IFloatFmt fmt = (suffixPart == "f" || suffixPart == "F") ? (IFloatFmt)new FloatFmt() : (IFloatFmt)new DoubleFmt();
            // 小数点の位置
            var dotIndexPos = mantissaPart.IndexOf('.');
            var dotIndex = (dotIndexPos != -1) ? (uint)dotIndexPos : (uint)mantissaPart.Length;

            // 仮数部(小数点を除去)
            mantissaPart = mantissaPart.Replace(".", "");
            var mantissa = ToUInt64(range, mantissaPart, 16);

            // 指数部
            var exponent = ToInt64(range, exponentPart, 10);

            // 仮数部の最左ビット位置
            var msbIndex = MostSignificantBit(mantissa);

            // 小数点のビット位置
            dotIndex = ((uint)mantissaPart.Length - dotIndex) * 4;

            // 仮数部が 1.xxxxxxx になるように指数部を正規化
            exponent = exponent + msbIndex - dotIndex;

            // 仮数部の最左ビット位置をfmt[:exponent_size]にするために必要な左シフト数
            var msbShiftValue = (int)(fmt.ExponentSize - msbIndex);


            // 仮数部の最左ビット位置をfmt[:exponent_size]にしたときの指数部が-126より大きいなら正規化数なのでケチ表現化できる。
            if (exponent + msbShiftValue > -126) {

                // ケチ表現化
                // puts "正規化数なのでケチ表現にする"

                // 仮数部の最左ビット位置がfmt[:exponent_size]になるようにシフトしてマスク
                if (msbShiftValue > 0) {
                    // 左シフト
                    mantissa = mantissa << msbShiftValue;
                } else {
                    // 右シフト
                    mantissa = mantissa >> -msbShiftValue;
                }
                mantissa = mantissa & ((1UL << (int)fmt.ExponentSize) - 1);

                // バイアスを指数に加算
                exponent += fmt.Bias;
            } else {
                // 非正規化数なのでケチ表現化は不可能
                // puts "非正規化数なのでケチ表現化はできない"

                // 仮数部の最左ビット位置がfmt[:exponent_size]になるようにシフト
                if (msbShiftValue > 0) {
                    // 左シフト
                    mantissa = mantissa << msbShiftValue;
                } else {
                    // 右シフト
                    mantissa = mantissa >> -msbShiftValue;
                }
                var shr = -(fmt.Bias - 1) - exponent;
                if (shr > 0) {
                    // 右シフト
                    mantissa = mantissa >> (int)shr;
                } else {
                    // 左シフト
                    mantissa = mantissa >> -(int)shr;
                }
                // 非正規仮数を示す0を設定
                exponent = 0;
            }

            return fmt.Binary2Double(mantissa, (ulong)exponent);
        }

        /// <summary>
        /// 16進数整数の解析
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Tuple<string, string> ParseHeximal(string str) {
            // 正規表現を用いるより手書きの方が高速かつ省メモリだった

            if (str.Length < 3 || str[0] != '0' || (str[1] != 'x' && str[1] != 'X')) {
                throw new Exception(str);
            }

            var i = 2;
            for (;i < str.Length; i++) {
                if (!IsXDigit(str[i])) {
                    break;
                }
            }
            var body = str.Substring(2, i-2);

            var suffix = "";
            for (;i < str.Length; i++) {
                var ch = str[i];
                if (ch == 'u' || ch == 'U') {
                    suffix = suffix + 'U';
                } else if (ch == 'l' || ch == 'L') {
                    suffix = 'L' + suffix;
                } else {
                    break;
                }
            }
            if (body.Length == 0 || i != str.Length) {
                throw new Exception(str);
            }
            return Tuple.Create(body, suffix);
            //var m = RegexHeximal.Match(str);
            //if (m.Success == false) {
            //    throw new Exception();
            //}
            //return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
        }

        /// <summary>
        /// 10進数整数の解析
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Tuple<string, string> ParseDecimal(string str) {
            var i = 0;

            for (;i < str.Length; i++) {
                if (!IsDigit(str[i])) {
                    break;
                }
            }
            var body = str.Substring(0, i);

            var suffix = "";
            for (;i < str.Length; i++) {
                var ch = str[i];
                if (ch == 'u' || ch == 'U') {
                    suffix = suffix + 'U';
                } else if (ch == 'l' || ch == 'L') {
                    suffix = 'L' + suffix;
                } else {
                    break;
                }
            }
            if (body.Length == 0 || i != str.Length) {
                throw new Exception(str);
            }
            return Tuple.Create(body, suffix);
            //var m = RegexDecimal.Match(str);
            //if (m.Success == false) {
            //    throw new Exception();
            //}
            //return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
        }

        /// <summary>
        /// 8進数整数の解析
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Tuple<string, string> ParseOctal(string str) {
            var i = 0;

            for (;i < str.Length; i++) {
                if (!IsOct(str[i])) {
                    break;
                }
            }
            var body = str.Substring(0, i);

            var suffix = "";
            for (;i < str.Length; i++) {
                var ch = str[i];
                if (ch == 'u' || ch == 'U') {
                    suffix = suffix + 'U';
                } else if (ch == 'l' || ch == 'L') {
                    suffix = 'L' + suffix;
                } else {
                    break;
                }
            }
            if (body.Length == 0 || i != str.Length) {
                throw new Exception(str);
            }
            return Tuple.Create(body, suffix);
            //var m = RegexOctal.Match(str);
            //if (!m.Success) {
            //    throw new Exception(str);
            //}
            //return Tuple.Create(m.Groups["Body"].Value, String.Concat(m.Groups["Suffix"].Value.ToCharArray().OrderBy(x => x)));
        }

        /// <summary>
        /// 符号なし整数文字列を数値として読み取る
        /// </summary>
        /// <param name="range"></param>
        /// <param name="s"></param>
        /// <param name="radix"></param>
        /// <returns></returns>
        private static UInt64 Read(LocationRange range, string s, int radix) {
            UInt64 ret = 0;
            switch (radix) {
                case 8:
                    foreach (var ch in s) {
                        if ('0' <= ch && ch <= '7') {
                            ret = ret * 8 + (ulong)(ch - '0');
                        } else {
                            throw new CompilerException.SpecificationErrorException(range, $"八進数に使えない文字{ch}が含まれています。");
                        }
                    }
                    break;
                case 10:
                    foreach (var ch in s) {
                        if ('0' <= ch && ch <= '9') {
                            ret = ret * 10 + (ulong)(ch - '0');
                        } else {
                            throw new CompilerException.SpecificationErrorException(range, $"十進数に使えない文字{ch}が含まれています。");
                        }
                    }
                    break;
                case 16:
                    foreach (var ch in s) {
                        if ('0' <= ch && ch <= '9') {
                            ret = ret * 16 + (ulong)(ch - '0');
                        } else if ('A' <= ch && ch <= 'F') {
                            ret = ret * 16 + (ulong)(ch - 'A' + 10);
                        } else if ('a' <= ch && ch <= 'f') {
                            ret = ret * 16 + (ulong)(ch - 'a' + 10);
                        } else {
                            throw new CompilerException.SpecificationErrorException(range, $"十六進数に使えない文字{ch}が含まれています。");
                        }
                    }
                    break;
                default:
                    throw new CompilerException.SpecificationErrorException(range, $"{radix}は対応していない基数です。");
            }
            return ret;
        }

        /// <summary>
        /// 32bit符号付き数値読み取り (Convert.ToInt32は桁あふれエラーを起こすため)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="s"></param>
        /// <param name="radix"></param>
        /// <returns></returns>
        public static Int32 ToInt32(LocationRange range, string s, int radix) {
            bool neg = false;
            for (; ; ) {
                switch (s[0]) {
                    case '+':
                        s = s.Remove(0, 1);
                        continue;
                    case '-':
                        neg = !neg;
                        s = s.Remove(0, 1);
                        continue;
                    default:
                        break;
                }
                break;
            }

            return unchecked((neg ? -1 : 1) * (Int32)Read(range, s, radix));
        }

        /// <summary>
        /// 32bit符号無し数値読み取り (Convert.ToUInt32は桁あふれエラーを起こすため)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="s"></param>
        /// <param name="radix"></param>
        /// <returns></returns>
        public static UInt32 ToUInt32(LocationRange range, string s, int radix) {
            return unchecked((UInt32)Read(range, s, radix));
        }

        /// <summary>
        /// 64bit符号付き数値読み取り (Convert.ToInt64は桁あふれエラーを起こすため)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="s"></param>
        /// <param name="radix"></param>
        /// <returns></returns>
        public static Int64 ToInt64(LocationRange range, string s, int radix) {
            bool neg = false;
            for (; ; ) {
                switch (s[0]) {
                    case '+':
                        s = s.Remove(0, 1);
                        continue;
                    case '-':
                        neg = !neg;
                        s = s.Remove(0, 1);
                        continue;
                    default:
                        break;
                }
                break;
            }
            return unchecked((neg ? -1 : 1) * (Int64)Read(range, s, radix));
        }

        /// <summary>
        /// 64bit符号無し数値読み取り (Convert.ToUInt64は桁あふれエラーを起こすため)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="s"></param>
        /// <param name="radix"></param>
        /// <returns></returns>
        public static UInt64 ToUInt64(LocationRange range, string s, int radix) {
            return Read(range, s, radix);
        }

        /// <summary>
        /// 文字定数の解析
        /// </summary>
        /// <param name="peek"></param>
        /// <param name="next"></param>
        /// <param name="write"></param>
        public static void CharIterator(Location loc, Func<int> peek, Action next, Action<byte> write) {
            int ret = 0;
            if (peek() == '\\') {
                next();
                switch (peek()) {
                    case '\'':
                    case '"':
                    case '?':
                    case '\\':
                        write((byte)peek());
                        next();
                        return;
                    case 'a':
                        next();
                        write((byte)'\a');
                        return;
                    case 'b':
                        next();
                        write((byte)'\b');
                        return;
                    case 'e':
                        next();
                        write((byte)0x1B);
                        return;
                    case 'f':
                        next();
                        write((byte)'\f');
                        return;
                    case 'n':
                        next();
                        write((byte)'\n');
                        return;
                    case 'r':
                        next();
                        write((byte)'\r');
                        return;
                    case 't':
                        next();
                        write((byte)'\t');
                        return;
                    case 'v':
                        next();
                        write((byte)'\v');
                        return;
                    case 'x': {
                            next();
                            int n = 0;
                            while (IsXDigit(peek())) {
                                ret = (ret << 4) | XDigitToInt(peek());
                                next();
                                n++;
                                if (n == 2) {
                                    write((byte)ret);
                                    n = 0;
                                    ret = 0;
                                }
                            }
                            if (n != 0) {
                                write((byte)ret);
                            }
                            return;
                        }
                    case 'u':
                        next();
                        for (var i = 0; i < 4; i++) {
                            if (IsXDigit(peek()) == false) {
                                throw new CompilerException.SyntaxErrorException(loc, loc, "不正なユニコード文字がありました。");
                            }
                            ret = (ret << 4) | XDigitToInt(peek());
                            next();
                            if ((i % 2) == 1) {
                                write((byte)ret);
                                ret = 0;
                            }
                        }
                        return;
                    case 'U':
                        next();
                        for (var i = 0; i < 8; i++) {
                            if (IsXDigit(peek()) == false) {
                                throw new CompilerException.SyntaxErrorException(loc, loc, "不正なユニコード文字がありました。");
                            }
                            ret = (ret << 4) | XDigitToInt(peek());
                            next();
                            if ((i % 2) == 1) {
                                write((byte)ret);
                                ret = 0;
                            }
                        }
                        return;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                        for (var i = 0; i < 3; i++) {
                            if (IsOct(peek()) == false) {
                                break;
                            }
                            ret = (ret << 3) | (peek() - '0');
                            next();
                        }
                        write((byte)ret);
                        return;
                    default:
                        Logger.Warning(loc, loc, $"不正なエスケープシーケンス '\\{(char)peek()}' がありました。エスケープ文字を無視し、'{(char)peek()}' として読み取ります。");
                        write((byte)peek());
                        next();
                        return;

                }
            } else {
                char ch = (char)peek();
                next();
                var bytes = System.Text.Encoding.UTF8.GetBytes(new[] { ch });
                foreach (var b in bytes) {
                    write(b);
                }
                return;
            }
        }

        /// <summary>
        /// 識別子の先頭に出現できる文字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsIdentifierHead(int ch) {
            return ('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z') || (ch == '_');
        }

        /// <summary>
        /// 識別子の先頭以外に出現できる文字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsIdentifierBody(int ch) {
            return ('A' <= ch && ch <= 'Z') || ('a' <= ch && ch <= 'z') || ('0' <= ch && ch <= '9') || (ch == '_');
        }

        /// <summary>
        /// 八進数字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsOct(int ch) {
            return ('0' <= ch && ch <= '7');
        }

        /// <summary>
        /// 十進数字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsDigit(int ch) {
            return ('0' <= ch && ch <= '9');
        }

        /// <summary>
        /// 十六進数字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsXDigit(int ch) {
            return ('0' <= ch && ch <= '9') || ('A' <= ch && ch <= 'F') || ('a' <= ch && ch <= 'f');
        }

        /// <summary>
        /// 十六進数字をintに
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static int XDigitToInt(int ch) {
            if ('0' <= ch && ch <= '9') {
                return ch - '0';
            }
            if ('A' <= ch && ch <= 'F') {
                return ch - 'A' + 10;
            }
            if ('a' <= ch && ch <= 'f') {
                return ch - 'a' + 10;
            }
            throw new Exception();
        }

        /// <summary>
        /// 空白文字なら真
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static bool IsSpace(int ch) {
            switch (ch) {
                case (int)'\r':
                case (int)'\n':
                case (int)'\v':
                case (int)'\f':
                case (int)'\t':
                case (int)' ':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// ファイル終端なら真
        /// </summary>
        /// <returns></returns>
        public bool IsEof() {
            return _tokens[_currentTokenPos].Kind == Token.TokenKind.EOF;
        }

        /// <summary>
        /// 予約語
        /// </summary>
        private readonly Dictionary<string, Token.TokenKind> _reserveWords = new Dictionary<string, Token.TokenKind>() {
            {"auto", Token.TokenKind.AUTO},
            {"break" , Token.TokenKind.BREAK},
            {"case" , Token.TokenKind.CASE},
            {"char" , Token.TokenKind.CHAR},
            {"const" , Token.TokenKind.CONST},
            {"continue" , Token.TokenKind.CONTINUE},
            {"default" , Token.TokenKind.DEFAULT},
            {"do" , Token.TokenKind.DO},
            {"double" , Token.TokenKind.DOUBLE},
            {"else" , Token.TokenKind.ELSE},
            {"enum" , Token.TokenKind.ENUM},
            {"extern" , Token.TokenKind.EXTERN},
            {"float" , Token.TokenKind.FLOAT},
            {"for" , Token.TokenKind.FOR},
            {"goto" , Token.TokenKind.GOTO},
            {"if" , Token.TokenKind.IF},
            {"int" , Token.TokenKind.INT},
            {"long" , Token.TokenKind.LONG},
            {"register" , Token.TokenKind.REGISTER},
            {"return" , Token.TokenKind.RETURN},
            {"short" , Token.TokenKind.SHORT},
            {"signed" , Token.TokenKind.SIGNED},
            {"sizeof" , Token.TokenKind.SIZEOF},
            {"static" , Token.TokenKind.STATIC},
            {"struct" , Token.TokenKind.STRUCT},
            {"switch" , Token.TokenKind.SWITCH},
            {"typedef" , Token.TokenKind.TYPEDEF},
            {"union" , Token.TokenKind.UNION},
            {"unsigned" , Token.TokenKind.UNSIGNED},
            {"void" , Token.TokenKind.VOID},
            {"volatile" , Token.TokenKind.VOLATILE},
            {"while" , Token.TokenKind.WHILE},
            // c99
            {"inline" , Token.TokenKind.INLINE},
            {"restrict" , Token.TokenKind.RESTRICT},
            {"_Bool" , Token.TokenKind._BOOL},
            {"_Complex" , Token.TokenKind._COMPLEX},
            {"_Imaginary" , Token.TokenKind._IMAGINARY},

            // c11
            {"_Alignas" , Token.TokenKind._Alignas},
            {"_Alignof" , Token.TokenKind._Alignof},
            {"_Noreturn" , Token.TokenKind._Noreturn},
            {"_Static_assert" , Token.TokenKind._Static_assert},

            // special
            {"near" , Token.TokenKind.NEAR},
            {"far" , Token.TokenKind.FAR},
            {"__asm__" , Token.TokenKind.__ASM__},
            {"__volatile__" , Token.TokenKind.__VOLATILE__},
            {"__alignof__" , Token.TokenKind._Alignof},
        };

        /// <summary>
        /// 予約記号
        /// </summary>
        private readonly Tuple<string, Token.TokenKind>[] _symbols = new Tuple<string, Token.TokenKind>[] {
            Tuple.Create("...", Token.TokenKind.ELLIPSIS),
            Tuple.Create(">>=", Token.TokenKind.RIGHT_ASSIGN),
            Tuple.Create("<<=", Token.TokenKind.LEFT_ASSIGN),
            Tuple.Create("+=", Token.TokenKind.ADD_ASSIGN),
            Tuple.Create("-=", Token.TokenKind.SUB_ASSIGN),
            Tuple.Create("*=", Token.TokenKind.MUL_ASSIGN),
            Tuple.Create("/=", Token.TokenKind.DIV_ASSIGN),
            Tuple.Create("%=", Token.TokenKind.MOD_ASSIGN),
            Tuple.Create("&=", Token.TokenKind.AND_ASSIGN),
            Tuple.Create("^=", Token.TokenKind.XOR_ASSIGN),
            Tuple.Create("|=", Token.TokenKind.OR_ASSIGN),
            Tuple.Create(">>", Token.TokenKind.RIGHT_OP),
            Tuple.Create("<<", Token.TokenKind.LEFT_OP),
            Tuple.Create("++", Token.TokenKind.INC_OP),
            Tuple.Create("--", Token.TokenKind.DEC_OP),
            Tuple.Create("->", Token.TokenKind.PTR_OP),
            Tuple.Create("&&", Token.TokenKind.AND_OP),
            Tuple.Create("||", Token.TokenKind.OR_OP),
            Tuple.Create("<=", Token.TokenKind.LE_OP),
            Tuple.Create(">=", Token.TokenKind.GE_OP),
            Tuple.Create("==", Token.TokenKind.EQ_OP),
            Tuple.Create("!=", Token.TokenKind.NE_OP),
            Tuple.Create(";", (Token.TokenKind)';'),
            Tuple.Create("{", (Token.TokenKind)'{'),
            Tuple.Create("<%", (Token.TokenKind)'{'),
            Tuple.Create("}", (Token.TokenKind)'}'),
            Tuple.Create("%>", (Token.TokenKind)'}'),
            Tuple.Create("<:", (Token.TokenKind)'['),
            Tuple.Create(":>", (Token.TokenKind)']'),
            Tuple.Create(",", (Token.TokenKind)','),
            Tuple.Create(":", (Token.TokenKind)':'),
            Tuple.Create("=", (Token.TokenKind)'='),
            Tuple.Create("(", (Token.TokenKind)'('),
            Tuple.Create(")", (Token.TokenKind)')'),
            Tuple.Create("[", (Token.TokenKind)'['),
            Tuple.Create("]", (Token.TokenKind)']'),
            Tuple.Create(".", (Token.TokenKind)'.'),
            Tuple.Create("&", (Token.TokenKind)'&'),
            Tuple.Create("!", (Token.TokenKind)'!'),
            Tuple.Create("~", (Token.TokenKind)'~'),
            Tuple.Create("-", (Token.TokenKind)'-'),
            Tuple.Create("+", (Token.TokenKind)'+'),
            Tuple.Create("*", (Token.TokenKind)'*'),
            Tuple.Create("/", (Token.TokenKind)'/'),
            Tuple.Create("%", (Token.TokenKind)'%'),
            Tuple.Create("<", (Token.TokenKind)'<'),
            Tuple.Create(">", (Token.TokenKind)'>'),
            Tuple.Create("^", (Token.TokenKind)'^'),
            Tuple.Create("|", (Token.TokenKind)'|'),
            Tuple.Create("?", (Token.TokenKind)'?'),
        }.OrderByDescending((x) => x.Item1.Length).ToArray();

        /// <summary>
        /// Ｃソースコード
        /// </summary>
        private readonly string _inputText;

        /// <summary>
        /// ソースコード上の読み取り位置
        /// </summary>
        private int _inputPos;

        /// <summary>
        /// 読み取り位置が行頭の場合には真になる。前処理指令認識用。
        /// </summary>
        private bool _beginOfLine;

        /// <summary>
        /// 入力ファイル名
        /// </summary>
        private string _filepath;

        /// <summary>
        /// 読み取り位置の行番号
        /// </summary>
        private int _line;

        /// <summary>
        /// 読み取り位置の列番号
        /// </summary>
        private int _column;

        /// <summary>
        /// ソースコードから得られたトークンの列
        /// </summary>
        private readonly List<Token> _tokens;

        /// <summary>
        /// 現在のトークンの読み取り位置
        /// </summary>
        private int _currentTokenPos;

        /// <summary>
        /// 読み取り位置情報の保存スタック
        /// </summary>
        private readonly Stack<int> _contextSaveStack = new Stack<int>();


        public Lexer(string source, string filepath = "") {
            _inputText = source;
            _inputPos = 0;
            _beginOfLine = true;
            _filepath = filepath;
            _line = 1;
            _column = 1;
            _tokens = new List<Token>();
            _currentTokenPos = 0;
        }

        /// <summary>
        /// 字句解析器が示す現在のファイル位置情報を取得する
        /// </summary>
        /// <returns></returns>
        private Location GetCurrentLocation() {
            return new Location(_filepath, _line, _column);
        }

        /// <summary>
        /// 字句解析器が示す現在のファイル位置情報と読み取り位置情報を取得する
        /// </summary>
        /// <returns></returns>
        private Tuple<Location, int> GetCurrentLocationWithInputPos() {
            return Tuple.Create(new Location(_filepath, _line, _column), _inputPos);
        }

        /// <summary>
        /// 始点と終点から部分文字列を取得する。
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private string Substring(int start, int end) {
            return _inputText.Substring(start, end - start);
        }

        /// <summary>
        /// ソースコードの読み取り位置をn進める
        /// </summary>
        /// <param name="n"></param>
        private void IncPos(int n) {
            if (n < 0) {
                throw new ArgumentOutOfRangeException($"{nameof(n)}に負数が与えられた。");
            }
            for (var i = 0; i < n; i++) {
                if (_inputText[_inputPos + i] == '\n') {
                    _line++;
                    _column = 1;
                } else {
                    _column++;
                }
            }
            _inputPos += n;
        }

        /// <summary>
        /// ソースコードの現在位置から offset だけ先の文字を先読みする
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        private int Peek(int offset = 0) {
            if (offset < 0) {
                throw new ArgumentOutOfRangeException($"{nameof(offset)}に負数が与えられた。");
            }
            if (_inputPos + offset >= _inputText.Length) {
                return -1;
            } else {
                return _inputText[_inputPos + offset];
            }
        }

        /// <summary>
        /// 現在位置から 文字列 str が先読みできるか調べる
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private bool Peek(string str) {
            // 部分文字列照合だと string.Compare より手書きの方が高速だった
            if (_inputText.Length <= _inputPos + str.Length) {
                return false;
            }
            for (int i = 0, j = _inputPos; i < str.Length; i++, j++) {
                if (_inputText[j] != str[i]) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 現在位置から 文字列 str が先読みできるか調べる
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private bool Peek(char ch) {
            // 部分文字列照合だと string.Compare より手書きの方が高速だった
            if (_inputText.Length <= _inputPos + 1) {
                return false;
            }
            if (_inputText[_inputPos] != ch) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// トークンを一つ読み取って _tokens に入れる。
        /// </summary>
        private void ScanToken() {
            // トークン列の末尾にＥＯＦトークンがある＝すでにファイル終端に到達
            if (_tokens.Count > 0 && _tokens[_tokens.Count - 1].Kind == Token.TokenKind.EOF) {
                // 読み取りを行わずに終わる
                return;
            }
            rescan:
            for (; ; ) {

                // 空白文字の連続の処理
                for (int ch; IsSpace(ch = Peek());) {
                    if (ch == '\n') {
                        _beginOfLine = true;
                    }
                    IncPos(1);
                }

                // ブロックコメントの処理
                if (Peek("/*")) {
                    IncPos(2);

                    bool terminated = false;
                    while (_inputPos < _inputText.Length) {
                        if (Peek('\\')) {
                            IncPos(1);
                            if (Peek('\n')) {
                                _beginOfLine = true;
                            }
                            IncPos(1);
                        } else if (Peek("*/")) {
                            IncPos(2);
                            terminated = true;
                            break;
                        } else {
                            if (Peek('\n')) {
                                _beginOfLine = true;
                            }
                            IncPos(1);
                        }
                    }
                    if (terminated == false) {
                        _tokens.Add(new Token(Token.TokenKind.EOF, GetCurrentLocation(), GetCurrentLocation(), ""));
                        return;
                    }
                    continue;
                }
                // 行コメントの処理
                if (Peek("//")) {
                    if (Settings.LanguageStandard == Settings.CLanguageStandard.C89) {
                        throw new CompilerException.SyntaxErrorException(this.GetCurrentLocation(), this.GetCurrentLocation(), $"行コメントは ISO/IEC 9899:1990 では許可されていません。");
                    } else { 
                        IncPos(2);

                        bool terminated = false;
                        while (_inputPos < _inputText.Length) {
                            int ch = Peek();
                            if (ch == '\\') {
                                IncPos(2);
                            } else if (ch == '\n') {
                                terminated = true;
                                break;
                            } else {
                                IncPos(1);
                            }
                        }
                        if (terminated == false) {
                            _tokens.Add(new Token(Token.TokenKind.EOF, GetCurrentLocation(), GetCurrentLocation(), ""));
                            return;
                        }
                        continue;
                    }
                }
                break;
            }


            // ファイル終端到達の確認
            if (Peek() == -1) {
                // ToDo: ファイル末尾が改行文字でない場合は警告を出力
                _tokens.Add(new Token(Token.TokenKind.EOF, GetCurrentLocation(), GetCurrentLocation(), ""));
                return;
            }

            // 読み取り開始位置を記録
            var start = GetCurrentLocationWithInputPos();

            // 前処理指令の扱い
            if (Peek('#')) {
                if (_beginOfLine) {
                    // 前処理指令（#lineや#pragma等）
                    while (Peek() != -1 && Peek('\n') == false) {
                        IncPos(1);
                    }
                    var end = GetCurrentLocationWithInputPos();
                    var str = Substring(start.Item2, end.Item2);
                    if (Peek() != -1) {
                        IncPos(1);
                    }

                    {
                        // line 指令
                        var match = RegexLineDirective.Match(str);
                        if (match.Success) {
                            _filepath = match.Groups["file"].Value;
                            _line = int.Parse(match.Groups["line"].Value);
                            _column = 1;
                            goto rescan;
                        }
                    }
                    {
                        // pragma pack 指令
                        // #pragma pack ( <packsize> )
                        // #pragma pack ( push, <packsize> )
                        // #pragma pack ( push )
                        // #pragma pack ( pop )
                        // #pragma pack ()
                        // #pragma pack <packsize>
                        // #pragma pack push
                        // #pragma pack pop
                        // #pragma pack

                        // プラグマ後の最初の struct、union、宣言から有効
                        var scanner = new StringScanner(str);
                        if (scanner.Read(new Regex(@"\s*#\s*pragma\s+pack\s*"))) {
                            var quote = false;
                            var op = "";
                            var pack = -1;
                            if (scanner.Read(new Regex(@"\(\s*"))) {
                                quote = true;
                            }
                            Match m = null;
                            if (scanner.Read(new Regex(@"\s*push\s*"))) {
                                op = "push";
                                if (quote && scanner.Read(new Regex(@"\s*,\s*(?<packsize>\d+)\s*"), out m)) {
                                    pack = int.Parse(m.Groups["packsize"].Value);
                                } else {
                                    pack = 0;
                                }
                            } else if (scanner.Read(new Regex(@"\s*pop\s*"))) {
                                op = "pop";
                                pack = 0;
                            } else if (scanner.Read(new Regex(@"\s*(?<packsize>\d+)\s*"), out m)) {
                                pack = int.Parse(m.Groups["packsize"].Value);
                            } else {
                                pack = 0;
                            }

                            if (quote) {
                                if (scanner.Read(new Regex(@"\)\s*")) == false) {
                                    Logger.Warning(start.Item1, end.Item1, "#pragma pack指令の閉じ括弧がありません。pragma全体を読み飛ばします。");
                                    goto rescan;
                                }
                            }
                            if (scanner.IsEoS() == false) {
                                Logger.Warning(start.Item1, end.Item1, "解釈できない書式の指令です。pragma全体を読み飛ばします。");
                            } else {
                                Settings.PackSize = pack;
                            }
                            goto rescan;
                        }
                    }

                    Logger.Warning(start.Item1, end.Item1, "解釈できない書式の指令です。pragma全体を読み飛ばします。");
                    goto rescan;
                } else {
                    throw new CompilerException.SyntaxErrorException(GetCurrentLocation(), GetCurrentLocation(), "前処理指令が出現できない位置に # が存在しています。");
                }
            }

            // トークンを読み取った結果、行頭以外になるので先に行頭フラグを倒しておく
            _beginOfLine = false;

            // 識別子の読み取り
            if (IsIdentifierHead(Peek())) {
                while (IsIdentifierBody(Peek())) {
                    IncPos(1);
                }
                var end = GetCurrentLocationWithInputPos();
                var str = Substring(start.Item2, end.Item2);
                Token.TokenKind kind;
                if (_reserveWords.TryGetValue(str, out kind)) {
                    _tokens.Add(new Token(kind, start.Item1, end.Item1, str));
                } else {
                    _tokens.Add(new Token(Token.TokenKind.IDENTIFIER, start.Item1, end.Item1, str));
                }
                return;
            }

            // 前処理数の読み取り
            if ((Peek(0) == '.' && IsDigit(Peek(1))) || IsDigit(Peek(0))) {
                // 翻訳フェーズの規定に従う場合、定数は前処理数として読み取ってから分類することになる。
                // そのため、0xe-0xe は ["0xe", "-", "0xe"] として正常に読み取ることは誤りで、["0xe-0xe"]として読み取り、サフィックスエラーとしなければならない。

                // \.?\d([eEpP][\+\-]|\.|({L}|{D}|_))*
                if (Peek('.')) {
                    IncPos(1);
                }
                IncPos(1);
                for (int ch; (ch = Peek()) != -1;) {
                    if ("eEpP".IndexOf((char)ch) != -1 && "+-".IndexOf((char)Peek(1)) != -1) {
                        IncPos(2);
                    } else if (ch == '.' || IsIdentifierBody(ch)) {
                        IncPos(1);
                    } else {
                        break;
                    }
                }
                var end = GetCurrentLocationWithInputPos();
                var str = Substring(start.Item2, end.Item2);
#if true
                switch (_numberScanner.GetFormat(str)) {
                    case NumberScanner.FormatType.Invalid:
                        _tokens.Add(new Token(Token.TokenKind.INVALID, start.Item1, end.Item1, str));
                        break;
                    case NumberScanner.FormatType.IntegerLiteral:
                        _tokens.Add(new Token(Token.TokenKind.DECIAML_CONSTANT, start.Item1, end.Item1, str));
                        break;
                    case NumberScanner.FormatType.HexLiteral:
                        _tokens.Add(new Token(Token.TokenKind.HEXIMAL_CONSTANT, start.Item1, end.Item1, str));
                        break;
                    case NumberScanner.FormatType.OctalLiteral:
                        _tokens.Add(new Token(Token.TokenKind.OCTAL_CONSTANT, start.Item1, end.Item1, str));
                        break;
                    case NumberScanner.FormatType.HexFloatLiteral:
                    case NumberScanner.FormatType.RealLiteral:
                        _tokens.Add(new Token(Token.TokenKind.FLOAT_CONSTANT, start.Item1, end.Item1, str));
                        break;
                }
#else
                if (RegexDecimalFloat.IsMatch(str) || RegexHeximalFloat.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.FLOAT_CONSTANT, start.Item1, end.Item1, str));
                } else if (RegexHeximal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.HEXIMAL_CONSTANT, start.Item1, end.Item1, str));
                } else if (RegexOctal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.OCTAL_CONSTANT, start.Item1, end.Item1, str));
                } else if (RegexDecimal.IsMatch(str)) {
                    _tokens.Add(new Token(Token.TokenKind.DECIAML_CONSTANT, start.Item1, end.Item1, str));
                } else {
                    //throw new Exception();
                    _tokens.Add(new Token(Token.TokenKind.INVALID, start.Item1, end.Item1, str));
                }
#endif
                return;
            }

            // 文字定数の読み取り
            // todo : wide char / unicode char
            if (Peek('\'')) {
                IncPos(1);
                while (_inputPos < _inputText.Length) {
                    if (Peek('\'')) {
                        IncPos(1);
                        var end = GetCurrentLocationWithInputPos();
                        var str = Substring(start.Item2, end.Item2);
                        _tokens.Add(new Token(Token.TokenKind.STRING_CONSTANT, start.Item1, end.Item1, str));
                        return;
                    } else {
                        CharIterator(GetCurrentLocation(), () => Peek(), () => IncPos(1), (b) => { });
                    }
                }
                throw new Exception();
            }

            // 文字列リテラルの読み取り
            // todo : wide char / unicode char
            if (Peek('"')) {
                IncPos(1);
                while (_inputPos < _inputText.Length) {
                    if (Peek('"')) {
                        IncPos(1);
                        var end = GetCurrentLocationWithInputPos();
                        var str = Substring(start.Item2, end.Item2);
                        _tokens.Add(new Token(Token.TokenKind.STRING_LITERAL, start.Item1, end.Item1, str));
                        return;
                    } else {
                        CharIterator(GetCurrentLocation(), () => Peek(), () => IncPos(1), (b) => { });
                    }
                }
                throw new Exception();
            }

            // 区切り子の読み取り
            {
                foreach (var sym in _symbols) {
                    if (Peek(sym.Item1)) {
                        IncPos(sym.Item1.Length);
                        var end = GetCurrentLocationWithInputPos();
                        var str = Substring(start.Item2, end.Item2);
                        _tokens.Add(new Token(sym.Item2, start.Item1, end.Item1, str));
                        return;
                    }
                }
            }

            // 不正な文字
            {
                IncPos(1);
                var end = GetCurrentLocationWithInputPos();
                var str = Substring(start.Item2, end.Item2);
                _tokens.Add(new Token(Token.TokenKind.INVALID, start.Item1, end.Item1, str));
                return;

            }

        }

        /// <summary>
        /// 現在の読み取り位置のトークンを得る
        /// </summary>
        /// <returns></returns>
        public Token CurrentToken() {
            if (_tokens.Count == _currentTokenPos) {
                ScanToken();
            }
            return _tokens[_currentTokenPos];
        }

        /// <summary>
        /// トークンを一つ読み進める
        /// </summary>
        public void NextToken() {
            if (_contextSaveStack.Count > 0) {
                _currentTokenPos++;
            } else {
                _tokens.RemoveRange(0, _currentTokenPos + 1);
                _currentTokenPos = 0;
            }

        }

        /// <summary>
        /// 現在のトークンが トークン種別候補 candidates に含まれるなら読み取って返す。
        /// 含まれないなら 例外 SyntaxErrorException を投げる
        /// トークン種別候補 candidates が空の場合はどんなトークンでもいいので一つ読むという動作になる。
        /// </summary>
        /// <param name="candidates"></param>
        public Token ReadToken(params Token.TokenKind[] candidates) {
            var token = CurrentToken();
            var kind = token.Kind;
            foreach (var candidate in candidates) {
                if (candidate == kind) {
                    NextToken();
                    return token;
                }
            }
            throw new CompilerException.SyntaxErrorException(token.Start, token.End, $" {String.Join(", ", candidates.Select(x => (((int)x < 256) ? ((char)x).ToString() : x.ToString())))} があるべき {(CurrentToken().Kind == Token.TokenKind.EOF ? "ですがファイル終端に到達しました。" : $"場所に{CurrentToken().Raw} があります。")} ");
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるなら読み取って返す。
        /// 含まれないなら 例外 SyntaxErrorException を投げる
        /// トークン種別候補 candidates が空の場合はどんなトークンでもいいので一つ読むという動作になる。
        /// </summary>
        /// <param name="candidates"></param>
        public Token ReadToken(params char[] candidates) {
            var token = CurrentToken();
            var kind = token.Kind;
            foreach (var candidate in candidates) {
                if ((Token.TokenKind)candidate == kind) {
                    NextToken();
                    return token;
                }
            }
            throw new CompilerException.SyntaxErrorException(token.Start, token.End, $" {String.Join(", ", candidates.Select(x => (((int)x < 256) ? ((char)x).ToString() : x.ToString())))} があるべき {(CurrentToken().Kind == Token.TokenKind.EOF ? "ですがファイル終端に到達しました。" : $"場所に{CurrentToken().Raw} があります。")} ");
        }


        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べる。。
        /// </summary>
        /// <param name="candidates"></param>
        public bool PeekToken(params Token.TokenKind[] candidates) {
            var kind = CurrentToken().Kind;
            foreach (var candidate in candidates) {
                if (candidate == kind) {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べ、含まれる場合は t にそのトークンの情報を格納する
        /// </summary>
        public bool PeekToken(out Token t, params Token.TokenKind[] candidates) {
            var ct = CurrentToken();
            var kind = ct.Kind;
            foreach (var candidate in candidates) {
                if (candidate == kind) {
                    t = ct;
                    return true;
                }
            }
            t = null;
            return false;
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べる。。
        /// </summary>
        public bool PeekToken(params char[] candidates) {
            var kind = CurrentToken().Kind;
            foreach (var candidate in candidates) {
                if ((Token.TokenKind)candidate == kind) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べ、含まれる場合は t にそのトークンの情報を格納する
        /// </summary>
        public bool PeekToken(out Token t, params char[] candidates) {
            var ct = CurrentToken();
            var kind = ct.Kind;
            foreach (var candidate in candidates) {
                if ((Token.TokenKind)candidate == kind) {
                    t = ct;
                    return true;
                }
            }
            t = null;
            return false;
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べ、含まれる場合は読み飛ばす
        /// </summary>
        public bool ReadTokenIf(params Token.TokenKind[] candidates) {
            if (PeekToken(candidates)) {
                NextToken();
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べ、含まれる場合は読み取り、 t にそのトークンの情報を格納する
        /// </summary>

        public bool ReadTokenIf(out Token t, params Token.TokenKind[] candidates) {
            if (PeekToken(candidates)) {
                t = ReadToken(candidates);
                return true;
            } else {
                t = null;
                return false;
            }
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べ、含まれる場合は読み飛ばす。
        /// </summary>
        public bool ReadTokenIf(params char[] candidates) {
            if (PeekToken(candidates)) {
                NextToken();
                return true;
            } else {
                return false;
            }
        }

        /// <summary>
        /// 現在のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べ、含まれる場合は読み取り、 t にそのトークンの情報を格納する
        /// </summary>
        public bool ReadTokenIf(out Token t, params char[] candidates) {
            if (PeekToken(candidates)) {
                t = ReadToken(candidates);
                return true;
            } else {
                t = null;
                return false;
            }
        }

        /// <summary>
        /// 次のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べる。
        /// </summary>
        public bool PeekNextToken(params Token.TokenKind[] candidates) {
            if (_tokens.Count <= _currentTokenPos + 1) {
                ScanToken();
                if (IsEof()) {
                    return false;
                }
            }
            return candidates.Contains(_tokens[_currentTokenPos + 1].Kind);
        }
        private bool PeekNextToken(IEnumerable<Token.TokenKind> candidates) {
            if (_tokens.Count <= _currentTokenPos + 1) {
                ScanToken();
                if (IsEof()) {
                    return false;
                }
            }
            return candidates.Contains(_tokens[_currentTokenPos + 1].Kind);
        }

        /// <summary>
        /// 次のトークンの種別が トークン種別候補 candidates に含まれるかどうかを調べる。
        /// </summary>
        public bool PeekNextToken(params char[] candidates) {
            return PeekNextToken(candidates.Select(x => (Token.TokenKind)x));
        }

        /// <summary>
        /// 現在の読み取り位置についての情報を保存する
        /// </summary>
        /// <returns></returns>
        public void SaveContext() {
            _contextSaveStack.Push(_currentTokenPos);
        }

        /// <summary>
        /// 現在の読み取り位置について保存した情報を破棄する
        /// </summary>
        /// <returns></returns>
        public void DiscardSavedContext() {
            _contextSaveStack.Pop();
        }

        /// <summary>
        /// 現在の読み取り位置についての情報を復帰する
        /// </summary>
        /// <returns></returns>
        public void RestoreSavedContext() {
            _currentTokenPos = _contextSaveStack.Pop();
        }
    }

    public class StringScanner {
        private readonly string str;
        private string subs;

        public StringScanner(string s) {
            this.str = s;
            this.subs = s;
        }

        public bool Read(string str) {
            if (this.subs.StartsWith(str)) {
                this.subs = this.subs.Substring(str.Length);
                return true;
            } else {
                return false;
            }
        }
        public bool IsEoS() {
            return this.subs.Length == 0;
        }
        public bool Read(Regex regex) {
            Match m;
            return Read(regex, out m);
        }
        public bool Read(Regex regex, out Match m) {
            regex = new Regex("^" + regex.ToString());
            m = regex.Match(this.subs);
            if (m.Success) {
                this.subs = this.subs.Substring(m.Value.Length);
                return true;
            } else {
                return false;
            }
        }
        public bool Peek(string str) {
            if (this.subs.StartsWith(str)) {
                return true;
            } else {
                return false;
            }
        }
        public bool Peek(Regex regex) {
            Match m;
            return Peek(regex, out m);
        }
        public bool Peek(Regex regex, out Match m) {
            regex = new Regex("^" + regex.ToString());
            m = regex.Match(this.subs);
            if (m.Success) {
                return true;
            } else {
                return false;
            }
        }
    }
}
