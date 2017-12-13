/**@
 spec: 構造体の中で型定義はできない。
 assertion: SyntaxErrorException
@**/

struct A
{
  typedef int int_t;  /* エラー */
 
  int a;
}; 

