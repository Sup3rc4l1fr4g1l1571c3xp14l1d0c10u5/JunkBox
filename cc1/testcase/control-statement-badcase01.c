/**@
spec: 適切ではない位置でbreak/continue命令を使っている(1)
 assertion: SyntaxErrorException
@**/

void foo() { 
  break; /* ループ外/switch文で break 命令を使用 */
}
