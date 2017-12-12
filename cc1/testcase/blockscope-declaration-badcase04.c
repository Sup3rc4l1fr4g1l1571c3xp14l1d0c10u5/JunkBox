/**@
 spec: ブロックスコープでの定義の違反例(4)
 assertion: TypeMissmatchError
@**/

int f1() {
    extern int i1;     // 外部結合になるので妥当
    extern int i1;     // 外部結合になるので妥当
	i1 = 0;
}
static int i1 = 0;	// //これ単体では内部結合だが、前に外部結合をもつ定義があるため，結合の不一致が生じ，6.2.2 によって動作は未定義となる  

