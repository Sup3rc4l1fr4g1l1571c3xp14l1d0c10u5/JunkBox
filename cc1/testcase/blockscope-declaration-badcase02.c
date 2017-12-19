/**@
 spec: ブロックスコープでの定義の違反例(2)
 assertion: SpecificationErrorException
@**/

void f1() {
    extern int i1 = 1; /* error: ‘i1’ has both ‘extern’ and initializer */
                       /* ブロックスコープ内のexternが初期化子を持っている */
}
