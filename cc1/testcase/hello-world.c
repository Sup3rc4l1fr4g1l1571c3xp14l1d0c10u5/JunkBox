/**@
 spec: お約束のhello, world
 assertion: 
@**/

extern int printf(const char *, ...);

int main(void) {
    printf("hello, world");	// 文字配列型から文字型へのポインタ型への暗黙の型変換
    return 0;
}
