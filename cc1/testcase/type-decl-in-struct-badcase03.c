/**@
 spec: 構造体の中で型定義はできない。
 assertion: SyntaxErrorException
@**/

struct A
{
  enum { c, d };  /* エラー */
 
  int a;
}; 

