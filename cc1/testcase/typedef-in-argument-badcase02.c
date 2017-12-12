/**@
 spec: 関数型の引数宣言 中で typedef
 assertion: SpecificationErrorException
@**/

typedef int (dummy)(typedef int INT32, INT32 x);

