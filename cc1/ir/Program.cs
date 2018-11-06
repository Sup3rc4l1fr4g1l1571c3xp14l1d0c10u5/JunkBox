using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace LLIR {
    public class Program {
        public static void Main(string[] args) {
            bool ssa = true;//args.Length > 0 && args[0].Trim() == "+ssa";
            LowLevelIntermediateRepresentation.Program program = LowLevelIntermediateRepresentation.Parser.Parse(Console.In, ssa);
            LowLevelIntermediateRepresentation.Interpreter interpreter = new LowLevelIntermediateRepresentation.Interpreter(program);
            Console.WriteLine(ssa ? "running with SSA mode" : "running without SSA mode");
            interpreter.InstructionLimit = 1 << 26;
            interpreter.Run();
            if (interpreter.Exception != null) {
                Console.WriteLine("Exception: " + interpreter.Exception.Message);
            } else {
                Console.WriteLine("ExitCode:  " + interpreter.ExitCode);
            }
        }
    }
}

namespace LowLevelIntermediateRepresentation {
    public interface IInstruction {
        int LineNo { get; }
        string LineText { get; }
    }

    internal interface IHasDestInstruction : IInstruction {
        string Dest { get; }
    }

    internal class PhiNode : IInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public Dictionary<string, string> Paths { get; }

