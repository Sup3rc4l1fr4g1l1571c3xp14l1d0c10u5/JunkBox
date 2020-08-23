using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace AnsiCParser {

    namespace DataType {
        /// <summary>
        /// タグ付き型
        /// </summary>
        public abstract class TaggedType : CType {
            protected TaggedType(string tagName, bool isAnonymous) {
                TagName = tagName;
                IsAnonymous = isAnonymous;
            }

            /// <summary>
            /// タグ名
            /// </summary>
            public string TagName {
                get;
            }

            /// <summary>
            /// 匿名型ならば真 
            /// </summary>
            public bool IsAnonymous {
                get;
            }

            /// <summary>
            /// 構造体・共用体型
            /// </summary>
            public class StructUnionType : TaggedType {
                public enum StructOrUnion {
                    Struct,
                    Union
                }

                public StructUnionType(StructOrUnion kind, string tagName, bool isAnonymous, int packSize, int alignSize) : base(tagName, isAnonymous) {
                    Kind = kind;
                    PackSize = packSize;
                    AlignSize = alignSize;
                }

                public StructOrUnion Kind {
                    get;
                }

                public List<MemberInfo> Members {
                    get; private set;
                }
                /// <summary>
                /// フレキシブル配列メンバを持つことを示す
                /// </summary>
                public bool HasFlexibleArrayMember {
                    get; private set;
                }

                /// <summary>
                /// 型サイズ
                /// </summary>
                private int _size;

                /// <summary>
                /// 型アライメント
                /// </summary>
                public int AlignSize { get; private set;  }

                /// <summary>
                /// パックサイズ（アライメント設定サイズ）
                /// </summary>
                public int PackSize { get; }

                /// <summary>
                /// フレキシブル配列メンバを持つ構造体を含むか判定
                /// </summary>
                /// <returns></returns>
                public override bool IsContainFlexibleArrayMemberStruct() {
                    return HasFlexibleArrayMember || Members.Any(x => x.Type.IsContainFlexibleArrayMemberStruct());
                }


                public override CType Duplicate() {
                    return new StructUnionType(Kind, TagName, IsAnonymous, PackSize, AlignSize) {
                        Members = Members.Select(x => x.Duplicate()).ToList(),
                        _size = _size,
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

                /// <summary>
                /// 構造体のレイアウト算出
                /// </summary>
                protected abstract class StructLayoutBase {
                    public static int PaddingOf(int value, int align) {
                        if (align == 0) {
                            return 0;
                        }
                        return (align - (value % align)) % align;
                    }


                    /// <summary>
                    /// 同じビットフィールドに入れることが出来る型と見なせるか？
                    /// </summary>
                    /// <param name="t1"></param>
                    /// <param name="t2"></param>
                    /// <returns></returns>
                    protected bool IsEqualBitField(CType t1, CType t2) {
                        if (t1.IsBasicType() && t2.IsBasicType()) {
                            // 同じサイズなら同一視
                            return ((BasicType)t1.Unwrap()).SizeOf() == ((BasicType)t2.Unwrap()).SizeOf();
                            //return (t1.Unwrap() as BasicType).Kind == (t2.Unwrap() as BasicType).Kind;
                        }
                        return false;
                    }

                    /// <summary>
                    /// バイト単位のパディングをメンバリストに追加
                    /// </summary>
                    /// <param name="result">メンバリスト</param>
                    /// <param name="size"></param>
                    /// <param name="bytePos"></param>
                    /// <returns></returns>
                    protected List<MemberInfo> CreateBytePaddingMemberInfo(List<MemberInfo> result, int size, int bytePos) {
                        CType ty;
                            switch (size) {
                                case 1:
                                    ty = CreateUnsignedChar();
                                    result.Add(new MemberInfo(null, ty, bytePos));
                                    break;
                                case 2:
                                    ty = CreateUnsignedShortInt();
                                    result.Add(new MemberInfo(null, ty, bytePos));
                                    break;
                                case 3:
                                    ty = CreateUnsignedChar();
                                    result.Add(new MemberInfo(null, ty, bytePos));
                                    ty = CreateUnsignedShortInt();
                                    result.Add(new MemberInfo(null, ty, bytePos + 1));
                                    break;
                                case 4:
                                    ty = CreateUnsignedLongInt();
                                    result.Add(new MemberInfo(null, ty, bytePos));
                                    break;
                                case 5:
                                    ty = CreateUnsignedChar();
                                    result.Add(new MemberInfo(null, ty, bytePos));
                                    ty = CreateUnsignedLongInt();
                                    result.Add(new MemberInfo(null, ty, bytePos + 1));
                                    break;
                                case 6:
                                    ty = CreateUnsignedShortInt();
                                    result.Add(new MemberInfo(null, ty, bytePos));
                                    ty = CreateUnsignedLongInt();
                                    result.Add(new MemberInfo(null, ty, bytePos + 2));
                                    break;
                                case 7:
                                    ty = CreateUnsignedChar();
                                    result.Add(new MemberInfo(null, ty, bytePos));
                                    ty = CreateUnsignedShortInt();
                                    result.Add(new MemberInfo(null, ty, bytePos + 1));
                                    ty = CreateUnsignedLongInt();
                                    result.Add(new MemberInfo(null, ty, bytePos + 3));
                                    break;
                                default:
                                    throw new Exception("");
                            }
                        return result;
                    }

                    /// <summary>
                    /// メンバ情報を追加
                    /// </summary>
                    /// <param name="result"></param>
                    /// <param name="ty"></param>
                    /// <param name="ident"></param>
                    /// <param name="bytePos"></param>
                    /// <param name="bitPos"></param>
                    /// <param name="bitSize"></param>
                    /// <returns></returns>
                    protected List<MemberInfo> CreateMemberInfo(List<MemberInfo> result, CType ty, Token ident, int bytePos, sbyte bitPos, sbyte bitSize) {
                        if (bitSize == -1) {
                            result.Add(new MemberInfo(ident, ty, bytePos));
                            return result;
                        } else {
                            result.Add(new MemberInfo(ident, new BitFieldType(ident, ty, bitPos, bitSize), bytePos));
                            return result;
                        }
                    }

                    /// <summary>
                    /// 構造体のレイアウト計算を実行
                    /// </summary>
                    /// <param name="members">メンバー情報</param>
                    /// <returns>(構造体のサイズ、構造体のアラインメント、メンバー情報（レイアウト情報が書き込まれた結果)</returns>
                    public abstract Tuple<int, int, List<MemberInfo>> Run(List<MemberInfo> members, int packSize, int alignSize);
                }

                /// <summary>
                /// PCCっぽい構造体レイアウト計算アルゴリズム
                /// </summary>
                protected class StructLayoutPcc : StructLayoutBase {
                    private List<MemberInfo> CreateBitPaddingMemberInfo(List<MemberInfo> result, CType ty, int bytePos, sbyte bitPos, sbyte bitSize) {
                        if (bytePos < 0 || bitSize <= 0) {
                            throw new Exception("");
                        } else {
                            //result.Add(new MemberInfo(null, new BitFieldType(null, ty, bitPos, bitSize), bytePos));
                            return result;
                        }
                    }

                    /// <summary>
                    /// 構造体のレイアウト計算を実行
                    /// </summary>
                    /// <param name="members"></param>
                    /// <returns></returns>
                    public override Tuple<int, int, List<MemberInfo>> Run(List<MemberInfo> members, int packSize, int alignSize) {
                        var result = new List<MemberInfo>();
                        BitFieldType bft;
                        CType et;

                        var currentBitPos = 0;
                        var packBitSize = packSize * 8;
                        var structureBitAlignment = alignSize * 8;

                        for (var i= 0; i< members.Count; i++) {
                            var member = members[i];
                            var memberIdent = member.Ident;
                            var memberBitAlign = (packBitSize == 0) ? member.Type.AlignOf() * 8 : member.Type.IsBitField(out bft) && bft.BitWidth == 0 ? member.Type.AlignOf() * 8 : packBitSize;
                            var memberTypeBitSize =(member.Type.IsIncompleteType() && member.Type.IsArrayType(out et)) ? et.SizeOf() * 8 : (member.Type.SizeOf() * 8);
                            var memberBitSize = member.Type.IsBitField(out bft) ? bft.BitWidth : (member.Type.IsIncompleteType() && member.Type.IsArrayType(out et)) ? et.SizeOf() * 8 : (member.Type.SizeOf() * 8);

                            // 前方向で最も近いアライメント境界
                            var headBitPos = currentBitPos - (currentBitPos % memberBitAlign);
                            // 前方向で最も近いアライメント境界からの使用済みビット数
                            var usedBitSize = currentBitPos - headBitPos;

                            if (packBitSize == 0) {
                                if ((memberTypeBitSize < usedBitSize + memberBitSize) || (member.Type.IsBitField() && memberBitSize == 0)) {
                                    // 空き部分にメンバーを入れることができないので、次のアライメント境界まで切り上げる
                                    //result = CreateBitPaddingMemberInfo(result, member.Type, headBitPos / 8, (sbyte)usedBitSize, (sbyte)freeBitSize);
                                    currentBitPos = headBitPos + memberBitAlign;
                                    headBitPos = currentBitPos;
                                    usedBitSize = 0;
                                }
                            } else {
                                // padding != 0の時はビットフィールドではない型は必ずアライメントに整列。ビットフィールドは詰める。
                                if ((member.Type.IsBitField() == false && (currentBitPos != headBitPos)) || (member.Type.IsBitField() && memberBitSize == 0)) {
                                    // 空き部分にメンバーを入れることができないので、次のアライメント境界まで切り上げる
                                    //result = CreateBitPaddingMemberInfo(result, member.Type, headBitPos / 8, (sbyte)usedBitSize, (sbyte)freeBitSize);
                                    currentBitPos = headBitPos + memberBitAlign;
                                    headBitPos = currentBitPos;
                                    usedBitSize = 0;
                                }
                            }
                            if (memberBitSize != 0) {
                                // メンバーを追加
                                if (member.Type.IsBitField()) {
                                    result = CreateMemberInfo(result, member.Type, memberIdent, headBitPos / 8, (sbyte)usedBitSize, (sbyte)memberBitSize);
                                } else {
                                    result = CreateMemberInfo(result, member.Type, memberIdent, headBitPos / 8, -1, -1);
                                }

                                if (i+1 == members.Count && member.Type.IsIncompleteType() && member.Type.IsArrayType()) {
                                    // 最後のメンバーが不完全型配列の場合はそのメンバーのサイズを追加しない
                                } else {
                                    currentBitPos += memberBitSize;
                                }

                                // アライメント算出
                                structureBitAlignment = Math.Max(structureBitAlignment, memberBitAlign);
                            }
                        }

                        // 末尾の隙間を埋める
                        var paddingBitSize = currentBitPos % 8;
                        if (paddingBitSize > 0) {
                            //result = CreateBitPaddingMemberInfo(result, CType.CreateUnsignedChar(), currentBitPos / 8, (sbyte)(currentBitPos%8), (sbyte)(8-paddingBitSize));
                            currentBitPos += (8 - paddingBitSize);
                        }

                        // アライメントにサイズをそろえる
                        if ((currentBitPos % structureBitAlignment) > 0) {
                            var pad = PaddingOf(currentBitPos, structureBitAlignment);
                            if ((pad % 8) > 0) { 
                                //result = CreateBitPaddingMemberInfo(result, CType.CreateUnsignedChar(), currentBitPos / 8, (sbyte)(currentBitPos % 8), (sbyte)(pad % 8));
                                currentBitPos += (pad % 8);
                                pad -= (pad % 8);
                            }
                            if ((pad / 8) > 0) {
                                //result = CreateBytePaddingMemberInfo(result, pad/8, currentBitPos/8);
                            }
                            currentBitPos += pad;
                        }

                        return Tuple.Create(currentBitPos / 8, structureBitAlignment / 8, result);


                    }
                }
                /// <summary>
                /// GCCっぽい構造体レイアウト計算アルゴリズム
                /// </summary>
                protected class StructLayoutGcc : StructLayoutBase {
                    private List<MemberInfo> CreateBitPaddingMemberInfo(List<MemberInfo> result, CType ty, int bytePos, sbyte bitPos, sbyte bitSize) {
                        if (bytePos < 0 || bitSize <= 0) {
                            throw new Exception("");
                        } else {
                            result.Add(new MemberInfo(null, new BitFieldType(null, ty, bitPos, bitSize), bytePos));
                            return result;
                        }
                    }

                    /// <summary>
                    /// 構造体のレイアウト計算を実行
                    /// </summary>
                    /// <param name="members"></param>
                    /// <returns></returns>
                    public override Tuple<int, int, List<MemberInfo>> Run(List<MemberInfo> members, int packSize, int alignSize) {
                        var result = new List<MemberInfo>();

                        CType currentBitfieldType = null;
                        var currentBitfieldCapacity = 0;
                        var currentBitfieldSize = 0;
                        var currentBytePosition = 0;

                        foreach (var member in members) {
                            BitFieldType bft;

                            var memberSize = member.Type.SizeOf();
                            var memberIdent = member.Ident;
                            var memberBitWidth = member.Type.IsBitField(out bft) ? bft.BitWidth : -1;
                            var memberType = member.Type.IsBitField(out bft) ? bft.Type : member.Type;
                            var memberTypeAlign = packSize == 0 ? memberType.AlignOf() : Math.Min(packSize, memberType.AlignOf());

                            // 幅0のビットフィールドの挿入の場合
                            if ((memberBitWidth == 0)) {
                                if (currentBitfieldType != null) {
                                    // ビットフィールドを終了させる場合：
                                    // ビットフィールドを指定した型の境界に整列させるために必要なパディングサイズを算出
                                    var currentBitfieldPaddingSize = PaddingOf(currentBitfieldSize, memberTypeAlign * 8);
                                    if (currentBitfieldPaddingSize > 0) {
                                        // 型の境界に整列させるためにはパディング挿入が必要

                                        System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                        System.Diagnostics.Debug.Assert(currentBitfieldPaddingSize <= sbyte.MaxValue);

                                        // パディングを挿入すると、ビットフィールド元の型のサイズを超える場合
                                        if (currentBitfieldType.SizeOf() * 8 < currentBitfieldSize + currentBitfieldPaddingSize) {
                                            // 溢れるビットサイズを求める。
                                            var overflowBitSize = (currentBitfieldSize + currentBitfieldPaddingSize) - currentBitfieldType.SizeOf() * 8;

                                            // 今のビットフィールドを終了させる分だけパディングを挿入
                                            var currentBitfieldClosingSize = currentBitfieldPaddingSize - overflowBitSize;
                                            result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)currentBitfieldClosingSize);

                                            // 挿入後のバイト位置を求める。
                                            var bytePosition = currentBytePosition + (currentBitfieldSize + currentBitfieldClosingSize) / 8;

                                            // 溢れた分は無名のバイトメンバとして挿入
                                            System.Diagnostics.Debug.Assert((overflowBitSize % 8) == 0);
                                            while (overflowBitSize >= 8) {
                                                result.Add(new MemberInfo(null, new BitFieldType(null, CType.CreateChar(), 0, 8), bytePosition));
                                                bytePosition += 1;
                                                overflowBitSize -= 8;
                                            }
                                            System.Diagnostics.Debug.Assert(overflowBitSize == 0);
                                        } else {
                                            // 溢れが発生しないので、パディングとして必要なサイズを挿入
                                            result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)currentBitfieldPaddingSize);
                                        }
                                        currentBytePosition += (currentBitfieldSize + currentBitfieldPaddingSize) / 8;
                                        currentBitfieldType = null;
                                        currentBitfieldCapacity = 0;
                                        currentBitfieldSize = 0;
                                        continue;
                                    } else if (currentBitfieldPaddingSize == 0) {
                                        // ちょうど型の境界上なのでパディングは挿入しない
                                        currentBitfieldType = null;
                                        currentBitfieldCapacity = 0;
                                        currentBitfieldSize = 0;
                                        continue;
                                    } else {
                                        // 負数はないわー
                                        throw new CompilerException.InternalErrorException(Location.Empty, Location.Empty, "ビットフィールドのレイアウト計算中に負のビットサイズが出現しました。（本実装の誤りだと思います。）");
                                    }
                                } else {
                                    // 非ビットフィールドを終了させる場合：
                                    // バイト境界も同様に指定した型の境界に向けて切り上げ
                                    var currentMemberPaddingSize = PaddingOf(currentBytePosition, memberTypeAlign);
                                    if (currentMemberPaddingSize > 0) {
                                        result = CreateBytePaddingMemberInfo(result, currentMemberPaddingSize, currentBytePosition);
                                    }
                                    currentBytePosition += currentMemberPaddingSize;
                                    currentBitfieldType = null;
                                    currentBitfieldCapacity = 0;
                                    currentBitfieldSize = 0;
                                    continue;
                                }
                                throw new Exception("ここに到達することはない。");
                            }

                            // 今のビットフィールド領域を終了するか？
                            if (currentBitfieldType != null) {
                                System.Diagnostics.Debug.Assert(memberBitWidth != 0, "幅0のビットフィールドは既に処理済みのはず");
                                if ((memberBitWidth == -1) // そもそも次のメンバはビットフィールドではない
                                // || (IsEqualBitField(type, currentBitfieldType) == false)  // 「今のビットフィールドと次のビットフィールドの型が違う」をGCCは見ていないっぽい
                                ) {
                                    var currentBitfieldPaddingSize = PaddingOf(currentBitfieldSize, 8);
                                    if (currentBitfieldPaddingSize > 0) {
                                        // 余り領域がある場合はパディングにする
                                        System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                        System.Diagnostics.Debug.Assert(currentBitfieldPaddingSize <= sbyte.MaxValue);
                                        result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)(currentBitfieldPaddingSize));
                                    }
                                    // ビットフィールドを終了させる
                                    currentBytePosition += (currentBitfieldSize + currentBitfieldPaddingSize) / 8;
                                    currentBitfieldType = null;
                                    currentBitfieldCapacity = 0;
                                    currentBitfieldSize = 0;
                                } else {
                                    // ビットフィールドが連続している
                                    if (packSize == 0) {
                                        // #pragma pack が未指定
                                        if ((currentBytePosition % currentBitfieldType.AlignOf()) == 0 && (currentBytePosition % memberTypeAlign) == 0) {
                                            // アラインメントが揃っている。
                                            var maxSize = Math.Max(currentBitfieldType.SizeOf(), memberType.SizeOf());
                                            if (currentBitfieldSize + memberBitWidth <= maxSize * 8) {
                                                // 未使用領域に押し込めるのでそう仕向けるように現在のビットフィールド情報を書き換える。
                                                currentBitfieldType = currentBitfieldType.SizeOf() > memberType.SizeOf() ? currentBitfieldType : memberType;
                                                currentBitfieldCapacity = maxSize * 8;
                                                goto okay;
                                            }
                                        }

                                        var currentBitfieldPaddingSize = PaddingOf(currentBitfieldSize, 8);
                                        if (currentBitfieldPaddingSize > 0) {
                                            // 余り領域がある場合はパディングにする
                                            System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                            System.Diagnostics.Debug.Assert(currentBitfieldPaddingSize <= sbyte.MaxValue);
                                            result = CreateBitPaddingMemberInfo(result, currentBitfieldType, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)(currentBitfieldPaddingSize));
                                        }
                                        // ビットフィールドを終了させる
                                        currentBytePosition += (currentBitfieldSize + currentBitfieldPaddingSize) / 8;
                                        currentBitfieldType = null;
                                        currentBitfieldCapacity = 0;
                                        currentBitfieldSize = 0;
                                        okay:;
                                    } else {
                                        // #pragma pack(n) が指定済み
                                        // gcc は隙間なく全ビットを詰めるのでパディングも入れないしビットフィールドを終了させたりもしない
                                    }
                                }
                            }

                            // ビットフィールドが終了している場合、境界調整が必要か調べて境界調整を行う
                            if (currentBitfieldType == null) {
                                var pad = PaddingOf(currentBytePosition, memberTypeAlign);
                                if (pad > 0) {
                                    result = CreateBytePaddingMemberInfo(result, pad, currentBytePosition);
                                }
                                currentBytePosition += pad;
                            }

                            // メンバーのフィールドを挿入する
                            if (memberBitWidth == -1) {
                                // 普通のフィールド
                                result = CreateMemberInfo(result, memberType, memberIdent, currentBytePosition, 0, -1);
                                currentBytePosition += memberSize;
                            } else if (memberBitWidth > 0) {
                                if (packSize == 0) {
                                    // ビットフィールド
                                    if (currentBitfieldType == null) {
                                        currentBitfieldType = memberType;
                                        currentBitfieldCapacity = memberSize * 8;
                                        currentBitfieldSize = 0; // 念のため
                                    }
                                    System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                    System.Diagnostics.Debug.Assert(memberBitWidth <= sbyte.MaxValue);
                                    result = CreateMemberInfo(result, memberType, memberIdent, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)memberBitWidth);
                                    currentBitfieldSize += memberBitWidth;
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
                                        currentBitfieldType = memberType;
                                        currentBitfieldCapacity = memberSize * 8;
                                        currentBitfieldSize = 0; // 念のため
                                    }
                                    System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                    System.Diagnostics.Debug.Assert(memberBitWidth <= sbyte.MaxValue);
                                    result = CreateMemberInfo(result, memberType, memberIdent, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)memberBitWidth);
                                    currentBitfieldSize += memberBitWidth;
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
                        // この指針より、境界調整について以下のことが導出できる
                        // - 境界を正しく調整された型の中にある要素は、境界を正しく調整されていなければならない。
                        // - 境界を正しく調整された結果はsizeof演算子の例2を満たさなければならない。
                        //
                        // このことより、構造体については以下の指針を満たす必要がある
                        // - 構造体の各メンバはそれぞれの境界調整に従う
                        // - 構造体全体の境界調整は構造体メンバの最大の境界調整となる
                        // - 上記より、構造体全体のサイズは構造体メンバの最大の境界調整の倍数となる
                        // 
                        // これは規格書には直接書いていないが、細かく読み取ると導出される。
                        var structureAlignment = alignSize != 0 ? alignSize : members.Where(x => { BitFieldType bft2; return !(x.Type.IsBitField(out bft2) && bft2.BitWidth == 0); }).Select(x => x.Type.AlignOf()).Max();
                        if (structureAlignment != 0 && (currentBytePosition % structureAlignment) > 0) {
                            var pad = PaddingOf(currentBytePosition, structureAlignment);
                            result = CreateBytePaddingMemberInfo(result, pad, currentBytePosition);
                            currentBytePosition += pad;
                        }

                        return Tuple.Create(currentBytePosition, structureAlignment, result);

                    }
                }

                /// <summary>
                /// MSVCっぽい構造体レイアウト計算アルゴリズム
                /// </summary>
                protected class StructLayoutMsvc : StructLayoutBase {
                    private List<MemberInfo> CreateBitPaddingMemberInfo(List<MemberInfo> result, CType ty, int bytepos, sbyte bitpos, sbyte bitsize) {
                        if (bytepos < 0 || bitsize <= 0 || ty.SizeOf() * 8 < bitpos + bitsize) {
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
                    public override Tuple<int, int, List<MemberInfo>> Run(List<MemberInfo> members, int packSize, int alignSize) {
                        var result = new List<MemberInfo>();

                        CType currentBitfieldType = null;
                        var currentBitfieldCapacity = 0;
                        var currentBitfieldSize = 0;
                        var currentBytePosition = 0;

                        foreach (var member in members) {
                            BitFieldType bft;

                            var size = member.Type.SizeOf();
                            var name = member.Ident;
                            var bit = member.Type.IsBitField(out bft) ? bft.BitWidth : -1;
                            var type = member.Type.IsBitField(out bft) ? bft.Type : member.Type;

                            // 幅0のビットフィールドの挿入
                            if ((bit == 0)) {
                                if (currentBitfieldType != null) {
                                    // 残りのビットフィールドを放棄するか？
                                    var pad = PaddingOf(currentBitfieldSize, (type.SizeOf() * 8));
                                    if (pad > 0) {
                                        System.Diagnostics.Debug.Assert(currentBitfieldSize <= sbyte.MaxValue);
                                        System.Diagnostics.Debug.Assert(pad <= sbyte.MaxValue);
                                        result = CreateBitPaddingMemberInfo(result, type, currentBytePosition, (sbyte)currentBitfieldSize, (sbyte)pad);
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
                                    // 境界調整を強制する
                                    var pad = PaddingOf(currentBytePosition, packSize == 0 ? type.AlignOf() : Math.Min(packSize, type.AlignOf()));
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
                                var pad = PaddingOf(currentBytePosition, packSize == 0 ? type.AlignOf() : Math.Min(packSize, type.AlignOf()));
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
                        var structureAlignment = (alignSize != 0) ? alignSize : members.Where(x => { BitFieldType bft2; return !(x.Type.IsBitField(out bft2) && bft2.BitWidth == 0); }).Select(x => x.Type.AlignOf()).Max();
                        if (structureAlignment != 0 && (currentBytePosition % structureAlignment) > 0) {
                            var pad = PaddingOf(currentBytePosition, structureAlignment);
                            result = CreateBytePaddingMemberInfo(result, pad, currentBytePosition);
                            currentBytePosition += pad;
                        }

                        return Tuple.Create(currentBytePosition, structureAlignment, result);

                    }
                }

                /// <summary>
                /// 共用体のレイアウト算出
                /// </summary>
                protected abstract class UnionLayoutBase {
                    public abstract Tuple<int, int> Run(List<MemberInfo> members, int alignSize);
                }

                /// <summary>
                /// PCCっぽい共用体レイアウト計算アルゴリズム
                /// </summary>
                protected class UnionLayoutPcc : UnionLayoutBase {
                    public override Tuple<int, int> Run(List<MemberInfo> members, int alignSize) {
                        var size = members.Max(x => {
                            BitFieldType bft;
                            if (x.Type.IsBitField(out bft)) {
                                return (bft.BitOffset + bft.BitWidth + 7) / 8;
                            } else {
                                return x.Type.SizeOf();
                            }
                        });
                        var align = alignSize != 0 ? alignSize : members.Max(x => {
                            BitFieldType bft;
                            if (x.Type.IsBitField(out bft) && bft.BitWidth == 0) {
                                return 0;
                            } else {
                                return x.Type.AlignOf();
                            }
                        });
                        var pad = StructLayoutBase.PaddingOf(size, align);
                        size += pad;
                        return Tuple.Create(size, align);
                    }
                }

                ///// <summary>
                ///// GCCっぽい共用体レイアウト計算アルゴリズム
                ///// </summary>
                //protected class UnionLayoutGcc : UnionLayoutBase {
                //    public override Tuple<int, int> Run(List<MemberInfo> members) {
                //        var size = members.Max(x => {
                //            BitFieldType bft;
                //            if (x.Type.IsBitField(out bft)) {
                //                return (bft.BitOffset + bft.BitWidth + 7) / 8;
                //            } else {
                //                return x.Type.SizeOf();
                //            }
                //        });
                //        var align = members.Max(x => {
                //            BitFieldType bft;
                //            if (x.Type.IsBitField(out bft) && bft.BitWidth == 0) {
                //                return 0;
                //            } else {
                //                return x.Type.AlignOf();
                //            }
                //        });
                //        return Tuple.Create(size, align);
                //    }
                //}

                /// <summary>
                /// MSVCっぽい共用体レイアウト計算アルゴリズム
                /// </summary>
                protected class UnionLayoutMscv : UnionLayoutBase {
                    public override Tuple<int, int> Run(List<MemberInfo> members, int alignSize) {
                        var size = members.Max(x => {
                            BitFieldType bft;
                            if (x.Type.IsBitField(out bft)) {
                                return (bft.BitOffset + bft.BitWidth + 7) / 8;
                            } else {
                                return x.Type.SizeOf();
                            }
                        });
                        var align = alignSize != 0 ? alignSize : members.Max(x => {
                            BitFieldType bft;
                            if (x.Type.IsBitField(out bft) && bft.BitWidth == 0) {
                                return 0;
                            } else {
                                return x.Type.AlignOf();
                            }
                        });
                        var pad = StructLayoutBase.PaddingOf(size, align);
                        size += pad;
                        return Tuple.Create(size, align);
                    }
                }

                /// <summary>
                /// メンバレイアウトの計算を行い、構造体情報を設定する
                /// </summary>
                public void Build(List<MemberInfo> members, bool hasFlexibleArrayMember) {
                    this.HasFlexibleArrayMember = hasFlexibleArrayMember;
                    this.Members = members;

                    if (Kind == StructOrUnion.Struct) {
                        // 構造体型の場合
                        var layout = new StructLayoutPcc();
                        var ret = layout.Run(Members, PackSize, AlignSize);
                        _size = ret.Item1;
                        AlignSize = ret.Item2;
                        Members = ret.Item3;
                    } else {
                        // 共用体型のの場合
                        var layout = new UnionLayoutPcc();
                        var ret = layout.Run(Members, AlignSize);
                        _size = ret.Item1;
                        AlignSize = ret.Item2;
                    }

                }

                /// <summary>
                /// 型のバイトサイズを取得（ビットフィールドの場合、元の型のサイズ）
                /// </summary>
                /// <returns></returns>
                public override int SizeOf() {
                    return _size;
                }

                /// <summary>
                /// 型の境界調整（アラインメント）を取得（ビットフィールドの場合、元の型のアラインメント）
                /// </summary>
                /// <returns></returns>
                public override int AlignOf() {
                    return AlignSize;
                }

                /// <summary>
                /// 構造体・共用体のメンバ情報
                /// </summary>
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
            /// 列挙型
            /// </summary>
            /// <remarks>
            ///  - 列挙型は，char，符号付き整数型又は符号無し整数型と適合する型とする。型の選択は，処理系定義とする。
            ///    しかし，その型は列挙型のすべてのメンバの値を表現できなければならない。列挙子の宣言の並びを終了するまでは列挙型は不完全型とする。
            ///    (処理系はすべての列挙定数が指定された後で整数型の選択を行うことができる。)
            /// （つまりsizeof(列挙型) は 処理系定義の値になる。また、char型なので符号の有無は処理系定義である。）
            /// </remarks>
            public class EnumType : TaggedType {
                public EnumType(string tagName, bool isAnonymous) : base(tagName, isAnonymous) {
                }


                public override CType Duplicate() {
                    var ret = new EnumType(TagName, IsAnonymous) {
                        Members = Members.Select<MemberInfo, MemberInfo>(x => x.Duplicate()).ToList()
                    };
                    ret.SelectedType = SelectedType.Duplicate() as BasicType;
                    return ret;
                }

                public List<MemberInfo> Members {
                    get; set;
                }

                public override void Fixup(CType type) {
                }

                public BasicType SelectedType {
                    get; private set;
                }

                public void UpdateSelectedType() {
                    SelectedType = Members.Any(x => x.Value < 0) ? CreateSignedInt() : CreateUnsignedInt();
                }

                /// <summary>
                /// 型のバイトサイズを取得（ビットフィールドの場合、元の型のサイズ）
                /// </summary>
                /// <returns></returns>
                public override int SizeOf() {
                    return SizeOf(BasicType.TypeKind.SignedInt);
                }

                /// <summary>
                /// 型の境界調整（アラインメント）を取得（ビットフィールドの場合、元の型のアラインメント）
                /// </summary>
                /// <returns></returns>
                public override int AlignOf() {
                    return AlignOf(BasicType.TypeKind.SignedInt);
                }

                /// <summary>
                /// 列挙型で宣言されている列挙定数
                /// </summary>
                /// <remarks>
                /// 6.4.4.3 列挙定数
                /// 意味規則
                /// 列挙定数として宣言された識別子は，型 int をもつ。
                /// ※ただし、列挙型の型は処理系定義となるため、int型とは限らない。(6.7.2.2 列挙型指定子の意味規則第２パラグラフより)
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
