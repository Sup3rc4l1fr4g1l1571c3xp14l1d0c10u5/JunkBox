using System.Collections.Generic;
using X86Asm.util;

namespace X86Asm.libelf
{


	public sealed class ElfFile
	{

		public ElfHeader elfHeader;

		public IList<ProgramHeader> programHeaders;

		public IList<SectionHeader> sectionHeaders;

		public byte[] data;



		public ElfFile()
		{
			programHeaders = new List<ProgramHeader>();
			sectionHeaders = new List<SectionHeader>();
		}



		public int DataOffset
		{
			get
			{
				int result = 0;
				result += ElfHeader.ELF_HEADER_SIZE;
				result += programHeaders.Count * ProgramHeader.PROGRAM_HEADER_ENTRY_SIZE;
				result += sectionHeaders.Count * SectionHeader.SECTION_HEADER_ENTRY_SIZE;
				return result;
			}
		}


		public byte[] toBytes()
		{
			ByteBuffer b = new ByteBuffer(DataOffset + data.Length);
			b.append(elfHeader.getBytes((short)programHeaders.Count, (short)sectionHeaders.Count));
			foreach (ProgramHeader ph in programHeaders)
			{
				b.append(ph.toBytes());
			}
			foreach (SectionHeader sh in sectionHeaders)
			{
				b.append(sh.toBytes());
			}
			b.append(data);
			return b.toArray();
		}

	}

}