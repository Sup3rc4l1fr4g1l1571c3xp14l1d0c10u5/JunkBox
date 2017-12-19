/**@
 spec: 入れ子スコープ中での typedef
 assertion: 
@**/

typedef double SINT;
int main(void) {
	typedef int SINT;   /* OK(override) */
	SINT x = 1;
	return (int)x;
}

