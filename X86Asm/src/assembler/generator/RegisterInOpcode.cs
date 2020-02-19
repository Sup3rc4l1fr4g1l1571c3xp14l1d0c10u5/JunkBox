namespace X86Asm.generator
{



	/// <summary>
	/// An option specifing that a register operand is encoded into the last byte of the instruction's opcode.
	/// </summary>
	public sealed class RegisterInOpcode : InstructionOption
	{

		/// <summary>
		/// The index of the operand to encode into the instruction's opcode.
		/// </summary>
		public readonly int operandIndex;



		/// <summary>
		/// Constructs a register-in-opcode option with the specified operand index. </summary>
		/// <param name="operandIndex"> the operand index </param>
		/// <exception cref="IllegalArgumentException"> if the operand index is negative </exception>
		public RegisterInOpcode(int operandIndex)
		{
			if (operandIndex < 0)
			{
				throw new System.ArgumentException("Invalid operand index");
			}
			this.operandIndex = operandIndex;
		}

	}

}