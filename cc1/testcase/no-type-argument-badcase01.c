/**@
 spec: 関数型中に型の無い仮引数名があるケース(1)
 assertion: SpecificationErrorException
@**/

struct hoge {
	int (*F)(int);
	int (*g)(x);	/* xの型が指定されていない */
};

