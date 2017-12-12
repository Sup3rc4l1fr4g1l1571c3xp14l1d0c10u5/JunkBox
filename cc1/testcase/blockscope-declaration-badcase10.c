/**@
 spec: ブロックスコープでの定義の違反例(9)
 assertion: SpecificationErrorException
@**/

extern int x = 0;

void f1() {
	extern int x = 1;
}

