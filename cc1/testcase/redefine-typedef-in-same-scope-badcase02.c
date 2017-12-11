/**@
 spec: 同一スコープ中での typedef (2)
 assertion: SpecificationErrorException
@**/

int main(void) {
	typedef int SINT;
	typedef int SINT;   // NG(redefine)
	SINT x = 1;
	return (int)x;0
}

