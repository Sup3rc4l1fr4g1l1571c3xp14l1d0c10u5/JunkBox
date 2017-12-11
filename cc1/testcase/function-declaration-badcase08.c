/**@
 spec: 不正なな関数宣言のケース(8)
 assertion: SpecificationErrorException
@**/

int main(void) {
	// 不正な再宣言の組み合わせ
	static int sum(int,int);
	sum(1,2);
}

static int sum(int x, int y) {
	return 0;
}

