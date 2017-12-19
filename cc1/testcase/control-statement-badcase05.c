/**@
 spec: 適切ではない位置でbreak/continue命令を使っている(5)
 assertion: SyntaxErrorException
@**/

void foo() { 
  for (({continue;});;); /* gcc拡張構文を使って for 文の評価式中で continueを使っている。 */

}
