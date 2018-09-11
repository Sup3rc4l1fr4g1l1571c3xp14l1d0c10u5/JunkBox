using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;

namespace CSCPP {
    public class Program {
        static int Main(string[] args) {



            if (System.Diagnostics.Debugger.IsAttached) {

                UniTextReader.Test();
                IntMaxT.Test();
                return Body(args);
            } else {
                try {
                    using (var sw = new System.IO.StreamWriter(Console.OpenStandardOutput()) { AutoFlush = false }) {
                        Console.SetOut(sw);
                        return Body(args);
                    }
                } catch (Exception e) {
                    Console.Error.WriteLine($@"
--------------------------------------------------------------------
アプリケーションの実行中に例外が発生したためプログラムを終了します。
お手数ですが以下の出力を添えて開発元までご連絡ください。
--------------------------------------------------------------------
■バージョン情報：
{Version.VersionString}
--------------------------------------------------------------------
■起動時の引数：
{string.Join(" ", args)}
--------------------------------------------------------------------
■発生した例外：{e.GetType().Name}
{e.Message}
--------------------------------------------------------------------
■例外発生時のスタックトレース：
{e.StackTrace}
--------------------------------------------------------------------
");
                    return -1;
                }
            }
        }

        private static void UtfTest() {
            UInt16[] utf16;
            TryUtf32ToUtf16(0x10302U, out utf16);
            System.Diagnostics.Debug.Assert(utf16[0] == 0xD800);
            System.Diagnostics.Debug.Assert(utf16[1] == 0xDF02);

            UInt32 utf32;
            TryUtf16ToUtf32(utf16, out utf32);
            System.Diagnostics.Debug.Assert(utf32 == 0x10302U);

            var bytes = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF32.GetString(System.BitConverter.GetBytes(utf32)));
            Byte[] utf8;
            TryUtf32ToUtf8(0x10302U, out utf8);
            System.Diagnostics.Debug.Assert(utf8.Length == bytes.Length);
            for (int i = 0; i < utf8.Length; i++) {
                System.Diagnostics.Debug.Assert(utf8[i] == bytes[i]);
            }
            TryUtf8ToUtf32(utf8, out utf32);
            System.Diagnostics.Debug.Assert(utf32 == 0x10302U);
        }

        private static bool Utf16IsHighSurrogate(ushort utf16) {
            return (utf16 >> 10) == 0x36U;
        }
        private static bool Utf16IsLowSurrogate(ushort utf16) {
            return (utf16 >> 10) == 0x37U;
        }
        private static bool Utf16IsSurrogate(ushort utf16) {
            return Utf16IsHighSurrogate(utf16) || Utf16IsLowSurrogate(utf16);
        }
        private static bool Utf16IsSurrogatePair(ushort hi, ushort lo) {
            return Utf16IsHighSurrogate(hi) && Utf16IsLowSurrogate(lo);
        }

        private static bool TryUtf16ToUtf32(ushort[] utf16, out uint utf32) {
            if (utf16.Length == 1) {
                utf32 = (uint)utf16[0];
                return true;
            } else if (utf16.Length == 2 && (utf16[0] >> 10) == 0x36U && (utf16[1] >> 10) == 0x37U) {
                var w = (utf16[0] >> 6) & ((1U << 4) - 1U);
                var u = (uint)((w + 1U) << 16);
                utf32 = u | (utf16[1] & (uint)((1 << 10) -   1)) | ((uint)(utf16[0] & (1 << 6 - 1)) << 10);
                return true;
            } else {
                utf32 = 0xFFFDU;
                return false;
            }
        }

        private static bool TryUtf32ToUtf16(uint utf32, out ushort[] utf16) {
            if (utf32 < 0x10000U) {
                utf16 = new ushort[] { (ushort)utf32 };
                return true;
            } else if (utf32 < 0x110000U) {
                ushort u = (ushort)((utf32 >> 16) & 0x1FU);
                ushort w = (ushort)((u - 1U) << 6);
                ushort lo = (ushort)((0x37U << 10) |     ((utf32      ) & ((1U << 10) - 1U)));
                ushort hi = (ushort)((0x36U << 10) | w | ((utf32 >> 10) & ((1U <<  6) - 1U)));
                utf16 =new ushort[] { hi, lo };
                return true;
            } else {
                utf16 = new ushort[] { 0xFFFD };
                return false;
            }
        }

