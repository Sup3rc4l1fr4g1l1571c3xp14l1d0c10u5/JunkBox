/**@
 spec: 2017-12-15 型合成の実装漏れを確認した例(1)
 assertion: 
@**/

extern int foo();

int main(void) {
    extern int foo();
    return foo();
}

int foo(void) {
    return 0;
}

