/**@
 spec: 不正なな変数宣言/定義(6)
 assertion: TypeMissmatchError
@**/

static int x;

int main(void) {
	extern double x;	/* 型違いエラーになる */
	return 0;
}
