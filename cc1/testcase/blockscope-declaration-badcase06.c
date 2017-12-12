/**@
 spec: ブロックスコープでの定義の違反例(6)
 assertion: TypeMissmatchError
@**/

static int    i1;

int f1() {
    extern double i1;     // 型が違うため違反
}

