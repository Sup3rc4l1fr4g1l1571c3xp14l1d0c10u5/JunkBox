/**@
spec: 左辺値と単項&演算子の不正な組み合わせ(1)
 assertion: SpecificationErrorException
@**/
int foo() {
    &"a"; /* 文字列リテラル */
}
