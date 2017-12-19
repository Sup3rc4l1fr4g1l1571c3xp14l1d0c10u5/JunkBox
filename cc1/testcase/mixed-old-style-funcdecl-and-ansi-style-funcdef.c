/**@
 spec: ANSI形式の関数宣言とK&R形式の関数定義が併用されていて、既定の実引数拡張によって引数型の一致が怪しくなるケース
 assertion: TypeMissmatchError
@**/

/* gcc    : -Wpedantic 時にのみ警告 promoted argument ‘x’ doesn't match prototype が出力される。 */
/* clang  :  warning: promoted type 'double' of K&R function parameter is not compatible with the parameter type 'float' declared in a previous prototype [-Wknr-promoted-parameter] */
/* splint : 宣言 float f(float); に対応する関数がないという警告。 */

float f(float); /* ANSI形式の関数宣言 */

int main(void)
{
	float x;
	f(x);
}

float f(x) /* K&R形式の関数定義 */
float x;   /* 既定の引数拡張で float -> double となるが、gcc はANSIとK&Rを混ぜると定義をプロトタイプ宣言で書き換える。 */
{
	return x;
}


