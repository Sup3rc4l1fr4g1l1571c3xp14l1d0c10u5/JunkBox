using System;
using System.Collections.Generic;
using System.IO;

namespace X86Asm.parser
{


	using InstructionStatement = X86Asm.InstructionStatement;
	using LabelStatement = X86Asm.LabelStatement;
	using Program = X86Asm.Program;
	using Immediate = X86Asm.operand.Immediate;
	using ImmediateValue = X86Asm.operand.ImmediateValue;
	using Label = X86Asm.operand.Label;
	using Memory = X86Asm.operand.Memory;
	using Operand = X86Asm.operand.Operand;
	using Register = X86Asm.operand.Register;
	using Register16 = X86Asm.operand.Register16;
	using Register32 = X86Asm.operand.Register32;
	using Register8 = X86Asm.operand.Register8;
	using SegmentRegister = X86Asm.operand.SegmentRegister;


	public sealed class Parser
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static X86Asm.Program parseFile(java.io.File file) throws java.io.IOException
		public static Program parseFile(Stream file)
		{
			if (file == null)
			{
				throw new ArgumentNullException(nameof(file));
			}
			BufferedTokenizer tokenizer = new BufferedTokenizer(new StringTokenizer(file));
			return (new Parser(tokenizer)).parseFile();
		}



		private BufferedTokenizer tokenizer;



		private Parser(BufferedTokenizer tokenizer)
		{
			if (tokenizer == null)
			{
				throw new ArgumentNullException();
			}
			this.tokenizer = tokenizer;
		}



		private Program parseFile()
		{
			Program program = new Program();
			while (!tokenizer.check(TokenType.END_OF_FILE))
			{
				parseLine(program);
			}
			return program;
		}


		private void parseLine(Program program)
		{
			// Parse label declarations
			while (tokenizer.check(TokenType.LABEL))
			{
				string name = tokenizer.next().text;
				name = name.Substring(0, name.Length - 1);
				program.addStatement(new LabelStatement(name));
			}

			// Parse instruction
			if (tokenizer.check(TokenType.NAME))
			{
				parseInstruction(program);
			}

			if (tokenizer.check(TokenType.NEWLINE))
			{
				tokenizer.next();
			}
			else
			{
				throw new Exception("Expected newline");
			}
		}


		private void parseInstruction(Program program)
		{
			// Parse mnemonic (easy)
			string mnemonic = tokenizer.next().text;

			// Parse operands (hard)
			IList<Operand> operands = new List<Operand>();
			bool expectcomma = false;
			while (!tokenizer.check(TokenType.NEWLINE))
			{
				if (!expectcomma)
				{
					if (tokenizer.check(TokenType.COMMA))
					{
						throw new Exception("Expected operand, got comma");
					}
				}
				else
				{
					if (!tokenizer.check(TokenType.COMMA))
					{
						throw new Exception("Expected comma");
					}
					tokenizer.next();
				}

				if (tokenizer.check(TokenType.REGISTER))
				{
					operands.Add(parseRegister(tokenizer.next().text));
				}
				else if (tokenizer.check(TokenType.DOLLAR))
				{
					tokenizer.next();
					operands.Add(parseImmediate());
				}
				else if (canParseImmediate() || tokenizer.check(TokenType.LEFT_PAREN))
				{
					Immediate disp;
					if (canParseImmediate())
					{
						disp = parseImmediate();
					}
					else
					{
						disp = ImmediateValue.ZERO;
					}
					operands.Add(parseMemory(disp));
				}
				else
				{
					throw new Exception("Expected operand");
				}
				expectcomma = true;
			}

			program.addStatement(new InstructionStatement(mnemonic, operands));
		}


		/// <summary>
		/// Tests whether the next token is a decimal number, hexadecimal number, or a label. </summary>
		/// <param name="tokenizer"> the tokenizer to test from </param>
		/// <returns> {@code true} if the next token is an immediate operand, {@code false} otherwise </returns>
		private bool canParseImmediate()
		{
			return tokenizer.check(TokenType.DECIMAL) || tokenizer.check(TokenType.HEXADECIMAL) || tokenizer.check(TokenType.NAME);
		}


