/**@
 spec: 適切ではない位置でbreak/continue命令を使っている(3)
 assertion: SyntaxErrorException
@**/

void foo(int first) { 
  switch(({ if (first) { first = 0; break; } 1; })) { /* gcc拡張構文を使って switch 文の評価式中で breakを使っている。 */
  case 2: return 2;
  default: return 0;
  }
}
