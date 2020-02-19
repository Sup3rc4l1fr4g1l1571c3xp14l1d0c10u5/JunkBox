using System;

namespace X86Asm
{


	/// <summary>
	/// A label statement. It contains the name of the label. Immutable.
	/// </summary>
	public class LabelStatement : Statement
	{

		/// <summary>
		/// The name of the label.
		/// </summary>
		private string name;



		/// <summary>
		/// Constructs a label statement with the specified name. </summary>
		/// <param name="name"> the name of the label </param>
		/// <exception cref="ArgumentNullException"> if the name is {@code null} </exception>
		public LabelStatement(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException();
			}
			this.name = name;
		}



		/// <summary>
		/// Returns the name of the label. </summary>
		/// <returns> the name of the label </returns>
		public virtual string Name
		{
			get
			{
				return name;
			}
		}


		/// <summary>
		/// Compares this label statement to the specified object for equality. Returns {@code true} if the specified object is a label statement with the same name. Otherwise returns {@code false}. </summary>
		/// <param name="obj"> the object to compare this label statement against </param>
		/// <returns> {@code true} if the object is a label statement with the same name, {@code false} otherwise </returns>
		public override bool Equals(object obj)
		{
			if (!(obj is LabelStatement))
			{
				return false;
			}
			else
			{
				return name.Equals(((LabelStatement)obj).name);
			}
		}


		/// <summary>
		/// Returns the hash code for this label statement. </summary>
		/// <returns> the hash code for this label statement </returns>
		public override int GetHashCode()
		{
			return name.GetHashCode();
		}


		/// <summary>
		/// Returns a string representation of this label statement. The format is subjected to change. </summary>
		/// <returns> a string representation of this label statement </returns>
		public override string ToString()
		{
			return name;
		}

	}

}