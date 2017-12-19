/**@
 spec: K&R形式の関数定義・宣言の例
 assertion: 
@**/

int count(); /* 引数についての情報を持たない関数の宣言（空引数という意味ではない） */

int main(void) {
    int n = count("hello"); /* 引数についての情報を持たないので引数の数や型の検査は行われない */
    return n;
}

int count(str)
char* str;
{
    char* p = str;
    while (*p != '\0') {
        p++;
    }
    return p - str;
}


