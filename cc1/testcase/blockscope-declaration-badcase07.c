/**@
 spec: ブロックスコープでの定義の違反例(7)
 assertion: SpecificationErrorException
@**/

static void f2() {
}

void f1() {
	static void f2();
}

