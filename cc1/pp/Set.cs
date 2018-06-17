namespace CSCPP
{
    /// <summary>
    /// 単方向リンクリストで作るimmutableな集合
    /// </summary>
    public class Set {
        public string Value { get; }
        public Set Parent { get; }
        public Set(string value, Set parent = null) {
            Value = value;
            Parent = parent;
        }
    }

    public static class SetExt {
        /// <summary>
        /// 値valueの追加
        /// </summary>
        /// <param name="self">親集合</param>
        /// <param name="value">値</param>
        /// <returns></returns>
        public static Set Add(this Set self, string value)
        {
                return new Set(value, self);
        }

        /// <summary>
        /// 集合中に値valueが含まれるか
        /// </summary>
        /// <param name="self">集合</param>
        /// <param name="value">値</param>
        /// <returns></returns>
        public static bool Contains(this Set self, string value)
        {

            for (var s = self; s != null; s = s.Parent)
            {
                if (s.Value == value)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 集合selfとotherの論理和を取った新しい集合を返す
        /// </summary>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static Set Union(this Set self, Set other)
        {
            Set result = other;
            for (var s = self; s != null; s = s.Parent)
            {
                if (!other.Contains(s.Value))
                {
                    result = result.Add(s.Value);
                }
            }
            return result;
        }

        /// <summary>
        /// 集合selfとotherの論理積を取った新しい集合を返す
        /// </summary>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static Set Intersect(this Set self, Set other)
        {
            Set result = other;
            for (var s = self; s != null; s = s.Parent) {
                if (other.Contains(s.Value)) {
                    result = result.Add(s.Value);
                }
            }
            return result;
        }
    }
}