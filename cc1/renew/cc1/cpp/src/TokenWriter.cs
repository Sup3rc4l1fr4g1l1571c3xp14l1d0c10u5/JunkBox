using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSCPP {
    public class TokenWriter {

        private string CurrentFile { get; set; }
        private long CurrentLine { get; set; }
        private bool BeginOfLine { get; set; }
        public int OutputLine { get; private set; }
        public int OutputColumn { get; private set; }

        public TokenWriter()
        {
            CurrentFile = String.Empty;
            CurrentLine = 0;
            BeginOfLine = true;
            OutputLine = 1;
            OutputColumn = 1;
        }

        /// <summary>
        /// 出力位置情報 を 文字列 str の出力後の情報へ更新
        /// </summary>
        /// <param name="str"></param>
        private void UpdateOutputPos(string str) {
            for (var i = str.Length-1; i >=0; i--)
            {
                if (str[i] == '\n')
                {
                    OutputColumn = str.Length - i;
                    for (var j = i; j >= 0; j--)
                    {
                        if (str[j] == '\n')
                        {
                            OutputLine++;
                        }
                    }
                    return;
                }
            }
            OutputColumn += str.Length;
        }

        private void WriteLine(string str, bool isDummy) {
            Write(str+"\n", isDummy);
        }

        private void Write(string str, bool isDummy) {
            UpdateOutputPos(str);
            if (!isDummy) {
                Console.Write(str);
            }
        }

        private static readonly System.Text.RegularExpressions.Regex RegexpReplace = new System.Text.RegularExpressions.Regex(@"(\r\n|\r|\n)", System.Text.RegularExpressions.RegexOptions.Compiled);

        private string ReplaceNewLine(string input)
        {
            return RegexpReplace.Replace(input, Environment.NewLine);
        }

        /// <summary>
        /// 行指令を出力してファイル位置を更新
        /// </summary>
        /// <param name="line"></param>
        /// <param name="path"></param>
        /// <param name="isDummy"></param>
        private void WriteLineDirective(long line, string path, bool isDummy)
        {
            if (!CppContext.Switchs.Contains("-P"))
            {
                string pragmaStr = CppContext.Features.Contains(Feature.OutputGccStyleLineDirective) ? "" : "line";
                WriteLine($"#{pragmaStr} {line} \"{path.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"", isDummy);
            }
        }

        /// <summary>
        /// 出力位置が行頭の場合はファイル位置の調整を実行
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="line"></param>
        /// <param name="isDummy"></param>
        private void PositionFixup(string fileName, long line, bool isDummy) {
            if (BeginOfLine) {
                if (fileName != CurrentFile) {
                    // ファイル自体が違う場合は #line 指令を挿入して現在位置を更新
                    WriteLineDirective(line, fileName, isDummy);
                    CurrentFile = fileName;
                    CurrentLine = line;
                } else {
                    // ファイルは同じだけど行番号が違う場合、改行で埋めて調整
                    if (line < CurrentLine + 5) {
                        for (long j = CurrentLine; j < line; j++) {
                            WriteLine("", isDummy);
                        }
                    } else {
                        WriteLineDirective(line, fileName, isDummy);
                    }
                    CurrentLine = line;
                }
            }
        }

        /// <summary>
        /// トークンの出力を行う
        /// </summary>
        /// <param name="tok"></param>
        private Position WriteToken(Token tok, bool isDummy)
        {
            System.Diagnostics.Debug.Assert(tok.File != null);

            // トークンの前に空白がある場合、その空白を出力
            foreach (var chunk in tok.Space.Chunks)
            {
                // 出力位置が行頭の場合はファイル位置の調整を実行
                this.PositionFixup(chunk.Pos.FileName, chunk.Pos.Line, isDummy);

                // 空白を出力する
                Write(ReplaceNewLine(chunk.Space), isDummy);

                // 空白中に改行が含まれるケース＝行を跨ぐブロックコメントの場合なので、
                // #line指令による行補正は行わずに行数のみを更新する
                CurrentLine += chunk.Space.Count(x => x == '\n');

                // 行頭情報を更新
                BeginOfLine = (BeginOfLine && String.IsNullOrEmpty(chunk.Space)) || chunk.Space.EndsWith("\n");
            }

            // 出力位置が行頭の場合はファイル位置の調整を実行
            PositionFixup(tok.File.Name, tok.Position.Line, isDummy);

            // トークンを出力する
            if (tok.Kind == Token.TokenKind.NewLine) {
                // トークンが改行の場合
                var ret = new Position("", OutputLine, OutputColumn);
                CurrentLine += 1;
                WriteLine("", isDummy);
                BeginOfLine = true;
                return ret;
            } else {
                // 改行以外の場合
                if (tok.HeadSpace && BeginOfLine == false) { Write(" ", isDummy); }
                var ret = new Position("", OutputLine, OutputColumn);
                Write(ReplaceNewLine(Token.TokenToStr(tok)), isDummy);
                if (tok.TailSpace) { Write(" ", isDummy); }
                BeginOfLine = false;
                return ret;
            }
        }

        public Position Write(Token tok)
        {
            return WriteToken(tok, false);
        }

        public Position WriteDummy(Token tok)
        {
            // save context
            var currentFile = CurrentFile;
            var currentLine = CurrentLine;
            var beginOfLine = BeginOfLine;
            var outputLine = OutputLine;
            var outputColumn = OutputColumn;

            var ret = WriteToken(tok,true);

            // restore context
            CurrentFile = currentFile;
            CurrentLine = currentLine;
            BeginOfLine = beginOfLine;
            OutputLine = outputLine;
            OutputColumn = outputColumn;

            return ret;
        }

    }
}
