/**@
 spec: 妥当な関数宣言のケース(4)
 assertion: 
@**/

int main(void) {
	// 妥当な再宣言の組み合わせ
	int sum(int,int);
	extern int sum(int,int);
	sum(1,2);
}

int sum(int x, int y) {
	return 0;
}

