/**@
 spec: ブロックスコープでの定義の違反例(5)
 assertion: TypeMissmatchError
@**/

int f1() {
    extern int    i1;     // 外部結合になるので妥当
    extern double i1;     // 型が違うため違反
}

