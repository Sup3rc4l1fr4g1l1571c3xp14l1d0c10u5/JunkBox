/**@
 spec: 不正なな関数宣言のケース(6)
 assertion: SpecificationErrorException
@**/

int main(void) {
	/* 不正な再宣言の組み合わせ */
	int sum(int,int);
	register int sum(int,int);
	sum(1,2);
}

int sum(int x, int y) {
	return 0;
}

