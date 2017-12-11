/**@
 spec: 不正なgoto文とラベル付き文の使い方(2)
 assertion: SpecificationErrorException
@**/

void foo() { 
 goto L1;
L1: ; 
L1: ; // 再定義
}
