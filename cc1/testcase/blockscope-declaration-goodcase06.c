/**@
 spec: ブロックスコープでの定義の妥当例(6)
 assertion: 
@**/

void f2(double x) {
}

void f1(int y) {
	void f1(int);
	void f2(double);
	{
		void f1(int);
		void f2(double);
	}
}

