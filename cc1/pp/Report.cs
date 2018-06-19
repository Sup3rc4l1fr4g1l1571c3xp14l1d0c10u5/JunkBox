
using System.Collections.Generic;
using System.Linq;

namespace CSCPP {
    /// <summary>
    /// レポートオプション
    /// </summary>
    public enum Report {
        GeneralInfo,
        MacroInfo,
        CompileSwitch
    }

    public static class Reporting {
        private static ExcelXMLSpreadsheet.Worksheet ReportGeneralInfo() {
            var ws = new ExcelXMLSpreadsheet.Worksheet();
            ws.Name = "プリプロセス情報";

            ws.AddColumn("処理日時", $"{Cpp.DateString} {Cpp.TimeString}");
            ws.AddColumn("対象ファイル", CppContext.TargetFilePath);

            ws.AddColumn("");

            CppContext.OriginalArguments.Aggregate("起動時引数", (s, x) => {
                ws.AddColumn(s, x);
                return "";
            });

            ws.AddColumn("");

            ws.AddColumn("警告オプション設定", "");
            foreach (var e in System.Enum.GetValues(typeof(Warning))) {
                var state = CppContext.Warnings.Contains((Warning)e) ? "有効" : "無効";
                ws.AddColumn(e.ToString(), state);
            }

            ws.AddColumn("");

            ws.AddColumn("機能オプション設定", "");
            foreach (var e in System.Enum.GetValues(typeof(Feature))) {
                var state = CppContext.Features.Contains((Feature)e) ? "有効" : "無効";
                ws.AddColumn(e.ToString(), state);
            }

            ws.AddColumn("");

            ws.AddColumn("スイッチ設定", "");
            foreach (var e in CppContext.Switchs) {
                ws.AddColumn(e.ToString(), "有効");
            }

            ws.AddColumn("");

            ws.AddColumn("エラー件数", CppContext.ErrorCount);
            ws.AddColumn("警告件数", CppContext.WarningCount);

            return ws;

        }
        private static ExcelXMLSpreadsheet.Worksheet ReportMacroInfo() {
            var ws = new ExcelXMLSpreadsheet.Worksheet();
            ws.Name = "マクロ情報";
            ws.AutoFilter = true;
            ws.AddColumn("ID", "マクロ名", "宣言ファイル", "行", "列", "種別", "使用状況", "引数の数", "可変長引数", "定義(引数)", "定義(本体)");
            var infos = new object[11];
            foreach (var macro in Cpp.DefinedMacros) {
                if (Macro.IsBuildinMacro(macro)) {
                    // 組み込みマクロについてはレポートしない
                    continue;
                }
                infos[0] = macro.UniqueId.ToString();
                infos[1] = macro.GetName();
                infos[2] = macro.GetFirstPosition().FileName.ToString();
                infos[3] = macro.GetFirstPosition().Line;
                infos[4] = macro.GetFirstPosition().Column;
                if (Macro.IsObjectMacro(macro)) {
                    var omac = macro as Macro.ObjectMacro;
                    infos[5] = "オブジェクト形式マクロ";
                    infos[6] = omac.Used || Cpp.RefMacros.Contains(omac.GetName());
                    infos[7] = -1;
                    infos[8] = false;
                    infos[9] = "";
                    infos[10] = string.Join(" ", omac.Body.Select(x => x.ToRawString()).Where(x => !string.IsNullOrWhiteSpace(x)));
                } else if (Macro.IsFuncMacro(macro)) {
                    var fmac = macro as Macro.FuncMacro;
                    infos[5] = "関数形式マクロ";
                    infos[6] = fmac.Used;
                    infos[7] = fmac.Args.Count;
                    infos[8] = fmac.IsVarg;
                    infos[9] = "("+string.Join(", ", fmac.Args.Select(x => x.ToRawString()).Where(x => !string.IsNullOrWhiteSpace(x)))+")";
                    infos[10] = string.Join(" ", fmac.Body.Select(x => x.ToRawString()).Where(x => !string.IsNullOrWhiteSpace(x)));
                }
                ws.AddColumn(infos);
            }
            return ws;
        }

        public static class TraceCompileSwitch {
            [ExcelXMLSpreadsheet.Worksheet(name: "コンパイルスイッチ情報")]
            public class Info {
                [ExcelXMLSpreadsheet.WorksheetMember(name: "ID", order: 0)]
                public int Id { get; }

