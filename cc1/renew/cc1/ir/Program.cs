using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LowLevelIntermediateRepresentation;

namespace LLIR {
    public class Program {

        private const string NonSsaSample1 = @"
func min $a $b {
%min_entry:
    $t = sle $a $b
    br $t %if_true %if_merge

%if_true:
    ret $a

%if_merge:
    ret $b
}

func main {
%main_entry:
    $x = move 10
    $y = move 20
    $x = call min $x $y 
    ret $x
}
";

        private const string NonSsaSample2 = @"
func sum $x $y {
%min_entry:
    $a = alloca 4
    $b = alloca 4
    $c = alloca 4
    $d = alloca 4

          store 4 $a $x 0
          store 4 $b $y 0
    $.0 = move 1000
          store 4 $c $.0 0
    $.1 = load  4 $a 0
          store 4 $d $.1 0

    
    $.2 = load 4 $a 0
    $.3 = load 4 $b 0
    $.4 = add $.2 $.3
    $.5 = load 4 $c 0
    $.6 = add $.4 $.5
    $.7 = load 4 $d 0
    $.8 = add $.6 $.7

    ret $.8
}

func main {
%main_entry:
    $x = move 10
    $y = move 20
    $x = call sum $x $y 
    ret $x
}
";

        private const string NonSsaSample3 = @"
void swap $a $b {
%min_entry:
    $.2 = load 4 $a 0
    $.3 = load 4 $b 0
          store 4 $a $.3 0
          store 4 $b $.2 0
    ret undef
}

func main {
%main_entry:
    $x = alloca 4
    $y = alloca 4

    $.0 = move 123
    store 4 $x $.0 0

    $.1 = move 456
    store 4 $y $.1 0

    call swap $x $y 

    $.2 = load 4 $x 0

    ret $.2
}
";

        private const string SsaSample = @"
func main {
%main.entry:
    $n.1  = move 10
    $f0.1 = move 0
    $f1.1 = move 1
    $i.1  = move 1
    jump %for_cond

%for_cond:
    $f2.1 = phi %for_step $f2.2  %main.entry undef
    $f1.2 = phi %for_step $f1.3  %main.entry $f1.1
    $i.2  = phi %for_step $i.3   %main.entry $i.1
    $f0.2 = phi %for_step $f0.3  %main.entry $f0.1
    $t.1  = slt $i.2 $n.1
    br $t.1 %for_loop %for_after

%for_loop:
    $t_2.1 = add $f0.2 $f1.2
    $f2.2  = move $t_2.1
    $f0.3  = move $f1.2
    $f1.3  = move $f2.2
    jump %for_step

%for_step:
    $i.3   = add $i.2 1
    jump %for_cond

%for_after:
    ret $f2.2
}
";
        public static void Main(string[] args) {
            if (System.Diagnostics.Debugger.IsAttached) {
                //{
                //    var program = LowLevelIntermediateRepresentation.Parser.Parse(NonSsaSample1, false);
                //    RunOne(program, false, true);
                //}
                {
                    var program = LowLevelIntermediateRepresentation.Parser.Parse(NonSsaSample3, false);
                    RunOne(program, false, true);
                }
                //{
                //    var program = LowLevelIntermediateRepresentation.Parser.Parse(SsaSample, true);
                //    RunOne(program, true, true);
                //}
                return;
            } else {
                var ssa = false;
                var dump = false;
                for (var i = 0; i < args.Length; i++) {
                    switch (args[i].Trim()) {
                        case "-ssa": {
                                ssa = true;
                                continue;
                            }
                        case "-dump": {
                                dump = true;
                                continue;
                            }
                        default: {
                                var program = LowLevelIntermediateRepresentation.Parser.Parse(Console.In, ssa);
                                RunOne(program, ssa, dump);
                                return;

                            }
                    }
                }
                Console.WriteLine("file not found.");
            }
        }

        private static void RunOne(LowLevelIntermediateRepresentation.Program program, bool ssa, bool dump) {
            if (dump) {
                Console.WriteLine("Program:");
                Console.WriteLine(program.ToString());
            }

            Console.WriteLine(ssa ? "running with SSA mode" : "running without SSA mode");

            try {

                Function mainFunction;
                if (!program.Functions.TryGetValue("main", out mainFunction)) {
                    throw new RuntimeError(null, "cannot find `main` function");
                }

                Context ctx = new Context(program, mainFunction);

                while (ctx.IsReady) {
                    Interpreter.RunInstruction(ref ctx);
                }
                Console.WriteLine("ExitCode:  " + ctx.ExitCode);
            } catch (RuntimeError e) {
                Console.WriteLine("Exception: " + e.Message);
            }

        }
    }
}

namespace LowLevelIntermediateRepresentation {

    public static class Ext {
        internal static TResult Apply<TInput, TResult>(this TInput self, Func<TInput, TResult> predicate) {
            return predicate(self);
        }
    }

    /// <summary>
    /// 文法構造違反例外
    /// </summary>
    public class SemanticError : Exception {
        public SemanticError(int lineNo, string line, string reason) : base($"{reason} | line {lineNo}: {line}") { }
    }

