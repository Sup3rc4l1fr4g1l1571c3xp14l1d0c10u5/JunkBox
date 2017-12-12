/**@
 spec: ブロックスコープでの定義の違反例(9)
 assertion: SpecificationErrorException
@**/

void f1() {
	void f2() = 0;
}

