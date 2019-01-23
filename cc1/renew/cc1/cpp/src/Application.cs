using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace CSCPP
{
    public class Application : ApplicationBase
    {

        /// <summary>
        /// デフォルトのシステムヘッダディレクトリ
        /// </summary>
        private string DefaultSystemHeaderFileDirectory { get; }

        public Application() : base() {
            DefaultSystemHeaderFileDirectory = System.IO.Path.Combine(this.ApplicationDirectory, "include");
        }

        public override int Main(string[] args) {
            using (var sw = new System.IO.StreamWriter(Console.OpenStandardOutput()) { AutoFlush = false }) {
                Console.SetOut(sw);
                return Body(args);
            }
        }

        public override int DebugMain(string[] args) {
            EncodedTextReader.Test();
            IntMaxT.Test();
            return Body(args);
        }

        private int Body(string[] args) {

            // 起動時引数を保存
            Cpp.OriginalArguments = args.ToArray();

            // デフォルトのWarningsを設定
            Cpp.Warnings.Add(WarningOption.Pedantic);
            Cpp.Warnings.Add(WarningOption.Trigraphs);
            // CppContext.Warnings.Add(WarningOption.UndefinedToken);
            // CppContext.Warnings.Add(WarningOption.UnknownDirectives);
            // CppContext.Warnings.Add(WarningOption.UnknownPragmas);
            // CppContext.Warnings.Add(WarningOption.UnusedMacros);
            // CppContext.Warnings.Add(WarningOption.ImplicitSystemHeaderInclude);

            // デフォルトのFeaturesを設定
            // CppContext.Features.Add(Feature.LineComment);
            // CppContext.Switchs.Add("-C");

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
                        Cpp.Error("空のオプション引数が与えられました。");
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
                                    Cpp.Error($"-{c} オプションの値 {param} でマクロ名が指定されていません。");
                                } else {
                                    sbDefine.AppendLine($"#ifndef {name}")
                                    .AppendLine($"#define {name} {value}")
                                    .AppendLine(@"#endif");
                                }
                                break;
                            }
                        case 'U':
                            if (string.IsNullOrWhiteSpace(param)) {
                                Cpp.Error($"-{c} オプションの値 {param} でマクロ名が指定されていません。");
                            } else {
                                sbDefine.AppendLine($"#ifdef {param}")
                                .AppendLine($"#undef {param}")
                                .AppendLine(@"#endif");
                            }
                            break;
                        case 'I': {
                                string path;
                                if (param.StartsWith("\"")) {
                                    if (param.EndsWith("\"")) {
                                        path = param.Substring(1, param.Length - 2);
                                    } else {
                                        Cpp.Error($"-{c} オプションの値 {param} の二重引用符の対応が取れません。");
                                        path = param.Substring(1);
                                    }
                                } else {
                                    path = param;
                                }
                                try {
                                    var fullpath = System.IO.Path.GetFullPath(path);
                                    if (System.IO.Directory.Exists(fullpath) == false) {
                                        Cpp.Warning($"-{c} オプションで指定されているディレクトリパス {param} を {fullpath} として解釈しましたがディレクトリが存在しません。");
                                    }
                                    Cpp.AddUserIncludePath(fullpath);
                                } catch {
                                    Cpp.Error($"-{c} オプションで指定されているディレクトリパス {param} はOSの解釈できない不正なパスです。無視します。");
                                }
                                break;
                            }
                        case 'S':
                            if (string.IsNullOrEmpty(param)) {
                                // 2017/08/07: 仕様変更
                                // -S のみを指定した場合はcscppと同じディレクトリにあるデフォルトの標準ヘッダディレクトリ(include)を追加する。
                                Cpp.AddSystemIncludePath(DefaultSystemHeaderFileDirectory);
                            } else {
                                string path;
                                if (param.StartsWith("\"")) {
                                    if (param.EndsWith("\"")) {
                                        path = param.Substring(1, param.Length - 2);
                                    } else {
                                        Cpp.Error($"-{c} オプションの値 {param} の二重引用符の対応が取れません。");
                                        path = param.Substring(1);
                                    }
                                } else {
                                    path = param;
                                }
                                try {
                                    var fullpath = System.IO.Path.GetFullPath(path);
                                    if (System.IO.Directory.Exists(fullpath) == false) {
                                        Cpp.Warning($"-{c} オプションで指定されているディレクトリパス {param} を {fullpath} として解釈しましたがディレクトリが存在しません。");
                                    }
                                    Cpp.AddSystemIncludePath(fullpath);
                                } catch {
                                    Cpp.Error($"-{c} オプションで指定されているディレクトリパス {param} はOSの解釈できない不正なパスです。無視します。");
                                }
                            }
                            break;
                        case 'W':
                            if (param == "All") {
                                foreach (var e in Enum.GetValues(typeof(WarningOption))) {
                                    Cpp.Warnings.Add((WarningOption)e);
                                }
                            } else {
                                if (Enum.IsDefined(typeof(WarningOption), param)) {
                                    Cpp.Warnings.Add((WarningOption)Enum.Parse(typeof(WarningOption), param));
                                } else {
                                    Cpp.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'w':
                            if (param == "All") {
                                Cpp.Warnings.Clear();
                            } else {
                                if (Enum.IsDefined(typeof(WarningOption), param)) {
                                    Cpp.Warnings.Remove(
                                        (WarningOption)Enum.Parse(typeof(WarningOption), param));
                                } else {
                                    Cpp.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'F':
                            if (param == "All") {
                                foreach (var e in Enum.GetValues(typeof(FeatureOption))) {
                                    Cpp.Features.Add((FeatureOption)e);
                                }
                            } else {
                                if (Enum.IsDefined(typeof(FeatureOption), param)) {
                                    Cpp.Features.Add((FeatureOption)Enum.Parse(typeof(FeatureOption), param));
                                } else {
                                    Cpp.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'f':
                            if (param == "All") {
                                Cpp.Features.Clear();
                            } else {
                                if (Enum.IsDefined(typeof(FeatureOption), param)) {
                                    Cpp.Features.Remove(
                                        (FeatureOption)Enum.Parse(typeof(FeatureOption), param));
                                } else {
                                    Cpp.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'R':
                            if (param == "All") {
                                foreach (var e in Enum.GetValues(typeof(Report))) {
                                    Cpp.Reports.Add((Report)e);
                                }
                            } else {
                                if (Enum.IsDefined(typeof(Report), param)) {
                                    Cpp.Reports.Add((Report)Enum.Parse(typeof(Report), param));
                                } else {
                                    Cpp.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'r':
                            if (param == "All") {
                                Cpp.Reports.Clear();
                            } else {
                                if (Enum.IsDefined(typeof(Report), param)) {
                                    Cpp.Reports.Remove(
                                        (Report)Enum.Parse(typeof(Report), param));
                                } else {
                                    Cpp.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'i':
                            sbDefine.AppendFormat("#include \"{0}\"", param).AppendLine();
                            break;
                        case 'o':
                            if (Cpp.OutputFilePath == null) {
                                Cpp.OutputFilePath = param;
                            } else {
                                Cpp.Error($"-{c} オプションが複数回指定されました。");
                            }
                            break;
                        case 'l':   
                            if (Cpp.LogFilePath == null) {
                                Cpp.LogFilePath = param;
                            } else {
                                Cpp.Error($"-{c} オプションが複数回指定されました。");
                            }
                            break;
                        case 'v':
                            if (param == "All") {
                                foreach (var e in Enum.GetValues(typeof(VerboseOption))) {
                                    Cpp.Verboses.Add((VerboseOption)e);
                                }
                            } else {
                                if (Enum.IsDefined(typeof(VerboseOption), param)) {
                                    Cpp.Verboses.Add((VerboseOption)Enum.Parse(typeof(VerboseOption), param));
                                } else {
                                    Cpp.Error($"-{c} オプションの値 {param} は不正な値です。");
                                }
                            }
                            break;
                        case 'V':
                            Cpp.Switchs.Add("-V");
                            break;
                        case 'C':
                            Cpp.Switchs.Add("-C");
                            break;
                        case 'e':
                        case 'E':
                            Cpp.AutoDetectEncoding = c == 'e';

                            if (string.IsNullOrWhiteSpace(param)) {
                                Cpp.DefaultEncoding = System.Text.Encoding.Default;
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
                                    Cpp.DefaultEncoding = encoding;
                                }
                            }
                            break;
                        case 'P':
                            Cpp.Switchs.Add("-P");
                            break;
                        case 'h':
                            Usage();
                            return -1;
                        default:
                            Cpp.Error($"不正なオプション {args[i]} が指定されました。");
                            return -1;
                    }
                }

                // 残りのパラメータをファイルパスと見なすが、プリプロセスする対象は最後に指定された一つだけ。
                for (; i < args.Length; i++) {
                    try {
                        if (System.IO.File.Exists(System.IO.Path.GetFullPath(args[i])) == false) {
                            Cpp.Error($"プリプロセス対象ファイル {args[i]} が見つかりません。無視します。");
                        } else {
                            Cpp.TargetFilePath = System.IO.Path.GetFullPath(args[i]);
                        }
                    } catch {
                        Cpp.Error($"指定されたプリプロセス対象ファイル {args[i]} はOSの解釈できない不正なパスです。無視します。");
                    }

                }
                if (Cpp.TargetFilePath == null) {
                    Cpp.Error("有効なプリプロセス対象ファイルが指定されませんでした。");
                    return -1;
                }
                if (Cpp.OutputFilePath != null) {
                    if (String.IsNullOrWhiteSpace(Cpp.OutputFilePath)) {
                        Cpp.OutputFilePath = System.IO.Path.ChangeExtension(Cpp.TargetFilePath, ".i");
                    } else {
                        try {
                            Cpp.OutputFilePath = System.IO.Path.GetFullPath(Cpp.OutputFilePath);
                            if (Cpp.OutputFilePath == Cpp.TargetFilePath) {
                                Cpp.Error("出力ファイルと入力ファイルが同一です。");
                                return -1;
                            }
                        } catch {
                            Cpp.Error($"指定された出力ファイル {args[i]} はOSの解釈できない不正なパスです。標準出力を出力先にします。");
                        }
                    }
                    {
                        var sw = new StreamWriter(Cpp.OutputFilePath);
                        sw.AutoFlush = true;
                        Console.SetOut(sw);
                    }
                }
                if (Cpp.LogFilePath != null) {
                    if (String.IsNullOrWhiteSpace(Cpp.LogFilePath)) {
                        Cpp.LogFilePath = System.IO.Path.ChangeExtension(Cpp.TargetFilePath, ".i.log");
                    } else {
                        try {
                            Cpp.LogFilePath = System.IO.Path.GetFullPath(Cpp.LogFilePath);
                            if (Cpp.LogFilePath == Cpp.TargetFilePath) {
                                Cpp.Error("ログファイルと入力ファイルが同一です。");
                                return -1;
                            }
                            if (Cpp.LogFilePath == Cpp.OutputFilePath) {
                                Cpp.Error("ログファイルと出力ファイルが同一です。");
                                return -1;
                            }
                        } catch {
                            Cpp.Error($"指定されたログファイル {args[i]} はOSの解釈できない不正なパスです。標準出力を出力先にします。");
                        }
                    }
                    {
                        var sw = new StreamWriter(Cpp.LogFilePath);
                        sw.AutoFlush = true;
                        Console.SetError(sw);
                    }
                }
            }

            // バナー表示オプションが指定されていた場合はバナーを表示
            if (Cpp.Switchs.Contains("-V")) {
                ShowBanner();
            }

            #region プリプロセッサと字句解析器を初期化
            Cpp.Init();
            Lex.Init();
            #endregion

            #region プリプロセス処理を実行
            {
                // 字句解析器に指定されたファイルを入力ファイルとして設定する
                Lex.Set(Cpp.TargetFilePath);

                // コマンドラインで指定された #include や #define コードは 入力の先頭で 記述されたファイルが include されたかのように振る舞わせる
                File.StreamPush(new File(sbDefine.ToString(), "<command-line>"));

                // 出力器を作成
                Cpp.TokenWriter = new TokenWriter();
                for (; ; ) {
                    // 展開などの処理を行ったトークンを読み取る
                    Token tok = Cpp.ReadToken();

                    if (tok.Kind == Token.TokenKind.EoF) {
                        // 読み取ったトークンがEOFだったので終了
                        break;
                    }

                    // トークンを書き出す
                    Cpp.TokenWriter.Write(tok);
                }
            }

            if (Cpp.Warnings.Contains(WarningOption.UnusedMacros)) {
                foreach (var macro in Cpp.EnumUnusedMacro()) {
                    Cpp.Warning(macro.GetFirstPosition(), $"定義されたマクロ {macro.GetName()} は一度も参照されていません。");
                }
            }
            #endregion

            if (Cpp.ErrorCount != 0) {
                Console.Error.WriteLine($"{(Cpp.TargetFilePath == "-" ? "標準入力" : Cpp.TargetFilePath)} のプリプロセスに失敗しました。");
            } else {
                Console.Error.WriteLine($"{(Cpp.TargetFilePath == "-" ? "標準入力" : Cpp.TargetFilePath)} のプリプロセスに成功しました。");
            }

            Console.Error.WriteLine($"  エラー: {Cpp.ErrorCount}件");
            Console.Error.WriteLine($"  警告: {Cpp.WarningCount}件");

            if (Cpp.Reports.Count > 0) {
                var reportFilePath = Cpp.TargetFilePath + ".report.xml";
                Reporting.CreateReport(reportFilePath);
            }

            if (Cpp.Verboses.Contains(VerboseOption.TraceMacroExpand)) {
                var expandTraceFilePath = (Cpp.TargetFilePath == "-" ? "stdin" : Cpp.TargetFilePath) + ".macro-expand-log.xml";
                try {
                    Cpp.ExpandLog.SaveToXml(expandTraceFilePath);
                } catch (System.IO.IOException) {
                    Cpp.Error($"マクロ展開結果 {expandTraceFilePath} の作成に失敗しました。書き込み先のファイルを開いている場合は閉じて再度実行してください。");
                }
            }

            return Cpp.ErrorCount != 0 ? -1 : 0;
        }

        private void ShowBanner() {
            Console.Error.WriteLine("CsCpp (The C preprocessor in C#) Version {0}", this.VersionString);
        }

        private void Usage() {
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
}
