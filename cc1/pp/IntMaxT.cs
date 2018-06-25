using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CSCPP {
    [StructLayout(LayoutKind.Explicit)]
    public struct IntMaxT {
        [FieldOffset(0)]
        private readonly Int32 Int32Value;
        [FieldOffset(0)]
        private readonly UInt32 UInt32Value;
        [FieldOffset(0)]
        private readonly Int64 Int64Value;
        [FieldOffset(0)]
        private readonly UInt64 UInt64Value;

        [FieldOffset(8)]
        private readonly Flags Flag;

        [Flags]
        public enum Flags : uint {
            Is32Bit = 0x00,
            Is64Bit = 0x01,
            IsUnsigned = 0x00,
            IsSigned = 0x02,

        }
        public IntMaxT(Int32 value) : this() { Int32Value = value; Flag = Flags.IsSigned | Flags.Is32Bit; }
        public IntMaxT(UInt32 value) : this() { UInt32Value = value; Flag = Flags.IsUnsigned | Flags.Is32Bit; }
        public IntMaxT(Int64 value) : this() { Int64Value = value; Flag = Flags.IsSigned | Flags.Is64Bit; }
        public IntMaxT(UInt64 value) : this() { UInt64Value = value; Flag = Flags.IsUnsigned | Flags.Is64Bit; }
        public bool Is32Bit() {
            return (Flag & Flags.Is64Bit) == 0;
        }
        public bool Is64Bit() {
            return (Flag & Flags.Is64Bit) != 0;
        }
        public bool IsSigned() {
            return (Flag & Flags.IsSigned) != 0;
        }
        public bool IsUnsigned() {
            return (Flag & Flags.IsSigned) == 0;
        }
        public bool IsNegative {
            get {
                return IsSigned() && (Is32Bit()) ? (Int32Value < 0) : (Int64Value < 0L);
            }
        }

        public static void Test() {
            {
                // 変換前後で値が変化しない例
                {
                    // (int32)-1LL
                    var i64Min = new IntMaxT(-1L);
                    bool overflow = false;
                    Int32 ret = i64Min.AsInt32((e, s) => {
                        Console.Error.WriteLine(s);
                        overflow = true;
                    });
                    System.Diagnostics.Debug.Assert(!overflow);
                    System.Diagnostics.Debug.Assert(ret == -1);
                }
                {
                    // (int32)(int64)LONG_MIN
                    var i64Min = new IntMaxT((long)Int32.MinValue);
                    bool overflow = false;
                    Int32 ret = i64Min.AsInt32((e, s) => {
                        Console.Error.WriteLine(s);
                        overflow = true;
                    });
                    System.Diagnostics.Debug.Assert(!overflow);
                    System.Diagnostics.Debug.Assert(ret == Int32.MinValue);
                }
            }
            {
                {
                    // 変換前後で値が変化する例：(int32)LLONG_MIN 
                    var i64Min = new IntMaxT(long.MinValue);
                    bool overflow = false;
                    Int32 ret = i64Min.AsInt32((e, s) => {
                        Console.Error.WriteLine(s);
                        overflow = true;
                    });
                    System.Diagnostics.Debug.Assert(overflow);
                    System.Diagnostics.Debug.Assert(ret == Int32.MinValue);
                }
                {
                    // 変換前後で値が変化する例：(Uint32)-1LL
                    var i64Min = new IntMaxT(-1L);
                    bool overflow = false;
                    UInt32 ret = i64Min.AsUInt32((e, s) => {
                        Console.Error.WriteLine(s);
                        overflow = true;
                    });
                    System.Diagnostics.Debug.Assert(overflow);
                    System.Diagnostics.Debug.Assert(ret == UInt32.MinValue);
                }
            }
            {
                // オーバーフローする例
                {
                    var i64Min = new IntMaxT(long.MinValue);
                    var i64Minus1 = new IntMaxT((long)-1);
                    bool overflow = false;
                    var ret = IntMaxT.Add(i64Min, i64Minus1, (e, s) => {
                        Console.Error.WriteLine(s);
                        overflow = true;
                    });
                    System.Diagnostics.Debug.Assert(overflow);
                    System.Diagnostics.Debug.Assert(ret.AsInt64() == long.MaxValue);
                }
                {
                    var i64Max = new IntMaxT(long.MaxValue);
                    var i64Plus1 = new IntMaxT((long)1);
                    bool overflow = false;
                    var ret = IntMaxT.Add(i64Max, i64Plus1, (e, s) => {
                        Console.Error.WriteLine(s);
                        overflow = true;
                    });
                    System.Diagnostics.Debug.Assert(overflow);
                    System.Diagnostics.Debug.Assert(ret.AsInt64() == long.MinValue);
                }
            }
            {
                // オーバーフローしない例
                {
                    var i64Min = new IntMaxT(long.MinValue);
                    var i64Plus1 = new IntMaxT((long)1);
                    bool overflow = false;
                    var ret = IntMaxT.Add(i64Min, i64Plus1, (e, s) => {
                        Console.Error.WriteLine(s);
                        overflow = true;
                    });
                    System.Diagnostics.Debug.Assert(!overflow);
                }
                {
                    var i64Max = new IntMaxT(long.MaxValue);
                    var i64Minus1 = new IntMaxT((long)-1);
                    bool overflow = false;
                    var ret = IntMaxT.Add(i64Max, i64Minus1, (e, s) => {
                        Console.Error.WriteLine(s);
                        overflow = true;
                    });
                    System.Diagnostics.Debug.Assert(!overflow);
                }
            }
        }

        public Int32 AsInt32(Action<bool, string> overflowHandler = null) {
            if (this.Is64Bit() && this.IsSigned()) {
                // int64_t -> int32_t
                if ((this.Int64Value < Int32.MinValue || Int32.MaxValue < this.Int64Value)) {
                    overflowHandler?.Invoke(false, "int64_t から int32_t への変換でデータの消失 (切り捨て) が発生しました。");
                }
                return unchecked((Int32)this.Int64Value);
            } else if (this.Is64Bit() && this.IsUnsigned()) {
                // uint64_t -> int32_t
                if ((Int32.MaxValue < this.UInt64Value)) {
                    overflowHandler?.Invoke(false, "uint64_t から int32_t への変換でデータの消失 (切り捨て) が発生しました。");
                }
                return unchecked((Int32)this.UInt64Value);
            } else if (this.Is32Bit() && this.IsSigned()) {
                // int32_t -> int32_t
                // 変化なし
                return unchecked((Int32)this.Int32Value);
            } else if (this.Is32Bit() && this.IsUnsigned()) {
                // uint32_t -> int32_t
                if ((Int32.MaxValue < this.UInt32Value)) {
                    overflowHandler?.Invoke(false, "uint32_t から int32_t への変換でデータの消失 (切り捨て) が発生しました。");
                }
                return unchecked((Int32)this.UInt32Value);
            } else {
                throw new Exception("");
            }
        }

        public UInt32 AsUInt32(Action<bool, string> overflowHandler = null) {
            if (this.Is64Bit() && this.IsSigned()) {
                // int64_t -> uint32_t
                if ((this.Int64Value < UInt32.MinValue || UInt32.MaxValue < this.Int64Value)) {
                    overflowHandler?.Invoke(false, "int64_t から uint32_t への変換でデータの消失 (切り捨て) が発生しました。");
                }
                return unchecked((UInt32)this.Int64Value);
            } else if (this.Is64Bit() && this.IsUnsigned()) {
                // uint64_t -> uint32_t
                if ((UInt32.MaxValue < this.UInt64Value)) {
                    overflowHandler?.Invoke(false, "uint64_t から uint32_t への変換でデータの消失 (切り捨て) が発生しました。");
                }
                return unchecked((UInt32)this.UInt64Value);
            } else if (this.Is32Bit() && this.IsSigned()) {
                // int32_t -> uint32_t
                if ((this.Int32Value < UInt32.MinValue)) {
                    overflowHandler?.Invoke(false, "int32_t から uint32_t への変換でデータの消失 (切り捨て) が発生しました。");
                }
                return unchecked((UInt32)this.Int32Value);
            } else if (this.Is32Bit() && this.IsUnsigned()) {
                // uint32_t -> uint32_t
                // 変化なし
                return unchecked((UInt32)this.UInt32Value);
            } else {
                throw new Exception("");
            }
        }

        public Int64 AsInt64(Action<bool, string> overflowHandler = null) {
            if (this.Is64Bit() && this.IsSigned()) {
                // int64_t -> int64_t
                // 変化なし
                return unchecked((Int64)this.Int64Value);
            } else if (this.Is64Bit() && this.IsUnsigned()) {
                // uint64_t -> int64_t
                if ((Int64.MaxValue < this.UInt64Value)) {
                    overflowHandler?.Invoke(false, "uint64_t から int64_t への変換でデータの消失 (切り捨て) が発生しました。");
                }
                return unchecked((Int64)this.UInt64Value);
            } else if (this.Is32Bit() && this.IsSigned()) {
                // int32_t -> int64_t
                // 上位型への変換
                return unchecked((Int64)this.Int32Value);
            } else if (this.Is32Bit() && this.IsUnsigned()) {
                // uint32_t -> int64_t
                // 上位型への変換
                return unchecked((Int64)this.UInt32Value);
            } else {
                throw new Exception("");
            }
        }
        public UInt64 AsUInt64(Action<bool, string> overflowHandler = null) {
            if (this.Is64Bit() && this.IsSigned()) {
                // int64_t -> uint64_t
                if ((this.Int64Value < 0)) {
                    overflowHandler?.Invoke(false, "int64_t から uint64_t への変換でデータの消失 (切り捨て) が発生しました。");
                }
                return unchecked((UInt64)this.Int64Value);
            } else if (this.Is64Bit() && this.IsUnsigned()) {
                // uint64_t -> uint64_t
                // 変化なし
                return unchecked((UInt64)this.UInt64Value);
            } else if (this.Is32Bit() && this.IsSigned()) {
                // int32_t -> uint64_t
                if ((this.Int32Value < 0)) {
                    overflowHandler?.Invoke(false, "int32_t から uint64_t への変換でデータの消失 (切り捨て) が発生しました。");
                }
                return unchecked((UInt64)this.Int32Value);
            } else if (this.Is32Bit() && this.IsUnsigned()) {
                // uint32_t -> uint64_t
                // 上位型への変換
                return unchecked((UInt64)this.UInt32Value);
            } else {
                throw new Exception("");
            }
        }

        public static IntMaxT Add(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    if (right > 0 && left > UInt32.MaxValue - right) {
                        overflowHandler?.Invoke(false, $"{lhs} + {rhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT((UInt32)(left + right));
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    if (right > 0 ? (left > Int32.MaxValue - right) : left < (Int32.MinValue - right)) {
                        overflowHandler?.Invoke(false, $"{lhs} + {rhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT((Int32)(left + right));
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    if (right > 0 && left > UInt64.MaxValue - right) {
                        overflowHandler?.Invoke(false, $"{lhs} + {rhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT((UInt64)(left + right));
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    if (right > 0 ? (left > Int64.MaxValue - right) : left < (Int64.MinValue - right)) {
                        overflowHandler?.Invoke(false, $"{lhs} + {rhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT((Int64)(left + right));
                }
            }
            throw new Exception();
        }
        public static IntMaxT Sub(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    if (right > 0 && left < UInt32.MinValue + right) {
                        overflowHandler?.Invoke(false, $"{lhs} - {rhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT((UInt32)(left - right));
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    if (right > 0 ? (left < Int32.MinValue + right) : left > (Int32.MaxValue + right)) {
                        overflowHandler?.Invoke(false, $"{lhs} - {rhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT((Int32)(left - right));
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    if (right > 0 && left < UInt64.MinValue + right) {
                        overflowHandler?.Invoke(false, $"{lhs} - {rhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT((UInt64)(left - right));
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    if (right > 0 ? (left < Int64.MinValue + right) : left > (Int64.MaxValue + right)) {
                        overflowHandler?.Invoke(false, $"{lhs} - {rhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT((Int64)(left - right));
                }
            }
            throw new Exception();
        }
        public static IntMaxT Mul(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    if (right > 0 && (left > UInt32.MaxValue / right)) {
                        overflowHandler?.Invoke(false, $"{lhs} * {rhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT((UInt32)(left * right));
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    if (right > 0 ? (left > Int32.MaxValue / right || left < Int32.MinValue / right)
                                  : (right < -1 ? (left > Int32.MinValue / right || left < Int32.MaxValue / right)
                                                : (right == -1 && left == Int32.MinValue))) {
                        overflowHandler?.Invoke(false, $"{lhs} * {rhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT((Int32)(left * right));
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    if (right > 0 && (left > UInt64.MaxValue / right)) {
                        overflowHandler?.Invoke(false, $"{lhs} * {rhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT((UInt64)(left * right));
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    if (right > 0 ? (left > Int64.MaxValue / right || left < Int64.MinValue / right)
                                  : (right < -1 ? (left > Int64.MinValue / right || left < Int64.MaxValue / right)
                                                : (right == -1 && left == Int64.MinValue))) {
                        overflowHandler?.Invoke(false, $"{lhs} * {rhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT((Int64)(left * right));
                }
            }
            throw new Exception();

        }
        public static IntMaxT Div(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    if (right == 0) {
                        overflowHandler?.Invoke(true, $"{lhs} / {rhs} はゼロ除算です。");
                        return new IntMaxT((UInt32)(0));
                    } else {
                        return new IntMaxT((UInt32)(left / right));
                    }
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    if (right == 0) {
                        overflowHandler?.Invoke(true, $"{lhs} / {rhs} はゼロ除算です。");
                        return new IntMaxT((UInt32)(0));
                    } else if ((left == Int32.MinValue) && (right == -1)) {
                        overflowHandler?.Invoke(false, $"{lhs} / {rhs} は演算結果がオーバーフローします。");
                        return new IntMaxT((UInt32)(0));
                    } else {
                        return new IntMaxT((Int32)(left / right));
                    }
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    if (right == 0) {
                        overflowHandler?.Invoke(true, $"{lhs} / {rhs} はゼロ除算です。");
                        return new IntMaxT((UInt64)(0));
                    } else {
                        return new IntMaxT((UInt64)(left / right));
                    }
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    if (right == 0) {
                        overflowHandler?.Invoke(true, $"{lhs} / {rhs} はゼロ除算です。");
                        return new IntMaxT((UInt64)(0));
                    } else if ((left == Int64.MinValue) && (right == -1)) {
                        overflowHandler?.Invoke(false, $"{lhs} / {rhs} は演算結果がオーバーフローします。");
                        return new IntMaxT((UInt64)(0));
                    } else {
                        return new IntMaxT((Int64)(left / right));
                    }
                }
            }
            throw new Exception();
        }
        public static IntMaxT Mod(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    if (right == 0) {
                        overflowHandler?.Invoke(true, $"{lhs} % {rhs} はゼロ除算です。");
                        return new IntMaxT((UInt32)(0));
                    } else {
                        return new IntMaxT((UInt32)(left % right));
                    }
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    if (right == 0) {
                        overflowHandler?.Invoke(true, $"{lhs} % {rhs} はゼロ除算です。");
                        return new IntMaxT((UInt32)(0));
                    } else if ((left == Int32.MinValue) && (right == -1)) {
                        overflowHandler?.Invoke(false, $"{lhs} % {rhs} は演算結果がオーバーフローします。");
                        return new IntMaxT((UInt32)(0));
                    } else {
                        return new IntMaxT((Int32)(left % right));
                    }
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    if (right == 0) {
                        overflowHandler?.Invoke(true, $"{lhs} % {rhs} はゼロ除算です。");
                        return new IntMaxT((UInt64)(0));
                    } else {
                        return new IntMaxT((UInt64)(left % right));
                    }
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    if (right == 0) {
                        overflowHandler?.Invoke(true, $"{lhs} % {rhs} はゼロ除算です。");
                        return new IntMaxT((UInt64)(0));
                    } else if ((left == Int64.MinValue) && (right == -1)) {
                        overflowHandler?.Invoke(false, $"{lhs} % {rhs} は演算結果がオーバーフローします。");
                        return new IntMaxT((UInt64)(0));
                    } else {
                        return new IntMaxT((Int64)(left % right));
                    }
                }
            }
            throw new Exception();
        }
        public static IntMaxT ShiftRight(IntMaxT lhs, int rhs, Action<bool, string> overflowHandler = null) {
            if (rhs < 0) {
                overflowHandler?.Invoke(false, $"{lhs} >> {rhs} の右シフト数が負数です。");
            }
            if (lhs.Is32Bit()) {
                if (rhs >= 32) {
                    overflowHandler?.Invoke(false, $"{lhs} >> {rhs} の右シフト数が左オペランドのビット幅以上です。");
                }
                if (lhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    return new IntMaxT((UInt32)(left >> rhs));
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    if (left < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} >> {rhs} の左オペランドが負数です。符号付きの負の数値の右シフト結果は実装依存の結果となります。");
                    }
                    return new IntMaxT((Int32)(left >> rhs));
                }
            }
            if (lhs.Is64Bit()) {
                if (rhs >= 64) {
                    overflowHandler?.Invoke(false, $"{lhs} >> {rhs} の右シフト数が左オペランドのビット幅以上です。");
                }
                if (lhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    return new IntMaxT((UInt64)(left >> rhs));
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    if (left < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} >> {rhs} の左オペランドが負数です。符号付きの負の数値の右シフト結果は実装依存の結果となります。");
                    }
                    return new IntMaxT((Int64)(left >> rhs));
                }
            }
            throw new Exception();
        }
        public static IntMaxT ShiftLeft(IntMaxT lhs, int rhs, Action<bool, string> overflowHandler = null) {
            if (rhs < 0) {
                overflowHandler?.Invoke(false, $"{lhs} << {rhs} の左シフト数が負数です。");
            }
            if (lhs.Is32Bit()) {
                if (rhs >= 32) {
                    overflowHandler?.Invoke(false, $"{lhs} << {rhs} の左シフト数が左オペランドのビット幅以上です。");
                }
                if (lhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    return new IntMaxT((UInt32)(left << rhs));
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    if (left < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} >> {rhs} の左オペランドが負数です。符号付きの負の数値の左シフト結果は実装依存の結果となります。");
                    }
                    return new IntMaxT((Int32)(left << rhs));
                }
            }
            if (lhs.Is64Bit()) {
                if (rhs >= 64) {
                    overflowHandler?.Invoke(false, $"{lhs} << {rhs} の左シフト数が左オペランドのビット幅以上です。");
                }
                if (lhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    return new IntMaxT((UInt64)(left << rhs));
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    if (left < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} >> {rhs} の左オペランドが負数です。符号付きの負の数値の左シフト結果は実装依存の結果となります。");
                    }
                    return new IntMaxT((Int64)(left << rhs));
                }
            }
            throw new Exception();
        }
        public static IntMaxT BitAnd(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    return new IntMaxT((UInt32)(left & right));
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    if (left < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} & {rhs} の左オペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    if (right < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} & {rhs} の右オペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    return new IntMaxT((Int32)(left & right));
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    return new IntMaxT((UInt64)(left & right));
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    if (left < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} & {rhs} の左オペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    if (right < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} & {rhs} の右オペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    return new IntMaxT((Int64)(left & right));
                }
            }
            throw new Exception();
        }
        public static IntMaxT BitOr(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    return new IntMaxT((UInt32)(left | right));
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    if (left < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} | {rhs} の左オペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    if (right < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} | {rhs} の右オペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    return new IntMaxT((Int32)(left | right));
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    return new IntMaxT((UInt64)(left | right));
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    if (left < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} | {rhs} の左オペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    if (right < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} | {rhs} の右オペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    return new IntMaxT((Int64)(left | right));
                }
            }
            throw new Exception();
        }
        public static IntMaxT BitXor(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    return new IntMaxT((UInt32)(left ^ right));
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    if (left < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} ^ {rhs} の左オペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    if (right < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} ^ {rhs} の右オペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    return new IntMaxT((Int32)(left ^ right));
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    return new IntMaxT((UInt64)(left ^ right));
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    if (left < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} ^ {rhs} の左オペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    if (right < 0) {
                        overflowHandler?.Invoke(false, $"{lhs} ^ {rhs} の右オペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    return new IntMaxT((Int64)(left ^ right));
                }
            }
            throw new Exception();
        }
        public static IntMaxT BitNot(IntMaxT lhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    return new IntMaxT((UInt32)(~left));
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    if (left < 0) {
                        overflowHandler?.Invoke(false, $"~{lhs} のオペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    return new IntMaxT((Int32)(~left));
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    return new IntMaxT((UInt64)(~left));
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    if (left < 0) {
                        overflowHandler?.Invoke(false, $"~{lhs} のオペランドが符号付きの負の数値です。符号付きの負の数値のビット単位の演算結果は処理系定義の結果となります。");
                    }
                    return new IntMaxT((Int64)(~left));
                }
            }
            throw new Exception();
        }
        public static IntMaxT Neg(IntMaxT lhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    if (left > ((UInt32)Int32.MaxValue + 1U)) {
                        overflowHandler?.Invoke(false, $"-{lhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT(-unchecked((Int32)(left)));
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    if (left == Int32.MinValue) {
                        overflowHandler?.Invoke(false, "-{lhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT(-unchecked((Int32)(left)));
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    if (left > ((UInt64)Int64.MaxValue + 1UL)) {
                        overflowHandler?.Invoke(false, $"-{lhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT(unchecked(-(Int64)(left)));
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    if (left == Int64.MinValue) {
                        overflowHandler?.Invoke(false, $"-{lhs} は演算結果がオーバーフローします。");
                    }
                    return new IntMaxT(unchecked(-(Int64)(left)));
                }
            }
            throw new Exception();
        }
        public static bool GreatThan(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    return (left > right);
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    return (left > right);
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    return (left > right);
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    return (left > right);
                }
            }
            throw new Exception();
        }
        public static bool LessThan(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    return (left < right);
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    return (left < right);
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    return (left < right);
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    return (left < right);
                }
            }
            throw new Exception();
        }
        public static bool GreatEqual(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    return (left >= right);
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    return (left >= right);
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    return (left >= right);
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    return (left >= right);
                }
            }
            throw new Exception();
        }
        public static bool LessEqual(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    return (left <= right);
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    return (left <= right);
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    return (left <= right);
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    return (left <= right);
                }
            }
            throw new Exception();
        }
        public static bool NotEqual(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    return (left != right);
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    return (left != right);
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    return (left != right);
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    return (left != right);
                }
            }
            throw new Exception();
        }
        public static bool Equal(IntMaxT lhs, IntMaxT rhs, Action<bool, string> overflowHandler = null) {
            if (lhs.Is32Bit() != rhs.Is32Bit()) {
                throw new Exception();
            }
            if (lhs.Is32Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt32(overflowHandler);
                    var right = rhs.AsUInt32(overflowHandler);
                    return (left == right);
                } else {
                    var left = lhs.AsInt32(overflowHandler);
                    var right = rhs.AsInt32(overflowHandler);
                    return (left == right);
                }
            }
            if (lhs.Is64Bit()) {
                if (lhs.IsUnsigned() || rhs.IsUnsigned()) {
                    var left = lhs.AsUInt64(overflowHandler);
                    var right = rhs.AsUInt64(overflowHandler);
                    return (left == right);
                } else {
                    var left = lhs.AsInt64(overflowHandler);
                    var right = rhs.AsInt64(overflowHandler);
                    return (left == right);
                }
            }
            throw new Exception();
        }

        public static bool Mode64 = false;
        public static IntMaxT CreateSigned(long v) {
            return Mode64 ? new IntMaxT((long)v) : new IntMaxT((int)v);
        }
        public static IntMaxT CreateUnsigned(ulong v) {
            return Mode64 ? new IntMaxT((ulong)v) : new IntMaxT((uint)v);
        }

        public override bool Equals(object obj) {
            if (!(obj is IntMaxT)) {
                return false;
            }

            var t = (IntMaxT)obj;
            return this.UInt64Value == t.UInt64Value;
        }

        public override int GetHashCode() {
            return -341342807 + EqualityComparer<object>.Default.GetHashCode(UInt64Value);
        }


        public static long SignedMaxValue { get { return Mode64 ? long.MaxValue : int.MaxValue; } }
        public static long SignedMinValue { get { return Mode64 ? long.MinValue : int.MinValue; } }
        public static ulong UnsignedMaxValue { get { return Mode64 ? ulong.MaxValue : uint.MaxValue; } }
        public static ulong UnsignedMinValue { get { return Mode64 ? ulong.MinValue : uint.MinValue; } }

        public override string ToString() {
            if (Is32Bit()) {
                if (IsUnsigned()) {
                    return $"{AsUInt32()}UL";
                } else {
                    return $"{AsInt32()}L";
                }
            }
            if (Is64Bit()) {
                if (IsUnsigned()) {
                    return $"{AsUInt64()}ULL";
                } else {
                    return $"{AsInt64()}LL";
                }
            }
            throw new Exception();
        }

    }
 }
