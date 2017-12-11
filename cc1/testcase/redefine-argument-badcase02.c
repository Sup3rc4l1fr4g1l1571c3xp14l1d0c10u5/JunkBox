/**@
 spec: 同名の宣言並びが存在するケース(2)
 assertion: SpecificationErrorException
@**/

int f(x) 
int x;
int f;  // 同名の宣言並びが存在
{
    return x;
}

