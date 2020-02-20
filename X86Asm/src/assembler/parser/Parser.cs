using System;
using System.Collections.Generic;
using System.IO;

namespace X86Asm.parser {
    using operand;

    /// <summary>
    /// アセンブリ言語のパーサ
    /// </summary>
    public sealed class Parser {
        /// <summary>
        /// ファイルから読み取り、解析を行う
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Program ParseFile(Stream file) {
            if (file == null) {
                throw new ArgumentNullException(nameof(file));
            }
            BufferedTokenizer tokenizer = new BufferedTokenizer(new StringTokenizer(file));
            return (new Parser(tokenizer)).ParseFile();
        }

        private BufferedTokenizer Tokenizer { get; }

        private Parser(BufferedTokenizer tokenizer) {
            if (tokenizer == null) {
                throw new ArgumentNullException();
            }
            Tokenizer = tokenizer;
        }

        /// <summary>
        /// ファイルの構文解析
        /// </summary>
        /// <returns></returns>
        private Program ParseFile() {
            Program program = new Program();
            //EOFに出会うまで行の解析を続ける
            while (!Tokenizer.Check(TokenType.END_OF_FILE)) {
                ParseLine(program);
            }
            return program;
        }

        /// <summary>
        /// 行の構文解析
        /// </summary>
        /// <param name="program"></param>
        private void ParseLine(Program program) {
            // ラベルがある限り読み取る
            while (Tokenizer.Check(TokenType.LABEL)) {
                string name = Tokenizer.Next().text;
                name = name.Substring(0, name.Length - 1);
                program.addStatement(new LabelStatement(name));
            }

            // 命令があれば読み取る
            if (Tokenizer.Check(TokenType.NAME)) {
                ParseInstruction(program);
            }

            // 改行を読み取る
            if (Tokenizer.Check(TokenType.NEWLINE)) {
                Tokenizer.Next();
            } else {
                throw new Exception("改行がありません。");
            }
        }

        /// <summary>
        /// 命令の構文解析
        /// </summary>
        /// <param name="program"></param>
        private void ParseInstruction(Program program) {
            // ニーモニックを取得
            string mnemonic = Tokenizer.Next().text;

            // オペランドを取得
            IList<Operand> operands = new List<Operand>();
            bool expectComma = false;
            while (!Tokenizer.Check(TokenType.NEWLINE)) {
                // オペランド区切りのコンマをチェック
                if (!expectComma) {
                    if (Tokenizer.Check(TokenType.COMMA)) {
                        throw new Exception("オペランドがあるべき場所にコンマがありました。");
                    }
                } else {
                    if (!Tokenizer.Check(TokenType.COMMA)) {
                        throw new Exception("コンマがありません。");
                    }
                    Tokenizer.Next();
                }

                if (Tokenizer.Check(TokenType.REGISTER)) {
                    // トークンがレジスタ名の場合はレジスタを読み取る
                    operands.Add(ParseRegister(Tokenizer.Next().text));
                } else if (Tokenizer.Check(TokenType.DOLLAR)) {
                    // トークンが '$' の場合は続く即値を解析する
                    Tokenizer.Next();
                    operands.Add(ParseImmediate());
                } else if (CanParseImmediate() || Tokenizer.Check(TokenType.LEFT_PAREN)) {
                    // トークンに即値要素が出現している場合はディスプレイメントアドレスとして即値を読み取る。
                    // トークンに開き丸括弧が出現している場合は、ディスプレイメントアドレスは０とする             
                    Immediate display = CanParseImmediate() ? ParseImmediate() : ImmediateValue.ZERO;
                    // ディスプレイメントに続くメモリ式を解析する
                    operands.Add(ParseMemory(display));
                } else {
                    throw new Exception("不明なオペランドです。");
                }
                // 一つでも要素を読み込んだらコンマの出現を求める
                expectComma = true;
            }

            // ニーモニックとオペランドから命令文を作ってプログラムに追加
            program.addStatement(new InstructionStatement(mnemonic, operands));
        }


