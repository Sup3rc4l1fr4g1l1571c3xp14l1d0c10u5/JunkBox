/**@
 spec: K&R識別子並び中ではregister以外の記憶クラス指定子を使えない(1)
 assertion: SpecificationErrorException
@**/

int f(x) 
typedef int SINT; /* typedef は使えない */
SINT x;
{
	return x;
}

