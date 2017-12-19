/**@
 spec: 妥当な変数宣言/定義(4)
 assertion: 
@**/

extern int x;	/* 不完全な宣言 */
int x;	/* 不完全な宣言 */
int main(void) {
	return 0;
}
int x = 0;	/* 完全な宣言は一つなのでOK */
