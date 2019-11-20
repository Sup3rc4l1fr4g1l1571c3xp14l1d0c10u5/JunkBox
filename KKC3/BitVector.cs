using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public class BitVector : IEnumerable<byte>, IEnumerable {
        public int Length { get; private set; }
        private int Capacity { get; set; }
        private List<byte> vector { get; } = new List<byte>();

        public int ByteLength { get { return vector.Count; } }
        public IEnumerable<byte> Bytes { get { return vector; } }

        public BitVector() { }
        public BitVector(IEnumerable<byte> collection) {
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
                return (byte)((vector[index] >> bitPos) & 0x01U);
            }
            set {
                if (n < 0) { throw new IndexOutOfRangeException(); }
                if (Length <= n) {
                    Length = n + 1;
                }
                var index = n / 8;
                var bitPos = n % 8;

                if (vector.Count <= index) {
                    vector.Capacity = index + 1;
                    for (var i = vector.Count; i <= index; i++) {
                        vector.Add(0);
                    }
                }
                if (value == 0) {
                    vector[index] &= (byte)~(1 << bitPos);
                } else {
                    vector[index] |= (byte)(1 << bitPos);
                }
            }
        }

        private static readonly byte[] BitCountTable = Enumerable.Range(0, 256).Select(x => CountBit((byte)x)).ToArray();

        private static byte CountBit(byte n) {
            byte cnt = 0;
            for (var i = 0; i < 8 && n != 0; i++) {
                if ((n & 0x01) != 0) {
                    cnt += 1;
                }
                n >>= 1;
            }
            return cnt;
        }

        /// <summary>
        /// ビット列の先頭から見て n 回目に target_bit が出現する位置を求める
        /// </summary>
        /// <param name="n"></param>
        /// <param name="target_bit"></param>
        /// <returns></returns>
        public int? select(int n, byte target_bit) {
            if (n <= 0) {
                return null;
            }
#if true
            if (target_bit != 0) {
                for (var i = 0; i < this.vector.Count; i++) {
                    var v = this.vector[i];
                    if (n > BitCountTable[v]) {
                        n -= BitCountTable[v];
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
                for (var i = 0; i < this.vector.Count; i++) {
                    var v = this.vector[i];
                    if (n > 8 - BitCountTable[v]) {
                        n -= (8 - BitCountTable[v]);
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
                if (this[i] == target_bit) {
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
        /// ビット列の[0..position]の範囲で target_bit が出現する回数を求める
        /// </summary>
        /// <param name="n"></param>
        /// <param name="target_bit"></param>
        /// <returns></returns>
        public int rank(int position, byte target_bit) {
            if (position < 0) {
                return 0;
            }
            target_bit = (byte)((target_bit != 0) ? 1 : 0);
#if true
            position += 1;
            var index = position / 8;
            var bitpos = position % 8;
            var n = 0;
            for (var i = 0; i < index; i++) {
                n += BitCountTable[this.vector[i]];
            }
            if (bitpos > 0) {
                var v = this.vector[index];
                while (bitpos > 0) {
                    if ((v & 0x01) != 0) {
                        n++;
                    }
                    bitpos--;
                    v >>= 1;
                }
            }
            return target_bit != 0 ? n : (position - n);
#else
            var n = 0;
            for (var i = 0; i <= position; i++) {
                if (this[i] == target_bit) {
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
