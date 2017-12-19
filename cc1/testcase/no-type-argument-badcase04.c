/**@
 spec: 関数型中に型の無い仮引数名があるケース(4)
 assertion: SpecificationErrorException
@**/

int f(int (*F)(x,y,z)) {	/* 引数F中に型の無い仮引数名がある */
	return 0;
}