        public PhiNode(int lineNo, string lineText, string dest) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Paths = new Dictionary<string, string>();
        }
    }

    internal class Store : IInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Op1 { get; }
        public string Op2 { get; }
        public int Size { get; }
        public int Offset { get; }

        public Store(int lineNo, string lineText, string op1, string op2, int size, int offset) {
            LineNo = lineNo;
            LineText = lineText;
            Op1 = op1;
            Op2 = op2;
            Size = size;
            Offset = offset;
        }
    }

    internal class Load : IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Op1 { get; }
        public int Size { get; }
        public int Offset { get; }

        public Load(int lineNo, string lineText, string dest, string op1, int size, int offset) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Op1 = op1;
            Size = size;
            Offset = offset;
        }
    }

    internal class Alloc : IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Op1 { get; }

        public Alloc(int lineNo, string lineText, string dest, string op1) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Op1 = op1;
        }
    }

    internal class Call : IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Op1 { get; }
        public List<string> Args { get; }

        public Call(int lineNo, string lineText, string dest, string op1, List<string> args) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Op1 = op1;
            Args = args;
        }
    }

    internal class Br : IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }
        public string Dest { get; }
        public string Op1 { get; }
        public string Op2 { get; }

        public Br(int lineNo, string lineText, string dest, string op1, string op2) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Op1 = op1;
            Op2 = op2;
        }
    }

    internal class Ret : IInstruction {
        public int LineNo { get; }
        public string LineText { get; }
        public string Op1 { get; }
        public Ret(int lineNo, string lineText, string op1) {
            LineNo = lineNo;
            LineText = lineText;
            Op1 = op1;
        }
    }

    internal class Jump : IInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Op1 { get; }
        public Jump(int lineNo, string lineText, string op1) {
            LineNo = lineNo;
            LineText = lineText;
            Op1 = op1;
        }
    }

    internal class Move : IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Op1 { get; }
        public Move(int lineNo, string lineText, string dest, string op1) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Op1 = op1;
        }
    }

    internal class BinOp : IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Op { get; }
        public string Op1 { get; }
        public string Op2 { get; }

        public BinOp(int lineNo, string lineText, string dest, string op, string op1, string op2) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Op = op;
            Op1 = op1;
            Op2 = op2;
        }
    }

    internal class UnaryOp : IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Op { get; }
        public string Op1 { get; }

        public UnaryOp(int lineNo, string lineText, string dest, string op, string op1) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Op = op;
            Op1 = op1;
        }
    }

    internal class CondOp : IHasDestInstruction {
        public int LineNo { get; }
        public string LineText { get; }

        public string Dest { get; }
        public string Op { get; }
        public string Op1 { get; }
        public string Op2 { get; }

        public CondOp(int lineNo, string lineText, string dest, string op, string op1, string op2) {
            LineNo = lineNo;
            LineText = lineText;
            Dest = dest;
            Op = op;
            Op1 = op1;
            Op2 = op2;
        }
    }

    internal class BasicBlock {
        public string Name { get; }
        public List<IInstruction> Instructions { get; }
        public List<PhiNode> Phi { get; }

        public BasicBlock(string name) {
            Name = name;
            Instructions = new List<IInstruction>();
            Phi = new List<PhiNode>();
        }
    }

    internal class Function {
        public bool HasReturnValue { get; }
        public string Name { get; }
        public BasicBlock Entry { get; set; }
        public List<string> Args { get; }
        public Dictionary<string, BasicBlock> Blocks { get; }

        public Function(bool hasReturnValue, string name, List<string> args) {
            HasReturnValue = hasReturnValue;
            Name = name;
            Args = args;
            Blocks = new Dictionary<string, BasicBlock>();
        }
    }

    internal class Register {
        public int Value { get; set; }
        public int Timestamp { get; set; }
    }

    internal class Parser {
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

        private void ReadInstruction() {
            // basic block
            if (LineText.StartsWith("%")) {
                if (!LineText.EndsWith(":")) {
                    throw new SemanticError(LineNo, LineText, "expected a `:`");
                }

                CurrentBasicBlock = new BasicBlock(LineText.Substring(0, LineText.Length - 1));
                if (CurrentFunction.Blocks.ContainsKey(CurrentBasicBlock.Name)) {
                    throw new SemanticError(LineNo, LineText, "label `" + CurrentBasicBlock.Name + "` has already been defined");
                }

                CurrentFunction.Blocks.Add(CurrentBasicBlock.Name, CurrentBasicBlock);
                if (CurrentFunction.Entry == null) {
                    CurrentFunction.Entry = CurrentBasicBlock;
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
                            throw new SemanticError(LineNo, LineText, "illegal operator " + words[0]);
                        }

                        var inst = new Store(
                            lineNo: LineNo,
                            lineText: LineText,
                            op1: words[2],
                            op2: words[3],
                            size: int.Parse(words[1]),
                            offset: int.Parse(words[4])
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "load": {
                        if (split.Length != 2) {
                            throw new SemanticError(LineNo, LineText, "illegal operator " + words[0]);
                        }

                        var inst = new Load(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim(),
                            op1: words[2],
                            size: int.Parse(words[1]),
                            offset: int.Parse(words[3])
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "alloc": {
                        if (split.Length != 2) {
                            throw new SemanticError(LineNo, LineText, "illegal operator " + words[0]);
                        }

                        var inst = new Alloc(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim(),
                            op1: words[1]
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
                                op1: words[1],
                                args: words.GetRange(2, words.Count - 2)
                            );
                            //if (split.Length == 2) {inst.Dest = split[0].Trim();}
                            //inst.Op1 = words[1];
                            //inst.Args = words.GetRange(2, words.Count - 2);
                            AllowPhi = false;
                            CurrentBasicBlock.Instructions.Add(inst);
                            return;
                        } else if (split.Length == 2) {
                            var inst = new Call(
                                lineNo: LineNo,
                                lineText: LineText,
                                dest: split[0].Trim(),
                                op1: words[1],
                                args: words.GetRange(2, words.Count - 2)
                            );
                            //if (split.Length == 2) {inst.Dest = split[0].Trim();}
                            //inst.Op1 = words[1];
                            //inst.Args = words.GetRange(2, words.Count - 2);
                            AllowPhi = false;
                            CurrentBasicBlock.Instructions.Add(inst);
                            return;
                        } else {
                            throw new SemanticError(LineNo, LineText, "illegal operator " + words[0]);
                        }
                    }
                case "br": {
                        if (split.Length != 1) {
                            throw new SemanticError(LineNo, LineText, "illegal operator " + words[0]);
                        }

                        var inst = new Br(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: words[1],
                            op1: words[2],
                            op2: words[3]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "phi": {
                        if (split.Length != 2) {
                            throw new SemanticError(LineNo, LineText, "illegal operator " + words[0]);
                        }

                        if (!AllowPhi) {
                            throw new SemanticError(LineNo, LineText, "`phi` is not allowed here");
                        }

                        if ((words.Count & 1) == 0) {
                            throw new SemanticError(LineNo, LineText, "the number of `phi` argument should be even");
                        }

                        var phi = new PhiNode(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim()
                        );

                        for (var i = 1; i < words.Count; i += 2) {
                            var label = words[i + 0];
                            var reg = words[i + 1];
                            if (!label.StartsWith("%")) {
                                throw new SemanticError(LineNo, LineText, "label should starts with `%`");
                            }

                            if (!reg.StartsWith("$") && reg != "undef") {
                                throw new SemanticError(LineNo, LineText, "source of a phi node should be a register or `undef`");
                            }

                            phi.Paths.Add(label, reg);
                        }

                        CurrentBasicBlock.Phi.Add(phi);
                        return;
                    }
                case "jump": {
                        if (split.Length != 1) {
                            throw new SemanticError(LineNo, LineText, "illegal operator " + words[0]);
                        }

                        var inst = new Jump(
                            lineNo: LineNo,
                            lineText: LineText,
                            op1: words[1]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "ret": {
                        if (split.Length != 1) {
                            throw new SemanticError(LineNo, LineText, "illegal operator " + words[0]);
                        }

                        var inst = new Ret(
                            lineNo: LineNo,
                            lineText: LineText,
                            op1: words[1]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }

                case "move": {
                        if (split.Length != 2) {
                            throw new SemanticError(LineNo, LineText, "illegal operator " + words[0]);
                        }

                        var inst = new Move(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim(),
                            op1: words[1]
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
                            throw new SemanticError(LineNo, LineText, "illegal operator " + words[0]);
                        }

                        var inst = new BinOp(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim(),
                            op: words[0],
                            op1: words[1],
                            op2: words[2]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                case "neg":
                case "not": {
                        if (split.Length != 2) {
                            throw new SemanticError(LineNo, LineText, "illegal operator " + words[0]);
                        }

                        var inst = new UnaryOp(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim(),
                            op: words[0],
                            op1: words[1]
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
                            throw new SemanticError(LineNo, LineText, "illegal operator " + words[0]);
                        }

                        var inst = new CondOp(
                            lineNo: LineNo,
                            lineText: LineText,
                            dest: split[0].Trim(),
                            op: words[0],
                            op1: words[1],
                            op2: words[2]
                        );
                        AllowPhi = false;
                        CurrentBasicBlock.Instructions.Add(inst);
                        return;
                    }
                default: {
                        throw new SemanticError(LineNo, LineText, "illegal operator " + words[0]);
                    }
            }
        }

        private void ReadFunction() {
            var words = SplitBySpaces(LineText);
            if (words[words.Count - 1] != "{") {
                throw new SemanticError(LineNo, LineText, "expected a `{`");
            }

            CurrentFunction = new Function(
                hasReturnValue: LineText.StartsWith("func "),
                name: words[1],
                args: words.GetRange(2, words.Count - 1 - 2)
            );
            if (Functions.ContainsKey(CurrentFunction.Name)) {
                throw new SemanticError(LineNo, LineText, "function `" + CurrentFunction.Name + "` has already been defined");
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
                if (parser.LineText.StartsWith("func ") || parser.LineText.StartsWith("void ")) {
                    parser.ReadFunction();
                }
            }

            var program = new Program(parser.Functions);
            if (ssaMode) {
                program.CheckValid();
            }

            return program;
        }
    }

    public class SemanticError : Exception {
        public SemanticError(int lineNo, string line, string reason) : base(reason + " | line " + lineNo + ": " + line) {
        }
    }

    public class RuntimeError : Exception {
        public RuntimeError(IInstruction curInst, string reason) : base(curInst != null ? reason + " | line " + curInst.LineNo + ": " + curInst.LineText : reason) {
        }
    }

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
                    foreach (var inst in basicBlock.Instructions) {
                        var hasDestInst = inst as IHasDestInstruction;
                        if (hasDestInst != null && !(hasDestInst is Br)) {
                            if (regDef.Contains(hasDestInst.Dest)) {
                                var lineNo = hasDestInst.LineNo;
                                var lineText = hasDestInst.LineText;
                                throw new SemanticError(lineNo, lineText, "a register should only be defined once");
                            } else {
                                regDef.Add(hasDestInst.Dest);
                            }
                        }
                    }
                }
            }
        }
    }

    public class Interpreter {
        private Random Randomize { get; }
        private Dictionary<string, Register> Registers { get; set; }
        private Dictionary<string, int> TempRegister { get; } // for phi node
        private Dictionary<int, byte> Memory { get; }

        private int HeapTop { get; set; }
        private int RetValue { get; set; }
        private bool Ret { get; set; }
        private int CountInstruction { get; set; }
        private BasicBlock LastBasicBlock { get; set; }

        private Program Program { get; }
        private BasicBlock CurrentBasicBlock { get; set; }
        private Function CurrentFunction { get; set; }
        private IInstruction CurrentInstruction { get; set; }
        public int ExitCode { get; private set; }
        public Exception Exception { get; private set; }
        public int InstructionLimit { get; set; }

        public bool IsReady { get; private set; }

        private byte MemoryRead(int address) {
            byte value;
            if (!Memory.TryGetValue(address, out value)) {
                throw new RuntimeError(CurrentInstruction, "memory read violation");
            }

            return value;
        }

        private void MemoryWrite(int address, byte value) {
            if (!Memory.ContainsKey(address)) {
                throw new RuntimeError(CurrentInstruction, "memory write violation");
            }

            Memory.Add(address, value);
        }

        private int RegisterRead(string name) {
            Register register;
            if (!Registers.TryGetValue(name, out register)) {
                throw new RuntimeError(CurrentInstruction, $"register `{name}` haven't been defined yet");
            }

            return register.Value;
        }

        private void RegisterWrite(string name, int value) {
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

        private int ReadSrc(string name) {
            if (name.StartsWith("$")) {
                return RegisterRead(name);
            } else {
                return int.Parse(name);
            }
        }

        private void Jump(string name) {
            BasicBlock basicBlock;
            if (!CurrentFunction.Blocks.TryGetValue(name, out basicBlock)) {
                throw new RuntimeError(CurrentInstruction, "cannot resolve block `" + name + "` in function `" + CurrentFunction.Name + "`");
            }

            LastBasicBlock = CurrentBasicBlock;
            CurrentBasicBlock = basicBlock;
        }

        private void RunInstruction() {
            if (++CountInstruction >= InstructionLimit) {
                throw new RuntimeError(CurrentInstruction, "instruction limit exceeded");
            }

            if (CurrentInstruction is Load) {
                var inst = CurrentInstruction as Load;
                var address = ReadSrc(inst.Op1) + inst.Offset;
                var res = 0;
                for (var i = 0; i < inst.Size; ++i) {
                    res = (res << 8) | MemoryRead(address + i);
                }
                RegisterWrite(inst.Dest, res);
                return;
            } else if (CurrentInstruction is Store) {
                var inst = CurrentInstruction as Store;
                var address = ReadSrc(inst.Op1) + inst.Offset;
                var data = ReadSrc(inst.Op2);
                for (var i = inst.Size - 1; i >= 0; --i) {
                    MemoryWrite(address + i, (byte)(data & 0xFF));
                    data >>= 8;
                }

                return;
            } else if (CurrentInstruction is Alloc) {
                var inst = CurrentInstruction as Alloc;

                var size = ReadSrc(inst.Op1);
                RegisterWrite(inst.Dest, HeapTop);
                for (var i = 0; i < size; ++i) {
                    Memory.Add(HeapTop + i, (byte)Randomize.Next(256));
                }

                HeapTop += Randomize.Next(4096);
                return;
            } else if (CurrentInstruction is Ret) {
                var inst = CurrentInstruction as Ret;
                RetValue = ReadSrc(inst.Op1);
                Ret = true;
                return;
            } else if (CurrentInstruction is Br) {
                var inst = CurrentInstruction as Br;
                var cond = ReadSrc(inst.Dest);
                Jump(cond == 0 ? inst.Op2 : inst.Op1);
                return;
            } else if (CurrentInstruction is Jump) {
                var inst = CurrentInstruction as Jump;
                Jump(inst.Op1);
                return;
            } else if (CurrentInstruction is Call) {
                var inst = CurrentInstruction as Call;
                Function function;
                if (!Program.Functions.TryGetValue(inst.Op1, out function)) {
                    throw new RuntimeError(inst, "cannot resolve function `" + inst.Op1 + "`");
                }

                if (inst.Dest != null && !function.HasReturnValue) {
                    throw new RuntimeError(inst, "function `" + function.Name + "` has not return value");
                }

                Dictionary<string, Register> registers = new Dictionary<string, Register>();
                if (inst.Args.Count != function.Args.Count) {
                    throw new RuntimeError(inst, "argument size cannot match");
                }

                for (var i = 0; i < inst.Args.Count; ++i) {
                    var name = function.Args[i];
                    Register register;
                    if (!registers.TryGetValue(name, out register)) {
                        register = new Register();
                        registers.Add(name, register);
                    }

                    register.Value = ReadSrc(inst.Args[i]);
                    register.Timestamp = CountInstruction;
                }

                var backRegisters = Registers;
                var backCurrentBasicBlock = CurrentBasicBlock;
                var backLastBasicBlock = LastBasicBlock;
                var backCurrentInst = CurrentInstruction;
                var backCurrentFunc = CurrentFunction;
                Registers = registers;

                RunFunction(function);

                Ret = false;
                CurrentFunction = backCurrentFunc;
                CurrentInstruction = backCurrentInst;
                LastBasicBlock = backLastBasicBlock;
                CurrentBasicBlock = backCurrentBasicBlock;
                Registers = backRegisters;
                if (inst.Dest != null) {
                    RegisterWrite(inst.Dest, RetValue);
                }

                return;
            } else if (CurrentInstruction is BinOp) {
                var inst = CurrentInstruction as BinOp;
                switch (inst.Op) {
                    case "add": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) + ReadSrc(inst.Op2));
                            return;
                        }
                    case "sub": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) - ReadSrc(inst.Op2));
                            return;
                        }
                    case "mul": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) * ReadSrc(inst.Op2));
                            return;
                        }
                    case "div": {
                            if (ReadSrc(inst.Op2) == 0) {
                                throw new RuntimeError(inst, "divide by zero");
                            }
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) / ReadSrc(inst.Op2));
                            return;
                        }
                    case "rem": {
                            if (ReadSrc(inst.Op2) == 0) {
                                throw new RuntimeError(inst, "mod by zero");
                            }
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) % ReadSrc(inst.Op2));
                            return;
                        }
                    case "shl": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) << ReadSrc(inst.Op2));
                            return;
                        }
                    case "shr": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) >> ReadSrc(inst.Op2));
                            return;
                        }
                    case "and": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) & ReadSrc(inst.Op2));
                            return;
                        }
                    case "or": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) | ReadSrc(inst.Op2));
                            return;
                        }
                    case "xor": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) ^ ReadSrc(inst.Op2));
                            return;
                        }
                    default: {
                            throw new RuntimeError(inst, "unknown binop `" + inst.Op + "`");
                        }
                }
            } else if (CurrentInstruction is Move) {
                var inst = CurrentInstruction as Move;

                RegisterWrite(inst.Dest, ReadSrc(inst.Op1));
                return;
            } else if (CurrentInstruction is UnaryOp) {
                var inst = CurrentInstruction as UnaryOp;
                switch (inst.Op) {

                    case "neg": {
                            RegisterWrite(inst.Dest, -ReadSrc(inst.Op1));
                            return;
                        }
                    case "not": {
                            RegisterWrite(inst.Dest, ~ReadSrc(inst.Op1));
                            return;
                        }
                    default: {
                            throw new RuntimeError(inst, "unknown unaryop `" + inst.Op + "`");
                        }
                }
                return;
            } else if (CurrentInstruction is CondOp) {
                var inst = CurrentInstruction as CondOp;
                switch (inst.Op) {

                    case "slt": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) < ReadSrc(inst.Op2) ? 1 : 0);
                            return;
                        }
                    case "sgt": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) > ReadSrc(inst.Op2) ? 1 : 0);
                            return;
                        }
                    case "sle": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) <= ReadSrc(inst.Op2) ? 1 : 0);
                            return;
                        }
                    case "sge": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) >= ReadSrc(inst.Op2) ? 1 : 0);
                            return;
                        }
                    case "seq": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) == ReadSrc(inst.Op2) ? 1 : 0);
                            return;
                        }
                    case "sne": {
                            RegisterWrite(inst.Dest, ReadSrc(inst.Op1) != ReadSrc(inst.Op2) ? 1 : 0);
                            return;
                        }
                    default: {
                            throw new RuntimeError(inst, "unknown condop `" + inst.Op + "`");
                        }

                }
                return;
            } else {
                throw new RuntimeError(CurrentInstruction, "unknown operation `" + CurrentInstruction.GetType().Name + "`");
            }
        }

        private void RunFunction(Function function) {
            CurrentFunction = function;
            CurrentBasicBlock = function.Entry;
            if (CurrentBasicBlock == null) {
                throw new RuntimeError(CurrentInstruction, "no entry block for function `" + function.Name + "`");
            }

            for (;;) {
                var basicBlock = CurrentBasicBlock;
                if (!(
                    (basicBlock.Instructions[basicBlock.Instructions.Count - 1] is Br) ||
                    (basicBlock.Instructions[basicBlock.Instructions.Count - 1] is Jump) ||
                    (basicBlock.Instructions[basicBlock.Instructions.Count - 1] is Ret))) {
                    throw new RuntimeError(CurrentInstruction, "block " + basicBlock.Name + " has no end instruction");
                }


                // run phi nodes concurrently
                if (CurrentBasicBlock.Phi.Any()) {
                    CountInstruction += 1;
                    TempRegister.Clear();
                    foreach (var phi in CurrentBasicBlock.Phi) {
                        CurrentInstruction = phi;
                        string registerName;
                        if (!phi.Paths.TryGetValue(LastBasicBlock.Name, out registerName)) {
                            throw new RuntimeError(CurrentInstruction, "this phi node has no value from incoming block `" + LastBasicBlock.Name + "`");
                        } else {
                            var value = registerName == "undef" ? Randomize.Next(int.MaxValue) : ReadSrc(registerName);
                            TempRegister.Add(phi.Dest, value);
                        }
                    }

                    foreach (var e in TempRegister) {
                        RegisterWrite(e.Key, e.Value);
                    }
                }

                foreach (var inst in basicBlock.Instructions) {
                    CurrentInstruction = inst;
                    RunInstruction();
                    if (Ret) {
                        return;
                    }

                    if (CurrentBasicBlock != basicBlock) {
                        break; // jumped
                    }
                }
            }
        }

        public Interpreter(Program program) {
            Program = program;
            Randomize = new Random();
            Registers = null;
            TempRegister = new Dictionary<string, int>(); // for phi node
            Memory = new Dictionary<int, byte>();

            HeapTop = Randomize.Next(4096);
            RetValue = 0;
            Ret = false;
            CountInstruction = 0;
            LastBasicBlock = null;

            CurrentBasicBlock = null;
            CurrentFunction = null;
            CurrentInstruction = null;
            ExitCode = -1;
            Exception = null;
            InstructionLimit = int.MaxValue;

            IsReady = true;
        }

        public void Run() {
            try {
                if (!IsReady) {
                    throw new Exception("not ready");
                }

                Function mainFunction;
                if (!Program.Functions.TryGetValue("main", out mainFunction)) {
                    throw new RuntimeError(CurrentInstruction, "cannot find `main` function");
                }

                Registers = new Dictionary<string, Register>();
                RunFunction(mainFunction);
                ExitCode = RetValue;
                Exception = null;
            } catch (RuntimeError e) {
                ExitCode = -1;
                Exception = e;
            }

            IsReady = false;
        }
    }
}