using System.Linq;

namespace KKC3 {
    public static class BitCountTable {
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

        private static readonly byte[] CountTable = Enumerable.Range(0, 256).Select(x => CountBit((byte)x)).ToArray();

        public static byte Count(byte n) {
            return CountTable[n];
        }

    }
}
