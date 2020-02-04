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
                /// <summary>
                /// フレキシブル配列メンバを持つことを示す
                /// </summary>
                public bool HasFlexibleArrayMember {
                    get; internal set;
                }

                private int _size;
                
                private int _align;

                /// <summary>
                /// フレキシブル配列メンバを持つ構造体を含むか判定
                /// </summary>
                /// <returns></returns>
                public override bool IsContainFlexibleArrayMemberStruct() {
                    return HasFlexibleArrayMember || Members.Any(x => x.Type.IsContainFlexibleArrayMemberStruct());
                }


                public override CType Duplicate() {
                    return new StructUnionType(Kind, TagName, IsAnonymous) {
                        Members = Members.Select(x => x.Duplicate()).ToList(),
                        _size = _size,
                        _align = _align,
                        HasFlexibleArrayMember = HasFlexibleArrayMember
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

                private static int PaddingOf(int value, int align) {
                    if (align == 0) {
                        return 0;
                    }
                    return (align - (value % align)) % align;
                }

                protected abstract class StructLayouter {
                    protected bool IsEqualBitField(CType t1, CType t2) {
                        if (t1.IsBasicType() && t2.IsBasicType()) {
                            return (t1.Unwrap() as BasicType).Sizeof() == (t2.Unwrap() as BasicType).Sizeof();  // 同じサイズなら同一視できる？
                            //return (t1.Unwrap() as BasicType).Kind == (t2.Unwrap() as BasicType).Kind;
                        }
                        return false;
                    }

                    protected List<MemberInfo> CreateBytePaddingMemberInfo(List<MemberInfo> result, int size, int bytepos) {
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
                            case 5:
                                ty = CreateUnsignedChar();
                                result.Add(new MemberInfo(null, ty, bytepos));
                                ty = CreateUnsignedLongInt();
                                result.Add(new MemberInfo(null, ty, bytepos + 1));
                                break;
                            case 6:
                                ty = CreateUnsignedShortInt();
                                result.Add(new MemberInfo(null, ty, bytepos));
                                ty = CreateUnsignedLongInt();
                                result.Add(new MemberInfo(null, ty, bytepos+2));
                                break;
                            case 7:
                                ty = CreateUnsignedChar();
                                result.Add(new MemberInfo(null, ty, bytepos));
                                ty = CreateUnsignedShortInt();
                                result.Add(new MemberInfo(null, ty, bytepos + 1));
                                ty = CreateUnsignedLongInt();
                                result.Add(new MemberInfo(null, ty, bytepos + 3));
                                break;
                            default:
                                throw new Exception("");
                        }
                        return result;
                    }
                    protected List<MemberInfo> CreateMemberInfo(List<MemberInfo> result, CType ty, Token ident, int bytepos, sbyte bitpos, sbyte bitsize) {
                        if (bitsize == -1) {
                            result.Add(new MemberInfo(ident, ty, bytepos));
                            return result;
                        } else {
                            result.Add(new MemberInfo(ident, new BitFieldType(ident, ty, bitpos, bitsize), bytepos));
                            return result;
                        }
                    }

                    /// <summary>
                    /// 構造体のレイアウト計算を実行
                    /// </summary>
                    /// <param name="members">メンバー情報</param>
                    /// <returns>(構造体のサイズ、構造体のアラインメント、メンバー情報（レイアウト情報が書き込まれた結果)</returns>
                    public abstract Tuple<int, int, List<MemberInfo>> Run(List<MemberInfo> members);
                }
                protected class StructLayouterGCC : StructLayouter
                {
                    private List<MemberInfo> CreateBitPaddingMemberInfo(List<MemberInfo> result, CType ty, int bytepos, sbyte bitpos, sbyte bitsize) {
                        if (bytepos < 0 || bitsize <= 0 /*|| ty.Sizeof() * 8 < bitpos + bitsize*/) {
                            throw new Exception("");
                        } else {
                            result.Add(new MemberInfo(null, new BitFieldType(null, ty, bitpos, bitsize), bytepos));
                            return result;
                        }
                    }

                    /// <summary>
                    /// gccっぽい構造体のレイアウト計算アルゴリズム
                    /// </summary>
                    /// <param name="members"></param>
                    /// <returns></returns>
                    public override Tuple<int, int, List<MemberInfo>> Run(List<MemberInfo> members) {
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
                            var typeAlign = Settings.PackSize == 0 ? type.Alignof() : Math.Min(Settings.PackSize, type.Alignof());
                            // 幅0のビットフィールドの挿入
                            if ((bit == 0)) {
                                if (currentBitfieldType != null) {
                                    // ビットフィールドを指定した型の境界に向けて切り上げ
                                    var pad = PaddingOf(currentBitfieldSize, typeAlign * 8);
                                    if (pad > 0) {
                                        System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                        System.Diagnostics.Debug.Assert(pad <= sbyte.MaxValue);
                                        if (currentBitfieldType.Sizeof() * 8 < currentBitfieldSize + pad) {
                                            var pad1 = (currentBitfieldSize + pad) - currentBitfieldType.Sizeof() * 8;
                                            var pad2 = pad - pad1;
                                            result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)pad2);
                                            var bytePosition = currentBytePosition + (currentBitfieldSize + pad2) / 8;
                                            while (pad1 >= 8) {
                                                result.Add(new MemberInfo(null, new BitFieldType(null, CType.CreateChar(), 0, 8), bytePosition));
                                                bytePosition += 1;
                                                pad1 -= 8;
                                            }
                                            System.Diagnostics.Debug.Assert(pad1 == 0);
                                        } else {
                                            result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)pad);
                                        }
                                        currentBytePosition += (currentBitfieldSize + pad) / 8;
                                        currentBitfieldType = null;
                                        currentBitfieldCapacity = 0;
                                        currentBitfieldSize = 0;
                                        continue;
                                    } else if (pad == 0) {
                                        // ちょうど境界上なのでパディングは挿入しない
                                        currentBitfieldType = null;
                                        currentBitfieldCapacity = 0;
                                        currentBitfieldSize = 0;
                                        continue;
                                    } else {
                                        // 負数はないわー
                                        throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "ビットフィールドのレイアウト計算中に負のビットサイズが出現しました。（本実装の誤りだと思います。）");
                                    }
                                } else {
                                    // バイト境界も同様に指定した型の境界に向けて切り上げ
                                    var pad = PaddingOf(currentBytePosition, typeAlign);
                                    if (pad > 0) {
                                        result = CreateBytePaddingMemberInfo(result, pad, currentBytePosition);
                                    }
                                    currentBytePosition += pad;
                                    currentBitfieldType = null;
                                    currentBitfieldCapacity = 0;
                                    currentBitfieldSize = 0;
                                    continue;
                                }
                            }

                            // 今のビットフィールド領域を終了するか？
                            if (currentBitfieldType != null) {
                                System.Diagnostics.Debug.Assert(bit != 0, "幅0のビットフィールドは既に処理済みのはず");
                                if ((bit == -1) // そもそも次のメンバはビットフィールドではない
                                // || (IsEqualBitField(type, currentBitfieldType) == false)  // 「今のビットフィールドと次のビットフィールドの型が違う」をGCCは見ていないっぽい
                                ) {
                                    var pad = PaddingOf(currentBitfieldSize, 8);
                                    if (pad > 0) {
                                        // 余り領域がある場合はパディングにする
                                        System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                        System.Diagnostics.Debug.Assert(pad <= sbyte.MaxValue);
                                        result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)(pad));
                                    }
                                    // ビットフィールドを終了させる
                                    currentBytePosition += (currentBitfieldSize + pad) / 8;
                                    currentBitfieldType = null;
                                    currentBitfieldCapacity = 0;
                                    currentBitfieldSize = 0;
                                } else {
                                    // ビットフィールドが連続している
                                    if (Settings.PackSize == 0) {
                                        // #pragma pack が未指定
                                        if ((bit > 0) && (currentBitfieldCapacity < currentBitfieldSize + bit)) {    // 今のビットフィールド領域の余りに次のビットフィールドが入らない
                                            var pad = PaddingOf(currentBitfieldSize, 8);
                                            if (pad > 0) {
                                                // 余り領域がある場合はパディングにする
                                                System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                                System.Diagnostics.Debug.Assert(pad <= sbyte.MaxValue);
                                                result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)(pad));
                                            }
                                            // ビットフィールドを終了させる
                                            currentBytePosition += (currentBitfieldSize + pad) / 8;
                                            currentBitfieldType = null;
                                            currentBitfieldCapacity = 0;
                                            currentBitfieldSize = 0;
                                        }
                                    } else {
                                        // #pragma pack(n) が指定済み
                                        // gcc は隙間なく全ビットを詰めるのでパディングも入れないしビットフィールドを終了させたりもしない
                                    }
                                }
                            }

                            // ビットフィールドが終了している場合、境界調整が必要か調べて境界調整を行う
                            if (currentBitfieldType == null) {
                                var pad = PaddingOf(currentBytePosition, typeAlign);
                                if (pad > 0) {
                                    result = CreateBytePaddingMemberInfo(result, pad, currentBytePosition);
                                }
                                currentBytePosition += pad;
                            }

                            // メンバーのフィールドを挿入する
                            if (bit == -1) {
                                // 普通のフィールド
                                result = CreateMemberInfo(result, type, name, currentBytePosition, 0, -1);
                                currentBytePosition += size;
                            } else if (bit > 0) {
                                if (Settings.PackSize == 0) {
                                    // ビットフィールド
                                    if (currentBitfieldType == null) {
                                        currentBitfieldType = type;
                                        currentBitfieldCapacity = size * 8;
                                        currentBitfieldSize = 0; // 念のため
                                    }
                                    System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                    System.Diagnostics.Debug.Assert(bit <= sbyte.MaxValue);
                                    result = CreateMemberInfo(result, type, name, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)bit);
                                    currentBitfieldSize += bit;
                                } else {
                                    // #pragma pack(n) が指定済み
                                    // gcc は隙間なく全ビットを詰めるのでややこしいことになる
                                    //     
                                    // 例: 
                                    //   #pragma pack(n) 
                                    //   struct X { char x:7; short y:12; char z:2;};
                                    //   このときのビットフィールドレイアウトはi386では以下のようになるため
                                    //   yはshort型なのに、3バイトの範囲に跨って存在することになる
                                    //   yxxxxxxx yyyyyyyy ???zzyyy
                                    //
                                    // ビットフィールドの型は short なので、型と実際に使うバイトサイズが一致しなくなる。
                                    // しょうがないのでコード生成器に頑張ってもらう前提で以下のようにする
                                    // 上記の場合 type=short byteoffset=0 bitoffset=7 とする
                                    if (currentBitfieldType == null) {
                                        currentBitfieldType = type;
                                        currentBitfieldCapacity = size * 8;
                                        currentBitfieldSize = 0; // 念のため
                                    }
                                    System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                    System.Diagnostics.Debug.Assert(bit <= sbyte.MaxValue);
                                    result = CreateMemberInfo(result, type, name, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)bit);
                                    currentBitfieldSize += bit;
                                }
                            } else {
                                // ここには来ないはず

                            }
                        }

                        // ビットフィールドが終端していないなら終端させる
                        if (currentBitfieldType != null) {
                            var pad = PaddingOf(currentBitfieldSize, 8);
                            if (pad > 0) {
                                System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                System.Diagnostics.Debug.Assert((currentBitfieldCapacity - currentBitfieldSize) <= sbyte.MaxValue);
                                result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)(pad));
                            }
                            currentBytePosition += (currentBitfieldSize + pad) / 8;
                            currentBitfieldType = null;
                            currentBitfieldCapacity = 0;
                            currentBitfieldSize = 0;
                        }

                        // 前提として規格書の以下の項目の知識が必要
                        // 
                        // 3.2  境界調整（alignment） 
                        // 特定の型のオブジェクトを特定のバイトアドレスの倍数のアドレスをもつ記憶域境界に割り付ける要求。
                        //
                        // 本題。
                        // 規格書には以下のような記載があるので、C言語における構造体メンバの境界調整（アラインメント）は処理系が規定することになっている。
                        //
                        // 6.7.2.1 構造体指定子及び共用体指定子 
                        // アドレス付け可能な記憶域単位の境界調整は，未規定とする。
                        //
                        // しかし、6.3.2.3 ポインタの部分の補足には以下のような指針が存在する
                        //
                        // 6.3.2.3 ポインタ
                        // (16) 一般には， 境界を正しく調整する という概念は推移的である。
                        //      すなわち，型 A のポインタが型 Bのポインタとして境界を正しく調整され，それが型 C のポインタとしても境界を正しく調整されているならば，型 A のポインタは型 C のポインタとして境界を正しく調整されている。
                        // 
                        // このことより、型Aを使う場合、以下のすべてで境界が正しく調整されているなければいけない。
                        // - 単体の変数として使う
                        // - 複合型中の要素として使う
                        // - ポインタで使われている
                        //
                        // また、6.5.3.4 sizeof演算子では以下のような言及がある
                        //
                        // 6.5.3.4 sizeof演算子
                        // (83) &*E は E と等価であり（E が空ポインタであっても）， &(E1[E2]) は ((E1) + (E2)) と等価である。
                        // 構造体型又は共用体型をもつオペランドに適用した場合の結果は，内部及び末尾の詰め物部分を含めたオブジェクトの総バイト数とする。
                        // 例2.  sizeof 演算子のもう一つの用途は，次のようにして配列の要素の個数を求めることである。
                        //       sizeof array / sizeof array[0]
                        //
                        // この指針より、境界調整についいて以下のことが導出できる
                        // - 境界を正しく調整された型の中にある要素は、境界を正しく調整されていなければならない。
                        // - 境界を正しく調整された結果はsizeof演算子の例2を満たさなければならない。
                        //
                        // このことより、構造体については以下の指針を満たす必要がある
                        // - 構造体の各メンバはそれぞれの境界調整に従う
                        // - 構造体全体の境界調整は構造体メンバの最大の境界調整となる
                        // - 上記より、構造体全体のサイズは構造体メンバの最大の境界調整の倍数となる
                        // 
                        // これは規格書には直接書いていないが、細かく読み取ると導出される。
                        var structureAlignment = Settings.PackSize != 0 ? Settings.PackSize : members.Where(x => { BitFieldType bft2; return !(x.Type.IsBitField(out bft2) && bft2.BitWidth == 0); }).Select(x => x.Type.Alignof()).Max();
                        if (structureAlignment != 0 && (currentBytePosition % structureAlignment) > 0) {
                            var pad = PaddingOf(currentBytePosition, structureAlignment);
                            result = CreateBytePaddingMemberInfo(result, pad, currentBytePosition);
                            currentBytePosition += pad;
                        }

                        return Tuple.Create(currentBytePosition, structureAlignment, result);

                    }
                }
                protected class StructLayouterMSVC : StructLayouter
                {
                    private List<MemberInfo> CreateBitPaddingMemberInfo(List<MemberInfo> result, CType ty, int bytepos, sbyte bitpos, sbyte bitsize) {
                        if (bytepos < 0 || bitsize <= 0 || ty.Sizeof() * 8 < bitpos + bitsize) {
                            throw new Exception("");
                        } else {
                            result.Add(new MemberInfo(null, new BitFieldType(null, ty, bitpos, bitsize), bytepos));
                            return result;
                        }
                    }

                    /// <summary>
                    /// VCっぽい構造体のレイアウト計算アルゴリズム
                    /// </summary>
                    /// <param name="members"></param>
                    /// <returns></returns>
                    public override  Tuple<int, int, List<MemberInfo>> Run(List<MemberInfo> members) {
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

                            // 幅0のビットフィールドの挿入
                            if ((bit == 0)) {
                                if (currentBitfieldType != null) {
                                    // 残りのビットフィールドを放棄するか？
                                    var pad = PaddingOf(currentBitfieldSize, (type.Sizeof() * 8));
                                    if (pad > 0) {
                                        System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                        System.Diagnostics.Debug.Assert(pad <= sbyte.MaxValue);
                                        result = CreateBitPaddingMemberInfo(result, type, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)pad);
                                        currentBytePosition += (currentBitfieldSize+pad) / 8;
                                        currentBitfieldType = null;
                                        currentBitfieldCapacity = 0;
                                        currentBitfieldSize = 0;
                                        continue;
                                    } else if (pad == 0) {
                                        // ちょうど境界上なのでパディングは挿入しない
                                        currentBitfieldType = null;
                                        currentBitfieldCapacity = 0;
                                        currentBitfieldSize = 0;
                                        continue;
                                    } else {
                                        // 負数はないわー
                                        throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "ビットフィールドのレイアウト計算中に負のビットサイズが出現しました。（本実装の誤りだと思います。）");
                                    }
                                } else {
                                    // 境界調整を強制する
                                    var pad = PaddingOf(currentBytePosition, Settings.PackSize == 0 ? type.Alignof() : Math.Min(Settings.PackSize, type.Alignof()));
                                    if (pad > 0) {
                                        result = CreateBytePaddingMemberInfo(result, pad, currentBytePosition);
                                    }
                                    currentBytePosition += pad;
                                    currentBitfieldType = null;
                                    currentBitfieldCapacity = 0;
                                    currentBitfieldSize = 0;
                                    continue;
                                }
                            }

                            // 今のビットフィールド領域を終了するか？
                            if (currentBitfieldType != null) {
                                if ((bit == -1) || // そもそも次のメンバはビットフィールドではない
                                    (IsEqualBitField(type, currentBitfieldType) == false)  // 今のビットフィールドと次のビットフィールドの型が違う
                                ) {
                                    if (currentBitfieldCapacity - currentBitfieldSize > 0) {
                                        // 余り領域がある場合はパディングにする
                                        System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                        System.Diagnostics.Debug.Assert(currentBitfieldCapacity - currentBitfieldSize <= sbyte.MaxValue);
                                        result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)(currentBitfieldCapacity - currentBitfieldSize));
                                    }
                                    // ビットフィールドを終了させる
                                    currentBytePosition += currentBitfieldCapacity / 8;
                                    currentBitfieldType = null;
                                    currentBitfieldCapacity = 0;
                                    currentBitfieldSize = 0;
                                } else if ((bit > 0) && (currentBitfieldCapacity < currentBitfieldSize + bit)) {    // 今のビットフィールド領域の余りに次のビットフィールドが入らない
                                    if (currentBitfieldCapacity - currentBitfieldSize > 0) {
                                        // 余り領域がある場合はパディングにする
                                        System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                        System.Diagnostics.Debug.Assert(currentBitfieldCapacity - currentBitfieldSize <= sbyte.MaxValue);
                                        result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)(currentBitfieldCapacity - currentBitfieldSize));
                                        // ビットフィールドの終了ではなく、次のビットフィールド領域への移動なので先頭バイト位置を更新し、ビット位置をリセットするのみ
                                        currentBytePosition += currentBitfieldCapacity / 8;
                                    }
                                    currentBitfieldSize = 0;
                                }
                            }

                            // ビットフィールドが終了している場合、境界調整が必要か調べて境界調整を行う
                            if (currentBitfieldType == null) {
                                var pad = PaddingOf(currentBytePosition, Settings.PackSize == 0 ? type.Alignof() : Math.Min(Settings.PackSize, type.Alignof()));
                                if (pad > 0) {
                                    result = CreateBytePaddingMemberInfo(result, pad, currentBytePosition);
                                }
                                currentBytePosition += pad;
                            }

                            // メンバーのフィールドを挿入する
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
                                System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                System.Diagnostics.Debug.Assert(bit <= sbyte.MaxValue);
                                result = CreateMemberInfo(result, type, name, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)bit);
                                currentBitfieldSize += bit;
                            } else {
                                // ここには来ないはず

                            }
                        }

                        // ビットフィールドが終端していないなら終端させる
                        if (currentBitfieldType != null) {
                            var pad = currentBitfieldCapacity - currentBitfieldSize;
                            if (pad > 0) {
                                System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                System.Diagnostics.Debug.Assert((currentBitfieldCapacity - currentBitfieldSize) <= sbyte.MaxValue);
                                result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)(currentBitfieldCapacity - currentBitfieldSize));
                            }
                            currentBytePosition += currentBitfieldCapacity / 8;
                            currentBitfieldType = null;
                            currentBitfieldCapacity = 0;
                            currentBitfieldSize = 0;
                        }

                        // 前提として規格書の以下の項目の知識が必要
                        // 
                        // 3.2  境界調整（alignment） 
                        // 特定の型のオブジェクトを特定のバイトアドレスの倍数のアドレスをもつ記憶域境界に割り付ける要求。
                        //
                        // 本題。
                        // 規格書には以下のような記載があるので、C言語における構造体メンバの境界調整（アラインメント）は処理系が規定することになっている。
                        //
                        // 6.7.2.1 構造体指定子及び共用体指定子 
                        // アドレス付け可能な記憶域単位の境界調整は，未規定とする。
                        //
                        // しかし、6.3.2.3 ポインタの部分の補足には以下のような指針が存在する
                        //
                        // 6.3.2.3 ポインタ
                        // (16) 一般には， 境界を正しく調整する という概念は推移的である。
                        //      すなわち，型 A のポインタが型 Bのポインタとして境界を正しく調整され，それが型 C のポインタとしても境界を正しく調整されているならば，型 A のポインタは型 C のポインタとして境界を正しく調整されている。
                        // 
                        // このことより、型Aを使う場合、以下のすべてで境界が正しく調整されているなければいけない。
                        // - 単体の変数として使う
                        // - 複合型中の要素として使う
                        // - ポインタで使われている
                        //
                        // また、6.5.3.4 sizeof演算子では以下のような言及がある
                        //
                        // 6.5.3.4 sizeof演算子
                        // (83) &*E は E と等価であり（E が空ポインタであっても）， &(E1[E2]) は ((E1) + (E2)) と等価である。
                        // 構造体型又は共用体型をもつオペランドに適用した場合の結果は，内部及び末尾の詰め物部分を含めたオブジェクトの総バイト数とする。
                        // 例2.  sizeof 演算子のもう一つの用途は，次のようにして配列の要素の個数を求めることである。
                        //       sizeof array / sizeof array[0]
                        //
                        // この指針より、境界調整についいて以下のことが導出できる
                        // - 境界を正しく調整された型の中にある要素は、境界を正しく調整されていなければならない。
                        // - 境界を正しく調整された結果はsizeof演算子の例2を満たさなければならない。
                        //
                        // このことより、構造体については以下の指針を満たす必要がある
                        // - 構造体の各メンバはそれぞれの境界調整に従う
                        // - 構造体全体の境界調整は構造体メンバの最大の境界調整となる
                        // - 上記より、構造体全体のサイズは構造体メンバの最大の境界調整の倍数となる
                        // 
                        // これは規格書には直接書いていないが、細かく読み取ると導出される。
                        var structureAlignment = members.Where(x => { BitFieldType bft2; return !(x.Type.IsBitField(out bft2) && bft2.BitWidth == 0); }).Select(x => x.Type.Alignof()).Max();
                        if (structureAlignment != 0 && (currentBytePosition % structureAlignment) > 0) {
                            var pad = PaddingOf(currentBytePosition, structureAlignment);
                            result = CreateBytePaddingMemberInfo(result, pad, currentBytePosition);
                            currentBytePosition += pad;
                        }

                        return Tuple.Create(currentBytePosition, structureAlignment, result);

                    }
                }

                private class UnionLayouter
                {
                    public Tuple<int, int> Run(List<MemberInfo> members) {
                        return Run_LikeGCC(members);
                        //return Run_LikeMSVC(members);
                    }
                    public Tuple<int, int> Run_LikeGCC(List<MemberInfo> members) {
                        var size = members.Max(x => {
                            BitFieldType bft;
                            if (x.Type.IsBitField(out bft)) {
                                return (bft.BitOffset + bft.BitWidth + 7) / 8;
                            } else {
                                return x.Type.Sizeof();
                            }
                        });
                        var align = members.Max(x => {
                            BitFieldType bft;
                            if (x.Type.IsBitField(out bft) && bft.BitWidth == 0) {
                                return 0;
                            } else {
                                return x.Type.Alignof();
                            }
                        });
                        return Tuple.Create(size, align);
                    }
                    public Tuple<int,int> Run_LikeMSVC(List<MemberInfo> members) {
                        var size = members.Max(x => {
                            BitFieldType bft;
                            if (x.Type.IsBitField(out bft)) {
                                return (bft.BitOffset + bft.BitWidth + 7) / 8;
                            } else {
                                return x.Type.Sizeof();
                            }
                        });
                        var align = members.Max(x => {
                            BitFieldType bft;
                            if (x.Type.IsBitField(out bft) && bft.BitWidth == 0) {
                                return 0;
                            } else {
                                return x.Type.Alignof();
                            }
                        });
                        var pad = PaddingOf(size, align);
                        size += pad;
                        return Tuple.Create(size, align);
                    }
                }

                public void Build() {

                    if (Kind == StructOrUnion.Struct) {
                        // 構造体型の場合
                        var layouter = new StructLayouterGCC();
                        var ret = layouter.Run(Members);
                        if (this.HasFlexibleArrayMember) {
                            _size = ret.Item1 - Members.Last().Type.Sizeof();   // フレキシブル配列メンバは最後の要素の型を無視する
                            _align = ret.Item2;
                            Members = ret.Item3;
                        } else {
                            _size = ret.Item1;
                            _align = ret.Item2;
                            Members = ret.Item3;
                        }
                    } else {
                        // 共用体型のの場合
                        var layouter = new UnionLayouter();
                        var ret = layouter.Run(Members);
                        _size = ret.Item1;
                        _align = ret.Item2;
                    }

                }

                /// <summary>
                /// 型のバイトサイズを取得（ビットフィールドの場合、元の型のサイズ）
                /// </summary>
                /// <returns></returns>
                public override int Sizeof() {
                    return _size;
                }

                /// <summary>
                /// 型の境界調整（アラインメント）を取得（ビットフィールドの場合、元の型のアラインメント）
                /// </summary>
                /// <returns></returns>
                public override int Alignof() {
                    return _align;
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

                /// <summary>
                /// 型のバイトサイズを取得（ビットフィールドの場合、元の型のサイズ）
                /// </summary>
                /// <returns></returns>
                public override int Sizeof() {
                    return Sizeof(BasicType.TypeKind.SignedInt);
                }

                /// <summary>
                /// 型の境界調整（アラインメント）を取得（ビットフィールドの場合、元の型のアラインメント）
                /// </summary>
                /// <returns></returns>
                public override int Alignof() {
                    return Alignof(BasicType.TypeKind.SignedInt);
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
