/**@
 spec: 不正なな関数宣言のケース(1)
 assertion: SpecificationErrorException
@**/

int g(void) { return 0; }

// 関数定義に初期値設定はダメ
int f(void) = g; 

