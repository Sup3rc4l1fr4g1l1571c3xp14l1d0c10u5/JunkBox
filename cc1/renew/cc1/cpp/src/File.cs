using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace CSCPP
{
    /// <summary>
    /// プリプロセッサ上での入力ファイルスコープを示すオブジェクト
    /// </summary>
    public class File
    {

        /// <summary>
        /// 実際の入力ソース
        /// </summary>
        private Source Source { get; set; }

        /// <summary>
        /// __FILE__で得られるファイル名
        /// </summary>
        public string Name { get { return PushBackBuffer.Count > 0 ? PushBackBuffer.Peek().Position.FileName : _name; } }

        private string _name;

        /// <summary>
        /// 現在の読み取り行番号
        /// </summary>
        public int Line { get { return PushBackBuffer.Count > 0 ? PushBackBuffer.Peek().Position.Line : _line; } }

        private int _line;

        /// <summary>
        /// 現在の読み取り列番号
        /// </summary>
        public int Column { get { return PushBackBuffer.Count > 0 ? PushBackBuffer.Peek().Position.Column : _column; } }

        private int _column;

        /// <summary>
        /// ファイルから読み取ったトークンの数
        /// </summary>
        public int NumOfToken { get; set; }

        /// <summary>
        /// Source から最後に読み取った文字
        /// </summary>
        private Utf32Char LastCharacter { get; set; } = new Utf32Char(Position.Empty, '\n');

        /// <summary>
        /// ファイルから読み取りか？
        /// </summary>
        private bool FromFile { get; }

        /// <summary>
        /// トークンの読み戻しスタック
        /// </summary>
        private Stack<Utf32Char> PushBackBuffer { get; } = new Stack<Utf32Char>();

        /// <summary>
        /// TextReader を Source とする入力ファイルを作成する
        /// </summary>
        /// <param name="textReader"></param>
        /// <param name="name"></param>
        public File(TextReader textReader, string name) {
            Source = new Source(Utf32Reader.FromTextReader(textReader));
            _name = name;
            _line = 1;
            _column = 1;
            FromFile = true;
        }

        /// <summary>
        /// 文字列 str を Source とする入力ファイルを作成する
        /// </summary>
        /// <param name="textReader"></param>
        /// <param name="name"></param>
        public File(string str, string name) {
            Source = new Source(Utf32Reader.FromTextReader(new StreamReader(new MemoryStream(System.Text.Encoding.Unicode.GetBytes(str)), System.Text.Encoding.Unicode)));
            _name = name;
            _line = 1;
            _column = 1;
            FromFile = false;
        }

        /// <summary>
        /// ファイルを閉じる
        /// </summary>
        private void Close() {
            if (Source != null) {
                Source.Dispose();
                Source = null;
            }
        }

        /// <summary>
        /// fileから１文字読み取る。
        /// </summary>
        /// <param name="skipBadchar">指定されたエンコードで表現できない不正な文字を警告どまりで読み飛ばす場合はtrueにする</param>
        /// <returns></returns>
        private Utf32Char ReadChFromFile(bool skipBadchar = false) {
            // ファイルから１文字読み取る
            var pos = new Position(Name, Line, Column);
            var c1 = Source.Read((s) => {
                if (skipBadchar == false) {
                    CppContext.Error(pos, $@"ファイル中に文字コード上で表現できない文字 \x{s} があります。");
                } else {
                    CppContext.Warning(pos, $@"ファイル中に文字コード上で表現できない文字 \x{s} があります。確認をしてください。");
                }
            });

            if (c1 == Utf32Reader.EoF) {
                if (FromFile) {
                    // 読み取り結果がEOFの場合、直前の文字が改行文字でもEOFでもなければ改行を読みとった扱いにする
                    if (LastCharacter != '\n' && !LastCharacter.IsEof()) {
                        if (CppContext.Warnings.Contains(Warning.Pedantic)) {
                            // ISO/IEC 9899：1999 5.1.1.2 翻訳フェーズの(2)で 
                            // 空でないソースファイルは，改行文字で終了しなければならない。さらに，この改行文字の直前に（接合を行う前の時点で）逆斜線があってはならない。
                            // となっている。
                            // 参考までに、C++の規格 2.1　翻訳過程では
                            // 空でないソースファイルが改行文字で終わっていない場合，その動作は，未定義とする。空でないソースファイルが逆斜線に続く改行文字で終わっている場合，その動作は，未定義とする。
                            // となっている
                            // Posix的にもテキストファイルは改行で終端すべしとなっている。
                            CppContext.Warning(LastCharacter.Position, "ファイルが改行文字で終了していません。");
                        }
                        c1 = '\n';
                    }
                }
            } else if (c1 == '\r') {
                // CRLFの場合を考慮
                var c2 = Source.Read((s) => {
                    if (skipBadchar == false) {
                        CppContext.Error(pos, $@"ファイル中に文字コード上で表現できない文字 \x{s} があります。");
                    } else {
                        CppContext.Warning(pos, $@"ファイル中に文字コード上で表現できない文字 \x{s} があります。確認をしてください。");
                    }
                });
                if (c2 != '\n') {
                    Source.Unread(c2);
                }
                c1 = '\n';
            }

            var ch = new Utf32Char(pos, c1);
            LastCharacter = ch;
            return ch;
        }

        /// <summary>
        /// 解析中のファイルが積まれるスタック
        /// </summary>
        private static Stack<File> _files = new Stack<File>();

        /// <summary>
        /// 解析中にfilesの待避が必要になった場合に待避させるためのスタック
        /// </summary>
        private static readonly Stack<Stack<File>> Stashed = new Stack<Stack<File>>();

        /// <summary>
        /// カレントファイルから一文字読み取る。ついでに読み取った文字に応じてファイル上の現在位置を更新する。
        /// トリグラフと行末のエスケープシーケンスは考慮しない。
        /// </summary>
        /// <param name="skipBadchar">指定されたエンコードで表現できない不正な文字を警告どまりで読み飛ばす場合はtrueにする</param>
        /// <returns>ファイル終端以外なら読み取った文字、終端に到達していたらEOF</returns>
        public static Utf32Char Get(bool skipBadchar = false) {
            bool dummy;
            return Get(out dummy, skipBadchar: skipBadchar);
        }

        /// <summary>
        /// カレントファイルから一文字読み取る。ついでに読み取った文字に応じてファイル上の現在位置を更新する。
        /// </summary>
        /// <param name="ungetted">読み戻された文字の場合は ungetted が trueにセットされる（この値を用いてトライグラフの警告が多重に出力されないようにする）</param>
        /// <param name="skipBadchar">指定されたエンコードで表現できない不正な文字を警告どまりで読み飛ばす場合はtrueにする</param>
        /// <returns>ファイル終端以外なら読み取った文字、終端に到達していたらEOF</returns>
        private static Utf32Char Get(out bool ungetted, bool skipBadchar = false) {
            File f = _files.Peek();
            if (f.PushBackBuffer.Count > 0) {
                // 読み戻しスタックに読み戻し文字がある場合はそれを読み出す
                Utf32Char c = f.PushBackBuffer.Pop();
                ungetted = true;
                return c;
            } else {
                // そうでなければ入力ソースから読み取る
                System.Diagnostics.Debug.Assert(f != null);
                Utf32Char c = f.ReadChFromFile(skipBadchar: skipBadchar);
                ungetted = false;
                // 読み取った文字に応じてファイル上の現在の読み取り位置を更新
                if (c == '\n') {
                    f._line++;
                    f._column = 1;
                } else if (!c.IsEof()) {
                    f._column++;
                }
                return c;
            }

        }

        /// <summary>
        /// トリグラフと行末のエスケープシーケンスを考慮して一文字読み取る。
        /// </summary>
        /// <param name="handleEof">EoFを読み飛ばさない場合は真</param>
        /// <param name="skipBadchar">指定されたエンコードで表現できない不正な文字を警告どまりで読み飛ばす場合はtrueにする</param>
        /// <returns>読み取った文字</returns>
        public static Utf32Char ReadCh(bool handleEof = false, bool skipBadchar = false) {
            for (; ; )
            {
                // カレントから一文字読み取る
                var c1 = Get();
                if (c1.IsEof() && handleEof == false) {
                    // 現在のファイルがスタック中に残った最後のファイルの場合はEOFを返す
                    if (_files.Count == 1) { return c1; }
                    // 現在のファイルをスタックからpopしてリトライ
                    _files.Pop().Close();
                    continue;
                }

                // トリグラフ処理
                if (CppContext.Features.Contains(Feature.Trigraphs)) {
                    var p2 = c1.Position;
                    // トライグラフの読み取りを行う
                    if (c1 == '?') {
                        var c2 = Get();
                        if (c2 == '?') {
                            bool ungetted;
                            var c3 = Get(out ungetted, skipBadchar: skipBadchar);
                            var tri = Trigraph(c3);
                            if (tri != '\0') {
                                if (CppContext.Warnings.Contains(Warning.Trigraphs) && !ungetted) {
                                    CppContext.Error(p2, $"トライグラフ ??{c3.ToString()} が {tri} に置換されました。");
                                }
                                return new Utf32Char(c1.Position, tri);
                            } else {
                                if (CppContext.Warnings.Contains(Warning.Trigraphs) && !ungetted) {
                                    CppContext.Error(p2, $"未定義のトライグラフ ??{c3.ToString()} が使用されています。");
                                }
                            }
                            UnreadCh(c3);
                        }
                        UnreadCh(c2);
                        return c1;
                    }
                }

                if (c1 != '\\') {
                    // \でないならそれ返して終わり
                    return c1;
                } else {
                    // 行末の\の場合、改行も読み飛ばしてリトライ
                    var c2 = Get();
                    if (c2 == '\n') {
                        var c3 = Get();
                        if (c3.IsEof()) {
                            // ISO/IEC 9899：1999 5.1.1.2 翻訳フェーズの(2)で 
                            // 空でないソースファイルは，改行文字で終了しなければならない。さらに，この改行文字の直前に（接合を行う前の時点で）逆斜線があってはならない。
                            // となっている。
                            CppContext.Warning(c1.Position, "ファイル終端の改行文字の直前に \\ があります。");
                        }
                        UnreadCh(c3);
                        continue;
                    } else {
                        // それ以外の場合は読み戻してから\を返す
                        UnreadCh(c2);
                        return c1;
                    }
                }
            }
        }

        private static char Trigraph(Utf32Char c) {
            switch (c.ToString()) {
                case "=": return '#';
                case "/": return '\\';
                case "\'": return '^';
                case "(": return '[';
                case ")": return ']';
                case "!": return '|';
                case "<": return '{';
                case ">": return '}';
                case "-": return '~';
                default: return '\0';
            }

        }

        /// <summary>
        /// 一文字読み戻す（正確には任意の一文字を現在の読み戻しスタックに挿入する）
        /// </summary>
        /// <param name="c"></param>
        public static void UnreadCh(Utf32Char c) {
            if (c.IsEof()) { return; }
            File f = _files.Peek();
            f.PushBackBuffer.Push(c);
        }

        /// <summary>
        /// 現在読み込みで用いているファイルを取得
        /// </summary>
        /// <returns></returns>
        public static File CurrentFile { get { return _files.Peek(); } }

        /// <summary>
        /// 読み込みに使うファイルを積む(include相当の処理)
        /// </summary>
        /// <param name="f"></param>
        public static void StreamPush(File f) {
            _files.Push(f);
        }

        /// <summary>
        /// 現在のファイルスタックを待避し、新しいファイルスタックを作り、ファイルfを積む。
        /// これは結合演算子 ## の結果を再度マクロ展開するときに使う
        /// </summary>
        /// <param name="f">新しく作ったファイルスタックに積むファイル</param>
        public static void StreamStash(File f) {
            Stashed.Push(_files);
            _files = new Stack<File>();
            _files.Push(f);
        }

        public static void StreamUnstash() {
            _files = Stashed.Pop();
        }

        /// <summary>
        /// 現在のファイルの位置情報を変更(#line指令で使用)
        /// </summary>
        /// <param name="line">行番号</param>
        /// <param name="filename">ファイル名</param>
        public static void OverwriteCurrentPosition(int line, string filename) {
            File f = CurrentFile;
            f._line = line;
            if (filename != null) {
                f._name = filename;
            }
        }
    }
}