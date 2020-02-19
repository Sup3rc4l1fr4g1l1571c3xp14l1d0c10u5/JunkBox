using System;
using System.Collections.Generic;

namespace X86Asm.operand
{



	/// <summary>
	/// A label operand. A label is simply a string name. Immutable.
	/// </summary>
	public class Label : Immediate
	{

		/// <summary>
		/// The name of the label. </summary>
		private string name;



		/// <summary>
		/// Constructs a label with the specified name. </summary>
		/// <param name="name"> the name of the label </param>
		public Label(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException();
			}
			this.name = name;
		}



		/// <summary>
		/// Returns the name of this label.
		/// </summary>
		public virtual string Name
		{
			get
			{
				return name;
			}
		}


		/// <summary>
		/// Returns the value of this label operand, given the specified label offset mapping. This method returns an <seealso cref="ImmediateValue"/> of {@code labelOffset.get(this.name)}. </summary>
		/// <param name="labelOffsets"> the mapping of label names to offset </param>
		/// <returns> the offset of this label </returns>
		public override ImmediateValue getValue(IDictionary<string, int> labelOffsets)
		{
			return new ImmediateValue(labelOffsets[name]);
		}


		/// <summary>
		/// Compares this label to the specified object for equality. Returns {@code true} if the specified object is a label with the same name. Otherwise returns {@code false}. </summary>
		/// <param name="obj"> the object to compare this label against </param>
		/// <returns> {@code true} if the object is a label with the same name, {@code false} otherwise </returns>
		public override bool Equals(object obj)
		{
			if (!(obj is Label))
			{
				return false;
			}
			else
			{
				return name.Equals(((Label)obj).name);
			}
		}


		/// <summary>
		/// Returns the hash code for this label. </summary>
		/// <returns> the hash code for this label </returns>
		public override int GetHashCode()
		{
			return name.GetHashCode();
		}


		/// <summary>
		/// Returns a string representation of this label operand. The format is subjected to change. </summary>
		/// <returns> a string representation of this label operand </returns>
		public override string ToString()
		{
			return name;
		}

	}

}