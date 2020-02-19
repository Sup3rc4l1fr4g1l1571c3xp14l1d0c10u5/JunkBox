using System;

namespace X86Asm.operand
{



	/// <summary>
	/// A register operand. Immutable. Each register has a number, which is typically used in the ModR/M byte or the SIB byte.
	/// </summary>
	public abstract class Register : Operand
	{

		/// <summary>
		/// The register number. </summary>
		private readonly uint registerNumber;

		/// <summary>
		/// The name, which is for display purposes only. </summary>
		private readonly string name;



		/// <summary>
		/// Constructs a register with the specified name and register number. </summary>
		/// <param name="name"> the name of the register </param>
		/// <param name="registerNumber"> the register number </param>
		/// <exception cref="ArgumentNullException"> if the name is {@code null} </exception>
		/// <exception cref="IllegalArgumentException"> if the register number is not in the range [0, 8) </exception>
		public Register(string name, uint registerNumber)
		{
			if (name == null)
			{
				throw new ArgumentNullException();
			}
			if (registerNumber < 0 || registerNumber >= 8)
			{
				throw new System.ArgumentException("Invalid register number");
			}
			this.name = name;
			this.registerNumber = registerNumber;
		}



		/// <summary>
		/// Returns the number of this register, which is an integer from 0 to 7 (inclusive). </summary>
		/// <returns> the number of this register </returns>
		public virtual uint RegisterNumber
		{
			get
			{
				return registerNumber;
			}
		}


		/// <summary>
		/// Returns a string representation of this register. The format is subjected to change. </summary>
		/// <returns> a string representation of this register </returns>
		public override string ToString()
		{
			return name;
		}

	}

}