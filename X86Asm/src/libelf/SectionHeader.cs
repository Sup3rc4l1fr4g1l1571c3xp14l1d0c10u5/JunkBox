using X86Asm.util;

namespace X86Asm.libelf
{

	public sealed class SectionHeader
	{

	    public const int SECTION_HEADER_ENTRY_SIZE = 40;


		public int name;

		public SectionType type;

		public int flags;

		public int addr;

		public int offset;

		public int size;

		public int link;

		public int info;

		public int addralign;

		public int entsize;



		internal byte[] toBytes()
		{
			ByteBuffer b = new ByteBuffer(SECTION_HEADER_ENTRY_SIZE);
			b.appendLittleEndian(name);
			b.appendLittleEndian((ushort)type);
			b.appendLittleEndian(flags);
			b.appendLittleEndian(addr);
			b.appendLittleEndian(offset);
			b.appendLittleEndian(size);
			b.appendLittleEndian(link);
			b.appendLittleEndian(info);
			b.appendLittleEndian(addralign);
			b.appendLittleEndian(entsize);
			return b.toArray();
		}

	}

}