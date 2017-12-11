/**@
 spec: 妥当な関数宣言のケース(3)
 assertion: 
@**/



typedef int Foo(int, int);

int main(void) {
	Foo sum;	// 関数宣言として妥当
	sum(1,2);
}

int sum(int x, int y) {
	return 0;
}

