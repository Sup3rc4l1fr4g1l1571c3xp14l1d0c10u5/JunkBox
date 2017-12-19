/**@
 spec: ブロックスコープでの定義の妥当例(4)
 assertion: 
@**/

int f1() {
    extern int i1;     /* 外部結合のため妥当 */
	{
	    static int i1;     /* 無結合でスコープが違うため妥当 */
	}
}

int f2() {
	static int i1;     /* 無結合でスコープが違うため妥当 */
	{
	    extern int i1;     /* 外部結合のため妥当 */
	}
}

