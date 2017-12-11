/**@
 spec: 妥当な変数宣言/定義(1)
 assertion: 
@**/

int main(void) {
	extern int x;   // 外部定義なので問題なし
	extern int x;
	return 0;
}
