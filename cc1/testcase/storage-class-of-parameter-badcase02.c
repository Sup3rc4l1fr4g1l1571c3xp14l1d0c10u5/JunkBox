/**@
 spec: K&R識別子並び中ではregister以外の記憶クラス指定子を使えない(2)
 assertion: SpecificationErrorException
@**/

int f(x) 
extern int x; /* extern は使えない */
{
	return x;
}

