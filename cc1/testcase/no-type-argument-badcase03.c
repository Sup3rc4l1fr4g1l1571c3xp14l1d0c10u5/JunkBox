/**@
 spec: 関数型中に型の無い仮引数名があるケース(3)
 assertion: SpecificationErrorException
@**/

typedef int (*F)(x,y,z);	/* typedef中もダメ */

