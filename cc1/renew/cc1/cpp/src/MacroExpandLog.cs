using System.Collections.Generic;

namespace CSCPP
{
    public class MacroExpandLog
    {
        /// <summary>
        /// マクロ展開情報
        /// </summary>
        public class ExpandInfo
        {
            public Token Use { get; }
            public Macro Define { get; }
            public int StartLine { get; }
            public int StartColumn { get; }
            public int EndLine { get; }
            public int EndColumn { get; }
            public ExpandInfo(Token use, Macro define, int startLine, int startColumn, int endLine, int endColumn) {
                Use = use;
                Define = define;
                StartLine = startLine;
                StartColumn = startColumn;
                EndLine = endLine;
                EndColumn = endColumn;
            }
        }

        /// <summary>
        /// 展開情報リスト
        /// </summary>
        private List<ExpandInfo> Logs { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MacroExpandLog() {
            Logs = new List<ExpandInfo>();
        }

        /// <summary>
        /// ログ追加
        /// </summary>
        /// <param name="use"></param>
        /// <param name="define"></param>
        /// <param name="startLine"></param>
        /// <param name="startColumn"></param>
        /// <param name="endLine"></param>
        /// <param name="endColumn"></param>
        public void Add(Token use, Macro define, int startLine, int startColumn, int endLine, int endColumn) {
            Logs.Add(new ExpandInfo(use, define, startLine, startColumn, endLine, endColumn));
        }

        /// <summary>
        /// XMLファイルに展開ログを保存
        /// </summary>
        /// <param name="path"></param>
        public void SaveToXml(string path) {
            using (System.Xml.XmlWriter writer = System.Xml.XmlWriter.Create(path)) {
                writer.WriteStartElement("MacroExpandLog");
                foreach (var log in this.Logs) {
                    writer.WriteStartElement("ExpandInfo");
                    writer.WriteAttributeString("EndColumn", log.EndColumn.ToString());
                    writer.WriteAttributeString("EndLine", log.EndLine.ToString());
                    writer.WriteAttributeString("StartColumn", log.StartColumn.ToString());
                    writer.WriteAttributeString("StartLine", log.StartLine.ToString());
                    writer.WriteAttributeString("DefEndColumn", log.Define.GetLastPosition().Column.ToString());
                    writer.WriteAttributeString("DefEndLine", log.Define.GetLastPosition().Line.ToString());
                    writer.WriteAttributeString("DefStartColumn", log.Define.GetFirstPosition().Column.ToString());
                    writer.WriteAttributeString("DefStartLine", log.Define.GetFirstPosition().Line.ToString());
                    writer.WriteAttributeString("DefFile", log.Define.GetFirstPosition().FileName);
                    writer.WriteAttributeString("UseColumn", log.Use.Position.Column.ToString());
                    writer.WriteAttributeString("UseLine", log.Use.Position.Line.ToString());
                    writer.WriteAttributeString("UseFile", log.Use.Position.FileName);
                    writer.WriteAttributeString("Name", log.Define.GetName());
                    writer.WriteAttributeString("Id", log.Define.UniqueId.ToString());
                    writer.WriteEndElement(/*ExpandInfo*/);
                }
                writer.WriteEndElement(/*MacroExpandLog*/);
            }

        }
    }

}