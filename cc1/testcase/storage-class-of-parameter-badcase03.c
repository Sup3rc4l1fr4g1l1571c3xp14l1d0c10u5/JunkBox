/**@
 spec: K&R識別子並び中ではregister以外の記憶クラス指定子を使えない(3)
 assertion: SpecificationErrorException
@**/

int f(x) 
static int  x;	/* static は使えない */
{
	return x;
}

