/**@
 spec: 妥当な関数宣言のケース(5)
 assertion: 
@**/

/* 暗黙の宣言と合成型によってすべてが妥当となる */
int main(void) {
	int (*f)(int (*)(), double (*)[3]);
	return process(f);
}

int process(int (*f)(int (*)(char *), double (*)[3])) {
	return 1;
}

int process(int (*f)(int (*)(), double (*)[3]));
extern int process(int (*f)(int (*)(char *), double (*)[]));



