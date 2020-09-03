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

                public StructUnionType(StructOrUnion kind, string tagName, bool isAnonymous, int packSize, int alignSize, LayoutMode layout) : base(tagName, isAnonymous) {
                    Kind = kind;
                    PackSize = packSize;
                    AlignSize = alignSize;
                    Layout = layout;
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
                public int AlignSize { get; private set; }

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
                    return new StructUnionType(Kind, TagName, IsAnonymous, PackSize, AlignSize, Layout) {
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
                /// 構造体のレイアウト算出アルゴリズム
                /// </summary>
                public enum LayoutMode {
                    /// <summary>
                    /// GCC互換
                    /// </summary>
                    PCC,
                    /// <summary>
                    /// MSVC互換
                    /// </summary>
                    MSVC,
                    /// <summary>
                    /// デフォルト
                    /// </summary>
                    Default = LayoutMode.PCC,
                }

                /// <summary>
                /// 構造体のレイアウト算出アルゴリズム
                /// </summary>
                public LayoutMode Layout { get; }

                /// <summary>
                /// 構造体のレイアウト算出
                /// </summary>
                protected abstract class LayoutBase {
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
                    public abstract Tuple<int, int, List<MemberInfo>> StructLayout(List<MemberInfo> members, int packSize, int alignSize);
                    public abstract Tuple<int, int> UnionLayout(List<MemberInfo> members, int alignSize);

                }

                /// <summary>
                /// PCCっぽい構造体レイアウト計算アルゴリズム
                /// </summary>
                protected class LayoutPcc : LayoutBase {
                    /// <summary>
                    /// 構造体のレイアウト計算を実行（http://jkz.wtf/bit-field-packing-in-gcc-and-clang）
                    /// </summary>
                    /// <param name="members"></param>
                    /// <returns></returns>
                    public override Tuple<int, int, List<MemberInfo>> StructLayout(List<MemberInfo> members, int packSize, int alignSize) {
                        var result = new List<MemberInfo>();
                        BitFieldType bft;
                        CType et;

                        var currentBitPos = 0;
                        var structAlign = 0;

                        for (var i = 0; i < members.Count; i++) {
                            var member = members[i];

                            if (member.Type.IsBitField(out bft) && bft.BitWidth == 0) {
                                // 長さ0のビットフィールドメンバ（境界）の場合

                                // 前方で最も近いアライメント境界を探す
                                int alignBitPos;
                                var overflow = currentBitPos % (member.Type.AlignOf() * 8);
                                if (overflow == 0) {
                                    // 現在地点（currentBitPos）がちょうどアライメント境界
                                    alignBitPos = currentBitPos;
                                } else {
                                    // 直前のアライメント境界を求める
                                    alignBitPos = currentBitPos - overflow;
                                }

                                // 位置を進める
                                currentBitPos = alignBitPos + member.Type.SizeOf() * 8;
                                continue;
                            } else if (member.Type.IsBitField(out bft) && bft.BitWidth != 0) {
                                // 長さ非0のビットフィールドメンバの場合
                                if (packSize == 0) {
                                    // packが0の場合は前方で最も近いアライメント境界にそろえる
                                    // 前方で最も近いアライメント境界を探す
                                    var overflow = currentBitPos % (member.Type.AlignOf() * 8);
                                    if (overflow == 0) {
                                        // 現在地点（currentBitPos）がちょうどアライメント境界なのでそのまま書き込む
                                        var alignBitPos = currentBitPos;
                                        result = CreateMemberInfo(result, member.Type, member.Ident, alignBitPos / 8, (sbyte)overflow, (sbyte)bft.BitWidth);
                                        currentBitPos = alignBitPos + bft.BitWidth;
                                        // 構造体のアライメントを求める
                                        structAlign = Math.Max(structAlign, member.Type.AlignOf());
                                        continue;
                                    } else {
                                        // 直前のアライメント境界を求める
                                        var alignBitPos = currentBitPos - overflow;
                                        // 次のアライメント境界までの間にメンバを追加できるかチェック
                                        if (member.Type.SizeOf() * 8 - overflow >= bft.BitWidth) {
                                            // 追加できるのでそのまま追加
                                            result = CreateMemberInfo(result, member.Type, member.Ident, alignBitPos / 8, (sbyte)overflow, (sbyte)bft.BitWidth);
                                            currentBitPos = currentBitPos + bft.BitWidth;
                                            // 構造体のアライメントを求める
                                            structAlign = Math.Max(structAlign, member.Type.AlignOf());
                                            continue;
                                        } else {
                                            // 追加できないので次のアライメント境界からスタートにする
                                            alignBitPos = alignBitPos + (member.Type.AlignOf() * 8);
                                            overflow = 0;
                                            result = CreateMemberInfo(result, member.Type, member.Ident, alignBitPos / 8, (sbyte)overflow, (sbyte)bft.BitWidth);
                                            currentBitPos = alignBitPos + bft.BitWidth;
                                            // 構造体のアライメントを求める
                                            structAlign = Math.Max(structAlign, member.Type.AlignOf());
                                            continue;
                                        }
                                    }
                                } else {
                                    // アライメント境界を無視して書き込む
                                    var alignBitPos = currentBitPos;
                                    result = CreateMemberInfo(result, member.Type, member.Ident, alignBitPos / 8, (sbyte)(alignBitPos % 8), (sbyte)bft.BitWidth);
                                    currentBitPos = alignBitPos + bft.BitWidth;
                                    // 構造体のアライメントを求める
                                    structAlign = Math.Max(structAlign, member.Type.AlignOf());
                                    continue;
                                }
                            } else {
                                // 非ビットフィールドメンバの場合
                                if (packSize == 0) {
                                    // 個々の型の最も近いアライメント境界にそろえる
                                    // 前方で最も近いアライメント境界を探す
                                    var overflow = currentBitPos % (member.Type.AlignOf() * 8);
                                    if (overflow == 0) {
                                        // 現在地点（currentBitPos）がちょうどアライメント境界なのでそのまま書き込む
                                        var alignBitPos = currentBitPos;
                                        if (member.Type.IsIncompleteType() && member.Type.IsArrayType(out et)) {
                                            // 不完全型配列の場合は、要素をちょうど一つ持つ配列として扱う。
                                            result = CreateMemberInfo(result, member.Type, member.Ident, alignBitPos / 8, -1, -1);
                                            currentBitPos = alignBitPos + et.SizeOf() * 8;
                                            // 構造体のアライメントを求める
                                            structAlign = Math.Max(structAlign, member.Type.AlignOf());
                                            continue;
                                        } else {
                                            // それ以外の場合は普通に扱う
                                            result = CreateMemberInfo(result, member.Type, member.Ident, alignBitPos / 8, -1, -1);
                                            currentBitPos = alignBitPos + member.Type.SizeOf() * 8;
                                            // 構造体のアライメントを求める
                                            structAlign = Math.Max(structAlign, member.Type.AlignOf());
                                            continue;
                                        }
                                    } else {
                                        // 追加できないので次のアライメント境界からスタートにする
                                        // 直前のアライメント境界を求める
                                        var alignBitPos = currentBitPos - overflow;
                                        alignBitPos = alignBitPos + (member.Type.AlignOf() * 8);
                                        if (member.Type.IsIncompleteType() && member.Type.IsArrayType(out et)) {
                                            // 不完全型配列の場合は、要素をちょうど一つ持つ配列として扱う。
                                            result = CreateMemberInfo(result, member.Type, member.Ident, alignBitPos / 8, -1, -1);
                                            currentBitPos = alignBitPos + et.SizeOf() * 8;
                                            // 構造体のアライメントを求める
                                            structAlign = Math.Max(structAlign, member.Type.AlignOf());
                                            continue;
                                        } else {
                                            // それ以外の場合は普通に扱う
                                            result = CreateMemberInfo(result, member.Type, member.Ident, alignBitPos / 8, -1, -1);
                                            currentBitPos = alignBitPos + member.Type.SizeOf() * 8;
                                            // 構造体のアライメントを求める
                                            structAlign = Math.Max(structAlign, member.Type.AlignOf());
                                            continue;
                                        }
                                    }
                                } else {
                                    // 指定されたアライメントに最も近いアライメント境界にそろえる
                                    // 前方で最も近いアライメント境界を探す
                                    var overflow = currentBitPos % (packSize * 8);
                                    if (overflow == 0) {
                                        // 現在地点（currentBitPos）がちょうどアライメント境界なのでそのまま書き込む
                                        var alignBitPos = currentBitPos;
                                        if (member.Type.IsIncompleteType() && member.Type.IsArrayType(out et)) {
                                            // 不完全型配列の場合は、要素をちょうど一つ持つ配列として扱う。
                                            result = CreateMemberInfo(result, member.Type, member.Ident, alignBitPos / 8, -1, -1);
                                            currentBitPos = alignBitPos + et.SizeOf() * 8;
                                            // 構造体のアライメントを求める
                                            structAlign = Math.Max(structAlign, packSize);
                                            continue;
                                        } else {
                                            // それ以外の場合は普通に扱う
                                            result = CreateMemberInfo(result, member.Type, member.Ident, alignBitPos / 8, -1, -1);
                                            currentBitPos = alignBitPos + member.Type.SizeOf() * 8;
                                            // 構造体のアライメントを求める
                                            structAlign = Math.Max(structAlign, packSize);
                                            continue;
                                        }
                                    } else {
                                        // 追加できないので次のアライメント境界からスタートにする
                                        // 直前のアライメント境界を求める
                                        var alignBitPos = currentBitPos - overflow;
                                        alignBitPos = alignBitPos + (packSize * 8);
                                        if (member.Type.IsIncompleteType() && member.Type.IsArrayType(out et)) {
                                            // 不完全型配列の場合は、要素をちょうど一つ持つ配列として扱う。
                                            result = CreateMemberInfo(result, member.Type, member.Ident, alignBitPos / 8, -1, -1);
                                            currentBitPos = alignBitPos + et.SizeOf() * 8;
                                            // 構造体のアライメントを求める
                                            structAlign = Math.Max(structAlign, packSize);
                                            continue;
                                        } else {
                                            // それ以外の場合は普通に扱う
                                            result = CreateMemberInfo(result, member.Type, member.Ident, alignBitPos / 8, -1, -1);
                                            currentBitPos = alignBitPos + member.Type.SizeOf() * 8;
                                            // 構造体のアライメントを求める
                                            structAlign = Math.Max(structAlign, packSize);
                                            continue;
                                        }
                                    }

                                }
                            }
                        }

                        if (alignSize != 0) {
                            structAlign = alignSize;
                        } else if (packSize != 0 && alignSize == 0) {
                            structAlign = 1;
                        }

                        {
                            // 構造体全体のサイズをアライメント境界にそろえる
                            var overflow = currentBitPos % (structAlign * 8);
                            if (overflow > 0) {
                                currentBitPos += (structAlign * 8 - overflow);
                            }
                        }

                        return Tuple.Create(currentBitPos / 8, structAlign, result);


                    }
                    /// <summary>
                    /// 共用体のレイアウト計算を実行
                    /// </summary>
                    /// <param name="members"></param>
                    /// <param name="alignSize"></param>
                    /// <returns></returns>
                    public override Tuple<int, int> UnionLayout(List<MemberInfo> members, int alignSize) {
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
                        var pad = LayoutBase.PaddingOf(size, align);
                        size += pad;
                        return Tuple.Create(size, align);
                    }
                }

                /// <summary>
                /// MSVCっぽい構造体レイアウト計算アルゴリズム
                /// </summary>
                protected class LayoutMsvc : LayoutBase {
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
                    public override Tuple<int, int, List<MemberInfo>> StructLayout(List<MemberInfo> members, int packSize, int alignSize) {
                        var result = new List<MemberInfo>();

                        CType currentBitfieldType = null;
                        var currentBitfieldStartBit = 0;
                        var currentBitPos = 0;
                        var structAlign = 0;

                        foreach (var member in members) {
                            BitFieldType bft;

                            var size = member.Type.SizeOf();
                            var name = member.Ident;
                            var bit = member.Type.IsBitField(out bft) ? bft.BitWidth : -1;
                            var type = member.Type.IsBitField(out bft) ? bft.Type : member.Type;

                            if (member.Type.IsBitField(out bft) && bft.BitWidth == 0) {
                                // 幅0のビットフィールド

                                // 境界を挿入する
                                if (currentBitfieldType != null) {
                                    // ビットフィールドの宣言直後なら現在位置をビットフィールド終端の次の位置にする
                                    currentBitPos = currentBitfieldStartBit + (currentBitfieldType.SizeOf() * 8);
                                }
                                // 幅0のビットフィールドの型でアライメントにそろえる
                                {
                                    var alignByte = packSize == 0 ? bft.Type.AlignOf() : Math.Min(packSize, bft.Type.AlignOf()); // https://docs.microsoft.com/ja-jp/cpp/c-language/storage-and-alignment-of-structures?view=vs-2019 より
                                    var overflow = currentBitPos % (alignByte * 8);
                                    if (overflow > 0) {
                                        currentBitPos += (alignByte * 8) - overflow;
                                    }
                                }

                                // 情報を更新
                                currentBitfieldType = null;
                                currentBitfieldStartBit = currentBitPos;
                                continue;
                            } else if (member.Type.IsBitField(out bft) && bft.BitWidth != 0) {
                                // 幅0ではないビットフィールド

                                // 構造体アライメントを更新
                                structAlign = Math.Max(structAlign, member.Type.AlignOf());

                                if (currentBitfieldType != null && IsEqualBitField(bft.Type, currentBitfieldType) == false) {
                                    // ビットフィールドの型が違う場合

                                    // ビットフィールドの宣言直後なら現在位置をビットフィールド終端の次の位置にする
                                    currentBitPos = currentBitfieldStartBit + (currentBitfieldType.SizeOf() * 8);

                                    // ビットフィールドの型でアライメントにそろえる
                                    {
                                        var alignByte = packSize == 0 ? bft.Type.AlignOf() : Math.Min(packSize, bft.Type.AlignOf()); // https://docs.microsoft.com/ja-jp/cpp/c-language/storage-and-alignment-of-structures?view=vs-2019 より
                                        var overflow = currentBitPos % (alignByte * 8);
                                        if (overflow > 0) {
                                            currentBitPos += (alignByte * 8) - overflow;
                                        }
                                    }

                                    // ビットフィールドを設定
                                    result = CreateMemberInfo(result, member.Type, member.Ident, currentBitPos / 8, (sbyte)0, (sbyte)bft.BitWidth);

                                    // 情報を更新
                                    currentBitfieldType = bft.Type;
                                    currentBitfieldStartBit = currentBitPos;
                                    currentBitPos += bft.BitWidth;

                                    // ビットフィールド末尾まで満たされたならビットフィールドを終わらせる
                                    if (currentBitfieldStartBit + bft.Type.SizeOf() * 8 == currentBitPos) {
                                        currentBitfieldType = null;
                                        currentBitfieldStartBit = currentBitPos;
                                    }

                                    continue;
                                } else if (currentBitfieldType != null && IsEqualBitField(bft.Type, currentBitfieldType) == true && ((currentBitfieldType.SizeOf() * 8) - (currentBitPos - currentBitfieldStartBit) < bft.BitWidth)) {
                                    // ビットフィールドの型が同一だが、追記したらアライメント境界を跨ぐ場合

                                    // ビットフィールドの宣言直後なら現在位置をビットフィールド終端の次の位置にする
                                    currentBitPos = currentBitfieldStartBit + (currentBitfieldType.SizeOf() * 8);

                                    // ビットフィールドの型でアライメントにそろえる
                                    {
                                        var alignByte = packSize == 0 ? bft.Type.AlignOf() : Math.Min(packSize, bft.Type.AlignOf()); // https://docs.microsoft.com/ja-jp/cpp/c-language/storage-and-alignment-of-structures?view=vs-2019 より
                                        var overflow = currentBitPos % (alignByte * 8);
                                        if (overflow > 0) {
                                            currentBitPos += (alignByte * 8) - overflow;
                                        }
                                    }

                                    // ビットフィールドを設定
                                    result = CreateMemberInfo(result, member.Type, member.Ident, currentBitPos / 8, (sbyte)0, (sbyte)bft.BitWidth);

                                    // 情報を更新
                                    currentBitfieldType = bft.Type;
                                    currentBitfieldStartBit = currentBitPos;
                                    currentBitPos += bft.BitWidth;

                                    // ビットフィールド末尾まで満たされたならビットフィールドを終わらせる
                                    if (currentBitfieldStartBit + bft.Type.SizeOf() * 8 == currentBitPos) {
                                        currentBitfieldType = null;
                                        currentBitfieldStartBit = currentBitPos;
                                    }

                                    continue;
                                } else if (currentBitfieldType != null && IsEqualBitField(bft.Type, currentBitfieldType) == true && ((currentBitfieldType.SizeOf() * 8) - (currentBitPos - currentBitfieldStartBit) >= bft.BitWidth)) {
                                    // ビットフィールドの型が同一で、追記してもアライメント境界を跨がない場合

                                    // ビットフィールドを設定
                                    result = CreateMemberInfo(result, member.Type, member.Ident, currentBitfieldStartBit / 8, (sbyte)(currentBitPos - currentBitfieldStartBit), (sbyte)bft.BitWidth);

                                    // 情報を更新
                                    currentBitfieldType = bft.Type;
                                    currentBitfieldStartBit = currentBitfieldStartBit;
                                    currentBitPos += bft.BitWidth;

                                    // ビットフィールド末尾まで満たされたならビットフィールドを終わらせる
                                    if (currentBitfieldStartBit + bft.Type.SizeOf() * 8 == currentBitPos) {
                                        currentBitfieldType = null;
                                        currentBitfieldStartBit = currentBitPos;
                                    }

                                    continue;
                                } else if (currentBitfieldType == null) {
                                    // 直前がビットフィールドではない場合

                                    // ビットフィールドの型でアライメントにそろえる
                                    {
                                        var alignByte = packSize == 0 ? bft.Type.AlignOf() : Math.Min(packSize, bft.Type.AlignOf()); // https://docs.microsoft.com/ja-jp/cpp/c-language/storage-and-alignment-of-structures?view=vs-2019 より
                                        var overflow = currentBitPos % (alignByte * 8);
                                        if (overflow > 0) {
                                            currentBitPos += (alignByte * 8) - overflow;
                                        }
                                    }

                                    // ビットフィールドを設定
                                    result = CreateMemberInfo(result, member.Type, member.Ident, currentBitPos / 8, (sbyte)0, (sbyte)bft.BitWidth);

                                    // 情報を更新
                                    currentBitfieldType = bft.Type;
                                    currentBitfieldStartBit = currentBitPos;
                                    currentBitPos += bft.BitWidth;

                                    // ビットフィールド末尾まで満たされたならビットフィールドを終わらせる
                                    if (currentBitfieldStartBit + bft.Type.SizeOf() * 8 == currentBitPos) {
                                        currentBitfieldType = null;
                                        currentBitfieldStartBit = currentBitPos;
                                    }

                                    continue;

                                } else {
                                    throw new Exception("来ないはず");
                                }
                            } else if (member.Type.IsBitField(out bft) == false) {
                                // ビットフィールドではない。

                                // 構造体アライメントを更新
                                structAlign = Math.Max(structAlign, member.Type.AlignOf());

                                // ビットフィールドが終わっていない場合、ビットフィールドを終わらせる
                                if (currentBitfieldType != null) {
                                    // ビットフィールドの型でアライメントにそろえる
                                    //var overflow = currentBitPos % (currentBitfieldType.AlignOf() * 8);
                                    //if (overflow > 0) {
                                    //    currentBitPos += (currentBitfieldType.AlignOf() * 8) - overflow;
                                    //}
                                    currentBitPos = currentBitfieldStartBit + (currentBitfieldType.AlignOf() * 8);
                                    // 情報を更新
                                    currentBitfieldType = null;
                                    currentBitfieldStartBit = currentBitPos;
                                }

                                // フィールドの型でアライメントにそろえる
                                {
                                    var alignByte = packSize == 0 ? member.Type.AlignOf() : Math.Min(packSize, member.Type.AlignOf()); // https://docs.microsoft.com/ja-jp/cpp/c-language/storage-and-alignment-of-structures?view=vs-2019 より
                                    var overflow = currentBitPos % (alignByte * 8);
                                    if (overflow > 0) {
                                        currentBitPos += (alignByte * 8) - overflow;
                                        currentBitfieldStartBit = currentBitPos;
                                    }
                                }

                                CType et;
                                if (member.Type.IsIncompleteType() && member.Type.IsArrayType(out et)) {
                                    // 不完全型配列の場合は、要素をちょうど一つ持つ配列として扱う

                                    // フィールドを設定
                                    result = CreateMemberInfo(result, member.Type, member.Ident, currentBitfieldStartBit / 8, -1, -1);

                                    // 情報を更新
                                    currentBitfieldType = null;
                                    currentBitPos += et.SizeOf() * 8;
                                    currentBitfieldStartBit = currentBitPos;
                                    continue;
                                } else {
                                    // それ以外の場合は普通に扱う

                                    // フィールドを設定
                                    result = CreateMemberInfo(result, member.Type, member.Ident, currentBitfieldStartBit / 8, -1, -1);

                                    // 情報を更新
                                    currentBitfieldType = null;
                                    currentBitPos += member.Type.SizeOf() * 8;
                                    currentBitfieldStartBit = currentBitPos;
                                    continue;
                                }


                            } else {
                                throw new Exception("来ないはず");
                            }
                        }


                        // ビットフィールドが終わっていない場合、ビットフィールドを終わらせる
                        if (currentBitfieldType != null) {
                            //// ビットフィールドの型でアライメントにそろえる
                            //var overflow = currentBitPos % (currentBitfieldType.AlignOf() * 8);
                            //if (overflow > 0) {
                            //    currentBitPos += (currentBitfieldType.AlignOf() * 8) - overflow;
                            //}
                            currentBitPos = currentBitfieldStartBit + (currentBitfieldType.AlignOf() * 8);

                            // 情報を更新
                            currentBitfieldType = null;
                            currentBitfieldStartBit = currentBitPos;
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
                        // の指針より、境界調整についいて以下のことが導出できる
                        // - 境界を正しく調整された型の中にある要素は、境界を正しく調整されていなければならない。
                        // - 境界を正しく調整された結果はsizeof演算子の例2を満たさなければならない。
                        //
                        // このことより、構造体については以下の指針を満たす必要がある
                        // - 構造体の各メンバはそれぞれの境界調整に従う
                        // - 構造体全体の境界調整は構造体メンバの最大の境界調整となる
                        // - 上記より、構造体全体のサイズは構造体メンバの最大の境界調整の倍数となる
                        // 
                        // これは規格書には直接書いていないが、細かく読み取ると導出される。
                        {
                            structAlign = alignSize != 0 ? alignSize : packSize == 0 ? structAlign : Math.Min(packSize, structAlign); // https://docs.microsoft.com/ja-jp/cpp/c-language/storage-and-alignment-of-structures?view=vs-2019 より
                            // 構造体全体のサイズをアライメント境界にそろえる
                            var overflow = currentBitPos % (structAlign * 8);
                            if (overflow > 0) {
                                currentBitPos += (structAlign * 8 - overflow);
                            }
                        }

                        return Tuple.Create(currentBitPos / 8, structAlign, result);

                    }

                    /// <summary>
                    /// 共用体のレイアウト計算を実行
                    /// </summary>
                    /// <param name="members"></param>
                    /// <param name="alignSize"></param>
                    /// <returns></returns>
                    public override Tuple<int, int> UnionLayout(List<MemberInfo> members, int alignSize) {
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
                        var pad = LayoutBase.PaddingOf(size, align);
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

                    LayoutBase layout;
                    switch (Layout) {
                        case LayoutMode.PCC:
                            layout = new LayoutPcc();
                            break;
                        case LayoutMode.MSVC:
                            layout = new LayoutMsvc();
                            break;
                        default:
                            layout = new LayoutPcc();
                            break;
                    }
                    if (Kind == StructOrUnion.Struct) {
                        // 構造体型の場合
                        var ret = layout.StructLayout(Members, PackSize, AlignSize);
                        _size = ret.Item1;
                        AlignSize = ret.Item2;
                        Members = ret.Item3;
                    } else {
                        // 共用体型のの場合
                        var ret = layout.UnionLayout(Members, AlignSize);
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
