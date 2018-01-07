extern int printf(char*,...);

static int static_var = 0x12;
       int nonstatic_var = 0x34;
extern int extern_var = 0x56;
static int x = 0x1234;

int main(void) {
	int nonstatic_var = 0x1234;
	{
		static int x = 0xABCD;
		extern int nonstatic_var;
		printf("x = %x\n",x);	/* abcd */
		printf("nonstatic_var = %x\n",nonstatic_var);	/* 34 */
	}
	printf("x = %x\n",x);	/* 1234 */
	printf("nonstatic_var; = %x\n",nonstatic_var);	/* 1234 */
	return 0;
}
