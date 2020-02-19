using X86Asm.util;

namespace X86Asm.libelf
{



	public sealed class ElfHeader
	{

		private const int ELFCLASS32 = 1;

		private const int ELFDATA2LSB = 1;

		private const int EV_CURRENT = 1;

		private const int EM_386 = 3;

		internal const int ELF_HEADER_SIZE = 52;


		private static byte[] ident = {0x7F, (byte)'E', (byte)'L', (byte)'F', ELFCLASS32, ELFDATA2LSB, EV_CURRENT, 0, 0, 0, 0, 0, 0, 0, 0, 0};

		public ObjectType type;

		public readonly short machine = EM_386;

		public readonly int version = EV_CURRENT;

		public int entry;

		public readonly int flags = 0; // Unused on x86

		public readonly short ehsize = ELF_HEADER_SIZE;

		public short shstrndx;



		internal byte[] getBytes(short phnum, short shnum)
		{
			int phoff = ELF_HEADER_SIZE;
			int shoff = phoff + phnum * ProgramHeader.PROGRAM_HEADER_ENTRY_SIZE;

			ByteBuffer b = new ByteBuffer(ELF_HEADER_SIZE);
			b.append(ident);
			b.appendLittleEndian((ushort)type);
			b.appendLittleEndian(machine);
			b.appendLittleEndian(version);
			b.appendLittleEndian(entry);
			if (phnum != 0)
			{
				b.appendLittleEndian(phoff);
			}
			else
			{
				b.appendLittleEndian(0);
			}
			if (shnum != 0)
			{
				b.appendLittleEndian(shoff);
			}
			else
			{
				b.appendLittleEndian(0);
			}
			b.appendLittleEndian(flags);
			b.appendLittleEndian((short)ELF_HEADER_SIZE);
			b.appendLittleEndian((short)ProgramHeader.PROGRAM_HEADER_ENTRY_SIZE);
			b.appendLittleEndian(phnum);
			b.appendLittleEndian((short)SectionHeader.SECTION_HEADER_ENTRY_SIZE);
			b.appendLittleEndian(shnum);
			b.appendLittleEndian(shstrndx);
			return b.toArray();
		}

	}

}