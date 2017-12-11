/**@
 spec: 不正なgoto文とラベル付き文の使い方(4)
 assertion: SyntaxErrorException
@**/

void foo() { 
L1: /* ラベル付き文なのでラベルの後ろに式要素が無いとエラー */ 
}

