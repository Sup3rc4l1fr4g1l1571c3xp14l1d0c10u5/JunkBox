/**@
 spec: 構造体の中で型定義はできない。
 assertion: SyntaxErrorException
@**/

struct A
{
  struct B { int b; };  /* エラー */
 
  int a;
}; 

