/**@
 spec: 妥当な変数宣言/定義(5)
 assertion: 
@**/

int x;  /* 不完全な定義 */
extern int x;  /* 不完全な定義 */
int main(void) {
	return 0;
}
extern int x = 0;   /* 完全な定義（ただし警告付き） */
