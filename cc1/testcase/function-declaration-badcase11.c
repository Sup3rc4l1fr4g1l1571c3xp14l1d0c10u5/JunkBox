/**@
 spec: 不正なな関数宣言のケース(11)
 assertion: SpecificationErrorException
@**/

int main(void) {
    int hoge(void);	// 宣言とは別に定義済みオブジェクトをグローバル空間に作る。
	return 4;
}
static int hoge(void) {	// 7行目で定義されたオブジェクトと記憶クラス指定子が違うのでエラーになる。
}
