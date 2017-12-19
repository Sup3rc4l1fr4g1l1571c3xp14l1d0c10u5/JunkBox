/**@
 spec: 規格書の 6.9.1 脚注(137)の妥当ケース(3)
 assertion: 
@**/

/* 型 F は“仮引数をもたず，int を返す関数” */
typedef int F(void);

/* 正：fpは型 F の関数を指す(関数ポインタ変数) */
int (*fp)(void);

/* 正：Fpは型 F の関数を指す(関数ポインタ変数) */
F  *Fp;
