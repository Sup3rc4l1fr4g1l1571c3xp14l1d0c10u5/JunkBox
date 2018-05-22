using System;
using System.Collections.Generic;
using System.Linq;
namespace AnsiCParser {

namespace DataType {
    /// <summary>
    ///     タグ付き型
    /// </summary>
    public abstract class TaggedType : CType {
        protected TaggedType(string tagName, bool isAnonymous) {
            TagName = tagName;
            IsAnonymous = isAnonymous;
        }

        public string TagName {
            get;
        }

        public bool IsAnonymous {
            get;
        }

        /// <summary>
        ///     構造体・共用体型
        /// </summary>
        public class StructUnionType : TaggedType {
            public enum StructOrUnion {
                Struct,
                Union
            }

            public StructUnionType(StructOrUnion kind, string tagName, bool isAnonymous) : base(tagName, isAnonymous) {
                Kind = kind;
            }

            public StructOrUnion Kind {
                get;
            }

            public List<MemberInfo> Members {
                get; internal set;
            }

            private int _size;

            public override CType Duplicate() {
                return new StructUnionType(Kind, TagName, IsAnonymous) {
                    Members = Members.Select(x => x.Duplicate()).ToList(),
                    _size = _size
                };
            }

            public override void Fixup(CType type) {
                for (var i = 0; i < Members.Count; i++) {
                    var member = Members[i];
                    if (member.Type is StubType) {
                        Members[i] = new MemberInfo(member.Ident, type, 0);
                    } else {
                        member.Type.Fixup(type);
                    }
                }
            }


            private class StructLayouter {
                private int AlignOf(CType type) {
                    switch (type.Sizeof()) {
                        case 1:
                            return 1;
                        case 2:
                            return 2;
                        default:
                            return 4;
                    }
                }

                private int PaddingOf(int value, int align) {
                    return (align - (value % align)) % align;
                }

                private bool IsEqualBitField(CType t1, CType t2) {
                    if (t1.IsBasicType() && t2.IsBasicType()) {
                        return (t1.Unwrap() as BasicType).Kind == (t1.Unwrap() as BasicType).Kind;
                    }
                    return false;
                }

                private List<MemberInfo> CreateBitPaddingMemberInfo(List<MemberInfo> result, CType ty, int bytepos, int bitpos, int bitsize) {
                    if (bytepos < 0 || bitsize <= 0 || ty.Sizeof() * 8 < bitpos + bitsize) {
                        throw new Exception("");
                    } else {
                        result.Add(new MemberInfo(null, new BitFieldType(null, ty, bitpos, bitsize), bytepos));
                        return result;
                    }
                }
                private List<MemberInfo> CreateBytePaddingMemberInfo(List<MemberInfo> result, int size, int bytepos) {
                    CType ty;
                    switch (size) {
                        case 1:
                            ty = CreateUnsignedChar();
                            result.Add(new MemberInfo(null, ty, bytepos));
                            break;
                        case 2:
                            ty = CreateUnsignedShortInt();
                            result.Add(new MemberInfo(null, ty, bytepos));
                            break;
                        case 3:
                            ty = CreateUnsignedChar();
                            result.Add(new MemberInfo(null, ty, bytepos));
                            ty = CreateUnsignedShortInt();
                            result.Add(new MemberInfo(null, ty, bytepos + 1));
                            break;
                        case 4:
                            ty = CreateUnsignedLongInt();
                            result.Add(new MemberInfo(null, ty, bytepos));
                            break;
                        default:
                            throw new Exception("");
                    }
                    return result;
                }
                private List<MemberInfo> CreateMemberInfo(List<MemberInfo> result, CType ty, Token ident, int bytepos, int bitpos, int bitsize) {
                    if (bitsize == -1) {
                        result.Add(new MemberInfo(ident, ty, bytepos));
                        return result;
                    } else {
                        result.Add(new MemberInfo(ident, new BitFieldType(ident, ty, bitpos, bitsize), bytepos));
                        return result;
                    }
                }
                public Tuple<int, List<MemberInfo>> Run(List<MemberInfo> members) {
                    var result = new List<MemberInfo>();

                    CType currentBitfieldType = null;
                    var currentBitfieldCapacity = 0;
                    var currentBitfieldSize = 0;
                    var currentBytePosition = 0;

                    foreach (var member in members) {
                        BitFieldType bft;

                        var size = member.Type.Sizeof();
                        var name = member.Ident;
                        var bit = member.Type.IsBitField(out bft) ? bft.BitWidth : -1;
                        var type = member.Type.IsBitField(out bft) ? bft.Type : member.Type;

                        // 今のバイト領域を終了するか？
                        if ((currentBitfieldType != null) && (bit == 0)) {
                            if ((currentBitfieldSize % 8) > 0) {
                                var pad = PaddingOf(currentBitfieldSize, 8);
                                result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, currentBitfieldSize, pad);
                                currentBitfieldSize += pad;
                                if (currentBitfieldCapacity != currentBitfieldSize) {
                                    currentBytePosition += currentBitfieldCapacity / 8;
                                    currentBitfieldType = null;
                                    currentBitfieldCapacity = 0;
                                    currentBitfieldSize = 0;
                                }
                                continue;
                            }
                        }

                        // 今のビットフィールド領域を終了するか？
                        if (((currentBitfieldType != null) && (!IsEqualBitField(type, currentBitfieldType))) || // 型が違う
                            ((currentBitfieldType != null) && (bit == -1))) { // ビットフィールドではない
                            // ビットフィールドの終了
                            if (currentBitfieldCapacity - currentBitfieldSize > 0) {
                                result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, currentBitfieldSize, (currentBitfieldCapacity - currentBitfieldSize));
                            }
                            currentBytePosition += currentBitfieldCapacity / 8;
                            currentBitfieldType = null;
                            currentBitfieldCapacity = 0;
                            currentBitfieldSize = 0;
                        } else if ((currentBitfieldType != null) && (bit > 0) && (currentBitfieldCapacity < currentBitfieldSize + bit)) { // 今の領域があふれる
                            result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, currentBitfieldSize, (currentBitfieldCapacity - currentBitfieldSize));
                            // ビットフィールドの終了ではなく、次のビットフィールド領域への移動なので先頭バイト位置を更新し、ビット位置をリセットするのみ
                            currentBytePosition += currentBitfieldCapacity / 8;
                            //current_bitfield_type = null;
                            //current_bitfield_capacity = 0;
                            currentBitfieldSize = 0;
                        }

