using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CParser2 {
    public class Scope {

        public static List<Scope> scopes { get; } = new List<Scope>();

        public static Scope Empty { get; } = new Scope();

        /// <summary>
        /// タグの名前空間
        /// </summary>
        public List<Tuple<string, CType.TaggedType>> tags {
            get;
        }

        /// <summary>
        /// 識別子の名前空間（宣言順序も必要になるのでリストを使う）
        /// </summary>
        public List<Tuple<string, IdentifierValue, SyntaxNode.StorageClassSpecifierKind, int>> identifiers {
            get;
        }

        /// <summary>
        /// 親スコープ
        /// </summary>
        public Scope Parent {
            get;
        }

        private Scope() {
            tags = null;
            identifiers = null;
            Parent = this;
            scopes.Add(this);
        }

        public Scope(Scope parent) {
            tags = new List<Tuple<string, CType.TaggedType>>();
            identifiers = new List<Tuple<string, IdentifierValue, SyntaxNode.StorageClassSpecifierKind,int>>();
            Parent = parent;
            scopes.Add(this);
        }

        public IdentifierValue FindIdentifierCurrent(string identifier) {
            var entry = this.identifiers.LastOrDefault(x => x.Item1 == identifier);
            if (entry != null) {
                return entry.Item2;
            }
            return null;
        }

        /// <summary>
        /// 識別子を検索
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public IdentifierValue FindIdentifier(string identifier) {
            var s = this;
            while (s != Empty) {
                var entry = s.identifiers.LastOrDefault(x => x.Item1 == identifier);
                if (entry != null) {
                    return entry.Item2;
                }
                s = s.Parent;
            }
            return null;
        }

        /// <summary>
        /// 識別子を追加
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public void AddIdentifier(
            string identifier, 
            IdentifierValue p,
            SyntaxNode.StorageClassSpecifierKind sc,
            int bit/*構造体・共用体のメンバ用*/) {
            identifiers.Add(Tuple.Create(identifier, p, sc, bit));
        }

        /// <summary>
        /// 識別子の値
        /// </summary>
        public abstract class IdentifierValue {
            /// <summary>
            /// 識別子は型を示す(Typedef名および組み込み型）
            /// </summary>
            public class Type : IdentifierValue {
                public CType type {
                    get;
                }
                public SyntaxNode.StorageClassSpecifierKind StorageClass { get; }
                public Type(CType type, SyntaxNode.StorageClassSpecifierKind sc) {
                    this.type = type;
                    this.StorageClass = sc;
                }
            }
            /// <summary>
            /// 識別子は列挙メンバを示す
            /// </summary>
            public class EnumMember : IdentifierValue {
                public CType.TaggedType.EnumType type {
                    get;
                }

                public int index {
                    get;
                }
                public EnumMember(CType.TaggedType.EnumType ctype, int index) {
                    this.type = ctype;
                    this.index = index;
                }
            }
            /// <summary>
            /// 識別子は変数を示す
            /// </summary>
            public class Variable : IdentifierValue {
                public CType type {
                    get;
                }
                public SyntaxNode.StorageClassSpecifierKind StorageClass { get; }

                public Instruction.Variable body {
                    get; set;
                }

                public Variable(CType type, SyntaxNode.StorageClassSpecifierKind sc) {
                    this.type = type;
                    this.StorageClass = sc;
                }
            }
            /// <summary>
            /// 識別子は関数を示す
            /// </summary>
            public class Function : IdentifierValue {
                public CType type {
                    get;
                }
                public SyntaxNode.StorageClassSpecifierKind StorageClass { get; }
                public Instruction.Label body {
                    get; set;
                }

                public Function(CType type, SyntaxNode.StorageClassSpecifierKind sc) {
                    this.type = type;
                    this.StorageClass = sc;
                }
            }

            internal class Argument : IdentifierValue {
                public CType type {
                    get;
                }
                public SyntaxNode.StorageClassSpecifierKind StorageClass { get; }

                public int index {
                    get; 
                }

                public Argument(CType type, SyntaxNode.StorageClassSpecifierKind sc, int i) {
                    this.type = type;
                    this.StorageClass = sc;
                    this.index = i;
                }
            }
        }

        /// <summary>
        /// タグ付き型を検索
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public CType.TaggedType FindTaggedType(string identifier) {
            var s = this;
            while (s != Empty) {
                var entry = s.tags.LastOrDefault(x => x.Item1 == identifier);
                if (entry != null) {
                    return entry.Item2;
                }
                s = s.Parent;
            }
            return null;
        }

        public CType.TaggedType FindTaggedTypeCurrent(string identifier) {
            var s = this;
            var entry = s.tags.LastOrDefault(x => x.Item1 == identifier);
            if (entry != null) {
                return entry.Item2;
            }
            return null;
        }

        /// <summary>
        /// 識別子を追加
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public void AddTaggedType(string identifier, CType.TaggedType p) {
            tags.Add(Tuple.Create(identifier, p));
        }

    }

}
