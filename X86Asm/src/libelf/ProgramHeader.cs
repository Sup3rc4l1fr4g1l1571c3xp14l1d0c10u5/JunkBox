using X86Asm.util;

namespace X86Asm.libelf
{



	public sealed class ProgramHeader
	{

		internal static int PROGRAM_HEADER_ENTRY_SIZE = 32;


		public SegmentType type;

		public int offset;

		public int vaddr;

		public readonly int paddr = 0; // Ignored on x86

		public int filesz;

		public int memsz;

		public int flags;

		public int align;



		internal byte[] toBytes()
		{
			ByteBuffer b = new ByteBuffer(PROGRAM_HEADER_ENTRY_SIZE);
			b.appendLittleEndian((ushort)type);
			b.appendLittleEndian(offset);
			b.appendLittleEndian(vaddr);
			b.appendLittleEndian(paddr);
			b.appendLittleEndian(filesz);
			b.appendLittleEndian(memsz);
			b.appendLittleEndian(flags);
			b.appendLittleEndian(align);
			return b.toArray();
		}

	}

}