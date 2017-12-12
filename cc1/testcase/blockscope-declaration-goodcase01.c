/**@
 spec: ブロックスコープでの定義の妥当例(1)
 assertion: 
@**/

int i1 = 1;  //定義，外部結合  
static int i2 = 2;  //定義，内部結合  
extern int i3 = 3;  //定義，外部結合  

void f1(int i1) {   // 無結合かつ別スコープなので妥当
}

void f2() {
    extern int i1;     // 外側の外部結合を参照するので妥当
    extern int i2;     // 外側の内部結合を参照するので妥当
    extern int i3;     // 外側の外部結合を参照するので妥当
}

void f3() {
    int i1 = 1;     // 無結合のため前の定義を隠す。妥当
    int i2 = 1;     // 無結合のため前の定義を隠す。妥当
    int i3 = 1;     // 無結合のため前の定義を隠す。妥当
}

void f4() {
    static int i1 = 1;     // 無結合のため前の定義を隠す。妥当
    static int i2 = 1;     // 無結合のため前の定義を隠す。妥当
    static int i3 = 1;     // 無結合のため前の定義を隠す。妥当
}
