/**@
 spec: ブロックスコープでの定義の妥当例(3)
 assertion: 
@**/

int f1() {
    extern int i1;     /* 外部結合になるので妥当 */
    extern int i1;     /* 外部結合になるので妥当 */
	i1 = 0;
}
int i1 = 0;
