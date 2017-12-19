/**@
 spec: 関数型中に型の無い仮引数名があるケース(8)
 assertion: SpecificationErrorException
@**/

int f() {
	extern int x(x,y,z); 	/* externで仮引数名の無い関数型がある */
	return 0;
}

