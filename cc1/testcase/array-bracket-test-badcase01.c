/**@
 spec: 配列と括弧の扱いのテストの間違いケース(1)
 assertion: SpecificationErrorException
@**/

void test1() {
	int a[] = {0,1,1,2,3};
	int y = a;  /* (Warning): initialization makes integer from pointer without a cast */
}
