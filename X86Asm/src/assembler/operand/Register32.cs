namespace X86Asm.operand
{


	/// <summary>
	/// A 32-bit register operand. Immutable.
	/// </summary>
	public sealed class Register32 : Register
	{

		/// <summary>
		/// The EAX register. </summary>
		public static Register32 EAX = new Register32("eax", 0);

		/// <summary>
		/// The ECX register. </summary>
		public static Register32 ECX = new Register32("ecx", 1);

		/// <summary>
		/// The EDX register. </summary>
		public static Register32 EDX = new Register32("edx", 2);

		/// <summary>
		/// The EBX register. </summary>
		public static Register32 EBX = new Register32("ebx", 3);

		/// <summary>
		/// The ESP register. </summary>
		public static Register32 ESP = new Register32("esp", 4);

		/// <summary>
		/// The EBP register. </summary>
		public static Register32 EBP = new Register32("ebp", 5);

		/// <summary>
		/// The ESI register. </summary>
		public static Register32 ESI = new Register32("esi", 6);

		/// <summary>
		/// The EDI register. </summary>
		public static Register32 EDI = new Register32("edi", 7);



		/// <summary>
		/// Constructs a 32-bit register with the specified name and register number. </summary>
		/// <param name="name"> the name of the register </param>
		/// <param name="registerNumber"> the register number </param>
		private Register32(string name, uint registerNumber) : base(name, registerNumber)
		{
		}

	}

}