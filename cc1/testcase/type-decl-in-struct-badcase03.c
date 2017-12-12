/**@
 spec: 構造体の中で型定義はできない。
 assertion: 
@**/

struct A
{
  enum { c, d };  /* エラー */
 
  int a;
}; 

