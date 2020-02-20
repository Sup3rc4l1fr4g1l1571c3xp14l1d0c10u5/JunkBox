using System;
using System.Collections.Generic;
using System.IO;

namespace X86Asm.parser {
    using operand;

    /// <summary>
    /// �A�Z���u������̃p�[�T
    /// </summary>
    public sealed class Parser {
        /// <summary>
        /// �t�@�C������ǂݎ��A��͂��s��
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
        /// �t�@�C���̍\�����
        /// </summary>
        /// <returns></returns>
        private Program ParseFile() {
            Program program = new Program();
            //EOF�ɏo��܂ōs�̉�͂𑱂���
            while (!Tokenizer.Check(TokenType.END_OF_FILE)) {
                ParseLine(program);
            }
            return program;
        }

        /// <summary>
        /// �s�̍\�����
        /// </summary>
        /// <param name="program"></param>
        private void ParseLine(Program program) {
            // ���x�����������ǂݎ��
            while (Tokenizer.Check(TokenType.LABEL)) {
                string name = Tokenizer.Next().text;
                name = name.Substring(0, name.Length - 1);
                program.addStatement(new LabelStatement(name));
            }

            // ���߂�����Γǂݎ��
            if (Tokenizer.Check(TokenType.NAME)) {
                ParseInstruction(program);
            }

            // ���s��ǂݎ��
            if (Tokenizer.Check(TokenType.NEWLINE)) {
                Tokenizer.Next();
            } else {
                throw new Exception("���s������܂���B");
            }
        }

        /// <summary>
        /// ���߂̍\�����
        /// </summary>
        /// <param name="program"></param>
        private void ParseInstruction(Program program) {
            // �j�[���j�b�N���擾
            string mnemonic = Tokenizer.Next().text;

            // �I�y�����h���擾
            IList<Operand> operands = new List<Operand>();
            bool expectComma = false;
            while (!Tokenizer.Check(TokenType.NEWLINE)) {
                // �I�y�����h��؂�̃R���}���`�F�b�N
                if (!expectComma) {
                    if (Tokenizer.Check(TokenType.COMMA)) {
                        throw new Exception("�I�y�����h������ׂ��ꏊ�ɃR���}������܂����B");
                    }
                } else {
                    if (!Tokenizer.Check(TokenType.COMMA)) {
                        throw new Exception("�R���}������܂���B");
                    }
                    Tokenizer.Next();
                }

                if (Tokenizer.Check(TokenType.REGISTER)) {
                    // �g�[�N�������W�X�^���̏ꍇ�̓��W�X�^��ǂݎ��
                    operands.Add(ParseRegister(Tokenizer.Next().text));
                } else if (Tokenizer.Check(TokenType.DOLLAR)) {
                    // �g�[�N���� '$' �̏ꍇ�͑������l����͂���
                    Tokenizer.Next();
                    operands.Add(ParseImmediate());
                } else if (CanParseImmediate() || Tokenizer.Check(TokenType.LEFT_PAREN)) {
                    // �g�[�N���ɑ��l�v�f���o�����Ă���ꍇ�̓f�B�X�v���C�����g�A�h���X�Ƃ��đ��l��ǂݎ��B
                    // �g�[�N���ɊJ���ۊ��ʂ��o�����Ă���ꍇ�́A�f�B�X�v���C�����g�A�h���X�͂O�Ƃ���             
                    Immediate display = CanParseImmediate() ? ParseImmediate() : ImmediateValue.ZERO;
                    // �f�B�X�v���C�����g�ɑ���������������͂���
                    operands.Add(ParseMemory(display));
                } else {
                    throw new Exception("�s���ȃI�y�����h�ł��B");
                }
                // ��ł��v�f��ǂݍ��񂾂�R���}�̏o�������߂�
                expectComma = true;
            }

            // �j�[���j�b�N�ƃI�y�����h���疽�ߕ�������ăv���O�����ɒǉ�
            program.addStatement(new InstructionStatement(mnemonic, operands));
        }


        /// <summary>
        /// ���̃g�[�N�������l�v�f�i�\�i���A�\�Z�i���A���x�����j�����ׂ�B
        /// </summary>
        /// <returns></returns>
        private bool CanParseImmediate() {
            return Tokenizer.Check(TokenType.DECIMAL) || Tokenizer.Check(TokenType.HEXADECIMAL) || Tokenizer.Check(TokenType.NAME);
        }


        /// <summary>
        /// ���l����͂���
        /// </summary>
        /// <returns></returns>
        private Immediate ParseImmediate() {
            if (Tokenizer.Check(TokenType.DECIMAL)) {
                // �\�i��
                return new ImmediateValue(Convert.ToInt32(Tokenizer.Next().text));
            } else if (Tokenizer.Check(TokenType.HEXADECIMAL)) {
                // �\�Z�i��
                string text = Tokenizer.Next().text;
                text = text.Substring(2, text.Length - 2);
                return new ImmediateValue((int)Convert.ToInt64(text, 16));
            } else if (Tokenizer.Check(TokenType.NAME)) {
                // ���x����
                return new Label(Tokenizer.Next().text);
            } else {
                throw new Exception("���l�v�f������ׂ��ł��B");
            }
        }

        /// <summary>
        /// ������������͂���
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
                    throw new Exception("���ۊ��ʂ��Ȃ�");
                }
            }

            return new Memory(@base, index, scale, displacement);
        }

        /// <summary>
        /// ���W�X�^���ƃ��W�X�^�I�u�W�F�N�g�̑Ή��\
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
        /// ���W�X�^�����烌�W�X�^�I�u�W�F�N�g�𓾂�B
        /// �啶���������͋�ʂ���Ȃ��B
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Register ParseRegister(string name) {
            Register register;
            if (!RegisterTable.TryGetValue(name.ToLower(), out register)) {
                throw new ArgumentException("�s���ȃ��W�X�^���ł�");
            }

            return register;
        }

    }

}