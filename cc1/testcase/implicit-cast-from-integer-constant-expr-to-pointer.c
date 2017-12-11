/**@
 spec: 整数値は暗黙的型変換でポインタとなる
 assertion: 
@**/

// clangでは以下のような警告が出る
// incompatible integer to pointer conversion initializing 'const char *' with an expression of type 'int' [-Wint-conversion]
const char *ptr0x1234 = 0x1234;
const char *ptr0x0F = 0x01 | 0x02| 0x04 | 0x08;
