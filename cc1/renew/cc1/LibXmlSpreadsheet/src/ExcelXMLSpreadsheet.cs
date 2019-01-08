using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Drawing;

namespace LibXmlSpreadsheet {

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class WorksheetAttribute : Attribute
    {
        public string Name { get; }
        public bool AutoFilter { get; }
        public WorksheetAttribute(string name, bool autoFilter = false) { this.Name = name; this.AutoFilter = autoFilter; }
    }

    public class WorksheetMemberAttribute : Attribute
    {
        public string Name { get; }
        public int Order { get; }
        public WorksheetMemberAttribute(string name, int order = 0) { this.Name = name; this.Order = order; }
    }

    public class ExcelXMLSpreadsheet {
        public static Worksheet CreateWorkseetFromCollection<T>(IList<T> collection) {
            var t = typeof(T);
            foreach (var x in t.GetCustomAttributes(true)) {
                if (x is WorksheetAttribute) {
                    Worksheet workseet = new Worksheet();
                    workseet.Name = ((WorksheetAttribute)x).Name;
                    workseet.AutoFilter = ((WorksheetAttribute)x).AutoFilter;

                    var captions = t.GetMembers()
                                    .Where(y => y.MemberType == System.Reflection.MemberTypes.Property)
                                    .Select(y => Tuple.Create(
                                                    y.Name,
                                                    y.GetCustomAttributes(true)
                                                     .Where(z => z is WorksheetMemberAttribute)
                                                     .Cast<WorksheetMemberAttribute>().FirstOrDefault()
                                                 )
                                           )
                                    .Where(y => y.Item2 != null)
                                    .OrderBy(y => y.Item2.Order)
                                    .ToList();
                    workseet.AddColumn(captions.Select(y => y.Item2.Name).ToArray());
                    foreach (var item in collection) {
                        workseet.AddColumn(captions.Select(y => t.GetProperty(y.Item1).GetValue(item).ToString()).ToArray());
                    }
                    return workseet;
                }
            }
            return null;
        }

        public class Worksheet {
            public string Name { get; set; }
            private List<List<object>> Table = new List<List<object>>();

            /// <summary>
            /// 1行目をオートフィルタとして使う
            /// </summary>
            public bool AutoFilter;

