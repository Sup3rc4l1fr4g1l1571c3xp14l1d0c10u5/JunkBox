/**@
 spec: 不正なな関数宣言のケース(4)
 assertion: SpecificationErrorException
@**/

typedef int Foo(int, int);

int main(void) {
	Foo sum = null;	/* 関数宣言なので初期化子は持てない */
	sum(1,2);
}

int sum(int x, int y) {
	return 0;
}

