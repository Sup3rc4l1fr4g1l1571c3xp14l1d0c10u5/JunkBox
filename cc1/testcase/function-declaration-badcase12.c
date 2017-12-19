/**@
 spec: 不正なな関数宣言のケース(12)
 assertion: TypeMissmatchError
@**/

int main(void) {
    int hoge(void);	/* 宣言とは別に定義済みオブジェクトをグローバル空間に作る。 */
	return 4;
}
int hoge(double d) {	/* 7行目で定義されたオブジェクトと引数型が違うのでTypeMissmatchErrorになる。 */
}
