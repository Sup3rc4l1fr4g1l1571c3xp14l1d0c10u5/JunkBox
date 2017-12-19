/**@
 spec: タグ型の宣言、変数宣言のどちらにもならない（意味を持たない）構造体の宣言。
 assertion: SpecificationErrorException
@**/

struct { int x; }; /* 文法の制約・意味上は問題がないが、意味のない宣言である。 */

