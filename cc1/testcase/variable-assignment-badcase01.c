/**@
 spec: 不正な変数代入(1)
 assertion: SpecificationErrorException
@**/

typedef char buf[4];

int main(void) {
	buf dummy = {'f','r','e','e'};
	buf x;
	
	x = dummy; // 無効な代入
}

