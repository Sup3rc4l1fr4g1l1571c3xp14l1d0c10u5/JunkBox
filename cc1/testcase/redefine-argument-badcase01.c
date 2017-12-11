/**@
 spec: 同名の宣言並びが存在するケース(1)
 assertion: SpecificationErrorException
@**/

int f(x) 
int x;
int x;  // 同名の宣言並びが存在
{
    return x;
}

