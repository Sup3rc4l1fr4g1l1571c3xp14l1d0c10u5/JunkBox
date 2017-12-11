/**@
 spec: 妥当な変数宣言/定義(2)
 assertion: 
@**/

int x;   // 不完全な定義同士の多重宣言なので問題なし
int x;
int main(void) {
	return 0;
}
