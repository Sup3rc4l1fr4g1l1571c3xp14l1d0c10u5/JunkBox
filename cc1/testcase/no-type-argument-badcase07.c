/**@
 spec: 関数型中に型の無い仮引数名があるケース(7)
 assertion: SpecificationErrorException
@**/

int f() {
	int (*x)(x,y,z) = 0; 	/* 仮引数に仮引数名の無い関数型がある */
	return 0;
}