                [ExcelXMLSpreadsheet.WorksheetMember(name: "種別", order: 6)]
                public string Kind { get; }

                public Position Position { get; }

                [ExcelXMLSpreadsheet.WorksheetMember(name: "宣言ファイル", order: 2)]
                public string FileName { get { return Position.FileName; } }

                [ExcelXMLSpreadsheet.WorksheetMember(name: "行", order: 2)]
                public long FileLine { get { return Position.Line; } }

                [ExcelXMLSpreadsheet.WorksheetMember(name: "列", order: 3)]
                public int FileColumn { get { return Position.Column; } }

                [ExcelXMLSpreadsheet.WorksheetMember(name: "式", order: 7)]
                public string Expr { get; }
                public bool Active { get; }
                [ExcelXMLSpreadsheet.WorksheetMember(name: "ネスト", order: 4)]
                public int Depth { get; }
                [ExcelXMLSpreadsheet.WorksheetMember(name: "グループ", order: 5)]
                public int Group { get; }

                public Info(int id, Position position, string kind, string expr, bool active, int depth, int group) {
                    Id = id;
                    Position = position;
                    Kind = kind;
                    Expr = expr ?? "";
                    Active = active;
                    Depth = depth;
                    Group = group;
                }
            }
            private static int IdCount { get; set; }
            public static List<Info> Trace { get; } = new List<Info>();
            private static Stack<int> GroupStack { get; set; } = new Stack<int>();

            public static void OnIf(Position p, string expr, bool active) {
                if (!CppContext.Reports.Contains(Report.CompileSwitch)) { return; }
                Trace.Add(new Info(IdCount, p, "#if", expr, active, GroupStack.Count(), IdCount));
                GroupStack.Push(IdCount);
                IdCount++;
            }
            public static void OnIfdef(Position p, string expr, bool active) {
                if (!CppContext.Reports.Contains(Report.CompileSwitch)) { return; }
                Trace.Add(new Info(IdCount, p, "#ifdef", expr, active, GroupStack.Count(), IdCount));
                GroupStack.Push(IdCount);
                IdCount++;
            }
            public static void OnIfndef(Position p, string expr, bool active) {
                if (!CppContext.Reports.Contains(Report.CompileSwitch)) { return; }
                Trace.Add(new Info(IdCount, p, "#ifndef", expr, active, GroupStack.Count(), IdCount));
                GroupStack.Push(IdCount);
                IdCount++;
            }
            public static void OnElif(Position position, string expr, bool active) {
                if (!CppContext.Reports.Contains(Report.CompileSwitch)) { return; }
                Trace.Add(new Info(IdCount, position, "#elif", expr, active, GroupStack.Count()-1, GroupStack.Peek()));
                IdCount++;
            }
            public static void OnElse(Position position, bool active) {
                if (!CppContext.Reports.Contains(Report.CompileSwitch)) { return; }
                Trace.Add(new Info(IdCount, position, "#else", null, active, GroupStack.Count() - 1, GroupStack.Peek()));
                IdCount++;
            }
            public static void OnEndif(Position position) {
                if (!CppContext.Reports.Contains(Report.CompileSwitch)) { return; }
                Trace.Add(new Info(IdCount, position, "#endif", null, true, GroupStack.Count() - 1, GroupStack.Peek()));
                GroupStack.Pop();
                IdCount++;
            }
        }

        private static ExcelXMLSpreadsheet.Worksheet ReportTraceCompileSwitch() {
            return ExcelXMLSpreadsheet.CreateWorkseetFromCollection(TraceCompileSwitch.Trace);
        }


        public static void CreateReport(string filename) {
            var ss = new ExcelXMLSpreadsheet();
            if (CppContext.Reports.Contains(Report.GeneralInfo)) {
                ss.Worksheets.Add(ReportGeneralInfo());
            }
            if (CppContext.Reports.Contains(Report.MacroInfo)) {
                ss.Worksheets.Add(ReportMacroInfo());
            }
            if (CppContext.Reports.Contains(Report.CompileSwitch)) {
                ss.Worksheets.Add(ReportTraceCompileSwitch());
            }

            ss.WriteTo(filename);
        }
    }
}