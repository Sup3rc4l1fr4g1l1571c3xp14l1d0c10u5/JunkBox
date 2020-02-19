using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace X86Asm.generator
{


	/// <summary>
	/// An instruction pattern.
	/// </summary>
	public sealed class InstructionPattern
	{

		/// <summary>
		/// The regular expression pattern for mnemonics, which is one lowercase letter followed by zero or more lowercase or numeric characters.
		/// </summary>
		private static Regex MNEMONIC_PATTERN = new Regex("[a-z][a-z0-9]*");



		public readonly string mnemonic;

		public readonly IReadOnlyList<OperandPattern> operands;

		public readonly OperandSizeMode operandSizeMode;

		public readonly IReadOnlyList<InstructionOption> options;


		public readonly byte[] opcodes;



		/// <summary>
		/// Constructions an instruction pattern with the specified parameters. </summary>
		/// <param name="mnemonic"> the mnemonic </param>
		/// <param name="operands"> the operands </param>
		/// <param name="operandSizeMode"> the operand size mode </param>
		/// <param name="opcodes"> the opcodes </param>
		/// <param name="options"> the options </param>
		/// <exception cref="ArgumentNullException"> if any argument is {@code null} </exception>
		public InstructionPattern(string mnemonic, OperandPattern[] operands, OperandSizeMode operandSizeMode, int[] opcodes, params InstructionOption[] options)
		{
			if (mnemonic == null || operands == null /*|| operandSizeMode == null */|| opcodes == null || options == null)
			{
				throw new ArgumentNullException();
			}

			if (!MNEMONIC_PATTERN.IsMatch(mnemonic))
			{
				throw new System.ArgumentException("Invalid mnemonic");
			}

			if (operands.Length > 10)
			{
				throw new System.ArgumentException("Invalid operands");
			}

			if (options.Length > 1)
			{
				throw new System.ArgumentException("Invalid options");
			}
			if (options.Length == 1)
			{
				InstructionOption option = options[0];
				if (option is RegisterInOpcode)
				{
					checkOption((RegisterInOpcode)option, operands);
				}
				else if (option is ModRM)
				{
					checkOption((ModRM)option, operands);
				}
				else
				{
					throw new Exception("Unrecognized instruction option");
				}
			}

			this.mnemonic = mnemonic;
			this.operandSizeMode = operandSizeMode;
			this.opcodes = toBytes(opcodes);

			this.operands = operands.ToList();

			this.options = options.ToList();
		}



		/// <summary>
		/// Returns a string representation of this instruction pattern. The format is subjected to change. </summary>
		/// <returns> a string representation of this instruction pattern </returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(mnemonic);

			if (operands.Count > 0)
			{
				sb.Append("  ");
				bool initial = true;
				foreach (OperandPattern op in operands)
				{
					if (initial)
					{
						initial = false;
					}
					else
					{
						sb.Append(", ");
					}
					sb.Append(op);
				}
			}

			return sb.ToString();
		}



		private static void checkOption(RegisterInOpcode option, OperandPattern[] operands)
		{
			if (option.operandIndex >= operands.Length)
			{
				throw new System.IndexOutOfRangeException("Parameter index out of bounds");
			}
			if (!isRegisterPattern(operands[option.operandIndex]))
			{
				throw new System.ArgumentException("Option does not match operand");
			}
		}


		private static void checkOption(ModRM option, OperandPattern[] operands)
		{
			if (option.rmOperandIndex >= operands.Length)
			{
				throw new System.IndexOutOfRangeException("Parameter index out of bounds");
			}
			if (!isRegisterMemoryPattern(operands[option.rmOperandIndex]))
			{
				throw new System.ArgumentException("Option does not match operand");
			}

			if (option.regOpcodeOperandIndex >= 10 && option.regOpcodeOperandIndex < 18) // No problem
			{
				;
			}
			else if (option.regOpcodeOperandIndex >= 18)
			{
				throw new System.ArgumentException("Invalid register/opcode constant value");
			}
			else if (option.regOpcodeOperandIndex >= operands.Length)
			{
				throw new System.IndexOutOfRangeException("Parameter index out of bounds");
			}
			else if (!isRegisterPattern(operands[option.regOpcodeOperandIndex]))
			{
				throw new System.ArgumentException("Option does not match operand");
			}
		}


		private static bool isRegisterMemoryPattern(OperandPattern pat)
		{
			return pat == OperandPattern.RM8 || pat == OperandPattern.RM16 || pat == OperandPattern.RM32 || pat == OperandPattern.MEM;
		}


		private static bool isRegisterPattern(OperandPattern pat)
		{
			return pat == OperandPattern.REG8 || pat == OperandPattern.REG16 || pat == OperandPattern.REG32 || pat == OperandPattern.SREG;
		}


		/// <summary>
		/// Returns a new unsigned byte array containing the same sequence of values as the specified int32 array. Each integer value must be in the range [0x00, 0xFF]. </summary>
		/// <param name="opcodes"> </param>
		/// <returns> a new byte array containing the same sequence of values as the int32 array </returns>
		/// <exception cref="IllegalArgumentException"> if any value of the int32 array is outside of the range [0x00, 0xFF] </exception>
		private static byte[] toBytes(int[] opcodes)
		{
			byte[] result = new byte[opcodes.Length];
			for (int i = 0; i < opcodes.Length; i++)
			{
				if ((opcodes[i] & 0xFF) != opcodes[i])
				{
					throw new System.ArgumentException("Byte value out of range");
				}
				result[i] = (byte)opcodes[i];
			}
			return result;
		}

	}

}