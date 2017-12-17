/**@
 spec: 2017-12-15 型合成の実装漏れを確認した例(2)
 assertion: 
@**/

extern int foo();

int main(void) {
    extern int foo(int);
    return foo(1);
}

int foo(int x) {
    return x;
}

