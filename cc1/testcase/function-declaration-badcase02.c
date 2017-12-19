/**@
 spec: 不正なな関数宣言のケース(2)
 assertion: SpecificationErrorException
@**/

int foo() 
int x; /* gcc: 仮引数 ‘x’ 用の宣言がありますが、そのような仮引数はありません */
{
    return x;
}
