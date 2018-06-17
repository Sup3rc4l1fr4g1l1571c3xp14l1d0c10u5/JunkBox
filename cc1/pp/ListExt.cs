using System.Collections.Generic;
using System.Linq;

namespace CSCPP
{
    public static class ListExt
    {
        /// <summary>
        /// List&lt;T&gt;をスタックのように使うための拡張メソッド
        /// </summary>
        public static T Pop<T>(this List<T> self)
        {
            T ret = self.Last();
            self.RemoveAt(self.Count - 1);

            return ret;
        }
    }

}