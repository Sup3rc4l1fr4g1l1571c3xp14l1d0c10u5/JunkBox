namespace X86Asm.operand
{


	/// <summary>
	/// An 8-bit register operand. Immutable.
	/// </summary>
	public sealed class Register8 : Register
	{

		/// <summary>
		/// The AL register. </summary>
		public static Register8 AL = new Register8("al", 0);

		/// <summary>
		/// The CL register. </summary>
		public static Register8 CL = new Register8("cl", 1);

		/// <summary>
		/// The DL register. </summary>
		public static Register8 DL = new Register8("dl", 2);

		/// <summary>
		/// The BL register. </summary>
		public static Register8 BL = new Register8("bl", 3);

		/// <summary>
		/// The AH register. </summary>
		public static Register8 AH = new Register8("ah", 4);

		/// <summary>
		/// The CH register. </summary>
		public static Register8 CH = new Register8("ch", 5);

		/// <summary>
		/// The DH register. </summary>
		public static Register8 DH = new Register8("dh", 6);

		/// <summary>
		/// The BH register. </summary>
		public static Register8 BH = new Register8("bh", 7);



		/// <summary>
		/// Constructs an 8-bit register with the specified name and register number. </summary>
		/// <param name="name"> the name of the register </param>
		/// <param name="registerNumber"> the register number </param>
		private Register8(string name, uint registerNumber) : base(name, registerNumber)
		{
		}

	}

}