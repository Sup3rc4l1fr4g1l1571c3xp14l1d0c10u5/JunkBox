/**@
 spec: 妥当な変数宣言/定義(6)
 assertion: 
@**/

int x;  // 不完全な定義
int main(void) {
	extern int x;  // グローバルスコープの変数の参照
	return 0;
}
int x = 0;   // 完全な定義

