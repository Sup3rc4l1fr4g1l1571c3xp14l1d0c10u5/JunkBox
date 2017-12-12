/**@
 spec: 関数の引数宣言 中で typedef
 assertion: SpecificationErrorException
@**/

int foo(typedef int INT32, INT32 x) {
	return 0;
}
