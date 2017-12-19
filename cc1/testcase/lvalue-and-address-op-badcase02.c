/**@
spec: 左辺値と単項&演算子の不正な組み合わせ(2)
 assertion: SpecificationErrorException
@**/
int foo() {
    &1; /* 定数値 */
}
