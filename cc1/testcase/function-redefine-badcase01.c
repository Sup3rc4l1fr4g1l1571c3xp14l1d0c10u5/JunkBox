/**@
 spec: 6.7.5.3 関数宣言子で定められた関数宣言子の違反例(1)
 assertion: TypeMissmatchError
@**/

/*
 * 6.7.5.3 関数宣言子（関数原型を含む）
 *
 * - 一方の型が仮引数型並びをもち，他方の型が関数定義の一部でない関数宣言子によって指定され，
 *   識別子並びが空の場合，仮引数型並びは省略記号を含まない。
 *   各仮引数の型は，既定の実引数拡張を適用した結果の型と適合する。
 */

/* 一方の型 */
int foo(float);

/*
 * この時点でfooは 引数にfloatを受け取りintを返す関数。
 */

/* 他方の型。 */
int foo();	

/*
 * int foo(); のみでは、引数情報を持たない関数（K&Rプロトタイプ）となる。
 * しかし、上記ルールにより、その前の foo の引数型に既定の実引数拡張を適用した結果の型(floatなのでdouble)と適合するようにされる。
 * よって、ANSIスタイルの int foo(double); として見なされる
 */

int main(void) {
	return foo(1.0f);
}

int foo(float x) {	/* 前のfoo定義が int foo(double); であるため、型違反となる */
	return (int)x;
}

