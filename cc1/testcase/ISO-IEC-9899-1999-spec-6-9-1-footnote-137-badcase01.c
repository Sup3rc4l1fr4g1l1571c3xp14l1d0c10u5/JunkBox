/**@
 spec: 規格書の 6.9.1 脚注(137)の不正ケース(1)
 assertion: SpecificationErrorException
@**/

/* 型 F は“仮引数をもたず，int を返す関数” */
typedef int F(void);

/* 誤：6.9.1 関数定義の制約違反。 */
F   f { 
	/* ... */ 
}
