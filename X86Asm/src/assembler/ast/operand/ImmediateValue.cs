using System;
using System.Collections.Generic;

namespace X86Asm.ast.operand {
    using X86Asm.model;

    /// <summary>
    /// 即値リテラル
    /// </summary>
    public class ImmediateValue : IImmediate {

        /// <summary>
        /// 値が0のリテラル（よく使うためキャッシュして使いまわす）
        /// </summary>
        public static readonly ImmediateValue Zero = new ImmediateValue(0);

        /// <summary>
        /// リテラルの値
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// リテラルがシンボルの場合
        /// </summary>
        public Symbol Symbol { get; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="value">リテラルの値</param>
        public ImmediateValue(int value) {
            Symbol = null;
            Value = value;
        }
        public ImmediateValue(Symbol symbol, int value) {
            Symbol = symbol;
            Value = value;
        }

        /// <summary>
        /// 値が0であるかテスト</summary>
        /// <returns>0ならば真</returns>
        public bool IsZero() {
            return Value == 0;
        }

        /// <summary>
        /// 値が符号付き8ビット整数であるかテスト
        /// </summary>
        /// <returns>値が符号付き8ビット整数で表現できる範囲ならばならば真</returns>
        public bool IsInt8() {
            return Symbol == null && ((byte)Value) == Value;
        }

        /// <summary>
        /// 値が符号付き16ビット整数であるかテスト
        /// </summary>
        /// <returns>値が符号付き16ビット整数で表現できる範囲ならばならば真</returns>
        public bool IsInt16() {
                return Symbol == null && ((short)Value) == Value;
        }

        /// <summary>
        /// 値が符号無し8ビット整数であるかテスト
        /// </summary>
        /// <returns>値が符号無し8ビット整数で表現できる範囲ならばならば真</returns>
        public bool IsUInt8() {
            return Symbol == null && Value >= -0x80 && Value < 0x100;
        }

        /// <summary>
        /// 値が符号無し16ビット整数であるかテスト
        /// </summary>
        /// <returns>値が符号無し16ビット整数で表現できる範囲ならばならば真</returns>
        public bool IsUInt16() {
            return Symbol == null && Value >= -0x8000 && Value < 0x10000;
        }

        /// <summary>
        /// ラベルオフセットを考慮した即値オペランドの値を返す
        /// ImmediateValueクラスはそれ自身が即値なので自身を返す。
        /// </summary>
        /// <param name="symbolTable"> シンボル表 </param>
        /// <returns>即値オペランドの値</returns>
        public ImmediateValue GetValue(IDictionary<string, Symbol> symbolTable) {
            return this;
        }

        /// <summary>
        /// 比較処理
        /// </summary>
        /// <param name="other">比較対象</param>
        /// <returns>即値の値が一致すれば真</returns>
        public override bool Equals(object other) {
            if (!(other is ImmediateValue)) {
                return false;
            } else {
                return Symbol == ((ImmediateValue)other).Symbol && Value == ((ImmediateValue)other).Value;
            }
        }

        /// <summary>
        /// このオブジェクトのハッシュ値を返す
        /// </summary>
        /// <returns>ハッシュ値</returns>
        public override int GetHashCode() {
            return Value;
        }

        /// <summary>
        /// このオブジェクトの文字列表現を返す
        /// </summary>
        /// <returns>文字列表現</returns>
        public override string ToString() {
            return Convert.ToString(Value);
        }


        /// <summary>
        /// 符号なし32bit整数値のバイト列表現（リトルエンディアン）を得る
        /// </summary>
        /// <returns>符号なし32bit整数値のバイト列表現（リトルエンディアン）</returns>
        public byte[] To4Bytes() {
            return BitConverter.GetBytes(Value);
        }


        /// <summary>
        /// 符号なし16bit整数値のバイト列表現（リトルエンディアン）を得る
        /// </summary>
        /// <returns>符号なし16bit整数値のバイト列表現（リトルエンディアン） </returns>
        public byte[] To2Bytes() {
            if (!IsUInt16()) {
                throw new InvalidOperationException("値が符号なし16bit整数値ではありません");
            }
            return BitConverter.GetBytes((UInt16)Value);
        }


        /// <summary>
        /// 符号なし8bit整数値のバイト列表現（リトルエンディアン）を得る
        /// </summary>
        /// <returns>符号なし8bit整数値のバイト列表現（リトルエンディアン） </returns>
        public byte[] To1Byte() {
            if (!IsUInt8()) {
                throw new InvalidOperationException("値が符号なし8bit整数値ではありません");
            }
            return new[] { (byte)((uint)Value >> 0) };
        }

    }

}