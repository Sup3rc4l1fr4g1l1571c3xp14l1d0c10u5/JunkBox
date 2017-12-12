/**@
 spec: ブロックスコープでの定義の違反例(3)
 assertion: SpecificationErrorException
@**/

void f1() {
    extern int i1;     // 外部結合のため妥当
    static int i1;     // 無結合のため重複宣言できない
}
