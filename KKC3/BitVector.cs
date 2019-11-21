using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace KKC3 {
    public class BitVector : IEnumerable<byte> {
        public int Length { get; private set; }
        private List<byte> Vector { get; }

        public int ByteLength { get { return Vector.Count; } }
        public IEnumerable<byte> Bytes { get { return Vector; } }

        public BitVector() {
            Length = 0;
            Vector = new List<byte>();
        }
        public BitVector(IEnumerable<byte> collection) {
            Length = 0;
            Vector = new List<byte>();
            var i = 0;
            foreach (var item in collection) {
                this[i++] = item;
            }
        }
        public byte this[int n] {
            get {
                if (n < 0 || Length <= n) { throw new IndexOutOfRangeException(); }
                var index = n / 8;
                var bitPos = n % 8;
                return (byte)((Vector[index] >> bitPos) & 0x01U);
            }
            set {
                if (n < 0) { throw new IndexOutOfRangeException(); }
                if (Length <= n) {
                    Length = n + 1;
                }
                var index = n / 8;
                var bitPos = n % 8;

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
                for (var i = 0; i < Vector.Count; i++) {
                    var v = Vector[i];
                    if (n > BitCountTable.Count(v)) {
                        n -= BitCountTable.Count(v);
                    } else {
                        var ret = i * 8;
                        for (;;) {
                            if ((v & 0x01) != 0) {
                                n--;
                                if (n == 0) {
                                    return ret;
                                }
                            }
                            ret++;
                            v >>= 1;
                        }
                    }
                }
            } else {
                for (var i = 0; i < Vector.Count; i++) {
                    var v = Vector[i];
                    if (n > 8 - BitCountTable.Count(v)) {
                        n -= (8 - BitCountTable.Count(v));
                    } else {
                        var ret = i * 8;
                        for (;;) {
                            if ((v & 0x01) == 0) {
                                n--;
                                if (n == 0) {
                                    return ret;
                                }
                            }
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
        /// ビット列の[0..position]の範囲で targetBit が出現する回数を求める
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
                n += BitCountTable.Count(Vector[i]);
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
        public void Add(byte bit) {
            this[Length] = bit;
        }
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
