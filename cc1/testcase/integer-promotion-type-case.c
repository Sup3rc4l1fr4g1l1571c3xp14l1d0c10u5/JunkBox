/**@
 spec: 整数拡張後の型
 assertion: 
@**/

void foo(void) { 
    signed char c = 0;
    signed char shift = 0;
    if (sizeof( c)  == sizeof(char)) {}
    if (sizeof(+c)  == sizeof(int)) {}
    if (sizeof(-c)  == sizeof(int)) {}
    if (sizeof(~c)  == sizeof(int)) {}
    if (sizeof(c,c) == sizeof(char)) {}
    if (sizeof(c<<shift) == sizeof(int)) {}
    if (sizeof(c>>shift) == sizeof(int)) {}
    return 0;
}

