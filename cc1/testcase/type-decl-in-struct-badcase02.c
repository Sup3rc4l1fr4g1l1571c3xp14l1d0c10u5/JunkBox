/**@
 spec: 構造体の中で型定義はできない。
 assertion: 
@**/

struct A
{
  struct B { int b; };  /* エラー */
 
  int a;
}; 

