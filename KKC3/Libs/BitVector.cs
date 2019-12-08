using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace KKC3
{
    /// <summary>
    /// ビットベクタ
    /// </summary>
    public class BitVector : IEnumerable<byte>
    {
        /// <summary>
        /// ビットベクタ長
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// ビットベクタ領域
        /// </summary>
        private List<byte> Vector { get; }

        /// <summary>
        /// ビットベクタのバイト長
        /// </summary>
        public int ByteLength { get { return Vector.Count; } }

        /// <summary>
        /// ビットベクタのバイト列
        /// </summary>
        public IEnumerable<byte> Bytes { get { return Vector; } }

        /// <summary>
        /// 空のビットベクタを作る
        /// </summary>
        public BitVector() {
            Length = 0;
            Vector = new List<byte>();
        }

        /// <summary>
        /// 初期値となるビット列を指定してビットベクタを作る
        /// </summary>
        /// <param name="bits">ビット列（バイト列ではない）</param>
        public BitVector(IEnumerable<byte> bits) {
            Length = 0;
            Vector = new List<byte>();
            var i = 0;
            foreach (var item in bits) {
                this[i++] = item;
            }
        }

        /// <summary>
        /// 指定した位置のビットを読み書きする
        /// </summary>
        /// <param name="pos">ビットの位置</param>
        /// <returns></returns>
        public byte this[int pos] {
            get {
                if (pos < 0 || Length <= pos) { throw new IndexOutOfRangeException(); }
                var index = pos / 8;
                var bitPos = pos % 8;
                return (byte)((Vector[index] >> bitPos) & 0x01U);
            }
            set {
                if (pos < 0) { throw new IndexOutOfRangeException(); }
                if (Length <= pos) {
                    Length = pos + 1;
                }
                var index = pos / 8;
                var bitPos = pos % 8;

                if (Vector.Count <= index) {
                    for (var i = Vector.Count; i <= index; i++) {
                        Vector.Add(0);
                    }
                }
                if (value == 0) {
                    Vector[index] &= (byte)~(1 << bitPos);
                } else {
                    Vector[index] |= (byte)(1 << bitPos);
                }
            }
        }

        /// <summary>
        /// ビット列の先頭から見て n 回目に targetBit が出現する位置を求める
        /// </summary>
        /// <param name="n"></param>
        /// <param name="targetBit"></param>
        /// <returns></returns>
        public int? Select(int n, byte targetBit) {
            if (n <= 0) {
                return null;
            }
#if true
            if (targetBit != 0) {
                var b = 0;
                for (var i = 0; i < Vector.Count && b < Length; i++) {
                    var v = Vector[i];
                    if (n > BitCounter.Count(v)) {
                        n -= BitCounter.Count(v);
                        b += 8;
                    } else {
                        var ret = i * 8;
                        for (; ; ) {
                            if (b >= Length) {
                                return null;
                            }
                            if ((v & 0x01) != 0) {
                                n--;
                                if (n == 0) {
                                    return ret;
                                }
                            }
                            b++;
                            ret++;
                            v >>= 1;
                        }
                    }
                }
            } else {
                var b = 0;
                for (var i = 0; i < Vector.Count && b < Length; i++) {
                    var v = Vector[i];
                    if (n > 8 - BitCounter.Count(v)) {
                        n -= (8 - BitCounter.Count(v));
                        b += 8;
                    } else {
                        var ret = i * 8;
                        for (; ; ) {
                            if (b >= Length) {
                                return null;
                            }
                            if ((v & 0x01) == 0) {
                                n--;
                                if (n == 0) {
                                    return ret;
                                }
                            }
                            b++;
                            ret++;
                            v >>= 1;
                        }
                    }
                }
            }
            return null;
#else
            for (var i = 0; i < this.Length; i++) {
                if (this[i] == targetBit) {
                    n -= 1;
                }
                if (n == 0) {
                    return i;
                }
            }
            return null;
#endif
        }

        /// <summary>
        /// ビット列の[0..position)の範囲で targetBit が出現する回数を求める
        /// </summary>
        /// <param name="position"></param>
        /// <param name="targetBit"></param>
        /// <returns></returns>
        public int Rank(int position, byte targetBit) {
            if (position < 0) {
                return 0;
            }
            targetBit = (byte)((targetBit != 0) ? 1 : 0);
#if true
            position += 1;
            var index = position / 8;
            var bitPos = position % 8;
            var n = 0;
            for (var i = 0; i < index; i++) {
                n += BitCounter.Count(Vector[i]);
            }
            if (bitPos > 0) {
                var v = Vector[index];
                while (bitPos > 0) {
                    if ((v & 0x01) != 0) {
                        n++;
                    }
                    bitPos--;
                    v >>= 1;
                }
            }
            return targetBit != 0 ? n : (position - n);
#else
            var n = 0;
            for (var i = 0; i <= position; i++) {
                if (this[i] == targetBit) {
                    n += 1;
                }
            }
            return n;
#endif
        }

        /// <summary>
        /// 末尾にビットを追加する
        /// </summary>
        /// <param name="bit"></param>
        public void Add(byte bit) {
            this[Length] = bit;
        }

        /// <summary>
        /// 末尾にビット列を追加する
        /// </summary>
        /// <param name="bit"></param>
        public void AddRange(IEnumerable<byte> bits) {
            foreach (var bit in bits) {
                Add(bit);
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            for (var i = 0; i < Length; i++) {
                sb.Append(this[i]);
            }
            return sb.ToString();
        }

        public IEnumerator GetEnumerator() {
            for (var i = 0; i < Length; i++) {
                yield return this[i];
            }
        }

        IEnumerator<byte> IEnumerable<byte>.GetEnumerator() {
            for (var i = 0; i < Length; i++) {
                yield return this[i];
            }
        }
    }

}
