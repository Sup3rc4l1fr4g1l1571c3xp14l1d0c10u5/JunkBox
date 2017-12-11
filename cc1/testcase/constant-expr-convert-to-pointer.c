/**@
 spec: 定数式はポインタに暗黙的なキャストができる
 assertion: 
@**/

// clangでは以下のような警告が出る
// warning: expression which evaluates to zero treated as a null pointer constant of type 'const char *' [-Wnon-literal-null-conversion]
const char *nullptr = (2*4/8-1); // 計算結果が 0 になるため、ヌルポインタ定数として扱われる

