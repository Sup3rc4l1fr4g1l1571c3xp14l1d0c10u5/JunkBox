/**@
 spec: 不正なな変数宣言/定義(1)
 assertion: SpecificationErrorException
@**/

int main(void) {
	extern int x;
	       int x;   /* 同一スコープで名前が衝突 */
	return 0;
}
