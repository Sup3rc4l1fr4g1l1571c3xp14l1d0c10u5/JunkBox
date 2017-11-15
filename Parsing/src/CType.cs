using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CParser2 {

    public abstract class CType {
        public List<SyntaxNode.TypeQualifierKind> type_qualifier {
            get;
        } = new List<SyntaxNode.TypeQualifierKind>();

        /// <summary>
        /// 型解析時にエラーとなったことを示す型
        /// </summary>
        public class InvalidType : CType {
            public string message {
                get;
            }

            public InvalidType(string message) {
                this.message = message;
            }
        }

        /// <summary>
        /// 未定義型
        /// </summary>
        public class UndefinedType : CType {
            public string message {
                get;
            }

            public UndefinedType(string message) {
                this.message = message;
            }
        }

        /// <summary>
        /// 基本型
        /// </summary>
        public class StandardType : CType {
            /// <summary>
            /// 基本型を示すビットフラグを示す列挙型
            /// </summary>
            [Flags]
            public enum TypeBit {
                _signed = 0x00100000,
                _unsigned = 0x00200000,
                _signBitMask = 0x00300000,

                _long = 0x00010000,
                _short = 0x00020000,
                _lenBitMask = 0x00030000,

                _char = 0x00000001,
                _int = 0x00000002,
                _float = 0x00000004,
                _double = 0x00000008,
                _void = 0x00000010,
                _bool = 0x00000020,
                _complex = 0x00000040,
                _imaginary = 0x00000080,
                _va_list = 0x00000100,
                _baseBitMask = 0x000001FF,
            }

            /// <summary>
            /// 基本型を示すビットフラグ
            /// </summary>
            public TypeBit bits {
                get;
            }

            public StandardType(TypeBit bits) {
                this.bits = bits;
            }
        }


        /// <summary>
        /// タグ付き型
        /// </summary>
        public abstract class TaggedType : CType {
            /// <summary>
            /// タグ名
            /// </summary>
            public string tag {
                get;
            }
            /// <summary>
            /// 匿名型
            /// </summary>
            public bool anonymous {
                get;
            }

            protected TaggedType(string tag, bool anonymous) {
                this.tag = tag;
                this.anonymous = anonymous;
            }

            /// <summary>
            /// 列挙型
            /// </summary>
            public class EnumType : TaggedType {
                /// <summary>
                /// メンバ
                /// </summary>
                public List<Tuple<string, int>> enumerators {
                    get; set;
                }

                public EnumType(string tag, bool anonymous, List<Tuple<string, int>> membersp) : base(tag, anonymous) {
                    this.enumerators = enumerators;
                }
            }

            /// <summary>
            /// 共用体型
            /// </summary>
            public class UnionType : TaggedType {
                public List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind, int>> members {
                    get; set;
                }

                public UnionType(string tag, bool anonymous, List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind, int>> members) : base(tag, anonymous) {
                    this.members = members;
                }
            }

            /// <summary>
            /// 構造体型
            /// </summary>
            public class StructType : TaggedType {
                public List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind, int>> members {
                    get; set;
                }

                public StructType(string tag, bool anonymous, List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind, int>> members) : base(tag, anonymous) {
                    this.members = members;
                }
            }
        }

        internal class PointerType : CType {
            public CType type {
                get;
            }

            public PointerType(CType type) {
                this.type = type;
            }
        }

        public class ArrayType : CType {
            public CType type {
                get;
            }

            public int size { get; }

            public ArrayType(CType type, int size) {
                this.type = type;
                this.size = size;
            }
        }

        public class FunctionType : CType {
            public List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>> args{ get; }
            public CType ty{ get; }
            public bool KAndR { get; }

            public FunctionType(List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>> args, CType ty, bool KAndR) {
                this.args = args;
                this.ty = ty;
                this.KAndR = KAndR;
            }
        }
    }

    /// <summary>
    /// 構文木から型を構築する
    /// </summary>
    public static class TypeBuilder {
        public static CType Parse(
            SyntaxNode.DeclarationSpecifiers declaration_specifiers,
            SyntaxNode.Declarator declarator,
            IReadOnlyList<SyntaxNode.Declaration> declaration_list,
            Scope currentScope,
            List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>> entry
        ) {
            var ret = Parse(
                declaration_specifiers.type_specifiers,
                declaration_specifiers.type_qualifiers,
                declaration_specifiers.storage_class_specifier,
                declaration_specifiers.function_specifier,
                declarator,
                declaration_list,
                currentScope,
                entry
                );
            return ret;

        }

        public static CType Parse(
            List<SyntaxNode.TypeSpecifier> type_specifiers, 
            List<SyntaxNode.TypeQualifierKind> type_qualifiers, 
            SyntaxNode.StorageClassSpecifierKind storage_class_specifier, 
            SyntaxNode.DeclarationSpecifiers.FuntionSpecifierKind function_specifier, 
            SyntaxNode.Declarator declarator, 
            IReadOnlyList<SyntaxNode.Declaration> declaration_list, 
            Scope currentScope,
            List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>> entry
        ) {
            // 宣言指定子を解析して型を取得
            var basetype = ParseTypeSpecifiers(type_specifiers, currentScope);

            // 型修飾子を適用
            var type_qualifier = type_qualifiers.Distinct().OrderBy(x => x).ToArray();
            basetype.type_qualifier.AddRange(type_qualifier);

            // basetypeを使って宣言子を解析して型を構築
            var symbollist = new List<Tuple<string, CType >>();
            var ret = ParseDeclarator(basetype, declarator, declaration_list, currentScope, symbollist);

            // ストレージクラスは名前に適用する
            entry.AddRange(symbollist.Select(x => Tuple.Create(x.Item1, x.Item2, storage_class_specifier)));

            return ret;
        }


        /// <summary>
        /// 宣言子を解析
        /// </summary>
        /// <param name="declaration_list"></param>
        /// <param name="basetype"></param>
        /// <param name="currentScope"></param>
        /// <returns></returns>
        private static CType ParseDeclarator(CType basetype, SyntaxNode.Declarator declarator, IReadOnlyList<SyntaxNode.Declaration> declaration_list, Scope currentScope, List<Tuple<string, CType>> ret) {

            // 解析スタックを作成
            Stack<Func<CType, List<Tuple<string, CType>>, CType>> parseActionStack = new Stack<Func<CType, List<Tuple<string, CType>>, CType>>();

            // ParseDeclaratorを探索して解析スタックを構築
            ParseDeclaratorInner(parseActionStack, declarator, currentScope);

            // 解析スタックを評価して型を導出
            var rettype = parseActionStack.Aggregate(basetype, (s, x) => x(s, ret));

            // declaration_listが存在する場合、関数宣言はK&Rスタイルの可能性がある
            if (declaration_list != null) {
                var ft = rettype as CType.FunctionType;
                if (ft == null) {
                    return new CType.InvalidType("関数形式ではない");
                }
                if (ft.KAndR == false) {
                    return new CType.InvalidType("K&R関数形式ではない");
                }

                // declaration_listから宣言リストを構築
                var argdecls = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
                foreach (var decl in declaration_list) {
                    foreach (var initdecl in decl.init_declarators) {
                        Parse(decl.declaration_specifiers, initdecl.declarator, new SyntaxNode.Declaration[0], currentScope, argdecls);
                    }
                }
                // 宣言リストと引数リストをマージする
                var list = ft.args.Select(x => {
                    var entry = argdecls.FirstOrDefault(y => y.Item1 == x.Item1);
                    if (entry != null) {
                        return entry;
                    } else {
                        return Tuple.Create(x.Item1, (CType)new CType.StandardType((CType.StandardType.TypeBit)0), SyntaxNode.StorageClassSpecifierKind.none);
                    }
                }).ToList();

                // マージした結果で引数リストを置き換える
                ft.args.Clear();
                ft.args.AddRange(list);
            }
            return rettype;
        }

        private static CType ParseDeclaratorApplyTypeQualifierKindWithPointer(IReadOnlyList<SyntaxNode.TypeQualifierKindWithPointer> pointer, CType ty) {
            if (pointer != null) {
            
            foreach (var p in pointer.Reverse()) {
                if (p == SyntaxNode.TypeQualifierKindWithPointer.none) {
                    continue;
                }

                if (p == SyntaxNode.TypeQualifierKindWithPointer.pointer_keyword) {
                    ty = new CType.PointerType(ty);
                } else {
                    ty.type_qualifier.Add((SyntaxNode.TypeQualifierKind)p);
                }
            }
            }
            return ty;
        }
        private static void ParseDeclaratorInner(Stack<Func<CType, List<Tuple<string, CType>>, CType>> parseActionStack, SyntaxNode.Declarator declarator, Scope currentScope) {
            if (declarator is SyntaxNode.Declarator.GroupedDeclarator) {
                // GroupedDeclarator は結合を示すだけなので中のDeclaratorを解析する
                var decl = (SyntaxNode.Declarator.GroupedDeclarator)declarator;
                ParseDeclaratorInner(parseActionStack, decl.@base, currentScope);
                return;
            }

            if (declarator is SyntaxNode.Declarator.AbstractDeclarator.GroupedAbstractDeclarator) {
                // GroupedAbstractDeclarator は GroupedDeclarator と同じ
                var decl = (SyntaxNode.Declarator.AbstractDeclarator.GroupedAbstractDeclarator)declarator;
                ParseDeclaratorInner(parseActionStack, decl.@base, currentScope);
                return;
            }

            if (declarator is SyntaxNode.Declarator.IdentifierDeclarator) {
                // IdentifierDeclarator は 識別子なので名前表retに宣言identifier=tyとして登録する
                var decl = (SyntaxNode.Declarator.IdentifierDeclarator)declarator;
                parseActionStack.Push((ty, ret) => {
                    ty = ParseDeclaratorApplyTypeQualifierKindWithPointer(decl.pointer, ty);
                    ret.Add(Tuple.Create(decl.identifier, ty));
                    return ty;
                });
                return;
            }


            if (declarator is SyntaxNode.Declarator.ArrayDeclarator) {
                var decl = (SyntaxNode.Declarator.ArrayDeclarator)declarator;
                ParseDeclaratorInner(parseActionStack, decl.@base, currentScope);
                parseActionStack.Push((ty, ret) => {
                    ty = new CType.ArrayType(ty, 0/*decl.size_expression*/);
                    ty = ParseDeclaratorApplyTypeQualifierKindWithPointer(decl.pointer, ty);
                    return ty;
                });
                return;
            }
            if (declarator is SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator) {
                var decl = (SyntaxNode.Declarator.AbstractDeclarator.ArrayAbstractDeclarator)declarator;
                ParseDeclaratorInner(parseActionStack, decl.@base, currentScope);
                parseActionStack.Push((ty, ret) => {
                    ty = new CType.ArrayType(ty, 0/*decl.size_expression*/);
                    ty = ParseDeclaratorApplyTypeQualifierKindWithPointer(decl.pointer, ty);
                    return ty;
                });
                return;
            }

            if (declarator is SyntaxNode.Declarator.FunctionDeclarator.AnsiFunctionDeclarator) {
                var decl = (SyntaxNode.Declarator.FunctionDeclarator.AnsiFunctionDeclarator)declarator;
                var args = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
                foreach (var parameter_decl in decl.parameter_type_list.parameters) {
                    var names = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
                    var ty = Parse(parameter_decl.declaration_specifiers, parameter_decl.declarator, new SyntaxNode.Declaration[0], currentScope, names);
                    var name = (names.Any()) ? names.First().Item1 : "";
                    var sc = (names.Any()) ? names.First().Item3 : SyntaxNode.StorageClassSpecifierKind.none;
                    args.Add(Tuple.Create(name, ty, sc));
                }
                ParseDeclaratorInner(parseActionStack, decl.@base, currentScope);
                parseActionStack.Push((ty, ret) => {
                    ty = new CType.FunctionType(args, ty, false);
                    ty = ParseDeclaratorApplyTypeQualifierKindWithPointer(decl.pointer, ty);
                    return ty;
                });
                return;
            }
            if (declarator is SyntaxNode.Declarator.AbstractDeclarator.FunctionAbstractDeclarator) {
                var decl = (SyntaxNode.Declarator.AbstractDeclarator.FunctionAbstractDeclarator)declarator;
                var args = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
                foreach (var parameter_decl in decl.parameter_type_list.parameters) {
                    var names = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
                    var ty = Parse(parameter_decl.declaration_specifiers, parameter_decl.declarator, new SyntaxNode.Declaration[0], currentScope, names);
                    var name = (names.Any()) ? names.First().Item1 : "";
                    var sc = (names.Any()) ? names.First().Item3 : SyntaxNode.StorageClassSpecifierKind.none;
                    args.Add(Tuple.Create(name, ty, sc));
                }
                ParseDeclaratorInner(parseActionStack, decl.@base, currentScope);
                parseActionStack.Push((ty, ret) => {
                    ty = new CType.FunctionType(args, ty, false);
                    ty = ParseDeclaratorApplyTypeQualifierKindWithPointer(decl.pointer, ty);
                    return ty;
                });
                return;
            }
            if (declarator is SyntaxNode.Declarator.FunctionDeclarator.KandRFunctionDeclarator) {
                var decl = (SyntaxNode.Declarator.FunctionDeclarator.KandRFunctionDeclarator)declarator;
                var args = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
                foreach (var parameter_ident in decl.identifier_list) {
                    args.Add(Tuple.Create(parameter_ident, (CType)null, SyntaxNode.StorageClassSpecifierKind.none));
                }
                ParseDeclaratorInner(parseActionStack, decl.@base, currentScope);
                parseActionStack.Push((ty, ret) => {
                    ty = new CType.FunctionType(args, ty, false);
                    ty = ParseDeclaratorApplyTypeQualifierKindWithPointer(decl.pointer, ty);
                    return ty;
                });
                return;
            }
            if (declarator is SyntaxNode.Declarator.FunctionDeclarator.KandRFunctionDeclarator) {
                var decl = (SyntaxNode.Declarator.FunctionDeclarator.KandRFunctionDeclarator)declarator;
                var args = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
                foreach (var parameter_ident in decl.identifier_list) {
                    args.Add(Tuple.Create(parameter_ident, (CType)null, SyntaxNode.StorageClassSpecifierKind.none));
                }
                ParseDeclaratorInner(parseActionStack, decl.@base, currentScope);
                parseActionStack.Push((ty, ret) => {
                    ty = new CType.FunctionType(args, ty, false);
                    ty = ParseDeclaratorApplyTypeQualifierKindWithPointer(decl.pointer, ty);
                    return ty;
                });
                return;
            }
            if (declarator is SyntaxNode.Declarator.FunctionDeclarator.AbbreviatedFunctionDeclarator) {
                var decl = (SyntaxNode.Declarator.FunctionDeclarator.AbbreviatedFunctionDeclarator)declarator;
                var args = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
                ParseDeclaratorInner(parseActionStack, decl.@base, currentScope);
                parseActionStack.Push((ty, ret) => {
                    ty = new CType.FunctionType(args, ty, false);
                    ty = ParseDeclaratorApplyTypeQualifierKindWithPointer(decl.pointer, ty);
                    return ty;
                });
                return;
            }
            if (declarator is SyntaxNode.Declarator.AbstractDeclarator.PointerAbstractDeclarator) {
                var decl = (SyntaxNode.Declarator.AbstractDeclarator.PointerAbstractDeclarator)declarator;
                var args = new List<Tuple<string, CType>>();
                ParseDeclaratorInner(parseActionStack, decl.@base, currentScope);
                parseActionStack.Push((ty, ret) => {
                    ty = new CType.PointerType(ty);
                    return ty;
                });
                return;
            }
        }

        /// <summary>
        /// 宣言指定子を解析
        /// </summary>
        /// <param name="type_specifiers"></param>
        /// <param name="currentScope"></param>
        /// <returns></returns>
        private static CType ParseTypeSpecifiers(
            List<SyntaxNode.TypeSpecifier> type_specifiers, 
            Scope currentScope
        ) {
            // type_specifiers に要素が無い場合はとりあえず暗黙的なint宣言と見なす
            if (type_specifiers.Any() == false) {
                return new CType.StandardType((CType.StandardType.TypeBit)0);
            }

            // type_specifiers の最初の要素を見て場合分けをする
            var type_specifier_fst = type_specifiers.First();


            // 基本型指定子の場合は基本型の並びと見なして解析
            if (type_specifier_fst is SyntaxNode.TypeSpecifier.StandardTypeSpecifier) {
                return ParseStandardSpecifier(type_specifiers, currentScope);
            }

            // 構造体型指定子の場合は構造体型と見なして解析
            if (type_specifier_fst is SyntaxNode.TypeSpecifier.StructSpecifier) {
                var ts = type_specifier_fst as SyntaxNode.TypeSpecifier.StructSpecifier;
                if (type_specifiers.Count > 1) {
                    return new CType.InvalidType("組み合わせられない型指定子の組み合わせ");
                }
                return ParseStructSpecifier(ts, currentScope);

            }

            // 共用体型指定子の場合は共用体型と見なして解析
            if (type_specifier_fst is SyntaxNode.TypeSpecifier.UnionSpecifier) {
                var ts = type_specifier_fst as SyntaxNode.TypeSpecifier.UnionSpecifier;
                if (type_specifiers.Count > 1) {
                    return new CType.InvalidType("組み合わせられない型指定子の組み合わせ");
                }
                return ParseUnionSpecifier(ts, currentScope);
            }

            // 列挙指定子の場合は列挙型と見なして解析
            if (type_specifier_fst is SyntaxNode.TypeSpecifier.EnumSpecifier) {
                var ts = type_specifier_fst as SyntaxNode.TypeSpecifier.EnumSpecifier;
                if (type_specifiers.Count > 1) {
                    return new CType.InvalidType("組み合わせられない型指定子の組み合わせ");
                }
                return ParseEnumSpecifier(ts, currentScope);
            }

            // typedef名の場合は名前表をチェックして場合分け
            if (type_specifier_fst is SyntaxNode.TypeSpecifier.TypedefTypeSpecifier) {
                var ts = type_specifier_fst as SyntaxNode.TypeSpecifier.TypedefTypeSpecifier;

                // 名前表から識別子を探す
                var ty = ResolvTypedef(ts, currentScope);

                // 登録されている識別子が基本型の場合は基本型の処理に丸投げ
                if (ty is CType.StandardType) {
                    return ParseStandardSpecifier(type_specifiers,currentScope);
                }

                // それ以外の型の場合は複合型と見なす
                if (type_specifiers.Count > 1) {
                    return new CType.InvalidType("組み合わせられない型指定子の組み合わせ");
                }

                return ty;
            }

            // ここには来ないはず
            return new CType.InvalidType("解釈不能な型");
        }

        private static CType ResolvTypedef(SyntaxNode.TypeSpecifier.TypedefTypeSpecifier ts, Scope currentScope) {
            // 名前表から識別子を探す
            var identifier = currentScope.FindIdentifier(ts.identifier);
            if (identifier == null) {
                return new CType.UndefinedType("未宣言の型");
            }

            // 識別子が型を示しているかチェック
            if (!(identifier is Scope.IdentifierValue.Type)) {
                return new CType.UndefinedType("識別子は型でない");
            }
            return (identifier as Scope.IdentifierValue.Type).type;
        }

        /// <summary>
        /// 列挙型を解析
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        private static CType ParseEnumSpecifier(SyntaxNode.TypeSpecifier.EnumSpecifier ts, Scope scope) {
            CType.TaggedType.EnumType et;

            var tag = ts.identifier;
            var anonymous = ts.anonymous;

            // 既に名前表に登録されているかチェック
            var ty = scope.FindTaggedType(tag);
            if (ty != null) {
                // 名前表に登録されているのでさらにチェック
                if (!(ty is CType.TaggedType.EnumType)) {
                    // 同名で別の要素が登録されている
                    return new CType.InvalidType("定義済みの識別子の再定義");
                }
                et = (CType.TaggedType.EnumType)ty;
                if (et.enumerators != null && ts.enumerators != null) {
                    // 登録されている要素も構文木側も、不完全な列挙型でない場合は型の再定義と見なす。
                    return new CType.InvalidType("型の再定義");
                }

                // 構文木側が不完全型の場合、名前表側の型を結果とする
                if (ts.enumerators == null) {
                    return ty;
                }

                // ここに到達した時点で、etは名前表に登録されている不完全型で、tsは完全型
            } else {
                // ここに到達した時点で、名前表に登録されていないので型を作成して名前表に登録
                et = new CType.TaggedType.EnumType(tag, anonymous, null);
                scope.AddTaggedType(tag, et);

                // 構文木側が不完全型の場合は本体の解析をしないで生成した型を結果とする
                if (ts.enumerators == null) {
                    return et;
                }
            }

            // 以降でメンバの解析を行って et にメンバを登録する。

            {
                var items = new List<Tuple<string, int>>();
                System.Diagnostics.Debug.Assert(ts.enumerators != null);
                foreach (var enumerator in ts.enumerators) {
                    var value = items.Count;
                    // メンバリストに登録
                    items.Add(Tuple.Create(enumerator.identifier, value));  // expressionを解析すること
                    // 識別子表にidentifierを登録
                    scope.AddIdentifier(enumerator.identifier, new Scope.IdentifierValue.EnumMember(et, value), -1);
                }
                et.enumerators = items;
                return et;
            }
        }


        /// <summary>
        /// 共用体を解析
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        private static CType ParseUnionSpecifier(SyntaxNode.TypeSpecifier.UnionSpecifier ts, Scope scope) {
            CType.TaggedType.UnionType et;

            var tag = ts.identifier;
            var anonymous = ts.anonymous;

            // 既に名前表に登録されているかチェック
            var ty = scope.FindTaggedType(tag);
            if (ty != null) {
                // 名前表に登録されているのでさらにチェック
                if (!(ty is CType.TaggedType.UnionType)) {
                    // 同名で別の要素が登録されている
                    return new CType.InvalidType("定義済みの識別子の再定義");
                }
                et = (CType.TaggedType.UnionType)ty;
                if (et.members != null && ts.struct_declarations != null) {
                    // 登録されている要素も構文木側も、不完全な共用体型でない場合は型の再定義と見なす。
                    return new CType.InvalidType("型の再定義");
                }

                // 構文木側が不完全型の場合、名前表側の型を結果とする
                if (ts.struct_declarations == null) {
                    return ty;
                }

                // ここに到達した時点で、etは名前表に登録されている不完全型で、tsは完全型
            } else {
                // ここに到達した時点で、名前表に登録されていないので型を作成して名前表に登録
                et = new CType.TaggedType.UnionType(tag, anonymous, null);
                scope.AddTaggedType(tag, et);

                // 構文木側が不完全型の場合は本体の解析をしないで生成した型を結果とする
                if (ts.struct_declarations == null) {
                    return et;
                }
            }

            // 以降でメンバの解析を行って et にメンバを登録する。
            {
                var items = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind, int>>();
                System.Diagnostics.Debug.Assert(ts.struct_declarations != null);
                var unionscope = new Scope(scope);
                foreach (var declaration in ts.struct_declarations) {
                    foreach (var member_declarator in declaration.struct_member_declarators) {
                        var ret = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>(); 
                            Parse(
                            declaration.specifier_qualifier_list.type_specifiers,
                            declaration.specifier_qualifier_list.type_qualifiers,
                            SyntaxNode.StorageClassSpecifierKind.none,
                            SyntaxNode.DeclarationSpecifiers.FuntionSpecifierKind.none,
                            member_declarator.declarator,
                            new SyntaxNode.Declaration[0],
                            unionscope,
                            ret
                        );
                        items.AddRange(ret.Select(x => Tuple.Create(x.Item1, x.Item2, x.Item3, 1))); // member_declarator.bitfield_exprがビットフィールドサイズ
                    }
                }
                et.members = items;
                return et;
            }
        }

        /// <summary>
        /// 構造体を解析
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        private static CType ParseStructSpecifier(SyntaxNode.TypeSpecifier.StructSpecifier ts, Scope scope) {
            CType.TaggedType.StructType et;

            var tag = ts.identifier;
            var anonymous = ts.anonymous;

            // 既に名前表に登録されているかチェック
            var ty = scope.FindTaggedType(tag);
            if (ty != null) {
                // 名前表に登録されているのでさらにチェック
                if (!(ty is CType.TaggedType.StructType)) {
                    // 同名で別の要素が登録されている
                    return new CType.InvalidType("定義済みの識別子の再定義");
                }
                et = (CType.TaggedType.StructType)ty;
                if (et.members != null && ts.struct_declarations != null) {
                    // 登録されている要素も構文木側も、不完全な構造体型でない場合は型の再定義と見なす。
                    return new CType.InvalidType("型の再定義");
                }

                // 構文木側が不完全型の場合、名前表側の型を結果とする
                if (ts.struct_declarations == null) {
                    return ty;
                }

                // ここに到達した時点で、etは名前表に登録されている不完全型で、tsは完全型
            } else {
                // ここに到達した時点で、名前表に登録されていないので型を作成して名前表に登録
                et = new CType.TaggedType.StructType(tag, anonymous, null);
                scope.AddTaggedType(tag, et);

                // 構文木側が不完全型の場合は本体の解析をしないで生成した型を結果とする
                if (ts.struct_declarations == null) {
                    return et;
                }
            }

            // 以降でメンバの解析を行って et にメンバを登録する。
            {
                var items = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind, int>>();
                System.Diagnostics.Debug.Assert(ts.struct_declarations != null);
                var unionscope = new Scope(scope);
                foreach (var declaration in ts.struct_declarations) {
                    foreach (var member_declarator in declaration.struct_member_declarators) {
                        var ret = new List<Tuple<string, CType, SyntaxNode.StorageClassSpecifierKind>>();
                        Parse(
                            declaration.specifier_qualifier_list.type_specifiers,
                            declaration.specifier_qualifier_list.type_qualifiers,
                            SyntaxNode.StorageClassSpecifierKind.none,
                            SyntaxNode.DeclarationSpecifiers.FuntionSpecifierKind.none,
                            member_declarator.declarator,
                            new SyntaxNode.Declaration[0],
                            unionscope,
                            ret
                        );
                        items.AddRange(ret.Select(x => Tuple.Create(x.Item1, x.Item2, x.Item3, 1))); // member_declarator.bitfield_exprがビットフィールドサイズ
                    }
                }
                et.members = items;
                return et;
            }
        }

        /// <summary>
        /// 基本型を解析
        /// </summary>
        /// <param name="type_specifiers"></param>
        /// <returns></returns>
        private static CType ParseStandardSpecifier(List<SyntaxNode.TypeSpecifier> type_specifiers, Scope currentScope) {
            CType.StandardType.TypeBit bits = 0;
            HashSet<SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind> standardtype_specs = new HashSet<SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind>();
            foreach (var type_specifier in type_specifiers) {
                if (type_specifier is SyntaxNode.TypeSpecifier.StandardTypeSpecifier) {
                    var ts = type_specifier as SyntaxNode.TypeSpecifier.StandardTypeSpecifier;
                    standardtype_specs.Add(ts.Kind);
                } else if (type_specifier is SyntaxNode.TypeSpecifier.TypedefTypeSpecifier) {
                    var ts = type_specifier as SyntaxNode.TypeSpecifier.TypedefTypeSpecifier;
                    // 名前表から識別子を探す
                    var ty = ResolvTypedef(ts, currentScope);

                    // 基本型以外はエラー
                    if (!(ty is CType.StandardType)) {
                        return ty;
                    }
                    // typedef済みをコピーしてしまう
                    bits |= (ty as CType.StandardType).bits;
                } else {
                    return new CType.InvalidType("組み合わせられない型指定子の組み合わせ");
                }
            }


            // 符号（排他）
            bits |= standardtype_specs.Contains(SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.signed_keyword) ? CType.StandardType.TypeBit._signed : 0;
            bits |= standardtype_specs.Contains(SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.unsigned_keyword) ? CType.StandardType.TypeBit._unsigned : 0;
            if ((bits & CType.StandardType.TypeBit._signBitMask) == CType.StandardType.TypeBit._signBitMask) {
                return new CType.InvalidType("組み合わせられない型指定子の組み合わせ");
            }

            // 長さ（排他）
            bits |= standardtype_specs.Contains(SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.long_keyword) ? CType.StandardType.TypeBit._long : 0;
            bits |= standardtype_specs.Contains(SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.short_keyword) ? CType.StandardType.TypeBit._short : 0;
            if ((bits & CType.StandardType.TypeBit._lenBitMask) == CType.StandardType.TypeBit._lenBitMask) {
                return new CType.InvalidType("組み合わせられない型指定子の組み合わせ");
            }

            // 値型（排他）
            bits |= standardtype_specs.Contains(SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.char_keyword) ? CType.StandardType.TypeBit._char : 0;
            bits |= standardtype_specs.Contains(SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.int_keyword) ? CType.StandardType.TypeBit._int : 0;
            bits |= standardtype_specs.Contains(SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.float_keyword) ? CType.StandardType.TypeBit._float : 0;
            bits |= standardtype_specs.Contains(SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.double_keyword) ? CType.StandardType.TypeBit._double : 0;
            bits |= standardtype_specs.Contains(SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.void_keyword) ? CType.StandardType.TypeBit._void : 0;
            bits |= standardtype_specs.Contains(SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.bool_keyword) ? CType.StandardType.TypeBit._bool : 0;
            bits |= standardtype_specs.Contains(SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.complex_keyword) ? CType.StandardType.TypeBit._complex : 0;
            bits |= standardtype_specs.Contains(SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.imaginary_keyword) ? CType.StandardType.TypeBit._imaginary : 0;
            bits |= standardtype_specs.Contains(SyntaxNode.TypeSpecifier.StandardTypeSpecifier.StandardTypeSpecifierKind.builtin_va_list_keyword) ? CType.StandardType.TypeBit._va_list : 0;


            uint uintbits = (uint)(bits & CType.StandardType.TypeBit._baseBitMask);
            if (uintbits.CountBit() > 1) {
                return new CType.InvalidType("組み合わせられない型指定子の組み合わせ");
            }

            return new CType.StandardType(bits);
        }

    }
    public static class Ext {
        public static int CountBit(this uint self) {
            return CountBit((ushort)(self >> 16)) + CountBit((ushort)(self & 0xFFFF));
        }
        public static int CountBit(this ushort self) {
            return CountBit((byte)(self >> 8)) + CountBit((byte)(self & 0xFF));
        }
        public static int CountBit(this byte self) {
            return CountBitTable[self];
        }
        private static byte[] CountBitTable = Enumerable.Range(0, 256).Select(x => (byte)Enumerable.Range(0, 8).Sum(y => ((x >> y) & 0x01))).ToArray();
    }


}
