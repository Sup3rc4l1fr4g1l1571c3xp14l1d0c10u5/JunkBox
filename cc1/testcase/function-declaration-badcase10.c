/**@
 spec: 不正なな関数宣言のケース(10)
 assertion: SpecificationErrorException
@**/

int main(void) {
    static int hoge(void);	// static は使えない
	return 4;
}
static int hoge(void) {
}