    /// <summary>
    /// 実行時例外
    /// </summary>
    public class RuntimeError : Exception {
        public RuntimeError(IInstruction curInst, string reason) : base(curInst != null ? ($"{reason } | line {curInst.LineNo}: {curInst.LineText}") : reason) { }
    }


    /// <summary>
    /// 中間表現の基底インタフェース
    /// </summary>
    public interface IInstruction {
        int LineNo { get; }
        string LineText { get; }
    }

    /// <summary>
    /// 中間表現が左辺代入を有することを示すインタフェース
    /// </summary>
    public interface IHasDestInstruction {
        string Dest { get; }
    }

    /// <summary>
    /// 中間表現がジャンプ命令であることを示すインタフェース
    /// </summary>
    public interface IJumpInstruction {
    }

    /// <summary>
    /// 中間表現の文字列化
    /// </summary>
    public static class StringWriter {
        public static string ToString(Phi inst) {
            return $"\t{inst.Dest} = phi {inst.Paths.SelectMany(x => new[] { x.Key, x.Value }).Apply(x => String.Join(" ", x))}";
        }

        internal static string ToString(Store inst) {
            return $"\tstore {inst.Size} {inst.Address} {inst.Src} {inst.Offset}";
        }

        internal static string ToString(Load inst) {
            return $"\t{inst.Dest} = load {inst.Size} {inst.Address} {inst.Offset}";
        }

        internal static string ToString(Alloc inst) {
            return $"\t{inst.Dest} = alloc {inst.Size}";
        }

        internal static string ToString(Alloca inst) {
            return $"\t{inst.Dest} = alloca {inst.Size}";
        }

        internal static string ToString(Call inst) {
            if (inst.Dest == null) {
                return $"\tcall {inst.FunctionName} {inst.Args.Apply(x => String.Join(" ", x))}";
            } else {
                return $"\t{inst.Dest} = call {inst.Args.Apply(x => String.Join(" ", x))}";
            }
        }

        internal static string ToString(Br inst) {
            return $"\tbr {inst.Condition} {inst.IfTrue} {inst.IfFalse}";
        }

        internal static string ToString(Ret inst) {
            return $"\tret {inst.Src}";
        }

        internal static string ToString(Jump inst) {
            return $"\tjump {inst.Target}";
        }

        internal static string ToString(Move inst) {
            return $"\t{inst.Dest} = move {inst.Src}";
        }

        internal static string ToString(BinOp inst) {
            return $"\t{inst.Dest} = {inst.Op} {inst.Src1} {inst.Src2}";
        }

        internal static string ToString(UnaryOp inst) {
            return $"\t{inst.Dest} = {inst.Op} {inst.Src}";
        }

        internal static string ToString(CondOp inst) {
            return $"\t{inst.Dest} = {inst.Op} {inst.Src1} {inst.Src2}";
        }

        internal static string ToString(BasicBlock basicBlock) {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"{basicBlock.Label}:");
            basicBlock.Phis.Aggregate(sb, (s, x) => s.AppendLine(x.ToString()));
            basicBlock.Instructions.Aggregate(sb, (s, x) => s.AppendLine(x.ToString()));
            return sb.ToString();
        }

