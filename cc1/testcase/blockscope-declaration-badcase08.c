/**@
 spec: ブロックスコープでの定義の違反例(8)
 assertion: SpecificationErrorException
@**/

void f1() {
	typedef void (f2)();
	f2 x = 0;
}