        /// <summary>
        /// 次のトークンが即値要素（十進数、十六進数、ラベル名）か調べる。
        /// </summary>
        /// <returns></returns>
        private bool CanParseImmediate() {
            return Tokenizer.Check(TokenType.DECIMAL) || Tokenizer.Check(TokenType.HEXADECIMAL) || Tokenizer.Check(TokenType.NAME);
        }


        /// <summary>
        /// 即値を解析する
        /// </summary>
        /// <returns></returns>
        private Immediate ParseImmediate() {
            if (Tokenizer.Check(TokenType.DECIMAL)) {
                // 十進数
                return new ImmediateValue(Convert.ToInt32(Tokenizer.Next().text));
            } else if (Tokenizer.Check(TokenType.HEXADECIMAL)) {
                // 十六進数
                string text = Tokenizer.Next().text;
                text = text.Substring(2, text.Length - 2);
                return new ImmediateValue((int)Convert.ToInt64(text, 16));
            } else if (Tokenizer.Check(TokenType.NAME)) {
                // ラベル名
                return new Label(Tokenizer.Next().text);
            } else {
                throw new Exception("即値要素があるべきです。");
            }
        }

        /// <summary>
        /// メモリ式を解析する
        /// </summary>
        /// <param name="displacement"></param>
        /// <returns></returns>
        private Memory ParseMemory(Immediate displacement) {
            Register32 @base = null;
            Register32 index = null;
            int scale = 1;

            if (Tokenizer.Check(TokenType.LEFT_PAREN)) {
                Tokenizer.Next();

                if (Tokenizer.Check(TokenType.REGISTER)) {
                    @base = (Register32)ParseRegister(Tokenizer.Next().text);
                }

                if (Tokenizer.Check(TokenType.COMMA)) {
                    Tokenizer.Next();

                    if (Tokenizer.Check(TokenType.REGISTER)) {
                        index = (Register32)ParseRegister(Tokenizer.Next().text);
                    }

                    if (Tokenizer.Check(TokenType.COMMA)) {
                        Tokenizer.Next();

                        if (Tokenizer.Check(TokenType.DECIMAL)) {
                            scale = Convert.ToInt32(Tokenizer.Next().text);
                        }
                    }

                }

                if (Tokenizer.Check(TokenType.RIGHT_PAREN)) {
                    Tokenizer.Next();
                } else {
                    throw new Exception("閉じ丸括弧がない");
                }
            }

            return new Memory(@base, index, scale, displacement);
        }

        /// <summary>
        /// レジスタ名とレジスタオブジェクトの対応表
        /// </summary>
        private static readonly IDictionary<string, Register> RegisterTable = new Dictionary<string, Register>() {
            { "%eax", Register32.EAX},
            { "%ebx", Register32.EBX},
            { "%ecx", Register32.ECX},
            { "%edx", Register32.EDX},
            { "%esp", Register32.ESP},
            { "%ebp", Register32.EBP},
            { "%esi", Register32.ESI},
            { "%edi", Register32.EDI},
            { "%ax", Register16.AX},
            { "%bx", Register16.BX},
            { "%cx", Register16.CX},
            { "%dx", Register16.DX},
            { "%sp", Register16.SP},
            { "%bp", Register16.BP},
            { "%si", Register16.SI},
            { "%di", Register16.DI},
            { "%al", Register8.AL},
            { "%bl", Register8.BL},
            { "%cl", Register8.CL},
            { "%dl", Register8.DL},
            { "%ah", Register8.AH},
            { "%bh", Register8.BH},
            { "%ch", Register8.CH},
            { "%dh", Register8.DH},
            { "%cs", SegmentRegister.CS},
            { "%ds", SegmentRegister.DS},
            { "%es", SegmentRegister.ES},
            { "%fs", SegmentRegister.FS},
            { "%gs", SegmentRegister.GS},
            { "%ss", SegmentRegister.SS},
        };

        /// <summary>
        /// レジスタ名からレジスタオブジェクトを得る。
        /// 大文字小文字は区別されない。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Register ParseRegister(string name) {
            Register register;
            if (!RegisterTable.TryGetValue(name.ToLower(), out register)) {
                throw new ArgumentException("不正なレジスタ名です");
            }

            return register;
        }

    }

}