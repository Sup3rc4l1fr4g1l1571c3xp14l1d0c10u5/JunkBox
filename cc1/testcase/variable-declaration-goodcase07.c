/**@
 spec: 妥当な変数宣言/定義(7)
 assertion: 
@**/

static int x;	/* スコープ */
extern int x;	/* グローバルスコープのXを参照するので上のxを示す */

int main(void) {
	extern int x;	/* グローバルスコープのXを参照する */
	return 0;
}

