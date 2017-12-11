/**@
 spec: 不正なgoto文とラベル付き文の使い方(1)
 assertion: SpecificationErrorException
@**/

void foo() { 
  goto L1; // 参照先がない
}
