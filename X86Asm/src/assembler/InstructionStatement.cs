using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace X86Asm
{


	using Operand = X86Asm.operand.Operand;


	/// <summary>
	/// An instruction statement. It consists of a mnenomic and a list of operands. Immutable.
	/// </summary>
	public class InstructionStatement : Statement
	{

		/// <summary>
		/// The instruction's mnemonic.
		/// </summary>
		private string mnemonic;

		/// <summary>
		/// The list of operands.
		/// </summary>
		private IReadOnlyList<Operand> operands;



		/// <summary>
		/// Constructs an instruction statement with the specified mnemonic and list of operands. </summary>
		/// <param name="mnemonic"> the mnemonic </param>
		/// <param name="operands"> the list of operands </param>
		/// <exception cref="ArgumentNullException"> if the mnemonic or list of operands or any operand is {@code null} </exception>
		public InstructionStatement(string mnemonic, IList<Operand> operands)
		{
			if (mnemonic == null || operands == null)
			{
				throw new ArgumentNullException();
			}
			foreach (Operand op in operands)
			{
				if (op == null)
				{
					throw new ArgumentNullException();
				}
			}

			this.mnemonic = mnemonic;
			this.operands = operands.ToList();
		}



		/// <summary>
		/// Returns the mnemonic of this instruction statement. </summary>
		/// <returns> the mnemonic of this instruction statement </returns>
		public virtual string Mnemonic
		{
			get
			{
				return mnemonic;
			}
		}


		/// <summary>
		/// Returns the list of operands of this instruction statement. The contents of the list cannot change or be changed. </summary>
		/// <returns> the list of operands of this instruction statement </returns>
		public virtual IReadOnlyList<Operand> Operands
		{
			get
			{
				return operands;
			}
		}


		/// <summary>
		/// Compares this instruction statement to the specified object for equality. Returns {@code true} if the specified object is an instruction statement with the same mnemonic and operands. Otherwise returns {@code false}. </summary>
		/// <param name="obj"> the object to compare this instruction statement against </param>
		/// <returns> {@code true} if the object is an instruction statement with the same mnemonic and operands, {@code false} otherwise </returns>
		public override bool Equals(object obj)
		{
			if (!(obj is LabelStatement))
			{
				return false;
			}
			else
			{
				InstructionStatement ist = (InstructionStatement)obj;
				return mnemonic.Equals(ist.mnemonic) && operands.Equals(ist.operands);
			}
		}


		/// <summary>
		/// Returns the hash code for this instruction statement. </summary>
		/// <returns> the hash code for this instruction statement </returns>
		public override int GetHashCode()
		{
			return mnemonic.GetHashCode() + operands.GetHashCode();
		}


		/// <summary>
		/// Returns a string representation of this instruction statement. The format is subjected to change. </summary>
		/// <returns> a string representation of this instruction statement </returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(mnemonic);

			if (operands.Count > 0)
			{
				sb.Append("  ");

				bool initial = true;
				foreach (Operand op in operands)
				{
					if (initial)
					{
						initial = false;
					}
					else
					{
						sb.Append(", ");
					}
					sb.Append(op);
				}
			}

			return sb.ToString();
		}

	}

}