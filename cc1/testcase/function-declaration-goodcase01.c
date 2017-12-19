/**@
 spec: 妥当な関数宣言のケース(1)
 assertion: 
@**/

int g(void) { return 0; }

/* 関数ポインタ変数の初期値設定なので妥当 */
int (*f)(void) = g;

