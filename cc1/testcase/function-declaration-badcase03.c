/**@
 spec: 不正なな関数宣言のケース(3)
 assertion: SpecificationErrorException
@**/

int foo(const x,  const y)  /* ANSI形式の仮引数並びになるので */
int x, y;   /* これは不正 */
{
return x;
}

