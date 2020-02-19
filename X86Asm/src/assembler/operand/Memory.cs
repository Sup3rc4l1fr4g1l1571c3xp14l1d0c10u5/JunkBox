using System;
using System.Text;

namespace X86Asm.operand
{


	/// <summary>
	/// A 32-bit memory reference. A memory operand has a base register (possibly {@code null}), an index register (possibly {@code null}), an index scale, and a displacement. A memory operand represents the address <var>base</var> + <var>index</var>*<var>scale</var> + <var>displacement</var>. Immutable. </summary>
	/// <seealso cref= Register32 </seealso>
	public sealed class Memory : Operand
	{

		/// <summary>
		/// The base register, which may be {@code null}. </summary>
		public readonly Register32 @base;

		/// <summary>
		/// The index register, which is not <seealso cref="Register32"/>.ESP, and may be {@code null}. </summary>
		public readonly Register32 index;

		/// <summary>
		/// The index scale, which is either 1, 2, 4, or 8. </summary>
		public readonly int scale;

		/// <summary>
		/// The displacement, which is not {@code null}. </summary>
		public readonly Immediate displacement;



		/// <summary>
		/// Constructs a memory operand with the specified base, index, scale, and displacement. The base register may be {@code null}. The index register may be {@code null}, but must not be <seealso cref="Register32"/>.ESP. The scale must be either 1, 2, 4, or 8. The displacement must not be {@code null}. </summary>
		/// <param name="base"> the base register, possibly {@code null} </param>
		/// <param name="index"> the index register, possibly {@code null} </param>
		/// <param name="scale"> the index scale, either 1, 2, 4, or 8 </param>
		/// <param name="displacement"> the displacement, which is not {@code null} </param>
		public Memory(Register32 @base, Register32 index, int scale, Immediate displacement)
		{
			if (index == Register32.ESP)
			{
				throw new System.ArgumentException("Invalid index register");
			}
			if (scale != 1 && scale != 2 && scale != 4 && scale != 8)
			{
				throw new System.ArgumentException("Invalid scale");
			}
			if (displacement == null)
			{
				throw new ArgumentNullException();
			}

			this.@base = @base;
			this.index = index;
			this.scale = scale;
			this.displacement = displacement;
		}



		/// <summary>
		/// Compares this memory operand to the specified object for equality. Returns {@code true} if the specified object is a memory operand with the same base, index, scale, and displacement. Otherwise returns {@code false}. </summary>
		/// <param name="obj"> the object to compare this memory operand against </param>
		/// <returns> {@code true} if the object is a memory operand with the same parameters, {@code false} otherwise </returns>
		public override bool Equals(object obj)
		{
			if (!(obj is Memory))
			{
				return false;
			}
			else
			{
				Memory other = (Memory)obj;
				return @base == other.@base && index == other.index && scale == other.scale && displacement.Equals(other.displacement);
			}
		}


		/// <summary>
		/// Returns the hash code for this memory operand. </summary>
		/// <returns> the hash code for this memory operand </returns>
		public override int GetHashCode()
		{
			return @base.GetHashCode() + index.GetHashCode() + scale + displacement.GetHashCode();
		}


		/// <summary>
		/// Returns a string representation of this memory operand. The format is subjected to change. </summary>
		/// <returns> a string representation of this memory operand </returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("[");
			bool hasTerm = false;

			if (@base != null)
			{
				if (hasTerm)
				{
					sb.Append("+");
				}
				else
				{
					hasTerm = true;
				}
				sb.Append(@base);
			}

			if (index != null)
			{
				if (hasTerm)
				{
					sb.Append("+");
				}
				else
				{
					hasTerm = true;
				}
				sb.Append(index);
				if (scale != 1)
				{
					sb.Append("*").Append(scale);
				}
			}

			if (displacement is Label || displacement is ImmediateValue && ((ImmediateValue)displacement).Value != 0 || !hasTerm)
			{
				if (hasTerm)
				{
					sb.Append("+");
				}
				else
				{
					hasTerm = true;
				}
				sb.Append(displacement);
			}

			sb.Append("]");
			return sb.ToString();
		}

	}

}