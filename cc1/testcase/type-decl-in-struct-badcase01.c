/**@
 spec: 構造体の中で型定義はできない。
 assertion: 
@**/

struct A
{
  typedef int int_t;  /* エラー */
 
  int a;
}; 

