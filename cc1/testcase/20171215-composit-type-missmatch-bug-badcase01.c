/**@
 spec: 2017-12-15 型合成の実装漏れを確認した例(3)
 assertion: 
@**/

extern int foo();

int main(void) {
    extern int foo(short);	/* conflicting type */
    return foo(1);
}

int foo(short x) {
    return x;
}