        internal static string ToString(Function function) {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"func {function.Name} {function.Args.Apply(x => String.Join(" ", x))} {{");
            function.Blocks.Aggregate(sb, (s, x) => s.AppendLine(x.Value.ToString()));
            sb.AppendLine($"}}");
            return sb.ToString();
        }

        internal static string ToString(Register register) {
            return $"{register.Value}({register.Timestamp})";
        }

        internal static string ToString(Program program) {
            return program.Functions.Select(x => x.Value.ToString()).Apply(x => String.Join("\r\n", x));
        }
    }

    /// <summary>
    /// φ関数
    /// </summary>
    public class Phi : IInstruction, IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public Dictionary<string, string> Paths { get; }

        public Phi(int lineNo, string lineText, string dest) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Paths = new Dictionary<string, string>();
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// メモリ書き込み
    /// </summary>
    public class Store : IInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Address { get; }
        public string Src { get; }
        public int Size { get; }
        public int Offset { get; }

        public Store(int lineNo, string lineText, string address, string src, int size, int offset) {
            LineNo = lineNo;
            LineText = lineText;
            Address = address;
            Src = src;
            Size = size;
            Offset = offset;
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// メモリ読み取り
    /// </summary>
    public class Load : IInstruction, IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Address { get; }
        public int Size { get; }
        public int Offset { get; }

        public Load(int lineNo, string lineText, string dest, string address, int size, int offset) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Address = address;
            Size = size;
            Offset = offset;
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// グローバルヒープ確保
    /// </summary>
    public class Alloc : IInstruction, IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Size { get; }

        public Alloc(int lineNo, string lineText, string dest, string size) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Size = size;
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// ローカルヒープ確保
    /// </summary>
    public class Alloca : IInstruction, IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Size { get; }

        public Alloca(int lineNo, string lineText, string dest, string size) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Size = size;
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// 関数呼び出し
    /// </summary>
    public class Call : IInstruction, IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string FunctionName { get; }
        public List<string> Args { get; }

        public Call(int lineNo, string lineText, string dest, string functionName, List<string> args) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            FunctionName = functionName;
            Args = args;
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// 分岐
    /// </summary>
    public class Br : IInstruction, IJumpInstruction {
        public int LineNo { get; }
        public string LineText { get; }
        public string Condition { get; }
        public string IfTrue { get; }
        public string IfFalse { get; }

        public Br(int lineNo, string lineText, string condition, string ifTrue, string ifFalse) {
            LineNo = lineNo;
            LineText = lineText;
            Condition = condition;
            IfTrue = ifTrue;
            IfFalse = ifFalse;
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// 関数終了
    /// </summary>
    public class Ret : IInstruction, IJumpInstruction {
        public int LineNo { get; }
        public string LineText { get; }
        public string Src { get; }
        public Ret(int lineNo, string lineText, string src) {
            LineNo = lineNo;
            LineText = lineText;
            Src = src;
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// ジャンプ
    /// </summary>
    public class Jump : IInstruction, IJumpInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Target { get; }
        public Jump(int lineNo, string lineText, string target) {
            LineNo = lineNo;
            LineText = lineText;
            Target = target;
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// レジスタのコピー
    /// </summary>
    public class Move : IInstruction, IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Src { get; }
        public Move(int lineNo, string lineText, string dest, string src) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Src = src;
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// 二項演算子
    /// </summary>
    public class BinOp : IInstruction, IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Op { get; }
        public string Src1 { get; }
        public string Src2 { get; }

        public BinOp(int lineNo, string lineText, string dest, string op, string src1, string src2) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Op = op;
            Src1 = src1;
            Src2 = src2;
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// 単項演算子
    /// </summary>
    public class UnaryOp : IInstruction, IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Op { get; }
        public string Src { get; }

        public UnaryOp(int lineNo, string lineText, string dest, string op, string src) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Op = op;
            Src = src;
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// 比較演算子
    /// </summary>
    public class CondOp : IInstruction, IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Op { get; }
        public string Src1 { get; }
        public string Src2 { get; }

        public CondOp(int lineNo, string lineText, string dest, string op, string src1, string src2) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Op = op;
            Src1 = src1;
            Src2 = src2;
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// 基本ブロック
    /// </summary>
    public class BasicBlock {
        public string Label { get; }
        public List<IInstruction> Instructions { get; }
        public List<Phi> Phis { get; }

        public BasicBlock(string label) {
            Label = label;
            Instructions = new List<IInstruction>();
            Phis = new List<Phi>();
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// 関数
    /// </summary>
    public class Function {
        public bool HasReturnValue { get; }
        public string Name { get; }
        public BasicBlock EntryBlock { get; set; }
        public List<string> Args { get; }
        public Dictionary<string, BasicBlock> Blocks { get; }

        public Function(bool hasReturnValue, string name, List<string> args) {
            HasReturnValue = hasReturnValue;
            Name = name;
            Args = args;
            Blocks = new Dictionary<string, BasicBlock>();
        }
        public override string ToString() {
            return StringWriter.ToString(this);
        }

    }

    /// <summary>
    /// レジスタ
    /// </summary>
    public class Register {
        public int Value { get; set; }
        public int Timestamp { get; set; }
        public override string ToString() {
            return StringWriter.ToString(this);
        }
    }

    /// <summary>
    /// パーサ
    /// </summary>
    public class Parser {
        private Dictionary<string, Function> Functions { get; }
        private BasicBlock CurrentBasicBlock { get; set; }
        private Function CurrentFunction { get; set; }

        private int LineNo { get; set; }
        private string LineText { get; set; }
        private bool AllowPhi { get; set; }

        private TextReader Reader { get; }
        private bool SsaMode { get; }


        public Parser(TextReader reader, bool ssaMode) {
            Functions = new Dictionary<string, Function>();
            CurrentBasicBlock = null;
            CurrentFunction = null;
            LineNo = 0;
            LineText = null;
            AllowPhi = false;

            Reader = reader;
            SsaMode = ssaMode;
        }

        private string ReadLine() {
            do {
                LineText = Reader.ReadLine();
                if (LineText == null) {
                    break;
                }

                LineNo += 1;
                LineText = LineText.Trim();
            } while (LineText == "");

            return LineText;
        }

        private static List<string> SplitBySpaces(string line) {
            return System.Text.RegularExpressions.Regex.Split(line.Trim(), @" +").ToList();
        }

        private SemanticError SemanticError(string msg) {
            throw new SemanticError(LineNo, LineText, msg);
        }

        private void ReadInstruction() {
            // basic block
            if (LineText.StartsWith("%")) {
                if (!LineText.EndsWith(":")) {
                    throw SemanticError("expected a `:`");
                }

                CurrentBasicBlock = new BasicBlock(LineText.Substring(0, LineText.Length - 1));
                if (CurrentFunction.Blocks.ContainsKey(CurrentBasicBlock.Label)) {
                    throw SemanticError($"label `{CurrentBasicBlock.Label}` has already been defined");
                }

                CurrentFunction.Blocks.Add(CurrentBasicBlock.Label, CurrentBasicBlock);
                if (CurrentFunction.EntryBlock == null) {
                    CurrentFunction.EntryBlock = CurrentBasicBlock;
                }

                AllowPhi = SsaMode;
                return;
            }

            // parse instruction
            string[] split = LineText.Split('=');
            List<string> words = SplitBySpaces(split[split.Length - 1]);

            // save operands to variables
            switch (words[0]) {
                case "store": {
                        if (split.Length != 1) {
                            throw SemanticError($"illegal operator {words[0]}");
                        }

                        var inst = new Store(
                            lineNo: LineNo,
                            lineText: LineText,
                            address: words[2],
                            src: words[3],
                            size: int.Parse(words[1]),
                            offset: int.Parse(words[4])
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "load": {
                        if (split.Length != 2) {
                            throw SemanticError($"illegal operator {words[0]}");
                        }

                        var inst = new Load(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim(),
                            address: words[2],
                            size: int.Parse(words[1]),
                            offset: int.Parse(words[3])
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "alloc": {
                        if (split.Length != 2) {
                            throw SemanticError($"illegal operator {words[0]}");
                        }

                        var inst = new Alloc(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim(),
                            size: words[1]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "alloca": {
                        if (split.Length != 2) {
                            throw SemanticError($"illegal operator {words[0]}");
                        }

                        var inst = new Alloca(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim(),
                            size: words[1]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "call": {
                        if (split.Length == 1) {
                            var inst = new Call(
                                lineNo: LineNo,
                                lineText: LineText,
                                dest: null,
                                functionName: words[1],
                                args: words.GetRange(2, words.Count - 2)
                            );
                            AllowPhi = false;
                            CurrentBasicBlock.Instructions.Add(inst);
                            return;
                        } else if (split.Length == 2) {
                            var inst = new Call(
                                lineNo: LineNo,
                                lineText: LineText,
                                dest: split[0].Trim(),
                                functionName: words[1],
                                args: words.GetRange(2, words.Count - 2)
                            );
                            AllowPhi = false;
                            CurrentBasicBlock.Instructions.Add(inst);
                            return;
                        } else {
                            throw SemanticError($"illegal operator {words[0]}");
                        }
                    }
                case "br": {
                        if (split.Length != 1) {
                            throw SemanticError($"illegal operator {words[0]}");
                        }

                        var inst = new Br(
                            lineNo: LineNo,
                            lineText: LineText,
                            condition: words[1],
                            ifTrue: words[2],
                            ifFalse: words[3]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "phi": {
                        if (split.Length != 2) {
                            throw SemanticError($"illegal operator {words[0]}");
                        }

                        if (!AllowPhi) {
                            throw SemanticError("`phi` is not allowed here");
                        }

                        if ((words.Count & 1) == 0) {
                            throw SemanticError("the number of `phi` argument should be even");
                        }

                        var phi = new Phi(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim()
                        );

                        for (var i = 1; i < words.Count; i += 2) {
                            var label = words[i + 0];
                            var reg = words[i + 1];
                            if (!label.StartsWith("%")) {
                                throw SemanticError("label should starts with `%`");
                            }

                            if (!reg.StartsWith("$") && reg != "undef") {
                                throw SemanticError("source of a phi node should be a register or `undef`");
                            }

                            phi.Paths.Add(label, reg);
                        }

                        CurrentBasicBlock.Phis.Add(phi);
                        return;
                    }
                case "jump": {
                        if (split.Length != 1) {
                            throw SemanticError($"illegal operator {words[0]}");
                        }

                        var inst = new Jump(
                            lineNo: LineNo,
                            lineText: LineText,
                            target: words[1]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "ret": {
                        if (split.Length != 1) {
                            throw SemanticError($"illegal operator {words[0]}");
                        }

                        var inst = new Ret(
                            lineNo: LineNo,
                            lineText: LineText,
                            src: words[1]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }

                case "move": {
                        if (split.Length != 2) {
                            throw SemanticError($"illegal operator {words[0]}");
                        }

                        var inst = new Move(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim(),
                            src: words[1]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "add":
                case "sub":
                case "mul":
                case "div":
                case "rem":
                case "shl":
                case "shr":
                case "and":
                case "or":
                case "xor": {
                        if (split.Length != 2) {
                            throw SemanticError($"illegal operator {words[0]}");
                        }

                        var inst = new BinOp(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim(),
                            op: words[0],
                            src1: words[1],
                            src2: words[2]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "neg":
                case "not": {
                        if (split.Length != 2) {
                            throw SemanticError($"illegal operator {words[0]}");
                        }

                        var inst = new UnaryOp(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim(),
                            op: words[0],
                            src: words[1]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "slt":
                case "sgt":
                case "sle":
                case "sge":
                case "seq":
                case "sne": {
                        if (split.Length != 2) {
                            throw SemanticError($"illegal operator {words[0]}");
                        }

                        var inst = new CondOp(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim(),
                            op: words[0],
                            src1: words[1],
                            src2: words[2]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                default: {
                        throw SemanticError($"illegal operator {words[0]}");
                    }
            }
        }

        private void ReadFunction() {
            var words = SplitBySpaces(LineText);
            if (words[words.Count - 1] != "{") {
                throw SemanticError("expected a `{`");
            }

            CurrentFunction = new Function(
                hasReturnValue: LineText.StartsWith("func "),
                name: words[1],
                args: words.GetRange(2, words.Count - 1 - 2)
            );
            if (Functions.ContainsKey(CurrentFunction.Name)) {
                throw SemanticError($"function `{ CurrentFunction.Name}` has already been defined");
            }

            Functions.Add(CurrentFunction.Name, CurrentFunction);

            AllowPhi = SsaMode;
            while (ReadLine() != "}") {
                ReadInstruction();
            }
        }

        public static Program Parse(TextReader sr, bool ssaMode) {
            var parser = new Parser(sr, ssaMode);

            while (parser.ReadLine() != null) {
                if (parser.LineText.StartsWith("func ") ||
                    parser.LineText.StartsWith("void ")) {
                    parser.ReadFunction();
                }
            }

            var program = new Program(parser.Functions);
            if (ssaMode) {
                program.CheckValid();
            }

            return program;
        }

        public static Program Parse(string str, bool ssaMode) {
            using (var sr = new StringReader(str)) {
                return Parse(sr, ssaMode);
            }
        }
    }

    /// <summary>
    /// プログラム
    /// </summary>
    public class Program {
        internal Dictionary<string, Function> Functions { get; }

        internal Program(Dictionary<string, Function> functions) {
            Functions = functions;
        }

        public void CheckValid() {
            HashSet<string> regDef = new HashSet<string>();
            foreach (var func in Functions.Values) {
                regDef.Clear();
                foreach (var basicBlock in func.Blocks.Values) {
                    // SSA形式チェック(1): レジスタの定義は１度のみ
                    foreach (var inst in basicBlock.Instructions) {
                        var hasDestInst = inst as IHasDestInstruction;
                        if (hasDestInst != null) {
                            if (regDef.Contains(hasDestInst.Dest)) {
                                var lineNo = inst.LineNo;
                                var lineText = inst.LineText;
                                throw new SemanticError(lineNo, lineText, "a register should only be defined once");
                            } else {
                                regDef.Add(hasDestInst.Dest);
                            }
                        }
                    }
                    // SSA形式チェック(2): ブロック末尾はジャンプ命令である
                    if (!(basicBlock.Instructions.Last() is IJumpInstruction)) {
                        var inst = basicBlock.Instructions.Last();
                        throw new SemanticError(inst.LineNo, inst.LineText, "last instruction of basic block should JumpInstruction");
                    }
                }
            }
        }

        public override string ToString() {
            return StringWriter.ToString(this);
        }

    }

    /// <summary>
    /// インタプリタの評価コンテキスト
    /// </summary>
    public class Context {
        /// <summary>
        /// ヒープの開始アドレス
        /// </summary>
        private const int HeapBase = 0x00000000;
        /// <summary>
        /// スタックの開始アドレス
        /// </summary>
        private const int StackBase = 0x40000000;

        public Random Randomize { get; }

        public int CountInstruction { get; internal set; }
        public int CurrentInstructionCounter { get; set; }
        public IInstruction CurrentInstruction {
            get {
                return CurrentBasicBlock.Instructions[CurrentInstructionCounter];
            }
        }

        public int InstructionLimit { get; internal set; }

        public int HeapTop { get; set; }
        public int StackTop { get; set; }

        public Dictionary<string, Register> Registers { get; set; }
        public Dictionary<int, byte> Memory { get; }

        public bool IsReady { get; set; }
        public int ExitCode { get; set; }
        public Exception Exception { get; set; }
        public Function CurrentFunction { get; internal set; }
        public BasicBlock LastBasicBlock { get; internal set; }
        public BasicBlock CurrentBasicBlock { get; internal set; }
        public Program Program { get; internal set; }
        public Context ParentContext { get; internal set; }

        public Context(Program program, Function function) {
            Program = program;
            Randomize = new Random();
            Registers = new Dictionary<string, Register>();
            Memory = new Dictionary<int, byte>();

            HeapTop = Randomize.Next(4096) + HeapBase;
            StackTop = Randomize.Next(4096) + StackBase;
            ExitCode = 0;
            IsReady = true;
            CountInstruction = 0;
            LastBasicBlock = null;

            CurrentBasicBlock = function.EntryBlock;
            CurrentFunction = function;
            CurrentInstructionCounter = 0;
            Exception = null;
            InstructionLimit = int.MaxValue;

            ParentContext = null;
        }

        public Context(Context parentContext, Function function) {
            Program = parentContext.Program;
            Randomize = parentContext.Randomize;
            Registers = new Dictionary<string, Register>();
            Memory = parentContext.Memory;

            HeapTop = parentContext.HeapTop;
            StackTop = parentContext.StackTop;
            ExitCode = 0;
            IsReady = true;
            CountInstruction = 0;
            LastBasicBlock = null;

            CurrentBasicBlock = function.EntryBlock;
            CurrentFunction = function;
            CurrentInstructionCounter = 0;
            Exception = null;
            InstructionLimit = int.MaxValue;

            ParentContext = parentContext;


        }

        public byte MemoryRead(int address) {
            byte value;
            if (!Memory.TryGetValue(address, out value)) {
                throw new RuntimeError(CurrentInstruction, "memory read violation");
            }

            return value;
        }

        public void MemoryWrite(int address, byte value) {
            if (!Memory.ContainsKey(address)) {
                throw new RuntimeError(CurrentInstruction, "memory write violation");
            }

            Memory[address] = value;
        }

        public int RegisterRead(string name) {
            Register register;
            if (!Registers.TryGetValue(name, out register)) {
                throw new RuntimeError(CurrentInstruction, $"register `{name}` haven't been defined yet");
            }

            return register.Value;
        }

        public void RegisterWrite(string name, int value) {
            if (!name.StartsWith("$")) {
                throw new RuntimeError(CurrentInstruction, "not a register");
            }

            Register register;
            if (!Registers.TryGetValue(name, out register)) {
                register = new Register();
                Registers.Add(name, register);
            }

            register.Value = value;
            register.Timestamp = CountInstruction;
        }

        public int ReadSrc(string name) {
            if (name.StartsWith("$")) {
                return RegisterRead(name);
            } else {
                return int.Parse(name);
            }
        }

        public int MemoryAllocate(int size) {
            var ret = HeapTop;
            for (var i = 0; i < size; ++i) {
                Memory[HeapTop + i] = (byte)Randomize.Next(256);
            }

            HeapTop += Randomize.Next(4096);
            return ret;
        }

        public int StackAllocate(int size) {
            var ret = StackTop;
            for (var i = 0; i < size; ++i) {
                Memory[StackTop + i] = (byte)Randomize.Next(256);
            }

            StackTop += Randomize.Next(4096);
            return ret;
        }
    }

    /// <summary>
    /// 中間表現評価器
    /// </summary>
    public static class Interpreter {

        public static void RunInstruction(ref Context context) {
            var currentInstruction = context.CurrentInstruction;
            if (++context.CountInstruction >= context.InstructionLimit) {
                throw new RuntimeError(currentInstruction, "instruction limit exceeded");
            }

            if (currentInstruction is Load) {
                context = OnLoad(context, currentInstruction as Load);
            } else if (currentInstruction is Store) {
                context = OnStore(context, currentInstruction as Store);
            } else if (currentInstruction is Alloc) {
                context = OnAlloc(context, currentInstruction as Alloc);
            } else if (currentInstruction is Alloca) {
                context = OnAlloca(context, currentInstruction as Alloca);
            } else if (currentInstruction is Ret) {
                context = OnRet(context, currentInstruction as Ret);
            } else if (currentInstruction is Br) {
                context = OnBr(context, currentInstruction as Br);
            } else if (currentInstruction is Jump) {
                context = OnJump(context, currentInstruction as Jump);
            } else if (currentInstruction is Call) {
                context = OnCall(context, currentInstruction as Call);
            } else if (currentInstruction is BinOp) {
                context = OnBinOp(context, currentInstruction as BinOp);
            } else if (currentInstruction is Move) {
                context = OnMove(context, currentInstruction as Move);
            } else if (currentInstruction is UnaryOp) {
                context = OnUnaryOp(context, currentInstruction as UnaryOp);
            } else if (currentInstruction is CondOp) {
                context = OnCondOp(context, currentInstruction as CondOp);
            } else {
                context = OnUnknownOp(context, currentInstruction);
            }
        }

        private static void RunJump(Context context, string name) {
            BasicBlock basicBlock;
            if (!context.CurrentFunction.Blocks.TryGetValue(name, out basicBlock)) {
                throw new RuntimeError(context.CurrentInstruction, "cannot resolve block `" + name + "` in function `" + context.CurrentFunction.Name + "`");
            }

            context.LastBasicBlock = context.CurrentBasicBlock;
            context.CurrentBasicBlock = basicBlock;
            context.CurrentInstructionCounter = 0;

            // run phi nodes concurrently
            if (context.CurrentBasicBlock.Phis.Any()) {
                context.CountInstruction += 1;
                var tempRegister = new Dictionary<string, int>();
                foreach (var phi in context.CurrentBasicBlock.Phis) {
                    var CurrentInstruction = phi;
                    string registerName;
                    if (!phi.Paths.TryGetValue(context.LastBasicBlock.Label, out registerName)) {
                        throw new RuntimeError(CurrentInstruction, "this phi node has no value from incoming block `" + context.LastBasicBlock.Label + "`");
                    } else {
                        var value = registerName == "undef" ? context.Randomize.Next(int.MaxValue) : context.ReadSrc(registerName);
                        tempRegister.Add(phi.Dest, value);
                    }
                }

                foreach (var e in tempRegister) {
                    context.RegisterWrite(e.Key, e.Value);
                }
            }
        }

        private static Context OnUnknownOp(Context context, IInstruction inst) {
            throw new RuntimeError(inst, "unknown operation `" + inst.GetType().Name + "`");
        }

        private static Context OnCondOp(Context context, CondOp inst) {
            switch (inst.Op) {
                case "slt": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) < context.ReadSrc(inst.Src2) ? 1 : 0);
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "sgt": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) > context.ReadSrc(inst.Src2) ? 1 : 0);
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "sle": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) <= context.ReadSrc(inst.Src2) ? 1 : 0);
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "sge": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) >= context.ReadSrc(inst.Src2) ? 1 : 0);
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "seq": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) == context.ReadSrc(inst.Src2) ? 1 : 0);
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "sne": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) != context.ReadSrc(inst.Src2) ? 1 : 0);
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                default: {
                        throw new RuntimeError(inst, "unknown condop `" + inst.Op + "`");
                    }
            }
        }

        private static Context OnUnaryOp(Context context, UnaryOp inst) {
            switch (inst.Op) {
                case "neg": {
                        context.RegisterWrite(inst.Dest, -context.ReadSrc(inst.Src));
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "not": {
                        context.RegisterWrite(inst.Dest, ~context.ReadSrc(inst.Src));
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                default: {
                        throw new RuntimeError(inst, "unknown unaryop `" + inst.Op + "`");
                    }
            }
        }

        private static Context OnMove(Context context, Move inst) {

            context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src));
            context.CurrentInstructionCounter += 1;
            return context;
        }

        private static Context OnBinOp(Context context, BinOp inst) {
            switch (inst.Op) {
                case "add": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) + context.ReadSrc(inst.Src2));
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "sub": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) - context.ReadSrc(inst.Src2));
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "mul": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) * context.ReadSrc(inst.Src2));
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "div": {
                        if (context.ReadSrc(inst.Src2) == 0) {
                            throw new RuntimeError(inst, "divide by zero");
                        }

                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) / context.ReadSrc(inst.Src2));
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "rem": {
                        if (context.ReadSrc(inst.Src2) == 0) {
                            throw new RuntimeError(inst, "mod by zero");
                        }

                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) % context.ReadSrc(inst.Src2));
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "shl": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) << context.ReadSrc(inst.Src2));
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "shr": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) >> context.ReadSrc(inst.Src2));
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "and": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) & context.ReadSrc(inst.Src2));
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "or": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) | context.ReadSrc(inst.Src2));
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                case "xor": {
                        context.RegisterWrite(inst.Dest, context.ReadSrc(inst.Src1) ^ context.ReadSrc(inst.Src2));
                        context.CurrentInstructionCounter += 1;
                        return context;
                    }
                default: {
                        throw new RuntimeError(inst, "unknown binop `" + inst.Op + "`");
                    }
            }
        }

        private static Context OnCall(Context context, Call inst) {
            Function function;
            if (!context.Program.Functions.TryGetValue(inst.FunctionName, out function)) {
                throw new RuntimeError(inst, "cannot resolve function `" + inst.FunctionName + "`");
            }

            if (inst.Dest != null && !function.HasReturnValue) {
                throw new RuntimeError(inst, "function `" + function.Name + "` has not return value");
            }

            if (inst.Args.Count != function.Args.Count) {
                throw new RuntimeError(inst, "argument size cannot match");
            }

            var nextContext = new Context(context, function);

            for (var i = 0; i < inst.Args.Count; ++i) {
                var name = function.Args[i];
                Register register;
                if (!nextContext.Registers.TryGetValue(name, out register)) {
                    register = new Register();
                    nextContext.Registers.Add(name, register);
                }

                register.Value = context.ReadSrc(inst.Args[i]);
                register.Timestamp = context.CountInstruction;
            }

            context = nextContext;

            return context;
        }

        private static Context OnJump(Context context, Jump inst) {
            RunJump(context, inst.Target);
            return context;
        }

        private static Context OnBr(Context context, Br inst) {
            var cond = context.ReadSrc(inst.Condition);
            RunJump(context, cond == 0 ? inst.IfFalse : inst.IfTrue);
            return context;
        }

        private static Context OnRet(Context context, Ret inst) {
            var function = context.CurrentFunction;

            if (context.ParentContext == null) {
                if (function.HasReturnValue && inst.Src != "undef") {
                    var retValue = context.ReadSrc(inst.Src);
                    context.ExitCode = retValue;
                } else if (function.HasReturnValue == false && inst.Src == "undef") {
                    //context.ExitCode = 0;
                } else {
                    // それ以外はエラー
                    throw new RuntimeError(context.CurrentInstruction, "return value is not captured in function `" + context.CurrentFunction.Name + "`");
                }

                context.IsReady = false;
                return context;
            } else {
                var parentContext = context.ParentContext;

                var callInst = parentContext.CurrentInstruction as Call;
                if (callInst.Dest != null && function.HasReturnValue && inst.Src != "undef") {
                    // 戻り値があって受け取る
                    var retValue = context.ReadSrc(inst.Src);
                    parentContext.RegisterWrite(callInst.Dest, retValue);
                } else if (callInst.Dest == null && function.HasReturnValue == false && inst.Src == "undef") {
                    // 戻り値はない
                } else {
                    // それ以外はエラー
                    throw new RuntimeError(context.CurrentInstruction, "return value is not captured in function `" + context.CurrentFunction.Name + "`");
                }

                context = parentContext;
                context.CurrentInstructionCounter += 1;
                return context;
            }
        }

        private static Context OnAlloca(Context context, Alloca inst) {
            var size = context.ReadSrc(inst.Size);
            var heapAddress = context.StackAllocate(size);
            context.RegisterWrite(inst.Dest, heapAddress);

            context.CurrentInstructionCounter += 1;
            return context;
        }

        private static Context OnAlloc(Context context, Alloc inst) {
            var size = context.ReadSrc(inst.Size);
            var heapAddress = context.MemoryAllocate(size);
            context.RegisterWrite(inst.Dest, heapAddress);
            context.CurrentInstructionCounter += 1;
            return context;
        }

        private static Context OnStore(Context context, Store inst) {
            var address = context.ReadSrc(inst.Address) + inst.Offset;
            var data = context.ReadSrc(inst.Src);
            for (var i = inst.Size - 1; i >= 0; --i) {
                context.MemoryWrite(address + i, (byte)(data & 0xFF));
                data >>= 8;
            }

            context.CurrentInstructionCounter += 1;
            return context;
        }

        private static Context OnLoad(Context context, Load inst) {
            var address = context.ReadSrc(inst.Address) + inst.Offset;
            var res = 0;
            for (var i = 0; i < inst.Size; ++i) {
                res = (res << 8) | context.MemoryRead(address + i);
            }

            context.RegisterWrite(inst.Dest, res);
            context.CurrentInstructionCounter += 1;
            return context;
        }
    }
}

