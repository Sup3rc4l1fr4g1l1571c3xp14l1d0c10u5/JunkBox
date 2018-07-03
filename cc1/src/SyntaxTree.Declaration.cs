using AnsiCParser.DataType;

namespace AnsiCParser.SyntaxTree {

    /// <inheritdoc />
    /// <summary>
    /// 宣言基底クラス
    /// </summary>
    public abstract class Declaration : Ast {

        /// <summary>
        /// 宣言名
        /// </summary>
        public string Ident {
            get;
        }

        /// <summary>
        /// 宣言の型
        /// </summary>
        public CType Type {
            get;
        }

        /// <summary>
        /// 宣言の記憶指定子
        /// </summary>
        public StorageClassSpecifier StorageClass {
            get;
        }

        /// <summary>
        /// 宣言と対応するリンケージ情報
        /// </summary>
        public LinkageObject LinkageObject {
            get; set;
        }

        /// <inheritdoc />
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="locationRange"></param>
        /// <param name="ident"></param>
        /// <param name="type"></param>
        /// <param name="storageClass"></param>
        protected Declaration(LocationRange locationRange, string ident, CType type, StorageClassSpecifier storageClass) : base(locationRange) {
            Ident = ident;
            Type = type;
            StorageClass = storageClass;
        }

        /// <inheritdoc />
        /// <summary>
        /// 関数宣言/関数定義
        /// </summary>
        public class FunctionDeclaration : Declaration {

            /// <summary>
            /// 関数の本体（これがnullの場合は関数宣言）
            /// </summary>
            public Statement Body {
                get; set;
            }

            /// <summary>
            /// 関数指定子
            /// </summary>
            public FunctionSpecifier FunctionSpecifier {
                get;
            }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="locationRange"></param>
            /// <param name="ident"></param>
            /// <param name="type"></param>
            /// <param name="storageClass"></param>
            /// <param name="functionSpecifier"></param>
            public FunctionDeclaration(LocationRange locationRange, string ident, CType type, StorageClassSpecifier storageClass, FunctionSpecifier functionSpecifier) : base(locationRange, ident, type, storageClass) {
                Body = null;
                FunctionSpecifier = functionSpecifier;
                LinkageObject = new LinkageObject(ident, type, LinkageKind.None);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// 変数宣言/変数定義
        /// </summary>
        public class VariableDeclaration : Declaration {

            /// <summary>
            /// 変数の初期化式（nullの場合は仮定義）
            /// </summary>
            public Initializer Init {
                get; set;
            }

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="locationRange"></param>
            /// <param name="ident"></param>
            /// <param name="type"></param>
            /// <param name="storageClass"></param>
            /// <param name="init"></param>
            public VariableDeclaration(LocationRange locationRange, string ident, CType type, StorageClassSpecifier storageClass/*, Initializer init*/) : base(locationRange, ident, type, storageClass) {
                Init = null;//init;
                LinkageObject = new LinkageObject(ident, type, LinkageKind.None);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// 仮引数定義
        /// </summary>
        public class ArgumentDeclaration : Declaration {

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="locationRange"></param>
            /// <param name="ident"></param>
            /// <param name="type"></param>
            /// <param name="storageClass"></param>
            public ArgumentDeclaration(LocationRange locationRange, string ident, CType type, StorageClassSpecifier storageClass)
                : base(locationRange, ident, type, storageClass) {
                LinkageObject = new LinkageObject(ident, type, LinkageKind.NoLinkage);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Typedef型宣言
        /// </summary>
        public class TypeDeclaration : Declaration {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="locationRange"></param>
            /// <param name="ident"></param>
            /// <param name="type"></param>
            public TypeDeclaration(LocationRange locationRange, string ident, CType type) : base(locationRange, ident, type, StorageClassSpecifier.None) {
                LinkageObject = new LinkageObject(ident, type, LinkageKind.NoLinkage);
            }
        }

        /// <inheritdoc />
        /// <summary>
        ///     列挙型のメンバ宣言（便宜上ここに挿入）
        /// </summary>
        /// <remarks>
        ///     6.4.4.3 列挙定数
        ///     意味規則
        ///     列挙定数として宣言された識別子は，型 int をもつ。
        /// </remarks>
        public class EnumMemberDeclaration : Declaration {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="locationRange"></param>
            /// <param name="mi"></param>
            public EnumMemberDeclaration(LocationRange locationRange, TaggedType.EnumType.MemberInfo mi) : base(locationRange, mi.Ident.Raw, CType.CreateSignedInt(), StorageClassSpecifier.None) {
                MemberInfo = mi;
                LinkageObject = new LinkageObject(mi.Ident.Raw, CType.CreateSignedInt(), LinkageKind.NoLinkage);
            }

            /// <summary>
            /// メンバの詳細情報
            /// </summary>
            public TaggedType.EnumType.MemberInfo MemberInfo {
                get;
            }
        }
    }
}
