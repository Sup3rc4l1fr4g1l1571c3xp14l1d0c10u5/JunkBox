/**@
 spec: 不正なな変数宣言/定義(4)
 assertion: SpecificationErrorException
@**/
int main(void) {
	static int x;
	extern int x;	/* リンケージ違いのエラーになる */
	return 0;
}