/*
Program 
  = _ functions: FuncOrProc+ _ { return functions; }

FuncOrProc
  = _ kind:( "func" / "void" ) _ name:Ident _ args:Args _ ":" _ ty:Type _ "{" _ blocks:Block+ _ "}" { return { kind: kind, name: name, args: args, retty: ty, blocks:blocks}; }

Args
  = "(" _ a:Arg _ as:("," _ arg:Arg { return arg; })*  _ ")" { return [a].concat(as); }
  / "(" _ ")" { return []; }

Arg
  = _ reg:Reg _ ":" _ ty:Type _ { return {reg:reg,type:ty}; }

Type "type"
  = [ui]("8"/"16"/"32"/"64") { return text(); }
  / [f]("32"/"64") { return text(); }
  
Block
  = _ name:Label _ ':' _ insts:Inst+ { return {label:name, insts:insts}; }

Inst
  = _ "ret"  _ value:RegOrUndef { return { op:"ret", value:value }; }
  / _ "jump" _ target:Label  { return { op:"jump", target:target }; }
  / _ "br" _ cond:Reg _ thenLabel:Label _ elseLabel:Label  { return { op: "br", cond:cond, thenLabel:thenLabel, elseLabel:elseLabel}; }
  / _ "store" _ ty:Type _ address:Reg _ src:Reg { return { op: "store", ty:ty, address:address, src:src}; }
  / _ "call" _ name:Ident _ args:Reg* { return { op: "call", dest: null, name: name, args: args}; }
  / _ dest:Reg _ "=" _ "load" _ ty:Type _ address:Reg { return { op: "load", dest: dest, ty: ty, address: address}; }
  / _ dest:Reg _ "=" _ "alloca" _ ty:Type _ cnt:Integer { return { op: "alloca", dest: dest, ty: ty, cnt: cnt }; }
  / _ dest:Reg _ "=" _ "alloc" _ size:Reg { return { op: "alloc", dest: dest, size: size}; }
  / _ dest:Reg _ "=" _ "call" _ name:Ident args: (_ reg:Reg {return reg;})* { return { op: "call", dest: dest, name: name, args: args}; }
  / _ dest:Reg _ "=" _ "move" _ ty:Type _ src:(Reg / Integer) { return { op: "move", dest: dest, ty: ty, src: src}; }
  / _ dest:Reg _ "=" _ "phi" _ ty1:Type _  targets:(_ label:Label _ ty2:Type _ reg:RegOrUndef { return {block: label, ty: ty2, reg: reg} } )*  { return { op: "phi", ty: ty1, targets: targets}; }
  / _ dest:Reg _ "=" _ op:UnaryOp _ ty:Type _ src:Reg  { return { op: op, dest: dest, ty: ty, src: src}; }
  / _ dest:Reg _ "=" _ op:BinOp _ ty:Type _ src1:Reg _ src2:Reg { return { op: op, dest:dest, ty:ty, src1:src1, src2: src2}; }

UnaryOp "unaryop"
  = "neg" { return text(); }
  / "not" { return text(); }
  
BinOp "binop"
  = "add" { return text(); }
  / "sub" { return text(); }
  / "mul" { return text(); }
  / "div" { return text(); }
  / "rem" { return text(); }
  / "shl" { return text(); }
  / "shr" { return text(); }
  / "and" { return text(); }
  / "or"  { return text(); }
  / "xor" { return text(); }
  / "slt" { return text(); }
  / "sgt" { return text(); }
  / "sle" { return text(); }
  / "sge" { return text(); }
  / "seq" { return text(); }
  / "sne" { return text(); }
  
Reg "reg"
  = ("$" [\.A-Za-z_0-9]+) { return text(); }

RegOrUndef "regorundef"
  = Reg
  / "undef"  { return text(); }

Label "label"
  = "%" [\.A-Za-z_0-9]+ { return text(); }

Ident "ident"
  = [A-Za-z_] [A-Za-z_0-9]+ { return text(); }

Integer "integer"
  = [0-9]+ { return parseInt(text(), 10); }

_ "whitespace"
  = [ \t\n\r]*
*/
