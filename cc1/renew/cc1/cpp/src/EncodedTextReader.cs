using System;
using System.IO;
using System.Linq;
using System.Text;

namespace CSCPP {
    /// <summary>
    /// エンコード情報を伴ってテキストを読み取るリーダー
    /// </summary>
    public class EncodedTextReader : IDisposable
    {
        /// <summary>
        /// 読み取り元ストリーム
        /// </summary>
        private Stream _inputStream { get; set; }

        /// <summary>
        /// 入力解析エンコード
        /// </summary>
        public Encoding InputEncoding { get; }

        /// <summary>
        /// 入力解析エンコードから生成したデコーダ
        /// </summary>
        private Decoder _inputDecoder { get; }

        /// <summary>
        /// ストリームから読み取ったRawなバイト列の格納先
        /// </summary>
        private byte[] readBytes;
        private int readBytesLength;

        /// <summary>
        /// 読み取った文字のRawなバイト表現
        /// </summary>
        private byte[] bytes;
        private int bytesLength;

        /// <summary>
        /// 読み取った文字のUTF16表現
        /// </summary>
        private char[] chars;
        private int charsLength;

        /// <summary>
        /// 読み取った文字のUTF32バイト列表現
        /// </summary>
        private byte[] utf32Bytes;

        public EncodedTextReader(Stream inputStream, Encoding inputEncoding)
        {
            this._inputStream = inputStream;
            this.InputEncoding = inputEncoding;
            this._inputDecoder = inputEncoding.GetDecoder();
            this.readBytes = new byte[16];
            this.readBytesLength = 0;
            this.bytes = new byte[16];
            this.bytesLength = 0;
            this.chars = new char[4];
            this.charsLength = 0;
            this.utf32Bytes = new byte[4];

        }

        /// <summary>
        /// エンコードに従って１文字読み取り、内部バッファに情報を格納する
        /// </summary>
        /// <returns></returns>
        private bool ReadChar()
        {
            _inputDecoder.Reset();
            for (; ; )
            {
                var data = _inputStream.ReadByte();
                if (data == -1)
                {
                    return false;
                }
                if (readBytes.Length <= readBytesLength)
                {
                    Array.Resize(ref readBytes, readBytesLength + 16);
                }
                readBytes[readBytesLength++] = (byte)data;
                var charLen = _inputDecoder.GetCharCount(readBytes.ToArray(), 0, readBytesLength);
                if (charLen == 0)
                {
                    continue;
                }
                if (chars.Length <= charLen)
                {
                    Array.Resize(ref chars, charLen);
                }
                int bytesUsed, charsUsed;
                bool completed;
                _inputDecoder.Convert(readBytes, 0, readBytesLength, chars, 0, 4, true, out bytesUsed, out charsUsed, out completed);
                charsLength = charLen;

                if (bytes.Length <= bytesUsed)
                {
                    Array.Resize(ref bytes, bytesUsed);
                }
                Array.Copy(readBytes, 0, bytes, 0, bytesUsed);
                bytesLength = bytesUsed;

                if (readBytesLength != bytesUsed)
                {
                    Array.Copy(readBytes, bytesUsed, readBytes, 0, readBytesLength - bytesUsed);
                }
                readBytesLength -= bytesUsed;

                return true;
            }

        }

        /// <summary>
        /// １文字読み取り、Rawバイト列を返す
        /// </summary>
        /// <returns></returns>
        public byte[] ReadRaw()
        {
            if (this.ReadChar() == false)
            {
                return null;
            }
            else
            {
                return this.bytes.Take(this.bytesLength).ToArray();
            }
        }

        /// <summary>
        /// １文字読み取り、UTF32コードを返す
        /// </summary>
        /// <returns></returns>
        public UInt32 ReadUtf32()
        {
            if (this.ReadChar() == false)
            {
                return UInt32.MaxValue;
            }
            else
            {
                int bytesUsed, charsUsed;
                bool completed;
                Encoding.UTF32.GetEncoder().Convert(chars, 0, charsLength, utf32Bytes, 0, 4, true, out charsUsed, out bytesUsed, out completed);
                return BitConverter.ToUInt32(utf32Bytes, 0);
            }
        }

        /// <summary>
        /// １文字読み取り、エンコード付き文字列を返す
        /// </summary>
        /// <returns></returns>
        public EncodedString ReadEString()
        {
            var ret = ReadRaw();
            if (ret == null)
            {
                return null;
            }
            else
            {
                return new EncodedString(ret, InputEncoding);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this._inputStream.Close();
                    this._inputStream.Dispose();
                    this._inputStream = null;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        public static void Test()
        {
            {
                //var _inputStream = Console.OpenStandardOutput();
                //var _inputDecoder = Console.InputEncoding.GetDecoder();
                var text = @"あいうえお①" + "\n";
                var sjisBytes = Encoding.GetEncoding(932).GetBytes(text);
                var utf32Bytes = Encoding.UTF32.GetBytes(text);

                var sjisBytesStream = new MemoryStream(sjisBytes.ToArray());
                var sjisReader = new EncodedTextReader(sjisBytesStream, Encoding.GetEncoding(932));
                var utf32BytesStream = new MemoryStream(utf32Bytes.ToArray());
                var utf32Reader = new EncodedTextReader(utf32BytesStream, Encoding.UTF32);
                UInt32 u1, u2;
                while ((u1 = sjisReader.ReadUtf32()) != (u2 = utf32Reader.ReadUtf32()))
                {
                    Console.WriteLine($"not match: sjisReader={u1:x8} && utf32Reader={u2:x8}");
                }
            }
        }

    }

}
