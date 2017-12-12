/**@
 spec: ブロックスコープでの定義の妥当例(7)
 assertion: 
@**/

void f1(int x) {
	void f1(int);
	void f2(double);
	{
		void f1(int);
		void f2(double);
	}
}

void f2(double x) {
}

