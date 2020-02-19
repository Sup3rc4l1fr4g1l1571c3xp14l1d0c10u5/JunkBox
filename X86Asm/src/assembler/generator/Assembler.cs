using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace X86Asm.generator
{


	using InstructionStatement = X86Asm.InstructionStatement;
	using LabelStatement = X86Asm.LabelStatement;
	using Program = X86Asm.Program;
	using Statement = X86Asm.Statement;
	using Operand = X86Asm.operand.Operand;
	using ElfFile = X86Asm.libelf.ElfFile;
	using ElfHeader = X86Asm.libelf.ElfHeader;
	using ObjectType = X86Asm.libelf.ObjectType;
	using ProgramHeader = X86Asm.libelf.ProgramHeader;
	using SegmentType = X86Asm.libelf.SegmentType;


	public sealed class Assembler
	{

		private static InstructionPatternTable patterntable = InstructionPatternTable.MODE32_TABLE;

		private const int ENTRY_POINT = 0x00100054;



		public static void assembleToFile(Program program, Stream outputfile)
		{
			if (program == null || outputfile == null)
			{
				throw new ArgumentNullException();
			}

			IDictionary<string, int> labelOffsets = computeLabelOffsets(program);
			assembleProgram(program, labelOffsets, outputfile);
		}


		private static IDictionary<string, int> computeLabelOffsets(Program program)
		{
			IDictionary<string, int> result = new Dictionary<string, int>();
			int offset = ENTRY_POINT;
			foreach (Statement st in program.Statements)
			{
				if (st is InstructionStatement)
				{
					InstructionStatement ist = (InstructionStatement)st;
					string mnemonic = ist.Mnemonic;
					IList<Operand> operands = ist.Operands.ToList();
					int length = CodeGenerator.getMachineCodeLength(patterntable, mnemonic, operands);
					offset += length;
				}
				else if (st is LabelStatement)
				{
					string name = ((LabelStatement)st).Name;
					result[name] = offset;
				}
			}
			return result;
		}

		private static void assembleProgram(Program program, IDictionary<string, int> labelOffsets, Stream outputfile)
		{
			byte[] code = assembleToBytes(program, labelOffsets);

			ElfFile elf = new ElfFile();

			ElfHeader eh = new ElfHeader();
			eh.type = ObjectType.ET_EXEC;
			eh.entry = ENTRY_POINT;
			eh.shstrndx = 0;
			elf.elfHeader = eh;

			ProgramHeader ph = new ProgramHeader();
			ph.type = SegmentType.PT_LOAD;
			ph.vaddr = ENTRY_POINT;
			ph.filesz = code.Length;
			ph.memsz = code.Length;
			ph.flags = 0x7;
			ph.align = 0x1000;
			elf.programHeaders.Add(ph);
			ph.offset = elf.DataOffset;

			elf.data = code;

			try {
                var bytes = elf.toBytes();
                outputfile.Write(bytes,0,bytes.Length);
			}
			finally
			{
                outputfile.Close();
			}
		}


		private static byte[] assembleToBytes(Program program, IDictionary<string, int> labelOffsets)
		{
			try
			{
				var @out = new System.IO.MemoryStream();
				int offset = ENTRY_POINT;
				foreach (Statement st in program.Statements)
				{
					if (st is InstructionStatement)
					{
						InstructionStatement ist = (InstructionStatement)st;
						string mnemonic = ist.Mnemonic;
						IList<Operand> operands = ist.Operands.ToList();
						byte[] machinecode = CodeGenerator.makeMachineCode(patterntable, mnemonic, operands, program, labelOffsets, offset);
						@out.Write(machinecode,0,machinecode.Length);
						offset += machinecode.Length;
					}
					else if (st is LabelStatement)
					{
						string name = ((LabelStatement)st).Name;
						if (offset != labelOffsets[name])
						{
							throw new InvalidOperationException("Label offset mismatch");
						}
					}
				}
				return @out.ToArray();
			}
			catch (IOException e) {
                throw e;
            }
		}



		/// <summary>
		/// Not instantiable.
		/// </summary>
		private Assembler()
		{
		}

	}

}