                        // アライメント挿入が必要？
                        if (currentBitfieldType == null) {
                            var pad = PaddingOf(currentBytePosition, Math.Min(Settings.PackSize, AlignOf(type)));
                            if (pad > 0) {
                                result = CreateBytePaddingMemberInfo(result, pad, currentBytePosition);
                            }
                            currentBytePosition += pad;
                        }

                        if (bit == -1) {
                            // 普通のフィールド
                            result = CreateMemberInfo(result, type, name, currentBytePosition, 0, -1);
                            currentBytePosition += size;
                        } else if (bit > 0) {
                            // ビットフィールド
                            if (currentBitfieldType == null) {
                                currentBitfieldType = type;
                                currentBitfieldCapacity = size * 8;
                                currentBitfieldSize = 0; // 念のため
                            }
                            result = CreateMemberInfo(result, type, name, currentBytePosition, currentBitfieldSize, bit);
                            currentBitfieldSize += bit;
                        } else {
                            // 境界の処理には到達しないはず
                        }
                    }
                    // ビットフィールドが終端していないなら終端させる
                    if (currentBitfieldType != null) {
                        var pad = currentBitfieldCapacity - currentBitfieldSize;
                        if (pad > 0) {
                            result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, currentBitfieldSize, (currentBitfieldCapacity - currentBitfieldSize));
                        }
                        currentBytePosition += currentBitfieldCapacity / 8;
                        currentBitfieldType = null;
                        currentBitfieldCapacity = 0;
                        currentBitfieldSize = 0;
                    }

                    // 構造体のサイズをアライメントにそろえる
                    var structureAlignment = Settings.PackSize;
                    if ((currentBytePosition % structureAlignment) > 0) {
                        var pad = PaddingOf(currentBytePosition, structureAlignment);
                        result = CreateBytePaddingMemberInfo(result, pad, currentBytePosition);
                        currentBytePosition += pad;
                    }

                    return Tuple.Create(currentBytePosition, result);

                }
            }

            public void Build() {

                if (Kind == StructOrUnion.Struct) {
                    // 構造体型の場合
                    // ビットフィールド部分のレイアウトを決定
                    var layouter = new StructLayouter();
                    var ret = layouter.Run(Members);
                    _size = ret.Item1;
                    Members = ret.Item2;

                } else {
                    // 共用体型の場合は登録時のままでいい
                    _size = Members.Max(x => x.Type.Sizeof());

                }

            }

            public override int Sizeof() {
                return _size;
            }

            public class MemberInfo {
                public MemberInfo(Token ident, CType type, int offset/*, int bitOffset, int bitSize*/) {
                    Ident = ident;
                    Type = type;
                    Offset = offset;
                }

                public Token Ident {
                    get;
                }

                public CType Type {
                    get;
                }

                public int Offset {
                    get;
                }

                public MemberInfo Duplicate() {
                    return new MemberInfo(Ident, Type.Duplicate(), Offset);
                }
            }
        }

        /// <summary>
        ///     列挙型
        /// </summary>
        /// <remarks>
        ///  - 列挙型は，char，符号付き整数型又は符号無し整数型と適合する型とする。型の選択は，処理系定義とする。
        /// （つまり、sizeof(列挙型) は 処理系定義の値になる。）
        /// </remarks>
        public class EnumType : TaggedType {
            public EnumType(string tagName, bool isAnonymous) : base(tagName, isAnonymous) {
            }


            public override CType Duplicate() {
                var ret = new EnumType(TagName, IsAnonymous) {
                    Members = Members.Select<MemberInfo, MemberInfo>(x => x.Duplicate()).ToList()
                };
                return ret;
            }

            public List<MemberInfo> Members {
                get; set;
            }

            public override void Fixup(CType type) {
            }

            public override int Sizeof() {
                return Sizeof(BasicType.TypeKind.SignedInt);
            }

            /// <summary>
            ///     列挙型で宣言されている列挙定数
            /// </summary>
            /// <remarks>
            ///     6.4.4.3 列挙定数
            ///     意味規則
            ///     列挙定数として宣言された識別子は，型 int をもつ。
            /// </remarks>
            public class MemberInfo {
                public MemberInfo(EnumType parentType, Token ident, int value) {
                    ParentType = parentType;
                    Ident = ident;
                    Value = value;
                }

                public Token Ident {
                    get;
                }

                public EnumType ParentType {
                    get;
                }

                public int Value {
                    get;
                }

                public MemberInfo Duplicate() {
                    return new MemberInfo(ParentType, Ident, Value);
                }
            }
        }
    }
}

}