/**@
 spec: ブロックスコープでの定義の妥当例(2)
 assertion: 
@**/

void f1(int i1) {   /* 無結合なので妥当 */
}

void f2() {
    extern int i1;     /* 外部結合になるので妥当 */
    extern int i2;     /* 外部結合になるので妥当 */
    extern int i3;     /* 外部結合になるので妥当 */
}

void f3() {
    int i1 = 1;     /* 無結合になるので妥当 */
    int i2 = 1;     /* 無結合になるので妥当 */
    int i3 = 1;     /* 無結合になるので妥当 */
}

void f4() {
    static int i1 = 1;     /* 無結合になるので妥当 */
    static int i2 = 1;     /* 無結合になるので妥当 */
    static int i3 = 1;     /* 無結合になるので妥当 */
}
