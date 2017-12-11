/**@
 spec: 型定義名として宣言された識別子を仮引数として再宣言してはならない。
 assertion: SpecificationErrorException
@**/

// 6.9.1 関数定義
// 型定義名として宣言された識別子を仮引数として再宣言してはならない。
// K&Rでは関数宣言中、typedef 名を仮パラメータ名として使用できる。つまり、typedef 宣言を隠す。 	
// ANSI以降ではtypedef 名として宣言された識別子を仮パラメータとして使用できない。
// gccでは警告すら出ないためか、あまり知られていない。

typedef struct foo foo;

void blah(int foo) {
  foo = 1;
}
