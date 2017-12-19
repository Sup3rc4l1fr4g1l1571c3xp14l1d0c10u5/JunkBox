/**@
 spec: 不正なな変数宣言/定義(2)
 assertion: SpecificationErrorException
@**/

int x = 0;   /* 完全な宣言が重複 */
int x = 1;

int main(void) {
	return 0;
}
