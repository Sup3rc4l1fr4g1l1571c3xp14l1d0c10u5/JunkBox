using System;

namespace X86Asm.util
{


	/// <summary>
	/// A mutable sequence of bytes. Bytes can be appended, and the whole sequence can be retrieved. The backing array automatically grows to accomodate the data. Unsynchronized.
	/// </summary>
	public sealed class ByteBuffer
	{

		/// <summary>
		/// The sequence of bytes. Only the substring [0, length) is used. This is not {@code null}.
		/// </summary>
		private byte[] data;

		/// <summary>
		/// The length of the sequence. This is non-negative.
		/// </summary>
		private int length;



		/// <summary>
		/// Constructs a byte buffer with a default initial capacity.
		/// </summary>
		public ByteBuffer() : this(64)
		{
		}


		/// <summary>
		/// Constructs a byte buffer with the specified initial capacity. The initial capacity must be positive. </summary>
		/// <param name="initCapacity"> the initial capacity </param>
		public ByteBuffer(int initCapacity)
		{
			if (initCapacity <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(initCapacity),"Non-positive capacity");
			}
			data = new byte[initCapacity];
			length = 0;
		}



		/// <summary>
		/// Appends the specified byte to this sequence. </summary>
		/// <param name="b"> the byte to append </param>
		public void append(byte b)
		{
			ensureCapacity((long)length + 1);
			data[length] = b;
			length++;
		}


		/// <summary>
		/// Appends the specified 16-bit integer to this sequence as 2 bytes in little endian. </summary>
		/// <param name="x"> the 16-bit integer to append </param>
		public void appendLittleEndian(short x)
		{
			ensureCapacity((long)length + 2);
			data[length + 0] = (byte)((ushort)x >> 0);
			data[length + 1] = (byte)((ushort)x >> 8);
			length += 2;
		}


		/// <summary>
		/// Appends the specified 32-bit integer to this sequence as 4 bytes in little endian. </summary>
		/// <param name="x"> the 32-bit integer to append </param>
		public void appendLittleEndian(int x)
		{
			ensureCapacity((long)length + 4);
			data[length + 0] = (byte)((uint)x >> 0);
			data[length + 1] = (byte)((uint)x >> 8);
			data[length + 2] = (byte)((uint)x >> 16);
			data[length + 3] = (byte)((uint)x >> 24);
			length += 4;
		}


		/// <summary>
		/// Appends the specified byte array to this sequence. </summary>
		/// <param name="x"> the byte array to append </param>
		public void append(byte[] b)
		{
			if (b == null)
			{
				throw new ArgumentNullException(nameof(b));
			}
			ensureCapacity((long)length + b.Length);
			Array.Copy(b, 0, data, length, b.Length);
			length += b.Length;
		}


		/// <summary>
		/// Returns this sequence as a new byte array. The returned array is not the same as the backing array for this buffer. </summary>
		/// <returns> this sequence as a new byte array </returns>
		public byte[] toArray()
		{
			byte[] result = new byte[length];
			Array.Copy(data, 0, result, 0, length);
			return result;
		}


		/// <summary>
		/// Ensures that the backing array is at least as large as the specified capacity. This method resizes the array if its length is less than the specified capacity, otherwise it does nothing. </summary>
		/// <param name="capacity"> </param>
		private void ensureCapacity(long capacity)
		{
			if (capacity < 0)
			{
				throw new System.ArgumentException("Negative capacity");
			}
			if (capacity > int.MaxValue)
			{
				throw new ArgumentOutOfRangeException(nameof(capacity),"Maximum length exceeded");
			}

			while (data.Length < capacity)
			{
				resize((int)Math.Min((long)data.Length * 2, int.MaxValue));
			}
		}


		/// <summary>
		/// Copies the backing array to a new array of the specified length and replaces the original backing array with the new one. </summary>
		/// <param name="newCapacity"> the length of the new backing array </param>
		private void resize(int newCapacity)
		{
			if (newCapacity < 0)
			{
				throw new System.ArgumentException("Negative capacity");
			}
			if (newCapacity < length)
			{
				throw new System.ArgumentException("Capacity less than length");
			}
			byte[] newdata = new byte[newCapacity];
			Array.Copy(data, 0, newdata, 0, length);
			data = newdata;
		}

	}

}