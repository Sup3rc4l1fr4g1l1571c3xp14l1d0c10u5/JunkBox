using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KKC3 {
    public static class CharConv {
        public static string toHiragana(string str) {
            // String.Concat(str.Select(x => (0x30A1 <= x && x <= 0x30F3) ? (char)(x - (0x30A1 - 0x3041)) : (char)x)) よりやや高速
            var s = str.ToCharArray();
            for (var i = 0; i < s.Length; i++) {
                var x = s[i] - 0x30A1U;
                if (x <= (0x30F3U - 0x30A1U)) {
                    s[i] = (char)(x + 0x3041U);
                }
            }
            return new string(s);
        }
    }
}
