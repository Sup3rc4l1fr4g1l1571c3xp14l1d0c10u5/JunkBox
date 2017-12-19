/**@
 spec: 不正なな変数宣言/定義(4)
 assertion: TypeMissmatchError
@**/

int main(void) {
	extern int x;
	extern double x;	/* 型が違う */
	return 0;
}
