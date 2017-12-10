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

            public Declaration(SyntaxTree.Declaration decl) {
                Decl = decl;
            }
        }
    }
}