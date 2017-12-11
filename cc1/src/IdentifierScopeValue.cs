using System;

namespace AnsiCParser {
    /// <summary>
    /// 通常の識別子の名前空間の要素
    /// </summary>
    public abstract class IdentifierScopeValue {

        public virtual bool IsEnumValue() {
            return false;
        }

        public virtual CType.TaggedType.EnumType.MemberInfo ToEnumValue() {
            throw new Exception("");
        }

        public virtual bool IsVariable() {
            return false;
        }

        public virtual SyntaxTree.Declaration.VariableDeclaration ToVariable() {
            throw new Exception("");
        }

        public virtual bool IsArgument() {
            return false;
        }

        public virtual SyntaxTree.Declaration.ArgumentDeclaration ToArgument() {
            throw new Exception("");
        }

        public virtual bool IsFunction() {
            return false;
        }

        public virtual SyntaxTree.Declaration.FunctionDeclaration ToFunction() {
            throw new Exception("");
        }

        /// <summary>
        /// 6.2.2 識別子の結合 (結合（linkage）)
        /// </summary>
        public enum LinkageKind {
            // 結合は，外部結合，内部結合及び無結合の 3 種類
            /// <summary>
            /// 指定なし
            /// </summary>
            None,

            /// <summary>
            /// 外部結合
            /// (スコープや翻訳単位に関係なく同名のものは常に同じものを指す。)
            /// </summary>
            ExternalLinkage,

            /// <summary>
            /// 内部結合
            /// (翻訳単位内で常に同じものを指す。)
            /// </summary>
            InternalLinkage,

            /// <summary>
            /// 無結合
            /// （指し示し先はすべて独立している）
            /// </summary>
            NoLinkage

        }


        // int foo();  
        // static int foo() { }
        // 
        // は 、まず int foo(); が
        // - 関数の識別子の宣言が記憶域クラス指定子をもたない場合，その結合は，記憶域クラス指定子 externを伴って宣言された場合と同じ規則で決定する。
        // より、extern int foo(); と見なされ、
        // - 可視である以前の宣言がない場合，又は以前の宣言が無結合である場合，この識別子は外部結合をもつ。
        // より、外部結合となる
        // 
        // その次の static int foo() {} は
        // - オブジェクト又は関数に対するファイル有効範囲の識別子の宣言が記憶域クラス指定子 static を含む場合，その識別子は，内部結合をもつ。
        // で内部結合となる。
        // 
        // そして、Linkageの解決で
        // - 翻訳単位の中で同じ識別子が内部結合と外部結合の両方で現れた場合，その動作は未定義とする。
        // となるのでこれは未定義動作になる。
        // 

        public LinkageKind Linkage {
            get; set;
        }

        public class EnumValue : IdentifierScopeValue {
            public override bool IsEnumValue() {
                return true;
            }

            public override CType.TaggedType.EnumType.MemberInfo ToEnumValue() {
                return ParentType.Members.Find(x => x.Ident == Ident);
            }

            public CType.TaggedType.EnumType ParentType {
                get;
            }

            public string Ident {
                get;
            }

            public EnumValue(CType.TaggedType.EnumType parentType, string ident) {
                ParentType = parentType;
                Ident = ident;
                Linkage = LinkageKind.NoLinkage;
            }
        }

        public class Declaration : IdentifierScopeValue {
            public override bool IsVariable() {
                return Decl is SyntaxTree.Declaration.VariableDeclaration && !(Decl is SyntaxTree.Declaration.ArgumentDeclaration);
            }

            public override SyntaxTree.Declaration.VariableDeclaration ToVariable() {
                return Decl as SyntaxTree.Declaration.VariableDeclaration;
            }
            public override bool IsArgument() {
                return Decl is SyntaxTree.Declaration.ArgumentDeclaration;
            }

            public override SyntaxTree.Declaration.ArgumentDeclaration ToArgument() {
                return Decl as SyntaxTree.Declaration.ArgumentDeclaration;
            }

            public override bool IsFunction() {
                return Decl is SyntaxTree.Declaration.FunctionDeclaration;
            }

            public override SyntaxTree.Declaration.FunctionDeclaration ToFunction() {
                return Decl as SyntaxTree.Declaration.FunctionDeclaration;
            }

            public SyntaxTree.Declaration Decl {
                get;
            }

            public Declaration(SyntaxTree.Declaration decl, LinkageKind linkage) {
                Decl = decl;
                Linkage = linkage;
            }

        }
    }
}