/**@
 spec: 入れ子の構造体
 assertion: 
@**/

struct A
{
  struct B
  {
    int value;
  } b;
} a;
 
struct B x;  /* OK */

