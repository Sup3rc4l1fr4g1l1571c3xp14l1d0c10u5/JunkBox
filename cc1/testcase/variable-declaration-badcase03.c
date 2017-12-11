/**@
 spec: 不正なな変数宣言/定義(3)
 assertion: TypeMissmatchError
@**/

int x ;

int main(void) {
	extern double x;	// 型が違う
	return 0;
}
