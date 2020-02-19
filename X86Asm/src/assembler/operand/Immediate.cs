using System.Collections.Generic;

namespace X86Asm.operand
{



	/// <summary>
	/// An immediate operand.
	/// </summary>
	public abstract class Immediate : Operand
	{

		/// <summary>
		/// Constructs an immediate operand.
		/// </summary>
		public Immediate()
		{
		}


		/// <summary>
		/// Returns the value of this immediate operand, given the specified label offset mapping. </summary>
		/// <param name="labelOffsets"> the mapping of label names to offsets </param>
		/// <returns> the value of this immediate operand </returns>
		public abstract ImmediateValue getValue(IDictionary<string, int> labelOffsets);

	}

}