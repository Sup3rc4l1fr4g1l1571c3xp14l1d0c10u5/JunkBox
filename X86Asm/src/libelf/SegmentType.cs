namespace X86Asm.libelf
{


	public enum SegmentType
	{

		PT_NULL = 0,
		PT_LOAD = 1,
		PT_DYNAMIC = 2,
		PT_INTERP = 3,
		PT_NOTE = 4,
		PT_SHLIB = 5,
		PT_PHDR = 6


//JAVA TO C# CONVERTER TODO TASK: Enums cannot contain fields in .NET:
//		public final int value;


//JAVA TO C# CONVERTER TODO TASK: Enums cannot contain methods in .NET:
//		private SegmentType(int value)
	//	{
	//		this.value = value;
	//	}

	}

}