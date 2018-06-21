using System;
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
        public string Name { get { return _pushBackBuffer.Any() ? _pushBackBuffer.Peek().position.FileName : name; } }
        private string name {get; set;}

        /// <summary>
        /// 現在の読み取り行番号
        /// </summary>
        public long Line { get { return _pushBackBuffer.Any() ? _pushBackBuffer.Peek().position.Line : line; } }
        private long line { get; set; }

        /// <summary>
        /// 現在の読み取り列番号
        /// </summary>
        public int Column { get { return _pushBackBuffer.Any() ? _pushBackBuffer.Peek().position.Column : column; } }
        private int column { get; set; }

        /// <summary>
        /// Source から読み取られたトークンの数
        /// </summary>
        public int NumOfToken { get; set; }

        /// <summary>
        /// Source から最後に読み取った文字
        /// </summary>
        private Char LastCharacter { get; set; } = new Char(new Position("", 0, 0), '\n');

        /// <summary>
        /// ファイルから読み取りか？
        /// </summary>
        public bool FromFile { get; }

        /// <summary>
        /// トークンの読み戻しスタック
        /// </summary>
        private Stack<Char> _pushBackBuffer { get; } = new Stack<Char>();

        public const int Eof = -1;

        public File(System.IO.TextReader file, string name)
        {
            Source = new Source(file);
            this.name = name;
            this.line = 1;
            this.column = 1;
            FromFile = true;
        }

        public File(string str, string name)
        {
            Source = new Source(new System.IO.StringReader(str));
            this.name = name;
            this.line = 1;
            this.column = 1;
            FromFile = false;
        }

        private void Close()
        {
            if (Source != null)
            {
                Source.Dispose();
                Source = null;
            }
        }

        /// <summary>
        /// fileから１文字読み取る。
        /// </summary>
        /// <returns></returns>
        private Char readc_file(bool skip_badchar = false)
        {
            // ファイルから１文字読み取る
            var pos =  new Position(this.Name, this.Line, this.Column);
            var c = Source.Read((s) => {
                if (skip_badchar == false) { 
                    CppContext.Error(pos, $@"ファイル中に文字コード上で表現できない文字 \u{s} があります。");
                } else {
                    CppContext.Warning(pos, $@"ファイル中に文字コード上で表現できない文字 \u{s} があります。確認をしてください。");
                }
            });
            
            if (c == -1)
            {
                if (FromFile)
                {
                    // 読み取り結果がEOFの場合、直前の文字が改行文字でもEOFでもなければ改行を読みとった扱いにする
                    if (LastCharacter.Value != '\n' && !LastCharacter.IsEof())
                    {
                        if (CppContext.Warnings.Contains(Warning.Pedantic))
                        {
                            // ISO/IEC 9899：1999 5.1.1.2 翻訳フェーズの(2)で 
                            // 空でないソースファイルは，改行文字で終了しなければならない。さらに，この改行文字の直前に（接合を行う前の時点で）逆斜線があってはならない。
                            // となっている。
                            // 参考までに、C++の規格 2.1　翻訳過程では
                            // 空でないソースファイルが改行文字で終わっていない場合，その動作は，未定義とする。空でないソースファイルが逆斜線に続く改行文字で終わっている場合，その動作は，未定義とする。
                            // となっている
                            // Posix的にもテキストファイルは改行で終端すべしとなっている。
                            CppContext.Warning(LastCharacter.position, "ファイルが改行文字で終了していません。");
                        }
                    }
                }
            } else if (c == '\r') {
                // CRLFの場合を考慮
                var c2 = Source.Read((s) => {
                    if (skip_badchar == false) { 
                        CppContext.Error(pos, $@"ファイル中に文字コード上で表現できない文字 \x{s} があります。");
                    } else {
                        CppContext.Warning(pos, $@"ファイル中に文字コード上で表現できない文字 \x{s} があります。確認をしてください。");
                    }
                });
                if (c2 != '\n')
                {
                    Source.Unread(c2);
                }
                c = '\n';
            }

            var ch = new Char(pos, c);
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
        /// </summary>
        /// <returns>ファイル終端以外なら読み取った文字、終端に到達していたらEOF</returns>
        public static Char Get(bool skip_badchar = false)
        {
            bool dummy;
            return Get(out dummy, skip_badchar: skip_badchar);
        }


        /// <summary>
        /// カレントファイルから一文字読み取る。ついでに読み取った文字に応じてファイル上の現在位置を更新する。
        /// 読み戻された文字の場合は ungetted が trueにセットされる（この値を用いてトライグラフの警告が多重に出力されないようにする）
        /// </summary>
        /// <returns>ファイル終端以外なら読み取った文字、終端に到達していたらEOF</returns>
        private static Char Get(out bool ungetted, bool skip_badchar = false)
        {
            File f = _files.Peek();
            Char c;
            if (f._pushBackBuffer.Any() )
            {
                // 読み戻しスタックに内容がある場合はそれを読み出す
                c = f._pushBackBuffer.Pop();
                ungetted = true;
                return c;
            } else
            {
                // そうでなければ入力ソースから読み取る
                System.Diagnostics.Debug.Assert(f != null);
                c = f.readc_file(skip_badchar: skip_badchar);
                ungetted = false;
                // 読み取った文字に応じてファイル上の現在の読み取り位置を更新
                if (c.Value == '\n')
                {
                    f.line++;
                    f.column = 1;
                }
                else if (!c.IsEof())
                {
                    f.column++;
                }
            }

            return c;
        }

        /// <summary>
        /// 一文字読み取る。
        /// </summary>
        /// <returns>読み取った文字</returns>
        public static Char ReadCh(bool handle_eof=false, bool skip_badchar=false)
        {
            for (;;)
            {
                // カレントから一文字読み取る
                var c = Get();
                var p1 = c.position;
                if (c.IsEof() && handle_eof == false)
                {
                    // 現在のファイルがスタック中に残った最後のファイルの場合はEOFを返す
                    if (_files.Count == 1)
                    {
                        return c;
                    }
                    // 現在のファイルをスタックからpopしてリトライ
                    _files.Pop().Close();
                    continue;
                }
                if (CppContext.Features.Contains(Feature.Trigraphs))
                {
                    var p2 = c.position;
                    // トライグラフの読み取りを行う
                    if (c.Value == '?')
                    {
                        var c2 = Get();
                        if (c2.Value == '?')
                        {
                            bool ungetted;
                            var c3 = Get(out ungetted, skip_badchar: skip_badchar);
                            var tri = Trigraph(c3.Value);
                            if (tri != '\0') {
                                if (CppContext.Warnings.Contains(Warning.Trigraphs) && !ungetted) {
                                    CppContext.Error(p2, $"トライグラフ ??{(char)c3.Value} が {tri} に置換されました。");
                                }
                                return new Char(c.position, tri);
                            } else {
                                if (CppContext.Warnings.Contains(Warning.Trigraphs) && !ungetted) {
                                    CppContext.Error(p2, $"未定義のトライグラフ ??{(char)c3.Value} が使用されています。");
                                }
                            }
                            UnreadCh(c3);
                        }
                        UnreadCh(c2);
                        return c;
                    }
                }

                // \でないならそれ返して終わり
                if (c.Value != '\\')
                {
                    return c;
                } else {
                    // 行末の\の場合、改行も読み飛ばしてリトライ
                    var c2 = Get();
                    if (c2.Value == '\n')
                    {
                        var c3 = Get();
                        if (c3.IsEof())
                        {
                            // ISO/IEC 9899：1999 5.1.1.2 翻訳フェーズの(2)で 
                            // 空でないソースファイルは，改行文字で終了しなければならない。さらに，この改行文字の直前に（接合を行う前の時点で）逆斜線があってはならない。
                            // となっている。
                            CppContext.Warning(p1, "ファイル終端の改行文字の直前に\\があります。");
                        }

                        UnreadCh(c3);
                        continue;
                    }

                    // それ以外の場合は読み戻してから\を返す
                    UnreadCh(c2);
                    return c;
                }
            }
        }

        private static char Trigraph(int c)
        {
            switch (c) {
                case '=':
                    return '#';
                case '/':
                    return '\\';
                case '\'':
                    return '^';
                case '(':
                    return '[';
                case ')':
                    return ']';
                case '!':
                    return '|';
                case '<':
                    return '{';
                case '>':
                    return '}';
                case '-':
                    return '~';
                default:
                    return '\0';
            }
        }

        /// <summary>
        /// 一文字読み戻す（正確には任意の一文字を現在の読み戻しスタックに挿入する）
        /// </summary>
        /// <param name="c"></param>
        public static void UnreadCh(Char c)
        {
            if (c.IsEof())
            {
                return;
            }
            File f = _files.Peek();
            f._pushBackBuffer.Push(c);
            //if (c.Value == '\n')
            //{
            //    f.Column = 1;
            //    f.Line--;
            //}
            //else
            //{
            //    f.Column--;
            //}
        }

        /// <summary>
        /// 現在読み込みで用いているファイルを取得
        /// </summary>
        /// <returns></returns>
        public static File current_file()
        {
            return _files.Peek();
        }

        /// <summary>
        /// 読み込みに使うファイルを積む(include相当の処理)
        /// </summary>
        /// <param name="f"></param>
        public static void stream_push(File f)
        {
            _files.Push(f);
        }

        /// <summary>
        /// 現在のファイルスタックを待避し、新しいファイルスタックを作り、ファイルfを積む。
        /// これは結合演算子 ## の結果を再度マクロ展開するときに使う
        /// </summary>
        /// <param name="f">新しく作ったファイルスタックに積むファイル</param>
        public static void stream_stash(File f)
        {
            Stashed.Push(_files);
            _files = new Stack<File>();
            _files.Push(f);
        }

        public static void stream_unstash()
        {
            _files = Stashed.Pop();
        }

        public static void OverWriteCurrentPosition(long line, string filename) {
            File f = current_file();
            f.line = line;
            if (filename != null) {
                f.name = filename;
            }
        }
    }
}