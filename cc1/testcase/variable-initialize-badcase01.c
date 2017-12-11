/**@
 spec: 不正な変数初期化子(1)
 assertion: SpecificationErrorException
@**/

typedef char buf[4];
buf dummy = {'f','r','e','e'};
buf x = dummy; // 無効な初期化

