/**@
 spec: ブロックスコープでの定義の違反例(1)
 assertion: SpecificationErrorException
@**/

void f1() {
    int i1 = 1;            // 無結合のため妥当
    static int i1 = 1;     // 無結合のため再定義できない。
}