            public object this[int row, int column] {
                get { return Table.ElementAtOrDefault(row)?.ElementAtOrDefault(column); }
                set { while (Table.Count <= row) { Table.Add(new List<object>()); } while (Table[row].Count <= column) { Table[row].Add(null); } Table[row][column] = value; }
            }
            public int RawCount { get { return Table.Count; } }
            public int ColumnCount { get { return Table.Max(x => x.Count); } }
            public void AddColumn(params object[] values) {
                Table.Add(values.ToList());
            }
            public void WriteTo(ExcelXMLSpreadsheet parent, XmlWriter writer) {
                writer.WriteStartElement("Worksheet");
                writer.WriteAttributeString("ss", "Name", null, String.IsNullOrEmpty(Name) ? "(untitled)" : Name);
                {
                    writer.WriteStartElement("Table");

                    using (var g = Graphics.FromHwnd(IntPtr.Zero))
                    using (var f = new Font(parent.FontName, parent.FontSize)) {
                        var widths = new int [Table.Max(x => x.Count)];
                        foreach (var table in Table) {
                            var _width = table.Select(y => y == null ? 0 : (int)Math.Ceiling(g.MeasureString(y.ToString(), f, PointF.Empty, StringFormat.GenericDefault).Width)).ToList();
                            for (int i=0;i<_width.Count; i++) {
                                widths[i] = Math.Min(Math.Max(widths[i], _width[i]),1000);
                            }
                        }
                        for (var i = 0; i < widths.Length; i++) {
                            writer.WriteStartElement("Column");
                            writer.WriteAttributeString("ss", "Index", null, $"{ i + 1}");
                            writer.WriteAttributeString("ss", "Width", null, $"{ widths[i] + ( (AutoFilter) ? parent.FontSize : 0)}");
                            writer.WriteEndElement(/*"Column"*/);
                        }
                    }

                    Table.ForEach(row => {
                        writer.WriteStartElement("Row");
                        row.ForEach(cell => {
                            writer.WriteStartElement("Cell");
                            {
                                writer.WriteStartElement("Data");
                                if (cell == null) {
                                    writer.WriteAttributeString("ss", "Type", null, "String");
                                    writer.WriteString("");
                                } else if (cell is sbyte || cell is short || cell is int || cell is long || cell is byte || cell is ushort || cell is uint || cell is ulong || cell is double || cell is float) {
                                    writer.WriteAttributeString("ss", "Type", null, "Number");
                                    writer.WriteString((cell).ToString());
                                } else if (cell is bool) {
                                    writer.WriteAttributeString("ss", "Type", null, "Boolean");
                                    writer.WriteString((((bool)cell) ? 1 : 0).ToString());
                                } else {
                                    writer.WriteAttributeString("ss", "Type", null, "String");
                                    writer.WriteString((cell).ToString());
                                }

                                writer.WriteEndElement(/*"Data "*/);
                            }
                            writer.WriteEndElement(/*"Cell"*/);
                        });
                        writer.WriteEndElement(/*"Row"*/);
                    });

                    writer.WriteEndElement(/*"Table"*/);
                }
                if (AutoFilter) {
                    writer.WriteStartElement("x", "AutoFilter",null);
                    writer.WriteAttributeString("x", "Range", null, $"R1C1:R{this.RawCount}C{this.ColumnCount}");
                    writer.WriteEndElement(/*"AutoFilter"*/);
                }

                { 
                    writer.WriteStartElement("", "WorksheetOptions", "urn:schemas-microsoft-com:office:excel");
                    if (AutoFilter) {
                        /*
                           <FreezePanes/>
                           <FrozenNoSplit/>
                           <SplitHorizontal>1</SplitHorizontal>
                           <TopRowBottomPane>1</TopRowBottomPane>
                           <ActivePane>2</ActivePane>
                        */
                        writer.WriteStartElement("FreezePanes");
                        writer.WriteEndElement(/*"FreezePanes"*/);
                        writer.WriteStartElement("FrozenNoSplit");
                        writer.WriteEndElement(/*"FrozenNoSplit"*/);
                        writer.WriteStartElement("SplitHorizontal");
                        writer.WriteString("1");
                        writer.WriteEndElement(/*"SplitHorizontal"*/);
                        writer.WriteStartElement("TopRowBottomPane");
                        writer.WriteString("1");
                        writer.WriteEndElement(/*"TopRowBottomPane"*/);
                        writer.WriteStartElement("ActivePane");
                        writer.WriteString("2");
                        writer.WriteEndElement(/*"ActivePane"*/);

                    }
                    writer.WriteEndElement(/*"Table"*/);
                }

                writer.WriteEndElement(/*"Worksheet"*/);


            }
        }

        public List<Worksheet> Worksheets { get; } = new List<Worksheet>();

        public int FontSize { get; set; } = 11;

        public string FontName { get; set; } = "ＭＳ ゴシック";

        public void WriteTo(string file) {
            try {
                using (XmlTextWriter writer = new XmlTextWriter(file, System.Text.Encoding.UTF8)) {
                    writer.Formatting = Formatting.Indented;
                    WriteTo(writer);
                }
            } catch(System.IO.IOException ex) {
                throw new System.IO.IOException($"ファイル {file} の作成に失敗しました。Excel等で書き込み先のファイルを開いている場合は閉じて再度実行してください。",ex);
            }
        }

        public void WriteTo(XmlTextWriter writer) {
            writer.WriteStartDocument();
            {
                writer.WriteProcessingInstruction("mso-application", "progid=\"Excel.Sheet\"");
                writer.WriteStartElement("Workbook", "urn:schemas-microsoft-com:office:spreadsheet");
                writer.WriteAttributeString("xmlns", "o", null, "urn:schemas-microsoft-com:office:office");
                writer.WriteAttributeString("xmlns", "x", null, "urn:schemas-microsoft-com:office:excel");
                writer.WriteAttributeString("xmlns", "ss", null, "urn:schemas-microsoft-com:office:spreadsheet");
                writer.WriteAttributeString("xmlns", "html", null, "http://www.w3.org/TR/REC-html40");

                writer.WriteStartElement("Styles");
                writer.WriteStartElement("Style");
                writer.WriteAttributeString("ss", "ID", null, "Default");
                writer.WriteStartElement("Font");
                writer.WriteAttributeString("ss", "Size", null, FontSize.ToString());
                writer.WriteAttributeString("ss", "FontName", null, FontName.ToString());
                writer.WriteEndElement(/*"Font"*/);
                writer.WriteEndElement(/*"Style"*/);
                writer.WriteEndElement(/*"Styles"*/);

                Worksheets.ForEach(x => x.WriteTo(this,writer));

                writer.WriteEndElement(/*"Workbook"*/);

            }
            writer.WriteEndDocument();

        }
    }
}