		private Immediate parseImmediate()
		{
			if (tokenizer.check(TokenType.DECIMAL))
			{
				return new ImmediateValue(Convert.ToInt32(tokenizer.next().text));
			}
			else if (tokenizer.check(TokenType.HEXADECIMAL))
			{
				string text = tokenizer.next().text;
				text = text.Substring(2, text.Length - 2);
				return new ImmediateValue((int)Convert.ToInt64(text, 16));
			}
			else if (tokenizer.check(TokenType.NAME))
			{
				return new Label(tokenizer.next().text);
			}
			else
			{
				throw new Exception("Expected immediate");
			}
		}


		private Memory parseMemory(Immediate displacement)
		{
			Register32 @base = null;
			Register32 index = null;
			int scale = 1;

			if (tokenizer.check(TokenType.LEFT_PAREN))
			{
				tokenizer.next();

				if (tokenizer.check(TokenType.REGISTER))
				{
					@base = (Register32)parseRegister(tokenizer.next().text);
				}

				if (tokenizer.check(TokenType.COMMA))
				{
					tokenizer.next();

					if (tokenizer.check(TokenType.REGISTER))
					{
						index = (Register32)parseRegister(tokenizer.next().text);
					}

					if (tokenizer.check(TokenType.COMMA))
					{
						tokenizer.next();

						if (tokenizer.check(TokenType.DECIMAL))
						{
							scale = Convert.ToInt32(tokenizer.next().text);
						}
					}

				}

				if (tokenizer.check(TokenType.RIGHT_PAREN))
				{
					tokenizer.next();
				}
				else
				{
					throw new Exception("Expected right parenthesis");
				}
			}

			return new Memory(@base, index, scale, displacement);
		}


		private static IDictionary<string, Register> REGISTER_TABLE;

		static Parser()
		{
			REGISTER_TABLE = new Dictionary<string, Register>();
			REGISTER_TABLE["%eax"] = Register32.EAX;
			REGISTER_TABLE["%ebx"] = Register32.EBX;
			REGISTER_TABLE["%ecx"] = Register32.ECX;
			REGISTER_TABLE["%edx"] = Register32.EDX;
			REGISTER_TABLE["%esp"] = Register32.ESP;
			REGISTER_TABLE["%ebp"] = Register32.EBP;
			REGISTER_TABLE["%esi"] = Register32.ESI;
			REGISTER_TABLE["%edi"] = Register32.EDI;
			REGISTER_TABLE["%ax"] = Register16.AX;
			REGISTER_TABLE["%bx"] = Register16.BX;
			REGISTER_TABLE["%cx"] = Register16.CX;
			REGISTER_TABLE["%dx"] = Register16.DX;
			REGISTER_TABLE["%sp"] = Register16.SP;
			REGISTER_TABLE["%bp"] = Register16.BP;
			REGISTER_TABLE["%si"] = Register16.SI;
			REGISTER_TABLE["%di"] = Register16.DI;
			REGISTER_TABLE["%al"] = Register8.AL;
			REGISTER_TABLE["%bl"] = Register8.BL;
			REGISTER_TABLE["%cl"] = Register8.CL;
			REGISTER_TABLE["%dl"] = Register8.DL;
			REGISTER_TABLE["%ah"] = Register8.AH;
			REGISTER_TABLE["%bh"] = Register8.BH;
			REGISTER_TABLE["%ch"] = Register8.CH;
			REGISTER_TABLE["%dh"] = Register8.DH;
			REGISTER_TABLE["%cs"] = SegmentRegister.CS;
			REGISTER_TABLE["%ds"] = SegmentRegister.DS;
			REGISTER_TABLE["%es"] = SegmentRegister.ES;
			REGISTER_TABLE["%fs"] = SegmentRegister.FS;
			REGISTER_TABLE["%gs"] = SegmentRegister.GS;
			REGISTER_TABLE["%ss"] = SegmentRegister.SS;
		}


		/// <summary>
		/// Returns the register associated with the specified name. Examples of register names include {@code "%eax"}, {@code "%sp"}, and {@code "%cs"}. The name is case-insensitive. </summary>
		/// <param name="name"> the name of the register </param>
		/// <returns> the register associated with the name </returns>
		/// <exception cref="IllegalArgumentException"> if no register is associated with the name </exception>
		private static Register parseRegister(string name)
		{
			name = name.ToLower();
			if (!REGISTER_TABLE.ContainsKey(name))
			{
				throw new System.ArgumentException("Invalid register name");
			}
			return REGISTER_TABLE[name];
		}

	}

}