using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace X86Asm.generator
{


	using Program = X86Asm.Program;
	using Immediate = X86Asm.operand.Immediate;
	using ImmediateValue = X86Asm.operand.ImmediateValue;
	using Memory = X86Asm.operand.Memory;
	using Operand = X86Asm.operand.Operand;
	using Register = X86Asm.operand.Register;
	using Register32 = X86Asm.operand.Register32;


	public sealed class CodeGenerator
	{

		public static int getMachineCodeLength(InstructionPatternTable table, string mnemonic, IList<Operand> operands)
		{
			InstructionPattern pat = table.match(mnemonic, operands);
			int length = 0;

			if (pat.operandSizeMode == OperandSizeMode.MODE16)
			{
				length++;
			}

			length += pat.opcodes.Length;

			if (pat.options.Count == 1 && pat.options[0] is ModRM)
			{
				length += getModRMBytesLength((ModRM)pat.options[0], operands);
			}

			for (int i = 0; i < pat.operands.Count; i++)
			{
				OperandPattern slot = pat.operands[i];
				if (slot == OperandPattern.IMM8 || slot == OperandPattern.IMM8S || slot == OperandPattern.REL8)
				{
					length += 1;
				}
				else if (slot == OperandPattern.IMM16 || slot == OperandPattern.REL16)
				{
					length += 2;
				}
				else if (slot == OperandPattern.IMM32 || slot == OperandPattern.REL32)
				{
					length += 4;
				}
			}

			return length;
		}


		private static int getModRMBytesLength(ModRM option, IList<Operand> operands)
		{
			Operand rm = operands[option.rmOperandIndex];

			if (rm is Register)
			{
				return 1;

			}
			else if (rm is Memory)
			{
				Memory m = (Memory)rm;
				Immediate disp = m.displacement;

				if (m.@base == null && m.index == null) // disp32
				{
					return 5;
				}
				else if (m.@base != Register32.ESP && m.@base != Register32.EBP && m.index == null && disp is ImmediateValue && ((ImmediateValue)disp).Zero) // eax, ecx, edx, ebx, esi, edi
				{
					return 1;
				}
				else if (m.@base != Register32.ESP && m.index == null && disp is ImmediateValue && ((ImmediateValue)disp).Signed8Bit) // (eax, ecx, edx, ebx, ebp, esi, edi) + disp8
				{
					return 2;
				}
				else if (m.@base != Register32.ESP && m.index == null) // (eax, ecx, edx, ebx, ebp, esi, edi) + disp32
				{
					return 5;
				}
				else // SIB
				{
					if (m.@base == null) // index*scale + disp32
					{
						return 6;
					}
					else if (m.@base != Register32.EBP && disp is ImmediateValue && ((ImmediateValue)disp).Zero) // (eax, ecx, edx, ebx, esp, esi, edi) + index*scale
					{
						return 2;
					}
					else if (disp is ImmediateValue && ((ImmediateValue)disp).Signed8Bit) // base + index*scale + disp8
					{
						return 3;
					}
					else // base + index*scale + disp32
					{
						return 6;
					}
				}

			}
			else
			{
				throw new Exception("Not a register or memory operand");
			}
		}


		public static byte[] makeMachineCode(InstructionPatternTable table, string mnemonic, IList<Operand> operands, Program program, IDictionary<string, int> labelOffsets, int offset)
		{
			// Get matching instruction pattern
			InstructionPattern pat = table.match(mnemonic, operands);

			// Initialize blank result
			byte[] result = new byte[0];

			// Append operand size override prefix if necessary
			if (pat.operandSizeMode == OperandSizeMode.MODE16)
			{
				result = concatenate(result, new byte[]{0x66});
			}

			// Process register-in-opcode option
			byte[] opcodes = pat.opcodes;
			if (pat.options.Count == 1 && pat.options[0] is RegisterInOpcode)
			{
				RegisterInOpcode option = (RegisterInOpcode)pat.options[0];
				opcodes = opcodes.ToArray();
				opcodes[opcodes.Length - 1] += (byte)((Register)operands[option.operandIndex]).RegisterNumber;
			}

			// Append opcode
			result = concatenate(result, opcodes);

			// Append ModR/M and SIB bytes if necessary
			if (pat.options.Count == 1 && pat.options[0] is ModRM)
			{
				result = concatenate(result, makeModRMBytes((ModRM)pat.options[0], operands, program, labelOffsets));
			}

			// Append immediate operands if necessary
			for (int i = 0; i < pat.operands.Count; i++)
			{
				OperandPattern slot = pat.operands[i];

				if (slot == OperandPattern.IMM8 || slot == OperandPattern.IMM8S || slot == OperandPattern.IMM16 || slot == OperandPattern.IMM32 || slot == OperandPattern.REL8 || slot == OperandPattern.REL16 || slot == OperandPattern.REL32)
				{

					ImmediateValue value = ((Immediate)operands[i]).getValue(labelOffsets);

					if (slot == OperandPattern.REL8 || slot == OperandPattern.REL16 || slot == OperandPattern.REL32)
					{
						value = new ImmediateValue(value.getValue(labelOffsets).Value - offset - getMachineCodeLength(table, mnemonic, operands));
					}

					// Encode signed or unsigned
					if (slot == OperandPattern.IMM8)
					{
						result = concatenate(result, value.to1Byte());
					}
					else if (slot == OperandPattern.IMM16)
					{
						result = concatenate(result, value.to2Bytes());
					}
					else if (slot == OperandPattern.IMM32 || slot == OperandPattern.REL32)
					{
						result = concatenate(result, value.to4Bytes());
					}

					// Encode signed
					else if (slot == OperandPattern.IMM8S || slot == OperandPattern.REL8)
					{
						if (!value.Signed8Bit)
						{
							throw new Exception("Not a signed 8-bit immediate operand");
						}
						result = concatenate(result, value.to1Byte());
					}
					else if (slot == OperandPattern.REL16)
					{
						if (!value.Signed16Bit)
						{
							throw new Exception("Not a signed 16-bit immediate operand");
						}
						result = concatenate(result, value.to2Bytes());
					}
					else
					{
						throw new Exception();
					}
				}
			}

			// Return machine code sequence
			return result;
		}


		private static byte[] makeModRMBytes(ModRM option, IList<Operand> operands, Program program, IDictionary<string, int> labelOffsets)
		{
			Operand rm = operands[option.rmOperandIndex];
			uint mod;
			uint rmvalue;
			byte[] rest;

			if (rm is Register)
			{
				mod = 3;
				rmvalue = ((Register)rm).RegisterNumber;
				rest = new byte[0];

			}
			else if (rm is Memory)
			{
				Memory m = (Memory)rm;
				ImmediateValue disp = m.displacement.getValue(labelOffsets);

				if (m.@base == null && m.index == null) // disp32
				{
					mod = 0;
					rmvalue = 5;
					rest = disp.to4Bytes();

				} // eax, ecx, edx, ebx, esi, edi
				else if (m.@base != Register32.ESP && m.@base != Register32.EBP && m.index == null && m.displacement is ImmediateValue && disp.Zero)
				{
					mod = 0;
					rmvalue = m.@base.RegisterNumber;
					rest = new byte[0];

				} // (eax, ecx, edx, ebx, ebp, esi, edi) + disp8
				else if (m.@base != Register32.ESP && m.index == null && m.displacement is ImmediateValue && disp.Signed8Bit)
				{
					mod = 1;
					rmvalue = m.@base.RegisterNumber;
					rest = disp.to1Byte();

				} // (eax, ecx, edx, ebx, ebp, esi, edi) + disp32
				else if (m.@base != Register32.ESP && m.index == null)
				{
					mod = 2;
					rmvalue = m.@base.RegisterNumber;
					rest = disp.to4Bytes();

				} // SIB
				else
				{
					rmvalue = 4;

					if (m.@base == null) // index*scale + disp32
					{
						mod = 0;
						rest = disp.to4Bytes();

					} // (eax, ecx, edx, ebx, esp, esi, edi) + index*scale
					else if (m.@base != Register32.EBP && m.displacement is ImmediateValue && disp.Zero)
					{
						mod = 0;
						rest = new byte[0];

					} // base + index*scale + disp8
					else if (m.displacement is ImmediateValue && disp.Signed8Bit)
					{
						mod = 1;
						rest = disp.to1Byte();

					} // base + index*scale + disp32
					else
					{
						mod = 2;
						rest = disp.to4Bytes();
					}

					byte[] sib = makeSIBByte(m);
					rest = concatenate(sib, rest);
				}

			}
			else
			{
				throw new Exception("Not a register or memory operand");
			}

			// Set reg/op value
			uint regopvalue;
			if (option.regOpcodeOperandIndex < 10)
			{
				Register regop = (Register)operands[option.regOpcodeOperandIndex];
				regopvalue = regop.RegisterNumber;
			}
			else
			{
				regopvalue = (uint)(option.regOpcodeOperandIndex - 10);
			}

			// Make ModR/M byte
			byte[] modrm = makeModRMByte(mod, regopvalue, rmvalue);

			// Concatenate and return
			return concatenate(modrm, rest);
		}


		private static byte[] makeModRMByte(uint mod, uint regop, uint rm)
		{
			if (mod < 0 || mod >= 4 || regop < 0 || regop >= 8 || rm < 0 || rm >= 8)
			{
				throw new Exception("Invalid ModR/M fields");
			}
			return new byte[]{(byte)(mod << 6 | regop << 3 | rm << 0)};
		}


		private static byte[] makeSIBByte(Memory mem)
		{
            uint scale = getScaleNumber(mem.scale);
            uint index = getIndexNumber(mem.index);
            uint @base = getBaseNumber(mem.@base);
			return new byte[]{(byte)(scale << 6 | index << 3 | @base << 0)};
		}


		private static uint getBaseNumber(Register32 @base)
		{
			if (@base != null)
			{
				return @base.RegisterNumber;
			}
			else
			{
				return 5;
			}
		}


		private static uint getIndexNumber(Register32 index)
		{
			if (index == Register32.ESP)
			{
				throw new System.ArgumentException("ESP register not allowed");
			}
			if (index != null)
			{
				return index.RegisterNumber;
			}
			else
			{
				return 4;
			}
		}


		private static uint getScaleNumber(int scale)
		{
			switch (scale)
			{
				case 1:
					return 0;
				case 2:
					return 1;
				case 4:
					return 2;
				case 8:
					return 3;
				default:
					throw new ArgumentOutOfRangeException(nameof(scale), "Invalid scale");
			}
		}


		private static byte[] concatenate(params byte[][] arrays)
		{
			int totalLength = 0;
			foreach (byte[] b in arrays)
			{
				totalLength += b.Length;
			}

			byte[] result = new byte[totalLength];
			int offset = 0;
			foreach (byte[] b in arrays)
			{
				Array.Copy(b, 0, result, offset, b.Length);
				offset += b.Length;
			}
			return result;
		}



		/// <summary>
		/// Not instantiable.
		/// </summary>
		private CodeGenerator()
		{
		}

	}

}