/**@
 spec: C99では、列挙子並びの末尾にコンマを付けることができる(c89ではできない)
 assertion: 
@**/

enum foo
{
  abc,
  def,
  ghi ,
};

