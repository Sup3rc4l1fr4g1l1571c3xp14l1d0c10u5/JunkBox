/**@
 spec: 同一スコープ中での typedef (1)
 assertion: SpecificationErrorException
@**/

typedef int SINT;
typedef int SINT;   /* NG(redefine) */

int main(void) {
	SINT x = 1;
	return (int)x;0
}

