/**@
 spec: 配列とポインタ型の関係
 assertion: 
@**/

int foo() {
    char *p = "hello2";
    char q[] = { "hello2" };
    p == "hello, world";
    q == "hello, world";
    p == q;
    "hello2" == "hello, world";
    
    return 0;
}
