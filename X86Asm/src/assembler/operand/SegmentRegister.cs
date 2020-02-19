namespace X86Asm.operand
{


	/// <summary>
	/// A segment register operand. Immutable.
	/// </summary>
	public sealed class SegmentRegister : Register
	{

		/// <summary>
		/// The ES register. </summary>
		public static SegmentRegister ES = new SegmentRegister("es", 0);

		/// <summary>
		/// The CS register. </summary>
		public static SegmentRegister CS = new SegmentRegister("cs", 1);

		/// <summary>
		/// The SS register. </summary>
		public static SegmentRegister SS = new SegmentRegister("ss", 2);

		/// <summary>
		/// The DS register. </summary>
		public static SegmentRegister DS = new SegmentRegister("ds", 3);

		/// <summary>
		/// The FS register. </summary>
		public static SegmentRegister FS = new SegmentRegister("fs", 4);

		/// <summary>
		/// The GS register. </summary>
		public static SegmentRegister GS = new SegmentRegister("gs", 5);



		/// <summary>
		/// Constructs a segment register with the specified name and register number. </summary>
		/// <param name="name"> the name of the register </param>
		/// <param name="registerNumber"> the register number </param>
		private SegmentRegister(string name, uint registerNumber) : base(name, registerNumber)
		{
		}

	}

}