        private static bool TryUtf8ToUtf32(byte[] utf8, out UInt32 utf32) {
            if (utf8.Length < 0 || utf8.Length > 6) {
                utf32 = 0xFFFD;
                return false;
            }

            int msbc = 0;
            for (byte v = utf8[0]; (v & 0x80) != 0; v <<= 1) {
                msbc++;
            }
            switch (msbc) {
                case 0: {
                        if (utf8.Length == 1 && ((utf8[0] & 0x80) == 0x00)) {
                            var code = (uint)((utf8[0] & 0x7F) << (6 * 0));
                            if (0x0000 <= code && code <= 0x007F) {
                                utf32 = code;
                                return true;
                            }
                        }
                        goto default;
                }
                case 2: {
                        if (utf8.Length == 2 && ((utf8[0] & 0xE0) == 0xC0) && ((utf8[1] & 0xC0) == 0x80)) {
                            var code = (uint)(((utf8[0] & 0x1F) << (6 * 1)) | ((utf8[1] & 0x3F) << (6 * 0)));
                            if (0x0080 <= code && code <= 0x07FF) {
                                utf32 = code;
                                return true;
                            }
                        }
                        goto default;
                    }
                case 3: {
                        if (utf8.Length == 3 && ((utf8[0] & 0xF0) == 0xE0) && ((utf8[1] & 0xC0) == 0x80) && ((utf8[2] & 0xC0) == 0x80)) {
                            var code = (uint)(((utf8[0] & 0x0F) << (6 * 2)) | ((utf8[1] & 0x3F) << (6 * 1)) | ((utf8[2] & 0x3F) << (6 * 0)));
                            if (0x0800 <= code && code <= 0xFFFF) {
                                utf32 = code;
                                return true;
                            }
                        }
                        goto default;
                    }
                case 4: {
                        if (utf8.Length == 4 && ((utf8[0] & 0xF8) == 0xF0) && ((utf8[1] & 0xC0) == 0x80) && ((utf8[2] & 0xC0) == 0x80) && ((utf8[3] & 0xC0) == 0x80)) {
                            var code = (uint)(((utf8[0] & 0x07) << (6 * 3)) | ((utf8[1] & 0x3F) << (6 * 2)) | ((utf8[2] & 0x3F) << (6 * 1)) | ((utf8[3] & 0x3F) << (6 * 0)));
                            if (0x10000 <= code && code <= 0x1FFFFF) {
                                utf32 = code;
                                return true;
                            }
                        }
                        goto default;
                    }
                case 5: {
                        if (utf8.Length == 5 && ((utf8[0] & 0xFC) == 0xF8) && ((utf8[1] & 0xC0) == 0x80) && ((utf8[2] & 0xC0) == 0x80) && ((utf8[3] & 0xC0) == 0x80) && ((utf8[4] & 0xC0) == 0x80)) {
                            var code = (uint)(((utf8[0] & 0x03) << (6 * 4)) | ((utf8[1] & 0x3F) << (6 * 3)) | ((utf8[2] & 0x3F) << (6 * 2)) | ((utf8[3] & 0x3F) << (6 * 1)) | ((utf8[4] & 0x3F) << (6 * 0)));
                            if (0x200000 <= code && code <= 0x3FFFFFF) {
                                utf32 = code;
                                return true;
                            }
                        }
                        goto default;
                    }
                case 6: {
                        if (utf8.Length == 6 && ((utf8[0] & 0xFE) == 0xFC) && ((utf8[1] & 0xC0) == 0x80) && ((utf8[2] & 0xC0) == 0x80) && ((utf8[3] & 0xC0) == 0x80) && ((utf8[4] & 0xC0) == 0x80) && ((utf8[5] & 0xC0) == 0x80)) {
                            var code = (uint)(((utf8[0] & 0x01) << (6 * 5)) | ((utf8[1] & 0x3F) << (6 * 4)) | ((utf8[2] & 0x3F) << (6 * 3)) | ((utf8[3] & 0x3F) << (6 * 2)) | ((utf8[4] & 0x3F) << (6 * 1)) | ((utf8[5] & 0x3F) << (6 * 0)));
                            if (0x4000000 <= code && code <= 0x7FFFFFFF) {
                                utf32 = code;
                                return true;
                            }
                        }
                        goto default;
                    }
                default:
                    utf32 = 0xFFFD;
                    return false;

            }
        }

        private static bool TryUtf32ToUtf8(uint utf32, out byte[] utf8) {
            if (utf32 <= 0x007F) {
                utf8 = new byte[] { (byte)utf32 };
                return true;
            } else if (utf32 <= 0x07FF) {
                utf8 = new byte[2];
                for (var i = 1; i >= 0; i--) {
                    utf8[i] = (byte)(0x80U | (utf32 & 0x3FU));
                    utf32 >>= 6;
                }
                utf8[0] |= 0x40;
                return true;
            } else if (utf32 <= 0xFFFF) {
                utf8 = new byte[3];
                for (var i = 2; i >= 0; i--) {
                    utf8[i] = (byte)(0x80U | (utf32 & 0x3FU));
                    utf32 >>= 6;
                }
                utf8[0] |= 0x60;
                return true;
            } else if (utf32 <= 0x1FFFFF) {
                utf8 = new byte[4];
                for (var i = 3; i >= 0; i--) {
                    utf8[i] = (byte)(0x80U | (utf32 & 0x3FU));
                    utf32 >>= 6;
                }
                utf8[0] |= 0x70;
                return true;
            } else if (utf32 <= 0x3FFFFFF) {
                utf8 = new byte[5];
                for (var i = 4; i >= 0; i--) {
                    utf8[i] = (byte)(0x80U | (utf32 & 0x3FU));
                    utf32 >>= 6;
                }
                utf8[0] |= 0x78;
                return true;
            } else if (utf32 <= 0x7FFFFFFF) {
                utf8 = new byte[6];
                for (var i = 5; i >= 0; i--) {
                    utf8[i] = (byte)(0x80U | (utf32 & 0x3FU));
                    utf32 >>= 6;
                }
                utf8[0] |= 0x7C;
                return true;
            } else {
                utf32 = 0xFFFD;
                utf8 = new byte[3];
                for (var i = 2; i >= 0; i--) {
                    utf8[i] = (byte)(0x80U | (utf32 & 0x3FU));
                    utf32 >>= 6;
                }
                utf8[0] |= 0x60;
                return false;
            }
        }


        /// <summary>
        /// プログラム本体のディレクトリ
        /// </summary>
        private static string ApplicationDirectory {
            get;
        }

        /// <summary>
        /// デフォルトのシステムヘッダディレクトリ
        /// </summary>
        private static string DefaultSystemHeaderFileDirectory {
            get;
        }

        static Program() {
            // 現在のパス情報などを一括で設定
            var apppath = System.Reflection.Assembly.GetEntryAssembly().Location;
            ApplicationDirectory = System.IO.Path.GetDirectoryName(apppath) ?? ".";
            DefaultSystemHeaderFileDirectory = System.IO.Path.Combine(ApplicationDirectory, "include");
        }

