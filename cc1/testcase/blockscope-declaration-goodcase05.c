/**@
 spec: ブロックスコープでの定義の妥当例(5)
 assertion: 
@**/

extern void f4(int x) {
}

static void f3(double x) {
}

int f2(void) {
}

void f1(void) {
	extern void f1(void);
	extern int f2(void);
	extern void f3(double);
	extern void f4(int);
	void f1(void);
	int f2(void);
	void f3(double);
	void f4(int);
	{
		extern void f1(void);
		extern int f2(void);
		extern void f3(double);
		extern void f4(int);
		void f1(void);
		int f2(void);
		void f3(double);
		void f4(int);
	}
}

