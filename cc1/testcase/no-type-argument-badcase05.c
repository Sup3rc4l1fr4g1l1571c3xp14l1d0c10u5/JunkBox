/**@
 spec: 関数型中に型の無い仮引数名があるケース(5)
 assertion: SpecificationErrorException
@**/

int (*f)(x,y,z)() {	/* 戻り値に仮引数名の無い関数型がある */
	return 0;
}

