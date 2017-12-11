/**@
 spec: 適切ではない位置でbreak/continue命令を使っている(2)
 assertion: SyntaxErrorException
@**/

void foo() { 
  continue; // ループ外で continue 命令を使用
}
