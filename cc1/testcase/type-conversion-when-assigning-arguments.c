/**@
 spec: 関数呼出し時の仮引数への代入時に暗黙的な型キャストが発生する例
 assertion: 
@**/

static  unsigned short
__bswap_16 (unsigned short __x)
{
  return (__x >> 8) | (__x << 8);
}

static  unsigned int
__bswap_32 (unsigned int __x)
{
  return (__bswap_16 (__x & 0xffff) << 16) | (__bswap_16 (__x >> 16));
/*

6.5.2.2 関数呼出し
呼び出される関数を表す式が関数原型を含む型をもつ場合，実引数の個数は，仮引数の個数と一致しなければならない。
各実引数は，対応する仮引数の型の非修飾版をもつオブジェクトにその値を代入することのできる型をもたなければならない。

とあるため、 __x & 0xffff および __x >> 16 の計算結果は unsigned int だが、
unsigned int は unsigned short にキャストなしで代入が可能なので、型不整合エラーにはならない。
*/
}

int main(void) {
    return __bswap_32(0x12345UL);
}

