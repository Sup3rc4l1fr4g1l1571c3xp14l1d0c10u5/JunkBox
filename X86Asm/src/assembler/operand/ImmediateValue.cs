using System;
using System.Collections.Generic;

namespace X86Asm.operand
{



	/// <summary>
	/// An immediate literal value. It is simply a 32-bit integer. Immutable.
	/// </summary>
	public class ImmediateValue : Immediate
	{

		/// <summary>
		/// The constant zero. </summary>
		public static readonly ImmediateValue ZERO = new ImmediateValue(0);



		/// <summary>
		/// The value. </summary>
		private int value;



		/// <summary>
		/// Constructs an immediate value with the specified signed or unsigned 32-bit integer value. </summary>
		/// <param name="value"> the value </param>
		public ImmediateValue(int value)
		{
			this.value = value;
		}



		/// <summary>
		/// Tests whether this value is zero. </summary>
		/// <returns> {@code true} if this value is zero, {@code false} otherwise </returns>
		public virtual bool Zero
		{
			get
			{
				return value == 0;
			}
		}


		/// <summary>
		/// Tests whether this value is a signed 8-bit integer. </summary>
		/// <returns> {@code true} if this value is a signed 8-bit integer, {@code false} otherwise </returns>
		public virtual bool Signed8Bit
		{
			get
			{
				return ((byte)value) == value;
			}
		}


		/// <summary>
		/// Tests whether this value is a signed 16-bit integer. </summary>
		/// <returns> {@code true} if this value is a signed 16-bit integer, {@code false} otherwise </returns>
		public virtual bool Signed16Bit
		{
			get
			{
				return ((short)value) == value;
			}
		}


		/// <summary>
		/// Tests whether this value is a signed or unsigned 8-bit integer. </summary>
		/// <returns> {@code true} if this value is a signed or unsigned 8-bit integer, {@code false} otherwise </returns>
		public virtual bool is8Bit()
		{
			return value >= -0x80 && value < 0x100;
		}


		/// <summary>
		/// Tests whether this value is a signed or unsigned 16-bit integer. </summary>
		/// <returns> {@code true} if this value is a signed or unsigned 16-bit integer, {@code false} otherwise </returns>
		public virtual bool is16Bit()
		{
			return value >= -0x8000 && value < 0x10000;
		}


		/// <summary>
		/// Returns the value of this immediate value, which is itself. </summary>
		/// <param name="labelOffsets"> the mapping of label names to offsets (ignored) </param>
		/// <returns> the value of this immediate value </returns>
		public override ImmediateValue getValue(IDictionary<string, int> labelOffsets)
		{
			return this;
		}


		/// <summary>
		/// Returns the value of this immediate value. </summary>
		/// <returns> the value </returns>
		public virtual int Value
		{
			get
			{
				return value;
			}
		}


		/// <summary>
		/// Compares this immediate value to the specified object for equality. Returns {@code true} if the specified object is an immediate value with the same value. Otherwise returns {@code false}. </summary>
		/// <param name="obj"> the object to compare this immediate value against </param>
		/// <returns> {@code true} if the object is an immediate value with the same value, {@code false} otherwise </returns>
		public override bool Equals(object other)
		{
			if (!(other is ImmediateValue))
			{
				return false;
			}
			else
			{
				return value == ((ImmediateValue)other).value;
			}
		}


		/// <summary>
		/// Returns the hash code for this immediate value. </summary>
		/// <returns> the hash code for this immediate value </returns>
		public override int GetHashCode()
		{
			return value;
		}


		/// <summary>
		/// Returns a string representation of this immediate value. The format is subjected to change. </summary>
		/// <returns> a string representation of this immediate value </returns>
		public override string ToString()
		{
			return Convert.ToString(value);
		}


		/// <summary>
		/// Returns this value encoded as 4 bytes in little endian. </summary>
		/// <returns> this value encoded as 4 bytes in little endian </returns>
		public virtual byte[] to4Bytes()
		{
			return new byte[]{(byte)((int)((uint)value >> 0)), (byte)((int)((uint)value >> 8)), (byte)((int)((uint)value >> 16)), (byte)((int)((uint)value >> 24))};
		}


		/// <summary>
		/// Returns the low 16 bits of this value encoded as 2 bytes in little endian. </summary>
		/// <returns> the low 16 bits of this value encoded as 2 bytes in little endian </returns>
		public virtual byte[] to2Bytes()
		{
			if (!is16Bit())
			{
				throw new InvalidOperationException("Not a 16-bit integer");
			}
			return new byte[]{(byte)((int)((uint)value >> 0)), (byte)((int)((uint)value >> 8))};
		}


		/// <summary>
		/// Returns the low 8 bits of this value encoded as 1 byte. </summary>
		/// <returns> the low 8 bits of this value encoded as 1 byte </returns>
		public virtual byte[] to1Byte()
		{
			if (!is8Bit())
			{
				throw new InvalidOperationException("Not an 8-bit integer");
			}
			return new byte[]{(byte)((int)((uint)value >> 0))};
		}

	}

}