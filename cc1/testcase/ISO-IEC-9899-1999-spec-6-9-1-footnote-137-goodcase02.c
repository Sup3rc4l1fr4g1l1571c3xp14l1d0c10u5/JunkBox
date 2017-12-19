/**@
 spec: 規格書の 6.9.1 脚注(137)の妥当ケース(2)
 assertion: 
@**/

/* 型 F は“仮引数をもたず，int を返す関数” */
typedef int F(void);

/* 正：e は関数へのポインタを返す */
F  *e1(void) { /* ... */ }


/* 正：同上，括弧は無関係 */
F  *((e2))(void) { /*... */ }
