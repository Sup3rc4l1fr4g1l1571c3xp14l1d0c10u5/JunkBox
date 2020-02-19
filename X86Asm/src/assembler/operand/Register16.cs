namespace X86Asm.operand
{


	/// <summary>
	/// A 16-bit register operand. Immutable.
	/// </summary>
	public sealed class Register16 : Register
	{

		/// <summary>
		/// The AX register. </summary>
		public static Register16 AX = new Register16("ax", 0);

		/// <summary>
		/// The CX register. </summary>
		public static Register16 CX = new Register16("cx", 1);

		/// <summary>
		/// The DX register. </summary>
		public static Register16 DX = new Register16("dx", 2);

		/// <summary>
		/// The BX register. </summary>
		public static Register16 BX = new Register16("bx", 3);

		/// <summary>
		/// The SP register. </summary>
		public static Register16 SP = new Register16("sp", 4);

		/// <summary>
		/// The BP register. </summary>
		public static Register16 BP = new Register16("bp", 5);

		/// <summary>
		/// The SI register. </summary>
		public static Register16 SI = new Register16("si", 6);

		/// <summary>
		/// The DI register. </summary>
		public static Register16 DI = new Register16("di", 7);



		/// <summary>
		/// Constructs a 16-bit register with the specified name and register number. </summary>
		/// <param name="name"> the name of the register </param>
		/// <param name="registerNumber"> the register number </param>
		private Register16(string name, uint registerNumber) : base(name, registerNumber)
		{
		}

	}

}