        static int Body(string[] args) {

            // 起動時引数を保存
            CppContext.OriginalArguments = args.ToArray();

            // デフォルトのWarningsを設定
            CppContext.Warnings.Add(Warning.Pedantic);
            CppContext.Warnings.Add(Warning.Trigraphs);
            // CppContext.Warnings.Add(Warning.UndefinedToken);
            // CppContext.Warnings.Add(Warning.UnknownDirectives);
            // CppContext.Warnings.Add(Warning.UnknownPragmas);
            // CppContext.Warnings.Add(Warning.UnusedMacros);
            // CppContext.Warnings.Add(Warning.ImplicitSystemHeaderInclude);

            // デフォルトのFeaturesを設定
            //CppContext.Features.Add(Feature.LineComment);
            //CppContext.Switchs.Add("-C");

            // コマンドラインで指定された #include や #define からコードを生成して格納する領域
            StringBuilder sbDefine = new StringBuilder();

            // 引数を解析
            {
                int i;
                for (i = 0; i < args.Length; i++) {
                    if (args[i].Length == 0 || args[i][0] != '-') {
                        break;
                    }
                    if (args[i].Length < 2) {
                        CppContext.Error("空のオプション引数が与えられました。");
                        return -1;
                    }
                    char c = args[i][1];
                    string param = args[i].Substring(2);
                    switch (c) {
                        case 'D': {
                                string name = param;
                                string value = "1";
                                int idx = name.IndexOf('=');
                                if (idx != -1) {
                                    name = param.Substring(0, idx);
                                    value = param.Substring(idx + 1);
                                }
                                if (string.IsNullOrWhiteSpace(name)) {
                                    CppContext.Error($"-{c} オプションの値 {param} でマクロ名が指定されていません。");
                                } else {
                                    sbDefine.AppendFormat(@"#ifndef {0}", name).AppendLine()
                                    .AppendFormat(@"#define {0} {1}", name, value).AppendLine()
                                    .AppendLine(@"#endif");
                                }
                                break;
                            }
                        case 'U':
                            if (string.IsNullOrWhiteSpace(param)) {
                                CppContext.Error($"-{c} オプションの値 {param} でマクロ名が指定されていません。");
                            } else {
                                sbDefine.AppendFormat(@"#ifdef {0}", param).AppendLine()
                                .AppendFormat(@"#undef {0}", param).AppendLine()
                                .AppendLine(@"#endif");
                            }
                            break;
                        case 'I': {
                                string path;
                                if (param.StartsWith("\"")) {
                                    if (param.EndsWith("\"")) {
                                        path = param.Substring(1, param.Length - 2);
                                    } else {
                                        CppContext.Error($"-{c} オプションの値 {param} の二重引用符の対応が取れません。");
                                        path = param.Substring(1);
                                    }
                                } else {
                                    path = param;
                                }
                                try {
                                    var fullpath = System.IO.Path.GetFullPath(path);
                                    if (System.IO.Directory.Exists(fullpath) == false) {
                                        CppContext.Warning($"-{c} オプションで指定されているディレクトリパス {param} を {fullpath} として解釈しましたがディレクトリが存在しません。");
                                    }
                                    Cpp.add_user_include_path(fullpath);
                                } catch {
                                    CppContext.Error($"-{c} オプションで指定されているディレクトリパス {param} はOSの解釈できない不正なパスです。無視します。");
                                }
                                break;
                            }
                        case 'S':
                            if (string.IsNullOrEmpty(param)) {
                                // 2017/08/07: 仕様変更
                                // -S のみを指定した場合はcscppと同じディレクトリにあるデフォルトの標準ヘッダディレクトリ(include)を追加する。
                                Cpp.add_include_path(DefaultSystemHeaderFileDirectory);
                            } else {
                                string path;
                                if (param.StartsWith("\"")) {
                                    if (param.EndsWith("\"")) {
                                        path = param.Substring(1, param.Length - 2);
                                    } else {
                                        CppContext.Error($"-{c} オプションの値 {param} の二重引用符の対応が取れません。");
                                        path = param.Substring(1);
                                    }
                                } else {
                                    path = param;
                                }
                                try {
                                    var fullpath = System.IO.Path.GetFullPath(path);
                                    if (System.IO.Directory.Exists(fullpath) == false) {
                                        CppContext.Warning($"-{c} オプションで指定されているディレクトリパス {param} を {fullpath} として解釈しましたがディレクトリが存在しません。");
                                    }
                                    Cpp.add_include_path(fullpath);
                                } catch {
                                    CppContext.Error($"-{c} オプションで指定されているディレクトリパス {param} はOSの解釈できない不正なパスです。無視します。");
                                }
                            }
                            break;
                        case 'W':
                            if (param == "All") {
                                foreach (var e in Enum.GetValues(typeof(Warning))) {
                                    CppContext.Warnings.Add((Warning)e);
                                }
                            } else {
                                if (Enum.IsDefined(typeof(Warning), param)) {
                                    CppContext.Warnings.Add((Warning)Enum.Parse(typeof(Warning), param));
                                } else {
                                    CppContext.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'w':
                            if (param == "All") {
                                CppContext.Warnings.Clear();
                            } else {
                                if (Enum.IsDefined(typeof(Warning), param)) {
                                    CppContext.Warnings.Remove(
                                        (Warning)Enum.Parse(typeof(Warning), param));
                                } else {
                                    CppContext.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'F':
                            if (param == "All") {
                                foreach (var e in Enum.GetValues(typeof(Feature))) {
                                    CppContext.Features.Add((Feature)e);
                                }
                            } else {
                                if (Enum.IsDefined(typeof(Feature), param)) {
                                    CppContext.Features.Add((Feature)Enum.Parse(typeof(Feature), param));
                                } else {
                                    CppContext.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'f':
                            if (param == "All") {
                                CppContext.Features.Clear();
                            } else {
                                if (Enum.IsDefined(typeof(Feature), param)) {
                                    CppContext.Features.Remove(
                                        (Feature)Enum.Parse(typeof(Feature), param));
                                } else {
                                    CppContext.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'R':
                            if (param == "All") {
                                foreach (var e in Enum.GetValues(typeof(Report))) {
                                    CppContext.Reports.Add((Report)e);
                                }
                            } else {
                                if (Enum.IsDefined(typeof(Report), param)) {
                                    CppContext.Reports.Add((Report)Enum.Parse(typeof(Report), param));
                                } else {
                                    CppContext.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'r':
                            if (param == "All") {
                                CppContext.Reports.Clear();
                            } else {
                                if (Enum.IsDefined(typeof(Report), param)) {
                                    CppContext.Reports.Remove(
                                        (Report)Enum.Parse(typeof(Report), param));
                                } else {
                                    CppContext.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'i':
                            sbDefine.AppendFormat("#include \"{0}\"", param).AppendLine();
                            break;
                        case 'v':
                            if (param == "All") {
                                foreach (var e in Enum.GetValues(typeof(Verbose))) {
                                    CppContext.Verboses.Add((Verbose)e);
                                }
                            } else {
                                if (Enum.IsDefined(typeof(Verbose), param)) {
                                    CppContext.Verboses.Add((Verbose)Enum.Parse(typeof(Verbose), param));
                                } else {
                                    CppContext.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'V':
                            CppContext.Switchs.Add("-V");
                            break;
                        case 'C':
                            CppContext.Switchs.Add("-C");
                            break;
                        case 'e':
                        case 'E':
                            CppContext.AutoDetectEncoding = c == 'e';

                            if (string.IsNullOrWhiteSpace(param)) {
                                CppContext.DefaultEncoding = System.Text.Encoding.Default;
                            } else {
                                System.Text.Encoding encoding = null;
                                try {
                                    int codepage;
                                    if (int.TryParse(param, out codepage)) {
                                        encoding = System.Text.Encoding.GetEncoding(codepage);
                                    } else {
                                        encoding = System.Text.Encoding.GetEncoding(param);
                                    }
                                } catch {
                                    // エラーであることが判ればよいので何もしない。
                                }
                                if (encoding == null) {
                                    Console.Error.WriteLine($"`-{c}` オプションの値 `{param}` は不正な値です。");
                                } else {
                                    CppContext.DefaultEncoding = encoding;
                                }
                            }
                            break;
                        case 'P':
                            CppContext.Switchs.Add("-P");
                            break;
                        case 'h':
                            Usage();
                            return -1;
                        default:
                            CppContext.Error($"不正なオプション {args[i]} が指定されました。");
                            return -1;
                    }
                }

                // 残りのパラメータをファイルパスと見なすが、プリプロセスする対象は最後に指定された一つだけ。
                for (; i < args.Length; i++) {
                    try {
                        if (System.IO.File.Exists(System.IO.Path.GetFullPath(args[i])) == false) {
                            CppContext.Error($"プリプロセス対象ファイル {args[i]} が見つかりません。無視します。");
                        } else {
                            CppContext.TargetFilePath = System.IO.Path.GetFullPath(args[i]);
                        }
                    } catch {
                        CppContext.Error($"指定されたプリプロセス対象ファイル {args[i]} はOSの解釈できない不正なパスです。無視します。");
                    }

                }
                if (CppContext.TargetFilePath == null) {
                    CppContext.Error("有効なプリプロセス対象ファイルが指定されませんでした。");
                    return -1;
                }
            }

            // バナー表示オプションが指定されていた場合はバナーを表示
            if (CppContext.Switchs.Contains("-V")) {
                ShowBanner();
            }

            #region プリプロセッサと字句解析器を初期化
            Cpp.Init();
            Lex.Init();
            #endregion

            #region プリプロセス処理を実行
            {
                // 字句解析器に指定されたファイルを入力ファイルとして設定する
                Lex.Set(CppContext.TargetFilePath);

                // コマンドラインで指定された #include や #define コードは 入力の先頭で 記述されたファイルが include されたかのように振る舞わせる
                File.stream_push(new File(sbDefine.ToString(), "<command-line>"));

                // 出力器を作成
                CppWriter writer = new CppWriter();
                CppContext.CppWriter = writer;
                for (;;) {
                    // 展開などの処理を行ったトークンを読み取る
                    Token tok = Cpp.ReadToken();

                    if (tok.Kind == Token.TokenKind.EoF) {
                        // 読み取ったトークンがEOFだったので終了
                        break;
                    }

                    writer.Write(tok);
                }
            }

            if (CppContext.Warnings.Contains(Warning.UnusedMacros)) {
                foreach (var macro in Cpp.EnumUnusedMacro()) {
                    var loc = macro.GetFirstPosition();
                    CppContext.Warning(loc, $"定義されたマクロ {macro.GetName()} は一度も参照されていません。");
                }
            }
            #endregion

            if (CppContext.ErrorCount != 0) {
                Console.Error.WriteLine($"{(CppContext.TargetFilePath == "-" ? "標準入力" : CppContext.TargetFilePath)} のプリプロセスに失敗しました。");
            } else {
                Console.Error.WriteLine($"{(CppContext.TargetFilePath == "-" ? "標準入力" : CppContext.TargetFilePath)} のプリプロセスに成功しました。");
            }
            Console.Error.WriteLine($"  エラー: {CppContext.ErrorCount}件");
            Console.Error.WriteLine($"  警告: {CppContext.WarningCount}件");

            if (CppContext.Reports.Any()) {
                var reportFilePath = CppContext.TargetFilePath + ".report.xml";
                Reporting.CreateReport(reportFilePath);
            }
            if (CppContext.Verboses.Contains(Verbose.TraceMacroExpand)) {
                var expandTraceFilePath = (CppContext.TargetFilePath == "-" ? "stdin" : CppContext.TargetFilePath) + ".macro-expand-log.xml";
                try {

                    using (System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create(expandTraceFilePath)) {
                        writer.WriteStartElement("MacroExpandLog");
                        foreach (var log in CppContext.ExpandLog) {
                            writer.WriteStartElement("ExpandInfo");
                            writer.WriteAttributeString("EndColumn", log.Item6.ToString());
                            writer.WriteAttributeString("EndLine", log.Item5.ToString());
                            writer.WriteAttributeString("StartColumn", log.Item4.ToString());
                            writer.WriteAttributeString("StartLine", log.Item3.ToString());
                            writer.WriteAttributeString("DefEndColumn", log.Item2.GetLastPosition().Column.ToString());
                            writer.WriteAttributeString("DefEndLine", log.Item2.GetLastPosition().Line.ToString());
                            writer.WriteAttributeString("DefStartColumn", log.Item2.GetFirstPosition().Column.ToString());
                            writer.WriteAttributeString("DefStartLine", log.Item2.GetFirstPosition().Line.ToString());
                            writer.WriteAttributeString("DefFile", log.Item2.GetFirstPosition().FileName);
                            writer.WriteAttributeString("UseColumn", log.Item1.Position.Column.ToString());
                            writer.WriteAttributeString("UseLine", log.Item1.Position.Line.ToString());
                            writer.WriteAttributeString("UseFile", log.Item1.Position.FileName);
                            writer.WriteAttributeString("Name", log.Item2.GetName());
                            writer.WriteAttributeString("Id", log.Item2.UniqueId.ToString());
                            writer.WriteEndElement(/*ExpandInfo*/);
                        }
                        writer.WriteEndElement(/*MacroExpandLog*/);
                    }
                } catch (System.IO.IOException) {
                    CppContext.Error($"マクロ展開結果 {expandTraceFilePath} の作成に失敗しました。書き込み先のファイルを開いている場合は閉じて再度実行してください。");
                }
            }

            if (CppContext.Reports.Any()) {
            }
            return CppContext.ErrorCount != 0 ? -1 : 0;
        }

        public class CppWriter {
            private string CurrentFile {
                get; set;
            }
            private long CurrentLine {
                get; set;
            }

            private bool BeginOfLine { get; set; } = true;

            public int OutputLine { get; set; } = 1;
            public int OutputColumn { get; set; } = 1;

            private Stack<Tuple<string, long, bool, int, int>> SavedContexts { get; } = new Stack<Tuple<string, long, bool, int, int>>();

            private void WriteLine(string s, bool isDummy) {
                foreach (var ch in s) {
                    if (ch == '\n') {
                        OutputLine++;
                        OutputColumn = 1;
                    } else {
                        OutputColumn++;
                    }
                }
                OutputLine++;
                OutputColumn = 1;
                if (!isDummy) {
                    Console.WriteLine(s);
                }
            }
            private void Write(string s, bool isDummy) {
                foreach (var ch in s) {
                    if (ch == '\n') {
                        OutputLine++;
                        OutputColumn = 1;
                    } else {
                        OutputColumn++;
                    }
                }
                if (!isDummy) {
                    Console.Write(s);
                }
            }

            /// <summary>
            /// 行指令を出力してファイル位置を更新
            /// </summary>
            /// <param name="line"></param>
            /// <param name="path"></param>
            /// <param name="isDummy"></param>
            private void WriteLineDirective(long line, string path, bool isDummy) {
                if (!CppContext.Switchs.Contains("-P")) {
                    if (CppContext.Features.Contains(Feature.OutputGccStyleLineDirective)) {
                        WriteLine($"# {line} \"{path.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"", isDummy);
                    } else {
                        WriteLine($"#line {line} \"{path.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"", isDummy);
                    }
                }
            }

            private static readonly System.Text.RegularExpressions.Regex RegexpReplace = new System.Text.RegularExpressions.Regex(@"(\r\n|\r|\n)", System.Text.RegularExpressions.RegexOptions.Compiled);

            private string ReplaceNewLine(string input) {
                return RegexpReplace.Replace(input, Environment.NewLine);
            }


            /// <summary>
            /// 出力を行う。
            /// </summary>
            /// <param name="tok"></param>
            public Tuple<int, int> Write(Token tok) {
                System.Diagnostics.Debug.Assert(tok.File != null);

                var isDummy = SavedContexts.Any();

                // トークンの前に空白がある場合、その空白を出力
                foreach (var chunk in tok.Space.chunks) {
                    if (BeginOfLine) {
                        if (chunk.Pos.FileName != CurrentFile){ 
                            // ファイル自体が違う場合は #line 指令を挿入して現在位置を更新
                            WriteLineDirective(chunk.Pos.Line, chunk.Pos.FileName, isDummy);
                            CurrentFile = chunk.Pos.FileName;
                            CurrentLine = chunk.Pos.Line;
                        } else {
                            // ファイルは同じだけど行番号が違う場合、改行で埋めて調整
                            if (chunk.Pos.Line < CurrentLine + 5) {
                                for (long j = CurrentLine; j < chunk.Pos.Line; j++) {
                                    if (CppContext.Verboses.Contains(Verbose.TraceOutputLine)) {
                                        Write($"{CurrentLine}:", isDummy);
                                    }
                                    WriteLine("", isDummy);
                                }
                            } else {
                                WriteLineDirective(chunk.Pos.Line, chunk.Pos.FileName, isDummy);
                            }
                            CurrentLine = chunk.Pos.Line;
                        }
                    }

                    //Write(chunk.Space);
                    Write(ReplaceNewLine(chunk.Space), isDummy);
                    // 空白中に改行が含まれるケース＝行を跨ぐブロックコメントの場合なので、#line指令による行補正は行わずに行数のみを更新する
                    CurrentLine += chunk.Space.Count(x => x == '\n');

                    // 行頭情報を更新
                    BeginOfLine = (BeginOfLine && String.IsNullOrEmpty(chunk.Space)) || chunk.Space.EndsWith("\n");
                }

                //if (tok.BeginOfLine) {    // トークンが行頭の場合はファイル位置の調整を実行
                if (BeginOfLine) {  // 現在の出力位置が行頭の場合はファイル位置の調整を実行

                    // トークン出力の準備
                    if (tok.File.Name != CurrentFile) {
                        // ファイル自体が違う場合は #line 指令を挿入
                        WriteLineDirective(tok.Position.Line, tok.Position.FileName, isDummy);
                        CurrentFile = tok.Position.FileName;
                        CurrentLine = tok.Position.Line;
                    } else {
                        // ファイルは同じだけど行番号が違う場合、改行で埋めて調整
                        if (tok.Position.Line < CurrentLine + 5) {
                            for (long j = CurrentLine; j < tok.Position.Line; j++) {
                                if (CppContext.Verboses.Contains(Verbose.TraceOutputLine)) {
                                    Write($"{CurrentLine}:", isDummy);
                                }
                                WriteLine("", isDummy);
                            }
                        } else {
                            WriteLineDirective(tok.Position.Line, tok.File.Name, isDummy);
                        }
                        CurrentLine = tok.Position.Line;
                    }

                    // 行情報を付与
                    if (CppContext.Verboses.Contains(Verbose.TraceOutputLine)) {
                        Write($"{CurrentLine}:", isDummy);
                    }
                } else if (tok.Kind == Token.TokenKind.NewLine) {
                    // トークンが改行文字

                    // 行情報を付与
                    if (CppContext.Verboses.Contains(Verbose.TraceOutputLine)) {
                        Write($"{CurrentLine}:", isDummy);
                    }

                } else {
                    // トークンが行頭でも改行文字でもない
                }


                // トークンを出力する
                if (tok.Kind == Token.TokenKind.NewLine) {
                    var ret = Tuple.Create(OutputLine, OutputColumn);

                    CurrentLine += 1;

                    WriteLine("", isDummy);
                    // 行頭情報を更新
                    BeginOfLine = true;
                    return ret;
                } else {

                    //if (tok.HeadSpace && tok.BeginOfLine == false) {
                    if (tok.HeadSpace && BeginOfLine == false) {
                        Write(" ", isDummy);
                    }
                    var ret = Tuple.Create(OutputLine, OutputColumn);
                    Write(ReplaceNewLine(Token.TokenToStr(tok)), isDummy);
                    if (tok.TailSpace) {
                        Write(" ", isDummy);
                    }
                    BeginOfLine = false;
                    return ret;
                }
            }


            public void EnterDummy() {
                SavedContexts.Push(Tuple.Create(CurrentFile, CurrentLine, BeginOfLine, OutputLine, OutputColumn));
            }
            public void LeaveDummy() {
                var o = SavedContexts.Pop();
                CurrentFile = o.Item1;
                CurrentLine = o.Item2;
                BeginOfLine = o.Item3;
                OutputLine = o.Item4;
                OutputColumn = o.Item5;
            }
        }


        private static void ShowBanner() {
            Console.Error.WriteLine("CsCpp (The C preprocessor in C#) Version {0}", Version.VersionString);
        }

        private static void Usage() {
            ShowBanner();
            Console.Error.Write(@"
Usage: cscpp.exe [options] <source>

options:

-D<macro>
    マクロ <macro> を 1 として定義します。
	以下のコードと等価です。

    #undef <macro>
    #define <macro> 1

-D<macro>=<value>
    マクロ <macro> を <value> として定義します。
	以下のコードと等価です。

    #undef <macro>
    #define <macro> <value>

-U<macro>
    マクロ <macro> の定義を取り消します。
    #undef <macro> と等価です。
    -Uオプションと -Dオプションは指定順序に従って有効になります。

-I<dir>
    ヘッダファイルを探索する インクルードディレクトリパス にディレクトリ<dir>を追加します。
    -Iオプションは、指定順序で有効になります。

-S
    システムヘッダファイルを探索する システムインクルードディレクトリパス に cscpp が提供する標準Cライブラリのヘッダファイルが格納されているディレクトリ(cscpp.exeと同じディレクトリ内にあるincludeフォルダ)を追加します。
    このヘッダファイルは最低限の標準Cライブラリ関数を宣言しているものになります。
    現在の環境では " + DefaultSystemHeaderFileDirectory + @" が使われます。
    -Sオプションは、指定順序で有効になります。

-S<dir>
    システムヘッダファイルを探索する システムインクルードディレクトリパス にディレクトリ<dir>を追加します。
    -Sオプションは、指定順序で有効になります。

-W<type>, -w<type>
    -W<type> は <type> で示される警告を有効にします。
    -w<type> は <type> で示される警告を無効にします。

    <type> には以下を指定することができます。

    All
        全ての警告オプションの指定。
    Trigraphs
        トライグラフが出現した場合に警告。
    UndefinedToken
        マクロ評価式中でに未定義の識別子が出現した場合に警告。
    UnusedMacros
        未使用であるマクロについて警告。
    Pedantic
        #else や #endif の後にコメントや空白以外のテキストが出現した場合に警告。
    UnknownPragmas
        認識出来ないプラグマが出現した場合に警告。
    UnknownDirectives
        認識出来ないプリプロセッサ指令が出現した場合に警告。
        本警告は -F オプションの UnknownDirectives と同時に使用する場合に限り有効。
    LineComment
        行コメントが出現した場合に警告。
        行コメントは ISO/IEC 9899-1999 以降で利用可能となった文法です。
        本警告は -F オプションの LineComment と同時に使用する場合に限り有効。
    RedefineMacro
        マクロが同一ではない宣言で再定義された場合に警告。
        「同一」の定義はISO/ICE 9899-1999 6.10.3 Macro replacement (JIS X 3010:2003 6.10.3 マクロ置き換え）に従います。
    ImplicitSystemHeaderInclude
        二重引用符形式で指定されたインクルードファイルがインクルードディレクトリパス中では見つからず、システムインクルードディレクトリパス中から発見された場合に警告。
        この動作自体は ISO/IEC 9899-1999 6.10.2  Source file inclusion (JIS X 3010:2003 6.10.2 ソースファイル取込み) で規定されている標準動作です。
    EmptyMacroArgument
        マクロ関数呼び出しの実引数が空の場合に警告。
        空の実引数は ISO/IEC 9899-1999 以降で利用可能となった標準動作ですが、規格書でも 6.10.3.2 で軽く触れられている程度のため、認知度が低い機能です。
        本警告は -F オプションの EmptyMacroArgument と同時に使用する場合に限り有効。
    VariadicMacro
        可変個引数マクロが宣言された場合に警告。
        可変個引数マクロは ISO/IEC 9899-1999 以降で利用可能となった文法です。
        本警告は -F オプションの VariadicMacro と同時に使用する場合に限り有効。
    LongLongConstant
        64bit整数型の定数値が出現した場合に警告。
        64bit整数型は ISO/IEC 9899-1999 以降で利用可能となった新しい機能です。
        本警告は -F オプションの LongLongConstant と同時に使用する場合に限り有効。
    Error
        警告をエラーとして扱う。
    CertCCodingStandard
        CERT C Coding Standard (CERT C コーディングスタンダード)の 01 プリプロセッサ (PRE) ルールに抵触する場合に警告。
        ルールへの対応/実装状況は以下のとおりです。(○：完全対応  △：部分対応  ×：未対応)
            [×] PRE00-C: 関数形式マクロよりもインライン関数やスタティック関数を使う
            [△] PRE01-C: マクロ内の引数名は括弧で囲む (例外には非対応)
            [△] PRE02-C: マクロ置換リストは括弧で囲む (例外2には非対応)
            [×] PRE03-C: ポインタ型でない型をエンコードするには define よりも typedef を選ぶ
            [△] PRE04-C: 標準ヘッダファイル名を再利用しない
            [×] PRE05-C: 字句の結合や文字列化を行う際のマクロ置換動作をよく理解する
            [×] PRE06-C: ヘッダファイルはインクルードガードで囲む　
            [×] PRE07-C: ""??"" の繰り返しは避ける
            [△] PRE08-C: ヘッダファイル名が一意であることを保証する
            [×] PRE09-C: セキュアな関数を非推奨関数や時代遅れの関数に置き換えない
            [×] PRE10-C: 複数の文からなるマクロは do-while ループで包む
            [△] PRE11-C: マクロ定義をセミコロンで終端しない
            [×] PRE12-C: 安全でないマクロを定義しない
            [×] PRE13-C: あらかじめ定義された標準マクロで準拠規格やバージョンを確認する
            [×] PRE30-C: 文字列連結によってユニバーサル文字名を作成しない
            [×] PRE31-C: 安全でないマクロの引数では副作用を避ける
            [△] PRE32-C: 関数形式マクロの呼出しのなかで前処理指令を使用しない
    UnspecifiedBehavior
        ISO/IEC 9899-1999で示されている以下の未規定動作について警告を出力します。
            - ISO/IEC 9899-1999 6.10.3.2 意味規則: #演算子及び##演算子の評価順序は，未規定とする。

-F<type>, -f<type>
    -F<type> は <type> で示される拡張機能を有効にします。
    -f<type> は <type> で示される拡張機能を無効にします。

    <type> には以下を指定することができます。

    All
        全ての拡張機能の指定。
    Trigraphs
        トライグラフを有効にします。
    UnknownDirectives
        認識出来ないプリプロセッサ指令をエラー扱いせず、プリプロセス結果にそのまま出力します。
    OutputGccStyleLineDirective
        前処理結果に出力する行番号情報を GCC 形式の書式にします。
    LineComment
        ISO/IEC 9899-1999 で導入された行コメント記法を有効にします。
    EmptyMacroArgument
        ISO/IEC 9899-1999 で導入されたマクロ関数呼び出しでの空の実引数の利用を有効にします。
    VariadicMacro 
        ISO/IEC 9899-1999 で導入された可変個引数マクロを有効にします。
    ExtensionForVariadicMacro
        gcc や clang でサポートされている可変個引数マクロの末尾コンマに対する拡張を有効にします。
        これは ISO/IEC 9899-1999 では定義されていない非標準の拡張です。

-R<type>, -r<type>
    -R<type> は <type> で示されるレポート機能を有効にします。
    -r<type> は <type> で示されるレポート機能を無効にします。

	レポートは `入力ファイル名.report.xml` という名前の Excel XML Spreadsheet 形式ファイルとして、入力ファイルと同じディレクトリ内に作成されます。

    <type> には以下を指定することができます。

    All
        全てのレポート機能の指定。
    GeneralInfo
        プリプロセスの全体情報を出力します。
    MacroInfo
        プリプロセス中のマクロ情報を出力します。

-i<file>
    プリプロセス時に<file>をファイル先頭でインクルードします。
    対象ファイルの先頭に #include ""<file>"" と記述した場合と等価です。
    -iオプションは、指定順序で有効になります。

-V
    バージョン情報を表示します。

-v<type>
    <type>で示されるデバッグ用の Verbose（冗長な）出力を有効にします。
    
    <type> には以下を指定することができます。

    All
        全てのVerbose（冗長な）出力機能の指定。
    TraceOutputLine
        プリプロセッサの行出力追跡機能を有効にします。
    TraceMacroExpand
        プリプロセッサのマクロ置換追跡機能を有効にします。
        追跡結果は <ファイル名>.macro-expand-log.xml として生成されます。

-C
    ソース中の有効範囲内のコメントを残します。

-E<encoding>, -e<encoding>
    入力ソースファイルの文字コードを指定します。

    -E<encoding> は 文字コードの自動認識を無効にします。<encoding>には読み取りに用いる文字コードを指定します。
    -e<encoding> は 文字コードの自動認識を有効にします。<encoding>には自動認識に失敗した場合に用いる文字コードを指定します。

    <encoding> には以下のコードページ番号、もしくは文字コード名を指定することができます。
    何も指定しない場合はシステムデフォルト(*付き)のエンコードが使用されます。

" + String.Join(Environment.NewLine, System.Text.Encoding.GetEncodings().Select(x => $"   {(x.CodePage == System.Text.Encoding.Default.CodePage ? "*" : " ")}{x.CodePage,-5:D0} {x.Name}")) + @"

-P
    #line行の出力を無効にします。
");
        }
    }


    class UniTextReader {
        private readonly Stream _inputStream;
        private System.Text.Encoding _inputEncoding;
        private Decoder _inputDecoder;
        private byte[] bytes;
        private char[] chars;
        private byte[] utf32Bytes;
        private int bytesIndex;

        public UniTextReader(System.IO.Stream inputStream, System.Text.Encoding inputEncoding ) {
            this._inputStream = inputStream;
            this._inputEncoding = inputEncoding;
            this._inputDecoder = inputEncoding.GetDecoder();
            this. bytes = new byte[16];
            this.chars = new char[4];
            this.bytesIndex = 0;
            this.utf32Bytes = new byte[4];

        }
        public byte[] ReadRaw() {
            _inputDecoder.Reset();
            for (;;) {
                var data = _inputStream.ReadByte();
                if (data == -1) {
                    return null;
                }
                if (bytes.Length <= bytesIndex) {
                    Array.Resize(ref bytes, bytesIndex + 16);
                }
                bytes[bytesIndex++] = (byte)data;
                var charLen = _inputDecoder.GetCharCount(bytes.ToArray(), 0, bytesIndex);
                if (charLen == 0) {
                    continue;
                }
                if (chars.Length <= charLen) {
                    Array.Resize(ref chars, charLen);
                }
                int bytesUsed, charsUsed;
                bool complete;
                _inputDecoder.Convert(bytes, 0, bytesIndex, chars, 0, 4, true, out bytesUsed, out charsUsed, out complete);
                byte[] ret = bytes.Take(bytesIndex).ToArray();
                if (bytesIndex != bytesUsed) {
                    Array.Copy(bytes, bytesUsed, bytes, 0, bytesIndex - bytesUsed);
                }
                bytesIndex = 0;
                return ret;
            }
        }
        public UInt32 ReadUtf32() {
            _inputDecoder.Reset();
            for (;;) {
                var data = _inputStream.ReadByte();
                if (data == -1) {
                    return UInt32.MaxValue;
                }
                if (bytes.Length <= bytesIndex) {
                    Array.Resize(ref bytes, bytesIndex + 16);
                }
                bytes[bytesIndex++] = (byte)data;
                var charLen = _inputDecoder.GetCharCount(bytes.ToArray(), 0, bytesIndex);
                if (charLen == 0) {
                    continue;
                }
                if (chars.Length <= charLen) {
                    Array.Resize(ref chars, charLen);
                }
                int bytesUsed, charsUsed;
                bool completed;
                _inputDecoder.Convert(bytes, 0, bytesIndex, chars, 0, 4, true, out bytesUsed, out charsUsed, out completed);
                byte[] ret = bytes.Take(bytesIndex).ToArray();
                if (bytesIndex != bytesUsed) {
                    Array.Copy(bytes, bytesUsed, bytes, 0, bytesIndex - bytesUsed);
                }
                bytesIndex = 0;
                System.Text.Encoding.UTF32.GetEncoder().Convert(chars, 0, charsUsed, utf32Bytes, 0, 4, true, out charsUsed, out bytesUsed, out completed);
                return BitConverter.ToUInt32(utf32Bytes, 0);
            }
        }
        public EString ReadEString() {
            _inputDecoder.Reset();
            for (;;) {
                var data = _inputStream.ReadByte();
                if (data == -1) {
                    return null;
                }
                if (bytes.Length <= bytesIndex) {
                    Array.Resize(ref bytes, bytesIndex + 16);
                }
                bytes[bytesIndex++] = (byte)data;
                var charLen = _inputDecoder.GetCharCount(bytes.ToArray(), 0, bytesIndex);
                if (charLen == 0) {
                    continue;
                }
                if (chars.Length <= charLen) {
                    Array.Resize(ref chars, charLen);
                }
                int bytesUsed, charsUsed;
                bool completed;
                _inputDecoder.Convert(bytes, 0, bytesIndex, chars, 0, 4, true, out bytesUsed, out charsUsed, out completed);
                byte[] ret = bytes.Take(bytesIndex).ToArray();
                if (bytesIndex != bytesUsed) {
                    Array.Copy(bytes, bytesUsed, bytes, 0, bytesIndex - bytesUsed);
                }
                bytesIndex = 0;
                System.Text.Encoding.UTF32.GetEncoder().Convert(chars, 0, charsUsed, utf32Bytes, 0, 4, true, out charsUsed, out bytesUsed, out completed);
                var utf32 = BitConverter.ToUInt32(utf32Bytes, 0);
                return new EString(bytes, _inputEncoding);
            }
        }

        public static void Test() {

            {
                //var _inputStream = Console.OpenStandardOutput();
                //var _inputDecoder = Console.InputEncoding.GetDecoder();
                var text1 = @"あいうえお①"+ "\n";
                var testdata1 = System.Text.Encoding.GetEncoding(932).GetBytes(text1);
                var text2 = @"東京特許許可局";
                var testdata2 = System.Text.Encoding.UTF8.GetBytes(text2);
                var _inputStream = new System.IO.MemoryStream(testdata1.Concat(new byte[] { 0xF0, 0x80, 0x80, 0xAF }).ToArray());
                var _inputDecoder = System.Text.Encoding.GetEncoding(932).GetDecoder();

                var reader = new UniTextReader(_inputStream, System.Text.Encoding.GetEncoding(932));
                UInt32 utf32;
                while ((utf32 = reader.ReadUtf32()) != UInt32.MaxValue) {
                    Console.WriteLine($"{utf32:x8}");
                }
            }
        }
    }
}