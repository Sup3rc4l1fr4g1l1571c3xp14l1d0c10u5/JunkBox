/**@
 spec: ブロックスコープでの定義の違反例(2)
 assertion: SpecificationErrorException
@**/

void f1() {
    extern int i1 = 1;     // ブロックスコープ内のexternが初期化子を持っている
}
