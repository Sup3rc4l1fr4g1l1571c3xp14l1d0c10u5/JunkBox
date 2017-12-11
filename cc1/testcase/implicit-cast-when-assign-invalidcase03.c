/**@
spec: 代入時に暗黙の型変換ができない不正なケース(3)
 assertion: SpecificationErrorException
@**/

void foo(void) { 
    long double    ld  = 0;
    unsigned int   *p  = 0;

    // ポインターは、浮動小数点型に変換できない
    ld = p; // gcc =>  error: incompatible types when assigning to type ‘long double’ from type ‘float *’

}
