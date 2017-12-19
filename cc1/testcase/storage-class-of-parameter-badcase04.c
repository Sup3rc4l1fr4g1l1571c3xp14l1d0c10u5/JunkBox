/**@
 spec: K&R識別子並び中ではregister以外の記憶クラス指定子を使えない(4)
 assertion: SpecificationErrorException
@**/

int f(x) 
auto int  x;	/* autoは使えない */
{
	return x;
}
