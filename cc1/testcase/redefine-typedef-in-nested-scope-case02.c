/**@
 spec: 入れ子スコープ中での typedef
 assertion: 
@**/

int main(void) {
	typedef double SINT;
	{
		typedef int SINT;   /* OK(override) */
		SINT x = 1;
		return (int)x;
	}
}

