/**@
 spec: 不正なgoto文とラベル付き文の使い方(3)
 assertion: SpecificationErrorException
@**/

void foo() { 
L1: ; 
}

void bar() { 
 goto L1; // スコープが違うから参照できない
}

