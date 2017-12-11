/**@
 spec: 適切ではない位置でbreak/continue命令を使っている(4)
 assertion: SyntaxErrorException
@**/

void foo() { 
  for (({break;});;); // gcc拡張構文を使って for 文の評価式中で breakを使っている。

}
