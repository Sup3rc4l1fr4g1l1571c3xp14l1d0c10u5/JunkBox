using System;

namespace X86Asm.generator
{

	using Operand = X86Asm.operand.Operand;


	/// <summary>
	/// An operand pattern that matches a specific operand value.
	/// </summary>
	public sealed class LiteralOperandPattern : OperandPattern
	{

		/// <summary>
		/// The operand value to match. </summary>
		private Operand literal;



		public LiteralOperandPattern(Operand literal) : base(literal.ToString())
		{
			this.literal = literal;
		}



		public override bool matches(Operand operand)
		{
			if (operand == null)
			{
				throw new ArgumentNullException();
			}
			return operand.Equals(literal);
		}

	}

}